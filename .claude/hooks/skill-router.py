#!/usr/bin/env python3
"""
UserPromptSubmit hook: scans the user's prompt against a regex rules table
and emits a "## Suggested skills" hint as additional context. Never blocks.

The rules table lives at .claude/skill-router.json. Adding a rule:
  { "pattern": "<case-insensitive python regex>", "skill": "<skill name>" }

Failsafe: on no match → exit 0 silently (zero context cost).
On any error → exit 0 silently (never block prompt submission).
"""
from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path


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


def load_rules(repo: Path) -> list[tuple[re.Pattern, str]]:
    p = repo / ".claude" / "skill-router.json"
    if not p.is_file():
        return []
    try:
        data = json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return []
    rules = []
    for entry in data.get("rules", []):
        try:
            pat = re.compile(entry["pattern"])
            rules.append((pat, entry["skill"]))
        except Exception:
            continue
    return rules


def main() -> int:
    payload = read_payload()
    repo = project_dir(payload)

    prompt = payload.get("prompt") or ""
    if not prompt or not isinstance(prompt, str):
        return 0

    rules = load_rules(repo)
    if not rules:
        return 0

    matched: list[str] = []
    for pat, skill in rules:
        try:
            if pat.search(prompt):
                if skill not in matched:
                    matched.append(skill)
        except Exception:
            continue

    if not matched:
        return 0

    # Emit a single one-line context hint. Skills are loaded lazily; this is
    # advisory, not invocation.
    sys.stdout.write(
        "## Suggested skills\n"
        f"Based on the prompt, these skills look relevant: {', '.join(f'`{s}`' for s in matched)}. "
        "Reference them via `.claude/skills/<name>/SKILL.md` if useful.\n"
    )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        sys.stderr.write(f"skill-router: {e}\n")
        sys.exit(0)
