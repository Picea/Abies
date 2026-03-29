using System.Reflection;
using System.Text.Json;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Tests;

#if DEBUG
public sealed class DebuggerRuntimeReplayApplicationTests
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
            replay: false);

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
            replay: false);

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
            replay: false);

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
            replay: false);

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

        public static Document View(ReplayCounterModel model) =>
            new("Replay Counter", div([], [
                button([onclick(new IncrementMessage())], [text("+")]),
                text($"count:{model.Count}")
            ]));

        public static Subscription Subscriptions(ReplayCounterModel model) =>
            SubscriptionModule.None;
    }
}
#endif
