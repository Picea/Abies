# Abies.Counter

This is an internal demo app used for framework development and testing.

## For New Applications

**Use the project template instead:**

```bash
# Install the templates (one-time)
dotnet new install Abies.Templates

# Create a new Abies application
dotnet new abies -n MyApp
cd MyApp
dotnet run
```

## Purpose

This project exists for:

- Framework developers working on Abies itself
- Testing changes to the core library
- Debugging and benchmarking
- Reference implementation for contributors

It uses a `ProjectReference` to the local Abies library, whereas end-user applications use the NuGet package.
