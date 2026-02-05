# 🔒 Configure Branch Protection on GitHub

This file contains step-by-step instructions for configuring branch protection rules on GitHub.

## Prerequisites

- Repository admin access
- At least one successful workflow run (so status checks appear in the list)

## Step-by-Step Instructions

### 1. Navigate to Settings

1. Go to https://github.com/Picea/Abies
2. Click **Settings** tab
3. In the left sidebar, click **Branches** (under "Code and automation")

### 2. Add Branch Protection Rule

Click **Add rule** button (or **Edit** if a rule already exists for `main`)

### 3. Configure Rule

#### Branch name pattern

```
main
```

#### Protect matching branches

Enable these checkboxes:

##### ✅ Require a pull request before merging

- ✅ **Required approvals**: `0` (for solo development) or `1` (for team)
  - **Note**: For solo development, set to `0` to allow self-approval
  - When you have team members, increase to `1`
- ⬜ **Dismiss stale pull request approvals when new commits are pushed** (optional for solo dev)
- ✅ **Require review from Code Owners** (if you add a CODEOWNERS file later)
- ⬜ **Require approval of the most recent reviewable push** (optional for solo dev)
- ⬜ **Allow specified actors to bypass required pull requests** (leave UNCHECKED)

##### ✅ Require status checks to pass before merging

- ✅ **Require status checks to pass before merging** (main checkbox)
- ✅ **Require branches to be up to date before merging**

Then select these status checks (search for each):

**From CD workflow:**
```
build
```

**From E2E workflow:**
```
e2e
```

**From CodeQL workflow:**
```
Analyze C# Code
```

**From PR Validation workflow:**
```
validate-pr-title
validate-pr-description
check-pr-size
lint-check
security-scan
check-todos
pr-validation-summary
```

> **Note**: These status checks only appear after they've run at least once. If you don't see them:
> 1. Create a test PR
> 2. Wait for workflows to complete
> 3. Return to this settings page
> 4. The checks will now be available in the searchable list

##### ✅ Require conversation resolution before merging

- ✅ **Require conversation resolution before merging**

##### ✅ Require signed commits (Optional)

- ⬜ **Require signed commits** (optional - enable if you want GPG signature enforcement)

##### ✅ Require linear history

- ✅ **Require linear history**

##### ✅ Require deployments to succeed before merging (Optional)

- ⬜ **Require deployments to succeed before merging** (optional - for future deployment workflows)

##### ⬜ Lock branch

- ⬜ **Lock branch** (leave UNCHECKED - we want to allow PRs)

##### ✅ Do not allow bypassing the above settings

- ✅ **Do not allow bypassing the above settings**

##### ✅ Restrict who can push to matching branches (Optional)

- ⬜ Leave empty for now (anyone with write access can create PRs)
- Configure later if you want to restrict who can approve/merge

#### Rules applied to everyone including administrators

- ✅ **Include administrators** (ensure admins also follow rules)

##### ✅ Allow force pushes

- ⬜ **Specify who can force push** (leave UNCHECKED - no force pushes allowed)

##### ✅ Allow deletions

- ⬜ **Allow deletions** (leave UNCHECKED - prevent branch deletion)

### 4. Configure Merge Button Settings

Go back to **Settings** → **General** → scroll to **Pull Requests** section:

#### Merge button

- ✅ **Allow squash merging** 
  - Default message: **Pull request title and description**
- ✅ **Allow rebase merging**
- ⬜ **Allow merge commits** (UNCHECK - we want clean history)

#### After pull request is merged

- ✅ **Automatically delete head branches**

#### Pull request title and description

- Default to pull request title for squash merge commits: ✅

### 5. Save Changes

Click **Create** (or **Save changes** if editing)

## Verification

### Test the Protection Rules

1. Try to push directly to main:
   ```bash
   git checkout main
   git commit --allow-empty -m "test"
   git push origin main
   ```
   **Expected**: Push should be rejected

2. Create a test PR:
   ```bash
   git checkout -b test/branch-protection
   git commit --allow-empty -m "feat: test branch protection"
   git push origin test/branch-protection
   gh pr create --title "feat: Test branch protection" --body "Testing the new branch protection rules"
   ```
   **Expected**: PR created, workflows run, requires approval before merge

3. Verify status checks:
   - All workflows should run automatically
   - PR should show required checks in the merge section
   - Merge button should be disabled until checks pass

### Using GitHub CLI

You can also verify via CLI:

```bash
# Check branch protection status
gh api repos/Picea/Abies/branches/main/protection | jq .

# View required status checks
gh api repos/Picea/Abies/branches/main/protection/required_status_checks | jq .
```

## Troubleshooting

### Status checks not appearing in list

**Problem**: Required status checks don't appear in the searchable list

**Solution**: 
1. The checks must run at least once before they appear
2. Create a draft PR to trigger workflows
3. Wait for them to complete
4. Return to branch protection settings
5. The checks will now be searchable

### Can't merge despite passing checks

**Problem**: Merge button disabled even with passing checks

**Check**:
- Branch is up-to-date with main
- All required checks have passed (not just some)
- PR has required number of approvals
- All conversations are resolved
- No merge conflicts

### Accidentally bypassed protection

**If** you somehow bypass protection (shouldn't happen with settings above):

1. Document why it happened
2. Review and fix the branch protection settings
3. Add the bypass incident to a postmortem
4. Consider additional safeguards

## Next Steps

After configuring:

1. ✅ Communicate the new workflow to all contributors
2. ✅ Test with a real PR
3. ✅ Monitor for any issues
4. ✅ Adjust as needed based on feedback

## References

- [GitHub Branch Protection Docs](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [Required Status Checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging)
- [CONTRIBUTING.md](../../CONTRIBUTING.md)
- [ADR-018](../docs/adr/ADR-018-trunk-based-development.md)

---

**Last Updated**: February 5, 2026
