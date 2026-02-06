using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Abies.Conduit.Services;

// =============================================================================
// API Request DTOs
// =============================================================================
// Explicit request types used for source-generated JSON serialization.
// These replace anonymous types which cannot be source-generated.
// =============================================================================

public record LoginRequest(
    [property: JsonPropertyName("user")] LoginUserBody User);

public record LoginUserBody(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

public record RegisterRequest(
    [property: JsonPropertyName("user")] RegisterUserBody User);

public record RegisterUserBody(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

public record UpdateUserRequest(
    [property: JsonPropertyName("user")] UpdateUserBody User);

public record UpdateUserBody(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("bio")] string Bio,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("password")] string? Password);

public record CreateArticleRequest(
    [property: JsonPropertyName("article")] CreateArticleBody Article);

public record CreateArticleBody(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("tagList")] List<string> TagList);

public record UpdateArticleRequest(
    [property: JsonPropertyName("article")] UpdateArticleBody Article);

public record UpdateArticleBody(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("body")] string Body);

public record AddCommentRequest(
    [property: JsonPropertyName("comment")] AddCommentBody Comment);

public record AddCommentBody(
    [property: JsonPropertyName("body")] string Body);

