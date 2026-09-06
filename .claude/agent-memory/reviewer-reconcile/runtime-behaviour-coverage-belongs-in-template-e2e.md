---
name: runtime-behaviour-coverage-belongs-in-template-e2e
description: Coverage for "does this actually wire up at runtime in a generated app" belongs in Picea.Abies.Templates.Testing.E2E, which already drives a real browser
metadata:
  type: feedback
---

When a review blocker is "prove this works at runtime in a generated app", the
right home is `Picea.Abies.Templates.Testing.E2E/ServerTemplateTests.cs`. That
project already scaffolds a generated `abies-server` app and drives it in a real
browser, so it can assert runtime startup side effects rather than only static
asset availability.

**Why:** Decided 2026-03-27, when a Reviewer blocker on InteractiveServer
debugger coverage needed somewhere to live. The shape of the proving assertion:
wait for `/_abies/debugger.js` to return `200`, then verify
`#abies-debugger-timeline[data-abies-debugger-adapter-initialized="1"]` exists
and `window.__abiesDebugger.enabled` is `true`. That catches import-path
regressions in `abies-server.js` without reopening locked implementation files.

**How to apply:** Do not accept a new unit test or a file-existence assertion as
the answer to a runtime-wiring blocker, and do not ask for a new E2E project —
point at this one. The generalisable pattern is: assert the *observable side
effect* of the wiring (a DOM marker, a global, a served response), not the
presence of the code that is supposed to produce it. Companion rule:
[[dynamic-imports-need-served-asset-proof]].

Verified 2026-09-02: `ServerTemplateTests.cs` still contains these assertions.
