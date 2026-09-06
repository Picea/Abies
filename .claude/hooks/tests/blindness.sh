#!/usr/bin/env bash
#
# Tests for the four PreToolUse blindness hooks added in WP-2, and for the
# sanctioned git-history channel they point at:
#
#   enforce-track-blindness.sh          Read|Grep|Glob|NotebookRead|mcp__*
#   enforce-review-blindness.sh         Read|Grep|Glob|NotebookRead|mcp__*
#   enforce-review-history-channel.sh   Bash
#   enforce-reviewer-readonly.sh        Write|Edit|MultiEdit|NotebookEdit
#   git-history-namestatus.sh           (not a hook)
#
# Designed to be sourced by run.sh, which owns `report`, `pass` and `fail`.
# It also runs standalone:
#
#   bash .claude/hooks/tests/blindness.sh
#
# What this proves, per hook: a denied path is denied, an allowed path is
# allowed, an unaffected agent is untouched, and — the case that matters —
# a denied path is still denied when the agent is running inside a git
# worktree. That last one is not ceremony. Hook paths do not follow a
# worktree: ${CLAUDE_PROJECT_DIR} stays at the project root where the session
# started while the payload's `cwd` is the worktree root, so a hook that
# resolves against CLAUDE_PROJECT_DIR stops matching inside a worktree and
# still passes every test that has no worktree in it. Each worktree case
# below sets CLAUDE_PROJECT_DIR to the *main* checkout and cwd to the
# worktree, so a CLAUDE_PROJECT_DIR-rooted implementation fails it.

# --- standalone bootstrap ------------------------------------------------
if ! declare -F report >/dev/null 2>&1; then
  set -uo pipefail
  SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
  pass=0
  fail=0
  # Same record as run.sh's report(): a file append survives a subshell, the
  # counter does not. Standalone runs are a supported entry point, so they need
  # the same integrity check -- an earlier version of this fix reached only
  # run.sh, and `bash invariant-chain.sh` still exited 0 on an uncounted FAIL.
  IC_FAIL_LOG="$(mktemp)" || { echo "cannot create failure log" >&2; exit 1; }
  trap 'rm -f "$IC_FAIL_LOG"' EXIT
  report() {
    local name="$1" ok="$2" detail="${3:-}"
    if [ "$ok" = "1" ]; then
      pass=$((pass + 1)); printf 'ok      %s\n' "$name"
    else
      fail=$((fail + 1)); printf '%s\n' "$name" >> "$IC_FAIL_LOG"
      printf 'FAIL    %s -- %s\n' "$name" "$detail"
    fi
  }
  BLINDNESS_STANDALONE=1
fi

HOOKS_DIR="$REPO_ROOT/.claude/hooks"

# SQUAD_NOW (the shadow-window clock
# override enforce-reviewer-readonly.sh reads) is honoured ONLY when
# SQUAD_TEST_HARNESS=1 is ALSO set -- exported here, once, for this whole
# suite, since only a test harness should ever be able to move that clock.
export SQUAD_TEST_HARNESS=1

# ---------------------------------------------------------------------------
# Fires one hook with a synthetic payload. project_dir is what
# CLAUDE_PROJECT_DIR is set to; cwd is what goes in the payload. Passing two
# different values is how the worktree cases are expressed.
# Echoes the exit code; stderr lands in $BLIND_STDERR.
# ---------------------------------------------------------------------------
# `fire` is always called inside $( ), i.e. a subshell, so it cannot hand the
# refusal text back through a variable. It writes stderr to a fixed file on
# disk instead, which the parent reads back via BLIND_STDERR below.
BLIND_ERRFILE=""
BLIND_STDERR=""
fire() {
  local hook="$1" project_dir="$2" cwd="$3" agent="$4" tool="$5" tool_input="$6"
  local rc=0
  printf '{"agent_type":%s,"agent_id":"a-1","session_id":"s-1","cwd":%s,"tool_name":%s,"tool_input":%s}' \
    "$(json_str "$agent")" "$(json_str "$cwd")" "$(json_str "$tool")" "$tool_input" \
    | CLAUDE_PROJECT_DIR="$project_dir" bash "$HOOKS_DIR/$hook" >/dev/null 2>"$BLIND_ERRFILE" || rc=$?
  printf '%s' "$rc"
}

json_str() { python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$1"; }

# $1 label, $2 expected rc, $3 actual rc, $4 substring expected in stderr (or "")
expect() {
  local label="$1" want="$2" got="$3" needle="${4:-}"
  BLIND_STDERR="$(cat "$BLIND_ERRFILE" 2>/dev/null || true)"
  if [ "$got" != "$want" ]; then
    report "$label" 0 "expected exit $want, got $got${BLIND_STDERR:+ -- stderr: ${BLIND_STDERR%%$'\n'*}}"
    return
  fi
  if [ -n "$needle" ]; then
    case "$BLIND_STDERR" in
      *"$needle"*) report "$label" 1 ;;
      *) report "$label" 0 "exit $got was right but the refusal did not mention '$needle' -- got: ${BLIND_STDERR:-<empty>}" ;;
    esac
    return
  fi
  report "$label" 1
}

# ---------------------------------------------------------------------------
# A real git repo with a real worktree. Not simulated: the whole point of the
# worktree cases is that `cwd` is a genuinely different directory from
# CLAUDE_PROJECT_DIR, containing the same tracked files.
# ---------------------------------------------------------------------------
BLIND_TMP="$(mktemp -d)"
MAIN_CO="$BLIND_TMP/main"
ELSEWHERE="$BLIND_TMP/elsewhere"
BLIND_ERRFILE="$BLIND_TMP/stderr.log"
WORKTREE=""
mkdir -p "$ELSEWHERE"

mkdir -p "$MAIN_CO/.claude/docs" "$MAIN_CO/.claude/agent-memory/critic" \
         "$MAIN_CO/.claude/skills/beast-mode-design" \
         "$MAIN_CO/.squad/design/demo" "$MAIN_CO/.squad/decisions/inbox" \
         "$MAIN_CO/src"
printf '# Decisions\n'  > "$MAIN_CO/.claude/docs/decisions.md"
printf '# Tech stack\n' > "$MAIN_CO/.claude/docs/tech-stack.md"
printf '# Memory\n'     > "$MAIN_CO/.claude/agent-memory/critic/MEMORY.md"
printf '# Scope\n'      > "$MAIN_CO/.squad/design/demo/00-scope.md"
printf '# Knowledge\n'  > "$MAIN_CO/.squad/design/demo/00-knowledge.md"
printf '# Track A\n'    > "$MAIN_CO/.squad/design/demo/01-track-a.md"
printf '# Track B\n'    > "$MAIN_CO/.squad/design/demo/02-track-b.md"
printf '# Skill\n'      > "$MAIN_CO/.claude/skills/beast-mode-design/SKILL.md"
printf 'class Order {}\n' > "$MAIN_CO/src/Order.cs"

# Glob-composition fixtures: a plain "docs" dir (a Glob `path` scope with no
# denied content of its own) and "src/deep" (a deeper legitimate scope),
# used by the Glob-composition assertions below -- `path` alone must not
# suppress containment of a climbing `pattern`. (This fix was referenced by
# the internal shorthand "T-012" before a real row existed for it.
# docs/security/threat-model.md, TM-015, Trust Boundary 6 -- "Dreamer/
# reviewer blindness boundary" -- is now that row; its Test column cites
# the `[T-012]`-tagged cases below by that label, which is why they keep
# it rather than being renamed. PR #358 review round 2, ⚠️-C registered the
# citation gap; security-expert closed it with TM-015 the same round.)
mkdir -p "$MAIN_CO/docs" "$MAIN_CO/src/deep" "$MAIN_CO/other" "$MAIN_CO/.claude/agents"
printf '# Readme\n' > "$MAIN_CO/docs/readme.md"
printf '# devops\n' > "$MAIN_CO/.claude/agents/devops.md"

# A second, nested full checkout under .claude/worktrees/ -- this repo's own
# worktree layout (see .gitignore) recreates every tracked path again inside
# one. Used by the "worktree copy of a denied file" assertions below, which
# pin the one thing that actually regressed.
mkdir -p "$MAIN_CO/.claude/worktrees/agent-x/.claude/docs" \
         "$MAIN_CO/.claude/worktrees/agent-x/.squad/design/y" \
         "$MAIN_CO/.claude/worktrees/agent-x/src"
printf '# Decisions (nested)\n' > "$MAIN_CO/.claude/worktrees/agent-x/.claude/docs/decisions.md"
printf '# Plan (nested)\n'      > "$MAIN_CO/.claude/worktrees/agent-x/.squad/design/y/plan.md"
printf 'class Order {}\n'       > "$MAIN_CO/.claude/worktrees/agent-x/src/Order.cs"

# Two ordinary directories that are *not* nested checkouts of anything --
# they merely happen to be named like a repo root. Used by the accepted
# false-positive-class assertions below: the
# trailing-run fallback cannot tell these apart from a real second copy of
# the tree, by path alone, so it refuses them too.
mkdir -p "$MAIN_CO/tests/fixtures/.claude" "$MAIN_CO/docs/examples/.squad"

if git -C "$MAIN_CO" init -q 2>/dev/null \
   && git -C "$MAIN_CO" -c user.email=t@t -c user.name=t add -A 2>/dev/null \
   && git -C "$MAIN_CO" -c user.email=t@t -c user.name=t -c commit.gpgsign=false \
        commit -qm "fixture" 2>/dev/null \
   && git -C "$MAIN_CO" worktree add -q -b wt "$BLIND_TMP/wt" 2>/dev/null; then
  WORKTREE="$BLIND_TMP/wt"
else
  report "worktree fixture: created" 0 "git init/worktree failed -- the worktree cases below cannot run"
fi

# ===========================================================================
# enforce-track-blindness.sh
# ===========================================================================
H=enforce-track-blindness.sh

expect "track-blindness: Track A denied the decision register" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".claude/docs/decisions.md"}')" \
  "may not read the decision register"

expect "track-blindness: Track A denied 00-knowledge.md" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".squad/design/demo/00-knowledge.md"}')" \
  "knowledge scan"

expect "track-blindness: Track A denied Track B's artifact" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".squad/design/demo/02-track-b.md"}')" \
  "Track B"

expect "track-blindness: Track A denied another agent's MEMORY.md" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".claude/agent-memory/critic/MEMORY.md"}')" \
  "persistent memory"

expect "track-blindness: Track A denied a Grep rooted in agent-memory/" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"state","path":".claude/agent-memory"}')" ""

expect "track-blindness: Track A allowed 00-scope.md" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".squad/design/demo/00-scope.md"}')" ""

expect "track-blindness: Track A allowed the codebase" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":"src/Order.cs"}')" ""

expect "track-blindness: Track B denied Track A's artifact (symmetry)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Read '{"file_path":".squad/design/demo/01-track-a.md"}')" \
  "mutually blind"

expect "track-blindness: Track B allowed the decision register" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Read '{"file_path":".claude/docs/decisions.md"}')" ""

expect "track-blindness: an unaffected agent passes through" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" realist Read '{"file_path":".claude/docs/decisions.md"}')" ""

expect "track-blindness: an MCP read tool is covered too" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles mcp__semgrep__semgrep_scan '{"paths":[".claude/docs/decisions.md"]}')" ""

# --- containment: one rule, "refuse a search whose effective root is at or
# above a denied path." No path at all is the root-of-everything instance of
# that rule (cwd is above everything), not a separate case -- so the same
# boundary is checked at the denied path itself, its parent, the repo root,
# a real sibling that only shares a string prefix, and a legitimate path
# below the denied areas. ---

expect "track-blindness: Track A denied a Grep with no path at all (root = cwd)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","output_mode":"content"}')" \
  "Scope your search to a path below the denied paths"

expect "track-blindness: Track A denied a path-less Grep even without output_mode" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret"}')" \
  "may not read the decision register"

expect "track-blindness: Track A denied a path-less Glob" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"**/*.md"}')" \
  "Scope your search to a path below the denied paths"

expect "track-blindness: an mcp__* call with no path-shaped argument at all is allowed, not refused" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles mcp__learn__search '{"question":"how do worktrees work"}')" ""

expect "track-blindness: an mcp__* call whose leaf IS a denied path is still refused (defence in depth)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles mcp__semgrep__semgrep_scan '{"paths":["","."]}')" \
  "may not read the decision register"

expect "track-blindness: Track A denied path=. (repo root -- same rule, not a special case)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":".","output_mode":"content"}')" \
  "Scope your search to a path below the denied paths"

expect "track-blindness: Track A denied path=.claude (real ancestor directory, not a deny-list entry itself)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":".claude","output_mode":"content"}')" \
  "may not read the decision register"

expect "track-blindness: Track A denied path=.claude/docs (the denied file's parent)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":".claude/docs","output_mode":"content"}')" \
  "may not read the decision register"

expect "track-blindness: Track A still denied the exact denied path" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"anything","path":".claude/docs/decisions.md","output_mode":"content"}')" \
  "may not read the decision register"

expect "track-blindness: Track A allowed a real sibling that only shares a string prefix (.claude/skills, not .claude/docs)" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"anything","path":".claude/skills","output_mode":"content"}')" ""

expect "track-blindness: Track A allowed an explicitly-scoped Grep of a permitted path" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"class","path":"src"}')" ""

# --- Grep's `pattern` is content, never a path: the two most likely
# searches in this repo -- for the literal text ".claude", and for "."
# (matches any line) -- must stay legitimate as long as `path` itself does
# not reach a denied path. Glob has no separate scope argument when `path`
# is absent -- its `pattern` *is* the scope, so a wildcard-first pattern is
# "no scope" and a pattern with a literal prefix is scoped by that prefix,
# exactly as if `path` had named it. ---

expect "track-blindness: Track A allowed Grep(pattern=\".claude\", path=\"src\") -- pattern is content, not a path" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":".claude","path":"src"}')" ""

expect "track-blindness: Track A allowed Grep(pattern=\".\", path=\"src\") -- matches any line, must stay usable" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":".","path":"src"}')" ""

expect "track-blindness: Track A allowed a Grep whose pattern is itself a denied path string, scope is legitimate" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":".claude/docs/decisions.md","path":"src"}')" ""

expect "track-blindness: Track A denied Grep(pattern=\".\") with no path -- pattern never substitutes for scope" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"."}')" \
  "may not read the decision register"

expect "track-blindness: Track A allowed a Glob scoped by pattern alone, no path (src/**/*.cs)" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"src/**/*.cs"}')" ""

expect "track-blindness: Track A denied a Glob scoped by pattern alone into a denied area, no path" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":".claude/docs/*.md"}')" \
  "may not read the decision register"

# --- [T-012] Glob-composition fix (docs/security/threat-model.md, TM-015,
# Trust Boundary 6 -- see enforce-track-blindness.sh's header for the
# citation history): Glob's effective root is the composition of `path` and
# `pattern`'s own derived root, ALWAYS -- an innocuous `path` must not
# suppress containment of a climbing `pattern`. Two independent fixes: (1)
# compose path with glob_root(pattern) instead of using `path` alone; (2)
# an undetermined glob_root (a
# brace-group, T-008) escalates to cwd rather than silently no-op-ing into
# `path` unchanged (D20). ---

expect "track-blindness: [T-012] a climbing pattern refuses even with an innocuous path -- path no longer suppresses pattern" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"../../.claude/docs/**","path":"src/deep"}')" \
  "may not read the decision register"

# glob_root used to return at the
# first wildcard segment and never scan past it, so a `..` or brace group
# BEHIND that point was invisible. `*/../../.claude/docs/*.md` genuinely
# resolves to real denied files (confirmed with bash and python glob) and
# was allowed; these pin the fix, not the symptom. Both of these are Case
# 2 (structural, shape-based) refusals, not genuine reach -- the
# unclassifiable-pattern branch has its own reporting path and message. ---

expect "track-blindness: climbing-after-wildcard refuses (not just a literal .. prefix)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"*/../../.claude/docs/*.md","path":"docs"}')" \
  "shape can't be classified"

expect "track-blindness: climbing-after-wildcard refuses with no path at all too" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"src/*/../../.claude/docs/*.md"}')" \
  "shape can't be classified"

expect "track-blindness: [T-012] the grouped-pattern evasion also refuses (fix 2 / D20, not just fix 1)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"{../.claude/docs,x}/**","path":"docs"}')" \
  "shape can't be classified"

expect "track-blindness: brace-behind-wildcard evasion refuses too, not just brace-at-position-zero (widens D20)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"**/*.{js,ts}"}')" \
  "shape can't be classified"

expect "track-blindness: brace-behind-wildcard evasion refuses even with an innocuous path (the exact allow the reviewer measured)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"**/*.{js,ts}","path":"src"}')" \
  "shape can't be classified"

expect "track-blindness: [T-012] benign control still allows -- path=docs, pattern=*.md (no escape)" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"*.md","path":"docs"}')" ""

expect "track-blindness: [T-012] benign control still allows -- no-escape recursive pattern under path" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"**/*.md","path":"docs"}')" ""

expect "track-blindness: benign control still allows -- path=.claude/agents, pattern=*.md" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"*.md","path":".claude/agents"}')" ""

expect "track-blindness: [accepted false-positive, not desired] D20 escalation refuses a grouped pattern under path even when neither branch actually climbs" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"{docs,other}/**","path":"src"}')" \
  "shape can't be classified"

expect "track-blindness: [message] Case 1 (genuine reach) uses the reach-not-searching wording" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"../../.claude/docs/**","path":"src/deep"}')" \
  "this refuses the reach, not the searching"

expect "track-blindness: Track B (also governed) climbing-after-wildcard refuses to its own artifact through a worktree-shaped route" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Glob '{"pattern":"*/../../.squad/design/demo/01-track-a.md","path":"docs"}')" \
  "shape can't be classified"

expect "track-blindness: [T-012] Track B (also governed) protected by the composed-scope fix, not just Track A" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Glob '{"pattern":"../../.squad/design/demo/01-track-a.md","path":"src/deep"}')" \
  "mutually blind"

# --- The arbitrary-label defect the ux-expert flagged: a Case 2 (structural, shape-based) refusal must never fall
# through the shared denied-path `hit` reporting path, which would name a
# denied label this call never touched. Checked directly on stderr content,
# not just via `expect`'s single-needle check, so both "the right message
# fired" and "the wrong message did not" are pinned in one place. ---
rc="$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":"{a,b}/**","path":"src"}')"
BLIND_STDERR="$(cat "$BLIND_ERRFILE" 2>/dev/null || true)"
case "$BLIND_STDERR" in
  *"may not read"*)
    report "track-blindness: [message] structural Glob refusal (Case 2) names no denied-path label" 0 \
      "message wrongly contains 'may not read' -- got: ${BLIND_STDERR%%$'\n'*}"
    ;;
  *"shape can't be classified"*)
    report "track-blindness: [message] structural Glob refusal (Case 2) names no denied-path label" 1
    ;;
  *)
    report "track-blindness: [message] structural Glob refusal (Case 2) names no denied-path label" 0 \
      "unexpected message shape (rc=$rc) -- got: ${BLIND_STDERR:-<empty>}"
    ;;
esac

# --- root resolution: `..`, `~`, and an absolute path above the repo are
# all "at or above cwd", same as "." -- and must be resolved against cwd,
# not compared as literal strings, or ".." (strictly more "above" than ".")
# would slip through where "." is caught. ---

expect "track-blindness: Track A denied path=\"..\" (strictly more above than \".\", which already refuses)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":"..","output_mode":"content"}')" \
  "may not read the decision register"

expect "track-blindness: Track A denied an absolute path above the repo" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":"'"$BLIND_TMP"'","output_mode":"content"}')" \
  "may not read the decision register"

expect "track-blindness: Track A denied path=\"~\" when cwd genuinely descends from home" 2 \
  "$(HOME="$BLIND_TMP" fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":"~","output_mode":"content"}')" \
  "may not read the decision register"

# --- .claude/worktrees/ IS now special-cased with a deny-list entry, for
# both agents this hook governs (PR #358 review round 2, finding 🔴-B). An
# earlier revision of this comment recorded that a coarse deny entry had
# been tried once before and "got it wrong in both directions at once" --
# but round 2 demonstrated, by execution against a real registered
# worktree, that the trailing-segment-run restoration alone (below) does
# NOT close the *ancestor* direction: `Grep(path=".claude/worktrees")` and
# `Glob(".claude/worktrees/**/decisions.md")` both exited 0, reaching every
# path on Track A's deny list through the nested checkout. The restoration
# still matters -- it is what catches an EXACT/DESCENDANT worktree copy of
# a denied file at whatever depth it recurs, the same way it catches a
# foreign checkout's copy in the `elsewhere` branch -- but it was never
# going to catch a query rooted coarsely AT the worktree directory itself,
# because nothing on either deny list named that directory. The new
# `(".claude/worktrees/**", ...)` entry closes exactly that gap. Its
# accepted collateral cost: a read rooted exactly at plain `.claude` is now
# refused for `dreamer-informed` too (see the flipped assertion below) --
# `.claude` is a real ancestor of `.claude/worktrees/**`, the same way it is
# already a real ancestor of `.claude/docs/decisions.md` for
# `dreamer-first-principles`. The ancestor direction through a TRUE SIBLING
# checkout -- the layout `git-advanced/SKILL.md` also documents, outside
# this checkout entirely -- stays open; there is no fixed path segment to
# deny for a location that is not, by that layout's own design, under this
# tree at all. ---

expect "track-blindness: Track A refused a Read of a worktree copy of decisions.md (now caught by the coarse .claude/worktrees/** entry itself -- it matches at true position before the trailing-run fallback that used to be the only thing catching this ever runs)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".claude/worktrees/agent-x/.claude/docs/decisions.md"}')" \
  "worktree checkout"

expect "track-blindness: [🔴-B] Track A denied a coarse Grep rooted exactly at .claude/worktrees (the reviewer's exact probe -- was exit 0)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":".claude/worktrees","output_mode":"content"}')" \
  "worktree checkout"

expect "track-blindness: [🔴-B] Track A denied a Glob rooted at .claude/worktrees/**/decisions.md (the reviewer's exact probe -- was exit 0)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Glob '{"pattern":".claude/worktrees/**/decisions.md"}')" \
  "worktree checkout"

expect "track-blindness: [🔴-B] Track A denied a Grep rooted exactly at the agent directory one level below .claude/worktrees" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":".claude/worktrees/agent-x","output_mode":"content"}')" \
  "worktree checkout"

expect "track-blindness: [🔴-B] Track B (also governed) denied the same coarse .claude/worktrees Grep" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Grep '{"pattern":"anything","path":".claude/worktrees","output_mode":"content"}')" \
  "worktree checkout"

expect "track-blindness: [accepted collateral cost of 🔴-B] Track B now refused a coarse Grep rooted exactly at plain .claude (was allowed before; .claude is a real ancestor of .claude/worktrees/**)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Grep '{"pattern":"anything","path":".claude","output_mode":"content"}')" \
  "worktree checkout"

expect "track-blindness: [🔴-B collateral is narrow] Track B still allowed a Grep of .claude/skills -- a real sibling of .claude/worktrees, not an ancestor of it" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Grep '{"pattern":"anything","path":".claude/skills","output_mode":"content"}')" ""

# --- mcp__*: no fixed schema, so a call with no path-shaped argument at
# all must be allowed (it may have no path concept whatsoever), while a
# leaf that IS a real denied path is still caught -- deliberately, not by
# requiring every mcp__* call to declare a scope it may not have. ---

expect "track-blindness: an mcp__* call with no path concept is allowed for Track A too" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles mcp__learn__search '{"question":"anything"}')" ""

expect "track-blindness: Track B (also governed) denied a path-less Grep" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Grep '{"pattern":"anything","output_mode":"content"}')" \
  "Scope your search to a path below the denied paths"

expect "track-blindness: Track B denied a Grep rooted above its denied artifact (.squad/design)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-informed Grep '{"pattern":"anything","path":".squad/design","output_mode":"content"}')" \
  "mutually blind"

expect "track-blindness: an agent this hook does not govern is unaffected by a path-less Grep" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" realist Grep '{"pattern":"secret","output_mode":"content"}')" ""

expect "track-blindness: an agent this hook does not govern is unaffected by path=.claude" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" realist Grep '{"pattern":"secret","path":".claude","output_mode":"content"}')" ""

# --- Accepted false-positive classes. Neither of these is a desired behaviour to protect -- each pins a
# known, deliberate cost of the trailing-run fallback so it stays visible
# and documented rather than being rediscovered as a bug report the day it
# is first hit. If either mechanism is ever narrowed to stop paying this
# cost, these are expected to flip to the opposite exit code; that is a
# decision to make deliberately, not a silent behaviour change to catch by
# accident. ---

expect "track-blindness: [accepted false-positive, not desired] an mcp__* free-text parameter that merely contains a denied-looking trailing segment refuses in full" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles mcp__learn__search '{"question":"check foo/.claude/docs"}')" \
  "may not read the decision register"

expect "track-blindness: [accepted false-positive, not desired] tests/fixtures/.claude is refused -- it is not a nested checkout, just named like one" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Grep '{"pattern":"secret","path":"tests/fixtures/.claude","output_mode":"content"}')" \
  "may not read the decision register"

if [ -n "$WORKTREE" ]; then
  expect "track-blindness: still denied from inside a worktree (cwd, not CLAUDE_PROJECT_DIR)" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" dreamer-first-principles Read '{"file_path":".claude/docs/decisions.md"}')" \
    "may not read the decision register"

  expect "track-blindness: worktree-absolute path is denied too" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" dreamer-first-principles Read "$(python3 -c 'import json,sys;print(json.dumps({"file_path":sys.argv[1]+"/.squad/design/demo/00-knowledge.md"}))' "$WORKTREE")")" \
    "knowledge scan"

  expect "track-blindness: the codebase is still readable from a worktree" 0 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" dreamer-first-principles Read '{"file_path":"src/Order.cs"}')" ""

  expect "track-blindness: a path-less Grep is still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" dreamer-first-principles Grep '{"pattern":"secret","output_mode":"content"}')" \
    "Scope your search to a path below the denied paths"

  expect "track-blindness: the coarse path=.claude is still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" dreamer-first-principles Grep '{"pattern":"secret","path":".claude","output_mode":"content"}')" \
    "may not read the decision register"
fi

# ===========================================================================
# enforce-review-blindness.sh
# ===========================================================================
H=enforce-review-blindness.sh

expect "review-blindness: reviewer-blind denied .squad/design/" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Read '{"file_path":".squad/design/demo/04-realist-plan.md"}')" \
  "may not read .squad/design/"

expect "review-blindness: reviewer-blind denied a Glob into .squad/design/" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"**/*.md","path":".squad/design"}')" ""

expect "review-blindness: reviewer-blind allowed the source it reviews" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Read '{"file_path":"src/Order.cs"}')" ""

expect "review-blindness: reviewer-reconcile passes through" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Read '{"file_path":".squad/design/demo/04-realist-plan.md"}')" ""

# --- containment: same one-rule shape as track-blindness -- exact, parent,
# repo root, a real sibling that only shares a string prefix, and a
# legitimate path stay distinguished by the segment-aware containment test,
# not by a separate "was a path given at all" pre-check. ---

expect "review-blindness: reviewer-blind denied a Grep with no path at all (root = cwd)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","output_mode":"content"}')" \
  "Scope your search to a path below the denied paths"

expect "review-blindness: reviewer-blind denied a path-less Glob" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"**/*.md"}')" \
  "Scope your search to a path below the denied paths"

expect "review-blindness: reviewer-blind denied path=. (repo root)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":".","output_mode":"content"}')" \
  "Scope your search to a path below the denied paths"

expect "review-blindness: reviewer-blind denied path=.squad (real ancestor of .squad/design)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":".squad","output_mode":"content"}')" \
  "may not read .squad/design/"

expect "review-blindness: reviewer-blind denied the exact denied root" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"anything","path":".squad/design","output_mode":"content"}')" \
  "may not read .squad/design/"

expect "review-blindness: reviewer-blind allowed a real sibling that only shares a string prefix (.squad/decisions, not .squad/design)" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"anything","path":".squad/decisions","output_mode":"content"}')" ""

expect "review-blindness: reviewer-blind allowed an explicitly-scoped Grep of a permitted path" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"class","path":"src"}')" ""

# --- pattern is content, not a path (Grep), and is the scope when path is
# absent (Glob) -- same distinction as track-blindness. ---

expect "review-blindness: reviewer-blind allowed Grep(pattern=\".squad/design\", path=\"src\") -- pattern is content" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":".squad/design","path":"src"}')" ""

expect "review-blindness: reviewer-blind allowed Grep(pattern=\".\", path=\"src\")" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":".","path":"src"}')" ""

expect "review-blindness: reviewer-blind denied Grep(pattern=\".\") with no path -- pattern never substitutes for scope" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"."}')" \
  "may not read .squad/design/"

expect "review-blindness: reviewer-blind allowed a Glob scoped by pattern alone, no path (src/**/*.cs)" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"src/**/*.cs"}')" ""

expect "review-blindness: reviewer-blind denied a Glob scoped by pattern alone into .squad/design, no path" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":".squad/design/*.md"}')" \
  "may not read .squad/design/"

# --- [T-012] Glob-composition fix (docs/security/threat-model.md, TM-015,
# Trust Boundary 6; see the track-blindness section's comment above for the
# citation history): same composed-scope fix as track-blindness. ---

expect "review-blindness: [T-012] a climbing pattern refuses even with an innocuous path -- path no longer suppresses pattern" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"../.squad/design/**/*.md","path":"docs"}')" \
  "may not read .squad/design/"

# --- Same whole-pattern-scan fix as track-blindness -- see that
# section's comment for the full reasoning. ---

expect "review-blindness: climbing-after-wildcard refuses (not just a literal .. prefix)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"*/../../.squad/design/**","path":"docs"}')" \
  "shape can't be classified"

expect "review-blindness: climbing-after-wildcard refuses with no path at all too" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"src/*/../../.squad/design/**"}')" \
  "shape can't be classified"

expect "review-blindness: [T-012] the grouped-pattern evasion also refuses (fix 2 / D20, not just fix 1)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"{../.squad/design,x}/**","path":"docs"}')" \
  "shape can't be classified"

expect "review-blindness: brace-behind-wildcard evasion refuses too, not just brace-at-position-zero (widens D20)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"**/*.{js,ts}"}')" \
  "shape can't be classified"

expect "review-blindness: brace-behind-wildcard evasion refuses even with an innocuous path (the exact allow the reviewer measured)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"**/*.{js,ts}","path":"src"}')" \
  "shape can't be classified"

expect "review-blindness: [T-012] benign control still allows -- path=docs, pattern=*.md (no escape)" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"*.md","path":"docs"}')" ""

expect "review-blindness: [T-012] benign control still allows -- no-escape recursive pattern under path" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"**/*.md","path":"docs"}')" ""

expect "review-blindness: benign control still allows -- path=.claude/agents, pattern=*.md" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"*.md","path":".claude/agents"}')" ""

expect "review-blindness: [accepted false-positive, not desired] D20 escalation refuses a grouped pattern under path even when neither branch actually climbs" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"{docs,other}/**","path":"src"}')" \
  "shape can't be classified"

expect "review-blindness: [message] Case 1 (genuine reach) uses the reach-not-searching wording" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"../.squad/design/**/*.md","path":"docs"}')" \
  "this refuses the reach, not the searching"

# --- Same arbitrary-label check as track-blindness -- see that section's
# comment. ---
rc="$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":"{a,b}/**","path":"src"}')"
BLIND_STDERR="$(cat "$BLIND_ERRFILE" 2>/dev/null || true)"
case "$BLIND_STDERR" in
  *"may not read .squad/design/"*)
    report "review-blindness: [message] structural Glob refusal (Case 2) names no denied-path label" 0 \
      "message wrongly contains 'may not read .squad/design/' -- got: ${BLIND_STDERR%%$'\n'*}"
    ;;
  *"shape can't be classified"*)
    report "review-blindness: [message] structural Glob refusal (Case 2) names no denied-path label" 1
    ;;
  *)
    report "review-blindness: [message] structural Glob refusal (Case 2) names no denied-path label" 0 \
      "unexpected message shape (rc=$rc) -- got: ${BLIND_STDERR:-<empty>}"
    ;;
esac

# --- root resolution: .., ~, and an absolute path above the repo. ---

expect "review-blindness: reviewer-blind denied path=\"..\"" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":"..","output_mode":"content"}')" \
  "may not read .squad/design/"

expect "review-blindness: reviewer-blind denied an absolute path above the repo" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":"'"$BLIND_TMP"'","output_mode":"content"}')" \
  "may not read .squad/design/"

expect "review-blindness: reviewer-blind denied path=\"~\" when cwd genuinely descends from home" 2 \
  "$(HOME="$BLIND_TMP" fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":"~","output_mode":"content"}')" \
  "may not read .squad/design/"

# --- .claude/worktrees/ IS now special-cased with a deny-list entry, for
# the same reason as track-blindness -- see that section for the fuller
# comment (PR #358 review round 2, finding 🔴-B). The trailing-run
# restoration below still closes the exact/descendant direction; the new
# coarse `(".claude/worktrees/**", ...)` entry additionally closes the
# ancestor direction the restoration never reached, verified by execution
# against a real registered worktree (`Grep(path=".claude/worktrees")` and
# `Glob(".claude/worktrees/**/*.md")` both used to exit 0). Its accepted
# collateral cost: reading a builder's source through a copy of it nested
# inside another agent's worktree is now refused too, where it previously
# was not -- reviewer-blind reviews the same source from its own cwd. ---

expect "review-blindness: reviewer-blind refused a Read of a worktree copy of a denied .squad/design file (now caught by the coarse .claude/worktrees/** entry itself -- it matches at true position before the trailing-run fallback that used to be the only thing catching this ever runs)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Read '{"file_path":".claude/worktrees/agent-x/.squad/design/y/plan.md"}')" \
  "worktree checkout"

expect "review-blindness: [accepted collateral cost of 🔴-B] reviewer-blind now refused reading a builder's source through another agent's worktree (was allowed before this round; reviewer-blind reviews the same source from its own cwd instead)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Read '{"file_path":".claude/worktrees/agent-x/src/Order.cs"}')" \
  "worktree checkout"

expect "review-blindness: [🔴-B] reviewer-blind denied a coarse Grep rooted exactly at .claude/worktrees (the reviewer's exact probe -- was exit 0)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":".claude/worktrees","output_mode":"content"}')" \
  "worktree checkout"

expect "review-blindness: [🔴-B] reviewer-blind denied a Glob rooted at .claude/worktrees/**/*.md (the reviewer's exact probe -- was exit 0)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Glob '{"pattern":".claude/worktrees/**/*.md"}')" \
  "worktree checkout"

# --- mcp__*: a call with no path concept at all must be allowed. ---

expect "review-blindness: an mcp__* call with no path concept is allowed" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind mcp__learn__search '{"question":"anything"}')" ""

expect "review-blindness: reviewer-reconcile (unaffected agent) passes a path-less Grep" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Grep '{"pattern":"plan","output_mode":"content"}')" ""

expect "review-blindness: reviewer-reconcile (unaffected agent) passes path=.squad" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Grep '{"pattern":"plan","path":".squad","output_mode":"content"}')" ""

# --- Accepted false-positive class --
# not a desired behaviour, see the matching comment in the track-blindness
# section above. ---

expect "review-blindness: [accepted false-positive, not desired] docs/examples/.squad is refused -- it is not a nested checkout, just named like one" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Grep '{"pattern":"plan","path":"docs/examples/.squad","output_mode":"content"}')" \
  "may not read .squad/design/"

if [ -n "$WORKTREE" ]; then
  expect "review-blindness: still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" reviewer-blind Read '{"file_path":".squad/design/demo/05-critic.md"}')" \
    "may not read .squad/design/"

  expect "review-blindness: a path-less Grep is still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" reviewer-blind Grep '{"pattern":"plan","output_mode":"content"}')" \
    "Scope your search to a path below the denied paths"

  expect "review-blindness: the coarse path=.squad is still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" reviewer-blind Grep '{"pattern":"plan","path":".squad","output_mode":"content"}')" \
    "may not read .squad/design/"
fi

# ===========================================================================
# Malformed / truncated payloads -- both blindness hooks must fail closed,
# universally, not just for the agents they govern. A payload that cannot
# be parsed carries no reliable agent_type, so there is no safe way to tell
# whether it belongs to a governed agent; the earlier `except Exception:
# sys.exit(0)` allowed by default here, which silently defeats the rule
# whenever the payload happens to be unreadable AND the call happens to be
# a governed agent's. `raw_fire`, unlike `fire`, sends its second argument
# to the hook's stdin exactly as given -- no JSON-encoding -- because the
# whole point is to send something that may not be valid JSON at all.
# ===========================================================================
raw_fire() {
  local hook="$1" raw_payload="$2"
  local rc=0
  printf '%s' "$raw_payload" \
    | CLAUDE_PROJECT_DIR="$MAIN_CO" bash "$HOOKS_DIR/$hook" >/dev/null 2>"$BLIND_ERRFILE" || rc=$?
  printf '%s' "$rc"
}

expect "track-blindness: a malformed (non-JSON) payload refuses, for every agent" 2 \
  "$(raw_fire enforce-track-blindness.sh 'not json')" \
  "could not parse"

expect "track-blindness: a truncated JSON payload refuses" 2 \
  "$(raw_fire enforce-track-blindness.sh '{"agent_type":"dreamer-first-principles","cwd":')" \
  "could not parse"

expect "track-blindness: well-formed JSON that is not an object refuses" 2 \
  "$(raw_fire enforce-track-blindness.sh '["not","a","dict"]')" \
  "could not parse"

expect "review-blindness: a malformed (non-JSON) payload refuses, for every agent" 2 \
  "$(raw_fire enforce-review-blindness.sh 'not json')" \
  "could not parse"

expect "review-blindness: a truncated JSON payload refuses" 2 \
  "$(raw_fire enforce-review-blindness.sh '{"agent_type":"reviewer-blind","tool_name":')" \
  "could not parse"

expect "review-blindness: well-formed JSON that is not an object refuses" 2 \
  "$(raw_fire enforce-review-blindness.sh '["not","a","dict"]')" \
  "could not parse"

# ===========================================================================
# enforce-review-history-channel.sh
# ===========================================================================
H=enforce-review-history-channel.sh

expect "history-channel: reviewer-blind denied git log" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git log --oneline -20"}')" \
  "git-history-namestatus.sh"

expect "history-channel: reviewer-blind denied git show" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git show HEAD"}')" "git show"

expect "history-channel: reviewer-blind denied git blame" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git blame src/Order.cs"}')" "git blame"

expect "history-channel: reviewer-blind denied gh pr view" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"gh pr view 42"}')" "gh pr view"

expect "history-channel: reviewer-blind denied gh issue view" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"gh issue view 7"}')" "gh issue view"

expect "history-channel: the sanctioned script is allowed" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"bash .claude/hooks/git-history-namestatus.sh main..HEAD"}')" ""

expect "history-channel: git diff is allowed" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git diff main...HEAD"}')" ""

expect "history-channel: reviewer-reconcile may read the narrative" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Bash '{"command":"gh pr view 42"}')" ""

if [ -n "$WORKTREE" ]; then
  expect "history-channel: still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" reviewer-blind Bash '{"command":"git log --name-status"}')" \
    "git-history-namestatus.sh"
fi

# Round 1 review, ⚠️-11: a global option's VALUE is a separate token
# (`-c core.pager=cat` is two words, not one `-c=core.pager=cat`), so the old
# `(?:-[^\s]+\s+)*` group consumed only `-c` and then required `log`
# immediately after it -- `core.pager=cat` sat in between and the match
# never fired. These are regression cases for the fix, not new coverage of
# a hole nobody hit: every one of them was a real, silent bypass.
expect "history-channel: git -c core.pager=cat log is still caught (was a bypass)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git -c core.pager=cat log"}')" \
  "git-history-namestatus.sh"

expect "history-channel: git -C <dir> log is still caught (separate-token value)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git -C /tmp/elsewhere log"}')" \
  "git-history-namestatus.sh"

expect "history-channel: git --git-dir <dir> show is still caught (long-form separate value)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git --git-dir /tmp/elsewhere show HEAD"}')" \
  "git show"

# Mutation control in the other direction: a flag that takes NO value
# (`--no-pager`) must not swallow the literal `log` that follows it as if it
# were that flag's value -- the optional value branch is only safe because
# the regex engine backtracks off it when doing so would leave no `log` to
# match. If a future edit made the value branch greedy/non-backtracking,
# this would start reporting "allowed" for a command this hook must deny.
expect "history-channel: git --no-pager log is still caught (value branch does not eat the subcommand)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Bash '{"command":"git --no-pager log"}')" \
  "git-history-namestatus.sh"

# ===========================================================================
# enforce-reviewer-readonly.sh
# ===========================================================================
H=enforce-reviewer-readonly.sh

expect "reviewer-readonly: reviewer-blind denied writing source" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Edit '{"file_path":"src/Order.cs"}')" \
  "may not write outside its own outputs"

expect "reviewer-readonly: reviewer-blind allowed its own artifact" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Write '{"file_path":".squad/design/demo/08-review-blind.md"}')" ""

expect "reviewer-readonly: reviewer-blind denied the verdict artifact (not its phase)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Write '{"file_path":".squad/design/demo/09-review-verdict.md"}')" ""

expect "reviewer-readonly: reviewer-reconcile denied writing source" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Edit '{"file_path":"src/Order.cs"}')" \
  "cannot fix what you find"

expect "reviewer-readonly: reviewer-reconcile allowed its verdict" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Write '{"file_path":".squad/design/demo/09-review-verdict.md"}')" ""

expect "reviewer-readonly: reviewer-reconcile allowed its decision drop" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Write '{"file_path":".squad/decisions/inbox/review-x.md"}')" ""

expect "reviewer-readonly: reviewer-reconcile allowed its own memory" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Write '{"file_path":".claude/agent-memory/reviewer-reconcile/MEMORY.md"}')" ""

expect "reviewer-readonly: reviewer-reconcile denied another agent's memory" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Write '{"file_path":".claude/agent-memory/critic/MEMORY.md"}')" ""

expect "reviewer-readonly: a builder passes through" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" csharp-dev Edit '{"file_path":"src/Order.cs"}')" ""

# ===========================================================================
# The ALLOW side must match the CANONICALISED destination, not merely a
# literal spelling that happens to match one of the allowed artifact
# names -- the same realpath treatment the cache deny above already gets.
# A symlink named exactly `09-review-verdict.md` under `.squad/design/*/`,
# but pointing OUTSIDE the repository, must refuse a governed agent's
# write through it; the same symlink pointing at an in-repo target must
# stay allowed (control, no over-match regression).
# ===========================================================================
ESCAPE_CO="$BLIND_TMP/escape"
mkdir -p "$ESCAPE_CO/.squad/design/demo" "$ESCAPE_CO/.squad/design/demo2"
OUTSIDE_TARGET="$BLIND_TMP/outside-target.txt"
: > "$OUTSIDE_TARGET"
ln -s "$OUTSIDE_TARGET" "$ESCAPE_CO/.squad/design/demo/09-review-verdict.md"
: > "$ESCAPE_CO/.squad/design/demo2/actual-target.md"
ln -s "actual-target.md" "$ESCAPE_CO/.squad/design/demo2/09-review-verdict.md"

expect "reviewer-readonly: a symlink named an allowed artifact, pointing OUTSIDE the repo, is refused" 2 \
  "$(fire $H "$ESCAPE_CO" "$ESCAPE_CO" reviewer-reconcile Write '{"file_path":".squad/design/demo/09-review-verdict.md"}')" \
  "may not write outside its own outputs"
expect "reviewer-readonly: control -- a symlink named an allowed artifact, pointing INSIDE the repo, stays allowed" 0 \
  "$(fire $H "$ESCAPE_CO" "$ESCAPE_CO" reviewer-reconcile Write '{"file_path":".squad/design/demo2/09-review-verdict.md"}')" ""

if [ -n "$WORKTREE" ]; then
  expect "reviewer-readonly: still denied from inside a worktree" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" reviewer-reconcile Edit '{"file_path":"src/Order.cs"}')" \
    "may not write outside its own outputs"

  expect "reviewer-readonly: own artifact still allowed from inside a worktree" 0 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" reviewer-reconcile Write '{"file_path":".squad/design/demo/09-review-verdict.md"}')" ""
fi

# ===========================================================================
# .squad/.last-review-verdict — deny-by-default for EVERY agent, listed or
# not. architect ruling arch-verdict-cache-and-gate-classification (INV-5,
# "single writer") plus security-expert's T-013
# (docs/security/threat-model.md, Trust Boundary 6): the merger
# (scribe-decision-merger.sh) is now the ONLY writer of this file; no tool
# call may write it, regardless of which agent asks. security-expert fired
# these four agent x Write/Edit combinations directly against the live
# hook and found all eight allowed (0); they must now all flip to refused
# (2).
# ===========================================================================
for a in csharp-dev devops js-dev tech-writer; do
  for t in Write Edit; do
    expect "reviewer-readonly: $a $t on the verdict cache is refused (T-013 flip)" 2 \
      "$(fire $H "$MAIN_CO" "$MAIN_CO" "$a" "$t" '{"file_path":".squad/.last-review-verdict"}')" \
      "exactly one writer"
  done
done

# reviewer-blind is CI-6 constraint-1 exempt: no active .gate-shadow is
# needed for its write to the cache to refuse -- MAIN_CO has no
# .squad/.gate-shadow file at all, and the refusal fires anyway. The
# message body renders the shadow file's path wrapped in backticks.
# Falsifier: reverting the escape on that backtick pair around
# .squad/.gate-shadow in the heredoc runs it as command substitution
# instead of printing it literally, which drops the backticked form from
# stderr entirely -- bash's own "line N: .squad/.gate-shadow: Permission
# denied" still contains the bare path, so a needle without backticks
# would stay green against a live defect. The backticked needle is the
# only one that goes red when the escape is reverted.
expect "reviewer-readonly: reviewer-blind write to the verdict cache names .squad/.gate-shadow literally" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-blind Write '{"file_path":".squad/.last-review-verdict"}')" \
  '`.squad/.gate-shadow`'

# reviewer-reconcile's OWN former allow-list entry for this path is gone too
# -- single writer means single writer, not "single writer plus the
# reviewer".
expect "reviewer-readonly: reviewer-reconcile's OWN write to the verdict cache is now refused (was allowed before)" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" reviewer-reconcile Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# No agent_type at all (the orchestrating Lead session) is refused too --
# the deny is unconditional, not keyed to a roster membership check.
expect "reviewer-readonly: unlisted/no agent_type writing the verdict cache is refused" 2 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" "" Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# Control: the deny is the PATH's, not a general lockdown -- an unlisted
# agent writing an ordinary file is unaffected.
expect "reviewer-readonly: control -- an unlisted agent writing an ordinary file stays allowed" 0 \
  "$(fire $H "$MAIN_CO" "$MAIN_CO" csharp-dev Write '{"file_path":"src/NewFile.cs"}')" ""

if [ -n "$WORKTREE" ]; then
  expect "reviewer-readonly: verdict cache deny reaches the worktree's own copy too" 2 \
    "$(fire $H "$MAIN_CO" "$WORKTREE" devops Write '{"file_path":".squad/.last-review-verdict"}')" \
    "exactly one writer"
fi

# ===========================================================================
# Six path spellings for the verdict-cache path, measured against the live
# hook: literal, ./-prefixed, absolute, and ..-traversal all refuse; a
# symlink FILE pointing at the cache and a symlink DIRECTORY standing in
# for .squad both refuse too (os.path.realpath resolves both); a hardlink
# and a case variant are UNCHANGED -- still allowed, named as a residual
# in the header (see docs/security/threat-model.md, Trust Boundary 6,
# T-017), not silently claimed closed. Pinning the true state so the
# header and this test agree.
# ===========================================================================
SYM_CO="$BLIND_TMP/symspell"
mkdir -p "$SYM_CO/.squad"
: > "$SYM_CO/.squad/.last-review-verdict"
ln -s ".squad/.last-review-verdict" "$SYM_CO/cachelink"
ln -s ".squad" "$SYM_CO/linkdir"
# The hardlink residual can only be exercised if ln(1) actually creates a
# hardlink on this filesystem -- some (notably across filesystem
# boundaries, or a restrictive mount) refuse it. HARDLINK_OK records
# which happened so the assertion below can tell "the residual is open"
# apart from "ln was unavailable here", rather than asserting a pass
# against an unrelated cp'd file that shares no inode with the cache at all.
HARDLINK_OK=0
if ln "$SYM_CO/.squad/.last-review-verdict" "$SYM_CO/hardlink" 2>/dev/null; then
  HARDLINK_OK=1
fi
: > "$SYM_CO/.squad/.LAST-REVIEW-VERDICT"

expect "reviewer-readonly: cache spelling -- literal path refuses" 2 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"
expect "reviewer-readonly: cache spelling -- ./-prefixed refuses" 2 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":"./.squad/.last-review-verdict"}')" \
  "exactly one writer"
expect "reviewer-readonly: cache spelling -- absolute path refuses" 2 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write "$(printf '{"file_path":%s}' "$(json_str "$SYM_CO/.squad/.last-review-verdict")")")" \
  "exactly one writer"
expect "reviewer-readonly: cache spelling -- ..-traversal refuses" 2 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":"linkdir2/../.squad/.last-review-verdict"}')" \
  "exactly one writer"
expect "reviewer-readonly: cache spelling -- symlink FILE pointing at the cache now refuses (was allowed)" 2 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":"cachelink"}')" \
  "exactly one writer"
expect "reviewer-readonly: cache spelling -- symlink DIRECTORY standing in for .squad now refuses (was allowed)" 2 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":"linkdir/.last-review-verdict"}')" \
  "exactly one writer"
if [ "$HARDLINK_OK" = "1" ]; then
  expect "reviewer-readonly: cache spelling -- hardlink is NOT covered, stays allowed (named residual)" 0 \
    "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":"hardlink"}')" ""
else
  report "reviewer-readonly: cache spelling -- hardlink is NOT covered, stays allowed (named residual)" 0 \
    "ln(1) could not create a hardlink on this filesystem -- the residual cannot be exercised here, so this reports a hard failure rather than a false pass against the cp fallback (an unrelated file sharing no inode with the cache)"
fi
expect "reviewer-readonly: cache spelling -- case variant is NOT covered, stays allowed (named residual)" 0 \
  "$(fire $H "$SYM_CO" "$SYM_CO" csharp-dev Write '{"file_path":".squad/.LAST-REVIEW-VERDICT"}')" ""

# ===========================================================================
# CI-6 shadow period, scoped to the verdict-cache deny only
# (.squad/.gate-shadow; 11-continuous-improvement-criterion.md § 6).
# SQUAD_NOW overrides the wall
# clock so expiry is exercised deterministically, not by waiting for a date.
#
# SQUAD_NOW used to be `export`ed then
# `unset` around each block -- a failure between the two would leak the
# fake clock into every later test in this same shell. Every use below is
# now a VAR=value PREFIX on the single `fire` call it governs (a bash
# function call scopes a prefixed assignment to that one invocation only,
# confirmed by direct execution: it never persists in the shell
# afterward), so there is nothing to leak and nothing to restore.
# ===========================================================================
SHADOW_CO="$BLIND_TMP/shadow"
mkdir -p "$SHADOW_CO/.squad" "$SHADOW_CO/src"
SHADOW_LOG="$SHADOW_CO/.squad/log/gate-shadow.md"

# In-shadow: expires: strictly after SQUAD_NOW -> allowed, and logged.
# 13-day initial span (compliant with the 14-day initial ceiling below).
printf 'created: 2026-09-01\nexpires: 2026-09-14\n' > "$SHADOW_CO/.squad/.gate-shadow"
rm -f "$SHADOW_LOG"
expect "reviewer-readonly: in-shadow, csharp-dev write to the cache is allowed" 0 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" ""
if [ -s "$SHADOW_LOG" ] && grep -q "SHADOW-ALLOW" "$SHADOW_LOG" 2>/dev/null; then
  report "reviewer-readonly: in-shadow allow logs one line to .squad/log/gate-shadow.md" 1
else
  report "reviewer-readonly: in-shadow allow logs one line to .squad/log/gate-shadow.md" 0 \
    "log missing or empty: $(cat "$SHADOW_LOG" 2>/dev/null)"
fi

# Constraint 1: a decision the PREVIOUS control already refused --
# reviewer-blind/scope-warden writing the cache -- stays refused even while
# the same shadow is actively allowing everyone else.
expect "reviewer-readonly: in-shadow, reviewer-blind write to the cache STILL refuses (CI-6 constraint 1)" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" reviewer-blind Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# Lapsed: expires: on or before SQUAD_NOW -> enforcing, not shadowing.
printf 'created: 2026-08-01\nexpires: 2026-08-15\n' > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: lapsed shadow enforces (refused, not allowed)" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# ===========================================================================
# _shadow_active's three date-arithmetic checks, each independently pinned:
# the initial span (created: -> the FIRST expires:) has its own 14-day
# ceiling, separate from the 28-day total; the extension is a COUNT (at
# most one appended expires: line, not merely a gap check); and created:
# must strictly precede the first expires:, not merely satisfy an
# unsigned days-over-ceiling comparison. Each is falsified independently
# below by reverting the corresponding check in enforce-reviewer-readonly.sh
# and re-firing the same fixture -- the row must then read exit 0 (allowed)
# instead of exit 2 (enforced).
# ===========================================================================

# Initial ceiling: a single, un-extended expires: 27 days after created:
# stays UNDER the 28-day total ceiling but exceeds the 14-day INITIAL
# ceiling on its own -- this must enforce even though the total-span
# check alone would let it through.
printf 'created: 2026-09-01\nexpires: 2026-09-28\n' > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: an un-extended shadow whose initial span exceeds 14 days enforces even though the 28-day total is not reached" 2 \
  "$(SQUAD_NOW=2026-09-20 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# Extension count: three expires: lines (created: plus TWO appended
# extensions), every adjacent gap individually <= 14 days and the total
# <= 28 days -- must enforce regardless, because "the user may extend
# once" is a count of expires: lines, not a gap check that a second
# small extension can slip under.
printf 'created: 2026-09-01\nexpires: 2026-09-08\nexpires: 2026-09-15\nexpires: 2026-09-22\n' \
  > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: a third expires: line (more than one extension) enforces regardless of individual gaps" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# Date ordering: created: strictly AFTER the (only) expires: line is a
# negative span. An unsigned days-over-ceiling comparison never sees a
# negative span as "over" anything and would honour this as a live
# shadow; it must enforce instead.
printf 'created: 2026-12-01\nexpires: 2026-09-29\n' > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: created: after expires: enforces rather than passing a negative span" 2 \
  "$(SQUAD_NOW=2026-09-20 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# Malformed expires: -> enforcing.
printf 'created: 2026-09-01\nexpires: not-a-date\n' > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: malformed expires: enforces (refused, not allowed)" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# Absent .gate-shadow -> enforcing. (Every MAIN_CO case above already
# exercises this implicitly -- no .gate-shadow file exists in that fixture
# at all -- this makes it an explicit, labelled assertion too.)
rm -f "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: absent .gate-shadow enforces (refused, not allowed)" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# --- The ceiling check must not defeat honouring an extension. -------
# An appended extension (a SECOND expires: line, per .squad/.gate-shadow's
# own documented procedure) is honoured -- the LAST expires:, not the
# first, governs.
printf 'created: 2026-09-05\nexpires: 2026-09-19\nexpires: 2026-10-03\n' \
  > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: appended expires: extension is honoured" 0 \
  "$(SQUAD_NOW=2026-09-25 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" ""

# Over the 28-day absolute ceiling (14 initial + one 14-day extension) from
# created: -> enforcing, even with a single well-formed expires: line.
printf 'created: 2026-09-05\nexpires: 2099-01-01\n' > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: over the 28-day ceiling from created: enforces" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# The appended extension itself exceeding 14 days beyond the FIRST
# expires: -> enforcing (the extension is capped, not just the total).
# Deliberately NOT over the 28-day ceiling (created -> last expires is
# 23 days) so this isolates the extension-gap rule specifically from
# the ceiling rule above -- only the GAP between first and last
# expires: (20 days) exceeds 14.
printf 'created: 2026-09-05\nexpires: 2026-09-08\nexpires: 2026-09-28\n' \
  > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: an extension exceeding 14 days beyond the first expires: enforces" 2 \
  "$(SQUAD_NOW=2026-09-20 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# created: missing -> malformed -> enforcing, same polarity as a malformed
# expires:.
printf 'expires: 2026-09-19\n' > "$SHADOW_CO/.squad/.gate-shadow"
expect "reviewer-readonly: missing created: enforces (malformed, not shadowed)" 2 \
  "$(SQUAD_NOW=2026-09-10 fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

# --- SQUAD_NOW is honoured ONLY when
# SQUAD_TEST_HARNESS=1 is ALSO set. Built dynamically (relative to today,
# whatever today is) so it never goes stale: a shadow that is LAPSED by
# the real wall clock but would read as IN-SHADOW if SQUAD_NOW were
# honoured -- the two directions disagree, so this actually discriminates
# rather than passing by coincidence of the calendar date.
m4_today="$(date -u +%Y-%m-%d)"
m4_created="$(date -u -d "$m4_today -20 days" +%Y-%m-%d)"
m4_lapsed_expires="$(date -u -d "$m4_today -6 days" +%Y-%m-%d)"
m4_fake_now="$(date -u -d "$m4_today -15 days" +%Y-%m-%d)"
printf 'created: %s\nexpires: %s\n' "$m4_created" "$m4_lapsed_expires" \
  > "$SHADOW_CO/.squad/.gate-shadow"

expect "reviewer-readonly: SQUAD_NOW outside SQUAD_TEST_HARNESS is ignored (real clock governs, lapsed enforces)" 2 \
  "$(unset SQUAD_TEST_HARNESS; SQUAD_NOW="$m4_fake_now" fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
  "exactly one writer"

expect "reviewer-readonly: control -- the SAME SQUAD_NOW, WITH SQUAD_TEST_HARNESS, is honoured (shadow allows)" 0 \
  "$(SQUAD_NOW="$m4_fake_now" fire $H "$SHADOW_CO" "$SHADOW_CO" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" ""

# ===========================================================================
# "No test observes the DEPLOYED shadow configuration" -- every assertion
# above runs against a mktemp fixture, so the suite could stay green while
# THIS repository's own .squad/.gate-shadow ships something CI-6 forbids.
# Rewriting the real file to `expires: 2099-01-01` leaves the rest of the
# suite passing with 0 failures (the pass/fail count itself varies with
# the tree and is not reproduced here as a number). This section reads the
# real file, at the real repository root.
#
# The lapse check below does NOT assert "expires: is in the future" against
# the real wall clock: a shadow that lapses is CI-6's own desired end
# state, not a violation, so asserting the calendar has not yet reached
# expiry turns the suite red the day it lapses -- the safe outcome reported
# as a failure. Instead it asserts the property CI-6 actually requires:
# LAPSED IMPLIES THE HOOK ENFORCES. It does so with the real file's own
# `created:`/`expires:` content, but a SIMULATED clock one day past the
# real last `expires:` (SQUAD_NOW, honoured only under SQUAD_TEST_HARNESS,
# exported once at the top of this file) -- so the assertion is green
# before, on, and after the real expiry date, rather than flipping to red
# the moment the calendar catches up to it.
# ===========================================================================
REAL_SHADOW="$REPO_ROOT/.squad/.gate-shadow"
if [ -f "$REAL_SHADOW" ]; then
  m1_created="$(grep -E '^created:' "$REAL_SHADOW" | head -n1 | sed -E 's/^created:[[:space:]]*//')"
  m1_expires_all=()
  while IFS= read -r m1_line; do
    m1_expires_all+=("$m1_line")
  done < <(grep -E '^expires:' "$REAL_SHADOW" | sed -E 's/^expires:[[:space:]]*//')
  m1_n_expires="${#m1_expires_all[@]}"
  m1_first_expires=""
  m1_last_expires=""
  if [ "$m1_n_expires" -gt 0 ]; then
    m1_first_expires="${m1_expires_all[0]}"
    m1_last_expires="${m1_expires_all[$((m1_n_expires - 1))]}"
  fi

  if [ -n "$m1_created" ] && [ -n "$m1_last_expires" ]; then
    report "the repository's own .gate-shadow parses (created:/expires: present)" 1
  else
    report "the repository's own .gate-shadow parses (created:/expires: present)" 0 \
      "created='$m1_created' expires_count=$m1_n_expires"
  fi

  m1_post_expiry=""
  if [ -n "$m1_last_expires" ]; then
    m1_post_expiry="$(date -u -d "$m1_last_expires +1 day" +%Y-%m-%d 2>/dev/null)"
  fi
  if [ -n "$m1_post_expiry" ]; then
    M1_FIXTURE="$BLIND_TMP/m1shadow"
    mkdir -p "$M1_FIXTURE/.squad"
    cp "$REAL_SHADOW" "$M1_FIXTURE/.squad/.gate-shadow"
    expect "the repository's own .gate-shadow enforces once its expiry lapses (simulated post-expiry clock, real file content)" 2 \
      "$(SQUAD_NOW=$m1_post_expiry fire $H "$M1_FIXTURE" "$M1_FIXTURE" csharp-dev Write '{"file_path":".squad/.last-review-verdict"}')" \
      "exactly one writer"
  else
    report "the repository's own .gate-shadow enforces once its expiry lapses (simulated post-expiry clock, real file content)" 0 \
      "could not compute a post-expiry date from expires='$m1_last_expires'"
  fi

  if [ -n "$m1_created" ] && [ -n "$m1_last_expires" ]; then
    m1_max_days=14
    [ "$m1_n_expires" -gt 1 ] && m1_max_days=28
    m1_span="$(python3 -c '
import datetime, sys
c = datetime.date.fromisoformat(sys.argv[1])
e = datetime.date.fromisoformat(sys.argv[2])
print((e - c).days)
' "$m1_created" "$m1_last_expires" 2>/dev/null)"
    if [ -n "$m1_span" ] && [ "$m1_span" -le "$m1_max_days" ] 2>/dev/null; then
      report "the repository's own .gate-shadow is within its ceiling (<=14d, <=28d with one extension)" 1
    else
      report "the repository's own .gate-shadow is within its ceiling (<=14d, <=28d with one extension)" 0 \
        "span=$m1_span days, max=$m1_max_days (created=$m1_created last_expires=$m1_last_expires, $m1_n_expires expires: lines)"
    fi
  fi
else
  report "the repository's own .gate-shadow, if absent, needs no check (enforcing by default)" 1
fi

# ===========================================================================
# .squad/log/gate-shadow.md must be TRACKED -- it is the only audit trail
# of what the CI-6 shadow period allowed. This measures TRACKING directly
# with `git ls-files --error-unmatch`, not the .gitignore negation line:
# `git check-ignore` evaluates ignore patterns against a path, and a path
# already IN THE INDEX is not evaluated against them at all -- confirmed
# by direct execution, a tracked file matching a later `!` negation still
# returns `check-ignore -v` exit 1 with no output, indistinguishable from a
# path no pattern touches. So a check built on check-ignore stays green
# forever if the file is removed from the index (`git rm --cached`) while
# the negation line in .gitignore is untouched. Falsifier: `git rm --cached
# .squad/log/gate-shadow.md` (index only, file left on disk) must turn
# this row red.
# ===========================================================================
if (cd "$REPO_ROOT" && git ls-files --error-unmatch .squad/log/gate-shadow.md) >/dev/null 2>&1; then
  report ".squad/log/gate-shadow.md is tracked (git ls-files --error-unmatch)" 1
else
  report ".squad/log/gate-shadow.md is tracked (git ls-files --error-unmatch)" 0 \
    "git ls-files --error-unmatch found no match -- the file is not in the index"
fi

# ===========================================================================
# git-history-namestatus.sh — the sanctioned channel
# ===========================================================================
ghns_out="$(cd "$MAIN_CO" && bash "$HOOKS_DIR/git-history-namestatus.sh" 2>/dev/null || true)"
case "$ghns_out" in
  *"fixture"*)
    report "git-history-namestatus: commit subjects do not leak" 0 \
      "output contained the commit subject 'fixture' -- this channel must emit hashes, dates and name-status only" ;;
  *) report "git-history-namestatus: commit subjects do not leak" 1 ;;
esac
case "$ghns_out" in
  *"A	src/Order.cs"*|*"A"*"src/Order.cs"*)
    report "git-history-namestatus: name-status is emitted" 1 ;;
  *) report "git-history-namestatus: name-status is emitted" 0 "expected an 'A src/Order.cs' line, got: ${ghns_out:-<empty>}" ;;
esac

ghns_rc=0
(cd "$MAIN_CO" && bash "$HOOKS_DIR/git-history-namestatus.sh" --format=%s >/dev/null 2>&1) || ghns_rc=$?
if [ "$ghns_rc" = "2" ]; then
  report "git-history-namestatus: a format flag is refused" 1
else
  report "git-history-namestatus: a format flag is refused" 0 "expected exit 2, got $ghns_rc"
fi

if [ -n "$WORKTREE" ]; then
  ghns_wt="$(cd "$WORKTREE" && bash "$HOOKS_DIR/git-history-namestatus.sh" 2>/dev/null || true)"
  case "$ghns_wt" in
    *"src/Order.cs"*) report "git-history-namestatus: works from inside a worktree" 1 ;;
    *) report "git-history-namestatus: works from inside a worktree" 0 "got: ${ghns_wt:-<empty>}" ;;
  esac
fi

# ===========================================================================
# The cwd-vs-CLAUDE_PROJECT_DIR requirement, checked directly.
#
# The worktree cases above all pass against a CLAUDE_PROJECT_DIR-rooted
# implementation too, because the fixture worktree is a sibling of the main
# checkout and the relative paths look the same from both. That is precisely
# the trap: a broken hook passes a worktree test that does not discriminate.
# These two do discriminate.
#
#   1. Structurally: none of the four hooks may reference CLAUDE_PROJECT_DIR
#      outside its header comment. A hook that never reads the variable cannot
#      be rooted at it.
#   2. Behaviourally: fire with CLAUDE_PROJECT_DIR pointing at an unrelated
#      empty directory and cwd at the worktree. An implementation that resolved
#      against CLAUDE_PROJECT_DIR would look for the denied file under
#      $ELSEWHERE, not find the pattern, and allow the read.
# ===========================================================================
for h in enforce-track-blindness.sh enforce-review-blindness.sh \
         enforce-review-history-channel.sh enforce-reviewer-readonly.sh; do
  if grep -v '^[[:space:]]*#' "$HOOKS_DIR/$h" | grep -q 'CLAUDE_PROJECT_DIR'; then
    report "path resolution: $h does not read CLAUDE_PROJECT_DIR" 0 \
      "found a live CLAUDE_PROJECT_DIR reference -- hook paths do not follow a worktree, so this stops matching inside one"
  else
    report "path resolution: $h does not read CLAUDE_PROJECT_DIR" 1
  fi
done

if [ -n "$WORKTREE" ]; then
  expect "path resolution: track-blindness denies with CLAUDE_PROJECT_DIR pointing elsewhere entirely" 2 \
    "$(fire enforce-track-blindness.sh "$ELSEWHERE" "$WORKTREE" dreamer-first-principles Read '{"file_path":".claude/docs/decisions.md"}')" \
    "may not read the decision register"

  expect "path resolution: review-blindness denies with CLAUDE_PROJECT_DIR pointing elsewhere entirely" 2 \
    "$(fire enforce-review-blindness.sh "$ELSEWHERE" "$WORKTREE" reviewer-blind Read '{"file_path":".squad/design/demo/04-realist-plan.md"}')" \
    "may not read .squad/design/"

  expect "path resolution: reviewer-readonly denies with CLAUDE_PROJECT_DIR pointing elsewhere entirely" 2 \
    "$(fire enforce-reviewer-readonly.sh "$ELSEWHERE" "$WORKTREE" reviewer-reconcile Edit '{"file_path":"src/Order.cs"}')" \
    "may not write outside its own outputs"
fi

# ===========================================================================
# Round 1 review, 🔴-5: gate 1's own output (00-warden-scan.md, written by
# scope-warden.sh, and 00-warden.md, written by the scope-warden subagent on
# top of it) sits in .squad/design/<slug>/ -- the exact directory Track A is
# told to read -- and 00-warden-scan.md quotes matched lexicon terms and their
# surrounding lines verbatim. Denied to dreamer-first-principles the same way
# 00-knowledge.md already is.
# ===========================================================================
expect "track-blindness: dreamer-first-principles denied 00-warden-scan.md (quotes the lexicon verbatim)" 2 \
  "$(fire enforce-track-blindness.sh "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".squad/design/demo/00-warden-scan.md"}')" \
  "gate 1"

expect "track-blindness: dreamer-first-principles denied 00-warden.md" 2 \
  "$(fire enforce-track-blindness.sh "$MAIN_CO" "$MAIN_CO" dreamer-first-principles Read '{"file_path":".squad/design/demo/00-warden.md"}')" \
  "gate 1"

expect "track-blindness: dreamer-informed may still read 00-warden-scan.md (not on its own deny list)" 0 \
  "$(fire enforce-track-blindness.sh "$MAIN_CO" "$MAIN_CO" dreamer-informed Read '{"file_path":".squad/design/demo/00-warden-scan.md"}')" ""

# ===========================================================================
# Round 1 review, ⚠️-7: enforce-track-blindness.sh and
# enforce-review-blindness.sh used to carry ~100 byte-identical lines of
# containment logic (leaves/segs/resolve/classify/reaches/reach_hit/
# glob_root/truthy_str). Both now import that logic from one sourced module,
# lib/path_containment.py, instead of each defining its own copy that
# nothing asserted agreed with the other. Checked structurally in both
# directions: the import is present, and the function bodies are gone from
# the hook files themselves (a stale copy left behind after a partial
# extraction would defeat the point as completely as never extracting it).
# ===========================================================================
for h in enforce-track-blindness.sh enforce-review-blindness.sh; do
  if grep -q 'from path_containment import' "$HOOKS_DIR/$h"; then
    report "shared containment: $h imports lib/path_containment.py" 1
  else
    report "shared containment: $h imports lib/path_containment.py" 0 \
      "no 'from path_containment import' line found"
  fi
  for fn in reach_hit glob_root leaves; do
    if grep -qE "^def ${fn}\\(" "$HOOKS_DIR/$h"; then
      report "shared containment: $h has no inline redefinition of $fn" 0 \
        "found 'def $fn(' inside the hook itself -- the extraction did not stick, and a fix to lib/path_containment.py would not reach this copy"
    else
      report "shared containment: $h has no inline redefinition of $fn" 1
    fi
  done
done

if [ -f "$HOOKS_DIR/lib/path_containment.py" ]; then
  report "shared containment: lib/path_containment.py exists" 1
else
  report "shared containment: lib/path_containment.py exists" 0 "file not found"
fi

# ===========================================================================
# Round 1 review, 🔴-1: claude-hooks-tests.yml's `paths:` filter is a
# hand-maintained enumeration of every $REPO_ROOT-relative file the four
# suites (run.sh, blindness.sh, phase-gates.sh, invariant-chain.sh) read
# OUTSIDE .claude/hooks/** (that one glob already covers everything under
# it). The filter drifted once already -- six files plus .squad/.gate-shadow
# were read by the suites and absent from it, so a PR touching ONLY one of
# them (e.g. deleting every PreToolUse matcher from .claude/settings.json,
# unwiring every hook this workflow exists to test) would not have
# triggered the workflow at all and would have merged green. This
# re-derives the same enumeration the fix's own comment documents having
# used, at test time, so a future drift is a loud local failure instead of
# a CI job silently not running.
# ===========================================================================
ci_workflow="$REPO_ROOT/.github/workflows/claude-hooks-tests.yml"
if [ -f "$ci_workflow" ]; then
  ci_missing="$(python3 -c '
import glob, os, re, sys

workflow_path, tests_dir = sys.argv[1], sys.argv[2]

with open(workflow_path, encoding="utf-8") as fh:
    workflow = fh.read()

filtered = set(re.findall(r"^\s*-\s*'"'"'([^'"'"']+)'"'"'\s*$", workflow, re.M))

refs = set()
for path in glob.glob(os.path.join(tests_dir, "*.sh")):
    with open(path, encoding="utf-8", errors="replace") as fh:
        content = fh.read()
    for m in re.finditer(r"\$(?:REPO_ROOT|ROOT)/([A-Za-z0-9_./*-]+)", content):
        rel = m.group(1)
        if rel == ".claude/hooks" or rel.startswith(".claude/hooks/"):
            continue  # already covered by the .claude/hooks/** glob
        refs.add(rel)


def covered(rel, patterns):
    for p in patterns:
        if p == rel:
            return True
        if p.endswith("/**"):
            base = p[:-3]
            if rel == base or rel.startswith(base + "/"):
                return True
    return False


missing = sorted(r for r in refs if not covered(r, filtered))
print("\n".join(missing))
' "$ci_workflow" "$SCRIPT_DIR")"

  if [ -z "$ci_missing" ]; then
    report "CI path coverage: every out-of-hooks path the suites read is in claude-hooks-tests.yml's paths: filter" 1
  else
    report "CI path coverage: every out-of-hooks path the suites read is in claude-hooks-tests.yml's paths: filter" 0 \
      "not covered by the workflow's paths: filter: $(printf '%s' "$ci_missing" | tr '\n' ' ')"
  fi
else
  report "CI path coverage: claude-hooks-tests.yml exists" 0 "$ci_workflow not found"
fi

# ===========================================================================
git -C "$MAIN_CO" worktree remove --force "$BLIND_TMP/wt" >/dev/null 2>&1 || true
rm -rf "$BLIND_TMP"

if [ "${BLINDNESS_STANDALONE:-0}" = "1" ]; then
  echo
  echo "Summary: $pass passed, $fail failed"
  logged="$(wc -l < "$IC_FAIL_LOG" 2>/dev/null | tr -d ' ')"; : "${logged:=0}"
  if [ "$logged" != "$fail" ]; then
    printf 'INTEGRITY: %s failure(s) recorded, counter says %s.\n' "$logged" "$fail" >&2
    printf 'An assertion reported a failure the tally never saw -- most likely it ran in a subshell, where report() increments a copy of the counters. Recorded:\n' >&2
    sed 's/^/  /' "$IC_FAIL_LOG" >&2
    exit 1
  fi
  [ "$logged" -eq 0 ]
fi
