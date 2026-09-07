---
name: what-belongs-in-the-lock
description: Deciding a stated behaviour into a plan step instead of the locked spec on "fixture cost" grounds — the user pulled one back in, and the fixture cost was much smaller than estimated
metadata:
  type: feedback
---

When a spec line describes behaviour someone would refuse to believe without seeing it fail, it
belongs **in the locked spec**, even if it needs its own fixture shape. Do not park it in a plan
step on fixture-cost grounds without first checking what the fixture actually costs.

**Why:** on the `undo-redo` pass I drafted thirteen spec lines and asserted nine, putting four
(pre-record navigation, `Clear`'s two effects, undo out of a terminal state, the InteractiveAuto
handoff) into plan steps 6 and 10, reasoning that each needed a fixture shape that would "triple
the fixture surface for one assertion each". I flagged the choice and asked. The user moved **one**
back in — *undo remains dispatchable out of a terminal state and can resurrect a terminated
program*. It cost one message, one `IsTerminal` predicate and one generator exclusion. My estimate
was wrong, and the line I had mis-sorted was the one making the most surprising claim of the four.

**How to apply:** sort spec lines by *"would a reader refuse to believe this without a red test?"*
rather than by fixture cost, and cost the fixture before deciding. Keep flagging the ones left out
with an explicit "moving it in now is free" — that offer is what made the correction cheap, and the
user took it. Lines that are genuinely head-shaped (a head with no initial URL, an Auto handoff)
are the ones that really do stay in plan steps.

Two mechanics worth reusing when a behaviour like this moves in:
- A terminal/absorbing state must be **excluded from the generator alphabet**, for the same reason
  the delivery partition exists — ordinary messages become no-ops in it, so INV-1's "state before
  the most recent action" is falsified by correct behaviour. See [[invariant-alphabet-partition]].
- Assert **both halves of a conjunction**. `IsTerminal(h) = TProgram.IsTerminal(h.Present) && both
  stacks empty`. Testing only the false half would still pass against a hardcoded `false`.

**Confirmed later the same pass, and this time I acted on it unprompted.** The Critic raised, as an
explicitly *optional* 🟡, that the pass's most user-recognisable behaviour (*type, press Back, press
undo — it refuses and names the navigation*) lived only in a property and had no acceptance-layer
example. I took it and added the test rather than reporting it as declinable. The acceptance layer's
only job is recognition, so "recognisable" is the strongest possible argument for inclusion; and the
fixture cost was **zero**, because the amendment that created the behaviour had already added the
field, the `Transition` case and the message. When a Critic offers an acceptance test as optional
and the fixture cost is nil, take it and say in the record that you took an optional item — do not
hand the user a decision that has no downside.

Related: [[spec-project-layout-abies]], [[claims-a-property-cannot-carry]]
