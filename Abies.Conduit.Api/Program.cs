using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Abies.Conduit.ServiceDefaults;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.AddServiceDefaults();

var app = builder.Build();
// Configure middleware
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");
// Strongly permissive CORS for local E2E/browser-wasm
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
    var reqHeaders = ctx.Request.Headers.ContainsKey("Access-Control-Request-Headers")
        ? ctx.Request.Headers["Access-Control-Request-Headers"].ToString()
        : "Content-Type, Authorization";
    ctx.Response.Headers["Access-Control-Allow-Headers"] = reqHeaders;
    if (string.Equals(ctx.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }
    await next();
});

// Health check endpoint
app.MapGet("/api/ping", () => Results.Json(new { pong = true }));

app.MapDefaultEndpoints();

// RealWorld Conduit API in-memory stores
var users = new ConcurrentDictionary<string, UserRecord>(StringComparer.OrdinalIgnoreCase);
var follows = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
var articles = new ConcurrentDictionary<string, ArticleRecord>(StringComparer.OrdinalIgnoreCase);
int nextCommentId = 1;
// Helper to get current user from Authorization header: 'Token {email}'
UserRecord? GetCurrentUser(HttpContext ctx)
{
    var auth = ctx.Request.Headers["Authorization"].FirstOrDefault();
    if (auth?.StartsWith("Token ") == true)
    {
        var email = auth[6..].Trim();
        users.TryGetValue(email, out var user);
        return user;
    }
    return null;
}

// Register new user
app.MapPost("/api/users", (RegisterRequest request) =>
{
    var u = request.User;
    
    // Validate required fields
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(u.Email))
        errors["email"] = new[] { "can't be empty" };
    else if (!u.Email.Contains("@"))
        errors["email"] = new[] { "is invalid" };
    
    if (string.IsNullOrWhiteSpace(u.Username))
        errors["username"] = new[] { "can't be empty" };
    
    if (string.IsNullOrWhiteSpace(u.Password))
        errors["password"] = new[] { "can't be empty" };
    else if (u.Password.Length < 8)
        errors["password"] = new[] { "is too short (minimum is 8 characters)" };
    
    if (errors.Count > 0)
        return Results.UnprocessableEntity(new { errors });
    
    // Check if email is already taken
    if (users.Values.Any(x => x.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase)))
        return Results.UnprocessableEntity(new { errors = new { email = new[] { "has already been taken" } } });
    
    // Check if username is already taken
    if (users.Values.Any(x => x.Username.Equals(u.Username, StringComparison.OrdinalIgnoreCase)))
        return Results.UnprocessableEntity(new { errors = new { username = new[] { "has already been taken" } } });
    
    var user = new UserRecord(u.Username, u.Email, u.Password, null, null);
    users[u.Email] = user;
    follows[user.Username] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    return Results.Created($"/api/users/{user.Username}", new { user = new UserResponse(user) });
});

// User login
app.MapPost("/api/users/login", (LoginRequest request) =>
{
    var u = request.User;
    
    // Validate required fields
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(u.Email))
        errors["email"] = new[] { "can't be empty" };
    
    if (string.IsNullOrWhiteSpace(u.Password))
        errors["password"] = new[] { "can't be empty" };
    
    if (errors.Count > 0)
        return Results.UnprocessableEntity(new { errors });
      // Find user by email
    var user = users.Values.FirstOrDefault(x => x.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase));
    if (user == null || user.Password != u.Password)
    {
        var errorDict = new Dictionary<string, string[]> {
            ["email or password"] = new[] { "is invalid" }
        };
        return Results.UnprocessableEntity(new { errors = errorDict });
    }
    
    return Results.Ok(new { user = new UserResponse(user) });
});

// Get current user
app.MapGet("/api/user", (HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { user = new UserResponse(user) });
});

// Update current user
app.MapPut("/api/user", (UpdateUserRequest request, HttpContext ctx) =>
{
    var currentUser = GetCurrentUser(ctx);
    if (currentUser is null) return Results.Unauthorized();
    
    var u = request.User;
    // If email changes, need to update the dictionary key
    if (!string.IsNullOrEmpty(u.Email) && u.Email != currentUser.Email)
    {
        if (users.ContainsKey(u.Email)) 
            return Results.UnprocessableEntity(new { errors = new { email = new[] { "already taken" } } });
        
        users.TryRemove(currentUser.Email, out _);
        currentUser = currentUser with { Email = u.Email };
    }
    
    // Update other fields if provided
    if (!string.IsNullOrEmpty(u.Username) && u.Username != currentUser.Username)
    {
        // Update follows references if username changes
        var oldUsername = currentUser.Username;
        if (follows.TryGetValue(oldUsername, out var followers))
        {
            follows[u.Username] = followers;
            follows.TryRemove(oldUsername, out _);
        }
        
        // Update author references in articles
        foreach (var article in articles.Values.Where(a => a.Author == oldUsername))
        {
            articles[article.Slug] = article with { Author = u.Username };
        }
        
        currentUser = currentUser with { Username = u.Username };
    }
    
    if (!string.IsNullOrEmpty(u.Password))
    {
        currentUser = currentUser with { Password = u.Password };
    }
    
    if (u.Bio != null) // Allow empty string to clear the bio
    {
        currentUser = currentUser with { Bio = u.Bio };
    }
    
    if (u.Image != null) // Allow empty string to clear the image
    {
        currentUser = currentUser with { Image = u.Image };
    }
    
    users[currentUser.Email] = currentUser;
    
    return Results.Ok(new { user = new UserResponse(currentUser) });
});

// Profiles
app.MapGet("/api/profiles/{username}", (string username, HttpContext ctx) =>
{
    var user = users.Values.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    if (user == null) return Results.NotFound();
    var current = GetCurrentUser(ctx);
    var following = current != null && follows[username].Contains(current.Username);
    return Results.Ok(new { profile = new ProfileResponse(user, following) });
});
app.MapPost("/api/profiles/{username}/follow", (string username, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current == null) return Results.Unauthorized();
    if (!follows.ContainsKey(username)) return Results.NotFound();
    follows[username].Add(current.Username);
    var user = users.Values.First(u => u.Username == username);
    return Results.Ok(new { profile = new ProfileResponse(user, true) });
});
app.MapDelete("/api/profiles/{username}/follow", (string username, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current == null) return Results.Unauthorized();
    if (!follows.ContainsKey(username)) return Results.NotFound();
    follows[username].Remove(current.Username);
    var user = users.Values.First(u => u.Username == username);
    return Results.Ok(new { profile = new ProfileResponse(user, false) });
});

// Articles listing
app.MapGet("/api/articles", (int? limit, int? offset, string? tag, string? author, string? favorited, HttpContext ctx) =>
{
    var query = articles.Values.AsEnumerable();
    if (!string.IsNullOrEmpty(tag)) query = query.Where(a => a.TagList.Contains(tag));
    if (!string.IsNullOrEmpty(author)) query = query.Where(a => a.Author == author);
    if (!string.IsNullOrEmpty(favorited)) query = query.Where(a => a.FavoritedBy.Contains(favorited));
    // Order by most recent first (RealWorld spec)
    query = query.OrderByDescending(a => a.CreatedAt);
    var total = query.Count();
    if (offset.HasValue) query = query.Skip(offset.Value);
    if (limit.HasValue) query = query.Take(limit.Value);
    var current = GetCurrentUser(ctx)?.Username;
    var list = query.Select(a => new ArticleResponse(
        a.Slug,
        a.Title,
        a.Description,
        a.Body,
        a.TagList,
        a.CreatedAt.ToString("o"),
        a.UpdatedAt.ToString("o"),
        current != null && a.FavoritedBy.Contains(current),
        a.FavoritedBy.Count,
        new ProfileResponse(users.Values.First(u => u.Username == a.Author), current != null && follows[a.Author].Contains(current))
    ));
    return Results.Ok(new { articles = list, articlesCount = total });
});
// Feed (articles by followed users)
app.MapGet("/api/articles/feed", (int? limit, int? offset, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx)?.Username;
    if (current == null) return Results.Unauthorized();
    // Compute the set of authors the current user follows
    var authorsFollowedByCurrent = follows
        .Where(kvp => kvp.Value.Contains(current))
        .Select(kvp => kvp.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    IEnumerable<ArticleRecord> query = articles.Values
        .Where(a => authorsFollowedByCurrent.Contains(a.Author))
        .OrderByDescending(a => a.CreatedAt);

    var total = query.Count();
    if (offset.HasValue) query = query.Skip(offset.Value);
    if (limit.HasValue) query = query.Take(limit.Value);

    var list = query.Select(a => new ArticleResponse(
        a.Slug,
        a.Title,
        a.Description,
        a.Body,
        a.TagList,
        a.CreatedAt.ToString("o"),
        a.UpdatedAt.ToString("o"),
        current != null && a.FavoritedBy.Contains(current),
        a.FavoritedBy.Count,
        new ProfileResponse(users.Values.First(u => u.Username == a.Author), current != null && follows[a.Author].Contains(current))
    ));
    return Results.Ok(new { articles = list, articlesCount = total });
});
// Get single article
app.MapGet("/api/articles/{slug}", (string slug, HttpContext ctx) =>
{
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    var current = GetCurrentUser(ctx)?.Username;
    var authorProfile = new ProfileResponse(users.Values.First(u => u.Username == a.Author), current != null && follows[a.Author].Contains(current));
    return Results.Ok(new { article = new ArticleResponse(
        a.Slug,
        a.Title,
        a.Description,
        a.Body,
        a.TagList,
        a.CreatedAt.ToString("o"),
        a.UpdatedAt.ToString("o"),
        current != null && a.FavoritedBy.Contains(current),
        a.FavoritedBy.Count,
        authorProfile
    ) });
});
// Create article
app.MapPost("/api/articles", (CreateArticleRequest req, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current == null) return Results.Unauthorized();
    var d = req.Article;
    
    // Validation
    if (string.IsNullOrWhiteSpace(d.Title)) 
        return Results.UnprocessableEntity(new { errors = new { title = new[] { "can't be empty" } } });
    if (string.IsNullOrWhiteSpace(d.Description)) 
        return Results.UnprocessableEntity(new { errors = new { description = new[] { "can't be empty" } } });
    if (string.IsNullOrWhiteSpace(d.Body)) 
        return Results.UnprocessableEntity(new { errors = new { body = new[] { "can't be empty" } } });
    
    // Generate slug from title - replace spaces with dashes, remove special chars, ensure lowercase
    var slug = d.Title.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace(".", "")
        .Replace(",", "")
        .Replace("!", "")
        .Replace("?", "")
        .Replace(":", "")
        .Replace(";", "")
        .Replace("@", "")
        .Replace("#", "")
        .Replace("$", "")
        .Replace("%", "")
        .Replace("&", "")
        .Replace("*", "")
        .Replace("(", "")
        .Replace(")", "")
        .Replace("+", "")
        .Replace("=", "")
        .Replace("'", "")
        .Replace("\"", "")
        .Replace("/", "")
        .Replace("\\", "");
    
    // Ensure slug uniqueness by appending a number if necessary
    var baseSlug = slug;
    var counter = 1;
    while (articles.ContainsKey(slug))
    {
        slug = $"{baseSlug}-{counter++}";
    }
    
    var now = DateTime.UtcNow;
    var tagList = d.TagList ?? new List<string>();
    var record = new ArticleRecord(slug, d.Title, d.Description, d.Body, tagList, now, now, current.Username);
    articles[slug] = record;
    var authorProfile = new ProfileResponse(current, false);
    return Results.Created($"/api/articles/{slug}", new { article = new ArticleResponse(
        record.Slug,
        record.Title,
        record.Description,
        record.Body,
        record.TagList,
        record.CreatedAt.ToString("o"),
        record.UpdatedAt.ToString("o"),
        false,
        0,
        authorProfile
    ) });
});
// Update article
app.MapPut("/api/articles/{slug}", (string slug, UpdateArticleRequest req, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current == null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    if (a.Author != current.Username) return Results.StatusCode(StatusCodes.Status403Forbidden);
    
    var u = req.Article;
    string newSlug = slug;
    
    // If title changed, generate a new slug
    if (!string.IsNullOrEmpty(u.Title) && u.Title != a.Title)
    {
        // Generate new slug from title - same logic as in create
        newSlug = u.Title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("&", "")
            .Replace("*", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("+", "")
            .Replace("=", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("/", "")
            .Replace("\\", "");
        
        // Ensure uniqueness but don't add a suffix if it matches the original slug
        if (newSlug != slug && articles.ContainsKey(newSlug))
        {
            var baseSlug = newSlug;
            var counter = 1;
            while (articles.ContainsKey(newSlug) && newSlug != slug)
            {
                newSlug = $"{baseSlug}-{counter++}";
            }
        }
    }
    
    // Create updated article with non-null values from request
    var updated = a with 
    { 
        Slug = newSlug,
        Title = u.Title ?? a.Title, 
        Description = u.Description ?? a.Description, 
        Body = u.Body ?? a.Body, 
        TagList = u.TagList ?? a.TagList, 
        UpdatedAt = DateTime.UtcNow 
    };
    
    // If slug changed, remove old entry and add new one
    if (newSlug != slug)
    {
        articles.TryRemove(slug, out _);
    }
    
    articles[newSlug] = updated;
    var authorProfile = new ProfileResponse(users.Values.First(u => u.Username == updated.Author), 
        follows[updated.Author].Contains(current.Username));
    
    return Results.Ok(new { article = new ArticleResponse(
        updated.Slug,
        updated.Title,
        updated.Description,
        updated.Body,
        updated.TagList,
        updated.CreatedAt.ToString("o"),
        updated.UpdatedAt.ToString("o"),
        updated.FavoritedBy.Contains(current.Username),
        updated.FavoritedBy.Count,
        authorProfile
    ) });
});
// Delete article
app.MapDelete("/api/articles/{slug}", (string slug, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current == null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    if (a.Author != current.Username) return Results.StatusCode(StatusCodes.Status403Forbidden);
    articles.TryRemove(slug, out _);
    return Results.NoContent();
});

// Comments
app.MapGet("/api/articles/{slug}/comments", (string slug, HttpContext ctx) =>
{
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    var current = GetCurrentUser(ctx)?.Username;
    var list = a.Comments.Select(c => new CommentResponse(
        c.Id,
        c.CreatedAt.ToString("o"),
        c.CreatedAt.ToString("o"),
        c.Body,
        new ProfileResponse(
            users.Values.First(u => u.Username == c.Author),
            current != null && follows[c.Author].Contains(current)
        )
    ));
    return Results.Ok(new { comments = list });
});
app.MapPost("/api/articles/{slug}/comments", (string slug, PostCommentRequest req, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user == null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    var c = new CommentRecord(nextCommentId++, req.Comment.Body, DateTime.UtcNow, user.Username);
    a.Comments.Add(c);
    var authorProfile = new ProfileResponse(user, false);
    return Results.Created($"/api/articles/{slug}/comments/{c.Id}", new { comment = new CommentResponse(
        c.Id,
        c.CreatedAt.ToString("o"),
        c.CreatedAt.ToString("o"),
        c.Body,
        authorProfile
    ) });
});
app.MapDelete("/api/articles/{slug}/comments/{id}", (string slug, int id, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user == null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    var c = a.Comments.FirstOrDefault(x => x.Id == id);
    if (c == null) return Results.NotFound();
    if (c.Author != user.Username) return Results.StatusCode(StatusCodes.Status403Forbidden);
    a.Comments.Remove(c);
    return Results.NoContent();
});

// Favorites
app.MapPost("/api/articles/{slug}/favorite", (string slug, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user == null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    a.FavoritedBy.Add(user.Username);
    return Results.Ok(new { article = new ArticleResponse(
        a.Slug,
        a.Title,
        a.Description,
        a.Body,
        a.TagList,
        a.CreatedAt.ToString("o"),
        a.UpdatedAt.ToString("o"),
        a.FavoritedBy.Contains(user.Username),
        a.FavoritedBy.Count,
        new ProfileResponse(users.Values.First(u => u.Username == a.Author), follows[a.Author].Contains(user.Username))
    ) });
});



app.MapDelete("/api/articles/{slug}/favorite", (string slug, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user == null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    a.FavoritedBy.Remove(user.Username);
    return Results.Ok(new { article = new ArticleResponse(
        a.Slug,
        a.Title,
        a.Description,
        a.Body,
        a.TagList,
        a.CreatedAt.ToString("o"),
        a.UpdatedAt.ToString("o"),
        a.FavoritedBy.Contains(user.Username),
        a.FavoritedBy.Count,
        new ProfileResponse(users.Values.First(u => u.Username == a.Author), follows[a.Author].Contains(user.Username))
    ) });
});

// Tags
app.MapGet("/api/tags", () => 
{
    // Extract all unique tags from all articles
    var allTags = articles.Values
        .SelectMany(a => a.TagList)
        .Where(tag => !string.IsNullOrWhiteSpace(tag))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(t => t)
        .ToArray();
    
    return Results.Json(new { tags = allTags });
});

// Start the API
app.Run();

// Request/Response DTOs for stub endpoints
public record CreateArticleRequest(ArticleData Article);
public record UpdateArticleRequest(ArticleData Article);
public record ArticleData(string Title, string Description, string Body, List<string> TagList);
public record PostCommentRequest(CommentData Comment);
public record CommentData(string Body);

// Data models
public record RegisterRequest(RegisterUser User);
public record LoginRequest(LoginUser User);
public record UpdateUserRequest(UpdateUser User);
public record RegisterUser(string Username, string Email, string Password);
public record LoginUser(string Email, string Password);
public record UpdateUser(string? Email, string? Username, string? Password, string? Bio, string? Image);
// User storage with optional profile fields
public record UserRecord(string Username, string Email, string Password, string? Bio = null, string? Image = null);
public record UserDto(string Username, string Email);

// Article and Comment internal storage types
public record ArticleRecord(
    string Slug,
    string Title,
    string Description,
    string Body,
    List<string> TagList,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Author
)
{
    public HashSet<string> FavoritedBy { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CommentRecord> Comments { get; } = new();
}
public record CommentRecord(int Id, string Body, DateTime CreatedAt, string Author);

// Response DTOs
public record UserResponse(string Username, string Email, string Bio, string Image, string Token)
{
    public UserResponse(UserRecord u)
        : this(u.Username, u.Email, u.Bio ?? string.Empty, u.Image ?? string.Empty, u.Email) { }
}
public record ProfileResponse(string Username, string Bio, string Image, bool Following)
{
    public ProfileResponse(UserRecord u, bool following)
        : this(u.Username, u.Bio ?? string.Empty, u.Image ?? string.Empty, following) { }
}
public record ArticleResponse(
    string Slug,
    string Title,
    string Description,
    string Body,
    List<string> TagList,
    string CreatedAt,
    string UpdatedAt,
    bool Favorited,
    int FavoritesCount,
    ProfileResponse Author
);
public record CommentResponse(
    int Id,
    string CreatedAt,
    string UpdatedAt,
    string Body,
    ProfileResponse Author
);
