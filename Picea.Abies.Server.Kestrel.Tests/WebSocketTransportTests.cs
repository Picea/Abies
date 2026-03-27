// =============================================================================
// WebSocket Transport Tests — Unit Tests for the Transport Adapter
// =============================================================================
// Tests the WebSocketTransport adapter in isolation using in-memory WebSockets
// connected via anonymous pipes. Verifies:
//   1. SendPatches sends binary frames
//   2. ReceiveEvent parses JSON text frames into DomEvent
//   3. Close frame returns null (clean disconnect)
//   4. CloseAsync sends close frame
//
// Uses System.IO.Pipes.AnonymousPipeServerStream/AnonymousPipeClientStream
// for reliable bidirectional byte transport between two WebSocket instances.
// =============================================================================

using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Picea.Abies.Server.Kestrel.Tests;

public class WebSocketTransportTests
{
    // =========================================================================
    // SendPatches — Binary Frames
    // =========================================================================

    [Test]
    public async Task SendPatches_SendsBinaryFrame()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var sendPatches = transport.CreateSendPatches();

        var data = new byte[] { 1, 2, 3, 4, 5 };
        await sendPatches(data);

        var buffer = new byte[1024];
        var result = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);

        await Assert.That(result.MessageType).IsEqualTo(WebSocketMessageType.Binary);
        await Assert.That(result.EndOfMessage).IsTrue();
        await Assert.That(buffer[..result.Count]).IsEquivalentTo(data);
    }

    // =========================================================================
    // ReceiveEvent — JSON Text Frames
    // =========================================================================

    [Test]
    public async Task ReceiveEvent_ParsesJsonTextFrame()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        var json = """{ "commandId":"cmd-1","eventName":"click","eventData":"{}"}""";
        var bytes = Encoding.UTF8.GetBytes(json);
        await pair.Client.SendAsync(
            bytes, WebSocketMessageType.Text, true, CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNotNull();
        var e = domEvent!.Value; // Safe: IsNotNull asserted above
        await Assert.That(e.CommandId).IsEqualTo("cmd-1");
        await Assert.That(e.EventName).IsEqualTo("click");
        await Assert.That(e.EventData).IsEqualTo("{}");
    }

    [Test]
    public async Task ReceiveEvent_OptionalTraceparentProperty_DoesNotBreakDeserialization()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        var json = """{ "commandId":"cmd-trace","eventName":"click","eventData":"{}","traceparent":"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"}""";
        var bytes = Encoding.UTF8.GetBytes(json);
        await pair.Client.SendAsync(
            bytes, WebSocketMessageType.Text, true, CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNotNull();
        var e = domEvent!.Value;
        await Assert.That(e.CommandId).IsEqualTo("cmd-trace");
        await Assert.That(e.EventName).IsEqualTo("click");
        await Assert.That(e.EventData).IsEqualTo("{}");
    }

    [Test]
    public async Task ReceiveEvent_CloseFrame_ReturnsNull()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        await pair.Client.CloseOutputAsync(
            WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNull();
    }

    [Test]
    public async Task ReceiveEvent_NullEventData_DefaultsToEmptyString()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        var json = """{ "commandId":"cmd-2","eventName":"click","eventData":null}""";
        var bytes = Encoding.UTF8.GetBytes(json);
        await pair.Client.SendAsync(
            bytes, WebSocketMessageType.Text, true, CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNotNull();
        var e = domEvent!.Value; // Safe: IsNotNull asserted above
        await Assert.That(e.CommandId).IsEqualTo("cmd-2");
        await Assert.That(e.EventData).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ReceiveEvent_ParsesTraceContextFields()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        var json = """{ "commandId":"cmd-trace","eventName":"click","eventData":"{}","traceparent":"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01","tracestate":"vendor=value"}""";
        var bytes = Encoding.UTF8.GetBytes(json);
        await pair.Client.SendAsync(
            bytes, WebSocketMessageType.Text, true, CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNotNull();
        var e = domEvent!.Value;
        await Assert.That(e.TraceParent).IsEqualTo("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
        await Assert.That(e.TraceState).IsEqualTo("vendor=value");
    }

    [Test]
    public async Task ReceiveEvent_FragmentedTextMessage_ReassemblesBeforeDeserializing()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        var json = JsonSerializer.Serialize(new
        {
            commandId = "cmd-frag",
            eventName = "input",
            eventData = "{\"value\":\"abc\"}"
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        var split = bytes.Length / 2;

        await pair.Client.SendAsync(bytes.AsMemory(0, split), WebSocketMessageType.Text, endOfMessage: false, CancellationToken.None);
        await pair.Client.SendAsync(bytes.AsMemory(split), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNotNull();
        var e = domEvent!.Value;
        await Assert.That(e.CommandId).IsEqualTo("cmd-frag");
        await Assert.That(e.EventName).IsEqualTo("input");
        await Assert.That(e.EventData).IsEqualTo("{\"value\":\"abc\"}");
    }

    [Test]
    public async Task ReceiveEvent_LargePayloadOver4Kb_ParsesCorrectly()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var receiveEvent = transport.CreateReceiveEvent();

        var largeEventData = new string('x', 8192);
        var json = JsonSerializer.Serialize(new
        {
            commandId = "cmd-large",
            eventName = "input",
            eventData = largeEventData
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        await pair.Client.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);

        var domEvent = await receiveEvent(CancellationToken.None);

        await Assert.That(domEvent).IsNotNull();
        var e = domEvent!.Value;
        await Assert.That(e.CommandId).IsEqualTo("cmd-large");
        await Assert.That(e.EventData.Length).IsEqualTo(8192);
        await Assert.That(e.EventData).IsEqualTo(largeEventData);
    }

    // =========================================================================
    // CloseAsync — Graceful Shutdown
    // =========================================================================

    [Test]
    public async Task CloseAsync_SendsCloseFrame()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);

        // Server initiates close on a background task (CloseAsync blocks until client ACKs)
        var closeTask = Task.Run(async () => await transport.CloseAsync());

        // Client receives the close frame and responds
        var buffer = new byte[1024];
        var result = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);
        await Assert.That(result.MessageType).IsEqualTo(WebSocketMessageType.Close);

        await pair.Client.CloseOutputAsync(
            WebSocketCloseStatus.NormalClosure, "ack", CancellationToken.None);

        await closeTask;
    }

    [Test]
    public async Task SendPatches_EmptyData_Succeeds()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);
        var sendPatches = transport.CreateSendPatches();

        await sendPatches(ReadOnlyMemory<byte>.Empty);

        var buffer = new byte[1024];
        var result = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);

        await Assert.That(result.MessageType).IsEqualTo(WebSocketMessageType.Binary);
        await Assert.That(result.EndOfMessage).IsTrue();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SendOperations_ConcurrentCalls_DoNotRaceAndPreserveBoundaries()
    {
        await using var pair = WebSocketPair.Create();
        using var transport = new WebSocketTransport(pair.Server);

        var sendText = transport.CreateSendText();
        var sendPatches = transport.CreateSendPatches();

        var sends = new List<Task>
        {
            sendText("text-0").AsTask(),
            sendPatches(new byte[] { 1, 2, 3, 4 }).AsTask(),
            sendText("text-1").AsTask(),
            sendPatches(new byte[] { 9, 8, 7 }).AsTask()
        };

        await Task.WhenAll(sends);

        var buffer = new byte[1024];

        var result1 = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);
        await Assert.That(result1.MessageType).IsEqualTo(WebSocketMessageType.Text);
        await Assert.That(result1.EndOfMessage).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(buffer, 0, result1.Count)).IsEqualTo("text-0");

        var result2 = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);
        await Assert.That(result2.MessageType).IsEqualTo(WebSocketMessageType.Binary);
        await Assert.That(result2.EndOfMessage).IsTrue();
        await Assert.That(buffer[..result2.Count]).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });

        var result3 = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);
        await Assert.That(result3.MessageType).IsEqualTo(WebSocketMessageType.Text);
        await Assert.That(result3.EndOfMessage).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(buffer, 0, result3.Count)).IsEqualTo("text-1");

        var result4 = await pair.Client.ReceiveAsync(buffer, CancellationToken.None);
        await Assert.That(result4.MessageType).IsEqualTo(WebSocketMessageType.Binary);
        await Assert.That(result4.EndOfMessage).IsTrue();
        await Assert.That(buffer[..result4.Count]).IsEquivalentTo(new byte[] { 9, 8, 7 });
    }
}

// -- WebSocket Test Infrastructure ----

/// <summary>
/// Creates a connected pair of WebSockets using anonymous pipes.
/// </summary>
/// <remarks>
/// <para>
/// Uses two pairs of <see cref="AnonymousPipeServerStream"/>/<see cref="AnonymousPipeClientStream"/>
/// to create a full-duplex byte transport. The WebSocket protocol framing is handled
/// by <see cref="WebSocket.CreateFromStream"/>.
/// </para>
/// <para>
/// Pipe A: Client writes, Server reads.
/// Pipe B: Server writes, Client reads.
/// </para>
/// </remarks>
internal sealed class WebSocketPair : IAsyncDisposable
{
    public WebSocket Client { get; }
    public WebSocket Server { get; }

    private readonly Stream _clientWriteStream;
    private readonly Stream _clientReadStream;
    private readonly Stream _serverWriteStream;
    private readonly Stream _serverReadStream;

    private WebSocketPair(
        WebSocket client, WebSocket server,
        Stream clientWriteStream, Stream clientReadStream,
        Stream serverWriteStream, Stream serverReadStream)
    {
        Client = client;
        Server = server;
        _clientWriteStream = clientWriteStream;
        _clientReadStream = clientReadStream;
        _serverWriteStream = serverWriteStream;
        _serverReadStream = serverReadStream;
    }

    /// <summary>
    /// Creates a connected WebSocket pair using anonymous pipes for transport.
    /// </summary>
    public static WebSocketPair Create()
    {
        // Pipe A: client -> server
        var pipeAServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
        var pipeAClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeAServer.ClientSafePipeHandle);

        // Pipe B: server -> client
        var pipeBServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
        var pipeBClient = new AnonymousPipeClientStream(PipeDirection.In, pipeBServer.ClientSafePipeHandle);

        // Client: reads from pipe B, writes to pipe A
        var clientStream = new BidirectionalStream(pipeBClient, pipeAClient);

        // Server: reads from pipe A, writes to pipe B
        var serverStream = new BidirectionalStream(pipeAServer, pipeBServer);

        var client = WebSocket.CreateFromStream(
            clientStream,
            new WebSocketCreationOptions { IsServer = false });

        var server = WebSocket.CreateFromStream(
            serverStream,
            new WebSocketCreationOptions { IsServer = true });

        return new WebSocketPair(
            client, server,
            pipeAClient, pipeBClient,
            pipeBServer, pipeAServer);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        Server.Dispose();
        await _clientWriteStream.DisposeAsync();
        await _clientReadStream.DisposeAsync();
        await _serverWriteStream.DisposeAsync();
        await _serverReadStream.DisposeAsync();
    }
}

/// <summary>
/// Combines a read-only stream and a write-only stream into a single
/// bidirectional stream suitable for <see cref="WebSocket.CreateFromStream"/>.
/// </summary>
internal sealed class BidirectionalStream(Stream readStream, Stream writeStream) : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => writeStream.Flush();
    public override Task FlushAsync(CancellationToken ct) => writeStream.FlushAsync(ct);

    public override int Read(byte[] buffer, int offset, int count) =>
        readStream.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        readStream.ReadAsync(buffer, offset, count, ct);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
        readStream.ReadAsync(buffer, ct);

    public override void Write(byte[] buffer, int offset, int count) =>
        writeStream.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        writeStream.WriteAsync(buffer, offset, count, ct);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) =>
        writeStream.WriteAsync(buffer, ct);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            readStream.Dispose();
            writeStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
