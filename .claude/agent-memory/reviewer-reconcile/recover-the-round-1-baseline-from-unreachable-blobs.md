---
name: recover-the-round-1-baseline-from-unreachable-blobs
description: When a re-review lands at the same HEAD with re-staged files, the previous round's content survives as unreachable git blobs — recover it instead of grading against your own prose.
metadata:
  type: feedback
---

On a re-review where the fixes are staged over the previous round **at the same
HEAD**, `git diff HEAD` gives the cumulative diff and cannot isolate what the
round actually changed. Do not fall back to grading against the quotes in your
own previous verdict. **Recover the previous round's index blobs**, which are
still in `.git/objects` as unreachable objects after the re-`git add`:

```
git fsck --unreachable --no-progress | awk '/blob/{print $3}'
```

then identify the right ones by size and by a marker string the previous round's
finding quoted, and **confirm each is neither the HEAD blob (`git rev-parse
HEAD:<path>`) nor the current index blob (`git ls-files -s <path>`)** before
using it. `git cat-file -p <sha> > baseline` and diff from there.

**Why:** this round's brief claimed seven fixes across two files. Against
`git diff HEAD` I could not have shown that *nothing else* changed inside those
two files, which was half of what was asked. The recovered blobs gave an exact
`+34/−20` and `+13/−2` and proved the plan delta was three hunks and only three.
Grading against my own round-1 quotes would have been circular — the previous
verdict is testimony about the tree, not the tree.

**How to apply:** any second or third review round on an uncommitted changeset.
Pair it with `find . -newer <previous-verdict>.md` to catch files that moved
*outside* the two you were told about — that sweep is what showed
`00-warden-scan.md` had a newer mtime but was byte-unchanged, and that everything
else newer was hook-written. Two independent methods, because mtime alone proves
nothing about content and content alone proves nothing about scope.

See [[verify-a-registration-pass-by-scope-not-substance]] for the `find -newer`
half, and [[arithmetic-adjudicates-a-missing-baseline]] for the case where no
baseline exists at all and the counts have to be made to reconcile instead.
