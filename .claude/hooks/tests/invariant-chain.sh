#!/usr/bin/env bash
#
# Tests for the stage-3 hooks:
#
#   validate-phase-artifact.sh   SubagentStop, phase agents   (blocks on shape)
#   enforce-review-verdict.sh    PreToolUse, Bash             (the commit gate)
#   session-logger.sh            SubagentStop                 (pass-cost rows)
#
# Sourced by run.sh; also runs standalone:
#
#   bash .claude/hooks/tests/invariant-chain.sh
#
# The commit gate is tested against a real git repository with real commits,
# because its whole contract is "a PASS for THIS HEAD" -- a fixture with no
# shas could not tell a fresh verdict from a stale one, which is the only
# distinction that matters.
#
# NOTE on the merge-command literals below. enforce-review-verdict.sh matches
# on command *text*, so a literal `gh pr merge` anywhere in a Bash command --
# including inside a heredoc that merely writes a file -- trips it. That is
# the same fail-closed shape as the four Bash hooks that predate it, and
# fail-closed is the right direction for a gate on merging. The literals here
# are assembled at runtime so this file can be edited through a shell without
# the editor's own command being refused.

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
  IC_STANDALONE=1
fi

IC_HOOKS="$REPO_ROOT/.claude/hooks"
IC_TMP="$(mktemp -d)"
IC_ERR="$IC_TMP/stderr.log"
IC_OUT="$IC_TMP/stdout.log"

# Assembled rather than written literally -- see the NOTE in the header.
IC_MERGE_CMD="gh pr ""merge"" 12 --squash"

ic_json() { python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$1"; }

ic_fire() {
  local hook="$1" project_dir="$2" body="$3" rc=0
  printf '%s' "$body" \
    | CLAUDE_PROJECT_DIR="$project_dir" bash "$IC_HOOKS/$hook" >"$IC_OUT" 2>"$IC_ERR" || rc=$?
  printf '%s' "$rc"
}

ic_expect() {
  local label="$1" want="$2" got="$3" needle="${4:-}"
  local out; out="$(cat "$IC_ERR" "$IC_OUT" 2>/dev/null)"
  if [ "$got" != "$want" ]; then
    report "$label" 0 "expected exit $want, got $got -- ${out%%$'\n'*}"
    return
  fi
  if [ -n "$needle" ]; then
    case "$out" in
      *"$needle"*) report "$label" 1 ;;
      *) report "$label" 0 "exit was right but output did not mention '$needle' -- ${out:-<empty>}" ;;
    esac
    return
  fi
  report "$label" 1
}

# ===========================================================================
# validate-phase-artifact.sh
# ===========================================================================
IC_V="$IC_TMP/validate"
mkdir -p "$IC_V/.squad/design/demo"
V() { printf '{"agent_type":"%s","cwd":%s,"stop_hook_active":false}' "$1" "$(ic_json "$IC_V")"; }
TRAIL="$(python3 -c 'print("Totality is forced by the requirement that every event be accounted for; from there the fold is forced, and the identity element follows from the empty history. "*3)')"

printf '# Track A\n## Candidate A1: x\n## Candidate A2: y\n' > "$IC_V/.squad/design/demo/01-track-a.md"
ic_expect "validator: 01 without a Reasoning Trail is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-first-principles)")" \
  "Reasoning Trail"

printf '# Track A\n## Candidate A1: x\n## Candidate A2: y\n## Reasoning Trail\n%s\n' "$TRAIL" \
  > "$IC_V/.squad/design/demo/01-track-a.md"
ic_expect "validator: 01 with a Reasoning Trail passes" 0 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-first-principles)")" ""

printf '# Track A\n## Candidate A1: x\n## Candidate A2: y\n## Reasoning Trail\nBecause.\n' \
  > "$IC_V/.squad/design/demo/01-track-a.md"
ic_expect "validator: a Reasoning Trail heading with nothing under it is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-first-principles)")" \
  "almost empty"

printf '# Track A\n## Candidate A1: x\n## Reasoning Trail\n%s\n' "$TRAIL" \
  > "$IC_V/.squad/design/demo/01-track-a.md"
ic_expect "validator: 01 with a single candidate is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-first-principles)")" \
  "Fewer than two candidates"

printf '# Track B\n## Candidates\nSee https://example.com/paper\n' > "$IC_V/.squad/design/demo/02-track-b.md"
ic_expect "validator: 02 without Known Failure Modes is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-informed)")" \
  "Known Failure Modes"

printf '# Track B\n## Known Failure Modes\n- unbounded growth\nSee https://example.com/paper\n' \
  > "$IC_V/.squad/design/demo/02-track-b.md"
ic_expect "validator: 02 with failure modes and a citation passes" 0 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-informed)")" ""

printf '# Track B\n## Known Failure Modes\n- unbounded growth\n' > "$IC_V/.squad/design/demo/02-track-b.md"
ic_expect "validator: 02 with no citation anywhere is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V dreamer-informed)")" \
  "wearing"

printf '# Critic\n## Verdict\nAPPROVE\n### 🔴 Blockers\n#### Might not scale\nProbably slow.\n' \
  > "$IC_V/.squad/design/demo/05-critic.md"
ic_expect "validator: a blocker with no failure scenario is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V critic)")" \
  "not a finding"

printf '# Critic\n## Verdict\nAPPROVE\n### 🔴 Blockers\n#### Replay drops events\nFailure scenario: given three events sharing a key, when replayed, then the last write wins and the audit trail is lost.\n' \
  > "$IC_V/.squad/design/demo/05-critic.md"
ic_expect "validator: a blocker with a concrete failure scenario passes" 0 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V critic)")" ""

printf '# Scope\n## Invariants\n- **INV-1:** totality\n- **INV-2:** monotonicity\n' \
  > "$IC_V/.squad/design/demo/00-scope.md"

printf '# Spec\n## Acceptance layer (example-based)\nhappy path\n## Invariant layer (property-based)\nINV_1_totality_holds\n' \
  > "$IC_V/.squad/design/demo/06-spec.md"
ic_expect "validator: 06 with an uncovered INV-n is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V spec-author)")" \
  "INV-2"

printf '# Spec\n## Acceptance layer (example-based)\nhappy path\n## Invariant layer (property-based)\nINV_1_totality_holds and INV_2_monotonicity_holds\n' \
  > "$IC_V/.squad/design/demo/06-spec.md"
ic_expect "validator: 06 covering every INV-n passes" 0 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V spec-author)")" ""

printf '# Spec\n## Acceptance\nINV-1 and INV-2 are mentioned but there is no property layer here.\n' \
  > "$IC_V/.squad/design/demo/06-spec.md"
ic_expect "validator: 06 naming the ids in prose with no property layer is refused" 2 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V spec-author)")" \
  "no property layer"

ic_expect "validator: an agent it does not govern passes through" 0 \
  "$(ic_fire validate-phase-artifact.sh "$IC_V" "$(V realist)")" ""

# ===========================================================================
# enforce-review-verdict.sh -- the commit gate, against a real repository
# ===========================================================================
IC_G="$IC_TMP/gate"
mkdir -p "$IC_G/.squad" "$IC_G/src"
printf 'class A {}\n' > "$IC_G/src/A.cs"
printf '# readme\n' > "$IC_G/README.md"
gate_ready=0
if git -C "$IC_G" init -q 2>/dev/null \
   && git -C "$IC_G" config user.email t@t && git -C "$IC_G" config user.name t \
   && git -C "$IC_G" config commit.gpgsign false \
   && git -C "$IC_G" add -A 2>/dev/null \
   && git -C "$IC_G" commit -qm init 2>/dev/null; then
  gate_ready=1
else
  report "commit-gate fixture: created" 0 "git init/commit failed"
fi

if [ "$gate_ready" = "1" ]; then
  G() { printf '{"cwd":%s,"tool_name":"Bash","tool_input":{"command":%s}}' \
          "$(ic_json "$IC_G")" "$(ic_json "$1")"; }
  HEAD_SHA="$(git -C "$IC_G" rev-parse HEAD)"
  git -C "$IC_G" branch -m main >/dev/null 2>&1 || true

  printf 'class B {}\n' > "$IC_G/src/B.cs"
  git -C "$IC_G" add src/B.cs

  ic_expect "commit-gate: committing code with no verdict at all is refused" 2 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git commit -m x')")" \
    "no verdict has been recorded"

  printf 'PASS\n' > "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: a PASS naming no commit is refused" 2 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git commit -m x')")" \
    "records no commit"

  printf 'PASS\ncommit: 0000000000000000000000000000000000000000\n' > "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: a PASS for a different commit is refused" 2 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git commit -m x')")" \
    "was recorded for"

  printf 'NEEDS-CHANGES\ncommit: %s\n' "$HEAD_SHA" > "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: NEEDS-CHANGES for this HEAD is refused" 2 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git commit -m x')")" \
    "not PASS"

  printf 'PASS\ncommit: %s\n' "$HEAD_SHA" > "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: a PASS for this HEAD allows the commit" 0 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git commit -m x')")" ""

  # A docs-only commit is not what this gate is for. A gate that fires on
  # every commit is a gate somebody disables.
  git -C "$IC_G" reset -q
  printf 'more\n' >> "$IC_G/README.md"
  git -C "$IC_G" add README.md
  rm -f "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: a docs-only commit is not gated" 0 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git commit -m docs')")" ""

  git -C "$IC_G" reset -q
  git -C "$IC_G" add src/B.cs
  printf 'PASS\ncommit: %s\n' "$HEAD_SHA" > "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: pushing to main with a current PASS is allowed" 0 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git push origin main')")" ""

  printf 'PASS\ncommit: 0000000000000000000000000000000000000000\n' > "$IC_G/.squad/.last-review-verdict"
  ic_expect "commit-gate: pushing to main with a stale PASS is refused" 2 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git push origin main')")" \
    "Pushing to 'main'"

  ic_expect "commit-gate: merging a PR with a stale PASS is refused" 2 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G "$IC_MERGE_CMD")")" \
    "Merging a pull request"

  git -C "$IC_G" checkout -q -b feature/1-x
  ic_expect "commit-gate: pushing a feature branch is not gated" 0 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git push origin feature/1-x')")" ""
  ic_expect "commit-gate: an unrelated Bash command passes through" 0 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'ls -la')")" ""
  ic_expect "commit-gate: git status passes through" 0 \
    "$(ic_fire enforce-review-verdict.sh "$IC_G" "$(G 'git status --short')")" ""

  # The gate must read the verdict of the repository it is called in, not the
  # one CLAUDE_PROJECT_DIR points at.
  git -C "$IC_G" checkout -q main 2>/dev/null || true
  if git -C "$IC_G" worktree add -q -b icwt "$IC_TMP/icwt" 2>/dev/null; then
    mkdir -p "$IC_TMP/icwt/.squad"
    printf 'PASS\ncommit: 0000000000000000000000000000000000000000\n' \
      > "$IC_TMP/icwt/.squad/.last-review-verdict"
    printf 'class C {}\n' > "$IC_TMP/icwt/src/C.cs"
    git -C "$IC_TMP/icwt" add src/C.cs 2>/dev/null
    ic_expect "commit-gate: reads the worktree's verdict, not CLAUDE_PROJECT_DIR's" 2 \
      "$(ic_fire enforce-review-verdict.sh "$IC_G" \
          "$(printf '{"cwd":%s,"tool_name":"Bash","tool_input":{"command":"git commit -m x"}}' \
             "$(ic_json "$IC_TMP/icwt")")")" \
      "was recorded for"
    git -C "$IC_G" worktree remove --force "$IC_TMP/icwt" >/dev/null 2>&1 || true
  fi
fi

# ===========================================================================
# session-logger.sh -- pass-cost rows
# ===========================================================================
IC_L="$IC_TMP/logger"
mkdir -p "$IC_L/.squad/design/demo"
: > "$IC_L/.squad/design/demo/01-track-a.md"
{
  printf '{"role":"assistant","timestamp":"2026-09-03T10:00:00Z","message":{"content":[{"type":"text","text":"start"}]}}\n'
  printf '{"role":"assistant","timestamp":"2026-09-03T10:07:31Z","message":{"content":[{"type":"text","text":"done"}]}}\n'
} > "$IC_L/transcript.jsonl"

L() { printf '{"stop_hook_active":false,"cwd":%s,"session_id":"s1","transcript_path":%s,"agent_id":"a1","agent_type":"%s"}' \
        "$(ic_json "$IC_L")" "$(ic_json "$IC_L/transcript.jsonl")" "$1"; }

ic_fire session-logger.sh "$IC_L" "$(L dreamer-first-principles)" >/dev/null
if grep -q 'dreamer-first-principles' "$IC_L/.squad/log/pass-cost.md" 2>/dev/null; then
  report "pass-cost: a phase agent is recorded" 1
else
  report "pass-cost: a phase agent is recorded" 0 "no row in .squad/log/pass-cost.md"
fi
if grep -q '| demo |' "$IC_L/.squad/log/pass-cost.md" 2>/dev/null; then
  report "pass-cost: the row is keyed by slug" 1
else
  report "pass-cost: the row is keyed by slug" 0 "slug column did not read 'demo'"
fi
if grep -q '7m 31s' "$IC_L/.squad/log/pass-cost.md" 2>/dev/null; then
  report "pass-cost: wall-clock comes from the transcript timestamps" 1
else
  report "pass-cost: wall-clock comes from the transcript timestamps" 0 \
    "expected '7m 31s', got: $(tail -n1 "$IC_L/.squad/log/pass-cost.md" 2>/dev/null)"
fi

ic_fire session-logger.sh "$IC_L" "$(L csharp-dev)" >/dev/null
if grep -q 'csharp-dev' "$IC_L/.squad/log/pass-cost.md" 2>/dev/null; then
  report "pass-cost: a builder is not recorded" 0 \
    "builders run in worktrees on their own schedule; their cost is a different question"
else
  report "pass-cost: a builder is not recorded" 1
fi

# ===========================================================================
# Structural checks for stage 3
# ===========================================================================
for h in validate-phase-artifact.sh enforce-review-verdict.sh; do
  if grep -v '^[[:space:]]*#' "$IC_HOOKS/$h" | grep -q 'CLAUDE_PROJECT_DIR'; then
    report "path resolution: $h does not read CLAUDE_PROJECT_DIR" 0 \
      "found a live CLAUDE_PROJECT_DIR reference -- hook paths do not follow a worktree"
  else
    report "path resolution: $h does not read CLAUDE_PROJECT_DIR" 1
  fi
done

if [ -f "$REPO_ROOT/.claude/agents/curator-adversary.md" ]; then
  report "roster: curator-adversary charter exists" 1
else
  report "roster: curator-adversary charter exists" 0 "not found"
fi
if grep -qE '^memory:' "$REPO_ROOT/.claude/agents/curator-adversary.md" 2>/dev/null; then
  report "roster: curator-adversary declares no memory" 0 \
    "an adversary that remembers which proposals it liked is no longer adversarial"
else
  report "roster: curator-adversary declares no memory" 1
fi

for a in csharp-dev js-dev devops; do
  if grep -qE '^isolation:[[:space:]]*worktree' "$REPO_ROOT/.claude/agents/$a.md"; then
    report "worktrees: $a declares isolation: worktree" 1
  else
    report "worktrees: $a declares isolation: worktree" 0 "frontmatter missing isolation: worktree"
  fi
done

if python3 -c "
import json,sys
d=json.load(open('$REPO_ROOT/.claude/settings.json'))
sys.exit(0 if d.get('worktree',{}).get('baseRef')=='head' else 1)"; then
  report "worktrees: settings.json sets worktree.baseRef = head" 1
else
  report "worktrees: settings.json sets worktree.baseRef = head" 0 \
    "builders would branch from the default branch instead of the work in progress"
fi

# The curator's self-counter-argument requirement is gone (WP-10).
if grep -q 'strongest argument against adopting it' "$REPO_ROOT/.claude/agents/curator.md" 2>/dev/null; then
  report "curation: the curator no longer argues against itself" 0 \
    "the self-counter-argument requirement is still in agents/curator.md"
else
  report "curation: the curator no longer argues against itself" 1
fi

# Phase agents are told to flag rather than pause (WP-10).
if grep -q 'Phase agents (dreamers, convergence, realist, critic, spec-author)' \
     "$REPO_ROOT/.claude/docs/principles-enforcement.md" 2>/dev/null; then
  report "enforcement: phase agents have their own entry (flag, do not pause)" 1
else
  report "enforcement: phase agents have their own entry (flag, do not pause)" 0 \
    "the directive still tells every agent to stop and wait while the phase charters forbid it"
fi

# Every populated notebook has a live charter declaring memory (WP-11).
undeclared="$(python3 - "$REPO_ROOT" <<'PY'
import pathlib, re, sys
root = pathlib.Path(sys.argv[1])
decl = {}
for f in sorted((root / ".claude" / "agents").glob("*.md")):
    t = f.read_text(encoding="utf-8", errors="replace").split("---")[1]
    n = re.search(r"^name:\s*(.+)$", t, re.M)
    m = re.search(r"^memory:\s*(.+)$", t, re.M)
    if n:
        decl[n.group(1).strip()] = bool(m)
bad = []
mem = root / ".claude" / "agent-memory"
if mem.is_dir():
    for d in sorted(p for p in mem.iterdir() if p.is_dir()):
        if not list(d.glob("*.md")):
            continue
        if not decl.get(d.name):
            bad.append(d.name)
print(",".join(bad))
PY
)"
if [ -z "$undeclared" ]; then
  report "memory: every populated notebook has a charter declaring memory" 1
else
  report "memory: every populated notebook has a charter declaring memory" 0 \
    "populated but never read back (memory is opt-in): $undeclared"
fi

# ===========================================================================
# scope-warden's Write is confined to its own report.
#
# The restriction is a claim about an OBJECT -- it cannot write the thing it
# judges -- not about a capability. It has to be, because it must produce
# 00-warden.md and cannot do that with no Write at all. An earlier charter
# stated it as "cannot write", which was broader than its own reason and left
# the agent unable to do its job; the contradiction sat in the charter through
# four review passes with a passing test behind it. See
# .claude/docs/flow-changelog.md (2026-09-04) and the "Why You Cannot Write
# What You Judge" section of .claude/agents/scope-warden.md.
#
# So: the report is allowed, the artifact it judges is denied, and an arbitrary
# path is denied. If the first of these ever fails, gate 1 has no artifact. If
# either of the others ever passes, the warden has become a co-author.
# ===========================================================================
IC_W="$IC_TMP/warden"
mkdir -p "$IC_W/.squad/design/demo"
WJ() { printf '{"agent_type":"scope-warden","cwd":%s,"tool_name":"Write","tool_input":{"file_path":"%s"}}' \
         "$(ic_json "$IC_W")" "$1"; }

ic_expect "warden: may write its own report" 0 \
  "$(ic_fire enforce-reviewer-readonly.sh "$IC_W" "$(WJ ".squad/design/demo/00-warden.md")")" ""
ic_expect "warden: may NOT write the scope it judges" 2 \
  "$(ic_fire enforce-reviewer-readonly.sh "$IC_W" "$(WJ ".squad/design/demo/00-scope.md")")" \
  "may not write outside its own outputs"
ic_expect "warden: may NOT write an arbitrary path" 2 \
  "$(ic_fire enforce-reviewer-readonly.sh "$IC_W" "$(WJ "src/Order.cs")")" ""
ic_expect "warden: may NOT write the hook's own mechanical scan" 2 \
  "$(ic_fire enforce-reviewer-readonly.sh "$IC_W" "$(WJ ".squad/design/demo/00-warden-scan.md")")" ""
# Edit is confined to the same object as Write, so the warden may Edit its own
# report and may not Edit the scope. The label says which of those is asserted:
# an earlier version read "may NOT edit, only write its report", which is false
# -- Edit on 00-warden.md returns 0. Behaviour right, label wrong, in a repair
# whose subject was that exact class.
ic_expect "warden: may NOT edit the scope it judges" 2 \
  "$(ic_fire enforce-reviewer-readonly.sh "$IC_W" "$(printf '{"agent_type":"scope-warden","cwd":%s,"tool_name":"Edit","tool_input":{"file_path":".squad/design/demo/00-scope.md"}}' "$(ic_json "$IC_W")")")" ""
ic_expect "warden: MAY edit its own report" 0 \
  "$(ic_fire enforce-reviewer-readonly.sh "$IC_W" "$(printf '{"agent_type":"scope-warden","cwd":%s,"tool_name":"Edit","tool_input":{"file_path":".squad/design/demo/00-warden.md"}}' "$(ic_json "$IC_W")")")" ""

# The grant and the confinement have to agree: Write present in the charter,
# no memory: declaration widening it behind the hook's back.
if grep -qE '^tools:.*\bWrite\b' "$REPO_ROOT/.claude/agents/scope-warden.md"; then
  report "warden: charter grants Write (it must produce its own report)" 1
else
  report "warden: charter grants Write (it must produce its own report)" 0 \
    "no Write in the tool grant -- the agent cannot produce 00-warden.md and gate 1 has no artifact"
fi
if grep -qE '^memory:' "$REPO_ROOT/.claude/agents/scope-warden.md"; then
  report "warden: declares no memory (nothing widens the grant)" 0 \
    "memory: re-enables Read/Write/Edit regardless of tools:, behind the hook's back"
else
  report "warden: declares no memory (nothing widens the grant)" 1
fi

# ===========================================================================
# The wiring itself.
#
# Every assertion in this suite invokes a hook script DIRECTLY. None of them
# goes through settings.json, so an unwired hook tests identically to a wired
# one -- proved, not supposed: dropping all twelve PreToolUse entries left the
# suite at 261 passed, 0 failed. Every hook-backed invariant in this design was
# therefore unverified in CI, including the one confining the reviewers` and
# the warden`s writes.
#
# These assertions close that. They are deliberately bidirectional and derive
# their expectations rather than listing them, so there is nothing to keep in
# step:
#
#   - a script is expected to be wired unless its own header says NOT A HOOK
#     (git-history-namestatus.sh is the only one, and says so at line 3)
#   - every command in settings.json must point at a file that exists
#   - the enforcement hooks must be wired at the event AND under a matcher that
#     covers the tool they guard -- a hook on the wrong matcher is unwired with
#     extra steps, which is exactly what the fourth mutation did
# ===========================================================================
WIRING="$(python3 - "$REPO_ROOT" <<'PYEOF'
import json, os, pathlib, re, sys
root = pathlib.Path(sys.argv[1])
cfg = json.loads((root / ".claude" / "settings.json").read_text())
hooks = cfg.get("hooks", {})

wired = {}          # basename -> list of (event, matcher)
cmd_paths = {}      # basename -> list of (command string, event)
for event, matchers in hooks.items():
    for m in matchers:
        for entry in m.get("hooks", []):
            cmd = entry.get("command", "")
            base = cmd.split("/")[-1]
            wired.setdefault(base, []).append((event, m.get("matcher", "")))
            cmd_paths.setdefault(base, []).append((cmd, event))

hd = root / ".claude" / "hooks"
scripts = sorted(p for p in hd.iterdir()
                 if p.is_file() and p.suffix in (".sh", ".py"))
for p in scripts:
    head = "".join(p.open(encoding="utf-8", errors="replace").readlines()[:20])
    # A deliberate declaration on its own comment line, not the phrase
    # appearing anywhere in the header. Adding
    #   # (Strictly speaking this is not a hook contract, just a guard.)
    # to a real hook exempted it and printed a green line affirming the
    # exemption -- a fail-open in the check written to catch fail-opens.
    exempt = bool(re.search(r"^#\s*NOT A HOOK\b", head, re.M | re.I))
    print("SCRIPT\t%s\t%s\t%s" % (p.name, "exempt" if exempt else "expected",
                                    "wired" if p.name in wired else "unwired"))

# Alternative coverage, derived rather than audited. Every alternative of every
# matcher must have a check_place pin. Without this the 38-of-38 census is a
# snapshot taken by hand: adding an alternative with no pin passed silently,
# which is how an unpinned NotebookRead let a dreamer read through a notebook.
pin_re = re.compile(r"^\s*check_place\s+(\S+)\s+(\S+)\s+(\S+)", re.M)
pins = set()
self_src = (root / ".claude" / "hooks" / "tests" / "invariant-chain.sh").read_text(
    encoding="utf-8", errors="replace")
for m in pin_re.finditer(self_src):
    pins.add((m.group(1), m.group(2), m.group(3).strip("'\"")))

for sc, ev, tl in sorted(pins):
    print("PIN\t%s\t%s\t%s" % (sc, ev, tl))

for event, matchers in hooks.items():
    for mm in matchers:
        for entry in mm.get("hooks", []):
            b = entry.get("command", "").split("/")[-1]
            alts = [a.strip() for a in (mm.get("matcher") or "").split("|") if a.strip()]
            if not alts:
                print("ALT\t%s\t%s\t%s\t%s" % (b, event, "(no matcher)",
                      "pinned" if (b, event, "ANY") in pins else "UNPINNED"))
                continue
            for a in alts:
                print("ALT\t%s\t%s\t%s\t%s" % (b, event, a,
                      "pinned" if (b, event, a) in pins else "UNPINNED"))

for base, places in sorted(wired.items()):
    # Resolve the command's own path with $CLAUDE_PROJECT_DIR expanded, not a
    # basename inside .claude/hooks/ -- a command repointed elsewhere with a
    # decoy left behind used to pass.
    def resolve(cmd):
        # The command may carry an interpreter -- `python3 <path>`. Take the
        # token that names a path, not the whole string.
        tok = next((t for t in cmd.split() if "/" in t), cmd)
        tok = tok.replace("${CLAUDE_PROJECT_DIR}/", "").replace("$CLAUDE_PROJECT_DIR/", "")
        return root / os.path.normpath(tok)

    # Two conditions, not one. The file must exist AND it must be the script
    # in .claude/hooks/ -- repointing a command at a copy elsewhere and leaving
    # the original in place satisfied an existence check alone.
    def good(c):
        r = resolve(c)
        return r.is_file() and r.parent == hd

    cmds = {c for c, _ in cmd_paths.get(base, [])}
    ok = bool(cmds) and all(good(c) for c in cmds)
    print("ENTRY\t%s\t%s" % (base, "present" if ok else "MISPOINTED"))
    for event, matcher in places:
        print("PLACE\t%s\t%s\t%s" % (base, event, matcher))
PYEOF
)"

while IFS=$'\t' read -r kind name a b c; do
  case "$kind" in
    SCRIPT)
      if [ "$a" = "exempt" ]; then
        report "wiring: $name declares NOT A HOOK and is exempt" 1
      elif [ "$b" = "wired" ]; then
        report "wiring: $name is wired in settings.json" 1
      else
        report "wiring: $name is wired in settings.json" 0 \
          "the script exists and does not declare NOT A HOOK, but nothing in settings.json runs it -- an unwired hook passes every direct test in this suite"
      fi ;;
    ALT)
      if [ "$c" = "pinned" ]; then
        report "coverage: $name $a matcher alternative '$b' has a pin" 1
      else
        report "coverage: $name $a matcher alternative '$b' has a pin" 0 \
          "settings.json wires $name at $a under an alternative with no check_place pin -- the alternative can be dropped silently, which is the eighth shape returning"
      fi ;;
    ENTRY)
      if [ "$a" = "present" ]; then
        report "wiring: settings.json entry $name points at a real file" 1
      else
        report "wiring: settings.json entry $name points at a real file" 0 \
          "settings.json runs $name from a path that is not .claude/hooks/$name -- either the file is absent, or the command was repointed at a copy elsewhere"
      fi ;;
  esac
done <<< "$WIRING"

# Event and matcher, for the hooks whose placement is load-bearing. A hook on
# the wrong matcher never fires for the tool it guards.
# $3 is required and must be a tool name or the literal ANY. An empty third
# argument used to mean "event only", so a pin whose tool was accidentally ''
# passed while printing "fires on PreToolUse for every invocation" -- a green
# line for a matcher that no longer covered the tool. ${3:?} makes omission
# abort loudly; ANY makes the event-only case deliberate and visible.
check_place() {
  local script="$1" want_event="$2" want_tool="${3:?check_place needs a tool name or ANY}" line found=0
  local any=0
  # Record the invocation. The coverage family parses pins out of this file's
  # SOURCE, so a pin's presence satisfied it whether or not it ran -- moving
  # all 38 into a never-called function left every coverage assertion green
  # and deleted the entire placement family. The executed set is compared to
  # the parsed set below.
  printf '%s\t%s\t%s\n' "$script" "$want_event" "$want_tool" >> "$IC_TMP/pins-executed"
  if [ "$want_tool" = "ANY" ]; then any=1; want_tool=""; fi
  while IFS=$'\t' read -r kind name event matcher; do
    [ "$kind" = "PLACE" ] || continue
    [ "$name" = "$script" ] || continue
    # Whole alternative, not substring: `Edit` is a substring of `MultiEdit`
    # and `Read` of `NotebookRead`, so `grep -q` passed for a matcher that
    # does not cover the tool. Narrowing Write|Edit|MultiEdit|NotebookEdit to
    # Write|MultiEdit left the Edit assertion green.
    if [ "$event" != "$want_event" ]; then
      continue
    fi
    if [ "$any" = "1" ]; then
      # ANY is for hooks wired with NO matcher -- they run on every invocation
      # of the event. It is NOT a way to weaken a pin: if the entry has a
      # matcher, an ANY pin would accept any narrowing of it, which is the
      # fail-open the empty-string form had. So ANY requires the matcher to be
      # genuinely absent.
      if [ -n "$matcher" ]; then
        report "wiring: $script pinned ANY but its $want_event matcher is '$matcher'" 0 \
          "ANY means the entry has no matcher and fires on every invocation. This one has one, so the pin accepts any narrowing of it -- name the tool instead."
        return
      fi
      found=1
    elif printf '%s' "$matcher" | tr '|' '\n' | sed 's/^ *//;s/ *$//' | grep -qxF "$want_tool"; then
      found=1
    fi
  done <<< "$WIRING"
  local label="wiring: $script fires on $want_event for ${want_tool:-every invocation}"
  if [ "$found" = "1" ]; then
    report "$label" 1
  else
    report "$label" 0 \
      "not wired at $want_event${want_tool:+ under a matcher covering $want_tool} -- the hook is unwired with extra steps"
  fi
}

check_place enforce-track-blindness.sh        PreToolUse   Read
check_place enforce-review-blindness.sh       PreToolUse   Read
check_place enforce-reviewer-readonly.sh      PreToolUse   Write
check_place enforce-reviewer-readonly.sh      PreToolUse   Edit
check_place enforce-phase-order.sh            PreToolUse   Write
check_place enforce-review-verdict.sh         PreToolUse   Bash
check_place enforce-review-history-channel.sh PreToolUse   Bash
check_place lexicon-check.sh                  SubagentStop dreamer-first-principles
check_place scope-warden.sh                   SubagentStop architect
check_place validate-phase-artifact.sh        SubagentStop spec-author
check_place block-direct-commits-to-main.sh   PreToolUse   Bash
check_place enforce-conventional-commits.sh   PreToolUse   Bash
check_place enforce-gpg-signing.sh            PreToolUse   Bash
check_place enforce-no-secrets.sh             PreToolUse   Bash
check_place block-large-files.sh              PreToolUse   Bash
check_place validate-branch-name.sh           PreToolUse   Bash
check_place enforce-track-blindness.sh        PreToolUse   Grep
check_place enforce-review-blindness.sh       PreToolUse   Glob
check_place enforce-reviewer-readonly.sh      PreToolUse   MultiEdit
check_place validate-phase-artifact.sh        SubagentStop dreamer-first-principles
check_place validate-phase-artifact.sh        SubagentStop dreamer-informed
check_place validate-phase-artifact.sh        SubagentStop critic
check_place enforce-track-blindness.sh        PreToolUse   Glob
check_place enforce-track-blindness.sh        PreToolUse   NotebookRead
check_place enforce-track-blindness.sh        PreToolUse   'mcp__.*'
check_place enforce-review-blindness.sh       PreToolUse   Grep
check_place enforce-review-blindness.sh       PreToolUse   NotebookRead
check_place enforce-review-blindness.sh       PreToolUse   'mcp__.*'
check_place enforce-reviewer-readonly.sh      PreToolUse   NotebookEdit
# Deletion is only loud where a hook is pinned. These six were unpinned, so
# removing them from disk and from settings.json together passed as merely
# consistent. check_place already exists; the reviewer priced this at one line
# each, which makes recording it instead of building it the wrong answer.
check_place session-context-loader.py         SessionStart     ANY
check_place skill-router.py                   UserPromptSubmit ANY
check_place dotnet-format-on-save.sh          PostToolUse      Write
check_place dotnet-format-on-save.sh          PostToolUse      Edit
check_place dotnet-format-on-save.sh          PostToolUse      MultiEdit
check_place precompact-snapshot.py            PreCompact       ANY
check_place session-logger.sh                 SubagentStop     ANY
check_place squad-rotate.py                   SessionEnd       ANY
check_place scribe-decision-merger.sh         SubagentStop ANY

# No two check_place invocations may be identical. A duplicate pins nothing
# and occupies the slot a real pin would have taken: :581 was byte-identical
# to :569, so the artifact validator went unpinned for BOTH dreamer tracks and
# narrowing its matcher to `critic|spec-author` passed at 327/0. The family
# read as 66 assertions and was 65 distinct.
#
# This makes the next one the suite's problem rather than a reviewer's.
# Normalise BEFORE filtering. The filter was whitespace-exact, so an indented
# or tab-separated duplicate never reached the normaliser -- three of five
# duplicate spellings passed green while the duplicate itself ran and reported.
# Strip quotes as well as whitespace. `mcp__.*` is the only quoted argument in
# the file -- quoted for exactly the reason someone else might not quote it --
# so a duplicate differing only in quoting escaped a whitespace-only normaliser.
dupe_list="$(sed "s/['\"]//g; s/\t/ /g; s/^[[:space:]]*//; s/[[:space:]]*\$//; s/  */ /g" \
               "$SCRIPT_DIR/invariant-chain.sh" \
             | grep -E '^check_place [a-z]' | sort | uniq -d)"
if [ -z "$dupe_list" ]; then
  report "wiring: no two check_place pins are identical" 1
else
  report "wiring: no two check_place pins are identical" 0 \
    "a duplicate pin occupies the slot a real pin would take and verifies nothing: $(printf '%s' "$dupe_list" | tr '\n' ';')"
fi

# A pin that exists in the source and never runs verifies nothing. Compare the
# set the coverage family parsed against the set that actually executed.
parsed_pins="$(printf '%s\n' "$WIRING" | awk -F'\t' '$1=="PIN"{print $2"\t"$3"\t"$4}' | sort -u)"
executed_pins="$(sort -u "$IC_TMP/pins-executed" 2>/dev/null || true)"
if [ "$parsed_pins" = "$executed_pins" ]; then
  report "coverage: every parsed pin actually executed" 1
else
  only_parsed="$(comm -23 <(printf '%s\n' "$parsed_pins") <(printf '%s\n' "$executed_pins") | tr '\t' ' ' | tr '\n' ';')"
  only_run="$(comm -13 <(printf '%s\n' "$parsed_pins") <(printf '%s\n' "$executed_pins") | tr '\t' ' ' | tr '\n' ';')"
  report "coverage: every parsed pin actually executed" 0 \
    "the coverage family reads pins from this file's source, so an unexecuted pin still satisfies it. In source but never ran: ${only_parsed:-none}. Ran but not parsed: ${only_run:-none}."
fi

# ===========================================================================
# Appendix A of the flow specification -- maintained outside this repository;
# .claude/docs/flow-changelog.md is the in-repo companion -- specifies the file
# layout below. A missing directory is a real
# gap, and the two log files below have to exist BEFORE the first pass runs
# rather than being created lazily on first append -- a measurement that only
# starts existing once somebody goes looking for it is a measurement nobody
# takes, and an override log that appears after the first false positive has
# already lost the evidence for narrowing the term.
# ===========================================================================
for p in .claude/agents .claude/commands .claude/skills .claude/hooks \
         .claude/docs/decisions.md .claude/docs/decision-schema.md \
         .claude/docs/principles-enforcement.md .claude/docs/memory-policy.md \
         .claude/docs/tech-stack.md .claude/docs/pattern-lexicon.md \
         .claude/agent-memory .claude/settings.json \
         .squad/learnings/inbox .squad/learnings/archive \
         .squad/log/lexicon-hits.md .squad/log/pass-cost.md; do
  if [ -e "$REPO_ROOT/$p" ]; then
    report "layout: $p exists" 1
  else
    report "layout: $p exists" 0 \
      "Appendix A of the flow specification lists this path and it is absent. That specification is maintained outside this repository; see .claude/docs/flow-changelog.md and .claude/docs/memory-policy.md for the in-repo account of the layout."
  fi
done

# The artifact contract and the charters are two statements of the same thing,
# and they drift apart first. Every artifact the flow produces must appear in
# the WRITES column of the contract table in beast-mode-design.
#
# The table specifically, not the file. The first version of these assertions
# grepped the whole SKILL.md, and nine of twelve matched prose elsewhere in it:
# deleting the entire Realist row left `contract: 04-realist-plan` green,
# because rule 5 says "a re-run of `realist` replaces `04-realist-plan.md`".
# An assertion that cannot fail on the mistake it names is worse than no
# assertion -- it converts an unchecked claim into a checked-looking one.
#
# The table has 12 rows naming 13 artifacts: row 1's Writes cell holds both
# `00-scope.md` and `00-knowledge.md`. That arithmetic matters for what the
# mutation testing can and cannot show, so it is stated rather than rounded:
#
#   - Row deleted, and Writes cell blanked with the row and its Reads cell
#     intact: 12 rows x 2 kinds = 24 distinct file states, each confirmed
#     failing the right assertion and then restored.
#   - Deleting row 1 fails TWO assertions together. Neither of those two
#     mutation kinds can test `00-scope` and `00-knowledge` individually.
#   - A third mutation does: remove one artifact from the shared cell and
#     leave the other. That was run, and fails exactly one.
#
# So: 24 file states, 13 artifacts, 11 of them individually isolable by the
# two named mutations and 2 needing the third. Not "all twenty-six
# individually", which an earlier version of this comment claimed.
#
# THOSE FIVE NUMBERS ARE PROSE AND NOTHING CHECKS THEM. Adding a 14th artifact
# to both the table and CONTRACT_LOOP -- the correct maintenance path, the one
# the reverse assertion's own remedy string instructs -- passes 263/0 and
# leaves counts in this paragraph stale. If you take that path, re-derive
# them; the arithmetic is rows, artifacts, rows x 2, and how many artifacts
# share a Writes cell.
#
# They stay prose deliberately, and the reason is the shape of the available
# check rather than the cost of the mistake. Asserting a constant (`rows == 12`)
# is the same maintenance with a red light, and goes red on legitimate growth.
# Asserting against this comment's own text is self-maintaining but fires on a
# harmless reword, and misses four of flow-changelog.md's five copies because
# they are spelled as words (the fifth, "24 distinct file states", is a
# numeral, and an extractor that catches it also false-positives on
# "invariant 11"). A check that fires on rewording and misses the
# other copy teaches people to edit the check. That is worse than stale prose
# beside a green suite. Settled; please do not re-litigate it without a check
# that avoids both failure modes.
#
# There is no ASSERTION count to maintain here any more. The reverse assertion
# below fails if the table names an artifact this loop does not check, so the
# loop list is the assertion count and drift in either direction is caught
# rather than remembered.
#
# The Writes column, not the row: a row can name an artifact in its Reads cell
# while claiming to write nothing, which is how the Gate 1 mutation slipped
# through the first rewrite too.
CONTRACT="$REPO_ROOT/.claude/skills/beast-mode-design/SKILL.md"

# Extract the Writes column (field 4) of every pipe-delimited row in the
# artifact-contract table -- the block between its header and the first blank
# line after it. Nothing else in the file is consulted.
contract_writes() {
  python3 - "$1" <<'PYEOF'
import re, sys
text = open(sys.argv[1], encoding="utf-8", errors="replace").read()
m = re.search(r"^\| Phase \| Agent \| Writes \| Reads \|\n(?:.*\n)*?(?=\n)", text, re.M)
if not m:
    sys.exit(0)
for line in m.group(0).splitlines():
    if not line.startswith("|"):
        continue
    cells = [c.strip() for c in line.strip().strip("|").split("|")]
    if len(cells) >= 3 and cells[0] not in ("Phase", "---"):
        print(cells[2])
PYEOF
}

CONTRACT_WRITES="$(contract_writes "$CONTRACT")"

# ONE list, read by both loops below. It was two: a forward list and a
# hand-copied duplicate of it, at :522-524 and :545 of the reviewed revision --
# a distance that reads as 21 lines or 23 depending on which end you measure
# from, which is why the line references are given instead of a number. And
# the reverse assertion compared the table against the copy rather than against
# the forward loop -- so it asserted table-subset-of-copy while three shipped
# sentences claimed table-subset-of-loop. Removing an artifact from the forward
# list alone left the reverse assertion reporting `ok` on a sentence that was
# false at that moment, and the suite one assertion lighter with nothing red.
#
# A duplicate is not a check on a check. The variable is the fix: there is
# nothing left for a maintainer to keep in step.
CONTRACT_LOOP="00-knowledge 00-scope 01-track-a 02-track-b 03-convergence \
               04-realist-plan 05-critic 06-spec 07-handoff \
               00-warden-scan 00-warden 08-review-blind 09-review-verdict"

for a in $CONTRACT_LOOP; do
  # Word-boundary match so 00-warden does not satisfy itself via
  # 00-warden-scan.md, and 00-knowledge does not match a Reads cell.
  if printf '%s\n' "$CONTRACT_WRITES" | grep -qE "(^|[^a-z0-9-])$a\.md"; then
    report "contract: $a is in the Writes column of the contract table" 1
  else
    report "contract: $a is in the Writes column of the contract table" 0 \
      "the contract and the charters have drifted -- $a is produced but its row does not claim to write it"
  fi
done

# The reverse direction. Without this the check is one-way: an artifact added
# to the table but not to the loop above is silently unchecked, and the only
# thing keeping the two in step is a maintainer remembering to edit both. That
# is an instruction where a check is available, which is the defect this
# repository keeps finding in itself -- so it is a check.
#
# It also removes the need to maintain a count anywhere: $CONTRACT_LOOP IS the
# count, and this assertion fails if the table outgrows it. Both loops read
# that one variable -- if they ever read two lists again, this assertion is a
# duplicate rather than a check.
for w in $(printf '%s\n' "$CONTRACT_WRITES" | grep -oE '[0-9]{2}-[a-z-]+\.md' | sort -u); do
  base="${w%.md}"
  case " $(printf '%s' "$CONTRACT_LOOP" | tr -s '[:space:]' ' ') " in
    *" $base "*) report "contract: $base in the table is also checked by the loop" 1 ;;
    *) report "contract: $base in the table is also checked by the loop" 0 \
         "the table names $w but no assertion above checks it -- add $base to the loop list, or the artifact is in the contract and unverified" ;;
  esac
done

# memory-policy.md must name the current artifact set, not a stale prefix of it.
if grep -q '09-review-verdict' "$REPO_ROOT/.claude/docs/memory-policy.md"; then
  report "memory-policy: names the current artifact set" 1
else
  report "memory-policy: names the current artifact set" 0 \
    "still stops short of 09-review-verdict.md"
fi

# The level-choice criterion has to live somewhere an agent will read it, and
# the curation pair has to be pointed at it -- a criterion nobody applies is a
# paragraph, not a rule.
if grep -q 'Choosing the level' "$REPO_ROOT/.claude/docs/principles-enforcement.md"; then
  report "levels: the warning-vs-control criterion is recorded" 1
else
  report "levels: the warning-vs-control criterion is recorded" 0 \
    "nothing says how to decide whether a new rule needs a check or an instruction"
fi
for a in curator curator-adversary; do
  if grep -q 'Choosing the level' "$REPO_ROOT/.claude/agents/$a.md"; then
    report "levels: $a is pointed at the criterion" 1
  else
    report "levels: $a is pointed at the criterion" 0 "no reference to it"
  fi
done

# ===========================================================================
rm -rf "$IC_TMP"

if [ "${IC_STANDALONE:-0}" = "1" ]; then
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
