---
name: level-choice-framework-internals
description: For Abies framework-internal features (library types in Picea.Abies with no adopting app), the spec level is workflow-direct through a real in-process Runtime — not the Aspire AppHost, not Playwright
metadata:
  type: project
---

For a feature that is a **library type inside `Picea.Abies`** with no adopting demo, template or
head, the spec level is **workflow-direct, driven through a real in-process `Runtime`**.

**Why:** the AppHost rule in `.claude/docs/decisions.md` § *Aspire AppHost Is the Test Fixture
(Amended 2026-09-02)* governs **end-to-end / cross-service** tests — a user journey or a service
against its real dependencies. A framework library has no service and no dependency, so the rule
does not reach it, and starting KurrentDB + PostgreSQL to exercise a pure composition would hide
the mechanism behind two containers. The fast-in-memory named exception is a *different* thing
(`WebApplicationFactory` / `TestServer`) and is a closed list — it is not what this is.

But a **pure static-function test is not enough either**: `Runtime.Render` is where subscriptions
are reconciled (`Runtime.cs:212-220`, asking `TProgram.Subscriptions(state)` at `:214`), so any
invariant about subscription activity is only falsifiable with a real `Runtime` running. The
precedent for driving one in-process is `Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs`.

**How to apply:** state both halves of the argument in the Test Strategy Room — why not the AppHost
*and* why not pure functions. Naming only the first reads as dodging the convention.

Two mechanical constraints that recur at this level:
- **No sleeps, ever.** Commit `ca2519d` removed wall-clock tests from this repo.
  `Runtime.cs:288-291` discards the `ValueTask` from a subscription dispatch, so ordering after a
  delivery needs a `TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)`
  completed from inside the program under test, awaited with `WaitAsync(timeout)` — precedent at
  `RuntimeIsolationAndSubscriptionFaultTests.cs:38,53`.
- **Test programs are `static`**, so any per-test recorder must be `AsyncLocal`-flowed or the tests
  must be `[NotInParallel]`. TUnit parallelises by default.

Related: [[spec-project-layout-abies]]
