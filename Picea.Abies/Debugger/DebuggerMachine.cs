// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

namespace Picea.Abies.Debugger;

public enum DebuggerState
{
    Recording,
    Paused,
    PlayingForward
}

/// <summary>
/// Single timeline entry with sequence, message metadata, timestamp, model snapshot preview,
/// and the number of DOM patches applied for this transition.
/// </summary>
public sealed record TimestampedEntry(
    long Sequence,
    string MessageType,
    string ArgsPreview,
    long Timestamp,
    string ModelSnapshotPreview,
    int PatchCount
);

/// <summary>
/// Stateful Mealy machine managing debugger state transitions and replay over a mutable timeline buffer.
/// Internally mutates its own state but performs deterministic transitions with no side effects on the host application during replay.
/// </summary>
public sealed class DebuggerMachine
{
    /// <summary>
    /// Wraps a model snapshot to satisfy the non-nullable class constraint on <see cref="RingBuffer{T}"/>.
    /// Debug-only - allocation overhead is negligible.
    /// </summary>
    private sealed record ModelSnapshotBox(object? Snapshot);

    private readonly RingBuffer<TimestampedEntry> _timeline;
    private readonly RingBuffer<ModelSnapshotBox> _timelineModelSnapshots;
    private DebuggerState _currentState;
    private int _cursorPosition;
    private string _currentModelSnapshotPreview;
    private object? _currentModelSnapshot;
    private bool _isCapturing;
    private long _lastTimestamp;
    private int _sideEffectCount;
    private string _initialModelSnapshotPreview;
    private object? _initialModelSnapshot;

    public event Action? TimelineChanged;

    public DebuggerMachine(int capacity = 10000)
    {
        _timeline = new RingBuffer<TimestampedEntry>(capacity);
        _timelineModelSnapshots = new RingBuffer<ModelSnapshotBox>(capacity);
        _currentState = DebuggerState.Recording;
        _cursorPosition = -1;
        _currentModelSnapshotPreview = string.Empty;
        _currentModelSnapshot = null;
        _isCapturing = true;
        _lastTimestamp = 0;
        _sideEffectCount = 0;
        _initialModelSnapshotPreview = string.Empty;
        _initialModelSnapshot = null;
    }

    public DebuggerState CurrentState => _currentState;
    public IReadOnlyList<TimestampedEntry> Timeline => _timeline.Entries;
    public int CursorPosition => _cursorPosition;
    public string CurrentModelSnapshotPreview => _currentModelSnapshotPreview;
    public object? CurrentModelSnapshot => _currentModelSnapshot;
    public int SideEffectCount => _sideEffectCount;

    /// <summary>
    /// Preview of the initial model state before any messages were dispatched.
    /// </summary>
    public string InitialModelSnapshotPreview => _initialModelSnapshotPreview;

    /// <summary>
    /// Full initial model snapshot for time-travel replay to the "before first message" state.
    /// </summary>
    public object? InitialModelSnapshot => _initialModelSnapshot;

    /// <summary>
    /// True when the cursor is at position 0 or the timeline is empty.
    /// </summary>
    public bool AtStart => _cursorPosition <= 0;

    /// <summary>
    /// True when the cursor is at the last entry or the timeline is empty.
    /// </summary>
    public bool AtEnd => _timeline.Count == 0 || _cursorPosition >= _timeline.Count - 1;

    /// <summary>
    /// Captures a message and transitions from Recording.
    /// </summary>
    public void CaptureMessage(object message, string modelSnapshotPreview, object? modelSnapshot = null, int patchCount = 0)
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
            ModelSnapshotPreview: modelSnapshotPreview,
            PatchCount: patchCount
        );

        _timeline.Add(entry);
        _timelineModelSnapshots.Add(new ModelSnapshotBox(modelSnapshot));

        // Update cursor if we're in Recording state
        if (_currentState == DebuggerState.Recording)
        {
            _cursorPosition = (int)(_timeline.NextSequence - 1);
            _currentModelSnapshotPreview = modelSnapshotPreview;
            _currentModelSnapshot = modelSnapshot;
        }

        _lastTimestamp = timestamp;
        TimelineChanged?.Invoke();
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
            _currentModelSnapshot = _timelineModelSnapshots[targetCursor].Snapshot;
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
            _currentModelSnapshot = _timelineModelSnapshots[_cursorPosition].Snapshot;
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
            _currentModelSnapshot = _timelineModelSnapshots[_cursorPosition].Snapshot;
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
        _timelineModelSnapshots.Clear();
        _cursorPosition = -1;
        _currentModelSnapshotPreview = string.Empty;
        _currentModelSnapshot = null;
        _currentState = DebuggerState.Recording;
        _sideEffectCount = 0;
        _initialModelSnapshotPreview = string.Empty;
        _initialModelSnapshot = null;
        TimelineChanged?.Invoke();
    }

    /// <summary>
    /// Captures the initial model state before any messages have been dispatched.
    /// Called once after the debugger is initialized to establish the "before first message" baseline.
    /// </summary>
    public void CaptureInitialModel(string modelSnapshotPreview, object? modelSnapshot = null)
    {
        _initialModelSnapshotPreview = modelSnapshotPreview;
        _initialModelSnapshot = modelSnapshot;
    }

    /// <summary>
    /// Gets the model snapshot preview that represents the state <em>before</em> the entry at the given index.
    /// For index 0, returns the initial model preview. For index N, returns entry[N-1]'s snapshot preview.
    /// </summary>
    public string GetPreviousModelSnapshotPreview(int index)
    {
        if (index <= 0 || _timeline.Count == 0)
            return _initialModelSnapshotPreview;

        var previousIndex = Math.Min(index - 1, _timeline.Count - 1);
        return _timeline[previousIndex].ModelSnapshotPreview;
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

    /// <summary>
    /// Builds an exportable debugger session payload with app identity metadata.
    /// </summary>
    public DebuggerAdapterSession ExportSession(DebuggerAppIdentity appIdentity)
    {
        ArgumentNullException.ThrowIfNull(appIdentity);

        return new DebuggerAdapterSession
        {
            App = appIdentity,
            Status = _currentState switch
            {
                DebuggerState.Recording => "recording",
                DebuggerState.Paused => "paused",
                DebuggerState.PlayingForward => "playing",
                _ => "paused"
            },
            CursorPosition = _cursorPosition,
            InitialModelSnapshotPreview = _initialModelSnapshotPreview,
            TimelineEntries = _timeline
                .Entries
                .Select(entry => new DebuggerAdapterTimelineEntry
                {
                    Sequence = entry.Sequence,
                    MessageType = entry.MessageType,
                    ArgsPreview = entry.ArgsPreview,
                    Timestamp = entry.Timestamp,
                    PatchCount = entry.PatchCount,
                    ModelSnapshotPreview = entry.ModelSnapshotPreview
                })
                .ToArray()
        };
    }

    /// <summary>
    /// Replaces the current timeline with an imported debugger session.
    /// </summary>
    public void ImportSession(DebuggerAdapterSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _timeline.Clear();
        _timelineModelSnapshots.Clear();

        _initialModelSnapshotPreview = session.InitialModelSnapshotPreview ?? string.Empty;
        _initialModelSnapshot = _initialModelSnapshotPreview;
        _currentModelSnapshot = _initialModelSnapshot;
        _sideEffectCount = 0;

        foreach (var entry in session.TimelineEntries)
        {
            var importedEntry = new TimestampedEntry(
                Sequence: entry.Sequence,
                MessageType: entry.MessageType,
                ArgsPreview: entry.ArgsPreview,
                Timestamp: entry.Timestamp,
                ModelSnapshotPreview: entry.ModelSnapshotPreview ?? string.Empty,
                PatchCount: entry.PatchCount);

            _timeline.Add(importedEntry);
            _timelineModelSnapshots.Add(new ModelSnapshotBox(importedEntry.ModelSnapshotPreview));
            _lastTimestamp = Math.Max(_lastTimestamp, entry.Timestamp);
        }

        _cursorPosition = _timeline.Count == 0
            ? -1
            : Math.Clamp(session.CursorPosition, 0, _timeline.Count - 1);

        if (_cursorPosition >= 0)
        {
            _currentModelSnapshotPreview = _timeline[_cursorPosition].ModelSnapshotPreview;
            _currentModelSnapshot = _timelineModelSnapshots[_cursorPosition].Snapshot;
        }
        else
        {
            _currentModelSnapshotPreview = _initialModelSnapshotPreview;
            _currentModelSnapshot = _initialModelSnapshot;
        }

        // Never restore "playing" state — imported sessions always land in Paused.
        // If we restored "playing", the JS bridge would see status="playing" and treat
        // the first Play button click as a Pause, leaving the timeline stuck at the end.
        _currentState = session.Status == "recording"
            ? DebuggerState.Recording
            : DebuggerState.Paused;
        _isCapturing = true;
        TimelineChanged?.Invoke();
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

            var fields = new Dictionary<string, string>();
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
