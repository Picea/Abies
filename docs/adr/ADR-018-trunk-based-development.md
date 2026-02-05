# ADR-018: Trunk-Based Development with Protected Main Branch

**Status:** Accepted  
**Date:** 2026-02-05  
**Decision Makers:** Maurice Peters  
**Tags:** #process #git #workflow #quality

## Context

As the Abies project grows and potentially attracts more contributors, we need to establish a clear and consistent development workflow that:

1. **Maintains main branch stability** - The main branch should always be in a deployable state
2. **Enforces code quality** - All code should be reviewed and tested before merging
3. **Provides clear history** - Git history should be clean and traceable
4. **Enables collaboration** - Multiple contributors should be able to work efficiently
5. **Prevents mistakes** - Automated checks should catch issues before they reach main

Currently, there are no formal branch protection rules, which means:
- Direct commits to main are possible
- Untested code could be merged
- History could be rewritten via force pushes
- No mandatory code review process

This poses risks for:
- **Production stability** - Breaking changes could be deployed without testing
- **Code quality** - Bugs could slip through without peer review
- **Security** - Vulnerabilities might not be caught early
- **Collaboration** - No structured feedback process

## Decision

We will adopt **trunk-based development** with the following implementation:

### 1. Branch Protection Rules

The `main` branch will be protected with:

- ✅ **Required pull requests** with at least 1 approval
- ✅ **Required status checks** (CD, E2E, CodeQL, PR Validation)
- ✅ **Up-to-date branch requirement** before merging
- ✅ **Conversation resolution** required
- ✅ **Linear history** enforced (squash or rebase only)
- ❌ **No force pushes** allowed
- ❌ **No branch deletion** allowed
- ❌ **No bypass** even for administrators

### 2. Pull Request Workflow

All changes must:

1. Be made in short-lived feature branches (< 2 days)
2. Go through pull request review
3. Pass all automated quality gates
4. Receive at least 1 approval
5. Be merged via squash or rebase (no merge commits)

### 3. Automated Quality Gates

Every PR must pass:

- **PR Validation Workflow**
  - Conventional Commits title format
  - Adequate PR description (min 50 chars)
  - Reasonable size (< 800 lines changed)
  - Code formatting check
  - Security vulnerability scan
  - No untracked TODOs

- **CD Workflow**
  - Build success
  - Unit tests pass
  - NuGet package creation

- **E2E Workflow**
  - End-to-end tests pass
  - Integration verification

- **CodeQL Workflow**
  - Security analysis
  - Code quality checks

### 4. Merge Strategy

- **Squash and Merge** (preferred) - Creates single commit with all changes
- **Rebase and Merge** (alternative) - Preserves individual commits
- ❌ Regular merge commits disabled

### 5. Documentation

- `CONTRIBUTING.md` - Contribution guidelines and workflow
- `.github/pull_request_template.md` - Standardized PR template
- `.github/BRANCH_PROTECTION.md` - Branch protection setup guide
- `.github/workflows/pr-validation.yml` - Automated PR validation

## Consequences

### Positive

✅ **Improved Code Quality**
- Mandatory peer review catches bugs and design issues
- Multiple sets of eyes on every change
- Knowledge sharing across the team

✅ **Always Deployable Main**
- All changes are tested before merge
- Confidence in main branch stability
- Reduced production incidents

✅ **Better Collaboration**
- Clear process for contributing
- Structured feedback mechanism
- Lower barrier for new contributors

✅ **Clean Git History**
- Linear history is easier to understand
- Clear attribution of changes
- Easier to revert if needed

✅ **Security and Compliance**
- Security scans on every PR
- Audit trail of all changes
- Clear accountability

✅ **Faster Development Cycles**
- Small, frequent changes
- Rapid feedback
- Reduced merge conflicts

### Negative

⚠️ **Slightly Slower Individual Changes**
- Must wait for reviews
- Cannot commit directly to main
- More process overhead

⚠️ **Initial Learning Curve**
- Team needs to learn the workflow
- Requires discipline with branch naming
- Must write good PR descriptions

⚠️ **Potential Bottlenecks**
- Review delays if reviewers are busy
- Blocked on failing CI checks
- Need to keep branches up-to-date

### Mitigations

🔧 **For Review Delays**
- Set expectations for review turnaround (< 24 hours)
- Enable auto-merge for approved PRs
- Allow multiple reviewers to spread load

🔧 **For CI Check Failures**
- Clear error messages in workflows
- Fast feedback (most checks < 5 minutes)
- Local testing guidance in CONTRIBUTING.md

🔧 **For Branch Staleness**
- Small PRs merge quickly
- Automated notifications when branch is stale
- Clear guidance on keeping branches updated

## Alternatives Considered

### 1. No Branch Protection (Status Quo)

**Rejected** because:
- High risk of breaking main
- No guarantee of code review
- Difficult to maintain quality at scale

### 2. GitFlow

**Rejected** because:
- Too complex for our needs
- Longer-lived branches increase merge conflicts
- Slower release cycles
- More overhead for small team

### 3. GitHub Flow (without protection)

**Rejected** because:
- Still allows direct commits to main
- No enforcement of quality gates
- Relies on discipline alone

### 4. Feature Flags + Continuous Deployment

**Considered** but:
- Adds complexity (feature flag management)
- Better suited when we have more contributors
- Can be added later if needed
- Trunk-based development is a prerequisite

## Implementation Plan

### Phase 1: Documentation (Completed ✅)
- [x] Create CONTRIBUTING.md
- [x] Create PR template
- [x] Create branch protection guide
- [x] Create PR validation workflow
- [x] Create this ADR

### Phase 2: GitHub Configuration (Next)
- [ ] Enable branch protection rules in GitHub
- [ ] Configure merge methods
- [ ] Enable auto-delete of head branches
- [ ] Test with a sample PR

### Phase 3: Team Adoption (Ongoing)
- [ ] Communicate changes to contributors
- [ ] Update README with links to CONTRIBUTING.md
- [ ] Monitor and adjust based on feedback
- [ ] Iterate on automation as needed

## Related ADRs

- [ADR-012: Test Strategy](ADR-012-test-strategy.md) - Testing requirements for PRs
- [ADR-013: OpenTelemetry](ADR-013-opentelemetry.md) - Observability for CI/CD

## References

- [Trunk-Based Development](https://trunkbaseddevelopment.com/)
- [GitHub Branch Protection Rules](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Flow](https://docs.github.com/en/get-started/quickstart/github-flow)

## Success Metrics

We will measure success by:

1. **Main branch stability** - Zero broken builds on main
2. **Code review coverage** - 100% of changes reviewed
3. **CI success rate** - > 95% of PRs pass on first try
4. **Review turnaround** - < 24 hours average
5. **Contributor satisfaction** - Feedback via surveys/discussions

## Review and Evolution

This ADR should be reviewed:
- After 1 month of adoption (March 2026)
- When we reach 5+ active contributors
- If significant issues arise
- Annually as part of process retrospective

---

**Approved by:** Maurice Peters  
**Implementation Date:** February 5, 2026
