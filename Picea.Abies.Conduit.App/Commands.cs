// =============================================================================
// Commands — HTTP and Side-Effect Commands for the Conduit Frontend
// =============================================================================

namespace Picea.Abies.Conduit.App;

public interface ConduitCommand : Command;

// ─── Article Commands ─────────────────────────────────────────────────────

public sealed record FetchArticles(
    string ApiUrl, string? Token,
    int Limit = 10, int Offset = 0,
    string? Tag = null, string? Author = null, string? Favorited = null
) : ConduitCommand;

public sealed record FetchFeed(string ApiUrl, string Token, int Limit = 10, int Offset = 0) : ConduitCommand;
public sealed record FetchArticle(string ApiUrl, string? Token, string Slug) : ConduitCommand;
public sealed record FavoriteArticle(string ApiUrl, string Token, string Slug) : ConduitCommand;
public sealed record UnfavoriteArticle(string ApiUrl, string Token, string Slug) : ConduitCommand;
public sealed record DeleteArticleCommand(string ApiUrl, string Token, string Slug) : ConduitCommand;

// ─── Comment Commands ─────────────────────────────────────────────────────

public sealed record FetchComments(string ApiUrl, string? Token, string Slug) : ConduitCommand;
public sealed record AddComment(string ApiUrl, string Token, string Slug, string Body) : ConduitCommand;
public sealed record DeleteCommentCommand(string ApiUrl, string Token, string Slug, Guid CommentId) : ConduitCommand;

// ─── Tag Commands ─────────────────────────────────────────────────────────

public sealed record FetchTags(string ApiUrl) : ConduitCommand;

// ─── Auth Commands ────────────────────────────────────────────────────────

public sealed record LoginUser(string ApiUrl, string Email, string Password) : ConduitCommand;
public sealed record RegisterUser(string ApiUrl, string Username, string Email, string Password) : ConduitCommand;
public sealed record PersistSession(Session Session) : ConduitCommand;
public sealed record ClearPersistedSession : ConduitCommand;

// ─── Profile Commands ─────────────────────────────────────────────────────

public sealed record FetchProfile(string ApiUrl, string? Token, string Username) : ConduitCommand;
public sealed record FollowUser(string ApiUrl, string Token, string Username) : ConduitCommand;
public sealed record UnfollowUser(string ApiUrl, string Token, string Username) : ConduitCommand;

// ─── Settings Commands ────────────────────────────────────────────────────

public sealed record UpdateUser(
    string ApiUrl, string Token,
    string Image, string Username, string Bio, string Email, string? Password
) : ConduitCommand;

// ─── Editor Commands ──────────────────────────────────────────────────────

public sealed record CreateArticle(
    string ApiUrl, string Token,
    string Title, string Description, string Body, IReadOnlyList<string> TagList
) : ConduitCommand;

public sealed record UpdateArticle(
    string ApiUrl, string Token, string Slug,
    string Title, string Description, string Body, IReadOnlyList<string> TagList
) : ConduitCommand;
