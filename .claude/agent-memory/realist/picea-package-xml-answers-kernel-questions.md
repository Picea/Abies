---
name: picea-package-xml-answers-kernel-questions
description: Where to read the Picea kernel's contract without decompiling — the shipped XML doc file in the NuGet cache
metadata:
  type: reference
---

`Picea` is an external NuGet package (`Picea.Abies.csproj` → `PackageReference Include="Picea"`),
so `AutomatonRuntime`, `Decider` and `Automaton` are not readable as source in this checkout.
Design passes keep hitting questions about the kernel's contract and reasoning around them.

They do not need to. The package ships a full XML documentation file:

`~/.nuget/packages/picea/<version>/lib/net10.0/Picea.xml`

It documents every type and member — `Automaton`, `Decider`, `AutomatonRuntime` (`State`,
`Events`, `Dispatch`, `InterpretEffect`, `Reset`, `MaxFeedbackDepth`), `DecidingRuntime`,
`Observer`, `Interpreter`, `EventLog`, `Result`/`Option`/`Unit`, and the `trackEvents` and
`threadSafe` parameters.

**Why:** on the `undo-redo` pass (2026-09-06) two "unreachable because Picea is a package"
unknowns had been carried through three phases; both were answered in ten minutes from this
file. Read it before declaring anything about the kernel unknowable.

**How to apply:** the XML establishes *contract*, not *accessibility* — csc emits doc entries
for documented members regardless of visibility (the `AutomatonRuntime` constructor entry
says outright that it is internal). If a plan depends on calling something, confirm with a
compile probe, not with the XML alone. Check `.nuget/packages/picea/` for which versions are
present and match the one the csproj actually references.

Related: [[namespace-plan]], [[higher-order-program-step-shape]].
