# ⚖️ Review Verdict — `undo-redo` design record (PR #359, branch `docs/0-undo-redo-design`)

**Blind assessment:** `08-review-blind.md` (written before any narrative was read; covered ~25% of the changeset — `.squad/design/` was denied to it by hook)
**Range:** `f0cb682bedb44d0ef9ca8a368ba0dfca0bc0b2b0..66379e7e2f9e7e5cbd5c3554369f1d5177fca176`
**HEAD reviewed:** `66379e7e2f9e7e5cbd5c3554369f1d5177fca176`
**Round:** 1
**Verdict:** 🔴 Changes Requested

Nothing here disputes the engineering. The design record is the best-grounded
artifact set this repository has produced, and I verified enough of it to say so
from evidence rather than impression. What blocks is the **record's own
metadata and provenance** — the Merge Criterion's clause (b): several verified
findings are not registered anywhere, and one registration that exists points at
a section of a file that does not exist.

---

## Reconciliation

### Where 08 and the narrative agreed

On everything mechanical. The blind reviewer checked twenty citations from the
drops into source it could read and found twenty accurate; I spot-checked the
same class inside `.squad/design/` and found the same discipline. All 67 files
are `.md`; no `.cs`, `.js`, `.csproj`, workflow or `appsettings.*` is in the
range. `bash .claude/hooks/tests/run.sh` → **823 passed, 0 failed** at this HEAD,
which is the changeset's stated property and it is green.

### Where they diverged, and what the code said

**08's P2 — "`decisions.md` cites an ADR and a trust boundary that do not
exist."** 08 could not tell whether these were aspirational or stale and said so.
They are **aspirational, and the record says so explicitly** — just not in the
document 08 could read:

- `04-realist-plan.md:1805` — step 13, `[ ]` unchecked, `→ tech-writer`:
  "ADR-030, docs/concepts/undo-redo.md, docs/guides/adding-undo.md, API
  reference, ADR-008 line 85 amendment…"
- `04-realist-plan.md:1865` — step 16, `[ ]` unchecked, `→ security-expert`:
  "…and Trust Boundary 7 — Runtime state-retention boundary (SEC-6) … Plus the
  hardening-backlog entries."
- `07-handoff.md:273` (SEC-6 → step 16), `:406` (step 13 → PR 7), and § 5.1,
  which carries the **full text** of Trust Boundary 7 "as `security-expert`
  wrote it" for step 16 to apply verbatim.

So this is **not** a missing-artifact finding. It is a legibility defect confined
to two sentences — see ⚠️ 2. 08 asked the right question and was right that it
could not answer it; the answer exonerates the pass and convicts the wording.

**08's P3 — "recorded only inside a design drop."** Half right. It is also in
`07-handoff.md:564` § 9 *Follow-on candidates*, with owner `devops`. But that
section's own heading is "**named, not scheduled**", and I confirmed it is in no
GitHub issue (`gh issue list --state all --limit 100` — nothing matches), not in
`hardening-backlog.md`, and not in `refutations.md`. So 08's substance stands.

I also found the defect is **wider than either the record or 08 states**. Not two
sources are unregistered but six, and `AddSource("Picea.Abies")` matches *no
`ActivitySource` in the repository at all*:

| declared `ActivitySource` | registered? |
|---|---|
| `Picea.Abies/Runtime.cs:88` — `"Picea.Abies.Runtime"` | ❌ |
| `Picea.Abies/Subscriptions/Manager.cs:32` — `"Picea.Abies.Subscriptions"` | ❌ |
| `Picea.Abies.Server/Page.cs:57` — `"Picea.Abies.Server.Page"` | ❌ |
| `Picea.Abies.Server/Session.cs:111` — `"Picea.Abies.Server.Session"` | ❌ |
| `Picea.Abies.Server.Kestrel/WebSocketTransport.cs:54` | ❌ |
| `Picea.Abies.Server.Kestrel/Endpoints.cs:44` | ❌ |
| `Picea.Abies.Server.Kestrel/OtlpProxyEndpoint.cs:52` | ✅ (templates only) |

Pre-existing, and out of scope for a docs-only PR — but it must be registered,
not left as a bullet in a handoff's explicitly-unscheduled list.

**08's P1 — "some agents read a clock and some invented a plausible-looking
number."** The pattern is real; the attribution is not. `critic` and `architect`
**have no `Bash` tool** (`.claude/agents/critic.md`, `.claude/agents/architect.md`
— `tools: Read, Grep, Glob, WebSearch, WebFetch, Write`). They cannot run
`date -u`. The correlation is exact:

| drop author | has `Bash`? | `created:` accuracy vs. merge time |
|---|---|---|
| `reviewer-reconcile` | yes | +39 s |
| `lead` | yes | +72 s |
| `critic` ×3 | **no** | `T184500Z`, `T000000Z`, `T000000Z` |
| `architect` ×2 | **no** | `T120000Z`, `T184500Z` (both future-dated) |

This is a **framework defect, not agent dishonesty**: `decision-schema.md`
requires a field five of the drop-writing roles are structurally unable to
produce, and `validate()` checks format, never plausibility, so it always passes.
It is also **pre-existing** — `2026-09-02T19-54-17-review-pr355-round5.md` carries
`created: 2026-09-02T20:30:00Z`, 36 minutes after its own merge, five days before
this branch existed. Per the Merge Criterion that makes it registrable rather
than blocking-on-its-own — but it is not registered, which is what blocks.

Two of 08's three stated harms do **not** materialise, and I checked rather than
assumed: `squad-rotate.py` ages files by mtime (`file_age_days`), not by
`created:`, so archival eligibility is untouched; and `decisions.md` lists the
drops in *merge* order, which is the true chronology (`:1954` → `:2031` → `:2105`
→ `:2158` → `:2245`). The out-of-order reading appears only under an `id` sort.
08's third harm — id-collision risk from two `T000000Z` values — stands.

### 08's P9, settled

`.squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md` (+86) is a **pure
append**: `@@ -242,3 +242,89 @@`, zero deletions. It is headed "**Appended
2026-09-06, not a re-review**", carries the ✅ forward unchanged, states the three
things it verified and, in its own "Claims I could not verify" section, states
what it inferred rather than diffed. Amending a closed verdict artifact is a
record-integrity question by shape; this instance is benign and well-executed.
No finding.

### 08's P7, and what only I could check

08 flagged that `flow-changelog.md`'s claim — gate 1 caught two prior-work leaks
a human review passed — rests entirely on files inside `.squad/design/`, so the
one role positioned to check a self-serving governance claim is the one role that
cannot. That is a real property of the split and worth keeping.

**I checked it, and the claim is true.** From the deleted draft at the base
commit (`git show f0cb682:.squad/design/undo-redo/00-scope-undo-redo.md`):

- line 25 — "It is **the starting point** for this work, not something to replace
  or duplicate."
- line 74 — "The problem has **a well-known conventional answer**…"

Both match the changelog's description. The final `00-scope.md:64` inverts the
first outright — "It is **not** a starting point, a foundation, or a thing to…" —
and the second framing is gone entirely. Gate 1 did what the entry says it did.

But the *evidence* is now unreachable, and that is a finding in its own right —
see ⚠️ 4.

### Claims I could not verify

1. **The design's engineering merits.** I verified the record's internal
   consistency, its citations and its disposition of every blocker. Whether
   `WithHistory` is the right design is the Dreamers', convergence's, the
   Critic's and the user's call across five gates — not mine to re-litigate, and
   I did not.
2. **Whether the commit was amended or recreated** (08's P10). `pass-cost.md`'s
   final row is `2026-09-07T10:03:32Z`, seven minutes after the commit's author
   date. Consistent with the row being written by `reviewer-blind` *after* the
   commit — the row names slug `undo-redo-design-record` and agent
   `reviewer-blind` — which is the innocent reading and the one I'd expect. Not
   a finding; noting it because 08 asked.
3. **Why this lands as a standalone docs branch** (08's Q6). The PR body answers
   it — "Landing it before implementation lets PR 0 (the locked spec) and the
   seven implementation PRs reference stable artifact paths." That is a claim I
   can't test, but it is coherent with `04-realist-plan.md`'s seven-PR structure
   and I have no reason to doubt it.

---

## Critic's accepted risks

The fourth pass returned **APPROVED WITH MITIGATIONS** (five 🟠, five 🟡); the
post-close-out confirmation pass returned **CONFIRMED WITH A BOUNDED LIST — two
🔴, four 🟠, seven 🟡**. Those two 🔴 are the ones that matter here, because a
conditionally-accepted risk whose condition went unmet is a blocker.

| risk | stated mitigation | present in the record? |
|---|---|---|
| **B10** — the seal keys on `Picea.Abies.UrlChanged`; a WASM adopter passing its own message type gets no seal, **with every property still green** | (1) state the obligation in the spec; (2) fix `README.md:183-190`; (3) analyzer rule = out of pass | ✅ (1) landed verbatim as `06-spec.md:1351` obligations line **17**, in line 2's register. (2) assigned to `tech-writer` step 13 and named in the architect's navigation drop `high:`. (3) recorded in `07-handoff.md` § 9 as a follow-on. Disposition table at `07-handoff.md:452`. |
| **B11** — *"After the first `record`, navigation classifies like any other message"* survives in both places marked *implement from this* | correct the clause in `04-realist-plan.md` § Origin re-basing **and** in `07-handoff.md` standing decision 7 | ✅ Both. `04-realist-plan.md:1230-1240` states the seal rule and flags the superseded clause; `07-handoff.md:207` corrects standing decision 7 and adds "Do not implement from any copy that still carries it." Marked "Discharged; nothing further owed" at `:453`. |
| **S25(a)/(b)** — two sentences in `04-realist-plan.md` are false and the plan will not be revised again | precedence rule (`06-spec.md` lines 4 and 15 win) stated where an implementer will meet it | ⚠️ **Partially.** The precedence rule is in `07-handoff.md:431-432` (dispatch table) and `:517-524` (residual risks, naming the failure mode explicitly: *"if an implementer reads only the plan, they will implement the wrong narrative"*), and in the architect drop's `high:`. But `04-realist-plan.md:195-199` and `:408-410` carry **no in-place marker** — a reader who lands there directly sees a confident false sentence. The risk is honestly registered, not mitigated. Accepted as documented; recorded here so it is not rediscovered. |
| **S31** — obligations table numbered 14 vs 16 across plan and spec | renumber the plan's row 14 → 16 and the header cross-reference at `:32` | ✅ Both. Plan `:32` reads "line **16** (renumbered from 14 per the confirmation pass's S31)"; `:2048` is row 16; `06-spec.md:1351` is row 17. Sequences match. |
| **S29** — superseded-branch retention is session-lifetime, not `Depth`-bounded | Trust Boundary 7 wording enumerating three retention shapes with the bound that applies to each | ✅ Text written and quoted in full at `07-handoff.md` § 5.1, with an explicit instruction that `room-security.md:589`'s wrong sentence "**must not be re-quoted as written**". Landing in `threat-model.md` is step 16 — pending, correctly. |
| **Item 6 / S20** — held anchor not added to SEC-3(b)'s reachability set | named in Trust Boundary 7 + existing DEBUG row, bracket-bounded, test at step 8(m), guide sentence at step 13, backlog fast-follow at step 16 | ✅ All five sites present in plan and handoff, with the specialist's overrule of the Critic recorded as such. |

**Structural checks on the artifacts themselves** (what `validate-phase-artifact.sh`
would have enforced, run by hand because the hook needs a live transcript path):
`00-scope.md` declares INV-1…INV-7; `06-spec.md` references all seven (12/35/12/10/11/8/19
hits). `01-track-a.md:489` has its Reasoning Trail. `02-track-b.md:404` has Known
Failure Modes. No `.lexicon-override` exists and `.squad/log/lexicon-hits.md` is
empty — Track A passed the lexicon gate cleanly rather than by waiver.

Agent-memory hygiene: **zero orphaned notes** (every file appears in its
directory's `MEMORY.md`) and **zero dangling `[[wikilinks]]`**, checked
mechanically across all nine directories.

---

## Findings

### 🔴 Must Fix (blocks merge)

- **[Merge Criterion (b), repo-wide]** — **Four verified findings are registered
  nowhere.** `.claude/docs/principles-enforcement.md` § *The Merge Criterion*
  makes ✅ conditional on "every finding that is not a regression of a stated
  property is registered — in `.claude/enforcement/refutations.md`, or the threat
  model for a security residual — with an **owner**, a **level consequence**, and
  an **`expires:` date**, written *before* the verdict." None of ⚠️ 1, ⚠️ 3, ⚠️ 5
  or ⚠️ 6 below is registered. Each is pre-existing and therefore registrable
  rather than fixable-here — registration is the whole remedy, and it is the one
  thing I am forbidden to do for you (`enforce-reviewer-readonly.sh`: the
  reviewer classifies, the author registers, the reviewer verifies).
  **Suggested fix:** four ledger entries in `.claude/enforcement/refutations.md`,
  appended after R-21 as R-22…R-25, following R-21's own shape. Suggested owners:
  `devops` for the OTEL gap and the `created:` field, `devops` for the two
  `session-logger.sh` defects. *Evidence: criterion quoted from
  `principles-enforcement.md`; absence confirmed by reading `refutations.md` in
  full at this commit — R-21 is the last entry and the ledger is gap-free R-1…R-21.*

- **[`.claude/enforcement/refutations.md`:550]** — **R-21's `source:` cites a
  section that exists in no file in the repository.** It reads:
  `` (`.squad/design/undo-redo/00-warden.md` § "Findings on Adjacent Artifacts" → "Production ADR: `docs/adr/ADR-008-immutable-state.md:85`") ``.
  The committed `00-warden.md` is titled "**Scope Warden — undo-redo (re-run)**",
  its verdict is **CLEAN**, its only sections are "Nothing found in" and the gate
  question, and it closes with "**0 findings**". There is no "Findings on
  Adjacent Artifacts" heading anywhere in the tree
  (`grep -rn "Findings on Adjacent Artifacts" . --include=*.md` → one hit, the
  citation itself). `git log --all -- .squad/design/undo-redo/00-warden.md`
  returns a single commit, so no earlier version exists to recover.
  **Why it matters.** The Merge Criterion promotes `refutations.md` from a note
  file to a gating document: an entry there is what converts a blocking finding
  into a non-blocking one. An entry whose evidence pointer does not resolve
  cannot be audited, and this is the first entry in the ledger for which that is
  true. R-21's *substance* is sound and I verified it independently —
  `enforce-track-blindness.sh:277-290` lists eleven `DENY` globs for
  `dreamer-first-principles` and none of them is `docs/adr/**`
  (`grep -n "docs/adr" enforce-track-blindness.sh` → no output). So the fix is
  the citation, not the entry.
  **Suggested fix:** repoint `source:` at evidence that survives — the two leaked
  sentences quoted inline (`00-scope-undo-redo.md:25` and `:74` at `f0cb682`),
  plus the direct `enforce-track-blindness.sh:277-290` read the entry already
  cites under `level consequence`.

- **[`.claude/docs/flow-changelog.md`:40-41]** — **The entry that grades gate 1's
  effectiveness misstates gate 1's data flow.** It reads "`scope-warden`,
  **reading `00-warden.md`'s category**". The warden *writes* `00-warden.md`; it
  *reads* `00-warden-scan.md`. Both committed files say so in their own text —
  `00-warden-scan.md`: "belongs to the `scope-warden` subagent, which reads this
  file and writes `00-warden.md`" — as do `CLAUDE.md` § 3 rule 2 and
  `memory-policy.md`'s artifact table.
  **Why it matters.** This is the one entry in the framework's governance record
  whose purpose is to certify that the mechanical-plus-judgement split earns its
  place, and as written it has the judgement half reading its own output. The
  claim it certifies is true — I verified it — which is exactly why the sentence
  describing it should be too.
  **Suggested fix:** "reading `00-warden-scan.md` and adding the category no
  regex reaches". One clause. *Evidence: both artifacts read in full; `CLAUDE.md`
  § 3 rule 2 quoted.*

### ⚠️ Should Fix

1. **[`.claude/docs/decisions.md` + `.squad/decisions/archive/2026-09/`]** —
   **Five of seven new drops carry unreadable-clock `created:` values, two of
   them dated after the commit that introduces them.**
   `architect-20260907T184500Z-undo-redo-navigation` declares 18:45:00Z; the
   commit is authored 09:56:59Z and the drop merged at 09:34:55Z. Root cause and
   pre-existence are established in *Reconciliation* above. The residual harm is
   `id` collision: two drops in this commit already use `T000000Z`, and
   `decision-schema.md` requires `id` globally unique — a third `critic` drop on
   either day with the same slug collides outright.
   **Suggested fix — two parts, and the second is the real one.** (a) Do *not*
   rewrite the five `id:` values: they are documented stable anchors
   (`memory-policy.md` § *Anchors and stable IDs*) and are already cited from
   three places outside the archive
   (`.claude/agent-memory/architect/undo-redo-withhistory-decision.md:9`, and the
   navigation drop's `references:` and body). Correct only the two **impossible**
   `created:` values to their merge times, which changes no anchor. (b) Close the
   class at its source: `scribe-decision-merger.sh` already computes the accurate
   UTC timestamp — it names the archive file from it (`:1202`, `:1237`) — so
   having the merger stamp `created:` from its own clock removes the requirement
   that a Bash-less agent guess. Register as a residual either way.

   **I reproduced this defect while writing this review, and it changes the
   finding.** My own drop below declares `created: 2026-09-07T12:15:00Z`. The
   merger archived it as `2026-09-07T10-25-28-review-pr359.md` — I was wrong by
   1 h 50 m, having written local time from context instead of reading `date -u`,
   which I hold and the `critic` and `architect` do not. So the root cause is
   **not** only "five roles cannot read a clock". It is that the schema asks
   every author for a value at *authoring* time that is only reliably knowable at
   *merge* time — and `scribe-decision-merger.sh` already knows it, because it
   names the archive file from exactly that value. An agent holding `Bash` still
   has to remember to use it, at the one moment nothing prompts them to. That
   makes fix (b) the real remedy and fix (a) cosmetic: a field the toolchain can
   compute should not be a field the author is asked to assert. My drop's own
   `created:` is wrong in the merged register and I cannot correct it —
   `enforce-reviewer-readonly.sh` denies me `.claude/docs/decisions.md` and the
   archive — so please correct it alongside the other two, to `2026-09-07T10:25:28Z`.
   The `id:` should stay as merged: it is already an anchor, and destabilising it
   is the harm this finding is about.

2. **[`.claude/docs/decisions.md`:2172, 2169; `.claude/agent-memory/architect/MEMORY.md`:6]** —
   **Deliverables written as accomplishments in the authoritative register.** The
   Security paragraph reads "Security: SEC-1 … SEC-7, Trust Boundary 7 (three
   retention shapes…), two threat-model rows … and two hardening-backlog
   fast-follows" — a list of outputs, in a drop where *every other* forward-looking
   item is marked as such ("the spec file … cannot compile until the end of plan
   step 6", "no demo adopts `WithHistory` this pass"). `targets:` additionally
   lists `docs/adr/ADR-030-…md`, which is within schema — `targets:` is
   documented as "paths/lines this drop **pertains to**", not paths it changed —
   so I am not flagging that line. The memory index line is the sharper one:
   "**ADR-030's decision**, its deliberate exclusions…" presents a file that does
   not exist as the settled authority, in a note whose whole job is to be
   believed by a future session.
   **Suggested fix:** one clause on the Security sentence — "…specified in
   `07-handoff.md` § 5–5.1, landing at plan step 16" — and change the memory index
   line to "the `WithHistory` decision (ADR-030 is step 13's deliverable)".
   *Evidence: plan steps 13 and 16 read in full, both `[ ]`; `07-handoff.md:273`,
   `:406`, § 5.1; `ls docs/adr/` — highest is ADR-028; `grep -n "Trust Boundary"
   docs/security/threat-model.md` — five boundaries; `git diff <range> --
   docs/security/` — empty.*

3. **[`Picea.Abies.Conduit.ServiceDefaults/Extensions.cs`:32]** — **Six declared
   `ActivitySource`s are collected nowhere, and the one framework registration
   matches nothing at all.** Full table in *Reconciliation*. `AddSource` matches
   exact names (wildcards require an explicit `*`), and **no `ActivitySource` in
   this repository is named exactly `"Picea.Abies"`** — so the framework's runtime,
   subscription, server-page, session and WebSocket-transport traces are dropped
   on the floor in Conduit today. This is a live violation of the team's own
   observability principle (`functional-ddd` § OTEL: "Register the
   `ActivitySource` in `ServiceDefaults`") and of the Reviewer's dimension 8 ("no
   dark services"). Pre-existing and **out of scope for this docs-only PR** — I am
   not asking for a code fix here. I am asking that "named, not scheduled" become
   scheduled.
   **Suggested fix:** a `refutations.md` residual owned by `devops` with an
   `expires:`, or a GitHub issue; `.AddSource("Picea.Abies*")` is the probable
   one-line remedy but belongs to its own PR and its own review pair.
   *Evidence: `grep -rn 'ActivitySource' --include=*.cs` and
   `grep -rn 'AddSource(' --include=*.cs`, both excluding bin/obj;
   `gh issue list --state all --limit 100` — no matching issue.*

4. **[framework observation — `00-warden.md`, and the two claims resting on it]** —
   **The warden's re-run destroys the evidence for gate 1's only effectiveness
   claim.** `CLAUDE.md` § 3 rule 2 prescribes that when the user sends a scope
   back, "the `architect` fixes it and the warden runs again" — and the warden
   writes to the same path, so the second report overwrites the first. That is
   what happened here: the surviving `00-warden.md` is the re-run, CLEAN, 0
   findings. Both the R-21 citation (🔴 above) and `flow-changelog.md`'s two-leaks
   claim point at a report that no longer exists. **Both claims are nonetheless
   true** — I verified each independently against `f0cb682`'s deleted draft and
   against the hook source. But a governance record whose evidence is destroyed
   by the documented happy path is a framework property worth naming, and it
   makes 08's P7 concrete rather than hypothetical: the evidence is not merely
   unreadable from outside `.squad/design/`, it is gone from inside it too.
   **Suggested fix:** quote the two leaked sentences inline in the changelog
   entry, which makes the claim checkable by anyone including a blind reviewer;
   and consider whether a warden re-run should append or write `00-warden-2.md`.
   The latter is a framework change, not this PR's business — register it.

5. **[`.squad/log/pass-cost.md`:3]** — **The file's header states units the data
   does not have.** Header: "**Wall-clock per design phase**, keyed by slug." The
   column is cumulative-since-session-start and monotonic (…1332m50s, 1335m18s,
   1340m41s, 1359m45s, 1362m02s, 1366m05s), and the counter does not even reset
   across slugs — the final row is slug `undo-redo-design-record` continuing
   `undo-redo`'s total. The correction exists only in one agent's private
   notebook (`dreamer-convergence/pass-cost-instrument-reads-cumulative.md`:
   "take deltas between consecutive rows or the denominator is nonsense").
   The header itself says this file is "the denominator" for whether the dual
   track earns its cost — so a reader who trusts the header computes the wrong
   answer, and `dreamer-convergence` is the only reader who won't.
   Pre-existing hook defect (`session-logger.sh`). **Register it**; the header fix
   is one line and belongs with the hook fix.

6. **[`.squad/log/2026-09-07-session.md`]** — **The session log records the
   orchestrator's intentions rather than the agents' results, at a 25:214
   signal-to-noise ratio.** 08 established this and I did not re-derive it; I
   confirm only that it is the second consecutive day with the same shape, which
   makes it a `session-logger.sh` defect rather than a one-off, and that
   `CLAUDE.md` directs the Lead to read these files when continuing prior work.
   Pre-existing. **Register it.**

7. **[`.claude/agent-memory/security-expert/state-retention-features-need-sensitivity-marker.md`]** —
   **A persistent note recommends a name the pass explicitly superseded.** Its
   "How to apply" says: "require a type-level marker interface (e.g.
   `ISensitiveCause : Message` …)". Gate-4 decision 5 removed the `I` prefix:
   `07-handoff.md:206` — "**`SensitiveCause`, no `I` prefix** … The register's *No
   I-Prefix* rule. Revision 2 said `ISensitiveCause` in seven places; revision 4
   says it in none." The plan's only two surviving occurrences (`:847`, `:2158`)
   are both historical narration of that very fix. This note is cross-session
   guidance for exactly the review that will police the name.
   **Suggested fix:** one token, plus a half-sentence recording that the prefixed
   form was rejected — the rejection is the useful part of the memory.

8. **[`.claude/agent-memory/critic/picea-xml-is-the-kernel-contract.md`,
   `.claude/agent-memory/realist/picea-package-xml-answers-kernel-questions.md`]** —
   **Two notes send future agents to `Picea.xml` without naming the version trap
   that defeats them.** Verified: `picea/1.0.0/lib/net10.0/Picea.xml` is 1369
   lines and documents `AutomatonRuntime`'s members; `picea/1.0.27-rc-0002/…/Picea.xml`
   is **477 lines** and mentions `AutomatonRuntime` exactly **once** — the type
   declaration — with zero documented members. Four projects reference the rc. An
   agent following the realist note's instruction ("Read it before declaring
   anything about the kernel unknowable") that lands on the rc concludes precisely
   the "unknowable" the note exists to prevent, with nothing signalling the wrong
   turn. One sentence fixes both. *Evidence: `wc -l` on both files;
   `grep -c AutomatonRuntime` on the rc → 1.*

### 💡 Nitpicks

- **Two concerns in one commit.** The `undo-redo` record travels with a
  `reviewer-reconcile` drop about PR #358 and an 86-line append to a different
  pass's closed verdict. The #358 material is hook fallout from an inbox not
  drained before the branch was cut, not deliberate inclusion, and splitting it
  now would cost more than it returns. Noting it so the next branch cut drains
  the inbox first.
- **`00-warden.md` records "153 lines" for a `00-scope.md` that is now 254.** The
  warden ran before three scope amendments. Accurate as of its run; a dated line
  would make that legible.
- **`04-realist-plan.md`:1237-1240 asserts "the handoff has no `[R4-nav]` row at
  all".** True when the Critic wrote it; `07-handoff.md:452-453` now has both B10
  and B11 rows. Harmless — the plan is explicitly superseded-in-places by
  `06-spec.md` — but it is a present-tense claim about a file that changed after.
- **The PR body does not link issue #162** ("feat: Undo/redo as a first-class
  framework primitive", OPEN), which is this pass's originating request.
- **`R-21` is inserted above `## Extensions` rather than at end-of-file** in a
  document whose opening declares it append-only. It edits nothing in place, and
  `## Extensions` is itself documented as the append target for *extensions*
  specifically. Compliant as I read it; confirming since 08 asked.

### ✅ What's Good

- **Twenty-for-twenty on citations, and it holds inside the design directory
  too.** 08 checked twenty drop citations into readable source and found no
  fabrication. I checked the same class inside `.squad/design/` — B10's, B11's,
  S25's, S29's, S31's and S20's dispositions, each against the artifact and line
  it names — and found the same. In a 11,738-line documentation changeset that is
  the property that decides whether the record is worth keeping.
- **The two 🔴 from the confirmation pass are discharged in the record, with the
  residue named rather than absorbed.** `07-handoff.md:452` says B10's mitigation
  (1) landed, (2) is *still owed* to `tech-writer`, and (3) is explicitly not in
  this pass. A record that says "still owed" about its own blocker is doing the
  job.
- **S25 is registered as an unresolved risk, in the author's own words, with its
  failure mode stated.** `07-handoff.md:517-524`: "This is a documented precedence
  rule, **not a resolution**; if an implementer reads only the plan, they will
  implement the wrong narrative into step 5's file docs." Authors do not usually
  write that sentence about their own artifact.
- **The commit-boundary confirmation (§ P9) is the right shape.** Pure append,
  labelled as not-a-re-review, with an explicit "Claims I could not verify"
  section distinguishing what was diffed from what was inferred.
- **Mechanically clean where it is easy to be sloppy.** Seven drops archived and
  merged exactly once each; R-21 is the next unused id in a gap-free ledger; zero
  orphaned memory notes; zero dangling wikilinks; all seven INV ids covered by the
  spec; Track A's Reasoning Trail and Track B's Known Failure Modes both present;
  lexicon gate passed with no override.
- **Track A's blindness held and the gate-1 experiment produced a real result.**
  `ADR-008:85` was left reachable deliberately and convergence classified Track A
  as having reasoned past it, on five pieces of stated evidence. The deny-list gap
  that made it reachable was then registered as R-21 rather than quietly closed
  mid-pass — the disciplined choice, and the one that keeps the experiment honest.

---

## Metrics

- **Files reviewed:** 67 / 67 (all `.md`) · **Lines:** +11,738 / −86
- **Design substance read:** all 12 files in `.squad/design/undo-redo/` (8,412 lines) — the ~85% `reviewer-blind` could not see
- **Stated property:** `bash .claude/hooks/tests/run.sh` → **823 passed, 0 failed** at `66379e7` ✅
- **Test coverage of new code:** n/a — no executable code in the range
- **Dimensions run: 11/11.** Substantive: 2 (readability/clarity), 3 (consistency), 4 (design & structure), 5 (testability — the spec's INV coverage and lock protocol), 6 (security — the threat-model deliverables' status), 8 (observability — ⚠️ 3), 9 (documentation — the bulk of the findings), 10 (Boy Scout), 11 (DoD). Recorded as not-applicable-by-shape, having been checked for applicability first: 1 (correctness — no executable statements) and 7 (performance — no hot path).
- **Round:** 1 of 2 before the cap applies.

---

## Where to stop

The three 🔴 items are all documentation edits plus four ledger registrations —
none touches code, so this stays a docs-only changeset and re-review is targeted
at the findings, not a re-run.

One process note, from `.claude/agent-memory/reviewer-reconcile/a-confirmation-pass-invalidates-its-own-cache.md`:
committing this verdict, `08-review-blind.md` and the hook-written log churn will
move `HEAD` away from `66379e7`, and the drop below pins `66379e7`. Land the
fixes and the review artifacts in **one** commit, then request the confirmation
pass against that commit — do not commit the artifacts first and the fixes after,
or the gate re-blocks between them.

Working tree at review time: `M .squad/design/undo-redo-design-record/08-review-blind.md`,
`M .squad/log/2026-09-07-session.md`, `M .squad/log/pass-cost.md` — all expected
hook or blind-reviewer output, none of it drift.
