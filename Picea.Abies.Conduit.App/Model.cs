// =============================================================================
// Model — Application State for the Conduit Frontend
// =============================================================================
// Single immutable model record following the Elm Architecture pattern.
// The Page discriminated union determines which view is rendered.
// Sub-models hold page-specific state (form fields, loaded data, etc.).
// =============================================================================

namespace Picea.Abies.Conduit.App;

// ─── Top-Level Model ──────────────────────────────────────────────────────────

/// <summary>
/// The complete application state. Immutable — updated via <c>with</c> expressions
/// in the Transition function.
/// </summary>
public sealed record Model(
    Page Page,
    Session? Session,
    string ApiUrl);

public sealed record ConduitStartup(
    string ApiUrl,
    Session? Session = null,
    Url? InitialUrl = null);

/// <summary>
/// An authenticated user session.
/// </summary>
public sealed record Session(
    string Token,
    string Username,
    string Email,
    string Bio,
    string? Image);

// ─── Page Discriminated Union ─────────────────────────────────────────────────

public abstract record Page
{
    private Page() { }
    public sealed record Home(HomeModel Data) : Page;
    public sealed record Login(LoginModel Data) : Page;
    public sealed record Register(RegisterModel Data) : Page;
    public sealed record Article(ArticleModel Data) : Page;
    public sealed record Settings(SettingsModel Data) : Page;
    public sealed record Editor(EditorModel Data) : Page;
    public sealed record Profile(ProfileModel Data) : Page;
    public sealed record NotFound : Page;
}

// ─── Feed Selection ─────────────────────────────────────────────────────────

public enum FeedTab { Global, Your, Tag }

// ─── Page Sub-Models ────────────────────────────────────────────────────────

public sealed record HomeModel(
    FeedTab ActiveTab, string? SelectedTag,
    IReadOnlyList<ArticlePreviewData> Articles, int ArticlesCount,
    int CurrentPage, IReadOnlyList<string> PopularTags, bool IsLoading);

public sealed record LoginModel(
    string Email, string Password,
    IReadOnlyList<string> Errors, bool IsSubmitting);

public sealed record RegisterModel(
    string Username, string Email, string Password,
    IReadOnlyList<string> Errors, bool IsSubmitting);

public sealed record ArticleModel(
    string Slug, ArticleData? Article,
    IReadOnlyList<CommentData> Comments, string CommentBody, bool IsLoading);

public sealed record SettingsModel(
    string Image, string Username, string Bio, string Email, string Password,
    IReadOnlyList<string> Errors, bool IsSubmitting);

public sealed record EditorModel(
    string? Slug, string Title, string Description, string Body,
    string TagInput, IReadOnlyList<string> TagList,
    IReadOnlyList<string> Errors, bool IsSubmitting);

public sealed record ProfileModel(
    string Username, ProfileData? Profile,
    IReadOnlyList<ArticlePreviewData> Articles, int ArticlesCount,
    int CurrentPage, bool ShowFavorites, bool IsLoading);

// ─── Shared Data Types ──────────────────────────────────────────────────────

public sealed record AuthorData(string Username, string Bio, string? Image, bool Following);

public sealed record ArticlePreviewData(
    string Slug, string Title, string Description,
    IReadOnlyList<string> TagList, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    bool Favorited, int FavoritesCount, AuthorData Author);

public sealed record ArticleData(
    string Slug, string Title, string Description, string Body,
    IReadOnlyList<string> TagList, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    bool Favorited, int FavoritesCount, AuthorData Author);

public sealed record CommentData(
    Guid Id, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    string Body, AuthorData Author);

public sealed record ProfileData(string Username, string Bio, string? Image, bool Following);

public static class Constants
{
    public const int ArticlesPerPage = 10;
}
