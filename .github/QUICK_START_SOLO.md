# Branch Protection Quick Start (Solo Developer)

This is a simplified guide for configuring branch protection when you're the sole contributor.

## 🎯 Quick Summary

**Goal**: Protect main branch with automated checks, allow self-approval

**What you get**:
- ✅ No direct commits to main (must use PRs)
- ✅ All automated checks must pass
- ✅ GitHub Copilot reviews your code automatically
- ✅ You can approve and merge your own PRs
- ✅ Clean git history with squash merging

## 📋 5-Minute Setup

### 1. GitHub Settings

Go to: `https://github.com/Picea/Abies/settings/branches`

Click **Add rule** for branch `main`

### 2. Enable These Settings

#### Required
- ✅ **Require a pull request before merging**
  - Set required approvals to: **0**
- ✅ **Require status checks to pass before merging**
  - ✅ **Require branches to be up to date before merging**
- ✅ **Require conversation resolution before merging**
- ✅ **Require linear history**
- ✅ **Do not allow bypassing the above settings**
- ✅ **Include administrators**
- ⬜ **Do not allow force pushes** (keep unchecked)
- ⬜ **Do not allow deletions** (keep unchecked)

#### Required Status Checks
After your first PR runs, add these checks:
- `build` (from CD workflow)
- `e2e` (from E2E workflow)
- `Analyze C# Code` (from CodeQL)
- `validate-pr-title` (from PR validation)
- `validate-pr-description` (from PR validation)
- `check-pr-size` (from PR validation)
- `lint-check` (from PR validation)
- `security-scan` (from PR validation)
- `pr-validation-summary` (from PR validation)

### 3. Merge Settings

Go to: `Settings → General → Pull Requests`

- ✅ **Allow squash merging**
- ✅ **Allow rebase merging**
- ⬜ **Allow merge commits** (uncheck)
- ✅ **Automatically delete head branches**

### 4. Test It

```bash
# Create test branch
git checkout -b test/branch-protection
git commit --allow-empty -m "feat: test branch protection"
git push origin test/branch-protection

# Create PR
gh pr create --title "feat: Test branch protection" \
  --body "Testing the new branch protection setup"

# Wait for checks to pass, then approve and merge
gh pr review --approve
gh pr merge --squash
```

## 🤖 GitHub Copilot Reviews

### Enable Copilot Reviews

The repository already has `.github/workflows/copilot-review.yml` configured.

Copilot will automatically review every PR and provide:
- 🔍 Code quality feedback
- 🐛 Bug detection
- 🔒 Security analysis
- ⚡ Performance suggestions
- 📚 Documentation checks

### Using Copilot Reviews

**Automatic** (default):
- Copilot reviews run on every PR automatically
- Comments appear on the PR within 2-5 minutes
- Review and address feedback as needed

**Manual** (optional):
```bash
# Request Copilot review via CLI
gh pr review --copilot
```

**In GitHub UI**:
1. Open your PR
2. Go to **Files changed** tab
3. Click **Review changes** → **Request Copilot review**

See [.github/COPILOT_REVIEW.md](.github/COPILOT_REVIEW.md) for details.

## 📝 Daily Workflow

### Standard Flow

```bash
# 1. Create feature branch from main
git checkout main
git pull origin main
git checkout -b feat/my-feature

# 2. Make changes and commit
# (multiple small commits are fine)
git add .
git commit -m "feat: add new feature"

# 3. Push and create PR
git push origin feat/my-feature
gh pr create \
  --title "feat: Add new feature" \
  --body "$(cat <<EOF
## Description
What: Added new feature X
Why: To improve Y
How: Using approach Z

## Testing
- [x] Unit tests added
- [x] E2E tests added
- [x] Manual testing performed
EOF
)"

# 4. Wait for automated checks
# - Copilot review (2-5 minutes)
# - PR validation (~2 minutes)
# - CD workflow (~5 minutes)
# - E2E tests (~3 minutes)
# - CodeQL (~2 minutes)

# 5. Address Copilot feedback if needed
# Make changes, commit, push

# 6. Once all checks pass, approve your PR
gh pr review --approve

# 7. Merge
gh pr merge --squash

# 8. Branch is automatically deleted
# Continue with next feature
git checkout main
git pull origin main
```

### Quick Commands

```bash
# Check PR status
gh pr status

# View PR checks
gh pr checks

# View Copilot review comments
gh pr view --comments

# Approve your PR
gh pr review --approve

# Merge when ready
gh pr merge --squash
```

## ⚠️ Important Notes

### What You CANNOT Do
- ❌ Push directly to `main`
- ❌ Merge without all checks passing
- ❌ Force push to protected branches
- ❌ Delete the main branch

### What You CAN Do
- ✅ Create PRs from feature branches
- ✅ Approve your own PRs (0 approvals required)
- ✅ Merge once all checks pass
- ✅ Request Copilot reviews manually

### Tips for Solo Development

**Keep PRs Small**
- < 400 lines changed (ideally)
- Single, focused change per PR
- Easier to review (even by yourself)

**Use Conventional Commits**
```
feat: add new feature
fix: resolve bug in X
docs: update README
test: add tests for Y
refactor: simplify Z
```

**Address Copilot Feedback**
- Copilot often catches real issues
- Don't ignore security warnings
- Learn from the suggestions

**Test Locally First**
```bash
# Run all tests before pushing
dotnet test

# Check formatting
dotnet format --verify-no-changes

# Check for vulnerabilities
dotnet list package --vulnerable
```

## 🚀 Benefits

### Quality Without Bottlenecks
- Automated checks catch issues
- No waiting for human reviewers
- Still maintain high standards

### Learning & Improvement
- Copilot teaches best practices
- Security feedback prevents vulnerabilities
- Performance suggestions improve code

### Clean History
- Squash merging keeps history clean
- Every PR tells a story
- Easy to revert if needed

### Ready to Scale
- Process already in place
- Easy to add team members later
- Documentation and patterns established

## 📚 Full Documentation

For complete details, see:
- [CONTRIBUTING.md](../../CONTRIBUTING.md) - Full contribution guide
- [.github/BRANCH_PROTECTION.md](.github/BRANCH_PROTECTION.md) - Detailed protection rules
- [.github/SETUP_BRANCH_PROTECTION.md](.github/SETUP_BRANCH_PROTECTION.md) - Step-by-step setup
- [.github/COPILOT_REVIEW.md](.github/COPILOT_REVIEW.md) - Copilot review guide
- [docs/adr/ADR-018-trunk-based-development.md](docs/adr/ADR-018-trunk-based-development.md) - Architecture decision

## 🆘 Troubleshooting

**Can't push to main**
- ✅ Expected! Create a PR instead

**PR checks failing**
- Check Actions tab for details
- Fix the issues and push again
- Checks will re-run automatically

**Status checks not appearing**
- They appear after first run
- Create and complete a test PR first
- Then add them to branch protection

**Copilot review not running**
- Check if GitHub Copilot is enabled
- Ensure workflow file exists
- Check Actions tab for errors

## ❓ Questions?

Open a [GitHub Discussion](https://github.com/Picea/Abies/discussions) for help!

---

**Last Updated**: February 5, 2026
