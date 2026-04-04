using System.Collections.Concurrent;
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

    private sealed record ScopedLazyModel(string Label);

    private sealed class ScopedLazyProgram : Program<ScopedLazyModel, string>
    {
        public static (ScopedLazyModel, Command) Initialize(string argument) =>
            (new ScopedLazyModel(argument), Commands.None);

        public static (ScopedLazyModel, Command) Transition(ScopedLazyModel model, Message message) =>
            (model, Commands.None);

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
