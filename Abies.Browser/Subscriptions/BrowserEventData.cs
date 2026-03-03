using System.Text.Json.Serialization;

namespace Abies;

/// <summary>
/// Represents a requestAnimationFrame timestamp.
/// </summary>
public readonly record struct AnimationFrameData(
    [property: JsonPropertyName("timestamp")] double Timestamp);

/// <summary>
/// Represents a requestAnimationFrame timestamp and delta from the previous frame.
/// </summary>
public readonly record struct AnimationFrameDeltaData(
    [property: JsonPropertyName("timestamp")] double Timestamp,
    [property: JsonPropertyName("delta")] double Delta);

/// <summary>
/// Represents the viewport size at a given moment.
/// </summary>
public readonly record struct ViewportSize(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

/// <summary>
/// Represents the document visibility state.
/// </summary>
public enum VisibilityState
{
    Visible,
    Hidden
}

/// <summary>
/// Represents a visibility change event.
/// </summary>
public readonly record struct VisibilityEventData(
    [property: JsonPropertyName("state")] VisibilityState State);

/// <summary>
/// Options for the scroll subscription specifying which element to observe.
/// </summary>
public readonly record struct ScrollSubscriptionOptions(
    [property: JsonPropertyName("elementId")] string? ElementId);
