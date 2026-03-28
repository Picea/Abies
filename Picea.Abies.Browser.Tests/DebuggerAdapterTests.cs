// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Picea.Abies.Browser.Debugger;
using Picea.Abies.Debugger;

namespace Picea.Abies.Browser.Tests;

/// <summary>
/// Validates the JS Adapter Layer contract for the time travel debugger.
/// Purpose: Verify that the adapter correctly serializes/deserializes debugger messages
/// (jump, step-forward, step-back, play, pause, clear) and does NOT execute replay logic.
/// 
/// The adapter is a TRANSPORT-ONLY layer: JSON serialization/deserialization, no state machine transitions.
/// All replay logic lives in C# (Mealy machine).
/// 
/// Test Strategy: Option A (C# unit tests with mocking)
/// - Mock the JS bridge via Moq or manual mock
/// - Test the C#→JS message serialization contract
/// - Validate that adapter does NOT contain state machine logic
/// </summary>
public class DebuggerAdapterTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    /// <summary>
    /// Test 1a: When user requests a "jump to entry" action, the adapter serializes the request
    /// to a valid JSON message conforming to the bridge contract.
    /// 
    /// Validates the seam: UI message → JSON serialization → C# transport contract.
    /// </summary>
    [Test]
    public async Task AdapterSerializesJumpMessageToJSON_WhenUserRequestedJump()
    {
        // Arrange
        var adapter = new Picea.Abies.Browser.Debugger.DebuggerAdapter();
        
        var jumpRequest = new DebuggerAdapterMessage
        {
            Type = "jump-to-entry",
            EntryId = 5
        };

        // Act
        var serializedJson = adapter.SerializeMessage(jumpRequest);

        // Assert
        await Assert.That(serializedJson).IsNotNull();
        await Assert.That(serializedJson).IsNotEmpty();
        
        // Verify JSON structure
        var parsed = JsonDocument.Parse(serializedJson);
        var root = parsed.RootElement;
        
        await Assert.That(root.GetProperty("type").GetString()).IsEqualTo("jump-to-entry");
        await Assert.That(root.GetProperty("entryId").GetInt32()).IsEqualTo(5);

        // Verify transport-only behavior remains deterministic.
        var serializedAgain = adapter.SerializeMessage(jumpRequest);
        await Assert.That(serializedAgain).IsEqualTo(serializedJson);
    }

    /// <summary>
    /// Test 1b: When adapter receives a C# response containing timeline update data,
    /// it deserializes the response to a strongly-typed object WITHOUT attempting to
    /// restore model state or execute any replay logic (C# Mealy machine owns that).
    /// 
    /// Validates the seam: C# response → JSON deserialization → no replay side effects.
    /// </summary>
    [Test]
    public async Task AdapterDeserializesJumpResponseFromC_WithoutReplayingCommands()
    {
        // Arrange
        var adapter = new Picea.Abies.Browser.Debugger.DebuggerAdapter();

        var c_Response = JsonSerializer.Serialize(new DebuggerAdapterResponse
        {
            Status = "paused",
            CursorPosition = 5,
            TimelineSize = 10,
            ModelSnapshotPreview = "{\"count\":5,\"items\":[1,2,3]}"
        }, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);

        // Act
        var response = adapter.DeserializeResponse(c_Response);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response.Status).IsEqualTo("paused");
        await Assert.That(response.CursorPosition).IsEqualTo(5);
        await Assert.That(response.TimelineSize).IsEqualTo(10);
        
        // CRITICAL: Adapter remains transport-only and can continue serializing messages.
        var probeMessage = new DebuggerAdapterMessage { Type = "pause" };
        var probeJson = adapter.SerializeMessage(probeMessage);
        await Assert.That(probeJson).Contains("\"type\":\"pause\"");
    }

    /// <summary>
    /// Test 1c: When adapter receives a malformed or unknown message type,
    /// it either throws ArgumentException with a descriptive message OR returns a well-formed error envelope.
    /// This validates input validation at the transport boundary.
    /// 
    /// Validates the seam: Error handling for invalid messages, graceful failure.
    /// </summary>
    [Test]
    public async Task AdapterDiscardsUnknownMessageTypes_WhenMalformedInputReceived()
    {
        // Arrange
        var adapter = new Picea.Abies.Browser.Debugger.DebuggerAdapter();
        
        var malformedRequest = new DebuggerAdapterMessage
        {
            Type = "invalid-command-type",  // NOT in contract (jump, step-forward, play, etc)
            Data = null
        };

        // Act & Assert: Either throws OR returns error envelope
        try
        {
            var serialized = adapter.SerializeMessage(malformedRequest);
            
            // If no exception, verify error envelope
            var parsed = JsonDocument.Parse(serialized);
            var root = parsed.RootElement;
            
            await Assert.That(root.TryGetProperty("error", out _)).IsTrue();
            await Assert.That(root.GetProperty("error").GetString()).Contains("invalid-command-type");
        }
        catch (ArgumentException ex)
        {
            // Alternative: throw descriptive exception
            await Assert.That(ex.Message).Contains("Unknown message type");
        }
    }

    /// <summary>
    /// Test 1d: The adapter correctly serializes all message types from the bridge contract:
    /// jump-to-entry, step-forward, step-back, play, pause, clear-timeline.
    /// Each produces valid JSON with correct structure and NO side effects.
    /// 
    /// Validates the seam: Message type coverage in serialization contract.
    /// </summary>
    [Test]
    [Arguments("step-forward")]
    [Arguments("step-back")]
    [Arguments("play")]
    [Arguments("pause")]
    [Arguments("clear-timeline")]
    [Arguments("get-timeline")]
    public async Task AdapterSerializesAllBridgeMessageTypes_WithNoSideEffects(string messageType)
    {
        // Arrange
        var adapter = new Picea.Abies.Browser.Debugger.DebuggerAdapter();
        
        var request = new DebuggerAdapterMessage
        {
            Type = messageType
        };

        // Act
        var serialized = adapter.SerializeMessage(request);

        // Assert
        await Assert.That(serialized).IsNotNull();
        await Assert.That(serialized).IsNotEmpty();
        
        var parsed = JsonDocument.Parse(serialized);
        await Assert.That(parsed.RootElement.GetProperty("type").GetString()).IsEqualTo(messageType);
        
        // Transport contract remains deterministic for repeated serialization.
        var serializedAgain = adapter.SerializeMessage(request);
        await Assert.That(serializedAgain).IsEqualTo(serialized);
    }

    [Test]
    public async Task DebuggerJsContainsOnlyMountAndTransportLogic_NoReplayDomainReferences()
    {
        var debuggerJsPath = Path.Combine(RepoRoot, "Picea.Abies.Browser", "wwwroot", "debugger.js");
        await Assert.That(File.Exists(debuggerJsPath)).IsTrue();

        var content = await File.ReadAllTextAsync(debuggerJsPath);

        // JS adapter should not embed C# replay/domain internals.
        var forbiddenTokens = new[]
        {
            "DebuggerMachine",
            "CaptureMessage",
            "StepForward(",
            "StepBackward(",
            "ClearTimeline(",
            "CurrentDebugger",
            "GenerateModelSnapshot"
        };

        foreach (var token in forbiddenTokens)
        {
            await Assert.That(content.Contains(token, StringComparison.Ordinal)).IsFalse();
        }

        await Assert.That(content).Contains("abies:debugger:message-dispatched");
    }

    [Test]
    public async Task AdapterDeserializesResponseWithNewFields_BoundaryAndTimelineEntries()
    {
        var adapter = new DebuggerAdapter();

        var responseJson = JsonSerializer.Serialize(new DebuggerAdapterResponse
        {
            Status = "paused",
            CursorPosition = 2,
            TimelineSize = 5,
            AtStart = false,
            AtEnd = false,
            CurrentEntry = new DebuggerAdapterTimelineEntry
            {
                Sequence = 2,
                MessageType = "Increment",
                ArgsPreview = "{\"value\":1}",
                Timestamp = 1000L,
                PatchCount = 3
            },
            ModelSnapshotPreview = "{\"count\":2}",
            PreviousModelSnapshotPreview = "{\"count\":1}",
            TimelineEntries = null
        }, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);

        var response = adapter.DeserializeResponse(responseJson);

        await Assert.That(response.AtStart).IsFalse();
        await Assert.That(response.AtEnd).IsFalse();
        await Assert.That(response.CurrentEntry).IsNotNull();
        await Assert.That(response.CurrentEntry!.Sequence).IsEqualTo(2);
        await Assert.That(response.CurrentEntry.MessageType).IsEqualTo("Increment");
        await Assert.That(response.CurrentEntry.PatchCount).IsEqualTo(3);
        await Assert.That(response.PreviousModelSnapshotPreview).IsEqualTo("{\"count\":1}");
    }

    [Test]
    public async Task AdapterDeserializesResponseWithTimelineEntries_WhenPresent()
    {
        var adapter = new DebuggerAdapter();

        var responseJson = JsonSerializer.Serialize(new DebuggerAdapterResponse
        {
            Status = "recording",
            CursorPosition = 1,
            TimelineSize = 2,
            AtStart = false,
            AtEnd = true,
            ModelSnapshotPreview = "{\"count\":2}",
            TimelineEntries =
            [
                new DebuggerAdapterTimelineEntry
                {
                    Sequence = 0, MessageType = "First", ArgsPreview = "{}", Timestamp = 100L, PatchCount = 5
                },

                new DebuggerAdapterTimelineEntry
                {
                    Sequence = 1, MessageType = "Second", ArgsPreview = "{}", Timestamp = 200L, PatchCount = 3
                }
            ]
        }, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);

        var response = adapter.DeserializeResponse(responseJson);

        await Assert.That(response.TimelineEntries).IsNotNull();
        await Assert.That(response.TimelineEntries!.Count).IsEqualTo(2);
        await Assert.That(response.TimelineEntries[0].MessageType).IsEqualTo("First");
        await Assert.That(response.TimelineEntries[0].PatchCount).IsEqualTo(5);
        await Assert.That(response.TimelineEntries[1].MessageType).IsEqualTo("Second");
    }
}
