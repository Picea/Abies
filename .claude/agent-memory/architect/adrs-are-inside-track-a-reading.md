---
name: adrs-are-inside-track-a-reading
description: docs/adr/ is codebase as far as enforce-track-blindness.sh is concerned, so ADRs can hand Track A a recorded conclusion the deny list would otherwise withhold
metadata:
  type: project
---

`enforce-track-blindness.sh` denies `dreamer-first-principles` the decision
register, the archive, the tech-stack record, the pattern lexicon, agent
memories, decision drops, `00-knowledge.md`, `00-warden*.md` and `02-track-b.md`.
It does **not** deny `docs/adr/**`. ADRs are read as codebase.

**Why:** ADRs in this repo are not neutral descriptions of what the code does —
they carry Consequences sections that record conclusions. Found 2026-09-06 while
opening the `undo-redo` pass: `docs/adr/ADR-008-immutable-state.md` states
*"Undo/redo: Trivial to implement by storing state snapshots"*, which is an
answer to that pass's central open question, sitting in a file Track A may read
freely. The deny list cannot see it because it is scoped by path, and ADRs live
with the code.

**How to apply:** When opening a deep pass, grep `docs/adr/` for the pass's
subject before writing `00-scope.md`, and report any hit to gate 1 alongside the
scope. Do not cite an ADR from `00-scope.md` when its Consequences section
contains a steer on an open question — citing it is a direct pointer even though
reading it is permitted. Whether to deny a specific ADR for a specific pass is
the user's call at gate 1, not the architect's. Related:
[[runtime-seams-anchor-replay-gating]].

**Outcome of the one experiment run so far (undo-redo, 2026-09-06/07):** the user
left ADR-008:85 reachable *deliberately*, to see whether Track A would copy it or
reason past it, and told convergence to classify the result. Verdict: reasoned
past — it contradicted the line's central adjective, declined its vocabulary,
reached the same storage answer as the survivor of three recorded rejections, and
produced a cost model the ADR does not contain. So a reachable steer is not
automatically fatal, and leaving one reachable *on purpose, with the
classification assigned in advance* is a legitimate option to offer at gate 1
alongside denial. The path gap itself is registered as **R-21** in
`.claude/enforcement/refutations.md`, owned by `devops`, closing after that pass —
check whether it closed before relying on this memory's premise.
See [[undo-redo-withhistory-decision]].
