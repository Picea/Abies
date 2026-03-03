// =============================================================================
// Head Content Types
// =============================================================================
// Platform-independent types for managing <head> elements in Abies
// applications. Supports meta tags, Open Graph, Twitter Cards, link tags,
// structured data (JSON-LD), and base href.
//
// These are pure data types with no browser dependencies. The browser-specific
// diffing and application logic is in Abies.Browser (HeadDiff class).
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
