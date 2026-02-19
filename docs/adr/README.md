# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records for the Abies framework.

ADRs document significant architectural decisions, their context, and consequences. They serve as a historical record for understanding why the framework is built the way it is.

## Index

| ID | Title | Status | Summary |
| --- | --- | --- | --- |
| [ADR-001](./ADR-001-mvu-architecture.md) | Model-View-Update Architecture | Accepted | Adopts MVU (The Elm Architecture) as the core pattern |
| [ADR-002](./ADR-002-pure-functional-programming.md) | Pure Functional Programming Style | Accepted | Mandates pure FP with immutable records |
| [ADR-003](./ADR-003-virtual-dom.md) | Virtual DOM Implementation | Accepted | Uses virtual DOM for declarative rendering |
| [ADR-004](./ADR-004-parser-combinators.md) | Parser Combinators for Routing | Accepted | Type-safe routing with parser combinators |
| [ADR-005](./ADR-005-webassembly-runtime.md) | WebAssembly Runtime | Accepted | Runs entirely in browser via .NET WASM |
| [ADR-006](./ADR-006-command-pattern.md) | Command Pattern for Side Effects | Accepted | Keeps Update pure; effects via Commands |
| [ADR-007](./ADR-007-subscriptions.md) | Subscription Model for External Events | Accepted | Declarative subscriptions for ongoing events |
| [ADR-008](./ADR-008-immutable-state.md) | Immutable State Management | Accepted | All state via immutable records |
| [ADR-009](./ADR-009-sum-types.md) | Sum Types for State Representation | Accepted | Discriminated unions for mutually exclusive states |
| [ADR-010](./ADR-010-option-type.md) | Option Type for Optional Values | Accepted | Explicit Option instead of null |
| [ADR-011](./ADR-011-javascript-interop.md) | JavaScript Interop Strategy | Accepted | JSImport/JSExport with thin JS layer |
| [ADR-012](./ADR-012-test-strategy.md) | Test Strategy | Accepted | Layered testing: unit, integration, E2E |
| [ADR-013](./ADR-013-opentelemetry.md) | OpenTelemetry Instrumentation | Accepted | OTEL for observability |
| [ADR-014](./ADR-014-compile-time-ids.md) | Compile-Time Unique ID Generation | Accepted | Source generator for stable element IDs |
| [ADR-015](./ADR-015-tracing-verbosity.md) | Tracing Verbosity Levels | Accepted | Configurable OTEL verbosity |
| [ADR-016](./ADR-016-keyed-dom-diffing.md) | Keyed DOM Diffing | Accepted | Key-based reconciliation for lists |
| [ADR-017](./ADR-017-dotnet-new-templates.md) | .NET New Templates | Accepted | Project templates for quick start |
| [ADR-018](./ADR-018-pr-lint-only-changed-files.md) | PR Lint Check Only Changed Files | Accepted | Lint only PR-changed files, not entire solution |
| [ADR-019](./ADR-019-trunk-based-development.md) | Trunk-Based Development | Accepted | Protected main branch with PR workflow |
| [ADR-020](./ADR-020-benchmark-quality-gates.md) | Benchmark Quality Gates | Accepted | Automated quality gates for performance benchmarks |
| [ADR-021](./ADR-021-html-validation-analyzers-over-typed-dsl.md) | HTML Validation via Roslyn Analyzers | Accepted | Analyzers over type-safe DSL for HTML correctness |

> **Note:** There are two files numbered ADR-005: [ADR-005-webassembly-runtime.md](./ADR-005-webassembly-runtime.md) (indexed above) and [ADR-005-security-scanning-sast-dast-sca.md](./ADR-005-security-scanning-sast-dast-sca.md) (security scanning). The security scanning ADR was created separately and retains its number for historical reasons.

## How to Use ADRs

### Reading ADRs

- Start with ADRs that relate to the code you're working on
- Each ADR explains the "why" behind a design decision
- Cross-references link related decisions

### Creating New ADRs

1. Copy `ADR-000-template.md` as a starting point
2. Assign the next sequential number
3. Fill in all sections
4. Update this index
5. Submit via pull request for review

### ADR Lifecycle

| Status | Meaning |
| --- | --- |
| Proposed | Under discussion, not yet decided |
| Accepted | Decision has been made and applies |
| Deprecated | No longer applies to new code |
| Superseded | Replaced by a newer ADR |

## Relationship Diagram

```text
┌─────────────────────────────────────────────────────────────────────┐
│                         ADR-001: MVU                                 │
│                    (Core Architecture)                               │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼
┌───────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ ADR-002: FP   │    │ ADR-003: VDOM   │    │ ADR-006: Cmds   │
│               │    │                 │    │                 │
└───────┬───────┘    └────────┬────────┘    └────────┬────────┘
        │                     │                      │
        ▼                     ▼                      ▼
┌───────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ ADR-008: Imm  │    │ ADR-011: Interop│    │ ADR-007: Subs   │
│ ADR-009: Sum  │    │ ADR-014: IDs    │    │                 │
│ ADR-010: Opt  │    │                 │    │                 │
└───────────────┘    └─────────────────┘    └─────────────────┘

┌───────────────┐    ┌─────────────────┐
│ ADR-004: Route│    │ ADR-005: WASM   │
│               │    │                 │
└───────────────┘    └─────────────────┘

┌───────────────┐    ┌─────────────────┐
│ ADR-012: Test │    │ ADR-013: OTEL   │
│               │    │                 │
└───────────────┘    └─────────────────┘
```

## References

- [Architectural Decision Records](https://adr.github.io/)
- [Michael Nygard's ADR Template](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [Elm Architecture](https://guide.elm-lang.org/architecture/)
