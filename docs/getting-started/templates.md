# Project Templates

Abies provides `dotnet new` templates for quickly scaffolding new applications. Templates include the Abies design system CSS, a working counter example, and all required configuration.

## Installation

```bash
dotnet new install Picea.Abies.Templates
```

## Available Templates

| Template | Short Name | Description |
| --- | --- | --- |
| Abies Browser App | `abies-browser` | Client-side WebAssembly application |
| Abies Browser (Empty) | `abies-browser-empty` | Minimal WASM app with no example code |

## Abies Browser App (`abies-browser`)

Creates a client-side WebAssembly application that runs entirely in the browser.

```bash
dotnet new abies-browser -n MyApp
cd MyApp
dotnet run
```

**What's included:**
- Counter example with increment/decrement
- Abies design system CSS (brand tokens, Fluent UI v9 compatible)
- `wwwroot/index.html` with WASM bootstrap
- `Program.cs` with runtime initialization
- `.csproj` targeting `net10.0` with WASM SDK

**Project structure:**

```text
MyApp/
  Program.cs              ← Entry point, starts the MVU runtime
  Counter.cs              ← Counter program (Initialize, Transition, View)
  MyApp.csproj            ← WASM project file
  wwwroot/
    index.html            ← HTML shell with WASM bootstrap
    site.css              ← Abies design system CSS
```

## Abies Browser Empty (`abies-browser-empty`)

A minimal starting point with no example code — just the bare WASM project structure.

```bash
dotnet new abies-browser-empty -n MyApp
cd MyApp
dotnet run
```

**What's included:**
- Empty `Program.cs` with TODO placeholder
- Minimal CSS (design system tokens only)
- `wwwroot/index.html`
- `.csproj` with required packages

## Template Options

All templates support standard `dotnet new` options:

```bash
# Custom output directory
dotnet new abies-browser -n MyApp -o src/MyApp

# Dry run (preview without creating)
dotnet new abies-browser -n MyApp --dry-run
```

## Updating Templates

```bash
# Update to latest version
dotnet new install Picea.Abies.Templates

# Check installed version
dotnet new list abies
```

## Next

- [**Your First App**](your-first-app.md) — Step-by-step counter tutorial
- [**Project Structure**](project-structure.md) — Understanding the full project layout
- [**Render Modes**](../concepts/render-modes.md) — Understanding the four render modes
- [**Choosing a Render Mode**](../guides/render-mode-selection.md) — Which mode is right for your project
