---
name: projector-hostile-slide-formats
description: Markdown tables and monospace/ASCII charts break on conference projectors — they are the highest-priority visual defect in any deck review
metadata:
  type: feedback
---

Two slide formats reliably fail on a conference projector and should be flagged
as blocking visual defects:

- **Markdown tables.** Column alignment depends on rendering width; a
  five-column table is unreadable from row six of the room.
- **ASCII bar charts in code blocks.** They depend on monospace alignment, which
  breaks with projector scaling and font substitution.

Both must become real charts, or be split across slides.

**Why:** Found in the 2026-04-15 express deck dry run, where the `tools` slide
(a five-column table with three product lines × 4–5 metrics) and the `trust`
slide (ASCII bars) were the two highest-priority interventions. Both look fine
in document mode on the author's screen, which is exactly why they survive to
delivery — the failure only appears at projection scale.

**How to apply:** Review decks in projection conditions, not document mode. When
flagging, propose the replacement chart rather than only the problem: the
usage/trust divergence in that deck (84% use, 29% trust) was identified as the
single best chart candidate and the ideal live-demo payoff. Related:
[[data-parade-fatigue]] and the Tech Writer's
[[presentation-review-heuristics]].
