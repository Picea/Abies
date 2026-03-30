using System.Text.Json.Serialization;

namespace Picea.Abies.Conduit.App;

// Source-generated JSON metadata for debugger model snapshots.
// Shared by WASM and InteractiveServer hosts to keep replay trim-safe.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(Model))]
public sealed partial class ConduitDebuggerJsonContext : JsonSerializerContext;
