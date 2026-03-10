// =============================================================================
// Schema — PostgreSQL DDL for Conduit Read Models
// =============================================================================
// Defines the denormalized table structure for the CQRS read side.
// Tables are designed for the specific query patterns of the Conduit API:
//   - User lookup by email (login)
//   - Profile by username (with following status)
//   - Article listing with filters (tag, author, favorited) and pagination
//   - Feed (articles by followed authors)
//   - Comments by article slug
//   - All tags
//
// Author data is denormalized into article/comment rows for single-query
// API responses. This means user profile updates must cascade to all
// articles and comments by that user (handled by the projections).
// =============================================================================

using System.Runtime.CompilerServices;
using Npgsql;

namespace Picea.Abies.Conduit.ReadStore.PostgreSQL;

/// <summary>
/// PostgreSQL DDL for the Conduit read model tables.
/// </summary>
public static class Schema
{
    /// <summary>
    /// The complete DDL to create all read model tables and indexes.
    /// Idempotent — uses IF NOT EXISTS throughout.
    /// </summary>
    public const string CreateAll = """
        -- Users table
        CREATE TABLE IF NOT EXISTS users (
            id              UUID            PRIMARY KEY,
            email           TEXT            NOT NULL,
            username        TEXT            NOT NULL,
            password_hash   TEXT            NOT NULL,
            bio             TEXT            NOT NULL DEFAULT '',
            image           TEXT            NOT NULL DEFAULT '',
            created_at      TIMESTAMPTZ     NOT NULL,
            updated_at      TIMESTAMPTZ     NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON users (email);
        CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username ON users (username);

        -- Follow relationships
        CREATE TABLE IF NOT EXISTS follows (
            follower_id     UUID            NOT NULL REFERENCES users(id),
            followee_id     UUID            NOT NULL REFERENCES users(id),
            PRIMARY KEY (follower_id, followee_id)
        );

        CREATE INDEX IF NOT EXISTS ix_follows_followee ON follows (followee_id);

        -- Articles with denormalized author data
        CREATE TABLE IF NOT EXISTS articles (
            id              UUID            PRIMARY KEY,
            slug            TEXT            NOT NULL,
            title           TEXT            NOT NULL,
            description     TEXT            NOT NULL,
            body            TEXT            NOT NULL,
            author_id       UUID            NOT NULL REFERENCES users(id),
            author_username TEXT            NOT NULL,
            author_bio      TEXT            NOT NULL DEFAULT '',
            author_image    TEXT            NOT NULL DEFAULT '',
            created_at      TIMESTAMPTZ     NOT NULL,
            updated_at      TIMESTAMPTZ     NOT NULL,
            favorites_count INT             NOT NULL DEFAULT 0,
            deleted         BOOLEAN         NOT NULL DEFAULT FALSE
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ix_articles_slug ON articles (slug) WHERE NOT deleted;
        CREATE INDEX IF NOT EXISTS ix_articles_author ON articles (author_id) WHERE NOT deleted;
        CREATE INDEX IF NOT EXISTS ix_articles_created ON articles (created_at DESC) WHERE NOT deleted;

        -- Article tags (many-to-many)
        CREATE TABLE IF NOT EXISTS article_tags (
            article_id      UUID            NOT NULL REFERENCES articles(id),
            tag             TEXT            NOT NULL,
            PRIMARY KEY (article_id, tag)
        );

        CREATE INDEX IF NOT EXISTS ix_article_tags_tag ON article_tags (tag);

        -- Favorites (user-article relationship)
        CREATE TABLE IF NOT EXISTS favorites (
            user_id         UUID            NOT NULL REFERENCES users(id),
            article_id      UUID            NOT NULL REFERENCES articles(id),
            PRIMARY KEY (user_id, article_id)
        );

        CREATE INDEX IF NOT EXISTS ix_favorites_article ON favorites (article_id);

        -- Comments with denormalized author data
        CREATE TABLE IF NOT EXISTS comments (
            id              UUID            PRIMARY KEY,
            article_id      UUID            NOT NULL REFERENCES articles(id),
            article_slug    TEXT            NOT NULL,
            author_id       UUID            NOT NULL REFERENCES users(id),
            author_username TEXT            NOT NULL,
            author_bio      TEXT            NOT NULL DEFAULT '',
            author_image    TEXT            NOT NULL DEFAULT '',
            body            TEXT            NOT NULL,
            created_at      TIMESTAMPTZ     NOT NULL,
            updated_at      TIMESTAMPTZ     NOT NULL,
            deleted         BOOLEAN         NOT NULL DEFAULT FALSE
        );

        CREATE INDEX IF NOT EXISTS ix_comments_article ON comments (article_id) WHERE NOT deleted;
        CREATE INDEX IF NOT EXISTS ix_comments_article_slug ON comments (article_slug) WHERE NOT deleted;
        """;

    /// <summary>
    /// Ensures all read model tables exist. Idempotent.
    /// </summary>
    /// <param name="dataSource">The Npgsql data source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask EnsureCreated(
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(CreateAll, connection);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
