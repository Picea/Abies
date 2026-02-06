// =============================================================================
// Conduit API Request DTOs
// =============================================================================
// Explicit request record types that replace anonymous types for trim-safe
// JSON source generation. Anonymous types cannot be registered with
// JsonSerializerContext, so we need concrete types.
// =============================================================================

namespace Abies.Conduit.Services;

/// <summary>Request envelope for user login.</summary>
public record LoginRequest(LoginRequest.LoginUser User)
{
    public record LoginUser(string Email, string Password);
}

/// <summary>Request envelope for user registration.</summary>
public record RegisterRequest(RegisterRequest.RegisterUser User)
{
    public record RegisterUser(string Username, string Email, string Password);
}

/// <summary>Request envelope for updating user settings.</summary>
public record UpdateUserRequest(UpdateUserRequest.UpdateUser User)
{
    public record UpdateUser(string Email, string Username, string Bio, string Image, string? Password = null);
}

/// <summary>Request envelope for creating an article.</summary>
public record CreateArticleRequest(CreateArticleRequest.CreateArticle Article)
{
    public record CreateArticle(string Title, string Description, string Body, List<string> TagList);
}

/// <summary>Request envelope for updating an article.</summary>
public record UpdateArticleRequest(UpdateArticleRequest.UpdateArticle Article)
{
    public record UpdateArticle(string Title, string Description, string Body);
}

/// <summary>Request envelope for adding a comment.</summary>
public record CreateCommentRequest(CreateCommentRequest.CreateComment Comment)
{
    public record CreateComment(string Body);
}
