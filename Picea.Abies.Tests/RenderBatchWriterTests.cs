using System.Buffers.Binary;
using System.Text;
using Picea.Abies.DOM;
using Attribute = Picea.Abies.DOM.Attribute;

namespace Picea.Abies.Tests;

public class RenderBatchWriterTests
{
    [Test]
    public async Task Write_UpdateText_EncodesParentOldIdTextAndNewId()
    {
        var writer = new RenderBatchWriter();
        var parent = new Element("e1", "p", []);
        var text = new Text("t1", "old");

        var batch = writer.Write([new UpdateText(parent, text, "new", "t2")]);
        var parsed = ParseBatch(batch.Span);

        await Assert.That(parsed.PatchCount).IsEqualTo(1);
        await Assert.That(parsed.Entries[0].Type).IsEqualTo((int)BinaryPatchType.UpdateText);
        await Assert.That(parsed.Entries[0].Field1).IsEqualTo("e1");
        await Assert.That(parsed.Entries[0].Field2).IsEqualTo("t1");
        await Assert.That(parsed.Entries[0].Field3).IsEqualTo("new");
        await Assert.That(parsed.Entries[0].Field4).IsEqualTo("t2");
    }

    [Test]
    public async Task Write_RemoveText_EncodesParentAndTextId()
    {
        var writer = new RenderBatchWriter();
        var parent = new Element("e1", "p", []);
        var text = new Text("t2", "remove me");

        var batch = writer.Write([new RemoveText(parent, text)]);
        var parsed = ParseBatch(batch.Span);

        await Assert.That(parsed.PatchCount).IsEqualTo(1);
        await Assert.That(parsed.Entries[0].Type).IsEqualTo((int)BinaryPatchType.RemoveText);
        await Assert.That(parsed.Entries[0].Field1).IsEqualTo("e1");
        await Assert.That(parsed.Entries[0].Field2).IsEqualTo("t2");
        await Assert.That(parsed.Entries[0].Field3).IsNull();
        await Assert.That(parsed.Entries[0].Field4).IsNull();
    }

    [Test]
    public async Task Write_AttributeChurn_OnlySerializesLastMutation()
    {
        var writer = new RenderBatchWriter();
        var element = new Element("e1", "div", []);
        var attr = new Attribute("a1", "class", "old");

        var batch = writer.Write([
            new AddAttribute(element, attr),
            new UpdateAttribute(element, attr, "new")
        ]);

        var parsed = ParseBatch(batch.Span);

        await Assert.That(parsed.PatchCount).IsEqualTo(1);
        await Assert.That(parsed.Entries[0].Type).IsEqualTo((int)BinaryPatchType.UpdateAttribute);
        await Assert.That(parsed.Entries[0].Field1).IsEqualTo("e1");
        await Assert.That(parsed.Entries[0].Field2).IsEqualTo("class");
        await Assert.That(parsed.Entries[0].Field3).IsEqualTo("new");
    }

    [Test]
    public async Task Write_HandlerChurn_OnlySerializesLastMutation()
    {
        var writer = new RenderBatchWriter();
        var element = new Element("e1", "button", []);
        var oldHandler = new Handler("click", "cmd-1", null, "h1");
        var newHandler = new Handler("click", "cmd-2", null, "h1");

        var batch = writer.Write([
            new AddHandler(element, oldHandler),
            new UpdateHandler(element, oldHandler, newHandler)
        ]);

        var parsed = ParseBatch(batch.Span);

        await Assert.That(parsed.PatchCount).IsEqualTo(1);
        await Assert.That(parsed.Entries[0].Type).IsEqualTo((int)BinaryPatchType.UpdateHandler);
        await Assert.That(parsed.Entries[0].Field1).IsEqualTo("e1");
        await Assert.That(parsed.Entries[0].Field2).IsEqualTo("data-event-click");
        await Assert.That(parsed.Entries[0].Field3).IsEqualTo("cmd-2");
    }

    [Test]
    public async Task Write_HeadChurn_OnlySerializesLastMutation()
    {
        var writer = new RenderBatchWriter();
        var first = Head.meta("description", "old");
        var second = Head.meta("description", "new");

        var batch = writer.Write([
            new AddHeadElement(first),
            new UpdateHeadElement(second)
        ]);

        var parsed = ParseBatch(batch.Span);

        await Assert.That(parsed.PatchCount).IsEqualTo(1);
        await Assert.That(parsed.Entries[0].Type).IsEqualTo((int)BinaryPatchType.UpdateHeadElement);
        await Assert.That(parsed.Entries[0].Field1).IsEqualTo("meta:description");
        await Assert.That(parsed.Entries[0].Field2).Contains("content=\"new\"");
    }

    private static ParsedBatch ParseBatch(ReadOnlySpan<byte> data)
    {
        var patchCount = BinaryPrimitives.ReadInt32LittleEndian(data[..4]);
        var stringTableOffset = BinaryPrimitives.ReadInt32LittleEndian(data[4..8]);
        var strings = ReadStringTable(data[stringTableOffset..]);

        const int headerSize = 8;
        const int entrySize = 20;
        var entries = new List<ParsedEntry>(patchCount);

        for (var i = 0; i < patchCount; i++)
        {
            var offset = headerSize + (i * entrySize);
            var type = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            var f1Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..]);
            var f2Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 8)..]);
            var f3Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 12)..]);
            var f4Idx = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 16)..]);

            entries.Add(new ParsedEntry(
                type,
                f1Idx >= 0 ? strings[f1Idx] : null,
                f2Idx >= 0 ? strings[f2Idx] : null,
                f3Idx >= 0 ? strings[f3Idx] : null,
                f4Idx >= 0 ? strings[f4Idx] : null));
        }

        return new ParsedBatch(patchCount, entries);
    }

    private static List<string> ReadStringTable(ReadOnlySpan<byte> data)
    {
        var strings = new List<string>();
        var offset = 0;

        while (offset < data.Length)
        {
            var byteLength = 0;
            var shift = 0;
            byte b;
            do
            {
                b = data[offset++];
                byteLength |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            strings.Add(Encoding.UTF8.GetString(data.Slice(offset, byteLength)));
            offset += byteLength;
        }

        return strings;
    }

    private sealed record ParsedBatch(int PatchCount, IReadOnlyList<ParsedEntry> Entries);

    private sealed record ParsedEntry(int Type, string? Field1, string? Field2, string? Field3, string? Field4);
}
