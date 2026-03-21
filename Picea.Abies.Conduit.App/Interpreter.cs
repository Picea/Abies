// =============================================================================
// Interpreter — HTTP Command Interpreter for the Conduit Frontend
// =============================================================================

using System.Net.Http.Json;
using Picea;

namespace Picea.Abies.Conduit.App;

public static class ConduitInterpreter
{
    private static readonly HttpClient _http = new();

    public static async ValueTask<Result<Message[], PipelineError>> Interpret(Command command)
    {
        try
        {
            Message[] messages = command switch
            {
                FetchArticles cmd => await HandleFetchArticles(cmd),
                FetchFeed cmd => await HandleFetchFeed(cmd),
                FetchArticle cmd => await HandleFetchArticle(cmd),
                FetchComments cmd => await HandleFetchComments(cmd),
                FetchTags cmd => await HandleFetchTags(cmd),
                LoginUser cmd => await HandleLogin(cmd),
                RegisterUser cmd => await HandleRegister(cmd),
                FetchProfile cmd => await HandleFetchProfile(cmd),
                FavoriteArticle cmd => await HandleFavorite(cmd),
                UnfavoriteArticle cmd => await HandleUnfavorite(cmd),
                FollowUser cmd => await HandleFollow(cmd),
                UnfollowUser cmd => await HandleUnfollow(cmd),
                AddComment cmd => await HandleAddComment(cmd),
                DeleteCommentCommand cmd => await HandleDeleteComment(cmd),
                DeleteArticleCommand cmd => await HandleDeleteArticle(cmd),
                UpdateUser cmd => await HandleUpdateUser(cmd),
                CreateArticle cmd => await HandleCreateArticle(cmd),
                UpdateArticle cmd => await HandleUpdateArticle(cmd),
                _ => []
            };
            return Result<Message[], PipelineError>.Ok(messages);
        }
        catch (Exception ex)
        {
            return Result<Message[], PipelineError>.Ok(
                [new ApiError([$"Network error: {ex.Message}"])]);
        }
    }

    private static async Task<Message[]> HandleFetchArticles(FetchArticles cmd)
    {
        var query = BuildArticleQuery(cmd.Limit, cmd.Offset, cmd.Tag, cmd.Author, cmd.Favorited);
        using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/articles{query}", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.MultipleArticlesDto);
        return dto is null ? [] : [new ArticlesLoaded(dto.Articles.Select(MapArticlePreview).ToList(), dto.ArticlesCount)];
    }

    private static async Task<Message[]> HandleFetchFeed(FetchFeed cmd)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/articles/feed?limit={cmd.Limit}&offset={cmd.Offset}", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.MultipleArticlesDto);
        return dto is null ? [] : [new ArticlesLoaded(dto.Articles.Select(MapArticlePreview).ToList(), dto.ArticlesCount)];
    }

    private static async Task<Message[]> HandleFetchArticle(FetchArticle cmd)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.SingleArticleDto);
        return dto?.Article is null ? [] : [new ArticleLoaded(MapArticle(dto.Article))];
    }

    private static async Task<Message[]> HandleFavorite(FavoriteArticle cmd)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}/favorite", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.SingleArticleDto);
        return dto?.Article is null ? [] : [new FavoriteToggled(MapArticlePreview(dto.Article))];
    }

    private static async Task<Message[]> HandleUnfavorite(UnfavoriteArticle cmd)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}/favorite", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.SingleArticleDto);
        return dto?.Article is null ? [] : [new FavoriteToggled(MapArticlePreview(dto.Article))];
    }

    private static async Task<Message[]> HandleDeleteArticle(DeleteArticleCommand cmd)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}", cmd.Token);
        using var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode ? [new ArticleDeleted()] : [new ApiError(await ReadErrors(response))];
    }

    private static async Task<Message[]> HandleFetchComments(FetchComments cmd)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}/comments", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.MultipleCommentsDto);
        return dto is null ? [] : [new CommentsLoaded(dto.Comments.Select(MapComment).ToList())];
    }

    private static async Task<Message[]> HandleAddComment(AddComment cmd)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}/comments", cmd.Token);
        request.Content = JsonContent.Create(new CommentRequest(new CommentPayload(cmd.Body)), ConduitJsonContext.Default.CommentRequest);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.SingleCommentDto);
        return dto?.Comment is null ? [] : [new CommentAdded(MapComment(dto.Comment))];
    }

    private static async Task<Message[]> HandleDeleteComment(DeleteCommentCommand cmd)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}/comments/{cmd.CommentId}", cmd.Token);
        using var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode ? [new CommentDeleted(cmd.CommentId)] : [new ApiError(await ReadErrors(response))];
    }

    private static async Task<Message[]> HandleFetchTags(FetchTags cmd)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/tags", null);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.TagsDto);
        return dto is null ? [] : [new TagsLoaded(dto.Tags)];
    }

    private static async Task<Message[]> HandleLogin(LoginUser cmd)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{cmd.ApiUrl}/api/users/login", null);
        request.Content = JsonContent.Create(new LoginRequest(new LoginUserPayload(cmd.Email, cmd.Password)), ConduitJsonContext.Default.LoginRequest);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.UserResponseDto);
        return dto?.User is null ? [] : [new UserAuthenticated(new Session(dto.User.Token, dto.User.Username, dto.User.Email, dto.User.Bio, dto.User.Image))];
    }

    private static async Task<Message[]> HandleRegister(RegisterUser cmd)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{cmd.ApiUrl}/api/users", null);
        request.Content = JsonContent.Create(new RegisterRequest(new RegisterUserPayload(cmd.Username, cmd.Email, cmd.Password)), ConduitJsonContext.Default.RegisterRequest);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.UserResponseDto);
        return dto?.User is null ? [] : [new UserAuthenticated(new Session(dto.User.Token, dto.User.Username, dto.User.Email, dto.User.Bio, dto.User.Image))];
    }

    private static async Task<Message[]> HandleFetchProfile(FetchProfile cmd)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/profiles/{cmd.Username}", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.ProfileResponseDto);
        return dto?.Profile is null ? [] : [new ProfileLoaded(MapProfile(dto.Profile))];
    }

    private static async Task<Message[]> HandleFollow(FollowUser cmd)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{cmd.ApiUrl}/api/profiles/{cmd.Username}/follow", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.ProfileResponseDto);
        return dto?.Profile is null ? [] : [new FollowToggled(MapProfile(dto.Profile))];
    }

    private static async Task<Message[]> HandleUnfollow(UnfollowUser cmd)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"{cmd.ApiUrl}/api/profiles/{cmd.Username}/follow", cmd.Token);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.ProfileResponseDto);
        return dto?.Profile is null ? [] : [new FollowToggled(MapProfile(dto.Profile))];
    }

    private static async Task<Message[]> HandleUpdateUser(UpdateUser cmd)
    {
        using var request = CreateRequest(HttpMethod.Put, $"{cmd.ApiUrl}/api/user", cmd.Token);
        request.Content = JsonContent.Create(new UpdateUserRequest(new UpdateUserPayload(cmd.Image, cmd.Username, cmd.Bio, cmd.Email, cmd.Password)), ConduitJsonContext.Default.UpdateUserRequest);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.UserResponseDto);
        return dto?.User is null ? [] : [new UserUpdated(new Session(dto.User.Token, dto.User.Username, dto.User.Email, dto.User.Bio, dto.User.Image))];
    }

    private static async Task<Message[]> HandleCreateArticle(CreateArticle cmd)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{cmd.ApiUrl}/api/articles", cmd.Token);
        request.Content = JsonContent.Create(new ArticleRequest(new ArticlePayload(cmd.Title, cmd.Description, cmd.Body, cmd.TagList)), ConduitJsonContext.Default.ArticleRequest);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.SingleArticleDto);
        return dto?.Article is null ? [] : [new ArticleSaved(dto.Article.Slug)];
    }

    private static async Task<Message[]> HandleUpdateArticle(UpdateArticle cmd)
    {
        using var request = CreateRequest(HttpMethod.Put, $"{cmd.ApiUrl}/api/articles/{cmd.Slug}", cmd.Token);
        request.Content = JsonContent.Create(new ArticleRequest(new ArticlePayload(cmd.Title, cmd.Description, cmd.Body, cmd.TagList)), ConduitJsonContext.Default.ArticleRequest);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [new ApiError(await ReadErrors(response))];
        var dto = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.SingleArticleDto);
        return dto?.Article is null ? [] : [new ArticleSaved(dto.Article.Slug)];
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);
        return request;
    }

    private static string BuildArticleQuery(int limit, int offset, string? tag, string? author, string? favorited)
    {
        var parts = new List<string> { $"limit={limit}", $"offset={offset}" };
        if (tag is not null)
            parts.Add($"tag={Uri.EscapeDataString(tag)}");
        if (author is not null)
            parts.Add($"author={Uri.EscapeDataString(author)}");
        if (favorited is not null)
            parts.Add($"favorited={Uri.EscapeDataString(favorited)}");
        return "?" + string.Join("&", parts);
    }

    private static async Task<IReadOnlyList<string>> ReadErrors(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync(ConduitJsonContext.Default.ErrorResponseDto);
            return body?.Errors?.Body ?? [$"HTTP {(int)response.StatusCode}"];
        }
        catch { return [$"HTTP {(int)response.StatusCode}"]; }
    }

    private static ArticlePreviewData MapArticlePreview(ArticleListItemDto dto) =>
        new(dto.Slug, dto.Title, dto.Description, dto.TagList, dto.CreatedAt, dto.UpdatedAt, dto.Favorited, dto.FavoritesCount, MapAuthor(dto.Author));

    private static ArticlePreviewData MapArticlePreview(ArticleItemDto dto) =>
        new(dto.Slug, dto.Title, dto.Description, dto.TagList, dto.CreatedAt, dto.UpdatedAt, dto.Favorited, dto.FavoritesCount, MapAuthor(dto.Author));

    private static ArticleData MapArticle(ArticleItemDto dto) =>
        new(dto.Slug, dto.Title, dto.Description, dto.Body, dto.TagList, dto.CreatedAt, dto.UpdatedAt, dto.Favorited, dto.FavoritesCount, MapAuthor(dto.Author));

    private static CommentData MapComment(CommentItemDto dto) =>
        new(dto.Id, dto.CreatedAt, dto.UpdatedAt, dto.Body, MapAuthor(dto.Author));

    private static AuthorData MapAuthor(ProfileItemDto dto) =>
        new(dto.Username, dto.Bio, dto.Image, dto.Following);

    private static ProfileData MapProfile(ProfileItemDto dto) =>
        new(dto.Username, dto.Bio, dto.Image, dto.Following);
}

// ─── Internal DTOs ────────────────────────────────────────────────────────

internal sealed record MultipleArticlesDto(IReadOnlyList<ArticleListItemDto> Articles, int ArticlesCount);
internal sealed record SingleArticleDto(ArticleItemDto Article);
internal sealed record ArticleItemDto(string Slug, string Title, string Description, string Body, IReadOnlyList<string> TagList, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, bool Favorited, int FavoritesCount, ProfileItemDto Author);
internal sealed record ArticleListItemDto(string Slug, string Title, string Description, IReadOnlyList<string> TagList, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, bool Favorited, int FavoritesCount, ProfileItemDto Author);
internal sealed record MultipleCommentsDto(IReadOnlyList<CommentItemDto> Comments);
internal sealed record SingleCommentDto(CommentItemDto Comment);
internal sealed record CommentItemDto(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string Body, ProfileItemDto Author);
internal sealed record ProfileResponseDto(ProfileItemDto Profile);
internal sealed record ProfileItemDto(string Username, string Bio, string? Image, bool Following);
internal sealed record UserResponseDto(UserItemDto User);
internal sealed record UserItemDto(string Email, string Token, string Username, string Bio, string? Image);
internal sealed record ErrorResponseDto(ErrorBodyDto? Errors);
internal sealed record ErrorBodyDto(IReadOnlyList<string>? Body);
internal sealed record TagsDto(IReadOnlyList<string> Tags);
