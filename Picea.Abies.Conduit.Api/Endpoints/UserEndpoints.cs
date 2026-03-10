// =============================================================================
// User Endpoints — Current User Operations (Authenticated)
// =============================================================================
// GET /api/user  — Get the current user
// PUT /api/user  — Update the current user
//
// Both endpoints require authentication.
// =============================================================================

using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadModel;
using Picea;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/user endpoints (current user get and update).
/// </summary>
public static class UserEndpoints
{
    /// <summary>Registers the /api/user endpoint group.</summary>
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/user")
            .RequireAuthorization();

        group.MapGet("/", GetCurrentUser)
            .WithName("GetCurrentUser");

        group.MapPut("/", UpdateCurrentUser)
            .WithName("UpdateCurrentUser");

        return group;
    }

    /// <summary>
    /// GET /api/user — Returns the currently authenticated user.
    /// </summary>
    private static async Task<IResult> GetCurrentUser(
        HttpContext context,
        FindUserById findUserById,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        var userId = JwtTokenService.GetUserId(context.User);
        if (userId is null)
            return ApiErrors.Unauthorized();

        var userOption = await findUserById(userId.Value, cancellationToken).ConfigureAwait(false);

        return userOption.Match(
            user =>
            {
                var token = jwtTokenService.GenerateToken(user.Id, user.Username, user.Email);

                return Results.Ok(new UserResponse(new UserDto(
                    Email: user.Email,
                    Token: token,
                    Username: user.Username,
                    Bio: user.Bio,
                    Image: string.IsNullOrEmpty(user.Image) ? null : user.Image)));
            },
            () => ApiErrors.NotFound("User not found."));
    }

    /// <summary>
    /// PUT /api/user — Updates the currently authenticated user.
    /// </summary>
    private static async Task<IResult> UpdateCurrentUser(
        UpdateUserRequest request,
        HttpContext context,
        AggregateStore aggregateStore,
        FindUserById findUserById,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        var userId = JwtTokenService.GetUserId(context.User);
        if (userId is null)
            return ApiErrors.Unauthorized();

        var body = request.User;

        // Build optional constrained types for each provided field
        var emailOption = Option<EmailAddress>.None;
        if (body.Email is not null)
        {
            var emailResult = EmailAddress.Create(body.Email);
            if (emailResult.IsErr)
                return ApiErrors.FromUserError(emailResult.Error);
            emailOption = Option<EmailAddress>.Some(emailResult.Value);
        }

        var usernameOption = Option<Username>.None;
        if (body.Username is not null)
        {
            var usernameResult = Username.Create(body.Username);
            if (usernameResult.IsErr)
                return ApiErrors.FromUserError(usernameResult.Error);
            usernameOption = Option<Username>.Some(usernameResult.Value);
        }

        var passwordHashOption = Option<PasswordHash>.None;
        if (body.Password is not null)
        {
            var passwordResult = Password.Create(body.Password);
            if (passwordResult.IsErr)
                return ApiErrors.FromUserError(passwordResult.Error);
            passwordHashOption = Option<PasswordHash>.Some(PasswordHasher.Hash(passwordResult.Value));
        }

        var bioOption = Option<Bio>.None;
        if (body.Bio is not null)
        {
            var bioResult = Bio.Create(body.Bio);
            if (bioResult.IsErr)
                return ApiErrors.FromUserError(bioResult.Error);
            bioOption = Option<Bio>.Some(bioResult.Value);
        }

        var imageOption = Option<ImageUrl>.None;
        if (body.Image is not null)
        {
            if (body.Image == string.Empty)
            {
                imageOption = Option<ImageUrl>.Some(ImageUrl.Empty);
            }
            else
            {
                var imageResult = ImageUrl.Create(body.Image);
                if (imageResult.IsErr)
                    return ApiErrors.FromUserError(imageResult.Error);
                imageOption = Option<ImageUrl>.Some(imageResult.Value);
            }
        }

        var command = new UserCommand.UpdateProfile(
            emailOption, usernameOption, passwordHashOption,
            bioOption, imageOption, Timestamp.Now());

        var result = await aggregateStore.HandleUserCommand(
            userId.Value, command, cancellationToken).ConfigureAwait(false);

        return result.Match(
            state =>
            {
                var token = jwtTokenService.GenerateToken(
                    userId.Value, state.Username.Value, state.Email.Value);

                return Results.Ok(new UserResponse(new UserDto(
                    Email: state.Email.Value,
                    Token: token,
                    Username: state.Username.Value,
                    Bio: state.Bio.Value,
                    Image: string.IsNullOrEmpty(state.Image.Value) ? null : state.Image.Value)));
            },
            error => ApiErrors.FromUserError(error));
    }
}
