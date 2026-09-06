---
name: a-disclaimer-does-not-fix-a-false-claim
description: An asterisk or hedge does not repair a claim whose direction is wrong — require the claim to be restated
metadata:
  type: feedback
---

When a factual claim points in the wrong direction, a disclaimer does not fix
it. The claim has to be restated.

**Why:** From the 2026-04-15 dry-run audit of the "Coderen met AI in 2026"
express deck. A slide claimed a benchmark "beats Blazor WASM on virtually all
duration tests"; the data showed roughly 5 of 9 wins and a 2.5× *loss* on
`clear1k`. An asterisked disclaimer had been added, and it did not help — the
audience takes the headline, not the footnote, and the headline was false. The
accepted fix was honest framing: competitive overall, wins on key creation,
gaps remain on clear and swap.

**How to apply:** Distinguish "imprecise" from "directionally wrong". Imprecision
can often be hedged; a wrong direction cannot, and a hedge on top of it reads as
knowing overclaim. This applies well beyond slides — benchmark READMEs, release
notes and comparison tables have the same failure mode. Related pressure point:
a second blocking finding in the same audit was one statistic attributed to two
different sources, so check attribution and direction as separate passes.
