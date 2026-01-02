# Functional Domain Modeling (DDD) — Agent Instructions

These instructions capture practical functional domain-driven design and adapt them to this repo’s constraints:

* C# (latest features), but **pure functional programming** style (see `csharp.instructions.md`).
* MVU apps exist (e.g., `Abies.Conduit`), but domain modeling principles still apply.
* **No OO-first design**: prefer immutable records + pure functions + explicit types.

---

## What “done” looks like

When new domain logic is added/changed, the result should:

* Use **ubiquitous language** names that match the domain.
* Encode invariants using **constrained types** (illegal states unrepresentable).
* Represent workflows as **functions** with explicit inputs/outputs.
* Make errors explicit using **Result/Option**, not exceptions/null.
* Push side effects to the edge via **capability functions**.

---

## Core principles

### 1) Focus on the domain, not the technology

* You are modeling business **capabilities**, not database tables.
* Prefer domain terms over technical terms (no `Manager`, `Helper`, `Util` in the domain).

### 2) Make illegal states unrepresentable

* Replace primitive obsession with constrained types (smart constructors).
* Model mutually exclusive states as **sum types** (discriminated unions).
* Model optional data as **Option**, not null.

### 3) Make workflows explicit

* A workflow is a function from **Command → (Events | Errors)**.
* Use types to make business rules and decision points obvious.
* Workflows should read like a business narrative.

### 4) Push IO to the edges (functional core, imperative shell)

* Domain functions are pure.
* Effects (time, persistence, external services) are supplied as dependencies.

### 5) Errors are part of the domain

* Expected failures are values.
* Use exceptions only for programmer bugs/unrecoverable infrastructure failures.

---

## Strategic design: bounded contexts and context mapping

Emphasize that **your model is always inside a bounded context**.

Rules:

* Every module/type belongs to one bounded context.
* Don’t share domain types across contexts. Translate.
* If two contexts need to integrate, define a clear boundary and mapping.

Practical guidance for this repo:

* For sample apps like Conduit, treat the app as one context unless explicitly split.
* If splitting, create distinct modules/namespaces per context (e.g., `Profiles`, `Articles`, `Auth`).

### Anti-corruption layer (ACL)

When integrating with an external system:

* Map external DTOs into internal domain types.
* Keep translation logic at the boundary.
* Never let an external schema leak into the internal model.

## Tactical design: modeling with types

### Constrained types (smart constructors)

Use constrained types for domain primitives:

* `EmailAddress`, `Username`, `Password`, `Slug`, `TagName`, `BioText`, etc.

Rules:

* Construction validates invariants.
* If invalid, return error as a value.
* Expose underlying primitive via `.Value`.

Example (pattern only):

```csharp
public readonly record struct EmailAddress(string Value);

public static class EmailAddressModule
{
    public static Result<EmailAddress, DomainError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Fail<EmailAddress, DomainError>(DomainError.Validation("Email is required"))
            : Result.Ok(new EmailAddress(value.Trim()));
}
```

### Value objects

Use `record` / `record struct`:

* Immutable
* Equality by value
* No identity

### Entities

Entities have identity + evolving state.

Rules:

* Use explicit ID types (`UserId`, `ArticleId`) rather than raw `Guid`/`int`.
* Updates return new values (no mutable setters).

Example shape:

```csharp
public readonly record struct UserId(Guid Value);
public record User(UserId Id, Username Username, EmailAddress Email, Bio Bio);
```

### Aggregates

If a set of entities/value objects must be consistent together, model it as an aggregate.

Rules:

* Only the aggregate enforces its invariants.
* External code must not modify internal collections.
* Workflows return updated aggregates and events.

### Domain events

Events are facts in the past tense:

* `UserRegistered`
* `ArticlePublished`
* `ArticleFavorited`

Rules:

* Immutable records
* Don’t embed full aggregates unless required
* Designed for downstream consumers (logging/audit/integration)

---

## Modeling choices: Product, Sum, and “and/or” types

### Product types (“AND”)

Use records for “has-a” relationships.

### Sum types (“OR”)

C# doesn’t have built-in DUs, so emulate with `abstract record` + case records.

Rules:

* Cases must be closed (no random inheritance).
* Use **exhaustive** pattern matching.

```csharp
public abstract record PaymentResult;
public sealed record PaymentApproved : PaymentResult;
public sealed record PaymentDeclined(string Reason) : PaymentResult;
```

Use exhaustive `switch`:

```csharp
var status = result switch
{
    PaymentApproved => OrderStatus.Confirmed,
    PaymentDeclined d => OrderStatus.Cancelled,
    _ => throw new UnreachableException()
};
```

### Option types

Use `Option<T>` (repo has `Abies/Option.cs`) for “might be missing”.

Rules:

* Don’t use null to represent “not found” inside domain/application.
* At API boundaries, map Option to 404/empty response as appropriate.

---

## Workflows: command → events (and errors)

Model behavior as workflows triggered by commands.

### Commands

Commands represent intent:

* `RegisterUser`
* `PublishArticle`
* `FavoriteArticle`

Rules:

* Commands are simple records.
* Validate/convert command fields into constrained domain types early.

### Workflow signature

Prefer signatures that:

* accept explicit capabilities for side effects
* return a single meaningful result type

Example (shape only):

```csharp
public static Result<ArticlePublished, PublishArticleError> PublishArticle(
    CheckSlugUnique checkSlugUnique,
    GetTimeUtc getTime,
    PublishArticleCommand cmd)
{
    // validate -> decide -> return event
}
```

### Railway-Oriented Programming (ROP)

Use `Result<T, TError>` to short-circuit on errors.

Define/consume a generic `Result<TSuccess, TError>` and use combinators like `Bind`, `Map`, and `Match`:

```csharp
public static Result<Order, DomainError> PlaceOrder(Customer customer, Cart cart) =>
    from order in Order.Create(customer, cart)
    from payment in ProcessPayment(order)
    select order;
```

Rules:

* Don’t throw for expected outcomes.
* Keep error cases domain-specific.
* Prefer discriminated error types over stringly errors.

---

## Dependencies as capabilities

Pass dependencies as functions (“capabilities”), not via domain-level service objects.

Examples of capabilities:

* `TryGetUserByEmail : EmailAddress -> Task<Option<User>>`
* `SaveUser : User -> Task<Unit>`
* `HashPassword : Password -> PasswordHash`
* `GetTimeUtc : unit -> DateTimeOffset`

Rules:

* Domain remains pure.
* Application layer wires real implementations.
* For tests, pass fakes.

No `I*` interface naming (repo rule).

## Application layer orchestration

Place orchestration logic here; avoid side effects inside domain functions:

```csharp
public class PlaceOrderUseCase
{
    private readonly OrderRepository orderRepository;

    public Task<Result<OrderId, DomainError>> Execute(PlaceOrderCommand cmd) =>
        Order.Create(cmd.Customer, cmd.Items)
            .Bind(order => OrderValidation.Validate(order))
            .Bind(order => orderRepository.Save(order));
}
```

Guidelines:

* If orchestration needs IO, that belongs here.
* Keep domain rules in domain functions.
* Keep mapping to/from DTOs at the boundary.

---

## Persistence and adapter boundaries

Domain types should not leak into infrastructure; map domain types to DTOs/entities:

```csharp
public static OrderDto ToDto(Order o) => new(...);
public static Order ToDomain(OrderDto dto) => ...;
```

Rules:

* Don’t put JSON/ORM attributes on domain types.
* If persisted data might be invalid, `ToDomain` should return a `Result` (data consistency error).

---

## Testing practices

- Domain logic is **pure**; tests do not require mocks for pure functions.  
- Use property-based tests for invariants across many scenarios.

Recommendations:

* Unit test “smart constructors” heavily (they guard invariants).
* Property test invariants (FsCheck is already used in this repo).
* For workflows, fake capabilities and assert on produced events/errors.

---

## C# feature guidance for functional DDD

Leverage modern C# features:

- Pattern matching (`switch`, `is`) for expressive logic  
- Extension methods for domain query/transform helpers  
- Future union types when available

Example extension:

```csharp
public static class OrderExtensions
{
    public static bool IsEmpty(this Order order) =>
        !order.Lines.Any();
}
```

Additional guidance:

* Prefer `record struct` for tiny value wrappers to reduce allocations.
* Prefer `switch` expressions and pattern matching.
* Keep modules as `static class ...Module` rather than OO services.

---

## Procedural rules for generated code

1. Start with the domain story: capability → workflow → command/event names.
2. Identify bounded context and keep types inside it.
3. Use constrained types to make illegal states unrepresentable.
4. Model state variants with sum types; pattern match exhaustively.
5. Use `Option<T>` for missing values; avoid null.
6. Use `Result<T, TError>` for expected errors; avoid exceptions.
7. Express behavior as workflows: `Command -> Result<Event(s), Error>` (or updated state + events).
8. Pass IO dependencies as capabilities (functions); wire them at the application edge.
9. Keep DTO mapping at the boundaries; domain types are annotation-free.
10. Add tests (at least one happy path + one edge/invariant case).


---

## Notes for MVU in this repo

For MVU apps:

* Treat UI messages (`Msg`) as commands.
* Treat `update` as the application layer/orchestrator.
* Keep domain workflows in separate pure modules and call them from `update`.

## Future enhancements

* Add a small “starter” example module in `docs/` showing command → workflow → event → adapter mapping.
* Add analyzers/tests that enforce “no null in domain”.