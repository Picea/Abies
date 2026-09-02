---
name: spec-by-example
description: The squad's executable-specification practice — Spec-by-Example tests are drafted by `spec-author` before implementation, approved by the user, and immutable during implementation. All specs use TUnit. Use when a feature needs a contract test, when planning new behavior, when reviewing whether a feature change has the required spec, or before challenging an approved spec on grounds of being unimplementable.
---

# Spec-by-Example

The squad practices **Specification by Example** (Adzic, 2011) — also called executable specifications, BDD when behaviour-driven, ATDD when acceptance-test-driven. The vocabulary varies; the discipline is what matters.

A **Spec-by-Example test** is:

1. **Written before implementation.** It captures the agreed behaviour, not what the code happens to do.
2. **Drafted by `spec-author`, approved by the user.** `spec-author` holds the Test Strategy Room, chooses the level, and drafts the candidate. The **user** approves — their recognition of the feature in the test is what makes it a *spec* rather than an aspirational test draft. A spec the user cannot read at a glance has failed at its only job.
3. **Concrete.** Specific examples of inputs and outputs, not abstract rules. "When user U with verified email submits draft article A, A's state transitions to PendingReview" — not "the system handles article submission."
4. **Domain-language.** Reads as a business narrative. A non-developer who understands the domain should be able to read the spec and say "yes, that's what we want" or "no, that's wrong."
5. **Immutable during implementation.** csharp-dev does not modify approved specs to make implementation easier. If implementation reveals the spec is wrong, work stops, csharp-dev surfaces the conflict, `spec-author` produces an updated spec, the user re-approves, then implementation resumes.
6. **Executable.** Runs as part of the test suite. Pass = feature done. Fail = feature incomplete.
7. **The documentation.** Once a spec exists, the test file is the canonical statement of what the feature does. README descriptions and design docs become commentary; the spec is the source of truth.

This is more disciplined than canonical TDD. In TDD the developer writes their own failing test. In Spec-by-Example the test is a **hand-off contract** between the specifying side (`spec-author` drafting, the user approving) and the implementer who satisfies it. Renegotiating mid-implementation is a process violation, not a normal flexibility.

## When a spec is required

| Change | Spec-by-Example required? | Rule |
|---|---|---|
| New feature (user-visible behaviour) | **Yes** | csharp-dev refuses to start without one (Stop condition) |
| Behaviour change to existing feature | **Yes** | New spec or amendment to existing spec, user-approved |
| Bug fix | **No spec; regression test required** | Different rule — the test reproduces the bug, doesn't pre-specify behaviour |
| Refactor (no behaviour change) | **No** | Existing specs must continue to pass; that's the safety net |
| Performance optimisation (no behaviour change) | **No** | Same as refactor; perf is measured separately by `performance-engineer` |
| Pure infrastructure (CI, hooks, build config) | **No** | Specs cover the system, not the toolchain |
| Documentation-only changes | **No** | |

If you're unsure whether a change qualifies as "behaviour change," err on the side of writing a spec. The reviewer flags missing specs as 🔴 Must Fix.

## What a spec is *not*

- **Not a unit test.** Unit tests cover internals; specs cover observable behaviour. A spec that asserts on private fields or internal state has been written at the wrong level.
- **Not a regression test.** Regression tests come from bugs; specs come from features. They live in different test projects (see Layout below).
- **Not the only test.** Specs are necessary, not sufficient. Properties tests, fuzz tests, performance tests, security regression tests all exist alongside specs and have their own roles.
- **Not negotiable mid-implementation.** This is the bit that takes discipline. When implementation reveals the spec is wrong, the natural urge is to "just tweak the test a tiny bit." Don't. Stop, surface, re-approve.

## Form: TUnit only

**The squad uses TUnit for all specs.** Plain TUnit tests with descriptive method names *are* executable specifications when written with the discipline below. Do not introduce SpecFlow, Reqnroll, Gherkin, or any other behaviour-driven testing framework — the squad has standardised on a single test stack and the maintenance/onboarding overhead of a second one isn't justified.

```csharp
// tests/YourProduct.Specs/PublicationLifecycle.cs
namespace YourProduct.Specs;

using YourProduct.Domain;

[Spec("Article publication lifecycle")]
public class PublicationLifecycle
{
    [Test]
    public async Task Verified_user_submitting_draft_advances_to_pending_review()
    {
        // Given
        var user = User.Verified(UserId.New(), Email.From("u@example.com"));
        var draft = Article.Draft(ArticleId.New(), user.Id, "First post", "...body...");

        // When
        var submitted = draft.Submit(at: DateTimeOffset.Parse("2026-05-06T10:00:00Z"));

        // Then
        await Assert.That(submitted).IsType<PendingReview>();
        await Assert.That(submitted.SubmittedAt).IsEqualTo(DateTimeOffset.Parse("2026-05-06T10:00:00Z"));
    }

    [Test]
    public async Task Unverified_user_cannot_submit_draft()
    {
        // Given
        var user = User.Unverified(UserId.New(), Email.From("u@example.com"));
        var draft = Article.Draft(ArticleId.New(), user.Id, "First post", "...body...");

        // When
        var result = draft.SubmitAs(user, at: DateTimeOffset.UtcNow);

        // Then
        await Assert.That(result).IsType<Result<Article, SubmitError>.Failure>();
        await Assert.That(result.Error).IsEqualTo(SubmitError.UserNotVerified);
    }
}
```

What makes this a spec, not just a test:

- **Method names read as domain assertions.** `Verified_user_submitting_draft_advances_to_pending_review` is a sentence `spec-author` writes and the user agrees with. Underscores between words (rather than camelCase) are deliberate — they make method names readable as English at a glance, which matters more in a spec than in a unit test.
- **Given/When/Then structure** is in comments. The *practice* of the three-phase test is preserved; no framework structure is enforced. If you find yourself wanting more enforcement than this, the discipline is missing — not the tooling.
- **No mocking of domain primitives.** The spec exercises real domain types. Mocking comes in for IO at the edges only.
- **One assertion per concept**, not "one assertion per method." A spec can assert several things in the Then phase if they're aspects of the same behaviour ("the article transitioned to PendingReview AND the timestamp was recorded").
- **`[Spec]` attribute** is a custom attribute (a 5-line `Attribute : Attribute` class with a string description) that the reviewer-agent searches for to verify spec coverage. Plain `[Test]` works equally well; the attribute exists for the reviewer's grep, not for execution.

### The `[Spec]` attribute (recommended)

```csharp
// tests/SpecSupport/SpecAttribute.cs
namespace SpecSupport;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SpecAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}
```

Apply it to the *class* containing related scenarios, not to individual `[Test]` methods. The class is a feature; the methods are scenarios within it.

### Naming conventions for spec methods

Method names should read as full sentences. Use underscores. Aim for active voice with a clear subject, action, and outcome:

✅ `Verified_user_submitting_draft_advances_to_pending_review`
✅ `Submission_after_deadline_is_rejected_with_DeadlineExceeded`
✅ `Cancelling_a_pending_submission_returns_article_to_Draft`

❌ `TestSubmit` — meaningless without context
❌ `Should_work` — what should work? Under what conditions?
❌ `SubmitArticleTest` — describes the method called, not the behaviour asserted

### When a scenario has multiple variants

Use TUnit's `[Arguments]` for data-driven scenarios where the same behaviour applies to several inputs:

```csharp
[Test]
[Arguments("u@example.com", true)]
[Arguments("U@Example.COM", true)]      // case insensitive
[Arguments("  u@example.com  ", true)]  // whitespace tolerated
[Arguments("not-an-email", false)]
[Arguments("", false)]
public async Task Email_validation_accepts_valid_addresses_and_rejects_invalid_ones(string input, bool expected)
{
    var result = Email.TryParse(input);
    await Assert.That(result.IsValid).IsEqualTo(expected);
}
```

For scenarios that vary in *behaviour* rather than just *input* (e.g., "verified user can submit, unverified user cannot"), prefer separate methods with clear names over one parameterised method — the difference between cases is informative, not noise.

## Layout

Specs live in their own test project, separate from unit tests, regression tests, integration tests, and benchmarks:

```
tests/
├── YourProduct.Specs/                  # Spec-by-Example (this skill)
│   ├── YourProduct.Specs.csproj
│   ├── PublicationLifecycle.cs         # Spec scenarios
│   └── SubmissionRules.cs
├── YourProduct.UnitTests/              # Internal-implementation tests
├── YourProduct.Regression/             # Bug-reproduction tests
├── YourProduct.Integration/            # Below-AppHost integration tests
├── YourProduct.E2E.Specs/              # End-to-end specs via Aspire AppHost
└── YourProduct.Benchmarks/             # BenchmarkDotNet
```

Why a separate project:

- The reviewer's "does this PR include a spec?" check becomes "is there a new file under `*/Specs/`?" — much easier than parsing across mixed test files.
- Spec tests are slower than unit tests on average (they exercise more of the system). Running them separately lets the dev loop stay fast.
- The approval surface is well-defined: "I approved every file in `Specs/` as of commit `<sha>`."

## End-to-end specs run through the Aspire AppHost

If a spec exercises a request flow that crosses service boundaries — HTTP from one service hitting an endpoint on another, or a workflow that spans multiple bounded contexts — it must start the system through `Aspire.Hosting.Testing.DistributedApplicationTestingBuilder`. This is the squad's E2E rule, narrowed from "all integration tests" to "tests that genuinely span the distributed system."

```csharp
// tests/YourProduct.E2E.Specs/ArticleSubmissionEndToEnd.cs
using Aspire.Hosting;
using Aspire.Hosting.Testing;

[Spec("Article submission end-to-end")]
public class ArticleSubmissionEndToEnd
{
    [Test]
    public async Task Submitted_article_appears_in_editorial_queue()
    {
        // Given — Aspire AppHost running articles + editorial + db
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.YourProduct_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var articlesClient = app.CreateHttpClient("articles");
        var editorialClient = app.CreateHttpClient("editorial");

        // When — POST through the public articles API
        var submitResponse = await articlesClient.PostAsJsonAsync("/articles/submit", new {
            authorId = "alice",
            title = "First post",
            body = "..."
        });
        await Assert.That(submitResponse.StatusCode).IsEqualTo(HttpStatusCode.Accepted);

        // Then — appears on editorial side after eventually-consistent propagation
        await Eventually(async () => {
            var queue = await editorialClient.GetFromJsonAsync<EditorialQueueItem[]>("/queue");
            return queue?.Any(item => item.Title == "First post") ?? false;
        }, timeout: TimeSpan.FromSeconds(10));
    }
}
```

Tests below this level — testing one service in isolation, testing a repository against a TestContainers Postgres, testing a single domain workflow — are scoped narrower and do *not* go through the AppHost. They are encouraged. The AppHost rule applies only to the *cross-service* tier.

## The approval workflow

A spec is "approved" when:

1. The **user** has explicitly signed off, in writing, on the spec content that `spec-author` drafted. (A human acting as the design authority can sign off directly.)
2. The sign-off is captured in a decision artefact at `.squad/decisions/<id>.md` for non-trivial specs — written by the `architect` at design-pass close-out — or in the PR description for routine ones. The full draft lives at `.squad/design/<slug>/06-spec.md`.
3. The spec is committed to the repo with a `feat(spec): ...` or `chore(spec): ...` commit message.

What "non-trivial" means for the decision-artefact requirement:

- The spec encodes a **policy** (auth rules, who-can-do-what, retention timelines, financial thresholds) — always non-trivial.
- The spec is the **first** spec in a new bounded context — non-trivial.
- The spec contradicts or replaces an existing spec — non-trivial.
- The spec adds a behaviour to an established context that's continuous with existing specs — trivial; PR description is enough.

The reviewer agent checks this. Specs without a clear approval trail are 🔴 Must Fix.

## Re-approval protocol when implementation reveals a spec problem

The hardest part of Spec-by-Example is the discipline around mid-implementation discovery. csharp-dev *will* sometimes find that an approved spec is wrong — incomplete, ambiguous, or impossible. The protocol:

1. **Stop implementing.** Do not push code that makes the spec pass through reinterpretation.
2. **Document the conflict** in a comment on the spec test (or as a TODO with `// SPEC CONFLICT:` prefix, which the reviewer agent grep-searches for).
3. **Hand back explicitly** — to the orchestrator, which re-spawns `spec-author`: "I cannot satisfy this spec because <X>. Here are the options: <A>, <B>, <C>. Recommend <A> because <reason>."
4. **`spec-author` produces an updated spec and the user re-approves.** New approval trail. New decision artefact if the change is structural.
5. **csharp-dev resumes** with the new spec.

What's forbidden:

- Editing the spec to "make it match what the code does" (the test passes, but the contract has been silently re-negotiated).
- Adding `[Skip]` or `Ignore` to a problematic spec to unblock other work.
- Implementing the spec literally even when csharp-dev knows it produces wrong behaviour.

The reviewer agent enforces this with a git-history check: spec files modified in the same PR that adds them to passing status are flagged. The spec's approval and csharp-dev's implementation should be in **separate commits**, ideally separate PRs.

## Common mistakes

**Specs at the wrong level.** A spec that asserts "the `Submit` method is called with the right argument" is testing implementation, not behaviour. Re-write to assert on the observable outcome (state change, side effect, return value).

**Specs that test the framework.** "When I send `Content-Type: application/json`, the server parses the body as JSON." That's ASP.NET, not your domain. Skip.

**One mega-spec covering everything.** A scenario that exercises 12 steps and 47 assertions across user signup, draft creation, submission, review, and publication is brittle and uninformative. Decompose into discrete specs each covering one decision point.

**Specs without examples.** "When a user submits an article, it goes to review" — versus — "When alice@example.com submits an article titled 'First post' at 10:00 UTC, it appears in editorial-queue with status PendingReview at 10:00 UTC, and an event of type ArticleSubmitted is published." The second is concrete; the first is aspiration.

**Spec drift via "the test was wrong."** Every modification to an approved spec must have a fresh approval trail. If the change is "fixing a typo in a string literal," that's still a modification — commit it as `chore(spec): fix typo in ArticleSubmissionEndToEnd` with the approver tagged.

**Mocking domain primitives.** If a spec mocks `Article` or `User`, it's no longer a spec — it's a test of the test setup. Use real domain primitives; mock IO only.

**Specs that pass for the wrong reason.** A spec that reads "the article transitions to PendingReview" passes if your code returns `null` and the assertion happens to be lenient. Always include a guard: `await Assert.That(result).IsType<PendingReview>()` before drilling into properties.

**Method names that read as test names rather than scenarios.** `TestArticleSubmission` describes the method; `Submitted_article_advances_to_pending_review` describes the behaviour. The reviewer flags spec method names that read as test-scaffolding rather than scenario names.

## Tools and references

- **TUnit**: https://github.com/thomhurst/TUnit — the squad's only test framework. All specs use it.
- **Aspire.Hosting.Testing**: https://learn.microsoft.com/en-us/dotnet/aspire/testing — for E2E specs that span services.
- **Gojko Adzic, *Specification by Example* (2011)**: the canonical reference. Practice is tool-agnostic; the squad's choice of TUnit doesn't change the discipline.
- **Dan North on BDD (2006)**: "Introducing BDD" — the original article that named the practice. Useful background; the squad doesn't use BDD frameworks.

## Cross-references in this framework

- `csharp-dev` agent's Stop conditions (`.claude/agents/csharp-dev.md`): refusing to implement features without an approved spec.
- `reviewer` agent's checklist (`.claude/agents/reviewer.md`): verifying spec presence and integrity at merge time.
- `spec-author` agent (`.claude/agents/spec-author.md`): drafts the spec and owns the approval workflow.
- `architect` agent (`.claude/agents/architect.md`): records the approval in the design-pass decision drop at close-out.
- `code-review` skill (`.claude/skills/code-review/SKILL.md`): how reviewer evaluates spec quality during a review pass.
- `decide` slash skill (`.claude/skills/decide/SKILL.md`): when a spec encodes a non-trivial policy, the decision artefact lives in `.squad/decisions/`.
