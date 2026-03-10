# Picea.Abies.Conduit.ReadStore.PostgreSQL

PostgreSQL read store for the Conduit RealWorld application — the query side of CQRS.

## Architecture

This project implements the **read side** of a CQRS architecture:

- **Write side**: Domain events are persisted to KurrentDB (EventStoreDB) via `Picea.Glauca.EventSourcing.KurrentDB`
- **Read side** (this project): Events are projected into denormalized PostgreSQL tables optimized for the Conduit API query patterns

```
KurrentDB (events) ─► Projections ─► PostgreSQL (read models) ─► Query API
```

## Components

### Schema

DDL for the denormalized read model tables:

| Table | Purpose |
|---|---|
| `users` | User profiles (email, username, bio, image) |
| `follows` | Follow relationships between users |
| `articles` | Articles with denormalized author data |
| `article_tags` | Tags per article (many-to-many) |
| `favorites` | User-article favorite relationships |
| `comments` | Comments with denormalized author data |

### Projections

Event handlers that transform domain events into SQL upserts:

- `UserProjection` — Handles `UserEvent.Registered`, `UserEvent.ProfileUpdated`, `UserEvent.Followed`, `UserEvent.Unfollowed`
- `ArticleProjection` — Handles all `ArticleEvent` variants

### Query Store

Raw SQL implementations of the query capability delegates defined in `Picea.Abies.Conduit.ReadModel.Queries`:

- `FindUserByEmail`, `FindUserById` — User lookups
- `GetProfile` — Profile with following status
- `ListArticles`, `GetFeed` — Article listing with filters and pagination
- `FindArticleBySlug` — Single article lookup
- `GetComments` — Comments for an article
- `GetTags` — All unique tags

## Design Decisions

- **Raw Npgsql** — No ORM. SQL is explicit, parameterized, and auditable.
- **Denormalized read models** — Author data is copied into article/comment rows. Updated when user profiles change. Trades storage for query speed.
- **Soft deletes** — Articles and comments are marked as deleted, not removed. Simplifies projection idempotency.
- **OpenTelemetry** — All database operations are traced via `ActivitySource`.

## Dependencies

- `Npgsql` — PostgreSQL .NET driver
- `Picea.Abies.Conduit` — Read model types and query capability delegates
- `Picea.Glauca` — EventStore, StoredEvent, Projection abstractions
