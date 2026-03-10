// =============================================================================
// User Projection — Projects UserEvents into PostgreSQL Read Models
// =============================================================================
// Handles:
//   Registered      → INSERT into users
//   ProfileUpdated  → UPDATE users + cascade to articles/comments author data
//   Followed        → INSERT into follows
//   Unfollowed      → DELETE from follows
//
// All operations are idempotent (ON CONFLICT DO UPDATE / DO NOTHING).
// =============================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Picea.Abies.Conduit.Domain.User;
using Npgsql;
using NpgsqlTypes;

namespace Picea.Abies.Conduit.ReadStore.PostgreSQL;

/// <summary>
/// Projects <see cref="UserEvent"/> instances into PostgreSQL read model tables.
/// </summary>
public static class UserProjection
{
    /// <summary>
    /// Applies a <see cref="UserEvent"/> to the PostgreSQL read store.
    /// </summary>
    /// <param name="dataSource">The Npgsql data source.</param>
    /// <param name="userId">The aggregate (user) ID that owns this event stream.</param>
    /// <param name="event">The user event to project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask Apply(
        NpgsqlDataSource dataSource,
        Guid userId,
        UserEvent @event,
        CancellationToken cancellationToken = default)
    {
        using var activity = ReadStoreDiagnostics.Source.StartActivity("UserProjection.Apply");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("event.type", @event.GetType().Name);

        switch (@event)
        {
            case UserEvent.Registered registered:
                await ApplyRegistered(dataSource, userId, registered, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case UserEvent.ProfileUpdated profileUpdated:
                await ApplyProfileUpdated(dataSource, userId, profileUpdated, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case UserEvent.Followed followed:
                await ApplyFollowed(dataSource, userId, followed, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case UserEvent.Unfollowed unfollowed:
                await ApplyUnfollowed(dataSource, userId, unfollowed, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static async ValueTask ApplyRegistered(
        NpgsqlDataSource dataSource,
        Guid userId,
        UserEvent.Registered registered,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO users (id, email, username, password_hash, bio, image, created_at, updated_at)
            VALUES (@id, @email, @username, @passwordHash, '', '', @createdAt, @createdAt)
            ON CONFLICT (id) DO UPDATE SET
                email = EXCLUDED.email,
                username = EXCLUDED.username,
                password_hash = EXCLUDED.password_hash,
                created_at = EXCLUDED.created_at,
                updated_at = EXCLUDED.updated_at
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, userId);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Text, registered.Email.Value);
        command.Parameters.AddWithValue("username", NpgsqlDbType.Text, registered.Username.Value);
        command.Parameters.AddWithValue("passwordHash", NpgsqlDbType.Text, registered.PasswordHash.Value);
        command.Parameters.AddWithValue("createdAt", NpgsqlDbType.TimestampTz, registered.CreatedAt.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyProfileUpdated(
        NpgsqlDataSource dataSource,
        Guid userId,
        UserEvent.ProfileUpdated profileUpdated,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Build dynamic UPDATE for users table — only set fields that changed
        var setClauses = new List<string> { "updated_at = @updatedAt" };
        var parameters = new List<NpgsqlParameter>
        {
            new("id", NpgsqlDbType.Uuid) { Value = userId },
            new("updatedAt", NpgsqlDbType.TimestampTz) { Value = profileUpdated.UpdatedAt.Value }
        };

        profileUpdated.Email.Map(email =>
        {
            setClauses.Add("email = @email");
            parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Text) { Value = email.Value });
            return email;
        });

        profileUpdated.Username.Map(username =>
        {
            setClauses.Add("username = @username");
            parameters.Add(new NpgsqlParameter("username", NpgsqlDbType.Text) { Value = username.Value });
            return username;
        });

        profileUpdated.PasswordHash.Map(passwordHash =>
        {
            setClauses.Add("password_hash = @passwordHash");
            parameters.Add(new NpgsqlParameter("passwordHash", NpgsqlDbType.Text) { Value = passwordHash.Value });
            return passwordHash;
        });

        profileUpdated.Bio.Map(bio =>
        {
            setClauses.Add("bio = @bio");
            parameters.Add(new NpgsqlParameter("bio", NpgsqlDbType.Text) { Value = bio.Value });
            return bio;
        });

        profileUpdated.Image.Map(image =>
        {
            setClauses.Add("image = @image");
            parameters.Add(new NpgsqlParameter("image", NpgsqlDbType.Text) { Value = image.Value });
            return image;
        });

        var sql = $"UPDATE users SET {string.Join(", ", setClauses)} WHERE id = @id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Cascade denormalized author data to articles and comments
        await CascadeAuthorUpdate(connection, userId, profileUpdated, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async ValueTask CascadeAuthorUpdate(
        NpgsqlConnection connection,
        Guid userId,
        UserEvent.ProfileUpdated profileUpdated,
        CancellationToken cancellationToken)
    {
        // Only cascade if username, bio, or image changed
        var hasAuthorFieldChanges = false;
        var articleSetClauses = new List<string>();
        var articleParams = new List<NpgsqlParameter>
        {
            new("authorId", NpgsqlDbType.Uuid) { Value = userId }
        };

        profileUpdated.Username.Map(username =>
        {
            articleSetClauses.Add("author_username = @authorUsername");
            articleParams.Add(new NpgsqlParameter("authorUsername", NpgsqlDbType.Text) { Value = username.Value });
            hasAuthorFieldChanges = true;
            return username;
        });

        profileUpdated.Bio.Map(bio =>
        {
            articleSetClauses.Add("author_bio = @authorBio");
            articleParams.Add(new NpgsqlParameter("authorBio", NpgsqlDbType.Text) { Value = bio.Value });
            hasAuthorFieldChanges = true;
            return bio;
        });

        profileUpdated.Image.Map(image =>
        {
            articleSetClauses.Add("author_image = @authorImage");
            articleParams.Add(new NpgsqlParameter("authorImage", NpgsqlDbType.Text) { Value = image.Value });
            hasAuthorFieldChanges = true;
            return image;
        });

        if (!hasAuthorFieldChanges)
            return;

        var setClause = string.Join(", ", articleSetClauses);

        // Update articles
        var articleSql = $"UPDATE articles SET {setClause} WHERE author_id = @authorId";
        await using var articleCmd = new NpgsqlCommand(articleSql, connection);
        articleCmd.Parameters.AddRange(articleParams.ToArray());
        await articleCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Update comments (reuse same parameter values)
        var commentParams = new List<NpgsqlParameter>
        {
            new("authorId", NpgsqlDbType.Uuid) { Value = userId }
        };

        var commentSetClauses = new List<string>();

        profileUpdated.Username.Map(username =>
        {
            commentSetClauses.Add("author_username = @authorUsername");
            commentParams.Add(new NpgsqlParameter("authorUsername", NpgsqlDbType.Text) { Value = username.Value });
            return username;
        });

        profileUpdated.Bio.Map(bio =>
        {
            commentSetClauses.Add("author_bio = @authorBio");
            commentParams.Add(new NpgsqlParameter("authorBio", NpgsqlDbType.Text) { Value = bio.Value });
            return bio;
        });

        profileUpdated.Image.Map(image =>
        {
            commentSetClauses.Add("author_image = @authorImage");
            commentParams.Add(new NpgsqlParameter("authorImage", NpgsqlDbType.Text) { Value = image.Value });
            return image;
        });

        var commentSql = $"UPDATE comments SET {string.Join(", ", commentSetClauses)} WHERE author_id = @authorId";
        await using var commentCmd = new NpgsqlCommand(commentSql, connection);
        commentCmd.Parameters.AddRange(commentParams.ToArray());
        await commentCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyFollowed(
        NpgsqlDataSource dataSource,
        Guid userId,
        UserEvent.Followed followed,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO follows (follower_id, followee_id)
            VALUES (@followerId, @followeeId)
            ON CONFLICT DO NOTHING
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("followerId", NpgsqlDbType.Uuid, userId);
        command.Parameters.AddWithValue("followeeId", NpgsqlDbType.Uuid, followed.FolloweeId.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ApplyUnfollowed(
        NpgsqlDataSource dataSource,
        Guid userId,
        UserEvent.Unfollowed unfollowed,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM follows
            WHERE follower_id = @followerId AND followee_id = @followeeId
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("followerId", NpgsqlDbType.Uuid, userId);
        command.Parameters.AddWithValue("followeeId", NpgsqlDbType.Uuid, unfollowed.FolloweeId.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
