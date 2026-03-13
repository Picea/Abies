// =============================================================================
// Aggregate Store — In-Process Lifecycle Management for AggregateRunners
// =============================================================================
// Caches AggregateRunner instances in a ConcurrentDictionary, keyed by
// stream ID. Each runner is loaded from KurrentDB on first access and reused
// for the process lifetime.
//
// After each successful Handle, the new events are projected to PostgreSQL
// via the UserProjection/ArticleProjection observers. This gives us
// synchronous read model updates within the request boundary — the read
// model is consistent by the time the HTTP response is sent.
//
// Thread safety:
//   - ConcurrentDictionary serializes GetOrAdd per key (hash bucket lock)
//   - AggregateRunner's internal SemaphoreSlim serializes Handle per aggregate
//   - Combined: safe for concurrent HTTP requests to the same aggregate
//
// Design decision: No eviction policy. For the Conduit showcase app, the
// aggregate count is bounded and small. Production systems would add
// LRU eviction or bounded cache size.
// =============================================================================

using System.Collections.Concurrent;
using Npgsql;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadStore.PostgreSQL;
using Picea.Glauca;

namespace Picea.Abies.Conduit.Api.Infrastructure;

/// <summary>
/// Type alias for the User AggregateRunner — reduces visual noise in endpoint code.
/// </summary>
public sealed class UserRunner()
{
    /// <summary>The full generic AggregateRunner type for the User aggregate.</summary>
    public static AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit> Create(
        EventStore<UserEvent> store, string streamId) =>
        AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>
            .Create(store, streamId, default);

    /// <summary>Loads a User aggregate from the event store.</summary>
    public static ValueTask<AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>> Load(
        EventStore<UserEvent> store, string streamId, CancellationToken ct = default) =>
        AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>
            .Load(store, streamId, default, ct);
}

/// <summary>
/// Type alias for the Article AggregateRunner — reduces visual noise in endpoint code.
/// </summary>
public sealed class ArticleRunner()
{
    /// <summary>The full generic AggregateRunner type for the Article aggregate.</summary>
    public static AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit> Create(
        EventStore<ArticleEvent> store, string streamId) =>
        AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>
            .Create(store, streamId, default);

    /// <summary>Loads an Article aggregate from the event store.</summary>
    public static ValueTask<AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>> Load(
        EventStore<ArticleEvent> store, string streamId, CancellationToken ct = default) =>
        AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>
            .Load(store, streamId, default, ct);
}

/// <summary>
/// Manages the lifecycle of event-sourced aggregates (User, Article).
/// </summary>
/// <remarks>
/// <para>
/// Responsibilities:
/// <list type="bullet">
///   <item>Cache AggregateRunner instances (load-once, reuse)</item>
///   <item>Wire observer projections (events → PostgreSQL read model)</item>
///   <item>Provide typed Handle methods that include projection side-effects</item>
/// </list>
/// </para>
/// <para>
/// The observer pattern here is explicit: after a successful Handle, we iterate
/// over the newly produced events and project each one. This is synchronous
/// within the request — the caller sees the updated read model immediately.
/// </para>
/// </remarks>
public sealed class AggregateStore
{
    private readonly EventStore<UserEvent> _userEventStore;
    private readonly EventStore<ArticleEvent> _articleEventStore;
    private readonly NpgsqlDataSource? _dataSource;

    private readonly ConcurrentDictionary<string, AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>> _userRunners = new();
    private readonly ConcurrentDictionary<string, AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>> _articleRunners = new();

    /// <summary>Creates a new aggregate store.</summary>
    /// <param name="userEventStore">The KurrentDB-backed event store for User events.</param>
    /// <param name="articleEventStore">The KurrentDB-backed event store for Article events.</param>
    /// <param name="dataSource">The PostgreSQL data source for projection writes. Null disables projection (testing).</param>
    public AggregateStore(
        EventStore<UserEvent> userEventStore,
        EventStore<ArticleEvent> articleEventStore,
        NpgsqlDataSource? dataSource)
    {
        _userEventStore = userEventStore;
        _articleEventStore = articleEventStore;
        _dataSource = dataSource;
    }

    // ─── Stream ID conventions ─────────────────────────────────────────────────

    /// <summary>Builds the stream ID for a User aggregate.</summary>
    public static string UserStreamId(UserId userId) => $"User-{userId.Value}";

    /// <summary>Builds the stream ID for a User aggregate.</summary>
    public static string UserStreamId(Guid userId) => $"User-{userId}";

    /// <summary>Builds the stream ID for an Article aggregate.</summary>
    public static string ArticleStreamId(ArticleId articleId) => $"Article-{articleId.Value}";

    /// <summary>Builds the stream ID for an Article aggregate.</summary>
    public static string ArticleStreamId(Guid articleId) => $"Article-{articleId}";

    // ─── User Aggregate ────────────────────────────────────────────────────────

    /// <summary>
    /// Handles a command on a User aggregate, projecting resulting events to PostgreSQL.
    /// </summary>
    /// <param name="userId">The user's aggregate ID.</param>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new user state on success, or a user error.</returns>
    public async ValueTask<Result<UserState, UserError>> HandleUserCommand(
        Guid userId,
        UserCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = ApiDiagnostics.Source.StartActivity("AggregateStore.HandleUserCommand");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("command.type", command.GetType().Name);

        var streamId = UserStreamId(userId);
        var runner = await GetOrLoadUserRunner(streamId, cancellationToken).ConfigureAwait(false);
        var versionBefore = runner.Version;

        var result = await runner.Handle(command, cancellationToken).ConfigureAwait(false);

        // Project new events to PostgreSQL (observer pattern)
        if (result.IsOk)
        {
            await ProjectUserEvents(streamId, userId, versionBefore, runner, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Gets the current state of a User aggregate (loading from store if needed).
    /// </summary>
    public async ValueTask<UserState> GetUserState(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var streamId = UserStreamId(userId);
        var runner = await GetOrLoadUserRunner(streamId, cancellationToken).ConfigureAwait(false);
        return runner.State;
    }

    private async ValueTask<AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>> GetOrLoadUserRunner(
        string streamId,
        CancellationToken cancellationToken)
    {
        if (_userRunners.TryGetValue(streamId, out var existing))
            return existing;

        var runner = await UserRunner.Load(_userEventStore, streamId, cancellationToken)
            .ConfigureAwait(false);

        // If another thread loaded concurrently, keep the first one
        return _userRunners.GetOrAdd(streamId, runner);
    }

    private async ValueTask ProjectUserEvents(
        string streamId,
        Guid userId,
        long versionBefore,
        AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit> runner,
        CancellationToken cancellationToken)
    {
        // Skip projection when no data source is configured (integration tests)
        if (_dataSource is null)
            return;

        // Load only the new events (after the version we had before Handle)
        var newEvents = await _userEventStore.LoadAsync(streamId, versionBefore, cancellationToken)
            .ConfigureAwait(false);

        foreach (var storedEvent in newEvents)
        {
            await UserProjection.Apply(_dataSource, userId, storedEvent.Event, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // ─── Article Aggregate ─────────────────────────────────────────────────────

    /// <summary>
    /// Handles a command on an Article aggregate, projecting resulting events to PostgreSQL.
    /// </summary>
    /// <param name="articleId">The article's aggregate ID.</param>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new article state on success, or an article error.</returns>
    public async ValueTask<Result<ArticleState, ArticleError>> HandleArticleCommand(
        Guid articleId,
        ArticleCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = ApiDiagnostics.Source.StartActivity("AggregateStore.HandleArticleCommand");
        activity?.SetTag("article.id", articleId);
        activity?.SetTag("command.type", command.GetType().Name);

        var streamId = ArticleStreamId(articleId);
        var runner = await GetOrLoadArticleRunner(streamId, cancellationToken).ConfigureAwait(false);
        var versionBefore = runner.Version;

        var result = await runner.Handle(command, cancellationToken).ConfigureAwait(false);

        // Project new events to PostgreSQL (observer pattern)
        if (result.IsOk)
        {
            await ProjectArticleEvents(streamId, articleId, versionBefore, runner, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Gets the current state of an Article aggregate (loading from store if needed).
    /// </summary>
    public async ValueTask<ArticleState> GetArticleState(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var streamId = ArticleStreamId(articleId);
        var runner = await GetOrLoadArticleRunner(streamId, cancellationToken).ConfigureAwait(false);
        return runner.State;
    }

    private async ValueTask<AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>> GetOrLoadArticleRunner(
        string streamId,
        CancellationToken cancellationToken)
    {
        if (_articleRunners.TryGetValue(streamId, out var existing))
            return existing;

        var runner = await ArticleRunner.Load(_articleEventStore, streamId, cancellationToken)
            .ConfigureAwait(false);

        return _articleRunners.GetOrAdd(streamId, runner);
    }

    private async ValueTask ProjectArticleEvents(
        string streamId,
        Guid articleId,
        long versionBefore,
        AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit> runner,
        CancellationToken cancellationToken)
    {
        // Skip projection when no data source is configured (integration tests)
        if (_dataSource is null)
            return;

        var newEvents = await _articleEventStore.LoadAsync(streamId, versionBefore, cancellationToken)
            .ConfigureAwait(false);

        foreach (var storedEvent in newEvents)
        {
            await ArticleProjection.Apply(_dataSource, articleId, storedEvent.Event, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
