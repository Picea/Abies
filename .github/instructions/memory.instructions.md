---
applyTo: '**'
---

# Agent Memory

This file contains important reminders and learned preferences for the AI assistant.

## Pull Request Guidelines

**See `.github/instructions/pr.instructions.md` for comprehensive PR guidelines.**

Key reminders:
- Always run `dotnet format --verify-no-changes` before submitting
- Follow the PR template at `.github/pull_request_template.md`
- Use Conventional Commits format for PR titles

## Benchmark Suite

The project has benchmark suites in `Abies.Benchmarks/`:
- `DomDiffingBenchmarks.cs` - Virtual DOM diffing performance
- `RenderingBenchmarks.cs` - HTML rendering performance
- `EventHandlerBenchmarks.cs` - Event handler creation performance

## Performance Optimizations Applied

The following Toub-inspired optimizations have been applied:
1. **Atomic counter for CommandIds** - Replaced `Guid.NewGuid().ToString()` with atomic counter
2. **SearchValues fast-path** - Skip HtmlEncode when no special chars present
3. **FrozenDictionary cache** - Cache event attribute names to avoid interpolation
4. **StringBuilder pooling** - Pool StringBuilders for HTML rendering
5. **Index string cache** - Pre-allocate "__index:{n}" strings for keyed diffing
