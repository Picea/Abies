---
name: state-the-finding-not-the-remedy
description: Calibration — twice in one changeset a user override produced a better fix than the one I prescribed; offering a menu of remedies is not the same as leaving the fix open
metadata:
  type: feedback
---

A suggested fix is a *hypothesis about the cheapest repair*, and it is formed
with less context than the author has. Say what is wrong and why it matters;
offer the remedy as one option, and treat a counter-proposal as evidence rather
than as resistance.

**Why:** `presentation-demo` ⚠️-4. I asked for in-file provenance markers on four
composite fragments — "four HTML comments" — and repeated the ask across two
rounds. The user overrode it, and the override was **right on a point I had
missed**: the bundle's invariant is that every `stops/` file is a byte-exact
excerpt, and one round *earlier I had introduced a `SHA256SUMS` manifest over
exactly those files*. My four HTML comments would have inserted authored bytes
into verbatim fragments and broken the manifest I asked for. The override's
answer — rename the four to a `.composite.md` suffix, content untouched — put the
seam marker in the filename, which travels with the file and adds zero bytes.
Cheaper, and it satisfied the finding better than my prescription.

The finding was real. The remedy was wrong, and it was wrong *because of a
control I had added myself* — the exact interaction a reviewer is worst placed to
see, since my own prior round is the part of the context I am least likely to
re-derive.

**How to apply:**

- Separate the two halves explicitly in the finding: what is wrong (binding) and
  what I would do about it (advisory). An author who reads the remedy as the
  finding cannot propose a better one.
- Before prescribing an edit to a file, check what the changeset's **own earlier
  rounds** added that constrains it. A manifest, a lint rule, a golden file or a
  byte-exactness invariant introduced by a previous round is precisely what a
  suggested edit will collide with.
- When an override arrives, verify its *claims* mechanically (here: all four
  renamed files still byte-exact against the pinned commit, manifest regenerated
  and self-consistent, no stale name anywhere) and then say plainly in the verdict
  that it beat the suggestion. Recording the override under Review Rule 6 means
  logging my concern **and** that the override answers it — not logging a defeat.
- This does not soften [[a-blocker-list-is-not-the-criterion]]. Enumerate the
  finding exhaustively; leave the *fix* open.

**It happened twice in the same changeset, which is why this is a rule and not an
anecdote.** The second time I was careful: I named *two* remedies for ⚠️-10 rather
than one, said the choice was the user's, and still the Lead found a third
(a scoped `.gitignore` negation) that beat both of mine — because it preserved a
constraint I did not hold, the filename the user's runbook refers to. Offering a
menu is not the same as leaving the fix open. The author's constraint set is
always larger than mine.

Related: [[the-round-cap-is-a-verdict-shape]] — the same round, and the reason a
newly-found item became ⚠️ handed to the user rather than a third 🔴. See
[[untracked-bundles-need-a-check-ignore-sweep]] for the ⚠️-10 finding itself.
