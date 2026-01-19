using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Abies.Conduit.IntegrationTests.Testing;

/// <summary>
/// A stateful fake API that simulates the Conduit backend.
/// Tracks users, articles, comments, favorites, and follows across multiple users.
/// This enables testing multi-user scenarios like "User B favorites User A's article".
/// </summary>
public sealed class StatefulFakeApi : HttpMessageHandler
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // State stores
    private readonly Dictionary<string, FakeUser> _users = new();
    private readonly Dictionary<string, FakeArticle> _articles = new();
    private readonly Dictionary<string, List<FakeComment>> _comments = new(); // slug -> comments
    private readonly HashSet<(string Username, string Slug)> _favorites = new();
    private readonly HashSet<(string Follower, string Followed)> _follows = new();

    private int _nextCommentId = 1;

    // Current authenticated user (set via Authorization header)
    private string? _currentUsername;

    public List<(HttpMethod Method, Uri Uri, string? Body, string? AuthUser)> Requests { get; } = new();

    #region Setup Methods

    /// <summary>
    /// Add a user to the fake API.
    /// </summary>
    public FakeUser AddUser(string username, string email, string password, string? bio = null, string? image = null)
    {
        var token = $"jwt-{username}-{Guid.NewGuid():N}";
        var user = new FakeUser(username, email, password, token, bio ?? "", image ?? "");
        _users[username] = user;
        return user;
    }

    /// <summary>
    /// Add an article to the fake API.
    /// </summary>
    public FakeArticle AddArticle(string slug, string title, string description, string body, 
        string authorUsername, List<string>? tagList = null)
    {
        var author = _users.GetValueOrDefault(authorUsername) 
            ?? throw new InvalidOperationException($"Author '{authorUsername}' not found. Add user first.");
        
        var article = new FakeArticle(
            slug, title, description, body,
            tagList ?? [],
            DateTime.UtcNow.ToString("o"),
            DateTime.UtcNow.ToString("o"),
            authorUsername);
        
        _articles[slug] = article;
        _comments[slug] = new List<FakeComment>();
        return article;
    }

    /// <summary>
    /// Add a comment to an article.
    /// </summary>
    public FakeComment AddComment(string slug, string authorUsername, string body)
    {
        if (!_articles.ContainsKey(slug))
            throw new InvalidOperationException($"Article '{slug}' not found.");
        if (!_users.ContainsKey(authorUsername))
            throw new InvalidOperationException($"User '{authorUsername}' not found.");

        var comment = new FakeComment(_nextCommentId++, body, authorUsername, DateTime.UtcNow.ToString("o"));
        _comments[slug].Add(comment);
        return comment;
    }

    /// <summary>
    /// Set a favorite relationship (user favorites an article).
    /// </summary>
    public void SetFavorite(string username, string slug, bool isFavorited)
    {
        if (isFavorited)
            _favorites.Add((username, slug));
        else
            _favorites.Remove((username, slug));
    }

    /// <summary>
    /// Set a follow relationship (follower follows followed).
    /// </summary>
    public void SetFollow(string followerUsername, string followedUsername, bool isFollowing)
    {
        if (isFollowing)
            _follows.Add((followerUsername, followedUsername));
        else
            _follows.Remove((followerUsername, followedUsername));
    }

    /// <summary>
    /// Check if a user has favorited an article.
    /// </summary>
    public bool IsFavorited(string username, string slug) =>
        _favorites.Contains((username, slug));

    /// <summary>
    /// Get the favorite count for an article.
    /// </summary>
    public int GetFavoritesCount(string slug) =>
        _favorites.Count(f => f.Slug == slug);

    /// <summary>
    /// Check if a user is following another user.
    /// </summary>
    public bool IsFollowing(string follower, string followed) =>
        _follows.Contains((follower, followed));

    #endregion

    #region HTTP Handler

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("RequestUri is required");
        var path = uri.AbsolutePath;
        var query = uri.Query;

        // Extract current user from Authorization header
        _currentUsername = ExtractUsername(request);

        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        Requests.Add((request.Method, uri, body, _currentUsername));

        try
        {
            return (request.Method.Method, path) switch
            {
                // Auth
                ("POST", "/api/users/login") => HandleLogin(body),
                ("POST", "/api/users") => HandleRegister(body),
                ("GET", "/api/user") => HandleGetCurrentUser(),
                
                // Articles
                ("GET", var p) when p == "/api/articles" => HandleGetArticles(query),
                ("GET", var p) when p.StartsWith("/api/articles/") && !p.Contains("/comments") && !p.Contains("/favorite") 
                    => HandleGetArticle(ExtractSlug(p)),
                ("POST", "/api/articles") => HandleCreateArticle(body),
                ("DELETE", var p) when p.StartsWith("/api/articles/") && !p.Contains("/comments") && !p.Contains("/favorite")
                    => HandleDeleteArticle(ExtractSlug(p)),
                
                // Favorites
                ("POST", var p) when p.EndsWith("/favorite") => HandleFavorite(ExtractSlugFromFavorite(p)),
                ("DELETE", var p) when p.EndsWith("/favorite") => HandleUnfavorite(ExtractSlugFromFavorite(p)),
                
                // Comments
                ("GET", var p) when p.Contains("/comments") => HandleGetComments(ExtractSlugFromComments(p)),
                ("POST", var p) when p.Contains("/comments") => HandleAddComment(ExtractSlugFromComments(p), body),
                ("DELETE", var p) when Regex.IsMatch(p, @"/api/articles/.+/comments/\d+") 
                    => HandleDeleteComment(ExtractSlugFromComments(p), ExtractCommentId(p)),
                
                // Profiles
                ("GET", var p) when p.StartsWith("/api/profiles/") && !p.Contains("/follow")
                    => HandleGetProfile(ExtractUsername(p)),
                ("POST", var p) when p.EndsWith("/follow") => HandleFollow(ExtractUsernameFromFollow(p)),
                ("DELETE", var p) when p.EndsWith("/follow") => HandleUnfollow(ExtractUsernameFromFollow(p)),
                
                // Tags
                ("GET", "/api/tags") => HandleGetTags(),
                
                _ => NotFound($"No handler for {request.Method} {path}")
            };
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    private string? ExtractUsername(HttpRequestMessage request)
    {
        var auth = request.Headers.Authorization;
        if (auth?.Scheme == "Token" && auth.Parameter is not null)
        {
            // Token format: jwt-{username}-{guid}
            var parts = auth.Parameter.Split('-');
            if (parts.Length >= 2)
                return parts[1];
        }
        return null;
    }

    #endregion

    #region Route Handlers

    private HttpResponseMessage HandleLogin(string? body)
    {
        var request = Deserialize<LoginRequest>(body);
        var user = _users.Values.FirstOrDefault(u => u.Email == request.User.Email && u.Password == request.User.Password);
        
        if (user is null)
            return ValidationError("email or password", "is invalid");

        return Json(new { user = UserToDto(user) });
    }

    private HttpResponseMessage HandleRegister(string? body)
    {
        var request = Deserialize<RegisterRequest>(body);
        
        if (_users.ContainsKey(request.User.Username))
            return ValidationError("username", "has already been taken");
        if (_users.Values.Any(u => u.Email == request.User.Email))
            return ValidationError("email", "has already been taken");

        var user = AddUser(request.User.Username, request.User.Email, request.User.Password);
        return Json(new { user = UserToDto(user) });
    }

    private HttpResponseMessage HandleGetCurrentUser()
    {
        if (_currentUsername is null || !_users.TryGetValue(_currentUsername, out var user))
            return Unauthorized();

        return Json(new { user = UserToDto(user) });
    }

    private HttpResponseMessage HandleGetArticles(string query)
    {
        var articles = _articles.Values.AsEnumerable();

        // Parse query params
        var queryParams = System.Web.HttpUtility.ParseQueryString(query);
        var tag = queryParams["tag"];
        var author = queryParams["author"];
        var favorited = queryParams["favorited"];
        var limit = int.TryParse(queryParams["limit"], out var l) ? l : 10;
        var offset = int.TryParse(queryParams["offset"], out var o) ? o : 0;

        if (!string.IsNullOrEmpty(tag))
            articles = articles.Where(a => a.TagList.Contains(tag));
        if (!string.IsNullOrEmpty(author))
            articles = articles.Where(a => a.AuthorUsername == author);
        if (!string.IsNullOrEmpty(favorited))
            articles = articles.Where(a => _favorites.Contains((favorited, a.Slug)));

        var total = articles.Count();
        var page = articles.Skip(offset).Take(limit).ToList();

        return Json(new
        {
            articles = page.Select(a => ArticleToDto(a, _currentUsername)).ToList(),
            articlesCount = total
        });
    }

    private HttpResponseMessage HandleGetArticle(string slug)
    {
        if (!_articles.TryGetValue(slug, out var article))
            return NotFound($"Article '{slug}' not found");

        return Json(new { article = ArticleToDto(article, _currentUsername) });
    }

    private HttpResponseMessage HandleCreateArticle(string? body)
    {
        if (_currentUsername is null)
            return Unauthorized();

        var request = Deserialize<CreateArticleRequest>(body);
        var slug = Slugify(request.Article.Title);
        
        var article = AddArticle(slug, request.Article.Title, request.Article.Description, 
            request.Article.Body, _currentUsername, request.Article.TagList);

        return Json(new { article = ArticleToDto(article, _currentUsername) });
    }

    private HttpResponseMessage HandleDeleteArticle(string slug)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_articles.TryGetValue(slug, out var article))
            return NotFound($"Article '{slug}' not found");

        if (article.AuthorUsername != _currentUsername)
            return Forbidden("You can only delete your own articles");

        _articles.Remove(slug);
        _comments.Remove(slug);
        _favorites.RemoveWhere(f => f.Slug == slug);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private HttpResponseMessage HandleFavorite(string slug)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_articles.TryGetValue(slug, out var article))
            return NotFound($"Article '{slug}' not found");

        _favorites.Add((_currentUsername, slug));

        return Json(new { article = ArticleToDto(article, _currentUsername) });
    }

    private HttpResponseMessage HandleUnfavorite(string slug)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_articles.TryGetValue(slug, out var article))
            return NotFound($"Article '{slug}' not found");

        _favorites.Remove((_currentUsername, slug));

        return Json(new { article = ArticleToDto(article, _currentUsername) });
    }

    private HttpResponseMessage HandleGetComments(string slug)
    {
        if (!_comments.TryGetValue(slug, out var comments))
            return NotFound($"Article '{slug}' not found");

        return Json(new
        {
            comments = comments.Select(c => CommentToDto(c)).ToList()
        });
    }

    private HttpResponseMessage HandleAddComment(string slug, string? body)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_articles.ContainsKey(slug))
            return NotFound($"Article '{slug}' not found");

        var request = Deserialize<AddCommentRequest>(body);
        var comment = AddComment(slug, _currentUsername, request.Comment.Body);

        return Json(new { comment = CommentToDto(comment) });
    }

    private HttpResponseMessage HandleDeleteComment(string slug, int commentId)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_comments.TryGetValue(slug, out var comments))
            return NotFound($"Article '{slug}' not found");

        var comment = comments.FirstOrDefault(c => c.Id == commentId);
        if (comment is null)
            return NotFound($"Comment '{commentId}' not found");

        if (comment.AuthorUsername != _currentUsername)
            return Forbidden("You can only delete your own comments");

        comments.Remove(comment);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private HttpResponseMessage HandleGetProfile(string username)
    {
        if (!_users.TryGetValue(username, out var user))
            return NotFound($"User '{username}' not found");

        return Json(new { profile = ProfileToDto(user, _currentUsername) });
    }

    private HttpResponseMessage HandleFollow(string username)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_users.TryGetValue(username, out var user))
            return NotFound($"User '{username}' not found");

        _follows.Add((_currentUsername, username));

        return Json(new { profile = ProfileToDto(user, _currentUsername) });
    }

    private HttpResponseMessage HandleUnfollow(string username)
    {
        if (_currentUsername is null)
            return Unauthorized();

        if (!_users.TryGetValue(username, out var user))
            return NotFound($"User '{username}' not found");

        _follows.Remove((_currentUsername, username));

        return Json(new { profile = ProfileToDto(user, _currentUsername) });
    }

    private HttpResponseMessage HandleGetTags()
    {
        var tags = _articles.Values.SelectMany(a => a.TagList).Distinct().ToList();
        return Json(new { tags });
    }

    #endregion

    #region DTO Converters

    private object UserToDto(FakeUser user) => new
    {
        email = user.Email,
        token = user.Token,
        username = user.Username,
        bio = user.Bio,
        image = user.Image
    };

    private object ArticleToDto(FakeArticle article, string? viewerUsername) => new
    {
        slug = article.Slug,
        title = article.Title,
        description = article.Description,
        body = article.Body,
        tagList = article.TagList,
        createdAt = article.CreatedAt,
        updatedAt = article.UpdatedAt,
        favorited = viewerUsername is not null && _favorites.Contains((viewerUsername, article.Slug)),
        favoritesCount = _favorites.Count(f => f.Slug == article.Slug),
        author = ProfileToDto(_users[article.AuthorUsername], viewerUsername)
    };

    private object ProfileToDto(FakeUser user, string? viewerUsername) => new
    {
        username = user.Username,
        bio = user.Bio,
        image = user.Image,
        following = viewerUsername is not null && _follows.Contains((viewerUsername, user.Username))
    };

    private object CommentToDto(FakeComment comment) => new
    {
        id = comment.Id,
        createdAt = comment.CreatedAt,
        updatedAt = comment.CreatedAt,
        body = comment.Body,
        author = ProfileToDto(_users[comment.AuthorUsername], _currentUsername)
    };

    #endregion

    #region Helpers

    private T Deserialize<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            throw new InvalidOperationException("Request body is required");
        return JsonSerializer.Deserialize<T>(json, _jsonOptions) 
            ?? throw new InvalidOperationException("Failed to deserialize request");
    }

    private HttpResponseMessage Json(object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private HttpResponseMessage NotFound(string message) =>
        new(HttpStatusCode.NotFound)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message }, _jsonOptions), Encoding.UTF8, "application/json")
        };

    private HttpResponseMessage Unauthorized() =>
        new(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"Unauthorized\"}", Encoding.UTF8, "application/json")
        };

    private HttpResponseMessage Forbidden(string message) =>
        new(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message }, _jsonOptions), Encoding.UTF8, "application/json")
        };

    private HttpResponseMessage ValidationError(string field, string message) =>
        new(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { errors = new Dictionary<string, string[]> { [field] = [message] } }, _jsonOptions),
                Encoding.UTF8, "application/json")
        };

    private HttpResponseMessage Error(string message) =>
        new(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message }, _jsonOptions), Encoding.UTF8, "application/json")
        };

    private static string ExtractSlug(string path) =>
        path.Replace("/api/articles/", "").Split('/')[0];

    private static string ExtractSlugFromFavorite(string path) =>
        Regex.Match(path, @"/api/articles/([^/]+)/favorite").Groups[1].Value;

    private static string ExtractSlugFromComments(string path) =>
        Regex.Match(path, @"/api/articles/([^/]+)/comments").Groups[1].Value;

    private static int ExtractCommentId(string path) =>
        int.Parse(Regex.Match(path, @"/comments/(\d+)").Groups[1].Value);

    private static string ExtractUsername(string path) =>
        path.Replace("/api/profiles/", "").Split('/')[0];

    private static string ExtractUsernameFromFollow(string path) =>
        Regex.Match(path, @"/api/profiles/([^/]+)/follow").Groups[1].Value;

    private static string Slugify(string title) =>
        Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    #endregion

    #region Request DTOs

    private record LoginRequest(LoginUserDto User);
    private record LoginUserDto(string Email, string Password);

    private record RegisterRequest(RegisterUserDto User);
    private record RegisterUserDto(string Username, string Email, string Password);

    private record CreateArticleRequest(CreateArticleDto Article);
    private record CreateArticleDto(string Title, string Description, string Body, List<string>? TagList);

    private record AddCommentRequest(AddCommentDto Comment);
    private record AddCommentDto(string Body);

    #endregion
}

#region Fake Domain Models

public record FakeUser(string Username, string Email, string Password, string Token, string Bio, string Image);

public record FakeArticle(
    string Slug,
    string Title,
    string Description,
    string Body,
    List<string> TagList,
    string CreatedAt,
    string UpdatedAt,
    string AuthorUsername);

public record FakeComment(int Id, string Body, string AuthorUsername, string CreatedAt);

#endregion
