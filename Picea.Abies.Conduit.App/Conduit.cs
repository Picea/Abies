// =============================================================================
// Conduit — Main MVU Program
// =============================================================================

using Picea;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Head;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Conduit.App;

public sealed class ConduitProgram : Program<Model, ConduitStartup>
{
    public static (Model, Command) Initialize(ConduitStartup startup)
    {
        var initialUrl = startup.InitialUrl ?? Url.Root;
        var (page, command) = Route.FromUrl(initialUrl, startup.Session, startup.ApiUrl);
        var model = new Model(page, startup.Session, startup.ApiUrl);
        return (model, command);
    }

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            UrlChanged url => HandleUrlChanged(model, url),

            LoginEmailChanged msg when model.Page is Page.Login login =>
                (model with { Page = new Page.Login(login.Data with { Email = msg.Value }) }, Commands.None),
            LoginPasswordChanged msg when model.Page is Page.Login login =>
                (model with { Page = new Page.Login(login.Data with { Password = msg.Value }) }, Commands.None),
            LoginSubmitted when model.Page is Page.Login login =>
                (model with { Page = new Page.Login(login.Data with { IsSubmitting = true, Errors = [] }) },
                 new LoginUser(model.ApiUrl, login.Data.Email, login.Data.Password)),

            RegisterUsernameChanged msg when model.Page is Page.Register reg =>
                (model with { Page = new Page.Register(reg.Data with { Username = msg.Value }) }, Commands.None),
            RegisterEmailChanged msg when model.Page is Page.Register reg =>
                (model with { Page = new Page.Register(reg.Data with { Email = msg.Value }) }, Commands.None),
            RegisterPasswordChanged msg when model.Page is Page.Register reg =>
                (model with { Page = new Page.Register(reg.Data with { Password = msg.Value }) }, Commands.None),
            RegisterSubmitted when model.Page is Page.Register reg =>
                (model with { Page = new Page.Register(reg.Data with { IsSubmitting = true, Errors = [] }) },
                 new RegisterUser(model.ApiUrl, reg.Data.Username, reg.Data.Email, reg.Data.Password)),

            CommentBodyChanged msg when model.Page is Page.Article art =>
                (model with { Page = new Page.Article(art.Data with { CommentBody = msg.Value }) }, Commands.None),
            CommentSubmitted when model.Page is Page.Article art && model.Session is not null =>
                (model with { Page = new Page.Article(art.Data with { CommentBody = "" }) },
                 new AddComment(model.ApiUrl, model.Session.Token, art.Data.Slug, art.Data.CommentBody)),

            SettingsImageChanged msg when model.Page is Page.Settings settings =>
                (model with { Page = new Page.Settings(settings.Data with { Image = msg.Value }) }, Commands.None),
            SettingsUsernameChanged msg when model.Page is Page.Settings settings =>
                (model with { Page = new Page.Settings(settings.Data with { Username = msg.Value }) }, Commands.None),
            SettingsBioChanged msg when model.Page is Page.Settings settings =>
                (model with { Page = new Page.Settings(settings.Data with { Bio = msg.Value }) }, Commands.None),
            SettingsEmailChanged msg when model.Page is Page.Settings settings =>
                (model with { Page = new Page.Settings(settings.Data with { Email = msg.Value }) }, Commands.None),
            SettingsPasswordChanged msg when model.Page is Page.Settings settings =>
                (model with { Page = new Page.Settings(settings.Data with { Password = msg.Value }) }, Commands.None),
            SettingsSubmitted when model.Page is Page.Settings settings && model.Session is not null =>
                (model with { Page = new Page.Settings(settings.Data with { IsSubmitting = true, Errors = [] }) },
                 new UpdateUser(model.ApiUrl, model.Session.Token,
                     settings.Data.Image, settings.Data.Username, settings.Data.Bio,
                     settings.Data.Email,
                     string.IsNullOrWhiteSpace(settings.Data.Password) ? null : settings.Data.Password)),

            EditorTitleChanged msg when model.Page is Page.Editor editor =>
                (model with { Page = new Page.Editor(editor.Data with { Title = msg.Value }) }, Commands.None),
            EditorDescriptionChanged msg when model.Page is Page.Editor editor =>
                (model with { Page = new Page.Editor(editor.Data with { Description = msg.Value }) }, Commands.None),
            EditorBodyChanged msg when model.Page is Page.Editor editor =>
                (model with { Page = new Page.Editor(editor.Data with { Body = msg.Value }) }, Commands.None),
            EditorTagInputChanged msg when model.Page is Page.Editor editor =>
                (model with { Page = new Page.Editor(editor.Data with { TagInput = msg.Value }) }, Commands.None),
            EditorAddTag when model.Page is Page.Editor editor
                && !string.IsNullOrWhiteSpace(editor.Data.TagInput)
                && !editor.Data.TagList.Contains(editor.Data.TagInput.Trim()) =>
                (model with
                {
                    Page = new Page.Editor(editor.Data with
                    { TagList = editor.Data.TagList.Append(editor.Data.TagInput.Trim()).ToList(), TagInput = "" })
                },
                 Commands.None),
            EditorTagKeyDown { Key: "Enter" } when model.Page is Page.Editor editor
                && !string.IsNullOrWhiteSpace(editor.Data.TagInput)
                && !editor.Data.TagList.Contains(editor.Data.TagInput.Trim()) =>
                (model with
                {
                    Page = new Page.Editor(editor.Data with
                    { TagList = editor.Data.TagList.Append(editor.Data.TagInput.Trim()).ToList(), TagInput = "" })
                },
                 Commands.None),
            EditorTagKeyDown => (model, Commands.None),
            EditorRemoveTag msg when model.Page is Page.Editor editor =>
                (model with
                {
                    Page = new Page.Editor(editor.Data with
                    { TagList = editor.Data.TagList.Where(t => t != msg.Tag).ToList() })
                },
                 Commands.None),
            EditorSubmitted when model.Page is Page.Editor editor && model.Session is not null =>
                HandleEditorSubmitted(model, editor.Data),

            ProfileTabChanged msg when model.Page is Page.Profile profile =>
                HandleProfileTabChanged(model, profile.Data, msg),
            PageChanged msg when model.Page is Page.Profile profile =>
                HandleProfilePageChanged(model, profile.Data, msg),

            FeedTabChanged msg when model.Page is Page.Home home =>
                HandleFeedTabChanged(model, home.Data, msg),
            PageChanged msg when model.Page is Page.Home home =>
                HandlePageChanged(model, home.Data, msg),

            ToggleFavorite msg when model.Session is not null =>
                (model, msg.Favorited
                    ? new UnfavoriteArticle(model.ApiUrl, model.Session.Token, msg.Slug)
                    : new FavoriteArticle(model.ApiUrl, model.Session.Token, msg.Slug)),
            ToggleFollow msg when model.Session is not null =>
                (model, msg.Following
                    ? new UnfollowUser(model.ApiUrl, model.Session.Token, msg.Username)
                    : new FollowUser(model.ApiUrl, model.Session.Token, msg.Username)),

            DeleteArticle msg when model.Session is not null =>
                (model, new DeleteArticleCommand(model.ApiUrl, model.Session.Token, msg.Slug)),
            DeleteComment msg when model.Session is not null =>
                (model, new DeleteCommentCommand(model.ApiUrl, model.Session.Token, msg.Slug, msg.CommentId)),

            ArticlesLoaded msg => HandleArticlesLoaded(model, msg),
            ArticleLoaded msg => HandleArticleLoaded(model, msg),
            CommentsLoaded msg => HandleCommentsLoaded(model, msg),
            TagsLoaded msg => HandleTagsLoaded(model, msg),
            ProfileLoaded msg => HandleProfileLoaded(model, msg),
            UserAuthenticated msg => HandleUserAuthenticated(model, msg),
            FavoriteToggled msg => HandleFavoriteToggled(model, msg),
            FollowToggled msg => HandleFollowToggled(model, msg),
            CommentAdded msg => HandleCommentAdded(model, msg),
            CommentDeleted msg => HandleCommentDeleted(model, msg),
            ArticleDeleted => HandleArticleDeleted(model),
            UserUpdated msg => HandleUserUpdated(model, msg),
            ArticleSaved msg => HandleArticleSaved(model, msg),

            Logout =>
                (model with { Session = null, Page = new Page.Home(new HomeModel(FeedTab.Global, null, [], 0, 1, [], true)) },
                 Commands.Batch(
                     new ClearPersistedSession(),
                     new FetchArticles(model.ApiUrl, null, Constants.ArticlesPerPage, 0),
                     new FetchTags(model.ApiUrl))),

            ApiError msg => HandleApiError(model, msg),
            _ => (model, Commands.None)
        };

    public static Result<Message[], Message> Decide(Model model, Message command) =>
        DecideDecision(model, command);

    public static bool IsTerminal(Model _) => false;

    public static Document View(Model model)
    {
        var content = model.Page switch
        {
            Page.Home home => Pages.Home.View(home.Data, model.Session),
            Page.Login login => Pages.Login.View(login.Data),
            Page.Register reg => Pages.Register.View(reg.Data),
            Page.Article art => Pages.Article.View(art.Data, model.Session),
            Page.Settings settings => Pages.Settings.View(settings.Data),
            Page.Editor editor => Pages.Editor.View(editor.Data),
            Page.Profile profile => Pages.Profile.View(profile.Data, model.Session),
            Page.NotFound => div([], [text("Page not found.")]),
            _ => div([], [text("Coming soon...")])
        };

        var title = model.Page switch
        {
            Page.Home => "Conduit",
            Page.Login => "Sign in \u2014 Conduit",
            Page.Register => "Sign up \u2014 Conduit",
            Page.Article { Data.Article: not null } art => $"{art.Data.Article.Title} \u2014 Conduit",
            Page.Settings => "Settings \u2014 Conduit",
            Page.Editor { Data.Slug: not null } => "Edit Article \u2014 Conduit",
            Page.Editor => "New Article \u2014 Conduit",
            Page.Profile { Data.Username: var u } => $"{u} \u2014 Conduit",
            _ => "Conduit"
        };

        return new Document(
            title,
            Views.Layout.Page(model.Page, model.Session, content),
            meta("otel-verbosity", "user"),
            stylesheet("//code.ionicframework.com/ionicons/2.0.1/css/ionicons.min.css"),
            stylesheet("//fonts.googleapis.com/css?family=Titillium+Web:700|Source+Serif+Pro:400,700|Merriweather+Sans:400,700|Source+Sans+Pro:400,300,600,700,300italic,400italic,600italic,700italic"),
            stylesheet("/main.css"));
    }

    public static Subscription Subscriptions(Model model) =>
        Navigation.UrlChanges(url => new UrlChanged(url));

    // ─── Transition Handlers ─────────────────────────────────────────────────

    private static (Model, Command) HandleUrlChanged(Model model, UrlChanged msg)
    {
        var (page, command) = Route.FromUrl(msg.Url, model.Session, model.ApiUrl);
        return (model with { Page = page }, command);
    }

    private static (Model, Command) HandleFeedTabChanged(Model model, HomeModel home, FeedTabChanged msg)
    {
        var newHome = home with { ActiveTab = msg.Tab, SelectedTag = msg.Tag, Articles = [], ArticlesCount = 0, CurrentPage = 1, IsLoading = true };
        var command = msg.Tab switch
        {
            FeedTab.Your when model.Session is not null => (Command)new FetchFeed(model.ApiUrl, model.Session.Token, Constants.ArticlesPerPage, 0),
            FeedTab.Tag when msg.Tag is not null => new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, 0, Tag: msg.Tag),
            _ => new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, 0)
        };
        var url = Route.ToUrl(new Page.Home(newHome));
        return (model with { Page = new Page.Home(newHome) }, Commands.Batch(command, Navigation.PushUrl(url)));
    }

    private static (Model, Command) HandlePageChanged(Model model, HomeModel home, PageChanged msg)
    {
        var offset = (msg.PageNumber - 1) * Constants.ArticlesPerPage;
        var newHome = home with { CurrentPage = msg.PageNumber, IsLoading = true };
        var command = home.ActiveTab switch
        {
            FeedTab.Your when model.Session is not null => (Command)new FetchFeed(model.ApiUrl, model.Session.Token, Constants.ArticlesPerPage, offset),
            FeedTab.Tag when home.SelectedTag is not null => new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, offset, Tag: home.SelectedTag),
            _ => new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, offset)
        };
        return (model with { Page = new Page.Home(newHome) }, command);
    }

    private static (Model, Command) HandleArticlesLoaded(Model model, ArticlesLoaded msg) =>
        model.Page switch
        {
            Page.Home home => (model with { Page = new Page.Home(home.Data with { Articles = msg.Articles, ArticlesCount = msg.ArticlesCount, IsLoading = false }) }, Commands.None),
            Page.Profile profile => (model with { Page = new Page.Profile(profile.Data with { Articles = msg.Articles, ArticlesCount = msg.ArticlesCount, IsLoading = false }) }, Commands.None),
            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleArticleLoaded(Model model, ArticleLoaded msg) =>
        model.Page switch
        {
            Page.Article art => (model with { Page = new Page.Article(art.Data with { Article = msg.Article, IsLoading = false }) }, Commands.None),
            Page.Editor editor => (model with { Page = new Page.Editor(editor.Data with { Title = msg.Article.Title, Description = msg.Article.Description, Body = msg.Article.Body, TagList = msg.Article.TagList }) }, Commands.None),
            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleCommentsLoaded(Model model, CommentsLoaded msg) =>
        model.Page is Page.Article art
            ? (model with { Page = new Page.Article(art.Data with { Comments = msg.Comments }) }, Commands.None)
            : (model, Commands.None);

    private static (Model, Command) HandleTagsLoaded(Model model, TagsLoaded msg) =>
        model.Page is Page.Home home
            ? (model with { Page = new Page.Home(home.Data with { PopularTags = msg.Tags }) }, Commands.None)
            : (model, Commands.None);

    private static (Model, Command) HandleProfileLoaded(Model model, ProfileLoaded msg) =>
        model.Page is Page.Profile profile
            ? (model with { Page = new Page.Profile(profile.Data with { Profile = msg.Profile, IsLoading = false }) }, Commands.None)
            : (model, Commands.None);

    private static (Model, Command) HandleUserAuthenticated(Model model, UserAuthenticated msg)
    {
        var newModel = model with { Session = msg.Session };
        var authenticatedHomeUrl = new Url([], new Dictionary<string, string> { ["feed"] = "following" }, Option<string>.None);
        var (page, command) = Route.FromUrl(authenticatedHomeUrl, msg.Session, model.ApiUrl);
        return (newModel with { Page = page }, Commands.Batch(new PersistSession(msg.Session), command, Navigation.PushUrl(authenticatedHomeUrl)));
    }

    private static (Model, Command) HandleFavoriteToggled(Model model, FavoriteToggled msg) =>
        model.Page switch
        {
            Page.Home home => (model with { Page = new Page.Home(home.Data with { Articles = home.Data.Articles.Select(a => a.Slug == msg.Article.Slug ? msg.Article : a).ToList() }) }, Commands.None),
            Page.Article art when art.Data.Article is not null && art.Data.Article.Slug == msg.Article.Slug =>
                (model with { Page = new Page.Article(art.Data with { Article = art.Data.Article with { Favorited = msg.Article.Favorited, FavoritesCount = msg.Article.FavoritesCount } }) }, Commands.None),
            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleFollowToggled(Model model, FollowToggled msg) =>
        model.Page switch
        {
            Page.Article art when art.Data.Article is not null =>
                (model with { Page = new Page.Article(art.Data with { Article = art.Data.Article with { Author = new AuthorData(msg.Profile.Username, msg.Profile.Bio, msg.Profile.Image, msg.Profile.Following) } }) }, Commands.None),
            Page.Profile profile when profile.Data.Profile is not null && profile.Data.Profile.Username == msg.Profile.Username =>
                (model with { Page = new Page.Profile(profile.Data with { Profile = msg.Profile }) }, Commands.None),
            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleCommentAdded(Model model, CommentAdded msg) =>
        model.Page is Page.Article art
            ? (model with { Page = new Page.Article(art.Data with { Comments = art.Data.Comments.Prepend(msg.Comment).ToList() }) }, Commands.None)
            : (model, Commands.None);

    private static (Model, Command) HandleCommentDeleted(Model model, CommentDeleted msg) =>
        model.Page is Page.Article art
            ? (model with { Page = new Page.Article(art.Data with { Comments = art.Data.Comments.Where(c => c.Id != msg.CommentId).ToList() }) }, Commands.None)
            : (model, Commands.None);

    private static (Model, Command) HandleArticleDeleted(Model model)
    {
        var (page, command) = Route.FromUrl(Url.Root, model.Session, model.ApiUrl);
        return (model with { Page = page }, Commands.Batch(command, Navigation.PushUrl(Url.Root)));
    }

    private static (Model, Command) HandleApiError(Model model, ApiError msg) =>
        model.Page switch
        {
            Page.Login login => (model with { Page = new Page.Login(login.Data with { Errors = msg.Errors, IsSubmitting = false }) }, Commands.None),
            Page.Register reg => (model with { Page = new Page.Register(reg.Data with { Errors = msg.Errors, IsSubmitting = false }) }, Commands.None),
            Page.Settings settings => (model with { Page = new Page.Settings(settings.Data with { Errors = msg.Errors, IsSubmitting = false }) }, Commands.None),
            Page.Editor editor => (model with { Page = new Page.Editor(editor.Data with { Errors = msg.Errors, IsSubmitting = false }) }, Commands.None),
            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleUserUpdated(Model model, UserUpdated msg) =>
        (model with { Session = msg.Session, Page = new Page.Settings(new SettingsModel(msg.Session.Image ?? "", msg.Session.Username, msg.Session.Bio, msg.Session.Email, "", [], false)) }, new PersistSession(msg.Session));

    private static (Model, Command) HandleArticleSaved(Model model, ArticleSaved msg)
    {
        var url = new Url(["article", msg.Slug], new Dictionary<string, string>(), Option<string>.None);
        var (page, command) = Route.FromUrl(url, model.Session, model.ApiUrl);
        return (model with { Page = page }, Commands.Batch(command, Navigation.PushUrl(url)));
    }

    private static (Model, Command) HandleEditorSubmitted(Model model, EditorModel editor)
    {
        var newModel = model with { Page = new Page.Editor(editor with { IsSubmitting = true, Errors = [] }) };
        Command command = editor.Slug is not null
            ? new UpdateArticle(model.ApiUrl, model.Session!.Token, editor.Slug, editor.Title, editor.Description, editor.Body, editor.TagList)
            : new CreateArticle(model.ApiUrl, model.Session!.Token, editor.Title, editor.Description, editor.Body, editor.TagList);
        return (newModel, command);
    }

    private static (Model, Command) HandleProfileTabChanged(Model model, ProfileModel profile, ProfileTabChanged msg)
    {
        var newProfile = profile with { ShowFavorites = msg.ShowFavorites, Articles = [], ArticlesCount = 0, CurrentPage = 1, IsLoading = true };
        Command articleCmd = msg.ShowFavorites
            ? new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, 0, Favorited: profile.Username)
            : new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, 0, Author: profile.Username);
        var url = Route.ToUrl(new Page.Profile(newProfile));
        return (model with { Page = new Page.Profile(newProfile) }, Commands.Batch(articleCmd, Navigation.PushUrl(url)));
    }

    private static (Model, Command) HandleProfilePageChanged(Model model, ProfileModel profile, PageChanged msg)
    {
        var offset = (msg.PageNumber - 1) * Constants.ArticlesPerPage;
        var newProfile = profile with { CurrentPage = msg.PageNumber, IsLoading = true };
        Command articleCmd = profile.ShowFavorites
            ? new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, offset, Favorited: profile.Username)
            : new FetchArticles(model.ApiUrl, model.Session?.Token, Constants.ArticlesPerPage, offset, Author: profile.Username);
        return (model with { Page = new Page.Profile(newProfile) }, articleCmd);
    }

    private static Result<Message[], Message> DecideDecision(Model model, Message command) =>
        command switch
        {
            LoginEmailChanged or LoginPasswordChanged when model.Page is not Page.Login =>
                Result<Message[], Message>.Err(new ApiError(["Login form is not active."])),
            LoginSubmitted when model.Page is not Page.Login =>
                Result<Message[], Message>.Err(new ApiError(["Login form is not active."])),
            LoginSubmitted when model.Page is Page.Login login
                && (string.IsNullOrWhiteSpace(login.Data.Email) || string.IsNullOrWhiteSpace(login.Data.Password)) =>
                Result<Message[], Message>.Err(new ApiError(["Email and password are required."])),

            RegisterUsernameChanged or RegisterEmailChanged or RegisterPasswordChanged when model.Page is not Page.Register =>
                Result<Message[], Message>.Err(new ApiError(["Registration form is not active."])),
            RegisterSubmitted when model.Page is not Page.Register =>
                Result<Message[], Message>.Err(new ApiError(["Registration form is not active."])),
            RegisterSubmitted when model.Page is Page.Register reg
                && (string.IsNullOrWhiteSpace(reg.Data.Username)
                    || string.IsNullOrWhiteSpace(reg.Data.Email)
                    || string.IsNullOrWhiteSpace(reg.Data.Password)) =>
                Result<Message[], Message>.Err(new ApiError(["Username, email, and password are required."])),

            CommentBodyChanged when model.Page is not Page.Article =>
                Result<Message[], Message>.Err(new ApiError(["Comments are only available on article pages."])),
            CommentSubmitted => model.Page is Page.Article article
                ? model.Session is null
                    ? Result<Message[], Message>.Err(new ApiError(["You must be signed in to comment."]))
                    : string.IsNullOrWhiteSpace(article.Data.CommentBody)
                        ? Result<Message[], Message>.Err(new ApiError(["Comment body is required."]))
                        : Result<Message[], Message>.Ok([command])
                : Result<Message[], Message>.Err(new ApiError(["Comments are only available on article pages."])),

            SettingsImageChanged or SettingsUsernameChanged or SettingsBioChanged or SettingsEmailChanged or SettingsPasswordChanged
                when model.Page is not Page.Settings =>
                Result<Message[], Message>.Err(new ApiError(["Settings form is not active."])),
            SettingsSubmitted => model.Page is Page.Settings settings
                ? model.Session is null
                    ? Result<Message[], Message>.Err(new ApiError(["You must be signed in to update settings."]))
                    : string.IsNullOrWhiteSpace(settings.Data.Username) || string.IsNullOrWhiteSpace(settings.Data.Email)
                        ? Result<Message[], Message>.Err(new ApiError(["Username and email are required."]))
                        : Result<Message[], Message>.Ok([command])
                : Result<Message[], Message>.Err(new ApiError(["Settings form is not active."])),

            EditorTitleChanged or EditorDescriptionChanged or EditorBodyChanged or EditorTagInputChanged or EditorRemoveTag
                when model.Page is not Page.Editor =>
                Result<Message[], Message>.Err(new ApiError(["Editor form is not active."])),
            EditorAddTag => model.Page is Page.Editor editor
                ? CanAddTag(editor.Data)
                    ? Result<Message[], Message>.Ok([command])
                    : Result<Message[], Message>.Err(new ApiError(["Tag must be non-empty and unique."]))
                : Result<Message[], Message>.Err(new ApiError(["Editor form is not active."])),
            EditorTagKeyDown { Key: "Enter" } => model.Page is Page.Editor editor
                ? CanAddTag(editor.Data)
                    ? Result<Message[], Message>.Ok([command])
                    : Result<Message[], Message>.Err(new ApiError(["Tag must be non-empty and unique."]))
                : Result<Message[], Message>.Err(new ApiError(["Editor form is not active."])),
            EditorTagKeyDown => model.Page is Page.Editor
                ? Result<Message[], Message>.Ok([command])
                : Result<Message[], Message>.Err(new ApiError(["Editor form is not active."])),
            EditorSubmitted => model.Page is Page.Editor editor
                ? model.Session is null
                    ? Result<Message[], Message>.Err(new ApiError(["You must be signed in to save an article."]))
                    : IsEditorSubmissionValid(editor.Data)
                        ? Result<Message[], Message>.Ok([command])
                        : Result<Message[], Message>.Err(new ApiError(["Title, description, and body are required."]))
                : Result<Message[], Message>.Err(new ApiError(["Editor form is not active."])),

            ProfileTabChanged when model.Page is not Page.Profile =>
                Result<Message[], Message>.Err(new ApiError(["Profile navigation is not active."])),
            FeedTabChanged when model.Page is not Page.Home =>
                Result<Message[], Message>.Err(new ApiError(["Feed navigation is not active."])),
            PageChanged msg when msg.PageNumber <= 0 =>
                Result<Message[], Message>.Err(new ApiError(["Page number must be greater than zero."])),
            PageChanged when model.Page is not (Page.Home or Page.Profile) =>
                Result<Message[], Message>.Err(new ApiError(["Pagination is not active on this page."])),

            ToggleFavorite or ToggleFollow or DeleteArticle or DeleteComment when model.Session is null =>
                Result<Message[], Message>.Err(new ApiError(["You must be signed in to perform this action."])),

            _ => Result<Message[], Message>.Ok([command])
        };

    private static bool CanAddTag(EditorModel editor) =>
        !string.IsNullOrWhiteSpace(editor.TagInput)
        && !editor.TagList.Contains(editor.TagInput.Trim());

    private static bool IsEditorSubmissionValid(EditorModel editor) =>
        !string.IsNullOrWhiteSpace(editor.Title)
        && !string.IsNullOrWhiteSpace(editor.Description)
        && !string.IsNullOrWhiteSpace(editor.Body);
}
