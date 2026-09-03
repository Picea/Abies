---
name: decider-cutover-overrode-staged-plan
description: Historical — the Architect's staged Program-to-Decider convergence was overridden by a user directive for an immediate breaking cutover
metadata:
  type: project
---

On 2026-04-04 the Architect recommended making `Program` decider-shaped through
a *staged* bridge: decider semantics plus a compatibility adapter, cutting over
only after parity tests, docs/template migration and performance gates passed.
A user directive the same day superseded that: execute the full decider
migration immediately, allow breaking changes, remove all compatibility shims.
The cutover shipped.

**Why:** The staging recommendation was driven by public-API blast radius and
runtime coupling to `AutomatonRuntime`. The user judged the breakage acceptable
and preferred one honest break over a long compatibility tail. Compat shims also
let partial implementers satisfy the compiler while violating the decider
contract, which was the argument that ultimately won.

**How to apply:** This is history, not current architecture — do not propose
re-introducing compatibility shims for `Program` on the grounds that staging was
"the plan". Current shape (verified 2026-09-02) is
`ProgramCore<TModel, TArgument> : Decider<...>` with `new static abstract Decide`
and `IsTerminal`, split from `ProgramView<TModel>`, recombined by
`Program<TModel, TArgument>` and `WithView<...>` — a further evolution beyond
what either the staged plan or the cutover described. More usefully, treat this
as calibration: when risk-staging a breaking change, present the option of
taking the break now, because that is what was chosen here.

The decision trail is in the team decisions file under the 2026-04-04 entries
("Program contract should be decider-shaped", "Full decider migration —
breaking contract target", "Full decider migration — implementation complete").
Related: [[program-core-requires-explicit-decide]].
