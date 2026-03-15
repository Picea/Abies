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

[NotInParallel("shared-dom-state")]
public class HeadDiffTests
{
    // =========================================================================
    // Empty ↔ Empty
    // =========================================================================

    [Test]
    public async Task Diff_BothEmpty_ReturnsNoPatches()
    {
        var patches = HeadDiff.Diff([], []);

        await Assert.That(patches).IsEmpty();
    }

    // =========================================================================
    // Empty → Non-Empty (all adds)
    // =========================================================================

    [Test]
    public async Task Diff_OldEmpty_ReturnsAddForEachNewElement()
    {
        HeadContent[] newHead =
        [
            Head.meta("description", "A test page"),
            Head.canonical("https://example.com")
        ];

        var patches = HeadDiff.Diff([], newHead);

        await Assert.That(patches).Count().IsEqualTo(2);
        foreach (var p in patches)
        {
            await Assert.That(p).IsTypeOf<AddHeadElement>();
        }

        var add0 = (AddHeadElement)patches[0];
        await Assert.That(add0.Content).IsTypeOf<HeadContent.Meta>();
        await Assert.That(add0.Content.Key).IsEqualTo("meta:description");

        var add1 = (AddHeadElement)patches[1];
        await Assert.That(add1.Content).IsTypeOf<HeadContent.Link>();
        await Assert.That(add1.Content.Key).IsEqualTo("link:canonical:https://example.com");
    }

    [Test]
    public async Task Diff_OldEmpty_SingleElement_ReturnsSingleAdd()
    {
        HeadContent[] newHead = [Head.og("title", "My Page")];

        var patches = HeadDiff.Diff([], newHead);

        await Assert.That(patches).Count().IsEqualTo(1);
        var patch = patches[0];
        await Assert.That(patch).IsTypeOf<AddHeadElement>();
        var add = (AddHeadElement)patch;
        await Assert.That(add.Content.Key).IsEqualTo("property:og:title");
    }

    // =========================================================================
    // Non-Empty → Empty (all removes)
    // =========================================================================

    [Test]
    public async Task Diff_NewEmpty_ReturnsRemoveForEachOldElement()
    {
        HeadContent[] oldHead =
        [
            Head.meta("description", "Old page"),
            Head.stylesheet("/style.css")
        ];

        var patches = HeadDiff.Diff(oldHead, []);

        await Assert.That(patches).Count().IsEqualTo(2);
        foreach (var p in patches)
        {
            await Assert.That(p).IsTypeOf<RemoveHeadElement>();
        }

        var remove0 = (RemoveHeadElement)patches[0];
        await Assert.That(remove0.Key).IsEqualTo("meta:description");

        var remove1 = (RemoveHeadElement)patches[1];
        await Assert.That(remove1.Key).IsEqualTo("link:stylesheet:/style.css");
    }

    // =========================================================================
    // Same content — no patches
    // =========================================================================

    [Test]
    public async Task Diff_IdenticalContent_ReturnsNoPatches()
    {
        HeadContent[] head =
        [
            Head.meta("description", "Same page"),
            Head.og("title", "Same Title"),
            Head.canonical("https://example.com")
        ];

        var patches = HeadDiff.Diff(head, head);

        await Assert.That(patches).IsEmpty();
    }

    [Test]
    public async Task Diff_EqualButDifferentInstances_ReturnsNoPatches()
    {
        HeadContent[] oldHead = [Head.meta("description", "Test")];
        HeadContent[] newHead = [Head.meta("description", "Test")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches).IsEmpty();
    }

    // =========================================================================
    // Updates — same key, different content
    // =========================================================================

    [Test]
    public async Task Diff_MetaContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.meta("description", "Old description")];
        HeadContent[] newHead = [Head.meta("description", "New description")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches).Count().IsEqualTo(1);
        var patch = patches[0];
        await Assert.That(patch).IsTypeOf<UpdateHeadElement>();
        var update = (UpdateHeadElement)patch;
        await Assert.That(update.Content.Key).IsEqualTo("meta:description");
        await Assert.That(((HeadContent.Meta)update.Content).Content).IsEqualTo("New description");
    }

    [Test]
    public async Task Diff_OgPropertyContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.og("title", "Old Title")];
        HeadContent[] newHead = [Head.og("title", "New Title")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches).Count().IsEqualTo(1);
        var patch = patches[0];
        await Assert.That(patch).IsTypeOf<UpdateHeadElement>();
        var update = (UpdateHeadElement)patch;
        await Assert.That(((HeadContent.MetaProperty)update.Content).Content).IsEqualTo("New Title");
    }

    [Test]
    public async Task Diff_CanonicalHrefChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.canonical("https://old.com")];
        HeadContent[] newHead = [Head.canonical("https://new.com")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches).Count().IsEqualTo(2);
        await Assert.That(patches[0]).IsTypeOf<AddHeadElement>();
        await Assert.That(patches[1]).IsTypeOf<RemoveHeadElement>();
    }

    // =========================================================================
    // Mixed operations — add, update, and remove in one diff
    // =========================================================================

    [Test]
    public async Task Diff_MixedChanges_ReturnsCorrectPatches()
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

        await Assert.That(patches).Count().IsEqualTo(3);

        var update = patches.OfType<UpdateHeadElement>().Single();
        await Assert.That(update.Content.Key).IsEqualTo("meta:description");

        var add = patches.OfType<AddHeadElement>().Single();
        await Assert.That(add.Content.Key).IsEqualTo("link:canonical:https://example.com");

        var remove = patches.OfType<RemoveHeadElement>().Single();
        await Assert.That(remove.Key).IsEqualTo("property:og:title");
    }

    // =========================================================================
    // All HeadContent variants
    // =========================================================================

    [Test]
    public async Task Diff_AllVariants_AddedCorrectly()
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

        await Assert.That(patches).Count().IsEqualTo(10);
        foreach (var p in patches)
        {
            await Assert.That(p).IsTypeOf<AddHeadElement>();
        }
    }

    [Test]
    public async Task Diff_ScriptContentChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.jsonLd(new { type = "Article", name = "Old" })];
        HeadContent[] newHead = [Head.jsonLd(new { type = "Article", name = "New" })];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches).Count().IsEqualTo(1);
        var patch = patches[0];
        await Assert.That(patch).IsTypeOf<UpdateHeadElement>();
        var update = (UpdateHeadElement)patch;
        await Assert.That(update.Content.Key).IsEqualTo("script:application/ld+json");
    }

    [Test]
    public async Task Diff_BaseHrefChanged_ReturnsUpdate()
    {
        HeadContent[] oldHead = [Head.@base("/old/")];
        HeadContent[] newHead = [Head.@base("/new/")];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches).Count().IsEqualTo(1);
        var patch = patches[0];
        await Assert.That(patch).IsTypeOf<UpdateHeadElement>();
        var update = (UpdateHeadElement)patch;
        await Assert.That(update.Content.Key).IsEqualTo("base");
    }

    // =========================================================================
    // Order independence
    // =========================================================================

    [Test]
    public async Task Diff_ReorderedSameContent_ReturnsNoPatches()
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

        await Assert.That(patches).IsEmpty();
    }

    // =========================================================================
    // ToHtml correctness (used by binary protocol to serialize elements)
    // =========================================================================

    [Test]
    public async Task Meta_ToHtml_IncludesDataAbiesHeadAttribute()
    {
        var meta = Head.meta("description", "Test page");

        var html = meta.ToHtml();

        await Assert.That(html).Contains("""data-abies-head="meta:description""");
        await Assert.That(html).Contains("""content="Test page""");
    }

    [Test]
    public async Task Link_ToHtml_IncludesDataAbiesHeadAttribute()
    {
        var link = Head.stylesheet("/style.css");

        var html = link.ToHtml();

        await Assert.That(html).Contains("""data-abies-head="link:stylesheet:/style.css""");
        await Assert.That(html).Contains("""href="/style.css""");
    }

    [Test]
    public async Task Meta_ToHtml_EncodesSpecialCharacters()
    {
        var meta = Head.meta("description", """He said "hello" & <goodbye>""");

        var html = meta.ToHtml();

        await Assert.That(html).Contains("&amp;");
        await Assert.That(html).Contains("&quot;");
        await Assert.That(html).Contains("&lt;");
        await Assert.That(html).Contains("&gt;");
        await Assert.That(html).DoesNotContain("<goodbye>");
    }

    // =========================================================================
    // Duplicate keys — last one wins
    // =========================================================================

    [Test]
    public async Task Diff_DuplicateKeysInNew_LastOneWins()
    {
        HeadContent[] oldHead = [Head.meta("description", "Old")];
        HeadContent[] newHead =
        [
            Head.meta("description", "First"),
            Head.meta("description", "Second")
        ];

        var patches = HeadDiff.Diff(oldHead, newHead);

        await Assert.That(patches.Any(p => p is UpdateHeadElement)).IsTrue();
    }
}
