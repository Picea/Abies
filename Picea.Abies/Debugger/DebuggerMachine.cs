// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

using System.Diagnostics;

namespace Picea.Abies.Debugger;

public enum DebuggerState
{
    Recording,
    Paused,
    PlayingForward,
    PlayingBackward,
    Jumped
}

/// <summary>
/// Single timeline entry with sequence, message metadata, timestamp, and model snapshot preview.
/// </summary>
public sealed record TimestampedEntry(
    long Sequence,
    string MessageType,
    string ArgsPreview,
    long Timestamp,
    string ModelSnapshotPreview
);

/// <summary>
/// Mealy machine managing debugger state transitions and replay.
/// Pure immutable state transformations — deterministic, no side effects during replay.
/// </summary>
public sealed class DebuggerMachine
{
    private readonly RingBuffer<TimestampedEntry> _timeline;
    private DebuggerState _currentState;
    private int _cursorPosition;
    private string _currentModelSnapshotPreview;
    private bool _isCapturing;
    private long _lastTimestamp;
    private int _sideEffectCount;

    public DebuggerMachine(int capacity = 10000)
    {
        _timeline = new RingBuffer<TimestampedEntry>(capacity);
        _currentState = DebuggerState.Recording;
        _cursorPosition = -1;
        _currentModelSnapshotPreview = string.Empty;
        _isCapturing = true;
        _lastTimestamp = 0;
        _sideEffectCount = 0;
    }

    public DebuggerState CurrentState => _currentState;
    public IReadOnlyList<TimestampedEntry> Timeline => _timeline.Entries;
    public int CursorPosition => _cursorPosition;
    public string CurrentModelSnapshotPreview => _currentModelSnapshotPreview;
    public int SideEffectCount => _sideEffectCount;

    /// <summary>
    /// Captures a message and transitions from Recording.
    /// </summary>
    public void CaptureMessage(object message, string modelSnapshotPreview)
    {
        if (!_isCapturing)
            return;

        var timestamp = GetMonotonicTimestamp();

        // Serialize message type and args
        var messageType = message?.GetType().Name ?? "Unknown";
        var argsJson = SerializeMessageArgs(message);

        var entry = new TimestampedEntry(
            Sequence: _timeline.NextSequence,
            MessageType: messageType,
            ArgsPreview: argsJson,
            Timestamp: timestamp,
            ModelSnapshotPreview: modelSnapshotPreview
        );

        _timeline.Add(entry);

        // Update cursor if we're in Recording state
        if (_currentState == DebuggerState.Recording)
        {
            _cursorPosition = (int)(_timeline.NextSequence - 1);
            _currentModelSnapshotPreview = modelSnapshotPreview;
        }

        _lastTimestamp = timestamp;
    }

    /// <summary>
    /// Jump to a specific entry in the timeline without side effects.
    /// </summary>
    public void Jump(int entrySequence)
    {
        if (_timeline.Count == 0)
            return;

        // Clamp to valid range
        int targetCursor = Math.Max(0, Math.Min(entrySequence, _timeline.Count - 1));

        _cursorPosition = targetCursor;
        if (targetCursor < _timeline.Count)
        {
            var entry = _timeline[targetCursor];
            _currentModelSnapshotPreview = entry.ModelSnapshotPreview;
        }

        _currentState = DebuggerState.Paused;
    }

    /// <summary>
    /// Step forward one entry in the timeline.
    /// </summary>
    public void StepForward()
    {
        if (_timeline.Count == 0)
            return;

        // If at or beyond the end, transition to Paused
        if (_cursorPosition >= _timeline.Count - 1)
        {
            _currentState = DebuggerState.Paused;
            if (_cursorPosition >= _timeline.Count)
            {
                _cursorPosition = _timeline.Count - 1;
            }
            return;
        }

        _cursorPosition++;
        if (_cursorPosition < _timeline.Count)
        {
            var entry = _timeline[_cursorPosition];
            _currentModelSnapshotPreview = entry.ModelSnapshotPreview;
        }

        // Check if we've reached the end after step
        if (_cursorPosition >= _timeline.Count - 1 && _currentState == DebuggerState.PlayingForward)
        {
            _currentState = DebuggerState.Paused;
        }
    }

    /// <summary>
    /// Step backward one entry in the timeline.
    /// </summary>
    public void StepBackward()
    {
        if (_timeline.Count == 0 || _cursorPosition <= 0)
            return;

        _cursorPosition--;
        if (_cursorPosition >= 0 && _cursorPosition < _timeline.Count)
        {
            var entry = _timeline[_cursorPosition];
            _currentModelSnapshotPreview = entry.ModelSnapshotPreview;
        }
    }

    /// <summary>
    /// Start playing forward through the timeline.
    /// </summary>
    public void Play()
    {
        _currentState = DebuggerState.PlayingForward;
    }

    /// <summary>
    /// Pause playback.
    /// </summary>
    public void Pause()
    {
        _currentState = DebuggerState.Paused;
    }

    /// <summary>
    /// Clear the entire timeline and reset state.
    /// </summary>
    public void ClearTimeline()
    {
        _timeline.Clear();
        _cursorPosition = -1;
        _currentModelSnapshotPreview = string.Empty;
        _currentState = DebuggerState.Recording;
        _sideEffectCount = 0;
    }

    /// <summary>
    /// Enable message capture.
    /// </summary>
    public void EnableCapture()
    {
        _isCapturing = true;
    }

    /// <summary>
    /// Disable message capture (messages are still dispatched, but not added to timeline).
    /// </summary>
    public void DisableCapture()
    {
        _isCapturing = false;
    }

    public void RecordSideEffect()
    {
        _sideEffectCount++;
    }

    // =====================
    // PRIVATE HELPERS
    // =====================

    private long GetMonotonicTimestamp()
    {
        var now = DateTime.UtcNow.Ticks;
        if (now <= _lastTimestamp)
        {
            return _lastTimestamp + 1;
        }
        return now;
    }

    private string SerializeMessageArgs(object? message)
    {
        try
        {
            if (message == null)
                return "{}";

            // Simple reflection-based serialization of message args and properties
            var messageType = message.GetType();
            var properties = messageType.GetProperties();
            
            if (properties.Length == 0)
                return "{}";

            var fields = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(message);
                    var serialized = System.Text.Json.JsonSerializer.Serialize(value);
                    fields[prop.Name] = serialized;
                }
                catch
                {
                    fields[prop.Name] = $"<{prop.PropertyType.Name}>";
                }
            }

            return System.Text.Json.JsonSerializer.Serialize(fields);
        }
        catch
        {
            return "{}";
        }
    }
}

#endif
