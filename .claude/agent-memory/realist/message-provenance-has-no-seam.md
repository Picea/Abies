---
name: message-provenance-has-no-seam
description: Estimation calibration — any plan that classifies messages by where they came from will lose a Critic pass, because the Abies kernel has no seam that distinguishes a subscription delivery, a user click, or a decider rejection
metadata:
  type: project
---

If a design's rule reads *"treat a message differently depending on **who sent it**"* — the user, a
subscription, the world, the application's own decider — cost a loop-back unless the plan states,
message kind by message kind, which real messages land on which side. The classification looks total
in a two-column table and is not.

**Why:** on the `undo-redo` pass, revision 3's four-row rule asked *"came through the envelope?"* and
never named a single real message that answered *no*. Two blockers fell out of the same omission, at
the third Critic pass.

- **A subscription delivery is a button click.** `Runtime.DispatchFromSubscription` and every DOM/
  native handler enter through **one delegate** into one public `Dispatch` into `Program.Decide`. A
  timer tick is enveloped exactly like a keystroke, so the design's "user action" branch fired every
  250 ms in `SubscriptionsDemo` and silently destroyed the redo branch. Recovering the distinction
  needs a runtime seam that tags subscription-origin messages — an architecture decision, not a plan
  detail, and it fires the `runtime-seams-anchor-replay-gating` revisit trigger.
- **A `Decide` rejection is *not* a decision.** `Runtime.Dispatch` treats the `Err` channel as a
  message to **dispatch**, not an event to decide (`decidedError = decision.Error`, then
  `_core.Dispatch(decidedError)`), and `AutomatonRuntime.Dispatch` runs *transition, observer,
  interpreter* — `Program.Decide` is in none of them. So a validation rejection arrives by the same
  bare path as interpreter feedback and gets classified as "the world".

After both fixes, the only bare arrivals are **feedback from a command the application issued**
(`InterpretEffect` dispatches feedback as events) and the wrapper's own messages. That is the whole
of "the world" at a program-level seam, and it is a much smaller set than any plan assumes.

**How to apply:** write the enumeration into the plan before the Critic does — for each message kind
the repo actually produces (DOM event, native control event, subscription delivery, `Decide` `Ok`,
`Decide` `Err`, interpreter feedback, the wrapper's own), which branch it takes. If the design needs
a distinction the enumeration says it cannot have, say so and put the choice to the user; do not
narrate the behaviour you wanted. And pair it with a property that has the real source in its
alphabet — a plan whose only autonomous source lives in a *different* step's test cannot observe the
failure at all.

Related: [[vacuously-met-invariants-are-the-loop-back-tell]],
[[picea-package-xml-answers-kernel-questions]], [[one-route-mitigations-repeat]].
