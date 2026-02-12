using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Abies.Tests;

[SupportedOSPlatform("browser")]
public sealed class SubscriptionManagerTests
{
    /// <summary>
    /// Ensures timer subscriptions dispatch messages on each tick.
    /// </summary>
    [Fact]
    public async Task Every_DispatchesMessagesOnTick()
    {
        var dispatched = new ConcurrentBag<Message>();
        Dispatch dispatch = msg => { dispatched.Add(msg); return new ValueTuple(); };

        var subscription = SubscriptionModule.Every(TimeSpan.FromMilliseconds(50), now => new TimerTick(now));
        var state = SubscriptionManager.Start(subscription, dispatch);

        // Wait for at least 3 ticks
        await WaitUntil(() => dispatched.Count >= 3, TimeSpan.FromSeconds(2));

        await SubscriptionManager.Stop(state);

        Assert.True(dispatched.Count >= 3, $"Expected at least 3 ticks, got {dispatched.Count}");
        Assert.All(dispatched, msg => Assert.IsType<TimerTick>(msg));
    }

    /// <summary>
    /// Ensures subscription diffing starts new sources and stops removed ones.
    /// </summary>
    [Fact]
    public async Task Update_StartsNewSources_StopsRemovedOnes()
    {
        // Track start/stop signals without relying on ordering.
        var started = new ConcurrentDictionary<string, int>();
        var stopped = new ConcurrentDictionary<string, int>();

        var initial = SubscriptionModule.Batch([
            CreateTracked("a", started, stopped),
            CreateTracked("b", started, stopped)
        ]);

        var state = SubscriptionManager.Start(initial, NoopDispatch);

        await WaitUntil(() => started.Count == 2, TimeSpan.FromSeconds(2));

        var updated = SubscriptionModule.Batch([
            CreateTracked("b", started, stopped),
            CreateTracked("c", started, stopped)
        ]);

        state = SubscriptionManager.Update(state, updated, NoopDispatch);

        await WaitUntil(() => started.ContainsKey("c"), TimeSpan.FromSeconds(2));
        await WaitUntil(() => stopped.ContainsKey("a"), TimeSpan.FromSeconds(2));

        Assert.True(started.ContainsKey("b"));
        Assert.False(stopped.ContainsKey("b"));
    }

    /// <summary>
    /// Ensures stopping a subscription state cancels and awaits all sources.
    /// </summary>
    [Fact]
    public async Task Stop_CancelsAndAwaitsAllSources()
    {
        // Track stop signals to ensure completion.
        var started = new ConcurrentDictionary<string, int>();
        var stopped = new ConcurrentDictionary<string, int>();

        var initial = SubscriptionModule.Batch([
            CreateTracked("a", started, stopped),
            CreateTracked("b", started, stopped)
        ]);

        var state = SubscriptionManager.Start(initial, NoopDispatch);

        await WaitUntil(() => started.Count == 2, TimeSpan.FromSeconds(2));

        await SubscriptionManager.Stop(state);

        await WaitUntil(() => stopped.Count == 2, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Ensures browser subscription helpers invoke JS interop wiring on start and stop.
    /// </summary>
    [Fact]
    public async Task BrowserSubscription_StartsAndStopsInterop()
    {
        var subscribed = new ConcurrentBag<(string key, string kind)>();
        var unsubscribed = new ConcurrentBag<string>();

        var originalSubscribe = SubscriptionModule.SubscriptionInterop.Subscribe;
        var originalUnsubscribe = SubscriptionModule.SubscriptionInterop.Unsubscribe;

        SubscriptionModule.SubscriptionInterop.Subscribe = (key, kind, _) => subscribed.Add((key, kind));
        SubscriptionModule.SubscriptionInterop.Unsubscribe = key => unsubscribed.Add(key);

        try
        {
            var subscription = SubscriptionModule.OnResize(_ => new TestMessage());
            var state = SubscriptionManager.Start(subscription, NoopDispatch);

            await WaitUntil(() => subscribed.Count == 1, TimeSpan.FromSeconds(2));

            await SubscriptionManager.Stop(state);

            await WaitUntil(() => unsubscribed.Count == 1, TimeSpan.FromSeconds(2));

            Assert.Contains(subscribed, item => item.key == "browser:resize" && item.kind == "resize");
            Assert.Contains(unsubscribed, key => key == "browser:resize");
        }
        finally
        {
            SubscriptionModule.SubscriptionInterop.Subscribe = originalSubscribe;
            SubscriptionModule.SubscriptionInterop.Unsubscribe = originalUnsubscribe;
        }
    }

    // Creates a subscription that signals when started and when cancelled.
    private static Subscription CreateTracked(
        string key,
        ConcurrentDictionary<string, int> started,
        ConcurrentDictionary<string, int> stopped)
        => SubscriptionModule.Create(key, async (_, cancellationToken) =>
        {
            started[key] = 1;

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is the expected shutdown mechanism.
            }

            stopped[key] = 1;
        });

    // Dispatch no-op to keep subscriptions running without side effects.
    private static readonly Dispatch NoopDispatch = _ => new ValueTuple();

    // Waits until a condition is true or times out to keep tests deterministic.
    private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

    private sealed record TestMessage : Message;
    private sealed record TimerTick(DateTimeOffset Now) : Message;
}
