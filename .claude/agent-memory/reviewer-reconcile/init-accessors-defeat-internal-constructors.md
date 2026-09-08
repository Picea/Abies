---
name: init-accessors-defeat-internal-constructors
description: A "no public constructor" reflection test passes while `with { PublicInitProp = ... }` is still a public construction expression — grade an encapsulation claim on the accessors and the nested positional records, never on the constructor alone
metadata:
  type: feedback
---

When a plan resolves a *Make Illegal States Unrepresentable* gate with the phrase
"the type gets **internal constructors**", that sentence is not the check. On a
`public sealed record`, `with { … }` is a construction expression that goes
through the public `init` accessors and never touches the constructor — and a
nested `public sealed record Held(object Anchor)` has a public constructor
regardless of what the enclosing type's constructor says.

**Why:** undo-redo PR 1 (2026-09-08). `History<TModel>` had a genuine `internal`
5-arg constructor, a `Debug`-clean build, and a passing reflection test asserting
`GetConstructors(Public|Instance)` is empty — step 2's own Done-when (b). All of
that was true and none of it closed anything: `Present` and `Movement` were
`public { get; init; }`, so an assembly outside `InternalsVisibleTo` moved
`Present` while `Past` and `Future` stayed put, building exactly the state the
type's XML doc said could not exist. The plan's principles-gate resolution
("no deviation is requested") rested on the constructor sentence, so the *only*
evidence that the principle was enforced was the sentence itself.

**How to apply:** when a changeset's encapsulation claim rests on constructor
accessibility, enumerate the other four channels before grading — public `init`
accessors on a record, positional parameters on any nested public case, public
setters, and public factory methods. Then prove it: compile a probe from an
assembly *not* in `InternalsVisibleTo`, with a positive control (the tamper that
must fail to compile) and a negative one (an internal member that must already be
unreachable), per [[a-negative-control-alone-proves-nothing]].

Two Abies-specific traps to expect on that probe: `Picea.Abies` is
`[RequiresPreviewFeatures]`, so the probe csproj needs
`<EnablePreviewFeatures>true</EnablePreviewFeatures>` or every line fails CA2252
and looks like inaccessibility; and a project reference does *not* grant
internals when the assembly name differs, which is what makes the probe valid.

The fix is usually available inside the same PR — `public TModel Present { get; internal init; }`
is legal and keeps the chrome's read. Check whether the locked spec writes the
member before saying so: grep it for `with {`. In this case the spec had none, so
the blocker was closable without touching the lock.
