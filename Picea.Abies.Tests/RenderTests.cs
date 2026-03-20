// =============================================================================
// Render Tests — HTML Rendering from Virtual DOM
// =============================================================================
// Tests the pure Render.Html() and Render.HtmlChildren() functions.
// These are pure functions with no browser dependencies.
//
// Coverage:
//   * Simple elements with attributes
//   * Nested elements
//   * Text nodes (inline, HTML-encoded)
//   * Raw HTML pass-through
//   * Void elements (no closing tag per HTML spec 13.1.2)
//   * Boolean attributes (bare name per HTML spec)
//   * Event handlers (data-event-{name}="{commandId}")
//   * Memo and LazyMemo unwrapping
//   * HtmlChildren batch rendering
// =============================================================================

using Picea.Abies.DOM;
using Attribute = Picea.Abies.DOM.Attribute;

namespace Picea.Abies.Tests;

[NotInParallel("shared-dom-state")]
public class RenderTests
{
    // =========================================================================
    // Simple Elements
    // =========================================================================

    [Test]
    public async Task Html_EmptyDiv_RendersOpenAndCloseTag()
    {
        var node = new Element("e1", "div", []);

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<div id="e1"></div>""");
    }

    [Test]
    public async Task Html_ElementWithAttribute_RendersAttribute()
    {
        var node = new Element("e1", "div", [new Attribute("a1", "class", "container")]);

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<div id="e1" class="container"></div>""");
    }

    [Test]
    public async Task Html_ElementWithMultipleAttributes_RendersAllAttributes()
    {
        var node = new Element("e1", "a",
        [
            new Attribute("a1", "href", "/home"),
            new Attribute("a2", "class", "link")
        ]);

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<a id="e1" href="/home" class="link"></a>""");
    }

    // =========================================================================
    // Nested Elements
    // =========================================================================

    [Test]
    public async Task Html_NestedElements_RendersHierarchy()
    {
        var node = new Element("e1", "div", [],
            new Element("e2", "span", [],
                new Text("t1", "hello")));

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<div id="e1"><span id="e2">hello</span></div>""");
    }

    [Test]
    public async Task Html_MultipleChildren_RendersInOrder()
    {
        var node = new Element("e1", "ul", [],
            new Element("li1", "li", [], new Text("t1", "A")),
            new Element("li2", "li", [], new Text("t2", "B")),
            new Element("li3", "li", [], new Text("t3", "C")));

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo(
            """<ul id="e1"><li id="li1">A</li><li id="li2">B</li><li id="li3">C</li></ul>""");
    }

    // =========================================================================
    // Text Nodes
    // =========================================================================

    [Test]
    public async Task Html_TextNode_RendersContent()
    {
        var node = new Element("e1", "p", [], new Text("t1", "Hello, World!"));

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<p id="e1">Hello, World!</p>""");
    }

    [Test]
    public async Task Html_TextWithSpecialChars_HtmlEncodes()
    {
        var node = new Element("e1", "p", [], new Text("t1", "<script>alert('xss')</script>"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("&lt;script&gt;");
        await Assert.That(html).DoesNotContain("<script>");
    }

    [Test]
    public async Task Html_AttributeValueWithSpecialChars_HtmlEncodes()
    {
        var node = new Element("e1", "div", [new Attribute("a1", "title", "a & b < c")]);

        var html = Render.Html(node);

        await Assert.That(html).Contains("a &amp; b &lt; c");
    }

    // =========================================================================
    // Raw HTML
    // =========================================================================

    [Test]
    public async Task Html_RawHtml_RendersInSpanWrapper()
    {
        var node = new RawHtml("r1", "<strong>bold</strong>");

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<span id="r1"><strong>bold</strong></span>""");
    }

    [Test]
    public async Task Html_RawHtml_DoesNotEncode()
    {
        var node = new RawHtml("r1", "<em>italic & fancy</em>");

        var html = Render.Html(node);

        await Assert.That(html).Contains("<em>italic & fancy</em>");
    }

    // =========================================================================
    // Void Elements (HTML Living Standard 13.1.2)
    // =========================================================================

    [Test]
    [Arguments("br")]
    [Arguments("hr")]
    [Arguments("img")]
    [Arguments("input")]
    [Arguments("meta")]
    [Arguments("link")]
    [Arguments("source")]
    [Arguments("col")]
    [Arguments("embed")]
    [Arguments("area")]
    [Arguments("track")]
    [Arguments("wbr")]
    public async Task Html_VoidElement_NoClosingTag(string tag)
    {
        var node = new Element("e1", tag, []);

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo($"""<{tag} id="e1">""");
        await Assert.That(html).DoesNotContain($"</{tag}>");
    }

    [Test]
    public async Task Html_VoidElementWithAttributes_NoClosingTag()
    {
        var node = new Element("e1", "img",
        [
            new Attribute("a1", "src", "/logo.png"),
            new Attribute("a2", "alt", "Logo")
        ]);

        var html = Render.Html(node);

        await Assert.That(html).IsEqualTo("""<img id="e1" src="/logo.png" alt="Logo">""");
    }

    // =========================================================================
    // Boolean Attributes
    // =========================================================================

    [Test]
    public async Task Html_BooleanAttributeTrue_RendersBareAttribute()
    {
        var node = new Element("e1", "input",
        [
            new Attribute("a1", "disabled", "true"),
            new Attribute("a2", "type", "text")
        ]);

        var html = Render.Html(node);

        await Assert.That(html).Contains(" disabled");
        await Assert.That(html).DoesNotContain("""disabled="true""");
        await Assert.That(html).Contains("""type="text""");
    }

    [Test]
    public async Task Html_BooleanAttributeEmpty_RendersBareAttribute()
    {
        var node = new Element("e1", "input", [new Attribute("a1", "required", "")]);

        var html = Render.Html(node);

        await Assert.That(html).Contains(" required");
        await Assert.That(html).DoesNotContain("""required=""" + "\"");
    }

    [Test]
    public async Task Html_BooleanAttributeFalse_RendersNormally()
    {
        var node = new Element("e1", "input", [new Attribute("a1", "disabled", "false")]);

        var html = Render.Html(node);

        // "false" is not a boolean-true value, so it renders as normal attribute
        await Assert.That(html).Contains("""disabled="false""");
    }

    // =========================================================================
    // Event Handlers
    // =========================================================================

    [Test]
    public async Task Html_Handler_RendersAsDataEventAttribute()
    {
        var handler = new Handler("click", "cmd-1", null, "h1");
        var node = new Element("e1", "button", [handler], new Text("t1", "Click me"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("""data-event-click="cmd-1""");
    }

    [Test]
    public async Task Html_MultipleHandlers_RendersAll()
    {
        var click = new Handler("click", "cmd-1", null, "h1");
        var input = new Handler("input", "cmd-2", null, "h2");
        var node = new Element("e1", "input", [click, input]);

        var html = Render.Html(node);

        await Assert.That(html).Contains("""data-event-click="cmd-1""");
        await Assert.That(html).Contains("""data-event-input="cmd-2""");
    }

    [Test]
    public async Task Html_HandlerNameProperty_ReturnsFullAttributeName()
    {
        // This test documents the C# record inheritance behavior that caused
        // the double-prefix bug documented in memory.instructions.md.
        var handler = new Handler("click", "cmd-1", null, "h1");

        await Assert.That(handler.Name).IsEqualTo("data-event-click");
        await Assert.That(handler.EventName).IsEqualTo("click");
    }

    // =========================================================================
    // Memo and LazyMemo
    // =========================================================================

    [Test]
    public async Task Html_MemoNode_RendersUnwrappedContent()
    {
        var inner = new Element("e1", "div", [], new Text("t1", "cached"));
        var memo = new Memo<int>("m1", 42, inner);

        var html = Render.Html(memo);

        await Assert.That(html).IsEqualTo("""<div id="e1">cached</div>""");
    }

    [Test]
    public async Task Html_LazyMemoNode_EvaluatesAndRenders()
    {
        var inner = new Element("e1", "span", [], new Text("t1", "lazy"));
        var lazy = new LazyMemo<string>("l1", "key", () => inner);

        var html = Render.Html(lazy);

        await Assert.That(html).IsEqualTo("""<span id="e1">lazy</span>""");
    }

    // =========================================================================
    // HtmlChildren — Batch Rendering
    // =========================================================================

    [Test]
    public async Task HtmlChildren_MultipleElements_ConcatenatesHtml()
    {
        var children = new Node[]
        {
            new Element("li1", "li", [], new Text("t1", "A")),
            new Element("li2", "li", [], new Text("t2", "B")),
            new Element("li3", "li", [], new Text("t3", "C"))
        };

        var html = Render.HtmlChildren(children);

        await Assert.That(html).IsEqualTo(
            """<li id="li1">A</li><li id="li2">B</li><li id="li3">C</li>""");
    }

    [Test]
    public async Task HtmlChildren_EmptyArray_ReturnsEmpty()
    {
        var html = Render.HtmlChildren([]);

        await Assert.That(html).IsEqualTo("");
    }

    [Test]
    public async Task HtmlChildren_WithMemoNodes_UnwrapsAndRenders()
    {
        var inner = new Element("e1", "p", [], new Text("t1", "memoized"));
        var children = new Node[]
        {
            new Memo<int>("m1", 1, inner)
        };

        var html = Render.HtmlChildren(children);

        await Assert.That(html).IsEqualTo("""<p id="e1">memoized</p>""");
    }
}
