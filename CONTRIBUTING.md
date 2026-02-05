# Contributing to Abies

Thank you for your interest in contributing to Abies! This document provides guidelines for contributing to the project.

## 🌳 Trunk-Based Development

We follow **trunk-based development** practices:

- **Main branch** (`main`) is always deployable and protected
- All changes go through **pull requests**
- Feature branches are **short-lived** (< 2 days)
- Commits are **small and frequent**
- CI/CD validates all changes before merge

## 🔒 Branch Protection Rules

The `main` branch is protected with the following rules:

### Required Status Checks
All PRs must pass:
- ✅ **CD workflow** - Build, test, and package validation
- ✅ **E2E workflow** - End-to-end integration tests
- ✅ **CodeQL** - Security and code quality analysis

### Pull Request Requirements
- ✅ **At least 1 approval** required (for team members)
  - **For solo development**: Set to `0` to allow self-approval
  - **GitHub Copilot reviews**: Automated code review on every PR (see [Copilot Review Guide](.github/COPILOT_REVIEW.md))
- ✅ **Up-to-date branches** - Must be current with main before merge
- ✅ **Conversation resolution** - All comments must be resolved
- ❌ **No force pushes** allowed
- ❌ **No branch deletions** allowed

### Additional Protections
- Administrators **must follow these rules** (no bypass)
- **Linear history** enforced (squash or rebase merging only)

## 🚀 Workflow

### 1. Create a Feature Branch

```bash
# Always start from the latest main
git checkout main
git pull origin main

# Create a short-lived feature branch
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

### 2. Make Small, Incremental Changes

- Keep commits focused and atomic
- Write descriptive commit messages
- Commit frequently (multiple times per day)
- Follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
feat: add new component for article comments
fix: resolve pagination navigation issue
docs: update API documentation
test: add E2E tests for profile page
refactor: simplify article update logic
```

### 3. Keep Your Branch Up-to-Date

```bash
# Regularly sync with main
git fetch origin
git rebase origin/main

# Or merge if you prefer (but rebase keeps history cleaner)
git merge origin/main
```

### 4. Run Tests Locally

Before pushing, ensure all tests pass:

```bash
# Restore and build
dotnet restore
dotnet build

# Run unit tests
dotnet test Abies.Tests/Abies.Tests.csproj

# Run integration tests
dotnet test Abies.Conduit.IntegrationTests/Abies.Conduit.IntegrationTests.csproj

# Run E2E tests (requires API server running)
dotnet test Abies.Conduit.E2E/Abies.Conduit.E2E.csproj
```

### 5. Push and Create Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name

# Create PR via GitHub UI or CLI
gh pr create --title "feat: your feature description" --body "Description of changes"
```

### 6. Address Review Feedback

- Respond to all comments
- Make requested changes in new commits
- Mark conversations as resolved when addressed
- Keep the PR up-to-date with main

### 7. Merge

Once approved and all checks pass:
- Use **Squash and Merge** (preferred) - Creates clean history
- Or **Rebase and Merge** - Preserves individual commits
- ❌ **Never use regular merge** - Creates messy history

## 📝 Pull Request Guidelines

### PR Title
Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
feat: add article favoriting functionality
fix: resolve navigation issue on profile page
docs: update installation instructions
test: add E2E tests for comment deletion
refactor: simplify routing logic
perf: optimize DOM diffing algorithm
```

### PR Description
Use the provided template. Include:
- **What** - What changes are being made
- **Why** - Why these changes are needed
- **How** - How the changes work
- **Testing** - What testing was performed
- **Related Issues** - Link to any related issues

### PR Size
- Keep PRs **small** (< 400 lines changed)
- Break large features into multiple PRs
- Each PR should be independently reviewable

### Code Quality
- Follow existing code style
- Add tests for new functionality
- Update documentation
- No commented-out code
- No TODO comments without tracking issues

## 🧪 Testing Requirements

### Unit Tests
- All new public APIs must have unit tests
- Aim for high code coverage (> 80%)
- Tests should be fast (< 1s per test)

### Integration Tests
- Test component interactions
- Test MVU message flow
- Test command execution

### E2E Tests
- Test complete user journeys
- Cover all critical paths
- Follow the [RealWorld spec](https://docs.realworld.show/)

**Note**: E2E test timeouts are treated as warnings (not failures) in CI, as they're often caused by slow infrastructure rather than bugs. Genuine assertion failures still fail the build. See [E2E Timeout Handling](.github/E2E_TIMEOUT_HANDLING.md) for details.

## 🔐 Security

- Review [SECURITY.md](SECURITY.md) for security policies
- Never commit secrets or API keys
- Use `.env` files for local configuration (gitignored)
- Report security vulnerabilities privately to me@mauricepeters.dev

## 📚 Code Style

### C# Guidelines
- Follow [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use pure functions where possible
- Prefer immutability
- Use pattern matching
- Explicit types preferred over `var` in most cases

### Blazor/Abies Specific
- Follow MVU architecture patterns
- Messages should be discriminated unions (inherit from base message type)
- Commands should be pure and composable
- No side effects in `Update` function
- Use proper HTML semantics

## 🐛 Bug Reports

When reporting bugs, include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, browser)
- Code samples or minimal reproduction

## 💡 Feature Requests

When requesting features:
- Check existing issues first
- Describe the use case
- Explain why it's valuable
- Propose implementation approach (if possible)
- Consider if it fits the framework's goals

## 📖 Documentation

- Update docs when changing APIs
- Include XML comments for public APIs
- Add examples for new features
- Keep README.md current
- Update CHANGELOG.md

## 🤝 Code Review

### As a Reviewer
- Be respectful and constructive
- Explain *why* changes are needed
- Suggest alternatives
- Approve when ready (don't be a blocker)
- Review within 24 hours when possible

### As an Author
- Respond to all comments
- Don't take feedback personally
- Ask questions if unclear
- Make changes promptly
- Thank reviewers

## 🎯 Definition of Done

A PR is ready to merge when:
- ✅ All CI checks pass (CD, E2E, CodeQL)
- ✅ At least 1 approval received
- ✅ All conversations resolved
- ✅ Branch is up-to-date with main
- ✅ Tests added/updated
- ✅ Documentation updated
- ✅ No merge conflicts
- ✅ Code follows style guidelines

## 🚫 What NOT to Do

- ❌ Commit directly to main
- ❌ Force push to shared branches
- ❌ Merge without approval
- ❌ Leave broken tests
- ❌ Add large binary files
- ❌ Commit sensitive data
- ❌ Create long-lived feature branches

## 📞 Getting Help

- **Questions?** Open a [GitHub Discussion](https://github.com/Picea/Abies/discussions)
- **Bug?** Open a [GitHub Issue](https://github.com/Picea/Abies/issues)
- **Security?** Email me@mauricepeters.dev

## 📜 License

By contributing, you agree that your contributions will be licensed under the same [MIT License](LICENSE) that covers this project.

---

Thank you for contributing to Abies! 🌲
