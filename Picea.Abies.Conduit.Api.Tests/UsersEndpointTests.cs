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
