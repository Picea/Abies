// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Picea.Abies.Browser.Debugger;

namespace Picea.Abies.Browser.Tests;

/// <summary>
/// Validates the JS Adapter Layer contract for the time travel debugger.
/// Purpose: Verify that the adapter correctly serializes/deserializes debugger messages
/// (jump, step-forward, step-back, play, pause, clear) and does NOT execute replay logic.
/// 
/// The adapter is a TRANSPORT-ONLY layer: JSON serialization/deserialization, no state machine transitions.
/// All replay logic lives in C# (Mealy machine).
/// 
/// NOTE: These tests are expected to FAIL TO COMPILE today (Picea.Abies.Browser.Debugger namespace
/// and DebuggerAdapter class do not exist yet). They document the contract for the JS adapter implementation.
/// 
/// Test Strategy: Option A (C# unit tests with mocking)
/// - Mock the JS bridge via Moq or manual mock
/// - Test the C#→JS message serialization contract
/// - Validate that adapter does NOT contain state machine logic
/// </summary>
public class DebuggerAdapterTests
{
    /// <summary>
    /// Test 1a: When user requests a "jump to entry" action, the adapter serializes the request
    /// to a valid JSON message conforming to the bridge contract.
    /// 
    /// Validates the seam: UI message → JSON serialization → C# transport contract.
    /// 
    /// TODAY: Fails to compile - Picea.Abies.Browser.Debugger.DebuggerAdapter does not exist.
    /// TOMORROW: Passes when DebuggerAdapter is implemented with SerializeMessage() method.
    /// </summary>
    [Test]
    public async Task AdapterSerializesJumpMessageToJSON_WhenUserRequestedJump()
    {
        // Arrange
        // EXPECTED FAILURE: Picea.Abies.Browser.Debugger.DebuggerAdapter does not exist
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
        
        // Verify NO side effects (no command execution, no state mutation)
        await Assert.That(adapter.PendingCommands).IsEmpty();
        await Assert.That((int)adapter.CurrentState).IsEqualTo((int)DebuggerAdapterState.Idle);
    }

    /// <summary>
    /// Test 1b: When adapter receives a C# response containing timeline update data,
    /// it deserializes the response to a strongly-typed object WITHOUT attempting to
    /// restore model state or execute any replay logic (C# Mealy machine owns that).
    /// 
    /// Validates the seam: C# response → JSON deserialization → no replay side effects.
    /// 
    /// TODAY: Fails to compile - No DeserializeResponse() API exists.
    /// TOMORROW: Passes when deserialization is implemented.
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
        });

        var initialCommandQueueSize = adapter.PendingCommands.Count;

        // Act
        var response = adapter.DeserializeResponse(c_Response);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response.Status).IsEqualTo("paused");
        await Assert.That(response.CursorPosition).IsEqualTo(5);
        await Assert.That(response.TimelineSize).IsEqualTo(10);
        
        // CRITICAL: Adapter should NOT replay logic or execute commands
        await Assert.That(adapter.PendingCommands.Count).IsEqualTo(initialCommandQueueSize);
        await Assert.That(adapter.ExecutedTransactions).IsEmpty();
    }

    /// <summary>
    /// Test 1c: When adapter receives a malformed or unknown message type,
    /// it either throws ArgumentException with a descriptive message OR returns a well-formed error envelope.
    /// This validates input validation at the transport boundary.
    /// 
    /// Validates the seam: Error handling for invalid messages, graceful failure.
    /// 
    /// TODAY: Fails to compile - No input validation in adapter yet.
    /// TOMORROW: Passes when validation is implemented.
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
    /// 
    /// TODAY: Fails to compile - Adapter class does not exist.
    /// TOMORROW: Passes when all message types are supported.
    /// </summary>
    [Test]
    [Arguments("step-forward")]
    [Arguments("step-back")]
    [Arguments("play")]
    [Arguments("pause")]
    [Arguments("clear-timeline")]
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
        
        // No side effects
        await Assert.That(adapter.PendingCommands).IsEmpty();
        await Assert.That((int)adapter.CurrentState).IsEqualTo((int)DebuggerAdapterState.Idle);
    }
}
