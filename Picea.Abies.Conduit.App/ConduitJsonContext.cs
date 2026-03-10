// =============================================================================
// ConduitJsonContext — Source-Generated JSON Serialization
// =============================================================================

using System.Text.Json.Serialization;

namespace Picea.Abies.Conduit.App;

internal sealed record LoginRequest(LoginUserPayload User);
internal sealed record LoginUserPayload(string Email, string Password);
internal sealed record RegisterRequest(RegisterUserPayload User);
internal sealed record RegisterUserPayload(string Username, string Email, string Password);
internal sealed record CommentRequest(CommentPayload Comment);
internal sealed record CommentPayload(string Body);
internal sealed record UpdateUserRequest(UpdateUserPayload User);
internal sealed record UpdateUserPayload(
    string? Image, string? Username, string? Bio, string? Email,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Password);
internal sealed record ArticleRequest(ArticlePayload Article);
internal sealed record ArticlePayload(string Title, string Description, string Body, IReadOnlyList<string> TagList);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(MultipleArticlesDto))]
[JsonSerializable(typeof(SingleArticleDto))]
[JsonSerializable(typeof(MultipleCommentsDto))]
[JsonSerializable(typeof(SingleCommentDto))]
[JsonSerializable(typeof(ProfileResponseDto))]
[JsonSerializable(typeof(UserResponseDto))]
[JsonSerializable(typeof(ErrorResponseDto))]
[JsonSerializable(typeof(TagsDto))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(CommentRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(ArticleRequest))]
internal sealed partial class ConduitJsonContext : JsonSerializerContext;
