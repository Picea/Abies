// =============================================================================
// Native Backend Abstraction
// =============================================================================
// The seam between the platform-neutral patch interpreter and a concrete UI
// framework. Picea.Abies.WinUI implements this over WinUI/Uno
// FrameworkElements; tests implement it with a fake recording backend.
//
// Child operations use DOM-like semantics (append / insert-before / move)
// because that is what the Abies patch stream expresses; backends translate
// to their framework's child collection model.
// =============================================================================

namespace Picea.Abies.Native.Rendering;

/// <summary>
/// Receives native control events from a backend and routes them to the
/// registered Abies handler. Implemented by <see cref="PatchInterpreter{T}"/>.
/// </summary>
public interface INativeEventSink
{
    /// <summary>
    /// Dispatches a native event to the handler registered for
    /// (<paramref name="elementId"/>, <paramref name="eventName"/>).
    /// </summary>
    /// <param name="elementId">The virtual DOM id of the source element.</param>
    /// <param name="eventName">The native event name (e.g., "Click").</param>
    /// <param name="data">Native event data record (e.g., <see cref="TextChangedData"/>), or null for static-message handlers.</param>
    void OnNativeEvent(string elementId, string eventName, object? data);

    /// <summary>
    /// True while a patch batch is being applied. Backends use this to
    /// suppress event echo when a patch writes a property that raises the
    /// same event a user edit would (e.g., TextBox.TextChanged).
    /// </summary>
    bool IsApplying { get; }
}

/// <summary>
/// A concrete UI framework binding for the patch interpreter.
/// </summary>
/// <typeparam name="T">The framework's control base type (e.g., FrameworkElement).</typeparam>
public interface INativeBackend<T> where T : class
{
    /// <summary>Creates a control instance for the given vocabulary tag.</summary>
    T CreateControl(string tag, string elementId);

    /// <summary>
    /// Sets a property from its string-encoded attribute value;
    /// <paramref name="value"/> null means reset to the default.
    /// </summary>
    void SetProperty(T control, string tag, string name, string? value);

    /// <summary>
    /// Appends a child to the parent's child collection (or content slot).
    /// Insertions always append: the diff restores ordering with a following
    /// <see cref="MoveChild"/>, so no positional-insert operation is needed.
    /// </summary>
    void AppendChild(T parent, string parentTag, T child);

    /// <summary>Replaces an existing child in place.</summary>
    void ReplaceChild(T parent, string parentTag, T oldChild, T newChild);

    /// <summary>Removes a child.</summary>
    void RemoveChild(T parent, string parentTag, T child);

    /// <summary>
    /// Moves an existing child before <paramref name="before"/>, or to the
    /// end when <paramref name="before"/> is null (insertBefore semantics).
    /// </summary>
    void MoveChild(T parent, string parentTag, T child, T? before);

    /// <summary>Removes all children.</summary>
    void ClearChildren(T parent, string parentTag);

    /// <summary>Installs the root control into the hosting surface.</summary>
    void SetRoot(T root);

    /// <summary>
    /// Subscribes the control's native event named <paramref name="eventName"/>
    /// and forwards occurrences to <paramref name="sink"/>. Implementations
    /// must keep the subscription so <see cref="DetachEvent"/> can remove it,
    /// and should honor <see cref="INativeEventSink.IsApplying"/> for events
    /// that echo property writes.
    /// </summary>
    void AttachEvent(T control, string tag, string eventName, string elementId, INativeEventSink sink);

    /// <summary>Removes a subscription installed by <see cref="AttachEvent"/>.</summary>
    void DetachEvent(T control, string tag, string eventName);
}
