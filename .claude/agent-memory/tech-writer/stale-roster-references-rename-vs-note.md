---
name: stale-roster-references-rename-vs-note
description: When a doc names retired agents/roles, check whether the work it describes shipped before deciding to rename vs. add a historical note.
metadata:
  type: feedback
---

When a document assigns work to agents/roles that no longer exist (e.g. the 2026-09 Tolkien-codename → role-based `.claude/agents/` migration), don't default to find-and-replace the names with current roster equivalents. First verify whether the work the document describes is already done (check git log for the merging PR, check any linked status/matrix doc for "done"/"verified" markers, check for TODO/issue-number references in code).

- **Work already shipped → add a dated historical note, don't rename.** Renaming makes a closed phase look like it has a live, current assignment. State plainly in the note that the names are retired, point to `.claude/agents/` and `CLAUDE.md` for current ownership, and leave the original table/names untouched as a record of how the work was actually staffed.
- **Work still open / document still governs live process → rename to current roster, with verification.** Don't invent a mapping when the old role has no clean successor (e.g. old "Frontend Dev" or "Tester" roles split across `js-dev`/`csharp-dev` with no dedicated tester now — say so explicitly rather than picking one). Confirm each mapping against `CLAUDE.md`'s Squad Members and Routing tables and, where plausible, against the actual code (e.g. grep for `Middleware` to confirm "host/API middleware hardening" is C#-only before assigning it solely to `csharp-dev`).

**Why:** a rename on a dead document is a false-currency signal — worse than an obviously dated doc — while leaving a *live* document's ownership table stale means real work has no owner. The same "just replace the names" instinct is wrong in both directions for different reasons; the fork depends entirely on whether the underlying work is done, which has to be checked (git log, linked status docs), not assumed from the doc's title or age.

**How to apply:** any future roster-migration doc sweep. Applied on 2026-09-03 to `docs/guides/abies-ui-issue-152-execution-plan.md` (historical note — issue #152 Phase 1 shipped in PR #167, confirmed via `git log` and `docs/guides/abies-ui-accessibility-matrix.md` showing all 7 components ✅ Verified) and `docs/security/hardening-backlog.md` (renamed — backlog has open P2 items, e.g. security regression test suite, threat model governance gate, so it's still a live document).
