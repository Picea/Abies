// =============================================================================
// Binary Render Batch Writer
// =============================================================================
// Provides a binary serialization format for DOM patches, inspired by Blazor's
// SharedMemoryRenderBatch. This eliminates JSON serialization overhead by
// writing directly to a byte buffer that JavaScript can read from WASM memory.
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// - ADR-011: JavaScript Interop Strategy (docs/adr/ADR-011-javascript-interop.md)
// =============================================================================

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Abies.DOM;

/// <summary>
/// Binary patch types corresponding to DOM operations.
/// Values must stay in sync with JavaScript reader.
/// </summary>
public enum BinaryPatchType : int
{
    /// <summary>Set entire app content (html in Field1)</summary>
    SetAppContent = 1,

    /// <summary>Replace child element (targetId in Field1, html in Field2)</summary>
    ReplaceChild = 2,

    /// <summary>Add child element (parentId in Field1, html in Field2)</summary>
    AddChild = 3,

    /// <summary>Remove child element (parentId in Field1, childId in Field2)</summary>
    RemoveChild = 4,

    /// <summary>Clear all children (parentId in Field1)</summary>
    ClearChildren = 5,

    /// <summary>Update attribute (targetId in Field1, attrName in Field2, attrValue in Field3)</summary>
    UpdateAttribute = 6,

    /// <summary>Add attribute (targetId in Field1, attrName in Field2, attrValue in Field3)</summary>
    AddAttribute = 7,

    /// <summary>Remove attribute (targetId in Field1, attrName in Field2)</summary>
    RemoveAttribute = 8,

    /// <summary>Update text content (targetId in Field1, text in Field2)</summary>
    UpdateText = 9,

    /// <summary>Update text content with new ID (targetId in Field1, text in Field2, newId in Field3)</summary>
    UpdateTextWithId = 10,

    /// <summary>Move child to new position (parentId in Field1, childId in Field2, beforeId in Field3, -1 = append)</summary>
    MoveChild = 11,

    /// <summary>Set all children via innerHTML (parentId in Field1, html in Field2). Eliminates N parseHtmlFragment + appendChild + addEventListeners calls.</summary>
    SetChildrenHtml = 12,

    /// <summary>Add a new element to the document head (key in Field1, html in Field2)</summary>
    AddHeadElement = 13,

    /// <summary>Update an existing managed element in the document head (key in Field1, html in Field2)</summary>
    UpdateHeadElement = 14,

    /// <summary>Remove a managed element from the document head (key in Field1)</summary>
    RemoveHeadElement = 15,
}

/// <summary>
/// Writes DOM patches to a binary format for efficient JS interop.
/// 
/// Binary Format:
/// <code>
/// Header (8 bytes):
///   - PatchCount: int32 (4 bytes)
///   - StringTableOffset: int32 (4 bytes)
/// 
/// Patch Entries (16 bytes each):
///   - Type: int32 (4 bytes) - BinaryPatchType enum value
///   - Field1: int32 (4 bytes) - string table index (-1 = null)
///   - Field2: int32 (4 bytes) - string table index (-1 = null)
///   - Field3: int32 (4 bytes) - string table index (-1 = null)
/// 
/// String Table:
///   - Strings are stored consecutively with LEB128 length prefix + UTF8 bytes
///   - String table indices point to byte offset from start of string table
/// </code>
/// </summary>
public sealed class RenderBatchWriter : IDisposable
{
    // Header size: PatchCount (4) + StringTableOffset (4)
    private const int HeaderSize = 8;

    // Each patch entry is exactly 16 bytes for O(1) indexing
    private const int PatchEntrySize = 16;

    // Initial buffer capacity
    private const int InitialCapacity = 4096;

    // The buffer we write to for final output
    private byte[] _buffer;
    private int _position;

    // Patch entries are written to a separate buffer first, then copied after header
    private readonly List<PatchEntry> _patches = new();

    // String table: maps strings to their index in the string data
    private readonly Dictionary<string, int> _stringIndices = new();

    // String data buffer (pooled to avoid allocations)
    private byte[] _stringBuffer;
    private int _stringPosition;
    private const int InitialStringBufferCapacity = 4096;

    private bool _disposed;

    /// <summary>
    /// Represents a single patch entry in the batch.
    /// </summary>
    private readonly struct PatchEntry
    {
        public readonly BinaryPatchType Type;
        public readonly int Field1;
        public readonly int Field2;
        public readonly int Field3;

        public PatchEntry(BinaryPatchType type, int field1, int field2, int field3)
        {
            Type = type;
            Field1 = field1;
            Field2 = field2;
            Field3 = field3;
        }
    }

    public RenderBatchWriter()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(InitialCapacity);
        _stringBuffer = ArrayPool<byte>.Shared.Rent(InitialStringBufferCapacity);
        _position = 0;
        _stringPosition = 0;
    }

    /// <summary>
    /// Writes a string to the string table and returns its index.
    /// Returns -1 for null strings.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteString(string? value)
    {
        if (value is null)
        {
            return -1;
        }

        // Check if we already have this string
        if (_stringIndices.TryGetValue(value, out var existingIndex))
        {
            return existingIndex;
        }

        // Get the current position in string data as the index
        var index = _stringPosition;
        _stringIndices[value] = index;

        // Get required byte count for UTF8 encoding (no allocation)
        var byteCount = Encoding.UTF8.GetByteCount(value);

        // Ensure string buffer capacity (including LEB128 overhead - max 5 bytes for 32-bit int)
        EnsureStringBufferCapacity(_stringPosition + byteCount + 5);

        // Write LEB128 length prefix directly to buffer
        WriteLEB128ToBuffer(byteCount);

        // Encode the string directly into the buffer (no intermediate allocation)
        Encoding.UTF8.GetBytes(value.AsSpan(), _stringBuffer.AsSpan(_stringPosition));
        _stringPosition += byteCount;

        return index;
    }

    /// <summary>
    /// Writes an unsigned integer in LEB128 format directly to string buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteLEB128ToBuffer(int value)
    {
        // LEB128 encoding: 7 bits per byte, MSB indicates continuation
        var remaining = (uint)value;
        do
        {
            var b = (byte)(remaining & 0x7F);
            remaining >>= 7;
            if (remaining != 0)
            {
                b |= 0x80; // Set continuation bit
            }
            _stringBuffer[_stringPosition++] = b;
        } while (remaining != 0);
    }

    /// <summary>
    /// Ensures the string buffer has sufficient capacity.
    /// </summary>
    private void EnsureStringBufferCapacity(int required)
    {
        if (_stringBuffer.Length >= required)
        {
            return;
        }

        var newCapacity = Math.Max(_stringBuffer.Length * 2, required);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        Buffer.BlockCopy(_stringBuffer, 0, newBuffer, 0, _stringPosition);
        ArrayPool<byte>.Shared.Return(_stringBuffer);
        _stringBuffer = newBuffer;
    }

    /// <summary>
    /// Adds a SetAppContent patch (set entire app HTML).
    /// </summary>
    public void WriteSetAppContent(string html)
    {
        var htmlIndex = WriteString(html);
        _patches.Add(new PatchEntry(BinaryPatchType.SetAppContent, htmlIndex, -1, -1));
    }

    /// <summary>
    /// Adds a ReplaceChild patch.
    /// </summary>
    public void WriteReplaceChild(string targetId, string html)
    {
        var targetIndex = WriteString(targetId);
        var htmlIndex = WriteString(html);
        _patches.Add(new PatchEntry(BinaryPatchType.ReplaceChild, targetIndex, htmlIndex, -1));
    }

    /// <summary>
    /// Adds an AddChild patch.
    /// </summary>
    public void WriteAddChild(string parentId, string html)
    {
        var parentIndex = WriteString(parentId);
        var htmlIndex = WriteString(html);
        _patches.Add(new PatchEntry(BinaryPatchType.AddChild, parentIndex, htmlIndex, -1));
    }

    /// <summary>
    /// Adds a RemoveChild patch.
    /// </summary>
    public void WriteRemoveChild(string parentId, string childId)
    {
        var parentIndex = WriteString(parentId);
        var childIndex = WriteString(childId);
        _patches.Add(new PatchEntry(BinaryPatchType.RemoveChild, parentIndex, childIndex, -1));
    }

    /// <summary>
    /// Adds a ClearChildren patch.
    /// </summary>
    public void WriteClearChildren(string parentId)
    {
        var parentIndex = WriteString(parentId);
        _patches.Add(new PatchEntry(BinaryPatchType.ClearChildren, parentIndex, -1, -1));
    }

    /// <summary>
    /// Adds an UpdateAttribute patch.
    /// </summary>
    public void WriteUpdateAttribute(string targetId, string attrName, string attrValue)
    {
        var targetIndex = WriteString(targetId);
        var nameIndex = WriteString(attrName);
        var valueIndex = WriteString(attrValue);
        _patches.Add(new PatchEntry(BinaryPatchType.UpdateAttribute, targetIndex, nameIndex, valueIndex));
    }

    /// <summary>
    /// Adds an AddAttribute patch.
    /// </summary>
    public void WriteAddAttribute(string targetId, string attrName, string attrValue)
    {
        var targetIndex = WriteString(targetId);
        var nameIndex = WriteString(attrName);
        var valueIndex = WriteString(attrValue);
        _patches.Add(new PatchEntry(BinaryPatchType.AddAttribute, targetIndex, nameIndex, valueIndex));
    }

    /// <summary>
    /// Adds a RemoveAttribute patch.
    /// </summary>
    public void WriteRemoveAttribute(string targetId, string attrName)
    {
        var targetIndex = WriteString(targetId);
        var nameIndex = WriteString(attrName);
        _patches.Add(new PatchEntry(BinaryPatchType.RemoveAttribute, targetIndex, nameIndex, -1));
    }

    /// <summary>
    /// Adds an UpdateText patch.
    /// </summary>
    public void WriteUpdateText(string targetId, string text)
    {
        var targetIndex = WriteString(targetId);
        var textIndex = WriteString(text);
        _patches.Add(new PatchEntry(BinaryPatchType.UpdateText, targetIndex, textIndex, -1));
    }

    /// <summary>
    /// Adds an UpdateTextWithId patch (when text node ID changes).
    /// </summary>
    public void WriteUpdateTextWithId(string targetId, string text, string newId)
    {
        var targetIndex = WriteString(targetId);
        var textIndex = WriteString(text);
        var newIdIndex = WriteString(newId);
        _patches.Add(new PatchEntry(BinaryPatchType.UpdateTextWithId, targetIndex, textIndex, newIdIndex));
    }

    /// <summary>
    /// Adds a MoveChild patch.
    /// </summary>
    public void WriteMoveChild(string parentId, string childId, string? beforeId)
    {
        var parentIndex = WriteString(parentId);
        var childIndex = WriteString(childId);
        var beforeIndex = WriteString(beforeId); // -1 if null (append)
        _patches.Add(new PatchEntry(BinaryPatchType.MoveChild, parentIndex, childIndex, beforeIndex));
    }

    /// <summary>
    /// Adds a SetChildrenHtml patch — sets all children of a parent element via a single innerHTML assignment.
    /// This replaces N individual AddChild patches with one bulk operation.
    /// </summary>
    public void WriteSetChildrenHtml(string parentId, string html)
    {
        var parentIndex = WriteString(parentId);
        var htmlIndex = WriteString(html);
        _patches.Add(new PatchEntry(BinaryPatchType.SetChildrenHtml, parentIndex, htmlIndex, -1));
    }

    /// <summary>
    /// Adds an AddHeadElement patch — inserts a new element into the document <c>&lt;head&gt;</c>.
    /// </summary>
    /// <param name="key">The stable identity key (stored in <c>data-abies-head</c>).</param>
    /// <param name="html">The full HTML string of the element to add.</param>
    public void WriteAddHeadElement(string key, string html)
    {
        var keyIndex = WriteString(key);
        var htmlIndex = WriteString(html);
        _patches.Add(new PatchEntry(BinaryPatchType.AddHeadElement, keyIndex, htmlIndex, -1));
    }

    /// <summary>
    /// Adds an UpdateHeadElement patch — replaces an existing managed element in the document <c>&lt;head&gt;</c>.
    /// </summary>
    /// <param name="key">The stable identity key (<c>data-abies-head</c> attribute value).</param>
    /// <param name="html">The full HTML string of the replacement element.</param>
    public void WriteUpdateHeadElement(string key, string html)
    {
        var keyIndex = WriteString(key);
        var htmlIndex = WriteString(html);
        _patches.Add(new PatchEntry(BinaryPatchType.UpdateHeadElement, keyIndex, htmlIndex, -1));
    }

    /// <summary>
    /// Adds a RemoveHeadElement patch — removes a managed element from the document <c>&lt;head&gt;</c>.
    /// </summary>
    /// <param name="key">The stable identity key (<c>data-abies-head</c> attribute value).</param>
    public void WriteRemoveHeadElement(string key)
    {
        var keyIndex = WriteString(key);
        _patches.Add(new PatchEntry(BinaryPatchType.RemoveHeadElement, keyIndex, -1, -1));
    }

    /// <summary>
    /// Gets the number of patches written.
    /// </summary>
    public int PatchCount => _patches.Count;

    /// <summary>
    /// Finalizes the batch and returns the binary data as a Memory&lt;byte&gt;.
    /// The returned memory is valid until this writer is disposed.
    /// </summary>
    public Memory<byte> ToMemory()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RenderBatchWriter));
        }

        // Calculate total size
        var patchDataSize = _patches.Count * PatchEntrySize;
        var stringTableOffset = HeaderSize + patchDataSize;
        var totalSize = stringTableOffset + _stringPosition;

        // Ensure buffer capacity
        EnsureCapacity(totalSize);

        // Write header
        _position = 0;
        WriteInt32(_patches.Count);
        WriteInt32(stringTableOffset);

        // Write patch entries
        foreach (var patch in _patches)
        {
            WriteInt32((int)patch.Type);
            WriteInt32(patch.Field1);
            WriteInt32(patch.Field2);
            WriteInt32(patch.Field3);
        }

        // Write string table from pooled buffer
        Buffer.BlockCopy(_stringBuffer, 0, _buffer, _position, _stringPosition);
        _position += _stringPosition;

        return new Memory<byte>(_buffer, 0, totalSize);
    }

    /// <summary>
    /// Resets the writer for reuse.
    /// </summary>
    public void Reset()
    {
        _position = 0;
        _stringPosition = 0;
        _patches.Clear();
        _stringIndices.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteInt32(int value)
    {
        // Little-endian write
        _buffer[_position++] = (byte)value;
        _buffer[_position++] = (byte)(value >> 8);
        _buffer[_position++] = (byte)(value >> 16);
        _buffer[_position++] = (byte)(value >> 24);
    }

    private void EnsureCapacity(int required)
    {
        if (_buffer.Length >= required)
        {
            return;
        }

        var newCapacity = Math.Max(_buffer.Length * 2, required);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);

        if (_position > 0)
        {
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _position);
        }

        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(_buffer);
        ArrayPool<byte>.Shared.Return(_stringBuffer);
        _buffer = Array.Empty<byte>();
        _stringBuffer = Array.Empty<byte>();
        _disposed = true;
    }
}

/// <summary>
/// Pool for RenderBatchWriter instances to avoid allocation overhead.
/// </summary>
public static class RenderBatchWriterPool
{
    // WASM is single-threaded, so Stack is fine
    private static readonly Stack<RenderBatchWriter> _pool = new();
    private const int MaxPoolSize = 4;

    /// <summary>
    /// Rents a RenderBatchWriter from the pool or creates a new one.
    /// </summary>
    public static RenderBatchWriter Rent()
    {
        if (_pool.TryPop(out var writer))
        {
            return writer;
        }
        return new RenderBatchWriter();
    }

    /// <summary>
    /// Returns a RenderBatchWriter to the pool for reuse.
    /// </summary>
    public static void Return(RenderBatchWriter writer)
    {
        if (_pool.Count < MaxPoolSize)
        {
            writer.Reset();
            _pool.Push(writer);
        }
        else
        {
            writer.Dispose();
        }
    }
}
