using System.Text.Json.Serialization;

namespace Abies.Html;

/// <summary>Data for input events.</summary>
public record InputEventData([
    property: JsonPropertyName("value")] string? Value);

/// <summary>Data for keyboard events.</summary>
public record KeyEventData([
    property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("repeat")] bool Repeat,
    [property: JsonPropertyName("altKey")] bool AltKey,
    [property: JsonPropertyName("ctrlKey")] bool CtrlKey,
    [property: JsonPropertyName("shiftKey")] bool ShiftKey);

/// <summary>Data for pointer or mouse events.</summary>
public record PointerEventData([
    property: JsonPropertyName("clientX")] double ClientX,
    [property: JsonPropertyName("clientY")] double ClientY,
    [property: JsonPropertyName("button")] int Button);

/// <summary>Data for scroll events on scrollable elements.</summary>
/// <remarks>
/// Contains scroll position and dimensions needed for virtualization.
/// Populated by the JS event handler from the scroll target element.
/// </remarks>
/// <example>
/// <code>
/// onscroll((ScrollEventData? data) =>
///     new Message.ScrollChanged(data?.ScrollTop ?? 0))
/// </code>
/// </example>
public record ScrollEventData(
    [property: JsonPropertyName("scrollTop")] double ScrollTop,
    [property: JsonPropertyName("scrollLeft")] double ScrollLeft,
    [property: JsonPropertyName("scrollHeight")] double ScrollHeight,
    [property: JsonPropertyName("scrollWidth")] double ScrollWidth,
    [property: JsonPropertyName("clientHeight")] double ClientHeight,
    [property: JsonPropertyName("clientWidth")] double ClientWidth);

/// <summary>Generic event data encompassing common fields.</summary>
public record GenericEventData([
    property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("checked")] bool? Checked,
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("repeat")] bool? Repeat,
    [property: JsonPropertyName("altKey")] bool AltKey,
    [property: JsonPropertyName("ctrlKey")] bool CtrlKey,
    [property: JsonPropertyName("shiftKey")] bool ShiftKey,
    [property: JsonPropertyName("clientX")] double? ClientX,
    [property: JsonPropertyName("clientY")] double? ClientY,
    [property: JsonPropertyName("button")] int? Button);
