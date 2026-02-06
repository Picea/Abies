// =============================================================================
// Source-Generated JSON Serialization Context
// =============================================================================
// Provides trim-safe JSON serialization for all framework event data types.
// .NET 10 WASM disables reflection-based serialization by default, so all
// types must be registered here for the source generator to emit metadata.
//
// Architecture Decision Records:
// - ADR-005: WebAssembly Runtime (docs/adr/ADR-005-webassembly-runtime.md)
// =============================================================================

using System.Text.Json.Serialization;
using Abies.Html;

namespace Abies;

/// <summary>
/// Source-generated JSON context for Abies framework event and subscription types.
/// Required for trim-safe serialization in .NET 10+ WebAssembly.
/// </summary>
[JsonSerializable(typeof(InputEventData))]
[JsonSerializable(typeof(KeyEventData))]
[JsonSerializable(typeof(PointerEventData))]
[JsonSerializable(typeof(GenericEventData))]
[JsonSerializable(typeof(AnimationFrameData))]
[JsonSerializable(typeof(AnimationFrameDeltaData))]
[JsonSerializable(typeof(ViewportSize))]
[JsonSerializable(typeof(VisibilityEventData))]
[JsonSerializable(typeof(WebSocketOptions))]
[JsonSerializable(typeof(WebSocketEventData))]
internal partial class AbiesJsonContext : JsonSerializerContext;
