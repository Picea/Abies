using System.Text.Json;
using System.Text.Json.Serialization;

namespace Picea.Abies.Testing;

/// <summary>
/// Deterministic replay schema contract used by <see cref="TestHarness{TProgram, TModel, TArgument}"/>.
/// </summary>
public static class TestHarnessReplaySchema
{
    /// <summary>
    /// Current replay session schema version.
    /// </summary>
    public const string Version1 = "v1";
}

/// <summary>
/// Deterministic replay session payload (v1).
/// </summary>
public sealed record TestHarnessReplaySessionV1
{
    /// <summary>
    /// Schema version for compatibility checks.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Explicit metadata for traceability and deterministic replay diagnostics.
    /// </summary>
    [JsonPropertyName("metadata")]
    public required TestHarnessReplayMetadataV1 Metadata { get; init; }

    /// <summary>
    /// Ordered message entries to replay.
    /// </summary>
    [JsonPropertyName("entries")]
    public required IReadOnlyList<TestHarnessReplayEntryV1> Entries { get; init; }

    internal void Validate()
    {
        if (SchemaVersion != TestHarnessReplaySchema.Version1)
        {
            throw new InvalidOperationException(
                $"Unsupported session schema version '{SchemaVersion}'. Expected '{TestHarnessReplaySchema.Version1}'.");
        }

        if (Metadata is null)
        {
            throw new InvalidOperationException("Session payload is invalid: metadata is required.");
        }

        Metadata.Validate();

        if (Entries is null)
        {
            throw new InvalidOperationException("Session payload is invalid: entries is required.");
        }

        for (var index = 0; index < Entries.Count; index++)
        {
            var entry = Entries[index];
            if (entry is null)
            {
                throw new InvalidOperationException($"Session payload is invalid: entry {index} is required.");
            }

            entry.Validate(index);
        }
    }
}

/// <summary>
/// Replay session metadata.
/// </summary>
public sealed record TestHarnessReplayMetadataV1
{
    /// <summary>
    /// Stable session identifier.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Program identity that produced the replay payload.
    /// </summary>
    [JsonPropertyName("programName")]
    public required string ProgramName { get; init; }

    /// <summary>
    /// Program version emitted by the recorder.
    /// </summary>
    [JsonPropertyName("programVersion")]
    public required string ProgramVersion { get; init; }

    /// <summary>
    /// UTC unix milliseconds when the session was recorded.
    /// </summary>
    [JsonPropertyName("recordedAtUnixMs")]
    public required long RecordedAtUnixMs { get; init; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            throw new InvalidOperationException("Session payload is invalid: metadata.sessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(ProgramName))
        {
            throw new InvalidOperationException("Session payload is invalid: metadata.programName is required.");
        }

        if (string.IsNullOrWhiteSpace(ProgramVersion))
        {
            throw new InvalidOperationException("Session payload is invalid: metadata.programVersion is required.");
        }

        if (RecordedAtUnixMs < 0)
        {
            throw new InvalidOperationException("Session payload is invalid: metadata.recordedAtUnixMs must be zero or greater.");
        }
    }
}

/// <summary>
/// One replay entry in the deterministic v1 schema.
/// </summary>
public sealed record TestHarnessReplayEntryV1
{
    /// <summary>
    /// Deterministic sequence number. Must match the entry index.
    /// </summary>
    [JsonPropertyName("sequence")]
    public required int Sequence { get; init; }

    /// <summary>
    /// Message identity used by entry-to-message mapping.
    /// </summary>
    [JsonPropertyName("messageType")]
    public required string MessageType { get; init; }

    /// <summary>
    /// Message payload object consumed by entry-to-message mapping.
    /// </summary>
    [JsonPropertyName("payload")]
    public required JsonElement Payload { get; init; }

    internal void Validate(int index)
    {
        if (Sequence != index)
        {
            throw new InvalidOperationException(
                $"Session payload is invalid: entry {index} sequence must be {index}, but was {Sequence}.");
        }

        if (string.IsNullOrWhiteSpace(MessageType))
        {
            throw new InvalidOperationException($"Session payload is invalid: entry {index} messageType is required.");
        }

        if (Payload.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Session payload is invalid: entry {index} payload must be a JSON object.");
        }
    }
}
