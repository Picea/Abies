# UI/UX Expert — History

## About This File
UX patterns, accessibility decisions, design system tokens, and usability findings. Read this before every session.

## Design System Tokens
*None yet — spacing scale, color palette, typography scale tracked here.*

## UX Patterns Established
*None yet — component behaviors, interaction conventions, layout patterns.*

## Accessibility Audit Log
| Date | Scope | Issues Found | Resolved | WCAG Level |
|---|---|---|---|---|
| *None yet* | | | | |

## Error Message Templates
*None yet — standardized error message formats and examples.*

## API DX Patterns
*None yet — endpoint naming conventions, error response formats, pagination defaults.*

## Usability Issues
| Date | Issue | Severity | Resolution |
|---|---|---|---|
| *None yet* | | | |

## Platform Conventions
*None yet — adopted conventions and intentional deviations with rationale.*

## Learnings

### 2026-04-15 — Express Deck Dry-Run

**Context**: 19-slide, ~30min conference talk (Dutch) on AI-assisted coding. Target: developer audience at a hands-on workshop day.

**Key UX findings:**

- **Data parade fatigue is the primary risk.** Slides 2–6 (Deel 1) present five consecutive stat-heavy screens. Structurally sound (positive→nuanced→contradicted→emotional pivot) but requires strong pacing from speaker. If read verbatim, audience drifts by slide 3.
- **Two slides fail for projection:** `tools` (markdown table) and `trust` (ASCII bar charts in code block). Both rely on monospace alignment that breaks on conference projectors. These are the highest-priority visual interventions.
- **The `tools` slide is the most overloaded slide in the deck.** Three product lines with 4–5 metrics each, plus a 5-column table. Needs to be split or replaced with a real chart.
- **The self-deprecating outro works — conditionally.** "Deze talk had eigenlijk grafieken nodig. Zoals jullie hebben gemerkt :)" is endearing if delivered with confidence; undermining if apologetic. The live demo segue is structurally clever.
- **"Begin bij de spec, niet bij de code"** is the talk's strongest thesis line. It appears only at the end. Echoing in the intro would unify the talk's arc.
- **Best slide in the deck:** `metr-followup`. The "mijn hoofd ontploft als ik het op de oude manier moet doen" anecdote is the moment the audience leans in hardest. Vivid, human, breaks the stats pattern.
- **Weakest slide:** `picea-abies`. Two technologies, multiple technical claims, asterisked disclaimer — too much for a synthesis slide at position 18.
- **The SlideKind taxonomy is architecturally correct** but only adds value if rendering visually differentiates Intro/Outro from Concept slides.
- **Top 3 visual interventions:** (1) trust/adoption divergence chart, (2) adoption trend line chart, (3) productivity-nuance split comparison. Building the trust chart live during the demo is the ideal payoff.
