---
name: audit-the-residual-ledger-in-both-directions
description: A residual register can fail by over-registering as well as by laundering; check every status:open entry against the tree, not just the fixed ones.
metadata:
  type: feedback
---

When a round is asked to register its unfixed findings in
`.claude/enforcement/refutations.md`, check **every** entry against the working
tree — not only the ones marked `closed`.

**Why:** PR #358 round 2. Eighteen entries were registered, four closed. The
laundering check (is anything registered that should have been fixed?) came
back clean. The *opposite* check did not: seven of the fourteen `status: open`
entries stated a `level consequence:` in the present tense that the same
working tree falsified — the CI paths were present, the DENY entries were
added, the regex was fixed, the header was rewritten. `security-expert` wrote
the register before `devops` and `tech-writer` landed their fixes, and closed
only the four it happened to re-verify. The preamble disclosed the ordering
hazard, which made it honest but not accurate.

That matters more here than in an ordinary doc: `refutations.md` is
**append-only**, it is the instrument the *next* round grades against, and once
it is history nobody can distinguish "open because unfixed" from "open because
unverified at the moment of writing."

**How to apply:** for each `status: open` entry, run the one command its
`level consequence:` implies and record the result in the verdict's
reconciliation table. Ask for appended `status: closed — verified <what>` lines
(the practice the file's own closed entries already establish), never an
in-place edit. Related: [[doc-inventory-counts-go-stale-within-the-same-pr]],
[[a-disclaimer-does-not-fix-a-false-claim]].
