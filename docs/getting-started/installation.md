# Installation

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A C# editor (Visual Studio, Rider, VS Code with C# Dev Kit)

## Using Templates (Recommended)

The fastest way to start is with the Abies project templates:

```bash
# Install the templates (one-time)
dotnet new install Picea.Abies.Templates

# Create a browser (WASM) app
dotnet new abies-browser -n MyApp

# Or create a server-rendered app
dotnet new abies-server -n MyApp

cd MyApp
dotnet run
```

See [Project Templates](templates.md) for all available templates and options.

## Manual Installation

If you prefer to set up manually, add the appropriate NuGet package:

### For Browser (WASM) Apps

```bash
dotnet add package Picea.Abies.Browser
```

Or in your `.csproj`:

```xml
<PackageReference Include="Picea.Abies.Browser" Version="2.*" />
```

### For Server-Rendered Apps

```bash
dotnet add package Picea.Abies.Server.Kestrel
```

Or in your `.csproj`:

```xml
<PackageReference Include="Picea.Abies.Server.Kestrel" Version="2.*" />
```

### Core Library Only

If you only need the MVU core (for custom runtimes or testing):

```bash
dotnet add package Picea.Abies
```

## Package Overview

| Package | Description | Use Case |
| --- | --- | --- |
| `Picea.Abies` | Core MVU library (virtual DOM, diffing, rendering) | All projects |
| `Picea.Abies.Browser` | Browser runtime (WASM host, JS interop) | Client-side apps |
| `Picea.Abies.Server` | Server runtime (SSR, Session, Page, Transport) | Server-rendered apps |
| `Picea.Abies.Server.Kestrel` | Kestrel integration (WebSocket, static files) | ASP.NET Core hosting |
| `Picea.Abies.Templates` | `dotnet new` project templates | Quick scaffolding |
| `Picea.Abies.Analyzers` | Roslyn analyzers for compile-time HTML checks | Development |

### Migration from `Abies` Packages

If you were using the old `Abies`, `Abies.Browser`, or `Abies.Server` packages, these are now **metapackages** that redirect to the new `Picea.Abies.*` packages. They continue to work but will show a deprecation notice:

```bash
# Old (deprecated, still works)
dotnet add package Abies.Browser

# New (recommended)
dotnet add package Picea.Abies.Browser
```

## Project Configuration

Abies targets .NET 10.0 and uses the latest C# language features:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

## Verify Installation

```bash
dotnet build
```

If the build succeeds, you're ready to go. See [Your First App](your-first-app.md) for the next step.

## Next

- [**Project Templates**](templates.md) — Template options and customization
- [**Your First App**](your-first-app.md) — Build a counter step-by-step
- [**Render Modes**](../concepts/render-modes.md) — Choose how your app renders
