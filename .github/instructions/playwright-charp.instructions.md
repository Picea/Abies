
---
description: "Playwright test generation and quality guidelines for C# (with rationale)"
applyTo: '**'
---

# Playwright C# Test Generation Instructions

This document defines **authoritative guidelines** for generating, reviewing, and maintaining **Playwright end-to-end (E2E) tests in C#**.  
Each instruction includes an explicit **motivation** to clarify *why* the rule exists and to help AI tools, reviewers, and developers make consistent trade-offs.

The goal is to produce tests that are:
- Deterministic and non-flaky
- Fast enough to run on every pull request
- Maintainable over long product lifetimes
- Aligned with real user behavior, without abusing the UI for setup

---

## 1. Core Principles

### 1.1 Prefer Determinism Over Realism
**Instruction**
- Use the UI only for *what is being validated*.
- Perform setup and teardown via APIs, database seeding, or fixtures.

**Motivation**
UI-driven setup introduces unnecessary flakiness, increases execution time, and couples tests to irrelevant workflows. Deterministic setup produces faster feedback and more trustworthy failures.


### 1.2 Tests Must Be Isolated and Order-Independent
**Instruction**
- Every test must be able to run in isolation.
- Tests must not rely on data or side effects from other tests.

**Motivation**
Order-dependent tests fail unpredictably in parallel execution and CI environments. Isolation is a prerequisite for reliable automation at scale.


### 1.3 Favor Accessibility-Based Locators
**Instruction**
- Use `GetByRole`, `GetByLabel`, `GetByText` wherever possible.
- Avoid brittle CSS or XPath selectors unless absolutely required.

**Motivation**
Accessibility-based locators are more stable over time and encourage accessible UI design. They also reflect how real users interact with the application.

---

## 2. Technology Stack Assumptions

- **Playwright for .NET**
- **xUnit or NUnit** for test execution
- **FluentAssertions** for expressive assertions (optional but recommended)
- **.NET 8+**

---

## 3. Project & Folder Structure

```
/tests
  /E2E
    /Fixtures
    /Seed
    LoginTests.cs
    SearchTests.cs
```

### 3.1 Naming Conventions
**Instruction**
- Test files: `<Feature>Tests.cs`
- Test methods: `<Action>_<Condition>_<ExpectedResult>`

**Motivation**
Explicit naming improves readability, test reporting, and failure diagnostics in CI systems.

---

## 4. Test Structure Guidelines

### 4.1 Async-First Design
**Instruction**
- All Playwright interactions must be awaited.
- Never block on async calls (`.Result`, `.Wait()`).

**Motivation**
Blocking async code can deadlock test runners and introduces nondeterministic timing behavior.


### 4.2 Centralized Browser & Context Management
**Instruction**
- Use fixtures (`IAsyncLifetime`, NUnit `[SetUpFixture]`) to manage:
  - Playwright instance
  - Browser
  - Browser context

**Motivation**
Centralization reduces duplication and ensures consistent configuration (timeouts, locale, storage state).

---

## 5. Locator Strategy

### 5.1 Preferred Locator Order
**Instruction**
1. `GetByRole`
2. `GetByLabel`
3. `GetByText`
4. `GetByTestId`
5. CSS selectors (last resort)

**Motivation**
This hierarchy maximizes resilience against UI refactors and visual redesigns.

---

## 6. Assertions

### 6.1 Use Auto-Retry Assertions
**Instruction**
- Prefer Playwright expectations (`Expect(locator).ToBeVisibleAsync()`).
- Avoid manual polling or fixed delays.

**Motivation**
Auto-retry assertions reduce flakiness by synchronizing with the browser’s actual state.


### 6.2 Assert User-Visible Outcomes Only
**Instruction**
- Assert what the user can see or experience.
- Avoid asserting implementation details (DOM structure, internal IDs).

**Motivation**
User-centric assertions are more stable and reflect real product requirements.

---

## 7. Data Seeding & Test Setup (Strongly Recommended)

### 7.1 API-Based Seeding (Preferred)
**Instruction**
- Seed test data via dedicated test APIs or internal endpoints.
- Perform seeding in fixtures or `[SetUp]` methods.

```csharp
await apiClient.PostAsync("/test/seed/user", payload);
```

**Motivation**
API seeding is fast, deterministic, and independent of UI workflows.


### 7.2 Database Reset / Snapshot Restore
**Instruction**
- Reset the database to a known baseline before each test or test class.
- Use one of:
  - Transaction rollback
  - Truncate + seed
  - Snapshot restore

**Motivation**
A known database state eliminates cross-test contamination and simplifies debugging.


### 7.3 Hybrid API + UI Testing
**Instruction**
- Use APIs to create state.
- Use UI to validate behavior.

**Motivation**
This preserves E2E confidence while keeping tests fast and focused.

---

## 8. Example Test (C# + xUnit)

```csharp
public class LoginTests : IAsyncLifetime
{
    private IPage _page;

    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync();
        _page = await context.NewPageAsync();

        await TestSeeder.SeedUserAsync("user@test.com", "password");
        await _page.GotoAsync("/login");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_ShouldNavigateToDashboard()
    {
        await _page.GetByLabel("Email").FillAsync("user@test.com");
        await _page.GetByLabel("Password").FillAsync("password");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

        await Expect(_page).ToHaveURLAsync("/dashboard");
    }
}
```

---

## 9. Tracing, Debugging & Diagnostics

### 9.1 Enable Tracing on Failure
**Instruction**
- Capture traces, screenshots, and videos on failure.

**Motivation**
Rich diagnostics significantly reduce time-to-resolution for flaky or environment-specific failures.

---

## 10. CI/CD Execution Strategy

**Instruction**
- Run E2E tests in parallel where possible.
- Tag tests (`[Trait("Category", "E2E")]`).
- Fail fast on infrastructure errors.

**Motivation**
E2E tests should act as a quality gate, not a bottleneck.

---

## 11. Quality Checklist (Pre-Merge)

- [ ] No UI-driven setup for test data
- [ ] Deterministic seeding or database reset
- [ ] Stable, accessible locators
- [ ] Clear test names
- [ ] Assertions reflect user expectations
- [ ] Tests pass in parallel execution

---

## 12. Non-Goals

- This document does **not** define unit or integration testing strategy.
- This document does **not** mandate a specific database or backend architecture.

---

**End of file**
