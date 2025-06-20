using System.Runtime.CompilerServices;

namespace Abies.Html;

public static class Attributes
{

    public static DOM.Attribute autofocus(string value = "true", [CallerLineNumber] int id = 0)
    => attribute("autofocus", value, id);

    public static DOM.Attribute spellcheck(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("spellcheck", value, id);

    public static DOM.Attribute contenteditable(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("contenteditable", value, id);

    public static DOM.Attribute draggable(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("draggable", value, id);

    public static DOM.Attribute lang(string value, [CallerLineNumber] int id = 0)
        => attribute("lang", value, id);

    public static DOM.Attribute tabindex(string value, [CallerLineNumber] int id = 0)
        => attribute("tabindex", value, id);

    public static DOM.Attribute ariaLabel(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-label", value, id);

    public static DOM.Attribute ariaHidden(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("aria-hidden", value, id);

    public static DOM.Attribute ariaExpanded(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-expanded", value, id);

    public static DOM.Attribute ariaControls(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-controls", value, id);

    public static DOM.Attribute ariaDescribedby(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-describedby", value, id);

    public static DOM.Attribute data(string name, string value, [CallerLineNumber] int id = 0)
        => attribute($"data-{name}", value, id);

    // SVG-specific attributes
    public static DOM.Attribute viewBox(string value, [CallerLineNumber] int id = 0)
        => attribute("viewBox", value, id);

    public static DOM.Attribute fill(string value, [CallerLineNumber] int id = 0)
        => attribute("fill", value, id);

    public static DOM.Attribute stroke(string value, [CallerLineNumber] int id = 0)
        => attribute("stroke", value, id);

    public static DOM.Attribute strokeWidth(string value, [CallerLineNumber] int id = 0)
        => attribute("stroke-width", value, id);

    public static DOM.Attribute cx(string value, [CallerLineNumber] int id = 0)
        => attribute("cx", value, id);

    public static DOM.Attribute cy(string value, [CallerLineNumber] int id = 0)
        => attribute("cy", value, id);

    public static DOM.Attribute r(string value, [CallerLineNumber] int id = 0)
        => attribute("r", value, id);

    public static DOM.Attribute x(string value, [CallerLineNumber] int id = 0)
        => attribute("x", value, id);

    public static DOM.Attribute y(string value, [CallerLineNumber] int id = 0)
        => attribute("y", value, id);

    public static DOM.Attribute x1(string value, [CallerLineNumber] int id = 0)
        => attribute("x1", value, id);

    public static DOM.Attribute y1(string value, [CallerLineNumber] int id = 0)
        => attribute("y1", value, id);

    public static DOM.Attribute x2(string value, [CallerLineNumber] int id = 0)
        => attribute("x2", value, id);

    public static DOM.Attribute y2(string value, [CallerLineNumber] int id = 0)
        => attribute("y2", value, id);

    public static DOM.Attribute points(string value, [CallerLineNumber] int id = 0)
        => attribute("points", value, id);

    public static DOM.Attribute d(string value, [CallerLineNumber] int id = 0)
        => attribute("d", value, id);

    // HTML specific
    public static DOM.Attribute ping(string value, [CallerLineNumber] int id = 0)
        => attribute("ping", value, id);

    public static DOM.Attribute inputmode(string value, [CallerLineNumber] int id = 0)
        => attribute("inputmode", value, id);

    public static DOM.Attribute is_(string value, [CallerLineNumber] int id = 0)
        => attribute("is", value, id);

    public static DOM.Attribute fallback(string value, [CallerLineNumber] int id = 0)
        => attribute("fallback", value, id);

    public static DOM.Attribute importance(string value, [CallerLineNumber] int id = 0)
        => attribute("importance", value, id);

    public static DOM.Attribute fetchpriority(string value, [CallerLineNumber] int id = 0)
        => attribute("fetchpriority", value, id);

    public static DOM.Attribute popovertarget(string value, [CallerLineNumber] int id = 0)
        => attribute("popovertarget", value, id);

    public static DOM.Attribute popover(string value, [CallerLineNumber] int id = 0)
        => attribute("popover", value, id);

    public static DOM.Attribute translate(string value, [CallerLineNumber] int id = 0)
        => attribute("translate", value, id);

    public static DOM.Attribute autocapitalize(string value, [CallerLineNumber] int id = 0)
        => attribute("autocapitalize", value, id);

    public static DOM.Attribute autoplay(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("autoplay", value, id);

    public static DOM.Attribute capture(string value, [CallerLineNumber] int id = 0)
        => attribute("capture", value, id);

    public static DOM.Attribute decoding(string value, [CallerLineNumber] int id = 0)
        => attribute("decoding", value, id);

    public static DOM.Attribute enterkeyhint(string value, [CallerLineNumber] int id = 0)
        => attribute("enterkeyhint", value, id);

    public static DOM.Attribute itemid(string value, [CallerLineNumber] int id = 0)
        => attribute("itemid", value, id);

    public static DOM.Attribute itemprop(string value, [CallerLineNumber] int id = 0)
        => attribute("itemprop", value, id);

    public static DOM.Attribute itemref(string value, [CallerLineNumber] int id = 0)
        => attribute("itemref", value, id);

    public static DOM.Attribute itemscope(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("itemscope", value, id);

    public static DOM.Attribute itemtype(string value, [CallerLineNumber] int id = 0)
        => attribute("itemtype", value, id);

    // Additional ARIA attributes
    public static DOM.Attribute ariaLevel(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-level", value, id);

    public static DOM.Attribute ariaValuenow(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-valuenow", value, id);

    public static DOM.Attribute ariaValuemin(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-valuemin", value, id);

    public static DOM.Attribute ariaValuemax(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-valuemax", value, id);

    public static DOM.Attribute ariaValuetext(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-valuetext", value, id);

    public static DOM.Attribute ariaPressed(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-pressed", value, id);

    public static DOM.Attribute ariaSelected(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-selected", value, id);

    public static DOM.Attribute accept(string value, [CallerLineNumber] int id = 0)
        => attribute("accept", value, id);

    public static DOM.Attribute checked_(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("checked", value, id);

    public static DOM.Attribute selected(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("selected", value, id);

    public static DOM.Attribute cols(string value, [CallerLineNumber] int id = 0)
        => attribute("cols", value, id);

    public static DOM.Attribute rows(string value, [CallerLineNumber] int id = 0)
        => attribute("rows", value, id);

    public static DOM.Attribute colspan(string value, [CallerLineNumber] int id = 0)
        => attribute("colspan", value, id);

    public static DOM.Attribute rowspan(string value, [CallerLineNumber] int id = 0)
        => attribute("rowspan", value, id);

    public static DOM.Attribute for_(string value, [CallerLineNumber] int id = 0)
        => attribute("for", value, id);

    public static DOM.Attribute headers(string value, [CallerLineNumber] int id = 0)
        => attribute("headers", value, id);

    public static DOM.Attribute hidden(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("hidden", value, id);

    public static DOM.Attribute list(string value, [CallerLineNumber] int id = 0)
        => attribute("list", value, id);

    public static DOM.Attribute max(string value, [CallerLineNumber] int id = 0)
        => attribute("max", value, id);

    public static DOM.Attribute min(string value, [CallerLineNumber] int id = 0)
        => attribute("min", value, id);

    public static DOM.Attribute step(string value, [CallerLineNumber] int id = 0)
        => attribute("step", value, id);

    public static DOM.Attribute multiple(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("multiple", value, id);

    public static DOM.Attribute open(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("open", value, id);

    public static DOM.Attribute wrap(string value, [CallerLineNumber] int id = 0)
        => attribute("wrap", value, id);

    public static DOM.Attribute accesskey(string value, [CallerLineNumber] int id = 0)
        => attribute("accesskey", value, id);

    public static DOM.Attribute dir(string value, [CallerLineNumber] int id = 0)
        => attribute("dir", value, id);

    public static DOM.Attribute form(string value, [CallerLineNumber] int id = 0)
        => attribute("form", value, id);

    public static DOM.Attribute formaction(string value, [CallerLineNumber] int id = 0)
        => attribute("formaction", value, id);

    public static DOM.Attribute formenctype(string value, [CallerLineNumber] int id = 0)
        => attribute("formenctype", value, id);

    public static DOM.Attribute formmethod(string value, [CallerLineNumber] int id = 0)
        => attribute("formmethod", value, id);

    public static DOM.Attribute formnovalidate(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("formnovalidate", value, id);

    public static DOM.Attribute formtarget(string value, [CallerLineNumber] int id = 0)
        => attribute("formtarget", value, id);

    public static DOM.Attribute hreflang(string value, [CallerLineNumber] int id = 0)
        => attribute("hreflang", value, id);

    public static DOM.Attribute media(string value, [CallerLineNumber] int id = 0)
        => attribute("media", value, id);

    public static DOM.Attribute loading(string value, [CallerLineNumber] int id = 0)
        => attribute("loading", value, id);

    public static DOM.Attribute sizes(string value, [CallerLineNumber] int id = 0)
        => attribute("sizes", value, id);

    public static DOM.Attribute srcset(string value, [CallerLineNumber] int id = 0)
        => attribute("srcset", value, id);

    public static DOM.Attribute preload(string value, [CallerLineNumber] int id = 0)
        => attribute("preload", value, id);

    public static DOM.Attribute controls(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("controls", value, id);

    public static DOM.Attribute loop(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("loop", value, id);

    public static DOM.Attribute muted(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("muted", value, id);

    public static DOM.Attribute poster(string value, [CallerLineNumber] int id = 0)
        => attribute("poster", value, id);

    public static DOM.Attribute sandbox(string value, [CallerLineNumber] int id = 0)
        => attribute("sandbox", value, id);

    public static DOM.Attribute scope(string value, [CallerLineNumber] int id = 0)
        => attribute("scope", value, id);

    public static DOM.Attribute cite(string value, [CallerLineNumber] int id = 0)
        => attribute("cite", value, id);

    public static DOM.Attribute datetime(string value, [CallerLineNumber] int id = 0)
        => attribute("datetime", value, id);

    public static DOM.Attribute reversed(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("reversed", value, id);

    public static DOM.Attribute start(string value, [CallerLineNumber] int id = 0)
        => attribute("start", value, id);
    public static DOM.Attribute attribute(string name, string value, [CallerLineNumber] int id = 0)
        => new(id, name, value);
    public static DOM.Attribute id(string value, [CallerLineNumber] int id = 0)
        => attribute("id", value, id);

    public static DOM.Attribute type(string value, [CallerLineNumber] int id = 0)
        => attribute("type", value, id);

    public static DOM.Attribute href(string value, [CallerLineNumber] int id = 0)
        => attribute("href", value, id);


    public static DOM.Attribute @class(string value, [CallerLineNumber] int id = 0)
        => attribute("class", value, id);

    public static DOM.Attribute style(string value, [CallerLineNumber] int id = 0)
        => attribute("style", value, id);

    public static DOM.Attribute title(string value, [CallerLineNumber] int id = 0)
        => attribute("title", value, id);

    public static DOM.Attribute alt(string value, [CallerLineNumber] int id = 0)
        => attribute("alt", value, id);

    public static DOM.Attribute src(string value, [CallerLineNumber] int id = 0)
        => attribute("src", value, id);

    public static DOM.Attribute width(string value, [CallerLineNumber] int id = 0)
        => attribute("width", value, id);

    public static DOM.Attribute height(string value, [CallerLineNumber] int id = 0)
        => attribute("height", value, id);

    public static DOM.Attribute placeholder(string value, [CallerLineNumber] int id = 0)
        => attribute("placeholder", value, id);

    public static DOM.Attribute value(string value, [CallerLineNumber] int id = 0)
        => attribute("value", value, id);

    public static DOM.Attribute name(string value, [CallerLineNumber] int id = 0)
        => attribute("name", value, id);

    public static DOM.Attribute disabled(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("disabled", value, id);

    public static DOM.Attribute @readonly(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("readonly", value, id);

    public static DOM.Attribute required(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("required", value, id);

    public static DOM.Attribute maxlength(string value, [CallerLineNumber] int id = 0)
        => attribute("maxlength", value, id);

    public static DOM.Attribute minlength(string value, [CallerLineNumber] int id = 0)
        => attribute("minlength", value, id);

    public static DOM.Attribute pattern(string value, [CallerLineNumber] int id = 0)
        => attribute("pattern", value, id);

    public static DOM.Attribute role(string value, [CallerLineNumber] int id = 0)
        => attribute("role", value, id);

    public static DOM.Attribute charset(string value, [CallerLineNumber] int id = 0)
        => attribute("charset", value, id);

    public static DOM.Attribute rel(string value, [CallerLineNumber] int id = 0)
        => attribute("rel", value, id);

    public static DOM.Attribute target(string value, [CallerLineNumber] int id = 0)
        => attribute("target", value, id);

    public static DOM.Attribute download(string value, [CallerLineNumber] int id = 0)
        => attribute("download", value, id);

    public static DOM.Attribute crossorigin(string value, [CallerLineNumber] int id = 0)
        => attribute("crossorigin", value, id);

    public static DOM.Attribute referrerpolicy(string value, [CallerLineNumber] int id = 0)
        => attribute("referrerpolicy", value, id);

    public static DOM.Attribute async(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("async", value, id);

    public static DOM.Attribute defer(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("defer", value, id);

    public static DOM.Attribute integrity(string value, [CallerLineNumber] int id = 0)
        => attribute("integrity", value, id);

    public static DOM.Attribute autocomplete(string value, [CallerLineNumber] int id = 0)
        => attribute("autocomplete", value, id);

    public static DOM.Attribute novalidate(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("novalidate", value, id);

    public static DOM.Attribute enctype(string value, [CallerLineNumber] int id = 0)
        => attribute("enctype", value, id);

    public static DOM.Attribute method(string value, [CallerLineNumber] int id = 0)
        => attribute("method", value, id);

    public static DOM.Attribute action(string value, [CallerLineNumber] int id = 0)
        => attribute("action", value, id);

    public static DOM.Attribute part(string value, [CallerLineNumber] int id = 0)
        => attribute("part", value, id);

    public static DOM.Attribute exportparts(string value, [CallerLineNumber] int id = 0)
        => attribute("exportparts", value, id);

    public static DOM.Attribute inert(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("inert", value, id);

    public static DOM.Attribute nomodule(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("nomodule", value, id);

    public static DOM.Attribute nonce(string value, [CallerLineNumber] int id = 0)
        => attribute("nonce", value, id);

    public static DOM.Attribute type_(string value, [CallerLineNumber] int id = 0)
        => attribute("type", value, id);

    public static DOM.Attribute acceptcharset(string value, [CallerLineNumber] int id = 0)
        => attribute("accept-charset", value, id);

    // Extremely rare but valid attributes
    public static DOM.Attribute allowfullscreen(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("allowfullscreen", value, id);

    public static DOM.Attribute as_(string value, [CallerLineNumber] int id = 0)
        => attribute("as", value, id);

    public static DOM.Attribute challenge(string value, [CallerLineNumber] int id = 0)
        => attribute("challenge", value, id);

    public static DOM.Attribute color(string value, [CallerLineNumber] int id = 0)
        => attribute("color", value, id);

    public static DOM.Attribute default_(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("default", value, id);

    public static DOM.Attribute dirname(string value, [CallerLineNumber] int id = 0)
        => attribute("dirname", value, id);

    public static DOM.Attribute high(string value, [CallerLineNumber] int id = 0)
        => attribute("high", value, id);

    public static DOM.Attribute keytype(string value, [CallerLineNumber] int id = 0)
        => attribute("keytype", value, id);

    public static DOM.Attribute kind(string value, [CallerLineNumber] int id = 0)
        => attribute("kind", value, id);

    public static DOM.Attribute low(string value, [CallerLineNumber] int id = 0)
        => attribute("low", value, id);

    public static DOM.Attribute optimum(string value, [CallerLineNumber] int id = 0)
        => attribute("optimum", value, id);

    public static DOM.Attribute playsinline(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("playsinline", value, id);

    public static DOM.Attribute results(string value, [CallerLineNumber] int id = 0)
        => attribute("results", value, id);

    public static DOM.Attribute seamless(string value = "true", [CallerLineNumber] int id = 0)
        => attribute("seamless", value, id);

    public static DOM.Attribute shape(string value, [CallerLineNumber] int id = 0)
        => attribute("shape", value, id);

    public static DOM.Attribute srcdoc(string value, [CallerLineNumber] int id = 0)
        => attribute("srcdoc", value, id);

    public static DOM.Attribute srclang(string value, [CallerLineNumber] int id = 0)
        => attribute("srclang", value, id);

    // More SVG attributes
    public static DOM.Attribute rx(string value, [CallerLineNumber] int id = 0)
        => attribute("rx", value, id);

    public static DOM.Attribute ry(string value, [CallerLineNumber] int id = 0)
        => attribute("ry", value, id);

    public static DOM.Attribute dx(string value, [CallerLineNumber] int id = 0)
        => attribute("dx", value, id);

    public static DOM.Attribute dy(string value, [CallerLineNumber] int id = 0)
        => attribute("dy", value, id);

    public static DOM.Attribute gradientUnits(string value, [CallerLineNumber] int id = 0)
        => attribute("gradientUnits", value, id);

    public static DOM.Attribute patternUnits(string value, [CallerLineNumber] int id = 0)
        => attribute("patternUnits", value, id);

    public static DOM.Attribute spreadMethod(string value, [CallerLineNumber] int id = 0)
        => attribute("spreadMethod", value, id);

    public static DOM.Attribute transform(string value, [CallerLineNumber] int id = 0)
        => attribute("transform", value, id);

    public static DOM.Attribute markerWidth(string value, [CallerLineNumber] int id = 0)
        => attribute("markerWidth", value, id);

    public static DOM.Attribute markerHeight(string value, [CallerLineNumber] int id = 0)
        => attribute("markerHeight", value, id);

    public static DOM.Attribute refX(string value, [CallerLineNumber] int id = 0)
        => attribute("refX", value, id);

    public static DOM.Attribute refY(string value, [CallerLineNumber] int id = 0)
        => attribute("refY", value, id);

    public static DOM.Attribute markerUnits(string value, [CallerLineNumber] int id = 0)
        => attribute("markerUnits", value, id);

    public static DOM.Attribute preserveAspectRatio(string value, [CallerLineNumber] int id = 0)
        => attribute("preserveAspectRatio", value, id);

    public static DOM.Attribute vectorEffect(string value, [CallerLineNumber] int id = 0)
        => attribute("vector-effect", value, id);

    public static DOM.Attribute opacity(string value, [CallerLineNumber] int id = 0)
        => attribute("opacity", value, id);

    public static DOM.Attribute offset(string value, [CallerLineNumber] int id = 0)
        => attribute("offset", value, id);

    // Additional ARIA attributes 
    public static DOM.Attribute ariaAtomic(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-atomic", value, id);

    public static DOM.Attribute ariaBusy(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-busy", value, id);

    public static DOM.Attribute ariaChecked(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-checked", value, id);

    public static DOM.Attribute ariaCurrent(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-current", value, id);

    public static DOM.Attribute ariaDisabled(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-disabled", value, id);

    public static DOM.Attribute ariaErrormessage(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-errormessage", value, id);

    public static DOM.Attribute ariaHaspopup(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-haspopup", value, id);

    public static DOM.Attribute ariaInvalid(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-invalid", value, id);

    public static DOM.Attribute ariaKeyshortcuts(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-keyshortcuts", value, id);

    public static DOM.Attribute ariaLive(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-live", value, id);

    public static DOM.Attribute ariaModal(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-modal", value, id);

    public static DOM.Attribute ariaMultiline(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-multiline", value, id);

    public static DOM.Attribute ariaMultiselectable(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-multiselectable", value, id);

    public static DOM.Attribute ariaOrientation(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-orientation", value, id);

    public static DOM.Attribute ariaPlaceholder(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-placeholder", value, id);

    public static DOM.Attribute ariaReadonly(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-readonly", value, id);

    public static DOM.Attribute ariaRelevant(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-relevant", value, id);

    public static DOM.Attribute ariaRequired(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-required", value, id);

    public static DOM.Attribute ariaRoledescription(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-roledescription", value, id);

    public static DOM.Attribute ariaSort(string value, [CallerLineNumber] int id = 0)
        => attribute("aria-sort", value, id);
}
