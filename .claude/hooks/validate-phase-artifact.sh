#!/usr/bin/env bash
#
# SubagentStop hook, matching the design-pass phase agents.
#
# Checks each phase artifact against its required shape and BLOCKS when it is
# malformed, so a defective artifact stops here rather than flowing downstream:
#
#   01-track-a.md        must carry a Reasoning Trail
#   02-track-b.md        must carry Known Failure Modes
#   05-critic.md         every finding must carry a concrete failure scenario
#   06-spec.md           must reference every INV-n declared in 00-scope.md,
#                        and must carry a property layer
#
# Rationale. Decision drops have been schema-validated since the beginning;
# design artifacts carry more weight and had no validation at all. A Track A
# artifact missing its reasoning trail destroys the only thing convergence
# exists to do — telling genuine novelty from a missed constraint — and
# nothing caught it. These are shape checks, not quality checks: they cannot
# tell a good reasoning trail from a bad one, only a present one from an
# absent one. That is still the difference between a phase that ran and a
# phase that returned.
#
# The matchers are declared in settings.json AND the agent is re-checked here
# against `agent_type` from the payload, which Claude Code populates when a
# hook fires inside a subagent.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR silently stops matching inside a worktree — and still
# passes any test that has no worktree in it.
#
# Exit codes:
#   0 — artifact well-formed, or not this hook's business
#   2 — malformed (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "dreamer-first-principles",
#     "cwd": "/path/to/checkout-or-worktree",
#     "stop_hook_active": false
#   }

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

reason="$(printf '%s' "$payload" | python3 -c '
import json, os, re, sys

# agent_type -> (artifact, [(label, test, remedy)])
AGENTS = {
    "dreamer-first-principles": "01-track-a.md",
    "dreamer-informed":         "02-track-b.md",
    "critic":                   "05-critic.md",
    "spec-author":              "06-spec.md",
}

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)

if d.get("stop_hook_active"):
    sys.exit(0)

agent = (d.get("agent_type") or "").strip()
artifact = AGENTS.get(agent)
if artifact is None:
    sys.exit(0)

root = d.get("cwd") or os.getcwd()
design = os.path.join(root, ".squad", "design")
if not os.path.isdir(design):
    sys.exit(0)

found = []
for slug in os.listdir(design):
    p = os.path.join(design, slug, artifact)
    if os.path.isfile(p):
        found.append((os.path.getmtime(p), slug, p))
if not found:
    sys.exit(0)
found.sort()
_, slug, path = found[-1]
pass_dir = os.path.dirname(path)
text = open(path, encoding="utf-8", errors="replace").read()
low = text.lower()

problems = []


def has_heading(t, *names):
    for n in names:
        if re.search(r"^\s{0,3}#{1,6}\s*[^\n]*" + re.escape(n.lower()), t, re.M):
            return True
    return False


if artifact == "01-track-a.md":
    if not has_heading(low, "reasoning trail"):
        problems.append(
            "No **Reasoning Trail** section.\n"
            "      dreamer-convergence reads the derivation to tell `Track A found\n"
            "      something the literature never would` from `Track A reasoned past a\n"
            "      constraint experience would have caught`. From the outside those look\n"
            "      identical, and the derivation is the only evidence separating them.\n"
            "      Without it the artifact cannot do the one job it exists for.")
    elif len(re.sub(r"\s+", " ", low.split("reasoning trail", 1)[1])) < 200:
        problems.append(
            "The **Reasoning Trail** section is present but almost empty.\n"
            "      It is not a heading to satisfy a checker. Write the derivation:\n"
            "      how you got from the constraints to the candidates.")
    if not re.search(r"candidate\s*a\s*2", low):
        problems.append(
            "Fewer than two candidates. The method asks for at least two derived\n"
            "      candidates (`Candidate A1`, `Candidate A2`); one candidate is a\n"
            "      conclusion, not an exploration.")

elif artifact == "02-track-b.md":
    if not has_heading(low, "known failure modes", "failure modes"):
        problems.append(
            "No **Known Failure Modes** section.\n"
            "      Track B stands on shoulders, and the most valuable thing prior art\n"
            "      carries is what the people who tried it first regretted. The Critic\n"
            "      reads this section specifically — the failures somebody already hit\n"
            "      are the cheapest ones to avoid.")
    if not re.search(r"https?://|\[[^\]]+\]\([^)]+\)", text):
        problems.append(
            "No citations anywhere in the artifact.\n"
            "      A candidate with no evidence behind it is a Track A candidate wearing\n"
            "      a costume — and it will be ranked as if it were evidenced.")

elif artifact == "05-critic.md":
    # Each 🔴/🟠 finding needs a concrete failure scenario. "This might not
    # scale" is not a finding.
    findings = re.findall(r"^#{2,5}\s*(?:🔴|🟠)?[^\n]*\n(.*?)(?=^#{2,5}\s|\Z)",
                          text, re.M | re.S)
    blocker_section = ""
    m = re.search(r"^#{2,4}\s*🔴[^\n]*\n(.*?)(?=^#{2,4}\s*(?:🟠|🟡|🟢)|\Z)", text, re.M | re.S)
    if m:
        blocker_section = m.group(1)
    if blocker_section.strip() and not re.search(
            r"failure scenario|given\b|when\b.*then\b|reproduc", blocker_section, re.I | re.S):
        problems.append(
            "Blockers are listed with no concrete failure scenario.\n"
            "      Every finding needs specific inputs or state leading to a specific\n"
            "      wrong outcome. `This might not scale` is not a finding — it is a\n"
            "      worry, and a plan cannot be changed in response to one.")
    if not re.search(r"verdict", low):
        problems.append("No **Verdict** — the phase closes on APPROVE or LOOP BACK.")

elif artifact == "06-spec.md":
    scope_path = os.path.join(pass_dir, "00-scope.md")
    declared = []
    if os.path.isfile(scope_path):
        scope = open(scope_path, encoding="utf-8", errors="replace").read()
        declared = sorted(set(re.findall(r"\bINV-(\d+)\b", scope)), key=int)
    # No trailing \b: a test named INV_2_a_published_article... has a word
    # character after the digit, so \b would never match the very naming
    # convention the spec asks for.
    covered = set(re.findall(r"\bINV[-_](\d+)(?![0-9])", text))
    missing = [i for i in declared if i not in covered]
    if missing:
        problems.append(
            "Invariants declared in 00-scope.md with no reference in the spec: %s\n"
            "      An invariant is a claim over the whole input space. One property per\n"
            "      INV-n, named so the id is visible. If an invariant cannot be expressed\n"
            "      as a property, say so and send it back to the architect — that is the\n"
            "      chain working. A weak property that satisfies this check is the chain\n"
            "      broken silently."
            % ", ".join("INV-" + i for i in missing))
    # A heading, not the word anywhere: prose such as "no property layer yet"
    # contains the word and would otherwise satisfy the check.
    if declared and not has_heading(low, "propert", "invariant layer"):
        problems.append(
            "00-scope.md declares invariants but the spec has no property layer.\n"
            "      The acceptance layer is example-based because its job is recognition.\n"
            "      The invariant layer is property-based because three examples prove\n"
            "      almost nothing about a claim over every input. Both, or neither is\n"
            "      doing its job.")

if problems:
    print("%s|%s|%s" % (agent, os.path.relpath(path, root), slug))
    for p in problems:
        print("  - " + p)
' 2>/dev/null || true)"

[ -z "$reason" ] && exit 0

head_line="$(printf '%s' "$reason" | head -n1)"
IFS='|' read -r agent artifact slug <<<"$head_line"
body="$(printf '%s' "$reason" | tail -n +2)"

cat >&2 <<EOF
🚫 ${artifact} is malformed and cannot go downstream.

  Phase: ${agent}
  Pass:  ${slug}

${body}

This is a shape check, not a quality check: it cannot tell a good artifact from
a bad one, only a complete one from an incomplete one. The next phase reads this
file and has none of your context — a missing section is not a formatting nit,
it is the next phase being asked to do its job without its input.

Fix the artifact and finish again.

See .claude/skills/beast-mode-design/SKILL.md and .claude/agents/${agent}.md.
EOF
exit 2
