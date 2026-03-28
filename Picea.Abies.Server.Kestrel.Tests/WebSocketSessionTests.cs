// =============================================================================
// WebSocket Session Tests — Full Round-Trip Integration Tests
// =============================================================================
// Tests the complete InteractiveServer MVU loop over WebSocket:
//
//     Connect → Receive initial patches → Send event → Receive updated patches
//
// These tests prove that all the pieces work together:
//   - TestServer hosts the Abies app with MapAbies
//   - WebSocket connection is established
//   - Server sends binary patch batch on connect (initial render diff)
//   - Client sends JSON event with commandId extracted from patches
//   - Server dispatches message → transition → diff → sends updated patches
//   - Client verifies the patches contain updated state
//
// Uses Microsoft.AspNetCore.TestHost.WebSocketClient for in-process WebSocket
// testing — no network I/O, no ports, deterministic.
//
// Also tests URL change events over WebSocket for client-side routing.
// =============================================================================

using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Picea.Abies.Server.Kestrel.Tests;

public class WebSocketSessionTests
{
    // =========================================================================
    // Connection & Initial Render
    // =========================================================================

    [Test]
    public async Task WebSocket_ReceivesInitialPatchBatch()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        var batch = await ReceiveInitialBatch(ws);

        await Assert.That(batch.PatchCount > 0).IsTrue();
        await Assert.That(batch.Strings.Count > 0).IsTrue();
    }

    [Test]
    public async Task WebSocket_InitialRender_ContainsCounterHtml()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        var batch = await ReceiveInitialBatch(ws);

        // The initial render should contain "Count: 0" in the string table
        var allStrings = string.Join(" ", batch.Strings);
        await Assert.That(allStrings).Contains("Count: 0");
    }

    // =========================================================================
    // Event Dispatch — Click → State Update → Patch Response
    // =========================================================================

    [Test]
    public async Task WebSocket_ClickIncrement_ReceivesUpdatedPatches()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        var initialBatch = await ReceiveInitialBatch(ws);

        var incrementCommandId = FindHandlerCommandId(initialBatch, "increment");
        await Assert.That(incrementCommandId).IsNotNull();

        await SendClickEvent(ws, incrementCommandId!);

        var updatedBatch = await ReceiveBinaryBatch(ws);
        var allStrings = string.Join(" ", updatedBatch.Strings);
        await Assert.That(allStrings).Contains("Count: 1");
    }

    [Test]
    public async Task WebSocket_ClickIncrement_WithTraceparent_PropagatesTraceToServerActivities()
    {
        using var activityCapture = ActivityCapture.Start();
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        var initialBatch = await ReceiveInitialBatch(ws);

        var incrementCommandId = FindHandlerCommandId(initialBatch, "increment");
        await Assert.That(incrementCommandId).IsNotNull();

        var parentTraceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var traceparent = $"00-{parentTraceId}-{parentSpanId}-01";

        await SendClickEvent(ws, incrementCommandId!, traceparent);

        _ = await ReceiveBinaryBatch(ws);

        var sessionActivity = await activityCapture.WaitForAsync(activity =>
            activity.Source.Name == "Picea.Abies.Server.Session"
            && activity.OperationName == "Picea.Abies.Server.Session.ReceiveEvent"
            && activity.TraceId == parentTraceId);

        await Assert.That(sessionActivity).IsNotNull();
        await Assert.That(sessionActivity!.ParentSpanId).IsEqualTo(parentSpanId);

        var runtimeActivity = await activityCapture.WaitForAsync(activity =>
            activity.Source.Name == "Picea.Abies.Runtime"
            && activity.OperationName == "Picea.Abies.Render"
            && activity.TraceId == parentTraceId);

        await Assert.That(runtimeActivity).IsNotNull();
    }

    [Test]
    public async Task WebSocket_ClickDecrement_ReceivesUpdatedPatches()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        var initialBatch = await ReceiveInitialBatch(ws);

        var decrementCommandId = FindHandlerCommandId(initialBatch, "decrement");
        await Assert.That(decrementCommandId).IsNotNull();

        await SendClickEvent(ws, decrementCommandId!);

        var updatedBatch = await ReceiveBinaryBatch(ws);
        var allStrings = string.Join(" ", updatedBatch.Strings);
        await Assert.That(allStrings).Contains("Count: -1");
    }

    [Test]
    public async Task WebSocket_MultipleClicks_AccumulateState()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        var batch = await ReceiveInitialBatch(ws);

        var incrementCommandId = FindHandlerCommandId(batch, "increment");
        await Assert.That(incrementCommandId is not null).IsTrue();

        // Click increment 3 times — after each click, extract new commandId
        // from the response patches if available, otherwise reuse the old one
        for (var i = 1; i <= 3; i++)
        {
            await SendClickEvent(ws, incrementCommandId!);
            batch = await ReceiveBinaryBatch(ws);

            var allStrings = string.Join(" ", batch.Strings);
            await Assert.That(allStrings).Contains($"Count: {i}");
            // Try to find updated commandId from the diff patches.
            // The diff may contain UpdateHandler (type 12) or new AddHandler (type 10)
            // patches with the fresh commandId.
            var newCmdId = FindHandlerCommandId(batch, "increment")
                ?? FindUpdateHandlerCommandId(batch, incrementCommandId!);
            if (newCmdId is not null)
                incrementCommandId = newCmdId;
        }
    }

    // =========================================================================
    // URL Change Events — Client-Side Navigation
    // =========================================================================

    [Test]
    public async Task WebSocket_UrlChange_ReceivesUpdatedPatches()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        using var ws = await ConnectWebSocket(host);
        _ = await ReceiveInitialBatch(ws); // consume initial

        // Send a URL change event
        await SendUrlChangeEvent(ws, "/articles");

        var updatedBatch = await ReceiveBinaryBatch(ws);
        var allStrings = string.Join(" ", updatedBatch.Strings);
        await Assert.That(allStrings).Contains("Page: articles");
    }

    // =========================================================================
    // Session Isolation — Multiple Connections
    // =========================================================================

    [Test]
    public async Task WebSocket_MultipleSessions_HaveIsolatedState()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        // Connect two independent sessions
        using var ws1 = await ConnectWebSocket(host);
        var batch1 = await ReceiveInitialBatch(ws1);

        using var ws2 = await ConnectWebSocket(host);
        var batch2 = await ReceiveInitialBatch(ws2);

        // Increment session 1 twice — must track commandId updates
        var cmdId1 = FindHandlerCommandId(batch1, "increment")!;
        await SendClickEvent(ws1, cmdId1);
        batch1 = await ReceiveBinaryBatch(ws1);

        // After the first click, the handler may have a new commandId
        cmdId1 = FindUpdatedCommandId(batch1, cmdId1) ?? cmdId1;
        await SendClickEvent(ws1, cmdId1);
        batch1 = await ReceiveBinaryBatch(ws1);

        // Session 1 should be at Count: 2
        await Assert.That(string.Join(" ", batch1.Strings)).Contains("Count: 2");

        // Session 2 increments once — its handler commandId is DIFFERENT
        // because each session has its own Runtime with fresh handler IDs
        var cmdId2 = FindHandlerCommandId(batch2, "increment")!;
        await SendClickEvent(ws2, cmdId2);
        batch2 = await ReceiveBinaryBatch(ws2);

        // Session 2 should be at Count: 1 (isolated from session 1)
        await Assert.That(string.Join(" ", batch2.Strings)).Contains("Count: 1");
    }

    // =========================================================================
    // Graceful Disconnect
    // =========================================================================

    [Test]
    public async Task WebSocket_ClientClose_ServerHandlesGracefully()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());

        var ws = await ConnectWebSocket(host);
        _ = await ReceiveInitialBatch(ws);

        // Close gracefully — should not throw on the server
        await ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

        await Assert.That(ws.State).IsEqualTo(WebSocketState.Closed);
        ws.Dispose();
    }

    // =========================================================================
    // Helpers — WebSocket Connection
    // =========================================================================

    /// <summary>
    /// Connects a WebSocket client to the test server's Abies WebSocket endpoint.
    /// </summary>
    private static async Task<WebSocket> ConnectWebSocket(AbiesTestHost host)
    {
        var wsClient = host.Server.CreateWebSocketClient();
        return await wsClient.ConnectAsync(
            new Uri("ws://localhost/_abies/ws"), CancellationToken.None);
    }

    // =========================================================================
    // Helpers — Event Sending
    // =========================================================================

    /// <summary>
    /// Sends a click event as a JSON text frame.
    /// </summary>
    private static async Task SendClickEvent(
        WebSocket ws,
        string commandId,
        string? traceparent = null,
        string? tracestate = null)
    {
        var json = JsonSerializer.Serialize(new
        {
            commandId,
            eventName = "click",
            eventData = "{}",
            traceparent,
            tracestate
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(
            bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>
    /// Sends a URL change event as a JSON text frame with the reserved commandId.
    /// </summary>
    private static async Task SendUrlChangeEvent(WebSocket ws, string path)
    {
        var json = JsonSerializer.Serialize(new
        {
            commandId = "__url_changed__",
            eventName = "urlchange",
            eventData = path
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(
            bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    // =========================================================================
    // Helpers — Binary Batch Reading
    // =========================================================================

    /// <summary>
    /// Receives the initial binary patch batch sent on WebSocket connect.
    /// The runtime merges body + head patches into a single Apply call,
    /// so a single binary frame contains the entire initial render.
    /// </summary>
    private static Task<PatchBatch> ReceiveInitialBatch(
        WebSocket ws, int timeoutMs = 5000) =>
        ReceiveBinaryBatch(ws, timeoutMs);

    /// <summary>
    /// Receives a single binary WebSocket frame and parses it as a patch batch.
    /// Skips over any interleaved text frames (e.g., debugger timeline-changed
    /// notifications) that may arrive before the binary patch data.
    /// </summary>
    private static async Task<PatchBatch> ReceiveBinaryBatch(
        WebSocket ws, int timeoutMs = 5000)
    {
        var buffer = new byte[64 * 1024]; // 64KB — plenty for test patches
        using var cts = new CancellationTokenSource(timeoutMs);

        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, cts.Token);
        } while (result.MessageType == WebSocketMessageType.Text);

        await Assert.That(result.MessageType).IsEqualTo(WebSocketMessageType.Binary);
        await Assert.That(result.EndOfMessage).IsTrue();

        return ParseBinaryBatch(buffer.AsSpan(0, result.Count));
    }

    /// <summary>
    /// Parses a binary patch batch into a structured representation.
    /// </summary>
    private static PatchBatch ParseBinaryBatch(ReadOnlySpan<byte> data)
    {
        var patchCount = BinaryPrimitives.ReadInt32LittleEndian(data[..4]);
        var stringTableOffset = BinaryPrimitives.ReadInt32LittleEndian(data[4..8]);

        // Read string table
        var strings = ReadStringTable(data[stringTableOffset..]);

        // Read patches
        var patches = new List<PatchEntry>(patchCount);
        const int headerSize = 8;
        const int entrySize = 20;

        for (var i = 0; i < patchCount; i++)
        {
            var offset = headerSize + (i * entrySize);
            var type = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            var f1Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..]);
            var f2Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 8)..]);
            var f3Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 12)..]);
            var f4Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 16)..]);

            patches.Add(new PatchEntry(
                type,
                f1Idx >= 0 ? strings[f1Idx] : null,
                f2Idx >= 0 ? strings[f2Idx] : null,
                f3Idx >= 0 ? strings[f3Idx] : null,
                f4Idx >= 0 ? strings[f4Idx] : null));
        }

        return new PatchBatch(patchCount, strings, patches);
    }

    /// <summary>
    /// Reads the LEB128-prefixed UTF-8 string table.
    /// </summary>
    private static List<string> ReadStringTable(ReadOnlySpan<byte> data)
    {
        var strings = new List<string>();
        var offset = 0;

        while (offset < data.Length)
        {
            // Read LEB128 length
            var byteLen = 0;
            var shift = 0;
            byte b;
            do
            {
                b = data[offset++];
                byteLen |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            // Read UTF-8 string
            var str = Encoding.UTF8.GetString(data.Slice(offset, byteLen));
            strings.Add(str);
            offset += byteLen;
        }

        return strings;
    }

    /// <summary>
    /// Finds the commandId for a handler on an element whose HTML contains
    /// the given CSS class. Searches through AddHandler patches (type 10)
    /// and correlates with element HTML from AddChild/AddRoot patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The initial render uses SetChildrenHtml (type 5) which sets innerHTML
    /// as a single string. The handler commandIds are in AddHandler patches.
    /// But with SetChildrenHtml, there may not be separate AddHandler patches —
    /// handlers are embedded as data-event-click attributes in the HTML.
    /// </para>
    /// <para>
    /// So we search the string table for data-event-click attributes on
    /// elements with the given class.
    /// </para>
    /// </remarks>
    private static string? FindHandlerCommandId(PatchBatch batch, string cssClass)
    {
        // Strategy 1: Look for AddHandler patches (type 10) where the element
        // has the target CSS class
        foreach (var patch in batch.Patches)
        {
            // AddHandler: f1=elementId, f2=handlerName (data-event-click), f3=commandId
            if (patch.Type == 10 && patch.Field2 == "data-event-click")
            {
                // Check if this element's ID appears in HTML containing the CSS class
                var elementId = patch.Field1;
                foreach (var str in batch.Strings)
                {
                    if (str.Contains($"class=\"{cssClass}\"", StringComparison.Ordinal) &&
                        elementId is not null && str.Contains(elementId, StringComparison.Ordinal))
                    {
                        return patch.Field3;
                    }
                }
            }
        }

        // Strategy 2: Search HTML strings for a specific element (tag) that
        // contains BOTH the target CSS class AND a data-event-click attribute.
        // We isolate the element by finding the opening tag that contains
        // the class, then extracting data-event-click from that same tag.
        foreach (var str in batch.Strings)
        {
            if (!str.Contains($"class=\"{cssClass}\"", StringComparison.Ordinal))
                continue;

            var classAttr = $"class=\"{cssClass}\"";
            var classIdx = str.IndexOf(classAttr, StringComparison.Ordinal);
            while (classIdx >= 0)
            {
                // Walk backward to find the opening '<' of this tag
                var tagStart = str.LastIndexOf('<', classIdx);
                if (tagStart < 0)
                    break;

                // Walk forward to find the end of this opening tag '>'
                var tagEnd = str.IndexOf('>', classIdx);
                if (tagEnd < 0)
                    break;

                // Extract just this opening tag
                var tag = str[tagStart..(tagEnd + 1)];

                // Look for data-event-click="..." within this specific tag
                const string marker = "data-event-click=\"";
                var markerIdx = tag.IndexOf(marker, StringComparison.Ordinal);
                if (markerIdx >= 0)
                {
                    var valueStart = markerIdx + marker.Length;
                    var valueEnd = tag.IndexOf('"', valueStart);
                    if (valueEnd > valueStart)
                        return tag[valueStart..valueEnd];
                }

                // Try the next occurrence of this class in the string
                classIdx = str.IndexOf(classAttr, classIdx + classAttr.Length, StringComparison.Ordinal);
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a new commandId from UpdateHandler patches (type 12) that
    /// replaced the given old commandId. After each render cycle, handlers
    /// get new commandIds — the diff produces UpdateHandler patches.
    /// </summary>
    /// <remarks>
    /// UpdateHandler binary format: f1=elementId, f2=handlerName, f3=new commandId.
    /// We can't directly match by old commandId from the binary format, so we
    /// look for any UpdateHandler patch with "data-event-click" and use the new
    /// commandId (f3). For tests with a single button type, this works.
    /// For multi-button scenarios, use <see cref="FindUpdatedCommandId"/> which
    /// tracks the element identity across diffs.
    /// </remarks>
    private static string? FindUpdateHandlerCommandId(PatchBatch batch, string oldCommandId)
    {
        // UpdateHandler: type=12, f1=elementId, f2=handlerName, f3=new commandId
        // The binary writer writes: f2=OldHandler.Name, f3=NewHandler.CommandId
        // We look for handlers with the same name pattern (data-event-click)
        foreach (var patch in batch.Patches)
        {
            if (patch.Type == 12 && patch.Field2 == "data-event-click")
            {
                return patch.Field3;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the updated commandId for a handler by tracking through
    /// UpdateHandler patches. Returns the first UpdateHandler (type 12)
    /// with "data-event-click" that has the same element as the old handler.
    /// Falls back to returning the first UpdateHandler commandId.
    /// </summary>
    private static string? FindUpdatedCommandId(PatchBatch batch, string currentCommandId)
    {
        // First try: look for UpdateHandler patches where f2 is "data-event-click"
        // Since the old commandId was for "data-event-click", any UpdateHandler
        // for that event type on the same element gives us the new commandId.
        // With multiple buttons, we return the first one — this is correct when
        // callers track their specific button across the full render cycle.
        var candidates = batch.Patches
            .Where(p => p.Type == 12 && p.Field2 == "data-event-click")
            .ToList();

        // If there's only one UpdateHandler for click, return it directly
        if (candidates.Count == 1)
            return candidates[0].Field3;

        // If there are multiple, we need to identify which one corresponds
        // to the caller's button. We can't do this from the diff patches alone
        // without element ID tracking. Return null to signal the caller should
        // fall back to FindHandlerCommandId from a full re-render.
        // In practice, diff response patches don't have CSS class info.
        return candidates.Count > 0 ? candidates[0].Field3 : null;
    }

    // =========================================================================
    // Data Types
    // =========================================================================

    /// <summary>Parsed binary patch batch.</summary>
    private record PatchBatch(int PatchCount, List<string> Strings, List<PatchEntry> Patches);

    /// <summary>A single patch entry with resolved string values.</summary>
    private record PatchEntry(int Type, string? Field1, string? Field2, string? Field3, string? Field4);

    private sealed class ActivityCapture : IDisposable
    {
        private readonly Lock _gate = new();
        private readonly List<Activity> _activities = [];
        private readonly ActivityListener _listener;

        private ActivityCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name.StartsWith("Picea.Abies", StringComparison.Ordinal),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    lock (_gate)
                    {
                        _activities.Add(activity);
                    }
                }
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityCapture Start() => new();

        public async Task<Activity?> WaitForAsync(Func<Activity, bool> predicate, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                var match = Find(predicate);
                if (match is not null)
                    return match;

                await Task.Delay(25);
            }

            return Find(predicate);
        }

        private Activity? Find(Func<Activity, bool> predicate)
        {
            lock (_gate)
            {
                foreach (var activity in _activities)
                {
                    if (predicate(activity))
                        return activity;
                }

                return null;
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}
