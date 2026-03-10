# Your First Abies Application

This guide walks you through building a counter application in Abies. You'll learn the MVU pattern by implementing all four parts: **Model**, **View**, **Transition**, and **Message**.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A code editor (VS Code, Rider, or Visual Studio)

## Choose Your Platform

Abies runs on two platforms. Pick one to start (you can add the other later):

| Platform | Best for | Startup time |
| -------- | -------- | ------------ |
| [Browser (WASM)](#browser-wasm) | SPAs, offline-first apps | Instant after download |
| [Server (Kestrel)](#server-kestrel) | SEO, thin clients, real-time | Instant (no download) |

---

## Browser (WASM)

### 1. Create the Project

```bash
dotnet new console -n MyCounter
cd MyCounter
dotnet add package Picea.Abies.Browser
```

### 2. Define the Model

The model holds all application state. Use a `record` for immutability:

```csharp
public record Model(int Count);
```

### 3. Define Messages

Messages describe everything that can happen. Use `record struct` for zero-allocation hot-path messages:

```csharp
using Abies;

public interface CounterMessage : Message
{
    record struct Increment : CounterMessage;
    record struct Decrement : CounterMessage;
}
```

### 4. Define the Program

A Program connects model, messages, and view:

```csharp
using Abies;
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

public class Counter : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit _)
        => (new Model(Count: 0), Commands.None);

    public static (Model, Command) Transition(Model model, Message message)
        => message switch
        {
            CounterMessage.Increment => (model with { Count = model.Count + 1 }, Commands.None),
            CounterMessage.Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Counter",
            div([], [
                button([onclick(new CounterMessage.Decrement())], [text("-")]),
                text($" {model.Count} "),
                button([onclick(new CounterMessage.Increment())], [text("+")])
            ]));

    public static Subscription Subscriptions(Model model)
        => SubscriptionModule.None;
}
```

### 5. Wire Up the Entry Point

The browser runtime is a single line:

```csharp
await Abies.Browser.Runtime.Run<Counter, Model, Unit>();
```

That's it. This:
1. Calls `Initialize` to get the initial model
2. Calls `View(model)` to render the initial virtual DOM
3. Diffs and patches the actual DOM
4. Listens for events and dispatches messages through `Transition`

### 6. Add the HTML Shell

Create `wwwroot/index.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Counter</title>
</head>
<body>
    <div id="main">Loading...</div>
</body>
</html>
```

### 7. Configure the Project

Update your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Picea.Abies.Browser" Version="1.0.0-*" />
  </ItemGroup>
</Project>
```

### 8. Run It

```bash
dotnet run
```

Open the URL shown in the terminal. Click **+** and **−** to see the counter update.

---

## Server (Kestrel)

### 1. Create the Project

```bash
dotnet new web -n MyCounter.Server
cd MyCounter.Server
dotnet add package Picea.Abies.Server.Kestrel
```

### 2. Define Model, Messages, and Program

The Model, Messages, and Program are **identical** to the browser version:

```csharp
using Abies;
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

public record Model(int Count);

public interface CounterMessage : Message
{
    record struct Increment : CounterMessage;
    record struct Decrement : CounterMessage;
}

public class Counter : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit _)
        => (new Model(Count: 0), Commands.None);

    public static (Model, Command) Transition(Model model, Message message)
        => message switch
        {
            CounterMessage.Increment => (model with { Count = model.Count + 1 }, Commands.None),
            CounterMessage.Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Counter",
            div([], [
                button([onclick(new CounterMessage.Decrement())], [text("-")]),
                text($" {model.Count} "),
                button([onclick(new CounterMessage.Increment())], [text("+")])
            ]));

    public static Subscription Subscriptions(Model model)
        => SubscriptionModule.None;
}
```

> **Notice:** The exact same `Counter` class works on both platforms. This is the power of the MVU pattern — your application logic is platform-agnostic.

### 3. Wire Up the Server

```csharp
using Abies.Server.Kestrel;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapAbies<Counter, Model, Unit>("/",
    renderMode: RenderMode.InteractiveServer("/ws"));

app.Run();
```

This:
1. Server-renders the initial HTML (instant first paint)
2. Opens a WebSocket connection at `/ws`
3. Runs the MVU loop on the server
4. Sends DOM patches over WebSocket in real time

### 4. Run It

```bash
dotnet run
```

Open the URL. The counter works immediately — no WASM download required.

---

## Understanding the Code

### The MVU Loop

Every Abies application follows this cycle:

```text
          Message
            │
            ▼
    ┌─────────────┐
    │  Transition  │  (Model, Message) → (Model, Command)
    └─────────────┘
            │
     new Model
            │
            ▼
    ┌─────────────┐
    │     View     │  Model → Document
    └─────────────┘
            │
     Virtual DOM
            │
            ▼
    ┌─────────────┐
    │  Diff + Patch │  VDom → DOM patches
    └─────────────┘
            │
     user event → Message → (loop)
```

### Key Types

| Type | Role | Example |
| ---- | ---- | ------- |
| `Model` | All application state | `record Model(int Count)` |
| `Message` | Events that can happen | `record struct Increment : CounterMessage` |
| `Command` | Side effects to perform | `Commands.None`, `new FetchData()` |
| `Document` | Page title + virtual DOM | `new("Title", body)` |
| `Subscription` | External event sources | `SubscriptionModule.Every(...)` |

### Why `record struct` for Messages?

Messages are created on every user interaction. Using `record struct` instead of `record class` keeps them on the stack — zero heap allocation, zero GC pressure. This matters for performance in hot paths like mouse moves and rapid clicks.

### Why `Unit`?

The `Unit` type parameter is the initialization argument. When your app doesn't need startup arguments, use `Unit` (the type with exactly one value). For apps that need flags (e.g., API base URL, initial route), use a custom argument type.

## Adding Side Effects

The counter doesn't perform side effects, so Transition always returns `Commands.None`. Here's how you'd add a save-to-server effect:

```csharp
// 1. Define the command
public record SaveCount(int Count) : Command;

// 2. Return it from Transition
case CounterMessage.Increment:
    var newCount = model.Count + 1;
    return (model with { Count = newCount }, new SaveCount(newCount));

// 3. Handle it in the interpreter
Interpreter<Command, Message> interpreter = async command =>
{
    if (command is SaveCount save)
    {
        await httpClient.PostAsync($"/api/count/{save.Count}", null);
    }
    return Result<Message[], PipelineError>.Ok([]);
};

// 4. Pass the interpreter to the runtime
await Abies.Browser.Runtime.Run<Counter, Model, Unit>(
    interpreter: interpreter);
```

## What's Next?

- [Project Structure](./project-structure.md) — How Abies projects are organized
- [Render Modes](../concepts/render-modes.md) — Static, Server, WASM, and Auto rendering
- [Commands and Effects](../concepts/commands-effects.md) — Handling side effects
- [Subscriptions](../concepts/subscriptions.md) — Listening to external events
