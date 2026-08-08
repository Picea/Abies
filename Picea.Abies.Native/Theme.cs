// =============================================================================
// Theme Brushes
// =============================================================================
// A literal colour like "#1F1F1F" is wrong in one of the two themes. WinUI
// already ships a full set of semantic brushes that follow light/dark, so this
// maps onto those rather than reinventing a token system: the theme story is
// "use the platform's", which is also what ADR-027 said it wanted.
//
// Values are carried as "theme:<ResourceKey>" so the backend can tell them from
// literal colours. A key that does not resolve on some platform leaves the
// property unset and reports through the renderer's fault callback, rather than
// throwing — a missing brush should not take down a window.
// =============================================================================

namespace Picea.Abies.Native;

/// <summary>
/// Semantic colour roles, mapped to the platform's theme brushes so they follow
/// light and dark automatically.
/// </summary>
public enum ThemeColor
{
    /// <summary>Primary body and heading text.</summary>
    TextPrimary,

    /// <summary>De-emphasised text: captions, secondary detail.</summary>
    TextSecondary,

    /// <summary>Text belonging to a disabled control.</summary>
    TextDisabled,

    /// <summary>The accent colour, for emphasis and selected state.</summary>
    Accent,

    /// <summary>The window's base background.</summary>
    SurfaceBase,

    /// <summary>A raised surface — cards, list rows, panels above the base.</summary>
    SurfaceCard,

    /// <summary>Default control and divider strokes.</summary>
    Stroke,

    /// <summary>Success state.</summary>
    Success,

    /// <summary>Caution state.</summary>
    Caution,

    /// <summary>Error or destructive state.</summary>
    Critical,
}

/// <summary>Maps semantic roles onto platform theme resource keys.</summary>
public static class Theme
{
    /// <summary>Prefix marking an attribute value as a theme lookup rather than a literal colour.</summary>
    public const string Prefix = "theme:";

    private static readonly Dictionary<ThemeColor, string> _resourceKeys = new()
    {
        [ThemeColor.TextPrimary] = "TextFillColorPrimaryBrush",
        [ThemeColor.TextSecondary] = "TextFillColorSecondaryBrush",
        [ThemeColor.TextDisabled] = "TextFillColorDisabledBrush",
        [ThemeColor.Accent] = "AccentFillColorDefaultBrush",
        [ThemeColor.SurfaceBase] = "SolidBackgroundFillColorBaseBrush",
        [ThemeColor.SurfaceCard] = "CardBackgroundFillColorDefaultBrush",
        [ThemeColor.Stroke] = "ControlStrokeColorDefaultBrush",
        [ThemeColor.Success] = "SystemFillColorSuccessBrush",
        [ThemeColor.Caution] = "SystemFillColorCautionBrush",
        [ThemeColor.Critical] = "SystemFillColorCriticalBrush",
    };

    /// <summary>The platform resource key backing a semantic role.</summary>
    public static string ResourceKey(ThemeColor color) =>
        _resourceKeys.TryGetValue(color, out var key)
            ? key
            : throw new ArgumentOutOfRangeException(nameof(color), color, "No theme resource mapped for this colour.");

    /// <summary>Encodes a semantic role as an attribute value.</summary>
    public static string Encode(ThemeColor color) => Prefix + ResourceKey(color);

    /// <summary>Encodes an explicit platform resource key, for brushes the enum does not name.</summary>
    public static string Encode(string resourceKey) => Prefix + resourceKey;

    /// <summary>
    /// Returns the resource key if <paramref name="value"/> is a theme lookup,
    /// or null if it is a literal colour.
    /// </summary>
    public static string? TryDecode(string? value) =>
        value is not null && value.StartsWith(Prefix, StringComparison.Ordinal)
            ? value[Prefix.Length..]
            : null;
}
