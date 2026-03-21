// =============================================================================
// Session — Server-Side MVU Session for Interactive Modes
// =============================================================================
// Wraps the Abies Runtime for server-side interactive use. Each connected
// client gets its own Session holding an MVU runtime instance with its own
// HandlerRegistry — enabling isolated, concurrent sessions.
//
// The session bridges the transport (delegates) to the runtime:
//
//     ReceiveEvent → CreateMessage → Runtime.Dispatch → Observe
//                                                        ↓
//                                               Diff → Patches
//                                                        ↓
//                                              RenderBatchWriter.Write
//                                                        ↓
//                                                   SendPatches
//
// This is still a pure computational library — the Session doesn't know
// about WebSockets, HTTP, or any server framework. It only knows about
// the delegate types from Transport.cs.
//
// The hosting adapter (e.g., Picea.Abies.Server.Kestrel) creates Sessions and
// wires the delegates to the actual transport.
//
// Thread safety: The session uses threadSafe: true on the underlying
// Picea runtime since server sessions are accessed from async I/O threads.
// Each session's HandlerRegistry is instance-based (not shared), so no
// cross-session contention exists.
// =============================================================================

using System.Diagnostics;

namespace Picea.Abies.Server;

/// <summary>
/// Static factory for creating server-side interactive MVU sessions.
/// </summary>
/// <remarks>
/// <para>
/// Provides a non-generic entry point for creating <see cref="Session{TProgram,TModel,TArgument}"/>
/// instances. Each session wraps an Abies <see cref="Runtime{TProgram,TModel,TArgument}"/>
/// with server-specific concerns:
/// </para>
/// <list type="bullet">
///   <item>Thread-safe dispatch (server sessions are multi-threaded)</item>
///   <item>Binary patch serialization via <see cref="RenderBatchWriter"/></item>
///   <item>Transport integration via <see cref="SendPatches"/> and <see cref="ReceiveEvent"/> delegates</item>
///   <item>Session lifecycle management (start, run event loop, dispose)</item>
/// </list>
/// <example>
/// <code>
/// // In a hosting adapter (e.g., Kestrel WebSocket handler):
/// var session = await Session.Start&lt;MyApp, MyModel, Unit&gt;(
///     sendPatches: bytes => webSocket.SendAsync(bytes, ...),
///     receiveEvent: ct => DeserializeFromWebSocket(webSocket, ct),
///     interpreter: MyInterpreter.Interpret);
///
/// await session.RunEventLoop(cancellationToken);
/// session.Dispose();
/// </code>
/// </example>
/// </remarks>
public static class Session
{
    /// <summary>
    /// Starts a server-side interactive MVU session.
    /// </summary>
    /// <typeparam name="TProgram">The Abies program type.</typeparam>
    /// <typeparam name="TModel">The application model.</typeparam>
    /// <typeparam name="TArgument">Initialization parameters.</typeparam>
    /// <param name="sendPatches">Delegate to send binary patch batches to the client.</param>
    /// <param name="receiveEvent">Delegate to receive DOM events from the client.</param>
    /// <param name="interpreter">Command interpreter for side effects.</param>
    /// <param name="sendText">Delegate to send text messages (e.g., navigation commands) to the client.</param>
    /// <param name="argument">Initialization parameters for the program.</param>
    /// <param name="initialUrl">Optional initial URL for routing.</param>
    /// <returns>A started session ready to run the event loop.</returns>
    public static Task<Session<TProgram, TModel, TArgument>> Start<TProgram, TModel, TArgument>(
        SendPatches sendPatches,
        ReceiveEvent receiveEvent,
        Interpreter<Command, Message> interpreter,
        SendText? sendText = null,
        TArgument argument = default!,
        Url? initialUrl = null)
        where TProgram : Program<TModel, TArgument>
    {
        return Session<TProgram, TModel, TArgument>.Start(
            sendPatches, receiveEvent, interpreter, sendText, argument, initialUrl);
    }
}

/// <summary>
/// Strongly-typed server-side MVU session.
/// </summary>
/// <typeparam name="TProgram">The Abies program type.</typeparam>
/// <typeparam name="TModel">The application model.</typeparam>
/// <typeparam name="TArgument">Initialization parameters.</typeparam>
public sealed class Session<TProgram, TModel, TArgument> : IDisposable
    where TProgram : Program<TModel, TArgument>
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Server.Session");

    /// <summary>
    /// Reserved command ID used by <c>abies-server.js</c> to signal a
    /// client-side URL change (pushState/popstate). This bypasses the
    /// <see cref="HandlerRegistry"/> and dispatches a <see cref="UrlChanged"/>
    /// message directly into the MVU loop.
    /// </summary>
    private const string _urlChangedCommandId = "__url_changed__";

    private readonly Runtime<TProgram, TModel, TArgument> _runtime;
    private readonly ReceiveEvent _receiveEvent;
    private readonly SendPatches _sendPatches;
    private bool _disposed;

    private Session(
        Runtime<TProgram, TModel, TArgument> runtime,
        ReceiveEvent receiveEvent,
        SendPatches sendPatches)
    {
        _runtime = runtime;
        _receiveEvent = receiveEvent;
        _sendPatches = sendPatches;
    }

    /// <summary>
    /// The current application model.
    /// </summary>
    public TModel Model => _runtime.Model;

    /// <summary>
    /// Starts a server-side interactive MVU session.
    /// </summary>
    internal static async Task<Session<TProgram, TModel, TArgument>> Start(
        SendPatches sendPatches,
        ReceiveEvent receiveEvent,
        Interpreter<Command, Message> interpreter,
        SendText? sendText = null,
        TArgument argument = default!,
        Url? initialUrl = null)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.Server.Session.Start");
        activity?.SetTag("abies.program", typeof(TProgram).Name);

        // Create the binary batch writer for serializing patches
        var batchWriter = new RenderBatchWriter();

        // Wire the Apply delegate: patches → binary batch → send over transport
        void ServerApply(IReadOnlyList<Patch> patches)
        {
            var binaryData = batchWriter.Write(patches);
            // Fire-and-forget the send — the transport handles backpressure
            _ = sendPatches(binaryData);
        }

        // Wire the navigation executor: NavigationCommand → JSON text frame → client
        // The client-side JS (abies-server.js) handles the text frame and calls
        // history.pushState/replaceState/back/forward accordingly.
        Action<NavigationCommand>? navigationExecutor = null;
        if (sendText is not null)
        {
            navigationExecutor = navCommand =>
            {
                (string? action, string? url) = navCommand switch
                {
                    NavigationCommand.Push push => ("push", FormatUrl(push.Url)),
                    NavigationCommand.Replace replace => ("replace", FormatUrl(replace.Url)),
                    NavigationCommand.GoBack => ("back", null),
                    NavigationCommand.GoForward => ("forward", null),
                    NavigationCommand.External ext => ("external", ext.Href),
                    _ => (null, null)
                };

                if (action is null)
                    return;

                var json = url is not null
                    ? $$"""{"type":"navigate","action":"{{action}}","url":"{{url}}"}"""
                    : $$"""{"type":"navigate","action":"{{action}}"}""";

                _ = sendText(json);
            };
        }

        // Start the core runtime with thread safety enabled (server is multi-threaded)
        var runtime = await Runtime<TProgram, TModel, TArgument>.Start(
            apply: ServerApply,
            interpreter: interpreter,
            argument: argument,
            navigationExecutor: navigationExecutor,
            initialUrl: initialUrl,
            threadSafe: true);

        activity?.SetStatus(ActivityStatusCode.Ok);

        return new Session<TProgram, TModel, TArgument>(runtime, receiveEvent, sendPatches);
    }

    /// <summary>
    /// Runs the event loop: receives DOM events from the client and dispatches
    /// them into the MVU runtime. Runs until the client disconnects or the
    /// cancellation token is triggered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loop is simple:
    /// <list type="number">
    ///   <item>Receive next DOM event from client (via <see cref="ReceiveEvent"/>)</item>
    ///   <item>Look up the handler in the runtime's <see cref="HandlerRegistry"/></item>
    ///   <item>Create a <see cref="Message"/> from the event data</item>
    ///   <item>Dispatch into the MVU runtime</item>
    ///   <item>The runtime's observer diffs and calls <see cref="Apply"/> → <see cref="SendPatches"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Token to stop the event loop.</param>
    public async Task RunEventLoop(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.Server.Session.EventLoop");

        while (!cancellationToken.IsCancellationRequested)
        {
            // Receive next event from the client
            var domEvent = await _receiveEvent(cancellationToken);

            // null means client disconnected
            if (domEvent is null)
                break;

            var evt = domEvent.Value;

            // Handle URL change events from client-side navigation.
            // These bypass the HandlerRegistry — there is no DOM element handler
            // for navigation. Instead, the JS client sends a reserved commandId
            // and the new path as eventData.
            if (evt.CommandId == _urlChangedCommandId)
            {
                var url = Navigation.ParseUrl(evt.EventData);
                await _runtime.Dispatch(new UrlChanged(url), cancellationToken);
                continue;
            }

            // Look up the handler in this session's registry and create a message
            var message = _runtime.Handlers.CreateMessage(evt.CommandId, evt.EventData);
            if (message is null)
                continue;

            // Dispatch into the MVU loop
            await _runtime.Dispatch(message, cancellationToken);
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _runtime.Dispose();
    }

    /// <summary>
    /// Formats an Abies <see cref="Url"/> record as a path string suitable
    /// for <c>history.pushState</c> (e.g., <c>/articles/my-slug?key=value#section</c>).
    /// </summary>
    private static string FormatUrl(Url url) => url.ToRelativeUri();
}
