// =============================================================================
// Profile Endpoints — User Profiles & Follow/Unfollow
// =============================================================================
// GET    /api/profiles/:username          — Get a user profile
// POST   /api/profiles/:username/follow   — Follow a user
// DELETE /api/profiles/:username/follow   — Unfollow a user
//
// GET is publicly accessible (with optional auth for following status).
// POST/DELETE require authentication.
// =============================================================================

using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/profiles endpoints (profile viewing and follow/unfollow).
/// </summary>
public static class ProfileEndpoints
{
    /// <summary>Registers the /api/profiles endpoint group.</summary>
    public static RouteGroupBuilder MapProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/profiles");

        group.MapGet("/{username}", GetProfile)
            .AllowAnonymous()
            .WithName("GetProfile");

        group.MapPost("/{username}/follow", FollowUser)
            .RequireAuthorization()
            .WithName("FollowUser");

        group.MapDelete("/{username}/follow", UnfollowUser)
            .RequireAuthorization()
            .WithName("UnfollowUser");

        return group;
    }

    /// <summary>
    /// GET /api/profiles/:username — Get a user's public profile.
    /// </summary>
    private static async Task<IResult> GetProfile(
        string username,
        HttpContext context,
        GetProfile getProfile,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        var currentUserIdOption = currentUserId is { } uid
            ? Option<Guid>.Some(uid)
            : Option<Guid>.None;

        var profileOption = await getProfile(username, currentUserIdOption, cancellationToken)
            .ConfigureAwait(false);

        return profileOption.Match(
            profile => Results.Ok(new ProfileResponse(profile.ToProfileDto())),
            () => ApiErrors.NotFound($"Profile '{username}' not found."));
    }

    /// <summary>
    /// POST /api/profiles/:username/follow — Follow a user.
    /// </summary>
    private static async Task<IResult> FollowUser(
        string username,
        HttpContext context,
        AggregateStore aggregateStore,
        FindUserByUsername findUserByUsername,
        GetProfile getProfile,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var targetUserOption = await findUserByUsername(username, cancellationToken)
            .ConfigureAwait(false);

        if (targetUserOption.IsNone)
            return ApiErrors.NotFound($"Profile '{username}' not found.");

        var targetUser = targetUserOption.Value;

        var command = new UserCommand.Follow(new UserId(targetUser.Id));
        var result = await aggregateStore.HandleUserCommand(
            currentUserId.Value, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async _ =>
            {
                var profileOption = await getProfile(
                    username, Option<Guid>.Some(currentUserId.Value), cancellationToken)
                    .ConfigureAwait(false);

                return profileOption.Match(
                    profile => Results.Ok(new ProfileResponse(profile.ToProfileDto())),
                    () => ApiErrors.NotFound($"Profile '{username}' not found."));
            },
            err: error => Task.FromResult(ApiErrors.FromUserError(error)));
    }

    /// <summary>
    /// DELETE /api/profiles/:username/follow — Unfollow a user.
    /// </summary>
    private static async Task<IResult> UnfollowUser(
        string username,
        HttpContext context,
        AggregateStore aggregateStore,
        FindUserByUsername findUserByUsername,
        GetProfile getProfile,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var targetUserOption = await findUserByUsername(username, cancellationToken)
            .ConfigureAwait(false);

        if (targetUserOption.IsNone)
            return ApiErrors.NotFound($"Profile '{username}' not found.");

        var targetUser = targetUserOption.Value;

        var command = new UserCommand.Unfollow(new UserId(targetUser.Id));
        var result = await aggregateStore.HandleUserCommand(
            currentUserId.Value, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async _ =>
            {
                var profileOption = await getProfile(
                    username, Option<Guid>.Some(currentUserId.Value), cancellationToken)
                    .ConfigureAwait(false);

                return profileOption.Match(
                    profile => Results.Ok(new ProfileResponse(profile.ToProfileDto())),
                    () => ApiErrors.NotFound($"Profile '{username}' not found."));
            },
            err: error => Task.FromResult(ApiErrors.FromUserError(error)));
    }
}
