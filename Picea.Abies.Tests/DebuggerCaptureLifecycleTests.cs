using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

public sealed class DebuggerCaptureLifecycleTests
{
    [Test]
    public async Task CaptureHookNoOp_WhenDebuggerNotRegistered()
    {
        var previous = DebuggerRuntimeRegistry.CurrentDebugger;
        DebuggerRuntimeRegistry.CurrentDebugger = null;

        HandlerRegistry.CaptureMessageToDebugger(new TestMessage("Increment", []), "{}");

        await Assert.That(DebuggerRuntimeRegistry.CurrentDebugger).IsNull();

        DebuggerRuntimeRegistry.CurrentDebugger = previous;
    }

    [Test]
    public async Task CaptureEnabledWhenDebuggerRegistered()
    {
        var debugger = new DebuggerMachine(capacity: 1000);
        debugger.CaptureMessage(new TestMessage("Increment", [5]), "{\"count\":5}");
        debugger.CaptureMessage(new TestMessage("Reset", []), "{\"count\":0}");

        await Assert.That(debugger.Timeline.Count).IsEqualTo(2);

        var first = debugger.Timeline[0];
        await Assert.That(first.MessageType).IsEqualTo(nameof(TestMessage));
        await Assert.That(first.ArgsPreview).Contains("Increment");

        var second = debugger.Timeline[1];
        await Assert.That(second.ArgsPreview).Contains("Reset");

    }

    [Test]
    public async Task CaptureToggleOnRuntime_EnablesAndDisablesCapture()
    {
        var debugger = new DebuggerMachine(capacity: 1000);
        debugger.CaptureMessage(new TestMessage("Phase1_Msg1", []), "{}");
        debugger.CaptureMessage(new TestMessage("Phase1_Msg2", []), "{}");
        debugger.CaptureMessage(new TestMessage("Phase1_Msg3", []), "{}");

        await Assert.That(debugger.Timeline.Count).IsEqualTo(3);

        debugger.DisableCapture();

        debugger.CaptureMessage(new TestMessage("Phase2_Msg1", []), "{}");
        debugger.CaptureMessage(new TestMessage("Phase2_Msg2", []), "{}");

        await Assert.That(debugger.Timeline.Count).IsEqualTo(3);

        debugger.EnableCapture();
        debugger.CaptureMessage(new TestMessage("Phase3_Msg1", []), "{}");

        await Assert.That(debugger.Timeline.Count).IsEqualTo(4);
        await Assert.That(debugger.Timeline[3].ArgsPreview).Contains("Phase3_Msg1");
    }

    [Test]
    public async Task CaptureMessageArgs_SerializesComplexObjectsCorrectly()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        var complexArg = new { id = 123, nested = new { name = "test", value = 45.6 } };
        debugger.CaptureMessage(new TestMessage("ComplexAction", [complexArg, "string", 789]), "{}");

        await Assert.That(debugger.Timeline.Count).IsEqualTo(1);

        var entry = debugger.Timeline[0];
        await Assert.That(entry.ArgsPreview).IsNotEmpty();
        await Assert.That(entry.ArgsPreview).Contains("ComplexAction");

    }

    private sealed record TestMessage(string Type, object?[] Args) : Message;
}
