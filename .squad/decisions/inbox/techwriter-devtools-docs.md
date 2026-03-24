### 2026-03-24: Debugger documentation placement

**By:** Tech Writer (via Coordinator, requested by Maurice)
**What:** Added `docs/guides/devtools.md` as the canonical reference for the Abies Time Travel Debugger (PR #181). `docs/guides/debugging.md` carries a 3-sentence summary + link. `docs/index.md` Guides table lists the new page.
**Why:** PR #181 shipped the debugger with no docs. Docs-ship-with-code policy requires this before merge.

### 2026-03-24: ADR-025 cross-reference

**By:** Tech Writer
**What:** `devtools.md` See Also section links to `../adr/ADR-025-time-travel-debugger.md`. If that ADR file does not exist yet, it needs to be created or the link needs updating before merge.
**Why:** The task prompt references "Architecture (ADR-025)" as the design record for this feature. Placeholder reference added so the gap is visible during review.
