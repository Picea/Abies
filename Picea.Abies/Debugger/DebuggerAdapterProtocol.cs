#if DEBUG

using System.Text.Json.Serialization;

namespace Picea.Abies.Debugger;

/// <summary>
/// Source-generated JSON serializer context for all debugger protocol types.
/// Required for WASM/AOT where reflection-based serialization is disabled.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DebuggerAdapterMessage))]
[JsonSerializable(typeof(DebuggerAdapterResponse))]
[JsonSerializable(typeof(DebuggerAdapterTimelineEntry))]
[JsonSerializable(typeof(DebuggerAppIdentity))]
[JsonSerializable(typeof(DebuggerAdapterSession))]
[JsonSerializable(typeof(DebuggerSessionImportRequest))]
public partial class DebuggerAdapterJsonContext : JsonSerializerContext;

/// <summary>
/// Serializable message type for debugger requests.
/// Part of the JSON bridge protocol shared by both WASM and Server transports.
/// </summary>
public record DebuggerAdapterMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("entryId")]
    public int? EntryId { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

/// <summary>
/// Serializable response type from the C# debugger backend.
/// Part of the JSON bridge protocol shared by both WASM and Server transports.
/// </summary>
public record DebuggerAdapterResponse
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("appVersion")]
    public required string AppVersion { get; init; }

    [JsonPropertyName("cursorPosition")]
    public int CursorPosition { get; init; }

    [JsonPropertyName("timelineSize")]
    public int TimelineSize { get; init; }

    [JsonPropertyName("atStart")]
    public bool AtStart { get; init; }

    [JsonPropertyName("atEnd")]
    public bool AtEnd { get; init; }

    [JsonPropertyName("currentEntry")]
    public DebuggerAdapterTimelineEntry? CurrentEntry { get; init; }

    [JsonPropertyName("initialModelSnapshotPreview")]
    public required string InitialModelSnapshotPreview { get; init; }

    [JsonPropertyName("modelSnapshotPreview")]
    public required string ModelSnapshotPreview { get; init; }

    [JsonPropertyName("previousModelSnapshotPreview")]
    public string? PreviousModelSnapshotPreview { get; init; }

    /// <summary>
    /// Full timeline entries. Only populated for <c>get-timeline</c> and <c>clear-timeline</c> commands.
    /// </summary>
    [JsonPropertyName("timelineEntries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DebuggerAdapterTimelineEntry>? TimelineEntries { get; init; }

    /// <summary>
    /// Human-readable error text for invalid debugger commands and import validation failures.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Exported debugger session payload. Populated for <c>export-session</c>.
    /// </summary>
    [JsonPropertyName("session")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DebuggerAdapterSession? Session { get; init; }
}

/// <summary>
/// Serializable timeline entry for the debugger UI.
/// </summary>
public record DebuggerAdapterTimelineEntry
{
    [JsonPropertyName("sequence")]
    public long Sequence { get; init; }

    [JsonPropertyName("messageType")]
    public required string MessageType { get; init; }

    [JsonPropertyName("argsPreview")]
    public required string ArgsPreview { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("patchCount")]
    public int PatchCount { get; init; }

    [JsonPropertyName("modelSnapshotPreview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModelSnapshotPreview { get; init; }
}

/// <summary>
/// Runtime identity used for debugger session compatibility checks.
/// </summary>
public record DebuggerAppIdentity
{
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("appVersion")]
    public required string AppVersion { get; init; }
}

/// <summary>
/// Exportable/importable debugger session data for the C# runtime bridge protocol.
/// This shape is intentionally distinct from the browser-only payload used by debugger.js.
/// </summary>
public record DebuggerAdapterSession
{
    [JsonPropertyName("app")]
    public required DebuggerAppIdentity App { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("cursorPosition")]
    public int CursorPosition { get; init; }

    [JsonPropertyName("initialModelSnapshotPreview")]
    public string InitialModelSnapshotPreview { get; init; } = string.Empty;

    [JsonPropertyName("timelineEntries")]
    public required IReadOnlyList<DebuggerAdapterTimelineEntry> TimelineEntries { get; init; }
}

/// <summary>
/// Wrapper request for importing a debugger session.
/// </summary>
public record DebuggerSessionImportRequest
{
    [JsonPropertyName("session")]
    public DebuggerAdapterSession? Session { get; init; }
}

#endif
