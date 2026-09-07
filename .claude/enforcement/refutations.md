# Refutation and Registration Ledger

This file holds **three disjoint kinds of entry**, per
`.squad/design/pathless-read-blindness/11-continuous-improvement-criterion.md`
§ 3.1:

| `type:` | Subject | Introduced by |
|---|---|---|
| *(refutation row, no `type:` field)* | an `S ⊋ R` observation for `(role, capability)` | `04-realist-plan.md` / BC-0 |
| `residual` | a finding not fixed in this changeset | `11-continuous-improvement-criterion.md` |
| `bound` | a structural limit no changeset can fix | `11-continuous-improvement-criterion.md` |

**They do not interact.** A `residual` or `bound` entry neither creates nor
clears an `absence-refuted` state, and is not a clearing route for a
refutation row. Routes A and E (`07-handoff.md` BC-1/BC-2) remain the only
ways to clear a refutation ledger entry. If an audit cannot tell these three
kinds apart, that is a defect in the audit, not licence to merge the
semantics.

This file is **append-only**. Extensions are appended, never edited in
place, cite the entry they extend by its position in this file, and **may be
authored only by the user** — an agent that extends its own expiry has built
the laundering shape the fields exist to prevent.

No entry in this file raises a published level. Registration records the
ceiling a rule's current evidence supports; it never claims more than that.

---

## Clarifications (2026-09-05)

Appended text only. Neither item edits an existing entry in place; both
resolve a reference that existing entries left unresolved for a reader of
this repository.

- **The D36 reference in entry 2's `remedy:` field.** That field points at
  the Lead's off-repo decision log by id. The fact it points at, stated
  directly: GitHub branch protection is unavailable on this repository's
  plan tier; the commit-gate hooks are the only merge control (Lead
  measurement, 2026-09-04).
- **`R-gate-classification` and `R-review-before-commit`**, used by entries
  1 and 3 without being defined in this file. Both are named and defined
  once, in `10-architect-ruling-classifier.md` § Q-G, which is their
  origin:
  - `R-gate-classification` — the gate refuses the classified shapes.
  - `R-review-before-commit` — no code-shaped change is committed without
    a PASS.

This section is carried over from the template as reference material for
the terms it defines. It resolves references made by template-originated
entries; this repository's own ledger below carries no entries of its own
yet, so nothing here currently has an entry to resolve against. Kept
because the definitions of `R-gate-classification` and
`R-review-before-commit` are load-bearing vocabulary for any entry this
repository's own agents register in the future.

---

## Refutation rows

None yet.

---

## Residuals and bounds

Registered by `security-expert` per `09-review-verdict.md` (PR #358 review
round 1, 🔴-9): every 🔴 the round's authors elected not to fix and every ⚠️,
so that Merge Criterion (b) has a real registration to check against rather
than "None yet". Each entry names the finding it comes from, an owner, the
level consequence of leaving it open, and an `expires:` date. Entries owned
by an agent or role outside this round's three file-sets are not touched by
any of the three and are registered here so the gap is visible rather than
silently absent. Where `security-expert` could directly verify, before
finishing this registration, that another of the round's three specialists
had already landed a fix for an entry's finding in the shared working tree,
the entry is marked `status: closed` with what was verified, rather than
removed -- this file is append-only. Everything else below is `status: open`
as of this registration; a later verification that closes one should do the
same rather than deleting the row.

### R-1 — CI does not trigger on six files the new hook-test suites assert about
- type: residual
- source: PR #358 review round 1, 🔴-1
- files: `.github/workflows/claude-hooks-tests.yml`
- owner: devops
- level consequence: stays at the level it shipped at -- a PR touching only
  `.claude/settings.json`, `.claude/commands/`, `.claude/docs/memory-policy.md`,
  `.claude/docs/pattern-lexicon.md`, `.claude/docs/principles-enforcement.md`,
  `.claude/skills/beast-mode-design/SKILL.md`, or `.squad/.gate-shadow` does
  not run the suite that asserts about those paths, and can merge green while
  reintroducing the exact bug this changeset's own § Why names as its motive.
- expires: 2026-09-27
- status: open

### R-2 — Gate 1's own scan output republishes the pattern lexicon into the one directory Track A must read
- type: residual
- source: PR #358 review round 1, 🔴-5
- files: `.claude/hooks/scope-warden.sh`, `.claude/hooks/enforce-track-blindness.sh`
- owner: devops
- level consequence: `00-warden-scan.md`/`00-warden.md` remain outside Track
  A's `DENY` list, so a per-pass excerpt of `pattern-lexicon.md` (matched
  terms, quoted in context) sits in the one directory
  `enforce-track-blindness.sh` tells `dreamer-first-principles` to read.
- expires: 2026-09-20
- status: open

### R-3 — The retired reviewer's notebook was deleted rather than migrated
- type: residual
- source: PR #358 review round 1, 🔴-7
- files: `.claude/agent-memory/reviewer/` (deleted), `.claude/agent-memory/reviewer-reconcile/` (empty)
- owner: lead (a `git mv` of retired-agent memory; outside all three
  specialists' file-sets this round)
- level consequence: four reviewer-verified lesson entries
  (`last-review-verdict-is-forgeable.md`, `command-text-matching-is-not-a-gate.md`,
  `shape-heuristics-are-defeated-by-one-line.md`,
  `safety-caps-must-fail-closed.md`) stay absent from
  `reviewer-reconcile`'s notebook even though this round reintroduces
  exactly those four shapes (🔴-2, 🔴-3, 🔴-4) and fixes them again from
  scratch rather than from the record.
- expires: 2026-09-13
- status: closed — verified by `security-expert` while registering this
  round's residuals: `.claude/agent-memory/reviewer-reconcile/` now carries
  all seventeen entries (including the four named above), landed by one of
  the other two specialists working this same round. Left in the ledger per
  this file's append-only rule rather than deleted.

### R-4 — `CLAUDE.md` § 4's residual disclosure is incomplete, and the shadow expiry is hard-coded in three places
- type: residual
- source: PR #358 review round 1, 🔴-8
- files: `CLAUDE.md`, `.claude/agents/reviewer-reconcile.md`, `.claude/commands/review.md`
- owner: tech-writer
- level consequence: § 4 reads as an exhaustive residual list and is not
  one (missing: the shadow-window mediates nothing for `Write`; `git commit
  -a`/pathspec was not gated before this round; a refspec push to `main` was
  not gated before this round). The hard-coded `2026-09-19` in two files
  disagrees with `.squad/.gate-shadow`'s `2026-09-20` and will disagree
  again after the next extension.
- expires: 2026-09-20
- status: closed — verified by `security-expert`: `CLAUDE.md` § 4 now names
  only the shadow-window gap (correct — the `-a`/pathspec and refspec gaps
  it used to also need naming are fixed in this same round, see
  R-14/`enforce-review-verdict.sh`), and the three hard-coded dates are
  replaced with a pointer to `expires:` in `.squad/.gate-shadow`. Landed by
  one of the other two specialists working this same round. Left in the
  ledger per this file's append-only rule rather than deleted.

### R-5 — Worktree-authored decision drops are stranded, and the failure mode routes the operator toward forgery
- type: residual
- source: PR #358 review round 1, ⚠️-2
- files: `.claude/hooks/enforce-reviewer-readonly.sh`, `.claude/hooks/scribe-decision-merger.sh`
- owner: security-expert, pending an architect decision (this is a design
  tension between two rulings already on record, not a code bug: the ALLOW
  confinement resolves `reviewer-reconcile`'s own outputs against payload
  `cwd` deliberately -- see both hooks' headers on why `CLAUDE_PROJECT_DIR`
  is wrong for that -- while `arch-verdict-cache-and-gate-classification`
  Q-E deliberately makes the inbox/archive/decisions.md register
  project-global. A `reviewer-reconcile` session running from a worktree
  satisfies the first and is invisible to the second.)
- level consequence: a review that happened and passed from a worktree
  session writes its decision drop somewhere `scribe-decision-merger.sh`
  never reads; `enforce-review-verdict.sh` then reports "no verdict has been
  recorded at all", and the only visible route past that message is a direct
  write to `.squad/.last-review-verdict`, which `enforce-reviewer-readonly.sh`
  correctly refuses as forgery. Not fixed in this round: closing it means
  changing which of the two rulings yields, which is an architecture
  decision, not a hook patch.
- expires: 2026-10-04
- status: open

### R-6 — Three `SubagentStop` hooks attribute artifacts by newest-mtime across all in-flight passes
- type: residual
- source: PR #358 review round 1, ⚠️-3
- files: `.claude/hooks/validate-phase-artifact.sh`, `.claude/hooks/lexicon-check.sh`, `.claude/hooks/scope-warden.sh`
- owner: devops
- level consequence: with two design passes in flight, a `SubagentStop` hook
  can validate or scan the wrong pass's directory, reporting green on a
  phase that never ran and overwriting a scan the user already gated on.
- expires: 2026-10-04
- status: open

### R-7 — `.gitignore` anchoring un-ignores every nested `log/`/`logs/` directory in the repository
- type: residual
- source: PR #358 review round 1, ⚠️-4
- files: `.gitignore`
- owner: devops
- level consequence: the fix closes the `.squad/log/*.md` swallowing bug it
  targets, but the comment's stated rationale is wrong and the anchoring is
  broader than intended across this repo's 20+ projects' own build-log
  directories.
- expires: 2026-09-20
- status: open

### R-8 — An unrelated design artifact (`undo-redo`) shipped in this changeset, misnamed against the hooks beside it
- type: residual
- source: PR #358 review round 1, ⚠️-5
- files: `.squad/design/undo-redo/00-scope-undo-redo.md`
- owner: lead
- level consequence: the only tracked design artifact in the repository is
  invisible to gate 1, `enforce-phase-order.sh` and
  `validate-phase-artifact.sh`, all three of which look for `00-scope.md`
  exactly -- and it should not have shipped in this PR regardless.
- expires: 2026-09-13
- status: open

### R-9 — `curator-adversary` is absent from `CLAUDE.md`'s roster table
- type: residual
- source: PR #358 review round 1, ⚠️-6
- files: `CLAUDE.md`
- owner: lead
- level consequence: since subagents cannot spawn subagents and `CLAUDE.md`
  is the only place delegation is described, `curator-adversary` is
  reachable only through `/curate` even though the charter, `/curate`,
  `decision-schema.md`, `principles-enforcement.md` and the test suite all
  already know it.
- expires: 2026-09-20
- status: closed — verified by `security-expert`: `CLAUDE.md`'s roster table
  now lists `curator-adversary`. Landed by one of the other two specialists
  working this same round. Left in the ledger per this file's append-only
  rule rather than deleted.

### R-10 — ~100 lines of security-critical path logic duplicated verbatim between the two blindness hooks
- type: residual
- source: PR #358 review round 1, ⚠️-7
- files: `.claude/hooks/enforce-track-blindness.sh`, `.claude/hooks/enforce-review-blindness.sh`
- owner: devops
- level consequence: a fix applied to one copy of `leaves`/`segs`/`resolve`/
  `classify`/`reaches`/`reach_hit`/`glob_root` does not reach the other, and
  nothing asserts the two stay identical.
- expires: 2026-10-04
- status: open

### R-11 — Two blocking shape checks in `validate-phase-artifact.sh` are close to unfailable
- type: residual
- source: PR #358 review round 1, ⚠️-8
- files: `.claude/hooks/validate-phase-artifact.sh`
- owner: devops
- level consequence: the `05-critic.md` "concrete failure scenario" test and
  the `01-track-a.md` emptiness test both accept prose that does not satisfy
  their own stated intent.
- expires: 2026-10-04
- status: open

### R-12 — Stale header in `enforce-review-history-channel.sh`
- type: residual
- source: PR #358 review round 1, ⚠️-10
- files: `.claude/hooks/enforce-review-history-channel.sh`
- owner: devops
- level consequence: cosmetic only -- the header still describes
  `reviewer-blind` as not yet landed, when the charter landed in this same
  commit range and the hook is already live against it.
- expires: 2026-09-27
- status: open

### R-13 — `enforce-review-history-channel.sh`'s command regex under-matches a `-c` global option
- type: residual
- source: PR #358 review round 1, ⚠️-11 (the `-c` regex half; the
  `block-direct-commits-to-main.sh` substring-overmatch half is closed by
  this round's changeset -- see `.claude/hooks/block-direct-commits-to-main.sh`)
- files: `.claude/hooks/enforce-review-history-channel.sh`
- owner: devops
- level consequence: `git -c core.pager=cat log` reaches `reviewer-blind`'s
  context because the `-c` option's VALUE is a separate token the regex
  does not skip.
- expires: 2026-09-27
- status: open

### R-14 — TM-012: `Bash` redirection bypasses the verdict-cache write confinement
- type: residual
- source: PR #358 review round 1, 🔴-6 (citation, formerly "T-013") and
  pre-existing (`CLAUDE.md` § 4's own disclosed residual)
- files: `.claude/hooks/enforce-reviewer-readonly.sh`, `.claude/hooks/enforce-review-verdict.sh`
- owner: security-expert
- level consequence: `.squad/.last-review-verdict` has exactly one
  tool-mediated writer (`Write`/`Edit`/`MultiEdit`/`NotebookEdit`, via
  `enforce-reviewer-readonly.sh`) but no coverage of a raw shell redirection
  from any of the nine `Bash`-holding roles -- the level stays "instruction",
  not "invariant", exactly as `CLAUDE.md` § 4 already says.
- expires: 2026-10-31
- status: open

### R-15 — TM-013: decision-drop authorship is unauthenticated
- type: residual
- source: PR #358 review round 1, 🔴-6 (citation, formerly "T-015")
- files: `.claude/hooks/scribe-decision-merger.sh`
- owner: security-expert
- level consequence: a well-formed, honest, non-nested
  `agent: reviewer-reconcile` / `verdict: PASS` self-declaration in
  `.squad/decisions/inbox/**` is archived and cached as a genuine verdict --
  every closed forgery round closed a PARSING differential, not this.
- expires: 2026-10-31
- status: open

### R-16 — TM-014: verdict-cache path-spelling confinement misses hardlinks and case-folding
- type: residual
- source: PR #358 review round 1, 🔴-6 (citation, formerly "T-017")
- files: `.claude/hooks/enforce-reviewer-readonly.sh`
- owner: security-expert
- level consequence: narrow -- a hardlink to `.squad/.last-review-verdict`,
  or a case-folded spelling on a case-insensitive filesystem, is not
  resolved by the existing `os.path.realpath` containment.
- expires: 2026-11-14
- status: open

### R-17 — `enforce-reviewer-readonly.sh`'s "Bash redirection" fix-direction citation named a threat-model row ("T-010") that was never present in this file
- type: residual
- source: PR #358 review round 1, 🔴-6 (the one dangling citation this
  round's threat-model additions do not give a referent to, because no
  record of what "T-010" was meant to name survived to be reconstructed)
- files: `.claude/hooks/enforce-reviewer-readonly.sh`
- owner: security-expert
- level consequence: the sentence citing it is reworded to drop the specific
  numeric reference rather than invent a row for a threat nobody can
  currently name; if a future pass recovers what T-010 was, replace this
  entry with a real one.
- expires: 2026-10-31
- status: closed — the dangling citation itself is fixed (reworded, this
  round, by `security-expert`); registered as a residual only because no
  referent could be reconstructed for the number it used to cite, which is
  a documentation note for anyone who later finds out what T-010 was, not
  an open gap in the hook's behaviour.

### R-18 — `.claude/agents/reviewer-reconcile.md:247` still cites the same dangling threat-model row this round fixes elsewhere
- type: residual
- source: PR #358 review round 1, 🔴-6
- files: `.claude/agents/reviewer-reconcile.md`
- owner: lead / tech-writer (outside all three specialists' file-sets this round)
- level consequence: `security-expert`'s fix in `enforce-reviewer-readonly.sh`
  and `scribe-decision-merger.sh` gives TM-012/013/014 real referents in
  `docs/security/threat-model.md`, but the citation in
  `reviewer-reconcile.md` was not in this round's assignments and still
  points at the old, unresolved numbering.
- expires: 2026-09-20
- status: open

---

## Round 2 closures (`security-expert`, PR #358 review round 2, 🔴-D)

Appended text only. None of the eight bullets below edits any existing entry
above in place — each cites the entry it closes by its heading/position, per
the append-only rule at the top of this file, the same mechanism R-3/R-4/R-9/
R-17 already used (there, the closure was written into the entry at the
moment of its own creation, because verification and registration happened
in the same pass; here, the entries already existed with `status: open`
written by an earlier pass, so the closure is a separate, later append
instead of a same-pass field value).

Round 1's registration disclosed the ordering hazard this closes — that the
entries below may describe defects the tree, at the moment of registration,
had already stopped having — and applied the closure mechanism to four
entries (R-3, R-4, R-9, R-17) but not to these seven, whose
`level consequence:` fields were left describing the defect in the present
tense regardless. Re-verified against the working tree, by execution or
direct code read as noted per item:

- **R-1** — closed. Verified by execution:
  `.github/workflows/claude-hooks-tests.yml`'s `paths:` filter now lists all
  six previously-missing files (`.claude/docs/memory-policy.md`,
  `.claude/docs/pattern-lexicon.md`, `.claude/docs/principles-enforcement.md`,
  `.claude/commands/**`, `.claude/skills/beast-mode-design/SKILL.md`) plus
  `.squad/.gate-shadow`, in both the `push` and `pull_request` blocks. Landed
  by `devops` working this same round; the drift guard `blindness.sh:1348`
  re-derives the same enumeration at test time (15 refs found, 0 missing),
  so a future drift re-opens loud rather than silently.

- **R-2** — closed. Verified by direct code read:
  `enforce-track-blindness.sh:244–245` lists both
  `.squad/design/*/00-warden-scan.md` and `.squad/design/*/00-warden.md` in
  `dreamer-first-principles`'s `DENY` list. Landed by `devops` working this
  same round.

- **R-6** — closed. Verified by direct code read: `validate-phase-artifact.sh`,
  `lexicon-check.sh`, and `scope-warden.sh` all now
  `from artifact_attribution import resolve_artifact` and call it in place of
  the bare newest-mtime scan the entry describes; the mtime scan survives
  only as a bounded, third-tier fallback inside `resolve_artifact` itself
  (per `lib/artifact_attribution.py`'s three-tier preference order:
  transcript ground truth, then mtime bounded by the invocation's own start,
  then unbounded with the method named in the return value). Landed by
  `devops` working this same round.

- **R-7** — closed. Verified by direct code read: `.gitignore` keeps the
  pattern deliberately unanchored (rejecting round 1's own proposed
  anchoring fix) and instead adds `!.squad/log/` ahead of the file-level
  negations (`!.squad/log/lexicon-hits.md`, `!.squad/log/pass-cost.md`,
  `!.squad/log/gate-shadow.md`), with a comment explaining why the directory
  negation must precede the file negations for git's own un-ignore rules to
  apply. Landed by `devops` working this same round.

- **R-11** — closed. Verified by direct code read: `validate-phase-artifact.sh`'s
  `05-critic.md` check now requires an ordered
  `failure scenario|reproduc|\bgiven\b.{0,200}?\bwhen\b.{0,200}?\bthen\b`
  triple rather than a bare `given\b`, and the `01-track-a.md` emptiness
  check measures the Reasoning Trail section body and refuses an "almost
  empty" one. Landed by `devops` working this same round.

- **R-12** — closed. Verified by direct code read:
  `enforce-review-history-channel.sh`'s header now reads "`reviewer-blind`
  is live: `agents/reviewer.md` has been split into `reviewer-blind` and
  `reviewer-reconcile`, and this hook fires on every `Bash` call
  reviewer-blind makes," replacing the stale "not yet landed" language.
  Landed by `devops` working this same round.

- **R-13** — closed. Verified by direct code read:
  `enforce-review-history-channel.sh`'s command regex now walks past a
  `-c`/`-C`/`--git-dir`/etc. global option and its separate-token VALUE
  before requiring the subcommand, so `git -c core.pager=cat log` is
  recognised. Landed by `devops` working this same round.

- **R-18** — closed. Verified by direct code read:
  `.claude/agents/reviewer-reconcile.md:247` no longer names the old,
  unresolved numbering; it now cites `docs/security/threat-model.md`, Trust
  Boundary 5, `TM-013`/`OR-008`, and ledger pointer R-15 — all of which
  exist. Landed by `tech-writer` working this same round. Note for a future
  reader: the reworded paragraph this closure verifies is separately
  reported (round 2 review, ⚠️-B) to make a DIFFERENT claim about this same
  repository that the working tree does not support ("four trust
  boundaries" — there are five, including the one this closure cites) — a
  distinct, currently open concern about the paragraph's OTHER sentence, not
  described by R-18, and not `security-expert`'s file to fix
  (`reviewer-reconcile.md` is `tech-writer`'s). Recorded here only so nobody
  reads this closure as covering the whole paragraph.

All eight entries above are left in the ledger verbatim per this file's
append-only rule; this section is the correction, not a rewrite.

---

## Residuals and bounds, continued

### B-1 — Variable indirection defeats all three command-classifying gates
- type: bound
- source: PR #358 review round 2, ⚠️-D
- files: `.claude/hooks/enforce-review-verdict.sh`, `.claude/hooks/block-direct-commits-to-main.sh`, `.claude/hooks/enforce-review-history-channel.sh`
- owner: security-expert
- level consequence: all three hooks classify by matching a `\bgit\s+...\b`
  (or, for merges, `\bgh\s+pr\s+merge\b`) pattern against the raw command
  TEXT. `G=git; $G commit -m x`, `g=git; $g log`, and equivalent
  variable-indirected forms never contain the literal word `git` adjacent to
  the subcommand anywhere in the command string, so none of the three ever
  produce a match. Verified by execution: `g=git; $g log` exits 0 against
  `enforce-review-history-channel.sh` for `reviewer-blind` (control: a
  literal `git log` for the same agent is refused), and a
  variable-indirected commit ran unrefused in a scratch repository against
  `enforce-review-verdict.sh`.
- why this is `type: bound`, not `type: residual`: resolving `$G` to `git`
  before classifying requires actually running the shell's own
  variable-expansion and command-lookup semantics on an untrusted string —
  which is a strictly larger problem than the classification these hooks
  perform, and running untrusted shell content inside a security hook to
  resolve it first is a worse trade than the gap it would close (the same
  "do not evaluate what you are trying to gate" posture
  `enforce-conventional-commits.sh`'s own header already states for
  `$(...)`/backtick command substitution, one section up in that same file).
  Command-text classification is detection, not prevention, for any command
  whose invoked-program identity is computed at runtime rather than written
  literally in the command string these hooks are given. No regex fixes
  that; only execution-time mediation (inspecting the ACTUAL argv the shell
  resolves, not the source text) would, and that is a different control,
  not a patch to this one. R-13 registered only the `-c`-global-option half
  of the surrounding under-matching problem, which round 2 has since fixed
  (see R-13's closure above); this entry is the first `bound` in the ledger.
- expires: N/A — structural. Re-evaluate only if these three hooks are ever
  replaced by execution-time mediation rather than command-text
  classification; that would be a different control, and this entry would
  be superseded, not fixed.
- status: open

---

## Residuals and bounds, round 3 (`security-expert`, PR #358 review round 3)

Registered per `09-review-verdict.md` (PR #358 review round 3), which reached
the Merge Criterion's round cap: two ⚠️ findings are the registration this
round's split requires before the user's chosen merge; a third (brace
expansion, ⚠️-3) is not registered here because `devops` is correcting the two
hook headers' stale "open" language to match `TM-015`'s existing ✅ Mitigated
row, which already covers it.

### R-19 — `Bash` is unmediated by both blindness hooks, and `reviewer-blind` holds `Bash`
- type: residual
- source: PR #358 review round 3, ⚠️-1
- files: `.claude/hooks/enforce-review-blindness.sh`, `.claude/hooks/enforce-track-blindness.sh`, `.claude/hooks/enforce-review-history-channel.sh`, `docs/security/threat-model.md` (Trust Boundary 6), `.claude/agents/reviewer-blind.md`
- owner: security-expert, pending an architect decision on fix direction
  (either close the `Bash` channel in-hook, or drop `Bash` from
  `reviewer-blind`'s grant and route history exclusively through
  `enforce-review-history-channel.sh`'s existing `git`/`gh` gating — either
  way this entry is the registration, not the fix)
- level consequence: verified by execution — every hook in `.claude/hooks/`
  was fired with a `reviewer-blind` `Bash` payload of
  `cat .squad/design/<slug>/04-realist-plan.md`; not one refused, including
  `enforce-review-history-channel.sh`, which gates `git log`/`show`/`blame`
  and `gh pr view`/`issue view` but not `cat`. `dreamer-first-principles`'s
  blindness is invariant-level: its `tools:` grant is `Read, Grep, Glob,
  Write`, so the four hook-mediated tools ARE its whole read surface, and
  `enforce-track-blindness.sh` refuses regardless of the agent's own
  compliance. `reviewer-blind`'s `tools:` grant additionally includes
  `Bash`, which reads the same design artifacts, decision history, and
  review narrative the four mediated tools are refused — so for
  `reviewer-blind` specifically, the blindness this repository enforces is
  instruction-level over `Bash` (nothing refuses the read; only
  `reviewer-blind.md`'s own charter, in the section titled "the One Thing
  That Remains a Promise," asks the agent not to) and invariant-level over
  `Read`/`Grep`/`Glob` (the hook refuses regardless of compliance). Trust
  Boundary 5's parallel disclosure (TM-012) names its own widest channel
  explicitly ("mediates none of those tools' equivalent effect via `Bash`");
  Trust Boundary 6, added the same round as this gap, scopes itself to
  "Agent tool calls (`Grep`/`Glob`/`Read`/`NotebookRead`)" only and does not
  name `Bash`, so the boundary's own text and this gap agree only because
  the boundary is silent on the one channel that differs between its two
  governed agents.
- expires: 2026-10-31
- status: open

### R-20 — The sibling-worktree and symlink deny-closure gaps both blindness hook headers name as "open, deliberately" are registered nowhere
- type: residual
- source: PR #358 review round 3, ⚠️-2
- files: `.claude/hooks/enforce-review-blindness.sh:56–72`, `.claude/hooks/enforce-track-blindness.sh:68–84`
- owner: devops
- level consequence: both hook headers' "What remains open, deliberately"
  section names two gaps — (1) a sibling worktree layout that resolves to
  the `elsewhere` branch and gets only the exact/descendant trailing-run
  treatment, not an ancestor one (Claude Code nests worktrees under
  `.claude/worktrees/`, so this is a documented alternative layout from
  `git-advanced/SKILL.md` rather than this repo's live convention, which is
  why the header names no fixed path segment it could deny for it), and (2)
  symlinks, which would need `os.path.realpath` and are untouched by the
  current containment check. `grep -n worktree .claude/enforcement/refutations.md`
  finds only R-5, which is about a `reviewer-reconcile` session running FROM
  a worktree, not about reading THROUGH one; `grep -n symlink
  docs/security/threat-model.md` finds only TM-014, the verdict-cache
  path-spelling gap on Trust Boundary 5, not blindness on Trust Boundary 6.
  TM-015's Mitigation and Test columns cover the Glob-composition threat and
  nothing else. Naming a gap in a hook header's comment is not registering
  it — the same argument accepted for round 2's 🔴-B, applied here to the
  milder sibling half that never got the entry the ancestor-direction half
  received.
- expires: 2026-10-04
- status: open

---

## Residuals and bounds, security-expert (2026-09-06, `undo-redo` pass gate 1)

### R-21 — `docs/adr/**` is reachable by `dreamer-first-principles` even though the squad-flow specification lists ADRs among the paths Track A may not read
- type: residual
- source: architect finding, `.claude/agent-memory/architect/adrs-are-inside-track-a-reading.md`,
  surfaced at gate 1 for the `undo-redo` pass currently open
  (`.squad/design/undo-redo/00-warden.md` § "Findings on Adjacent Artifacts" →
  "Production ADR: `docs/adr/ADR-008-immutable-state.md:85`"); registered here
  by `security-expert` after direct verification against
  `enforce-track-blindness.sh`'s `DENY` table.
- files: `.claude/hooks/enforce-track-blindness.sh` (`DENY["dreamer-first-principles"]`,
  lines 278–290), `docs/adr/ADR-008-immutable-state.md:85`
- owner: devops
- level consequence: verified by direct code read —
  `enforce-track-blindness.sh:278–290` denies `dreamer-first-principles` the
  decision register, the archived decision register, the tech-stack record,
  the pattern lexicon, agent memories, worktrees, decision drops,
  `00-knowledge.md`, `02-track-b.md`, and both `00-warden-scan.md` and
  `00-warden.md`. It has no entry for `docs/adr/**`, so ADRs are read as
  ordinary codebase, not as the recorded-conclusions register they carry
  (their Consequences sections). Concretely,
  `docs/adr/ADR-008-immutable-state.md:85` states "Undo/redo: Trivial to
  implement by storing state snapshots," which directly pre-answers the
  `undo-redo` pass's central open question ("At what level undo operates —
  what the history consists of, and what the unit of a single undoable step
  is," `00-scope.md` per the warden report). At gate 1 for this pass the user
  chose to leave both the ADR and the hook as they are, so the experiment is
  not disturbed mid-run. Net: Track A's blindness to ADRs is
  instruction-level for the `undo-redo` pass — nothing in
  `enforce-track-blindness.sh` refuses the read; only the pass's own gate-1
  record and `dreamer-first-principles`'s charter ask it to derive
  independently rather than cite the line — and would become invariant-level
  only once the hook's `DENY` table actually covers `docs/adr/**` (or an
  equivalent per-pass override analogous to `.lexicon-override` is built for
  the cases, per `adrs-are-inside-track-a-reading.md`, where an ADR is
  genuinely relevant reading and should stay open).
- closure route: a `DENY` entry for `docs/adr/**` (or a scoped equivalent) in
  `enforce-track-blindness.sh`, plus corresponding assertions in
  `.claude/hooks/tests/blindness.sh`, through its own review pair. Deferred
  deliberately until the `undo-redo` pass closes, so amending the hook
  mid-pass does not retroactively alter what Track A already had available to
  read in this run.
- expires: 2026-10-04
- status: open

---

### Correction to R-21 — `source:` citation repointed to surviving evidence

Appended text only; does **not** edit R-21's `source:` field in place, per
this file's own append-only rule (R-21 is not the `## Extensions` case below —
this is a citation correction on an already-registered, still-`open` entry,
the same shape as the "Round 2 closures" section above, which
`security-expert` also authored directly rather than routing through the
user, because it corrects a pointer rather than extends an `expires:` or
otherwise widens what the entry licenses).

R-21's `source:` field cites `` `.squad/design/undo-redo/00-warden.md` §
"Findings on Adjacent Artifacts" `` — a heading that exists in no committed
version of that file. Per `09-review-verdict.md` (PR #359 review round 1,
🔴-2): the committed `00-warden.md` is titled "Scope Warden — undo-redo
(re-run)", verdict CLEAN, sections "Nothing found in" and the gate question
only, closing "0 findings"; `git log --all -- .squad/design/undo-redo/00-warden.md`
returns a single commit, so no earlier version survives to recover. This is
the documented happy path, not data loss: `CLAUDE.md` § 3 rule 2 has the
warden re-run and overwrite the same path when the user sends a scope back,
and that is what happened here.

The entry's *substance* is unaffected and independently verified — the
citation is what fails to resolve, not the finding. Read R-21's `source:`
together with this correction as pointing instead at:

- the deleted draft scope, at the commit before `00-scope.md` was rewritten:
  `git show f0cb682:.squad/design/undo-redo/00-scope-undo-redo.md` line 25 —
  "It is **the starting point** for this work, not something to replace or
  duplicate" — and line 74 — "The problem has **a well-known conventional
  answer**…". Both are the leaked framings gate 1's re-run corrected; the
  final `00-scope.md:64` inverts the first outright and drops the second
  entirely.
- `09-review-verdict.md` (PR #359 review round 1), § "08's P7, and what only
  I could check", which independently re-derives both quotations from the
  same commit and states "Gate 1 did what the entry says it did."
- `enforce-track-blindness.sh:277-290` (`DENY["dreamer-first-principles"]`),
  already cited under R-21's `level consequence:`, confirmed to have no
  `docs/adr/**` entry — this half of R-21's citation was never in question.

R-21 is otherwise unchanged by this correction: `status: open`,
`expires: 2026-10-04`, owner `devops`, closure route unchanged.

---

## Residuals and bounds, security-expert (2026-09-07, PR #359 review round 1)

Registered per `09-review-verdict.md` (PR #359 review round 1, 🔴 "Merge
Criterion (b), repo-wide"), which names ⚠️-1, ⚠️-3, ⚠️-5 and ⚠️-6 as verified
findings registered nowhere. All four are pre-existing defects in files
outside this docs-only PR's own file-set (Conduit `ServiceDefaults` and two
`.claude/hooks/*.sh` scripts owned by `devops`), so registration is the
remedy, not a fix landed here. ⚠️-2, ⚠️-4's changelog half, ⚠️-7 and ⚠️-8 are
not registered here: each names a direct edit to a specific file owned by an
agent other than `security-expert` (`decisions.md`/agent-memory index lines,
`flow-changelog.md`, this agent's own memory note, and the `critic`/`realist`
memory notes respectively) and are fixable in place rather than requiring a
residual.

### R-22 — Decision-drop `created:` timestamps are author-asserted at a moment they cannot be reliably known, producing two impossible values and same-day `id` collisions
- type: residual
- source: PR #359 review round 1, ⚠️-1
- files: `.claude/docs/decisions.md`, `.squad/decisions/archive/2026-09/`, `.claude/hooks/scribe-decision-merger.sh`
- owner: devops
- level consequence: five of the seven drops in this pass carry
  unreadable-clock `created:` values (`critic` ×3 and `architect` ×2 hold no
  `Bash` and cannot run `date -u`), two of them dated after the commit that
  introduces them. The residual harm is `id` collision: two drops in this
  commit already share `T000000Z`, and `decision-schema.md` requires `id`
  globally unique, so a third same-slug drop on either day collides outright.
  Root cause is broader than "Bash-less roles": `reviewer-reconcile`'s own
  round-1 drop reproduced the defect while holding `Bash`, off by 1h50m,
  because the schema asks every author for a value only reliably knowable at
  merge time, and `scribe-decision-merger.sh` already computes that exact
  value to name the archive file — it just does not stamp it back onto
  `created:`.
- expires: 2026-09-21
- status: open

### R-23 — Six declared `ActivitySource`s are collected nowhere; the one framework registration in Conduit's `ServiceDefaults` matches none of them
- type: residual
- source: PR #359 review round 1, ⚠️-3
- files: `Picea.Abies.Conduit.ServiceDefaults/Extensions.cs:32`,
  `Picea.Abies/Runtime.cs:88`, `Picea.Abies/Subscriptions/Manager.cs:32`,
  `Picea.Abies.Server/Page.cs:57`, `Picea.Abies.Server/Session.cs:111`,
  `Picea.Abies.Server.Kestrel/WebSocketTransport.cs:54`,
  `Picea.Abies.Server.Kestrel/Endpoints.cs:44`
- owner: devops
- level consequence: `AddSource` matches exact names only (wildcards require
  an explicit `*`), and no `ActivitySource` in this repository is named
  exactly `"Picea.Abies"` — so the framework's runtime, subscription,
  server-page, session and WebSocket-transport traces are dropped on the
  floor in Conduit today. Live violation of the team's own OTEL
  `ActivitySource`-registration principle and of the Reviewer's
  observability dimension. Pre-existing and out of scope for the docs-only
  PR that surfaced it; probable one-line remedy
  (`.AddSource("Picea.Abies*")`) belongs to its own PR and review pair.
- expires: 2026-09-21
- status: open

### R-24 — `pass-cost.md`'s header states units the underlying data does not have
- type: residual
- source: PR #359 review round 1, ⚠️-5
- files: `.squad/log/pass-cost.md`, `.claude/hooks/session-logger.sh`
- owner: devops
- level consequence: the header states "wall-clock per design phase, keyed
  by slug"; the column is cumulative-since-session-start and monotonic, and
  does not reset across slugs — a later slug's rows continue an earlier
  slug's running total. The header itself states this file is the
  denominator for whether the dual-Dreamer-track design costs its keep, so a
  reader who trusts the header computes the wrong answer. The correction
  (take deltas between consecutive rows) exists only in one agent's private
  memory note, not in the file itself.
- expires: 2026-09-21
- status: open

### R-25 — Session log signal-to-noise: the log records the orchestrator's intentions, not the agents' results
- type: residual
- source: PR #359 review round 1, ⚠️-6
- files: `.squad/log/2026-09-07-session.md`, `.claude/hooks/session-logger.sh`
- owner: devops
- level consequence: established by `reviewer-blind`'s independent pass at
  roughly a 25:214 signal-to-noise ratio; confirmed here as the second
  consecutive day with the same shape, which makes it a `session-logger.sh`
  defect rather than a one-off. `CLAUDE.md` directs the Lead to read these
  files when continuing prior work, so the defect degrades exactly the
  cross-session handoff mechanism it exists to support.
- expires: 2026-09-21
- status: open

---

## Extensions

Extensions are appended here, never edited in place, per the append-only
rule above. Each extension cites the entry it extends by its position in
this file and **may be authored only by the user** — an agent that extends
its own expiry has built the laundering shape the fields exist to prevent.
No extensions have been authored for this repository's ledger yet.
