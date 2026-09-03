---
name: line-splitting-fixes-must-start-at-the-open-call
description: A parser guard that depends on indentation can be bypassed by any character the layer BELOW it treats as a line terminator — check open()'s newline= before believing a splitter fix.
metadata:
  type: feedback
---

When reviewing a fix to a text parser whose security depends on **indentation**
(column-0 guards, YAML-ish front-matter, config allowlists), the splitter is
never the bottom layer. Check the `open()` call first.

Python's `open(path, "r")` defaults to `newline=None` — **universal newlines** —
which rewrites every `\r\n` and every lone `\r` to `\n` *before* any application
code runs. A fix expressed as `re.split(r"\r\n|\r|\n", text)` therefore cannot
observe a `\r` at all: it is dead with respect to the exact character it names.

**Why:** In `scribe-decision-merger.sh` (PR #355, blocker 4, round 3), the
column-0 guard was bypassed three times. Round 2 was tab/NBSP/em-space (the
`lstrip(" ")` fix) plus vertical tab and form feed (the `splitlines()` fix).
Round 3 shipped both fixes and the tests were genuinely good — 146/146, with
vendored pre-fix hooks as positive controls, and I confirmed independently that
reverting *only* the splitter re-forges the vtab/form-feed fixtures. It still
fell to a bare `\r` used as the indent prefix, because universal-newline
translation had already promoted the nested line to column 0.

**How to apply:**
- Enumerate the character classes each layer treats as a break, and diff them:
  `open(newline=...)` ⊃ `str.splitlines()` ⊃ `re.split(r"\n")` ⊃ `grep`.
  A guard is only as strong as the *widest* layer beneath it.
- `str.splitlines()` breaks on \v \f \x1c \x1d \x1e \x85     as well as
  the three real ones. Every one of those is also `str.isspace()`, so a
  whitespace-aware `lstrip()` covers them — but only if they survive to the
  guard. `\r` is the outlier: it is stripped *earlier*, by the file layer.
- Grep-based auditing uses LF only. If the parser and `grep` disagree about what
  a line is, that divergence is itself the finding — here `grep -n '^agent:'`
  showed only the honest declaration while the parser recorded the forged one.
- Non-space format controls (U+200B, U+FEFF, U+180E, U+2060, U+00AD) are the
  mirror case: not stripped, not split, so they stay in the key name and fail
  closed. Test both directions.
- Fix shape that worked: `open(..., newline="")` plus splitting on `\r\n|\n`
  only, leaving a lone `\r` as in-line whitespace the indent counter can see.

Related: [[last-review-verdict-is-forgeable]],
[[a-disclaimer-does-not-fix-a-false-claim]].
