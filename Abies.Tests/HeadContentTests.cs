using System.Runtime.Versioning;
using static Abies.HeadDiff;

namespace Abies.Tests;

[SupportedOSPlatform("browser")]
public class HeadContentTests
{
    // =========================================================================
    // HeadContent Key Generation
    // =========================================================================

    [Fact]
    public void Meta_Key_UsesNameAttribute() =>
        Assert.Equal("meta:description", new HeadContent.Meta("description", "test").Key);

    [Fact]
    public void MetaProperty_Key_UsesPropertyAttribute() =>
        Assert.Equal("property:og:title", new HeadContent.MetaProperty("og:title", "test").Key);

    [Fact]
    public void Link_Key_UsesRelAndHref() =>
        Assert.Equal("link:canonical:/about", new HeadContent.Link("canonical", "/about").Key);

    [Fact]
    public void Script_Key_UsesType() =>
        Assert.Equal("script:application/ld+json", new HeadContent.Script("application/ld+json", "{}").Key);

    [Fact]
    public void Base_Key_IsConstant() =>
        Assert.Equal("base", new HeadContent.Base("/app").Key);

    // =========================================================================
    // HeadContent HTML Rendering
    // =========================================================================

    [Fact]
    public void Meta_ToHtml_RendersCorrectly() =>
        Assert.Equal(
            """<meta name="description" content="A blog" data-abies-head="meta:description">""",
            new HeadContent.Meta("description", "A blog").ToHtml());

    [Fact]
    public void Meta_ToHtml_EncodesContent() =>
        Assert.Equal(
            """<meta name="description" content="&lt;script&gt;xss&lt;/script&gt;" data-abies-head="meta:description">""",
            new HeadContent.Meta("description", "<script>xss</script>").ToHtml());

    [Fact]
    public void MetaProperty_ToHtml_RendersCorrectly() =>
        Assert.Equal(
            """<meta property="og:title" content="My Article" data-abies-head="property:og:title">""",
            new HeadContent.MetaProperty("og:title", "My Article").ToHtml());

    [Fact]
    public void Link_ToHtml_RendersWithoutType() =>
        Assert.Equal(
            """<link rel="canonical" href="/about" data-abies-head="link:canonical:/about">""",
            new HeadContent.Link("canonical", "/about").ToHtml());

    [Fact]
    public void Link_ToHtml_RendersWithType() =>
        Assert.Equal(
            """<link rel="preload" href="/style.css" type="text/css" data-abies-head="link:preload:/style.css">""",
            new HeadContent.Link("preload", "/style.css", "text/css").ToHtml());

    [Fact]
    public void Script_ToHtml_RendersCorrectly() =>
        Assert.Equal(
            """<script type="application/ld+json" data-abies-head="script:application/ld+json">{"@type":"Article"}</script>""",
            new HeadContent.Script("application/ld+json", """{"@type":"Article"}""").ToHtml());

    [Fact]
    public void Base_ToHtml_RendersCorrectly() =>
        Assert.Equal(
            """<base href="/app/" data-abies-head="base">""",
            new HeadContent.Base("/app/").ToHtml());

    // =========================================================================
    // Head Helper Functions
    // =========================================================================

    [Fact]
    public void Head_meta_CreatesMetaContent() =>
        Assert.IsType<HeadContent.Meta>(Head.meta("description", "test"));

    [Fact]
    public void Head_og_CreatesMetaPropertyWithOgPrefix() =>
        Assert.Equal("og:title", ((HeadContent.MetaProperty)Head.og("title", "test")).Property);

    [Fact]
    public void Head_twitter_CreatesMetaWithTwitterPrefix() =>
        Assert.Equal("twitter:card", ((HeadContent.Meta)Head.twitter("card", "summary")).Name);

    [Fact]
    public void Head_canonical_CreatesCanonicalLink() =>
        Assert.Equal("canonical", ((HeadContent.Link)Head.canonical("/page")).Rel);

    [Fact]
    public void Head_stylesheet_CreatesStylesheetLink() =>
        Assert.Equal("stylesheet", ((HeadContent.Link)Head.stylesheet("/style.css")).Rel);

    [Fact]
    public void Head_preload_CreatesPreloadLink() =>
        Assert.Equal("text/css", ((HeadContent.Link)Head.preload("/font.woff", "text/css")).Type);

    [Fact]
    public void Head_jsonLd_CreatesScriptWithSerializedContent()
    {
        var content = Head.jsonLd(new { type = "Article" });
        var script = Assert.IsType<HeadContent.Script>(content);
        Assert.Equal("application/ld+json", script.Type);
        Assert.Contains("Article", script.Content);
    }

    [Fact]
    public void Head_link_CreatesCustomLink()
    {
        var content = Head.link("icon", "/favicon.ico", "image/x-icon");
        var link = Assert.IsType<HeadContent.Link>(content);
        Assert.Equal("icon", link.Rel);
        Assert.Equal("/favicon.ico", link.Href);
        Assert.Equal("image/x-icon", link.Type);
    }

    [Fact]
    public void Head_property_CreatesMetaProperty()
    {
        var content = Head.property("article:author", "John");
        var prop = Assert.IsType<HeadContent.MetaProperty>(content);
        Assert.Equal("article:author", prop.Property);
    }

    [Fact]
    public void Head_base_CreatesBase()
    {
        var content = Head.@base("/app/");
        var b = Assert.IsType<HeadContent.Base>(content);
        Assert.Equal("/app/", b.Href);
    }

    // =========================================================================
    // HeadDiff — Empty Cases
    // =========================================================================

    [Fact]
    public void Diff_EmptyToEmpty_ReturnsNoPatches() =>
        Assert.Empty(Diff([], []));

    [Fact]
    public void Diff_EmptyToSingle_ReturnsOneAdd()
    {
        HeadContent[] newHead = [Head.meta("description", "test")];
        var patches = Diff([], newHead);

        var patch = Assert.Single(patches);
        Assert.IsType<HeadPatch.Add>(patch);
    }

    [Fact]
    public void Diff_SingleToEmpty_ReturnsOneRemove()
    {
        HeadContent[] oldHead = [Head.meta("description", "test")];
        var patches = Diff(oldHead, []);

        var patch = Assert.Single(patches);
        var remove = Assert.IsType<HeadPatch.Remove>(patch);
        Assert.Equal("meta:description", remove.Key);
    }

    // =========================================================================
    // HeadDiff — Add/Update/Remove
    // =========================================================================

    [Fact]
    public void Diff_SameContent_ReturnsNoPatches()
    {
        HeadContent[] head = [
            Head.meta("description", "My Site"),
            Head.og("title", "My Site")
        ];

        Assert.Empty(Diff(head, head));
    }

    [Fact]
    public void Diff_ContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.meta("description", "Old description")];
        HeadContent[] newHead = [Head.meta("description", "New description")];

        var patches = Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var update = Assert.IsType<HeadPatch.Update>(patch);
        Assert.Equal("New description", ((HeadContent.Meta)update.Content).Content);
    }

    [Fact]
    public void Diff_AddNewElement_ReturnsAdd()
    {
        HeadContent[] oldHead = [Head.meta("description", "Site")];
        HeadContent[] newHead = [
            Head.meta("description", "Site"),
            Head.og("title", "Site")
        ];

        var patches = Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        Assert.IsType<HeadPatch.Add>(patch);
    }

    [Fact]
    public void Diff_RemoveElement_ReturnsRemove()
    {
        HeadContent[] oldHead = [
            Head.meta("description", "Site"),
            Head.og("title", "Site")
        ];
        HeadContent[] newHead = [Head.meta("description", "Site")];

        var patches = Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var remove = Assert.IsType<HeadPatch.Remove>(patch);
        Assert.Equal("property:og:title", remove.Key);
    }

    // =========================================================================
    // HeadDiff — Complex Scenarios
    // =========================================================================

    [Fact]
    public void Diff_NavigationChangesAllContent_ReturnsAddUpdateRemove()
    {
        HeadContent[] oldHead = [
            Head.meta("description", "Home page"),
            Head.canonical("/"),
            Head.og("title", "Home")
        ];
        HeadContent[] newHead = [
            Head.meta("description", "Article about cats"),
            Head.canonical("/articles/cats"),
            Head.og("type", "article")
        ];

        var patches = Diff(oldHead, newHead);

        // description: updated (same key, different content)
        Assert.Contains(patches, p => p is HeadPatch.Update u
            && u.Content.Key == "meta:description");

        // canonical: updated (same rel, different href → different key → add new + remove old)
        Assert.Contains(patches, p => p is HeadPatch.Add a
            && a.Content.Key == "link:canonical:/articles/cats");
        Assert.Contains(patches, p => p is HeadPatch.Remove r
            && r.Key == "link:canonical:/");

        // og:title removed, og:type added
        Assert.Contains(patches, p => p is HeadPatch.Remove r
            && r.Key == "property:og:title");
        Assert.Contains(patches, p => p is HeadPatch.Add a
            && a.Content.Key == "property:og:type");
    }

    [Fact]
    public void Diff_StructuredData_UpdatesJsonLd()
    {
        HeadContent[] oldHead = [Head.jsonLd(new { type = "WebSite" })];
        HeadContent[] newHead = [Head.jsonLd(new { type = "Article" })];

        var patches = Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var update = Assert.IsType<HeadPatch.Update>(patch);
        Assert.Contains("Article", ((HeadContent.Script)update.Content).Content);
    }

    [Fact]
    public void Diff_MultipleStylesheets_TrackedIndependently()
    {
        HeadContent[] oldHead = [
            Head.stylesheet("/base.css"),
            Head.stylesheet("/theme-light.css")
        ];
        HeadContent[] newHead = [
            Head.stylesheet("/base.css"),
            Head.stylesheet("/theme-dark.css")
        ];

        var patches = Diff(oldHead, newHead);

        // base.css unchanged, theme-light removed, theme-dark added
        Assert.Equal(2, patches.Count);
        Assert.Contains(patches, p => p is HeadPatch.Add a
            && a.Content.Key == "link:stylesheet:/theme-dark.css");
        Assert.Contains(patches, p => p is HeadPatch.Remove r
            && r.Key == "link:stylesheet:/theme-light.css");
    }

    // =========================================================================
    // Document Backward Compatibility
    // =========================================================================

    [Fact]
    public void Document_WithoutHead_HasEmptyHeadArray()
    {
        var doc = new DOM.Document("Title", new DOM.Text("1", "body"));
        Assert.Empty(doc.Head);
    }

    [Fact]
    public void Document_WithHead_HasPopulatedHeadArray()
    {
        var doc = new DOM.Document(
            "Title",
            new DOM.Text("1", "body"),
            Head.meta("description", "test"),
            Head.og("title", "test")
        );
        Assert.Equal(2, doc.Head.Length);
    }
}
