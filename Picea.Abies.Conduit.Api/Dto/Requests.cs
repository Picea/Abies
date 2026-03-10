// =============================================================================
// API DTOs — Request Bodies
// =============================================================================
// These records model the JSON request payloads from the Conduit spec.
// All request bodies are wrapped in a named root object per the spec:
//   { "user": { ... } }
//   { "article": { ... } }
//   { "comment": { ... } }
//
// Using System.Text.Json source generation for trim-safe deserialization.
// =============================================================================

namespace Picea.Abies.Conduit.Api.Dto;

// ─── User / Auth ──────────────────────────────────────────────────────────────

/// <summary>
/// POST /api/users — Register a new user.
/// <code>{ "user": { "username": "...", "email": "...", "password": "..." } }</code>
/// </summary>
public sealed record RegisterUserRequest(RegisterUserBody User);

/// <summary>Inner body for user registration.</summary>
public sealed record RegisterUserBody(string Username, string Email, string Password);

/// <summary>
/// POST /api/users/login — Authenticate an existing user.
/// <code>{ "user": { "email": "...", "password": "..." } }</code>
/// </summary>
public sealed record LoginUserRequest(LoginUserBody User);

/// <summary>Inner body for user login.</summary>
public sealed record LoginUserBody(string Email, string Password);

/// <summary>
/// PUT /api/user — Update the current user.
/// <code>{ "user": { "email": "...", "username": "...", "password": "...", "bio": "...", "image": "..." } }</code>
/// </summary>
public sealed record UpdateUserRequest(UpdateUserBody User);

/// <summary>Inner body for user update. All fields are optional.</summary>
public sealed record UpdateUserBody(
    string? Email = null,
    string? Username = null,
    string? Password = null,
    string? Bio = null,
    string? Image = null);

// ─── Article ──────────────────────────────────────────────────────────────────

/// <summary>
/// POST /api/articles — Create a new article.
/// <code>{ "article": { "title": "...", "description": "...", "body": "...", "tagList": [...] } }</code>
/// </summary>
public sealed record CreateArticleRequest(CreateArticleBody Article);

/// <summary>Inner body for article creation.</summary>
public sealed record CreateArticleBody(
    string Title,
    string Description,
    string Body,
    string[]? TagList = null);

/// <summary>
/// PUT /api/articles/:slug — Update an existing article.
/// <code>{ "article": { "title": "...", "description": "...", "body": "..." } }</code>
/// </summary>
public sealed record UpdateArticleRequest(UpdateArticleBody Article);

/// <summary>Inner body for article update. All fields are optional.</summary>
public sealed record UpdateArticleBody(
    string? Title = null,
    string? Description = null,
    string? Body = null);

// ─── Comment ──────────────────────────────────────────────────────────────────

/// <summary>
/// POST /api/articles/:slug/comments — Add a comment to an article.
/// <code>{ "comment": { "body": "..." } }</code>
/// </summary>
public sealed record AddCommentRequest(AddCommentBody Comment);

/// <summary>Inner body for adding a comment.</summary>
public sealed record AddCommentBody(string Body);
