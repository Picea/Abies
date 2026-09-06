# ⚖️ Review Verdict — squad-flow v2.1 delta (PR #358, `chore/0-squad-flow-v2-1`)

**Blind assessment:** `08-review-blind.md` (round 1; `reviewer-blind` does not re-run)
**Reviewed tree:** working tree on top of `6e28e7403f79ed02b93d0d1f3324152e97c7b628`
(`git diff HEAD` — 55 files, +3330/−730 — plus untracked
`.claude/hooks/lib/artifact_attribution.py` and
`.claude/hooks/lib/path_containment.py`)
**Round:** 3, closing pass — **the split the round cap prescribed, completed**
**Verdict:** ✅ Approved

---

## What this pass is, and what it is not

Round 3 reached the Merge Criterion's round cap of two and returned the split:
criterion (a) green, every round-2 finding fixed, and **three findings that
asked for registration rather than code**. The user chose *register now, then
merge* — one of the two dispositions the cap explicitly permits.

So this is **not a fourth review round**, and I am not treating it as one. Its
entire scope is: *did the three registrations land, and are they true of the
tree?* I re-ran the dimensions that the changed files could plausibly disturb
(Security, Documentation, Consistency, Testability) and did not reopen the seven
that passed in round 3. No finding below is new substance.

**Verification of scope, not asserted but measured.** Everything modified since
the round-3 drop was archived (`find -newer` against
`.squad/decisions/archive/2026-09/2026-09-06T13-33-43-review-pr358-round3.md`):

```
.claude/agents/reviewer-blind.md          ← claim 3 (tech-writer)
.claude/enforcement/refutations.md        ← claim 1 (security-expert)
docs/security/threat-model.md             ← claim 1 (security-expert)
.claude/hooks/enforce-review-blindness.sh ← claim 2 (devops)
.claude/hooks/enforce-track-blindness.sh  ← claim 2 (devops)
.claude/docs/decisions.md                 ← hook-written (round-3 drop merge)
.squad/.last-review-verdict               ← hook-written
.squad/log/{pass-cost,2026-09-06-session}.md ← hook-written
.claude/agent-memory/reviewer-reconcile/*    ← mine
```

**Exactly the five claimed files, plus hook-written state and my own notebook.
No scope creep, no opportunistic edits riding along with the registration.**
That is the property that makes a register-then-merge disposition safe, and it
holds.

---

## Reconciliation

### The three claims, verified

| # | Claim | Verified state | How |
|---|---|---|---|
| 1a | `security-expert` appended R-19 + R-20 under a new round-3 section | ✅ **true** — `## Residuals and bounds, round 3` at `refutations.md:472`, appended below B-1, originals untouched (append-only rule honoured). R-19 and R-20 each carry `type`, `source`, `files`, `owner`, `level consequence`, `expires`, `status: open` — the full schema, not a stub | ledger read + section-heading audit |
| 1b | R-19 states the instruction-vs-invariant level split | ✅ **true, and precisely** — names `dreamer-first-principles`'s grant (`Read, Grep, Glob, Write`) as making the four mediated tools *its whole read surface* → invariant-level, against `reviewer-blind`'s grant (additionally `Bash`) → **"instruction-level over `Bash` … and invariant-level over `Read`/`Grep`/`Glob`."** This is the distinction round 3 asked for, stated in the ledger's own words | code read |
| 1c | Threat model: TB-6 scope note, TM-016, OR-010 | ✅ **all three present.** Scope note under Trust Boundary 6 concedes the tool list is `dreamer-first-principles`'s entire read surface *and not* `reviewer-blind`'s; TM-016 is a full row (🔴 Open, Mitigation honestly reading **"None in the hooks"**); OR-010 carries owner, fix-direction-pending-architect, `expires: 2026-10-31`, ledger pointer R-19. R-19 ↔ TM-016 ↔ OR-010 cross-cite in all directions and the two `expires:` agree | diff read + cross-reference check |
| 2 | `devops` reworded brace expansion in both hook headers, citing TM-015 | ✅ **true in both**, and the rewording is *correct in sign* — `` `{a,b}`-style brace expansion … is NOT open``, explains the `("grouped", None)` → D20 `cwd` escalation, discloses the over-block cost (`a benign {a,b}/*.md is refused too`), cites `TM-015` ✅ Mitigated. Symlinks correctly **left** in the open sentence, where they are still accurate | code read + execution |
| 3 | `tech-writer` retitled `reviewer-blind.md` § 62, naming the `Bash` channel | ✅ **true, and it does more than retitle** — "the **Two** Things That Remain a Promise", both residues numbered, and the second one names the exact commands (`cat`, `sed`, `head`, `less`), states why the `tools:` grant includes `Bash` at all, and gives the agent a *catch-yourself* trigger rather than an abstract prohibition | code read |

### Re-verified by execution, because the new documentation asserts it

**The `Bash` gap is still live — which is correct, R-19 registers it, it does not
fix it.** I re-fired **all 19 hooks** with a `reviewer-blind` `Bash` payload of
`cat .squad/design/chore-0-squad-flow-v2-1/04-realist-plan.md`:

```
hooks fired: 19   refused: 0
```

Unchanged from round 3. R-19's `level consequence` and TM-016's Mitigation
column both describe this accurately, and neither overstates a fix.

**The brace-expansion claim the reworded headers now make is true**, both hooks,
both governed agents:

```
enforce-track-blindness.sh  / dreamer-first-principles
  {src,.claude/docs}/**/*.md    → exit 2      (grouped, refused)
  {a,b}/*.md        (benign)    → exit 2      ← the disclosed over-block cost
  src/**/*.cs       (control)   → exit 0
enforce-review-blindness.sh / reviewer-blind
  {src,.squad/design}/**/*.md   → exit 2
  {a,b}/*.md        (benign)    → exit 2
  src/**/*.cs       (control)   → exit 0
  */../../.squad/design/**      → exit 2      (climb control)
```

The headers and `TM-015` now agree with each other **and** with the code. Round
3's ⚠️-3 was exactly that three-way disagreement; it is closed.

**R-20's residuals are genuinely still open**, so the entry is not
over-registration: `grep -n realpath` across both hooks and
`lib/path_containment.py` finds hits **only inside the comment that says symlinks
would need it** — no call site. Sibling-worktree containment is unchanged.
(Per my own calibration note, I audit the ledger in both directions; neither new
entry registers something already fixed.)

**Suite re-run by me, not taken on report:** `823 passed, 0 failed`. Working
tree unchanged at 67 entries before and after, no `.orig`/`.rej`/`.tmp` residue.

### Where 08 and the narrative stand now

08's open question 3 — *has the user accepted the CI-6 shadow decision* — is
still open and still not answerable by a specialist. `.squad/.gate-shadow`
carries `expires: 2026-09-20`; today is 2026-09-06, so the shadow is live and
**no action is required for it to enforce.** Recorded for the fourth time, not
blocked on. Everything else in 08 is dispositioned in the rounds 1–3 tables.

### Claims I could not verify

- **The upstream spec.** Unverifiable by construction, as in rounds 1–3.
- **Whether the shadow decision has been accepted by the user** (above).
- **Whether the two worktrees on disk are transient.** Still two, both at
  `6e28e74`. Not load-bearing for any finding; see 💡.

---

## Critic's accepted risks

| risk | stated mitigation | present in the code? |
|---|---|---|
| — | — | **No `05-critic.md` exists for this slug.** |

Unchanged through all rounds: no design pass backs this changeset, so criterion
(a) is satisfied by substitution — the author-named property (the hook suite) is
executed by me and green at **823/0**. Recorded, not blocked on.

---

## Findings

### 🔴 Must Fix (blocks merge)

**None.** All three round-3 findings are closed: two by registration
(⚠️-1 → R-19 + TM-016 + OR-010; ⚠️-2 → R-20), one by the reword that its own
round's `TM-015` had already overtaken (⚠️-3). No regression, no scope creep.

### ⚠️ Should Fix

**None unregistered.** Every residual this changeset leaves behind now has an
entry with an owner, a level consequence and an `expires:` — R-19 and R-20 join
R-1/R-2/R-5/R-6/R-7/R-8/R-10/R-11/R-12/R-13 and bound B-1. Under the Merge
Criterion these are ⚠️-registered and do not block ✅.

Merge Criterion (b) is now **satisfied**, which is the single thing that
separated round 3's ⚠️ from this ✅.

### 💡 Nitpicks

- **`refutations.md:529` — R-20's own evidence command is self-stale.** The entry
  says *"`grep -n worktree .claude/enforcement/refutations.md` finds only R-5"*.
  Run against the file that now contains R-20, it returns **8 hits**, including
  R-20's own title. The meaning is recoverable (it describes the state that
  justified the entry) but it is written in the present tense as a reproducible
  check. Same shape as round 2's stale inventory count. Not worth an append on
  its own; worth folding into whichever pass next touches the ledger.
- **`reviewer-blind.md` cites the residual by file, not by id.** It says *"tracked
  as a residual in `.claude/enforcement/refutations.md`"* where the sibling
  charter `reviewer-reconcile.md` cites *"residual R-15"* by number. R-19 cites
  `reviewer-blind.md` in its `files:` list, so the link is one-way. Adding `R-19`
  is a two-character change for whoever is next in that file.
- **Carried from round 3, unchanged and still correct as recorded:**
  `validate-phase-artifact.sh:133–139`'s heading-level comment (fail-closed,
  cosmetic); nothing asserts CLAUDE.md § 4's list still matches the hook's `case`
  list (§ 4 does correctly declare the hook authoritative on drift);
  `appsettings.*` vs `appsettings*.json`; `invariant-chain.sh:507`'s stub loop
  keyed on an incidental `grep -q 'python3'`.
- **Two registered worktrees still on disk**, both full copies at `6e28e74`, so
  repo-wide `grep` still returns tripled hits. `git worktree remove` when the pass
  ends. I scoped every grep in this pass to the live tree to avoid it.
- `enforce-reviewer-readonly.sh` again refused my scratchpad writes; probes were
  heredoc-authored scripts under the session scratchpad invoked through `Bash`.
  Working as designed, noted so it is not rediscovered.

### ✅ What's Good

- **The three specialists coordinated on line numbers, and it shows.** R-20 cites
  `enforce-review-blindness.sh:56–72` and `enforce-track-blindness.sh:68–84`.
  `devops` reworded that exact block *after* the ledger entry was written, and I
  checked: `sed -n '56,72p'` and `sed -n '68,84p'` still land precisely on "What
  remains open, deliberately". Line citations across two authors' concurrent edits
  usually rot on contact. These did not.
- **The ledger's round-3 preamble closes the loop on the finding it does *not*
  register.** It states outright that ⚠️-3 is absent *because* `devops` is
  correcting the headers to match `TM-015`'s existing ✅ Mitigated row. A reader
  auditing "three findings, two entries" gets the answer in the ledger instead of
  having to reconstruct it — this is the failure mode of registration-based merges,
  pre-empted.
- **TM-016's Mitigation column says "None in the hooks."** The honest thing was
  available and taken. It would have been very easy to write "charter instructs the
  agent not to" in that column and let the row read as partially mitigated; instead
  the charter's role is described and then explicitly labelled as *not* enforcement.
- **Trust Boundary 6's scope note concedes the argument rather than defending the
  original text.** *"This boundary's own text is accurate for the tools it names —
  it agrees with the ledger only because it does not claim coverage of the one
  channel that differs between its two governed agents."* That is a boundary
  documenting the limit of its own claim, which is the thing round 3 said Trust
  Boundary 5 did well and Boundary 6 had not.
- **The charter reword is operational, not decorative.** Round 3 asked for "one
  sentence" naming the second residue. `tech-writer` instead gave the agent a
  concrete trigger — *"if you catch yourself about to `cat`, `sed -n`, or `grep` a
  path under `.squad/design/` … stop"* — and closed with why the discipline matters
  (*"holding this rule by discipline is what keeps it a tracked residual instead of
  a live hole"*). An instruction-level control is only as good as its salience, and
  this one is written to be noticed at the moment of temptation.
- **The reworded header discloses the cost of its own fix.** *"the measured cost is
  the opposite of a gap, a benign `{a,b}/*.md` is refused too."* Documenting an
  over-block is rarer than documenting a fix, and it is what stops a future reader
  from "correcting" the false positive and reopening the hole.
- **823 assertions, 0 failures, no residue** — verified by execution, unchanged
  from round 3, confirming the registration touched no behaviour.

---

## Metrics

- **Files reviewed this pass:** 5 changed since the round-3 drop (100% of the
  non-hook-written delta), against the full 55-file + 2-untracked changeset
  reviewed cumulatively across rounds 1–3
- **Lines changed (cumulative):** +3330 / −730
- **Test coverage:** 823 assertions, 0 failing — executed by me. **Unchanged from
  round 3, correctly** — this pass added documentation and ledger entries, not
  behaviour, so an unchanged count is the expected result and a changed one would
  have been the finding. Known coverage gaps, both now registered rather than
  silent: no assertion that a `Bash` read of design paths is refused for
  `reviewer-blind` (TM-016 names adding one as the fix direction); no assertion
  that CLAUDE.md § 4 and the hook's `case` list agree (💡)
- **Findings verified by execution:** the `Bash` channel (all 19 hooks re-fired),
  brace/climb/control globs on both hooks and both agents, the suite, the residue
  check, and the `find -newer` scope check. Remainder by code reading, `grep`,
  cross-reference audit and ledger audit in both directions
- **Round-3 disposition:** 0 🔴 · 3 ⚠️ → **3 closed** (2 registered, 1 fixed) ·
  1 💡 carried · **0 regressions** · **0 scope creep**
- **Dimensions run:** 11/11 across the cumulative review. Re-run this pass:
  Correctness, Consistency, Security, Testability, Documentation. Not reopened
  (passed round 3, untouched by this delta): Readability, Design, Performance,
  Boy Scout, DoD. Observability (8) and Spec-by-Example (5) remain N/A — no runtime
  services, no feature
- **Merge Criterion:** (a) **satisfied** — author-named property green at 823/0 ·
  (b) **satisfied** — every residual is now registered with owner, consequence and
  `expires:`; this is what changed since round 3 · (c) **satisfied** — 0
  regressions introduced · **the round-cap split is complete; the changeset is
  clear to merge**
