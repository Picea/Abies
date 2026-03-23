// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

/// <summary>
/// Validates ring buffer FIFO eviction, timestamp monotonicity, and capacity management.
/// Purpose: Verify that the ring buffer correctly enforces capacity limits via FIFO eviction,
/// maintains sequence ordering, and guarantees timestamp monotonicity.
/// 
/// NOTE: These tests are expected to FAIL TO COMPILE today (Picea.Abies.Debugger.RingBuffer
/// type does not exist yet). They document the contract for the ring buffer implementation.
/// </summary>
public class DebuggerRingBufferTests
{
    /// <summary>
    /// Test 2a: When adding entries beyond capacity, the ring buffer evicts the oldest entries
    /// (FIFO), and the remaining entries maintain correct sequence numbering and monotonic timestamps.
    /// 
    /// Validates the seam: Capacity enforcement, FIFO eviction, sequence integrity, timestamp order.
    /// TODAY: Fails to compile - RingBuffer&lt;T&gt; type does not exist.
    /// </summary>
    [Test]
    public void RingBufferEvictsOldestEntryWhenCapacityExceeded()
    {
        // Arrange
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 3);
        var entries = new List<TimestampedEntry>();
        
        // Add 5 entries (sequences 0-4) with strictly increasing timestamps
        for (int i = 0; i < 5; i++)
        {
            var entry = new TimestampedEntry(
                Sequence: i,
                MessageType: $"Message{i}",
                ArgsPreview: $"Args{i}",
                Timestamp: 1000L + i * 100,  // 1000, 1100, 1200, 1300, 1400
                ModelSnapshotPreview: $"Snapshot{i}"
            );
            buffer.Add(entry);
            entries.Add(entry);
        }

        // Assert
        Assert.That(buffer.Count, Is.EqualTo(3),
            "Buffer should contain exactly 3 entries (oldest 2 evicted)");
        
        var bufferList = buffer.Entries.ToList();
        Assert.That(bufferList[0].Sequence, Is.EqualTo(2),
            "First remaining entry should be sequence 2 (oldest 0, 1 evicted)");
        Assert.That(bufferList[1].Sequence, Is.EqualTo(3),
            "Second remaining entry should be sequence 3");
        Assert.That(bufferList[2].Sequence, Is.EqualTo(4),
            "Third remaining entry should be sequence 4");
        
        // Verify timestamp monotonicity after eviction
        for (int i = 0; i < bufferList.Count - 1; i++)
        {
            Assert.That(bufferList[i].Timestamp, Is.LessThanOrEqualTo(bufferList[i + 1].Timestamp),
                $"Timestamps should be monotonically increasing at positions {i} → {i + 1}");
        }
    }

    /// <summary>
    /// Test 2b: When adding 100 entries with simulated monotonic timestamps (1ms spacing),
    /// the buffer maintains strict timestamp ordering throughout.
    /// 
    /// Validates the seam: Timestamp monotonicity guarantee across all buffer operations.
    /// TODAY: Fails to compile - RingBuffer&lt;T&gt; does not exist.
    /// </summary>
    [Test]
    public void RingBufferMaintainsTimestampMonotonicity()
    {
        // Arrange
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 100);
        
        // Add 100 entries with 1ms timestamps apart
        for (int i = 0; i < 100; i++)
        {
            var entry = new TimestampedEntry(
                Sequence: i,
                MessageType: $"MonoMsg{i}",
                ArgsPreview: $"Arg{i}",
                Timestamp: 1000000L + i * 1,  // 1000000, 1000001, ..., 1000099
                ModelSnapshotPreview: "{}"
            );
            buffer.Add(entry);
        }

        // Act: Iterate all entries and verify monotonicity
        var entries = buffer.Entries.ToList();

        // Assert
        for (int i = 0; i < entries.Count - 1; i++)
        {
            Assert.That(entries[i].Timestamp, Is.LessThanOrEqualTo(entries[i + 1].Timestamp),
                $"Timestamp at index {i} ({entries[i].Timestamp}) should be ≤ index {i + 1} ({entries[i + 1].Timestamp})");
        }
    }

    /// <summary>
    /// Test 2c: When Clear() is called, all entries are removed. The next Add() should
    /// start with sequence 0 again (sequence resets after clear).
    /// 
    /// Validates the seam: Clear operation, sequence reset, buffer reuse.
    /// TODAY: Fails to compile - Clear() method does not exist.
    /// </summary>
    [Test]
    public void RingBufferClearAllEntriesOnDemand()
    {
        // Arrange
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 10);
        
        // Add 10 entries
        for (int i = 0; i < 10; i++)
        {
            var entry = new TimestampedEntry(
                Sequence: i,
                MessageType: $"ClearMsg{i}",
                ArgsPreview: "arg",
                Timestamp: 2000L + i,
                ModelSnapshotPreview: "{}"
            );
            buffer.Add(entry);
        }
        
        Assert.That(buffer.Count, Is.EqualTo(10), "Buffer should have 10 entries before clear");
        var expectedFirstSeqAfterClear = buffer.NextSequence;

        // Act
        buffer.Clear();

        // Assert
        Assert.That(buffer.Count, Is.EqualTo(0),
            "Buffer count should be 0 after Clear()");
        
        // The next added entry should start with the sequence after clear
        var newEntry = new TimestampedEntry(
            Sequence: expectedFirstSeqAfterClear,
            MessageType: "FirstAfterClear",
            ArgsPreview: "arg",
            Timestamp: 3000L,
            ModelSnapshotPreview: "{}"
        );
        buffer.Add(newEntry);
        
        Assert.That(buffer.Count, Is.EqualTo(1),
            "Buffer should have 1 entry after adding new entry post-clear");
        Assert.That(buffer.Entries.First().Sequence, Is.EqualTo(expectedFirstSeqAfterClear),
            "First entry after clear should have correct sequence");
    }

    /// <summary>
    /// Test 2d: Ring buffer enforces maximum capacity and does not allow count to exceed it.
    /// 
    /// Validates the seam: Capacity enforcement, no overflow.
    /// TODAY: Fails to compile - RingBuffer&lt;T&gt; does not exist.
    /// </summary>
    [Test]
    public void RingBufferNeverExceedsCapacity()
    {
        // Arrange
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 5);

        // Act: Add 10 entries (double capacity)
        for (int i = 0; i < 10; i++)
        {
            var entry = new TimestampedEntry(
                Sequence: i,
                MessageType: $"CapacityMsg{i}",
                ArgsPreview: "arg",
                Timestamp: 4000L + i,
                ModelSnapshotPreview: "{}"
            );
            buffer.Add(entry);
        }

        // Assert
        Assert.That(buffer.Count, Is.LessThanOrEqualTo(5),
            "Buffer count should never exceed capacity of 5");
        Assert.That(buffer.Count, Is.EqualTo(5),
            "Buffer should contain exactly 5 entries (oldest 5 evicted)");
        
        // Verify oldest entries were evicted (sequences 0-4 gone, 5-9 remain)
        var entries = buffer.Entries.ToList();
        Assert.That(entries.First().Sequence, Is.EqualTo(5),
            "Oldest remaining entry should be sequence 5");
        Assert.That(entries.Last().Sequence, Is.EqualTo(9),
            "Newest entry should be sequence 9");
    }
}
