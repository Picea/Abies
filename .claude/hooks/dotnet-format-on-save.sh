#!/usr/bin/env bash
#
# PostToolUse hook for Write|Edit|MultiEdit.
# Runs `dotnet format` on a single C# file after Claude saves it, so the
# codebase stays formatted to the team's `.editorconfig` without manual
# intervention.
#
# Scope (by file extension):
#   .cs          — C# source
#   .csproj      — project files (whitespace + ordering)
#   .props/.targets — MSBuild files
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

# Only fire for C#-shaped files.
case "$file_path" in
  *.cs|*.csproj|*.props|*.targets) ;;
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

# Run dotnet format in the background. We deliberately:
#   - cap runtime via `timeout` (60s is generous for one file),
#   - silence stdout (chatty), keep stderr for genuine errors,
#   - use `nohup` + `&` so Claude's hook return doesn't block on it.
log_dir="${CLAUDE_PROJECT_DIR:-$PWD}/.squad/log"
mkdir -p "$log_dir" 2>/dev/null || true
log_file="$log_dir/dotnet-format-$(date -u +%Y-%m-%d).log"

(
  ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
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
) </dev/null >/dev/null 2>&1 &

# Detach so the hook returns immediately.
disown 2>/dev/null || true

exit 0
