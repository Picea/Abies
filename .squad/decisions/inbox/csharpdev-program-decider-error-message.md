# C# Dev Decision Note — Program Decider Err as Message

Date: 2026-04-04  
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Decision

For MVU Program deciders, use `Result<Message[], Message>` instead of `Result<Message[], Unit>` so command validation failures are represented as true `Err` values that can still flow through app transitions.

## Why

- `Unit` as error type forced validation failures into synthetic success events (`Ok(ApiError)`), which blurred decider semantics.
- The runtime already owns message dispatch; routing `decision.Error` through transition keeps user-visible behavior intact while restoring explicit command rejection semantics.

## Applied Changes

- Updated Program contract in `Picea.Abies/Program.cs` to `Decider<..., Command, Message, ...>` and `Decide : Result<Message[], Message>`.
- Updated runtime dispatch in `Picea.Abies/Runtime.cs` to dispatch `decision.Error` when `decision.IsErr`.
- Migrated Conduit validation in `Picea.Abies.Conduit.App/Conduit.cs` from `Ok(ApiError)` to `Err(ApiError)`.
- Updated impacted program implementations/templates/tests to the new `Result<Message[], Message>` signature.
- Added focused tests:
  - Runtime error dispatch: `Runtime_DispatchesDecisionErr_ThroughProgramMessageFlow`
  - Conduit decider Err-path checks: `ConduitDecideTests`

## Compatibility

User-visible behavior remains consistent in Conduit because `ApiError` still reaches transition handling (`HandleApiError`) through runtime dispatch.

## Validation

- `runTests` on `Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs`: pass
- `runTests` on Conduit WASM test files (including new `ConduitDecideTests.cs`): pass
