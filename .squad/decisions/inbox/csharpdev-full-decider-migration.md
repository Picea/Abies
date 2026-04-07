# C# Dev Decision Note — Full Decider Migration

Date: 2026-04-04  
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Scope Completed

Implemented the breaking migration to strict decider contracts across Abies runtime surface and Program contract usage.

### Core changes

- Removed Program-level compatibility defaults in `Picea.Abies/Program.cs`:
  - Removed default pass-through `Decide`
  - Removed default `IsTerminal`
  - Kept only strict static abstract decider members
- Made runtime command handling decider-native in `Picea.Abies/Runtime.cs`:
  - `Dispatch` now executes canonical decider flow:
    - `IsTerminal(state)` guard
    - `Decide(state, command)`
    - dispatch each decided event through transition pipeline
  - Added a narrow decision gate (`SemaphoreSlim`) so `IsTerminal` + `Decide` run atomically per command while command interpretation remains non-blocking for unrelated dispatches
  - Removed direct command->event dispatch behavior that bypassed `Decide`
- Updated decider contract references in docs/comments:
  - `docs/reference/runtime-internals.md`
  - `docs/api/program.md`
  - Presentation embedded interface snippets in `Picea.Abies.Presentation/Program.cs`

## Compatibility Paths Removed

- Program default shim behavior:
  - Removed implicit `Decide(state, command) => Ok([command])`
  - Removed implicit `IsTerminal(state) => false`
- Runtime command path that treated incoming message as event without decisioning:
  - Replaced direct `_core.Dispatch(message)` route with decider flow (`Decide` -> dispatch events)

## Validation Matrix Outcomes

### Passed

- `dotnet build Picea.Abies/Picea.Abies.csproj -c Debug`
- `dotnet build Picea.Abies.Server/Picea.Abies.Server.csproj -c Debug`
- `dotnet build Picea.Abies.Browser/Picea.Abies.Browser.csproj -c Debug`
- `dotnet build Picea.Abies.Conduit/Picea.Abies.Conduit.csproj -c Debug`
- `dotnet test --project Picea.Abies.Server.Tests/Picea.Abies.Server.Tests.csproj -c Debug -v minimal`
- `dotnet test --project Picea.Abies.Server.Kestrel.Tests/Picea.Abies.Server.Kestrel.Tests.csproj -c Debug -v minimal`
- `dotnet test --project Picea.Abies.Templates.Testing/Picea.Abies.Templates.Testing.csproj -c Debug -v minimal -- --maximum-parallel-tests 1`
- `dotnet test --project Picea.Abies.Templates.Testing.E2E/Picea.Abies.Templates.Testing.E2E.csproj -c Debug -v minimal`
- `dotnet test --project Picea.Abies.Tests/Picea.Abies.Tests.csproj -c Debug -v minimal`
- `dotnet test --project Picea.Abies.Conduit.Tests/Picea.Abies.Conduit.Tests.csproj -c Debug -v minimal`
- `dotnet build Picea.Abies.sln -c Debug`

### Notes

- A previously observed Conduit E2E failure (`DeleteArticle_AsAuthor_ShouldNavigateToHome`) came from earlier exploratory runs and is not treated as the validation result for this combined PR note.

## Follow-ups

- Add focused runtime tests that assert command rejection/terminal short-circuit paths (`Decide` returning `Err`, `IsTerminal == true`) to lock in decider-first dispatch semantics.
