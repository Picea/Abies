# 👁️ Blind Review — `undo-redo-design-record`

**Reviewed:** `f0cb682bedb44d0ef9ca8a368ba0dfca0bc0b2b0..e15125c` (1 commit, 1 file:
`.squad/design/undo-redo/00-scope-undo-redo.md`, deleted, −86 lines) on branch
`docs/0-undo-redo-design`. Context read: `.gitignore`,
`.claude/hooks/enforce-review-blindness.sh`,
`.claude/hooks/enforce-track-blindness.sh`, `.claude/hooks/tests/run.sh`,
`.claude/hooks/validate-branch-name.sh`, `.claude/hooks/block-large-files.sh`,
`.claude/hooks/scope-warden.sh`, `.claude/hooks/enforce-phase-order.sh`,
`.claude/hooks/validate-phase-artifact.sh`, `.claude/docs/principles-enforcement.md`,
`.claude/enforcement/refutations.md`, `.github/workflows/*`, `CLAUDE.md`.

**History channel:** `git-history-namestatus.sh` (`e15125c 2026-09-07T11:56:59+02:00`,
`D .squad/design/undo-redo/00-scope-undo-redo.md`). No `git log`/`show`/`blame`,
no `gh pr view`/`issue view`.

**Narrative reaching this context:** ⚠️ Four disclosures, three of them mine.

1. **The dispatching prompt described a scope that is not in the range.** It said
   the change is "confined to `.squad/`, `.claude/docs/`, `.claude/enforcement/`,
   `.claude/agent-memory/`, and `docs/security/`". The range contains one deleted
   file under `.squad/design/`. Nothing under `.claude/docs/`,
   `.claude/enforcement/`, `.claude/agent-memory/` or `docs/security/` is touched
   by it. I reviewed the range as given and treat the mismatch itself as a finding
   (P5).
2. **⚠️ I leaked four lines of a prior blind review into this context, by accident.**
   Running `git grep -n "squad/design/undo-redo" HEAD` (unscoped over the tracked
   tree) returned three hits inside
   `.squad/design/chore-0-squad-flow-v2-1/08-review-blind.md` — a file my charter
   denies me. What I saw: that a previous blind review flagged this same file as an
   unexplained addition, quoted the same `git ls-tree` claim I had already verified
   myself, and asked whether the filename was deliberate or a mistake. I had already
   reached and recorded both of those observations independently before the grep
   (the `ls-tree` verification and the `00-scope-undo-redo.md` vs `00-scope.md`
   naming mismatch are in P1 and § "Why it might be needed", both established from
   the hook sources and the on-disk directory listing). I did not read further into
   that file. Weigh this section accordingly.
3. **`.claude/enforcement/refutations.md` R-8 is prior-review narrative about this
   exact file.** It is a tracked, non-denied repo file and a reviewer is expected to
   read the residual ledger, but it is a prior finding, not code. Flagged so it can
   be discounted.
4. **A grep of `.claude/docs/decisions.md` surfaced narrative about the `undo-redo`
   *feature* design pass** (two Critic LOOP BACK verdicts, an ADR-008 gate-1 ruling).
   That is narrative about the feature being designed, not about this commit. I did
   not pursue it.

**Method note:** I did not read the contents of any file under `.squad/design/`.
Where I needed to know what exists there, I used directory listings (names, sizes,
mtimes) and `git ls-tree`/`git ls-files`, not file reads — except for the accidental
`git grep` disclosed above.

---

## What this change does

**Old behaviour.** At `f0cb682`, `git ls-tree -r --name-only HEAD -- .squad/design`
returned three paths:

```
.squad/design/chore-0-squad-flow-v2-1/08-review-blind.md
.squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
.squad/design/undo-redo/00-scope-undo-redo.md
```

**New behaviour.** At `e15125c` it returns the first two. The 86-line scope artifact
is removed from the index and is also absent from the working tree.

**What is left on disk.** `.squad/design/undo-redo/` still exists and holds a complete
design pass — 13 files, 640 KB — none of them tracked and none of them ignored:

```
00-knowledge.md (13.8K)  00-scope.md (15.8K)   00-warden.md (0.9K)
00-warden-scan.md (0.9K) 01-track-a.md (38.6K) 02-track-b.md (33.1K)
03-convergence.md (51.8K) 04-realist-plan.md (188.9K) 05-critic.md (64.7K)
06-spec.md (105.2K)      room-security.md (59.7K)     07-handoff.md (57.3K)
```

**Behavioural impact on the framework: none that I can find.** Every consumer of
design artifacts resolves paths against the working tree, not the index —
`scope-warden.sh:94`, `enforce-phase-order.sh:107` and `validate-phase-artifact.sh:193`
all `os.path.join(pass_dir, "00-scope.md")`. The only three references to
`git ls-tree … .squad/design` in the whole framework are comments (verified by
grepping `ls-tree`, `ls-files` and `cat-file` across `.claude/hooks/`; the only
runtime `cat-file` is `block-large-files.sh:153`, unrelated). CI is unaffected: the
`.squad/**` paths in `trivy.yml`/`semgrep.yml` are ignore-filters, and the
docs-only classifiers in `cd.yml`/`pr-validation.yml` key off extension. The branch
name `docs/0-undo-redo-design` satisfies `validate-branch-name.sh`'s pattern.

So this is a pure record-keeping change: what it alters is what a fresh clone of this
repository contains.

## Why it might be needed

Two motivations are legible from the code, and they point the same way.

**The filename did not match what the framework looks for.** `scope-warden.sh`,
`enforce-phase-order.sh` and `validate-phase-artifact.sh` all look for `00-scope.md`
*exactly*; `CLAUDE.md`, `memory-policy.md:47` and the `architect`/`scope-warden`
charters all name `00-scope.md`. A file called `00-scope-undo-redo.md` is invisible to
all three hooks. A conforming `00-scope.md` (15.8 KB, mtime 2026-09-07 11:32, i.e.
24 minutes before this commit) now sits in the same directory, untracked. **The most
economical reading is that this is the second half of a rename whose first half was
never staged** — the misnamed file was removed from the index, the correctly-named
replacement was written but never `git add`ed.

**It was already registered as a defect.** `.claude/enforcement/refutations.md` R-8
(HEAD, lines 194–204) is titled "An unrelated design artifact (`undo-redo`) shipped in
this changeset, misnamed against the hooks beside it", is owned by `lead`, and expires
2026-09-13. Deleting the file is a plausible remediation of that entry.

**What I cannot infer.** Which of two incompatible intents is in force: *"the record
should be tracked, under the right name, and the follow-up commit is coming"*, or
*"design artifacts are session-local scratch and should never have been tracked"*. The
commit is consistent with the first and only half-consistent with the second — under
the second, the two `chore-0-squad-flow-v2-1` artifacts would go too and `.squad/design/`
would be added to `.gitignore`. Nothing in `.gitignore`, `decisions.md` or
`flow-changelog.md` states a convention either way (I grepped all three for
`.squad/design` combined with track/commit/ignore terms and found no rule).

## Is this the right approach?

**As a standalone commit, it lands in the least defensible of the three available
states.**

- *If the intent is the rename:* `git mv` (or deleting the old path and staging
  `00-scope.md` in the same commit) would have been atomic, self-explanatory in
  `--name-status`, and would have left the record intact. As committed, the repo
  passes through a state where the scope artifact simply does not exist, and stays
  there until a follow-up lands.
- *If the intent is "design artifacts are local":* the remedy this repository has
  already established for exactly this situation is in `.gitignore` lines 488–496,
  which add `.claude/worktrees/` and give the reason in the file itself: such a tree is
  "machine- and session-local state, never meaningful to commit, and large enough … that
  leaving it untracked-but-unignored invites it being swept up by an unrelated
  `git add -A`". `.squad/design/undo-redo/` is now in precisely that state at 640 KB.
  The convention exists; it was not applied.
- *If the intent is "this artifact was mis-scoped into the previous PR":* removing it
  is right, but then the ledger entry that recorded the mis-scoping should close with
  it (P2), and the four places that cite it by name should stop citing it (P1).

Simpler alternative that fits the codebase better: one commit that (a) stages
`00-scope.md`, (b) flips R-8 to `closed — verified by …` in the append-only ledger,
and (c) retargets the four citations. That is the shape the repository's own
conventions already prescribe, and it is not materially larger than what was done.

## Problems

### P1 — Four in-repo citations now name a path that does not exist

**Where:**
- `.claude/hooks/enforce-review-blindness.sh:72–74`
- `.claude/hooks/enforce-track-blindness.sh:84–86`
- `.claude/hooks/tests/run.sh:2758–2760`
- `.claude/docs/principles-enforcement.md:315–318`

All four carry the same sentence:

> `git ls-tree -r --name-only HEAD -- .squad/design` returns only `undo-redo/00-scope-undo-redo.md`

**Verified by execution.** At HEAD that command returns
`.squad/design/chore-0-squad-flow-v2-1/08-review-blind.md` and
`…/09-review-verdict.md` — and nothing else. Note the claim was *already* inaccurate at
`f0cb682` (it returned three paths, not one); what this commit changes is that the
named path is now absent entirely.

**Why it matters.** Three of the four use the sentence as an actionable instruction —
"if that pass is ever imported here, retarget this citation at the real path" — so it
is not decorative prose; it is the pointer a future maintainer is told to follow, and
it now points at nothing. The fourth instance is inside `principles-enforcement.md`'s
**Merge Criterion** section, which that file declares binding ("where it and the
`reviewer-reconcile` charter differ, **this summary wins**"). The code-review
convention on stale comments applies: *delete or update obsolete comments when the
code changes.*

**Severity note for reconcile:** no functional impact. All three `ls-tree` occurrences
are inside `#` comment blocks; `run.sh:2760` sits in a comment above test 7e, not in an
assertion, so the hook suite does not break.

### P2 — R-8 remediates but is left `status: open`

**Where:** `.claude/enforcement/refutations.md`, R-8 (HEAD lines 194–204).

The entry's `files:` field is `.squad/design/undo-redo/00-scope-undo-redo.md` — the
exact path this commit deletes. The commit performs R-8's remediation and leaves the
ledger asserting the residual is open, with `expires: 2026-09-13`.

**Verified:** the working-tree copy of `refutations.md` (which has 47 uncommitted added
lines elsewhere) still reads `status: open` for R-8, so no unstaged companion edit
closes it either.

**Why it matters.** The ledger's own convention is demonstrated three entries down at
R-9: `status: closed — verified by security-expert: … Left in the ledger per this
file's append-only rule rather than deleted.` A residual ledger whose entries stay open
after the thing they describe is fixed stops being usable as a ledger — it accumulates
false positives, and the expiry dates stop meaning anything.

**Secondary:** R-8's `level consequence` text says "the only tracked design artifact in
the repository", which was false at `f0cb682` (three tracked paths) and is now doubly
so. If the entry is being touched to close it, that line wants correcting too.

### P3 — The `undo-redo` design record now exists in exactly one working tree

**Where:** `.squad/design/undo-redo/` — 13 files, 640 KB, untracked and unignored,
on a branch literally named `docs/0-undo-redo-design`.

`CLAUDE.md` describes `.squad/design/` as "how phases hand work to each other
losslessly across isolated contexts, and it survives session restarts." A record that
is neither tracked nor ignored survives a session restart on this machine and survives
nothing else — not a fresh clone, not a second contributor, not `git clean -fdx`.

**Asymmetry that makes this hard to read as deliberate:** after this commit the
previous pass (`chore-0-squad-flow-v2-1`) still has two of its artifacts tracked, and
this pass has none. Two passes, two different policies, no stated rule distinguishing
them.

### P4 — 640 KB untracked-but-unignored, the state this repo's `.gitignore` argues against

`.gitignore:488–496` establishes the standing argument for why a large, session-local
tree should be *ignored* rather than left merely untracked. `.squad/design/undo-redo/`
now matches that description on every clause except the ignore. Practical consequence:
an `git add -A` from any agent or contributor sweeps 640 KB of unreviewed design prose
— including `04-realist-plan.md` at 189 KB — into a commit silently.
`block-large-files.sh`'s 5 MB per-file threshold does not catch it (largest file is
189 KB), so nothing in the framework would stop that.

### P5 — The reviewed range and the described change do not match, in both directions

The dispatch described a change spanning `.claude/docs/`, `.claude/enforcement/`,
`.claude/agent-memory/` and `docs/security/`. The range contains one file, under
`.squad/design/`.

Meanwhile `main..HEAD` is **2 commits and 106 files**, including
`.claude/settings.json`, `.github/workflows/claude-hooks-tests.yml`, `.gitignore`,
`.claude/hooks/lib/*.py`, and roughly 4,600 lines of new shell across
`enforce-review-blindness.sh`, `enforce-track-blindness.sh`,
`enforce-reviewer-readonly.sh`, `enforce-review-verdict.sh`, `enforce-phase-order.sh`,
`lexicon-check.sh`, `scope-warden.sh`, `validate-phase-artifact.sh`,
`validate-branch-name.sh`, `block-direct-commits-to-main.sh` and three new test suites.
By `CLAUDE.md` § 5's own list — workflows, `settings.json`, "any file the runtime
executes" — that is squarely code-shaped and requires the review pair.

The dispatch's claim that "no `.cs`, `.js`, `.csproj`, workflow, or settings file is
touched" is true of the one-commit range and **false of the branch**. If the intended
review unit is the branch rather than the commit, this review has covered under 1% of
it and the rest is unreviewed. `reviewer-reconcile` should settle which unit is
correct before issuing a verdict.

### P6 — Most of the record change is uncommitted

`git status` on this branch shows 9 modified tracked files — `.claude/docs/decisions.md`,
`.claude/enforcement/refutations.md`, `.claude/docs/flow-changelog.md`, three agent
`MEMORY.md` files, `.squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md`,
`.squad/log/2026-09-06-session.md`, `.squad/log/pass-cost.md` — plus 12+ untracked
paths, among them seven new decision drops under `.squad/decisions/archive/2026-09/`
and five new agent-memory directories.

The commit under review is one deletion; the actual state of the design record lives
mostly in unstaged edits. Whatever would resolve P1, P2 or P3 is not in this commit and
is not staged.

**Related, and worth a direct question:** one of those uncommitted modifications is
`.squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md` — a tracked review verdict
from a *previous* pass being edited after the fact. I did not read it (charter). If a
landed verdict is being amended in the working tree, that is worth reconcile's
attention on its own.

### P7 — Minor: a non-conforming artifact name remains in the pass directory

`room-security.md` sits alongside the numbered phase artifacts. `enforce-phase-order.sh`
and `validate-phase-artifact.sh` key off the numbered `NN-name.md` shape, so this file
is outside their reach in the same way `00-scope-undo-redo.md` was — the defect R-8
names, in a second instance, in the same directory. Outside this commit's range;
observed from the directory listing only (I did not read the file). Noted because if
R-8 is being closed on the grounds that the naming problem is fixed, it is not fully
fixed.

## What I could not determine from the code alone

1. **Is a companion commit coming that stages `.squad/design/undo-redo/00-scope.md`?**
   The entire reading of this change turns on this. If yes, P3 and P4 are transient and
   the only real findings are P1 and P2. If no, the design record for a feature pass is
   being deliberately dropped from version control, and that is a convention decision
   that should be written down somewhere.
2. **Is there a decision — anywhere — on whether `.squad/design/` artifacts are
   tracked?** I could not find one in `.gitignore`, `decisions.md` or
   `flow-changelog.md`. If one exists, it explains the `chore-0-squad-flow-v2-1` /
   `undo-redo` asymmetry; if not, the asymmetry is unexplained and one of the two is
   wrong.
3. **Was this file's removal the deliberate closure of R-8, or incidental?** If
   deliberate, why the ledger was not closed in the same commit. If incidental, R-8 is
   now silently satisfied and will expire unactioned on 2026-09-13.
4. **Is the review unit the commit or the branch?** See P5. The two answers imply
   wildly different reviews. If the branch, then 106 files of security-critical hook
   logic, `settings.json` and a workflow have not been reviewed here.
5. **What does the commit message say?** `git log`/`show` are denied to me by hook, so I
   could not check it against `enforce-conventional-commits.sh`, nor read any stated
   rationale it carries. If the rationale for the deletion is in the commit message,
   nothing in the tree carries it.
6. **Was `00-scope-undo-redo.md`'s content preserved in the untracked `00-scope.md`, or
   was it rewritten?** I did not read either file. `git` cannot detect the rename
   because the destination is untracked, so the diff shows a pure deletion of 86 lines
   against a 15.8 KB replacement — an 86-line file and a 15.8 KB file are not obviously
   the same document, which is why I am flagging this as unresolved rather than
   assuming a clean rename.
7. **Why does the branch not contain `ae70285` ("template spec 2.1")?** Branch
   `docs/0-undo-redo-design` forks from `main` at `8b25458`; `f0cb682` and `ae70285`
   have different trees. If `f0cb682` is meant to be the same work as `ae70285`, the
   divergence between them is unreviewed.
