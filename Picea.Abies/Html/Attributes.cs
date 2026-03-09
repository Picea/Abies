// =============================================================================
// HTML Attributes
// =============================================================================
// Provides functions for creating HTML attributes for virtual DOM elements.
// Each function returns a DOM.Attribute that can be attached to elements.
//
// Uses Praefixum source generator for compile-time unique IDs.
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM
// - ADR-014: Compile-Time Unique IDs
// =============================================================================

using Praefixum;

namespace Picea.Abies.Html;

/// <summary>
/// Provides factory functions for creating HTML attributes.
/// </summary>
/// <remarks>
/// All attribute functions are pure and return immutable Attribute records.
/// </remarks>
public static class Attributes
{
    // =========================================================================
    // Core Attribute Factory
    // =========================================================================

    /// <summary>
    /// Creates an attribute with the given name and value.
    /// This is the general-purpose factory; the named functions below are
    /// convenience wrappers.
    /// </summary>
    public static DOM.Attribute attribute(string name, string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(id ?? string.Empty, name, value);

    // =========================================================================
    // Global Attributes
    // =========================================================================

    public static DOM.Attribute id(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("id", value, id);

    public static DOM.Attribute class_(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("class", value, id);

    public static DOM.Attribute style(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("style", value, id);

    public static DOM.Attribute title(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("title", value, id);

    public static DOM.Attribute lang(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("lang", value, id);

    public static DOM.Attribute dir(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("dir", value, id);

    public static DOM.Attribute tabindex(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("tabindex", value, id);

    public static DOM.Attribute accesskey(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("accesskey", value, id);

    public static DOM.Attribute role(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("role", value, id);

    public static DOM.Attribute slot(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("slot", value, id);

    // =========================================================================
    // Boolean Attributes
    // =========================================================================

    public static DOM.Attribute hidden(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("hidden", value, id);

    public static DOM.Attribute disabled(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("disabled", value, id);

    public static DOM.Attribute checked_(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("checked", value, id);

    public static DOM.Attribute selected(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("selected", value, id);

    public static DOM.Attribute readonly_(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("readonly", value, id);

    public static DOM.Attribute required(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("required", value, id);

    public static DOM.Attribute multiple(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("multiple", value, id);

    public static DOM.Attribute autofocus(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("autofocus", value, id);

    public static DOM.Attribute autoplay(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("autoplay", value, id);

    public static DOM.Attribute controls(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("controls", value, id);

    public static DOM.Attribute loop(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("loop", value, id);

    public static DOM.Attribute muted(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("muted", value, id);

    public static DOM.Attribute defer(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("defer", value, id);

    public static DOM.Attribute async_(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("async", value, id);

    public static DOM.Attribute novalidate(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("novalidate", value, id);

    public static DOM.Attribute formnovalidate(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("formnovalidate", value, id);

    public static DOM.Attribute open_(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("open", value, id);

    public static DOM.Attribute inert(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("inert", value, id);

    public static DOM.Attribute reversed(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("reversed", value, id);

    public static DOM.Attribute allowfullscreen(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("allowfullscreen", value, id);

    public static DOM.Attribute default_(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("default", value, id);

    public static DOM.Attribute ismap(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("ismap", value, id);

    public static DOM.Attribute nomodule(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("nomodule", value, id);

    public static DOM.Attribute playsinline(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("playsinline", value, id);

    // =========================================================================
    // Editability & Interaction
    // =========================================================================

    public static DOM.Attribute contenteditable(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("contenteditable", value, id);

    public static DOM.Attribute draggable(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("draggable", value, id);

    public static DOM.Attribute spellcheck(string value = "true", [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("spellcheck", value, id);

    public static DOM.Attribute translate(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("translate", value, id);

    public static DOM.Attribute inputmode(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("inputmode", value, id);

    public static DOM.Attribute enterkeyhint(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("enterkeyhint", value, id);

    // =========================================================================
    // Common Form / Input Attributes
    // =========================================================================

    public static DOM.Attribute type(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("type", value, id);

    public static DOM.Attribute name(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("name", value, id);

    public static DOM.Attribute value(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("value", value, id);

    public static DOM.Attribute placeholder(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("placeholder", value, id);

    public static DOM.Attribute action(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("action", value, id);

    public static DOM.Attribute method(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("method", value, id);

    public static DOM.Attribute enctype(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("enctype", value, id);

    public static DOM.Attribute for_(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("for", value, id);

    public static DOM.Attribute min(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("min", value, id);

    public static DOM.Attribute max(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("max", value, id);

    public static DOM.Attribute step(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("step", value, id);

    public static DOM.Attribute maxlength(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("maxlength", value, id);

    public static DOM.Attribute minlength(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("minlength", value, id);

    public static DOM.Attribute size(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("size", value, id);

    public static DOM.Attribute pattern(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("pattern", value, id);

    public static DOM.Attribute accept(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("accept", value, id);

    public static DOM.Attribute autocomplete(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("autocomplete", value, id);

    public static DOM.Attribute list(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("list", value, id);

    public static DOM.Attribute form(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("form", value, id);

    public static DOM.Attribute formaction(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("formaction", value, id);

    public static DOM.Attribute formmethod(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("formmethod", value, id);

    public static DOM.Attribute formenctype(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("formenctype", value, id);

    public static DOM.Attribute formtarget(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("formtarget", value, id);

    // =========================================================================
    // Link & Navigation Attributes
    // =========================================================================

    public static DOM.Attribute href(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("href", value, id);

    public static DOM.Attribute src(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("src", value, id);

    public static DOM.Attribute target(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("target", value, id);

    public static DOM.Attribute rel(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("rel", value, id);

    public static DOM.Attribute download(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("download", value, id);

    public static DOM.Attribute ping(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("ping", value, id);

    public static DOM.Attribute referrerpolicy(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("referrerpolicy", value, id);

    public static DOM.Attribute hreflang(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("hreflang", value, id);

    // =========================================================================
    // Image / Media Attributes
    // =========================================================================

    public static DOM.Attribute alt(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("alt", value, id);

    public static DOM.Attribute width(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("width", value, id);

    public static DOM.Attribute height(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("height", value, id);

    public static DOM.Attribute loading(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("loading", value, id);

    public static DOM.Attribute decoding(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("decoding", value, id);

    public static DOM.Attribute srcset(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("srcset", value, id);

    public static DOM.Attribute sizes(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("sizes", value, id);

    public static DOM.Attribute poster(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("poster", value, id);

    public static DOM.Attribute preload(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("preload", value, id);

    public static DOM.Attribute crossorigin(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("crossorigin", value, id);

    public static DOM.Attribute usemap(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("usemap", value, id);

    // =========================================================================
    // Table Attributes
    // =========================================================================

    public static DOM.Attribute colspan(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("colspan", value, id);

    public static DOM.Attribute rowspan(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("rowspan", value, id);

    public static DOM.Attribute scope(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("scope", value, id);

    public static DOM.Attribute headers(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("headers", value, id);

    public static DOM.Attribute span_(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("span", value, id);

    // =========================================================================
    // Meta / Script Attributes
    // =========================================================================

    public static DOM.Attribute charset(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("charset", value, id);

    public static DOM.Attribute content(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("content", value, id);

    public static DOM.Attribute http_equiv(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("http-equiv", value, id);

    public static DOM.Attribute integrity(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("integrity", value, id);

    public static DOM.Attribute nonce(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("nonce", value, id);

    public static DOM.Attribute media(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("media", value, id);

    // =========================================================================
    // Iframe / Embed Attributes
    // =========================================================================

    public static DOM.Attribute sandbox(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("sandbox", value, id);

    public static DOM.Attribute allow(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("allow", value, id);

    // =========================================================================
    // ARIA Attributes
    // =========================================================================

    public static DOM.Attribute ariaLabel(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-label", value, id);

    public static DOM.Attribute ariaLabelledby(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-labelledby", value, id);

    public static DOM.Attribute ariaDescribedby(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-describedby", value, id);

    public static DOM.Attribute ariaHidden(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-hidden", value, id);

    public static DOM.Attribute ariaExpanded(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-expanded", value, id);

    public static DOM.Attribute ariaControls(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-controls", value, id);

    public static DOM.Attribute ariaLive(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-live", value, id);

    public static DOM.Attribute ariaAtomic(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-atomic", value, id);

    public static DOM.Attribute ariaCurrent(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-current", value, id);

    public static DOM.Attribute ariaDisabled(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-disabled", value, id);

    public static DOM.Attribute ariaSelected(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-selected", value, id);

    public static DOM.Attribute ariaChecked(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-checked", value, id);

    public static DOM.Attribute ariaValuenow(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-valuenow", value, id);

    public static DOM.Attribute ariaValuemin(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-valuemin", value, id);

    public static DOM.Attribute ariaValuemax(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("aria-valuemax", value, id);

    // =========================================================================
    // Data Attributes & Keyed Diffing
    // =========================================================================

    /// <summary>
    /// Creates a custom data-* attribute.
    /// </summary>
    /// <param name="dataName">The data attribute name (without the "data-" prefix).</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="id">Compile-time unique identifier.</param>
    public static DOM.Attribute data(string dataName, string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute($"data-{dataName}", value, id);

    /// <summary>
    /// Creates a data-key attribute for explicit keyed DOM diffing.
    /// When present, the diff algorithm uses this as the element's key
    /// instead of its compile-time ID.
    /// </summary>
    /// <param name="value">The unique key value for this element within its parent.</param>
    /// <param name="id">Compile-time unique identifier.</param>
    public static DOM.Attribute key(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("data-key", value, id);

    // =========================================================================
    // Miscellaneous Attributes
    // =========================================================================

    public static DOM.Attribute is_(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("is", value, id);

    public static DOM.Attribute as_(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("as", value, id);

    public static DOM.Attribute color(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("color", value, id);

    public static DOM.Attribute start(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("start", value, id);

    public static DOM.Attribute fetchpriority(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("fetchpriority", value, id);

    public static DOM.Attribute importance(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("importance", value, id);

    public static DOM.Attribute challenge(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("challenge", value, id);

    public static DOM.Attribute fallback(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("fallback", value, id);

    public static DOM.Attribute coords(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("coords", value, id);

    public static DOM.Attribute shape(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("shape", value, id);

    public static DOM.Attribute wrap(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("wrap", value, id);

    public static DOM.Attribute rows(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("rows", value, id);

    public static DOM.Attribute cols(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("cols", value, id);

    public static DOM.Attribute cite_(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("cite", value, id);

    public static DOM.Attribute datetime(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("datetime", value, id);

    // =========================================================================
    // SVG Attributes
    // =========================================================================

    public static DOM.Attribute viewBox(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("viewBox", value, id);

    public static DOM.Attribute xmlns(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("xmlns", value, id);

    public static DOM.Attribute fill(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("fill", value, id);

    public static DOM.Attribute stroke(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke", value, id);

    public static DOM.Attribute strokeWidth(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke-width", value, id);

    public static DOM.Attribute strokeLinecap(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke-linecap", value, id);

    public static DOM.Attribute strokeLinejoin(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke-linejoin", value, id);

    public static DOM.Attribute strokeDasharray(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke-dasharray", value, id);

    public static DOM.Attribute strokeDashoffset(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke-dashoffset", value, id);

    public static DOM.Attribute strokeOpacity(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("stroke-opacity", value, id);

    public static DOM.Attribute fillOpacity(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("fill-opacity", value, id);

    public static DOM.Attribute fillRule(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("fill-rule", value, id);

    public static DOM.Attribute clipRule(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("clip-rule", value, id);

    public static DOM.Attribute opacity(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("opacity", value, id);

    public static DOM.Attribute transform(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("transform", value, id);

    public static DOM.Attribute d(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("d", value, id);

    public static DOM.Attribute points(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("points", value, id);

    public static DOM.Attribute cx(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("cx", value, id);

    public static DOM.Attribute cy(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("cy", value, id);

    public static DOM.Attribute r(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("r", value, id);

    public static DOM.Attribute rx(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("rx", value, id);

    public static DOM.Attribute ry(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("ry", value, id);

    public static DOM.Attribute x(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("x", value, id);

    public static DOM.Attribute y(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("y", value, id);

    public static DOM.Attribute x1(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("x1", value, id);

    public static DOM.Attribute y1(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("y1", value, id);

    public static DOM.Attribute x2(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("x2", value, id);

    public static DOM.Attribute y2(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("y2", value, id);

    public static DOM.Attribute dx(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("dx", value, id);

    public static DOM.Attribute dy(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("dy", value, id);

    public static DOM.Attribute textAnchor(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("text-anchor", value, id);

    public static DOM.Attribute dominantBaseline(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("dominant-baseline", value, id);

    public static DOM.Attribute gradientUnits(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("gradientUnits", value, id);

    public static DOM.Attribute patternUnits(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("patternUnits", value, id);

    public static DOM.Attribute spreadMethod(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("spreadMethod", value, id);

    public static DOM.Attribute preserveAspectRatio(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("preserveAspectRatio", value, id);

    public static DOM.Attribute xlinkHref(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("xlink:href", value, id);

    public static DOM.Attribute markerStart(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("marker-start", value, id);

    public static DOM.Attribute markerMid(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("marker-mid", value, id);

    public static DOM.Attribute markerEnd(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => attribute("marker-end", value, id);
}
