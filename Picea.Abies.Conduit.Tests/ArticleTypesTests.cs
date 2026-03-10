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

    [Theory]
    [InlineData("hello-world")]
    [InlineData("a")]
    [InlineData("my-first-post")]
    [InlineData("123")]
    public void Slug_ValidSlugs_ReturnsOk(string slug)
    {
        var result = Slug.Create(slug);

        Assert.True(result.IsOk);
        Assert.Equal(slug, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Slug_EmptyOrWhitespace_ReturnsErr(string? slug)
    {
        var result = Slug.Create(slug!);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("HAS SPACES")]
    [InlineData("has_underscores")]
    [InlineData("has@special")]
    [InlineData("-starts-with-hyphen")]
    public void Slug_InvalidFormat_ReturnsErr(string slug)
    {
        var result = Slug.Create(slug);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("My First Post!", "my-first-post")]
    [InlineData("  Spaces  Everywhere  ", "spaces-everywhere")]
    public void Slug_FromTitle_GeneratesCorrectSlug(string titleValue, string expected)
    {
        var title = new Title(titleValue);

        var slug = Slug.FromTitle(title);

        Assert.Equal(expected, slug.Value);
    }

    // =========================================================================
    // Title
    // =========================================================================

    [Theory]
    [InlineData("Hello World")]
    [InlineData("A")]
    public void Title_ValidTitles_ReturnsOk(string title)
    {
        var result = Title.Create(title);

        Assert.True(result.IsOk);
        Assert.Equal(title.Trim(), result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Title_EmptyOrWhitespace_ReturnsErr(string? title)
    {
        var result = Title.Create(title!);

        Assert.True(result.IsErr);
    }

    [Fact]
    public void Title_TooLong_ReturnsErr()
    {
        var longTitle = new string('a', 257);

        var result = Title.Create(longTitle);

        Assert.True(result.IsErr);
    }

    // =========================================================================
    // Description
    // =========================================================================

    [Fact]
    public void Description_ValidText_ReturnsOk()
    {
        var result = Description.Create("A short description.");

        Assert.True(result.IsOk);
    }

    [Fact]
    public void Description_TooLong_ReturnsErr()
    {
        var longDesc = new string('a', 1001);

        var result = Description.Create(longDesc);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_EmptyOrWhitespace_ReturnsErr(string desc)
    {
        var result = Description.Create(desc);

        Assert.True(result.IsErr);
    }

    // =========================================================================
    // Body
    // =========================================================================

    [Fact]
    public void Body_ValidText_ReturnsOk()
    {
        var result = Body.Create("# Hello\n\nThis is markdown.");

        Assert.True(result.IsOk);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Body_EmptyOrWhitespace_ReturnsErr(string body)
    {
        var result = Body.Create(body);

        Assert.True(result.IsErr);
    }

    // =========================================================================
    // Tag
    // =========================================================================

    [Theory]
    [InlineData("csharp")]
    [InlineData("web-dev")]
    [InlineData("dotnet10")]
    public void Tag_ValidTags_ReturnsOk(string tag)
    {
        var result = Tag.Create(tag);

        Assert.True(result.IsOk);
        Assert.Equal(tag, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Tag_EmptyOrWhitespace_ReturnsErr(string? tag)
    {
        var result = Tag.Create(tag!);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("HAS CAPS")]
    [InlineData("-starts-with-hyphen")]
    [InlineData("has@special")]
    public void Tag_InvalidFormat_ReturnsErr(string tag)
    {
        var result = Tag.Create(tag);

        Assert.True(result.IsErr);
    }

    [Fact]
    public void Tag_TooLong_ReturnsErr()
    {
        var longTag = new string('a', 31);

        var result = Tag.Create(longTag);

        Assert.True(result.IsErr);
    }

    // =========================================================================
    // CommentBody
    // =========================================================================

    [Fact]
    public void CommentBody_ValidText_ReturnsOk()
    {
        var result = CommentBody.Create("Great article!");

        Assert.True(result.IsOk);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CommentBody_EmptyOrWhitespace_ReturnsErr(string body)
    {
        var result = CommentBody.Create(body);

        Assert.True(result.IsErr);
    }

    [Fact]
    public void CommentBody_TooLong_ReturnsErr()
    {
        var longComment = new string('a', 5001);

        var result = CommentBody.Create(longComment);

        Assert.True(result.IsErr);
    }
}
