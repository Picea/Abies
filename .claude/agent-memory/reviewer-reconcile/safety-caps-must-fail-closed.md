---
name: safety-caps-must-fail-closed
description: A bounded scan whose cap-hit returns the permissive answer converts a DoS guard into an authentication bypass — check the cap boundary, and check which loop actually needed the cap.
metadata:
  type: feedback
---

When a validator adds a **safety cap** to a scan, the first question is what the
function returns when the cap is hit — not whether the cap is big enough.

**Why:** `scribe-decision-merger.sh` (PR #355, round 5).
`_fence_hidden_behind_junk_prefix()` walked a leading junk run to decide
"reject vs. fall through to the unvalidated LEGACY branch", capped at 256
characters, and returned `False` (= LEGACY) on cap-hit. So the cap *was* the
bypass: 256 junk bytes rejected, 257 sailed through. Reachable with one NUL
plus 300 ordinary spaces — no exotic prefix needed.

Two compounding ironies worth expecting again:
- The cap was on the **wrong loop**. The capped walk was already O(prefix) and
  terminated at the first visible character; the *uncapped* loop above it
  (`while ...: text = text[1:]`) was the genuinely quadratic one — 1.2 MB of
  leading BOMs measured at 1.7s, 4x per doubling.
- The comment called the cap "not load-bearing for correctness", which is the
  tell. A guard described as non-load-bearing is a guard nobody attacked.

**How to apply:**
- Probe cap-1, cap, cap+1 explicitly. Off-by-cap is the whole finding.
- Ask "what does hitting the cap *mean* here" — if it means the permissive
  branch, the cap is a fail-open. Either delete it (when the scan is already
  bounded by input the process has fully in memory) or make cap-hit reject.
- Time the *uncapped* loops in the same function before accepting a
  performance justification for the capped one.

Related: [[prefix-stripping-fixes-are-unbounded]],
[[a-disclaimer-does-not-fix-a-false-claim]].
