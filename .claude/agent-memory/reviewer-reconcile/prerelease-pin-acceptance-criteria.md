---
name: prerelease-pin-acceptance-criteria
description: A temporary prerelease version pin is acceptable only when all four conditions hold — scoped, isolated, restore-green, and documented with exit criteria
metadata:
  type: feedback
---

A temporary prerelease floor pin is acceptable as a scoped compatibility bridge
when **all four** hold:

1. Prerelease direct pins are limited to the coupled projects only.
2. Every other project stays on the stable release.
3. Restore is verified green — no `NU1605`.
4. Migration docs include explicit rollback and exit criteria.

**Why:** Ruled on 2026-05-06 for the Picea/Glauca bridge. The risk with a
"temporary" pin is not the pin, it is that nobody records what would end it, so
it becomes permanent by default. Conditions 1 and 2 keep the blast radius
auditable; 4 is what makes "temporary" mean something.

**How to apply:** Check all four explicitly and name any that fail — a pin
meeting three of four is not "mostly fine", it is missing the control that
matters. Condition 4 in particular is the one most often absent, and its absence
is a legitimate changes-requested item on its own.

Related: [[nu1605-transitive-floor-is-ship-blocker]],
[[picea-glauca-prerelease-floor]].
