---
name: shape-heuristics-are-defeated-by-one-line
description: A discriminator that infers intent from line shape has a one-line separator that resets it and a one-line separator that fails to — attack both directions before accepting the residual note.
metadata:
  type: feedback
---

When a validator can't decide structurally and falls back to **inferring intent
from shape** ("does this look like an abandoned frontmatter block?"), it will
always have two failure directions. Find one reproduction of each before you
read the residual paragraph, then check the paragraph against them.

**Why:** `scribe-decision-merger.sh` route 3 (PR #355, round 6).
`_unterminated_fence_looks_schema_shaped()` requires two distinct required-field
names inside one contiguous run of "mapping-block-shaped" lines; an *unindented,
colon-free* line ends the run and resets the count. That single predicate is the
whole surface:

- **Attacker direction** — insert a line that IS unindented and colon-free
  between each required field. A bare YAML block-sequence item (`- none`) is
  exactly that, *and is valid YAML*. A forged drop that PyYAML parses to a clean
  `{agent: reviewer, verdict: PASS, ...}` dict archives under `<!-- legacy -->`
  with every check skipped. The shipped comment reasoned only about padding
  *before* or *between* blocks, never about separators placed *inside* one.
- **Honest-author direction** — any prose line that happens to contain a colon
  does NOT end the run. "Standup at 09:30 covered the migration." keeps it alive.
  So do URLs on their own line, `Note:`/`Context:` sentences, colon-containing
  markdown headings, and every indented line (code blocks, blockquotes). The
  comment claimed the residual needed "adjacent labels with NO prose between
  them". Two genuine notes with real prose between the signals quarantined.

**How to apply:**
- Read the reset predicate first, then write the two minimal lines that satisfy
  and violate it. That is the whole attack; it takes one probe each.
- Do not prescribe another iteration. A shape heuristic cannot be made sound —
  three iterations were already discarded in-pass. Block on the *residual
  description* being wrong in both directions, and say explicitly that you are
  not asking for iteration N+1.
- Calibrate before blocking: here the bypass granted strictly LESS than the
  already-accepted self-declaration route (LEGACY never writes
  `.last-review-verdict`). That made it a documentation blocker, not a
  capability one — say which, or the author over-fixes.

**Reconstructed regression fixtures can be verified, not just trusted.** The one
hand-rebuilt pre-fix fixture in that suite was proven faithful by (a) `diff`
against current being exactly and only the fix, and (b) a sweep of all prior
rounds' exploits diverging on the target case *alone*. A sha256 pin guards future
drift; it attests nothing about history. Behavioural equivalence does.

Related: [[prefix-stripping-fixes-are-unbounded]],
[[safety-caps-must-fail-closed]], [[a-disclaimer-does-not-fix-a-false-claim]].

**Round 7 follow-up — pinning a residual in a test is asymmetric, and that is
correct.** After four rounds of blocking on comments that understated a residual,
devops added fixtures that assert the *documented residual is real* (QUARANTINED
on the current hook, ARCHIVED on the pre-fix one), separate from the
genuine-legacy controls that assert correct non-rejection. My read, which held up:

- It converts **narrowing** drift from silent to loud — if someone later tightens
  the discriminator, the fixture flips and the test fails, forcing the prose to be
  updated. Real ratchet against the exact pattern.
- It cannot catch **widening** — two fixtures pin a lower bound, not a universal.
  Add a sixth signal key and they still pass while the prose goes stale. Say so;
  don't let "it's tested now" read as "the claim is verified".
- **Do not pin the bypass the same way.** An assertion that "this forgery
  succeeds" penalises anyone who later closes it as a side effect. Pin the
  false-positive residual (nobody wants honest docs quarantined, so no perverse
  incentive); leave the false-negative in prose.
- Check the failure message's polarity: it must say "update the paragraph, not
  this test."

**Confirmation-pass technique worth reusing:** to verify "no logic edits", re-run
the whole accumulated exploit corpus and diff outcomes rather than reading the
diff. And when a fix edits a *pinned* fixture, tamper one byte and confirm the
integrity check still fails — recomputing a pin is exactly the operation that can
silently disable the guard.
