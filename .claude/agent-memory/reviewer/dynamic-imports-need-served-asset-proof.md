---
name: dynamic-imports-need-served-asset-proof
description: Every new dynamic import must be checked against the paths the package actually serves — an unserved import is a silent no-op, not an error
metadata:
  type: feedback
---

When reviewing a change that adds a dynamic `import()`, verify the target
against the static asset paths the serving package actually publishes. Adding a
relative import under `/_abies/` without also adding the asset — or a test that
proves it is served — creates a silent no-op regression.

**Why:** From the 2026-03-27 InteractiveServer debugger bootstrap review. A
failed dynamic import in a best-effort bootstrap is caught and ignored by
design, so the feature quietly does nothing while every build and unit test
passes. Static-asset existence checks in the source tree are not proof either:
the file can exist in the repo and not be published by the package.

**How to apply:** Two questions on any new dynamic import — "which package
publishes this path?" and "what test would fail if it stopped being served?".
If the answer to the second is "none", that is the changes-requested item. See
[[runtime-behaviour-coverage-belongs-in-template-e2e]] for where the proving
test goes.
