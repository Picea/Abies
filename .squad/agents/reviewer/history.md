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
- 2026-03-26: Template counter UI now uses plain '-' and '+' labels with aria labels ('Decrease'/'Increase'); tests were updated to query accessible names where appropriate.
- 2026-03-26: Browser and browser-empty templates now include '.Host' projects with OTEL setup and OTLP proxy mapping; defaults tests assert host file presence plus 'app.MapOtlpProxy()' and '.AddConsoleExporter()'.
- 2026-03-26: TUnit filtering in this repo should use tree-node filters (or full runs); FullyQualifiedName filters result in zero-test runs.
- 2026-03-26: One template build-test failure observed was infrastructure-related (Nerdbank.GitVersioning CompareFiles EndOfStreamException), not a functional assertion failure in the reviewed requirements.
- 2026-03-27: Browser OTLP export fix is viable with protobuf exporter plus explicit export-on-end, but CDN dependency pinning must be complete (API + SDK + exporter) to avoid drift between partially pinned package versions.
- 2026-03-27: For InteractiveServer bootstrap reviews, verify every new dynamic import against the server package's actually served static asset paths; adding a relative import under '/_abies/' without adding the target asset or a serving test creates a silent no-op regression.

## Review Statistics
| Date | Scope | Verdict | 🔴 | ⚠️ | 💡 | Files |
|---|---|---|---|---|---|---|
| *None yet* | | | | | | |
