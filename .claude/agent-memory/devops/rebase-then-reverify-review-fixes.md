---
name: rebase-then-reverify-review-fixes
description: On a PR conflict-cleanup branch, rebase first and then grep for the exact previously-reviewed patterns before pushing
metadata:
  type: feedback
---

When cleaning up conflicts on a branch that has already been through review:
rebase first, then run targeted `rg` searches for the *exact* patterns a
reviewer previously commented on, and only then push.

**Why:** Adopted 2026-04-19. A conflict resolution can quietly reinstate the
pre-review version of a hunk — the merge is "successful", the diff looks
plausible, and a fix the reviewer already paid attention to is gone. Grepping
for the specific pattern proves the fix survived without re-reading the whole
branch, and without making unrelated edits that would expand the review surface
again.

**How to apply:** Keep a short list of the reviewer's concrete strings (a
deprecated call, a removed flag, a renamed symbol) from the review thread, and
run them as a checklist after every rebase. Resist the temptation to tidy
anything else while you are in there — the value of a conflict-cleanup branch is
that it is trivially reviewable.
