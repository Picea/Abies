# Pass cost

Wall-clock per design phase, keyed by slug. Appended by
`.claude/hooks/session-logger.sh` on every `SubagentStop` from a phase agent.

Read by `dreamer-convergence`, whose memory is the only place that answers
whether the dual track earns its cost. The novelty hit rate is a numerator;
this is the denominator. **If the novelty hit rate stays at zero, the
charter requires convergence to say so plainly** — a topology that cannot
report its own uselessness will never be dismantled.

A blank duration means the transcript carried no usable timestamps. The row
still records that the phase ran.

| when | slug | phase | wall-clock |
|---|---|---|---|
| 2026-09-06T11:33:57Z | chore-0-squad-flow-v2-1 | `reviewer-blind` | 15m 09s |
| 2026-09-06T12:47:05Z | chore-0-squad-flow-v2-1 | `reviewer-reconcile` | 79m 06s |
| 2026-09-06T13:34:34Z | chore-0-squad-flow-v2-1 | `reviewer-reconcile` | 127m 31s |
| 2026-09-06T13:45:42Z | chore-0-squad-flow-v2-1 | `reviewer-reconcile` | 147m 21s |
