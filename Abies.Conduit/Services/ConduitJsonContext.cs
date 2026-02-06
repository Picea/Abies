using System.Text.Json.Serialization;

namespace Abies.Conduit.Services;

/// <summary>
/// Source-generated JSON serialization context for the Conduit API client.
/// Covers all API response models and request DTOs.
/// Uses camelCase naming to match the Conduit API convention.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(UserData))]
[JsonSerializable(typeof(ArticlesResponse))]
[JsonSerializable(typeof(ArticleResponse))]
[JsonSerializable(typeof(ArticleData))]
[JsonSerializable(typeof(CommentsResponse))]
[JsonSerializable(typeof(CommentResponse))]
[JsonSerializable(typeof(CommentData))]
[JsonSerializable(typeof(ProfileResponse))]
[JsonSerializable(typeof(ProfileData))]
[JsonSerializable(typeof(TagsResponse))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginUserBody))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(RegisterUserBody))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(UpdateUserBody))]
[JsonSerializable(typeof(CreateArticleRequest))]
[JsonSerializable(typeof(CreateArticleBody))]
[JsonSerializable(typeof(UpdateArticleRequest))]
[JsonSerializable(typeof(UpdateArticleBody))]
[JsonSerializable(typeof(AddCommentRequest))]
[JsonSerializable(typeof(AddCommentBody))]
internal partial class ConduitJsonContext : JsonSerializerContext;
