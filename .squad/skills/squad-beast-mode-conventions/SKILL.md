# Beast Mode × Squad Conventions

**Confidence:** high

## Pattern

This squad operates using Beast Mode 4.1 × Disney Creative Strategy integrated into Squad's multi-agent framework.

### Workflow
1. Architect runs Dreamer → Realist → Critic for significant work (pauses for user at each transition)
2. Lead triages and assigns implementation to specialists
3. Specialists execute (C# Dev, JS Dev) in parallel
4. Reviewer performs independent code review (fresh eyes, no prior design context)
5. Security Expert validates threat model and runs automated scans
6. Tech Writer ensures docs ship with code

### Key Conventions
- Principles enforcement: every deviation requires explicit user approval + documentation
- Functional DDD: immutable records, pure functions, Result/Option, state machines not flags, smart constructors with private type constructors
- Namespace-as-bounded-context: `Picea.Abies.{Context}.{Concept}`, depth over width, folder mirrors namespace
- No I-prefix on your own interfaces, no Async suffix
- TUnit only (no xUnit/NUnit/MSTest)
- Aspire AppHost is the only way to start the SUT for integration/E2E tests
- Full OTEL trace coverage on every functional flow
- Living threat model updated after every attack-surface change
- Security scanning (SAST/SCA/DAST/secrets) runs locally AND in CI

### Memory Mapping
| Beast Mode Concept | Squad Location |
|---|---|
| Session Journal | Agent `history.md` files |
| Decision Map | `.squad/decisions.md` |
| Pattern Library | `.squad/skills/` |
| Expert Rooms | Specialist agents or Architect's analysis |
| Code Reviewer's Log | `reviewer/history.md` |
| ADRs | `/docs/adr/*.md` |

## Learned From
- Initial squad setup and Beast Mode integration
