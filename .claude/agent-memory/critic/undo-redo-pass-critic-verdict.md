---
name: undo-redo-pass-critic-verdict
description: 2026-09-06/07 undo/redo design pass — four Critic passes, three LOOP BACK then APPROVED WITH MITIGATIONS; the nine blockers, which survived each re-plan, and what the user decided at each gate
metadata:
  type: project
---

Design pass `undo-redo` (H1 / `WithHistory`, a higher-order Program over a past/present/future
zipper). **Four Critic passes: three LOOP BACK TO REALIST, then APPROVED WITH MITIGATIONS**,
2026-09-06 and 2026-09-07. The direction was never at fault; every one of the nine blockers was in
the mechanism, and each pass's blockers landed in whatever organ the previous revision had just
rebuilt.

## Pass 1 — B1…B4

1. **Bare-event drift.** Model changes arriving outside the `Decide` envelope moved `Present`
   without recording a step. INV-1/2/3 all passed while the feature lost data.
2. **Redo unreachable with any live subscription** — both the record and continue paths cleared
   `Future`.
3. **INV-7 falsified by any undo run of length ≥ 2**, with no test vehicle.
4. **Late `NothingToUndo` delivered a refusal**, falsifying INV-4 (`_decisionGate` releases at
   `Runtime.cs:474` before `_core.Dispatch` at `:494`).

**Gate-3 answers:** un-defer the projection lens (erased `Restore`/`SameUndoable`, no fifth type
parameter); typed refusal under the identity default; INV-7 gets the transit reading; seven PRs;
`SensitiveCause` without the `I`; `security-expert` spawned.

## Pass 2 — B5…B7

5. **B5 — the transit hold's unstated precondition**: it held only for presses closer together than
   `SettleWindow` (250 ms, borrowed from ProseMirror's *edit-coalescing* delay), and the planned
   property ran with `SettleWindow = null`, excluding all three triggers by construction.
6. **B6 — the settle signal carried no ordinal**; `DispatchFromSubscription` is fire-and-forget, so
   cancelling the window could not recall a `Settle` it had already dispatched.
7. **B7 — the security mitigation missed its own worked example**: `SensitiveCause` redacts
   `Step.Cause`; Conduit's passwords are **model** fields. Fixed by `Scrub` + `Restore(Scrub(a),b) =
   Restore(a,b)`.

**Gate-4 answers:** delete the clock (a movement is one press, or an explicit `Hold`…`Settle`
bracket; the user accepted that a chrome which holds and never settles pins the anchor); `Scrub` +
L5; reconsider the default (`seal` not barrier, symmetric typed refusal,
`WholeModelHistoryPolicy`); one decision = one enveloped event, folded and command-batched; two
scope amendments (INV-1 availability precondition, INV-7 definition of a movement).

## Pass 3 — B8, B9 (revision 3)

Revision 3 closed B5, B6, B7 and all seventeen 🟠/🟡. Both survivors are in the organ revision 3
rebuilt — the four-row `record`/`pass`/`seal` table, whose first question is *"came through the
envelope?"* and whose real answer the plan never states.

8. **B8 — an application's `Decide` **error** reaches `Transition` bare.** `Runtime.cs:460,484`
   dispatches the `Err` channel's message through `_core.Dispatch`, and `Picea.xml:1229-1239` shows
   `AutomatonRuntime.Dispatch` is transition → observer → interpreter with **no `Decide`**. So a
   validation rejection classifies as `seal`/`SealedByWorld`: undo and redo permanently blocked,
   reason misnamed. One line: `Err(e) -> Err(Enveloped(m,[e]))`.
9. **B9 — the framework cannot tell a subscription from a click.** `Runtime.cs:288-291`
   (`DispatchFromSubscription`) and `:332` (`_handlerRegistry.Dispatch = DispatchFromSubscription`)
   are the same delegate into the same public `Dispatch`. So a timer tick **is** enveloped, and
   `record` clears `Future` — redo silently destroyed every 250 ms in `SubscriptionsDemo`, i.e. B2's
   harm and S9's opacity on the *record* path, while the plan's narrative claimed a typed refusal
   naming `FastTick`. "The world" under this mechanism is **interpreter feedback and `Decide`
   errors, and nothing else.**

## Pass 4 — APPROVED WITH MITIGATIONS (revision 4)

Gate-5 answers: "the world" = command feedback **only** (a rejection routes through the envelope as
`Err(Enveloped(m,[e]))`, not `SealedByDecision`); B9 option **(b)** —
`EdgeState.SupersededByNewAction(Cause)`, branch preserved and sealed, redo refuses by name,
ceiling `2 × Depth` = 200 models, bought for diagnosability not reach; S18 as "complete model,
partial effects"; S19/S20 per the security room's second follow-up. The user upheld **both** of the
Realist's flagged judgement calls (keep S23's behaviour and fix the description; follow the room's
refusal to widen SEC-3(b) over my literal wording — **my wording was itself the mistake S20 names**).

Revision 4 carried all nine items. Five 🟠 left (S25–S29), none mechanism: two survivals of the
pre-B9 narrative; the supersession behaviour missing from § *Spec obligations*; the autonomous
source put into step 8's **shared** alphabet, which falsifies INV-1 and INV-3 by correct behaviour;
8(k) asserting an ordered interleave against fire-and-forget `DispatchFromSubscription` with no
signal; and — the one worth remembering — **decision (b) changed the retention *lifetime*, not only
the count**: a superseded `Future` step leaves only when `Future` next exceeds `Depth`, i.e.
possibly never, where revision 3 released it at the next action. The security room's whole
comparison for the anchor rested on the old `Depth`-dispatch bound.

**Two things I derived rather than accepted, and both are reusable:** no edge in this algebra ever
returns to `Crossable` (`sealTops`/`ReplaceTop` only seal; `StepBack`/`StepForward` push fresh
steps), which is what makes the gate-2 "retained branch ⇒ redo is a relation" argument inapplicable
to a *sealed* branch; and `Past + Future` is conserved by movement and incremented by `record`, so
supersession without a `Future` trim would have been unbounded.

**How to apply if this pass resumes:** verify resolutions in the code, never in the plan's own
disposition table — every pass has had at least one table row that the step's acceptance criterion
did not carry. If a later pass touches `Picea.Abies.History`, re-check B1, B2, B8 and B9 first: all
four are about which messages reach `Transition` outside `Decide`, and a refactor reintroduces them
quietly. The kernel's contract is readable — `~/.nuget/packages/picea/<ver>/lib/net10.0/Picea.xml`
answered in one read what three phases had reasoned around.

**Outcome of the 🛑 (fill in when known):** pass 2's answers are the gate-4 decisions above; pass
3's are the gate-5 decisions in § Pass 4; pass 4's pause pending.

## Confirmation pass — post-close-out amendment `[R4-nav]` (2026-09-07)

The user found, after close-out, that **incoming navigation was never classified**: after the first
record a browser-delivered `UrlChanged` was enveloped and `record`ed, so undo restored a model
without its URL. Fix: one rule beside the origin re-basing rule,
`origin is UrlChanged && Origin is Established -> seal, SealedByWorld(UrlChanged)`, both edges;
`INV-2` amended so the world includes the browser's location; a **second** INV-2 property in the
spec. Verdict **CONFIRMED WITH A BOUNDED LIST** — appended to `05-critic.md`, not a fifth pass.

- **B10** — the rule keys on `origin is UrlChanged`, but on the WASM head the incoming URL change
  reaches the program as **the application's own message**: `Navigation.UrlChanges(Func<Url,Message>
  toMessage)` (`Navigation.cs:17-29`) is the only assigner of `OnUrlChange`. Conduit and every
  tutorial pass `url => new UrlChanged(url)`; **`README.md:190` passes `new UrlChangedTo(url)`** and
  gets no seal, silently, with every property green. Server head is safe (`Session.cs:271` dispatches
  the framework type itself). Resolution offered: scope the claim as an *application obligation*
  beside L1–L6, documentation only.
- **B11** — *"After the first `record`, navigation classifies like any other message"* survived in
  the block marked *"what `spec-author` and `csharp-dev` implement from"* (`04-realist-plan.md:1138`)
  **and** in `07-handoff.md:146`'s standing-decisions table. The handoff had not been re-opened at
  all: it records only amendment 1 (A9).
- **S30–S33 / 🟡 1–7**: "the world is command feedback and nothing else" now false in four places
  (step 5 (iv) sits directly above (vi)); reach now has **three** bounds and every statement gives
  one or two; the obligations line is 14 in the plan and 16 in the spec **and the two 14s are
  different obligations** (the spec's is S26's, which the plan's table never gained) — 16 is right;
  `ux-expert` is dispatched **first** with a pre-amendment premise for q9/q13; `UrlChanged` is sealed
  and framework-owned so an adopter cannot mark it `SensitiveCause` while its full `Url` is now the
  guaranteed edge cause handed to the chrome.

**Two derivations worth keeping.** Of the four classification rows, only `seal` satisfies the
amended INV-2 — `record` and `pass` each produce the forbidden (model, location) pair, `rebase` is
`Fresh`-only — so the rule is forced, not chosen. And **INV-3 does not need the location**: `Route`
moves only on a `UrlChanged`, which seals both incident edges, so the location is constant across
any window where a movement is permitted and a location clause would be a conjunct no property could
turn red on.

**Calibration note.** Three of my four passes' recommendations were improved on by someone else:
the room refused my SEC-3(b) rewording (S20), the Realist found a better reason than mine for S23,
and item 2's wording had already gone stale by the time B8 was decided. A finding can be right and
its proposed mitigation wrong; rank the finding, and offer the mitigation as an option rather than
as the answer.

See [[abies-recurring-risk-patterns]].
