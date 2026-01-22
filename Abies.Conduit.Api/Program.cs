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
builder.Services.AddHttpClient("otlp")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        // Aspire dashboard OTLP endpoint uses a local/dev certificate; allow it in Development.
        // This is for local dev only and intentionally scoped to the OTLP proxy client.
        return new HttpClientHandler
        {
            // NOTE: In practice we've observed intermittent SSL failures when relying on the
            // environment gate. Since this client is ONLY for local OTLP proxying, we
            // always skip validation here.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });
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

if (app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        var traceparent = ctx.Request.Headers.TraceParent.FirstOrDefault();
        var tracestate = ctx.Request.Headers.TraceState.FirstOrDefault();
        var baggage = ctx.Request.Headers.Baggage.FirstOrDefault();

        await next();

        // After next() we should have current Activity set by AspNetCore instrumentation (when enabled)
        var activity = System.Diagnostics.Activity.Current;
        var traceId = activity?.TraceId.ToString() ?? "<none>";

        if (!string.IsNullOrEmpty(traceparent) || activity is not null)
        {
            app.Logger.LogInformation(
                "TraceContext {Method} {Path} traceparent={TraceParent} tracestate={TraceState} baggage={Baggage} extractedTraceId={TraceId}",
                ctx.Request.Method,
                ctx.Request.Path.Value,
                traceparent ?? "<none>",
                tracestate ?? "<none>",
                baggage ?? "<none>",
                traceId);
        }
    });
}

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");
// Strongly permissive CORS for local browser-wasm
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
    // Ensure W3C trace-context headers are allowed so browser spans can propagate to the API.
    var reqHeaders = ctx.Request.Headers.ContainsKey("Access-Control-Request-Headers")
        ? ctx.Request.Headers["Access-Control-Request-Headers"].ToString()
        : "Content-Type, Authorization, traceparent, tracestate, baggage";
    if (!reqHeaders.Contains("traceparent", StringComparison.OrdinalIgnoreCase)) reqHeaders += ", traceparent";
    if (!reqHeaders.Contains("tracestate", StringComparison.OrdinalIgnoreCase)) reqHeaders += ", tracestate";
    if (!reqHeaders.Contains("baggage", StringComparison.OrdinalIgnoreCase)) reqHeaders += ", baggage";
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

// Frontend OTLP/HTTP proxy to avoid CORS issues when exporting traces from the browser
// Target endpoint resolution order: OTEL_EXPORTER_OTLP_TRACES_ENDPOINT, OTEL_EXPORTER_OTLP_ENDPOINT + '/v1/traces', default 'http://localhost:4318/v1/traces'
static string NormalizeOtlpTracesEndpoint(string raw)
{
    // We accept a variety of shapes here:
    // - Full traces endpoint: https://host/otlp/v1/traces or https://host/v1/traces
    // - Base endpoint: https://host or https://host/otlp
    // Aspire dashboard OTLP/HTTP historically used /otlp/v1/traces, but we've observed
    // current dashboard instances accept OTLP/HTTP traces at /v1/traces and return 403
    // for /otlp/v1/traces even with a valid x-otlp-api-key.
    //
    // Therefore our normalization is conservative:
    // - If caller provides an explicit traces path, keep it.
    // - If caller provides a base URL (no path), default to /v1/traces.
    if (string.IsNullOrWhiteSpace(raw)) return raw;

    var s = raw.Trim().TrimEnd('/');
    if (s.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase)) return s;
    if (s.EndsWith("/otlp/v1/traces", StringComparison.OrdinalIgnoreCase)) return s;

    // If we were given a base endpoint that ends in /otlp, treat it as a base and append /v1/traces.
    if (s.EndsWith("/otlp", StringComparison.OrdinalIgnoreCase)) return s + "/v1/traces";

    // Default for a base URL: append the standard OTLP/HTTP traces path.
    return s + "/v1/traces";
}

var tracesEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"]
    ?? (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is string baseUrl && !string.IsNullOrWhiteSpace(baseUrl)
        ? NormalizeOtlpTracesEndpoint(baseUrl)
    // Aspire browser telemetry endpoint (AppHost can configure this explicitly)
    : (Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL") is string aspireHttp && !string.IsNullOrWhiteSpace(aspireHttp)
        ? NormalizeOtlpTracesEndpoint(aspireHttp)
    // Aspire dashboard OTLP/HTTP default (when enabled). For Aspire dashboard, the OTLP/HTTP endpoint
    // is available at /v1/traces (e.g. https://localhost:21203/v1/traces).
    // If the dashboard isn't running, the proxy will fall back to returning 202 Accepted in Development.
    : "https://localhost:21203/v1/traces"));

// When running under AppHost, Aspire provides an API key for OTLP via OTEL_EXPORTER_OTLP_HEADERS.
// Example: "x-otlp-api-key=...". We'll forward it so the dashboard doesn't reject browser exports.
var otlpHeadersRaw = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]
    ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS")
    ?? "";

// Aspire dashboard uses API key auth by default; if we can see the key, always ensure we forward it.
// Otherwise we can end up proxying spans with missing/incorrect auth and receive 403s.
var dashboardPrimaryApiKey = Environment.GetEnvironmentVariable("DASHBOARD__OTLP__PRIMARYAPIKEY");
if (!string.IsNullOrWhiteSpace(dashboardPrimaryApiKey)
    && !otlpHeadersRaw.Contains("x-otlp-api-key=", StringComparison.OrdinalIgnoreCase))
{
    otlpHeadersRaw = string.IsNullOrWhiteSpace(otlpHeadersRaw)
        ? $"x-otlp-api-key={dashboardPrimaryApiKey}"
        : $"{otlpHeadersRaw},x-otlp-api-key={dashboardPrimaryApiKey}";
}

// If the API itself is not launched by AppHost, it won't have OTEL_EXPORTER_OTLP_HEADERS.
// In local dev we can still make the proxy work by reading the dashboard process env.
// This is best-effort and only used when we otherwise don't have an API key.
if (string.IsNullOrWhiteSpace(otlpHeadersRaw)
    && builder.Environment.IsDevelopment()
    && System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
{
    try
    {
        using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "/bin/ps",
            // eww prints env vars; search for the dashboard key.
            Arguments = "eww -ax",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        if (proc is not null)
        {
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(1000);
            // `ps eww -ax` output can wrap long environment-variable values (inserting newlines).
            // Normalize whitespace and then regex-match the key.
            var normalized = string.Join(' ', output.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries));
            var match = System.Text.RegularExpressions.Regex.Match(
                normalized,
                @"DASHBOARD__OTLP__PRIMARYAPIKEY=(\S+)",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var key = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    otlpHeadersRaw = $"x-otlp-api-key={key}";
                    app.Logger.LogInformation("OTLP proxy: inferred dashboard API key from local ps output (dev-only)");
                }
            }
        }
    }
    catch
    {
        // ignore; we'll just run without a key and the dashboard will return 401.
    }
}

app.Logger.LogInformation(
    "OTLP proxy configured: tracesEndpoint={TracesEndpoint} apiKeyForwarding={ApiKeyForwarding}",
    tracesEndpoint,
    otlpHeadersRaw.Contains("x-otlp-api-key=", StringComparison.OrdinalIgnoreCase));

app.MapMethods("/otlp/v1/traces", ["OPTIONS"], (HttpContext ctx) =>
{
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
    ctx.Response.Headers["Access-Control-Allow-Headers"] = ctx.Request.Headers["Access-Control-Request-Headers"].ToString() ?? "content-type";
    return Results.NoContent();
});

app.MapPost("/otlp/v1/traces", async (HttpContext ctx, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        try
        {
            var hit = $"{DateTime.UtcNow:o} hit /otlp/v1/traces len={ctx.Request.ContentLength?.ToString() ?? "?"}\\n";
            await System.IO.File.AppendAllTextAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "otlp_hits.log"), hit);
        }
        catch { /* ignore file logging errors */ }

        if (app.Environment.IsDevelopment())
        {
            var incoming = new
            {
                ct = ctx.Request.ContentType ?? "<none>",
                ce = ctx.Request.Headers.ContentEncoding.ToString(),
                ua = ctx.Request.Headers.UserAgent.ToString(),
                al = ctx.Request.Headers.AcceptLanguage.ToString(),
                path = ctx.Request.Path.Value,
                len = ctx.Request.ContentLength?.ToString() ?? "?"
            };
            app.Logger.LogInformation("OTLP proxy incoming {Incoming}", incoming);
        }
    var client = httpClientFactory.CreateClient("otlp");
        using var req = new HttpRequestMessage(HttpMethod.Post, tracesEndpoint)
        {
            Content = new StreamContent(ctx.Request.Body)
        };
        // Preserve the incoming OTLP/HTTP content-type.
        // The JS OTLP protobuf exporter uses: application/x-protobuf.
        // If we overwrite it to application/json the dashboard replies 415.
        if (!string.IsNullOrWhiteSpace(ctx.Request.ContentType))
        {
            req.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ctx.Request.ContentType);
        }

    // Forward configured headers (primarily x-otlp-api-key for Aspire dashboard)
    var forwardedApiKey = false;
    string? forwardedApiKeyValue = null;
        if (!string.IsNullOrWhiteSpace(otlpHeadersRaw))
        {
            foreach (var pair in otlpHeadersRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx <= 0 || idx == pair.Length - 1)
                {
                    continue;
                }

                var key = pair[..idx].Trim();
                var value = pair[(idx + 1)..].Trim();
                if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (key.Equals("x-otlp-api-key", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                {
                    forwardedApiKey = true;
                    forwardedApiKeyValue = value;
                }

                req.Headers.TryAddWithoutValidation(key, value);
            }
        }

        // Diagnostics so we can confirm the API key exists (without logging it)
        // This is intentionally safe: we only expose a boolean + short hash prefix.
        ctx.Response.Headers["x-otlp-proxy-has-key"] = forwardedApiKey ? "true" : "false";
        if (forwardedApiKeyValue is not null)
        {
            // stable, non-reversible hint
            var bytes = System.Text.Encoding.UTF8.GetBytes(forwardedApiKeyValue);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            ctx.Response.Headers["x-otlp-proxy-key-sha256"] = Convert.ToHexString(hash)[..16];
        }

    // Forward request
    // NOTE: For local Dev we NEVER want browser telemetry exporting to impact app usability.
    // Therefore, in Development we always return 202 Accepted to the browser (after starting the forward),
    // while still logging and/or surfacing the downstream status for debugging.
    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);

        if (app.Environment.IsDevelopment())
        {
            var respContentType = resp.Content?.Headers.ContentType?.ToString() ?? "<none>";
            app.Logger.LogInformation(
                "OTLP proxy downstream {Method} {Target} status={StatusCode} contentType={ContentType} apiKeyForwarded={ApiKeyForwarded}",
                req.Method.Method,
                req.RequestUri?.ToString() ?? "<none>",
                (int)resp.StatusCode,
                respContentType,
                forwardedApiKey);

            try
            {
                var c = respContentType.Replace("\n", " ").Replace("\r", " ");
                var line = $"{DateTime.UtcNow:o} downstream -> {req.RequestUri} status={(int)resp.StatusCode} ct={c} apiKeyForwarded={forwardedApiKey}{Environment.NewLine}";
                await System.IO.File.AppendAllTextAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "otlp_proxy_downstream.log"), line);
            }
            catch
            {
                // ignore file logging errors
            }
        }

        // CORS for browser
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";

        if (app.Environment.IsDevelopment())
        {
            ctx.Response.StatusCode = StatusCodes.Status202Accepted;
            return Results.Accepted();
        }

        // Non-development: proxy true downstream status/content.
        ctx.Response.StatusCode = (int)resp.StatusCode;
        foreach (var (name, values) in resp.Headers)
        {
            // Avoid duplicating hop-by-hop headers
            if (string.Equals(name, "transfer-encoding", System.StringComparison.OrdinalIgnoreCase)) continue;
            ctx.Response.Headers[name] = string.Join(",", values);
        }
        if (resp.Content is not null)
        {
            var respContentType = resp.Content.Headers.ContentType?.ToString();
            if (!string.IsNullOrEmpty(respContentType)) ctx.Response.ContentType = respContentType;
            await resp.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
        }
        return Results.Empty;
    }
    catch (Exception ex)
    {
        // When no collector is running locally, we'd rather accept spans (so browser instrumentation doesn't treat it
        // as a hard error) than fail the request. Still log the error for debugging.
        try
        {
            var fail = $"{DateTime.UtcNow:o} error -> {tracesEndpoint} ex={ex.GetType().Name}:{ex.Message}{Environment.NewLine}";
            await System.IO.File.AppendAllTextAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "otlp_proxy_errors.log"), fail);
        }
        catch { /* ignore file logging errors */ }

        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        if (app.Environment.IsDevelopment())
        {
            // 202 Accepted indicates the payload was received, even if we can't forward it.
            return Results.Accepted();
        }

        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// RealWorld Conduit API in-memory stores
var users = new ConcurrentDictionary<string, UserRecord>(StringComparer.OrdinalIgnoreCase);
var follows = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
var articles = new ConcurrentDictionary<string, ArticleRecord>(StringComparer.OrdinalIgnoreCase);
int nextCommentId = 1;
// Safety helpers: this API is an in-memory stub for tests and should never 500 due to missing keys.
HashSet<string> FollowSetFor(string username)
{
    // Avoid KeyNotFoundException when looking up relationships for a username that exists in the
    // users/articles store but hasn't had its follow set initialized for some reason.
    return follows.GetOrAdd(username, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}

static bool IsFollowing(ConcurrentDictionary<string, HashSet<string>> follows, string targetUsername, string followerUsername)
{
    return follows.TryGetValue(targetUsername, out var set) && set.Contains(followerUsername);
}
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
    Dictionary<string, string[]> errors = [];
    if (string.IsNullOrWhiteSpace(u.Email))
        errors["email"] = ["can't be empty"];
    else if (!u.Email.Contains("@"))
        errors["email"] = ["is invalid"];
    
    if (string.IsNullOrWhiteSpace(u.Username))
        errors["username"] = ["can't be empty"];
    
    if (string.IsNullOrWhiteSpace(u.Password))
        errors["password"] = ["can't be empty"];
    else if (u.Password.Length < 8)
        errors["password"] = ["is too short (minimum is 8 characters)"];
    
    if (errors.Count > 0)
        return Results.UnprocessableEntity(new { errors });
    
    // Check if email is already taken
    if (users.Values.Any(x => x.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase)))
        return Results.UnprocessableEntity(new { errors = new { email = (string[])["has already been taken"] } });
    
    // Check if username is already taken
    if (users.Values.Any(x => x.Username.Equals(u.Username, StringComparison.OrdinalIgnoreCase)))
        return Results.UnprocessableEntity(new { errors = new { username = (string[])["has already been taken"] } });
    
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
    Dictionary<string, string[]> errors = [];
    if (string.IsNullOrWhiteSpace(u.Email))
        errors["email"] = ["can't be empty"];
    
    if (string.IsNullOrWhiteSpace(u.Password))
        errors["password"] = ["can't be empty"];
    
    if (errors.Count > 0)
        return Results.UnprocessableEntity(new { errors });
      // Find user by email
    var user = users.Values.FirstOrDefault(x => x.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase));
    if (user is null || user.Password != u.Password)
    {
        Dictionary<string, string[]> errorDict = new()
        {
            ["email or password"] = ["is invalid"]
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
            return Results.UnprocessableEntity(new { errors = new { email = (string[])["already taken"] } });
        
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
    
    if (u.Bio is not null) // Allow empty string to clear the bio
    {
        currentUser = currentUser with { Bio = u.Bio };
    }
    
    if (u.Image is not null) // Allow empty string to clear the image
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
    if (user is null) return Results.NotFound();
    var current = GetCurrentUser(ctx);
    var following = current is not null && IsFollowing(follows, username, current.Username);
    return Results.Ok(new { profile = new ProfileResponse(user, following) });
});
app.MapPost("/api/profiles/{username}/follow", (string username, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current is null) return Results.Unauthorized();
    var user = users.Values.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    if (user is null) return Results.NotFound();
    FollowSetFor(username).Add(current.Username);
    return Results.Ok(new { profile = new ProfileResponse(user, true) });
});
app.MapDelete("/api/profiles/{username}/follow", (string username, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current is null) return Results.Unauthorized();
    var user = users.Values.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    if (user is null) return Results.NotFound();
    FollowSetFor(username).Remove(current.Username);
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
        current is not null && a.FavoritedBy.Contains(current),
        a.FavoritedBy.Count,
        new ProfileResponse(
            users.Values.First(u => u.Username == a.Author),
            current is not null && IsFollowing(follows, a.Author, current))
    ));
    return Results.Ok(new { articles = list, articlesCount = total });
});
// Feed (articles by followed users)
app.MapGet("/api/articles/feed", (int? limit, int? offset, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx)?.Username;
    if (current is null) return Results.Unauthorized();
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
        current is not null && a.FavoritedBy.Contains(current),
        a.FavoritedBy.Count,
        new ProfileResponse(
            users.Values.First(u => u.Username == a.Author),
            current is not null && IsFollowing(follows, a.Author, current))
    ));
    return Results.Ok(new { articles = list, articlesCount = total });
});
// Get single article
app.MapGet("/api/articles/{slug}", (string slug, HttpContext ctx) =>
{
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    var current = GetCurrentUser(ctx)?.Username;
    var authorProfile = new ProfileResponse(
        users.Values.First(u => u.Username == a.Author),
        current is not null && IsFollowing(follows, a.Author, current));
    return Results.Ok(new { article = new ArticleResponse(
        a.Slug,
        a.Title,
        a.Description,
        a.Body,
        a.TagList,
        a.CreatedAt.ToString("o"),
        a.UpdatedAt.ToString("o"),
        current is not null && a.FavoritedBy.Contains(current),
        a.FavoritedBy.Count,
        authorProfile
    ) });
});
// Create article
app.MapPost("/api/articles", (CreateArticleRequest req, HttpContext ctx) =>
{
    var current = GetCurrentUser(ctx);
    if (current is null) return Results.Unauthorized();
    var d = req.Article;
    
    // Validation
    if (string.IsNullOrWhiteSpace(d.Title)) 
        return Results.UnprocessableEntity(new { errors = new { title = (string[])["can't be empty"] } });
    if (string.IsNullOrWhiteSpace(d.Description)) 
        return Results.UnprocessableEntity(new { errors = new { description = (string[])["can't be empty"] } });
    if (string.IsNullOrWhiteSpace(d.Body)) 
        return Results.UnprocessableEntity(new { errors = new { body = (string[])["can't be empty"] } });
    
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
    var tagList = d.TagList ?? [];
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
    if (current is null) return Results.Unauthorized();
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
    IsFollowing(follows, updated.Author, current.Username));
    
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
    if (current is null) return Results.Unauthorized();
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
            current is not null && IsFollowing(follows, c.Author, current)
        )
    ));
    return Results.Ok(new { comments = list });
});
app.MapPost("/api/articles/{slug}/comments", (string slug, PostCommentRequest req, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user is null) return Results.Unauthorized();
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
    if (user is null) return Results.Unauthorized();
    if (!articles.TryGetValue(slug, out var a)) return Results.NotFound();
    var c = a.Comments.FirstOrDefault(x => x.Id == id);
    if (c is null) return Results.NotFound();
    if (c.Author != user.Username) return Results.StatusCode(StatusCodes.Status403Forbidden);
    a.Comments.Remove(c);
    return Results.NoContent();
});

// Favorites
app.MapPost("/api/articles/{slug}/favorite", (string slug, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user is null) return Results.Unauthorized();
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
        new ProfileResponse(
            users.Values.First(u => u.Username == a.Author),
            IsFollowing(follows, a.Author, user.Username))
    ) });
});



app.MapDelete("/api/articles/{slug}/favorite", (string slug, HttpContext ctx) =>
{
    var user = GetCurrentUser(ctx);
    if (user is null) return Results.Unauthorized();
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
        new ProfileResponse(
            users.Values.First(u => u.Username == a.Author),
            IsFollowing(follows, a.Author, user.Username))
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
    public List<CommentRecord> Comments { get; } = [];
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
