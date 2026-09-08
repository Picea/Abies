---
name: a-restated-mechanism-can-arrive-inverted
description: When an artifact restates a prior finding's mechanism, diff the restatement against the finding's own wording — a state qualifier flips silently, and both MSBuild controls in a scratch project settle which state is meant
metadata:
  type: feedback
---

An author fixing a review finding usually restates the finding's *mechanism* in
their own words. **Go back to the finding and compare the two sentences.** The
operative instruction survives; the justification clause is where a negation
gets added or dropped.

**Why:** `04-realist-plan.md:1508` came back as *"it is inert once the `Compile`
glob **no longer** claims the file"*. The finding it answered
(`undo-redo-pr0/09-review-verdict.md` ⚠️-14) said *"once the `Compile` glob
**claims** the file the SDK's default `None` glob excludes it"* — the opposite
state. In the state `:1508` names, `<None Include>` is the **only** thing
holding the file in any item group, which is exactly why PR 0's review asked for
it. The same document used "inert" correctly two sections later, for the
post-step-6 state, so the inversion was invisible to anyone reading only the
plan.

Settled in three scratch builds, not by argument:

| csproj shape | `Compile` | `None` |
|---|---|---|
| `Compile Remove` alone | 0 | **0** |
| `Compile Remove` + `None Include` | 0 | **1** |
| `None Include` alone (stale, post-removal) | default glob | 1, builds clean |

The mechanism worth remembering: the SDK's default `None` glob excludes
`@(Compile)` **as evaluated in the props**, before the project body's
`<Compile Remove>` runs — so a `.cs` file removed from `Compile` lands in
neither group unless something puts it back. That is one fact and it explains
both this plan clause and the Presentation project's `content\**` comment.

**How to apply:** whenever a fix's prose explains *why* something is safe, and
the explanation turns on a state ("once X", "after Y", "while Z"), name the
state and run **both** controls. A single control cannot distinguish "inert" from
"load-bearing"; it only confirms the state you already believed.

Related: [[a-negative-control-alone-proves-nothing]],
[[measure-each-guard-item-separately]],
[[landed-as-retro-dates-the-whole-sentence]].
