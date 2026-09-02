# Memory and rotation policy

The squad accumulates state in `.claude/docs/decisions.md`, `.squad/log/`, `.squad/design/`, `.squad/decisions/archive/`, and `.squad/learnings/` as sessions run. Without rotation this state grows unbounded, which:

1. Bloats `decisions.md` past the ~200-line threshold beyond which Anthropic's docs warn adherence drops.
2. Makes the daily session-logger output harder to scan in retros.
3. Creates the impression of "more memory = more knowledge" when in practice the operative knowledge is a small head index plus a long tail nobody reads.

This file documents the policy. The mechanical execution lives in `.claude/hooks/squad-rotate.py` (run on `SessionEnd` and via the `/rotate` skill on demand).

## Targets and policies

### `decisions.md`

- **Active section** (everything below `## Session Decisions` and above `## Archive Index`): cap at **40 entries** of *active* (non-superseded, non-retired) decisions.
- **Superseded / retired entries**: when a new decision references an older one in `references:` and uses `verdict: NEEDS-CHANGES` or includes the keyword `supersedes` in the body, the older entry becomes a candidate for archival.
- **Archive**: entries older than **90 days** with no recent reference, or marked superseded, move to `.claude/docs/decisions-archive/<YYYY-QQ>.md`. The head of `decisions.md` carries an `## Archive Index` listing the archive files and date ranges.

### `.squad/log/`

- **Session logs** (`<date>-session.md`): keep the most recent **30** in-place. Older ones are gzipped together monthly into `.squad/log/archive/<YYYY-MM>.tar.gz`.
- **Transcripts** (`.squad/log/transcripts/`): rotation is handled by the precompact-snapshot hook itself (last 20). The rotation hook here does not touch them.
- **Snapshots** (`.squad/log/snapshots/`): same — the precompact hook rotates these.
- **dotnet-format logs** (`dotnet-format-<date>.log`): keep last 14 days; older deleted.

### `.squad/decisions/archive/`

- **Monthly buckets** under `.squad/decisions/archive/<YYYY-MM>/`. Already structured this way by the scribe-decision-merger.
- **Cold storage**: buckets older than **6 months** are tarred to `.squad/decisions/archive/cold/<YYYY-QQ>.tar.gz`.

### `.squad/decisions/quarantine/`

- Quarantine entries older than **30 days** are moved to `.squad/decisions/cold-quarantine/<YYYY-MM>/` and the rotation hook emits a one-line warning about the count to the next session log. They are never auto-deleted — quarantine is forensic evidence.

### `.squad/learnings/inbox/`

- **Cold learnings**: items older than **14 days** that have not been promoted (no matching entry in `decisions.md` referencing the slug) move to `.squad/learnings/cold/`. The curator subagent reads cold/ on demand but does not auto-promote from there.
- **Promoted learnings**: when the user accepts a curator proposal and applies it manually, the proposal file moves to `.squad/learnings/archive/<YYYY-MM>/`. (This is a manual move; the rotation hook does not detect "accepted" automatically.)

### `.squad/design/`

- **Per-pass directories** `<slug>/` holding the numbered phase artifacts (`00-scope.md` through `07-handoff.md`).
- **Not auto-rotated.** Design artifacts are the reasoning trail behind decisions in `decisions.md` — when someone asks "why is it built this way", the decision drop is the summary and this is the evidence. Manual prune only.
- A pass whose decision drop has been archived is a candidate for manual archival too, but the drop must keep a working reference. If you move a design directory, update the `references` in its decision.

### `.squad/orchestration-log/`

- **Not auto-rotated.** This is forensic state for "what did the Lead do" and you may need months-old entries during postmortems. Manual prune only.

## What is NOT rotated

These files are load-bearing and the hook never touches them:

- `.claude/docs/principles-enforcement.md`
- `.claude/docs/tech-stack.md`
- `.claude/docs/decision-schema.md`
- All charter files in `.claude/agents/*.md`
- All hook scripts in `.claude/hooks/*`
- `.claude/settings.json`
- `CLAUDE.md`
- Skills in `.claude/skills/*/SKILL.md` (rotation is conceptual content; skills are documentation)

## Interaction with the curator

Rotation is mechanical: move + gzip + index update. Semantic pruning ("this convention is no longer relevant") is a curator concern. The curator writes proposals to `.squad/learnings/inbox/` recommending entries be marked superseded; the user accepts; only then does the rotation hook treat them as candidates for archival.

The rotation hook never deletes evidence. It moves and compresses. Anything it touches is recoverable.

## Anchors and stable IDs

The rotation hook references decisions by their `id:` field, not by their position in `decisions.md`. Skills, charters, and other docs that reference specific decisions should also use IDs (e.g., `architect-20260415T120000Z-article-state-machine`), not line numbers. Line numbers shift on every rotation pass; IDs do not.
