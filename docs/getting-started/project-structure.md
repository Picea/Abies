# Project Structure

This guide explains how Abies projects are organized, covering both browser (WASM) and server deployments.

## Browser (WASM) Project

A minimal browser project:

```text
MyApp.Wasm/
├── MyApp.Wasm.csproj       ← BlazorWebAssembly SDK, Picea.Abies.Browser ref
├── Program.cs              ← Entry point: Runtime.Run<App, Model, Unit>()
└── wwwroot/
    ├── index.html           ← HTML shell with loading placeholder
    └── css/
        └── app.css          ← Application styles
```

### Key Files

**`Program.cs`** — The entry point. A single line starts the runtime:

```csharp
await Abies.Browser.Runtime.Run<App, Model, Unit>();
```

Or with an interpreter for side effects:

```csharp
await Abies.Browser.Runtime.Run<App, Model, Unit>(
    interpreter: MyInterpreter.Handle);
```

**`wwwroot/index.html`** — The HTML shell. Include a loading placeholder for fast first paint:

```html
<body>
    <div id="main">Loading...</div>
</body>
```

**`.csproj`** — Uses the BlazorWebAssembly SDK for WASM compilation:

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

## Server Project

A minimal server project:

```text
MyApp.Server/
├── MyApp.Server.csproj     ← Web SDK, Picea.Abies.Server.Kestrel ref
├── Program.cs              ← Entry point: MapAbies<App, Model, Unit>(...)
└── Properties/
    └── launchSettings.json  ← Development URLs and profiles
```

### Key Files

**`Program.cs`** — Configures Kestrel and maps the Abies endpoint:

```csharp
using Abies.Server.Kestrel;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapAbies<App, Model, Unit>("/",
    renderMode: RenderMode.InteractiveServer("/ws"),
    interpreter: MyInterpreter.Handle);

app.Run();
```

**`.csproj`** — Uses the standard Web SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Picea.Abies.Server.Kestrel" Version="1.0.0-*" />
  </ItemGroup>
</Project>
```

## Shared Application Logic

For real applications, extract the application logic into a shared library:

```text
MyApp/
├── MyApp.csproj             ← Class library with app logic
├── App.cs                   ← Program<Model, Unit> implementation
├── Model.cs                 ← Application state
├── Messages.cs              ← Message types
└── Views/
    ├── HomePage.cs           ← View functions for home page
    ├── ArticlePage.cs        ← View functions for article page
    └── Components.cs         ← Shared view functions

MyApp.Wasm/
├── MyApp.Wasm.csproj        ← References MyApp
└── Program.cs               ← Runtime.Run<App, Model, Unit>()

MyApp.Server/
├── MyApp.Server.csproj      ← References MyApp
└── Program.cs               ← MapAbies<App, Model, Unit>(...)
```

This is the recommended structure for any non-trivial application. The shared library contains:

- **All application logic** — Model, Messages, Transition, View, Subscriptions
- **No platform dependencies** — References only `Picea.Abies` (the core package)

The platform projects (`Wasm` and `Server`) are thin shells that only wire up the runtime.

## Real-World Example: Conduit

The [Conduit](https://github.com/picea/abies/tree/main/Picea.Abies.Conduit) application is a full-featured social blogging platform (Medium clone) built with Abies. Here's its structure:

```text
Picea.Abies.Conduit/                    ← Shared application logic
├── Domain/                              ← Domain model
│   ├── Article.cs
│   ├── Comment.cs
│   ├── Profile.cs
│   └── User.cs
├── ReadModel/                           ← Query-side models
│   ├── ArticleFeed.cs
│   └── TagList.cs
└── ...other application concerns

Picea.Abies.Conduit.App/                ← MVU application
├── App.cs                               ← Program<Model, Unit>
├── Model.cs                             ← Application state
├── Messages.cs                          ← Message types
├── Routing.cs                           ← URL → Page routing
└── Views/                               ← View functions

Picea.Abies.Conduit.Api/                ← REST API backend
├── Endpoints/                           ← Minimal API endpoints
├── Authentication/                      ← JWT auth
└── Infrastructure/                      ← Database, DI

Picea.Abies.Conduit.Wasm/               ← Browser host
└── Program.cs                           ← One-liner entry point

Picea.Abies.Conduit.Server/             ← Server host
└── Program.cs                           ← MapAbies + API endpoints

Picea.Abies.Conduit.Tests/              ← Unit tests
Picea.Abies.Conduit.Wasm.Tests/         ← WASM-specific tests
Picea.Abies.Conduit.Api.Tests/          ← API integration tests
Picea.Abies.Conduit.Testing.E2E/        ← Playwright E2E tests
```

## Testing Structure

Abies projects follow a predictable test structure:

```text
MyApp.Tests/              ← Unit tests for pure functions
├── TransitionTests.cs     ← Test Transition(model, message) => (model, command)
├── ViewTests.cs           ← Test View(model) => Document
└── RoutingTests.cs        ← Test URL parsing and route matching

MyApp.Testing.E2E/        ← End-to-end tests (Playwright)
├── CounterTests.cs        ← Full browser interaction tests
└── NavigationTests.cs     ← Client-side routing tests
```

Because Transition and View are pure functions, unit testing is trivial:

```csharp
[Fact]
public void Increment_IncreasesCount()
{
    var model = new Model(Count: 5);
    var (result, command) = Counter.Transition(model, new CounterMessage.Increment());
    Assert.Equal(6, result.Count);
    Assert.IsType<Command.None>(command);
}
```

## Naming Conventions

| Convention | Example |
| ---------- | ------- |
| App library | `MyApp` |
| Browser host | `MyApp.Wasm` |
| Server host | `MyApp.Server` |
| Unit tests | `MyApp.Tests` |
| E2E tests | `MyApp.Testing.E2E` |
| API backend | `MyApp.Api` |
| API tests | `MyApp.Api.Tests` |

For the Picea ecosystem, prefix with `Picea.Abies.`:

| Convention | Example |
| ---------- | ------- |
| App library | `Picea.Abies.Conduit` |
| Browser host | `Picea.Abies.Conduit.Wasm` |
| Server host | `Picea.Abies.Conduit.Server` |

## See Also

- [Your First App](./your-first-app.md) — Build a counter from scratch
- [Render Modes](../concepts/render-modes.md) — Static, Server, WASM, and Auto
- [Installation](./installation.md) — Package installation guide
