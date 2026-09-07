---
name: vacuously-met-invariants-are-the-loop-back-tell
description: Estimation calibration — a plan that says an invariant holds "vacuously" or "by construction" is where the Critic will loop it back; the real design is missing
metadata:
  type: project
---

When a plan claims an invariant is satisfied **vacuously**, **by construction**, or **because the
design has no interior**, treat that as an unwritten section rather than as a discharged
obligation — and cost it as a step, not a sentence.

**Why:** on the `undo-redo` pass (2026-09-06) revision 1 of `04-realist-plan.md` claimed INV-7
(subscriptions during undo) held vacuously, inheriting the phrase from `03-convergence.md`. The
Critic falsified it in two lines: the claim was true of *one* movement and the invariant quantified
over *sequences*. Three of the four blockers had the same shape — an untreated case ("a model
change that did not come through the wrapper's envelope") that every planned property passed
straight through. The loop-back added a whole mechanism (a transit anchor, a settling signal, a
runtime-level test vehicle) plus a lens the plan had proposed to defer. Roughly a 40% growth in the
todo list, none of it visible at the first 🛑.

Two related tells from the same pass:
- An invariant with **no test vehicle at the planned test level** is not covered, whatever the
  todo list says. A property about the *runtime's* behaviour across a sequence of wrapper states
  cannot live in a domain test.
- A plan that names a **separable/deferrable step** and then, elsewhere, argues that step is what
  makes another part *safe* has already told you the deferral is wrong. Read your own Cleanness
  section back before the 🛑.

**How to apply:** for every invariant, write the sentence "this is falsified by …" and check that
the falsifier ranges over the same things the invariant does (sequences, not single operations;
the whole observable state, not one factor). If the answer is "nothing can falsify it", ask which
untreated case the design is silently classifying as a no-op.

Related: [[higher-order-program-step-shape]], [[namespace-plan]].
