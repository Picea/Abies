// =============================================================================
// Article Endpoints — CRUD, Favorites, Feed
// =============================================================================

using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.ReadModel;
using Picea;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/articles endpoint group.
/// </summary>
public static class ArticleEndpoints
{
    /// <summary>Registers the /api/articles endpoints.</summary>
    public static RouteGroupBuilder MapArticleEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/articles");

        group.MapGet("/", ListArticles).AllowAnonymous().WithName("ListArticles");
        group.MapGet("/feed", GetFeed).RequireAuthorization().WithName("GetFeed");
        group.MapGet("/{slug}", GetArticle).AllowAnonymous().WithName("GetArticle");
        group.MapPost("/", CreateArticle).RequireAuthorization().WithName("CreateArticle");
        group.MapPut("/{slug}", UpdateArticle).RequireAuthorization().WithName("UpdateArticle");
        group.MapDelete("/{slug}", DeleteArticle).RequireAuthorization().WithName("DeleteArticle");
        group.MapPost("/{slug}/favorite", FavoriteArticle).RequireAuthorization().WithName("FavoriteArticle");
        group.MapDelete("/{slug}/favorite", UnfavoriteArticle).RequireAuthorization().WithName("UnfavoriteArticle");

        return group;
    }

    private static async Task<IResult> ListArticles(
        HttpContext context,
        ListArticles listArticles,
        string? tag = null,
        string? author = null,
        string? favorited = null,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        var currentUserIdOption = currentUserId is { } uid
            ? Option<Guid>.Some(uid)
            : Option<Guid>.None;

        var filter = new ArticleFilter(
            Tag: tag is not null ? Option<string>.Some(tag) : Option<string>.None,
            Author: author is not null ? Option<string>.Some(author) : Option<string>.None,
            FavoritedBy: favorited is not null ? Option<string>.Some(favorited) : Option<string>.None,
            Limit: limit,
            Offset: offset,
            CurrentUserId: currentUserIdOption);

        var result = await listArticles(filter, cancellationToken).ConfigureAwait(false);

        return Results.Ok(new MultipleArticlesResponse(
            Articles: result.Articles.Select(a => a.ToArticleListDto()).ToList(),
            ArticlesCount: result.ArticlesCount));
    }

    private static async Task<IResult> GetFeed(
        HttpContext context,
        GetFeed getFeed,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var result = await getFeed(currentUserId.Value, limit, offset, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new MultipleArticlesResponse(
            Articles: result.Articles.Select(a => a.ToArticleListDto()).ToList(),
            ArticlesCount: result.ArticlesCount));
    }

    private static async Task<IResult> GetArticle(
        string slug,
        HttpContext context,
        FindArticleBySlug findArticleBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        var currentUserIdOption = currentUserId is { } uid
            ? Option<Guid>.Some(uid)
            : Option<Guid>.None;

        var articleOption = await findArticleBySlug(slug, currentUserIdOption, cancellationToken)
            .ConfigureAwait(false);

        return articleOption.Match(
            some: article => Results.Ok(new SingleArticleResponse(article.ToArticleDto())),
            none: () => ApiErrors.NotFound("Article not found."));
    }

    private static async Task<IResult> CreateArticle(
        CreateArticleRequest request,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var body = request.Article;

        var titleResult = Title.Create(body.Title);
        if (titleResult.IsErr)
            return ApiErrors.FromArticleError(titleResult.Error);

        var descriptionResult = Description.Create(body.Description);
        if (descriptionResult.IsErr)
            return ApiErrors.FromArticleError(descriptionResult.Error);

        var bodyResult = Body.Create(body.Body);
        if (bodyResult.IsErr)
            return ApiErrors.FromArticleError(bodyResult.Error);

        var tags = new HashSet<Tag>();
        if (body.TagList is { Length: > 0 })
        {
            foreach (var tagStr in body.TagList)
            {
                var tagResult = Tag.Create(tagStr);
                if (tagResult.IsErr)
                    return ApiErrors.FromArticleError(tagResult.Error);
                tags.Add(tagResult.Value);
            }
        }

        var articleId = ArticleId.New();
        var command = new ArticleCommand.CreateArticle(
            articleId, titleResult.Value, descriptionResult.Value, bodyResult.Value,
            tags, new UserId(currentUserId.Value), Timestamp.Now());

        var result = await aggregateStore.HandleArticleCommand(
            articleId.Value, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async state =>
            {
                var articleOption = await findArticleBySlug(
                    state.Slug.Value,
                    Option<Guid>.Some(currentUserId.Value),
                    cancellationToken).ConfigureAwait(false);

                return articleOption.Match(
                    some: article => Results.Created(
                        $"/api/articles/{article.Slug}",
                        new SingleArticleResponse(article.ToArticleDto())),
                    none: () => Results.Created(
                        $"/api/articles/{state.Slug.Value}", null));
            },
            err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
    }

    private static async Task<IResult> UpdateArticle(
        string slug,
        UpdateArticleRequest request,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var articleIdOption = await findArticleIdBySlug(slug, cancellationToken)
            .ConfigureAwait(false);
        if (articleIdOption.IsNone)
            return ApiErrors.NotFound("Article not found.");

        var articleId = articleIdOption.Value;
        var body = request.Article;

        var titleOption = Option<Title>.None;
        if (body.Title is not null)
        {
            var titleResult = Title.Create(body.Title);
            if (titleResult.IsErr)
                return ApiErrors.FromArticleError(titleResult.Error);
            titleOption = Option<Title>.Some(titleResult.Value);
        }

        var descriptionOption = Option<Description>.None;
        if (body.Description is not null)
        {
            var descResult = Description.Create(body.Description);
            if (descResult.IsErr)
                return ApiErrors.FromArticleError(descResult.Error);
            descriptionOption = Option<Description>.Some(descResult.Value);
        }

        var bodyOption = Option<Body>.None;
        if (body.Body is not null)
        {
            var bodyResult = Body.Create(body.Body);
            if (bodyResult.IsErr)
                return ApiErrors.FromArticleError(bodyResult.Error);
            bodyOption = Option<Body>.Some(bodyResult.Value);
        }

        var command = new ArticleCommand.UpdateArticle(
            titleOption, descriptionOption, bodyOption,
            new UserId(currentUserId.Value), Timestamp.Now());

        var result = await aggregateStore.HandleArticleCommand(
            articleId, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async state =>
            {
                var articleOption = await findArticleBySlug(
                    state.Slug.Value,
                    Option<Guid>.Some(currentUserId.Value),
                    cancellationToken).ConfigureAwait(false);

                return articleOption.Match(
                    some: article => Results.Ok(
                        new SingleArticleResponse(article.ToArticleDto())),
                    none: () => Results.Ok(null));
            },
            err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
    }

    private static async Task<IResult> DeleteArticle(
        string slug,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var articleIdOption = await findArticleIdBySlug(slug, cancellationToken)
            .ConfigureAwait(false);
        if (articleIdOption.IsNone)
            return ApiErrors.NotFound("Article not found.");

        var command = new ArticleCommand.DeleteArticle(new UserId(currentUserId.Value));
        var result = await aggregateStore.HandleArticleCommand(
            articleIdOption.Value, command, cancellationToken).ConfigureAwait(false);

        return result.Match(
            ok: _ => Results.Ok(),
            err: error => ApiErrors.FromArticleError(error));
    }

    private static async Task<IResult> FavoriteArticle(
        string slug,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var articleIdOption = await findArticleIdBySlug(slug, cancellationToken)
            .ConfigureAwait(false);
        if (articleIdOption.IsNone)
            return ApiErrors.NotFound("Article not found.");

        var command = new ArticleCommand.FavoriteArticle(new UserId(currentUserId.Value));
        var result = await aggregateStore.HandleArticleCommand(
            articleIdOption.Value, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async _ =>
            {
                var articleOption = await findArticleBySlug(
                    slug, Option<Guid>.Some(currentUserId.Value), cancellationToken)
                    .ConfigureAwait(false);

                return articleOption.Match(
                    some: article => Results.Ok(
                        new SingleArticleResponse(article.ToArticleDto())),
                    none: () => Results.Ok(null));
            },
            err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
    }

    private static async Task<IResult> UnfavoriteArticle(
        string slug,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var articleIdOption = await findArticleIdBySlug(slug, cancellationToken)
            .ConfigureAwait(false);
        if (articleIdOption.IsNone)
            return ApiErrors.NotFound("Article not found.");

        var command = new ArticleCommand.UnfavoriteArticle(new UserId(currentUserId.Value));
        var result = await aggregateStore.HandleArticleCommand(
            articleIdOption.Value, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async _ =>
            {
                var articleOption = await findArticleBySlug(
                    slug, Option<Guid>.Some(currentUserId.Value), cancellationToken)
                    .ConfigureAwait(false);

                return articleOption.Match(
                    some: article => Results.Ok(
                        new SingleArticleResponse(article.ToArticleDto())),
                    none: () => Results.Ok(null));
            },
            err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
    }
}
