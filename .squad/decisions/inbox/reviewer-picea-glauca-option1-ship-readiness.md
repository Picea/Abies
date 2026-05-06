# reviewer-picea-glauca-option1-ship-readiness

Date: 2026-05-06
Owner: Reviewer

## Decision

Option 1 (temporary Glauca compatibility pin) is shippable for the migration objective.

## Why

1. Prerelease direct `Picea` pin is scoped only to Glauca-coupled projects.
2. Non-Glauca projects remain pinned to stable `Picea` `1.0.0`.
3. Solution restore succeeds and no `NU1605` downgrade blocker remains.
4. Migration documentation explicitly labels the strategy as temporary and includes concrete exit criteria.

## Guardrails

1. Treat `1.0.27-rc-0002` as temporary compatibility debt.
2. Remove temporary pins once a Glauca release supports stable `Picea` `1.0.0` or Glauca coupling is removed from Conduit API/read-store/test paths.
3. Keep this migration slice focused; unrelated CI/workflow/test-infra changes should ship in separate PRs.
