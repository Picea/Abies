# Abies Browser Application (Empty)

A minimal [Abies](https://github.com/Picea/Abies) browser application with a blank slate for building your own MVU application.

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

In Debug builds, the browser debugger panel is enabled by default.

To force-disable the **Abies Time Travel Debugger** before calling `Runtime.Run()`:

```csharp
#if DEBUG
Picea.Abies.Debugger.DebuggerConfiguration.ConfigureDebugger(
    new Picea.Abies.Debugger.DebuggerOptions { Enabled = false });
#endif
```

In Release builds, keep debugger configuration inside `#if DEBUG` to avoid debug UI setup.

See [Devtools Guide](https://github.com/Picea/Abies/blob/main/docs/guides/devtools.md) for the full guide.

### Hot Reload (Debug Mode)

```bash
dotnet watch run
```

Edit your view code and save — changes apply instantly without losing state.

## Documentation

- [Abies Documentation](https://github.com/Picea/Abies/blob/main/docs/index.md)
- [Debugging Guide](https://github.com/Picea/Abies/blob/main/docs/guides/debugging.md)
- [Complete Counter Example](https://github.com/Picea/Abies/tree/main/Picea.Abies.Templates/templates/abies-browser) — See a full working example

## License

Apache 2.0 — See [LICENSE](https://github.com/Picea/Abies/blob/main/LICENSE) for details.
