using Picea.Abies.Debugger;

namespace Picea.Abies.Tests;

public sealed class DebuggerRingBufferTests
{
    [Test]
    public async Task RingBufferEvictsOldestEntryWhenCapacityExceeded()
    {
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 3);

        for (var i = 0; i < 5; i++)
        {
            var entry = new TimestampedEntry(
                Sequence: i,
                MessageType: $"Message{i}",
                ArgsPreview: $"Args{i}",
                Timestamp: 1000L + i * 100,  // 1000, 1100, 1200, 1300, 1400
                ModelSnapshotPreview: $"Snapshot{i}"
            );
            buffer.Add(entry);
        }

        await Assert.That(buffer.Count).IsEqualTo(3);

        var bufferList = buffer.Entries.ToList();
        await Assert.That(bufferList[0].Sequence).IsEqualTo(2L);
        await Assert.That(bufferList[1].Sequence).IsEqualTo(3L);
        await Assert.That(bufferList[2].Sequence).IsEqualTo(4L);

        for (var i = 0; i < bufferList.Count - 1; i++)
        {
            await Assert.That(bufferList[i].Timestamp).IsLessThanOrEqualTo(bufferList[i + 1].Timestamp);
        }
    }

    [Test]
    public async Task RingBufferMaintainsTimestampMonotonicity()
    {
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 100);

        for (var i = 0; i < 100; i++)
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

        var entries = buffer.Entries.ToList();

        for (var i = 0; i < entries.Count - 1; i++)
        {
            await Assert.That(entries[i].Timestamp).IsLessThanOrEqualTo(entries[i + 1].Timestamp);
        }
    }

    [Test]
    public async Task RingBufferClearAllEntriesOnDemand()
    {
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 10);

        for (var i = 0; i < 10; i++)
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

        await Assert.That(buffer.Count).IsEqualTo(10);
        var expectedFirstSeqAfterClear = buffer.NextSequence;

        buffer.Clear();

        await Assert.That(buffer.Count).IsEqualTo(0);

        var newEntry = new TimestampedEntry(
            Sequence: expectedFirstSeqAfterClear,
            MessageType: "FirstAfterClear",
            ArgsPreview: "arg",
            Timestamp: 3000L,
            ModelSnapshotPreview: "{}"
        );
        buffer.Add(newEntry);

        await Assert.That(buffer.Count).IsEqualTo(1);
        await Assert.That(buffer.Entries.First().Sequence).IsEqualTo(expectedFirstSeqAfterClear);
    }

    [Test]
    public async Task RingBufferNeverExceedsCapacity()
    {
        var buffer = new RingBuffer<TimestampedEntry>(capacity: 5);

        for (var i = 0; i < 10; i++)
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

        await Assert.That(buffer.Count).IsLessThanOrEqualTo(5);
        await Assert.That(buffer.Count).IsEqualTo(5);

        var entries = buffer.Entries.ToList();
        await Assert.That(entries.First().Sequence).IsEqualTo(5L);
        await Assert.That(entries.Last().Sequence).IsEqualTo(9L);
    }
}
