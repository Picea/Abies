# Architect Migration Target — Full Decider Cutover (Breaking)

Date: 2026-04-04  
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Migration Target (Implementation Contract)

### 1) Runtime engine contract (decider-native)

- Abies runtime must be decider-native end-to-end. Runtime generic constraints are decider constraints, not automaton-compatibility shims.
- `Runtime<TProgram, TModel, TArgument>` and all hosting entry points (`Browser.Runtime.Run`, `Server.Session.Start`, `Server.Kestrel.MapAbies`, `Server.Page.Render`) must compile against a decider-shaped program contract.
- Runtime message flow is canonical decider flow: `Decide(state, command) -> events`, `Transition(state, event) -> (state, effect)`, `Interpret(effect) -> commands/messages`.
- Remove reliance on compatibility behavior that bypasses decider flow (for example route pre-processing via direct `Transition` invocation).
- Public/internal code and docs must stop describing runtime as `AutomatonRuntime`-first and instead describe decider-native runtime semantics.

### 2) Program interface shape (remove compatibility shims)

- `Program<TModel, TArgument>` remains the app contract but is strict decider shape.
- Program contract keeps only app-specific surface beyond decider (`View`, `Subscriptions`).
- Remove compatibility/default shim members from Program interface:
  - Remove static default pass-through `Decide` implementation.
  - Remove static default `IsTerminal` implementation.
- Consequence: every concrete Program implementer explicitly defines `Decide` and `IsTerminal`.

### 3) Required program/app changes (Conduit + other apps + templates + tests)

- Conduit app program (`Picea.Abies.Conduit.App.ConduitProgram`) must satisfy strict decider contract explicitly and remain source of truth for Conduit UI state transitions.
- Conduit domain deciders (`Domain/Article`, `Domain/User`) remain decider-native; no compatibility wrappers are added around them.
- All Program implementers across repository must explicitly satisfy decider members and compile without fallback defaults, including:
  - Conduit app, Counter, SubscriptionsDemo, Presentation, UI Demo, Benchmark app.
  - Browser debugger UI program.
  - Server test programs and runtime test programs.
  - Template-generated programs (`abies-browser`, `abies-browser-empty`, `abies-server`).
- Template outputs and template tests must validate generated programs compile and run with strict decider contract (no hidden defaults).
- Runtime and server rendering/routing tests must assert decider flow invariants (decision before transition, explicit terminal semantics where relevant).

### 4) Required removal of temporary compatibility behavior

- Remove staged-migration compatibility posture from implementation surface:
  - No Program-level static default shims for decider members.
  - No runtime generic constraints that depend on pre-decider compatibility assumptions.
  - No code path that exists only to keep old Program contract behavior alive.
- Remove stale staged-convergence wording from architecture/runtime docs where contract is now final.
- Keep only one contract path: decider-native Program.

## Acceptance Criteria

1. Core runtime and all hosts compile with strict decider Program contract and no Program-level default decider shims.
2. Every Program implementer in repository (apps, templates, tests, debugger UI) explicitly declares `Decide` and `IsTerminal` and compiles.
3. No runtime path bypasses decider decision/evolution semantics for app flow initialization/routing.
4. Template-generated projects (`abies-browser`, `abies-browser-empty`, `abies-server`) build and publish with decider-native contract.
5. Conduit app and Conduit user journeys remain green under existing integration/E2E suites.
6. Runtime/internal documentation is updated to describe decider-native engine contract (not staged compatibility).

## Minimal Validation Matrix

| Scope | Validation | Pass Condition |
|---|---|---|
| Core contract | `dotnet build Picea.Abies/Picea.Abies.csproj -c Debug` | Success, no Program/Decider contract errors |
| Host adapters | `dotnet build Picea.Abies.Server/Picea.Abies.Server.csproj -c Debug` and `dotnet build Picea.Abies.Browser/Picea.Abies.Browser.csproj -c Debug` | Success |
| Conduit | `dotnet build Picea.Abies.Conduit/Picea.Abies.Conduit.csproj -c Debug` | Success |
| Templates smoke | `dotnet test --project Picea.Abies.Templates.Testing/Picea.Abies.Templates.Testing.csproj -c Debug -v minimal` | Success |
| Templates E2E | `dotnet test --project Picea.Abies.Templates.Testing.E2E/Picea.Abies.Templates.Testing.E2E.csproj -c Debug -v minimal` | Success |
| Runtime/server tests | `dotnet test --project Picea.Abies.Server.Tests/Picea.Abies.Server.Tests.csproj -c Debug -v minimal` and `dotnet test --project Picea.Abies.Server.Kestrel.Tests/Picea.Abies.Server.Kestrel.Tests.csproj -c Debug -v minimal` | Success |
| Conduit tests | `dotnet test --project Picea.Abies.Conduit.Tests/Picea.Abies.Conduit.Tests.csproj -c Debug -v minimal` and `dotnet test --project Picea.Abies.Conduit.Testing.E2E/Picea.Abies.Conduit.Testing.E2E.csproj -c Debug -v minimal` | Success |
| Regression guard | `dotnet test --project Picea.Abies.Tests/Picea.Abies.Tests.csproj -c Debug -v minimal` | Success, no runtime replay/contract regressions |
