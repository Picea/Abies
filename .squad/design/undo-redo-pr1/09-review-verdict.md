# ⚖️ Review Verdict — undo-redo PR 1 (plan steps 1 and 2)

**Blind assessment:** `08-review-blind.md`
**Round:** 1
**Reviewed:** working tree on `feature/0-undo-redo-pr1`, HEAD `77aa2ba5e80130b5fbfcbd8ddae0b618e77c49fe`
**Verdict:** 🔴 **Changes Requested**

Four new files, 732 lines: `Picea.Abies/History/HistoryStack.cs`,
`Picea.Abies/History/History.cs`, `Picea.Abies.Tests/History/HistoryStackTests.cs`,
`Picea.Abies.Tests/History/HistoryTests.cs`.

---

## Verification performed

Everything below is something I ran, not something I read.

| Check | Result |
|---|---|
| `dotnet build Picea.Abies.Tests` | succeeded, **0 warnings** |
| `dotnet run --project Picea.Abies.Tests` (full suite) | **249 / 249 passed**, 1 s 556 ms — csharp-dev's claim verified |
| `dotnet format --verify-no-changes` on `Picea.Abies/History/` and `Picea.Abies.Tests/History/` | **clean**, exit 0 both — verified |
| `git log -- Picea.Abies.Tests/History/UndoRedoSpec.cs` | last touched by `70d9ae5` (PR #361, PR 0). **Unmodified by this changeset** — the Lock's git-history check passes |
| `git diff HEAD --stat` | only `.squad/log/*` modified; the four files are untracked additions. No csproj touch, no `Runtime.cs`, no `.js` — the plan's slip-signal row is respected |
| **External-assembly probe** — a scratch project named `ExternalProbe`, *not* in `Picea.Abies.csproj`'s `InternalsVisibleTo` list, with `EnablePreviewFeatures=true` (which every adopter template already sets — `Picea.Abies.Templates/templates/*/AbiesApp.csproj`, and the assembly is `[RequiresPreviewFeatures]`) | see findings 1 and 2 |
| **Negative control** on the same probe — `h with { Past = null! }` and `h.Origin` | `CS0117` / `CS1061`. Internals are genuinely out of reach, so the positive results below are not an artefact of the probe having internals access |
| `Trim(0)` on a **non-empty** stack (the untested boundary blind flagged) | arithmetic is correct, no exception. A coverage gap, **not** a bug |
| Spec's use of `History<TModel>`'s public surface | the locked `UndoRedoSpec.cs` contains **no `with { … }` expression at all** — it only ever *reads* `Present` and `Movement`. Finding 1 is closable without touching the locked file |

---

## Reconciliation

`08-review-blind.md` is unusually candid about its own contamination (§ *Narrative
reaching this context*), and it is right to be: `Plan step 1 —` / `Plan step 2 —`
headers in both new test files, `"plan step 6's record"` at `HistoryTests.cs:14-15`
and `:80`, and the `.squad/design/` citations in `Picea.Abies.Tests.csproj:24-28`
(inherited from PR 0). See finding 7. Discounting for that, its independent
findings held up better than usual: **all three of its compiled probes reproduce
exactly** on my own, independently-built external assembly.

### Where 08 and the narrative diverged, and what the code said

| 08 says | Narrative says | What I found | Disposition |
|---|---|---|---|
| **P1** `with { Present = … }` desynchronises a public record from its stacks | `04-realist-plan.md:1470-1473`: *"internal constructors … all movement goes through the wrapper … **No deviation from Make Illegal States Unrepresentable is requested**"*; `History.cs:107-112` repeats it | 08 is right and the narrative is wrong. Probe output: `P1 Present tampered externally: M { V = z }`, `Past`/`Future` untouched. `Present` and `Movement` are `public … { get; init; }`, so `with` is a public write channel that internal constructors do not close | **🔴 finding 1.** A deviation this changeset *introduces* — a regression under **The Merge Criterion**, blocks unconditionally |
| **P2** the public `Held` constructor admits any anchor | `History.cs:66-68`: *"the framework … is the only thing that ever constructs one"* | 08 is right. `P2 Anchor runtime type: System.String`, `P4 Movement.Held public ctors: 1`. **And the narrative gives the reason it is now dangerous:** the plan specifies `Held(TModel Anchor)` (`:157`, `:982`, `:1060`) — it shipped as `Held(object Anchor)` | **🔴 finding 2**, plus **⚠️ finding 5** for the unflagged shape change |
| **P3** record equality is reference-based on the stacks | plan step 2 Done-when (a): *"Past/Future **structurally equal** … the round trip is now **value-equal end to end**"*; ADR-008 / plan: *"the history is **a value**"* | 08 is right, and the narrative raises rather than lowers the stakes. `P3 two structurally identical histories Equals? False`, `same hash? False`. The locked spec **does** compare whole `History` values — `UndoRedoSpec.cs:226` and `:851` — and passes only because those paths return the same object | **⚠️ finding 3** |
| **P4** `StepBack`/`StepForward` hard-code `Crossable` and never check the edge | plan `Transition(h, Undo)` recomputes `Backward(h)` and calls `StepBack` **only** in the `Available` case; and the plan argues the hard-coded `Crossable` explicitly (*"an edge reaches `Future` only by an undo that was permitted"*) | **The narrative settles this against 08.** The hard-coded `Crossable` is step 2 Done-when **(c)**, required. The missing edge check is a step-6 obligation the plan already assigns | **Settled — not a PR 1 defect.** 💡 for the undocumented precondition only |
| **P5** `SealTops` silently overwrites a seal | Critic 🟡 c and 🟡 6, both accepted by the user: *"`sealTops` and `record`'s `ReplaceTop` overwrite each other's cause, **last writer wins**. Both are refusals, so INV-6's agreement is unaffected"*; step 3's Done-when requires *"the **most recently** sealing or superseding message"* | **The narrative settles this against 08.** Last-writer-wins is the designed, Critic-reviewed, user-accepted behaviour | **Settled — not a defect.** 💡 for the missing doc sentence |
| **P6** `Step.Cause` / `AtTicks` docs contradict what `StepBack`/`StepForward` store | plan `StepBack(h)` = `Step(Scrub(h.Present), **s.Cause**, Crossable, **s.AtTicks**)` — the crossed step's cause and tick, deliberately | **Behaviour matches the plan exactly; the doc does not.** 08's reading (these name the *edge*, not the *step*) is the one the plan implements | **⚠️ finding 4** — the doc is wrong, not the code |
| **P7** namespace/type collision `Picea.Abies.History.History` | csharp-dev: the test namespace shadows the static class, so the locked spec's in-namespace `using` will trip the format check at step 6 | **csharp-dev's claim verifies**, and 08's two proposed remedies do not work — see below | **Settled — plan-level item for step 6.** 💡 |

### The namespace collision — verified, and both of 08's remedies are unavailable

I derived the shadowing independently from C# name resolution and agree with the
20-line comment at `HistoryTests.cs:16-32`: inside namespace `Picea.Abies.Tests.History`,
a compilation-unit `using` is consulted *after* the enclosing `Picea.Abies.Tests`,
whose member namespace `History` wins; a namespace-body `using` is consulted
*before* the enclosing namespace, so the type wins. `.editorconfig:132` is
`csharp_using_directive_placement = outside_namespace:warning`, and
`UndoRedoSpec.cs:32-34` places its `using Picea.Abies.History;` **after**
`namespace Picea.Abies.Tests.History;` — deliberately, because it calls
`History.Backward(h)` / `History.Forward(h)` at `:152`, `:193`, `:401`, `:417`, `:621`.

08 proposes two cheaper alternatives. **Neither is available:**

- *"put these types in the root `Picea.Abies` namespace"* — the shadowing is caused
  by the **test** namespace `Picea.Abies.Tests.History`, which is inside the locked
  file. Moving the framework types does not remove the shadow, and would falsify
  the locked `using Picea.Abies.History;` besides.
- *"move the factory onto the generic type (`History<TModel>.Start`)"* — the locked
  spec also calls `History.Backward(h)` and `History.Forward(h)`, which are members
  of the static class, not of `History<TModel>`.

So the collision is **inherited** — from the plan's Namespace Plan and from PR 0's
approved spec, both of which the user gated — not introduced by this author's free
choice. csharp-dev is right to flag it and right not to decide it now. The decision
owed at step 6 is between a scoped `.editorconfig`/`#pragma` allowance on one file
and a `// SPEC CONFLICT:` hand-back; that belongs in the step-6 review brief.

### Claims I could not verify

- **`Step.AtTicks` has no consumer anywhere.** I grepped the plan: `AtTicks` appears
  in exactly two places (`:1057` the type shape, `:1165` `StepBack` threading it
  through). Step 14's coalescing uses `TPolicy.CoalesceWindow + TPolicy.Time`
  (`:1946-1951`), **not** `AtTicks`. Nothing in the locked spec mentions it. 08's
  open question 7 stands unanswered by the narrative — see 💡.
- **The step-6 format prediction.** csharp-dev's claim that the locked spec's
  `using` placement will trip the format check is sound by inspection, but I cannot
  execute it: the file is `<Compile Remove>`d, so `dotnet format` does not see it
  today. Recorded as a prediction, not a verified fact.

### What 08 could not determine, answered from the narrative

Of its nine open questions, the narrative answers (2) *edge, not step* — finding 4;
(3) *step 6's `record` calls `Trim` at `TPolicy.Depth`*; (4) *last-wins, deliberate*;
(5) *`Movement` is public for chrome pattern-matching, and the locked spec forces it
non-generic*; (6) *the collision was **not** a considered decision at plan time* —
the Namespace Plan names no `History` static class; and (8) *yes, wiring nothing is
correct for a foundation step*. It leaves (1), (7) and (9) genuinely open. A
description that answers six of nine questions a careful reader actually had is
doing its job.

---

## Critic's accepted risks — those that bind on steps 1 and 2

| Risk | Stated mitigation | Present in the code? |
|---|---|---|
| **🟡 b** — is trimming `Future` at its far end safe? | *"State in **step 1 or step 6** why the `Future` trim is safe: the crossable prefix above the topmost superseded edge can never exceed `Depth`"* | **No — and legitimately so.** The mitigation is disjunctive; the author took the step-6 branch. `HistoryStack.Trim`'s doc says nothing about it. Carry to step 6, do not lose |
| **🟡 c / 🟡 6** — three writers of an edge cause, last writer wins | *"step 3's criterion must read **the most recently** sealing or superseding message"* | **Behaviour is correct** (`SealTops` stamps unconditionally); the *wording* obligation is step 3's. Not owed here. 💡 for one doc sentence |
| **S17** — namespace-wide name-collision check | *"Step 4's Done-when adds an explicit namespace-wide name-collision check"* | **Owed at step 4, and step 1/2 has already produced one the check was not written to catch** — `History` (static class) vs. the `History` namespace segment. Flag it into step 4's check now rather than discovering it twice |
| **S20 / `[R4-6]`** — `Movement.Held(Anchor)` is a retention site outside SEC-3(b) | *"neither scrubbed nor made unreachable: named in TB7, the DEBUG row, `Anchor_never_reaches_a_release_path_surface` at step 8(m), a guide sentence, a fast-follow"* | **The mitigation was drafted against `Held(TModel Anchor)`.** It shipped as `Held(object Anchor)` with a **public constructor and a public getter**. The room's bound ("retained for the life of one bracket") is untouched, but the *write* and *read* surfaces are wider than anything TB7 was worded against. See findings 2 and 5 |
| **S29** | TB7 lifetime wording → step 16 | Not owed here |
| **SEC-1** (*"lands in steps 2, 4"*; step 2's half is *"`HistoryRedacted` designed alongside `Step`"*) | marker + `HistoryRedacted(string OriginalTypeName)`, redaction at every `Cause` store (SEC-2, step 6) | **Satisfied in substance, silently.** `Step.Cause` is typed `Message`, so a `HistoryRedacted : Message` substitutes cleanly at step 6's store sites. Nothing in `Step`'s docs says so. 💡 |

---

## Findings

### 🔴 Must Fix (blocks merge)

The criterion for finding 1 and finding 2 is **not** the two sites below. It is:

> **Every public construction or mutation channel into `History<TModel>` and
> `Movement` is closed to assemblies outside `InternalsVisibleTo`, demonstrated by
> a probe compiled from such an assembly** — the same shape of evidence used to
> open the finding. Two sites are known; the criterion is the rule, not the list.

Both are deviations from *Make Illegal States Unrepresentable* that **this
changeset introduces**, which under **The Merge Criterion — Continuous
Improvement** (`.claude/docs/principles-enforcement.md`) is a regression and blocks
unconditionally. Registration in `.claude/enforcement/refutations.md` is **not**
available for either: registration documents an inherited gap.

---

**1. `History<TModel>.cs:118,121` — `Present` and `Movement` are `public { get; init; }` on a `public sealed record`, so `with` is a public mutation channel; the type's own XML doc and the plan's principles-gate resolution both say it is not.**

`History.cs:107-112` states the invariant that justifies the whole encapsulation
design:

> *"Constructed only by the framework … so that no caller can build a value whose
> stacks disagree with its `Movement`, or whose stacks were never scrubbed. There
> is no public constructor."*

There is no public *constructor* — that part is true and is what step 2's Done-when
(b) reflection test asserts, correctly. But `init` accessors are public, and `with`
is a construction expression. From `ExternalProbe`, an assembly outside
`InternalsVisibleTo`, configured exactly as the shipped adopter templates are:

```
P1 Present tampered externally: M { V = z }  (Past/Future untouched)
P4 History<M> public ctors: 0
```

`Present` moved; `Past` and `Future` stayed. That is precisely the state the doc
says cannot be built. The negative control on the same probe shows internals are
genuinely unreachable (`h with { Past = … }` → `CS0117`), so this is not the probe
having privileges an adopter lacks.

**Why it matters.** `04-realist-plan.md:1470-1473` is the principles-gate
resolution for this pass: *"internal constructors; the only public factory is
`History.Start(TModel)`; **all movement goes through the wrapper** … No deviation
from *Make Illegal States Unrepresentable* is requested."* `:2293` repeats it:
*"internal constructors close the one place the principle was instructed rather
than enforced."* As built, movement does **not** all go through the wrapper, and the
principle is still instructed rather than enforced. The failure mode — a `Present`
that no longer corresponds to its stacks — is silent, and every later step is
entitled to rely on the stated invariant.

**Evidence that it is closable inside PR 1:** the locked `UndoRedoSpec.cs` contains
no `with { … }` expression anywhere (grepped) — it only reads `Present` and
`Movement`. `HistoryTests.cs` writes them, but from an `InternalsVisibleTo`
assembly. Nothing in the lock depends on these accessors being public.

---

**2. `History.cs:85` — `Movement.Held`'s constructor is public and its `object` anchor is unvalidated; the type's own XML doc says the framework "is the only thing that ever constructs one".**

```
P2 Movement tampered externally: Held { Anchor = not a model at all }
P2 Anchor runtime type: System.String
P4 Movement.Held public ctors: 1
```

**Why it matters, and why it is not the same finding as 1.** `History.cs:63-69`
argues the `object` erasure is sound *because* of the sole-constructor property:
*"The framework recovers the type when it reads the anchor back, because it is the
only thing that ever constructs one."* That sentence is the entire safety argument
for the erasure, and it is false as written. When step 6 reads the anchor back and
casts it to `TModel` — which is the only reason to store it — a wrong anchor is an
`InvalidCastException` raised inside framework code at a site with no context about
where the bad value came from.

The plan makes this sharper than 08 could see: the plan specifies **`Held(TModel Anchor)`**
(`:157`, `:982`, `:1060`), where a public constructor is type-safe by construction.
The shipped `Held(object Anchor)` is what makes the public constructor a defect. See
finding 5 for the shape change itself.

Note also that `Anchor` is a **public getter on a public type**. Per `[R4-6]` and
Trust Boundary 7 the anchor is *"a bounded, unscrubbed live model"* — deliberately
not scrubbed, because `Subscriptions(Scrub(m))` may differ from `Subscriptions(m)`.
The room accepted that exposure through the **DEBUG snapshot path**; a public getter
in release builds is a wider read surface than the row it was bounded by. Both
accessors are in scope for the criterion above.

---

### ⚠️ Should Fix

**3. `HistoryStack.cs` — `History<TModel>` is a `record` whose value equality and `GetHashCode` are reference-based on its two principal fields, and nothing in the type states which answer is intended.**

`HistoryStack<T>` overrides neither `Equals` nor `GetHashCode`. The synthesized
`History<TModel>.Equals` compares all instance fields including `Past`/`Future`, and
every operation returns a fresh stack instance. Reproduced independently:

```
P3 two structurally identical stacks Equals? False
P3 two structurally identical histories Equals? False
P3 same hash? False
P3 same stack instance -> Equals? True
```

**What the narrative adds to 08's reading.** Three separate claims sit on top of
this: the plan calls the history *"a value"* and cites ADR-008 for it; step 2's
Done-when (a) says the round trip is *"value-equal end to end"*; and the locked spec
compares **whole `History` values** at `UndoRedoSpec.cs:226` and `:851`
(`await Assert.That(runtime.Model).IsEqualTo(before)`), where `runtime.Model` is
`History<EditorModel>` with a possibly non-empty `Past`.

Those spec assertions pass today only because the record's generated `Equals` opens
with a reference check and the plan's `Nothing` branches return `h` itself
(`Transition(h, Undo) = Nothing -> (h, Commands.None)`). That is a property of
**step 6's** implementation, not of this type. A step-6 author who writes
`h with { }` on a no-op path turns two locked properties red for a reason that will
look like a wrapper bug and is not.

I am not prescribing the answer — structural `Equals`/`GetHashCode` on
`HistoryStack<T>`, or an explicit equality contract documented on `History<TModel>`,
are both defensible. What should not ship is a public `record` with neither, and no
test pinning either. I did not raise this to 🔴: the plan's *"value-equal end to
end"* is genuinely ambiguous between content-equality and `operator ==`, and the
change is behaviourally forward-compatible if made later.

**4. `History.cs:14,20-21` — `Step.Cause` and `Step.AtTicks` are documented as belonging to the step; the code, per the plan, stores the *edge's* cause and tick.**

Documented as *"the message responsible for **this step** existing"* and *"the
dispatch tick **this step** was recorded at"*. `StepBack` (`:161`) and `StepForward`
(`:179`) build the step being created **now**, by an undo press, as
`new Step<TModel>(scrub(Present), crossed.Cause, new EdgeState.Crossable(), crossed.AtTicks)`.

This is the plan's own instruction, verbatim: `StepBack(h) = … Future.Push(Step(TPolicy.Scrub(h.Present), s.Cause, Crossable, s.AtTicks))`.
So the **code is right and the doc is wrong**, and 08's reading — these two fields
name the *edge*, whose identity survives being crossed in either direction — is the
one the plan implements. `HistoryTests.cs:102-104` asserts the surprising behaviour
and calls it correct, so nothing will catch the doc.

It matters more than a stale comment usually would because `Cause` is SEC-2's
redaction target (*"wherever `WithHistory` stores a `Cause`"*) and INV-6's refusal
name, and step 6's author will read these two sentences to decide which message to
redact and which to name.

**5. `History.cs:85` — `Movement.Held(object Anchor)` is a shape change from the plan's `Held(TModel Anchor)`, correctly forced by the locked spec, but shipped without a flag — and the security analysis that bounds this exact site was written against the other shape.**

The plan says `Held(TModel Anchor)` in three places. csharp-dev's stated reason for
the erasure — *"the locked spec asserts `IsTypeOf<Movement.Held>()` non-generically"* —
**verifies**: `UndoRedoSpec.cs:399` is `await Assert.That(h.Movement).IsTypeOf<Movement.Held>();`,
which cannot resolve if `Movement` is generic. The lock outranks the plan, so the
erasure is the right call and I am not asking for it to be reverted.

What is owed is the flag. `[R4-6]` (`04-realist-plan.md:980-1046`), Trust Boundary 7's
wording (`07-handoff.md` § 5.1), the DEBUG-row bullet, and step 8(m)'s
`Anchor_never_reaches_a_release_path_surface` were all drafted against
`Held(TModel Anchor)`. The erased shape changes at least three things none of them
considered: a wrong-typed anchor is now constructible (finding 2), the DEBUG
`JsonTypeInfo` obligation for an `object`-typed property is not the same obligation
as for a `TModel`-typed one (SEC-5, step 13), and the read surface is `object` rather
than the model type. `security-expert` has not seen the erased shape. Owner:
`architect` at close-out or `security-expert` at step 16 — registrable under
criterion (b) with an owner, a level consequence and an `expires:`.

**6. Six sites — `HistoryStackTests.cs:51-92` (four) and `HistoryTests.cs:152-171` (two) — assert exceptions with a hand-rolled `try`/`catch`/`bool` where TUnit's own idiom is available and used elsewhere in the repository on the identical pinned version.**

```csharp
var thrown = false;
try
{ HistoryStack<int>.Empty.Peek(); }
catch (InvalidOperationException) { thrown = true; }

await Assert.That(thrown).IsTrue();
```

Verified: `Picea.Abies.Testing.Tests` uses `await Assert.That(act).Throws<T>()` at
`TestHarnessTests.cs:57,71,80,89,103,113` and `TestHarnessVisualTests.cs:109,165`,
including for both `InvalidOperationException` and `ArgumentOutOfRangeException`, and
both projects pin `TUnit 1.19.57` — so the remedy compiles as-is. I also verified
this idiom appears **nowhere else** in `Picea.Abies.Tests`: these two files introduce
it. The `code-review` catalog's *"Use modern TUnit patterns — `await Assert.That(...)`,
not manual return-code-style success indicators"* is directly on point.

Beyond consistency, the hand-rolled form is weaker in three ways: it catches the
base type (an `ObjectDisposedException` would satisfy the `InvalidOperationException`
test), it asserts nothing about the messages — which are deliberately written, good,
and currently untested — and on failure it reports `Expected True` rather than naming
the exception.

**7. `HistoryStackTests.cs:7` and `HistoryTests.cs:11,14-15,80` — design-pass structure is embedded in shipped source, and it is what compromised this review's blind half.**

`Plan step 1 —`, `Plan step 2 —`, *"plan step 6's `record`"*, and a comment block
discussing a decision to be made at step 6. To be precise about scope: the new files
contain **no `.squad/` paths** (I grepped) — those are only in
`Picea.Abies.Tests.csproj:24-28`, inherited unchanged from PR 0 and out of scope
here.

Two costs. The practical one is that `plan step 6` stops resolving the moment the
pass closes, and a contributor outside the squad cannot resolve it at all; the
catalog asks for durable, searchable references. The one that makes this a finding
rather than a nit is that `08-review-blind.md` names these lines as the reason its
independence was weaker than the design intends — and there are five more PRs in this
series, each of which will be reviewed by a blind half that these headers will reach
the same way.

I am not prescribing the wording. This is a call the user may want to make once for
the series rather than six times: keep the step framing because it genuinely helps a
maintainer follow a seven-PR build-out, or strip it in favour of an issue reference.
The long C# name-resolution comment at `HistoryTests.cs:16-32` is a different thing
entirely, is accurate — I re-derived it — and should stay.

---

### 💡 Nitpicks

- **`HistoryStack.cs:8-18` — the design justification is written in the present
  indicative about a discipline nothing yet applies.** *"the stacks this type backs
  … **are kept trimmed** to a small, caller-supplied depth"*. `Trim` has no caller in
  `Picea.Abies/` (grepped, confirming 08); step 6's `record` is the intended one.
  Either the tense, or a sentence in `Trim`'s contract naming who is obliged to call
  it. Critic 🟡 b's *"why the `Future` trim is safe"* sentence is the natural
  companion, and step 6 owes it either way.
- **`History.cs:155,173` — the `Crossable` precondition on `StepBack`/`StepForward`
  is real, correct, and written down nowhere.** The plan puts the check in step 6's
  `Transition`, which is the right split; but these are internal methods with a
  safety-critical precondition documented in neither the `<exception>` block nor a
  `Debug.Assert`. `Debug.Assert(crossed.Edge is EdgeState.Crossable)` is what the
  catalog asks for on internal invariants, costs nothing in release, and fails loudly
  in exactly the scenario the feature exists to prevent.
- **`History.cs:202-207` — `SealTops`' last-writer-wins is deliberate and
  undocumented.** Critic 🟡 c / 🟡 6 settled it and the user accepted it; one sentence
  in the `<summary>` would stop the next reader re-deriving it. Re-sealing is also
  untested in either direction.
- **`Step.AtTicks` has no consumer in the code, the tests, the locked spec, or the
  plan.** Step 14's coalescing uses `TPolicy.CoalesceWindow + TPolicy.Time`
  (`04-realist-plan.md:1946-1951`), not `AtTicks`. Implementing it is *not* a
  deviation — the plan carries it in the type shape at `:1057` — which is why this is
  a nit and not a finding. But it is a field threaded through seven PRs with no
  stated reader, and the honest options are a one-line "scaffolding for X" note or a
  question to the architect at close-out.
- **`Step<TModel>` is `internal` but declares all four members `public`.** Harmless;
  inconsistent with intent. Same for `HistoryStack<T>.GetEnumerator`.
- **Test coverage gaps, none of them Done-when items.** `Trim`'s documented
  *identity* no-op (`ReferenceEquals(stack.Trim(2), stack)`) — the doc promises
  *"returning `this` unchanged"* and a future edit to `new HistoryStack<T>(_items)`
  would pass every test; `Trim(0)` on a non-empty stack (I verified the arithmetic is
  correct, so this is coverage, not risk); `SealTops` with **both** stacks empty;
  `Cleared()` on an already-empty history; the exception **messages**.
- **`HistoryStackTests.cs:127-131` asserts inside a 10,000-iteration loop.** It runs
  in well under the suite's 1.5 s, so this is not about speed — but on failure it
  will fail ~9,900 times, and a bound check plus one assertion after the loop reads
  as what it is.
- **`new EdgeState.Crossable()` and `new Movement.Settled()` allocate per call** on
  what becomes a per-dispatch path. Static singletons are the obvious answer;
  step 7 is the step that gets to have an opinion backed by a number.
- **`History<TModel>.ToString()` prints only `Present` and `Movement`** — record
  `PrintMembers` covers public members, so the internal stacks are excluded, and the
  DEBUG debugger's `model?.ToString()` fallback (`Runtime.cs:630`) will show no undo
  state. This is *accidentally* the SEC-safe behaviour, which is a good outcome
  arrived at by codegen. Worth one sentence so step 16 records it as a decision.

---

### ✅ What's Good

- **The `HistoryStack<T>` summary argues its own O(n) rather than asserting it**, and
  the argument is correct: a persistent linked list does not serve trim-from-the-far-end
  either. The repo has `System.Collections.Immutable` available in the analyzers
  project, so `ImmutableStack<T>` was passed on for a stated reason. Reviewers rarely
  get handed the counter-argument pre-empted.
- **Every Done-when is met and green.** Step 1: push, pop, replace-top, far-end
  eviction, depth invariance over 10,000 pushes, persistence, bound stated as
  worst-case O(Depth). Step 2 (a) round trip with `Present` reference-identical and
  `Movement` untouched, (b) the reflection tests, (c) the opposite-side push as
  `Crossable`. Criterion (a) of the Merge Criterion holds.
- **`StepBack_applies_restore_and_scrub_rather_than_assuming_whole_model_replacement`**
  is the best test in the changeset: its `restore` throws unless it sees *both*
  arguments correctly, so a `StepBack` that silently used one or the other cannot
  pass. That is a test built to fail for the right reason.
- **The scope discipline is exact.** Zero lines of `Runtime.cs`, `Program.cs`, any
  head adapter, any `.js`, any csproj. The plan's slip-signal row is honoured to the
  letter, and the locked spec is untouched (`70d9ae5`).
- **The C# name-resolution comment at `HistoryTests.cs:16-32`** correctly diagnoses a
  genuinely non-obvious constraint, correctly identifies it as a step-6 decision, and
  correctly declines to make that decision now. I re-derived it and it is right.
- **`08-review-blind.md` naming its own contamination up front** is what made this
  reconciliation cheap. It told me which of its conclusions were transcription and
  which were inference, and the distinction held.

---

## Metrics

- **Files reviewed:** 4 (all new) — 732 lines. Every line read.
- **Test coverage of new code:** 25 tests over `HistoryStack<T>` and `History<TModel>`;
  suite 249/249 green; build 0 warnings; format clean on both new directories.
- **Complexity:** low. No async, no threading, no unsafe, no IO, no allocation
  in a loop beyond the deliberate copy-on-write.
- **Public API surface added:** 3 types — `History<TModel>`, `History`, `Movement`
  (+ `Movement.Settled`, `Movement.Held`).
- **Dimensions run:** 11/11.
- **Round:** 1 of a maximum 2 before the split rule applies.

### Merge Criterion — how this verdict was computed

- **(a) stated properties green** — ✅. Step 1 and step 2 (a)(b)(c) all execute and pass.
- **(b) every finding that is not a regression is registered** — **binds on the 🔴
  and ⚠️ grades only**; 💡 nitpicks are advisory and need no ledger entry.
  Findings 1 and 2 are **regressions this changeset introduces** and are therefore
  *not* registrable — they must be fixed. Findings 3–7 are registrable in
  `.claude/enforcement/refutations.md` with an owner, a level consequence and an
  `expires:`, written by the author before the next verdict; I cannot write them, by
  design.
- **(c) nothing published above its computed level** — ✅, nothing is published.

Verdict is 🔴 on (b): two introduced regressions. No unregistered ⚠️ can produce ✅
either, so fixing only findings 1 and 2 without registering 3–7 does not close this.

---

## Re-review scope

Targeted at the findings, not a full re-run. `reviewer-blind` does not re-run, so
`08-review-blind.md` stays the independent reading of record.

1. The probe. Re-run an external-assembly compile with **both** controls — a positive
   (`with { Present = … }`, `new Movement.Held(<wrong type>)`) that must now fail to
   compile, and a negative (something legitimately public) that must still succeed.
   A one-sided probe proves nothing here.
2. Confirm the fix did not narrow anything the locked `UndoRedoSpec.cs` reads —
   it reads `Present`, `Movement`, `Past`, `Future`, `Origin` and `Step`'s members,
   and constructs none of them.
3. `.claude/enforcement/refutations.md` entries for findings 3–7, checked for owner,
   level consequence and `expires:`, and checked in **both** directions — an
   over-registered advisory is a failure too.
4. Suite still 249/249 plus whatever the fix adds; format still clean.
