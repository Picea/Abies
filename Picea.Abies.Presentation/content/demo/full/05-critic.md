# 🔍 Critic — undo/redo (`WithHistory`) — **fourth pass**

Read this revision: `00-scope.md` **as amended and landed**, `04-realist-plan.md` **revision 4** in
full, `room-security.md` in full including both 2026-09-07 follow-ups, and my own third-pass
assessment (the artifact this file overwrites; its findings are preserved below in § *Verification
of the nine items* and § *Disposition*). Re-opened in the code this pass, because revision 4's two
mechanism changes both turn on runtime facts:
`Picea.Abies/Runtime.cs:282-292` (the subscription delegate) and `:441-513` (the whole `Dispatch`
body, including the `Err`-channel branch at `:482-489` that `[R4-1]` routes through), and
`Picea.Abies.SubscriptionsDemo/Program.cs:99-198` (the tick's cadence *and* its command).

**Finding ids continue.** B1–B9 and S1–S24 keep their meanings. New significant findings start at
**S25**. There are no new blockers.

**Scope of this pass, per the user's instruction.** Revision 4 was confined to the nine items in my
third pass's § *What revision 4 must contain*, and everything the third pass marked closed was
carried through verbatim. I have therefore verified the nine landings item by item, re-derived the
invariant-to-property chain under the two new mechanisms (`SupersededByNewAction` and the enveloped
`Err` channel), and stress-tested **only** what revision 4 changed. I have not re-litigated anything
the user settled at gates 2 through 5, and I have not reopened either of the two judgement calls the
user upheld.

---

## Verdict

**APPROVED WITH MITIGATIONS.**

Revision 4 carries all nine items. Two of them landed as the user explicitly varied them rather than
as I worded them — item 2's narrative (*"the world" is command feedback **and nothing else***,
because decision 2 removes rejections from it) and item 6 (SEC-3(b)'s reachability set **not**
widened, per the security room's refusal) — and both variations are better than what I asked for. In
both cases the Realist flagged the divergence in its own § *Two places where the composition… is
narrower*, rather than absorbing it, and put it to the user, who confirmed it. That is the behaviour
this loop has been trying to produce for four passes.

**The new mechanism holds.** I traced `record` → supersede → undo → redo → supersede through the
edge algebra rather than accepting the plan's account of it, and the two claims decision (b) rests
on are true:

- **No edge ever returns to `Crossable`.** `sealTops` and `record`'s `ReplaceTop` only ever seal;
  `StepBack`/`StepForward` push *fresh* `Crossable` steps rather than mutating existing ones.
  Therefore a superseded `Future` entry can never be popped, never becomes `Present`, and Track A's
  forcing argument — a retained branch makes redo a relation, which has no inverse — genuinely does
  not reach it. The gate-2 change from *discard* to *refuse across* is sound, not merely plausible.
- **`Past + Future` is conserved by movement and incremented by `record`**, so without the new
  `Future` trim the retained set would grow without bound across a long session; with it the ceiling
  is exactly `2 × Depth`. The trim is necessary and the arithmetic behind the "200 models" figure the
  user approved is right.

**B8's fix is as cheap as claimed, and I checked the boundary it crosses.** `Runtime.cs:482-489`
dispatches whatever `Message` sits in the `Err` channel and then returns `Ok(Unit.Value)` unless that
dispatch itself faults — the payload is *not* returned to the caller — so routing
`Enveloped(m,[e])` instead of `e` changes nothing observable at the dispatch boundary. The wrapper's
own `Err(MovementRefused …)` staying bare is correct, because `apply` branches on it *before* the
envelope test.

What is left is five 🟠 and five 🟡. **None of them is mechanism, none of them changes a user
decision, and none of them needs a revision 5.** Three land in `06-spec.md`, two in step 8's
acceptance criterion, one in step 16 with a narrow fourth `security-expert` question. They are
carried by `spec-author` and by the named steps, from this file, which is the artifact those phases
read.

---

## Verification of the nine items

Checked against revision 4's text **and**, where the item made a claim about the runtime, against the
code. Not against the plan's own disposition table.

| # | Item | Landed? |
|---|---|---|
| 1 | **B8** — `Err(e) → Err(Enveloped(m,[e]))`, or a third `EdgeState` with the reason written down; plus step 8's property | **Yes, as the user decided.** `[R4-1]` at `:614-633`; wrapper `Decide` at `:986`; step 6 at `:1356-1358`; step 8(j) at `:1409-1414` **with its falsifier named** ("revert the line and the property must fail with `BlockedByWorld`"). The `SealedByDecision` alternative is rejected in writing at `:627-629`. Verified against `Runtime.cs:482-489`. |
| 2 | **B9** — § *The default policy* rewritten; the `SubscriptionsDemo` paragraph replaced; the choice carried into step 5's docs, `06-spec.md`, ADR-030, the guide and ux q13 | **Yes, with one false sentence surviving in the rewrite and one uncorrected elsewhere — S25.** `[R4-2]` at `:432-496`; the one-sentence statement at `:523-527`; step 5(iv)(v) at `:1350-1354`; spec obligations 3 and 4; step 13 at `:1482-1486`; ux q13 at `:1622-1630`. |
| 3 | **B9** — an autonomous-source property | **Yes, and it is the best-specified property in the plan** — 8(k) at `:1415-1423` names the shape rather than hoping the generator finds it, asserts on what the user can observe (`BlockedBySupersedingAction`, **not** `Nothing`), and carries a mutation check. Two gaps in *how* it runs — S27, S28. |
| 4 | **S18** — batch error semantics as a spec line + step 6 Done-when | **Yes, exactly as the user phrased it.** `[R4-4]` at `:635-654`; spec line 7; step 6 Done-when at `:1370-1373` asserts it **against the unwrapped program's opposite behaviour**, which is what makes it a recorded difference rather than a passing test. |
| 5 | **S19** — three sentences + the trap becomes a red test | **Yes.** `[R4-5]` at `:766-816`; step 5 docs (ii) at `:1344-1347`; step 5 Done-when at `:1340-1341`; SEC-5's negative form at `:741`; SEC-3(b)'s joint-lawfulness precondition at `:835-840`; `Scrub_overridden_alone_violates_L5` filed at 8(l) as a **lens-law** test. Matches the room's second follow-up (`room-security.md:486-553`) and supersedes its first answer *out loud* (`:1752`). |
| 6 | **S20** — clause (b) reworded to name the anchor, anchor's treatment settled by `security-expert` | **Varied, by the user's explicit decision, and correctly.** The room **declines** the rewording with a reason I accept (`room-security.md:613-620`): a clause discharged by `Scrub` cannot name a site `Scrub` must not reach. Clause (b) is unchanged; the anchor gets TB7, the DEBUG-row bullet, `Anchor_never_reaches_a_release_path_surface` at 8(m), one guide sentence and a fast-follow. `[R4-6]` at `:863-927`. **My wording was the mistake the finding was about; the room caught it.** |
| 7 | **S21** — step 9 assertion (4) rewritten + the spec line | **Yes.** `[R4-7]` at `:1442-1449`, rewritten to *while a hold is open the reported key set is constant and equal to the anchor's* — stronger and falsifiable, as asked. Spec line 9 carries the suspended-subscription-contract statement. |
| 8 | **S22 / S23 / S24** — three statements of fact | **All three.** S22 `[R4-8a]` at `:264-288` + spec line 8 + ux q11(b). S23 `[R4-8b]` at `:1104-1125` — behaviour kept, description corrected, **and the reason given is better than the one I offered**: the one-line alternative would make pre-record navigations recorded stops that restore a model without its URL. S24 `[R4-8c]` at `:201-246` — chrome contract gains `blur`/`pointercancel`, `Clear` named as the one in-band rescue, step 10 asserts a mid-bracket blur settles the run. |
| 9 | 🟡 1–6 | **All six taken.** `Available(int Depth)` with the O(Depth)-per-render alternative rejected on cost; the two-chrome alias in the guide; assertion 2's shape seeded explicitly; SEC-5 names `HistoryEvent.Enveloped`; PR 6a/6b pre-authorised; undo-out-of-terminal as a spec line. |

**Nothing outside the nine items was added.** I checked for scope creep and found none: no new type beyond `SupersededByNewAction` / `BlockedBySupersedingAction`, no new step, no new dependency, no `Runtime.cs` line.

---

## 🟠 Significant

### S25 — Two sentences of the pre-B9 narrative survive, and both are false under the mechanism revision 4 now has. One of them is inside the paragraph that argues *for* the projection requirement.

Item 2 was *"§ The default policy rewritten to what the mechanism does"*. The rewrite at `:505-562`
is correct. Two sentences elsewhere in the artifact were not brought with it.

**(a) The in-bracket sentence, `:195-199`.** *"A message that arrives during a hold is applied to
`Present` and classified normally (it may `seal` the history, which is the correct and typed
consequence) […] The world does not get to redefine what the user's gesture was."*

Under revision 4 a subscription-delivered message arriving mid-bracket does not seal. It **records** —
pushing an undo stop whose `Model` is a state the user is passing through — and **supersedes** the
forward branch the held key is walking. And it is no longer *"the world"* at all, by decision 1. So
the parenthesis names the wrong outcome and the closing sentence is now backwards: under this
mechanism the timer does not redefine the gesture, it *joins* it. This is the third of the three
consequences I scoped under B9 ("Inside a bracket"), and it is the only one the rewrite did not
reach.

**Mitigation.** Replace the parenthesis with what happens: *it may seal the history, or record a new
undo stop whose model is a transit state and supersede the forward branch, mid-movement — all three
typed.* One sentence, in § *Movement*, and it belongs in the guide's chrome-contract section beside
the liveness obligation, because a chrome author who brackets a held key over a timer-driven model
needs to know the run is being written to underneath them.

**(b) The `SubscriptionsDemo` sentence, `:408-410`.** *"The `SubscriptionsDemo` history is still not
ticks-only after 25 seconds, because a tick that is `pass`ed under a projection pushes nothing and a
tick that `record`s pushes one step like any other action."*

Under the default there is no projection, so no tick is `pass`ed. I checked the demo rather than
reasoning about it: `Program.cs:110` declares `Every("fast-tick", 250 ms)`, and `:146-154` shows
`Message.FastTick` moves the model and returns **`Commands.None`** — silent, so each tick `record`s a
**`Crossable`** step. Four steps a second against `Depth` 100 means the user's own steps are fully
evicted from `Past` in **25 seconds**. The history *is* ticks-only after 25 seconds. This is
alternative (A)'s tombstone objection arriving at (C) by a different route — softened, because the
tick steps are crossable rather than dead, but not absent.

That matters beyond the sentence, because it is a **third reach bound that appears nowhere in the
artifact**. Reach is bounded by (i) the last command feedback (S10, stated in five places), (ii)
`Depth`, and (iii) — with a model-mutating subscription under the default policy — `Depth ÷ tick
rate`, *in wall-clock seconds*. (iii) is the strongest argument the plan has for the
projection-is-required rule, and it is missing from the paragraph that argues for it. Spec line 4
currently says redo "will not function"; the honest form is that **undo's reach also expires on a
timer**.

**Mitigation.** Strike the sentence, and add to spec line 4 / step 5(v) / the guide: *under the
whole-model policy an application with a model-mutating subscription loses not only redo but undo
reach, at `Depth` ÷ the subscription's rate — in `SubscriptionsDemo`'s shape, twenty-five seconds.*
Same weight as the reach statement, in the same places.

### S26 — § *Spec obligations* omits the pass's headline new behaviour. `spec-author` reads that table.

The table at `:1650-1665` exists because *"four of the nine items say 'and it goes in `06-spec.md`'
and `spec-author` reads one artifact"*. It carries thirteen lines. **None of them states what
decision (b) does.** Line 3 says what "the world" is; line 4 says redo will not function under a
model-mutating subscription. Neither says that acting after an undo *preserves* the forward branch,
seals its first edge, and refuses redo **by name, in advance** — which is the answer to
`00-scope.md` open question 3, the change to a gate-2 rule, and the whole content of the user's
decision 1.

It is not absent from the plan — § *The third edge state*, step 3, step 8(k) and step 10 all carry
it. It is absent from the one collected list that the next phase is told to read. Given that
`00-scope.md`'s *Done means* requires all five open questions answered, an `06-spec.md` that does
not carry question 3's answer is the exact failure the table was built to prevent.

**Mitigation.** One line, worded for lifting:

> **Acting after an undo does not discard the forward branch.** The branch is preserved and its
> first edge sealed with `SupersededByNewAction(cause)`; `Forward(h)` returns
> `BlockedBySupersedingAction(cause)` before the press, naming the message that superseded it. The
> branch is never crossed, and `Future` is trimmed to `Depth` at its far end.

### S27 — 8(k) puts an autonomous source into *step 8's* generator alphabet. If that alphabet is shared, it falsifies 8(b) (INV-1) and INV-3's property — by producing correct behaviour.

Step 8's header declares one alphabet: *"generated `(action | undo | redo | hold | settle)*`
sequences"* (`:1388`). 8(k) then says *"**the** generator's alphabet gains a source that delivers
without being asked"* (`:1415-1416`). Read literally, the source is now in the alphabet every step-8
property draws from. Two properties then fail, and they fail because the design is working:

- **INV-1, 8(b).** The invariant is *"a single undo returns the application to the state immediately
  before the most recent action"*, quantified in `00-scope.md:128-133` over interleavings of **user
  actions**. Under this design a delivery **is** an action: it records. So a sequence
  `actionA → delivery → undo` lands before the *delivery*, not before `actionA`, and the property as
  stated is falsified by a mechanism that is behaving exactly as decision 1 specifies.
- **INV-3.** *"If undo is available in `s` and produces `s'`, then redo applied to `s'` produces a
  state indistinguishable from `s`."* A delivery between the undo and the redo supersedes the fresh
  `Crossable` step `StepBack` just pushed, so the redo **refuses**. Correct behaviour; falsified
  property.

The likely outcomes at implementation are both bad: either the source is excluded from the whole of
step 8 to make the properties green — which deletes 8(k) with it and reproduces, for the fifth time
in this pass, a property that cannot see its own trigger — or the two properties are weakened until
they pass. Neither is visible in a diff as a decision.

**Mitigation.** State the partition in step 8: *the autonomously-delivering source is in (k)'s
alphabet and in (k)'s alone; (b) and INV-3's property generate user actions only, and the reason is
that a delivery is an action under this design, so a property quantified over "the most recent
action" or over an immediate round trip must exclude one to mean anything.* Two sentences. If the
Realist would rather quantify than partition — INV-1 over delivery-free windows, INV-3 over
immediate round trips — that is equally good, but it has to be one or the other and it has to be
written down.

### S28 — 8(k) asserts an *ordered* interleave against a fire-and-forget delivery, in a plan that forbids sleeping.

8(k)'s shape is *"undo, then a delivery that moves the projection, then redo"*. The delivery goes
through `Runtime.cs:288-291`:

```csharp
private void DispatchFromSubscription(Message message) =>
    _ = _replay ? default : Dispatch(message);
```

The returned `ValueTask` is discarded. There is no completion signal and nothing for the driver to
await, so "then redo" has no ordering guarantee — the redo can be dispatched before the delivery has
transitioned, in which case `Forward(h)` returns `Available`, the redo succeeds, and the property
goes red for a reason that has nothing to do with the mechanism. The reflex fix is a sleep, which
step 9 forbids in capitals and `ca2519d` removed from this repository.

This is not hypothetical difficulty: it is the same fire-and-forget dispatch that produced **B6**.

**Mitigation.** Name the mechanism in 8(k)'s acceptance criterion, and the precedent is in the file
the plan already cites for 8(j):
`Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs:38` and `:53` use
`new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)` completed from inside
the program under test and awaited with `WaitAsync(timeout)`. `HistoryTestProgram`'s `Transition`
already records messages; it completes the source when it sees the delivered message, and the driver
awaits that before dispatching the redo. One sentence, no new mechanism.

### S29 — Decision (b) changed the retention *lifetime* of a walked-back model, not only its count — and `security-expert` has not seen it. The room's own bound sentence is now false for `Future`.

The user was told the count: the ceiling moves from 100 retained models to 200, bounded and measured
at step 7. That is right, and I re-derived it. What neither the plan nor the room states is that the
**duration** changed, and it changed by more than the count did.

- **Revision 3:** a divergence *cleared* `Future`. Every model the user had walked back through was
  released **at the next action**.
- **Revision 4:** the branch is preserved. A superseded `Step.Model` leaves `Future` only when
  `Future` next exceeds `Depth` and is trimmed at the far end — and `record` trims but never pushes
  to `Future`, so the only thing that can grow `Future` past `Depth` is the user performing another
  long run of undos. **In a session where they do not, the models are retained until the process
  ends.**

The room's second follow-up states the contrast it relied on in as many words: *"`Step.Model` is
retained for up to `TPolicy.Depth` (100) subsequent dispatches — the room's original finding"*
(`room-security.md:589`). That sentence is the baseline it used to argue the anchor's bracket-bound
exposure was acceptable *by comparison*. It is now false for the `Future` stack, and the room wrote
it before decision (b) existed.

What is retained is not abstract. Under `WholeModelHistoryPolicy`, **`Scrub` is provably the
identity** — that is S19, which this same revision writes down — so the canonical
Conduit-Settings-shaped fixture's live `Password` (`Conduit.App/Model.cs:94-96`) is exactly what sits
in a superseded `Future` step, for as long as the session lasts. SEC-3(b) covers it (its reachability
set is `Step.Model` in `Past` **and** `Future`), so nothing is *outside* the guarantee — this is a
data-minimisation regression inside a clause that still holds, which is precisely the kind of change
that passes a security regression test.

**Mitigation, small and split.** (i) § *Memory* and Cleanness 9 say *retained until trimmed, which
may be never in a session that does not undo again* rather than *"until they are trimmed"*. (ii) Step
16 tells `security-expert` that TB7's retention window for `Future` is now session-lifetime rather
than `Depth`-dispatch-bounded, so the boundary's wording and the DEBUG row reflect the design the
user approved rather than the one the room assessed. (iii) A fourth, narrow spawn — below.

---

## 🟡 Minor

- **The File-Level Changes row for `MovementAvailability.cs` is stale.** `:1262` lists
  *"`Available` / `Nothing` / `BlockedByEffect` / `BlockedByWorld`"*; the Architecture Sketch
  (`:945-947`) and step 3 (`:1327-1328`) both carry `BlockedBySupersedingAction`. `csharp-dev` reads
  the file table to know what a file contains. (My dossier's standing heuristic is that every pass
  has had one table row the step's acceptance criterion did not carry; this pass it is the reverse,
  which is the harmless direction.)
- **The `Future` trim's direction is unstated, and the argument that it is safe is not written
  down.** Step 1 says the stack evicts *"at the far end"*, which for `Future` is the
  furthest-forward step. That is correct, and it is *provably* safe: the crossable prefix above the
  topmost superseded edge can never exceed `Depth`, because a run of undos is bounded by `|Past|`
  and any `record` that refills `Past` also supersedes the top of that prefix. So trimming `Future`
  to `Depth` can only ever discard steps already unreachable behind a sealed edge. Worth one line in
  step 1 or step 6, because a reviewer cannot otherwise tell whether the trim is dropping reachable
  redo.
- **`sealTops` and `record`'s `ReplaceTop` overwrite each other's cause; last writer wins.** A
  command feedback that seals a `Future` top already marked `SupersededByNewAction(FastTick)`
  replaces the reason with `SealedByWorld(ProfileLoaded)`, and vice versa. Both are refusals, so
  INV-6's agreement is unaffected — but step 3's criterion says *"the carried cause is the message
  that sealed or superseded the edge"*, which reads as unique. Say *most recently*.
- **`SupersededByNewAction` names a classification the shipped default will usually apply to a
  timer.** The design's own position is that a delivery *is* an action, so the name is consistent —
  but it is the identifier a reviewer and an adopter read first. One sentence in step 4's docs
  ("*new action* includes a message a subscription delivered, per § *The default policy*") costs
  nothing; the *rendered* wording is already ux q13(b)'s.
- **A rejected decision now supersedes the redo branch too.** Follows correctly from decisions 1 and
  2 composed — a rejection is the application's own response, so it records, so it supersedes — but
  it means a user who undoes twice and then submits a form that fails validation sees *"redo
  unavailable — superseded by `CommandRejected`"*. Right behaviour, surprising sentence; one line in
  the guide beside the reach statement.

---

## 🟢 What revision 4 gets right

- **The (b) mechanism composes, and I derived that rather than accepting it.** No edge ever returns
  to `Crossable` — `sealTops` and `ReplaceTop` only seal, and `StepBack`/`StepForward` push fresh
  steps instead of mutating existing ones — so a superseded entry is unreachable for the life of the
  history and the gate-2 forcing argument genuinely does not apply to it. INV-2 and INV-3 survive
  intact. The plan's *"you can redo the undo you just did, but not past the point where the timer
  fired"* really does fall out of `Backward`/`Forward` reading one edge, rather than being arranged.
- **The `Future` trim was spotted, and it is genuinely necessary.** `Past + Future` is conserved by
  `StepBack`/`StepForward` and incremented by `record`; without the trim the retained set grows
  monotonically across a session. A revision that had added supersession and *not* the trim would
  have shipped an unbounded history. Nothing in my third pass asked for this — the Realist found it
  by working the consequence through.
- **`redact` on the superseding cause, and the `Scrub` site rule applied rather than restated.**
  `record`'s supersession stores `SupersededByNewAction(redact(origin))`, and the plan states — with
  the reasoning shown — that supersession is a `ReplaceTop` of an `Edge` and therefore **not** a
  `Step`-construction site, so no new `Scrub` site exists. Both are correct. This is the first time
  in the pass a new retention-adjacent mechanism has arrived with its security consequences already
  worked out against the rule instead of against a list.
- **Both narrowings were flagged, not absorbed, and both went to the user.** § *Two places where the
  composition of these four decisions is narrower than the wording of any one of them* is the single
  best paragraph of the four revisions: it names the place where my own item wording had gone stale
  (item 2, written before B8 was decided) and the place where a specialist overruled me (item 6), and
  it refuses to settle either silently.
- **S23's answer is better than the mitigation I proposed.** I offered two equal options; the Realist
  found a third fact — that setting `Origin := Established` in `rebase` would make pre-record
  navigations *recorded* stops whose crossing restores a model without its URL — and chose on it.
  *"Trading a wrong sentence for wrong behaviour is a bad trade"* is the right instinct and the right
  order of operations.
- **The security room was allowed to overrule the Critic, and the record says so.** `[R4-6]` does not
  quietly do what I asked; it quotes the room's refusal, gives the reason, and notes that my proposed
  rewording was itself an instance of the mistake S20 is about. A plan that had simply complied would
  have shipped a clause that is false the instant it is adopted.
- **The three self-corrections of revision 3's own false sentences** (`:1725-1732`) are the same
  discipline revision 3 earned credit for, applied to itself. Three of my seven 🟠 last pass existed
  only because someone wrote down a number or a claim that later moved; this is the habit that finds
  them.
- **8(j) and 8(k) both carry falsifiers.** *"Revert `Decide`'s `Err` branch and this property must
  fail with `BlockedByWorld`"*; *"observed to fail when `record` clears `Future` instead of
  superseding it"*. After four passes in which properties kept being planned in configurations that
  could not see their own trigger, both of the new ones state how they go red.

---

## Gates

- **Bug-fix regression test:** not applicable — new shipping behaviour. (SEC-3's two named tests plus
  `Anchor_never_reaches_a_release_path_surface` are security regression tests and are correctly
  scoped as such; `Scrub_overridden_alone_violates_L5` is correctly filed as a lens-law test and not
  as one of them.)
- **Principles compliance:** **clean.** The live deviations — the lens trading INV-2's
  by-construction status for L1–L6 plus an unstatable reachability assumption; the effect-interleaving
  *and* error-semantics difference between wrapped and unwrapped programs; and now retention of a
  branch that can never be crossed — are **all three explicitly user-approved** (gate 3 decision 1;
  gate 4 decision 4 with its second half disclosed at S18 and confirmed by decision 3; gate 5
  decision 2). All three are recorded as Cleanness compromises 1, 4 and 9 rather than as solutions.
  Sum types where flags would have done (`EdgeState` now four cases, `Movement`, `Origin`,
  `Direction`, `MovementAvailability` now five), errors as values, internal constructors, no
  `I`-prefix, no new dependency, namespace named for the domain, no clock on any shipping path. No
  unapproved deviation.
- **Terminates at reviewer:** **yes.** `:1305-1306` and the PR table at `:1552-1559` route all seven
  PRs through `reviewer-blind` → `reviewer-reconcile`; no step declares itself done by another route.
- **Invariant coverage (INV-1 … INV-7), re-derived under `SupersededByNewAction` and the enveloped
  `Err` channel:**

  | Id | Property | Assessment |
  |---|---|---|
  | INV-1 | step 8(b) | **Writable.** Quantified over histories where `Backward(h)` is `Available`, per the landed scope amendment. **Conditional on S27** — if the autonomous source is in this property's alphabet, a delivery is "the most recent action" and the property is falsified by correct behaviour. |
  | INV-2 | step 8 | **Writable, and now stronger than it was.** A superseded step is never crossed and no edge ever un-seals, so supersession adds no reachable state; under the whole-model lens INV-2 remains by construction, and under an application lens it is up to L1–L6 plus the named reachability assumption. |
  | INV-3 | step 8 | **Writable, and the derivation still holds** — `Restore(P, Restore(s.Model, P)) = Restore(P, P) = P` by L3 then L1, surviving `Scrub` via L5. Supersession happens only in `record` and never on an undo's forward push, so the round trip is untouched. **Conditional on S27** for the same reason as INV-1. |
  | INV-4 | step 8(c) | **Writable.** Unaffected by both new mechanisms; `Ok([])` still short-circuits at `Runtime.cs:465-468` before any transition, render or interpretation. |
  | INV-5 | step 8 + 8(j) + 8(k) | **Writable, and the hole is closed.** Four typed reasons against one no-op, in both directions. B9's silent path is gone: a destroyed redo is now `BlockedBySupersedingAction`, not `Nothing`, and 8(k) asserts exactly that distinction. B8's wrong *category* is gone: a rejection classifies as `record`/`pass`. **This is the first revision in which every decline in the design is typed.** |
  | INV-6 | step 3 | **Writable and clean.** Five cases both directions, one pure function that `Decide` and `Transition` both route on, so the advance answer and the outcome cannot disagree. `Backward` handling the superseded case defensively rather than asserting unreachability is the right call. |
  | INV-7 | step 9 | **Writable, in the shipped mode, with a real falsifier** — assertion (4) now describes what an open hold does, the mutation check stands, and 🟡 3's corpus shape is seeded explicitly instead of hoped for. Spec lines 8 and 9 carry the two honest limitations (satisfied-by-definition for an un-bracketed press; the subscription contract suspended while a hold is open). **S25(a)** touches the prose around it, not the property. |

  Seven ids, seven properties, across steps 8 and 9. No invariant is example-tested.

---

## Specialist Spawns Recommended

- **`security-expert` — a fourth spawn, one question, narrow.** **S29.** Decision (b) changed the
  retention *lifetime* of a walked-back `Step.Model` from "released at the next action" to "retained
  until `Future` next exceeds `Depth`, which may be never in a session". The room's second follow-up
  argued the anchor's bracket-bound exposure was acceptable *by comparison with* `Step.Model`'s
  `Depth`-dispatch bound (`room-security.md:589`) — a comparison that no longer holds, and the room
  wrote it before decision (b) existed. Under `WholeModelHistoryPolicy` `Scrub` is the identity, so
  what is retained is the room's own canonical fixture's live password. Does TB7's wording and the
  DEBUG row need to say session-lifetime rather than `Depth`-bounded for `Future`, and does the
  comparison the anchor's answer rests on need restating? **Documentation-and-wording only; I am not
  asking for mechanism, and neither should the answer.**
- **`performance-engineer` — as step 7 schedules, unchanged.** The two additions are already folded
  (the fold's `Command.Batch` allocation against its N−1 saved renders, stated *which way it came
  out*; the retained-graph measurement extended to a superseded `Future` branch). One line worth
  adding: measure the retained graph at the **`2 × Depth` steady state**, not at `Depth`, since S29
  makes that state persistent rather than transient.
- **`ux-expert` — as step 12 schedules.** q13 already asks the three right questions and q11(b) asks
  the real one behind S22. Nothing to add.

---

## 🛑 Pause — for the user

> *Here are the risks and proposed mitigations. Accept the mitigations, loop back, or proceed as
> planned?*

The plan is approved. Nothing below reopens a decision you have made, and none of it needs the
Realist — the five items are sentences that land in `06-spec.md`, in two step-8 acceptance criteria,
and in `security-expert`'s step-16 brief, all of which read this file. My recommendation is to
accept them and let the pass move to `spec-author`.

Three things worth hearing about, none of them a choice you have to make unless you want to:

1. **One number you were given is right; one you were not given has changed.** The retention ceiling
   is 200 models, as you were told, and I re-derived it. What was not said is that those models now
   live *longer*: before, acting after an undo threw the walked-back branch away immediately; now it
   is kept until the user does another long run of undos, which in many sessions never happens. The
   count is bounded; the time is the session. Under the default policy `Scrub` is the identity, so
   what is kept is the whole model — passwords included. Nothing escapes the guarantee the security
   room wrote, but the room assessed the anchor by comparing it against a shorter window than the one
   that now exists, so I am asking for one narrow re-spawn to get the wording right. **No mechanism
   change, and no change to the trade you accepted.**

2. **Two sentences in the plan still describe the old mechanism**, both about the timer case that
   caused the loop-back. One says a message arriving during a held run "may seal the history" — it
   now records and supersedes instead. The other says `SubscriptionsDemo`'s history is "still not
   ticks-only after 25 seconds"; I checked the demo, and it is: the tick is silent and fires four
   times a second, so at a depth of 100 the user's own undo steps are gone in twenty-five seconds.
   That last one matters more than a wording fix, because it means an application with a
   model-mutating subscription loses **undo reach on a wall clock**, not just redo — which is the
   strongest reason for the projection rule you approved, and it is currently missing from the
   paragraph that argues for it.

3. **The one place I would ask you to look twice** is that the collected spec list `spec-author`
   reads does not contain the behaviour you decided. It has thirteen lines and none of them says
   "acting after an undo preserves the forward branch and refuses redo by name". It is elsewhere in
   the plan four times over, so this is a plumbing gap rather than a design one — but that table
   exists precisely because the next phase reads one artifact, and the answer to the scope's third
   open question should not be the thing it leaves out.

---
---

# 🔍 Critic — confirmation pass, 2026-09-07 (post-close-out amendment `[R4-nav]`)

**This section is appended, not a fifth pass.** The fourth pass above stands unchanged and its
verdict is unaltered. What is confirmed here is the one post-close-out amendment: incoming
navigation is classified, `INV-2` amended in `00-scope.md`, `[R4-nav]` in `04-realist-plan.md`,
`INV_2_no_movement_lands_on_a_model_whose_url_is_not_the_one_the_browser_shows` in `06-spec.md`.

**Finding ids continue.** B1–B9 and S1–S29 keep their meanings; new blockers are **B10, B11** and
new significant findings start at **S30**.

**What I re-opened in the code**, because two of the questions asked turn on runtime facts rather
than on artifact text: `Picea.Abies/Navigation.cs:17-29,49-67` (how an incoming URL change actually
reaches a program), `Picea.Abies/Program.cs:87-139` (`Url`, `ToRelativeUri`, `UrlChanged`),
`Picea.Abies/Runtime.cs:367-369,431-434` (the navigation command executor and the bootstrap
dispatch), `Picea.Abies.Server/Session.cs:271`, `Picea.Abies.Browser/Interop.cs:319-362`,
`Picea.Abies.Browser/wwwroot/abies.js:991-1008,1087-1181`, `Picea.Abies.Conduit.App/Conduit.cs:198`,
`README.md:183-190`, and `.claude/hooks/validate-phase-artifact.sh:192-220`.

## Verdict

**CONFIRMED WITH A BOUNDED LIST — two 🔴, four 🟠, seven 🟡.**

The amendment is right, and the mechanism is the *forced* choice rather than a chosen one (§ 🟢).
It is confined at the level that matters: no new type, no new `EdgeState`, no new
`MovementAvailability` case, no new `Scrub` site, no policy member, no new dependency, no
`Runtime.cs` line, and the claimed spec-fixture cost checks out clause by clause. But it is **not**
finished: one of the two blockers is a sentence that now says the opposite of the rule, standing in
the two places an implementer is told to implement from, and the other is that the rule keys on a
message type this repository's own front-page example does not use.

Everything below is a sentence, a number, or a question in a brief that is already open. **Nothing
here needs a revision 5, nothing reopens a gate decision, and nothing asks for mechanism.**

## Answers to the five questions asked

| # | Question | Answer |
|---|---|---|
| 1 | Do the three artifacts agree? | **On mechanism, yes** — the rule, its placement in `apply`, the both-edges seal and the cause are identical in all three. **On prose, no**, in four places: B11, S30, S31 and 🟡 2. |
| 2 | Which obligations number is right — 14 or 16? | **16.** See S31: the spec's table is the union of the plan's thirteen plus the Critic's S26 and S25(a), and it is the table the lock, `csharp-dev` and `reviewer-reconcile` read. The plan's row 14 must become row 16, and the plan's header cross-reference at `:32` with it. |
| 3 | Does the amended INV-2 permit the seal and forbid the stale-URL restore? | **Both, and it forces the seal** — of the four classification rows only `seal` satisfies it. Derived in § 🟢 1. |
| 4 | Does INV-3 need the location too? | **No, and the reason should be written down rather than left as "raised and not acted on".** Derived in § 🟢 2. |
| 5 | Does the validator tolerate two properties on `INV-2`? | **Yes, mechanically** — `validate-phase-artifact.sh:197-202` collects declared ids from `00-scope.md`, collects `INV[-_](\d+)` hits from the spec, and reports only ids that are **missing**; there is no count and no cap, and `has_heading(low, "propert", …)` at `:214` is satisfied by `### The properties`. **The prose that says otherwise is in two other files** — 🟡 1. |
| 6 | Is the amendment confined to the finding? | **Yes for scope creep** — I checked for it specifically and found none: no new type, step, question, dependency or behaviour beyond the classification of one message. **No for consequences** — it left four true statements false elsewhere (B11, S30, 🟡 2, 🟡 3) and did not land in two steps that state the same thing to adopters (S32). |

## 🔴 Blockers

### B10 — The rule keys on a message type the seam does not guarantee. This repository's own README hands an adopter the shape that defeats it, silently, with every property still green.

**Failure scenario.** An application follows `README.md:183-190`:

```csharp
public record UrlChangedTo(Url Url) : Message;

public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch(
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new Tick()),
        Navigation.UrlChanges(url => new UrlChangedTo(url)));
```

It composes with `WithHistory` under `WholeModelHistoryPolicy`. The user types (record → `Origin`
becomes `Established`), presses the browser's **Back** button, then presses undo. `apply`'s new rule
tests `origin is UrlChanged`; the origin is `UrlChangedTo`. The rule does not fire. The message is
enveloped, it moves the projection, its command is silent — so it takes the `record` row, exactly as
before the amendment. The next undo crosses that step and restores the previous page's model while
the browser stays where Back put it. **This is the precise failure the amendment exists to remove,
and it is unremoved.**

**Evidence.** On the WebAssembly head there is no framework-owned delivery of an incoming URL
change. `abies.js:1112-1116` and `:1173-1175` call `onUrlChangedCallback`; `Interop.cs:361-362`
routes that to `NavigationCallbacks.HandleUrlChanged`; `Navigation.cs:53-67` invokes
`OnUrlChange`; and `OnUrlChange` is assigned in exactly one place — `Navigation.cs:17-29`, inside
`Navigation.UrlChanges(Func<Url, Message> toMessage)`, where **the application supplies the message
constructor**. `Picea.Abies.Conduit.App/Conduit.cs:198` and every tutorial pass `url => new
UrlChanged(url)`; `README.md:190` does not. The server head is unaffected — `Session.cs:271`
dispatches the framework's `new UrlChanged(url)` itself — so the hole is exactly the head where
browser Back matters most.

This is my dossier's recurring shape #6, *"a classification that needs an identity the seam does not
carry"*, for the **third** time in this pass: B8 (the `Err` channel), B9 (subscription versus click),
and now this. The test that catches it is the same one each time — enumerate the actual messages on
each side of the classification, from the runtime, before believing the rule.

**Why no property sees it.** `06-spec.md`'s fixture dispatches the framework `UrlChanged` directly
(`:181`, `:735-745`), so INV-2's second property is green for an application that has the bug. Its
named falsifier (`:1192`) deletes the *rule*, not the *idiom*, so it does not reach this either.

**Mitigation — three options; the middle one is the only one that costs nothing and I am not
choosing for you.**

1. **Scope the claim.** `00-scope.md`'s amended INV-2 currently asserts the location guarantee
   unconditionally. Qualify it the way the pass already qualifies L1–L6: the guarantee holds for
   navigation delivered as `Picea.Abies.UrlChanged`, and mapping incoming navigation to that
   message is **the application's obligation** — the same register as spec line 2's
   independent-reachability assumption, which is likewise an assumption the framework cannot test.
   Then say it in step 5's docs (vi), in the guide (step 13), and in spec line 16. Documentation
   only, no mechanism, no gate decision touched. **This is the option that fits the constraint you
   set.**
2. **Fix the README** (`tech-writer`, step 13) so the two idioms in this repository stop
   disagreeing, and note that the README example is not itself a `WithHistory` adopter. Cheap and
   worth doing under either option, but on its own it is documentation standing in for an
   invariant.
3. **Reach for mechanism** — a policy predicate, an analyzer rule, or a framework-owned wrapper
   around `UrlChanges`. All three are new mechanism, one of them reverses a gate-4 deletion, and
   none is needed to *state the truth*. I would file the analyzer as a fast-follow candidate and
   take (1) now.

### B11 — The sentence "After the first `record`, navigation classifies like any other message" survives in both places that are explicitly marked *implement from this*, eleven lines above the rule that contradicts it.

**Failure scenario.** `csharp-dev` opens step 6, is sent to § *Origin re-basing*, and finds the
block introduced as *"the correct statement, which is what `spec-author` and `csharp-dev` implement
from"* (`04-realist-plan.md:1134`), ending:

> **… After the first `record`, navigation classifies like any other message.**
> — `04-realist-plan.md:1138`

It implements exactly that: the post-record navigation takes the `record` row. `[R4-nav]` at
`:1149-1164` says the opposite. The same sentence is the closing clause of **standing decision 7**
in `07-handoff.md:146` — the dispatch document, in the table headed *"Standing decisions carried
into implementation"*, which is the artifact that survives the design directory in a specialist's
context. Both were true when written and are false now.

**Evidence.** `04-realist-plan.md:1136-1138` (the quoted normative block) against `:1031` and
`:1157-1158` (the rule); `07-handoff.md:146` (unchanged since close-out, which predates the
amendment).

**Mitigation.** One clause in each: *"After the first `record`, an incoming `UrlChanged` **seals
both incident edges** (`[R4-nav]`); every other message classifies by the four-row table."* And
`07-handoff.md` needs the amendment recorded at all — § 1's approval row 5 lists **only** amendment
1 (A9), and § 6.1 maps the fourth pass's mitigations to owners with no `[R4-nav]` row. As it stands,
the handoff is the one artifact from which the classification rule is entirely absent. That file is
the architect's.

## 🟠 Significant

### S30 — *"The world is a command's feedback and nothing else"* is now false, and it is stated as a first-class adopter-facing fact in four places. One of them is the bullet directly above the new one.

`04-realist-plan.md` step 5's file-docs obligations now read, adjacent:

- **(iv)** *"'the world' is a command's feedback and nothing else; a subscription-delivered message
  is a user action"* (`:1404-1406`)
- **(vi)** *"that incoming navigation is the world"* (`:1409-1413`)

These contradict, and not merely verbally: on the browser head an incoming `UrlChanged` **is** a
subscription delivery (B10's evidence chain — it arrives through `Navigation.UrlChanges`, i.e.
`DispatchFromSubscription`), so (iv) and (vi) classify the same message on the same path two
different ways. The same pair sits in `04-realist-plan.md` § *Spec obligations* lines 3 and 14, in
`06-spec.md` lines 3 and 16, and — unqualified, with no navigation bullet anywhere near it — in
**step 13's** tech-writer brief at `:1556-1558`, which is what ADR-030, `docs/concepts/undo-redo.md`
and `docs/guides/adding-undo.md` will say.

**Second half, and it is content rather than wording.** The **reach** statement now has three
bounds, and every place that states it gives one or two:

1. the last command feedback (S10, stated in five places);
2. `Depth` ÷ subscription rate, in wall-clock seconds (S25(b), landed in spec line 4);
3. **the last incoming navigation** — new, and in a routed application it will usually be the
   binding one. In Conduit, undo reaches back to the current page and no further.

Spec line 5 and step 13's third bullet both still say *"bounded by the last command feedback"*.

**Mitigation.** One qualifying clause wherever the "and nothing else" sentence appears — *"…and
nothing else, with one exception the same table carries: an incoming `UrlChanged` once `Origin` is
`Established` (line 16)"* — and the third bound added to the reach statement in spec line 5, step
5(iv), step 13 and the guide, at the same weight as the other two. Doc-only, five sentences.

### S31 — The obligations line is 14 in the plan and 16 in the spec, and the plan's 14 and the spec's 14 are **different obligations**.

`04-realist-plan.md:1739` numbers the navigation obligation **14**. `06-spec.md:1231-1233` numbers
**14** the superseded-branch line (S26), **15** the mid-bracket line (S25(a)) and **16** the
navigation line. Both tables are internally consistent; together they make the token "obligation 14"
name two different behaviours.

**Failure scenario.** `reviewer-reconcile` reads `04-realist-plan.md` as claims to verify and
`06-spec.md` as the bar. It checks "obligation 14" in the plan — the navigation seal — ticks it,
and never checks the spec's 14, the superseded-branch behaviour, which has **no plan-side row at
all**: S26's mitigation was routed from `05-critic.md` straight into the spec, so the plan's table
never gained it. The answer to `00-scope.md` open question 3 is the row that falls through the gap —
which is the same failure S26 was raised about, one artifact further downstream.

**Mitigation. 16 is right.** The spec's table is the union and is the one the lock and the review
read. Renumber the plan's navigation row to **16**, and add rows **14** and **15** to the plan's
table pointing at `05-critic.md` S26 and S25(a) so the plan's table is a prefix of the spec's rather
than a different sequence. Fix the cross-reference at `04-realist-plan.md:32` with it.

### S32 — The amendment landed in six places in the plan and in neither of the two steps that state the same thing to a human — and `ux-expert`, whose brief is wrong, is dispatched **first**.

`07-handoff.md:279` puts step 12 (`ux-expert`) in dispatch wave 0, *"because its answers land before
step 5 fixes the policy defaults"*. Its questions were written against the pre-amendment behaviour:
q9 is the navigation question, and `04-realist-plan.md:1147` says in as many words *"`ux-expert` q9
confirms with the corrected description in front of it"* — the S23 correction, not this one.

**Failure scenario.** `ux-expert` answers q9 believing that after the first record a navigation is
an ordinary undoable step, recommends chrome wording and a default on that basis, and the answer
lands in step 5's defaults before anyone notices. Step 10's Done-when (`:1535-1538`) then asks for
the rendered refusal *"blocked — the page changed while you were editing (ProfileLoaded)"* — a
sentence which, for a user who has just pressed **Back**, becomes *"the page changed while you were
editing (UrlChanged)"*. That is a confusing thing to say to someone who changed the page on purpose,
and the wording question is exactly what q13(b) exists for.

**Mitigation.** Add the navigation bullet to step 13's tech-writer brief (beside, and qualifying,
`:1556-1558`); add the navigation case to step 10's rendering Done-when; and put the amendment in
front of `ux-expert` as part of q9/q13 **before** wave 0 is dispatched, since it is scheduled first.
No new question and no new spawn — the questions exist; their premise changed.

### S33 — `UrlChanged` is framework-sealed, so an application cannot mark it `SensitiveCause`. The amendment guarantees that the whole `Url` of every post-record navigation becomes a stored edge cause and is handed to the chrome.

**Failure scenario.** A password-reset or magic-link flow: the browser is at
`/reset?token=8f2c…`. The user types, the app records, the head delivers `UrlChanged`. `seal` stores
`SealedByWorld(redact(origin))` (`04-realist-plan.md:1043-1044`), and `redact` (`:1070`) rewrites
only a cause that implements `SensitiveCause`. `UrlChanged` is
`public sealed record UrlChanged(Url Url) : Message` (`Picea.Abies/Program.cs:139`) — **sealed, and
framework-owned**, so no adopter can mark it. `Url` carries `Path`, `Query` and `Fragment`
(`Program.cs:87`) and the token rides in `Query`. `Backward(h)` then returns
`BlockedByWorld(thatUrlChanged)` from the history value on **every render**, and step 10's chrome
renders the cause.

I am deliberately not calling this a new exposure *class*: before the amendment the same
`UrlChanged` was retained as `Step.Cause` on the recorded step, so the value was already in the
history. What the amendment changes is that it is now **certain** rather than incidental (every
post-record navigation seals), and that the value's route to a rendered surface is the refusal the
chrome is being designed to display. SEC-4 is unaffected — telemetry tags are
`Cause.GetType().Name` only.

**Mitigation.** One sentence appended to the fourth `security-expert` spawn that S29 already opened,
so no new spawn is created: *"`[R4-nav]` makes the full `Url` — path, query and fragment — the
stored `EdgeState` cause of every post-record navigation, returned by `Backward`/`Forward` and
rendered by the chrome; `UrlChanged` is sealed and framework-owned, so `SensitiveCause` is
unavailable to an adopter for it. Is that accepted and documented, or does SEC-2 need a
framework-side rule for this one cause?"* A question, not a mechanism — and note the irony worth
putting in front of the room: under B10 an application that names its own navigation message **can**
mark it `SensitiveCause` and gets no seal, while one that uses `UrlChanged` gets the seal and cannot
mark it. Both are the same seam.

## 🟡 Minor

1. **"One property per id" still reads as *exactly* one in the two files that state the rule.**
   `00-scope.md:113-114` (*"`06-spec.md` must carry one property per id"*) and
   `04-realist-plan.md:1453` step 8(a) (*"one property per invariant id"*). The departure is
   recorded in `06-spec.md:853-855` and in the approval record, but not where the rule is written.
   The architect is amending `00-scope.md` anyway; *"at least one property per id"* costs a word,
   and step 8(a) should read the same, so that a reviewer verifying step 8(a) as a claim does not
   have to adjudicate two properties on one id.
2. **`04-realist-plan.md:345-357` § *The scope clause this needs — both amendments have landed*
   now says something false**: *"Nothing in revision 4 asks for a third amendment."* The header at
   `:32-35` qualifies the *"`INV-2` is untouched"* sentence at `:96` — correctly and honourably —
   but not this section. One clause: *"a third has since landed, `[R4-nav]`."*
3. **The amendment lengthened `INV-2` and every citation into `00-scope.md` past it is now stale.**
   `04-realist-plan.md:350` cites `00-scope.md:188-192` for INV-7's definition of a movement (that
   range is now inside **INV-6**; the definition is at `:198-201`) and `:195-197` for the
   `abies:history:` reservation (now `:204-208`); `:1648` cites `00-scope.md:188-191` for the same
   clause; `06-spec.md:976` cites `00-scope.md:147-150` for document-owned state (now `:163-165`).
   The cheap durable fix is to cite the invariant id and the clause rather than the line range — the
   scope is now demonstrably a file that moves, and it is the one file the ids exist to make
   citable.
4. **`04-realist-plan.md:1504-1505`'s contingency for 8(n) — *"if that proves impracticable at the
   generator level, it moves to step 10's composition tests rather than being dropped"* — is no
   longer available.** `06-spec.md` locks that assertion inside `UndoRedoSpec.cs`. After the
   approval commit a move is a `// SPEC CONFLICT:` hand-back and a re-approval, not a relocation.
   One clause in 8(n) saying so. (The spec's own solution is sound and makes the contingency moot:
   it never reads the head's URL, it models the browser as the last `UrlChanged` it dispatched, and
   it says why that is honest for this fixture — `06-spec.md:871-877`.)
5. **A silent, model-identical `UrlChanged` bypasses the seal**, because `apply`'s true-no-op branch
   (`:1028`) precedes the rule (`:1031`). Reachable when a program leaves `UrlChanged` to its
   `_ =>` fall-through, returning the same model reference with `Commands.None`: the browser moves,
   the history is untouched, and nothing seals. Benign in the sense that such a program has no
   location in its model to restore — but the amended INV-2 says *"on any head where the
   application has one"*, and this is the case that phrase has to be read against. One sentence
   somewhere, or a deliberate decision that the phrase already covers it.
6. **A third writer for the last-writer-wins cause.** Pass 4's 🟡 noted that `sealTops` and
   `record`'s `ReplaceTop` overwrite each other's cause; `[R4-nav]`'s seal is now a third. Concretely:
   navigate (Future top ← `SealedByWorld(UrlChanged)`), then act (`record` → `ReplaceTop` →
   `SupersededByNewAction(cause)`). Redo still refuses — nothing unseals, so no reachability
   changes — but it refuses under the later name. Step 3's *"most recently"* wording fix already
   requested covers this if it is written to include the navigation seal.
7. **The pass's most user-recognisable behaviour has no acceptance-layer example.** Nine `A`-tests
   cover reach, supersession, the world, batching, brackets and terminality; spec line 16's only
   home is a property (`06-spec.md:1233`). *"Type, press Back, press undo — it refuses and names the
   navigation"* is the shape a user recognises, and the acceptance layer exists for recognition. The
   property asserts the observable directly, so this is genuinely optional and it costs one more
   fixture-free test; I raise it as an option, not a requirement, and you approved a minimal fixture
   cost knowing the shape of it.

## 🟢 What the amendment gets right

1. **The seal is not a choice, it is the only row the amended INV-2 leaves — and that is
   derivable, which is what makes the amendment sound rather than merely agreed.** Take the four
   rows of § *The classification rule* against an incoming `UrlChanged` once `Origin` is
   `Established`. `record` mints an undo stop whose crossing produces a (model, location) pair no
   ordinary interaction produced — INV-2's new falsifier, verbatim. `pass` moves `Present` and
   records nothing, but leaves the *earlier* stop crossable, so the first undo restores the
   pre-navigation model while the browser stays — the same falsifier by a slower route. `rebase` is
   `Fresh`-only by construction. **`seal` is what is left**, and it satisfies the invariant on the
   nose: `Present.Route` moves with the location in the same transition (`:1043`), and both incident
   edges refuse thereafter (`:1061-1062`), which is precisely *"the two move together or not at
   all"*. The amended INV-2 both permits it and forces it. I checked the composition too: after a
   seal, later `record`s push fresh `Crossable` steps **above** the sealed one, so undo works inside
   the new page and stops at the navigation — the reach bound falls out of `Backward` reading one
   edge, and is not arranged.
2. **INV-3 does not need the location, and the reason is stronger than "we left it alone".**
   `Present.Route` can only move on a transition whose origin is `UrlChanged`; after the first
   record every such transition seals **both** incident edges, and before it there is nothing to
   cross. So the location is constant across any window in which a movement is permitted, and
   INV-3's round trip — which requires `Backward` **and** `Forward` to be `Available`
   (`06-spec.md:943,949`) — can never span a location change: an undo pushes a fresh `Crossable`
   step onto `Future`, and a `UrlChanged` arriving before the redo seals exactly that step, so the
   property skips on its guard rather than failing. Adding a location clause to INV-3 would add a
   conjunct **no property could ever turn red on**, which is worse than leaving it out. Under a
   projection the same conclusion arrives by the other road: `Restore` keeps `current`'s `Route`
   (`06-spec.md:222`), so movement never touches the location at all. The architect was right not to
   act; the derivation should be recorded next to INV-3 or in the handoff so it is not re-raised at
   review as an omission.
3. **The amended INV-2's third falsifier — the two stacks disagreeing — is covered by derivation,
   and the derivation holds.** Nothing in a movement can touch the browser's stack:
   `StepBack`/`StepForward` return `Commands.None` (`:1010,1013`), so no `NavigationCommand` is ever
   issued by undo or redo, and `Runtime.cs:367-369` is the only route from a command to the
   location. Combined with the property's standing assertion that `Present.Route` equals what the
   browser shows, drift is unreachable. Worth one sentence in the spec so no reader believes that
   clause is separately tested, since the fixture has no browser stack to diverge — but the claim is
   sound.
4. **The two rules partition rather than overlap, so the "tested after" instruction is
   belt-and-braces rather than load-bearing.** `Origin` is `Fresh | Established`; `:1030` and
   `:1031` are mutually exclusive and jointly exhaustive over `origin is UrlChanged`. Order cannot
   silently matter, which is the right shape for a rule pair that reads the same message. S23's
   behaviour is untouched, and I verified that rather than accepting it.
5. **The property is falsifiable, and its two falsifiers are the right two.** Deleting the rule
   turns it red through a stale `Route` — I traced it: the fixture's `UrlChanged` case
   (`06-spec.md:181`) moves the model and is silent, so without the rule it takes `record` with a
   `Crossable` edge and the first crossing reverts `Route` while `browserShows` does not. And
   *"separately, seal only the past edge"* is what converts *"both edges"* from a sentence into a
   checked claim. The note that the corpus is not sufficient until **both** `Count > 0` branches
   have been observed taken (`:924-929`) is the difference between a guard and a softening, and it
   is stated as such.
6. **The fixture-cost claim is true, and I checked it clause by clause rather than accepting it.**
   One defaulted field (`:141`), one `Transition` case (`:181`), one generator symbol (`:735-745`),
   one property. `View` unchanged, so no document text and no `DocumentComparer` result shifts;
   `EditorMessages.All` unchanged, so INV-3's behaviour sweep is untouched; `Route` defaults to
   `"/"` and no other alphabet draws `UrlChanged`, so every pre-existing literal and every other
   property sees a constant. The terminality exclusion (`CloseEditor` out, `ZoomChanged` never 0)
   turns out to be load-bearing for the *new* property too — the wrapper's terminal guard would make
   a `UrlChanged` a no-op while `browserShows` moved — and it is inherited rather than restated,
   which is correct.
7. **The `Url` reference-equality caveat (`:904-908`) would have been a flaky test in anyone else's
   hands.** `Url` carries `IReadOnlyList` and `IReadOnlyDictionary` (`Program.cs:87`), whose record
   equality is reference equality, so comparing against a freshly constructed `UrlChanged` would
   fail for a reason having nothing to do with the mechanism. Asserting against the dispatched
   instance is the right call and the reason is written down.
8. **The alphabet extension is correct and the exclusion is honestly labelled as *uniform, not
   required*** (`:767-775`). After four passes in which properties kept being written in
   configurations that could not see their own trigger, this one states which properties may see
   `UrlChanged`, which may not, and — the part that matters — that the exclusion is a tidiness
   choice rather than a necessity, so a later reader does not preserve it for the wrong reason.

## Gates

- **Bug-fix regression test:** **present, and this is the one place in the pass where the gate
  actually bites.** The amendment is a fix to a defect found in an approved design, and it ships
  with a property whose named falsifier reproduces the original defect (delete the rule → a
  navigation records → the first crossing lands on a model whose URL is not the one the browser
  shows). Red before, green after. **Subject to B10:** the falsifier reproduces the defect for a
  program that uses the framework's `UrlChanged`, and the *other* way of reaching the same defect —
  an application-named navigation message — has no test and, under mitigation option (1), would not
  have one because it would be out of the claim.
- **Principles compliance:** **clean.** No new deviation. The rule is one branch in a total
  classification over a closed sum (`Origin`), the refusal stays a typed value on the ordinary
  message path, no exception, no flag, no null, no new primitive. The three live deviations are the
  three the fourth pass recorded, all user-approved. **One new *documented obligation* is proposed
  rather than a deviation** — B10 mitigation (1) puts a second assumption on the adopter beside
  L1–L6's reachability assumption, and that is a scope-wording decision for you and the architect,
  not a principle question.
- **Terminates at reviewer:** **yes**, unchanged — the amendment adds no step and no PR;
  `07-handoff.md:144` still routes all seven PRs through `reviewer-blind` → `reviewer-reconcile`.
- **Invariant coverage:** **seven ids, eight properties.** `INV-2` now carries two, both quantified
  over the whole input space, both with named falsifiers, and the second one is the only property in
  the file whose claim is about anything outside the model. The validator tolerates it
  (`validate-phase-artifact.sh:197-202` — presence per id, no cap). No invariant is example-tested.
  The gate is met.

## Specialist Spawns Recommended

- **`security-expert` — no new spawn; one sentence appended to the fourth spawn S29 already
  opened.** S33's question, verbatim above. Documentation and wording only.
- **`ux-expert` — no new spawn; a corrected premise for q9 and q13, before wave 0.** S32. It is
  dispatched first in `07-handoff.md:279`, so this is the one item with an ordering deadline.
- **`performance-engineer` — nothing.** A seal pushes no `Step`; the amendment does not touch
  retention, the `2 × Depth` ceiling or the dispatch path, and a routed application will now retain
  slightly *fewer* steps.

## 🛑 Pause — for the user

> *Here are the risks and proposed mitigations. Accept the mitigations, loop back, or proceed as
> planned?*
