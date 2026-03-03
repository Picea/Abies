// =============================================================================
// Subscriptions
// =============================================================================
// Subscriptions represent external event sources (timers, browser events,
// WebSockets) that feed messages into the MVU loop. They are declarative:
// you describe what you want to subscribe to, and the runtime manages the
// lifecycle (starting/stopping) automatically based on state changes.
//
// Key concepts:
// - Subscription.None: No subscriptions
// - Subscription.Batch: Multiple subscriptions grouped together
// - Subscription.Source: A concrete subscription with key and start function
// - SubscriptionManager: Diffs and manages active subscriptions
//
// Architecture Decision Records:
// - ADR-007: Subscriptions for External Events (docs/adr/ADR-007-subscriptions.md)
// - ADR-001: MVU Architecture (docs/adr/ADR-001-mvu-architecture.md)
// - ADR-006: Command Pattern for Side Effects (docs/adr/ADR-006-command-pattern.md)
// =============================================================================

namespace Abies;

/// <summary>
/// Dispatches a message into the MVU loop.
/// </summary>
public delegate Unit Dispatch(Message message);

/// <summary>
/// Starts a subscription effect and returns a task that completes when the subscription ends.
/// </summary>
public delegate Task StartSubscription(Dispatch dispatch, CancellationToken cancellationToken);

/// <summary>
/// Identifies a subscription instance for diffing and lifecycle control.
/// </summary>
public readonly record struct SubscriptionKey(string Value);

/// <summary>
/// Represents external event sources that feed messages into the MVU loop.
/// </summary>
/// <remarks>
/// Subscriptions are the Elm-style way to handle external events. Unlike Commands
/// (which fire once), Subscriptions produce a stream of messages over time.
///
/// See ADR-007: Subscriptions for External Events
/// </remarks>
public abstract record Subscription
{
    /// <summary>
    /// Represents the absence of subscriptions.
    /// </summary>
    public sealed record None : Subscription;

    /// <summary>
    /// Groups multiple subscriptions into one value.
    /// </summary>
    public sealed record Batch(IReadOnlyList<Subscription> Subscriptions) : Subscription;

    /// <summary>
    /// Represents a concrete subscription source.
    /// </summary>
    public sealed record Source(SubscriptionKey Key, StartSubscription Start) : Subscription;
}

/// <summary>
/// Provides platform-independent helpers for constructing subscription values.
/// Browser-specific subscriptions (keyboard, mouse, scroll, WebSocket, etc.)
/// are in <c>Abies.Browser.BrowserSubscriptions</c>.
/// </summary>
public static class SubscriptionModule
{
    /// <summary>
    /// Represents an empty set of subscriptions.
    /// </summary>
    public static Subscription None => new Subscription.None();

    /// <summary>
    /// Combines many subscriptions into a single batch.
    /// </summary>
    public static Subscription Batch(IEnumerable<Subscription> subscriptions)
    {
        // Normalize to a stable list so diffing is deterministic.
        var list = subscriptions?.ToArray() ?? Array.Empty<Subscription>();
        return new Subscription.Batch(list);
    }

    /// <summary>
    /// Creates a custom subscription source identified by a stable key.
    /// </summary>
    public static Subscription Create(string key, StartSubscription start)
    {
        // Ensure keys are valid and stable for diffing across updates.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Subscription key must be non-empty.", nameof(key));
        }

        if (start is null)
        {
            throw new ArgumentNullException(nameof(start));
        }

        return new Subscription.Source(new SubscriptionKey(key), start);
    }

    /// <summary>
    /// Emits messages on a fixed interval using a periodic timer.
    /// </summary>
    public static Subscription Every(TimeSpan interval, Func<DateTimeOffset, Message> toMessage)
    {
        var key = $"timer:{interval.TotalMilliseconds}";
        return Every(key, interval, toMessage);
    }

    /// <summary>
    /// Emits messages on a fixed interval using a custom key for diffing.
    /// </summary>
    public static Subscription Every(string key, TimeSpan interval, Func<DateTimeOffset, Message> toMessage)
    {
        // Ensure a stable key so multiple timers can coexist.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Subscription key must be non-empty.", nameof(key));
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be positive.");
        }

        if (toMessage is null)
        {
            throw new ArgumentNullException(nameof(toMessage));
        }

        return Create(key, async (dispatch, cancellationToken) =>
        {
            // Use PeriodicTimer to match Elm-style recurring subscriptions.
            using var timer = new PeriodicTimer(interval);

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    dispatch(toMessage(DateTimeOffset.UtcNow));
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation ends the subscription cleanly.
            }
        });
    }
}
