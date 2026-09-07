---
name: a-clock-in-a-plan-is-a-mode-the-property-skips
description: Estimation calibration — when a plan uses a timer to infer a user's gesture boundary, the property will end up running with the timer disabled; delete the timer instead of tuning it
metadata:
  type: project
---

If a plan detects the boundary of a user gesture by **quiescence** (a settle window, a debounce, a
coalescing delay), expect two things at the next Critic pass: the window will be shorter than a real
person, and the property written to cover the invariant will run with the window disabled so it can
stay deterministic. Those two facts together mean the invariant has **no** coverage in the mode the
product ships.

**Why:** on the `undo-redo` pass, revision 2 ended an undo "run" after 250 ms of quiescence, borrowed
from ProseMirror/CodeMirror `newGroupDelay` — a number tuned for coalescing *keystrokes*, not for
bounding *movement*. Two undo presses 350 ms apart were two runs, which reconciled subscriptions
against the state in between and falsified INV-7 exactly as the first loop-back had. The planned
property ran with `SettleWindow = null` and an explicit terminal settle, so it could not see it. The
fix that worked was not a better number: it was **deleting the clock** and making the gesture
boundary explicit (`Hold` … `Settle` dispatched by the chrome). That single deletion closed one
blocker outright, made a second one (a stale-settle race needing an ordinal on the message) moot,
removed a whole file, removed a policy member, removed the framework's only self-declared
subscription, removed a wall-clock test the repo had already learned to hate (`ca2519d`), and made
the property's mode identical to the shipped mode.

**How to apply:** before costing a quiescence window, ask *who actually knows where the gesture
ends*. If some component already sees the closing input event (key-up, pointer-up, blur, drag-end),
make it say so and delete the timer. Two checks before committing to that: (1) verify the closing
event exists **on every head** — `Picea.Abies.Native/Events.cs` has only `OnClick`/`OnTextChanged`/
`OnToggled`/`OnValueChanged`/`OnSelectionChanged`/`OnScrollChanged`, no key-up, no pointer-up, no
blur, so browser-only bracketing is a head-coverage statement you owe the plan; (2) name what
happens when the bracket is opened and never closed, because deleting the timer also deletes the
thing that used to rescue it.

Related: [[vacuously-met-invariants-are-the-loop-back-tell]], [[higher-order-program-step-shape]].
