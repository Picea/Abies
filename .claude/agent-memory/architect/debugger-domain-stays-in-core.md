---
name: debugger-domain-stays-in-core
description: Debugger domain logic lives in Picea.Abies; JavaScript is adapter-only and must contain no debugger state machine
metadata:
  type: project
---

The debugger's domain — the timeline, the cursor, snapshot capture and replay —
lives in `Picea.Abies`. JavaScript (`debugger.js`) is an adapter only: it
renders and transports, it does not hold debugger state. Decided 2026-03-23
alongside [[runtime-seams-anchor-replay-gating]].

**Why:** A debugger that keeps its own state in the browser diverges from the
runtime it is supposed to be observing, and the divergence is invisible until
replay produces the wrong model. Keeping one authoritative machine in the core
also means the same debugger works for the WASM, InteractiveServer and native
render modes without three implementations.

**How to apply:** Reject designs that move timeline/cursor/snapshot decisions
into JS. The boundary is enforced by contract tests
(`Picea.Abies.Browser.Tests/DebuggerAdapterTests.cs` and
`DebuggerJavaScriptAdapterContractTests.cs`) that fail if the adapter script
contains tokens such as `DebuggerMachine`, `CaptureMessage`, `StepForward(` or
`GenerateModelSnapshot` — so a violating design will be caught, but late. Catch
it at design time instead.

Verified 2026-09-02: both contract tests still enforce the forbidden-token list.
