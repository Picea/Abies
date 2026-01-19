using Abies.Conduit.Main;
using Abies.Conduit.Page.Home;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Abies.Conduit.Services;

public static class ApiClient
{
    private static HttpClient Client = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    
    private static string BaseUrl = "http://localhost:5179/api";

    /// <summary>
    /// Override the underlying <see cref="HttpClient"/> used by the Conduit UI.
    /// Intended for tests (near-E2E integration) to plug in a fake handler.
    /// </summary>
    public static void ConfigureHttpClient(HttpClient httpClient)
    {
        Client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Override the API base URL used by the Conduit UI.
    /// Intended for tests or custom deployments.
    /// </summary>
    public static void ConfigureBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));
        BaseUrl = baseUrl.TrimEnd('/');
    }

    public static void SetAuthToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Client.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        }
    }

    // Authentication

    public static async Task<UserResponse> LoginAsync(string email, string password)
    {
        var loginRequest = new
        {
            user = new
            {
                email,
                password
            }
        };

        return await PostAsync<UserResponse>("/users/login", loginRequest);
    }

    public static async Task<UserResponse> RegisterAsync(string username, string email, string password)
    {
        var registerRequest = new
        {
            user = new
            {
                username,
                email,
                password
            }
        };

        return await PostAsync<UserResponse>("/users", registerRequest);
    }

    public static async Task<UserResponse> GetCurrentUserAsync()
    {
        return await GetAsync<UserResponse>("/user");
    }

    public static async Task<UserResponse> UpdateUserAsync(string email, string username, string bio, string image, string? password = null)
    {
        var updateRequest = new
        {
            user = new
            {
                email,
                username,
                bio,
                image,
                password
            }
        };

        return await PutAsync<UserResponse>("/user", updateRequest);
    }

    // Articles

public static async Task<ArticlesResponse> GetArticlesAsync(string? tag = null, string? author = null, string? favoritedBy = null, int limit = 10, int offset = 0)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(tag)) queryParams.Add($"tag={tag}");
        if (!string.IsNullOrEmpty(author)) queryParams.Add($"author={author}");
        if (!string.IsNullOrEmpty(favoritedBy)) queryParams.Add($"favorited={favoritedBy}");
        queryParams.Add($"limit={limit}");
        queryParams.Add($"offset={offset}");

        var url = $"/articles?{string.Join("&", queryParams)}";
        return await GetAsync<ArticlesResponse>(url);
    }

    public static async Task<ArticlesResponse> GetFeedArticlesAsync(int limit = 10, int offset = 0)
    {
        var url = $"/articles/feed?limit={limit}&offset={offset}";
        return await GetAsync<ArticlesResponse>(url);
    }

    public static async Task<ArticleResponse> GetArticleAsync(string slug)
    {
        return await GetAsync<ArticleResponse>($"/articles/{slug}");
    }

    public static async Task<ArticleResponse> CreateArticleAsync(string title, string description, string body, List<string> tagList)
    {
        var createRequest = new
        {
            article = new
            {
                title,
                description,
                body,
                tagList
            }
        };

        return await PostAsync<ArticleResponse>("/articles", createRequest);
    }

    public static async Task<ArticleResponse> UpdateArticleAsync(string slug, string title, string description, string body)
    {
        var updateRequest = new
        {
            article = new
            {
                title,
                description,
                body
            }
        };

        return await PutAsync<ArticleResponse>($"/articles/{slug}", updateRequest);
    }

    public static async Task DeleteArticleAsync(string slug)
    {
        await DeleteAsync($"/articles/{slug}");
    }

    public static async Task<ArticleResponse> FavoriteArticleAsync(string slug)
    {
        return await PostAsync<ArticleResponse>($"/articles/{slug}/favorite", null);
    }

    public static async Task<ArticleResponse> UnfavoriteArticleAsync(string slug)
    {
        return await DeleteAsync<ArticleResponse>($"/articles/{slug}/favorite");
    }

    // Comments

    public static async Task<CommentsResponse> GetCommentsAsync(string slug)
    {
        return await GetAsync<CommentsResponse>($"/articles/{slug}/comments");
    }

    public static async Task<CommentResponse> AddCommentAsync(string slug, string body)
    {
        var createRequest = new
        {
            comment = new
            {
                body
            }
        };

        return await PostAsync<CommentResponse>($"/articles/{slug}/comments", createRequest);
    }

    public static async Task DeleteCommentAsync(string slug, string id)
    {
        await DeleteAsync($"/articles/{slug}/comments/{id}");
    }

    // Profiles

    public static async Task<ProfileResponse> GetProfileAsync(string username)
    {
        return await GetAsync<ProfileResponse>($"/profiles/{username}");
    }

    public static async Task<ProfileResponse> FollowUserAsync(string username)
    {
        return await PostAsync<ProfileResponse>($"/profiles/{username}/follow", null);
    }

    public static async Task<ProfileResponse> UnfollowUserAsync(string username)
    {
        return await DeleteAsync<ProfileResponse>($"/profiles/{username}/follow");
    }

    // Tags

    public static async Task<TagsResponse> GetTagsAsync()
    {
        return await GetAsync<TagsResponse>("/tags");
    }

    // Helper methods

    private static async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await Client.GetAsync($"{BaseUrl}{endpoint}");
        return await ProcessResponseAsync<T>(response);
    }

    private static async Task<T> PostAsync<T>(string endpoint, object? data)
    {
        var content = data != null
            ? new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json")
            : new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"{BaseUrl}{endpoint}", content);
        return await ProcessResponseAsync<T>(response);
    }

    private static async Task<T> PutAsync<T>(string endpoint, object data)
    {
        var content = new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json");
        var response = await Client.PutAsync($"{BaseUrl}{endpoint}", content);
        return await ProcessResponseAsync<T>(response);
    }

    private static async Task<T> DeleteAsync<T>(string endpoint)
    {
        var response = await Client.DeleteAsync($"{BaseUrl}{endpoint}");
        return await ProcessResponseAsync<T>(response);
    }

    private static async Task DeleteAsync(string endpoint)
    {
        var response = await Client.DeleteAsync($"{BaseUrl}{endpoint}");
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API error: {response.StatusCode}, {errorContent}");
        }
    }

    private static async Task<T> ProcessResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }
        if (!response.IsSuccessStatusCode)
        {
            // Try to parse error response
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, JsonOptions);
                if (errorResponse == null || errorResponse.Errors.Count == 0)
                {
                    throw new HttpRequestException($"API error: {response.StatusCode}, {content}");
                }
                throw new ApiException(errorResponse.Errors);
            }
            catch (JsonException)
            {
                throw new HttpRequestException($"API error: {response.StatusCode}, {content}");
            }
        }

        var result = JsonSerializer.Deserialize<T>(content, JsonOptions) 
            ?? throw new JsonException($"Failed to deserialize response: {content}");
        return result;
    }
}

// Response models
public class ErrorResponse
{
    public Dictionary<string, string[]> Errors { get; set; } = new();
}

public class UserResponse
{
    public UserData User { get; set; } = default!;
}

public class UserData
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; } = string.Empty;
    public string? Image { get; set; } = string.Empty;
}

public class ArticlesResponse
{
    public List<ArticleData> Articles { get; set; } = new();
    public int ArticlesCount { get; set; }
}

public class ArticleResponse
{
    public ArticleData Article { get; set; } = default!;
}

public class ArticleData
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> TagList { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public bool Favorited { get; set; }
    public int FavoritesCount { get; set; }
    public ProfileData Author { get; set; } = default!;
}

public class CommentsResponse
{
    public List<CommentData> Comments { get; set; } = new();
}

public class CommentResponse
{
    public CommentData Comment { get; set; } = default!;
}

public class CommentData
{
    public int Id { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public ProfileData Author { get; set; } = default!;
}

public class ProfileResponse
{
    public ProfileData Profile { get; set; } = default!;
}

public class ProfileData
{
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; } = string.Empty;
    public string? Image { get; set; } = string.Empty;
    public bool Following { get; set; }
}

public class TagsResponse
{
    public List<string> Tags { get; set; } = new();
}

public class ApiException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ApiException(Dictionary<string, string[]> errors)
    {
        Errors = errors;
    }
}

public class UnauthorizedException : Exception
{
}
