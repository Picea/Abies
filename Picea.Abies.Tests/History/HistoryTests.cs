using System.Reflection;
using Picea.Abies.History;

namespace Picea.Abies.Tests.History;

file sealed record SampleModel(string Value);

file sealed record SampleMessage(string Text) : Message;

/// <summary>
/// <c>History&lt;TModel&gt;</c>, <c>Step&lt;TModel&gt;</c>, <c>EdgeState</c>,
/// <c>Movement</c>, <c>Origin</c>. A minimal model and message pair stand in for a
/// wrapped program's — the richer fixture <c>Picea.Abies.Tests/History/UndoRedoSpec.cs</c>
/// uses belongs beside the wrapper that composes these types into a running program.
/// <para>
/// Calls to the static factory are qualified as <c>Abies.History.History.Start(...)</c>
/// rather than the shorter <c>History.Start(...)</c>: this namespace's own trailing
/// segment is "History" — the same simple name as that factory's type — and with a
/// <c>using</c> placed before a file-scoped namespace declaration (this file's shape,
/// and the team convention `.editorconfig` asks for), C#'s name resolution lets the
/// enclosing namespace <c>Picea.Abies.Tests.History</c> shadow the imported type, so
/// the fully-unqualified short form fails to resolve (CS0234) rather than binding to
/// the factory. Qualifying down to <c>Abies.History.History</c> — one segment short of
/// fully-qualified — resolves via the enclosing <c>Picea</c> namespace instead of the
/// `using`, sidesteps the collision, and is what `dotnet format` itself simplifies the
/// fully-qualified form to. <c>UndoRedoSpec.cs</c>, this namespace's locked sibling,
/// avoids the same collision the other way — by placing its <c>using</c> inside the
/// namespace body instead, which resolves the ambiguity in the type's favour at the
/// cost of a style deviation <c>dotnet format</c> will flag once that file is compiled.
/// </para>
/// </summary>
public sealed class HistoryTests
{
    [Test]
    public async Task Start_produces_an_empty_settled_fresh_history()
    {
        var model = new SampleModel("initial");

        var history = Abies.History.History.Start(model);

        await Assert.That(history.Present).IsEqualTo(model);
        await Assert.That(history.Past.IsEmpty).IsTrue();
        await Assert.That(history.Future.IsEmpty).IsTrue();
        await Assert.That(history.Movement).IsTypeOf<Movement.Settled>();
        await Assert.That(history.Origin).IsEqualTo(Origin.Fresh);
    }

    // ── (b) reflection: no public constructor ───────────────────────────────────

    [Test]
    public async Task History_exposes_no_public_constructor()
    {
        var publicConstructors = typeof(History<SampleModel>).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        await Assert.That(publicConstructors).IsEmpty();
    }

    [Test]
    public async Task Step_exposes_no_public_constructor()
    {
        var publicConstructors = typeof(Step<SampleModel>).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        await Assert.That(publicConstructors).IsEmpty();
    }

    // ── (a) undo-then-redo round trip; (c) StepBack/StepForward push Crossable ──

    [Test]
    public async Task StepBack_then_StepForward_round_trip_leaves_present_reference_identical_and_stacks_structurally_equal()
    {
        var p0 = new SampleModel("p0");
        var p1 = new SampleModel("p1");
        var cause = new SampleMessage("edit");

        // Simulates what the wrapper's own recording logic will do: push the state
        // being left as a crossable step, and move Present. Built directly here since
        // that recording logic is not part of this type.
        var recorded = new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 1);
        var h1 = Abies.History.History.Start(p0) with
        {
            Past = HistoryStack<Step<SampleModel>>.Empty.Push(recorded),
            Present = p1,
        };

        SampleModel Restore(SampleModel remembered, SampleModel _) => remembered;
        SampleModel Scrub(SampleModel model) => model;

        var h2 = h1.StepBack(Restore, Scrub);

        await Assert.That(h2.Past.IsEmpty).IsTrue();
        await Assert.That(ReferenceEquals(h2.Present, p0)).IsTrue();
        await Assert.That(h2.Future.Peek().Edge).IsTypeOf<EdgeState.Crossable>();
        await Assert.That(h2.Movement).IsTypeOf<Movement.Settled>(); // a press never touches Movement

        var h3 = h2.StepForward(Restore, Scrub);

        await Assert.That(h3.Future.IsEmpty).IsTrue();
        await Assert.That(ReferenceEquals(h3.Present, p1)).IsTrue(); // reference-identical
        await Assert.That(h3.Past.Peek().Edge).IsTypeOf<EdgeState.Crossable>(); // (c)
        await Assert.That(h3.Past.Peek().Cause).IsEqualTo(cause);
        await Assert.That(h3.Past.Peek().Model).IsEqualTo(p0);
        await Assert.That(h3.Past.Peek().AtTicks).IsEqualTo(1L);
        await Assert.That(h3.Movement).IsTypeOf<Movement.Settled>();
    }

    [Test]
    public async Task StepBack_applies_restore_and_scrub_rather_than_assuming_whole_model_replacement()
    {
        // A projection-shaped restore/scrub: only "Value" is undoable, everything else
        // of a hypothetical wider model would carry over from `current`. Modelled here
        // with SampleModel itself standing in for "the undoable part" — restore takes
        // the remembered value and ignores current outright is the WHOLE-MODEL case;
        // this test uses a restore that is provably not the identity of either
        // argument, so a StepBack that silently used one or the other would be caught.
        var p0 = new SampleModel("remembered");
        var p1 = new SampleModel("current");
        var cause = new SampleMessage("edit");

        var recorded = new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 0);
        var h1 = Abies.History.History.Start(p0) with
        {
            Past = HistoryStack<Step<SampleModel>>.Empty.Push(recorded),
            Present = p1,
        };

        var combined = new SampleModel("combined");
        var scrubbed = new SampleModel("scrubbed");

        var h2 = h1.StepBack(
            restore: (remembered, current) =>
            {
                if (!ReferenceEquals(remembered, p0))
                    throw new InvalidOperationException("wrong remembered value");
                if (!ReferenceEquals(current, p1))
                    throw new InvalidOperationException("wrong current value");
                return combined;
            },
            scrub: model =>
            {
                if (!ReferenceEquals(model, p1))
                    throw new InvalidOperationException("scrub saw the wrong model");
                return scrubbed;
            });

        await Assert.That(ReferenceEquals(h2.Present, combined)).IsTrue();
        await Assert.That(ReferenceEquals(h2.Future.Peek().Model, scrubbed)).IsTrue();
    }

    [Test]
    public async Task StepBack_on_an_empty_Past_throws()
    {
        var act = () => Abies.History.History.Start(new SampleModel("p0")).StepBack((r, _) => r, m => m);

        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task StepForward_on_an_empty_Future_throws()
    {
        var act = () => Abies.History.History.Start(new SampleModel("p0")).StepForward((r, _) => r, m => m);

        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    // ── SealTops / Cleared ───────────────────────────────────────────────────────

    [Test]
    public async Task SealTops_marks_the_top_of_each_non_empty_stack_and_is_a_no_op_where_empty()
    {
        var p0 = new SampleModel("p0");
        var cause = new SampleMessage("cause");
        var step = new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 0);

        var h = Abies.History.History.Start(p0) with { Past = HistoryStack<Step<SampleModel>>.Empty.Push(step) };

        var sealedHistory = h.SealTops(new EdgeState.SealedByWorld(cause));

        await Assert.That(sealedHistory.Past.Peek().Edge).IsTypeOf<EdgeState.SealedByWorld>();
        await Assert.That(sealedHistory.Future.IsEmpty).IsTrue();
    }

    [Test]
    public async Task SealTops_marks_both_tops_when_both_are_non_empty()
    {
        var p0 = new SampleModel("p0");
        var cause = new SampleMessage("cause");
        var step = new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 0);

        var h = Abies.History.History.Start(p0) with
        {
            Past = HistoryStack<Step<SampleModel>>.Empty.Push(step),
            Future = HistoryStack<Step<SampleModel>>.Empty.Push(step),
        };

        var sealedHistory = h.SealTops(new EdgeState.SupersededByNewAction(cause));

        await Assert.That(sealedHistory.Past.Peek().Edge).IsTypeOf<EdgeState.SupersededByNewAction>();
        await Assert.That(sealedHistory.Future.Peek().Edge).IsTypeOf<EdgeState.SupersededByNewAction>();
    }

    [Test]
    public async Task Cleared_discards_both_stacks_without_touching_present_movement_or_origin()
    {
        var p0 = new SampleModel("p0");
        var cause = new SampleMessage("cause");
        var step = new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 0);

        var h = Abies.History.History.Start(p0) with
        {
            Past = HistoryStack<Step<SampleModel>>.Empty.Push(step),
            Future = HistoryStack<Step<SampleModel>>.Empty.Push(step),
            Movement = new Movement.Held(p0),
        };

        var cleared = h.Cleared();

        await Assert.That(cleared.Past.IsEmpty).IsTrue();
        await Assert.That(cleared.Future.IsEmpty).IsTrue();
        await Assert.That(cleared.Present).IsEqualTo(h.Present);
        await Assert.That(cleared.Movement).IsEqualTo(h.Movement);
        await Assert.That(cleared.Origin).IsEqualTo(h.Origin);
    }

    // ── Movement's erased anchor ─────────────────────────────────────────────────

    [Test]
    public async Task Held_recovers_the_anchor_it_was_given()
    {
        var anchor = new SampleModel("anchor");

        var movement = new Movement.Held(anchor);

        await Assert.That(movement.Anchor).IsEqualTo(anchor);
    }

    // ── Structural equality ──────────────────────────────────────────────────────
    // History<TModel>'s record-synthesized Equals compares Past/Future through
    // HistoryStack<T>'s own Equals, so two histories built independently but with
    // the same content must compare equal — every operation returns a fresh stack
    // instance, so reference equality alone would say otherwise.

    [Test]
    public async Task Two_histories_with_the_same_content_are_equal_even_though_every_operation_returned_a_fresh_stack()
    {
        var p0 = new SampleModel("p0");
        var p1 = new SampleModel("p1");
        var cause = new SampleMessage("edit");

        History<SampleModel> Build() =>
            Abies.History.History.Start(p0) with
            {
                Present = p1,
                Past = HistoryStack<Step<SampleModel>>.Empty.Push(new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 1)),
            };

        var left = Build();
        var right = Build();

        await Assert.That(ReferenceEquals(left.Past, right.Past)).IsFalse(); // distinct stack instances
        await Assert.That(left).IsEqualTo(right);
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task Histories_differing_only_in_Past_are_not_equal()
    {
        var p0 = new SampleModel("p0");
        var cause = new SampleMessage("edit");

        var withOneStep = Abies.History.History.Start(p0) with
        {
            Past = HistoryStack<Step<SampleModel>>.Empty.Push(new Step<SampleModel>(p0, cause, new EdgeState.Crossable(), atTicks: 1)),
        };
        var withNoSteps = Abies.History.History.Start(p0);

        await Assert.That(withOneStep).IsNotEqualTo(withNoSteps);
    }

    // ── Hierarchy closure ────────────────────────────────────────────────────────
    // A record's compiler-generated copy constructor is `protected` regardless of
    // the declaring type's own accessibility, so a `private` declared constructor
    // alone does not stop an external assembly from writing
    // `sealed record Evil(Movement o) : Movement(o);` and reaching the default arm
    // of an exhaustive switch. That is a compile-time guarantee and cannot be
    // reproduced as a runtime test from inside this assembly: Picea.Abies.Tests
    // has InternalsVisibleTo, so it could implement the closing member itself,
    // which would prove nothing about an assembly that cannot. What the two tests
    // below verify at runtime is the mechanism an external assembly actually runs
    // into: the closing member's accessor is neither public nor protected, so it
    // cannot be overridden from outside Picea.Abies no matter how that assembly is
    // compiled. The compile-time claim itself was verified separately with an
    // external probe project (no InternalsVisibleTo, EnablePreviewFeatures=true):
    // `public sealed record Evil(Movement o) : Movement(o);` — which compiled and
    // ran before this fix — now fails with CS0535, "does not implement
    // 'Movement.HierarchySeal'".

    [Test]
    public async Task Movement_closes_its_hierarchy_with_an_internal_only_abstract_member()
    {
        var seal = typeof(Movement).GetProperty("HierarchySeal", BindingFlags.NonPublic | BindingFlags.Instance);

        await Assert.That(seal).IsNotNull();
        await Assert.That(seal!.GetMethod!.IsAssembly).IsTrue();  // internal: not public, not protected
        await Assert.That(seal.GetMethod!.IsPublic).IsFalse();
        await Assert.That(seal.GetMethod!.IsFamily).IsFalse();    // protected would still admit external derivation
    }

    [Test]
    public async Task EdgeState_closes_its_hierarchy_with_an_internal_only_abstract_member()
    {
        var seal = typeof(EdgeState).GetProperty("HierarchySeal", BindingFlags.NonPublic | BindingFlags.Instance);

        await Assert.That(seal).IsNotNull();
        await Assert.That(seal!.GetMethod!.IsAssembly).IsTrue();
        await Assert.That(seal.GetMethod!.IsPublic).IsFalse();
        await Assert.That(seal.GetMethod!.IsFamily).IsFalse();
    }

    [Test]
    public async Task Movements_only_concrete_cases_are_the_two_declared_here()
    {
        var cases = typeof(Movement).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Movement).IsAssignableFrom(t));

        await Assert.That(cases).IsEquivalentTo([typeof(Movement.Settled), typeof(Movement.Held)]);
    }

    [Test]
    public async Task EdgeStates_only_concrete_cases_are_the_four_declared_here()
    {
        var cases = typeof(EdgeState).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(EdgeState).IsAssignableFrom(t));

        await Assert.That(cases).IsEquivalentTo([
            typeof(EdgeState.Crossable),
            typeof(EdgeState.SealedByEffect),
            typeof(EdgeState.SealedByWorld),
            typeof(EdgeState.SupersededByNewAction),
        ]);
    }
}
