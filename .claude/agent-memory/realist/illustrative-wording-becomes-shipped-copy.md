---
name: illustrative-wording-becomes-shipped-copy
description: A sentence written in a plan to demonstrate a mechanism ("Undo unavailable — superseded by FastTick") gets asserted on in a Done-when and shipped — write the template, or mark it illustrative, but never leave it bare
metadata:
  type: feedback
---

When a plan needs to show that a typed cause is *renderable*, the fastest way to show it is to write
the sentence out — *"Redo unavailable — superseded by `FastTick`"*. That sentence then travels: into
the step's **Done-when** (where a composition test asserts on it), into the **tech-writer** step's
guide obligation, into the **file-docs** bullet, and into the **narrative** section that motivated it.
By the time a UX room reads the plan, the placeholder is the specification.

**Why:** in `undo-redo`, two such sentences reached wave 0 — a raw C# message type name as end-user
copy, and a world-refusal sentence ("the page changed while you were editing") reused for a
navigation the *user* performed on purpose. Both came back 🔴. The mechanism was right in each case;
only the words were placeholders that nobody had labelled as placeholders. A `Cause.GetType().Name`
is a developer artifact and a user-facing string at once, which is exactly why it slips through.

**How to apply:** when a plan writes user-visible text to illustrate a mechanism, do one of three
things at the point of writing — (a) write the actual template and say it is settled, (b) annotate it
*"illustrative of mechanism, not production copy"*, or (c) name the question and the room that owns
it **and keep it out of the Done-when until that room answers**. A Done-when that quotes unsettled
copy is a test that will be written against a guess. Grep the quoted fragment before declaring a
wording fold complete: in the same pass the two sentences had **seven** homes between them, three of
them in narrative prose that no step obligation reaches.

Corollary from the same room report: a refusal that is *"answerable in advance"* is a mechanism
property, not a presentation one. "Refuses **by name**" reads as "shows the name" unless the plan
says otherwise — qualify it where the reasons are defined, not only where they are rendered.

Related: [[an-exception-named-by-element-has-four-sites]], [[one-route-mitigations-repeat]],
[[framework-messages-need-naming-not-just-origin]].
