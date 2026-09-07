# 00-scope.md — `undo-redo`

This is the problem as stated. It stands alone: `dreamer-first-principles` reads this file
and the codebase, and nothing else.

---

## Problem statement

An Abies application must offer undo and redo over the whole of its application state.

An Abies application runs on a single kernel, and every change to application state goes
through one transition function. The shape, in `Picea.Abies/Program.cs` and
`Picea.Abies/Runtime.cs`, is two-stage: an incoming message is turned into zero or more
events by a decision function, and each event is applied by the transition function to
produce the next model together with an effect.

Because there is one kernel and one transition function, undo and redo are
application-wide. There is **one history for the running application**. There is no
history belonging to a component, a screen or a subtree, and no design may introduce one.

The kernel is pure. The transition function returns an effect alongside the next model,
and effects are carried out at a boundary outside the kernel, where they can reach the
document, the network, browser history, subscriptions and navigation.

Application models are immutable records; the codebase holds to this without exception.

### Subscriptions and timers

Alongside the transition function, a program declares which ongoing sources of messages
it wants active for a given model — a periodic timer, a long-running custom source, a
stream of navigation changes. A timer is not a separate mechanism: it is one kind of
subscription, declared the same way as every other kind.

Two consequences of that shape bind this work.

- Which subscriptions are active is derived from the model — the same model the
  transition function produces. A change to the model can start a subscription or stop
  one.
- The messages those subscriptions emit enter the application through the same path as a
  message caused by a person, and are applied by the same transition function. Past that
  point nothing distinguishes a timer tick from a button press.

**Hard requirement.** Moving the application to an earlier or a later state — replay,
restore, or whatever else a design invents — must suppress subscriptions and timers. For
the duration of that movement no subscription may be started, stopped or torn down, and
no subscription may deliver a message into the application, as a consequence of passing
through those states. Subscriptions the history mechanism declares for itself are part of
the movement; see INV-7. What a design does once the application has settled on the state
the user asked for is open; what happens on the way there is not. A design must state how
it meets this.

This requirement is carried below as **INV-7**, so that it is falsifiable by a property
rather than only assertable in prose.

### The existing debug-only capability

There is already a capability under `Picea.Abies/Debugger/` that lets a developer move an
application backwards and forwards through its past states while debugging. It is
excluded from release builds by conditional compilation, and its browser-side asset is
excluded from release output.

It is named here for two reasons only: a requirement below refers to it, and one of the
open questions is about it. It is **not** a starting point, a foundation, or a thing to
be extended by default. Whether this work reuses it, extends it, relocates part of it, or
stays entirely separate from it is one of the questions this pass exists to answer. Read
the code and judge it on what you find there.

### Where this must run

The same kernel runs under four heads: static HTML, server-rendered over a persistent
connection, client-side WebAssembly in the browser, and native desktop controls. A design
must state which of these it applies to, and why any it excludes are excluded.

### Budgets that bind

- Released browser artifacts are measured in CI against a size limit. The trimmed
  WebAssembly framework output has a hard limit of 15 MB, at which the build fails, and a
  warning threshold of 10 MB. Anything added to release builds is spent against that
  budget. The measurement is in whole megabytes, so it is a coarse instrument: passing it
  is not evidence that an addition is free.
- Rendering performance is gated in CI against a 5% regression threshold.
- The browser and native heads run under trimming and ahead-of-time compilation. Anything
  that relies on reflection over the model type, or on serializing an arbitrary model
  without generated metadata, is not reliably available in a released application. The
  codebase already carries the marks of that constraint; look for them before assuming
  otherwise.

### Out of scope for this pass: durable undo

Stated as an exclusion rather than left as an omission, so that no design reads the
silence as an opening.

**Undo does not have to survive the application going away and coming back.** A page
reload, a process restart, and a dropped-then-re-established connection on the
server-rendered head may each begin with no history at all, and a design that behaves that
way is not deficient for it. Nothing in this pass requires the history to be held anywhere
but in the memory of the running application, and no design owes an argument for or
against holding it elsewhere.

Two consequences, so the exclusion is not read wider or narrower than it is:

- A design is **not** to be ranked up for offering durability, and **not** to be ranked
  down for lacking it. If durability falls out cheaply, that is a remark for the handoff
  and a candidate for a later pass — not a requirement met here.
- The exclusion is about *surviving the application's own disappearance*. It does not
  weaken anything above. Undo must still work across the whole of a single running
  session, and the budgets, the head coverage and every invariant below apply unchanged.

---

## Invariants

Every one of these must be expressible as a property over the whole input space, and
`06-spec.md` must carry at least one property per id.

**On the ids.** They are scoped to this pass and this directory. They are stable for its
life: an amendment may append an id, and never renumbers an existing one, because
`05-critic.md` and `06-spec.md` refer to them by id and a renumber would silently
re-point those references. The ids are *not* repository-wide, and the form is not free —
the artifact validator reads `INV-<n>` and nothing else, so an id cannot be qualified by
renaming it without making it invisible to the very chain the id exists to feed.
`INV-5` below is therefore this pass's fifth invariant and is unrelated to the `INV-5`
that numbers the verdict-cache set in `.claude/hooks/`; the two are told apart by the
qualifier carried on the id line itself, so that a grep hit disambiguates without needing
its surrounding context.

- **INV-1 — One history for the application.** For any interleaving of user actions across
  different, independent parts of the interface, a single undo returns the application to
  the state immediately before the most recent action, whichever part of the interface
  that action came from. *Falsified by:* acting in region X, then in region Y, then
  undoing, and arriving at the state before X's action. Quantified over histories in which undo
  is available; a typed refusal under INV-5 is not a violation of this invariant.

- **INV-2 — Undo and redo land only on reachable states.** The state this quantifies over
  is not the model alone. It is the model together with the world the application has
  already committed to, and **on any head where the application has one, the location the
  user is at is part of that world.** A navigation is a commitment in both directions: one
  the application asked for, and one the browser delivered when the user pressed back or
  forward. Either way the location is as much part of where the application is as the model
  is, and the two move together or not at all. For any sequence of actions interleaved with
  undo and redo operations, the resulting state — model and location together — is one that
  some sequence of ordinary actions alone could also have produced. No undo may put the
  application into a state it was never in and could never have been in, and that includes
  any pairing of a model with a location the two were never in together. *Falsified by:*
  any generated action/undo/redo sequence whose resulting model is outside the set of
  models reachable by ordinary interaction; any such sequence containing a navigation the
  browser delivered, after which the resulting model and the location the user is at are a
  pairing no sequence of ordinary actions could have produced; and any such sequence after
  which the browser's own back/forward stack and the application's history disagree about
  where the user is.

- **INV-3 — Undo and redo are inverse.** If undo is available in state `s` and produces
  `s'`, then redo applied to `s'` produces a state indistinguishable from `s`: the same
  model value, a rendered document equal to the one rendered from `s` **up to renaming of
  event-handler command ids**, and the same subsequent behaviour — meaning that any message
  dispatched afterwards yields the same `(model, command)` pair it would have yielded from
  `s`. State owned by the document rather than by the model — caret position, focus, scroll
  offset, `<details>` open state, and the values of uncontrolled inputs — is **outside this
  invariant and outside this pass**. *Falsified by:* any undo-then-redo round trip whose
  resulting model differs, whose rendered document differs other than in handler command
  ids, or after which some message produces a different `(model, command)` pair than it
  would have before.

- **INV-4 — The ends of the history are no-ops.** Undo with nothing left to undo leaves
  the application unchanged. Redo with nothing left to redo leaves the application
  unchanged. Neither faults, throws, nor leaves the application partly changed.
  *Falsified by:* any exception, any state change, or any emitted effect at either end.

- **INV-5** *(this pass's INV-5; unrelated to the `INV-5` in `.claude/hooks/`)* **— A
  refusal is typed, and it explains itself.** For any application state in which undo is
  requested and the design declines to perform it for any reason other than there being
  nothing left to undo, the application receives a value that names the reason, delivered
  along the same path that carries every other message into the application. The refusal
  is a value in the application's own vocabulary, not an exception, not a log line, not a
  silence. It must be **distinguishable by the application** from the no-op INV-4 demands:
  an observer with access only to what the application received must be able to tell *"there
  was nothing to undo"* from *"undo was declined, for this reason"*. *Falsified by:* any
  declined undo that leaves the application with nothing to observe and so is
  indistinguishable from INV-4's no-op; any decline reported by a route other than the
  ordinary message path; and any decline whose carried value does not identify which reason
  applied.

- **INV-6 — Availability is answerable from the history alone, in advance.** For any value
  of whatever a design uses to hold the history, whether undo is available, and if it is
  not then why, are both computable from that value by itself — before undo is attempted,
  without attempting it, without consulting anything outside the value, and without causing
  any effect. The computed answer must agree with what actually happens when undo is then
  attempted on that same value. *Falsified by:* any generated history value where the
  answer computed in advance disagrees with the outcome of actually attempting undo; any
  history value for which the answer cannot be reached without looking outside the value;
  and any design in which the reason for unavailability exists only after the refusal
  rather than before it.

- **INV-7 — Nothing starts, stops or arrives on the way.** For any sequence of undo and
  redo operations, no subscription is started, stopped or torn down, and no subscription
  delivers a message into the application, on account of any state the application merely
  passed through in getting to the state the user asked for. A movement is a single undo or redo
  request, or a maximal run of them bracketed by an explicit hold and settle. "The state the user
  asked for" is the state at the end of the movement; the states between a bracket's hold and its
  settle are the states passed through. Reconciling subscriptions once
  against the state finally settled on is permitted and is not a violation; the invariant
  governs the transit, not the destination. The key prefix `abies:history:` is reserved for
  subscriptions the history mechanism declares for itself: any such subscription is part of the
  movement and not activity on account of a state passed through, and the invariant quantifies
  over application-declared subscriptions. No such subscription exists in the design as it
  stands, so the carve-out is currently empty and the invariant holds unqualified; the prefix
  stays reserved, and no application-declared subscription may use it. *Falsified by:* any generated undo/redo sequence
  during which a subscription starts, stops or delivers a message on account of a state only
  passed through — equivalently, any such sequence whose total subscription activity differs
  from that of a single reconciliation against the settled state alone.

---

## Degrees of freedom — deliberately open

These came from the scoping exchange with the user and are the questions this pass exists
to answer. Nothing about them is settled, and the user has asked specifically that none of
them be narrowed on a guess.

1. **At what level undo operates** — what the history consists of, and what the unit of a
   single undoable step is. The user calls this the central question. Derive it; do not
   adopt one and justify it afterwards.
2. **What is retained, and how much** — whether the history is bounded, and if so, by what
   and how.
3. **What happens when the user acts after undoing** — the forward history can be
   discarded, kept, or handled some other way. Whichever, argue for it.
4. **Which parts of application state, if any, are exempt from undo** — not all state is
   necessarily the user's to undo.
5. **Whether any of the existing debug-only machinery needs to move into release builds,**
   and what that costs against the size budget above.

---

## Done means

A design is complete for this pass when all of the following hold.

- It satisfies INV-1 through INV-7, and each invariant has a stated way of being falsified.
- It states how it meets INV-5 and INV-6 specifically: what the refusal value is, and what
  the history exposes that lets availability be answered before undo is attempted.
- It answers all five open questions above by reasoning from the problem's structure, not
  by preference.
- It states its relationship to the existing debug-only capability explicitly — reuse,
  extension, relocation, or separation — with the reason.
- It states what happens at the effect boundary during undo. **The policy is settled: undo
  refuses to cross an action that has already spoken to the world** (gate 2). What remains
  open is the mechanism — what the refusal value is, how the boundary is recorded in the
  history, and whether the application may name an inverse that makes a particular boundary
  crossable.
- It states which of the four heads it applies to.
- It states its memory behaviour over a long-running session.
- If anything moves into release builds, it states the cost against the size budget and
  the performance gate.
