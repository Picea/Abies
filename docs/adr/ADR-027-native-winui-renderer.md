# ADR-027: Native WinUI Renderer via Patch-Stream Interpretation

**Status:** Accepted
**Date:** 2026-07-28 (accepted 2026-08-07)
**Decision Makers:** Maurice Peters

## Context

Microsoft's experimental [microsoft-ui-reactor](https://github.com/microsoft/microsoft-ui-reactor) validates
declarative, diffed C# UI over native WinUI 3 controls — using React-style hooks and component-local state.
Abies already has a stricter foundation (pure MVU on the Picea Decider kernel, effects as data, event-sourced
replay) and two renderers (browser DOM via WASM, interactive server). The question was whether native WinUI
could become a third renderer without forking or reshaping the core.

Exploration established that the core's only rendering seam is
`public delegate void Apply(IReadOnlyList<Patch> patches)` (`Picea.Abies/Runtime.cs`), that patches carry
real `Element`/`Text` node objects (HTML strings appear only in the browser/server serialization path), and
that everything a renderer needs is public. See
[docs/research/native-winui-mvu.md](../research/native-winui-mvu.md) for the full study.

## Decision

1. **Render natively by interpreting the existing patch stream** — no changes to `Picea.Abies`. A new
   `Picea.Abies.Native` project provides:
   - a native element vocabulary (`StackPanel`, `TextBlock`, `Button`, `TextBox`, …) whose factories produce
     the existing `Element(Id, Tag, Attributes, Children)` shapes, so `Diff`/`Patch`/`Runtime` are reused
     unchanged;
   - native event handler factories (`OnClick`, `OnTextChanged`, …) carrying native event names and
     `WithData` factories over native event-data records (`Handler.Deserializer` stays null — the JSON path
     is never used);
   - a platform-neutral `PatchInterpreter<T>` over an `INativeBackend<T>` seam, testable headlessly.
     Child operations are append / replace / remove / move: insertions append and the diff restores
     ordering with a following `MoveChild`, so the backend needs no positional-insert operation.
     Every path that stops tracking a control detaches its native event subscriptions first —
     backends key their subscription bookkeeping by control instance, so skipping that retains dead
     controls and their closures for the process lifetime.
2. **Element-only trees**: the native vocabulary never emits `Text` or `RawHtml` nodes — text is always a
   property attribute (`Text`/`Content`). This structurally prevents the diff's HTML fallback paths
   (`ReplaceRaw`/`Render.Html`) from firing; the interpreter throws on the Text/Raw patch families as a
   tripwire. Native tags must never be named `Input`, `Source`, `Track`, `Base`, or `Link`
   (case-insensitive), which `HtmlSpec.VoidElements` would silently skip child-diffing for.
3. **`Picea.Abies.WinUI` codes against the WinUI 3 API surface** (`Microsoft.UI.Xaml`) and is built with the
   Uno.Sdk so the same code runs on Windows App SDK (real WinUI 3) and, via Uno Platform's Skia heads, on
   macOS/Linux/WebAssembly. Both heads are built in CI: `net10.0-desktop` on Linux and, guarded by
   `IsOSPlatform('Windows')`, `net10.0-windows10.0.26100` on `windows-latest`. It contains the `WinUIBackend` (control factory, string→native property parsing,
   event wiring with `IsApplying` echo suppression) and a one-line `Runtime.Run` bootstrap that marshals
   `Apply` to the `DispatcherQueue`.
4. **Vocabulary and renderer are separate projects** so shared app code references only `Picea.Abies.Native`
   and a future Avalonia/AppKit backend reuses the interpreter.

## Consequences

### Positive

- Zero core changes; browser and server renderers unaffected.
- Shared Model/Update/Commands across web, server, and native — demonstrated by
  `Picea.Abies.Counter.Native` delegating everything but `View` to the shared `CounterProgram`.
- The full loop is testable with no UI framework (`Picea.Abies.Native.Tests` FakeBackend), and the existing
  `TestHarness`, time-travel debugger core, and hot-reload seam apply to native programs as-is.
- Real native controls — layout, theming, accessibility come from WinUI itself rather than a re-implemented
  layout engine.

### Negative

- The vocabulary is deliberately small and string-encodes typed properties, so the backend parses
  at apply time rather than the compiler checking at call sites.
- Maintaining two heads means tooling run on one can produce code that breaks the other. This is not
  hypothetical: `dotnet format`'s IDE0002 rewrote `Panel.BackgroundProperty` to
  `FrameworkElement.BackgroundProperty` — correct for Uno, nonexistent in WinUI 3 — before the code
  was first committed. Access through the derived type is portable, and a project-level
  `.editorconfig` disables IDE0002 to keep it that way. The `windows-latest` CI job is what makes
  this class of divergence visible.
- ~~`View` is per-platform: a native program must delegate to a shared program class (boilerplate)~~ —
  resolved by [ADR-028](./ADR-028-program-core-view-split.md): a native app now supplies a
  `ProgramView` and pairs it with the shared core via `WithView`, with no forwarding members.
- The native vocabulary starts small (10 controls) and string-encodes typed properties; breadth, styling,
  and virtualized lists are roadmap work.
- Uno.Sdk enters the toolchain (`msbuild-sdks` pin in `global.json`), so every root `dotnet restore`
  resolves the Uno SDK and package graph. Verified to work on `ubuntu-latest` with no workload install
  and no vulnerable transitive packages; NuGet caching keeps it off the critical path.

### Neutral

- `Handler.Name` still reads `data-event-{EventName}` in native trees — it is only a diff key there.
- `Document.Head` and head patches are no-ops natively; `Document.Title` maps to `Window.Title`.

## Alternatives Considered

### Alternative 1: HTML→native mapping (reuse the Html DSL)

One `View` runs everywhere by mapping `div`/`button`/`input` onto native controls at apply time. Rejected:
the mapping is lossy (CSS, flow layout, raw HTML) and fights WinUI's layout model; this is where such
efforts historically stall.

### Alternative 2: Hooks/component model (Reactor's approach)

Rejected as contrary to Abies's architecture: component-local state defeats single-model determinism,
event-sourced replay, and the time-travel debugger.

### Alternative 3: Serialize patches (reuse RenderBatchWriter) into a native shell

Rejected: the binary wire format serializes subtrees to HTML strings; a native renderer wants the real node
objects, which `Apply` already provides in-process.

## Status of Implementation

Landed on `main` as four PRs: the platform-neutral vocabulary and interpreter (#321), the WinUI
backend and Uno desktop head (#322), the native Counter sample (#323), and this record (#324).
The Windows App SDK head and its `windows-latest` CI job followed in #326 — the first time the real
WinUI 3 target was compiled, which is what allows this ADR to describe the renderer as WinUI rather
than Uno Skia. Remaining work is tracked in
[the production-readiness plan](../investigations/winui-native-production-readiness.md).

## Related Decisions

- [ADR-001: MVU Architecture](./ADR-001-mvu-architecture.md)
- [ADR-003: Virtual DOM](./ADR-003-virtual-dom.md)
- [ADR-014: Compile-Time Unique IDs](./ADR-014-compile-time-unique-ids.md)
- [ADR-022: Picea Ecosystem Migration](./ADR-022-picea-ecosystem-migration.md)

## References

- [Research: A Native WinUI Renderer for Abies](../research/native-winui-mvu.md)
- [microsoft/microsoft-ui-reactor](https://github.com/microsoft/microsoft-ui-reactor)
- [Uno Platform 6.4 — .NET 10 support](https://platform.uno/blog/uno-platform-6-4/)
