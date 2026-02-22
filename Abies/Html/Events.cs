// =============================================================================
// HTML Events
// =============================================================================
// Provides functions for attaching event handlers to virtual DOM elements.
// Event handlers return Handler records which are special Attributes that
// map DOM events to MVU Messages.
//
// Uses Praefixum source generator for compile-time unique IDs.
//
// Performance optimization: Uses atomic counter instead of Guid.NewGuid().ToString()
// for CommandId generation, inspired by Stephen Toub's .NET performance articles.
// This reduces allocation from ~200 bytes to ~8-16 bytes per handler.
//
// Architecture Decision Records:
// - ADR-001: MVU Architecture (docs/adr/ADR-001-mvu-architecture.md)
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// - ADR-006: Command Pattern for Side Effects (docs/adr/ADR-006-command-pattern.md)
// - ADR-014: Compile-Time Unique IDs (docs/adr/ADR-014-compile-time-ids.md)
// =============================================================================

using System.Runtime.CompilerServices;
using Abies.DOM;
using Praefixum;

namespace Abies.Html;

/// <summary>
/// Provides factory functions for creating event handlers.
/// </summary>
/// <remarks>
/// Event handlers connect DOM events to MVU Messages.
/// When an event fires, the corresponding Message is dispatched to the update function.
/// See ADR-001: MVU Architecture.
/// </remarks>
public static class Events
{
    // Rarely used touch/pointer/mobile events
    public static Handler ongotpointercapture(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gotpointercapture", command, id);

    public static Handler ongotpointercapture(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gotpointercapture", factory, id);

    public static Handler onlostpointercapture(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("lostpointercapture", command, id);

    public static Handler onlostpointercapture(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("lostpointercapture", factory, id);

    // Advanced media handling
    public static Handler onencrypted(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("encrypted", command, id);

    public static Handler onencrypted(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("encrypted", factory, id);

    public static Handler onwaiting(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("waiting", command, id);

    public static Handler onwaiting(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("waiting", factory, id);

    // The newest bleeding-edge events (HTML Living Standard)
    public static Handler onformdata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("formdata", command, id);

    public static Handler onformdata(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("formdata", factory, id);

    public static Handler onbeforexrselect(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforexrselect", command, id);

    public static Handler onbeforexrselect(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforexrselect", factory, id);

    public static Handler onafterprint(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("afterprint", command, id);

    public static Handler onafterprint(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("afterprint", factory, id);

    public static Handler onbeforeprint(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeprint", command, id);

    public static Handler onbeforeprint(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeprint", factory, id);

    public static Handler onlanguagechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("languagechange", command, id);

    public static Handler onlanguagechange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("languagechange", factory, id);

    public static Handler onmessage(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("message", command, id);

    public static Handler onmessage(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("message", factory, id);

    public static Handler onmessageerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("messageerror", command, id);

    public static Handler onmessageerror(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("messageerror", factory, id);

    public static Handler onrejectionhandled(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("rejectionhandled", command, id);

    public static Handler onrejectionhandled(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("rejectionhandled", factory, id);

    public static Handler onunhandledrejection(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unhandledrejection", command, id);

    public static Handler onunhandledrejection(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unhandledrejection", factory, id);

    public static Handler onsecuritypolicyviolation(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("securitypolicyviolation", command, id);

    public static Handler onsecuritypolicyviolation(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("securitypolicyviolation", factory, id);

    // Experimental sensor/device API events
    public static Handler ondevicemotion(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("devicemotion", command, id);

    public static Handler ondevicemotion(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("devicemotion", factory, id);

    public static Handler ondeviceorientation(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("deviceorientation", command, id);

    public static Handler ondeviceorientation(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("deviceorientation", factory, id);

    public static Handler ondeviceorientationabsolute(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("deviceorientationabsolute", command, id);

    public static Handler ondeviceorientationabsolute(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("deviceorientationabsolute", factory, id);
    // Dialog events
    public static Handler onclose(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("close", command, id);

    public static Handler onclose(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("close", factory, id);

    public static Handler oncancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cancel", command, id);

    public static Handler oncancel(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cancel", factory, id);

    // Page visibility
    public static Handler onvisibilitychange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("visibilitychange", command, id);

    public static Handler onvisibilitychange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("visibilitychange", factory, id);

    // Newer events
    public static Handler onselectionchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("selectionchange", command, id);

    public static Handler onselectionchange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("selectionchange", factory, id);

    public static Handler ongesturestart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gesturestart", command, id);

    public static Handler ongesturestart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gesturestart", factory, id);

    public static Handler ongesturechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gesturechange", command, id);

    public static Handler ongesturechange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gesturechange", factory, id);

    public static Handler ongestureend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gestureend", command, id);

    public static Handler ongestureend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gestureend", factory, id);

    // Web Audio-related events
    public static Handler onaudioprocess(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("audioprocess", command, id);

    public static Handler onaudioprocess(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("audioprocess", factory, id);

    // Popover-related (new in spec)
    public static Handler onbeforetoggle(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforetoggle", command, id);

    public static Handler onbeforetoggle(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforetoggle", factory, id);

    public static Handler ontoggle(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("toggle", command, id);

    public static Handler ontoggle(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("toggle", factory, id);
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

    // Intersection Observer related
    public static Handler onintersect(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("intersect", command, id);

    public static Handler onintersect(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("intersect", factory, id);

    // Web Animation related
    public static Handler onfinish(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("finish", command, id);

    public static Handler onfinish(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("finish", factory, id);

    // Web Component related
    public static Handler onslotchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("slotchange", command, id);

    public static Handler onslotchange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("slotchange", factory, id);

    // Screen related
    public static Handler onfullscreenchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenchange", command, id);

    public static Handler onfullscreenchange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenchange", factory, id);

    public static Handler onfullscreenerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenerror", command, id);

    public static Handler onfullscreenerror(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenerror", factory, id);

    // =============================================================================
    // Command ID Generation - Performance Optimization
    // =============================================================================
    // Uses simple counter instead of Guid.NewGuid().ToString() to reduce allocations.
    // Inspired by Stephen Toub's .NET performance articles.
    //
    // GUID allocation cost: ~200+ bytes per call (GUID struct + string allocation + formatting)
    // Counter allocation cost: ~8-16 bytes per call (long.ToString() for small numbers)
    //
    // Single-threaded: WASM is single-threaded, so we use simple ++ instead of Interlocked.
    // Format: "h{counter}" where h = handler prefix to distinguish from other IDs
    //
    // Overflow consideration: At 1 million handlers/second, overflow would take ~292 million
    // years. For a WebAssembly UI framework, this is effectively infinite - no handling needed.
    // =============================================================================
    private static long _commandIdCounter;

    /// <summary>
    /// Generates a unique command ID using a simple counter.
    /// This is much faster and allocates less than Guid.NewGuid().ToString().
    /// Uses simple increment since WASM is single-threaded (no Interlocked needed).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NextCommandId()
    {
        var id = ++_commandIdCounter;
        // Buffer size 24: max long is 19 digits + 'h' prefix = 20 chars, with margin
        return string.Create(null, stackalloc char[24], $"h{id}");
    }

    public static Handler on(string name, Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(name, NextCommandId(), command, id ?? string.Empty);

    public static Handler on<T>(string name, Func<T?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(name, NextCommandId(), null, id ?? string.Empty, o => factory((T?)o), typeof(T));

    public static Handler on(string name, Func<string?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on<string?>(name, factory, id);
    public static Handler onclick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("click", command, id);

    public static Handler onclick(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("click", factory, id);

    public static Handler onchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("change", command, id);

    public static Handler onchange(Func<InputEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("change", factory, id);

    public static Handler onblur(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("blur", command, id);

    public static Handler onblur(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("blur", factory, id);

    public static Handler onfocus(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focus", command, id);

    public static Handler onfocus(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focus", factory, id);

    public static Handler oninput(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("input", command, id);

    public static Handler oninput(Func<InputEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("input", factory, id);

    public static Handler onkeydown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keydown", command, id);

    public static Handler onkeydown(Func<KeyEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keydown", factory, id);

    public static Handler onkeypress(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keypress", command, id);

    public static Handler onkeypress(Func<KeyEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keypress", factory, id);

    public static Handler onkeyup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keyup", command, id);

    public static Handler onkeyup(Func<KeyEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keyup", factory, id);

    public static Handler onmousedown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousedown", command, id);

    public static Handler onmousedown(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousedown", factory, id);

    public static Handler onmouseup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseup", command, id);

    public static Handler onmouseup(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseup", factory, id);

    public static Handler onmouseover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseover", command, id);

    public static Handler onmouseover(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseover", factory, id);

    public static Handler onmouseout(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseout", command, id);

    public static Handler onmouseout(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseout", factory, id);

    public static Handler onmouseenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseenter", command, id);

    public static Handler onmouseenter(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseenter", factory, id);

    public static Handler onmouseleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseleave", command, id);

    public static Handler onmouseleave(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseleave", factory, id);

    public static Handler onmousemove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousemove", command, id);

    public static Handler onmousemove(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousemove", factory, id);

    public static Handler onwheel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("wheel", command, id);

    public static Handler onwheel(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("wheel", factory, id);

    public static Handler onsubmit(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("submit", command, id);

    public static Handler onsubmit(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("submit", factory, id);

    public static Handler onreset(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("reset", command, id);

    public static Handler onreset(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("reset", factory, id);

    public static Handler ondrag(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drag", command, id);

    public static Handler ondrag(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drag", factory, id);

    public static Handler ondragstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragstart", command, id);

    public static Handler ondragstart(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragstart", factory, id);

    public static Handler ondragend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragend", command, id);

    public static Handler ondragend(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragend", factory, id);

    public static Handler ondragenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragenter", command, id);

    public static Handler ondragenter(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragenter", factory, id);

    public static Handler ondragleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragleave", command, id);

    public static Handler ondragleave(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragleave", factory, id);

    public static Handler ondragover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragover", command, id);

    public static Handler ondragover(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragover", factory, id);

    public static Handler ondrop(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drop", command, id);

    public static Handler ondrop(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drop", factory, id);

    public static Handler oncontextmenu(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("contextmenu", command, id);

    public static Handler oncontextmenu(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("contextmenu", factory, id);

    public static Handler ondblclick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dblclick", command, id);

    public static Handler ondblclick(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dblclick", factory, id);

    public static Handler onscroll(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("scroll", command, id);

    /// <summary>
    /// Attaches a scroll handler that receives scroll position data.
    /// </summary>
    /// <remarks>
    /// Use this overload to receive <see cref="ScrollEventData"/> with scrollTop, scrollLeft,
    /// scrollHeight, scrollWidth, clientHeight, and clientWidth. Essential for virtualization.
    /// </remarks>
    /// <example>
    /// <code>
    /// div([style("overflow-y:auto;height:600px"), onscroll(data => new ScrollChanged(data?.ScrollTop ?? 0))], [...])
    /// </code>
    /// </example>
    public static Handler onscroll(Func<ScrollEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("scroll", factory, id);

    public static Handler onscroll(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("scroll", factory, id);

    public static Handler onresize(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("resize", command, id);

    public static Handler onresize(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("resize", factory, id);

    public static Handler onerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("error", command, id);

    public static Handler onerror(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("error", factory, id);

    public static Handler onload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("load", command, id);

    public static Handler onload(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("load", factory, id);

    public static Handler onunload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unload", command, id);

    public static Handler onunload(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unload", factory, id);

    public static Handler ontouchstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchstart", command, id);

    public static Handler ontouchstart(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchstart", factory, id);

    public static Handler ontouchend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchend", command, id);

    public static Handler ontouchend(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchend", factory, id);

    public static Handler ontouchmove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchmove", command, id);

    public static Handler ontouchmove(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchmove", factory, id);

    public static Handler ontouchcancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchcancel", command, id);

    public static Handler ontouchcancel(Func<PointerEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchcancel", factory, id);

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

    public static Handler ontransitionstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionstart", command, id);

    public static Handler ontransitionstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionstart", factory, id);

    public static Handler ontransitionend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionend", command, id);

    public static Handler ontransitionend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionend", factory, id);

    public static Handler onplay(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("play", command, id);

    public static Handler onplay(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("play", factory, id);

    public static Handler onpause(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pause", command, id);

    public static Handler onpause(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pause", factory, id);

    public static Handler ontimeupdate(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("timeupdate", command, id);

    public static Handler ontimeupdate(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("timeupdate", factory, id);

    public static Handler onended(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ended", command, id);

    public static Handler onended(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ended", factory, id);

    public static Handler onbeforeunload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeunload", command, id);

    public static Handler onbeforeunload(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeunload", factory, id);

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

    public static Handler onoffline(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("offline", command, id);

    public static Handler onoffline(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("offline", factory, id);

    public static Handler ononline(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("online", command, id);

    public static Handler ononline(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("online", factory, id);

    public static Handler onstorage(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("storage", command, id);

    public static Handler onstorage(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("storage", factory, id);

    public static Handler oncanplay(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplay", command, id);

    public static Handler oncanplay(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplay", factory, id);

    public static Handler oncanplaythrough(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplaythrough", command, id);

    public static Handler oncanplaythrough(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplaythrough", factory, id);

    public static Handler ondurationchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("durationchange", command, id);

    public static Handler ondurationchange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("durationchange", factory, id);

    public static Handler onemptied(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("emptied", command, id);

    public static Handler onemptied(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("emptied", factory, id);

    public static Handler onstalled(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("stalled", command, id);

    public static Handler onstalled(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("stalled", factory, id);

    public static Handler onsuspend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("suspend", command, id);

    public static Handler onsuspend(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("suspend", factory, id);

    public static Handler onratechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ratechange", command, id);

    public static Handler onratechange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ratechange", factory, id);

    public static Handler onvolumechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("volumechange", command, id);

    public static Handler onvolumechange(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("volumechange", factory, id);

    public static Handler onseeked(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeked", command, id);

    public static Handler onseeked(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeked", factory, id);

    public static Handler onseeking(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeking", command, id);

    public static Handler onseeking(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeking", factory, id);

    public static Handler onshow(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    => on("show", command, id);

    public static Handler onshow(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("show", factory, id);


    public static Handler oninvalid(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("invalid", command, id);

    public static Handler oninvalid(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("invalid", factory, id);

    public static Handler onsearch(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("search", command, id);

    public static Handler onsearch(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("search", factory, id);

    public static Handler onprogress(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("progress", command, id);

    public static Handler onprogress(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("progress", factory, id);

    public static Handler onloadstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadstart", command, id);

    public static Handler onloadstart(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadstart", factory, id);

    public static Handler onloadedmetadata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadedmetadata", command, id);

    public static Handler onloadedmetadata(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadedmetadata", factory, id);

    public static Handler onloadeddata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadeddata", command, id);

    public static Handler onloadeddata(Func<GenericEventData?, Message> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadeddata", factory, id);
}
