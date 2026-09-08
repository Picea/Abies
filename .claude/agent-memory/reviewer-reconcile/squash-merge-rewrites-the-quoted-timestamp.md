---
name: squash-merge-rewrites-the-quoted-timestamp
description: A document pinned to a squash-merge sha can faithfully quote a timestamp that belongs to the pre-squash branch commit — check every "the commit's own date" claim against git show -s on the pinned sha
metadata:
  type: feedback
---

When an artifact pins itself to a commit (`sourced from a single commit, <sha>`)
and then quotes a *prior* document's phrase "the commit itself is authored …",
the quote silently re-hosts: the prior document meant the branch commit it was
reviewing, the new document means the squash-merge sha. **Squash-merge assigns a
new author date**, so the number is faithfully copied and the attribution is
false.

**Why:** `presentation-demo` round 2. `README.md` offered
`2026-09-07T11:56:59+02:00` as "the commit's own author date … citable
separately if the slide needs a clock reading". That is `66379e7`, the pre-squash
branch commit `08-review-blind.md` was reviewing at `:105`. The pinned commit
`07607bf` is authored `13:00:00+02:00` — 63 minutes off, in a document whose
line 4 defines "the commit" as `07607bf`. Byte-exactness checks pass on this,
because the sentence *is* a faithful quote; only a metadata check catches it.

**How to apply:**

- On any bundle, design record or slide deck pinned to a sha, grep the index for
  claims about the **commit's own metadata** — author date, committer date,
  message, parent, line counts of files "at" that commit — and check each with
  `git show -s --format='%aI %cI %s' <sha>`. This is a category distinct from
  citation-resolution and from byte-exactness; a clean sweep of those two will
  not touch it.
- `git log --all --oneline -- <path>` reveals the pre-squash twin. If a quoted
  timestamp matches the twin and not the pin, that is this bug.
- The fix usually improves the artifact: naming both commits *is* the
  overwrite-in-place / history-rewriting story such records are often about.
- Same family as [[inherited-or-introduced-decides-the-verdict]] — the number is
  inherited, the attribution is introduced, and only the introduced half is a
  defect made here.
