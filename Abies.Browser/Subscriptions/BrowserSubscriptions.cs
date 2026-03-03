// =============================================================================
// Browser Subscriptions
// =============================================================================
// Browser-specific subscription helpers that bridge JS event sources
// (keyboard, mouse, scroll, animation frame, WebSocket, etc.) into the
// platform-independent Subscription model defined in Abies.
//
// These methods call into JS via Interop and register deserialization handlers
// via Runtime.RegisterSubscriptionHandler so that incoming JSON payloads are
// mapped to strongly-typed .NET records before being handed to the user's
// toMessage function.
// =============================================================================

using Abies;
using Abies.Html;

namespace Abies.Browser;

/// <summary>
/// Browser-specific subscription factory methods.
/// </summary>
/// <remarks>
/// These subscriptions are backed by JavaScript event sources and require the
/// WASM runtime. For platform-independent subscriptions (timers, custom sources),
/// see <see cref="SubscriptionModule"/>.
/// </remarks>
public static class BrowserSubscriptions
{
    private const string BrowserKeyPrefix = "browser:";

    internal static class SubscriptionInterop
    {
        // Allows tests to override interop wiring without touching JSImport methods.
        internal static Action<string, string, string?> Subscribe = Interop.Subscribe;
        internal static Action<string> Unsubscribe = Interop.Unsubscribe;
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
    /// Emits a message when the window is scrolled.
    /// </summary>
    /// <remarks>
    /// Tracks window-level scroll events, throttled to one event per animation frame.
    /// For element-level scroll tracking (e.g., a scrollable container), use the
    /// overload that accepts an element ID.
    /// </remarks>
    public static Subscription OnScroll(Func<ScrollEventData, Message> toMessage)
        => OnScroll($"{BrowserKeyPrefix}scroll", toMessage);

    /// <summary>
    /// Emits a message when the window is scrolled using a custom key.
    /// </summary>
    public static Subscription OnScroll(string key, Func<ScrollEventData, Message> toMessage)
        => CreateBrowserSubscription(key, "scroll", toMessage);

    /// <summary>
    /// Emits a message when a specific element is scrolled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tracks scroll events on a specific DOM element identified by its <c>id</c> attribute.
    /// This is the preferred way to track scroll position for virtualized lists.
    /// Events are throttled to one per animation frame to avoid flooding the MVU loop.
    /// </para>
    /// <para>
    /// If the target element is not yet in the DOM when the subscription starts,
    /// a MutationObserver waits for it to appear before attaching the listener.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// BrowserSubscriptions.OnScroll("feed-scroll", "article-feed-container", data =>
    ///     new Message.ScrollChanged(data.ScrollTop, data.ClientHeight))
    /// </code>
    /// </example>
    public static Subscription OnScroll(string key, string elementId, Func<ScrollEventData, Message> toMessage)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(
            new ScrollSubscriptionOptions(elementId),
            AbiesJsonContext.Default.ScrollSubscriptionOptions);
        return CreateBrowserSubscription(key, "scroll", payload, toMessage);
    }

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

        var payload = System.Text.Json.JsonSerializer.Serialize(options, AbiesJsonContext.Default.WebSocketOptions);
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

        return SubscriptionModule.Create(key, async (_, cancellationToken) =>
        {
            Runtime.RegisterSubscriptionHandler(
                key,
                data =>
                {
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
