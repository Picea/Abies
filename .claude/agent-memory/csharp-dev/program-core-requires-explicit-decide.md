---
name: program-core-requires-explicit-decide
description: Every ProgramCore implementer must declare Decide and IsTerminal itself — static interface defaults do not satisfy inherited static abstract members
metadata:
  type: feedback
---

Every concrete `ProgramCore<TModel, TArgument>` implementer must declare
`Decide` and `IsTerminal` explicitly. A static interface default in a base
interface does **not** satisfy a static abstract member inherited from
`Decider<...>`; the compiler reports CS0535 on the implementing type. For a
plain MVU program the correct bodies are pass-through decisioning
(`Result<Message[], Message>.Ok([message])`) and `IsTerminal => false`.

**Why:** Learned on 2026-04-04 during the decider migration. The default shims
that used to live in `Program.cs` were deliberately removed, because they let a
partial implementer compile while silently violating the decider contract — a
program with no real `Decide` looked correct and behaved as a no-op gate. The
explicit declaration is the point, not an accident of the type system.

**How to apply:** When adding a new program, or when a `Program`/`ProgramCore`
member changes, expect to touch *every* implementer at once — production
programs, templates under `Picea.Abies.Templates/templates/`, and the test
doubles in `Picea.Abies.Server.Tests` and `Picea.Abies.Server.Kestrel.Tests`.
Treat a CS0535 sweep as part of the change, not as follow-up work. Do not
"fix" it by reintroducing a default implementation on the interface.

Verified 2026-09-02: `Picea.Abies/Program.cs` declares
`new static abstract Result<Message[], Message> Decide(...)` and
`new static abstract bool IsTerminal(...)` on `ProgramCore`, with no defaults.
Related: [[decider-cutover-overrode-staged-plan]].
