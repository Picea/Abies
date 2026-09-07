# ⚖️ Convergence Analysis — `undo-redo`

Read: `00-scope.md`, `00-knowledge.md`, `01-track-a.md`, `02-track-b.md`. Both tracks
landed; neither saw the other. Track A was not skipped, so this is a real convergence and
not a pass-through.

I have added no candidates. Where I judged a gap, I say so and leave it open.

**Load-bearing claims I re-verified in the code before ranking on them** (both tracks made
factual claims that decide the ranking, and a convergence that ranks on unverified claims
is just two opinions averaged):

| Claim | Track | Verified |
|---|---|---|
| `Transition` reads the wall clock in a shipped app | A (constraint 4) | ✅ `Picea.Abies.SubscriptionsDemo/Program.cs:133,140,142,151,160,169,179,189` — inside `Transition` |
| `View` is not a function of the model; handler ids come from a global counter | A (A2, Trail 5) | ✅ `Picea.Abies/Html/Events.cs:53-55,71,84` — `Interlocked.Increment` at element-construction time |
| `Decide` returning zero events short-circuits before transition/render/effect; `Err` does not | A (constraint 9, Trail 10) | ✅ `Runtime.cs:465-468` vs `:482-489` |
| `_decisionGate` is released between `Decide` and `_core.Dispatch` | A (derived rule), B (concurrency room) | ✅ `Runtime.cs:448-475` then `:492-500` |
| `WithView` is already a static-forwarding higher-order program | A (A1), B (B1, "most important local fact") | ✅ `Program.cs:64-85` |
| `InterpretCommand` returns `[]` under `replay`; `replay` is constructor-time | A, B | ✅ `Runtime.cs:334-346` — flag captured in `Start`, closed over |
| Debugger captures once after all decided events are applied | B | ✅ `Runtime.cs:502-510` |

---

## All Candidates

| ID | Name | Track | Core idea |
|----|------|-------|-----------|
| A1 | The outer walk — `Chronicle<TModel>` | First Principles | A `Program` whose model *contains* the application model as `(Past, Present, Future)`. Undo is a transition of the outer layer returning `Command.None`. A stop is recorded when the transition constructed a new model (reference inequality); `Past` is truncated when the transition emitted a command. No runtime change. |
| A2 | Perceptible-step ledger | First Principles | History of `(TModel, Document)` pairs held in `Runtime`, admitting a stop iff the render diff was non-empty, coalescing adjacent stops with the same patch write-footprint. Undo diffs back to the *stored* document rather than re-rendering. |
| A3 | Named projection + adjoined inverses | First Principles | The app supplies a lens `get : TModel → TUndoable` / `put : (TUndoable, TModel) → TModel` under two round-trip laws, and optionally `inverse : Command → Command option`. Undo's reach is exactly the invertible sub-algebra of the command monoid. A modifier, not a rival. |
| B1 | `WithHistory` — history as a higher-order program | Informed Design | `History<TModel>` list zipper (Huet 1997) wrapped by a static-forwarding `Program`, the exact analogue of `WithView`. Undo/redo are ordinary `Message`s returning `Command.None`. Depth 100, optional 500 ms coalescing, exemption by projection or by provenance, effects-not-recalled contract with opt-in compensation/deferral. |
| B2 | Event-sourced replay from a checkpoint | Informed Design | Retain decided events plus periodic checkpoints; move backwards by replaying into a replay-gated kernel and adopting the result. The ADR-025 lineage answer; the existing debugger is ~80% of it. Only candidate offering durable history. |
| B3 | Inverse-command stack | Informed Design | `QUndoStack`/`NSUndoManager` shape: each action registers its inverse; undo dispatches it forward. Best memory profile, `mergeWith()` grouping, `setClean()` dirty marker, and a path to selective undo. |

---

## Convergences

### C1 — The same object, derived twice: A1 ≡ B1

This is the strongest convergence I have seen in this repository, and it is not partial.
Independently, both tracks produced: a higher-order `Program` that wraps `TModel` into a
past/present/future triple; undo and redo as ordinary messages travelling the ordinary
dispatch path; the undo transition returning `Command.None`; subscriptions satisfied
because no intermediate state is ever constructed; the forward branch discarded; a bounded
`Past`; zero lines of `Runtime.cs` changed; and — independently — *both tracks pointed at
`WithView` as the existing local precedent for the composition*. Track A: "the same
composition shape the codebase already uses in `WithView`". Track B: "the single most
important local fact in this pass."

**What this tells us about the problem's structure.** The convergence is not evidence that
the zipper is a good pattern; it is evidence that four properties of *this kernel* jointly
force the shape, and that a designer who knows the properties reaches it whether or not
they know the pattern:

1. One dispatch funnel ⇒ the history is a sequence, so INV-1 is a description of the
   system rather than a requirement to engineer. Track A states this outright; Track B
   arrives at it through the survey and finds nothing to add.
2. `Transition` returns `(TModel, Command)` with effects already isolated behind a
   boundary ⇒ a state-restoring transition emits `Command.None` and there is nothing to
   suppress. Both tracks use the same word, *vacuously*, and both explicitly contrast it
   with suppression.
3. Subscriptions derived from the model inside `Render` ⇒ one settled state, one
   reconciliation. Both tracks note the scope's hard requirement is met by the design
   having no interior, and both note the residual honestly (a restarted timer resets its
   phase; Track B says so, Track A's "one reconciliation" implies it).
4. `Program` is a static-forwarding interface with `WithView` already exercising it ⇒ the
   wrapper needs no framework change, only a type argument at `Runtime.Run`.

The right reading is not "two tracks liked the zipper." It is: **the kernel already
contains the cursor; the design's job is to expose it, and the exposure has one shape.**
That is a stronger warrant for B1 than Track B's citations alone could give it, because
the citations show the shape works elsewhere and Track A shows it is forced here.

### C2 — Both tracks killed replay, from opposite directions

Replay is the option the codebase most invites: `Picea.Abies/Debugger/` plus the four
gated seams are, as Track B says, ~80% of B2 already. Both tracks rejected it as the
primary mechanism.

- Track B rejected it on **cost**: `replay` is constructor-time, so it must become a live
  mode (fifth seam, revisit trigger fires) or land back on `TrySetCoreState`; plus
  serialization obligations, `JsonPolymorphic`, and ADR-025 partially superseded. It also
  named Fowler's second-order problem — external *queries* during replay — that Track A
  had no way to know about.
- Track A rejected it on **correctness**: `Transition` reads the clock in a shipped app in
  this repository, so re-running the walk does not reproduce it and INV-3 fails outright.

Track A's kill is the stronger one and it is dispositive rather than economic: no amount
of budget makes a non-deterministic transition replayable. Track B's is the one that says
what it would cost if you tried anyway. The convergence matters because it settles open
question 5 with two independent arguments: **ADR-025's release-strip clause holds
unamended.** Track B flags — correctly — that the pass's framing invites the opposite
assumption and that the close-out drop should say the ADR was examined and found not to
need superseding.

### C3 — Both rejected computed inverses as the *primary* mechanism, on INV-2

Track A, Trail item 3: any design that computes a past state rather than remembering one
widens the reachable set by exactly the amount the computation gets wrong, and no property
test closes the gap because "the inverse and the forward function come from the same
author and the test would check them against each other."

Track B, B3: "correctness moves into application code. Every inverse is an opportunity to
produce a state no forward sequence could reach — a direct INV-2 hazard, and unlike B1 the
framework cannot enforce it. Berlage's paper is careful about exactly this."

Same argument. One derived it as a statement about what a property test can and cannot
witness; the other found three decades of toolkits and a 1994 TOCHI paper being careful
about it. When the derivation and the literature agree on *why* an option is weaker, that
option is weaker.

### C4 — Discard the forward branch, for two different and compatible reasons

Track A: INV-3 requires redo to be a left inverse of undo; only functions have inverses;
keeping the branch gives the cursor two forward destinations, so redo becomes a relation
and INV-3 becomes unstatable. Linearising the branch reconstructs the original walk, so
branching buys nothing.

Track B: every linear-undo system surveyed discards (redux-undo, `UndoList`, Qt `push()`,
`NSUndoManager`), and keeping the branch is *selective undo*, which is why the group-editor
literature (Sun 2002, Berlage 1994, Prakash & Knister 1994) exists.

Open question 3 is closed as firmly as anything in this pass gets closed: the derivation
says it is unstatable otherwise, the evidence says everyone who tried it found it hard.

### C5 — Same heads, same exclusion, same reason

Static excluded (no MVU loop — Track A calls it a category error, Track B calls it no
interaction to undo); Server, WASM and Native included with identical implementations
because the design lives entirely in `Picea.Abies`. Track A adds the Auto-render-mode
finding neither the scope nor Track B raised: the history must *begin* at the
server→client handoff, because carrying it across would require serialising past `TModel`s,
which the AOT/trimming constraint forbids. That belongs in the spec.

### C6 — Same effect contract, same escape hatches

Both tracks: undo/redo emit no effects; effects already dispatched are not recalled by the
framework; crossing further requires the *application* to supply the inverse. Track A
frames it algebraically (below); Track B supplies the precedent (Qt `undo()`, Fowler's
reversal requiring difference-shaped events) and a second escape Track A did not consider —
**deferral** (Gmail's Undo Send is a 5–30 s hold, not a reversal), which for genuinely
irrevocable actions is the correct answer and needs no undo machinery at all.

### C7 — Both refuse to treat the size gate as evidence

Both tracks independently restate that the integer-MB measurement cannot resolve a
sub-megabyte addition and must not be reported as a clean bill of health. Both defer to
`performance-engineer`. Neither measured anything, and neither pretended to.

---

## Divergences

### D1 — Stratification: undo must not be a point in the walk it navigates
**Track A only. Genuine novelty (of warrant, not of outcome).**

Both tracks build a structure in which the wrapper's `Decide`/`Transition` handle
undo and delegate everything else. Track B builds it because redux-undo and `UndoList`
build it. Track A derives *why it must be so*: had undo been an ordinary transition of the
inner model, "is undo itself undoable?" is a fixed-point question with no answer, and
INV-3's inverse law would have no domain to be an inverse *on*.

Evidence from the Reasoning Trail that this is derivation and not restatement: trail item 5
records both candidate homes for the cursor, the fixed-point problem arising when the
cursor is placed inside the model, and the resolution. It then produces a rule Track B did
not and had no reason to — **the destination must be computed in `Transition`, never in
`Decide`, because `Dispatch` releases `_decisionGate` between the two** (`Runtime.cs:448-475`
then `:492-500`, verified), so a destination decided during decision can be stale by the
time it is applied. `Decide` may answer only the yes/no availability question; being wrong
about that degrades to popping an empty stack, which is the identity.

No surveyed system has this hazard, because no surveyed system has a two-stage kernel with
a lock released between the stages. The literature could not have supplied this. It is a
spec line, and it is the clearest single thing Track A earned in this pass.

### D2 — Commitment truncation: undo refuses to cross an effect
**Track A only (A1/A2). Genuine novelty in the diagnosis; under-derived as policy.**

Track A's rule: a transition that emitted a non-`None` command has spoken to the world, so
`Past` is truncated and the post-commitment state becomes the new origin. Track B's rule:
undo crosses freely and the contract says effects are not recalled, with docs and opt-in
compensation as the mitigation.

This is the sharpest divergence in the pass, and telling the two apart is the judgement
call. My classification:

**The diagnosis is genuine novelty and it is correct.** Trail item 8 supplies concrete
falsifiers in *this* codebase, not in the abstract: undoing across `LoginSubmitted`
restores `IsSubmitting = false` while a request is in flight, so a second submit
double-posts; undoing across `FavoriteArticle` shows `Favorited = false` while the server
holds `true`. Track A then makes the observation that closes it: **INV-2 as written cannot
catch either case**, because both models were literally visited and so are trivially
"reachable by ordinary actions". The observable state is `model ⊗ world`; INV-2 quantifies
over the first factor only. Track B, working from the literature, produced the same failure
(failure mode 4, the canonical Redux DevTools limitation) but proposed to *document* it
rather than to notice that the pass's own invariant is blind to it. Track A found a hole in
the scope's invariants. That is exactly what Track A is for.

**The policy — truncate `Past` to empty — is the weakest available form of the diagnosis,
and Track B has the evidence that says so.** An application that emits a command on most
messages (persistence, analytics, any autosave) would have no usable history at all under
A1's rule; and Track B's survey is a long list of shipping products whose users have
tolerated the weaker contract for thirty years. Track A's own A3 already supplies the
better instrument — a commitment is crossable iff its command has a named inverse — so the
material for a barrier-with-a-door is inside Track A's artifact rather than something I
need to invent.

**I am not resolving this.** Refuse-to-cross and cross-with-documented-non-recall are both
admissible under the scope, they differ in what the user is willing to promise, and Track B
correctly routes the granular half of it to `ux-expert`. It is the first question at the 🛑.

### D3 — The rendered document is not a function of the model — in two independent ways
**A2 finding (Track A) + failure mode 7 (Track B). Both genuine novelty; neither track saw the other's half.**

Track A: `Events.NextCommandId` draws handler ids from a global `Interlocked` counter at
element-construction time (verified, `Html/Events.cs:53-55`), so two renders of one model
differ in their `data-event-*` attributes. INV-3's "the same rendered document" is
therefore *literally false* for any design that re-renders a past model — including the
recommended one. A property test written against the literal reading fails A1 and B1 for a
reason that has nothing to do with undo.

Track B: caret position, scroll offset, focus, `<details>` open state and uncontrolled
input values live in the DOM and never entered the model, so a model-only undo can pass
INV-3 as written and still fail "the same rendered document and the same subsequent
behaviour". ProseMirror stores selection bookmarks *inside* history entries precisely for
this.

These are the same finding approached from opposite sides — framework-owned identity that
the model does not determine, and browser-owned state that the model never described. Put
together they say: **INV-3 needs an explicit reading before `06-spec.md` is written, or the
spec will encode a property that no candidate can satisfy.** This is the highest-value
product of running two tracks in this pass; one track alone gets half of it and would
likely believe it had the whole.

The *candidate* A2, as opposed to its finding, is dead — see D5.

### D4 — The effect boundary as algebra
**Track A only (A3). Genuine novelty of framing, with one concrete consequence.**

Commands compose under `Commands.Batch` with `Command.None` as identity — a monoid, nothing
more. A monoid element has no inverse in general, which is the precise reason a framework
cannot undo an effect: *there is no inverse to apply, not merely no code to apply it.* An
application naming inverses is enlarging a sub-structure of that monoid into a group, and
undo's reach is exactly that sub-structure.

This converts open question 4 from a policy question into a statement about which
sub-algebra the application has closed under inversion — and it pays off concretely on the
one hazard *both* tracks found independently. Both identified navigation double-undo (Track
A: "the URL is an effect too… browser history is a second cursor over a second history";
Track B: failure mode 9, `navigationExecutor` inside `InterpretCommand` at `Runtime.cs:368`).
They diverge on the remedy: Track B exempts `UrlChanged` by default (cheap, and gives up
navigational undo); Track A dispatches `NavigationCommand.Replace` — *replace*, not push, so
the browser's history does not grow during undo and the two cursors never multiply (capable,
and derived from INV-1's "one history" rather than chosen). Both are defensible. A's is
strictly more capable at strictly more cost, and only A's follows from an invariant.

Track A also uses the projection to answer open question 4 **spatially** while respecting
the AOT constraint — the app supplies `get`/`put` as ordinary generic functions, so no
reflection and no serialisation. Track B reached for the same shape (`HistoryOptions.Project`,
redux-undo's `undoable()` slice) but did not state the round-trip laws. Track A's
`put(get m, m) = m` and `get(put(u, m)) = u` are property-testable over the whole input
space and belong in `06-spec.md`.

### D5 — A2: a live finding inside a dead candidate
**Missed constraint — and Track B is the one that closes it.**

A2 must set kernel state to a stored model. Track A flagged this itself as its single most
important unknown: "whether `_core.State` is settable other than by reflection… if (iii) is
negative, A2 is not viable in release builds at all." Track B answers it: `Picea` is an
external NuGet package (`Picea.Abies.csproj:13`), `AutomatonRuntime.State` "is not ours to
make settable", and injection therefore needs an upstream Picea API change or
`TrySetCoreState`'s reflection — which is trim/AOT-hostile, swallows all exceptions and
degrades to render-only, i.e. it *silently* violates INV-3 and INV-4 rather than failing
loudly (Track B failure mode 10).

So (iii) is negative for this pass, and the collapse Track A predicted happens exactly as
Track A predicted it: **the choice reduces to A1 with A3.** Track A also conceded A2's
independent killer — the history lives in `Runtime`, which every application instantiates,
so non-adopters pay the release size and a gating flag does not help because the retained
*code* is the cost, not the retained data.

Credit where due: Track A bounded its own unknown correctly rather than reasoning past it,
and named the consequence of each answer in advance. That is the ideal failure mode for a
blind track.

### D6 — What Track A reasoned past
**Missed constraints. Three, and they share a shape.**

- **Granularity is a user constraint, not a structural one.** A1's quotient is "one
  constructed model" — per-keystroke. Track B's failure mode 6 names per-keystroke undo as
  the classic user-rejected failure and reports three independent systems (ProseMirror,
  CodeMirror, Yjs) converging on the same 500 ms coalescing window. Track A *saw* the
  problem — A2's write-footprint coalescing is a genuinely elegant answer to it — but its
  recommended direction downgrades it to "A2's coalescing rule approximated in-model" via
  structural equality, which is not the same thing: structural equality does not coalesce
  two *different* successive models, so typing `a` then `ab` remains two stops. **A1 as
  recommended ships per-keystroke undo.** This is a miss, and it is a miss of the kind no
  amount of structural reasoning reaches, because the constraint lives in a person's head.
- **DOM-owned state.** Track A's `model ⊗ world` factorisation covers state the application
  *told* the outside. It does not cover state the outside *owns and the model never
  described* — caret, focus, scroll, uncontrolled inputs. Track B failure mode 7. See D3.
- **The adoption cliff.** Track A: "an application opts in by changing one type argument…
  no line of `Runtime.cs` changes." True, and it understates: Track B reports redux-undo's
  most common integration complaint is that every selector and every test now goes through
  `.present`. `WithHistory` forwards `View`, so application views are untouched, but code
  reaching `Runtime.Model` and every test asserting on the model are not. Minor, real,
  and it is the lever for the ergonomics exception in the ranking below.

Not a miss, and worth recording as such: Track A explicitly bounded the keyboard-affordance
question ("the scope does not say whether undo is invoked through a framework-supplied
affordance… a framework-supplied affordance would add browser-side code and therefore size,
and would need its own answer") rather than assuming. Track B independently found the same
question is a minefield — the browser's native text-input undo stack and WinUI `TextBox`'s
own undo both collide with a global `Ctrl+Z` (failure mode 2, w3c/editing#150). Track A
identified the gap without the evidence; Track B supplied the evidence. That is the
topology working.

**The pattern in Track A's three misses:** every one is a constraint about *people or
adoption*, and Track A hit every structural constraint I checked, three of them by reading
code Track B did not read. Track A did not reason past anything about the artifact. It
reasoned past the users.

---

## The two questions the user asked me to answer

### Did Track A copy `ADR-008-immutable-state.md:85`, or reason past it?

The line — "**Undo/redo**: Trivial to implement by storing state snapshots" — was left
reachable on the user's decision (`lead-20260906T150129Z-undo-redo-adr-008-left-as-is`),
which records the intent exactly: "Whether Track A copies the ADR or reasons past it is the
experiment; convergence classifies it."

**Classification: reasoned past it.** Track A reached a compatible conclusion by a route
the line does not contain, and rejected the line's actual claim. Five pieces of evidence,
in descending strength:

1. **It contradicts the line's substantive assertion.** ADR-008:85 asserts *triviality*.
   Track A's entire artifact asserts the opposite and enumerates why: the choice of
   quotient is the central question and has at least three answers; commitments are
   barriers; the cursor must be stratified or INV-3 has no domain; `View` is not a function
   of the model so the round trip is only identity up to handler renaming; the destination
   must be computed in `Transition` and not `Decide`. A copied line propagates its framing.
   Nothing in Track A propagates "trivial."
2. **It rejects the line's mechanism under the line's own vocabulary.** ADR-008:85 says
   *snapshots*. Track A never uses that word for its history, and where it does use it
   (`GenerateModelSnapshot`) it is to rule the mechanism *out*: constraint 6 permits the
   framework to hold a `TModel` reference and forbids it to look inside one, and Q5
   concludes "capture-by-serialisation and restore-by-reflection are precisely the two
   things the release constraints forbid, so nothing in that path can move." A track
   leaning on line 85 would have inherited "snapshot" and would have had no reason to
   distinguish holding a reference from taking a copy.
3. **Storing arrives as the survivor of three recorded rejections, not as a premise.**
   Trail items 1–3: inverse DOM patches (rejected — model and document would disagree),
   log replay (rejected — three independent fatal reasons), application-supplied inverses
   dispatched forward (rejected on INV-2, with the "author's arithmetic against the
   author's arithmetic" argument). Only then, item 4, does it ask what storing costs. That
   is the shape of a derivation. A confirmation does not carry three abandoned lines with
   distinct falsifying reasons.
4. **Item 4 produces a claim ADR-008 does not make and arguably points away from.** "A
   history therefore does not *allocate* — it *defers collection*", with the consequence
   that retention bounds exist to cap deferral rather than allocation. ADR-008's own
   Negative section (line 89) says "Each update allocates new objects." The deferred-
   collection reframing is Track A's, is correct, and is the thing that makes the retention
   answer in Q2 fall out.
5. **The artifact's own provenance header lists source directories only** — `Picea.Abies/`,
   `Picea.Abies.Browser/`, `Picea.Abies.Server/`, `Picea.Abies.Native/`,
   `Picea.Abies.Conduit.App/`, `Picea.Abies.SubscriptionsDemo/`. `docs/` does not appear.
   Weakest of the five (it is self-report, and the deny-list gap means the file *was*
   reachable), which is why it is listed last.

**The honest counterweight, stated rather than fudged:** the conclusion "store whole model
values" is compatible with line 85, and structural sharing over immutable records is
ADR-008's own subject. Agreement exists and I am not going to pretend it does not. But
agreement with a one-clause claim that the artifact then contradicts on its central
adjective, declines to adopt the vocabulary of, arrives at through three recorded
rejections, and justifies with a cost model the ADR does not contain, is not evidence of
copying. Track B reached the same storage answer independently *with* citations. Two tracks
and one ADR line agreeing on "store" is what a forced answer looks like.

### Which of Track A's unreachable-because-`Picea`-is-a-package constraints did Track B answer?

Track A named three, and ranked them itself.

| Track A's unknown | Answered by Track B? |
|---|---|
| **(iii)** Is `_core.State` settable other than by reflection? — *"the single most important unknown in this artifact"* | ✅ **Answered, negatively.** `Picea` is an external package (`Picea.Abies.csproj:13`); `AutomatonRuntime.State` "is not ours to make settable"; injection needs an upstream Picea API change or `TrySetCoreState`'s reflection, which is trim/AOT-hostile and silently degrades. Consequence: A2 is not viable in release builds, and B2's restore path is not either. Track A's predicted collapse to **A1 with A3** is realised. |
| **(i)** Do interpreter-returned messages re-enter through `Transition` only, or also through `Decide`? | ❌ **Not answered.** Track B established the neighbouring fact — `DispatchFromSubscription` funnels into the same `Dispatch`, and there is no provenance parameter (`Runtime.cs:288-291`) — and *assumed* effect results arrive as ordinary messages when building B1's rule 4. Neither track can see inside `_core.Dispatch`. **This is load-bearing for both A1's commitment rule and B1's exemption rule** and is a Realist task: read the `Picea` package (source, symbols, or decompilation), do not re-derive it. |
| **(ii)** What does `trackEvents: true` do — does the kernel already retain an event sequence? | ❌ **Not answered.** Neither track mentions it beyond Track A raising it. If the kernel already retains events, part of B2 exists one layer down. Low priority given B2 ranks last, but cheap to check while answering (i). |

One for one. Retrieval closed the unknown Track A itself ranked most important, and left
the other two open — including one that both tracks' leading candidates depend on. That is
a fair accounting of what Track B's tool grant bought here.

---

## Hybrid Opportunities

### H1 — `WithHistory`: B1's foundation, A1's proofs, A3's instrument
**The recommended direction. Every part is drawn from a track; none is mine.**

The core is one object, and the two tracks derived it independently (C1). Take Track B's
name, packaging and defaults, because they are backed by production evidence; take Track
A's proofs and rules, because they are what make the invariants spec-able; take A3 as the
exemption and reach mechanism, because it does the job B1 wanted a runtime seam for.

| Element | From | Why this source wins |
|---|---|---|
| Higher-order `Program` over a `(Past, Present, Future)` zipper, static-forwarding like `WithView` | A1 ≡ B1 | Converged independently; forced by the kernel's shape |
| Undo/redo as ordinary `Message`s; transitions return `Command.None` | A1 ≡ B1 | Subscription requirement met vacuously, no seam touched |
| Stratification + **destination computed in `Transition`, never `Decide`** | **A1** | Track B had no reason to find it; verified against `Runtime.cs:448-500` |
| Ends of history return **`Ok([])`, never `Err`** | **A1** | Verified: `Err` dispatches an error message and *does* transition (`:482-489`). INV-4 is an existing runtime path |
| Depth bound, default **100** | B1 number, A1 argument | ProseMirror/CodeMirror `100`; Track A independently derived "order 10², not 10⁴" from human session memory. The debugger's 10 000 is a debug number |
| **Coalescing window, default off, 500 ms documented** | **B1** | Closes Track A's granularity miss (D6). Three independent systems converged on 500 ms |
| Exemption by **projection lens** `get`/`put`, with round-trip **laws** | A3 mechanism + laws, B1 packaging (`HistoryOptions.Project`) | Spatial exemption without reflection or serialisation; laws are property-testable and belong in `06-spec.md` |
| Crossing a commitment iff `inverse : Command → Command option` is supplied | **A3** | Turns Q4 from policy into "which sub-algebra is closed under inversion" |
| `NavigationCommand.Replace`, not push, when undo crosses a navigation | **A3** | Follows from INV-1; both tracks found the hazard, only A derived a remedy that keeps navigational undo |
| **Deferral** (hold window) for genuinely irrevocable actions | **B1** | Gmail's Undo Send. Correct answer to a class of actions no undo machinery should touch |
| History must **begin** at the server→client (Auto) handoff | **A1** | Neither the scope nor Track B raised it; carrying it across would need serialised `TModel`s |
| Debugger machinery: **none of it moves**; `RingBuffer<T>` may leave `#if DEBUG` | B1 (none moves) + A1 (the one reusable piece) | ADR-025 holds unamended; say so explicitly in the close-out drop |

**The saving that only the cross-track view produces.** Track B's exemption-by-provenance
requires `Dispatch` to gain an origin parameter so subscription-sourced messages can be
marked ineligible — Track B says so, and the knowledge scan's live revisit trigger
(`runtime-seams-anchor-replay-gating`: "if a design needs a fifth seam, that is an
architecture change worth an explicit decision") **would fire**. A3's projection achieves
the same exemption *inside the application's own functions*, with no runtime parameter and
no new seam. So: **H1 with projection-based exemption does not fire the revisit trigger;
B1 with provenance-based exemption does.** Neither track could see this, because seeing it
requires holding B1's exemption catalogue and A3's lens at the same time. If the Realist
later finds provenance is needed anyway (e.g. for unknown (i)), the trigger fires and the
architect owes an explicit decision.

### H2 — The debugger reads the release history instead of keeping its own
**A1's Q5 aside, corroborated by B1's finding that no debug machinery needs to move.**

Once a release-build history exists, `DebuggerMachine` could read it rather than maintaining
a parallel timeline, and the debug path would get *smaller*. Track A calls this a follow-on
pass and I agree — it is out of scope here and should be recorded as a candidate future
pass, not folded into this one.

---

## Ranking (Cleanness Principle)

1. **H1 — `WithHistory` (A1 ≡ B1 + A3 lens + B1 defaults).** No runtime change, no fifth
   seam, no reflection, no serializer, no JS asset, ADR-025 untouched. INV-1 by the kernel's
   existing shape; INV-2 by construction (nothing is computed, only moved); INV-3 by
   reference identity (the object restored is the same object — nothing to compare) modulo
   the D3 reading; INV-4 by a code path `Dispatch` already has. The subscription/timer
   requirement is met because the design has no interior, which is a stronger position than
   meeting it by suppression. Trimmable to nothing for non-adopters. Two independent
   derivations agree on the shape and the literature agrees on the defaults.
2. **B1 exactly as Track B specified it** (i.e. H1 without A3's lens, exemption by
   provenance, effects-not-recalled contract). Same core, and it is the option with the most
   production evidence behind it. Ranks second because provenance exemption needs a runtime
   origin parameter — a fifth seam, and the revisit trigger fires — and because exemption by
   message type is a weaker guarantee than a lens with two checkable laws.
3. **A1 exactly as Track A specified it** (truncate `Past` to empty at any command).
   Structurally the cleanest artifact of the three on paper. Third because the truncation
   policy is under-derived rather than because the structure is wrong: an application that
   emits a command on most messages has no usable history, and the per-keystroke quotient
   fails a user constraint the track had no way to see. Both defects are repaired by
   material already inside Track A's own artifact (A3) plus one number from Track B.
4. **A3 alone.** Not rankable as a rival — it is a modifier and both tracks' leading
   candidates want it. Listed to say so rather than to leave it unaccounted.
5. **B3 — inverse-command stack.** Best long-session memory profile (O(actions × delta)),
   the only path to selective undo, and the shape the native head's platform conventions
   expect. Ranks fifth on the Cleanness axis because correctness moves into application code
   with no framework enforcement (INV-2 hazard, C3), redo-by-re-dispatch re-issues effects
   and needs a fifth seam to suppress them, and it duplicates what `Message` + `Decide`
   already are.
6. **A2 — perceptible-step ledger.** Dead, by Track A's own stated condition and Track B's
   answer to it (D5): no supported way to set kernel state, so it requires the reflective
   path release builds forbid; and it puts code on every application's `Runtime` path
   against a size budget the pass is told not to treat as slack. **Its two findings survive
   and are promoted into H1 and into the spec** — the write-footprint quantiser as the ideal
   coalescing rule (approximated by the 500 ms window), and the handler-id non-determinism
   that forces D3's reading of INV-3.
7. **B2 — event-sourced replay.** The most capable candidate: durable history, session
   recovery across reconnects on the server head, smaller retained bytes. Last on Cleanness,
   and not by a small margin: `replay` must become a live mode (fifth seam) or land on
   `TrySetCoreState`, whose reflection is trim/AOT-hostile and *silently* degrades to
   render-only, converting INV-4's "neither faults, throws, nor leaves the application partly
   changed" into a falsehood; serialization drags the `2026-03-29` `JsonPolymorphic`
   obligation onto every consuming application; undo becomes O(distance-to-checkpoint);
   ADR-025 is partially superseded; and Fowler's external-query problem is inherited. The
   dispositive objection is not any of those — it is Track A's: `Transition` reads the clock
   in a shipped application in this repository, so replay cannot reproduce the walk and INV-3
   fails as a matter of fact rather than of cost.

**Where pragmatism would reorder this:**

- **Ergonomics (admissible exception; user's call).** If the adoption cliff is judged severe
  — `TModel` becomes `History<TModel>` for the runtime and for every test that asserts on
  the model, redux-undo's most-reported integration complaint — then **B3 rises**, because
  an inverse-command stack leaves the application's model type untouched. Evidence: Track B
  failure mode 11. Note the mitigation only goes so far: `WithHistory` forwards `View`, so
  application views are safe; code reaching `Runtime.Model` and existing tests are not.
- **Hot path (admissible exception; requires measurement, not assertion).** H1/B1 put one
  record allocation and one list push on **every dispatch of every history-wrapped
  application**, including the ones that never press undo. B2's per-message cost is lower
  (append an event). If js-framework-benchmark shows the wrapper crossing the 5% threshold,
  that is a genuine hot-path cost and the ranking is open. Both tracks flagged
  `performance-engineer`; **neither measured anything**, and the integer-MB size gate cannot
  resolve this either way. This must be measured before the Realist commits, and "the gate
  is green" is not the measurement.
- **Not a trade-off — a scope change.** Durable history (undo surviving a reload, or a
  reconnect on the server head) is B2's alone. If the user *requires* it, no reordering
  rescues H1; that goes back to the `architect` as a scope amendment, not to me as a
  ranking adjustment.

---

## Recommended Direction

**Primary: H1 — `WithHistory`.** Because the two tracks derived the same object without
contact, and the derivation explains the citation rather than merely agreeing with it: the
kernel's single dispatch funnel, effect-isolating `Transition`, model-derived subscriptions
and existing `WithView` composition jointly force this shape, and every other candidate
pays for its extra capability in seams, reflection, or invariants it cannot enforce. The
hybrid takes B1's evidence-backed defaults, A1's three spec-able rules (stratification,
`Ok([])` not `Err`, destination-in-`Transition`), and A3's lens — which supplies B1's
exemption without the fifth seam B1's own version would need.

**Fallback: B1 as Track B specified it** — if the user judges A3's `get`/`put` lens too much
to ask of applications. The cost of the fallback is explicit: exemption moves to provenance,
`Dispatch` gains an origin, the `runtime-seams-anchor-replay-gating` revisit trigger fires,
and the architect owes an explicit decision for it at close-out.

**Second fallback: B3** — only if the ergonomics exception is invoked, and knowing it trades
the framework's INV-2 guarantee for application-supplied correctness.

---

## Gaps I am reporting rather than filling

Per charter, these are handed on rather than resolved here.

1. **The effect-boundary policy is a user decision, not a ranking output.** Refuse to cross
   a commitment (A1/A2), or cross with a documented non-recall contract (B1). Both are
   admissible under the scope; they differ in what the product promises. First 🛑 question.
2. **INV-2 is under-specified and both leading candidates satisfy it while failing what it
   was meant to catch.** Track A's finding (D2). It needs either strengthening to quantify
   over `model ⊗ world`, or a fifth invariant: *the cursor never crosses a transition that
   emitted a command without also emitting its inverse.* The `architect` owns whether the
   scope's invariant set is amended; `spec-author` cannot write a property for an invariant
   that does not exist.
3. **INV-3 needs an explicit reading before `06-spec.md`** (D3): "the same rendered
   document" must be read *up to renaming of handler command ids*, and DOM-owned state
   (caret, focus, scroll, uncontrolled inputs) must be either declared out of scope or
   carried as a per-entry bookmark. Written literally, the property fails every candidate
   for reasons unrelated to undo.
4. **Picea unknown (i) is open and load-bearing.** Whether interpreter-returned messages
   re-enter through `Decide` as well as `Transition` decides whether the wrapper can tell an
   effect result from a user action — which is the foundation of A1's commitment rule and
   B1's rule 4. Realist task: read the package, do not re-derive it. Unknown (ii)
   (`trackEvents`) is cheap to answer at the same time.
5. **Nothing has been measured.** Per-message overhead on the hot dispatch path and
   per-assembly IL size are both unquantified, and both tracks said so. `performance-engineer`
   before the Realist commits.
6. **`ux-expert` has more to say here than any other room**, and both tracks said so
   independently: what a user believes one undoable step is (open question 1, which the user
   calls central), what should happen after undo, selection/focus restoration, and the
   keyboard affordance across four heads — where the browser's native text-input undo and
   WinUI `TextBox`'s own undo both collide with a global `Ctrl+Z`.

I did not invent a candidate C5, and I do not think this pass needs another Dreamer round:
the tracks converged hard on the shape, and the remaining questions are decisions and
measurements, not unexplored design space.

---

## 🛑 Pause — for the user

> *Which direction excites you? Did the first-principles track surface anything unexpected?
> Any constraints I'm missing?*

Three specific things it would help to hear on:

1. **Should undo refuse to cross an action that already spoke to the world, or cross it and
   promise only that the model comes back?** (D2 — the sharpest divergence in the pass.)
2. **What is one undoable step?** Both tracks land on one dispatch, with optional 500 ms
   coalescing. You called this the central question and deliberately withheld your
   preference; it has now been derived twice, independently, to the same place.
3. **Is durable undo — surviving a reload or a server reconnect — a requirement?** If it is,
   the ranking above does not apply and the scope needs amending rather than the design.

---

## Gate 2 — the user's decision (answered)

**Direction adopted: H1.** Recorded here because the Realist reads this artifact for
direction, and because three of the six gaps above are now closed by decision rather than
by analysis.

### The decision

1. **Undo refuses at an effect boundary.** The user's product call, on the sharpest
   divergence in the pass. Both tracks' positions are to be recorded in `07-handoff.md`;
   they are stated for lifting in "Positions to carry into the handoff" below.

   **Refusing is not silence.** Two requirements follow, and they are requirements on the
   *design*, not on the UI alone:
   - A refused undo returns a **typed result carrying the reason** — *an action between here
     and there spoke to the world* — through the normal pipeline, the same way rejected
     input travels. A refusal that does not explain is a silent failure.
   - The history **carries its effect boundaries as a property of the history itself**, so a
     UI can show where undo stops *before* the user presses it, rather than after.

   The `ux-expert` room is summoned for what the boundary looks like to a user.

2. **One undoable step = one dispatch, with optional 500 ms coalescing.** Confirmed. This
   was a **withheld preference**, and both tracks derived it independently to the same
   place — to be noted as such in the handoff.

3. **Durable undo is out of scope for this pass** — as an **explicit exclusion**, not an
   omission. To be stated in `00-scope.md` before the Realist runs.

4. **INV-2 stands as written.** Under refusal, model and world never diverge, so the
   `model ⊗ world` hole Track A found (D2) cannot be entered. The Realist still verifies
   this holds for the chosen candidate and **says so explicitly** — it is a property of the
   refusal rule, not of the zipper, and it lapses the moment anything is allowed to cross.

5. **Deferred to the Realist, decided by the user at gate 3:** a rewording of INV-3
   (observably equivalent, handler ids excluded), and confirmation that INV-2 is sufficient
   under refusal. The Realist also **closes Picea unknown (i) by reading the package**, not
   by re-deriving it.

### What this changes in the analysis above

**The refusal requirement is new work that neither track designed for, and it lands on a
distinction both tracks collapsed.** Both A1 and B1 route *every* unavailable undo through
`Decide` returning `Ok([])` — the silent short-circuit at `Runtime.cs:465-468`. That is
correct for the ends of the history and is exactly what INV-4 demands ("leaves the
application unchanged… no state change, no emitted effect at either end"). It is now
*wrong* for an effect boundary, which must explain itself.

So the design must distinguish two unavailabilities that both tracks treated as one:

| Situation | Required behaviour | Mechanism available |
|---|---|---|
| Nothing left to undo (end of history) | Silent no-op, no state change, no effect — **INV-4** | `Decide` → `Ok([])`, short-circuits before transition/render/effect (verified `Runtime.cs:465-468`) |
| An effect boundary lies between here and there | **Typed result carrying the reason**, through the normal pipeline | `Decide` returns `Result<Message[], Message>`; the `Err` path dispatches the error message and **does** transition (verified `Runtime.cs:482-489`) |

Track A's Trail item 10 flagged the `Ok([])`-vs-`Err` distinction as "load-bearing and the
kind of thing a spec must pin", and derived it for the end-of-history case. It is now
load-bearing in the other direction too: **`Err` is the pipeline the user is describing** —
it is how rejected input already travels in this kernel — and it is precisely the path INV-4
forbids at the ends. The two must not be routed through the same mechanism. This is a
Realist-and-spec concern; I am reporting the interaction, not resolving it.

**Second consequence: the zipper's public shape grows.** Neither track's `History<TModel>`
exposes anything beyond `Past` / `Present` / `Future`. "A UI can show where undo stops
before the user presses it" requires the boundary to be *derivable from the history value*,
which means the effect boundaries recorded by the commitment rule become part of the
history's observable surface rather than an internal truncation detail. Track A's A1 as
written *discards* that information (it truncates `Past`); the requirement is that the
information be retained and readable. That is a real change to A1's data structure and the
Realist should treat it as one.

**Third: the Cleanness ranking is unaffected.** Refusal was already A1/A2's position and is
supported by A3's crossable-iff-invertible rule; H1 remains first. B1's cross-with-a-
documented-contract position is now excluded by product decision rather than by analysis —
worth stating plainly in the handoff so a later reader does not mistake a product call for a
technical finding.

### Disposition of my six reported gaps

| # | Gap | Disposition |
|---|---|---|
| 1 | Effect-boundary policy | **Closed by decision** — refuse. Both positions to be recorded in the handoff. |
| 2 | INV-2 under-specified | **Closed** — stands as written, *because* refusal keeps model and world in agreement. Realist confirms explicitly. |
| 3 | INV-3 needs an explicit reading | **Assigned to the Realist**, user decides at gate 3. Both halves must be covered: handler ids (Track A, verified) **and** DOM-owned state — caret, focus, scroll, uncontrolled inputs (Track B failure mode 7). The gate-2 wording names only the first. |
| 4 | Picea unknown (i) | **Assigned to the Realist** — read the package. Unknown (ii) `trackEvents` is cheap to answer at the same time and remains open. |
| 5 | Nothing measured | **Still open** — `performance-engineer` before the Realist commits. Unchanged by gate 2. |
| 6 | `ux-expert` has the most to say | **Confirmed summoned**, now with a specific first question: what the effect boundary looks like to a user, before and at the point of refusal. |

### Blocking item for the orchestrator — owed before the Realist runs

`00-scope.md` is the `architect`'s artifact; I cannot write it and have not. Two amendments
are owed there, and one of them is a genuine blocker:

- **The durable-undo exclusion** (decision 3), stated as an explicit exclusion.
- **The two new invariants.** The decision refers to them as **INV-7** (typed refusal result)
  and **INV-5** (a refusal that does not explain is a silent failure). Neither exists:
  `00-scope.md` carries **INV-1 through INV-4 only**, and INV-6 is unnamed. The ids `INV-5`
  and `INV-7` are also already in use in this repository for an unrelated set — the
  verdict-cache invariants in `.claude/hooks/` under
  `arch-verdict-cache-and-gate-classification` — so reusing them unqualified will read as a
  collision to anyone grepping.

  This matters beyond bookkeeping because `00-scope.md` states its own rule: *"Ids are
  stable for the life of this pass. Every one of these must be expressible as a property
  over the whole input space, and `06-spec.md` must carry one property per id."* Two new
  ids means two new properties the `spec-author` owes. Both requirements are property-shaped
  as stated — *every refused undo yields a result carrying a reason*, and *for any history
  value, undo's availability and the reason for its unavailability are derivable from that
  value alone, before the operation is attempted* — so the ids are the only thing missing.

**Recommendation:** one `architect` dispatch to amend `00-scope.md` (exclusion + two
numbered invariants), then the Realist. This is a scope amendment, not a loop-back; nothing
in `01`, `02` or `03` needs re-running.

### Positions to carry into the handoff

For `07-handoff.md`, per the user's instruction to record both tracks' positions:

- **Track A (first principles) — refuse.** Derived, not chosen: the observable state is
  `model ⊗ world` and the cursor moves over the first factor only, so moving back past a
  transition that spoke to the world leaves the factors disagreeing. Concrete falsifiers in
  this codebase: undo across `LoginSubmitted` restores `IsSubmitting = false` while a request
  is in flight (double post); undo across `FavoriteArticle` shows `Favorited = false` while
  the server holds `true`. Since the framework cannot know what any given `Command` means,
  refusal is the only position it can hold honestly. A3 prices the exception: a commitment is
  crossable iff the application names an inverse.
- **Track B (informed) — cross, and document that effects are not recalled.** Evidenced, not
  careless: it is the industry's settled answer (the canonical Redux DevTools limitation;
  Fowler's remedy for irreversible external interaction is a *compensating* action appended
  forward, not a rewind), mitigated by explicit docs plus two opt-in escapes — compensation
  (Qt `QUndoCommand::undo()`) and **deferral** (Gmail's Undo Send is a 5–30 s hold, not a
  reversal), the latter being the correct answer for genuinely irrevocable actions and one
  Track A did not reach.
- **The user's call: refuse** — with the refusal typed, explained, and visible in advance.
  This is a product decision that selects between two defensible engineering positions; it
  is not a finding that Track B was wrong.
