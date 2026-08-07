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

    /// <summary>Two-state switch. Set via Properties.IsOn, listen via Events.OnToggled.</summary>
    public static Node ToggleSwitch(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ToggleSwitch", attributes, [], id);

    /// <summary>
    /// Drop-down list. Children must be <see cref="ComboBoxItem"/>; select via
    /// Properties.SelectedIndex and listen via Events.OnSelectionChanged.
    /// </summary>
    public static Node ComboBox(DOM.Attribute[] attributes, Node[] items, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ComboBox", attributes, items, id);

    /// <summary>An entry in a <see cref="ComboBox"/>; content is carried as a "Content" attribute.</summary>
    public static Node ComboBoxItem(DOM.Attribute[] attributes, string content, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ComboBoxItem", [.. attributes, new DOM.Attribute(id ?? string.Empty, "Content", content)], [], id);

    // =========================================================================
    // Progress
    // =========================================================================

    /// <summary>Indeterminate-by-default spinner; control with Properties.IsActive.</summary>
    public static Node ProgressRing(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ProgressRing", attributes, [], id);

    /// <summary>Determinate bar via Properties.Minimum/Maximum/Value, or Properties.IsIndeterminate.</summary>
    public static Node ProgressBar(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ProgressBar", attributes, [], id);

    // =========================================================================
    // Element content
    // =========================================================================
    // The string-content overloads above cover the common case. These take a
    // Node instead, for controls whose content is itself a tree.

    /// <summary>Button whose content is an element tree rather than a string.</summary>
    public static Node Button(DOM.Attribute[] attributes, Node content, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("Button", attributes, [content], id);

    /// <summary>Bare content host, for wrapping a subtree without adding layout.</summary>
    public static Node ContentControl(DOM.Attribute[] attributes, Node content, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ContentControl", attributes, [content], id);
}
