# Installation

This guide covers setting up your development environment for Abies.

## Prerequisites

### Required

- **.NET 10 SDK** (or later)
  - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
  - Verify: `dotnet --version` should show `10.0.x` or higher

- **A modern browser** with WebAssembly support
  - Chrome, Firefox, Safari, or Edge (all recent versions)

### Recommended

- **Visual Studio Code** with the C# Dev Kit extension
- **JetBrains Rider** 2024.3 or later
- **Visual Studio 2022** 17.8 or later

## Create a New Project

### Option 1: Clone the Repository

The fastest way to get started is to clone the Abies repository and run a sample:

```bash
git clone https://github.com/Picea/Abies.git
cd Abies
dotnet run --project Abies.Counter
```

This starts a minimal counter app at `http://localhost:5000`.

### Option 2: Start from a Template

The recommended way to create a new Abies project is using the `dotnet new` templates:

```bash
# Install the Abies templates (one-time)
dotnet new install Abies.Templates

# Create a new project with counter example
dotnet new abies -n MyApp

# Or create an empty project
dotnet new abies-empty -n MyApp

# Run your new app
cd MyApp
dotnet run
```

This creates a fully configured Abies project with:
- WebAssembly project structure
- Sample counter application (or empty template)
- Development server configuration
- Launch settings for debugging

### Option 3: Add to Existing Project

Add the Abies NuGet package to your project:

```bash
dotnet add package Abies
```

Then configure your project for WebAssembly. See [Project Structure](./project-structure.md) for the required setup.

## Verify Installation

Run the counter sample to verify everything works:

```bash
cd Abies
dotnet run --project Abies.Counter
```

You should see:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

Open `http://localhost:5000` in your browser. You should see a counter with increment and decrement buttons.

## Project Structure

After cloning, the repository contains:

```
Abies/
├── Abies/                    # Core framework library
├── Abies.Counter/            # Minimal counter sample
├── Abies.SubscriptionsDemo/  # Subscriptions showcase
├── Abies.Conduit/            # Full-featured sample app
├── Abies.Conduit.Api/        # Backend API for Conduit
├── Abies.Tests/              # Unit tests
└── docs/                     # This documentation
```

## Troubleshooting

### "SDK not found" error

Ensure you have .NET 10 SDK installed. Check `global.json` in the repository root—it pins the SDK version.

### Browser shows blank page

1. Open browser DevTools (F12)
2. Check the Console tab for errors
3. Ensure WebAssembly is enabled in your browser

### Port already in use

The samples default to port 5000. If it's in use, specify a different port:

```bash
dotnet run --project Abies.Counter --urls http://localhost:5001
```

## Next Steps

Continue to [Your First App](./your-first-app.md) to build a counter from scratch and understand how Abies works.
