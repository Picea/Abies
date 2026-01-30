# ADR-012: Test Strategy

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Testing web applications traditionally requires complex infrastructure:

- Browser automation for UI tests
- Mocking HTTP services
- Managing async behavior
- Test isolation and cleanup

Abies's architecture—with pure functions, immutable state, and explicit side effects—creates opportunities for simpler, faster testing. We needed a test strategy that:

1. Leverages the purity of Update and View functions
2. Tests side effects at appropriate boundaries
3. Provides confidence without excessive mocking
4. Supports both unit and integration testing

## Decision

We adopt a **layered test strategy** with three distinct levels:

### Level 1: Unit Tests (Pure Function Tests)

Test `Update`, `View`, routing, and domain logic as pure functions:

```csharp
[Fact]
public void Update_Increment_IncreasesCount()
{
    var model = new Model(Count: 0);
    var (newModel, command) = Program.Update(new Increment(), model);
    
    Assert.Equal(1, newModel.Count);
    Assert.IsType<Command.None>(command);
}

[Fact]
public void Route_Profile_ParsesUsername()
{
    var result = Route.Match.Parse("/profile/alice");
    
    Assert.True(result.Success);
    Assert.IsType<Route.Profile>(result.Value);
    Assert.Equal("alice", ((Route.Profile)result.Value).UserName.Value);
}
```

Characteristics:
- No mocking required
- Extremely fast (thousands per second)
- Run in `Abies.Tests` project
- Test the "what" of business logic

### Level 2: Integration Tests (Component/DOM Tests)

Test UI components and DOM rendering with fake services:

```csharp
[Fact]
public async Task HomePage_LoadsArticles()
{
    // Arrange: fake HTTP responses
    var fakeHttp = new FakeHttpMessageHandler();
    fakeHttp.AddResponse("/api/articles", ArticleFixtures.SampleArticles);
    
    // Act: simulate application flow
    var (model, _) = Program.Initialize(Url.Create("/"), new Arguments());
    await Program.HandleCommand(new LoadArticlesCommand(), Dispatch);
    
    // Assert: verify model state
    Assert.Equal(3, ((Page.Home)model.Page).Model.Articles.Count);
}
```

Characteristics:
- Fake HTTP clients inject test data
- Tests command handling and state transitions
- Run in `Abies.Conduit.IntegrationTests` project
- No browser required

### Level 3: End-to-End Tests (Browser Tests)

Test full user journeys with Playwright:

```csharp
[Fact]
public async Task User_CanLogin_AndViewProfile()
{
    await Page.GotoAsync("/");
    await Page.ClickAsync("text=Sign in");
    await Page.FillAsync("[data-testid=email]", "test@example.com");
    await Page.FillAsync("[data-testid=password]", "password");
    await Page.ClickAsync("text=Sign in");
    
    await Expect(Page.Locator("text=Your Feed")).ToBeVisibleAsync();
}
```

Characteristics:
- Real browser (headless by default)
- Tests real server + real frontend
- Run in `Abies.Conduit.E2E` project
- Slowest but highest confidence

## Test Distribution Pyramid

```
         ┌─────────┐
         │  E2E    │  ~10% - Critical user journeys
         ├─────────┤
         │  Integ  │  ~30% - Component integration
         ├─────────┤
         │  Unit   │  ~60% - Pure function logic
         └─────────┘
```

## Consequences

### Positive

- **Fast feedback**: Unit tests run instantly; most bugs caught early
- **Minimal mocking**: Pure functions don't need mocks
- **Focused tests**: Each level tests appropriate concerns
- **Deterministic**: No flaky tests from async timing (at unit level)
- **Refactoring confidence**: Comprehensive coverage enables safe changes

### Negative

- **Three test projects**: More project structure to maintain
- **E2E infrastructure**: Browser tests need Playwright setup
- **Test doubles for services**: Integration tests need fake implementations
- **Coverage gaps**: Boundary between levels can miss edge cases

### Neutral

- BenchmarkDotNet for performance-critical code (separate from correctness tests)
- Property-based testing (FsCheck) for invariants when appropriate
- Test naming follows `Method_Scenario_ExpectedResult` convention

## Alternatives Considered

### Alternative 1: E2E Only

Test everything through the browser:

- Highest realism
- Extremely slow
- Flaky (network, timing)
- Hard to test edge cases

Rejected because it's too slow for TDD.

### Alternative 2: Mocking Framework Heavy

Mock all dependencies at every level:

- Familiar approach
- Mock setup overhead
- Tests coupled to implementation
- Mocks can lie about behavior

Rejected because pure functions make this unnecessary.

### Alternative 3: Snapshot Testing

Capture UI output and compare:

- Good for regression detection
- Hard to maintain (snapshot churn)
- Doesn't verify behavior
- Large binary diffs

Not rejected—can supplement—but not primary strategy.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)
- [ADR-006: Command Pattern for Side Effects](./ADR-006-command-pattern.md)

## References

- [The Testing Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [FsCheck Property-Based Testing](https://fscheck.github.io/FsCheck/)
- [`Abies.Tests/`](../../Abies.Tests/) - Unit tests
- [`Abies.Conduit.IntegrationTests/`](../../Abies.Conduit.IntegrationTests/) - Integration tests
- [`Abies.Conduit.E2E/`](../../Abies.Conduit.E2E/) - End-to-end tests
