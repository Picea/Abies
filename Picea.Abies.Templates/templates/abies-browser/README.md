# Abies Browser Application

A minimal [Abies](https://github.com/MCGPPeters/Abies) browser application demonstrating the Model-View-Update (MVU) pattern in WebAssembly.

## What is Abies?

Abies is a functional MVU framework for building web applications in C# that compile to WebAssembly. It follows the Elm architecture pattern and integrates with the Picea ecosystem.

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
├── Program.cs              # Application entry point (MVU loop)
├── AbiesApp.csproj         # Project configuration
├── wwwroot/                # Static files served to the browser
│   ├── index.html          # Bootstrap HTML (includes abies.js)
│   └── app.css             # Application styles
└── Properties/
    └── launchSettings.json # Development server configuration
```

## Understanding the Application

This template implements a simple counter application showcasing the MVU pattern:

1. **Model** — Application state (`record Model(int Count)`)
2. **View** — Render state as HTML (the `View` function)
3. **Update** — Process messages and transition to new state (the `Transition` function)
4. **Commands** — Side effects (empty in this example)
5. **Subscriptions** — Ongoing event sources (empty in this example)

See `Program.cs` for the complete implementation.

## Features

### Debugger (Built-In)

The **Abies Time Travel Debugger** is automatically enabled in Debug builds. No setup required:

1. Open the app in your browser
2. Look for the debugger panel at the bottom or side of the screen
3. Interact with the app — see each action and state transition recorded
4. Click any event to time travel back to that state

To disable the debugger, set this before calling `Runtime.Run()`:

```csharp
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });
```

See [docs/guides/devtools.md](../../docs/guides/devtools.md) for the full debugger guide.

### Hot Reload (Debug Mode)

Edit your view code and save — Abies automatically hot reloads without losing your application state:

```bash
dotnet watch run
```

Change the `View` function, save, and see the changes instantly.

### Release Optimization

Release builds compile out all debug code:
- Zero overhead from the debugger (completely stripped)
- Optimized WebAssembly output
- Minimal bundle size

## Customization

### Styling

Edit `wwwroot/app.css` to customize the look and feel:

```css
.app {
  background: white;
  font-family: system-ui, sans-serif;
}
```

### Model State

Modify the `Model` record to add more state:

```csharp
public record Model(int Count, string UserName);
```

### Messages

Add new message types to handle different user actions:

```csharp
public record Increment : Message;
public record Decrement : Message;
public record ResetClick : Message;
```

### Subscriptions

Hook into timers, events, or API streams:

```csharp
public static Subscription Subscriptions(Model model) =>
    model.Count > 10 ? 
        Subscriptions.Interval(1000, () => new TimerTick()) :
        Subscriptions.Empty;
```

## Building for Production

To build an optimized Release package:

```bash
dotnet publish -c Release
```

The output is in `bin/Release/net10.0/publish/wwwroot/`. Deploy to any static file host or CDN.

## Documentation

- [Abies Documentation](../../docs/index.md) — Complete Abies guide
- [Debugging Guide](../../docs/guides/debugging.md) — Debug strategies and tools
- [Performance Guide](../../docs/guides/performance.md) — Optimization tips
- [Architecture Decision Records (ADRs)](../../docs/adr/) — Design rationale

## Examples

See [Conduit](../../Picea.Abies.Conduit/) for a full-featured real-world application built with Abies.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT — See [LICENSE](../../LICENSE) for details.
