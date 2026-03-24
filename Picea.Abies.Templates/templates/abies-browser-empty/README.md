# Abies Browser Application (Empty)

A minimal [Abies](https://github.com/MCGPPeters/Abies) browser application with a blank slate for building your own MVU application.

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

## Customize Your App

This template provides a blank starting point with:

- An empty `Model` for your application state
- A `View` function to render (currently shows a placeholder)
- A `Transition` function to handle messages
- An empty `Subscriptions` function

Edit `Program.cs` to add your own:

1. **Model** — Define your application state
2. **Messages** — Add message types for user interactions
3. **View** — Build your UI
4. **Transition** — Handle state changes
5. **Subscriptions** — Optional: subscribe to timers, events, or API streams

## Features

### Debugger (Built-In)

The **Abies Time Travel Debugger** is automatically enabled in Debug builds:

1. Open the app in your browser
2. Look for the debugger panel
3. Interact with the app — see each action and state transition recorded
4. Click any event to time travel back to that state

To disable:

```csharp
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });
```

See [docs/guides/devtools.md](../../docs/guides/devtools.md) for the full guide.

### Hot Reload (Debug Mode)

```bash
dotnet watch run
```

Edit your view code and save — changes apply instantly without losing state.

## Documentation

- [Abies Documentation](../../docs/index.md)
- [Debugging Guide](../../docs/guides/debugging.md)
- [Complete Counter Example](../abies-browser/) — See a full working example

## License

MIT — See [LICENSE](../../LICENSE) for details.
