# reviewer-picea-1.0.0-review

Date: 2026-05-06
Owner: Reviewer

## Decision

The current working-tree migration for `Picea` `1.0.0` is **not shippable** and must not merge as-is.

## Blocking Facts

1. Conduit restore fails with `NU1605` downgrade errors because Glauca requires prerelease `Picea` floors while direct references are pinned to `1.0.0`.
2. The change set mixes migration concerns with unrelated CI policy and large visual-regression infrastructure additions, making risk and rollback scope unclear.
3. New dependencies (`Microsoft.Playwright`, `SixLabors.ImageSharp`) were introduced without recorded dependency-approval evidence required by principles enforcement.

## Required Next Step

Split into focused deliverables:

1. **Migration-only branch/PR** that contains package/docs/changelog/version updates and restores cleanly.
2. **CI policy branch/PR** for E2E trigger/gating changes, with explicit approval for PR-gate removal.
3. **Visual regression branch/PR** for test harness + snapshots + workflow, with dependency approval and baseline maintenance policy.

## Compatibility Remediation Options

1. Publish `Picea.Glauca` compatible with stable `Picea 1.0.0` and keep direct pins at `1.0.0`.
2. Temporarily align direct pins to the Glauca transitive floor (`>= 1.0.27-rc-0002`) until compatible Glauca ships.
3. Remove/replace Glauca usage in affected Conduit API/test paths with in-repo abstractions.
