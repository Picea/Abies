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

    public static Result<Message[], Message> Decide(TestModel _, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(TestModel _) => false;

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

    [Test]
    public async Task Render_ProducesCompleteHtmlDocument()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).StartsWith("<!DOCTYPE html>");
        await Assert.That(html).Contains("<html>");
        await Assert.That(html).Contains("</html>");
        await Assert.That(html).Contains("<head>");
        await Assert.That(html).Contains("</head>");
        await Assert.That(html).Contains("<body>");
        await Assert.That(html).Contains("</body>");
    }

    [Test]
    public async Task Render_IncludesCharsetAndViewportMeta()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).Contains("""<meta charset="utf-8">""");
        await Assert.That(html).Contains("""<meta name="viewport" content="width=device-width, initial-scale=1">""");
    }

    // =========================================================================
    // Title Tests
    // =========================================================================

    [Test]
    public async Task Render_IncludesDocumentTitle()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).Contains("<title>Test Counter</title>");
    }

    // =========================================================================
    // Head Content Tests
    // =========================================================================

    [Test]
    public async Task Render_IncludesHeadElements()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).Contains("""<meta name="description" content="A test counter app" data-abies-head="meta:description">""");
        await Assert.That(html).Contains("""<link rel="stylesheet" href="/styles.css" data-abies-head="link:stylesheet:/styles.css">""");
    }

    // =========================================================================
    // Body Content Tests
    // =========================================================================

    [Test]
    public async Task Render_IncludesBodyContent()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).Contains("Count: 0");
        await Assert.That(html).Contains("Page: home");
    }

    [Test]
    public async Task Render_BodyUsesRenderHtml()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        // Verify the body HTML contains proper element structure from Render.Html()
        await Assert.That(html).Contains("""class="counter""");
        await Assert.That(html).Contains("<h1");
    }

    // =========================================================================
    // Render Mode: Static — No Scripts
    // =========================================================================

    [Test]
    public async Task Render_StaticMode_NoScripts()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).DoesNotContain("<script");
    }

    // =========================================================================
    // Render Mode: InteractiveServer — WebSocket Script
    // =========================================================================

    [Test]
    public async Task Render_InteractiveServerMode_IncludesWebSocketScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveServer());

        await Assert.That(html).Contains("abies-server.js");
        await Assert.That(html).Contains("""data-ws-path="/_abies/ws""");
    }

    [Test]
    public async Task Render_InteractiveServerMode_CustomWebSocketPath()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveServer(WebSocketPath: "/custom/ws"));

        await Assert.That(html).Contains("""data-ws-path="/custom/ws""");
    }

    [Test]
    public async Task Render_InteractiveServerMode_DoesNotIncludeWasmScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveServer());

        await Assert.That(html).DoesNotContain("dotnet.js");
    }

    // =========================================================================
    // Render Mode: InteractiveWasm — WASM Bootstrap Script
    // =========================================================================

    [Test]
    public async Task Render_InteractiveWasmMode_IncludesWasmScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveWasm());

        await Assert.That(html).Contains("import { dotnet } from '/_framework/dotnet.js'");
        await Assert.That(html).Contains("await dotnet.run()");
    }

    [Test]
    public async Task Render_InteractiveWasmMode_DoesNotIncludeWebSocketScript()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveWasm());

        await Assert.That(html).DoesNotContain("abies-server.js");
    }

    // =========================================================================
    // Render Mode: InteractiveAuto — Both Scripts
    // =========================================================================

    [Test]
    public async Task Render_InteractiveAutoMode_IncludesBothScripts()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.InteractiveAuto());

        await Assert.That(html).Contains("abies-server.js");
        await Assert.That(html).Contains("dotnet.js");
        await Assert.That(html).Contains("""data-auto="true""");
    }

    // =========================================================================
    // URL Routing — Initial URL Applied Before Render
    // =========================================================================

    [Test]
    public async Task Render_WithInitialUrl_RoutesBeforeRendering()
    {
        var url = Url.FromUri(new Uri("http://localhost/articles"));

        var html = Page.Render<TestCounter, TestModel, Unit>(
            new RenderMode.Static(),
            initialUrl: url);

        // The TestCounter program sets CurrentPage from the URL path
        await Assert.That(html).Contains("Page: articles");
    }

    [Test]
    public async Task Render_WithoutInitialUrl_UsesDefaultState()
    {
        var html = Page.Render<TestCounter, TestModel, Unit>(new RenderMode.Static());

        await Assert.That(html).Contains("Page: home");
    }

    // =========================================================================
    // RenderDocument — Direct Document Rendering
    // =========================================================================

    [Test]
    public async Task RenderDocument_ProducesValidHtml()
    {
        var document = new Document("Direct Test",
            div([], [text("Hello")]));

        var html = Page.RenderDocument(document, new RenderMode.Static());

        await Assert.That(html).Contains("<title>Direct Test</title>");
        await Assert.That(html).Contains("Hello");
    }

    [Test]
    public async Task RenderDocument_EscapesTitleSpecialChars()
    {
        var document = new Document("Title <with> & \"quotes\"",
            div([], [text("body")]));

        var html = Page.RenderDocument(document, new RenderMode.Static());

        await Assert.That(html).Contains("Title &lt;with&gt; &amp; &quot;quotes&quot;");
    }

    [Test]
    public async Task RenderDocument_WithHeadContent_RendersInHead()
    {
        var document = new Document("Test",
            div([], [text("body")]),
            Head.canonical("https://example.com/page"),
            Head.og("title", "My Page"));

        var html = Page.RenderDocument(document, new RenderMode.Static());

        await Assert.That(html).Contains("""<link rel="canonical" href="https://example.com/page""");
        await Assert.That(html).Contains("""<meta property="og:title" content="My Page""");
    }
}
