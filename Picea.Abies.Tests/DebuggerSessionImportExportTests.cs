using System.Text.Json;
using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

public sealed class DebuggerSessionImportExportTests
{
    [Test]
    public async Task ImportSession_Succeeds_WhenAppAndVersionMatch()
    {
        var appIdentity = new DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Conduit.Wasm",
            AppVersion = "1.2.3"
        };

        var sourceDebugger = new DebuggerMachine(capacity: 16);
        sourceDebugger.CaptureInitialModel("{\"count\":0}");
        sourceDebugger.CaptureMessage(new TestMessage { Type = "Increment" }, "{\"count\":1}", patchCount: 2);
        sourceDebugger.CaptureMessage(new TestMessage { Type = "Increment" }, "{\"count\":2}", patchCount: 3);

        var exportResponse = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage { Type = "export-session" },
            sourceDebugger,
            appIdentity);

        var importTarget = new DebuggerMachine(capacity: 16);

        var importResponse = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage
            {
                Type = "import-session",
                Data = new DebuggerSessionImportRequest { Session = exportResponse.Session }
            },
            importTarget,
            appIdentity);

        await Assert.That(importResponse.Status).IsNotEqualTo("error");
        await Assert.That(importResponse.Error).IsNull();
        await Assert.That(importTarget.Timeline.Count).IsEqualTo(2);
        await Assert.That(importTarget.CursorPosition).IsEqualTo(1);
        await Assert.That(importTarget.Timeline[0].MessageType).IsEqualTo("TestMessage");
        await Assert.That(importTarget.Timeline[0].PatchCount).IsEqualTo(2);
    }

    [Test]
    public async Task ImportSession_Fails_WhenAppDiffers()
    {
        var expectedIdentity = new DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Conduit.Wasm",
            AppVersion = "1.2.3"
        };

        var session = CreateSession(
            appName: "Picea.Abies.Counter.Wasm",
            appVersion: "1.2.3");

        var targetDebugger = new DebuggerMachine(capacity: 8);

        var response = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage
            {
                Type = "import-session",
                Data = new DebuggerSessionImportRequest { Session = session }
            },
            targetDebugger,
            expectedIdentity);

        await Assert.That(response.Status).IsEqualTo("error");
        await Assert.That(response.Error).Contains("app mismatch");
        await Assert.That(targetDebugger.Timeline.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ImportSession_Fails_WhenVersionDiffers()
    {
        var expectedIdentity = new DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Conduit.Wasm",
            AppVersion = "1.2.3"
        };

        var session = CreateSession(
            appName: "Picea.Abies.Conduit.Wasm",
            appVersion: "2.0.0");

        var targetDebugger = new DebuggerMachine(capacity: 8);

        var response = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage
            {
                Type = "import-session",
                Data = new DebuggerSessionImportRequest { Session = session }
            },
            targetDebugger,
            expectedIdentity);

        await Assert.That(response.Status).IsEqualTo("error");
        await Assert.That(response.Error).Contains("version mismatch");
        await Assert.That(targetDebugger.Timeline.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ImportSession_Fails_WhenPayloadMalformed()
    {
        var expectedIdentity = new DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Conduit.Wasm",
            AppVersion = "1.2.3"
        };

        var targetDebugger = new DebuggerMachine(capacity: 8);

        var response = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage
            {
                Type = "import-session",
                Data = new { unexpected = true }
            },
            targetDebugger,
            expectedIdentity);

        await Assert.That(response.Status).IsEqualTo("error");
        await Assert.That(response.Error).Contains("malformed payload");
        await Assert.That(targetDebugger.Timeline.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ImportSession_Succeeds_WhenPayloadComesFromJsonElement()
    {
        var appIdentity = new DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Conduit.Wasm",
            AppVersion = "1.2.3"
        };

        var sourceDebugger = new DebuggerMachine(capacity: 16);
        sourceDebugger.CaptureInitialModel("{\"count\":0}");
        sourceDebugger.CaptureMessage(new TestMessage { Type = "Increment" }, "{\"count\":1}", patchCount: 1);

        var exportResponse = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage { Type = "export-session" },
            sourceDebugger,
            appIdentity);

        var importEnvelope = new DebuggerAdapterMessage
        {
            Type = "import-session",
            Data = new DebuggerSessionImportRequest { Session = exportResponse.Session }
        };

        var envelopeJson = JsonSerializer.Serialize(
            importEnvelope,
            DebuggerAdapterJsonContext.Default.DebuggerAdapterMessage);

        var importViaJsonElement = JsonSerializer.Deserialize(
            envelopeJson,
            DebuggerAdapterJsonContext.Default.DebuggerAdapterMessage);

        var importTarget = new DebuggerMachine(capacity: 16);
        var importResponse = DebuggerRuntimeBridge.Execute(
            importViaJsonElement!,
            importTarget,
            appIdentity);

        await Assert.That(importResponse.Status).IsNotEqualTo("error");
        await Assert.That(importResponse.Error).IsNull();
        await Assert.That(importTarget.Timeline.Count).IsEqualTo(1);
        await Assert.That(importTarget.CursorPosition).IsEqualTo(0);
    }

    [Test]
    public async Task BridgeResponse_PropagatesRuntimeMetadata_ForJsCompatibilityChecks()
    {
        var appIdentity = new DebuggerAppIdentity
        {
            AppName = "Picea.Abies.Conduit.Wasm",
            AppVersion = "1.2.3"
        };

        var debugger = new DebuggerMachine(capacity: 8);
        debugger.CaptureInitialModel("{\"count\":0}");

        var response = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage { Type = "get-timeline" },
            debugger,
            appIdentity);

        await Assert.That(response.AppName).IsEqualTo(appIdentity.AppName);
        await Assert.That(response.AppVersion).IsEqualTo(appIdentity.AppVersion);
    }

    [Test]
    public async Task DebuggerMachine_RaisesTimelineChanged_WhenCapturingMessage()
    {
        var debugger = new DebuggerMachine(capacity: 8);
        var raised = false;
        debugger.TimelineChanged += () => raised = true;

        debugger.CaptureInitialModel("{\"count\":0}");
        debugger.CaptureMessage(new TestMessage { Type = "Increment" }, "{\"count\":1}");

        await Assert.That(raised).IsTrue();
    }

    private static DebuggerAdapterSession CreateSession(string appName, string appVersion) =>
        new()
        {
            App = new DebuggerAppIdentity
            {
                AppName = appName,
                AppVersion = appVersion
            },
            Status = "paused",
            CursorPosition = 0,
            InitialModelSnapshotPreview = "{\"count\":0}",
            TimelineEntries =
            [
                new DebuggerAdapterTimelineEntry
                {
                    Sequence = 0,
                    MessageType = "TestMessage",
                    ArgsPreview = "{}",
                    Timestamp = 1,
                    PatchCount = 0,
                    ModelSnapshotPreview = "{\"count\":1}"
                }
            ]
        };
}
