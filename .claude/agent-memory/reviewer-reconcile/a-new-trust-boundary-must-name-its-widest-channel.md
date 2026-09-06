---
name: a-new-trust-boundary-must-name-its-widest-channel
description: When a round creates a threat-model trust boundary, enumerate the tools it excludes and the tool grants of the agents it governs — the excluded channel is usually the real one
metadata:
  type: feedback
---

A newly written trust boundary states the tool set it covers. That sentence is
the finding: compare it against the `tools:` frontmatter of every agent the
boundary governs, and against how the *sibling* boundary in the same file handled
the same question.

**Why:** PR #358 round 2 asked for a real threat-model row behind the dangling
`T-012` citations. Round 3 got Trust Boundary 6, "Dreamer/reviewer blindness
boundary", scoped in its own words to `Grep`/`Glob`/`Read`/`NotebookRead` —
matching `enforce-review-blindness.sh:250`, which exits 0 for every other tool.
But `reviewer-blind`'s grant is `Read, Grep, Glob, Bash, Write`. Firing **every**
hook in `.claude/hooks/` at a `reviewer-blind` `Bash` payload of
`cat .squad/design/<slug>/04-realist-plan.md` produced exit 0 from all of them,
`enforce-review-history-channel.sh` included — it gates `git log`/`show`/`blame`
and `gh pr view`, not `cat`. `dreamer-first-principles` has no `Bash`, so *its*
blindness really is structural; the two governed agents are not equivalent and
the boundary treats them as if they were.

The tell was one boundary up: Trust Boundary 5, written by the same specialist in
the same tree, discloses exactly this class for itself — TM-012, *"it mediates
none of those tools' equivalent effect via `Bash`."* When the disclosure pattern
exists and was applied next door and not here, that is evidence, not style.

**How to apply:**

- For each governed agent, diff the boundary's tool list against the agent's
  `tools:` line. A tool in the grant and not in the boundary is a finding.
- Prove it by firing every hook, not the two you expect to matter — an exhaustive
  loop over `.claude/hooks/*.sh` is four lines and removes the "surely something
  else catches it" objection.
- Check the governed agent's charter for an overclaim in the same breath:
  `reviewer-blind.md` says *"Your independence is structural instead"* and titles
  its residual section "the One Thing That Remains a Promise". For a
  `Bash`-holding agent there are two.
- Grade it a **residual**, not a regression, when the channel predates the
  changeset — blocking only until registered.

Related: [[the-round-cap-is-a-verdict-shape]],
[[command-text-matching-is-not-a-gate]], [[last-review-verdict-is-forgeable]].
