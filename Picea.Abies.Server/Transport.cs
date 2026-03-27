// =============================================================================
// Transport — Delegate Types for Interactive Modes
// =============================================================================
// Defines the transport abstraction for Interactive Server and Interactive Auto
// render modes. These are pure delegate types — no implementation, no server
// dependencies. The hosting adapter provides concrete implementations backed
// by WebSocket, in-memory channels, or any other bidirectional transport.
//
// The transport carries two flows:
//
//     Server → Client:  Binary patch batches (same format as WASM interop)
//     Client → Server:  DOM events (commandId + event data + optional trace context)
//
// By expressing transport as delegates rather than interfaces, we stay
// consistent with the Abies pattern (see Apply delegate in Runtime.cs)
// and avoid forcing consumers into a class hierarchy.
//
// Architecture: This is the "port" side of a Ports & Adapters architecture.
// The hosting adapter (e.g., Picea.Abies.Server.Kestrel) provides the "adapter"
// that maps these delegates to WebSocket I/O.
// =============================================================================

namespace Picea.Abies.Server;

/// <summary>
/// Sends a binary patch batch to the client.
/// </summary>
/// <remarks>
/// <para>
/// The binary format is the same as <see cref="RenderBatchWriter"/> produces —
/// header + patch entries + string table. This is the same format used by
/// the WASM interop, so the client-side JavaScript can share the same
/// binary parser.
/// </para>
/// <para>
/// Implementations should handle backpressure and connection lifecycle.
/// If the client disconnects, the delegate should throw or return a faulted task
/// so the session can clean up.
/// </para>
/// </remarks>
/// <param name="patchBatch">The serialized binary patch batch.</param>
/// <returns>A task that completes when the data has been sent.</returns>
public delegate ValueTask SendPatches(ReadOnlyMemory<byte> patchBatch);

/// <summary>
/// Receives the next DOM event from the client.
/// </summary>
/// <remarks>
/// <para>
/// Returns <c>null</c> when the client disconnects (clean close).
/// The caller (Session) uses this to drive the MVU loop:
/// receive event → dispatch as Message → transition → diff → send patches.
/// </para>
/// <para>
/// The event data is transport-agnostic — the hosting adapter deserializes
/// from the wire format (e.g., WebSocket binary frame) into a <see cref="DomEvent"/>.
/// </para>
/// </remarks>
/// <param name="cancellationToken">Token to cancel the receive operation.</param>
/// <returns>The next DOM event, or <c>null</c> if the client disconnected.</returns>
public delegate ValueTask<DomEvent?> ReceiveEvent(CancellationToken cancellationToken = default);

/// <summary>
/// A DOM event received from the client in Interactive Server mode.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the same event structure that the WASM interop uses:
/// a command ID (from Praefixum) that identifies the event handler,
/// plus the event name, serialized event data, and optional W3C trace context.
/// </para>
/// <para>
/// The <see cref="CommandId"/> is looked up in the <see cref="HandlerRegistry"/>
/// to find the handler function that produces a <see cref="Message"/>.
/// </para>
/// </remarks>
/// <param name="CommandId">
/// The Praefixum-generated command ID identifying the event handler.
/// This matches the <c>id</c> attribute on the DOM element.
/// </param>
/// <param name="EventName">
/// The DOM event name (e.g., <c>"click"</c>, <c>"input"</c>, <c>"submit"</c>).
/// </param>
/// <param name="EventData">
/// Serialized event data (JSON string). The format depends on the event type.
/// For click events this may be empty; for input events it contains the value.
/// </param>
/// <param name="TraceParent">
/// Optional W3C <c>traceparent</c> header value captured in the browser when
/// OpenTelemetry UI-event tracing is enabled.
/// </param>
/// <param name="TraceState">
/// Optional W3C <c>tracestate</c> header value captured alongside
/// <see cref="TraceParent"/>.
/// </param>
public readonly record struct DomEvent(
	string CommandId,
	string EventName,
	string EventData,
	string? TraceParent = null,
	string? TraceState = null);

/// <summary>
/// Sends a text message to the client over the transport.
/// </summary>
/// <remarks>
/// <para>
/// Used for out-of-band server-to-client messages that are not DOM patches.
/// The primary use case is <see cref="NavigationCommand"/> execution: when the
/// MVU runtime produces a <see cref="NavigationCommand.Push"/> or
/// <see cref="NavigationCommand.Replace"/>, the server sends a text frame
/// instructing the client to call <c>history.pushState</c> or <c>replaceState</c>.
/// </para>
/// <para>
/// The text format is a JSON object with a <c>type</c> discriminator field.
/// Navigation messages use: <c>{"type":"navigate","action":"push|replace|back|forward|external","url":"..."}</c>
/// </para>
/// </remarks>
/// <param name="text">The text message to send.</param>
/// <returns>A task that completes when the data has been sent.</returns>
public delegate ValueTask SendText(string text);
