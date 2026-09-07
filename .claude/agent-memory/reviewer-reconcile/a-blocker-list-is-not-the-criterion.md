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
