# Orchestration Log — Reviewer Dry Run (Factual Accuracy)

**Date:** 2026-04-28  
**Agent:** Reviewer (independent quality authority)  
**Session type:** Express presentation factual audit  
**Requested by:** Maurice  
**Status:** 🔴 BLOCKING — two issues must be resolved before presenting

---

## Task Scope

Factual accuracy and data claims review of `_expressSlides` (19 slides, Dutch, "Coderen met AI in 2026"). Independent audit against documented benchmark results, memory notes, and cited sources.

---

## Blocking Findings

### 🔴 BLOCKING 1: Benchmark claim overstates competitive position

**Slide:** `picea-abies`  
**Claim:** "Verslaat Blazor WASM op vrijwel alle js-framework duration tests*"

Cross-referencing `benchmark-results/blazor-baseline.json`, post-SetChildrenHtml Abies numbers (Feb 19, 2026), and `docs/benchmarks.md`:

| Benchmark | Abies | Blazor | Winner |
|---|---|---|---|
| 01_run1k | ~72ms | ~85ms | **Abies** |
| 02_replace1k | ~96ms | ~100ms | **Abies** |
| 03_update10th1k | ~45ms | ~95ms | **Abies** |
| 04_select1k | ~8ms | ~83ms | **Abies** (needs verification) |
| 05_swap1k | ~108ms | ~95ms | **Blazor** |
| 06_remove-one-1k | ~35ms | ~40ms | **Abies** |
| 07_create10k | ~950ms | ~766ms | **Blazor** |
| 08_create1k-after1k_x2 | ~105ms | ~103ms | ~Equal |
| 09_clear1k | ~92ms | ~37ms | **Blazor** (2.5× gap) |

Abies wins approximately 5 of 9 benchmarks in the most favorable reading — not "vrijwel alle." The asterisk disclaimer does not save the directional claim. The 09_clear1k regression (2.5×) is an easy audience gotcha.

**Required fix:** Replace with honest framing — e.g., "is competitive with Blazor WASM and beats it on key creation benchmarks, while gaps remain on clear and swap."

---

### 🔴 BLOCKING 2: "51% dagelijks" attributed to two different surveys

**Slides:** `adoption` and `productivity`

The `adoption` slide attributes "51% dagelijks" to JetBrains AI Pulse January 2026. The `productivity` slide attributes the same figure to Stack Overflow 2025. Two different surveys, months apart — one citation is wrong. An audience member with either source can catch this live.

**Required fix:** Verify which survey produced the 51% daily figure and remove it from the other slide, or differentiate the figures if both surveys genuinely agree independently.

---

## Non-Blocking Findings — Verify Before Presenting (10)

1. **METR follow-up: "dezelfde devs, betere modellen"** — The February 2026 follow-up may have had partially different participants. Confirm cohort is identical before using "dezelfde."
2. **"Devs weigerden om aan de 'no AI' conditie mee te doen"** — Attributed to METR screen-recordings/study notes. Needs a sentence-level citation from the METR report; presented as a primary finding without sourcing.
3. **Claude Code "#1 most-loved in 8 maanden"** — The "most-loved" framing needs a named survey. If not Stack Overflow, name the source. The 8-month timeline requires confirming the exact Claude Code launch date (~Feb 2025) and the measurement point.
4. **Stack Overflow trust drop 70%+ → 29%** — Verify both figures use equivalent question phrasing ("any trust" vs. "some trust" vs. "high trust") across 2023 and 2025 surveys.
5. **Pragmatic Engineer March 2026: 95% weekly, 55% agents** — Very recent. Confirm this is from the final publication, not a preview or draft.
6. **Veracode: 45% failure, 2.74× vulnerabilities** — Name the specific report (e.g., "Veracode State of Software Security 2025") to give audience a verification path.
7. **DX Q4 2025: 135,000 devs, 3.6h/week, 60% more PRs** — Include report name and URL in speaker notes.
8. **Microsoft .NET runtime: 878 PRs, 68% merge rate, 0.6% revert** — Name the source. "38% → 69% with better preparation" is a specific claim requiring attribution.
9. **"VS+CA" abbreviation on tools slide** — Unexplained. Expand or annotate in speaker notes.
10. **METR July 2025 vs. February 2026 internal consistency** — Bridge the two studies with a sentence: they are separate studies with different designs, not a straight replication.

---

## Solid — No Corrections Needed

- Squad attribution to Brady Gaster with correct GitHub URL ✅
- METR July 2025 core finding (16 devs, 246 tasks, 19% slower) ✅
- General adoption trend table (SO 2023–2025 / JetBrains 2026) — internally consistent ✅
- Personal squad workflow narrative framed as personal experience, not universal claim ✅

---

## Status

🔴 **Blocking until both must-fix items are resolved.** The benchmark framing and the duplicate 51% citation must be corrected before this deck is presented publicly.
