// =============================================================================
// WinUI Backend
// =============================================================================
// INativeBackend implementation over the WinUI 3 API surface
// (Microsoft.UI.Xaml). Runs on Windows App SDK and, via Uno Platform, on
// macOS/Linux/WebAssembly — this code has no Uno-specific API usage.
//
// String-encoded attribute values are parsed here into native types
// (double, Thickness, GridLength, Brush, enums). Unknown tags or properties
// throw: in a spike, silence hides bugs.
// =============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Picea.Abies.Native;
using Picea.Abies.Native.Rendering;
using System.Globalization;
using Windows.UI;

namespace Picea.Abies.WinUI;

/// <summary>
/// Binds the platform-neutral patch interpreter to WinUI controls.
/// </summary>
public sealed class WinUIBackend : INativeBackend<FrameworkElement>
{
    private readonly Panel _rootHost;
    private readonly Dictionary<(FrameworkElement Control, string EventName), Action> _detachers = [];

    /// <param name="rootHost">The panel that receives the root control (e.g., the window's content grid).</param>
    public WinUIBackend(Panel rootHost) => _rootHost = rootHost;

    // =========================================================================
    // Control factory
    // =========================================================================

    public FrameworkElement CreateControl(string tag, string elementId) => tag switch
    {
        "StackPanel" => new StackPanel(),
        "Grid" => new Grid(),
        "Border" => new Border(),
        "ScrollViewer" => new ScrollViewer(),
        "TextBlock" => new TextBlock(),
        "Button" => new Button(),
        "TextBox" => new TextBox(),
        "CheckBox" => new CheckBox(),
        "Slider" => new Slider(),
        "Image" => new Image(),
        _ => throw new NotSupportedException($"Unknown native tag '{tag}' (element '{elementId}')."),
    };

    // =========================================================================
    // Properties
    // =========================================================================

    public void SetProperty(FrameworkElement control, string tag, string name, string? value)
    {
        // FrameworkElement-level properties, any control.
        switch (name)
        {
            case "Width":
                if (value is null) control.ClearValue(FrameworkElement.WidthProperty);
                else control.Width = D(value);
                return;
            case "Height":
                if (value is null) control.ClearValue(FrameworkElement.HeightProperty);
                else control.Height = D(value);
                return;
            case "Margin":
                if (value is null) control.ClearValue(FrameworkElement.MarginProperty);
                else control.Margin = ParseThickness(value);
                return;
            case "Opacity":
                if (value is null) control.ClearValue(UIElement.OpacityProperty);
                else control.Opacity = D(value);
                return;
            case "HorizontalAlignment":
                if (value is null) control.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
                else control.HorizontalAlignment = value switch
                {
                    "Start" => HorizontalAlignment.Left,
                    "Center" => HorizontalAlignment.Center,
                    "End" => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Stretch,
                };
                return;
            case "VerticalAlignment":
                if (value is null) control.ClearValue(FrameworkElement.VerticalAlignmentProperty);
                else control.VerticalAlignment = value switch
                {
                    "Start" => VerticalAlignment.Top,
                    "Center" => VerticalAlignment.Center,
                    "End" => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Stretch,
                };
                return;
            case "IsEnabled" when control is Control c:
                c.IsEnabled = value is null || bool.Parse(value);
                return;
            case "FontSize" when control is TextBlock tbFs:
                if (value is null) tbFs.ClearValue(TextBlock.FontSizeProperty);
                else tbFs.FontSize = D(value);
                return;
            case "FontSize" when control is Control cFs:
                if (value is null) cFs.ClearValue(Control.FontSizeProperty);
                else cFs.FontSize = D(value);
                return;
            case "Grid.Row":
                Grid.SetRow(control, I(value ?? "0"));
                return;
            case "Grid.Column":
                Grid.SetColumn(control, I(value ?? "0"));
                return;
        }

        // Control-specific properties.
        switch (control)
        {
            case StackPanel sp:
                switch (name)
                {
                    case "Orientation":
                        sp.Orientation = value == "Horizontal"
                            ? Microsoft.UI.Xaml.Controls.Orientation.Horizontal
                            : Microsoft.UI.Xaml.Controls.Orientation.Vertical;
                        return;
                    case "Spacing": sp.Spacing = value is null ? 0 : D(value); return;
                    case "Padding": sp.Padding = value is null ? default : ParseThickness(value); return;
                    case "Background": sp.Background = ParseBrushOrNull(value); return;
                }
                break;
            case Grid g:
                switch (name)
                {
                    case "RowDefinitions":
                        g.RowDefinitions.Clear();
                        foreach (var part in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                            g.RowDefinitions.Add(new RowDefinition { Height = ParseGridLength(part.Trim()) });
                        return;
                    case "ColumnDefinitions":
                        g.ColumnDefinitions.Clear();
                        foreach (var part in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                            g.ColumnDefinitions.Add(new ColumnDefinition { Width = ParseGridLength(part.Trim()) });
                        return;
                    case "Padding": g.Padding = value is null ? default : ParseThickness(value); return;
                    case "Background": g.Background = ParseBrushOrNull(value); return;
                }
                break;
            case Border b:
                switch (name)
                {
                    case "Padding": b.Padding = value is null ? default : ParseThickness(value); return;
                    case "CornerRadius": b.CornerRadius = value is null ? default : new CornerRadius(D(value)); return;
                    case "BorderThickness": b.BorderThickness = value is null ? default : ParseThickness(value); return;
                    case "BorderBrush": b.BorderBrush = ParseBrushOrNull(value); return;
                    case "Background": b.Background = ParseBrushOrNull(value); return;
                }
                break;
            case TextBlock tb:
                switch (name)
                {
                    case "Text": tb.Text = value ?? ""; return;
                    case "FontWeight": tb.FontWeight = ParseFontWeight(value); return;
                    case "Foreground": tb.Foreground = ParseBrushOrNull(value) ?? tb.Foreground; return;
                }
                break;
            case TextBox tx:
                switch (name)
                {
                    case "Text":
                        // Only write on real change: rewriting identical text
                        // still resets the caret on some platforms.
                        var newText = value ?? "";
                        if (tx.Text != newText) tx.Text = newText;
                        return;
                    case "PlaceholderText": tx.PlaceholderText = value ?? ""; return;
                }
                break;
            case CheckBox cb:
                switch (name)
                {
                    case "Content": cb.Content = value; return;
                    case "IsChecked": cb.IsChecked = value is not null && bool.Parse(value); return;
                    case "Foreground": cb.Foreground = ParseBrushOrNull(value) ?? cb.Foreground; return;
                }
                break;
            case Slider s:
                switch (name)
                {
                    case "Minimum": s.Minimum = value is null ? 0 : D(value); return;
                    case "Maximum": s.Maximum = value is null ? 100 : D(value); return;
                    case "Value": s.Value = value is null ? 0 : D(value); return;
                }
                break;
            case Button btn:
                switch (name)
                {
                    case "Content": btn.Content = value; return;
                    case "Padding": btn.Padding = value is null ? default : ParseThickness(value); return;
                    case "Foreground": btn.Foreground = ParseBrushOrNull(value) ?? btn.Foreground; return;
                    case "Background": btn.Background = ParseBrushOrNull(value) ?? btn.Background; return;
                }
                break;
            case ScrollViewer sv:
                switch (name)
                {
                    case "Padding": sv.Padding = value is null ? default : ParseThickness(value); return;
                }
                break;
            case Image img:
                switch (name)
                {
                    case "Source": img.Source = value is null ? null : new BitmapImage(new Uri(value, UriKind.RelativeOrAbsolute)); return;
                }
                break;
        }

        throw new NotSupportedException($"Property '{name}' is not supported on tag '{tag}'.");
    }

    // =========================================================================
    // Children
    // =========================================================================

    public void AppendChild(FrameworkElement parent, string parentTag, FrameworkElement child)
    {
        switch (parent)
        {
            case Panel p: p.Children.Add(child); return;
            case Border b: b.Child = child; return;
            case ContentControl cc: cc.Content = child; return;
        }
        throw ChildrenNotSupported(parentTag);
    }

    public void InsertBefore(FrameworkElement parent, string parentTag, FrameworkElement child, FrameworkElement before)
    {
        if (parent is Panel p)
        {
            p.Children.Insert(p.Children.IndexOf(before), child);
            return;
        }
        AppendChild(parent, parentTag, child);
    }

    public void ReplaceChild(FrameworkElement parent, string parentTag, FrameworkElement oldChild, FrameworkElement newChild)
    {
        switch (parent)
        {
            case Panel p: p.Children[p.Children.IndexOf(oldChild)] = newChild; return;
            case Border b: b.Child = newChild; return;
            case ContentControl cc: cc.Content = newChild; return;
        }
        throw ChildrenNotSupported(parentTag);
    }

    public void RemoveChild(FrameworkElement parent, string parentTag, FrameworkElement child)
    {
        switch (parent)
        {
            case Panel p: p.Children.Remove(child); return;
            case Border b when ReferenceEquals(b.Child, child): b.Child = null; return;
            case ContentControl cc when ReferenceEquals(cc.Content, child): cc.Content = null; return;
        }
    }

    public void MoveChild(FrameworkElement parent, string parentTag, FrameworkElement child, FrameworkElement? before)
    {
        if (parent is not Panel p)
            return; // single-child hosts have nothing to reorder
        p.Children.Remove(child);
        if (before is null)
            p.Children.Add(child);
        else
            p.Children.Insert(p.Children.IndexOf(before), child);
    }

    public void ClearChildren(FrameworkElement parent, string parentTag)
    {
        switch (parent)
        {
            case Panel p: p.Children.Clear(); return;
            case Border b: b.Child = null; return;
            case ContentControl cc: cc.Content = null; return;
        }
    }

    public void SetRoot(FrameworkElement root)
    {
        _rootHost.Children.Clear();
        _rootHost.Children.Add(root);
    }

    private static NotSupportedException ChildrenNotSupported(string parentTag) =>
        new($"Tag '{parentTag}' does not support element children.");

    // =========================================================================
    // Events
    // =========================================================================

    public void AttachEvent(FrameworkElement control, string tag, string eventName, string elementId, INativeEventSink sink)
    {
        switch (control, eventName)
        {
            case (ButtonBase button, "Click"):
            {
                RoutedEventHandler handler = (_, _) => sink.OnNativeEvent(elementId, "Click", null);
                button.Click += handler;
                _detachers[(control, eventName)] = () => button.Click -= handler;
                return;
            }
            case (TextBox textBox, "TextChanged"):
            {
                TextChangedEventHandler handler = (s, _) =>
                {
                    if (!sink.IsApplying)
                        sink.OnNativeEvent(elementId, "TextChanged", new TextChangedData(((TextBox)s).Text));
                };
                textBox.TextChanged += handler;
                _detachers[(control, eventName)] = () => textBox.TextChanged -= handler;
                return;
            }
            case (CheckBox checkBox, "Toggled"):
            {
                RoutedEventHandler handler = (s, _) =>
                {
                    if (!sink.IsApplying)
                        sink.OnNativeEvent(elementId, "Toggled", new ToggledData(((CheckBox)s).IsChecked == true));
                };
                checkBox.Checked += handler;
                checkBox.Unchecked += handler;
                _detachers[(control, eventName)] = () =>
                {
                    checkBox.Checked -= handler;
                    checkBox.Unchecked -= handler;
                };
                return;
            }
            case (Slider slider, "ValueChanged"):
            {
                RangeBaseValueChangedEventHandler handler = (_, e) =>
                {
                    if (!sink.IsApplying)
                        sink.OnNativeEvent(elementId, "ValueChanged", new ValueChangedData(e.NewValue, e.OldValue));
                };
                slider.ValueChanged += handler;
                _detachers[(control, eventName)] = () => slider.ValueChanged -= handler;
                return;
            }
        }

        throw new NotSupportedException($"Event '{eventName}' is not supported on tag '{tag}'.");
    }

    public void DetachEvent(FrameworkElement control, string tag, string eventName)
    {
        if (_detachers.Remove((control, eventName), out var detach))
            detach();
    }

    // =========================================================================
    // Parsing
    // =========================================================================

    private static double D(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static int I(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static Thickness ParseThickness(string value)
    {
        var parts = value.Split(',');
        return parts.Length switch
        {
            1 => new Thickness(D(parts[0])),
            4 => new Thickness(D(parts[0]), D(parts[1]), D(parts[2]), D(parts[3])),
            _ => throw new FormatException($"Invalid Thickness '{value}' — expected 1 or 4 comma-separated numbers."),
        };
    }

    private static GridLength ParseGridLength(string part) => part switch
    {
        "Auto" or "auto" => GridLength.Auto,
        "*" => new GridLength(1, GridUnitType.Star),
        _ when part.EndsWith('*') => new GridLength(D(part[..^1]), GridUnitType.Star),
        _ => new GridLength(D(part), GridUnitType.Pixel),
    };

    private static Windows.UI.Text.FontWeight ParseFontWeight(string? value) => value switch
    {
        "Bold" => Microsoft.UI.Text.FontWeights.Bold,
        "SemiBold" => Microsoft.UI.Text.FontWeights.SemiBold,
        _ => Microsoft.UI.Text.FontWeights.Normal,
    };

    private static Brush? ParseBrushOrNull(string? value) =>
        value is null ? null : new SolidColorBrush(ParseColor(value));

    private static Color ParseColor(string value)
    {
        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            return hex.Length switch
            {
                6 => Color.FromArgb(0xFF, H(hex, 0), H(hex, 2), H(hex, 4)),
                8 => Color.FromArgb(H(hex, 0), H(hex, 2), H(hex, 4), H(hex, 6)),
                _ => throw new FormatException($"Invalid color '{value}' — expected #RRGGBB or #AARRGGBB."),
            };
        }
        return value.ToLowerInvariant() switch
        {
            "black" => Microsoft.UI.Colors.Black,
            "white" => Microsoft.UI.Colors.White,
            "red" => Microsoft.UI.Colors.Red,
            "green" => Microsoft.UI.Colors.Green,
            "blue" => Microsoft.UI.Colors.Blue,
            "gray" or "grey" => Microsoft.UI.Colors.Gray,
            "transparent" => Microsoft.UI.Colors.Transparent,
            _ => throw new FormatException($"Unknown color name '{value}'."),
        };

        static byte H(string hex, int offset) => byte.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber);
    }
}
