# Orchestration Log — UX Dev Dry Run (Audience Journey & Visual Design)

**Date:** 2026-04-28  
**Agent:** UX Expert  
**Session type:** Express presentation audience journey and visual/design review  
**Requested by:** Maurice  

---

## Task Scope

Audience journey, visual design, and slide density review of `_expressSlides` (19 slides, ~30 minutes, Dutch, developer conference audience).

---

## Critical Friction Points (3)

### 1. `tools` slide (slide 3) — projection-breaking density

Current state: 3 tool-line bullets with 4–5 metrics each, followed by a 5-column markdown table. On a conference projector, markdown table alignment breaks without monospace rendering at controlled width. Audience reads wall-of-text and disengages before the data lands.

**Action:** Replace the markdown table with a visual comparison chart rendered by the app, or split into two slides — "Tool highlights" (3 simplified bullets) + "Adoption over time" (chart only).

### 2. `trust` slide (slide 7) — highest-impact slide, lowest-fidelity rendering

The usage/trust divergence (84% use, 29% trust) is the single most dramatic data point in the talk. The ASCII bar charts in a code block will not render well projected and break the visual register.

**Action:** This is the primary candidate for the live-demo chart. Build a real rendered chart in the demo segment. The ASCII version gives the audience the before; the rendered version is the payoff.

### 3. `picea-abies` slide (slide 18) — new complexity too late

19 slides in, the audience is in synthesis mode. Full technical bullet lists for Picea and Abies at this stage exceed what the audience can absorb. The "shameless plug" label is honest but doesn't reduce the density.

**Action:** Reduce to 3 lines max. The slide's job is brand awareness and curiosity, not technical onboarding.

Suggested simplification:
- "Picea: pure Mealy machine kernel. Decider → Result. No exceptions."
- "Abies: MVU on top. C# all the way down. Competitive with Blazor on creation benchmarks."
- "Built with the squad. Over the last few months."

---

## Top 3 Visual Opportunities

### 1. Trust/usage divergence chart — primary demo payoff

The `trust` slide data (84% use, 29% trust) is the strongest visual bet in the deck. This is where the talk's central tension becomes visible. Build it as a live interactive chart with the Abies stack during the demo. The contrast between the ASCII version and the rendered version IS the demo narrative.

### 2. Adoption trend table as chart

The adoption trend across SO 2023–2025 and JetBrains 2026 tells a clean time-series story. A rendered line or bar chart would communicate the acceleration pattern faster than the current markdown table.

### 3. Deel 1 → Deel 2 section break card

A minimal visual break between data-heavy Deel 1 and Deel 2 — even a section title card with no body — resets the audience's mental register. Currently the transition is invisible and the statistics pattern continues without signaling that the frame has changed.

---

## Energy Curve Assessment

**Natural "lean in" moments:**
- Slide 6 (`metr-followup`) — the Uber anecdote
- Slide 9 (`common-cause`) — the rhetorical pivot
- Slide 13 (`evolution`) — personal story begins
- Slide 19 (`today`) — demo setup, audience agency returns

**"Drift" risk moments:**
- Slide 3 (`tools`) — table + metric overload
- Slide 17 (`lessons`) — 6 lesson bullets after 4 workflow slides
- Slide 18 (`picea-abies`) — introduces new complexity late

The CTA "Begin bij de spec, niet bij de code" should appear in the intro as the through-line premise, not only on slide 19.

---

## Slide Density Reference

Slides exceeding 5–6 meaningful body lines for projection:

| Slide | Current items | Projection risk |
|---|---|---|
| `tools` | 3 bullets + 5-row table | Critical — table breaks |
| `objections` | 3 headers × 2–3 subbullets | High |
| `common-cause` | 2 lists × 5 items | High |
| `agent-landscape` | 3 tiers × 4–6 tool names | Medium |
| `squad-what` | 2 paragraphs + code block | Medium (intentional) |
| `picea-abies` | 2 technologies × 3–4 bullets + disclaimer | Critical |

---

## Verdict

**The talk lands despite the slides.** The narrative structure, the speaker's credibility arc, and the thematic anchors are strong. The friction is in projection-hostile density and one missed visual payoff opportunity (the trust chart). Fix the tools slide density and build the trust chart as the demo centerpiece — the rest follows.
