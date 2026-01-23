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
