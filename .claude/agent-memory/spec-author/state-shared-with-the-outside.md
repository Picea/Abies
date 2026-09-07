---
name: state-shared-with-the-outside
description: When part of the model is also held by something outside the application (browser URL, native window state), a reachability property cannot see the two copies diverge — that needs a second property comparing against the outside copy
metadata:
  type: project
---

When a model field is a *copy* of state something outside the application also holds — the browser's
location is the case that came up, and a native window's size or a server session's identity are the
same shape — a reachability-style invariant **cannot see the two copies diverge**. Write a second
property whose comparison has the outside copy on the other side.

**Why:** on the `undo-redo` pass INV-2 said *undo and redo land only on reachable states*, tested by
replaying the script's ordinary actions through the unwrapped program and asserting `h.Present` is in
that set. When the user found that incoming `UrlChanged` was unclassified — enveloped and recorded
like a user action, so undo restored a model with a stale URL while the browser still showed the new
one — my first instinct was to widen INV-2's alphabet. That would not have worked: a stale `Route`
restored by undo *is* a model the application was genuinely once in, so it is in the reachable set
and the property stays green while the browser stack and the application history drift apart. The
bug is only visible when the assertion compares the model against **what the browser shows**, which
is a different comparison, so it is a different property.

**How to apply:** when an `INV-n` gains "…and X is part of the world", ask whether the existing
property's *right-hand side* changes. If it does not, a wider alphabet buys nothing and a second
property is the honest answer — `validate-phase-artifact.sh` matches on the id and does not cap the
count, so two properties on one `INV-n` pass, but it departs from the skill's *exactly one per id*
and needs the user's say-so. Two things that made this one cheap and are worth repeating:
- **Model the outside copy in the test loop, not in the fixture.** The property tracked
  `browserShows` from the script it was already driving. No recorder member, no new seam.
- **Default the new model field.** `string Route = "/"` on the positional record meant not one of
  the nine acceptance tests' `new EditorModel(...)` literals had to change — which kept an
  amendment-before-the-lock from touching approved text.

One trap: comparing a refusal cause built fresh (`new UrlChanged(sameUrl)`) against the dispatched
one fails, because `Url` carries an `IReadOnlyDictionary` and record equality on it is reference
equality. Hold the dispatched instance and compare against that.

Related: [[invariant-alphabet-partition]], [[what-belongs-in-the-lock]]
