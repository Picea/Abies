using Abies.Conduit.Main;
using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit;

public class LoginCommand : ApiCommand<Page.Login.Message.LoginSuccess>
{
    private readonly string _email;
    private readonly string _password;

    public LoginCommand(string email, string password)
    {
        _email = email;
        _password = password;
    }

    public override async Task<Page.Login.Message.LoginSuccess> ExecuteAsync()
    {
        try
        {
            var user = await AuthService.LoginAsync(_email, _password);
            return new Page.Login.Message.LoginSuccess(user);
        }
        catch (ApiException ex)
        {
            string[] errors = { "Invalid email or password" };
            await RuntimeDispatcher.Dispatch(new Page.Login.Message.LoginError(errors));
            throw;
        }
        catch (Exception)
        {
            string[] errors = { "An unexpected error occurred" };
            await RuntimeDispatcher.Dispatch(new Page.Login.Message.LoginError(errors));
            throw;
        }
    }
}

public class RegisterCommand : ApiCommand<Page.Register.Message.RegisterSuccess>
{
    private readonly string _username;
    private readonly string _email;
    private readonly string _password;

    public RegisterCommand(string username, string email, string password)
    {
        _username = username;
        _email = email;
        _password = password;
    }

    public override async Task<Page.Register.Message.RegisterSuccess> ExecuteAsync()
    {
        try
        {
            var user = await AuthService.RegisterAsync(_username, _email, _password);
            return new Page.Register.Message.RegisterSuccess(user);
        }
        catch (ApiException ex)
        {
            await RuntimeDispatcher.Dispatch(new Page.Register.Message.RegisterError(ex.Errors));
            throw;
        }
        catch (Exception)
        {
            var errors = new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "error", new[] { "An unexpected error occurred" } }
            };
            await RuntimeDispatcher.Dispatch(new Page.Register.Message.RegisterError(errors));
            throw;
        }
    }
}

public class UpdateUserCommand : ApiCommand<Page.Settings.Message.SettingsSuccess>
{
    private readonly string _username;
    private readonly string _email;
    private readonly string _bio;
    private readonly string _image;
    private readonly string? _password;

    public UpdateUserCommand(string username, string email, string bio, string image, string? password = null)
    {
        _username = username;
        _email = email;
        _bio = bio;
        _image = image;
        _password = password;
    }

    public override async Task<Page.Settings.Message.SettingsSuccess> ExecuteAsync()
    {
        try
        {
            var user = await AuthService.UpdateUserAsync(_username, _email, _bio, _image, _password);
            return new Page.Settings.Message.SettingsSuccess(user);
        }
        catch (ApiException ex)
        {
            await RuntimeDispatcher.Dispatch(new Page.Settings.Message.SettingsError(ex.Errors));
            throw;
        }
        catch (Exception)
        {
            var errors = new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "error", new[] { "An unexpected error occurred" } }
            };
            await RuntimeDispatcher.Dispatch(new Page.Settings.Message.SettingsError(errors));
            throw;
        }
    }
}

public class LoadArticlesCommand : ApiCommand<Page.Home.Message.ArticlesLoaded>
{
    private readonly string? _tag;
    private readonly string? _author;
    private readonly string? _favoritedBy;
    private readonly int _limit;
    private readonly int _offset;

    public LoadArticlesCommand(string? tag = null, string? author = null, string? favoritedBy = null, int limit = 10, int offset = 0)
    {
        _tag = tag;
        _author = author;
        _favoritedBy = favoritedBy;
        _limit = limit;
        _offset = offset;
    }

    public override async Task<Page.Home.Message.ArticlesLoaded> ExecuteAsync()
    {
        try
        {
            var (articles, count) = await ArticleService.GetArticlesAsync(_tag, _author, _favoritedBy, _limit, _offset);
            return new Page.Home.Message.ArticlesLoaded(articles, count);
        }
        catch (Exception)
        {
            return new Page.Home.Message.ArticlesLoaded(new System.Collections.Generic.List<Page.Home.Article>(), 0);
        }
    }
}

public class LoadFeedCommand : ApiCommand<Page.Home.Message.ArticlesLoaded>
{
    private readonly int _limit;
    private readonly int _offset;

    public LoadFeedCommand(int limit = 10, int offset = 0)
    {
        _limit = limit;
        _offset = offset;
    }

    public override async Task<Page.Home.Message.ArticlesLoaded> ExecuteAsync()
    {
        try
        {
            var (articles, count) = await ArticleService.GetFeedArticlesAsync(_limit, _offset);
            return new Page.Home.Message.ArticlesLoaded(articles, count);
        }
        catch (Exception)
        {
            return new Page.Home.Message.ArticlesLoaded(new System.Collections.Generic.List<Page.Home.Article>(), 0);
        }
    }
}

public class LoadTagsCommand : ApiCommand<Page.Home.Message.TagsLoaded>
{
    public override async Task<Page.Home.Message.TagsLoaded> ExecuteAsync()
    {
        try
        {
            var tags = await TagService.GetTagsAsync();
            return new Page.Home.Message.TagsLoaded(tags);
        }
        catch (Exception)
        {
            return new Page.Home.Message.TagsLoaded(new System.Collections.Generic.List<string>());
        }
    }
}

public class LoadArticleCommand : ApiCommand<Page.Article.Message.ArticleLoaded>
{
    private readonly string _slug;

    public LoadArticleCommand(string slug)
    {
        _slug = slug;
    }

    public override async Task<Page.Article.Message.ArticleLoaded> ExecuteAsync()
    {
        try
        {
            var article = await ArticleService.GetArticleAsync(_slug);
            return new Page.Article.Message.ArticleLoaded(article);
        }
        catch (Exception)
        {
            return new Page.Article.Message.ArticleLoaded(null);
        }
    }
}

public class LoadCommentsCommand : ApiCommand<Page.Article.Message.CommentsLoaded>
{
    private readonly string _slug;

    public LoadCommentsCommand(string slug)
    {
        _slug = slug;
    }

    public override async Task<Page.Article.Message.CommentsLoaded> ExecuteAsync()
    {
        try
        {
            var comments = await ArticleService.GetCommentsAsync(_slug);
            return new Page.Article.Message.CommentsLoaded(comments);
        }
        catch (Exception)
        {
            return new Page.Article.Message.CommentsLoaded(new List<Page.Article.Comment>());
        }
    }
}

public class SubmitCommentCommand : ApiCommand
{
    private readonly string _slug;
    private readonly string _body;

    public SubmitCommentCommand(string slug, string body)
    {
        _slug = slug;
        _body = body;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            var comment = await ArticleService.AddCommentAsync(_slug, _body);
            return new Page.Article.Message.CommentSubmitted(comment);
        }
        catch (Exception)
        {
            return new Page.Article.Message.SubmitComment();
        }
    }
}

public class DeleteCommentCommand : ApiCommand
{
    private readonly string _slug;
    private readonly string _commentId;

    public DeleteCommentCommand(string slug, string commentId)
    {
        _slug = slug;
        _commentId = commentId;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            await ArticleService.DeleteCommentAsync(_slug, _commentId);
            return new Page.Article.Message.CommentDeleted(_commentId);
        }
        catch (Exception)
        {
            return new Page.Article.Message.CommentDeleted("");
        }
    }
}

public class CreateArticleCommand : ApiCommand
{
    private readonly string _title;
    private readonly string _description;
    private readonly string _body;
    private readonly System.Collections.Generic.List<string> _tagList;

    public CreateArticleCommand(string title, string description, string body, System.Collections.Generic.List<string> tagList)
    {
        _title = title;
        _description = description;
        _body = body;
        _tagList = tagList;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            var article = await ArticleService.CreateArticleAsync(_title, _description, _body, _tagList);
            return new Page.Editor.Message.ArticleSubmitSuccess(article.Slug);
        }
        catch (ApiException ex)
        {
            return new Page.Editor.Message.ArticleSubmitError(ex.Errors);
        }
        catch (Exception)
        {
            var errors = new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "error", new[] { "An unexpected error occurred" } }
            };
            return new Page.Editor.Message.ArticleSubmitError(errors);
        }
    }
}

public class UpdateArticleCommand : ApiCommand
{
    private readonly string _slug;
    private readonly string _title;
    private readonly string _description;
    private readonly string _body;

    public UpdateArticleCommand(string slug, string title, string description, string body)
    {
        _slug = slug;
        _title = title;
        _description = description;
        _body = body;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            var article = await ArticleService.UpdateArticleAsync(_slug, _title, _description, _body);
            return new Page.Editor.Message.ArticleSubmitSuccess(article.Slug);
        }
        catch (ApiException ex)
        {
            return new Page.Editor.Message.ArticleSubmitError(ex.Errors);
        }
        catch (Exception)
        {
            var errors = new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "error", new[] { "An unexpected error occurred" } }
            };
            return new Page.Editor.Message.ArticleSubmitError(errors);
        }
    }
}

public class ToggleFavoriteCommand : ApiCommand
{
    private readonly string _slug;
    private readonly bool _currentState;

    public ToggleFavoriteCommand(string slug, bool currentState)
    {
        _slug = slug;
        _currentState = currentState;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            var article = _currentState
                ? await ArticleService.UnfavoriteArticleAsync(_slug)
                : await ArticleService.FavoriteArticleAsync(_slug);
                
            return new Page.Article.Message.ArticleLoaded(article);
        }
        catch (Exception)
        {
            return new Page.Article.Message.ToggleFavorite();
        }
    }
}

public class LoadProfileCommand : ApiCommand<Page.Profile.Message.ProfileLoaded>
{
    private readonly string _username;

    public LoadProfileCommand(string username)
    {
        _username = username;
    }

    public override async Task<Page.Profile.Message.ProfileLoaded> ExecuteAsync()
    {
        try
        {
            var profile = await ProfileService.GetProfileAsync(_username);
            return new Page.Profile.Message.ProfileLoaded(profile);
        }
        catch (Exception)
        {
            return new Page.Profile.Message.ProfileLoaded(new Page.Home.Profile(_username, "", "", false));
        }
    }
}

public class ToggleFollowCommand : ApiCommand
{
    private readonly string _username;
    private readonly bool _currentState;

    public ToggleFollowCommand(string username, bool currentState)
    {
        _username = username;
        _currentState = currentState;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            var profile = _currentState
                ? await ProfileService.UnfollowUserAsync(_username)
                : await ProfileService.FollowUserAsync(_username);
                
            return new Page.Profile.Message.ProfileLoaded(profile);
        }
        catch (Exception)
        {
            return new Page.Profile.Message.ToggleFollow();
        }
    }
}

public class DeleteArticleCommand : ApiCommand
{
    private readonly string _slug;

    public DeleteArticleCommand(string slug)
    {
        _slug = slug;
    }

    public override async Task<Abies.Message> ExecuteAsync()
    {
        try
        {
            await ArticleService.DeleteArticleAsync(_slug);
            return new Page.Article.Message.DeleteArticle();
        }
        catch (Exception)
        {
            return new Page.Article.Message.DeleteArticle();
        }
    }
}
