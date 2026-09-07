---
name: undo-redo-withhistory-decision
description: Undo/redo decided as WithHistory with refusal at the effect boundary; ADR-030 not yet written; what it deliberately excluded and which triggers would reopen it
metadata:
  type: project
---

Deep pass `undo-redo`, closed 2026-09-07 after three Critic loop-backs. Drop:
`architect-20260907T120000Z-undo-redo`. Artifacts: `.squad/design/undo-redo/`.

**Decision.** `Picea.Abies.History.WithHistory<TProgram, TPolicy, TModel, TArgument>` —
a higher-order `Program` over a `(Past, Present, Future)` zipper, composed at the
application's call site in the same static-forwarding shape as `WithView`. Undo
**refuses** to cross an action that spoke to the world, with a typed reason that
is answerable from the history value *before* the press.

**Why:** both blind Dreamer tracks derived the same object independently, and the
kernel's structure forces it — one dispatch funnel, effect-isolating `Transition`,
model-derived subscriptions. Zero lines of `Runtime.cs`; no reflection, no
serializer; **ADR-025 examined and found not to need superseding**, so nothing
moves out of `#if DEBUG`. The `runtime-seams-anchor-replay-gating` revisit trigger
did **not** fire — see [[runtime-seams-anchor-replay-gating]].

**Amended after close-out (`[R4-nav]`, 2026-09-07, from the user).** Incoming navigation
was unclassified: after the first record, a browser back/forward press recorded, so undo
restored a model without its URL. INV-2 amended — **the world it quantifies over includes
the browser's location; a navigation is a commitment in both directions.** One
classification row (`UrlChanged && Origin is Established -> seal, SealedByWorld`), both
edges. Critic CONFIRMED, no revision 5. Three things worth keeping:
*seal is forced, not chosen* — of four rows only it survives the amended INV-2;
*INV-3 needed no location clause* — Route moves only on `UrlChanged`, which seals both
edges, so the location is constant across any window where a movement is permitted, and a
location conjunct would be one no property could turn red on;
*the guarantee is conditional on adopter wiring* (`Navigation.UrlChanges` takes the
application's message constructor) — stated as spec obligation line 17, **not** as a
qualifier on the invariant.

**How to apply — deliberate exclusions that owe something if they return:**

- **A3(b), crossable-iff-the-application-names-an-inverse.** Excluded twice. It
  owes **its own invariant**: INV-2 is only sufficient in this design *because*
  nothing crosses. Track A's `model ⊗ world` factorisation is that invariant's
  starting point. Do not let a later pass reintroduce crossing under INV-2 alone.
- **Deferral / hold windows** (Gmail's Undo Send) — correct for irrevocable
  actions, out because it means the framework holding commands back, i.e. a fifth
  seam and an architecture decision.
- **Durable undo, DOM-owned state, `NavigationCommand.Replace`.** Each excluded
  with a stated reason, not by omission.
- **No demo or template adopts `WithHistory`**, which makes both CI gates
  structurally silent on it. "The benchmark is green" is not evidence here, and
  the integer-MB size gate never was.

**Revisit triggers.**

- If anything is ever allowed to cross an effect boundary, INV-2 lapses that
  instant — reopen with a new invariant, not with a step.
- If `ux-expert` q4 makes a selection/caret bookmark mandatory, DOM-owned state
  becomes a scope amendment rather than a follow-on.
- If `AutomatonRuntime.Reset(TState)` is confirmed public (strong evidence in
  `Picea.xml`; step 15 was to prove it by compilation), the deferred **H2** pass —
  the debugger reading the release history instead of keeping a parallel timeline,
  which makes the debug path *smaller* — becomes cheap.
