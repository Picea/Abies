# 🔍 Track B — Informed Design

Pass: `undo-redo`. Written without sight of `01-track-a.md`.

---

## Landscape Survey

### Known approaches

Undo/redo has four families in production use, and the whole design space of this pass
falls inside them.

1. **State-snapshot history (past / present / future zipper).** The container holds a list
   of prior whole-states, the current state, and a list of undone states. Undo slides the
   present one step back. Used by [elm-community/undo-redo's `UndoList`](https://github.com/elm-community/undo-redo)
   and by [redux-undo](https://github.com/omnidan/redux-undo), which describes itself
   precisely as a *higher order reducer* — a wrapper that turns a state container into a
   history-carrying state container. The state it exposes is `{past, present, future,
   _latestUnfiltered, group, index, limit}`.
2. **Inverse-command / undoable-edit stack.** Each user action is reified as an object with
   `redo()` and `undo()`. Canonical in [Qt's `QUndoStack`/`QUndoCommand`](https://doc.qt.io/qt-6/qundostack.html),
   Cocoa's [`NSUndoManager`](https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/UndoArchitecture/Articles/UndoManager.html),
   Swing's `UndoManager`/`UndoableEdit`. The academic root is
   [Berlage, *A selective undo mechanism for graphical user interfaces based on command
   objects*, ACM TOCHI 1(3), 1994](https://dl.acm.org/doi/10.1145/196699.196721).
3. **Event-sourced replay from a checkpoint.** Keep the log of inputs; to move backwards,
   re-derive by replaying the log from a checkpoint with side effects suppressed. This is
   [Fowler's Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) and how
   Redux DevTools' `recomputeStates` implements action-skipping. It is also, structurally,
   what `Picea.Abies/Debugger/` plus the `replay` flag already are.
4. **Operation-transform / CRDT undo** for concurrent editing —
   [Sun, *Undo as concurrent inverse in group editors*, TOCHI 9(4), 2002](https://dl.acm.org/doi/10.1145/586081.586085),
   [Yjs `UndoManager`](https://docs.yjs.dev/api/undo-manager). Out of scope here (Abies has
   one kernel, one client, no concurrent writers), but its literature is the reason to be
   cautious about branching histories — see Known Failure Modes.

Editors sit on top of one of these and add **grouping**: ProseMirror's `history()` defaults
to `depth: 100`, `newGroupDelay: 500` ms and exposes `closeHistory(tr)` to force a break
([source](https://raw.githubusercontent.com/ProseMirror/prosemirror-history/master/src/history.ts));
CodeMirror 6 and Yjs use the same 500 ms coalescing window (Yjs `captureTimeout`, default
500 ms, `stopCapturing()` to break).

### Prior art in this codebase

- **ADR-025 (Issue 160 Debugger Boundary Contract)** — the governing prior decision.
  It names the four runtime seams, keeps timeline/replay domain in `Picea.Abies`, and
  requires that *stripping debugger features does not alter normal runtime behavior or
  public runtime contracts*. It explicitly excludes "time-travel mutation tools that alter
  historical events" from Phase 1. Open question 5 is a direct question about that clause.
- **Pattern `runtime-seams-anchor-replay-gating`** — four seams: `_apply`,
  `InterpretCommand`, `SubscriptionManager.Start`/`Update`, `navigationExecutor`. Live
  revisit trigger: *a design needing a fifth seam is an architecture change worth an
  explicit decision.* Which seams each candidate touches is the sharpest discriminator
  between them, so I report it per candidate.
- **Pattern `debugger-domain-stays-in-core`** — history and cursor decisions live in C#,
  never in `debugger.js`; enforced by forbidden-token contract tests in
  `Picea.Abies.Browser.Tests`. All three candidates below honour this by construction.
- **ADR-008 (Immutable state)** — records + `with` throughout. This is what makes snapshot
  retention affordable (structural sharing), and it is load-bearing for B1.
  Its Consequences line 85 was deliberately left unedited for this pass
  (`.claude/docs/decisions.md`, entry `lead-20260906T150129Z-undo-redo-adr-008-left-as-is`);
  I note it here because Track B is expected to reach a snapshot-shaped answer *with*
  evidence, and the convergence phase needs to be able to tell that apart from Track A
  reaching it by reading the line.
- **ADR-024 (four render modes)**, **ADR-006 (commands)**, **ADR-007 (subscriptions)**,
  **ADR-028 (ProgramCore/ProgramView split)**.
- **Decision `2026-03-29`** — polymorphic DU model roots need explicit `JsonPolymorphic`
  metadata or snapshot round-trip silently no-ops. Binds any candidate that serializes.
- **Decision `2026-04-01`** — js-framework-benchmark at a 5% regression threshold.

### Codebase facts that decide between the families

Verified by reading, 2026-09-06:

- `Message` is an open marker interface (`Picea.Abies/Message.cs:3`). Framework-defined
  undo/redo messages are expressible with no kernel change.
- `WithView<TCore, TView, TModel, TArgument>` (`Program.cs:64-85`) **already is a
  higher-order program**: it wraps a core and a view into a `Program<,>` by static
  forwarding. The framework therefore already has the composition shape that redux-undo
  calls a higher-order reducer. This is the single most important local fact in this pass.
- Subscription reconciliation is derived from the model *inside* `Render`
  (`Runtime.cs:212-220`), once per settled state.
- `Runtime.Dispatch` (`:441-513`) takes one message, decides N events, applies them, and
  the debugger captures **once, after all of them** (`:502-510`). One dispatch = one
  settled model.
- `DispatchFromSubscription` (`:288-291`) funnels into the same `Dispatch`. There is
  currently **no provenance parameter** — the runtime cannot tell a timer tick from a click
  once inside `Dispatch`.
- `InterpretCommand` returns `[]` when `replay` is true (`:343-346`); `replay` is a
  **constructor-time** flag, not a mode a live runtime can enter.
- `TrySetCoreState` (`:581-615`) writes the automaton's state by reflection over `State`,
  then `<State>k__BackingField`, then any field of type `TModel`, swallowing all exceptions
  and degrading to render-only.
- `Picea` is an **external NuGet package** (`Picea.Abies.csproj:13`, `Version="1.0.0"`).
  `AutomatonRuntime.State` is not ours to make settable. Any candidate that needs to inject
  state into the kernel needs either an upstream Picea API change or the reflection hack.
- The browser head exposes keyboard events only as element-level handlers
  (`abies.js:128-129, 422-423`); there is no application-level global key stream. A
  `Ctrl+Z` affordance is a new subscription source, not an existing capability.

### External references

- elm-community/undo-redo — https://github.com/elm-community/undo-redo
- redux-undo (README + FAQ) — https://github.com/omnidan/redux-undo · https://redux-undo.js.org/main/faq
- Redux "Implementing Undo History" recipe — https://redux.ruanyifeng.com/recipes/ImplementingUndoHistory.html
- prosemirror-history source (defaults) — https://raw.githubusercontent.com/ProseMirror/prosemirror-history/master/src/history.ts
- Y.UndoManager — https://docs.yjs.dev/api/undo-manager
- Qt `QUndoStack` — https://doc.qt.io/qt-6/qundostack.html
- Cocoa Undo Manager / `groupsByEvent` — https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/UndoArchitecture/Articles/UndoManager.html · https://blog.mbcharbonneau.com/2006/10/07/automatic-grouping-in-nsundomanager/
- Fowler, *Event Sourcing* — https://martinfowler.com/eaaDev/EventSourcing.html
- Berlage, TOCHI 1994 — https://dl.acm.org/doi/10.1145/196699.196721
- Sun, *Undo as concurrent inverse in group editors*, TOCHI 2002 — https://dl.acm.org/doi/10.1145/586081.586085
- Huet, *The Zipper*, JFP 7(5) 1997 — the past/present/future triple is a list zipper.
- Driscoll, Sarnak, Sleator, Tarjan, *Making Data Structures Persistent*, JCSS 1989 — the
  cost model for snapshot retention over persistent structures.
- Nielsen heuristic #3, User Control and Freedom — https://www.nngroup.com/articles/user-control-and-freedom/
- "Native Undo & Redo for the Web" (Chrome team) — https://dev.to/chromiumdev/-native-undo--redo-for-the-web-3fl3
- W3C `UndoManager and DOM Transaction` → superseded by the still-unshipped Undo API —
  https://dvcs.w3.org/hg/undomanager/raw-file/tip/undomanager.html · https://rniwa.github.io/undo-api/

---

## Candidate B1: History as a higher-order program (`WithHistory`)

**Based on:** redux-undo's higher-order reducer; elm-community `UndoList`; the list zipper
(Huet 1997); and this codebase's own `WithView<TCore, TView, TModel, TArgument>`.

### How it works

A new bounded context `Picea.Abies.History` supplies:

- `History<TModel>` — a list zipper: `Past` (bounded), `Present`, `Future`. Immutable
  record, per ADR-008.
- `HistoryMessage : Message` — `Undo`, `Redo`, `Checkpoint`, `Clear`. Ordinary messages;
  they travel the ordinary dispatch path.
- `HistoryOptions` — `Depth`, an entry predicate, an optional coalescing window.
- `WithHistory<TCore, TView, TModel, TArgument> : Program<History<TModel>, TArgument>` —
  the exact analogue of `WithView`, forwarding statically:
  - `View(h) => TView.View(h.Present)`
  - `Decide(h, m)` — `HistoryMessage` decides to itself; everything else delegates to
    `TCore.Decide(h.Present, m)`.
  - `Transition(h, e)` — for `Undo`/`Redo`, slide the zipper and return `Command.None`;
    otherwise `TCore.Transition(h.Present, e)` and push the prior present onto `Past`,
    clearing `Future`.
  - `Subscriptions(h) => TCore.Subscriptions(h.Present)`.
  - `IsTerminal(h) => TCore.IsTerminal(h.Present)`.

The application opts in by starting `Runtime<WithHistory<CounterProgram, CounterView,
CounterModel, Unit>, History<CounterModel>, Unit>`. Nothing in the runtime changes.

**The history is application state.** That is the whole idea, and it is what redux-undo and
`UndoList` both do: since the state container already owns transitions, give it a state
whose transitions include undo.

### Answers to the five open questions

1. **Unit.** One accepted, history-eligible **incoming message** — equivalently, one
   `Runtime.Dispatch` call — produces one entry, storing the model value that resulted.
   Evidence: `NSUndoManager.groupsByEvent` defaults to true and groups per run-loop pass,
   so *"when the user clicks a button or hits a key, it creates one entry in the undo
   stack, even if the application performs several tasks behind the scenes"* — one user
   gesture, one entry, regardless of internal fan-out. Abies' fan-out is exactly this:
   `Decide` may return N events, all applied before the model settles
   (`Runtime.cs:492-500`). redux-undo's unit is the action; `UndoList`'s is the `New a`
   message. Optional coalescing sits *on top*, with the 500 ms window that ProseMirror,
   CodeMirror and Yjs independently converged on, exposed as `HistoryOptions.CoalesceWindow`
   and defaulting to **off** (text editing needs it; a counter does not).
2. **Retention.** Bounded ring, default depth **100**, matching ProseMirror `depth: 100` and
   CodeMirror `minDepth: 100`. Qt (`undoLimit` default 0) and redux-undo (`limit` default
   `false`) both default to unbounded and both have a long tail of memory complaints as a
   result; the codebase's own debugger default of 10 000 is a debug-tool number, not a
   shipping one.
3. **Acting after undo.** The forward history is **discarded**. Every linear-undo system in
   the survey does this — redux-undo clears `future`, `UndoList.new` drops the future, Qt's
   `push()` deletes commands above the current index, `NSUndoManager` clears the redo stack
   on the next registration. It is also the only branch policy that keeps **INV-2** cheap:
   a retained branch plus redo after divergence is *selective undo*, and Berlage 1994 and
   the group-editor literature exist because making that land on a reachable state is hard.
4. **Exemptions.** Two composable mechanisms, both with precedent:
   - *By model shape* — wrap only part of the model. redux-undo's `undoable()` wraps a
     slice of the reducer tree; state outside the slice is exempt by construction. In Abies
     this is `History<TUndoable>` beside a non-undoable remainder, or a
     `HistoryOptions.Project` lens.
   - *By provenance* — Yjs `trackedOrigins` (default: local, un-originated changes only),
     redux-undo `filter`, ProseMirror's exclusion of `appendTransaction` results. The Abies
     analogue requires a small runtime change: `Dispatch` gains an origin so
     `DispatchFromSubscription` can mark its messages non-eligible. **Default exempt:
     subscription-originated messages, effect-result messages, `UrlChanged`, and transient
     UI state** (in-flight flags, focus, hover).
   `UrlChanged` deserves its own line: the browser already owns a history stack, and
   double-driving it is the classic collision (see Known Failure Modes).
5. **Debugger machinery.** **None of it moves.** B1 needs no snapshot serializer
   (`GenerateModelSnapshot` / `DeserializeDebuggerModelSnapshot`), no `TrySetCoreState`, no
   ring buffer of boxed models, no `debugger.js`. It sets kernel state the only supported
   way: by being the kernel's state. ADR-025's release-strip clause therefore **holds
   unamended** — B1 does not cross it. `Picea.Abies/Debugger/` stays exactly as it is.

### Subscriptions and timers — the hard requirement

B1 satisfies it **vacuously, not by suppression**. There is no traversal: undo is a single
transition from the present model to the restored model. No intermediate model is ever
constructed, rendered, or passed to `TProgram.Subscriptions`. Reconciliation runs once, in
`Render`, on the settled state (`Runtime.cs:212-220`), which the scope leaves open. No seam
is touched, no gate is added, and the `runtime-seams-anchor-replay-gating` revisit trigger
does **not** fire.

One honest caveat: a subscription that was running before the undo and is not desired after
it is stopped and, if a later redo restores it, restarted fresh — a `Every(1 s)` timer
resets its phase rather than resuming. That is a consequence of subscriptions being derived
from the model, not of this design, and is the same behaviour as any ordinary model change
that toggles a subscription.

### Effects, including those already gone

- Undo/redo transitions return `Command.None`. No effect is re-issued. This matches
  every system surveyed; it is also Fowler's gateway rule stated the easy way — there is no
  replay, so there is nothing for a gateway to suppress.
- **Effects already dispatched are not recalled.** This is the documented contract, and it
  is the industry's answer, not a shortcut: Redux DevTools' well-known limitation is
  exactly this, and Fowler's remedy for irreversible external interaction is a
  *compensating* action appended forward, not a rewind.
- Two opt-in escapes, both with production precedent:
  - *Compensation* — a history entry may carry a `Command` to emit on undo. Fowler's
    reversal (which requires difference-shaped events and *"the event should ensure it
    stores everything needed for reversal during processing"*); Qt's
    `QUndoCommand::undo()`. Off by default; the app supplies it.
  - *Deferral* — for genuinely irrevocable actions the correct pattern is not undo at all
    but a delay window: Gmail's Undo Send is a 5–30 s hold, not a reversal. Route this
    through the ordinary command/subscription mechanism.
- **In-flight results racing an undo.** `_core.Dispatch` is awaited *outside*
  `_decisionGate` (`Runtime.cs:448-500`), so an effect result can land after an undo. Under
  B1 it is applied to the post-undo present, which is correct (it is a real event about the
  world), and by rule 4 it creates no history entry.

### Which heads

**InteractiveServer, WASM, Native.** All three run the dispatch loop and all three get the
identical implementation, because B1 lives entirely in `Picea.Abies` and touches no
platform adapter. **Static HTML is excluded**: it has no live message loop, so there is no
interaction to undo. The `Ctrl+Z` / `Cmd+Z` *affordance* is per-head and is **not** part of
B1's core — see the UX flag below; the native head in particular must not steal `Ctrl+Z`
from a focused `TextBox`, which has its own undo.

### Memory over a long session

O(depth) retained model versions with structural sharing over immutable records — the
persistent-structure cost model of Driscoll et al. 1989, i.e. per-entry cost is the changed
path, not the whole model. Bounded at `Depth` (default 100), so the ceiling is reached and
held; a long session does not grow the history. The honest hazard: a retained snapshot
**pins the whole object graph it references**, so a model holding a large fetched list keeps
that list alive for up to `Depth` versions. Mitigated by `Depth` and by exempting bulk
caches from the undoable projection (question 4).

### Size and performance cost

Handful of small types, no serializer, no reflection, no JS asset. Expected well under
1 MB — which the 15 MB/10 MB integer-MB gate cannot see, so **that gate is not evidence of
freeness** (the knowledge scan makes the same point). Per-message overhead: one record
allocation plus one list push. `performance-engineer` should measure per-assembly IL size
and run js-framework-benchmark against the 5% threshold, because history is on the hot
dispatch path even for apps that never press undo — the mitigation is that `WithHistory` is
opt-in composition, so apps that do not wrap pay nothing at all.

**Trade-offs, from the literature:** the state-snapshot family cannot do selective undo
(Berlage's stated limitation), cannot persist history without serializing the model, and
puts model-shaped memory pressure where a command stack would put delta-shaped pressure.
It also changes the application's model type, which is a breaking-shaped adoption cost for
existing programs (mitigated: opt-in, and `WithView` already establishes the wrapper idiom).

---

## Candidate B2: Event-sourced replay from a checkpoint

**Based on:** Fowler's Event Sourcing (replay + gateways + snapshot checkpoints); Redux
DevTools' `recomputeStates`; and this codebase's existing `Picea.Abies/Debugger/` + `replay`
machinery, which is already 80% of this candidate.

### How it works

Retain the **log of decided events** (not models) plus periodic model checkpoints. To undo
to step *k*: take the nearest checkpoint at or before *k*, construct a replay-gated kernel,
feed it events *checkpoint..k*, and adopt the resulting model as the live state. The
existing four seams supply the gating: `InterpretCommand` returns `[]`
(`Runtime.cs:343-346`), subscription reconciliation is skipped (`:212`),
`DispatchFromSubscription` is a no-op (`:288-291`), navigation is inside `InterpretCommand`
so it is gated too (`:368`).

This is the ADR-025 lineage answer, and it is the only candidate that meets the scope's
subscription requirement by *actually suppressing* rather than by not traversing.

### Evidence

- Fowler on exactly this problem: *"if these events cause update messages to be sent to
  external systems, then things will go wrong because those external systems don't know the
  difference between real processing and replays"*, resolved by a gateway that has *"a
  reference to the event processor and checking whether it's in replay mode before passing
  the external call off to the outside world."* `InterpretCommand(replay: true)` **is that
  gateway**, already built. The convergence between Fowler's prescription and
  `Runtime.cs:337-346` is close to line-for-line.
- Fowler also names the second-order problem this candidate inherits and B1 does not:
  external *queries*. *"If I ask for an exchange rate on December 5th and replay that event
  on December 20th, I will need the exchange rate on Dec 5 not the later one."* The remedy
  is logging every external query response — a substantial addition.
- Redux DevTools recomputes state by replaying retained actions from a committed base; the
  approach is proven at scale as a *developer* tool.
- Event logs are small and serializable, which is why this is the only candidate that
  offers **durable** history: undo surviving a reload or a reconnect on the server head.

### What it costs here

- `replay` is **constructor-time** (`Runtime.cs:317-335`). B2 needs it to become a mode a
  live runtime enters and leaves, or needs a second shadow runtime whose result is injected
  into the live one. The first is a change in kind to the gating at seams 2–4; the second
  lands back on `TrySetCoreState`. Either way the
  `runtime-seams-anchor-replay-gating` revisit trigger **fires** and this needs an explicit
  architecture decision, not an incidental addition.
- `Picea` is an external package, so `AutomatonRuntime.State` cannot simply be made
  settable in this repo. `TrySetCoreState` (`:581-615`) is reflection-based, trim/AOT
  hostile, catches everything, and falls through to render-only — it *silently* violates
  INV-3 and INV-4 rather than failing loudly. Promoting it to release is the thing ADR-025
  never contemplated.
- Durable history additionally requires serializing events, which drags in the
  `2026-03-29` `JsonPolymorphic` obligation and pushes it onto every consuming application.
- Undo is O(distance-to-checkpoint) rather than O(1).

### Answers to the five questions (where they differ from B1)

1. **Unit** — the decided **event**, which is finer than B1's message. `Decide` may return
   several events per message, so the log needs message-boundary markers to present a
   coherent unit to the user; without them the granularity mismatch noted in the knowledge
   scan bites INV-3 directly.
2. **Retention** — checkpoint interval × log depth; the classic event-sourcing snapshot
   tuning problem. Smaller retained bytes than B1, more retained complexity.
3. **After undo** — same discard rule; nothing in the family changes the argument.
4. **Exemptions** — by event type, filtered at log-append. Weaker than B1's structural
   projection, because an exempt event still has to be *skipped* during replay, and skipping
   an event that a later event depends on is exactly the INV-2 hazard.
5. **Debugger machinery** — this candidate **is** the debugger machinery promoted to
   release. Answer to question 5 is a loud "yes", and it needs ADR-025's release-strip
   clause superseded in part, plus a supported replacement for `TrySetCoreState`.

**Heads:** all four minus static, as B1. The server head benefits most (durable history
across reconnects); the WASM head pays most (serializer + trim metadata against the size
budget).

---

## Candidate B3: Inverse-command stack (`QUndoStack` / `NSUndoManager` shape)

**Based on:** Berlage TOCHI 1994; Qt `QUndoStack`/`QUndoCommand`; Cocoa `NSUndoManager`;
Swing `UndoableEdit`.

### How it works

Each undoable action registers an inverse. In Abies terms, the application supplies
`Undoable(Message forward, Message inverse)` — or the framework derives one from a
difference-shaped event, per Fowler's *"add $10"* rather than *"set to $110"*. Undo
dispatches the inverse through the ordinary path; redo dispatches the forward again.
Merging of adjacent edits is Qt's `mergeWith()` with a command `id`; the clean/saved marker
is `setClean()`/`cleanChanged()`, which is the standard way to render a "document modified"
indicator and has no analogue in B1 or B2 without extra work.

### Evidence

Three decades of desktop toolkits; it is the default shape for native applications and
therefore the *most familiar* one on the native head. Memory is O(number of actions ×
delta), not O(depth × model) — the best long-session profile of the three. It is the only
candidate that can support **selective undo** later (Berlage's whole subject).

### Trade-offs, from the literature

- Correctness moves into application code. Every inverse is an opportunity to produce a
  state no forward sequence could reach — a direct **INV-2** hazard, and unlike B1 the
  framework cannot enforce it. Berlage's paper is careful about exactly this.
- Redo-by-re-dispatch re-issues the forward message's **effects**, so redo can re-POST.
  Suppressing that needs a new gate at the dispatch boundary — a **fifth seam**, so the
  revisit trigger fires here too.
- `NSUndoManager`'s own hard-won lesson is that per-registration granularity is too fine
  for users, which is why `groupsByEvent` groups by run-loop pass by default. Any B3 here
  inherits the grouping problem that B1 gets for free from the dispatch boundary.
- It is the least idiomatic of the three for this codebase: Abies has no command-object
  layer, and adding one duplicates what `Message` + `Decide` already are.

I include B3 because it is the answer the native head's platform conventions expect and
because it dominates the others on memory; I do not rank it first, on INV-2 grounds.

---

## Ranking within Track B (🏛️ Cleanness)

**B1 > B2 > B3.**

B1 is the only candidate that adds no seam, needs no gate, needs no reflection, needs no
serializer, and satisfies the subscription/timer requirement structurally rather than by
suppression — because it never moves *through* states. It is also the one that composes with
`WithView` rather than beside it. B2 is the most powerful (durable history, session
recovery) and the most costly (fifth-seam gating, `TrySetCoreState`, serialization
obligations, ADR-025 partially superseded). B3 has the best memory profile and the worst
invariant guarantees.

---

## Known Failure Modes

Drawn from what has actually gone wrong for people who took these approaches.

1. **Unbounded history is a slow memory leak.** Qt's `undoLimit` and redux-undo's `limit`
   both default to unbounded; retained snapshots pin whole object graphs. ProseMirror and
   CodeMirror both chose a bound (100) instead. *Mitigation:* bound by default; make
   unbounded the opt-in.

2. **The application's undo fights the platform's undo.** A global `Ctrl+Z` handler in a
   web app collides with the browser's native text-input undo stack: preventing default on
   `beforeinput`/`historyUndo` empties the native stack so later undos never fire, and the
   two stacks interleave incoherently
   ([Chrome team writeup](https://dev.to/chromiumdev/-native-undo--redo-for-the-web-3fl3),
   [w3c/editing#150](https://github.com/w3c/editing/issues/150)). The identical hazard
   exists on the native head, where `TextBox` has its own undo. *Mitigation:* the framework
   ships the history, **not** the key binding; per-head affordances are the app's, scoped
   away from focused text-editing controls. Flag for `ux-expert`.

3. **The platform never solved this generically, and that is informative.** W3C's
   `UndoManager and DOM Transaction` spec did not ship; its successor
   [Undo API](https://rniwa.github.io/undo-api/) is still a preliminary draft after a
   decade. Every shipping app — Figma, Google Docs, VS Code — implements undo internally.
   Do not design toward a platform primitive arriving.

4. **Time travel does not undo side effects, and users will assume it does.** The canonical
   Redux DevTools limitation: jumping back in the store leaves the network requests,
   analytics events and writes that already happened. *Mitigation:* state the contract
   explicitly in docs and in the ADR, and provide the compensation/deferral escapes rather
   than pretending.

5. **Replay re-triggers the outside world unless every gateway checks the mode.** Fowler's
   warning, and the reason Abies already gates four seams. The failure is asymmetric: it is
   silent in tests (which usually stub the interpreter) and loud in production. *Any*
   candidate that reintroduces traversal must show all four seams gated, and must show that
   a *fifth* has not appeared.

6. **Undo granularity that matches the code, not the user, is rejected by users.** Per-
   keystroke undo in text editors is the classic case; the industry answer is coalescing,
   and three independent systems (ProseMirror, CodeMirror, Yjs) converged on the same
   500 ms window. Conversely, coalescing too aggressively loses work. *Mitigation:*
   coalescing off by default, configurable, with the 500 ms figure as the documented
   starting point.

7. **Undo that restores the model but not the selection is not perceived as undo.**
   ProseMirror stores selection bookmarks *inside* history events for exactly this reason.
   Caret position, scroll offset, focus, `<details>` open state and uncontrolled input
   values live in the DOM, not the model, so a model-only undo can pass INV-3 as written
   and still fail it as *"the same rendered document and the same subsequent behaviour"*.
   This is the sharpest INV-3 risk in the pass and it is not visible from the model type.
   *Mitigation:* either declare DOM-owned state out of scope explicitly, or carry a focus/
   selection bookmark per entry. Needs a user decision; flag for `ux-expert`.

8. **Branching histories and selective undo land on unreachable states.** The entire
   group-editor undo literature (Sun 2002; Prakash & Knister 1994; Berlage 1994) exists
   because "undo an action from the middle" is genuinely hard to make correct. A design that
   keeps the forward branch and allows redo after divergence is signing up for that
   problem. *Mitigation:* linear undo, discard on divergence (question 3).

9. **Navigation double-undo.** If `UrlChanged` is undoable and browser back is also live,
   one user gesture can move two histories, or `Ctrl+Z` can appear to navigate.
   `navigationExecutor` sits inside `InterpretCommand` (`Runtime.cs:368`), so a
   compensating navigation command *would* fire on undo under B1's compensation escape.
   *Mitigation:* exempt `UrlChanged` by default (question 4).

10. **Silent degradation is worse than failure.** `TrySetCoreState` catches everything and
    falls back to render-only; `DeserializeDebuggerModelSnapshot` returns `default` with no
    `JsonTypeInfo<TModel>`. Both are acceptable in a debug tool and both would turn INV-4
    ("neither faults, throws, nor leaves the application partly changed") into a lie by
    leaving the view and the kernel disagreeing. Any candidate reusing them must replace
    the degradation with a hard failure or a supported API.

11. **Wrapping the model is an adoption cliff.** redux-undo's most common integration
    complaint is that every selector and every test now goes through `.present`. B1 has the
    same shape: `TModel` becomes `History<TModel>` for the runtime, views, and tests.
    *Mitigation:* `WithHistory` forwards `View` so application views are untouched; the cost
    lands on code that reaches for `Runtime.Model` directly.

---

## Expert-room notes

- 🎨 **UX — spawn `ux-expert`.** Question 1 (what a user believes one step is), question 3
  (behaviour after undo), failure mode 7 (selection/focus restoration), failure mode 2
  (key bindings across four heads, including WinUI's built-in `TextBox` undo). This is the
  room with the most to say and I have represented it thinly on purpose.
- ⚡ **Performance — spawn `performance-engineer`.** Per-message overhead on the hot
  dispatch path even when undo is never used; retained-graph pinning; and the fact that the
  integer-MB size gate cannot resolve a sub-megabyte addition, so it must not be reported as
  a clean bill of health.
- 🔀 **Concurrency — in-room.** `_core.Dispatch` is awaited outside `_decisionGate`
  (`Runtime.cs:448-500`) and subscriptions dispatch asynchronously, so an undo interleaving
  with an in-flight effect result is real. B1's answer: results apply to the post-undo
  present and create no entry. Worth an explicit property in `06-spec.md`.
- 🗄️ **Data — in-room, conditional.** Only B2's durable variant persists anything; it
  inherits the `2026-03-29` `JsonPolymorphic` obligation and a history schema-versioning
  problem. B1 and B3 persist nothing.
- 🧪 **Test Strategy — in-room.** All four invariants are property-shaped. FsCheck over
  generated `(action | undo | redo)*` sequences gives INV-1 through INV-4 directly; INV-2
  needs a reachable-state oracle, which for B1 is "some pure action sequence yields this
  model" and is cheap to state because the transition function is pure.
- 🛡️ **Security — not summoned.** No candidate crosses a threat boundary; re-summon if the
  durable variant of B2 is chosen, since it would persist application state.

---

## Constraint flag for the orchestrator

Not a request to change the scope, recorded per instruction: **ADR-025's release-strip
contract is only crossed by B2.** If convergence selects B1, the close-out decision drop
should say so explicitly — that ADR-025 was examined and found *not* to require
superseding — because the natural assumption from the pass's framing is the opposite.
