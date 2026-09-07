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
| 2026-09-06T13:51:40Z | chore-0-squad-flow-v2-1 | `reviewer-reconcile` | 157m 59s |
| 2026-09-06T14:49:45Z | undo-redo | `architect` | 210m 17s |
| 2026-09-06T14:51:01Z | undo-redo | `scope-warden` | 216m 53s |
| 2026-09-06T15:02:59Z | undo-redo | `architect` | 229m 37s |
| 2026-09-06T15:04:21Z | undo-redo | `scope-warden` | 230m 21s |
| 2026-09-06T15:13:15Z | undo-redo | `dreamer-informed` | 231m 23s |
| 2026-09-06T15:13:54Z | undo-redo | `dreamer-first-principles` | 240m 07s |
| 2026-09-06T15:22:19Z | undo-redo | `dreamer-convergence` | 241m 03s |
| 2026-09-06T17:51:24Z | undo-redo | `dreamer-convergence` | 249m 21s |
| 2026-09-06T18:23:37Z | undo-redo | `architect` | 427m 31s |
| 2026-09-06T18:24:20Z | undo-redo | `architect` | 430m 36s |
| 2026-09-06T18:38:47Z | undo-redo | `realist` | 431m 31s |
| 2026-09-06T19:17:52Z | undo-redo | `architect` | 484m 09s |
| 2026-09-06T19:30:48Z | undo-redo | `critic` | 484m 57s |
| 2026-09-06T21:18:18Z | undo-redo | `realist` | 590m 27s |
| 2026-09-06T21:20:01Z | undo-redo | `architect` | 606m 04s |
| 2026-09-06T21:20:30Z | undo-redo | `architect` | 606m 59s |
| 2026-09-06T21:35:32Z | undo-redo | `critic` | 607m 33s |
| 2026-09-07T06:00:40Z | undo-redo | `realist` | 1109m 58s |
| 2026-09-07T06:19:37Z | undo-redo | `architect` | 1145m 12s |
| 2026-09-07T06:35:08Z | undo-redo | `critic` | 1146m 43s |
| 2026-09-07T07:11:18Z | undo-redo | `realist` | 1183m 49s |
| 2026-09-07T07:27:02Z | undo-redo | `critic` | 1202m 22s |
| 2026-09-07T07:40:06Z | undo-redo | `spec-author` | 1220m 49s |
| 2026-09-07T07:57:18Z | undo-redo | `spec-author` | 1241m 43s |
| 2026-09-07T08:04:49Z | undo-redo | `architect` | 1244m 31s |
| 2026-09-07T08:11:01Z | undo-redo | `architect` | 1257m 04s |
| 2026-09-07T08:13:04Z | undo-redo | `realist` | 1257m 53s |
| 2026-09-07T08:14:03Z | undo-redo | `realist` | 1260m 07s |
| 2026-09-07T08:16:04Z | undo-redo | `spec-author` | 1260m 54s |
| 2026-09-07T08:29:34Z | undo-redo | `critic` | 1263m 13s |
| 2026-09-07T09:24:52Z | undo-redo | `spec-author` | 1327m 04s |
| 2026-09-07T09:26:00Z | undo-redo | `spec-author` | 1331m 52s |
| 2026-09-07T09:28:08Z | undo-redo | `realist` | 1332m 50s |
| 2026-09-07T09:33:38Z | undo-redo | `architect` | 1335m 18s |
| 2026-09-07T09:34:59Z | undo-redo | `architect` | 1340m 41s |
| 2026-09-07T09:55:00Z | undo-redo | `realist` | 1359m 45s |
| 2026-09-07T09:56:23Z | undo-redo | `architect` | 1362m 02s |
| 2026-09-07T10:03:32Z | undo-redo-design-record | `reviewer-blind` | 1366m 05s |
| 2026-09-07T10:13:42Z | undo-redo | `reviewer-blind` | 1373m 01s |
| 2026-09-07T10:27:11Z | undo-redo | `reviewer-reconcile` | 1380m 52s |
| 2026-09-07T10:28:35Z | undo-redo | `architect` | 1394m 43s |
| 2026-09-07T10:28:59Z | undo-redo | `architect` | 1395m 34s |
| 2026-09-07T10:41:53Z | undo-redo | `reviewer-reconcile` | 1400m 14s |
