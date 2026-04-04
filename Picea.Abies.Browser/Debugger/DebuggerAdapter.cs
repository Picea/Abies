// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

using System.Text.Json;
using Picea.Abies.Debugger;

namespace Picea.Abies.Browser.Debugger;

/// <summary>
/// Transport layer for the time travel debugger.
/// Serializes user requests to JSON and deserializes C# responses.
/// CRITICAL: Contains ZERO replay logic. All state machine transitions live in C# (Mealy machine).
///
/// This adapter is a pure serialization/deserialization bridge between UI and backend.
/// The protocol DTOs (<see cref="DebuggerAdapterMessage"/>, <see cref="DebuggerAdapterResponse"/>,
/// <see cref="DebuggerAdapterTimelineEntry"/>) live in the shared <c>Picea.Abies.Debugger</c>
/// namespace so both Browser and Server can use them.
/// </summary>
public sealed class DebuggerAdapter
{
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

        if (!IsValidMessageType(msg.Type))
        {
            throw new ArgumentException(
                $"Unknown message type: {msg.Type}. Must be one of: jump-to-entry, step-forward, step-back, play, pause, clear-timeline, get-timeline, export-session, import-session.",
                nameof(request)
            );
        }

        return JsonSerializer.Serialize(msg, DebuggerAdapterJsonContext.Default.DebuggerAdapterMessage);
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
            var response = JsonSerializer.Deserialize(json, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse)
                ?? throw new InvalidOperationException("Failed to deserialize response.");

            if (string.IsNullOrWhiteSpace(response.Status))
            {
                throw new ArgumentException("Response JSON must contain a non-empty status.", nameof(json));
            }

            return response;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid response JSON: {ex.Message}", nameof(json), ex);
        }
    }

    private static bool IsValidMessageType(string type) =>
        type switch
        {
            "jump-to-entry" => true,
            "step-forward" => true,
            "step-back" => true,
            "play" => true,
            "pause" => true,
            "clear-timeline" => true,
            "get-timeline" => true,
            "export-session" => true,
            "import-session" => true,
            _ => false
        };
}

#endif
