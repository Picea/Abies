// =============================================================================
// Native Control Properties
// =============================================================================
// Typed property helpers that encode native control properties as
// string-valued virtual DOM attributes. The diff compares attribute values
// as strings; the backend parses them into native types (enums, doubles,
// Thickness, Brush) when applying patches.
//
// All numeric formatting uses InvariantCulture so diffing and parsing are
// locale-independent.
//
// Two attribute names are special and never reach the backend as properties:
//   - "id"  — overrides the element's virtual DOM id (see Elements.element)
//   - "key" — feeds the diff's keyed child reconciliation
// =============================================================================

using System.Globalization;
using Praefixum;

namespace Picea.Abies.Native;

/// <summary>
/// Layout orientation for stack-like panels. Named <c>StackOrientation</c>
/// rather than <c>Orientation</c> so it does not collide with the
/// <see cref="Properties.Orientation"/> factory when both this namespace and
/// <c>Properties</c> are imported with <c>using static</c> — the method would
/// otherwise hide the type and force an alias at every use site.
/// </summary>
public enum StackOrientation { Vertical, Horizontal }

/// <summary>
/// Platform-neutral alignment. The backend maps Start/End onto
/// Left/Right (horizontal) or Top/Bottom (vertical).
/// </summary>
public enum Alignment { Start, Center, End, Stretch }

/// <summary>
/// Font weight for text-bearing controls. Named <c>TextWeight</c> to avoid
/// colliding with the <see cref="Properties.FontWeight"/> factory; see
/// <see cref="StackOrientation"/>.
/// </summary>
public enum TextWeight { Normal, SemiBold, Bold }

/// <summary>
/// Factory functions for native control property attributes.
/// </summary>
public static class Properties
{
    private static string Num(double v) => v.ToString(CultureInfo.InvariantCulture);

    private static DOM.Attribute Attr(string name, string value, string? id)
        => new(id ?? string.Empty, name, value);

    // =========================================================================
    // Identity & keying
    // =========================================================================

    /// <summary>
    /// Overrides the element's virtual DOM id. Required for elements created
    /// in loops, where the compile-time call-site id is shared by all
    /// iterations. Consumed by the element factory; never sent to the backend.
    /// </summary>
    public static DOM.Attribute Id(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("id", value, id);

    /// <summary>
    /// Reconciliation key for keyed child diffing (minimal moves via LIS).
    /// Never sent to the backend.
    /// </summary>
    public static DOM.Attribute Key(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("key", value, id);

    // =========================================================================
    // Layout
    // =========================================================================

    public static DOM.Attribute Orientation(StackOrientation value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Orientation", value.ToString(), id);

    public static DOM.Attribute Spacing(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Spacing", Num(value), id);

    public static DOM.Attribute Padding(double uniform, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Padding", Num(uniform), id);

    public static DOM.Attribute Padding(double left, double top, double right, double bottom, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Padding", $"{Num(left)},{Num(top)},{Num(right)},{Num(bottom)}", id);

    public static DOM.Attribute Margin(double uniform, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Margin", Num(uniform), id);

    public static DOM.Attribute Margin(double left, double top, double right, double bottom, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Margin", $"{Num(left)},{Num(top)},{Num(right)},{Num(bottom)}", id);

    public static DOM.Attribute Width(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Width", Num(value), id);

    public static DOM.Attribute Height(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Height", Num(value), id);

    public static DOM.Attribute HorizontalAlignment(Alignment value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("HorizontalAlignment", value.ToString(), id);

    public static DOM.Attribute VerticalAlignment(Alignment value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("VerticalAlignment", value.ToString(), id);

    // =========================================================================
    // Grid layout
    // =========================================================================

    /// <summary>Comma-separated row sizes: "Auto", "*", "2*", or a number.</summary>
    public static DOM.Attribute RowDefinitions(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("RowDefinitions", value, id);

    /// <summary>Comma-separated column sizes: "Auto", "*", "2*", or a number.</summary>
    public static DOM.Attribute ColumnDefinitions(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("ColumnDefinitions", value, id);

    /// <summary>Grid.Row attached property.</summary>
    public static DOM.Attribute GridRow(int value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Grid.Row", value.ToString(CultureInfo.InvariantCulture), id);

    /// <summary>Grid.Column attached property.</summary>
    public static DOM.Attribute GridColumn(int value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Grid.Column", value.ToString(CultureInfo.InvariantCulture), id);

    // =========================================================================
    // Appearance
    // =========================================================================

    public static DOM.Attribute FontSize(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("FontSize", Num(value), id);

    public static DOM.Attribute FontWeight(TextWeight value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("FontWeight", value.ToString(), id);

    /// <summary>Foreground color: "#RRGGBB", "#AARRGGBB", or a named color.</summary>
    public static DOM.Attribute Foreground(string color, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Foreground", color, id);

    /// <summary>
    /// Foreground from the platform theme, so it follows light and dark.
    /// Prefer this over a literal colour, which is necessarily wrong in one theme.
    /// </summary>
    public static DOM.Attribute Foreground(ThemeColor color, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Foreground", Theme.Encode(color), id);

    /// <summary>Background color: "#RRGGBB", "#AARRGGBB", or a named color.</summary>
    public static DOM.Attribute Background(string color, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Background", color, id);

    /// <summary>Background from the platform theme, so it follows light and dark.</summary>
    public static DOM.Attribute Background(ThemeColor color, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Background", Theme.Encode(color), id);

    public static DOM.Attribute CornerRadius(double uniform, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("CornerRadius", Num(uniform), id);

    public static DOM.Attribute BorderThickness(double uniform, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("BorderThickness", Num(uniform), id);

    /// <summary>Border brush color: "#RRGGBB", "#AARRGGBB", or a named color.</summary>
    public static DOM.Attribute BorderBrush(string color, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("BorderBrush", color, id);

    /// <summary>Border brush from the platform theme, so it follows light and dark.</summary>
    public static DOM.Attribute BorderBrush(ThemeColor color, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("BorderBrush", Theme.Encode(color), id);

    public static DOM.Attribute Opacity(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Opacity", Num(value), id);

    // =========================================================================
    // Control state
    // =========================================================================

    public static DOM.Attribute IsEnabled(bool value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("IsEnabled", value ? "True" : "False", id);

    /// <summary>
    /// TextBox text (two-way: pair with <c>Events.OnTextChanged</c>).
    /// <para>
    /// The backend skips writes that would not change the text, so ordinary
    /// typing never disturbs the caret. When the model transforms the input
    /// (upper-casing, trimming) the write does happen, and the backend restores
    /// the selection around it, clamped to the new length. A model that
    /// rewrites the whole string will still move the caret — there is no
    /// general answer for where it should land after an arbitrary rewrite.
    /// </para>
    /// </summary>
    public static DOM.Attribute Text(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Text", value, id);

    /// <summary>TextBox placeholder text.</summary>
    public static DOM.Attribute PlaceholderText(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("PlaceholderText", value, id);

    /// <summary>CheckBox checked state (pair with OnToggled).</summary>
    public static DOM.Attribute IsChecked(bool value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("IsChecked", value ? "True" : "False", id);

    /// <summary>Slider minimum.</summary>
    public static DOM.Attribute Minimum(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Minimum", Num(value), id);

    /// <summary>Slider maximum.</summary>
    public static DOM.Attribute Maximum(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Maximum", Num(value), id);

    /// <summary>Slider value (pair with OnValueChanged).</summary>
    public static DOM.Attribute Value(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Value", Num(value), id);

    /// <summary>Image source URI.</summary>
    public static DOM.Attribute Source(string uri, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("Source", uri, id);

    /// <summary>
    /// ScrollViewer vertical scroll position. Writing it scrolls the control;
    /// identical writes are skipped so a scroll-driven model does not fight the
    /// user. Pair with <c>Events.OnScrollChanged</c>.
    /// </summary>
    public static DOM.Attribute VerticalOffset(double value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("VerticalOffset", Num(value), id);

    // =========================================================================
    // Accessibility
    // =========================================================================
    // Controls with visible text — Button, CheckBox, TextBlock — already expose
    // that text to assistive technology. These are for the ones that do not: an
    // icon-only button, a TextBox with no adjacent label, a Slider.

    /// <summary>
    /// The name assistive technology announces for this control. Required for
    /// controls with no visible text of their own.
    /// </summary>
    public static DOM.Attribute AutomationName(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("AutomationName", value, id);

    /// <summary>
    /// Supplementary description announced after the name — the equivalent of a
    /// tooltip for assistive technology. Use for hints, not for the name itself.
    /// </summary>
    public static DOM.Attribute AutomationHelpText(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("AutomationHelpText", value, id);

    /// <summary>
    /// Hides a control from assistive technology. For decoration only — never
    /// for something the user must be able to reach.
    /// </summary>
    public static DOM.Attribute AccessibilityHidden(bool value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("AccessibilityHidden", value ? "True" : "False", id);

    /// <summary>ToggleSwitch state (pair with OnToggled).</summary>
    public static DOM.Attribute IsOn(bool value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("IsOn", value ? "True" : "False", id);

    /// <summary>ComboBox selected index; -1 for no selection (pair with OnSelectionChanged).</summary>
    public static DOM.Attribute SelectedIndex(int value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("SelectedIndex", value.ToString(CultureInfo.InvariantCulture), id);

    /// <summary>Whether a ProgressRing is spinning.</summary>
    public static DOM.Attribute IsActive(bool value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("IsActive", value ? "True" : "False", id);

    /// <summary>Whether a ProgressBar shows indeterminate progress.</summary>
    public static DOM.Attribute IsIndeterminate(bool value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Attr("IsIndeterminate", value ? "True" : "False", id);
}
