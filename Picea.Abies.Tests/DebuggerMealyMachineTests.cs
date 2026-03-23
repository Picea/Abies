// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

/// <summary>
/// Validates Mealy machine state transitions for the time travel debugger.
/// Purpose: Verify that state transitions (Recording → Paused → Playing → Jumped) produce
/// deterministic outputs without side effects during replay.
/// 
/// NOTE: These tests are expected to FAIL TO COMPILE today (Picea.Abies.Debugger namespace
/// does not exist yet). They document the contract for the C# debugger implementation.
/// </summary>
public class DebuggerMealyMachineTests
{
    /// <summary>
    /// Test 1a: When the first message is captured, the debugger transitions from Idle to Recording
    /// and records exactly one timeline entry with sequence=0 and monotonic timestamp.
    /// 
    /// Validates the seam: Initial state capture, sequence numbering, timestamp generation.
    /// TODAY: Fails to compile - no Picea.Abies.Debugger namespace.
    /// </summary>
    [Test]
    public void RecordMessageAndTransitionToStarted_WhenFirstMessageCaptured()
    {
        // Arrange
        var debugger = new DebuggerMachine(capacity: 1000);
        var message = new Message { Type = "TestMessage", Args = new object[] { "value1", 42 } };

        // Act
        debugger.CaptureMessage(message, "{}");  // modelSnapshotPreview = "{}"

        // Assert
        Assert.That(debugger.CurrentState, Is.EqualTo(DebuggerState.Recording),
            "State should transition to Recording after first message capture");
        Assert.That(debugger.Timeline.Count, Is.EqualTo(1),
            "Timeline should contain exactly 1 entry after first capture");
        
        var entry = debugger.Timeline[0];
        Assert.That(entry.Sequence, Is.EqualTo(0),
            "First entry should have sequence = 0");
        Assert.That(entry.MessageType, Is.EqualTo("TestMessage"),
            "Entry MessageType should match captured message type");
        Assert.That(entry.Timestamp, Is.GreaterThan(0),
            "Timestamp should be generated and non-zero");
        Assert.That(entry.ModelSnapshotPreview, Is.Not.Empty,
            "ModelSnapshotPreview should be non-empty string");
    }

    /// <summary>
    /// Test 1b: When jumping to a specific cursor position, the Mealy machine transitions from
    /// Playing to Paused WITHOUT re-executing commands or re-registering subscriptions (deterministic replay only).
    /// 
    /// Validates the seam: Replay determinism (no side effects), state transitions, cursor positioning.
    /// TODAY: Fails to compile - DebuggerMachine.Jump() API does not exist.
    /// </summary>
    [Test]
    public void JumpToCursor_TransitionsFromPlayingToPausedWithoutSideEffects()
    {
        // Arrange
        var debugger = new DebuggerMachine(capacity: 1000);
        debugger.CurrentState = DebuggerState.Playing;  // Simulate playing state
        
        // Add 10 timeline entries (sequences 0-9)
        for (int i = 0; i < 10; i++)
        {
            var message = new Message { Type = $"Message{i}", Args = new object[] { i } };
            debugger.CaptureMessage(message, $"{{\"count\":{i}}}");
        }
        
        debugger.CursorPosition = 5;  // Currently at entry 5
        var initialTimeline = debugger.Timeline.Count;

        // Act
        debugger.Jump(entrySequence: 7);

        // Assert
        Assert.That(debugger.CurrentState, Is.EqualTo(DebuggerState.Paused),
            "State should transition to Paused after Jump");
        Assert.That(debugger.CursorPosition, Is.EqualTo(7),
            "Cursor should move to entry 7");
        Assert.That(debugger.Timeline.Count, Is.EqualTo(initialTimeline),
            "Timeline should remain unchanged after jump (no side effects)");
        Assert.That(debugger.SideEffectCount, Is.EqualTo(0),
            "No Commands or Subscriptions should be re-executed during replay");
    }

    /// <summary>
    /// Test 1c: When stepping forward from a paused state, the cursor increments by 1,
    /// state remains Paused, and ModelSnapshotPreview is updated to reflect the next entry.
    /// 
    /// Validates the seam: Single-step replay, cursor increment, snapshot update.
    /// TODAY: Fails to compile - StepForward() API does not exist.
    /// </summary>
    [Test]
    public void StepForward_IncreasesCursorByOne_WhenNotAtEnd()
    {
        // Arrange
        var debugger = new DebuggerMachine(capacity: 1000);
        debugger.CurrentState = DebuggerState.Paused;
        
        for (int i = 0; i < 5; i++)
        {
            var message = new Message { Type = $"Step{i}", Args = new object[] { } };
            debugger.CaptureMessage(message, $"{{\"step\":{i}}}");
        }
        
        debugger.CursorPosition = 2;
        var entryAt3 = debugger.Timeline[3];

        // Act
        debugger.StepForward();

        // Assert
        Assert.That(debugger.CurrentState, Is.EqualTo(DebuggerState.Paused),
            "State should remain Paused after step");
        Assert.That(debugger.CursorPosition, Is.EqualTo(3),
            "Cursor should increment to 3");
        Assert.That(debugger.CurrentModelSnapshotPreview, Is.EqualTo(entryAt3.ModelSnapshotPreview),
            "ModelSnapshotPreview should update to reflect entry 3");
    }

    /// <summary>
    /// Test 1d: When playing forward and reaching the last timeline entry, playback automatically
    /// pauses (no attempt to fetch a non-existent entry beyond the timeline).
    /// 
    /// Validates the seam: Boundary condition (cursor at timeline end), automatic pause.
    /// TODAY: Fails to compile - PlaybackLoop/AutostepLogic does not exist.
    /// </summary>
    [Test]
    public void PlaybackStopsAtTimelineEnd_WhenPlayingAndReachesLastEntry()
    {
        // Arrange
        var debugger = new DebuggerMachine(capacity: 1000);
        debugger.CurrentState = DebuggerState.PlayingForward;
        
        // Add 3 timeline entries (indices 0, 1, 2)
        for (int i = 0; i < 3; i++)
        {
            var message = new Message { Type = $"PlayMsg{i}", Args = new object[] { } };
            debugger.CaptureMessage(message, $"{{}}");
        }
        
        debugger.CursorPosition = 0;

        // Act: Simulate autoplay stepping through all entries
        debugger.StepForward();  // cursor → 1
        debugger.StepForward();  // cursor → 2 (at end)
        var stateBeforeStep = debugger.CurrentState;
        debugger.StepForward();  // attempt step beyond end

        // Assert
        Assert.That(stateBeforeStep, Is.EqualTo(DebuggerState.PlayingForward),
            "Should still be playing before reaching end");
        Assert.That(debugger.CurrentState, Is.EqualTo(DebuggerState.Paused),
            "Should auto-pause when cursor reaches timeline end");
        Assert.That(debugger.CursorPosition, Is.EqualTo(2),
            "Cursor should remain at last entry (index 2)");
    }
}
