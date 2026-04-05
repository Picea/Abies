# Picea Abies Templates

Templates for creating MVU-style web applications with the [Abies](https://github.com/Picea/Abies) framework.

## Installation

```bash
dotnet new install Picea.Abies.Templates::1.0.*-*
```

## Available Templates

| Template | Short Name | Description |
|----------|------------|-------------|
| Abies Browser Application | `abies-browser` | A minimal Abies Browser MVU application with counter example (WebAssembly) |
| Abies Browser Empty | `abies-browser-empty` | An empty Abies Browser MVU application (WebAssembly) |
| Abies Server Application | `abies-server` | A minimal Abies Server MVU application with counter example (server-side rendering) |

## Usage

### Create a new Abies application

```bash
# Create a new WASM app with the counter example
dotnet new abies-browser -n MyApp

# Create a new server-rendered app with the counter example
dotnet new abies-server -n MyApp

# Create an empty WASM app
dotnet new abies-browser-empty -n MyApp
```

### Run the application

```bash
# Browser templates (abies-browser / abies-browser-empty)
cd MyApp
dotnet run --project MyApp.Host

# Server template (abies-server)
cd MyApp
dotnet run
```

For browser templates, if you run directly from `MyApp.Host`, build the app project first so the host can serve the generated AppBundle:

```bash
cd MyApp/MyApp.Host
dotnet build ../MyApp.csproj
dotnet run
```

Then open your browser to the URL shown in the terminal (typically https://localhost:7xxx).

## Render Modes

Abies supports four render modes. These templates cover the two interactive modes:

| Mode | Template | Description |
|------|----------|-------------|
| **InteractiveWasm** | `abies-browser` | MVU loop runs in the browser via .NET WebAssembly |
| **InteractiveServer** | `abies-server` | MVU loop runs on the server, patches sent via WebSocket |
| Static | — | Server-rendered HTML, no interactivity |
| InteractiveAuto | — | Starts as server, transitions to WASM when ready |

Both templates use the **same `Program` interface** — application code is identical across render modes. Only the hosting entry point differs.

## Template Options

### abies-browser / abies-server

| Option | Description | Default |
|--------|-------------|-------|
| `-n, --name` | The name for the output being created | Current directory name |
| `-o, --output` | Location to place the generated output | Current directory |
| `--Framework` | Target framework (`net10.0`) | `net10.0` |

## What's Included

### abies-browser

- Counter example demonstrating the MVU pattern (increment, decrement, reset)
- WebAssembly configuration with trimming for Release builds
- `index.html` with Abies design system CSS
- References `Picea.Abies.Browser` package

### abies-server

- Counter example demonstrating the MVU pattern (increment, decrement, reset)
- ASP.NET Core server with WebSocket support
- Server-side rendering with binary DOM patches
- Abies design system CSS
- References `Picea.Abies.Server.Kestrel` package

## Debugger Defaults (Important)

Browser runtime templates (`abies-browser`, `abies-browser-empty`) show the debugger panel by default in Debug builds.

To force-disable it explicitly before `Runtime.Run(...)`:

```csharp
#if DEBUG
Picea.Abies.Debugger.DebuggerConfiguration.ConfigureDebugger(
    new Picea.Abies.Debugger.DebuggerOptions { Enabled = false });
#endif
```

Release builds can keep this behind `#if DEBUG` to avoid shipping debug UI setup code.

Server template (`abies-server`) enables the browser debugger panel by default in local debug startup.

Hide panel options:

- URL query: `?abies-debugger=off`
- Meta tag: `<meta name="abies-debugger" content="off">`
- Global config: `window.__abiesDebugger = { enabled: false }`

To disable server-side debugger runtime in Debug startup, set `ABIES_DEBUG_UI=0`.

In Release builds, debugger panel assets are absent. Use server logs, browser DevTools, and OpenTelemetry traces.

## Out-of-Box Quality Expectations

A fresh template project should work without manual fixes:

- `dotnet run` starts the app.
- `dotnet build` succeeds.
- `dotnet publish -c Release` succeeds.

If any of these fail for newly generated projects, treat it as a template quality regression.

## Learn More

- [Abies Documentation](https://github.com/Picea/Abies/tree/main/docs)
- [Render Mode Selection Guide](https://github.com/Picea/Abies/blob/main/docs/guides/render-mode-selection.md)
- [Routing Guide](https://github.com/Picea/Abies/blob/main/docs/guides/routing.md)

## License

[Apache 2.0](https://github.com/Picea/Abies/blob/main/LICENSE)
