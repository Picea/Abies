// =============================================================================
// Event Serialization — JSON (De)Serialization for KurrentDB
// =============================================================================
// Provides serialize/deserialize delegates for UserEvent and ArticleEvent
// compatible with the KurrentDBEventStore<TEvent> constructor.
//
// Uses System.Text.Json with [JsonDerivedType] for polymorphic serialization.
// The event type discriminator is a string matching the record type name,
// stored as KurrentDB event type metadata for human-readable stream inspection.
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.User;

namespace Picea.Abies.Conduit.Api.Infrastructure;

/// <summary>
/// JSON converter that deserializes <see cref="IReadOnlySet{T}"/> as <see cref="HashSet{T}"/>.
/// System.Text.Json cannot instantiate interface types; this converter bridges the gap
/// so that domain events can use immutable collection interfaces while remaining serializable.
/// </summary>
file sealed class ReadOnlySetConverter<T> : JsonConverter<IReadOnlySet<T>>
{
    public override IReadOnlySet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<HashSet<T>>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

/// <summary>
/// JSON converter factory for <see cref="Option{T}"/>.
/// Serializes <c>Some(value)</c> as the value itself and <c>None</c> as <c>null</c>.
/// On deserialization, <c>null</c> becomes <c>None</c> and any non-null value becomes <c>Some</c>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Option{T}"/> is a readonly struct with private fields, so System.Text.Json
/// cannot construct it via default (de)serialization — it would always produce <c>None</c>
/// regardless of the serialized value. This converter ensures faithful round-tripping.
/// </para>
/// <para>
/// Follows the Null Object mapping: <c>Some(x) ↔ x</c>, <c>None ↔ null</c>.
/// This is the natural isomorphism between <c>Option&lt;T&gt;</c> and nullable JSON values.
/// </para>
/// </remarks>
file sealed class OptionConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class OptionConverter<T> : JsonConverter<Option<T>>
    {
        public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return Option<T>.None;

            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            return value is null ? Option<T>.None : Option<T>.Some(value);
        }

        public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
        {
            if (value.IsSome)
                JsonSerializer.Serialize(writer, value.Value, options);
            else
                writer.WriteNullValue();
        }
    }
}

/// <summary>
/// JSON serialization configuration for domain events stored in KurrentDB.
/// </summary>
public static class EventSerialization
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new ReadOnlySetConverter<Tag>(),
            new OptionConverterFactory()
        }
    };

    // ─── UserEvent ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Serializes a <see cref="UserEvent"/> to a KurrentDB-compatible tuple.
    /// </summary>
    public static (string EventType, ReadOnlyMemory<byte> Data) SerializeUserEvent(UserEvent @event) =>
        (@event.GetType().Name, JsonSerializer.SerializeToUtf8Bytes<object>(@event, Options));

    /// <summary>
    /// Deserializes a <see cref="UserEvent"/> from KurrentDB event type and data.
    /// </summary>
    public static UserEvent DeserializeUserEvent(string eventType, ReadOnlyMemory<byte> data) =>
        eventType switch
        {
            nameof(UserEvent.Registered) =>
                JsonSerializer.Deserialize<UserEvent.Registered>(data.Span, Options)!,
            nameof(UserEvent.ProfileUpdated) =>
                JsonSerializer.Deserialize<UserEvent.ProfileUpdated>(data.Span, Options)!,
            nameof(UserEvent.Followed) =>
                JsonSerializer.Deserialize<UserEvent.Followed>(data.Span, Options)!,
            nameof(UserEvent.Unfollowed) =>
                JsonSerializer.Deserialize<UserEvent.Unfollowed>(data.Span, Options)!,
            _ => throw new InvalidOperationException($"Unknown UserEvent type: {eventType}")
        };

    // ─── ArticleEvent ───────────────────────────────────────────────────────────

    /// <summary>
    /// Serializes an <see cref="ArticleEvent"/> to a KurrentDB-compatible tuple.
    /// </summary>
    public static (string EventType, ReadOnlyMemory<byte> Data) SerializeArticleEvent(ArticleEvent @event) =>
        (@event.GetType().Name, JsonSerializer.SerializeToUtf8Bytes<object>(@event, Options));

    /// <summary>
    /// Deserializes an <see cref="ArticleEvent"/> from KurrentDB event type and data.
    /// </summary>
    public static ArticleEvent DeserializeArticleEvent(string eventType, ReadOnlyMemory<byte> data) =>
        eventType switch
        {
            nameof(ArticleEvent.ArticleCreated) =>
                JsonSerializer.Deserialize<ArticleEvent.ArticleCreated>(data.Span, Options)!,
            nameof(ArticleEvent.ArticleUpdated) =>
                JsonSerializer.Deserialize<ArticleEvent.ArticleUpdated>(data.Span, Options)!,
            nameof(ArticleEvent.ArticleDeleted) =>
                JsonSerializer.Deserialize<ArticleEvent.ArticleDeleted>(data.Span, Options)!,
            nameof(ArticleEvent.CommentAdded) =>
                JsonSerializer.Deserialize<ArticleEvent.CommentAdded>(data.Span, Options)!,
            nameof(ArticleEvent.CommentDeleted) =>
                JsonSerializer.Deserialize<ArticleEvent.CommentDeleted>(data.Span, Options)!,
            nameof(ArticleEvent.ArticleFavorited) =>
                JsonSerializer.Deserialize<ArticleEvent.ArticleFavorited>(data.Span, Options)!,
            nameof(ArticleEvent.ArticleUnfavorited) =>
                JsonSerializer.Deserialize<ArticleEvent.ArticleUnfavorited>(data.Span, Options)!,
            _ => throw new InvalidOperationException($"Unknown ArticleEvent type: {eventType}")
        };
}
