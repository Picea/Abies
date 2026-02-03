# Project Templates

Abies provides `dotnet new` templates for quickly scaffolding new MVU applications.

## Installation

Install the templates package from NuGet:

```bash
dotnet new install Abies.Templates
```

## Available Templates

| Template | Short Name | Description |
|----------|------------|-------------|
| Abies Application | `abies` | A minimal Abies MVU application with counter example |
| Abies Application (Empty) | `abies-empty` | An empty Abies MVU application |

## Usage

### Create a Counter Application

```bash
dotnet new abies -n MyApp
cd MyApp
dotnet run
```

This creates a project with:

- A working counter example
- Model-View-Update pattern demonstration
- Styled HTML with responsive design
- Development server configuration

### Create an Empty Application

```bash
dotnet new abies-empty -n MyApp
cd MyApp
```

This creates a minimal project with:

- Empty Model and App class stubs
- Basic HTML template
- Placeholder implementations for you to fill in

## Template Options

Both templates support these options:

| Option | Description | Default |
|--------|-------------|---------|
| `-n, --name` | The name for the project | Current directory name |
| `-o, --output` | Location to place the generated output | Current directory |
| `--Framework` | Target framework (`net10.0` or `net9.0`) | `net10.0` |
| `--skipRestore` | Skip automatic package restore | `false` |

### Examples

```bash
# Create project with custom name
dotnet new abies -n MyCounter

# Create in specific directory
dotnet new abies -n MyApp -o ./projects/MyApp

# Target .NET 9
dotnet new abies -n MyApp --Framework net9.0

# Skip restore (useful for CI)
dotnet new abies -n MyApp --skipRestore
```

## Project Structure

Projects created from templates have this structure:

```text
MyApp/
├── MyApp.csproj          # WebAssembly project file
├── Program.cs            # Application entry point with MVU setup
├── Properties/
│   └── launchSettings.json   # Development server configuration
└── wwwroot/
    └── index.html        # HTML host page
```

## Updating Templates

To update to the latest version:

```bash
dotnet new install Abies.Templates
```

## Uninstalling Templates

To remove the templates:

```bash
dotnet new uninstall Abies.Templates
```

## Troubleshooting

### Template not found

If `dotnet new abies` shows "No templates found", ensure the templates are installed:

```bash
dotnet new list abies
```

If nothing shows, reinstall the templates:

```bash
dotnet new install Abies.Templates --force
```

### Package restore fails

If the Abies package cannot be restored, ensure you have access to NuGet.org or your configured package sources.

### Ports in use

The template generates random ports for development. If you encounter port conflicts, edit `Properties/launchSettings.json` and change the `applicationUrl` values.

## See Also

- [Your First App](./your-first-app.md) - Tutorial building an app step by step
- [Project Structure](./project-structure.md) - Understanding the project layout
- [ADR-017: dotnet new Templates](../adr/ADR-017-dotnet-new-templates.md) - Architecture decision record
