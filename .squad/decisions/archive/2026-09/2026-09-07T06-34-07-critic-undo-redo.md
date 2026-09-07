---
id: critic-20260907T000000Z-undo-redo-pass3
agent: critic
verdict: NEEDS-CHANGES
scope: architecture
created: 2026-09-07T00:00:00Z
targets:
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "605-665"
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "342-365"
  - path: Picea.Abies/Runtime.cs
    lines: "288-291"
blockers:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 618
    reason: "B8 — an application's Decide Err message reaches Transition bare (Runtime.cs:460,484; Picea.xml:1229-1239 shows AutomatonRuntime.Dispatch does not call Decide), so apply classifies it as seal/SealedByWorld. A validation rejection permanently blocks undo and redo and names the wrong category of reason, violating INV-5's identify-which-reason clause. Fix: Err(e) -> Err(HistoryEvent.Enveloped(m,[e]))."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 352
    reason: "B9 — subscription-delivered messages are indistinguishable from user actions at the wrapper (Runtime.cs:288-291 and :332 are the same delegate), so a timer tick is enveloped and records, clearing Future. The plan's default-policy narrative claims the opposite (a typed refusal naming FastTick). In SubscriptionsDemo redo is silently destroyed every 250 ms — S9's opacity and B2's harm on the record path. Requires a corrected narrative, a step-8 autonomous-source property, and a user decision on whether a discarded redo branch should refuse with a reason."
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S18 — the Enveloped fold also changes error semantics (Runtime.cs:353-365 aborts the batch after the model is fully folded); the user approved the ordering change only."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S19 — L5 forces Scrub to the identity under WholeModelHistoryPolicy; an adopter overriding Scrub alone silently deletes the field on undo."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S20 — Movement.Held(Anchor) is a live unscrubbed model outside SEC-3(b)'s 'reachable via any Step.Model' set. Third occurrence of the wrong-field shape in this pass."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S21 — step 9 assertion (4) describes a hold left open settling, which the design cannot do."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S22/S23/S24 — INV-7's protection is confined to bracketed runs and no button chrome can bracket; the origin re-basing exception is stated far more narrowly than the rule behaves; the recommended keydown/keyup bracket loses its Settle on a lost key-up."
medium: []
good:
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "Deleting the settle clock closed B5 and B6 by subtraction rather than by guard; the edge algebra is consistent under Pop; Scrub survives the INV-3 derivation via L5."
references: []
---

Third Critic pass on `undo-redo` revision 3: LOOP BACK TO REALIST with two blockers, both in the classification of who moved the model.

Revision 3 closes B5, B6, B7 and all seventeen 🟠/🟡 from pass 2, verified in the code rather than in
its own disposition table. What survives is the four-row record/pass/seal table's first question,
"came through the envelope?", whose real answer — interpreter feedback and `Decide` errors, and
nothing else — is never stated. B8 misclassifies a validation rejection as the world moving; B9
misclassifies a subscription delivery as a user action, which silently destroys redo in the
`SubscriptionsDemo` shape and contradicts the default-policy narrative the user approved.

Neither needs new mechanism. `05-critic.md` § *What revision 4 must contain* lists nine bounded
items; nothing goes back to the Dreamer and H1 is untouched.
