#!/usr/bin/env python3
"""
SessionStart hook for the squad framework.

Emits a compact "Squad State" summary to stdout, which Claude Code injects
as additional context. Runs on `startup`, `resume`, `clear`, and `compact`.
On `resume` it emits a smaller payload because the prior context already
exists in the conversation.

Inputs read (all read-only):
  - `git rev-parse --abbrev-ref HEAD`           -> current branch
  - `git log -3 --pretty=format:%h %s`          -> last 3 commit subjects
  - .claude/docs/decisions.md                    -> head 30 lines
  - .claude/docs/principles-enforcement.md       -> head 20 lines
  - .squad/decisions/inbox/*.md                  -> count
  - .squad/decisions/quarantine/*.md             -> count
  - .squad/learnings/inbox/*.md                  -> count
  - .squad/log/<latest>-session.md               -> last entry
  - .squad/.last-review-verdict                  -> single line

Hard cap on output size: 2000 tokens-ish, enforced as ~8000 chars.

Exit codes: always 0 (this hook never blocks startup).
"""
from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from pathlib import Path

CHAR_BUDGET = 8000  # ~2k tokens hard cap


def read_payload() -> dict:
    try:
        return json.loads(sys.stdin.read())
    except Exception:
        return {}


def project_dir(payload: dict) -> Path:
    candidates = [
        os.environ.get("CLAUDE_PROJECT_DIR"),
        payload.get("cwd"),
        os.getcwd(),
    ]
    for c in candidates:
        if c:
            return Path(c)
    return Path.cwd()


def git_branch(repo: Path) -> str | None:
    try:
        out = subprocess.run(
            ["git", "rev-parse", "--abbrev-ref", "HEAD"],
            cwd=repo,
            capture_output=True,
            text=True,
            timeout=2,
        )
        if out.returncode == 0:
            return out.stdout.strip()
    except Exception:
        pass
    return None


def git_recent_commits(repo: Path, n: int = 3) -> list[str]:
    try:
        out = subprocess.run(
            ["git", "log", f"-{n}", "--pretty=format:%h %s"],
            cwd=repo,
            capture_output=True,
            text=True,
            timeout=2,
        )
        if out.returncode == 0 and out.stdout.strip():
            return out.stdout.strip().splitlines()
    except Exception:
        pass
    return []


def git_uncommitted_count(repo: Path) -> int | None:
    try:
        out = subprocess.run(
            ["git", "status", "--porcelain"],
            cwd=repo,
            capture_output=True,
            text=True,
            timeout=2,
        )
        if out.returncode == 0:
            return len([l for l in out.stdout.splitlines() if l.strip()])
    except Exception:
        pass
    return None


def head_lines(path: Path, n: int) -> str:
    if not path.is_file():
        return ""
    try:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()[:n]
        return "\n".join(lines)
    except Exception:
        return ""


def count_files(directory: Path, pattern: str = "*.md") -> int:
    if not directory.is_dir():
        return 0
    return sum(1 for _ in directory.glob(pattern))


def latest_session_log_tail(repo: Path) -> str:
    log_dir = repo / ".squad" / "log"
    if not log_dir.is_dir():
        return ""
    sessions = sorted(log_dir.glob("*-session.md"))
    if not sessions:
        return ""
    try:
        text = sessions[-1].read_text(encoding="utf-8", errors="replace")
        # Last non-empty line.
        for line in reversed(text.splitlines()):
            if line.strip():
                return line.strip()
    except Exception:
        pass
    return ""


def last_verdict(repo: Path) -> str:
    p = repo / ".squad" / ".last-review-verdict"
    if not p.is_file():
        return "–"
    try:
        v = p.read_text(encoding="utf-8").strip().splitlines()
        return v[0] if v else "–"
    except Exception:
        return "–"


def write_hooks_sentinel(repo: Path) -> None:
    """Touch a sentinel file the statusline reads to detect hook freshness."""
    try:
        sentinel = repo / ".squad" / ".hooks-ok"
        sentinel.parent.mkdir(parents=True, exist_ok=True)
        sentinel.touch()
    except Exception:
        pass


def git_signing_health(repo: Path) -> tuple[str, str]:
    """Return (status, detail) where status is one of: 'ok', 'warn', 'unavailable'.

    Checks (in order):
      1. `gpg` binary on PATH.
      2. `commit.gpgsign` is true.
      3. `user.signingkey` is set.
      4. The signing key's secret half is in the local keyring.
      5. `user.email` matches at least one UID on the signing key.

    Misses on (2)-(5) return 'warn' with a specific reason.
    Miss on (1) returns 'unavailable'.
    All checks pass → 'ok'.
    """
    # gpg present?
    try:
        gpg_check = subprocess.run(
            ["gpg", "--version"], capture_output=True, text=True, timeout=2,
        )
        if gpg_check.returncode != 0:
            return ("unavailable", "gpg not on PATH")
    except FileNotFoundError:
        return ("unavailable", "gpg not on PATH")
    except Exception:
        return ("unavailable", "gpg invocation failed")

    def cfg(key: str) -> str:
        try:
            r = subprocess.run(
                ["git", "config", "--get", key],
                cwd=repo, capture_output=True, text=True, timeout=2,
            )
            return r.stdout.strip() if r.returncode == 0 else ""
        except Exception:
            return ""

    gpgsign = cfg("commit.gpgsign").lower()
    if gpgsign != "true":
        return ("warn", "commit.gpgsign is not true")

    key_id = cfg("user.signingkey")
    if not key_id:
        return ("warn", "user.signingkey is not set")

    user_email = cfg("user.email")
    if not user_email:
        return ("warn", "user.email is not set")

    # Inspect the secret keyring for that key id and its UIDs.
    try:
        r = subprocess.run(
            ["gpg", "--list-secret-keys", "--with-colons", key_id],
            capture_output=True, text=True, timeout=3,
        )
        if r.returncode != 0:
            return ("warn", f"signing key {key_id} not found in local keyring")
    except Exception:
        return ("warn", "could not query gpg keyring")

    # Parse colon-separated output for UID emails.
    # `uid:` lines have the user-id in field 10; format is "Name (comment) <email>".
    emails: list[str] = []
    for line in r.stdout.splitlines():
        if not line.startswith("uid:"):
            continue
        parts = line.split(":")
        if len(parts) < 10:
            continue
        uid = parts[9]
        # Extract the <email> portion if present.
        m = re.search(r"<([^>]+)>", uid)
        if m:
            emails.append(m.group(1).lower())
        else:
            # Some UIDs are bare email; keep the lower-cased token if it looks like one.
            token = uid.strip().lower()
            if "@" in token:
                emails.append(token)

    if user_email.lower() not in emails:
        return (
            "warn",
            f"user.email '{user_email}' is not a UID on key {key_id} "
            f"(GitHub will mark commits Unverified)",
        )

    return ("ok", f"key {key_id}, email {user_email}")


def render(payload: dict, repo: Path) -> str:
    source = payload.get("source", "startup")
    is_resume = source == "resume"

    branch = git_branch(repo)
    uncommitted = git_uncommitted_count(repo)
    decisions_inbox = count_files(repo / ".squad" / "decisions" / "inbox")
    decisions_quarantine = count_files(repo / ".squad" / "decisions" / "quarantine")
    learnings_inbox = count_files(repo / ".squad" / "learnings" / "inbox")
    verdict = last_verdict(repo)

    parts: list[str] = []
    parts.append("## Squad State")
    parts.append("")
    parts.append(f"_Loaded by `.claude/hooks/session-context-loader.py` ({source})._")
    parts.append("")

    # Git snapshot.
    git_bits = []
    if branch:
        git_bits.append(f"branch=`{branch}`")
    if uncommitted is not None:
        git_bits.append(f"uncommitted={uncommitted}")
    if git_bits:
        parts.append("**Git:** " + ", ".join(git_bits))

    # Git signing health.
    sign_status, sign_detail = git_signing_health(repo)
    # Write the cache file the enforce-gpg-signing hook reads to short-circuit.
    try:
        cache = repo / ".squad" / ".signing-health"
        cache.parent.mkdir(parents=True, exist_ok=True)
        # Detail is human-readable here; the commit hook re-detects on cache miss
        # using its own machine-readable codes, so the cache only needs to
        # convey verdict + a human-friendly tail. Slug spaces to keep | safe.
        cache.write_text(f"{sign_status}|{sign_detail}\n", encoding="utf-8")
    except Exception:
        pass

    if sign_status == "ok":
        parts.append(f"**Signing:** ✅ {sign_detail}")
    elif sign_status == "warn":
        parts.append(f"**Signing:** ⚠️ misconfigured — {sign_detail}")
    else:  # unavailable
        parts.append(f"**Signing:** ⏸ {sign_detail} (commits will be unsigned)")

    if not is_resume:
        commits = git_recent_commits(repo, 3)
        if commits:
            parts.append("")
            parts.append("**Recent commits:**")
            for c in commits:
                parts.append(f"- `{c}`")

    parts.append("")
    parts.append(
        f"**Inbox:** decisions={decisions_inbox}, quarantine={decisions_quarantine}, "
        f"learnings={learnings_inbox}"
    )
    parts.append(f"**Last reviewer verdict:** `{verdict}`")

    last_log = latest_session_log_tail(repo)
    if last_log:
        # Trim very long log lines.
        if len(last_log) > 200:
            last_log = last_log[:197] + "..."
        parts.append(f"**Last session log entry:** {last_log}")

    # Decisions excerpt — only on cold-start sources, not on resume.
    if not is_resume:
        decisions_head = head_lines(repo / ".claude" / "docs" / "decisions.md", 30)
        principles_head = head_lines(
            repo / ".claude" / "docs" / "principles-enforcement.md", 20
        )
        if decisions_head:
            parts.append("")
            parts.append("**`decisions.md` head (first 30 lines):**")
            parts.append("```markdown")
            parts.append(decisions_head)
            parts.append("```")
        if principles_head:
            parts.append("")
            parts.append("**`principles-enforcement.md` head (first 20 lines):**")
            parts.append("```markdown")
            parts.append(principles_head)
            parts.append("```")

    parts.append("")
    parts.append(
        "_Reminder: you are the Lead. Reviewer is the terminal node for code. "
        "See `CLAUDE.md` and `.claude/docs/principles-enforcement.md` for the "
        "deviation protocol._"
    )

    out = "\n".join(parts)
    if len(out) > CHAR_BUDGET:
        out = out[: CHAR_BUDGET - 50] + "\n[truncated]\n"
    return out


def main() -> int:
    payload = read_payload()
    repo = project_dir(payload)
    write_hooks_sentinel(repo)
    try:
        sys.stdout.write(render(payload, repo))
        sys.stdout.write("\n")
    except Exception as e:
        # Never block startup.
        sys.stderr.write(f"session-context-loader: {e}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
