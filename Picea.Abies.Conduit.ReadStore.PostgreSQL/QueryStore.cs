// =============================================================================
// Query Store — PostgreSQL Implementations of Read Model Queries
// =============================================================================
// Provides concrete implementations of the query capability delegates defined
// in Picea.Abies.Conduit.ReadModel.Queries. All queries use raw parameterized SQL
// via Npgsql. Each function is a factory that captures the NpgsqlDataSource
// and returns a delegate matching the capability signature.
//
// Design:
//   - Factory pattern: QueryStore.CreateFindUserByEmail(dataSource) → FindUserByEmail
//   - No ORM — SQL is explicit and auditable
//   - OpenTelemetry tracing on all operations
//   - Option<T> for nullable results (no null propagation)
// =============================================================================

using System.Diagnostics;
using Npgsql;
using NpgsqlTypes;
using Picea;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.ReadStore.PostgreSQL;

/// <summary>
/// Factory for creating query capability delegate implementations backed by PostgreSQL.
/// </summary>
public static class QueryStore
{
    // ─── User Queries ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="FindUserByEmail"/> capability backed by PostgreSQL.
    /// </summary>
    public static FindUserByEmail CreateFindUserByEmail(NpgsqlDataSource dataSource) =>
        async (email, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.FindUserByEmail");
            activity?.SetTag("query.email", email);

            const string sql = """
                SELECT id, email, username, password_hash, bio, image, created_at, updated_at
                FROM users
                WHERE email = @email
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("email", NpgsqlDbType.Text, email);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Option<UserReadModel>.Some(ReadUser(reader));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Option<UserReadModel>.None;
        };

    /// <summary>
    /// Creates a <see cref="FindUserById"/> capability backed by PostgreSQL.
    /// </summary>
    public static FindUserById CreateFindUserById(NpgsqlDataSource dataSource) =>
        async (userId, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.FindUserById");
            activity?.SetTag("query.userId", userId);

            const string sql = """
                SELECT id, email, username, password_hash, bio, image, created_at, updated_at
                FROM users
                WHERE id = @id
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Option<UserReadModel>.Some(ReadUser(reader));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Option<UserReadModel>.None;
        };

    /// <summary>
    /// Creates a <see cref="FindUserByUsername"/> capability backed by PostgreSQL.
    /// </summary>
    public static FindUserByUsername CreateFindUserByUsername(NpgsqlDataSource dataSource) =>
        async (username, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.FindUserByUsername");
            activity?.SetTag("query.username", username);

            const string sql = """
                SELECT id, email, username, password_hash, bio, image, created_at, updated_at
                FROM users
                WHERE username = @username
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("username", NpgsqlDbType.Text, username);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Option<UserReadModel>.Some(ReadUser(reader));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Option<UserReadModel>.None;
        };

    /// <summary>
    /// Creates a <see cref="GetProfile"/> capability backed by PostgreSQL.
    /// </summary>
    public static GetProfile CreateGetProfile(NpgsqlDataSource dataSource) =>
        async (username, currentUserId, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.GetProfile");
            activity?.SetTag("query.username", username);

            const string sql = """
                SELECT u.username, u.bio, u.image,
                       CASE WHEN f.follower_id IS NOT NULL THEN TRUE ELSE FALSE END AS following
                FROM users u
                LEFT JOIN follows f ON f.followee_id = u.id AND f.follower_id = @currentUserId
                WHERE u.username = @username
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("username", NpgsqlDbType.Text, username);
            command.Parameters.AddWithValue("currentUserId", NpgsqlDbType.Uuid,
                currentUserId.Match(id => (object)id, () => DBNull.Value));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Option<ProfileReadModel>.Some(new ProfileReadModel(
                    Username: reader.GetString(0),
                    Bio: reader.GetString(1),
                    Image: reader.GetString(2),
                    Following: reader.GetBoolean(3)));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Option<ProfileReadModel>.None;
        };

    // ─── Article Queries ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="ListArticles"/> capability backed by PostgreSQL.
    /// </summary>
    public static ListArticles CreateListArticles(NpgsqlDataSource dataSource) =>
        async (filter, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.ListArticles");

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            var whereClauses = new List<string> { "a.deleted = FALSE" };
            var parameters = new List<NpgsqlParameter>();

            filter.Tag.Map(tag =>
            {
                whereClauses.Add("EXISTS (SELECT 1 FROM article_tags at2 WHERE at2.article_id = a.id AND at2.tag = @tag)");
                parameters.Add(new NpgsqlParameter("tag", NpgsqlDbType.Text) { Value = tag });
                return tag;
            });

            filter.Author.Map(author =>
            {
                whereClauses.Add("a.author_username = @author");
                parameters.Add(new NpgsqlParameter("author", NpgsqlDbType.Text) { Value = author });
                return author;
            });

            filter.FavoritedBy.Map(favoritedBy =>
            {
                whereClauses.Add("EXISTS (SELECT 1 FROM favorites fav2 JOIN users fu ON fav2.user_id = fu.id WHERE fav2.article_id = a.id AND fu.username = @favoritedBy)");
                parameters.Add(new NpgsqlParameter("favoritedBy", NpgsqlDbType.Text) { Value = favoritedBy });
                return favoritedBy;
            });

            var where = string.Join(" AND ", whereClauses);

            // Count total matching articles
            var countSql = $"SELECT COUNT(*) FROM articles a WHERE {where}";
            await using var countCmd = new NpgsqlCommand(countSql, connection);
            countCmd.Parameters.AddRange(parameters.Select(p => p.Clone()).ToArray());
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false));

            // Fetch articles with tags inlined via array_agg (avoids N+1 / MARS issue)
            var querySql = $"""
                SELECT a.id, a.slug, a.title, a.description, a.body,
                       a.author_id, a.author_username, a.author_bio, a.author_image,
                       a.created_at, a.updated_at, a.favorites_count,
                       CASE WHEN fav.user_id IS NOT NULL THEN TRUE ELSE FALSE END AS favorited,
                       CASE WHEN fol.follower_id IS NOT NULL THEN TRUE ELSE FALSE END AS following,
                       COALESCE(array_agg(t.tag ORDER BY t.tag) FILTER (WHERE t.tag IS NOT NULL), ARRAY[]::TEXT[]) AS tags
                FROM articles a
                LEFT JOIN favorites fav ON fav.article_id = a.id AND fav.user_id = @currentUserId
                LEFT JOIN follows fol ON fol.followee_id = a.author_id AND fol.follower_id = @currentUserId
                LEFT JOIN article_tags t ON t.article_id = a.id
                WHERE {where}
                GROUP BY a.id, a.slug, a.title, a.description, a.body,
                         a.author_id, a.author_username, a.author_bio, a.author_image,
                         a.created_at, a.updated_at, a.favorites_count,
                         fav.user_id, fol.follower_id
                ORDER BY a.created_at DESC
                LIMIT @limit OFFSET @offset
                """;

            await using var queryCmd = new NpgsqlCommand(querySql, connection);
            queryCmd.Parameters.AddRange(parameters.Select(p => p.Clone()).ToArray());
            queryCmd.Parameters.AddWithValue("currentUserId", NpgsqlDbType.Uuid,
                filter.CurrentUserId.Match(id => (object)id, () => DBNull.Value));
            queryCmd.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, filter.Limit);
            queryCmd.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, filter.Offset);

            var articles = new List<ArticleQueryResult>();
            await using var reader = await queryCmd.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var tags = reader.GetFieldValue<string[]>(14);

                articles.Add(new ArticleQueryResult(
                    Slug: reader.GetString(1),
                    Title: reader.GetString(2),
                    Description: reader.GetString(3),
                    Body: reader.GetString(4),
                    TagList: tags,
                    CreatedAt: reader.GetDateTime(9),
                    UpdatedAt: reader.GetDateTime(10),
                    Favorited: reader.GetBoolean(12),
                    FavoritesCount: reader.GetInt32(11),
                    Author: new ProfileReadModel(
                        Username: reader.GetString(6),
                        Bio: reader.GetString(7),
                        Image: reader.GetString(8),
                        Following: reader.GetBoolean(13))));
            }

            activity?.SetTag("query.count", articles.Count);
            activity?.SetTag("query.total", totalCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new ArticleListResult(articles, totalCount);
        };

    /// <summary>
    /// Creates a <see cref="GetFeed"/> capability backed by PostgreSQL.
    /// </summary>
    public static GetFeed CreateGetFeed(NpgsqlDataSource dataSource) =>
        async (userId, limit, offset, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.GetFeed");
            activity?.SetTag("query.userId", userId);

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            // Count total feed articles
            const string countSql = """
                SELECT COUNT(*)
                FROM articles a
                INNER JOIN follows f ON f.followee_id = a.author_id AND f.follower_id = @userId
                WHERE a.deleted = FALSE
                """;

            await using var countCmd = new NpgsqlCommand(countSql, connection);
            countCmd.Parameters.AddWithValue("userId", NpgsqlDbType.Uuid, userId);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false));

            // Fetch feed articles with tags inlined via array_agg
            const string querySql = """
                SELECT a.id, a.slug, a.title, a.description, a.body,
                       a.author_id, a.author_username, a.author_bio, a.author_image,
                       a.created_at, a.updated_at, a.favorites_count,
                       CASE WHEN fav.user_id IS NOT NULL THEN TRUE ELSE FALSE END AS favorited,
                       TRUE AS following,
                       COALESCE(array_agg(t.tag ORDER BY t.tag) FILTER (WHERE t.tag IS NOT NULL), ARRAY[]::TEXT[]) AS tags
                FROM articles a
                INNER JOIN follows f ON f.followee_id = a.author_id AND f.follower_id = @userId
                LEFT JOIN favorites fav ON fav.article_id = a.id AND fav.user_id = @userId
                LEFT JOIN article_tags t ON t.article_id = a.id
                WHERE a.deleted = FALSE
                GROUP BY a.id, a.slug, a.title, a.description, a.body,
                         a.author_id, a.author_username, a.author_bio, a.author_image,
                         a.created_at, a.updated_at, a.favorites_count,
                         fav.user_id
                ORDER BY a.created_at DESC
                LIMIT @limit OFFSET @offset
                """;

            await using var queryCmd = new NpgsqlCommand(querySql, connection);
            queryCmd.Parameters.AddWithValue("userId", NpgsqlDbType.Uuid, userId);
            queryCmd.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
            queryCmd.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);

            var articles = new List<ArticleQueryResult>();
            await using var reader = await queryCmd.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var tags = reader.GetFieldValue<string[]>(14);

                articles.Add(new ArticleQueryResult(
                    Slug: reader.GetString(1),
                    Title: reader.GetString(2),
                    Description: reader.GetString(3),
                    Body: reader.GetString(4),
                    TagList: tags,
                    CreatedAt: reader.GetDateTime(9),
                    UpdatedAt: reader.GetDateTime(10),
                    Favorited: reader.GetBoolean(12),
                    FavoritesCount: reader.GetInt32(11),
                    Author: new ProfileReadModel(
                        Username: reader.GetString(6),
                        Bio: reader.GetString(7),
                        Image: reader.GetString(8),
                        Following: reader.GetBoolean(13))));
            }

            activity?.SetTag("query.count", articles.Count);
            activity?.SetTag("query.total", totalCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new ArticleListResult(articles, totalCount);
        };

    /// <summary>
    /// Creates a <see cref="FindArticleBySlug"/> capability backed by PostgreSQL.
    /// </summary>
    public static FindArticleBySlug CreateFindArticleBySlug(NpgsqlDataSource dataSource) =>
        async (slug, currentUserId, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.FindArticleBySlug");
            activity?.SetTag("query.slug", slug);

            const string sql = """
                SELECT a.id, a.slug, a.title, a.description, a.body,
                       a.author_id, a.author_username, a.author_bio, a.author_image,
                       a.created_at, a.updated_at, a.favorites_count,
                       CASE WHEN fav.user_id IS NOT NULL THEN TRUE ELSE FALSE END AS favorited,
                       CASE WHEN fol.follower_id IS NOT NULL THEN TRUE ELSE FALSE END AS following,
                       COALESCE(array_agg(t.tag ORDER BY t.tag) FILTER (WHERE t.tag IS NOT NULL), ARRAY[]::TEXT[]) AS tags
                FROM articles a
                LEFT JOIN favorites fav ON fav.article_id = a.id AND fav.user_id = @currentUserId
                LEFT JOIN follows fol ON fol.followee_id = a.author_id AND fol.follower_id = @currentUserId
                LEFT JOIN article_tags t ON t.article_id = a.id
                WHERE a.slug = @slug AND a.deleted = FALSE
                GROUP BY a.id, a.slug, a.title, a.description, a.body,
                         a.author_id, a.author_username, a.author_bio, a.author_image,
                         a.created_at, a.updated_at, a.favorites_count,
                         fav.user_id, fol.follower_id
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("slug", NpgsqlDbType.Text, slug);
            command.Parameters.AddWithValue("currentUserId", NpgsqlDbType.Uuid,
                currentUserId.Match(id => (object)id, () => DBNull.Value));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var tags = reader.GetFieldValue<string[]>(14);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Option<ArticleQueryResult>.Some(new ArticleQueryResult(
                    Slug: reader.GetString(1),
                    Title: reader.GetString(2),
                    Description: reader.GetString(3),
                    Body: reader.GetString(4),
                    TagList: tags,
                    CreatedAt: reader.GetDateTime(9),
                    UpdatedAt: reader.GetDateTime(10),
                    Favorited: reader.GetBoolean(12),
                    FavoritesCount: reader.GetInt32(11),
                    Author: new ProfileReadModel(
                        Username: reader.GetString(6),
                        Bio: reader.GetString(7),
                        Image: reader.GetString(8),
                        Following: reader.GetBoolean(13))));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Option<ArticleQueryResult>.None;
        };

    // ─── Comment Queries ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="FindArticleIdBySlug"/> capability backed by PostgreSQL.
    /// </summary>
    public static FindArticleIdBySlug CreateFindArticleIdBySlug(NpgsqlDataSource dataSource) =>
        async (slug, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.FindArticleIdBySlug");
            activity?.SetTag("query.slug", slug);

            const string sql = """
                SELECT id FROM articles WHERE slug = @slug AND deleted = FALSE
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("slug", NpgsqlDbType.Text, slug);

            var result = await command.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is Guid id)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Option<Guid>.Some(id);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Option<Guid>.None;
        };

    /// <summary>
    /// Creates a <see cref="GetComments"/> capability backed by PostgreSQL.
    /// </summary>
    public static GetComments CreateGetComments(NpgsqlDataSource dataSource) =>
        async (slug, currentUserId, cancellationToken) =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.GetComments");
            activity?.SetTag("query.slug", slug);

            const string sql = """
                SELECT c.id, c.created_at, c.updated_at, c.body,
                       c.author_username, c.author_bio, c.author_image,
                       CASE WHEN fol.follower_id IS NOT NULL THEN TRUE ELSE FALSE END AS following
                FROM comments c
                LEFT JOIN users u ON u.username = c.author_username
                LEFT JOIN follows fol ON fol.followee_id = u.id AND fol.follower_id = @currentUserId
                WHERE c.article_slug = @slug AND c.deleted = FALSE
                ORDER BY c.created_at ASC
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("slug", NpgsqlDbType.Text, slug);
            command.Parameters.AddWithValue("currentUserId", NpgsqlDbType.Uuid,
                currentUserId.Match(id => (object)id, () => DBNull.Value));

            var comments = new List<CommentQueryResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                comments.Add(new CommentQueryResult(
                    Id: reader.GetGuid(0),
                    CreatedAt: reader.GetDateTime(1),
                    UpdatedAt: reader.GetDateTime(2),
                    Body: reader.GetString(3),
                    Author: new ProfileReadModel(
                        Username: reader.GetString(4),
                        Bio: reader.GetString(5),
                        Image: reader.GetString(6),
                        Following: reader.GetBoolean(7))));
            }

            activity?.SetTag("query.count", comments.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return comments;
        };

    // ─── Tag Queries ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="GetTags"/> capability backed by PostgreSQL.
    /// </summary>
    public static GetTags CreateGetTags(NpgsqlDataSource dataSource) =>
        async cancellationToken =>
        {
            using var activity = ReadStoreDiagnostics.Source.StartActivity("QueryStore.GetTags");

            const string sql = """
                SELECT DISTINCT tag FROM article_tags ORDER BY tag
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);

            var tags = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                tags.Add(reader.GetString(0));
            }

            activity?.SetTag("query.count", tags.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return tags;
        };

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static UserReadModel ReadUser(NpgsqlDataReader reader) =>
        new(
            Id: reader.GetGuid(0),
            Email: reader.GetString(1),
            Username: reader.GetString(2),
            PasswordHash: reader.GetString(3),
            Bio: reader.GetString(4),
            Image: reader.GetString(5),
            CreatedAt: reader.GetDateTime(6),
            UpdatedAt: reader.GetDateTime(7));
}
