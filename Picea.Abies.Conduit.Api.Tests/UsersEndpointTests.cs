// =============================================================================
// Users Endpoint Tests — Registration & Login
// =============================================================================
// Tests the public /api/users endpoints (register, login) including:
//   - Successful registration with JWT token response
//   - Successful login with JWT token response
//   - Validation errors for invalid input
//   - Login with wrong credentials
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;

namespace Picea.Abies.Conduit.Api.Tests;

public sealed class UsersEndpointTests : IAsyncDisposable
{
    private readonly ConduitApiFactory _factory = new();
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UsersEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task Register_ValidInput_ReturnsCreatedWithToken()
    {
        var request = new RegisterUserRequest(new RegisterUserBody(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "securepassword123"));

        var response = await _client.PostAsJsonAsync("/api/users", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.User.Email).IsEqualTo("newuser@example.com");
        await Assert.That(body.User.Username).IsEqualTo("newuser");
        await Assert.That(body.User.Token).IsNotNull();
        await Assert.That(body.User.Token).IsNotEmpty();
    }

    [Test]
    public async Task Register_WithSameIdempotencyKey_ReplaysInitialCreatedResponse()
    {
        var request = new RegisterUserRequest(new RegisterUserBody(
            Username: "idempotentuser",
            Email: "idempotent@example.com",
            Password: "securepassword123"));

        using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        firstMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "register-idempotency-001");

        using var firstResponse = await _client.SendAsync(firstMessage);
        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        secondMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "register-idempotency-001");

        using var secondResponse = await _client.SendAsync(secondMessage);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(secondResponse.Headers.Contains(RequestIdempotencyStore.ReplayHeaderName)).IsTrue();
        await Assert.That(secondBody).IsEqualTo(firstBody);
    }

    [Test]
    public async Task Register_WithSameIdempotencyKeyAndDifferentPayload_ReturnsValidationError()
    {
        var firstRequest = new RegisterUserRequest(new RegisterUserBody(
            Username: "idempotentuser-a",
            Email: "idempotent-a@example.com",
            Password: "securepassword123"));

        var secondRequest = new RegisterUserRequest(new RegisterUserBody(
            Username: "idempotentuser-b",
            Email: "idempotent-b@example.com",
            Password: "securepassword123"));

        using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(firstRequest, options: JsonOptions)
        };
        firstMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "register-idempotency-002");

        using var firstResponse = await _client.SendAsync(firstMessage);
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(secondRequest, options: JsonOptions)
        };
        secondMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "register-idempotency-002");

        using var secondResponse = await _client.SendAsync(secondMessage);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Login_WithSameIdempotencyKey_AcrossDifferentAnonymousClients_DoesNotReplayAcrossClients()
    {
        _factory.SeedUser(
            username: "idempotencycrossclient",
            email: "idempotencycrossclient@example.com",
            password: "correctpassword");

        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();

        var login = new LoginUserRequest(new LoginUserBody(
            Email: "idempotencycrossclient@example.com",
            Password: "correctpassword"));

        using var messageA = new HttpRequestMessage(HttpMethod.Post, "/api/users/login")
        {
            Content = JsonContent.Create(login, options: JsonOptions)
        };
        messageA.Headers.Add(RequestIdempotencyStore.HeaderName, "login-idempotency-cross-client-001");

        using var responseA = await clientA.SendAsync(messageA);
        await Assert.That(responseA.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(responseA.Headers.Contains(RequestIdempotencyStore.ReplayHeaderName)).IsFalse();

        using var messageB = new HttpRequestMessage(HttpMethod.Post, "/api/users/login")
        {
            Content = JsonContent.Create(login, options: JsonOptions)
        };
        messageB.Headers.Add(RequestIdempotencyStore.HeaderName, "login-idempotency-cross-client-001");

        using var responseB = await clientB.SendAsync(messageB);
        await Assert.That(responseB.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(responseB.Headers.Contains(RequestIdempotencyStore.ReplayHeaderName)).IsFalse();
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var user = _factory.SeedUser(
            username: "loginuser",
            email: "loginuser@example.com",
            password: "correctpassword");

        var request = new LoginUserRequest(new LoginUserBody(
            Email: "loginuser@example.com",
            Password: "correctpassword"));

        var response = await _client.PostAsJsonAsync("/api/users/login", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.User.Email).IsEqualTo("loginuser@example.com");
        await Assert.That(body.User.Username).IsEqualTo("loginuser");
        await Assert.That(body.User.Token).IsNotNull();
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsValidationError()
    {
        _factory.SeedUser(
            username: "wrongpwuser",
            email: "wrongpw@example.com",
            password: "correctpassword");

        var request = new LoginUserRequest(new LoginUserBody(
            Email: "wrongpw@example.com",
            Password: "wrongpassword"));

        var response = await _client.PostAsJsonAsync("/api/users/login", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Login_NonexistentUser_ReturnsValidationError()
    {
        var request = new LoginUserRequest(new LoginUserBody(
            Email: "nobody@example.com",
            Password: "whatever"));

        var response = await _client.PostAsJsonAsync("/api/users/login", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
