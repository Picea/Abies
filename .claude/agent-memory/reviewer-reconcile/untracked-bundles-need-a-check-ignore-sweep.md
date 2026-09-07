---
name: untracked-bundles-need-a-check-ignore-sweep
description: A changeset made of untracked files can be byte-perfect and still ship incomplete — git add silently skips ignored paths, so run git check-ignore over every file and compare the git-visible count to find
metadata:
  type: feedback
---

When the changeset under review is a directory of **untracked** files, verifying
the files is not verifying the changeset. `git add` silently skips anything an
ignore rule matches — no error, no warning, no non-zero exit — so the tree you
proved correct and the tree that lands can differ by whole files.

**Why:** `presentation-demo`, rounds 1–3. I verified 19 byte-identical copies and
20 byte-exact fragments three separate times and missed, twice, that
`content/demo/timing/hooks-fired.log` is matched by the repo-wide `*.log` at
`.gitignore:120`. It was one of only **two** authored files in the bundle, indexed
by its own top-level README section, and the bundle's `SHA256SUMS` covered
`full/` and `stops/` only — so `timing/` had no manifest and *nothing in the
changeset would have detected the loss*. Worse, my round-1 metrics line asserted
"`.gitignore` has no `content`/`demo` rule", which is true and is the wrong
question: the rule that bites is repo-wide, not path-specific. Scoping an ignore
check to the changeset's own directory finds nothing by construction.

**How to apply:**

- Run it over **every** file, not the directory:
  `git check-ignore -v $(find <dir> -type f)` — exit 0 means at least one match,
  and the output names the rule and line that did it.
- Cross-check the counts, which catches rules `check-ignore` can't reach:
  `git status --porcelain --untracked-files=all <dir> | wc -l` against
  `find <dir> -type f | wc -l`. A gap is a file that will not be committed. Run
  `git status` from the **repo root** — a relative path resolves against the
  shell's cwd and silently returns 0 from inside the directory.
- Ask which extensions the repo ignores globally before praising the fragment
  naming. `.log`, `.tmp`, `.bak`, `bin/`, `obj/` are the usual ones, and a demo
  or fixture bundle is exactly the kind of change that names files after what
  they *depict* rather than what they *are*.
- Grade it against the manifest's coverage. If the changeset ships a checksum
  file, note which directories it does **not** cover — those are where a silent
  divergence survives, and an ignored file inside them is invisible twice.
- Resolutions are cheap and the choice is the user's. I named two — `git add -f`
  (zero file changes) and rename to a non-ignored extension — and the Lead took a
  **third that beat both**: a scoped negation appended to `.gitignore`
  (`!<bundle-dir>/**/*.log`, one comment line). It fixes the folder once instead
  of per-commit, keeps the filename the user's runbook names, and needs no
  index-row edit. Later rules win in `.gitignore`, and a negation works here only
  because the excluding rule is a **file** pattern (`*.log`); a negation cannot
  re-include anything under an excluded **directory**. None of the three is a
  residual-ledger entry — see [[audit-the-residual-ledger-in-both-directions]].
- Confirm the fix on the **staged** tree, not the working tree, and make the
  checks fail-loud: pipe the staged list through `git check-ignore -v --stdin`
  (exit 1, no output = none ignored); run `git ls-files --others <dir>` **without**
  `--exclude-standard` so ignored stragglers show up; and compare
  `git rev-parse :<path>` to `git hash-object <path>` per file, which catches an
  index that no longer matches disk. Also confirm the negation did not leak —
  the previously-ignored logs elsewhere should still be reported ignored.

Related: [[a-record-is-checkable-only-where-its-evidence-survives]] — the same
family of defect, where the artifact is right and its surroundings lose it.
