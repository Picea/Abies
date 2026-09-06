#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Validates branch names when creating a new branch via `git checkout -b` or `git switch -c`.
#
# Required shape:  <type>/<issue-number>-<short-slug>
# Allowed types:   feature, fix, docs, refactor, test, perf, security, ci, build, chore
# Issue number:    digits only (use 0 if there is no tracked issue)
# Short slug:      lowercase letters, digits, and hyphens
#
# Examples:
#   feature/123-article-publish-endpoint
#   fix/456-expired-token-handling
#   chore/0-bump-dotnet-sdk
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

# Extract a candidate branch name from one of the two creation forms.
branch=""
if printf '%s' "$command" | grep -qE 'git[[:space:]]+checkout[[:space:]]+-b[[:space:]]+'; then
  branch="$(printf '%s' "$command" | sed -nE 's/.*git[[:space:]]+checkout[[:space:]]+-b[[:space:]]+([^[:space:]]+).*/\1/p' | head -n1)"
elif printf '%s' "$command" | grep -qE 'git[[:space:]]+switch[[:space:]]+-c[[:space:]]+'; then
  branch="$(printf '%s' "$command" | sed -nE 's/.*git[[:space:]]+switch[[:space:]]+-c[[:space:]]+([^[:space:]]+).*/\1/p' | head -n1)"
else
  exit 0
fi

if [ -z "$branch" ]; then
  exit 0
fi

# Strip surrounding quotes if present.
branch="${branch%\"}"; branch="${branch#\"}"
branch="${branch%\'}"; branch="${branch#\'}"

pattern='^(feature|fix|docs|refactor|test|perf|security|ci|build|chore)/[0-9]+-[a-z0-9-]+$'

if ! printf '%s' "$branch" | grep -qE "$pattern"; then
  cat >&2 <<EOF
🚫 Branch name does not match the squad convention.

Got:
  ${branch}

Required shape:
  <type>/<issue-number>-<short-slug>

Allowed types: feature, fix, docs, refactor, test, perf, security, ci, build, chore
Issue number: digits only (use 0 if there is no tracked issue)
Slug:         lowercase letters, digits, and hyphens

Examples:
  feature/123-article-publish-endpoint
  fix/456-expired-token-handling
  chore/0-bump-dotnet-sdk

See .claude/docs/decisions.md (Git Workflow) for the full rule.
EOF
  exit 2
fi

exit 0
