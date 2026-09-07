---
name: invariant-alphabet-partition
description: When one property's generator alphabet contains an event that is falsifying-by-correct-behaviour for another property, write the partition down explicitly — otherwise implementation silently weakens one of them
metadata:
  type: project
---

When several `INV-n` properties share a generated alphabet, check whether any symbol in it
**falsifies another property by producing correct behaviour**. If so, partition the alphabet
explicitly in `06-spec.md`, per property, with the reason.

**Why:** on the `undo-redo` pass the design classified a subscription-delivered message as *an
action* (it records an undo stop and supersedes the forward branch). Putting an autonomously
delivering source into the shared step-8 alphabet then falsifies INV-1 ("the state before the most
recent action" — the delivery *is* the most recent action) and INV-3 (a delivery between the undo
and the redo makes the redo correctly refuse). Both go red while the mechanism behaves exactly as
specified. The two likely implementation-time outcomes are both invisible in a diff: drop the
source from the whole step — which deletes the one property that could see the bug it was added
for — or weaken the two properties until they pass. The Critic caught this as S27; nobody
downstream would have.

**How to apply:** put a per-property alphabet table in the spec, and for each excluded symbol say
whether exclusion is **required** (the property is falsified by correct behaviour) or merely
**uniform** (unaffected, kept on the same alphabet so the rule is one rule). Where a property is
excluded from a symbol, check that the *case* it was covering is still reachable another way — on
`undo-redo`, INV-6's `BlockedBySupersedingAction` case is reached by a user action after an undo,
so excluding deliveries cost nothing.

A related habit from the same pass, worth keeping: **every property gets a named mutation that must
be observed to turn it red**, listed in the spec. Four consecutive Critic passes on `undo-redo`
found properties planned in configurations that could not see their own trigger. A falsifier table
is the cheapest defence, and it also guards the unlocked support files.

Related: [[level-choice-framework-internals]]
