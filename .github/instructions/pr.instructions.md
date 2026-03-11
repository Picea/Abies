---
applyTo: '**'
---

# Pull Request Guidelines

This document contains all guidelines for creating and maintaining pull requests in the Abies project.

## Code Formatting

### `dotnet format` Must Pass

Before submitting a PR, ensure your code passes the formatting check:

```bash
dotnet format --verify-no-changes
```

If there are formatting issues, fix them with:

```bash
dotnet format
```

**Important:** When fixing formatting issues in a PR branch, use `--include` to target only your changed files to avoid reformatting unrelated code:

```bash
dotnet format Picea.Abies/Picea.Abies.csproj --include path/to/your/file.cs
```

### EditorConfig Rules

The project uses `.editorconfig` to enforce consistent code style. Key rules include:
- **Whitespace alignment** - Proper indentation and spacing
- **Naming conventions** - IDE1006 rules (note: HTML event handlers like `onclick` are intentionally lowercase)
- **Unused imports** - IDE0005 removes unnecessary `using` directives

## PR Template

**ALWAYS** follow the PR template at `.github/pull_request_template.md`.

### Required Sections

1. **📝 Description**
   - **What**: Describe what changes are being made
   - **Why**: Why are these changes needed?
   - **How**: How do the changes work?

2. **🔗 Related Issues**
   - Link issues with `Fixes #`, `Closes #`, or `Resolves #`

3. **✅ Type of Change**
   - Check all applicable boxes:
     - [ ] 🐛 Bug fix (non-breaking change that fixes an issue)
     - [ ] ✨ New feature (non-breaking change that adds functionality)
     - [ ] 💥 Breaking change (fix or feature causing existing functionality to change)
     - [ ] 📚 Documentation update
     - [ ] 🧹 Code refactoring (no functional changes)
     - [ ] ⚡ Performance improvement
     - [ ] ✅ Test update

4. **🧪 Testing**
   - **Test Coverage**: Check what tests were added/updated
   - **Testing Details**: Describe how changes were tested

5. **✨ Changes Made**
   - Bullet list of main changes

6. **🔍 Code Review Checklist**
   - Verify all items before requesting review

### Example PR Description

```markdown
## 📝 Description

### What
Add atomic counter for command IDs to improve performance

### Why
`Guid.NewGuid().ToString()` allocates and is slower than an atomic counter

### How
Replace GUID generation with `Interlocked.Increment` on a static counter

## 🔗 Related Issues
Fixes #33

## ✅ Type of Change
- [x] ⚡ Performance improvement
- [x] ✅ Test update

## 🧪 Testing
### Test Coverage
- [x] Unit tests added/updated
- [x] Benchmarks added/updated

### Testing Details
- Ran benchmark suite showing 40% improvement
- All existing tests pass

## ✨ Changes Made
- Replaced GUID with atomic counter in `Command.cs`
- Added benchmark for command ID generation

## 🔍 Code Review Checklist
- [x] Code follows the project's style guidelines
- [x] Self-review of code performed
- [x] Code changes generate no new warnings
- [x] Tests added/updated and passing
```

## Labeling

When creating a pull request, it's important to label it appropriately. Labels help categorize the PR and make it easier for reviewers to understand its purpose. Here are some common labels to consider:

- **Type**: Indicates the nature of the changes (e.g., `bug`, `feature`, `documentation`)
- **Performance**: Indicates changes that improve performance OR could effect performance (e.g., `performance`, `optimization`)

## PR Title Guidelines

### Conventional Commits Format

PR titles MUST follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>[optional scope]: <description>
```

### Types

| Type | Description |
|------|-------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation only changes |
| `style` | Changes that don't affect code meaning (formatting, whitespace) |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `perf` | Performance improvement |
| `test` | Adding or correcting tests |
| `build` | Changes to build system or dependencies |
| `ci` | Changes to CI configuration |
| `chore` | Other changes that don't modify src or test files |

### Examples

✅ Good PR titles:
- `feat: Add article pagination support`
- `fix: Resolve login redirect issue`
- `perf: Optimize DOM diffing algorithm`
- `docs: Update API documentation`
- `test: Add E2E tests for favorites`
- `refactor(html): Simplify event handler creation`

❌ Bad PR titles:
- `Update code` (too vague)
- `Fixed bug` (missing type prefix)
- `WIP` (not descriptive)
- `Changes` (meaningless)

## CI Requirements

Before a PR can be merged, the following CI checks must pass:

1. **Build** - `dotnet build` must succeed with no errors
2. **Lint** - `dotnet format --verify-no-changes` must pass
3. **Tests** - All unit tests must pass
4. **E2E Tests** - All end-to-end tests must pass (when applicable)

## Review Process

1. Request review from at least one team member
2. Address all review comments
3. Re-request review after making changes
4. Ensure all CI checks pass
5. Squash and merge when approved
