# 00-knowledge.md — `undo-redo`

> **Not readable by `dreamer-first-principles`** (`enforce-track-blindness.sh`
> denies `.squad/design/*/00-knowledge.md`). Decision ids, pattern names,
> mechanism descriptions and the weighting rationale live here on purpose.
> `00-scope.md` carries the same constraints restated in plain language.

---

## 📚 KNOWLEDGE SCAN

### Related patterns (architect MEMORY.md)

- **`runtime-seams-anchor-replay-gating`** — replay/effect gating is anchored to four
  seams in `Picea.Abies/Runtime.cs`: the `_apply` patch callback, `InterpretCommand`,
  `SubscriptionManager.Start`/`Update`, and the `navigationExecutor` delegate. Decided
  2026-03-23 for issue #160. The memory carries a live revisit trigger: *"If a design
  needs a fifth seam, that is an architecture change worth an explicit decision, not an
  incidental addition."* **This trigger is likely to fire in this pass** — undo that
  suppresses or compensates effects will either express itself at those four seams or
  ask for a fifth.
- **`debugger-domain-stays-in-core`** — timeline, cursor, capture and replay live in
  `Picea.Abies`; `debugger.js` is a transport/mount adapter only, enforced by
  forbidden-token contract tests in `Picea.Abies.Browser.Tests`
  (`DebuggerAdapterTests.cs`, `DebuggerJavaScriptAdapterContractTests.cs`). Any undo
  design that puts history or cursor decisions in JS will be caught late by those tests;
  catch it at design time.
- **`decider-cutover-overrode-staged-plan`** — calibration only. When risk-staging a
  breaking change in this repo, present *taking the break now* as a real option; that is
  what was chosen last time.

### Related decisions

Architecture decision records:

- **ADR-025** *Issue 160 Debugger Boundary Contract (Phase 1)* — the governing prior
  decision for this pass. Names the runtime seams, puts timeline/replay domain in
  `Picea.Abies`, gates interpreter execution, subscription lifecycle and navigation
  during replay, and mandates a release-strip strategy with the contract requirement
  that *"stripping debugger features does not alter normal runtime behavior or public
  runtime contracts."* Explicitly out of scope in Phase 1: *"time-travel mutation tools
  that alter historical events."* Open question 5 in the scope is a direct question
  about whether this ADR's release-strip clause still holds.
- **ADR-026** *Debugger auto-mount with C# API*.
- **ADR-024** *Four render modes* — static, InteractiveServer, WASM, native.
- **ADR-008** *Immutable state* — mandates records + `with` for all model types.
  ⚠️ **Contamination note:** ADR-008's Consequences section contains the line
  *"Undo/redo: Trivial to implement by storing state snapshots."* That is an answer to
  open question 1 sitting in a file Track A is allowed to read as codebase. Raised for
  gate 1; see the handback note.
- **ADR-006** *Command pattern* (effects), **ADR-007** *Subscriptions*,
  **ADR-028** *ProgramCore/ProgramView split*.

Decision register (`.claude/docs/decisions.md`):

- `2026-03-27` *Core abies.js remains debugger-free in release contract* — the release
  strip contract for the browser asset.
- `2026-03-27` *Browser package ships debugger.js in debug package artifacts*.
- `2026-03-29` *App polymorphic DU roots must declare JsonPolymorphic metadata* — model
  snapshot JSON round-trip requires explicit `[JsonPolymorphic]`/`[JsonDerivedType]` on
  abstract application DU roots; without it, snapshot application silently no-ops. Any
  design that serializes the model inherits this obligation and pushes it onto every
  consuming application.
- `2026-04-04` *Program contract should be decider-shaped* / *Full decider migration —
  breaking contract target* / *Program.Decide return type is `Result<Message[], Message>`*
  — establishes the current two-stage kernel shape.
- `2026-04-01` *CI Runtime Policy* — js-framework-benchmark is the authoritative
  performance gate at a 5% regression threshold.
- `2026-09-02` *Abies diverges from squad-template on the Aspire-AppHost-only test rule*
  — relevant to `spec-author`'s level choice.

### Related sessions

The issue #160 debugger work (2026-03-23 → 2026-03-29) is the only prior pass in this
problem's territory. It produced ADR-025, both architect memories above, and the
`#if DEBUG` machinery this pass must relate to.

### Codebase facts (verified 2026-09-06)

Recorded here so the Realist and Critic do not re-derive them. Track A is expected to
find these itself; that is why they are here and not in the scope.

- **Kernel shape** — `Picea.Abies/Program.cs:15-24`.
  `ProgramCore<TModel, TArgument> : Decider<TModel, Message, Message, Command, Message, TArgument>`
  with `Decide(state, message) -> Result<Message[], Message>`, and
  `Transition(model, event) -> (TModel, Command)` inherited from the automaton.
  Two stages: decision (message → events) then transition (event → model + effect).
  **The unit question in open question 1 has at least four candidate answers already
  present in the kernel** — the incoming message, the decided event, the
  `(model, Command)` pair, and the model value itself.
- **Dispatch** — `Runtime.cs:441-513`. `Decide` under `_decisionGate`, then each decided
  event through `_core.Dispatch`. Debugger capture happens *after* all events are
  applied (`Runtime.cs:502-510`), so one captured entry can correspond to several
  transitions. That granularity mismatch is a real hazard for INV-3.
- **Effect boundary** — `Runtime.cs:334-370`. `InterpretCommand` returns `[]` immediately
  when `replay` is true (`:343-346`); subscription reconciliation is skipped in replay
  (`:212`); `DispatchFromSubscription` is a no-op in replay (`:288-291`);
  `navigationExecutor` is invoked inside `InterpretCommand` (`:368`), so it is gated by
  the same flag. `replay` is a **constructor-time** flag on the runtime, not a mode the
  running application can enter and leave.
- **Existing debug machinery** — `Picea.Abies/Debugger/`, all `#if DEBUG`.
  `DebuggerMachine` holds two fixed-capacity ring buffers (default 10 000) — one of
  `TimestampedEntry` metadata, one of boxed model references. It stores the **live model
  object reference**, not a serialized copy; serialization is used only for the preview
  string and for export/import (`GenerateModelSnapshot`, `Runtime.cs:619-643`). So
  retention cost is structural sharing over immutable records, not copies.
- **State restoration is reflective and release-hostile** — `TrySetCoreState`,
  `Runtime.cs:581-615`, writes the automaton's `State` property, then its
  `<State>k__BackingField`, then any field of type `TModel`, catching everything and
  falling through to a render-only best effort. This is the single largest obstacle to
  promoting any of this machinery into release builds: it is trim/AOT-hostile and
  silently degrades. A release-path undo needs a supported way to set kernel state, or
  must not set it at all.
- **Snapshot deserialization needs source-generated metadata** —
  `DeserializeDebuggerModelSnapshot`, `Runtime.cs:569-579`, returns `default` when no
  `JsonTypeInfo<TModel>` was supplied. Serialization-based designs are not universally
  available.
- **Release size budget** — `.github/workflows/pr-validation.yml:758-794`. Trimmed
  Release publish of `Picea.Abies.Conduit.Wasm`, `_framework` directory measured with
  `du -sb`; hard limit 15 MB (job fails), soft limit 10 MB (warning). Note the gate is
  integer-MB and coarse: a sub-megabyte addition is invisible to it. Do not treat "the
  gate is green" as evidence a design is free.
- **Performance gate** — js-framework-benchmark, 5% regression threshold
  (`.github/workflows/benchmark.yml`). Per user auto-memory the Size and Benchmark
  checks are non-required, so a regression there does not block merge; it does block
  honestly reporting the design as free.

### Implication

Three things shape this pass.

1. **The prior decision (ADR-025) drew a line this pass may need to cross.** ADR-025's
   release-strip clause and its "no time-travel mutation" exclusion were both written for
   a *developer tool*. Undo is a *shipping user feature*. Open question 5 is therefore not
   a detail — it is a question about whether ADR-025 needs superseding in part. Say so in
   the close-out drop if the chosen direction crosses it.
2. **The existing machinery's two weakest joints are exactly where undo would lean.**
   Reflective state injection and optional JSON metadata are acceptable in a debug tool
   that degrades to render-only; they are not acceptable in a shipping feature. A design
   that says "reuse the debugger" without addressing `TrySetCoreState` has not answered
   the question.
3. **The kernel's two-stage shape makes open question 1 genuinely open.** Message, event,
   `(model, effect)` pair, and model value are all real units already present. There is no
   obvious answer to hand to Track A, which is why the emphasis below is on Track A.

---

## Phase Plan

Full deep pass, per user directive. No skips.

```
architect                → 00-knowledge.md + 00-scope.md   (done)
scope-warden             → 00-warden.md                    🛑 gate 1
  ├─ dreamer-first-principles → 01-track-a.md  ⎫ one message, parallel
  └─ dreamer-informed         → 02-track-b.md  ⎭
dreamer-convergence      → 03-convergence.md               🛑
realist                  → 04-realist-plan.md              🛑
critic                   → 05-critic.md                    🛑
spec-author              → 06-spec.md                      🛑
architect                → 07-handoff.md + decision drop
```

`spec-author` runs: this is new shipping behaviour, not a refactor. The Critic is never
skipped. Gate 1 is never skipped.

**Pre-dispatch condition (gate 1):** the stale draft
`.squad/design/undo-redo/00-scope-undo-redo.md` must be removed or moved out of the pass
directory before either Dreamer track is dispatched. It is not on Track A's deny list and
it contains material the deny list exists to withhold. See the handback note.

---

## Dynamic Weighting

**Track A weighted heavily.** The problem has strong gravity toward a familiar shape, and
this codebase's structural properties — immutable models, one transition function, effects
already isolated behind a gated boundary — may make parts of that shape unnecessary. This
is the case where layering a familiar answer risks accidental complexity on a foundation
that already supplies most of what is needed. Track A must derive the unit of history from
the kernel's structure, not adopt one.

**Track B at full strength, not as a foil.** Undo/redo has a large body of production
evidence across editors, IDEs, collaborative documents and UI frameworks, and a real
literature on operational transformation, invertible operations, and persistent data
structures. Argue it from that evidence. Convergence is only informative if both tracks
are good.

**Critic weighted normally.** No incident, no time pressure. Its sharpest angles here are
INV-2 (a design that can synthesise an unreachable state), the effect boundary, and the
long-session memory claim.

**Brownfield adjustment:** the Realist carries extra weight on the relationship question.
`04-realist-plan.md` must be concrete about what happens to `Picea.Abies/Debugger/` —
untouched, extended, partly relocated, or superseded.

---

## Expert Rooms to Summon

- 🎨 **UX — real subagent.** Open question 1 is a UX question before it is a technical
  one: what a user believes one undoable step is. Also open question 3 (what happens to
  forward history after acting) and discoverability/keyboard affordances across four heads,
  including the native head where platform undo conventions already exist.
- ⚡ **Performance — real subagent.** Required if any candidate retains history without a
  bound, and required regardless if anything moves into release builds: the js-framework
  -benchmark 5% threshold and the 15 MB/10 MB bundle gate are both live budgets, and the
  bundle gate's integer-MB granularity makes it a weak instrument that must not be
  mistaken for a clean bill of health.
- 🧪 **Test Strategy — in-room, then `spec-author`.** All four invariants are
  property-shaped. FsCheck over generated action/undo/redo sequences is the natural fit
  for INV-2 and INV-3.
- 🔀 **Concurrency — in-room.** `Dispatch` serialises decisions behind `_decisionGate` but
  awaits `_core.Dispatch` outside it, and subscriptions dispatch asynchronously. An undo
  arriving mid-flight is a real interleaving, not a hypothetical.
- 🗄️ **Data — in-room.** Only if a candidate persists history across sessions or
  reloads; then serialization, versioning and the `JsonPolymorphic` obligation apply.
- 🛡️ **Security — not summoned.** No threat boundary is crossed by undo confined to
  in-memory application state. Re-summon if any candidate persists history to storage the
  user does not control, or transmits it.

---

## Notes for the pass

- The user named open question 1 the central question and holds a preference they have
  deliberately withheld so it can be derived rather than confirmed. **No phase may narrow
  that axis on a guess about what the preference is.** Present the candidates on their
  merits at convergence; the user decides there.
- `📓 Journal:` the `replay` flag being constructor-time rather than a runtime mode is
  the sharpest structural fact in `Runtime.cs` for this pass. Every existing effect gate
  hangs off it, and undo in a *live* application needs gating in a runtime that was not
  constructed for replay.
- `🗺️ Decision:` one decision drop at close-out, `arch-undo-redo`, referencing ADR-025
  if the chosen direction crosses its release-strip clause.
