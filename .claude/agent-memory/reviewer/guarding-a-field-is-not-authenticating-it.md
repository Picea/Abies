---
name: guarding-a-field-is-not-authenticating-it
description: A self-declared `agent:` field can never be un-forgeable; separate "parsing-differential impersonation" from "impersonation" or the same blocker reopens forever.
metadata:
  type: feedback
---

When a pipeline records an identity that the payload *declares about itself*,
guarding the parser does not make the identity trustworthy. Split the finding
in two before writing it up:

1. **Parsing-differential impersonation** — the machine reads a different
   identity than a human or `grep` reads from the same bytes. This is closeable
   and worth blocking on.
2. **Plain self-declaration** — the payload honestly says `agent: reviewer` at
   column 0. Not closeable at the parser layer at all.

**Why:** `.squad/decisions/inbox/*.md` drops carry `agent:` and `verdict:` as
plain front-matter, and the `SubagentStop` payload has no authorship channel —
only `stop_hook_active` and `cwd`. PR #355 spent three review rounds on the
nested-key promotion bug while an ordinary drop declaring
`agent: reviewer / verdict: PASS / scope: review / blockers: []` archived as
`[reviewer · PASS]` and wrote PASS to `.squad/.last-review-verdict` the whole
time. Verified at every round. Not naming that explicitly is what let the
narrower bug read like the whole problem.

**How to apply:**
- Treat `.squad/.last-review-verdict` and the `[agent · verdict]` heading as
  **advisory display state, not a trust boundary**, and say so in the review.
- Still block on the parsing differential — it is the part that defeats human
  audit, and it is cheap to close.
- Push back on in-code comments that say a class is "closed" when only one
  mechanism within it is. Scope the word to the fixture.
- If someone wants the identity to actually mean something, that is a design
  change (authorship binding at write time), not a validator patch — route it
  to the architect rather than accepting another parser fix.

Related: [[last-review-verdict-is-forgeable]],
[[line-splitting-fixes-must-start-at-the-open-call]].
