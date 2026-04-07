# Abies Server Application

An [Abies](https://github.com/Picea/Abies) server-rendered MVU application with Kestrel hosting and WebSocket support.

## What is Abies?

Abies is a functional MVU framework for building web applications in C# that work across multiple render modes:

- **Server**: Runs on the server, sends DOM updates over WebSocket
- **WASM**: Runs in the browser via WebAssembly
- **Auto**: Automatically selects between server and WASM based on capability

This template uses the **Server** render mode.

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- A modern web browser

### Running the Application

```bash
dotnet run
```

The application starts on the URL shown in the terminal output.

### Project Structure

```text
├── Program.cs                  # Application entry point & Kestrel setup
├── AbiesServerApp.csproj      # Project configuration
├── wwwroot/                    # Static assets
│   └── site.css                # Template styling
└── Properties/
    └── launchSettings.json    # Development server configuration
```

## Understanding the Application

In **Server render mode**:

1. The MVU loop runs **on the server** in C#
2. Your `Model`, `View`, `Transition`, and `Subscriptions` execute on the server
3. The server diffs the virtual DOM and sends binary patches over WebSocket
4. A thin JavaScript client applies patches to the real DOM

This approach provides:

- ✅ Fast initial page load (HTML renders on server)
- ✅ Progressive enhancement (works without JavaScript, degrades gracefully)
- ✅ Reduced client-side overhead (no .NET WASM runtime)
- ✅ Easy debugging (breakpoint in your C# code)
- ✅ Access to server resources (databases, files, APIs)

## Features

### Debugging (Server Mode)

In local debug startup, the browser-mounted Abies Time Travel Debugger panel is enabled by default.

For `abies-server`, use:

1. Abies Time Travel Debugger panel (default on)
2. Server logs (`Console.WriteLine` output)
3. Browser DevTools (network + WebSocket frames)
4. OpenTelemetry traces for end-to-end flow

Disable the panel when needed:

- URL query: `?abies-debugger=off`
- Meta tag: `<meta name="abies-debugger" content="off">`
- Global config: `window.__abiesDebugger = { enabled: false }`

To disable server-side debugger runtime in Debug startup, set `ABIES_DEBUG_UI=0`.

In Release builds, debugger panel assets are absent.

See [Debugging Guide](https://github.com/Picea/Abies/blob/main/docs/guides/debugging.md) for the recommended server-mode workflow.

### Hot Reload (Debug Mode)

```bash
dotnet watch run
```

Edit your `View`, `Transition`, or `Model` — changes apply instantly. Your browser session continues with the new code.

### Render Modes

To switch to **WASM** mode, create a `.Wasm` project and use: `await Picea.Abies.Browser.Runtime.Run<>()`

See [ADR-024: Four Render Modes](https://github.com/Picea/Abies/blob/main/docs/adr/ADR-024-four-render-modes.md) for architecture details.

## Customization

### Adding State

Modify the `Model`:

```csharp
public record Model(int Count, List<string> Items, bool IsLoading);
```

### Adding Messages

```csharp
public record Increment : Message;
public record AddItem(string Text) : Message;
```

### Updating the View

Edit the `View` function to render your UI. Use the `<form>` element with `data-event-*` attributes or standard Abies HTML helpers:

```csharp
form([], [
    input([type_("text"), placeholder_("Enter text")], [])
])
```

### Server-Side Commands & Subscriptions

Server render mode has full access to your server's capabilities:

```csharp
public static Subscription Subscriptions(Model model) =>
    model.IsLoading
        ? SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new RefreshData())
        : SubscriptionModule.None;
```

## Building for Production

```bash
dotnet publish -c Release
```

Release builds:

- Strip all debug code (no debugger overhead)
- Optimize .NET runtime
- Minimize bundle size

Deploy to any server that supports .NET 10.

## Routing & Navigation

Use standard Abies routing with URL parameters:

```csharp
var page = url.Path switch
{
    ["admin"] => RenderAdminPage(),
    ["articles", id] => RenderArticle(id),
    [] => RenderHomePage(),
    _ => RenderNotFound()
};
```

Navigation from the View:

```csharp
a([href_("/admin")], [text("Admin")])
```

## WebSocket Protocol

Server mode communicates via binary WebSocket messages (same format as WASM):

- Browser → Server: JSON-encoded events
- Server → Browser: Binary patches (optimized format)

The protocol is internal and automatic — no manual setup required.

## Scaling Considerations

For larger applications:

- Session state is stored **per connection** (one `Model` instance per user)
- Consider memory usage if handling many concurrent sessions
- Use database/cache for shared state between sessions
- WebSocket reconnection is automatic on client disconnect

## Documentation

- [Abies Documentation](https://github.com/Picea/Abies/blob/main/docs/index.md)
- [Debugging Guide](https://github.com/Picea/Abies/blob/main/docs/guides/debugging.md)
- [Four Render Modes (ADR-024)](https://github.com/Picea/Abies/blob/main/docs/adr/ADR-024-four-render-modes.md)
- [Server Integration Guide](https://github.com/Picea/Abies/blob/main/docs/guides/deployment.md)

## License

Apache 2.0 — See [LICENSE](https://github.com/Picea/Abies/blob/main/LICENSE) for details.
