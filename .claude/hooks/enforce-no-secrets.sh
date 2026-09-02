#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Runs gitleaks against the staged diff before allowing `git commit`.
#
# Strict toolchain: gitleaks must be on PATH. The squad has decided that
# secrets discipline is non-negotiable, so missing gitleaks blocks the
# commit (rather than silently degrading) — same posture as the GPG hook.
#
# Bypass mechanisms (any one allows the commit):
#   - SQUAD_ALLOW_SECRETS_BYPASS=1 in the parent shell environment.
#     Use deliberately. Every bypass should be auditable; the squad's
#     orchestration log captures the env-var-set event.
#   - The commit is a merge or revert (those don't introduce new lines
#     that gitleaks should be inspecting; the underlying commits should
#     have been clean already).
#
# Performance: gitleaks's `--staged` mode reads only the staging area,
# typically <100ms even on large repos. Acceptable for every commit;
# no caching needed.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

command="$(printf '%s' "$payload" | python3 -c '
import json, sys
try:
    d = json.loads(sys.stdin.read())
    print(d.get("tool_input", {}).get("command", ""))
except Exception:
    print("")
' 2>/dev/null)"

# Short-circuit on anything that isn't a `git commit` invocation.
case "$command" in
  *"git commit"*) ;;
  *) exit 0 ;;
esac

# Skip merges and reverts.
case "$command" in
  *"git commit -m \"Merge "*|*"git commit -m 'Merge "*) exit 0 ;;
  *"git commit -m \"Revert "*|*"git commit -m 'Revert "*) exit 0 ;;
esac

# Env var escape hatch.
if [ "${SQUAD_ALLOW_SECRETS_BYPASS:-}" = "1" ]; then
  exit 0
fi

# 1. Toolchain check — gitleaks present?
if ! command -v gitleaks >/dev/null 2>&1; then
  cat >&2 <<EOF
🚫 gitleaks is not on PATH but the squad scans every commit for secrets.

Install:
  • macOS:    brew install gitleaks
  • linux:    download from https://github.com/gitleaks/gitleaks/releases
              or via homebrew/linuxbrew if you have it
  • via go:   go install github.com/gitleaks/gitleaks/v8@latest

Bypass for this one commit (deliberate; foreign env or you've already
verified externally):
  SQUAD_ALLOW_SECRETS_BYPASS=1 git commit ...

Reference: .claude/skills/security-toolchain/SKILL.md → "Layer 3 — Secrets".
EOF
  exit 2
fi

# 2. Run gitleaks against the staged diff.
project_dir="${CLAUDE_PROJECT_DIR:-$PWD}"

# Prefer a project-level config if one exists; otherwise gitleaks uses its
# defaults. The squad's recommended location is .gitleaks.toml at the repo
# root for project-specific rules and allowlists.
config_arg=""
if [ -f "$project_dir/.gitleaks.toml" ]; then
  config_arg="--config $project_dir/.gitleaks.toml"
fi

# `gitleaks protect --staged` scans the staging area only (not working tree,
# not history). Exit code 1 = leaks found; 0 = clean; other = error.
# `--no-banner` quiets the ASCII art that pollutes hook output.
# `--report-format json` to a temp file lets us format findings cleanly.
report="$(mktemp)"
trap 'rm -f "$report"' EXIT

gitleaks_out="$(gitleaks protect --staged --no-banner $config_arg --report-format json --report-path "$report" 2>&1)"
gitleaks_exit=$?

case "$gitleaks_exit" in
  0)
    # No leaks.
    exit 0
    ;;
  1)
    # Leaks found. Pretty-print findings.
    findings_summary="$(python3 - <<PY
import json, sys
try:
    with open("$report", "r", encoding="utf-8") as fh:
        findings = json.load(fh)
except Exception:
    print("(unable to parse gitleaks report)")
    sys.exit(0)

if not findings:
    print("(no findings parsed despite non-zero exit)")
    sys.exit(0)

# Show up to 5; collapse the rest.
shown = findings[:5]
extra = len(findings) - len(shown)

for f in shown:
    rule = f.get("RuleID") or f.get("Rule", "unknown")
    file = f.get("File", "?")
    line = f.get("StartLine", "?")
    desc = f.get("Description", rule)
    print(f"  • {file}:{line}  [{rule}]")
    print(f"      {desc}")

if extra > 0:
    print(f"  ... and {extra} more (see full report at {sys.argv[0] if False else '$report'})")
PY
)"

    cat >&2 <<EOF
🚫 Secrets detected in the staged diff. Commit blocked.

$findings_summary

Fix one of:
  • Remove the secret from the staged file. If it was added by accident,
    \`git restore --staged <file>\` then re-stage carefully.
  • If the value is a false positive (e.g. test fixture, public sample),
    add it to the allowlist in .gitleaks.toml at the repo root:
      [allowlist]
      regexes = ['<your-regex>']
      paths   = ['^tests/fixtures/.*\$']
  • Last-resort bypass for this commit only (auditable; use sparingly):
      SQUAD_ALLOW_SECRETS_BYPASS=1 git commit ...

Reference: .claude/skills/security-toolchain/SKILL.md → "Layer 3 — Secrets".
EOF
    exit 2
    ;;
  *)
    # gitleaks errored (config syntax, etc.). Fail loud.
    cat >&2 <<EOF
🚫 gitleaks errored while scanning the staged diff (exit $gitleaks_exit).

Output:
$gitleaks_out

This is most likely a malformed .gitleaks.toml. Fix the config and retry.
Bypass for one commit (only if you're confident the diff is clean):
  SQUAD_ALLOW_SECRETS_BYPASS=1 git commit ...
EOF
    exit 2
    ;;
esac
