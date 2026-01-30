# Next Steps

Congratulations! You've learned the basics of Abies. Here's where to go next based on what you want to learn.

## Learning Paths

### Path 1: Build Real Applications

Work through the tutorials in order:

1. [Counter App](../tutorials/01-counter-app.md) — Reinforce MVU basics
2. [Todo List](../tutorials/02-todo-list.md) — Lists, adding/removing items
3. [API Integration](../tutorials/03-api-integration.md) — HTTP requests, commands
4. [Routing](../tutorials/04-routing.md) — Multi-page navigation
5. [Forms](../tutorials/05-forms.md) — Input binding, validation
6. [Subscriptions](../tutorials/06-subscriptions.md) — Timers, events, WebSockets
7. [Real-World App](../tutorials/07-real-world-app.md) — Conduit walkthrough

### Path 2: Understand the Concepts

Dive deep into how Abies works:

1. [MVU Architecture](../concepts/mvu-architecture.md) — The pattern in depth
2. [Pure Functions](../concepts/pure-functions.md) — Why purity matters
3. [Commands & Effects](../concepts/commands-effects.md) — Side effect model
4. [Virtual DOM](../concepts/virtual-dom.md) — Rendering internals

### Path 3: Reference While Building

Use the API reference as you build:

- [Program Interface](../api/program.md) — All the methods you must implement
- [HTML Elements](../api/html-elements.md) — Available HTML helpers
- [Events](../api/html-events.md) — Event handlers and data types
- [Commands](../api/commands.md) — Built-in command types

## Sample Applications

### Abies.Counter

The minimal example. Study this to understand the absolute basics.

```bash
dotnet run --project Abies.Counter
```

### Abies.SubscriptionsDemo

Demonstrates subscriptions: timers, browser events, WebSockets.

```bash
dotnet run --project Abies.SubscriptionsDemo
```

### Abies.Conduit

A full RealWorld app implementation. This is the best example of a production-like Abies application.

```bash
# Start the API server
dotnet run --project Abies.Conduit.Api

# In another terminal, start the frontend
dotnet run --project Abies.Conduit
```

## Common Tasks

### Add a New Page

1. Create a file in `Page/` with a model, messages, and view
2. Add a route case in `Route.cs`
3. Handle the route in your main `Update` function
4. Add navigation commands as needed

See [Routing Tutorial](../tutorials/04-routing.md) for details.

### Make an API Call

1. Define a command: `public record FetchData : Command;`
2. Return it from `Update`: `(model with { IsLoading = true }, new FetchData())`
3. Handle it in `HandleCommand`:

```csharp
case FetchData:
    var data = await httpClient.GetAsync(...);
    dispatch(new DataLoaded(data));
    break;
```

See [API Integration Tutorial](../tutorials/03-api-integration.md) for details.

### Add a Timer or Event Subscription

Return a subscription from `Subscriptions`:

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new Tick());
```

See [Subscriptions Tutorial](../tutorials/06-subscriptions.md) for details.

## Getting Help

### Documentation

- This documentation: comprehensive guides and API reference
- [ADRs](../adr/README.md): understand design decisions

### Code

- Study the sample applications in the repository
- Read the framework source code in `Abies/`

### Community

- GitHub Issues: report bugs or request features
- GitHub Discussions: ask questions

## Architecture Decision Records

Want to understand *why* Abies works the way it does? Read the ADRs:

| ADR | Question It Answers |
| --- | ------------------- |
| [ADR-001](../adr/ADR-001-mvu-architecture.md) | Why MVU instead of MVVM or MVC? |
| [ADR-002](../adr/ADR-002-pure-functional-programming.md) | Why functional programming in C#? |
| [ADR-006](../adr/ADR-006-command-pattern.md) | Why commands instead of direct side effects? |
| [ADR-007](../adr/ADR-007-subscriptions.md) | How do subscriptions differ from commands? |

## Ready to Build?

Pick a tutorial and start building. The best way to learn Abies is to use it.

→ [Start with the Counter Tutorial](../tutorials/01-counter-app.md)
