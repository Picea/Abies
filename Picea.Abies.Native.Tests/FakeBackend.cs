// =============================================================================
// Fake Backend — Headless Recording Implementation of INativeBackend
// =============================================================================
// A control is a plain object with a property bag and child list. The backend
// records every operation so tests can assert both the final tree shape and
// the operations that produced it (e.g., "reorder moved controls instead of
// recreating them").
// =============================================================================

using Picea.Abies.Native.Rendering;

namespace Picea.Abies.Native.Tests;

public sealed class FakeControl(string tag, string id)
{
    public string Tag { get; } = tag;
    public string Id { get; } = id;
    public Dictionary<string, string?> Props { get; } = [];
    public List<FakeControl> Children { get; } = [];
    public HashSet<string> AttachedEvents { get; } = [];
}

public sealed class FakeBackend : INativeBackend<FakeControl>
{
    public FakeControl? Root { get; private set; }
    public List<FakeControl> AllCreated { get; } = [];
    public List<string> Log { get; } = [];

    public FakeControl CreateControl(string tag, string elementId)
    {
        var control = new FakeControl(tag, elementId);
        AllCreated.Add(control);
        Log.Add($"create {tag}#{elementId}");
        return control;
    }

    public void SetProperty(FakeControl control, string tag, string name, string? value)
    {
        if (value is null)
            control.Props.Remove(name);
        else
            control.Props[name] = value;
        Log.Add($"set {tag}#{control.Id}.{name}={value ?? "<null>"}");
    }

    public void AppendChild(FakeControl parent, string parentTag, FakeControl child)
    {
        parent.Children.Add(child);
        Log.Add($"append {child.Id} -> {parent.Id}");
    }

    public void ReplaceChild(FakeControl parent, string parentTag, FakeControl oldChild, FakeControl newChild)
    {
        parent.Children[parent.Children.IndexOf(oldChild)] = newChild;
        Log.Add($"replace {oldChild.Id} with {newChild.Id} in {parent.Id}");
    }

    public void RemoveChild(FakeControl parent, string parentTag, FakeControl child)
    {
        parent.Children.Remove(child);
        Log.Add($"remove {child.Id} from {parent.Id}");
    }

    public void MoveChild(FakeControl parent, string parentTag, FakeControl child, FakeControl? before)
    {
        parent.Children.Remove(child);
        if (before is null)
            parent.Children.Add(child);
        else
            parent.Children.Insert(parent.Children.IndexOf(before), child);
        Log.Add($"move {child.Id} before {before?.Id ?? "<end>"} in {parent.Id}");
    }

    public void ClearChildren(FakeControl parent, string parentTag)
    {
        parent.Children.Clear();
        Log.Add($"clear {parent.Id}");
    }

    public void SetRoot(FakeControl root)
    {
        Root = root;
        Log.Add($"root {root.Id}");
    }

    public void AttachEvent(FakeControl control, string tag, string eventName, string elementId, INativeEventSink sink)
    {
        control.AttachedEvents.Add(eventName);
        Log.Add($"attach {eventName} on {elementId}");
    }

    public void DetachEvent(FakeControl control, string tag, string eventName)
    {
        control.AttachedEvents.Remove(eventName);
        Log.Add($"detach {eventName} on {control.Id}");
    }
}
