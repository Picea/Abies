// =============================================================================
// HTML Event Handlers
// =============================================================================
// Provides functions for creating DOM event handler attributes. Each function
// returns a Handler that renders as a data-event-{name} attribute in the DOM.
//
// Every event has two overloads:
//   1. Static message  — onclick(new Increment())
//   2. Data-carrying   — oninput(e => new TextChanged(e.Value))
//
// Uses Praefixum source generator for compile-time unique IDs, ensuring
// stable event handler identification for efficient diffing.
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM
// - ADR-014: Compile-Time Unique IDs
// - ADR-002: Pure Functional Programming
// =============================================================================

using Picea.Abies.DOM;
using Praefixum;

namespace Picea.Abies.Html;

/// <summary>
/// Provides factory functions for creating DOM event handler attributes.
/// </summary>
/// <remarks>
/// <para>
/// Event handlers are modeled as <see cref="Handler"/> attributes. They render
/// as <c>data-event-{name}="{commandId}"</c> in the DOM. The runtime uses
/// event delegation at the document level to dispatch messages.
/// </para>
/// <para>
/// Command IDs are generated via a thread-safe monotonic counter using
/// <see cref="Interlocked.Increment(ref long)"/>. The IDs are formatted
/// as "h{n}" using stack-allocated buffers for zero-allocation string creation.
/// </para>
/// </remarks>
public static class Events
{
    // =========================================================================
    // Command ID Generation
    // =========================================================================

    private static long _commandIdCounter;

    /// <summary>
    /// Generates the next unique command ID for event handler registration.
    /// Thread-safe via <see cref="Interlocked.Increment(ref long)"/>.
    /// Uses stack-allocated buffer for zero-allocation formatting.
    /// </summary>
    private static string NextCommandId()
    {
        var id = Interlocked.Increment(ref _commandIdCounter);
        return string.Create(null, stackalloc char[24], $"h{id}");
    }

    // =========================================================================
    // Core Event Handler Factories
    // =========================================================================

    /// <summary>
    /// Creates an event handler that dispatches a static message.
    /// </summary>
    /// <param name="name">The DOM event name (e.g., "click", "input").</param>
    /// <param name="command">The message to dispatch when the event fires.</param>
    /// <param name="id">Compile-time unique identifier for this handler.</param>
    public static Handler on(string name, Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    {
        return new(name, NextCommandId(), command, id ?? string.Empty);
    }

    /// <summary>
    /// Creates an event handler that produces a message from deserialized event data.
    /// Uses source-generated JSON deserialization for trim-safe WASM support.
    /// </summary>
    /// <typeparam name="T">The event data type (e.g., <see cref="InputEventData"/>, <see cref="KeyEventData"/>).</typeparam>
    /// <param name="name">The DOM event name (e.g., "click", "input").</param>
    /// <param name="factory">A function that creates a message from event data.</param>
    /// <param name="id">Compile-time unique identifier for this handler.</param>
    public static Handler on<T>(string name, Func<T?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    {
        return new(name, NextCommandId(), null, id ?? string.Empty,
            o => factory((T?)o),
            EventDataDeserializers.Get<T>());
    }

    // =========================================================================
    // Mouse Events
    // =========================================================================

    public static Handler onclick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("click", command, id);

    public static Handler onclick(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("click", factory, id);

    public static Handler ondblclick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dblclick", command, id);

    public static Handler ondblclick(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dblclick", factory, id);

    public static Handler oncontextmenu(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("contextmenu", command, id);

    public static Handler oncontextmenu(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("contextmenu", factory, id);

    public static Handler onmousedown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousedown", command, id);

    public static Handler onmousedown(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousedown", factory, id);

    public static Handler onmouseup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseup", command, id);

    public static Handler onmouseup(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseup", factory, id);

    public static Handler onmousemove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousemove", command, id);

    public static Handler onmousemove(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousemove", factory, id);

    public static Handler onmouseenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseenter", command, id);

    public static Handler onmouseenter(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseenter", factory, id);

    public static Handler onmouseleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseleave", command, id);

    public static Handler onmouseleave(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseleave", factory, id);

    public static Handler onmouseover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseover", command, id);

    public static Handler onmouseover(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseover", factory, id);

    public static Handler onmouseout(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseout", command, id);

    public static Handler onmouseout(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseout", factory, id);

    public static Handler onwheel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("wheel", command, id);

    public static Handler onwheel(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("wheel", factory, id);

    // =========================================================================
    // Keyboard Events
    // =========================================================================

    public static Handler onkeydown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keydown", command, id);

    public static Handler onkeydown(Func<KeyEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keydown", factory, id);

    public static Handler onkeyup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keyup", command, id);

    public static Handler onkeyup(Func<KeyEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keyup", factory, id);

    public static Handler onkeypress(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keypress", command, id);

    public static Handler onkeypress(Func<KeyEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keypress", factory, id);

    // =========================================================================
    // Input / Form Events
    // =========================================================================

    public static Handler oninput(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("input", command, id);

    public static Handler oninput(Func<InputEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("input", factory, id);

    public static Handler onchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("change", command, id);

    public static Handler onchange(Func<InputEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("change", factory, id);

    public static Handler onsubmit(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("submit", command, id);

    public static Handler onsubmit(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("submit", factory, id);

    public static Handler onreset(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("reset", command, id);

    public static Handler onreset(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("reset", factory, id);

    public static Handler oninvalid(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("invalid", command, id);

    public static Handler oninvalid(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("invalid", factory, id);

    public static Handler onselect(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("select", command, id);

    public static Handler onselect(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("select", factory, id);

    // =========================================================================
    // Focus Events
    // =========================================================================

    public static Handler onfocus(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focus", command, id);

    public static Handler onfocus(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focus", factory, id);

    public static Handler onblur(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("blur", command, id);

    public static Handler onblur(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("blur", factory, id);

    public static Handler onfocusin(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focusin", command, id);

    public static Handler onfocusin(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focusin", factory, id);

    public static Handler onfocusout(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focusout", command, id);

    public static Handler onfocusout(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focusout", factory, id);

    // =========================================================================
    // Pointer Events
    // =========================================================================

    public static Handler onpointerdown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerdown", command, id);

    public static Handler onpointerdown(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerdown", factory, id);

    public static Handler onpointerup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerup", command, id);

    public static Handler onpointerup(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerup", factory, id);

    public static Handler onpointermove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointermove", command, id);

    public static Handler onpointermove(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointermove", factory, id);

    public static Handler onpointercancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointercancel", command, id);

    public static Handler onpointercancel(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointercancel", factory, id);

    public static Handler onpointerover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerover", command, id);

    public static Handler onpointerover(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerover", factory, id);

    public static Handler onpointerout(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerout", command, id);

    public static Handler onpointerout(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerout", factory, id);

    public static Handler onpointerenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerenter", command, id);

    public static Handler onpointerenter(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerenter", factory, id);

    public static Handler onpointerleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerleave", command, id);

    public static Handler onpointerleave(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerleave", factory, id);

    public static Handler ongotpointercapture(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gotpointercapture", command, id);

    public static Handler ongotpointercapture(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gotpointercapture", factory, id);

    public static Handler onlostpointercapture(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("lostpointercapture", command, id);

    public static Handler onlostpointercapture(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("lostpointercapture", factory, id);

    // =========================================================================
    // Touch Events
    // =========================================================================

    public static Handler ontouchstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchstart", command, id);

    public static Handler ontouchstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchstart", factory, id);

    public static Handler ontouchend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchend", command, id);

    public static Handler ontouchend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchend", factory, id);

    public static Handler ontouchmove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchmove", command, id);

    public static Handler ontouchmove(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchmove", factory, id);

    public static Handler ontouchcancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchcancel", command, id);

    public static Handler ontouchcancel(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchcancel", factory, id);

    // =========================================================================
    // Drag Events
    // =========================================================================

    public static Handler ondrag(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drag", command, id);

    public static Handler ondrag(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drag", factory, id);

    public static Handler ondragstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragstart", command, id);

    public static Handler ondragstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragstart", factory, id);

    public static Handler ondragend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragend", command, id);

    public static Handler ondragend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragend", factory, id);

    public static Handler ondragenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragenter", command, id);

    public static Handler ondragenter(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragenter", factory, id);

    public static Handler ondragleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragleave", command, id);

    public static Handler ondragleave(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragleave", factory, id);

    public static Handler ondragover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragover", command, id);

    public static Handler ondragover(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragover", factory, id);

    public static Handler ondrop(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drop", command, id);

    public static Handler ondrop(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drop", factory, id);

    // =========================================================================
    // Scroll & Resize Events
    // =========================================================================

    public static Handler onscroll(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("scroll", command, id);

    public static Handler onscroll(Func<ScrollEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("scroll", factory, id);

    public static Handler onresize(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("resize", command, id);

    public static Handler onresize(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("resize", factory, id);

    // =========================================================================
    // Clipboard Events
    // =========================================================================

    public static Handler oncopy(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("copy", command, id);

    public static Handler oncopy(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("copy", factory, id);

    public static Handler oncut(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cut", command, id);

    public static Handler oncut(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cut", factory, id);

    public static Handler onpaste(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("paste", command, id);

    public static Handler onpaste(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("paste", factory, id);

    // =========================================================================
    // Animation Events
    // =========================================================================

    public static Handler onanimationstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationstart", command, id);

    public static Handler onanimationstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationstart", factory, id);

    public static Handler onanimationend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationend", command, id);

    public static Handler onanimationend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationend", factory, id);

    public static Handler onanimationiteration(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationiteration", command, id);

    public static Handler onanimationiteration(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationiteration", factory, id);

    public static Handler onanimationcancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationcancel", command, id);

    public static Handler onanimationcancel(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationcancel", factory, id);

    // =========================================================================
    // Transition Events
    // =========================================================================

    public static Handler ontransitionstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionstart", command, id);

    public static Handler ontransitionstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionstart", factory, id);

    public static Handler ontransitionend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionend", command, id);

    public static Handler ontransitionend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionend", factory, id);

    public static Handler ontransitionrun(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionrun", command, id);

    public static Handler ontransitionrun(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionrun", factory, id);

    public static Handler ontransitioncancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitioncancel", command, id);

    public static Handler ontransitioncancel(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitioncancel", factory, id);

    // =========================================================================
    // Media Events
    // =========================================================================

    public static Handler onplay(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("play", command, id);

    public static Handler onplay(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("play", factory, id);

    public static Handler onpause(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pause", command, id);

    public static Handler onpause(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pause", factory, id);

    public static Handler onended(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ended", command, id);

    public static Handler onended(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ended", factory, id);

    public static Handler ontimeupdate(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("timeupdate", command, id);

    public static Handler ontimeupdate(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("timeupdate", factory, id);

    public static Handler onvolumechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("volumechange", command, id);

    public static Handler onvolumechange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("volumechange", factory, id);

    public static Handler onseeking(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeking", command, id);

    public static Handler onseeking(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeking", factory, id);

    public static Handler onseeked(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeked", command, id);

    public static Handler onseeked(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeked", factory, id);

    public static Handler onratechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ratechange", command, id);

    public static Handler onratechange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ratechange", factory, id);

    public static Handler ondurationchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("durationchange", command, id);

    public static Handler ondurationchange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("durationchange", factory, id);

    public static Handler oncanplay(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplay", command, id);

    public static Handler oncanplay(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplay", factory, id);

    public static Handler oncanplaythrough(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplaythrough", command, id);

    public static Handler oncanplaythrough(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplaythrough", factory, id);

    public static Handler onwaiting(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("waiting", command, id);

    public static Handler onwaiting(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("waiting", factory, id);

    public static Handler onplaying(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("playing", command, id);

    public static Handler onplaying(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("playing", factory, id);

    public static Handler onstalled(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("stalled", command, id);

    public static Handler onstalled(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("stalled", factory, id);

    public static Handler onsuspend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("suspend", command, id);

    public static Handler onsuspend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("suspend", factory, id);

    public static Handler onemptied(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("emptied", command, id);

    public static Handler onemptied(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("emptied", factory, id);

    public static Handler onloadeddata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadeddata", command, id);

    public static Handler onloadeddata(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadeddata", factory, id);

    public static Handler onloadedmetadata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadedmetadata", command, id);

    public static Handler onloadedmetadata(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadedmetadata", factory, id);

    public static Handler onloadstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadstart", command, id);

    public static Handler onloadstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadstart", factory, id);

    public static Handler onprogress(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("progress", command, id);

    public static Handler onprogress(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("progress", factory, id);

    public static Handler onabort(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("abort", command, id);

    public static Handler onabort(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("abort", factory, id);

    // =========================================================================
    // Load & Error Events
    // =========================================================================

    public static Handler onload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("load", command, id);

    public static Handler onload(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("load", factory, id);

    public static Handler onerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("error", command, id);

    public static Handler onerror(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("error", factory, id);

    public static Handler onunload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unload", command, id);

    public static Handler onunload(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unload", factory, id);

    public static Handler onbeforeunload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeunload", command, id);

    public static Handler onbeforeunload(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeunload", factory, id);

    // =========================================================================
    // Toggle & Misc Events
    // =========================================================================

    public static Handler ontoggle(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("toggle", command, id);

    public static Handler ontoggle(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("toggle", factory, id);

    public static Handler onclose(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("close", command, id);

    public static Handler onclose(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("close", factory, id);

    public static Handler oncancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cancel", command, id);

    public static Handler oncancel(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cancel", factory, id);

    public static Handler onfullscreenchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenchange", command, id);

    public static Handler onfullscreenchange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenchange", factory, id);

    public static Handler onfullscreenerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenerror", command, id);

    public static Handler onfullscreenerror(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenerror", factory, id);

    // =========================================================================
    // Composition Events (for IME / CJK input)
    // =========================================================================

    public static Handler oncompositionstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("compositionstart", command, id);

    public static Handler oncompositionstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("compositionstart", factory, id);

    public static Handler oncompositionupdate(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("compositionupdate", command, id);

    public static Handler oncompositionupdate(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("compositionupdate", factory, id);

    public static Handler oncompositionend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("compositionend", command, id);

    public static Handler oncompositionend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("compositionend", factory, id);
}
