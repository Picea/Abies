# README Badges Guide

## Current Badges in README.md

The Abies README now includes the following status badges:

### 🔧 Build & CI/CD Badges

#### 1. **CD (Continuous Delivery)**
```markdown
[![CD](https://github.com/Picea/Abies/actions/workflows/cd.yml/badge.svg)](https://github.com/Picea/Abies/actions/workflows/cd.yml)
```
- **Shows:** Build, test, and NuGet publish status
- **Runs on:** Every push to main, every PR
- **Green means:** Code builds, unit tests pass, ready to publish

#### 2. **E2E Tests**
```markdown
[![E2E Tests](https://github.com/Picea/Abies/actions/workflows/e2e.yml/badge.svg)](https://github.com/Picea/Abies/actions/workflows/e2e.yml)
```
- **Shows:** End-to-end Playwright test status
- **Runs on:** Every push to main, every PR
- **Green means:** Conduit app works correctly in real browsers

#### 3. **CodeQL Security Analysis**
```markdown
[![CodeQL](https://github.com/Picea/Abies/actions/workflows/codeql.yml/badge.svg)](https://github.com/Picea/Abies/actions/workflows/codeql.yml)
```
- **Shows:** Static code security analysis status
- **Runs on:** Every push to main, every PR, weekly scheduled
- **Green means:** No critical security vulnerabilities detected

### 📦 Project Info Badges

#### 4. **.NET Version**
```markdown
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
```
- **Shows:** Target .NET version
- **Static badge:** Manually updated when version changes

#### 5. **License**
```markdown
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
```
- **Shows:** Project license type
- **Links to:** LICENSE file

#### 6. **Security**
```markdown
[![Security](https://img.shields.io/badge/security-SAST%20%2B%20SCA-green)](SECURITY.md)
```
- **Shows:** Security scanning tools enabled
- **Links to:** SECURITY.md policy
- **Indicates:** Both SAST (CodeQL) and SCA (NuGet Audit) active

## Badge Layout in README

```
# Abies (/ˈa.bi.eːs/)

A WebAssembly library for building MVU-style web applications with .NET.

[.NET] [License] [CD] [E2E Tests] [CodeQL] [Security]
```

## What Each Badge Status Means

### ✅ Green/Passing
- All checks passed
- Safe to use/deploy
- No action needed

### ❌ Red/Failing
- Build failed or tests failing
- **Action:** Check workflow logs
- **Don't merge:** Fix issues first

### 🟡 Yellow/Pending
- Workflow currently running
- **Wait:** Give it a few minutes

### ⚪ Unknown/No Status
- Workflow hasn't run yet
- Or workflow file has issues

## Maintaining Badges

### When to Update

1. **Auto-updated (no action needed):**
   - CD status
   - E2E test status
   - CodeQL status

2. **Manual updates needed:**
   - .NET version badge (when upgrading .NET)
   - License badge (if license changes)
   - Security badge (if adding/removing tools)

### How to Update Manual Badges

#### Update .NET Version Badge
When upgrading to .NET 11:
```markdown
[![.NET](https://img.shields.io/badge/.NET-11-512BD4)](https://dotnet.microsoft.com/)
```

#### Update Security Badge
If adding new tools:
```markdown
[![Security](https://img.shields.io/badge/security-SAST%20%2B%20SCA%20%2B%20Secret%20Scanning-green)](SECURITY.md)
```

## Additional Badges to Consider (Optional)

### NuGet Package Badge
```markdown
[![NuGet](https://img.shields.io/nuget/v/Abies.svg)](https://www.nuget.org/packages/Abies/)
[![Downloads](https://img.shields.io/nuget/dt/Abies.svg)](https://www.nuget.org/packages/Abies/)
```

### Code Coverage Badge
If you add code coverage:
```markdown
[![codecov](https://codecov.io/gh/Picea/Abies/branch/main/graph/badge.svg)](https://codecov.io/gh/Picea/Abies)
```

### Contributors Badge
```markdown
[![Contributors](https://img.shields.io/github/contributors/Picea/Abies.svg)](https://github.com/Picea/Abies/graphs/contributors)
```

### Activity Badges
```markdown
[![Last Commit](https://img.shields.io/github/last-commit/Picea/Abies.svg)](https://github.com/Picea/Abies/commits/main)
[![Issues](https://img.shields.io/github/issues/Picea/Abies.svg)](https://github.com/Picea/Abies/issues)
```

## Troubleshooting

### Badge Shows "Unknown"
- Workflow hasn't run yet → Make a commit
- Workflow file has error → Check Actions tab
- Badge URL incorrect → Verify owner/repo names

### Badge Not Updating
- GitHub caching → Wait 5-10 minutes
- Badge URL wrong → Check workflow filename
- Private repo → Badge only works for public repos

### Badge Shows Wrong Status
- Click badge → Check actual workflow status
- Clear browser cache
- Verify badge URL matches workflow filename

## Best Practices

1. **Keep badges organized** - Group by type (build, quality, info)
2. **Don't overdo it** - 5-8 badges is plenty
3. **Make them clickable** - Always link to relevant page
4. **Update regularly** - Keep manual badges current
5. **Test links** - Verify badges work after adding

## Badge Services Used

- **shields.io** - Static custom badges
- **GitHub Actions** - Dynamic workflow status badges

---

**Current Badge Count:** 6  
**Auto-updating:** 3 (CD, E2E, CodeQL)  
**Manual:** 3 (.NET, License, Security)
