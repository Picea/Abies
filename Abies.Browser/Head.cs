// =============================================================================
// Head Content Diffing and Application (Browser)
// =============================================================================
// Browser-specific diffing and application logic for HeadContent elements.
// Computes minimal patches and applies them via the binary render batch protocol.
//
// The platform-independent HeadContent types and Head factory are in Abies/Head.cs.
// =============================================================================

namespace Abies;

/// <summary>
/// Diffing and application logic for <see cref="HeadContent"/> elements.
/// Computes the minimal set of add/update/remove operations needed to
/// transform the old head state into the new head state.
/// </summary>
public static class HeadDiff
{
    /// <summary>
    /// Represents an operation to apply to the document <c>&lt;head&gt;</c>.
    /// </summary>
    public interface HeadPatch
    {
        /// <summary>Add a new element to <c>&lt;head&gt;</c>.</summary>
        sealed record Add(HeadContent Content) : HeadPatch;

        /// <summary>Update an existing element in <c>&lt;head&gt;</c>.</summary>
        sealed record Update(HeadContent Content) : HeadPatch;

        /// <summary>Remove an element from <c>&lt;head&gt;</c> by its key.</summary>
        sealed record Remove(string Key) : HeadPatch;
    }

    /// <summary>
    /// Computes the patches needed to transform <paramref name="oldHead"/> into <paramref name="newHead"/>.
    /// </summary>
    /// <param name="oldHead">The previous head content (empty array for first render).</param>
    /// <param name="newHead">The new head content.</param>
    /// <returns>A list of patches to apply.</returns>
    public static List<HeadPatch> Diff(ReadOnlySpan<HeadContent> oldHead, ReadOnlySpan<HeadContent> newHead)
    {
        var patches = new List<HeadPatch>();

        // Build dictionary of old head content by key
        var oldByKey = new Dictionary<string, HeadContent>(oldHead.Length);
        foreach (var item in oldHead)
        {
            oldByKey[item.Key] = item;
        }

        // Process new head content
        foreach (var newItem in newHead)
        {
            if (oldByKey.TryGetValue(newItem.Key, out var oldItem))
            {
                // Key exists in both — update if content changed
                if (!newItem.Equals(oldItem))
                {
                    patches.Add(new HeadPatch.Update(newItem));
                }
                oldByKey.Remove(newItem.Key);
            }
            else
            {
                // Key only in new — add
                patches.Add(new HeadPatch.Add(newItem));
            }
        }

        // Remaining keys in old but not in new — remove
        foreach (var key in oldByKey.Keys)
        {
            patches.Add(new HeadPatch.Remove(key));
        }

        return patches;
    }

    /// <summary>
    /// Writes head patches to a <see cref="DOM.RenderBatchWriter"/> for inclusion
    /// in a binary render batch. This allows head patches to be sent in the same
    /// batch as body patches, eliminating separate JS interop calls.
    /// </summary>
    /// <param name="patches">The patches to write.</param>
    /// <param name="writer">The binary batch writer to append patches to.</param>
    public static void WriteTo(List<HeadPatch> patches, DOM.RenderBatchWriter writer)
    {
        foreach (var patch in patches)
        {
            switch (patch)
            {
                case HeadPatch.Add add:
                    writer.WriteAddHeadElement(add.Content.Key, add.Content.ToHtml());
                    break;
                case HeadPatch.Update update:
                    writer.WriteUpdateHeadElement(update.Content.Key, update.Content.ToHtml());
                    break;
                case HeadPatch.Remove remove:
                    writer.WriteRemoveHeadElement(remove.Key);
                    break;
            }
        }
    }

    /// <summary>
    /// Applies head patches via the binary render batch protocol.
    /// Used for standalone head updates (e.g., initial render).
    /// For combined body + head updates, use <see cref="WriteTo"/> instead.
    /// </summary>
    /// <param name="patches">The patches to apply.</param>
    public static void Apply(List<HeadPatch> patches)
    {
        if (patches.Count == 0)
        {
            return;
        }

        var writer = DOM.RenderBatchWriterPool.Rent();
        try
        {
            WriteTo(patches, writer);
            var memory = writer.ToMemory();
            Interop.ApplyBinaryBatch(memory.Span);
        }
        finally
        {
            DOM.RenderBatchWriterPool.Return(writer);
        }
    }
}
