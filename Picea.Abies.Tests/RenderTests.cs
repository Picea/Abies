// =============================================================================
// Render Tests — HTML Rendering from Virtual DOM
// =============================================================================
// Tests the pure Render.Html() and Render.HtmlChildren() functions.
// These are pure functions with no browser dependencies.
//
// Coverage:
//   • Simple elements with attributes
//   • Nested elements
//   • Text nodes (inline, HTML-encoded)
//   • Raw HTML pass-through
//   • Void elements (no closing tag per HTML spec §13.1.2)
//   • Boolean attributes (bare name per HTML spec)
//   • Event handlers (data-event-{name}="{commandId}")
//   • Memo and LazyMemo unwrapping
//   • HtmlChildren batch rendering
// =============================================================================

using Picea.Abies.DOM;
using Attribute = Picea.Abies.DOM.Attribute;

namespace Picea.Abies.Tests;

public class RenderTests
{
    // =========================================================================
    // Simple Elements
    // =========================================================================

    [Fact]
    public void Html_EmptyDiv_RendersOpenAndCloseTag()
    {
        var node = new Element("e1", "div", []);

        var html = Render.Html(node);

        Assert.Equal("""<div id="e1"></div>""", html);
    }

    [Fact]
    public void Html_ElementWithAttribute_RendersAttribute()
    {
        var node = new Element("e1", "div", [new Attribute("a1", "class", "container")]);

        var html = Render.Html(node);

        Assert.Equal("""<div id="e1" class="container"></div>""", html);
    }

    [Fact]
    public void Html_ElementWithMultipleAttributes_RendersAllAttributes()
    {
        var node = new Element("e1", "a",
        [
            new Attribute("a1", "href", "/home"),
            new Attribute("a2", "class", "link")
        ]);

        var html = Render.Html(node);

        Assert.Equal("""<a id="e1" href="/home" class="link"></a>""", html);
    }

    // =========================================================================
    // Nested Elements
    // =========================================================================

    [Fact]
    public void Html_NestedElements_RendersHierarchy()
    {
        var node = new Element("e1", "div", [],
            new Element("e2", "span", [],
                new Text("t1", "hello")));

        var html = Render.Html(node);

        Assert.Equal("""<div id="e1"><span id="e2">hello</span></div>""", html);
    }

    [Fact]
    public void Html_MultipleChildren_RendersInOrder()
    {
        var node = new Element("e1", "ul", [],
            new Element("li1", "li", [], new Text("t1", "A")),
            new Element("li2", "li", [], new Text("t2", "B")),
            new Element("li3", "li", [], new Text("t3", "C")));

        var html = Render.Html(node);

        Assert.Equal(
            """<ul id="e1"><li id="li1">A</li><li id="li2">B</li><li id="li3">C</li></ul>""",
            html);
    }

    // =========================================================================
    // Text Nodes
    // =========================================================================

    [Fact]
    public void Html_TextNode_RendersContent()
    {
        var node = new Element("e1", "p", [], new Text("t1", "Hello, World!"));

        var html = Render.Html(node);

        Assert.Equal("""<p id="e1">Hello, World!</p>""", html);
    }

    [Fact]
    public void Html_TextWithSpecialChars_HtmlEncodes()
    {
        var node = new Element("e1", "p", [], new Text("t1", "<script>alert('xss')</script>"));

        var html = Render.Html(node);

        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Html_AttributeValueWithSpecialChars_HtmlEncodes()
    {
        var node = new Element("e1", "div", [new Attribute("a1", "title", "a & b < c")]);

        var html = Render.Html(node);

        Assert.Contains("a &amp; b &lt; c", html);
    }

    // =========================================================================
    // Raw HTML
    // =========================================================================

    [Fact]
    public void Html_RawHtml_RendersInSpanWrapper()
    {
        var node = new RawHtml("r1", "<strong>bold</strong>");

        var html = Render.Html(node);

        Assert.Equal("""<span id="r1"><strong>bold</strong></span>""", html);
    }

    [Fact]
    public void Html_RawHtml_DoesNotEncode()
    {
        var node = new RawHtml("r1", "<em>italic & fancy</em>");

        var html = Render.Html(node);

        Assert.Contains("<em>italic & fancy</em>", html);
    }

    // =========================================================================
    // Void Elements (HTML Living Standard §13.1.2)
    // =========================================================================

    [Theory]
    [InlineData("br")]
    [InlineData("hr")]
    [InlineData("img")]
    [InlineData("input")]
    [InlineData("meta")]
    [InlineData("link")]
    [InlineData("source")]
    [InlineData("col")]
    [InlineData("embed")]
    [InlineData("area")]
    [InlineData("track")]
    [InlineData("wbr")]
    public void Html_VoidElement_NoClosingTag(string tag)
    {
        var node = new Element("e1", tag, []);

        var html = Render.Html(node);

        Assert.Equal($"""<{tag} id="e1">""", html);
        Assert.DoesNotContain($"</{tag}>", html);
    }

    [Fact]
    public void Html_VoidElementWithAttributes_NoClosingTag()
    {
        var node = new Element("e1", "img",
        [
            new Attribute("a1", "src", "/logo.png"),
            new Attribute("a2", "alt", "Logo")
        ]);

        var html = Render.Html(node);

        Assert.Equal("""<img id="e1" src="/logo.png" alt="Logo">""", html);
    }

    // =========================================================================
    // Boolean Attributes
    // =========================================================================

    [Fact]
    public void Html_BooleanAttributeTrue_RendersBareAttribute()
    {
        var node = new Element("e1", "input",
        [
            new Attribute("a1", "disabled", "true"),
            new Attribute("a2", "type", "text")
        ]);

        var html = Render.Html(node);

        Assert.Contains(" disabled", html);
        Assert.DoesNotContain("""disabled="true""", html);
        Assert.Contains("""type="text""", html);
    }

    [Fact]
    public void Html_BooleanAttributeEmpty_RendersBareAttribute()
    {
        var node = new Element("e1", "input", [new Attribute("a1", "required", "")]);

        var html = Render.Html(node);

        Assert.Contains(" required", html);
        Assert.DoesNotContain("""required=""" + "\"", html);
    }

    [Fact]
    public void Html_BooleanAttributeFalse_RendersNormally()
    {
        var node = new Element("e1", "input", [new Attribute("a1", "disabled", "false")]);

        var html = Render.Html(node);

        // "false" is not a boolean-true value, so it renders as normal attribute
        Assert.Contains("""disabled="false""", html);
    }

    // =========================================================================
    // Event Handlers
    // =========================================================================

    [Fact]
    public void Html_Handler_RendersAsDataEventAttribute()
    {
        var handler = new Handler("click", "cmd-1", null, "h1");
        var node = new Element("e1", "button", [handler], new Text("t1", "Click me"));

        var html = Render.Html(node);

        Assert.Contains("""data-event-click="cmd-1""", html);
    }

    [Fact]
    public void Html_MultipleHandlers_RendersAll()
    {
        var click = new Handler("click", "cmd-1", null, "h1");
        var input = new Handler("input", "cmd-2", null, "h2");
        var node = new Element("e1", "input", [click, input]);

        var html = Render.Html(node);

        Assert.Contains("""data-event-click="cmd-1""", html);
        Assert.Contains("""data-event-input="cmd-2""", html);
    }

    [Fact]
    public void Html_HandlerNameProperty_ReturnsFullAttributeName()
    {
        // This test documents the C# record inheritance behavior that caused
        // the double-prefix bug (see memory.instructions.md TODO).
        var handler = new Handler("click", "cmd-1", null, "h1");

        Assert.Equal("data-event-click", handler.Name);
        Assert.Equal("click", handler.EventName);
    }

    // =========================================================================
    // Memo and LazyMemo
    // =========================================================================

    [Fact]
    public void Html_MemoNode_RendersUnwrappedContent()
    {
        var inner = new Element("e1", "div", [], new Text("t1", "cached"));
        var memo = new Memo<int>("m1", 42, inner);

        var html = Render.Html(memo);

        Assert.Equal("""<div id="e1">cached</div>""", html);
    }

    [Fact]
    public void Html_LazyMemoNode_EvaluatesAndRenders()
    {
        var inner = new Element("e1", "span", [], new Text("t1", "lazy"));
        var lazy = new LazyMemo<string>("l1", "key", () => inner);

        var html = Render.Html(lazy);

        Assert.Equal("""<span id="e1">lazy</span>""", html);
    }

    // =========================================================================
    // HtmlChildren — Batch Rendering
    // =========================================================================

    [Fact]
    public void HtmlChildren_MultipleElements_ConcatenatesHtml()
    {
        var children = new Node[]
        {
            new Element("li1", "li", [], new Text("t1", "A")),
            new Element("li2", "li", [], new Text("t2", "B")),
            new Element("li3", "li", [], new Text("t3", "C"))
        };

        var html = Render.HtmlChildren(children);

        Assert.Equal(
            """<li id="li1">A</li><li id="li2">B</li><li id="li3">C</li>""",
            html);
    }

    [Fact]
    public void HtmlChildren_EmptyArray_ReturnsEmpty()
    {
        var html = Render.HtmlChildren([]);

        Assert.Equal("", html);
    }

    [Fact]
    public void HtmlChildren_WithMemoNodes_UnwrapsAndRenders()
    {
        var inner = new Element("e1", "p", [], new Text("t1", "memoized"));
        var children = new Node[]
        {
            new Memo<int>("m1", 1, inner)
        };

        var html = Render.HtmlChildren(children);

        Assert.Equal("""<p id="e1">memoized</p>""", html);
    }
}
