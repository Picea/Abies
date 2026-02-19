using Microsoft.CodeAnalysis;

namespace Abies.Analyzers;

/// <summary>
/// Central registry of all Abies HTML analyzer diagnostic descriptors.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string _category = "Abies.Html";
    private const string _accessibilityCategory = "Abies.Html.Accessibility";
    private const string _contentModelCategory = "Abies.Html.ContentModel";

    // =========================================================================
    // ABIES001: img() missing alt attribute
    // =========================================================================
    public static readonly DiagnosticDescriptor ImgMissingAlt = new(
        id: "ABIES001",
        title: "img element missing 'alt' attribute",
        messageFormat: "img() should include an alt() attribute for accessibility; use alt(\"\") for decorative images",
        category: _accessibilityCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The HTML specification requires the 'alt' attribute on <img> elements. Screen readers use this to describe images to users who cannot see them. Use alt(\"\") for purely decorative images.",
        helpLinkUri: "https://developer.mozilla.org/en-US/docs/Web/HTML/Element/img#accessibility");

    // =========================================================================
    // ABIES002: Block element inside phrasing-only (inline) element
    // =========================================================================
    public static readonly DiagnosticDescriptor BlockInsideInline = new(
        id: "ABIES002",
        title: "Block element nested inside inline element",
        messageFormat: "'{0}' (flow content) should not be nested inside '{1}' (phrasing content only)",
        category: _contentModelCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "HTML content model rules prohibit placing flow content (block-level elements like <div>, <p>, <section>) inside elements that only accept phrasing content (like <span>, <strong>, <em>). This can cause unpredictable browser rendering.",
        helpLinkUri: "https://developer.mozilla.org/en-US/docs/Web/HTML/Content_categories#phrasing_content");

    // =========================================================================
    // ABIES003: a() missing href attribute
    // =========================================================================
    public static readonly DiagnosticDescriptor AnchorMissingHref = new(
        id: "ABIES003",
        title: "a element missing 'href' attribute",
        messageFormat: "a() should include an href() attribute; without href the anchor is not interactive",
        category: _category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "An <a> element without an href attribute is not a hyperlink. While technically valid, this is usually unintentional.",
        helpLinkUri: "https://developer.mozilla.org/en-US/docs/Web/HTML/Element/a#href");

    // =========================================================================
    // ABIES004: button() missing type attribute
    // =========================================================================
    public static readonly DiagnosticDescriptor ButtonMissingType = new(
        id: "ABIES004",
        title: "button element missing 'type' attribute",
        messageFormat: "button() should include a type() attribute; default type is 'submit' which may cause unexpected form submissions",
        category: _category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A <button> element without an explicit type attribute defaults to type='submit'. This can cause unexpected form submissions. Always specify type('button'), type('submit'), or type('reset').",
        helpLinkUri: "https://developer.mozilla.org/en-US/docs/Web/HTML/Element/button#type");

    // =========================================================================
    // ABIES005: input() missing type attribute
    // =========================================================================
    public static readonly DiagnosticDescriptor InputMissingType = new(
        id: "ABIES005",
        title: "input element missing 'type' attribute",
        messageFormat: "input() should include a type() attribute; default type is 'text' but being explicit improves readability",
        category: _category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "An <input> element without an explicit type attribute defaults to type='text'. Being explicit about the input type improves code readability and makes the intent clear.",
        helpLinkUri: "https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#type");
}
