#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Blocks `git commit` and `git push` when the DESTINATION branch is `main` or
# `master`.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Bash",
#     "tool_input": { "command": "...", "description": "..." }
#   }
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR reads the wrong repository's branch from inside a
# worktree — and still passes any test that has no worktree in it. Same
# rationale as enforce-review-verdict.sh, which gates the identical class of
# commands.

set -uo pipefail

# 🔴-4 (PR #358 review round 1): a missing/broken python3 used to produce
# empty stdout, indistinguishable from "not a git command", so this hook
# silently allowed every commit/push instead of refusing. Checked once, here,
# before anything else runs.
command -v python3 >/dev/null 2>&1 || {
  echo "🚫 block-direct-commits-to-main.sh: python3 is required to classify this command and is not on PATH -- refusing rather than silently letting an unclassified git invocation past this gate." >&2
  exit 2
}

hook_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export HOOKS_LIB_DIR="$hook_dir/lib"

payload="$(cat)"

# ⚠️-11 (PR #358 review round 1): `case "$command" in *"git commit"*)` fires
# on ANY command that merely CONTAINS that text -- a `printf` probe whose
# payload happened to contain the string "git commit" was refused before it
# ever ran, and conversely `git -c core.pager=cat commit` (a global option
# between "git" and "commit") never contains the literal substring "git
# commit" at all and was never even inspected. Replaced with the same
# argv-aware classifier enforce-review-verdict.sh uses: `\bgit\s+(?:
# -[^\s]+\s+)*commit\b`/`push`, which requires "git" and the subcommand to
# actually be adjacent tokens (global flags between them tolerated, plain
# text merely containing the words is not).
#
# 🔴-3 (PR #358 review round 1): for `push`, the local CURRENT branch alone
# is blind to the REFSPEC -- `git push origin HEAD:main` from a feature
# branch used to exit 0 unconditionally, because nothing here ever looked at
# the command's own destination argument. push_destinations() parses the
# actual destination(s) at the argv level; the current-branch check below
# stays as the fallback ONLY for the no-refspec form (`git push`, `git push
# origin`), where git's own default push behaviour targets the current
# branch.
#
# push_destinations() used to be reimplemented here byte-identically to
# enforce-review-verdict.sh's own inline copy, justified by a claim that
# these two hooks had "no established sourcing convention between them" --
# false even at the time (four bash hooks already `source
# lib/git-commit-detect.sh`), and false again the moment
# lib/path_containment.py landed in this same changeset for the blindness
# hooks. Both push gates now import the same functions from
# lib/git_commit_detect.py instead (PR #358 review round 2, 🔴-A).
parsed="$(printf '%s' "$payload" | python3 -c '
import json, os, re, sys

sys.path.insert(0, os.environ["HOOKS_LIB_DIR"])
from git_commit_detect import push_destinations

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)
if (d.get("tool_name") or "").strip() != "Bash":
    sys.exit(0)
cmd = (d.get("tool_input") or {}).get("command") or ""
if not cmd:
    sys.exit(0)

kind = ""
if re.search(r"\bgit\s+(?:-[^\s]+\s+)*push\b", cmd):
    kind = "push"
elif re.search(r"\bgit\s+(?:-[^\s]+\s+)*commit\b", cmd):
    kind = "commit"
if not kind:
    sys.exit(0)

push_kind, push_dests = ("", [])
if kind == "push":
    push_kind, push_dests = push_destinations(cmd)

print("%s\t%s\t%s" % (kind, push_kind, ",".join(push_dests)))
' 2>/dev/null)"
py_status=$?

if [ "$py_status" -ne 0 ]; then
  echo "🚫 block-direct-commits-to-main.sh: the command classifier exited non-zero (${py_status}) -- refusing rather than treating a parser failure as \"nothing to gate\"." >&2
  exit 2
fi

[ -z "$parsed" ] && exit 0
IFS=$'\t' read -r KIND PUSH_KIND PUSH_DESTS <<<"$parsed"

repo="$(printf '%s' "$payload" | python3 -c 'import json,sys
try:
    print(json.loads(sys.stdin.read()).get("cwd") or "")
except Exception:
    print("")
' 2>/dev/null || true)"
[ -d "$repo" ] || repo="$PWD"

# Discover the current branch. If we're not in a git repo, do nothing.
if ! current_branch="$(git -C "$repo" rev-parse --abbrev-ref HEAD 2>/dev/null)"; then
  exit 0
fi

destination=""
if [ "$KIND" = "push" ]; then
  case "$PUSH_KIND" in
    ALL)
      destination="__ALL__"
      ;;
    DESTS)
      IFS=',' read -ra __bd_dests <<< "$PUSH_DESTS"
      for __d in "${__bd_dests[@]}"; do
        case "$__d" in
          main|master) destination="$__d"; break ;;
        esac
      done
      [ -z "$destination" ] && exit 0
      ;;
    NONE)
      case "$current_branch" in
        main|master) destination="$current_branch" ;;
        *) exit 0 ;;
      esac
      ;;
    *)
      # UNKNOWN, or the push classifier never ran -- the destination could
      # not be confidently determined. Fail closed rather than exit 0.
      destination="__UNKNOWN__"
      ;;
  esac
else
  case "$current_branch" in
    main|master) destination="$current_branch" ;;
    *) exit 0 ;;
  esac
fi

label="${destination}"
case "$destination" in
  __ALL__)   label="a branch this push could reach (--all/--mirror)" ;;
  __UNKNOWN__) label="a branch this push's destination could not be confidently determined" ;;
esac

cat >&2 <<EOF
🚫 Direct commits/pushes to '${label}' are forbidden.

The squad's git workflow requires all changes to go through feature branches and pull requests:

  1. Create a feature branch:
       git checkout -b <type>/<issue-number>-<short-slug>
     where <type> is one of: feature, fix, docs, refactor, test, perf, security, ci, build, chore

  2. Commit your work on the feature branch.

  3. Push the feature branch and open a pull request to 'main'.

See .claude/docs/decisions.md (Git Workflow) for the full rule.
EOF
exit 2
