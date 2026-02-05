# GitHub Copilot Code Review

This document explains how to use GitHub Copilot for automated code reviews in the Abies project.

## Overview

GitHub Copilot can provide automated code reviews on pull requests, offering:

- 🔍 **Code quality analysis** - Best practices and patterns
- 🐛 **Bug detection** - Potential issues and edge cases
- 🔒 **Security scanning** - Vulnerability identification
- ⚡ **Performance insights** - Optimization suggestions
- 📚 **Documentation checks** - Missing or unclear docs
- ✅ **Test coverage** - Gaps in testing

## Two Ways to Use Copilot Reviews

### Option 1: Automated Workflow (Recommended for Solo Dev)

The repository includes a GitHub Actions workflow that automatically runs Copilot reviews on every PR.

**File**: `.github/workflows/copilot-review.yml`

**Triggers**:
- When a PR is opened
- When new commits are pushed
- When a PR is marked ready for review

**What it does**:
- Reviews all changed files
- Posts comments on specific lines
- Provides overall feedback
- Suggests improvements

**Configuration**:

The workflow is pre-configured with Abies-specific guidelines:
- Pure functional programming checks
- MVU pattern verification
- Immutability enforcement
- Type safety validation
- Test coverage requirements

### Option 2: Manual Copilot Review

You can also request Copilot reviews manually:

#### Using GitHub UI

1. Open a pull request
2. Go to the **Files changed** tab
3. Click the **Review changes** dropdown
4. Select **Request Copilot review**

#### Using GitHub CLI

```bash
# Request Copilot review on current PR
gh pr review --copilot

# Request review with specific focus
gh pr review --copilot --focus "security,performance"
```

## For Solo Development

### Current Setup (Solo Contributor)

Since you're currently the only contributor, here's the recommended setup:

1. **Branch Protection**: Required (prevents direct commits to main)
2. **Required Approvals**: `0` (allows self-approval)
3. **Copilot Review**: Automated on every PR
4. **Required Status Checks**: All CI/CD checks must pass

**Workflow**:
```bash
# 1. Create feature branch
git checkout -b feat/my-feature

# 2. Make changes and commit
git commit -m "feat: add new feature"

# 3. Push and create PR
git push origin feat/my-feature
gh pr create

# 4. Wait for automated checks:
#    ✅ Copilot review (automated feedback)
#    ✅ PR validation (format, size, etc.)
#    ✅ CD workflow (build, test)
#    ✅ E2E tests
#    ✅ CodeQL security scan

# 5. Review Copilot's feedback and address if needed

# 6. Approve your own PR (since you're solo)
gh pr review --approve

# 7. Merge when all checks pass
gh pr merge --squash
```

### Benefits for Solo Development

✅ **Quality Gates Without Bottlenecks**
- Automated checks catch issues
- No waiting for human reviewers
- Copilot provides feedback within minutes

✅ **Learn and Improve**
- Copilot suggests better patterns
- Identifies potential issues early
- Educational feedback on your code

✅ **Maintain Standards**
- Consistent code quality
- Security scanning
- Documentation reminders

✅ **Ready for Team Growth**
- Process already in place
- Easy to add human reviewers later
- Documentation and patterns established

## Transitioning to Team Development

When you add contributors, update the branch protection:

1. **Increase required approvals** to `1`
2. **Keep Copilot reviews** for automated feedback
3. **Add human reviewers** for design decisions
4. **Update CODEOWNERS** to specify review responsibilities

```bash
# In GitHub Settings → Branches → main → Edit rule
# Change: Required approvals: 0 → 1
```

## Copilot Review Quality

### What Copilot Reviews Well

✅ **Code patterns and best practices**
✅ **Common bugs and logic errors**
✅ **Security vulnerabilities (SQL injection, XSS, etc.)**
✅ **Performance anti-patterns**
✅ **Documentation completeness**
✅ **Test coverage gaps**
✅ **Framework-specific patterns (via custom guidelines)**

### What Copilot Might Miss

⚠️ **Business logic correctness** - Needs domain knowledge
⚠️ **Architecture decisions** - Needs project context
⚠️ **User experience** - Subjective evaluation
⚠️ **Complex interactions** - Multi-component behavior
⚠️ **Specific requirements** - Issue/spec compliance

### Best Practice

Use **both** Copilot and human review:
- Copilot: First pass, catches technical issues
- Human: Second pass, validates business logic and design

For solo development:
- Copilot: Automated technical review
- You: Manual review of Copilot's feedback
- CI/CD: Automated testing and validation

## Custom Review Guidelines

The Copilot review workflow includes Abies-specific guidelines:

```yaml
guidelines: |
  This is the Abies framework - a pure functional MVU architecture.
  
  Focus areas:
  1. Pure functional programming - Update functions must be pure
  2. Immutability - State should use immutable records
  3. MVU pattern - Model-View-Update separation
  4. Type safety - Leverage C# type system
  5. Test coverage - All new code should have tests
  6. Documentation - Public APIs need XML comments
  7. Security - Check for XSS, injection vulnerabilities
  8. Performance - Virtual DOM efficiency
```

You can customize these in `.github/workflows/copilot-review.yml`.

## Interpreting Copilot Feedback

Copilot comments are categorized by severity:

| Icon | Severity | Action |
|------|----------|--------|
| 🔴 | Critical | Must fix before merge |
| 🟡 | Warning | Should fix if applicable |
| 🔵 | Info | Consider for improvement |
| 💡 | Suggestion | Optional enhancement |

### Example Copilot Comment

```
🟡 Potential null reference exception

The property `User.Name` could be null here. Consider using 
the null-conditional operator or checking for null explicitly.

Suggested fix:
-  var name = user.Name.ToUpper();
+  var name = user.Name?.ToUpper() ?? "Unknown";
```

### How to Respond

1. **Review the suggestion** - Is it valid for your case?
2. **Make the fix** if appropriate
3. **Reply to comment** if you disagree (explain why)
4. **Resolve conversation** when addressed

## Costs and Limits

GitHub Copilot code review is included with:
- ✅ GitHub Copilot Individual subscription
- ✅ GitHub Copilot Business subscription
- ✅ GitHub Copilot Enterprise subscription

**Limits**:
- No hard limits on number of reviews
- Rate limits apply (usually not hit in normal use)
- Reviews typically complete in 2-5 minutes

## Disabling Copilot Reviews

If you want to disable automated Copilot reviews:

```bash
# Disable the workflow
git rm .github/workflows/copilot-review.yml
git commit -m "chore: disable automated Copilot reviews"
```

Or keep the file but disable it:

```yaml
# Add to .github/workflows/copilot-review.yml
on:
  workflow_dispatch:  # Only manual trigger
```

## Troubleshooting

### Copilot Review Not Running

**Check**:
1. GitHub Copilot is enabled for the repository
2. Workflow file is present and correctly formatted
3. PR is not in draft state
4. Repository has Copilot access (check settings)

### Copilot Comments Not Appearing

**Possible causes**:
1. Review completed but found no issues
2. Permission issues (check workflow permissions)
3. Rate limit hit (wait and re-run)
4. Check Actions tab for workflow errors

### False Positives

Copilot sometimes flags code that's actually correct:

**Solutions**:
1. Reply to comment explaining why code is correct
2. Add code comment explaining the pattern
3. Update custom guidelines to clarify
4. Mark as resolved if not applicable

## Examples

### Good Copilot Feedback

```
🔴 Security: Potential XSS vulnerability

User input is being rendered without sanitization in the 
HTML output. This could allow script injection.

Consider using proper HTML encoding or the framework's 
built-in sanitization.
```

### Actionable Performance Feedback

```
⚡ Performance: Unnecessary list allocation

This LINQ query creates an intermediate list. Consider 
using `Where()` directly instead of `ToList().Where()`.

-  var items = source.ToList().Where(x => x.IsActive);
+  var items = source.Where(x => x.IsActive);
```

### MVU Pattern Validation

```
🟡 Architecture: Side effect in Update function

The Update function contains a Console.WriteLine() call. 
Per MVU architecture, Update must be pure. Move side 
effects to Commands.

-  public static (Model, Command) Update(Message msg, Model model)
-  {
-      Console.WriteLine($"Processing {msg}");
-      return (model, Commands.None);
-  }
+  public static (Model, Command) Update(Message msg, Model model)
+  {
+      return (model, Commands.Batch([
+          Commands.None,
+          Commands.Log($"Processing {msg}")
+      ]));
+  }
```

## Related Documentation

- [CONTRIBUTING.md](../../CONTRIBUTING.md) - Contribution guidelines
- [.github/workflows/pr-validation.yml](../workflows/pr-validation.yml) - PR validation workflow
- [ADR-018](../../docs/adr/ADR-018-trunk-based-development.md) - Trunk-based development

## Feedback

Have suggestions for improving Copilot reviews? Open a discussion or issue!

---

*Last Updated: February 5, 2026*
