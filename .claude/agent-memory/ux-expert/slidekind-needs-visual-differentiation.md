---
name: slidekind-needs-visual-differentiation
description: Open recommendation — the SlideKind taxonomy in the presentation project is correct but earns nothing until rendering differentiates the kinds visually
metadata:
  type: project
---

`Picea.Abies.Presentation` models slides with a `SlideKind` taxonomy. Assessed
on 2026-04-15 as architecturally correct — but it only adds user-visible value
if rendering actually differentiates the kinds, in particular Intro and Outro
slides from Concept slides. As of that review it did not.

**Why:** A taxonomy that does not change what the audience sees is bookkeeping.
The value of marking a slide as an intro is that the audience gets a visual
signal they are entering a new movement of the talk, which is what makes section
transitions land — the same gap the Tech Writer flagged from the narrative side
in [[presentation-review-heuristics]].

**How to apply:** This is an open recommendation, not a completed change. If
asked to improve the presentation project's rendering, this is the highest-value
item that is already scaffolded — the model work is done and only the view side
is missing. Verify current rendering behaviour first: `SlideKind` still exists
in `Picea.Abies.Presentation/Program.cs` as of 2026-09-02, but whether the
differentiation has since been implemented was not re-checked.
