---
name: gh-cli
description: GitHub CLI (`gh`) reference for the squad's PR/release/Actions workflow — conventional-commit-driven PR titles, draft PRs for in-progress work, release tagging that matches the changelog discipline, run inspection for this template's CI workflows (`hook-tests.yml`, `template-bootstrap.yml`), the project's PR-comment etiquette, and the gh aliases worth setting. Use when creating/listing/merging PRs, inspecting workflow runs, releasing, or scripting GitHub state via `gh api`. Do not use for git operations (use the git-advanced skill) or for Actions workflow authoring (cicd-pipelines skill).
---

# `gh` — GitHub CLI for the squad

Most of the squad's GitHub interaction routes through `gh` rather than the web UI. This is squad-specific reference: the conventions and the flags worth memorising.

This template ships no CI-based review, security, or performance gate — that review happens locally through the squad's subagents (`reviewer`, `security-expert`, `performance-engineer`), not as an automated PR check. The two CI workflows this repo runs are `hook-tests.yml` (regression suite for the `SubagentStop` hooks, path-scoped, check name `scribe-decision-merger`) and `template-bootstrap.yml` (intended to run once on first push to a repo created from this template, then delete itself; not a PR check — currently a no-op, see the README's "Use this template" known limitation). If you add your own CI-based gates, the patterns below still apply to inspecting them.

Generic `gh` documentation is at `gh help <subcommand>` and Claude already knows it. What follows is the squad-flavoured part.

## Authentication and scope

```bash
gh auth status                       # confirm which account, which scopes
gh auth refresh -s read:project,workflow,write:packages  # add scopes when needed
```

The squad needs `workflow` to inspect CI runs (`hook-tests.yml`, `template-bootstrap.yml`, or any workflow you add later). It does **not** need `delete_repo`, `admin:org`, or anything destructive; refuse to add those scopes via Claude.

## Pull requests

### Creating

PR titles follow conventional commits — `enforce-conventional-commits.sh` already gates commit messages, but the **PR title is what merges to main** when squash-merging, so the PR title is what matters for the changelog.

```bash
# Standard creation, body lifted from the most recent commit message body
gh pr create --fill

# With explicit title and template
gh pr create --title "feat(auth): rotate refresh tokens on password change" \
             --body-file .github/PULL_REQUEST_TEMPLATE.md \
             --assignee @me

# Draft for in-progress work
gh pr create --draft --fill

# Mark ready when it's actually ready
gh pr ready
```

The squad's draft convention: open as draft when the diff is incomplete. `hook-tests.yml` still runs on drafts (it has no draft check), but it's path-scoped to `.claude/hooks/**`, `.claude/agents/**`, and `.claude/docs/decision-schema.md`, so it only fires when a PR actually touches those paths.

**Dependency-bump PRs (Dependabot-authored) still need a `reviewer` verdict before merge, same as any other PR.** This template has no CI-based gate that enforces that automatically — nothing skips or waives review for Dependabot PRs because nothing was ever conditioned on the actor in the first place. Run the local squad review (`reviewer`, and `security-expert` for the dependency diff) the same way you would for a human-authored PR.

### Inspecting

```bash
gh pr view <num>                     # summary in terminal
gh pr view <num> --web               # open in browser
gh pr diff <num>                     # full diff to stdout — pipe to less
gh pr checks <num>                   # status of all checks
gh pr checks <num> --watch           # block until checks finish
```

For `hook-tests.yml` specifically (the one workflow that runs as a PR check in this template):

```bash
# Just the hook-tests status
gh pr checks <num> | grep -E "scribe-decision-merger"

# Its full output
gh run view --log-failed -j scribe-decision-merger
```

Since this template has no CI-based review gate, PR checks alone don't tell you review happened — check for the squad's local `reviewer` (and, for security-relevant or dependency PRs, `security-expert`) verdict in the PR conversation instead.

### Merging

```bash
# Squash + delete branch + use PR title as commit message
gh pr merge <num> --squash --delete-branch

# Auto-merge once required checks pass
gh pr merge <num> --squash --auto
```

**Squad rule:** never `--admin` merge. The `block-direct-commits-to-main.sh` hook is the local guard; admin-merging on the remote bypasses it. If a required check is broken, fix the check rather than bypassing it.

## Workflow runs

```bash
gh run list                          # recent runs across all workflows
gh run list --workflow=hook-tests.yml --limit 10
gh run list --workflow=hook-tests.yml --branch=feature/142-auth

# Inspect a specific run
gh run view <run-id>                 # summary
gh run view <run-id> --log           # full log
gh run view <run-id> --log-failed    # only the failed steps' logs
gh run view <run-id> -j scribe-decision-merger  # specific job

# Re-run failed jobs (useful when a run flaked, not when it was right)
gh run rerun <run-id> --failed
```

`hook-tests.yml` runs a single job (`scribe-decision-merger`) that either passes or fails on its exit code — it doesn't post a PR review comment. `template-bootstrap.yml` isn't a PR check at all: it's meant to run once on the first push to `main` on a newly-created repo, then delete itself, but currently no-ops (see the README's known limitation), so there's nothing to inspect on a PR either way.

## Releases

The squad's release flow: `main` is always releasable; tags are semantic (`v<major>.<minor>.<patch>`); the changelog is generated from conventional-commit history.

```bash
# Create a release matching the most recent tag
gh release create v1.4.0 \
  --generate-notes \
  --target main

# Pre-release / draft
gh release create v1.5.0-rc.1 --prerelease --generate-notes --draft

# Upload artifacts (rarely — Aspire deployment is via azd, not GitHub releases)
gh release upload v1.4.0 ./artifacts/*.zip --clobber
```

`--generate-notes` reads commit messages since the last tag. Conventional commits make this readable; non-conformant commits get filtered into `Other Changes`.

## Issues

```bash
gh issue create --title "..." --body "..." --label bug
gh issue list --label "needs-triage" --state open
gh issue close <num> --comment "Resolved in #142"
```

The squad's labels are deliberately small. The set of labels in the canonical repo's `.github/labels.yml`:
- `bug`, `feat`, `chore`, `docs`, `refactor` — type
- `priority:low`, `priority:high` — only used when there's a real reason
- `performance`, `perf` — signals to the squad that `performance-engineer` should review the PR; no CI job reads this label in this template
- `security` — adds security-expert as a default reviewer (set via CODEOWNERS, not a label rule)
- `needs-triage`, `blocked`, `wontfix` — state

If you find yourself adding a new label, that's a `/decide` moment, not a casual `gh label create`.

## Scripting via `gh api`

The REST/GraphQL passthrough. Worth knowing for things the CLI doesn't surface:

```bash
# Most recent successful main-branch run of hook-tests.yml
gh api repos/:owner/:repo/actions/workflows/hook-tests.yml/runs \
  --jq '.workflow_runs | map(select(.head_branch=="main" and .conclusion=="success"))[0]'

# All open PRs touching a path
gh api graphql -f query='
  query($owner:String!, $repo:String!) {
    repository(owner:$owner, name:$repo) {
      pullRequests(states:OPEN, first:50) {
        nodes { number title files(first:100) { nodes { path } } }
      }
    }
  }' -F owner=<owner> -F repo=<repo> \
  | jq '.data.repository.pullRequests.nodes[] | select(.files.nodes[].path | startswith("src/Auth"))'
```

Prefer `gh api --jq` for one-shot queries; reach for `gh api graphql` when REST would require multiple round-trips.

## Aliases worth having

The squad's recommended `~/.config/gh/config.yml` aliases:

```yaml
aliases:
  pr-mine: pr list --author "@me" --state open
  pr-review: pr list --search "review-requested:@me"
  checks: pr checks $(gh pr view --json number -q .number) --watch
  recent: run list --workflow=hook-tests.yml --limit 5
```

`gh checks` is the one developers actually live in: it watches the current PR's checks until they finish.

## Failure modes and footguns

- **`--admin` merge** — never. See above.
- **`gh repo delete`** — never via Claude. If you need to delete a repo, do it in the web UI where the confirmation dialog matches the gravity.
- **`gh secret set`** — fine for repo-level non-production secrets; refuse for org-level or production. Production secrets route through the Azure Key Vault → GitHub OIDC flow, not `gh secret set`.
- **`gh pr merge --auto` on a PR with failing required checks** — `--auto` waits for required checks to pass; it doesn't bypass them. Safe.
- **Forgetting `--repo`** when scripting outside the working tree — `gh` defaults to the current directory's git remote. In CI or when scripting, always pass `--repo <owner>/<repo>` explicitly.
