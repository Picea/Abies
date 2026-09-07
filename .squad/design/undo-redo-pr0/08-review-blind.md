# 👁️ Blind Review — undo/redo locked spec (PR 0), uncommitted working tree on `test/0-undo-redo-spec`

**Reviewed:** working tree vs `3bd7af3d7e5cc79bb194912c21d45de692557e6c`
- `Picea.Abies.Tests/History/UndoRedoSpec.cs` (new, untracked, 852 lines)
- `Picea.Abies.Tests/SpecAttribute.cs` (new, untracked, 7 lines)
- `Picea.Abies.Tests/Picea.Abies.Tests.csproj` (modified, +7 lines / one `ItemGroup`)

**History channel:** `git-history-namestatus.sh`

**Narrative reaching this context:** ⚠️ partial, and by two routes I could not avoid.

1. **The diff itself carries narrative.** The added csproj comment quotes
   `.squad/design/undo-redo/06-spec.md § "The Lock"` and `04-realist-plan.md § "Todo List"`,
   and names "PR 0", "[R4-spec-commit]", "plan step 6 (PR 3)". Lines 1–9 of
   `UndoRedoSpec.cs` do the same and add "Approved by the user 2026-09-07". I read these
   because they are in the code under review. I did **not** follow them.
2. **The full design pass is committed inside the repository, outside `.squad/design/`,
   and is therefore not blocked by the blindness hook.** `git-history-namestatus.sh`
   showed commit `7d32cdb` (2026-09-07T15:22) adding
   `Picea.Abies.Presentation/content/demo/full/{00-scope,01-track-a,02-track-b,03-convergence,04-realist-plan,05-critic,06-spec,07-handoff}.md`,
   `.../decision-drops/*` (including two dated `…review-pr359…` and `…review-pr359-round2…`),
   and `.../stops/5.3-convergence-verdict.md`, `5.4-critic-verdicts.composite.md`,
   `5.6-review-headers.composite.md`. **I did not open any of them.** I am recording their
   existence because the isolation this review depends on is, for this repository, held only
   by my own restraint — a reviewer who greps the Presentation project for a symbol will walk
   straight into the convergence artifact and the previous review's verdict.

   The one file in that tree I did read is `Picea.Abies.Presentation/content/demo/stops/5.5-property.cs`
   — surfaced by a `grep` for `WithHistory` across `*.cs`. It turned out to be a verbatim
   duplicate of `INV-3` from the file under review, so it carried no narrative, only code I had
   already read. I note the duplication as a finding below.

Everything else below is from the code, the build system, and probes I ran against
TUnit 1.19.57 in a scratch project.

---

## What this change does

Three things, one of which is the substance.

**1. It commits an 852-line test file that is deliberately excluded from the build.**
`Picea.Abies.Tests/History/UndoRedoSpec.cs` declares `namespace Picea.Abies.Tests.History`,
`using Picea.Abies.History;`, and a single `public sealed class UndoRedoSpec` carrying
`[Spec("Undo and redo over the whole of an Abies application's state")]`. It contains
21 `[Test]` methods in two layers:

- an **acceptance layer** (A1–A10, 13 tests) covering: one-dispatch-one-undo-step and
  redo ordering; refusal to undo across an action that spoke to the world
  (`BlockedByWorld`) or issued a command (`BlockedByEffect`); acting after an undo
  preserving rather than clearing the forward branch (`BlockedBySupersedingAction`);
  the empty-history no-op (`Nothing`) being observably different from a refusal;
  a rejected `Decide` recording an undo stop rather than a seal; two behavioural
  differences between wrapped and unwrapped programs asserted *against* an unwrapped
  control; `Hold`/`Settle` brackets as a single movement for subscription reconciliation;
  whole-model vs. projected policy under a background `Tick`; undo remaining dispatchable
  out of a terminal model; and a browser `UrlChanged` sealing both history edges.
- an **invariant layer** (8 tests, `[Property("Invariant", "INV-n")]`) — INV-1 through
  INV-7 with two properties for INV-2.

None of the types it exercises exist. `grep` across the repository finds no
`namespace Picea.Abies.History`, no `WithHistory`, no `MovementAvailability`, no
`HistoryMessage`. Nor do the fixtures it calls (`Start`, `StartBare`, `NoFeedback`,
`EditorLog`, `Gen`, `Both`, `Drive`, `Movements`, `SingleReconciliation`, `Keys`,
`CursorMoved`, `ReachableByOrdinaryActionsAlone`, `EditorMessages`, `CommandShape`,
`DocumentComparer`).

**2. It suppresses the resulting build break** with a new `ItemGroup` in the test csproj:
`<Compile Remove="History\UndoRedoSpec.cs" />`, commented as temporary and to be removed
in a later PR.

**3. It adds a bespoke `[Spec(string description)]` attribute** — `public sealed`,
`AttributeUsage(AttributeTargets.Class)`, one get-only `Description` property. This file
*is* compiled. Its only consumer is the excluded file, so as of this change it is an
unused public type in the test assembly.

**Old behaviour → new behaviour:** the test assembly compiled and ran N tests; it still
compiles and still runs the same N tests. I verified this — `dotnet build` on the test
project succeeds with **0 warnings, 0 errors**, and `dotnet msbuild -getItem:Compile`
confirms `History/UndoRedoSpec.cs` is absent from the compile set while `SpecAttribute.cs`
is present. The net observable change to CI is: nothing, plus one dead attribute type.

## Why it might be needed

Inferable, and I think correctly: this is spec-before-implementation, committed early so
that the acceptance criteria are **fixed and dated before the code that must satisfy them
exists**. The header states the intent explicitly — "Implementation must make this file
pass without modifying it; a conflict is marked `// SPEC CONFLICT:` and handed back, never
edited away." The value is that a later reviewer can diff the shipped behaviour against a
spec that provably predates it, and that the implementer cannot quietly relax a criterion
to make a test go green. The csproj exclusion is the price of committing it before the
types exist rather than sitting on it in a branch.

What I **cannot** infer from the code: why the spec had to land as a separate commit at all
rather than as the first commit of the implementation PR, and what the exclusion buys over
simply not committing the file yet. The stated benefit (a tamper-evident timestamp) is real
but is also what git already provides for a file committed on a feature branch. That is a
question for the narrative, not a finding.

## Is this the right approach?

**The mechanism is sound and has in-repo precedent.** `Picea.Abies.Presentation.csproj:12-15`
already does exactly this — `<Compile Remove="content\**" />` with a comment explaining that
the folder holds "one excerpted test method (not a compilation unit) that must never
compile". So `Compile Remove` is the established way this repository parks non-compiling
`.cs` files, and the new csproj follows it including the explanatory comment. Good.

**But the approach has one structural weakness that the whole design rests on and does not
address: an excluded file is never type-checked, and this one has been declared immutable.**
The header says implementation "must make this file pass without modifying it". A file that
has never been compiled is being frozen. Every typo, every wrong overload, every misuse of
the assertion library is therefore locked in too, and — by the stated protocol — becomes a
`// SPEC CONFLICT:` hand-back rather than a one-line fix. That is a lot of ceremony
purchased for defects that a compiler would have caught in four seconds.

**And it is not hypothetical.** I compiled the assertion patterns this file uses against
TUnit 1.19.57 (the pinned version) in a scratch project. Findings below: one is a hard
compile error, one silently makes a test vacuous. Both are now inside a lock.

**A cheaper approach that would have preserved the property being sought:** keep the file
compiled and mark the whole class `[Skip("awaiting Picea.Abies.History — PR 3")]`, against
stub types (or an `#if` guard). The spec stays tamper-evident, dated, and reviewable, *and*
the compiler and the analyzers keep checking it. The reason that is not possible here is
that the feature types do not exist at all — but the fixtures do not exist either, so PR 0
is already accepting that the file references a world it cannot see. Committing a minimal
`Picea.Abies.History` type skeleton alongside would have made the spec compile-checked from
the start. Whether that trade was considered is a narrative question; that it was *not*
taken is visible in the two defects below.

**Smaller approach questions:**

- The bespoke `[Spec]` attribute duplicates something the same file already uses.
  `[Property("Invariant", "INV-1")]` is TUnit's own metadata attribute and appears eight
  times; `[Spec("…")]` appears once and is hand-rolled. `[Property("Spec", "…")]` would need
  no new type, would flow into TUnit's test metadata and reports, and would remove a public
  type from the test assembly. Nothing reads `SpecAttribute.Description`.
- The spec is "property-based" in name only. `[Property]` in TUnit is arbitrary key/value
  metadata, unrelated to property-based testing, and the eight invariants are `foreach`
  loops over a fixed `Gen.Corpus` — deterministic table-driven tests with no generation, no
  shrinking, and no counterexample minimisation. The `functional-ddd` skill names FsCheck for
  this; FsCheck is not referenced in the csproj and no new dependency is proposed. Calling
  them properties sets an expectation the design does not meet.

## Problems

### P1 — `INV-6` cannot compile: `.Or.That(...)` does not exist in TUnit 1.19.57

`UndoRedoSpec.cs:787-788`:

```csharp
await Assert.That(runtime.Model.Present).IsNotEqualTo(h.Present)
    .Or.That(CursorMoved(h, runtime.Model)).IsTrue().Because($"seed {seed}");
```

**Verified by compilation.** In a scratch project referencing `TUnit 1.19.57`:

```
error CS1061: 'OrContinuation<int>' does not contain a definition for 'That'
```

TUnit's `.Or` continues the assertion chain on the *same* subject
(`Assert.That(x).IsEqualTo(1).Or.IsEqualTo(2)`). It has no `.That(otherSubject)`, so a
disjunction across two different values — which is precisely what this line needs — is not
expressible this way at all.

Why it matters: the file's header says it "does not compile until plan step 6", framing the
non-compilation as purely a missing-types problem that PR 3 resolves. It will not resolve
this one. And because the file is locked, the fix has to travel the `// SPEC CONFLICT:`
hand-back path rather than being corrected now, while it costs nothing. This is the single
strongest argument that the lock was applied to an unverified artifact.

### P2 — `A6`'s ordering test is vacuous: `IsEquivalentTo` ignores order

`UndoRedoSpec.cs:190-203`. The test is named
`A_multi_event_decision_applies_every_event_before_any_command_is_interpreted` and its whole
purpose is to record an **ordering** difference between the wrapped and unwrapped programs:

```csharp
await Assert.That(wrappedOrder).IsEquivalentTo([
    "applied:BatchFirst", "applied:BatchSecond",
    "interpreted:FailingCommand", "interpreted:MarkCommand"]);
await Assert.That(bareOrder).IsEquivalentTo([
    "applied:BatchFirst", "interpreted:FailingCommand",
    "applied:BatchSecond", "interpreted:MarkCommand"]);
```

The two expected sequences are **permutations of the same four strings**.

**Verified by execution.** I ran, against TUnit 1.19.57:

```csharp
string[] actual = ["a", "b", "c", "d"];
await Assert.That(actual).IsEquivalentTo(["a", "c", "b", "d"]);   // PASSES
```

`IsEquivalentTo` is order-insensitive. Therefore **both assertions pass regardless of which
interleaving actually occurred**, and the test cannot fail for the reason it exists. A
regression that reverted the wrapper to the unwrapped interleaving would leave it green.

I also checked for an ordering-sensitive overload: `IsEquivalentTo(expected, CollectionOrdering.Matching)`
does **not** compile on 1.19.57 (`CS0103: The name 'CollectionOrdering' does not exist`), so
the remedy is a sequence-equality assertion, not an option flag.

Same pattern, lower stakes, at `:267-268` (`SubscriptionActivity` equivalent to
`["start:…", "stop:…"]` — "stop then start" would also pass) and `:843`.

### P3 — no `[NotInParallel]` on a class that is built entirely on process-wide mutable state

Every test in the file reads and writes a static `EditorLog` — `EditorLog.Clear()` at
`:76, 118, 145, 192, 217, 240, 260, 440, 703, 735, 767, 820`, plus `EditorLog.MessagesApplied`,
`.PatchesApplied`, `.CommandsInterpreted`, `.SubscriptionActivity`, `.Timeline`,
`.DeliveredKeys`, `.ReportedKeySetsDuringHold`, `.AllReportedKeys`. TUnit runs tests in
parallel by default. There is no `[NotInParallel]` anywhere in the file.

**This repository already knows the answer.** Six existing test classes carry class-level
`[NotInParallel("shared-dom-state")]` / `[NotInParallel("hot-reload-registry")]`
(`DiffTests.cs:27`, `RenderTests.cs:24`, `HeadDiffTests.cs:15`, `UiComponentRenderTests.cs:8`,
`UiAccessibilityContractTests.cs:7`, `HotReloadTests.cs:8`) and `NavigationTests.cs` uses
method-level `[NotInParallel(nameof(NavigationCallbacks))]` four times. The new spec departs
from an established, visible convention in the same directory, with no note.

Consequence when the file is un-excluded: 21 tests interleaving on one static log. Assertions
like `await Assert.That(EditorLog.MessagesApplied).IsEmpty()` (`:143`, `:684`, `:775`) are
flaky by construction. Because the file is locked, the fixture cannot fix this — the attribute
belongs on the class declaration, which is inside the lock.

### P4 — the file is in *no* MSBuild item group, so it is invisible in IDE project trees

`dotnet msbuild -getItem:Compile` and `-getItem:None` both return nothing for
`History/UndoRedoSpec.cs`. `Compile Remove` deletes it from the compile set without adding it
anywhere else, and the SDK's `None` glob has already been evaluated by then. Visual Studio and
Rider will not show it in the project unless "Show All Files" is on.

For an ordinary excluded file that is cosmetic. For a file whose entire purpose is to be
*looked at and kept intact for three PRs*, being invisible to the tool most people browse the
project with is the wrong outcome. `<None Include="History\UndoRedoSpec.cs" />` alongside the
`Compile Remove` fixes it.

Worth noting for context, not as a fault of this change: the precedent it copies has the same
gap and a comment that misdescribes it — `Picea.Abies.Presentation.csproj:12` claims "the
files stay visible as None items", and I verified they are in neither `None` nor `Content`
there either.

### P5 — two properties document falsifiers they do not implement

`UndoRedoSpec.cs:795-800`, the comment closing INV-6:

> The corpus is not complete until every one of the ten (case × direction) combinations has
> been observed at least once; **the property asserts that coverage at the end of the loop**
> rather than assuming the generator found them.

INV-6's body (`:754-793`) ends at the closing brace of the seed loop. **There is no coverage
assertion.** Nothing counts the five `MovementAvailability` cases, nothing counts directions,
nothing fails if the corpus only ever produced `Nothing`.

Same at `:611-618`, closing INV-2's second property:

> the property is not done until both branches have been observed taken at least once — a run
> in which `Future` is always empty has checked only half of "both edges", and the falsifier
> is chosen so that a one-sided seal goes red.

The two branches are `if (runtime.Model.Past.Count > 0)` (`:599`) and
`if (runtime.Model.Future.Count > 0)` (`:604`). **Neither is counted.** If `Future` is always
empty across the corpus the property is green having checked one edge, exactly the outcome
the comment says it prevents.

In a normal test file this is a stale comment. In a file declared immutable and offered as the
authority against which the implementation is judged, it is a **durable false claim about
coverage** — and it is the kind of claim a later reviewer is most likely to take at face value
rather than re-derive.

### P6 — several properties can pass having asserted nothing at all

`continue` guards at `:491, :493` (INV-1), `:643, :650` (INV-3), `:700` (INV-4), `:733`
(INV-5). Each skips the seed when the precondition is not met — correct in itself, but nothing
anywhere records that *some* seed reached the assertions. A `Gen.Corpus` that never produces an
available undo (or a `Gen` implementation that regresses to producing trivial scripts) leaves
INV-1, INV-3, INV-4 and INV-5 all green and all vacuous. Combined with P5, four of the eight
invariants have no floor under them.

### P7 — `INV-5` may compare against a stale availability

`:730-748`:

```csharp
foreach (var (direction, message, availability) in Both(runtime.Model))
{
    ...
    await runtime.Dispatch(message);
    ...
    await Assert.That(refusals[0].Reason).IsEqualTo(availability)...
```

`Both(...)` is snapshotted **once**, before the loop. The body then dispatches, and per the
spec's own rules a `MovementRefused` travels "along the ordinary message path" into the
application (`:80-84`, `:118-124`). Under the whole-model policy the file establishes in A8,
an ordinary message pushes a history step and supersedes the forward branch. So the first
iteration's refusal can plausibly change the availability that the second iteration then
asserts against, using the stale tuple.

I cannot settle this without the implementation — it turns on whether the wrapper feeds
`MovementRefused` back through itself or emits it beside the history. I raise it as a question
the locked file forecloses: if the answer is "yes, it is an ordinary message", INV-5 has a
false-failure mode baked in.

By contrast INV-6 (`:765-766`) re-reads `var h = runtime.Model` and recomputes `answer` inside
the loop, and is not exposed to this. The inconsistency between the two is itself a signal.

### P8 — `A6` forces an undocumented aliasing contract on the fixture

`:190-196`:

```csharp
var wrappedOrder = EditorLog.Timeline;
EditorLog.Clear();
...
var bareOrder = EditorLog.Timeline;
```

If `Timeline` returns a live reference to the log's backing list rather than a snapshot,
`Clear()` empties `wrappedOrder` too and both variables alias the same object — the two
assertions at `:197` and `:201` then both run against the bare timeline. Because the spec is
locked, the fixture is now *obliged* to return a copy, and nothing states that obligation.
The same test also keeps `wrapped` alive (`using var`, disposed at method end) while `bare`
runs, so both runtimes can write to the one static log concurrently.

### P9 — `A1` does not reset the log it asserts on

`:24-53`. Every sibling test calls `EditorLog.Clear()` before an emptiness assertion; A1's
final `await Assert.That(EditorLog.CommandsInterpreted).IsEmpty();` (`:52`) has no `Clear()`
anywhere in the method, so it relies on `Start<Editor>()` resetting the static log — an
unstated contract, inconsistent with the rest of the file, and (with P3) unsound under
parallel execution regardless.

### P10 — build-file and test-file comments are coupled to `.squad/` process paths

`Picea.Abies.Tests.csproj:24-26` and `UndoRedoSpec.cs:1-9` both direct the reader to
`.squad/design/undo-redo/06-spec.md` and `04-realist-plan.md`. `.squad/` is squad process
state, not shipped source, and the slug the csproj cites (`undo-redo`) is not the slug this
review was dispatched under (`undo-redo-pr0`) — so the pointer is already at least
ambiguous. The csproj comment is self-limiting (the `ItemGroup` is deleted in PR 3), but the
nine-line file header is not: it stays in `Picea.Abies.Tests` permanently, citing section
names in files that a repository consumer has no reason to have. `docs/adr/` is the
repository's durable home for this kind of rationale.

### P11 — `SpecAttribute` is dead code as shipped, and under-specified

`SpecAttribute.cs`. Its only use site is excluded from compilation, so this change adds a
public type with no consumers. Additionally:

- No XML doc comment on a `public` type (the code-review catalog calls for these on new
  public APIs; the test assembly is `IsPackable=false`, so this is minor).
- `[AttributeUsage(AttributeTargets.Class)]` leaves `Inherited` at its default `true`. For a
  descriptive marker on `sealed` test classes, `Inherited = false` is the accurate
  declaration; as written the attribute would propagate to derived classes if any ever exist.
- See the approach section: `[Property("Spec", "…")]` would need no new type at all.

### P12 — `INV-3` is duplicated verbatim into checksummed presentation content

`Picea.Abies.Presentation/content/demo/stops/5.5-property.cs` is a near-verbatim copy of
INV-3 (`UndoRedoSpec.cs:637-670`), committed at HEAD in `7d32cdb`, and that directory carries
a `SHA256SUMS`. Two copies of a "locked" artifact, one of them checksummed, is a drift
hazard: any correction to INV-3 (P1's sibling issues, say) now has two homes and a checksum
to refresh. Worth a decision on which copy is canonical and whether the demo copy should be a
generated excerpt rather than a hand copy.

### P13 — smaller notes

- `TimeSpan.FromSeconds(5)` appears four times as a literal (`:67, :237, :257, :305`) with no
  shared constant. The event-based `WhenApplied<T>().WaitAsync(...)` shape is otherwise good
  and matches the repository's recent move away from sleeping (`ca2519d`, "Signal instead of
  sleeping in the dispatch-fault tests").
- The seed loops are hand-rolled `foreach (var seed in Gen.Corpus)` rather than TUnit
  `[Arguments]` / `[MethodDataSource]`. Consequence: one reported test per invariant instead
  of one per seed, the first failing seed aborts the remaining seeds, and no per-seed
  isolation. The code-review catalog prefers `[Arguments]` cases for exactly this.
- No `[Timeout]` on properties that start a runtime per seed and dispatch 24–32 messages
  (INV-7 uses `Gen.Corpus.Concat(Gen.NamedShapeSeeds)` at length 32, with an inner loop over
  maximal movements). Wall-clock cost is unknown and unbounded.
- `namespace Picea.Abies.Tests.History` sits directly beneath `Picea.Abies.Tests` while the
  feature namespace is `Picea.Abies.History` and the spec calls a static `History.Backward(h)`
  unqualified ~15 times. I built a probe reproducing this exact shape and it **does** resolve
  to the type rather than the namespace — so this is not a defect, but three things named
  `History` in one lookup scope is a legibility cost the file pays on every line.
- For the ~13 unqualified fixture calls (`Start`, `NoFeedback`, `Both`, `Drive`, …) to bind at
  all, the support files must supply a `global using static …` — the locked class is
  `sealed`, not `partial`, and declares no base type, so no other file can inject members into
  it. I verified a `global using static` works. This is a hard, unstated constraint the lock
  places on files that do not exist yet.

## What I could not determine from the code alone

1. **Why the spec was locked before it was ever compiled.** P1 and P2 are both defects a
   compile-and-run would have surfaced immediately. Was the lock intended to apply to the
   *behavioural claims* rather than to the literal text, and if so, does correcting a
   non-compiling assertion count as "modifying the spec"?
2. **What the `// SPEC CONFLICT:` protocol actually costs.** If every wrong overload is a
   hand-back to the approver, does PR 3 realistically get through? Is there a fast path for
   mechanical fixes that preserve the claim?
3. **Whether `MovementRefused` is fed back through the wrapper as an ordinary message.**
   P7 turns entirely on this, and A5/A8's rules point both ways.
4. **Whether `EditorLog` is intended to be static at all**, or whether the fixture will make
   it per-runtime — which would dissolve P3, P8 and P9 at a stroke, but cannot, because the
   locked file calls `EditorLog.Clear()` as a static twelve times.
5. **Why `[Spec]` exists** rather than `[Property("Spec", …)]`. Is something downstream
   (a report generator, a doc extractor) meant to read `Description`?
6. **Whether "property-based" was meant literally.** Is a generative library (FsCheck) a
   later step, with `Gen.Corpus` as a placeholder, or is the fixed corpus the final design?
   The absence of any shrinking machinery suggests the latter; the prose suggests the former.
7. **Whether the un-exclusion in PR 3 is gated on anything.** Nothing in the working tree
   fails, warns, or reminds if the `Compile Remove` is simply never removed — the spec would
   sit dark and green indefinitely. Is there a tracking issue, a CI check, or a dated
   assertion somewhere that I cannot see?
8. **Whether the committed design pass under `Picea.Abies.Presentation/content/demo/full/`
   is intended to be a permanent repository fixture.** If so, the review-blindness guarantee
   for this repository needs revisiting — the hook protects `.squad/design/`, and a complete
   copy of the same material now lives outside it.
