using System.Text.Json.Serialization;

namespace Picea.Abies.Conduit.App;

// Source-generated JSON metadata for debugger model snapshots.
// This keeps debugger import/export replay trim-safe for WASM AOT.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(Model))]
internal sealed partial class ConduitDebuggerJsonContext : JsonSerializerContext;
