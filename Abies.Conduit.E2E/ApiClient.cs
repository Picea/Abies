using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Abies.Conduit.E2E;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };
    }

    public async Task<UserInfo> RegisterUserAsync(string? email = null, string? username = null)
    {
        email ??= $"e2e{Guid.NewGuid():N}@example.com";
        username ??= $"user{Guid.NewGuid():N}".Substring(0, 16);
        var payload = new
        {
            user = new
            {
                username,
                email,
                password = "Password1!"
            }
        };
        var resp = await _http.PostAsync("/api/users", AsJson(payload));
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("user").GetProperty("token").GetString() ?? string.Empty;
        return new UserInfo(email, username, token);
    }

    public async Task<string> CreateArticleAsync(string token, string title, string description, string body, IEnumerable<string>? tags = null)
    {
        var payload = new
        {
            article = new
            {
                title,
                description,
                body,
                tagList = tags?.ToArray() ?? []
            }
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = AsJson(payload)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        var slug = doc.RootElement.GetProperty("article").GetProperty("slug").GetString() ?? string.Empty;
        return slug;
    }

    public record UserInfo(string Email, string Username, string Token);

    private StringContent AsJson(object obj) =>
        new(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");
}
