using System.Runtime.CompilerServices;
using Abies.DOM;

namespace Abies.Html;

public static class Elements
{

    public static Element element(string tag, DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => new(id.ToString(), tag, [Attributes.id(id.ToString()), .. attributes], children);

    public static Node output(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
    => element("output", attributes, children, id);

    public static Node mark(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("mark", attributes, children, id);

    public static Node template(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("template", attributes, children, id);

    public static Node slot(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("slot", attributes, children, id);

    public static Node picture(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("picture", attributes, children, id);

    public static Node source(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("source", attributes, Array.Empty<Node>(), id);

    public static Node track(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("track", attributes, Array.Empty<Node>(), id);

    public static Node wbr(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("wbr", attributes, Array.Empty<Node>(), id);

    // Obsolete but sometimes still found
    public static Node menu(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("menu", attributes, children, id);

    public static Node menuitem(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("menuitem", attributes, children, id);


    public static Node data(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("data", attributes, children, id);


    // More SVG elements
    public static Node svg(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("svg", attributes, children, id);

    public static Node line(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("line", attributes, Array.Empty<Node>(), id);

    public static Node polyline(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("polyline", attributes, Array.Empty<Node>(), id);

    public static Node polygon(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("polygon", attributes, Array.Empty<Node>(), id);

    public static Node g(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("g", attributes, children, id);

    public static Node defs(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("defs", attributes, children, id);

    public static Node use(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("use", attributes, Array.Empty<Node>(), id);

    public static Node stop(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("stop", attributes, Array.Empty<Node>(), id);

    public static Node linearGradient(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("linearGradient", attributes, children, id);

    public static Node radialGradient(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("radialGradient", attributes, children, id);

    public static Node mask(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("mask", attributes, children, id);

    public static Node clipPath(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("clipPath", attributes, children, id);

    public static Node a(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("a", attributes, children, id);

    public static Node div(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("div", attributes, children, id);

    public static Node button(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("button", attributes, children, id);

    public static Node text(string value, [CallerLineNumber] int id = 0)
        => new Text(id.ToString(), value);

    public static Node h1(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("h1", attributes, children, id);

    public static Node h2(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("h2", attributes, children, id);

    public static Node h3(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("h3", attributes, children, id);

    public static Node h4(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("h4", attributes, children, id);

    public static Node h5(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("h5", attributes, children, id);

    public static Node h6(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("h6", attributes, children, id);

    public static Node p(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("p", attributes, children, id);

    public static Node span(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("span", attributes, children, id);

    public static Node img(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("img", attributes, Array.Empty<Node>(), id);

    public static Node input(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("input", attributes, Array.Empty<Node>(), id);

    public static Node form(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("form", attributes, children, id);

    public static Node select(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("select", attributes, children, id);

    public static Node option(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("option", attributes, children, id);

    public static Node textarea(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("textarea", attributes, children, id);

    public static Node label(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("label", attributes, children, id);

    public static Node br(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("br", attributes, Array.Empty<Node>(), id);

    public static Node hr(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("hr", attributes, Array.Empty<Node>(), id);

    public static Node ul(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("ul", attributes, children, id);

    public static Node li(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("li", attributes, children, id);

    public static Node ol(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("ol", attributes, children, id);

    public static Node table(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("table", attributes, children, id);

    public static Node tr(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("tr", attributes, children, id);

    public static Node td(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("td", attributes, children, id);

    public static Node th(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("th", attributes, children, id);

    public static Node strong(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("strong", attributes, children, id);

    public static Node em(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("em", attributes, children, id);

    public static Node small(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("small", attributes, children, id);

    public static Node code(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("code", attributes, children, id);

    public static Node pre(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("pre", attributes, children, id);

    public static Node blockquote(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("blockquote", attributes, children, id);

    public static Node nav(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("nav", attributes, children, id);

    public static Node section(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("section", attributes, children, id);

    public static Node article(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("article", attributes, children, id);

    public static Node aside(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("aside", attributes, children, id);

    public static Node footer(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("footer", attributes, children, id);

    public static Node header(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("header", attributes, children, id);

    public static Node main(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("main", attributes, children, id);

    public static Node details(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("details", attributes, children, id);

    public static Node summary(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("summary", attributes, children, id);

    public static Node dialog(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("dialog", attributes, children, id);

    public static Node progress(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("progress", attributes, children, id);

    public static Node meter(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("meter", attributes, children, id);

    public static Node canvas(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("canvas", attributes, children, id);

    public static Node video(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("video", attributes, children, id);

    public static Node audio(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("audio", attributes, children, id);

    public static Node iframe(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("iframe", attributes, children, id);

    public static Node link(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("link", attributes, children, id);

    public static Node b(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("b", attributes, children, id);

    public static Node i(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("i", attributes, children, id);

    public static Node u(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("u", attributes, children, id);

    public static Node del(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("del", attributes, children, id);

    public static Node ins(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("ins", attributes, children, id);

    public static Node abbr(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("abbr", attributes, children, id);

    public static Node cite(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("cite", attributes, children, id);

    public static Node dfn(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("dfn", attributes, children, id);

    public static Node kbd(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("kbd", attributes, children, id);

    public static Node samp(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("samp", attributes, children, id);

    public static Node var(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("var", attributes, children, id);

    public static Node sup(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("sup", attributes, children, id);

    public static Node sub(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("sub", attributes, children, id);

    public static Node bdi(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("bdi", attributes, children, id);

    public static Node bdo(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("bdo", attributes, children, id);

    public static Node q(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("q", attributes, children, id);

    public static Node time(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("time", attributes, children, id);

    public static Node address(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("address", attributes, children, id);

    public static Node figure(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("figure", attributes, children, id);

    public static Node figcaption(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("figcaption", attributes, children, id);

    public static Node fieldset(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("fieldset", attributes, children, id);

    public static Node legend(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("legend", attributes, children, id);

    public static Node datalist(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("datalist", attributes, children, id);

    public static Node optgroup(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("optgroup", attributes, children, id);

    public static Node map(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("map", attributes, children, id);

    public static Node area(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("area", attributes, Array.Empty<Node>(), id);

    public static Node @object(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("object", attributes, children, id);

    public static Node param(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("param", attributes, Array.Empty<Node>(), id);

    public static Node embed(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("embed", attributes, Array.Empty<Node>(), id);

    public static Node script(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("script", attributes, children, id);

    public static Node style(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("style", attributes, children, id);

    public static Node title(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("title", attributes, children, id);

    public static Node meta(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("meta", attributes, Array.Empty<Node>(), id);

    public static Node @base(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("base", attributes, Array.Empty<Node>(), id);

    public static Node caption(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("caption", attributes, children, id);

    public static Node col(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("col", attributes, Array.Empty<Node>(), id);

    public static Node colgroup(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("colgroup", attributes, children, id);

    public static Node thead(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("thead", attributes, children, id);

    public static Node tbody(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("tbody", attributes, children, id);

    public static Node tfoot(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("tfoot", attributes, children, id);

    public static Node ruby(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("ruby", attributes, children, id);

    public static Node rt(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("rt", attributes, children, id);

    public static Node rp(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("rp", attributes, children, id);

    public static Node portal(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("portal", attributes, children, id);

    public static Node noscript(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("noscript", attributes, children, id);

    public static Node html(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("html", attributes, children, id);

    public static Node head(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("head", attributes, children, id);

    public static Node body(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("body", attributes, children, id);

    // HTML5 semantic elements

    public static Node hgroup(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("hgroup", attributes, children, id);

    // Canvas and SVG-related
    public static Node path(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("path", attributes, Array.Empty<Node>(), id);

    public static Node circle(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("circle", attributes, Array.Empty<Node>(), id);

    public static Node rect(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("rect", attributes, Array.Empty<Node>(), id);

}

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


    public static DOM.Attribute className(string value, [CallerLineNumber] int id = 0)
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
}

public static class Events
{

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

    public static Handler onwaiting(Message command, [CallerLineNumber] int id = 0)
        => on("waiting", command, id);

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