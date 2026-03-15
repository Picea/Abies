// =============================================================================
// Article Types Tests — Constrained Type Validation
// =============================================================================
// Tests the smart constructors for all Article domain types. Each type guards
// its invariants at construction time, returning Result<T, ArticleError>.
// =============================================================================

using Picea.Abies.Conduit.Domain.Article;

namespace Picea.Abies.Conduit.Tests;

public class ArticleTypesTests
{
    // =========================================================================
    // Slug
    // =========================================================================

    [Test]
    [Arguments("hello-world")]
    [Arguments("a")]
    [Arguments("my-first-post")]
    [Arguments("123")]
    public async Task Slug_ValidSlugs_ReturnsOk(string slug)
    {
        var result = Slug.Create(slug);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(slug);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task Slug_EmptyOrWhitespace_ReturnsErr(string? slug)
    {
        var result = Slug.Create(slug!);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("HAS SPACES")]
    [Arguments("has_underscores")]
    [Arguments("has@special")]
    [Arguments("-starts-with-hyphen")]
    public async Task Slug_InvalidFormat_ReturnsErr(string slug)
    {
        var result = Slug.Create(slug);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("Hello World", "hello-world")]
    [Arguments("My First Post!", "my-first-post")]
    [Arguments("  Spaces  Everywhere  ", "spaces-everywhere")]
    public async Task Slug_FromTitle_GeneratesCorrectSlug(string titleValue, string expected)
    {
        var title = new Title(titleValue);

        var slug = Slug.FromTitle(title);

        await Assert.That(slug.Value).IsEqualTo(expected);
    }

    // =========================================================================
    // Title
    // =========================================================================

    [Test]
    [Arguments("Hello World")]
    [Arguments("A")]
    public async Task Title_ValidTitles_ReturnsOk(string title)
    {
        var result = Title.Create(title);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(title.Trim());
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task Title_EmptyOrWhitespace_ReturnsErr(string? title)
    {
        var result = Title.Create(title!);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    public async Task Title_TooLong_ReturnsErr()
    {
        var longTitle = new string('a', 257);

        var result = Title.Create(longTitle);

        await Assert.That(result.IsErr).IsTrue();
    }

    // =========================================================================
    // Description
    // =========================================================================

    [Test]
    public async Task Description_ValidText_ReturnsOk()
    {
        var result = Description.Create("A short description.");

        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
    public async Task Description_TooLong_ReturnsErr()
    {
        var longDesc = new string('a', 1001);

        var result = Description.Create(longDesc);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Description_EmptyOrWhitespace_ReturnsErr(string desc)
    {
        var result = Description.Create(desc);

        await Assert.That(result.IsErr).IsTrue();
    }

    // =========================================================================
    // Body
    // =========================================================================

    [Test]
    public async Task Body_ValidText_ReturnsOk()
    {
        var result = Body.Create("# Hello\n\nThis is markdown.");

        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Body_EmptyOrWhitespace_ReturnsErr(string body)
    {
        var result = Body.Create(body);

        await Assert.That(result.IsErr).IsTrue();
    }

    // =========================================================================
    // Tag
    // =========================================================================

    [Test]
    [Arguments("csharp")]
    [Arguments("web-dev")]
    [Arguments("dotnet10")]
    public async Task Tag_ValidTags_ReturnsOk(string tag)
    {
        var result = Tag.Create(tag);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(tag);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task Tag_EmptyOrWhitespace_ReturnsErr(string? tag)
    {
        var result = Tag.Create(tag!);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("HAS CAPS")]
    [Arguments("-starts-with-hyphen")]
    [Arguments("has@special")]
    public async Task Tag_InvalidFormat_ReturnsErr(string tag)
    {
        var result = Tag.Create(tag);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    public async Task Tag_TooLong_ReturnsErr()
    {
        var longTag = new string('a', 31);

        var result = Tag.Create(longTag);

        await Assert.That(result.IsErr).IsTrue();
    }

    // =========================================================================
    // CommentBody
    // =========================================================================

    [Test]
    public async Task CommentBody_ValidText_ReturnsOk()
    {
        var result = CommentBody.Create("Great article!");

        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task CommentBody_EmptyOrWhitespace_ReturnsErr(string body)
    {
        var result = CommentBody.Create(body);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    public async Task CommentBody_TooLong_ReturnsErr()
    {
        var longComment = new string('a', 5001);

        var result = CommentBody.Create(longComment);

        await Assert.That(result.IsErr).IsTrue();
    }
}
