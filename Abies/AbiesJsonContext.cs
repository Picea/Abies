using System.Text.Json.Serialization;
using Abies.Html;

namespace Abies;

/// <summary>
/// Source-generated JSON serialization context for all Abies framework data types.
/// Covers event data (DOM events) and subscription data (browser subscriptions).
/// This enables trim-safe and AOT-compatible JSON serialization.
/// </summary>
[JsonSerializable(typeof(InputEventData))]
[JsonSerializable(typeof(KeyEventData))]
[JsonSerializable(typeof(PointerEventData))]
[JsonSerializable(typeof(GenericEventData))]
[JsonSerializable(typeof(AnimationFrameData))]
[JsonSerializable(typeof(AnimationFrameDeltaData))]
[JsonSerializable(typeof(ViewportSize))]
[JsonSerializable(typeof(VisibilityEventData))]
[JsonSerializable(typeof(WebSocketEventData))]
[JsonSerializable(typeof(WebSocketOptions))]
[JsonSerializable(typeof(string))]
internal partial class AbiesJsonContext : JsonSerializerContext;
