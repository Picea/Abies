// =============================================================================
// Users Endpoints — Registration & Login (Public)
// =============================================================================
// POST /api/users       — Register a new user
// POST /api/users/login — Authenticate an existing user
//
// Neither endpoint requires authentication.
// =============================================================================

using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/users endpoints (registration and login).
/// </summary>
public static class UsersEndpoints
{
    /// <summary>Registers the /api/users endpoint group.</summary>
    public static RouteGroupBuilder MapUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users");

        group.MapPost("/", Register)
            .AllowAnonymous()
            .WithName("RegisterUser");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("LoginUser");

        return group;
    }

    /// <summary>
    /// POST /api/users — Register a new user account.
    /// </summary>
    private static async Task<IResult> Register(
        RegisterUserRequest request,
        HttpContext context,
        RequestIdempotencyStore idempotencyStore,
        AggregateStore aggregateStore,
        FindUserByEmail findUserByEmail,
        FindUserByUsername findUserByUsername,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        return await idempotencyStore.ExecuteAsync(
            context,
            async ct =>
            {
                var body = request.User;

                // Validate constrained types at the boundary
                var emailResult = EmailAddress.Create(body.Email);
                if (emailResult.IsErr)
                    return ApiErrors.FromUserError(emailResult.Error);

                var usernameResult = Username.Create(body.Username);
                if (usernameResult.IsErr)
                    return ApiErrors.FromUserError(usernameResult.Error);

                var passwordResult = Password.Create(body.Password);
                if (passwordResult.IsErr)
                    return ApiErrors.FromUserError(passwordResult.Error);

                // ─── Pre-commit Uniqueness Checks (Phase 2, Task 1) ──────────────────────
                // Check if email already exists (prevents duplicate registration)
                var existingByEmail = await findUserByEmail(emailResult.Value.Value, ct)
                    .ConfigureAwait(false);
                if (existingByEmail.IsSome)
                    return ApiErrors.FromUserError(new UserError.DuplicateEmail());

                // Check if username already exists (prevents duplicate registration)
                var existingByUsername = await findUserByUsername(usernameResult.Value.Value, ct)
                    .ConfigureAwait(false);
                if (existingByUsername.IsSome)
                    return ApiErrors.FromUserError(new UserError.DuplicateUsername());
                // ────────────────────────────────────────────────────────────────────────

                // Hash password at the boundary (capability pattern)
                var passwordHash = PasswordHasher.Hash(passwordResult.Value);

                // Generate user ID and timestamp (capabilities)
                var userId = UserId.New();
                var timestamp = Timestamp.Now();

                // Build and handle command
                var command = new UserCommand.Register(
                    userId, emailResult.Value, usernameResult.Value, passwordHash, timestamp);

                var result = await aggregateStore.HandleUniqueUserRegistration(
                    userId.Value,
                    command,
                    emailResult.Value.Value,
                    usernameResult.Value.Value,
                    ct).ConfigureAwait(false);

                return result.Match(
                    state =>
                    {
                        var token = jwtTokenService.GenerateToken(
                            userId.Value, state.Username.Value, state.Email.Value);

                        return Results.Created($"/api/user", new UserResponse(new UserDto(
                            Email: state.Email.Value,
                            Token: token,
                            Username: state.Username.Value,
                            Bio: state.Bio.Value,
                            Image: string.IsNullOrEmpty(state.Image.Value) ? null : state.Image.Value)));
                    },
                    error => ApiErrors.FromUserError(error));
            },
                    payloadFingerprintInput: System.Text.Json.JsonSerializer.Serialize(request),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// POST /api/users/login — Authenticate an existing user.
    /// </summary>
    private static async Task<IResult> Login(
        LoginUserRequest request,
        HttpContext context,
        RequestIdempotencyStore idempotencyStore,
        FindUserByEmail findUserByEmail,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        return await idempotencyStore.ExecuteAsync(
            context,
            async ct =>
            {
                var body = request.User;

                // Look up user by email in read model
                var userOption = await findUserByEmail(body.Email, ct).ConfigureAwait(false);

                return userOption.Match(
                    user =>
                    {
                        // Verify password
                        if (!PasswordHasher.Verify(body.Password, new PasswordHash(user.PasswordHash)))
                            return ApiErrors.Validation("Invalid email or password.");

                        var token = jwtTokenService.GenerateToken(user.Id, user.Username, user.Email);

                        return Results.Ok(new UserResponse(new UserDto(
                            Email: user.Email,
                            Token: token,
                            Username: user.Username,
                            Bio: user.Bio,
                            Image: string.IsNullOrEmpty(user.Image) ? null : user.Image)));
                    },
                    () => ApiErrors.Validation("Invalid email or password."));
            },
                    payloadFingerprintInput: System.Text.Json.JsonSerializer.Serialize(request),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
