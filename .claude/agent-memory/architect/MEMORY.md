# Architect memory

- [Runtime seams anchor replay gating](runtime-seams-anchor-replay-gating.md) — the four `Runtime.cs` seams any effect-interception design must use.
- [Debugger domain stays in core](debugger-domain-stays-in-core.md) — debugger state lives in `Picea.Abies`; JS is adapter-only, enforced by contract tests.
- [Decider cutover overrode staged plan](decider-cutover-overrode-staged-plan.md) — historical: user directive replaced staged convergence with an immediate breaking cutover.
- [ADRs are inside Track A's reading](adrs-are-inside-track-a-reading.md) — `docs/adr/**` is not denied to Track A; grep it for the pass subject before writing the scope.
- [Undo/redo is `WithHistory`](undo-redo-withhistory-decision.md) — the decision and its reopening triggers; ADR-030 is still an unwritten step-13 deliverable.
- [The user takes the typed refusal over the silence](user-chooses-typed-refusal-over-silence.md) — every gate, even when it costs memory or overturns a settled rule.
- [Invariants must name their state space](invariants-must-name-their-state-space.md) — "application state" in an INV-n leaks; and cite the scope by id, not by line.
