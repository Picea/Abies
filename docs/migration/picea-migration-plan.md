# Picea Migration Plan

Status: Published and active reference  
Owner: Maurice Peters  
Last updated: 2026-05-01

This plan is the implementation companion for:

- [ADR-022: Migration from Automaton to Picea Ecosystem](../adr/ADR-022-picea-ecosystem-migration.md)
- [ADR-023: Package Rename - Abies -> Picea.Abies](../adr/ADR-023-package-rename.md)

## Purpose

The migration split the former Automaton monorepo into focused repositories under the Picea GitHub organization, while preserving history and keeping package adoption practical for existing users.

This document is the canonical migration checklist and mapping reference for contributors.

## Repository Split Plan

| Repository | Source in automaton monorepo | Domain |
| --- | --- | --- |
| `picea/picea` | `Automaton/` | Kernel primitives (`Automaton`, `Result`, `Option`, `Decider`) |
| `picea/abies` | `Abies/`, `Abies.Browser/`, `Abies.Server/`, related samples/tools | MVU framework for .NET |
| `picea/mariana` | `Automaton.Resilience/` | Resilience patterns |
| `picea/glauca` | `Automaton.Patterns/EventSourcing/` | Event sourcing patterns |
| `picea/rubens` | `Automaton/Actor/` | Actor model patterns |

## Git History Strategy

- `picea/picea`: cherry-picked kernel commits.
- `picea/abies`: rebased Automaton MVU commits on top of existing Abies history.
- `picea/glauca`, `picea/rubens`, `picea/mariana`: extracted with `git filter-repo`.

Principle: preserve enough history so `git blame` and `git log` remain useful after extraction.

## Package Migration Map

| Old package | Current package | Migration policy |
| --- | --- | --- |
| `Automaton` | `Picea` | Rename and move to ecosystem root package |
| `Abies` | `Picea.Abies` | Old name retained as metapackage forwarding to new package |
| `Abies.Browser` | `Picea.Abies.Browser` | Old name retained as metapackage forwarding to new package |
| `Abies.Server` | `Picea.Abies.Server` | Old name retained as metapackage forwarding to new package |

Related additions in the Abies repository:

- `Picea.Abies.Server.Kestrel`
- `Picea.Abies.Analyzers`
- `Picea.Abies.Templates`

## Namespace Migration Rule

Migrate namespaces one-to-one with package rename:

- `Abies.*` -> `Picea.Abies.*`
- `Automaton.*` -> `Picea.*`

Do not introduce mixed namespace trees in new code.

## Contributor Checklist

Use this checklist when touching migration-sensitive docs, samples, templates, or package metadata.

1. Verify package IDs in project files use `Picea.*` naming.
2. Verify sample and tutorial `using` statements use `Picea.*` namespaces.
3. Keep metapackage guidance intact for users still on old package IDs.
4. Keep ADR references aligned with this plan when adding migration notes.
5. Keep links to this file stable from ADR-022 and ADR-023.

## Current Repository Rule

For this repository, the canonical browser runtime script is:

- `Picea.Abies.Browser/wwwroot/abies.js`

Do not edit copied `abies.js` files in sample or template projects; they are synchronized from the canonical file during build.

## Acceptance Criteria for ADR-022 Linking

This plan satisfies the ADR acceptance requirement that migration references resolve by providing a maintained document at:

- `docs/migration/picea-migration-plan.md`

## Related References

- [ADR-022: Migration from Automaton to Picea Ecosystem](../adr/ADR-022-picea-ecosystem-migration.md)
- [ADR-023: Package Rename - Abies -> Picea.Abies](../adr/ADR-023-package-rename.md)
- [Documentation Index](../index.md)
