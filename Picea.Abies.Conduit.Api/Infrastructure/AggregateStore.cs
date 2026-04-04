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
    private const int RecentArticleCacheLimit = 512;

    private readonly EventStore<UserEvent> _userEventStore;
    private readonly EventStore<ArticleEvent> _articleEventStore;
    private readonly NpgsqlDataSource? _dataSource;

    private readonly ConcurrentDictionary<string, AggregateRunner<User, UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>> _userRunners = new();
    private readonly ConcurrentDictionary<string, AggregateRunner<Article, ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>> _articleRunners = new();
    private readonly ConcurrentDictionary<string, byte> _registeredEmails = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _registeredUsernames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _registeredArticleSlugs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _uniquenessLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, ArticleState> _recentArticlesBySlug = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, string> _recentArticleSlugById = new();
    private readonly ConcurrentDictionary<string, long> _recentArticleSlugVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<(string Slug, long Version)> _recentArticleSlugQueue = new();
    private long _recentArticleVersionCounter;

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
    /// Handles user registration with an atomic in-process uniqueness gate.
    /// </summary>
    public async ValueTask<Result<UserState, UserError>> HandleUniqueUserRegistration(
        Guid userId,
        UserCommand.Register command,
        string email,
        string username,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeUniquenessKey(email);
        var normalizedUsername = NormalizeUniquenessKey(username);

        await using var guard = await AcquireUniquenessLocks(
            [$"user:email:{normalizedEmail}", $"user:username:{normalizedUsername}"],
            cancellationToken).ConfigureAwait(false);

        if (_registeredEmails.ContainsKey(normalizedEmail))
            return Result<UserState, UserError>.Err(new UserError.DuplicateEmail());

        if (_registeredUsernames.ContainsKey(normalizedUsername))
            return Result<UserState, UserError>.Err(new UserError.DuplicateUsername());

        var result = await HandleUserCommand(userId, command, cancellationToken).ConfigureAwait(false);
        if (result.IsOk)
        {
            _registeredEmails.TryAdd(normalizedEmail, 0);
            _registeredUsernames.TryAdd(normalizedUsername, 0);
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

    /// <summary>
    /// Handles a profile update command with an atomic in-process uniqueness gate.
    /// Only checks/reserves email and username that are actually changing (i.e., differ
    /// from the current state). This prevents false conflicts when a user keeps their
    /// existing email or username.
    /// </summary>
    public async ValueTask<Result<UserState, UserError>> HandleUniqueUserUpdate(
        Guid userId,
        UserCommand.UpdateProfile command,
        string? newEmail,
        string? newUsername,
        CancellationToken cancellationToken = default)
    {
        // If neither email nor username is being changed, skip uniqueness gate entirely
        if (newEmail is null && newUsername is null)
            return await HandleUserCommand(userId, command, cancellationToken).ConfigureAwait(false);

        // Get current user state to determine which values are actually changing
        var currentState = await GetUserState(userId, cancellationToken).ConfigureAwait(false);

        // Only enforce uniqueness on values that differ from the current ones
        var emailChanging = newEmail is not null &&
            NormalizeUniquenessKey(newEmail) != NormalizeUniquenessKey(currentState.Email.Value);
        var usernameChanging = newUsername is not null &&
            NormalizeUniquenessKey(newUsername) != NormalizeUniquenessKey(currentState.Username.Value);

        if (!emailChanging && !usernameChanging)
            return await HandleUserCommand(userId, command, cancellationToken).ConfigureAwait(false);

        var lockKeys = new List<string>();
        if (emailChanging)
            lockKeys.Add($"user:email:{NormalizeUniquenessKey(newEmail!)}");
        if (usernameChanging)
            lockKeys.Add($"user:username:{NormalizeUniquenessKey(newUsername!)}");

        await using var guard = await AcquireUniquenessLocks(lockKeys, cancellationToken).ConfigureAwait(false);

        if (emailChanging && _registeredEmails.ContainsKey(NormalizeUniquenessKey(newEmail!)))
            return Result<UserState, UserError>.Err(new UserError.DuplicateEmail());

        if (usernameChanging && _registeredUsernames.ContainsKey(NormalizeUniquenessKey(newUsername!)))
            return Result<UserState, UserError>.Err(new UserError.DuplicateUsername());

        var result = await HandleUserCommand(userId, command, cancellationToken).ConfigureAwait(false);

        if (result.IsOk)
        {
            if (emailChanging)
            {
                _registeredEmails.TryRemove(NormalizeUniquenessKey(currentState.Email.Value), out _);
                _registeredEmails.TryAdd(NormalizeUniquenessKey(newEmail!), 0);
            }
            if (usernameChanging)
            {
                _registeredUsernames.TryRemove(NormalizeUniquenessKey(currentState.Username.Value), out _);
                _registeredUsernames.TryAdd(NormalizeUniquenessKey(newUsername!), 0);
            }
        }

        return result;
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

            UpdateRecentArticleCache(articleId, result.Value);
        }

        return result;
    }

    /// <summary>
    /// Handles article creation with an atomic in-process uniqueness gate for slug.
    /// </summary>
    public async ValueTask<Result<ArticleState, ArticleError>> HandleUniqueArticleCreation(
        Guid articleId,
        ArticleCommand.CreateArticle command,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeUniquenessKey(slug);

        await using var guard = await AcquireUniquenessLocks(
            [$"article:slug:{normalizedSlug}"],
            cancellationToken).ConfigureAwait(false);

        if (_registeredArticleSlugs.ContainsKey(normalizedSlug))
            return Result<ArticleState, ArticleError>.Err(new ArticleError.DuplicateSlug());

        var result = await HandleArticleCommand(articleId, command, cancellationToken).ConfigureAwait(false);
        if (result.IsOk)
            _registeredArticleSlugs.TryAdd(normalizedSlug, 0);

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

    // ─── Shadow Data Support ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets all events from the event store for a given stream ID.
    /// Used for shadow data reconstruction when read model has projection lag.
    /// </summary>
    public async ValueTask<IReadOnlyList<UserEvent>> GetUserEvents(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var streamId = UserStreamId(userId);
        var events = await _userEventStore.LoadAsync(streamId, 0, cancellationToken)
            .ConfigureAwait(false);
        return events.Select(e => e.Event).ToList();
    }

    /// <summary>
    /// Gets all events from the event store for a given stream ID.
    /// Used for shadow data reconstruction when read model has projection lag.
    /// </summary>
    public async ValueTask<IReadOnlyList<ArticleEvent>> GetArticleEvents(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var streamId = ArticleStreamId(articleId);
        var events = await _articleEventStore.LoadAsync(streamId, 0, cancellationToken)
            .ConfigureAwait(false);
        return events.Select(e => e.Event).ToList();
    }

    /// <summary>
    /// Attempts to resolve a recently written article from in-memory write-side state.
    /// </summary>
    public bool TryGetRecentArticleBySlug(string slug, out ArticleState state) =>
        _recentArticlesBySlug.TryGetValue(slug, out state!);

    private static string NormalizeUniquenessKey(string value) =>
        value.Trim().ToLowerInvariant();

    private async ValueTask<UniquenessLockGuard> AcquireUniquenessLocks(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        var orderedKeys = keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var locks = new SemaphoreSlim[orderedKeys.Length];
        for (var i = 0; i < orderedKeys.Length; i++)
        {
            var semaphore = _uniquenessLocks.GetOrAdd(orderedKeys[i], _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            locks[i] = semaphore;
        }

        return new UniquenessLockGuard(locks);
    }

    private void UpdateRecentArticleCache(Guid articleId, ArticleState state)
    {
        if (state is { Published: true, Deleted: false })
        {
            var newSlug = state.Slug.Value;

            if (_recentArticleSlugById.TryGetValue(articleId, out var oldSlug)
                && !string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase))
            {
                _recentArticlesBySlug.TryRemove(oldSlug, out _);
                _registeredArticleSlugs.TryRemove(NormalizeUniquenessKey(oldSlug), out _);
            }

            _recentArticleSlugById[articleId] = newSlug;
            _recentArticlesBySlug[newSlug] = state;
            var newVersion = Interlocked.Increment(ref _recentArticleVersionCounter);
            _recentArticleSlugVersions[newSlug] = newVersion;

            _recentArticleSlugQueue.Enqueue((newSlug, newVersion));
            while (_recentArticleSlugQueue.Count > RecentArticleCacheLimit
                   && _recentArticleSlugQueue.TryDequeue(out var evict))
            {
                if (_recentArticleSlugVersions.TryGetValue(evict.Slug, out var currentVersion)
                    && currentVersion == evict.Version)
                {
                    _recentArticlesBySlug.TryRemove(evict.Slug, out _);
                    _recentArticleSlugVersions.TryRemove(evict.Slug, out _);
                }
            }

            _registeredArticleSlugs.TryAdd(NormalizeUniquenessKey(newSlug), 0);
            return;
        }

        if (_recentArticleSlugById.TryRemove(articleId, out var removedSlug))
        {
            _recentArticlesBySlug.TryRemove(removedSlug, out _);
            _recentArticleSlugVersions.TryRemove(removedSlug, out _);
            _registeredArticleSlugs.TryRemove(NormalizeUniquenessKey(removedSlug), out _);
        }
    }

    private readonly struct UniquenessLockGuard : IAsyncDisposable
    {
        private readonly SemaphoreSlim[] _locks;

        public UniquenessLockGuard(SemaphoreSlim[] locks) => _locks = locks;

        public ValueTask DisposeAsync()
        {
            for (var i = _locks.Length - 1; i >= 0; i--)
                _locks[i].Release();

            return ValueTask.CompletedTask;
        }
    }
}
