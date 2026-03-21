// =============================================================================
// WebSocketTransport — Adapts System.Net.WebSockets to Abies Transport Delegates
// =============================================================================
// Maps a raw WebSocket to the SendPatches and ReceiveEvent delegates
// defined in Picea.Abies.Server.Transport. This is the "adapter" in the
// Ports & Adapters architecture — the port is the delegate pair,
// the adapter is this class.
//
// Binary protocol (Server → Client):
//   WebSocket binary frames containing the same binary batch format
//   as the WASM interop (RenderBatchWriter output). The client-side
//   abies-server.js parses these using the same DataView-based reader.
//
// Event protocol (Client → Server):
//   WebSocket text frames containing JSON:
//     { "commandId": "...", "eventName": "...", "eventData": "..." }
//
// The transport is stateless — it adapts a single WebSocket connection.
// One WebSocketTransport per Session per client connection.
//
// See also:
//   - Picea.Abies.Server/Transport.cs — delegate type definitions
//   - Picea.Abies.Server/Session.cs — consumes these delegates
//   - Endpoints.cs — creates WebSocketTransport per connection
// =============================================================================

using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Picea.Abies.Server.Kestrel;

/// <summary>
/// Adapts a <see cref="WebSocket"/> to the Abies transport delegate pair.
/// </summary>
/// <remarks>
/// <para>
/// This is a thin adapter — it translates between the raw WebSocket
/// binary/text frames and the typed <see cref="SendPatches"/>/<see cref="ReceiveEvent"/>
/// delegates that <see cref="Session"/> expects.
/// </para>
/// <para>
/// Each client connection gets its own <see cref="WebSocketTransport"/>
/// instance, which lives for the duration of the WebSocket connection.
/// </para>
/// </remarks>
public sealed class WebSocketTransport : IDisposable
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Server.Kestrel.WebSocketTransport");

    private readonly WebSocket _webSocket;
    private bool _disposed;

    /// <summary>
    /// Creates a new transport adapter for the given WebSocket.
    /// </summary>
    /// <param name="webSocket">The WebSocket connection to adapt.</param>
    public WebSocketTransport(WebSocket webSocket) =>
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));

    /// <summary>
    /// Returns a <see cref="SendPatches"/> delegate bound to this WebSocket.
    /// Sends binary patch batches as WebSocket binary frames.
    /// </summary>
    public SendPatches CreateSendPatches() => SendPatchesImpl;

    /// <summary>
    /// Returns a <see cref="ReceiveEvent"/> delegate bound to this WebSocket.
    /// Receives DOM events as WebSocket text frames containing JSON.
    /// </summary>
    public ReceiveEvent CreateReceiveEvent() => ReceiveEventImpl;

    /// <summary>
    /// Returns a <see cref="SendText"/> delegate bound to this WebSocket.
    /// Sends text messages (e.g., navigation commands) as WebSocket text frames.
    /// </summary>
    public SendText CreateSendText() => SendTextImpl;

    /// <summary>
    /// Sends a binary patch batch over the WebSocket as a binary frame.
    /// </summary>
    private async ValueTask SendPatchesImpl(ReadOnlyMemory<byte> patchBatch)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.WebSocket.SendPatches");
        activity?.SetTag("abies.patchBatch.size", patchBatch.Length);

        if (_webSocket.State is not WebSocketState.Open)
            return;

        await _webSocket.SendAsync(
            patchBatch,
            WebSocketMessageType.Binary,
            endOfMessage: true,
            CancellationToken.None);

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Sends a text message over the WebSocket as a text frame.
    /// Used for out-of-band server-to-client messages (e.g., navigation commands).
    /// </summary>
    private async ValueTask SendTextImpl(string text)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.WebSocket.SendText");
        activity?.SetTag("abies.text.length", text.Length);

        if (_webSocket.State is not WebSocketState.Open)
            return;

        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        await _webSocket.SendAsync(
            bytes.AsMemory(),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Receives the next DOM event from the WebSocket.
    /// Returns <c>null</c> when the client disconnects.
    /// </summary>
    private async ValueTask<DomEvent?> ReceiveEventImpl(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.WebSocket.ReceiveEvent");

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            var result = await _webSocket.ReceiveAsync(
                buffer.AsMemory(),
                cancellationToken);

            // Client sent close frame
            if (result.MessageType is WebSocketMessageType.Close)
            {
                activity?.SetTag("abies.event", "close");
                await _webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Session ended",
                    CancellationToken.None);
                return null;
            }

            // We expect text frames with JSON event data
            if (result.MessageType is not WebSocketMessageType.Text)
            {
                activity?.SetTag("abies.event", "unexpected_binary");
                return null;
            }

            var json = buffer.AsSpan(0, result.Count);
            var domEvent = JsonSerializer.Deserialize<DomEventDto>(json);

            if (domEvent is null)
                return null;

            activity?.SetTag("abies.event.commandId", domEvent.CommandId);
            activity?.SetTag("abies.event.name", domEvent.EventName);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new DomEvent(
                domEvent.CommandId ?? string.Empty,
                domEvent.EventName ?? string.Empty,
                domEvent.EventData ?? string.Empty);
        }
        catch (WebSocketException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during graceful shutdown
            return null;
        }
        catch (WebSocketException)
        {
            // Connection dropped
            activity?.SetStatus(ActivityStatusCode.Error, "WebSocket connection lost");
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Closes the WebSocket connection gracefully.
    /// </summary>
    public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Session ended",
                cancellationToken);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _webSocket.Dispose();
    }

    /// <summary>
    /// DTO for deserializing DOM events from WebSocket text frames.
    /// </summary>
    /// <remarks>
    /// Uses camelCase property names to match the JavaScript client convention.
    /// </remarks>
    private sealed record DomEventDto(
        [property: JsonPropertyName("commandId")] string? CommandId,
        [property: JsonPropertyName("eventName")] string? EventName,
        [property: JsonPropertyName("eventData")] string? EventData);
}
