---
name: dotnet-cli
description: `.NET` SDK CLI reference for the squad — TUnit-flavoured test invocation (NOT xUnit), `dotnet ef` migration discipline, BenchmarkDotNet harness commands, `dotnet format` integration with the existing dotnet-format-on-save hook, NuGet package management with central package versioning, Directory.Build.props/.targets conventions, and Aspire-aware run/test patterns. Use when running tests, adding/applying EF migrations, formatting, building, packing, or scaffolding new projects. Do not use for project-specific architectural concerns (use functional-ddd) or for performance benchmark interpretation (use performance-engineering).
---

# `dotnet` — .NET SDK CLI for the squad

The squad runs .NET 10 / C# 14 with TUnit (not xUnit), Aspire for orchestration, EF Core for persistence, and BenchmarkDotNet for performance work. This skill captures the squad-flavored invocations — what to type when, and what *not* to type.

Generic SDK help: `dotnet <command> --help`. Claude knows the syntax. The squad-specific quirks are below.

## Solution and project structure

**New solutions use `.slnx`** (the XML solution format), not `.sln`. Both formats work in modern tooling, but the squad standardises on `.slnx` because it diffs cleanly and merges without the line-noise conflicts the legacy `.sln` format produces. Existing `.sln` files in older repos are not migrated reflexively — only when there's a reason to touch them — but no new `.sln` should be created.

```bash
# Create a new solution (defaults to .slnx in recent SDK versions; pass
# explicitly to be safe across SDK versions)
dotnet new sln --format slnx -n MyProduct

# Convert an existing .sln to .slnx (one-shot; the old file can then be deleted)
dotnet sln migrate
```

```bash
dotnet sln list                                   # what's in the solution
dotnet sln add src/Articles/Articles.csproj       # add a project
dotnet sln remove src/Old/Old.csproj              # remove (doesn't delete files)

# New project scaffolding from squad templates (if the team has shipped templates)
dotnet new <template> -n <Name> -o src/<Name>
```

Common conventions in `Directory.Build.props` (root-level, applies to every project):

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <EnablePreviewFeatures>true</EnablePreviewFeatures>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>moderate</NuGetAuditLevel>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Nerdbank.GitVersioning" Version="3.9.50" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

What each property buys you:

- **`LangVersion=latest` + `EnablePreviewFeatures=true`** — the squad runs on the leading edge of C#. In-development language features are available (`field` keyword, params collections improvements, etc.). Don't pin to a released version "for safety" — that defeats the choice.
- **`ImplicitUsings=enable`** — the SDK injects a default set of `using` directives (`System`, `System.Collections.Generic`, `System.Linq`, etc.) so files don't repeat them. The set varies by project SDK (`Microsoft.NET.Sdk` vs `.Web` vs `.Worker`).
- **`Nullable=enable`** — full nullable reference type analysis. Combined with `TreatWarningsAsErrors`, null-flow violations break the build.
- **`TreatWarningsAsErrors=true`** — squad policy. Compiler warnings are bugs. Local escape hatch is `dotnet build -p:TreatWarningsAsErrors=false`; never commit `WarningsNotAsErrors` lists without a `/decide` artifact explaining why.
- **`NuGetAudit` triple** — built-in SCA. Runs on every `dotnet restore`. `mode=all` covers transitive deps too (not just direct refs); `level=moderate` includes moderate/high/critical CVEs. Pairs with the `security-toolchain` skill's Layer 2.
- **`Nerdbank.GitVersioning`** — every project gets its `Version`/`AssemblyVersion`/`InformationalVersion` derived from git history (tags, branch, commit count). `PrivateAssets=all` keeps it from leaking as a transitive dep to consumers. Configuration lives in `version.json` at the repo root; see the `cicd-pipelines` skill for the release-tagging pattern.

`TargetFramework` is **not** in `Directory.Build.props` — it's a per-project concern (apps target `net10.0`, shared libraries might target `netstandard2.0`).

## Central Package Management

The squad uses **Central Package Management** (CPM): every package version lives in `Directory.Packages.props` at the repo root, and `.csproj` files reference packages without a version.

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="TUnit" Version="0.4.20" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Aspire.Hosting" Version="10.0.0" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

```xml
<!-- src/Articles/Articles.csproj — no Version attribute -->
<ItemGroup>
  <PackageReference Include="TUnit" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
</ItemGroup>
```

Why both flags matter:
- **`ManagePackageVersionsCentrally`** moves direct refs into the central file.
- **`CentralPackageTransitivePinningEnabled`** also pins *transitive* deps to versions declared in the central file. Without it, transitive resolution can drift between machines and over time. With it, every dependency in your dep graph has exactly one source-controlled version. Pairs particularly well with `NuGetAudit` — when audit flags a vuln in a transitive dep, the fix is one `PackageVersion` line ("promote this version").

The `dotnet add package` command, when CPM is on, updates `Directory.Packages.props` automatically rather than touching the `.csproj`. The `.csproj` stays version-free.

## Building

```bash
dotnet restore                              # rare to run alone; build/test do this
dotnet build                                # Debug by default
dotnet build -c Release                     # for benchmarks, packing, deploys
dotnet build -p:TreatWarningsAsErrors=false # local-only escape hatch; never commit
dotnet build --no-restore                   # in CI when restore was a separate step
```

**Warnings-as-errors is squad policy.** If you find yourself disabling it, that's a `/decide` moment, not a flag flip. Common cases that warrant a real exception (with `#pragma warning disable` + comment + tracking issue):
- A nullable analyzer false positive against a third-party API.
- Explicit obsolete-API consumption during a migration window.

## Testing — **TUnit, not xUnit**

The squad uses **TUnit**. Never assume xUnit conventions. Differences that matter:
- `[Test]` not `[Fact]`.
- `[Arguments(...)]` not `[InlineData(...)]`.
- TUnit runs as a console app (it's source-generated), not via `dotnet test`'s legacy adapter.
- Filtering syntax differs.

```bash
# Run tests in a project (TUnit's source-generated runner; equivalent to dotnet test)
dotnet run --project tests/Articles.Tests -c Release

# Or via dotnet test (TUnit ships an adapter for compatibility)
dotnet test tests/Articles.Tests

# Filter by test name (TUnit adapter)
dotnet test tests/Articles.Tests --filter "Name~PublishCommand"

# Filter by category/trait
dotnet test tests/Articles.Tests --filter "Category=integration"

# Watch mode — recompile + rerun on file change
dotnet watch test --project tests/Articles.Tests

# Coverage (Coverlet collector, output to .trx + cobertura.xml)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

**Squad rule**: if a test project's NuGet references include `xunit`, that's a finding. The reviewer subagent flags it.

### Property-based tests

The squad uses FsCheck (or CsCheck) for property-based tests of algebraic invariants on Result/Option/state-machine types. They run as ordinary TUnit tests.

```csharp
// in test code
[Test]
public async Task ResultBindIsAssociative() {
    await Prop.ForAll<int>(x => /* ... */).RunAsync();
}
```

Nothing special at the CLI level — they run with `dotnet test` like everything else, just slower.

## EF Core migrations

```bash
# Add a migration (always to a development DB; never against prod)
dotnet ef migrations add <DescriptiveName> \
  --project src/Articles.Persistence \
  --startup-project src/Articles.AppHost

# Apply migrations to the connection string in startup project's config
dotnet ef database update --project src/Articles.Persistence --startup-project src/Articles.AppHost

# Apply a specific migration (rollback included — name 0 to revert all)
dotnet ef database update <MigrationName> \
  --project src/Articles.Persistence --startup-project src/Articles.AppHost

# Generate idempotent SQL script for production deploys
dotnet ef migrations script --idempotent \
  --project src/Articles.Persistence --startup-project src/Articles.AppHost \
  -o ./artifacts/migration.sql

# Remove the most recent unapplied migration
dotnet ef migrations remove --project src/Articles.Persistence --startup-project src/Articles.AppHost
```

**Squad migration discipline:**
1. Migrations are **never destructive without an explicit two-step deploy**. Dropping a column means: PR 1 stops writing to it, PR 2 (after a release window) drops it.
2. Migrations are reviewed for *what they generate* (`dotnet ef migrations script` output), not just for what they look like in C#.
3. Production migrations apply via the deployment pipeline (`azd` + the idempotent SQL script), not via `dotnet ef database update` against a prod connection string. If you're typing `dotnet ef database update` and the connection string ends in `.azure.com`, stop.

## Aspire (`AppHost`)

Aspire's AppHost is a runnable .NET project. `dotnet run` orchestrates the whole composition (services, DBs, message brokers, OTEL collector, dashboard).

```bash
# Run the full local composition
dotnet run --project src/<Product>.AppHost

# Aspire dashboard is auto-started; URL is in the console output (typically
# https://localhost:17000 in dev). The OTEL collector is also part of the
# composition and is what feeds the squad's local observability.

# Forward args to a specific resource
dotnet run --project src/<Product>.AppHost -- --launch-profile https
```

The AppHost project is the source of truth for "what runs in dev". Connection strings, resource names, and inter-service references are all declared there. If you find yourself hand-editing `appsettings.Development.json` to make local dev work, that's a sign the AppHost is missing wiring.

## Formatting

`dotnet format` is wired to `PostToolUse(Write|Edit|MultiEdit)` via the existing `dotnet-format-on-save.sh` hook, so most of the time you don't invoke it manually. When you do:

```bash
dotnet format                               # whole solution; use sparingly (slow)
dotnet format --include src/Articles/Foo.cs # one file; matches the hook's pattern
dotnet format --verify-no-changes           # CI gate; fails if anything would change
dotnet format whitespace                    # just whitespace, fast
dotnet format style                         # style rules
dotnet format analyzers                     # analyzer rules (the slowest of the three)
```

The hook walks up to the nearest `.sln`/`.slnx` and runs `dotnet format` with `--include <changed-file>` so single-file edits don't re-format the whole solution.

## NuGet

With CPM on, `dotnet add package` updates `Directory.Packages.props` (not the `.csproj`). The `.csproj` gets a versionless `<PackageReference>`.

```bash
# Add a package — version lands in Directory.Packages.props
dotnet add src/Articles package <PackageId>

# Pin to a specific version (the version still lands in Directory.Packages.props)
dotnet add src/Articles package <PackageId> --version 8.1.2

# Remove from a project (the PackageVersion entry stays in Directory.Packages.props
# even if no project references it; clean it up by hand if it's truly unused)
dotnet remove src/Articles package <PackageId>

# List references for a project
dotnet list src/Articles package

# Outdated / vulnerable / deprecated checks. With NuGetAudit=true in
# Directory.Build.props, --vulnerable runs implicitly on every restore,
# but explicit invocation is useful in audit scripts.
dotnet list package --outdated
dotnet list package --vulnerable          # known CVEs in current versions
dotnet list package --deprecated
dotnet list package --transitive          # see what your transitive graph contains
```

`dotnet list package --vulnerable` is one of the three SCA layers (`NuGetAudit` at restore, this command on demand, Dependabot/Renovate on schedule). All three matter; they catch different things at different cadences. See `security-toolchain` skill for the full picture.

`dotnet list package --vulnerable` is a SCA layer the security-toolchain skill references. Run it in CI; act on its output.

## BenchmarkDotNet

Benchmarks live in `tests/<Context>.Benchmarks/` as console apps that reference the project under test.

```bash
# Run all benchmarks in the project (Release is mandatory)
dotnet run -c Release --project tests/Articles.Benchmarks

# Run a specific benchmark class
dotnet run -c Release --project tests/Articles.Benchmarks -- --filter '*Publish*'

# Compare two implementations via [Params]
# (defined in code; CLI just runs it)

# Output to a specific directory
dotnet run -c Release --project tests/Articles.Benchmarks -- --artifacts ./BenchmarkArtifacts
```

**Always Release mode. Always.** Debug builds are meaningless for performance numbers. The performance-engineer subagent will flag a benchmark run that wasn't `-c Release`.

## Packing and publishing

```bash
# NuGet package (for shared libraries)
dotnet pack -c Release src/SharedLib -o ./artifacts/nupkg

# Self-contained publish (rare — Container Apps prefers framework-dependent images)
dotnet publish -c Release -r linux-x64 --self-contained false src/Articles.Api -o ./artifacts/publish

# AOT publish (when project opts in via PublishAot=true)
dotnet publish -c Release -r linux-x64 src/Articles.Api -o ./artifacts/publish-aot
```

The squad's deploy artifacts are container images built by `azd`/Aspire's deployment manifest, not raw `dotnet publish` outputs. `dotnet publish` is only for ad-hoc inspection or library packaging.

## Failure modes and footguns

- **Running `dotnet ef database update` against a production connection string.** The migration tool will happily apply DDL to prod. Squad rule: never. Use the idempotent SQL script + deployment pipeline instead.
- **Tests passing locally but failing in CI** — usually `dotnet test` vs `dotnet run` divergence (TUnit's two execution paths), or culture-sensitive string comparisons (set `InvariantGlobalization=true` in tests).
- **`dotnet build` producing no output** — `--verbosity quiet` is the SDK default for some operations; `-v normal` shows what's actually happening.
- **`dotnet new` creating xUnit-shaped tests** — the SDK's default templates don't know about TUnit. The squad has its own templates in `eng/templates/` that produce TUnit-flavoured projects; use those (`dotnet new squad-tests` if installed).
- **`dotnet watch` not picking up file changes in WSL** — known issue with cross-filesystem events. `DOTNET_USE_POLLING_FILE_WATCHER=true` env var fixes it.
- **Central package versioning conflict** — `dotnet add package <X> --version Y` updates `Directory.Packages.props`. Because that file is solution-wide, the change applies to every project that references `<X>`. Verify the diff and the consuming projects before committing — bumping `EntityFrameworkCore` for one new reference can also bump it for the existing six. With `CentralPackageTransitivePinningEnabled=true`, transitive deps that depend on a different version of the same package are forcibly downgraded/upgraded to your pinned version; if that breaks a transitive consumer, the build fails loudly (which is what you want) rather than resolving to two copies silently.
- **`dotnet format` reformatting files you didn't change** — happens when `Directory.Build.props` enables a new analyzer. Either fix the analyzer findings or pin the analyzer version; don't suppress globally.
