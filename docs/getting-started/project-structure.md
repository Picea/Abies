# Project Structure

This guide explains how Abies projects are organized and the role of each file.

## Minimal Project Structure

A minimal Abies application requires:

```text
MyApp/
├── MyApp.csproj          # Project file with Abies reference
├── Program.cs            # Application entry point and MVU implementation
└── wwwroot/
    └── index.html        # HTML shell for WebAssembly
```

## Project File (*.csproj)

The project file configures .NET for WebAssembly:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Abies" Version="*" />
  </ItemGroup>

</Project>
```

Key settings:

- `Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"` — Enables WebAssembly compilation
- `TargetFramework` — Must be .NET 10 or later
- `Nullable` — Recommended for safety

## Entry Point (Program.cs)

The entry point starts the Abies runtime:

```csharp
await Runtime.Run<App, Arguments, Model>(new Arguments());
```

This file typically contains:

1. The `await Runtime.Run<...>` call
2. `Arguments` record (data passed to Initialize)
3. `Model` record (application state)
4. Message types
5. The `Program<Model, Arguments>` implementation

For larger apps, split these into separate files.

## HTML Shell (wwwroot/index.html)

The HTML file loads the WebAssembly module:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>My Abies App</title>
    <link rel="stylesheet" href="css/app.css" />
</head>
<body>
    <script type="module" src="abies.js"></script>
</body>
</html>
```

Key elements:

- Empty `<body>` — Abies renders directly to `document.body`
- `abies.js` — Abies JavaScript interop layer (loads the .NET WebAssembly runtime internally)

## Larger Project Structure

As your app grows, organize files by feature:

```text
MyApp/
├── MyApp.csproj
├── Program.cs              # Entry point only
├── Main.cs                 # Main Program implementation
├── Model.cs                # Model and Messages
├── Commands.cs             # Command types and HandleCommand
├── Route.cs                # Routing logic
│
├── Page/                   # Page-level components
│   ├── Home.cs
│   ├── Profile.cs
│   └── Settings.cs
│
├── Components/             # Reusable UI components
│   ├── Header.cs
│   ├── Footer.cs
│   └── ArticleCard.cs
│
├── Services/               # API clients and services
│   ├── ApiClient.cs
│   └── AuthService.cs
│
└── wwwroot/
    ├── index.html
    ├── css/
    │   └── app.css
    └── abies.js
```

## The Conduit Sample Structure

The Conduit sample demonstrates a real-world structure:

```text
Abies.Conduit/
├── Abies.Conduit.csproj
├── Program.cs              # await Runtime.Run<...>
├── Main.cs                 # Main Program with routing
├── Commands.cs             # All command handling
├── Navigation.cs           # URL change handling
├── Route.cs                # Route definitions
│
├── Page/                   # Each page is an Element
│   ├── Home.cs             # Home feed with articles
│   ├── Article.cs          # Article detail view
│   ├── Editor.cs           # Article editor
│   ├── Login.cs            # Login form
│   ├── Register.cs         # Registration form
│   ├── Profile.cs          # User profile
│   └── Settings.cs         # User settings
│
├── Services/               # API communication
│   ├── ApiClient.cs        # HTTP client wrapper
│   ├── ArticleService.cs   # Article CRUD
│   ├── AuthService.cs      # Authentication
│   └── ProfileService.cs   # Profile operations
│
├── Local/                  # Local storage helpers
│   └── Storage.cs
│
└── wwwroot/
    ├── index.html
    ├── css/
    └── abies.js
```

## File Naming Conventions

| File Type | Convention | Example |
| --------- | ---------- | ------- |
| Page | `{PageName}.cs` in `Page/` | `Page/Home.cs` |
| Component | `{Name}.cs` in `Components/` | `Components/Header.cs` |
| Service | `{Name}Service.cs` in `Services/` | `Services/AuthService.cs` |
| Messages | Nested in feature file or `Messages.cs` | Inline in `Home.cs` |
| Commands | `Commands.cs` or nested | `Commands.cs` |

## Organizing Messages

For small apps, define messages inline:

```csharp
// In Page/Home.cs
public record LoadArticles : Message;
public record ArticlesLoaded(List<Article> Articles) : Message;
```

For larger apps, use nested interfaces:

```csharp
// In Main.cs
public interface Message : Abies.Message
{
    public interface Command : Message
    {
        public sealed record ChangeRoute(Route Route) : Abies.Command;
    }

    public interface Event : Message
    {
        public sealed record UrlChanged(Url Url) : Event;
        public sealed record UserLoggedIn(User User) : Event;
    }
}
```

## Sharing State Between Pages

The top-level `Model` owns global state (current user, route). Pages receive what they need through their model:

```csharp
// Main model owns global state
public record Model(Page Page, Route CurrentRoute, User? CurrentUser);

// Page model receives what it needs
public record HomeModel(List<Article> Articles, User? CurrentUser);
```

When navigating, the main `Update` function creates the page model with current global state.

## Next Steps

- [Next Steps](./next-steps.md) — Where to go from here
- [Tutorial 1: Counter App](../tutorials/01-counter-app.md) — Reinforce the basics
