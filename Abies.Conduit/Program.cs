using Abies;
using Abies.Conduit;
using Abies.Conduit.Capabilities;
using Abies.Conduit.Main;
using Abies.Conduit.Services;
using static Abies.Option.Extensions;

// =============================================================================
// Anti-Corruption Layer: Adapt exception-throwing services to Result types
// =============================================================================
// The API services still throw exceptions (ApiException, UnauthorizedException).
// These adapters catch at the boundary and convert to ConduitError values.
// This is the ACL from DDD — translation logic lives at the boundary.
// =============================================================================

static async Task<Result<T, ConduitError>> TryCatch<T>(Func<Task<T>> operation)
{
    try
    {
        return new Ok<T, ConduitError>(await operation());
    }
    catch (ApiException ex)
    {
        return new Error<T, ConduitError>(new ValidationError(ex.Errors));
    }
    catch (UnauthorizedException)
    {
        return new Error<T, ConduitError>(new Unauthorized());
    }
    catch (Exception ex)
    {
        return new Error<T, ConduitError>(new UnexpectedError(ex.Message));
    }
}

static async Task<Result<Unit, ConduitError>> TryCatchUnit(Func<Task> operation)
{
    try
    {
        await operation();
        return new Ok<Unit, ConduitError>(default);
    }
    catch (ApiException ex)
    {
        return new Error<Unit, ConduitError>(new ValidationError(ex.Errors));
    }
    catch (UnauthorizedException)
    {
        return new Error<Unit, ConduitError>(new Unauthorized());
    }
    catch (Exception ex)
    {
        return new Error<Unit, ConduitError>(new UnexpectedError(ex.Message));
    }
}

// Compose the command handler by wiring real service implementations as capabilities.
// Each adapter wraps the exception-throwing service in TryCatch, converting to Result.
var handleCommand = Handlers.ComposeAll(
    login: async (email, password) =>
        await TryCatch(() => AuthService.LoginAsync(email, password)),
    register: async (username, email, password) =>
        await TryCatch(() => AuthService.RegisterAsync(username, email, password)),
    updateUser: async (username, email, bio, image, password) =>
        await TryCatch(() => AuthService.UpdateUserAsync(username, email, bio, image, password)),
    loadCurrentUser: async () =>
    {
        try
        {
            var user = await AuthService.LoadUserFromLocalStorageAsync();
            return user is not null ? Some(user) : None<User>();
        }
        catch
        {
            return None<User>();
        }
    },
    logout: async () =>
        await TryCatchUnit(() => AuthService.Logout()),
    loadArticles: async (tag, author, favoritedBy, limit, offset) =>
        await TryCatch(() => ArticleService.GetArticlesAsync(tag, author, favoritedBy, limit, offset)),
    loadFeed: async (limit, offset) =>
        await TryCatch(() => ArticleService.GetFeedArticlesAsync(limit, offset)),
    getArticle: async slug =>
        await TryCatch(() => ArticleService.GetArticleAsync(slug)),
    createArticle: async (title, description, body, tagList) =>
        await TryCatch(() => ArticleService.CreateArticleAsync(title, description, body, tagList)),
    updateArticle: async (slug, title, description, body) =>
        await TryCatch(() => ArticleService.UpdateArticleAsync(slug, title, description, body)),
    deleteArticle: async slug =>
        await TryCatchUnit(() => ArticleService.DeleteArticleAsync(slug)),
    favoriteArticle: async slug =>
        await TryCatch(() => ArticleService.FavoriteArticleAsync(slug)),
    unfavoriteArticle: async slug =>
        await TryCatch(() => ArticleService.UnfavoriteArticleAsync(slug)),
    loadComments: async slug =>
        await TryCatch(() => ArticleService.GetCommentsAsync(slug)),
    addComment: async (slug, body) =>
        await TryCatch(() => ArticleService.AddCommentAsync(slug, body)),
    deleteComment: async (slug, id) =>
        await TryCatchUnit(() => ArticleService.DeleteCommentAsync(slug, id)),
    loadTags: async () =>
        await TryCatch(() => TagService.GetTagsAsync()),
    loadProfile: async username =>
        await TryCatch(() => ProfileService.GetProfileAsync(username)),
    followUser: async username =>
        await TryCatch(() => ProfileService.FollowUserAsync(username)),
    unfollowUser: async username =>
        await TryCatch(() => ProfileService.UnfollowUserAsync(username))
);

await Runtime.Run<Abies.Conduit.Main.Program, Arguments, Model>(new Arguments(), handleCommand);
