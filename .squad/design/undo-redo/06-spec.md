# 🧪 Spec-by-Example — undo/redo (`WithHistory`)

Read before this file: `00-scope.md` **as amended and landed** (INV-1 … INV-7),
`00-knowledge.md`, `04-realist-plan.md` **revision 4** including § *Spec obligations*,
`05-critic.md` **fourth pass** in full (APPROVED WITH MITIGATIONS; S25–S29), and
`room-security.md` including both landed 2026-09-07 follow-ups.

Three of the Critic's five 🟠 land here and are carried below by id: **S25(b)** (spec line 4 gains
the wall-clock reach bound), **S26** (the missing superseded-branch line, now line 14),
**S27** (the generator alphabet partition) and **S28** (the interleave ordering mechanism, no
sleeps). **S29 is deliberately not here** — the Critic scoped it as threat-model wording for
`security-expert`'s step-16 brief, and its third `room-security.md` follow-up may still be landing.
Nothing in this spec depends on it and nothing in it changes mechanism.

**Amended twice on 2026-09-07, both times by the user as approver and both times before the
approval commit landed — so the lock had not yet taken effect and neither is a re-approval.**
(1) Spec line 12 moved out of plan step 6 and into the locked file as **A9**. (2) After the finding
that **incoming browser navigation was not classified** — a `UrlChanged` delivered by the browser's
own back or forward button was enveloped and recorded like a user action, so an application undo
restored a model without its URL and the two stacks diverged — **INV-2 gained a second property**
and the obligations table gained **line 16**. The mechanism side of (2) is the plan's:
`origin is UrlChanged && Origin is Established -> seal, SealedByWorld(UrlChanged)`, **both** edges,
with `Backward(h)` and `Forward(h)` refusing and naming the navigation. § *Approval Request* carries
the record of both.

**Amended a third time on 2026-09-07, after `05-critic.md`'s confirmation pass, and this one
*narrows* the claim rather than widening it.** The Critic's **B10** established that the seal keys on
`Picea.Abies.UrlChanged`, and that on the WebAssembly head there is no framework-owned delivery of an
incoming URL change: `Picea.Abies/Navigation.cs:17-29` takes the message constructor **from the
application**, so an application that names its own navigation message gets no seal — and INV-2's
second property stays green, because the fixture dispatches the framework type. The user took the
Critic's mitigation (1): **scope the claim.** The location guarantee holds for navigation delivered
as `Picea.Abies.UrlChanged`, and mapping incoming navigation to that message is the **application's**
obligation — the same register as spec line 2's independent-reachability assumption, and stated as
an instruction beside it at **line 17** of the obligations table. Doc-only: no mechanism, no gate
decision, no new type, no fixture change, no property change. The navigation obligation **stays at
line 16** (`05-critic.md` S31 — 16 is right; the plan's row renumbers to match, which is the
realist's, not this file's). § *Approval Request* carries the record.

**Amended a fourth time, and this one is a re-opening rather than a pre-lock amendment.** PR 0's
review — `.squad/design/undo-redo-pr0/08-review-blind.md` and `09-review-verdict.md`, verdict
🔴 *Changes Requested* — compiled and executed this file's assertion **shapes** against the pinned
**TUnit 1.19.57** and established that the defects it found are in the **approved text of this
file**, not in `csharp-dev`'s transcription: the reviewer extracted every `csharp` fence from this
document, reassembled it and diffed it against the committed `UndoRedoSpec.cs`. So the route is a
`spec-author` amendment with **user re-approval**, not a `csharp-dev` correction. Fixed below:
`.Or.That(…)` (does not compile), A6's and A7's order-insensitive `IsEquivalentTo` (cannot fail for
the reason they exist), three comments claiming coverage the code did not implement, unguarded
`continue`s that let five properties pass having asserted nothing, and `[NotInParallel]` on a class
built on process-wide static state, plus `[Timeout(30_000)]` on the same argument. § *Approval
Request* carries the full record and the 🛑 **re-approval question — answered and re-approved on
2026-09-07 with three decisions**: re-approved as amended and immutable in assertions and claims
from the approval commit; INV-6 keeps its feedback interpreter, because narrowing the claim to the
old fixture would be adjusting the spec to the code; and `[Timeout]` lands before the commit,
generous and sized from a measured seed run.

**Amended a fifth time, because amendment 4's remedy for 🔴-2 was itself wrong.** PR 0's **round-2
re-review** opened by correcting its own round-1 measurements in both directions: `IsEqualTo` on a
collection is not order-sensitive but **unsatisfiable** (it fails on the matching sequence, across
six collection types), and `CollectionOrdering` **does** exist on 1.19.57 — the reported `CS0103`
was a missing `using`. So amendment 4 replaced three assertions that were green whatever happened
with three that are red whatever happens, and wrote the bad evidence into the locked text as fact.
Amendment 5 fixes both in one pass — `IsEquivalentTo(expected, CollectionOrdering.Matching)` with
`using TUnit.Assertions.Enums;`, and the paragraph replaced by the verified measurements — plus a
seventh support-file obligation, the `[Timeout]` caveat, and what the lock does and does not cover.
§ *Amendment 5* carries the record and the 🛑 **re-approval question, which is open. This is the
last round before the reviewer splits the changeset.**

---

## 🧪 TEST STRATEGY ROOM — undo/redo across the whole of an Abies application's state

**Assessment.** This feature has no HTTP surface, no service boundary, no new endpoint and no
adopting application. `WithHistory<TProgram, TPolicy, TModel, TArgument>` is a library type inside
`Picea.Abies`, opt-in by composition at an application's call site, and gate-3 decision 4 settled
that **no demo and no template adopts it this pass**. There is therefore nothing for an AppHost to
orchestrate and nothing for Playwright to click: the user-observable behaviour is what an
application composed with `WithHistory` does when a message is dispatched into it.

**Risks in the level choice, ranked.** (1) Testing the wrapper as pure static functions alone would
be dishonest — subscription reconciliation happens inside `Runtime.Render` (`Runtime.cs:212-220`)
and INV-7's entire mechanism is the wrapper *answering* the question `Runtime.Render` already asks
at `Runtime.cs:214`. A pure-function test cannot see it. (2) Going to the AppHost for that reason
would start KurrentDB and PostgreSQL to press an undo button, exercising nothing this pass builds
and hiding the mechanism behind two containers. (3) A fire-and-forget subscription dispatch
(`Runtime.cs:288-291`) makes ordering assertions racy, and the reflex fix is a sleep — which
`ca2519d` removed from this repository.

**Recommendations.** Drive a real `Runtime` in-process. Substitute only the framework's own two IO
seams, which `Runtime.Start` already takes as parameters. Order every autonomous delivery with a
`TaskCompletionSource` completed from inside the program under test.

**References.** `.claude/docs/decisions.md` § *Aspire AppHost Is the Test Fixture (Amended
2026-09-02)*; `Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs` (the in-process
`Runtime` precedent, and at `:38`/`:53` the `TaskCompletionSource` precedent); commit `ca2519d`.

✅ TEST STRATEGY ROOM concerns resolved — level, count and scope below.

---

## Test Strategy

**Level:** **Workflow-direct, driven through a real in-process `Runtime`.**

**Why this level:**

- The observable behaviour of this feature is *"I dispatched `Undo` and the application went back
  one step / refused and told me why"*. That is a message-in, model-and-document-out fact about a
  composed `Program`, which is exactly what a `Runtime` exposes (`runtime.Model`,
  `runtime.CurrentDocument`, `runtime.Dispatch`).
- The `Runtime` is required, not optional: `Subscriptions(h)` answering with the held anchor is
  only *asked* by `Runtime.Render`, and INV-7 is falsifiable only where that question is asked.
  Steps 6, 9, 10 and 11 of the plan drive a real `Runtime` for the same reason.
- **The Aspire AppHost is not used, and this is not an exception being claimed.** The decisions
  entry governs *end-to-end / cross-service* tests — tests that verify a user journey or a
  service against real dependencies. This is a framework library with no service, no dependency
  and no adopter. The fast-in-memory named exception is not invoked either: no
  `WebApplicationFactory`, no `TestServer`, no Testcontainers. A `Runtime` is a class.
- **E2E through the UI is excluded** because no head adopts `WithHistory` this pass. When one
  does, that is a new spec, not an amendment to this one.

**Test owner:** **`csharp-dev`.** Single owner, single layer — no `js-dev` (no `.js` file is
touched anywhere in this pass) and no multi-layer coordination.

**Scope:**

| | |
|---|---|
| **In** | `WithHistory` composed over the `EditorProgram` fixture and driven by a real `Runtime`; `History<TModel>`, `Step<TModel>`, `EdgeState`, `Movement`, `MovementAvailability`, `Backward`/`Forward`; `WholeModelHistoryPolicy`, one projection policy and one depth-3 policy; a `CountingSubscription` that records start/stop/deliver and dispatches autonomously on start. |
| **Substituted** | The `Apply` delegate (a recording sink instead of a browser or native surface) and the `Interpreter` (a deterministic test interpreter). Both are parameters of `Runtime.Start` — this is supplying the framework's IO boundary, not mocking a domain type. No domain primitive is mocked anywhere in this spec. |
| **Out** | No AppHost, no browser, no Playwright, no HTTP, no database, no Conduit, no demo, no template, no head adapter, no `.js`. No `Runtime.cs` line. |
| **Infrastructure** | None. In-process only. |
| **Clock** | **No test in this spec sleeps or reads a wall clock.** The one `Task.Delay` below is `Timeout.Infinite` under a cancellation token — a park that never elapses, used to keep a subscription source alive, not a wait. |
| **Dependencies** | None added. Generators are hand-rolled and seeded; TUnit only; no FsCheck, no Gherkin, no BDD framework. |

**Proposed paths:**

| Path | Status |
|---|---|
| `Picea.Abies.Tests/History/UndoRedoSpec.cs` | **new, and this is the locked file** — the acceptance layer and all seven invariant properties |
| `Picea.Abies.Tests/History/HistoryTestProgram.cs` | already in the plan's file table — carries the `EditorProgram` fixture, `CountingSubscription` and the recorders below |
| `Picea.Abies.Tests/History/Generators.cs` | already in the plan's file table — the seeded generators and the fixed-seed corpus |
| `Picea.Abies.Tests/SpecAttribute.cs` | **new, and an addition to the plan's file table** — a five-line `[Spec]` attribute. This repository has none; it exists for `reviewer-reconcile`'s grep, not for execution. Flagged rather than slipped in. |

Everything else stays exactly where `04-realist-plan.md` § *File-Level Changes* puts it. Steps 8, 9
and 10 add their remaining properties to `HistoryInvariantTests.cs`, `HistoryLensLawTests.cs`,
`HistorySecurityRegressionTests.cs`, `HistoryMovementTests.cs` and `HistoryCompositionTests.cs` —
they **extend** this spec, they do not replace it (plan `:1305`).

**Why one file rather than a `*.Specs` project.** The `spec-by-example` skill's default is a
separate test project. It is declined here on a fact, not a preference: `History<TModel>` and
`Step<TModel>` get **internal** constructors, and `Picea.Abies.csproj:24` already carries
`<InternalsVisibleTo Include="Picea.Abies.Tests" />`. A new project would need a `csproj` change,
and `04-realist-plan.md`'s file table lists `Picea.Abies.csproj` under **unchanged — deliberately**,
with the note that touching it is a signal the design has slipped. One locked file inside the
existing test project gives the same clean approval surface — *"I approved
`Picea.Abies.Tests/History/UndoRedoSpec.cs` as of commit `<sha>`"* — at no cost.

---

## The fixture

Support code, referenced by every test below. Its **contract** is part of this spec; its mechanism
is `csharp-dev`'s.

```csharp
// Picea.Abies.Tests/History/HistoryTestProgram.cs
namespace Picea.Abies.Tests.History;

using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;

/// Five fields, chosen so the spec can talk about independence and ownership.
///   Title, Zoom  — two INDEPENDENT regions of the interface, both the user's
///   LoadedFrom   — moved only by command feedback: this is "the world"
///   Ticks        — moved only by a subscription delivery
///   Route        — moved only by an INCOMING UrlChanged: this is the world too, and it
///                  is the one part of the model that something OUTSIDE the application
///                  is also holding a copy of. Added by the 2026-09-07 amendment.
///                  DEFAULTED, deliberately: every EditorModel literal in the acceptance
///                  layer below predates it and none of them had to change.
public sealed record EditorModel(
    string Title, int Zoom, string LoadedFrom, int Ticks, string Route = "/");

public sealed record TitleChanged(string Text) : Message;
public sealed record ZoomChanged(int Level) : Message;
public sealed record LoadRequested : Message;          // issues a command, does not move the model
public sealed record Loaded(string Source) : Message;  // command FEEDBACK — arrives bare
public sealed record SaveRequested : Message;          // issues a command, does not move the model
public sealed record Tick : Message;                   // what the subscription delivers
public sealed record RejectedEdit : Message;           // Decide returns Err for this one
public sealed record EditRejected(string Reason) : Message;  // the Err payload; moves the model
public sealed record BeginBatch : Message;             // decides TWO events
public sealed record BatchFirst : Message;
public sealed record BatchSecond : Message;
public sealed record CloseEditor : Message;            // moves the model into a TERMINAL shape

public sealed record LoadCommand : Command;
public sealed record SaveCommand : Command;
public sealed record FailingCommand : Command;
public sealed record MarkCommand : Command;

public sealed class EditorProgram : Program<EditorModel, Unit>
{
    public static (EditorModel, Command) Initialize(Unit _) =>
        (new EditorModel(Title: "", Zoom: 100, LoadedFrom: "none", Ticks: 0), Commands.None);

    public static (EditorModel, Command) Transition(EditorModel m, Message message)
    {
        EditorLog.Applied(message);
        return message switch
        {
            TitleChanged t  => (m with { Title = t.Text }, Commands.None),
            ZoomChanged z   => (m with { Zoom = z.Level }, Commands.None),
            LoadRequested   => (m, new LoadCommand()),
            Loaded l        => (m with { LoadedFrom = l.Source }, Commands.None),
            SaveRequested   => (m, new SaveCommand()),
            Tick            => (m with { Ticks = m.Ticks + 1 }, Commands.None),
            EditRejected    => (m with { Title = m.Title + "!" }, Commands.None),
            BatchFirst      => (m with { Title = m.Title + "1" }, new FailingCommand()),
            BatchSecond     => (m with { Zoom = m.Zoom + 1 }, new MarkCommand()),
            CloseEditor     => (m with { Zoom = 0 }, Commands.None),
            UrlChanged u    => (m with { Route = u.Url.ToRelativeUri() }, Commands.None),
            // The fall-through is LOAD-BEARING, not a default. `MovementRefused` arrives here,
            // and it must be SILENT: under plan rule S7 a refusal takes the `pass` row, and
            // `pass` calls `sealTops(SealedByEffect(...))` whenever the command is not silent —
            // which would seal both edges under INV-5's once-snapshotted `Both(...)` loop and
            // make its second iteration assert against a stale availability. ⚠️-5(f).
            _               => (m, Commands.None)
        };
    }

    public static Result<Message[], Message> Decide(EditorModel _, Message message) =>
        message switch
        {
            RejectedEdit => Result<Message[], Message>.Err(new EditRejected("title too long")),
            BeginBatch   => Result<Message[], Message>.Ok([new BatchFirst(), new BatchSecond()]),
            _            => Result<Message[], Message>.Ok([message])
        };

    /// A closed editor — zoom zero — is terminal. Reached only by CloseEditor, which is
    /// NOT in the generator alphabet; see the alphabet partition below for why.
    public static bool IsTerminal(EditorModel m) => m.Zoom == 0;

    public static Document View(EditorModel m) =>
        new("Editor", div([], [text($"title:{m.Title};zoom:{m.Zoom};ticks:{m.Ticks};from:{m.LoadedFrom}")]));

    /// Model-derived, and deliberately NON-MONOTONIC in Zoom: the magnifier is declared at
    /// exactly 200 and at no other value. That makes Zoom 200 a state that can be PASSED
    /// THROUGH — declaring a source neither the anchor nor the destination declares — which
    /// is the only shape under which INV-7 can fail. Named here rather than hoped for.
    public static Subscription Subscriptions(EditorModel m) =>
        m.Zoom == 200
            ? SubscriptionModule.Create(EditorLog.MagnifierKey, EditorLog.CountingSource(EditorLog.MagnifierKey))
            : SubscriptionModule.None;
}
```

**The whole fixture cost of the navigation amendment, stated so it can be checked.** One defaulted
record field, one `Transition` case, one generator symbol (`Gen.UserActionsAndNavigation`, below).
Nothing else moves: `View` is **not** changed — `Route` is deliberately not rendered, so no
document text and no `DocumentComparer` result shifts; `EditorMessages.All` is **not** changed, so
INV-3's behaviour-equality sweep is untouched; `EditorLog` gains **no** member, because the property
tracks the browser's location itself from the script it is driving; no policy changes; and no
**existing** acceptance test changes, because `Route` defaults to `"/"` and no other property's
alphabet contains `UrlChanged`, so `Route` is the constant `"/"` everywhere else in this file.
(**A10**, added later the same day for 🟡 7, is a *new* acceptance test that uses these three
additions; it still adds nothing to the fixture, which is why it was free to take.) The two fixture
policies still behave correctly without amendment: `WholeModelHistoryPolicy` remembers `Route` along
with everything else — which is precisely how the divergence becomes reachable — and
`EditorProjectionPolicy.Restore` already keeps `current`'s `Route`, which is the right answer for a
projection, since the URL is not the user's undoable state.

**`EditorLog` — the recorder.** Records, per test, in dispatch order: every message `Transition`
applied, every command the interpreter interpreted, and every subscription start / stop / delivery
with its key. It also exposes `WhenApplied<TMessage>()`, returning a `Task` completed from inside
`Transition` the first time a message of that type is applied.

Two obligations on it, both part of this spec because both are ways a test could silently stop
meaning anything:

1. **Per-test isolation.** TUnit runs tests in parallel and `EditorProgram` is static. The
   recorder must be flowed per test (`AsyncLocal`), or every test in this file must be
   `[NotInParallel]`. ~~`csharp-dev` picks~~ — **amendment 4 picks, because the attribute site is
   inside the lock and `csharp-dev` could not add it later without a hand-back.** The class now
   carries `[NotInParallel("history-editor-log")]`, matching the six sibling classes in this
   project that already do it for process-wide state. An `AsyncLocal`-backed `EditorLog` remains
   welcome and would let the key be relaxed later, but it is no longer load-bearing. Leaking
   state between tests was never an option and now cannot happen by default.
2. **`WhenApplied` is the only ordering mechanism (S28).** `Runtime.cs:288-291` is
   `DispatchFromSubscription(Message) => _ = _replay ? default : Dispatch(message);` — the
   `ValueTask` is discarded, so a delivery has no completion signal and *"then redo"* has no
   ordering guarantee. Every test that dispatches after a delivery awaits
   `EditorLog.WhenApplied<Tick>().WaitAsync(TimeSpan.FromSeconds(5))` first. The precedent is
   `RuntimeIsolationAndSubscriptionFaultTests.cs:38,53`. **No test sleeps.**

```csharp
public static StartSubscription CountingSource(string key) => async (dispatch, ct) =>
{
    EditorLog.SubscriptionStarted(key);
    dispatch(new Tick());                 // delivers WITHOUT being asked — a source that
                                          // should not be live proves it by delivering
    try { await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false); }
    catch (OperationCanceledException) { }
    finally { EditorLog.SubscriptionStopped(key); }
};
```

**Policies.** `WholeModelHistoryPolicy<EditorModel>` is the framework's own. Two fixture policies,
written out in full because C# cannot inherit static abstract implementations:

```csharp
/// The user's state is Title and Zoom. LoadedFrom and Ticks are not theirs to undo.
/// L5 holds trivially: Scrub is the identity, which is always lawful.
public sealed class EditorProjectionPolicy : HistoryPolicy<EditorModel>
{
    public static int Depth => 100;
    public static EditorModel Restore(EditorModel remembered, EditorModel current) =>
        current with { Title = remembered.Title, Zoom = remembered.Zoom };
    public static bool SameUndoable(EditorModel a, EditorModel b) =>
        a.Title == b.Title && a.Zoom == b.Zoom;
    public static EditorModel Scrub(EditorModel model) => model;
}

/// Whole-model, depth 3 — so eviction is reachable in three dispatches instead of a hundred.
public sealed class DepthThreePolicy : HistoryPolicy<EditorModel>
{
    public static int Depth => 3;
    public static EditorModel Restore(EditorModel remembered, EditorModel _) => remembered;
    public static bool SameUndoable(EditorModel a, EditorModel b) => a == b;
    public static EditorModel Scrub(EditorModel model) => model;
}
```

**Aliases**, so the acceptance tests read as sentences rather than as type arguments:

```csharp
using Editor        = WithHistory<EditorProgram, WholeModelHistoryPolicy<EditorModel>, EditorModel, Unit>;
using ProjectedEditor = WithHistory<EditorProgram, EditorProjectionPolicy, EditorModel, Unit>;
using ShallowEditor = WithHistory<EditorProgram, DepthThreePolicy, EditorModel, Unit>;
```

**`Start`** is a one-line helper: `Runtime<TWrapped, History<EditorModel>, Unit>.Start(_ => { },
interpreter)` with a recording `Apply` and the test's interpreter.

---

## Acceptance layer (example-based)

Ten scenarios. Each is one decision point the user settled at a gate, stated concretely enough to
agree or disagree with. This is the layer to read for recognition. (A10 is the tenth, added
2026-09-07 for 🟡 7 — spec line 16 had no acceptance-layer example and it is the pass's most
user-recognisable behaviour.)

```csharp
// Picea.Abies.Tests/History/UndoRedoSpec.cs
namespace Picea.Abies.Tests.History;

using Picea.Abies.History;
// amendment 5 — required by the three `IsEquivalentTo(expected, CollectionOrdering.Matching)`
// calls below. `CollectionOrdering` lives in `TUnit.Assertions.Enums` on the pinned 1.19.57; the
// round-1 report of `CS0103: The name 'CollectionOrdering' does not exist` was a MISSING USING
// read as an absent type, and this line is the whole of the fix.
using TUnit.Assertions.Enums;

[Spec("Undo and redo over the whole of an Abies application's state")]
// ⚠️-6, amendment 4. `Picea.Abies.Tests` has no assembly-level parallel configuration and no
// `.runsettings`, so TUnit's default is parallel; every test in this class reads and writes the
// process-wide static `EditorLog`, and the class calls `EditorLog.Clear()` a dozen times with
// emptiness assertions after it. Six sibling classes in this same project already carry the
// class-level form for exactly this reason — `DiffTests.cs:27`, `RenderTests.cs:24`,
// `HeadDiffTests.cs:15`, `UiComponentRenderTests.cs:8`, `UiAccessibilityContractTests.cs:7`,
// `HotReloadTests.cs:8` — and `NavigationTests.cs` uses the method-level form four times. The
// attribute site is the class declaration, which is INSIDE the lock: it cannot be added at
// step 6 without a `// SPEC CONFLICT:` hand-back, which is why it lands now.
[NotInParallel("history-editor-log")]
// 💡 → taken, amendment 4. Same argument as `[NotInParallel]` above: the attribute site is inside
// the lock, so a hang discovered at step 6 would be a `// SPEC CONFLICT:` hand-back rather than a
// one-line fix. The number is MEASURED, then deliberately over-shot.
//
//   Measurement. `csharp-dev` timed a proxy in the shape of
//   `RuntimeIsolationAndSubscriptionFaultTests.cs` — a real `Runtime` per seed, 24–32 dispatches,
//   a subscription toggling every third dispatch. One seed 0.22 ms; 200 seeds 41.8 ms. Machine:
//   an unshared Ryzen 9 9950X on .NET 10.0.110, so this is a FLOOR, not CI.
//
//   Derivation. 42 ms × 8 properties × 10 = 3,360 ms. The ×10 covers CI slowdown plus what the
//   proxy does not model: the real properties' per-step `Because` string interpolation, the
//   nested `Both(h)` loops, generator construction, and INV-7's extra named seeds.
//
//   Why 30 s and not 3.4 s. The value is INSIDE THE LOCK, so it can only be raised by a
//   re-approval and can never be lowered by a hand-back that matters. Over-shooting costs
//   nothing — a green suite never reaches the timeout — while under-shooting costs a
//   false red on a loaded CI runner and a hand-back on the pass's most load-bearing file.
//   Asymmetric, so round generously. If it ever fires, that is a real hang, not a slow machine.
//
//   WHAT IT DOES AND DOES NOT BUY (⚠️-8, measured on 1.19.57, amendment 5). The user's decision
//   to include it stands; the limit is stated here rather than discovered at step 6.
//     - It DOES fire on an await-shaped body: a `[Timeout(2_000)]` test awaiting
//       `Task.Delay(6_000)` failed at 2 s with TUnit's `TimeoutException`, with no
//       `CancellationToken` parameter present. The seed loops are await-dense and a real hang in
//       them is async-shaped, so this is the case the attribute was chosen for.
//     - It does NOT fire on a synchronous spin: a `[Timeout(2_000)]` test spinning 6 s PASSED.
//       A property wedged in a tight non-awaiting loop is not covered by this attribute at all.
//     - A timed-out body KEEPS RUNNING after TUnit records the failure — the probe printed
//       `body completed after 5999 ms` well after the test was reported failed. On a class whose
//       correctness rests on the process-wide static `EditorLog`, an orphaned body still
//       dispatching into that log will corrupt whichever test runs next. `[NotInParallel]` does
//       NOT prevent this: the orphan is a detached continuation, not a test. So a timeout here
//       is a diagnosis of one hang, not a containment of it — treat the first red after a
//       timeout as suspect rather than as a second independent failure.
//     - Class-level `[Timeout]` on parameterless `[Test]` methods emits `TUnit0015: Missing
//       TimeoutAttribute cancellation token parameter`, once per test method. Nothing sets
//       `TreatWarningsAsErrors`, so step 6 gains 25 WARNINGS, not 25 errors — but round 1's
//       "build is clean, 0 warnings" stops being true the moment the csproj exclusion comes off.
//       The remedy is a `CancellationToken` parameter on each `[Test]`, which is a SIGNATURE
//       change: neither an assertion nor a claim, therefore OUTSIDE Decision 1's lock and
//       available to step 6 without a hand-back. Stated because that reading is not obvious.
[Timeout(30_000)]
public sealed class UndoRedoSpec
{
    // ── A1 ───────────────────────────────────────────────────────────────────────
    // The happy path. One dispatch is one undoable step (open question 1), there is
    // ONE history for the application (INV-1), and redo returns what undo took (INV-3).

    [Test]
    public async Task Undoing_three_edits_walks_back_one_edit_per_press_and_redo_returns_them_in_order()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("Draft"));
        await runtime.Dispatch(new ZoomChanged(150));          // a DIFFERENT region of the interface
        await runtime.Dispatch(new TitleChanged("Draft two"));

        await Assert.That(runtime.Model.Present)
            .IsEqualTo(new EditorModel("Draft two", 150, "none", 0));

        await runtime.Dispatch(new HistoryMessage.Undo());
        await Assert.That(runtime.Model.Present)
            .IsEqualTo(new EditorModel("Draft", 150, "none", 0));

        await runtime.Dispatch(new HistoryMessage.Undo());      // crosses back into the OTHER region
        await Assert.That(runtime.Model.Present)
            .IsEqualTo(new EditorModel("Draft", 100, "none", 0));

        await runtime.Dispatch(new HistoryMessage.Undo());
        await Assert.That(runtime.Model.Present)
            .IsEqualTo(new EditorModel("", 100, "none", 0));

        await runtime.Dispatch(new HistoryMessage.Redo());
        await runtime.Dispatch(new HistoryMessage.Redo());
        await runtime.Dispatch(new HistoryMessage.Redo());

        await Assert.That(runtime.Model.Present)
            .IsEqualTo(new EditorModel("Draft two", 150, "none", 0));
        await Assert.That(EditorLog.CommandsInterpreted).IsEmpty();   // undo emits no effect
    }

    // ── A2 ───────────────────────────────────────────────────────────────────────
    // Gate 2, in use: undo refuses to cross an action that has already spoken to the
    // world, the refusal is TYPED, it NAMES the message, and it is answerable from the
    // history value BEFORE the press (INV-5 + INV-6). Both directions refuse (S9).

    [Test]
    public async Task Undo_refuses_across_a_command_boundary_and_names_the_message_before_the_press()
    {
        using var runtime = await Start<Editor>(LoadReturns("server"));

        await runtime.Dispatch(new TitleChanged("Draft"));
        await runtime.Dispatch(new LoadRequested());            // issues LoadCommand
        await EditorLog.WhenApplied<Loaded>().WaitAsync(TimeSpan.FromSeconds(5));

        var h = runtime.Model;
        await Assert.That(h.Present.LoadedFrom).IsEqualTo("server");

        // BEFORE the press, from the value alone, with no effect and no attempt:
        await Assert.That(History.Backward(h))
            .IsEqualTo(new MovementAvailability.BlockedByWorld(new Loaded("server")));

        EditorLog.Clear();
        await runtime.Dispatch(new HistoryMessage.Undo());

        // The refusal arrives as a VALUE, along the ordinary message path:
        await Assert.That(EditorLog.MessagesApplied).Contains(
            new MovementRefused(Direction.Backward,
                                new MovementAvailability.BlockedByWorld(new Loaded("server"))));
        await Assert.That(runtime.Model.Present).IsEqualTo(h.Present);   // nothing moved
    }

    [Test]
    public async Task Undo_refuses_across_an_action_that_issued_a_command_and_names_that_action()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("Draft"));
        await runtime.Dispatch(new SaveRequested());            // issues SaveCommand, no feedback

        await Assert.That(History.Backward(runtime.Model))
            .IsEqualTo(new MovementAvailability.BlockedByEffect(new SaveRequested()));
    }

    // ── A3 ───────────────────────────────────────────────────────────────────────
    // The answer to 00-scope.md open question 3, and the change to a gate-2 rule.
    // Acting after an undo does NOT discard the forward branch (S26).

    [Test]
    public async Task Acting_after_an_undo_preserves_the_forward_branch_and_redo_refuses_by_name()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("one"));
        await runtime.Dispatch(new TitleChanged("one two"));
        await runtime.Dispatch(new HistoryMessage.Undo());      // Present = "one", Future has a step

        await runtime.Dispatch(new ZoomChanged(150));           // the user acts instead of redoing

        var h = runtime.Model;
        await Assert.That(h.Future.Count).IsEqualTo(1);         // PRESERVED, not cleared
        await Assert.That(History.Forward(h)).IsEqualTo(
            new MovementAvailability.BlockedBySupersedingAction(new ZoomChanged(150)));

        EditorLog.Clear();
        await runtime.Dispatch(new HistoryMessage.Redo());

        await Assert.That(EditorLog.MessagesApplied).Contains(
            new MovementRefused(Direction.Forward,
                new MovementAvailability.BlockedBySupersedingAction(new ZoomChanged(150))));
        await Assert.That(runtime.Model.Present.Title).IsEqualTo("one");   // never crossed

        // And the undo the user JUST did is still redoable-adjacent: a fresh undo pushes a
        // Crossable step above the superseded one, so undo/redo keep working locally.
        await runtime.Dispatch(new HistoryMessage.Undo());
        await Assert.That(History.Forward(runtime.Model))
            .IsEqualTo(new MovementAvailability.Available(2));
    }

    // ── A4 ───────────────────────────────────────────────────────────────────────
    // INV-4 concretely, and the distinction INV-5 exists to protect: "nothing to undo"
    // and "undo declined" must not look the same to the application.

    [Test]
    public async Task Undo_at_the_start_of_the_history_changes_nothing_and_tells_the_application_nothing()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        var before = runtime.Model;
        await Assert.That(History.Backward(before)).IsEqualTo(new MovementAvailability.Nothing());

        EditorLog.Clear();
        await runtime.Dispatch(new HistoryMessage.Undo());      // does not throw

        await Assert.That(runtime.Model).IsEqualTo(before);
        await Assert.That(EditorLog.MessagesApplied).IsEmpty();      // Ok([]) short-circuits
        await Assert.That(EditorLog.PatchesApplied).IsEmpty();
        await Assert.That(EditorLog.CommandsInterpreted).IsEmpty();

        // The whole point: what the application observes here is EMPTY, and what it observes
        // for a refusal (A2, A3) is a MovementRefused. An observer with access only to what
        // the application received can tell the two apart.
    }

    // ── A5 ───────────────────────────────────────────────────────────────────────
    // Gate-5 decision 2 (B8): a rejected decision is the application's own response to the
    // user's own message, not the world. Spec line 3.

    [Test]
    public async Task A_rejected_decision_is_the_applications_own_response_and_records_an_undo_stop()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("Draft"));
        await runtime.Dispatch(new RejectedEdit());             // Decide returns Err(EditRejected)

        await Assert.That(runtime.Model.Present.Title).IsEqualTo("Draft!");   // the Err moved the model

        // A record, NOT a seal — undo is still available and is not blocked by "the world".
        await Assert.That(History.Backward(runtime.Model))
            .IsEqualTo(new MovementAvailability.Available(2));

        await runtime.Dispatch(new HistoryMessage.Undo());
        await Assert.That(runtime.Model.Present.Title).IsEqualTo("Draft");
    }

    // ── A6 ───────────────────────────────────────────────────────────────────────
    // The two behavioural differences between a wrapped and an unwrapped program, both
    // asserted AGAINST the unwrapped program so the difference is recorded as a difference
    // rather than as a passing test. Spec lines 6 and 7.

    [Test]
    public async Task A_multi_event_decision_applies_every_event_before_any_command_is_interpreted()
    {
        using var wrapped = await Start<Editor>(RecordingInterpreter);
        await wrapped.Dispatch(new BeginBatch());
        var wrappedOrder = EditorLog.Timeline;

        EditorLog.Clear();
        using var bare = await StartBare(RecordingInterpreter);
        await bare.Dispatch(new BeginBatch());
        var bareOrder = EditorLog.Timeline;

        // 🔴-2 (amendment 4) then 🔴-5 (amendment 5). The claim these two assertions make is an
        // ORDERING claim: the two expected sequences are permutations of the same four strings,
        // and the whole point of the test is which permutation occurred. Two shapes have been
        // tried and only the third is right, so the facts are recorded here rather than the
        // history — under Decision 1 this paragraph is a LOCKED CLAIM, and the previous version
        // of it stated two falsehoods about TUnit as fact, on a review's authority.
        //
        // Verified on the pinned TUnit 1.19.57, each with BOTH a passing and a failing control:
        //   - `IsEquivalentTo(expected)` is ORDER-INSENSITIVE. It passed on a permutation, so
        //     these assertions could not fail for the reason they exist. (Round 1, upheld.)
        //   - `IsEqualTo(expected)` on a collection compiles and is NOT order-sensitive — it is
        //     UNSATISFIABLE. The collection expression is target-typed to a
        //     `<>z__ReadOnlyArray<string>` and compared by `Equals`, i.e. by reference, so it
        //     fails on the MATCHING sequence too:
        //         `Expected to be equal to <>z__ReadOnlyArray`1[System.String]
        //          but received <>z__ReadOnlyArray`1[System.String]`
        //     Swept across `string[]`, `List<string>`, `IEnumerable<string>`,
        //     `IReadOnlyList<string>`, `ImmutableArray<string>` and `ImmutableList<string>` —
        //     all six fail on identical content — and against a bespoke `[CollectionBuilder]`
        //     type with correct `IEquatable<T>` value equality, which fails while printing
        //     `equals-new=True` on the line before. No support-file choice for
        //     `EditorLog.Timeline` rescues it. (Round 2, 🔴-5.)
        //   - `IsEquivalentTo(expected, CollectionOrdering.Matching)` — matching order passes,
        //     permutation fails. Verified on `string[]`, `IEnumerable<string>` and
        //     `IReadOnlyList<string>` subjects. `CollectionOrdering` is in
        //     `TUnit.Assertions.Enums`, imported at the top of this file. THIS is the shape.
        await Assert.That(wrappedOrder).IsEquivalentTo([
            "applied:BatchFirst", "applied:BatchSecond",
            "interpreted:FailingCommand", "interpreted:MarkCommand"],
            CollectionOrdering.Matching);

        await Assert.That(bareOrder).IsEquivalentTo([
            "applied:BatchFirst", "interpreted:FailingCommand",
            "applied:BatchSecond", "interpreted:MarkCommand"],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task A_failing_command_skips_later_commands_where_unwrapped_it_would_skip_later_events()
    {
        using var wrapped = await Start<Editor>(FirstCommandFails);
        await wrapped.Dispatch(new BeginBatch());

        // COMPLETE MODEL, PARTIAL EFFECTS — both events applied, the second command skipped.
        await Assert.That(wrapped.Model.Present.Title).IsEqualTo("1");
        await Assert.That(wrapped.Model.Present.Zoom).IsEqualTo(101);
        await Assert.That(EditorLog.CommandsInterpreted).DoesNotContain("MarkCommand");

        EditorLog.Clear();
        using var bare = await StartBare(FirstCommandFails);
        await bare.Dispatch(new BeginBatch());

        // PARTIAL MODEL, NO LATER EFFECTS — the unwrapped program's opposite behaviour.
        await Assert.That(bare.Model.Title).IsEqualTo("1");
        await Assert.That(bare.Model.Zoom).IsEqualTo(100);
        await Assert.That(EditorLog.CommandsInterpreted).DoesNotContain("MarkCommand");
    }

    // ── A7 ───────────────────────────────────────────────────────────────────────
    // What a movement IS. A bracketed run is one movement; an un-bracketed press is a
    // complete movement. Spec lines 8 and 9, and S25(a)'s in-bracket truth.

    [Test]
    public async Task A_held_bracket_reconciles_subscriptions_once_against_the_state_it_settles_on()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new ZoomChanged(200));           // magnifier declared
        await EditorLog.WhenApplied<Tick>().WaitAsync(TimeSpan.FromSeconds(5));
        await runtime.Dispatch(new ZoomChanged(300));           // magnifier gone

        EditorLog.Clear();
        await runtime.Dispatch(new HistoryMessage.Hold());
        await runtime.Dispatch(new HistoryMessage.Undo());      // passes THROUGH Zoom 200
        await runtime.Dispatch(new HistoryMessage.Undo());      // lands on Zoom 100
        await runtime.Dispatch(new HistoryMessage.Settle());

        await Assert.That(runtime.Model.Present.Zoom).IsEqualTo(100);
        await Assert.That(EditorLog.SubscriptionActivity).IsEmpty();   // nothing started,
        await Assert.That(EditorLog.MessagesApplied.OfType<Tick>()).IsEmpty();  // nothing arrived
    }

    [Test]
    public async Task Two_un_bracketed_presses_are_two_movements_and_reconcile_twice()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new ZoomChanged(200));
        await EditorLog.WhenApplied<Tick>().WaitAsync(TimeSpan.FromSeconds(5));
        await runtime.Dispatch(new ZoomChanged(300));

        EditorLog.Clear();
        await runtime.Dispatch(new HistoryMessage.Undo());      // LANDS on Zoom 200
        await runtime.Dispatch(new HistoryMessage.Undo());      // lands on Zoom 100

        // Stated rather than softened: this is legal. An un-bracketed press is a complete
        // movement with no states passed through, so INV-7 is satisfied by definition and
        // the magnifier really does start and stop.
        //
        // 🔴-2's third instance (amendment 4), corrected by 🔴-5 (amendment 5). *"Reconcile
        // twice"* is an ORDERING claim: under bare `IsEquivalentTo` a *stop then start* — the
        // magnifier surviving the first press and being torn down on the second — passed
        // identically. Amendment 4 reached for `IsEqualTo`, which on a collection is not
        // order-sensitive but UNSATISFIABLE; see A6 above for the measurements and both
        // controls. The `CollectionOrdering.Matching` overload is the shape that discriminates
        // order and can still go green.
        await Assert.That(EditorLog.SubscriptionActivity).IsEquivalentTo([
            $"start:{EditorLog.MagnifierKey}", $"stop:{EditorLog.MagnifierKey}"],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task A_message_arriving_during_a_bracket_records_a_transit_state_and_supersedes_the_branch()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("one"));
        await runtime.Dispatch(new TitleChanged("one two"));
        await runtime.Dispatch(new HistoryMessage.Hold());
        await runtime.Dispatch(new HistoryMessage.Undo());      // Present = "one", mid-movement

        await runtime.Dispatch(new ZoomChanged(150));           // the world writes under the run

        var h = runtime.Model;
        await Assert.That(h.Movement).IsTypeOf<Movement.Held>();          // the run does NOT end
        await Assert.That(h.Past.Peek().Model.Title).IsEqualTo("one");    // a TRANSIT state, recorded
        await Assert.That(History.Forward(h)).IsEqualTo(
            new MovementAvailability.BlockedBySupersedingAction(new ZoomChanged(150)));
    }

    // ── A8 ───────────────────────────────────────────────────────────────────────
    // The single most consequential adoption fact in the design. Spec lines 4 and 5.

    [Test]
    public async Task Under_the_whole_model_policy_a_background_tick_supersedes_the_redo_branch_by_name()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("one"));
        await runtime.Dispatch(new TitleChanged("one two"));
        await runtime.Dispatch(new HistoryMessage.Undo());
        await Assert.That(History.Forward(runtime.Model))
            .IsEqualTo(new MovementAvailability.Available(1));

        await runtime.Dispatch(new ZoomChanged(200));           // starts the source
        await EditorLog.WhenApplied<Tick>().WaitAsync(TimeSpan.FromSeconds(5));  // S28 — no sleep

        // Told, by name, in advance — not silence.
        await Assert.That(History.Forward(runtime.Model))
            .IsEqualTo(new MovementAvailability.BlockedBySupersedingAction(new Tick()));
    }

    [Test]
    public async Task An_application_that_declares_a_projection_keeps_its_redo_branch_across_a_tick()
    {
        using var runtime = await Start<ProjectedEditor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("one"));
        await runtime.Dispatch(new TitleChanged("one two"));
        await runtime.Dispatch(new HistoryMessage.Undo());
        await runtime.Dispatch(new Tick());                     // outside the projection → pass

        await Assert.That(History.Forward(runtime.Model))
            .IsEqualTo(new MovementAvailability.Available(1));

        await runtime.Dispatch(new HistoryMessage.Redo());
        await Assert.That(runtime.Model.Present.Title).IsEqualTo("one two");
        await Assert.That(runtime.Model.Present.Ticks).IsEqualTo(1);   // the tick is NOT undone
    }

    [Test]
    public async Task Under_the_whole_model_policy_a_recurring_tick_also_evicts_the_users_own_undo_stops()
    {
        using var runtime = await Start<ShallowEditor>(NoFeedback);   // Depth 3

        await runtime.Dispatch(new TitleChanged("one"));
        await runtime.Dispatch(new TitleChanged("one two"));

        await runtime.Dispatch(new Tick());
        await runtime.Dispatch(new Tick());
        await runtime.Dispatch(new Tick());

        // S25(b), made mechanical: reach is bounded by Depth ÷ tick rate. After Depth ticks
        // the user's own steps are gone. Dispatched directly rather than timed, because a
        // delivered Tick and a dispatched Tick are indistinguishable at the wrapper's seam —
        // which is exactly what B9 established.
        await Assert.That(runtime.Model.Past.Count).IsEqualTo(3);
        await Assert.That(runtime.Model.Past.All(s => s.Cause is Tick)).IsTrue();
    }

    // ── A9 ───────────────────────────────────────────────────────────────────────
    // Spec line 12, moved into the lock by the user's amendment. A program whose model
    // reached a terminal state is exactly the case where a user most wants to back out of
    // it, so the history keeps working: Decide's history cases precede the terminal guard,
    // and IsTerminal(h) is TProgram.IsTerminal(h.Present) AND both stacks empty.

    [Test]
    public async Task Undo_remains_dispatchable_out_of_a_terminal_state_and_resurrects_the_program()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("Draft"));
        await runtime.Dispatch(new CloseEditor());

        // The APPLICATION has terminated…
        await Assert.That(EditorProgram.IsTerminal(runtime.Model.Present)).IsTrue();
        // …and ordinary messages are ignored from here on:
        await runtime.Dispatch(new TitleChanged("ignored"));
        await Assert.That(runtime.Model.Present.Title).IsEqualTo("Draft");

        // …but the WRAPPED program is not terminal, because the history is not empty,
        // and undo is still available and still dispatchable.
        await Assert.That(Editor.IsTerminal(runtime.Model)).IsFalse();
        await Assert.That(History.Backward(runtime.Model))
            .IsEqualTo(new MovementAvailability.Available(2));

        await runtime.Dispatch(new HistoryMessage.Undo());

        // Resurrected — and the application accepts work again.
        await Assert.That(EditorProgram.IsTerminal(runtime.Model.Present)).IsFalse();
        await Assert.That(runtime.Model.Present.Zoom).IsEqualTo(100);
        await runtime.Dispatch(new TitleChanged("Draft two"));
        await Assert.That(runtime.Model.Present.Title).IsEqualTo("Draft two");
    }

    [Test]
    public async Task A_terminated_program_with_no_history_left_is_terminal()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("Draft"));
        await runtime.Dispatch(new CloseEditor());
        await runtime.Dispatch(new HistoryMessage.Clear());

        // The other half of the conjunction. Without this, A9 would still pass if
        // IsTerminal(h) were hardcoded false — which is a different bug, not this feature.
        await Assert.That(runtime.Model.Past).IsEmpty();
        await Assert.That(runtime.Model.Future).IsEmpty();
        await Assert.That(Editor.IsTerminal(runtime.Model)).IsTrue();
    }

    // ── A10 ──────────────────────────────────────────────────────────────────────
    // Spec line 16 in the acceptance layer, added 2026-09-07 for 🟡 7. "Type, press Back,
    // press undo — it refuses and names the navigation" is the shape a user recognises, and
    // recognition is the only job this layer has; until now line 16's sole home was a
    // property. It costs NO fixture: UrlChanged is the framework's own message, EditorProgram
    // already has a Transition case for it, and Route is already a defaulted field.
    //
    // SCOPE — and this is the whole of B10. This test drives the FRAMEWORK's UrlChanged. An
    // application that maps incoming navigation to a message of its own (README.md:190's
    // shape, via the application-supplied converter at Navigation.cs:17-29) gets no seal, and
    // neither this test nor INV-2's second property would see it. That case is obligation 17,
    // the application's to classify, and it is deliberately outside what this test claims.

    [Test]
    public async Task Navigating_with_the_browsers_own_back_button_seals_the_history_and_undo_names_the_navigation()
    {
        using var runtime = await Start<Editor>(NoFeedback);

        await runtime.Dispatch(new TitleChanged("Draft"));       // Origin becomes Established
        await runtime.Dispatch(new TitleChanged("Draft two"));
        await runtime.Dispatch(new HistoryMessage.Undo());       // now a step on BOTH stacks

        // The user presses the browser's own BACK button. The head delivers a navigation the
        // application never asked for.
        var back = new UrlChanged(Url.FromUri(new Uri("https://app.test/articles/2")));
        await runtime.Dispatch(back);

        // The model's URL moved with the browser, in the same transition — the two move
        // together, which is the whole of INV-2's location clause.
        await Assert.That(runtime.Model.Present.Route).IsEqualTo("/articles/2");

        // BOTH edges sealed, both answerable from the value BEFORE any press, both naming the
        // navigation. Compared against the dispatched INSTANCE: Url carries an
        // IReadOnlyDictionary, whose record equality is reference equality.
        await Assert.That(History.Backward(runtime.Model))
            .IsEqualTo(new MovementAvailability.BlockedByWorld(back));
        await Assert.That(History.Forward(runtime.Model))
            .IsEqualTo(new MovementAvailability.BlockedByWorld(back));

        EditorLog.Clear();
        await runtime.Dispatch(new HistoryMessage.Undo());

        await Assert.That(EditorLog.MessagesApplied).Contains(
            new MovementRefused(Direction.Backward,
                                new MovementAvailability.BlockedByWorld(back)));

        // Nothing moved — and in particular the URL the application shows is still the one
        // the browser is showing.
        await Assert.That(runtime.Model.Present.Title).IsEqualTo("Draft");
        await Assert.That(runtime.Model.Present.Route).IsEqualTo("/articles/2");
    }
}
```

---

## Invariant layer (property-based)

One property per `INV-n`, named so the id is greppable, and carrying `[Property("Invariant",
"INV-n")]`. Two mechanisms, because a rename that breaks the name should not silently drop coverage.

### The generator, and the alphabet partition (S27)

Hand-rolled, seeded, no new dependency. A `Script` is a list of steps, each either a message to
dispatch or a pulse of the autonomous source. Each property is a single `[Test]` that walks the
**fixed seed corpus** and **prints the failing seed in every assertion message** — that is what
makes a hand-rolled generator debuggable and it is not optional.

```csharp
file static class Gen
{
    /// CI runs exactly these, plus the named shapes. A property that goes red names its seed.
    public static readonly int[] Corpus = [.. Enumerable.Range(1, 200)];

    /// Shapes random generation over a small model may never produce, seeded EXPLICITLY
    /// rather than hoped for (plan step 8 🟡 3, step 9's generator note):
    ///   - a state INSIDE a bracket declaring a source neither anchor nor destination declares
    ///   - undo → delivery-that-moves-the-projection → redo
    ///   - Settle with no hold; Hold twice; a hold left open
    ///   - a decision rejected by Decide whose error message moves the projection
    public static readonly Script[] NamedShapes = [ /* … */ ];

    /// TitleChanged | ZoomChanged | LoadRequested | SaveRequested | RejectedEdit | BeginBatch
    /// | Undo | Redo | Hold | Settle | Clear
    ///
    /// NOT in the alphabet: CloseEditor, and ZoomChanged never draws 0. Both would put the
    /// fixture into a TERMINAL state, in which ordinary messages become no-ops that record
    /// nothing — so INV-1's "the state before the most recent action" would be the state a
    /// no-op did not move, and the property would go red for correct behaviour. Same shape of
    /// exclusion as the delivery partition below, and stated for the same reason.
    /// Terminality is specified by A9 instead, which dispatches CloseEditor directly.
    public static Script UserActions(int seed, int length);

    /// The above, PLUS pulses of the autonomously-delivering source.
    public static Script UserActionsAndDeliveries(int seed, int length);

    /// `UserActions`, PLUS incoming `UrlChanged` — the browser's own back/forward button
    /// delivering navigation the application did not ask for. Two constraints on where the
    /// symbol may be drawn, both required rather than tidy:
    ///   - NEVER before the first recording action. While `Origin` is `Fresh` a `UrlChanged`
    ///     RE-BASES and records nothing (plan `:1011`, `:1114`), so a script that navigates
    ///     first would exercise the bootstrap path, not the classification this property is
    ///     about. That path is spec line 10's and stays in plan step 6.
    ///   - Each drawn `UrlChanged` carries a FRESH `Url` whose `ToRelativeUri()` differs from
    ///     the one before it, so a missing seal shows up as a changed `Route` rather than
    ///     coincidentally agreeing with it.
    public static Script UserActionsAndNavigation(int seed, int length);
}
```

**The partition, and why it is a partition rather than a judgement call per property.** Under this
design **a subscription delivery is an action** — it records an undo stop and supersedes the
forward branch, by gate-5 decision 1. Two properties are therefore *falsified by correct
behaviour* if the delivering source is in their alphabet, and the Critic is right that the likely
implementation-time outcomes (drop the source from step 8 entirely, or weaken the two properties
until they pass) are both invisible in a diff:

| Property | Alphabet | Reason |
|---|---|---|
| INV-1 | `UserActions` | **Must exclude.** A delivery is an action, so `actionA → delivery → undo` correctly lands before the *delivery*; INV-1 as written ("the most recent action") would go red for a mechanism behaving exactly as specified. |
| INV-3 | `UserActions` | **Must exclude.** A delivery between the undo and the redo supersedes the fresh `Crossable` step, so the redo correctly refuses and the round trip cannot be asserted. |
| INV-2 *(first property)*, INV-4, INV-6 | `UserActions` | Unaffected either way; kept on the same alphabet so the rule is one rule. INV-6's `BlockedBySupersedingAction` case is still reached, because a *user action* after an undo supersedes exactly as a delivery does. |
| INV-5, INV-7 | `UserActionsAndDeliveries` | **Must include.** INV-5's is the property that can see a tick destroy a redo (plan step 8(k)); INV-7's is the property that can see a source declared only by a transit state deliver. |
| **INV-2 *(second property)*** | **`UserActionsAndNavigation`** | **Must include, and no other property may.** This is the only property whose claim is about the URL, and it is the only one that can see the divergence: with `UrlChanged` absent from an alphabet, `Route` is constant and the bug is invisible. |

Every delivery is ordered by `EditorLog.WhenApplied<Tick>()` before the next dispatch (S28). No
property sleeps.

**Why `UrlChanged` is excluded from `UserActions` — and it is a *uniform* exclusion, not a required
one.** Unlike the delivering source, incoming navigation does not falsify any other property by
behaving correctly: under the amended rule it *seals*, so INV-1 and INV-3 simply skip on their
`Available` guards, and INV-2's first property still holds because `ReachableByOrdinaryActionsAlone`
replays the navigations too. It is excluded because keeping it out costs those properties nothing
and keeps one alphabet rule per property; and because the case it would cover — a *typed* refusal
naming a navigation — is a strictly stronger claim than "some refusal happened", and it is asserted
directly below rather than incidentally. Stated explicitly so that a future reader does not read
the exclusion as required and preserve it for the wrong reason.

### The properties

```csharp
    // ── INV-1 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-1")]
    public async Task INV_1_a_single_undo_returns_the_state_before_the_most_recent_action_whichever_region_it_came_from()
    {
        // 🔴-3's closing paragraph, amendment 4. The two `continue` guards below are correct in
        // themselves, but nothing recorded that ANY seed reached the assertion — so a `Gen` that
        // regressed to trivial scripts would leave this property green and empty. One counter,
        // one floor assertion after the loop. The same shape is applied to INV-3, INV-4 and
        // INV-5; INV-6 and INV-2's second property get the stronger coverage assertions their
        // own prose already promised.
        var reached = 0;

        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);     // S27: no deliveries here
            using var runtime = await Start<Editor>(NoFeedback);

            EditorModel? beforeMostRecentAction = null;
            foreach (var step in script)
            {
                if (step.IsOrdinaryAction) beforeMostRecentAction = runtime.Model.Present;
                await runtime.Dispatch(step.Message);
            }

            // Quantified over histories where undo is AVAILABLE — INV-1's clause "quantified
            // over histories in which undo is available"; cited by id and clause, not by line
            // range, because the scope file moves (🟡 3) —
            // a typed refusal under INV-5 is not a violation of this invariant.
            if (History.Backward(runtime.Model) is not MovementAvailability.Available) continue;
            if (beforeMostRecentAction is null) continue;

            await runtime.Dispatch(new HistoryMessage.Undo());
            reached++;

            await Assert.That(runtime.Model.Present)
                .IsEqualTo(beforeMostRecentAction)
                .Because($"seed {seed}: one history for the application, regardless of which " +
                         $"region the most recent action came from");
        }

        await Assert.That(reached > 0).IsTrue()
            .Because($"no seed in the corpus reached an available undo, so this property is " +
                     $"green having asserted nothing (0 of {Gen.Corpus.Length} seeds)");
    }

    // ── INV-2 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-2")]
    public async Task INV_2_every_state_reached_by_undo_or_redo_is_reachable_by_ordinary_actions_alone()
    {
        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);

            // The reachable set, computed by REPLAYING the ordinary actions of the script
            // through the UNWRAPPED program — no history, no undo, no redo. Policy-independent,
            // so it is computed once and used for both lenses below.
            var reachable = ReachableByOrdinaryActionsAlone(script);

            // 🔴-3(b), amendment 4. The prose below this property has always said "INV-2 runs
            // twice, over Editor (whole-model) and ProjectedEditor", and the INV-n table has
            // always said "under BOTH the whole-model and the projection policy". It ran once,
            // over Editor; `ProjectedEditor` appeared nowhere in the invariant layer. The claim
            // is load-bearing rather than decorative — under the whole-model lens undo MOVES to
            // a remembered model and the invariant holds by construction, whereas under a
            // projection undo COMPUTES `Restore(remembered, present)` and the invariant holds
            // only up to L1–L6 plus independent reachability. Testing only the lens where it
            // holds by construction is testing the half that cannot fail. So the code is made
            // true to the claim rather than the claim weakened to the code.

            using (var wholeModel = await Start<Editor>(NoFeedback))
            {
                foreach (var step in script)
                {
                    await wholeModel.Dispatch(step.Message);
                    await Assert.That(reachable).Contains(wholeModel.Model.Present)
                        .Because($"seed {seed}, whole-model lens: undo may not put the " +
                                 $"application into a state it was never in and could never " +
                                 $"have been in");
                }
            }

            using (var projected = await Start<ProjectedEditor>(NoFeedback))
            {
                foreach (var step in script)
                {
                    await projected.Dispatch(step.Message);
                    await Assert.That(reachable).Contains(projected.Model.Present)
                        .Because($"seed {seed}, projection lens: `Restore(remembered, present)` " +
                                 $"may not compose a model out of two states that produces one " +
                                 $"the application was never in");
                }
            }
        }
    }
```

> **INV-2 runs twice**, over `Editor` (whole-model) and `ProjectedEditor`. Under the whole-model
> lens undo *moves* to a remembered model and the invariant holds by construction. Under a
> projection undo *computes* `Restore(remembered, present)` and the invariant holds up to L1–L6
> **plus** an assumption the laws do not capture: that the projection and its complement are
> independently reachable in the application's transition system. `EditorModel` is built so they
> are — `Title`/`Zoom` and `LoadedFrom`/`Ticks` move independently. **That assumption is not
> property-testable by the framework** (Cleanness compromise 1) and this property does not claim
> to test it; it is spec line 2's obligation on the application.
>
> **This paragraph was true as intent and false as a description of the code until amendment 4**
> (🔴-3(b)): the property ran once, over `Editor`, and `ProjectedEditor` appeared nowhere in the
> invariant layer. The body now walks both lenses. Note which half that added — the whole-model
> half is the one that holds *by construction*, so what was being tested was the half that cannot
> fail.

**INV-2 has a second property, and why it is a second one rather than a wider alphabet.** The
2026-09-07 finding is that the browser's location is part of the world, and therefore part of the
state INV-2 quantifies over: a `(model, browser location)` pair the application could never have
produced by ordinary action is exactly what INV-2 forbids. The first property cannot see this even
with `UrlChanged` in its alphabet — a stale `Route` restored by undo *is* a model the application
was once in, so `ReachableByOrdinaryActionsAlone` contains it and the assertion stays green while
the two stacks diverge. The claim needs the browser's location on the other side of the comparison,
which is a different assertion, so it is a different property. Both carry
`[Property("Invariant", "INV-2")]`; `validate-phase-artifact.sh` matches on the id and does not cap
the count, and the departure from the skill's *exactly one property per `INV-n`* is the user's, made
as approver on 2026-09-07.

```csharp
    // ── INV-2, second property ───────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-2")]
    public async Task INV_2_no_movement_lands_on_a_model_whose_url_is_not_the_one_the_browser_shows()
    {
        // 🔴-3(c), amendment 4. The note below this property has always said "the property is
        // not done until both branches have been observed taken at least once" — and nothing
        // counted them. A corpus in which `Future` was always empty left the property green
        // having checked one edge, which is exactly the outcome the note claims it prevents.
        // The two flags below are that claim, made mechanical.
        var pastEdgeChecked = false;
        var futureEdgeChecked = false;

        foreach (var seed in Gen.Corpus)
        {
            // Whole-model policy DELIBERATELY: it is the policy that remembers Route, and so
            // the only one under which an undo can restore a URL the browser is not showing.
            // Under a projection the URL is not the user's undoable state and Restore keeps
            // the current one, which is a different (and correct) behaviour, not this claim.
            var script = Gen.UserActionsAndNavigation(seed, length: 24);
            using var runtime = await Start<Editor>(NoFeedback);

            // What the browser is showing. Nothing in this fixture issues a navigation
            // command, so the browser's location moves ONLY when the head delivers a
            // UrlChanged — which is exactly the step this loop is driving. That is the whole
            // model of "the browser" here, and it is honest precisely because the fixture
            // cannot navigate on its own.
            var browserShows = "/";
            UrlChanged? mostRecentNavigation = null;

            foreach (var step in script)
            {
                if (step.Message is UrlChanged nav)
                {
                    browserShows = nav.Url.ToRelativeUri();
                    mostRecentNavigation = nav;
                }

                await runtime.Dispatch(step.Message);

                // THE CLAIM, after every step and so after every Undo and every Redo: the
                // application's URL is the one the browser shows. This is the falsifier the
                // user named — an undo that lands on a model whose URL is not the one the
                // browser shows — asserted as a standing property rather than as one case.
                await Assert.That(runtime.Model.Present.Route)
                    .IsEqualTo(browserShows)
                    .Because($"seed {seed}: the browser's location is part of the world, so a " +
                             $"movement may not land on a model carrying a different URL");

                // …and the reason it cannot is TYPED, NAMES the navigation, and is answerable
                // from the value alone before any press — in BOTH directions, because the
                // rule seals BOTH edges.
                if (step.Message is UrlChanged && mostRecentNavigation is not null)
                {
                    // Compared against the dispatched INSTANCE. Url carries an
                    // IReadOnlyDictionary, whose record equality is reference equality, so a
                    // freshly constructed `new UrlChanged(sameUrlValue)` would not compare
                    // equal and the assertion would be flaky for a reason that has nothing to
                    // do with the mechanism.
                    if (runtime.Model.Past.Count > 0)
                    {
                        pastEdgeChecked = true;
                        await Assert.That(History.Backward(runtime.Model))
                            .IsEqualTo(new MovementAvailability.BlockedByWorld(mostRecentNavigation))
                            .Because($"seed {seed}: the past edge is sealed by the navigation");
                    }

                    if (runtime.Model.Future.Count > 0)
                    {
                        futureEdgeChecked = true;
                        await Assert.That(History.Forward(runtime.Model))
                            .IsEqualTo(new MovementAvailability.BlockedByWorld(mostRecentNavigation))
                            .Because($"seed {seed}: the future edge is sealed by the same navigation");
                    }
                }
            }
        }

        // The corpus is not sufficient until BOTH branches have been taken. Without these two
        // lines "both edges" is a sentence in a comment rather than a checked claim, and the
        // second half of the property's named falsifier — *seal only the past edge* — could not
        // turn it red on a corpus that never populated `Future`.
        await Assert.That(pastEdgeChecked).IsTrue()
            .Because("no seed navigated with a non-empty Past, so the past-edge seal was never " +
                     "asserted");
        await Assert.That(futureEdgeChecked).IsTrue()
            .Because("no seed navigated with a non-empty Future, so the future-edge seal was " +
                     "never asserted and only half of \"both edges\" has been checked");
    }
```

> **The two `Count > 0` guards are guards, not softenings.** `Clear` is in the alphabet and empties
> both stacks, so a navigation can legitimately arrive with nothing to seal; asserting a refusal
> there would be asserting against `Nothing`, which is INV-4's answer and correct. The guards are
> also why the corpus is not sufficient on its own: **the property is not done until both branches
> have been observed taken at least once** — a run in which `Future` is always empty has checked
> only half of "both edges", and the falsifier below is chosen so that a one-sided seal goes red.
> **Amendment 4 made that mechanical** (🔴-3(c)): it was prose and nothing counted the branches,
> so the corpus could have satisfied the sentence's opposite in silence.

**What this property covers, said plainly: the framework's message type, and nothing else (B10).**
The script dispatches `Picea.Abies.UrlChanged`, and the seal keys on that type. On the WebAssembly
head there is **no framework-owned delivery of an incoming URL change**: the browser's popstate
reaches `NavigationCallbacks.HandleUrlChanged` and then `OnUrlChange`, which is assigned in exactly
one place — `Picea.Abies/Navigation.cs:17-29`, inside `Navigation.UrlChanges(Func<Url, Message>
toMessage)`, where **the application supplies the message constructor**. `Conduit.cs:198` and every
tutorial pass `url => new UrlChanged(url)` and are covered. `README.md:190` passes an
application-defined `UrlChangedTo`, and for that shape the rule never fires: the navigation is
enveloped, moves the projection, is silent, and takes the `record` row exactly as before the
amendment — while **this property stays green**, because the fixture dispatches the framework type.
The server head is unaffected (`Session.cs:271` dispatches `new UrlChanged(url)` itself), so the hole
is precisely the head where browser Back matters most.

So the claim is scoped rather than repaired here: **the location guarantee holds for navigation
delivered as `Picea.Abies.UrlChanged`, and routing incoming navigation to that message is the
application's obligation** — obligation **17**, stated as an instruction in the table below, in the
same register as spec line 2's independent-reachability assumption. The framework cannot see an
application's own message type, so this is an assumption a property cannot carry, not a property
this file declined to write. Fixing `README.md` (Critic mitigation 2, `tech-writer`, plan step 13)
and an analyzer rule as a fast-follow (mitigation 3) are both outside this file.

**One further case neither this property nor A10 sees (🟡 5).** A program that leaves `UrlChanged` to
its `_ =>` fall-through returns a model-identical result, and `apply`'s true-no-op branch precedes
the seal rule, so the browser moves and nothing seals. The fixture cannot reach it — `Transition` has
an explicit `UrlChanged` case and `UserActionsAndNavigation` draws a fresh, differing `Url` each
time, so the model always moves. Benign for such a program, which has no location in its model to
restore; but whether `00-scope.md` INV-2's *"on any head where the application has one"* already
covers it, or the rule moves ahead of the no-op branch, is the architect's and the realist's to
settle. This file only records that no property here would go red on it.

```csharp
    // ── INV-3 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-3")]
    public async Task INV_3_redo_after_undo_restores_the_model_the_document_and_all_subsequent_behaviour()
    {
        var reached = 0;                                        // 🔴-3's floor; see INV-1

        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);     // S27: no deliveries here
            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script) await runtime.Dispatch(step.Message);

            if (History.Backward(runtime.Model) is not MovementAvailability.Available) continue;

            var s = runtime.Model;
            var documentAtS = runtime.CurrentDocument!;

            await runtime.Dispatch(new HistoryMessage.Undo());
            if (History.Forward(runtime.Model) is not MovementAvailability.Available) continue;
            await runtime.Dispatch(new HistoryMessage.Redo());
            reached++;

            var roundTripped = runtime.Model;

            // (i) the same model value
            await Assert.That(roundTripped.Present).IsEqualTo(s.Present).Because($"seed {seed}");

            // (ii) a document equal UP TO RENAMING OF EVENT-HANDLER COMMAND IDS
            await Assert.That(DocumentComparer.EqualUpToHandlerIds(runtime.CurrentDocument!, documentAtS))
                .IsTrue().Because($"seed {seed}");

            // (iii) the same subsequent behaviour: every message in the alphabet yields the
            //       same (model, command) pair from s' as it would have from s.
            foreach (var m in EditorMessages.All)
            {
                var (fromS, cmdFromS)   = Editor.Transition(s, m);
                var (fromS2, cmdFromS2) = Editor.Transition(roundTripped, m);
                await Assert.That(fromS2.Present).IsEqualTo(fromS.Present).Because($"seed {seed}, {m}");
                await Assert.That(CommandShape.Of(cmdFromS2)).IsEqualTo(CommandShape.Of(cmdFromS))
                    .Because($"seed {seed}, {m}");
            }
        }

        await Assert.That(reached > 0).IsTrue()
            .Because($"no seed in the corpus reached an undo-then-redo round trip, so this " +
                     $"property is green having asserted nothing (0 of {Gen.Corpus.Length} seeds)");
    }
```

> Document-owned state — caret, focus, scroll offset, `<details>` open state, uncontrolled input
> values — is **outside INV-3 and outside this pass**, per `00-scope.md` INV-3's clause *"State
> owned by the document rather than by the model … is outside this invariant and outside this
> pass"* (cited by id and clause rather than by line range — 🟡 3).
> `DocumentComparer.EqualUpToHandlerIds` normalises every handler command id to its ordinal
> position in a pre-order walk before comparing; that normalisation is the whole content of "up
> to renaming", and getting it wrong in the permissive direction would make (ii) vacuous, so it
> carries its own falsifier below.

```csharp
    // ── INV-4 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-4")]
    public async Task INV_4_a_movement_at_either_end_of_the_history_faults_nothing_changes_nothing_and_emits_nothing()
    {
        var reached = 0;                                        // 🔴-3's floor; see INV-1

        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);
            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script) await runtime.Dispatch(step.Message);

            foreach (var (direction, message, availability) in Both(runtime.Model))
            {
                if (availability is not MovementAvailability.Nothing) continue;

                var before = runtime.Model;
                EditorLog.Clear();

                await runtime.Dispatch(message);               // must not throw
                reached++;

                await Assert.That(runtime.Model).IsEqualTo(before).Because($"seed {seed}, {direction}");
                await Assert.That(EditorLog.MessagesApplied).IsEmpty().Because($"seed {seed}, {direction}");
                await Assert.That(EditorLog.PatchesApplied).IsEmpty().Because($"seed {seed}, {direction}");
                await Assert.That(EditorLog.CommandsInterpreted).IsEmpty().Because($"seed {seed}, {direction}");
            }
        }

        await Assert.That(reached > 0).IsTrue()
            .Because($"no seed in the corpus ended at either end of the history, so this " +
                     $"property is green having asserted nothing (0 of {Gen.Corpus.Length} seeds)");
    }

    // ── INV-5 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-5")]
    public async Task INV_5_a_declined_movement_delivers_a_typed_reason_the_application_can_tell_from_a_no_op()
    {
        var reached = 0;                                        // 🔴-3's floor; see INV-1

        foreach (var seed in Gen.Corpus)
        {
            // S27: THIS property, and INV-7's, are the only ones whose alphabet contains a
            // source that delivers without being asked. It is the property that can see a
            // tick destroy a redo.
            var script = Gen.UserActionsAndDeliveries(seed, length: 24);
            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script) await Drive(runtime, step);   // S28 ordering inside

            // ⚠️-5(f), amendment 4 — a FIXTURE OBLIGATION, stated here because this loop is
            // the only one in the file that snapshots `Both(...)` once and then dispatches
            // inside it. INV-6's loop re-reads `runtime.Model` per iteration and is immune; the
            // inconsistency between the two is real and is inside the lock, so it is named
            // rather than left to be discovered. Under plan rule S7
            // (`04-realist-plan.md:1120` — `if e is MovementRefused -> pass(h, next, cmd,
            // origin)`) a refusal records no step and supersedes nothing, so the RECORDING path
            // cannot stale this snapshot. But `pass` still calls
            // `sealTops(SealedByEffect(redact(origin)))` when the command is not silent. The
            // obligation is therefore exact and checkable: **`EditorProgram.Transition` must
            // return `Commands.None` for `MovementRefused`** — which the fixture's `_ =>`
            // fall-through already does, and which must not be changed. If a later fixture
            // gives `MovementRefused` a command, the first iteration's refusal seals both edges
            // and the second iteration asserts against a stale `availability`.
            foreach (var (direction, message, availability) in Both(runtime.Model))
            {
                if (availability is MovementAvailability.Available or MovementAvailability.Nothing)
                    continue;

                EditorLog.Clear();
                await runtime.Dispatch(message);               // must not throw
                reached++;

                // A VALUE, in the application's own vocabulary, along the SAME path that
                // carries every other message into the application.
                var refusals = EditorLog.MessagesApplied.OfType<MovementRefused>().ToArray();
                await Assert.That(refusals).Count().IsEqualTo(1).Because($"seed {seed}, {direction}");
                await Assert.That(refusals[0].Direction).IsEqualTo(direction).Because($"seed {seed}");

                // It IDENTIFIES WHICH REASON APPLIED, and names the message responsible.
                await Assert.That(refusals[0].Reason).IsEqualTo(availability).Because($"seed {seed}");
                await Assert.That(refusals[0].Reason).IsNotEqualTo(new MovementAvailability.Nothing())
                    .Because($"seed {seed}: a refusal must be distinguishable from INV-4's no-op");
            }
        }

        await Assert.That(reached > 0).IsTrue()
            .Because($"no seed in the corpus produced a declined movement, so this property is " +
                     $"green having asserted nothing (0 of {Gen.Corpus.Length} seeds)");
    }

    // ── INV-6 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-6")]
    public async Task INV_6_availability_and_its_reason_are_computable_from_the_history_value_alone_and_agree_with_the_attempt()
    {
        // 🔴-3(a), amendment 4 — the coverage assertion the note below this property has always
        // claimed: *"the property asserts that coverage at the end of the loop rather than
        // assuming the generator found them."* It did not exist; the body ended at the closing
        // brace of the seed loop, nothing counted the five cases, nothing counted directions,
        // and nothing failed if the corpus only ever produced `Nothing`. It matters more here
        // than anywhere else in this file, because § The Lock names *"every property carries a
        // named falsifier"* as one of the two things closing the support-file hole — and for
        // INV-6 the named falsifier **is** this coverage assertion. The Lock's own stated
        // closure was unbuilt for the property whose comment claimed it loudest.
        var observed = new HashSet<(Direction Direction, string Case)>();

        // Amendment 4 also changes this property's INTERPRETER, and the coverage assertion is
        // why: `BlockedByWorld` is caused by a command's FEEDBACK, and under `NoFeedback` no
        // feedback ever arrives — so that case was unreachable in both directions and the
        // ten-combination claim could not have been satisfied by any seed. The ALPHABET is
        // unchanged (still `Gen.UserActions`, so S27's partition table is untouched); what
        // changes is that `LoadRequested`'s `LoadCommand` now answers, and `Drive` carries the
        // S28 ordering for that feedback exactly as it does for a delivery. Writing the missing
        // assertion is what exposed the unreachable case — which is the argument for writing it.
        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);
            using var runtime = await Start<Editor>(LoadReturns("server"));
            foreach (var step in script) await Drive(runtime, step);   // S28 ordering inside

            foreach (var (direction, message, _) in Both(runtime.Model))
            {
                var h = runtime.Model;
                EditorLog.Clear();

                // Computed IN ADVANCE, from the value BY ITSELF, WITHOUT causing any effect:
                var answer = direction is Direction.Backward ? History.Backward(h) : History.Forward(h);
                observed.Add((direction, answer.GetType().Name));

                await Assert.That(runtime.Model).IsEqualTo(h).Because($"seed {seed}: answering is pure");
                await Assert.That(EditorLog.MessagesApplied).IsEmpty().Because($"seed {seed}");
                await Assert.That(EditorLog.PatchesApplied).IsEmpty().Because($"seed {seed}");
                await Assert.That(EditorLog.CommandsInterpreted).IsEmpty().Because($"seed {seed}");

                // …and it AGREES with what actually happens on that same value:
                await runtime.Dispatch(message);

                switch (answer)
                {
                    case MovementAvailability.Nothing:
                        await Assert.That(runtime.Model).IsEqualTo(h).Because($"seed {seed}, {direction}");
                        await Assert.That(EditorLog.MessagesApplied).IsEmpty();
                        break;
                    case MovementAvailability.Available:
                    {
                        // 🔴-1, amendment 4. This assertion was written as:
                        //
                        //     await Assert.That(runtime.Model.Present).IsNotEqualTo(h.Present)
                        //         .Or.That(CursorMoved(h, runtime.Model)).IsTrue()...
                        //
                        // which does not compile on the pinned TUnit 1.19.57 —
                        // `error CS1061: 'OrContinuation<int>' does not contain a definition
                        // for 'That'`. TUnit's `.Or` continues the chain on the SAME subject, so
                        // a disjunction across two DIFFERENT values is not a wrong overload but
                        // a wrong shape: no version of it would have compiled after step 6
                        // delivered every missing type, and the file header's *"does not compile
                        // until plan step 6"* would have stayed false. The CLAIM is unchanged —
                        // an `Available` movement must actually move, either because the model
                        // value changed or because the cursor moved between two models that
                        // happen to be equal by value. Computed as one boolean, asserted once.
                        var moved = runtime.Model.Present != h.Present || CursorMoved(h, runtime.Model);
                        await Assert.That(moved).IsTrue()
                            .Because($"seed {seed}, {direction}: `Available` promised a movement " +
                                     $"and neither the model nor the cursor moved");
                        await Assert.That(EditorLog.MessagesApplied.OfType<MovementRefused>()).IsEmpty()
                            .Because($"seed {seed}, {direction}");
                        break;
                    }
                    default:   // BlockedByEffect | BlockedByWorld | BlockedBySupersedingAction
                        await Assert.That(EditorLog.MessagesApplied.OfType<MovementRefused>().Single().Reason)
                            .IsEqualTo(answer).Because($"seed {seed}, {direction}");
                        break;
                }
            }
        }

        // The corpus is not complete until all TEN (case × direction) combinations have been
        // seen. Asserted, not assumed — this is the property's named falsifier and the Lock's
        // stated closure for it.
        foreach (var direction in new[] { Direction.Backward, Direction.Forward })
            foreach (var name in new[]
                     {
                         nameof(MovementAvailability.Nothing),
                         nameof(MovementAvailability.Available),
                         nameof(MovementAvailability.BlockedByEffect),
                         nameof(MovementAvailability.BlockedByWorld),
                         nameof(MovementAvailability.BlockedBySupersedingAction)
                     })
                await Assert.That(observed.Contains((direction, name))).IsTrue()
                    .Because($"the corpus never produced {name} in the {direction} direction, so " +
                             $"this property is green having checked only part of its claim");
    }
```

> INV-6 is checked in **both directions** and must be exercised over all **five** cases —
> `Nothing`, `Available`, `BlockedByEffect`, `BlockedByWorld`, `BlockedBySupersedingAction`. The
> corpus is not complete until every one of the ten (case × direction) combinations has been
> observed at least once; the property asserts that coverage at the end of the loop rather than
> assuming the generator found them.
>
> **Amendment 4 made that last clause true.** It was prose and nothing else (🔴-3(a)) — and
> writing the assertion immediately exposed that `BlockedByWorld` was *unreachable in either
> direction* under `NoFeedback`, so the ten-combination claim could never have been met. That is
> why this property now drives `LoadReturns("server")` through `Drive`: an unimplemented
> coverage claim had been concealing an unreachable case.
>
> **One honest caveat for step 6, from round 2's review (amendment 5).** That fix was reached by
> *reasoning*, not execution — I have no `Bash` and the file does not compile — so it is not
> settled that all ten combinations are reachable; `BlockedBySupersedingAction` on the **backward**
> edge is the one nobody has been able to check. If the coverage assertion goes red at step 6,
> **the first hypothesis is an unreachable combination, not a bug in `WithHistory`**, and the route
> is a `// SPEC CONFLICT:` hand-back naming the combination. That is the cost of converting
> ⚠️-5(e) from *hoped for* into *fails loudly*, and it is the right trade — but it should not be
> misdiagnosed as a feature defect.

```csharp
    // ── INV-7 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-7")]
    public async Task INV_7_no_subscription_starts_stops_or_delivers_on_account_of_a_state_only_passed_through()
    {
        foreach (var seed in Gen.Corpus.Concat(Gen.NamedShapeSeeds))
        {
            // Includes malformed brackets: Settle with no hold, Hold twice, a hold left open.
            var script = Gen.UserActionsAndDeliveries(seed, length: 32);
            using var runtime = await Start<Editor>(NoFeedback);

            foreach (var movement in Movements(script))     // maximal movements: one un-bracketed
            {                                               // press, or one Hold…Settle bracket
                var before = runtime.Model;
                EditorLog.Clear();
                foreach (var step in movement) await Drive(runtime, step);

                // (1) start/stop activity equals ONE reconciliation from the pre-movement set
                //     to the SETTLED set — INV-7's "equivalently" falsifier, cited by id and
                //     clause rather than by line range (🟡 3).
                await Assert.That(EditorLog.SubscriptionActivity).IsEquivalentTo(
                        SingleReconciliation(EditorProgram.Subscriptions(before.Present),
                                             EditorProgram.Subscriptions(runtime.Model.Present)))
                    .Because($"seed {seed}");

                // (2) every DELIVERY inside a bracket carries a key from the ANCHOR's key set.
                if (movement.IsBracketed)
                {
                    var anchorKeys = Keys(EditorProgram.Subscriptions(before.Present));
                    await Assert.That(EditorLog.DeliveredKeys.All(anchorKeys.Contains)).IsTrue()
                        .Because($"seed {seed}: a source declared only by a transit state is " +
                                 $"never started, therefore never delivers");

                    // (4) while a hold is open the reported key set is CONSTANT and equal to
                    //     the anchor's, no matter how far Present moved.
                    await Assert.That(EditorLog.ReportedKeySetsDuringHold.Distinct())
                        .IsEquivalentTo([anchorKeys]).Because($"seed {seed}");
                }
            }

            // (3) the abies:history: prefix is reserved and the framework declares nothing.
            await Assert.That(EditorLog.AllReportedKeys.Any(k => k.StartsWith("abies:history:")))
                .IsFalse().Because($"seed {seed}");
        }
    }
```

### `INV-n` → property

| Invariant | Property test | What it asserts over the whole input space |
|---|---|---|
| **INV-1** | `INV_1_a_single_undo_returns_the_state_before_the_most_recent_action_whichever_region_it_came_from` | For every generated interleaving of user actions across two independent regions, quantified over histories where `Backward(h)` is `Available`, a single undo lands on the state immediately before the most recent action — whichever region it came from. One history for the application. |
| **INV-2** | `INV_2_every_state_reached_by_undo_or_redo_is_reachable_by_ordinary_actions_alone` | For every generated action/undo/redo/hold/settle sequence, under **both** the whole-model and the projection policy, every `h.Present` is a member of the set of models reachable by ordinary actions alone. |
| **INV-2** *(second)* | `INV_2_no_movement_lands_on_a_model_whose_url_is_not_the_one_the_browser_shows` | For every generated sequence containing **incoming browser navigation after the first recorded step, delivered as `Picea.Abies.UrlChanged`**, under the whole-model policy: after every step — and so after every undo and every redo — `h.Present.Route` is the URL the browser is showing. The browser's location is part of the world, so a `(model, location)` pair ordinary actions could never have produced is an unreachable state. Where a movement would have to cross a navigation to reach one, it refuses in **both** directions with `BlockedByWorld` naming that navigation, answerable from the value before the press. **Quantified over the framework's message type only** — an application that routes incoming navigation through a message of its own is obligation 17's case, not this property's (B10). |
| **INV-3** | `INV_3_redo_after_undo_restores_the_model_the_document_and_all_subsequent_behaviour` | For every generated history where undo then redo are both available, the round trip is indistinguishable in all three senses `00-scope.md` names: same model value, same document up to handler-id renaming, and the same `(model, command)` pair for **every** message in the alphabet afterwards. |
| **INV-4** | `INV_4_a_movement_at_either_end_of_the_history_faults_nothing_changes_nothing_and_emits_nothing` | For every generated history value at either end, in either direction: no exception, no state change, no patch, no command, and nothing delivered to the application. |
| **INV-5** | `INV_5_a_declined_movement_delivers_a_typed_reason_the_application_can_tell_from_a_no_op` | For every generated history value — **including deliveries from an autonomous source** — in which a movement is declined for any reason other than there being nothing left, the application receives exactly one `MovementRefused`, along the ordinary message path, carrying the direction and the reason that identifies which of the three applied and names the message responsible; and that value is never `Nothing`, so it is distinguishable from INV-4's no-op. |
| **INV-6** | `INV_6_availability_and_its_reason_are_computable_from_the_history_value_alone_and_agree_with_the_attempt` | For every generated history value and both directions: the answer is computable from the value alone before the attempt, causes no effect while being computed, and agrees with what the attempt on that same value then does — across all five cases in both directions. |
| **INV-7** | `INV_7_no_subscription_starts_stops_or_delivers_on_account_of_a_state_only_passed_through` | For every generated sequence including brackets, malformed brackets and autonomous deliveries: per maximal movement, start/stop activity equals a single reconciliation from the pre-movement set to the settled set; every delivery inside a bracket carries an anchor key; the reported key set is constant while a hold is open; and no application-declared key begins with `abies:history:`. |

**Invariants covered:** INV-1, INV-2, INV-3, INV-4, INV-5, INV-6, INV-7 — every id declared in
`00-scope.md`.

**Invariants I could not express as a property:** **none.** All seven are quantified claims and all
seven have a falsifier `00-scope.md` already states. Two are worth flagging as *narrower than they
read*, honestly rather than as a defect:

- **INV-7 is true and empty for the chrome the guide will show.** An un-bracketed press is a
  complete movement with no states passed through, so the invariant is satisfied *by definition*
  for it. The property's non-trivial content lives entirely in the bracket cases, which is why the
  fixture's `Subscriptions` is non-monotonic in `Zoom` and why the corpus **seeds explicitly** a
  transit state declaring a source neither anchor nor destination declares. Without that seeding
  the property is green and vacuous, and a random generator over a small model may never find the
  shape. This is spec line 8, and it is a consequence of the user's decision to define a movement
  by input rather than by a timer — not a weakness in the property.
- **INV-2's residual is not framework-testable.** Under an application-supplied lens the invariant
  holds up to L1–L6 *plus* the independent-reachability assumption, which is a statement about the
  application's own reachable set. The property tests the framework's fixture, where the assumption
  holds by construction. Spec line 2 carries the assumption as an assumption.
- **INV-2's location clause has a second residual of exactly the same kind (B10, added 2026-09-07).**
  The clause is quantified over navigation delivered as `Picea.Abies.UrlChanged`. Whether an
  application's incoming navigation arrives as that message is decided at
  `Picea.Abies/Navigation.cs:17-29` by the application, not by the framework, so it is an assumption
  about the adopter's own wiring and no property in this file can carry it — the same shape as
  independent reachability, and it is written down for the same reason. Spec line **17** carries it
  as an obligation.

---

## Falsifiers — how each of these goes red

A property that has never been observed failing is a property nobody has checked. Each carries a
named mutation, and **step 8 / step 9 are not done until each has been observed to fail under it**:

| Property | Mutation that must turn it red |
|---|---|
| INV-1 | let one dispatch record two steps (a later event recording its own step) |
| INV-2 | make `StepBack` compute `Restore(present, remembered)` — arguments swapped. And **separately, for the projection walk amendment 4 added**: make `EditorProjectionPolicy.Restore` also carry `remembered.Ticks + 1`, a pairing no ordinary action ever produced — the projection walk must go red where the whole-model walk stays green, which is what shows the second lens is doing work rather than decorating a comment |
| INV-2 *(second)* | **delete the `origin is UrlChanged && Origin is Established -> seal` rule** so a navigation `record`s like any other action: the property must then go red on the first undo that crosses one, with `Present.Route` holding the pre-navigation URL while `browserShows` holds the new one. And **separately, seal only the past edge** — the `History.Forward` assertion must then go red, which is what makes *"both edges"* a checked claim rather than a sentence. **Note what these falsifiers do not reach:** they delete the *rule*, not the *idiom*, so the other route to the same defect — an application-named navigation message, which never reaches the rule at all — has no falsifier here and is out of the claim by obligation 17 (B10). **And separately, feed a corpus in which a navigation never arrives with a non-empty `Future`** — `futureEdgeChecked` must go red, which is what turns *"both edges"* from a note into a checked claim (amendment 4, 🔴-3(c)) |
| A10 | the same first mutation. A10 is the acceptance-layer twin of the property's first falsifier: delete the seal rule and `History.Backward` returns `Available` where A10 demands `BlockedByWorld(back)`. It is not a second line of defence and is not claimed as one — it exists for recognition (🟡 7) |
| INV-3 | make `DocumentComparer` compare raw HTML, or make `StepBack` carry the popped step's edge forward instead of pushing `Crossable` |
| INV-4 | return `Ok([Undo])` instead of `Ok([])` when `Backward(h)` is `Nothing` |
| INV-5 | make `record` **clear** `Future` instead of superseding it — the property must then fail with `Nothing` where it expected `BlockedBySupersedingAction` (plan step 8(k)); and separately, revert `Decide`'s `Err` branch to `Err(e)` — the property must fail with `BlockedByWorld` (plan step 8(j)) |
| INV-6 | make `Available` carry the crossable count instead of `Depth`, or report the cause of the message that *created* the step instead of the one that sealed it. **And separately, restrict `Gen.Corpus` so one (case × direction) combination is never produced** — the coverage assertion amendment 4 added must go red. That is the one falsifier in this table that is also the Lock's *stated closure mechanism*, so until it has been observed the closure is a claim rather than a fact |
| INV-7 | **remove the anchor** — report `Subscriptions(h.Present)` during a hold. Assertions (1), (2) and (4) must all break |

**How this spec first fails, honestly.** `WithHistory`, `History<TModel>` and
`MovementAvailability` do not exist yet, so on the day it is committed this file does not compile.
That failure names exactly what is missing, which is the right signal, but a compile error is a
weaker one than a red assertion. The obligation on `csharp-dev` is therefore explicit: **at the
point the spec first compiles — the end of plan step 6 — every test in it must be observed red for
the right reason before any of them is made green**, and each falsifier above must be observed
before its step is closed. I have no `Bash` and cannot run any of this; that confirmation is the
owner's first implementation act, not mine.

**And that sentence was not enough, which is amendment 4's lesson.** PR 0's review compiled this
file's assertion *shapes* against the pinned **TUnit 1.19.57** in a scratch project — the one
check neither the missing types nor the missing fixtures prevented — and found two defects that
"observe it red at the end of step 6" would not have caught, because one of them never compiles at
all and the other passes for the wrong reason forever. `reviewer-blind` put it exactly right:
*"A file that has never been compiled is being frozen."* The two the reviewer proved are fixed
above. **The honest residual: the rest of this file's assertion shapes are still unverified by me**
— I have no `Bash`, and the verification that exists is the reviewer's probes, not a build of this
file. `csharp-dev`'s first act at step 6 is still to compile it and observe every test red for the
right reason — but that is now the *second* line of defence, not the first.

**And amendment 5 sharpened the rule, because amendment 4's version of it was not enough.**
Amendment 4 said: *where there is a choice of shape, take the one the reviewer has already
executed.* Round 2 established that the datum it took — *"`IsEqualTo` on a collection is
order-sensitive"* — was **wrong in both directions**: the reviewer had probed only the negative
case, and a permutation failing is equally consistent with *order-sensitive* and with *never
satisfiable*. It was the latter. The rule that survives is therefore stricter: **a shape is
verified only when someone has run a passing control AND a failing control.** One probe that
fails proves nothing about a shape; it proves something about that input. Every assertion shape
this file now relies on has both controls behind it, and where the shape matters the controls are
recorded beside the assertion rather than in a review nobody reads at step 6.

---

## Spec lines carried from the plan and the Critic

`04-realist-plan.md` § *Spec obligations* lists thirteen; line 4 is **amended** per S25(b), line
**14** is **added** per S26, **15** per S25(a), **16** by the navigation amendment and **17** by
B10's scoping. This table is the union and is the one the lock, `csharp-dev` and
`reviewer-reconcile` read (`05-critic.md` S31); the plan's numbering follows it, not the other way
round. These are statements the spec carries as behaviour, each already executable above or named
as not-captured below — **except line 17, which is an obligation on the adopter rather than a
behaviour of the framework, and is therefore stated rather than asserted.**

| # | Spec line | Where it is asserted |
|---|---|---|
| 1 | One property per `INV-1` … `INV-7`. | the invariant layer |
| 2 | L1–L6 are the **application's** obligation, with independent reachability named as an assumption the framework cannot test. **Line 17 is a second obligation of the same kind and sits beside this one.** | stated at INV-2; the properties themselves are plan step 8(e)(f) |
| 3 | *"The world" is a command's feedback **and one exception this same table carries: an incoming `Picea.Abies.UrlChanged` once `Origin` is `Established` (line 16)**.* Otherwise: a subscription-delivered message is a user action; a rejected decision is the application's own response and classifies like an accepted one. **The exception has to be stated here rather than only at line 16, because on the browser head an incoming `UrlChanged` *is* a subscription delivery** — it arrives through `Navigation.UrlChanges`, i.e. `DispatchFromSubscription` — so *"a subscription-delivered message is a user action"* and *"incoming navigation is the world"* would otherwise classify the same message on the same path two different ways. (S30, accepted 2026-09-07.) | A5, A8; INV-5's alphabet — and **A10 / INV-2's second property** for the exception |
| 4 | An application with a model-mutating subscription **must** declare a projection or redo will not function — **and, under the whole-model policy, undo's reach also expires, at `Depth` ÷ the subscription's rate; in `SubscriptionsDemo`'s shape that is twenty-five seconds** (S25(b)). | A8, all three tests |
| 5 | The whole-model policy's **reach** has **three** bounds, all at the same weight: (i) the last command feedback (S10); (ii) `Depth` ÷ the subscription's rate, in wall-clock seconds, where a model-mutating subscription runs (S25(b), line 4); and (iii) **the last incoming navigation** (line 16) — which in a *routed* application will usually be the binding one: in Conduit's shape, undo reaches back to the current page and no further. (S30's second half, accepted 2026-09-07; the third bound was missing here and is a consequence of the navigation amendment, not a new decision.) | A2 for (i); A8's third test for (ii); **A10 / INV-2's second property** for (iii) |
| 6 | A wrapped multi-event decision **interleaves effects differently**: all events applied, then batched commands interpreted in order. | A6, first test |
| 7 | And **fails differently**: a failing command skips later *commands*, not later *events* — *"complete model, partial effects"* where an unwrapped program gives *"partial model, no later effects"*. | A6, second test |
| 8 | For an **un-bracketed press** INV-7 is satisfied *by definition*, and no button chrome can bracket a run, so the shown chrome gets per-press reconciliation. | A7, second test |
| 9 | **While a hold is open the application's live subscriptions do not track its model at all** — the framework's central subscription contract is *suspended*, for as long as the chrome likes. | INV-7 assertion (4); A7, first test |
| 10 | Navigation before the session's first recorded step never becomes an undo stop, on any head. | **not captured** — see below |
| 11 | `Clear` makes previously-available undo unavailable without moving the cursor, and **also settles an open bracket**. | **not captured** — see below |
| 12 | **Undo remains dispatchable out of a terminal state** and can resurrect a terminated program. | **A9** — moved into the locked file by the user's amendment of 2026-09-07 |
| 13 | Under **InteractiveAuto** the history begins at the client handoff. | **not captured** — see below |
| **14** | **Acting after an undo does not discard the forward branch.** The branch is preserved and its first edge sealed with `SupersededByNewAction(cause)`; `Forward(h)` returns `BlockedBySupersedingAction(cause)` before the press, naming the message that superseded it. The branch is never crossed, and `Future` is trimmed to `Depth` at its far end. (S26 — the answer to `00-scope.md` open question 3.) | A3, A8; INV-5 |
| **15** | A message arriving **during a bracket** does not end the run: it is applied to `Present`, and it may seal the history **or record a new undo stop whose model is a transit state and supersede the forward branch, mid-movement** — all three typed. (S25(a).) | A7, third test |
| **16** | **Incoming navigation is the world.** A `Picea.Abies.UrlChanged` the application did not ask for, arriving once `Origin` is `Established`, **seals both edges** with `SealedByWorld(UrlChanged)`; `Backward(h)` and `Forward(h)` both refuse with the navigation as cause. The consequence is the point: **the application's URL never diverges from the one the browser is showing**, and the browser's own back/forward stack never diverges from the application's history — **for an application whose incoming navigation arrives as that message; see line 17 for the one that does not**. Before the first recorded step the rule does not apply — navigation re-bases instead, which is spec line 10. (The user's amendment of 2026-09-07; `00-scope.md`'s INV-2 gains the browser's location as part of the world, and `04-realist-plan.md` § *Spec obligations* gains this line **as its own line 16** — both are the architect's and the realist's to land, not this file's. **This number does not move:** `05-critic.md` S31 settles it at 16, the spec's table is the union the lock and the review read, and the plan's row renumbers to match.) | **INV-2's second property**, for the framework message type only; **A10** in the acceptance layer |
| **17** | **An application that routes the browser's location through its own message type must classify it as a commitment itself; the framework cannot see it.** Incoming navigation reaches a WebAssembly program through an application-supplied converter (`Picea.Abies/Navigation.cs:17-29`), so line 16's seal fires only for `Picea.Abies.UrlChanged`. An adopter that writes `Navigation.UrlChanges(url => new MyOwnNavigationMessage(url))` gets no seal, its navigations become ordinary undo stops, and undo will restore a model whose URL the browser is not showing — with every property in this file still green. **Pass `url => new UrlChanged(url)`**, as `Conduit.cs:198` and every tutorial do, or accept that line 16's guarantee does not hold for you. This is an obligation on the adopter of the same kind as line 2's independent-reachability assumption, and for the same reason: it is a statement about the application's own wiring, which no framework property can quantify over. (`05-critic.md` B10, mitigation 1, accepted by the user 2026-09-07. `README.md:190` is the repository's own counter-example and is `tech-writer`'s to fix at plan step 13 — mitigation 2. An analyzer rule — mitigation 3 — is a fast-follow candidate and is **not** in this pass.) | **not asserted, and not assertable here** — stated at INV-2's second property and in *Behaviour NOT captured* |

---

## What This Test Proves

**Behaviour captured.** If every test in `UndoRedoSpec.cs` passes:

- An application composed with `WithHistory` has **one** undo history, and one dispatch is one
  undoable step, across independent regions of its interface.
- Undo and redo are inverse, land only on reachable states, are no-ops at the ends, and never fault.
- Every decline is **typed**, arrives as a value along the ordinary message path, names the message
  responsible, is answerable from the history value **before** the press, and is distinguishable
  from *"there was nothing to undo"*. There is no silent path left in the design.
- Undo refuses to cross an action that spoke to the world; acting after an undo preserves and seals
  the forward branch rather than destroying it.
- **The world includes the browser's own location — for an application whose incoming navigation
  arrives as `Picea.Abies.UrlChanged`.** Such an application never shows a URL the browser is not
  showing: navigation it did not ask for seals the history in both directions rather than becoming
  an undoable step, so the browser's back stack and the application's history cannot drift apart.
  **This is the one claim in the list with a stated precondition on the adopter** (obligation 17):
  an application that routes the location through a message of its own gets no seal and no
  guarantee, and nothing in this file goes red for it.
- A rejected decision is the application's own response, not the world.
- A bracketed run reconciles subscriptions exactly once, against the state it settles on, and
  nothing declared only by a state passed through starts, stops or delivers.
- The two behavioural differences between a wrapped and an unwrapped program are recorded **as
  differences**, against the unwrapped program's actual behaviour.
- An adopter is told, by mechanism rather than by documentation, that a model-mutating subscription
  costs them both redo and undo reach unless they declare a projection.

**Behaviour NOT captured**, and each of these has a home:

- **Spec lines 10, 11 and 13** — pre-record navigation re-basing, `Clear`'s two effects, and the
  InteractiveAuto handoff. All three are real behaviour and all three are stated here; they are
  tested by plan steps 6 and 10 rather than by this file because each needs a fixture shape (a
  head with no initial URL, an Auto handoff) that would enlarge the fixture surface for one
  assertion each. **Line 12 was in this list and is not any more** — the user moved undo-out-of-a-
  terminal-state into the locked file on 2026-09-07 (A9). Note that A9's second test dispatches
  `Clear`, but asserts only on terminality: **spec line 11's own content — that `Clear` makes
  available undo unavailable without moving the cursor, and settles an open bracket — is still
  step 6's**, and A9 does not cover it. **Line 10 is likewise still step 6's after the navigation
  amendment**: INV-2's second property covers navigation only once `Origin` is `Established`, and
  its generator is forbidden from drawing one before the first record — so the *re-basing* half,
  a navigation before anything has been recorded never becoming an undo stop, is untouched by it.
  That seam is the one place a reader could believe more is locked than is, which is why it is
  named twice.
- **Spec line 17 — an application-named navigation message (B10).** Not captured, and **not
  capturable from inside the framework**: the message type is chosen by the adopter at
  `Navigation.cs:17-29`, so a property here can only ever drive the type the fixture chooses. This
  is the one entry in this list that is not deferred to a later step — there is no step that will
  cover it, by design. Its homes are documentation (plan steps 5(vi) and 13, and the guide) and a
  possible analyzer as a fast-follow. **A reader who takes line 16 without line 17 will believe
  more is guaranteed than is.** The same paragraph names 🟡 5's silent, model-identical
  `UrlChanged`, which no property here sees either.
- **The lens laws L1–L6**, including `Scrub_overridden_alone_violates_L5` — plan step 8(e)(f)(l),
  `HistoryLensLawTests.cs`. Properties of the algebra, not of the feature's observable behaviour.
- **The SEC-3 security regressions** and `Anchor_never_reaches_a_release_path_surface` — plan step
  8(g)(m), `HistorySecurityRegressionTests.cs`. Named for threats, not for scenarios, and they
  belong beside the threat model rather than beside the spec.
- **The B4 interleave** (a decided undo whose availability changed before transition) — plan step
  8(c). It needs a driver that dispatches two `Undo`s **without awaiting the first**, which is a
  concurrency probe rather than a specification of user-visible behaviour.
- **Composition and chrome rendering** — plan step 10. **Telemetry** — plan step 11.
  **Performance** — plan step 7; a green integer-MB size gate and a green benchmark run are not
  evidence for this design, and the report must say so.
- **Anything durable.** Undo does not survive the application going away and coming back, by
  `00-scope.md`'s explicit exclusion. Nothing here tests reload, restart or reconnection.
- **Document-owned state** — caret, focus, scroll, `<details>`, uncontrolled inputs. Outside INV-3
  and outside this pass.
- **S29** — the retention *lifetime* of a superseded `Step.Model`. Threat-model and DEBUG-row
  wording for `security-expert` at plan step 16. No mechanism, therefore nothing to specify.

---

## 🔒 The Lock

Once approved, **`Picea.Abies.Tests/History/UndoRedoSpec.cs` is immutable for this feature.**
Implementation must make it pass without modifying it.

- The spec's **approval commit and the commits that make it pass are separate commits**, and the
  spec lands **before plan step 1** (plan `:1304`). `reviewer-reconcile` runs a git-history check:
  a spec file modified in the same PR that brings it to passing is flagged.
- If implementation reveals the spec is wrong, `csharp-dev` **stops**, marks the conflict
  `// SPEC CONFLICT:` on the test, and hands back with options and a recommendation. `spec-author`
  produces an updated spec, the user re-approves, then implementation resumes.
- Forbidden: editing the spec to match what the code does; adding `[Skip]` to unblock other work;
  implementing it literally while knowing it produces wrong behaviour.
- **The support files are not locked, and that is a hole worth naming.** A weakened generator or a
  permissive `DocumentComparer` could make a locked property vacuous without touching the locked
  file. Two things close it: the generator contract above is part of the approved spec text, and
  every property carries a named falsifier that a weakened generator would fail to reproduce.
  `reviewer-blind` should read `Generators.cs` and `DocumentComparer` as carefully as the spec —
  including `UserActionsAndNavigation`'s two stated constraints, since a generator that draws every
  `UrlChanged` before the first record, or that reuses one `Url`, leaves INV-2's second property
  green and empty.

**The stated closure was incomplete, and here is the rest of it (⚠️-5, amendment 4).** PR 0's
review enumerated the obligations this locked file places on the *unlocked* support files, and
found that the generator contract plus the per-property falsifiers covered exactly one of them
(`DocumentComparer.EqualUpToHandlerIds`). The remainder are listed now, so that PR 3's author
inherits a list rather than a surprise, and so that `reviewer-blind` has something to check them
against:

| # | Obligation on the support files | Why the locked file cannot supply it |
|---|---|---|
| **(a)** | A **`global using static`** must bind the ~13 unqualified fixture calls — `Start`, `StartBare`, `NoFeedback`, `Both`, `Drive`, `Movements`, `SingleReconciliation`, `Keys`, `CursorMoved`, `ReachableByOrdinaryActionsAlone`, `LoadReturns`, `RecordingInterpreter`, `FirstCommandFails`. | `UndoRedoSpec` is `sealed`, not `partial`, and has no base type, so no other file can inject members into it. Nothing else can work. |
| **(b)** | `EditorLog` **should** be `AsyncLocal`-flowed. | No longer load-bearing: amendment 4 put `[NotInParallel("history-editor-log")]` on the class. Kept as a preference, not a requirement. |
| **(c)** | `EditorLog.Timeline` must return a **snapshot**, not the backing list. | A6 captures `wrappedOrder`, calls `Clear()`, then captures `bareOrder`. A live reference makes both variables alias the bare timeline and the wrapped assertion vacuous. |
| **(d)** | `Start<…>()` must **reset the static log**. | A1 asserts `EditorLog.CommandsInterpreted` is empty with no `Clear()` anywhere in the method — the only test in the file that relies on this. |
| **(e)** | `Gen.Corpus` must actually reach **all five availability cases in both directions**, and `Gen.UserActionsAndNavigation` must reach a navigation with a non-empty `Future`. | Was unstated and unchecked. **Amendment 4 makes both checkable from inside the lock** — INV-6's coverage assertion and INV-2's two branch flags fail loudly rather than passing quietly. |
| **(f)** | `EditorProgram.Transition` must return `Commands.None` for `MovementRefused`. | Stated at INV-5's loop. `pass` calls `sealTops(SealedByEffect(…))` on a non-silent command, which would stale that loop's once-taken `Both(…)` snapshot. |
| **(g)** | `Keys(…)` and `EditorLog.ReportedKeySetsDuringHold` must yield a **value-equality element type**. | ⚠️-9, added by amendment 5, and it is a *seventh* obligation the round-1 table missed. INV-7's assertion (4) compares `ReportedKeySetsDuringHold.Distinct()` against `[anchorKeys]` with `IsEquivalentTo`, which compares **elements** by `Equals`. Round 1 cleared this site on reasoning — *"order-insensitive by intent, a one-element distinct set"* — which was right about ordering and silent about element equality. Measured on 1.19.57: with `HashSet<string>` or `string[]` **elements** it fails on identical content; with a `record` element it passes on a match and fails on a mismatch, both controls run. So the locked assertion is correct and the obligation is the fixture's. **`SingleReconciliation(…)` in assertion (1) is safe if it returns a flat `IEnumerable<string>`** and has the same problem if it returns a set of sets. |

**On what the lock covers, said once so it is not argued at step 6 (💡, amendment 5).** Decision 1
makes the file immutable **in its assertions and its claims**. Three consequences follow, and the
third is the one that has already caused confusion:

1. **Whitespace is outside.** See the paragraph below.
2. **Signatures and attribute parameters are outside.** Adding a `CancellationToken` parameter to a
   `[Test]` — the remedy for `TUnit0015` noted beside `[Timeout]` — changes neither an assertion nor
   a claim, so step 6 may do it without a hand-back.
3. **Commentary `csharp-dev` adds to the transcription is inside from the approval commit, and was
   never approved here.** The review found two such blocks — the 18-line file header and a 15-line
   bridge introducing the invariant layer — and verified every claim in both. They are welcome and
   they are **`csharp-dev`'s to own**: the lock protects them from later silent edits, but this
   file's approval does not extend to them, and a false sentence in one is `csharp-dev`'s to correct
   rather than a `spec-author` amendment. That distinction is exactly what round 1's 🔴-1 header
   defect turned on.

**On byte-faithfulness (⚠️-8).** The lock is on the **assertions and their claims**, not on
whitespace. `.claude/hooks/dotnet-format-on-save.sh` fires on every `.cs` write with no exclusion
for a file the process declares immutable, and `.editorconfig`'s
`csharp_preserve_single_line_statements = false` will split this file's `if (…) continue;` lines
the moment it re-enters the compile set. That is **not** a `// SPEC CONFLICT:` and must not be
handed back as one. Any change that alters what is asserted, or what a comment claims, is. (The
hook's lack of an exclusion is `devops`', not this file's.)

Stated here explicitly so the implementing specialist cannot claim they did not know.

---

## 🛑 Approval Request — answered 2026-09-07, **re-opened by amendment 4 (see the end of this file)**

> **This test is the executable specification for undo/redo. If this test passes, do you consider
> the feature done? Anything missing? Anything wrong?**

**Approved by the user, with two amendments, both before the lock.** The level (in-process
`Runtime`, no AppHost), INV-7's stated narrowing, and leaving the support files outside the lock
with the generator contract and the per-property falsifiers as the closure were all accepted as
drafted. Lines 10, 11 and 13 stay in steps 6 and 10.

**Amendment 1 — A9.** Spec line 12 — *undo remains dispatchable out of a terminal state and can
resurrect a terminated program* — moved from plan step 6 into the locked file.

**Amendment 2 — INV-2's second property.** The user, still as approver and still before the
approval commit, found that **incoming navigation was not classified**: after the first record a
`UrlChanged` delivered by the browser's back or forward button was enveloped and *recorded*, so an
application undo restored a model without its URL and the browser's stack diverged from the
application's history. Their decision, and this file's share of it: **one property, with
`UrlChanged` in its alphabet after the first record, whose falsifier is an undo that lands on a
model whose URL is not the one the browser shows.** It is attached to INV-2 as a second property —
`validate-phase-artifact.sh` matches on the id and does not cap the count — rather than folded into
the first, because the first cannot see the bug: a stale `Route` restored by undo is a model the
application really was once in, so the reachable-set assertion stays green while the two stacks
diverge. The mechanism side is the plan's (`origin is UrlChanged && Origin is Established -> seal,
SealedByWorld(UrlChanged)`, both edges, `Backward`/`Forward` refusing with the navigation as cause)
and the scope side is the architect's (INV-2 gains the browser's location as part of the world).
Obligations table gains **line 16**. Fixture cost: one defaulted record field, one `Transition`
case, one generator symbol — no acceptance test, no policy, no recorder and no `View` change.

**Amendment 3 — B10's scoping, S30's two spec lines, and 🟡 3 and 🟡 7.** After `05-critic.md`'s confirmation pass of
2026-09-07 the user accepted the Critic's **mitigation (1)** for B10: scope the claim rather than
reach for mechanism. Their words, carried verbatim into the obligations table as **line 17**: *an
application that routes the browser's location through its own message type must classify it as a
commitment itself; the framework cannot see it.* It sits **beside the lens laws** — line 2's
register, an assumption about the adopter's own wiring that no framework property can quantify over
— and it is stated as an **instruction** to the adopter, not as a caveat. The navigation obligation
**stays at line 16** (S31); the plan's numbering follows this table.

Also taken, being the 🟡 items that fall in this file: **🟡 3** — the three `00-scope.md:NNN`
citations now cite the invariant **id and clause** instead, because the scope file demonstrably
moves and the ids exist to make it citable; and **🟡 7** — spec line 16 gains an acceptance-layer
example, **A10** (*type, press Back, press undo — it refuses and names the navigation*). A10 was
raised by the Critic as optional. I took it: it is the pass's most user-recognisable behaviour, the
acceptance layer's only job is recognition, and it costs **nothing** in fixture — the same
judgement I got wrong in the other direction when I first parked spec line 12 in a plan step.

**S30's two instances in this file, corrected.** S30 was among the thirteen mitigations the user
accepted, and two of its instances are rows in this table. **Spec line 3** said *"the world is a
command's feedback and nothing else"* — false beside line 16, and not merely verbally: on the
browser head an incoming `UrlChanged` **is** a subscription delivery, so line 3's *"a
subscription-delivered message is a user action"* and line 16 were classifying the same message on
the same path two different ways. Line 3 now carries the exception and says why it has to be stated
there. **Spec line 5** gave one of the reach bounds; it now gives all three, at equal weight, with
the navigation bound (iii) named as usually the binding one in a routed application. Neither is a
new decision — both are consequences of the navigation amendment that had not been written down.

**Deliberately not taken, and each is one sentence if you want it.** The 🟢 offers stay untaken:

- **🟢 3** — the Critic's derivation that the two stacks cannot drift (no movement issues a
  `NavigationCommand`) is covered by derivation, not by a test; the fixture has no browser stack to
  diverge. Worth one sentence here so no reader believes that clause is separately tested.
- **🟢 2** — the derivation that INV-3 does not need the location. The Critic offered *"next to INV-3
  or in the handoff"*; the handoff is the architect's and I have not assumed the choice.
- **🟡 1, 🟡 2, 🟡 4, 🟡 6** — none is this file's. 🟡 1's *"exactly one property per id"* wording is
  in `00-scope.md` and the plan (this file already records the departure); 🟡 2 and 🟡 4 are the
  plan's; 🟡 6's last-writer-wins cause is step 3's wording. I did check 🟡 6 against INV-2's second
  property: its refusal assertions run only on the step whose message **is** the navigation, so a
  later action overwriting the cause never falls under them, and the property is unaffected.

**Nothing else changed in amendments 1–3.** The four questions below are kept as the record of what
was asked and how it was answered. **Amendment 4 is recorded separately, at the end of this file,
because it is a re-opening rather than a pre-lock amendment and it carries its own 🛑.**

Four things worth looking at before you answer:

1. **The level.** No AppHost, no Playwright — a real `Runtime` in-process. My reason is that
   nothing in this pass adopts `WithHistory`, so there is no application for an AppHost to start
   and no page for Playwright to click; the AppHost rule governs cross-service tests and this is a
   library with no service. If you would rather see the first spec go through a head, that means
   adopting `WithHistory` in a demo — which gate-3 decision 4 explicitly excluded, and reversing it
   is your call, not mine.

2. **Four spec lines are stated but not asserted here** — pre-record navigation, `Clear`'s two
   effects, undo out of a terminal state, and the InteractiveAuto handoff (lines 10–13). Each needs
   a fixture shape that exists for one assertion. I put them in plan steps 6 and 10 rather than in
   the locked file. **If any of those four is something you would want to see fail before you
   believed the feature was broken, it belongs in the lock**, and moving it in now is free.

   **Answered: line 12 moves in, the other three stay out.** A9 is the result. The fixture cost
   was one message (`CloseEditor`), one `IsTerminal` predicate, and one generator exclusion —
   smaller than I estimated when I put it in step 6, and the user was right that a feature which
   claims to resurrect a terminated program should have to prove it before anyone believes it.

3. **INV-7's property is honest but narrow.** Its non-trivial content is entirely in the bracket
   cases, because an un-bracketed press satisfies the invariant by definition — and the shape that
   can actually falsify it has to be *seeded explicitly*, since random generation over a small
   model may never produce it. I have built the fixture's `Subscriptions` to be non-monotonic
   precisely so that shape exists. It is the property in this spec I would most want a second pair
   of eyes on.

4. **The support files sit outside the lock.** The generators and the document comparer can make a
   locked property vacuous without the locked file changing. I have closed it with a written
   generator contract and a falsifier per property rather than by locking more files, because
   locking generators would freeze a debugging tool. Tell me if you would rather they were locked
   too.

   **Partly answered by amendment 4** — the closure was incomplete, and ⚠️-5's six obligations are
   now enumerated in § *The Lock*. The support files are still not locked; the difference is that
   three of the six are now *checkable from inside* the lock rather than merely hoped for.

---

## 🔧 Amendment 4 — the PR-0 review, 2026-09-07

**Why this is a re-opening and not a fourth pre-lock amendment.** Amendments 1–3 were the user
finding gaps while still holding the approver's pen, before the approval commit. This one comes
from **outside**: `csharp-dev` transcribed the approved text into
`Picea.Abies.Tests/History/UndoRedoSpec.cs`, `reviewer-blind` read the changeset with the narrative
out of reach, and `reviewer-reconcile` returned **🔴 Changes Requested**.

**The evidence, and why it settles who acts.** The reviewer did the one check that neither the
missing feature types nor the missing fixtures prevented: it compiled and executed this file's
assertion **shapes** against the pinned **TUnit 1.19.57** in a scratch project — six probes — and
then extracted every `csharp` fence from *this document* (`06-spec.md`), reassembled them and diffed
the reassembly against the committed `.cs`. The finding is the whole verdict: **both compile-verified
defects originate in the approved text of this file, verbatim, not in the transcription.**
`.Or.That(…)` was `06-spec.md:1186`; A6's two `IsEquivalentTo` calls were `:505` and `:509`; the
third was `:575`. `csharp-dev` may not correct approved text — that is *"editing it to match what
the code does"*, a 🔴 by the lock's own terms — so the route is a `spec-author` amendment with **user
re-approval**, and **PR 0 is the approval commit**, which makes this the last cheap moment.

Every change below stays **inside the approved-text fences**, so `csharp-dev` re-transcribes the
fences verbatim rather than hand-patching the `.cs`.

### The reviewer's list, and what each one did to this file

| id | Finding | Disposition here |
|---|---|---|
| **🔴-1** | `.Or.That(…)` — `error CS1061: 'OrContinuation<int>' does not contain a definition for 'That'`. TUnit's `.Or` continues on the **same** subject, so a disjunction across two different values is a wrong *shape*, not a wrong overload; it would still not compile after step 6 delivered every missing type. | **Taken.** INV-6's `Available` case computes one boolean — `runtime.Model.Present != h.Present \|\| CursorMoved(h, runtime.Model)` — and asserts it once. The claim is unchanged. The **file-header sentence** the reviewer also faults is `csharp-dev`'s, not approved text (`grep -c "Locked spec for undo/redo" 06-spec.md` → `0`); it is theirs to correct either way. |
| **🔴-2** | A6's two `IsEquivalentTo` calls are order-insensitive and the two expected sequences are permutations of the same four strings, so the ordering test could not fail for the reason it exists. | **Taken, and the third instance with them** — A7's second test asserts `["start:…","stop:…"]`, and *"reconcile twice"* is an ordering claim that *stop-then-start* satisfied. **⚠️ The remedy amendment 4 chose was wrong and amendment 5 replaces it** — round 1's supporting claims (*`IsEqualTo` on a collection is order-sensitive*; *`CollectionOrdering` does not exist*) were both false. See § *Amendment 5*. **Deliberately left as bare `IsEquivalentTo`:** INV-7's assertion (1) (a reconciliation **set**) and assertion (4) (a one-element **distinct** set). They are order-insensitive *by intent*, and this note exists so nobody "fixes" them later — though assertion (4) turned out to carry an element-equality obligation, now Lock table row **(g)**. |
| **🔴-3(a)** | INV-6's comment claims a coverage assertion at the end of the loop. There is none. **This is the Lock's own named closure for this property.** | **Taken**, and it cost more than a line: writing the assertion showed that `BlockedByWorld` was **unreachable in either direction** under `NoFeedback`, so the ten-combination claim could never have been met by any seed. INV-6 therefore now drives `LoadReturns("server")` through `Drive`. The **alphabet is unchanged** — S27's partition table is untouched; only the interpreter moved. |
| **🔴-3(b)** | *"INV-2 runs twice, over `Editor` and `ProjectedEditor`."* It ran once; `ProjectedEditor` appeared nowhere in the invariant layer. **The reviewer found this; `08` did not.** | **Taken by making the code true to the claim, not by weakening the claim.** The claim is load-bearing: under the whole-model lens the invariant holds *by construction*, so testing only that lens tests the half that cannot fail. The property now walks the script under both lenses against one reachable set, and the falsifier table gains a projection-specific mutation. |
| **🔴-3(c)** | INV-2's second property claims *"not done until both branches have been observed"*. Neither branch was counted. | **Taken.** Two flags, two assertions after the loop. Without them the property's own second falsifier — *seal only the past edge* — could not turn it red on a corpus that never populated `Future`. |
| **🔴-3**, closing ¶ | Unguarded `continue` guards let INV-1, INV-3, INV-4, INV-5 and INV-6 all pass having asserted nothing. | **Taken.** A `reached` counter and a floor assertion on INV-1, INV-3, INV-4 and INV-5; INV-6 and INV-2's second property get the stronger coverage assertions instead. |
| **🔴-4** | The route is an amendment with re-approval, and committing PR 0 as-is forecloses it. | **This section, and the 🛑 below.** |
| **⚠️-5** | The lock places at least six obligations on the unlocked support files; the stated closure covered one. | **Taken** — enumerated as a table in § *The Lock*. (b) is downgraded by ⚠️-6's fix; (e) becomes *checkable* rather than merely stated, because 🔴-3(a) and (c) now fail loudly. |
| **⚠️-5(f)** | INV-5 snapshots `Both(…)` once and dispatches inside the loop. The reviewer **narrowed** `08`'s P7: under plan rule S7 (`04-realist-plan.md:1120`) a refusal takes the `pass` row and records no step — but `pass` still calls `sealTops(SealedByEffect(…))` when the command is not silent. | **Taken as a stated fixture obligation**, in both places it can be read: at INV-5's loop, and on the fixture `Transition`'s `_ =>` fall-through, which is where it is actually satisfied. `EditorProgram.Transition` must return `Commands.None` for `MovementRefused`. |
| **⚠️-6** | No `[NotInParallel]`, against six sibling classes in the same project and 25 tests on one static log. The attribute site is inside the lock. | **Taken**, and this is the clearest *"now or a hand-back"* item in the list. `[NotInParallel("history-editor-log")]`, matching the siblings' key convention. |
| **⚠️-8** | The format-on-save hook has no exclusion for a file the process declares immutable, and already reformatted the approved text. | **Half taken.** The hook is `devops`'. What is this file's is the consequence: § *The Lock* now says plainly that the lock is on **assertions and claims**, not whitespace, so a reformatted `if (…) continue;` is **not** a `// SPEC CONFLICT:`. Without that sentence the first hand-back would have been about a line break. |

**Not this file's, and left alone:** ⚠️-7 (the csproj wants `<None Include>` beside the
`Compile Remove` — `csharp-dev`), ⚠️-9 (the design pass is readable outside `.squad/design/` —
`devops`), ⚠️-11 (the PR body's precondition), ⚠️-12 (`.squad/log/` churn on `git commit -a`), and
💡 `SpecAttribute` (the reviewer probed `[Property("Spec", …)]` at class level and it compiles, so
the new public type is avoidable — but the approval trail is intact and it is not mine to reverse).

**One consequence I am flagging rather than burying: ⚠️-10 just fired.** INV-3 exists in three
copies, and `Picea.Abies.Presentation/content/demo/stops/5.5-property.cs` is line 36 of
`content/demo/SHA256SUMS`. Amendment 4 adds a counter and a floor assertion to INV-3, so that copy
and its checksum now diverge in **content**, not only in the whitespace the reviewer already
measured. The decision the reviewer asks for — which copy is canonical, and whether the demo copy
should be a generated excerpt — is now due rather than merely advisable.

### Offered, and taken — `[Timeout(30_000)]`

The user directed it in. It sits on the **class**, beside `[NotInParallel]`, on the same argument:
the attribute site is inside the lock, so it can only ever land before the approval commit.

`csharp-dev` measured a proxy in the shape of `RuntimeIsolationAndSubscriptionFaultTests.cs` — a
real `Runtime` per seed, 24–32 dispatches, a subscription toggling every third dispatch: **one seed
0.22 ms, 200 seeds 41.8 ms**, on an unshared Ryzen 9 9950X running .NET 10.0.110. That is a floor,
not CI. The derivation is **42 ms × 8 properties × 10 = 3,360 ms**, where the ×10 covers CI
slowdown plus what the proxy does not model — the real properties' per-step `Because` interpolation,
the nested `Both(h)` loops, generator construction, and INV-7's extra named seeds. Rounded to
**30 seconds**, roughly 9× the derived figure, because the number is inside the lock and the error
is asymmetric: over-shooting is free (a green suite never reaches a timeout, and the value can only
be *raised* by another re-approval), while under-shooting buys a false red on a loaded runner and a
hand-back on this pass's most load-bearing file. If it ever fires, that is a hang, not a slow
machine. The measurement and the derivation are recorded beside the attribute in the approved text,
so the next reader can check the arithmetic instead of guessing at the intent.

### Offered, not taken — say the word and either lands before the commit

- **`[Arguments]` / `[MethodDataSource]` instead of the hand-rolled `foreach (var seed in …)`.**
  One reported test per seed, no first-failure abort, per-seed isolation. This restructures all
  eight properties and changes what the corpus *is*, so it is a design change rather than a defect
  fix — I would want it as its own decision, not folded into an amendment about broken shapes.
- **A named constant for `TimeSpan.FromSeconds(5)`** (four sites) — needs a fixture member, which
  puts a locked file's behaviour behind an unlocked constant. I lean against; it is your call.

---

## 🛑 Re-approval Request — **answered 2026-09-07**

> **This test is the executable specification for undo/redo, and it has changed since you approved
> it. The changes are corrections, not new behaviour: two assertion shapes that TUnit 1.19.57
> proved could never do their job, three comments that claimed coverage the code did not implement,
> and a floor under five properties that could otherwise pass having asserted nothing. One change
> goes further than a correction — INV-6 now runs against a feedback interpreter, because writing
> its missing coverage assertion revealed that one of its five cases was unreachable.**
>
> **Do you re-approve this file as amended? Is INV-6's interpreter change what you want, or would
> you rather the coverage assertion claimed only the combinations the old fixture could reach? And
> does `[Timeout]` go in before the commit, given it cannot go in afterwards?**

**Re-approved, with three decisions.**

**Decision 1 — re-approved as amended, and the lock takes effect at the approval commit.** The file
is immutable **in its assertions and its claims** from that commit; whitespace normalised by the
repository's formatter is not a `// SPEC CONFLICT:` (§ *The Lock*). Everything else is: a change to
what is asserted, or to what a comment claims, goes back through `spec-author` and this section.

**Decision 2 — INV-6 keeps the feedback interpreter.** The user's reason, and it is the more
important half of the decision: **narrowing the coverage assertion to the combinations the old
fixture could reach would be adjusting the spec to the code** — the one move the lock exists to
prevent, arriving by the back door as a "smaller" change. `BlockedByWorld` was always part of
INV-6's claim; the fixture could not reach it; the fixture moved. **Alphabet and S27's partition are
untouched** — `Gen.UserActions` still, with only the interpreter changed — so no other property's
configuration shifts and the partition table stands as written.

**Decision 3 — `[Timeout]` goes in before the commit, generous, sized from a measured seed run.**
`[Timeout(30_000)]` on the class, with `csharp-dev`'s measurement and the ×8 × ×10 derivation
recorded beside it in the approved text. Recorded above under *Offered, and taken*.

**Also settled, outside this file.** The demo bundle stays **canonical at `07607bf`**; a re-export
against PR 0's merge commit comes later as a new README row. That is the answer to ⚠️-10's *"which
copy is canonical"* question — it does not change anything here, and it means the INV-3 divergence
this amendment introduces is a scheduled re-export rather than drift.

**Nothing is committed by this file.** `Picea.Abies.Tests/History/UndoRedoSpec.cs` is `csharp-dev`'s
to re-transcribe from the fences above, and the approval commit is theirs to make.

---

## 🔧 Amendment 5 — PR 0's round-2 re-review, 2026-09-07

**Amendment 4's remedy for 🔴-2 was wrong, and it was wrong because the evidence behind it was
wrong.** `csharp-dev` re-transcribed the amended fences; `reviewer-reconcile` re-reviewed
(`.squad/design/undo-redo-pr0/09-review-verdict.md` § *Re-review — round 2*, verdict 🔴 Changes
Requested) and opened by correcting **its own round-1 measurements**, in both directions:

| round 1 claimed | round 2 measured | how round 1 got it wrong |
|---|---|---|
| *"`IsEqualTo` on a collection **is** order-sensitive — my probe failed on a permutation, as it should."* | `IsEqualTo` on a collection fails on the **matching** sequence too. It is not order-sensitive; it is **never satisfiable**. | Only the **negative** control was run. A permutation failing is equally consistent with *order-sensitive* and with *always fails*. |
| *"`IsEquivalentTo(expected, CollectionOrdering.Matching)` → `CS0103` … there is no option flag on this version."* | `CollectionOrdering` **exists** on 1.19.57, in `TUnit.Assertions.Enums`. `CS0103` was a **missing `using`**. | A name-resolution failure read as an API absence, without checking the assembly. `strings` on `TUnit.Assertions.dll` lists `CollectionOrdering` and `IsEquivalentToAssertion\`2`. |

Amendment 4 had explicitly said it took *"the shape the reviewer had already executed"* rather than
one that merely looked reasonable. That instinct was right and it was aimed at a bad datum, which
is why § *How this spec first fails, honestly* now carries the stricter rule: **a shape is verified
only when a passing control and a failing control have both been run.**

### What amendment 5 changes — one amendment, both blockers

The round cap makes this deliberate rather than tidy: **round 3 splits the changeset**, and *"the
split never ships a red stated property"* — so three assertions that cannot go green could not be
split off into "ships anyway". Both blockers had to land together.

| id | Finding | Change |
|---|---|---|
| **🔴-5** | `Assert.That(<collection>).IsEqualTo([…])` **compiles and can never pass.** The collection expression is target-typed to `<>z__ReadOnlyArray<string>` and compared by `Equals` — reference equality — so subject and expectation are never equal whatever they contain. Swept across `string[]`, `List<string>`, `IEnumerable<string>`, `IReadOnlyList<string>`, `ImmutableArray<string>` and `ImmutableList<string>`: **all six fail on identical content**, and so does a bespoke `[CollectionBuilder]` type with correct `IEquatable<T>` value equality, whose probe prints `equals-new=True` on the line before the failure. **No support-file choice for `EditorLog.Timeline` rescues it.** Amendment 4 replaced three assertions that were green-whatever-happened with three that are red-whatever-happens — and step 6's obligation is *observe red for the right reason, then make green*, which three unsatisfiable tests can never close. | All three — A6's two and A7's one — become `IsEquivalentTo(expected, CollectionOrdering.Matching)`, the shape verified **both ways**: matching order passes, permutation fails, on `string[]`, `IEnumerable<string>` and `IReadOnlyList<string>` subjects. `using TUnit.Assertions.Enums;` added to the fence header, which is the whole of what `CS0103` was about. The alternative the reviewer also verified — `Assert.That(actual.SequenceEqual([…])).IsTrue()` — was **not** taken: it passes on a match but throws away the diff in the failure message, and a spec that fails without saying *how* the order differed is a worse specification. |
| **🔴-6** | The A6 paragraph amendment 4 wrote records both false claims **as fact, inside the locked text, citing the review as authority** — and under Decision 1 the lock covers *claims*, so after the approval commit correcting it would itself be a hand-back. A7 carried the same claim in shorter form. | Both paragraphs replaced with the **verified** facts and the round-2 evidence: all three shapes, what each does, and the exact `<>z__ReadOnlyArray` failure text. The measurements now live **beside the assertion** rather than in a review nobody re-reads at step 6. |

### Also taken

- **Lock table row (g) — a seventh support-file obligation (⚠️-9).** INV-7's assertion (4) compares
  `ReportedKeySetsDuringHold.Distinct()` against `[anchorKeys]` with `IsEquivalentTo`, which
  compares **elements** by `Equals`. Round 1 cleared this site on reasoning — *"order-insensitive by
  intent"* — which was right about ordering and silent about element equality. Measured: with
  `HashSet<string>` or `string[]` elements it fails on identical content; with a `record` element it
  passes on a match and fails on a mismatch. So the **locked assertion is correct** and the
  obligation is the fixture's: `Keys(…)` must return a value-equality type. `SingleReconciliation(…)`
  is safe if it returns a flat `IEnumerable<string>`.
- **The `[Timeout(30_000)]` caveat (⚠️-8), recorded beside the value.** Decision 3 stands — the
  attribute stays — and its **limit** is now stated: it fires on an await-shaped hang (verified: a
  `[Timeout(2_000)]` test awaiting `Task.Delay(6_000)` failed at 2 s), it does **not** fire on a
  synchronous spin (verified: a 6 s spin **passed**), and a timed-out body **keeps running** —
  `body completed after 5999 ms`, long after the failure was recorded. On a class whose correctness
  rests on the static `EditorLog` an orphaned body will corrupt the next test, and `[NotInParallel]`
  cannot prevent it because the orphan is a detached continuation, not a test. So the first red
  **after** a timeout is suspect, not independent. The `TUnit0015` warning (25 of them, non-fatal)
  and its remedy are recorded there too.
- **💡 — what the lock covers, said once.** § *The Lock* now states the three consequences of
  Decision 1's *"assertions and claims"*: whitespace outside, **signatures and attribute parameters
  outside** (so step 6 may add a `CancellationToken` parameter for `TUnit0015` without a hand-back),
  and `csharp-dev`'s own transcription commentary — the file header and the invariant-layer bridge —
  **inside the lock from the approval commit but outside this file's approval**, therefore theirs to
  own and theirs to correct.
- **One caveat at INV-6.** The review could not verify that all ten (case × direction) combinations
  are reachable; amendment 4 fixed one unreachable case by reasoning, not execution. If the coverage
  assertion goes red at step 6, the **first hypothesis is an unreachable combination, not a bug in
  `WithHistory`**. Named so it is not misdiagnosed as a feature defect.

**Not this file's, and unchanged:** ⚠️-7 — `History/UndoRedoSpec.cs` is still in no MSBuild item
group (`-getItem:None` → `{"None": []}`). Amendment 4 assigned it to `csharp-dev`; it was neither
done nor registered, and it is the only round-1 ⚠️ in that state. ⚠️-11 (no PR yet) and ⚠️-12
(`.squad/log/` churn — stage explicitly) are also still open and also not mine.

**What round 2 confirmed, which is worth keeping next to the failure:** six of the seven round-1
findings that were amendment 4's to fix are fixed properly, the transcription is byte-faithful to
the six fences (87 diff lines, none of them an assertion), the four `reached` floors all sit after
the last guard and before the first assertion, and the two findings that mattered most — INV-2's
projection lens and INV-6's interpreter — were fixed by making the code true to the claim rather
than the claim true to the code.

---

## 🛑 Re-approval Request — amendment 5, **open**

> **This file changed again, and this time because a correction was itself wrong. Three assertions
> that amendment 4 made *unsatisfiable* now use the one shape verified in both directions on the
> pinned TUnit — `IsEquivalentTo(expected, CollectionOrdering.Matching)` — and the paragraph that
> recorded the bad evidence as fact, inside the lock, is replaced by the measurements. Nothing else
> about what the spec claims has moved: the same ordering claims, the same nine acceptance
> scenarios, the same eight properties.**
>
> **Do you re-approve? Two things I would look at specifically: (i) I chose the
> `CollectionOrdering.Matching` overload over `Assert.That(actual.SequenceEqual([…])).IsTrue()`,
> which also works — my reason is the failure message, since a spec that fails without saying *how*
> the order differed is a worse specification; and (ii) the `[Timeout]` caveat is now recorded but
> the attribute is unchanged, on your Decision 3 — an orphaned body after a timeout can corrupt the
> next test through the static `EditorLog`, and no attribute available inside the lock prevents
> that.**

**This is round 2 of 2.** On round 3 the reviewer splits the changeset, and the split *never ships
a red stated property* — so a still-broken A6/A7 could not be carved off as "ships anyway". That is
the concrete reason both blockers are in one amendment, and the reason this is the last round in
which a correction is cheap.

Nothing is committed. The amendment is in this artifact only; `UndoRedoSpec.cs` is `csharp-dev`'s
to re-transcribe from the fences above once you have answered.
