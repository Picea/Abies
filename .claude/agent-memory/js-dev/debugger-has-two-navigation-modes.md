---
name: debugger-has-two-navigation-modes
description: debugger.js navigates in either bridge mode or detached mode; every navigation handler must implement both or the UI silently stops responding
metadata:
  type: feedback
---

`debugger.js` has two mutually exclusive navigation modes and every handler —
transport-bar click, scrubber, event-list click, keyboard arrows — must handle
both:

- **Bridge mode.** `invokeRuntimeBridge` talks to C#. It sets
  `detachedImportedSession = false` on entry, so any live bridge call exits
  detached mode.
- **Detached mode.** `applyImportedSession` runs only when no bridge is present
  and sets `detachedImportedSession = true`. Navigation is served locally by
  `navigateDetached(cursor)`, which rebuilds `lastResponse` from `localTimeline`
  and calls `updateUI()`. Back/Next/scrubber stay enabled based on cursor
  position; Play and Clear stay disabled because there is no live runtime.

**Why:** A 2026-03-29 bug shipped because handlers checked
`if (detachedImportedSession)` and showed a notice instead of navigating, while
`updateDisabledStates` disabled Back/Next/scrubber wholesale via
`canControlLiveRuntime = hasBridge && !detachedImportedSession`. The user could
see an imported timeline and could not move through it at all. The failure mode
is silent — the UI just does nothing — which is the same class of problem as
[[debugger-snapshot-failures-are-silent]] on the C# side.

**How to apply:** When adding or changing a debugger navigation affordance, ask
"what does this do with no bridge?" before writing it, and check
`updateDisabledStates` treats detached mode as its own branch rather than
falling through to the live-runtime predicate. There is no test that catches
this; the modes are distinguishable only by manual import-without-bridge.

Verified 2026-09-02: `detachedImportedSession`/`navigateDetached` still present
in `Picea.Abies.Browser/wwwroot/debugger.js`. Edit only that copy — see
[[debugger-js-has-one-source-of-truth]].
