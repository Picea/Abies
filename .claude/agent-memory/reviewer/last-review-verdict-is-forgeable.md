---
name: last-review-verdict-is-forgeable
description: The decision-drop validator's indent guard only counts spaces, so a tab- or NBSP-indented nested key still forges a Reviewer PASS — do not trust .last-review-verdict as evidence
metadata:
  type: project
---

`.claude/hooks/scribe-decision-merger.sh`'s minimal YAML parser promotes nested
`key: value` lines to top level, last write wins. A drop declaring
`agent: csharp-dev` with a nested `meta:` block containing `agent: reviewer` /
`verdict: PASS` is archived into `decisions.md` as `[reviewer · PASS]` and
writes `PASS` into `.squad/.last-review-verdict`.

**Status as of 2026-09-02 (PR #355 re-review): still open, narrowed only.**
A guard was added — `if indent > 0 and k in REQUIRED_TOPLEVEL_FIELDS: continue`
for `{id, agent, verdict, scope, created}` — but it computes
`indent = len(line) - len(line.lstrip(" "))`, i.e. **spaces only**. Two working
exploits verified against the guarded hook:

1. Indent the nested `agent:`/`verdict:` with a **tab**, NBSP, ideographic
   space, vertical tab, form feed, or em space instead of spaces. `indent`
   evaluates to 0, the guard never fires, `str.strip()` removes the character
   anyway, and the forgery lands unchanged.
2. Spaces only, using the deliberately-unguarded `blockers` key: a
   `agent: reviewer` / `verdict: PASS` drop with a real non-empty top-level
   `blockers` list plus a nested `notes:\n  blockers: []` is archived as PASS.
   This also defeats the verdict↔blockers consistency check that is currently
   the *only* thing rejecting exploit 1's space-indented ancestor.

**Why:** the sibling `agent_identity.py` and the V13 fixtures in
`tests/run.sh` already handle non-ASCII whitespace at the frontmatter fence,
so the codebase knows this class — the guard just didn't reuse it. Upstream
tracking is MCGPPeters/squad-template#8, still open.

**How to apply:** never cite `.squad/.last-review-verdict` or a
`[reviewer · PASS]` heading in `decisions.md` as proof a review happened — open
the archived drop and check the *column-0* `agent:` yourself. When re-reviewing
this hook, the fix is to normalise indentation over all whitespace
(`len(line) - len(line.lstrip())`, or reject any leading non-space whitespace
outright) and to extend the guard to `blockers`. A fixture that only exercises
space indentation will pass while the bug is live — insist on tab/NBSP cases.
Related: [[command-text-matching-is-not-a-gate]],
[[doc-inventory-counts-go-stale-within-the-same-pr]].
