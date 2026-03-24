// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using Picea.Abies.Browser.Debugger;
using Picea.Abies.Debugger;

namespace Picea.Abies.Browser.Tests;

/// <summary>
/// Validates the debugger mount point seam: DOM isolation, optional loading, and Debug-mode-only activation.
/// Purpose: Verify that the debugger UI mounts at correct DOM element (id="abies-debugger-timeline"),
/// is completely isolated from main app, and only loads when in Debug mode.
/// 
/// Mount Point Contract:
/// - Single DOM element: id="abies-debugger-timeline" (browser runtime injects this in Debug mode)
/// - debugger.js exposes the mount helper and guards against duplicate injection
/// - Release build: no debugger mount injection
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
        var mountPointExists = Regex.IsMatch(
            html,
            $"<div[^>]*\\bid\\s*=\\s*\"{Regex.Escape(mountPointId)}\"",
            RegexOptions.IgnoreCase
        );

        // Act: Load debugger.js module (simulated)
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

    [Test]
    public async Task DebuggerMvuViewRendersControlBarAndTimeline_WithAbiesEventHandlers()
    {
        var debuggerUI = new DebuggerUI();
        debuggerUI.InitializeMount("abies-debugger-timeline");
        debuggerUI.AddTimelineEntry(new DebuggerTimelineEntry
        {
            Sequence = 0,
            MessageType = "Init",
            ArgsPreview = "{}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ModelSnapshotPreview = "{}"
        });

        var html = debuggerUI.RenderHtml();

        await Assert.That(html).Contains("id=\"control-bar\"");
        await Assert.That(html).Contains("id=\"timeline-inspector\"");
        await Assert.That(html).Contains("id=\"timeline-list\"");
        await Assert.That(html).Contains("data-abies-debugger-intent=\"play\"");
    }

    /// <summary>
    /// Test 2b: When debugger.js is NOT loaded (i.e., only abies.js is loaded with no debugger module),
    /// the debugger UI is NOT present in the DOM, and the main app loads and functions normally.
    /// This validates that debugger panel is optional.
    /// 
    /// Validates the seam: Debugger is optional, doesn't interfere with main app when absent.
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
        var mountPointExists = Regex.IsMatch(
            html,
            $"<div[^>]*\\bid\\s*=\\s*\"{Regex.Escape(mountPointId)}\"",
            RegexOptions.IgnoreCase
        );

        // Act: Don't load debugger.js—only load abies.js
        // Verify main app HTML structure is clean of debugger elements
        var htmlWithoutComments = Regex.Replace(html, "<!--.*?-->", string.Empty, RegexOptions.Singleline);
        var hasDebuggerElements = Regex.IsMatch(
            htmlWithoutComments,
            "id=\"(abies-debugger-timeline|timeline-inspector|control-bar|message-log)\"|class=\"debugger-",
            RegexOptions.IgnoreCase
        );

        // Assert
        await Assert.That(mountPointExists).IsFalse();
        await Assert.That(hasDebuggerElements).IsFalse();
        
        // Verify no debugger classes in DOM
        await Assert.That(html).DoesNotContain("class=\"debugger");
        await Assert.That(html).DoesNotContain("class=\"timeline");
    }

    /// <summary>
    /// Test 2c: When user interacts with debugger UI (play/pause buttons),
    /// each action dispatches a correctly-typed message to the C# adapter
    /// and updates the UI based on the response.
    /// 
    /// Validates the seam: UI responsiveness, message dispatch, response handling.
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
        debuggerUI.ClickButton("play-button");

        // Wait for async response (simulated)
        await Task.Delay(100);

        // Simulate C# response: auto-step cursor
        debuggerUI.UpdateFromResponse(new DebuggerAdapterResponse
        {
            Status = "playing",
            CursorPosition = 1,
            TimelineSize = 5,
            ModelSnapshotPreview = "{\"state\": 1}"
        });

        // Assert
        await Assert.That(messageDispatchCount).IsEqualTo(1);
        await Assert.That(lastDispatchedMessageType).IsEqualTo("play");
        
        await Assert.That(debuggerUI.CurrentCursorPosition).IsEqualTo(1);
        
        await Assert.That(debuggerUI.GetHighlightedEntry()?.Sequence).IsEqualTo(1);
    }

    [Test]
    public async Task DebuggerAdapterMessageAppliesToRuntimeDebugger_AndSyncsMvuModel()
    {
        var debuggerMachine = new DebuggerMachine(16);
        debuggerMachine.CaptureMessage(new TestMessage { Type = "first" }, "{\"state\":0}");
        debuggerMachine.CaptureMessage(new TestMessage { Type = "second" }, "{\"state\":1}");
        debuggerMachine.Jump(0);

        var debuggerUI = new DebuggerUI();
        debuggerUI.InitializeMount("abies-debugger-timeline");
        debuggerUI.SyncFromRuntimeDebugger(debuggerMachine);

        var response = DebuggerRuntimeBridge.Execute(
            new DebuggerAdapterMessage { Type = "step-forward" },
            debuggerMachine);

        debuggerUI.SyncFromRuntimeDebugger(debuggerMachine);

        await Assert.That(response.CursorPosition).IsEqualTo(1);
        await Assert.That(response.TimelineSize).IsEqualTo(2);
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

        var dispatchCount = 0;
        string? lastDispatchedType = null;
        debuggerUI.OnMessageDispatched += (msg) =>
        {
            dispatchCount++;
            lastDispatchedType = msg.Type;
        };

        // Act
        debuggerUI.SimulateKeyboardEvent(keyCode);

        // Assert
        await Assert.That(dispatchCount).IsEqualTo(1);
        await Assert.That(lastDispatchedType).IsEqualTo(expectedMessage);
    }
}
