#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Blocks `git commit` when the staged diff includes files exceeding the
# size threshold.
#
# Default threshold: 5 MB. Override with SQUAD_LARGE_FILE_LIMIT (bytes).
#
# Why size-on-disk and not "added bytes"? Because the failure mode this
# hook prevents is "binary lands in git history and is painful to remove
# later" — and a 50MB binary added in one commit is exactly as bad as a
# 50MB binary that grew across many. We measure the staged blob.
#
# Bypass mechanisms:
#   - SQUAD_ALLOW_LARGE_FILES=1 in the parent shell environment.
#     Use deliberately. If you legitimately need a large binary in the
#     repo (rare; consider git-lfs first), this is the escape hatch.
#   - The commit is a merge or revert.
#
# Files only inspected if they are *added* or *modified* in the staging
# area. Files merely present on disk but not staged are ignored.
#
# Performance: one `git diff --cached --numstat` call plus one `git ls-files`
# per staged file. Fast even on large repos.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

command="$(printf '%s' "$payload" | python3 -c '
import json, sys
try:
    d = json.loads(sys.stdin.read())
    print(d.get("tool_input", {}).get("command", ""))
except Exception:
    print("")
' 2>/dev/null)"

hook_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/git-commit-detect.sh
source "$hook_dir/lib/git-commit-detect.sh"

# Detection of "is this a `git commit`" is delegated to
# lib/git-commit-detect.sh (argv-level, shared with the other three
# commit-time hooks) rather than a substring match on "git commit" — see
# that file's header for the false-positive/false-negative bugs that
# replaces. The staged-file scan below runs against the repo the command
# actually targets ($GIT_COMMIT_REPO_DIR), so `git -C <other-repo> commit`
# is checked against <other-repo>'s staging area, not the hook's own
# working directory.
git_commit_detect "$command"
if [ "$GIT_COMMIT_MATCH" != "1" ]; then
  exit 0
fi

# Skip merges and reverts (checked against the parsed argv, not raw text).
first_message=""
have_first_message=0
i=0
n=${#GIT_COMMIT_ARGV[@]}
while [ "$i" -lt "$n" ]; do
  tok="${GIT_COMMIT_ARGV[$i]}"
  case "$tok" in
    -m|--message)
      if [ "$have_first_message" != "1" ]; then
        have_first_message=1
        i=$((i + 1))
        [ "$i" -lt "$n" ] && first_message="${GIT_COMMIT_ARGV[$i]}"
      fi
      ;;
    --message=*)
      if [ "$have_first_message" != "1" ]; then
        have_first_message=1
        first_message="${tok#--message=}"
      fi
      ;;
  esac
  i=$((i + 1))
done
case "$first_message" in
  "Merge "*|"Revert "*) exit 0 ;;
esac

# Env var escape hatch.
if [ "${SQUAD_ALLOW_LARGE_FILES:-}" = "1" ]; then
  exit 0
fi

# Threshold (bytes). Default 5 MB.
limit="${SQUAD_LARGE_FILE_LIMIT:-5242880}"

# Validate threshold is a positive integer.
if ! printf '%s' "$limit" | grep -qE '^[1-9][0-9]*$'; then
  cat >&2 <<EOF
🚫 SQUAD_LARGE_FILE_LIMIT='$limit' is not a positive integer (bytes).
Fix the env var or unset it to use the 5 MB default.
EOF
  exit 2
fi

# Collect staged files. `git diff --cached --name-only --diff-filter=AM`
# gives us files that are Added (A) or Modified (M) in the index — which
# are the ones whose blob will land in this commit. Deletions are excluded
# (D), as are renames-without-content-change (R) which we'd rather not
# re-flag if the bytes themselves haven't grown.
project_dir="${CLAUDE_PROJECT_DIR:-$PWD}"
target_dir="${GIT_COMMIT_REPO_DIR:-$project_dir}"
cd "$target_dir" 2>/dev/null || exit 0

# If we're not in a git repo, the hook has nothing useful to say.
if ! git rev-parse --git-dir >/dev/null 2>&1; then
  exit 0
fi

# Use NUL-delimited output to be safe with paths containing spaces or
# unusual characters. We pipe directly into mapfile (a bash builtin that
# can hold NULs) rather than going through a $() command substitution
# (which strips them).
declare -a files=()

# Determine whether HEAD exists. On a fresh repo with no commits yet,
# `git diff --cached HEAD` fails; we use `--no-index`-like fallback by
# diffing against the empty tree explicitly.
if git rev-parse --verify --quiet HEAD >/dev/null; then
  while IFS= read -r -d '' f; do
    files+=("$f")
  done < <(git diff --cached --name-only --diff-filter=AM -z 2>/dev/null)
else
  # Empty tree SHA-1 (constant in git for the empty tree).
  empty_tree="$(git hash-object -t tree /dev/null 2>/dev/null || echo "4b825dc642cb6eb9a060e54bf8d69288fbee4904")"
  while IFS= read -r -d '' f; do
    files+=("$f")
  done < <(git diff --cached --name-only --diff-filter=AM -z "$empty_tree" 2>/dev/null)
fi

if [ "${#files[@]}" -eq 0 ]; then
  # Nothing staged for content addition/modification → nothing to inspect.
  exit 0
fi

# Walk each staged file. We measure the *staged blob* size via `git
# cat-file -s :"<path>"` rather than the working-tree size — those can
# differ if the file was modified after `git add`.
violations=""
violation_count=0

for f in "${files[@]}"; do
  # Skip empty entries (defensive).
  [ -z "$f" ] && continue

  # Get the staged blob size in bytes.
  size="$(git cat-file -s ":$f" 2>/dev/null || echo 0)"

  if [ "$size" -gt "$limit" ]; then
    violation_count=$((violation_count + 1))
    # Format size human-readably (always MB resolution; this is for human
    # eyes, not parsing).
    size_mb="$(python3 -c "print(f'{$size/1024/1024:.1f}')" 2>/dev/null || echo "?")"
    limit_mb="$(python3 -c "print(f'{$limit/1024/1024:.1f}')" 2>/dev/null || echo "?")"
    violations+="  • $f  (${size_mb} MB, limit ${limit_mb} MB)"$'\n'
  fi
done

if [ "$violation_count" -eq 0 ]; then
  exit 0
fi

# Trim trailing newline from violations.
violations="${violations%$'\n'}"

cat >&2 <<EOF
🚫 Staged commit contains $violation_count file(s) exceeding the size limit:

$violations

Once a large blob enters git history, removing it requires a history
rewrite (\`git filter-repo\`) — disruptive on a shared repo. Catch it now.

Fix one of:
  • Unstage the offending file:
      git restore --staged <path>
    Then add it to .gitignore if it's a build artifact.
  • If the file *should* be tracked, prefer git-lfs:
      git lfs track "*.<extension>"
      git add .gitattributes <path>
  • Override the threshold for this commit only (rare; better to use lfs):
      SQUAD_LARGE_FILE_LIMIT=$((limit * 4)) git commit ...
  • Last-resort bypass (genuinely auditable use cases only):
      SQUAD_ALLOW_LARGE_FILES=1 git commit ...

Common offenders: BenchmarkArtifacts/, bin/, obj/, node_modules/,
trace.zip from playwright, .pdb files, large fixture data.
EOF
exit 2
