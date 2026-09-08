---
name: a-timeout-attribute-is-not-a-hang-guard
description: TUnit's [Timeout] on parameterless tests warns TUnit0015, misses synchronous spins, and leaves the timed-out body running — which corrupts the next test when the class shares static state.
metadata:
  type: project
---

`[Timeout(n)]` on a TUnit class with parameterless `[Test]` methods buys less
than it looks like it buys. Measured on TUnit 1.19.57, .NET 10:

- **`warning TUnit0015: Missing TimeoutAttribute cancellation token parameter`**,
  one per test method. Abies has no `TreatWarningsAsErrors` in
  `Directory.Build.props`, the test csprojs or any workflow, so it is warnings
  rather than a build break — but a class-level `[Timeout]` on 25 tests is 25
  new warnings.
- **It does fire on `await`-shaped bodies** without a `CancellationToken`
  parameter — `[Timeout(2_000)]` + `await Task.Delay(6_000)` failed at 2 s with
  TUnit's `TimeoutException`.
- **It does not fire on a synchronous spin** — a 6 s busy loop under
  `[Timeout(2_000)]` **passed**.
- **The body keeps running after the failure is reported.** The probe printed
  `PROBE: body completed after 5999 ms` long after the test was recorded failed.

**Why it matters here:** the last point is the one that bites. On a class whose
tests all read and write a process-wide static log, an orphaned body still
dispatching into that log after its own test was declared failed will corrupt
whichever test runs next, turning one timeout into a cascade.
`[NotInParallel]` does **not** prevent it — the orphan is a detached
continuation, not a test the scheduler knows about.

**How to apply:** when a `[Timeout]` is proposed as a hang guard, ask what shape
the hang would have. `await`-dense loops: it works. Anything CPU-bound or
lock-bound: it does not. And if the class shares mutable static state, say
explicitly that a fired timeout leaves the suite in an undefined state, so the
first red is trustworthy and the ones after it are not.

Related: [[a-negative-control-alone-proves-nothing]] — this entry exists because
the probe ran the sync case as well as the async one.
