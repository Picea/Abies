using System.Text.Json.Serialization;

namespace Picea.Abies.Html;

[JsonSerializable(typeof(InputEventData))]
[JsonSerializable(typeof(KeyEventData))]
[JsonSerializable(typeof(PointerEventData))]
[JsonSerializable(typeof(ScrollEventData))]
[JsonSerializable(typeof(GenericEventData))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class AbiesEventJsonContext : JsonSerializerContext;
