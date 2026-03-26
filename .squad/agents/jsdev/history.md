# Senior JavaScript Developer — History

## About This File

Project-specific learnings from JS/TS work. Read this before every session.

## Patterns Established

*None yet — this grows as the codebase takes shape.*

## Dependencies Added

| Package | Why | Date |
| ------- | --- | ---- |
| *None yet* | | |

## Performance Observations

*None yet.*

## Gotchas & Quirks

*None yet.*

## Conventions

*None yet — propose team-wide conventions via `.squad/decisions/inbox/`.*

## Learnings

- 2026-03-23: Added issue #160 Release asset contract gate in `Picea.Abies.Templates.Testing/TemplateBuildTests.cs`.
- Gate publishes `abies-browser` template with `dotnet publish -c Release`, locates published `abies.js` under the publish output, scans for debugger runtime marker strings, and intentionally fails pending implementation.
- TUnit filtering via `dotnet test --filter` is unsupported in this setup; targeted execution was validated through the TUnit host (`dotnet run --project ...`) and full-run output captured the expected failure message.
- 2026-03-26: Updated counter buttons in `abies-browser` and `abies-server` templates to visible `+` and `-` labels, with `ariaLabel("Increase")` and `ariaLabel("Decrease")` to keep accessible names descriptive.
- 2026-03-26: Template tests should query button role names by accessibility labels (`Increase`/`Decrease`) once symbolic labels are used, instead of relying on glyph names.
