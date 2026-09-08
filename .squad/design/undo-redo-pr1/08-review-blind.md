# 👁️ Blind Review — undo-redo-pr1

**Reviewed:** working tree on `feature/0-undo-redo-pr1` against HEAD `77aa2ba5e80130b5fbfcbd8ddae0b618e77c49fe`
- `Picea.Abies/History/HistoryStack.cs` (new, 102 lines)
- `Picea.Abies/History/History.cs` (new, 220 lines)
- `Picea.Abies.Tests/History/HistoryStackTests.cs` (new, 167 lines)
- `Picea.Abies.Tests/History/HistoryTests.cs` (new, 243 lines)

Read as callers/context (unchanged at HEAD): `Picea.Abies.Tests/History/UndoRedoSpec.cs`,
`Picea.Abies.Tests/Picea.Abies.Tests.csproj`, `Picea.Abies/Picea.Abies.csproj`,
`Directory.Build.props`, `.editorconfig`, `Picea.Abies/Message.cs`, `Picea.Abies/Program.cs`,
`Picea.Abies/Runtime.cs`, `Picea.Abies/Debugger/*`.

**History channel:** `git-history-namestatus.sh` (one prior commit touching these paths: `70d9ae5`,
which added `UndoRedoSpec.cs`. No churn, no reverts, no prior attempt at these four files.)

**Verification performed:** `dotnet build Picea.Abies.Tests` (succeeded, 0 warnings);
`dotnet run --project Picea.Abies.Tests -- --treenode-filter ".../Picea.Abies.Tests.History/..."`
(25 tests, 25 passed, 158 ms); `dotnet format --verify-no-changes` on both new source directories
(clean); and two purpose-built probe programs compiled in the scratchpad against the built
`Picea.Abies.dll` from an assembly *without* `InternalsVisibleTo`, to test the encapsulation and
equality claims made in the XML docs. Probe output is quoted inline below.

**Narrative reaching this context:** ⚠️ **Yes, from two directions. Naming it rather than
pretending otherwise.**

1. **In the dispatch prompt.** I was told this is one of a numbered series ("PR1"), that
   `UndoRedoSpec.cs` is "the locked consumer these types must satisfy", and that its csproj
   exclusion is "not part of this change". That is framing I would otherwise have had to infer.
2. **In the code itself, at HEAD and in the diff.** `Picea.Abies.Tests.csproj:24-28` cites
   `.squad/design/undo-redo/06-spec.md § "The Lock"` and `04-realist-plan.md § "Todo List"` and
   names "plan step 6 (PR 3)". `UndoRedoSpec.cs`'s 30-line header recounts an approval history,
   two prior `reviewer-reconcile` 🔴 verdicts, and amendments 4 and 5. Both **new test files**
   open with `<summary>Plan step 1 — ...</summary>` and `<summary>Plan step 2 — ...</summary>`,
   and `HistoryTests.cs:14-15` and `:80` explain what "plan step 6's `record`" will do.

   I did not open any file under `.squad/design/`, by any tool. But a reviewer cannot read this
   change without absorbing that a plan exists, is numbered, and has been through review before.
   That is itself a finding (see Problem 12), and it means my independence here is weaker than
   the design intends. Discount my "why it might be needed" section accordingly — parts of it
   are transcription, not inference.

---

## What this change does

It adds a new `Picea.Abies.History` namespace to the framework assembly containing an undo/redo
state model, plus unit tests for it. **Nothing in the shipping runtime constructs or consumes any
of it.** I grepped `Picea.Abies/` for callers of `Trim`, `SealTops`, `Cleared`, `StepBack`,
`StepForward`, `ReplaceTop` and `Origin.Established` and found none outside `History/` itself —
the only hits were the pre-existing, unrelated `DebuggerMachine.StepForward/StepBackward`.

**Old behaviour → new behaviour at runtime: unchanged.** No existing code path is touched. What
actually changes is the package's **public API surface**, which gains three types:

| Type | Accessibility | Shape |
|---|---|---|
| `History<TModel>` | `public sealed record` | `Present` (public init), `Movement` (public init), `Past`/`Future`/`Origin` (internal init), internal 5-arg ctor, internal `StepBack`/`StepForward`/`Cleared`/`SealTops` |
| `History` | `public static class` | one member: `Start<TModel>(TModel)` |
| `Movement` | `public abstract record` | private ctor; nested `Settled`, `Held(object Anchor)` |
| `Step<TModel>`, `EdgeState`, `Origin`, `HistoryStack<T>` | `internal` | — |

The internal pieces:

- **`HistoryStack<T>`** — a persistent stack over a single `T[]`, copied whole on every mutating
  operation. `Push`, `Pop`, `ReplaceTop`, `Trim(depth)` (evicting oldest-first), `Peek`, `Count`,
  `IsEmpty`, `IEnumerable<T>` oldest→newest. `Empty` is a per-closed-generic static singleton.
  Every operation returns a new instance; `Trim` returns `this` when already within the bound.
- **`Step<TModel>`** — `(Model, Cause: Message, Edge: EdgeState, AtTicks: long)`.
- **`EdgeState`** — closed sum: `Crossable | SealedByEffect(Message) | SealedByWorld(Message) |
  SupersededByNewAction(Message)`.
- **`Movement`** — closed sum: `Settled | Held(object Anchor)`, anchor deliberately type-erased.
- **`Origin`** — enum `Fresh | Established`. `Established` is never produced by any code, test
  or production.
- **`History<TModel>.StepBack/StepForward`** — pop the crossed side, push the state being left
  onto the other side as a **fresh `Crossable` step carrying the crossed step's `Cause` and
  `AtTicks`**, and move `Present` through caller-supplied `restore(remembered, current)` /
  `scrub(current)` delegates. `Movement` is untouched by both.
- **`Cleared()`** — empties both stacks, leaves `Present`/`Movement`/`Origin` alone.
- **`SealTops(state)`** — stamps `state` onto the top of each non-empty stack.

The tests are 25 cases covering stack mechanics, persistence, a 10,000-push depth-bound loop,
`Start`'s initial value, absence of public constructors (by reflection), a `StepBack`→`StepForward`
round trip asserting reference identity of `Present`, delegate-argument threading, empty-stack
throws, `SealTops`, `Cleared`, and `Movement.Held`'s anchor.

---

## Why it might be needed

Inferable from the code with reasonable confidence, though see the contamination note above.

`UndoRedoSpec.cs` — present at HEAD, excluded from compilation by
`<Compile Remove="History\UndoRedoSpec.cs" />` — is an executable specification for
application-wide undo/redo where `History<TModel>` is *the program's model*: it does
`runtime.Model.Present`, `runtime.Model.Past.Count`, `runtime.Model.Future`,
`h.Movement is Movement.Held`. It also needs `History.Backward(h)` / `History.Forward(h)`
returning a `MovementAvailability` sum (`Nothing | Available(int) | BlockedByEffect |
BlockedByWorld | BlockedBySupersedingAction`), a `MovementRefused` message, `Direction`,
`HistoryMessage.Hold`/`Settle`, and a `WithHistory` wrapper. **None of those exist yet.**

So the shape is clear: this is the data layer under an as-yet-unwritten policy layer. Every
`EdgeState` case maps 1:1 onto a `MovementAvailability.Blocked*` case in the spec, which is
strong evidence the sum type was derived from the spec rather than guessed. `Origin` maps onto
the spec's `// Origin becomes Established` comment at `UndoRedoSpec.cs:534`, about navigation
re-basing versus sealing.

**What I cannot infer:** why `Step.AtTicks` exists. Nothing in the locked spec reads it, no
production code reads it, and the only test assertion on it is a round-trip identity. It may be
for coalescing rapid edits, but nothing in the code says so.

---

## Is this the right approach?

**Broadly yes, with two design choices I would push back on.**

### The array-backed stack is defensible, and the doc defends it well

The `<summary>` on `HistoryStack<T>` pre-empts the obvious objection ("why is this O(n)?") with
an actual argument: the access pattern is push-at-the-near-end plus trim-from-the-far-end, which
a persistent linked list does *not* serve — trimming the oldest element of an immutable linked
stack rebuilds the whole spine anyway. That is correct reasoning and rare to see written down.
The repo already uses `System.Collections.Immutable` in the analyzers project, so the author had
`ImmutableStack<T>` available and passed on it for a stated reason. Good.

I would still want the argument to be *load-bearing rather than decorative* — see Problem 4.

### The namespace/type name collision is a real, already-materialised cost

`Picea.Abies.History` is both a namespace and (via `Picea.Abies.History.History`) a type name.
This is not hypothetical damage:

- `HistoryTests.cs` needs a **20-line XML doc block** (lines 16-32) to explain why every call
  site reads `Abies.History.History.Start(...)` instead of `History.Start(...)`. I verified the
  underlying claim by C# name-resolution rules: inside namespace `Picea.Abies.Tests.History`,
  the simple name `History` binds to the *namespace* `Picea.Abies.Tests.History` (a member of
  the enclosing `Picea.Abies.Tests`), shadowing the imported type, so `History.Start` is CS0234.
  The analysis in that comment is accurate.
- The locked `UndoRedoSpec.cs` dodges the same collision the other way, by placing
  `using Picea.Abies.History;` *after* its file-scoped namespace declaration — which
  `.editorconfig:132` declares a warning (`csharp_using_directive_placement = outside_namespace:warning`).
  That file's own header at line 88-93 already anticipates 25 new warnings at step 6; this adds
  more, on a file whose text is under a lock.

The codebase's own convention argues against the collision: `Message`, `Command`,
`Program`/`ProgramCore`/`ProgramView`, `Url*` and `Runtime` all live directly in `Picea.Abies`
with no sub-namespace. `Picea.Abies.Subscriptions`, `.Html`, `.DOM`, `.Debugger` each contain
*no type of the same simple name*. Two cheaper alternatives that cost nothing later:
put these types in the root `Picea.Abies` namespace like their siblings, or keep the namespace
and move the factory onto the generic type (`History<TModel>.Start(model)`), which reads better
anyway.

### `Movement.Held(object)` buys non-genericity at the price of an unenforced invariant

Erasing the anchor to `object` so `Movement` need not be generic is a legitimate trade, and the
doc states it. But the sentence that makes it safe — *"The framework recovers the type when it
reads the anchor back, because it is the only thing that ever constructs one"* — is false as
written, and I confirmed it (Problem 2). If the erasure is kept, the constructor needs to be
`internal`.

### Everything else fits the codebase

`ArgumentOutOfRangeException.ThrowIfNegative`, closed sum types via `private` base ctor + nested
sealed records, `init`-only records, `internal` + `InternalsVisibleTo` for test access, XML docs
on every public member with `GenerateDocumentationFile` on — all consistent with the surrounding
framework. Build is clean at 0 warnings and `dotnet format --verify-no-changes` passes on both
new directories.

On **functional DDD**: `StepBack`/`StepForward`/`HistoryStack` throw `InvalidOperationException`
rather than returning `Result<T,E>`. I read this as *within* the team's rule rather than a
deviation — the spec's `MovementAvailability.Nothing` is the domain-level "nothing to undo", so
by the time `StepBack` is reached an empty stack is a programmer bug, and the principles reserve
exceptions for exactly that. Flagging it only so the decision is visible rather than assumed.

---

## Problems

### 1. `History<TModel>`'s stated encapsulation invariant is not enforced — any external assembly can desynchronise `Present` from the stacks

`History.cs:107-112` claims:

> *"Constructed only by the framework ... so that no caller can build a value whose stacks
> disagree with its `Movement`, or whose stacks were never scrubbed. There is no public
> constructor."*

There is no public *constructor*, but `Present` and `Movement` are `public ... { get; init; }` on
a `public sealed record`, so `with` is a public mutation channel. I compiled a probe from an
assembly with no `InternalsVisibleTo`:

```csharp
var h = History.Start(new M("a"));
var tampered = h with { Present = new M("z") };   // compiles and runs
```

Output: `Present tampered externally: M { V = z }` — `Present` moved while `Past` and `Future`
stayed exactly where they were. `Past`/`Future`/`Origin` being `internal init` blocks three of
the five, but the two that are public are enough to break the stated invariant. Why it matters:
the claim is the justification for the whole encapsulation design, so it will be relied on by
later steps, and the failure mode (a history whose `Present` no longer corresponds to its stacks)
is silent.

### 2. `Movement.Held`'s anchor is publicly constructible and unvalidated; the doc says it is not

`History.cs:66-68` says the framework "is the only thing that ever constructs one".
`Movement.Held(object Anchor)` is a public positional record nested in a public type, so its
constructor is public. Same probe, same non-IVT assembly:

```csharp
var bad = h with { Movement = new Movement.Held("not a model at all") };
```

Output:
```
Movement tampered externally: Held { Anchor = not a model at all }
Anchor runtime type: System.String
```

Why it matters: the erasure is only sound because of the claimed sole-constructor property. When
a later step reads the anchor back and casts it to `TModel` — which is the whole point of storing
it — this is an `InvalidCastException` in framework code, raised at a site with no context about
where the bad anchor came from. `internal Held(object Anchor)` (or a `Movement.Hold<TModel>`
factory) closes it.

### 3. `History<TModel>` is a `record`, but its value equality and `GetHashCode` are reference-based on the stacks

`HistoryStack<T>` overrides neither `Equals` nor `GetHashCode`, so it uses reference equality.
The compiler-synthesized `History<TModel>.Equals` compares **all** instance fields, including the
internal `Past`/`Future` backing fields. Since `Push` always returns a fresh instance, two
histories with identical content compare unequal. Confirmed via a reflection probe that built two
histories differing only in which `HistoryStack` instance holds an identical `Step`:

```
two structurally identical stacks Equals? False
two structurally identical histories Equals? False
same hash? False
same stack instance -> Equals? True
```

Why it matters, specifically here: in this framework `History<TModel>` becomes the *application
model*. `record` is the team's signal for value semantics, and `==` on a model is a natural thing
for user code, memoization and test assertions to do. It currently answers "different" for
histories that are the same, and the hash codes differ too, so histories are unusable as
dictionary/set keys. The tests do not exercise equality in either direction, so nothing pins the
intended answer. Note that the locked spec consistently compares `runtime.Model.Present`, never
the whole model — which may mean equality genuinely does not matter, but if so the type should
say so, and if it does matter `HistoryStack<T>` needs structural `Equals`/`GetHashCode`.

I checked whether this is a *live* bug: `Runtime.cs` does not compare models for equality
anywhere (the only `Equals` call, line 59, is head-element diffing). So today it is latent.

### 4. `HistoryStack<T>`'s central design justification describes a discipline no production code applies

The `<summary>` (lines 8-18) rests the whole worst-case-O(Depth) argument on the stacks being
"kept trimmed to a small, caller-supplied depth (see `Trim`)". `Trim` has **no caller anywhere in
`Picea.Abies/`** — verified by grep. Its only callers are `HistoryStackTests`. So as shipped, the
type's bound is a property of a test, not of the system.

I recognise this is a foundation-first change and the trimming caller is presumably later. But
the doc is written in the present indicative about a system property that does not yet hold, and
the same paragraph is what a future reader will use to decide whether the O(n) copy is
acceptable. Either the tense should change, or `Trim`'s depth contract should say who is
obliged to call it.

### 5. `Step.Cause` and `Step.AtTicks` documentation contradicts what `StepBack`/`StepForward` store

`History.cs:14` and `:20-21` document these as "the message responsible for **this step**
existing" and "the dispatch tick **this step** was recorded at". But `StepBack` (line 161) and
`StepForward` (line 179) build the step being pushed as:

```csharp
var left = new Step<TModel>(scrub(Present), crossed.Cause, new EdgeState.Crossable(), crossed.AtTicks);
```

The step being *created now*, by an undo press, is stamped with the *crossed* step's cause and
tick. `HistoryTests.cs:102-104` asserts exactly this round-trip and calls it correct, so it is
deliberate.

The behaviour is probably right — reading these two fields as naming the **edge** rather than the
**step** makes it coherent, since an edge's identity survives being crossed in either direction.
But then both the property names and both doc comments are wrong, and a future reader reconciling
"recorded at" against a tick from three presses ago has nothing to go on. Given `AtTicks` has no
reader anywhere (Problem 11), the discrepancy is currently unfalsifiable by any test.

### 6. `StepBack`/`StepForward` neither check nor preserve `EdgeState`, and the precondition lives in a caller that does not exist

Both methods guard only emptiness. Neither looks at `Past.Peek().Edge`. A step whose edge is
`SealedByEffect` will be crossed without complaint, and the step pushed to the other side is
hard-coded to `new EdgeState.Crossable()` — so crossing a sealed edge also *unseals* it in the
opposite direction.

The refusal logic is clearly meant to live in the not-yet-written `History.Backward`/`Forward`
(the spec's `MovementAvailability.Blocked*`). That is a reasonable split. But right now these are
internal methods with a safety-critical precondition that is neither documented in the
`<exception>` block, nor asserted with `Debug.Assert`, nor covered by a test. Given the pattern
catalog's preference for asserting internal invariants, a `Debug.Assert(crossed.Edge is
EdgeState.Crossable)` would cost nothing and would fail loudly in the exact scenario the whole
feature exists to prevent.

### 7. `SealTops` silently overwrites an existing seal; last writer wins and the earlier reason is lost

`SealTops` stamps unconditionally (lines 202-207). An edge already `SealedByEffect(SaveRequested)`
that is later sealed again becomes `SealedByWorld(...)`, and the first reason is gone. The
spec's INV-5 is about a refusal being able to *name itself*, and `UndoRedoSpec.cs:904-910`
asserts on `refusals[0].Reason`, so which reason survives is observable behaviour. I cannot tell
from the code whether last-wins, first-wins, or "already sealed is a no-op" is intended. Nothing
documents the choice and no test exercises re-sealing.

### 8. Exception tests use a hand-rolled `try`/`catch`/`bool` where the codebase has an established TUnit idiom

Six sites — `HistoryStackTests.cs:51-92` (four) and `HistoryTests.cs:152-171` (two) — use:

```csharp
var thrown = false;
try
{ HistoryStack<int>.Empty.Peek(); }
catch (InvalidOperationException) { thrown = true; }

await Assert.That(thrown).IsTrue();
```

`Picea.Abies.Testing.Tests/TestHarnessTests.cs` already uses `await Assert.That(act).Throws<T>()`
about a dozen times on the same pinned TUnit 1.19.57, including for `InvalidOperationException`
and `ArgumentOutOfRangeException`. Beyond consistency, the hand-rolled form is weaker: it catches
the *base* type (so a `ObjectDisposedException` would satisfy the `InvalidOperationException`
test), it asserts nothing about the message — and the messages here are good, deliberately
written, and currently untested — and on failure it reports `Expected True` instead of naming the
exception that was or was not thrown.

### 9. `StepBack` collides conceptually with the existing `DebuggerMachine.StepBackward`

`Picea.Abies/Debugger/DebuggerMachine.cs:161,195` already has `StepForward()` /
`StepBackward()`. The new type has `StepForward` / `Step**Back**`. Two "walk the history
backwards" operations in one assembly, differing by four letters. Minor, but it is the kind of
thing that is free to fix now and awkward later.

### 10. Test gaps

Everything below is reachable and untested. Listed because each is a place a later step could
change behaviour without a test going red:

- `Trim(0)` on a **non-empty** stack. `Trim(0)` is only tested on `Empty` (line 118). The
  non-empty path hits `Array.Copy(src, len, dst, 0, 0)` — correct, but the boundary between
  "return `this`" and "allocate" is not pinned on the interesting side.
- `Trim`'s documented **identity** no-op. Line 110-111 asserts `Count`, not
  `ReferenceEquals(stack.Trim(2), stack)`. The doc explicitly promises "returning `this`
  unchanged"; a future edit to `new HistoryStack<T>(_items)` would pass every test.
- `Cleared()` on an already-empty history; `SealTops` when **both** stacks are empty (the
  documented "no-op on a side that holds nothing" is only tested for `Future`).
- `StepBack` when `Past.Peek().Edge` is *not* `Crossable` — i.e. Problem 6's whole surface.
- `History<TModel>` equality in either direction (Problem 3).
- `Peek`/`Pop`/`ReplaceTop`/`Trim` exception **messages**.

### 11. Three declared states are produced by nothing, including tests

- `Origin.Established` — never assigned anywhere. `Origin` is set to `Fresh` by `Start` and
  never transitions.
- `EdgeState.SealedByEffect` and `EdgeState.SupersededByNewAction` — constructed only in
  `HistoryTests.cs`. `SealedByWorld` likewise.
- `Step.AtTicks` — written by `StepBack`/`StepForward`, read by nothing outside one test
  assertion; the locked spec never mentions it.

For a foundational change this is expected rather than wrong, and I would not treat it as dead
code. I raise it because `AtTicks` in particular has *no* consumer even in the locked spec, which
is the one artifact that could justify it, and because the pattern catalog's "delete dead code"
rule needs an explicit "this is scaffolding for step N" to be set aside rather than assumed.

### 12. The source files themselves carry design-process narrative

`HistoryStackTests.cs:7` and `HistoryTests.cs:11` open with `Plan step 1 —` / `Plan step 2 —`;
`HistoryTests.cs:14-15` and `:80` refer to "plan step 6's `record`"; `HistoryTests.cs:27-32`
discusses a decision to be made at step 6. `Picea.Abies.Tests.csproj:24-28` (unchanged at HEAD)
cites `.squad/design/undo-redo/06-spec.md` and `04-realist-plan.md` by path and section.

Two costs. First, the practical one: these references rot the moment the plan closes or the
artifacts move, and a contributor outside the squad cannot resolve them. The pattern catalog asks
for durable, searchable references (a tracking issue) rather than paths into a working directory.
Second, and this is why I list it as a problem rather than a nit: it is what compromised this
review's blindness. Design-pass structure embedded in shipped source means no future reviewer of
these files can be blind to it either.

The long technical comment in `HistoryTests.cs:16-32` about C# name resolution is a different
thing entirely and should stay — it explains a genuine non-obvious constraint and I verified it
is accurate. It is the `Plan step N` framing and the `.squad/` paths I would strip.

### 13. Per-recorded-step allocation cost is argued but not measured

Once a recording caller exists, `Push` followed by `Trim` is two `T[]` allocations per undoable
step, on a path that runs on every dispatch in an MVU framework. The XML doc reasons about the
bound but cites no measurement, and `Picea.Abies.Benchmarks` exists. Not a defect in this change
— there is no caller yet — but the doc's confident framing ("This is deliberate, not an
oversight") is doing the work a benchmark would normally do, and the moment to notice that is
before the caller lands rather than after.

### 14. Minor

- `Step<TModel>` is `internal` but declares all four members `public`. Harmless, inconsistent
  with intent.
- `HistoryStack<T>` implements `IEnumerable<T>` (so `Peek()` plus LINQ, which the locked spec
  uses at `UndoRedoSpec.cs:462`) but exposes no indexer; a `Count`-driven read of the *n*th
  element from the top has to enumerate. May never be needed.
- `History<TModel>.ToString()` prints only `Present` and `Movement` — record `PrintMembers`
  covers public members only, so the internal stacks are excluded. Verified:
  `History { Present = M { V = a }, Movement = Settled { } }`. In `DEBUG`, `Runtime.cs:630`
  falls back to `model?.ToString()` for the debugger's timeline snapshot, so a history-wrapped
  application's debugger preview will show no undo state at all. Possibly intended (it keeps the
  preview small); worth a deliberate decision rather than an accident of `record` codegen.
- `HistoryStackTests.cs:127-131` performs an `await Assert.That(...)` inside a 10,000-iteration
  loop. It runs in well under the suite's 158 ms, so this is not a complaint about speed — but a
  loop that only needs one assertion at the end plus a bound check is clearer as such, and if it
  ever fails it will fail 9,900 times.

---

## What I could not determine from the code alone

1. **Is `History<TModel>` ever compared by value?** This decides whether Problem 3 is a defect or
   a non-issue. Nothing in the code, tests, or locked spec compares two histories.
2. **Is `Step.Cause`/`AtTicks` naming the step or the edge?** (Problem 5.) Both readings are
   self-consistent; only one matches the docs, and the code implements the other.
3. **Who calls `Trim`, at what depth, and where does the depth come from?** (Problem 4.) A
   caller-supplied depth implies a public knob that does not yet exist on `History<TModel>`.
4. **Is `SealTops`' last-seal-wins deliberate?** (Problem 7.)
5. **Why is `Movement` public while `EdgeState`, `Step`, `Origin` and `HistoryStack` are
   internal?** The doc says chrome pattern-matches on `Movement`, which explains the visibility
   but not why `Held`'s *constructor* must also be public (Problem 2).
6. **Was the `Picea.Abies.History.History` collision a considered decision?** The 20-line
   workaround comment reads as discovery-at-implementation rather than as an accepted cost. If it
   was considered and accepted, that reasoning belongs in the code, not in a design artifact.
7. **What does `AtTicks` exist for?** Coalescing is my guess; nothing supports it.
8. **Was this PR intended to wire anything into `Runtime`/`Program`?** It wires nothing. I read
   that as correct for a foundation step, but I am inferring the boundary.
9. **Is the 10,000-push test meant as a bound test or as a stand-in for a benchmark?**
   (Problem 13.)

Of these, (1), (2) and (4) are the ones where a claim in the narrative would genuinely change my
reading of whether something is a defect. The rest would mostly confirm that a later step handles
it.
