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
# `reviewer-blind` is live: `agents/reviewer.md` has been split into
# `reviewer-blind` and `reviewer-reconcile`, and this hook fires on every
# `Bash` call reviewer-blind makes — it blocked reviewer-blind's own `git
# log` probe during round 1's review of this very changeset.
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

# Fail closed, not open, when the interpreter itself is unavailable. The
# python3 call below is wrapped in a top-level try/except that refuses on a
# malformed payload -- but an interpreter that is missing, the wrong
# version, or broken by a bad edit never reaches that except at all, and the
# previous `2>/dev/null || true` on the substitution turned that failure into
# an EMPTY $reason, indistinguishable from "nothing to report" (allow).
if ! command -v python3 >/dev/null 2>&1; then
  cat >&2 <<'EOF'
🚫 python3 is not available on PATH.

This hook enforces reviewer-blind's git-history channel using an embedded
Python command matcher. Without python3 there is no way to evaluate the
command, so refusing is the only choice that does not silently defeat the
rule this hook exists to enforce.

Install python3 (or add it to PATH) and retry.
EOF
  exit 2
fi

payload="$(cat)"

# `set -e` alone does not get this to exit 2: under `-euo pipefail`, a
# failing substitution aborts the SCRIPT immediately with WHATEVER exit
# code the failing command returned (127 for "command not found" inside a
# broken interpreter stub, for instance) -- and Claude Code only treats
# exit 2 from a PreToolUse hook as "block"; any other nonzero code is a
# non-blocking error that lets the tool call proceed, which is fail-OPEN.
# `-e` is suspended for exactly this one substitution so the exit code can
# be inspected and converted to a real, deliberate `exit 2` below, instead
# of leaking whatever raw code the interpreter happened to return.
set +e
reason="$(printf '%s' "$payload" | python3 -c '
import json, re, sys

AGENT = "reviewer-blind"

# (regex, what it would leak)
#
# The repeated group matches a run of global options before the subcommand
# word, one option at a time: a dash-prefixed token (`-c`, `-C`, `--git-dir`,
# ...), optionally followed by a SEPARATE, non-dash-prefixed value token
# (`-c core.pager=cat`, `-C /path`, `--git-dir /path`) -- not just an
# inline `--opt=value` on one token, which `-[^\s]+` alone already covers.
# Without the optional separate-value branch, `git -c core.pager=cat log`
# does not match at all: `-c` consumes the flag, but the next token,
# `core.pager=cat`, does not start with `-`, so the old pattern required
# `log` to appear immediately after `-c` and never found it. The value
# branch is optional and backtracks: `git --no-pager log` still matches
# with `log` recognised as the subcommand, not swallowed as the value of
# `--no-pager`, because the engine backs off the optional value when
# consuming it would leave no literal `log` to match afterwards.
_OPT = r"-[^\s]+(?:\s+(?!-)[^\s]+)?"
DENIED = [
    (r"\bgit\s+(?:%s\s+)*log\b" % _OPT,   "git log"),
    (r"\bgit\s+(?:%s\s+)*show\b" % _OPT,  "git show"),
    (r"\bgit\s+(?:%s\s+)*blame\b" % _OPT, "git blame"),
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
' 2>/dev/null)"
py_rc=$?
set -e

if [ "$py_rc" -ne 0 ]; then
  cat >&2 <<EOF
🚫 this hook's embedded Python command matcher exited with an unexpected
error (exit $py_rc) instead of a clean allow or a reported denial.

This hook enforces reviewer-blind's git-history channel; an internal crash
is not the same thing as "nothing to report" and must not be treated as an
allow. Refusing is the only choice that does not silently defeat the rule
this hook exists to enforce.

Check python3's version and the hook's own syntax -- this is a bug in the
hook, not in the command it was evaluating.
EOF
  exit 2
fi

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
