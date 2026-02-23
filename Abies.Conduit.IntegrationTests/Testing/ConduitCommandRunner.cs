using Abies.Commanding;
using Abies.Conduit.Capabilities;
using Abies.Conduit.Main;
using Abies.Conduit.Services;
using static Abies.Option.Extensions;

namespace Abies.Conduit.IntegrationTests.Testing;

/// <summary>
/// Deterministic command runner:
/// runs Conduit <see cref="Command"/> through the composed handler pipeline
/// (backed by <see cref="Handlers.ComposeAll"/> wired to the real service methods,
/// which themselves route through a fake <see cref="HttpClient"/>).
///
/// Handlers now return <see cref="Option{T}"/> of <see cref="Message"/>
/// instead of dispatching via callback.
/// </summary>
internal static class ConduitCommandRunner
{
    // ── ACL: Adapt exception-throwing services to Result types ──

    private static async Task<Result<T, ConduitError>> TryCatch<T>(Func<Task<T>> operation)
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

    private static async Task<Result<Unit, ConduitError>> TryCatchUnit(Func<Task> operation)
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

    private static readonly Handler _handler = Handlers.ComposeAll(
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

    public static async Task<IReadOnlyList<Message>> RunAsync(Command command)
    {
        var result = await _handler(command);
        return result switch
        {
            Some<Message>(var msg) => [msg],
            _ => []
        };
    }

    public static async Task<IReadOnlyList<Message>> RunAsync(IEnumerable<Command> commands)
    {
        List<Message> dispatched = [];
        foreach (var cmd in commands)
        {
            dispatched.AddRange(await RunAsync(cmd));
        }

        return dispatched;
    }

    public static IEnumerable<Command> Flatten(Command command)
    {
        if (command is Command.None)
        {
            yield break;
        }

        if (command is Command.Batch batch)
        {
            foreach (var item in batch.Commands)
            {
                foreach (var flat in Flatten(item))
                {
                    yield return flat;
                }
            }

            yield break;
        }

        yield return command;
    }
}
