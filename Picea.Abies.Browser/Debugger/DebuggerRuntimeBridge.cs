// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

using Picea.Abies.Debugger;

namespace Picea.Abies.Browser.Debugger;

/// <summary>
/// Runtime bridge that applies debugger adapter commands to the debugger machine.
/// Keeps runtime command execution outside the UI rendering class.
/// </summary>
public static class DebuggerRuntimeBridge
{
    public static DebuggerAdapterResponse Execute(
        DebuggerAdapterMessage message,
        DebuggerMachine debugger)
    {
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
                break;
        }

        return new DebuggerAdapterResponse
        {
            Status = MapStatus(debugger.CurrentState),
            CursorPosition = debugger.CursorPosition,
            TimelineSize = debugger.Timeline.Count,
            ModelSnapshotPreview = debugger.CurrentModelSnapshotPreview
        };
    }

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
