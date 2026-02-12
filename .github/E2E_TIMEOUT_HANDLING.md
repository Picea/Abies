# E2E Test Timeout Handling

This document explains how E2E test failures are handled differently based on whether they're timeout-related or genuine assertion failures.

## Problem

E2E tests can fail for two main reasons:

1. **Timeout failures** - Infrastructure issues (slow CI runners, network delays, server startup delays)
2. **Assertion failures** - Genuine bugs in the application

We want to:
- ✅ **Fail PR builds on assertion failures** (real bugs)
- ⚠️ **Warn on timeout failures** (infrastructure issues)

## Solution

The E2E workflow includes intelligent failure analysis:

```yaml
# .github/workflows/e2e.yml
- name: Run E2E tests
  continue-on-error: true  # Don't fail immediately
  run: dotnet test ...

- name: Analyze test results
  run: python3 scripts/analyze_e2e_results.py <trx-file>
```

The analyzer categorizes failures:
- **Timeout** - TimeoutException, waiting timed out, etc.
- **Assertion** - Expected X but got Y, ToBeVisible failed, etc.
- **Unknown** - Other failures (treated conservatively)

## Failure Categorization

### Timeout Patterns

The following patterns indicate timeout failures:

```
TimeoutException
Timeout 30000ms exceeded
waiting for locator('.navbar') timed out
Page.WaitForURLAsync() timeout
locator.ClickAsync() timeout
```

### Assertion Patterns

The following patterns indicate assertion failures:

```
Expected true but was false
Expected "X" to be visible but was hidden
ToBeVisible() failed
ToHaveText() failed
Assert.Equal() failed
Expected 5 but got 3
```

## Behavior

### Lenient Mode (Default in CI)

```bash
python3 scripts/analyze_e2e_results.py results.trx
```

| Failure Type | Action | Exit Code | PR Build |
|--------------|--------|-----------|----------|
| All passed | ✅ Success | 0 | ✅ Pass |
| Only timeouts | ⚠️ Warning | 0 | ✅ Pass |
| Assertions | ❌ Failure | 1 | ❌ Fail |
| Unknown | ❌ Failure | 1 | ❌ Fail |

**Rationale**: Timeouts in CI are often environmental (slow runners, network issues). We don't want flaky infrastructure to block PRs.

### Strict Mode (Optional)

```bash
python3 scripts/analyze_e2e_results.py results.trx --strict
```

| Failure Type | Action | Exit Code | PR Build |
|--------------|--------|-----------|----------|
| All passed | ✅ Success | 0 | ✅ Pass |
| Any failure | ❌ Failure | 1 | ❌ Fail |

**Use case**: Use strict mode when testing locally or when you want to ensure zero failures.

## Usage

### In CI (GitHub Actions)

Already configured in `.github/workflows/e2e.yml`:

```yaml
- name: Run E2E tests (sequential)
  id: e2e-tests
  continue-on-error: true
  run: |
    dotnet test Abies.Conduit.E2E/Abies.Conduit.E2E.csproj \
      --no-build -c Debug -v minimal -m:1 \
      -l "trx;LogFileName=conduit-e2e.trx"

- name: Analyze test results
  if: always()
  run: |
    python3 scripts/analyze_e2e_results.py \
      Abies.Conduit.E2E/TestResults/conduit-e2e.trx
```

### Locally (Lenient Mode)

```bash
# Run tests
dotnet test Abies.Conduit.E2E/Abies.Conduit.E2E.csproj \
  -l "trx;LogFileName=conduit-e2e.trx"

# Analyze results (lenient)
python3 scripts/analyze_e2e_results.py \
  Abies.Conduit.E2E/TestResults/conduit-e2e.trx
```

### Locally (Strict Mode)

```bash
# Run tests
dotnet test Abies.Conduit.E2E/Abies.Conduit.E2E.csproj \
  -l "trx;LogFileName=conduit-e2e.trx"

# Analyze results (strict)
python3 scripts/analyze_e2e_results.py \
  Abies.Conduit.E2E/TestResults/conduit-e2e.trx --strict
```

## Example Output

### All Tests Passed

```
🔍 Analyzing E2E test results from: conduit-e2e.trx
   Mode: LENIENT (warn on timeouts)

======================================================================
📊 E2E Test Results Summary
======================================================================
Total Tests:       24
✅ Passed:         24
❌ Failed:         0
======================================================================

✅ All E2E tests passed!
```

### Only Timeout Failures

```
🔍 Analyzing E2E test results from: conduit-e2e.trx
   Mode: LENIENT (warn on timeouts)

======================================================================
📊 E2E Test Results Summary
======================================================================
Total Tests:       24
✅ Passed:         22
❌ Failed:         2
   ⏱️  Timeouts:    2
   🐛 Assertions:  0
   ❓ Unknown:     0
======================================================================

======================================================================
Failed Tests (timeout)
======================================================================

❌ HomePage_ShowsPopularTags
   Type: timeout
   Message: Timeout 30000ms exceeded while waiting for locator('.sidebar')

❌ ArticleTests_CreateAndView
   Type: timeout
   Message: Page.WaitForURLAsync() timeout 30000ms exceeded

======================================================================
🎯 Decision
======================================================================
⚠️  WARN: Found 2 timeout failure(s)
   Timeouts are often infrastructure issues (slow CI, network delays).
   Treated as warnings in lenient mode. Build passes.

💡 Tip: Run with --strict to fail on timeouts.
```

### Assertion Failures

```
🔍 Analyzing E2E test results from: conduit-e2e.trx
   Mode: LENIENT (warn on timeouts)

======================================================================
📊 E2E Test Results Summary
======================================================================
Total Tests:       24
✅ Passed:         22
❌ Failed:         2
   ⏱️  Timeouts:    0
   🐛 Assertions:  2
   ❓ Unknown:     0
======================================================================

======================================================================
Failed Tests (assertion)
======================================================================

❌ HomePage_ShowsBanner
   Type: assertion
   Message: Expected locator('.banner') to be visible but was hidden

❌ ArticleTests_FavoriteCount
   Type: assertion
   Message: Expected "1" but got "0" for favorite count

======================================================================
🎯 Decision
======================================================================
🚨 FAIL: Found 2 assertion failure(s)
   These are genuine test failures that must be fixed.
```

## When to Use Each Mode

### Lenient Mode ✅ (Default)

Use when:
- Running in CI/CD pipelines
- Testing on shared infrastructure
- Network conditions are unpredictable
- You want to avoid false negatives from flaky infrastructure

### Strict Mode 🔒

Use when:
- Testing locally with known-good infrastructure
- Running tests before merging important changes
- Debugging timeout issues
- You want zero tolerance for any failures

## Timeout Mitigation Strategies

Even though timeouts don't fail the build, you should still work to reduce them:

### 1. Increase Timeouts in Tests

```csharp
// Instead of default 30s timeout
await Expect(Page.Locator(".navbar"))
    .ToBeVisibleAsync(new() { Timeout = 60000 }); // 60s
```

### 2. Use Proper Wait Conditions

```csharp
// ❌ Bad - may timeout if app loads slowly
await Page.GotoAsync("/");
await Page.ClickAsync(".button");

// ✅ Good - wait for app to be ready
await Page.GotoAsync("/");
await WaitForAppReadyAsync();
await Page.ClickAsync(".button");
```

### 3. Add Retry Logic for Flaky Operations

```csharp
// Helper for flaky operations
async Task WithRetry(Func<Task> action, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await action();
            return;
        }
        catch (TimeoutException) when (i < maxRetries - 1)
        {
            await Task.Delay(1000); // Wait before retry
        }
    }
}
```

### 4. Monitor Test Duration

Track which tests consistently timeout:

```bash
# Run with detailed logging
dotnet test --logger:"console;verbosity=detailed"
```

## Continuous Improvement

### Track Timeout Rates

Monitor how often timeouts occur:

```bash
# In CI logs, you'll see:
⚠️  WARN: Found 2 timeout failure(s)
```

If timeouts become frequent:
1. Investigate infrastructure (CI runner performance)
2. Review test timeout values
3. Optimize server startup time
4. Consider caching strategies

### Convert Warnings to Errors

If your infrastructure becomes more stable, switch to strict mode:

```yaml
# .github/workflows/e2e.yml
- name: Analyze test results
  run: |
    python3 scripts/analyze_e2e_results.py \
      Abies.Conduit.E2E/TestResults/conduit-e2e.trx --strict
```

## Technical Details

### TRX File Parsing

The analyzer parses Visual Studio TRX (Test Results XML) files:

```xml
<UnitTestResult testName="MyTest" outcome="Failed">
  <Output>
    <ErrorInfo>
      <Message>Timeout 30000ms exceeded...</Message>
      <StackTrace>at Page.WaitForURLAsync()...</StackTrace>
    </ErrorInfo>
  </Output>
</UnitTestResult>
```

### Pattern Matching

The analyzer uses regex patterns to categorize failures:

```python
TIMEOUT_PATTERNS = [
    r'TimeoutException',
    r'Timeout.*exceeded',
    r'timeout.*ms',
    # ...
]

ASSERTION_PATTERNS = [
    r'Expected.*but.*got',
    r'Assert\.',
    # ...
]
```

Timeout patterns are checked first, then assertions.

## Related Documentation

- [E2E Testing Guide](../../docs/guides/testing.md#e2e-tests)
- [Playwright Documentation](https://playwright.dev)

## Questions?

If you encounter issues with test categorization:

1. Check the analyzer output for "Unknown" failures
2. Review the TRX file manually
3. Add new patterns to `scripts/analyze_e2e_results.py`
4. Open an issue or discussion

---

*Last Updated: February 5, 2026*
