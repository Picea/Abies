#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
#
# `git log` carries two things through one pipe: which files changed when and
# whether something was reverted — code context the blind reviewer needs — and
# what the author wrote about it, which is narrative. They separate. This hook
# refuses the raw commands for `reviewer-blind` and points at the sanctioned
# channel, .claude/hooks/git-history-namestatus.sh, which emits name-status and
# dates and nothing else. Commit messages never reach the blind context.
#
# Denied for `reviewer-blind`:  git log, git show, git blame,
#                               gh pr view, gh issue view
#
# `reviewer-blind` is introduced in a later stage of this work (WP-5, which
# splits agents/reviewer.md into reviewer-blind and reviewer-reconcile).
# This hook is written now, keyed by name, and is inert until that charter
# lands.
#
# Identity comes from `agent_type` in the payload, which Claude Code
# populates when a hook fires inside a subagent. Any other agent — including
# reviewer-reconcile, which is supposed to read the narrative — passes
# through untouched.
#
# This hook matches on the command text rather than on a path, so it has no
# ${CLAUDE_PROJECT_DIR} dependency to get wrong. The sanctioned script it
# names does resolve against the payload `cwd`.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "reviewer-blind",
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Bash",
#     "tool_input": { "command": "...", "description": "..." }
#   }

set -euo pipefail

payload="$(cat)"

reason="$(printf '%s' "$payload" | python3 -c '
import json, re, sys

AGENT = "reviewer-blind"

# (regex, what it would leak)
DENIED = [
    (r"\bgit\s+(?:-[^\s]+\s+)*log\b",   "git log"),
    (r"\bgit\s+(?:-[^\s]+\s+)*show\b",  "git show"),
    (r"\bgit\s+(?:-[^\s]+\s+)*blame\b", "git blame"),
    (r"\bgh\s+pr\s+view\b",             "gh pr view"),
    (r"\bgh\s+issue\s+view\b",          "gh issue view"),
]

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)

if (d.get("agent_type") or "").strip() != AGENT:
    sys.exit(0)
if (d.get("tool_name") or "").strip() != "Bash":
    sys.exit(0)

cmd = (d.get("tool_input") or {}).get("command") or ""
if not cmd:
    sys.exit(0)

for rx, label in DENIED:
    if re.search(rx, cmd):
        print(label)
        break
' 2>/dev/null || true)"

[ -z "$reason" ] && exit 0

cat >&2 <<EOF
🚫 reviewer-blind may not run \`${reason}\`.

That command carries the author's narrative — commit messages, PR bodies, issue
threads — into a context whose independence depends on not having it. The code
context it also carries is real and you are entitled to it, so it is available
through the sanctioned channel instead:

  bash .claude/hooks/git-history-namestatus.sh [<rev-range>] [-- <path>...]

That emits commit hashes, dates and --name-status only. No subjects, no bodies,
no trailers. It answers "which files changed when, and was anything reverted",
which is the part of the history a code reviewer needs.

reviewer-reconcile reads the narrative — that is its job, and it treats every
part of it as a claim to verify against your 08-review-blind.md.

See .claude/agents/reviewer-blind.md.
EOF
exit 2
