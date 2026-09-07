---
name: pass-cost-instrument-reads-cumulative
description: .squad/log/pass-cost.md records cumulative session elapsed time, not per-phase duration — read deltas between consecutive rows, not the numbers themselves.
metadata:
  type: project
---

`.squad/log/pass-cost.md` is meant to give the denominator for the novelty hit rate. As of
2026-09-06 the numbers in the `wall-clock` column are **cumulative elapsed time since the
session started**, not the duration of the phase named in the row.

**Why:** the values increase monotonically across sequential phases and across a re-run of
an earlier phase, and reach 240m for a pass whose phases plainly did not each take hours.
`session-logger.sh` appends on `SubagentStop`, and what it is capturing looks like the
offset from session start rather than the span of the subagent.

**How to apply:** take **deltas between consecutive rows for the same slug**, not the raw
figure. For `undo-redo`: the parallel Dreamer step ran from 230m21s (warden returned) to
240m07s (Track A returned), so the *pair* cost ≈ 9m 46s wall-clock, and Track A added
≈ 8m 44s beyond Track B's return. That is the real marginal cost of running the dual track
for that pass — and it is cheap enough that the topology is easy to justify if the novelty
claims survive.

Deltas are only meaningful when the rows are consecutive and the phases were genuinely
sequential. For the two Dreamer tracks, which are dispatched in one message and run
concurrently, the later row is the pair's completion, not the sum of two costs. Do not add
them.

Worth raising with the user if it recurs — this is the instrument that decides whether the
dual track gets dismantled, and right now it measures something else. Not mine to fix
(`session-logger.sh` is a hook, and hooks are load-bearing framework files).

See [[calibration-dual-track]].
