# ADR 0005: PR Lint Check Only Changed Files

## Status

Accepted

## Context

The PR validation workflow was failing on lint checks even when the PR author only modified a small number of files. The issue was that `dotnet format --verify-no-changes` was checking the **entire solution** for formatting issues, not just the files changed in the PR.

This caused two major problems:

1. **False failures**: PRs would fail lint checks due to formatting issues in files the author didn't touch
2. **Poor developer experience**: Contributors were blocked from merging valid changes because of unrelated formatting problems

### The Root Cause

The original lint check in `.github/workflows/pr-validation.yml` was:

```yaml
- name: Format check
  run: |
    dotnet format --verify-no-changes --verbosity diagnostic
    if [ $? -ne 0 ]; then
      echo "❌ Code formatting issues detected. Run 'dotnet format' locally."
      exit 1
    fi
    echo "✅ Code formatting is correct"
```

This command checks ALL files in the solution, not just changed files.

## Decision

We will modify the PR validation workflow to:

1. **Detect changed C# files** in the PR using `git diff`
2. **Only run format checks** on those changed files using `dotnet format --include`
3. **Skip the check entirely** if no C# files were changed

### Implementation

The new workflow:

```yaml
- name: Get changed C# files
  id: changed-files
  run: |
    # Get list of changed .cs files in the PR
    CHANGED_FILES=$(git diff --name-only --diff-filter=ACMRT origin/${{ github.event.pull_request.base.ref }}...HEAD | grep '\.cs$' || true)
    
    if [ -z "$CHANGED_FILES" ]; then
      echo "has_cs_files=false" >> $GITHUB_OUTPUT
      echo "ℹ️  No C# files changed in this PR"
    else
      echo "has_cs_files=true" >> $GITHUB_OUTPUT
      FILES_SPACE_SEPARATED=$(echo "$CHANGED_FILES" | tr '\n' ' ')
      echo "files=$FILES_SPACE_SEPARATED" >> $GITHUB_OUTPUT
      echo "📝 Changed C# files:"
      echo "$CHANGED_FILES"
    fi

- name: Format check changed files
  if: steps.changed-files.outputs.has_cs_files == 'true'
  run: |
    FILES="${{ steps.changed-files.outputs.files }}"
    echo "🔍 Checking formatting for changed files..."
    echo "Files: $FILES"
    dotnet format --verify-no-changes --include $FILES --verbosity diagnostic
    
    if [ $? -ne 0 ]; then
      echo ""
      echo "❌ Code formatting issues detected in your changes."
      echo "Please run the following command locally:"
      echo "  dotnet format --include $FILES"
      echo ""
      exit 1
    fi
    
    echo "✅ Code formatting is correct for all changed files"
```

## Consequences

### Positive

- ✅ **PRs only fail for formatting issues in changed files** - contributors are only responsible for the code they touch
- ✅ **Faster feedback** - only checking changed files is faster than checking the entire solution
- ✅ **Better developer experience** - clear, actionable error messages with the exact command to fix issues
- ✅ **Gradual improvement** - the codebase can be cleaned up incrementally as files are touched
- ✅ **Flexible** - skips the check entirely if no C# files changed (e.g., documentation-only PRs)

### Negative

- ⚠️ **Existing formatting issues remain** - files not touched by PRs will keep their formatting issues
- ⚠️ **Inconsistent formatting across solution** - during the transition period, some files will be formatted, others won't

### Mitigation Strategy

To address the negatives:

1. **Create a separate issue** to format the entire codebase (can be done incrementally)
2. **Run `dotnet format` on main periodically** as a scheduled job (optional)
3. **Document the requirement** - require `dotnet format` to be run before submitting PRs in CONTRIBUTING.md

## Alternatives Considered

### Alternative 1: Format the entire solution now

**Rejected** because:

- Would require touching 100+ files
- Risk of introducing subtle bugs
- Large PR would be hard to review
- Blocks current PRs from merging

### Alternative 2: Disable lint checks entirely

**Rejected** because:

- Would allow formatting inconsistencies to grow
- No enforcement of code style standards
- Poor long-term maintainability

### Alternative 3: Use a `.editorconfig` whitelist

**Rejected** because:

- Would require manual maintenance of file lists
- Doesn't solve the problem of checking unchanged files
- More complex to implement

## References

- GitHub Issue: [Link to issue describing the problem]
- PR validation workflow: `.github/workflows/pr-validation.yml`
- EditorConfig: `.editorconfig`

## Notes

- `dotnet format` uses the `.editorconfig` file for formatting rules
- The `--include` flag accepts space-separated file paths
- `ACMRT` in `git diff --diff-filter=ACMRT` filters for Added, Copied, Modified, Renamed, and Type-changed files (excludes Deleted)

## Date

2026-02-05
