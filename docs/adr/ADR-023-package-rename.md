# ADR-023: Package Rename — Abies → Picea.Abies

**Status:** Accepted  
**Date:** 2026-03-11  
**Decision Makers:** Maurice Peters  
**Related:** ADR-022 (Picea Ecosystem Migration)

## Context

As part of the migration to the Picea GitHub organization (ADR-022), all NuGet packages need a naming strategy that:

1. **Reflects the ecosystem hierarchy** — packages should clearly communicate their relationship to the Picea kernel
2. **Follows the Domain-Driven Namespace Principle** — namespaces are bounded contexts, not abbreviations
3. **Provides backward compatibility** — existing users of `Abies` and `Abies.Browser` packages should have a smooth migration path
4. **Avoids name collisions** — the `Picea.` prefix creates a unique namespace on NuGet

## Decision

Rename all packages to use the `Picea.` prefix, and provide metapackages under the old names for backward compatibility.

### Package Mapping

| Old Package | New Package | Type |
|---|---|---|
| `Abies` | `Picea.Abies` | Renamed |
| `Abies.Browser` | `Picea.Abies.Browser` | Renamed |
| `Abies.Server` | `Picea.Abies.Server` | **New** |
| — | `Picea.Abies.Server.Kestrel` | **New** |
| — | `Picea.Abies.Analyzers` | **New** |
| — | `Picea.Abies.Templates` | **New** |
| `Automaton` | `Picea` | Renamed |

### Metapackage Strategy

The old package names (`Abies`, `Abies.Browser`, `Abies.Server`) become **metapackages**:

- Contain **no code** — only a `<PackageReference>` forwarding to the `Picea.Abies.*` equivalent
- Include a `<PackageDeprecationMessage>` guiding users to the new names
- Will be maintained for **2 major versions**, then archived
- Published from the `Metapackages/` directory in the picea/abies repo

```xml
<!-- Example: Abies metapackage -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <Description>Metapackage — use Picea.Abies instead.</Description>
    <PackageDeprecationMessage>
      This package has been renamed to Picea.Abies.
      Install Picea.Abies instead: dotnet add package Picea.Abies
    </PackageDeprecationMessage>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Picea.Abies" Version="$(Version)" />
  </ItemGroup>
</Project>
```

### Namespace Alignment

C# namespaces follow the package names exactly:

| Old Namespace | New Namespace |
|---|---|
| `Abies` | `Picea.Abies` |
| `Abies.DOM` | `Picea.Abies.DOM` |
| `Abies.Html` | `Picea.Abies.Html` |
| `Abies.Subscriptions` | `Picea.Abies.Subscriptions` |
| `Abies.Browser` | `Picea.Abies.Browser` |
| `Abies.Server` | `Picea.Abies.Server` |

This follows the Domain-Driven Namespace Principle: `Picea` is the genus (ecosystem), `Abies` is the bounded context (MVU framework), and sub-namespaces are domain concepts within that context.

## Consequences

### Positive

- Package names clearly communicate the ecosystem hierarchy: `Picea.Abies.Browser` reads as "the browser runtime of the Abies MVU framework in the Picea ecosystem"
- The `Picea.` prefix creates a unique namespace on NuGet, avoiding potential collisions
- Metapackages provide a zero-effort migration path for existing users
- Namespace alignment means `using Picea.Abies;` brings the package name into the code, improving code readability

### Negative

- Existing users must update their package references (though metapackages soften this)
- Longer package names (`Picea.Abies.Browser` vs `Abies.Browser`)
- Metapackages add maintenance overhead for 2 major versions

### Neutral

- All project directories in the repo are renamed to match (`Picea.Abies/`, `Picea.Abies.Browser/`, etc.)
- The solution file is renamed to `Picea.Abies.sln`

## Alternatives Considered

### Alternative 1: Keep `Abies` Package Names

Keep packages as `Abies`, `Abies.Browser`, etc., without the `Picea.` prefix.

**Rejected** because: this breaks the namespace-as-bounded-context principle. The packages *are* part of the Picea ecosystem, and their names should reflect that. It also risks name collisions — `Abies` is a common word (it's the Latin genus for fir trees).

### Alternative 2: Flat `Picea.*` Without `Abies`

Name packages `Picea.Mvu`, `Picea.Mvu.Browser`, dropping the `Abies` name entirely.

**Rejected** because: `Abies` is the established brand name with existing users and documentation. Dropping it would break backward compatibility more severely and lose brand recognition.

### Alternative 3: No Metapackages

Require users to manually update package references with no forwarding.

**Rejected** because: this creates unnecessary friction for existing users. Metapackages are cheap to maintain and provide a smooth migration path.

## Related Decisions

- [ADR-022: Picea Ecosystem Migration](./ADR-022-picea-ecosystem-migration.md) — The broader migration decision
- [ADR-017: .NET New Templates](./ADR-017-dotnet-new-templates.md) — Template package names updated accordingly

## References

- [NuGet Package Naming Best Practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices#package-id)
- [Picea Migration Plan — NuGet Package Map](../migration/picea-migration-plan.md)
