// =============================================================================
// ApiSeeder — HTTP-Based Test Data Seeding
// =============================================================================

using System.Net.Http.Json;
using System.Text.Json;

namespace Picea.Abies.Conduit.Testing.E2E.Helpers;

/// <summary>
/// Seeds test data via the Conduit REST API.
/// Each method returns the relevant response data for use in assertions.
/// Includes retry logic for transient infrastructure failures during startup.
/// </summary>
public sealed class ApiSeeder
{
    private readonly HttpClient _http;
    private readonly string _apiUrl;
    private const int MaxRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Creates a new seeder targeting the given API URL.</summary>
    public ApiSeeder(string apiUrl)
    {
        _apiUrl = apiUrl;
        _http = new HttpClient();
    }

    /// <summary>
    /// Registers a new user and returns their auth info.
    /// </summary>
    public async Task<UserSeedResult> RegisterUserAsync(
        string username,
        string email,
        string password)
    {
        var response = await SendWithRetryAsync(() =>
            _http.PostAsJsonAsync(
                $"{_apiUrl}/api/users",
                new { user = new { username, email, password } },
                _jsonOptions));

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        return new UserSeedResult(
            body!.User.Username,
            body.User.Email,
            body.User.Token,
            body.User.Bio,
            body.User.Image);
    }

    /// <summary>
    /// Logs in an existing user and returns their auth info.
    /// </summary>
    public async Task<UserSeedResult> LoginUserAsync(string email, string password)
    {
        var response = await SendWithRetryAsync(() =>
            _http.PostAsJsonAsync(
                $"{_apiUrl}/api/users/login",
                new { user = new { email, password } },
                _jsonOptions));

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        return new UserSeedResult(
            body!.User.Username,
            body.User.Email,
            body.User.Token,
            body.User.Bio,
            body.User.Image);
    }

    /// <summary>
    /// Creates an article via the API and returns its slug.
    /// </summary>
    public async Task<ArticleSeedResult> CreateArticleAsync(
        string token,
        string title,
        string description,
        string body,
        string[]? tagList = null)
    {
        var response = await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/api/articles");
            request.Headers.Add("Authorization", $"Token {token}");
            request.Content = JsonContent.Create(
                new { article = new { title, description, body, tagList } },
                options: _jsonOptions);
            return _http.SendAsync(request);
        });

        var result = await response.Content.ReadFromJsonAsync<ArticleResponse>(_jsonOptions);
        return new ArticleSeedResult(
            result!.Article.Slug,
            result.Article.Title,
            result.Article.Description,
            result.Article.Body);
    }

    /// <summary>
    /// Adds a comment to an article via the API and returns the comment ID.
    /// </summary>
    public async Task<CommentSeedResult> AddCommentAsync(
        string token,
        string slug,
        string commentBody)
    {
        var response = await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, $"{_apiUrl}/api/articles/{slug}/comments");
            request.Headers.Add("Authorization", $"Token {token}");
            request.Content = JsonContent.Create(
                new { comment = new { body = commentBody } },
                options: _jsonOptions);
            return _http.SendAsync(request);
        });

        var result = await response.Content.ReadFromJsonAsync<CommentResponse>(_jsonOptions);
        return new CommentSeedResult(result!.Comment.Id, result.Comment.Body);
    }

    /// <summary>
    /// Follows a user via the API.
    /// </summary>
    public async Task FollowUserAsync(string token, string username)
    {
        await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, $"{_apiUrl}/api/profiles/{username}/follow");
            request.Headers.Add("Authorization", $"Token {token}");
            return _http.SendAsync(request);
        });
    }

    /// <summary>
    /// Favorites an article via the API.
    /// </summary>
    public async Task FavoriteArticleAsync(string token, string slug)
    {
        await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, $"{_apiUrl}/api/articles/{slug}/favorite");
            request.Headers.Add("Authorization", $"Token {token}");
            return _http.SendAsync(request);
        });
    }

    /// <summary>
    /// Updates the current user via the API and returns the resulting user data.
    /// </summary>
    public async Task<UserSeedResult> UpdateUserAsync(
        string token,
        string? image = null,
        string? username = null,
        string? bio = null,
        string? email = null,
        string? password = null)
    {
        var response = await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{_apiUrl}/api/user");
            request.Headers.Add("Authorization", $"Token {token}");
            request.Content = JsonContent.Create(
                new { user = new { image, username, bio, email, password } },
                options: _jsonOptions);
            return _http.SendAsync(request);
        });

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        return new UserSeedResult(
            body!.User.Username,
            body.User.Email,
            body.User.Token,
            body.User.Bio,
            body.User.Image);
    }

    // ─── Read-After-Write Consistency Helpers ──────────────────────────────────

    /// <summary>
    /// Waits until a profile is available in the read model.
    /// </summary>
    public async Task WaitForProfileAsync(string username, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await _http.GetAsync($"{_apiUrl}/api/profiles/{username}");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Transient error
            }
            await Task.Delay(500);
        }
        throw new TimeoutException(
            $"Profile '{username}' not available in read model after {timeoutSeconds}s.");
    }

    /// <summary>
    /// Waits until an article is available in the read model.
    /// </summary>
    public async Task WaitForArticleAsync(string slug, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await _http.GetAsync($"{_apiUrl}/api/articles/{slug}");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Transient error
            }
            await Task.Delay(500);
        }
        throw new TimeoutException(
            $"Article '{slug}' not available in read model after {timeoutSeconds}s.");
    }

    /// <summary>
    /// Waits until an article is no longer available in the read model.
    /// </summary>
    public async Task WaitForArticleDeletedAsync(string slug, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await _http.GetAsync($"{_apiUrl}/api/articles/{slug}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return;
            }
            catch
            {
                // Transient error
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Article '{slug}' still available in read model after {timeoutSeconds}s.");
    }

    /// <summary>
    /// Waits until an article is available AND its title matches (for update consistency).
    /// </summary>
    public async Task WaitForArticleWithTitleAsync(string slug, string expectedTitle, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        string lastStatus = "no attempt";
        string lastBody = "";
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await _http.GetAsync($"{_apiUrl}/api/articles/{slug}");
                lastBody = await response.Content.ReadAsStringAsync();
                lastStatus = $"{(int)response.StatusCode} {response.StatusCode}";
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ArticleResponse>(lastBody, _jsonOptions);
                    if (result?.Article.Title == expectedTitle)
                        return;
                    lastStatus += $" (title mismatch: got '{result?.Article.Title}')";
                }
            }
            catch (Exception ex)
            {
                lastStatus = $"exception: {ex.Message}";
            }
            await Task.Delay(500);
        }
        throw new TimeoutException(
            $"Article '{slug}' with title '{expectedTitle}' not available after {timeoutSeconds}s. " +
            $"Last status: {lastStatus}. Last body (first 300 chars): {(lastBody.Length > 300 ? lastBody[..300] : lastBody)}");
    }

    /// <summary>
    /// Retries transient failures (5xx, connection errors) with exponential backoff.
    /// Non-transient errors (4xx) are thrown immediately.
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> sendAsync)
    {
        HttpResponseMessage? lastResponse = null;
        string? lastErrorBody = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(RetryDelay * attempt);

            try
            {
                var response = await sendAsync();

                // 4xx errors are not transient — fail immediately
                if ((int)response.StatusCode is >= 400 and < 500)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"API returned {(int)response.StatusCode}: {body}");
                }

                // Success
                if (response.IsSuccessStatusCode)
                    return response;

                // 5xx — transient, retry
                lastResponse = response;
                lastErrorBody = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException) when (lastResponse is null)
            {
                // Connection failure — transient, retry
                if (attempt == MaxRetries)
                    throw;
            }
        }

        throw new HttpRequestException(
            $"API request failed after {MaxRetries + 1} attempts. " +
            $"Last status: {lastResponse?.StatusCode}. Body: {lastErrorBody}");
    }

    // ─── Response DTOs (private, seeder-internal) ──────────────────────────────

    private sealed record UserResponse(UserDto User);
    private sealed record UserDto(string Email, string Token, string Username, string Bio, string? Image);

    private sealed record ArticleResponse(ArticleDto Article);
    private sealed record ArticleDto(string Slug, string Title, string Description, string Body);

    private sealed record CommentResponse(CommentDto Comment);
    private sealed record CommentDto(Guid Id, string Body);
}

// ─── Seed Result Records ────────────────────────────────────────────────────────────

/// <summary>Seeded user data.</summary>
public sealed record UserSeedResult(
    string Username,
    string Email,
    string Token,
    string Bio,
    string? Image);

/// <summary>Seeded article data.</summary>
public sealed record ArticleSeedResult(
    string Slug,
    string Title,
    string Description,
    string Body);

/// <summary>Seeded comment data.</summary>
public sealed record CommentSeedResult(Guid Id, string Body);
