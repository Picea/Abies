using System.Collections.Generic;
using System.Collections.Immutable;

namespace Abies.Analyzers;

/// <summary>
/// HTML specification knowledge base for analyzer rules.
/// Contains content model categories, required attributes, and element metadata.
/// </summary>
/// <remarks>
/// Based on the WHATWG HTML Living Standard:
/// https://html.spec.whatwg.org/multipage/dom.html#content-models
/// </remarks>
internal static class HtmlSpec
{
    /// <summary>
    /// Elements that accept only phrasing content (inline elements).
    /// Placing flow content (block elements) inside these is a content model violation.
    /// </summary>
    public static readonly ImmutableHashSet<string> PhrasingOnlyParents = ImmutableHashSet.Create(
        // Text-level semantics (phrasing content model)
        "span", "strong", "em", "small", "s", "cite", "q", "dfn",
        "abbr", "code", "var", "samp", "kbd", "sub", "sup",
        "i", "b", "u", "mark", "bdi", "bdo", "time",
        // Heading elements (phrasing content model)
        "h1", "h2", "h3", "h4", "h5", "h6",
        // Other phrasing-only parents
        "label", "legend", "caption", "summary", "figcaption",
        "dt", "option", "p"
    );

    /// <summary>
    /// Flow content elements (block-level elements) that should NOT appear
    /// inside phrasing-only parents.
    /// </summary>
    public static readonly ImmutableHashSet<string> FlowContentElements = ImmutableHashSet.Create(
        // Sectioning content
        "article", "aside", "nav", "section",
        // Heading content
        "h1", "h2", "h3", "h4", "h5", "h6", "hgroup",
        // Grouping content
        "div", "p", "hr", "pre", "blockquote",
        "ol", "ul", "li", "dl", "dd",
        "figure", "figcaption", "main",
        // Table content
        "table", "thead", "tbody", "tfoot", "tr", "td", "th",
        "caption", "colgroup", "col",
        // Form content
        "form", "fieldset",
        // Interactive content  
        "details", "dialog",
        // Other flow content
        "header", "footer", "address"
    );

    /// <summary>
    /// Required attributes per element, mapping element name → set of required attribute names.
    /// </summary>
    public static readonly ImmutableDictionary<string, ImmutableArray<RequiredAttribute>> RequiredAttributes =
        ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, ImmutableArray<RequiredAttribute>>(
                "img",
                ImmutableArray.Create(
                    new RequiredAttribute("alt", "ABIES001")
                )),
        });

    /// <summary>
    /// Recommended attributes per element, mapping element name → set of recommended attribute names.
    /// These produce Info-level diagnostics.
    /// </summary>
    public static readonly ImmutableDictionary<string, ImmutableArray<RecommendedAttribute>> RecommendedAttributes =
        ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, ImmutableArray<RecommendedAttribute>>(
                "a",
                ImmutableArray.Create(
                    new RecommendedAttribute("href", "ABIES003")
                )),
            new KeyValuePair<string, ImmutableArray<RecommendedAttribute>>(
                "button",
                ImmutableArray.Create(
                    new RecommendedAttribute("type", "ABIES004")
                )),
            new KeyValuePair<string, ImmutableArray<RecommendedAttribute>>(
                "input",
                ImmutableArray.Create(
                    new RecommendedAttribute("type", "ABIES005")
                )),
        });
}

/// <summary>
/// Represents an attribute that the HTML spec requires on an element.
/// </summary>
internal readonly struct RequiredAttribute
{
    public string AttributeName { get; }
    public string DiagnosticId { get; }

    public RequiredAttribute(string attributeName, string diagnosticId)
    {
        AttributeName = attributeName;
        DiagnosticId = diagnosticId;
    }
}

/// <summary>
/// Represents an attribute that is strongly recommended on an element.
/// </summary>
internal readonly struct RecommendedAttribute
{
    public string AttributeName { get; }
    public string DiagnosticId { get; }

    public RecommendedAttribute(string attributeName, string diagnosticId)
    {
        AttributeName = attributeName;
        DiagnosticId = diagnosticId;
    }
}
