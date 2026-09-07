---
name: abies-kernel-constraints
description: Load-bearing structural facts about the Abies runtime/kernel that constrain most design passes — verified by reading, 2026-09-06
metadata:
  type: project
---

Facts about `Picea.Abies` that repeatedly decide between design candidates. Verified by
reading on 2026-09-06; re-verify line numbers before quoting them, the file moves.

- **`Picea` is an external NuGet package** (`Picea.Abies/Picea.Abies.csproj`, `Version="1.0.0"`),
  not vendored source. `AutomatonRuntime<...>.State` is therefore not ours to make settable.
  This is why `Runtime.TrySetCoreState` is a reflection hack, and it means any candidate
  that wants to inject state into the kernel needs an upstream change or the hack.
- **`WithView<TCore, TView, TModel, TArgument>` (`Program.cs`) is already a higher-order
  program** — static forwarding of every `ProgramCore` member. The framework has the
  wrapper-composition idiom; reach for it before inventing a new extension point.
- **`Message` is an open marker interface** (`Picea.Abies/Message.cs`), so framework-defined
  messages need no kernel change.
- **`replay` is a constructor-time flag on `Runtime`, not a mode.** All four effect gates
  hang off it. Any design needing a runtime to enter and leave replay is changing that, and
  trips the architect's `runtime-seams-anchor-replay-gating` revisit trigger.
- **Subscription reconciliation happens inside `Render`, derived from the settled model.**
  One reconciliation per settled state — so a design that produces a single transition never
  has to suppress subscriptions at all.
- **`DispatchFromSubscription` funnels into the same `Dispatch` with no provenance.** The
  runtime cannot currently distinguish a timer tick from a click. Adding provenance is a
  small but real change.
- **Debug-only machinery degrades silently.** `TrySetCoreState` catches everything and falls
  back to render-only; `DeserializeDebuggerModelSnapshot` returns `default` without a
  `JsonTypeInfo<TModel>`. Acceptable in a debug tool, disqualifying in a shipping feature —
  say so rather than assuming reuse.
- **The CI size gate is integer-megabyte** (`du -sb` on `_framework`, 15 MB hard / 10 MB
  warn). A sub-megabyte addition is invisible to it. Never report "the gate is green" as
  evidence a design is free; ask `performance-engineer` for per-assembly IL size.

See [[prior-art-undo-redo]].
