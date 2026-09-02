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
#   * Legacy drops with no front-matter are tolerated indefinitely, not for
#     a single cycle: appended under a <!-- legacy --> marker and archived
#     normally, with no automated flush of that marker's contents.
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
                  "curator","tech-writer","ux-expert","csharp-dev","js-dev","devops","lead",
                  "critic","realist","spec-author",
                  "dreamer-first-principles","dreamer-informed","dreamer-convergence"}
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

# Detect front-matter; tolerate leading whitespace/newlines, a leading
# UTF-8 BOM, and a degenerate empty-body fence (`---\n---\n`).
#
# TODO(#8): this fence-boundary detection used to be a single
# `re.match(r"\s*---\s*\n(.*?)\n---\s*\n", text, re.DOTALL)`, which got two
# cases wrong -- both reviewer-verified false GREENs, proved end-to-end
# against this live validator:
#   * A leading UTF-8 BOM (U+FEFF, category Cf) is not matched by `\s`, so
#     a BOM'd drop with plainly-present frontmatter read as "no front
#     matter at all" and fell through to LEGACY mode below -- archived and
#     appended to decisions.md with the whitelist, verdict-enum,
#     scope-enum, and blockers-consistency checks skipped entirely. A
#     BOM'd drop declaring `agent: bogus-not-a-real-agent` was archived
#     clean; the same drop without the BOM was correctly quarantined for
#     "unknown agent".
#   * `---\n---\n` (zero lines between the markers) needed *two* newlines
#     under the old pattern -- one ending the opening line, one before the
#     closing line -- but the input only has one, so it also fell into
#     LEGACY mode instead of being quarantined for missing required
#     fields.
# Both are fixed here: strip a leading BOM, then find the closing fence as
# its own line-anchored search rather than requiring a blank line before
# it. See .claude/hooks/tests/agent_identity.py's `_frontmatter_body()` for
# the sibling fix to the *other* frontmatter-fence regex this codebase
# has (agent charter identity resolution, a different schema, not shared
# with this parser) -- same one-line root cause, fixed independently
# because the two parsers diverge immediately after fence detection.
#
# This is DISTINCT from the nested-key-promotion bug further down (search
# "KNOWN BUG" below), which remains open pending a user scope decision --
# only the fence-boundary bug above is fixed by this comment's change.
if text.startswith("\ufeff"):
    text = text[1:]
# Leading run: `\s*`, anchored at text-start (post-BOM-strip), so it can
# only ever consume whitespace before matching `---` -- NOT narrowed to
# `[ \t\r\n]`, which excludes 25 other code points Python's `\s` matches on
# `str` (NBSP U+00A0, form feed U+000C, vertical tab U+000B, U+001C-001F,
# U+0085, U+1680, U+2000-200A, U+2028, U+2029, U+202F, U+205F, U+3000). A
# prior narrowing here reopened the exact bug this comment block already
# describes for the BOM: an NBSP/form-feed/etc-prefixed drop with a bogus
# `agent:` fell into LEGACY mode (whitelist/verdict/scope/blockers checks
# skipped) instead of being parsed and quarantined -- reviewer-verified
# end-to-end, same blast radius as the BOM case above. The trailing
# `[ \t]*` immediately below is intentionally NOT `\s*`: it sits before the
# fence line's own newline and must not consume it, so it stays restricted
# to horizontal whitespace. See .claude/hooks/tests/agent_identity.py's
# `_frontmatter_body()` for the sibling fix.
m = re.match(r"\s*---[ \t]*\r?\n", text)
if not m:
    # Legacy mode — no front-matter at all.
    print("LEGACY")
    sys.exit(0)
rest = text[m.end():]
# `(?:\r?\n|\Z)`, not a bare `\r?\n`: a closing fence at end-of-file with
# no trailing newline (`---\nid: x\n---`, no final `\n`) is a
# syntactically complete fence, but a newline-only terminator never
# matches at `\Z` -- pre-existing, same class as the empty-body-fence bug
# above, not introduced by this pass. `\Z` rather than `$` because `$` in
# MULTILINE mode also matches just before a trailing `\n`, which would
# wrongly accept a fence line followed by more content on the same
# logical line. See agent_identity.py's `_frontmatter_body()` for the
# sibling fix.
close = re.search(r"(?m)^---[ \t]*(?:\r?\n|\Z)", rest)
if not close:
    print("LEGACY")
    sys.exit(0)
fm = rest[:close.start()]

# Minimal YAML parse: lines of `key: value`, plus list items starting with
# `- ` (block style, e.g. `  - file: ...`) whose *further-indented* follow-on
# `key: value` lines extend the same list item — the nested-mapping form
# documented as canonical in decision-schema.md. We don't need the full spec.
def _unquote(v):
    if (v.startswith('"') and v.endswith('"')) or (v.startswith("'") and v.endswith("'")):
        return v[1:-1]
    return v

fields = {}
current_list = None        # name of the key currently collecting list items
current_item = None        # dict of the list item currently being extended
current_item_indent = None # indentation column of that item's `- ` marker

for raw in fm.splitlines():
    line = raw.rstrip()
    if not line or line.lstrip().startswith("#"):
        continue
    indent = len(line) - len(line.lstrip(" "))
    stripped = line.strip()

    if stripped.startswith("- "):
        if current_list is None:
            print("malformed yaml: list item without parent key")
            sys.exit(1)
        item_body = stripped[2:].strip()
        # `- key: value` starts a mapping item; further-indented lines below
        # it extend the same mapping (see the continuation branch below).
        # Flow-style (`- {k: v, ...}`) and bare scalar items are stored as-is.
        if item_body.startswith("{") or ":" not in item_body:
            fields.setdefault(current_list, []).append(item_body)
            current_item = None
        else:
            k, _, v = item_body.partition(":")
            current_item = {k.strip(): _unquote(v.strip())}
            fields.setdefault(current_list, []).append(current_item)
        current_item_indent = indent
        continue

    if current_item is not None and indent > current_item_indent and ":" in stripped:
        # Continuation of the current list item's mapping (e.g. `line:`,
        # `reason:` following `- file:` at a deeper indent).
        k, _, v = stripped.partition(":")
        current_item[k.strip()] = _unquote(v.strip())
        continue

    if ":" in stripped:
        k, _, v = stripped.partition(":")
        k = k.strip()
        v = v.strip()
        current_item = None
        current_item_indent = None
        if v == "":
            current_list = k
            fields.setdefault(k, [])
        else:
            current_list = None
            # KNOWN BUG, tracked deliberately -- TODO(#8): NOT fixed in
            # this pass; scope needs a user call (see below). Note this is
            # the OTHER half of #8 -- the fence-boundary bug documented
            # above (BOM / empty-body fence) IS fixed as of this pass; only
            # the nested-key-promotion bug below remains open. This branch
            # has no indentation guard: it treats ANY `key: value` line as
            # top-level, including one nested under an unrelated key (e.g.
            # `meta:\n  agent: reviewer`), and a later top-level write wins.
            # Confirmed reachable and exploitable against this live
            # validator:
            #   agent: bogus-not-a-real-agent
            #   meta:
            #     agent: reviewer
            #   -> VALID|reviewer|... (bogus- agent is what actually
            #   declared the drop; the nested `agent: reviewer` silently
            #   overwrote it and passed the ALLOWED_AGENTS check).
            # Consequences: the agent whitelist above is bypassable, and
            # `.last-review-verdict` (read by the statusline) is forgeable
            # by any drop that nests an `agent:`/`verdict:` key -- a drop
            # can impersonate a `reviewer` PASS it never earned. Fixing this
            # requires an indentation guard equivalent to the one the list-
            # item continuation branch already has above, which would
            # change acceptance behaviour for some currently-accepted drops
            # -- see https://github.com/MCGPPeters/squad-template/issues/8
            # for the scope question raised to the user; do not silently
            # tighten this without that decision.
            fields[k] = _unquote(v)
    else:
        # Unrecognised continuation — ignore.
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
