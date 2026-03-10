// =============================================================================
// Article Projection — Projects ArticleEvents into PostgreSQL Read Models
// =============================================================================
// Handles:
//   ArticleCreated    → INSERT into articles + article_tags
//   ArticleUpdated    → UPDATE articles (title/description/body/slug)
//   ArticleDeleted    → Soft-delete (SET deleted = TRUE)
//   CommentAdded      → INSERT into comments
//   CommentDeleted    → Soft-delete comment
//   ArticleFavorited  → INSERT into favorites + increment favorites_count
//   ArticleUnfavorited→ DELETE from favorites + decrement favorites_count
//
// All operations are idempotent (ON CONFLICT DO UPDATE / DO NOTHING).
// Author denormalized data is looked up from the users table at projection
// time — this ensures consistency with the latest user profile.
// =============================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Picea.Abies.Conduit.Domain.Article;
using Npgsql;
using NpgsqlTypes;

namespace Picea.Abies.Conduit.ReadStore.PostgreSQL;

/// <summary>
/// Projects <see cref="ArticleEvent"/> instances into PostgreSQL read model tables.
/// </summary>
public static class ArticleProjection
{
    /// <summary>
    /// Applies an <see cref="ArticleEvent"/> to the PostgreSQL read store.
    /// </summary>
    /// <param name="dataSource">The Npgsql data source.</param>
    /// <param name="articleId">The aggregate (article) ID that owns this event stream.</param>
    /// <param name="event">The article event to project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask Apply(
        NpgsqlDataSource dataSource,
        Guid articleId,
        ArticleEvent @event,
        CancellationToken cancellationToken = default)
    {
        using var activity = ReadStoreDiagnostics.Source.StartActivity("ArticleProjection.Apply");
        activity?.SetTag("article.id", articleId);
        activity?.SetTag("event.type", @event.GetType().Name);

        switch (@event)
        {
            case ArticleEvent.ArticleCreated created:
                await ApplyArticleCreated(dataSource, articleId, created, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ArticleEvent.ArticleUpdated updated:
                await ApplyArticleUpdated(dataSource, articleId, updated, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ArticleEvent.ArticleDeleted:
                await ApplyArticleDeleted(dataSource, articleId, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ArticleEvent.CommentAdded commentAdded:
                await ApplyCommentAdded(dataSource, articleId, commentAdded, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ArticleEvent.CommentDeleted commentDeleted:
                await ApplyCommentDeleted(dataSource, commentDeleted, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ArticleEvent.ArticleFavorited favorited:
                await ApplyArticleFavorited(dataSource, articleId, favorited, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ArticleEvent.ArticleUnfavorited unfavorited:
                await ApplyArticleUnfavorited(dataSource, articleId, unfavorited, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static async ValueTask ApplyArticleCreated(
        NpgsqlDataSource dataSource,
        Guid articleId,
        ArticleEvent.ArticleCreated created,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Look up author data from users table
        var (authorUsername, authorBio, authorImage) = await LookupAuthor(
            connection, created.AuthorId.Value, cancellationToken).ConfigureAwait(false);

        // Insert article
        const string articleSql = """
            INSERT INTO articles (id, slug, title, description, body, author_id,
                                  author_username, author_bio, author_image,
                                  created_at, updated_at, favorites_count, deleted)
            VALUES (@id, @slug, @title, @description, @body, @authorId,
                    @authorUsername, @authorBio, @authorImage,
                    @createdAt, @createdAt, 0, FALSE)
            ON CONFLICT (id) DO UPDATE SET
                slug = EXCLUDED.slug,
                title = EXCLUDED.title,
                description = EXCLUDED.description,
                body = EXCLUDED.body,
                author_id = EXCLUDED.author_id,
                author_username = EXCLUDED.author_username,
                author_bio = EXCLUDED.author_bio,
                author_image = EXCLUDED.author_image,
                created_at = EXCLUDED.created_at,
                updated_at = EXCLUDED.updated_at
            """;

        await using var articleCmd = new NpgsqlCommand(articleSql, connection, transaction);
        articleCmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, articleId);
        articleCmd.Parameters.AddWithValue("slug", NpgsqlDbType.Text, created.Slug.Value);
        articleCmd.Parameters.AddWithValue("title", NpgsqlDbType.Text, created.Title.Value);
        articleCmd.Parameters.AddWithValue("description", NpgsqlDbType.Text, created.Description.Value);
        articleCmd.Parameters.AddWithValue("body", NpgsqlDbType.Text, created.Body.Value);
        articleCmd.Parameters.AddWithValue("authorId", NpgsqlDbType.Uuid, created.AuthorId.Value);
        articleCmd.Parameters.AddWithValue("authorUsername", NpgsqlDbType.Text, authorUsername);
        articleCmd.Parameters.AddWithValue("authorBio", NpgsqlDbType.Text, authorBio);
        articleCmd.Parameters.AddWithValue("authorImage", NpgsqlDbType.Text, authorImage);
        articleCmd.Parameters.AddWithValue("createdAt", NpgsqlDbType.TimestampTz, created.CreatedAt.Value);

        await articleCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Insert tags
        if (created.Tags.Count > 0)
        {
            const string tagSql = """
                INSERT INTO article_tags (article_id, tag)
                VALUES (@articleId, @tag)
                ON CONFLICT DO NOTHING
                """;

            foreach (var tag in created.Tags)
            {
                await using var tagCmd = new NpgsqlCommand(tagSql, connection, transaction);
                tagCmd.Parameters.AddWithValue("articleId", NpgsqlDbType.Uuid, articleId);
                tagCmd.Parameters.AddWithValue("tag", NpgsqlDbType.Text, tag.Value);
                await tagCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyArticleUpdated(
        NpgsqlDataSource dataSource,
        Guid articleId,
        ArticleEvent.ArticleUpdated updated,
        CancellationToken cancellationToken)
    {
        var setClauses = new List<string> { "updated_at = @updatedAt", "slug = @slug" };
        var parameters = new List<NpgsqlParameter>
        {
            new("id", NpgsqlDbType.Uuid) { Value = articleId },
            new("updatedAt", NpgsqlDbType.TimestampTz) { Value = updated.UpdatedAt.Value },
            new("slug", NpgsqlDbType.Text) { Value = updated.Slug.Value }
        };

        updated.Title.Map(title =>
        {
            setClauses.Add("title = @title");
            parameters.Add(new NpgsqlParameter("title", NpgsqlDbType.Text) { Value = title.Value });
            return title;
        });

        updated.Description.Map(description =>
        {
            setClauses.Add("description = @description");
            parameters.Add(new NpgsqlParameter("description", NpgsqlDbType.Text) { Value = description.Value });
            return description;
        });

        updated.Body.Map(body =>
        {
            setClauses.Add("body = @body");
            parameters.Add(new NpgsqlParameter("body", NpgsqlDbType.Text) { Value = body.Value });
            return body;
        });

        var sql = $"UPDATE articles SET {string.Join(", ", setClauses)} WHERE id = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Update the slug in comments too (for slug-based comment lookups)
        const string commentSlugSql = """
            UPDATE comments SET article_slug = @slug WHERE article_id = @id
            """;
        await using var commentCmd = new NpgsqlCommand(commentSlugSql, connection);
        commentCmd.Parameters.AddWithValue("slug", NpgsqlDbType.Text, updated.Slug.Value);
        commentCmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, articleId);
        await commentCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyArticleDeleted(
        NpgsqlDataSource dataSource,
        Guid articleId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE articles SET deleted = TRUE WHERE id = @id
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, articleId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyCommentAdded(
        NpgsqlDataSource dataSource,
        Guid articleId,
        ArticleEvent.CommentAdded commentAdded,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Look up author and article slug
        var (authorUsername, authorBio, authorImage) = await LookupAuthor(
            connection, commentAdded.AuthorId.Value, cancellationToken).ConfigureAwait(false);

        var articleSlug = await LookupArticleSlug(connection, articleId, cancellationToken)
            .ConfigureAwait(false);

        const string sql = """
            INSERT INTO comments (id, article_id, article_slug, author_id,
                                  author_username, author_bio, author_image,
                                  body, created_at, updated_at, deleted)
            VALUES (@id, @articleId, @articleSlug, @authorId,
                    @authorUsername, @authorBio, @authorImage,
                    @body, @createdAt, @createdAt, FALSE)
            ON CONFLICT (id) DO UPDATE SET
                body = EXCLUDED.body,
                author_username = EXCLUDED.author_username,
                author_bio = EXCLUDED.author_bio,
                author_image = EXCLUDED.author_image
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, commentAdded.CommentId.Value);
        command.Parameters.AddWithValue("articleId", NpgsqlDbType.Uuid, articleId);
        command.Parameters.AddWithValue("articleSlug", NpgsqlDbType.Text, articleSlug);
        command.Parameters.AddWithValue("authorId", NpgsqlDbType.Uuid, commentAdded.AuthorId.Value);
        command.Parameters.AddWithValue("authorUsername", NpgsqlDbType.Text, authorUsername);
        command.Parameters.AddWithValue("authorBio", NpgsqlDbType.Text, authorBio);
        command.Parameters.AddWithValue("authorImage", NpgsqlDbType.Text, authorImage);
        command.Parameters.AddWithValue("body", NpgsqlDbType.Text, commentAdded.Body.Value);
        command.Parameters.AddWithValue("createdAt", NpgsqlDbType.TimestampTz, commentAdded.CreatedAt.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyCommentDeleted(
        NpgsqlDataSource dataSource,
        ArticleEvent.CommentDeleted commentDeleted,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE comments SET deleted = TRUE WHERE id = @id
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, commentDeleted.CommentId.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyArticleFavorited(
        NpgsqlDataSource dataSource,
        Guid articleId,
        ArticleEvent.ArticleFavorited favorited,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        const string insertSql = """
            INSERT INTO favorites (user_id, article_id)
            VALUES (@userId, @articleId)
            ON CONFLICT DO NOTHING
            """;

        await using var insertCmd = new NpgsqlCommand(insertSql, connection, transaction);
        insertCmd.Parameters.AddWithValue("userId", NpgsqlDbType.Uuid, favorited.UserId.Value);
        insertCmd.Parameters.AddWithValue("articleId", NpgsqlDbType.Uuid, articleId);
        var rowsAffected = await insertCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Only increment count if the favorite was actually new
        if (rowsAffected > 0)
        {
            const string updateSql = """
                UPDATE articles SET favorites_count = favorites_count + 1 WHERE id = @articleId
                """;

            await using var updateCmd = new NpgsqlCommand(updateSql, connection, transaction);
            updateCmd.Parameters.AddWithValue("articleId", NpgsqlDbType.Uuid, articleId);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyArticleUnfavorited(
        NpgsqlDataSource dataSource,
        Guid articleId,
        ArticleEvent.ArticleUnfavorited unfavorited,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        const string deleteSql = """
            DELETE FROM favorites WHERE user_id = @userId AND article_id = @articleId
            """;

        await using var deleteCmd = new NpgsqlCommand(deleteSql, connection, transaction);
        deleteCmd.Parameters.AddWithValue("userId", NpgsqlDbType.Uuid, unfavorited.UserId.Value);
        deleteCmd.Parameters.AddWithValue("articleId", NpgsqlDbType.Uuid, articleId);
        var rowsAffected = await deleteCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Only decrement count if the favorite actually existed
        if (rowsAffected > 0)
        {
            const string updateSql = """
                UPDATE articles SET favorites_count = GREATEST(favorites_count - 1, 0) WHERE id = @articleId
                """;

            await using var updateCmd = new NpgsqlCommand(updateSql, connection, transaction);
            updateCmd.Parameters.AddWithValue("articleId", NpgsqlDbType.Uuid, articleId);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────────

    private static async ValueTask<(string Username, string Bio, string Image)> LookupAuthor(
        NpgsqlConnection connection,
        Guid authorId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT username, bio, image FROM users WHERE id = @id
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, authorId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2));
        }

        // Author not yet projected — use empty defaults
        return (string.Empty, string.Empty, string.Empty);
    }

    private static async ValueTask<string> LookupArticleSlug(
        NpgsqlConnection connection,
        Guid articleId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT slug FROM articles WHERE id = @id
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, articleId);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result as string ?? string.Empty;
    }
}
