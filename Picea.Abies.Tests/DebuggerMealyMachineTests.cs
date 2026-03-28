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

    [Test]
    public async Task AtStart_IsTrue_WhenTimelineIsEmptyOrCursorAtZero()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        // Empty timeline: AtStart should be true (cursor = -1, <= 0)
        await Assert.That(debugger.AtStart).IsTrue();

        debugger.CaptureMessage(new TestMessage("first", null), "{}");
        debugger.Jump(entrySequence: 0);

        await Assert.That(debugger.AtStart).IsTrue();
    }

    [Test]
    public async Task AtEnd_IsTrue_WhenCursorAtLastEntry()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        for (var i = 0; i < 5; i++)
        {
            debugger.CaptureMessage(new TestMessage($"msg-{i}", null), $"{{\"i\":{i}}}");
        }

        // In Recording state, cursor tracks the last entry
        await Assert.That(debugger.AtEnd).IsTrue();

        debugger.Jump(entrySequence: 2);
        await Assert.That(debugger.AtEnd).IsFalse();

        debugger.Jump(entrySequence: 4);
        await Assert.That(debugger.AtEnd).IsTrue();
    }

    [Test]
    public async Task AtEnd_IsTrue_WhenTimelineIsEmpty()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        await Assert.That(debugger.AtEnd).IsTrue();
    }

    [Test]
    public async Task CaptureInitialModel_StoresSnapshotPreviewAndFullSnapshot()
    {
        var debugger = new DebuggerMachine(capacity: 1000);
        var model = new { Count = 0 };

        debugger.CaptureInitialModel("{\"Count\":0}", model);

        await Assert.That(debugger.InitialModelSnapshotPreview).IsEqualTo("{\"Count\":0}");
        await Assert.That(debugger.InitialModelSnapshot).IsEqualTo(model);
    }

    [Test]
    public async Task CaptureInitialModel_ResetsOnClearTimeline()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        debugger.CaptureInitialModel("{\"Count\":0}", new { Count = 0 });
        debugger.CaptureMessage(new TestMessage("msg", null), "{}");

        debugger.ClearTimeline();

        await Assert.That(debugger.InitialModelSnapshotPreview).IsEmpty();
        await Assert.That(debugger.InitialModelSnapshot).IsNull();
    }

    [Test]
    public async Task GetPreviousModelSnapshotPreview_ReturnsInitialModelForFirstEntry()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        debugger.CaptureInitialModel("{\"Count\":0}");
        debugger.CaptureMessage(new TestMessage("first", null), "{\"Count\":1}");
        debugger.CaptureMessage(new TestMessage("second", null), "{\"Count\":2}");

        var previous = debugger.GetPreviousModelSnapshotPreview(0);

        await Assert.That(previous).IsEqualTo("{\"Count\":0}");
    }

    [Test]
    public async Task GetPreviousModelSnapshotPreview_ReturnsPreviousEntryForLaterIndices()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        debugger.CaptureInitialModel("{\"Count\":0}");
        debugger.CaptureMessage(new TestMessage("first", null), "{\"Count\":1}");
        debugger.CaptureMessage(new TestMessage("second", null), "{\"Count\":2}");
        debugger.CaptureMessage(new TestMessage("third", null), "{\"Count\":3}");

        await Assert.That(debugger.GetPreviousModelSnapshotPreview(1)).IsEqualTo("{\"Count\":1}");
        await Assert.That(debugger.GetPreviousModelSnapshotPreview(2)).IsEqualTo("{\"Count\":2}");
    }

    [Test]
    public async Task PatchCount_CapturedCorrectlyInTimestampedEntry()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        debugger.CaptureMessage(new TestMessage("no-patches", null), "{}", patchCount: 0);
        debugger.CaptureMessage(new TestMessage("some-patches", null), "{}", patchCount: 42);
        debugger.CaptureMessage(new TestMessage("many-patches", null), "{}", patchCount: 1000);

        await Assert.That(debugger.Timeline[0].PatchCount).IsEqualTo(0);
        await Assert.That(debugger.Timeline[1].PatchCount).IsEqualTo(42);
        await Assert.That(debugger.Timeline[2].PatchCount).IsEqualTo(1000);
    }

    [Test]
    public async Task PatchCount_DefaultsToZero_WhenNotProvided()
    {
        var debugger = new DebuggerMachine(capacity: 1000);

        debugger.CaptureMessage(new TestMessage("default-patches", null), "{}");

        await Assert.That(debugger.Timeline[0].PatchCount).IsEqualTo(0);
    }

    private sealed record TestMessage(string Name, object? Payload) : Message;
}
