#!/usr/bin/env bash
#
# PostToolUse hook for Write|Edit|MultiEdit.
# Runs `dotnet format` on a single C# file after Claude saves it, so the
# codebase stays formatted to the team's `.editorconfig` without manual
# intervention.
#
# Scope: `.cs` files only. `dotnet format` does not touch MSBuild XML
# (`.csproj`/`.props`/`.targets`) — verified: running it against one of
# those pays the full workspace-load cost (a measured 12.8s warm load
# across this solution's 48 projects) for zero formatting effect, since
# there's nothing in a `.csproj`/`.props`/`.targets` file for `dotnet
# format`'s whitespace/analyzer-fix engine to act on. An earlier version of
# this hook also matched those three extensions; that was dead weight, not
# a real capability.
#
# Behaviour:
#   - Looks up the nearest enclosing solution (.sln/.slnx) or project
#     (.csproj/.fsproj) walking up from the saved file. `dotnet format`
#     needs that as its workspace argument.
#   - Targets the single edited file via `--include <relative-path>` so we
#     don't reformat the entire solution on every keystroke.
#   - Falls back to `--folder` mode if no solution/project is found yet
#     (e.g. very early in scaffolding).
#   - Runs in the background with a short timeout so editor latency stays
#     low. The hook never blocks Claude.
#   - Serializes concurrent runs against the SAME workspace with a lock
#     file: a burst of edits (MultiEdit, several quick Edits in a row) each
#     fire this hook independently with no debounce, and each `dotnet
#     format` invocation pays its own full MSBuild workspace load. Without
#     serialization those loads stack concurrently. The lock doesn't
#     collapse the burst into one run (that needs real debounce logic this
#     PostToolUse hook has no place to keep state for between invocations
#     — no daemon, no persistent timer), but it does turn "N formats
#     running at once, each reloading the workspace" into "N formats
#     running one after another," which is the actual resource problem.
#     Uses `flock` when available (Linux; part of util-linux); falls back
#     to an atomic `mkdir`-based lock with staleness detection everywhere
#     else (e.g. macOS, which ships BSD flock with different semantics).
#
# Exit codes:
#   0 — always (formatting failures are logged but don't block Claude).

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

# Extract tool_input.file_path. Handle MultiEdit too (it has the same field).
file_path="$(
  printf '%s' "$payload" | python3 -c '
import json,sys
try:
    d = json.loads(sys.stdin.read())
except Exception:
    d = {}
ti = d.get("tool_input", {}) or {}
# Write/Edit/MultiEdit all use file_path
print(ti.get("file_path") or "")
' 2>/dev/null
)"

[ -z "$file_path" ] && exit 0
[ -f "$file_path" ] || exit 0

# Only fire for C# source. See header — .csproj/.props/.targets are
# deliberately NOT in scope; dotnet format never touched them.
case "$file_path" in
  *.cs) ;;
  *) exit 0 ;;
esac

# Skip generated files: anything under obj/ or bin/, anything ending in
# .Designer.cs or .g.cs, and anything inside a node_modules just in case.
case "$file_path" in
  */obj/*|*/bin/*|*/node_modules/*) exit 0 ;;
  *.Designer.cs|*.g.cs|*.g.i.cs) exit 0 ;;
esac

# Need the dotnet CLI. If it's not on PATH, log to stderr and exit cleanly.
if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet-format hook: 'dotnet' not on PATH; skipping format of $file_path" >&2
  exit 0
fi

# Walk up from the file to find the workspace anchor.
# Preference order:
#   1. nearest .sln or .slnx in any parent directory
#   2. nearest .csproj or .fsproj in any parent directory
#   3. fall back to the file's directory
abs_file="$(cd "$(dirname "$file_path")" && pwd)/$(basename "$file_path")"
dir="$(dirname "$abs_file")"

workspace=""
workspace_kind=""

while [ "$dir" != "/" ] && [ -n "$dir" ]; do
  # Prefer .slnx/.sln (workspace-wide).
  for f in "$dir"/*.slnx "$dir"/*.sln; do
    [ -f "$f" ] && { workspace="$f"; workspace_kind="solution"; break 2; }
  done
  dir="$(dirname "$dir")"
done

if [ -z "$workspace" ]; then
  dir="$(dirname "$abs_file")"
  while [ "$dir" != "/" ] && [ -n "$dir" ]; do
    for f in "$dir"/*.csproj "$dir"/*.fsproj; do
      [ -f "$f" ] && { workspace="$f"; workspace_kind="project"; break 2; }
    done
    dir="$(dirname "$dir")"
  done
fi

# Compute file path relative to the workspace dir for --include.
if [ -n "$workspace" ]; then
  workspace_dir="$(dirname "$workspace")"
  rel_file="${abs_file#$workspace_dir/}"
else
  workspace_kind="folder"
  workspace="$(dirname "$abs_file")"
  rel_file="$(basename "$abs_file")"
fi

# Lock scoped to the workspace (not the file) — that's the resource that's
# actually expensive to reload, and it's what a concurrent second `dotnet
# format` against the same solution/project would contend on anyway.
log_dir="${CLAUDE_PROJECT_DIR:-$PWD}/.squad/log"
lock_dir="${CLAUDE_PROJECT_DIR:-$PWD}/.squad/.locks"
mkdir -p "$log_dir" "$lock_dir" 2>/dev/null || true
log_file="$log_dir/dotnet-format-$(date -u +%Y-%m-%d).log"
lock_name="dotnet-format-$(printf '%s' "$workspace" | cksum | cut -d' ' -f1)"

(
  ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

  run_format() {
    if [ "$workspace_kind" = "folder" ]; then
      timeout 60s dotnet format --folder "$workspace" --include "$rel_file" \
        >>"$log_file" 2>&1 \
        && echo "$ts ok    folder $workspace $rel_file" >>"$log_file" \
        || echo "$ts fail  folder $workspace $rel_file (exit $?)" >>"$log_file"
    else
      timeout 60s dotnet format "$workspace" --include "$rel_file" \
        >>"$log_file" 2>&1 \
        && echo "$ts ok    $workspace_kind $workspace $rel_file" >>"$log_file" \
        || echo "$ts fail  $workspace_kind $workspace $rel_file (exit $?)" >>"$log_file"
    fi
  }

  if command -v flock >/dev/null 2>&1; then
    # Linux (util-linux flock): block up to 90s waiting for any in-flight
    # format against this same workspace, then run. `9` is an arbitrary
    # free file descriptor held only for the duration of this subshell.
    (
      flock -w 90 9 || exit 0
      run_format
    ) 9>"$lock_dir/$lock_name.lock"
  else
    # No flock (e.g. macOS ships BSD flock, which doesn't support the same
    # invocation) — fall back to an atomic mkdir-based lock. `mkdir` on an
    # existing directory fails atomically, which is what makes this safe
    # against a genuine race between two hook invocations; a plain
    # test-then-mkdir would not be.
    lock_mkdir="$lock_dir/$lock_name.d"
    waited=0
    while ! mkdir "$lock_mkdir" 2>/dev/null; do
      # Stale-lock recovery: if the lock directory is older than 120s, a
      # previous holder almost certainly died without cleaning up (the
      # 60s `timeout` above bounds a healthy run). Reclaim it rather than
      # wait forever.
      if [ -d "$lock_mkdir" ]; then
        # `date -r <path>` (NOT `date -r <epoch-seconds>`) is GNU-only
        # semantics -- round 4 re-review (review-b93b430.md, "four still-
        # open items") flagged this as the exact BSD/macOS bug this whole
        # mkdir-lock branch exists for: BSD/macOS `date -r` takes an epoch
        # SECONDS COUNT as its argument, not a path, so passing
        # `$lock_mkdir` there fails to parse, `|| echo 0` makes lock_age
        # enormous (now minus epoch 0), and every waiter reclaims the lock
        # immediately -- no serialization on the one platform this branch
        # is for. `stat` is used instead, tried in BOTH dialects: BSD/macOS
        # `stat -f %m` first (the platform this fallback targets), then
        # GNU `stat -c %Y` (so this also degrades correctly on a Linux box
        # that genuinely lacks `flock`, the other case that reaches this
        # branch).
        #
        # Each candidate's output is validated as a bare non-negative
        # integer, NOT just "did the command exit non-zero" -- verified
        # live: GNU `stat -f %m <path>` doesn't fail silently on the wrong
        # dialect, it falls back to printing a multi-line filesystem-info
        # block (`File: ... ID: ... Block size: ...`) to STDOUT while
        # still exiting non-zero. A bare `cmd1 2>/dev/null || cmd2
        # 2>/dev/null || echo 0` only silences STDERR, so that whole
        # garbage block would have been captured as part of $lock_mtime,
        # making the `$(( ))` arithmetic below a syntax error and this
        # entire subshell abort -- the mkdir-lock fallback's OWN attempted
        # fix would have wedged the hook harder than the bug it replaced.
        # Regex-validating each candidate before accepting it closes that
        # regardless of which dialect's failure mode looks like.
        lock_mtime="$(stat -f %m "$lock_mkdir" 2>/dev/null)"
        if ! [[ "$lock_mtime" =~ ^[0-9]+$ ]]; then
          lock_mtime="$(stat -c %Y "$lock_mkdir" 2>/dev/null)"
        fi
        if ! [[ "$lock_mtime" =~ ^[0-9]+$ ]]; then
          lock_mtime=0
        fi
        lock_age=$(( $(date +%s) - lock_mtime ))
        if [ "$lock_age" -gt 120 ]; then
          rmdir "$lock_mkdir" 2>/dev/null || true
          continue
        fi
      fi
      waited=$((waited + 1))
      if [ "$waited" -gt 90 ]; then
        echo "$ts skip  lock-timeout $workspace $rel_file" >>"$log_file"
        exit 0
      fi
      sleep 1
    done
    run_format
    rmdir "$lock_mkdir" 2>/dev/null || true
  fi
) </dev/null >/dev/null 2>&1 &

# Detach so the hook returns immediately.
disown 2>/dev/null || true

exit 0
