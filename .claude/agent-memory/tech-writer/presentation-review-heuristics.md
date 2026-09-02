---
name: presentation-review-heuristics
description: Reusable findings from reviewing conference decks in this repo — one format per slide, explicit section transitions, and Dunglish watch
metadata:
  type: feedback
---

Heuristics from the 2026-04-15 dry-run review of the "Coderen met AI in 2026"
express deck (19 slides, Dutch, ~30 min), which live in
`Picea.Abies.Presentation/Program.cs` under `_expressSlides`:

- **One format per slide.** The `tools` slide carried both bullets and a
  five-column table — pick one. Two formats competing for the same content is
  the most reliable overload signal.
- **Write the section transitions.** Missing bridges between Deel 2→3 and
  Deel 3→4 created real navigational gaps. A speaker under pressure needs the
  transition on the slide, not in their memory.
- **Watch for Dunglish.** In otherwise clean Dutch, one anglicised construction
  stands out badly ("compoundt over sessies" → "bouwt op over sessies").
- **A human anecdote is worth more than a stat.** The `metr-followup` slide was
  the structural linchpin — it converted data into a behavioural insight and was
  the moment the audience leaned in. Protect that slide in edits rather than
  compressing it.
- **Self-deprecating closings work** when delivered with confidence, and
  undermine when apologetic. Flag them as a delivery risk, not a text problem.

**Why:** The deck was reviewed before delivery and the findings were accepted
into the team decisions file (entries dated 2026-04-28, "Express presentation
dry run"). These five generalise beyond that talk; the slide-by-slide verdicts
do not and are not repeated here.

**How to apply:** Use as a checklist for any future deck review in this repo.
Pair with the UX Expert's projection-format constraints
([[projector-hostile-slide-formats]]) and the Reviewer's rule that
[[a-disclaimer-does-not-fix-a-false-claim]].
