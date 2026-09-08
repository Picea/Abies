# 🎨 UX Room — `undo-redo`, plan step 12

Read: `07-handoff.md` § 6 (wave-0 `ux-expert` row, carrying the corrected premise for q9 and
q13(d) after `[R4-nav]`); `04-realist-plan.md` § *What `ux-expert` must answer* (step 12, the
thirteen questions, q9/q13(d) corrected in place per S32); `00-scope.md` INV-1 … INV-7 and the
refusal policy settled at gate 2. Answered against the corrected premise as it stands, not from
memory of the pre-amendment version: **after the first recorded step, a browser back or forward
press is not an undoable step. It seals the history in both directions. Undo and redo both
refuse, naming the navigation.**

Scope of this document: one undoable step, as it is felt across the browser, server-rendered and
native heads — the refusal at a sealed edge (`BlockedByEffect`, `BlockedByWorld`,
`BlockedBySupersedingAction`), the superseded redo branch, hold/settle brackets in the chrome, the
native head's missing closing event, key bindings against native/browser text-field undo. DOM
state (selection, focus, scroll) restoration is out of scope for this pass, per INV-3 as amended,
and is treated as such below.

---

## Summary

**Overall: ⚠️ Needs Changes before step 10/13 lock their wording.**

The mechanism is sound and every refusal is answerable in advance (INV-6) — that is the load-
bearing win, and it is what makes a good error message possible at all. But two of the plan's own
illustrative sentences must not ship as written: reusing "the page changed while you were editing
(`UrlChanged`)" for a navigation the user performed on purpose (q9/q13(d)), and showing
"superseded by `FastTick`" as end-user copy (q13(b)). Both are C# identifiers standing in for
sentences a person has to read under stress, and Krug's rule about system internals applies to a
`Message` type name exactly as it applies to a stack trace.

---

## Answers to the thirteen questions

### q1 — What the effect boundary looks like before the attempt and at the attempt

**Recommendation.** Before the attempt: render Undo/Redo as `disabled` (native `disabled`, not
`aria-disabled` alone — there is no case here where a screen-reader user benefits from a
disabled-but-focusable control, because the reason is available up front and nothing is gained by
letting focus land on it), **with the reason as visible adjacent text, not a tooltip.** At the
attempt: `MovementRefused` is the defensive backstop for the render/press race, not the primary
communication channel — if the chrome renders availability correctly (INV-6 exists precisely so it
can), a user should never see `MovementRefused` fire against a control that looked enabled.

**Reasoning.** A disabled button with no stated reason is Krug's "don't make me think" violation in
its purest form — the user is left to guess whether the app is broken, whether they clicked wrong,
or whether they should wait. Tooltip-only text fails on touch (no hover) and is inconsistently
announced by screen readers (WCAG 3.3.2, 4.1.2); adjacent static text has neither problem. INV-6 is
the invariant that makes this possible at all — the reason exists *before* the press — so putting it
only behind an interaction (hover, or the press itself via `MovementRefused`) throws away the one
thing the mechanism bought.

**Changes a stated default?** Yes — this is a concrete chrome-contract requirement for step 10 that
was open in the plan. Composition tests must assert on the adjacent text, not merely on the
disabled state or on `MovementRefused`'s payload.

---

### q2 / q13(a) — Does discarding (now: superseding) the redo branch need any communication, and should it be silent like a text editor's?

**Recommendation.** No, it must not be silent — the refusal is the right and sufficient signal, and
it must always be shown. Do not add a second, proactive notification at the moment of
supersession itself (see q13(d) for the same judgment applied to navigation).

**Reasoning.** A text editor stays silent about a discarded redo branch because the only thing that
can cause the divergence is the *same user's own next keystroke* — the cause is self-evident, so
naming it would be noise. That assumption does not hold here: under the whole-model policy the
thing that supersedes a user's redo branch can be a background timer they never touched
(`FastTick`), a network response, or another part of the interface entirely (INV-1's whole point is
that the application is one history across independent regions). Silence in that situation is
indistinguishable from the exact bug this pass exists to close — redo dying with no explanation
(S9/B9). The refusal must always fire; it is INV-5's purpose.

**Changes a stated default?** No — confirms the mechanical decision already made (`[R4-2]`,
`SupersededByNewAction`). Settles the plan's own open item.

---

### q3 — Keyboard affordances across the three heads

**Recommendation.** Confirm: the framework ships **no** key binding. The guide should tell an
adopter who wants `Ctrl+Z`/`Ctrl+Y` at the application level to bind it only when
`document.activeElement` is not an editable native control (`input`, `textarea`,
`contenteditable`), and to say so explicitly rather than leave the collision to be discovered — a
global `Ctrl+Z` that fires while someone is mid-edit in a text field, undoing an app-level action
instead of their last keystroke, is one of the most disorienting failures a keyboard user can hit
(w3c/editing#150 is the right citation to keep). Undo/redo must still be reachable without a
keyboard shortcut at all — visible `<button>` elements, in the tab order, Enter/Space-activatable —
because an app-level feature with no visible affordance and no default binding is undiscoverable by
definition (Krug: don't make the user guess a shortcut exists).

**Reasoning.** Jakob's Law: users' expectation of `Ctrl+Z` is bound to the focused text field, not
the surrounding application, in every mainstream editor. Overriding that expectation at the window
level without checking focus breaks a much stronger, older convention than the one being served.

**Changes a stated default?** No — confirms the plan's own recommendation. Adds the focus-check
guidance the guide (step 13) is missing.

---

### q4 — Is DOM-owned state (caret, focus, scroll, `<details>`, uncontrolled inputs) acceptable to leave out of scope for v1?

**Recommendation.** Yes, accept the exclusion for v1. Do **not** reopen `00-scope.md`/INV-3 for a
minimum bookmark this pass.

**Reasoning.** A partial bookmark — say, scroll position restored but not caret — is worse than no
bookmark at all, because it teaches the user that undo restores "where I was" and then breaks that
promise unpredictably depending on which kind of document-owned state happened to be involved. An
honestly incomplete feature (nothing about DOM state is restored, full stop) is easier to reason
about than a feature that's *usually* complete. The right instrument for this — ProseMirror-style
position bookmarks stored inside history entries — is a real design in its own right and belongs to
the DOM-owned-state follow-on candidate already named in `07-handoff.md` § 9, not to a bolt-on here.

**Changes a stated default?** No — confirms the plan's Unknown #4 stays closed. One documentation
ask: the guide should say plainly, near the reach statement, that a caret or scroll position is not
part of what undo restores, so an adopter doesn't discover it by surprise in front of a user.

---

### q5 — Is coalescing off by default right?

**Recommendation.** Yes, off by default is correct.

**Reasoning.** This is not a text-field undo stack where "coalesce keystrokes into a word" is the
established convention (and even there it's a convention specific to *text*, not to app state).
Here, one dispatch equals one undoable step is the unit both Dreamer tracks converged on
independently and the user confirmed at gate 2 — the strongest single result of the whole pass. A
500 ms coalescing window is a second, hidden timer sitting next to a design that spent an entire
revision (B5) removing the first one (`SettleWindow`) because an invisible clock falsified an
invariant. Reintroducing timer-based grouping as the default would put the same class of surprise
back, at the one point where the design just proved it doesn't need a clock. Off-by-default keeps
the mental model at "what I pressed is what I undo," which is boring and correct.

**Changes a stated default?** No — confirms the plan's default. If an adopter wants word-level
coalescing (e.g., for a text field bound into an undoable model field), that is a local, opt-in
decision they make with full knowledge of what they're grouping — not a framework-wide default.

---

### q6 — Should `Step.Cause` surface as a label (Qt's `text()`)?

**Recommendation.** Not by default, and not by auto-deriving `Cause.GetType().Name` for anything
shown in ordinary (non-refusal) chrome, such as a history list or "Undo: ___" menu item. Ship a
generic default ("Undo last change" / "Redo last change") and document an optional
`Cause -> string` humanizer hook an adopter can supply. For the sensitive-cause case, "Undo
(sensitive change)" is already right and needs no further UX input.

**Reasoning.** `Cause` is now the incoming message after the fold, which is exactly the value a
label wants semantically — but a raw type name is a developer artifact, not a sentence. Showing
`ProfileLoaded` or `ZoomChanged150` as user-facing text violates the same rule that governs error
messages: no system internals in front of the person using the product. The fix is not to withhold
the capability, it's to not make the raw identifier the default rendering of it.

**Changes a stated default?** Yes for anything the guide would otherwise show as a copy-pasteable
example — the worked example in step 13 must show the humanizer pattern, not `Cause.GetType().Name`
printed directly, as the recommended shape for a labeled history UI.

---

### q7 — What should the user see when undo/redo is blocked because the world moved? (Now the common case, not the exceptional one — S10.)

**Recommendation.** A concrete template, applied consistently (see also q12/q13(c)):

> **"[Undo/Redo] unavailable — [reason]."**

with `[reason]` filled from a small, adopter-owned lookup from `Cause`'s shape to plain language
(e.g. "your changes were saved" for a save-confirmation feedback message, "the page finished
loading" for an initial-load feedback message) — not from the raw type name. Where no humanized
entry exists for a given cause, fall back to a generic, still-honest phrase: "something changed
since then" — never the bare identifier as primary text. The raw `Cause` type name stays available
for diagnosis exactly where SEC-4 already puts it: telemetry span tags, not the rendered document.

**Reasoning.** Since S10 makes this the default experience for any application with real network
traffic, the sentence a first-time user reads here is now the single most common thing this feature
says to anyone. It has to pass the same "what happened / why / what can I do" test as any other
error message. "What can I do" here is simple and should be stated too: nothing to do but continue
— the refused direction stays refused, the other direction is untouched, and that's fine to say in
one clause so the user doesn't sit there re-trying the same press.

**Changes a stated default?** Yes — this replaces bare `Cause.GetType().Name` as the recommended
rendering in the guide's worked example and in step 10's illustrative sentence. It does not change
the mechanism: `Cause` is still carried and still available, only its *default presentation* moves.

---

### q8 — Confirmation: pressing undo at a blocked boundary may not destroy redo

**Recommendation.** Confirmed, no further UX input needed. A refusal is a `pass` and never touches
`Future` — that is exactly the behavior a person needs from a "nothing happens" response: truly
nothing happens elsewhere in the history either.

---

### q9 — First undo of a freshly loaded page, and (corrected premise) the post-record navigation refusal

**Recommendation, both halves.**

- **Pre-record (re-basing).** Confirmed acceptable. While nothing has been recorded, a
  back/forward press simply re-bases and creates no undo stop — from the user's side this is
  indistinguishable from "there is nothing to undo yet," which is INV-4's ordinary empty-history
  state. No special wording is owed here; a fresh, unpopulated Undo button already reads correctly
  with no message at all. Do not invent language for this half — it would be explaining something
  that never happened from the user's point of view.
- **Post-record (the `[R4-nav]` seal).** Yes, a refusal is the right thing to show, and it must not
  reuse the "the page changed while you were editing (`X`)" sentence written for feedback-caused
  refusals. That sentence implies something happened *to* the user; a browser Back press is
  something the user did *on purpose*. See q13(d) for the exact wording.

**Reasoning.** Consistency of *mechanism* (a refusal, always typed, always answerable in advance)
should not be read as consistency of *wording* — the whole reason INV-5 requires the carried value
to identify which reason applied is so that the two can be told apart, and a person deserves the
same courtesy the type system already gets.

**Changes a stated default?** Yes for the post-record wording (see q13(d)) — no change for the
pre-record half, which stays the ordinary empty-history rendering already planned.

---

### q10 — Withdrawn

No answer owed; there is no settle window.

---

### q11 — Bracketed runs: the lost-key-up case, and per-press reconciliation for the shown chrome

**(a) Lost key-up / focus loss.** **Recommendation.** Keep the automatic settle on `blur` and
`pointercancel` (already decided, R4-8c) — a bracket must never be able to strand permanently, full
stop. **In addition**, render the open-bracket state visibly while it's live: a pressed/active
affordance on whichever control started the hold (the key, or the scrubber thumb), for as long as
`Movement` is `Held`. This costs nothing beyond reading a value the chrome already reads for INV-6.

**Reasoning.** Auto-settling on `blur`/`pointercancel` is the correct safety net and should not be
questioned — a stranded bracket with no in-band rescue would be a real defect. But a safety net that
fires silently is still a mechanism the user never sees working; showing "a movement is in progress"
while it's true is Nielsen's visibility-of-system-status applied to the one state in this design
that is genuinely transient and genuinely invisible otherwise. It costs no new mechanism, only a
render.

**(b) Per-press reconciliation for the chrome actually shipping (buttons, no scrubber).**
**Recommendation.** Acceptable for this pass. Do not reopen the architecture question.

**Reasoning.** Five clicks producing five reconciliations is invisible to the end user in ordinary
use — it looks and feels like five separate actions, because from the user's perspective it *is*
five separate actions; nothing about a two-button chrome asks for or implies "one continuous
gesture" the way a held key or a dragged scrubber does. The cost this carries (sources starting and
stopping at each intermediate state) is a subscription-design concern for the adopter, not something
a person clicking Undo five times in a row will ever perceive as wrong. Solving it properly needs a
scrubber (its own component, its own UX spec) or a runtime seam (an architecture decision) — neither
is proportionate to unlock for a chrome this pass isn't shipping. When the scrubber follow-on is
picked up, this document's answer to (a) is the starting point for its bracket-visibility spec.

**Changes a stated default?** (a) is a new, low-cost recommendation for step 10's chrome (not
previously specified). (b) confirms the plan's own conclusion (S22/S24) and does not reopen it.

---

### q12 — Should undo and redo refuse with the same words, or does direction need different language?

**Recommendation.** Same template, direction-appropriate verb only — not two independently authored
messages. E.g. for a world-caused refusal: "Undo unavailable — [reason]" / "Redo unavailable —
[reason]," with the bracketed reason clause identical in both directions when the cause is the same
edge. Where the underlying relationship is directional in meaning ("you cannot go back past X" /
"you cannot go forward past X"), keep that symmetry rather than writing two unrelated sentences.

**Reasoning.** Miller's rule: one sentence shape learned once should cover both directions. Writing
independently-voiced copy for Undo versus Redo doubles what a person has to parse the first time
they hit each one, for no benefit — the *direction* is already obvious from which button they
pressed; only the *reason* is new information, so only the reason needs to vary.

**Changes a stated default?** Yes, in the sense that this was explicitly left open ("mine to
answer") — it fixes the wording pattern step 6/10 should render.

---

### q13(b) — Does "superseded by `FastTick`" mean anything to a person?

**Recommendation.** No, and it must not ship as default end-user copy. Apply the same treatment as
q7: "Redo unavailable — your later action replaced this" (or, where a humanized name is supplied,
"Redo unavailable — replaced by a background update") as the default/fallback phrasing, with the
raw `Cause` type name reserved for telemetry and for a dev-facing detail (e.g. a `title` attribute or
expandable technical detail row, not the primary text a screen reader announces first).

**Reasoning.** `FastTick`, `ProfileLoaded`, `ZoomChanged` are C# identifiers. A person who has never
opened this codebase gains nothing from reading one and loses trust in the message that contains it
— it reads as leaked debug output, which is exactly the "shows system internals" rule error-message
design exists to prevent. The mechanism should keep `Cause` fully — that's what makes the message
diagnosable at all — but *diagnosable-by-a-developer-in-telemetry* and *readable-by-a-user-on-
screen* are different bars, and this pass's current worked examples conflate them.

**Changes a stated default?** Yes — this is a 🔴 Must Fix relative to the plan's own illustrative
sentence (see Findings). The guide's worked example (step 13) and step 10's Done-when sentence must
carry an explicit annotation that the raw-identifier form is illustrative-of-mechanism only, not
recommended production copy.

---

### q13(c) — Should the three blocked reasons (effect, world, supersession) read as three distinct messages, or one message with three internal causes?

**Recommendation.** One message shape at the presentation layer, matching the one sum type
(`MovementAvailability`) already chosen at the mechanism layer. Same template as q7/q12: "[Undo/
Redo] unavailable — [reason]," with the reason clause varying by which of the three cases applies,
but the same visual treatment (same control, same position, same styling) in all three cases.

**Reasoning.** Hick's Law: three differently-styled or differently-placed messages would ask the
user to learn three response patterns for what is, from where they're standing, the same event —
"I pressed a button and it told me why it can't." The type system already correctly keeps the three
causes distinct and typed for the application's own use (INV-5 requires exactly that); nothing about
that distinction needs to become three different *user experiences*.

**Changes a stated default?** No new mechanism; confirms a single presentation template applies
across all three `MovementAvailability` refusal cases.

---

### q13(d) — Does the navigation refusal need its own words, and does the moment of the seal want a signal of its own?

**Recommendation.** Own words: yes (see q9). A concrete pair:

> Undo: **"Undo unavailable — you navigated away from this page."**
> Redo: **"Redo unavailable — you navigated to a different page."**

Own proactive signal at the moment the seal happens (i.e., something shown right when Back/Forward
is pressed, before the user has touched Undo or Redo at all): **no.**

**Reasoning.** The wording half is the direct fix for the failure `05-critic.md` S32 named: the
feedback-refusal sentence ("the page changed while you were editing") is written for something that
happened *to* the user and is a strange, faintly accusatory thing to say to someone who changed the
page themselves by pressing Back. The two causes need to read as different kinds of event because
they *are* — one is the world acting, one is the user acting — and INV-5's own requirement that a
refusal "explains itself" is not met if the explanation contradicts what the user just did.

The no-proactive-signal half follows the same reasoning as q2/q13(a): a browser Back/Forward press
is one of the single most common gestures on the web, performed by people who in the overwhelming
majority of cases have no interest in this application's undo stack at that instant. Interrupting
every navigation with a notice about a history feature would be the "confirmation dialog for a
routine action" failure mode at web scale — exactly the kind of thing Krug and this squad's own
"push back on" list warns against (a confirmation for something the user can simply discover, later,
passively, at the one moment it's relevant: when they reach for Undo). The always-visible,
answerable-in-advance disabled/reason state (q1, INV-6) already *is* the signal — it is there the
moment someone looks at the Undo button, and silent the rest of the time, which is exactly right.

**Changes a stated default?** Yes — this directly overrides the plan's own literal illustrative
sentence for the post-navigation case (see Findings, 🔴 Must Fix).

---

## Cognitive Load

- **Hick's Law (choice count).** Undo/Redo remain two controls with one refusal template each; the
  three internal causes (q13(c)) do not multiply into three visible response shapes. Good.
- **Miller's Rule (chunking).** One sentence template ("[Undo/Redo] unavailable — [reason]") covers
  every refusal in the design. Confirmed as the right chunk size — do not let step 10/13 drift into
  bespoke wording per cause.
- **Fitts's Law (target size & proximity).** Not newly at issue here — Undo/Redo are ordinary
  buttons; touch-target sizing is a step-10 chrome implementation detail with no new guidance from
  this pass's questions. Flag as a standing constraint (≥44×44px) for whoever builds the reference
  chrome.

## Accessibility (WCAG 2.2 AA)

- **Keyboard navigation.** Native `<button>` for Undo/Redo (per q1); no framework-level global key
  binding (q3), so no keyboard trap and no override of expected in-field undo behavior.
- **Screen reader experience.** Disabled state plus adjacent static text (q1) is announced reliably;
  a tooltip-only reason (rejected in q1) would not be. The reason text must be in the accessible
  name/description path (e.g., visually adjacent `<span>` referenced by `aria-describedby`, or
  simply inline text before/after the button), not a `title` attribute alone.
- **Color contrast / non-text indicators.** Not newly at issue in these thirteen questions; carried
  as a standing constraint for step 10's implementation (disabled state must not rely on color
  alone — pair with the text reason, which it already must have per q1).
- **Focus management.** Selection/focus restoration after undo/redo is explicitly out of scope for
  this pass (INV-3 as amended) and this document does not reopen it. No new focus-management
  recommendation beyond: don't move focus away from the Undo/Redo control the user just activated.
- **Reduced motion.** No animation is introduced by anything answered here; the bracket-visibility
  indicator recommended in q11(a) should be a static state change (pressed/active style), not a
  transition that needs a `prefers-reduced-motion` guard.

## Error Handling

- **Error messages.** The template recommended across q7/q12/q13(b)/q13(c)/q13(d) is the single
  most consequential output of this document: **"[Undo/Redo] unavailable — [reason]"**, with
  `[reason]` always humanized and the raw `Cause` type name reserved for telemetry/diagnostics, never
  primary on-screen text. This is the one place the plan's own illustrative wording must change
  before it locks (see Findings).
- **Empty states.** INV-4's no-op (nothing to undo/redo) needs no message at all — a disabled
  control with no history is self-explanatory and should stay silent, distinct from a *refused*
  disabled control, which always carries a reason (q9 pre-record case).
- **Loading / network failure.** Out of scope for this batch of questions; not addressed by any of
  the thirteen.

## Mobile / Responsive

- **Touch targets.** Not newly raised by these questions; the drag-scrubber gesture is explicitly
  not part of this pass's chrome (no scrubber ships), and the native head has no bracket at all
  (q11), so no drag-target sizing question arises here. Carried forward as a concern for the
  scrubber follow-on named in `07-handoff.md` § 9.
- **No horizontal scroll.** N/A to this feature's UI surface as scoped.

---

## Findings

### 🔴 Must Fix

- **Step 10's Done-when sentence / step 13's guide** — do not ship "the page changed while you were
  editing (`UrlChanged`)" as the rendered text for a post-navigation refusal. It tells a user who
  just pressed Back that something happened *to* them, when they did it *on purpose*. Replace with
  distinct navigation wording per **q9/q13(d)**: "Undo unavailable — you navigated away from this
  page." / "Redo unavailable — you navigated to a different page."
- **Step 10's Done-when sentence / step 13's guide** — do not ship "superseded by `FastTick`" (or any
  raw `Cause.GetType().Name`) as end-user-facing primary text. Per **q13(b)/q7/q6**: default to a
  humanized fallback phrase, keep the raw identifier for telemetry/dev-detail only, and mark any
  existing raw-identifier example in the guide as illustrative-of-mechanism, not recommended
  production copy.

### ⚠️ Should Fix

- **Step 10's chrome contract** — the refusal reason must be rendered as adjacent, always-visible
  text, not a tooltip or `title`-only affordance (**q1**). Composition tests should assert on the
  visible text, not only on the disabled state or the `MovementRefused` payload.
- **Step 10's chrome contract** — render a visible "movement in progress" state for the duration of
  a `Held` bracket, using the `Movement` value the chrome already reads for INV-6 (**q11(a)**). No
  new mechanism; a render-only addition.
- **Step 13's guide** — add the focus-check caveat for any adopter binding `Ctrl+Z`/`Ctrl+Y` at the
  application level: check `document.activeElement` is not a native editable control first
  (**q3**).
- **Step 13's guide** — add a sentence near the reach statement that undo does not restore caret,
  focus, or scroll position, so this is discovered by reading, not by surprise (**q4**).

### 💡 Suggestions

- Adopt a single wording template — **"[Undo/Redo] unavailable — [reason]"** — across all three
  `MovementAvailability` refusal cases and both directions, varying only the reason clause and the
  Undo/Redo verb (**q12, q13(c)**). This is the one piece of copy worth locking down once so every
  downstream rendering site (step 10's chrome, the guide's worked example, any adopter's own UI)
  inherits it rather than re-deriving it.
- When the scrubber follow-on (named in `07-handoff.md` § 9) is picked up, start its bracket-
  visibility spec from this document's answer to **q11(a)** rather than re-deriving it.

## What's Good

- **INV-6 is what makes any of this answerable at all.** Every refusal being computable before the
  press, from the history value alone, is the single design decision that turns "undo did nothing
  and I don't know why" into a message a person can actually read in advance. Nothing in this
  document works without it.
- **The refusal is always typed and always shown (`[R4-2]`).** Choosing "refuse and name it" over
  "silently discard" for the superseded branch is the right call on its own terms (q2/q13(a)), independent
  of any wording fix — it closes a class of bug (silent redo destruction) that this document would
  otherwise have had to argue for from scratch.
- **No key binding shipped by default (q3)** avoids the single most common way an app-level undo
  feature breaks a text field's own undo expectations. Correctly recommended and correctly not
  reopened here.
- **Declining to invent a partial DOM-state bookmark (q4)** is the right kind of "no" — an honestly
  incomplete feature over a feature that quietly breaks its own promise some of the time.
