// =============================================================================
// Program.cs — Composition Root for the Conduit API
// =============================================================================
// Wires together:
//   - Aspire ServiceDefaults (OpenTelemetry, health checks, service discovery)
//   - KurrentDB event stores (User + Article)
//   - PostgreSQL data source + schema migration
//   - JWT Token authentication ("Token" scheme)
//   - Query capability delegates (factory pattern)
//   - AggregateStore (command handling + projection)
//   - Endpoint groups (Users, User, Profile, Article, Comment, Tag)
//
// Configuration:
//   - Jwt:Secret — HMAC-SHA256 signing secret (min 32 chars)
//   - Jwt:Issuer — JWT issuer claim (default: "conduit")
//   - ConnectionStrings:kurrentdb — KurrentDB connection string
//   - ConnectionStrings:conduitdb — PostgreSQL connection string
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using KurrentDB.Client;
using Npgsql;
using Picea.Abies.Conduit.Api.Authentication;
using Picea.Abies.Conduit.Api.Endpoints;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadStore.PostgreSQL;
using Picea.Abies.Conduit.ServiceDefaults;
using Picea.Glauca;
using Picea.Glauca.KurrentDB;

var builder = WebApplication.CreateBuilder(args);

// ─── Aspire ServiceDefaults ────────────────────────────────────────────────
builder.AddServiceDefaults();

// ─── JSON Serialization ────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ─── KurrentDB ─────────────────────────────────────────────────────────────
var kurrentDbConnectionString = builder.Configuration.GetConnectionString("kurrentdb")
    ?? "esdb://localhost:2113?tls=false";

var kurrentDbSettings = KurrentDBClientSettings.Create(kurrentDbConnectionString);
var kurrentDbClient = new KurrentDBClient(kurrentDbSettings);

var userEventStore = new KurrentDBEventStore<UserEvent>(
    kurrentDbClient,
    EventSerialization.SerializeUserEvent,
    EventSerialization.DeserializeUserEvent);

var articleEventStore = new KurrentDBEventStore<ArticleEvent>(
    kurrentDbClient,
    EventSerialization.SerializeArticleEvent,
    EventSerialization.DeserializeArticleEvent);

builder.Services.AddSingleton<EventStore<UserEvent>>(userEventStore);
builder.Services.AddSingleton<EventStore<ArticleEvent>>(articleEventStore);

// ─── PostgreSQL ────────────────────────────────────────────────────────────
var postgresConnectionString = builder.Configuration.GetConnectionString("conduitdb")
    ?? "Host=localhost;Database=conduit;Username=postgres;Password=postgres";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

// ─── JWT Token Service ─────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (builder.Environment.IsDevelopment())
    {
        // In development we fall back to a well-known secret to simplify local setup.
        // Non-development environments must explicitly configure Jwt:Secret.
        jwtSecret = "this-is-a-development-secret-key-that-is-at-least-32-characters";
    }
    else
    {
        throw new InvalidOperationException("Configuration value 'Jwt:Secret' is required in non-development environments.");
    }
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "conduit";

var jwtTokenService = new JwtTokenService(jwtSecret, jwtIssuer);
builder.Services.AddSingleton(jwtTokenService);

// ─── Authentication ("Token" scheme) ───────────────────────────────────────
builder.Services
    .AddAuthentication("Token")
    .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>("Token", _ => { });

builder.Services.AddAuthorization();

// ─── Aggregate Store (command handling + projection) ───────────────────────
builder.Services.AddSingleton(sp =>
    new AggregateStore(
        sp.GetRequiredService<EventStore<UserEvent>>(),
        sp.GetRequiredService<EventStore<ArticleEvent>>(),
        sp.GetRequiredService<NpgsqlDataSource>()));

// ─── Query Capability Delegates (factory pattern) ──────────────────────────
builder.Services.AddSingleton(sp =>
    QueryStore.CreateFindUserByEmail(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateFindUserById(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateFindUserByUsername(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateGetProfile(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateListArticles(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateGetFeed(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateFindArticleBySlug(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateFindArticleIdBySlug(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateGetComments(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton(sp =>
    QueryStore.CreateGetTags(sp.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddSingleton<RequestIdempotencyStore>();

// ─── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Schema Migration ──────────────────────────────────────────────────────
// Only run schema migration in development — test and production environments
// manage their own database lifecycle.
if (app.Environment.IsDevelopment())
{
    var pgDataSource = app.Services.GetService<NpgsqlDataSource>();
    if (pgDataSource is not null)
    {
        await Schema.EnsureCreated(pgDataSource).ConfigureAwait(false);
    }
}

// ─── Middleware ─────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.TryAdd("X-Content-Type-Options", "nosniff");
    headers.TryAdd("X-Frame-Options", "DENY");
    headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    headers.TryAdd("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");

    await next().ConfigureAwait(false);
});

app.UseAuthentication();
app.UseAuthorization();

// ─── Aspire Health/Alive Endpoints ─────────────────────────────────────────
app.MapDefaultEndpoints();

// ─── API Endpoints ─────────────────────────────────────────────────────────
app.MapUsersEndpoints();
app.MapUserEndpoints();
app.MapProfileEndpoints();
app.MapArticleEndpoints();
app.MapCommentEndpoints();
app.MapTagEndpoints();

app.Run();

// Expose for WebApplicationFactory<Program> in integration tests
/// <summary>Marker class for integration test hosting.</summary>
public partial class Program;
