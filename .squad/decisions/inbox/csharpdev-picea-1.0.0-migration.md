# csharpdev-picea-1.0.0-migration

Date: 2026-05-06
Owner: C# Dev

## Context

The requested migration updates Conduit project references from prerelease `Picea` to stable `1.0.0`.

Projects with direct `Picea` references were updated accordingly, but several Conduit projects also depend on `Picea.Glauca`.

## Observed Constraint

Published `Picea.Glauca` versions (`0.1.12`, `0.1.13`, `0.1.14`) depend on prerelease `Picea`:

- `0.1.12` -> `Picea >= 1.0.22-rc-0001`
- `0.1.13` -> `Picea >= 1.0.22-rc-0001`
- `0.1.14` -> `Picea >= 1.0.27-rc-0002`

With direct `Picea` pinned to `1.0.0`, restore fails (`NU1605`) for:

- `Picea.Abies.Conduit.Api`
- `Picea.Abies.Conduit.Api.Tests`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL.Tests`

## Decision

Record this as a hard compatibility boundary:

- Keep direct `Picea` updates to `1.0.0` in scope.
- Do not perform broad event-store architecture rewrites in this migration step.
- Track Glauca compatibility as the gating item for full Conduit stabilization on `Picea 1.0.0`.

## Next Options

1. Publish a `Picea.Glauca` version compatible with stable `Picea 1.0.0`.
2. Replace Glauca usage in Conduit API/tests with alternative in-repo abstractions.
