// =============================================================================
// Conduit Command Handlers
// =============================================================================
// Individual handler functions per command type. Each handler closes over
// the capability functions it needs, making dependencies explicit and testable.
//
// Handlers use Railway-Oriented Programming: capabilities return Result types,
// and handlers pattern match on the result to produce the appropriate message.
// No try/catch — errors are values, not exceptions.
//
// Testing is trivial: inject fake lambdas for capabilities.
// =============================================================================

using System.Diagnostics;
using Abies.Commanding;
using Abies.Conduit.Capabilities;
using Abies.Conduit.Page.Home;
using static Abies.Option.Extensions;
using MainMessage = Abies.Conduit.Main.Message;
using User = Abies.Conduit.Main.User;

namespace Abies.Conduit;

/// <summary>
/// Individual command handler factories. Each method takes capability delegates
/// and returns a <see cref="Handler"/> that handles one command type.
/// </summary>
public static class Handlers
{
    // ── Auth handlers ──

    public static Handler LoadCurrentUserHandler(LoadCurrentUser loadCurrentUser) =>
        Pipeline.For<MainMessage.Command.LoadCurrentUser>(async _ =>
        {
            var maybeUser = await loadCurrentUser();
            return Some<Message>(maybeUser switch
            {
                Some<User>(var user) => new MainMessage.Event.UserLoggedIn(user),
                _ => new MainMessage.Event.InitializationComplete()
            });
        });

    public static Handler LoginHandler(Login login) =>
        Pipeline.For<LoginCommand>(async cmd =>
        {
            var result = await login(cmd.Email, cmd.Password);
            return Some<Message>(result switch
            {
                Ok<User, ConduitError>(var user) => new Page.Login.Message.LoginSuccess(user),
                Error<User, ConduitError>(var err) => err switch
                {
                    ValidationError => new Page.Login.Message.LoginError(["Invalid email or password"]),
                    Unauthorized => new MainMessage.Event.UserLoggedOut(),
                    UnexpectedError => new Page.Login.Message.LoginError(["An unexpected error occurred"]),
                    _ => throw new UnreachableException()
                },
                _ => throw new UnreachableException()
            });
        });

    public static Handler RegisterHandler(Register register) =>
        Pipeline.For<RegisterCommand>(async cmd =>
        {
            var result = await register(cmd.Username, cmd.Email, cmd.Password);
            return Some<Message>(result switch
            {
                Ok<User, ConduitError>(var user) => new Page.Register.Message.RegisterSuccess(user),
                Error<User, ConduitError>(var err) => err switch
                {
                    ValidationError v => new Page.Register.Message.RegisterError(v.Errors),
                    Unauthorized => new MainMessage.Event.UserLoggedOut(),
                    _ => new Page.Register.Message.RegisterError(
                        new Dictionary<string, string[]> { { "error", ["An unexpected error occurred"] } })
                },
                _ => throw new UnreachableException()
            });
        });

    public static Handler UpdateUserHandler(UpdateUser updateUser) =>
        Pipeline.For<UpdateUserCommand>(async cmd =>
        {
            var result = await updateUser(cmd.Username, cmd.Email, cmd.Bio, cmd.Image, cmd.Password);
            return Some<Message>(result switch
            {
                Ok<User, ConduitError>(var user) => new Page.Settings.Message.SettingsSuccess(user),
                Error<User, ConduitError>(var err) => err switch
                {
                    ValidationError v => new Page.Settings.Message.SettingsError(v.Errors),
                    Unauthorized => new MainMessage.Event.UserLoggedOut(),
                    _ => new Page.Settings.Message.SettingsError(
                        new Dictionary<string, string[]> { { "error", ["An unexpected error occurred"] } })
                },
                _ => throw new UnreachableException()
            });
        });

    public static Handler LogoutHandler(Logout logout) =>
        Pipeline.For<LogoutCommand>(async _ =>
        {
            var result = await logout();
            return result switch
            {
                Ok<Unit, ConduitError> => Some<Message>(new MainMessage.Event.UserLoggedOut()),
                _ => None<Message>()
            };
        });

    // ── Article handlers ──

    public static Handler LoadArticlesHandler(LoadArticles loadArticles) =>
        Pipeline.For<LoadArticlesCommand>(async cmd =>
        {
            var result = await loadArticles(cmd.Tag, cmd.Author, cmd.FavoritedBy, cmd.Limit, cmd.Offset);
            if (result is Ok<(List<Article> Articles, int Count), ConduitError>(var data))
            {
                if (!string.IsNullOrEmpty(cmd.Author))
                    return Some<Message>(new Page.Profile.Message.ArticlesLoaded(data.Articles, data.Count));
                if (!string.IsNullOrEmpty(cmd.FavoritedBy))
                    return Some<Message>(new Page.Profile.Message.FavoritedArticlesLoaded(data.Articles, data.Count));
                return Some<Message>(new Page.Home.Message.ArticlesLoaded(data.Articles, data.Count));
            }
            if (result is Error<(List<Article> Articles, int Count), ConduitError>(var err))
            {
                if (err is Unauthorized)
                    return Some<Message>(new MainMessage.Event.UserLoggedOut());
                if (!string.IsNullOrEmpty(cmd.Author))
                    return Some<Message>(new Page.Profile.Message.ArticlesLoaded([], 0));
                if (!string.IsNullOrEmpty(cmd.FavoritedBy))
                    return Some<Message>(new Page.Profile.Message.FavoritedArticlesLoaded([], 0));
                return Some<Message>(new Page.Home.Message.ArticlesLoaded([], 0));
            }
            throw new UnreachableException();
        });

    public static Handler LoadFeedHandler(LoadFeed loadFeed) =>
        Pipeline.For<LoadFeedCommand>(async cmd =>
        {
            var result = await loadFeed(cmd.Limit, cmd.Offset);
            return Some<Message>(result switch
            {
                Ok<(List<Article> Articles, int Count), ConduitError>(var data) =>
                    new Page.Home.Message.ArticlesLoaded(data.Articles, data.Count),
                _ => new Page.Home.Message.ArticlesLoaded([], 0)
            });
        });

    public static Handler LoadTagsHandler(LoadTags loadTags) =>
        Pipeline.For<LoadTagsCommand>(async _ =>
        {
            var result = await loadTags();
            return Some<Message>(result switch
            {
                Ok<List<string>, ConduitError>(var tags) => new Page.Home.Message.TagsLoaded(tags),
                Error<List<string>, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Home.Message.TagsLoaded([])
            });
        });

    public static Handler LoadArticleHandler(GetArticle getArticle) =>
        Pipeline.For<LoadArticleCommand>(async cmd =>
        {
            var result = await getArticle(cmd.Slug);
            return Some<Message>(result switch
            {
                Ok<Article, ConduitError>(var article) => new Page.Article.Message.ArticleLoaded(article),
                Error<Article, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Article.Message.ArticleLoaded(null)
            });
        });

    public static Handler LoadArticleForEditorHandler(GetArticle getArticle) =>
        Pipeline.For<LoadArticleForEditorCommand>(async cmd =>
        {
            var result = await getArticle(cmd.Slug);
            return Some<Message>(result switch
            {
                Ok<Article, ConduitError>(var article) => new Page.Editor.Message.ArticleLoaded(article),
                Error<Article, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Editor.Message.ArticleLoaded(
                    new Article(
                        Slug: "", Title: "", Description: "", Body: "",
                        TagList: [], CreatedAt: "", UpdatedAt: "",
                        Favorited: false, FavoritesCount: 0,
                        Author: new Profile("", "", "", false)))
            });
        });

    public static Handler LoadCommentsHandler(LoadComments loadComments) =>
        Pipeline.For<LoadCommentsCommand>(async cmd =>
        {
            var result = await loadComments(cmd.Slug);
            return Some<Message>(result switch
            {
                Ok<List<Page.Article.Comment>, ConduitError>(var comments) =>
                    new Page.Article.Message.CommentsLoaded(comments),
                Error<List<Page.Article.Comment>, ConduitError> { Value: Unauthorized } =>
                    new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Article.Message.CommentsLoaded([])
            });
        });

    public static Handler SubmitCommentHandler(AddComment addComment) =>
        Pipeline.For<SubmitCommentCommand>(async cmd =>
        {
            var result = await addComment(cmd.Slug, cmd.Body);
            return Some<Message>(result switch
            {
                Ok<Page.Article.Comment, ConduitError>(var comment) =>
                    new Page.Article.Message.CommentSubmitted(comment),
                Error<Page.Article.Comment, ConduitError> { Value: Unauthorized } =>
                    new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Article.Message.SubmitComment()
            });
        });

    public static Handler DeleteCommentHandler(DeleteComment deleteComment) =>
        Pipeline.For<DeleteCommentCommand>(async cmd =>
        {
            var result = await deleteComment(cmd.Slug, cmd.CommentId);
            return Some<Message>(result switch
            {
                Ok<Unit, ConduitError> => new Page.Article.Message.CommentDeleted(cmd.CommentId),
                Error<Unit, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Article.Message.CommentDeleted("")
            });
        });

    public static Handler CreateArticleHandler(CreateArticle createArticle) =>
        Pipeline.For<CreateArticleCommand>(async cmd =>
        {
            var result = await createArticle(cmd.Title, cmd.Description, cmd.Body, cmd.TagList);
            return Some<Message>(result switch
            {
                Ok<Article, ConduitError>(var article) => new Page.Editor.Message.ArticleSubmitSuccess(article.Slug),
                Error<Article, ConduitError>(var err) => err switch
                {
                    ValidationError v => new Page.Editor.Message.ArticleSubmitError(v.Errors),
                    Unauthorized => new MainMessage.Event.UserLoggedOut(),
                    _ => new Page.Editor.Message.ArticleSubmitError(
                        new Dictionary<string, string[]> { { "error", ["An unexpected error occurred"] } })
                },
                _ => throw new UnreachableException()
            });
        });

    public static Handler UpdateArticleHandler(UpdateArticle updateArticle) =>
        Pipeline.For<UpdateArticleCommand>(async cmd =>
        {
            var result = await updateArticle(cmd.Slug, cmd.Title, cmd.Description, cmd.Body);
            return Some<Message>(result switch
            {
                Ok<Article, ConduitError>(var article) => new Page.Editor.Message.ArticleSubmitSuccess(article.Slug),
                Error<Article, ConduitError>(var err) => err switch
                {
                    ValidationError v => new Page.Editor.Message.ArticleSubmitError(v.Errors),
                    Unauthorized => new MainMessage.Event.UserLoggedOut(),
                    _ => new Page.Editor.Message.ArticleSubmitError(
                        new Dictionary<string, string[]> { { "error", ["An unexpected error occurred"] } })
                },
                _ => throw new UnreachableException()
            });
        });

    public static Handler ToggleFavoriteHandler(FavoriteArticle favoriteArticle, UnfavoriteArticle unfavoriteArticle) =>
        Pipeline.For<ToggleFavoriteCommand>(async cmd =>
        {
            var result = cmd.CurrentState
                ? await unfavoriteArticle(cmd.Slug)
                : await favoriteArticle(cmd.Slug);
            return Some<Message>(result switch
            {
                Ok<Article, ConduitError>(var article) => new Page.Article.Message.ArticleLoaded(article),
                Error<Article, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Article.Message.ToggleFavorite()
            });
        });

    public static Handler DeleteArticleHandler(DeleteArticle deleteArticle) =>
        Pipeline.For<DeleteArticleCommand>(async cmd =>
        {
            var result = await deleteArticle(cmd.Slug);
            return Some<Message>(result switch
            {
                Ok<Unit, ConduitError> => new Page.Article.Message.ArticleDeleted(),
                Error<Unit, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Article.Message.ArticleDeleted()
            });
        });

    // ── Profile handlers ──

    public static Handler LoadProfileHandler(LoadProfile loadProfile) =>
        Pipeline.For<LoadProfileCommand>(async cmd =>
        {
            var result = await loadProfile(cmd.Username);
            return Some<Message>(result switch
            {
                Ok<Profile, ConduitError>(var profile) => new Page.Profile.Message.ProfileLoaded(profile),
                Error<Profile, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Profile.Message.ProfileLoaded(new Profile(cmd.Username, "", "", false))
            });
        });

    public static Handler ToggleFollowHandler(FollowUser followUser, UnfollowUser unfollowUser) =>
        Pipeline.For<ToggleFollowCommand>(async cmd =>
        {
            var result = cmd.CurrentState
                ? await unfollowUser(cmd.Username)
                : await followUser(cmd.Username);
            return Some<Message>(result switch
            {
                Ok<Profile, ConduitError>(var profile) => new Page.Profile.Message.ProfileLoaded(profile),
                Error<Profile, ConduitError> { Value: Unauthorized } => new MainMessage.Event.UserLoggedOut(),
                _ => new Page.Profile.Message.ToggleFollow()
            });
        });

    // ── Composition ──

    public static Handler ComposeAll(
        Login login,
        Register register,
        UpdateUser updateUser,
        LoadCurrentUser loadCurrentUser,
        Logout logout,
        LoadArticles loadArticles,
        LoadFeed loadFeed,
        GetArticle getArticle,
        CreateArticle createArticle,
        UpdateArticle updateArticle,
        DeleteArticle deleteArticle,
        FavoriteArticle favoriteArticle,
        UnfavoriteArticle unfavoriteArticle,
        LoadComments loadComments,
        AddComment addComment,
        DeleteComment deleteComment,
        LoadTags loadTags,
        LoadProfile loadProfile,
        FollowUser followUser,
        UnfollowUser unfollowUser) =>
        Pipeline.Compose(
            LoadCurrentUserHandler(loadCurrentUser),
            LoginHandler(login),
            RegisterHandler(register),
            UpdateUserHandler(updateUser),
            LogoutHandler(logout),
            LoadArticlesHandler(loadArticles),
            LoadFeedHandler(loadFeed),
            LoadTagsHandler(loadTags),
            LoadArticleHandler(getArticle),
            LoadArticleForEditorHandler(getArticle),
            LoadCommentsHandler(loadComments),
            SubmitCommentHandler(addComment),
            DeleteCommentHandler(deleteComment),
            CreateArticleHandler(createArticle),
            UpdateArticleHandler(updateArticle),
            ToggleFavoriteHandler(favoriteArticle, unfavoriteArticle),
            DeleteArticleHandler(deleteArticle),
            LoadProfileHandler(loadProfile),
            ToggleFollowHandler(followUser, unfollowUser)
        );
}
