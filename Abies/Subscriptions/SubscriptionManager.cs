// =============================================================================
// Subscription Manager
// =============================================================================
// Manages the lifecycle of subscriptions: starting new ones, stopping removed
// ones, and maintaining a stable set of running subscriptions across updates.
// Uses key-based diffing to determine which subscriptions changed.
//
// Architecture Decision Records:
// - ADR-007: Subscriptions for External Events (docs/adr/ADR-007-subscriptions.md)
// - ADR-001: MVU Architecture (docs/adr/ADR-001-mvu-architecture.md)
// - ADR-013: OpenTelemetry Observability (docs/adr/ADR-013-opentelemetry.md)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Abies;

internal readonly record struct SubscriptionState(IReadOnlyDictionary<SubscriptionKey, RunningSubscription> Running);

internal sealed record RunningSubscription(SubscriptionKey Key, CancellationTokenSource CancellationTokenSource, Task Task);

/// <summary>
/// Manages subscription lifecycle: starting, stopping, and diffing subscriptions.
/// </summary>
/// <remarks>
/// The subscription manager uses key-based diffing (Elm-style) to determine
/// which subscriptions need to be started or stopped when the model changes.
/// 
/// See ADR-007: Subscriptions for External Events
/// </remarks>
internal static class SubscriptionManager
{
    public static SubscriptionState Start(Subscription subscription, Dispatch dispatch)
    {
        // Start all subscriptions from the initial model (Elm-style).
        using var activity = Instrumentation.ActivitySource.StartActivity("Subscription.StartAll");
        var sources = ToSources(subscription);
        var running = sources.Values.ToDictionary(source => source.Key, source => StartSubscription(source, dispatch));
        return new SubscriptionState(running);
    }

    public static SubscriptionState Update(SubscriptionState state, Subscription subscription, Dispatch dispatch)
    {
        // Diff by key so subscriptions are stable across updates.
        using var activity = Instrumentation.ActivitySource.StartActivity("Subscription.Update");
        var sources = ToSources(subscription);
        var running = new Dictionary<SubscriptionKey, RunningSubscription>(state.Running);

        foreach (var existing in state.Running.Keys)
        {
            if (!sources.ContainsKey(existing))
            {
                StopSubscription(running[existing]);
                running.Remove(existing);
            }
        }

        foreach (var source in sources.Values)
        {
            if (!running.ContainsKey(source.Key))
            {
                running[source.Key] = StartSubscription(source, dispatch);
            }
        }

        return new SubscriptionState(running);
    }

    public static async Task Stop(SubscriptionState state)
    {
        // Cancel all running subscriptions and wait for completion.
        using var activity = Instrumentation.ActivitySource.StartActivity("Subscription.StopAll");
        foreach (var running in state.Running.Values)
        {
            StopSubscription(running);
        }

        var tasks = state.Running.Values.Select(running => running.Task).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<SubscriptionKey, Subscription.Source> ToSources(Subscription subscription)
    {
        // Flatten batches into a key-indexed map for diffing.
        var sources = new Dictionary<SubscriptionKey, Subscription.Source>();

        Flatten(subscription, sources);
        return sources;
    }

    private static void Flatten(Subscription subscription, IDictionary<SubscriptionKey, Subscription.Source> sources)
    {
        // Last-write-wins so callers can override keys deliberately.
        switch (subscription)
        {
            case Subscription.None:
                return;
            case Subscription.Batch batch:
                foreach (var sub in batch.Subscriptions)
                {
                    Flatten(sub, sources);
                }
                return;
            case Subscription.Source source:
                sources[source.Key] = source;
                return;
            default:
                throw new InvalidOperationException($"Unsupported subscription type: {subscription.GetType().Name}");
        }
    }

    private static RunningSubscription StartSubscription(Subscription.Source source, Dispatch dispatch)
    {
        // Start each source with a dedicated cancellation token.
        using var activity = Instrumentation.ActivitySource.StartActivity("Subscription.Start");
        activity?.SetTag("subscription.key", source.Key.Value);

        var cts = new CancellationTokenSource();
        var task = source.Start(dispatch, cts.Token);
        return new RunningSubscription(source.Key, cts, task);
    }

    private static void StopSubscription(RunningSubscription running)
    {
        // Trigger cancellation without blocking the update loop.
        using var activity = Instrumentation.ActivitySource.StartActivity("Subscription.Stop");
        activity?.SetTag("subscription.key", running.Key.Value);
        running.CancellationTokenSource.Cancel();
    }
}
