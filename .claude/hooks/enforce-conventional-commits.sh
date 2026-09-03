#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Validates that `git commit` messages follow Conventional Commits.
#
# Allowed types: feat, fix, docs, refactor, test, perf, security, ci, build, chore
# Allowed shape:  <type>[(<scope>)][!]: <subject>
# Examples:
#   feat: add article publish endpoint
#   fix(auth): reject expired tokens
#   refactor!: rename Article to Post
#
# Detection of "is this a `git commit`, and what did it pass as the
# subject" is delegated to lib/git-commit-detect.sh (argv-level, shared with
# the other three commit-time hooks) — see that file's header for why a
# substring match on "git commit" doesn't work and what compound-command
# forms are and aren't handled.
#
# Message extraction walks the parsed argv looking for the FIRST -m /
# --message / -F / --file occurrence (git itself also treats the first
# -m/-F as the subject source when several are given — later -m flags
# become additional paragraphs, not a replacement subject). This replaces
# a `sed -nE 's/.*-m.../'` extraction that was greedy and validated the
# LAST -m value instead of the first, so
# `git commit -m "fix(auth): reject expired tokens" -m "body"` used to
# validate "body" against the subject pattern and block a legitimate
# commit.
#
# -F/--file: best-effort. If the referenced file is readable at hook time,
# its first non-empty line is validated as the subject, same as git treats
# it. If it isn't (not written yet, outside a readable path), we fail open
# — we'd rather under-block than block on a file we can't see.
#
# `-m "$(...)"` / backtick command substitution (the attribution heredoc
# form this repo's own commit process mandates, e.g.
# `git commit -m "$(cat <<'EOF' ... EOF)"`): the argv-level parser captures
# the literal, unexecuted text of the substitution, not what the shell
# would actually produce at commit time. We deliberately do not evaluate
# it ourselves — running arbitrary shell content extracted from an
# untrusted command string inside a security hook is a worse outcome than
# the message going unvalidated — so this form fails open too. That is a
# conscious trade-off, not an oversight: it's also the sanctioned,
# most-used commit shape in this repo, so failing open on it is "don't
# block the standard case," not "silently allow the common bypass."
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)

set -euo pipefail

payload="$(cat)"
command="$(printf '%s' "$payload" | python3 -c 'import json,sys
try:
    print(json.loads(sys.stdin.read()).get("tool_input",{}).get("command",""))
except Exception:
    print("")
' 2>/dev/null)"

hook_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/git-commit-detect.sh
source "$hook_dir/lib/git-commit-detect.sh"

git_commit_detect "$command"
if [ "$GIT_COMMIT_MATCH" != "1" ]; then
  exit 0
fi

argv=("${GIT_COMMIT_ARGV[@]}")
n=${#argv[@]}

is_amend=0
has_new_message=0
message_source=""   # "m" | "file" | ""
message=""
file_path=""

i=0
while [ "$i" -lt "$n" ]; do
  tok="${argv[$i]}"
  case "$tok" in
    --amend)
      is_amend=1
      ;;
    -m|--message)
      has_new_message=1
      if [ -z "$message_source" ]; then
        message_source="m"
        i=$((i + 1))
        [ "$i" -lt "$n" ] && message="${argv[$i]}"
      fi
      ;;
    --message=*)
      has_new_message=1
      if [ -z "$message_source" ]; then
        message_source="m"
        message="${tok#--message=}"
      fi
      ;;
    -F|--file)
      has_new_message=1
      if [ -z "$message_source" ]; then
        message_source="file"
        i=$((i + 1))
        [ "$i" -lt "$n" ] && file_path="${argv[$i]}"
      fi
      ;;
    --file=*)
      has_new_message=1
      if [ -z "$message_source" ]; then
        message_source="file"
        file_path="${tok#--file=}"
      fi
      ;;
  esac
  i=$((i + 1))
done

# --amend without a new -m/-F/--message is a metadata-only amend (author,
# date, tree, ...) that doesn't touch the subject line. Nothing to validate.
if [ "$is_amend" = "1" ] && [ "$has_new_message" != "1" ]; then
  exit 0
fi

subject=""

if [ "$message_source" = "m" ]; then
  subject="$message"
elif [ "$message_source" = "file" ] && [ -n "$file_path" ]; then
  resolved="$file_path"
  case "$resolved" in
    /*) ;;
    *)
      base="${GIT_COMMIT_REPO_DIR:-${CLAUDE_PROJECT_DIR:-$PWD}}"
      resolved="$base/$resolved"
      ;;
  esac
  if [ -r "$resolved" ]; then
    subject="$(awk 'NF{print; exit}' "$resolved" 2>/dev/null || true)"
  fi
fi

# No -m/-F we could resolve at all (editor-based commit, unreadable -F
# file, ...). Nothing to validate — let it through.
if [ -z "$subject" ]; then
  exit 0
fi

# Unevaluated command substitution in the subject — see header. Fail open.
case "$subject" in
  *'$('*|*'`'*) exit 0 ;;
esac

# Merges and reverts generate their own messages.
case "$subject" in
  "Merge "*|"Revert "*) exit 0 ;;
esac

# Conventional Commits regex. Subject must be non-empty.
pattern='^(feat|fix|docs|refactor|test|perf|security|ci|build|chore)(\([a-z0-9-]+\))?!?: .+'

if ! printf '%s' "$subject" | grep -qE "$pattern"; then
  cat >&2 <<EOF
🚫 Commit message does not follow Conventional Commits.

Got:
  ${subject}

Required shape:
  <type>[(<scope>)][!]: <subject>

Allowed types: feat, fix, docs, refactor, test, perf, security, ci, build, chore
Use ! after the type/scope to mark a breaking change.

Examples:
  feat: add article publish endpoint
  fix(auth): reject expired tokens
  refactor!: rename Article to Post

See .claude/docs/decisions.md (Git Workflow) for the full rule.
EOF
  exit 2
fi

exit 0
