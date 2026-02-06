---
applyTo: '**'
---

# Agent Memory

This file contains important reminders and learned preferences for the AI assistant.

## Pull Request Guidelines

### ALWAYS Use the PR Template
When creating a pull request, **ALWAYS** read and follow the template at `.github/pull_request_template.md`.

The template requires:
1. **📝 Description** with What/Why/How sections
2. **🔗 Related Issues** - Link issues with `Fixes #`, `Closes #`, etc.
3. **✅ Type of Change** - Check applicable boxes (Bug fix, Feature, Performance, etc.)
4. **🧪 Testing** - Describe test coverage and testing details
5. **✨ Changes Made** - Bullet list of main changes
6. **🔍 Code Review Checklist** - Verify all items before requesting review

### PR Description Format
```markdown
## 📝 Description

### What
[Describe what changes are being made]

### Why
[Why are these changes needed?]

### How
[How do the changes work?]

## 🔗 Related Issues
Fixes #XX

## ✅ Type of Change
- [x] ⚡ Performance improvement
- [x] ✅ Test update

## 🧪 Testing
### Test Coverage
- [x] Unit tests added/updated
- [x] Integration tests added/updated

### Testing Details
- [Describe testing performed]

## ✨ Changes Made
- Change 1
- Change 2

## 🔍 Code Review Checklist
- [x] Code follows the project's style guidelines
- [x] Self-review of code performed
- [x] Tests added/updated and passing
```

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
