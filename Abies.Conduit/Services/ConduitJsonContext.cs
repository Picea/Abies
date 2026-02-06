// =============================================================================
// Conduit API JSON Serialization Context
// =============================================================================
// Source-generated JSON context for all Conduit API request and response types.
// Uses camelCase naming to match the RealWorld API specification.
// =============================================================================

using System.Text.Json.Serialization;

namespace Abies.Conduit.Services;

/// <summary>
/// Source-generated JSON context for Conduit API types.
/// Uses camelCase property naming to match the RealWorld API specification.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(CreateArticleRequest))]
[JsonSerializable(typeof(UpdateArticleRequest))]
[JsonSerializable(typeof(CreateCommentRequest))]
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
[JsonSerializable(typeof(ErrorResponse))]
internal partial class ConduitJsonContext : JsonSerializerContext;
