# C# Dev Decision Note: Temporary Picea Pin for Glauca-Coupled Conduit Projects

Date: 2026-05-06
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Context

Conduit migration target is direct `Picea` `1.0.0`.

Current `Picea.Glauca` package line requires prerelease `Picea` versions (`>= 1.0.22-rc-0001` and currently `>= 1.0.27-rc-0002`), which triggers `NU1605` downgrade errors when Glauca-coupled projects pin direct `Picea` to `1.0.0`.

## Decision

Adopt migration option 1:

1. Keep all non-Glauca projects on direct `Picea` `1.0.0`.
2. For Glauca-coupled projects only, pin direct `Picea` to `1.0.27-rc-0002` as a temporary compatibility floor.

Applied to:

- `Picea.Abies.Conduit.Api`
- `Picea.Abies.Conduit.Api.Tests`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL.Tests`

## Rationale

This is the smallest targeted change that unblocks restore/build while preserving stable `Picea` `1.0.0` for all projects not coupled to Glauca.

## Exit Criteria

Remove temporary prerelease pins and return all Conduit projects to direct `Picea` `1.0.0` when either:

1. `Picea.Glauca` releases a version compatible with stable `Picea` `1.0.0`, or
2. Conduit removes/replaces Glauca coupling in the affected API/read-store/test paths.
