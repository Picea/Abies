---
name: claims-a-property-cannot-carry
description: When a rule keys on a type the application supplies, the property drives the framework's type and stays green for the shape that has the bug — scope the claim as a stated adopter obligation and say so where the property is
metadata:
  type: feedback
---

When an invariant's rule keys on something the **application** chooses — a message type, a lens, a
wiring decision — a property in the framework's own test project can only ever drive the choice the
fixture made. It is green for every adopter that chose differently, including the one with the bug.
The move is not to widen the alphabet (you cannot: the type does not exist yet) but to **scope the
claim and write the obligation down as an instruction to the adopter**, in the same register as the
other assumptions the framework cannot test.

**Why:** on `undo-redo`, INV-2's second property asserted that a movement never lands on a model
whose URL the browser is not showing. The seal keyed on `Picea.Abies.UrlChanged`. But on the WASM
head there is no framework-owned delivery of an incoming URL change at all — it arrives through
`Navigation.UrlChanges(Func<Url, Message> toMessage)` (`Picea.Abies/Navigation.cs:17-29`), where the
**application** supplies the constructor. Conduit and the tutorials pass the framework's message;
the repository's own `README.md:190` passes an application-defined one, so the rule never fires,
navigations become ordinary undo stops, and the property stays green because the fixture dispatches
the framework type. The Critic found it (B10); the user took "scope the claim", not "add mechanism".

**How to apply:**
- Put it in the **obligations table as an instruction**, phrased at the adopter ("an application
  that … must …"), not as a caveat on the property. Cross-reference it from the existing
  application-obligation row so the two sit together — a reader who finds one finds the other.
- Say it **three times, in the three places a reader stops**: beside the property, in the
  `INV-n → property` table's coverage cell, and in *Behaviour NOT captured*. Mark the last one as
  **not capturable** rather than deferred, or someone will look for the later step that covers it.
- Check the **falsifier** too. Ours deleted the *rule*; the other route to the same defect never
  reaches the rule, so the falsifier does not reproduce it. Say that in the falsifier table.
- Also qualify the "what this test proves" bullet. A proof bullet with an unstated precondition on
  the adopter is the sentence that gets quoted back later.

The tell to look for while drafting: **the rule names a type, and the type reaches the runtime
through a delegate the application supplies.** Enumerate the actual messages on each side of a
classification, from the runtime, before believing the rule — this was the third time in one pass
that a classification needed an identity the seam did not carry.

Related: [[state-shared-with-the-outside]], [[invariant-alphabet-partition]],
[[what-belongs-in-the-lock]]
