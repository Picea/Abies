using System.Runtime.CompilerServices;
using Abies.DOM;

namespace Abies.Html;

public static class Elements
{
    // Math-related and scientific content elements
    public static Node math(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("math", attributes, children, id);

    public static Node var(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("var", attributes, children, id);

    // Less common but valid elements
    public static Node s(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("s", attributes, children, id);

    public static Node rb(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("rb", attributes, children, id);

    public static Node rtc(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("rtc", attributes, children, id);

    // Additional SVG elements
    public static Node desc(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("desc", attributes, children, id);

    public static Node ellipse(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("ellipse", attributes, Array.Empty<Node>(), id);

    public static Node text(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("text", attributes, children, id);

    public static Node tspan(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("tspan", attributes, children, id);

    public static Node filter(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("filter", attributes, children, id);

    public static Node feBlend(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feBlend", attributes, Array.Empty<Node>(), id);

    public static Node feColorMatrix(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feColorMatrix", attributes, Array.Empty<Node>(), id);

    public static Node feComponentTransfer(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("feComponentTransfer", attributes, children, id);

    public static Node feComposite(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feComposite", attributes, Array.Empty<Node>(), id);

    public static Node feConvolveMatrix(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feConvolveMatrix", attributes, Array.Empty<Node>(), id);

    public static Node feDiffuseLighting(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("feDiffuseLighting", attributes, children, id);

    public static Node feDisplacementMap(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feDisplacementMap", attributes, Array.Empty<Node>(), id);

    public static Node feFlood(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feFlood", attributes, Array.Empty<Node>(), id);

    public static Node feGaussianBlur(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feGaussianBlur", attributes, Array.Empty<Node>(), id);

    public static Node feImage(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feImage", attributes, Array.Empty<Node>(), id);

    public static Node feMerge(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("feMerge", attributes, children, id);

    public static Node feMergeNode(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feMergeNode", attributes, Array.Empty<Node>(), id);

    public static Node feMorphology(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feMorphology", attributes, Array.Empty<Node>(), id);

    public static Node feOffset(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feOffset", attributes, Array.Empty<Node>(), id);

    public static Node feSpecularLighting(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("feSpecularLighting", attributes, children, id);

    public static Node feTile(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feTile", attributes, Array.Empty<Node>(), id);

    public static Node feTurbulence(DOM.Attribute[] attributes, [CallerLineNumber] int id = 0)
        => element("feTurbulence", attributes, Array.Empty<Node>(), id);

    public static Node symbol(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("symbol", attributes, children, id);

    public static Node pattern(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
        => element("pattern", attributes, children, id);

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
