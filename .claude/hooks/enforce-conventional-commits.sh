#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Validates that `git commit -m "..."` messages follow Conventional Commits.
#
# Allowed types: feat, fix, docs, refactor, test, perf, security, ci, build, chore
# Allowed shape:  <type>[(<scope>)][!]: <subject>
# Examples:
#   feat: add article publish endpoint
#   fix(auth): reject expired tokens
#   refactor!: rename Article to Post
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)

set -euo pipefail

payload="$(cat)"
command="$(printf '%s' "$payload" | python3 -c 'import json,sys
try:
    print(json.loads(sys.stdin.read()).get("tool_input",{}).get("command",""))
except Exception:
    print("")
' 2>/dev/null)"

# Only inspect commits.
case "$command" in
  *"git commit"*) ;;
  *) exit 0 ;;
esac

# Skip --amend without a new message, merges, and reverts (which generate their own messages).
case "$command" in
  *" --amend"*)
    case "$command" in
      *" -m "*|*" --message"*) ;;  # amending with a new message — still validate
      *) exit 0 ;;
    esac
    ;;
  *"git commit -m \"Merge "*|*"git commit -m 'Merge "*) exit 0 ;;
  *"git commit -m \"Revert "*|*"git commit -m 'Revert "*) exit 0 ;;
esac

# Extract the message after `-m` or `--message`. Handles both single and double quotes.
# We deliberately keep this regex modest; pathological shell quoting is the user's problem.
message="$(printf '%s' "$command" | sed -nE "s/.*-m[[:space:]]+[\"']([^\"']*)[\"'].*/\1/p" | head -n1)"

# If we couldn't extract a message (e.g. heredoc, file-based), let it through — we'd rather
# under-block than false-positive.
if [ -z "$message" ]; then
  exit 0
fi

# Conventional Commits regex. Subject must be non-empty.
pattern='^(feat|fix|docs|refactor|test|perf|security|ci|build|chore)(\([a-z0-9-]+\))?!?: .+'

if ! printf '%s' "$message" | grep -qE "$pattern"; then
  cat >&2 <<EOF
🚫 Commit message does not follow Conventional Commits.

Got:
  ${message}

Required shape:
  <type>[(<scope>)][!]: <subject>

Allowed types: feat, fix, docs, refactor, test, perf, security, ci, build, chore
Use ! after the type/scope to mark a breaking change.

Examples:
  feat: add article publish endpoint
  fix(auth): reject expired tokens
  refactor!: rename Article to Post

See .claude/docs/decisions.md (Git Workflow) for the full rule.
EOF
  exit 2
fi

exit 0
