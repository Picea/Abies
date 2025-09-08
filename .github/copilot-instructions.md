# Abies - GitHub Copilot Agent Instructions

## Repository Overview

**Abies** (/ˈa.bi.eːs/) is a WebAssembly library for building MVU (Model-View-Update) style web applications with .NET. The name means "fir tree" in Latin.

### High-Level Project Information

- **Project Type**: .NET WebAssembly library and example applications
- **Target Framework**: .NET 9.0 (with .NET 10 preview support)
- **Primary Language**: C# with latest language features enabled
- **Architecture**: MVU (Model-View-Update) pattern similar to Elm
- **Package Dependencies**: Uses Praefixum for code generation, FsCheck for property testing, Playwright for E2E testing

### Repository Structure

The solution contains 9 projects:
- **Abies**: Main WebAssembly library (core framework)
- **Abies.Counter**: Simple counter example application
- **Abies.Conduit**: Full-featured blog/social media demo (like RealWorld demo)
- **Abies.Conduit.Api**: Backend API for the Conduit demo
- **Abies.Conduit.E2E**: End-to-end tests using Playwright
- **Abies.Tests**: Unit tests using xUnit and FsCheck property tests
- **Abies.Benchmarks**: Performance benchmarks
- **Abies.Conduit.ServiceDefaults**: Service configuration defaults
- **Abies.Conduit.AppHost**: Application host configuration

## Build and Validation Instructions

### Prerequisites

**ALWAYS ensure these prerequisites are met before building:**

1. **.NET 9.0 SDK** (minimum required version specified in `global.json`)
2. **WebAssembly workloads** must be installed: `dotnet workload install wasm-experimental wasm-tools`
3. **Playwright browsers** (for E2E tests): automatically installed during E2E test execution

### Build Commands

**Always follow this exact sequence for building:**

```bash
# 1. Clean (when needed - removes all build artifacts)
dotnet clean

# 2. Restore dependencies (ALWAYS run before building)
dotnet restore

# 3. Build entire solution
dotnet build --no-restore

# 4. Run unit tests
dotnet test Abies.Tests/Abies.Tests.csproj --no-build --verbosity minimal

# 5. Run E2E tests (requires both API and frontend services running)
dotnet test Abies.Conduit.E2E/Abies.Conduit.E2E.csproj --no-build
```

### Development Server Commands

**To run services individually for development/debugging:**

```bash
# Run API server (in separate terminal)
dotnet run --project Abies.Conduit.Api --no-build
# Serves at: http://localhost:5168

# Run frontend WebAssembly application (in separate terminal) 
dotnet run --project Abies.Conduit --no-build
# Serves at: http://localhost:5209

# API test endpoints available in: Abies.Conduit.Api/Abies.Conduit.Api.http
```

**Build Timings (for timeout planning):**
- `dotnet restore`: ~60-70 seconds (first time), ~10 seconds (subsequent)
- `dotnet build --no-restore`: ~25 seconds (full build), ~5-10 seconds (incremental)
- Unit tests: ~1-2 seconds (38 tests)
- Clean build from scratch: ~11-12 seconds after restore

### Known Build Warnings (Expected - Not Errors)

The build generates **~1077 warnings** which are expected and do not indicate build failures:
- **CA1416 warnings**: Platform compatibility warnings for WebAssembly-specific APIs
- **CS0612 warnings**: Obsolete API usage warnings in Playwright E2E tests  
- **NU1603 warnings**: Package version resolution warnings (Markdig dependency)

**These warnings should be ignored - they do not prevent successful builds or deployments.**

### Environment Setup Requirements

**Critical environment variables for WebAssembly builds:**
```bash
export DOTNET_ROOT=/path/to/dotnet  # Point to .NET 9.0 installation
export PATH="$PATH:$DOTNET_ROOT"    # Ensure dotnet is in PATH
```

**Required for E2E tests:**
- Playwright browsers are auto-installed via `Microsoft.Playwright.Program.Main(new[] { "install" })`
- E2E tests start both API server (port 5000) and frontend (port 5209) automatically
- Tests wait for server availability before proceeding

### Service Port Configuration

**Development servers run on these ports:**
- **API server**: `http://localhost:5168` (Abies.Conduit.Api - see `Abies.Conduit.Api.http` for test endpoints)
- **Frontend**: `http://localhost:5209` (Abies.Conduit - WebAssembly application)
- **E2E test fixture**: Uses port 5000 for API, port 5209 for frontend during test runs

## Project Architecture and Key Files

### Core Library Structure (`Abies/`)

- **`Abies.csproj`**: Main library project with WebAssembly support, unsafe code enabled
- **`wwwroot/abies.js`**: JavaScript interop layer with OpenTelemetry tracing
- **`DOM/`**: Virtual DOM implementation with diffing algorithm
- **`Html/`**: HTML element builders and utilities
- **`Parser.cs`**: Functional parser combinators
- **`Runtime.cs`**: Core MVU runtime and message dispatching
- **`Interop.cs`**: JavaScript interop definitions
- **`build/Abies.targets`**: MSBuild targets for automatic JS file copying

### Configuration Files

- **`global.json`**: Specifies required .NET 9.0 SDK version (critical for builds)
- **`Directory.Build.props`**: Global project settings (C# latest, preview features, interceptors)
- **`version.json`**: GitVersioning configuration (uses Nerdbank.GitVersioning)
- **`Global/Usings.cs`**: Global using statements for all projects
- **`.vscode/launch.json`**: Debug configurations for WebAssembly and browsers

### CI/CD Pipeline (`.github/workflows/cd.yml`)

**GitHub Actions workflow runs on every push/PR:**
1. Installs .NET 9.0 and .NET 10 preview
2. Installs WASM workloads: `dotnet workload install wasm-experimental wasm-tools`
3. Installs GitVersioning: `dotnet tool install --global nbgv`
4. Runs: `dotnet restore` → `dotnet build --no-restore` → `dotnet test Abies.Tests/Abies.Tests.csproj --no-build`
5. Packages and publishes to NuGet (main branch only)

### Key Dependencies

- **Praefixum**: Code generation framework (interceptors support)
- **FsCheck**: Property-based testing framework
- **Microsoft.Playwright**: Browser automation for E2E tests
- **Markdig**: Markdown processing for Conduit demo
- **xUnit**: Unit testing framework

## Common Development Patterns

### MVU Architecture
The framework follows Elm's MVU pattern:
- **Model**: Application state
- **View**: Function that renders model to DOM
- **Update**: Function that processes messages and returns new model + commands

### Virtual DOM Implementation
Located in `docs/virtual_dom_algorithm.md`:
- Uses stack-based traversal to avoid recursion overhead
- Generates minimal patches for DOM updates
- Attributes compared using O(1) dictionary lookups
- Patches executed via JavaScript interop

### Testing Patterns
- **Unit tests**: Property-based testing with FsCheck generators
- **E2E tests**: Playwright browser automation with fixture management
- **Test collections**: E2E tests use shared fixture (`ConduitCollection`) to manage server lifecycle

## Validation and Quality Assurance

### Pre-commit Validation Steps
```bash
# Always run these commands before committing:
dotnet restore
dotnet build --no-restore
dotnet test Abies.Tests/Abies.Tests.csproj --no-build

# Optional: Run E2E tests (slower, requires server startup)
dotnet test Abies.Conduit.E2E/Abies.Conduit.E2E.csproj --no-build
```

### Expected Test Results
- **Unit tests**: 38 tests should pass in ~1-2 seconds
- **E2E tests**: Multiple browser automation tests (authentication, articles, comments, etc.)
- **Zero test failures** are expected in a healthy build

### Performance Considerations
- Clean builds take ~70+ seconds due to WebAssembly toolchain
- Incremental builds are much faster (~5-10 seconds)
- Large warning count (~1077) is normal and expected
- E2E tests require server startup time (~30-60 seconds for initialization)

## Critical Notes for Agents

1. **Trust these instructions first** - only search for additional information if these instructions are incomplete or incorrect
2. **WebAssembly builds are complex** - always install required workloads before building
3. **Warning count is normal** - 1000+ warnings do not indicate build failure
4. **E2E tests require infrastructure** - API server and frontend must be running
5. **Use exact command sequences** - the order of build commands matters for WebAssembly projects
6. **Preview features enabled** - project uses C# preview features and interceptors
7. **Browser platform target** - code is marked with `[SupportedOSPlatform("browser")]` attributes

## File Locations Reference

**Key source files**: `Abies/*.cs`, `Abies/DOM/*.cs`, `Abies/Html/*.cs`  
**Example apps**: `Abies.Counter/`, `Abies.Conduit/`  
**Tests**: `Abies.Tests/` (unit), `Abies.Conduit.E2E/` (integration)  
**Configuration**: `global.json`, `Directory.Build.props`, `.github/workflows/cd.yml`  
**Documentation**: `README.md`, `docs/virtual_dom_algorithm.md`