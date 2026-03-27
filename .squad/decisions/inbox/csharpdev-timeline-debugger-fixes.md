# csharpdev timeline debugger fixes

Date: 2026-03-27

## Decisions

1. `DebuggerAdapter` test contract in browser tests is transport-focused.
- Rationale: adapter internal state members were removed from production API.
- Decision: tests must validate serialize/deserialize contract stability, not removed internals.

2. Template browser host AppBundle resolution must probe both Debug and Release output.
- Rationale: generated template runs can build one configuration and launch another, causing startup `DirectoryNotFoundException`.
- Decision: host template startup now resolves first existing AppBundle path (preferred config, then fallback).

3. Template E2E restore must avoid stale global NuGet cache for same package version.
- Rationale: local debug-packed packages can be shadowed by previously cached release packages.
- Decision: generated template `nuget.config` now sets `globalPackagesFolder` to `.nuget/packages`.

4. Browser package must ship debugger module in Debug package artifacts.
- Rationale: browser runtime debug bootstrap imports `../debugger.js`; if not packed/copied, debugger shell never mounts in generated browser template apps.
- Decision: `Picea.Abies.Browser` now packs `wwwroot/debugger.js` and the `.targets` file copies it when present (Release-safe conditional copy).

## Validation

- `dotnet test --project Picea.Abies.Browser.Tests/Picea.Abies.Browser.Tests.csproj -c Debug -v minimal`
- `dotnet test --project Picea.Abies.Templates.Testing.E2E/Picea.Abies.Templates.Testing.E2E.csproj -c Debug -v minimal`

Both suites pass after these changes.
