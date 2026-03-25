using System.Diagnostics;

namespace Picea.Abies.Subscriptions;

/// <summary>
/// The current state of all running subscriptions.
/// </summary>
public readonly record struct SubscriptionState(
    IReadOnlyDictionary<SubscriptionKey, RunningSubscription> Running)
{
    public static readonly SubscriptionState Empty =
        new(new Dictionary<SubscriptionKey, RunningSubscription>());
}

/// <summary>
/// A subscription source that is currently running as a background task.
/// </summary>
public sealed record RunningSubscription(SubscriptionKey Key, CancellationTokenSource CTS, Task Task);

/// <summary>
/// A fault raised by a running subscription source.
/// </summary>
/// <param name="Key">Subscription key that produced the fault.</param>
/// <param name="Exception">Observed exception from the subscription task.</param>
public readonly record struct SubscriptionFault(SubscriptionKey Key, Exception Exception);

/// <summary>
/// Manages subscription lifecycle by diffing desired subscriptions against running ones.
/// </summary>
internal static class SubscriptionManager
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Subscriptions");

    internal static SubscriptionState Start(
        Subscription subscription,
        Dispatch dispatch,
        Action<SubscriptionFault>? faultObserver = null) =>
        Update(SubscriptionState.Empty, subscription, dispatch, faultObserver);

    internal static SubscriptionState Update(
        SubscriptionState current,
        Subscription desired,
        Dispatch dispatch,
        Action<SubscriptionFault>? faultObserver = null)
    {
        using var activity = _activitySource.StartActivity("Subscriptions.Update");

        var desiredSources = Flatten(desired);
        var newRunning = new Dictionary<SubscriptionKey, RunningSubscription>();

        foreach (var source in desiredSources)
        {
            if (current.Running.TryGetValue(source.Key, out var existing))
            {
                newRunning[source.Key] = existing;
            }
            else
            {
                var running = StartSubscription(source, dispatch, faultObserver);
                newRunning[source.Key] = running;
            }
        }

        foreach (var (key, running) in current.Running)
        {
            if (!newRunning.ContainsKey(key))
            {
                StopSubscription(running);
            }
        }

        activity?.SetTag("subscriptions.started", newRunning.Count - current.Running.Count);
        activity?.SetTag("subscriptions.total", newRunning.Count);

        return new SubscriptionState(newRunning);
    }

    internal static void Stop(SubscriptionState state)
    {
        using var activity = _activitySource.StartActivity("Subscriptions.Stop");

        foreach (var (_, running) in state.Running)
        {
            StopSubscription(running);
        }

        activity?.SetTag("subscriptions.stopped", state.Running.Count);
    }

    internal static IReadOnlyList<Subscription.Source> Flatten(Subscription subscription)
    {
        var sources = new List<Subscription.Source>();
        FlattenInto(subscription, sources);
        return sources;
    }

    private static void FlattenInto(Subscription subscription, List<Subscription.Source> sources)
    {
        switch (subscription)
        {
            case Subscription.None:
                break;

            case Subscription.Source source:
                sources.Add(source);
                break;

            case Subscription.Batch batch:
                foreach (var sub in batch.Subscriptions)
                {
                    FlattenInto(sub, sources);
                }
                break;
        }
    }

    private static RunningSubscription StartSubscription(
        Subscription.Source source,
        Dispatch dispatch,
        Action<SubscriptionFault>? faultObserver)
    {
        using var activity = _activitySource.StartActivity("Subscription.Start");
        activity?.SetTag("subscription.key", source.Key.Value);

        var cts = new CancellationTokenSource();
        var task = Task.Run(async () =>
        {
            try
            {
                await source.Start(dispatch, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                using var faultActivity = _activitySource.StartActivity("Subscription.Fault");
                faultActivity?.SetTag("subscription.key", source.Key.Value);
                faultActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                try
                {
                    faultObserver?.Invoke(new SubscriptionFault(source.Key, ex));
                }
                catch
                {
                    // Observer callbacks should never crash subscription teardown.
                }
            }
        });

        return new RunningSubscription(source.Key, cts, task);
    }

    private static void StopSubscription(RunningSubscription running)
    {
        using var activity = _activitySource.StartActivity("Subscription.Stop");
        activity?.SetTag("subscription.key", running.Key.Value);

        running.CTS.Cancel();
        running.CTS.Dispose();
    }
}
