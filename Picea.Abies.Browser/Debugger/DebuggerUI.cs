// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

using Picea.Abies.Debugger;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Browser.Debugger;

/// <summary>
/// Debugger UI component that mounts at a specific DOM element and wires user interactions
/// to the adapter for message dispatch.
/// 
/// CRITICAL: This class coordinates user input → message dispatching → response rendering.
/// It does NOT contain replay logic or runtime command execution.
/// 
/// Responsibilities:
/// - Mount/unmount at DOM element (id="abies-debugger-timeline")
/// - Wire button clicks and keyboard events to message dispatch
/// - Render timeline based on C# responses
/// - Provide test hooks for verification
/// </summary>
public sealed class DebuggerUI
{
    private const string _defaultMountPointId = "abies-debugger-timeline";

    private DebuggerUiModel _model = DebuggerUiProgram.Initialize(
        new DebuggerUiInit(_defaultMountPointId)).Item1;
    private Document _document;

    public DebuggerUI()
    {
        _document = DebuggerUiProgram.View(_model);
    }

    public bool IsMounted => _model.IsMounted;
    public int CurrentCursorPosition
    {
        get => _model.CursorPosition;
        set
        {
            Dispatch(new DebuggerUiCursorUpdated(value));
        }
    }
    public bool MainAppModified => _model.MainAppModified;

    internal Document CurrentDocument => _document;

    public string RenderHtml() => Render.Html(_document.Body);

    /// <summary>
    /// Event fired when a message is dispatched to the adapter (play, pause, step, jump, etc).
    /// </summary>
    public event Action<DebuggerAdapterMessage>? OnMessageDispatched;

    /// <summary>
    /// Initialize and mount the debugger UI at the specified DOM element ID.
    /// </summary>
    public void InitializeMount(string mountPointId)
    {
        var effectiveMountPointId = string.IsNullOrWhiteSpace(mountPointId)
            ? _defaultMountPointId
            : mountPointId;

        Dispatch(new DebuggerUiMounted(effectiveMountPointId));
    }

    /// <summary>
    /// Check if a UI element exists in the mounted debugger.
    /// </summary>
    public bool ContainsElement(string elementId)
    {
        return ContainsElementById(_document.Body, elementId);
    }

    /// <summary>
    /// Add a timeline entry to the debugger's timeline history.
    /// </summary>
    public void AddTimelineEntry(DebuggerTimelineEntry entry)
    {
        Dispatch(new DebuggerUiTimelineEntryAdded(entry));
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

        Dispatch(new DebuggerUiControlInvoked(messageType));

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
    /// - J: focus jump input (UI-only)
    /// - Escape: close/blur (UI-only)
    /// </summary>
    public void SimulateKeyboardEvent(string keyCode)
    {
        // Always notify the UI state machine about shortcut activity.
        Dispatch(new DebuggerUiKeyboardShortcutInvoked(keyCode));

        var messageType = keyCode switch
        {
            " " => "play",  // Space → play/pause toggle
            "ArrowRight" => "step-forward",
            "ArrowLeft" => "step-back",
            _ => null
        };

        if (messageType is null)
        {
            return;  // Unknown or UI-only key, no adapter message
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
        Dispatch(new DebuggerUiResponseApplied(response));
    }

    /// <summary>
    /// Get the currently highlighted/selected timeline entry.
    /// </summary>
    public DebuggerTimelineEntry? GetHighlightedEntry()
    {
        return _model.TimelineEntries.FirstOrDefault(e => e.Sequence == _model.CursorPosition);
    }

    /// <summary>
    /// Render the timeline UI based on current state.
    /// (In real implementation, this would update the actual DOM.)
    /// </summary>
    public void RenderTimeline(DebuggerAdapterResponse response)
    {
        UpdateFromResponse(response);
    }

    /// <summary>
    /// Synchronizes the debugger panel from the runtime debugger machine snapshot.
    /// </summary>
    public void SyncFromRuntimeDebugger(DebuggerMachine debugger)
    {
        var entries = debugger.Timeline
            .Select(entry => new DebuggerTimelineEntry
            {
                Sequence = checked((int)entry.Sequence),
                MessageType = entry.MessageType,
                ArgsPreview = entry.ArgsPreview,
                Timestamp = entry.Timestamp,
                ModelSnapshotPreview = entry.ModelSnapshotPreview
            })
            .ToArray();

        Dispatch(new DebuggerUiRuntimeSnapshotApplied(entries, debugger.CursorPosition));
    }

    private void Dispatch(DebuggerUiMessage message)
    {
        var (model, _) = DebuggerUiProgram.Transition(_model, message);
        _model = model;
        _document = DebuggerUiProgram.View(_model);
    }

    private static bool ContainsElementById(Node node, string targetId)
    {
        return node switch
        {
            Element element when element.Id == targetId => true,
            Element element => element.Children.Any(child => ContainsElementById(child, targetId)),
            LazyMemoNode lazy => ContainsElementById(lazy.CachedNode ?? lazy.Evaluate(), targetId),
            MemoNode memo => ContainsElementById(memo.CachedNode, targetId),
            _ => false
        };
    }
}

internal readonly record struct DebuggerUiInit(string MountPointId);

internal sealed record DebuggerUiModel(
    string MountPointId,
    bool IsMounted,
    int CursorPosition,
    string Status,
    IReadOnlyList<DebuggerTimelineEntry> TimelineEntries,
    bool MainAppModified
);

internal interface DebuggerUiMessage : Message;

internal sealed record DebuggerUiMounted(string MountPointId) : DebuggerUiMessage;

internal sealed record DebuggerUiTimelineEntryAdded(DebuggerTimelineEntry Entry) : DebuggerUiMessage;

internal sealed record DebuggerUiCursorUpdated(int CursorPosition) : DebuggerUiMessage;

internal sealed record DebuggerUiControlInvoked(string ControlType) : DebuggerUiMessage;

internal sealed record DebuggerUiKeyboardShortcutInvoked(string KeyCode) : DebuggerUiMessage;

internal sealed record DebuggerUiResponseApplied(DebuggerAdapterResponse Response) : DebuggerUiMessage;

internal sealed record DebuggerUiRuntimeSnapshotApplied(
    IReadOnlyList<DebuggerTimelineEntry> Entries,
    int CursorPosition
) : DebuggerUiMessage;

internal sealed class DebuggerUiProgram : Program<DebuggerUiModel, DebuggerUiInit>
{
    public static (DebuggerUiModel, Command) Initialize(DebuggerUiInit init) =>
        (new DebuggerUiModel(
            MountPointId: init.MountPointId,
            IsMounted: false,
            CursorPosition: 0,
            Status: "idle",
            TimelineEntries: [],
            MainAppModified: false),
         Commands.None);

    public static (DebuggerUiModel, Command) Transition(DebuggerUiModel model, Message message) =>
        message switch
        {
            DebuggerUiMounted mount =>
                (model with { MountPointId = mount.MountPointId, IsMounted = true }, Commands.None),

            DebuggerUiTimelineEntryAdded timelineAdded =>
                (model with { TimelineEntries = model.TimelineEntries.Append(timelineAdded.Entry).ToArray() }, Commands.None),

            DebuggerUiCursorUpdated cursor =>
                (model with { CursorPosition = cursor.CursorPosition }, Commands.None),

            DebuggerUiControlInvoked control =>
                (model with { Status = StatusFromControl(control.ControlType) }, Commands.None),

            DebuggerUiKeyboardShortcutInvoked key =>
                (model with { Status = StatusFromKeyboardShortcut(model.Status, key.KeyCode) }, Commands.None),

            DebuggerUiResponseApplied response =>
                (model with
                {
                    CursorPosition = response.Response.CursorPosition,
                    Status = response.Response.Status
                },
                 Commands.None),

            DebuggerUiRuntimeSnapshotApplied snapshot =>
                (model with
                {
                    TimelineEntries = snapshot.Entries,
                    CursorPosition = snapshot.CursorPosition
                },
                 Commands.None),

            _ => (model, Commands.None)
        };

    public static Document View(DebuggerUiModel model)
    {
        var timelineSummary = model.IsMounted
            ? $"Timeline: {model.TimelineEntries.Count} entries"
            : "Timeline: not mounted";

        var logText = model.IsMounted
            ? "Debugger timeline initialized"
            : "Debugger not mounted";

        return new Document(
            "Abies Debugger",
            div([id(model.MountPointId), class_("abies-debugger-root")],
            [
                div([id("control-bar"), class_("debugger-control-bar")],
                [
                    button([
                        id("play-button"),
                        data("abies-debugger-intent", "play"),
                        data("abies-debugger-payload", "{}")
                    ], [text("▶ Play")]),
                    button([
                        id("pause-button"),
                        data("abies-debugger-intent", "pause"),
                        data("abies-debugger-payload", "{}")
                    ], [text("⏸ Pause")]),
                    button([
                        id("step-forward-button"),
                        data("abies-debugger-intent", "step-forward"),
                        data("abies-debugger-payload", "{}")
                    ], [text("→ Step")]),
                    button([
                        id("step-back-button"),
                        data("abies-debugger-intent", "step-back"),
                        data("abies-debugger-payload", "{}")
                    ], [text("← Back")]),
                    input([
                        id("jump-input"),
                        type("number"),
                        min("0"),
                        placeholder("Jump to entry..."),
                        data("abies-debugger-intent", "jump-to-entry")
                    ]),
                    button([
                        id("clear-button"),
                        data("abies-debugger-intent", "clear-timeline"),
                        data("abies-debugger-payload", "{}")
                    ], [text("✕ Clear")])
                ]),
                div([id("message-log"), class_("debugger-message-log")],
                [
                    div([class_("log-entry")], [text(logText)])
                ]),
                div([id("timeline-inspector"), class_("debugger-timeline-inspector")],
                [
                    text(timelineSummary)
                ]),
                ul([id("timeline-list"), class_("debugger-timeline-list")],
                    model.TimelineEntries
                        .Select(entry => li(
                            [data("sequence", entry.Sequence.ToString())],
                            [
                                text($"#{entry.Sequence} {entry.MessageType} {entry.ArgsPreview}")
                            ]))
                        .ToArray())
            ]));
    }

    public static Subscription Subscriptions(DebuggerUiModel _) =>
        new Subscription.None();

    private static string StatusFromControl(string controlType) =>
        controlType switch
        {
            "play" => "playing",
            "pause" => "paused",
            "clear-timeline" => "recording",
            _ => "paused"
        };

    private static string StatusFromKeyboardShortcut(string currentStatus, string keyCode) =>
        keyCode switch
        {
            " " => currentStatus == "playing" ? "paused" : "playing",
            "ArrowRight" => "paused",
            "ArrowLeft" => "paused",
            _ => currentStatus
        };
}

/// <summary>
/// Timeline entry representing a single message + model snapshot history point.
/// Used for rendering the timeline UI.
/// </summary>
public sealed record DebuggerTimelineEntry
{
    public int Sequence { get; init; }
    public required string MessageType { get; init; }
    public required string ArgsPreview { get; init; }
    public long Timestamp { get; init; }
    public required string ModelSnapshotPreview { get; init; }
}

#endif
