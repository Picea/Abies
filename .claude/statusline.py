#!/usr/bin/env python3
"""
Statusline for Claude Code sessions in this repository.

Wired up in .claude/settings.json:
    "statusLine": {"type": "command",
                    "command": "python3 $CLAUDE_PROJECT_DIR/.claude/statusline.py",
                    "refreshInterval": 10}

Claude Code invokes this every `refreshInterval` seconds, feeding a JSON
payload describing the current session on stdin (model, workspace/cwd,
etc. — see Claude Code's statusLine docs for the exact schema; this script
only reads fields it needs and tolerates every field being absent). It
prints exactly one line to stdout; that line becomes the statusline.

This script is intentionally the sole consumer of three cache files three
different hooks maintain purely to feed it (see .claude/docs/decisions.md,
"statusLine dangling reference" migration entry, for why that's called out
explicitly rather than left implicit):

  - .squad/.last-review-verdict   written by scribe-decision-merger.sh
  - .squad/.hooks-ok              written by session-context-loader.py
  - .squad/.signing-health        written by session-context-loader.py
    and enforce-gpg-signing.sh

Design constraints:
  - MUST NOT raise. Every helper below degrades to a placeholder on any
    failure (missing file, malformed content, git/subprocess error) rather
    than let a stack trace become the statusline text or block the UI.
  - MUST be fast. This runs every 10 seconds; every external call has a
    short timeout and nothing here does unbounded I/O.
"""
from __future__ import annotations

import json
import os
import subprocess
import sys
import time
from pathlib import Path


def read_stdin_json() -> dict:
    try:
        raw = sys.stdin.read()
        if not raw.strip():
            return {}
        parsed = json.loads(raw)
        return parsed if isinstance(parsed, dict) else {}
    except Exception:
        return {}


def resolve_project_dir(payload: dict) -> Path:
    env_dir = os.environ.get("CLAUDE_PROJECT_DIR")
    if env_dir:
        return Path(env_dir)
    workspace = payload.get("workspace", {}) if isinstance(payload, dict) else {}
    for key in ("project_dir", "current_dir"):
        candidate = workspace.get(key) if isinstance(workspace, dict) else None
        if candidate:
            return Path(candidate)
    return Path.cwd()


def git_branch(repo: Path) -> str:
    try:
        result = subprocess.run(
            ["git", "-C", str(repo), "rev-parse", "--abbrev-ref", "HEAD"],
            capture_output=True, text=True, timeout=2,
        )
        if result.returncode == 0:
            branch = result.stdout.strip()
            return branch if branch else "?"
    except Exception:
        pass
    return "?"


def last_review_verdict(repo: Path) -> str:
    """Single line written by scribe-decision-merger.sh: PASS|NEEDS-CHANGES|BLOCKED|-"""
    p = repo / ".squad" / ".last-review-verdict"
    if not p.is_file():
        return "–"  # en dash — "no verdict recorded yet"
    try:
        lines = p.read_text(encoding="utf-8").strip().splitlines()
        return lines[0].strip() if lines and lines[0].strip() else "–"
    except Exception:
        return "–"


def count_files(directory: Path) -> int:
    if not directory.is_dir():
        return 0
    try:
        return sum(1 for entry in directory.iterdir() if entry.is_file())
    except Exception:
        return 0


def signing_health(repo: Path) -> str:
    """First field of .squad/.signing-health: ok|warn|unavailable."""
    p = repo / ".squad" / ".signing-health"
    if not p.is_file():
        return "?"
    try:
        first_line = p.read_text(encoding="utf-8").strip().splitlines()
        if not first_line:
            return "?"
        verdict = first_line[0].split("|", 1)[0].strip()
        return verdict or "?"
    except Exception:
        return "?"


def hooks_freshness(repo: Path) -> str:
    """Age of .squad/.hooks-ok, touched every SessionStart by
    session-context-loader.py. A large age means hooks haven't run this
    session (or the session is stale)."""
    p = repo / ".squad" / ".hooks-ok"
    if not p.is_file():
        return "never"
    try:
        age = int(time.time() - p.stat().st_mtime)
    except Exception:
        return "?"
    if age < 0:
        return "0s"
    if age < 60:
        return f"{age}s"
    if age < 3600:
        return f"{age // 60}m"
    return f"{age // 3600}h"


def active_profiles(repo: Path) -> list[str]:
    """Reads profiles-active.md at the repo root, one profile slug per
    bullet line, if that file exists. See
    .claude/docs/profiles/sovereignty-profile.md, "Statusline indicator"."""
    p = repo / "profiles-active.md"
    if not p.is_file():
        return []
    try:
        text = p.read_text(encoding="utf-8")
    except Exception:
        return []
    profiles = []
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("-"):
            profiles.append(stripped.lstrip("- ").strip().lower())
    return profiles


def fmt_profile(profiles: list[str]) -> str:
    if "sovereignty" in profiles:
        return "\U0001f1ea\U0001f1fa sov "  # EU flag
    return ""


def model_label(payload: dict) -> str:
    model = payload.get("model", {}) if isinstance(payload, dict) else {}
    if isinstance(model, dict):
        return model.get("display_name") or model.get("id") or "claude"
    return "claude"


def main() -> int:
    payload = read_stdin_json()
    repo = resolve_project_dir(payload)

    try:
        branch = git_branch(repo)
        verdict = last_review_verdict(repo)
        inbox_n = count_files(repo / ".squad" / "decisions" / "inbox")
        quarantine_n = count_files(repo / ".squad" / "decisions" / "quarantine")
        sign = signing_health(repo)
        hooks_age = hooks_freshness(repo)
        profile_tag = fmt_profile(active_profiles(repo))
        model = model_label(payload)

        line = (
            f"{profile_tag}{model} | {branch} | "
            f"last-review:{verdict} | inbox={inbox_n} Q={quarantine_n} | "
            f"sign:{sign} | hooks:{hooks_age}"
        )
    except Exception:
        # Last-resort fallback: never let this script's own output be a
        # traceback. A short, honest placeholder beats a broken statusline.
        line = "claude-squad (statusline error)"

    print(line)
    return 0


if __name__ == "__main__":
    sys.exit(main())
