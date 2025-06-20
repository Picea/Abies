using System.Runtime.CompilerServices;
using Abies.DOM;

namespace Abies.Html;

public static class Events
{
    // Rarely used touch/pointer/mobile events
    public static Handler ongotpointercapture(Message command, [CallerLineNumber] int id = 0)
        => on("gotpointercapture", command, id);

    public static Handler onlostpointercapture(Message command, [CallerLineNumber] int id = 0)
        => on("lostpointercapture", command, id);

    // Advanced media handling
    public static Handler onencrypted(Message command, [CallerLineNumber] int id = 0)
        => on("encrypted", command, id);

    public static Handler onwaiting(Message command, [CallerLineNumber] int id = 0)
        => on("waiting", command, id);

    // The newest bleeding-edge events (HTML Living Standard)
    public static Handler onformdata(Message command, [CallerLineNumber] int id = 0)
        => on("formdata", command, id);

    public static Handler onbeforexrselect(Message command, [CallerLineNumber] int id = 0)
        => on("beforexrselect", command, id);

    public static Handler onafterprint(Message command, [CallerLineNumber] int id = 0)
        => on("afterprint", command, id);

    public static Handler onbeforeprint(Message command, [CallerLineNumber] int id = 0)
        => on("beforeprint", command, id);

    public static Handler onlanguagechange(Message command, [CallerLineNumber] int id = 0)
        => on("languagechange", command, id);

    public static Handler onmessage(Message command, [CallerLineNumber] int id = 0)
        => on("message", command, id);

    public static Handler onmessageerror(Message command, [CallerLineNumber] int id = 0)
        => on("messageerror", command, id);

    public static Handler onrejectionhandled(Message command, [CallerLineNumber] int id = 0)
        => on("rejectionhandled", command, id);

    public static Handler onunhandledrejection(Message command, [CallerLineNumber] int id = 0)
        => on("unhandledrejection", command, id);

    public static Handler onsecuritypolicyviolation(Message command, [CallerLineNumber] int id = 0)
        => on("securitypolicyviolation", command, id);

    // Experimental sensor/device API events
    public static Handler ondevicemotion(Message command, [CallerLineNumber] int id = 0)
        => on("devicemotion", command, id);

    public static Handler ondeviceorientation(Message command, [CallerLineNumber] int id = 0)
        => on("deviceorientation", command, id);

    public static Handler ondeviceorientationabsolute(Message command, [CallerLineNumber] int id = 0)
        => on("deviceorientationabsolute", command, id);
    // Dialog events
    public static Handler onclose(Message command, [CallerLineNumber] int id = 0)
        => on("close", command, id);

    public static Handler oncancel(Message command, [CallerLineNumber] int id = 0)
        => on("cancel", command, id);

    // Page visibility
    public static Handler onvisibilitychange(Message command, [CallerLineNumber] int id = 0)
        => on("visibilitychange", command, id);

    // Newer events
    public static Handler onselectionchange(Message command, [CallerLineNumber] int id = 0)
        => on("selectionchange", command, id);

    public static Handler ongesturestart(Message command, [CallerLineNumber] int id = 0)
        => on("gesturestart", command, id);

    public static Handler ongesturechange(Message command, [CallerLineNumber] int id = 0)
        => on("gesturechange", command, id);

    public static Handler ongestureend(Message command, [CallerLineNumber] int id = 0)
        => on("gestureend", command, id);

    // Web Audio-related events
    public static Handler onaudioprocess(Message command, [CallerLineNumber] int id = 0)
        => on("audioprocess", command, id);

    // Popover-related (new in spec)
    public static Handler onbeforetoggle(Message command, [CallerLineNumber] int id = 0)
        => on("beforetoggle", command, id);

    public static Handler ontoggle(Message command, [CallerLineNumber] int id = 0)
        => on("toggle", command, id);
    public static Handler onpointerdown(Message command, [CallerLineNumber] int id = 0)
        => on("pointerdown", command, id);

    public static Handler onpointerup(Message command, [CallerLineNumber] int id = 0)
        => on("pointerup", command, id);

    public static Handler onpointermove(Message command, [CallerLineNumber] int id = 0)
        => on("pointermove", command, id);

    public static Handler onpointercancel(Message command, [CallerLineNumber] int id = 0)
        => on("pointercancel", command, id);

    public static Handler onpointerover(Message command, [CallerLineNumber] int id = 0)
        => on("pointerover", command, id);

    public static Handler onpointerout(Message command, [CallerLineNumber] int id = 0)
        => on("pointerout", command, id);

    public static Handler onpointerenter(Message command, [CallerLineNumber] int id = 0)
        => on("pointerenter", command, id);

    public static Handler onpointerleave(Message command, [CallerLineNumber] int id = 0)
        => on("pointerleave", command, id);

    // Intersection Observer related
    public static Handler onintersect(Message command, [CallerLineNumber] int id = 0)
        => on("intersect", command, id);

    // Web Animation related
    public static Handler onfinish(Message command, [CallerLineNumber] int id = 0)
        => on("finish", command, id);

    // Web Component related
    public static Handler onslotchange(Message command, [CallerLineNumber] int id = 0)
        => on("slotchange", command, id);

    // Screen related
    public static Handler onfullscreenchange(Message command, [CallerLineNumber] int id = 0)
        => on("fullscreenchange", command, id);

    public static Handler onfullscreenerror(Message command, [CallerLineNumber] int id = 0)
        => on("fullscreenerror", command, id);

    public static Handler on(string name, Message command, [CallerLineNumber] int id = 0)
        => new(name, Guid.NewGuid().ToString(), command, id);
    public static Handler onclick(Message command, [CallerLineNumber] int id = 0)
        => on("click", command, id);

    public static Handler onchange(Message command, [CallerLineNumber] int id = 0)
        => on("change", command, id);

    public static Handler onblur(Message command, [CallerLineNumber] int id = 0)
        => on("blur", command, id);

    public static Handler onfocus(Message command, [CallerLineNumber] int id = 0)
        => on("focus", command, id);

    public static Handler oninput(Message command, [CallerLineNumber] int id = 0)
        => on("input", command, id);

    public static Handler onkeydown(Message command, [CallerLineNumber] int id = 0)
        => on("keydown", command, id);

    public static Handler onkeypress(Message command, [CallerLineNumber] int id = 0)
        => on("keypress", command, id);

    public static Handler onkeyup(Message command, [CallerLineNumber] int id = 0)
        => on("keyup", command, id);

    public static Handler onmousedown(Message command, [CallerLineNumber] int id = 0)
        => on("mousedown", command, id);

    public static Handler onmouseup(Message command, [CallerLineNumber] int id = 0)
        => on("mouseup", command, id);

    public static Handler onmouseover(Message command, [CallerLineNumber] int id = 0)
        => on("mouseover", command, id);

    public static Handler onmouseout(Message command, [CallerLineNumber] int id = 0)
        => on("mouseout", command, id);

    public static Handler onmouseenter(Message command, [CallerLineNumber] int id = 0)
        => on("mouseenter", command, id);

    public static Handler onmouseleave(Message command, [CallerLineNumber] int id = 0)
        => on("mouseleave", command, id);

    public static Handler onmousemove(Message command, [CallerLineNumber] int id = 0)
        => on("mousemove", command, id);

    public static Handler onwheel(Message command, [CallerLineNumber] int id = 0)
        => on("wheel", command, id);

    public static Handler onsubmit(Message command, [CallerLineNumber] int id = 0)
        => on("submit", command, id);

    public static Handler onreset(Message command, [CallerLineNumber] int id = 0)
        => on("reset", command, id);

    public static Handler ondrag(Message command, [CallerLineNumber] int id = 0)
        => on("drag", command, id);

    public static Handler ondragstart(Message command, [CallerLineNumber] int id = 0)
        => on("dragstart", command, id);

    public static Handler ondragend(Message command, [CallerLineNumber] int id = 0)
        => on("dragend", command, id);

    public static Handler ondragenter(Message command, [CallerLineNumber] int id = 0)
        => on("dragenter", command, id);

    public static Handler ondragleave(Message command, [CallerLineNumber] int id = 0)
        => on("dragleave", command, id);

    public static Handler ondragover(Message command, [CallerLineNumber] int id = 0)
        => on("dragover", command, id);

    public static Handler ondrop(Message command, [CallerLineNumber] int id = 0)
        => on("drop", command, id);

    public static Handler oncontextmenu(Message command, [CallerLineNumber] int id = 0)
        => on("contextmenu", command, id);

    public static Handler ondblclick(Message command, [CallerLineNumber] int id = 0)
        => on("dblclick", command, id);

    public static Handler onscroll(Message command, [CallerLineNumber] int id = 0)
        => on("scroll", command, id);

    public static Handler onresize(Message command, [CallerLineNumber] int id = 0)
        => on("resize", command, id);

    public static Handler onerror(Message command, [CallerLineNumber] int id = 0)
        => on("error", command, id);

    public static Handler onload(Message command, [CallerLineNumber] int id = 0)
        => on("load", command, id);

    public static Handler onunload(Message command, [CallerLineNumber] int id = 0)
        => on("unload", command, id);

    public static Handler ontouchstart(Message command, [CallerLineNumber] int id = 0)
        => on("touchstart", command, id);

    public static Handler ontouchend(Message command, [CallerLineNumber] int id = 0)
        => on("touchend", command, id);

    public static Handler ontouchmove(Message command, [CallerLineNumber] int id = 0)
        => on("touchmove", command, id);

    public static Handler ontouchcancel(Message command, [CallerLineNumber] int id = 0)
        => on("touchcancel", command, id);

    public static Handler onanimationstart(Message command, [CallerLineNumber] int id = 0)
        => on("animationstart", command, id);

    public static Handler onanimationend(Message command, [CallerLineNumber] int id = 0)
        => on("animationend", command, id);

    public static Handler onanimationiteration(Message command, [CallerLineNumber] int id = 0)
        => on("animationiteration", command, id);

    public static Handler ontransitionstart(Message command, [CallerLineNumber] int id = 0)
        => on("transitionstart", command, id);

    public static Handler ontransitionend(Message command, [CallerLineNumber] int id = 0)
        => on("transitionend", command, id);

    public static Handler onplay(Message command, [CallerLineNumber] int id = 0)
        => on("play", command, id);

    public static Handler onpause(Message command, [CallerLineNumber] int id = 0)
        => on("pause", command, id);

    public static Handler ontimeupdate(Message command, [CallerLineNumber] int id = 0)
        => on("timeupdate", command, id);

    public static Handler onended(Message command, [CallerLineNumber] int id = 0)
        => on("ended", command, id);

    public static Handler onbeforeunload(Message command, [CallerLineNumber] int id = 0)
        => on("beforeunload", command, id);

    public static Handler oncopy(Message command, [CallerLineNumber] int id = 0)
        => on("copy", command, id);

    public static Handler oncut(Message command, [CallerLineNumber] int id = 0)
        => on("cut", command, id);

    public static Handler onpaste(Message command, [CallerLineNumber] int id = 0)
        => on("paste", command, id);

    public static Handler onoffline(Message command, [CallerLineNumber] int id = 0)
        => on("offline", command, id);

    public static Handler ononline(Message command, [CallerLineNumber] int id = 0)
        => on("online", command, id);

    public static Handler onstorage(Message command, [CallerLineNumber] int id = 0)
        => on("storage", command, id);

    public static Handler oncanplay(Message command, [CallerLineNumber] int id = 0)
        => on("canplay", command, id);

    public static Handler oncanplaythrough(Message command, [CallerLineNumber] int id = 0)
        => on("canplaythrough", command, id);

    public static Handler ondurationchange(Message command, [CallerLineNumber] int id = 0)
        => on("durationchange", command, id);

    public static Handler onemptied(Message command, [CallerLineNumber] int id = 0)
        => on("emptied", command, id);

    public static Handler onstalled(Message command, [CallerLineNumber] int id = 0)
        => on("stalled", command, id);

    public static Handler onsuspend(Message command, [CallerLineNumber] int id = 0)
        => on("suspend", command, id);

    public static Handler onratechange(Message command, [CallerLineNumber] int id = 0)
        => on("ratechange", command, id);

    public static Handler onvolumechange(Message command, [CallerLineNumber] int id = 0)
        => on("volumechange", command, id);

    public static Handler onseeked(Message command, [CallerLineNumber] int id = 0)
        => on("seeked", command, id);

    public static Handler onseeking(Message command, [CallerLineNumber] int id = 0)
        => on("seeking", command, id);

    public static Handler onshow(Message command, [CallerLineNumber] int id = 0)
    => on("show", command, id);


    public static Handler oninvalid(Message command, [CallerLineNumber] int id = 0)
        => on("invalid", command, id);

    public static Handler onsearch(Message command, [CallerLineNumber] int id = 0)
        => on("search", command, id);

    public static Handler onprogress(Message command, [CallerLineNumber] int id = 0)
        => on("progress", command, id);

    public static Handler onloadstart(Message command, [CallerLineNumber] int id = 0)
        => on("loadstart", command, id);

    public static Handler onloadedmetadata(Message command, [CallerLineNumber] int id = 0)
        => on("loadedmetadata", command, id);

    public static Handler onloadeddata(Message command, [CallerLineNumber] int id = 0)
        => on("loadeddata", command, id);
}