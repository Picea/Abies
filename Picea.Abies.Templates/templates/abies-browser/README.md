# Abies Browser Application

A minimal [Abies](https://github.com/Picea/Abies) browser application demonstrating the Model-View-Update (MVU) pattern in WebAssembly.

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

The application starts on the URL shown in the terminal output.

### Project Structure

```text
├── Program.cs              # Application entry point (MVU loop)
├── AbiesApp.csproj         # Project configuration
├── wwwroot/                # Static files served to the browser
│   ├── index.html          # Bootstrap HTML (includes abies.js)
│   └── site.css            # Application styles
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

In Debug builds, the browser debugger panel is enabled by default.

To force-disable the **Abies Time Travel Debugger** before calling `Runtime.Run()`:

```csharp
#if DEBUG
Picea.Abies.Debugger.DebuggerConfiguration.ConfigureDebugger(
  new Picea.Abies.Debugger.DebuggerOptions { Enabled = false });
#endif
```

In Release builds, keep debugger configuration inside `#if DEBUG` to avoid debug UI setup.

See [Devtools Guide](https://github.com/Picea/Abies/blob/main/docs/guides/devtools.md) for the full debugger guide.

### Hot Reload (Debug Mode)

Edit your view code and save — Abies automatically hot reloads without losing your application state:

```bash
dotnet watch run
```

Change the `View` function, save, and see the changes instantly.

### Release Optimization

Release builds optimize WebAssembly output and minimize bundle size.

## Customization

### Styling

Edit `wwwroot/site.css` to customize the look and feel:

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
  model.Count > 10
    ? SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new TimerTick())
    : SubscriptionModule.None;
```

## Building for Production

To build an optimized Release package:

```bash
dotnet publish -c Release
```

The output is in `bin/Release/net10.0/publish/wwwroot/`. Deploy to any static file host or CDN.

## Documentation

- [Abies Documentation](https://github.com/Picea/Abies/blob/main/docs/index.md) — Complete Abies guide
- [Debugging Guide](https://github.com/Picea/Abies/blob/main/docs/guides/debugging.md) — Debug strategies and tools
- [Performance Guide](https://github.com/Picea/Abies/blob/main/docs/guides/performance.md) — Optimization tips
- [Architecture Decision Records (ADRs)](https://github.com/Picea/Abies/tree/main/docs/adr) — Design rationale

## Examples

See [Conduit](https://github.com/Picea/Abies/tree/main/Picea.Abies.Conduit) for a full-featured real-world application built with Abies.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](https://github.com/Picea/Abies/blob/main/CONTRIBUTING.md) for guidelines.

## License

Apache 2.0 — See [LICENSE](https://github.com/Picea/Abies/blob/main/LICENSE) for details.
