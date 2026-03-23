// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Picea.Abies.Browser.Tests;

/// <summary>
/// Validates the debugger mount point seam: DOM isolation, optional loading, and Debug-mode-only activation.
/// Purpose: Verify that the debugger UI mounts at correct DOM element (id="abies-debugger-timeline"),
/// is completely isolated from main app, and only loads when in Debug mode.
/// 
/// Mount Point Contract:
/// - Single DOM element: id="abies-debugger-timeline" (C# template supplies this in Debug mode)
/// - debugger.js module: Optional JS file that initializes mount point when present
/// - Release build: NO debugger.js file, NO mount point in HTML
/// 
/// NOTE: These tests are expected to FAIL TO COMPILE or FAIL AT RUNTIME today.
/// - Tests 2a/2b: Fail because debugger.js does not exist or mount logic not implemented
/// - Tests 2c: May pass vacuously if debugger UI is not exposed yet
/// 
/// Test Strategy: Mix of C# unit tests + integration tests
/// - C# tests: Validate mount point element presence/absence in rendered HTML
/// - Integration: Load actual wwwroot/ files and check structure
/// </summary>
public class DebuggerMountPointTests
{
    /// <summary>
    /// Test 2a: When debugger UI bundle is loaded, the mount point DOM element
    /// (id="abies-debugger-timeline") exists in the document and contains expected UI structure.
    /// 
    /// Validates the seam: Mount point DOM element presence, correct ID, UI isolation from main app.
    /// 
    /// TODAY: Fails - debugger.js does not exist or mount logic not implemented.
    /// TOMORROW: Passes when debugger.js mounts the UI panel.
    /// </summary>
    [Test]
    public async Task DebuggerPanelMountsAtCorrectDOMElement_WhenDebuggerEnabled()
    {
        // Arrange
        var html = """
        <!DOCTYPE html>
        <html>
        <head>
            <title>Abies Debug</title>
        </head>
        <body>
            <div id="abies-debugger-timeline"></div>
            <div id="main"><!-- Main app container --></div>
        </body>
        </html>
        """;

        // Parse HTML (simplified—would use HtmlDocument or similar in real test)
        var mountPointId = "abies-debugger-timeline";
        var mountPointExists = html.Contains($"id=\"{mountPointId}\"");

        // Act: Load debugger.js module (simulated)
        // EXPECTED FAILURE: Picea.Abies.Browser.Debugger.DebuggerUI does not have InitializeMount() method
        var debuggerUI = new Picea.Abies.Browser.Debugger.DebuggerUI();
        debuggerUI.InitializeMount(mountPointId);

        // Assert
        await Assert.That(mountPointExists).IsTrue();
        
        await Assert.That(debuggerUI.IsMounted).IsTrue();
        
        // Verify UI structure is present
        await Assert.That(debuggerUI.ContainsElement("message-log")).IsTrue();
        await Assert.That(debuggerUI.ContainsElement("control-bar")).IsTrue();
        await Assert.That(debuggerUI.ContainsElement("timeline-inspector")).IsTrue();
        
        // Verify isolation: main app NOT modified
        await Assert.That(debuggerUI.MainAppModified).IsFalse();
    }

    /// <summary>
    /// Test 2b: When debugger.js is NOT loaded (i.e., only abies.js is loaded with no debugger module),
    /// the debugger UI is NOT present in the DOM, and the main app loads and functions normally.
    /// This validates that debugger panel is optional.
    /// 
    /// Validates the seam: Debugger is optional, doesn't interfere with main app when absent.
    /// 
    /// TODAY: Passes vacuously (debugger UI doesn't exist, so naturally not present).
    /// TOMORROW: Continues to pass when debugger.js optional loading is implemented.
    /// </summary>
    [Test]
    public async Task DebuggerWidgetNotPresentWhenDebuggerJS_NotLoaded()
    {
        // Arrange
        var html = """
        <!DOCTYPE html>
        <html>
        <head>
            <title>Abies App</title>
        </head>
        <body>
            <div id="main"><!-- Main app container --></div>
            <!-- Note: No id="abies-debugger-timeline" element -->
        </body>
        </html>
        """;

        var mountPointId = "abies-debugger-timeline";
        var mountPointExists = html.Contains($"id=\"{mountPointId}\"");

        // Act: Don't load debugger.js—only load abies.js
        // Simulate main app loading without debugger
        var mainApp = new Picea.Abies.Browser.Runtime();
        var isMainAppInitialized = mainApp.IsInitialized;

        // Assert
        await Assert.That(mountPointExists).IsFalse();
        
        await Assert.That(isMainAppInitialized).IsTrue();
        
        // Verify no debugger classes in DOM
        await Assert.That(html).DoesNotContain("class=\"debugger");
        await Assert.That(html).DoesNotContain("class=\"timeline");
        
        // Verify console is clean
        await Assert.That(mainApp.ConsoleErrors).IsEmpty();
    }

    /// <summary>
    /// Test 2c: When user interacts with debugger UI (play/pause buttons),
    /// each action dispatches a correctly-typed message to the C# adapter
    /// and updates the UI based on the response.
    /// 
    /// Validates the seam: UI responsiveness, message dispatch, response handling.
    /// 
    /// NOTE: This test requires Playwright or similar browser automation—
    /// can be skipped in this phase if E2E harness unavailable.
    /// TODAY: Fails at runtime - debugger UI not implemented.
    /// TOMORROW: Passes when UI events are wired to adapter.
    /// </summary>
    [Test]
    public async Task DebuggerUIRespondsToPlayButton_WithMessageDispatch()
    {
        // Arrange: Simulate debugger UI with mock timeline entries
        var debuggerUI = new Picea.Abies.Browser.Debugger.DebuggerUI();
        debuggerUI.InitializeMount("abies-debugger-timeline");

        // Add mock timeline entries
        for (int i = 0; i < 5; i++)
        {
            debuggerUI.AddTimelineEntry(new DebuggerTimelineEntry
            {
                Sequence = i,
                MessageType = $"Message{i}",
                ArgsPreview = $"arg{i}",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + i,
                ModelSnapshotPreview = $"{{\"state\": {i}}}"
            });
        }

        debuggerUI.CurrentCursorPosition = 0;
        int messageDispatchCount = 0;
        string? lastDispatchedMessageType = null;

        // Mock adapter callback
        debuggerUI.OnMessageDispatched += (msg) =>
        {
            messageDispatchCount++;
            lastDispatchedMessageType = msg.Type;
        };

        // Act: Click play button
        // EXPECTED FAILURE: No PlayButton element or click handler
        debuggerUI.ClickButton("play-button");

        // Wait for async response (simulated)
        System.Threading.Thread.Sleep(100);

        // Simulate C# response: auto-step cursor
        debuggerUI.UpdateFromResponse(new DebuggerAdapterResponse
        {
            Status = "playing",
            CursorPosition = 1,
            TimelineSize = 5,
            ModelSnapshotPreview = "{\"state\": 1}"
        });

        // Assert
        await Assert.That(messageDispatchCount).IsGreaterThan(0);
        await Assert.That(lastDispatchedMessageType).IsEqualTo("play");
        
        await Assert.That(debuggerUI.CurrentCursorPosition).IsEqualTo(1);
        
        await Assert.That(debuggerUI.GetHighlightedEntry()?.Sequence).IsEqualTo(1);
    }

    /// <summary>
    /// Test 2d: Keyboard shortcuts work correctly in debugger UI:
    /// - Space: Play/Pause toggle
    /// - ArrowRight: Step Forward
    /// - ArrowLeft: Step Back
    /// - J: Focus jump input
    /// - Escape: Close debugger
    /// 
    /// Validates the seam: Keyboard accessibility, no interfering with main app shortcuts.
    /// 
    /// TODAY: Fails at runtime - keyboard handlers not implemented.
    /// TOMORROW: Passes when keyboard event handlers are wired.
    /// </summary>
    [Test]
    [Arguments(" ", "play")]  // Space → play/pause toggle
    [Arguments("ArrowRight", "step-forward")]
    [Arguments("ArrowLeft", "step-back")]
    public async Task DebuggerUIRespondToKeyboardShortcuts_WithMessageDispatch(string keyCode, string expectedMessage)
    {
        // Arrange
        var debuggerUI = new Picea.Abies.Browser.Debugger.DebuggerUI();
        debuggerUI.InitializeMount("abies-debugger-timeline");
        debuggerUI.CurrentCursorPosition = 2;

        string? lastDispatchedType = null;
        debuggerUI.OnMessageDispatched += (msg) => lastDispatchedType = msg.Type;

        // Act
        // EXPECTED FAILURE: No keyboard event handler registered
        debuggerUI.SimulateKeyboardEvent(keyCode);

        // Assert
        await Assert.That(lastDispatchedType).IsEqualTo(expectedMessage);
    }
}

/// <summary>
/// Mock definition: DebuggerTimelineEntry
/// </summary>
public class DebuggerTimelineEntry
{
    public int Sequence { get; init; }
    public required string MessageType { get; init; }
    public required string ArgsPreview { get; init; }
    public long Timestamp { get; init; }
    public required string ModelSnapshotPreview { get; init; }
}
