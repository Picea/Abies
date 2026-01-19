using System.Runtime.CompilerServices;
using Abies.DOM;
using Praefixum;

namespace Abies.Html;

public static class Elements
{
    // Math-related and scientific content elements
    public static Node math(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("math", attributes, children, id);

    public static Node var(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("var", attributes, children, id);

    // Less common but valid elements
    public static Node s(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("s", attributes, children, id);

    public static Node rb(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rb", attributes, children, id);

    public static Node rtc(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rtc", attributes, children, id);

    // Additional SVG elements
    public static Node desc(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("desc", attributes, children, id);

    public static Node ellipse(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ellipse", attributes, [], id);

    public static Node text(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("text", attributes, children, id);

    public static Node tspan(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tspan", attributes, children, id);

    public static Node filter(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("filter", attributes, children, id);

    public static Node feBlend(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feBlend", attributes, [], id);

    public static Node feColorMatrix(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feColorMatrix", attributes, [], id);

    public static Node feComponentTransfer(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feComponentTransfer", attributes, children, id);

    public static Node feComposite(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feComposite", attributes, [], id);

    public static Node feConvolveMatrix(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feConvolveMatrix", attributes, [], id);

    public static Node feDiffuseLighting(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feDiffuseLighting", attributes, children, id);

    public static Node feDisplacementMap(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feDisplacementMap", attributes, [], id);

    public static Node feFlood(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feFlood", attributes, [], id);

    public static Node feGaussianBlur(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feGaussianBlur", attributes, [], id);

    public static Node feImage(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feImage", attributes, [], id);

    public static Node feMerge(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feMerge", attributes, children, id);

    public static Node feMergeNode(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feMergeNode", attributes, [], id);

    public static Node feMorphology(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feMorphology", attributes, [], id);

    public static Node feOffset(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feOffset", attributes, [], id);

    public static Node feSpecularLighting(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feSpecularLighting", attributes, children, id);

    public static Node feTile(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feTile", attributes, [], id);

    public static Node feTurbulence(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feTurbulence", attributes, [], id);

    public static Node symbol(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("symbol", attributes, children, id);

    public static Node pattern(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("pattern", attributes, children, id);

    public static Element element(string tag, DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new(id.ToString(), tag, [Attributes.id(id.ToString()), .. attributes], children);

    public static Node output(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    => element("output", attributes, children, id);

    public static Node mark(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("mark", attributes, children, id);

    public static Node template(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("template", attributes, children, id);

    public static Node slot(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("slot", attributes, children, id);

    public static Node picture(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("picture", attributes, children, id);

    public static Node source(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("source", attributes, [], id);

    public static Node track(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("track", attributes, [], id);

    public static Node wbr(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("wbr", attributes, [], id);

    // Obsolete but sometimes still found
    public static Node menu(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("menu", attributes, children, id);

    public static Node menuitem(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("menuitem", attributes, children, id);


    public static Node data(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("data", attributes, children, id);


    // More SVG elements
    public static Node svg(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("svg", attributes, children, id);

    public static Node line(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("line", attributes, [], id);

    public static Node polyline(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("polyline", attributes, [], id);

    public static Node polygon(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("polygon", attributes, [], id);

    public static Node g(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("g", attributes, children, id);

    public static Node defs(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("defs", attributes, children, id);

    public static Node use(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("use", attributes, [], id);

    public static Node stop(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("stop", attributes, [], id);

    public static Node linearGradient(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("linearGradient", attributes, children, id);

    public static Node radialGradient(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("radialGradient", attributes, children, id);

    public static Node mask(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("mask", attributes, children, id);

    public static Node clipPath(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("clipPath", attributes, children, id);

    public static Node a(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("a", attributes, children, id);

    public static Node div(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("div", attributes, children, id);

    public static Node button(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("button", attributes, children, id);

    public static Node text(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new Text(id.ToString(), value);

    public static Node h1(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h1", attributes, children, id);

    public static Node h2(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h2", attributes, children, id);

    public static Node h3(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h3", attributes, children, id);

    public static Node h4(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h4", attributes, children, id);

    public static Node h5(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h5", attributes, children, id);

    public static Node h6(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h6", attributes, children, id);

    public static Node p(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("p", attributes, children, id);

    public static Node span(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("span", attributes, children, id);

    public static Node img(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("img", attributes, [], id);

    public static Node input(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("input", attributes, [], id);

    public static Node form(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("form", attributes, children, id);

    public static Node select(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("select", attributes, children, id);

    public static Node option(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("option", attributes, children, id);

    public static Node textarea(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("textarea", attributes, children, id);

    public static Node label(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("label", attributes, children, id);

    public static Node br(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("br", attributes, [], id);

    public static Node hr(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("hr", attributes, [], id);

    public static Node ul(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ul", attributes, children, id);

    public static Node li(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("li", attributes, children, id);

    public static Node ol(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ol", attributes, children, id);

    public static Node table(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("table", attributes, children, id);

    public static Node tr(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tr", attributes, children, id);

    public static Node td(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("td", attributes, children, id);

    public static Node th(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("th", attributes, children, id);

    public static Node strong(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("strong", attributes, children, id);

    public static Node em(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("em", attributes, children, id);

    public static Node small(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("small", attributes, children, id);

    public static Node code(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("code", attributes, children, id);

    public static Node pre(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("pre", attributes, children, id);

    public static Node blockquote(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("blockquote", attributes, children, id);

    public static Node nav(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("nav", attributes, children, id);

    public static Node section(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("section", attributes, children, id);

    public static Node article(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("article", attributes, children, id);

    public static Node aside(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("aside", attributes, children, id);

    public static Node footer(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("footer", attributes, children, id);

    public static Node header(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("header", attributes, children, id);

    public static Node main(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("main", attributes, children, id);

    public static Node details(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("details", attributes, children, id);

    public static Node summary(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("summary", attributes, children, id);

    public static Node dialog(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dialog", attributes, children, id);

    public static Node progress(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("progress", attributes, children, id);

    public static Node meter(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("meter", attributes, children, id);

    public static Node canvas(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("canvas", attributes, children, id);

    public static Node video(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("video", attributes, children, id);

    public static Node audio(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("audio", attributes, children, id);

    public static Node iframe(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("iframe", attributes, children, id);

    public static Node link(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("link", attributes, children, id);

    public static Node b(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("b", attributes, children, id);

    public static Node i(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("i", attributes, children, id);

    public static Node u(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("u", attributes, children, id);

    public static Node del(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("del", attributes, children, id);

    public static Node ins(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ins", attributes, children, id);

    public static Node abbr(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("abbr", attributes, children, id);

    public static Node cite(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("cite", attributes, children, id);

    public static Node dfn(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dfn", attributes, children, id);

    public static Node kbd(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("kbd", attributes, children, id);

    public static Node samp(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("samp", attributes, children, id);


    public static Node sup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("sup", attributes, children, id);

    public static Node sub(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("sub", attributes, children, id);

    public static Node bdi(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("bdi", attributes, children, id);

    public static Node bdo(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("bdo", attributes, children, id);

    public static Node q(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("q", attributes, children, id);

    public static Node time(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("time", attributes, children, id);

    public static Node address(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("address", attributes, children, id);

    public static Node figure(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("figure", attributes, children, id);

    public static Node figcaption(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("figcaption", attributes, children, id);

    public static Node fieldset(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("fieldset", attributes, children, id);

    public static Node legend(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("legend", attributes, children, id);

    public static Node datalist(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("datalist", attributes, children, id);

    public static Node optgroup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("optgroup", attributes, children, id);

    public static Node map(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("map", attributes, children, id);

    public static Node area(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("area", attributes, [], id);

    public static Node object_(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("object", attributes, children, id);

    public static Node param(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("param", attributes, [], id);

    public static Node embed(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("embed", attributes, [], id);

    public static Node script(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("script", attributes, children, id);

    public static Node style(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("style", attributes, children, id);

    public static Node title(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("title", attributes, children, id);

    public static Node meta(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("meta", attributes, [], id);

    public static Node @base(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("base", attributes, [], id);

    public static Node caption(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("caption", attributes, children, id);

    public static Node col(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("col", attributes, [], id);

    public static Node colgroup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("colgroup", attributes, children, id);

    public static Node thead(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("thead", attributes, children, id);

    public static Node tbody(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tbody", attributes, children, id);

    public static Node tfoot(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tfoot", attributes, children, id);

    public static Node ruby(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ruby", attributes, children, id);

    public static Node rt(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rt", attributes, children, id);

    public static Node rp(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rp", attributes, children, id);

    public static Node portal(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("portal", attributes, children, id);

    public static Node noscript(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("noscript", attributes, children, id);

    public static Node html(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("html", attributes, children, id);

    public static Node head(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("head", attributes, children, id);

    public static Node body(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("body", attributes, children, id);

    // HTML5 semantic elements

    public static Node hgroup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("hgroup", attributes, children, id);

    // Canvas and SVG-related
    public static Node path(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("path", attributes, [], id);

    public static Node circle(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("circle", attributes, [], id);

    public static Node rect(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rect", attributes, [], id);

    public static Node raw(string html, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new DOM.RawHtml(id.ToString(), html);

}
