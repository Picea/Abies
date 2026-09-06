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
# Usage — either call works from a hook script; use whichever one matches
# what you already have in hand:
#
#   source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/lib/git-commit-detect.sh"
#
#   # (a) $command already extracted from the PreToolUse JSON payload:
#   git_commit_detect "$command"
#
#   # (b) the RAW PreToolUse JSON payload, not yet unpacked — extraction and
#   #     detection happen in ONE guarded python3 invocation instead of an
#   #     unguarded extraction ahead of a separately-guarded detection:
#   git_commit_detect_from_payload "$payload"
#
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
# Fails CLOSED (exit 2 — terminates the CALLING hook script, since this file
# is sourced rather than subshelled) when python3 is missing or the
# classifier itself exits non-zero for any reason, including a stub
# interpreter present on PATH but broken (PR #358 review round 2, 🔴-C).
# Before this guard existed, `git_commit_detect()`'s `if ! parsed=...; then
# return 0; fi` treated "the classifier could not run at all" identically to
# "the classifier ran and found no match" — under a broken python3, every one
# of the four hooks that source this file (enforce-no-secrets.sh,
# enforce-gpg-signing.sh, block-large-files.sh,
# enforce-conventional-commits.sh) silently ALLOWED every commit instead of
# refusing. One guard here closes it for all of them and for any future
# caller of either public function below, rather than each hook carrying its
# own copy of the check.

# Fails CLOSED (exit 2) if python3 is not on PATH. Called as a plain
# statement, never inside a `$(...)` substitution, so `exit` here terminates
# the calling hook script rather than a throwaway subshell.
_git_commit_detect_require_python3() {
  command -v python3 >/dev/null 2>&1 && return 0
  echo "🚫 git-commit-detect.sh: python3 is required to classify this command and is not on PATH -- refusing rather than silently letting an unclassified git invocation past this gate." >&2
  exit 2
}

# Fails CLOSED (exit 2). Called as a plain statement (via `||`), never inside
# a `$(...)` substitution, for the same reason as above.
_git_commit_detect_fail_closed() {
  echo "🚫 git-commit-detect.sh: the git-commit classifier exited non-zero -- refusing rather than treating a parser failure as \"nothing to gate\"." >&2
  exit 2
}

git_commit_detect() {
  local command="$1"
  local lib_dir
  lib_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

  GIT_COMMIT_MATCH=0
  GIT_COMMIT_GLOBAL_ARGS=()
  GIT_COMMIT_ARGV=()
  GIT_COMMIT_REPO_DIR=""

  _git_commit_detect_require_python3

  local parsed
  parsed="$(printf '%s' "$command" | python3 "$lib_dir/git_commit_detect.py" 2>/dev/null)" \
    || _git_commit_detect_fail_closed

  # $parsed is produced entirely by git_commit_detect.py using shlex.quote
  # on every token, so it's safe to eval: the only unquoted content is the
  # fixed variable names and the literal 0/1 on GIT_COMMIT_MATCH.
  eval "$parsed"

  if [ "$GIT_COMMIT_MATCH" = "1" ]; then
    GIT_COMMIT_REPO_DIR="$(git "${GIT_COMMIT_GLOBAL_ARGS[@]}" rev-parse --show-toplevel 2>/dev/null || true)"
  fi

  return 0
}

# Same contract as git_commit_detect(), but takes the RAW PreToolUse JSON
# payload (before tool_input.command has been pulled out of it) instead of an
# already-extracted command string. Lets the four commit-time hooks get JSON
# extraction and argv-level detection from ONE guarded python3 invocation,
# instead of each running its own unguarded extraction ahead of this file
# ever being sourced (PR #358 review round 2, 🔴-C).
git_commit_detect_from_payload() {
  local payload="$1"
  local lib_dir
  lib_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

  GIT_COMMIT_MATCH=0
  GIT_COMMIT_GLOBAL_ARGS=()
  GIT_COMMIT_ARGV=()
  GIT_COMMIT_REPO_DIR=""

  _git_commit_detect_require_python3

  local parsed
  parsed="$(printf '%s' "$payload" | python3 "$lib_dir/git_commit_detect.py" --payload 2>/dev/null)" \
    || _git_commit_detect_fail_closed

  eval "$parsed"

  if [ "$GIT_COMMIT_MATCH" = "1" ]; then
    GIT_COMMIT_REPO_DIR="$(git "${GIT_COMMIT_GLOBAL_ARGS[@]}" rev-parse --show-toplevel 2>/dev/null || true)"
  fi

  return 0
}
