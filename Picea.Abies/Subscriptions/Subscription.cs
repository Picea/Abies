namespace Picea.Abies.Subscriptions;

/// <summary>
/// A function that dispatches a message into the MVU loop.
/// Used by subscription sources to feed external events into the application.
/// </summary>
/// <param name="message">The message to dispatch.</param>
public delegate void Dispatch(Message message);

/// <summary>
/// A function that starts a subscription source. The subscription should
/// run until the <paramref name="cancellationToken"/> is cancelled, dispatching
/// messages via <paramref name="dispatch"/> as external events arrive.
/// </summary>
/// <param name="dispatch">Function to dispatch messages into the MVU loop.</param>
/// <param name="cancellationToken">Token that signals when the subscription should stop.</param>
/// <returns>A task that completes when the subscription has fully stopped.</returns>
public delegate Task StartSubscription(Dispatch dispatch, CancellationToken cancellationToken);

/// <summary>
/// A unique key identifying a subscription source. Used by the
/// <see cref="SubscriptionManager"/> to determine which subscriptions
/// to start, keep, or stop when the model changes.
/// </summary>
/// <param name="Value">The unique key value.</param>
public readonly record struct SubscriptionKey(string Value);

/// <summary>
/// Describes the external event sources the application wants to listen to.
/// </summary>
public abstract record Subscription
{
    /// <summary>Prevents external inheritance.</summary>
    private Subscription() { }

    /// <summary>
    /// No subscriptions. The identity element of the subscription monoid.
    /// </summary>
    public sealed record None : Subscription;

    /// <summary>
    /// A batch of subscriptions to manage together. The binary operation of the subscription monoid.
    /// </summary>
    /// <param name="Subscriptions">The subscriptions in this batch.</param>
    public sealed record Batch(IReadOnlyList<Subscription> Subscriptions) : Subscription;

    /// <summary>
    /// A single subscription source identified by a unique key.
    /// </summary>
    /// <param name="Key">Unique identifier for this subscription source.</param>
    /// <param name="Start">The function that starts the subscription.</param>
    public sealed record Source(SubscriptionKey Key, StartSubscription Start) : Subscription;
}

/// <summary>
/// Factory methods for creating <see cref="Subscription"/> values.
/// </summary>
public static class SubscriptionModule
{
    /// <summary>
    /// No subscriptions. Singleton instance.
    /// </summary>
    public static readonly Subscription None = new Subscription.None();

    /// <summary>
    /// Combines multiple subscriptions into a single batch.
    /// </summary>
    public static Subscription Batch(params Subscription[] subscriptions) =>
        subscriptions.Length switch
        {
            0 => None,
            1 => subscriptions[0],
            _ => new Subscription.Batch(subscriptions)
        };

    /// <summary>
    /// Creates a named subscription source with a custom start function.
    /// </summary>
    public static Subscription Create(string key, StartSubscription start) =>
        new Subscription.Source(new SubscriptionKey(key), start);

    /// <summary>
    /// Creates a periodic timer subscription that dispatches a message at fixed intervals.
    /// </summary>
    public static Subscription Every(TimeSpan interval, Func<Message> message) =>
        new Subscription.Source(
            new SubscriptionKey($"every:{interval.TotalMilliseconds}"),
            async (dispatch, cancellationToken) =>
            {
                using var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    dispatch(message());
                }
            });

    /// <summary>
    /// Creates a periodic timer subscription with a custom key for disambiguation.
    /// </summary>
    public static Subscription Every(string key, TimeSpan interval, Func<Message> message) =>
        new Subscription.Source(
            new SubscriptionKey(key),
            async (dispatch, cancellationToken) =>
            {
                using var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    dispatch(message());
                }
            });
}
