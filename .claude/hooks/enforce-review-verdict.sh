#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool. The commit gate.
#
# Refuses, unless .squad/.last-review-verdict records a PASS for the CURRENT
# HEAD:
#
#   git commit        when the staged/committed change touches code paths
#   git push          to a protected branch (main, master, release/*)
#   gh pr merge       always
#
# Rationale. "Never declare your own work complete" was an instruction, and
# the Missing Review Lockout enforced it socially. Self-approval is the
# failure mode that quietly destroys review as an institution, and every
# instance looks reasonable in isolation: small change, reviewer busy,
# obviously works. The lockout made skipping review visible. This makes it
# fail.
#
# The verdict must name the current HEAD. A bare PASS with no commit would
# approve every future change too, which is precisely the failure this exists
# to stop, so `commit: <sha>` on the second line is required and must match.
# .squad/.last-review-verdict is written by reviewer-reconcile and by
# scribe-decision-merger.sh.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR reads the wrong repository's verdict from inside a
# worktree — and still passes any test that has no worktree in it.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Bash",
#     "tool_input": { "command": "..." }
#   }

set -uo pipefail

payload="$(cat)"

parsed="$(printf '%s' "$payload" | python3 -c '
import json, re, sys
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
if re.search(r"\bgh\s+pr\s+merge\b", cmd):
    kind = "merge"
elif re.search(r"\bgit\s+(?:-[^\s]+\s+)*push\b", cmd):
    kind = "push"
elif re.search(r"\bgit\s+(?:-[^\s]+\s+)*commit\b", cmd):
    kind = "commit"
if not kind:
    sys.exit(0)
print("%s\t%s" % (kind, (d.get("cwd") or "")))
' 2>/dev/null || true)"

[ -z "$parsed" ] && exit 0
IFS=$'\t' read -r KIND PAYLOAD_CWD <<<"$parsed"

repo="${PAYLOAD_CWD:-$PWD}"
[ -d "$repo" ] || repo="$PWD"

git -C "$repo" rev-parse --is-inside-work-tree >/dev/null 2>&1 || exit 0

branch="$(git -C "$repo" rev-parse --abbrev-ref HEAD 2>/dev/null || echo "")"

# --- push: only protected branches are gated -------------------------------
if [ "$KIND" = "push" ]; then
  case "$branch" in
    main|master|release/*) ;;
    *) exit 0 ;;
  esac
fi

# --- commit: only when code-shaped paths are staged ------------------------
# A docs-only or decision-drop commit is not what this gate is for, and a gate
# that fires on every commit is one somebody disables.
if [ "$KIND" = "commit" ]; then
  staged="$(git -C "$repo" diff --cached --name-only 2>/dev/null || true)"
  [ -z "$staged" ] && exit 0
  code=0
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    case "$f" in
      *.cs|*.csproj|*.js|*.mjs|*.ts|*.tsx|*.jsx|*.py|*.sh|\
      Dockerfile|*/Dockerfile|*.dockerfile|\
      .github/workflows/*|*/Migrations/*|\
      package.json|*/package.json|\
      Directory.Build.props|Directory.Packages.props|\
      appsettings*.json|*/appsettings*.json)
        code=1; break ;;
    esac
  done <<< "$staged"
  [ "$code" -eq 0 ] && exit 0
fi

head_sha="$(git -C "$repo" rev-parse HEAD 2>/dev/null || echo "")"
cache="$repo/.squad/.last-review-verdict"

verdict=""
verdict_sha=""
if [ -f "$cache" ]; then
  verdict="$(head -n1 "$cache" | tr -d '[:space:]')"
  verdict_sha="$(grep -aiE '^commit:' "$cache" 2>/dev/null | head -n1 | sed -E 's/^[Cc]ommit:[[:space:]]*//' | tr -d '[:space:]')"
fi

# For a commit, the verdict necessarily names the commit BEFORE this one --
# the change being committed does not have a sha yet. For push and merge, it
# must name the current HEAD.
want_sha="$head_sha"
what="the current HEAD"
if [ "$KIND" = "commit" ]; then
  what="HEAD (the commit this one builds on)"
fi

problem=""
if [ ! -f "$cache" ]; then
  problem="no verdict has been recorded at all (.squad/.last-review-verdict does not exist)"
elif [ "$verdict" != "PASS" ]; then
  problem="the recorded verdict is ${verdict:-<empty>}, not PASS"
elif [ -z "$verdict_sha" ]; then
  problem="the verdict records no commit, so it cannot be said to apply to this one"
elif [ -n "$want_sha" ] && [ "$verdict_sha" != "$want_sha" ]; then
  problem="the PASS was recorded for ${verdict_sha:0:12}, but ${what} is ${want_sha:0:12}"
fi

[ -z "$problem" ] && exit 0

case "$KIND" in
  commit) action="Committing code" ;;
  push)   action="Pushing to '${branch}'" ;;
  merge)  action="Merging a pull request" ;;
esac

cat >&2 <<EOF
🚫 ${action} requires a PASS verdict for ${what}.

  Problem: ${problem}

Run the review pair and let it finish:

  1. reviewer-blind      → .squad/design/<slug>/08-review-blind.md
  2. reviewer-reconcile  → 09-review-verdict.md, the decision drop,
                           and .squad/.last-review-verdict

Or use /review, which dispatches both in order.

"Never declare your own work complete" used to be an instruction, and the
Missing Review Lockout enforced it socially. Self-approval is the failure mode
that quietly destroys review as an institution, and every instance looks
reasonable in isolation — small change, reviewer busy, obviously works. That is
exactly why this is a gate and not a reminder.

If the user has explicitly overridden the review, they can record that
themselves; you cannot record it on their behalf. Writing a PASS into
.squad/.last-review-verdict to get past this message is forging a verdict, and
it is the one thing this hook exists to make impossible to do quietly.

See .claude/docs/principles-enforcement.md (Missing Review Lockout).
EOF
exit 2
