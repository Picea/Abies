using System.Collections.Frozen;
using System.Text.Json;

namespace Picea.Abies.Html;

internal static class EventDataDeserializers
{
    private static readonly FrozenDictionary<Type, Func<string, object?>> Deserializers =
        new Dictionary<Type, Func<string, object?>>
        {
            [typeof(InputEventData)] = json =>
                JsonSerializer.Deserialize(json, AbiesEventJsonContext.Default.InputEventData),
            [typeof(KeyEventData)] = json =>
                JsonSerializer.Deserialize(json, AbiesEventJsonContext.Default.KeyEventData),
            [typeof(PointerEventData)] = json =>
                JsonSerializer.Deserialize(json, AbiesEventJsonContext.Default.PointerEventData),
            [typeof(ScrollEventData)] = json =>
                JsonSerializer.Deserialize(json, AbiesEventJsonContext.Default.ScrollEventData),
            [typeof(GenericEventData)] = json =>
                JsonSerializer.Deserialize(json, AbiesEventJsonContext.Default.GenericEventData),
        }.ToFrozenDictionary();

    public static Func<string, object?> Get<T>() =>
        Deserializers.TryGetValue(typeof(T), out var deserializer)
            ? deserializer
            : throw new InvalidOperationException(
                $"No source-generated deserializer registered for event data type '{typeof(T).Name}'. " +
                $"Register it in {nameof(AbiesEventJsonContext)} and {nameof(EventDataDeserializers)}.");
}
