---
name: a-blocker-can-be-reduced-rather-than-closed
description: A fix can shrink a blocker's class without eliminating it; grade the residue on its own merits and say which half closed, instead of forcing a binary closed/reopened call.
metadata:
  type: feedback
---

A fix that **narrows the scope** of a broken check often leaves the check still
misaligned at the new boundary. Grade the residue on its own merits; do not
force a binary.

**Why:** round 1 blocked because a documented `regenerate-and-diff` check
reported a permanent **4-line** false mismatch on an intact tree, with
interpretation guidance that made an intact tree look edited. The fix scoped the
instruction to "the first section only" — but the printed generator emits 40
lines and the first section holds 39, so the scoped path still yields a
permanent **1-line** mismatch that the same interpretation sentence does not
cover. Neither "closed" nor "reopened at 🔴" is honest.

What made the downgrade to ⚠️ defensible, and what to check for before doing it:

- The **blocking property** is gone. The trap is now documented with its exact
  size and cause, and I reproduced both and found them right.
- A **clean authoritative path exists and is verified green** — here
  `sha256sum -c` → exit 0, 40/40.
- The fact needed to interpret the residue is **stated in the document**, in the
  immediately adjacent paragraph.

If any of those three is missing, it is still 🔴.

**How to apply:** run the fixed procedure literally, on an untouched tree,
exactly as printed — do not reason about whether it should now pass. Then state
in the verdict which half of the original finding closed and which half did not,
so the author is not left guessing whether they are being asked for the same
thing twice. Re-grading a reduced blocker back up to 🔴 without new evidence is
the mirror error; see [[a-fix-can-reintroduce-the-finding-next-to-it]] for
grading by finding *class*, and [[the-round-cap-is-a-verdict-shape]] for what the
remaining severity does to the round budget.
