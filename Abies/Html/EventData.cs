namespace Abies.Html;

/// <summary>Data for input events.</summary>
public record InputEventData(string? Value);

/// <summary>Data for keyboard events.</summary>
public record KeyEventData(string Key, bool AltKey, bool CtrlKey, bool ShiftKey);

/// <summary>Data for pointer or mouse events.</summary>
public record PointerEventData(double ClientX, double ClientY, int Button);

/// <summary>Generic event data encompassing common fields.</summary>
public record GenericEventData(string? Value, bool? Checked, string? Key, bool AltKey, bool CtrlKey, bool ShiftKey, double? ClientX, double? ClientY, int? Button);
