# PR Lint Check - Changed Files Only

## Problem

PR builds were failing on lint errors from files that weren't modified in the PR. The `dotnet format --verify-no-changes` command was checking the **entire solution**, not just the changed files.

## Solution

Modified `.github/workflows/pr-validation.yml` to:

1. Detect which C# files changed in the PR
2. Only run `dotnet format` on those specific files
3. Skip the check if no C# files were changed

## Changes Made

### File: `.github/workflows/pr-validation.yml`

**Before:**

```yaml
- name: Format check
  run: |
    dotnet format --verify-no-changes --verbosity diagnostic
```

**After:**

```yaml
- name: Get changed C# files
  id: changed-files
  run: |
    CHANGED_FILES=$(git diff --name-only --diff-filter=ACMRT origin/${{ github.event.pull_request.base.ref }}...HEAD | grep '\.cs$' || true)
    if [ -z "$CHANGED_FILES" ]; then
      echo "has_cs_files=false" >> $GITHUB_OUTPUT
    else
      echo "has_cs_files=true" >> $GITHUB_OUTPUT
      FILES_SPACE_SEPARATED=$(echo "$CHANGED_FILES" | tr '\n' ' ')
      echo "files=$FILES_SPACE_SEPARATED" >> $GITHUB_OUTPUT
    fi

- name: Format check changed files
  if: steps.changed-files.outputs.has_cs_files == 'true'
  run: |
    FILES="${{ steps.changed-files.outputs.files }}"
    dotnet format --verify-no-changes --include $FILES --verbosity diagnostic
```

## Benefits

✅ PRs only fail for formatting issues in **your** changed files  
✅ Faster feedback (only checking changed files)  
✅ Better error messages with exact fix command  
✅ Skips check for non-C# changes (docs, configs, etc.)  

## For Contributors

If your PR fails the lint check:

1. Run the command shown in the error message:

   ```bash
   dotnet format --include <your changed files>
   ```

2. Or format all changed files:

   ```bash
   dotnet format
   ```

3. Commit the formatting changes and push

## Documentation

See [ADR 018](../adr/ADR-018-pr-lint-only-changed-files.md) for full architectural decision details.
