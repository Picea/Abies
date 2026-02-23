// =============================================================================
// Conduit Capabilities
// =============================================================================
// Capability delegate types represent the "ports" of the application — the
// boundary between pure command handling logic and impure side effects.
//
// Each delegate maps to a single side-effect operation. Real implementations
// point to API service methods; test implementations are plain lambdas.
//
// All capabilities return Result<T, ConduitError> (or Option<T>) instead of
// throwing exceptions, making errors explicit in the type system.
//
// Design references:
// - DDD instructions: "Pass dependencies as functions ('capabilities')"
// - DDD instructions: "Make errors explicit using Result/Option, not exceptions/null"
// - Mark Seemann's "Dependency Rejection"
// - Scott Wlaschin's "Dependency Interpretation"
// - Scott Wlaschin's "Railway-Oriented Programming"
// =============================================================================

using Abies.Conduit.Main;
using Abies.Conduit.Page.Article;
using Abies.Conduit.Page.Home;

namespace Abies.Conduit.Capabilities;

// ── Domain error types ──

/// <summary>
/// Represents errors that can occur during Conduit operations.
/// </summary>
/// <remarks>
/// <para>
/// Expected failures are values, not exceptions. Handlers pattern match on
/// these error variants to produce the appropriate UI message.
/// </para>
/// <example>
/// <code>
/// var result = await login(email, password);
/// Message message = result switch
/// {
///     Ok&lt;User, ConduitError&gt;(var user) => new LoginSuccess(user),
///     Error&lt;User, ConduitError&gt;(var err) => err switch
///     {
///         ValidationError v => new LoginError(v.Errors),
///         Unauthorized => new UserLoggedOut(),
///         UnexpectedError e => new LoginError(e.Message),
///         _ => throw new UnreachableException()
///     },
///     _ => throw new UnreachableException()
/// };
/// </code>
/// </example>
/// </remarks>
public abstract record ConduitError;

/// <summary>API returned validation errors (e.g., invalid email format, duplicate username).</summary>
public sealed record ValidationError(Dictionary<string, string[]> Errors) : ConduitError;

/// <summary>The user's session has expired or the request is unauthorized.</summary>
public sealed record Unauthorized : ConduitError;

/// <summary>An unexpected infrastructure error occurred.</summary>
public sealed record UnexpectedError(string Message) : ConduitError;

// ── Auth capabilities ──

/// <summary>
/// Authenticates a user with email and password.
/// Returns <see cref="ValidationError"/> for invalid credentials,
/// <see cref="Unauthorized"/> for expired sessions.
/// </summary>
public delegate Task<Result<User, ConduitError>> Login(string email, string password);

/// <summary>
/// Registers a new user.
/// Returns <see cref="ValidationError"/> for duplicate username/email.
/// </summary>
public delegate Task<Result<User, ConduitError>> Register(string username, string email, string password);

/// <summary>
/// Updates user profile information.
/// Returns <see cref="ValidationError"/> for invalid fields.
/// </summary>
public delegate Task<Result<User, ConduitError>> UpdateUser(string username, string email, string bio, string image, string? password);

/// <summary>
/// Loads the currently authenticated user from local storage.
/// Returns <see cref="Some{T}"/> when a user session exists,
/// <see cref="None{T}"/> when no user is authenticated.
/// </summary>
public delegate Task<Option<User>> LoadCurrentUser();

/// <summary>
/// Logs out the current user and clears stored credentials.
/// Returns <see cref="UnexpectedError"/> if cleanup fails.
/// </summary>
public delegate Task<Result<Unit, ConduitError>> Logout();

// ── Article capabilities ──

/// <summary>
/// Loads a paginated list of articles with optional filters.
/// </summary>
public delegate Task<Result<(List<Article> Articles, int Count), ConduitError>> LoadArticles(
    string? tag = null, string? author = null, string? favoritedBy = null, int limit = 10, int offset = 0);

/// <summary>
/// Loads the authenticated user's article feed.
/// </summary>
public delegate Task<Result<(List<Article> Articles, int Count), ConduitError>> LoadFeed(int limit = 10, int offset = 0);

/// <summary>
/// Loads a single article by slug.
/// </summary>
public delegate Task<Result<Article, ConduitError>> GetArticle(string slug);

/// <summary>
/// Creates a new article.
/// Returns <see cref="ValidationError"/> for invalid content.
/// </summary>
public delegate Task<Result<Article, ConduitError>> CreateArticle(string title, string description, string body, List<string> tagList);

/// <summary>
/// Updates an existing article.
/// Returns <see cref="ValidationError"/> for invalid content.
/// </summary>
public delegate Task<Result<Article, ConduitError>> UpdateArticle(string slug, string title, string description, string body);

/// <summary>
/// Deletes an article by slug.
/// </summary>
public delegate Task<Result<Unit, ConduitError>> DeleteArticle(string slug);

/// <summary>
/// Favorites an article, returning the updated article.
/// </summary>
public delegate Task<Result<Article, ConduitError>> FavoriteArticle(string slug);

/// <summary>
/// Unfavorites an article, returning the updated article.
/// </summary>
public delegate Task<Result<Article, ConduitError>> UnfavoriteArticle(string slug);

// ── Comment capabilities ──

/// <summary>
/// Loads all comments for an article.
/// </summary>
public delegate Task<Result<List<Comment>, ConduitError>> LoadComments(string slug);

/// <summary>
/// Adds a comment to an article.
/// </summary>
public delegate Task<Result<Comment, ConduitError>> AddComment(string slug, string body);

/// <summary>
/// Deletes a comment from an article.
/// </summary>
public delegate Task<Result<Unit, ConduitError>> DeleteComment(string slug, string commentId);

// ── Tag capabilities ──

/// <summary>
/// Loads all available tags.
/// </summary>
public delegate Task<Result<List<string>, ConduitError>> LoadTags();

// ── Profile capabilities ──

/// <summary>
/// Loads a user profile by username.
/// </summary>
public delegate Task<Result<Profile, ConduitError>> LoadProfile(string username);

/// <summary>
/// Follows a user, returning the updated profile.
/// </summary>
public delegate Task<Result<Profile, ConduitError>> FollowUser(string username);

/// <summary>
/// Unfollows a user, returning the updated profile.
/// </summary>
public delegate Task<Result<Profile, ConduitError>> UnfollowUser(string username);

