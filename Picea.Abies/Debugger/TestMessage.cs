#if DEBUG

namespace Picea.Abies.Debugger;

/// <summary>
/// Simple concrete message type for testing and demonstration purposes.
/// Tests can create instances like: new TestMessage { Type = "MyEvent", Args = new object[] { ... } }
/// </summary>
public sealed record TestMessage : Message
{
    public string Type { get; set; } = string.Empty;
    public object?[]? Args { get; set; }
}

#endif
