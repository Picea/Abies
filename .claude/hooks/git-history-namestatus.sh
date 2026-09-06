#!/usr/bin/env bash
#
# NOT A HOOK. Wired into no event in settings.json.
#
# That first line is load-bearing. `.claude/hooks/tests/invariant-chain.sh`
# asserts every script in this directory is wired unless its header declares
# `NOT A HOOK` on a comment line of its own, and this is the only script that
# does. Reword it and the suite will correctly report this file as an unwired
# hook. The check matches the marker at the start of a comment line rather
# than the phrase anywhere in the header, because prose that merely mentioned
# "not a hook" once exempted a real hook and printed a green line saying so.
#
# The sanctioned git-history channel for `reviewer-blind`. `git log` carries
# code context and author narrative through one pipe; this script emits only
# the first half — commit hash, author date, and --name-status — so the blind
# reviewer can answer "which files changed when, and was anything reverted"
# without ever seeing a commit subject, body or trailer.
#
# .claude/hooks/enforce-review-history-channel.sh refuses raw `git log`,
# `git show`, `git blame`, `gh pr view` and `gh issue view` for that agent and
# names this script in the refusal.
#
# Usage:
#   bash .claude/hooks/git-history-namestatus.sh [<rev-range>] [-- <path>...]
#
#   bash .claude/hooks/git-history-namestatus.sh
#   bash .claude/hooks/git-history-namestatus.sh main..HEAD
#   bash .claude/hooks/git-history-namestatus.sh -- src/Orders
#   bash .claude/hooks/git-history-namestatus.sh main..HEAD -- src/Orders
#
# Output, one commit per block:
#   <short-sha>  <author-date, ISO-8601>
#   M  path/one.cs
#   A  path/two.cs
#
# The status letters are git's: A added, M modified, D deleted, R renamed,
# C copied. A revert shows up as the inverse status on the same paths, which
# is the signal this channel exists to preserve.
#
# Runs against the working directory it is invoked from, so it follows a
# worktree the way the agent does. There is no ${CLAUDE_PROJECT_DIR} in it,
# deliberately: that variable stays at the project root where the session
# started and would report the wrong checkout's history from inside a worktree.
#
# Exit codes:
#   0 — output written
#   1 — not a git repository, or git failed
#   2 — the argument list contains a format flag (see below)

set -euo pipefail

MAX_COMMITS="${GIT_HISTORY_MAX_COMMITS:-200}"

# Refuse any argument that could reintroduce the narrative through a format
# string. The whole point of this script is that the caller cannot ask for
# %s or %b back.
for arg in "$@"; do
  case "$arg" in
    --format*|--pretty*|--oneline|-p|--patch|-u|--stat|--shortstat|--graph|--all-match|--grep*)
      printf '%s\n' "🚫 '$arg' is not accepted here." >&2
      printf '%s\n' "This channel emits hashes, dates and --name-status only. Formatting flags could carry commit messages back through it, which is exactly what it exists to withhold." >&2
      exit 2
      ;;
  esac
done

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  printf '%s\n' "Not inside a git work tree." >&2
  exit 1
fi

exec git --no-pager log \
  --max-count="$MAX_COMMITS" \
  --no-color \
  --no-decorate \
  --name-status \
  --date=iso-strict \
  --format='%h  %ad' \
  "$@"
