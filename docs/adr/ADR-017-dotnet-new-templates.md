# ADR-017: dotnet new Template Package

**Status:** Accepted  
**Date:** 2026-02-03  
**Decision Makers:** Maurice CGP Peters

## Context

Developers who want to use the Abies framework need a quick and easy way to scaffold new projects. Currently, they would need to:

1. Create a new WebAssembly project manually
2. Add the Abies package reference
3. Set up the correct project structure (wwwroot, index.html, etc.)
4. Write boilerplate code for the MVU architecture

This manual process is error-prone and creates friction for new adopters. The .NET ecosystem provides a standard solution through `dotnet new` custom templates, which allow developers to scaffold projects with a single command.

## Decision

We will create a NuGet template package (`Abies.Templates`) that provides `dotnet new` templates for creating Abies applications:

1. **`abies`** - A minimal Abies MVU application with a counter example demonstrating:
   - Model-View-Update pattern
   - Message handling
   - Event handling
   - Document rendering

2. **`abies-empty`** - An empty Abies MVU application for developers who want to start from scratch

The template package will:
- Be published to NuGet.org for easy installation
- Support the `sourceName` feature for automatic namespace/project name replacement
- Generate random ports for development server URLs
- Include proper launch settings for debugging

## Consequences

### Positive

- **Improved Developer Experience**: New users can start with `dotnet new abies -n MyApp` immediately
- **Reduced Friction**: Eliminates manual setup steps
- **Consistent Project Structure**: All new projects follow the same conventions
- **Discoverability**: Templates appear in `dotnet new list` and Visual Studio's new project dialog
- **Best Practices**: Templates embed best practices for Abies development

### Negative

- **Maintenance Overhead**: Templates need to be updated when Abies APIs change
- **Version Synchronization**: Template package version needs to stay in sync with Abies package version
- **Documentation Requirements**: Need to document how to use templates

### Neutral

- Templates are packaged separately from the main Abies library
- Users need to install templates separately with `dotnet new install Abies.Templates`

## Alternatives Considered

### Alternative 1: CLI Tool

Create a separate `dotnet tool` (e.g., `dotnet abies new`) for project scaffolding.

**Rejected because:**
- More complex to implement and maintain
- Non-standard approach - `dotnet new` is the expected pattern
- Requires additional installation step

### Alternative 2: Documentation Only

Provide documentation and sample code without templates.

**Rejected because:**
- Higher barrier to entry for new users
- More error-prone manual setup
- Poor developer experience compared to templates

### Alternative 3: Single Template

Provide only one template without the empty variant.

**Rejected because:**
- Experienced developers may want a minimal starting point
- Different use cases benefit from different templates
- Low additional maintenance cost for the empty template

## Implementation

The template package structure:

```
Abies.Templates/
├── Abies.Templates.csproj
├── README.md
└── templates/
    ├── abies/
    │   ├── .template.config/
    │   │   └── template.json
    │   ├── AbiesApp.csproj
    │   ├── Program.cs
    │   ├── Properties/
    │   │   └── launchSettings.json
    │   └── wwwroot/
    │       └── index.html
    └── abies-empty/
        ├── .template.config/
        │   └── template.json
        ├── AbiesApp.csproj
        ├── Program.cs
        ├── Properties/
        │   └── launchSettings.json
        └── wwwroot/
            └── index.html
```

Installation: `dotnet new install Abies.Templates`

Usage:
- `dotnet new abies -n MyApp`
- `dotnet new abies-empty -n MyApp`

## Related Decisions

- [ADR-001: MVU Architecture](./ADR-001-mvu-architecture.md)
- [ADR-005: WebAssembly Runtime](./ADR-005-webassembly-runtime.md)

## References

- [Custom templates for dotnet new](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [Tutorial: Create a project template](https://learn.microsoft.com/en-us/dotnet/core/tutorials/cli-templates-create-project-template)
- [dotnet/templating GitHub repo Wiki](https://github.com/dotnet/templating/wiki)
