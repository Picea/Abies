---
name: functional-ddd
description: Pattern catalog for pure functional domain modeling in C# 14 / .NET 10 — smart constructors, state machines, sum types, Option, Result, ROP, capability functions, persistence boundaries, ACL, EF Core conventions, Aspire defaults, OTEL spans, TUnit recipes. Use when designing or implementing domain code, reviewing C# changes for functional-DDD compliance, or onboarding to the team's domain modeling style.
---

# Functional Domain-Driven Design in C#

This skill is the deep reference catalog for the team's C# / .NET style. The C# Dev's charter (`.claude/agents/csharp-dev.md`) covers role and protocol. The team's principles live in `.claude/docs/decisions.md`. The toolchain version specifics (project namespace root, paths to `Result<T,E>` / `Option<T>`) live in `.claude/docs/tech-stack.md`. This skill catalogs the *patterns themselves*, with code samples.

When in doubt, prefer simplicity. When the simple approach violates a principle, prefer the principle. When the principle would severely hurt UX or hot-path performance, pause and consult the user per `principles-enforcement.md`.

---

## Constrained Types & Smart Constructors

**Replace primitive obsession with constrained types.** Wrap primitives in records with private constructors and public smart constructor factory methods that validate.

```csharp
public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress, EmailValidationError> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new EmailValidationError.Empty();
        if (!raw.Contains('@'))
            return new EmailValidationError.MissingAtSign();
        if (raw.Length > 254)
            return new EmailValidationError.TooLong(raw.Length);

        return new EmailAddress(raw.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;
}

public abstract record EmailValidationError
{
    public sealed record Empty : EmailValidationError;
    public sealed record MissingAtSign : EmailValidationError;
    public sealed record TooLong(int Length) : EmailValidationError;
}
```

**Rules:**
- The constructor is **private**. There is no public way to create an instance that bypasses validation.
- The smart constructor returns `Result<T, TError>` — never throws on validation failure (validation failure is an expected outcome, not a programmer bug).
- The error type is a closed sum type, so callers know exactly what can go wrong.
- Reviewer flags any direct instantiation that bypasses the smart constructor.

---

## State Machines as Distinct Types

**Never model entity lifecycle state with boolean flags or nullable fields.** Each state is a distinct type carrying only the data valid in that state. Transitions are methods on the source state type that return the next state type. The compiler enforces valid transitions.

```csharp
public sealed record DraftArticle(ArticleId Id, Title Title, Author Author, MarkdownBody Body)
{
    public PublishedArticle Publish(IClock clock) =>
        new(Id, Title, Author, Body, clock.UtcNow);
}

public sealed record PublishedArticle(
    ArticleId Id,
    Title Title,
    Author Author,
    MarkdownBody Body,
    DateTimeOffset PublishedAt)
{
    public ArchivedArticle Archive(IClock clock, ArchiveReason reason) =>
        new(Id, Title, Author, Body, PublishedAt, clock.UtcNow, reason);
}

public sealed record ArchivedArticle(
    ArticleId Id,
    Title Title,
    Author Author,
    MarkdownBody Body,
    DateTimeOffset PublishedAt,
    DateTimeOffset ArchivedAt,
    ArchiveReason Reason);
```

The compiler now prevents you from publishing an already-published article, archiving a draft directly, or accessing `PublishedAt` on a draft.

**Anti-pattern (do not do this):**

```csharp
public sealed record Article(
    ArticleId Id,
    string Status,             // ❌ stringly-typed state
    bool IsPublished,          // ❌ boolean flag
    DateTimeOffset? PublishedAt,  // ❌ nullable field
    DateTimeOffset? ArchivedAt);
```

Reviewer flags both patterns as 🔴 Must Fix.

---

## Sum Types

C# doesn't have native discriminated unions. The team's convention is **sealed abstract record + sealed record cases**, plus pattern matching at the use site.

```csharp
public abstract record DomainEvent
{
    public sealed record ArticleDrafted(ArticleId Id, Author Author) : DomainEvent;
    public sealed record ArticlePublished(ArticleId Id, DateTimeOffset PublishedAt) : DomainEvent;
    public sealed record ArticleArchived(ArticleId Id, ArchiveReason Reason) : DomainEvent;
}

// At the use site, exhaustive pattern matching:
public static string Describe(DomainEvent evt) =>
    evt switch
    {
        DomainEvent.ArticleDrafted d   => $"Drafted by {d.Author}",
        DomainEvent.ArticlePublished p => $"Published at {p.PublishedAt:O}",
        DomainEvent.ArticleArchived a  => $"Archived: {a.Reason}",
        _ => throw new UnreachableException()
    };
```

The compiler does not enforce exhaustiveness over `abstract record` hierarchies — use `_ => throw new UnreachableException()` to fail fast if a new case is added without updating all match sites. Reviewer can flag missing `UnreachableException` defaults.

---

## `Option<T>` over null

**Use `Option<T>` for intentional absence.** Reserve nullable reference types for "the compiler can't prove this isn't null" (e.g., interop, deserialization boundaries).

The project's `Option<T>` lives at the path noted in `.claude/docs/tech-stack.md` — typically `<Root>.Functional.Option`. Standard interface:

```csharp
Option<EmailAddress> maybeEmail = await repo.FindEmailAsync(userId, ct);

return maybeEmail.Match(
    some: email => Results.Ok(email),
    none: () => Results.NotFound());

// Or with an extension method:
return maybeEmail
    .Map(email => email.Value)
    .OrElse("no email on file");
```

**Rules:**
- A repository that returns "not found" returns `Option<T>`, not `T?`.
- A domain function that may have no result returns `Option<T>`.
- Reviewer flags `T?` returns from domain functions where `Option<T>` is the right choice.

---

## `Result<T, TError>` and Railway-Oriented Programming

**Errors are values.** Workflow functions return `Result<T, TError>` where `TError` is a closed domain error sum type. Compose with `Map`, `Bind`, `MapError`.

```csharp
public abstract record PublishArticleError
{
    public sealed record NotFound(ArticleId Id) : PublishArticleError;
    public sealed record AlreadyPublished(ArticleId Id) : PublishArticleError;
    public sealed record AuthorNotAuthorized(AuthorId Author) : PublishArticleError;
    public sealed record Validation(IReadOnlyList<string> Errors) : PublishArticleError;
}

public static class PublishArticleWorkflow
{
    public static async Task<Result<PublishedArticle, PublishArticleError>> Run(
        PublishArticleCommand cmd,
        IArticleRepository repo,
        IClock clock,
        CancellationToken ct)
    {
        var maybeDraft = await repo.FindDraftAsync(cmd.ArticleId, ct);

        return await maybeDraft.Match<Task<Result<PublishedArticle, PublishArticleError>>>(
            none: () => Task.FromResult<Result<PublishedArticle, PublishArticleError>>(
                new PublishArticleError.NotFound(cmd.ArticleId)),
            some: async draft =>
            {
                if (draft.Author.Id != cmd.RequestedBy)
                    return new PublishArticleError.AuthorNotAuthorized(cmd.RequestedBy);

                var published = draft.Publish(clock);
                await repo.SaveAsync(published, ct);
                return published;
            });
    }
}
```

**Rules:**
- Domain workflows never throw for expected business errors — they return `Result.Error(...)`.
- Exceptions are reserved for: programmer bugs (preconditions violated by code, not by user input), unrecoverable infrastructure failures.
- The error type is closed (sum type), so callers know exhaustively what can go wrong.
- Reviewer flags `try/catch` blocks in domain code that handle expected business errors.

---

## Capability Functions (no Service Locator, no DI in the Domain)

**The domain doesn't know about DI.** Effects are passed as functions or interfaces with single methods (capabilities). The application layer wires real implementations.

```csharp
// In the domain: a capability is just a delegate.
public delegate DateTimeOffset Now();

// Or, for things with multiple operations, a single-method interface:
public interface ArticleRepository
{
    Task<Option<DraftArticle>> FindDraftAsync(ArticleId id, CancellationToken ct);
    Task SaveAsync(PublishedArticle article, CancellationToken ct);
}

// Workflow takes capabilities, no IServiceProvider, no static singletons:
public static Task<Result<PublishedArticle, PublishArticleError>> Run(
    PublishArticleCommand cmd,
    ArticleRepository repo,
    Now now,
    CancellationToken ct) { /* ... */ }
```

The application layer wires real capabilities at startup using whatever DI the host provides. Domain code remains pure and testable without DI.

---

## Persistence Boundaries

**Domain types never leak into infrastructure.** No JSON/ORM attributes on domain records. Map to/from a persistence DTO at the boundary.

```csharp
// In the infrastructure layer:
internal sealed class ArticleEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "";   // "draft", "published", "archived"
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchiveReason { get; set; }

    public Result<DraftArticle, MappingError> ToDraft() { /* validates and maps */ }
    public Result<PublishedArticle, MappingError> ToPublished() { /* validates and maps */ }
    public static ArticleEntity FromDraft(DraftArticle a) { /* maps cleanly */ }
    public static ArticleEntity FromPublished(PublishedArticle a) { /* maps cleanly */ }
}
```

**Rules:**
- `ToDomain*` methods return `Result<T, MappingError>` because persisted data may be invalid (introduced by old code, manual DB edits, schema migrations).
- `FromDomain*` methods don't return `Result` — they always succeed because the source is valid by construction.
- Reviewer flags `[Table]`, `[Column]`, `[JsonPropertyName]`, EF `[Key]`, or any infrastructure attribute on a domain record.

---

## Anti-Corruption Layer (ACL)

When integrating with external systems (third-party APIs, legacy systems, message buses you don't own), map external DTOs into internal domain types at the boundary. **Never let an external schema leak into the internal model.**

```csharp
// External DTO (mirrors the external API exactly):
internal sealed class ExternalUserDto
{
    public string user_name { get; set; } = "";
    public string e_mail { get; set; } = "";
    public int status_code { get; set; }   // 1=active, 2=suspended, 3=deleted
}

// ACL maps to internal domain:
internal static class ExternalUserAcl
{
    public static Result<DomainUser, AclError> ToDomain(ExternalUserDto dto)
    {
        var username = Username.Create(dto.user_name);
        var email = EmailAddress.Create(dto.e_mail);
        var status = dto.status_code switch
        {
            1 => UserStatus.Active,
            2 => UserStatus.Suspended,
            3 => UserStatus.Deleted,
            _ => (UserStatus?)null
        };

        if (status is null) return new AclError.UnknownStatus(dto.status_code);
        // chain validations and return either error or domain user...
    }
}
```

The internal codebase only ever sees `DomainUser`. If the external API changes, only the ACL changes.

---

## EF Core Conventions

- **No lazy loading.** Ever. Eager-load with `Include`/`ThenInclude`, or split into multiple queries.
- **Code-first migrations.** No EF Database-First. No `Database.EnsureCreated()` for production.
- **Explicit `IEntityTypeConfiguration<T>`.** No `[Table]`/`[Column]` attributes — even on entity types. Configure everything in dedicated classes.
- **No domain types as entities.** Map domain records to and from persistence-shaped entities at the boundary.
- **No tracking by default for queries.** Use `AsNoTracking()` for reads. Track only when modifying.

```csharp
internal sealed class ArticleEntityConfig : IEntityTypeConfiguration<ArticleEntity>
{
    public void Configure(EntityTypeBuilder<ArticleEntity> b)
    {
        b.ToTable("articles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Status);
    }
}
```

---

## Aspire Defaults

Every runnable service calls `AddServiceDefaults()` in its `Program.cs`. The `*.ServiceDefaults` project provides:
- OpenTelemetry tracing, metrics, logging with OTLP exporter
- Health checks at `/health` and `/alive`
- Service discovery
- HTTP resilience (retry, circuit breaker, timeout)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();   // ← always

builder.Services.AddArticlesModule(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();      // ← health endpoints

app.MapArticlesEndpoints();
app.Run();
```

The Aspire AppHost orchestrates the services for local dev and tests. Reviewer flags any service missing `AddServiceDefaults()`.

---

## OTEL Instrumentation

**Every functional flow has full trace coverage.** The Aspire dashboard shows the request path from ingress through every backend hop. Custom spans on workflow entry points.

```csharp
internal static class ArticleTelemetry
{
    public static readonly ActivitySource Source = new("<Root>.Articles");
}

public static async Task<Result<PublishedArticle, PublishArticleError>> Run(
    PublishArticleCommand cmd,
    ArticleRepository repo,
    Now now,
    CancellationToken ct)
{
    using var activity = ArticleTelemetry.Source.StartActivity("PublishArticle");
    activity?.SetTag("article.id", cmd.ArticleId.Value);
    activity?.SetTag("requested.by", cmd.RequestedBy.Value);

    var result = await DoTheWork(cmd, repo, now, ct);

    if (result.IsError)
    {
        activity?.SetStatus(ActivityStatusCode.Error, result.Error.GetType().Name);
        activity?.AddTag("error.kind", result.Error.GetType().Name);
    }

    return result;
}
```

Register the `ActivitySource` in `ServiceDefaults`:

```csharp
.WithTracing(tracing => tracing
    .AddSource("<Root>.Articles")
    .AddSource("<Root>.Authentication")
    // ... one per bounded context
)
```

---

## Testing — TUnit + Aspire

### Unit tests (pure domain)

```csharp
public class PublishArticleWorkflowTests
{
    [Test]
    public async Task Returns_AlreadyPublished_when_article_is_published()
    {
        var alreadyPublished = ArticleFixtures.Published();
        var repo = FakeArticleRepository.With(alreadyPublished);
        var clock = FakeClock.At(DateTimeOffset.Parse("2026-01-15T00:00:00Z"));

        var result = await PublishArticleWorkflow.Run(
            new PublishArticleCommand(alreadyPublished.Id, alreadyPublished.Author.Id),
            repo, clock, CancellationToken.None);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.Error).IsTypeOf<PublishArticleError.AlreadyPublished>();
    }
}
```

### Integration / E2E tests (through Aspire AppHost)

```csharp
public class ArticlePublishingE2ETests
{
    [Test]
    public async Task User_can_publish_an_owned_draft()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var http = app.CreateHttpClient("articles-api");
        await app.WaitForStatusAsync("articles-api", KnownResourceStates.Running);

        var draft = await CreateDraftAsync(http);
        var publishResponse = await http.PostAsJsonAsync($"/articles/{draft.Id}/publish", new { });

        await Assert.That(publishResponse.IsSuccessStatusCode).IsTrue();
    }
}
```

**Rules:**
- TUnit only. No xUnit, NUnit, MSTest.
- Integration / E2E through Aspire `DistributedApplicationTestingBuilder` by default — never `WebApplicationFactory`, never Testcontainers, unless the project's `.claude/docs/decisions.md` documents a specific, closed exception for pre-existing fixtures. That file is the authoritative source for this project's actual convention; this skill states the generic pattern.
- No `// Arrange`, `// Act`, `// Assert` comments.
- E2E tests for every user journey. TUnit + Playwright via TUnit.Playwright for browser-driven scenarios.
- Property-based tests via FsCheck for invariants (e.g., "any valid `EmailAddress.Create` round-trips through serialization").

---

## What This Skill Does NOT Cover

- **Code review patterns** (independent reviewer-side checks) — `code-review` skill.
- **Performance benchmarking** — `performance-engineering` skill.
- **Security toolchain** — `security-toolchain` skill.
- **Browser-side JS** — `vanilla-js-playbook` skill.
- **CI/CD packaging** — `cicd-pipelines` skill.

If a pattern here conflicts with `principles-enforcement.md` or `decisions.md`, the squad's authoritative docs win.
