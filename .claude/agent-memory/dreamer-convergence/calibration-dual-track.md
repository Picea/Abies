---
name: calibration-dual-track
description: Per-pass calibration record for the dual-track Dreamer in Abies — convergence rate, novelty hit rate, and wall-clock cost. The one record that answers whether Track A earns its keep.
metadata:
  type: project
---

The squad's running answer to "is the dual track worth it for this class of work."
One row per pass. **Novelty hit rate is only honest with the cost beside it** — see
[[pass-cost-instrument-reads-cumulative]] for how to read the denominator.

## Ledger

| Pass | Kind of problem | Convergence | Novelty claims | Survived Critic? | Shipped? | Marginal cost of Track A |
|---|---|---|---|---|---|---|
| `undo-redo` (2026-09-06) | Greenfield framework feature on a brownfield kernel; Track A weighted heavily by the architect | **Very high** — A1 and B1 are the same object, derived independently, both pointing at the same local precedent (`WithView`) | **5** (see below) | pending | pending | **≈ 8m 44s** extra wall-clock (tracks ran in parallel; pair cost ≈ 9m 46s total) |

**Gate 2 outcome (`undo-redo`):** H1 adopted. The user took **Track A's position** on the
pass's sharpest divergence (refuse to undo across an effect, rather than Track B's
cross-with-a-documented-contract), and confirmed the granularity answer both tracks derived
independently to the same place. Novelty claim #2 (INV-2's `model ⊗ world` blindness) is the
one that decided it — the user's reasoning was that *under refusal* model and world never
diverge, so INV-2 stands as written. That is claim #2 doing real work at a gate, though it
does not count as "survived the Critic" yet.

**Running novelty hit rate: 0/5 confirmed, 5 pending.** Not zero-because-nothing-was-found;
zero-because-nothing-has-reached-the-Critic-yet. The charter's "say so plainly if it stays
at zero" clause is not yet triggered, but it will be if these five do not survive. Come
back to this row after `05-critic.md` and again after review.

## `undo-redo` — the five novelty claims, so a later pass can score them

1. **Stratification + "destination computed in `Transition`, never `Decide`."** Because
   `Dispatch` releases `_decisionGate` between the stages (`Runtime.cs:448-500`). No
   surveyed system has this hazard because none has a two-stage kernel with a lock released
   between stages — retrieval could not have supplied it.
2. **INV-2 is blind to the failure it was written for.** Undoing across an effect lands on
   a model that *was* visited, so it is trivially "reachable"; the observable state is
   `model ⊗ world` and INV-2 quantifies over the first factor only. Track A found a hole in
   the scope's own invariant set. Highest-value single find of the pass.
3. **`View` is not a function of the model** — `Events.NextCommandId` uses a global
   `Interlocked` counter at element-construction time, so INV-3's "same rendered document"
   is literally false for any re-rendering design. Verified.
4. **The effect boundary as algebra** — commands are a monoid under `Batch`; undo's reach is
   exactly the sub-structure the app has closed under inversion. Yielded one concrete rule
   Track B did not reach: undo across a navigation issues `Replace`, not push.
5. **`Transition` reads the wall clock in a shipped app** (`SubscriptionsDemo/Program.cs:140`),
   which kills replay-based reconstruction on *correctness*, where Track B killed it only on
   *cost*. Required reading an application rather than the framework.

## What the convergence itself taught (worth reusing)

Independent convergence in this repo has been informative in a specific way: both tracks
reached the higher-order-`Program`-over-a-zipper shape, and the value was not the agreement
but that **Track A's derivation explained Track B's citation.** The citations show a shape
works elsewhere; the derivation showed it is *forced here* by four kernel properties. When
writing future convergences, say which one explains which — "both tracks agreed" on its own
is close to worthless.

Track B's retrieval closed exactly one of the three `Picea`-is-an-external-package unknowns
Track A flagged — and it was the one Track A itself ranked most important. One for one is a
fair return; do not oversell it.

See also [[track-a-missed-constraint-patterns]] and
[[adr-line-left-reachable-is-an-experiment]].
