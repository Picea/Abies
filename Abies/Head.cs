// =============================================================================
// Head Content Management
// =============================================================================
// Provides types and helper functions for managing <head> elements in Abies
// applications. Supports meta tags, Open Graph, Twitter Cards, link tags,
// structured data (JSON-LD), and base href.
//
// The runtime diffs HeadContent between renders and applies only the changes
// to the real <head> element via the binary render batch protocol, unified
// with body DOM patching for a single JS interop call per render cycle.
//
// Architecture Decision Records:
// - ADR-001: Model-View-Update Architecture (docs/adr/ADR-001-mvu-architecture.md)
// =============================================================================

using System.Text.Json;

namespace Abies;

/// <summary>
/// Represents an element managed by Abies in the document <c>&lt;head&gt;</c>.
/// This is a sum type (discriminated union) with variants for each kind of
/// head element: meta tags, link tags, script tags, and base href.
/// </summary>
/// <remarks>
/// Each variant produces a stable <see cref="Key"/> used for diffing between
/// renders. When the key matches between old and new head content, the element
/// is updated in place. When a key is absent from the new render, the element
/// is removed. When a key is new, the element is added.
///
/// Managed elements are tagged with <c>data-abies-head</c> in the real DOM
/// so they never conflict with user-defined head elements.
/// </remarks>
public interface HeadContent
{
    /// <summary>
    /// Stable identity key for diffing. Two <see cref="HeadContent"/> values
    /// with the same key represent the same logical head element.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Renders this head content to an HTML string for insertion into the <c>&lt;head&gt;</c>.
    /// Includes the <c>data-abies-head</c> attribute for managed element identification.
    /// </summary>
    string ToHtml();

    /// <summary>
    /// A <c>&lt;meta name="..." content="..."&gt;</c> tag.
    /// Identity is based on the <c>name</c> attribute.
    /// </summary>
    /// <example>
    /// <code>
    /// Head.meta("description", "A social blogging site")
    /// // renders: &lt;meta name="description" content="A social blogging site" data-abies-head="meta:description"&gt;
    /// </code>
    /// </example>
    sealed record Meta(string Name, string Content) : HeadContent
    {
        /// <inheritdoc />
        public string Key => $"meta:{Name}";

        /// <inheritdoc />
        public string ToHtml() =>
            $"""<meta name="{Name}" content="{HtmlEncode(Content)}" data-abies-head="{Key}">""";
    }

    /// <summary>
    /// A <c>&lt;meta property="..." content="..."&gt;</c> tag for Open Graph and similar protocols.
    /// Identity is based on the <c>property</c> attribute.
    /// </summary>
    /// <example>
    /// <code>
    /// Head.og("title", "My Article")
    /// // renders: &lt;meta property="og:title" content="My Article" data-abies-head="property:og:title"&gt;
    /// </code>
    /// </example>
    sealed record MetaProperty(string Property, string Content) : HeadContent
    {
        /// <inheritdoc />
        public string Key => $"property:{Property}";

        /// <inheritdoc />
        public string ToHtml() =>
            $"""<meta property="{Property}" content="{HtmlEncode(Content)}" data-abies-head="{Key}">""";
    }

    /// <summary>
    /// A <c>&lt;link rel="..." href="..."&gt;</c> tag.
    /// Identity is based on <c>rel</c> + <c>href</c> to allow multiple stylesheets.
    /// </summary>
    /// <example>
    /// <code>
    /// Head.canonical("/article/my-article")
    /// // renders: &lt;link rel="canonical" href="/article/my-article" data-abies-head="link:canonical:/article/my-article"&gt;
    /// </code>
    /// </example>
    sealed record Link(string Rel, string Href, string? Type = null) : HeadContent
    {
        /// <inheritdoc />
        public string Key => $"link:{Rel}:{Href}";

        /// <inheritdoc />
        public string ToHtml() => Type is not null
            ? $"""<link rel="{Rel}" href="{HtmlEncode(Href)}" type="{Type}" data-abies-head="{Key}">"""
            : $"""<link rel="{Rel}" href="{HtmlEncode(Href)}" data-abies-head="{Key}">""";
    }

    /// <summary>
    /// A <c>&lt;script type="..." &gt;...&lt;/script&gt;</c> tag, typically for JSON-LD structured data.
    /// Identity is based on the <c>type</c> attribute.
    /// </summary>
    /// <example>
    /// <code>
    /// Head.jsonLd(new { @context = "https://schema.org", @type = "Article", headline = "My Article" })
    /// // renders: &lt;script type="application/ld+json" data-abies-head="script:application/ld+json"&gt;{...}&lt;/script&gt;
    /// </code>
    /// </example>
    sealed record Script(string Type, string Content) : HeadContent
    {
        /// <inheritdoc />
        public string Key => $"script:{Type}";

        /// <inheritdoc />
        public string ToHtml() =>
            $"""<script type="{Type}" data-abies-head="{Key}">{Content}</script>""";
    }

    /// <summary>
    /// A <c>&lt;base href="..."&gt;</c> tag. There can only be one per document.
    /// </summary>
    sealed record Base(string Href) : HeadContent
    {
        /// <inheritdoc />
        public string Key => "base";

        /// <inheritdoc />
        public string ToHtml() =>
            $"""<base href="{HtmlEncode(Href)}" data-abies-head="base">""";
    }

    /// <summary>
    /// Minimal HTML attribute value encoding for head element content.
    /// </summary>
    private static string HtmlEncode(string value) =>
        value.Replace("&", "&amp;")
             .Replace("\"", "&quot;")
             .Replace("<", "&lt;")
             .Replace(">", "&gt;");
}

/// <summary>
/// Convenience factory functions for creating <see cref="HeadContent"/> values.
/// Import with <c>using static Abies.Head;</c> for concise usage in View functions.
/// </summary>
/// <example>
/// <code>
/// using static Abies.Head;
///
/// public static Document View(Model model)
///     => new Document(
///         "Conduit - Home",
///         WithLayout(HomePage(model), model),
///         meta("description", "A social blogging site"),
///         og("title", "Conduit"),
///         og("type", "website"),
///         canonical("/")
///     );
/// </code>
/// </example>
public static class Head
{
    /// <summary>Creates a <c>&lt;meta name="..." content="..."&gt;</c> tag.</summary>
    public static HeadContent meta(string name, string content) =>
        new HeadContent.Meta(name, content);

    /// <summary>Creates an Open Graph <c>&lt;meta property="og:..." content="..."&gt;</c> tag.</summary>
    public static HeadContent og(string property, string content) =>
        new HeadContent.MetaProperty($"og:{property}", content);

    /// <summary>Creates a Twitter Card <c>&lt;meta name="twitter:..." content="..."&gt;</c> tag.</summary>
    public static HeadContent twitter(string name, string content) =>
        new HeadContent.Meta($"twitter:{name}", content);

    /// <summary>Creates a <c>&lt;link rel="canonical" href="..."&gt;</c> tag.</summary>
    public static HeadContent canonical(string href) =>
        new HeadContent.Link("canonical", href);

    /// <summary>Creates a <c>&lt;link rel="stylesheet" href="..."&gt;</c> tag.</summary>
    public static HeadContent stylesheet(string href) =>
        new HeadContent.Link("stylesheet", href);

    /// <summary>Creates a <c>&lt;link rel="preload" href="..." as="..."&gt;</c> tag.</summary>
    public static HeadContent preload(string href, string asType) =>
        new HeadContent.Link("preload", href, asType);

    /// <summary>Creates a JSON-LD structured data <c>&lt;script type="application/ld+json"&gt;</c> tag.</summary>
    public static HeadContent jsonLd(object data) =>
        new HeadContent.Script("application/ld+json", JsonSerializer.Serialize(data));

    /// <summary>Creates a <c>&lt;link rel="..." href="..."&gt;</c> tag with custom rel.</summary>
    public static HeadContent link(string rel, string href, string? type = null) =>
        new HeadContent.Link(rel, href, type);

    /// <summary>Creates a <c>&lt;meta property="..." content="..."&gt;</c> tag with custom property.</summary>
    public static HeadContent property(string prop, string content) =>
        new HeadContent.MetaProperty(prop, content);

    /// <summary>Creates a <c>&lt;base href="..."&gt;</c> tag.</summary>
    public static HeadContent @base(string href) =>
        new HeadContent.Base(href);
}

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
