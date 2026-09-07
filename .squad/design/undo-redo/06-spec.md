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
   `[NotInParallel]`. `csharp-dev` picks; leaking state between tests is not an option.
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

[Spec("Undo and redo over the whole of an Abies application's state")]
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

        await Assert.That(wrappedOrder).IsEquivalentTo([
            "applied:BatchFirst", "applied:BatchSecond",
            "interpreted:FailingCommand", "interpreted:MarkCommand"]);

        await Assert.That(bareOrder).IsEquivalentTo([
            "applied:BatchFirst", "interpreted:FailingCommand",
            "applied:BatchSecond", "interpreted:MarkCommand"]);
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
        await Assert.That(EditorLog.SubscriptionActivity).IsEquivalentTo([
            $"start:{EditorLog.MagnifierKey}", $"stop:{EditorLog.MagnifierKey}"]);
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

            await Assert.That(runtime.Model.Present)
                .IsEqualTo(beforeMostRecentAction)
                .Because($"seed {seed}: one history for the application, regardless of which " +
                         $"region the most recent action came from");
        }
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
            // through the UNWRAPPED program — no history, no undo, no redo.
            var reachable = ReachableByOrdinaryActionsAlone(script);

            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script)
            {
                await runtime.Dispatch(step.Message);
                await Assert.That(reachable).Contains(runtime.Model.Present)
                    .Because($"seed {seed}: undo may not put the application into a state it " +
                             $"was never in and could never have been in");
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
                        await Assert.That(History.Backward(runtime.Model))
                            .IsEqualTo(new MovementAvailability.BlockedByWorld(mostRecentNavigation))
                            .Because($"seed {seed}: the past edge is sealed by the navigation");

                    if (runtime.Model.Future.Count > 0)
                        await Assert.That(History.Forward(runtime.Model))
                            .IsEqualTo(new MovementAvailability.BlockedByWorld(mostRecentNavigation))
                            .Because($"seed {seed}: the future edge is sealed by the same navigation");
                }
            }
        }
    }
```

> **The two `Count > 0` guards are guards, not softenings.** `Clear` is in the alphabet and empties
> both stacks, so a navigation can legitimately arrive with nothing to seal; asserting a refusal
> there would be asserting against `Nothing`, which is INV-4's answer and correct. The guards are
> also why the corpus is not sufficient on its own: **the property is not done until both branches
> have been observed taken at least once** — a run in which `Future` is always empty has checked
> only half of "both edges", and the falsifier below is chosen so that a one-sided seal goes red.

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

                await Assert.That(runtime.Model).IsEqualTo(before).Because($"seed {seed}, {direction}");
                await Assert.That(EditorLog.MessagesApplied).IsEmpty().Because($"seed {seed}, {direction}");
                await Assert.That(EditorLog.PatchesApplied).IsEmpty().Because($"seed {seed}, {direction}");
                await Assert.That(EditorLog.CommandsInterpreted).IsEmpty().Because($"seed {seed}, {direction}");
            }
        }
    }

    // ── INV-5 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-5")]
    public async Task INV_5_a_declined_movement_delivers_a_typed_reason_the_application_can_tell_from_a_no_op()
    {
        foreach (var seed in Gen.Corpus)
        {
            // S27: THIS property, and INV-7's, are the only ones whose alphabet contains a
            // source that delivers without being asked. It is the property that can see a
            // tick destroy a redo.
            var script = Gen.UserActionsAndDeliveries(seed, length: 24);
            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script) await Drive(runtime, step);   // S28 ordering inside

            foreach (var (direction, message, availability) in Both(runtime.Model))
            {
                if (availability is MovementAvailability.Available or MovementAvailability.Nothing)
                    continue;

                EditorLog.Clear();
                await runtime.Dispatch(message);               // must not throw

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
    }

    // ── INV-6 ────────────────────────────────────────────────────────────────────
    [Test]
    [Property("Invariant", "INV-6")]
    public async Task INV_6_availability_and_its_reason_are_computable_from_the_history_value_alone_and_agree_with_the_attempt()
    {
        foreach (var seed in Gen.Corpus)
        {
            var script = Gen.UserActions(seed, length: 24);
            using var runtime = await Start<Editor>(NoFeedback);
            foreach (var step in script) await runtime.Dispatch(step.Message);

            foreach (var (direction, message, _) in Both(runtime.Model))
            {
                var h = runtime.Model;
                EditorLog.Clear();

                // Computed IN ADVANCE, from the value BY ITSELF, WITHOUT causing any effect:
                var answer = direction is Direction.Backward ? History.Backward(h) : History.Forward(h);

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
                        await Assert.That(runtime.Model.Present).IsNotEqualTo(h.Present)
                            .Or.That(CursorMoved(h, runtime.Model)).IsTrue().Because($"seed {seed}");
                        await Assert.That(EditorLog.MessagesApplied.OfType<MovementRefused>()).IsEmpty();
                        break;
                    default:   // BlockedByEffect | BlockedByWorld | BlockedBySupersedingAction
                        await Assert.That(EditorLog.MessagesApplied.OfType<MovementRefused>().Single().Reason)
                            .IsEqualTo(answer).Because($"seed {seed}, {direction}");
                        break;
                }
            }
        }
    }
```

> INV-6 is checked in **both directions** and must be exercised over all **five** cases —
> `Nothing`, `Available`, `BlockedByEffect`, `BlockedByWorld`, `BlockedBySupersedingAction`. The
> corpus is not complete until every one of the ten (case × direction) combinations has been
> observed at least once; the property asserts that coverage at the end of the loop rather than
> assuming the generator found them.

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
| INV-2 | make `StepBack` compute `Restore(present, remembered)` — arguments swapped |
| INV-2 *(second)* | **delete the `origin is UrlChanged && Origin is Established -> seal` rule** so a navigation `record`s like any other action: the property must then go red on the first undo that crosses one, with `Present.Route` holding the pre-navigation URL while `browserShows` holds the new one. And **separately, seal only the past edge** — the `History.Forward` assertion must then go red, which is what makes *"both edges"* a checked claim rather than a sentence. **Note what these falsifiers do not reach:** they delete the *rule*, not the *idiom*, so the other route to the same defect — an application-named navigation message, which never reaches the rule at all — has no falsifier here and is out of the claim by obligation 17 (B10) |
| A10 | the same first mutation. A10 is the acceptance-layer twin of the property's first falsifier: delete the seal rule and `History.Backward` returns `Available` where A10 demands `BlockedByWorld(back)`. It is not a second line of defence and is not claimed as one — it exists for recognition (🟡 7) |
| INV-3 | make `DocumentComparer` compare raw HTML, or make `StepBack` carry the popped step's edge forward instead of pushing `Crossable` |
| INV-4 | return `Ok([Undo])` instead of `Ok([])` when `Backward(h)` is `Nothing` |
| INV-5 | make `record` **clear** `Future` instead of superseding it — the property must then fail with `Nothing` where it expected `BlockedBySupersedingAction` (plan step 8(k)); and separately, revert `Decide`'s `Err` branch to `Err(e)` — the property must fail with `BlockedByWorld` (plan step 8(j)) |
| INV-6 | make `Available` carry the crossable count instead of `Depth`, or report the cause of the message that *created* the step instead of the one that sealed it |
| INV-7 | **remove the anchor** — report `Subscriptions(h.Present)` during a hold. Assertions (1), (2) and (4) must all break |

**How this spec first fails, honestly.** `WithHistory`, `History<TModel>` and
`MovementAvailability` do not exist yet, so on the day it is committed this file does not compile.
That failure names exactly what is missing, which is the right signal, but a compile error is a
weaker one than a red assertion. The obligation on `csharp-dev` is therefore explicit: **at the
point the spec first compiles — the end of plan step 6 — every test in it must be observed red for
the right reason before any of them is made green**, and each falsifier above must be observed
before its step is closed. I have no `Bash` and cannot run any of this; that confirmation is the
owner's first implementation act, not mine.

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

Stated here explicitly so the implementing specialist cannot claim they did not know.

---

## 🛑 Approval Request — **answered 2026-09-07**

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

**Nothing else changed.** The four questions below are kept as the record of what was asked and how
it was answered.

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
