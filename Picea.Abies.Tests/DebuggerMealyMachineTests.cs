using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

public sealed class DebuggerMealyMachineTests
{
    [Test]
    public async Task RecordMessageAndTransitionToRecording_WhenFirstMessageCaptured()
    {
        var debugger = new DebuggerMachine(capacity: 1000);
        var message = new TestMessage("first", 42);

        debugger.CaptureMessage(message, "{\"count\":0}");

        await Assert.That(debugger.CurrentState).IsEqualTo(DebuggerState.Recording);
        await Assert.That(debugger.Timeline.Count).IsEqualTo(1);

        var entry = debugger.Timeline[0];
        await Assert.That(entry.Sequence).IsEqualTo(0);
        await Assert.That(entry.MessageType).IsEqualTo(nameof(TestMessage));
        await Assert.That(entry.Timestamp).IsGreaterThan(0L);
        await Assert.That(entry.ModelSnapshotPreview).IsNotEmpty();
    }

    [Test]
    public async Task JumpToCursor_TransitionsFromPlayingToPausedWithoutTimelineMutation()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        for (var i = 0; i < 10; i++)
        {
            debugger.CaptureMessage(new TestMessage($"message-{i}", i), $"{{\"count\":{i}}}");
        }

        debugger.Play();
        var initialTimelineCount = debugger.Timeline.Count;

        debugger.Jump(entrySequence: 7);

        await Assert.That(debugger.CurrentState).IsEqualTo(DebuggerState.Paused);
        await Assert.That(debugger.CursorPosition).IsEqualTo(7);
        await Assert.That(debugger.Timeline.Count).IsEqualTo(initialTimelineCount);
        await Assert.That(debugger.SideEffectCount).IsEqualTo(0);
    }

    [Test]
    public async Task StepForward_IncreasesCursorByOne_WhenNotAtEnd()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        for (var i = 0; i < 5; i++)
        {
            debugger.CaptureMessage(new TestMessage($"step-{i}", null), $"{{\"step\":{i}}}");
        }

        debugger.Jump(entrySequence: 2);
        var expectedSnapshot = debugger.Timeline[3].ModelSnapshotPreview;

        debugger.StepForward();

        await Assert.That(debugger.CurrentState).IsEqualTo(DebuggerState.Paused);
        await Assert.That(debugger.CursorPosition).IsEqualTo(3);
        await Assert.That(debugger.CurrentModelSnapshotPreview).IsEqualTo(expectedSnapshot);
    }

    [Test]
    public async Task PlaybackStopsAtTimelineEnd_WhenPlayingAndReachesLastEntry()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        for (var i = 0; i < 3; i++)
        {
            debugger.CaptureMessage(new TestMessage($"play-{i}", null), "{}");
        }

        debugger.Jump(entrySequence: 0);
        debugger.Play();

        debugger.StepForward();
        debugger.StepForward();

        var stateBeforeFinalStep = debugger.CurrentState;
        debugger.StepForward();

        await Assert.That(stateBeforeFinalStep).IsEqualTo(DebuggerState.Paused);
        await Assert.That(debugger.CurrentState).IsEqualTo(DebuggerState.Paused);
        await Assert.That(debugger.CursorPosition).IsEqualTo(2);
    }

    private sealed record TestMessage(string Name, object? Payload) : Message;
}
