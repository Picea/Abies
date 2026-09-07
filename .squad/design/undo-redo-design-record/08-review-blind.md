# 👁️ Blind Review — `undo-redo` design record (branch `docs/0-undo-redo-design`)

**Reviewed:** `f0cb682bedb44d0ef9ca8a368ba0dfca0bc0b2b0..66379e7e2f9e7e5cbd5c3554369f1d5177fca176` — one commit, 67 files, +11738/−86. All 67 files are `.md`.
**History channel:** `git-history-namestatus.sh`
**Narrative reaching this context:** ⚠️ **Yes, substantially, and structurally unavoidable — see below.**

---

## ⚠️ Two blindness caveats that shape everything after this

### 1. About 85% of this change is unreadable to me by charter

`enforce-review-blindness.sh` denies me `.squad/design/`. That is where 15 of the 67 files and roughly **8,900 of the 11,738 added lines** live — `00-knowledge.md`, `00-scope.md`, `00-warden-scan.md`, `00-warden.md`, `01-track-a.md`, `02-track-b.md`, `03-convergence.md`, `04-realist-plan.md` (2,256 lines), `05-critic.md` (818), `06-spec.md` (1,555), `07-handoff.md` (577), `room-security.md` (802), the deleted `00-scope-undo-redo.md`, the modified `chore-0-squad-flow-v2-1/09-review-verdict.md` (+86), and the prior `08-review-blind.md` I am overwriting.

I did not read any of them, by `Read`, `Grep`, `Glob` **or** `Bash`. Every `git diff` and every search below was pathspec-scoped away from `.squad/design/`.

**So this is a blind review of roughly a quarter of the changeset.** That is not a hedge — it is the single most important fact `reviewer-reconcile` needs from me. The substance of the design pass (the plan, the critique, the spec, the security room, the handoff) is unreviewed by the blind half of the review pair. If those files need adversarial reading, only `reviewer-reconcile` can do it, and it will do so *after* reading the narrative, which is exactly the condition the split exists to avoid.

### 2. The readable files *are* the narrative

`.claude/docs/decisions.md` (+497), `.claude/docs/flow-changelog.md` (+41), `.claude/enforcement/refutations.md` (+47), the 30 new `.claude/agent-memory/**` notes and the two session logs are, in content, the `undo-redo` design pass's own account of itself: the architect's decision, three Critic loop-backs, the convergence verdict, the security derivation, the gate decisions and the user's reasoning at each 🛑.

Reviewing them requires reading them. I cannot review this commit and stay uncontaminated about `undo-redo`. Nothing arrived in my prompt beyond the scope statement, but the diff itself carried the narrative, and I am saying so rather than pretending the isolation held. For any *future* review of the `undo-redo` **implementation**, treat this artifact's author as no longer blind to that design.

I also note the orchestrator's scope statement said the range touches `docs/security/`. It does not — `git diff … -- docs/security/` is empty.

---

## What this change does

It commits the **paper trail** of a completed deep design pass. No behaviour changes anywhere; not one `.cs`, `.js`, `.csproj`, workflow or settings file is touched, and no file this repository executes is in the range.

Concretely, in the part I can see:

1. **Seven new decision drops** archived under `.squad/decisions/archive/2026-09/` and merged into `.claude/docs/decisions.md`: one `lead` INFO (leave `ADR-008:85` reachable), three `critic` NEEDS-CHANGES (three successive LOOP BACK TO REALIST verdicts), two `architect` INFO (the `WithHistory` decision and a post-close-out navigation amendment), and one `reviewer-reconcile` PASS (a commit-boundary confirmation for the *previous* PR, #358).
2. **A new residual `R-21`** in `.claude/enforcement/refutations.md`: `docs/adr/**` is not on `enforce-track-blindness.sh`'s `DENY` list for `dreamer-first-principles`, so ADR Consequences sections are readable by Track A as ordinary codebase. Owner `devops`, expires 2026-10-04, status open.
3. **Two `flow-changelog.md` observations**, both explicitly "level movement: none" — the ADR deny-list gap, and a claim that gate 1 caught two prior-work leaks that a human scope review had passed.
4. **Thirty new agent-memory notes** across `architect`, `critic`, `dreamer-convergence`, `dreamer-informed`, `realist`, `reviewer-reconcile`, `security-expert` and `spec-author`, plus five `MEMORY.md` index updates. This is the first time `critic`, `realist`, `spec-author` and `dreamer-convergence` have any memory at all.
5. **Session logs** for 2026-09-06 (+154 lines) and 2026-09-07 (new, 233 lines), and 39 new rows in `.squad/log/pass-cost.md`.
6. The pre-hook scope draft `00-scope-undo-redo.md` is deleted; `00-scope.md` replaces it.

Old behaviour → new: the squad's persistent memory previously had no record of the `undo-redo` pass; now it does, and four phase agents that had no cross-session memory have started accumulating it.

## Why it might be needed

Legible directly from the code. `.claude/docs/memory-policy.md` designates `.squad/design/` as the reasoning trail behind `decisions.md` and says explicitly it is **not** auto-rotated — "when someone asks 'why is it built this way', the decision drop is the summary and this is the evidence." `CLAUDE.md` requires the architect to write a decision drop at close-out. `scribe-decision-merger.sh` requires drops to pass through `.squad/decisions/inbox/`, be validated, be merged into `decisions.md` and be archived. So this commit is the mechanically-required output of finishing a deep pass, plus the agents' voluntary memory writes.

The one thing I could **not** infer is why this lands as a documentation commit on its own branch rather than travelling with the implementation. That reading is available and sensible — the design pass produced no code, and a design record is worth having on disk before the implementation PRs start — but nothing in the readable files states it.

## Is this the right approach?

Broadly yes, and the shape is what the framework's own rules prescribe. Three structural observations:

- **The size gate degrades correctly, and I verified it rather than assuming.** `pr-validation.yml` sets `hardLimit = 1500`, and `isMaintenancePath` exempts anything ending in `.md`. All 67 files are `.md`, so `maintenanceOnly` is true and 11,738 changed lines produce a warning, not a failure. That exemption is deliberate and commented (`.claude/hooks/**` and `settings.json` stay subject to the limit as code). No finding — but it is why a 11.7k-line PR is admissible here and would not be otherwise.

- **The commit is atomic where it should be.** All seven drops are archived *and* merged, exactly once each — I checked every `id:` in `.squad/decisions/archive/2026-09/*.md` against `.claude/docs/decisions.md` and got exactly one match per new drop, zero duplicates. `R-21` is the next unused id in a sequential, gap-free `R-1`…`R-21` ledger. The agent-memory indexes have no orphaned files and no dangling links (one exception, below). This is mechanically clean.

- **The design record is unusually well-grounded, and I say that from verification, not impression.** The drops make roughly twenty precise citations into source I *can* read. I checked all of them and every one is accurate, most to the exact line:

  | Claim | Verified |
  |---|---|
  | `Runtime.cs:288-291` `DispatchFromSubscription` is fire-and-forget | ✅ exact |
  | `Runtime.cs:332` assigns `_handlerRegistry.Dispatch = DispatchFromSubscription` (so DOM events and subscription deliveries are the same delegate) | ✅ exact |
  | `Runtime.cs:212-220` subscription reconcile sits outside the patch guard | ✅ exact |
  | `Runtime.cs:474` `_decisionGate.Release()` precedes the dispatch at `:494` | ✅ exact |
  | `Runtime.cs:353-365` `Batch` aborts after the model is folded | ✅ exact |
  | `Command.cs:12` `Commands.None => new Command.None()` allocates per call | ✅ exact |
  | `Subscriptions/Manager.cs:51-66` keys the diff | ✅ exact |
  | `Navigation.cs:17-29` `UrlChanges(Func<Url, Message>)` takes an app-supplied constructor | ✅ exact |
  | `Debugger/RingBuffer.cs:12` is a mutable `sealed class` | ✅ exact |
  | `Picea.Abies.csproj:24` `InternalsVisibleTo Picea.Abies.Tests` | ✅ exact |
  | `pr-validation.yml` 1500-line hard limit at ~:211 | ✅ exact |
  | `SubscriptionsDemo/Program.cs:110` 250 ms `FastTick` | ✅ exact |
  | `SubscriptionsDemo/Program.cs:140` `DateTimeOffset.UtcNow` inside `Transition` | ✅ exact |
  | `Conduit.App/Model.cs:82-96` password fields in three models | ✅ exact |
  | `Conduit.App/Messages.cs:12,17,29` three `*PasswordChanged` types | ✅ all three exact |
  | `ADR-008-immutable-state.md:85` "Trivial to implement by storing state snapshots" | ✅ exact |
  | `README.md:185,190` hands an adopter `url => new UrlChangedTo(url)` | ✅ exact |
  | `Html/Events.cs` `NextCommandId` global counter | ✅ present |
  | `RuntimeIsolationAndSubscriptionFaultTests.cs:218` multi-event `Decide` | ✅ exact |
  | `ServiceDefaults/Extensions.cs:32` registers only `AddSource("Picea.Abies")` | ✅ exact |

  Twenty for twenty. I looked for a fabricated citation and did not find one. That is worth recording explicitly, because the problems below are almost all in the *record's own metadata and self-description*, not in its engineering claims.

Where I would push back is on scope discipline: this commit carries the `undo-redo` design record **and** a `reviewer-reconcile` drop about PR #358 **and** an 86-line amendment to a *different* pass's closed review verdict (`chore-0-squad-flow-v2-1/09-review-verdict.md`). Two unrelated concerns share one commit. The `code-review` skill's PR-hygiene line ("keep PRs focused on their stated scope") applies, though the #358 material is hook-driven fallout rather than deliberate inclusion.

---

## Problems

### P1 — Decision-drop `created:`/`id:` timestamps are fabricated, and two are dated *after* the commit that introduces them

**What.** `scribe-decision-merger.sh` names archive files from `date -u +%Y-%m-%dT%H-%M-%S` at merge time (`:1202`, `:1237`), so the filename is a trustworthy UTC merge timestamp. Comparing it against each drop's self-declared `created:`:

| drop | `created:` (claimed) | actual merge (filename, UTC) | delta |
|---|---|---|---|
| `reviewer-reconcile-…T134928Z-pr358-commit-confirm` | 13:49:28Z | 13:50:07Z | +39 s ✅ |
| `lead-…T150129Z-undo-redo-adr-008-left-as-is` | 15:01:29Z | 15:02:41Z | +72 s ✅ |
| `critic-20260906T184500Z-undo-redo` | 18:45:00Z | 19:29:55Z | +45 min, plausible |
| `critic-20260906T000000Z-undo-redo-pass-2` | **00:00:00Z** | 21:34:31Z | −21.5 h |
| `critic-20260907T000000Z-undo-redo-pass3` | **00:00:00Z** | 06:34:07Z | −6.5 h |
| `architect-20260907T120000Z-undo-redo` | 12:00:00Z | 08:03:52Z | **+4 h in the future** |
| `architect-20260907T184500Z-undo-redo-navigation` | 18:45:00Z | 09:34:55Z | **+9 h in the future** |

The commit itself is authored `2026-09-07T11:56:59+02:00` = **09:56:59Z**. So `architect-20260907T184500Z` declares it was created 8 h 48 m *after* the commit that contains it. Both architect drops and all three critic drops carry round-number values (`T000000Z`, `T120000Z`, `T184500Z`); the two drops with accurate times (`lead`, `reviewer-reconcile`) are accurate to within 72 seconds. The pattern is that some agents read a clock and some invented a plausible-looking number.

**Where.** `.claude/docs/decisions.md` and `.squad/decisions/archive/2026-09/{2026-09-06T21-34-31,2026-09-07T06-34-07,2026-09-07T08-03-52,2026-09-07T09-34-55}-*.md`, `id:` and `created:` fields.

**Why it matters — three concrete harms, not just tidiness.**
1. **The record reads out of order.** `critic-20260906T000000Z-undo-redo-pass-2` sorts *before* `critic-20260906T184500Z-undo-redo`, so the second Critic pass precedes the first under id ordering. The whole point of this record is that it narrates three sequential loop-backs.
2. **`id` is the documented stable anchor.** `.claude/docs/memory-policy.md` § "Anchors and stable IDs" says the rotation hook references decisions by `id:`, not position, and that skills and charters should cite by id. `decisions.md`'s archive rule keys on entries "older than 90 days" — a future-dated `created:` postpones archival eligibility, a backdated one accelerates it.
3. **Uniqueness is at risk.** `decision-schema.md` requires `id` to be globally unique. Two drops in this commit already use `T000000Z`; a third `critic` drop on the same day with the same slug would collide outright.

**How I verified.** Read `scribe-decision-merger.sh:1202,1237` for the filename source; extracted `id:` from each archived drop and cross-matched against `decisions.md`; took the commit's author date from `git-history-namestatus.sh`. `validate()` (`:960`, `:1002`) checks field *format*, not plausibility, so nothing in the toolchain would have caught this.

### P2 — `decisions.md` cites an ADR and a trust boundary that do not exist

**What.** The architect drop's `targets:` names `docs/adr/ADR-030-undo-redo-as-a-history-program.md` and `docs/security/threat-model.md`, and its body states: *"Security: SEC-1 … SEC-7, Trust Boundary 7 (three retention shapes, each with the bound that actually applies), two threat-model rows … and two hardening-backlog fast-follows."* Checked against the tree at this commit:

- `docs/adr/` contains **no ADR-030**. The highest is ADR-028 per `CLAUDE.md`.
- `docs/security/threat-model.md` has **five** trust boundaries. There is no Trust Boundary 6, let alone 7.
- `docs/security/hardening-backlog.md` has three items, none about undo/redo or state retention.
- `docs/security/` is **untouched** by this commit (`git diff … -- docs/security/` is empty).

`.claude/agent-memory/architect/MEMORY.md` compounds it: the index line reads *"[Undo/redo is `WithHistory`](…) — **ADR-030's decision**, its deliberate exclusions…"*, presenting a non-existent ADR as the settled authority.

**Where.** `.claude/docs/decisions.md` (architect drop `architect-20260907T120000Z-undo-redo`, `targets:` and § Security), `.claude/agent-memory/architect/MEMORY.md:6`.

**Why it matters.** `decisions.md` is described in `CLAUDE.md` as the *authoritative* team-wide decision register, and is what the Lead reads before triaging. A reader who follows "Trust Boundary 7" or "ADR-030" finds nothing, and cannot tell from the text whether the reference is aspirational (a deliverable of implementation) or stale (something that existed and was lost). Every other forward-looking item in the same drop is marked as such — "the spec file … cannot compile until the end of plan step 6", "no demo adopts `WithHistory` this pass" — so the ADR and threat-model references read as accomplished by contrast. The `code-review` skill's "don't reference … in user-facing text things that don't exist" and "delete or update obsolete comments" both bear; so does the drop-schema's `targets:` field, which is documented as "paths/lines this drop pertains to" and is being used here for paths that do not yet exist.

**How I verified.** `ls docs/adr/ | grep 030`; `grep -n "Trust Boundary" docs/security/threat-model.md`; `tail -20 docs/security/hardening-backlog.md`; `git diff <range> -- docs/security/`.

### P3 — A live, verified observability defect in shipped code is recorded only inside a design drop

**What.** The first Critic drop states: *"`AddSource` matches exact names, so `Picea.Abies.History` (and, **already, `Picea.Abies.Runtime` and `Picea.Abies.Subscriptions`**) is collected nowhere."* I verified it and it is true today:

- `Picea.Abies/Runtime.cs:88` — `new ActivitySource("Picea.Abies.Runtime")`
- `Picea.Abies/Subscriptions/Manager.cs:32` — `new ActivitySource("Picea.Abies.Subscriptions")`
- `Picea.Abies.Conduit.ServiceDefaults/Extensions.cs:32` — registers `AddSource("Picea.Abies")`, exact-match, no wildcard.

So the framework's runtime and subscription traces are dropped on the floor in Conduit today, independent of anything undo/redo.

**Where.** The finding lives at `.claude/docs/decisions.md`, `critic-20260906T184500Z-undo-redo`, `high:` list. The defect lives at `Picea.Abies.Conduit.ServiceDefaults/Extensions.cs:32`.

**Why it matters.** This is a pre-existing production defect against the team's own observability principle (`.claude/skills/functional-ddd` § OTEL: *"Every functional flow has full trace coverage … Register the `ActivitySource` in `ServiceDefaults`"*). It is not scoped to the pass that discovered it. Filed where it is, it will be found only by someone reading a `critic` drop about undo/redo. It is not in `docs/security/hardening-backlog.md`, not in a GitHub issue, and not in `refutations.md` — and this commit is precisely the moment at which those records were being updated for other things. The `code-review` skill's "track deferred work with GitHub issues and searchable TODOs" applies.

### P4 — `pass-cost.md` documents itself as per-phase and is actually cumulative, and double-counts phases

**What.** `.squad/log/pass-cost.md` header: *"**Wall-clock per design phase**, keyed by slug."* The data is cumulative-since-session-start — 210m17s for the first `architect` row rising monotonically to 1366m05s. The correction exists only in one agent's private notebook: `.claude/agent-memory/dreamer-convergence/pass-cost-instrument-reads-cumulative.md` — *"take deltas between consecutive rows or the denominator is nonsense."*

Separately, phases are logged twice. Five agent/phase pairs appear as adjacent duplicate rows seconds apart — `scope-warden` 14:51:01/15:04:21 is a genuine re-run, but `architect` 18:23:37/18:24:20 (43 s), `realist` 08:13:04/08:14:03 (59 s), `spec-author` 09:24:52/09:26:00 (68 s) and `architect` 09:33:38/09:34:59 (81 s) are the same stop recorded twice. Under the delta convention that manufactures a spurious ~1-minute phase and shortens the real one.

**Where.** `.squad/log/pass-cost.md` (header line 3, and the table).

**Why it matters.** The file's stated purpose is to be *the denominator* for whether the dual-track Dreamer earns its cost — the header says as much, and it is the sole quantitative input to `dreamer-convergence`'s calibration ledger. A file whose own header describes its units wrongly, and whose correction is private to one agent, will be misread by every other reader, including a human doing a retro. Note that `.claude/agent-memory/dreamer-convergence/calibration-dual-track.md` *does* apply the delta convention correctly (I checked its arithmetic: 240m07s − 231m23s = 8m44s for Track A's marginal cost, and 240m07s − 230m21s = 9m46s for the pair — both exact). So the one agent that knows the convention uses it right, and the file misleads everyone else.

### P5 — Session logs are ~90% duplicate noise and do not record what any subagent returned

**What.** `.squad/log/2026-09-07-session.md` has 239 entry rows. **214** are keyed by an opaque hex id (`a32e31e7e8c688333`); **25** are keyed by an agent name. Every hex row duplicates, verbatim, the text of the named row it precedes — I counted runs of up to 14 identical lines.

Worse than the volume: the text attributed to each subagent is not that subagent's summary. The `realist` row at 06:00:40Z reads *"The Realist is writing revision 3 with all three answers and the security follow-up in its brief…"* — the Lead's own forward-looking message about what is **about to** happen, not what the Realist reported. Entries are also truncated mid-word with no ellipsis (`"…is the only t"`, `"…I'll surface the "`).

**Where.** `.squad/log/2026-09-06-session.md` (+154), `.squad/log/2026-09-07-session.md` (233 lines). Producer: `.claude/hooks/session-logger.sh` on `SubagentStop`.

**Why it matters.** `CLAUDE.md` tells the Lead to read *"the most recent 1–2 files in `.squad/log/` if continuing prior work"*, and `memory-policy.md` keeps 30 of these in place before archiving. A log whose signal-to-noise is 25:214 and which records the orchestrator's intentions instead of the agents' results does not serve either purpose. This is the second consecutive day with the same shape, so it is a hook defect, not a one-off. It also inflates every future session-log commit — 387 lines of log in a 67-file commit, most of it duplicated text.

### P6 — `flow-changelog.md` states the wrong artifact for `scope-warden`'s input

**What.** The new entry reads: *"`scope-warden`, **reading `00-warden.md`'s category** — prior work presented as reference material, the one no regex reaches — found two leaks in it."*

`scope-warden` **writes** `00-warden.md`; it reads `00-warden-scan.md`. `CLAUDE.md` § 3 rule 2 is explicit: *"Dispatch `scope-warden`, which reads that scan, adds the one category no regex reaches … and writes `00-warden.md`."* `memory-policy.md`'s artifact table says the same. The sentence as written has the warden reading its own output.

**Where.** `.claude/docs/flow-changelog.md`, entry "2026-09-06 — Observation: gate 1 caught two prior-work leaks a human review passed".

**Why it matters.** `flow-changelog.md` is the framework's own record of how the flow behaves and what level each control sits at. An entry that misdescribes the gate's data flow, in the entry whose whole purpose is to record evidence *for that gate*, is the worst place for the error to be. Cheap fix; I flag it because the file's authority depends on this kind of precision.

### P7 — The gate-1 evidence claim cannot be corroborated from anything readable, and the one readable quote says the opposite

**What.** The same `flow-changelog.md` entry is titled *"gate 1 caught two prior-work leaks a human review passed"* and is offered as evidence justifying gate 1's mechanical-plus-judgement split. Every source it rests on — `00-scope-undo-redo.md` (deleted in this commit), `00-warden.md`, `00-scope.md` — is inside `.squad/design/` and therefore unreadable to me.

The only warden output visible outside that directory is in `.squad/log/2026-09-06-session.md` at 14:51:37Z, where the Lead quotes it verbatim: *"## Scope Status: **CLEAN** — No decision ids, pattern names, denied paths, or prior work presented as reference material…"*.

These may well both be true — leaks in the *deleted draft*, CLEAN on the *dispatched* scope, with two warden runs at 14:51:01 and 15:04:21 in `pass-cost.md` to match. But I cannot establish that, and neither can any reader working outside `.squad/design/`.

**Where.** `.claude/docs/flow-changelog.md`; corroborating quote at `.squad/log/2026-09-06-session.md`.

**Why it matters.** This is the framework grading its own control's effectiveness, and the grade is recorded in `flow-changelog.md`, a governance document. The evidence sits entirely inside the region a blind reviewer is forbidden to enter — so the one role structurally positioned to check a self-serving claim is the one role that cannot. That is a design property of the review split worth naming, not just a documentation nit. At minimum the entry should quote the two leaked sentences inline, so the claim is checkable without opening the design directory.

### P8 — `picea-xml-is-the-kernel-contract.md` omits the one fact that defeats its own purpose

**What.** Two `critic`/`realist` memory notes instruct future agents to settle kernel questions from `~/.nuget/packages/picea/<version>/lib/net10.0/Picea.xml`, and the critic note adds "(1.0.0 as of 2026-09)". Both are right that the version must be matched. Neither records what actually happens if you match it to the wrong project:

- `picea/1.0.0/…/Picea.xml` — **1369 lines**, fully documents `AutomatonRuntime<5>.Dispatch`, `.InterpretEffect`, `.Reset`. This is what `Picea.Abies` references and what the B8 blocker's derivation rests on. Sound.
- `picea/1.0.27-rc-0002/…/Picea.xml` — **477 lines**. It declares the type `T:Picea.AutomatonRuntime\`5` and documents **none of its members**. No `Dispatch`, no `InterpretEffect`.

Four projects (`Conduit.Api`, `Conduit.Api.Tests`, `Conduit.ReadStore.PostgreSQL`, `…PostgreSQL.Tests`) reference the rc.

**Where.** `.claude/agent-memory/critic/picea-xml-is-the-kernel-contract.md`, `.claude/agent-memory/realist/picea-package-xml-answers-kernel-questions.md`.

**Why it matters.** The realist note's stated purpose is *"Read it before declaring anything about the kernel unknowable."* An agent that follows it, lands on the rc package, and finds `AutomatonRuntime` with no documented members will conclude exactly the "unknowable" the note exists to prevent — and the note gives no signal that it has landed in the wrong place. One sentence fixes it. I raise it because these two notes are the only cross-session mechanism by which the B8 derivation stays reproducible.

### P9 — A closed review verdict from a different pass gained 86 lines, and I cannot review the change

`.squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md` is **modified**, +86, in this commit. It is a terminal verdict artifact for PR #358, a pass that closed and merged before this branch existed. The accompanying drop explains the amendment as a commit-boundary confirmation that re-ran nothing. That explanation is plausible and the drop is internally careful about it.

I flag it only because the file is inside `.squad/design/` and I could not read one byte of the change. Amending a closed verdict artifact after the verdict is a record-integrity question by shape; whether this instance is benign is `reviewer-reconcile`'s to determine, with the file open.

### P10 — Housekeeping

- **Empty untracked `.claude/agent-memory/reviewer/`** (created 2026-09-06 13:53, zero files, zero tracked). Residue of the `reviewer` → `reviewer-reconcile` `git mv` recorded in `reviewer-reconcile/MEMORY.md`. Harmless; it is the only directory in `.claude/agent-memory/` with no `MEMORY.md`, so any tooling that iterates the directory hits a special case for nothing.
- **`docs/security/hardening-backlog.md` § Suggested Owners still assigns work to `reviewer`**, a role this repository split into `reviewer-blind` and `reviewer-reconcile` on 2026-09-06. Outside this diff, but this commit is where that roster change was written into memory, and `tech-writer`'s own note `stale-roster-references-rename-vs-note.md` covers exactly this case.
- **`pass-cost.md`'s final row is `2026-09-07T10:03:32Z`**, seven minutes *after* the commit's author date of 09:56:59Z, and the file is committed clean. Consistent with the commit having been amended or recreated (the orchestrator's brief mentions a mis-staged commit that no longer exists), but it does mean the commit's author timestamp does not bound its content. Worth one sentence of confirmation rather than a finding.
- **Working tree is not clean**: `.squad/log/2026-09-07-session.md` is modified post-commit by `session-logger.sh`. Expected append-only hook state; noting it so a later `git status` check is not read as drift.
- **`R-21` is inserted above the `## Extensions` section** rather than at end-of-file, in a document whose opening line declares it append-only. It edits nothing in place, so I read this as compliant in spirit; raising it only so `reviewer-reconcile` can confirm the intended reading, since the file's own convention text is cited by four other entries.

---

## What I could not determine from the code alone

1. **Everything in `.squad/design/undo-redo/` — 8,900 lines, the substance of the pass.** Is the plan sound? Does `06-spec.md` state properties that can actually fail? Does `room-security.md` reach the conclusion `security-expert`'s memory note claims it does? Does `05-critic.md`'s fourth pass actually approve what the architect drop says it approved? I have no basis for any of it. This is not a question the narrative should answer for me — it is a gap only a reviewer with read access to those files can close.
2. **Are ADR-030, Trust Boundary 7 and the two threat-model rows (P2) deliverables of the not-yet-started implementation, or references to work believed complete?** The drop's phrasing does not distinguish, and the answer determines whether P2 is a wording fix or a missing-artifact finding.
3. **What did the two prior-work leaks (P7) actually say?** Quoting them inline would make the framework's own effectiveness claim checkable from outside the design directory.
4. **Were the fabricated timestamps (P1) deliberate — a convention I have not found — or an agent inventing a plausible number?** I found no convention permitting them; `decision-schema.md` gives the format and its examples all use precise times. If there is a documented allowance, it is somewhere I did not look.
5. **Is the observability gap (P3) already tracked somewhere I cannot see** — an open issue, or a line in `05-critic.md`'s own follow-up list? I checked `hardening-backlog.md` and `refutations.md` and found nothing; I did not check GitHub, and `gh issue view` is denied to me.
6. **Why does this land as a standalone documentation branch** rather than as the first commit of the implementation PR series? Legible design intent, not stated anywhere readable.
7. **Was this commit amended or recreated** (P10, third bullet), and if so, is the content at 10:03:32Z the intended final state?
8. **Is the `reviewer-reconcile` PR #358 drop's presence in this commit deliberate**, or hook fallout from the previous pass's inbox not having been drained before the new branch was cut?

---

*Verdict, severity grading and the eleven dimensions are `reviewer-reconcile`'s. This artifact was on disk before any narrative from `.squad/design/` was read — and, for this changeset specifically, `.squad/design/` was never read at all.*
