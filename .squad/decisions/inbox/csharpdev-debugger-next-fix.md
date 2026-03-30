# Decision: Annotate abstract DU roots with [JsonPolymorphic] for debugger snapshot round-trip

**Date**: 2026-03-29  
**Author**: C# Dev agent  
**Status**: Implemented

## Context

The time-travel debugger serializes model snapshots to JSON via `GenerateModelSnapshot` (default `JsonSerializer.Serialize`). When a session is imported, each timeline entry's snapshot is stored as the JSON string from `ModelSnapshotPreview`. On step-forward (or any navigation), `TryApplyDebuggerSnapshot` attempts `JsonSerializer.Deserialize<TModel>(json)`.

For any model whose root or nested fields contain an **abstract polymorphic type** (e.g. a discriminated union), the default serializer emits the concrete type's properties but **no `$type` discriminator**. Deserialization of that field throws `NotSupportedException: Deserialization of abstract types is not supported`, which the catch block silently swallows — returning `false`, skipping `Render()`, producing zero DOM patches, and leaving the UI unchanged.

In the Conduit app the offender is `Page` in `Picea.Abies.Conduit.App/Model.cs`:

```csharp
// Before fix — abstract with no type info emitted to JSON
public abstract record Page { ... }
```

## Decision

Annotate every abstract DU root that participates in debugger snapshot serialization with `[JsonPolymorphic]` and one `[JsonDerivedType]` per concrete variant. This ensures:

1. `JsonSerializer.Serialize(model)` emits `"$page": "Home"` (or the chosen discriminator name) alongside each page's properties.
2. `JsonSerializer.Deserialize<Model>(json)` reads that field, picks the correct concrete type, and succeeds.

No changes to the runtime or framework are required — the standard .NET 7+ attribute mechanism handles it automatically.

## Rejected alternatives

- **Custom `JsonSerializerOptions` via `TProgram.DebuggerSnapshotSerializerOptions`**: Would keep domain types clean but requires every app that uses the debugger to also supply options. More boilerplate, no semantic benefit.
- **Replay-based snapshot reconstruction**: Requires messages to also be deserializable (same polymorphism problem) and introduces significant runtime complexity.
- **Store typed model alongside JSON in export**: Export format would grow large and versioning becomes complex.

## Files changed

| File | Change |
|---|---|
| `Picea.Abies.Conduit.App/Model.cs` | Added `[JsonPolymorphic]` + 8× `[JsonDerivedType]` to `Page` |
| `Picea.Abies.Tests/DebuggerRuntimeReplayApplicationTests.cs` | Added `ImportedSession_StepForward_AppliesSnapshotToRenderedDocument` and `ImportedSession_StepForward_WithPolymorphicPage_AppliesSnapshotToRenderedDocument` tests |

## Rule for future models

Any `abstract record` that is a field of an MVU model **must** carry `[JsonPolymorphic]` + `[JsonDerivedType]` for every concrete subtype. This rule applies to the application layer (`*.App`, `*.Wasm`, etc.) — domain types in `Domain/` stay annotation-free and are mapped at the application boundary.
