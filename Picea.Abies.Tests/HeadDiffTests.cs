// =============================================================================
// HeadDiff Tests
// =============================================================================
// Tests for the pure head content diffing algorithm.
// Verifies correct add/update/remove patch generation for all HeadContent
// variants and edge cases (empty→full, full→empty, reorder, same content).
//
// Head patches are standard Patch types (AddHeadElement, UpdateHeadElement,
// RemoveHeadElement) that flow through the same binary batch protocol as
// body patches.
// =============================================================================

namespace Picea.Abies.Tests;

public class HeadDiffTests
{
    // =========================================================================
    // Empty ↔ Empty
    // =========================================================================

    [Fact]
    public void Diff_BothEmpty_ReturnsNoPatches()
    {
        var patches = HeadDiff.Diff([], []);

        Assert.Empty(patches);
    }

    // =========================================================================
    // Empty → Non-Empty (all adds)
    // =========================================================================

    [Fact]
    public void Diff_OldEmpty_ReturnsAddForEachNewElement()
    {
        HeadContent[] newHead =
        [
            Head.meta("description", "A test page"),
            Head.canonical("https://example.com")
        ];

        var patches = HeadDiff.Diff([], newHead);

        Assert.Equal(2, patches.Count);
        Assert.All(patches, p => Assert.IsType<AddHeadElement>(p));

        var add0 = (AddHeadElement)patches[0];
        Assert.IsType<HeadContent.Meta>(add0.Content);
        Assert.Equal("meta:description", add0.Content.Key);

        var add1 = (AddHeadElement)patches[1];
        Assert.IsType<HeadContent.Link>(add1.Content);
        Assert.Equal("link:canonical:https://example.com", add1.Content.Key);
    }

    [Fact]
    public void Diff_OldEmpty_SingleElement_ReturnsSingleAdd()
    {
        HeadContent[] newHead = [Head.og("title", "My Page")];

        var patches = HeadDiff.Diff([], newHead);

        var patch = Assert.Single(patches);
        var add = Assert.IsType<AddHeadElement>(patch);
        Assert.Equal("property:og:title", add.Content.Key);
    }

    // =========================================================================
    // Non-Empty → Empty (all removes)
    // =========================================================================

    [Fact]
    public void Diff_NewEmpty_ReturnsRemoveForEachOldElement()
    {
        HeadContent[] oldHead =
        [
            Head.meta("description", "Old page"),
            Head.stylesheet("/style.css")
        ];

        var patches = HeadDiff.Diff(oldHead, []);

        Assert.Equal(2, patches.Count);
        Assert.All(patches, p => Assert.IsType<RemoveHeadElement>(p));

        var remove0 = (RemoveHeadElement)patches[0];
        Assert.Equal("meta:description", remove0.Key);

        var remove1 = (RemoveHeadElement)patches[1];
        Assert.Equal("link:stylesheet:/style.css", remove1.Key);
    }

    // =========================================================================
    // Same content — no patches
    // =========================================================================

    [Fact]
    public void Diff_IdenticalContent_ReturnsNoPatches()
    {
        HeadContent[] head =
        [
            Head.meta("description", "Same page"),
            Head.og("title", "Same Title"),
            Head.canonical("https://example.com")
        ];

        var patches = HeadDiff.Diff(head, head);

        Assert.Empty(patches);
    }

    [Fact]
    public void Diff_EqualButDifferentInstances_ReturnsNoPatches()
    {
        HeadContent[] oldHead = [Head.meta("description", "Test")];
        HeadContent[] newHead = [Head.meta("description", "Test")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        Assert.Empty(patches);
    }

    // =========================================================================
    // Updates — same key, different content
    // =========================================================================

    [Fact]
    public void Diff_MetaContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.meta("description", "Old description")];
        HeadContent[] newHead = [Head.meta("description", "New description")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var update = Assert.IsType<UpdateHeadElement>(patch);
        Assert.Equal("meta:description", update.Content.Key);
        Assert.Equal("New description", ((HeadContent.Meta)update.Content).Content);
    }

    [Fact]
    public void Diff_OgPropertyContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.og("title", "Old Title")];
        HeadContent[] newHead = [Head.og("title", "New Title")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var update = Assert.IsType<UpdateHeadElement>(patch);
        Assert.Equal("New Title", ((HeadContent.MetaProperty)update.Content).Content);
    }

    [Fact]
    public void Diff_CanonicalHrefChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.canonical("https://old.com")];
        HeadContent[] newHead = [Head.canonical("https://new.com")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        Assert.Equal(2, patches.Count);
        Assert.IsType<AddHeadElement>(patches[0]);
        Assert.IsType<RemoveHeadElement>(patches[1]);
    }

    // =========================================================================
    // Mixed operations — add, update, and remove in one diff
    // =========================================================================

    [Fact]
    public void Diff_MixedChanges_ReturnsCorrectPatches()
    {
        HeadContent[] oldHead =
        [
            Head.meta("description", "Old desc"),
            Head.og("title", "Title"),
            Head.stylesheet("/style.css")
        ];

        HeadContent[] newHead =
        [
            Head.meta("description", "New desc"),
            Head.stylesheet("/style.css"),
            Head.canonical("https://example.com")
        ];

        var patches = HeadDiff.Diff(oldHead, newHead);

        Assert.Equal(3, patches.Count);

        var update = patches.OfType<UpdateHeadElement>().Single();
        Assert.Equal("meta:description", update.Content.Key);

        var add = patches.OfType<AddHeadElement>().Single();
        Assert.Equal("link:canonical:https://example.com", add.Content.Key);

        var remove = patches.OfType<RemoveHeadElement>().Single();
        Assert.Equal("property:og:title", remove.Key);
    }

    // =========================================================================
    // All HeadContent variants
    // =========================================================================

    [Fact]
    public void Diff_AllVariants_AddedCorrectly()
    {
        HeadContent[] newHead =
        [
            Head.meta("viewport", "width=device-width"),
            Head.og("description", "OG desc"),
            Head.twitter("card", "summary"),
            Head.canonical("https://example.com"),
            Head.stylesheet("/main.css"),
            Head.preload("/font.woff2", "font"),
            Head.link("icon", "/favicon.ico"),
            Head.property("article:author", "Jane"),
            Head.@base("/app/"),
            Head.jsonLd(new { type = "Article" })
        ];

        var patches = HeadDiff.Diff([], newHead);

        Assert.Equal(10, patches.Count);
        Assert.All(patches, p => Assert.IsType<AddHeadElement>(p));
    }

    [Fact]
    public void Diff_ScriptContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.jsonLd(new { type = "Article", name = "Old" })];
        HeadContent[] newHead = [Head.jsonLd(new { type = "Article", name = "New" })];

        var patches = HeadDiff.Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var update = Assert.IsType<UpdateHeadElement>(patch);
        Assert.Equal("script:application/ld+json", update.Content.Key);
    }

    [Fact]
    public void Diff_BaseHrefChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.@base("/old/")];
        HeadContent[] newHead = [Head.@base("/new/")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        var patch = Assert.Single(patches);
        var update = Assert.IsType<UpdateHeadElement>(patch);
        Assert.Equal("base", update.Content.Key);
    }

    // =========================================================================
    // Order independence
    // =========================================================================

    [Fact]
    public void Diff_ReorderedSameContent_ReturnsNoPatches()
    {
        HeadContent[] oldHead =
        [
            Head.meta("description", "Desc"),
            Head.og("title", "Title"),
            Head.canonical("https://example.com")
        ];

        HeadContent[] newHead =
        [
            Head.canonical("https://example.com"),
            Head.meta("description", "Desc"),
            Head.og("title", "Title")
        ];

        var patches = HeadDiff.Diff(oldHead, newHead);

        Assert.Empty(patches);
    }

    // =========================================================================
    // ToHtml correctness (used by binary protocol to serialize elements)
    // =========================================================================

    [Fact]
    public void Meta_ToHtml_IncludesDataAbiesHeadAttribute()
    {
        var meta = Head.meta("description", "Test page");

        var html = meta.ToHtml();

        Assert.Contains("""data-abies-head="meta:description""", html);
        Assert.Contains("""content="Test page""", html);
    }

    [Fact]
    public void Link_ToHtml_IncludesDataAbiesHeadAttribute()
    {
        var link = Head.stylesheet("/style.css");

        var html = link.ToHtml();

        Assert.Contains("""data-abies-head="link:stylesheet:/style.css""", html);
        Assert.Contains("""href="/style.css""", html);
    }

    [Fact]
    public void Meta_ToHtml_EncodesSpecialCharacters()
    {
        var meta = Head.meta("description", """He said "hello" & <goodbye>""");

        var html = meta.ToHtml();

        Assert.Contains("&amp;", html);
        Assert.Contains("&quot;", html);
        Assert.Contains("&lt;", html);
        Assert.Contains("&gt;", html);
        Assert.DoesNotContain("<goodbye>", html);
    }

    // =========================================================================
    // Duplicate keys — last one wins
    // =========================================================================

    [Fact]
    public void Diff_DuplicateKeysInNew_LastOneWins()
    {
        HeadContent[] oldHead = [Head.meta("description", "Old")];
        HeadContent[] newHead =
        [
            Head.meta("description", "First"),
            Head.meta("description", "Second")
        ];

        var patches = HeadDiff.Diff(oldHead, newHead);

        Assert.Contains(patches, p => p is UpdateHeadElement);
    }
}
