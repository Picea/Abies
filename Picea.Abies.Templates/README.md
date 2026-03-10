# Picea Abies Templates

Templates for creating MVU-style WebAssembly applications with the [Abies](https://github.com/Picea/Abies) framework.

## Installation

```bash
dotnet new install Picea.Abies.Templates
```

## Available Templates

| Template | Short Name | Description |
|----------|------------|-------------|
| Abies Browser Application | `abies-browser` | A minimal Abies Browser MVU application with counter example |
| Abies Browser Empty | `abies-browser-empty` | An empty Abies Browser MVU application |

## Usage

### Create a new Abies application

```bash
# Create a new Abies app with the counter example
dotnet new abies-browser -n MyApp

# Create an empty Abies app
dotnet new abies-browser-empty -n MyApp
```

### Run the application

```bash
cd MyApp
dotnet run
```

Then open your browser to the URL shown in the terminal (typically https://localhost:7xxx).

## Template Options

### abies-browser

| Option | Description | Default |
|--------|-------------|---------|
| `-n, --name` | The name for the output being created | Current directory name |
| `-o, --output` | Location to place the generated output | Current directory |

## What's Included

The `abies-browser` template creates a minimal MVU application with:

- A counter example demonstrating increment/decrement messages
- Type-safe state management with records
- Virtual DOM rendering with static HTML helpers
- WebAssembly configuration

## Learn More

- [Abies Documentation](https://github.com/Picea/Abies/tree/main/docs)
- [Getting Started Guide](https://github.com/Picea/Abies/blob/main/docs/getting-started.md)
- [MVU Walkthrough](https://github.com/Picea/Abies/blob/main/docs/mvu-walkthrough.md)

## License

[Apache 2.0](https://github.com/Picea/Abies/blob/main/LICENSE)