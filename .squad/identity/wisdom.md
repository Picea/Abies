last_updated: 2026-03-20T13:20:00.000Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** Treat VS Code terminal commands as fragile; prefer atomic commands with log redirection over complex pipes/chains.
**Context:** Long-running commands, CI diagnostics, benchmark runs, and any workflow where partial output can kill producer processes.

**Pattern:** Capture benchmark power state before measuring and keep A/B comparisons in one session.
**Context:** Performance investigations on macOS laptops where power/thermal behavior can distort benchmark outcomes.

**Pattern:** Validate performance changes with js-framework-benchmark before concluding improvements.
**Context:** When micro-benchmarks and real-world benchmark results diverge.

**Pattern:** Run `dotnet format --verify-no-changes` before opening or updating a PR; use scoped `--include` fixes for touched files.
**Context:** PR prep and CI hardening to avoid unrelated formatting churn.

**Pattern:** PRs merge faster when title, template sections, and test evidence are complete before requesting review.
**Context:** Review readiness checks by lead/reviewer and GitHub CI gating.

**Pattern:** When `.github/instructions/*` changes, sync squad-derived memory with `.squad/templates/instruction-sync-checklist.md` instead of updating ad hoc.
**Context:** Keeping `.squad/decisions.md`, `.squad/identity/wisdom.md`, agent histories, and impacted charters aligned with canonical repo instructions.
