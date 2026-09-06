#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Blocks `git commit` and `git push` when the current branch is `main` or `master`.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "tool_name": "Bash",
#     "tool_input": { "command": "...", "description": "..." }
#   }

set -euo pipefail

# Read the payload, extract the command via python3 (no jq dependency).
payload="$(cat)"
command="$(printf '%s' "$payload" | python3 -c 'import json,sys
try:
    print(json.loads(sys.stdin.read()).get("tool_input",{}).get("command",""))
except Exception:
    print("")
' 2>/dev/null)"

# Only inspect git commands.
case "$command" in
  *"git commit"*|*"git push"*) ;;
  *) exit 0 ;;
esac

# Discover the current branch. If we're not in a git repo, do nothing.
if ! current_branch="$(git rev-parse --abbrev-ref HEAD 2>/dev/null)"; then
  exit 0
fi

case "$current_branch" in
  main|master)
    cat >&2 <<EOF
🚫 Direct commits to '${current_branch}' are forbidden.

The squad's git workflow requires all changes to go through feature branches and pull requests:

  1. Create a feature branch:
       git checkout -b <type>/<issue-number>-<short-slug>
     where <type> is one of: feature, fix, docs, refactor, test, perf, security, ci, build, chore

  2. Commit your work on the feature branch.

  3. Push the feature branch and open a pull request to '${current_branch}'.

See .claude/docs/decisions.md (Git Workflow) for the full rule.
EOF
    exit 2
    ;;
esac

exit 0
