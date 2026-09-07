# 🤝 Handoff — `undo-redo`

**Pass slug:** `undo-redo`. **Direction:** H1 — `WithHistory`, with refusal at the effect boundary.
**Closing artifact.** Written after all five gates were answered by the user. Read with
`00-scope.md` (as amended, INV-1 … INV-7), `04-realist-plan.md` revision 4, `05-critic.md` fourth
pass, `06-spec.md` as approved and amended, and `room-security.md` including all three dated
follow-ups.

The design artifacts remain the source of truth. This file is the execution contract: who does what,
in what order, against which bar, and what a later reader must not mistake for a technical finding.

---

## 1. Approvals — all five, verified before writing this file

| Gate | What was approved | Evidence |
|---|---|---|
| 1 | `00-scope.md`, after `scope-warden`'s re-run returned **CLEAN** (0 findings). The ADR-008 line 85 contamination was left reachable **deliberately**, as a recorded experiment; the pre-hook draft `00-scope-undo-redo.md` was removed from the pass directory before either Dreamer was dispatched | `00-warden.md`; `00-warden-scan.md`; `lead-20260906T150129Z-undo-redo-adr-008-left-as-is` |
| 2 | Convergence direction **H1**, with **refusal at the effect boundary** — typed, explained, and answerable in advance | `03-convergence.md` § *Gate 2 — the user's decision (answered)* |
| 3 | The Realist plan, **revision 4**, after three Critic loop-backs | `04-realist-plan.md` rev. 4 |
| 4 | The Critic's **fourth pass — APPROVED WITH MITIGATIONS** (five 🟠 S25–S29, five 🟡; no blockers, no revision 5) | `05-critic.md` fourth pass |
| 5 | The spec at `06-spec.md` **as amended**: spec line 12 (undo out of a terminal state) moved into the locked file as **A9**; lines 10, 11, 13 stay in plan steps 6 and 10 | `06-spec.md` § *Approval Request — answered 2026-09-07* |
| **5a — after close-out** | **`[R4-nav]` — incoming navigation is classified.** Raised by the user after close-out, not by any phase. `00-scope.md`'s **INV-2** amended so the world it quantifies over includes the browser's location; `04-realist-plan.md` gains **one classification row** and nothing else; `06-spec.md` gains INV-2's **second property**, obligations lines **16** and **17**, and acceptance test **A10**. The Critic **CONFIRMED WITH A BOUNDED LIST** — two 🔴, four 🟠, seven 🟡, **no revision 5, no gate reopened, no mechanism asked for** — and the user accepted **all thirteen** mitigations | `05-critic.md` § *confirmation pass, 2026-09-07*; `04-realist-plan.md` § *header* `[R4-nav]` and § *The classification rule*; `06-spec.md` § *Amendment 3* |

No gate was skipped and none was inferred. Gate 1 counts as an approval in its own right: the warden's
report existing is not the same as the user having answered it, and the user answered it.

**`[R4-nav]` is the pass's third `00-scope.md` amendment**, and the numbering matters because
`04-realist-plan.md` § *The scope clause* tracks them. Amendment 1: **INV-1**'s availability
precondition. Amendment 2: **INV-7**'s definition of a movement, with the `abies:history:`
reservation. Amendment 3: **INV-2**'s location clause, § 2.5 below. All three are in the file. The
spec's own amendment numbering is a different sequence (A9 is *its* amendment 1) and the two must not
be conflated.

**The classification rule itself, not merely the invariant it answers to, is a standing constraint on
implementation.** It is stated in § 2.5, carried in standing decision 7, and mapped to owners in
§ 6.1. It was absent from this file at close-out — the file predates the amendment — which is
`05-critic.md` **B11**'s second half, and this is where that is discharged.

---

## 2. Positions carried, because the record must not flatten them

### 2.1 Refusal versus crossing at the effect boundary — a product call between two defensible engineering positions

Both tracks reached a coherent, well-argued, *different* answer. Convergence declined to resolve it
and put it to the user as the first question at the 🛑. It is recorded here in convergence's own
terms so that no later reader mistakes a product decision for a finding that one track was wrong.

- **Track A (first principles) — refuse. Derived, not chosen.** The observable state is
  `model ⊗ world`; the cursor moves over the first factor only, so moving back past a transition
  that spoke to the world leaves the factors disagreeing. Concrete falsifiers in *this* codebase:
  undo across `LoginSubmitted` restores `IsSubmitting = false` while a request is in flight, so a
  second submit double-posts; undo across `FavoriteArticle` shows `Favorited = false` while the
  server holds `true`. Track A then found the hole that made the diagnosis load-bearing: **INV-2 as
  written cannot catch either case**, because both models were literally visited and are therefore
  trivially "reachable by ordinary actions". Since the framework cannot know what any given
  `Command` means, refusal is the only position it can hold honestly. A3 prices the exception — a
  commitment is crossable iff the application names an inverse.
- **Track B (informed) — cross, and document that effects are not recalled. Evidenced, not
  careless.** It is the industry's settled answer: the canonical Redux DevTools limitation, and
  Fowler's remedy for irreversible external interaction is a *compensating* action appended forward
  rather than a rewind. Mitigated by explicit documentation plus two opt-in escapes — compensation
  (Qt `QUndoCommand::undo()`) and **deferral** (Gmail's Undo Send is a 5–30 s hold, not a reversal),
  the latter being the correct answer for genuinely irrevocable actions and one Track A did not
  reach.
- **The user's call: refuse** — with the refusal typed, explained, and visible in advance. **This is
  a product decision selecting between two defensible engineering positions. It is not a finding
  that Track B was wrong.** Track B's cross-with-a-documented-contract position is excluded by
  product decision, not by analysis. ADR-030 must say this in these words.

Deferral, which was Track B's escape and is genuinely the right answer for irrevocable actions, is
**out of this pass** with a stated reason: it means the framework holding commands back, which is a
fifth runtime seam. Named as a follow-on candidate below.

### 2.2 The unit of history — a withheld preference, derived independently by both tracks

The user named *"at what level undo operates"* the central question of the pass (`00-scope.md`
degree of freedom 1) and **deliberately withheld their own preference so it would be derived rather
than confirmed.** No phase was permitted to narrow that axis on a guess.

Both tracks landed on **one dispatch = one undoable step**, without contact, from opposite methods —
Track A from the kernel's structure (one dispatch funnel, `Transition` returning `(TModel, Command)`,
subscriptions derived from the model, `WithView` as the existing composition), Track B from the
production evidence (redux-undo, `UndoList`, ProseMirror, CodeMirror, Qt). The user then confirmed
the same answer at gate 2. **This is the strongest single result of running two blind tracks in this
repository**, and it is recorded as such: the withheld preference was matched by two independent
derivations, which is what a forced answer looks like.

Optional 500 ms coalescing (step 14, off by default, separable, may be cut) is Track B's number
closing Track A's one granularity miss.

### 2.3 The projection lens — deferred at gate 3, reversed at the first Critic pause

At **gate 3** the user deferred A3's projection lens: the plan was to ship the whole-model policy and
treat a projection as a later refinement, on the ergonomics argument that asking every adopter for a
`get`/`put` pair was too much.

At the **first Critic pause** the user reversed that, and the reason is on the record and is not a
matter of taste: the Critic showed (S9/S10, later compounded by B9) that under the whole-model policy
alone, undo in a Conduit-shaped application stops at every fetch and every navigation, permanently,
and redo was **silently destroyed** rather than refused. The lens is not a refinement of that
behaviour — it is the only instrument that makes the feature function at all in an application with a
world or with a model-mutating subscription, and it does so **without a fifth runtime seam**, which
exemption-by-provenance would have required. The erased form (`Restore` / `SameUndoable`, no fifth
type parameter) was kept from the gate-3 decision; what was reversed is that a projection is
optional in practice.

The consequence must ship as first-class text, not as a footnote: **an application with a
model-mutating subscription must declare a projection or redo will not function**, and under the
whole-model policy **undo's reach also expires on a wall clock** at `Depth` ÷ the subscription's rate
— twenty-five seconds in `SubscriptionsDemo`'s shape (Critic S25(b)).

The erasure had a security cost that was not visible when it was taken: A3's un-erased form, where a
`Step` stores `TUndoable`, would have made B7 structurally impossible. That is on the record in
`04-realist-plan.md` § *Security* and is not reopened here.

### 2.4 Gate 2's "discard the forward branch" rule, superseded

**Gate 2 settled:** discard the forward branch when the user acts after undoing. **Revision 4
supersedes it:** *refuse across the superseded branch.* The branch is preserved, its first edge
sealed with `SupersededByNewAction(cause)`, and `Forward(h)` returns
`BlockedBySupersedingAction(cause)` before the press, naming the message responsible.

**Why the original reason does not reach a sealed edge.** Gate 2's rule rested on Track A's forcing
argument: a *retained* forward branch gives the cursor two forward destinations, so redo becomes a
relation, a relation has no inverse, and INV-3 becomes unstatable. That argument is about a branch
that is retained **and crossable**. A superseded branch is retained and **never** crossable —
`Forward(h)` refuses at its first edge in both `Decide` and `Transition`, so no message sequence can
reach it. Redo stays a partial function and INV-3, which quantifies over the case where redo is
available, is untouched. The Critic verified this independently rather than accepting the account:
**no edge ever returns to `Crossable`** (`sealTops` and `record`'s `ReplaceTop` only seal;
`StepBack`/`StepForward` push *fresh* `Crossable` steps rather than mutating existing ones), so a
superseded entry is unreachable for the life of the history.

The change is a strengthening of gate 2's own instinct — the same instinct that chose a typed refusal
at the effect boundary — applied to the one boundary that was still silent. It is the answer to
`00-scope.md` open question 3, and it is **spec line 14**.

Its price, stated: retention. A superseded `Step.Model` is retained and can never be restored,
bounded at `2 × Depth` by the new `Future` trim, and — per Critic **S29** — retained for a duration
that is **session-lifetime, not `Depth`-dispatch-bounded**, because only a further long run of undos
trims `Future`. It buys diagnosability, not reach.

### 2.5 `[R4-nav]` — incoming navigation is the world, and the seal is forced rather than chosen

Raised by the user after close-out. Before the amendment, only navigation *before* anything was
recorded had a rule (S23, standing decision 7). After `Origin` becomes `Established`, a `UrlChanged`
the browser delivered on a **back or forward press** was enveloped like any other message and took
the `record` row — so undo would restore a model carrying the previous page's `Route` while the
browser stayed where the user put it, and the browser's own stack walked away from the application's
history.

**The rule, one row beside the re-basing rule:**

> **`origin is UrlChanged && Origin is Established -> seal, SealedByWorld(UrlChanged)`.** Both
> incident edges. `Backward(h)` and `Forward(h)` refuse with the navigation as cause.

**Why it is forced.** Of the four classification rows, `record` and `pass` each produce a
`(model, location)` pair no ordinary interaction could produce — the amended INV-2's new falsifier
verbatim, one press apart; `rebase` is `Fresh`-only by construction and unavailable in this window;
`seal` is what is left, and it satisfies the invariant exactly, because `Present.Route` moves with
the location in the same transition and both edges refuse thereafter. *"The two move together or not
at all."* The Critic derived this independently rather than accepting the account
(`05-critic.md` § *confirmation pass* answer 3 and § 🟢 1); the derivation is recorded beside the rule
at `04-realist-plan.md` § *The classification rule*, `[R4-nav]`. **A reader who cannot reconstruct it
will read the seal as a preference — it is not.**

**Why `INV-3` did not also need a location clause, and why that is a finding rather than an
omission.** `Present.Route` can only move on a transition whose origin is `UrlChanged`; after the
first `record` every such transition seals **both** incident edges, and before it there is nothing to
cross. **The location is therefore constant across any window in which a movement is permitted.**
INV-3's round trip requires `Backward` **and** `Forward` to be `Available`: an undo pushes a fresh
`Crossable` step onto `Future`, and a `UrlChanged` arriving before the redo seals exactly that step,
so the property skips on its guard rather than failing. A location conjunct added to INV-3 would be
one **no property could ever turn red on**, which is worse than leaving it out. Under a projection
the same conclusion arrives by the other road: `Restore` keeps `current`'s `Route`, so a movement
never touches the location. The derivation is at `04-realist-plan.md` ~`:1261-1292` (§ *The
classification rule*, `[R4-nav]`, the two paragraphs headed *Why `seal` is the only row the amended
`INV-2` leaves* and *Why `INV-3` needs no location clause*). `00-scope.md`'s INV-3 is **correctly
untouched**, and this paragraph exists so that nobody re-raises the omission at review as an
oversight.

**The guarantee is conditional on the adopter's own wiring, and the condition is stated where it can
be read rather than in the invariant.** Incoming navigation reaches a WebAssembly program through an
application-supplied converter, so the seal fires only for the framework's `Picea.Abies.UrlChanged`
(`05-critic.md` **B10**). `00-scope.md`'s INV-2 is deliberately left asserting the guarantee
unconditionally; the qualification lives in **`06-spec.md` obligations line 17**, in line 2's
register — an obligation on the application that no framework property can quantify over. That
placement was the user's choice of the Critic's **mitigation (1)**, doc-only, and it is why the scope
was not re-amended a fourth time.

---

## 3. Standing decisions carried into implementation

Each of these was settled by the user at a gate. They are constraints on execution, not suggestions.

| # | Decision | Consequence for implementers |
|---|---|---|
| 1 | **A3(b) — "a commitment is crossable iff the application names an inverse" — excluded to a later pass, with its own invariant** | No `inverse : Command → Command option` member anywhere. The command monoid's invertible sub-algebra stays trivial this pass. The later pass owes a **new invariant** governing crossing, because INV-2 does not cover a crossed commitment — that is exactly the `model ⊗ world` hole Track A found, and it is only closed here *because* nothing crosses |
| 2 | **No demo and no template adopts `WithHistory`** | `Picea.Abies.Counter` is load-bearing for the benchmarks, the `dotnet new` templates and four E2E fixtures across all heads. Adoption is demonstrated by tests, the benchmark and the guide only. It also means the CI benchmark gate is **structurally silent** on this pass, which step 7's report must say in one sentence |
| 3 | **ADR-008 line 85 is amended at implementation; git history keeps the original** | `tech-writer`, step 13: replace *"Undo/redo: Trivial to implement by storing state snapshots"* with a pointer to ADR-030. Do **not** rewrite history, and do **not** annotate the amendment as a correction of a contaminating line — the amendment is substantive (the claim of triviality is false), and the experiment's record lives in `03-convergence.md` and in the decision drop |
| 4 | **Hand-rolled seeded generators; no new test dependency** | No FsCheck, no Gherkin, no BDD framework. Fixed-seed corpus runs in CI; the failing seed is printed in every assertion message (step 8(h)) |
| 5 | **Seven sequential PRs to `main`** | Per the PR cut in `04-realist-plan.md` § *Delivery*, driven by `pr-validation.yml:211`'s 1500-line hard limit. PR 5 may split 5a/5b; **PR 6 may split 6a/6b, pre-authorised**. Each PR terminates at `reviewer-blind` → `reviewer-reconcile` |
| 6 | **`SensitiveCause`, no `I` prefix** | The register's *No I-Prefix* rule. Revision 2 said `ISensitiveCause` in seven places; revision 4 says it in none. The security room's original text uses the prefixed name — read `04-realist-plan.md` SEC-1 as authoritative over `room-security.md:160` |
| 7 | **The S23 judgement call is upheld** | Origin re-basing keeps its **behaviour** and corrects its **description**. Setting `Origin := Established` in `rebase` would make the second and third pre-record navigations *recorded* undo stops whose crossing restores a page's model without its URL. *"Trading a wrong sentence for wrong behaviour is a bad trade."* Implement from the corrected statement: **while nothing has been recorded, every `UrlChanged` re-bases and records no undo stop, on every head; after the first `record`, an incoming `UrlChanged` seals both incident edges (`[R4-nav]`, § 2.5); every other message classifies by the four-row table.** ⚠️ The closing clause of this decision read *"after the first `record`, navigation classifies like any other message"* until 2026-09-07. That was true when written and is **false** under `[R4-nav]`; it is corrected here per `05-critic.md` **B11**, and the same clause is corrected in `04-realist-plan.md` § *Origin re-basing*. Do not implement from any copy that still carries it |
| 8 | **The item-6 judgement call is upheld** | SEC-3 clause (b)'s reachability set is **not** widened to name `Movement.Held(Anchor)`. `security-expert` declined the Critic's own proposed rewording with a reason the Critic accepted: a clause discharged by `Scrub` cannot name a site `Scrub` must not reach, and widening it would make the clause false the instant it is adopted. The anchor gets its own honestly-scoped guarantee (TB7, the DEBUG-row bullet, `Anchor_never_reaches_a_release_path_surface`, a guide sentence, a fast-follow) |

Two further standing facts, carried because they are easy to lose: **ADR-025 was examined and needs
no superseding** — nothing moves out of `#if DEBUG`, including `RingBuffer<T>`; and the
`runtime-seams-anchor-replay-gating` **revisit trigger does not fire** — the wrapper declares no
subscription and only *answers* the question `Runtime.Render` already asks at `Runtime.cs:214`. Zero
lines of `Runtime.cs`.

---

## 4. The spec test and the bar

**Spec test: `Picea.Abies.Tests/History/UndoRedoSpec.cs`. Implementation passes when this test passes
without modification.**

- Immutable for this feature from the approval commit. Editing it to match what the code does,
  adding `[Skip]`, or implementing it literally while knowing it produces wrong behaviour are all
  forbidden and are 🔴 Must Fix at review.
- If implementation reveals the spec is wrong: `csharp-dev` **stops**, marks `// SPEC CONFLICT:` on
  the test, and hands back with options and a recommendation. `spec-author` produces an updated
  spec, the user re-approves, then implementation resumes.
- Level: **workflow-direct through a real in-process `Runtime`**. No AppHost, no Playwright, no
  `WebApplicationFactory`, no Testcontainers. This is not an exception being claimed against the
  *Aspire AppHost Is the Test Fixture (Amended 2026-09-02)* entry — that entry governs
  cross-service tests, and this is a framework library with no service and no adopter.
- **At the point the spec first compiles — the end of plan step 6 — every test in it must be
  observed red for the right reason before any of them is made green**, and each named falsifier
  must be observed before its step is closed.
- **Plan file-table amendment, flagged by `spec-author` rather than slipped in:**
  `Picea.Abies.Tests/SpecAttribute.cs` — a five-line `[Spec]` attribute this repository does not
  have. It exists for `reviewer-reconcile`'s grep, not for execution. Owner `csharp-dev`; it lands
  with the spec file. It is **not** a `csproj` change and does not touch the deliberately-unchanged
  list.

### 4.1 The review brief for step 8 — mandatory wording

The lock has a known hole and it must be closed at review rather than by locking more files: **a
weakened generator or a permissive `DocumentComparer` can make a locked property vacuous without the
locked file ever changing.** Two things close it — the generator contract is part of the approved
spec text, and every property carries a named falsifier a weakened generator would fail to
reproduce.

> **The step-8 review brief must state that `reviewer-blind` reads
> `Picea.Abies.Tests/History/Generators.cs` and the `DocumentComparer` with the same weight as the
> spec.** They are outside the lock, they can silently void it, and they are the one place in this
> pass where a green suite would prove nothing.

This wording is not optional and is not a paraphrase to be improved. It goes verbatim into the PR 5
review request and into PR 6's if the comparer moves.

---

## 5. Security — SEC-1 … SEC-7, as landed

Owner of the threat-model deliverables: `security-expert` (step 16). Owner of the code: `csharp-dev`.
Owner of the guide text: `tech-writer` (step 13). Full text in `04-realist-plan.md` § *Security* and
`room-security.md`.

| # | Requirement | Lands in |
|---|---|---|
| **SEC-1** | `SensitiveCause : Message` marker (**no `I` prefix**) plus `HistoryRedacted(string OriginalTypeName) : Message`. Marker idiom, no attribute, no reflection, trim/AOT-safe | steps 2, 4 |
| **SEC-2** | Redaction at record time: `cause is SensitiveCause ⇒ store new HistoryRedacted(cause.GetType().Name)`. Checked **unconditionally by the wrapper**, never gated by `TPolicy` | step 6 |
| **SEC-3** | Two clauses, neither subsuming the other. **(a)** no original payload of a `SensitiveCause` message reachable via any `Step.Cause`; **(b)** no field value the model's `Scrub` removes reachable via any `Step.Model` in `Past` **or** `Future` — with the **joint-lawfulness precondition**: (b) holds only when `Restore`/`SameUndoable`/`Scrub` are jointly lawful under L1–L6. Reachability set deliberately **not** widened to the anchor (§ 3, decision 8) | step 8(g), plus 8(m) as a third, explicitly-not-(b) test |
| **SEC-4** | Telemetry backstop: spans record `Cause.GetType().Name` and never the cause itself or any serialised form; `Movement` is never tagged at all | step 11 |
| **SEC-5** | The guide states the marker **and `Scrub`, in the same worked example as the projection**, with the **negative form directly beneath** (override `Scrub` alone, and the L5 test that catches it). Uses the Conduit password fields and `SettingsModel.Password`. Carries the DEBUG `JsonPolymorphic` framing naming **`HistoryEvent.Enveloped`** as well as `HistoryRedacted`, because `Enveloped` is the type that carries the inner payloads. Adds the one sentence on the unscrubbed anchor to the chrome-contract section | step 13 |
| **SEC-6** | Threat-model: two *Threats and Mitigations* rows split High/server-side and Medium/client-only; one *Open Risks* entry for the un-retrofitted `SerializeMessageArgs` leak; **Trust Boundary 7**. Rows must say explicitly that `Scrub` and `SensitiveCause` are **independent** mitigations of **different** fields, neither a superset of the other | step 16 |
| **SEC-7** | Fast-follow register: the pre-existing DEBUG-only `SerializeMessageArgs` `ToString()` leak (`DebuggerMachine.cs:408-422`), and the serialization-boundary substitution for `Held<TModel>` — the only mechanism that would properly close the anchor's DEBUG-snapshot exposure, declined mid-loop by both the Critic and the room | close-out (this file) + `hardening-backlog.md` at step 16 |

### 5.1 Trust Boundary 7 — the S29 lifetime wording, as `security-expert` wrote it

The room's third follow-up (`room-security.md` § *Follow-up — 2026-09-07 (III)*) supersedes the
single undifferentiated "extended in-memory retention window" clause. Step 16 applies **this**
enumeration, which names three shapes each with the bound that actually applies to it:

> **7. Runtime state-retention boundary** — application state and dispatched messages crossing from
> a single, ephemeral dispatch (consumed and discarded once `Transition` returns) into: an in-memory
> retention window bounded by `TPolicy.Depth` dispatches (`Picea.Abies.History.Past`, any
> bounded-history feature); the same window's superseded-but-preserved branch
> (`Picea.Abies.History.Future`, under a policy that retains rather than clears a divergence),
> bounded in count at `2 × Depth` but **not** in time — it persists for the remainder of the session
> unless a further long run of undos trims it; a bounded, unscrubbed live model retained in
> `Movement.Held(Anchor)` for the duration of a bracket; or a developer-portable diagnostic artifact
> (`Picea.Abies.Debugger` snapshot/export) — on any head including server-side circuits under
> InteractiveServer.

Naming the three separately is the point: it is what stops the next reader borrowing `Past`'s short
bound for `Future`'s long one, which is the error the room's own prose made at `room-security.md:589`.
That sentence — *"`Step.Model` is retained for up to `TPolicy.Depth` (100) subsequent dispatches"* —
**must not be re-quoted as written**. Its honest restatement: `Step.Model` in `Past` remains
`Depth`-dispatch-bounded; `Step.Model` in a superseded `Future` branch is count-bounded at
`2 × Depth` but not dispatch- or time-bounded within a session.

The anchor answer (S20/Q2) is **unchanged** by this. It never rested on the `Step.Model` comparison;
it rested on the anchor's own bound, the `Hold`/`Settle` contract with `blur`, `pointercancel` and
`Clear` as its in-band closers. If anything the anchor's bracket-duration bound is now demonstrably
*tighter* than `Future`'s.

### 5.2 Threat-model rows for step 16

1. **Row 1 — release-reachable.** Undo/redo history retains credential-bearing dispatched messages
   (`Step.Cause`) in process memory across all heads including server-side InteractiveServer
   circuits. STRIDE: Information Disclosure. Entry point: `Picea.Abies.History.WithHistory`.
   **Split** High (server-side, many concurrent circuits in one process) / Medium (client-only).
   Mitigation: SEC-1 + SEC-2. Test: `SensitiveCause_never_retains_original_payload`.
2. **Row 2 — the DEBUG row.** DEBUG-only `ExportSession` can surface plaintext values once an
   application adds `JsonPolymorphic` metadata. It gains **two** bullets, at the same attachment
   point, same severity (Medium), same status (⚠️ Partially mitigated), **not** new rows:
   - the anchor (S20): `Movement.Held(Anchor)` is a **distinct reachable site**, reached through
     `History<TModel>.Movement` rather than `Past`/`Future`, and not — and not safely — covered by
     `Scrub`;
   - the superseded branch (S29): the export also captures `History<TModel>.Future`; before revision
     4 the realistic window was the brief interval before the next dispatch, and since option (b) it
     is the remainder of the session in a session with no further long undo run.
3. **Open Risks** — the un-retrofitted `SerializeMessageArgs` leak (SEC-7).

Neither S29 wording needs a new test: SEC-3(b)'s existing test already exercises `Past` and `Future`,
and a test proves reachability, not duration.

4. **A separate bullet — not a row-2 bullet — for S33**, per § 5.3.

### 5.3 S33 — the sealed `UrlChanged`'s full URL is a *rendered-chrome* exposure, and no adopter can redact it

Added by the Critic's confirmation pass and answered by the room in the **same** fourth spawn S29
opened, so no new spawn exists: `room-security.md` § *Follow-up — 2026-09-07 (III)*, subsection
**S33**. Step 16 applies the room's wording, not a paraphrase of it, exactly as it applies the S29
enumeration in § 5.1 from the same section.

`[R4-nav]`'s seal makes the **whole `Url` — path, query string and fragment, verbatim** — the stored
`EdgeState` cause of every post-record navigation. `UrlChanged` is
`public sealed record UrlChanged(Url Url) : Message` — **sealed and framework-owned** — so it cannot
be marked `SensitiveCause` and `redact` does not rewrite it. `Backward(h)`/`Forward(h)` return that
cause on **every render** the edge stays refused, and step 10's chrome renders it. A password-reset
or magic-link token riding in the query string is therefore shown in plain text on the ordinary
running screen.

Three things about this must survive into step 16, in the room's own framing:

- **It is not the retention exposure the rest of § 5 describes.** It is not waiting in `Past`/`Future`
  for a DEBUG export or in an anchor's bracket-duration hold; it is **displayed**. That is why it
  gets its own bullet rather than a third bullet on the DEBUG row, and why row 2's Medium/⚠️ status
  does not govern it.
- **The disposition is accepted-and-documented**, by the user's decision — a stated limitation, not a
  framework-side rule in SEC-2. Record it as such, alongside the **B10 asymmetry**, which belongs on
  the same bullet because it is the same seam read from the other end: *an application-named
  navigation message **can** be marked `SensitiveCause` and gets no seal; `UrlChanged` gets the seal
  and cannot be marked.*
- **SEC-4 is unaffected** — telemetry tags remain `Cause.GetType().Name` only, and `Movement` is
  never tagged.

`→ tech-writer` (step 13) carries the adopter-facing half, also in the room's terms: **do not carry a
secret, session token, or password-reset/magic-link token in the query string or fragment of any URL
that reaches the framework's `UrlChanged` — undo/redo will surface it back to the screen.** A flow
that must carry such a token in the URL should dispatch its own navigation message so it can be
marked `SensitiveCause` — which is the same wiring choice obligations line 17 governs, seen from the
security side, and the two sentences must not be written as if they were unrelated.

No new test. A test proves reachability, and the value's reachability was already true before the
amendment — what changed is that it is now **certain** rather than incidental, and that its route is
a rendered surface.

---

## 6. Task list and dispatch order

Numbers are `04-realist-plan.md`'s step numbers, unchanged so that `05-critic.md`'s references still
resolve. `tech-writer` is included, as it is on every pass.

**Precondition — the spec lands first, alone, as PR 0.** Answered by the user 2026-09-07 and recorded
by the Realist as `[R4-spec-commit]` (§ 8 item 1 below; `04-realist-plan.md` ~`:1531-1564`).

`→ csharp-dev: PR 0 — commit Picea.Abies.Tests/History/UndoRedoSpec.cs +
Picea.Abies.Tests/SpecAttribute.cs as the approved, locked spec, and nothing else, before step 1.`

- `UndoRedoSpec.cs` cannot compile until the end of step 6, so the **same commit** adds
  `<Compile Remove="History\UndoRedoSpec.cs" />` to `Picea.Abies.Tests/Picea.Abies.Tests.csproj`.
  `SpecAttribute.cs` has no dependency on anything this pass builds, compiles from the moment it
  lands, and is **not** excluded.
- **Step 6 (PR 3) removes the exclusion**, as a stated, expected change — it is in step 6's Done-when
  and named by file and element in the plan's slip-signal row (`04-realist-plan.md` ~`:1508`). It is
  the **one** sanctioned csproj touch in this pass; any other remains a signal that the design has
  slipped, and nothing shelters behind this one.
- **PR 0 terminates at `reviewer-blind` → `reviewer-reconcile`** like every other PR. It is a seventh
  PR ahead of the seven, not an exemption from the cut.
- **No PR body asks a reviewer to disregard a check.** That is the reason for this shape rather than
  landing the spec as PR 3's first commit: the Lock's git-history check flags a spec file modified in
  the PR that brings it to passing, and under PR 0 the check never has to be waived, because the
  approval commit and the commits that green it are in different PRs. A waiver requested once is a
  waiver available always.

| Wave | Dispatch | Steps |
|---|---|---|
| **PR 0 — before wave 0, alone** | `→ csharp-dev` | The locked spec: `Picea.Abies.Tests/History/UndoRedoSpec.cs` + `Picea.Abies.Tests/SpecAttribute.cs` + the one-line `<Compile Remove>` in `Picea.Abies.Tests.csproj`, **and nothing else**. No plan step number — it precedes step 1. Terminates at `reviewer-blind` → `reviewer-reconcile` before wave 0 is dispatched. **= PR 0** (`[R4-spec-commit]`) |
| **0 — in one message, parallel** | `→ ux-expert` | **12** — the thirteen questions (q11(b) and q13 are the two that decide defaults). Starts first because its answers land before step 5 fixes the policy defaults. **⚠️ Corrected premise, and it must travel with this dispatch — `05-critic.md` S32.** q9 and q13 were written against the pre-amendment behaviour, and `04-realist-plan.md`'s *"`ux-expert` q9 confirms with the corrected description in front of it"* refers to the **S23** correction, not to this one. The premise `ux-expert` must be given is § 2.5's rule: **after the first recorded step, a browser back or forward press does not create an undoable step — it seals the history in both directions, and undo and redo both refuse, naming the navigation.** Two consequences for the questions as asked: q9 is no longer *"is a navigation a good undo stop?"* but *"is a refusal at a navigation the right thing to show, and what does it say?"*; and q13(b)'s wording problem is now live rather than hypothetical, because step 10's Done-when sentence *"blocked — the page changed while you were editing (ProfileLoaded)"* becomes *"…(UrlChanged)"* for a user who **changed the page on purpose** — a confusing thing to say, and exactly what q13(b) exists to settle. No new question and no second spawn: the questions stand, their premise changed |
| | `→ csharp-dev` | **1** `HistoryStack<T>`; **2** `History<TModel>` / `Step<TModel>` / `EdgeState` / `Movement` / `Origin`. Independent of each other. **= PR 1** |
| **1** | `→ csharp-dev` | **3** `MovementAvailability` + `Backward`/`Forward`; **4** `HistoryMessage` / `MovementRefused` / `HistoryEvent.Enveloped` / `SensitiveCause` / `HistoryRedacted`; **5** `HistoryPolicy` + `WholeModelHistoryPolicy`. All three depend on 2, none on each other. **= PR 2** |
| **1b — as soon as 4 lands** | `→ security-expert` | **16** — TB7 (§ 5.1 wording), the two rows and their bullets (§ 5.2), the Open Risk, both hardening-backlog entries. Needs the type names, not the implementation |
| **2** | `→ csharp-dev` | **6** `WithHistory` — the whole wrapper. Depends on 3, 4, 5; everything downstream depends on it. **The largest single PR and the one to keep clean. = PR 3.** The spec first compiles here and must be observed **red for the right reason** before anything is made green |
| **3** | `→ performance-engineer` (harness by `csharp-dev`) | **7** — re-sequenced **before** 8–11 so a bad number invalidates one step rather than four. **= PR 4** |
| **3b — as soon as 6 lands, alongside 7–11** | `→ tech-writer` | **13** — ADR-030, `docs/concepts/undo-redo.md`, `docs/guides/adding-undo.md`, API reference, **ADR-008 line 85 amendment**, CHANGELOG, **+ doc-sync verification** (ADR-024 heads table, ADR-025's unamended statement, `tech-stack.md`). **= PR 7 with 16** |
| **4 — parallel** | `→ csharp-dev` | **8** properties INV-1…INV-6 + L1–L6 + SEC-3 (**= PR 5**, split 5a/5b if needed); **9** INV-7 at runtime level; **10** composition + chrome; **11** telemetry (**9, 10, 11 = PR 6**, split 6a/6b pre-authorised) |
| **5** | `→ csharp-dev` | **14** opt-in coalescing — separable, **may be cut**, its own PR if it survives; **15** report-only `Reset` probe — anywhere, blocks nothing, commits nothing |
| **Terminal, per PR** | `→ reviewer-blind` → `→ reviewer-reconcile` | Non-negotiable. No path from "code changed" to "done" goes anywhere else |

**Specialists the orchestrator spawns, and what runs in parallel:** `ux-expert` (step 12) in parallel
with `csharp-dev` from the very start; `security-expert` (step 16) in parallel from the moment step 4
lands; `performance-engineer` (step 7) after step 6 and before steps 8–11; `tech-writer` (step 13) in
parallel with steps 7–11. The **fourth `security-expert` spawn the Critic asked for at S29 has already
run** — its answer is `room-security.md` § *Follow-up — 2026-09-07 (III)* — so step 16 applies it
rather than re-asking.

### 6.1 Mitigations mapped to owners — the fourth pass, then the confirmation pass

Two tables. The first is the fourth pass's five 🟠 and five 🟡, unchanged since close-out. The second
is the confirmation pass's thirteen, tagged `[R4-nav]`. **Ids do not collide across them:** the
fourth pass's minors are lettered 🟡 a–e, the confirmation pass's are numbered 🟡 1–7, and the
significant findings continue the single S-sequence.

Three of the five 🟠 landed in `06-spec.md` and need nothing further. The rest are carried here
because **`04-realist-plan.md` revision 4 was not re-revised**, so its text still contains what the
Critic found.

| Id | Where it must land now |
|---|---|
| **S25(a)** | `06-spec.md` **line 15** is authoritative over `04-realist-plan.md:195-199`. A message arriving during a bracket does not end the run: it is applied to `Present` and may seal the history **or record a new undo stop whose model is a transit state and supersede the forward branch, mid-movement** — all three typed. The plan's closing sentence *"the world does not get to redefine what the user's gesture was"* is **backwards** under revision 4's mechanism and must not be reproduced. → `tech-writer` (guide, chrome-contract section, beside the liveness obligation) |
| **S25(b)** | `06-spec.md` **line 4** is authoritative over `04-realist-plan.md:408-410`, which is **false**: `SubscriptionsDemo`'s `FastTick` is silent and fires four times a second, so at `Depth` 100 the user's own steps are fully evicted in **twenty-five seconds**. The third reach bound — `Depth` ÷ tick rate, in wall-clock seconds — goes in step 5(v)'s file docs, ADR-030 and the guide with the same weight as the reach statement. → `csharp-dev` (step 5 docs), `tech-writer` (step 13) |
| **S26** | Landed as `06-spec.md` **line 14** |
| **S27** | The **generator alphabet partition**: the autonomously-delivering source is in 8(k)'s alphabet **and in 8(k)'s alone**; 8(b) (INV-1) and INV-3's property generate user actions only — because under this design a delivery *is* an action, so a property quantified over "the most recent action" or over an immediate round trip would otherwise be falsified by correct behaviour. `06-spec.md` carries the partition for its own generator; **step 8's acceptance criterion must carry it too, in writing.** → `csharp-dev` |
| **S28** | 8(k)'s ordered interleave against a fire-and-forget delivery (`Runtime.cs:288-291`) uses `new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)` completed from inside the program under test and awaited with `WaitAsync(timeout)` — the precedent is `RuntimeIsolationAndSubscriptionFaultTests.cs:38,53`. **No sleeps** (`ca2519d`). → `csharp-dev` |
| **S29** | § 5.1 above. → `security-expert` (step 16); plus `04-realist-plan.md` § *Memory* and Cleanness 9 read as *retained until trimmed, which may be never in a session that does not undo again*; plus `performance-engineer` measures the retained graph at the **`2 × Depth` steady state**, not at `Depth` |
| 🟡 a | The file-table row for `MovementAvailability.cs` (`04-realist-plan.md:1262`) is **stale** — it omits `BlockedBySupersedingAction`, which the Architecture Sketch and step 3 both carry. `csharp-dev` reads the file table to know what a file contains; the sketch wins |
| 🟡 b | State in step 1 or step 6 **why the `Future` trim is safe**: the crossable prefix above the topmost superseded edge can never exceed `Depth`, so trimming `Future` at its far end can only discard steps already unreachable behind a sealed edge |
| 🟡 c | Step 3's criterion must read *the **most recently** sealing or superseding message* — `sealTops` and `record`'s `ReplaceTop` overwrite each other's cause, last writer wins. Both are refusals, so INV-6's agreement is unaffected |
| 🟡 d | One sentence in step 4's docs: *"new action" includes a message a subscription delivered* |
| 🟡 e | One line in the guide beside the reach statement: a rejected decision now supersedes the redo branch too, so a user who undoes twice and then fails form validation sees *"redo unavailable — superseded by `CommandRejected`"*. Right behaviour, surprising sentence |

#### `[R4-nav]` — the confirmation pass's thirteen, all accepted by the user 2026-09-07

The rule itself is § 2.5. These are its consequences. **All thirteen are sentences, numbers or a
brief's premise — none is mechanism**, which is what let the Critic confirm without a revision 5.
Most have already been applied by the Realist, `spec-author` and `security-expert`; the column says
what is still owed and by whom, so that nothing reads as outstanding when it is not.

| Id | Where it lands, and who owes it |
|---|---|
| **B10** | The seal keys on `Picea.Abies.UrlChanged`, and on the WebAssembly head the message type is the **application's** choice (`Navigation.cs:17-29`), so an adopter passing `url => new MyOwnNavigationMessage(url)` gets no seal **with every property still green**. User took **mitigation (1) — scope the claim, doc-only.** Landed as **`06-spec.md` obligations line 17**, in line 2's register. `00-scope.md`'s INV-2 stays unconditional by that same decision (§ 2.5). Still owed: **mitigation (2)** — `README.md:183-190` is this repository's own counter-example → `tech-writer` (step 13), and note there that the README example is not itself a `WithHistory` adopter. Mitigation (3), an analyzer rule, is a **fast-follow candidate and not in this pass** (§ 9) |
| **B11** | The *"navigation classifies like any other message"* clause, in the two places marked *implement from this*. Corrected in **standing decision 7 above** and in `04-realist-plan.md` § *Origin re-basing*. Discharged; nothing further owed. `csharp-dev` implements from the corrected clause and from § 2.5 |
| **S30** | *"The world is a command's feedback and nothing else"* is now false and is stated as adopter-facing fact in four places. One qualifying clause in each — *"…with one exception the same table carries: an incoming `UrlChanged` once `Origin` is `Established` (line 16)"*. **Second half is content, not wording:** the **reach** statement now has a **third bound** — *the last incoming navigation*, which in a routed application is usually the binding one (in Conduit, undo reaches back to the current page and no further). It goes wherever the other two bounds go, at the same weight → `csharp-dev` (step 5(iv) file docs), `tech-writer` (step 13: ADR-030, `docs/concepts/undo-redo.md`, `docs/guides/adding-undo.md`) |
| **S31** | The obligations line is **16**, in both tables. `06-spec.md`'s table is the **union** and is the one the lock and the review read; the plan's table renumbered to match and gained rows 14 and 15. Consequence for review: **`reviewer-reconcile` verifies against `06-spec.md`'s numbering, not the plan's** — before the fix, "obligation 14" named two different behaviours in the two files, and the spec's 14 (the superseded branch, S26) had no plan-side row at all |
| **S32** | The corrected premise for `ux-expert` q9/q13 — landed in the **wave-0 dispatch cell** of § 6 above, because `ux-expert` is dispatched **first** and a brief that arrives after the answers is worth nothing. Also owed: the navigation bullet in step 13's tech-writer brief → `tech-writer`; the rendered navigation case in step 10's Done-when → `csharp-dev` |
| **S33** | § 5.3 above → `security-expert` (step 16, its own threat-model bullet), `tech-writer` (step 13, the token-in-URL sentence). Answered by the room inside the spawn S29 already opened — **do not open a fifth** |
| 🟡 1 | *"One property per id"* read as **exactly** one where the rule is written, and INV-2 now carries two. `00-scope.md` amended to **"at least one property per id"**; `04-realist-plan.md` step 8(a) reads the same. The validator tolerates it (`validate-phase-artifact.sh:197-202` reports only **missing** ids — no count, no cap), so this is prose catching up with mechanism. Consequence: a reviewer verifying step 8(a) as a claim does not have to adjudicate two properties on one id |
| 🟡 2 | `04-realist-plan.md` § *The scope clause*'s *"Nothing in revision 4 asks for a third amendment"* — qualified in place: **a third has since landed**. Done |
| 🟡 3 | The amendment lengthened INV-2 and **moved every line below it**, staling four citations into `00-scope.md` across the plan and the spec. Now cited **by invariant id and quoted clause** instead. **Standing instruction for implementation and review: cite `00-scope.md` by id, never by line.** The scope is demonstrably a file that moves, and the ids exist to make it citable |
| 🟡 4 | Step 8(n)'s contingency (*"if impracticable at the generator level it moves to step 10"*) is **no longer available** — `06-spec.md` locks that assertion. After the approval commit a move is a `// SPEC CONFLICT:` hand-back and a re-approval, **not a relocation** → `csharp-dev` reads it that way at step 8 |
| 🟡 5 | A **silent, model-identical** `UrlChanged` bypasses the seal, because `apply`'s true-no-op branch precedes the rule. **Not a hole:** such a program has no location in its model to restore, which is the case INV-2's *"on any head where the application has one"* is written against. Stated beside the rule in the plan so it is not read as an oversight — and **nobody moves the rule above the no-op branch to "fix" it**, which would seal a history on a message that changed nothing |
| 🟡 6 | The seal is a **third writer** of a last-writer-wins cause (with `sealTops` and `record`'s `ReplaceTop`): navigate, then act, and redo still refuses but under the later name. Reachability is unchanged — nothing unseals. Covered by step 3's *"most recently"* wording, **provided that wording is written to include the navigation seal** → `csharp-dev` (step 3), and it is the same fix as 🟡 c above |
| 🟡 7 | The pass's most user-recognisable behaviour had no acceptance-layer example. `spec-author` took the option: **A10** — *type, press Back, press undo — it refuses and names the navigation*. In `06-spec.md`; nothing owed |

---

## 7. Step 15, partially answered here

Step 15 asks whether `Picea.AutomatonRuntime<…>.Reset(TState)` is public, and says the answer is
recorded in this file. I have no `Bash` and could not compile the scratch probe, so this is evidence,
not the answer:

`~/.nuget/packages/picea/1.0.0/lib/net10.0/Picea.xml` contains
`<member name="M:Picea.AutomatonRuntime`5.Reset(`1)">` — *"Resets the current runtime state to the
provided value"* — and `<member name="M:Picea.DecidingRuntime`7.Reset(`1)">`. A documentation XML
normally carries only externally-visible members, so this is **strong evidence the member is public**,
and it points the same way as Track B's finding that `AutomatonRuntime.State` is not settable: the
*property* is not, but a `Reset` method may well be.

**Step 15 still runs**, because a compile against the package is proof and an XML entry is not.
Nothing in this pass changes on the answer. What changes is the economics of the deferred H2 pass and
of any future design that wants to set kernel state without `TrySetCoreState`'s reflection — which is
why the question is worth the ten minutes.

---

## 8. What I could not reconcile

Four items. **Item 1 is now answered** — kept, with the answer, because the reasoning is what a later
reader needs. The rest are recorded so that a later reader does not mistake them for oversights.

1. **✅ ANSWERED 2026-09-07 by the user; recorded as `[R4-spec-commit]` in
   `04-realist-plan.md` ~`:1531-1564`. The spec lands alone, before step 1, as PR 0.**

   *The problem as I left it:* `04-realist-plan.md:1304` says the spec is *"authored and approved
   before step 1"*; `06-spec.md`'s Lock reads that as *"the spec lands before plan step 1"* and adds
   that `reviewer-reconcile` flags a spec file modified in the same PR that brings it to passing. But
   `UndoRedoSpec.cs` cannot compile until the end of step 6, and step 6 is PR 3 — so a spec commit
   before PR 1 puts a non-compiling test file on `main` for two PRs, and a spec commit inside PR 3 is
   exactly the shape the same-PR check flags.

   *My reading, which the user overruled:* land the file as the first commit of PR 3 and tell
   `reviewer-reconcile` in the PR body that the same-PR flag is expected.

   *The decision:* the Lock's sentence is taken **literally**. The spec and `SpecAttribute.cs` are
   committed alone, before step 1, as **PR 0**, through the review pair, with
   `<Compile Remove="History\UndoRedoSpec.cs" />` in `Picea.Abies.Tests.csproj` — removed again in
   step 6 as a stated expected change, and named by file and element in the slip-signal row so no
   other csproj touch can shelter behind it. **The reason my reading was wrong is worth keeping:
   nothing in a PR body tells a reviewer to ignore a check.** A waiver requested once is a waiver
   available always, and the check's whole value is that it is not negotiable from inside the change
   it is checking. I had priced the csproj touch as the larger cost; it is the smaller one — one line
   of one project file, reviewed in PR 0 and removed on schedule, against a precedent for waiving a
   review check on the pass's most load-bearing file. Execution detail is in § 6's precondition.

2. **`04-realist-plan.md` revision 4 still contains the two sentences S25 found false, and it will
   not be revised again.** The Critic explicitly scoped S25 as sentences that land in `06-spec.md`
   and downstream documents rather than as a revision 5, and the user accepted that. The consequence
   is that the plan — the artifact `csharp-dev` reads most — contains two statements the spec
   contradicts. § 6.1 names both and states which artifact wins on each. This is a documented
   precedence rule, not a resolution; **if an implementer reads only the plan, they will implement
   the wrong narrative into step 5's file docs.** That risk is why S25 appears in the dispatch table
   and not only here.

3. **`room-security.md`'s own text uses `ISensitiveCause` and a `Type`-valued `HistoryRedacted`
   throughout its first section.** Both were superseded — no `I` prefix (gate-4 decision 5) and
   `HistoryRedacted(string OriginalTypeName)`, which the room itself later confirmed. The room's
   first section was never rewritten. `04-realist-plan.md` § *Security* is authoritative over
   `room-security.md`'s first section wherever they differ; the room's two 2026-09-07 follow-ups are
   authoritative over both.

4. **`00-scope.md`'s `INV-5` and `INV-7` collide by name with the verdict-cache invariants in
   `.claude/hooks/`.** The scope handles this by qualifying the id on its own line so a grep hit
   disambiguates without context, and the artifact validator reads `INV-<n>` and nothing else, so
   renaming was not available. It works, and it is fragile: it depends on a reader noticing the
   qualifier. Recorded as a live hazard for the next pass that declares invariants in this
   repository, and as an input to whoever eventually revisits the validator's id grammar.

**Not unreconciled, recorded to say so:** the ADR-008 line-85 reachability was a deliberate,
user-recorded experiment, and convergence classified the result — Track A **reasoned past** the line,
on five pieces of evidence, contradicting its central adjective, declining its vocabulary, and
arriving through three recorded rejections. The deny-list gap that made the file reachable is
registered as **R-21** in `.claude/enforcement/refutations.md`, owned by `devops`, closing after this
pass.

---

## 9. Follow-on candidates — named, not scheduled

None of these is in scope. Each is named so that the next pass inherits a question rather than
rediscovering it.

| Candidate | Why it is a pass and not a task |
|---|---|
| **DOM-owned state** — caret, focus, scroll, `<details>`, uncontrolled input values | Explicitly outside INV-3 and outside this pass. `ux-expert` **question 4** decides whether a minimum per-entry bookmark is mandatory for v1; if it is, this reopens as a scope amendment, not as a step. ProseMirror stores selection bookmarks *inside* history entries, which is the shape to start from |
| **A3(b) — crossable-iff-the-application-names-an-inverse** | Excluded at gate 3 and again at revision 4. It owes **its own invariant**: INV-2 is only sufficient here *because* nothing crosses, and it lapses the moment anything is allowed to. Track A's `model ⊗ world` factorisation is that invariant's starting point |
| **The serialization-boundary scrub for the held anchor** | The only mechanism that would properly close `Movement.Held(Anchor)`'s DEBUG-snapshot exposure. Declined mid-loop by both the Critic and the security room as new mechanism in a closed loop. Logged in SEC-7's register; it is a small pass, not a backlog line, because it touches a serialization seam |
| **The `Reset` accessibility question** (step 15's answer) | Changes nothing here; changes the economics of the deferred **H2** pass — the debugger reading the release history instead of maintaining its own parallel timeline, at which point the debug path gets *smaller* |
| **An analyzer rule for the navigation converter** (`05-critic.md` B10, mitigation 3) | The seal fires only for `Picea.Abies.UrlChanged`, and on the WebAssembly head the message type is the adopter's choice. Mitigation (1) — state the obligation, `06-spec.md` line 17 — is what shipped, and an obligation a compiler could check is better than one a guide asserts. Out of this pass because it is **new mechanism**, and one shape of it reverses a gate-4 deletion. A rule flagging `Navigation.UrlChanges(url => new X(url))` where `X` is not `UrlChanged`, in a program composed under `WithHistory`, is a small analyzer pass — `Picea.Abies.Analyzers` already exists |
| **The `docs/adr/` deny-list gap** | Already registered as **R-21**, owned by `devops`, closing after this pass. `docs/adr/**` is reachable by `dreamer-first-principles` even though the squad-flow specification lists ADRs among the paths Track A may not read |
| **Deferral / hold window** (Gmail's Undo Send) | Track B's escape for genuinely irrevocable actions and the correct answer for that class. Out of this pass because it means the framework holding commands back — a fifth runtime seam, and therefore an architecture decision |
| **`NavigationCommand.Replace` on an undo that crosses a navigation** | Track A's remedy for the two-cursor hazard both tracks found. Out because under this design a navigation is a commitment and undo does not cross it. It returns if A3(b) ever does |
| **The inherited OTEL registration gap** | `"Picea.Abies.Runtime"` and `"Picea.Abies.Subscriptions"` are registered nowhere; every registration in the repo is the exact string `"Picea.Abies"`, and `AddSource` matches exactly, not by prefix. Pre-existing, not introduced here. `devops` follow-up |
| **A window- or document-level key-handling seam, and a native key-up/pointer-up event** | Both are what INV-7's non-trivial content would need in order to apply to the chrome the guide actually shows. `ux-expert` q11(b) decides whether per-press reconciliation is acceptable; if it is not, the answer is a scrubber or an architecture decision, and both are separate passes |

---

## 10. Terminal node

`reviewer-reconcile` is the terminal node for every one of the seven PRs, and `reviewer-blind` must
have written `08-review-blind.md` before it starts. No specialist and no orchestrator declares this
work complete. The step-8 review brief's `Generators.cs` / `DocumentComparer` wording (§ 4.1) is part
of that contract.

I remain available during implementation — re-spawn me with questions rather than deciding a design
question inside a step.
