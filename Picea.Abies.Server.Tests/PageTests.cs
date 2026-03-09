// =============================================================================
// Page Rendering Tests — Static SSR
// =============================================================================
// Tests the pure Page.Render() and Page.RenderDocument() functions.
// Uses a minimal Counter program defined inline to verify that:
//
//   1. A full HTML page is produced with DOCTYPE, html, head, body
//   2. Head elements (title, meta, stylesheets) are rendered
//   3. Body content is rendered via Render.Html()
//   4. Render modes inject the correct bootstrap scripts
//   5. URL routing is applied before rendering
//
// No server dependencies, no browser, no I/O — pure computation tests.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Picea;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Server.Tests;

// ── Test Program: Counter ────────────────────────────────────────────────────────
// A minimal Counter program for testing page rendering.
// Duplicated here to avoid a dependency on Abies.Counter.Wasm.

public record TestModel(int Count, string CurrentPage);

public interface TestMessage : Message;
public record Increment : TestMessage;
public record Decrement : TestMessage;

public sealed class TestCounter : Program<TestModel, Unit>
{
    public static (TestModel, Command) Initialize(Unit argument) =>
        (new TestModel(0, "home"), Commands.None);

    public static (TestModel, Command) Transition(TestModel model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            UrlChanged url => (model with
            {
                CurrentPage = url.Url.Path.Count > 0 ? url.Url.Path[0] : "home"
            }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(TestModel model) =>
        new("Test Counter",
            div([class_("counter")],
            [
                h1([], [text($"Count: {model.Count}")]),
                p([], [text($"Page: {model.CurrentPage}")])
            ]),
            Head.meta("description", "A test counter app"),
            Head.stylesheet("/styles.css"));

    public static Subscription Subscriptions(TestModel model) =>
        new Subscription.None();
}

// ── Tests ────────────────────────────────────────────────────────────────────────

public class PageTests
{
    // =========================================================================
    // Structure Tests — DOCTYPE, html, head, body
    // =========================================================================

    [Fact]
    public void Render_ProducesCompleteHtmlDocument()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("<head>", html);
        Assert.Contains("</head>", html);
        Assert.Contains("<body>", html);
        Assert.Contains("</body>", html);
    }

    [Fact]
    public void Render_IncludesCharsetAndViewportMeta()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.Contains("""<meta charset="utf-8">""", html);
        Assert.Contains("""<meta name="viewport" content="width=device-width, initial-scale=1">""", html);
    }

    // =========================================================================
    // Title Tests
    // =========================================================================

    [Fact]
    public void Render_IncludesDocumentTitle()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.Contains("<title>Test Counter</title>", html);
    }

    // =========================================================================
    // Head Content Tests
    // =========================================================================

    [Fact]
    public void Render_IncludesHeadElements()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.Contains("""<meta name="description" content="A test counter app" data-abies-head="meta:description">""", html);
        Assert.Contains("""<link rel="stylesheet" href="/styles.css" data-abies-head="link:stylesheet:/styles.css">""", html);
    }

    // =========================================================================
    // Body Content Tests
    // =========================================================================

    [Fact]
    public void Render_IncludesBodyContent()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.Contains("Count: 0", html);
        Assert.Contains("Page: home", html);
    }

    [Fact]
    public void Render_BodyUsesRenderHtml()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        // Verify the body HTML contains proper element structure from Render.Html()
        Assert.Contains("""class="counter""", html);
        Assert.Contains("<h1", html);
    }

    // =========================================================================
    // Render Mode: Static — No Scripts
    // =========================================================================

    [Fact]
    public void Render_StaticMode_NoScripts()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.DoesNotContain("<script", html);
    }

    // =========================================================================
    // Render Mode: InteractiveServer — WebSocket Script
    // =========================================================================

    [Fact]
    public void Render_InteractiveServerMode_IncludesWebSocketScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveServer());

        Assert.Contains("abies-server.js", html);
        Assert.Contains("""data-ws-path="/_abies/ws""", html);
    }

    [Fact]
    public void Render_InteractiveServerMode_CustomWebSocketPath()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveServer(WebSocketPath: "/custom/ws"));

        Assert.Contains("""data-ws-path="/custom/ws""", html);
    }

    [Fact]
    public void Render_InteractiveServerMode_DoesNotIncludeWasmScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveServer());

        Assert.DoesNotContain("dotnet.js", html);
    }

    // =========================================================================
    // Render Mode: InteractiveWasm — WASM Bootstrap Script
    // =========================================================================

    [Fact]
    public void Render_InteractiveWasmMode_IncludesWasmScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveWasm());

        Assert.Contains("import { dotnet } from '/_framework/dotnet.js'", html);
        Assert.Contains("await dotnet.run()", html);
    }

    [Fact]
    public void Render_InteractiveWasmMode_DoesNotIncludeWebSocketScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveWasm());

        Assert.DoesNotContain("abies-server.js", html);
    }

    // =========================================================================
    // Render Mode: InteractiveAuto — Both Scripts
    // =========================================================================

    [Fact]
    public void Render_InteractiveAutoMode_IncludesBothScripts()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveAuto());

        Assert.Contains("abies-server.js", html);
        Assert.Contains("dotnet.js", html);
        Assert.Contains("""data-auto="true""", html);
    }

    // =========================================================================
    // URL Routing — Initial URL Applied Before Render
    // =========================================================================

    [Fact]
    public void Render_WithInitialUrl_RoutesBeforeRendering()
    {
        var url = Url.FromUri(new Uri("http://localhost/articles"));

        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.Static(),
            initialUrl: url);

        // The TestCounter program sets CurrentPage from the URL path
        Assert.Contains("Page: articles", html);
    }

    [Fact]
    public void Render_WithoutInitialUrl_UsesDefaultState()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        Assert.Contains("Page: home", html);
    }

    // =========================================================================
    // RenderDocument — Direct Document Rendering
    // =========================================================================

    [Fact]
    public void RenderDocument_ProducesValidHtml()
    {
        var document = new Document("Direct Test",
            div([], [text("Hello")]));

        var html = Page.RenderDocument(document, new RenderMode.Static());

        Assert.Contains("<title>Direct Test</title>", html);
        Assert.Contains("Hello", html);
    }

    [Fact]
    public void RenderDocument_EscapesTitleSpecialChars()
    {
        var document = new Document("Title <with> & \"quotes\"",
            div([], [text("body")]));

        var html = Page.RenderDocument(document, new RenderMode.Static());

        Assert.Contains("Title &lt;with&gt; &amp; &quot;quotes&quot;", html);
    }

    [Fact]
    public void RenderDocument_WithHeadContent_RendersInHead()
    {
        var document = new Document("Test",
            div([], [text("body")]),
            Head.canonical("https://example.com/page"),
            Head.og("title", "My Page"));

        var html = Page.RenderDocument(document, new RenderMode.Static());

        Assert.Contains("""<link rel="canonical" href="https://example.com/page""", html);
        Assert.Contains("""<meta property="og:title" content="My Page""", html);
    }
}
