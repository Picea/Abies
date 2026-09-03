---
name: picea-glauca-prerelease-floor
description: Glauca-coupled Conduit projects are pinned to prerelease Picea because Picea.Glauca depends on a prerelease Picea floor; stable 1.0.0 there causes NU1605
metadata:
  type: project
---

Conduit cannot move wholesale to stable `Picea` `1.0.0`. Every published
`Picea.Glauca` (0.1.12–0.1.14 as of 2026-05-06) depends on a *prerelease*
`Picea` floor (`>= 1.0.22-rc-0001`, later `>= 1.0.27-rc-0002`). Pinning a direct
`Picea` reference to stable `1.0.0` in a project that also references Glauca
produces an `NU1605` package-downgrade error.

The adopted split ("option 1", 2026-05-06): non-Glauca projects stay on stable
`Picea 1.0.0`; the four Glauca-coupled projects — `Conduit.Api`,
`Conduit.Api.Tests`, `Conduit.ReadStore.PostgreSQL`,
`Conduit.ReadStore.PostgreSQL.Tests` — pin direct `Picea` to the Glauca floor
`1.0.27-rc-0002`.

**Why:** This is a scoped compatibility bridge, not a preference. The exit
condition is upstream: a `Picea.Glauca` release whose dependency floor points at
a stable `Picea`. Until then the prerelease pin is the only way to get a green
restore for those projects.

**How to apply:** Before "upgrading Conduit to stable Picea", check whether
Glauca still requires a prerelease floor — if it does, the upgrade is blocked
and the answer is to wait or roll back, not to force the pin. Keep the boundary
tight: a prerelease `Picea` pin leaking into a non-Glauca project is a mistake.
Related: [[nu1605-transitive-floor-is-ship-blocker]] and
[[prerelease-pin-acceptance-criteria]] in the Reviewer's memory.

Verified 2026-09-02: the four projects above reference
`Picea 1.0.27-rc-0002` + `Picea.Glauca 0.1.14`; `Picea.Abies` and
`Picea.Abies.Conduit` reference `Picea 1.0.0`. The bridge is still in place.
