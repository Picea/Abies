using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Tests;

#if DEBUG
public sealed partial class DebuggerRuntimeReplayApplicationTests
{
    [Test]
    public async Task Dispatch_CapturesPostTransitionModelSnapshots()
    {
        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        static void Apply(IReadOnlyList<Patch> _)
        { }

        using var runtime = await Runtime<ReplayCounterProgram, ReplayCounterModel, Unit>.Start(
            apply: Apply,
            interpreter: Interpreter,
            argument: default,
            titleChanged: null,
            navigationExecutor: null,
            subscriptionFaulted: null,
            initialUrl: null,
            threadSafe: false,
            replay: false,
            debuggerModelJsonTypeInfo: ReplayTestJsonContext.Default.ReplayCounterModel);

        runtime.UseDebugger();

        await runtime.Dispatch(new IncrementMessage());
        await runtime.Dispatch(new IncrementMessage());

        var timeline = runtime.Debugger!.Timeline;
        await Assert.That(timeline.Count).IsEqualTo(2);

        await Assert.That(ReadCount(timeline[0].ModelSnapshotPreview)).IsEqualTo(1);
        await Assert.That(ReadCount(timeline[1].ModelSnapshotPreview)).IsEqualTo(2);
    }

    [Test]
    public async Task ReplayJumpAndStep_ApplySnapshotToRenderedDocument()
    {
        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        static void Apply(IReadOnlyList<Patch> _)
        { }

        using var runtime = await Runtime<ReplayCounterProgram, ReplayCounterModel, Unit>.Start(
            apply: Apply,
            interpreter: Interpreter,
            argument: default,
            titleChanged: null,
            navigationExecutor: null,
            subscriptionFaulted: null,
            initialUrl: null,
            threadSafe: false,
            replay: false,
            debuggerModelJsonTypeInfo: ReplayTestJsonContext.Default.ReplayCounterModel);

        runtime.UseDebugger();

        await runtime.Dispatch(new IncrementMessage());
        await runtime.Dispatch(new IncrementMessage());

        var debugger = runtime.Debugger!;

        debugger.Jump(0);
        _ = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("count:1");

        debugger.StepForward();
        _ = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("count:2");
    }

    [Test]
    public async Task ImportedSession_CanApplySnapshotToRenderedDocument()
    {
        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        static void Apply(IReadOnlyList<Patch> _)
        { }

        using var runtime = await Runtime<ReplayCounterProgram, ReplayCounterModel, Unit>.Start(
            apply: Apply,
            interpreter: Interpreter,
            argument: default,
            titleChanged: null,
            navigationExecutor: null,
            subscriptionFaulted: null,
            initialUrl: null,
            threadSafe: false,
            replay: false,
            debuggerModelJsonTypeInfo: ReplayTestJsonContext.Default.ReplayCounterModel);

        runtime.UseDebugger();

        await runtime.Dispatch(new IncrementMessage());
        await runtime.Dispatch(new IncrementMessage());

        var debugger = runtime.Debugger!;
        var exportedSession = debugger.ExportSession(new Abies.Debugger.DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Tests",
            AppVersion = "1.0.0"
        });

        debugger.ClearTimeline();
        debugger.ImportSession(exportedSession);
        debugger.Jump(0);

        var applied = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(applied).IsTrue();
        await Assert.That(runtime.Model.Count).IsEqualTo(1);
        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("count:1");

        await runtime.Dispatch(new IncrementMessage());
        await Assert.That(runtime.Model.Count).IsEqualTo(2);
        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("count:2");
    }

    /// <summary>
    /// Regression test: pressing "next" after session import must change the rendered UI.
    /// Before the fix, StepForward from an imported session returned the snapshot as a JSON
    /// string, and TryApplyDebuggerSnapshot failed silently for models with abstract polymorphic
    /// types (like the Conduit Page DU), leaving the document unchanged.
    /// </summary>
    [Test]
    public async Task ImportedSession_StepForward_AppliesSnapshotToRenderedDocument()
    {
        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        static void Apply(IReadOnlyList<Patch> _)
        { }

        using var runtime = await Runtime<ReplayCounterProgram, ReplayCounterModel, Unit>.Start(
            apply: Apply,
            interpreter: Interpreter,
            argument: default,
            titleChanged: null,
            navigationExecutor: null,
            subscriptionFaulted: null,
            initialUrl: null,
            threadSafe: false,
            replay: false,
            debuggerModelJsonTypeInfo: ReplayTestJsonContext.Default.ReplayCounterModel);

        runtime.UseDebugger();

        await runtime.Dispatch(new IncrementMessage());
        await runtime.Dispatch(new IncrementMessage());

        var debugger = runtime.Debugger!;
        var exportedSession = debugger.ExportSession(new Abies.Debugger.DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Tests",
            AppVersion = "1.0.0"
        });

        // Clear and re-import to simulate cross-browser/cross-session import.
        debugger.ClearTimeline();
        debugger.ImportSession(exportedSession);

        // Start at position 0 (post-first-message state).
        debugger.Jump(0);
        _ = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("count:1");

        // Step forward to position 1 — must render count:2.
        debugger.StepForward();
        var applied = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(applied).IsTrue();
        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("count:2");
    }

    /// <summary>
    /// Regression test: StepForward after import must work for models with abstract polymorphic
    /// types that carry <see cref="JsonPolymorphicAttribute"/>.
    /// Without the attribute, default JSON serialization omits the type discriminator,
    /// making deserialization throw and TryApplyDebuggerSnapshot return false silently.
    /// </summary>
    [Test]
    public async Task ImportedSession_StepForward_WithPolymorphicPage_AppliesSnapshotToRenderedDocument()
    {
        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        static void Apply(IReadOnlyList<Patch> _)
        { }

        using var runtime = await Runtime<PolyCounterProgram, PolyCounterModel, Unit>.Start(
            apply: Apply,
            interpreter: Interpreter,
            argument: default,
            titleChanged: null,
            navigationExecutor: null,
            subscriptionFaulted: null,
            initialUrl: null,
            threadSafe: false,
            replay: false,
            debuggerModelJsonTypeInfo: ReplayTestJsonContext.Default.PolyCounterModel);

        runtime.UseDebugger();

        // Navigate: initial = Home(0) → Home(1) → Home(2)
        await runtime.Dispatch(new PolyIncrementMessage());
        await runtime.Dispatch(new PolyIncrementMessage());

        var debugger = runtime.Debugger!;
        var exportedSession = debugger.ExportSession(new Abies.Debugger.DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Tests",
            AppVersion = "1.0.0"
        });

        debugger.ClearTimeline();
        debugger.ImportSession(exportedSession);

        debugger.Jump(0);
        var firstApplied = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(firstApplied).IsTrue();
        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("poly:1");

        debugger.StepForward();
        var secondApplied = runtime.TryApplyDebuggerSnapshot(debugger.CurrentModelSnapshot);

        await Assert.That(secondApplied).IsTrue();
        await Assert.That(Render.Html(runtime.CurrentDocument!.Body)).Contains("poly:2");
    }

    [Test]
    public async Task HandlerDispatchPath_CapturesDebuggerSnapshots()
    {
        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        static void Apply(IReadOnlyList<Patch> _)
        { }

        using var runtime = await Runtime<ReplayCounterProgram, ReplayCounterModel, Unit>.Start(
            apply: Apply,
            interpreter: Interpreter,
            argument: default,
            titleChanged: null,
            navigationExecutor: null,
            subscriptionFaulted: null,
            initialUrl: null,
            threadSafe: false,
            replay: false,
            debuggerModelJsonTypeInfo: ReplayTestJsonContext.Default.ReplayCounterModel);

        runtime.UseDebugger();

        var dispatchProperty = typeof(HandlerRegistry).GetProperty("Dispatch", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Handler registry dispatch callback was not found.");

        if (dispatchProperty.GetValue(runtime.Handlers) is not Action<Message> dispatch)
        {
            throw new InvalidOperationException("Handler registry dispatch callback is not available.");
        }

        dispatch(new IncrementMessage());
        dispatch(new IncrementMessage());

        await Task.Delay(25);

        await Assert.That(runtime.Model.Count).IsEqualTo(2);
        await Assert.That(runtime.Debugger!.Timeline.Count).IsEqualTo(2);
        await Assert.That(ReadCount(runtime.Debugger.Timeline[1].ModelSnapshotPreview)).IsEqualTo(2);
    }

    private static int ReadCount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Count").GetInt32();
    }

    // ─── Simple counter model ─────────────────────────────────────────────────

    private sealed record ReplayCounterModel(int Count);

    private sealed record IncrementMessage : Message;

    private sealed class ReplayCounterProgram : Program<ReplayCounterModel, Unit>
    {
        public static (ReplayCounterModel, Command) Initialize(Unit argument) =>
            (new ReplayCounterModel(0), Commands.None);

        public static (ReplayCounterModel, Command) Transition(ReplayCounterModel model, Message message) =>
            message switch
            {
                IncrementMessage => (model with { Count = model.Count + 1 }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(ReplayCounterModel _, Message command) =>
            Result<Message[], Message>.Ok([command]);

        public static bool IsTerminal(ReplayCounterModel _) => false;

        public static Document View(ReplayCounterModel model) =>
            new("Replay Counter", div([], [
                button([onclick(new IncrementMessage())], [text("+")]),
                text($"count:{model.Count}")
            ]));

        public static Subscription Subscriptions(ReplayCounterModel model) =>
            SubscriptionModule.None;
    }

    // ─── Polymorphic page model (mirrors Conduit's Page DU pattern) ───────────

    /// <summary>
    /// Abstract discriminated union with [JsonPolymorphic] — must annotate for debugger
    /// snapshot round-trips to succeed. Without the attribute, serialization omits the
    /// '$type' discriminator and deserialization throws for abstract types.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$pt")]
    [JsonDerivedType(typeof(PolyHomePage), "Home")]
    private abstract record PolyPage
    {
        private PolyPage() { }
        public sealed record PolyHomePage(int Count) : PolyPage;
    }

    private sealed record PolyCounterModel(PolyPage Page);

    private sealed record PolyIncrementMessage : Message;

    private sealed class PolyCounterProgram : Program<PolyCounterModel, Unit>
    {
        public static (PolyCounterModel, Command) Initialize(Unit argument) =>
            (new PolyCounterModel(new PolyPage.PolyHomePage(0)), Commands.None);

        public static (PolyCounterModel, Command) Transition(PolyCounterModel model, Message message) =>
            message switch
            {
                PolyIncrementMessage when model.Page is PolyPage.PolyHomePage home =>
                    (model with { Page = new PolyPage.PolyHomePage(home.Count + 1) }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(PolyCounterModel _, Message command) =>
            Result<Message[], Message>.Ok([command]);

        public static bool IsTerminal(PolyCounterModel _) => false;

        public static Document View(PolyCounterModel model) =>
            model.Page switch
            {
                PolyPage.PolyHomePage home => new("Poly Counter", div([], [text($"poly:{home.Count}")])),
                _ => new("Poly Counter", div([], [text("unknown")]))
            };

        public static Subscription Subscriptions(PolyCounterModel model) =>
            SubscriptionModule.None;
    }

    [JsonSerializable(typeof(ReplayCounterModel))]
    [JsonSerializable(typeof(PolyCounterModel))]
    private sealed partial class ReplayTestJsonContext : JsonSerializerContext
    {
    }
}
#endif
