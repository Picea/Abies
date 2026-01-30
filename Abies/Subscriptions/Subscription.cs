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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abies.Html;

namespace Abies;

/// <summary>
/// Dispatches a message into the MVU loop.
/// </summary>
public delegate ValueTuple Dispatch(Message message);

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
/// Provides helpers for constructing subscription values.
/// </summary>
public static class SubscriptionModule
{
    private const string BrowserKeyPrefix = "browser:";

    internal static class SubscriptionInterop
    {
        // Allows tests to override interop wiring without touching JSImport methods.
        internal static Action<string, string, string?> Subscribe = Interop.Subscribe;
        internal static Action<string> Unsubscribe = Interop.Unsubscribe;
    }

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

    /// <summary>
    /// Emits a message for every animation frame.
    /// </summary>
    public static Subscription OnAnimationFrame(Func<AnimationFrameData, Message> toMessage)
        => OnAnimationFrame($"{BrowserKeyPrefix}animationFrame", toMessage);

    /// <summary>
    /// Emits a message for every animation frame using a custom key.
    /// </summary>
    public static Subscription OnAnimationFrame(string key, Func<AnimationFrameData, Message> toMessage)
        => CreateBrowserSubscription(key, "animationFrame", toMessage);

    /// <summary>
    /// Emits a message for every animation frame with delta time.
    /// </summary>
    public static Subscription OnAnimationFrameDelta(Func<AnimationFrameDeltaData, Message> toMessage)
        => OnAnimationFrameDelta($"{BrowserKeyPrefix}animationFrameDelta", toMessage);

    /// <summary>
    /// Emits a message for every animation frame with delta time using a custom key.
    /// </summary>
    public static Subscription OnAnimationFrameDelta(string key, Func<AnimationFrameDeltaData, Message> toMessage)
        => CreateBrowserSubscription(key, "animationFrameDelta", toMessage);

    /// <summary>
    /// Emits a message whenever the viewport size changes.
    /// </summary>
    public static Subscription OnResize(Func<ViewportSize, Message> toMessage)
        => OnResize($"{BrowserKeyPrefix}resize", toMessage);

    /// <summary>
    /// Emits a message whenever the viewport size changes using a custom key.
    /// </summary>
    public static Subscription OnResize(string key, Func<ViewportSize, Message> toMessage)
        => CreateBrowserSubscription(key, "resize", toMessage);

    /// <summary>
    /// Emits a message whenever document visibility changes.
    /// </summary>
    public static Subscription OnVisibilityChange(Func<VisibilityEventData, Message> toMessage)
        => OnVisibilityChange($"{BrowserKeyPrefix}visibilityChange", toMessage);

    /// <summary>
    /// Emits a message whenever document visibility changes using a custom key.
    /// </summary>
    public static Subscription OnVisibilityChange(string key, Func<VisibilityEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "visibilityChange", toMessage);

    /// <summary>
    /// Emits a message on key down events.
    /// </summary>
    public static Subscription OnKeyDown(Func<KeyEventData, Message> toMessage)
        => OnKeyDown($"{BrowserKeyPrefix}keyDown", toMessage);

    /// <summary>
    /// Emits a message on key down events using a custom key.
    /// </summary>
    public static Subscription OnKeyDown(string key, Func<KeyEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "keyDown", toMessage);

    /// <summary>
    /// Emits a message on key up events.
    /// </summary>
    public static Subscription OnKeyUp(Func<KeyEventData, Message> toMessage)
        => OnKeyUp($"{BrowserKeyPrefix}keyUp", toMessage);

    /// <summary>
    /// Emits a message on key up events using a custom key.
    /// </summary>
    public static Subscription OnKeyUp(string key, Func<KeyEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "keyUp", toMessage);

    /// <summary>
    /// Emits a message on mouse down events.
    /// </summary>
    public static Subscription OnMouseDown(Func<PointerEventData, Message> toMessage)
        => OnMouseDown($"{BrowserKeyPrefix}mouseDown", toMessage);

    /// <summary>
    /// Emits a message on mouse down events using a custom key.
    /// </summary>
    public static Subscription OnMouseDown(string key, Func<PointerEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "mouseDown", toMessage);

    /// <summary>
    /// Emits a message on mouse up events.
    /// </summary>
    public static Subscription OnMouseUp(Func<PointerEventData, Message> toMessage)
        => OnMouseUp($"{BrowserKeyPrefix}mouseUp", toMessage);

    /// <summary>
    /// Emits a message on mouse up events using a custom key.
    /// </summary>
    public static Subscription OnMouseUp(string key, Func<PointerEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "mouseUp", toMessage);

    /// <summary>
    /// Emits a message on mouse move events.
    /// </summary>
    public static Subscription OnMouseMove(Func<PointerEventData, Message> toMessage)
        => OnMouseMove($"{BrowserKeyPrefix}mouseMove", toMessage);

    /// <summary>
    /// Emits a message on mouse move events using a custom key.
    /// </summary>
    public static Subscription OnMouseMove(string key, Func<PointerEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "mouseMove", toMessage);

    /// <summary>
    /// Emits a message on click events.
    /// </summary>
    public static Subscription OnClick(Func<PointerEventData, Message> toMessage)
        => OnClick($"{BrowserKeyPrefix}click", toMessage);

    /// <summary>
    /// Emits a message on click events using a custom key.
    /// </summary>
    public static Subscription OnClick(string key, Func<PointerEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "click", toMessage);

    /// <summary>
    /// Connects to a WebSocket and emits messages for open/close/error/message events.
    /// </summary>
    public static Subscription WebSocket(WebSocketOptions options, Func<WebSocketEvent, Message> toMessage)
        => WebSocket($"{BrowserKeyPrefix}websocket:{options.Url}", options, toMessage);

    /// <summary>
    /// Connects to a WebSocket with a custom key and emits messages for open/close/error/message events.
    /// </summary>
    public static Subscription WebSocket(string key, WebSocketOptions options, Func<WebSocketEvent, Message> toMessage)
    {
        if (string.IsNullOrWhiteSpace(options.Url))
        {
            throw new ArgumentException("WebSocket URL must be non-empty.", nameof(options));
        }

        if (toMessage is null)
        {
            throw new ArgumentNullException(nameof(toMessage));
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(options);
        return CreateBrowserSubscription(
            key,
            "websocket",
            payload,
            (WebSocketEventData data) => toMessage(data.ToEvent()));
    }

    // Builds a subscription that is backed by a JS event source.
    private static Subscription CreateBrowserSubscription<T>(string key, string kind, Func<T, Message> toMessage)
        => CreateBrowserSubscription(key, kind, null, toMessage);

    // Builds a subscription that is backed by a JS event source with optional payload.
    private static Subscription CreateBrowserSubscription<T>(string key, string kind, string? payload, Func<T, Message> toMessage)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Subscription kind must be non-empty.", nameof(kind));
        }

        if (toMessage is null)
        {
            throw new ArgumentNullException(nameof(toMessage));
        }

        return Create(key, async (_, cancellationToken) =>
        {
            Runtime.RegisterSubscriptionHandler(
                key,
                data => {
                    return data is T typed
                        ? toMessage(typed)
                        : throw new InvalidOperationException($"Subscription data mismatch for key '{key}'.");
                },
                typeof(T));

            SubscriptionInterop.Subscribe(key, kind, payload);

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is the expected shutdown path.
            }
            finally
            {
                SubscriptionInterop.Unsubscribe(key);
                Runtime.UnregisterSubscriptionHandler(key);
            }
        });
    }
}
