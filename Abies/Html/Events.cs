using System.Runtime.CompilerServices;
using Abies.DOM;
using Praefixum;

namespace Abies.Html;

public static class Events
{
    // Rarely used touch/pointer/mobile events
    public static Handler ongotpointercapture(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gotpointercapture", command, id);

    public static Handler onlostpointercapture(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("lostpointercapture", command, id);

    // Advanced media handling
    public static Handler onencrypted(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("encrypted", command, id);

    public static Handler onwaiting(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("waiting", command, id);

    // The newest bleeding-edge events (HTML Living Standard)
    public static Handler onformdata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("formdata", command, id);

    public static Handler onbeforexrselect(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforexrselect", command, id);

    public static Handler onafterprint(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("afterprint", command, id);

    public static Handler onbeforeprint(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeprint", command, id);

    public static Handler onlanguagechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("languagechange", command, id);

    public static Handler onmessage(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("message", command, id);

    public static Handler onmessageerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("messageerror", command, id);

    public static Handler onrejectionhandled(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("rejectionhandled", command, id);

    public static Handler onunhandledrejection(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unhandledrejection", command, id);

    public static Handler onsecuritypolicyviolation(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("securitypolicyviolation", command, id);

    // Experimental sensor/device API events
    public static Handler ondevicemotion(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("devicemotion", command, id);

    public static Handler ondeviceorientation(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("deviceorientation", command, id);

    public static Handler ondeviceorientationabsolute(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("deviceorientationabsolute", command, id);
    // Dialog events
    public static Handler onclose(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("close", command, id);

    public static Handler oncancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cancel", command, id);

    // Page visibility
    public static Handler onvisibilitychange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("visibilitychange", command, id);

    // Newer events
    public static Handler onselectionchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("selectionchange", command, id);

    public static Handler ongesturestart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gesturestart", command, id);

    public static Handler ongesturechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gesturechange", command, id);

    public static Handler ongestureend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("gestureend", command, id);

    // Web Audio-related events
    public static Handler onaudioprocess(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("audioprocess", command, id);

    // Popover-related (new in spec)
    public static Handler onbeforetoggle(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforetoggle", command, id);

    public static Handler ontoggle(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("toggle", command, id);
    public static Handler onpointerdown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerdown", command, id);

    public static Handler onpointerup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerup", command, id);

    public static Handler onpointermove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointermove", command, id);

    public static Handler onpointercancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointercancel", command, id);

    public static Handler onpointerover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerover", command, id);

    public static Handler onpointerout(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerout", command, id);

    public static Handler onpointerenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerenter", command, id);

    public static Handler onpointerleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pointerleave", command, id);

    // Intersection Observer related
    public static Handler onintersect(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("intersect", command, id);

    // Web Animation related
    public static Handler onfinish(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("finish", command, id);

    // Web Component related
    public static Handler onslotchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("slotchange", command, id);

    // Screen related
    public static Handler onfullscreenchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenchange", command, id);

    public static Handler onfullscreenerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("fullscreenerror", command, id);

    public static Handler on(string name, Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(name, Guid.NewGuid().ToString(), command, id);
    public static Handler onclick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("click", command, id);

    public static Handler onchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("change", command, id);

    public static Handler onblur(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("blur", command, id);

    public static Handler onfocus(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("focus", command, id);

    public static Handler oninput(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("input", command, id);

    public static Handler onkeydown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keydown", command, id);

    public static Handler onkeypress(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keypress", command, id);

    public static Handler onkeyup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("keyup", command, id);

    public static Handler onmousedown(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousedown", command, id);

    public static Handler onmouseup(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseup", command, id);

    public static Handler onmouseover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseover", command, id);

    public static Handler onmouseout(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseout", command, id);

    public static Handler onmouseenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseenter", command, id);

    public static Handler onmouseleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mouseleave", command, id);

    public static Handler onmousemove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("mousemove", command, id);

    public static Handler onwheel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("wheel", command, id);

    public static Handler onsubmit(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("submit", command, id);

    public static Handler onreset(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("reset", command, id);

    public static Handler ondrag(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drag", command, id);

    public static Handler ondragstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragstart", command, id);

    public static Handler ondragend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragend", command, id);

    public static Handler ondragenter(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragenter", command, id);

    public static Handler ondragleave(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragleave", command, id);

    public static Handler ondragover(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dragover", command, id);

    public static Handler ondrop(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("drop", command, id);

    public static Handler oncontextmenu(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("contextmenu", command, id);

    public static Handler ondblclick(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("dblclick", command, id);

    public static Handler onscroll(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("scroll", command, id);

    public static Handler onresize(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("resize", command, id);

    public static Handler onerror(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("error", command, id);

    public static Handler onload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("load", command, id);

    public static Handler onunload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("unload", command, id);

    public static Handler ontouchstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchstart", command, id);

    public static Handler ontouchend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchend", command, id);

    public static Handler ontouchmove(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchmove", command, id);

    public static Handler ontouchcancel(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("touchcancel", command, id);

    public static Handler onanimationstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationstart", command, id);

    public static Handler onanimationend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationend", command, id);

    public static Handler onanimationiteration(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("animationiteration", command, id);

    public static Handler ontransitionstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionstart", command, id);

    public static Handler ontransitionend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("transitionend", command, id);

    public static Handler onplay(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("play", command, id);

    public static Handler onpause(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("pause", command, id);

    public static Handler ontimeupdate(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("timeupdate", command, id);

    public static Handler onended(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ended", command, id);

    public static Handler onbeforeunload(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("beforeunload", command, id);

    public static Handler oncopy(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("copy", command, id);

    public static Handler oncut(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("cut", command, id);

    public static Handler onpaste(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("paste", command, id);

    public static Handler onoffline(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("offline", command, id);

    public static Handler ononline(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("online", command, id);

    public static Handler onstorage(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("storage", command, id);

    public static Handler oncanplay(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplay", command, id);

    public static Handler oncanplaythrough(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("canplaythrough", command, id);

    public static Handler ondurationchange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("durationchange", command, id);

    public static Handler onemptied(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("emptied", command, id);

    public static Handler onstalled(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("stalled", command, id);

    public static Handler onsuspend(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("suspend", command, id);

    public static Handler onratechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("ratechange", command, id);

    public static Handler onvolumechange(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("volumechange", command, id);

    public static Handler onseeked(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeked", command, id);

    public static Handler onseeking(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("seeking", command, id);

    public static Handler onshow(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    => on("show", command, id);


    public static Handler oninvalid(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("invalid", command, id);

    public static Handler onsearch(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("search", command, id);

    public static Handler onprogress(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("progress", command, id);

    public static Handler onloadstart(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadstart", command, id);

    public static Handler onloadedmetadata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadedmetadata", command, id);

    public static Handler onloadeddata(Message command, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => on("loadeddata", command, id);
}