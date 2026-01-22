using System.Text.Json.Serialization;

namespace Abies;

/// <summary>
/// Options for configuring a WebSocket subscription.
/// </summary>
public sealed record WebSocketOptions(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("protocols")] string[]? Protocols = null);

/// <summary>
/// Represents a WebSocket event emitted by the browser.
/// </summary>
public sealed record WebSocketEventData(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("messageKind")] string? MessageKind = null,
    [property: JsonPropertyName("data")] string? Data = null,
    [property: JsonPropertyName("code")] int? Code = null,
    [property: JsonPropertyName("reason")] string? Reason = null,
    [property: JsonPropertyName("wasClean")] bool? WasClean = null);

/// <summary>
/// Discriminated union for WebSocket events.
/// </summary>
public abstract record WebSocketEvent
{
    public sealed record Opened : WebSocketEvent;
    public sealed record Closed(int Code, string Reason, bool WasClean) : WebSocketEvent;
    public sealed record Errored : WebSocketEvent;
    public sealed record MessageReceived(WebSocketMessageKind Kind, string Data) : WebSocketEvent;
}

/// <summary>
/// Describes the payload type for a WebSocket message.
/// </summary>
public enum WebSocketMessageKind
{
    Text,
    Binary
}

public static class WebSocketEventDataModule
{
    public static WebSocketEvent ToEvent(this WebSocketEventData data)
        => data.Type switch
        {
            "open" => new WebSocketEvent.Opened(),
            "close" => new WebSocketEvent.Closed(
                data.Code ?? 1000,
                data.Reason ?? string.Empty,
                data.WasClean ?? true),
            "error" => new WebSocketEvent.Errored(),
            "message" => new WebSocketEvent.MessageReceived(
                ParseKind(data.MessageKind),
                data.Data ?? string.Empty),
            _ => throw new InvalidOperationException($"Unknown WebSocket event type '{data.Type}'.")
        };

    private static WebSocketMessageKind ParseKind(string? kind)
        => kind switch
        {
            "binary" => WebSocketMessageKind.Binary,
            _ => WebSocketMessageKind.Text
        };
}
