"""
Shared artifact attribution for the SubagentStop gate hooks
(scope-warden.sh, lexicon-check.sh, validate-phase-artifact.sh).

Each of these hooks needs to answer "which design pass does THIS invocation's
artifact belong to" from a `SubagentStop` payload that carries a `cwd` and an
`agent_type` but no `slug`. All three previously answered it the same way:
scan every `.squad/design/<slug>/` directory for the expected filename and
take whichever copy has the newest mtime, anywhere in the repository. See
review round 1 of PR #358, ⚠️-3, for why that is the wrong signal:

  - With two passes in flight, the newest-mtime file across ALL slugs is not
    necessarily THIS invocation's slug -- a concurrent pass's touch can win.
  - A phase agent that returns without writing its artifact at all still
    finds *some* file by this search (the previous pass's), and the hook
    reports green against work this invocation never did -- precisely the
    "a phase that ran vs. a phase that returned" distinction these hooks
    exist to draw.
  - `scope-warden.sh`'s own close-out re-run can overwrite a scan the user
    already gated on, attributed to the wrong slug.

`enforce-phase-order.sh` (a PreToolUse hook, not a SubagentStop one) does not
have this problem: it fires ON the Write call itself and reads the target
path directly out of tool_input. A SubagentStop hook fires only after the
fact and has no direct equivalent -- but the payload does carry
`transcript_path` (see session-logger.sh, which already reads it), and the
transcript is a JSONL log of the very turn that just ended, including every
tool_use block the agent issued. A Write to the expected artifact recorded
IN THAT TRANSCRIPT is ground truth for what this invocation did; nothing
else here is.

Attribution preference order, cheapest and strongest first:

  1. TRANSCRIPT WRITE -- the transcript for this invocation itself records a
     Write whose `file_path` ends with `/<artifact_name>`. Its slug comes
     directly from that path; there is no cross-slug guessing at all. This
     is the fix: ground truth in place of a global mtime guess.
  2. NEWEST-SINCE-START -- no such Write is recoverable (older payload shape
     with no `transcript_path`, an unreadable transcript, or a transcript
     whose shape this parser's assumptions do not cover), so fall back to
     the pre-existing newest-mtime search across every slug -- but refuse
     (report no candidate at all) if even that newest file predates this
     invocation's own earliest transcript timestamp. A file that already
     existed before this turn began cannot be this turn's output, so
     attributing it to this invocation would be validating (or scanning)
     someone else's pass under this agent's name.
  3. NEWEST-NO-BOUND -- no transcript timestamp is available at all (no
     `transcript_path`, or it could not be read), so there is no start time
     to bound the guess by. The pre-existing newest-mtime result is used
     as-is. This is the one case with no stronger signal available in the
     payload; documented here rather than silently indistinguishable from
     case 2.

Callers get `method` back and may report it (or not); it exists so a reader
of a hook's future output can tell a ground-truth attribution from a guess.
"""

import datetime
import json
import os


def _transcript_signal(transcript_path, artifact_name):
    """(start_ts, write_paths) from a JSONL transcript. `start_ts` is the
    earliest timestamp found anywhere in the transcript (a datetime, or None
    if the transcript has none / cannot be read). `write_paths` is every
    `file_path` from a `Write` tool_use block whose value ends with
    `/<artifact_name>`, in transcript order (so the LAST entry is the most
    recent state on disk for this invocation's own output). Best-effort:
    any read/parse failure yields (None, []), never raises -- a hook that
    cannot use this signal falls back to the mtime guess, it does not
    crash."""
    start = None
    writes = []
    if not transcript_path or not os.path.isfile(transcript_path):
        return start, writes
    try:
        with open(transcript_path, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                line = line.strip()
                if not line:
                    continue
                try:
                    obj = json.loads(line)
                except Exception:
                    continue

                ts_raw = obj.get("timestamp")
                if ts_raw is None and isinstance(obj.get("message"), dict):
                    ts_raw = obj["message"].get("timestamp")
                if isinstance(ts_raw, str):
                    try:
                        ts = datetime.datetime.fromisoformat(
                            ts_raw.replace("Z", "+00:00"))
                        if start is None or ts < start:
                            start = ts
                    except Exception:
                        pass

                msg = obj.get("message") if isinstance(obj.get("message"), dict) else obj
                content = msg.get("content") if isinstance(msg, dict) else None
                if not isinstance(content, list):
                    continue
                for blk in content:
                    if not isinstance(blk, dict):
                        continue
                    if blk.get("type") != "tool_use" or blk.get("name") != "Write":
                        continue
                    fp = (blk.get("input") or {}).get("file_path")
                    if isinstance(fp, str) and fp.replace("\\", "/").endswith("/" + artifact_name):
                        writes.append(fp)
    except OSError:
        pass
    return start, writes


def _safe_slug(design_dir, path):
    """The slug component of `path` relative to `design_dir`, or None if
    `path` does not actually live under `design_dir` at all.

    `os.path.relpath` alone is unbounded: it happily computes a `..`-laden
    relative path between any two locations, whether or not one is really
    inside the other. Round 2 review of PR #358 (💡 nitpick, `devops`'s
    files): a transcript `Write` recorded for a path ending `/<artifact_name>`
    OUTSIDE `.squad/design/` -- e.g. a scratch file at `/tmp/foo/00-scope.md`
    -- would previously yield a slug like `../../tmp/foo`, which is not a
    privilege escalation (the caller already owns whatever it writes next)
    but does reach a user-facing report (`scope-warden.sh`'s own scan output)
    as if it were a real design-pass slug. Rejecting anything whose relative
    form climbs out of `design_dir` keeps the returned slug a genuine
    `.squad/design/<slug>` subdirectory name, never a traversal."""
    design_dir_abs = os.path.normpath(design_dir)
    path_abs = os.path.normpath(path)
    rel = os.path.relpath(os.path.dirname(path_abs), design_dir_abs)
    if rel == os.curdir:
        return None  # the artifact would live directly in design_dir, not <slug>/
    if rel.startswith(os.pardir) or os.path.isabs(rel):
        return None  # climbs out of design_dir -- not a real slug
    return rel


def resolve_artifact(design_dir, artifact_name, transcript_path):
    """Best available (slug, path, mtime, method) for `artifact_name` under
    `design_dir/<slug>/`, attributed to the SPECIFIC agent invocation this
    SubagentStop payload describes. Returns None if no candidate exists at
    all, or if the only candidate found is provably older than this
    invocation's own start. See the module docstring for the three-tier
    preference order."""
    start, writes = _transcript_signal(transcript_path, artifact_name)

    if writes:
        fp = writes[-1]
        # fp, as recorded by the Write tool_use in the transcript, is
        # relative to the AGENT'S cwd (typically the repo/worktree root),
        # not to design_dir -- design_dir is that root's ".squad/design"
        # subdirectory, so the root is two levels up from it, never
        # design_dir itself.
        root = os.path.dirname(os.path.dirname(design_dir))
        path = fp if os.path.isabs(fp) else os.path.normpath(os.path.join(root, fp))
        if os.path.isfile(path):
            slug = _safe_slug(design_dir, path)
            if slug is not None:
                try:
                    mtime = os.path.getmtime(path)
                except OSError:
                    mtime = None
                return (slug, path, mtime, "transcript-write")
            # The transcript recorded a Write, but it resolves outside
            # design_dir entirely -- not this hook's business to attribute
            # to a slug at all. Fall through to the mtime search below,
            # same as "the file is not there now".
        # The transcript recorded a Write but the file is not there now
        # (moved, deleted, rewritten elsewhere) -- fall through rather than
        # report a path that does not exist.

    candidates = []
    if os.path.isdir(design_dir):
        for slug in os.listdir(design_dir):
            p = os.path.join(design_dir, slug, artifact_name)
            if os.path.isfile(p):
                try:
                    candidates.append((os.path.getmtime(p), slug, p))
                except OSError:
                    continue
    if not candidates:
        return None
    candidates.sort()
    mtime, slug, path = candidates[-1]

    if start is not None:
        try:
            file_ts = datetime.datetime.fromtimestamp(mtime, datetime.timezone.utc)
        except Exception:
            file_ts = None
        if file_ts is not None and file_ts < start:
            # The newest matching file anywhere still predates this
            # invocation's own start: it cannot be this turn's output.
            # Refusing to attribute is safer than validating (or scanning)
            # a different pass under this agent's name.
            return None
        return (slug, path, mtime, "newest-since-start")

    return (slug, path, mtime, "newest-no-bound")
