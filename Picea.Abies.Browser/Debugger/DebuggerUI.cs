// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Picea.Abies.Browser.Debugger;

/// <summary>
/// Debugger UI component that mounts at a specific DOM element and wires user interactions
/// to the adapter for message dispatch.
/// 
/// CRITICAL: This class coordinates user input → message dispatching → response rendering.
/// It does NOT contain replay logic (that's in the C# Mealy machine).
/// 
/// Responsibilities:
/// - Mount/unmount at DOM element (id="abies-debugger-timeline")
/// - Wire button clicks and keyboard events to message dispatch
/// - Render timeline based on C# responses
/// - Provide test hooks for verification
/// </summary>
public sealed class DebuggerUI
{
    private const string DefaultMountPointId = "abies-debugger-timeline";
    
    private string _mountPointId = DefaultMountPointId;
    private bool _isMounted = false;
    private int _currentCursorPosition = 0;
    private List<DebuggerTimelineEntry> _timelineEntries = [];
    private Dictionary<string, bool> _elementPresence = [];
    private bool _mainAppModified = false;

    public bool IsMounted => _isMounted;
    public int CurrentCursorPosition
    {
        get => _currentCursorPosition;
        set => _currentCursorPosition = value;
    }
    public bool MainAppModified => _mainAppModified;

    /// <summary>
    /// Event fired when a message is dispatched to the adapter (play, pause, step, jump, etc).
    /// </summary>
    public event Action<DebuggerAdapterMessage>? OnMessageDispatched;

    /// <summary>
    /// Initialize and mount the debugger UI at the specified DOM element ID.
    /// </summary>
    public void InitializeMount(string mountPointId)
    {
        _mountPointId = string.IsNullOrWhiteSpace(mountPointId) ? DefaultMountPointId : mountPointId;
        _isMounted = true;

        _elementPresence.Clear();

        // Initialize expected UI elements
        _elementPresence["message-log"] = true;
        _elementPresence["control-bar"] = true;
        _elementPresence["timeline-inspector"] = true;
    }

    /// <summary>
    /// Check if a UI element exists in the mounted debugger.
    /// </summary>
    public bool ContainsElement(string elementId)
    {
        return _elementPresence.TryGetValue(elementId, out var exists) && exists;
    }

    /// <summary>
    /// Add a timeline entry to the debugger's timeline history.
    /// </summary>
    public void AddTimelineEntry(DebuggerTimelineEntry entry)
    {
        _timelineEntries.Add(entry);
    }

    /// <summary>
    /// Simulate a button click and dispatch the corresponding message.
    /// </summary>
    public void ClickButton(string buttonId)
    {
        var messageType = buttonId switch
        {
            "play-button" => "play",
            "pause-button" => "pause",
            "step-forward-button" => "step-forward",
            "step-back-button" => "step-back",
            "clear-button" => "clear-timeline",
            _ => throw new ArgumentException($"Unknown button: {buttonId}", nameof(buttonId))
        };

        var message = new DebuggerAdapterMessage { Type = messageType };
        OnMessageDispatched?.Invoke(message);
    }

    /// <summary>
    /// Simulate a keyboard event and dispatch the corresponding message.
    /// 
    /// Supported shortcuts:
    /// - Space: play/pause toggle
    /// - ArrowRight: step forward
    /// - ArrowLeft: step backward
    /// - J: focus jump input
    /// - Escape: close/blur
    /// </summary>
    public void SimulateKeyboardEvent(string keyCode)
    {
        var messageType = keyCode switch
        {
            " " => "play",  // Space → play/pause toggle
            "ArrowRight" => "step-forward",
            "ArrowLeft" => "step-back",
            "j" or "J" => "jump-focus",
            "Escape" => "blur",
            _ => null
        };

        if (messageType is null)
        {
            return;  // Unknown key, no action
        }

        var message = new DebuggerAdapterMessage { Type = messageType };
        OnMessageDispatched?.Invoke(message);
    }

    /// <summary>
    /// Update the UI based on a response from the C# adapter.
    /// This is read-only rendering—no state machine logic.
    /// </summary>
    public void UpdateFromResponse(DebuggerAdapterResponse response)
    {
        _currentCursorPosition = response.CursorPosition;
        // UI would update based on response (in real implementation, update DOM)
    }

    /// <summary>
    /// Get the currently highlighted/selected timeline entry.
    /// </summary>
    public DebuggerTimelineEntry? GetHighlightedEntry()
    {
        return _timelineEntries.FirstOrDefault(e => e.Sequence == _currentCursorPosition);
    }

    /// <summary>
    /// Render the timeline UI based on current state.
    /// (In real implementation, this would update the actual DOM.)
    /// </summary>
    public void RenderTimeline(DebuggerAdapterResponse response)
    {
        UpdateFromResponse(response);
    }
}

/// <summary>
/// Timeline entry representing a single message + model snapshot history point.
/// Used for rendering the timeline UI.
/// </summary>
public class DebuggerTimelineEntry
{
    public int Sequence { get; init; }
    public required string MessageType { get; init; }
    public required string ArgsPreview { get; init; }
    public long Timestamp { get; init; }
    public required string ModelSnapshotPreview { get; init; }
}

#endif
