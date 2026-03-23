// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

/// <summary>
/// Validates that message capture respects the UseDebugger() opt-in hook.
/// Purpose: Verify that messages are only captured when UseDebugger() has been called
/// during runtime initialization, and that capture can be toggled on/off.
/// 
/// NOTE: These tests are expected to FAIL TO COMPILE today (UseDebugger() does not exist
/// on Runtime, and no capture hooks are wired). They document the contract for message capture.
/// </summary>
public class DebuggerCaptureLifecycleTests
{
    /// <summary>
    /// Test 3a: By default, when no UseDebugger() is called, a fresh runtime does NOT capture
    /// messages. The Timeline remains null or empty.
    /// 
    /// Validates the seam: Capture opt-in (off by default), no side effects.
    /// TODAY: Fails to compile - Runtime.Debugger field does not exist.
    /// </summary>
    [Test]
    public void CaptureDisabledByDefault_WhenDebuggerNotInitialized()
    {
        // Arrange
        var runtime = new Runtime(initialModel: new { count = 0 });
        // Intentionally NOT calling runtime.UseDebugger()

        // Act: Dispatch a message
        runtime.Dispatch(new Message { Type = "Increment", Args = new object[] { } });
        runtime.Dispatch(new Message { Type = "Increment", Args = new object[] { } });

        // Assert
        // If Runtime has no debugger, Timeline should be null or inaccessible
        var timeline = runtime.Debugger;
        Assert.That(timeline, Is.Null,
            "Debugger should be null when UseDebugger() was not called");
        
        // Alternative form if debugger is always present but disabled:
        // Assert.That(runtime.Debugger.Timeline.Count, Is.EqualTo(0), "Messages should not be captured");
    }

    /// <summary>
    /// Test 3b: When UseDebugger() is called during runtime initialization,
    /// subsequent messages ARE captured in the Timeline with correct Type and Args.
    /// 
    /// Validates the seam: UseDebugger() enables capture, message metadata preserved.
    /// TODAY: Fails to compile - UseDebugger() method does not exist on Runtime.
    /// </summary>
    [Test]
    public void CaptureEnabledWhenUseDebuggerCalled()
    {
        // Arrange
        var runtime = new Runtime(initialModel: new { count = 0 });
        runtime.UseDebugger(capacity: 1000);  // Enable debugger

        var message1 = new Message { Type = "Increment", Args = new object[] { 5 } };
        var message2 = new Message { Type = "Reset", Args = new object[] { } };

        // Act: Dispatch messages
        runtime.Dispatch(message1);
        runtime.Dispatch(message2);

        // Assert
        Assert.That(runtime.Debugger, Is.Not.Null,
            "Debugger should be initialized after UseDebugger()");
        Assert.That(runtime.Debugger.Timeline.Count, Is.EqualTo(2),
            "Timeline should contain exactly 2 captured messages");
        
        var entry0 = runtime.Debugger.Timeline[0];
        Assert.That(entry0.MessageType, Is.EqualTo("Increment"),
            "First entry MessageType should be 'Increment'");
        Assert.That(entry0.ArgsPreview, Contains.Substring("5"),
            "First entry ArgsPreview should contain '5'");
        
        var entry1 = runtime.Debugger.Timeline[1];
        Assert.That(entry1.MessageType, Is.EqualTo("Reset"),
            "Second entry MessageType should be 'Reset'");
    }

    /// <summary>
    /// Test 3c: Capture can be toggled on and off at runtime. Messages sent while capture
    /// is disabled are not added to the timeline, even if the debugger is initialized.
    /// 
    /// Validates the seam: DisableCapture() and EnableCapture() toggle, selective recording.
    /// TODAY: Fails to compile - DisableCapture()/EnableCapture() APIs do not exist.
    /// </summary>
    [Test]
    public void CaptureToggleOnRuntime_EnablesAndDisablesCapture()
    {
        // Arrange
        var runtime = new Runtime(initialModel: new { count = 0 });
        runtime.UseDebugger(capacity: 1000);

        // Phase 1: Send 3 messages with capture enabled
        runtime.Dispatch(new Message { Type = "Phase1_Msg1", Args = new object[] { } });
        runtime.Dispatch(new Message { Type = "Phase1_Msg2", Args = new object[] { } });
        runtime.Dispatch(new Message { Type = "Phase1_Msg3", Args = new object[] { } });
        
        Assert.That(runtime.Debugger.Timeline.Count, Is.EqualTo(3),
            "Should have 3 entries after phase 1");

        // Act: Disable capture
        runtime.Debugger.DisableCapture();
        
        // Phase 2: Send 2 messages with capture disabled
        runtime.Dispatch(new Message { Type = "Phase2_Msg1", Args = new object[] { } });
        runtime.Dispatch(new Message { Type = "Phase2_Msg2", Args = new object[] { } });

        // Assert: Still 3 entries (phase 2 messages not recorded)
        Assert.That(runtime.Debugger.Timeline.Count, Is.EqualTo(3),
            "Timeline should still have 3 entries (phase 2 messages disabled)");

        // Act: Re-enable capture
        runtime.Debugger.EnableCapture();
        runtime.Dispatch(new Message { Type = "Phase3_Msg1", Args = new object[] { } });

        // Assert: 4 entries (one from phase 3)
        Assert.That(runtime.Debugger.Timeline.Count, Is.EqualTo(4),
            "Timeline should have 4 entries after re-enabling and sending one message");
        
        var lastEntry = runtime.Debugger.Timeline[3];
        Assert.That(lastEntry.MessageType, Is.EqualTo("Phase3_Msg1"),
            "Last entry should be from phase 3");
    }

    /// <summary>
    /// Test 3d: Message args are correctly captured and previewed in the timeline,
    /// even for complex nested structures.
    /// 
    /// Validates the seam: ArgsPreview serialization, complex object handling.
    /// TODAY: Fails to compile - UseDebugger() method does not exist.
    /// </summary>
    [Test]
    public void CaptureMessageArgs_SerializesComplexObjectsCorrectly()
    {
        // Arrange
        var runtime = new Runtime(initialModel: new { count = 0 });
        runtime.UseDebugger(capacity: 1000);

        var complexArg = new { id = 123, nested = new { name = "test", value = 45.6 } };
        var message = new Message { Type = "ComplexAction", Args = new object[] { complexArg, "string", 789 } };

        // Act
        runtime.Dispatch(message);

        // Assert
        Assert.That(runtime.Debugger.Timeline.Count, Is.EqualTo(1),
            "Should have 1 captured entry");
        
        var entry = runtime.Debugger.Timeline[0];
        Assert.That(entry.ArgsPreview, Is.Not.Empty,
            "ArgsPreview should not be empty");
        Assert.That(entry.ArgsPreview, Contains.Substring("123"),
            "ArgsPreview should contain nested id value");
        Assert.That(entry.ArgsPreview, Contains.Substring("test"),
            "ArgsPreview should contain nested name value");
    }
}
