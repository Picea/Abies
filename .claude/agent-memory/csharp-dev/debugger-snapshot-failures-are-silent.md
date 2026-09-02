---
name: debugger-snapshot-failures-are-silent
description: TryApplyDebuggerSnapshot swallows every deserialization failure and returns false, so a broken debugger shows as "UI does not update" with no error
metadata:
  type: feedback
---

`Runtime.TryApplyDebuggerSnapshot` returns `false` on every failure path and
logs nothing: bare `catch { return false; }` around both the `JsonElement` and
`string` branches, plus a silent `return default` when
`_debuggerModelJsonTypeInfo` is null. When it returns false, `Render()` is never
called, so there are no DOM patches and the UI simply does not change.

**Why:** This cost a full debugging session on 2026-03-29. The reported symptom
was "step-forward after import does nothing", and the obvious hypothesis — that
`step-forward` was not reaching `TryApplyDebuggerSnapshot` — was wrong. All nine
debugger message types (`jump-to-entry`, `step-forward`, `step-back`, `play`,
`pause`, `clear-timeline`, `get-timeline`, `export-session`, `import-session`)
unconditionally apply the snapshot after bridge execution, on both the browser
and server paths. The real fault was snapshot *content*: the model failed to
deserialize and the failure was eaten.

**How to apply:** When the debugger visibly does nothing, do not go looking for
a missing `ApplySnapshot` call — start from snapshot content and
serialization. The known content failure is an abstract discriminated-union root
serializing without a `$type` discriminator, which makes deserialization throw
on the abstract base; the fix is `[JsonPolymorphic]` + `[JsonDerivedType]` on
the DU root (this is now a team decision: "App polymorphic DU roots must declare
JsonPolymorphic metadata"). Also check that `_debuggerModelJsonTypeInfo` is
actually supplied, since without source-generated metadata the runtime declines
to deserialize at all for trim-safety.

Verified 2026-09-02, with one correction to the original note: serialization now
goes through source-generated `JsonTypeInfo`
(`GenerateModelSnapshot`/`DeserializeDebuggerModelSnapshot`), not
`JsonSerializer.Serialize(model)` with default options. The Conduit `Page` DU
carries `[JsonPolymorphic(TypeDiscriminatorPropertyName = "$page")]` and eight
`[JsonDerivedType]` registrations, and now lives in
`Picea.Abies.Conduit.App/Model.cs` (not `Picea.Abies.Conduit`, which is
domain-only). Test coverage was noted as missing at the time — see
`Picea.Abies.Tests/DebuggerSessionImportExportTests.cs` before assuming it still
is.
