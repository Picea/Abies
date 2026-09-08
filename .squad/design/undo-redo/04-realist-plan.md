# 🔧 Realist Plan — undo/redo (`WithHistory`)

**Revision 4**, after `05-critic.md`'s **third LOOP BACK TO REALIST** (B8, B9; seven 🟠;
six 🟡). Direction **H1 stands**; nothing goes back to the Dreamers, and nothing in this revision
requires `03-convergence.md` to be re-opened. This artifact overwrites revision 3 in place, as the
loop-back protocol requires. The Critic's findings stay in `05-critic.md`; every one is answered
below, by id, in § Disposition.

**Revision 4 is `05-critic.md` § *What revision 4 must contain* and nothing else**, by the user's
instruction. Everything the Critic marked closed in revision 3 is carried through verbatim. Where a
change was available and I did not take it, that is deliberate and said so. Nine items, each tagged
`[R4-n]` at the point it lands so the Critic can find it without re-reading the artifact:

| # | Finding | Where it lands |
|---|---|---|
| 1 | **B8** — the `Err` channel is enveloped | § *The classification rule* `[R4-1]`, wrapper `Decide`, step 6, step 8(j) |
| 2 | **B9(b)** — `SupersededByNewAction`, and the default-policy narrative rewritten | § *The default policy* `[R4-2]`, § *Architecture Sketch*, steps 3, 5, 6, 10, 13, 16, ux q13 |
| 3 | **B9** — an autonomous-source property | step 8(k) `[R4-3]` |
| 4 | **S18** — the batch's error semantics | § *The classification rule* `[R4-4]`, step 6 Done-when, § *Spec obligations* |
| 5 | **S19** — `Scrub` is conditional on a projection | § *Security* `[R4-5]`, steps 5, 8(l), 13 |
| 6 | **S20** — the held anchor, named and bounded rather than scrubbed | § *Security* `[R4-6]`, steps 8(m), 13, 16 |
| 7 | **S21** — what an open hold actually does | step 9 assertion (4) `[R4-7]`, § *Spec obligations* |
| 8 | **S22 / S23 / S24** — three statements of fact | § *Movement* `[R4-8a]`, § *Movement* `[R4-8c]`, wrapper `[R4-8b]`, steps 9, 10, 13 |
| 9 | 🟡 1–6 — all six taken | `[R4-9]` at each site |

**One addition after close-out, from the user and not from the Critic**, tagged `[R4-nav]` so it is
not mistaken for a tenth Critic item: incoming navigation is not classified, so after the first
recorded step a browser back or forward press `record`s. One classification rule beside the origin
re-basing rule — `origin is UrlChanged && Origin is Established -> seal,
SealedByWorld(UrlChanged)`, both edges — lands in § *The classification rule* and § *The wrapper*'s
`apply`, with the S23 interaction in § *Origin re-basing*, step 5 docs obligation (vi), step 6's
`apply` enumeration, step 8 property (n), and § *Spec obligations* line **16** (renumbered from 14
per the confirmation pass's S31, so that the plan's table and `06-spec.md`'s are the same sequence —
the navigation obligation is line 16 in both). **Unlike the nine above it does carry a scope amendment**: `INV-2` is
being amended by the architect so that "the world" includes the browser's location, a navigation
being a commitment in both directions — which qualifies the *"`INV-2` is untouched"* sentence below,
written of the nine.

**The `[R4-nav]` amendment was then confirmed by the Critic** (`05-critic.md` § *confirmation pass,
2026-09-07*: two 🔴, four 🟠, seven 🟡, **no revision 5**), and the user accepted all thirteen
mitigations on 2026-09-07. What that added to this artifact, all of it sentences and none of it
mechanism, is tagged `[R4-nav]` at each site so it reads as one amendment rather than fourteen:
**B11** — the normative sentence in § *Origin re-basing* now states the seal rule instead of
contradicting it; **B10** — the application obligation, stated in full in § *The lens, and its six
obligations* and carried into step 5(vi), step 13 and obligations line 16; **S30** — the *"and
nothing else"* sentence qualified in its four homes (§ *What "the world" is*, step 5(iv), step 13,
obligations line 3) and the reach statement given its third bound wherever it is stated; **S31** —
the obligations table renumbered to sixteen, with rows 14 and 15 added, matching `06-spec.md`;
**S32** — step 13's navigation bullet, step 10's rendered navigation case, and the corrected premise
for `ux-expert` q9/q13(d) before wave 0; and the plan-side 🟡 — step 8(a) (*at least* one property
per id), § *The scope clause* (a third amendment has landed; citations by id rather than by line),
step 8(n) (the contingency is a spec hand-back, not a relocation), 🟡 5 and 🟡 6 beside the rule, and
step 3's *most recently*. **S29's and S33's** sentences belong to the `security-expert` brief and
`07-handoff.md`, not here; **🟡 7**'s optional acceptance test belongs to `06-spec.md`.

Read this revision: `00-scope.md` **as amended and landed**, `00-knowledge.md`,
`03-convergence.md`, `04-realist-plan.md` revision 3, `05-critic.md` **third pass** in full, and
`room-security.md` in full **including both 2026-09-07 follow-up sections**. Re-verified in the code
this revision: `Picea.Abies/Runtime.cs:280-292,441-513` (the `Err`-channel dispatch path B8 turns
on, and the single delegate B9 turns on) and
`~/.nuget/packages/picea/1.0.0/lib/net10.0/Picea.xml:1229-1247` (`AutomatonRuntime.Dispatch`
runs *"transition, observer, and interpreter pipelines"* and `InterpretEffect` *"dispatches any
produced feedback events"* — `Program.Decide` is in neither list, which is what makes interpreter
feedback the only remaining bare arrival). Revision 3's own code citations are unchanged and not
re-listed.

## The four decisions relayed by the orchestrator, and what each did to the plan

1. **B9, option (b) — the discard becomes typed.** `EdgeState` gains
   `SupersededByNewAction(Cause)`. When `record` would discard a non-empty `Future`, the branch is
   **preserved and its first edge sealed**; `Forward(h)` refuses with the cause, in advance, from
   the value alone. The gate-2 rule becomes **"refuse across the superseded branch"** — and the
   reason the original rule existed (a *crossable* retained branch makes redo a relation, which has
   no inverse, so INV-3 becomes unstatable) **does not apply to a sealed one**, because a sealed
   edge is never crossed. This is the answer to `00-scope.md` open question 3. § *The default
   policy*.
2. **B8 — a rejection is the application's own response, not the world.** `Decide`'s delegating
   branch returns `Err(HistoryEvent.Enveloped(m, [e]))` instead of `Err(e)`. § *The classification
   rule*.
3. **S18 accepted** — the fold applies all events of one decision before any command is
   interpreted, so a failing command skips later *commands*, not later *events*. Stated as a
   behavioural difference from an unwrapped program, in the same register as the ordering
   difference the user accepted at gate 4.
4. **S19 and S20 per the security room's second follow-up** (`room-security.md:474-663`) —
   SEC-3(b) gains the joint-lawfulness precondition and its reachability set is **unchanged**; a
   lens-law test `Scrub_overridden_alone_violates_L5`; the held anchor is neither scrubbed nor made
   unreachable this pass, and is instead named, bounded, tested and logged for fast-follow.

**Two places where the composition of these four decisions is narrower than the wording of any one
of them, flagged rather than absorbed.** Both are consequences, not deviations, and neither is mine
to settle silently:

- The Critic's item 2 wording — *"'the world' is command feedback **and rejected decisions**"* — was
  written before item 1 (B8) was decided. With B8 decided as envelope-routing, a rejected decision
  is **no longer** the world; it is the application's own response, which is decision 2's stated
  reason. The truthful narrative after both decisions is therefore **"the world" is a command's
  feedback, and nothing else.** § *The default policy* is written that way. If the user meant the
  narrative to keep naming rejections as the world, that is B8's rejected alternative
  (`SealedByDecision`) and it needs saying.
- The Critic's item 6 wording — *"SEC-3 clause (b) reworded to quantify over models retained by a
  `History` value, naming `Movement.Held(Anchor)`"* — is **declined by `security-expert`**, in the
  same sentence of the Critic's that deferred the anchor's treatment to it: widening clause (b) to
  name a site it cannot discharge makes the clause false the instant it is adopted
  (`room-security.md:613-620`). Clause (b)'s wording is unchanged; the anchor gets its own,
  honestly-scoped guarantee. § *Security* `[R4-6]`.

**Unchanged and carried forward** (settled at gates 3, 4 and 5): the seal default named
**`WholeModelHistoryPolicy`**; explicit `Hold`/`Settle` brackets with **no timer**; **`Scrub` + L5
at every `Step`-construction site**; **one enveloped event per decision**; A3(b)
crossable-iff-inverse excluded; no demo or template adopts `WithHistory`; ADR-008 line 85 amended at
implementation; hand-rolled seeded generators, no new test dependency; seven sequential PRs to
`main`; **`SensitiveCause`, no `I` prefix**; the **erased** lens (`Restore`/`SameUndoable` over
`TModel`, no fifth type parameter).

**No scope amendment is forced by this revision, and I checked rather than assumed.** `INV-2` is
untouched — a superseded step is never crossed, so no new state becomes reachable. `INV-3` is
untouched — supersession happens on `record`, never on `redo`-applied-to-an-undo, so the round trip
it quantifies over is unaffected. `INV-5` is *better* served: `SupersededByNewAction` is one more
typed reason, which is what the invariant asks for. `INV-1` and `INV-7` already carry the two
amendments revision 3 proposed; both have landed (§ *The scope clause*). If the architect disagrees
on any of these at close-out, the wording is theirs to write and mine to flag — it is flagged here.

---

## Chosen Direction

**H1 — `WithHistory`**, per `03-convergence.md` § "Gate 2 — the user's decision (answered)":
B1's higher-order-program foundation, A1's three spec-able rules (stratification; `Ok([])` and not
`Err` at the ends; destination computed in `Transition`), A3's projection lens as the exemption
instrument, and B1's evidence-backed defaults.

The user's reasons, unchanged since gate 2: undo refuses at an effect boundary, with the refusal
typed, explained and visible in advance; one undoable step = one dispatch; durable undo out of
scope; INV-2 stands as written.

**Explicitly out of H1's element list, and revision 2 failed to say so** (Critic 🟡 5):
`03-convergence.md:420`'s **deferral / hold window** (Gmail's Undo Send) is **not in this pass**.
It is a mechanism for delaying an irrevocable effect, which is a property of the *effect boundary*,
not of the history; admitting it would mean the framework holding commands back, which is a fifth
runtime seam. Noted for a later pass. `03-convergence.md:419`'s **`NavigationCommand.Replace`** is
also out, and revision 2 dropped it silently: under this design `NavigationCommand` is not silent,
so a navigation is a commitment and undo does not cross it. Both exclusions are now on the record.

---

## Movement: what a run is, now that there is no clock

Revision 2 decided a run was over after 250 ms of quiescence. B5 showed that two undo presses 350
ms apart are then two runs, that the second run reconciles subscriptions against the state in
between, and that the planned property ran in a mode (`SettleWindow = null`) that could not see it.
The user's decision removes the clock. This section is the replacement mechanism.

### The shape

```
Movement = Settled
         | Held(TModel Anchor)
```

```
Subscriptions(h) = h.Movement switch
                   { Settled  => TProgram.Subscriptions(h.Present)
                   , Held(a)  => TProgram.Subscriptions(a) }
View(h)          = TProgram.View(h.Present)          // transit is visible, as it must be
```

**The wrapper declares no subscription of its own.** No settle source, no `abies:history:settle`
key, no `Subscription.Batch`, no `TimeProvider` on the default path. `Picea.Abies.History`'s
dependency on `Picea.Abies.Subscriptions` shrinks to the `Subscription` *return type* that every
`ProgramCore` already names (`Program.cs:24`).

Four messages drive it:

| Message | Precondition (`Decide`) | Effect (`Transition`) |
|---|---|---|
| `Undo` / `Redo` | `Ok([])` when unavailable; `Err(MovementRefused …)` when blocked | move the cursor. `Movement` is **left exactly as it was** |
| `Hold` | `Ok([])` when already `Held` | `Settled → Held(h.Present)` |
| `Settle` | `Ok([])` when already `Settled` | `Held → Settled` |
| `Clear` | `Ok([])` when `Past` and `Future` are both empty | clears both stacks **and** settles |

So:

- **An un-bracketed press is a complete movement.** `Movement` stays `Settled`, the render that
  follows reconciles once against the state the press landed on, and there is no transit state to
  leak into the next dispatch. This is what every chrome that draws two buttons gets, with no
  obligation and no configuration.
- **A bracketed run is one movement.** `Hold` captures the anchor; every press inside the bracket
  moves `Present` while `Subscriptions` keeps reporting `TProgram.Subscriptions(anchor)`;
  `Settle` releases, and the next `Render` performs exactly one reconciliation from the anchor's
  set to the settled set.
- **A movement is therefore delimited by the user's own input, not by elapsed time** — which is
  precisely the reading the user asked for, made discrete.

### Why this is free at the runtime seam (unchanged from revision 2, re-verified)

`Runtime.Render` asks **`TProgram.Subscriptions(state)`** (`Runtime.cs:214`, and `:424` for the
initial set). Under this design `TProgram` *is* `WithHistory<…>`, so answering with the anchor's set
is not suppression — it is the wrapper answering the question it is already asked, as the sole
author of the desired set. `SubscriptionManager.Update` is keyed and diff-based
(`Manager.cs:51-74`): report the same keys, and it starts nothing, stops nothing, tears nothing
down. And `Runtime.cs:212-220` sits **outside** the `if (allPatches.Count > 0)` guard at `:200`, so
a settling render reconciles even when the document is unchanged. Zero lines of `Runtime.cs`. The
`runtime-seams-anchor-replay-gating` revisit trigger does **not** fire.

### INV-7's three verbs, each answered

INV-7 has three: *started*, *stopped*, *delivers a message*. Revision 2 claimed only the first two
(`04-realist-plan.md` rev. 2 `:196`) and B5 was right that the anchor made the third *worse*.

- **started / stopped.** During a `Held` run the reported key set is constant and equal to
  `keys(TProgram.Subscriptions(anchor))`. Nothing starts or stops on account of an intermediate
  state. For an un-bracketed press there is no intermediate state: the press's destination is the
  state the user asked for.
- **delivers.** During a `Held` run the *only* sources that are live are those the anchor declared.
  A source declared solely by a state passed through is never started, therefore never delivers.
  This is a positive statement about a set, and it is what the property asserts directly: every
  delivery observed inside a bracket carries a key drawn from the anchor's key set. Break the
  anchor — report `Subscriptions(h.Present)` during a hold — and a source declared only by an
  intermediate state starts and delivers, and the assertion fails. **That is a falsifier for the
  delivery verb, not an exclusion of it.**

**B5 scenario (ii) is now closed rather than deflected.** Revision 2's rule 2 said *any*
non-movement event collapses `Movement` to `Settled` before applying, so a tick landing mid-run
ended the run and the following render reconciled against the intermediate state. **Rule 2 is
deleted.** A `Held` run ends on `Settle` or `Clear` and on nothing else. A message that arrives
during a hold is applied to `Present` and classified normally (it may `seal` the history, which is
the correct and typed consequence), but it does not end the movement and it does not cause a
reconciliation against a state the user is passing through. The world does not get to redefine what
the user's gesture was.

### The cost of deleting rule 2, stated — **and how the recommended chrome hits it** `[R4-8c]`

A chrome that dispatches `Hold` and never dispatches `Settle` pins the application's subscription
set at the anchor indefinitely. There is no timer to rescue it and no *world* event to rescue it. In
the worst case an application whose only message source is a subscription declared by the
*destination* state and not by the anchor makes no further progress until something external
dispatches.

**S24, and revision 3 was wrong about how exotic this is.** The `keydown`/`keyup` bracket the guide
is about to recommend loses its `Settle` on ordinary browser behaviour: a `keyup` is not delivered
to an element that has lost focus, so alt-tab, a focus change, or a click elsewhere mid-hold strands
the bracket. That is not an edge case, it is Tuesday. `Html/Events.cs` already carries the antidotes
— `onblur` (`:231`) and `onpointercancel` (`:271`) — and revision 3 reached for `onblur` for the
scrubber but not for the key bracket, which is exactly backwards: the key bracket is the one that
loses focus.

**Two corrections to revision 3's text, both mine:**

- **The chrome contract grows two verbs.** It is no longer *"whoever dispatches `Hold` owns the
  matching `Settle`"* but *"whoever dispatches `Hold` owns the matching `Settle`, **and must also
  settle on `blur` and on `pointercancel`**"*. Step 10's composition test renders a chrome that
  does; step 13 states it in the guide's chrome-contract section.
- **There *is* one in-band rescue, and revision 3 said there was none.** `Transition(h, Clear)`
  settles (§ *The wrapper*), so the history-clear affordance a chrome already offers ends a stranded
  bracket. That is the only one; it is worth a sentence precisely because a reader who believes
  "nothing rescues it" will not reach for the affordance that does.

With those two, the accepted cost is a real edge case rather than a predictable one, and it is
bounded exactly:

- It requires a chrome that **opts in** to bracketing. A chrome that never dispatches `Hold` cannot
  reach the state.
- It is **visible**: `h.Movement` is part of the value the chrome already reads for INV-6, so the
  chrome can render "movement in progress" and a test can assert on it.
- It is **recoverable in band** — `blur`, `pointercancel`, or `Clear`.

**One further alias worth one line in the guide** (🟡 2) `[R4-9]`: `Decide(h, Hold)` is `Ok([])` when
already `Held`, so a second `Hold` from a *different* chrome is a no-op and the first `Settle`
closes both brackets. Harmless for INV-7 — the orphaned chrome's later presses become per-press
complete movements, which is the default behaviour — but surprising enough that the chrome contract
should say two chromes cannot hold independently.

I judge a visible, opt-in obligation on the one component that asked for the capability to be
better than an invisible 250 ms window that falsified the invariant for every application. If the
user disagrees, the fallback is revision 2's rule 2 restored, which reopens B5 (ii) — I would then
want the architect to qualify INV-7 rather than let the plan carry a known falsifier.

### What the chrome must do, per head — verified against the actual event surfaces

| Gesture | Browser / InteractiveServer | Native (WinUI/Uno) |
|---|---|---|
| Click an Undo button | `onclick(new HistoryMessage.Undo())`. Complete movement; nothing else needed. | `OnClick(new HistoryMessage.Undo())` (`Picea.Abies.Native/Events.cs:57`). Same. |
| Hold a key (auto-repeat) | `onkeydown` → `Hold` on the first, `Undo` on each; `onkeyup` → `Settle` (`Html/Events.cs:163-172`) | **Not available.** |
| Drag a history scrubber | `onpointerdown` → `Hold`; `onpointerup` → `Settle` (`Html/Events.cs:259-262`); `onblur` (`:231`) as a belt-and-braces `Settle` | **Not available.** |

**A verified head limitation, stated rather than assumed.** `Picea.Abies.Native/Events.cs` exposes
exactly `OnClick`, `OnTextChanged`, `OnToggled`, `OnValueChanged`, `OnSelectionChanged`,
`OnScrollChanged` (`:57-80`). There is **no key-up, no pointer-up and no blur on the native head**,
so a bracketed run cannot be closed there with today's event surface. The native head therefore
uses per-press complete movements — which is the default, costs nothing, and is INV-7-clean. A
native scrubber would need a new native event, which is a separate pass. This goes in the guide's
head-coverage table and in `ux-expert` question 11.

### What INV-7 actually protects, now that a movement is defined — stated, not softened `[R4-8a]`

**S22, carried as a consequence of the user's decision 1 rather than argued with.** Under the
amended INV-7, an un-bracketed press is a complete movement with **no states passed through**, so
the invariant is satisfied *by definition* for it. Five clicks on an Undo button are five movements
and reconcile subscriptions five times, starting and stopping sources at each intermediate state.
That is the behaviour B3 was raised about; the amendment makes it **legal**, not absent.

And bracketing is harder to reach than revision 3's chrome table implies. The framework's handlers
are **element-scoped** — `onkeydown`/`onkeyup` attach to an element (`Html/Events.cs:163-172`) and
there is no window- or document-level key handling anywhere in the framework — so the keyboard
bracket has no home unless the application builds one on a focused element, which sits awkwardly
beside `ux-expert` q3's standing recommendation that the framework ship no key binding at all. A
`<button>` chrome can bracket a single click with `onpointerdown`/`onpointerup`, which brackets one
press and therefore buys nothing. **With the clock deleted, there is no mechanism by which a
two-button chrome can make a run of clicks one movement.**

So, plainly: **INV-7's non-trivial content applies to a history scrubber and to a held key, and the
scrubber is the one chrome nothing in this pass builds.** For the chrome the guide will actually
show, INV-7 is true and empty. This is not a defect of the design — it is the honest consequence of
defining a movement by the user's own input rather than by a timer, which is what removed a
falsifier that applied to *every* application. It goes, in these words, in `06-spec.md`, in ADR-030's
consequences, and in the guide's head-coverage table beside the native-head limitation. And
`ux-expert` q11 is re-asked as the real question: **is per-press reconciliation acceptable for the
chrome we are actually going to show people?**

### Why B6's ordinal is no longer needed — the answer the user asked for

**Removed, not carried.** B6 was a race between a settle that a cancelled `Task.Delay` had *already
dispatched* (`Runtime.cs:288-291` is fire-and-forget) and the run that replaced it. With the settle
source deleted there is no dispatched-then-cancelled settle, and the only `Settle` messages in the
system are ones the chrome sent deliberately, in the order the input events occurred.

A duplicate or late `Settle` is harmless by construction: `Decide(h, Settle) = h.Movement is Settled
? Ok([]) : Ok([Settle])`, which short-circuits at `Runtime.cs:465-468` before any transition, render
or effect — the INV-4 shape. `Transition` re-checks on the value it has, so even the B4 interleave
cannot make a stale `Settle` do work. **No ordinal, no key re-arming, no `HistorySettleSource.cs`,
no reserved subscription key.** One test covers it: *a `Settle` dispatched with no run open changes
nothing, renders nothing and emits nothing.*

### INV-7's property, in the mode the product ships (step 9)

`SettleWindow` no longer exists, so the objection that the property "runs in a mode no application
will ship in" cannot be made of this design. The property's alphabet:

- application actions that **change which subscriptions the model declares**;
- `Undo`, `Redo`, `Hold`, `Settle`, in generated order, including malformed orders (`Settle` with no
  hold, `Hold` twice, a hold left open);
- **autonomous deliveries**: `CountingSubscription` dispatches on start and on a test-driven pulse,
  so a source that should not be live *proves* it by delivering. Revision 2's "dispatches on demand"
  is replaced.

Assertions, over a real `Runtime`:

1. for each maximal movement — one un-bracketed `Undo`/`Redo`, or one `Hold`…`Settle` bracket — the
   application-keyed **start/stop** log equals the log of a single reconciliation from the
   pre-movement set to the settled set (`00-scope.md` **INV-7**'s *"equivalently"* falsifier,
   verbatim — cited by id per 🟡 3 of the confirmation pass, since the amendment moved the ranges);
2. every **delivery** recorded inside a bracket carries a key in the anchor's key set;
3. no application-declared key begins with `abies:history:` (Critic 🟡 4 — the prefix stops being
   documentation-only and becomes an assertion, even though the framework now declares nothing);
4. **the property fails when the anchor is removed.** Assertions 1 and 2 must both break under a
   deliberately broken `Subscriptions`, and step 9 is not done until that has been observed.

**No test in this plan sleeps or measures elapsed wall-clock time.** Commit `ca2519d`
(*"fix(tests): Signal instead of sleeping in the dispatch-fault tests"*) removed exactly that kind
of test from this project, and revision 2's "one non-property test with a real 50 ms `SettleWindow`"
is **deleted** rather than made deterministic (S15). The only clock left anywhere in the design is
step 14's opt-in coalescing window, which is separable, off by default, and — if it ships — tested
with a test-owned deterministic `TimeProvider` in `Picea.Abies.Tests`, no new dependency.

### The scope clause this needs — **both amendments have landed, and a third has since joined them**

Revision 3 proposed two `00-scope.md` amendments and worded them for the architect. Both are in the
file, verified this revision: **INV-1**'s availability precondition (*"Quantified over histories in
which undo is available; a typed refusal under INV-5 is not a violation of this invariant"*) and
**INV-7**'s definition of a movement (*"A movement is a single undo or redo … the states between a
bracket's hold and its settle are the states passed through"*). The `abies:history:` reservation is
also in place, in **INV-7**'s closing clause, and is **currently empty** — the framework declares no
subscription — so INV-7 holds unqualified and the prefix stays reserved. Step 9 asserts the
emptiness rather than documenting it.

**🟡 3 of the confirmation pass — cited by id and clause rather than by line range, deliberately.**
This
paragraph carried `00-scope.md:132-133`, `:188-192` and `:195-197` until the navigation amendment
lengthened INV-2 and moved every range below it. `00-scope.md` is demonstrably a file that moves,
and its ids exist to make it citable; the id and a quoted clause survive an amendment, a line number
does not. Every citation into `00-scope.md` in this artifact is now of that form.

Nothing in revision 4's nine items asks for a third amendment — the one place a reader might expect
one, open question 3 (*"what happens when the user acts after undoing"*), is a question
`00-scope.md` poses for this pass to **answer**, not a constraint to amend, and § *The default
policy* answers it. **A third amendment has since landed all the same**, from the user and not from
the Critic: `[R4-nav]` required **INV-2** to gain the browser's location as part of the world, and
it is in the file (🟡 2 of the confirmation pass — this section read *"Nothing in revision 4 asks
for a third amendment"* unqualified, which the header at the top of this artifact corrected for the
*"`INV-2` is untouched"* sentence and not for this one).

---

## The default policy — reconsidered, with the Critic's evidence in front of me

The user did not confirm revision 2's identity-lens-with-barriers default, and S9/S10 are why:
under it, undo in Conduit stops at every fetch and every navigation, permanently, and redo does not
refuse — it is silently destroyed. Here is what I weighed.

### The four alternatives

**(A) Revision 2's default — whole-model lens, world changes push an uncrossable `barrier` step,
`Future` cleared.** Undo reach = actions since the world last spoke. Redo dies silently (S9). The
history fills with tombstone steps that can never be crossed and whose retained models can never be
restored — in `SubscriptionsDemo` the 100-entry history is tick tombstones after 25 seconds. Two
defects, one of them the exact opacity B2 was about.

**(B) No default policy at all — delete the type, make `TPolicy` a decision every adopter must
make.** Structurally the cleanest answer to *"what happens to an application that writes no
policy"*: it does not compile. But it is wrong on the merits, because the whole-model lens is not a
fallback — it is **exactly correct** for an application with no world (Counter, a form, a wizard,
the native demo), where every model change arrives through a dispatch and undo is complete. Forcing
those applications to hand-write an identity `Restore`/`SameUndoable` pair is ceremony that teaches
nothing. Rejected, but it is the alternative I came closest to recommending.

**(C) Whole-model lens, world changes `seal` the incident edges instead of pushing a step, and
redo refuses symmetrically. ← recommended.** Detailed below.

**(D) Empty projection by default (`SameUndoable ≡ true`) — nothing is undoable until the
application declares something.** Safe and silent; undo simply never does anything, with no
diagnosis. That is the same class of failure as revision 1's silent `Ok([])`, which the user
already rejected. Rejected.

### (C), in full

Two changes to the classification rule, and one rename.

**1. `seal` replaces `barrier`.** The fourth row of the three-outcome table — a change that arrived
bare and moved the projection — no longer pushes a `Step`. It moves `Present` and **marks the two
edges incident on `Present`**: the top of `Past` and the top of `Future`.

The rule underneath is derivable rather than chosen. An edge between two states is crossable iff,
between them, the application told the world nothing and the world moved nothing. Undo crosses the
backward edge from `Present`; redo crosses the forward edge into `Future`. Anything that happens
*at* `Present` — the application speaks, or the world moves the projection — taints **both**
incident edges, symmetrically. Revision 2 marked only one, which is why redo died silently instead
of refusing.

Consequences, against (A):

- **Reach is identical.** Undo still reaches back exactly as far as the last thing the world did —
  in Conduit, everything since the last server response. I am not claiming to have fixed S10's
  reach, because fixing it would require the framework to guess which state is the user's, and the
  whole point of the lens is that only the application knows.
- **Redo refuses instead of dying.** `Future` is preserved and its first edge is sealed;
  `Redo(h)` returns `BlockedByWorld(cause)` before the press. **S9 closed.**
- **The world retains nothing.** A sealed step's model can never be restored, so revision 2 retained
  it pointlessly. Under `seal` the world's changes push no entries at all. **Qualified since
  revision 4:** `[R4-2]` deliberately retains one thing that can never be restored — a *superseded*
  forward branch — in exchange for being able to name why redo is refused. That is retention for
  something, it is bounded at `Depth`, and it is the user's decision; § *The third edge state* and
  Cleanness 9 carry the cost. The `SubscriptionsDemo` history is still not ticks-only after 25
  seconds, because a tick that is `pass`ed under a projection pushes nothing and a tick that
  `record`s pushes one step like any other action.
- **One fewer `Step`-construction site** — which has a security consequence, flagged under
  § Security.
- **Nothing is lost.** B1's Conduit bio-field scenario still refuses with `BlockedByWorld` and the
  loaded profile is still not discarded.

**2. Both refusals are typed, and the refusal names the message that actually blocked.** Revision 2
carried `StepBarrier = None | SpokeToTheWorld | ChangedOutsideHistory` and then reported
`BlockedByEffect(Past.Peek().Cause)` — the message that *created* the step, not the message that
*sealed* it. With retroactive marking those are different messages, so revision 2 would have named
the wrong one. The seal carries its own cause:

```
EdgeState = Crossable
          | SealedByEffect(Message Cause)          // the application spoke to the world here
          | SealedByWorld(Message Cause)           // the world moved the projection here
          | SupersededByNewAction(Message Cause)   // R4: you acted, so this branch is no longer yours
```

INV-5 requires the carried value to *identify which reason applied*; naming the wrong message is a
misidentification, so this is a correction rather than a refinement.

### `[R4-2]` The third edge state — B9, option (b), and what it does to gate 2

**The finding.** A subscription-delivered message is indistinguishable from a button click at the
wrapper's seam. `Runtime.cs:288-291` is `DispatchFromSubscription(Message) => Dispatch(message)`,
and `Runtime.cs:332` assigns it as the handler registry's dispatch — a DOM event handler and a timer
source enter through the **same private method** into the same public `Dispatch` into
`TProgram.Decide`. So a tick is *enveloped*, moves the projection, and under revision 3
**`record`s and clears `Future`**. In `SubscriptionsDemo` (250 ms `Every`, `Program.cs:110`;
`FastTick` moves the model at `:146-154`) that destroys a walked-back redo branch every quarter
second, silently, and the user cannot tell it from *"there was nothing to redo"*. Revision 3's
default-policy narrative claimed the opposite. The Critic is right, and it is right that this is S9
by another route and B2's harm.

**The user's answer is (b): make the discard typed too.**

```
record: if Future is non-empty:
            Future := Future.ReplaceTop(top with { Edge = SupersededByNewAction(redact(origin)) })
        // the branch is PRESERVED, not cleared
```

`Forward(h)` then returns `BlockedBySupersedingAction(cause)` — computable in advance, from the
value alone, in the same one function `Decide` and `Transition` both route on. *"Redo unavailable —
superseded by `FastTick`"* — **the mechanism's cause, written out to show what the value carries,
and not the sentence a person reads** (`[R4-ux]`: the rendered copy is step 10's template, *"Redo
unavailable — your later action replaced this."*, with `FastTick` kept for telemetry) — is
renderable **before** the press, which is what INV-6 exists for, and distinguishable from INV-4's
no-op, which is what INV-5 exists for. This closes the last silent path
in the design: **every** divergence now refuses with a reason, not only the world's.

**What this does to gate 2, said plainly because it is a change to something settled.** The rule was
*"discard the forward branch on divergence"*, and its reason — Track A's forcing argument, carried
in § *Backing Patterns* — was that a **retained** branch makes redo a *relation* rather than a
function, a relation has no inverse, and INV-3 becomes unstatable. **That argument does not survive
contact with a sealed branch.** It applies to a branch that is retained *and crossable*: two
distinct forward states reachable from one present. A superseded branch is retained and **never
crossable** — `Forward(h)` refuses at its first edge, in both `Decide` and `Transition`, so no
sequence of messages can reach it. Redo remains a partial function; INV-3 quantifies over the case
where redo is available and is unaffected. The rule becomes **"refuse across the superseded
branch"**, and it is a strengthening of gate 2's own instinct — the same instinct that made the user
choose a typed refusal at the effect boundary — applied to the one boundary that was still silent.

**Three costs, none hidden.**

1. **Retention.** A superseded branch's `Step.Model`s are retained but can never be restored. This
   is the "tombstones" objection revision 3 levelled at alternative (A), and it is fair to level it
   here — with two differences that make it much smaller. The retained models are ones the user
   *actually walked back through*, not ones the world manufactured; and they are bounded, because
   **`Future` trims to `TPolicy.Depth` at its far end exactly as `Past` does**. That trim is new in
   revision 4 and necessary: before supersession, `Future` was naturally bounded by how far `Past`
   had been walked; now a superseded branch can sit underneath fresh undo pushes. Step 1's
   `HistoryStack<T>` already has `Trim`; step 6 calls it on both stacks.
2. **Retention is a security surface.** A superseded `Step.Model` is a retained model like any
   other, so it is inside SEC-3(b) and inside `Scrub`'s call sites, unchanged — `record` and
   `StepBack`/`StepForward` already scrub every model they push, and supersession pushes nothing new,
   it only re-labels an edge. **No new `Scrub` site.** Verified against the site rule rather than
   assumed: supersession is a `ReplaceTop` of an `Edge`, not a `Step` construction.
3. **One more `MovementAvailability` case** and one more branch in `Forward(h)`. That is the whole
   implementation cost.

**What (b) does *not* fix, and the user should not be told otherwise.** It does not make redo
survive a background tick. Under `WholeModelHistoryPolicy` a model-mutating subscription still
supersedes the redo branch every time it fires; what changes is that the user is *told*, in advance,
by name. **A projection is still required for redo to function in an application with a
model-mutating subscription** — that sentence carries the same weight as the reach statement, in
step 5's file docs, `06-spec.md`, ADR-030 and the guide. (b) buys diagnosability, not reach. I would
rather say that here than let it be discovered at step 8.

**3. `DefaultHistoryPolicy<TModel>` is renamed `WholeModelHistoryPolicy<TModel>`.** "Default" reads
as "the sensible one" and invites adoption without reading. The type's actual content is a
declaration: *every field of my model is the user's, and anything that moves it without coming
through a dispatch is the world speaking.* An adopter who writes `WholeModelHistoryPolicy<Model>` at
their call site has been told what they signed up for, in the one place they cannot skip. There is
then no type named `Default` to reach for thoughtlessly. One identifier; it carries real
information.

### What "the world" is, under this mechanism — the narrative, rewritten `[R4-2]`

Revision 3's § *default policy* described a mechanism the design does not have. Here is the one it
does have, derived from where each kind of message enters.

**A message reaches `WithHistory.Transition` inside an envelope if and only if it went through
`TProgram.Decide`.** After `[R4-1]` routes the `Err` channel through the envelope too, that is:
every DOM event, every native control event, **every message a subscription delivers**, and every
message an application's own decider accepts *or rejects*. The only messages that arrive **bare**
are (i) the wrapper's own `MovementRefused`, which has its own branch, and (ii) **feedback from a
command the application issued** — `AutomatonRuntime.InterpretEffect` *"interprets an effect and
dispatches any produced feedback events"* (`Picea.xml:1240-1247`), dispatching them as events, so
`Program.Decide` never sees them. Conduit's HTTP responses come back exactly this way
(`Conduit.App/Interpreter.cs:14`).

So, in one sentence, and this is the sentence that goes in step 5's docs, `06-spec.md`, ADR-030 and
the guide:

> **"The world", under `WholeModelHistoryPolicy`, means the feedback from a command the application
> issued — with one exception the same classification table carries: an incoming `UrlChanged` once
> `Origin` is `Established` (`[R4-nav]`, § *Spec obligations* line 16). A message a subscription
> delivers is otherwise treated as a user action: it records an undo stop and supersedes the forward
> branch. A decision the application rejects is the application's own response to the user's own
> message, and is classified exactly like one it accepts.**

**S30, confirmation pass — why the exception has to be in the sentence and not beside it.** Until
this amendment the sentence ended *"and nothing else"*, and on the browser head that is now false in
the most direct way available: an incoming `UrlChanged` **is** a subscription delivery — it arrives
through `Navigation.UrlChanges`, i.e. `DispatchFromSubscription` — so *"a subscription-delivered
message is a user action"* and *"incoming navigation is the world"* classify the same message on the
same path two different ways. The exception is a row of the classification table, not a caveat about
it, so it travels with the sentence wherever the sentence goes.

**What an application that adopts `WithHistory` and writes no policy actually gets:**

- Undo and redo that work completely, for everything since the last command feedback. For an
  application with no commands — a form, a wizard, Counter, the native demo — that is the whole
  session, and undo is complete.
- At the point feedback last arrived: **both** directions refuse, both typed, both answerable from
  the history value **before** the press (INV-6), both carrying the message that blocked them —
  *"`BlockedByWorld(ProfileLoaded)`"*, which is what the **value** says. What the **chrome renders**
  is step 10's template with the cause humanized: *"Undo unavailable — the page finished loading."*
  (`[R4-ux]`).
- If the user acts after undoing — including when the actor is a timer — the forward branch is
  **preserved and sealed**, and redo refuses carrying the superseding message —
  *"`BlockedBySupersedingAction(FastTick)`"* in the value, rendered as *"Redo unavailable — your
  later action replaced this."* (`[R4-ux]`; the identifier is the mechanism's cause and stays in
  telemetry, never in the document). Nothing is silently destroyed anywhere in the design.
- **On a `SubscriptionsDemo`-shaped application — a timer that moves the model — redo does not
  function.** Not "reach collapses to roughly one tick": every tick records a step and supersedes
  whatever branch the user was walking, so a user who presses undo three times and pauses finds redo
  refused, by name, within 250 ms. Undo itself still works and still reaches back through the ticks.
  **The fix is a projection that excludes the tick-derived fields** — two lines — after which a tick
  is `SameUndoable` → `pass`, the branch survives, and redo behaves. That makes a projection a
  **precondition for redo functioning at all** in any application with a model-mutating
  subscription, not *"how the feature becomes usable"*. The guide says so in those words, beside the
  reach statement and with the same weight.

**This is the honest ceiling.** A framework that has been told nothing about which state is the
user's cannot tell the user's action from a timer's, because they arrive through one delegate, and
cannot reach further back than the last time a command answered. Every alternative that reaches
further does so by guessing — and the one alternative that would not guess, a runtime seam tagging
subscription-origin messages (the Critic's option (c)), fires the
`runtime-seams-anchor-replay-gating` revisit trigger, is an architecture decision rather than a plan
detail, and is not recommended by the Critic or by me.

**The reach statement has three bounds, and every place that states it states all three.** Undo's
reach under the whole-model policy is bounded by (i) **the last command feedback** (S10); (ii)
**`Depth` ÷ the subscription's rate, in wall-clock seconds**, where a model-mutating subscription
runs under the default policy — in `SubscriptionsDemo`'s shape, four silent ticks a second against a
depth of 100, so twenty-five seconds (S25(b)); and (iii) **the last incoming navigation**
(`[R4-nav]`), which in a routed application is usually the binding one — in Conduit, undo reaches
back to the current page and no further. Stating one or two of the three understates the limit an
adopter is accepting, so the three travel together: § *Spec obligations* line 5, step 5's file docs
(iv), step 13's brief, and the guide.

Four things are therefore first-class statements rather than footnotes, in `06-spec.md`, ADR-030,
step 5's file docs and the guide: the **reach** bound with all three of its parts (S10, S25(b),
`[R4-nav]`), the **projection-is-required-for-redo** bound (B9), the fact that a **rejected decision
is not the world** (B8), and that **incoming navigation is the world** — with the application
obligation that comes with it (B10, § *The lens, and its six obligations*). `ux-expert` q7 is
re-asked as the **common** case rather than the exceptional one, and gains **q13** on whether the
superseded-branch refusal should read differently from the world's.

---

## The classification rule, made total (S11)

Revision 2 called the three-outcome table *"a total function over the two questions"* and the
implementing pseudo-code was not: `HistoryEvent.Continue` was unconditionally transparent, so a
projection change carried by the **second** decided event of one message was neither recorded nor
sealed. The Critic's evidence is real and in the repository:
`Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs:218` decides
`Ok([new BeginSlowWorkflow(), new SlowObserved()])`, where the first returns `(model, new
DelayCommand())` (`:209`) and the second increments the model (`:210`). Multi-event `Decide` is a
decision-recorded capability (*2026-04-04 `Program.Decide` return type is `Result<Message[],
Message>`*).

**Fix: the envelope carries the whole decision as one event.**

```
HistoryEvent.Enveloped(Message Origin, IReadOnlyList<Message> Inner) : Message
```

`Decide` wraps `TProgram.Decide(h.Present, m)`'s entire event array in **one** `Enveloped`, carrying
the incoming message as `Origin`. `Transition` folds `TProgram.Transition` over `Inner`, batching
the commands with `Commands.Batch` (`Command.cs:14`). The classification then runs **once per
incoming message**, on the net `(h.Present → final, batched command)`:

| Came through the envelope? | Moved the undoable projection? | Treatment |
|---|---|---|
| yes | yes | **record** — push a crossable `Step`; **supersede** a non-empty `Future` (`[R4-2]`) |
| yes | no | **pass** — move `Present`, record nothing, keep `Future` |
| no | no | **pass** — same |
| no | yes | **seal** — move `Present`, push nothing, seal both incident edges |

**`[R4-nav]` One override precedes this table, and it lives beside the origin re-basing rule in
`apply` (§ *The wrapper*).** After the session's first recorded step, a transition whose origin is
`UrlChanged` **seals** — `origin is UrlChanged && Origin is Established -> seal,
SealedByWorld(UrlChanged)` — regardless of which row it would otherwise take. Both incident edges
are marked, so `Backward(h)` and `Forward(h)` both refuse with the navigation as the cause. The
table above stays total over the messages that reach it; incoming navigation is classified before
it does.

Total by construction, not by inspection. `HistoryEvent.Step`/`Continue` are **deleted** — which
also removes a name collision revision 2 shipped, `HistoryEvent.Step` beside `Step<TModel>` in the
same namespace (S17's cousin).

**`[R4-1]` Which real messages answer *no* to the first column — B8.** Revision 3 never stated it,
and the answer it implied was wrong. `Runtime.Dispatch` treats the `Err` channel as a message to
**dispatch**, not as an event to decide: `decidedError = decision.Error` (`Runtime.cs:460`) then
`await _core.Dispatch(decidedError, …)` (`:484`), and `_core.Dispatch` runs *"transition, observer,
and interpreter pipelines"* (`Picea.xml:1229-1232`) — `Program.Decide` is not among them. So under
revision 3 an application's own **validation rejection** reached `Transition` bare, moved the model,
and fell to **`seal`** → `SealedByWorld(ValidationRejected)`: the first rejected form submission
blocked undo *and* redo permanently and told the user *"the page changed"*. That is a
misclassification with a permanent consequence, condemned by revision 3's own standard for naming
the wrong message, and it lands on a decision-recorded, shipped-test-harness capability
(*2026-04-04 `Program.Decide` return type is `Result<Message[], Message>`*;
`RuntimeIsolationAndSubscriptionFaultTests.cs:185-190`, `TestHarnessTests.cs:302`,
`TestHarnessReplayDiagnosticsTests.cs:185`, `TestHarnessReplayBenchmarks.cs:101`).

**The fix is one line in the delegating branch of `Decide`, and the fold already built the vehicle:**

```
Err(e) -> Err(HistoryEvent.Enveloped(m, [e]))     // was: Err(e)
```

`Runtime.Dispatch` dispatches whatever `Message` sits in the `Err` channel, and `apply` unwraps
`Enveloped` without caring which channel delivered it. A rejected decision then classifies exactly
like an accepted one — `record` if it moved the projection, `pass` if not — **which is the correct
reading, because a rejection is the application's own response to the user's own message.** Two
things this deliberately does **not** touch: the wrapper's own `Err(MovementRefused …)` from the
`Undo`/`Redo` branches is **not** enveloped (it has its own branch in `apply` and must stay bare),
and the fold over `[e]` is a one-element fold, so the model and command an unwrapped program would
produce are produced identically. The rejected alternative — a third `SealedByDecision(Cause)` edge
state — is **not taken**: it would make a rejected form submission a permanent barrier, which is not
what a rejection means, and the user's decision 2 says so directly.

Step 8 gains property **(j)**: *a decision that is rejected and whose error message moves the
projection produces the same classification as an accepted decision that moves it by the same
amount.*

**`[R4-4]` The fold changes error semantics as well as ordering — S18, accepted and stated.** The
Critic asked for this to be attacked and it survives, with one addition the user is owed.
`InterpretCommand`'s batch case returns on the first error — `if (result.IsErr) return result;`
(`Runtime.cs:359-360`) — discarding the remaining sub-commands. For a two-event decision where
`c1` errs:

- **Unwrapped today:** `_core.Dispatch(e1)` transitions, observes, interprets `c1`, fails;
  `Runtime.Dispatch` returns the error at `:495-498` and **`e2` is never applied**. Model: `e1`
  only. Effects: none after the failure.
- **Wrapped, under the fold:** both `e1` and `e2` are folded in `Transition` before any
  interpretation, so the model has **both**; then `Batch([c1, c2])` aborts at `c1` and `c2` never
  runs.

The fold converts *"partial model, no later effects"* into *"complete model, partial effects"*. I
judge the second more correct for the same reason the ordering change is more correct — the events
of one decision describe **one** state change, and a command failing downstream of it is not a
reason to have applied half of it — but "more correct" is my judgement and the difference is
observable, so it is a **spec line beside the interleaving one** in `06-spec.md` and an addition to
step 6's Done-when, not a footnote. It is also in the pause question, because the user accepted the
ordering half of this consequence and was not told the failure half.

**Three things this fold changes, stated rather than absorbed.**

1. **One incoming message renders once instead of N times.** `Observe` calls `Render` per decided
   event (`Runtime.cs:163-168`), so today a two-event decision renders and reconciles twice. Under
   the fold it renders once. This is a small performance improvement and it removes intermediate
   subscription reconciliations from the ordinary dispatch path.
2. **Effect interleaving changes for multi-event decisions.** Today `_core.Dispatch` is awaited per
   event (`Runtime.cs:492-499`), so event 1's command is interpreted — and its feedback dispatched
   recursively — *before* event 2 is applied. Under the fold, all events are applied, then
   `Command.Batch` is interpreted in order (`Runtime.cs:353-365` preserves order and collects all
   feedback), then feedback is dispatched. **This is a behavioural difference between a wrapped and
   an unwrapped program, and it is observable only for multi-event decisions.** I judge it correct
   rather than merely acceptable: in the decider shape the events of one decision describe one
   atomic state change, and interleaving effects between them is an artifact of `Dispatch`'s loop
   rather than a contract. It is a **spec line** in `06-spec.md`, not a footnote, and it is the one
   thing in this revision I most want the Critic to attack.
3. **The alternatives I rejected.** (i) Tracking "a step is open for this message" as a flag on
   `History` — reachable race under the B4 interleave (two enveloped messages decided before either
   transitions fold into one entry, falsifying INV-1). (ii) An explicit boundary event before each
   envelope — race-free, but it costs an extra `_core.Dispatch` and therefore an extra `Render` on
   **every** user action, forever. (iii) Letting a later event record its own step — one message
   yields two undo stops, which falsifies INV-1 as written. The fold is the only one of the four
   that is both race-free and free.

**`IsUndoable` is removed from the policy (S12).** The Critic is right that its entire remaining
effect was to turn a `record` into an uncrossable barrier — a knob whose every non-default use kills
undo, named as though it enabled it. The lens subsumes the question it was asked, which is gate-3
decision 1's whole point. One fewer member, one fewer footgun, one fewer conjunct in `apply`, and
the security room's argument about using it for passwords becomes moot rather than merely
discouraged.

---

## The lens, and its six obligations

The erased form is unchanged and accepted at gate 3:

```
static abstract TModel Restore(TModel remembered, TModel current);   // = put(get(remembered), current)
static abstract bool   SameUndoable(TModel a, TModel b);             // = get(a) == get(b)
static abstract TModel Scrub(TModel model);                          // NEW — decision 2
```

`WholeModelHistoryPolicy<TModel>` supplies `Restore(r, _) => r`,
`SameUndoable(a, b) => EqualityComparer<TModel>.Default.Equals(a, b)`, `Scrub(m) => m`.

**The obligation set, renumbered once and never again.** The user's decision 2 names the `Scrub` law
**L5**; S13's equivalence-relation law therefore becomes **L6**. Both numbers are fixed here so
`06-spec.md` and `05-critic.md` cannot drift.

- **L1 (GetPut):** `Restore(m, m) = m`
- **L2 (PutGet):** `SameUndoable(Restore(a, b), a)`
- **L3 (PutPut):** `Restore(a, Restore(b, c)) = Restore(a, c)`
- **L4 (coherence):** `SameUndoable(a, b) ⟺ Restore(a, b) = b` — a theorem in A3's un-erased form,
  an axiom after the erasure
- **L5 (scrub safety, decision 2):** `Restore(Scrub(a), b) = Restore(a, b)` — scrubbing may only
  change state outside the projection. Under L1/L2 this forces `get(Scrub(a)) = get(a)`, which is
  the security room's own derivation (`room-security.md:339-345`)
- **L6 (S13):** `SameUndoable` is an **equivalence relation** — reflexive, symmetric, transitive

**Why L6 is load-bearing and not pedantry.** In A3's un-erased form `SameUndoable(a,b)` *is*
`get(a) == get(b)`, so the three properties come free from equality on `TUndoable`. After the
erasure they must be axioms, and L1–L4 do not supply them: reflexivity follows from L4 via L1, but
**symmetry does not** — `Restore(a,b) = b` and `Restore(b,a) = a` are independent statements once
there is no `get` to factor through. `apply` calls `SameUndoable(next, h.Present)` while INV-3's
round-trip argument reasons about the remembered model against the present one; an asymmetric
`SameUndoable` satisfying L1–L5 makes the record/pass decision order-dependent. Three generated
triples in step 8; cheap.

INV-3's round trip remains a *derivation* rather than an assertion:
`Restore(P, Restore(s.Model, P)) = Restore(P, P)` by L3, `= P` by L1.

### `[R4-nav]` A second application obligation, stated here because it is the same kind of thing — B10

L1–L6 are obligations the framework states and cannot discharge for the adopter; Cleanness 1 already
records a residual of exactly this shape (independent reachability, not property-testable by the
framework). The navigation amendment adds one more, and it belongs beside them rather than buried in
a classification rule:

> **An application that routes the browser's location through its own message type must classify
> that message as a commitment itself; the framework cannot see it.** The `[R4-nav]` guarantee —
> that undo never lands on a model whose URL is not the one the browser is showing — holds for
> incoming navigation delivered as `Picea.Abies.UrlChanged`. Mapping incoming navigation to that
> message is the **application's** obligation, in the same register as L1–L6 and the
> independent-reachability assumption.

**Why it cannot be discharged framework-side, in one line.** `Navigation.UrlChanges(Func<Url,
Message> toMessage)` (`Navigation.cs:17-29`) is the only assignment of `OnUrlChange`, and the
application supplies the constructor; `WithHistory` sees an enveloped message of a type it was never
told about. `apply`'s rule tests `origin is UrlChanged`, so an application-named navigation message
takes the `record` row and the defect the amendment exists to remove survives — silently, with every
step-8 property green, because the spec's fixture dispatches the framework's `UrlChanged`. The
server head is unaffected (`Session.cs:271` dispatches `new UrlChanged(url)` itself); the hole is
exactly the head where browser Back matters most.

**And this repository hands an adopter the shape that defeats it.** `README.md:183-190` — the
front-page example — declares `public record UrlChangedTo(Url Url) : Message` and passes
`url => new UrlChangedTo(url)`, while `Conduit.App/Conduit.cs:198` and every tutorial pass
`url => new UrlChanged(url)`. The README example is not itself a `WithHistory` adopter, so it is not
wrong today; it is the idiom a reader copies. Step 13 carries both halves: the obligation in the
guide and ADR-030, and the README brought into line with the tutorials so the two idioms in this
repository stop disagreeing.

**What is deliberately not done.** A policy predicate, an analyzer rule or a framework-owned wrapper
around `UrlChanges` would each turn the obligation into a mechanism. All three are new mechanism,
one reverses a gate-4 deletion, and none is needed to *state the truth*. The analyzer is filed as a
**fast-follow candidate** in the same register as `[R4-6]`'s serialization-boundary substitution,
not as work in this pass.

---

## Security — SEC-1 … SEC-7, with the 2026-09-07 follow-up folded in

The room's second section answers B7. Its net effect is folded below, with the two places where
this revision's other decisions changed the shape flagged rather than silently adjusted.

| # | Requirement | Owner | Lands in |
|---|---|---|---|
| **SEC-1** | **`SensitiveCause : Message`** marker in `Picea.Abies.History` (no `I` prefix — gate-4 decision 5; revision 2 said `ISensitiveCause` in seven places and this revision says it in none), plus `HistoryRedacted(string OriginalTypeName) : Message`. Marker idiom, no attribute, no reflection, trim/AOT-safe. | csharp-dev | steps 2, 4 |
| **SEC-2** | Redaction at record time. Wherever `WithHistory` stores a `Cause`, `cause is SensitiveCause ⇒ store new HistoryRedacted(cause.GetType().Name)`. Checked **unconditionally by the wrapper**, never gated by `TPolicy`. | csharp-dev | step 6 |
| **SEC-3** | **Split into two clauses that do not subsume one another** — see below, now with clause (b)'s **joint-lawfulness precondition** (`[R4-5]`). Two named regression tests, plus a third for the held anchor (`[R4-6]`) that is explicitly **not** part of clause (b). | csharp-dev | step 8 |
| **SEC-4** | Telemetry backstop: spans record `Cause.GetType().Name` and never `Cause` itself or any serialised form. | csharp-dev | step 11 |
| **SEC-5** | Guide states the marker **and `Scrub`, in the same worked example as the projection** (`room-security.md:380-385`) — not as a follow-on paragraph. Uses `LoginPasswordChanged` / `RegisterPasswordChanged` / `SettingsPasswordChanged` (`Conduit.App/Messages.cs:12,17,29`) and the `SettingsModel.Password` field (`Conduit.App/Model.cs:94-96`). States plainly that the whole-model policy retains payloads verbatim unless marked, and carries the corrected DEBUG `JsonPolymorphic` framing — which **names `HistoryEvent.Enveloped` as well as `HistoryRedacted`** (🟡 4, `[R4-9]`): `Enveloped` is a `Message` that reaches `_core.Dispatch` carrying `IReadOnlyList<Message> Inner`, so under the DEBUG snapshot path an application declaring polymorphic metadata must cover it, and covering it means the **inner** messages serialise structurally. `Enveloped` is the type that carries the payloads. **Adds `[R4-5]`'s negative form** (override `Scrub` alone, and the L5 test that catches it) directly under the worked example, and **`[R4-6]`'s one sentence** on the unscrubbed anchor in the chrome-contract section. | tech-writer | step 13 |
| **SEC-6** | Threat-model items: two *Threats and Mitigations* rows split High/server-side and Medium/client-only, one *Open Risks* entry for the un-retrofitted `SerializeMessageArgs` leak, and **Trust Boundary 7 — Runtime state-retention boundary**. The rows must say explicitly that `Scrub` and `SensitiveCause` are **independent** mitigations of **different** fields, neither a superset of the other (`room-security.md:447-449`). **`[R4-6]`:** Trust Boundary 7 gains the clause *"…or a bounded, unscrubbed live model retained in `Movement.Held(Anchor)` for the duration of a bracket"*, and the **existing** DEBUG-snapshot row gains a second bullet naming `Movement.Held(Anchor)` as a distinct reachable site — same severity, same "⚠️ Partially mitigated" status, **not a new row** (`room-security.md:602-612`). | security-expert | step 16 |
| **SEC-7** | The pre-existing DEBUG-only `SerializeMessageArgs` `ToString()` leak (`DebuggerMachine.cs:408-422`) is out of scope but not left without a ticket: decision-log item at close-out plus a `hardening-backlog.md` entry. **`[R4-6]` fast-follow, logged in the same register:** a serialization-boundary substitution for `Held<TModel>`, which is the only mechanism that would close the anchor's DEBUG-snapshot exposure properly and which both the Critic and the room declined to build mid-loop. | architect / security-expert | close-out, step 16 |

### `Scrub`, and the call-site count

```
static abstract TModel Scrub(TModel model);     // default: identity
```

**The rule is: `Scrub` runs at every site that constructs a `Step` from a live model.** The room's
follow-up enumerates four such sites under revision 2's shape — `record`, `barrier`, `StepBack`'s
`Future.Push`, `StepForward`'s `Past.Push` (`room-security.md:461-462`) — and it is right that
revision 2's `StepBack`/`StepForward` were the miss that made B7's shape repeat: a Conduit user with
a partial password typed into Settings who presses undo to back out an unrelated bio edit puts
`h.Present`, password and all, into a fresh `Future` step.

**Under this revision there are three sites, not four, and I am flagging it rather than absorbing
it.** `seal` replaces `barrier` (§ *The default policy*) and pushes no `Step` at all, so there is no
model to scrub there. The three live sites are **`record`, `StepBack`'s `Future.Push`, and
`StepForward`'s `Past.Push`**. If the user rejects `seal` and keeps `barrier`, the fourth site
returns and `Scrub` runs there too — the rule is stated over sites, not as a list of four, precisely
so it cannot go stale again. I would rather say this out loud than have `csharp-dev` implement from
a list of four and discover one of them does not exist.

### `[R4-5]` `Scrub` is conditional on a projection — S19, and the answer the room revised

**The finding, and it is about the default the whole design ships with.** L5 is
`Restore(Scrub(a), b) = Restore(a, b)` for all `b`. `WholeModelHistoryPolicy` supplies
`Restore(r, _) => r`. Substitute: **`Scrub(a) = a`**. Under the whole-model policy `Scrub` is
provably the identity — not by convention, by the law step 8(f) is about to property-test.

The failure mode is not the framework violating the law; it is an adopter **following SEC-5**. The
guide says *"mark your sensitive messages, and scrub your model."* An adopter with a
`SettingsModel.Password` derives from the whole-model defaults and overrides only `Scrub`:

```
Scrub(m) => m with { Settings = m.Settings with { Password = "" } };
```

`record` stores `Scrub(P)`; undo computes `Restore(Scrub(M), P)` = `Scrub(M)` under the identity
lens = **`M` with the password blanked**. Pressing undo to back out an unrelated edit **erases the
password the user is in the middle of typing**, in both directions, silently. It is an INV-3
violation, and the adopter got there by following the security guidance. Nothing in the plan
enforces L1–L6 in an *adopting* application — the property tests live in `Picea.Abies.Tests` over
the framework's own policies, and § Cleanness 1 correctly records the laws as the application's
obligation. So this ships as a trap unless it is written down.

**The answer, three sentences and one test, per `room-security.md:486-553`.** The room checked
whether a compile-time shape was available and ruled it out for a stated reason: making "override
one, override all" a compile-time fact means folding the three members back into A3's un-erased
`get`/`put` pair, which is exactly what gate-3 decision 1 traded away. Not reopening that.

1. **Step 5's file documentation** states, beside the existing sentence about verbatim payload
   retention: *`Scrub` is only meaningful in the presence of a non-identity projection; under the
   whole-model lens L5 forces it to the identity, and overriding it alone is unlawful.*
2. **SEC-5's worked example** (step 13) shows the negative form directly under the positive one —
   override `Scrub` alone, and the L5 test that catches it — so the trap and its detector are in one
   reading rather than in two documents.
3. **Step 5's Done-when** adds: *a policy that overrides `Scrub` without overriding
   `Restore`/`SameUndoable` fails L5.* That turns the trap into a red test.

The test is **`HistoryLensLawTests.Scrub_overridden_alone_violates_L5`** and it belongs in step 8
beside L1–L6, **not** in `HistorySecurityRegressionTests`. The room is firm about why and I agree:
it proves a property of the algebra (an inconsistent triple is L5-unsatisfiable), not that a payload
leaked, and giving `Scrub_never_retains_original_payload` a second job is how the next version of
this exact gap gets missed. It is also the route by which the rule becomes self-enforcing *outside*
this repository — an adopter who points the framework's L1–L6 harness at their own policy hits it —
and step 5's docs say so.

**Mandatory vs. default-identity, revised.** The room's first follow-up said *"default to identity,
weight it in the guide"*. Its second follow-up replaces that with the Critic's own reading:
**neither — `Scrub` is conditional on a non-identity projection.** The framework still cannot detect
"an application with a credential field" at compile time and a name heuristic still belongs in a
scanner rule, so the *member* keeps its identity default; what changed is that the guide must state
the condition, next to a test that fails when it is not met.

### SEC-3, restated — two clauses, neither subsuming the other

Revision 2 dropped the room's `Step.Cause` qualifier and produced a property that is **false** for
any realistic test model. Restated, in the room's own words at `room-security.md:400-416`:

> For any application under test, including at least one generated test-model shape carrying a field
> the application's own `Scrub` is defined to remove (a Conduit-Settings-shaped model with a live
> `Password` excluded from the projection is the canonical case, and the generator corpus must
> include it), no generated `History` value's `Past`/`Future` exposes:
> **(a)** the original payload of a message implementing `SensitiveCause`, reachable via any
> `Step.Cause` — by direct inspection, by `.ToString()`, or through the serializer the composition
> test or the DEBUG snapshot path would apply; **discharged by** SEC-1/SEC-2's `redact()` at every
> cause-recording site;
> **(b)** any field value the model's `Scrub` is defined to remove, reachable via any `Step.Model` —
> same three reachability modes; **discharged by** `Scrub` at every `Step`-construction site,
> together with **L5**.
>
> **Precondition on (b) (`[R4-5]`, `room-security.md:506-511`).** Clause (b)'s guarantee holds
> **only when the policy's `Restore`/`SameUndoable`/`Scrub` triple is jointly lawful (L1–L6)**. A
> policy that overrides `Scrub` without overriding `Restore`/`SameUndoable` to match is not covered
> by this clause; under the whole-model policy specifically, L5 forces `Scrub` to the identity, and
> overriding it alone **inverts** the guarantee — undo then deletes the field clause (b) exists to
> protect.
>
> **Clause (b)'s reachability set is `Step.Model` in `Past` and `Future`, and is deliberately not
> widened (`[R4-6]`).** The Critic proposed rewording it to quantify over *every* model a `History`
> value retains, naming `Movement.Held(Anchor)` — and deferred the anchor's treatment to
> `security-expert`, which **declines the rewording** (`room-security.md:613-620`): the anchor is
> deliberately not scrubbed, so naming it in a clause discharged by `Scrub` would make the clause
> false the instant it is adopted. That is the same shape of mistake — a mitigation aimed at one
> route to a value — that S20 itself is about. The anchor gets its own guarantee below.

Both required. Two tests, named for the threats:

- `HistorySecurityRegressionTests.SensitiveCause_never_retains_original_payload` — clause (a).
- `HistorySecurityRegressionTests.Scrub_never_retains_original_payload` — clause (b): a
  Conduit-Settings-shaped fixture with a live password, **undo-then-redo through `StepBack` and
  `StepForward`**, inspecting every retained `Step.Model` in both `Past` **and `Future`**. This is
  the test that would have caught B7 mechanically.

**L5's property must exercise the redo-direction push** (the user's decision 2, and
`room-security.md:368-370`): its generated sequences must be long enough to run `StepForward`, not
only `record` in isolation, or the property can pass while the site is still open. That is written
into step 8's acceptance criterion, not left to the implementer.

### `[R4-6]` `Movement.Held(Anchor)` — the third occurrence of one shape, answered at the shape

**The finding.** `Held(TModel Anchor)` captures `h.Present` when `Hold` is dispatched and holds it
for the whole bracket. It is not a `Step`, so the rule *"`Scrub` runs at every site that constructs
a `Step` from a live model"* does not reach it, and clause (b)'s predicate does not quantify over
it. A Conduit-Settings-shaped fixture with a live password passes
`Scrub_never_retains_original_payload` while the password sits in `h.Movement`. The Critic is right
that this is the **third** time in this pass a mitigation has been aimed at one route to the same
value — `Step.Cause`, then `Step.Model`, now `Movement.Held` — and right that the lesson is not
"add a fourth site".

**The room's answer is: neither scrub it nor make it unreachable, this pass. Name it, bound it, and
prove the boundary with a test.** Both rejections have reasons, not preferences:

- **Scrubbing the anchor is actively wrong, not merely unenforceable.** The anchor's only job is to
  answer `TProgram.Subscriptions(anchor)`, and `Subscriptions` is derived from the **whole** model by
  design. `Subscriptions(Scrub(m))` may differ from `Subscriptions(m)`, which produces exactly the
  *"declares a source neither the anchor nor the destination declares"* failure step 9's assertion 2
  exists to catch. Scrubbing the anchor would break INV-7's mechanism to close a smaller exposure.
- **A serialization-boundary substitute is a new mechanism**, and this loop is closed to new
  mechanism. Logged as a fast-follow under SEC-7's register instead.

**What is available, and why it is enough here.** `Step.Model` is retained for up to `Depth` (100)
subsequent dispatches. `Movement.Held(Anchor)` is retained for the life of **one bracket** — and
brackets are opt-in, so the default chrome never creates one. The bound is the same chrome contract
the plan already states for liveness, now with `blur`, `pointercancel` and `Clear` as its in-band
closers (`[R4-8c]`). That is a real bound, already written down for another reason.

**Four places it lands, none of them clause (b):**

1. **Trust Boundary 7** gains one clause naming the anchor (SEC-6, step 16).
2. **The existing DEBUG-snapshot threat-model row** gains a second bullet naming
   `Movement.Held(Anchor)` as a distinct reachable site — distinct because it is reached through
   `History<TModel>.Movement` rather than `Past`/`Future`, and distinct because it is not, and
   cannot safely be, covered by `Scrub`. Same severity, same status, not a new row (step 16).
3. **A third, separately named test** in step 8:
   `HistorySecurityRegressionTests.Anchor_never_reaches_a_release_path_surface` — over a `Held`
   bracket with the same Conduit-Settings fixture, the anchor's payload appears in **none** of: the
   rendered `View` output (only `h.Present` is ever passed to `TProgram.View`), any
   `HistoryTelemetry` span or tag (SEC-4's type-name-only discipline, extended explicitly to the
   fact that `Movement` is never tagged at all, not only `Cause`), or any
   `MovementAvailability`/`EdgeState` returned by `Backward`/`Forward`. **This does not prove the
   anchor is safe** — it is not, under the DEBUG snapshot path, and the threat-model row says so. It
   proves the only leak this pass tolerates is the one named in that row, and closes off the fourth
   and fifth routes before someone finds them the way these three were found.
4. **One guide sentence** (step 13), in the chrome-contract section that already exists for S24:
   *unlike a recorded `Step`, the anchor held during a bracket is never scrubbed — closing the
   bracket promptly is a data-minimisation practice, not only a liveness one.*

**Both mechanisms are kept, and the record says why.** `Scrub` has no reach into `Step.Cause` — a
`LoginSubmit(Email, Password)`-shaped message that seals the history is retained verbatim as a cause
regardless of how well `Scrub` redacts the model. `SensitiveCause` has no reach into `Step.Model` —
which is B7 exactly: the next recordable step is opened by an *unrelated* message whose cause was
never sensitive, and the live password rides along in the model. Different fields, different
sources, different policy questions, independent failure modes.

**The note the user is owed, restated because it belongs on the record and not only in the
Critic's file.** The erased lens accepted at gate 3 kept `WithHistory` at four type parameters; A3's
un-erased form, where a `Step` stores `TUndoable` rather than `TModel`, would have made B7
structurally impossible, because the complement — and every secret in it — would never have been
retained. I am not reopening the erasure; `Scrub` + L5 closes the gap at one policy member. But the
erasure had a security cost that was not visible when the decision was made.

`HistoryRedacted(string OriginalTypeName)` in place of the room's original `Type`-valued property is
**confirmed** by the room (`room-security.md:451-456`). No longer a deviation.

---

## Architecture Sketch

One new bounded context, entirely inside `Picea.Abies`, opt-in by composition. **Zero lines of
`Runtime.cs`, `Program.cs`, any head adapter, or any `.js` file change.**

```
Picea.Abies.History
├── HistoryStack<T>          internal immutable bounded stack (array copy-on-write)
├── History<TModel>          (Past, Present, Future, Movement, Origin)
├── Step<TModel>             (Model, Cause, Edge, AtTicks)
├── EdgeState                Crossable | SealedByEffect(Cause) | SealedByWorld(Cause)
│                            | SupersededByNewAction(Cause)                      // R4-2
├── Movement                 Settled | Held(Anchor)
├── Origin                   Fresh | Established
├── MovementAvailability     Available(int Depth) | Nothing                      // R4-9 (🟡 1)
│                            | BlockedByEffect(Cause) | BlockedByWorld(Cause)
│                            | BlockedBySupersedingAction(Cause)                 // R4-2
├── Direction                Backward | Forward
├── HistoryMessage           Undo | Redo | Hold | Settle | Clear            : Message
├── MovementRefused(Direction, MovementAvailability)                        : Message
├── HistoryEvent.Enveloped(Message Origin, IReadOnlyList<Message> Inner)    : Message
├── SensitiveCause                                                          : Message   (SEC-1)
├── HistoryRedacted(string OriginalTypeName)                                : Message   (SEC-1)
├── HistoryPolicy<TModel>          Depth, Restore, SameUndoable, Scrub
├── WholeModelHistoryPolicy<TModel>  Depth 100, identity lens, identity scrub
└── WithHistory<TProgram, TPolicy, TModel, TArgument> : Program<History<TModel>, TArgument>
```

Gone since revision 2: `HistorySettleSource`, `StepBarrier`, `UndoRefused`, `HistoryEvent.Step`,
`HistoryEvent.Continue`, `TPolicy.IsUndoable`, `TPolicy.SettleWindow`, `DefaultHistoryPolicy`, and
`TPolicy.Time` on the default path (it returns only with step 14, if step 14 ships).

**The wrapper, in full.**

```
Initialize(a)     = let (m, c) = TProgram.Initialize(a) in (History.Start(m), c)
View(h)           = TProgram.View(h.Present)
IsTerminal(h)     = TProgram.IsTerminal(h.Present) && h.Past.IsEmpty && h.Future.IsEmpty
Subscriptions(h)  = h.Movement is Held(a) ? TProgram.Subscriptions(a)
                                          : TProgram.Subscriptions(h.Present)

Decide(h, Undo)   = Backward(h) switch
                    { Nothing            -> Ok([])                              // INV-4
                    , blocked r          -> Err(MovementRefused(Backward, r))   // INV-5
                    , Available _        -> Ok([Undo]) }
                    // "blocked" = BlockedByEffect | BlockedByWorld
                    //           | BlockedBySupersedingAction                   // R4-2
Decide(h, Redo)   = Forward(h) switch { ... mirror image ... }                  // S9, R4-2
Decide(h, Hold)   = h.Movement is Held    ? Ok([]) : Ok([Hold])
Decide(h, Settle) = h.Movement is Settled ? Ok([]) : Ok([Settle])               // B6 moot
Decide(h, Clear)  = (h.Past.IsEmpty && h.Future.IsEmpty) ? Ok([]) : Ok([Clear])
Decide(h, m) when TProgram.IsTerminal(h.Present) = Ok([])
Decide(h, m)      = TProgram.Decide(h.Present, m) match
                    { Ok([])     -> Ok([])
                    , Ok(events) -> Ok([ HistoryEvent.Enveloped(m, events) ])   // S11
                    , Err(e)     -> Err(HistoryEvent.Enveloped(m, [e])) }      // R4-1 (B8)
                    // Err's payload is one Message, not an array — Result<Message[], Message>
                    // the wrapper's OWN Err(MovementRefused …) above is NOT enveloped

Transition(h, Undo)   = recompute Backward(h):
                          Available -> (h.StepBack(),  Commands.None)
                          Nothing   -> (h,             Commands.None)           // B4
                          blocked r -> delegate MovementRefused(Backward, r)
Transition(h, Redo)   = mirror image                                            // B4
Transition(h, Hold)   = (h.Movement is Held ? h : h with { Movement = Held(h.Present) }, None)
Transition(h, Settle) = (h with { Movement = Settled }, Commands.None)
Transition(h, Clear)  = h.Past.IsEmpty && h.Future.IsEmpty
                          ? (h, Commands.None)                                  // B4
                          : (h.Cleared() with { Movement = Settled }, Commands.None)
Transition(h, e)      = apply(h, e)        // NO rule 2: an ordinary event does not end a run
```

```
apply(h, e):
    (origin, inners, enveloped) = e is Enveloped en ? (en.Origin, en.Inner, true)
                                                    : (e,        [e],       false)
    (next, cmds) = fold TProgram.Transition over inners from h.Present
    cmd          = cmds.Count == 1 ? cmds[0] : Commands.Batch(cmds)
    if ReferenceEquals(next, h.Present) && IsSilent(cmd)   -> h                 // true no-op
    if e is MovementRefused                                -> pass(h, next, cmd, origin)   // S7
    if h.Origin is Fresh && origin is UrlChanged           -> rebase(h, next)   // S3, 🟡 3
    if origin is UrlChanged && h.Origin is Established     -> seal(h, next, cmd, origin)   // R4-nav
                                                           // -> SealedByWorld(UrlChanged) on BOTH
                                                           //    incident edges, via sealTops
    if TPolicy.SameUndoable(next, h.Present)               -> pass(h, next, cmd, origin)
    if enveloped                                           -> record(h, next, cmd, origin)
    otherwise                                              -> seal(h, next, cmd, origin)

pass(h, next, cmd, origin)   = h with { Present = next }
                             ; if !IsSilent(cmd):
                                   sealTops(SealedByEffect(redact(origin)))
                             ; Future preserved                                 // B2's closure

seal(h, next, cmd, origin)   = h with { Present = next }
                             ; sealTops(SealedByWorld(redact(origin)))
                             ; Past and Future both preserved                   // B1's closure, S9

record(h, next, cmd, origin) = Past.Push(Step(TPolicy.Scrub(h.Present), redact(origin),
                                              IsSilent(cmd) ? Crossable
                                                            : SealedByEffect(redact(origin)),
                                              ticks))
                             ; Present := next
                             ; if Future non-empty:                             // R4-2 (B9 b)
                                   Future := Future.ReplaceTop(
                                       top with { Edge = SupersededByNewAction(redact(origin)) })
                                   // PRESERVED and sealed — not cleared
                             ; trim BOTH stacks to TPolicy.Depth                // R4-2
                             ; Origin := Established

rebase(h, next)              = h with { Present = next }        // origin re-based, nothing recorded

sealTops(state)              = if Past   non-empty: Past   := Past.ReplaceTop(top with { Edge = state })
                             ; if Future non-empty: Future := Future.ReplaceTop(top with { Edge = state })

StepBack(h)    = let s = Past.Peek() in
                 { Past    = Past.Pop(),
                   Present = TPolicy.Restore(s.Model, h.Present),
                   Future  = Future.Push(Step(TPolicy.Scrub(h.Present), s.Cause, Crossable, s.AtTicks)),
                   Movement = h.Movement }                       // a press never changes Movement
StepForward(h) = mirror image, pushing Step(TPolicy.Scrub(h.Present), …) onto Past
redact(m)      = m is SensitiveCause ? new HistoryRedacted(m.GetType().Name) : m     // SEC-2
```

`StepBack` pushes the forward step **`Crossable`** rather than carrying the popped step's edge
state: an edge reaches `Future` only by an undo that was permitted, so it was crossable at that
moment, and it can only *become* sealed later, by `seal` or by `pass`'s effect marking. Revision 2
copied the popped barrier forward, which conflated "why the backward edge was sealed" with "whether
the forward edge is crossable".

**How `StepBack` composes with a superseded branch (`[R4-2]`), because a reviewer will ask.** After
supersession, `Future` is `[superseded, …]`. An undo pushes a fresh **`Crossable`** step on top, so
the user can redo back to where they just were; the superseded edge is still underneath, still
refusing, and still naming the message that superseded it. The two mechanisms compose without a
special case: `Backward`/`Forward` read the top edge and nothing else, and *"you can redo the undo
you just did, but not past the point where the timer fired"* falls out rather than being arranged.

**INV-6 — availability from the value alone, in advance.**

```
Backward(h) = h.Past.IsEmpty                               -> Nothing
              h.Past.Peek().Edge is SealedByEffect       c -> BlockedByEffect(c)
              h.Past.Peek().Edge is SealedByWorld        c -> BlockedByWorld(c)
              h.Past.Peek().Edge is SupersededByNewAction c-> BlockedBySupersedingAction(c)  // R4-2
              otherwise                                    -> Available(h.Past.Count)
Forward(h)  = mirror image over h.Future                                        // S9, R4-2
```

Pure, O(1), computed from the value and nothing else, and the **same function** `Decide` and
`Transition` route on — so the advance answer and the outcome cannot disagree.

`SupersededByNewAction` can only ever appear on a **`Future`** top under this design — `record`
seals only the forward branch. `Backward` handles it anyway rather than treating it as unreachable:
`Backward` and `Forward` are one function over a stack, and a branch that says *"this cannot
happen"* is the kind of thing that stops being true one revision later. It costs one line.

**`[R4-9]` (🟡 1) `Available` carries `Depth`, not a promise.** Revision 3 named the payload
`Remaining`, and `Available(h.Past.Count)` counts the whole stack even when the second edge down is
sealed — so a chrome would render *"12 steps available"* when one is. In a value whose entire
purpose is to be answerable in advance, that is a small lie. Renamed to **`Available(int Depth)`**,
documented as *the number of retained steps on this side, not the number that can be crossed*. The
alternative — computing the crossable prefix — is O(Depth) on a path the chrome calls **on every
render**, and it buys a number no chrome in this pass displays. Renaming is O(1) and honest; if a
chrome later wants the crossable count it can walk the stack itself, having been told that is what
it is doing.

**`IsSilent(Command)`** is a **type test** — `Command.None`, and a `Command.Batch` all of whose
members are silent (`Command.cs:5-7`). Not reference equality: `Commands.None` allocates a fresh
`Command.None()` on every call (`Command.cs:12`). `NavigationCommand` is not silent, which is how a
navigation becomes a commitment.

**Origin re-basing (S3, and 🟡 3's exception stated).** `Runtime.Start` dispatches the bootstrap
`UrlChanged` through the ordinary `Dispatch` path after the first render, **and only when
`initialUrl is not null`** (`Runtime.cs:431-434`, read this revision). The wrapper cannot observe
whether the head supplied one. Rule: `Origin = Fresh` until the first `record`, and while `Fresh` a
transition whose origin is `UrlChanged` re-bases instead of recording.

**`[R4-8b]` The exception, restated as what the rule does — S23.** Revision 3 described this as
*"one lost undo stop, only ever the first message of a session, only on a head with no initial
URL"*, and that was wrong on all three counts. `Origin` becomes `Established` **only in `record`**;
`pass`, `seal` and `rebase` all leave it `Fresh`. So the rule fires for **every** `UrlChanged` until
the first recorded step, on **every** head. Conduit reaches this trivially: a link click emits a
non-silent `NavigationCommand` and usually does not move the model → `pass`, and with `Past` empty
`sealTops` marks nothing; the head then dispatches `UrlChanged` → `Origin` still `Fresh` → re-base,
nothing recorded. Click through three pages before typing anything and **none of the three is an
undo stop**. The correct statement, which is what `spec-author` and `csharp-dev` implement from:

> **While nothing has been recorded, every `UrlChanged` re-bases the origin and records no undo
> stop. Navigation performed before the session's first recorded step therefore never becomes an
> undo stop, on any head. After the first `record`, an incoming `UrlChanged` seals BOTH incident
> edges as `SealedByWorld(UrlChanged)` (`[R4-nav]`, below); every other message classifies by the
> four-row table.**

**B11, confirmation pass.** That last clause read *"After the first `record`, navigation classifies
like any other message"* until this amendment. It was true when it was written and `[R4-nav]` made
it false eleven lines further down, in the one block both `spec-author` and `csharp-dev` are told to
implement from. The same clause is the closing sentence of **standing decision 7** in
`07-handoff.md:146`, which is the architect's to correct — the handoff has no `[R4-nav]` row at all,
in either its approval table or its § 6.1 mitigation-to-owner map, and it is the artifact that
survives this directory in a specialist's context.

**I am keeping the behaviour and correcting the description, rather than taking the one-line
alternative.** Setting `Origin := Established` in `rebase` would make revision 3's sentence true as
written — but it would also make the second and third pre-record navigations *recorded* undo stops,
and crossing one of those restores the previous page's **model** without its **URL**, because
`record` pushes a `Crossable` edge and undo issues no navigation command. Trading a wrong sentence
for wrong behaviour is a bad trade. The design's own stance is that navigation is a commitment
(§ *Chosen Direction*), and "undo does not walk back through pages you only passed through" is the
reading consistent with it. `ux-expert` q9 confirms with the corrected description in front of it.

**`[R4-nav]` Incoming navigation, after the first recorded step — the user's rule, raised after
close-out.** S23 above covers navigation *before* anything is recorded, and the re-basing rule was
the only thing the plan said about `UrlChanged`. It said nothing about the case after `Origin`
becomes `Established`, and the default there was wrong: a `UrlChanged` the browser delivers on a
back or forward press is enveloped like any other message, so it `record`s, and application undo
then restores a model without its URL while the browser's own stack walks away from the
application's history. One classification rule, beside the re-basing rule and in the same place:

> **`origin is UrlChanged && Origin is Established -> seal, SealedByWorld(UrlChanged)`.** Both
> edges. `Forward(h)` and `Backward(h)` refuse with the navigation as cause.

**Why `seal` is the only row the amended `INV-2` leaves — the derivation, recorded here rather than
left as an agreement.** The Critic derived this in the confirmation pass and it belongs beside the
rule, because a reader who cannot reconstruct it will read the seal as a preference. Take the four
rows of § *The classification rule* against an incoming `UrlChanged` once `Origin` is `Established`:

- **`record`** mints an undo stop whose crossing restores a model carrying the *previous* page's
  `Route` while the browser stays where the user put it — a `(model, location)` pair no ordinary
  interaction produced, which is the amended `INV-2`'s new falsifier verbatim
  (`00-scope.md` INV-2, *"any such sequence containing a navigation the browser delivered, after
  which the resulting model and the location the user is at are a pairing no sequence of ordinary
  actions could have produced"*).
- **`pass`** moves `Present` and records nothing, but leaves the *earlier* stop crossable, so the
  next undo restores the pre-navigation model while the browser stays — the same falsifier, one
  press later.
- **`rebase`** is `Fresh`-only by construction (`h.Origin is Fresh` is the guard), so it is not
  available in this window at all.
- **`seal`** is what is left, and it satisfies the invariant on the nose: `Present.Route` moves with
  the location in the same transition, and both incident edges refuse thereafter — which is exactly
  *"the two move together or not at all"*.

So the amended `INV-2` both **permits** the seal and **forces** it. The composition holds too: after
a seal, later `record`s push fresh `Crossable` steps **above** the sealed one, so undo works inside
the new page and stops at the navigation — the reach bound falls out of `Backward` reading one edge
rather than being arranged.

**Why `INV-3` needs no location clause, stated rather than left as raised-and-not-acted-on.**
`Present.Route` can only move on a transition whose origin is `UrlChanged`; after the first `record`
every such transition seals **both** incident edges, and before it there is nothing to cross. The
location is therefore **constant across any window in which a movement is permitted**, and INV-3's
round trip requires `Backward` **and** `Forward` to be `Available`: an undo pushes a fresh
`Crossable` step onto `Future`, and a `UrlChanged` arriving before the redo seals exactly that step,
so the property skips on its guard rather than failing. A location conjunct added to INV-3 would be
one **no property could ever turn red on**, which is worse than leaving it out. Under a projection
the same conclusion arrives by the other road: `Restore` keeps `current`'s `Route`, so movement
never touches the location. `00-scope.md`'s INV-3 is correctly untouched; this paragraph exists so
the omission is not re-raised at review as one.

**🟡 5 (confirmation pass) — the one navigation the rule does not reach, and why the amended INV-2
already covers it.**
`apply`'s true-no-op branch precedes the rule, so a `UrlChanged` that returns the same model
reference with a silent command — the shape a program produces when it leaves `UrlChanged` to an
`_ =>` fall-through — bypasses the seal: the browser moves and the history is untouched. This is not
a hole. Such a program has **no location in its model to restore**, so no crossing of any step can
produce a model-and-location pair the user could not have reached, which is the clause the amended
INV-2 is written with: *"on any head where the application has one"*. Named here so a reader meeting
the ordering does not read it as an oversight, and so nobody moves the rule above the no-op branch
to "fix" it — doing so would seal a history on a message that changed nothing.

**🟡 6 (confirmation pass) — this seal is a third writer of a last-writer-wins cause.** Pass 4
recorded that `sealTops`
and `record`'s `ReplaceTop` overwrite each other's cause; `[R4-nav]`'s seal joins them. Concretely:
navigate (`Future` top ← `SealedByWorld(UrlChanged)`), then act (`record` → `ReplaceTop` →
`SupersededByNewAction(cause)`) — redo still refuses, because nothing ever un-seals and no
reachability changes, but it refuses under the **later** name. Step 3's criterion says *most
recently*, and its enumeration names this seal as one of the three writers.

**No new mechanism.** `seal` and `sealTops` already exist and already mark both incident edges;
`SealedByWorld` already exists and already carries the message that sealed the edge; `Backward`
and `Forward` already read the top edge and return `BlockedByWorld(cause)`. No new `EdgeState`, no
new `MovementAvailability` case, no new `Scrub` site — `seal` pushes no `Step`. The rule is one
line in `apply`.

**Its interaction with S23, because the two rules read the same message and must not be confused.**
They partition on `h.Origin`, and the re-basing rule is tested first, so **pre-record navigation
stays exactly as `[R4-8b]` left it** — every `UrlChanged` before the first recorded step re-bases,
records no undo stop, and seals nothing (with `Past` and `Future` empty there is nothing for
`sealTops` to mark in any case). Only navigation *after* the first `record` seals. Conduit's
three-pages-before-typing walk therefore behaves as S23 describes, unchanged; a back press *after*
the user has typed something refuses in both directions, by name.

**The scope clause this needs — and unlike revision 4's nine items, this one does force an
amendment.** `INV-2` is being amended by the architect so that "the world" includes the browser's
location, a navigation being a commitment in both directions. Under the unamended reading a sealed
navigation was arguably over-strict; under the amended one it is the only classification that keeps
undo landing on reachable states, since a state whose model and whose URL disagree is not one
ordinary interaction produces. The wording is the architect's; the dependency is recorded here.

**B10 — the rule keys on a message type the seam does not guarantee, so the guarantee carries an
application obligation.** `Navigation.UrlChanges(Func<Url, Message> toMessage)`
(`Picea.Abies/Navigation.cs:17-29`) lets the **application** supply the constructor for an incoming
URL change, and on the WebAssembly head that is the only route by which one arrives. An application
that maps incoming navigation to its own message type gets no seal from this rule and every property
in step 8 stays green. The resolution is an **application obligation of the same kind as L1–L6** —
stated in full in § *The lens, and its six obligations*, carried into step 5's docs obligation (vi),
step 13's guide brief and § *Spec obligations* line 16, and written into `06-spec.md`'s lens-law
section by `spec-author`. It is documentation, not mechanism: no policy predicate, no analyzer rule
and no framework-owned wrapper around `UrlChanges` is added by this amendment.

**`[R4-9]` (🟡 6) Undo out of a terminal state, stated rather than left to surprise someone.**
`IsTerminal(h) = TProgram.IsTerminal(h.Present) && Past.IsEmpty && Future.IsEmpty`, and `Decide`'s
history cases precede the terminal guard, so **undo remains dispatchable out of a terminal state**
and can resurrect a terminated program. That is deliberate: a program whose model reached a terminal
state is exactly the case where a user most wants to back out of it, and a history that is
unreachable once the program terminates is a history that stops working when it matters. Unremarked
in revision 3; now a spec line in `06-spec.md`, because *"undo resurrects a terminated program"* will
surprise someone otherwise.

**How an application draws undo chrome.** `View` forwards `h.Present`, so the chrome is an **outer**
`WithView`, because `WithHistory<…>` is a `Program<History<TModel>, TArgument>` and therefore
satisfies `ProgramCore<History<TModel>, TArgument>` (`Program.cs:64-66`):

```
Runtime<WithView<WithHistory<CounterProgram, CounterHistoryPolicy, CounterModel, Unit>,
                 CounterHistoryView, History<CounterModel>, Unit>,
        History<CounterModel>, Unit>.Start(…)
```

`CounterHistoryView.View(h)` draws `CounterProgram.View(h.Present)` plus buttons whose disabled
state and tooltip come from `Backward(h)` / `Forward(h)`. Both wrapping orders compile and differ
semantically; the three-layer stack `WithView<WithHistory<WithView<Core, NativeView, …>, …>,
ChromeView, …>` is the composition the native head will actually attempt, and step 10 compiles it.

**Head coverage.** InteractiveServer, InteractiveWasm, Native — identical implementation; the
context is platform-free and no head adapter changes. `Static` excluded: no MVU loop. Under
**InteractiveAuto** the client runtime starts from `Initialize` (`Runtime.cs:376-383`) and nothing
carries a model across the handoff, so the history **begins** at the handoff — spec line, not a
defect. The one head-specific limitation is bracketed runs on Native, stated above.

**Memory over a long session.** Bounded at `TPolicy.Depth` (default 100) `Step`s in each stack; each
is one model reference, one message reference, one small sum value and a long. Models are
structurally shared through `with`, so the history defers collection rather than allocating. Three
honest hazards for `performance-engineer`:

- a retained model **pins the whole graph it references**, so a model holding a large fetched list
  keeps it alive for up to `Depth` versions;
- `Held(TModel Anchor)` pins one further whole model graph **for the duration of a bracketed run**,
  on top of `Depth` (Critic 🟡 7). Un-bracketed presses pin nothing;
- `seal` pushes no step, so world changes no longer consume history slots — a strict improvement on
  revision 2 for exactly the `SubscriptionsDemo` shape that motivated the concern;
- **`[R4-2]` a superseded `Future` branch is retained rather than cleared**, so models the user
  walked back through and can never return to stay alive until they are trimmed. Bounded: `Future`
  now trims to `Depth` at its far end exactly as `Past` does, so the ceiling is `2 × Depth` retained
  models rather than `Depth`, plus the anchor during a bracket. This is the memory price of B9(b),
  it is the one cost of the user's decision that is not free, and `performance-engineer` measures it
  at step 7 alongside the existing retained-graph measurement rather than as a new item.

---

## Backing Patterns

- **List zipper** (Huet, *The Zipper*, JFP 7(5) 1997) — `(Past, Present, Future)`. Track B's
  citation; Track A derived the same object from the kernel's shape.
- **Very-well-behaved lenses** (Foster, Greenwald, Moore, Pierce, Schmitt, *Combinators for
  Bidirectional Tree Transformations*, TOPLAS 29(3) 2007) — GetPut / PutGet / PutPut are L1–L3; L4
  is the coherence obligation the projection-type erasure adds; L6 is what the erasure removed and
  must restore as an axiom.
- **Explicit gesture bracketing rather than inferred quiescence** — Qt's `QUndoStack::beginMacro` /
  `endMacro` and CodeMirror's explicit history-boundary call are both *explicit* boundaries in
  systems that also offer inferred ones. Revision 2 took the inferred half (ProseMirror /
  CodeMirror `newGroupDelay`, evidence tuned for coalescing **keystrokes**) and applied it to
  **movement**, which is what B5 falsified. This revision takes the explicit half, applied to
  movement. `newGroupDelay`'s 500 ms remains cited for step 14's coalescing, where it is the same
  consumer the evidence was gathered for.
- **Higher-order reducer / static-forwarding program** — redux-undo's `undoable()`, elm-community
  `UndoList`; locally, `WithView` (`Program.cs:64-85`) is the same shape already in this codebase,
  reused for the chrome composition.
- **Keyed reconciliation as the hold mechanism** — the anchor is the same idea as a virtual-DOM keyed
  diff: report the same keys, nothing moves. `SubscriptionManager.Update` already implements it
  (`Manager.cs:51-74`); the design adds no mechanism, only an answer.
- **Persistent data structures** (Driscoll, Sarnak, Sleator, Tarjan, JCSS 1989), with Okasaki
  (*Purely Functional Data Structures*, ch. 5–6) on why persistence and amortisation do not compose
  — hence array copy-on-write with a **worst-case O(Depth)** bound, not an amortised one.
- **Bounded depth = 100** — ProseMirror `depth: 100`, CodeMirror `minDepth: 100`. Qt (`undoLimit` 0)
  and redux-undo (`limit: false`) default to unbounded and both carry a long tail of memory
  complaints; the debugger's 10 000 is a debug-tool number.
- **~~Discard the forward branch on divergence~~ → `[R4-2]` refuse across the superseded branch.**
  Every linear-undo system surveyed discards, and Track A's forcing argument was that a retained
  branch makes redo a *relation*, which has no inverse, so INV-3 becomes unstatable. **The argument
  is about a retained *crossable* branch and does not reach a sealed one**: `Forward(h)` refuses at
  the superseded edge in both `Decide` and `Transition`, so no message sequence can reach the branch,
  redo stays a partial function, and INV-3 — which quantifies over the case where redo is available
  — is untouched. The prior art is unanimous about discarding and silent about *telling the user*,
  which is the gap B9 found: Qt, ProseMirror, CodeMirror and redux-undo all destroy the branch with
  no signal, because in a text editor the divergence is always a keystroke the user just made. In a
  framework where a timer can be the actor, it is not, and the survey's silence is not evidence.
- **Effects are not recalled; a boundary seals an edge** — undo's reach is exactly the invertible
  sub-algebra of the command monoid (A3); this pass keeps that sub-algebra trivial.
- **Contested points, settled on facts:** replay-from-log rejected because `Transition` reads the
  wall clock in a shipped app in this repository (`SubscriptionsDemo/Program.cs:133,140,142,151,160,
  169,179,189`); inverse-command stacks rejected because the framework cannot enforce INV-2 over
  application-authored inverses (Berlage, TOCHI 1994, is careful about exactly this).

---

## Namespace Plan

**New bounded context: `Picea.Abies.History`** — folder `Picea.Abies/History/`, same assembly. The
context is *the history of an application's states*; undo and redo are operations over it. That is
why it is not `Picea.Abies.Undo` — a namespace names the domain, not the gesture.

| Neighbour | Relationship |
|---|---|
| `Picea.Abies` (root) | Depends on it for `Message`, `Command`, `Commands`, `Program`, `ProgramCore`, `ProgramView`, `UrlChanged`. The root does **not** depend on `History`. |
| `Picea.Abies.Subscriptions` | **Narrowed since revision 2.** With the settle source deleted, the dependency is on the `Subscription` **return type only** (`Subscription.cs:1`, named by `ProgramCore.Subscriptions`, `Program.cs:24`) — no `Subscription.Batch`, no `SubscriptionModule.Create`, no framework-declared source. Nothing in `Subscriptions` references `History`. |
| `Picea.Abies.DOM` / `.Html` | No reference in either direction. |
| `Picea.Abies.Debugger` | **No reference in either direction.** Total separation; nothing moves out of `#if DEBUG`, including `RingBuffer<T>`, which is a mutable class with in-place `Add`/`Clear` (`Debugger/RingBuffer.cs:12-89`) and cannot be a field of a model value under ADR-008. A future H2 pass would add `Debugger → History`; it does not exist now. |
| Head adapters (`.Browser`, `.Server`, `.WinUI`) | No reference. All three entry points are already generic over `TProgram, TModel, TArgument`, so adoption is a type argument at the application's call site. |

No new assembly, no new package reference. Every type is generic and unreferenced by non-adopting
applications — the basis of the "trims to nothing" claim, which step 7 must **measure**.

**Visibility.** `History<TModel>` and `Step<TModel>` get **internal constructors**; the only public
factory is `History.Start(TModel)` and all movement goes through the wrapper.
`Picea.Abies.csproj:24` already carries `<InternalsVisibleTo Include="Picea.Abies.Tests" />` (`:25`
for the benchmarks), so the test seam exists with **no csproj change**. No deviation from *Make
Illegal States Unrepresentable* is requested.

**Relationship to `Picea.Abies/Debugger/` — total separation, nothing moves.** `TrySetCoreState` is
not called, reused or touched; no reflection, no `JsonTypeInfo`, no serializer, no `debugger.js`, no
browser asset. **ADR-025 holds unamended.** The DEBUG snapshot consequence stands as revision 2
reframed it: for a wrapped application in a DEBUG build `_core.State` is `History<TModel>`, so
`GenerateModelSnapshot` (`Runtime.cs:619-643`) wants a `JsonTypeInfo<History<TModel>>`, which drags
the *2026-03-29 `JsonPolymorphic`* obligation onto the application's message hierarchy — and
**SEC-1's redaction is what makes adding that metadata safe** (SEC-5).

---

## File-Level Changes

| Path | Change | Owner |
|---|---|---|
| `Picea.Abies/History/HistoryStack.cs` | **new** — internal immutable bounded stack, array copy-on-write: Push / Pop / Peek / ReplaceTop / Trim. Worst-case O(Depth), Depth bounded; trivially persistent | csharp-dev |
| `Picea.Abies/History/History.cs` | **new** — `History<TModel>`, `Step<TModel>`, `EdgeState`, `Movement`, `Origin`; `Start` / `StepBack` / `StepForward` / `Cleared` / `SealTops`. **Internal constructors** | csharp-dev |
| `Picea.Abies/History/MovementAvailability.cs` | **new** — `Available` / `Nothing` / `BlockedByEffect` / `BlockedByWorld`, `Direction`, plus the pure `Backward(h)` / `Forward(h)` queries | csharp-dev |
| `Picea.Abies/History/HistoryMessage.cs` | **new** — `HistoryMessage.Undo/Redo/Hold/Settle/Clear`, `MovementRefused`, `HistoryEvent.Enveloped`, **`SensitiveCause`**, **`HistoryRedacted`** | csharp-dev |
| `Picea.Abies/History/HistoryPolicy.cs` | **new** — static-abstract policy (`Depth`, `Restore`, `SameUndoable`, **`Scrub`**) + `WholeModelHistoryPolicy<TModel>` | csharp-dev |
| `Picea.Abies/History/WithHistory.cs` | **new** — the higher-order program | csharp-dev |
| `Picea.Abies/History/HistoryTelemetry.cs` | **new** — `ActivitySource("Picea.Abies.History")`; spans on movement, refusal, hold and settle only; **type-name-only cause tags** (SEC-4) | csharp-dev |
| ~~`Picea.Abies/History/HistorySettleSource.cs`~~ | **deleted from the plan** — no framework-declared subscription exists | — |
| `Picea.Abies.Tests/History/HistoryStackTests.cs` | **new** — including a persistence test | csharp-dev |
| `Picea.Abies.Tests/History/HistoryTestProgram.cs` | **new** — purpose-built program + model, model-derived `Subscriptions`, message-recording `Transition`; `CountingSubscription` that **dispatches autonomously on start** | csharp-dev |
| `Picea.Abies.Tests/History/Generators.cs` | **new** — hand-rolled seeded generators, fixed seed corpus | csharp-dev |
| `Picea.Abies.Tests/History/HistoryInvariantTests.cs` | **new** — INV-1 … INV-6 | csharp-dev |
| `Picea.Abies.Tests/History/HistoryLensLawTests.cs` | **new** — L1 … L6, plus **`Scrub_overridden_alone_violates_L5`** (`[R4-5]`) | csharp-dev |
| `Picea.Abies.Tests/History/HistorySecurityRegressionTests.cs` | **new** — SEC-3 (a) and (b), plus **`Anchor_never_reaches_a_release_path_surface`** as a third, explicitly-not-(b) test (`[R4-6]`), plus **`Anchor_is_always_the_model_the_bracket_was_opened_against`** as a fourth — beside the third, not merged into it, and a correctness test rather than a confidentiality one (`[R4-anchor]`, step 8(o)) | csharp-dev |
| `Picea.Abies.Tests/History/HistoryMovementTests.cs` | **new** — INV-7 at runtime level, plus the malformed-bracket cases | csharp-dev |
| `Picea.Abies.Tests/History/HistoryCompositionTests.cs` | **new** — both `WithView` orders + the three-layer stack | csharp-dev |
| `Picea.Abies.Benchmarks/HistoryDispatchBenchmarks.cs` | **new** — per-dispatch overhead, wrapped vs. bare | csharp-dev (design/analysis: performance-engineer) |
| `docs/adr/ADR-030-undo-redo-as-a-history-program.md` | **new** — ADR (content from `07-handoff.md`) | tech-writer |
| `docs/concepts/undo-redo.md` | **new** — the cursor, the projection, the refusal contract, movements and bracketing | tech-writer |
| `docs/guides/adding-undo.md` | **new** — composing `WithHistory`, writing a projection **and `Scrub` in the same example**, drawing affordances with an outer `WithView`, the three-layer case, the chrome's `Hold`/`Settle` contract and the native-head limitation, key bindings are the app's, `SensitiveCause`, the DEBUG `JsonTypeInfo` note | tech-writer |
| `docs/api/` | **update** — public surface of `Picea.Abies.History` | tech-writer |
| `docs/adr/ADR-008-immutable-state.md` | **amend line 85** — "Undo/redo: trivial to implement by storing state snapshots" replaced with a pointer to ADR-030 | tech-writer |
| `docs/adr/ADR-025-…-phase1.md` | **doc-sync only** — ADR-030 records that ADR-025 was examined and needs no superseding | tech-writer |
| `CHANGELOG.md` | **update** | tech-writer |
| `docs/security/threat-model.md` | **update** — two Threats rows, one Open Risk, Trust Boundary 7 (SEC-6) | security-expert |
| `docs/security/hardening-backlog.md` | **update** — the `SerializeMessageArgs` retrofit (SEC-7) | security-expert |
| `Picea.Abies/Runtime.cs`, `Program.cs`, `Subscriptions/**`, `Debugger/**`, `Picea.Abies.Browser/**`, `Picea.Abies.Server/**`, `Picea.Abies.WinUI/**`, `Picea.Abies.Native/**`, every `wwwroot/*.js`, `Picea.Abies.csproj` | **unchanged — deliberately.** If a step proposes touching any of these, that is a signal the design has slipped, not a detail. **`[R4-spec-commit]` One exception, named by file and element so it is not read as a slip and so no other csproj touch can shelter behind it:** `Picea.Abies.Tests/Picea.Abies.Tests.csproj` gains **two items in one `ItemGroup`** — `<Compile Remove="History\UndoRedoSpec.cs" />` **and `<None Include="History\UndoRedoSpec.cs" />`** — in the **PR 0** spec commit (landed as **PR #361**, merge commit `70d9ae58b142039f274d4f124902a9a60b6a6186`) and loses **both** again in **step 6 (PR 3)**. The `None Include` was added during PR 0's review so the excluded file stays visible in IDE solution trees and to `-getItem:None`, and while the exclusion is in force it is **load-bearing, not decorative**: the `Compile` glob does not claim the file in that state, so the `None Include` is the only thing holding `History\UndoRedoSpec.cs` in any MSBuild item group at all. It becomes inert the other way round — when step 6 removes the exclusion and the default `Compile` glob claims the file again — which is why it comes out **with** the exclusion rather than being left behind as a stale item. That is the whole exception — one project file, two items, added once and removed once, both times as a stated expected change. Note that the test project is *not* in this row's list (the row names `Picea.Abies.csproj`, the framework project); the exception is written here because the slip-signal rule is what a reader applies to **any** csproj touch, and a reader who finds an unexplained one should still treat it as a signal. | — |

**Not in this pass, deliberately** (gate-3 decision 4): no demo or template adopts `WithHistory`.
`Picea.Abies.Counter` is load-bearing for the benchmarks, the `dotnet new` templates and four E2E
fixtures across all heads. Adoption is demonstrated by tests, the benchmark and the guide.

**OTEL registration.** `ActivitySource("Picea.Abies.History")` is registered nowhere, and neither
are `"Picea.Abies.Runtime"` or `"Picea.Abies.Subscriptions"` — every registration in the repo is the
exact name `"Picea.Abies"` (`Conduit.ServiceDefaults/Extensions.cs:32`,
`Templates/templates/abies-server/Program.cs:34`), and `AddSource` matches exactly, not by prefix.
Step 11 is verified with an in-process `ActivityListener`; the guide tells an adopting application
to add `.AddSource("Picea.Abies.History")`; the **inherited** two-source gap is raised as a separate
item for the architect's close-out and a `devops` follow-up.

---

## Todo List

The spec test (`06-spec.md`) is authored and approved **before** step 1 and is immutable during
implementation; the properties in steps 3, 8, 9 and 10 extend it, they do not replace it. Every step
terminates at `reviewer-blind` → `reviewer-reconcile`. Step numbers are unchanged from revision 2 so
that `05-critic.md`'s references still resolve.

**`[R4-spec-commit]` Precondition — the spec commit stands alone, before step 1.** The user's answer
to `07-handoff.md` § 8 item 1, 2026-09-07, and it overrules the reading I recorded there. The
approval commit is `Picea.Abies.Tests/History/UndoRedoSpec.cs` + `Picea.Abies.Tests/SpecAttribute.cs`
and **nothing else**, landing **before step 1** — which is what `06-spec.md` § *The Lock* already
reads *"the spec lands before plan step 1"* to mean, taken literally rather than reinterpreted as
"authored and approved before step 1".

**As landed, "nothing else" held for the commit and not for the PR.** The approval commit `b39ac58`
is those three files and nothing else, as planned. The PR that carried it, **#361**, is not: the
branch also held the design-record and amendment-docs commits, so its squash merge
`70d9ae58b142039f274d4f124902a9a60b6a6186` measures **40 files / 7,669 changed lines** and
`Check PR Size` **failed** against the 1500-line hard limit
(`.github/workflows/pr-validation.yml:211`; the check is non-required, so the merge proceeded, and
whether that was the right call is the user's, not this plan's). The isolation this precondition
argues for — the approval commit standing alone, so the Lock's git-history check never has to be
waived — is a property of the **commit**, and it survived intact. The PR-level "three files, nothing
else" reading did not. Read the same correction in the PR-cut table's PR 0 row.

`UndoRedoSpec.cs` cannot compile until the end of step 6, so the same commit adds, to
`Picea.Abies.Tests/Picea.Abies.Tests.csproj`:

```xml
<ItemGroup>
  <!-- Locked undo-redo spec (PR 0, [R4-spec-commit]), awaiting the feature types it
       exercises. Removed in plan step 6 (PR 3) once WithHistory et al. exist. Carried as
       <None> in the meantime so it stays visible in IDE solution trees and `-getItem:None`
       rather than falling out of every MSBuild item group entirely. -->
  <Compile Remove="History\UndoRedoSpec.cs" />
  <None Include="History\UndoRedoSpec.cs" />
</ItemGroup>
```

**Landed** as **PR #361**, merge commit `70d9ae58b142039f274d4f124902a9a60b6a6186`, with **two**
items in that `ItemGroup` rather than the one this plan originally named. The `<None Include>` was
added during PR 0's review so the excluded file stays visible in IDE solution trees and to
`-getItem:None` instead of falling out of every item group; it is inert (the SDK's default `None`
glob already excludes what the `Compile` glob claims, so there is no `NETSDK1022`), and it is stale
the moment the exclusion goes. **Both items are therefore the expected change for step 6 to remove,
along with the `ItemGroup` and its comment, which hold nothing else.**

`SpecAttribute.cs` is a five-line attribute with no dependency on anything this pass builds; it
compiles from the moment it lands and is **not** excluded. Only `UndoRedoSpec.cs` is.

It is **PR 0** — its own PR, terminating at `reviewer-blind` → `reviewer-reconcile` like every other,
carrying the locked spec, the attribute and the two-item exclusion group. Both items are removed in
**step 6 (PR 3)** as a stated, expected change (step 6's Done-when; the file table's slip-signal row
names them by file and element).

**Why this shape and not the first commit of PR 3.** `06-spec.md` § *The Lock* has
`reviewer-reconcile` run a git-history check that flags a spec file modified in the same PR that
brings it to passing. Landing the spec inside PR 3 would trip that check on the pass's most
load-bearing file and require the PR body to ask the reviewer to disregard it — **and nothing in a PR
body tells a reviewer to ignore a check.** A waiver requested once is a waiver available always, and
the check's whole value is that it is not negotiable from inside the change it is checking. Under
PR 0 the check never has to be waived: the approval commit and every commit that greens the spec are
in different PRs, which is exactly the separation the Lock asks for. The cost is a deliberately
excluded test file on `main` for two PRs, which is visible in one line of one csproj, reviewed in
PR 0, and removed on schedule.

```
 1. [ ] → csharp-dev: HistoryStack<T> — internal immutable bounded stack, array copy-on-write:
         Push / Pop / Peek / ReplaceTop / Trim to Depth. No new package reference. Done when:
         TUnit tests cover push, pop, replace-top, eviction at the far end, depth invariance
         under 10 000 pushes, AND persistence (operations on a derived instance leave an older
         instance observably unchanged). Bound stated as worst-case O(Depth), not amortised.
 2. [ ] → csharp-dev: History<TModel>, Step<TModel>, EdgeState, Movement, Origin; Start /
         StepBack / StepForward / Cleared / SealTops. Records + `with` (ADR-008), INTERNAL
         constructors. HistoryRedacted designed alongside Step (SEC-1). Done when: (a) an
         undo-then-redo round trip leaves Present reference-identical and Past/Future
         structurally equal, with Movement unchanged — note that with the clock gone there is
         no ordinal, so the round trip is now value-equal end to end and revision 2's INV-3
         caveat disappears; (b) a reflection test asserts History<> and Step<> expose no public
         constructor; (c) StepBack/StepForward push the opposite-side step as Crossable.
 3. [ ] → csharp-dev: MovementAvailability + Direction + the pure Backward(h) / Forward(h)
         queries. Available carries Depth, documented as retained-steps-on-this-side and NOT a
         crossable count (R4-9, 🟡 1). Done when: a property over generated history values shows
         the advance answer agrees with the outcome of actually stepping, for all five cases in
         BOTH directions — Nothing, Available, BlockedByEffect, BlockedByWorld and
         BlockedBySupersedingAction (R4-2) — and the carried cause is the message that MOST
         RECENTLY sealed or superseded the edge rather than the message that created the step
         (INV-6, S9's symmetry). MOST RECENTLY is load-bearing: there are THREE writers of an
         edge's cause and the last one wins — sealTops on command feedback, record's ReplaceTop
         on supersession, and R4-nav's navigation seal (🟡 6 of the confirmation pass, which
         extends pass 4's two-writer 🟡 to three). Navigate then act, and the Future
         top reads SupersededByNewAction rather than SealedByWorld(UrlChanged); both refuse,
         nothing un-seals, and no reachability changes — but the criterion must not read as though
         the cause were unique.
 4. [ ] → csharp-dev: HistoryMessage (Undo/Redo/Hold/Settle/Clear), MovementRefused(Direction,
         MovementAvailability), HistoryEvent.Enveloped(Origin, Inner), SensitiveCause,
         HistoryRedacted(string OriginalTypeName). Done when: each is a Message, sealed where it
         should be; no identifier in the namespace carries an I-prefix; no type name collides
         with another in the same namespace (checked explicitly — revision 2 shipped
         HistoryEvent.Step beside Step<TModel>).
 5. [ ] → csharp-dev: HistoryPolicy<TModel> static-abstract (Depth, Restore, SameUndoable,
         Scrub) + WholeModelHistoryPolicy<TModel> (100, identity lens, identity scrub). NO
         IsUndoable, NO SettleWindow, NO Time. Done when: a policy with Depth 3 bounds a history
         at 3 through WithHistory, IN BOTH STACKS (R4-2 trims Future too); a policy with a
         non-identity Restore/SameUndoable/Scrub triple satisfies L1–L6; A POLICY THAT OVERRIDES
         Scrub WITHOUT OVERRIDING Restore/SameUndoable FAILS L5 (R4-5); and the file documents,
         each in one sentence beside the others:
         (i) that overriding one lens member requires overriding the others;
         (ii) that Scrub is only meaningful with a non-identity projection — under the whole-model
              lens L5 forces it to the identity and overriding it alone is unlawful, and the
              framework's own L1–L6 harness pointed at an adopter's policy is the only route by
              which this is self-enforcing outside this repository (R4-5);
         (iii) that the whole-model policy retains message payloads verbatim unless marked
              SensitiveCause;
         (iv) that its undo reach has THREE bounds, all three stated together and at the same
              weight (S30): the last COMMAND FEEDBACK (S10); Depth ÷ a model-mutating
              subscription's rate, in WALL-CLOCK SECONDS — twenty-five in SubscriptionsDemo's
              shape (S25(b)); and the last INCOMING NAVIGATION (R4-nav), which in a routed
              application is usually the binding one. And that "the world" is a command's feedback
              WITH ONE EXCEPTION THE SAME TABLE CARRIES — an incoming UrlChanged once Origin is
              Established, bullet (vi) — while a subscription-delivered message is otherwise a
              user action and a rejected decision is the application's own response (R4-1, R4-2,
              R4-nav);
         (v) that an application with a model-mutating subscription MUST declare a projection or
              redo will not function — the branch is superseded by name on every tick (R4-2);
         (vi) that incoming navigation is the world: after the first recorded step a UrlChanged
              seals both incident edges as SealedByWorld(UrlChanged), so a browser back or forward
              press makes undo AND redo refuse by name rather than restoring a model without its
              URL — and that navigation before the first recorded step is re-based instead, and is
              never an undo stop (R4-nav, and R4-8b/S23 for the pre-record half); AND, in the same
              breath and not as a footnote, THE APPLICATION OBLIGATION THAT COMES WITH IT (B10,
              § The lens, and its six obligations): the guarantee holds for navigation delivered
              as Picea.Abies.UrlChanged, and an application that routes the browser's location
              through its OWN message type must classify it as a commitment itself, because the
              framework cannot see it — the same register as bullet (ii)'s lens obligation and
              spec line 2's reachability assumption. Note for the writer of this file that
              README.md:183-190 passes url => new UrlChangedTo(url), i.e. the shape that defeats
              the rule, while Conduit and every tutorial pass url => new UrlChanged(url);
         (vii) [R4-ux] that "refuse BY NAME", in (v) and (vi), means the typed cause is CARRIED and
              answerable in advance — NOT that the cause's type name is what a person reads. The
              rendered default is one template across all three refusal cases and both directions,
              "[Undo/Redo] unavailable — [reason].", with the reason clause humanized and
              "something changed since then" as the fallback; a raw Cause.GetType().Name is never
              default end-user text and stays in telemetry (SEC-4). The navigation case in (vi)
              carries its OWN words, because the user performed it deliberately: "Undo unavailable
              — you navigated away from this page." / "Redo unavailable — you navigated to a
              different page." — and NOT the world-refusal sentence written for ProfileLoaded
              (room-ux.md q7, q12, q13(b), q13(c), q9/q13(d)). Step 10 renders it, step 13 writes
              it; this bullet exists so the file that defines the reasons does not read as though
              the identifier were the message;
 6. [ ] → csharp-dev: WithHistory<TProgram, TPolicy, TModel, TArgument>. Decide (routing,
         terminal guard, single Enveloped event per decision, AND the delegated Err channel
         enveloped the same way — Err(e) -> Err(Enveloped(m, [e])), R4-1; the wrapper's own
         Err(MovementRefused …) stays BARE). Transition (movement with recompute + the B4 no-op
         branch for Undo/Redo/Clear; Hold and Settle; NO rule-2 collapse on ordinary events; the
         four-row record/pass/seal rule; record SUPERSEDES a non-empty Future rather than clearing
         it, and trims BOTH stacks to Depth, R4-2; MovementRefused transparency; origin re-basing,
         AND the navigation seal that sits beside it — origin is UrlChanged && Origin is Established
         -> seal, SealedByWorld(UrlChanged) on BOTH incident edges, tested AFTER the re-basing rule
         so pre-record navigation still re-bases, R4-nav; Scrub at every Step-construction site — supersession is a ReplaceTop of an Edge and is NOT
         one; redaction at every Cause store, including the superseding cause). Subscriptions
         (anchor while Held, TProgram.Subscriptions otherwise, no framework source). Initialize /
         View / IsTerminal forwarding. IsSilent as a type test that recurses into Command.Batch.
         Done when: a wrapped HistoryTestProgram undoes and redoes through a real Runtime with no
         change to Runtime.cs; a bracketed two-press run starts and stops zero application-declared
         subscriptions; an UNbracketed two-press run reconciles exactly twice, once per
         destination; a Settle with no run open changes nothing, renders nothing and emits nothing;
         AND (R4-4, S18) a two-event decision whose FIRST command errs leaves the model with BOTH
         events applied and the second command uninterpreted — asserted against the unwrapped
         program's opposite behaviour (first event only, later events never applied) so the
         difference is recorded as a difference, not as a passing test;
         AND (R4-spec-commit) BOTH items added to Picea.Abies.Tests.csproj by the PR 0 spec
         commit (landed as PR #361) are REMOVED in this step —
         <Compile Remove="History\UndoRedoSpec.cs" /> AND <None Include="History\UndoRedoSpec.cs" />,
         the latter added during PR 0's review so the excluded file stayed visible in IDE trees —
         together with the ItemGroup and comment that hold them and nothing else. Removing only
         the Compile Remove would leave an inert stale item, not a build error, which is exactly
         the kind of residue this Done-when exists to prevent. This is an
         EXPECTED change, not a slip: it is the one csproj touch the pass authorises, the file
         table's unchanged-files row names both by file and element, and the PR 3 body states it.
         Removing the exclusion is not an edit to the locked file and does not touch it. The spec
         compiles and runs for the first time here and must be observed RED FOR THE RIGHT REASON
         before anything is made green — the exclusion coming off is what makes that observation
         possible, so it happens FIRST in the step, not last.
         AND (R4-ns) ONE DECISION IS OWED BEFORE THE EXCLUSION COMES OFF, stated here rather
         than left to the moment it goes red. The test namespace Picea.Abies.Tests.History
         shares its trailing segment with the production static class
         Picea.Abies.History.History. A using Picea.Abies.History; placed BEFORE a file-scoped
         namespace declaration is consulted AFTER the enclosing Picea.Abies.Tests, whose member
         namespace History wins, so the unqualified History.Start(...) / History.Backward(h)
         fail to resolve (CS0234) rather than binding to the factory. Reproduced empirically in
         PR 1 and derived independently by reviewer-reconcile (undo-redo-pr1/09-review-verdict.md,
         P7, "the namespace collision — verified"). The locked UndoRedoSpec.cs avoids it the
         other way: its using sits INSIDE the namespace body (:34, after the namespace at :32),
         which resolves in the type's favour and is the placement .editorconfig:132
         (csharp_using_directive_placement = outside_namespace:warning) asks against. The file
         is immutable in its ASSERTIONS AND ITS CLAIMS and a using placement is neither
         (06-spec.md § The Lock, amendment 5's item on what the lock covers) — but MOVING it is
         not available, because moving it is what breaks resolution. PR 1's own tests take the
         third route, qualifying as Abies.History.History.Start(...) (HistoryTests.cs:17-32),
         which the spec cannot do to its own call sites without editing them. Both remedies
         08-review-blind proposed are unavailable: the shadow comes from the TEST namespace,
         which is inside the locked file, so moving the framework types neither removes it nor
         leaves the locked using true; and History<TModel>.Start does not carry Backward/Forward,
         which the spec also calls (:152, :193, :401, :417, :621). THE OPTIONS, WITH THEIR COST:
         (1) an IDE0065 suppression scoped to that one file in .editorconfig — cheapest to write,
             and it is a permanent style carve-out in a shared file, naming one test file;
         (2) a format-check exclusion for the locked file — narrower to argue for, wider in
             effect: it exempts the file from the whole formatter, not from the one rule;
         (3) an architect ruling that a using placement is outside the lock — which by amendment
             5's terms it ALREADY is, so the ruling alone changes nothing and still needs (1) or
             (2) to make the format check pass; its value is that it removes any argument that
             step 6 is editing a locked file.
         WHERE THE CHECK ACTUALLY BITES, verified so the cost is neither overstated nor missed:
         nothing in this repository sets TreatWarningsAsErrors or EnforceCodeStyleInBuild, so
         IDE0065 does not fail the build; the enforcer is dotnet format, and pr-validation.yml's
         lint job --includes only the .cs files CHANGED IN THE PR (:288, :317). PR 3 changes the
         csproj and not UndoRedoSpec.cs, so CI can stay green here while an IDE and a repo-wide
         dotnet format both flag the file, and the first later PR that touches it turns red.
         reviewer-reconcile recorded the step-6 format prediction as a PREDICTION it could not
         execute (the file is Compile Remove'd today); this bound is why. The decision is
         therefore owed for the next author rather than to make PR 3 pass — which is the reason
         to take it deliberately here. csharp-dev states the choice and its reason in the PR 3
         body; reviewer-reconcile verifies it as a claim.
 7. [ ] → performance-engineer (csharp-dev implementing the harness): RE-SEQUENCED HERE, before
         8–11, so a bad number invalidates one step rather than four. BenchmarkDotNet A/B —
         wrapped vs. bare dispatch — with a per-dispatch budget DERIVED from the 5% figure rather
         than observed, since no demo adopts WithHistory and the CI benchmark gate is therefore
         structurally silent on this pass. Plus: per-assembly IL size for a non-adopting artifact;
         retained-graph size at Depth 100; the cost of Restore/SameUndoable on the bare path —
         explicitly as O(largest text field) per dispatch under the whole-model policy, since
         EqualityComparer<TModel>.Default on a record walks nested records and compares strings by
         value (Critic 🟡 6), NOT as a constant; the saving from folding N decided events into one
         render; and the HistoryStack representation question. NOTE: the settle source is gone, so
         revision 2's "per-run cost of the settle source" measurement is withdrawn. Done when:
         numbers exist, the budget is stated, and the report says in one sentence that a green
         integer-MB gate and a green js-framework-benchmark run are not evidence for this design.
 8. [ ] → csharp-dev: properties INV-1 … INV-6, lens laws L1 … L6, and the SEC-3 security
         regression properties, over generated (action | undo | redo | hold | settle)* sequences
         in Picea.Abies.Tests/History/. Hand-rolled seeded generators, no new dependency.
         Done when ALL of:
         (a) AT LEAST one property per invariant id (S31 and 🟡 1 of the confirmation pass — NOT
             pass 4's 🟡 1, which is Available(int Depth) in step 3). Exactly one for six of the seven;
             INV-2 carries TWO — the reachable-states property over both lenses, and R4-nav's
             location property, which is a different assertion rather than a wider alphabet
             (06-spec.md states why). Both carry [Property("Invariant", "INV-2")].
             validate-phase-artifact.sh:197-202 collects declared ids from 00-scope.md and reports
             only ids that are MISSING — there is no count and no cap — so two properties on one
             id is tolerated mechanically; the departure from "one property per id" is the user's,
             made as approver on 2026-09-07, and it is written here so that a reviewer verifying
             step 8(a) as a claim does not have to adjudicate it. 00-scope.md's own wording is
             being amended to "at least one" by the architect;
         (b) INV-1's property is quantified over histories where Backward(h) is Available (S16),
             and interleaves actions across independent regions of the test model;
         (c) the B4 INTERLEAVING property is present in the acceptance criterion, not only in a
             disposition table (S14): a decided undo whose availability changed before transition
             never produces a state change unless the new availability is blocked. Needs a driver
             that dispatches two Undos WITHOUT awaiting the first, since Runtime.Dispatch is
             awaitable and a sequential test cannot produce the interleave;
         (d) the S11 property: a multi-event Decide whose FIRST event leaves the projection
             unchanged and whose SECOND moves it produces exactly one recorded step;
         (e) L6's three generated triples (reflexive, symmetric, transitive);
         (f) L5's sequences are long enough to exercise StepForward, not only record;
         (g) SEC-3 (a) and (b) as two separately named tests, (b) against a Conduit-Settings-
             shaped fixture with a live password, undone and redone, inspecting every Step.Model
             in Past AND Future;
         (h) a FIXED SEED CORPUS runs in CI and the failing seed is printed in every assertion
             message;
         (i) each property fails for the right reason when its rule is deliberately broken;
         (j) R4-1 (B8): a decision that is REJECTED and whose error message moves the projection
             produces the same classification as an accepted decision that moves it by the same
             amount — i.e. a record, not a seal. Built on the shape that already exists in
             RuntimeIsolationAndSubscriptionFaultTests.cs:176-190 (Decide returns Err, the error
             message increments the model, Commands.None). The falsifier: revert Decide's Err
             branch to Err(e) and this property must fail with BlockedByWorld;
         (k) R4-3 (B9): an AUTONOMOUS-SOURCE property. The generator's alphabet gains a source
             that delivers without being asked (CountingSubscription's pulse, step 9's mechanism,
             reused here). The shape that must be generated and asserted is: undo, then a delivery
             that moves the projection, then redo — and the assertion is on WHAT THE USER CAN
             OBSERVE about the redo: Forward(h) returns BlockedBySupersedingAction naming the
             delivered message, before the press, and NOT Nothing. This is the property that can
             see a tick destroy a redo; step 9 asserts subscription activity and never history
             content, so without this one nothing in the plan can. It must also be observed to
             fail when record clears Future instead of superseding it;
         (l) R4-5 (S19): HistoryLensLawTests.Scrub_overridden_alone_violates_L5 — a deliberately
             inconsistent fixture policy (whole-model Restore/SameUndoable left at default, Scrub
             overridden to remove a field) MUST fail the L5 property. A lens-law test, not a
             security regression test, and named for what it proves;
         (m) R4-6 (S20): HistorySecurityRegressionTests.Anchor_never_reaches_a_release_path_surface
             — over a Held bracket on the same Conduit-Settings fixture, the anchor's payload
             appears in none of the rendered View output, any HistoryTelemetry span or tag, or any
             MovementAvailability/EdgeState returned by Backward/Forward. Filed BESIDE SEC-3 (a)
             and (b) as a third, explicitly-not-(b) item; it proves the boundary, not safety.
         (n) R4-nav: UrlChanged IN THE GENERATOR'S ALPHABET, admitted only AFTER the first recorded
             step. The shape generated and asserted: record a step, deliver UrlChanged, then assert
             on WHAT THE USER CAN OBSERVE BEFORE EITHER PRESS — Backward(h) AND Forward(h) both
             return BlockedByWorld naming the UrlChanged, not Available and not Nothing. Two
             falsifiers, both of which must be OBSERVED to fail and not merely asserted to hold:
             remove the rule from apply and the property fails with the enveloped UrlChanged
             recording as an ordinary action; and — the user's own falsifier, which is what the
             property exists to make impossible — AN UNDO THAT LANDS ON A MODEL WHOSE URL IS NOT
             THE ONE THE BROWSER SHOWS. The second needs the head's URL as an observable beside the
             model, so this property is the one place in step 8 that reads something outside
             History<TModel>. THE CONTINGENCY IS NO LONGER A RELOCATION (🟡 4, confirmation pass):
             06-spec.md locks
             this assertion inside UndoRedoSpec.cs, and the approved spec is immutable during
             implementation — so if it proves impracticable at the generator level, the route is a
             "// SPEC CONFLICT:" hand-back to the user and a RE-APPROVAL, not a move to step 10's
             composition tests. It should not arise: the spec's own solution never reads the head's
             URL, it models the browser as the last UrlChanged it dispatched, and it says why that
             is honest for this fixture.
             Pre-record navigation is NOT in this property's scope — it is R4-8b/S23 and unchanged.
         (o) R4-anchor (room-security.md, 2026-09-08 follow-up):
             HistorySecurityRegressionTests.Anchor_is_always_the_model_the_bracket_was_opened_against
             — over a Held bracket opened by Hold(m), assert
             held.Anchor is TModel recovered && ReferenceEquals(recovered, m), or value-equality
             where the fixture's model is a record compared by value. Lettered (o) rather than
             inserted after (m) so that (n)'s existing citations still resolve; it belongs in the
             SAME FILE and BESIDE (m), and the two are DELIBERATELY NOT MERGED. (m) proves
             CONFIDENTIALITY — the anchor's payload reaches no release-path surface. This one proves
             IDENTITY — that what Hold stored is the model the bracket was opened against and not
             some other object that merely happens not to leak through those three surfaces. One
             mechanism per proof, named for what it proves, which is how the room has kept adjacent
             tests apart throughout. It is a CORRECTNESS/AVAILABILITY regression test — it guards
             an InvalidCastException and a silent misbinding — so it does NOT take SEC-3 (a)/(b)'s
             naming convention and must not be filed under it.
             WHY IT EXISTS NOW AND DID NOT BEFORE: Movement.Held ships carrying object, not TModel,
             because the locked UndoRedoSpec.cs:399 asserts IsTypeOf<Movement.Held>() non-generically
             and so forces Movement to stay non-generic (09-review-verdict.md finding 5). Under
             Held(TModel Anchor) this identity was compiler-guaranteed and needed no test. Under
             Held(object Anchor) "an anchor holding something that is not the model" is a compilable
             program. An internal constructor (finding 1's criterion, this round) closes construction
             from outside the assembly; it cannot close it from inside, because object accepts
             anything and there is no TModel left to check against. This test is therefore what now
             stands where the compiler used to stand, and step 16's new Open Risk names it by name.
         INV-3's property needs a document comparer that normalises handler command ids and a
         generator over the test program's message type — both are work items inside this step.
 9. [ ] → csharp-dev: INV-7 at runtime level. HistoryTestProgram with model-derived Subscriptions
         + CountingSubscription that records start/stop/deliver AND dispatches autonomously on
         start. Property over generated sequences including Hold/Settle brackets, malformed
         brackets (settle with no hold, hold twice, hold left open) and autonomous deliveries.
         Asserts: (1) per maximal movement, application-keyed start/stop activity equals a single
         reconciliation from the pre-movement set to the settled set; (2) every delivery inside a
         bracket carries a key from the anchor's key set; (3) no application-declared key begins
         with `abies:history:`; (4) R4-7 (S21) — WHILE A HOLD IS OPEN, the reported key set is
         CONSTANT and equal to the anchor's, no matter how many ordinary messages are applied and
         how far Present moves. Revision 3's assertion (4) said "a hold left open followed by an
         ordinary action produces exactly one reconciliation, against the state the run settled
         on", which describes something the design cannot do: a Held run ends on Settle or Clear
         and on nothing else, so there is no state it settled on and the number of reconciliations
         away from the anchor is ZERO. The replacement is stronger and falsifiable, and it makes
         the accepted liveness cost visible in a test rather than only in a guide sentence.
         Generator note for (2) (🟡 3, R4-9): random generation over a small test model may never
         place a state INSIDE a bracket that declares a source neither the anchor nor the
         destination declares — which is the only shape under which (2) can fail. That shape is
         named here and seeded EXPLICITLY in the fixed-seed corpus rather than left to the
         generator. Done when: all four pass, AND both (1) and (2) are observed to FAIL when the
         anchor is deliberately removed. NO TEST IN THIS STEP SLEEPS OR READS A WALL CLOCK.
10. [ ] → csharp-dev: composition tests — WithView<WithHistory<…>, ChromeView, …> renders undo
         AND redo availability plus the sealing cause from the history value;
         WithHistory<WithView<…>, …> also compiles; the three-layer stack compiles. The rendered
         chrome MUST honour the full contract (R4-8c): it dispatches Settle on blur and on
         pointercancel as well as on key-up/pointer-up.
         [R4-ux] THE CHROME CONTRACT GAINS TWO REQUIREMENTS, both render-only, both from
         room-ux.md and neither previously specified: (i) the refusal reason renders as ADJACENT,
         ALWAYS-VISIBLE TEXT — never a tooltip or title-only affordance, which fails on touch and
         is announced inconsistently by screen readers — and the composition test asserts on that
         VISIBLE TEXT, not merely on the disabled state or on MovementRefused's payload
         (room-ux.md q1); (ii) while Movement is Held the chrome renders a VISIBLE
         MOVEMENT-IN-PROGRESS state — a pressed/active affordance on whatever started the hold —
         read from the same Movement value the chrome already reads for INV-6, and a static state
         change rather than an animation (room-ux.md q11(a)).
         [R4-ux] AND THE WORDING IS SETTLED, no longer illustrative. One template across all three
         MovementAvailability refusal cases and both directions — "[Undo/Redo] unavailable —
         [reason]." — varying only the verb and the reason clause, with the same control, position
         and styling in all three cases (room-ux.md q7, q12, q13(c)). The reason clause is
         HUMANIZED: an adopter-owned lookup from the cause's shape to plain language, falling back
         to "something changed since then" where no entry exists. A RAW MESSAGE TYPE NAME IS NEVER
         DEFAULT END-USER TEXT — no Cause.GetType().Name in the rendered document; the raw cause
         stays where SEC-4 already puts it, in telemetry (step 11), and at most in a dev-facing
         detail row that is not the primary text a screen reader announces first (room-ux.md
         q13(b), q6).
         Done when: the rendered document shows a typed reason for BOTH directions before any
         press is attempted — "Undo unavailable — the page finished loading." / "Redo unavailable
         — the page finished loading." on the BlockedByWorld path, which is INV-6 in use in both
         directions (S9); AND it shows the superseded reason — "Redo unavailable — your later
         action replaced this." — on the BlockedBySupersedingAction path (R4-2), which is the
         rendering that makes B9's silence visible, and NOT "superseded by FastTick", which
         room-ux.md q13(b) rules out as default copy; AND (S32) it shows the NAVIGATION case in
         both directions after a post-record UrlChanged IN ITS OWN WORDS — "Undo unavailable — you
         navigated away from this page." / "Redo unavailable — you navigated to a different page."
         (room-ux.md q9/q13(d)) — the same BlockedByWorld path, but the cause is a navigation the
         user performed DELIBERATELY, so "the page changed while you were editing (UrlChanged)"
         MUST NOT SHIP; AND NO proactive signal fires at the moment of the seal — the
         always-visible disabled-plus-reason state IS the signal (room-ux.md q13(d)); AND a blur
         delivered mid-bracket settles the run.
11. [ ] → csharp-dev: HistoryTelemetry — spans on undo, redo, hold, settle, clear and refusal
         only, never on the pass-through path; cause tags are Cause.GetType().Name only (SEC-4).
         Done when: an in-process ActivityListener shows one span per movement, one per settle,
         zero for a hundred ordinary dispatches, and no span tag contains a cause payload; and a
         refusal span carries the direction and the reason type name, so the whole-model policy's
         reach limit is diagnosable from traces without reading code (S10).
12. [x] → ux-expert: the thirteen questions below (q13 is new, from R4-2; q11 sharpened by R4-8a
         and R4-8c; q9 and q13(d) carry a CORRECTED PREMISE from R4-nav — S32 — and this step is
         dispatched in wave 0, BEFORE step 5 fixes the defaults, so the correction must travel
         with the dispatch and not follow it: no new question and no new spawn, the questions
         exist and their premise changed). Done when: defaults for steps 5, 6 and 14 are settled and the guide has a
         keyboard-affordance section it can state as guidance.
13. [ ] → tech-writer: ADR-030, docs/concepts/undo-redo.md, docs/guides/adding-undo.md, API
         reference, ADR-008 line 85 amendment, CHANGELOG + doc-sync verification (ADR-024 heads
         table, ADR-025 unamended statement, tech-stack.md). Must carry SEC-5 verbatim in scope,
         with Scrub shown BESIDE the projection in the same worked example, AND the negative form
         under it — override Scrub alone, and the L5 test that catches it (R4-5). Must state, as
         first-class behaviour and not a footnote:
         - what "the world" is — a command's feedback, WITH ONE EXCEPTION THE SAME TABLE CARRIES:
           an incoming UrlChanged once Origin is Established (R4-nav, next bullet but one). A
           subscription-delivered message is otherwise a user action; a rejected decision is the
           application's own response (R4-1, R4-2, S30). Do not write "and nothing else" — on the
           browser head an incoming navigation IS a subscription delivery, so the unqualified
           sentence classifies one message two ways;
         - that an application with a model-mutating subscription MUST declare a projection or
           redo will not function, with the same weight as the reach statement (R4-2);
         - the whole-model policy's reach, WITH ALL THREE BOUNDS AT THE SAME WEIGHT (S30): the
           last command feedback (S10); Depth ÷ a model-mutating subscription's rate in wall-clock
           seconds, twenty-five in SubscriptionsDemo's shape (S25(b)); and the last incoming
           navigation, which in a routed application is usually the binding one — in Conduit, undo
           reaches back to the current page and no further (R4-nav);
         - INCOMING NAVIGATION, as first-class behaviour in ADR-030 and in
           docs/guides/adding-undo.md and not as a footnote (S32, B10): after the first recorded
           step a browser back or forward press seals BOTH incident edges, so undo and redo both
           refuse by name rather than restoring a model without its URL; before it, navigation
           re-bases and is never an undo stop; AND the APPLICATION OBLIGATION — the guarantee
           holds for navigation delivered as Picea.Abies.UrlChanged, and an application that
           routes the browser's location through its own message type must classify it as a
           commitment itself, because the framework cannot see it. State it beside L1–L6 and the
           independent-reachability assumption, in the same register, since it is the same kind of
           thing (§ The lens, and its six obligations; 06-spec.md's lens-law section carries the
           same obligation). ALSO: bring README.md:183-190 into line with Conduit and the
           tutorials — it passes url => new UrlChangedTo(url), the one shape that defeats the
           rule, and it is the front-page example an adopter copies. It is not itself a
           WithHistory adopter, so say which idiom is which rather than implying the README was
           broken;
         - [R4-ux] THE REFUSAL WORDING, settled by room-ux.md and no longer deferred to it. ONE
           TEMPLATE across all three refusal causes and both directions — "[Undo/Redo] unavailable
           — [reason]." — varying only the verb and the reason clause, with the same visual
           treatment in all three cases (q7, q12, q13(c)). The reason clause is HUMANIZED, through
           an adopter-supplied cause-to-string lookup, with "something changed since then" as the
           honest fallback; the guide's worked example shows THAT pattern and not
           Cause.GetType().Name, and any raw-identifier example that survives is annotated as
           ILLUSTRATIVE OF MECHANISM, NOT RECOMMENDED PRODUCTION COPY (q6, q13(b)). A raw message
           type name is never default end-user text: the raw cause is for telemetry (SEC-4, step
           11) and at most a dev-facing detail row. "Superseded by FastTick" does not ship; the
           default is "Redo unavailable — your later action replaced this." (q13(b)). And the
           NAVIGATION refusal gets ITS OWN WORDS, because a user who pressed Back changed the page
           ON PURPOSE and the sentence written for ProfileLoaded — "the page changed while you were
           editing" — is a strange, faintly accusatory thing to say to them: "Undo unavailable —
           you navigated away from this page." / "Redo unavailable — you navigated to a different
           page." (q9, q13(d), S32). No proactive notice fires at the moment of the seal — the
           always-visible disabled-plus-reason state is the signal, and interrupting every
           back/forward press with a notice about a history feature is the failure mode this squad
           pushes back on (q13(d));
         - the chrome's Hold/Settle contract and who owns it — INCLUDING blur and pointercancel,
           Clear as the one in-band rescue, and that two chromes cannot hold independently
           (R4-8c, 🟡 2); AND [R4-ux] the contract's two render-only additions that step 10 now
           tests, stated in the same chrome-contract section: the refusal reason is adjacent,
           always-visible text — never tooltip- or title-only, and in the accessible
           name/description path (q1) — and a visible movement-in-progress state is rendered for as
           long as a Held bracket is open, static rather than animated (q11(a));
         - that the anchor held during a bracket is never scrubbed, so closing promptly is a
           data-minimisation practice and not only a liveness one (R4-6);
         - the native head's lack of key-up/pointer-up, and — in the head-coverage table — that no
           button chrome can bracket a run, so INV-7's protection is confined to a held key and a
           scrubber and per-press reconciliation is what the shown chrome gets (R4-8a);
         - the effect-interleaving AND error semantics of a wrapped multi-event decision (R4-4);
         - the JsonPolymorphic obligation naming HistoryEvent.Enveloped as well as HistoryRedacted,
           since Enveloped is the type that carries the inner payloads (🟡 4).
         Done when: every public type has reference documentation and no existing doc contradicts
         the new behaviour.
14. [ ] → csharp-dev: OPT-IN coalescing window (TPolicy.CoalesceWindow + TPolicy.Time) and
         HistoryMessage.SplitStep — renamed from revision 2's HistoryMessage.Barrier, which was
         the third distinct meaning of "barrier" in one namespace (S17). SEPARABLE — may be cut.
         Done when: two keystrokes inside the window make one undoable step, a SplitStep splits
         them, the default path never reads the clock, and the test uses a test-owned
         deterministic TimeProvider rather than sleeping.
15. [ ] → csharp-dev: report-only — is Picea's AutomatonRuntime<…>.Reset(TState) public? A scratch
         console project created OUTSIDE the solution directory, referencing the Picea package,
         compiled and discarded. Nothing enters the repository. Done when: the answer is recorded
         in 07-handoff.md.
16. [ ] → security-expert: threat-model rows 1 and 2 (split High/server-side, Medium/client-only),
         the Open Risks entry for the un-retrofitted SerializeMessageArgs leak, and Trust Boundary
         7 — Runtime state-retention boundary (SEC-6). Rows must state that Scrub and
         SensitiveCause are independent mitigations of different fields, neither subsuming the
         other. R4-6: Trust Boundary 7 gains the clause naming Movement.Held(Anchor) as a bounded,
         unscrubbed live-model retention site, and the EXISTING DEBUG-snapshot row gains a second
         bullet naming it as a distinct reachable site — same severity, same "⚠️ Partially
         mitigated" status, NOT a new row. Plus the hardening-backlog entries: SEC-7's
         SerializeMessageArgs retrofit, and the fast-follow serialization-boundary substitution for
         Held<TModel> (R4-6), logged in the same register and not before the types land.
         R4-anchor (room-security.md, 2026-09-08 follow-up), TWO ADDITIONS AND EXPLICITLY NO ROW:
         (i) TB7's Held(Anchor) clause gains a PROVENANCE sentence, appended to the clause R4-6
             already puts there rather than replacing it — the anchor is held as object and not
             TModel, because the locked spec's non-generic IsTypeOf<Movement.Held>() forces Movement
             to stay non-generic (09-review-verdict.md finding 5); the "live model" property is
             enforced by INTERNAL-ONLY CONSTRUCTION AND A SINGLE INTERNAL CALL SITE, not by the type
             system, and is REGRESSION-TESTED RATHER THAN COMPILER-CHECKED (step 8(o)). The clause's
             existing sentence stays true after the erasure; what it stops being is true BY
             CONSTRUCTION, and a boundary that reads as compiler-backed when it is test-backed
             over-promises. Same severity, same status — a provenance note on an existing clause,
             not a new threat.
         (ii) ONE NEW OPEN RISK, LOW SEVERITY, owner csharp-dev, stated explicitly rather than left
             implicit: a future internal change to WithHistory could construct Movement.Held with a
             value that is not the bracket's model, undetected by the compiler, caught only by
             Anchor_is_always_the_model_the_bracket_was_opened_against if that test is written and
             kept. Mitigated by that test rather than by a type-level fix, since re-introducing a
             typed anchor is foreclosed by the locked spec.
         NO NEW ROW IN THREATS AND MITIGATIONS, no change to SEC-3(b)'s text, and no change to the
         S20 conclusion's wording — the exposure surfaces (View, telemetry, EdgeState, DEBUG export)
         are unchanged by the erasure. What moved is the MECHANISM THAT KEEPS THE ANCHOR HONEST,
         from the type system to a test, and (i) and (ii) are where that move is recorded. Done when:
         threat-model.md names the boundary and both threats, each with its mitigation and its
         test, the anchor appears as a named site rather than an implied one, and TB7's anchor clause
         says which of its guarantees is compiler-backed and which is test-backed.
```

---

## Parallelisable

- **Start together:** 12 (`ux-expert`) and 1–2 (`csharp-dev`). The UX answers land before step 5
  fixes the policy defaults, which is why 12 starts first.
- **1 ∥ 2** — independent types.
- **3, 4, 5** all depend on 2 and are independent of one another.
- **6** depends on 3, 4, 5. Everything downstream depends on 6.
- **7** immediately after 6, **before 8–11** (S5's re-sequencing; revision 2's todo text said
  "before 7–11", Critic 🟡 8 — corrected).
- **8 ∥ 9 ∥ 10 ∥ 11** after 7.
- **13** (`tech-writer`) starts as soon as 6 lands and runs alongside 7–11 — docs ship with code.
- **16** (`security-expert`) starts as soon as 4 lands; the threat-model rows need the type names,
  not the implementation.
- **14** after 8; may be cut.
- **15** anywhere; blocks nothing.
- **Terminal:** `reviewer-blind` → `reviewer-reconcile`. No path from "code changed" to "done" goes
  anywhere else.

### Delivery — the PR cut

`.github/workflows/pr-validation.yml:211` sets `hardLimit = 1500` changed non-doc lines and calls
`core.setFailed`; the maintenance-only escape at `:193-208` covers `docs/**`, the named root files
and `*.md` and nothing else. Seven sequential PRs to `main`, each terminating at the review pair,
**preceded by the precondition spec commit as PR 0 (`[R4-spec-commit]`)** — the seven are unchanged
in content and numbering, so every reference to "PR 3" elsewhere in this artifact still resolves:

| PR | Steps | Note |
|---|---|---|
| **0** | — (precondition) | **`[R4-spec-commit]`** `Picea.Abies.Tests/History/UndoRedoSpec.cs` + `Picea.Abies.Tests/SpecAttribute.cs` as the approval commit, alone, plus `<Compile Remove="History\UndoRedoSpec.cs" />` **and `<None Include="History\UndoRedoSpec.cs" />`** in `Picea.Abies.Tests.csproj` (both items landed; the `None Include` was added during PR 0's review for IDE-tree visibility, and step 6 removes both). Three files, no framework code, well under the line gate — **as planned, and true of the approval commit `b39ac58` only**. **As landed:** PR **#361**, squash merge `70d9ae58b142039f274d4f124902a9a60b6a6186`; the branch also carried the design-record and amendment-docs commits, so the PR measured **40 files / 7,669 changed lines** and `Check PR Size` **failed** the 1500-line hard limit (non-required check; merged anyway). PR 0 is therefore **not** an instance of the line-gate argument that shapes PRs 1–7 below — those are code PRs and the argument stands for them; PR 0 escaped it by carrying docs the plan did not schedule into it. **State in the PR body** that the spec is deliberately excluded from compilation until step 6 removes the exclusion, and that this is what keeps the Lock's git-history check meaningful — the check is never waived, so the body asks the reviewer for nothing. `reviewer-blind` reviews the spec as a specification, which is the one PR where that is the whole job. |
| 1 | 1, 2 | Additive only; the new types are unreferenced until PR 3. **State that in the PR body** so `reviewer-blind` is not surprised by dead code — it is reviewing a foundation, not an orphan. |
| 2 | 3, 4, 5 | Same note. |
| 3 | 6 | Completes the vertical; the largest single PR and the one to keep clean. |
| 4 | 7 | Benchmarks + the performance report. |
| 5 | 8 | Properties + generators. Split into 5a (INV-1…INV-4, generators) and 5b (INV-5, INV-6, L1–L6, SEC-3 a+b) if 5 approaches the limit. |
| 6 | 9, 10, 11 | Runtime-level INV-7, composition, telemetry. **Pre-authorised split into 6a (step 9, INV-7 at runtime level with the malformed brackets and the mutation check) and 6b (steps 10 and 11)** if it approaches the limit — 🟡 5, `[R4-9]`. Step 9 grew again this revision (assertion (4) rewritten, the named corpus shape) and it is the step most likely to push PR 6 over, so it gets the same escape hatch PR 5 already had rather than discovering the need at push time. |
| 7 | 13, 16 | Docs and threat model — `docs/**` and `*.md`, so exempt from the line gate. |

Step 14 is its own PR if it survives; step 15 commits nothing.

---

## Unknowns / Research Needed

| # | Unknown | What would resolve it |
|---|---|---|
| 1 | **Nothing is measured**, and the CI gates cannot measure it: no demo adopts `WithHistory`, so js-framework-benchmark exercises a non-adopting application and its 5% threshold cannot regress on account of this work whatever it costs an adopter. | Step 7 — BenchmarkDotNet A/B with a **derived** budget, per-assembly IL, and a stated sentence that the CI gate is silent here. |
| 2 | **Which `Past` representation is cheapest at Depth 100.** The *bound* is settled: a persistent structure cannot carry an amortised bound (Okasaki ch. 5–6) and this one **is** used persistently. Restated: array copy-on-write, worst-case O(Depth), Depth bounded, no `System.Collections.Immutable` dependency. What remains open is the constant. | Steps 1 and 7 together. |
| 3 | **Property-testing tooling.** Settled: hand-rolled seeded generators, no new dependency. Determinism is closed by step 8(h). | Closed. |
| 4 | **DOM-owned state** — accepted out of scope (INV-3 as amended). Re-opened only if `ux-expert` says the minimum bookmark is mandatory. | **Closed — not re-opened.** `room-ux.md` q4: accept the exclusion for v1; a *partial* bookmark is worse than none, because it teaches a promise it then breaks unpredictably. One guide sentence follows, and it is step 13's. |
| 5 | **`AutomatonRuntime.Reset` accessibility.** Changes nothing in this pass; changes the economics of the deferred H2 pass. | Step 15. |
| 6 | ~~INV-7's scope clause~~ | **Closed.** `00-scope.md`'s **INV-7** already carries the `abies:history:` clause (cited by id, not by line — 🟡 3 of the confirmation pass), and this revision no longer needs it: the framework declares no subscription, so INV-7 holds unqualified. Two *new* amendments are proposed instead — INV-7's definition of a movement, and INV-1's availability precondition — both worded above for lifting. |
| 7 | ~~Whether the default `SettleWindow` of 250 ms is right~~ | **Closed by deletion, and its successor is now answered.** There is no window. `ux-expert` q11 replaced it and `room-ux.md` q11 answers both halves: (a) keep the `blur`/`pointercancel` auto-settle **and** render the open bracket visibly for its duration; (b) per-press reconciliation is acceptable for the chrome this pass actually ships — do not re-open the architecture question. Both land in step 10 as `[R4-ux]`. |
| 8 | ~~Whether the effect-interleaving change for multi-event decisions is acceptable~~ | **Closed.** Attacked by the Critic at S18 and it survived; the user accepted it. What the attack surfaced is a **second** difference — a failing command no longer prevents later events from being applied — which is now spec line 7 and step 6's Done-when (`[R4-4]`), and which the user is asked to confirm in the pause because they accepted the ordering half without it. |
| 9 | **Whether a refusal naming a message type is legible to a person.** `[R4-2]` makes *"superseded by `FastTick`"* the default redo refusal in any application with a model-mutating subscription, and `FastTick` is an identifier the user has never seen. This is the same question q6 half-answered for step labels, arriving now at the refusal. | **Answered: no** — `room-ux.md` q13(b), with q7 and q6. A raw `Cause.GetType().Name` is never default end-user text; the default is a humanized reason clause under one template, with *"something changed since then"* as the fallback, and the raw cause kept for telemetry (SEC-4) and dev-facing detail only. It changes the wording in steps 5, 10 and 13 — tagged `[R4-ux]` at each — and not the mechanism: `Cause` is retained either way. |

**What `ux-expert` must answer** (step 12) — six from revision 1, five from the Critic, two mine.

> **`[R4-ux]` Step 12 is ANSWERED.** Dispatched in wave 0 and returned 2026-09-08 as
> `.squad/design/undo-redo/room-ux.md`, against the corrected `[R4-nav]`/S32 premise for q9 and
> q13(d). Twelve answered, q10 withdrawn. **Two 🔴 overrides**, folded in above as `[R4-ux]`: the
> illustrative post-navigation sentence *"the page changed while you were editing (`UrlChanged`)"*
> **must not ship** — the navigation refusal gets its own direction-specific words, with no
> proactive signal at the seal (q9/q13(d), steps 5(vii), 10, 13); and a raw message type name —
> *"superseded by `FastTick`"*, or any `Cause.GetType().Name` — is **never default end-user text**,
> the default being a humanized reason clause with the raw cause kept for telemetry per SEC-4
> (q13(b)/q7/q6, steps 5(vii), 10, 13). **Two additions to step 10's chrome contract**, both
> render-only: the refusal reason as adjacent always-visible text, never tooltip-only (q1), and a
> visible movement-in-progress state for the duration of a `Held` bracket (q11(a)). Everything else
> is **confirmed as designed**. The step's own Done-when is met: defaults for steps 5, 6 and 14 are
> settled — **step 14's coalescing stays off by default** (q5, unchanged) and step 14 names no
> user-facing wording, so it is untouched by this fold — and the guide has its keyboard-affordance
> guidance (q3). The questions below are left as asked, so `room-ux.md`'s answers read against the
> premise they were answered on.

1. What the effect boundary looks like to a user — *before* the attempt (the disabled/annotated
   affordance driven by `BlockedByEffect(Cause)`) and *at* the attempt (`MovementRefused`).
2. Whether discarding the redo branch on divergence needs any communication at all.
   **Mechanically answered by `[R4-2]`** — the branch is no longer discarded, it is preserved and
   refused by name — so this becomes: is the refusal enough, or does the moment of supersession
   itself want a signal? See q13.
3. Keyboard affordances across the three heads. Recommendation: the framework ships **no** key
   binding — the browser's native text-input undo stack and WinUI `TextBox`'s own undo both collide
   with a global `Ctrl+Z` (w3c/editing#150). Confirm, and say what the guide should tell an
   application to do instead.
4. Whether DOM-owned state out of scope is acceptable for v1, and if not, the minimum bookmark.
5. Whether coalescing off by default is right, given per-keystroke undo is the classic
   user-rejected failure and 500 ms is the industry's converged number.
6. Whether `Step.Cause` should surface as a label ("Undo *Delete comment*", Qt's `text()`).
   **Half-answered by the security room:** a redacted step renders as "Undo (sensitive change)" and
   never the value. Note that under the fold, `Cause` is now the **incoming message**, which is what
   a label actually wants — the non-sensitive case is still yours.
7. **(Critic, re-framed)** What the user should see when undo or redo is blocked because the world
   moved. **S10 makes this the common case, not the uncommon one** — under the whole-model policy an
   application with any background traffic hits it constantly. Answer it as the default experience.
8. **(Critic)** Whether pressing undo at a blocked boundary may destroy redo. **Fixed mechanically**
   — a refusal is a `pass` and never clears `Future`. Confirmation only.
9. **(Critic)** Whether the first undo of a freshly loaded page returning to an unrouted state is
   acceptable. **Fixed mechanically** by origin re-basing. Confirmation, plus the stated exception
   for a head with no initial URL. **Premise corrected before this question is asked (S32):** this
   question was written when navigation *after* the first recorded step behaved like any other
   message. It does not any more — `[R4-nav]` seals both edges, so a browser back or forward press
   makes undo **and** redo refuse by name. Both halves are in front of `ux-expert` when q9 is
   answered: the pre-record half (re-basing, `[R4-8b]`/S23) and the post-record half (`[R4-nav]`).
   This step is dispatched in wave 0, **before** step 5 fixes the policy defaults, so the corrected
   premise has to travel with the dispatch rather than arrive after it.
10. **(mine, revision 2)** ~~What the default `SettleWindow` should be~~ — withdrawn; there is no
    window.
11. **(mine, revision 3; sharpened by the Critic at S22/S24)** What a **bracketed run** should look
    like to a person: should the chrome show that a movement is open, and what closes it on a touch
    device? **Plus the two things revision 3 did not ask.** (a) The lost-key-up case: a `keyup` is
    not delivered to an element that has lost focus, so alt-tab or a click elsewhere strands the
    bracket — is settling on `blur`/`pointercancel` the right behaviour, or should a stranded
    bracket be visible to the user? (b) **The real question behind S22:** no button chrome can
    bracket a run, and the framework has no window-level key handling, so the chrome the guide will
    actually show gets **per-press reconciliation** — five clicks, five reconciliations, sources
    starting and stopping at each intermediate state. **Is that acceptable for the chrome we are
    going to show people?** If it is not, the answer is a scrubber or an architecture decision, and
    both are separate passes.
12. **(mine, revision 3)** `MovementRefused` now carries a `Direction`. Should undo and redo refuse
    with the same words, or does "you cannot go forward past what the server told you" need
    different language from "you cannot go back past it"?
13. **(Critic, third pass — new, from B9)** A background message — a timer tick, a subscription
    delivery — now **supersedes** the redo branch the user was walking, and `Forward(h)` refuses by
    name (*"superseded by `FastTick`"*). Three sub-questions, and they are the ones that decide
    whether `[R4-2]` earns its case. (a) Is a refusal-with-a-reason the right treatment, or should
    supersession be silent the way every text editor's is? (b) Does *"superseded by `FastTick`"*
    mean anything to a person, given the cause is a message type name the user has never seen? (c)
    Should the three blocked reasons — effect, world, supersession — read as three distinct
    messages, or is "you cannot redo, because things moved on" one message with three internal
    causes? **(d) — added with the same premise correction as q9 (S32).** The *world* refusal now
    has two very different causes behind one wording. `ProfileLoaded` is something that happened
    **to** the user; a post-record `UrlChanged` is something the user did **on purpose**, by
    pressing Back. *"Blocked — the page changed while you were editing (`UrlChanged`)"* is a
    strange sentence to show someone who changed the page themselves. Does the navigation refusal
    need its own words, and does the moment of the seal want a signal of its own? Step 10 renders
    whatever this answers, and step 13 writes it.

**What `performance-engineer` must answer** (step 7): as listed in the step, with the three
Critic-added shapes folded in (`SameUndoable` as O(largest text field), the withdrawn settle-source
measurement, and the sequencing note that the measurement only makes sense after the movement design
is settled — which it now is). **Two additions this revision.** From the Critic's third pass: the
fold allocates a `Command.Batch` per multi-event decision and saves N−1 renders; both are in the same
measurement and neither is likely to matter, but the report must say **which way it came out** in one
sentence rather than leaving the trade unquantified. From `[R4-2]`: the retained-graph measurement
now covers a **superseded** `Future` branch as well as `Past`, since the ceiling moved from `Depth`
to `2 × Depth` retained models.

---

## Spec obligations — what `06-spec.md` must carry beyond one property per invariant id

Gathered here rather than scattered, because four of the nine items say *"and it goes in
`06-spec.md`"* and `spec-author` reads one artifact. Nothing here is new; it is the same content,
collected.

**S31, confirmation pass — this table is now sixteen lines and is the same sequence as
`06-spec.md`'s.** It carried thirteen; the spec's carries sixteen, because S26 (the superseded
branch) and S25(a) (a message arriving during a bracket) were routed from `05-critic.md` **straight
into `06-spec.md`** and never gained a plan-side row. The result was that *"obligation 14"* named
two different behaviours in the two files, and that the answer to `00-scope.md`'s open question 3
had no row here at all — which is S26's own failure, one artifact further downstream, and exactly
what `reviewer-reconcile` would have ticked off against the wrong line. **16 is right**: the spec's
table is the union, and it is the one the spec lock and the review read. Rows **14** and **15** are
added below pointing at their source in `05-critic.md`, and the navigation row is renumbered from 14
to **16**, matching `06-spec.md:1231-1233`. The header cross-reference at the top of this artifact
is corrected with it.

| # | Spec line | Source |
|---|---|---|
| 1 | One property per `INV-1` … `INV-7`, per `00-scope.md`. | scope |
| 2 | L1–L6 as the **application's** obligation, with the reachability assumption named as an assumption the framework cannot test. | Cleanness 1 |
| 3 | *"The world" is a command's feedback* — **with one exception this same table carries: an incoming `UrlChanged` once `Origin` is `Established` (line 16).** A subscription-delivered message is otherwise a user action; a rejected decision is the application's own response and classifies like an accepted one. **Not *"and nothing else"***: on the browser head an incoming navigation *is* a subscription delivery, so the unqualified sentence classifies one message on one path two ways (S30). | `[R4-1]`, `[R4-2]`, `[R4-nav]` |
| 4 | An application with a model-mutating subscription **must** declare a projection or redo will not function — and, under the whole-model policy, undo's reach also expires on a wall clock (line 5, bound (ii)). | `[R4-2]`, S25(b) |
| 5 | The whole-model policy's **reach** has **three** bounds, stated together and at the same weight: (i) the last **command feedback**; (ii) **`Depth` ÷ a model-mutating subscription's rate, in wall-clock seconds** — twenty-five in `SubscriptionsDemo`'s shape; (iii) the last **incoming navigation**, which in a routed application is usually the binding one — in Conduit, undo reaches back to the current page and no further. | S10, S25(b), `[R4-nav]` (S30) |
| 6 | A wrapped multi-event decision **interleaves effects differently** from an unwrapped one: all events are applied, then the batched commands are interpreted in order. | gate-4 decision |
| 7 | And **fails differently**: a failing command skips later *commands*, not later *events* — where an unwrapped program would have applied only the first event and issued no later effects. | `[R4-4]` (S18) |
| 8 | For an **un-bracketed press** INV-7 is satisfied *by definition* — a complete movement has no states passed through — and no button chrome can bracket a run, so the shown chrome gets per-press reconciliation. | `[R4-8a]` (S22) |
| 9 | **While a hold is open, the application's live subscriptions do not track its model at all.** Not an INV-7 violation — INV-7 governs transit — but a state in which the framework's central subscription contract (*"which subscriptions are active is derived from the model"*, `00-scope.md` § *Two consequences of that shape bind this work*) is **suspended**, for as long as the chrome likes. Stated as plainly as the reach limit. | `[R4-7]` (S21) |
| 10 | Navigation before the session's first recorded step never becomes an undo stop, on any head. | `[R4-8b]` (S23) |
| 11 | `Clear` makes previously-available undo unavailable without moving the cursor, and **also settles an open bracket** — the one in-band rescue for a stranded hold. | Cleanness 7, `[R4-8c]` |
| 12 | **Undo remains dispatchable out of a terminal state** and can resurrect a terminated program. | 🟡 6, `[R4-9]` |
| 13 | Under **InteractiveAuto** the history begins at the client handoff. | head coverage |
| **14** | **Acting after an undo does not discard the forward branch.** The branch is preserved and its first edge sealed with `SupersededByNewAction(cause)`; `Forward(h)` returns `BlockedBySupersedingAction(cause)` before the press, naming the message that superseded it. The branch is never crossed, and `Future` is trimmed to `Depth` at its far end. **This is the answer to `00-scope.md` open question 3**, and it had no row in this table until the confirmation pass. | `05-critic.md` S26; § *The third edge state* `[R4-2]` |
| **15** | A message arriving **during a bracket** does not end the run: it is applied to `Present`, and it may seal the history **or record a new undo stop whose model is a transit state and supersede the forward branch, mid-movement** — all three typed. | `05-critic.md` S25(a); § *Movement* |
| **16** | **Incoming navigation after the first recorded step seals both incident edges.** A browser back or forward press delivers `UrlChanged`, which classifies as the world: `Backward(h)` and `Forward(h)` both return `BlockedByWorld(UrlChanged)`, in advance and by name. Undo therefore never restores a model without its URL, and the browser's stack cannot diverge from the application's history unobserved. Pairs with line 10, which governs the pre-record half and is unchanged. **Carries an application obligation of the same kind as line 2's** (B10): the guarantee holds for navigation delivered as `Picea.Abies.UrlChanged`, and an application that routes the browser's location through its own message type must classify it as a commitment itself, because the framework cannot see it — `06-spec.md` states it in the lens-law section beside L1–L6. | `[R4-nav]`, B10 |

## Cleanness Compromises

1. **The projection lens trades one guarantee for six laws — invoked, by user decision at gate 3.**
   With the whole-model lens (the default), undo *moves* to a remembered model and INV-2 holds by
   construction. With an application-supplied lens, undo *computes* `Restore(remembered, present)`,
   and INV-2 holds up to L1–L6 **plus** an assumption the laws do not capture: that the projection
   and its complement are independently reachable in the application's transition system. A lens
   satisfying every law over a model whose two factors are *not* independently reachable can still
   land undo on a state no ordinary sequence produced. **That residual is not property-testable by
   the framework** — it is a statement about the application's own reachable set. The guide must say
   so, and `06-spec.md` must carry L1–L6 as the application's obligation with the reachability
   assumption named as an assumption.
2. **A3(b) — "a commitment is crossable iff the application names an inverse" — remains excluded**
   (gate-3 decision, unchanged).
3. **A bracketed run has no automatic end.** Deleting rule 2 makes INV-7 exactly true through both of
   B5's scenarios and puts a liveness obligation on any chrome that dispatches `Hold`. Bounded,
   opt-in, visible in `h.Movement`, stated in the guide as a contract. **This is the one place where
   revision 3 chose an obligation over a guarantee, and it is the choice most worth arguing with.**
4. **Folding a decision into one event changes effect interleaving *and error semantics* for
   multi-event decisions.** Argued above as more correct rather than merely acceptable, but it *is*
   a behavioural difference between wrapped and unwrapped programs — **two** differences, and the
   user accepted the first without being told the second (`[R4-4]`, S18). Two spec lines, step 6's
   Done-when, and a sentence in the pause question. Unknown 8 is now closed by the Critic having
   attacked it and by the user having accepted it; what replaces it is the disclosure.
5. **Impurity for coalescing (step 14) — unchanged and still opt-in.** With `SettleWindow` deleted,
   step 14 is now the **only** place a clock appears anywhere in the design, and it is off by
   default and separable.
6. **The adoption cliff, for the record.** `TModel` becomes `History<TModel>` for `Runtime.Model` and
   for every test that asserts on the model — redux-undo's most-reported integration complaint.
   Application **views** are untouched. Not severe enough to invoke the ergonomics exception; the
   alternative that avoids it (B3) trades the framework's INV-2 guarantee for application-authored
   inverses. Recorded, not invoked.
7. **`HistoryMessage.Clear` is a public, history-destroying operation.** Recomputed in `Transition`
   like the other movements; `Decide` returns `Ok([])` when there is nothing to clear. It still has
   no invariant of its own and no confirmation story — deliberately: confirmation is the
   application's, and the spec states plainly that `Clear` makes previously-available undo
   unavailable without moving the cursor.
8. **The whole-model policy's reach is short in an application with a world**, and no amount of
   framework cleverness fixes it without guessing. Recorded as designed behaviour, with a typed
   refusal, an advance answer, a trace signal and a two-line fix — not as a defect and not as a
   compromise I am asking permission for. § *The default policy* is the argument.
9. **`[R4-2]` A superseded branch is retained and can never be crossed.** This is the objection
   revision 3 levelled at alternative (A) — retention for nothing — now accepted deliberately in one
   place, in exchange for the refusal being answerable in advance by name. Bounded at `Depth` per
   stack, scrubbed like every other retained model, and measured at step 7. **User decision, not
   mine, and it is the one place revision 4 spends memory to buy diagnosability.**
10. **`[R4-8a]` INV-7's protection is true and empty for the chrome the guide will show.** Two
    buttons cannot bracket a run, so per-press reconciliation is what an ordinary application gets,
    and the invariant is satisfied by definition rather than by mechanism. Carried as a consequence
    of the user's decision to define a movement by input rather than by a timer — that decision
    removed a falsifier which applied to every application, and this is its price. Stated in
    `06-spec.md`, in ADR-030's consequences and in the guide; **not** silently absorbed, and put to
    `ux-expert` as q11(b) rather than answered here.
11. **`[R4-6]` The held anchor is a live, unscrubbed model, deliberately.** Scrubbing it would break
    the INV-7 mechanism it exists for; making it unreachable needs a mechanism this loop is closed
    to. Named in Trust Boundary 7 and on the existing DEBUG-snapshot row, bounded by the bracket,
    proved not to reach any release-path surface by its own test, and logged for fast-follow.
    `security-expert`'s call, recorded as a compromise rather than as a solution.

**Corrections to revision 3's own text**, made rather than glossed — three sentences it asserted
that are false: *"There is no timer to rescue it and, by design, no world event to rescue it
either"* (`Clear` settles, and `blur`/`pointercancel` are available to the chrome — S24); *"one lost
undo stop, only ever the first message of a session, only on a head with no initial URL"* (wrong on
all three counts — S23); and *"a typed refusal naming `FastTick`… reach collapses to roughly one
tick"* for `SubscriptionsDemo` (there was no refusal at all on that path; redo was destroyed
silently — B9). Revision 3 earned a good verdict partly by saying out loud where its own numbers had
moved; these three are the same discipline applied to itself.

**Corrections to inherited material**, made rather than glossed: `RingBuffer<T>` is mutable and
cannot be reused; Track B's "`AutomatonRuntime.State` is not ours to make settable" understates the
package; revision 1's "wrapping the other way does not type-check" was false; revision 1's
"amortised O(1)" was unachievable for a persistently-used structure; **revision 2's "the transit
hold means no application subscription starts, stops or is torn down" claimed two of INV-7's three
verbs and silently dropped the third**; and **revision 2's `BlockedByEffect(Peek().Cause)` named the
message that created the step rather than the message that sealed it.**

---

## Disposition of the Critic's **third pass** — the nine items

| Id | Item | Disposition |
|---|---|---|
| **B8** | `Decide`'s error reaches `Transition` bare and is classified as the world | **Closed by the user's decision 2, `[R4-1]`.** One line: `Err(e) -> Err(HistoryEvent.Enveloped(m, [e]))` in the delegating branch only; the wrapper's own `Err(MovementRefused …)` stays bare. A rejection now classifies exactly like an acceptance. The third-`EdgeState` alternative (`SealedByDecision`) is **not** taken and the reason is written down. Step 8(j), with the falsifier named (revert the line, the property must fail with `BlockedByWorld`). |
| **B9** | A subscription message records and clears `Future`; the narrative says the opposite | **Closed by the user's decision 1, option (b), `[R4-2]`.** `EdgeState.SupersededByNewAction(Cause)`; `record` preserves the branch and seals its first edge; `Forward(h)` refuses by name in advance. § *The default policy* is rewritten to what the mechanism does, the `SubscriptionsDemo` paragraph replaced, and the rewrite carried into step 5's docs, § *Spec obligations*, ADR-030, the guide and `ux-expert` q13. Gate 2's rule becomes *"refuse across the superseded branch"*, with the reason the original rule existed shown not to apply to a sealed edge. Three costs stated: retention, its `2 × Depth` ceiling, and that **(b) buys diagnosability, not reach**. |
| **B9** | No property can observe a tick destroying a redo | **Closed, `[R4-3]`.** Step 8(k): an autonomous source in step 8's alphabet, the undo→delivery→redo shape generated explicitly, and the assertion on what the **user can observe** — `BlockedBySupersedingAction`, not `Nothing`. Observed to fail when `record` clears instead of superseding. This is the fourth time in the pass a property was planned in a configuration that could not see its own trigger; the correction is that the property names the shape rather than hoping the generator finds it, and 🟡 3 gets the same treatment at step 9. |
| **S18** | The fold changes error semantics, not only ordering | **Accepted and stated, `[R4-4]`.** Spec line 7 beside line 6, plus step 6's Done-when asserting the difference against the unwrapped behaviour rather than only asserting the new one. In the pause question, because the user accepted half of this consequence. |
| **S19** | L5 forces `Scrub` to the identity under the default; overriding it alone deletes data | **Closed, `[R4-5]`.** Three sentences (step 5 docs, SEC-5's negative form, step 5 Done-when), SEC-3(b)'s joint-lawfulness precondition, and `HistoryLensLawTests.Scrub_overridden_alone_violates_L5` at step 8(l) — a **lens-law** test, not a security regression test, for the room's stated reason. The room's own earlier answer ("default to identity, weight it in the guide") is **superseded**, and I say so rather than letting two follow-ups disagree in the record. |
| **S20** | `Movement.Held(Anchor)` is a retention site outside clause (b) | **Closed, `[R4-6]`, per `security-expert`.** Clause (b)'s reachability set is **not** widened — the Critic proposed the rewording and deferred the decision to the room, and the room declines it with a reason. The anchor is neither scrubbed (it would break INV-7's mechanism) nor made unreachable (new mechanism, out of this loop): it is named in Trust Boundary 7 and on the existing DEBUG row, bounded by the bracket contract, given `Anchor_never_reaches_a_release_path_surface` at step 8(m), one guide sentence at step 13, and a hardening-backlog fast-follow at step 16. |
| **S21** | Step 9's assertion (4) describes something the design cannot do | **Closed, `[R4-7]`.** Rewritten to *while a hold is open the reported key set is constant and equal to the anchor's* — stronger, falsifiable, and it puts the accepted liveness cost in a test. Plus spec line 9: while a hold is open the framework's central subscription contract is suspended. |
| **S22 / S23 / S24** | Three statements of fact | **All three made, `[R4-8a]` / `[R4-8b]` / `[R4-8c]`.** S22: INV-7's protection is confined to a held key and a scrubber, no button chrome can bracket, per-press reconciliation is what the shown chrome gets — in `06-spec.md`, ADR-030's consequences, the guide's head-coverage table, and `ux-expert` q11(b). S23: the exception is **restated** rather than fixed by `Origin := Established` in `rebase`, and the reason is given — the one-line fix would make pre-record navigations recorded stops that restore a model without its URL. S24: the chrome contract gains `blur` and `pointercancel`, `Clear` is named as the one in-band rescue, and step 10's chrome honours it. |
| 🟡 1–6 | none gate | **All six taken, `[R4-9]`.** 1: `Available(int Depth)`, documented as not a crossable count, with the O(Depth)-per-render alternative rejected on cost. 2: the two-chrome alias in the guide's chrome contract. 3: assertion 2's shape named and seeded explicitly in step 9's corpus. 4: SEC-5 names `HistoryEvent.Enveloped` as the type that carries the payloads. 5: PR 6a/6b pre-authorised. 6: undo out of a terminal state as a spec line. |

## Disposition of the Critic's first and second passes (carried forward, all closed)

| Id | Disposition |
|---|---|
| **B5** transit hold defeated by cadence and by delivery; property blind to both | **Closed by removing the clock.** A movement is an un-bracketed press or a `Hold`…`Settle` bracket — the user's input, not elapsed time. Scenario (i) cannot arise: there is no window to be shorter than a person. Scenario (ii) cannot arise: rule 2 is deleted, so a delivery does not end a run and no reconciliation happens against a state in transit. The delivery verb is answered positively (only anchor-declared sources are live) and asserted directly. **The property now runs in the mode the product ships, because there is no other mode**, over an alphabet including brackets, malformed brackets, generated inter-press ordering and an autonomously-delivering source, and step 9 is not done until removing the anchor is observed to fail it. |
| **B6** stale `Settle` ends the wrong run | **Moot, and I say why rather than adding the ordinal.** The race existed because a cancelled `Task.Delay` had already fire-and-forget-dispatched (`Runtime.cs:288-291`). With no framework settle source there is no such dispatch. Explicit settles arrive in input order; a duplicate or late one short-circuits at `Decide` (`Ok([])`) and is re-checked in `Transition`. No ordinal, no re-armed key, no `HistorySettleSource.cs`. One test covers the no-op. |
| **B7** `SensitiveCause` redacts `Cause`; the credential lives in `Model` | **Closed.** `Scrub` + **L5**, at every site that constructs a `Step` from a live model. SEC-3 splits into clause (a) `Step.Cause` / clause (b) `Step.Model`, neither subsuming the other, each with its own named regression test; L5's property exercises the redo-direction push. The site count is **three, not four**, because `seal` pushes no step — flagged in § Security rather than absorbed. The erasure's security cost is on the record. |
| **S9** redo dies silently | **Closed.** `seal` preserves `Future` and seals its top edge; `Forward(h)` returns the same four typed outcomes `Backward(h)` does; `MovementRefused` carries a `Direction`. Steps 3, 10 and 11 all assert on both directions. |
| **S10** the default blocks at the last thing the world did | **Accepted as designed behaviour, and promoted from footnote to first-class statement.** The reach is unchanged and unfixable without guessing; what changed is that nothing is destroyed, nothing is retained for nothing, the policy is renamed to declare what it is, both directions refuse with the sealing message named, and the reach is stated in step 5's file docs, `06-spec.md`, ADR-030, the guide and a diagnosable telemetry span. `ux-expert` q7 is re-asked as the common case. |
| **S11** multi-event `Decide` hole; the table is not total | **Closed structurally.** One decision becomes one `HistoryEvent.Enveloped`, classified once. `Step`/`Continue` deleted. The `RuntimeIsolationAndSubscriptionFaultTests.cs:218` shape is step 8(d)'s property. Three rejected alternatives and one new cost (unknown 8) are stated. |
| **S12** `IsUndoable` naming inversion | **Closed by removal**, which is the Critic's own preference and mine. The lens subsumes the question. |
| **S13** `SameUndoable` not required to be an equivalence relation | **Closed as L6**, renumbered around the user's L5 so no reference re-points. Step 8(e). |
| **S14** interleaving property claimed, absent from step 8 | **Closed** — step 8(c), with the note that it needs a driver that does not await the first `Undo`. |
| **S15** wall-clock settle test | **Closed by deletion.** The test is removed, not made deterministic, because the mechanism it tested no longer exists. No test in this plan sleeps or reads a wall clock. `ca2519d`'s lesson is stated in step 9. |
| **S16** INV-1 needs an availability precondition | **Closed** — step 8(b) writes the property that way, and a one-clause `00-scope.md` amendment is worded above for the architect. |
| **S17** three meanings of "barrier" | **Closed by eliminating the word.** `StepBarrier` → `EdgeState` (`Crossable`/`SealedByEffect`/`SealedByWorld`); the treatment is `seal`; step 14's message is `HistoryMessage.SplitStep`. Step 4's Done-when adds an explicit namespace-wide name-collision check, because revision 2 also shipped `HistoryEvent.Step` beside `Step<TModel>`. |
| 🟡 `ISensitiveCause` in seven places | **Fixed** — `SensitiveCause`, no prefix, everywhere in this artifact. |
| 🟡 stale citations | **Fixed** — the DOM-owned-state carve-out is `00-scope.md` **INV-3**'s closing clause; unknown 6 is closed and marked so. (Re-fixed at the confirmation pass's 🟡 3: this row cited `00-scope.md:144-151`, which the navigation amendment moved. Citations into that file are now by invariant id.) |
| 🟡 origin re-basing assumes the bootstrap `UrlChanged` | **Fixed by stating the exception**, having verified `Runtime.cs:431-434` this revision. `Origin = Fresh \| Established` gates the rule; the exception is one lost undo stop, only as the first message of a session, only on a head with no initial URL. |
| 🟡 `abies:history:` reserved by documentation only | **Fixed** — step 9 assertion (3). Stronger than asked: the framework now declares no subscription at all. |
| 🟡 H1's deferral / hold window unaccounted for | **Fixed** — explicitly out, with the reason, in § Chosen Direction. `NavigationCommand.Replace` is also explicitly out; revision 2 dropped it silently. |
| 🟡 `SameUndoable` is a full structural comparison per dispatch | **Fixed** — handed to step 7 in that exact shape, O(largest text field), not a constant. |
| 🟡 `InTransit` pins a whole model graph | **Fixed** — § Memory, and narrowed: only a **bracketed** run pins an anchor; un-bracketed presses pin nothing. |
| 🟡 step 7's todo text says "before 7–11" | **Fixed** — 8–11, in both the step and § Parallelisable. |
| Specialist spawns | `security-expert` **re-spawned and folded in** (its 2026-09-07 section). `performance-engineer` scheduled at step 7 with the three additions and one withdrawal. `ux-expert` gets q11 and q12 in place of the withdrawn q10, and q7 is re-framed as the common case. |

---

## Constraining Decisions

`decisions.md` carries headings rather than ids; cited by heading and date.

**ADRs.** ADR-008 *Immutable state* (records + `with`; the history is a value, which is why
`RingBuffer<T>` cannot serve — and line 85 needs amending). ADR-006 *Command pattern* (the monoid
whose non-invertibility is why a framework cannot recall an effect, and whose `Batch` case
`Runtime.cs:353-365` interprets in order — load-bearing for the S11 fold). ADR-007 *Subscriptions*
(derived from the model, reconciled inside `Render` — the load-bearing ADR for INV-7, and the reason
the anchor works without a seam). ADR-024 *Four render modes* (heads; Static excluded; the Auto
handoff line). ADR-025 *Issue 160 Debugger Boundary Contract Phase 1* — **examined and found not to
require superseding**. ADR-028 *ProgramCore/ProgramView split* (`WithView` is the composition
precedent, three times over). New: **ADR-030**.

**Register.** *Namespaces Are Bounded Contexts* / *Domain Terms Only* (`Picea.Abies.History`).
*Make Illegal States Unrepresentable* / *State Machines, Not Flags* / *Errors Are Values* —
`EdgeState`, `Movement`, `Origin`, `Direction` and `MovementAvailability` are all sum types where
revision 1 or 2 had a flag or a conflated field, and internal constructors close the one place the
principle was instructed rather than enforced. *No I-Prefix* — **now clean**: `SensitiveCause`
carries no prefix (gate-4 decision 5), so the exception revision 2 flagged is withdrawn. *Pure
Functional Programming* / *Push IO to the Edges* — stronger this revision: the design contains **no
clock at all** on any shipping path. *TUnit Only* / *No Arrange/Act/Assert Comments*. *Aspire
AppHost Is the Test Fixture (Amended 2026-09-02)* — steps 1–5 and 8 are workflow-direct domain
tests; steps 6, 9, 10 and 11 drive a real `Runtime` in-process, the existing precedent in
`RuntimeIsolationAndSubscriptionFaultTests.cs`, and still need no AppHost. *Spec-by-Example for New
Features*. **The 2026-04-04 decision is load-bearing twice over this revision**: the
`Ok([])`-versus-`Err` distinction is what `Decide` routes on, and `[R4-1]` turns on the fact that
`Runtime` *dispatches* the `Err` payload rather than deciding it — a rejected decision is a
supported, shipped, benchmarked capability and the design now classifies it as the application's own
response rather than as the world. *Every New Dependency Requires Review* — none added. *Full OTEL Trace Coverage* (step 11;
the inherited registration gap raised separately). *Docs Ship with Code*, *Diátaxis*, *ADR
Template*, *Definition of Done*. *2026-04-01 CI Runtime Policy* (5% threshold — and the statement
that it is silent here). *2026-04-04 `Program.Decide` return type is `Result<Message[], Message>`* —
the `Ok([])`-versus-`Err` distinction this design routes on, and the capability whose multi-event
form S11 was about. *2026-03-29 App polymorphic DU roots must declare JsonPolymorphic metadata* —
inherited by the DEBUG snapshot path only, made safe by SEC-1.

**Patterns.** `runtime-seams-anchor-replay-gating` — **the revisit trigger does not fire**, and this
revision strengthens the claim rather than weakening it: with the settle source gone the wrapper
declares nothing at all, and it still only *answers* the question `Runtime.Render` already asks
(`Runtime.cs:214`). No parameter, no flag, no `Runtime.cs` line.
`debugger-domain-stays-in-core` — honoured by construction; no JavaScript is touched.

---

## 🛑 Pause — for the user

> *Here's the plan and who does what. Approve as-is, or should anything change before the Critic
> stress-tests it?*

Revision 4 implements your four decisions and the Critic's nine items, and adds nothing else. Three
things it would help most to hear on — the first is the only one where I have written something
narrower than the words you gave me, and I would rather ask than assume.

1. **"The world" now means one thing, not two.** You told me to rewrite the narrative so that "the
   world" is *command feedback and rejected decisions*, and separately that a rejection should be
   routed through the envelope because it is the application's own response. Those two together
   leave **"the world" meaning a command's feedback and nothing else** — a rejected decision is no
   longer part of it, by your own second decision. That is how § *The default policy* and every
   downstream document are now worded. If you meant the narrative to keep naming rejections as the
   world, that is a different design — a third `SealedByDecision` edge state, which makes a rejected
   form submission a permanent barrier — and one word from you turns it around.

2. **The superseded branch costs memory, and this is the one place revision 4 spends any.**
   Preserving a redo branch instead of clearing it means models the user walked back through, and
   can now never return to, stay alive until they are trimmed. I have bounded it — `Future` trims to
   `Depth` exactly as `Past` does, so the ceiling moves from 100 retained models to 200 — and step
   7 measures it. You are buying diagnosability with it: a refusal that carries `FastTick` in the
   value and reads as *"Redo unavailable — your later action replaced this."* on screen
   (`[R4-ux]`; the identifier is the mechanism's cause, not the copy), answerable before the press,
   instead of silence. Worth knowing, though: **it buys
   diagnosability, not reach.** An application with a model-mutating subscription still needs a
   projection for redo to *work*; what changes is that it is now told so by name rather than finding
   out. Is that the trade you meant?

3. **The other half of the interleaving change — the half you weren't told about.** You accepted
   that folding a decision into one event moves command feedback to *after* the later events are
   applied. The same fold also changes what happens when a command **fails**: today a failing
   command stops the later events from being applied at all; under the fold they are already
   applied, and it is the later *commands* that are skipped. So *"partial model, no later effects"*
   becomes *"complete model, partial effects"*. I judge the second more correct — the events of one
   decision are one state change, and a command failing afterwards is not a reason to have applied
   half of it — but it is a spec line either way and you should be the one to say it is acceptable.

Two things I am **not** asking about, recorded so their absence is deliberate rather than accidental:
the held anchor stays unscrubbed (`security-expert`'s call, with its own test and threat-model row),
and INV-7's protection is confined to a held key and a scrubber (a consequence of your decision to
define a movement by input rather than by a timer — it is stated in three documents and put to
`ux-expert`, not softened).
