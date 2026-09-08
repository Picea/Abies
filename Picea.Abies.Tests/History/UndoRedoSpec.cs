// This file is the locked specification for undo/redo, approved by the user on
// 2026-09-07 and re-approved as amended twice: on 2026-09-07 with amendment 4, then on
// 2026-09-08 with amendment 5 (.squad/design/undo-redo/06-spec.md — PR 0's review and round-2
// re-review, reviewer-reconcile 🔴 Changes Requested both times, each resolved by a
// spec-author amendment and user re-approval; see 06-spec.md § "Amendment 4", § "Amendment
// 5 — PR 0's round-2 re-review" and both "Re-approval Request" sections).
//
// It is excluded from compilation by Picea.Abies.Tests.csproj's
// <Compile Remove="History\UndoRedoSpec.cs" /> until plan step 6 removes that exclusion
// (it is carried as <None Include> in the meantime so it stays visible in IDE solution
// trees, per 06-spec.md ⚠️-7).
//
// It is immutable IN ITS ASSERTIONS AND ITS CLAIMS from the approval commit — see
// 06-spec.md § "The Lock": editing an assertion or what a comment claims to match what
// the code does, adding [Skip], or implementing it literally while knowing it produces
// wrong behaviour are all forbidden and are 🔴 Must Fix at review. Whitespace normalised
// by this repository's formatter is explicitly NOT a conflict (06-spec.md ⚠️-8), and
// neither is a signature or attribute-parameter change such as a [Test] method gaining a
// CancellationToken parameter (06-spec.md § "Amendment 5", the 💡 item on what the lock
// covers). If implementation reveals this file is wrong, csharp-dev stops, marks the
// conflict "// SPEC CONFLICT:" on the test, and hands back with options and a
// recommendation; spec-author produces an updated spec, the user re-approves, then
// implementation resumes.
//
// This header, and the note introducing the invariant layer below, are csharp-dev's own
// transcription commentary — not text from the approved fences. Per 06-spec.md §
// "Amendment 5" (the 💡 item on what the lock covers): both are inside the lock from the
// approval commit, but outside this file's approval, so a false sentence in either is
// csharp-dev's to correct directly, not a spec-author amendment.

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

    // ─────────────────────────────────────────────────────────────────────────────
    // Invariant layer (property-based). One property per INV-n, named so the id is
    // greppable, and carrying [Property("Invariant", "INV-n")]. INV-2 carries TWO
    // properties (the user's departure from "exactly one property per INV-n", accepted
    // as approver 2026-09-07); every other id carries exactly one. Eight properties total.
    //
    // The generator (Gen), the fixture (EditorProgram, EditorLog, the policy aliases
    // Editor / ProjectedEditor / ShallowEditor) and helpers such as Start, StartBare,
    // NoFeedback, Both, Drive, Movements, SingleReconciliation, Keys, CursorMoved,
    // ReachableByOrdinaryActionsAlone, LoadReturns, RecordingInterpreter,
    // FirstCommandFails, EditorMessages.All, CommandShape.Of and
    // DocumentComparer.EqualUpToHandlerIds are support code owned by HistoryTestProgram.cs
    // and Generators.cs (04-realist-plan.md § File-Level Changes) — outside this lock, per
    // 06-spec.md § "The Lock": "The support files are not locked, and that is a hole worth
    // naming." § The Lock (a) requires a global using static for the ~13 unqualified names,
    // and (g) (amendment 5) requires Keys(...) and ReportedKeySetsDuringHold to yield a
    // value-equality element type for INV-7's IsEquivalentTo(...) comparisons.
    //
    // This note, like the file header above, is csharp-dev's own transcription commentary,
    // not text from the approved fences: inside the lock from the approval commit, but
    // outside this file's approval, and therefore csharp-dev's to correct directly rather
    // than through a spec-author amendment (06-spec.md § "Amendment 5", the 💡 item on what
    // the lock covers).

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
}
