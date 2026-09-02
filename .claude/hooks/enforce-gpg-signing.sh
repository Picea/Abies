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
#   - The user passes `--no-gpg-sign` or `-S` is followed by an empty key id
#     (i.e., they've explicitly chosen to bypass signing for this commit).
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

# Short-circuit on anything that isn't a `git commit` invocation.
case "$command" in
  *"git commit"*) ;;
  *) exit 0 ;;
esac

# Skip merges, reverts, and explicit bypasses.
case "$command" in
  *"git commit -m \"Merge "*|*"git commit -m 'Merge "*) exit 0 ;;
  *"git commit -m \"Revert "*|*"git commit -m 'Revert "*) exit 0 ;;
  *" --no-gpg-sign"*) exit 0 ;;
esac

# Env var escape hatch — note the SQUAD_ALLOW_UNSIGNED check intentionally
# inspects the *parent* environment, not the inline command env, because
# we want this to be a deliberate per-shell decision.
if [ "${SQUAD_ALLOW_UNSIGNED:-}" = "1" ]; then
  exit 0
fi

project_dir="${CLAUDE_PROJECT_DIR:-$PWD}"
cache="$project_dir/.squad/.signing-health"

# The hook supports a fast path: if the SessionStart probe wrote an "ok"
# cache less than 5 minutes ago, trust it and exit immediately. On any other
# cached verdict we re-run the full check to get an accurate, current reason
# string for the error message.
if [ -f "$cache" ]; then
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

# Either no cache, stale cache, or cached non-ok → run the full check.
verdict=""
detail=""
read -r verdict detail <<<"$(python3 - <<'PY'
import os, re, subprocess, sys

def cfg(key):
    try:
        r = subprocess.run(
            ["git", "config", "--get", key],
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
# session can early-out with the same verdict).
if [ -d "$project_dir/.squad" ] && [ -n "$verdict" ]; then
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

Fix the underlying config, then retry the commit. The squad's reference setup:

  git config --global user.signingkey D92532059F0414A3
  git config --global commit.gpgsign true
  git config --global tag.gpgsign true
  git config --global user.name "Maurice Cornelius Gerardus Petrus Peters"
  git config --global user.email "me@mauricepeters.dev"

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
