// =============================================================================
// Native Event Handlers
// =============================================================================
// Factory functions for native control event handlers. Each returns a
// Handler carrying the native event name (e.g., "Click", "TextChanged") and
// either a static message or a WithData factory over a native event-data
// record from EventData.cs.
//
// Unlike the HTML DSL, no JSON deserializer is registered: the native
// renderer constructs the event-data record directly from real event args
// and invokes Handler.WithData itself (it never goes through
// HandlerRegistry.CreateMessage).
//
// Command IDs use the "nh{n}" prefix to stay disjoint from the HTML DSL's
// "h{n}" ids when both vocabularies are loaded in one process.
// =============================================================================

using Picea.Abies.DOM;
using Praefixum;

namespace Picea.Abies.Native;

/// <summary>
/// Factory functions for native control event handler attributes.
/// </summary>
public static class Events
{
    private static long _commandIdCounter;

    private static string NextCommandId()
    {
        var id = Interlocked.Increment(ref _commandIdCounter);
        return string.Create(null, stackalloc char[24], $"nh{id}");
    }

    /// <summary>
    /// Creates a handler that dispatches a static message when the named
    /// native event fires.
    /// </summary>
    /// <param name="name">The native event name (e.g., "Click").</param>
    /// <param name="command">The message to dispatch.</param>
    /// <param name="id">Compile-time unique identifier for this handler.</param>
    public static Handler On(string name, Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(name, NextCommandId(), command, id ?? string.Empty);

    /// <summary>
    /// Creates a handler that produces a message from native event data.
    /// </summary>
    /// <typeparam name="T">The event data record type (e.g., <see cref="TextChangedData"/>).</typeparam>
    /// <param name="name">The native event name (e.g., "TextChanged").</param>
    /// <param name="factory">Creates a message from the event data.</param>
    /// <param name="id">Compile-time unique identifier for this handler.</param>
    public static Handler On<T>(string name, Func<T?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(name, NextCommandId(), null, id ?? string.Empty, o => factory((T?)o));

    /// <summary>Button (and other ButtonBase) click.</summary>
    public static Handler OnClick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => On("Click", command, id);

    /// <summary>TextBox text change, carrying the current text.</summary>
    public static Handler OnTextChanged(Func<TextChangedData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => On("TextChanged", factory, id);

    /// <summary>CheckBox checked/unchecked, unified into one toggle event.</summary>
    public static Handler OnToggled(Func<ToggledData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => On("Toggled", factory, id);

    /// <summary>Slider value change.</summary>
    public static Handler OnValueChanged(Func<ValueChangedData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => On("ValueChanged", factory, id);

    /// <summary>ComboBox selection change.</summary>
    public static Handler OnSelectionChanged(Func<SelectionChangedData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => On("SelectionChanged", factory, id);

    /// <summary>
    /// ScrollViewer position change. Pair with a model that slices its data to
    /// the visible range — see the list windowing guidance on Elements.ScrollViewer.
    /// </summary>
    public static Handler OnScrollChanged(Func<ScrollChangedData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => On("ScrollChanged", factory, id);
}
