// =============================================================================
// ConduitApiFactory — WebApplicationFactory for Integration Tests
// =============================================================================
// Replaces external dependencies (KurrentDB, PostgreSQL) with in-memory fakes.
// This lets us test the full HTTP pipeline — routing, auth, serialization,
// validation, command handling — without containers.
//
// Architecture:
//   - InMemoryEventStore replaces KurrentDB
//   - In-memory query delegates replace PostgreSQL QueryStore
//   - Real JWT token service (for auth flow testing)
//   - Real AggregateStore (wired to in-memory event stores)
// =============================================================================

using System.Collections.Concurrent;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadModel;
using Picea;
using Picea.Glauca.EventSourcing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Picea.Abies.Conduit.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces external dependencies with in-memory fakes.
/// </summary>
public sealed class ConduitApiFactory : WebApplicationFactory<Program>
{
    /// <summary>In-memory store for user events.</summary>
    public InMemoryEventStore<UserEvent> UserEventStore { get; } = new();

    /// <summary>In-memory store for article events.</summary>
    public InMemoryEventStore<ArticleEvent> ArticleEventStore { get; } = new();

    /// <summary>In-memory user store backing the query delegates.</summary>
    public ConcurrentDictionary<Guid, UserReadModel> Users { get; } = new();

    /// <summary>In-memory article store backing the query delegates.</summary>
    public ConcurrentDictionary<string, ArticleQueryResult> ArticlesBySlug { get; } = new();

    /// <summary>In-memory mapping of article slug → aggregate GUID.</summary>
    public ConcurrentDictionary<string, Guid> ArticleIdsBySlug { get; } = new();

    /// <summary>In-memory follow relationships.</summary>
    public ConcurrentBag<(Guid FollowerId, Guid FolloweeId)> Follows { get; } = [];

    /// <summary>In-memory tags.</summary>
    public ConcurrentBag<string> Tags { get; } = [];

    /// <summary>In-memory comments.</summary>
    public ConcurrentDictionary<string, List<CommentQueryResult>> CommentsBySlug { get; } = new();

    /// <summary>The JWT secret used for token generation in tests.</summary>
    public const string TestJwtSecret = "test-secret-key-that-is-at-least-32-characters-long!!";

    /// <summary>The JWT issuer used in tests.</summary>
    public const string TestJwtIssuer = "conduit-test";

    private JwtTokenService? _jwtTokenService;

    /// <summary>Gets the JWT token service for generating tokens in tests.</summary>
    public JwtTokenService JwtTokenService =>
        _jwtTokenService ??= new JwtTokenService(TestJwtSecret, TestJwtIssuer);

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide the JWT secret via configuration so Program.cs' fail-fast check
        // passes before DI service replacement occurs.
        builder.UseSetting("Jwt:Secret", TestJwtSecret);
        builder.UseSetting("Jwt:Issuer", TestJwtIssuer);

        builder.ConfigureServices(services =>
        {
            // Remove all real infrastructure registrations
            RemoveService<EventStore<UserEvent>>(services);
            RemoveService<EventStore<ArticleEvent>>(services);
            RemoveService<NpgsqlDataSource>(services);
            RemoveService<JwtTokenService>(services);
            RemoveService<AggregateStore>(services);
            RemoveService<FindUserByEmail>(services);
            RemoveService<FindUserById>(services);
            RemoveService<FindUserByUsername>(services);
            RemoveService<GetProfile>(services);
            RemoveService<ListArticles>(services);
            RemoveService<GetFeed>(services);
            RemoveService<FindArticleBySlug>(services);
            RemoveService<FindArticleIdBySlug>(services);
            RemoveService<GetComments>(services);
            RemoveService<GetTags>(services);

            // Register in-memory event stores
            services.AddSingleton<EventStore<UserEvent>>(UserEventStore);
            services.AddSingleton<EventStore<ArticleEvent>>(ArticleEventStore);

            // Register JWT token service with test secret
            services.AddSingleton(JwtTokenService);

            // Register AggregateStore without PostgreSQL projection
            // We use a special test version that skips projection (no NpgsqlDataSource)
            services.AddSingleton(sp =>
                new AggregateStore(
                    sp.GetRequiredService<EventStore<UserEvent>>(),
                    sp.GetRequiredService<EventStore<ArticleEvent>>(),
                    null!)); // No PostgreSQL projection in tests

            // Register in-memory query delegates
            services.AddSingleton<FindUserByEmail>(FindUserByEmailImpl);
            services.AddSingleton<FindUserById>(FindUserByIdImpl);
            services.AddSingleton<FindUserByUsername>(FindUserByUsernameImpl);
            services.AddSingleton<GetProfile>(GetProfileImpl);
            services.AddSingleton<ListArticles>(ListArticlesImpl);
            services.AddSingleton<GetFeed>(GetFeedImpl);
            services.AddSingleton<FindArticleBySlug>(FindArticleBySlugImpl);
            services.AddSingleton<FindArticleIdBySlug>(FindArticleIdBySlugImpl);
            services.AddSingleton<GetComments>(GetCommentsImpl);
            services.AddSingleton<GetTags>(GetTagsImpl);
        });

        // Remove the Schema.EnsureCreated call by skipping the startup filter
        builder.UseEnvironment("Testing");
    }

    // ─── In-Memory Query Implementations ─────────────────────────────────────────

    private ValueTask<Option<UserReadModel>> FindUserByEmailImpl(
        string email, CancellationToken ct)
    {
        var user = Users.Values.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return new ValueTask<Option<UserReadModel>>(
            user is not null ? Option<UserReadModel>.Some(user) : Option<UserReadModel>.None);
    }

    private ValueTask<Option<UserReadModel>> FindUserByIdImpl(
        Guid userId, CancellationToken ct)
    {
        var result = Users.TryGetValue(userId, out var user)
            ? Option<UserReadModel>.Some(user)
            : Option<UserReadModel>.None;
        return new ValueTask<Option<UserReadModel>>(result);
    }

    private ValueTask<Option<UserReadModel>> FindUserByUsernameImpl(
        string username, CancellationToken ct)
    {
        var user = Users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return new ValueTask<Option<UserReadModel>>(
            user is not null ? Option<UserReadModel>.Some(user) : Option<UserReadModel>.None);
    }

    private ValueTask<Option<ProfileReadModel>> GetProfileImpl(
        string username, Option<Guid> currentUserId, CancellationToken ct)
    {
        var user = Users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
            return new ValueTask<Option<ProfileReadModel>>(Option<ProfileReadModel>.None);

        var following = currentUserId.IsSome &&
            Follows.Any(f => f.FollowerId == currentUserId.Value && f.FolloweeId == user.Id);

        var profile = new ProfileReadModel(user.Username, user.Bio, user.Image, following);
        return new ValueTask<Option<ProfileReadModel>>(Option<ProfileReadModel>.Some(profile));
    }

    private ValueTask<ArticleListResult> ListArticlesImpl(
        ArticleFilter filter, CancellationToken ct)
    {
        var articles = ArticlesBySlug.Values.ToList();
        return new ValueTask<ArticleListResult>(
            new ArticleListResult(articles, articles.Count));
    }

    private ValueTask<ArticleListResult> GetFeedImpl(
        Guid userId, int limit, int offset, CancellationToken ct) =>
        new(new ArticleListResult([], 0));

    private ValueTask<Option<ArticleQueryResult>> FindArticleBySlugImpl(
        string slug, Option<Guid> currentUserId, CancellationToken ct)
    {
        var result = ArticlesBySlug.TryGetValue(slug, out var article)
            ? Option<ArticleQueryResult>.Some(article)
            : Option<ArticleQueryResult>.None;
        return new ValueTask<Option<ArticleQueryResult>>(result);
    }

    private ValueTask<Option<Guid>> FindArticleIdBySlugImpl(
        string slug, CancellationToken ct)
    {
        var result = ArticleIdsBySlug.TryGetValue(slug, out var id)
            ? Option<Guid>.Some(id)
            : Option<Guid>.None;
        return new ValueTask<Option<Guid>>(result);
    }

    private ValueTask<IReadOnlyList<CommentQueryResult>> GetCommentsImpl(
        string slug, Option<Guid> currentUserId, CancellationToken ct)
    {
        var comments = CommentsBySlug.TryGetValue(slug, out var list)
            ? (IReadOnlyList<CommentQueryResult>)list
            : [];
        return new ValueTask<IReadOnlyList<CommentQueryResult>>(comments);
    }

    private ValueTask<IReadOnlyList<string>> GetTagsImpl(CancellationToken ct) =>
        new(Tags.Distinct().ToList());

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
    }

    /// <summary>
    /// Seeds a user into the in-memory store and returns the user read model.
    /// </summary>
    public UserReadModel SeedUser(
        Guid? id = null,
        string username = "testuser",
        string email = "test@example.com",
        string password = "password123",
        string bio = "",
        string image = "")
    {
        var userId = id ?? Guid.NewGuid();
        var passwordHash = PasswordHasher.Hash(
            Password.Create(password).Value);
        var user = new UserReadModel(
            userId, email, username, passwordHash.Value, bio, image,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        Users[userId] = user;
        return user;
    }

    /// <summary>
    /// Creates an HttpClient with an Authorization header for the given user.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(UserReadModel user)
    {
        var client = CreateClient();
        var token = JwtTokenService.GenerateToken(user.Id, user.Username, user.Email);
        client.DefaultRequestHeaders.Add("Authorization", $"Token {token}");
        return client;
    }
}
