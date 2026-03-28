
#if DEBUG

namespace Picea.Abies.Debugger;

/// <summary>
/// Runtime bridge that applies debugger adapter commands to the debugger machine.
/// Shared between WASM and Server transports — no platform-specific dependencies.
/// </summary>
public static class DebuggerRuntimeBridge
{
    /// <summary>
    /// Executes a debugger command against the machine and builds a full response.
    /// </summary>
    public static DebuggerAdapterResponse Execute(
        DebuggerAdapterMessage message,
        DebuggerMachine debugger)
    {
        var includeTimeline = false;

        switch (message.Type)
        {
            case "jump-to-entry":
                if (message.EntryId is int entryId)
                {
                    debugger.Jump(entryId);
                }
                break;
            case "step-forward":
                debugger.StepForward();
                break;
            case "step-back":
                debugger.StepBackward();
                break;
            case "play":
                debugger.Play();
                break;
            case "pause":
                debugger.Pause();
                break;
            case "clear-timeline":
                debugger.ClearTimeline();
                includeTimeline = true;
                break;
            case "get-timeline":
                includeTimeline = true;
                break;
        }

        return BuildResponse(debugger, includeTimeline);
    }

    private static DebuggerAdapterResponse BuildResponse(DebuggerMachine debugger, bool includeTimeline)
    {
        var cursor = debugger.CursorPosition;
        var timeline = debugger.Timeline;

        DebuggerAdapterTimelineEntry? currentEntry = null;
        string? previousModelPreview = null;

        if (cursor >= 0 && cursor < timeline.Count)
        {
            var entry = timeline[cursor];
            currentEntry = MapEntry(entry);
            previousModelPreview = debugger.GetPreviousModelSnapshotPreview(cursor);
        }

        IReadOnlyList<DebuggerAdapterTimelineEntry>? timelineEntries = null;
        if (includeTimeline)
        {
            timelineEntries = timeline.Select(MapEntry).ToList();
        }

        return new DebuggerAdapterResponse
        {
            Status = MapStatus(debugger.CurrentState),
            CursorPosition = cursor,
            TimelineSize = timeline.Count,
            AtStart = debugger.AtStart,
            AtEnd = debugger.AtEnd,
            CurrentEntry = currentEntry,
            ModelSnapshotPreview = debugger.CurrentModelSnapshotPreview,
            PreviousModelSnapshotPreview = previousModelPreview,
            TimelineEntries = timelineEntries
        };
    }

    private static DebuggerAdapterTimelineEntry MapEntry(TimestampedEntry entry) =>
        new()
        {
            Sequence = entry.Sequence,
            MessageType = entry.MessageType,
            ArgsPreview = entry.ArgsPreview,
            Timestamp = entry.Timestamp,
            PatchCount = entry.PatchCount
        };

    private static string MapStatus(DebuggerState state) =>
        state switch
        {
            DebuggerState.Recording => "recording",
            DebuggerState.Paused => "paused",
            DebuggerState.PlayingForward => "playing",
            _ => "paused"
        };
}

#endif
