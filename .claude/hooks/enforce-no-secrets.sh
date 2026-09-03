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
# Detection of "is this a `git commit`" is delegated to
# lib/git-commit-detect.sh (argv-level, shared with the other three
# commit-time hooks) rather than a substring match on "git commit" — see
# that file's header for the false-positive/false-negative bugs that
# replaces. The scan itself runs against the repo the command actually
# targets ($GIT_COMMIT_REPO_DIR), so `git -C <other-repo> commit` scans
# <other-repo>'s staging area, not the hook's own working directory.
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

hook_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/git-commit-detect.sh
source "$hook_dir/lib/git-commit-detect.sh"

git_commit_detect "$command"
if [ "$GIT_COMMIT_MATCH" != "1" ]; then
  exit 0
fi

# Skip merges and reverts (checked against the parsed argv, not raw text).
first_message=""
have_first_message=0
i=0
n=${#GIT_COMMIT_ARGV[@]}
while [ "$i" -lt "$n" ]; do
  tok="${GIT_COMMIT_ARGV[$i]}"
  case "$tok" in
    -m|--message)
      if [ "$have_first_message" != "1" ]; then
        have_first_message=1
        i=$((i + 1))
        [ "$i" -lt "$n" ] && first_message="${GIT_COMMIT_ARGV[$i]}"
      fi
      ;;
    --message=*)
      if [ "$have_first_message" != "1" ]; then
        have_first_message=1
        first_message="${tok#--message=}"
      fi
      ;;
  esac
  i=$((i + 1))
done
case "$first_message" in
  "Merge "*|"Revert "*) exit 0 ;;
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

# 2. Run gitleaks against the staged diff, scoped to the repo the command
# actually targets — $GIT_COMMIT_REPO_DIR when -C/--git-dir was given,
# otherwise this project's own root. Same repo whose .gitleaks.toml (if
# any) should govern the scan.
project_dir="${CLAUDE_PROJECT_DIR:-$PWD}"
target_dir="${GIT_COMMIT_REPO_DIR:-$project_dir}"

# If the resolved repo dir isn't actually accessible, fail open rather than
# let a `cd` failure masquerade as a gitleaks "malformed config" error below.
#
# `[ ! -d "$target_dir" ]` alone is not enough (round 4 re-review,
# review-b93b430.md, "four still-open items"): a directory can exist and
# pass `-d` while still not being `cd`-able (e.g. no execute/search
# permission, or removed out from under the process between checks). The
# gitleaks invocation below runs as
# `gitleaks_out="$(cd "$target_dir" ... && gitleaks ... 2>&1)"` --
# on a `cd` failure, `&&` short-circuits, gitleaks never runs, and the
# command substitution's exit status is `cd`'s own failure exit code, 1.
# The dispatch below treats exit 1 as "leaks found" (see the `case
# "$gitleaks_exit" in 1)` branch), so a non-cd-able-but-existing
# `target_dir` is read as leaks found and blocks the commit with an empty
# findings report -- verified live: `cd` into a 0-permission directory
# exits 1 with empty stdout, indistinguishable at that point from
# gitleaks itself finding something. Testing `cd`-ability directly here,
# in its own subshell so it can't change this script's actual working
# directory, closes that: a directory that exists but can't be entered
# now fails open (same posture as the plain `-d` case) instead of
# blocking on a phantom finding.
if [ ! -d "$target_dir" ] || ! (cd "$target_dir" 2>/dev/null); then
  exit 0
fi

# Prefer a project-level config if one exists; otherwise gitleaks uses its
# defaults. The squad's recommended location is .gitleaks.toml at the repo
# root for project-specific rules and allowlists.
gitleaks_args=(git --staged --no-banner --redact)
if [ -f "$target_dir/.gitleaks.toml" ]; then
  gitleaks_args+=(--config "$target_dir/.gitleaks.toml")
fi

# `gitleaks git --staged` scans the staging area only (not working tree,
# not history) — this repo's own .githooks/pre-commit uses the same
# subcommand. The previous version of this hook used the deprecated
# `gitleaks protect --staged`: on a gitleaks major bump that removes
# `protect` entirely, the unknown-subcommand error falls into the
# catch-all branch below and blocks EVERY commit with a misleading
# "malformed .gitleaks.toml" message, since gitleaks' own error text for
# an unrecognised subcommand doesn't distinguish itself from a config
# parse error in this hook's exit-code-only dispatch. `git` is the current,
# non-deprecated subcommand for staged/working-tree/history scans.
# Exit code 1 = leaks found; 0 = clean; other = error.
# `--no-banner` quiets the ASCII art that pollutes hook output.
# `--redact` keeps the actual secret value out of gitleaks' own stdout/
# report (this hook does its own bounded excerpt below).
# `--report-format json` to a temp file lets us format findings cleanly.
report="$(mktemp)"
trap 'rm -f "$report"' EXIT
gitleaks_args+=(--report-format json --report-path "$report")

gitleaks_out="$(cd "$target_dir" 2>/dev/null && gitleaks "${gitleaks_args[@]}" 2>&1)"
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
