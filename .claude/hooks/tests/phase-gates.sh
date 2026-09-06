#!/usr/bin/env bash
#
# Tests for the stage-2 gate hooks:
#
#   scope-warden.sh          SubagentStop, matching architect       (reports)
#   lexicon-check.sh         SubagentStop, matching Track A         (blocks, overridable)
#   enforce-phase-order.sh   PreToolUse, Write                      (blocks)
#
# Designed to be sourced by run.sh, which owns `report`, `pass` and `fail`.
# Also runs standalone:
#
#   bash .claude/hooks/tests/phase-gates.sh
#
# Per hook: the blocking case, the allowed case, and the same denied case
# reached from inside a real git worktree. The worktree cases matter for the
# same reason as in blindness.sh — ${CLAUDE_PROJECT_DIR} does not follow a
# worktree while the payload's `cwd` does, so each worktree case here sets
# CLAUDE_PROJECT_DIR to an unrelated directory, which a CLAUDE_PROJECT_DIR-
# rooted implementation cannot survive.

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
  PHASEGATES_STANDALONE=1
fi

PG_HOOKS="$REPO_ROOT/.claude/hooks"
PG_TMP="$(mktemp -d)"
PG_ERR="$PG_TMP/stderr.log"
PG_OUT="$PG_TMP/stdout.log"
PG_ELSEWHERE="$PG_TMP/elsewhere"
mkdir -p "$PG_ELSEWHERE"

pg_json() { python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$1"; }

# hook, project_dir, cwd, json-payload-body -> echoes exit code
pg_fire() {
  local hook="$1" project_dir="$2" body="$3" rc=0
  printf '%s' "$body" \
    | CLAUDE_PROJECT_DIR="$project_dir" bash "$PG_HOOKS/$hook" >"$PG_OUT" 2>"$PG_ERR" || rc=$?
  printf '%s' "$rc"
}

# label, want-rc, got-rc, needle-in(stderr+stdout)
pg_expect() {
  local label="$1" want="$2" got="$3" needle="${4:-}"
  local out; out="$(cat "$PG_ERR" "$PG_OUT" 2>/dev/null)"
  if [ "$got" != "$want" ]; then
    report "$label" 0 "expected exit $want, got $got -- ${out%%$'\n'*}"
    return
  fi
  if [ -n "$needle" ]; then
    case "$out" in
      *"$needle"*) report "$label" 1 ;;
      *) report "$label" 0 "exit was right but output did not mention '$needle' -- got: ${out:-<empty>}" ;;
    esac
    return
  fi
  report "$label" 1
}

# Build a project fixture with a lexicon and a design pass dir.
pg_project() {
  local root="$1" slug="${2:-demo}"
  mkdir -p "$root/.claude/docs" "$root/.squad/design/$slug" "$root/src"
  cp "$REPO_ROOT/.claude/docs/pattern-lexicon.md" "$root/.claude/docs/"
  printf 'class Order {}\n' > "$root/src/Order.cs"
}

MAIN="$PG_TMP/main"
pg_project "$MAIN"

# A real worktree, for the cwd-resolution cases.
PG_WT=""
if git -C "$MAIN" init -q 2>/dev/null \
   && git -C "$MAIN" -c user.email=t@t -c user.name=t add -A 2>/dev/null \
   && git -C "$MAIN" -c user.email=t@t -c user.name=t -c commit.gpgsign=false \
        commit -qm "fixture" 2>/dev/null \
   && git -C "$MAIN" worktree add -q -b pgwt "$PG_TMP/wt" 2>/dev/null; then
  PG_WT="$PG_TMP/wt"
  pg_project "$PG_WT"
else
  report "phase-gates worktree fixture: created" 0 "git init/worktree failed"
fi

# ===========================================================================
# scope-warden.sh — reports, never blocks, never rewrites
# ===========================================================================
cat > "$MAIN/.squad/design/demo/00-scope.md" <<'EOS'
# Scope — demo
This is the same shape as decision `architect-20260415T120000Z-article-state-machine`.
Typically we would reach for event sourcing here.
See `.claude/docs/decisions.md` for context.
Transitions must be total: every state accepts every event or explicitly rejects it.
EOS
scope_before="$(cat "$MAIN/.squad/design/demo/00-scope.md")"

pg_expect "scope-warden: reports findings and does not block" 0 \
  "$(pg_fire scope-warden.sh "$MAIN" "$(printf '{"agent_type":"architect","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" \
  "finding(s) in 00-scope.md"

if [ -f "$MAIN/.squad/design/demo/00-warden-scan.md" ]; then
  report "scope-warden: writes 00-warden-scan.md" 1
else
  report "scope-warden: writes 00-warden-scan.md" 0 "file not created"
fi

for needle in "architect-20260415T120000Z-article-state-machine" "event sourcing" "typically" ".claude/docs/decisions.md"; do
  if grep -qF "$needle" "$MAIN/.squad/design/demo/00-warden-scan.md" 2>/dev/null; then
    report "scope-warden: scan reports '$needle'" 1
  else
    report "scope-warden: scan reports '$needle'" 0 "not present in 00-warden-scan.md"
  fi
done

if [ "$(cat "$MAIN/.squad/design/demo/00-scope.md")" = "$scope_before" ]; then
  report "scope-warden: 00-scope.md is not rewritten" 1
else
  report "scope-warden: 00-scope.md is not rewritten" 0 \
    "the scope changed -- a checker that can fix what it finds is a co-author"
fi

cat > "$MAIN/.squad/design/demo/00-scope.md" <<'EOS'
# Scope — demo
Transitions must be total. Ordering must survive a replay. The write path must be idempotent.
EOS
pg_expect "scope-warden: a clean scope reports CLEAN" 0 \
  "$(pg_fire scope-warden.sh "$MAIN" "$(printf '{"agent_type":"architect","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" \
  "CLEAN"

pg_expect "scope-warden: does not run for another agent" 0 \
  "$(pg_fire scope-warden.sh "$MAIN" "$(printf '{"agent_type":"realist","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" ""
if [ -s "$PG_OUT" ]; then
  report "scope-warden: silent for another agent" 0 "produced output: $(head -c 120 "$PG_OUT")"
else
  report "scope-warden: silent for another agent" 1
fi

if [ -n "$PG_WT" ]; then
  cat > "$PG_WT/.squad/design/demo/00-scope.md" <<'EOS'
# Scope — demo
Typically we would reach for event sourcing here.
EOS
  pg_expect "scope-warden: runs against cwd inside a worktree, not CLAUDE_PROJECT_DIR" 0 \
    "$(pg_fire scope-warden.sh "$PG_ELSEWHERE" "$(printf '{"agent_type":"architect","cwd":%s,"stop_hook_active":false}' "$(pg_json "$PG_WT")")")" \
    "finding(s) in 00-scope.md"
  if [ -f "$PG_WT/.squad/design/demo/00-warden-scan.md" ]; then
    report "scope-warden: writes the scan into the worktree, not the main checkout" 1
  else
    report "scope-warden: writes the scan into the worktree, not the main checkout" 0 "scan not found in the worktree"
  fi
fi

# ===========================================================================
# lexicon-check.sh — blocks, and the override exists from the first run
# ===========================================================================
cat > "$MAIN/.squad/design/demo/01-track-a.md" <<'EOS'
# Track A
Candidate A1 preserves total transitions across replays.
Typically this is handled with event sourcing.
The structural property is that the fold is associative.
EOS

pg_expect "lexicon-check: blocks a named pattern" 2 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" \
  "event sourcing"
pg_expect "lexicon-check: blocks recall grammar" 2 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" \
  "typically"
pg_expect "lexicon-check: the refusal names the override path" 2 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" \
  ".lexicon-override"

if grep -q 'BLOCKED' "$MAIN/.squad/log/lexicon-hits.md" 2>/dev/null; then
  report "lexicon-check: the hit is logged as BLOCKED" 1
else
  report "lexicon-check: the hit is logged as BLOCKED" 0 "no BLOCKED row in .squad/log/lexicon-hits.md"
fi

# Selective override: one term only.
printf 'event sourcing\n' > "$MAIN/.squad/design/demo/.lexicon-override"
pg_expect "lexicon-check: a single-term override still blocks the other term" 2 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" \
  "typically"

# Blanket override.
: > "$MAIN/.squad/design/demo/.lexicon-override"
pg_expect "lexicon-check: a blanket override lets the artifact through" 0 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" ""

if grep -q 'OVERRIDDEN' "$MAIN/.squad/log/lexicon-hits.md" 2>/dev/null; then
  report "lexicon-check: the override is logged as OVERRIDDEN" 1
else
  report "lexicon-check: the override is logged as OVERRIDDEN" 0 \
    "no OVERRIDDEN row -- the override is the only calibration input the check has, and an unlogged one is evidence of nothing"
fi

rm -f "$MAIN/.squad/design/demo/.lexicon-override"
cat > "$MAIN/.squad/design/demo/01-track-a.md" <<'EOS'
# Track A
Candidate A1 preserves total transitions across replays.
The structural property is that the fold is associative and the identity is the empty history.
EOS
pg_expect "lexicon-check: a clean artifact passes" 0 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" ""

pg_expect "lexicon-check: does not run for another agent" 0 \
  "$(pg_fire lexicon-check.sh "$MAIN" "$(printf '{"agent_type":"dreamer-informed","cwd":%s,"stop_hook_active":false}' "$(pg_json "$MAIN")")")" ""

# An empty lexicon must say so rather than passing silently.
pg_empty="$PG_TMP/nolex"
pg_project "$pg_empty"
printf '# Pattern lexicon\n\n## Terms\n\n## Recall grammar\n' > "$pg_empty/.claude/docs/pattern-lexicon.md"
printf '# Track A\nTypically this is event sourcing.\n' > "$pg_empty/.squad/design/demo/01-track-a.md"
pg_expect "lexicon-check: an empty lexicon warns instead of passing silently" 0 \
  "$(pg_fire lexicon-check.sh "$pg_empty" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$pg_empty")")")" \
  "no terms and no recall grammar"

if [ -n "$PG_WT" ]; then
  cat > "$PG_WT/.squad/design/demo/01-track-a.md" <<'EOS'
# Track A
Typically this is handled with event sourcing.
EOS
  pg_expect "lexicon-check: still blocks from inside a worktree" 2 \
    "$(pg_fire lexicon-check.sh "$PG_ELSEWHERE" "$(printf '{"agent_type":"dreamer-first-principles","cwd":%s,"stop_hook_active":false}' "$(pg_json "$PG_WT")")")" \
    "event sourcing"
  if grep -q 'BLOCKED' "$PG_WT/.squad/log/lexicon-hits.md" 2>/dev/null; then
    report "lexicon-check: logs into the worktree, not the main checkout" 1
  else
    report "lexicon-check: logs into the worktree, not the main checkout" 0 "no hit log in the worktree"
  fi
fi

# ===========================================================================
# enforce-phase-order.sh
# ===========================================================================
pg_order="$PG_TMP/order"
pg_project "$pg_order"
W() { printf '{"cwd":%s,"tool_name":"Write","tool_input":{"file_path":"%s"}}' "$(pg_json "$1")" "$2"; }

pg_expect "phase-order: 03 without 01 or 02 is refused" 2 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/03-convergence.md")")" \
  "01-track-a.md, 02-track-b.md"

: > "$pg_order/.squad/design/demo/01-track-a.md"
pg_expect "phase-order: 03 with only 01 is still refused" 2 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/03-convergence.md")")" \
  "02-track-b.md"

: > "$pg_order/.squad/design/demo/02-track-b.md"
pg_expect "phase-order: 03 with both tracks is allowed" 0 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/03-convergence.md")")" ""

# A documented skip of Track A is honoured; Track B never is.
rm -f "$pg_order/.squad/design/demo/01-track-a.md"
printf '# Scope\n\n⏩ Skipping first-principles track — bug fix with a known root cause.\n' \
  > "$pg_order/.squad/design/demo/00-scope.md"
pg_expect "phase-order: a documented Track A skip is honoured" 0 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/03-convergence.md")")" ""
rm -f "$pg_order/.squad/design/demo/02-track-b.md"
pg_expect "phase-order: Track B is never skippable, documented or not" 2 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/03-convergence.md")")" \
  "02-track-b.md"

pg_expect "phase-order: 05 without 04 is refused" 2 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/05-critic.md")")" \
  "04-realist-plan.md"
: > "$pg_order/.squad/design/demo/04-realist-plan.md"
pg_expect "phase-order: 05 with 04 is allowed" 0 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/05-critic.md")")" ""

pg_expect "phase-order: 09 without 08 is refused" 2 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/09-review-verdict.md")")" \
  "08-review-blind.md"
: > "$pg_order/.squad/design/demo/08-review-blind.md"
pg_expect "phase-order: 09 with 08 is allowed" 0 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/09-review-verdict.md")")" ""

pg_expect "phase-order: an ungoverned artifact passes through" 0 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" ".squad/design/demo/06-spec.md")")" ""
pg_expect "phase-order: a same-named file outside .squad/design/ passes through" 0 \
  "$(pg_fire enforce-phase-order.sh "$pg_order" "$(W "$pg_order" "docs/03-convergence.md")")" ""

if [ -n "$PG_WT" ]; then
  rm -rf "$PG_WT/.squad/design/wtpass"; mkdir -p "$PG_WT/.squad/design/wtpass"
  pg_expect "phase-order: still refuses from inside a worktree" 2 \
    "$(pg_fire enforce-phase-order.sh "$PG_ELSEWHERE" "$(W "$PG_WT" ".squad/design/wtpass/09-review-verdict.md")")" \
    "08-review-blind.md"
  : > "$PG_WT/.squad/design/wtpass/08-review-blind.md"
  pg_expect "phase-order: allows from inside a worktree once 08 exists there" 0 \
    "$(pg_fire enforce-phase-order.sh "$PG_ELSEWHERE" "$(W "$PG_WT" ".squad/design/wtpass/09-review-verdict.md")")" ""
fi

# ===========================================================================
# Path resolution, checked structurally.
# ===========================================================================
for h in scope-warden.sh lexicon-check.sh enforce-phase-order.sh; do
  if grep -v '^[[:space:]]*#' "$PG_HOOKS/$h" | grep -q 'CLAUDE_PROJECT_DIR'; then
    report "path resolution: $h does not read CLAUDE_PROJECT_DIR" 0 \
      "found a live CLAUDE_PROJECT_DIR reference -- hook paths do not follow a worktree"
  else
    report "path resolution: $h does not read CLAUDE_PROJECT_DIR" 1
  fi
done

# ===========================================================================
# The commands and the roster exist as the flow assumes.
# ===========================================================================
for c in design review curate; do
  if [ -f "$REPO_ROOT/.claude/commands/$c.md" ]; then
    report "commands: /$c exists" 1
  else
    report "commands: /$c exists" 0 "$REPO_ROOT/.claude/commands/$c.md not found"
  fi
done

for a in scope-warden reviewer-blind reviewer-reconcile; do
  if [ -f "$REPO_ROOT/.claude/agents/$a.md" ]; then
    report "roster: $a charter exists" 1
  else
    report "roster: $a charter exists" 0 "charter not found"
  fi
done
if [ -f "$REPO_ROOT/.claude/agents/reviewer.md" ]; then
  report "roster: the unsplit reviewer is gone" 0 "agents/reviewer.md still present"
else
  report "roster: the unsplit reviewer is gone" 1
fi

# reviewer-blind must NOT declare memory -- memory would re-grant Write/Edit,
# and a remembered narrative is a narrative.
for a in reviewer-blind scope-warden; do
  if grep -qE '^memory:' "$REPO_ROOT/.claude/agents/$a.md" 2>/dev/null; then
    report "roster: $a declares no memory" 0 \
      "a memory: declaration re-enables Read/Write/Edit and widens the grant this charter depends on lacking"
  else
    report "roster: $a declares no memory" 1
  fi
done

# ===========================================================================
git -C "$MAIN" worktree remove --force "$PG_TMP/wt" >/dev/null 2>&1 || true
rm -rf "$PG_TMP"

if [ "${PHASEGATES_STANDALONE:-0}" = "1" ]; then
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
