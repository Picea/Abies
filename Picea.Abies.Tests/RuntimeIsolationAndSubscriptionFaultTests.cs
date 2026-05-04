using System.Collections.Concurrent;
using System.Diagnostics;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Tests;

public sealed class RuntimeIsolationAndSubscriptionFaultTests
{
    [Test]
    public async Task Runtime_ViewCache_IsIsolatedPerRuntimeSession()
    {
        ClearViewCache();

        using var runtimeA = await Runtime<ScopedLazyProgram, ScopedLazyModel, string>.Start(
            _ => { },
            _ => ValueTask.FromResult(Result<Message[], PipelineError>.Ok([])),
            argument: "alpha");

        var htmlA = Render.Html(runtimeA.CurrentDocument!.Body);

        using var runtimeB = await Runtime<ScopedLazyProgram, ScopedLazyModel, string>.Start(
            _ => { },
            _ => ValueTask.FromResult(Result<Message[], PipelineError>.Ok([])),
            argument: "beta");

        var htmlB = Render.Html(runtimeB.CurrentDocument!.Body);

        await Assert.That(htmlA).Contains("label:alpha");
        await Assert.That(htmlB).Contains("label:beta");
    }

    [Test]
    public async Task Runtime_ReportsConcurrentSubscriptionFaults_WithKeysAndExceptions()
    {
        var faults = new ConcurrentQueue<SubscriptionFault>();
        var allFaultsObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedFaultCount = 0;

        using var runtime = await Runtime<FaultProbeProgram, FaultProbeModel, Unit>.Start(
            _ => { },
            _ => ValueTask.FromResult(Result<Message[], PipelineError>.Ok([])),
            subscriptionFaulted: fault =>
            {
                faults.Enqueue(fault);
                if (Interlocked.Increment(ref observedFaultCount) == 2)
                {
                    allFaultsObserved.TrySetResult();
                }
            });

        await allFaultsObserved.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var observed = faults.ToArray();

        await Assert.That(observed).Count().IsEqualTo(2);
        await Assert.That(observed.Any(f => f.Key.Value == "fault:alpha")).IsTrue();
        await Assert.That(observed.Any(f => f.Key.Value == "fault:beta")).IsTrue();
        await Assert.That(observed.Any(f => f.Exception is InvalidOperationException)).IsTrue();
        await Assert.That(observed.Any(f => f.Exception is ArgumentException)).IsTrue();
    }

    [Test]
    public async Task Runtime_DispatchesDecisionErr_ThroughProgramMessageFlow()
    {
        using var runtime = await Runtime<DecisionErrProgram, DecisionErrModel, Unit>.Start(
            _ => { },
            _ => ValueTask.FromResult(Result<Message[], PipelineError>.Ok([])));

        await runtime.Dispatch(new TriggerDecisionErr());

        await Assert.That(runtime.Model.DecisionErrCount).IsEqualTo(1);
        await Assert.That(runtime.Model.TriggerCount).IsEqualTo(0);
        await Assert.That(runtime.Model.LastError).IsEqualTo("decider rejected command");
    }

    [Test]
    public async Task Runtime_DoesNotBlockFastDispatch_WhileSlowCommandIsInFlight()
    {
        static async ValueTask<Result<Message[], PipelineError>> Interpreter(Command command)
        {
            if (command is DelayCommand)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }

            return Result<Message[], PipelineError>.Ok([]);
        }

        using var runtime = await Runtime<DispatchProbeProgram, DispatchProbeModel, Unit>.Start(
            _ => { },
            Interpreter);

        var slowDispatch = runtime.Dispatch(new BeginSlowWorkflow());
        await Task.Delay(TimeSpan.FromMilliseconds(25));

        var stopwatch = Stopwatch.StartNew();
        await runtime.Dispatch(new FastUiCommand());
        stopwatch.Stop();

        await slowDispatch;

        await Assert.That(stopwatch.ElapsedMilliseconds < 200).IsTrue();
        await Assert.That(runtime.Model.FastCount).IsEqualTo(1);
        await Assert.That(runtime.Model.SlowCount).IsEqualTo(1);
    }

    [Test]
    public async Task Runtime_DoesNotBlockUrlChangedDispatch_WhileSlowCommandIsInFlight()
    {
        static async ValueTask<Result<Message[], PipelineError>> Interpreter(Command command)
        {
            if (command is DelayCommand)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }

            return Result<Message[], PipelineError>.Ok([]);
        }

        using var runtime = await Runtime<NavigationDispatchProbeProgram, NavigationDispatchProbeModel, Unit>.Start(
            _ => { },
            Interpreter);

        var slowDispatch = runtime.Dispatch(new BeginSlowWorkflow());
        await Task.Delay(TimeSpan.FromMilliseconds(25));

        var stopwatch = Stopwatch.StartNew();
        await runtime.Dispatch(new UrlChanged(new Url(["articles"], new Dictionary<string, string>(), Option<string>.None)));
        stopwatch.Stop();

        await slowDispatch;

        await Assert.That(stopwatch.ElapsedMilliseconds < 200).IsTrue();
        await Assert.That(runtime.Model.UrlChangedCount).IsEqualTo(1);
        await Assert.That(runtime.Model.SlowCount).IsEqualTo(1);
    }

    private sealed record DecisionErrModel(int TriggerCount, int DecisionErrCount, string? LastError);

    private sealed record DispatchProbeModel(int FastCount, int SlowCount);

    private sealed record NavigationDispatchProbeModel(int UrlChangedCount, int SlowCount);

    private sealed record TriggerDecisionErr : Message;

    private sealed record DecisionErrMessage(string Text) : Message;

    private sealed record BeginSlowWorkflow : Message;

    private sealed record FastUiCommand : Message;

    private sealed record SlowObserved : Message;

    private sealed record DelayCommand : Command;

    private sealed class DecisionErrProgram : Program<DecisionErrModel, Unit>
    {
        public static (DecisionErrModel, Command) Initialize(Unit argument) =>
            (new DecisionErrModel(0, 0, null), Commands.None);

        public static (DecisionErrModel, Command) Transition(DecisionErrModel model, Message message) =>
            message switch
            {
                TriggerDecisionErr => (model with { TriggerCount = model.TriggerCount + 1 }, Commands.None),
                DecisionErrMessage error =>
                    (model with
                    {
                        DecisionErrCount = model.DecisionErrCount + 1,
                        LastError = error.Text
                    }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(DecisionErrModel _, Message command) =>
            command switch
            {
                TriggerDecisionErr => Result<Message[], Message>.Err(new DecisionErrMessage("decider rejected command")),
                _ => Result<Message[], Message>.Ok([command])
            };

        public static bool IsTerminal(DecisionErrModel _) => false;

        public static Document View(DecisionErrModel model) =>
            new("Decision Err", div([], [text($"errors:{model.DecisionErrCount}")]));

        public static Subscription Subscriptions(DecisionErrModel model) =>
            SubscriptionModule.None;
    }

    private sealed class DispatchProbeProgram : Program<DispatchProbeModel, Unit>
    {
        public static (DispatchProbeModel, Command) Initialize(Unit argument) =>
            (new DispatchProbeModel(0, 0), Commands.None);

        public static (DispatchProbeModel, Command) Transition(DispatchProbeModel model, Message message) =>
            message switch
            {
                BeginSlowWorkflow => (model, new DelayCommand()),
                SlowObserved => (model with { SlowCount = model.SlowCount + 1 }, Commands.None),
                FastUiCommand => (model with { FastCount = model.FastCount + 1 }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(DispatchProbeModel _, Message command) =>
            command switch
            {
                BeginSlowWorkflow => Result<Message[], Message>.Ok([new BeginSlowWorkflow(), new SlowObserved()]),
                FastUiCommand => Result<Message[], Message>.Ok([new FastUiCommand()]),
                _ => Result<Message[], Message>.Ok([command])
            };

        public static bool IsTerminal(DispatchProbeModel _) => false;

        public static Document View(DispatchProbeModel model) =>
            new("Dispatch Probe", div([], [text($"fast:{model.FastCount};slow:{model.SlowCount}")]));

        public static Subscription Subscriptions(DispatchProbeModel model) =>
            SubscriptionModule.None;
    }

    private sealed class NavigationDispatchProbeProgram : Program<NavigationDispatchProbeModel, Unit>
    {
        public static (NavigationDispatchProbeModel, Command) Initialize(Unit argument) =>
            (new NavigationDispatchProbeModel(0, 0), Commands.None);

        public static (NavigationDispatchProbeModel, Command) Transition(NavigationDispatchProbeModel model, Message message) =>
            message switch
            {
                BeginSlowWorkflow => (model, new DelayCommand()),
                SlowObserved => (model with { SlowCount = model.SlowCount + 1 }, Commands.None),
                UrlChanged => (model with { UrlChangedCount = model.UrlChangedCount + 1 }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(NavigationDispatchProbeModel _, Message command) =>
            command switch
            {
                BeginSlowWorkflow => Result<Message[], Message>.Ok([new BeginSlowWorkflow(), new SlowObserved()]),
                UrlChanged changed => Result<Message[], Message>.Ok([changed]),
                _ => Result<Message[], Message>.Ok([command])
            };

        public static bool IsTerminal(NavigationDispatchProbeModel _) => false;

        public static Document View(NavigationDispatchProbeModel model) =>
            new("Navigation Dispatch Probe", div([], [text($"url:{model.UrlChangedCount};slow:{model.SlowCount}")]));

        public static Subscription Subscriptions(NavigationDispatchProbeModel model) =>
            SubscriptionModule.None;
    }

    private sealed record ScopedLazyModel(string Label);

    private sealed class ScopedLazyProgram : Program<ScopedLazyModel, string>
    {
        public static (ScopedLazyModel, Command) Initialize(string argument) =>
            (new ScopedLazyModel(argument), Commands.None);

        public static (ScopedLazyModel, Command) Transition(ScopedLazyModel model, Message message) =>
            (model, Commands.None);

        public static Result<Message[], Message> Decide(ScopedLazyModel _, Message command) =>
            Result<Message[], Message>.Ok([command]);

        public static bool IsTerminal(ScopedLazyModel _) => false;

        public static Document View(ScopedLazyModel model) =>
            new(
                "Scoped lazy",
                div(
                    [],
                    [
                        lazy(0, () => text($"label:{model.Label}"))
                    ]));

        public static Subscription Subscriptions(ScopedLazyModel model) =>
            SubscriptionModule.None;
    }

    private sealed record FaultProbeModel;

    private sealed class FaultProbeProgram : Program<FaultProbeModel, Unit>
    {
        public static (FaultProbeModel, Command) Initialize(Unit argument) =>
            (new FaultProbeModel(), Commands.None);

        public static (FaultProbeModel, Command) Transition(FaultProbeModel model, Message message) =>
            (model, Commands.None);

        public static Result<Message[], Message> Decide(FaultProbeModel _, Message command) =>
            Result<Message[], Message>.Ok([command]);

        public static bool IsTerminal(FaultProbeModel _) => false;

        public static Document View(FaultProbeModel model) =>
            new("Fault probe", div([], [text("ready")]));

        public static Subscription Subscriptions(FaultProbeModel model) =>
            SubscriptionModule.Batch(
                SubscriptionModule.Create(
                    "fault:alpha",
                    async (_, cancellationToken) =>
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
                        throw new InvalidOperationException("subscription alpha failed");
                    }),
                SubscriptionModule.Create(
                    "fault:beta",
                    async (_, cancellationToken) =>
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
                        throw new ArgumentException("subscription beta failed");
                    }));
    }
}
