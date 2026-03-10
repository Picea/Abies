// =============================================================================
// Tag Endpoints — Public Tag List
// =============================================================================
// GET /api/tags — Get all tags
// =============================================================================

using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/tags endpoint group.
/// </summary>
public static class TagEndpoints
{
    /// <summary>Registers the /api/tags endpoints.</summary>
    public static RouteGroupBuilder MapTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tags");

        group.MapGet("/", GetTags)
            .AllowAnonymous()
            .WithName("GetTags");

        return group;
    }

    /// <summary>
    /// GET /api/tags — List all tags used across articles.
    /// </summary>
    private static async Task<IResult> GetTags(
        GetTags getTags,
        CancellationToken cancellationToken)
    {
        var tags = await getTags(cancellationToken).ConfigureAwait(false);
        return Results.Ok(new TagsResponse(tags));
    }
}
