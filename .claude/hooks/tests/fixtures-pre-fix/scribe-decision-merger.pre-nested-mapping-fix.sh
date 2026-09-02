#!/usr/bin/env bash
#
# SubagentStop hook.
# Validates and merges decision drops from .squad/decisions/inbox/ into
# .claude/docs/decisions.md, quarantining malformed entries and updating
# the .squad/.last-review-verdict cache used by the statusline.
#
# Schema reference: .claude/docs/decision-schema.md
#
# Behaviour:
#   * Each *.md file in inbox/ is parsed for YAML front-matter.
#   * Valid drops are appended under "## Session Decisions" and moved to
#     .squad/decisions/archive/<YYYY-MM>/.
#   * Malformed drops are moved to .squad/decisions/quarantine/ with a
#     sibling .reason file containing the validator's complaint.
#   * Legacy drops with no front-matter are tolerated once: appended under
#     a <!-- legacy --> marker, archived normally.
#   * If the most recent successfully-merged drop is from `agent: reviewer`,
#     its verdict is written to .squad/.last-review-verdict.
#
# Exit codes:
#   0 — always (this hook never blocks).

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

read -r STOP_ACTIVE PAYLOAD_CWD <<<"$(
  printf '%s' "$payload" | python3 -c '
import json,sys
try:
    d = json.loads(sys.stdin.read())
except Exception:
    d = {}
print("true" if d.get("stop_hook_active") else "false", d.get("cwd",""))
' 2>/dev/null
)"

[ "${STOP_ACTIVE:-false}" = "true" ] && exit 0

project_dir="${CLAUDE_PROJECT_DIR:-${PAYLOAD_CWD:-$PWD}}"
[ -z "$project_dir" ] && project_dir="$PWD"

inbox="$project_dir/.squad/decisions/inbox"
quarantine="$project_dir/.squad/decisions/quarantine"
decisions_doc="$project_dir/.claude/docs/decisions.md"
verdict_cache="$project_dir/.squad/.last-review-verdict"

[ -d "$inbox" ] || exit 0
[ -f "$decisions_doc" ] || exit 0

shopt -s nullglob
files=( "$inbox"/*.md )
shopt -u nullglob
[ "${#files[@]}" -eq 0 ] && exit 0

mkdir -p "$quarantine"

anchor='## Session Decisions'
grep -qF "$anchor" "$decisions_doc" || printf '\n%s\n' "$anchor" >> "$decisions_doc"

today="$(date -u +%Y-%m-%d)"
archive_month="$(date -u +%Y-%m)"
archive_dir="$project_dir/.squad/decisions/archive/$archive_month"
mkdir -p "$archive_dir"

# Validator. Returns:
#   0 + stdout="VALID|<agent>|<verdict>|<scope>|<id>"  for valid front-matter drops
#   0 + stdout="LEGACY"                                 for files with no front-matter (legacy mode)
#   1 + stdout="<reason>"                               for invalid drops
validate() {
  local file="$1"
  python3 - "$file" <<'PY'
import sys, re
ALLOWED_AGENTS = {"reviewer","security-expert","performance-engineer","architect",
                  "curator","tech-writer","ux-expert","csharp-dev","js-dev","devops","lead"}
ALLOWED_VERDICTS = {"PASS","NEEDS-CHANGES","BLOCKED","INFO"}
ALLOWED_SCOPES   = {"review","decision","threat-model","benchmark","retro",
                    "handoff","architecture","doc","other"}

path = sys.argv[1]
try:
    with open(path, "r", encoding="utf-8") as fh:
        text = fh.read()
except Exception as e:
    print(f"unreadable: {e}")
    sys.exit(1)

# Detect front-matter; tolerate leading whitespace/newlines.
m = re.match(r"\s*---\s*\n(.*?)\n---\s*\n", text, re.DOTALL)
if not m:
    # Legacy mode — no front-matter at all.
    print("LEGACY")
    sys.exit(0)

fm = m.group(1)

# Minimal YAML parse: lines of `key: value`, plus simple list items
# starting with `  - `. We don't need the full spec.
fields = {}
current_list = None
for raw in fm.splitlines():
    line = raw.rstrip()
    if not line or line.lstrip().startswith("#"):
        continue
    if line.startswith("  - ") or line.startswith("- "):
        if current_list is None:
            print("malformed yaml: list item without parent key")
            sys.exit(1)
        fields.setdefault(current_list, []).append(line.strip()[2:].strip())
        continue
    if ":" in line:
        k, _, v = line.partition(":")
        k = k.strip()
        v = v.strip()
        if v == "":
            current_list = k
            fields.setdefault(k, [])
        else:
            current_list = None
            # Strip surrounding quotes if any.
            if (v.startswith('"') and v.endswith('"')) or (v.startswith("'") and v.endswith("'")):
                v = v[1:-1]
            fields[k] = v
    else:
        # Continuation of a list item or unknown — ignore.
        pass

required = ["id","agent","verdict","scope","created"]
missing = [r for r in required if r not in fields or fields[r] in (None,"",[])]
if missing:
    print(f"missing required fields: {','.join(missing)}")
    sys.exit(1)

agent = fields["agent"]
verdict = fields["verdict"]
scope = fields["scope"]

if agent not in ALLOWED_AGENTS:
    print(f"unknown agent: {agent}")
    sys.exit(1)
if verdict not in ALLOWED_VERDICTS:
    print(f"invalid verdict: {verdict}")
    sys.exit(1)
if scope not in ALLOWED_SCOPES:
    print(f"invalid scope: {scope}")
    sys.exit(1)

# Verdict ↔ blockers consistency.
blockers = fields.get("blockers", [])
if isinstance(blockers, str):
    # Inline form like `blockers: []`.
    blockers = [] if blockers.strip() in ("[]","") else [blockers]
has_blockers = bool(blockers)

if verdict == "PASS" and has_blockers:
    print("verdict PASS but blockers list is non-empty")
    sys.exit(1)
if verdict in ("NEEDS-CHANGES","BLOCKED") and not has_blockers:
    print(f"verdict {verdict} but blockers list is empty")
    sys.exit(1)

print(f"VALID|{agent}|{verdict}|{scope}|{fields['id']}")
sys.exit(0)
PY
}

last_reviewer_verdict=""
last_reviewer_created=""
appended_count=0

tmp_append="$(mktemp)"
trap 'rm -f "$tmp_append"' EXIT

for f in "${files[@]}"; do
  result="$(validate "$f" 2>&1)"
  status=$?

  if [ "$status" -ne 0 ]; then
    # Quarantine.
    ts="$(date -u +%Y-%m-%dT%H-%M-%S)"
    base="$(basename "$f")"
    mv "$f" "$quarantine/${ts}-${base}" 2>/dev/null || true
    printf 'reason: %s\nquarantined_at: %sZ\n' "$result" "$(date -u +%Y-%m-%dT%H:%M:%S)" > "$quarantine/${ts}-${base}.reason"
    continue
  fi

  if [ "$result" = "LEGACY" ]; then
    {
      printf '\n<!-- legacy -->\n### %s — %s\n\n' "$today" "$(basename "$f" .md)"
      cat "$f"
      printf '\n'
    } >> "$tmp_append"
  else
    # VALID|agent|verdict|scope|id
    IFS='|' read -r _tag agent verdict scope drop_id <<<"$result"
    {
      printf '\n### %s — %s [%s · %s]\n\n' "$today" "$drop_id" "$agent" "$verdict"
      cat "$f"
      printf '\n'
    } >> "$tmp_append"

    if [ "$agent" = "reviewer" ] && [ "$scope" = "review" ]; then
      # Pull the `created` field to compare; pick the latest.
      created="$(grep -E '^created:' "$f" | head -n1 | sed -E 's/^created:[[:space:]]*//' | tr -d '"'"'")"
      if [ -z "$last_reviewer_created" ] || [[ "$created" > "$last_reviewer_created" ]]; then
        last_reviewer_verdict="$verdict"
        last_reviewer_created="$created"
      fi
    fi
  fi

  appended_count=$((appended_count + 1))
  ts="$(date -u +%Y-%m-%dT%H-%M-%S)"
  mv "$f" "$archive_dir/${ts}-$(basename "$f")" 2>/dev/null || true
done

if [ "$appended_count" -gt 0 ]; then
  cat "$tmp_append" >> "$decisions_doc"
fi

# Update the verdict cache for the statusline. Only overwrite if we saw a
# reviewer verdict this round; otherwise leave the previous value alone.
if [ -n "$last_reviewer_verdict" ]; then
  printf '%s\n' "$last_reviewer_verdict" > "$verdict_cache"
fi

exit 0
