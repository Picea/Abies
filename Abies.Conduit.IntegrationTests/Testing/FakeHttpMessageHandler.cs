using System.Net;
using System.Text;
using System.Text.Json;

namespace Abies.Conduit.IntegrationTests.Testing;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<(HttpMethod Method, string PathAndQuery), HttpResponseMessage> _routes = [];

    public List<(HttpMethod Method, Uri Uri, string? Body)> Requests { get; } = [];

    /// <summary>
    /// When true, the handler throws an exception for unregistered routes instead of returning 404.
    /// This helps catch missing routes early in tests.
    /// </summary>
    public bool StrictMode { get; set; } = false;

    public void When(HttpMethod method, string pathAndQuery, HttpStatusCode statusCode, object? jsonBody)
    {
        var response = new HttpResponseMessage(statusCode);
        if (jsonBody is not null)
        {
            var json = JsonSerializer.Serialize(jsonBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        _routes[(method, pathAndQuery)] = response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("RequestUri is required");
        var pathAndQuery = uri.PathAndQuery;

        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        Requests.Add((request.Method, uri, body));

        if (_routes.TryGetValue((request.Method, pathAndQuery), out var resp))
        {
            // Clone to avoid reusing disposed content between calls
            var clone = new HttpResponseMessage(resp.StatusCode);
            foreach (var header in resp.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            if (resp.Content is not null)
            {
                var content = await resp.Content.ReadAsStringAsync(cancellationToken);
                clone.Content = new StringContent(content, Encoding.UTF8, resp.Content.Headers.ContentType?.MediaType ?? "application/json");
            }
            return clone;
        }

        if (StrictMode)
        {
            throw new InvalidOperationException(
                $"FakeHttpMessageHandler (StrictMode): No route registered for {request.Method} {pathAndQuery}. " +
                $"Registered routes: [{string.Join(", ", _routes.Keys.Select(k => $"{k.Method} {k.PathAndQuery}"))}]");
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No fake route registered for {request.Method} {pathAndQuery}")
        };
    }
}
