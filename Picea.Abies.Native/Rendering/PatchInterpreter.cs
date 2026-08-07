// =============================================================================
// Patch Interpreter
// =============================================================================
// Interprets the Abies patch stream against an INativeBackend. This is the
// native counterpart of abies.js's applyPatch: it owns the elementId →
// control map, the (elementId, eventName) → Handler map, and child → parent
// tracking (needed because ReplaceChild patches carry no parent).
//
// Threading: Apply must be called on the backend's UI thread. The hosting
// adapter (e.g., Picea.Abies.WinUI) is responsible for marshaling — the
// Abies runtime invokes its Apply delegate on whatever thread dispatched the
// message.
//
// Native trees contain only Element nodes (see Elements.cs). The Text and
// RawHtml patch families therefore throw here as a tripwire: if one fires,
// a view produced a Text/RawHtml child and the diff took an HTML fallback.
// =============================================================================

using Picea.Abies.DOM;

namespace Picea.Abies.Native.Rendering;

/// <summary>
/// Applies Abies patches to native controls via an <see cref="INativeBackend{T}"/>.
/// </summary>
/// <typeparam name="T">The framework's control base type.</typeparam>
public sealed class PatchInterpreter<T> : INativeEventSink where T : class
{
    private readonly INativeBackend<T> _backend;
    private readonly Dictionary<string, T> _controls = [];
    private readonly Dictionary<string, string> _tags = [];
    private readonly Dictionary<string, string> _parents = [];
    private readonly Dictionary<(string ElementId, string EventName), Handler> _handlers = [];

    public PatchInterpreter(INativeBackend<T> backend) => _backend = backend;

    /// <summary>
    /// Dispatches messages produced by native events into the Abies runtime.
    /// Set once during bootstrap, before the first event can fire.
    /// </summary>
    public Func<Message, ValueTask>? Dispatch { get; set; }

    /// <summary>
    /// Receives faults that would otherwise be lost. Dispatch is
    /// fire-and-forget — a native event hands a message to the runtime and
    /// returns, matching the browser renderer — so without this hook a failing
    /// update or command interpreter fails silently and the UI simply stops
    /// responding. Set during bootstrap, before the first event can fire.
    /// </summary>
    public Action<Exception>? Faulted { get; set; }

    /// <inheritdoc />
    public bool IsApplying { get; private set; }

    /// <summary>Number of live tracked controls (diagnostics/tests).</summary>
    public int ControlCount => _controls.Count;

    /// <summary>
    /// Applies a patch batch. Must run on the backend's UI thread.
    /// </summary>
    public void Apply(IReadOnlyList<Patch> patches)
    {
        IsApplying = true;
        try
        {
            foreach (var patch in patches)
                ApplyPatch(patch);
        }
        finally
        {
            IsApplying = false;
        }
    }

    /// <inheritdoc />
    public void OnNativeEvent(string elementId, string eventName, object? data)
    {
        if (!_handlers.TryGetValue((elementId, eventName), out var handler))
            return;

        var message = handler.Command ?? handler.WithData?.Invoke(data);
        if (message is null)
            return;

        var dispatch = Dispatch
            ?? throw new InvalidOperationException("PatchInterpreter.Dispatch is not wired to a runtime.");

        ValueTask pending;
        try
        {
            pending = dispatch(message);
        }
        catch (Exception ex)
        {
            // Dispatch threw before returning a task (synchronous failure).
            Report(ex);
            return;
        }

        if (pending.IsCompletedSuccessfully)
            return;

        Observe(pending);
    }

    /// <summary>
    /// Awaits a dispatch that did not complete synchronously, so its failure
    /// reaches <see cref="Faulted"/> rather than becoming an unobserved
    /// exception. Never throws: the whole body is guarded.
    /// </summary>
    private async void Observe(ValueTask pending)
    {
        try
        {
            await pending;
        }
        catch (Exception ex)
        {
            Report(ex);
        }
    }

    /// <summary>Reports a fault, never letting the reporter itself throw.</summary>
    private void Report(Exception exception)
    {
        var faulted = Faulted;
        if (faulted is null)
            return;
        try
        {
            faulted(exception);
        }
        catch
        {
            // A throwing fault handler must not escalate into the caller —
            // for Observe that would be an unhandled exception on the UI thread.
        }
    }

    private void ApplyPatch(Patch patch)
    {
        switch (patch)
        {
            case AddRoot p:
                Reset();
                _backend.SetRoot(BuildSubtree(p.Element, parentId: null));
                break;

            case ReplaceChild p:
            {
                var oldControl = Control(p.OldElement.Id);
                var parentId = _parents[p.OldElement.Id];
                var parentControl = Control(parentId);
                var parentTag = _tags[parentId];
                // Unregister BEFORE building: old and new subtrees may share
                // element ids (same view call sites), and building first would
                // let the unregistration wipe the fresh registrations.
                UnregisterSubtree(p.OldElement);
                var newControl = BuildSubtree(p.NewElement, parentId);
                _backend.ReplaceChild(parentControl, parentTag, oldControl, newControl);
                break;
            }

            case AddChild p:
                _backend.AppendChild(Control(p.Parent.Id), p.Parent.Tag,
                    BuildSubtree(p.Child, p.Parent.Id));
                break;

            case RemoveChild p:
                _backend.RemoveChild(Control(p.Parent.Id), p.Parent.Tag, Control(p.Child.Id));
                UnregisterSubtree(p.Child);
                break;

            case ClearChildren p:
                _backend.ClearChildren(Control(p.Parent.Id), p.Parent.Tag);
                foreach (var child in p.OldChildren)
                    UnregisterSubtree(child);
                break;

            case SetChildrenHtml p:
            {
                // 0→N children fast path; clear defensively in case the diff
                // ever uses it as a full replacement.
                var parentControl = Control(p.Parent.Id);
                _backend.ClearChildren(parentControl, p.Parent.Tag);
                UnregisterChildrenOf(p.Parent.Id);
                AppendMaterialized(parentControl, p.Parent, p.Children);
                break;
            }

            case AppendChildrenHtml p:
                AppendMaterialized(Control(p.Parent.Id), p.Parent, p.Children);
                break;

            case MoveChild p:
                _backend.MoveChild(Control(p.Parent.Id), p.Parent.Tag, Control(p.Child.Id),
                    p.BeforeId is null ? null : Control(p.BeforeId));
                break;

            case UpdateAttribute p:
                SetProperty(p.Element, p.Attribute.Name, p.Value);
                break;

            case AddAttribute p:
                if (p.Attribute is Handler addedHandler)
                    AddHandlerToLiveControl(p.Element, addedHandler);
                else
                    SetProperty(p.Element, p.Attribute.Name, p.Attribute.Value);
                break;

            case RemoveAttribute p:
                if (p.Attribute is Handler removedHandler)
                    RemoveHandlerFromLiveControl(p.Element, removedHandler.EventName);
                else
                    SetProperty(p.Element, p.Attribute.Name, null);
                break;

            case AddHandler p:
                AddHandlerToLiveControl(p.Element, p.Handler);
                break;

            case RemoveHandler p:
                RemoveHandlerFromLiveControl(p.Element, p.Handler.EventName);
                break;

            case UpdateHandler p:
                // Same CommandId, fresh closure: swap the stored handler only —
                // the native event subscription stays in place.
                _handlers[(p.Element.Id, p.NewHandler.EventName)] = p.NewHandler;
                break;

            case UpdateText or AddText or RemoveText or AddRaw or RemoveRaw or ReplaceRaw or UpdateRaw:
                throw new NotSupportedException(
                    $"Patch {patch.GetType().Name} is not supported in native trees: " +
                    "Text and RawHtml nodes must not appear in native views. " +
                    "Use TextBlock(...) or a Content-bearing control instead.");

            case AddHeadElement or UpdateHeadElement or RemoveHeadElement:
                break; // no document head natively

            default:
                throw new NotSupportedException($"Unknown patch type: {patch.GetType().Name}");
        }
    }

    // =========================================================================
    // Tree construction
    // =========================================================================

    private T BuildSubtree(Element element, string? parentId)
    {
        var control = _backend.CreateControl(element.Tag, element.Id);
        _controls[element.Id] = control;
        _tags[element.Id] = element.Tag;
        if (parentId is not null)
            _parents[element.Id] = parentId;
        else
            _parents.Remove(element.Id);

        foreach (var attribute in element.Attributes)
        {
            if (attribute is Handler handler)
            {
                // Fresh control: always attach, even if a same-id entry
                // lingers from a subtree that is about to be unregistered.
                _handlers[(element.Id, handler.EventName)] = handler;
                _backend.AttachEvent(control, element.Tag, handler.EventName, element.Id, this);
            }
            else if (!IsVirtualAttribute(attribute.Name))
            {
                _backend.SetProperty(control, element.Tag, attribute.Name, attribute.Value);
            }
        }

        // Resolve unwraps memo nodes and throws on Text/RawHtml; OfType then
        // skips Empty, which is the only other kind a native tree may contain.
        foreach (var childElement in element.Children.Select(Resolve).OfType<Element>())
            _backend.AppendChild(control, element.Tag, BuildSubtree(childElement, element.Id));

        return control;
    }

    private void AppendMaterialized(T parentControl, Element parent, Node[] children)
    {
        foreach (var childElement in children.Select(Resolve).OfType<Element>())
            _backend.AppendChild(parentControl, parent.Tag, BuildSubtree(childElement, parent.Id));
    }

    /// <summary>
    /// Unwraps Memo/LazyMemo wrappers and rejects node kinds that native
    /// trees must not contain. Empty resolves to itself and is skipped by
    /// callers; Text/RawHtml throw the same tripwire as their patches.
    /// </summary>
    private static Node Resolve(Node node) => node switch
    {
        MemoNode memo => Resolve(memo.CachedNode),
        LazyMemoNode lazy => Resolve(lazy.CachedNode ?? lazy.Evaluate()),
        Text or RawHtml => throw new NotSupportedException(
            "Text and RawHtml nodes are not supported in native trees. " +
            "Use TextBlock(...) or a Content-bearing control instead."),
        _ => node,
    };

    // =========================================================================
    // Bookkeeping
    // =========================================================================

    private T Control(string elementId) =>
        _controls.TryGetValue(elementId, out var control)
            ? control
            : throw new InvalidOperationException($"No native control tracked for element id '{elementId}'.");

    private void SetProperty(Element element, string name, string? value)
    {
        if (IsVirtualAttribute(name))
            return;
        _backend.SetProperty(Control(element.Id), element.Tag, name, value);
    }

    private static bool IsVirtualAttribute(string name) =>
        name is "key" or "data-key" or "id";

    /// <summary>A live control gains a handler for an event it had none for.</summary>
    private void AddHandlerToLiveControl(Element element, Handler handler)
    {
        var key = (element.Id, handler.EventName);
        var alreadyAttached = _handlers.ContainsKey(key);
        _handlers[key] = handler;
        if (!alreadyAttached)
            _backend.AttachEvent(Control(element.Id), element.Tag, handler.EventName, element.Id, this);
    }

    private void RemoveHandlerFromLiveControl(Element element, string eventName)
    {
        if (_handlers.Remove((element.Id, eventName)))
            _backend.DetachEvent(Control(element.Id), element.Tag, eventName);
    }

    /// <summary>Drops all tracking for a removed subtree.</summary>
    private void UnregisterSubtree(Node node)
    {
        if (Resolve(node) is not Element element)
            return;

        foreach (var child in element.Children)
            UnregisterSubtree(child);

        DropControl(element.Id);
    }

    private void UnregisterChildrenOf(string parentId)
    {
        // Materialized before the loop: DropControl mutates _parents.
        var doomed = _parents
            .Where(entry => entry.Value == parentId)
            .Select(entry => entry.Key)
            .ToList();

        foreach (var childId in doomed)
        {
            UnregisterChildrenOf(childId);
            DropControl(childId);
        }
    }

    /// <summary>
    /// Stops tracking a control, detaching its native event subscriptions
    /// first. Every path that drops a control must go through here: a backend
    /// keyed on the control instance (as the WinUI one is) otherwise keeps the
    /// dead control and its handler closures alive for the process lifetime.
    /// </summary>
    private void DropControl(string elementId)
    {
        DetachHandlersFor(elementId);
        _controls.Remove(elementId);
        _tags.Remove(elementId);
        _parents.Remove(elementId);
    }

    /// <summary>
    /// Detaches and forgets every handler registered for an element. Reads the
    /// live handler map rather than the node's attributes, so handlers added by
    /// later AddHandler patches are detached too.
    /// </summary>
    private void DetachHandlersFor(string elementId)
    {
        // Materialized before the loop: the loop mutates _handlers.
        var doomed = _handlers.Keys
            .Where(key => key.ElementId == elementId)
            .Select(key => key.EventName)
            .ToList();
        if (doomed.Count == 0)
            return;

        _controls.TryGetValue(elementId, out var control);
        var tag = _tags.GetValueOrDefault(elementId, string.Empty);
        foreach (var eventName in doomed)
        {
            _handlers.Remove((elementId, eventName));
            if (control is not null)
                _backend.DetachEvent(control, tag, eventName);
        }
    }

    private void Reset()
    {
        foreach (var elementId in _handlers.Keys.Select(key => key.ElementId).Distinct().ToArray())
            DetachHandlersFor(elementId);

        _controls.Clear();
        _tags.Clear();
        _parents.Clear();
        _handlers.Clear();
    }
}
