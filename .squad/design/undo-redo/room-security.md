# 🔒 Security Room — undo/redo (`WithHistory`)

Summoned by the orchestrator on the Critic's recommendation (`05-critic.md`, "Specialist Spawns
Recommended", first bullet), which the user accepted. Design-time input only — I do not edit
`04-realist-plan.md`, `00-scope.md`, or `docs/security/threat-model.md`. The Realist folds the
requirements below into the re-planned artifact; the threat-model rows I specify land at
implementation.

Read: `00-scope.md`, `04-realist-plan.md`, `05-critic.md` in full. Code read directly:
`Picea.Abies/Debugger/DebuggerMachine.cs`, `Picea.Abies/Debugger/DebuggerRuntimeBridge.cs`,
`Picea.Abies/Debugger/DebuggerAdapterProtocol.cs`, `Picea.Abies/Runtime.cs` (the `#if DEBUG`
capture/snapshot call sites), `Picea.Abies.Conduit.App/Messages.cs`,
`Picea.Abies.Conduit.App/ConduitDebuggerJsonContext.cs`, `docs/security/threat-model.md`.

---

## Verdict

**NEEDS-CHANGES at the plan level.** One requirement is release-reachable and must be met before
`WithHistory` ships to any adopter, not deferred as a "debug-experience consequence." A second is
DEBUG-only and lower severity, but the same mechanism closes both, so there is no reason to solve
only the release half. Neither requires reopening anything the Critic already blocked (B1–B4); the
fix composes cleanly with the redo/subscription mechanism still to be settled.

---

## What the retained history exposes

Two separate things get retained, in two different places, and they must not be conflated.

**1. `Step<TModel>.Cause` — a live `Message` reference, retained in process memory, in release
builds, by default.**

`04-realist-plan.md:184` types the step as `Step<TModel>(TModel Before, bool Committed, Message
Cause, long AtTicks)`. `DefaultHistoryPolicy` (`:415`, `IsUndoable => true`) makes every dispatched
message eligible for envelope-and-record by default (`:277`). `record` (`:287-290`) pushes `Cause
= inner` verbatim — the actual message object, not a summary of it.

This is new. Before this pass, a dispatched message is transient: it is consumed by `Decide` and
`Transition` and becomes garbage once the dispatch completes (`Runtime.cs:448-500`). Nothing in
the framework retains it. `WithHistory` changes that unconditionally for every adopting
application: a message is now kept alive, by the framework itself, for up to `TPolicy.Depth`
(default 100) subsequent dispatches — regardless of whether the message carries a form keystroke,
a page-size change, or a password.

`Picea.Abies.Conduit.App/Messages.cs:12,17,29` names the concrete instances that would flow
through this path the day Conduit (or any application shaped like it) adopts `WithHistory`:
`LoginPasswordChanged(string Value)`, `RegisterPasswordChanged(string Value)`,
`SettingsPasswordChanged(string Value)`. Each keystroke is its own message and, under
`DefaultHistoryPolicy`, its own retained `Step`. A user typing an eight-character password
produces up to eight live, distinct, in-memory copies of successively-longer password fragments,
each pinned by `Past` for up to 100 further dispatches — i.e., for as long as the user keeps using
the form, not just for the duration of typing.

This is not confined to the browser. `WithHistory` is head-agnostic by design
(`04-realist-plan.md:335-341`: "InteractiveServer, InteractiveWasm, Native — identical
implementation"). Under InteractiveServer the retained `Step.Cause` values live in the ASP.NET
Core server process, per user circuit — server-side memory, not the user's own device. A crash
dump, a memory-pressure diagnostic capture, or a container-level memory inspection on that server
now has a materially larger window of plaintext credential exposure than it did before this
feature existed, for every circuit that adopts `WithHistory` with the default policy. This is a
straightforward increase in blast radius on Trust Boundary considerations the current threat model
does not name (see below) and is unrelated to whether the export/debugger surface is reachable.

**2. The DEBUG-only debugger snapshot and export surface — amplifies (1) into a portable
artifact, but only in DEBUG builds.**

Confirmed by reading the code, not inferred from the plan:

- `Picea.Abies/Debugger/DebuggerMachine.cs:4` and `Picea.Abies/Debugger/DebuggerRuntimeBridge.cs:2`
  are both wrapped in `#if DEBUG` / `#endif` for their entire contents. `DebuggerAdapterProtocol.cs`
  likewise. This matches the scope's own framing ("excluded from release builds by conditional
  compilation") and ADR-025's release-strip clause, which `04-realist-plan.md:220-221` confirms is
  not crossed by this design. **The export/import surface (`ExportSession`/`ImportSession`,
  `export-session`/`import-session` bridge commands) does not exist in a release build. This is
  not release-reachable, and I have nothing to add to the plan's own conclusion on that point.**
- What the plan does add, and does not fully reckon with: `Runtime.cs:105,619-625` shows
  `GenerateModelSnapshot` serializes whatever `TModel` currently is via a supplied
  `JsonTypeInfo<TModel>`. For any application that adopts `WithHistory`, `TModel` **becomes**
  `History<TModel>` at the `Runtime` boundary — `04-realist-plan.md:232-234` says this correctly.
  `History<TModel>` carries `Step<TModel>.Cause : Message`, an interface, so a `JsonTypeInfo`
  capable of serializing it needs `[JsonPolymorphic]`/`[JsonDerivedType]` coverage of the
  application's **entire message hierarchy** — confirmed against
  `Picea.Abies.Conduit.App/ConduitDebuggerJsonContext.cs:10`, which today declares only
  `[JsonSerializable(typeof(Model))]` and nothing about `ConduitMessage`.
- The plan (`04-realist-plan.md:236-238`) calls the resulting obligation a "debug-experience
  consequence, not a release-path one" and stops there. That undersells it. The moment an
  application adds that polymorphic metadata — which it will need to do to get anything better
  than `ToString()` in the debugger's model-snapshot preview — `System.Text.Json` will happily
  serialize `Step.Cause`'s actual field values into the snapshot JSON, structurally, by property
  name. That snapshot feeds the live DEBUG debugger UI panel (`Picea.Abies.Browser/Debugger/
  DebuggerUI.cs`, `DebuggerAdapter.cs`) and, via `DebuggerMachine.ExportSession`
  (`DebuggerMachine.cs:289-318`) and `DebuggerRuntimeBridge`'s `export-session` command
  (`DebuggerRuntimeBridge.cs:54-56`), a JSON file a developer can download and hand to someone
  else — a bug report, a ticket, a Slack thread. That is a classic diagnostic-artifact secret leak,
  and it is a direct, mechanical consequence of a metadata obligation this pass introduces, even
  though the debugger code itself is untouched.
- **Pre-existing, not introduced by this pass, but the same shape:** `DebuggerMachine.
  SerializeMessageArgs` (`DebuggerMachine.cs:408-422`) already calls `message.ToString()` on every
  captured message today, with no `WithHistory` involved at all. C#'s compiler-generated record
  `ToString()` prints every property by name and value — `LoginPasswordChanged { Value = ... }` —
  so `TimestampedEntry.ArgsPreview` already carries plaintext credentials in the DEBUG debugger's
  10,000-entry ring buffer, and `ExportSession` already includes `ArgsPreview` verbatim
  (`DebuggerMachine.cs:311`). This is a real, currently-shipping gap. It is not this pass's to fix
  — the scope for this pass is undo/redo, and the debugger is declared out of reach
  (`04-realist-plan.md:209-224`, "total separation") — but it is the same class of bug this pass is
  about to reproduce structurally in `Step.Cause`, and the mechanism I am recommending below (a
  type-level sensitivity marker) is cheap to point at both. See "Recommendation, not a blocker"
  below.

---

## Trust boundary

None of the six boundaries in `docs/security/threat-model.md` describes this precisely, and I am
not going to force a fit. Boundary 1 ("Public client boundary — browser and external HTTP clients
crossing into API routes") is about traffic direction across the network edge; this is about
retention *within* a process the user or the server already trusts, for longer than the framework
previously retained anything. It applies to the InteractiveServer head's *server-side* memory as
much as to the browser's, so folding it into "the client" undersells the InteractiveServer case.

**I recommend a new boundary be added at implementation**, in the same style as boundaries 5 and 6
(which are also about retention/write-confinement inside an already-trusted process rather than
about a network perimeter):

> **7. Runtime state-retention boundary** — application state and dispatched messages crossing
> from a single, ephemeral dispatch (consumed and discarded once `Transition` returns) into an
> extended in-memory retention window (`Picea.Abies.History`, any bounded-history feature) or a
> developer-portable diagnostic artifact (`Picea.Abies.Debugger` snapshot/export), on any head
> including server-side circuits under InteractiveServer.

The Realist/architect should judge the exact wording; the content above is what the boundary needs
to say to be actionable.

---

## Reachability summary

| Path | Release-reachable? | Evidence |
|---|---|---|
| `Step.Cause` retention in `Past`/`Future` | **Yes.** Default-on, no build-config gate. | `04-realist-plan.md:184,277,287-290`; `WithHistory` carries no `#if DEBUG` anywhere in the plan's file list (`:411-419`). |
| DEBUG debugger snapshot serializing `History<TModel>` structurally | **No.** `#if DEBUG` only. | `DebuggerMachine.cs:4`, `DebuggerRuntimeBridge.cs:2`, `Runtime.cs:102-134,502-510,618-644` all `#if DEBUG`-gated. |
| `ExportSession`/`ImportSession` | **No.** `#if DEBUG` only. | `DebuggerMachine.cs:289,323` inside the file's single `#if DEBUG` block. |
| Existing `SerializeMessageArgs`/`ToString()` plaintext leak | **No.** `#if DEBUG` only; pre-existing, not introduced here. | `DebuggerMachine.cs:408-422`, same block. |

The severity ordering follows directly: the release-reachable item is the one that must be a plan
requirement, not a documentation note. The DEBUG-only items are real but bounded to developer/QA
time, and the mitigation for the release item also closes them for free if built the right way.

---

## What the plan must require

### Requirement 1 (blocking) — a type-level marker for sensitive messages, and a redaction step in `WithHistory`'s recording path

**Mechanism.** Add a marker interface to `Picea.Abies.History`, in the same idiom as `Message`/
`Command` already used throughout the codebase — no attribute, no reflection, trim/AOT-safe:

```
Picea.Abies.History.ISensitiveCause : Message
```

An application opts a message type into it: `public sealed record LoginPasswordChanged(string
Value) : ConduitMessage, ISensitiveCause;` — the application, not the framework, is the only party
that knows which of its own messages carry a secret, which is why this has to be an opt-in the
application declares rather than something the framework infers.

In `record` (`04-realist-plan.md:287-290`, step 6), before pushing `Cause = inner`: if `inner is
ISensitiveCause`, push `Cause = new HistoryRedacted(inner.GetType())` instead — a new `Message`-
typed sentinel in `Picea.Abies.History` that carries only the original type's name, never its
payload. **`Step<TModel>`'s shape does not change** (`Cause` stays `Message`), so this does not
reopen INV-5/INV-6, does not touch the routing table B4 fixes, and does not interact with B1/B2's
bare-path mechanics at all — redaction is orthogonal to undoability by construction. It also
directly answers `ux-expert`'s question 6 (`04-realist-plan.md:541-542`, whether `Step.Cause`
should surface as a label): a redacted step renders as "Undo (sensitive change)" or equivalent,
never the value — which is the answer from the security side of that question, independent of
whatever `ux-expert` decides for the non-sensitive case.

**Why not just `TPolicy.IsUndoable(Message) => false` for credential messages instead of a new
mechanism.** It looks like it should work and it is worth ruling out explicitly: `IsUndoable =>
false` routes the message through the **bare** path (`Decide`, `:277`, only envelopes when
`IsUndoable` is true), and `continue` (`:291-293`) still clears `Future` whenever the model
changed — which per B1/B2 it will, on every keystroke. Relying on `IsUndoable` as the redaction
mechanism means every credential field an application forgets to exempt is retained in full, *and*
every credential field it remembers to exempt loses redo the instant the user starts typing a
password, for reasons that have nothing to do with security and everything to do with B1/B2's
mechanics. The two concerns — "should this be undoable" and "is this payload safe to retain
verbatim" — are independent questions and the plan should not let the fix for one double as the
fix for the other.

**Where this attaches:**
- **Step 2** (`History.cs`) — no shape change needed if `HistoryRedacted` is introduced as a
  sibling type; flag it there so the type is designed alongside `Step<TModel>`, not bolted on
  after.
- **Step 4** (`HistoryMessage.cs`) — `HistoryRedacted(Type OriginalType) : Message` belongs beside
  `HistoryMessage`/`UndoRefused` in the same file, since it is a framework-authored `Message` value
  in the same sense `UndoRefused` is.
- **Step 5** (`HistoryPolicy.cs`) — `ISensitiveCause` is a marker, not a policy member, so nothing
  in the static-abstract interface changes; note in the same file's documentation that
  `DefaultHistoryPolicy`'s "everything undoable" default does **not** mean "everything retained
  verbatim" — the marker is checked unconditionally by `WithHistory`, not gated by the policy, so
  an application cannot silently ship a policy that forgets to redact.
- **Step 6** (`WithHistory.cs`) — the `record` function change described above. This is the one
  required code change; everything else here is documentation, tests, and the telemetry rule
  below.
- **Step 7** (invariant + property tests) — add one security regression property, named for the
  requirement rather than for an `INV-` id (it is not one of the scope's seven): *for any message
  type implementing `ISensitiveCause`, no generated history value's `Past`/`Future` contains that
  message's original payload anywhere reachable from `Step.Cause`* — reachable meaning by direct
  inspection, by `.ToString()`, and by whatever serializer the composition test (step 8) or the
  DEBUG snapshot path would apply to it. This is the regression test the skill's pattern calls for:
  it stays in the suite permanently and is named for the threat, e.g.
  `HistorySecurityRegressionTests.SensitiveCause_never_retains_original_payload`.
- **Step 9** (`HistoryTelemetry.cs`) — regardless of the marker, spans on undo/redo/clear/refusal
  must record `Cause.GetType().Name` and never `Cause` itself or any serialized form of it. This is
  a backstop for any message type an application forgot to mark — cheap, and it costs nothing
  against the "movement and refusal only" telemetry budget the plan already commits to
  (`04-realist-plan.md:469-471`).
- **Step 12** (docs/guide) — the guide must state the marker, show the Conduit password fields as
  the worked example (`LoginPasswordChanged`, `RegisterPasswordChanged`, `SettingsPasswordChanged`
  — named specifically, since they are the concrete case this room found), and say plainly that
  `DefaultHistoryPolicy` retains message payloads verbatim unless the application marks them.
  This is also where the corrected framing of the "debug-experience consequence"
  (`04-realist-plan.md:236-238`) belongs: state that the same marker is what makes adding
  `JsonPolymorphic` metadata for the debugger snapshot safe, and that without it, doing so is a
  security-relevant choice, not a cosmetic one.

### Requirement 2 (non-blocking, same mechanism) — the DEBUG snapshot/export path must inherit the redaction, not bypass it

Once `HistoryRedacted` exists, it is an ordinary `Message` value like any other, so
`GenerateModelSnapshot`'s serialization of `History<TModel>` and `DebuggerMachine.ExportSession`
both serialize the sentinel, not the original payload, automatically — **provided** the
application's `[JsonPolymorphic]` declaration for its message hierarchy includes
`HistoryRedacted` as one of the derived types it declares metadata for, same as any other message.
State this explicitly in the guide (step 12) as the reason the debugger obligation is safe, rather
than leaving the DEBUG consequence as an open question for whoever adopts `WithHistory` next.

### Recommendation, not a blocker — retrofit `SerializeMessageArgs`

`DebuggerMachine.SerializeMessageArgs`'s `ToString()`-based leak (evidence above) predates this
pass and is out of this pass's stated scope ("total separation" from the debugger,
`04-realist-plan.md:209`). I am not asking the Realist to fix it here. I am recommending a
follow-up: once `ISensitiveCause` exists, wiring `SerializeMessageArgs` to return
`"{\"redacted\":\"" + message.GetType().Name + "\"}"` for any `ISensitiveCause` message is a small,
low-risk change that closes a real, currently-shipping gap using infrastructure this pass builds
anyway. Route as a decision-log item for `csharp-dev`/`architect` to pick up as a fast-follow, or
fold into step 6 if the user prefers — either is fine; leaving it untouched with no ticket is not.
I will log it in `docs/security/hardening-backlog.md` at implementation regardless of which the
user picks.

---

## New threat-model rows needed at implementation

Not written by me; specified here for whoever updates `docs/security/threat-model.md` (per the
scope's Done-means item on stating memory behaviour, and per this project's standing rule that a
change altering the attack surface gets a threat-model update).

1. **New row, Threats And Mitigations table.** *Threat:* undo/redo history retains
   credential-bearing dispatched messages (`Step.Cause`) in process memory for up to
   `TPolicy.Depth` entries, across all heads including server-side InteractiveServer circuits,
   extending the temporal exposure window of plaintext secrets well beyond a single dispatch.
   *STRIDE:* Information Disclosure. *Entry point:* `Picea.Abies.History.WithHistory`, any
   adopting application's undo-enabled model. *Severity:* High (server-side case), Medium
   (client-only case) — the Realist/architect should pick one row or split it; I would split it,
   since the server-side blast radius (many concurrent circuits, one process) is qualitatively
   worse than a single user's own browser memory. *Mitigation:* `ISensitiveCause` marker +
   `WithHistory` redaction in `record` (Requirement 1 above). *Test:*
   `HistorySecurityRegressionTests.SensitiveCause_never_retains_original_payload` (step 7).
2. **New row, same table.** *Threat:* DEBUG-only debugger export (`ExportSession`) can surface
   plaintext credential values from `History<TModel>.Past[].Cause` once an application adds
   `JsonPolymorphic` metadata to its message hierarchy for debugger snapshot support, producing a
   human-portable JSON artifact containing secrets. *STRIDE:* Information Disclosure. *Entry
   point:* DEBUG-only, `Picea.Abies.Debugger` `export-session` bridge command. *Severity:* Medium
   (requires a DEBUG build, developer action, and voluntary export — not attacker-reachable
   remotely, but a realistic path to a secret landing in a bug report or ticket). *Mitigation:*
   Requirement 2 above — the redaction sentinel is what the JsonPolymorphic metadata actually
   serializes. *Status:* should land as ⚠️ Partially mitigated rather than ✅, since it depends on
   every future adopter remembering to mark its sensitive messages — the telemetry backstop (step
   9) does not cover this path, only the trace path.
3. **Open Risks entry**, if the retrofit to `SerializeMessageArgs` (pre-existing `ToString()` leak)
   is not folded into this pass: owner `security-expert`, target date at the Realist/architect's
   discretion, acceptance rationale "pre-existing gap, DEBUG-only, not introduced by this pass, but
   sharing a mitigation with a change landing now."
4. **New Trust Boundary entry (7)**, as drafted above, or equivalent wording the architect prefers.

---

## What I am not asking for

- I am not asking to reopen B1–B4. The redaction mechanism composes with whichever of the Critic's
  three named resolutions (lens, barrier, or the stated "no usable redo" trade-off) the user picks
  — it operates on `Cause`'s payload, not on whether a `Step` gets recorded or `Future` gets
  cleared.
- I am not asking for the marker to be mandatory-enforced by a Roslyn analyzer in this pass. A
  scanner rule that flags a message type with a `string` property named `Password`/`Secret`/`Token`
  that does not implement `ISensitiveCause` would be a reasonable follow-on for a `.semgrep/`
  rule once the marker exists, and I will propose it once `csharp-dev` lands the type — not before,
  since there is nothing to point the rule at yet.
- I am not asking for the DEBUG export path to be removed, restricted further, or gated behind
  anything beyond what ADR-025 already does. It is not release-reachable and the plan's "total
  separation" from the debugger is correct as stated; my finding is entirely about what happens
  *inside* that already-correct boundary once this pass's new type starts flowing through it.

---

## Summary for the Realist

One required code change (the `ISensitiveCause` marker + redaction in `WithHistory.record`, steps
2/4/6), one required test (step 7, security regression), one required telemetry rule (step 9,
type-name-only), and documentation (step 12, name the Conduit fields explicitly). One
recommendation to route as a decision, not a requirement (the `SerializeMessageArgs` retrofit).
Four threat-model items for whoever updates the document at implementation. Nothing here reopens
the Critic's four blockers or changes the H1 direction.

---

## Follow-up — 2026-09-07, re-spawned by the Critic's second pass (B7)

Scope of this section only: the two questions in `05-critic.md` B7 / "Specialist Spawns
Recommended". I re-read `05-critic.md` B7 and its spawn paragraph, this file in full, and
`04-realist-plan.md` **revision 2** SEC-1 … SEC-7 (`:463-495`), the wrapper pseudocode
(`:504-592`, especially `record`, `barrier`, `StepBack`/`StepForward`, `redact`), and the lens laws
L1–L4 (`:109-150`). I did not re-read the rest of revision 2. Per the task that spawned this
section, I am not editing `04-realist-plan.md`, `00-scope.md`, `docs/security/threat-model.md`, or
the disposition ledger — the Realist folds this in.

The Critic found the gap correctly: I scoped `room-security.md`'s original property to "anywhere
reachable from `Step.Cause`" (`:208-210`) because I reasoned from the message side only and did not
check whether the same values arrive by `Step.Model`. They do, in the room's own worked example.
Answers below.

### Question 1 — does `Scrub` + L5 close B7, and should it be mandatory rather than defaulted to identity for any application with a credential field?

**`Scrub` + L5 closes the gap the Critic found — but the mitigation text as worded names too few
call sites, and I want the missing one on record before `csharp-dev` implements from it.**

The algebra is sound. `Restore(a, b) = put(get(a), b)` (`04-realist-plan.md:122`, the doc comment
on the erased lens). For **L5: `Restore(Scrub(a), b) = Restore(a, b)`** to hold for every `b`, and
given `put` is faithful in its first argument for a fixed `b` — which L1 (`Restore(m,m)=m`) and L2
(`SameUndoable(Restore(a,b),a)=true`) already require of any lawful lens — the law forces
`get(Scrub(a)) = get(a)`. That is exactly the safety property wanted: **`Scrub` is constrained by
its own law to touch only fields the projection does not see.** It composes with L1–L4 rather than
sitting beside them as a separate, hand-checked claim, and it is cheap to property-test alongside
them (three generated triples, same as L1–L4). I confirm the law as the Critic worded it.

**The call-site list is short by one, and it is not a small one.** The Critic's mitigation text
says `Scrub` is "applied to the model **before** it is stored in a `Step` (both `record` and
`barrier`)" (`05-critic.md:217`). Revision 2's wrapper has a third and fourth site that construct a
`Step` directly from a live model, outside `record`/`barrier` entirely:

```
StepBack(h)    = let s = Past.Peek() in
                 { Past = Past.Pop(),
                   Present = TPolicy.Restore(s.Model, h.Present),
                   Future = Future.Push(Step(h.Present, s.Cause, s.Barrier, s.AtTicks)) }
StepForward(h) = mirror image
```

(`04-realist-plan.md:586-590`.) Both push `h.Present` — the model as it stands **at the moment of
the undo/redo press** — into a fresh `Step` on the opposite stack. If a Conduit user has a partial
password typed into the Settings field and presses undo to back out an unrelated bio-field edit,
`h.Present` at that instant carries the live password value, and it lands in `Future`'s new `Step`
unscrubbed unless `StepBack`/`StepForward` also call `Scrub`, not only `record`/`barrier`. This is
the same shape of miss B7 itself is about — a mitigation stated for the two most obvious sites and
silent on two more that construct the same shape by a different path. **Requirement, not a
suggestion: `Scrub` must be applied to every model a `Step` is constructed from — `record`,
`barrier`, `StepBack`'s `Future` push, and `StepForward`'s `Past` push — and step 8's L5 property
must generate undo-then-redo sequences long enough to exercise `StepBack`/`StepForward`, not only
`record`/`barrier` in isolation, or the property can pass while this site is still open.**

**Mandatory vs. default-identity.** Default to identity, for the same reason `ISensitiveCause`
is opt-in rather than framework-inferred: the framework cannot know which of an application's model
fields are credentials without the application saying so, and a heuristic (field named `Password`,
`Secret`, `Token`) belongs in a scanner rule, not a type-system requirement that would either miss
non-obviously-named fields or false-positive on fields that only look sensitive. I am not asking
for a compile error tied to "any application with a credential field" — there is no reliable way to
detect that condition at compile time, the same argument the room already made for
`ISensitiveCause` (`room-security.md:295-299`) and the Critic did not ask to revisit. What changes
relative to the room's original framing is the **weight** the guide gives it: `Scrub` is
lower-cost than `ISensitiveCause` (one function per application vs. one marker per sensitive
message type), so SEC-5's guide update should show it in the *same* worked example as the
projection — not as an optional add-on paragraph after — and SEC-3's generator (see Question 2)
must include a model shape that would silently fail without it, so an application that skips
`Scrub` sees a failing regression test rather than a silent gap. I will also propose the same
follow-on the room already flagged for `ISensitiveCause` (`room-security.md:295-299`) once
`csharp-dev` lands the type: a `.semgrep/` rule flagging a model type with a `string` property named
`Password`/`Secret`/`Token`/`Ssn` that is retained in a `WithHistory`-wrapped model with no
overriding `Scrub`. Not this pass; nothing to point the rule at yet.

### Question 2 — how should SEC-3 be worded to cover `Step.Model` and `Step.Cause`, and be satisfiable?

`04-realist-plan.md:472`'s wording — *"no generated history value's `Past`/`Future` exposes the
original payload by direct inspection, by `ToString()`, or through the serializer"* — dropped my
own scoping qualifier ("reachable from `Step.Cause`") and, read as written, quantifies over the
whole `Step`. That is false for any test model that retains the value in `Model`, which per B7 is
the realistic case, not an edge case. **Restated, in two clauses that name what discharges each:**

> **SEC-3 (restated).** For any application under test, including at least one generated test-model
> shape carrying a field the application's own `Scrub` is defined to remove (a Conduit-Settings- or
> Conduit-Login-shaped model — a live `Password` field excluded from the undo projection is the
> canonical case, and the property's generator corpus must include it, not just a model with no
> sensitive field), no generated `History` value's `Past`/`Future` exposes:
> **(a)** the original payload of a message implementing `ISensitiveCause`, reachable via any
> `Step.Cause` — by direct inspection, by `.ToString()`, or through the serializer the composition
> test or DEBUG snapshot path would apply; **discharged by** SEC-1/SEC-2's `redact()`, applied at
> every message-recording site (`record`, `barrier`);
> **(b)** any field value the model's `Scrub` is defined to remove, reachable via any `Step.Model`
> — same three reachability modes; **discharged by** `Scrub`, applied at every `Step`-construction
> site (`record`, `barrier`, `StepBack`, `StepForward`) together with **L5**.
>
> Both clauses are required. A test suite that only proves (a) is a test of the mitigation the
> room originally scoped, not of the threat the room named — the "up to eight live, distinct,
> in-memory copies of successively-longer password fragments" scenario is a **(b)** finding; it was
> never a **(a)** finding, because the framework's own worked example folds passwords out of the
> projection precisely so their keystrokes are `transparent` and never reach `Cause` at all.

Test name: keep `HistorySecurityRegressionTests.SensitiveCause_never_retains_original_payload` for
clause (a) (it is named correctly for what it covers), and add
`HistorySecurityRegressionTests.Scrub_never_retains_original_payload` for clause (b), asserting
directly against a Conduit-Settings-shaped fixture with a live password, undo-then-redo through
`StepBack`/`StepForward`, and inspection of every retained `Step.Model` in both `Past` and `Future`
— this is the test that would have caught B7 mechanically rather than by re-reading the code.

**Is `ISensitiveCause` still needed, or subsumed by `Scrub`?** **Still needed — not subsumed, and
not subsuming.** They redact two different fields of `Step`, populated from two different sources,
and each has a failure mode the other does not cover:

- **`Scrub` does not protect `Step.Cause`.** A message that both triggers a `barrier` (because it
  issues a non-silent command) and carries a secret directly as a constructor argument — e.g. a
  `LoginSubmit(Email, Password)`-shaped message, rather than the per-keystroke
  `LoginPasswordChanged` the room's example used — is retained verbatim as `Cause` regardless of
  how well `Scrub` redacts `Model`, because `Cause` stores the message object, not a projection of
  the model. `Scrub` has no reach into it.
- **`ISensitiveCause` does not protect `Step.Model`.** This is B7 exactly: even a message
  hierarchy where *every* sensitive message type correctly implements `ISensitiveCause` still
  retains the live password in the **next** recordable `Step.Model`, because that `Step` is opened
  by an unrelated message (a tab switch, a bio-field edit) whose `Cause` was never sensitive to
  begin with. The threat is in what the model *was holding at the time*, not in what caused the
  step.
- They are not even triggered by the same policy question. `ISensitiveCause` is a per-message-type
  opt-in the application declares on its `Message` hierarchy; `Scrub` is a per-model function the
  application declares on its `HistoryPolicy`. An application can get one right and the other wrong
  independently, and B7 is the demonstration that it did, in the room's own reference application,
  under the room's own recommended instrument (the projection).

Keep both. Neither is a special case of the other, and the SEC-6 threat-model rows (still
`security-expert`'s to write at implementation, not touched by this note) should say so explicitly
rather than let `Scrub` read as a superset fix.

**One-line confirmation, since it was in the same spawn paragraph:** `HistoryRedacted(string
OriginalTypeName)` in place of the room's original `HistoryRedacted(Type OriginalType)`
(`04-realist-plan.md:478-484`) is confirmed. `System.Text.Json` has no safe, trim/AOT-friendly
default treatment for a `System.Type`-valued property, and the sentinel's only job is to survive
serialization at the DEBUG snapshot boundary without leaking assembly-qualified detail; a string
type name carries everything the trace and the UI label need. No objection.

### Net effect on the plan (for the Realist to fold in, not applied here)

- `HistoryPolicy<TModel>` gains `static abstract TModel Scrub(TModel model)`, default identity.
- `Scrub` is applied at four sites, not two: `record`, `barrier`, `StepBack`'s `Future.Push`, and
  `StepForward`'s `Past.Push`.
- **L5:** `Restore(Scrub(a), b) = Restore(a, b)`, property-tested alongside L1–L4.
- SEC-3 splits into clause (a) (`Step.Cause`, unchanged mitigation) and clause (b) (`Step.Model`,
  new), each with its own regression test, and the generator corpus for both must include a model
  shape with a live sensitive field excluded from the projection.
- SEC-5's guide update shows `Scrub` beside the projection in the same worked example, not as a
  follow-on paragraph.
- `ISensitiveCause` is retained, unmodified, as a required and independent mitigation — not
  superseded by `Scrub`.

---

## Follow-up — 2026-09-07 (II), re-spawned by the Critic's third pass (S19, S20)

Scope of this section only: the two questions in `05-critic.md`'s "Specialist Spawns Recommended"
first bullet, against **S19** (`:272-306`) and **S20** (`:308-334`). Re-read `05-critic.md` in
full for this pass (verdict, B8/B9, the disposition table, S18–S24, the gates section), this file
in full including the 2026-09-07 (I) follow-up, and `04-realist-plan.md` **revision 3**'s lens
section (`:436-475`), the SEC table and `Scrub` call-site discussion (`:477-562`), and the
Architecture Sketch (`:566-594`, `Movement Settled | Held(Anchor)`). I did not re-read B8/B9's own
subject matter (interleaving, the `Decide`-error/subscription classification) — outside this
spawn's two questions. Per the task, I am not editing `04-realist-plan.md`, `00-scope.md`,
`docs/security/threat-model.md`, or the disposition ledger.

### Question 1 (S19) — what must SEC-3(b) say so that `Scrub` and the projection cannot be overridden inconsistently?

**A rule stated in two places, discharged by a test — not a compile-time shape, and I checked
whether one was available before ruling it out.**

**Why no compile-time shape exists.** `Restore`, `SameUndoable` and `Scrub` are three independent
`static abstract` members on `HistoryPolicy<TModel>` (`04-realist-plan.md:441-444`). The only way
to make "override one, must override all" a compile-time fact is to fold them back into a single
structural obligation — A3's un-erased form, where a `Step` stores `TUndoable` and `get`/`put` are
one pair a type either has or does not. That is exactly the shape gate-3 decision 1 traded away to
keep `WithHistory` at four type parameters (`room-security.md`, this file, "the note the user is
owed" at `04-realist-plan.md:554-559`, restated by the Realist as a cost that "was not visible when
the decision was made"). Proposing a compile-time fix here means re-opening a settled gate-3
decision to guard a member the same decision introduced; I am not asking for that, and the Critic's
own framing of S19 as "three sentences, no mechanism" (`:298`) already points away from it. So: no
compile-time shape, by construction of the erasure this pass already accepted.

**The rule.** State it once, as a precondition on SEC-3(b) itself, not as a general remark about
`Scrub`:

> SEC-3(b)'s guarantee — no generated `History` value's `Past`/`Future` exposes, via any
> `Step.Model`, any field the model's `Scrub` is defined to remove — holds only when the policy's
> `Restore`/`SameUndoable`/`Scrub` triple is jointly lawful (L1–L6). A policy that overrides `Scrub`
> without overriding `Restore`/`SameUndoable` to match is not covered by this clause; under the
> whole-model policy specifically, L5 forces `Scrub` to the identity, and overriding it alone
> **inverts** the guarantee — undo now deletes the field SEC-3(b) exists to protect.

That sentence is the missing half of SEC-3(b)'s "discharged by `Scrub` at every `Step`-construction
site, together with L5" (`04-realist-plan.md:531-532`): today's wording reads as if `Scrub` alone
discharges the clause, and S19 shows that in the one shape the whole-model default ships with,
`Scrub` alone can only discharge it by being the identity. The clause was never wrong about what
`Scrub` does when it's lawful; it was silent about what happens when it isn't, and the guide is
about to tell every adopter to write exactly the unlawful form.

**The test that makes the rule real, not aspirational.** The Critic's proposed Done-when — "a
policy that overrides `Scrub` without overriding `Restore`/`SameUndoable` fails L5" — is exactly
right and belongs beside L1–L6, not beside SEC-3. It is a **lens-law** test, not a **security
regression** test: it proves a property of the algebra (an inconsistent triple is L5-unsatisfiable),
not that a specific payload leaked. Filing it as a security regression test would blur the
distinction the room drew in the first follow-up — `Scrub` and `SensitiveCause` protect different
fields for different reasons, and conflating a law violation with a payload-retention proof invites
the same "one mechanism, stated once, assumed to cover everything" failure this pass keeps finding.

- **Attaches to `HistoryLensLawTests.cs` (step 8, alongside (e) and (f))**: a deliberately
  inconsistent fixture policy — `WholeModelHistoryPolicy`'s `Restore`/`SameUndoable` left at
  default, `Scrub` overridden to remove a field — must fail the L5 property. Name it for what it
  proves, not for the threat: `HistoryLensLawTests.Scrub_overridden_alone_violates_L5`. This is the
  test a copy-pasting adopter would hit if the framework's own L1–L6 harness were reused against
  their policy — which step 5's docs (below) should say explicitly, since it is the only route by
  which the rule becomes self-enforcing outside this repository.
- **Attaches to step 5's Done-when (`HistoryPolicy.cs`)**: the file documentation states the rule
  above, verbatim or near it, beside the existing sentence about the whole-model policy retaining
  payloads unless marked (`04-realist-plan.md:900-902`). This is where an adopter reads the contract
  before writing a policy, not after breaking it.
- **Attaches to SEC-5 (step 13, the guide)**: the worked example already shows `Scrub` beside the
  projection (`04-realist-plan.md:488`, `540`); add the negative form directly under it — override
  `Scrub` alone and show the L5 test that catches it — so the trap and its detector are in the same
  reading, not the rule in one document and the test in another the adopter never runs.
- **Does not attach to `HistorySecurityRegressionTests.cs`.** SEC-3(b)'s own test
  (`Scrub_never_retains_original_payload`) stays exactly as scoped — a correct fixture, proving the
  payload doesn't leak when the triple is lawful. It should not also be asked to prove the triple
  *is* lawful; that is `HistoryLensLawTests.cs`'s job, and giving one test two jobs is how the next
  version of this exact gap gets missed.

This changes the answer the room gave in the 2026-09-07 (I) follow-up
(`room-security.md:372-379`) from "default to identity, weight it in the guide" to what the Critic
already named: **neither mandatory nor bare-default — `Scrub` is conditional on a non-identity
projection, and the guide must say so next to a test that fails when it isn't.**

### Question 2 (S20) — must `Movement.Held(Anchor)` be scrubbed, made unreachable from serialization, or both?

**Neither, this pass. Name it, bound it, and prove the boundary with a test — the same discipline
Question 1 needed, applied to a value `Scrub` cannot safely reach.**

**Why scrubbing the anchor is unsafe, not merely undesirable.** The anchor's only job is to answer
`TProgram.Subscriptions(anchor)` for the duration of a bracket (`04-realist-plan.md:83-136`), and
INV-7's whole argument is that this is the *unprojected* model — a source declared only by a
transit state must not start, which the room's own worked case (a Conduit user typing a password
mid-bracket) does not change: `Subscriptions` is derived from the whole model by design, and
`Scrub`bing the anchor before storing it can silently remove a field `Subscriptions` reads,
producing exactly the "declares a source neither the anchor nor the destination declares" failure
mode step 9's assertion 2 exists to catch (`04-realist-plan.md:955-962`). The Critic's own framing —
"`Subscriptions(Scrub(m))` may differ from `Subscriptions(m)`" — is not a hypothetical for this
design; it is INV-7's mechanism read backwards. I confirm the Critic's preference for (ii) over
(i), and for a stronger reason than "I prefer it": (i) is not merely unenforceable, it is
**actively wrong** for any application whose `Subscriptions` reads a field `Scrub` would remove,
which includes the room's own canonical fixture if a chrome ever keyed a subscription off session
state adjacent to the password field. Scrubbing the anchor is not on the table.

**Why "made unreachable from serialization" is not achievable this pass, not merely undesirable.**
The mechanism is missing, not merely unbuilt: `Movement`/`Held` are public types the chrome reads
directly for INV-6 (`04-realist-plan.md:159-160`), so `Anchor` is public API surface with no
distinguishing marker the way `Cause` gets `SensitiveCause`/`HistoryRedacted` — there is nothing for
a redaction step to check `is` against, because the anchor is a whole model, not a message, and
"redact the whole model" is `Scrub`, which the paragraph above rules out. Building a
serialization-time-only substitute (a converter that intercepts `Held<TModel>` during
`JsonSerializer` and emits a placeholder without touching the runtime value) is a real mechanism,
but it is a **new** one, and it is exactly the kind of code-level change ADR-025's "total
separation, nothing moves" (`04-realist-plan.md:807-813`) and the Critic's closing constraint on
this loop — "no new mechanism, and nothing outside this list" (`05-critic.md:493`) — both rule out
mid-loop. I am not asking for it here.

**What is available, and it is enough for this pass because the exposure is bounded differently
than `Step.Model`'s.** `Step.Model` is retained for up to `TPolicy.Depth` (100) subsequent
dispatches — the room's original finding. `Movement.Held(Anchor)` is retained for the life of one
bracket: a single press under the default chrome (none — brackets are opt-in), or until `Settle`/
`Clear` for a chrome that opts in. `04-realist-plan.md:149-153` already accepts, as a *named,
bounded* cost, that a chrome which never dispatches `Settle` pins this indefinitely — S24 shows the
realistic way that happens (a lost key-up). So the anchor's exposure window is not zero, and it is
not `Depth`-bounded either; it is bounded by the same "whoever dispatches `Hold` owns the matching
`Settle`" contract the plan already states as a chrome obligation, plus `Clear` as the one in-band
rescue the Critic named at S24 (`:412-413`). That is a real bound, it is already written down for
an unrelated reason, and it is the right bound to cite here rather than inventing a second one.

**Mitigation, split by what it closes:**

1. **Name it, twice, next to the value it's next to.** Trust Boundary 7's wording
   (`room-security.md:126-130`) gets one clause added: *"…or a bounded, unscrubbed live model
   retained in `Movement.Held(Anchor)` for the duration of a bracket."* Threat-model row 2 (the
   DEBUG snapshot/export row) gets a second bullet naming `Movement.Held(Anchor)` as a **distinct**
   reachable site from `Step.Model`/`Step.Cause` — distinct because it is reached through
   `History<TModel>.Movement`, not through `Past`/`Future`, and distinct because it is **not**, and
   cannot safely be, covered by `Scrub`. Keep the row's existing "⚠️ Partially mitigated" status; do
   not let the anchor's addition read as new information changing the severity — it is the same
   DEBUG-build, same-JsonPolymorphic-obligation, same voluntary-export chain the row already
   describes for `Step.Model`, one more site on it. **Attaches to step 16 (security-expert,
   `docs/security/threat-model.md`).**
2. **Do not widen SEC-3(b)'s reachability set to include the anchor.** The Critic's own suggested
   rewording — "any model retained by a `History` value... and `Movement.Held(Anchor)`"
   (`05-critic.md:326-327`) — would make clause (b) false the instant it's adopted, since the
   anchor is deliberately not scrubbed. Widening the clause to name a site it cannot discharge is
   the same shape of mistake SEC-3(b) already made once this pass (S20 itself: "aimed at one route
   to the same value"). Leave clause (b)'s wording exactly as revision 3 has it — scoped to
   `Step.Model` in `Past`/`Future` — and give the anchor its own, separate, honestly-scoped
   guarantee instead (next item).
3. **A third, separately named test — proving the boundary rather than asserting it in prose.**
   `HistorySecurityRegressionTests.cs` gains
   `Anchor_never_reaches_a_release_path_surface`: over a `Held` bracket with a live
   `SensitiveCause`-free-but-sensitive-fielded anchor (the same Conduit-Settings fixture SEC-3(b)
   already builds), assert the anchor's payload appears in **none** of: the rendered `View` output
   (only `h.Present` is ever passed to `TProgram.View`, `04-realist-plan.md:80`), any
   `HistoryTelemetry` span or tag (SEC-4's existing type-name-only discipline, extended explicitly
   to cover the fact that `Movement` itself is never tagged, not only `Cause`), and any
   `MovementAvailability`/`EdgeState` value returned by `Backward`/`Forward`. This does not prove
   the anchor is *safe* — it is not, under the DEBUG snapshot path, and the threat-model row says so
   — it proves the *only* leak this pass tolerates is the one named in row 2, and closes off a
   fourth or fifth route to the same value being found in a later pass the way `Step.Cause` →
   `Step.Model` → `Movement.Held` were found in this one. **Attaches to step 8, filed beside SEC-3
   (a)/(b) as a third, explicitly-not-(b) item — not folded into clause (b)'s wording.**
4. **The guide states the asymmetry, not just the boundary.** Step 13's guide, in the same
   Hold/Settle contract section that already states "whoever dispatches `Hold` owns the matching
   `Settle`" (`04-realist-plan.md:161-162`), adds one sentence: *unlike a recorded `Step`, the
   anchor held during a bracket is never scrubbed — closing the bracket promptly is also a
   data-minimisation practice, not only a liveness one.* This reuses the sentence the plan already
   needs for S24 rather than adding a new section. **Attaches to step 13 (tech-writer, SEC-5).**
5. **A fast-follow, logged the way SEC-7 is, not solved now.** If a future pass wants to close the
   DEBUG-snapshot exposure properly, the mechanism is a serialization-boundary substitution for
   `Held<TModel>` (option the Critic and I both declined to build mid-loop) — worth a
   `hardening-backlog.md` entry once `SEC-7`'s retrofit is picked up, since both are "known,
   DEBUG-only, bounded, not this pass's to fix" in the same register. I will log it alongside SEC-7
   at step 16, not before — there is nothing to point either entry at until `csharp-dev` lands the
   types.

**Net effect on the plan (for the Realist to fold in, not applied here):**

- No new `HistoryPolicy<TModel>` member, no change to `Scrub`'s signature or call sites.
- SEC-3(b)'s wording gains one precondition sentence (Question 1); its reachability set is
  **unchanged** — still `Step.Model` in `Past`/`Future` only (Question 2 answer 2, above).
- One new lens-law test (`HistoryLensLawTests.Scrub_overridden_alone_violates_L5`, step 8) and one
  new, separately-named security regression test
  (`HistorySecurityRegressionTests.Anchor_never_reaches_a_release_path_surface`, step 8) — neither
  replaces or widens an existing test.
- Step 5's file docs and SEC-5's guide (step 13) each gain the sentence pairing described above.
- Trust Boundary 7 and threat-model row 2 (step 16) each gain one clause naming
  `Movement.Held(Anchor)` as a second, distinct, `Scrub`-ineligible retention site — same severity,
  same status, not a new row.
- No mechanism-level change anywhere; both answers are documentation-plus-test, which is what the
  Critic's closing constraint on this loop asked for.

---

## Follow-up — 2026-09-07 (III), re-spawned by the Critic's fourth pass (S29)

Scope of this section only: the single question in `05-critic.md`'s fourth-pass spawn (`S29`,
`:366-375`), against the finding at `:215-247`. Re-read `05-critic.md` S29 and its spawn paragraph
in full, this file in full including both prior follow-ups, and `04-realist-plan.md` **revision
4** § B9(b) (`:1157-1174`, disposition-table row `:1749`). Per the task: wording only. I am not
proposing a mechanism, not reopening B9(b), and not widening SEC-3(b)'s reachability set unless the
answer below says so — it doesn't.

### What changed, restated once so the two wordings below are traceable to it

Revision 3: a superseded `Future` step was released at the next dispatched action. Revision 4,
option (b): it is preserved, and leaves `Future` only when `Future` next exceeds `Depth` and is
trimmed at the far end. Count-bounded (`2 × Depth`, re-derived and confirmed correct by the
Critic). **Not time-bounded or dispatch-bounded within a session** — a session in which the user
does not perform a further long run of undos retains it until the process ends. Under
`WholeModelHistoryPolicy`, `Scrub` is the identity (S19), so what is retained is the whole model,
canonical-fixture password included. This is a data-minimisation regression inside a guarantee that
still holds — SEC-3(b)'s reachability set was always `Step.Model` in `Past` **and** `Future`, so
decision (b) does not put anything outside the clause. It changes how long the thing already inside
the clause sits there.

### Trust Boundary 7 wording

TB7's own sentence (`:126-130`) never quantified a duration — it names the crossing, not the
window — so it is not false as written. What is false, because it was written before decision (b)
existed and before S20's anchor clause, is the room's *habit* of glossing "extended in-memory
retention window" as uniformly `Depth`-dispatch-bounded whenever the boundary gets cited elsewhere
in this file (`:589` is the instance the Critic named, and it is not the only place the shorthand
appears). The fix is not to touch the boundary's opening clause; it is to make the boundary's
enumeration of *what* crosses it name the three shapes that duration now takes, instead of one.
Folding S20's already-staged anchor clause and this pass's finding into a single enumeration, for
whoever applies it at step 16:

> **7. Runtime state-retention boundary** — application state and dispatched messages crossing from
> a single, ephemeral dispatch (consumed and discarded once `Transition` returns) into: an
> in-memory retention window bounded by `TPolicy.Depth` dispatches (`Picea.Abies.History.Past`, any
> bounded-history feature); the same window's superseded-but-preserved branch
> (`Picea.Abies.History.Future`, under a policy that retains rather than clears a divergence),
> bounded in count at `2 × Depth` but **not** in time — it persists for the remainder of the
> session unless a further long run of undos trims it; a bounded, unscrubbed live model retained in
> `Movement.Held(Anchor)` for the duration of a bracket; or a developer-portable diagnostic artifact
> (`Picea.Abies.Debugger` snapshot/export) — on any head including server-side circuits under
> InteractiveServer.

Three named shapes (`Past`, superseded `Future`, `Held(Anchor)`), each with the bound that actually
applies to it, plus the diagnostic-artifact crossing unchanged from the original. No shape's
severity or status changes by being named; naming them separately is what stops the next reader
from borrowing `Past`'s short bound for `Future`'s long one, which is exactly the error the room's
own prose made at `:589`.

### The threat-model row wording (row 2, the DEBUG row)

Row 2 (`:270-280`) already gained one bullet from S20 (the anchor, as a distinct reachable site).
It gains a second, for the same reason and at the same attachment point:

> Row 2, third bullet: *the same `ExportSession` artifact also captures `History<TModel>.Future`.
> Before revision 4, a superseded branch was cleared at the next dispatch, so the export's realistic
> window for catching it was the brief interval before that dispatch. Since revision 4's option
> (b), the branch is retained until `Future` next exceeds `Depth` — in a session with no further
> long undo run, that is the remainder of the session — so the export can capture it at any point
> from the divergence onward, not only in a narrow window immediately after. This is the same
> DEBUG-only, developer-action, voluntary-export threat the row already names, applied to a branch
> that now lives longer; it does not change the row's severity (Medium) or status (⚠️ Partially
> mitigated), and it does not add a new entry point.*

Both wordings **attach to step 16** (`security-expert`, `docs/security/threat-model.md` and TB7),
same as S20's. Neither needs a new test: `HistorySecurityRegressionTests.
Scrub_never_retains_original_payload` (SEC-3(b)'s existing test, `:420-423`) already exercises
`Past` and `Future`, and a test proves reachability, not duration — there is no assertion shape that
would distinguish "retained for `Depth` dispatches" from "retained for the session" other than the
count bound both wordings already state in prose. Nothing here asks for one.

### Does the lifetime change alter the earlier anchor answer or SEC-3(b)?

**SEC-3(b): no.** Its reachability set has been `Step.Model` in `Past` **and** `Future` since the
2026-09-07 (II) follow-up (`:653`, "unchanged — still `Step.Model` in `Past`/`Future` only") —
written after decision (b) was already on the table for this pass. Decision (b) lengthens how long
a value already inside that set stays there; it does not place any value outside it. Nothing to
widen, narrow, or re-word in the clause itself.

**The anchor answer (Q2, S20, `:555-647`): the conclusion does not change; one supporting sentence
does.** The conclusion — do not scrub the anchor, do not attempt serialization-time unreachability
this pass, name and bound it instead — never rested *on* the `Step.Model` comparison; it rested on
the anchor's own, independently-real bound (the `Hold`/`Settle` contract, `Clear` as the one
in-band rescue, `:592-598`). The comparison at `:589` ("`Step.Model` is retained for up to
`TPolicy.Depth` (100) subsequent dispatches — the room's original finding") was cited as
*illustration* that a real, already-written-down, non-`Scrub` bound is an acceptable shape of
argument — not as the premise the argument needed. That illustration is now false for `Future` and
must be restated, honestly, as: "`Step.Model` in `Past` remains `Depth`-dispatch-bounded;
`Step.Model` in a superseded `Future` branch is count-bounded (`2 × Depth`) but not dispatch- or
time-bounded within a session." Restating it this way does not weaken Q2's answer — if anything the
anchor's bracket-duration bound is now demonstrably *tighter* than `Future`'s session-lifetime
bound, which makes citing "a real bound, of a different shape" a *stronger* move than it was when
the room first made it, not a weaker one. No re-derivation of Q2 is needed; only the one sentence at
`:589` should not be repeated as written the next time this file or a downstream document quotes it.

### Net effect (for whoever applies this at step 16 — nothing here is applied to this file, `04-realist-plan.md`, `00-scope.md`, or `docs/security/threat-model.md`)

- Trust Boundary 7: one enumeration, naming `Past`, superseded `Future`, and `Held(Anchor)` each
  with their own bound, in place of the single undifferentiated "extended in-memory retention
  window" clause plus S20's already-staged anchor addition — folded into one sentence above.
- Threat-model row 2: a third bullet, alongside S20's anchor bullet, stating `Future`'s export
  window is now session-lifetime rather than next-dispatch-bounded. Same severity, same status, no
  new row.
- SEC-3(b): unchanged, no wording action needed.
- The anchor answer (Q2, S20): conclusion unchanged; the one illustrative sentence it quoted from
  this file's first-pass finding (`:589`) is superseded by the two-clause restatement above wherever
  it is next quoted.
- No mechanism, no new test, no reopening of B9(b) or any of B1–B4. Wording only, as asked.

### S33 — accepted: the full `Url`, query string and fragment, is a rendered-chrome exposure, not a retention one; step 16 and step 13 must say so

Answering S33's question (`05-critic.md:653-659`) as accepted-and-documented, per the user's
decision, and stating the exposure honestly rather than folding it into this thread's other rows:
`[R4-nav]`'s seal makes the *whole* `Url` — path, query string, and fragment, verbatim — the stored
`EdgeState` cause of every post-record navigation. `UrlChanged` is sealed and framework-owned, so no
adopter can mark it `SensitiveCause`, and `Backward`/`Forward` return that cause to the chrome on
every render the edge stays refused. This is not the retention exposure the rest of this thread
describes — it is not waiting in `Past`/`Future` for a DEBUG export or an anchor's bracket-duration
hold; it is displayed, on the ordinary running screen, to whoever is looking at it. A password-reset
or magic-link token riding in the query string is shown there in plain text.

Step 16's threat-model wording must give this its own bullet, separate from row 2's DEBUG-export
bullets, because the surface is the ordinary chrome rather than an opt-in diagnostic artifact: name
`UrlChanged`'s `Url` — query string and fragment specifically — as a value no adopter can redact,
state that it is rendered live rather than merely retained, and record the disposition as an
accepted, documented limitation rather than a framework-side rule, alongside the B10 asymmetry
already on the table (an application-named navigation message can be marked `SensitiveCause` and is
never sealed; `UrlChanged` is sealed and cannot be marked).

Step 13's guide must tell an adopter, in plain terms: do not carry a secret, session token, or
password-reset/magic-link token in the query string or fragment of any URL that reaches the
framework's `UrlChanged` — undo/redo will surface it back to the screen. A flow that must carry such
a token in the URL should dispatch its own navigation message rather than rely on `UrlChanged`, so
it can be marked `SensitiveCause`.

---

## Follow-up — 2026-09-08, prompted by `09-review-verdict.md` finding 5 (PR 1's `Movement.Held` object erasure)

Scope of this section only: whether `Held(object Anchor)` — shipped in place of the plan's
`Held(TModel Anchor)` because the locked `UndoRedoSpec.cs:399` asserts
`IsTypeOf<Movement.Held>()` non-generically — changes SEC-3(b), Trust Boundary 7's wording, the
S20 anchor conclusion, or step 8(m)'s `Anchor_never_reaches_a_release_path_surface`, and what
step 16 must add as a result. Per the task: wording only. Read `09-review-verdict.md` (findings 1,
2 and 5) and `Picea.Abies/History/History.cs` as it stands in the working tree (`Held` at `:85`,
its doc at `:63-69`) for this pass; did not re-read the rest of revision 4 or re-open any earlier
follow-up. I am not proposing a mechanism, not reopening B9(b), S19, or S20's recommendation
against scrubbing, and not widening SEC-3(b)'s reachability set.

### What the erasure is, precisely, and what the coming fix does and does not restore

`Held(TModel Anchor)` gave the anchor a compiler-enforced identity with the model: any value
constructible as `Anchor` was, by the type system, an instance of the exact `TModel` the bracket
belongs to. `Held(object Anchor)` — forced by the locked spec's non-generic
`IsTypeOf<Movement.Held>()` — removes that. Finding 2's probe demonstrates the consequence while
the constructor is public: `P2 Anchor runtime type: System.String`, a value the plan never
intended to be constructible. Making the constructor internal (this round, per finding 1's
criterion) closes exactly one of the two things the typed constructor used to guarantee: it
restores "no assembly outside `InternalsVisibleTo` can construct a wrong-typed anchor." It does
**not** restore the other: inside the assembly, nothing stops a future internal call site from
writing `new Movement.Held(someWrongThing)`, because `object` accepts anything and the compiler has
no `TModel` to check it against. Before the erasure, that second guarantee was free — the type
system carried it. After, it is carried only by there being exactly one internal call site (`Hold`)
that constructs a `Held`, and by nobody changing that later without noticing. That is a weaker kind
of guarantee than the one SEC-3(b), TB7 and S20 were each drafted against, even though none of the
three names the anchor's *construction* as their subject.

### Does it change SEC-3(b)?

**No wording change — the reachability set was never conditioned on the anchor's type, and the
erasure does not open a route into it.** SEC-3(b) (the 2026-09-07 (II) restatement, held since) is
scoped to "any field value the model's `Scrub` is defined to remove, reachable via any
`Step.Model`" — `Past`/`Future` only. S20 answer 2 already excluded `Movement.Held(Anchor)` from
this clause on grounds that have nothing to do with typing: scrubbing the anchor is *actively
wrong* because `Subscriptions` needs the unprojected model (INV-7), not because the anchor was
typed `TModel` at the time. That argument is unaffected by which C# type spells "the model" at the
call site. Confirmed: no change to SEC-3(b)'s text or reachability set.

### Does it change Trust Boundary 7's wording?

**One clause should be added, not to describe a new exposure but to stop the boundary's existing
sentence over-promising.** The 2026-09-07 (III) wording says the boundary carries "a bounded,
unscrubbed **live model** retained in `Movement.Held(Anchor)` for the duration of a bracket." That
sentence is true of what the framework's own `Hold` call site actually stores, and remains true
after the erasure — but it is no longer true *by construction*. Under `Held(TModel Anchor)` a
reader could trust the sentence because the compiler enforced it; under `Held(object Anchor)`, even
with the constructor internal, the sentence is true only because exactly one internal call site is
disciplined to keep it so, and that discipline is not itself checked anywhere the boundary can
point to until step 8(m)'s test carries the weight (see below). TB7 should say this plainly,
appended to the existing `Held(Anchor)` clause:

> — held as `object`, not `TModel`, because the locked spec's non-generic
> `IsTypeOf<Movement.Held>()` forces `Movement` to stay non-generic (`09-review-verdict.md`
> finding 5); the "live model" property is enforced by internal-only construction and a single
> internal call site, not by the type system, and is regression-tested rather than compiler-checked
> (step 8(m)).

Same severity, same status as everything else in TB7 — this is a provenance note on an existing
clause, not a new threat.

### Does it change the S20 anchor conclusion?

**No — the conclusion holds, and for the same reasons it held before.** Do-not-scrub rests on
INV-7 (`Subscriptions` needs the unprojected model); do-not-build-a-serialization-substitute rests
on ADR-025's "total separation" and the Critic's "no new mechanism, and nothing outside this list"
constraint; the accepted bound is the `Hold`/`Settle` contract plus `Clear`. None of those three
arguments reads the anchor's declared type anywhere in its reasoning — they are about what the
anchor is *for* (answering `Subscriptions` for the bracket's duration) and about *how long* it is
held, not about what C# type spells its container. The erasure changes nothing the conclusion
depended on. It does sharpen why item 3 (the test) matters more than it looked like it did when S20
was written: with a typed anchor, a defect that stored the wrong value would have been a compile
error; with an erased one, it is a runtime `InvalidCastException` at best and a silent wrong-anchor
at worst, and the regression test is now the *only* thing standing where the compiler used to
stand.

### Does it change step 8(m)'s test?

**Yes — one assertion should be added; nothing already specified should be removed.**
`Anchor_never_reaches_a_release_path_surface` as scoped in the 2026-09-07 (II) follow-up proves the
anchor's *payload* does not reach View, telemetry, or `EdgeState`/`MovementAvailability` — a
confidentiality property. It says nothing about the anchor's *identity*: that what `Hold` stored is
in fact the same model instance (or an equal one) the bracket was opened against, rather than some
other object that happens not to leak through those three surfaces. Under `Held(TModel Anchor)`
that identity was compiler-guaranteed and needed no test. Under `Held(object Anchor)` it needs one,
because "an `object` anchor can hold something that is not the model" is now a compilable program,
foreclosed only by `Hold`'s own implementation. Add, to the same file, beside it rather than folded
into it:

> `Anchor_is_always_the_model_the_bracket_was_opened_against` — over a `Held` bracket opened by
> `Hold(m)`, assert `held.Anchor is TModel recovered && ReferenceEquals(recovered, m)` (or
> value-equality if the fixture's model is a record compared by value) — i.e., prove by test what
> the erased type no longer proves by construction.

This is a correctness/availability regression test (it guards against `InvalidCastException` and
silent misbinding), not a confidentiality one — it does not belong under SEC-3(a)/(b)'s naming
convention and should not adopt it, but it belongs beside
`Anchor_never_reaches_a_release_path_surface` in step 8 for the same reason the room has kept
adjacent-but-distinct tests apart throughout this file: one mechanism per proof, named for what it
proves.

### What step 16 must add

1. TB7's `Held(Anchor)` clause gains the provenance sentence above (erased type, internal-only
   construction, regression-tested rather than compiler-checked).
2. A new Open Risk, low severity, naming it explicitly rather than leaving it implicit: "a future
   internal change to `WithHistory` could construct `Movement.Held` with a value that is not the
   bracket's model, undetected by the compiler, caught only by
   `Anchor_is_always_the_model_the_bracket_was_opened_against` if that test is written and kept."
   Owner `csharp-dev`, mitigated by the test above rather than by a type-level fix, since
   re-introducing a typed anchor is foreclosed by the locked spec (finding 5's own conclusion,
   unchanged here).
3. No change to SEC-3(b)'s text, no change to the S20 conclusion's wording, no new row in the
   Threats And Mitigations table — the exposure surfaces (View, telemetry, `EdgeState`, DEBUG
   export) are unchanged by the erasure; only the *mechanism that keeps the anchor honest* moved
   from the type system to a test, and that move is what TB7's added clause and the new test above
   both record.

Net effect: no security-relevant exposure is introduced by the `object` erasure itself — findings
1/2's public-constructor problem was the exposure, and making the constructor internal closes it
per the Merge Criterion's own two-sided probe requirement. What the erasure removes is a
compile-time guarantee TB7, S20 and (implicitly) step 8(m) had all been quietly relying on without
naming it; this follow-up names it, points TB7 and step 8(m) at the one place that now has to carry
it, and leaves SEC-3(b) and the S20 conclusion exactly as they stood.
