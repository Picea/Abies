using Picea.Abies.DOM;
#if DEBUG
using Picea.Abies.Debugger;
#endif

namespace Picea.Abies;

public sealed class HandlerRegistry
{
    private readonly Dictionary<string, Handler> _handlers = new();

    internal Action<Message>? Dispatch { get; set; }

    public void Register(Handler handler)
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("ABIES_DEBUG_HANDLERS") == "1")
            System.Diagnostics.Debug.WriteLine($"[HandlerRegistry] Register: {handler.CommandId} ({handler.Name})");
#endif
        _handlers[handler.CommandId] = handler;
    }

    public void Unregister(string commandId)
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("ABIES_DEBUG_HANDLERS") == "1")
            System.Diagnostics.Debug.WriteLine($"[HandlerRegistry] Unregister: {commandId}");
#endif
        _handlers.Remove(commandId);
    }

    public Message? CreateMessage(string commandId, string eventData)
    {
#if DEBUG
        const string envKey = "ABIES_DEBUG_HANDLERS";
        bool logEnabled = Environment.GetEnvironmentVariable(envKey) == "1";
        if (logEnabled)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[HandlerRegistry] CreateMessage called. CommandId={commandId}, RegisteredCount={_handlers.Count}, EventDataLen={eventData?.Length ?? 0}");
            if (!_handlers.ContainsKey(commandId))
                System.Diagnostics.Debug.WriteLine(
                    $"[HandlerRegistry] MISS: CommandId={commandId} not found. Available: {string.Join(", ", _handlers.Keys.Take(10))}{(_handlers.Count > 10 ? "..." : "")}");
        }
#endif
        
        if (!_handlers.TryGetValue(commandId, out var handler))
            return null;

        if (handler.Command is not null)
            return handler.Command;

        if (handler.WithData is not null && handler.Deserializer is not null)
        {
            var data = string.IsNullOrEmpty(eventData)
                ? null
                : handler.Deserializer(eventData);
            return handler.WithData(data);
        }

        return null;
    }

#if DEBUG
    internal static void CaptureMessageToDebugger(object? message, string modelSnapshotPreview)
    {
        if (DebuggerRuntimeRegistry.CurrentDebugger != null && message != null)
        {
            DebuggerRuntimeRegistry.CurrentDebugger.CaptureMessage(message, modelSnapshotPreview);
        }
    }
#endif

    internal void Clear() => _handlers.Clear();

    public void RegisterHandlers(Node? node)
    {
        switch (node)
        {
            case Element element:
                foreach (var attr in element.Attributes)
                {
                    if (attr is Handler handler)
                    {
                        Register(handler);
                    }
                }

                foreach (var child in element.Children)
                {
                    RegisterHandlers(child);
                }

                break;

            case MemoNode memo:
                RegisterHandlers(memo.CachedNode);
                break;

            case LazyMemoNode lazy:
                RegisterHandlers(lazy.CachedNode ?? lazy.Evaluate());
                break;
        }
    }

    public void UnregisterHandlers(Node? node)
    {
        switch (node)
        {
            case Element element:
                foreach (var attr in element.Attributes)
                {
                    if (attr is Handler handler)
                    {
                        Unregister(handler.CommandId);
                    }
                }

                foreach (var child in element.Children)
                {
                    UnregisterHandlers(child);
                }

                break;

            case MemoNode memo:
                UnregisterHandlers(memo.CachedNode);
                break;

            case LazyMemoNode lazy:
                UnregisterHandlers(lazy.CachedNode);
                break;
        }
    }
}
