# Branch Protection Setup

This document describes how to configure branch protection rules for the `main` branch in GitHub.

## 🎯 Goal

Implement **trunk-based development** with mandatory pull requests and automated quality gates.

## 🔧 Configuration Steps

### 1. Navigate to Branch Protection Settings

1. Go to the repository on GitHub
2. Click **Settings** → **Branches**
3. Click **Add rule** or edit existing rule for `main`

### 2. Branch Name Pattern

```
main
```

### 3. Protection Rules

#### ✅ Require Pull Request Reviews

- [x] **Require a pull request before merging**
  - [x] Require approvals: **1**
  - [x] Dismiss stale pull request approvals when new commits are pushed
  - [x] Require review from Code Owners (if CODEOWNERS file exists)
  - [x] Require approval of the most recent reviewable push
  - [ ] Allow specified actors to bypass required pull requests (leave unchecked)

#### ✅ Require Status Checks

- [x] **Require status checks to pass before merging**
  - [x] Require branches to be up to date before merging

**Required status checks:**
- `CD / build`
- `E2E / e2e`
- `CodeQL Security Analysis / Analyze C# Code`
- `PR Validation / validate-pr-title`
- `PR Validation / validate-pr-description`
- `PR Validation / check-pr-size`
- `PR Validation / lint-check`
- `PR Validation / security-scan`
- `PR Validation / pr-validation-summary`

> **Note**: Status checks will appear in the list after they run at least once

#### ✅ Require Conversation Resolution

- [x] **Require conversation resolution before merging**

#### ✅ Require Signed Commits (Optional but Recommended)

- [ ] **Require signed commits** (optional - enable if you want to enforce GPG signatures)

#### ✅ Require Linear History

- [x] **Require linear history**
  - This enforces squash or rebase merging (no merge commits)

#### ✅ Require Deployments to Succeed (Optional)

- [ ] **Require deployments to succeed before merging** (if you have deployment workflows)

#### ❌ Lock Branch

- [ ] **Do not lock the branch** (allow PRs)

#### ✅ Restrict Push Access

- [x] **Do not allow bypassing the above settings**
  - This ensures even admins follow the rules

#### ✅ Restrict Force Pushes

- [x] **Do not allow force pushes**

#### ✅ Restrict Deletions

- [x] **Do not allow deletions**

### 4. Rules Applied to Everyone

- [x] **Include administrators**
  - Ensures even repo admins must follow branch protection rules

### 5. Allow Fork Syncing

- [x] **Allow force pushes** → **Only from user with bypass permissions**
  - This allows updating from forks but not direct force pushes

## 📋 Summary of Protection Rules

| Rule | Setting | Purpose |
|------|---------|---------|
| **PR Required** | ✅ Yes | All changes through PRs |
| **Approvals** | 1+ | Peer review required |
| **Status Checks** | CD, E2E, CodeQL, PR Validation | Automated quality gates |
| **Up-to-date Branch** | ✅ Required | Prevent integration issues |
| **Conversation Resolution** | ✅ Required | All feedback addressed |
| **Linear History** | ✅ Required | Clean git history |
| **Force Push** | ❌ Blocked | Prevent history rewriting |
| **Delete Branch** | ❌ Blocked | Protect main from deletion |
| **Bypass Rules** | ❌ Not allowed | Rules apply to everyone |

## 🔄 Merge Methods

Configure allowed merge methods in **Settings** → **General** → **Pull Requests**:

### Recommended Configuration

- [x] **Allow squash merging** ✅ (Preferred - creates clean history)
  - Default commit message: `Pull request title and description`
- [x] **Allow rebase merging** ✅ (Alternative - preserves commits)
- [ ] **Allow merge commits** ❌ (Disabled - creates messy history)

### Merge Button Settings

- [x] **Automatically delete head branches** ✅ (Clean up after merge)
- [ ] **Allow auto-merge** (optional - useful for dependabot)

## 🚀 Workflow

Once configured, the workflow becomes:

1. **Create branch** from main
2. **Make changes** and commit
3. **Open PR** with descriptive title and description
4. **Automated checks run** (PR validation, CD, E2E, CodeQL)
5. **Request review** from team member(s)
6. **Address feedback** and resolve conversations
7. **Checks pass** ✅
8. **Approval received** ✅
9. **Squash and merge** to main
10. **Branch auto-deleted** after merge

## 🎯 Benefits

### Code Quality
- ✅ All code is peer-reviewed
- ✅ Automated tests must pass
- ✅ Security scans prevent vulnerabilities
- ✅ Format and lint checks enforce consistency

### Process Quality
- ✅ Clear change history with meaningful commit messages
- ✅ All changes traceable through PRs
- ✅ Documented decision-making in PR discussions
- ✅ Reduced risk of breaking changes

### Team Collaboration
- ✅ Knowledge sharing through reviews
- ✅ Consistent quality standards
- ✅ Clear ownership and accountability
- ✅ Better onboarding for new contributors

## 📝 Related Documentation

- [CONTRIBUTING.md](../CONTRIBUTING.md) - Contribution guidelines
- [Pull Request Template](pull_request_template.md) - PR template
- [Security Policy](../SECURITY.md) - Security guidelines

## 🔍 Verification

To verify branch protection is configured correctly:

```bash
# Using GitHub CLI
gh api repos/:owner/:repo/branches/main/protection

# Or check in GitHub UI
# Settings → Branches → Branch protection rules → main
```

## 🆘 Troubleshooting

### Status checks not appearing

Status checks only appear in the list after they've run at least once. To populate:

1. Create a test PR
2. Wait for workflows to complete
3. Return to branch protection settings
4. The checks will now be available to select

### Can't merge PR despite passing checks

Ensure:
- Branch is up-to-date with main
- All required checks have passed
- At least 1 approval received
- All conversations resolved

### Need to bypass protection temporarily

Branch protection should **not be bypassed**. If absolutely necessary:

1. Document the reason
2. Get team approval
3. Temporarily disable the specific rule
4. Make the change
5. **Immediately re-enable** the rule
6. Document in commit/PR why bypass was needed

## 📞 Questions?

Open a [GitHub Discussion](https://github.com/Picea/Abies/discussions) for questions about branch protection setup.

---

*Last Updated: February 5, 2026*
