// =============================================================================
// Article Endpoints — CRUD, Favorites, Feed
// =============================================================================

using Picea;
using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/articles endpoint group.
/// </summary>
public static class ArticleEndpoints
{
    private const int MinLimit = 1;
    private const int MaxLimit = 100;

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
        var paginationValidationError = ValidatePagination(limit, offset);
        if (paginationValidationError is not null)
            return paginationValidationError;

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
        if (JwtTokenService.GetUserId(context.User) is not { } currentUserId)
            return ApiErrors.Unauthorized();

        var paginationValidationError = ValidatePagination(limit, offset);
        if (paginationValidationError is not null)
            return paginationValidationError;

        var result = await getFeed(currentUserId, limit, offset, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new MultipleArticlesResponse(
            Articles: result.Articles.Select(a => a.ToArticleListDto()).ToList(),
            ArticlesCount: result.ArticlesCount));
    }

    private static async Task<IResult> GetArticle(
        string slug,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        FindUserById findUserById,
        GetProfile getProfile,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        var currentUserIdOption = currentUserId is { } uid
            ? Option<Guid>.Some(uid)
            : Option<Guid>.None;

        var articleOption = await findArticleBySlug(slug, currentUserIdOption, cancellationToken)
            .ConfigureAwait(false);

        if (articleOption.IsSome)
            return Results.Ok(new SingleArticleResponse(articleOption.Value.ToArticleDto()));

        // ─── Shadow Data Layer (Phase 2, Task 2) ────────────────────────────────
        // Read model doesn't have the article yet, check event store for shadow data
        var shadowArticle = await GetShadowArticle(
            slug, aggregateStore, findUserById, getProfile, currentUserIdOption, cancellationToken)
            .ConfigureAwait(false);

        if (shadowArticle.IsSome)
            return Results.Ok(new SingleArticleResponse(shadowArticle.Value.ToArticleDto()));
        // ────────────────────────────────────────────────────────────────────────

        return ApiErrors.NotFound("Article not found.");
    }

    /// <summary>
    /// Builds shadow data from write-side state when read model has projection lag.
    /// Used when an article is created/updated but hasn't been projected yet.
    /// </summary>
    private static async Task<Option<ArticleQueryResult>> GetShadowArticle(
        string slug,
        AggregateStore aggregateStore,
        FindUserById findUserById,
        GetProfile getProfile,
        Option<Guid> currentUserId,
        CancellationToken cancellationToken)
    {
        if (!aggregateStore.TryGetRecentArticleBySlug(slug, out var shadowState))
            return Option<ArticleQueryResult>.None;

        if (!shadowState.Published || shadowState.Deleted)
            return Option<ArticleQueryResult>.None;

        var authorOption = await findUserById(shadowState.AuthorId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (authorOption.IsNone)
            return Option<ArticleQueryResult>.None;

        var authorReadModel = authorOption.Value;
        var profileOption = await getProfile(
            authorReadModel.Username,
            currentUserId,
            cancellationToken).ConfigureAwait(false);

        var author = profileOption.Match(
            some: profile => profile,
            none: () => new ProfileReadModel(
                authorReadModel.Username,
                authorReadModel.Bio,
                authorReadModel.Image,
                false));

        var favorited = currentUserId.IsSome
            && shadowState.FavoritedBy.Contains(new UserId(currentUserId.Value));

        return Option<ArticleQueryResult>.Some(new ArticleQueryResult(
            Slug: shadowState.Slug.Value,
            Title: shadowState.Title.Value,
            Description: shadowState.Description.Value,
            Body: shadowState.Body.Value,
            TagList: shadowState.Tags.Select(t => t.Value).OrderBy(t => t).ToArray(),
            CreatedAt: shadowState.CreatedAt.Value,
            UpdatedAt: shadowState.UpdatedAt.Value,
            Favorited: favorited,
            FavoritesCount: shadowState.FavoritedBy.Count,
            Author: author));
    }

        private static async Task<Option<ArticleQueryResult>> FindArticleWithShadowFallback(
            string slug,
            Guid currentUserId,
            FindArticleBySlug findArticleBySlug,
            AggregateStore aggregateStore,
            FindUserById findUserById,
            GetProfile getProfile,
            CancellationToken cancellationToken)
        {
            var currentUserOption = Option<Guid>.Some(currentUserId);

            var articleOption = await findArticleBySlug(slug, currentUserOption, cancellationToken)
                .ConfigureAwait(false);
            if (articleOption.IsSome)
                return articleOption;

            return await GetShadowArticle(
                slug,
                aggregateStore,
                findUserById,
                getProfile,
                currentUserOption,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<IResult> CreateArticle(
            CreateArticleRequest request,
            HttpContext context,
            RequestIdempotencyStore idempotencyStore,
            AggregateStore aggregateStore,
            FindArticleBySlug findArticleBySlug,
            FindUserById findUserById,
            GetProfile getProfile,
            CancellationToken cancellationToken)
        {
            return await idempotencyStore.ExecuteAsync(
                context,
                async ct =>
                {
                    if (JwtTokenService.GetUserId(context.User) is not { } currentUserId)
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
                        foreach (var tagResult in body.TagList.Select(Tag.Create))
                        {
                            if (tagResult.IsErr)
                                return ApiErrors.FromArticleError(tagResult.Error);
                            tags.Add(tagResult.Value);
                        }
                    }

                    // ─── Pre-commit Uniqueness Checks (Phase 2, Task 1) ──────────────────────
                    // Generate slug from title to check for duplicates
                    var slug = Slug.FromTitle(titleResult.Value);

                    // Check if article with this slug already exists (prevents duplicate articles)
                    var existingArticle = await findArticleBySlug(
                        slug.Value,
                        Option<Guid>.None,
                        ct).ConfigureAwait(false);

                    if (existingArticle.IsSome)
                        return ApiErrors.FromArticleError(new ArticleError.DuplicateSlug());
                    // ────────────────────────────────────────────────────────────────────────

                    var articleId = ArticleId.New();
                    var command = new ArticleCommand.CreateArticle(
                        articleId, titleResult.Value, descriptionResult.Value, bodyResult.Value,
                        tags, new UserId(currentUserId), Timestamp.Now());

                    var result = await aggregateStore.HandleUniqueArticleCreation(
                        articleId.Value,
                        command,
                        slug.Value,
                        ct).ConfigureAwait(false);

                    return await result.Match(
                        ok: async state =>
                        {
                            var articleOption = await FindArticleWithShadowFallback(
                                state.Slug.Value,
                                currentUserId,
                                findArticleBySlug,
                                aggregateStore,
                                findUserById,
                                getProfile,
                                ct).ConfigureAwait(false);

                            return articleOption.Match(
                                some: article => Results.Created(
                                    $"/api/articles/{article.Slug}",
                                    new SingleArticleResponse(article.ToArticleDto())),
                                none: () => ApiErrors.ServiceUnavailable(
                                    "Article was created but is temporarily unavailable. Please retry."));
                        },
                        err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
                },
                payloadFingerprintInput: System.Text.Json.JsonSerializer.Serialize(request),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static async Task<IResult> UpdateArticle(
            string slug,
            UpdateArticleRequest request,
            HttpContext context,
            RequestIdempotencyStore idempotencyStore,
            AggregateStore aggregateStore,
            FindArticleBySlug findArticleBySlug,
            FindArticleIdBySlug findArticleIdBySlug,
            FindUserById findUserById,
            GetProfile getProfile,
            CancellationToken cancellationToken)
        {
            return await idempotencyStore.ExecuteAsync(
                context,
                async ct =>
                {
                    if (JwtTokenService.GetUserId(context.User) is not { } currentUserId)
                        return ApiErrors.Unauthorized();

                    var articleIdOption = await findArticleIdBySlug(slug, ct)
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
                        new UserId(currentUserId), Timestamp.Now());

                    var result = await aggregateStore.HandleArticleCommand(
                        articleId, command, ct).ConfigureAwait(false);

                    return await result.Match(
                        ok: async state =>
                        {
                            var articleOption = await FindArticleWithShadowFallback(
                                state.Slug.Value,
                                currentUserId,
                                findArticleBySlug,
                                aggregateStore,
                                findUserById,
                                getProfile,
                                ct).ConfigureAwait(false);

                            return articleOption.Match(
                                some: article => Results.Ok(
                                    new SingleArticleResponse(article.ToArticleDto())),
                                none: () => ApiErrors.ServiceUnavailable(
                                    "Article was updated but is temporarily unavailable. Please retry."));
                        },
                        err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
                },
                payloadFingerprintInput: System.Text.Json.JsonSerializer.Serialize(request),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            }

    private static IResult? ValidatePagination(int limit, int offset)
    {
        if (limit < MinLimit || limit > MaxLimit)
            return ApiErrors.Validation($"limit must be between {MinLimit} and {MaxLimit}.");

        if (offset < 0)
            return ApiErrors.Validation("offset must be greater than or equal to 0.");

        return null;
    }

    private static async Task<IResult> DeleteArticle(
        string slug,
        HttpContext context,
        RequestIdempotencyStore idempotencyStore,
        AggregateStore aggregateStore,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        return await idempotencyStore.ExecuteAsync(
            context,
            async ct =>
            {
                if (JwtTokenService.GetUserId(context.User) is not { } currentUserId)
                    return ApiErrors.Unauthorized();

                var articleIdOption = await findArticleIdBySlug(slug, ct)
                    .ConfigureAwait(false);
                if (articleIdOption.IsNone)
                    return ApiErrors.NotFound("Article not found.");

                var command = new ArticleCommand.DeleteArticle(new UserId(currentUserId));
                var result = await aggregateStore.HandleArticleCommand(
                    articleIdOption.Value, command, ct).ConfigureAwait(false);

                return result.Match(
                    ok: _ => Results.Ok(),
                    err: error => ApiErrors.FromArticleError(error));
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> FavoriteArticle(
        string slug,
        HttpContext context,
        RequestIdempotencyStore idempotencyStore,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        return await idempotencyStore.ExecuteAsync(
            context,
            async ct =>
            {
                if (JwtTokenService.GetUserId(context.User) is not { } currentUserId)
                    return ApiErrors.Unauthorized();

                var articleIdOption = await findArticleIdBySlug(slug, ct)
                    .ConfigureAwait(false);
                if (articleIdOption.IsNone)
                    return ApiErrors.NotFound("Article not found.");

                var command = new ArticleCommand.FavoriteArticle(new UserId(currentUserId));
                var result = await aggregateStore.HandleArticleCommand(
                    articleIdOption.Value, command, ct).ConfigureAwait(false);

                return await result.Match(
                    ok: async _ =>
                    {
                        var articleOption = await findArticleBySlug(
                                slug,
                                Option<Guid>.Some(currentUserId),
                                ct)
                            .ConfigureAwait(false);

                        return articleOption.Match(
                            some: article => Results.Ok(
                                new SingleArticleResponse(article.ToArticleDto())),
                            none: () => Results.Ok(null));
                    },
                    err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> UnfavoriteArticle(
        string slug,
        HttpContext context,
        RequestIdempotencyStore idempotencyStore,
        AggregateStore aggregateStore,
        FindArticleBySlug findArticleBySlug,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        return await idempotencyStore.ExecuteAsync(
            context,
            async ct =>
            {
                if (JwtTokenService.GetUserId(context.User) is not { } currentUserId)
                    return ApiErrors.Unauthorized();

                var articleIdOption = await findArticleIdBySlug(slug, ct)
                    .ConfigureAwait(false);
                if (articleIdOption.IsNone)
                    return ApiErrors.NotFound("Article not found.");

                var command = new ArticleCommand.UnfavoriteArticle(new UserId(currentUserId));
                var result = await aggregateStore.HandleArticleCommand(
                    articleIdOption.Value, command, ct).ConfigureAwait(false);

                return await result.Match(
                    ok: async _ =>
                    {
                        var articleOption = await findArticleBySlug(
                                slug,
                                Option<Guid>.Some(currentUserId),
                                ct)
                            .ConfigureAwait(false);

                        return articleOption.Match(
                            some: article => Results.Ok(
                                new SingleArticleResponse(article.ToArticleDto())),
                            none: () => Results.Ok(null));
                    },
                    err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
            },
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
