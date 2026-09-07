---
name: a-blocker-list-is-not-the-criterion
description: When a Merge Criterion (b) blocker enumerates which findings need registering, the author will treat the list as exhaustive — enumerate by rule, not by index, or (b) comes back unmet
metadata:
  type: feedback
---

A 🔴 that cites Merge Criterion (b) and then names the findings it wants
registered creates two texts that can disagree: the criterion ("**every** finding
that is not a regression of a stated property is registered") and the reviewer's
list. The author will follow the list.

**Why:** PR #359 round 1. My blocker read "None of ⚠️ 1, ⚠️ 3, ⚠️ 5 or ⚠️ 6 is
registered" — but ⚠️ 4's own body ended "*register it*", and ⚠️ 7 and ⚠️ 8 were
unfixed too. `security-expert` registered exactly four, and said in the ledger's
own preamble that it read my blocker's list as the scope. That reading is
defensible on my text and wrong on the rule, so round 2 came back with (b) still
unmet on three items — a second round spent on a defect in my *round-1
enumeration*, not in the fix pass. At round 2 of a 2-round cap that is expensive.

**How to apply:**

- Write the blocker as **"every unfixed finding below, which at minimum
  includes …"**, not as a closed list. The list is a pointer to the criterion,
  never a substitute for it.
- Before issuing, sweep your own ⚠️ section for the string *register* and for any
  finding you left unfixed-and-unregistered. Anything that says "register it" in
  its body and is missing from the blocker is the exact gap this note is about.
- A finding that is **fixable in place** still needs registration **if it is not
  fixed**. "Fixable in place" is a routing statement, not a disposition — on #359
  `security-expert` classified its *own* memory note that way and then did not
  edit it, and the note shipped still recommending the name the pass rejected.
- Grade the return honestly anyway: the three blockers I *did* state were closed
  cleanly, and escalating previously-⚠️ items to 🔴 in the round that answered my
  🔴 is the goalpost-moving the cap exists to stop. ⚠️ Needs Human Review, with
  the proportionality call handed to the user, is the shape — see
  [[the-round-cap-is-a-verdict-shape]].

Related: [[audit-the-residual-ledger-in-both-directions]] — the same round's
other lesson, and the one that catches a registration written *before* its own
commit's fix landed.

## Round-2 addendum, `presentation-demo` (2026-09-07): stating it as a rule is not enough either

I applied this note's advice literally on `presentation-demo` round 1 — the
blocker was written as a rule, with "*Instances I found:*" and an explicit
warning that "an enumeration will be read as the scope and these are examples,
not the criterion." **The author still fixed exactly the four instances and did
not sweep.** Fixing all four, well, and stopping is the rational reading of any
blocker that ships with a list, however the list is framed.

So the framing fix is necessary but insufficient. The load-bearing move is:
**run the sweep yourself and say the list is exhaustive.** On round 2 I checked
every numeric citation in the index (27), every claim about the pinned commit's
own metadata (1), and every "this fragment is X's summary" claim against the
fragment's content — found two more, and wrote *"this list is exhaustive, and I
say so deliberately … round 3 closes this."* That converts an open-ended rule
into a bounded fix list and is what keeps the cap reachable.

Do the sweep in **round 1** where the finding is enumerable. If it is genuinely
not enumerable in round 1, say *that* in the blocker — "I have not swept; the
rule, not my four examples, is the criterion" — so the author knows the sweep is
theirs and unbounded, rather than inferring a four-item scope from four items.
See [[prefix-stripping-fixes-are-unbounded]] for the same shape from the other
direction.
