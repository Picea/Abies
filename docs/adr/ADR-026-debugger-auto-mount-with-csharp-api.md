# ADR-026: Debugger Auto-Mount with C# API

**Status:** Accepted  
**Date:** 2026-03-24  
**Decision Makers:** Maurice Peters  
**Related:** ADR-025 (Debugger Boundary Contract), ADR-005 (WebAssembly), ADR-011 (JavaScript Interop)
## Context

The Abies Time Travel Debugger (from ADR-025) provides a complete trace of the MVU loop: every message, transition, and render operation. However, enabling the debugger required manual setup:
1. **Developer creates the mount div** in `index.html`:
   ```html
   <div id="abies-debugger-timeline"></div>
   ```
2. **Developer manually mounts the UI** via JavaScript

This approach worked, but added friction:
- New developers had to know about the mount step
- It wasn't obvious how to disable the debugger in Release builds
- Manual mounting meant the debugger didn't "just work" out of the box

## Decision

**The debugger is auto-mounted during browser runtime startup and configured through a C# API:**

```csharp
// In Program.cs (before Runtime.Run):
#if DEBUG
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = true });
// Or:
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });  // Disable explicitly
#endif
```
### Design Rationale

1. **Auto-mount is the default** — Most developers want to use the debugger when available. Defaulting to enabled matches developer expectations.
2. **Explicit opt-out over implicit opt-in** — An escape hatch (`Enabled = false`) lets developers disable it when needed (e.g., in shared CI environments, or when the UI is unwanted).
3. **C# configuration API** — Keeps all app setup in one place (Program.cs) instead of split between C# and HTML.
4. **Compile-time stripping in Release** — The entire debugger infrastructure lives inside `#if DEBUG` blocks. Release builds have **zero bytes** of debugger code — no footprint, no runtime cost.
5. **C#-first configuration with browser runtime auto-mount** — On current main, the automatic mount is implemented in `Picea.Abies.Browser.Runtime` and the debug-only `debugger.js` module. The public configuration surface remains the C# API, even though the mount itself happens on the browser side.
### API Shape

```csharp
namespace Picea.Abies.Debugger;

/// <summary>
/// Configuration for the Abies Time Travel Debugger.
/// </summary>
public record DebuggerOptions
{
    /// <summary>
   /// Enable or disable the debugger UI.
   /// Defaults to true. In Release builds, the debugger is compiled out so this has no effect.
    /// </summary>
    public bool Enabled { get; init; } = true;
}
```

```csharp
namespace Picea.Abies.Debugger;

public static class DebuggerConfiguration
{
   public static DebuggerOptions Default { get; }

   public static void ConfigureDebugger(DebuggerOptions? options);
}
```

### Implementation Strategy

1. **Mount point injection** happens in `Picea.Abies.Browser/Runtime.cs`:
   - After the core MVU loop is initialized
   - Check: `if (DebuggerConfiguration.Default.Enabled)` inside `#if DEBUG`
   - Call `runtime.UseDebugger()` and `Interop.MountDebugger()`
   - Idempotent: the JS side returns early if the mount point already exists

2. **JavaScript side** (`debugger.js`):
   - The browser runtime imports the debug-only module and calls `mountDebugger()`
   - `mountDebugger()` creates `#abies-debugger-timeline` when needed and then initializes the adapter wiring
   - The function appends the mount point to `document.body`

3. **Compile-time stripping**:
   - The `#if DEBUG` blocks in `Runtime.cs` ensure no debugger code in Release builds
   - The `.csproj` file already has conditions to exclude `debugger.js` from Release publishes
4. **Idempotency**:
   - Multiple browser-runtime startups do not create duplicate mount points
   - JavaScript checks for existing `#abies-debugger-timeline` before injecting

### Alternatives Considered

#### A. Keep Manual Mount (Rejected)

**Pros:** No magic, explicit control  
**Cons:** Friction for new developers, inconsistent setup experience, easy to forget

#### B. Auto-Mount but Opt-In (Rejected)

**Pros:** Conservative, developers must explicitly enable  
**Cons:** Defeats the purpose — reduced friction if developers have to think about it

#### C. UI Environment Variable (Rejected)

**Pros:** Can be toggled at runtime without code changes  
**Cons:** Not discoverable, inconsistent with .NET conventions (use config/options), harder to test

#### D. Global Flag in `window.__ABIES__` (Rejected)

**Pros:** Works without C# changes  
**Cons:** Bypasses type safety and IDE support, not idiomatic .NET

### Trade-Offs

| Aspect | Choice | Cost |
|--------|--------|------|
| **Enabled by default** | Yes | Requires explicit `Enabled = false` to opt out, may surprise users who don't want the overhead (negligible in Debug builds) |
| **C# configuration** | Yes | Adds ~10 lines of C# code per template (trivial) |
| **Auto-mount vs manual** | Auto-mount in browser runtime | Current implementation is narrower than a cross-runtime abstraction, but matches the shipped code path |
| **Release stripping** | Via `#if DEBUG` | Requires discipline in codebase — mitigated by code review and static analysis |

### Future Extensibility

The `DebuggerOptions` record can be extended with future fields:

```csharp
public record DebuggerOptions
{
    public bool Enabled { get; init; } = true;
    public int? TimelineCapacity { get; init; }  // Future: max events to retain
    public bool AutoClear { get; init; }  // Future: clear on navigate
}
```

### Release Impact

- **Release builds**: Zero bytes (all code stripped via `#if DEBUG`)
- **Debug builds**: ~50 bytes (mount point div + comment)
- **Runtime cost**: Negligible (one-time injection at startup)
- **Browser memory**: The timeline grows with usage (bounded by browser memory, cleared on hard reload)
### Security Considerations

The debugger exposes **internal MVU state** (messages, model state, transitions). In Debug builds, this is acceptable because:
1. Debug builds are not deployed to production
2. The debugger UI runs only in the browser (not exposed to network)
3. Developers can disable it explicitly with `Enabled = false`

Release builds strip the debugger entirely, so there is no security exposure.

## See Also

- [ADR-025: Debugger Boundary Contract](ADR-025-debugger-boundary-contract.md) — Interface and semantics of debugger integration
- [docs/guides/devtools.md](../guides/devtools.md) — User guide: Using the debugger
- [docs/guides/debugging.md](../guides/debugging.md) — Debugging strategies for MVU applications
## Implementation Checklist

- [x] Create `DebuggerOptions` record
- [x] Add `ConfigureDebugger()` API method
- [x] Store options via `DebuggerConfiguration`
- [x] Inject mount point in `Picea.Abies.Browser/Runtime.cs` (Debug builds only)
- [x] Update `debugger.js` with idempotent mount logic
- [x] Update `devtools.md` (remove manual mount step)
- [x] Update template READMEs
- [x] Add CHANGELOG entry