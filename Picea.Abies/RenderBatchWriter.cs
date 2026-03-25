using System.Buffers;
using System.Text;

namespace Picea.Abies;

internal enum BinaryPatchType : int
{
    AddRoot = 0,
    ReplaceChild = 1,
    AddChild = 2,
    RemoveChild = 3,
    ClearChildren = 4,
    SetChildrenHtml = 5,
    AppendChildrenHtml = 23,
    MoveChild = 6,
    UpdateAttribute = 7,
    AddAttribute = 8,
    RemoveAttribute = 9,
    AddHandler = 10,
    RemoveHandler = 11,
    UpdateHandler = 12,
    UpdateText = 13,
    AddText = 14,
    RemoveText = 15,
    AddRaw = 16,
    RemoveRaw = 17,
    ReplaceRaw = 18,
    UpdateRaw = 19,
    AddHeadElement = 20,
    UpdateHeadElement = 21,
    RemoveHeadElement = 22
}

internal sealed class RenderBatchWriter
{
    private const int HeaderSize = 8;
    private const int PatchEntrySize = 20;
    private const int NullIndex = -1;

    private readonly ArrayBufferWriter<byte> _buffer = new(4096);
    private readonly Dictionary<string, int> _stringTable = new();
    private readonly List<string> _strings = [];

    public ReadOnlyMemory<byte> Write(IReadOnlyList<Patch> patches)
    {
        _buffer.Clear();
        _stringTable.Clear();
        _strings.Clear();

        WriteInt32ToBuffer(0);
        WriteInt32ToBuffer(0);

        foreach (var patch in patches)
        {
            WritePatch(patch);
        }

        var stringTableOffset = _buffer.WrittenCount;

        WriteStringTable();

        var memory = _buffer.WrittenMemory;
        System.Runtime.InteropServices.MemoryMarshal.TryGetArray(
            memory, out var segment);
        var headerSpan = segment.Array.AsSpan(segment.Offset, HeaderSize);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            headerSpan[..4], patches.Count);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            headerSpan[4..8], stringTableOffset);

        return memory;
    }

    private void WritePatch(Patch patch)
    {
        switch (patch)
        {
            case AddRoot p:
                WriteEntry(BinaryPatchType.AddRoot,
                    Intern(p.Element.Id),
                    Intern(Render.Html(p.Element)),
                    NullIndex,
                    NullIndex);
                break;

            case ReplaceChild p:
                WriteEntry(BinaryPatchType.ReplaceChild,
                    Intern(p.OldElement.Id),
                    Intern(p.NewElement.Id),
                    Intern(Render.Html(p.NewElement)),
                    NullIndex);
                break;

            case AddChild p:
                WriteEntry(BinaryPatchType.AddChild,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Id),
                    Intern(Render.Html(p.Child)),
                    NullIndex);
                break;

            case RemoveChild p:
                WriteEntry(BinaryPatchType.RemoveChild,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Id),
                    NullIndex,
                    NullIndex);
                break;

            case ClearChildren p:
                WriteEntry(BinaryPatchType.ClearChildren,
                    Intern(p.Parent.Id),
                    NullIndex,
                    NullIndex,
                    NullIndex);
                break;

            case SetChildrenHtml p:
                WriteEntry(BinaryPatchType.SetChildrenHtml,
                    Intern(p.Parent.Id),
                    Intern(Render.HtmlChildren(p.Children)),
                    NullIndex,
                    NullIndex);
                break;

            case AppendChildrenHtml p:
                WriteEntry(BinaryPatchType.AppendChildrenHtml,
                    Intern(p.Parent.Id),
                    Intern(Render.HtmlChildren(p.Children)),
                    NullIndex,
                    NullIndex);
                break;

            case MoveChild p:
                WriteEntry(BinaryPatchType.MoveChild,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Id),
                    p.BeforeId is not null ? Intern(p.BeforeId) : NullIndex,
                    NullIndex);
                break;

            case UpdateAttribute p:
                WriteEntry(BinaryPatchType.UpdateAttribute,
                    Intern(p.Element.Id),
                    Intern(p.Attribute.Name),
                    Intern(p.Value),
                    NullIndex);
                break;

            case AddAttribute p:
                WriteEntry(BinaryPatchType.AddAttribute,
                    Intern(p.Element.Id),
                    Intern(p.Attribute.Name),
                    Intern(p.Attribute.Value),
                    NullIndex);
                break;

            case RemoveAttribute p:
                WriteEntry(BinaryPatchType.RemoveAttribute,
                    Intern(p.Element.Id),
                    Intern(p.Attribute.Name),
                    NullIndex,
                    NullIndex);
                break;

            case AddHandler p:
                WriteEntry(BinaryPatchType.AddHandler,
                    Intern(p.Element.Id),
                    Intern(p.Handler.Name),
                    Intern(p.Handler.CommandId),
                    NullIndex);
                break;

            case RemoveHandler p:
                WriteEntry(BinaryPatchType.RemoveHandler,
                    Intern(p.Element.Id),
                    Intern(p.Handler.Name),
                    Intern(p.Handler.CommandId),
                    NullIndex);
                break;

            case UpdateHandler p:
                WriteEntry(BinaryPatchType.UpdateHandler,
                    Intern(p.Element.Id),
                    Intern(p.OldHandler.Name),
                    Intern(p.NewHandler.CommandId),
                    NullIndex);
                break;

            case UpdateText p:
                WriteEntry(BinaryPatchType.UpdateText,
                    Intern(p.Parent.Id),
                    Intern(p.Node.Id),
                    Intern(p.Text),
                    Intern(p.NewId));
                break;

            case AddText p:
                WriteEntry(BinaryPatchType.AddText,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Value),
                    Intern(p.Child.Id),
                    NullIndex);
                break;

            case RemoveText p:
                WriteEntry(BinaryPatchType.RemoveText,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Id),
                    NullIndex,
                    NullIndex);
                break;

            case AddRaw p:
                WriteEntry(BinaryPatchType.AddRaw,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Html),
                    Intern(p.Child.Id),
                    NullIndex);
                break;

            case RemoveRaw p:
                WriteEntry(BinaryPatchType.RemoveRaw,
                    Intern(p.Parent.Id),
                    Intern(p.Child.Id),
                    NullIndex,
                    NullIndex);
                break;

            case ReplaceRaw p:
                WriteEntry(BinaryPatchType.ReplaceRaw,
                    Intern(p.OldNode.Id),
                    Intern(p.NewNode.Id),
                    Intern(p.NewNode.Html),
                    NullIndex);
                break;

            case UpdateRaw p:
                WriteEntry(BinaryPatchType.UpdateRaw,
                    Intern(p.Node.Id),
                    Intern(p.Html),
                    Intern(p.NewId),
                    NullIndex);
                break;

            case AddHeadElement p:
                WriteEntry(BinaryPatchType.AddHeadElement,
                    Intern(p.Content.Key),
                    Intern(p.Content.ToHtml()),
                    NullIndex,
                    NullIndex);
                break;

            case UpdateHeadElement p:
                WriteEntry(BinaryPatchType.UpdateHeadElement,
                    Intern(p.Content.Key),
                    Intern(p.Content.ToHtml()),
                    NullIndex,
                    NullIndex);
                break;

            case RemoveHeadElement p:
                WriteEntry(BinaryPatchType.RemoveHeadElement,
                    Intern(p.Key),
                    NullIndex,
                    NullIndex,
                    NullIndex);
                break;
        }
    }

    private void WriteEntry(BinaryPatchType type, int field1, int field2, int field3, int field4)
    {
        var span = _buffer.GetSpan(PatchEntrySize);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[..4], (int)type);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[4..8], field1);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[8..12], field2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[12..16], field3);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[16..20], field4);
        _buffer.Advance(PatchEntrySize);
    }

    private int Intern(string value)
    {
        if (_stringTable.TryGetValue(value, out var index))
            return index;

        index = _strings.Count;
        _strings.Add(value);
        _stringTable[value] = index;
        return index;
    }

    private void WriteStringTable()
    {
        foreach (var str in _strings)
        {
            var byteCount = Encoding.UTF8.GetByteCount(str);
            WriteLeb128(byteCount);

            var span = _buffer.GetSpan(byteCount);
            Encoding.UTF8.GetBytes(str.AsSpan(), span);
            _buffer.Advance(byteCount);
        }
    }

    private void WriteLeb128(int value)
    {
        do
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value > 0)
                b |= 0x80;

            var span = _buffer.GetSpan(1);
            span[0] = b;
            _buffer.Advance(1);
        } while (value > 0);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void WriteInt32ToBuffer(int value)
    {
        var span = _buffer.GetSpan(4);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span, value);
        _buffer.Advance(4);
    }
}
