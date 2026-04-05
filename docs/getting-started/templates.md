# Project Templates

Abies provides `dotnet new` templates for quickly scaffolding new applications. Templates include the Abies design system CSS, a working counter example, and all required configuration.

## Installation

```bash
dotnet new install Picea.Abies.Templates::1.0.*-*
```

## Available Templates

| Template | Short Name | Description |
| --- | --- | --- |
| Abies Browser App | `abies-browser` | Client-side WebAssembly application |
| Abies Browser (Empty) | `abies-browser-empty` | Minimal WASM app with no example code |
| Abies Server App | `abies-server` | Interactive server-rendered application (WebSocket patches) |

## Abies Browser App (`abies-browser`)

Creates a client-side WebAssembly application that runs entirely in the browser.

```bash
dotnet new abies-browser -n MyApp
cd MyApp
dotnet run --project MyApp.Host
```

> Browser templates are split into an app project and a host project. Run from the app root with `--project MyApp.Host` so the host serves the generated AppBundle from the app project.
>
> If you run directly inside `MyApp.Host`, build the app project first:
>
> ```bash
> dotnet build ../MyApp.csproj
> dotnet run
> ```

**What's included:**

- Counter example with increment, decrement, and reset actions
- Abies design system CSS (brand tokens, Fluent UI v9 compatible)
- `wwwroot/index.html` with WASM bootstrap
- `Program.cs` with runtime initialization
- `.csproj` targeting `net10.0` with WASM SDK

**Project structure:**

```text
MyApp/
  Program.cs              ← Counter program (Initialize/Transition/View)
  MyApp.csproj            ← Shared app project
  MyApp.Host/             ← Runnable host project (dotnet run from here)
    Program.cs            ← Host startup + static files
    MyApp.Host.csproj     ← Host project file
  wwwroot/
    index.html            ← HTML shell with WASM bootstrap
    site.css              ← Abies design system CSS
```

## Abies Server App (`abies-server`)

Creates an interactive server-rendered app where the MVU loop runs on the server and patches are sent over WebSocket.

```bash
dotnet new abies-server -n MyServerApp
cd MyServerApp
dotnet run
```

**What's included:**

- Counter example with increment, decrement, and reset actions
- Kestrel host configuration for interactive server mode
- Server-side patch streaming over WebSocket
- `Program.cs` with host + runtime bootstrap
- `.csproj` targeting `net10.0`

## Debugger Defaults (Important)

For browser runtime templates (`abies-browser`, `abies-browser-empty`):

- In Debug builds, the browser debugger panel is enabled by default.
- In Release builds, debugger panel assets are absent.

To force-disable in your app code before `Runtime.Run(...)`:

```csharp
#if DEBUG
Picea.Abies.Debugger.DebuggerConfiguration.ConfigureDebugger(
  new Picea.Abies.Debugger.DebuggerOptions { Enabled = false });
#endif
```

For server template (`abies-server`):

- The browser debugger panel is enabled by default in local debug startup.
- To hide only the panel UI, use one of these options:
  - URL query: `?abies-debugger=off`
  - Meta tag: `<meta name="abies-debugger" content="off">`
  - Global config: `window.__abiesDebugger = { enabled: false }`
- To disable server-side debugger runtime entirely in Debug startup, set `ABIES_DEBUG_UI=0`.
- In Release builds, the debug panel assets are absent. Use server logs, browser DevTools, and OpenTelemetry traces.

## Out-of-Box Quality Expectations

Freshly generated template projects are expected to:

- Run with `dotnet run` without manual setup changes.
- Build cleanly with `dotnet build`.
- Publish with `dotnet publish -c Release`.

If these expectations are not met, treat it as a template quality regression.

## Abies Browser Empty (`abies-browser-empty`)

A minimal starting point with no example code — just the bare WASM project structure.

```bash
dotnet new abies-browser-empty -n MyApp
cd MyApp
dotnet run --project MyApp.Host
```

> Same prerequisite applies when starting from `MyApp.Host` directly: build `../MyApp.csproj` first.

**What's included:**

- Minimal starter `Program.cs` and model
- Minimal CSS (design system tokens only)
- `wwwroot/index.html`
- `.csproj` with required packages

## Template Options

All templates support standard `dotnet new` options:

```bash
# Custom output directory
dotnet new abies-browser -n MyApp -o src/MyApp

# Dry run (preview without creating)
dotnet new abies-browser -n MyApp --dry-run
```

## Updating Templates

```bash
# Update to latest version
dotnet new install Picea.Abies.Templates::1.0.*-*

# Check installed version
dotnet new list abies
```

## Next

- [**Your First App**](your-first-app.md) — Step-by-step counter tutorial
- [**Project Structure**](project-structure.md) — Understanding the full project layout
- [**Render Modes**](../concepts/render-modes.md) — Understanding the four render modes
- [**Choosing a Render Mode**](../guides/render-mode-selection.md) — Which mode is right for your project
