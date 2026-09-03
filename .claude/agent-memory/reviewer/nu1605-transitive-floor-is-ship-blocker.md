---
name: nu1605-transitive-floor-is-ship-blocker
description: A package migration can look complete at direct-reference level and still be non-shippable — treat NU1605 downgrade errors as hard blockers
metadata:
  type: feedback
---

`NU1605` package-downgrade errors are hard ship blockers. A migration whose
direct `PackageReference` versions all read correctly can still be
non-shippable, because a transitive dependency imposes a higher floor than the
direct pin.

**Why:** Learned reviewing the Picea 1.0.0 migration on 2026-05-06. Every direct
reference had been moved to `1.0.0` and the change looked done; restore failed
because `Picea.Glauca` depends on a prerelease `Picea` floor. Reading the diff
was not enough to see it — only a restore surfaced the conflict.

**How to apply:** For any dependency-version change, do not accept the diff as
evidence. Require a green restore, and read the restore output rather than the
csproj. Then the only acceptable resolutions are a compatible transitive release
or a scoped rollback — never suppressing the warning. If a scoped prerelease pin
is proposed as the bridge, apply
[[prerelease-pin-acceptance-criteria]]. Background on this specific case:
[[picea-glauca-prerelease-floor]] in the C# Dev's memory.
