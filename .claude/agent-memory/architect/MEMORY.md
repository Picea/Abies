# Architect memory

- [Runtime seams anchor replay gating](runtime-seams-anchor-replay-gating.md) — the four `Runtime.cs` seams any effect-interception design must use.
- [Debugger domain stays in core](debugger-domain-stays-in-core.md) — debugger state lives in `Picea.Abies`; JS is adapter-only, enforced by contract tests.
- [Decider cutover overrode staged plan](decider-cutover-overrode-staged-plan.md) — historical: user directive replaced staged convergence with an immediate breaking cutover.
