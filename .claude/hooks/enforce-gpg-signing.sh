#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool.
# Blocks `git commit` when GPG signing is misconfigured.
#
# Checks (in order; first failure aborts):
#   1. `gpg` binary on PATH.
#   2. `commit.gpgsign` is true.
#   3. `user.signingkey` is set.
#   4. The signing key's secret half is in the local keyring.
#   5. `user.email` matches at least one UID on the signing key.
#
# Bypass mechanisms (any one allows the commit):
#   - The user passes `--no-gpg-sign` (explicitly opts out of signing for
#     this commit). Note: `-S`/`--gpg-sign` only accepts an *attached*
#     value per git's own option parsing (`-S<keyid>` or
#     `--gpg-sign[=<keyid>]`) — there is no "`-S` followed by a separate,
#     empty key id" form to special-case. An earlier version of this
#     comment claimed one existed; nothing ever implemented it, and it
#     doesn't correspond to real git syntax, so the claim is removed
#     rather than the gap filled.
#   - The env var SQUAD_ALLOW_UNSIGNED is set to 1 (intentional escape hatch
#     for foreign environments — devcontainers, CI, etc.).
#   - The commit is a merge or revert (those generate their own messages and
#     historically have weaker signing-discipline expectations; matches the
#     conventional-commits hook).
#
# Performance: short-circuits on non-commit commands. Reads a cached signing
# verdict from .squad/.signing-health (written by session-context-loader.py
# at SessionStart) when the cache is < 5 minutes old; otherwise re-checks
# from scratch.
#
# Detection of "is this a `git commit`" is delegated to
# lib/git-commit-detect.sh (argv-level, shared with the other three
# commit-time hooks) rather than a substring match on "git commit" — see
# that file's header for the false-positive/false-negative bugs that
# replaces. The signing check itself is queried against the repo the
# command actually targets ($GIT_COMMIT_REPO_DIR / $GIT_COMMIT_GLOBAL_ARGS),
# so `git -C <other-repo> commit` is checked against <other-repo>'s config,
# not the hook's own working directory.
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

git_commit_detect "$command"
if [ "$GIT_COMMIT_MATCH" != "1" ]; then
  exit 0
fi

# Skip merges, reverts, and explicit bypasses (checked against the parsed
# argv, not raw command text).
skip=0
first_message=""
have_first_message=0
for tok in "${GIT_COMMIT_ARGV[@]}"; do
  case "$tok" in
    --no-gpg-sign) skip=1 ;;
  esac
done
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
  "Merge "*|"Revert "*) skip=1 ;;
esac
if [ "$skip" = "1" ]; then
  exit 0
fi

# Env var escape hatch — note the SQUAD_ALLOW_UNSIGNED check intentionally
# inspects the *parent* environment, not the inline command env, because
# we want this to be a deliberate per-shell decision.
if [ "${SQUAD_ALLOW_UNSIGNED:-}" = "1" ]; then
  exit 0
fi

project_dir="${CLAUDE_PROJECT_DIR:-$PWD}"
cache="$project_dir/.squad/.signing-health"

# The SessionStart-populated cache is specific to THIS squad project
# (session-context-loader.py writes it under $CLAUDE_PROJECT_DIR/.squad/).
# It's only trustworthy when the command actually targets this project's
# repo — a `git -C <other-repo> commit` (or --git-dir=<other-repo>/.git)
# targets a different repo's config entirely, so the fast path is skipped
# for those and the full check always runs (still scoped correctly via
# GIT_COMMIT_GLOBAL_ARGS below), and the result isn't written back into
# this project's cache either.
target_dir="${GIT_COMMIT_REPO_DIR:-$project_dir}"
use_project_cache=0
if [ "$target_dir" = "$project_dir" ]; then
  use_project_cache=1
fi

# The hook supports a fast path: if the SessionStart probe wrote an "ok"
# cache less than 5 minutes ago, trust it and exit immediately. On any other
# cached verdict we re-run the full check to get an accurate, current reason
# string for the error message.
if [ "$use_project_cache" = "1" ] && [ -f "$cache" ]; then
  age_seconds="$(python3 -c "
import os, sys, time
try:
    print(int(time.time() - os.stat('$cache').st_mtime))
except Exception:
    print(99999)
" 2>/dev/null)"
  if [ "${age_seconds:-99999}" -lt 300 ]; then
    cached_verdict="$(head -n1 "$cache" 2>/dev/null | cut -d'|' -f1)"
    if [ "$cached_verdict" = "ok" ]; then
      exit 0
    fi
  fi
fi

# Either no cache, stale cache, cached non-ok, or the command targets a
# different repo → run the full check, scoped to the targeted repo via
# GIT_COMMIT_GLOBAL_ARGS (empty when no -C/--git-dir/-c was given, in which
# case `git config` behaves exactly as it always did — the hook's own cwd).
if [ "${#GIT_COMMIT_GLOBAL_ARGS[@]}" -eq 0 ]; then
  global_args_json="[]"
else
  global_args_json="$(printf '%s\n' "${GIT_COMMIT_GLOBAL_ARGS[@]}" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read().splitlines()))')"
fi
export GIT_COMMIT_GLOBAL_ARGS_JSON="$global_args_json"

verdict=""
detail=""
read -r verdict detail <<<"$(python3 - <<'PY'
import json, os, re, subprocess, sys

GLOBAL_ARGS = json.loads(os.environ.get("GIT_COMMIT_GLOBAL_ARGS_JSON", "[]"))

def cfg(key):
    try:
        r = subprocess.run(
            ["git", *GLOBAL_ARGS, "config", "--get", key],
            capture_output=True, text=True, timeout=2,
        )
        return r.stdout.strip() if r.returncode == 0 else ""
    except Exception:
        return ""

# 1. gpg present?
try:
    r = subprocess.run(["gpg", "--version"], capture_output=True, text=True, timeout=2)
    if r.returncode != 0:
        print("unavailable gpg-not-on-PATH"); sys.exit(0)
except FileNotFoundError:
    print("unavailable gpg-not-on-PATH"); sys.exit(0)
except Exception:
    print("unavailable gpg-invocation-failed"); sys.exit(0)

# 2. gpgsign true?
if cfg("commit.gpgsign").lower() != "true":
    print("warn commit.gpgsign-is-not-true"); sys.exit(0)

# 3. signingkey set?
key_id = cfg("user.signingkey")
if not key_id:
    print("warn user.signingkey-is-not-set"); sys.exit(0)

# 4. user.email set?
user_email = cfg("user.email")
if not user_email:
    print("warn user.email-is-not-set"); sys.exit(0)

# 5. key in keyring + UID matches email?
try:
    r = subprocess.run(
        ["gpg", "--list-secret-keys", "--with-colons", key_id],
        capture_output=True, text=True, timeout=3,
    )
    if r.returncode != 0:
        print(f"warn key-{key_id}-not-in-keyring"); sys.exit(0)
except Exception:
    print("warn cannot-query-keyring"); sys.exit(0)

emails = []
for line in r.stdout.splitlines():
    if not line.startswith("uid:"):
        continue
    parts = line.split(":")
    if len(parts) < 10:
        continue
    m = re.search(r"<([^>]+)>", parts[9])
    if m:
        emails.append(m.group(1).lower())
    elif "@" in parts[9]:
        emails.append(parts[9].strip().lower())

if user_email.lower() not in emails:
    print(f"warn email-{user_email}-not-on-key-{key_id}"); sys.exit(0)

print(f"ok key-{key_id}-email-{user_email}")
PY
)"

# Refresh the cache so the next commit in this session is fast (or the next
# session can early-out with the same verdict) — only when the check was
# actually about this project's own repo; never cache another repo's
# verdict under this project's cache file.
if [ "$use_project_cache" = "1" ] && [ -d "$project_dir/.squad" ] && [ -n "$verdict" ]; then
  printf '%s|%s\n' "$verdict" "$detail" > "$cache"
fi

# Allow on `ok`. Block on `warn` and `unavailable` (with different messaging).
case "$verdict" in
  ok)
    exit 0
    ;;
  unavailable)
    cat >&2 <<EOF
🚫 GPG is unavailable but the squad signs every commit.

Reason: ${detail:-unknown}

Fix one of:
  • Install gpg and import your signing key.
  • Set SQUAD_ALLOW_UNSIGNED=1 in this shell to bypass intentionally
    (only do this in foreign environments where signing genuinely
    isn't possible — CI runners that don't import a key, fresh
    devcontainers, etc.).
  • Pass --no-gpg-sign on this specific commit.

Reference setup: .claude/skills/git-advanced/SKILL.md → "Signing commits with GPG".
EOF
    exit 2
    ;;
  warn)
    # Translate machine-readable detail back to a human reason.
    case "$detail" in
      "commit.gpgsign-is-not-true")
        reason="commit.gpgsign is not set to true in your git config." ;;
      "user.signingkey-is-not-set")
        reason="user.signingkey is not set in your git config." ;;
      "user.email-is-not-set")
        reason="user.email is not set in your git config." ;;
      key-*-not-in-keyring)
        key_id="${detail#key-}"; key_id="${key_id%-not-in-keyring}"
        reason="signing key ${key_id} is not in the local GPG keyring." ;;
      email-*-not-on-key-*)
        rest="${detail#email-}"
        email_part="${rest%-not-on-key-*}"
        key_part="${rest##*-not-on-key-}"
        reason="user.email '${email_part}' is not a UID on key ${key_part} (GitHub will mark this commit Unverified)." ;;
      *)
        reason="$detail" ;;
    esac

    cat >&2 <<EOF
🚫 GPG signing is misconfigured.

Reason: ${reason}

Fix the underlying config, then retry the commit. Reference shape (fill in
YOUR OWN key id, name, and the email that matches a UID on that key —
copying another contributor's values here configures a key you don't hold
and this check will then block every commit you make):

  git config --global user.signingkey <YOUR_GPG_KEY_ID>
  git config --global commit.gpgsign true
  git config --global tag.gpgsign true
  git config --global user.name "<Your Name>"
  git config --global user.email "<your-email-that-matches-the-key-UID>"

To bypass for one commit:
  --no-gpg-sign        on the commit itself
  SQUAD_ALLOW_UNSIGNED=1 in this shell (deliberate foreign-env escape hatch)

Reference: .claude/skills/git-advanced/SKILL.md → "Signing commits with GPG".
EOF
    exit 2
    ;;
  *)
    # Defensive: never block on a result we couldn't classify.
    exit 0
    ;;
esac
