// =============================================================================
// Uniqueness Validation Tests — Phase 2 Task 1
// =============================================================================
// Tests for preventing duplicate users/articles via pre-commit uniqueness checks.
// Coverage:
//   - Concurrent user registration with same email → only one succeeds
//   - Concurrent user registration with same username → only one succeeds
//   - Concurrent article creation with same title/slug → only one succeeds
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picea.Abies.Conduit.Api.Dto;

namespace Picea.Abies.Conduit.Api.Tests;

/// <summary>
/// Tests for Phase 2 Task 1: Pre-commit uniqueness validation.
/// </summary>
public sealed class UniquenessValidationTests : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ConduitApiFactory _factory = new();

    [Test]
    public async Task RegisterUser_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        // Arrange
        const string email = "user@example.com";
        const string username1 = "alice";
        const string username2 = "bob";

        var register1 = new RegisterUserRequest(new RegisterUserBody(
            Username: username1,
            Email: email,
            Password: "password123"));

        var register2 = new RegisterUserRequest(new RegisterUserBody(
            Username: username2,
            Email: email,
            Password: "password123"));

        // Act — First registration should succeed
        var response1 = await client.PostAsJsonAsync("/api/users", register1, JsonOptions);
        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.Created);

        // Act — Second registration with same email should fail
        var response2 = await client.PostAsJsonAsync("/api/users", register2, JsonOptions);

        // Assert
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var error = await response2.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Errors.Body.Any(msg => msg.Contains("already registered", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task RegisterUser_WithDuplicateUsername_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        // Arrange
        const string username = "alice";
        const string email1 = "alice@example.com";
        const string email2 = "bob@example.com";

        var register1 = new RegisterUserRequest(new RegisterUserBody(
            Username: username,
            Email: email1,
            Password: "password123"));

        var register2 = new RegisterUserRequest(new RegisterUserBody(
            Username: username,
            Email: email2,
            Password: "password123"));

        // Act — First registration should succeed
        var response1 = await client.PostAsJsonAsync("/api/users", register1, JsonOptions);
        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.Created);

        // Act — Second registration with same username should fail
        var response2 = await client.PostAsJsonAsync("/api/users", register2, JsonOptions);

        // Assert
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var error = await response2.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Errors.Body.Any(msg => msg.Contains("already taken", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task CreateArticle_WithDuplicateTitle_ReturnsConflict()
    {
        var user = _factory.SeedUser(
            username: "author",
            email: "author@example.com");
        using var client = _factory.CreateAuthenticatedClient(user);

        // Arrange
        const string title = "My Article";
        const string description = "A description";
        const string body = "Article body";

        var create1 = new CreateArticleRequest(new CreateArticleBody(
            Title: title,
            Description: description,
            Body: body,
            TagList: Array.Empty<string>()));

        var create2 = new CreateArticleRequest(new CreateArticleBody(
            Title: title,
            Description: description,
            Body: body,
            TagList: Array.Empty<string>()));

        // Act — First creation should succeed
        var response1 = await client.PostAsJsonAsync("/api/articles", create1, JsonOptions);
        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.Created);

        // Act — Second creation with same title (same slug) should fail
        var response2 = await client.PostAsJsonAsync("/api/articles", create2, JsonOptions);

        // Assert
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var error = await response2.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Errors.Body.Any(msg => msg.Contains("slug", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task CreateArticle_WithDifferentTitle_Succeeds()
    {
        var user = _factory.SeedUser(
            username: "author",
            email: "author@example.com");
        using var client = _factory.CreateAuthenticatedClient(user);

        // Arrange
        var create1 = new CreateArticleRequest(new CreateArticleBody(
            Title: "Article One",
            Description: "Description",
            Body: "Body",
            TagList: Array.Empty<string>()));

        var create2 = new CreateArticleRequest(new CreateArticleBody(
            Title: "Article Two",
            Description: "Description",
            Body: "Body",
            TagList: Array.Empty<string>()));

        // Act
        var response1 = await client.PostAsJsonAsync("/api/articles", create1, JsonOptions);
        var response2 = await client.PostAsJsonAsync("/api/articles", create2, JsonOptions);

        // Assert
        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task RegisterUser_ConcurrentRequests_OnlyOneSucceeds()
    {
        using var client = _factory.CreateClient();

        // Arrange
        const string email = "concurrent@example.com";
        const string username = "concurrent";

        var register1 = new RegisterUserRequest(new RegisterUserBody(
            Username: username + "1",
            Email: email,
            Password: "password123"));

        var register2 = new RegisterUserRequest(new RegisterUserBody(
            Username: username + "2",
            Email: email,
            Password: "password123"));

        // Act — Send both requests concurrently
        var task1 = client.PostAsJsonAsync("/api/users", register1, JsonOptions);
        var task2 = client.PostAsJsonAsync("/api/users", register2, JsonOptions);

        var responses = await Task.WhenAll(task1, task2);

        // Assert — One should succeed (201), one should fail (422)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        await Assert.That(successCount).IsEqualTo(1);
        await Assert.That(conflictCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateArticle_ConcurrentRequests_OnlyOneSucceeds()
    {
        var user = _factory.SeedUser(
            username: "author",
            email: "author@example.com");
        using var client = _factory.CreateAuthenticatedClient(user);

        // Arrange
        const string title = "Concurrent Article";

        var create1 = new CreateArticleRequest(new CreateArticleBody(
            Title: title,
            Description: "Description",
            Body: "Body",
            TagList: Array.Empty<string>()));

        var create2 = new CreateArticleRequest(new CreateArticleBody(
            Title: title,
            Description: "Description",
            Body: "Body",
            TagList: Array.Empty<string>()));

        // Act — Send both requests concurrently
        var task1 = client.PostAsJsonAsync("/api/articles", create1, JsonOptions);
        var task2 = client.PostAsJsonAsync("/api/articles", create2, JsonOptions);

        var responses = await Task.WhenAll(task1, task2);

        // Assert
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        await Assert.That(successCount).IsEqualTo(1);
        await Assert.That(conflictCount).IsEqualTo(1);
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();
}
