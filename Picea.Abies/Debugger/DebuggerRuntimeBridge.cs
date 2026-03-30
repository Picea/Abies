
#if DEBUG

using System.Reflection;
using System.Text.Json;

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
        DebuggerMachine debugger,
        DebuggerAppIdentity? appIdentity = null)
    {
        appIdentity ??= ResolveCurrentAppIdentity();
        var includeTimeline = false;
        DebuggerAdapterSession? exportedSession = null;

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
            case "export-session":
                includeTimeline = true;
                exportedSession = debugger.ExportSession(appIdentity);
                break;
            case "import-session":
                if (!TryReadImportSession(message.Data, out var importedSession, out var readError))
                {
                    return BuildErrorResponse(debugger, appIdentity, readError ?? "Debugger session import rejected: malformed payload.");
                }

                var metadataError = ValidateImportMetadata(importedSession, appIdentity);
                if (metadataError is not null)
                {
                    return BuildErrorResponse(debugger, appIdentity, metadataError);
                }

                debugger.ImportSession(importedSession);
                includeTimeline = true;
                break;
        }

        return BuildResponse(debugger, appIdentity, includeTimeline, exportedSession);
    }

    private static DebuggerAdapterResponse BuildResponse(
        DebuggerMachine debugger,
        DebuggerAppIdentity appIdentity,
        bool includeTimeline,
        DebuggerAdapterSession? exportedSession = null)
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
            AppName = appIdentity.AppName,
            AppVersion = appIdentity.AppVersion,
            CursorPosition = cursor,
            TimelineSize = timeline.Count,
            AtStart = debugger.AtStart,
            AtEnd = debugger.AtEnd,
            CurrentEntry = currentEntry,
            InitialModelSnapshotPreview = debugger.InitialModelSnapshotPreview,
            ModelSnapshotPreview = debugger.CurrentModelSnapshotPreview,
            PreviousModelSnapshotPreview = previousModelPreview,
            TimelineEntries = timelineEntries,
            Session = exportedSession
        };
    }

    private static DebuggerAdapterResponse BuildErrorResponse(DebuggerMachine debugger, DebuggerAppIdentity appIdentity, string error) =>
        BuildResponse(debugger, appIdentity, includeTimeline: false) with
        {
            Status = "error",
            Error = error
        };

    private static bool TryReadImportSession(
        object? data,
        out DebuggerAdapterSession session,
        out string? error)
    {
        session = null!;
        error = null;

        if (data is DebuggerSessionImportRequest typedRequest && typedRequest.Session is not null)
        {
            session = typedRequest.Session;
            return true;
        }

        if (data is DebuggerAdapterSession typedSession)
        {
            session = typedSession;
            return true;
        }

        if (data is JsonElement jsonElement)
        {
            try
            {
                if (jsonElement.ValueKind is JsonValueKind.Object)
                {
                    var request = jsonElement.Deserialize(DebuggerAdapterJsonContext.Default.DebuggerSessionImportRequest);
                    if (request?.Session is not null)
                    {
                        session = request.Session;
                        return true;
                    }

                    var direct = jsonElement.Deserialize(DebuggerAdapterJsonContext.Default.DebuggerAdapterSession);
                    if (direct is not null)
                    {
                        session = direct;
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                error = "Debugger session import rejected: malformed JSON payload.";
                return false;
            }
        }

        error = "Debugger session import rejected: malformed payload. Expected { session: { app, versioned metadata, timelineEntries } }.";
        return false;
    }

    private static string? ValidateImportMetadata(DebuggerAdapterSession session, DebuggerAppIdentity expected)
    {
        if (session.App is null || string.IsNullOrWhiteSpace(session.App.AppName) || string.IsNullOrWhiteSpace(session.App.AppVersion))
        {
            return "Debugger session import rejected: missing app metadata (appName/appVersion).";
        }

        if (!string.Equals(session.App.AppName, expected.AppName, StringComparison.Ordinal))
        {
            return $"Debugger session import rejected: app mismatch. Expected '{expected.AppName}', received '{session.App.AppName}'.";
        }

        if (!string.Equals(session.App.AppVersion, expected.AppVersion, StringComparison.Ordinal))
        {
            return $"Debugger session import rejected: version mismatch. Expected '{expected.AppVersion}', received '{session.App.AppVersion}'.";
        }

        return null;
    }



    private static DebuggerAppIdentity ResolveCurrentAppIdentity()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var name = entryAssembly?.GetName().Name;
        var version = entryAssembly?.GetName().Version?.ToString();

        return new DebuggerAppIdentity
        {
            AppName = string.IsNullOrWhiteSpace(name) ? AppDomain.CurrentDomain.FriendlyName : name,
            AppVersion = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version
        };
    }

    private static DebuggerAdapterTimelineEntry MapEntry(TimestampedEntry entry) =>
        new()
        {
            Sequence = entry.Sequence,
            MessageType = entry.MessageType,
            ArgsPreview = entry.ArgsPreview,
            Timestamp = entry.Timestamp,
            PatchCount = entry.PatchCount,
            ModelSnapshotPreview = entry.ModelSnapshotPreview
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
