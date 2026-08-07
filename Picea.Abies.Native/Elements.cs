// =============================================================================
// Native Control Elements
// =============================================================================
// Factory functions describing native controls as Abies virtual DOM elements.
// Tags are WinUI control names ("StackPanel", "TextBlock", ...); the diff and
// patch machinery is vocabulary-agnostic and reused unchanged.
//
// Rules that keep the HTML fallbacks out of native trees:
//   1. Only Element nodes — text is always a property attribute (Text/Content),
//      never a Text child node. This prevents the diff's Text/RawHtml
//      ReplaceRaw fallback from ever firing.
//   2. Never name a tag Input, Source, Track, Base, or Link (case-insensitive):
//      HtmlSpec.VoidElements would silently skip child diffing for them.
//
// Like the HTML DSL, a user-supplied Properties.Id attribute overrides the
// compile-time Praefixum id — required for elements created in loops.
// =============================================================================

using Picea.Abies.DOM;
using Praefixum;

namespace Picea.Abies.Native;

/// <summary>
/// Factory functions for native control elements.
/// </summary>
public static class Elements
{
    /// <summary>
    /// Core element factory. If the attributes contain an "id" attribute
    /// (from <see cref="Properties.Id"/>), its value becomes the element id
    /// and the attribute is removed; otherwise the Praefixum call-site id is
    /// used.
    /// </summary>
    public static Element element(string tag, DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    {
        var explicitId = Array.Find(attributes, a => a.Name == "id");
        var elementId = explicitId?.Value ?? id!;
        var filteredAttributes = explicitId != null
            ? Array.FindAll(attributes, a => a.Name != "id")
            : attributes;
        return new(elementId, tag, filteredAttributes, children);
    }

    // =========================================================================
    // Panels
    // =========================================================================

    public static Node StackPanel(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("StackPanel", attributes, children, id);

    public static Node Grid(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("Grid", attributes, children, id);

    public static Node Border(DOM.Attribute[] attributes, Node child, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("Border", attributes, [child], id);

    public static Node ScrollViewer(DOM.Attribute[] attributes, Node child, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ScrollViewer", attributes, [child], id);

    // =========================================================================
    // Text & media
    // =========================================================================

    /// <summary>Read-only text. The text is carried as a "Text" attribute.</summary>
    public static Node TextBlock(DOM.Attribute[] attributes, string text, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("TextBlock", [.. attributes, new DOM.Attribute(id ?? string.Empty, "Text", text)], [], id);

    public static Node Image(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("Image", attributes, [], id);

    // =========================================================================
    // Input controls
    // =========================================================================

    /// <summary>Button with string content, carried as a "Content" attribute.</summary>
    public static Node Button(DOM.Attribute[] attributes, string content, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("Button", [.. attributes, new DOM.Attribute(id ?? string.Empty, "Content", content)], [], id);

    /// <summary>Editable text box. Set text via Properties.Text, listen via Events.OnTextChanged.</summary>
    public static Node TextBox(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("TextBox", attributes, [], id);

    /// <summary>CheckBox with string content, carried as a "Content" attribute.</summary>
    public static Node CheckBox(DOM.Attribute[] attributes, string content, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("CheckBox", [.. attributes, new DOM.Attribute(id ?? string.Empty, "Content", content)], [], id);

    /// <summary>Range slider. Set via Properties.Minimum/Maximum/Value, listen via Events.OnValueChanged.</summary>
    public static Node Slider(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("Slider", attributes, [], id);
}
