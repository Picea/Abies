# Conduit Migration Note: Picea 1.0.0 Compatibility

Date: 2026-05-06

## Summary

Conduit projects stay on `Picea` `1.0.0` by default.

A restore blocker remains for Glauca-dependent projects, where NuGet resolves `Picea.Glauca` versions that require prerelease `Picea`:

- `Picea.Abies.Conduit.Api`
- `Picea.Abies.Conduit.Api.Tests`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL.Tests`

To keep restore/build green, these four Glauca-coupled projects are temporarily pinned to direct `Picea` `1.0.27-rc-0002` (the current minimum floor observed from `NU1605`: `>= 1.0.27-rc-0002`).

All non-Glauca projects remain on direct `Picea` `1.0.0`.

## Temporary Compatibility Strategy (Option 1)

1. Keep direct `Picea` `1.0.0` everywhere by default.
2. For projects that reference `Picea.Glauca` or `Picea.Glauca.KurrentDB`, pin direct `Picea` to `1.0.27-rc-0002`.
3. Revert these temporary pins as soon as Glauca no longer requires prerelease `Picea`.

## Exit Criteria

Remove the temporary prerelease pins and move all Conduit projects back to `Picea` `1.0.0` when one of the following is true:

1. A `Picea.Glauca` release compatible with stable `Picea 1.0.0`.
2. Replacing `Picea.Glauca` usage in Conduit API/test paths with non-Glauca event-store abstractions.

Until one of these is completed, Glauca-coupled projects cannot restore with direct `Picea` pinned to `1.0.0`.
