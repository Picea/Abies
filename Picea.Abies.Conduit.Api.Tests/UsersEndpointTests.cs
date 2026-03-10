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

public sealed class UsersEndpointTests : IClassFixture<ConduitApiFactory>
{
    private readonly ConduitApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UsersEndpointTests(ConduitApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidInput_ReturnsCreatedWithToken()
    {
        var request = new RegisterUserRequest(new RegisterUserBody(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "securepassword123"));

        var response = await _client.PostAsJsonAsync("/api/users", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("newuser@example.com", body.User.Email);
        Assert.Equal("newuser", body.User.Username);
        Assert.NotNull(body.User.Token);
        Assert.NotEmpty(body.User.Token);
    }

    [Fact]
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("loginuser@example.com", body.User.Email);
        Assert.Equal("loginuser", body.User.Username);
        Assert.NotNull(body.User.Token);
    }

    [Fact]
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

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonexistentUser_ReturnsValidationError()
    {
        var request = new LoginUserRequest(new LoginUserBody(
            Email: "nobody@example.com",
            Password: "whatever"));

        var response = await _client.PostAsJsonAsync("/api/users/login", request, JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
