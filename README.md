# Abies (/ˈa.bi.eːs/)

A WebAssembly library for building MVU-style web applications with .NET.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

## Overview

Abies is a pure functional web framework for .NET that brings the **Model-View-Update (MVU)** pattern to WebAssembly. Inspired by [Elm](https://elm-lang.org/), it provides:

- **Virtual DOM** with efficient diffing and patching
- **Type-safe routing** with parser combinators
- **Unidirectional data flow** for predictable state management
- **Pure functional architecture** - no side effects in your domain logic

## Quick Start

### Using Templates (Recommended)

```bash
# Install the Abies templates (one-time)
dotnet new install Abies.Templates

# Create a new Abies application
dotnet new abies -n MyApp
cd MyApp
dotnet run
```

### Counter Example

```csharp
using Abies;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<Counter, Arguments, Model>(new Arguments());

public record Arguments;
public record Model(int Count);

public record Increment : Message;
public record Decrement : Message;

public class Counter : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(0), Commands.None);

    public static (Model, Command) Update(Message message, Model model)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Counter",
            div([], [
                button([onclick(new Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(new Increment())], [text("+")])
            ]));

    public static Message OnUrlChanged(Url url) => new Increment();
    public static Message OnLinkClicked(UrlRequest urlRequest) => new Increment();
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
    public static Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

## Subscriptions

Subscriptions let you react to external event sources (timers, browser events)
without putting side effects in `Update`.

```csharp
public record Tick : Message;
public record ViewportChanged(ViewportSize Size) : Message;
public record SocketEvent(WebSocketEvent Event) : Message;

public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch([
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new Tick()),
        SubscriptionModule.OnResize(size => new ViewportChanged(size)),
        SubscriptionModule.WebSocket(
            new WebSocketOptions("wss://example.com/socket"),
            evt => new SocketEvent(evt))
    ]);
```

## Example Application

The repository includes **Conduit**, a full implementation of the [RealWorld](https://github.com/gothinkster/realworld) specification - a Medium.com clone with:

- User authentication (login/register)
- Article CRUD operations
- Comments and favorites
- User profiles and following
- Tag filtering and pagination

## Project Structure

| Project               | Description                      |
| --------------------- | -------------------------------- |
| `Abies`               | Core framework library           |
| `Abies.Templates`     | `dotnet new` project templates   |
| `Abies.Conduit`       | RealWorld example app (frontend) |
| `Abies.Conduit.Api`   | RealWorld example app (backend)  |
| `Abies.Counter`       | Minimal counter example          |
| `Abies.SubscriptionsDemo` | Subscriptions demo app       |
| `Abies.Tests`         | Unit tests                       |

See `docs/subscriptions-demo.md` for the demo walkthrough (including the mock WebSocket feed).

## Requirements

- .NET 10 SDK or later
- A modern browser with WebAssembly support

## Building

```bash
dotnet build
```

## Running the Example

```bash
# Start the API server
dotnet run --project Abies.Conduit.Api

# In another terminal, start the frontend
dotnet run --project Abies.Conduit
```

## Documentation

See the [docs](./docs/) folder for:

- [Documentation Index](./docs/index.md)
- [Installation](./docs/getting-started/installation.md)
- [Your First App](./docs/getting-started/your-first-app.md)
- [Project Structure](./docs/getting-started/project-structure.md)
- [MVU Architecture](./docs/concepts/mvu-architecture.md)
- [Commands & Effects](./docs/concepts/commands-effects.md)
- [Components (Elements)](./docs/concepts/components.md)
- [Subscriptions](./docs/concepts/subscriptions.md)
- [Testing](./docs/guides/testing.md)
- [Runtime API](./docs/api/runtime.md)
- [Virtual DOM](./docs/concepts/virtual-dom.md)

### Tutorials

Step-by-step guides to learn Abies:

1. [Counter App](./docs/tutorials/01-counter-app.md) — Your first Abies app
2. [Todo List](./docs/tutorials/02-todo-list.md) — Managing collections
3. [API Integration](./docs/tutorials/03-api-integration.md) — HTTP commands
4. [Routing](./docs/tutorials/04-routing.md) — Multi-page apps
5. [Forms](./docs/tutorials/05-forms.md) — Input handling & validation
6. [Subscriptions](./docs/tutorials/06-subscriptions.md) — External events
7. [Real World App](./docs/tutorials/07-real-world-app.md) — Complete example
8. [Tracing](./docs/tutorials/08-tracing.md) — OpenTelemetry integration

## The Name

Abies is a Latin name meaning "fir tree".

### Pronunciation

- **A**: as in "father" [a]
- **bi**: as in "machine" [bi]
- **es**: as in "they" but shorter [eːs]

**Stress**: First syllable (A-bi-es)  
**Phonetic**: AH-bee-ehs

## License

[Apache 2.0](LICENSE)
