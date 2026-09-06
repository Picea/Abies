#!/usr/bin/env bash
#
# PreToolUse hook for the Bash tool. The commit gate.
#
# Refuses, unless .squad/.last-review-verdict records a PASS for the CURRENT
# HEAD:
#
#   git commit        when the staged/committed change touches code paths
#   git push          to a protected branch (main, master, release/*)
#   gh pr merge       always
#
# Rationale. "Never declare your own work complete" was an instruction, and
# the Missing Review Lockout enforced it socially. Self-approval is the
# failure mode that quietly destroys review as an institution, and every
# instance looks reasonable in isolation: small change, reviewer busy,
# obviously works. The lockout made skipping review visible. This makes it
# fail.
#
# The verdict must name the current HEAD. A bare PASS with no commit would
# approve every future change too, which is precisely the failure this exists
# to stop, so `commit: <sha>` on the second line is required and must match.
# .squad/.last-review-verdict is written by reviewer-reconcile and by
# scribe-decision-merger.sh.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR reads the wrong repository's verdict from inside a
# worktree — and still passes any test that has no worktree in it.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Bash",
#     "tool_input": { "command": "..." }
#   }

set -uo pipefail

# 🔴-4 (PR #358 review round 1): every downstream check in this file assumes
# the classifier below actually ran. Before this preamble, a missing/broken
# python3 produced empty stdout indistinguishable from "not a gated command",
# and `[ -z "$parsed" ] && exit 0` treated the two identically -- an
# interpreter that is absent, the wrong version, or crashing on a syntax
# error introduced by an edit silently ALLOWED every commit/push/merge
# instead of refusing. Checked once, here, before anything else runs, so a
# missing interpreter is loud rather than a silent bypass of the whole gate.
command -v python3 >/dev/null 2>&1 || {
  echo "🚫 enforce-review-verdict.sh: python3 is required to classify this command and is not on PATH -- refusing rather than silently letting an unclassified git/gh invocation past the review gate." >&2
  exit 2
}

hook_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export HOOKS_LIB_DIR="$hook_dir/lib"

payload="$(cat)"

# split_simple_commands()/push_destinations() used to be reimplemented here
# byte-identically to block-direct-commits-to-main.sh's own inline copy, with
# nothing asserting the two agreed -- and a WEAKER prefix-skip than the
# shared lib/git_commit_detect.py's own `_strip_transparent_prefix()`: the
# inline copy only skipped a single leading `sudo`/`env`/`NAME=VALUE` token,
# so it missed `env -u FOO git push ...` entirely (that form is now
# recognised correctly, because `_strip_transparent_prefix` knows `env`'s
# own value-taking flags; `sudo -u user git push ...` is a separate,
# documented scope limit of that same stripper and is unchanged either way).
# Both hooks now import the same functions from lib/git_commit_detect.py,
# the way enforce-track-blindness.sh and enforce-review-blindness.sh already
# import lib/path_containment.py (PR #358 review round 2, 🔴-A).
parsed="$(printf '%s' "$payload" | python3 -c '
import json, os, re, sys

sys.path.insert(0, os.environ["HOOKS_LIB_DIR"])
from git_commit_detect import push_destinations

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)
if (d.get("tool_name") or "").strip() != "Bash":
    sys.exit(0)
cmd = (d.get("tool_input") or {}).get("command") or ""
if not cmd:
    sys.exit(0)

kind = ""
if re.search(r"\bgh\s+pr\s+merge\b", cmd):
    kind = "merge"
elif re.search(r"\bgit\s+(?:-[^\s]+\s+)*push\b", cmd):
    kind = "push"
elif re.search(r"\bgit\s+(?:-[^\s]+\s+)*commit\b", cmd):
    kind = "commit"
if not kind:
    sys.exit(0)

push_kind, push_dests = ("", [])
if kind == "push":
    push_kind, push_dests = push_destinations(cmd)

# ⚠️-A (PR #358 review round 2): `git add -A && git commit -m x` and
# `git add B.cs; git commit -m x` both stage a brand-new file WITHIN THE
# SAME Bash call this hook is asked to gate -- at the moment this
# PreToolUse hook runs, the `add` half has not executed yet, so the file is
# still untracked and invisible to both `git diff --cached` and `git diff`
# (tracked-only). Text-matching for a staging verb anywhere in the command
# is enough to flip a flag the bash side uses to ALSO scan untracked files
# via `git status --porcelain`, rather than trusting the union of two
# tracked-only diffs to be complete.
has_add = bool(re.search(r"\bgit\s+(?:-[^\s]+\s+)*(?:add|stage)\b", cmd))

# Fields are joined on \x1e (ASCII Record Separator), not \t: bash classifies
# TAB as "IFS whitespace" regardless of what IFS is set to, so consecutive
# delimiters collapse and empty fields silently vanish (verified live --
# adding a fifth, frequently-empty field here exposed it: for a commit-kind
# line, the two empty PUSH_KIND/PUSH_DESTS fields merged with the
# surrounding tabs into ONE delimiter, so the fifth `read` variable silently
# took the THIRD fields value instead, and HAS_ADD came back empty on every
# commit). \x1e is not whitespace, so `read` keeps every field distinct,
# including empty ones -- same fix shape the blindness hooks already use
# \x1e for.
print("%s\x1e%s\x1e%s\x1e%s\x1e%s" % (kind, (d.get("cwd") or ""), push_kind, ",".join(push_dests), "1" if has_add else "0"))
' 2>/dev/null)"
py_status=$?

if [ "$py_status" -ne 0 ]; then
  echo "🚫 enforce-review-verdict.sh: the command classifier exited non-zero (${py_status}) -- refusing rather than treating a parser failure as \"nothing to gate\"." >&2
  exit 2
fi

[ -z "$parsed" ] && exit 0
IFS=$'\x1e' read -r KIND PAYLOAD_CWD PUSH_KIND PUSH_DESTS HAS_ADD <<<"$parsed"

repo="${PAYLOAD_CWD:-$PWD}"
[ -d "$repo" ] || repo="$PWD"

git -C "$repo" rev-parse --is-inside-work-tree >/dev/null 2>&1 || exit 0

branch="$(git -C "$repo" rev-parse --abbrev-ref HEAD 2>/dev/null || echo "")"

# --- push: only protected branches are gated -------------------------------
# 🔴-3 (PR #358 review round 1): the local branch alone is blind to the
# REFSPEC -- `git push origin HEAD:main` from a feature branch used to exit 0
# here unconditionally. PUSH_KIND/PUSH_DESTS (computed above, argv-level, not
# a text match) name the actual destination(s) when the command carries an
# explicit refspec; the local-branch check below is the fallback ONLY for the
# no-refspec form (`git push`, `git push origin`), where git's own default
# push behaviour targets the current branch.
if [ "$KIND" = "push" ]; then
  case "$PUSH_KIND" in
    ALL)
      : # --all / --mirror: unconditionally protected, fall through to gate.
      ;;
    DESTS)
      protected=0
      IFS=',' read -ra __ic_dests <<< "$PUSH_DESTS"
      for __d in "${__ic_dests[@]}"; do
        case "$__d" in main|master|release/*) protected=1; break ;; esac
      done
      [ "$protected" -eq 0 ] && exit 0
      ;;
    NONE)
      case "$branch" in
        main|master|release/*) ;;
        *) exit 0 ;;
      esac
      ;;
    *)
      # UNKNOWN, or the push classifier never ran (empty PUSH_KIND) -- the
      # destination could not be confidently determined. Fail closed: fall
      # through to the verdict check rather than exit 0 on ambiguity.
      :
      ;;
  esac
fi

# --- commit: only when code-shaped paths are staged -------------------------
# A docs-only or decision-drop commit is not what this gate is for, and a gate
# that fires on every commit is one somebody disables.
#
# 🔴-2 (PR #358 review round 1): `git commit -a`/`--all` and
# `git commit <pathspec>` both stage AT COMMIT TIME, so the index
# (`--cached`) is empty here even though real content is about to be
# committed -- classifying on the index alone let both forms bypass the gate
# entirely. Classify over the UNION of the index and the tracked working-tree
# diff instead: `-a` stages exactly the tracked modifications/deletions this
# union already contains, and a pathspec commit can only ever select a
# SUBSET of a *tracked* file that's already staged or modified.
#
# ⚠️-A (PR #358 review round 2): that union is complete for TRACKED files
# only. It previously claimed completeness outright ("an untracked file
# cannot be committed without `git add` first, which would already show up
# staged") -- true across two separate Bash calls, false within one:
# `git add -A && git commit -m x` and `git add B.cs; git commit -m x` both
# stage a brand-new file in the SAME command this hook gates, and at the
# moment this PreToolUse hook runs, that `add` has not executed yet, so the
# file is still untracked and invisible to both diffs above. HAS_ADD (set
# above from a text match for a staging verb anywhere in the command) adds a
# third source, `git status --porcelain` (untracked files), whenever that
# verb is present -- deliberately over-inclusive (it doesn't try to resolve
# exactly which untracked paths a given `add` invocation would select), on
# the same "fail closed on ambiguity" posture as everything else here.
if [ "$KIND" = "commit" ]; then
  staged="$(git -C "$repo" diff --cached --name-only 2>/dev/null || true)"
  worktree="$(git -C "$repo" diff --name-only 2>/dev/null || true)"
  untracked=""
  if [ "$HAS_ADD" = "1" ]; then
    untracked="$(git -C "$repo" status --porcelain --untracked-files=all 2>/dev/null \
      | sed -n 's/^?? //p')"
  fi
  changed="$staged
$worktree
$untracked"
  if [ -z "$(printf '%s' "$changed" | tr -d '[:space:]')" ]; then
    exit 0
  fi
  code=0
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    case "$f" in
      *.cs|*.csproj|*.js|*.mjs|*.ts|*.tsx|*.jsx|*.py|*.sh|\
      Dockerfile|*/Dockerfile|*.dockerfile|\
      .github/workflows/*|*.yml|*.yaml|*/Migrations/*|\
      package.json|*/package.json|\
      global.json|Directory.Build.props|Directory.Build.targets|Directory.Packages.props|\
      .claude/settings.json|.claude/agents/*.md|\
      appsettings*.json|*/appsettings*.json)
        code=1; break ;;
    esac
  done <<< "$changed"
  [ "$code" -eq 0 ] && exit 0
fi

head_sha="$(git -C "$repo" rev-parse HEAD 2>/dev/null || echo "")"
cache="$repo/.squad/.last-review-verdict"

verdict=""
verdict_sha=""
if [ -f "$cache" ]; then
  verdict="$(head -n1 "$cache" | tr -d '[:space:]')"
  verdict_sha="$(grep -aiE '^commit:' "$cache" 2>/dev/null | head -n1 | sed -E 's/^[Cc]ommit:[[:space:]]*//' | tr -d '[:space:]')"
fi

# For a commit, the verdict necessarily names the commit BEFORE this one --
# the change being committed does not have a sha yet. For push and merge, it
# must name the current HEAD.
want_sha="$head_sha"
what="the current HEAD"
if [ "$KIND" = "commit" ]; then
  what="HEAD (the commit this one builds on)"
fi

problem=""
if [ ! -f "$cache" ]; then
  problem="no verdict has been recorded at all (.squad/.last-review-verdict does not exist)"
elif [ "$verdict" != "PASS" ]; then
  problem="the recorded verdict is ${verdict:-<empty>}, not PASS"
elif [ -z "$verdict_sha" ]; then
  problem="the verdict records no commit, so it cannot be said to apply to this one"
elif [ -n "$want_sha" ] && [ "$verdict_sha" != "$want_sha" ]; then
  problem="the PASS was recorded for ${verdict_sha:0:12}, but ${what} is ${want_sha:0:12}"
fi

[ -z "$problem" ] && exit 0

case "$KIND" in
  commit) action="Committing code" ;;
  push)   action="Pushing to '${branch}'" ;;
  merge)  action="Merging a pull request" ;;
esac

cat >&2 <<EOF
🚫 ${action} requires a PASS verdict for ${what}.

  Problem: ${problem}

Run the review pair and let it finish:

  1. reviewer-blind      → .squad/design/<slug>/08-review-blind.md
  2. reviewer-reconcile  → 09-review-verdict.md, the decision drop,
                           and .squad/.last-review-verdict

Or use /review, which dispatches both in order.

"Never declare your own work complete" used to be an instruction, and the
Missing Review Lockout enforced it socially. Self-approval is the failure mode
that quietly destroys review as an institution, and every instance looks
reasonable in isolation — small change, reviewer busy, obviously works. That is
exactly why this is a gate and not a reminder.

If the user has explicitly overridden the review, they can record that
themselves; you cannot record it on their behalf. Writing a PASS into
.squad/.last-review-verdict to get past this message is forging a verdict, and
it is the one thing this hook exists to make impossible to do quietly.

See .claude/docs/principles-enforcement.md (Missing Review Lockout).
EOF
exit 2
