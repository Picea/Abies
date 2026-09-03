#!/usr/bin/env python3
"""
Squad rotation hook (SessionEnd, async).

Mechanical state-management:
  - Compresses session logs older than the keep window into monthly tarballs.
  - Deletes dotnet-format logs older than 14 days.
  - Moves stale quarantine entries to cold-quarantine/<YYYY-MM>/.
  - Moves stale learnings inbox entries to .squad/learnings/cold/.
  - Tars cold archive buckets older than 6 months.

Does NOT touch:
  - decisions.md (semantic; manual + curator-driven)
  - principles-enforcement.md, charters, hooks, settings.json, CLAUDE.md
  - .squad/orchestration-log/ (forensic; manual prune only)
  - .squad/log/transcripts/ and .squad/log/snapshots/ (rotated by precompact hook)

Policy reference: .claude/docs/memory-policy.md.

Exit: always 0. Failures are logged to stderr but never block.
Run via SessionEnd hook (async: true) and via the /rotate skill on demand.
"""
from __future__ import annotations

import json
import os
import shutil
import sys
import tarfile
from datetime import datetime, timezone, timedelta
from pathlib import Path

# Policy windows.
SESSION_LOGS_KEEP_RECENT       = 30          # files
DOTNET_FORMAT_LOG_KEEP_DAYS    = 14
QUARANTINE_COLD_AFTER_DAYS     = 30
LEARNING_COLD_AFTER_DAYS       = 14
ARCHIVE_BUCKET_TAR_AFTER_DAYS  = 180         # 6 months


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


def file_age_days(path: Path) -> float:
    try:
        return (datetime.now(timezone.utc).timestamp() - path.stat().st_mtime) / 86400.0
    except Exception:
        return 0.0


def gzip_session_logs(repo: Path) -> None:
    log_dir = repo / ".squad" / "log"
    if not log_dir.is_dir():
        return
    sessions = sorted(log_dir.glob("*-session.md"))
    if len(sessions) <= SESSION_LOGS_KEEP_RECENT:
        return

    archive_dir = log_dir / "archive"
    archive_dir.mkdir(parents=True, exist_ok=True)

    # Files beyond the keep window get moved into per-month tarballs.
    overflow = sessions[: len(sessions) - SESSION_LOGS_KEEP_RECENT]
    by_month: dict[str, list[Path]] = {}
    for f in overflow:
        # filename pattern: YYYY-MM-DD-session.md
        try:
            month = f.name[:7]  # "YYYY-MM"
        except Exception:
            continue
        by_month.setdefault(month, []).append(f)

    for month, files in by_month.items():
        tar_path = archive_dir / f"{month}.tar.gz"
        try:
            mode = "a" if tar_path.exists() else "w:gz"
            # tarfile doesn't support 'a' on .tar.gz directly; if exists, build a temp.
            if tar_path.exists():
                tmp = tar_path.with_suffix(".tar.gz.tmp")
                with tarfile.open(tar_path, "r:gz") as src, tarfile.open(tmp, "w:gz") as dst:
                    for m in src.getmembers():
                        f_obj = src.extractfile(m)
                        if f_obj is None:
                            continue
                        dst.addfile(m, f_obj)
                    for f in files:
                        dst.add(f, arcname=f.name)
                tmp.replace(tar_path)
            else:
                with tarfile.open(tar_path, "w:gz") as dst:
                    for f in files:
                        dst.add(f, arcname=f.name)
            for f in files:
                try:
                    f.unlink()
                except Exception:
                    pass
        except Exception as e:
            sys.stderr.write(f"squad-rotate: gzip session logs ({month}): {e}\n")


def prune_dotnet_format_logs(repo: Path) -> None:
    log_dir = repo / ".squad" / "log"
    if not log_dir.is_dir():
        return
    for f in log_dir.glob("dotnet-format-*.log"):
        if file_age_days(f) > DOTNET_FORMAT_LOG_KEEP_DAYS:
            try:
                f.unlink()
            except Exception:
                pass


def cold_quarantine(repo: Path) -> None:
    quarantine = repo / ".squad" / "decisions" / "quarantine"
    if not quarantine.is_dir():
        return
    cold = repo / ".squad" / "decisions" / "cold-quarantine"
    moved = 0
    for f in list(quarantine.iterdir()):
        if not f.is_file():
            continue
        if file_age_days(f) <= QUARANTINE_COLD_AFTER_DAYS:
            continue
        # group by mtime month
        try:
            mtime = datetime.fromtimestamp(f.stat().st_mtime, tz=timezone.utc)
            month = mtime.strftime("%Y-%m")
        except Exception:
            month = "unknown"
        target_dir = cold / month
        target_dir.mkdir(parents=True, exist_ok=True)
        try:
            f.replace(target_dir / f.name)
            moved += 1
        except Exception as e:
            sys.stderr.write(f"squad-rotate: cold quarantine: {e}\n")
    if moved:
        # Note in the next session log; the session-logger hook will pick this up
        # but we also write a tiny breadcrumb here.
        log_dir = repo / ".squad" / "log"
        log_dir.mkdir(parents=True, exist_ok=True)
        bf = log_dir / f"{datetime.now(timezone.utc).strftime('%Y-%m-%d')}-rotation.md"
        try:
            with bf.open("a", encoding="utf-8") as fh:
                fh.write(
                    f"- {datetime.now(timezone.utc).isoformat(timespec='seconds')} "
                    f"moved {moved} stale quarantined drop(s) to cold-quarantine/\n"
                )
        except Exception:
            pass


def cold_learnings(repo: Path) -> None:
    inbox = repo / ".squad" / "learnings" / "inbox"
    if not inbox.is_dir():
        return
    cold = repo / ".squad" / "learnings" / "cold"
    cold.mkdir(parents=True, exist_ok=True)
    for f in list(inbox.glob("*.md")):
        if f.name == ".gitkeep":
            continue
        if file_age_days(f) <= LEARNING_COLD_AFTER_DAYS:
            continue
        try:
            f.replace(cold / f.name)
        except Exception as e:
            sys.stderr.write(f"squad-rotate: cold learning {f.name}: {e}\n")


def tar_old_archive_buckets(repo: Path) -> None:
    """Compress decision-archive monthly buckets older than 6 months."""
    archive_root = repo / ".squad" / "decisions" / "archive"
    if not archive_root.is_dir():
        return
    cold_root = archive_root / "cold"
    cold_root.mkdir(parents=True, exist_ok=True)

    cutoff = datetime.now(timezone.utc) - timedelta(days=ARCHIVE_BUCKET_TAR_AFTER_DAYS)
    for d in archive_root.iterdir():
        if not d.is_dir() or d.name == "cold":
            continue
        # buckets are named "YYYY-MM"
        try:
            bucket_dt = datetime.strptime(d.name, "%Y-%m").replace(tzinfo=timezone.utc)
        except ValueError:
            continue
        if bucket_dt > cutoff:
            continue
        # Group quarter
        quarter = f"{bucket_dt.year}-Q{(bucket_dt.month - 1) // 3 + 1}"
        tar_path = cold_root / f"{quarter}.tar.gz"
        try:
            if tar_path.exists():
                # append-mode for tar.gz requires rebuild; simpler to extend a sibling
                tmp = tar_path.with_suffix(".tar.gz.tmp")
                with tarfile.open(tar_path, "r:gz") as src, tarfile.open(tmp, "w:gz") as dst:
                    for m in src.getmembers():
                        fo = src.extractfile(m)
                        if fo is None:
                            continue
                        dst.addfile(m, fo)
                    dst.add(d, arcname=d.name)
                tmp.replace(tar_path)
            else:
                with tarfile.open(tar_path, "w:gz") as dst:
                    dst.add(d, arcname=d.name)
            shutil.rmtree(d)
        except Exception as e:
            sys.stderr.write(f"squad-rotate: archive {d.name}: {e}\n")


def main() -> int:
    payload = read_payload()
    repo = project_dir(payload)
    try:
        gzip_session_logs(repo)
        prune_dotnet_format_logs(repo)
        cold_quarantine(repo)
        cold_learnings(repo)
        tar_old_archive_buckets(repo)
    except Exception as e:
        sys.stderr.write(f"squad-rotate: top-level: {e}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
