---
name: track-a-missed-constraint-patterns
description: Constraints the first-principles track repeatedly reasons past in Abies — so far, every miss is about people or adoption, never about the artifact. Candidates for the scope template.
metadata:
  type: project
---

Recurring misses belong in `00-scope.md`'s template so future passes state them up front.
One pass of evidence so far; do not promote to the template until a second pass confirms.

## The pattern, stated as a hypothesis

**Track A hits structural constraints and misses human ones.** In `undo-redo` it derived
every structural constraint I independently verified — three of them by reading code Track B
never opened — and missed exactly three things, all of which live in a person's head or in an
adoption cost rather than in the artifact.

This makes sense mechanically: retrieval is where the record of *what users rejected* lives,
and Track A has no retrieval. It is not a defect in the reasoning; it is the shape of the
blindness, and it means Track B's failure-mode catalogue is where to look for Track A's gaps
first.

## Misses observed (`undo-redo`, 2026-09-06)

- **User-perceived granularity.** Derived a per-keystroke quotient and downgraded its own
  better coalescing rule to something that does not coalesce. The industry answer (three
  independent systems converged on a 500 ms window) is unreachable without retrieval.
- **State the model never described.** Its `model ⊗ world` factorisation covered state the
  app *told* the outside, but not state the outside *owns* — caret, focus, scroll,
  uncontrolled inputs. A model-only undo passes the invariant and still is not undo.
- **Adoption cost.** "Opts in by changing one type argument" — true, and it understates what
  happens to every test asserting on the model. The equivalent complaint is the single
  most-reported integration problem for the analogous library.

## Not misses — credit these correctly

Track A *bounded* the keyboard-affordance question and the three external-package unknowns
explicitly rather than guessing, and named in advance what each answer would imply. That is
the ideal behaviour for a blind track and should be read as a positive signal, not as a gap.
Do not score "I could not determine X" as a miss.

## If this recurs

Candidate scope-template line, for the `architect` to consider — **not** something to add
unilaterally: a short "constraints from users, not from structure" section stating the
known human-side limits up front, in plain language, without pattern names (it has to
survive `scope-warden`).

See [[calibration-dual-track]].
