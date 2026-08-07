// =============================================================================
// Native Event Data
// =============================================================================
// Event payload records for native control events. Unlike the browser event
// data in Picea.Abies.Html (which is deserialized from JSON produced by
// extractEventData in abies.js), these records are constructed directly by
// the native backend from real event args — no serialization is involved,
// and Handler.Deserializer stays null.
// =============================================================================

namespace Picea.Abies.Native;

/// <summary>Payload for text-input changes (TextBox.TextChanged).</summary>
/// <param name="Text">The current text of the control.</param>
public sealed record TextChangedData(string Text);

/// <summary>Payload for two-state toggles (CheckBox Checked/Unchecked).</summary>
/// <param name="IsChecked">The new checked state.</param>
public sealed record ToggledData(bool IsChecked);

/// <summary>Payload for range-value changes (Slider.ValueChanged).</summary>
/// <param name="NewValue">The new value.</param>
/// <param name="OldValue">The previous value.</param>
public sealed record ValueChangedData(double NewValue, double OldValue);
