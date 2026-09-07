---
id: architect-20260907T184500Z-undo-redo-navigation
agent: architect
verdict: INFO
scope: architecture
created: 2026-09-07T18:45:00Z
targets:
  - path: Picea.Abies.History
  - path: .squad/design/undo-redo/
blockers: []
high:
  - reason: "Adopter obligation (06-spec.md obligations line 17): incoming navigation reaches a WebAssembly program through an application-supplied converter (Picea.Abies/Navigation.cs:17-29), so the seal fires only for the framework's Picea.Abies.UrlChanged. An application composing WithHistory must pass `url => new UrlChanged(url)`; one that names its own navigation message gets no seal, its navigations become ordinary undo stops, and undo restores a model whose URL the browser is not showing — with every property in the spec still green. No framework property can quantify over this."
  - reason: "Accepted, documented limitation (room-security.md § Follow-up 2026-09-07 (III), S33): the seal makes the whole Url — path, query string and fragment — the stored EdgeState cause of every post-record navigation, returned by Backward/Forward and rendered by the chrome on every render the edge stays refused. UrlChanged is sealed and framework-owned, so no adopter can mark it SensitiveCause. A password-reset or magic-link token in the query string is displayed in plain text. Rendered-chrome exposure, not a retention one; disposition is accepted-and-documented rather than a framework-side SEC-2 rule."
  - file: README.md
    reason: "README.md:183-190 is this repository's own counter-example to the obligation above — it hands an adopter `url => new UrlChangedTo(url)`. tech-writer fixes it at plan step 13, noting that the README example is not itself a WithHistory adopter."
medium:
  - reason: "Cite 00-scope.md by invariant id and quoted clause, never by line range. The amendment lengthened INV-2 and staled four line-range citations across the plan and the spec at once. The scope is a file that moves; the ids exist to make it citable."
  - reason: "00-scope.md now reads 'at least one property per id' — INV-2 carries two properties, and validate-phase-artifact.sh reports only missing ids, with no count and no cap. Plan step 8(a) reads the same."
good:
  - reason: "The seal is derivable rather than agreed: of the four classification rows, only `seal` survives the amended INV-2. The Critic re-derived it independently instead of accepting the account, and confirmed the amendment with no revision 5, no gate reopened and no mechanism added."
references:
  - architect-20260907T120000Z-undo-redo
---

Incoming browser navigation is a commitment in both directions: INV-2's world now includes the browser's location, and a post-record `UrlChanged` seals the history on both edges instead of recording an undo stop.

Amends `architect-20260907T120000Z-undo-redo` (`WithHistory`, refusal at the effect boundary). The direction, the mechanism and every gate decision in that drop stand; this is one classification row and its consequences.

## What was wrong

Only navigation *before* anything was recorded had a rule (origin re-basing). After `Origin` becomes `Established`, a `UrlChanged` delivered by the browser on a **back or forward press** was enveloped like any other message and took the `record` row. Undo across it restored a model carrying the previous page's `Route` while the browser stayed where the user put it, and the browser's own back/forward stack walked away from the application's history. Raised by the user after close-out; no phase caught it.

The reason no phase caught it is worth more than the bug: `00-scope.md`'s INV-2 said *"application state"* and its falsifier said **model**, so every downstream property quantified over the model alone and was green for this whole class of violation. Track A had already named the shape — `model ⊗ world` — in `01-track-a.md`; the scope had not.

## Decision

**INV-2 amended** (the pass's third `00-scope.md` amendment): the state it quantifies over is the model **together with the location the user is at**, on any head where the application has one. A navigation is a commitment in both directions — one the application asked for, and one the browser delivered. The two move together or not at all. Falsifier extended with the navigation case and with the browser-stack divergence case; id and falsifier shape kept.

**One classification rule, a standing constraint on implementation:**

> `origin is UrlChanged && Origin is Established -> seal, SealedByWorld(UrlChanged)` — both incident edges. `Backward(h)` and `Forward(h)` refuse with the navigation as cause.

## Why the seal is forced, not chosen

Of the four classification rows against an incoming `UrlChanged` once `Origin` is `Established`: `record` mints an undo stop whose crossing produces a `(model, location)` pair no ordinary interaction produced — the amended INV-2's new falsifier verbatim; `pass` leaves the *earlier* stop crossable and hits the same falsifier one press later; `rebase` is `Fresh`-only by construction and unavailable in this window; `seal` satisfies the invariant exactly, because `Present.Route` moves with the location in the same transition and both edges refuse thereafter. The amended INV-2 both permits the seal and **forces** it.

**INV-3 needed no location clause, and that is a finding rather than an omission.** `Present.Route` moves only on a transition whose origin is `UrlChanged`; after the first `record` every such transition seals both incident edges, and before it there is nothing to cross — so the location is **constant across any window in which a movement is permitted**. A location conjunct on INV-3 would be one no property could ever turn red on, which is worse than leaving it out.

## Consequences

- **The reach statement gains a third bound** — the last incoming navigation — alongside the last command feedback and `Depth` ÷ subscription rate. In a routed application it is usually the binding one: in Conduit, undo reaches back to the current page and no further.
- ***"The world is a command's feedback and nothing else"* is no longer true unqualified.** It carries one exception in each of its four homes.
- **`ux-expert`'s q9/q13 premise changed** and that brief is dispatched first, so the corrected premise travels with wave 0.
- **Cost:** no new type, no new `EdgeState`, no new `MovementAvailability` case, no new `Scrub` site, no policy member, no dependency, no line of `Runtime.cs`. Doc-and-one-row.

## Alternatives considered

- **Qualifying INV-2 with the adopter's wiring condition** — rejected. The condition is a statement about the application's own composition, which no framework property can quantify over; it belongs in the spec's obligations table beside the lens laws, not inside an invariant that is true of the framework.
- **Mechanism for the converter hole** (policy predicate, framework-owned `UrlChanges` wrapper, analyzer rule) — all three are new mechanism and one reverses a gate-4 deletion; none is needed to state the truth. **An analyzer rule is named as a follow-on candidate**, since an obligation a compiler can check beats one a guide asserts, and `Picea.Abies.Analyzers` already exists.
- **A framework-side SEC-2 rule for `UrlChanged`'s URL** — declined by the user in favour of the documented limitation above.

Full artifacts: `.squad/design/undo-redo/`. The rule and its derivation are `04-realist-plan.md` § *The classification rule* `[R4-nav]`; the confirmation is `05-critic.md` § *confirmation pass, 2026-09-07* (CONFIRMED WITH A BOUNDED LIST — two 🔴, four 🟠, seven 🟡, all thirteen mitigations accepted by the user); the execution contract is `07-handoff.md` § 2.5, standing decision 7, § 5.3 and § 6.1.
