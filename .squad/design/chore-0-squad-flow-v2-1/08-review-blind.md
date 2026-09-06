# 👁️ Blind Review — squad-flow v2.1 delta (`chore/0-squad-flow-v2-1`)

**Reviewed:** `8b25458..6e28e74` (2 commits, 85 files, +9921/−1104) — `.claude/**`, `CLAUDE.md`, `.gitignore`, `.squad/**`
**History channel:** `.claude/hooks/git-history-namestatus.sh`
**Narrative reaching this context:** none. The dispatch prompt gave me the branch, the commit range, a one-line scope statement ("confined to `.claude/`, `CLAUDE.md`, `.gitignore`, `.squad/`") and the slug. No PR body, no issue text, no design artifact content. `.squad/design/` was refused by hook on every attempt; I deliberately did not route around it with `cat`, and the only thing I read out of it is the *filename* list already carried by the sanctioned name-status channel.

Where a finding says "verified by execution", I ran the hook against a synthetic payload and recorded the exit code. One probe (F-1) caused `enforce-reviewer-readonly.sh` to append a line to the tracked file `.squad/log/gate-shadow.md`; I restored it to its committed empty state with `git checkout --` and confirmed size 0.

---

## What this change does

It converts the squad framework's review and design discipline from **written instructions** into **executable gates**, and splits the single reviewer role in half.

**Roles.** `.claude/agents/reviewer.md` (293 lines) is deleted and replaced by `reviewer-blind.md` (190) and `reviewer-reconcile.md` (322). Two more agents are added: `scope-warden.md` (a mechanical check on the architect's `00-scope.md`, dispatched between the architect and the Dreamer tracks) and `curator-adversary.md` (an adversarial pass over the curator's proposals). The roster goes from 17 to 19 charters.

Old behaviour: one `reviewer` agent read the code, the plan, the critique, the spec and the PR body together, then issued a verdict. New behaviour: `reviewer-blind` reads only code and writes `08-review-blind.md`; `reviewer-reconcile` then reads that file *plus* the narrative, treating the narrative as claims to verify, and writes `09-review-verdict.md` and a decision drop. The stated property is temporal, not informational — the blind reading is on disk before any narrative is read, so it cannot be softened retroactively.

**Enforcement.** Eleven new hook scripts (~2,900 lines of bash+embedded Python) and a fourth PreToolUse matcher group in `settings.json`:

| Hook | Event | What it turns into a gate |
|---|---|---|
| `enforce-review-blindness.sh` | PreToolUse Read/Grep/Glob/mcp | `reviewer-blind` cannot reach `.squad/design/` |
| `enforce-track-blindness.sh` | same | Track A cannot reach recorded conclusions; Track B cannot reach Track A |
| `enforce-review-history-channel.sh` | PreToolUse Bash | `reviewer-blind` cannot run `git log/show/blame`, `gh pr/issue view` |
| `git-history-namestatus.sh` | *(not a hook)* | the sanctioned replacement channel — hashes, dates, `--name-status` only |
| `enforce-reviewer-readonly.sh` | PreToolUse Write/Edit/… | three agents confined to their own artifact; `.squad/.last-review-verdict` deny-by-default for everyone |
| `enforce-review-verdict.sh` | PreToolUse Bash | commit/push/merge require a PASS naming HEAD |
| `enforce-phase-order.sh` | PreToolUse Write | `03` needs `01`+`02`; `05` needs `04`; `09` needs `08` |
| `validate-phase-artifact.sh` | SubagentStop | shape checks on `01`, `02`, `05`, `06` |
| `lexicon-check.sh` | SubagentStop | blocks `01-track-a.md` naming a pattern, with a user-only override |
| `scope-warden.sh` | SubagentStop | scans `00-scope.md`, writes `00-warden-scan.md` |
| `validate-branch-name.sh`, `block-direct-commits-to-main.sh` | PreToolUse Bash | branch naming; no commits on main |

`scribe-decision-merger.sh` is substantially rewritten: `.squad/.last-review-verdict` now has one automated writer, its destination is derived from the payload `cwd` rather than any field of the drop, and the sha written is read fresh from `git rev-parse HEAD` and cross-checked against a mandatory full-40-hex `commit:` in the drop. `worktree_for_sha()` and a `--replay` CLI are deleted outright.

**Tests.** Three new suites (`blindness.sh` 1267, `invariant-chain.sh` 897, `phase-gates.sh` 375) sourced into the existing `run.sh`, which now reports **749 passing, 0 failing**. I ran all four; all green.

**Docs.** New: `pattern-lexicon.md` (97 blocking terms/phrases), `flow-changelog.md`, `.claude/enforcement/refutations.md`, three slash commands (`/review`, `/design`, `/curate`), `beast-mode-track-a` skill. `principles-enforcement.md` grows by 237 lines. `CLAUDE.md` is updated throughout for the split.

**Two things that are not framework wiring**, and that I would not have expected in this range:
- `.claude/agent-memory/reviewer/` — 19 files, ~600 lines of accumulated review learnings — is **deleted**, with no successor directory created.
- `.squad/design/undo-redo/00-scope-undo-redo.md` (86 lines) is added: a scope artifact for an unrelated product feature.

**`.gitignore`.** `[Ll]og/` and `[Ll]ogs/` are anchored to `/[Ll]og/`, `/[Ll]ogs/`, and three `!.squad/log/*.md` negations are added.

---

## Why it might be needed

Legible from the code, and the code is unusually explicit about it. The recurring argument, restated in six or seven hook headers in almost identical words, is that a rule stated as *"do not do X"* survives exactly as long as no one is under pressure, and that the framework had accumulated a set of load-bearing rules held up by nothing but prose. The specific failure the split reviewer targets is named directly: a reviewer who knows the intent reviews the intent, and after the fact you cannot tell a captured review from an independent one, because both look the same.

The `enforce-reviewer-readonly.sh` header supplies the sharpest version, and it is a good argument: two restrictions here are about an *object* rather than a *capability* (`reviewer-blind` must not write source but must write its own report; `scope-warden` must not fix `00-scope.md` but needs `Write` to file its report), so removing the tool cannot express the rule and the check has to live on the call. `reviewer-reconcile` declaring `memory: project` — which silently re-grants Write/Edit regardless of its `tools:` line — is cited as the concrete reason a tool grant cannot be trusted. I checked that: `reviewer-reconcile.md` declares `tools: Read, Grep, Glob, Bash` and `memory: project`, so the argument is real, not hypothetical.

What I **cannot** infer from the code is why this landed as one 85-file changeset rather than as separable passes, and why the reviewer's memory deletion and the `undo-redo` scope artifact travelled with it. Nothing in the diff explains either. That gap is itself a finding (F-6, F-13).

---

## Is this the right approach?

Broadly yes, and the analysis behind it is better than the average change of this size. The object-vs-capability framing is correct, the choice to put both the *check* and the *identity re-check* in the hook (settings matcher plus `agent_type`) is sound belt-and-braces, and the containment rewrite in the two blindness hooks — refusing an *ancestor* scope or a missing `path`, not just an exact match — closes a hole an exact-prefix matcher genuinely has. The decision to accept a named false-positive class (`docs/examples/.squad` refused for `reviewer-blind`) rather than paper over it is the right trade and is documented where a maintainer will hit it.

Three reservations about the shape rather than the intent:

**The headers have outgrown the code.** `enforce-reviewer-readonly.sh` is 512 lines, of which roughly 130 are executable; `enforce-track-blindness.sh` is 568 for ~180. The commentary is doing real work — it records what a control does *not* claim, which is unusually honest — but it has also become the place where claims are made that the code does not support (F-8, F-9) and where citations point at documents that do not exist in this repository (F-7). At this ratio the prose is no longer verifiable by reading the file it sits in, which is the property that made it worth writing.

**Command-text matching is used as a gate mechanism in three places.** `enforce-review-history-channel.sh`, `enforce-review-verdict.sh` and `block-direct-commits-to-main.sh` all classify a `Bash` call by regex over its command string. That is inherently a heuristic over an unbounded input, and F-2/F-3 are both instances of it failing on ordinary, non-adversarial command forms. The headers acknowledge the adversarial limit ("an agent not actively working around it") but the two holes I found are not adversarial — `git commit -am` and `git push origin HEAD:main` are how these commands are normally written.

**Deliberate non-enforcement at ship time.** The single most important new control — the verdict-cache write deny, which is what makes the commit gate mean anything — ships inside an active shadow window and is verifiably *not* enforcing today (F-1). That is a documented, reasoned choice with a hard expiry and an audit log, which is far better than the alternatives; but the net effect is that this changeset's headline invariant is, on the day it merges, an instruction with better logging.

A simpler alternative I considered and would not recommend: doing this via `git` hooks and branch protection instead of Claude Code hooks. `CLAUDE.md` and `refutations.md` both already say branch protection is unavailable on this repository's plan tier and name the relocation as a future pass, so the chosen level looks like the right one for the constraints — the problem is that the prose in a few places claims the higher level anyway (F-8).

---

## Problems

### F-1 — The verdict-cache write deny is not enforcing, so the commit gate is forgeable with the `Write` tool today

**Where:** `.claude/hooks/enforce-reviewer-readonly.sh` (shadow logic, `_shadow_active`/`SHADOW_EXEMPT_AGENTS`); `.squad/.gate-shadow`

`.squad/.gate-shadow` carries `created: 2026-09-06`, `expires: 2026-09-20`. Today is 2026-09-06. While that window is open, any `agent_type` other than `reviewer-blind`/`scope-warden` — i.e. 17 of the 19 charters — may write `.squad/.last-review-verdict` through `Write`/`Edit`; the hook logs and exits 0.

`enforce-review-verdict.sh` reads exactly that file as the sole authorisation for `git commit` on code paths, `git push` to a protected branch, and `gh pr merge`. So for the next fourteen days the framework's terminal control can be satisfied by writing two lines (`PASS`, `commit: <rev-parse HEAD>`).

**Verified by execution:**
```
$ echo '{"agent_type":"reviewer-reconcile","cwd":"…/Abies","tool_name":"Write",
         "tool_input":{"file_path":".squad/.last-review-verdict"}}' \
    | bash .claude/hooks/enforce-reviewer-readonly.sh ; echo $?
0
$ cat .squad/log/gate-shadow.md
2026-09-06T11:28:10Z SHADOW-ALLOW agent=reviewer-reconcile tool=Write
  target=.squad/.last-review-verdict session=<none> result=allow
  (would refuse once the shadow at .squad/.gate-shadow expires)
```

**Why it matters:** `CLAUDE.md` § 4 tells the reader the residual risk is the *Bash redirection* channel for nine Bash-holding roles. That understates it for the shadow window: the tool-mediated channel is open too, for seventeen roles, including the ten that hold no `Bash` at all and are therefore described everywhere else as unable to reach this file. The shadow rationale in `.squad/.gate-shadow` is coherent (CI-6 default applies because no clean-flip decision was recorded), and I am not arguing the shadow is wrong — I am recording that the changeset's central claim is currently false, that no document states the fourteen-day exposure in those terms, and that nothing outside the file's own `expires:` line will make anyone notice when it lapses.

### F-2 — `git commit -am` bypasses the commit gate entirely

**Where:** `.claude/hooks/enforce-review-verdict.sh`, the `if [ "$KIND" = "commit" ]` block

The gate decides whether a commit is "code-shaped" from `git diff --cached --name-only`, and short-circuits on an empty result:

```bash
staged="$(git -C "$repo" diff --cached --name-only 2>/dev/null || true)"
[ -z "$staged" ] && exit 0
```

`git commit -a` and `git commit <pathspec>` both stage *at commit time*, so the index is empty when the hook runs and every such commit is allowed regardless of content.

**Verified by execution** (working tree clean, index empty, no PASS on record):
```
$ echo '{"cwd":"…/Abies","tool_name":"Bash",
         "tool_input":{"command":"git commit -am \"x\""}}' \
    | bash .claude/hooks/enforce-review-verdict.sh ; echo $?
0
```
The same payload with a staged `.cs` file blocks with exit 2, so the gate works — it just cannot see this shape.

**Why it matters:** this is not an evasion, it is the most common way to commit. The hook's own header calls itself "the commit gate" and says self-approval "is exactly why this is a gate and not a reminder"; a gate with this shape open is a reminder with extra steps. The fix is small (`--cached` union `git diff --name-only` when `-a`/a pathspec is present, or simply refuse to classify and fail closed when the index is empty but the worktree is dirty).

### F-3 — `git push origin HEAD:main` bypasses the protected-branch gate

**Where:** `.claude/hooks/enforce-review-verdict.sh` (`branch="$(git … rev-parse --abbrev-ref HEAD)"`, then `case "$branch" in main|master|release/*)`); same shape in `block-direct-commits-to-main.sh`

The protected-branch decision is made from the *local* branch name, never from the refspec. Pushing a feature branch's HEAD straight onto `main` names no protected branch locally and is allowed.

**Verified by execution** (on `chore/0-squad-flow-v2-1`, no PASS on record):
```
$ echo '{"cwd":"…/Abies","tool_name":"Bash",
         "tool_input":{"command":"git push origin HEAD:main"}}' \
    | bash .claude/hooks/enforce-review-verdict.sh ; echo $?
0
```

**Why it matters:** with branch protection unavailable on this plan tier (stated in `refutations.md` § Clarifications), these two hooks are described as the only merge control. Both read the local branch; neither parses the refspec. `git push origin HEAD:main` and `git push origin mybranch:main` are ordinary commands, not contortions.

### F-4 — Every hook fails **open** if `python3` is unavailable or broken, contradicting explicit fail-closed claims

**Where:** all 16 hook scripts; the claim is loudest in `enforce-review-blindness.sh` and `enforce-track-blindness.sh`

Each hook does its real work in an embedded Python program invoked as:
```bash
reason="$(printf '%s' "$payload" | python3 -c '…' 2>/dev/null || true)"
…
[ -z "$reason" ] && exit 0
```
`2>/dev/null || true` also neutralises `set -euo pipefail`. Malformed *JSON* is handled inside Python and does fail closed, as the header promises. But a failure one level up — interpreter missing, wrong version, syntax error introduced by an edit, import failure — produces empty output and is indistinguishable from "allow".

**Verified by execution** (a `python3` stub on `PATH` that exits 127, `cat`/`printf` still present):
```
$ printf '{"agent_type":"reviewer-blind","cwd":"…/Abies","tool_name":"Read",
           "tool_input":{"file_path":".squad/design/x/04-realist-plan.md"}}' \
    | env PATH=$STUB /usr/bin/bash .claude/hooks/enforce-review-blindness.sh ; echo $?
0
```
The same payload with a working `python3` exits 2. So a denied read of a design artifact is allowed when the interpreter is broken.

**Why it matters:** the header of that file argues at length that a malformed payload must refuse "for every agent, not just reviewer-blind", because "a call that is loudly refused for everyone gets noticed; a leak that is silently allowed would not be". That reasoning applies verbatim one level up and is not applied there. The changeset already demonstrates it knows the pattern: `scribe-decision-merger.sh` adds an explicit `command -v timeout` check for precisely this reason ("without this check the refusal below sends a maintainer to debug git, not PATH"). No hook does the equivalent for `python3`, on which all of them depend absolutely — `grep -l "command -v python3" .claude/hooks/*.sh` returns nothing.

### F-5 — Gate 1's own output republishes the pattern lexicon into a directory Track A is allowed to read

**Where:** `.claude/hooks/scope-warden.sh` (the `section("Pattern names and recall grammar", …)` writer) vs. `.claude/hooks/enforce-track-blindness.sh` `DENY["dreamer-first-principles"]`

`scope-warden.sh` writes `.squad/design/<slug>/00-warden-scan.md` containing, for every lexicon hit in the scope, the **matched term verbatim and the full surrounding line**:

```python
section("Pattern names and recall grammar", names,
        lambda r: "- **L%d:** %s `%s`\n  > %s" % (r[0], …, r[2], r[1]), …)
```

Track A's deny list is:
```
.claude/docs/decisions.md, .claude/docs/decisions-archive/**,
.claude/docs/tech-stack.md, .claude/docs/pattern-lexicon.md,
.claude/agent-memory/**, .squad/decisions/**,
.squad/design/*/00-knowledge.md, .squad/design/*/02-track-b.md
```

`00-warden-scan.md` and the `scope-warden` subagent's `00-warden.md` are not on it, and both sit in `.squad/design/<slug>/`, the one directory Track A is explicitly told to read. So the artifact whose purpose is to *detect* pattern names leaking into the scope is itself an unguarded, in-scope-directory copy of those pattern names, produced automatically on every architect `SubagentStop`.

**Why it matters:** `.claude/docs/pattern-lexicon.md` is denied to Track A with the note that it is "literally, a list of pattern names". Gate 1 manufactures a per-pass excerpt of that same list, quoted in context, one directory away from Track A's required reading. This is new in this changeset — before `scope-warden.sh` existed, no such file was produced. I did not verify this by execution (doing so would have required reading `.squad/design/`); it is read off the deny table and the writer side by side.

### F-6 — The retired reviewer's entire memory is deleted, contradicting the principle the same commit adds

**Where:** commit `6e28e74` deletes `.claude/agent-memory/reviewer/MEMORY.md` and 18 learning files (~600 lines). `ls .claude/agent-memory/` confirms no `reviewer-reconcile/` directory was created.

`enforce-reviewer-readonly.sh` allow-lists `.claude/agent-memory/reviewer-reconcile/**` for the successor agent, so the intended destination exists in the rules but not on disk, and nothing was migrated into it.

The same changeset adds this to `scribe-decision-merger.sh`, as a deliberate, commented design choice:

```python
# Names with no charter that must still validate … Retiring an
# agent must not retroactively invalidate the record it left behind: a drop
# that was valid when written stays valid, and quarantining history would
# destroy exactly the evidence the register exists to keep.
#
#   reviewer — split into reviewer-blind + reviewer-reconcile (spec v2.1 WP-5).
LEGACY_AGENTS = {"reviewer"}
```

**Why it matters, specifically.** I cannot read the deleted content, but the sanctioned history channel gives me the filenames, and they are on point:

- `last-review-verdict-is-forgeable.md` (43 lines)
- `command-text-matching-is-not-a-gate.md` (31 lines)
- `shape-heuristics-are-defeated-by-one-line.md` (76 lines)
- `safety-caps-must-fail-closed.md` (35 lines)

Those four titles describe F-1, F-2/F-3, F-2 and F-4 respectively. The changeset that reintroduces those shapes deletes the notebook recording that this reviewer had already learned them. Whether or not the content is recoverable from git, the successor agent starts empty, and `memory-policy.md` describes these directories as the mechanism for exactly this kind of carry-over.

### F-7 — Hook headers and one user-facing refusal cite documents and sections that do not exist in this repository

**Where:** `enforce-reviewer-readonly.sh` (header and the `VERDICT` refusal body printed to the agent), `enforce-track-blindness.sh`, `enforce-review-blindness.sh`, `.claude/enforcement/refutations.md`

Checked against the repo:

| Citation | Reality |
|---|---|
| `docs/security/threat-model.md` Trust Boundary 6 | the file has exactly 4 trust boundaries |
| threat-model rows T-013, T-015, T-017 | `grep -nE "T-01[357]"` on that file: no matches |
| "ledger pointer: refutations.md entry 10" | `.claude/enforcement/refutations.md` § Refutation rows reads "None yet"; § Residuals and bounds reads "None yet" |
| `04-realist-plan.md:2054`, `07-handoff.md` BC-1/BC-2, `10-architect-ruling-classifier.md` § Q-G, `11-continuous-improvement-criterion.md` § 6, `09-review-verdict.md` rounds 4/5/6 | `git ls-tree -r --name-only HEAD -- .squad/design` returns exactly one path: `.squad/design/undo-redo/00-scope-undo-redo.md` |

Two of these are not merely comments. The refusal `enforce-reviewer-readonly.sh` prints *to the blocked agent* ends:

> `(This governs a path spelling … see docs/security/threat-model.md, Trust Boundary 6, T-017 — ledger pointer refutations.md entry 10.)`
> `See … docs/security/threat-model.md (Trust Boundary 6, T-013).`

An agent or maintainer following either pointer finds nothing.

**Why it matters:** these read as an upstream template's provenance chain imported without its referents. `refutations.md` is candid about part of this ("This section is carried over from the template … this repository's own ledger below carries no entries of its own yet"), which makes the *unqualified* citations elsewhere the inconsistency. The residual risks named as "tracked as T-017" are, in this repository, tracked nowhere.

### F-8 — `scribe-decision-merger.sh` justifies a design decision by citing sibling behaviour that sibling does not have

**Where:** `.claude/hooks/scribe-decision-merger.sh`, the `project_dir` comment block

```
# `enforce-review-verdict.sh` refuses outright in the identical condition
# ("CLAUDE_PROJECT_DIR is unset or not a directory -- refusing rather than
# guessing"); two hooks taking opposite directions on the same environment
# condition cannot both be right, and this hook's direction is the one that
# writes an authorisation token.
```

`enforce-review-verdict.sh` does no such thing. `grep -n CLAUDE_PROJECT_DIR` on it returns two hits, both inside a header comment saying the opposite — that paths resolve against the payload `cwd` and *never* against `CLAUDE_PROJECT_DIR`, because that variable "reads the wrong repository's verdict from inside a worktree". The quoted string is `scribe-decision-merger.sh`'s own error message, attributed to its sibling.

**Why it matters:** the observation ("two hooks taking opposite directions cannot both be right") is correct and the two hooks *do* now take opposite directions — the merger anchors on `CLAUDE_PROJECT_DIR` with no fallback, the gate anchors on payload `cwd`. The comment resolves that live disagreement by asserting agreement that does not exist, which means a maintainer reading it will not go looking for the divergence. A reader is entitled to treat a hook header as verified; this one is not.

### F-9 — Worktree drops are silently stranded, and the failure mode is a deadlock

**Where:** interaction of `enforce-reviewer-readonly.sh` (allows `reviewer-reconcile` → `.squad/decisions/inbox/**`, resolved against payload `cwd`) and `scribe-decision-merger.sh` (reads only `$CLAUDE_PROJECT_DIR/.squad/decisions/inbox`)

The merger's own header states this outcome plainly and calls it out as the case previous wording did not name: a drop authored from a worktree lands in `<worktree>/.squad/decisions/inbox/` and "is stranded, not merged, until a human moves it by hand". The header also records that this was *measured* — `reviewer-reconcile` declares no `isolation:` and a session run from a worktree observed its own `cwd` as the worktree.

**Why it matters:** naming a gap is good, but the downstream consequence is not named. If the drop never reaches the inbox, `.squad/.last-review-verdict` is never written, and `enforce-review-verdict.sh` then blocks the commit with *"no verdict has been recorded at all"* — after a review that actually happened and passed. The agent's only visible route past that message is to write the cache directly, which is the one thing the framework tries hardest to prevent (and which F-1 shows currently succeeds). A fail-closed control whose most likely failure pushes the operator toward the forgery path is worth more than a comment.

### F-10 — Three SubagentStop hooks attribute artifacts by newest-mtime across *all* passes

**Where:** `validate-phase-artifact.sh`, `lexicon-check.sh`, `scope-warden.sh` (all: `for slug in os.listdir(design): … found.sort(); … = found[-1]`); also `session-logger.sh`'s pass-cost slug resolution

None of these hooks knows which pass the finishing subagent belonged to. Each scans every slug directory and picks the most recently modified matching file. Three consequences:

- **Wrong-pass attribution.** With two passes in flight, `scope-warden.sh` writes `00-warden-scan.md` into whichever pass has the newer `00-scope.md`, not the one the architect just ran.
- **False green.** If a phase agent finishes *without writing its artifact*, the hook validates the previous pass's file and passes. That is precisely the "a phase that ran vs. a phase that returned" distinction the header says these checks exist to draw.
- **Spurious re-run.** The architect's close-out (step 10) re-fires `scope-warden.sh`, re-scanning and overwriting a `00-warden-scan.md` the user already gated on. On the fast path — where no `00-scope.md` is written at all — it attaches a scan to an unrelated older pass.

`enforce-phase-order.sh`, by contrast, derives the pass directory from the *target path* of the Write. That is the right shape; the SubagentStop hooks have no equivalent signal available and guess instead.

### F-11 — CI does not run the suite on changes to most of what the suite asserts about

**Where:** `.github/workflows/claude-hooks-tests.yml` `paths:` filters (unchanged by this range) vs. the real-repo files the new suites read

`run.sh` sources all three new suites, so CI does execute them (749 assertions; I ran the whole thing locally, all green). The problem is the trigger. The filters are `.claude/hooks/**`, `.claude/agents/**`, `.claude/docs/decision-schema.md`, `.claude/statusline.py`, and the workflow itself. Enumerating the suites' `$REPO_ROOT`-relative reads outside `.claude/hooks/`:

```
.claude/settings.json                        ← every "wiring:" assertion
.claude/commands/*.md                        ← command-existence assertions
.claude/docs/pattern-lexicon.md              ← layout assertion
.claude/docs/memory-policy.md                ← "names the current artifact set"
.claude/docs/principles-enforcement.md       ← "the warning-vs-control criterion is recorded"
.claude/skills/beast-mode-design/SKILL.md    ← contract-table assertions
```

None of those six are in the filter. This changeset modified `settings.json` (77 new lines, a whole new matcher group), added `pattern-lexicon.md` and the three commands, and grew `principles-enforcement.md` by 237 lines — and simultaneously added assertions that depend on them, without extending the filter. A future PR that only edits `settings.json` can unwire `enforce-reviewer-readonly.sh` and go green.

### F-12 — `.gitignore` un-ignores every nested `log/` and `logs/` directory in the repository

**Where:** `.gitignore`, `[Ll]og/` → `/[Ll]og/`, `[Ll]ogs/` → `/[Ll]ogs/`

The stated reason is correct and the bug it fixes is real: an unanchored `[Ll]og/` excludes `.squad/log/`, and git cannot re-include a file whose parent directory is excluded, so the `!.squad/log/*.md` negations below would never have fired.

The comment then asserts the anchored form "keeps the original intent (.NET build diagnostics dumped to `<root>/log/`)". Unanchored `[Ll]og/` is the stock Visual Studio `.gitignore` entry and its intent is *any* build-log directory at any depth, not just the root — this repo has 20+ projects. After this change, a `log/` directory produced under any project is untracked-but-offered rather than ignored.

A narrower fix that does not have this side effect: keep the patterns unanchored and add `!.squad/log/` (re-including the directory itself) before the file-level negations. Flagging the scope of the change, not disputing that the original was broken.

### F-13 — Two unrelated payloads in an infrastructure changeset, one of which the new machinery cannot see

**Where:** `.squad/design/undo-redo/00-scope-undo-redo.md` (added, 86 lines); `.squad/log/2026-09-02-session.md` (285), `.squad/log/2026-09-03-session.md` (81)

The scope statement I was given confines this branch to `.claude/`, `CLAUDE.md`, `.gitignore` and `.squad/`, which is technically satisfied — but a product feature's design scope (`undo-redo`) is not squad-flow infrastructure, and it is the only design artifact tracked in the repository.

Separately, and verifiable without reading it: the filename is `00-scope-undo-redo.md`, not `00-scope.md`. All three new hooks that key on the scope — `scope-warden.sh`, `enforce-phase-order.sh`'s skip-detection, `validate-phase-artifact.sh`'s `INV-n` cross-check — look for `00-scope.md` exactly. That pass's scope is invisible to the entire gate-1 mechanism this changeset installs. Either the artifact is misnamed, or the hooks' assumption about artifact naming is narrower than reality; the two contradict each other and the contradiction is inside one commit.

### F-14 — Smaller items

- **`curator-adversary` is not in `CLAUDE.md`.** The charter, `/curate`, `decision-schema.md`, `principles-enforcement.md`, `flow-changelog.md` and the test suite all know it; the Lead's roster table and routing section — the only place delegation is described — do not. Since subagents cannot spawn subagents, an agent absent from the Lead's table cannot be reached except through `/curate`. (`scope-warden`, `reviewer-blind` and `reviewer-reconcile` were all added correctly.)
- **`/review` states the wrong shadow expiry.** It says "until `expires:` in `.squad/.gate-shadow` (2026-09-19)". The file says `expires: 2026-09-20`. A slash command hard-coding a transient date will also be stale in two weeks regardless.
- **Stale header.** `enforce-review-history-channel.sh` says *"`reviewer-blind` is introduced in a later stage of this work (WP-5) … This hook is written now, keyed by name, and is inert until that charter lands."* The charter landed in the same commit range; the hook is live (it blocked my own `git log` probe).
- **~100 lines of security-critical logic duplicated verbatim.** `leaves`/`segs`/`resolve`/`classify`/`reaches`/`reach_hit`/`glob_root` are byte-identical between `enforce-track-blindness.sh` and `enforce-review-blindness.sh` apart from docstrings — I diffed the extracted regions to confirm. Both headers cross-reference each other for "the full derivation", which is an admission the code is one thing in two places. A fix applied to one will not reach the other, and nothing tests that they agree.
- **Dead code and a stale docstring in `validate-phase-artifact.sh`.** `findings = re.findall(…)` is computed and never used. The `AGENTS` comment says `agent_type -> (artifact, [(label, test, remedy)])`; the dict maps to a bare string.
- **A blocking check that almost cannot fail.** `05-critic.md`'s "concrete failure scenario" test is `re.search(r"failure scenario|given\b|when\b.*then\b|reproduc", …)`. Bare `given\b` matches any prose use of the word "given". Likewise `01-track-a.md`'s emptiness test splits on the *first* occurrence of "reasoning trail" and measures everything after it — usually the rest of the document — so a heading near the top with nothing under it still passes.
- **Command-regex gaps beyond F-2/F-3.** `\bgit\s+(?:-[^\s]+\s+)*log\b` does not match `git -c core.pager=cat log` (the `-c` value is a separate word), so that form reaches `reviewer-blind`'s context. Conversely `block-direct-commits-to-main.sh` uses `case "$command" in *"git commit"*`, which fires on any command merely *containing* that text — `echo "git commit"` is blocked on `main`.
- **Lexicon false-positive load.** 77 terms + 20 recall phrases, matched per sentence, blocking. The recall list includes bare `commonly`, `typically`, `traditionally`, `conventionally`, `well known`. "The constraint typically holds" blocks Track A. The override is well designed (user-only, logged to a never-rotated calibration file) and this is clearly a deliberate detection-over-prevention trade — but the first several passes will be dominated by overrides, and `.squad/log/lexicon-hits.md` currently has no rows to calibrate against.
- **`.claude/hooks/tests/p3_overmatch.py`** (86 lines, added) is referenced by nothing — not `run.sh`, not the three suites, not the workflow.
- **`CLAUDE.md`'s definition of "code-shaped" is broader than the gate's.** The prose names `global.json`, `Directory.Build.targets` and `*.yml`/`*.yaml` workflows; `enforce-review-verdict.sh`'s `case` list has none of the three (it has `.github/workflows/*`, but not `*.yml` generally, and not `Directory.Build.targets` or `global.json`). Two definitions of the same term, one advisory and one executable.
- **Markdown nit.** In `CLAUDE.md` § 2, the new paragraph under **Review** is inserted between the bold label and the bullet list that used to follow it, leaving the list orphaned from its heading.

---

## What I could not determine from the code alone

1. **Was the deletion of `.claude/agent-memory/reviewer/` intentional, and was anything migrated?** Nothing in the diff says. The successor directory is allow-listed but empty. If the intent was "the split invalidates the old notebook", that argument is not made anywhere, and it sits directly against the `LEGACY_AGENTS` reasoning added in the same commit (F-6).
2. **Why is `.squad/design/undo-redo/00-scope-undo-redo.md` in this range**, and is its filename deliberate or a mistake? Either answer implies a different fix (F-13).
3. **Was the CI-6 shadow window a user decision or an agent default?** `.squad/.gate-shadow` says the user "was not available to record a clean-flip decision, and CI-6 applies its default when no such decision is recorded" — i.e. an agent chose non-enforcement on the user's behalf for the framework's central control. Whether the user has since seen and accepted that, and whether anything will surface the 2026-09-20 lapse, I cannot tell (F-1).
4. **Do the cited threat-model rows and design artifacts exist somewhere out of reach?** Several headers reference "the flow specification, which is maintained outside this repository". If T-013/T-015/T-017 and the `pathless-read-blindness` pass live there, the citations are merely unresolvable *here* rather than fabricated — but a refusal message printed to an agent in *this* repo still points at nothing (F-7).
5. **Were F-2, F-3 and F-4 known and accepted, or missed?** `CLAUDE.md` § 4 discusses the `Bash`-redirection residual at length and `principles-enforcement.md` gained a "Three Levels" section, so the changeset clearly reasoned about gate completeness. None of the three appears in any accepted-risk list I can reach. If they were accepted, I would expect them named in `refutations.md` § Residuals — which reads "None yet".
6. **Is the `00-warden-scan.md` leak (F-5) a known trade?** Every other Track A denial is argued explicitly. This one is not mentioned at all, which reads more like an oversight than a decision, but the design pass may have considered it.
7. **Was the `enforce-review-verdict.sh` / `scribe-decision-merger.sh` `cwd`-vs-`CLAUDE_PROJECT_DIR` divergence decided deliberately?** The merger's comment (F-8) suggests someone believed the two agreed. If it *was* deliberate — register project-global, cache per-tree — then the justification is right and only its supporting citation is wrong, which is a much smaller fix.
8. **What is the intended lifecycle of `.squad/log/gate-shadow.md`?** It is tracked and empty. Nothing appends to it except a control that is not enforcing; once the shadow lapses it will never gain a row. Whether it should then be deleted, or kept as evidence, is not stated.

---

*No verdict is issued here. Severity, the eleven dimensions, and reconciliation against the narrative and the accepted-risk list belong to `reviewer-reconcile`.*
