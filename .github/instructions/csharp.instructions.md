---
description: 'Guidelines for building C# applications'
applyTo: '**/*.cs'
---

# C# Development

## C# Instructions
- Always use the latest version C# features.
- Always prefer new language features and APIs.
- Always replace deprecated APIs with their recommended alternatives.
- Always replace old language features with their new counterparts.
- Write clear and concise comments for each function.

## General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.

## Pure functional programming
- Using pure functional programming practices. 
- Do not use object orientation!
- Ignore these guidelines for code in performance critical (hot) paths. Then use what you need to optimize performance.

## Naming Conventions

- Never prefix interface names with "I" (e.g., IUserService).
- Never use the naming convention for async code using the Async suffix

## Formatting

- Apply code-formatting style defined in `.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions by default.
- Use expression-bodied members by default.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

## Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

## Validation and Error Handling

## Logging and Monitoring

- Always instrument the code base using OTEL using best practices 

## Testing

- Always include test cases for critical paths of the application.
- Always write E2E tests for user journeys.
- Do not emit "Act", "Arrange" or "Assert" comments.

## Performance Optimization

- Optimize performance in performance critical (hot) paths, but only there unless otherwise instructed.
- Always measure and benchmark API performance for performance critical paths.
- Use the outcomes to guide optimizations.
- Use the BenchmarkDotNet library for micro-benchmarks.
- Optimize performance in hot paths.
- Extensively comment on performance-critical code sections and motivate design decisions.
- Prefer asynchronous programming models to improve scalability.