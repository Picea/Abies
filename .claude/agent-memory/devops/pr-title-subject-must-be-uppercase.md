---
name: pr-title-subject-must-be-uppercase
description: PR title validation enforces Conventional Commits AND an uppercase first letter in the subject — lowercase subjects fail an otherwise valid title
metadata:
  type: feedback
---

`.github/workflows/pr-validation.yml` validates PR titles against Conventional
Commits *and* applies `subjectPattern: ^[A-Z].+$`. So `fix(hooks): close BOM
bypass` fails; `fix(hooks): Close BOM bypass` passes.

**Why:** Noted 2026-04-19 after a title that satisfied every Conventional
Commits rule was rejected. The failure message talks about the subject not
matching "the configured pattern", which does not obviously mean
"capitalise the first word", so the check costs a full CI round-trip to
diagnose.

**How to apply:** Capitalise the first letter of the PR subject when opening or
renaming a PR. This applies only to PR *titles* — the repo's commit convention
is separate. Note this deliberately diverges from the common Conventional
Commits style of a lowercase subject, so do not "fix" a passing title to match
habit.

Verified 2026-09-02: `subjectPattern: ^[A-Z].+$` is still configured, with a
`subjectPatternError` that spells out the uppercase requirement.
