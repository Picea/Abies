---
name: abies-recurring-risk-patterns
description: Risk shapes that recur in Abies design passes — unearned "by construction" claims, deferred instruments leaving Done-means items open, CI gates blind to the work, properties written in a mode the product never ships in, mitigations aimed at the wrong field, classifications needing an identity the seam does not carry, shared generator alphabets, retention bounds stated as counts, and amendments that never reach the handoff
metadata:
  type: project
---

Nine shapes have shown up in Abies design-pass artifacts. Check each one explicitly.

**1. "By construction" that is actually by instruction.** The repository's own review pair
already found hooks failing open where a claim said otherwise, so the squad has form here.
Test: *what enforces this — a type, or a sentence in one file?* If the guarantee is "only
this wrapper writes that field" and the field's type is public and constructible, it is by
instruction. Undo/redo pass (2026-09): `History<TModel>` / `Step<TModel>` claimed
INV-2-by-construction while both records were publicly constructible, and the plan's own
property test required fabricating arbitrary values of them.

**2. A deferred optional step silently orphans a Done-means item.** When the user defers or
cuts a "separable" step at a gate, re-read the scope's *Done means* list and the open
questions: the deferred piece is often the only instrument answering one of them. Undo/redo:
the projection lens was deferred at gate 3, and it was the only spatial exemption mechanism,
so open question 4 ("which state is exempt from undo") went unanswered and two blockers fell
out of the gap. The Realist usually flags the trade-off honestly *and then proceeds anyway* —
the flag is not the mitigation.

**3. A cited CI gate that cannot see the work.** Plans cite `.github/workflows/benchmark.yml`
(5% js-framework-benchmark) and `pr-validation.yml` (integer-MB bundle size) as if they gate
the design. Ask what they actually measure. If no application in the repo adopts the new code,
the benchmark measures a non-adopter and is silent whatever the feature costs; the size gate
is whole-megabyte and blind below 1 MB. Also: `pr-validation.yml`'s **1500-line hard PR-size
limit** exempts `docs/**` and `*.md` only — plans with 7 new source files plus a property
suite need a stated PR decomposition and almost never have one.

**4. A property written in a mode the product never ships in.** *Third time* this shape has
appeared in one pass. Read the property's *configuration*, not its statement. Undo/redo revision
2 planned INV-7's property with `SettleWindow = null`, an explicit terminal `Settle` and a
subscription that only dispatches on demand — so the auto-settle clock, the inter-press interval
and autonomous delivery were all excluded by construction, and the property could not falsify any
of the three ways the mechanism actually fails. Test: *would this property go red if the
mechanism were deleted and replaced with the thing it replaced?* If the answer needs the shipped
default and the property does not run under it, it is not coverage.

**5. A specialist requirement folded in against the wrong field.** A room reports the threat it
found by the route it walked. Check the *other* routes to the same value before treating the
requirement as met. Undo/redo: `security-expert` found credentials in `Step.Cause` (newly
retained) and specified a marker + redaction; the same credentials arrive via `Step.Model`,
because a controlled input requires the value to be in the model — so the mitigation missed the
room's own worked example. Also re-read the property the room specified: the Realist's rewording
of it had dropped the scoping clause and become unsatisfiable. **Third occurrence in the same
pass:** revision 3 stated the rule as *"`Scrub` runs at every site that constructs a `Step`"* and
the anchor (`Movement.Held(TModel)`) is a live retained model that is not a `Step`. The fix is
never "add another site" — it is to make the property quantify over the *value* rather than over a
named field.

**6. A classification that needs an identity the seam does not carry.** Before believing a rule of
the form *"if the change came from X, treat it as Y"*, find the point where X would have to be
observable and check that it is. Undo/redo B8/B9: the plan's `record`/`pass`/`seal` table asks
*"came through the envelope?"* — and `Runtime.cs:288-291` plus `:332` make a DOM handler event and a
subscription delivery **the same delegate into the same `Dispatch`**, while `Runtime.cs:460,484`
sends the `Decide` `Err` channel straight to `Transition` (`Picea.xml:1229-1239`: `AutomatonRuntime.
Dispatch` is transition → observer → interpreter, no `Decide`). So the only messages answering "no"
were interpreter feedback and decide errors — a set the plan never enumerated, and one that made a
timer tick a user action and a validation rejection "the world". Test: *enumerate the actual
messages on each side of the classification, from the runtime, before ranking any finding about it.*

**7. A generator alphabet shared by properties that contradict each other.** The cousin of #4, and
it appeared as soon as #4 was fixed. Undo/redo revision 4 answered "the property cannot see its
trigger" by adding an autonomously-delivering source to *step 8's* alphabet — which is shared, so
INV-1 ("the state immediately before the most recent action") and INV-3 (an immediate undo/redo
round trip) are both falsified by the mechanism working correctly, since a delivery **is** an action
under that design. Test: *for each property in a step, would the newly-added alphabet symbol make it
go red for a right reason?* If yes, the alphabet has to be partitioned or the property re-quantified,
and the plan must say which — otherwise the implementer silently deletes the symbol and #4 returns.

**8. A retention bound stated as a count when the risk is the duration.** Undo/redo S29: preserving a
superseded redo branch instead of clearing it moved the ceiling from 100 models to 200 — which was
disclosed and approved — while moving the *lifetime* of a walked-back model from "released at the
next action" to "possibly the whole session", which was not, and which invalidated the comparison a
security room's earlier answer rested on. Test: when a design starts retaining something it used to
discard, ask **both** *how many* and *for how long*, and check whether any prior specialist answer
was argued by comparison against the old window.

**9. A post-close-out amendment that lands in the plan and the spec but not in the handoff, and that
moves every line number behind it.** Undo/redo `[R4-nav]` (2026-09-07): the navigation rule landed in
six places in `04-realist-plan.md` and in `06-spec.md`, while `07-handoff.md` — written at close-out,
and the artifact that survives into a specialist's context — recorded neither the rule nor the scope
amendment, and still carried the sentence the rule contradicts in its *Standing decisions carried
into implementation* table. Separately, amending an invariant in `00-scope.md` lengthened it and
falsified every `00-scope.md:<line>` citation past it in both downstream artifacts. Test: after any
amendment, (i) re-read `07-handoff.md` and every *"implement from this"* block for sentences the
amendment made false, (ii) re-check line-range citations into the amended file, and (iii) check
whether any obligations/spec-line **numbering** now collides across artifacts — the plan and the spec
number the same list independently, so an appended row means "line 14" names two things.

**Why:** each of these produces a plan that reads as rigorous and fails at implementation or
at review, which is the most expensive place to find them.

**How to apply:** run all nine against every Realist plan before ranking findings. They are
cheap and they have each paid out at least once. Corollary from the undo/redo pass: when a
user decision *erases* something from a signature (a type parameter, a field), ask what the
framework is now obliged to retain that it would not have been — the erasure of `TUndoable`
is what forced whole models, and whole models is what retained the secrets.

See [[undo-redo-pass-critic-verdict]].
