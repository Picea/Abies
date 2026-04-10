using System.Text.Json.Serialization;

namespace Picea.Abies.Testing;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TestHarnessReplaySessionV1))]
[JsonSerializable(typeof(TestHarnessReplayMetadataV1))]
[JsonSerializable(typeof(TestHarnessReplayEntryV1))]
internal sealed partial class TestHarnessReplayJsonContext : JsonSerializerContext;
