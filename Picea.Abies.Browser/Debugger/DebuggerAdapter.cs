// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Picea.Abies.Browser.Debugger;

/// <summary>
/// Transport layer for the time travel debugger.
/// Serializes user requests to JSON and deserializes C# responses.
/// CRITICAL: Contains ZERO replay logic. All state machine transitions live in C# (Mealy machine).
/// 
/// This adapter is a pure serialization/deserialization bridge between UI and backend.
/// </summary>
public sealed class DebuggerAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a debugger message (jump, step, play, etc.) to JSON.
    /// Validates message type against the contract.
    /// 
    /// NO SIDE EFFECTS: Does not modify state, execute commands, or transition state machine.
    /// </summary>
    public string SerializeMessage(object request)
    {
        if (request is not DebuggerAdapterMessage msg)
        {
            throw new ArgumentException(
                $"Unknown message type: {request?.GetType().Name}. Expected DebuggerAdapterMessage.",
                nameof(request)
            );
        }

        // Validate message type is known
        if (!IsValidMessageType(msg.Type))
        {
            throw new ArgumentException(
                $"Unknown message type: {msg.Type}. Must be one of: jump-to-entry, step-forward, step-back, play, pause, clear-timeline.",
                nameof(request)
            );
        }

        // Serialize to JSON
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        return json;
    }

    /// <summary>
    /// Deserializes a C# response JSON into a strongly-typed object.
    /// 
    /// NO SIDE EFFECTS: Does not replay commands or modify state machine.
    /// </summary>
    public DebuggerAdapterResponse DeserializeResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Response JSON cannot be empty.", nameof(json));
        }

        try
        {
            var normalizedJson = NormalizeResponseJson(json);
            var response = JsonSerializer.Deserialize<DebuggerAdapterResponse>(normalizedJson, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize response.");

            if (string.IsNullOrWhiteSpace(response.Status))
            {
                throw new ArgumentException("Response JSON must contain a non-empty status.", nameof(json));
            }

            if (response.ModelSnapshotPreview is null)
            {
                throw new ArgumentException("Response JSON must contain modelSnapshotPreview.", nameof(json));
            }

            return response;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid response JSON: {ex.Message}", nameof(json), ex);
        }
    }

    private static string NormalizeResponseJson(string json)
    {
        var node = JsonNode.Parse(json);
        return node?.ToJsonString(JsonOptions) ?? json;
    }

    /// <summary>
    /// Validates that a message type is part of the contract.
    /// </summary>
    private static bool IsValidMessageType(string type)
    {
        return type switch
        {
            "jump-to-entry" => true,
            "step-forward" => true,
            "step-back" => true,
            "play" => true,
            "pause" => true,
            "clear-timeline" => true,
            _ => false
        };
    }
}

/// <summary>
/// Serializable message type for debugger requests.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
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
/// Serializable response type from C# debugger backend.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public record DebuggerAdapterResponse
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("cursorPosition")]
    public int CursorPosition { get; init; }

    [JsonPropertyName("timelineSize")]
    public int TimelineSize { get; init; }

    [JsonPropertyName("modelSnapshotPreview")]
    public required string ModelSnapshotPreview { get; init; }
}

#endif
