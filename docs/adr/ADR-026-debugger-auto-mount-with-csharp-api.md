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

**The debugger is now auto-mounted at runtime with a C# configuration API:**

```csharp
// In Program.cs (before Runtime.Run):
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = true });
// Or:
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });  // Disable explicitly
```

### Design Rationale

1. **Auto-mount is the default** — Most developers want to use the debugger when available. Defaulting to enabled matches developer expectations.
2. **Explicit opt-out over implicit opt-in** — An escape hatch (`Enabled = false`) lets developers disable it when needed (e.g., in shared CI environments, or when the UI is unwanted).
3. **C# configuration API** — Keeps all app setup in one place (Program.cs) instead of split between C# and HTML.
4. **Compile-time stripping in Release** — The entire debugger infrastructure lives inside `#if DEBUG` blocks. Release builds have **zero bytes** of debugger code — no footprint, no runtime cost.
5. **Both WASM and Server modes** — The auto-mount happens in `Runtime.cs`, which is used by both `Picea.Abies.Browser.Runtime` (WASM) and `Picea.Abies.Server.Runtime` (Server). The debugger works in both modes without special handling.
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
    /// Defaults to true in Debug builds, false in Release builds.
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

1. **Mount point injection** happens in `Runtime.cs` (platform-agnostic MVU runtime):
   - After the core MVU loop is initialized
   - Check: `if (debuggerOptions.Enabled && DEBUG)`
   - Create the mount div, inject it into the DOM (via JavaScript interop)
   - Idempotent: check if already present before injecting

2. **JavaScript side** (`debugger.js`):
   - When injected, the mount div includes a transparency comment/attribute
   - Existing `debugger.js` timeline UI mounts into the provided div

3. **Compile-time stripping**:
   - The `#if DEBUG` blocks in `Runtime.cs` ensure no debugger code in Release builds
   - The `.csproj` file already has conditions to exclude `debugger.js` from Release publishes
4. **Idempotency**:
   - Multiple calls to `ConfigureDebugger()` (or re-importing the module) don't create duplicate mount points
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
| **Auto-mount vs manual** | Auto-mount | Sacrifices explicitness for convenience — justified because Release builds strip it anyway |
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

- [ ] Create `DebuggerOptions` record
- [ ] Add `ConfigureDebugger()` API method
- [ ] Store options in `Program`
- [ ] Inject mount point in `Runtime.cs` (Debug builds only)
- [ ] Update `debugger.js` with idempotent mount logic
- [ ] Update `devtools.md` (remove manual mount step)
- [ ] Update template READMEs
- [ ] Add CHANGELOG entry
- [ ] Update all three templates (`abies-browser`, `abies-browser-empty`, `abies-server`)