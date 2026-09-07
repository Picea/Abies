---
name: state-retention-features-need-sensitivity-marker
description: Any Abies framework feature that retains dispatched Message values beyond a single dispatch (undo/redo history, replay logs, time-travel debugging) needs an opt-in type-level marker so credential-bearing messages can be redacted before retention.
metadata:
  type: project
---

Abies's MVU kernel treats a dispatched `Message` as transient by default — consumed by `Decide`/
`Transition` and garbage once the dispatch completes (`Picea.Abies/Runtime.cs:448-500`). Any new
feature that changes this by retaining message values in memory for longer than one dispatch
(the `undo-redo` design pass's `WithHistory`/`Step<TModel>.Cause`, and by extension any future
replay-log or time-travel feature) reintroduces the same risk class: application message
hierarchies routinely carry credential fields as plain constructor arguments —
`Picea.Abies.Conduit.App/Messages.cs:12,17,29` (`LoginPasswordChanged`, `RegisterPasswordChanged`,
`SettingsPasswordChanged`) — and a framework that retains messages by default retains those
values by default too, in process memory, across every head including server-side
InteractiveServer circuits (not just the browser).

**Why:** found during the `undo-redo` design pass room
(`.squad/design/undo-redo/room-security.md`). `DefaultHistoryPolicy.IsUndoable => true` made
every dispatched message eligible for verbatim retention with no redaction concept at all — a
release-reachable gap, not a debug-only one. The plan's own author correctly identified the
downstream DEBUG-serialization consequence (needing `JsonPolymorphic` metadata across the message
hierarchy for the debugger snapshot) but classified it as a "debug-experience consequence, not a
release-path one" — which undersold the release-side retention risk that exists independently of
any debugger involvement.

**How to apply:** when reviewing or threat-modeling any feature that retains `Message` values
(history, undo/redo, replay, time-travel debugging, audit logging of dispatched messages), require
a type-level marker interface (e.g. `ISensitiveCause : Message`, mirrored after this codebase's
existing `Message`/`Command` marker-interface idiom — no attributes, no reflection, trim/AOT-safe)
that application authors opt sensitive message types into, and require the retaining mechanism to
substitute a redacted sentinel (type name only) before storing, not merely document the risk or
rely on an unrelated policy flag (e.g. `IsUndoable`) as a stand-in for redaction — those are
orthogonal concerns and conflating them produces side effects unrelated to security (in
`WithHistory`'s case, marking a message "not undoable" to avoid retention also silently discarded
the redo stack, an unrelated mechanism collision). Also require any telemetry/tracing on the
retained value to record only the type name, as a backstop for message types an application forgot
to mark. See [pr-blocking-security-minimum](pr-blocking-security-minimum.md) for the general
principle that a real information-disclosure finding on a release-reachable path blocks, not just
gets noted.

**Correction (Critic B7, second pass, 2026-09-07): a message-side marker alone is not sufficient
— it misses its own worked example.** I originally scoped the redaction property to "anywhere
reachable from `Step.Cause`" — the retained *message* — and the `WithHistory` design's own
recommended instrument (a projection lens that excludes the sensitive field from the undo
comparison) means a sensitive field's own keystroke messages become `transparent` and **never
reach `Cause` at all**. The value still leaks, through the *retained model snapshot*
(`Step.Model`/`Step.Before`) captured at the next unrelated recordable step (a tab switch, an
unrelated field edit) — the model at that instant still holds the live, un-redacted value in a
field the projection excludes but does not erase. **General rule: any state-retention feature that
snapshots a whole model (not just a message) needs a model-side scrub function, symmetric to and
independent of the message-side marker** — in this design, `HistoryPolicy<TModel>.Scrub(TModel) :
TModel`, applied at *every* site that constructs a retained snapshot (not just the obvious
"record" path — this design also had two easy-to-miss sites, the redo/undo-direction pushes that
snapshot the live "current" model onto the opposite stack). The two mechanisms are not
substitutes: a message can carry a secret directly (redacted only by the marker) and a snapshot can
carry a secret the projection excludes but a scrub function doesn't erase (redacted only by
`Scrub`). Require both, verify both with a lawful obligation the retaining mechanism must satisfy
(here, `Restore(Scrub(a), b) = Restore(a, b)` — scrubbing may only touch fields the projection does
not see) and a regression test built from a fixture that actually carries the sensitive field, not
one that happens not to. See `.squad/design/undo-redo/room-security.md`'s dated follow-up section
for the full worked derivation.
