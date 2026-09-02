---
name: git-advanced
description: Advanced git operations the squad reaches for occasionally — worktrees for parallel agent isolation, bisect for regression hunting, range-diff for reviewing rebased PRs, rebase --autosquash with fixup commits, reflog recovery, partial-clone and sparse-checkout for large repos, signed commits via SSH keys, and the conventional-commits + branch-name conventions enforced by the existing PreToolUse hooks. Use when the basic add/commit/push/pull operations aren't enough — for archaeological digs, history rewrites you actually understand, multi-branch parallel work, or recovery after a destructive operation. Do not use for routine commits (Claude already knows those) or for GitHub-side operations (use the gh-cli skill).
---

# `git` — advanced operations for the squad

The squad's everyday git is `add/commit/push/pull` plus the existing PreToolUse hooks (block-direct-commits-to-main, enforce-conventional-commits, validate-branch-name) that gate the rest. This skill is for the operations you reach for less often and forget the flags for.

The squad's load-bearing git conventions are **already automated**. Don't fight them; this skill explains how to live with them and what the escape hatches are.

## Squad git conventions (enforced by hooks)

Already in `.claude/hooks/`:

- **`block-direct-commits-to-main.sh`** — `PreToolUse(Bash)`. Blocks `git commit` and `git push` when the current branch is `main` or `master`. Override only via the documented bypass (a marker in the commit message that explicitly acknowledges the deviation).
- **`enforce-conventional-commits.sh`** — `PreToolUse(Bash)`. Validates `git commit -m "..."` messages match `<type>(<scope>): <subject>` per [conventionalcommits.org](https://www.conventionalcommits.org). Types: `feat fix chore docs refactor test perf build ci style revert`.
- **`validate-branch-name.sh`** — `PreToolUse(Bash)`. Validates `git checkout -b <name>` and `git switch -c <name>` against the squad's pattern: `<type>/<issue-or-slug>` (e.g. `feat/142-rotate-tokens`, `fix/article-empty-state`).

Don't bypass these casually. If a hook is wrong, fix the hook (it's a `/decide` moment).

## Worktrees — the squad's parallel-work pattern

A worktree is a separate working directory pointing at the same `.git`. The squad uses worktrees for:
1. **Parallel architect Track A / Track B explorations** (the `architect` agent has `isolation: worktree` in its frontmatter — Tier 2 Proposal 13 from the recommendations report).
2. Reviewing a PR while leaving your in-progress work untouched.
3. Running tests on one branch while editing on another.

```bash
# Create a worktree for a feature branch (creates the branch if absent)
git worktree add ../articles-feat-142 -b feat/142-rotate-tokens

# Existing branch
git worktree add ../articles-bugfix bugfix/article-empty-state

# List
git worktree list

# Remove (after the branch is merged or abandoned)
git worktree remove ../articles-feat-142
git worktree prune                    # cleans up stale entries

# Detached worktree for a specific commit (e.g., reviewing a PR's exact state)
git worktree add --detach ../review-temp <sha>
```

**Squad rule:** worktrees do NOT isolate Aspire/Postgres/external state. If two worktrees both run `dotnet ef database update`, they'll conflict. Either (a) use per-worktree connection strings via `appsettings.<worktree>.json`, or (b) only one worktree at a time touches mutating dev infrastructure. The architect agent's `isolation: worktree` only isolates the filesystem.

## Bisect — finding the commit that broke something

When a test passes on `<old-sha>` and fails on `<new-sha>` and you don't know why:

```bash
git bisect start
git bisect bad                                       # current HEAD is broken
git bisect good <known-good-sha>                     # this one worked

# git checks out a midpoint; you test, then mark
git bisect good   # or
git bisect bad

# Repeat until git identifies the offending commit.
git bisect reset                                     # done; restore HEAD
```

**Automated bisect** — by far the most useful form. Pass a script that exits 0 (good) or non-zero (bad):

```bash
git bisect start HEAD <known-good-sha>
git bisect run dotnet test tests/Articles.Tests --filter "Name~ThePassingThenFailingTest"
```

The squad's TUnit tests work cleanly with `git bisect run` because `dotnet test` exits non-zero on any test failure. For tests that are flaky, repeat the failing test 3× in the bisect script (`for i in 1 2 3; do dotnet test ... || exit 1; done`).

## Range-diff — reviewing rebased PRs

When a PR is rebased on `main` (force-push), GitHub's "files changed" tab shows the new diff vs. main, but reviewers care about **what changed in the PR's actual commits between revisions**.

```bash
# Compare two versions of the same branch (before/after rebase)
git fetch
git range-diff origin/main..feat/142-rotate-tokens@{1} origin/main..feat/142-rotate-tokens

# After someone force-pushed and you want to see what they actually changed
git fetch origin feat/142-rotate-tokens
git range-diff <old-tip>..<old-tip-end> <new-tip>..<new-tip-end>
```

Output format: 1-to-1 commit mapping; `=` means identical, `!` means changed, `<` / `>` mean dropped/added. If `range-diff` shows `=` for every line, the rebase preserved intent.

## Rebase with autosquash

The squad's PR flow: many small commits during work, squashed at merge time. But during work, you want to retroactively edit *previous* commits without breaking the chain. Use fixup + autosquash:

```bash
# You committed something, then realized it was wrong
git commit -m "feat: add auth"
# ... realize the commit was missing a test ...
git add tests/AuthTests.cs
git commit --fixup HEAD~1                    # creates a fixup! commit referencing the prev

# Or fix a specific older commit
git commit --fixup <sha-of-commit-to-fix>

# When ready to clean up:
git rebase -i --autosquash origin/main
# Editor opens with the fixup commits already positioned next to their targets
# and marked `fixup`; just save.
```

`git config rebase.autosquash true` makes `--autosquash` the default. The squad recommends this in `~/.gitconfig`.

## Reflog — recovery after a destructive operation

`reflog` is git's "undo history" for HEAD movements. Resets, force-pushes, and bad rebases are recoverable from it for ~90 days.

```bash
git reflog                                 # all HEAD movements with timestamps
git reflog show <branch>                   # for a specific ref

# Recover a branch deleted with `git branch -D feat/142`
git reflog | grep "feat/142"               # find the SHA
git checkout -b feat/142-recovered <sha>

# Recover from `git reset --hard` to the wrong commit
git reset --hard HEAD@{1}                  # back to where HEAD was before the reset

# Recover from a bad rebase
git reflog                                 # find the pre-rebase HEAD@{N}
git reset --hard HEAD@{N}
```

**Squad rule:** when something seems lost, `git reflog` is the first move, before reaching for backups or asking around. Stash and reflog together cover ~95% of "I lost my work" cases.

## Stash — staging temporary work

```bash
git stash push -m "wip: refactor article state"      # named stash
git stash push -u                                    # include untracked files
git stash push -k -- src/Foo.cs                      # only Foo.cs; keep index untouched
git stash list
git stash show -p stash@{0}                          # full diff of a stash
git stash pop                                        # apply latest + drop
git stash apply stash@{2}                            # apply specific without dropping
git stash drop stash@{2}
```

The squad's stash discipline: name them. `git stash push` without `-m` produces "WIP on branch", which is useless when you have 4 of them.

## Partial clone / sparse checkout (large repos)

For monorepos where you only care about one product:

```bash
# Partial clone — fetch the tree but not historical blobs
git clone --filter=blob:none --no-checkout https://github.com/<owner>/<repo>
cd <repo>

# Sparse checkout — work with a subset of the tree
git sparse-checkout init --cone
git sparse-checkout set src/Articles tests/Articles.Tests

# Now `git checkout main` only materializes those paths.
```

The squad's repos are mostly per-product (Aspire-shaped), so this isn't usually needed. If you find yourself in a true monorepo, `--filter=blob:none` + `--depth=50` cuts clone time from minutes to seconds.

## Signing commits with GPG

The squad signs commits with GPG keys. GitHub verifies them when the public key is registered to your account.

### Reference setup

The canonical `~/.gitconfig` global block:

```bash
git config --global user.signingkey D92532059F0414A3
git config --global commit.gpgsign true
git config --global tag.gpgsign true
git config --global user.name "Maurice Cornelius Gerardus Petrus Peters"
git config --global user.email "me@mauricepeters.dev"
```

The signing-key id `D92532059F0414A3` is the long-form key id; the GPG key's UID email must match `me@mauricepeters.dev` for GitHub's "Verified" badge to render.

### Daily commands

```bash
# List GPG keys (confirm the signing key is present)
gpg --list-secret-keys --keyid-format=long

# Per-commit override (when global signing is off, or to force on a specific commit)
git commit -S -m "feat: add ..."

# Verify
git log --show-signature
```

### Uploading the public key to GitHub

```bash
# Export armoured public key
gpg --armor --export D92532059F0414A3

# Or upload via gh in one step
gpg --armor --export D92532059F0414A3 | gh gpg-key add -
```

### Common GPG signing footguns

- **`gpg: signing failed: Inappropriate ioctl for device`** — the GPG agent can't reach a tty for the passphrase prompt. Set `export GPG_TTY=$(tty)` in your shell rc. The squad's recommended `~/.bashrc`/`~/.zshrc` line.
- **Signing prompts every commit** — the agent's cached passphrase TTL is too short. In `~/.gnupg/gpg-agent.conf`: `default-cache-ttl 3600` and `max-cache-ttl 86400`. Reload with `gpg-connect-agent reloadagent /bye`.
- **Commits show "Unverified" on GitHub despite signing** — the email on the GPG key UID must match a verified email on the GitHub account (`me@mauricepeters.dev` for the squad's canonical setup). `gpg --list-secret-keys` shows the UID; if mismatched, edit with `gpg --edit-key D92532059F0414A3` then `adduid` / `revuid`. Or generate a new key with the right UID.
- **`gpg: WARNING: unsafe ownership on homedir`** — `chmod 700 ~/.gnupg && chmod 600 ~/.gnupg/*`.
- **CI commits unsigned** — bot/automation commits are unsigned by default. If the squad needs CI-produced commits signed (e.g., automated changelog updates), the workflow needs to import a dedicated signing key from a secret. Ask before going down that path; it's rarely worth it.
- **Subkey vs primary key for signing** — the squad recommends a separate signing subkey, with the primary key kept offline. `gpg --edit-key <id>` then `addkey` → choose `(4) RSA (sign only)` or `(10) ECC (sign only)`. Then `git config user.signingkey <subkey-id>!` (the trailing `!` pins to the subkey).

## Conventional commits — the actual format

Enforced by `enforce-conventional-commits.sh`. The pattern:

```
<type>(<optional-scope>): <subject>
                                            # blank line
<optional body, wrapped at 72>
                                            # blank line
<optional footer: BREAKING CHANGE, Refs, Co-authored-by>
```

Types the hook accepts: `feat fix chore docs refactor test perf build ci style revert`.

Examples:
- `feat(auth): rotate refresh tokens on password change`
- `fix(article): handle empty body in publish flow`
- `chore(deps): bump TUnit to 0.4.20`
- `refactor(persistence)!: replace EF interceptors with explicit pipeline`  ← `!` marks breaking change

The hook validates the *first line* matches `<type>(<scope>): ` or `<type>: `. Body and footer are not validated; they're convention only.

**The PR title is what merges.** `gh pr merge --squash` uses the PR title as the squashed commit message. Make the PR title conventional, even if the individual commits aren't perfectly clean.

## Branch naming — the actual format

Enforced by `validate-branch-name.sh`. Pattern: `<type>/<issue-or-slug>`.

Accepted:
- `feat/142-rotate-tokens`
- `fix/article-empty-state`
- `chore/bump-deps`
- `refactor/auth-pipeline`
- `docs/review-skill-readme`

Rejected:
- `mybranch` (no type)
- `feature/142` (must be `feat`, not `feature`)
- `fix/142_rotate_tokens` (underscores; use hyphens)

## Failure modes and footguns

- **`git push --force` on a shared branch** — never. `--force-with-lease` is the safer variant; it refuses if someone else has pushed since your last fetch. Make `--force-with-lease` your reflex.
- **`git rebase` on a branch other people are working on** — same problem. Rebase your own branches; merge shared ones.
- **`git clean -fdx`** — deletes everything including ignored files. Useful occasionally, devastating when run from the wrong directory. Always `git clean -ndx` (dry run) first.
- **Lost a commit after `git reset --hard`** — see reflog above. It's almost certainly recoverable; don't despair.
- **`git checkout <file>` confused with `git checkout <branch>`** — use `git restore <file>` for file-restoration and `git switch <branch>` for branch-switching. They were introduced specifically to disambiguate.
- **Commit author email mismatched with GitHub account** — commits show as "unknown author" instead of linked to your profile. Check `git config user.email` matches a verified email on your GitHub account.
- **Worktree directory deleted manually (without `git worktree remove`)** — `git worktree list` still shows it. `git worktree prune` cleans up.
- **Rebase conflicts that look impossible** — `git rebase --abort` and reconsider. Often a merge is the better choice for the situation.
