---
name: namespace-plan
description: Bounded contexts inside Picea.Abies and how they relate — the map maintained across design passes, including contexts proposed but not yet built
metadata:
  type: project
---

The bounded-context map for `Picea.Abies`. Nobody else maintains this; each pass adds a row.

| Context (folder = namespace) | Owns | Depends on | Depended on by |
|---|---|---|---|
| `Picea.Abies` (root) | `Message`, `Command`/`Commands`, `Program`/`ProgramCore`/`ProgramView`, `WithView`, `Runtime`, `Url` | Picea package | everything |
| `Picea.Abies.DOM` / `.Html` | documents, nodes, patches, diffing, handlers | root | root (`Runtime.Render`) |
| `Picea.Abies.Subscriptions` | subscription declaration + reconciliation | root | root (`Runtime.Render`) |
| `Picea.Abies.Debugger` | timeline, cursor, capture, replay — **all `#if DEBUG`** | root | root, under `#if DEBUG` only |
| `Picea.Abies.History` | **proposed 2026-09-06, rev. 4 2026-09-07** (`undo-redo` pass): `History<TModel>`, `Step<TModel>`, `EdgeState`, `Movement`, `Origin`, `MovementAvailability`, `Direction`, `HistoryMessage`, `HistoryEvent.Enveloped`, `SensitiveCause`, `HistoryRedacted`, `HistoryPolicy`, `WholeModelHistoryPolicy`, `WithHistory` | root, plus `Picea.Abies.Subscriptions` for the **`Subscription` return type only** — rev. 3 deleted the framework-declared settle source, so no `Batch`/`SubscriptionModule.Create` use remains | nothing — opt-in by composition at the application's `Runtime.Start` call |

**Why:** namespaces here are bounded contexts, not abbreviations (`decisions.md` §
"Namespaces Are Bounded Contexts", "Domain Terms Only"). A context is named for the domain it
owns, not for the gesture it enables — `History`, not `Undo`.

**How to apply:** before proposing a new context, check whether an existing one already owns
the concept, and state the new context's relationship to every neighbour in the plan's
Namespace Plan section. Two standing facts worth reusing: (1) head adapters (`.Browser`,
`.Server`, `.WinUI`) are generic over `TProgram, TModel, TArgument`, so a new
`Program`-shaped context needs no head changes; (2) `Picea.Abies.Debugger` is `#if DEBUG` in
its entirety and nothing in it is safely reusable in release — `RingBuffer<T>` in particular
is a **mutable class** and cannot back an immutable model value. (3) `Picea.Abies.csproj:24-28`
already declares `InternalsVisibleTo` for `Picea.Abies.Tests` and `Picea.Abies.Benchmarks`, so
internal constructors on a new public model type cost no csproj change — reach for them rather
than accepting a *Make Illegal States Unrepresentable* deviation.

Related: [[higher-order-program-step-shape]], [[picea-package-xml-answers-kernel-questions]].
