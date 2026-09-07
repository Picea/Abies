# ⚖️ Review Verdict — `presentation-demo` (working tree on `docs/0-presentation-demo`)

**Blind assessment:** `08-review-blind.md`
**Reviewed:** working tree vs `07607bf71152ef8d224352e2d2a737d8312dcb2d` (= `origin/main` = HEAD).
43 new files under `Picea.Abies.Presentation/content/demo/**` (10,144 lines) plus a 7-line
`ItemGroup` in `Picea.Abies.Presentation/Picea.Abies.Presentation.csproj` (+7/−0).
**Verdict:** 🔴 Changes Requested

Two gates apply to this change and they are not the same gate. `CLAUDE.md`'s routing rule
says `Picea.Abies.Presentation/**` **factual claims additionally require
`reviewer-reconcile` sign-off before a talk ships**. So a finding here is either
*merge-blocking* (the artifact is wrong as committed) or *talk-blocking* (the artifact is
faithful, but a slide built from it would state something false). Both blockers below are
labelled. Nothing in this change is dishonest, and no copy claim it makes is false.

---

## Reconciliation

The blind reading is unusually good and its self-declared boundary was exactly right: it
named the one class of claim it could not reach and told me precisely which `diff`s to run.
I ran all of them. The result changes the disposition of its headline finding.

### Where 08 and the narrative agreed, and the code confirmed both

The runbook instruction claimed: copies only; nothing under `.squad/` moved, edited or
deleted; every `stops/` file a verbatim excerpt. **All three claims are true, and I verified
each mechanically rather than accepting either the runbook or the README.**

- **All 19 `full/` copies are byte-identical to their sources** — the nine numbered
  `undo-redo` artifacts plus `room-security.md`, and all eight decision drops. I checked
  them twice: against the working tree, and against `git show 07607bf:<path>`. Identical
  both ways. This is the change's central claim and it holds completely. 08 could verify
  two of nineteen; the other seventeen are now verified.
- **Nothing under `.squad/` was edited or deleted.** The two dirty `.squad/` files
  (`log/2026-09-07-session.md`, `log/pass-cost.md`) are `+64/−0` — pure appends by the
  `session-logger` hook, one of which is this very review pass's own row
  (`presentation-demo | reviewer-blind`). No deletions anywhere. The claim holds.
- **Every `stops/` fragment is verbatim.** 08 verified the ones reachable from outside
  `.squad/design/`; because `full/` is now proven identical to the source, its
  intra-bundle verifications transfer. I separately verified the two fragments 08 could
  reach by neither route — `5.6-review-headers.md` and `5.6-round-2-escalation.md` — against
  `git show 07607bf:.squad/design/undo-redo-design-record/*`. Both byte-exact, differing
  only by one blank line inserted as a span separator. I also reassembled
  `5.4-critic-verdicts.md` from all five cited spans: exact, **no authored text added**
  despite its stitched appearance.

### Where 08 and the code diverged — its problem 1, settled

08's most serious finding was that stop 5.1 ships "three mutually contradictory statements"
and that `timing/hooks-fired.log` — the one authored file — makes an original claim the
bundle contradicts. **Both halves of that need correcting, in opposite directions.**

**The discrepancy is entirely in the source at the pinned commit, not in the copies.** At
`07607bf`:

| artifact @ 07607bf | says |
|---|---|
| `.squad/design/undo-redo/00-warden.md:3` (subagent report) | `00-scope.md` **(153 lines)** |
| `.squad/design/undo-redo/00-scope.md` | is **254** lines |
| `.squad/design/undo-redo/00-warden-scan.md:9` (hook scan) | `00-scope.md` **(254 lines)** |

The demo bundle reproduces the first two faithfully. So 08's alternative — "an error
introduced here" — is excluded. It is a faithful copy of a genuinely inconsistent source.

**And `hooks-fired.log` is accurate; 08 conflated two of its rows.** The log distinguishes
the hook from the subagent and attributes the line count only to the hook:

- "`scope-warden.sh` (mechanical scan), **run 2** — `00-warden-scan.md` at the amended
  (254-line) scope: verdict CLEAN" — **true**; the surviving scan file says 254.
- "`scope-warden` (subagent judgement), **run 2** — `00-warden.md` (re-run, current at this
  commit): CLEAN, 0 findings" — claims **no** line count.

So the authored file does not assert what 08 read it as asserting. That finding is
withdrawn, and it is the one place the blind reading over-called.

**What survives, and why it still matters.** The warden report's own internal citations
split against the shipped scope: `"Subscriptions and timers" (lines 28–50)` is **correct**
for the 254-line file (heading is at line 28), while `"at what level undo operates"
(line 99–101)` is **wrong** — that sentence is at line **221**; lines 99–101 are unrelated
prose about the durability exclusion. And the 153-line scope was **never committed** — the
design directory first appears in history at `07607bf` already at 254 lines — so those
citations are permanently unverifiable against anything. The bundle therefore places, on the
slide whose stated subject is *"the warden earning its place"*, a gate report contradicting
the scope file shipped beside it in `full/`, with the README silent. That is blocker 2.

### The Critic's accepted risks

No `05-critic.md` exists for this slug — `.squad/design/presentation-demo/` contains only
`08-review-blind.md`. This change ran as a direct runbook instruction, not a design pass, so
there is no Critic table to reconcile. Correct for a change of this shape; recorded so the
absence is not mistaken for an omission.

| risk | stated mitigation | present in the code? |
|---|---|---|
| _(no Critic pass for this slug)_ | — | n/a |

### What 08 could not determine, answered

Its list of seven open questions is the right test of the narrative. Five are now answered
by evidence; two remain genuinely open and are the reason this is not a clean approve.

1. **Are the `full/` copies verbatim?** — **Yes, all 19, at the pinned commit.** Resolved.
2. **Is the "153 lines" copied or introduced?** — **Copied.** Resolved; 08 called this "the
   single highest-value check in the list" and it was.
3. **Why do the copies exist at all?** — the runbook instruction supplies the intent (a
   self-contained demo folder for a talk) but **not the reason duplication is preferred to
   `git show`**. Still unanswered; see ⚠️-3.
4. **Permanent or one-shot?** — still unanswered, and it is the hinge for ⚠️-3 and 💡-2.
5. **Was the `check-pr-size` failure priced in?** — **Yes, and it is acceptable.** See 💡-1.
6. **Are the not-created fragments and the 5.6 sources permanent gaps?** — the README treats
   the former as closed and is silent on the latter. Blocker 2 covers it.
7. **Is the deck itself in scope?** — no deck, script or runbook file is in the change, so
   the fragment→slide mapping remains uncheckable by anyone. Confirmed; noted in ⚠️-4.

### Where I corrected 08 on detail

- 08 reports `stops/` holding **21** files and the README **24** rows. Actual: **20** files
  and **23** rows. The errors cancel and its conclusion is right — I re-ran the check with
  `comm`: the three rows with no file on disk are **exactly** `5.2-line-abstract.txt`,
  `5.2-refusal.log`, `5.6-refusal.log`, and there are **zero** files without a row. Index
  integrity confirmed.
- 08 says the README cites `refutations.md:722` **twice**. It cites it **once** (line 65);
  the `5.2-track-b-summary.md` row says "Same caveat as above (R-25)" without repeating the
  number. The off-by-one itself is real — R-25 is at 723, 722 is blank.

### Claims I could not verify

- That the 21 fragments cover the talk's stops, and that each README rationale matches what
  its slide asserts. No deck is in the change. Outside anyone's reach, mine included.
- That the warden report's `(line 99–101)` was ever correct, for any version of the scope.
  The 153-line file is not in git history. Permanently unverifiable — which is itself the
  most honest thing the slide could say.

---

## Findings

### 🔴 Must Fix (blocks merge)

**1. `[Picea.Abies.Presentation.csproj:12-19]` — three of the four `Remove` items are
unnecessary, and the one that isn't a no-op hides the demo tree from the IDE it is meant to
be presented from. (Merge-blocking.)**

The comment states the intent precisely: the tree "must never be compiled, embedded, or
published". I tested each item against that intent by installing a `<Compile Remove>`-only
variant and measuring, rather than reasoning about glob ordering:

| item | effect with `<Compile Remove>` already present |
|---|---|
| `<Compile Remove="content\**" />` | **necessary** — without it the build fails |
| `<Content Remove="content\**" />` | **no-op** — 0 `Content` items match `content/demo` either way |
| `<EmbeddedResource Remove="content\**" />` | **no-op** — 0 items either way |
| `<None Remove="content\**" />` | **removes 42 `None` items** — the only observable effect |

With `<Compile Remove="content\**" />` as the *sole* guard: `Build succeeded`, and
`-t:ComputeFilesToPublish -getItem:ResolvedFileToPublish` returns **0** entries under
`content/demo` (3 total publish entries, none of them demo material). So all three stated
goals are met by one line. `None` items are not published by the WebAssembly SDK — they are
what populates the Solution Explorer / Rider file tree. Removing them buys nothing against
the stated intent and costs the presenter visibility of all 42 files in the IDE, permanently
and silently. A future contributor adding a fragment will not see it appear.

*Suggested fix:* reduce the `ItemGroup` to `<Compile Remove="content\**" />` and amend the
comment to say what is actually true — that the tree contains one excerpted `.cs` that is
not a compilation unit. If belt-and-braces on `Content`/`EmbeddedResource` is wanted despite
being a no-op today, keep those two and drop **`<None Remove>`** specifically.

*Evidence:* removed the guard entirely → `error CS0106` and `error CS1513` in
`stops/5.5-property.cs`, `Build FAILED` (so the `.cs` extension makes the guard genuinely
load-bearing — 08 was right). Installed `<Compile Remove>` alone → `Build succeeded`,
`Content`/`EmbeddedResource` 0 items, `None` **42** items, publish set clean. csproj
restored byte-identical afterwards; `git diff --numstat` is `7 0` as before I started.

**2. `[content/demo/README.md]` — the index does not annotate the places where the bundle
contradicts itself or where a rationale claims more than its span contains.
(Talk-blocking, not merge-blocking.)**

Stating this as a rule rather than a list, because an enumeration will be read as the scope
and these are examples, not the criterion:

> **Every shipped fragment that contradicts another shipped file, and every README "why this
> excerpt" that asserts more than the cited span actually contains, must be annotated in the
> index — to the same standard the README already applies elsewhere.**

The README already meets this standard repeatedly and well: it flags the `5.2-track-b-summary`
mid-sentence cut as "not an edit made here", explains the excluded PR #358 drop, records the
R-24 units caveat "worth saying on stage", and refuses to fabricate three fragments. That
established standard is what makes the gaps defects rather than taste. Instances I found:

- **`stops/5.1-warden-report.md:3` vs `full/00-scope.md`.** The fragment says the gate
  checked a **153-line** scope; the README's own `full/` row says the shipped scope is
  "as amended and landed — **254 lines**". Both are in the bundle, on the same slide's
  material, unreconciled. The fragment's `(line 99–101)` citation additionally lands on
  unrelated prose in the shipped scope (the quoted sentence is at **221**). Neither is an
  error made here — but an audience member who follows the citation finds the wrong text,
  and the presenter has no warning.
- **`stops/5.1-warden-report.md` truncation.** The R-21 half ends on "…so the experiment
  is", one line before "Net: Track A's blindness to ADRs is **instruction-level**" — the
  classification that is presumably why the fragment is on the slide. The README flags
  exactly this defect on `5.2-track-b-summary.md` and not here. (08's problem 2; confirmed
  against `refutations.md:570-572`.)
- **`stops/5.2-hook-line.sh` README rationale.** The row claims the span contains "the
  'unreadable → refuse for everyone' fallback". The span (`:316-330`) contains
  `print("UNREADABLE"); sys.exit(0)` — the **signal**. The refusal-for-everyone behaviour is
  at `enforce-track-blindness.sh:422` (`if [ "$reason" = "UNREADABLE" ]`), outside the span,
  and the comment block explaining that intent is at `:308-315`, immediately above and also
  outside. Three of the row's four claims are supported; this one is not, as shown.
- **`stops/5.6-*` self-containment.** `5.6-review-headers.md` and
  `5.6-round-2-escalation.md` are sourced from
  `.squad/design/undo-redo-design-record/08-review-blind.md` and `09-review-verdict.md`.
  Neither is copied into `full/`, and the slug appears nowhere in the README's `full/` table
  or its exclusions note. The bundle is self-contained for 5.1–5.5 and not for 5.6 — the one
  stop about the review chain catching things is the one that cannot be checked in context.
  The asymmetry is undocumented while a far smaller exclusion (one PR #358 drop) gets a full
  sentence.

*Suggested fix:* for the first, one sentence in the `5.1-warden-report.md` row noting the
header's line count is stale against the amended scope, that the `99–101` citation does not
resolve in the shipped file, and that the 153-line version was never committed — this is a
*better* slide, not a worse one, since the pass's own overwrite-in-place behaviour is
already the stop's subject. For the second, add the truncation note the README already uses
verbatim elsewhere. For the third, extend the span to `:308-330` (8 more lines) so the claim
becomes true, or restate the rationale. For the fourth, either copy the two
`undo-redo-design-record` files into `full/` or add them to the exclusions note with a
reason.

*Evidence:* `git show 07607bf` on all three source artifacts; `grep -n` for the quoted
sentence (line 221) and the heading (line 28); `sed -n '99,101p'`; `sed -n '569,573p'` on
`refutations.md`; `grep -n UNREADABLE` on the hook → 327 and 422; `comm` diff of README rows
against `ls stops/`.

### ⚠️ Should Fix

**3. `[content/demo/]` — the duplication has no divergence detector, and the bundle's
permanence is undeclared.** `full/refutations.md` copies a *living* ledger and the `full/*`
artifacts copy files the design process **overwrites in place** — the README explains this
twice, as the reason several fragments exist at all. The copies will therefore diverge from
their sources by design. Today "unedited copies" is verified (I verified it). The moment
anyone edits a file in `full/`, it silently degrades to asserted, with no checksum manifest,
no CI check and nothing marking the tree read-only. A `SHA256SUMS` generated at snapshot
time would make the change's own central claim cheaply re-checkable forever — including by
`reviewer-blind`, which is denied `.squad/design/` by hook and could otherwise never verify
it. This is 08's point and I endorse it; it is ⚠️ rather than 🔴 only because the claim is
true *now*. Its weight depends on 08's open question 4: if the tree is permanent, this
matters more, not less.

**4. `[content/demo/stops/]` — composite fragments carry no in-file provenance.** Four
fragments are stitched from multiple sources and none carries a label, source comment, or in
most cases a separator. `5.4-critic-verdicts.md` is the clearest: five spans from four files,
ending in two bare `## Verdict` headings ("APPROVED WITH MITIGATIONS", "CONFIRMED WITH A
BOUNDED LIST") with nothing indicating they come from different passes of the same document.
I verified all five spans are exact and that **no authored text was added** — the fragments
are honest. But the correctness lives only in the README table, and slide content is
precisely the thing that gets separated from its index. An HTML comment at the top of each
composite naming its spans costs nothing and keeps the fragment honest when quoted alone.

### 💡 Nitpicks

1. **`check-pr-size` will hard-fail, and that is acceptable.** Confirmed mechanically:
   `isMaintenancePath` covers `.gitkeep`, `docs/`, `.github/instructions/`,
   README/CHANGELOG/CONTRIBUTING/SECURITY/LICENSE/BENCHMARK_PR_STATUS and `.md`. Six files
   fail it (`5.2-frontmatter-track-a.yaml`, `5.2-frontmatter-track-b.yaml`,
   `5.2-hook-line.sh`, `5.2-line-charter.txt`, `5.5-property.cs`, `hooks-fired.log`) plus the
   `.csproj`, so `maintenanceOnly` is false and 10,151 lines trips `core.setFailed` above the
   1500 hard limit. **This does not block merge:** `Check PR Size` is not a required status
   check in branch protection — the PR shows `UNSTABLE`, not `BLOCKED`, and squash-merges
   normally. **So the layout should not change on account of the size gate.** Note that 08's
   observation is decisive here: renaming every fragment to `.md` would *not* help, because
   the `.csproj` alone keeps `maintenanceOnly` false. The only real cost is that
   `detect-changes` sets `docs_only=false`, running the full lint/CodeQL/e2e matrix on a
   documentation change. Worth a line in the PR description so the red X is understood as
   priced-in rather than missed.
2. **Extensions invite misreading on a projector.** `5.2-hook-line.sh` is **Python** (the
   body of a heredoc inside `enforce-track-blindness.sh`) — any highlighter will render it as
   shell and get it wrong. `timing/hooks-fired.log` is markdown with `##` headings named
   `.log`, and it is the one file in the bundle that is *authored rather than copied*; a
   `.log` extension invites reading it as raw machine evidence, which is exactly what it is
   not. Both would be better as `.py`/`.txt` and `.md`. Renaming `5.5-property.cs` would
   additionally dissolve 🔴-1 entirely — the csproj guard exists only because of that one
   extension.
3. **README off-by-one.** `full/refutations.md:722` is cited for R-25; `### R-25` is at
   **723** and 722 is blank. The neighbouring R-24 citation (`:707`) is exact. Trivial except
   that line-precise citation is this artifact's entire premise, and R-25 is the caveat
   doing load-bearing work on two fragments.

### ✅ What's Good

- **The verbatim discipline is real and it survived every check I could devise.** 19 whole-file
  copies byte-identical at the pinned commit; 21 fragments byte-exact; composites that look
  authored but contain no authored text; a `pass-cost.md` filter that keeps exactly the 42
  `undo-redo` rows and correctly drops the other 6. I went looking for drift and found none.
- **Refusing to fabricate is the strongest thing here.** Three requested fragments were not
  created because the claimed text does not exist at `07607bf`, and the README says so with
  what was searched and what the nearest genuine quote is. A fourth item — a PR #358 decision
  drop — is excluded with a stated reason. Producing three "not found" rows against a runbook
  that asked for 23 fragments is the correct and harder outcome.
- **`hooks-fired.log` is scrupulous where it would have been easy not to be.** It declares
  itself compiled-not-verbatim in its own header, separates hook runs from subagent runs,
  and has a "Not evidenced at all" section. Its evidence claims check out where checkable,
  and — contrary to 08's reading — it does not overclaim the warden's line count.
- **The csproj guard is correctly placed and genuinely necessary.** It sits after the SDK's
  default globs, the Windows-style separators normalise on Linux, and I proved by removal
  that without it the project does not build. The over-reach in 🔴-1 is about scope, not
  correctness.
- **Hygiene is clean.** No CRLF; every non-empty file newline-terminated; no secrets,
  credentials or tokens (the `secret`/`password` hits are all design *discussion* of
  Conduit's threat model); no absolute or home-directory paths.
- **The blind reading was worth its cost.** It found seven real issues, named the one class
  it structurally could not verify, and handed me an exact list of `diff`s. Six of its eight
  problems stand; one was over-called and one had counting errors that did not affect its
  conclusion. That is a good ratio for a pass run with the narrative out of reach.

---

## Metrics

- **Files reviewed:** 44 (43 new + 1 modified `.csproj`) — every line of the `.csproj` hunk;
  full read of `README.md`, `hooks-fired.log`, all 20 `stops/` fragments; mechanical
  whole-file verification of all 19 `full/` copies and `timing/pass-cost.md`.
- **Lines changed:** +10,151 / −0.
- **Test coverage of new code:** n/a — no executable code added. `dotnet build` succeeds,
  0 warnings, 0 errors. No regression test is owed: no bug is fixed and no behaviour changes.
- **Spec-by-Example:** not applicable — no feature or behaviour change.
- **Definition of Done:** applicable items met (no principle deviations; no hardcoded
  secrets; build green; documentation shipped with the change in the form of the index).
  Testing/observability/threat-model items are not applicable to inert documentation.
- **Doc-sync:** checked. `README.md:314` describes `Picea.Abies.Presentation` as "Conference
  presentation app" — still accurate. No existing doc describes the project's internal folder
  layout, so nothing is made stale by this change. `.gitignore` has no `content`/`demo` rule.
- **Dimensions run:** 11/11.

---

# ⚖️ Re-review — round 2 (fixes, uncommitted working tree on `07607bf`)

**Blind assessment:** `08-review-blind.md` (round 1; `reviewer-blind` does not re-run — re-read
before starting this pass to keep the independent reading in view).
**Reviewed:** working tree vs `07607bf71152ef8d224352e2d2a737d8312dcb2d` (= HEAD = `origin/main`).
**Verdict:** 🔴 Changes Requested

Targeted at the round-1 findings, per review rule 5. Scope of change since round 1 established
mechanically by mtime against the round-1 drop's merge time (14:03 local): exactly three paths
under `content/` were touched — `stops/5.1-warden-report.md` (14:07), `SHA256SUMS` (14:09, new),
`README.md` (14:09) — plus the `.csproj` and the decision-register cleanup. All 40 other files in
the bundle are untouched, and I re-verified rather than assumed (below).

---

## Verification of each round-1 finding

### 🔴-1 csproj over-reach — **CLOSED.** Measured, not read.

The `ItemGroup` is now the single `<Compile Remove="content\**" />`. I re-ran the same
measurements that produced the finding:

| measurement | result |
|---|---|
| `dotnet build` | `Build succeeded`, **0 warnings, 0 errors** |
| `Compile` items under `content/demo` | **0** — never compiled ✅ |
| `EmbeddedResource` items under `content/demo` | **0** — never embedded ✅ |
| `Content` items under `content/demo` | **0** — (4 project-wide, none demo) ✅ |
| `-t:ComputeFilesToPublish -getItem:ResolvedFileToPublish` | 3 entries total, **0** under `content/demo` — never published ✅ |
| `None` items under `content/demo` | **43** (was 0) — the folder is visible in the IDE again ✅ |

All three stated goals are met by the one line, and the 42→0 IDE-visibility cost that made this
merge-blocking is gone. The build succeeding is itself the proof that the guard is load-bearing:
`stops/5.5-property.cs` is still an invalid compilation unit in the tree and no longer breaks the
build. The reworded comment is accurate.

*One residual, 💡 not blocking:* 43 of the 44 files are `None` items — the exception is
`stops/5.5-property.cs`, the very file the comment is about. `<Compile Remove>` takes it out of
`Compile` without putting it into `None`, so that one fragment stays invisible in Solution
Explorer / Rider. Strictly better than round 1 (0 visible → 43 visible) and not worth a second
`ItemGroup`; the comment's "the files stay visible as `None` items" is true of 43/44 and slightly
overstated for the one it names.

### ⚠️-3 no divergence detector — **CLOSED, and done well.**

`SHA256SUMS` covers exactly the 39 files under `full/` (19) and `stops/` (20). Verified three ways:
`sha256sum -c SHA256SUMS` from `content/demo/` → **39 OK, 0 failures**; re-running the documented
generation command produces a **byte-identical** file; the count matches the README's "19 + 20".
The README's new §`SHA256SUMS` correctly indexes it as *authored tooling output, not a copy or an
excerpt* (so it does not silently join the verbatim inventory), states the generating command,
and — the part that matters — declares itself a **snapshot, not a live guarantee**, naming
`full/refutations.md` and the overwrite-in-place artifacts as the reason a future mismatch may mean
the *source* moved rather than the copy being edited. That is exactly the distinction the finding
was about, and it is stated more carefully than the finding asked for.

### 🔴-2 index annotations — **the four named instances are CLOSED; the stated rule is NOT yet met.**

Round 1 stated this as a rule, with the four instances flagged as examples rather than the
criterion. The four are fixed, and three of them well:

- **(a) 153-vs-254 and `(line 99–101)`** — CLOSED and correct. The `5.1-warden-report.md` row now
  states that both defects are inherited from the source and not introduced here, that the
  153-line scope was never committed, and that the `99–101` citation resolves to unrelated prose
  with the intended sentence at line 221. I re-verified every number: `full/00-scope.md` is 254
  lines, `:28` is the `### Subscriptions and timers` heading, `:99–101` is the tail of the
  durability-exclusion paragraph, `:221` is "**At what level undo operates** — what the history
  consists of…". And the never-committed claim now holds against **both** committed revisions, not
  one: `git log --all` on that path yields `66379e7` and `07607bf`, and both are **254 lines**.
- **(b) truncation** — CLOSED. The ledger span is extended `546-570` → `546-572` and the fragment
  now reaches the classification "…blindness to ADRs is instruction-level for the `undo-redo`
  pass", which was the missing conclusion. Byte-exactness re-checked against
  `.claude/enforcement/refutations.md` (unmodified in the working tree, so == `07607bf`): lines
  546–571 are byte-identical, and line 572 is cut mid-line, dropping the trailing `" — nothing in"`.
  **That mid-line cut is disclosed twice** — in the source column ("cut mid-line at
  '…instruction-level for the `undo-redo` pass'") and in the rationale, which also says the closing
  invariant-level qualifier is deliberately left out. It is a pure prefix of the source with no
  authored text, and it matches the bundle's existing precedent (`5.5-inv-coverage.md` is cut
  mid-line at "…already states." and disclosed the same way). Accepted.
  I also re-verified the fragment's other half: `00-warden.md:1-19` is byte-exact and is the whole
  file (19 lines), separated by a single blank line. No authored text anywhere in the fragment.
- **(c) `5.2-hook-line.sh` rationale** — CLOSED. The corrected row's claims all check out: the
  span `:316-330` is byte-exact and does contain the JSON parse, the `agent_type` attribution, the
  `print("UNREADABLE"); sys.exit(0)` signal and the `DENY.get(agent)` lookup; `:308-315` is exactly
  the comment block; and `:422` is `if [ "$reason" = "UNREADABLE" ]; then`, the refuse-for-everyone
  enforcement. *One inaccuracy in the new text (💡):* it says both out-of-span items are "too far
  from `:316-330` to fold in without pulling in unrelated surrounding script". That is true of
  `:422`; it is not true of `:308-315`, which is contiguous and directly above, and folding it in
  would pull in zero unrelated lines. Restating the rationale was one of the two fixes round 1
  offered, so the finding is closed — but the justification for not extending overstates the cost.
- **(d) 5.6 self-containment** — CLOSED. Both rows now name the `undo-redo-design-record` slug,
  state that neither source file is copied into `full/`, state that stops 5.1–5.5 are checkable
  from the bundle alone while 5.6 is not, and give the `git show` command that would check it.
  Recorded rather than fixed by copying, which round 1 offered as an option. Both fragments
  re-verified byte-exact against `07607bf`.

**What is not yet met is the rule itself.** I ran the sweep the rule implies — every numeric
citation and every identity claim in `README.md`, mechanically, against `07607bf` — and found two
further instances of the same class. **Both existed at round 1 and I missed them; neither is a
regression introduced by these fixes.** See 🔴-5 below. The sweep is now complete and its results
are enumerated there, so this is closable in one more round rather than open-ended.

### ⚠️-4 composite provenance — **NOT addressed.**

The four composite fragments still carry no in-file label naming their spans. This was ⚠️ Should
Fix in round 1 and is unregistered as a residual, so by Verdict Consistency Rule 1 it prevents ✅
on its own, independently of 🔴-5. I am *not* asking for it to be registered in
`.claude/enforcement/refutations.md` — a demo-folder annotation does not belong in the enforcement
ledger, and over-registration is its own failure. Fix it (four HTML comments), or have the user
override it explicitly with their rationale recorded.

### Decision-register cleanup — **CLOSED and correct.**

The malformed drop's entry is gone from `.claude/docs/decisions.md` (no occurrence of `120238Z`
anywhere outside `.git/`), its archive file `2026-09-07T12-03-08-…` is deleted, and the valid
`2026-09-07T12-03-39-review-presentation-demo.md` remains with its `decisions.md` entry intact and
well-formed (`id: reviewer-reconcile-20260907T120325Z-presentation-demo`,
`commit: 07607bf…`). `.squad/decisions/inbox/` and `.squad/decisions/quarantine/` are both empty.
`.squad/.last-review-verdict` correctly reads `NEEDS-CHANGES` / `commit: 07607bf…`, written by the
merger from the valid drop — the mechanism works, which is what the cleanup was protecting.

---

## Nothing else changed

- **All 19 `full/` copies re-verified byte-identical** against `git show 07607bf:<source>` — the
  nine numbered `undo-redo` artifacts, `room-security.md`, `refutations.md`, and all eight decision
  drops. Zero mismatches.
- **All 20 `stops/` fragments re-verified byte-exact** against their cited spans at `07607bf`:
  twelve single-span fragments by direct diff (all OK), the two 5.6 composites, the five-span
  `5.4-critic-verdicts.md` (spans exact, blank separators only, **no authored text**),
  `5.2-hook-line.sh`, both 5.2 track-summary lines, `5.5-inv-coverage.md` (mid-line, disclosed) and
  the rebuilt `5.1-warden-report.md`.
- **File count 43 → 44**, the one addition being `SHA256SUMS`. Nothing removed.
- **`timing/` and `graphics/` untouched** (mtimes predate the round-1 verdict); `pass-cost.md`
  still holds exactly 42 `undo-redo` rows plus the header.
- **`.squad/` still unedited and undeleted.** The only dirty `.squad/` files remain pure appends
  from the `session-logger` hook.
- **`.csproj` diff is now `4 0`**, down from `7 0`. No other tracked file changed except
  `.claude/docs/decisions.md`, the two session logs, and my own agent memory.
- **No CRLF anywhere.** One new trailing-newline regression — see 💡-6.

---

## Findings

### 🔴 Must Fix (blocks merge)

**5. `[content/demo/README.md]` — the 🔴-2 rule is still unmet: two further instances, one of them
a statement that is simply false as committed. (One talk-blocking, one merge-blocking.)**

The rule from round 1 stands unchanged:

> **Every shipped fragment that contradicts another shipped file, and every README "why this
> excerpt" that asserts more than the cited span actually contains, must be annotated in the
> index — to the same standard the README already applies elsewhere.**

**This time the list is exhaustive, and I say so deliberately.** I have now checked, mechanically
and against `07607bf`, *every* numeric citation in the index (all 27 of them), *every* claim the
README makes about the pinned commit's own metadata (one), and *every* claim of the form "this
fragment is X's summary/report/verdict" against the fragment's actual content (all of them). There
is no third instance. Round 3 closes this.

- **(e) `README.md:97` — "the commit's own author date" is wrong for `07607bf`. Merge-blocking:
  this is a false factual claim in the committed artifact, not merely a slide hazard.** The row
  offers `2026-09-07T11:56:59+02:00` (`= 09:56:59Z`) as "the commit's own author date… citable
  separately if the slide needs a clock reading". But `07607bf` is authored
  **`2026-09-07T13:00:00+02:00`**. `11:56:59+02:00` is the author date of **`66379e7`**, the
  pre-squash branch commit — which is what `08-review-blind.md` was reviewing when it wrote the
  sentence at `:105`. The number is faithfully quoted; the *attribution* is introduced here, and
  it is introduced inside a document whose line 4 defines "the commit" as `07607bf` and whose every
  column header reads "Source @ `07607bf`". A presenter following this row states a timestamp
  63 minutes off on stage, for the one commit the whole bundle is pinned to.
  *Suggested fix, one clause:* "…the author date of `66379e7`, the pre-squash branch commit
  `08-review-blind.md` was reviewing; `07607bf` itself is authored `2026-09-07T13:00:00+02:00`."
  That is a *better* slide — the squash rewriting the timestamp the blind reviewer cited is the
  same overwrite-in-place theme stop 5.1 already exists to make.
  *Evidence:* `git show -s --format='%aI' 66379e7 07607bf` → `11:56:59+02:00` / `13:00:00+02:00`;
  `git show 07607bf:.squad/design/undo-redo-design-record/08-review-blind.md | sed -n 105p`.

- **(f) `stops/5.2-track-a-summary.md` / `5.2-track-b-summary.md` — the fragment named for each
  track contains the Lead's narration about the *other* one, and the index does not say so.
  (Talk-blocking.)** Both spans are byte-exact and both labels are correct — `:424` really is the
  `dreamer-first-principles` row and `:422` really is the `dreamer-informed` row. The problem is
  the content:
  - `5.2-track-a-summary.md` (labelled Track A) reads "**Track B has landed** with three ranked
    candidates. I'm holding its content until Track A finishes… **Waiting on Track A.**" — a line
    that explicitly states Track A had *not* finished.
  - `5.2-track-b-summary.md` (labelled Track B) reads "Both Dreamer tracks are running in parallel
    and isolated…" — about the pair and the lexicon check, not about Track B's findings.

  The README's R-25 caveat is generic and good as far as it goes ("logs the *Lead's* outgoing
  narration at that moment, labelled with whichever agent just stopped — not the subagent's own
  returned text verbatim"), and it is exactly why round 1 did not catch this. But a caveat about
  *whose voice* it is does not warn that *the subject is the other track*. A slide captioned
  "Track A's summary" showing text that says Track B landed and Track A is still running states
  something false, and the presenter has no warning in the index.
  *Suggested fix:* one sentence per row — that this particular line's subject is the other track,
  and that on the Track A row the text predates Track A finishing. Same standard the README already
  applies to the `5.2-track-b-summary.md` mid-sentence truncation two clauses later.
  *Evidence:* `git show 07607bf:.squad/log/2026-09-06-session.md | sed -n '422p;424p'`, diffed
  against both fragments.

### ⚠️ Should Fix

**4 (carried, unaddressed).** Composite fragments carry no in-file provenance. See above — this
alone keeps the verdict off ✅ under Rule 1, and the intended resolutions are *fix* or *explicit
user override*, not residual registration.

### 💡 Nitpicks

**6. `stops/5.1-warden-report.md` no longer ends with a newline.** It is now the only file in the
bundle without a trailing newline; round 1 recorded "every non-empty file newline-terminated" as
verified, so this is a small regression introduced by the hand-cut at line 572. Add the newline.

**7. Two `+1` off-by-one citations, same class, both cheap.** `full/refutations.md:722` is cited
for R-25, which is at **723** (722 is blank) — carried unfixed from round-1 💡-3. And
`full/05-critic.md:427` is cited for the navigation confirmation, whose heading is at **428**
(427 is blank). Everything else resolves exactly, including the neighbouring `:707`, `:3`,
`:56`, `:58`, `:39`, `:25-27` and `:446-448`. Trivial except that line-precise citation is this
artifact's premise.

**8. `5.2-hook-line.sh` rationale overstates the cost of extending to `:308`.** See 🔴-2(c) above.

**9. `5.5-property.cs` stays invisible in the IDE.** See 🔴-1 above.

**Carried from round 1, still open and still non-blocking:** 💡-1 (`check-pr-size` hard-fails and
that is priced in — worth a line in the PR description), 💡-2 (`.sh`/`.log` extensions invite
misreading on a projector).

### ✅ What's Good

- **Both blockers were engaged on their merits rather than worked around.** The csproj fix is the
  minimal one and was re-verified by the author before hand-off; the four README annotations are
  written to the bundle's own standard rather than bolted on, and (a) and (d) in particular are
  *better slide material* than what they replace.
- **The truncation fix chose the honest resolution.** Extending the span and disclosing the
  mid-line cut twice — including naming the clause deliberately left out — is a harder and more
  defensible choice than a README-only flag, and it matches the precedent already set by
  `5.5-inv-coverage.md`. It would have been easy to trim quietly; it was not trimmed quietly.
- **`SHA256SUMS` exceeds what the finding asked for.** Indexing it as generated-not-excerpted, and
  pre-stating that a future mismatch may mean the source moved rather than the copy being edited,
  is the caveat that keeps the manifest from becoming a false guarantee later.
- **The register cleanup is complete and verifiable** — no residue of the malformed entry anywhere,
  quarantine and inbox both empty, and the surviving drop is well-formed.
- **The verbatim discipline survived a second full mechanical sweep.** 19 whole-file copies and 20
  fragments, all still byte-exact at `07607bf` after the edits. The one file that changed changed
  exactly as described.

---

## Metrics — round 2

- **Files reviewed:** 5 changed since round 1 (`.csproj`, `README.md`, `stops/5.1-warden-report.md`,
  `SHA256SUMS` new, plus the register cleanup in `.claude/docs/decisions.md` and the archive) —
  every line of each. Re-verified all 44 bundle files mechanically for non-change.
- **Lines changed vs `07607bf`:** `.csproj` +4/−0 (was +7/−0); `content/` +10,192/−0 net (44 files).
- **Build:** `Build succeeded`, 0 warnings, 0 errors. Publish set clean (0 of 3 entries under
  `content/demo`).
- **Manifest:** `sha256sum -c` → 39/39 OK; regeneration byte-identical.
- **Citation audit:** 27/27 numeric citations resolved; 25 exact, 2 off-by-one (💡-7).
- **Test coverage / Spec-by-Example:** n/a — no executable code, no behaviour change, no bug fixed.
- **Definition of Done:** applicable items met except documentation accuracy — 🔴-5(e) is a false
  statement in shipped documentation.
- **Dimensions run:** targeted re-review per review rule 5, not a full 11/11 re-run. Dimensions
  exercised: Correctness, Consistency, Design & Structure, Documentation, Doc-sync, Boy Scout,
  Definition of Done.
- **Round:** 2 of 3. Round 3 should be the close — 🔴-5's list is exhaustive and the remaining fixes
  are four one-sentence edits, one newline and (for ✅) four HTML comments or a user override.

---

# ⚖️ Re-review — round 3 (fixes, uncommitted working tree on `07607bf`)

**Blind assessment:** `08-review-blind.md` (round 1; `reviewer-blind` does not re-run — re-read
before starting this pass to keep the independent reading in view).
**Reviewed:** working tree vs `07607bf71152ef8d224352e2d2a737d8312dcb2d` (= HEAD = `origin/main`).
**Verdict:** ⚠️ Needs Human Review

**Every round-2 finding is closed and verified.** Both 🔴-5 blockers, all three mechanical
fixes, and ⚠️-4 by an explicit, properly-recorded user override. Nothing else in the bundle
changed, and I re-proved that mechanically rather than inferring it from mtimes.

**This is round 3 of a 2-round cap, and I am not requesting a round 4.** The one open item
below is not a fix pass — it is a decision the user takes at `git add` time, and it is stated
with both of its one-step resolutions. See "The cap, and where to stop" at the end.

---

## Round count — evidenced before grading

Two archived `reviewer-reconcile` drops carry `commit: 07607bf…`:
`2026-09-07T12-03-39-review-presentation-demo.md` (round 1) and
`2026-09-07T12-23-39-review-07607bf-round2.md` (round 2), both `NEEDS-CHANGES`. The round-2
section above carries `**Round:** 2 of 3`. Both sources agree: this is round 3. The Merge
Criterion's round cap therefore governs the *shape* of this verdict, and I checked it before
assembling findings rather than after.

---

## Verification of each round-2 finding

### 🔴-5(e) the `5.6-review-headers` author date — **CLOSED, and it became the better slide.**

`README.md:97` now reads: `2026-09-07T11:56:59+02:00` (`= 09:56:59Z`, per
`08-review-blind.md:105`) **is the author date of `66379e7`**, the pre-squash branch commit the
blind reviewer was reading; **`07607bf` itself is authored `2026-09-07T13:00:00+02:00`**, with
the verifying command inline. Both numbers re-verified independently:

| commit | `%aI` | `%cI` |
|---|---|---|
| `66379e7` | `2026-09-07T11:56:59+02:00` | `2026-09-07T12:04:15+02:00` |
| `07607bf` | `2026-09-07T13:00:00+02:00` | `2026-09-07T13:00:00+02:00` |

And the source sentence still says what the row says it says:
`git show 07607bf:.squad/design/undo-redo-design-record/08-review-blind.md | sed -n 105p` →
"The commit itself is authored `2026-09-07T11:56:59+02:00` = **09:56:59Z**." The row took the
suggested framing — the squash rewriting the timestamp the blind reviewer cited is now stated
as the same overwrite-in-place theme stop 5.1 exists to make. The false attribution is gone
and what replaced it is stronger material than what was there in round 1.

### 🔴-5(f) the two 5.2 summary rows — **CLOSED.** Subject stated, not just voice.

Both rows now name the subject in bold, and both descriptions are accurate against the source:

- `README.md:87` (`5.2-track-a-summary.md`) — "**This line's subject is not Track A's own work —
  it is the Lead reporting that Track B has landed while Track A has not**", quotes the line,
  and says it is timestamped *before* Track A finished. It then makes the distinction explicitly:
  "Whose voice the line is in (the Lead's) and what it is about (Track B, and Track A's own
  incompleteness) are two different facts; a caption reading only 'Track A's summary' states the
  second one wrong." That is exactly the gap the finding named.
- `README.md:88` (`5.2-track-b-summary.md`) — "**Its subject is not Track B's findings either**",
  correctly describes it as the Lead noting both tracks running isolated and the pending lexicon
  check, and closes with "both fragments in this pair are the Lead talking about the *pair*".

Re-verified against the source, not against the round-2 text: `:424` is the
`dreamer-first-principles` row ("Track B has landed… Waiting on Track A.") and `:422` is the
`dreamer-informed` row ("Both Dreamer tracks are running in parallel and isolated… I'll surface
the"), and **both fragments are byte-identical to those lines**, mid-sentence truncation
included and disclosed. Labels correct, content correct, captions now safe.

### 💡-6 trailing newline — **CLOSED.** Swept, not spot-checked.

I did not check only the file that regressed. Every non-empty file in the bundle was tested for
a terminating newline: **44 files, 0 without one.** `stops/5.1-warden-report.composite.md` ends
with a newline again and its content is otherwise unchanged — re-proved by reassembling it from
`00-warden.md:1-19` + blank + `refutations.md:546-572` at `07607bf`, which differs at exactly one
line, the disclosed mid-line cut dropping `" — nothing in"`. No other delta.

### 💡-7 the two `+1` citations — **CLOSED, both.**

- `full/refutations.md:723` is now cited for R-25, and `:723` is
  `### R-25 — Session log signal-to-noise…`. Correct (722 is blank).
- `05-critic.md:428` is now cited for the navigation confirmation, and `:428` is
  `# 🔍 Critic — confirmation pass, 2026-09-07 (post-close-out amendment [R4-nav])`. Correct
  (427 is blank).

I re-resolved the neighbouring citations rather than assuming they were untouched: `:707` is
`### R-24 — pass-cost.md's header states units…`, `:546` is `### R-21 — docs/adr/** is
reachable…`, `00-scope.md:128` is INV-1, `:153` is INV-3, `:215` is `## Degrees of freedom —
deliberately open`, `:28` is `### Subscriptions and timers`, `:99–101` is the durability-exclusion
prose, `:221` is "**At what level undo operates**", and `05-critic.md:25` and `:446` are both
`## Verdict`. Every claim blocker-(a) makes about the 153-vs-254 inheritance still holds.

### ⚠️-4 composite provenance — **RESOLVED by user override, correctly recorded.**

Round 2 named two acceptable resolutions — fix it, or "have the user override it explicitly with
their rationale recorded". The second was taken, and it is recorded to the standard Review Rule 6
asks for: a new `## Overrides` section at `README.md:109`, attributed to the user and dated,
quoting them verbatim in a blockquote, with the reasoning intact rather than paraphrased:

> The rule that every file under stops/ is a verbatim excerpt exists for one reason: nothing
> that reaches a slide may be text that is not in the source. A marker at a seam inside a
> composite file is exactly that. […] Provenance lives beside the fragment, never inside it.

**I record my concern alongside it, as the rule requires, and I also record that the override
answers it.** My concern was that a fragment detached from the README loses its seam. The
override's second half — the `.composite.md` suffix — puts that metadata in the filename, which
travels with the file, and it does so without adding a byte the source does not contain. That is
a better resolution than the four HTML comments I asked for: the comments would have broken the
manifest that ⚠️-3 introduced one round earlier, which the override notices and I did not.

The rename claim is verified, not accepted:

| composite | content byte-exact vs `07607bf`? |
|---|---|
| `5.1-warden-report.composite.md` | ✅ `00-warden.md:1-19` + `refutations.md:546-572`, one disclosed mid-line cut |
| `5.4-critic-verdicts.composite.md` | ✅ all five spans, blank separators only, **no authored text** |
| `5.6-review-headers.composite.md` | ✅ both `:1-10` headers, exact |
| `5.6-round-2-escalation.composite.md` | ✅ `:475-487` + `:735-748`, exact |

`SHA256SUMS` regenerated: `sha256sum -c` → **39/39 OK, 0 failures**; re-running the documented
command produces a **byte-identical** file; the manifest's 39 paths and the 39 files actually on
disk under `full/` + `stops/` are the **same set exactly** (`diff` of sorted lists, empty). No
stale pre-rename name survives anywhere in the bundle — the only surviving occurrences repo-wide
are in `08-review-blind.md`, the two archived drops and `decisions.md`, which are historical
records and correctly left as-written.

---

## Nothing else changed

- **All 19 `full/` copies re-verified byte-identical** to `git show 07607bf:<source>` — the nine
  numbered `undo-redo` artifacts, `room-security.md`, `refutations.md` and all eight decision
  drops. Zero mismatches. (`room-security.md`'s source is `.squad/design/undo-redo/`, not
  `docs/security/` — my first path guess was wrong and the file is fine.)
- **All 20 `stops/` fragments re-verified byte-exact** against their cited spans at `07607bf`:
  fourteen single-span by direct diff, the two 5.2 log lines, the four composites. The only two
  deltas anywhere are the two disclosed mid-line cuts (`5.1-warden-report.composite.md` line 47,
  `5.5-inv-coverage.md` line 2), both documented in their README rows.
- **File count 44 → 44.** Four renames, no additions, no removals.
- **`timing/` and `graphics/` untouched**; `timing/pass-cost.md` still holds exactly 42
  `undo-redo` rows.
- **`.csproj` unchanged from round 2** — still `+4/−0`, the single `<Compile Remove="content\**" />`.
  `dotnet build` → **Build succeeded, 0 warnings, 0 errors.**
- **`.squad/` still unedited and undeleted.** `git diff --numstat` on `.squad/log/` is `124 0`
  and `3 0` — pure appends. `.claude/docs/decisions.md` is `+265/−0` at a single hunk appended
  at end of file (`@@ -2476,0 +2477,265 @@`) — merger output, no rewrite.
- **No CRLF anywhere. Every non-empty file newline-terminated.**
- **Inbox and quarantine both empty**; `.squad/.last-review-verdict` correctly reads
  `NEEDS-CHANGES` / `commit: 07607bf…`.

---

## Findings

### 🔴 Must Fix (blocks merge)

None. Both round-2 blockers are closed.

### ⚠️ Should Fix

**10. `content/demo/timing/hooks-fired.log` is matched by `.gitignore:120` (`*.log`) and will
be silently dropped by `git add`.** One file in the bundle — and it is one of only **two**
authored files — cannot be staged by the command anyone will use to commit this tree.

```
$ git check-ignore -v Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
.gitignore:120:*.log    Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
```

It is the only ignored file in the bundle — I ran `git check-ignore -v` over all 44 and it
returned exactly this one. `git status --untracked-files=all` on `content/` lists **43** paths
against **44** on disk; the missing one is this file.

**Why it matters, and why nothing in the bundle would catch it.** `SHA256SUMS` covers `full/`
and `stops/` only — by design, correctly stated in the README — so `timing/` has no manifest.
The README's `## timing/` section indexes `hooks-fired.log` with a full description. Commit with
plain `git add` and the bundle ships 43 files, a top-level README section pointing at a file
that is not in the repository, and **no error, no warning and no failing check anywhere**. It is
precisely the silent divergence ⚠️-3 introduced the manifest to prevent, in the one directory
the manifest does not reach.

**This is mine, twice over.** Round 1's metrics line asserted "`.gitignore` has no
`content`/`demo` rule" — true, and the wrong question: the rule that bites is the repo-wide
`*.log`. Round 2 repeated the omission. It is not a regression introduced by the round-3 fixes,
and I am not charging it to this fix pass.

**Two one-step resolutions; the choice is the user's, not mine.**

- **Force-add at commit time** — `git add -f Picea.Abies.Presentation/content/demo/timing/hooks-fired.log`
  alongside the rest. Zero file changes; the tree as it stands is correct and complete.
  *Verification:* staged paths under `content/` should be **44**, not 43.
- **Rename to a non-ignored extension** — e.g. `hooks-fired.md` — which makes the bundle robust
  for anyone who re-adds it later, at the cost of one README row edit and one more author touch.
  It also happens to retire carried-💡-2 (the `.sh`/`.log` extensions that "invite misreading on
  a projector") for this file.

I lean to the rename for durability and the force-add for closing today; both are defensible and
the trade-off is a proportionality call, which is the user's.

*Not a residual-ledger item.* A demo-folder commit-mechanics trap does not belong in
`.claude/enforcement/refutations.md`, for the same reason ⚠️-4 did not — over-registration is its
own failure. It needs a decision, not an entry.

### 💡 Nitpicks

**Carried, still open, still non-blocking:** 💡-1 (`check-pr-size` hard-fails; priced in, worth a
line in the PR description), 💡-2 (`.sh`/`.log` extensions on a projector — see ⚠️-10's rename
option), 💡-8 (`5.2-hook-line.sh` rationale overstates the cost of extending to `:308`, which is
contiguous), 💡-9 (`5.5-property.cs` is the one file `<Compile Remove>` leaves invisible in the
IDE, and it is the file the csproj comment names).

None of these changed, none was expected to, and none is worth another round.

### ✅ What's Good

- **The author fixed the finding and then improved on it, in both blockers.** (e) did not just
  correct the attribution — it took the suggested framing and turned the squash-rewrote-the-clock
  fact into stop material. (f) separated *voice* from *subject* explicitly, in the README's own
  register, rather than bolting a warning on.
- **The override is the best outcome ⚠️-4 had available, and it beat my own suggestion.** Four
  HTML comments would have put authored bytes inside verbatim fragments and broken the manifest
  introduced one round earlier — a collision I did not see and the override did. Moving the seam
  marker into the *filename* satisfies the finding with zero bytes added to any fragment. It is
  recorded as the user's, quoted verbatim, dated, with the applied consequence spelled out and
  the byte-unchanged claim independently verifiable. That is what a good override record looks
  like.
- **The renames were done without touching content.** Four files renamed, all four still
  byte-exact against `07607bf`, manifest regenerated and self-consistent, no stale name left in
  the bundle. It would have been easy to "tidy" a fragment while renaming it; nothing was tidied.
- **The mechanical fixes were swept, not spot-fixed.** Both off-by-ones were corrected, not just
  the one I described first, and the newline is now uniform across all 44 files.
- **Three full mechanical sweeps, three clean results.** 19 copies and 20 fragments, byte-exact
  at `07607bf` in rounds 1, 2 and 3, across two rounds of edits and a set of renames. The
  verbatim discipline this bundle is built on has held every single time it was tested.

---

## The cap, and where to stop

The Merge Criterion's **round cap is two**, and this is round 3. The cap's instruction is to
*split*: what passes (a)–(c) ships, the rest becomes the next changeset. Applied here:

- **(a) stated properties green.** The runbook's three claims — copies only, every `stops/` file
  a verbatim excerpt, nothing under `.squad/` moved/edited/deleted — are green, verified
  mechanically for the third time. No red stated property, so the cap does not escalate to the
  `architect`.
- **(b)** the one open finding is not a regression of a stated property and is not a
  registration case; it is a decision with two one-step answers.
- **(c)** no level claim is made by this changeset.

So the honest shape is ⚠️ Needs Human Review with the proportionality call handed over, **not**
a fourth fix round. Concretely:

1. **Pick a resolution for ⚠️-10** (force-add, or rename + one README row).
2. **Either** override this ⚠️ under Review Rule 6, recording the choice and rationale the same
   way the ⚠️-4 override was recorded — the commit gate reads `.squad/.last-review-verdict`,
   which will hold `NEEDS-CHANGES` for `07607bf` — **or** dispatch one targeted confirmation.
3. **If a confirmation pass is dispatched, its scope is one check:** staged paths under
   `Picea.Abies.Presentation/content/` = **44**. Nothing else needs re-verifying; this section
   is the evidence for everything else.
4. **Commit the bundle and the `.csproj` only, then stop.** Committing this verdict file, the
   decision drops or the agent-memory notes in the same operation moves `HEAD` past the commit
   the verdict is pinned to and re-blocks the gate on a changeset that already passed. If they
   are to be committed, that is a separate commit after the bundle lands, or the same commit
   with the gate consciously overridden — not an accident.

## Metrics — round 3

- **Files reviewed:** 6 changed since round 2 — `README.md` (the (e)/(f)/citation edits and the
  new `## Overrides` section), `stops/5.1-warden-report.composite.md` (newline + rename),
  three further renames, `SHA256SUMS` (regenerated) — every line of each. Re-verified all 44
  bundle files mechanically for non-change.
- **Lines changed vs `07607bf`:** `.csproj` +4/−0 (unchanged from round 2); `content/` 44
  untracked files, of which 43 are `git add`-visible (see ⚠️-10).
- **Build:** `Build succeeded`, 0 warnings, 0 errors.
- **Manifest:** `sha256sum -c` → 39/39 OK; regeneration byte-identical; manifest set == disk set.
- **Verbatim audit:** 19/19 `full/` copies byte-identical; 20/20 `stops/` fragments byte-exact
  against their cited spans, two disclosed mid-line cuts and no authored text anywhere.
- **Citation audit:** all numeric citations re-resolved, including both round-2 off-by-ones.
  0 unresolved, 0 off-by-one.
- **Hygiene:** 0 CRLF; 44/44 files newline-terminated; 1/44 files gitignored (⚠️-10).
- **Test coverage / Spec-by-Example:** n/a — no executable code, no behaviour change, no bug fixed.
- **Definition of Done:** applicable items met. Documentation accuracy is now clean — the false
  author-date claim is gone and no shipped statement is false.
- **Overrides logged:** 1 (⚠️-4, user, 2026-09-07, rationale recorded verbatim at
  `README.md:109`; my concern and its answer recorded above).
- **Dimensions run:** targeted re-review per review rule 5, not a full 11/11 re-run. Dimensions
  exercised: Correctness, Consistency, Design & Structure, Documentation, Doc-sync, Boy Scout,
  Definition of Done.
- **Round:** 3 of 3 — **at the cap.** No round 4 is requested.

---

# ✅ Confirmation — round 3's single open item (staged tree on `07607bf`)

**Verdict:** ✅ Approved. **Scope:** the one check round 3 named, and nothing else.

**⚠️-10 is closed.** The Lead took a third resolution rather than either of the two I named —
a scoped `.gitignore` negation preserving the runbook filename — and it is the better one.
Both of my options had a cost this one does not: the force-add leaves the trap armed for
anyone who re-adds the folder later, and the rename would have edited `README.md:44` and
diverged from the user's runbook. Verified mechanically, not accepted:

| check | result |
|---|---|
| `git diff --cached --name-only` | **46** paths: **44** under `content/demo/`, plus the `.csproj` and `.gitignore` |
| files on disk under `content/demo/` | **44** — `diff` of sorted lists against the staged set is empty |
| `git check-ignore -v` on `timing/hooks-fired.log` | **exit 1, no output** — un-ignored |
| `git check-ignore -v --stdin` over all 46 staged paths | **exit 1, no output** — none ignored |
| `git ls-files --others` under `content/demo/` (**ignored included**) | empty — nothing left behind |
| staged blob vs disk bytes, all 46 | `git rev-parse :<path>` = `git hash-object <path>`, **0 mismatches** |
| CR bytes in any staged blob | none |
| `sha256sum -c SHA256SUMS` | **39/39 OK, 0 failures** |
| `git diff --name-only -- Picea.Abies.Presentation .gitignore` | empty — index and working tree identical |

**The bytes are round 3's bytes.** Nothing under `content/demo/` has an mtime later than
`15:01` local, and `09-review-verdict.md`'s round-3 section was written at `15:09`; the only
file touched after it is `.gitignore` at `15:12`, which is the fix. `sha256sum -c` re-proves
the 39 manifested files independently of mtimes, and the five unmanifested ones
(`README.md`, `SHA256SUMS`, `graphics/.gitkeep`, `timing/hooks-fired.log`,
`timing/pass-cost.md`) all predate the round-3 verdict. No re-verification of the 19 copies or
20 fragments was performed or needed — round 3 is the evidence for those.

**The negation is correctly placed and correctly scoped.** It is appended at end of file,
after `.gitignore:120` (`*.log`), so the later rule wins; it is path-anchored to
`Picea.Abies.Presentation/content/demo/**`, and `*.log` still bites everywhere else —
`.squad/log/dotnet-format-2026-09-07.log` is still reported ignored. The one comment line
states the reason rather than restating the pattern. `hooks-fired.log` is now the only
tracked `*.log` in the repository, which is exactly the intent. `README.md:44` still indexes
the file under its original name, so the runbook and the tree agree with no doc edit.

**Not staged, correctly:** the `.squad/` log appends, the `decisions.md` merger output, my own
agent-memory files and the three archived drops all remain unstaged. That matches round 3's
instruction 4.

## Findings

**🔴 Must Fix:** none. **⚠️ Should Fix:** none — ⚠️-10 was the only open item and it is closed.

**💡 Nitpicks.** 💡-1, 💡-2, 💡-8 and 💡-9 carry unchanged and remain non-blocking; the chosen
resolution deliberately keeps the `.log` extension, so 💡-2 stays open by design rather than
by oversight. One new, and it is a note not a request: the negation is scoped by *path* but
open by *kind* — any future `*.log` written anywhere under `content/demo/` will now be
tracked by default. For a curated content folder under a manifest discipline that is the
right trade; it is worth remembering only if that folder ever becomes a place where tooling
writes.

## Where to stop

**Commit the 46 staged paths and nothing else.** This verdict file, the confirmation drop and
the agent-memory notes must not ride along: committing them moves `HEAD` past
`07607bf71152ef8d224352e2d2a737d8312dcb2d`, which is the commit this PASS is pinned to, and
the gate re-blocks on a changeset that already passed. If they are to be committed, that is a
separate commit afterwards — deliberately, not by `git add -A`.

## Metrics — confirmation

- **Files reviewed:** 1 changed since round 3 — `.gitignore` (+3/−0), every line.
- **Staged:** 46 paths; 44 content, `.csproj` +4/−0 (unchanged since round 2), `.gitignore` +3/−0.
- **Dimensions run:** scoped by the Lead to the single ⚠️-10 check; round 3 carries 11/11.

## Commit-boundary confirmation — `b181a154a21db627b14d1618b0202a0260812401`

**✅ PASS carries to `b181a15`.** Its sole parent is `07607bf`; `git diff --name-status 07607bf..b181a15` is exactly the 46 paths confirmed above and nothing else (44 under `content/demo/`, plus `.csproj` and `.gitignore`, all `A` bar those two `M`); every one of the 46 committed blobs equals the working-tree bytes I verified (`git rev-parse b181a15:<path>` vs `git hash-object <path>`, 46 compared, 0 mismatches), `sha256sum -c SHA256SUMS` re-proves 39/39 OK at the committed tree, no committed path is ignored (`check-ignore --stdin` exits 1 silently), the `.gitignore` negation still lands at EOF after `*.log:120` and still leaves `content/other/x.log` ignored, `hooks-fired.log` is the only tracked `*.log` in the tree, nothing under `content/demo/` has an mtime later than `15:01` (`.gitignore` `15:12`, both before the `15:16:39` commit), and the working tree holds only hook-written state (`.squad/log/*`, `.claude/docs/decisions.md`, four archived drops) and review artifacts (`.squad/design/presentation-demo/`, my agent-memory notes) — no code-shaped path is dirty.
