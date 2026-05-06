# Reviewer — History

## About This File
This file captures project-specific learnings from the Reviewer's code reviews. It grows as the Reviewer identifies recurring patterns, quality issues, and team conventions. Read this before every review.

## Recurring Quality Issues
*None yet — this section tracks patterns that keep showing up across reviews.*

## Implementation Drift Patterns
*None yet — this tracks cases where code diverged from the Architect's plan.*

## Style & Consistency Observations
*None yet — this tracks conventions that should be standardized across the squad.*

## Deferred Items
*None yet — items flagged but not blocking, to revisit later.*

## Learnings
- 2026-05-06: Temporary prerelease floor pinning is acceptable as a scoped compatibility bridge when (a) prerelease direct pins are limited to Glauca-coupled projects, (b) non-Glauca projects remain on stable Picea, (c) restore is verified green (no NU1605), and (d) migration docs include explicit rollback/exit criteria.
- 2026-05-06: Picea 1.0.0 migration can appear complete at direct package-reference level while still being non-shippable due transitive floor mismatches (Glauca requiring prerelease Picea). Reviewer must treat NU1605 downgrade errors as hard ship blockers and require either compatible transitive releases or scoped rollback.
- 2026-05-06: Migration work items are vulnerable to scope drift (CI policy changes plus large new testing infrastructure in the same changeset). Reviewer should enforce split PRs when unrelated concerns dilute release-risk assessment.
- 2026-05-06: New dependency additions in code-touching migration work must include explicit dependency-approval evidence per principles; missing Security Expert review trail is a merge blocker even when package choices are reasonable.
- 2026-04-15: Express presentation dry-run audit — two blocking issues found: (1) benchmark claim "verslaat Blazor WASM op vrijwel alle duration tests" not supported by benchmark data (Abies wins ~5/9, not ~8/9; loses clear1k by 2.5×); (2) "51% dagelijks" attributed to both JetBrains AI Pulse and Stack Overflow 2025 — citation conflict. Non-blocking: METR "dezelfde devs" claim needs verification; Claude Code "8 maanden" timeline and survey source needed; Veracode/DX figures need report names for speaker notes. The asterisk disclaimer does not fix a false directional claim.
- 2026-03-26: Template counter UI now uses plain '-' and '+' labels with aria labels ('Decrease'/'Increase'); tests were updated to query accessible names where appropriate.
- 2026-03-26: Browser and browser-empty templates now include '.Host' projects with OTEL setup and OTLP proxy mapping; defaults tests assert host file presence plus 'app.MapOtlpProxy()' and '.AddConsoleExporter()'.
- 2026-03-26: TUnit filtering in this repo should use tree-node filters (or full runs); FullyQualifiedName filters result in zero-test runs.
- 2026-03-26: One template build-test failure observed was infrastructure-related (Nerdbank.GitVersioning CompareFiles EndOfStreamException), not a functional assertion failure in the reviewed requirements.
- 2026-03-27: Browser OTLP export fix is viable with protobuf exporter plus explicit export-on-end, but CDN dependency pinning must be complete (API + SDK + exporter) to avoid drift between partially pinned package versions.
- 2026-03-27: For InteractiveServer bootstrap reviews, verify every new dynamic import against the server package's actually served static asset paths; adding a relative import under '/_abies/' without adding the target asset or a serving test creates a silent no-op regression.

## Review Statistics
| Date | Scope | Verdict | 🔴 | ⚠️ | 💡 | Files |
|---|---|---|---|---|---|---|
| 2026-05-06 | Picea 1.0.0 migration (working tree) | 🔴 Changes Requested | 3 | 2 | 0 | 31 |
| *None yet* | | | | | | |
