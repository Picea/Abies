using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace Abies;

public static partial class Runtime
{
    [JSExport]
    public static void DispatchData(string messageId, string? json)
    {
        if (_dataHandlers.TryGetValue(messageId, out var entry))
        {
            object? data = json is null ? null : JsonSerializer.Deserialize(json, entry.dataType, AbiesJsonContext.Default);
            var message = entry.handler(data);
            Dispatch(message);
            return;
        }

        if (_handlers.TryGetValue(messageId, out var message2))
        {
            Dispatch(message2);
            return;
        }

        // Ignore missing handlers gracefully
    }

    [JSExport]
    /// <summary>
    /// Dispatches subscription data from JavaScript into the MVU loop.
    /// </summary>
    public static void DispatchSubscriptionData(string key, string? json)
    {
        if (_subscriptionHandlers.TryGetValue(key, out var entry))
        {
            object? data = json is null ? null : JsonSerializer.Deserialize(json, entry.dataType, AbiesJsonContext.Default);
            var message = entry.handler(data);
            Dispatch(message);
            return;
        }

        // Ignore missing handlers gracefully
    }
}
