#!/usr/bin/env bash
#
# Shared argv-level detector for `git ... commit ...` invocations. Sourced
# by the four commit-time PreToolUse hooks (enforce-conventional-commits.sh,
# enforce-gpg-signing.sh, enforce-no-secrets.sh, block-large-files.sh) so the
# detection logic exists in exactly one place instead of four divergent
# copies.
#
# Each of those hooks stays independently runnable: this file only defines
# a function, it has no side effects on its own, and every hook still works
# standalone as a PreToolUse command.
#
# Why not `case "$command" in *"git commit"*)`? Two confirmed bugs in that
# form (see PR #355's review and .claude/docs/decisions.md):
#   - False negative: `git -C <path> commit`, `git --git-dir=... commit`,
#     and `git -c k=v commit` never contain the literal substring
#     "git commit", so all four hooks silently exit 0.
#   - False positive: a payload that merely *contains* the text
#     "git commit" (e.g. `echo see: git commit -m msg`) triggers them.
#
# Usage (from a hook script, after $command has been extracted from the
# PreToolUse JSON payload):
#
#   source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/lib/git-commit-detect.sh"
#   git_commit_detect "$command"
#   if [ "$GIT_COMMIT_MATCH" != "1" ]; then
#     exit 0
#   fi
#   # ${GIT_COMMIT_GLOBAL_ARGS[@]} — global git options (-C, --git-dir,
#   #   --work-tree, -c, ...) exactly as given, in order. Replay these in
#   #   front of any further `git` invocation to act on the SAME repo the
#   #   original command targeted, e.g.:
#   #     git "${GIT_COMMIT_GLOBAL_ARGS[@]}" config --get user.email
#   # ${GIT_COMMIT_ARGV[@]} — every token after the `commit` subcommand
#   #   itself (e.g. `-m msg --amend`).
#   # $GIT_COMMIT_REPO_DIR — best-effort resolved worktree root for the
#   #   targeted repo (via `git "${GIT_COMMIT_GLOBAL_ARGS[@]}" rev-parse
#   #   --show-toplevel`). Empty string if resolution failed; callers
#   #   should fall back to $CLAUDE_PROJECT_DIR / $PWD in that case, same
#   #   as before this helper existed.
#
# Parsing itself lives in the co-located git_commit_detect.py (real argv
# tokenization via `shlex`, not string matching) — see its header for the
# documented scope limits (no command-substitution evaluation, best-effort
# on unrecognised global flags).
#
# Fails open on any internal error (python3 missing, parser crash,
# unparseable quoting): GIT_COMMIT_MATCH=0, matching the "under-block over
# false-positive" posture every hook here already takes.

git_commit_detect() {
  local command="$1"
  local lib_dir
  lib_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

  GIT_COMMIT_MATCH=0
  GIT_COMMIT_GLOBAL_ARGS=()
  GIT_COMMIT_ARGV=()
  GIT_COMMIT_REPO_DIR=""

  local parsed
  if ! parsed="$(printf '%s' "$command" | python3 "$lib_dir/git_commit_detect.py" 2>/dev/null)"; then
    return 0
  fi

  # $parsed is produced entirely by git_commit_detect.py using shlex.quote
  # on every token, so it's safe to eval: the only unquoted content is the
  # fixed variable names and the literal 0/1 on GIT_COMMIT_MATCH.
  eval "$parsed"

  if [ "$GIT_COMMIT_MATCH" = "1" ]; then
    GIT_COMMIT_REPO_DIR="$(git "${GIT_COMMIT_GLOBAL_ARGS[@]}" rev-parse --show-toplevel 2>/dev/null || true)"
  fi

  return 0
}
