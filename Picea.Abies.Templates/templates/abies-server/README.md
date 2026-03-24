# Abies Server Application

An [Abies](https://github.com/MCGPPeters/Abies) server-rendered MVU application with Kestrel hosting and WebSocket support.

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

The application will start on `http://localhost:5000` by default.

### Project Structure

```
├── Program.cs                  # Application entry point & Kestrel setup
├── Pages/
│   └── Index.cshtml           # Server-rendered page shell
├── AbiesServerApp.csproj      # Project configuration
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

### Debugger (Built-In)

The **Abies Time Travel Debugger** is automatically enabled in Debug builds:

1. Open the app in your browser
2. Look for the debugger panel
3. Use it to inspect every server-side state transition
4. Time travel back to any previous state (server re-processes transitions)

To disable:

```csharp
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });
```

See [docs/guides/devtools.md](../../docs/guides/devtools.md) for the full guide.

### Hot Reload (Debug Mode)

```bash
dotnet watch run
```

Edit your `View`, `Transition`, or `Model` — changes apply instantly. Your browser session continues with the new code.

### Render Modes

To switch to **WASM** mode, create a `.Wasm` project and use: `await Picea.Abies.Browser.Runtime.Run<>()`

See [ADR-024: Four Render Modes](../../docs/adr/ADR-024-four-render-modes.md) for architecture details.

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
    model.IsLoading ? 
        Subscriptions.Interval(1000, () => new RefreshData()) :
        Subscriptions.Empty;
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

- [Abies Documentation](../../docs/index.md)
- [Debugging Guide](../../docs/guides/debugging.md)
- [Four Render Modes (ADR-024)](../../docs/adr/ADR-024-four-render-modes.md)
- [Server Integration Guide](../../docs/guides/deployment.md)

## License

MIT — See [LICENSE](../../LICENSE) for details.
