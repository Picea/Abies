#!/usr/bin/env python3
"""
PreCompact hook for the squad framework.

Snapshots the transcript and current decisions/inbox state before Claude Code
runs compaction (manual `/compact` or auto). Compaction is lossy by design;
this gives operators a recovery point.

Outputs:
  .squad/log/transcripts/<date>-<sessionId>-<trigger>.jsonl
      Verbatim copy of the transcript JSONL at compact time.

  .squad/log/snapshots/<date>-<sessionId>-precompact.md
      Markdown snapshot listing:
        - timestamp, trigger, custom instructions if manual
        - branch, head SHA
        - current decisions inbox file list
        - current quarantine file list
        - current learnings inbox file list
        - tail of decisions.md (last 60 lines, post-Session-Decisions anchor)

Rotation: keeps last 20 transcripts and last 20 snapshots; older ones are
deleted to keep disk pressure bounded. Snapshots and transcripts rotate
independently.

Exit codes: always 0. Async-safe — set "async": true in settings.json so
this never blocks compaction.
"""
from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

KEEP = 20  # rotation depth, per directory


def read_payload() -> dict:
    try:
        return json.loads(sys.stdin.read())
    except Exception:
        return {}


def project_dir(payload: dict) -> Path:
    for c in (os.environ.get("CLAUDE_PROJECT_DIR"), payload.get("cwd"), os.getcwd()):
        if c:
            return Path(c)
    return Path.cwd()


def git_head(repo: Path) -> tuple[str, str]:
    branch = ""
    sha = ""
    try:
        b = subprocess.run(
            ["git", "rev-parse", "--abbrev-ref", "HEAD"],
            cwd=repo, capture_output=True, text=True, timeout=2,
        )
        if b.returncode == 0:
            branch = b.stdout.strip()
        s = subprocess.run(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=repo, capture_output=True, text=True, timeout=2,
        )
        if s.returncode == 0:
            sha = s.stdout.strip()
    except Exception:
        pass
    return branch, sha


def list_dir(directory: Path, pattern: str = "*.md") -> list[str]:
    if not directory.is_dir():
        return []
    return sorted(p.name for p in directory.glob(pattern))


def decisions_tail(repo: Path, n: int = 60) -> str:
    p = repo / ".claude" / "docs" / "decisions.md"
    if not p.is_file():
        return ""
    try:
        text = p.read_text(encoding="utf-8", errors="replace")
        # Take everything after the first "## Session Decisions" anchor;
        # if absent, fall back to the last n lines of the whole file.
        anchor = "## Session Decisions"
        idx = text.find(anchor)
        if idx >= 0:
            after = text[idx + len(anchor):]
            lines = after.splitlines()
        else:
            lines = text.splitlines()
        return "\n".join(lines[-n:])
    except Exception:
        return ""


def rotate(directory: Path, pattern: str, keep: int) -> None:
    if not directory.is_dir():
        return
    files = sorted(directory.glob(pattern), key=lambda p: p.name)
    excess = len(files) - keep
    for f in files[:excess if excess > 0 else 0]:
        try:
            f.unlink()
        except Exception:
            pass


def main() -> int:
    payload = read_payload()
    repo = project_dir(payload)

    log_dir = repo / ".squad" / "log"
    transcripts_dir = log_dir / "transcripts"
    snapshots_dir = log_dir / "snapshots"
    transcripts_dir.mkdir(parents=True, exist_ok=True)
    snapshots_dir.mkdir(parents=True, exist_ok=True)

    transcript_path = payload.get("transcript_path", "")
    session_id = (payload.get("session_id") or "unknown")[:8]
    trigger = payload.get("trigger", "unknown")  # "manual" or "auto"
    custom_instructions = payload.get("custom_instructions", "")

    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    ts = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H-%M-%S")

    # 1. Copy the transcript JSONL.
    if transcript_path and Path(transcript_path).is_file():
        dst = transcripts_dir / f"{ts}-{session_id}-{trigger}.jsonl"
        try:
            shutil.copy2(transcript_path, dst)
        except Exception as e:
            sys.stderr.write(f"precompact-snapshot: transcript copy failed: {e}\n")

    # 2. Write the markdown snapshot.
    branch, sha = git_head(repo)
    inbox = list_dir(repo / ".squad" / "decisions" / "inbox")
    quarantine = list_dir(repo / ".squad" / "decisions" / "quarantine")
    learnings = list_dir(repo / ".squad" / "learnings" / "inbox")
    decisions = decisions_tail(repo, 60)

    snapshot_path = snapshots_dir / f"{ts}-{session_id}-precompact.md"
    try:
        with snapshot_path.open("w", encoding="utf-8") as fh:
            fh.write(f"# Precompact snapshot — {ts} ({trigger})\n\n")
            fh.write(f"- session: `{session_id}`\n")
            fh.write(f"- trigger: `{trigger}`\n")
            fh.write(f"- branch: `{branch or 'unknown'}`\n")
            fh.write(f"- head: `{sha or 'unknown'}`\n")
            if custom_instructions:
                fh.write(f"- custom instructions: {custom_instructions!r}\n")
            fh.write(f"- transcript: `{Path(transcript_path).name if transcript_path else '–'}`\n\n")

            fh.write("## Decisions inbox at compact time\n\n")
            if inbox:
                for n in inbox:
                    fh.write(f"- `{n}`\n")
            else:
                fh.write("_(empty)_\n")
            fh.write("\n")

            if quarantine:
                fh.write("## Quarantined decisions\n\n")
                for n in quarantine:
                    fh.write(f"- `{n}`\n")
                fh.write("\n")

            fh.write("## Learnings inbox at compact time\n\n")
            if learnings:
                for n in learnings:
                    fh.write(f"- `{n}`\n")
            else:
                fh.write("_(empty)_\n")
            fh.write("\n")

            fh.write("## decisions.md tail (last 60 lines after Session Decisions)\n\n")
            fh.write("```markdown\n")
            fh.write(decisions or "_(empty)_\n")
            fh.write("\n```\n")
    except Exception as e:
        sys.stderr.write(f"precompact-snapshot: snapshot write failed: {e}\n")

    # 3. Rotate.
    rotate(transcripts_dir, "*.jsonl", KEEP)
    rotate(snapshots_dir, "*.md", KEEP)

    return 0


if __name__ == "__main__":
    sys.exit(main())
