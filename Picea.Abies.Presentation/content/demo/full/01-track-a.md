# 🧠 Track A — First Principles

Derived from `00-scope.md` and the code under `Picea.Abies/`, `Picea.Abies.Browser/`,
`Picea.Abies.Server/`, `Picea.Abies.Native/`, `Picea.Abies.Conduit.App/` and
`Picea.Abies.SubscriptionsDemo/`. No retrieval; no prior art.

---

## Problem Decomposition

### What the running application actually is

Reading `Runtime.Dispatch` and `Program.cs`: the application's life is a single totally
ordered walk through a labelled transition system.

```
s₀ ──m₁──▶ s₁ ──m₂──▶ s₂ ── … ──mₙ──▶ sₙ
```

States are `TModel` values. Labels are `Message` values. The order is total because every
message funnels through one `Dispatch`, and `_decisionGate` plus the single `_core`
serialise them. There is exactly one walk because there is exactly one kernel.

Undo and redo are therefore not "an operation on state." They are **a movable cursor over
a walk that is being extended at one end.** Everything else follows from that.

### Irreducible constraints

1. **The walk is a total order.** One kernel, one funnel. So the history is a sequence, and
   any per-region history would have to be a projection of this sequence — which the scope
   forbids anyway, and which the structure would not support cleanly. INV-1 is not a
   requirement to engineer; it is the shape the system already has, and any design that
   loses it has *added* something.

2. **The cursor may only land on points of the walk.** INV-2. The set of legal destinations
   is the set of states the walk has passed through, plus the ordinary successors of those.
   *Any design that computes a past state rather than remembering one widens the legal set
   by exactly the amount the computation gets wrong.* That widening is unfalsifiable from
   inside — a property test over an application's own inverse functions checks the author's
   arithmetic against the author's arithmetic.

3. **Redo must be single-valued.** INV-3 says redo undoes undo. A relation that is not a
   function has no left inverse. So from any position the cursor has at most one forward
   destination.

4. **Forward re-execution is not reproducible.** `Picea.Abies.SubscriptionsDemo/Program.cs`
   calls `DateTimeOffset.UtcNow` inside `Transition`. `Transition` is therefore not a
   function of `(model, message)` across all applications. Re-running it on the same inputs
   can produce a different state. This is not a defect to be fixed elsewhere; it is a fact
   about the input space this design must be correct over. **It kills every design that
   reconstructs a past state by re-running the transition function.** Such a design cannot
   satisfy INV-3, because the round trip lands somewhere the original walk never was.

5. **State that leaves the kernel cannot be recalled.** `Transition` returns
   `(TModel, Command)`. The `Command` half is handed to an interpreter outside the kernel and
   reaches the network (`FavoriteArticle`, `AddComment`), storage (`PersistSession`) and
   browser history (`Navigation.PushUrl`). The full observable state is `model ⊗ world`.
   The cursor can move over the `model` factor. It cannot move over the `world` factor.

6. **No reflection over the model, no serialisation without app-supplied metadata.** The
   trimming/AOT constraint. `Runtime.TrySetCoreState` uses reflection and is documented
   there as best-effort; `GenerateModelSnapshot` degrades to `ToString()` without a
   `JsonTypeInfo<TModel>`. Both are debug-only for exactly this reason. A release design may
   *hold a reference to* a `TModel`; it may not look inside one.

7. **Models are immutable and are built by `with`.** Every `with` shares all untouched
   subtrees. So the past states of the walk **already exist in memory** during the forward
   walk; the collector reclaims them only because nothing holds them. Retaining a history of
   states costs no copying and no serialisation: it costs *deferred collection* of
   allocations the program has already made. This is the single most consequential property
   of this codebase for this problem.

8. **Subscriptions are a pure function of the model, reconciled by diffing.**
   `Runtime.Render` calls `TProgram.Subscriptions(state)` then `SubscriptionManager.Update`,
   which is a set difference between desired and running. So subscription churn is caused by
   *rendering an intermediate state*, not by moving. A movement that renders exactly one
   state causes exactly one reconciliation.

9. **`Decide` returning zero events is already a total no-op.** In `Runtime.Dispatch`,
   `decidedEvents.Length == 0 → shouldShortCircuit → return Ok`. No transition, no render, no
   effect, no exception. INV-4 is a path the runtime already has; it does not need building.

### Degrees of freedom

- **Which points of the walk are stops.** The history is a *subsequence* of the walk: a
  monotone injection from history index to walk index. "At what level undo operates" is
  exactly this choice, and nothing else. This is the central question and it is a choice of
  quotient, not a choice of mechanism.
- **Where the cursor lives** — inside the model (visible to the transition function) or
  beside it (visible to the runtime, which additionally sees documents and patches).
- **How far the cursor may reach** — the extent over which the `world` factor stays
  consistent with the `model` factor.
- **What bounds retention.**

### Structural shape

A cursor over a growing sequence, where the sequence's elements are values in a lattice of
structurally shared immutable trees, and where moving the cursor must be an **atomic,
effect-free, single-observation** step. That last clause is what the subscription
requirement really says: *the movement must have no interior.*

---

## Candidate A1: The outer walk — history as a model, undo as a transition

**Derived from:** constraints 1, 2, 4, 6, 7, 8, 9. Chiefly from 6 and 7: the framework may
hold `TModel` references but may not inspect them, and holding them is nearly free.

### How it works

Add a type that is a `Program` whose model *contains* an application model:

```
Chronicle<TModel>  =  (Past : TModel[], Present : TModel, Future : TModel[])

Chronicle<TCore, TModel, TArgument> : Program<Chronicle<TModel>, TArgument>
    Initialize(a)        = let (m, c) = TCore.Initialize(a) in (Chronicle([], m, []), c)
    View(h)              = TCore.View(h.Present)
    Subscriptions(h)     = TCore.Subscriptions(h.Present)
    IsTerminal(h)        = TCore.IsTerminal(h.Present)
    Decide(h, Undo)      = h.Past   is empty ? Ok([]) : Ok([Undo])
    Decide(h, Redo)      = h.Future is empty ? Ok([]) : Ok([Redo])
    Decide(h, m)         = TCore.Decide(h.Present, m)
    Transition(h, Undo)  = (pop-backward h, Command.None)
    Transition(h, Redo)  = (pop-forward  h, Command.None)
    Transition(h, m)     = let (next, cmd) = TCore.Transition(h.Present, m) in
                           (record h next cmd, cmd)
```

This is the same composition shape the codebase already uses in
`WithView<TCore, TView, TModel, TArgument>` (`Picea.Abies/Program.cs`): a `Program` built by
forwarding another `Program`'s static members. An application opts in by changing one type
argument at its `Runtime.Run` call. **No line of `Runtime.cs` changes.**

`record h next cmd`:

- If `ReferenceEquals(next, h.Present)` — the transition constructed nothing — the walk did
  not move and no stop is recorded. Idiomatic transitions in this codebase return `model`
  unchanged in their fall-through arm (`_ => (model, Commands.None)` in both Conduit and the
  subscriptions demo), so this is an O(1) test that costs nothing and admits no false
  negatives.
- If `cmd` is not `Command.None`, the transition has spoken to the world. `Past` is
  **truncated to empty** and `Present` becomes `next`: the post-commitment state is the new
  origin of the walk. See "the effect boundary" below for why this is forced rather than
  chosen.
- Otherwise push `h.Present` onto `Past`, set `Present = next`, and set `Future = []`.

`Past` is a bounded buffer with eviction at the far end only.

### Structural property

**Stratification.** The cursor lives in a layer that the layer it navigates cannot see.
`TCore` never receives a `Chronicle`; it only ever receives a `TModel`. Consequently `Undo`
is not a point in the walk it moves over — which is what makes the cursor well defined. Had
undo been an ordinary transition of the inner model, "undo the undo" would be a fixed-point
question with no answer, and INV-3's inverse law would have no domain. The stratification is
not tidiness; it is what gives the inverse law something to be an inverse *on*.

**INV-2 holds by construction, not by argument.** `Past` and `Future` contain only values
that were `Present`. The reachable set is not widened by a single element, because nothing
is ever computed — only moved.

**INV-3 holds by identity, not by equivalence.** Undo moves `Present` into `Future` and pops
`Past`; redo moves it back. The object restored is the *same reference*. There is nothing to
compare.

**INV-4 falls out of `Dispatch`.** `Decide` returns `Ok([])` at either end. The runtime
short-circuits before any transition, render, or interpretation. Not `Err` — an `Err`
dispatches an error message and *does* transition (`Runtime.cs` lines 482–489). The
distinction is load-bearing and is the kind of thing a spec must pin.

**The movement has no interior.** One `Transition` produces one new `Present`; `Observe`
renders once; `Subscriptions` is evaluated once, on the settled state; `SubscriptionManager`
diffs once, from the pre-undo running set straight to the post-undo desired set. No
subscription is started, stopped or torn down *on the way*, because there is no way — the
design never constructs an intermediate state. The scope's hard requirement is satisfied
**vacuously**, which is a stronger position than satisfying it by suppression.

**No message can interleave inside the movement.** The movement is one message through the
one funnel; `_decisionGate` and the single `_core` serialise it against anything a
subscription dispatches. One derived rule, though: **the destination must be computed in
`Transition`, never in `Decide`.** `Dispatch` releases `_decisionGate` between the two
(`Runtime.cs` 448–500), so a destination computed during decision could be stale by the time
it is applied. `Decide` may only answer the yes/no question; being wrong about that degrades
to a `Transition` that pops an empty stack, which is the identity.

### Answers to the five open questions

1. **Level.** A step is *one constructed model along a world-silent run*. The quotient is:
   collapse adjacent walk points whose transition constructed nothing. Detected by reference
   inequality; no equality traversal, no reflection, O(1). Coarser quantisation than this is
   not available from inside the model, because the model layer cannot see the rendered
   document (see A2, which can).
2. **Retention.** Two bounds, both emergent rather than configured. The *primary* bound is
   the commitment truncation: `Past` never outlives the current world-silent run, which in
   Conduit is a form-filling burst. The *backstop* is a fixed-capacity buffer for
   applications that emit no commands at all (the counter demo would otherwise grow without
   limit). The backstop's size is a human constant — the depth of the user's own memory of
   the session — so order 10², not order 10⁴ as the debug timeline uses.
3. **Acting after undoing.** `Future` is cleared. Forced, not preferred: keeping the
   abandoned continuation would give the cursor two forward destinations from the branch
   point, redo would become a relation rather than a function, and a relation has no
   inverse — INV-3 would be unstatable. Linearising the branches chronologically instead
   just reproduces the original walk, so branching buys nothing under INV-3.
4. **Exemptions.** *None spatially; all temporally.* The framework cannot see inside
   `TModel` without reflection (constraint 6), so exemption cannot be "these fields." It must
   be "these intervals" — and the interval boundaries are the commitments. This is the
   answer the constraint forces, and it is worth stating plainly: **the AOT/trimming
   constraint converts the question "which state is exempt" into "which intervals are
   reachable."**
5. **Debug machinery.** Separation, with one small relocation. Judged on what is in
   `Picea.Abies/Debugger/`: `DebuggerMachine` stores serialised previews plus boxed
   snapshots, and `Runtime.TrySetCoreState` restores by walking properties and fields
   reflectively with a `catch { }` around it. Capture-by-serialisation and
   restore-by-reflection are precisely the two things the release constraints forbid, so
   nothing in that path can move. The only reusable piece is `RingBuffer<T>` — a generic
   bounded buffer with no debug-specific content — which can leave `#if DEBUG`. The
   interesting relationship runs the *other* way: once a release-build history exists,
   the debugger could read it instead of maintaining a parallel timeline, which would make
   the debug path smaller. That is a follow-on pass, not this one.

### The effect boundary

**During the movement:** nothing. `Transition(h, Undo)` returns `Command.None`. There is no
effect to suppress because none is produced — again, vacuous rather than suppressed. Contrast
`Runtime`'s existing `_replay` flag, which discards commands and skips subscription
management (`Runtime.cs` 212–220, 288–291, 342–346): that flag is set in the constructor and
cannot be entered and left during a live session, so it is unavailable to a live undo and
should not be stretched to fit.

**Effects already gone:** the design **refuses to undo across them.** A transition that
emitted a command has told the world something; restoring the model to before that point
puts the model into disagreement with an authority it does not control. Concretely in
Conduit: undoing across `LoginSubmitted` restores `IsSubmitting = false` while a request is
in flight, so a second submit double-posts; undoing across `FavoriteArticle` shows
`Favorited = false` while the server holds `true`. **Both of those are states that ordinary
interaction could not have produced** — even though both were literally visited, which is why
INV-2 as written does not catch them (see "constraint the scope is missing" below). Refusing
is one of the three answers the scope admits, and it is the only one available to a framework
that cannot know what a given command means. A3 shows what an application must supply to buy
its way across.

**The URL is an effect too.** `Navigation.PushUrl` is a `Command`, and browser history is a
second cursor over a second history. Because commitments truncate the history, undo can
never cross a navigation in this candidate, so the URL and the model can never disagree, and
the browser's own back button remains the only cursor over navigation. INV-1's "one history"
is preserved in the strong sense: two cursors never coexist.

### Which heads

- **Static** — excluded. `RenderMode.Static` has no MVU loop (`RenderMode.cs`: "MVU Loop:
  None"); the walk has one point; a cursor over a one-point sequence is the identity. Not a
  limitation, a category error.
- **Server-rendered** — applies. The history lives in the per-session `Runtime` inside
  `Session<>`; it dies with the session, as the model does. Retention multiplies by
  concurrent sessions, which is why the commitment truncation (not a large fixed capacity)
  is the primary bound.
- **WebAssembly** — applies. Size cost below.
- **Native** — applies. Same AOT constraint as WASM, no size budget.
- **Auto** — applies *after* the handoff only. The server session is disposed and a client
  session starts; carrying the history across would require serialising past `TModel`s, which
  is exactly what constraint 6 forbids. So the history must **begin** at the handoff, and
  undo before it must be unavailable rather than wrong. This should be stated in the spec
  rather than discovered later.

### Size and performance

The added type is generic and lives in a library. An application that does not name
`Chronicle<...>` never instantiates it, so there is no instantiated code for the trimmer to
keep. The expected release-artifact cost for a non-adopting application is therefore near
zero — but the scope is right that whole-megabyte measurement cannot demonstrate "free", so
this needs measuring against the 10 MB warning line, not assuming.

Per-message cost when adopted: one reference comparison, and one array/stack push on
stops. Both are O(1) and neither touches `Render`, so the 5% rendering gate is untouched by
construction. Undo itself costs one `View` + one `Diff` — the same as any message.

### Feels like

Giving the application a second, outer application whose only job is to remember where the
first one has been, and which the first one cannot see.

---

## Candidate A2: The perceptible-step ledger — history beside the runtime, quantised by what changed on screen

**Derived from:** constraints 2, 7, and a property of `Runtime.Render` the model layer cannot
reach: the patch list is *already computed* on every transition, and `_currentDocument` is
already retained.

### How it works

The history lives in `Runtime<TProgram, TModel, TArgument>` as a bounded sequence of
`(TModel model, Document document)` pairs. `Render` already computes
`Operations.Diff(_currentDocument?.Body, newDocument.Body)`; the ledger admits a new stop
**iff that diff was non-empty** — that is, iff the transition changed something a person
could see.

Adjacent stops **coalesce** when their patch sets have the same *write footprint*: the same
node ids, the same attribute or text slots, and no structural patches (`AddChild`,
`RemoveChild`, `ReplaceChild`, `ClearChildren`). Two consecutive writes to the same cell
compose under last-write-wins without losing anything a person could name as a separate act;
a structural patch changes *which cells exist* and so cannot be absorbed.

Undo restores the stored pair: `_core`'s state goes back to the stored model, and `Render`
diffs `_currentDocument` against the **stored document** rather than re-rendering.

### Structural property

**Undo is defined on the observational quotient.** Two walk points with the same rendered
document are indistinguishable to the user; a history containing both admits an undo that
appears to do nothing, and an undo that appears to do nothing is indistinguishable from a
broken undo. So document-distinctness is *necessary* for the history to be honest, and this
candidate makes it definitional. The resulting guarantee is strictly stronger than INV-4:
not merely "the ends are no-ops" but **"nothing but the ends is a no-op."** That property is
worth a spec line of its own.

**Restoring the document, not re-rendering it, makes INV-3 literal.** `Events.NextCommandId`
draws from a global `Interlocked.Increment` counter at *render* time (`Events.cs` 46–57), so
`View` is not a function of the model: two renders of one model differ in their
`data-event-*` command ids. A design that re-renders satisfies INV-3 only up to renaming of
handler identities. A design that keeps the past `Document` and diffs back to it restores the
original handler ids exactly — `UpdateHandlerRegistry` re-registers them from the patches —
so the round trip is identity on the document as well as on the model. **This is the only
reason to prefer a runtime-resident history over an in-model one, and it is a real reason.**

**The quantiser is free.** The predicate "did this change anything visible" is
`allPatches.Count > 0`, which `Render` computes anyway. No new traversal, no equality walk,
no reflection.

### Answers to the five open questions

1. **Level.** A step is a *maximal run of writes to one footprint*. Typing coalesces into a
   single undoable edit; a click that adds or removes an element does not coalesce with
   anything. Nobody declares anything; the rule is read off the patch algebra.
2. **Retention.** Bounded by stop count in the existing `RingBuffer<T>` shape. Both halves of
   each entry are structurally shared — models by `with`, documents by the `lazy` memo cache
   in `Elements` — so the marginal cost of a stop is the nodes that actually changed. Cost is
   roughly doubled relative to A1 (documents as well as models), which is the price of the
   literal INV-3.
3. **Acting after undoing.** Forward ledger cleared. Same forcing argument as A1.
4. **Exemptions.** Same temporal answer as A1: commitments segment the ledger. The runtime is
   better placed to detect them than the model layer, since it already interprets `Command`
   and can see `Command.None` before the interpreter runs.
5. **Debug machinery.** Partial relocation, and more of it than A1 needs. `RingBuffer<T>`
   moves out of `#if DEBUG`; the cursor arithmetic in `DebuggerMachine` (`AtStart`, `AtEnd`,
   `ResolveCursor`, the step/jump transitions) is generic sequence-cursor logic that this
   candidate needs in release and could share. The serialisation and reflective-restore paths
   stay debug-only and unchanged. This candidate is the one that would let
   `DebuggerMachine` stop maintaining its own timeline.

### The effect boundary

During movement, nothing: the restore path never calls `TProgram.Transition`, so no `Command`
is produced. Subscriptions are reconciled once, on the settled model, by the existing
`Render`. Effects already gone are handled by the same commitment segmentation as A1 — the
ledger is truncated at a transition that returned a non-`None` command.

### Which heads

All four minus Static, exactly as A1, with the same handoff finding for Auto — and one extra
cost: this history lives in `Runtime`, which **every** application instantiates. Unlike A1,
the code cannot be trimmed away for non-adopting applications, so the release-size cost is
paid by every browser artifact whether the feature is used or not. Against a 15 MB hard
limit and a 10 MB warning that this pass is explicitly told not to treat as slack, that is
the decisive objection to this candidate, and it is an objection A1 does not have. A gating
flag would not fix it: the retained code is what costs, not the retained data.

### Feels like

Letting the screen decide what counts as one action, because the screen is the only thing the
user was ever looking at.

---

## Candidate A3: Named projection and adjoined inverses — buying reach across a commitment

**Derived from:** constraint 5 and the refusal in A1/A2. Both of those stop the cursor at the
first commitment. This candidate asks what an application must supply to move it further,
and answers in terms of the algebra rather than in terms of policy.

### How it works

Two independent widenings, each opt-in, each with a law that can be tested.

**(a) The projection.** The application names a part of its model that is *its own*, together
with a write-back:

```
get : TModel → TUndoable
put : (TUndoable, TModel) → TModel
```

subject to two round-trip laws, both property-testable over the whole input space:
`put(get m, m) = m` and `get(put(u, m)) = u`. The history stores `get(sᵢ)` rather than `sᵢ`,
and undo produces `put(get(sᵢ), current)`. The world-mirror part of the model — whatever the
interpreter's feedback writes — is left at its current value and is a fixed point of undo.

This dissolves the failure A1 accepts: after typing in the editor and having a fetch land,
undo restores the text without resurrecting a stale `IsLoading` or discarding the fetched
article, because those live outside the projection.

**(b) Adjoined inverses.** For a command the application knows how to reverse, it supplies
`inverse : Command → Command option`. A commitment is crossable iff its command has an
inverse; crossing dispatches the inverse command at the settled endpoint. Where no inverse
is named, the commitment is a barrier as in A1.

### Structural property

**Undo reaches exactly as far as the effect algebra is invertible.** Commands compose under
`Commands.Batch` with `Command.None` as identity — a monoid, and nothing more. A monoid
element has no inverse in general, which is the precise reason a framework alone cannot undo
an effect: *there is no inverse to apply, not merely no code to apply it.* An application
that names inverses is enlarging a sub-structure of that monoid into a group, and undo's
reach is exactly that sub-structure. This reframes "what happens to effects that have
already left" from a policy question into a statement about which sub-algebra the
application has closed under inversion. Everything outside it is a barrier, by definition
and not by choice.

**The projection makes exemption spatial again — at the application's expense, not the
framework's.** Constraint 6 says the framework cannot see inside `TModel`. The projection
does not violate that: the application supplies both directions as ordinary generic
functions, so no reflection and no serialisation are involved and AOT is unaffected. The cost
is moved to the only party that can pay it.

**It also changes what is retained.** `get(sᵢ)` is smaller than `sᵢ`, sometimes far smaller,
so the retention bound in A1/A2 becomes a bound on the projection rather than on the whole
model.

### Answers to the five open questions

1. **Level.** Inherited from whichever of A1/A2 it composes with, refined by one rule: a stop
   is recorded only when the *projection* changes. Transitions that move only world-mirror
   state are invisible to the history. That is a coarser and more faithful quotient than
   either candidate achieves alone.
2. **Retention.** Bounded as in A1, over projections rather than models.
3. **Acting after undoing.** Unchanged; the forcing argument is about redo's single-valuedness
   and does not depend on what is stored.
4. **Exemptions.** Exactly the complement of the projection, named by the application, checked
   by two laws. This is the only candidate that answers question 4 spatially, and the only one
   that requires anything of the application.
5. **Debug machinery.** Same as A1: separation, with `RingBuffer<T>` relocated.

### The effect boundary

Named inverses are dispatched **at the settled endpoint only** — never on the way, since there
is no way. The scope explicitly leaves the endpoint open, and this is the use of that
latitude. The URL case is where this pays: an application that supplies `Route.ToUrl` (Conduit
already has it) makes navigation invertible, and undo across a navigation issues one
`NavigationCommand.Replace` — *replace*, not *push*, so the browser's history does not grow
during undo and the two cursors do not multiply. That choice is forced by INV-1's "one
history," not chosen for tidiness.

### Which heads

All four minus Static. The projection and inverse functions are ordinary code and are
platform-free, so nothing head-specific arises. Size cost is the application's, in its own
assembly.

### Feels like

Telling the framework which half of the state is yours to take back, and paying for anything
further by naming its opposite.

---

## What these three are to each other

They are not three answers to one question. They settle three different axes:

| Axis | A1 | A2 | A3 |
|---|---|---|---|
| Where the cursor lives | in the model | beside the runtime | either |
| What one step is | one constructed model | one perceptible change, coalesced by write footprint | one change to the named projection |
| How far it reaches | to the last commitment | to the last commitment | as far as the invertible sub-algebra |

A1 and A2 are genuine rivals — the choice between them is "literal INV-3 and a free
quantiser" against "trimmable to zero for non-adopters", and the size budget as stated
argues hard for A1. A3 is orthogonal to both and composes with either.

If the pass wants one direction from this track: **A1, with A3's named inverses available as
an opt-in, and A2's coalescing rule approximated in-model.** The in-model approximation of
A2's quantiser is reference-inequality followed by structural equality on the (rare) cases
where a new instance was constructed — strictly finer than document-distinctness, since
different models can render identically, but it needs nothing the model layer cannot see.

---

## Reasoning Trail

**Where the derivation started.** The scope's phrase "one history for the running
application" is not a constraint to be enforced — it is a description of what a single
kernel with a single dispatch funnel already is. That reframing turned the whole problem
from "build a history" into "expose the cursor over the walk that already exists," and
everything after followed from asking what that cursor is allowed to touch.

**Lines followed and abandoned, with reasons:**

1. *Walk the DOM backwards using inverse patches.* The runtime already computes a patch list
   per transition, and a patch list is a morphism between documents; if patches were
   invertible given the old document, the screen could be rewound with no model involvement
   at all. Abandoned: the model would then disagree with the document, and the next ordinary
   transition would render from the stale model and diff against a document that no longer
   matches it. INV-2 is violated immediately and loudly. Kept as evidence that "the cheapest
   thing to store" is not the right question.

2. *Replay the message log from the origin.* Abandoned for three independent reasons, any one
   of which is fatal: (i) `Transition` reads the clock in at least one application in this
   repository, so re-running it does not reproduce the walk, and INV-3 fails; (ii) messages
   returned by the interpreter cannot be re-obtained without re-issuing the effects that
   produced them; (iii) replay must fabricate the suppression of effects that the transition
   function genuinely asked for — a lie about what the program said. `Runtime`'s existing
   `_replay` flag is precisely that quarantine, and it is set in the constructor, so it
   cannot be entered and left during a live session. That constructor-time placement is
   itself evidence that replay is not a live-session mechanism.

3. *Let the application supply an inverse for each event and dispatch it forward.* Rejected on
   INV-2. Any scheme that *computes* a past state rather than *remembering* one widens the
   reachable set by exactly the amount the computation gets wrong, and no property test can
   close that gap, because the inverse and the forward function come from the same author and
   the test would check them against each other. Remembering has no such gap: a remembered
   state was, by definition, reached. This argument is the reason all three surviving
   candidates store rather than derive, and it is the load-bearing step of the whole
   derivation.

4. *Why storing is affordable here specifically.* Once (3) forced storing, the question was
   what storing costs. Because models are immutable and are built with `with`, every past
   state is a tree that shares all its untouched subtrees with its successors, and every one
   of those trees was allocated by the forward walk anyway. A history therefore does not
   *allocate* — it *defers collection*. That is not true of a system with mutable state, and
   it is why a design that would be extravagant elsewhere is close to free here. Everything
   about retention bounds follows from this: the bound exists to cap deferral, not to cap
   allocation.

5. *Where to put the cursor.* Two viable homes. Inside the model, the transition function can
   see it and undo becomes an ordinary message — which immediately raised the fixed-point
   problem (is undo itself undoable?) and resolved it by stratification: the navigating layer
   must not be a point in the domain it navigates, or the inverse law has no domain. Beside
   the runtime, the cursor additionally sees documents and patches, which turned out to matter
   for a reason not visible from the model layer: `Events.NextCommandId` uses a global
   counter, so `View` is not a function of the model and re-rendering a past model yields a
   document that differs in handler identities. That is the whole of A2's advantage, and it
   collides with the size budget, since `Runtime` is on every application's path.

6. *What one step is.* This is the scope's central question and it resolved into: the history
   is a subsequence of the walk, so "level" is a choice of quotient. Three quotients were
   derived, in increasing coarseness: constructed-a-new-model (reference inequality, O(1),
   available in-model); changed-the-document (already computed by `Render`, available only in
   the runtime); changed-the-named-projection (needs the application). The middle one carries
   a property worth naming — every undo is visible — which strengthens INV-4 from "the ends
   are no-ops" to "nothing but the ends is a no-op." That property is the reason to care about
   the quotient at all: a history with invisible stops is indistinguishable, from the user's
   side, from a broken one.

7. *Why the forward continuation must be discarded.* Not a preference. INV-3 requires redo to
   be a left inverse of undo, and only functions have inverses. Retaining the abandoned
   continuation gives the cursor two forward destinations from a branch point, so redo becomes
   a relation and the invariant becomes unstatable. Linearising the branch chronologically to
   restore single-valuedness just reconstructs the original walk, so branching buys nothing.

8. *Why commitments are barriers.* The observable state is `model ⊗ world`; the cursor moves
   over the first factor only. Moving it back past a transition that spoke to the world leaves
   the two factors disagreeing, and the concrete Conduit cases are bad — a form that shows
   "not submitting" while a request is in flight admits a double post; a heart that shows
   unfavourited while the server holds favourited. Since the framework cannot know what any
   given `Command` means, refusal is the only position it can hold honestly, and A3 shows the
   exact price of moving further: an inverse element in the command monoid, supplied by the
   only party that knows one exists.

9. *Why the subscription requirement stopped being a requirement.* The scope demands that no
   subscription be started, stopped, torn down or delivered *on the way* to the destination.
   Once (2) removed replay from consideration, there is no "way": a jump produces one new
   state, `Render` runs once, `SubscriptionManager.Update` diffs once from the running set to
   the settled desired set. The requirement is met because the design has no interior, not
   because it suppresses anything. The one residual hazard — a subscription message arriving
   *during* the movement — is closed by the movement being an ordinary message through the
   one funnel, plus the derived rule that the destination is computed in `Transition` and
   never in `Decide`, because `Dispatch` releases `_decisionGate` between the two.

10. *Why INV-4 needed no mechanism.* Reading `Dispatch` closely: `decidedEvents.Length == 0`
    short-circuits before transition, render, interpretation and any exception path. So "undo
    with nothing to undo" is `Decide` returning `Ok([])`. It must not be `Err`, which
    dispatches an error message and does transition. The invariant is a path the runtime
    already has, and the design's job is to route into it rather than to build one.

**Two things the scope's invariants do not catch, which the next phases should know:**

- **INV-2 is necessary but not sufficient.** It quantifies over models reachable by ordinary
  actions. Every model in a stored history is trivially reachable — it was reached. So INV-2
  cannot detect the failure in constraint 5: undoing across a commitment lands on a model
  that *was* visited but that no longer agrees with the world. To catch that, INV-2 needs
  strengthening to quantify over `model ⊗ world`, or a fifth invariant is needed saying that
  the cursor never crosses a transition that emitted a command without also emitting its
  inverse. All three candidates satisfy INV-2 as written; only their barrier rule saves them
  from the failure INV-2 misses. Worth surfacing at the next 🛑.

- **INV-3's "same rendered document" cannot be literally true for any re-rendering design.**
  `Events.NextCommandId` draws handler command ids from a global monotonic counter at render
  time, so two renders of one model differ in their `data-event-*` attributes. INV-3 must
  either be read up to renaming of handler identities, or the design must store documents
  (A2) rather than re-render them. This is a real fork and the spec should say which reading
  it means, because a property test written against the literal reading will fail A1 and A3
  for a reason that has nothing to do with undo.

**Constraints I could not derive, because the information is not reachable from here:**

- `Picea` is an external package (`PackageReference Include="Picea"`), so `AutomatonRuntime`
  and `Decider` are not readable in this checkout. Three things follow that I have had to
  reason around rather than confirm: (i) whether messages returned by the interpreter re-enter
  through `TProgram.Transition` only, or also through `TProgram.Decide` — this decides whether
  A1's outer layer sees interpreter feedback as an ordinary message, which its commitment rule
  depends on; (ii) what `trackEvents: true` does, given `Runtime.Start` passes `false` — if the
  kernel already retains an event sequence, part of this problem may already be solved one
  layer down; (iii) whether `_core.State` is settable other than by reflection, which decides
  whether A2 can restore without the reflective `TrySetCoreState` path that release builds
  forbid. **If (iii) is negative, A2 is not viable in release builds at all**, and the choice
  collapses to A1 with A3. That is the single most important unknown in this artifact.

- The scope does not say whether undo is invoked through a framework-supplied affordance
  (a keyboard binding, a control) or purely by the application dispatching a message. All
  three candidates assume the latter, since it needs nothing new; a framework-supplied
  affordance would add browser-side code and therefore size, and would need its own answer.
