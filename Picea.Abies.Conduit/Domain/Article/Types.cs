// =============================================================================
// Article Domain — Constrained Types (Smart Constructors)
// =============================================================================
// Each type encodes its invariants at construction time, making illegal
// states unrepresentable. Construction returns Result<T, ArticleError> so
// validation failures are values, not exceptions.
//
// These types form the ubiquitous language of the Article bounded context:
//   Slug, Title, Description, Body, Tag, ArticleId, CommentId, CommentBody
// =============================================================================

using System.Text.RegularExpressions;

namespace Picea.Abies.Conduit.Domain.Article;

/// <summary>
/// A unique identifier for an article.
/// </summary>
public readonly record struct ArticleId(Guid Value)
{
    /// <summary>Generates a new unique article ID.</summary>
    public static ArticleId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A unique identifier for a comment.
/// </summary>
public readonly record struct CommentId(Guid Value)
{
    /// <summary>Generates a new unique comment ID.</summary>
    public static CommentId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A URL-friendly slug derived from the article title.
/// </summary>
public readonly record struct Slug(string Value)
{
    private static readonly Regex ValidPattern = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a validated <see cref="Slug"/> from a raw string.
    /// </summary>
    public static Result<Slug, ArticleError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Slug, ArticleError>.Err(new ArticleError.Validation("Slug is required."))
            : !ValidPattern.IsMatch(value.Trim().ToLowerInvariant())
                ? Result<Slug, ArticleError>.Err(new ArticleError.Validation(
                    "Slug must contain only lowercase letters, numbers, and hyphens."))
                : Result<Slug, ArticleError>.Ok(new Slug(value.Trim().ToLowerInvariant()));

    /// <summary>
    /// Generates a slug from a title by normalizing whitespace and special characters.
    /// </summary>
    public static Slug FromTitle(Title title)
    {
        var slug = Regex.Replace(title.Value.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s]+", "-");
        slug = slug.Trim('-');
        return new Slug(slug);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated article title (1–256 characters).
/// </summary>
public readonly record struct Title(string Value)
{
    private const int MaxLength = 256;

    /// <summary>
    /// Creates a validated <see cref="Title"/>.
    /// </summary>
    public static Result<Title, ArticleError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Title, ArticleError>.Err(new ArticleError.Validation("Title is required."))
            : value.Trim().Length > MaxLength
                ? Result<Title, ArticleError>.Err(new ArticleError.Validation(
                    $"Title must be at most {MaxLength} characters."))
                : Result<Title, ArticleError>.Ok(new Title(value.Trim()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated article description (max 1000 characters).
/// </summary>
public readonly record struct Description(string Value)
{
    private const int MaxLength = 1000;

    /// <summary>
    /// Creates a validated <see cref="Description"/>.
    /// </summary>
    public static Result<Description, ArticleError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Description, ArticleError>.Err(new ArticleError.Validation("Description is required."))
            : value.Trim().Length > MaxLength
                ? Result<Description, ArticleError>.Err(new ArticleError.Validation(
                    $"Description must be at most {MaxLength} characters."))
                : Result<Description, ArticleError>.Ok(new Description(value.Trim()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated article body (Markdown content).
/// </summary>
public readonly record struct Body(string Value)
{
    /// <summary>
    /// Creates a validated <see cref="Body"/>.
    /// </summary>
    public static Result<Body, ArticleError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Body, ArticleError>.Err(new ArticleError.Validation("Body is required."))
            : Result<Body, ArticleError>.Ok(new Body(value.Trim()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated tag name (1–30 characters, lowercase alphanumeric and hyphens).
/// </summary>
public readonly record struct Tag(string Value)
{
    private static readonly Regex Pattern = new(
        @"^[a-z0-9][a-z0-9-]{0,29}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a validated <see cref="Tag"/>.
    /// </summary>
    public static Result<Tag, ArticleError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Tag, ArticleError>.Err(new ArticleError.Validation("Tag is required."))
            : !Pattern.IsMatch(value.Trim().ToLowerInvariant())
                ? Result<Tag, ArticleError>.Err(new ArticleError.Validation(
                    "Tag must be 1-30 lowercase characters: letters, numbers, hyphens."))
                : Result<Tag, ArticleError>.Ok(new Tag(value.Trim().ToLowerInvariant()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated comment body (1–5000 characters).
/// </summary>
public readonly record struct CommentBody(string Value)
{
    private const int MaxLength = 5000;

    /// <summary>
    /// Creates a validated <see cref="CommentBody"/>.
    /// </summary>
    public static Result<CommentBody, ArticleError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<CommentBody, ArticleError>.Err(new ArticleError.Validation("Comment body is required."))
            : value.Trim().Length > MaxLength
                ? Result<CommentBody, ArticleError>.Err(new ArticleError.Validation(
                    $"Comment must be at most {MaxLength} characters."))
                : Result<CommentBody, ArticleError>.Ok(new CommentBody(value.Trim()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// An immutable comment on an article.
/// </summary>
public readonly record struct Comment(
    CommentId Id,
    Shared.UserId AuthorId,
    CommentBody Body,
    Shared.Timestamp CreatedAt);
