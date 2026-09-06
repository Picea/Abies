#!/usr/bin/env bash
#
# PreToolUse hook for Write.
#
# Refuses a design-pass artifact whose predecessors are absent, which makes
# phase ordering an invariant rather than good conduct by the orchestrator:
#
#   03-convergence.md    requires 01-track-a.md AND 02-track-b.md
#   05-critic.md         requires 04-realist-plan.md
#   09-review-verdict.md requires 08-review-blind.md
#
# Each of the three protects a different failure. Convergence over one track is
# not convergence, and the agent that would notice is the one being skipped.
# A critique of a plan that does not exist is a critique of something else.
# And a verdict without a blind reading behind it is the capture the split
# reviewer exists to prevent — the second reviewer has read the narrative, so
# it cannot retroactively supply the independent assessment.
#
# Skipped phases are honoured. If 00-scope.md records that Track A was skipped
# — the conductor writes "⏩ Skipping first-principles track" — 03 is allowed
# without 01. There is no such escape for 02, for 04 or for 08: the Critic is
# never skipped, and neither is the blind review.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR silently stops matching inside a worktree — and still
# passes any test that has no worktree in it.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Write",
#     "tool_input": { "file_path": "..." }
#   }

set -euo pipefail

# 🔴-4 (PR #358 review round 1): a missing/broken python3 used to produce
# empty stdout via `|| true`, indistinguishable from "this artifact has no
# phase-order rule" -- silently allowing a Write this hook exists to refuse.
# Checked once, here, before anything else runs.
command -v python3 >/dev/null 2>&1 || {
  echo "🚫 enforce-phase-order.sh: python3 is required to evaluate phase order and is not on PATH -- refusing rather than silently allowing an artifact whose predecessors cannot be checked." >&2
  exit 2
}

payload="$(cat)"

set +e
reason="$(printf '%s' "$payload" | python3 -c '
import json, os, re, sys

# artifact -> (required predecessors, optional-if-skipped)
REQUIRES = {
    "03-convergence.md":    (["01-track-a.md", "02-track-b.md"], {"01-track-a.md": "first-principles"}),
    "05-critic.md":         (["04-realist-plan.md"], {}),
    "09-review-verdict.md": (["08-review-blind.md"], {}),
}

WHY = {
    "03-convergence.md":
        "Convergence over one track is not convergence. Its whole job is to tell "
        "a genuine divergence from a missed constraint, and it needs both "
        "artifacts to do it.",
    "05-critic.md":
        "The Critic stress-tests a plan. Without 04-realist-plan.md there is no "
        "plan to stress-test, only a direction.",
    "09-review-verdict.md":
        "The verdict depends on an independent reading formed before the "
        "narrative was seen. reviewer-reconcile has already read the narrative, "
        "so it cannot supply that reading retroactively — that is exactly the "
        "capture the split exists to prevent. Dispatch reviewer-blind first.",
}

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)

if (d.get("tool_name") or "").strip() != "Write":
    sys.exit(0)

cwd = d.get("cwd") or os.getcwd()
target = (d.get("tool_input") or {}).get("file_path")
if not isinstance(target, str) or not target:
    sys.exit(0)

path = os.path.normpath(os.path.join(cwd, os.path.expanduser(target)))
base = os.path.basename(path)
rule = REQUIRES.get(base)
if rule is None:
    sys.exit(0)

pass_dir = os.path.dirname(path)
# Only govern artifacts written inside a .squad/design/<slug>/ directory.
if os.path.basename(os.path.dirname(pass_dir)) != "design":
    sys.exit(0)

needed, skippable = rule

scope = ""
scope_path = os.path.join(pass_dir, "00-scope.md")
if os.path.isfile(scope_path):
    scope = open(scope_path, encoding="utf-8", errors="replace").read().lower()

missing = []
for pred in needed:
    if os.path.isfile(os.path.join(pass_dir, pred)):
        continue
    token = skippable.get(pred)
    if token and re.search(r"skip\w*\s+[^\n]{0,40}" + re.escape(token), scope):
        continue
    missing.append(pred)

if missing:
    print("%s|%s|%s|%s" % (base, os.path.relpath(pass_dir, cwd),
                           ", ".join(missing), WHY[base]))
' 2>/dev/null)"
phase_py_status=$?
set -e

if [ "$phase_py_status" -ne 0 ]; then
  echo "🚫 enforce-phase-order.sh: the phase-order classifier exited non-zero (${phase_py_status}) -- refusing rather than treating a parser failure as \"no predecessor rule applies\"." >&2
  exit 2
fi

[ -z "$reason" ] && exit 0

IFS='|' read -r artifact pass_dir missing why <<<"$reason"

cat >&2 <<EOF
🚫 ${artifact} cannot be written yet — its predecessor is missing.

  Pass:    ${pass_dir}
  Missing: ${missing}

${why}

Phase order is an invariant here, not a convention the orchestrator is trusted
to remember. Run the missing phase, then write this artifact.

If a phase was deliberately skipped, that belongs in 00-scope.md as an explicit
line — *"⏩ Skipping first-principles track — [reason]"* — written by the
architect when it planned the pass, not decided now by whoever hit this message.
The Critic and the blind review are never skippable.

See CLAUDE.md § 3 and .claude/skills/beast-mode-design/SKILL.md.
EOF
exit 2
