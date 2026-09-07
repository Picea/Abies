# spec-author memory

## Level choices, and how they aged
- [Level choice for framework internals](level-choice-framework-internals.md) — library types in `Picea.Abies` spec at workflow-direct through a real in-process `Runtime`; why not AppHost *and* why not pure functions

## Spec mechanics in this repo
- [Spec project layout in Abies](spec-project-layout-abies.md) — no `*.Specs` project, no `[Spec]` attribute; one locked file inside `Picea.Abies.Tests`, and why
- [Invariant alphabet partition](invariant-alphabet-partition.md) — when one property's alphabet falsifies another by correct behaviour, write the partition down; plus the falsifier-per-property habit
- [State shared with the outside](state-shared-with-the-outside.md) — a reachability property can't see a model's URL diverge from the browser's; that needs a second property, not a wider alphabet
- [Claims a property cannot carry](claims-a-property-cannot-carry.md) — when a rule keys on a type the *application* supplies, scope the claim as a stated adopter obligation and say so in three places

## Drafting judgement, corrected by the user
- [What belongs in the lock](what-belongs-in-the-lock.md) — I parked a stated behaviour in a plan step on fixture-cost grounds; the user moved it back and the fixture cost was tiny

## Re-approval events
- [Assertion shapes are not verified by me](assertion-shapes-are-not-verified-by-me.md) — `undo-redo` PR 0, 2026-09-07: `.Or` does not cross subjects, `IsEquivalentTo` ignores order, and three comments claimed coverage the code did not implement

Amendments **before** the lock took effect (not re-approvals, and much cheaper): `undo-redo`
2026-09-07, three times — A9 (see [[what-belongs-in-the-lock]]), INV-2's second property
(see [[state-shared-with-the-outside]]), and B10's scoping plus A10
(see [[claims-a-property-cannot-carry]]). The first two were the user finding a gap while still
holding the approver's pen; the third was the **Critic's confirmation pass** finding that the second
amendment's property could not see the shape the repository's own README hands an adopter. That
window is worth keeping open explicitly — say in the approval request which things are still free to
move, and list what you deliberately did **not** take with "say the word and it lands here".

**Amendment 4 was the first real re-opening** — from outside, on the PR-0 review's evidence, not
from the user finding a gap. Two lessons beyond the shapes themselves. (1) *"The attribute site is
inside the lock"* is a decisive argument: `[NotInParallel]` could not be added at step 6 without a
hand-back, so the user directed it in. Sweep every draft for attributes and declarations that can
only ever land before the lock. (2) Writing a missing coverage assertion **found a second defect** —
INV-6's `BlockedByWorld` was unreachable under `NoFeedback`, so its ten-combination claim could
never have been met. Unimplemented coverage claims conceal unreachable cases; implementing them is
how you find out.
