#!/usr/bin/env bash
#
# Regression + coverage tests for .claude/hooks/scribe-decision-merger.sh.
#
# Run:
#   bash .claude/hooks/tests/run.sh
#
# No dependencies beyond bash 4+ (this suite uses `declare -A`; the hook
# itself only needs bash 3) and python3 (already required by the hook
# itself). No git history requirement: the two pre-fix hook revisions used
# by the regression checks are vendored as fixtures (see
# fixtures-pre-fix/), not read from the commit graph.
#
# agent_identity.py, colocated in this directory, is imported by
# roster_of() and duplicate_names_of() below via a $SCRIPT_DIR-relative
# sys.path insert -- no install step, no PYTHONPATH setup. It is test-only
# tooling (identity resolution for the roster invariant), not sourced by
# the production hook, which has its own, deliberately separate,
# frontmatter-fence handling for its own schema.
#
# What this proves:
#
#   1. The regression tests required by principles-enforcement.md:78 for the
#      two bugs fixed in this pass:
#        a. the nested-mapping list-item parser bug fixed by the revision
#           vendored as
#           fixtures-pre-fix/scribe-decision-merger.pre-nested-mapping-fix.sh
#           ("fix: parse nested-mapping list items in decision-drop YAML").
#        b. the stale ALLOWED_AGENTS whitelist (six Beast Mode phase agents
#           missing) fixed in the commit that follows it.
#      Each is checked against a *vendored copy* of the hook script from the
#      revision where that specific bug was still present, not a live git
#      ref. This repo allows squash and rebase merges, and the whitelist fix
#      commit lives only on this unpushed branch -- post-squash its SHA is
#      unreachable even with a full clone, and a `git show <ref>` regression
#      check would fail loud on `main` blaming a shallow clone. Vendoring
#      the two "before" hook copies as fixtures removes that dependency
#      entirely: no history reachability requirement, no `fetch-depth: 0`
#      needed by this suite specifically. (`actions/checkout` still
#      defaults to a shallow clone; nothing here requires deepening it.)
#      Dynamic discovery (`git log -n 2 -- <file> | tail -1`) has the same
#      problem in a different shape: once the whitelist fix commit lands on
#      top of the nested-mapping-fix revision, "the previous revision of the
#      file" becomes the nested-mapping-fix revision itself -- which already
#      contains the parser fix, so a floating reference would silently stop
#      proving bug (a) ever existed.
#
#   2. Coverage: the 18-case corpus built while reviewing the revision
#      vendored as
#      fixtures-pre-fix/scribe-decision-merger.pre-nested-mapping-fix.sh, each case
#      cross-checked against PyYAML 6.0.3 as ground truth (see case 08 for
#      the one accepted, reviewed divergence). Run through the current hook
#      in a single batch, matching real SubagentStop behaviour (one
#      invocation processes the whole inbox).
#
#   3. Agent-whitelist completeness: a synthetic minimal drop for every
#      Beast Mode phase agent (critic, realist, spec-author,
#      dreamer-first-principles, dreamer-informed, dreamer-convergence),
#      plus one deliberately bogus agent name to prove the whitelist still
#      rejects what it should.
#
#   4. The roster *invariant*, not just the fixed instance: ALLOWED_AGENTS
#      in the hook must equal the file-name roster of .claude/agents/*.md
#      plus `lead`. PR #5 added six agent files without ever touching this
#      hook or its `paths:`-scoped CI job, and section 3 above only proves
#      the six agents *currently* known about are accepted -- it says
#      nothing about the next one. This section drops a throwaway agent
#      file into .claude/agents/ at test time and asserts the suite fails,
#      then removes it, proving the invariant check actually fires rather
#      than only checking today's roster is a subset.
#
#   5. Content coverage the outcome-of-directory check can't see: a hook
#      that archives every drop's *file* but never appends its *content* to
#      decisions.md still passes sections 2/3 (they only check which
#      directory the file landed in). Section 5 greps decisions.md for the
#      drop's id to close that gap, plus dedicated coverage for the LEGACY
#      (no-front-matter) path and the `.last-review-verdict` latest-by-
#      `created` selection, neither of which the corpus/whitelist sections
#      exercise.
#
# Exit code: 0 if every case matches its expected outcome, 1 otherwise, with
# a per-case report on stdout.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
FIXTURES="$SCRIPT_DIR/fixtures"
PRE_FIX="$SCRIPT_DIR/fixtures-pre-fix"
HOOK_REL=".claude/hooks/scribe-decision-merger.sh"
CURRENT_HOOK="$REPO_ROOT/$HOOK_REL"
AGENTS_DIR="$REPO_ROOT/.claude/agents"

# Vendored pre-fix hook revisions -- see header comment for why these are
# fixtures rather than `git show <ref>:<path>` against a commit SHA.
NESTED_MAPPING_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-nested-mapping-fix.sh"  # mis-parsed nested-mapping list items
STALE_WHITELIST_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-whitelist-fix.sh"      # has the parser fix, still missing the 6 phase agents

pass=0
fail=0

report() {
  local name="$1" ok="$2" detail="${3:-}"
  if [ "$ok" = "1" ]; then
    pass=$((pass + 1))
    printf 'ok      %s\n' "$name"
  else
    fail=$((fail + 1))
    printf 'FAIL    %s -- %s\n' "$name" "$detail"
  fi
}

# ---------------------------------------------------------------------------
# Integrity check for the vendored fixtures-pre-fix/ copies. They're
# identified by commit *message* in the header comment above, not content --
# reviewer could only verify byte-identity to the originals by reaching
# 26e985b^/03848dd^ directly, which won't survive a squash merge. Pinning a
# SHA-256 here means drift (or a future edit landing on the "frozen
# regression anchor" files by mistake) fails this suite loudly instead of
# silently, with no git history reachability required to re-verify.
sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | cut -d' ' -f1
  else
    shasum -a 256 "$1" | cut -d' ' -f1
  fi
}

verify_fixture_sha256() {
  local label="$1" path="$2" expected="$3" got
  if [ ! -f "$path" ]; then
    report "fixture integrity: $label" 0 "$path not found"
    return
  fi
  got="$(sha256_of "$path")"
  if [ "$got" = "$expected" ]; then
    report "fixture integrity: $label" 1
  else
    report "fixture integrity: $label" 0 \
      "expected sha256 $expected, got $got -- $path no longer matches the pinned pre-fix revision"
  fi
}

verify_fixture_sha256 \
  "pre-nested-mapping-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-nested-mapping-fix.sh" \
  "faeb96e509bb7767b240a9470827a29ed0d37905775b17f7111b67529f9a5476"
verify_fixture_sha256 \
  "pre-whitelist-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-whitelist-fix.sh" \
  "c0f0f14c86ed736c189f486329c5622793d86927a9994f40ba7445cbc98b6c0d"

# ---------------------------------------------------------------------------
# Sandbox helper: builds a throwaway project dir with a given hook script and
# a given set of inbox fixtures, invokes the hook once (matching real
# SubagentStop batching, which processes the whole inbox per invocation), and
# echoes the sandbox path on stdout.
# ---------------------------------------------------------------------------
run_hook_sandbox() {
  local hook_script="$1"; shift
  local sandbox
  sandbox="$(mktemp -d)"
  mkdir -p "$sandbox/.claude/hooks" "$sandbox/.claude/docs" "$sandbox/.squad/decisions/inbox"
  cp "$hook_script" "$sandbox/.claude/hooks/scribe-decision-merger.sh"
  chmod +x "$sandbox/.claude/hooks/scribe-decision-merger.sh"
  printf '# Decisions\n' > "$sandbox/.claude/docs/decisions.md"
  for f in "$@"; do
    cp "$f" "$sandbox/.squad/decisions/inbox/"
  done
  printf '{"stop_hook_active": false, "cwd": "%s"}' "$sandbox" \
    | CLAUDE_PROJECT_DIR="$sandbox" bash "$sandbox/.claude/hooks/scribe-decision-merger.sh" \
    > "$sandbox/.hook-stdout.log" 2> "$sandbox/.hook-stderr.log"
  printf '%s\n' "$sandbox"
}

# $1 = sandbox, $2 = original basename -> prints ARCHIVED / QUARANTINED / MISSING
outcome_of() {
  local sandbox="$1" base="$2"
  if find "$sandbox/.squad/decisions/archive" -type f -name "*-$base" 2>/dev/null | grep -q .; then
    echo ARCHIVED
  elif find "$sandbox/.squad/decisions/quarantine" -type f -name "*-$base" 2>/dev/null | grep -q .; then
    echo QUARANTINED
  else
    echo MISSING
  fi
}

# $1 = sandbox, $2 = original basename -> prints the .reason file content (or empty)
reason_of() {
  local sandbox="$1" base="$2" f
  f="$(find "$sandbox/.squad/decisions/quarantine" -type f -name "*-$base.reason" 2>/dev/null | head -n1)"
  [ -n "$f" ] && cat "$f"
}

# $1 = human label, $2 = vendored pre-fix hook script,
# $3 = fixture path, $4 = substring expected in the pre-fix hook's .reason
# file. Asserts the pre-fix hook QUARANTINEs the fixture *for the expected
# reason* (proving the bug, not just some unrelated quarantine cause -- the
# staleness mode the failure message at the top of this file warns about:
# vendoring only makes drift loud, not meaningful, without this check) and
# the CURRENT hook ARCHIVEs it (proving the fix).
check_regression() {
  local label="$1" before_hook="$2" fixture="$3" expected_reason="$4" base
  base="$(basename "$fixture")"

  if [ -f "$before_hook" ]; then
    local before_sandbox before_outcome before_reason
    before_sandbox="$(run_hook_sandbox "$before_hook" "$fixture")"
    before_outcome="$(outcome_of "$before_sandbox" "$base")"
    if [ "$before_outcome" = "QUARANTINED" ]; then
      report "regression: $label -- vendored pre-fix hook reproduces the original failure" 1

      before_reason="$(reason_of "$before_sandbox" "$base")"
      case "$before_reason" in
        *"$expected_reason"*)
          report "regression: $label -- vendored pre-fix hook fails for the expected reason" 1
          ;;
        *)
          report "regression: $label -- vendored pre-fix hook fails for the expected reason" 0 \
            "expected reason containing '$expected_reason', got '${before_reason:-<none>}' -- the fixture may have quarantined for an unrelated cause, not the bug this check targets"
          ;;
      esac
    else
      report "regression: $label -- vendored pre-fix hook reproduces the original failure" 0 \
        "expected QUARANTINED, got $before_outcome -- $before_hook may no longer represent the pre-fix state"
      report "regression: $label -- vendored pre-fix hook fails for the expected reason" 0 \
        "skipped -- outcome was not QUARANTINED"
    fi
    rm -rf "$before_sandbox"
  else
    report "regression: $label -- missing vendored pre-fix hook" 0 "$before_hook not found"
    report "regression: $label -- vendored pre-fix hook fails for the expected reason" 0 \
      "skipped -- $before_hook not found"
  fi

  local after_sandbox after_outcome
  after_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$fixture")"
  after_outcome="$(outcome_of "$after_sandbox" "$base")"
  if [ "$after_outcome" = "ARCHIVED" ]; then
    report "regression: $label -- current hook fixes it" 1
  else
    report "regression: $label -- current hook fixes it" 0 "expected ARCHIVED, got $after_outcome"
  fi
  rm -rf "$after_sandbox"
}

# ===========================================================================
# 0. Synthetic phase-agent fixtures, generated once and reused by both the
#    regression checks and the whitelist-completeness section below.
# ===========================================================================
whitelist_dir="$(mktemp -d)"

make_drop() {
  local agent="$1" scope="$2" out="$3"
  cat > "$out" <<DROP
---
id: ${agent}-20260821T000000Z-whitelist-check
agent: ${agent}
scope: ${scope}
verdict: INFO
created: 2026-08-21T00:00:00Z
blockers: []
---

Synthetic drop generated by .claude/hooks/tests/run.sh to prove ${agent} is
accepted by ALLOWED_AGENTS.
DROP
}

make_drop "critic"                     "decision"     "$whitelist_dir/critic.md"
make_drop "realist"                    "handoff"      "$whitelist_dir/realist.md"
make_drop "spec-author"                "doc"          "$whitelist_dir/spec-author.md"
make_drop "dreamer-first-principles"   "architecture" "$whitelist_dir/dreamer-first-principles.md"
make_drop "dreamer-informed"           "architecture" "$whitelist_dir/dreamer-informed.md"
make_drop "dreamer-convergence"        "architecture" "$whitelist_dir/dreamer-convergence.md"
make_drop "not-a-real-agent"           "decision"     "$whitelist_dir/bogus-agent.md"

# ===========================================================================
# 1. DoD regression tests -- must fail before each fix, pass after.
# ===========================================================================
check_regression \
  "nested-mapping list items (blocker: schema's own example was rejected)" \
  "$NESTED_MAPPING_BUG_HOOK" \
  "$FIXTURES/01-schema-nested-2blockers.md" \
  "malformed yaml: list item without parent key"

check_regression \
  "stale agent whitelist (blocker: every phase-agent drop silently quarantined)" \
  "$STALE_WHITELIST_BUG_HOOK" \
  "$whitelist_dir/critic.md" \
  "unknown agent: critic"

# ===========================================================================
# 2. Corpus coverage -- 18 cases, cross-checked against PyYAML 6.0.3.
# ===========================================================================
declare -A EXPECTED=(
  ["01-schema-nested-2blockers.md"]="ARCHIVED"
  ["02-schema-pass.md"]="ARCHIVED"
  ["03-flow-style.md"]="ARCHIVED"
  ["04-orphan-list-item.md"]="QUARANTINED"
  ["05-pass-with-blockers.md"]="QUARANTINED"
  ["06-nc-empty-blockers.md"]="QUARANTINED"
  ["07-tab-indent.md"]="QUARANTINED"
  # Known, reviewed divergence from strict YAML: ragged indentation inside a
  # list item is accepted here (flattened) where PyYAML raises a syntax
  # error. No PASS<->blockers gate hole is reachable through it (flattening
  # can only add to an already non-empty list) -- reviewer traced this and
  # accepted it as behaviour, not a bug. Tracked here so any future change
  # that widens the gap is a deliberate decision, not a silent drift.
  ["08-ragged-indent.md"]="ARCHIVED"
  ["09-nested-list.md"]="ARCHIVED"
  ["10-empty-values.md"]="ARCHIVED"
  ["11-colon-in-quotes.md"]="ARCHIVED"
  ["12-crlf.md"]="ARCHIVED"
  ["13-zero-indent-list.md"]="ARCHIVED"
  ["14-verdict-only-nested.md"]="QUARANTINED"
  ["15-nested-verdict-shadow.md"]="ARCHIVED"
  ["16-block-scalar.md"]="ARCHIVED"
  ["17-bare-scalars.md"]="ARCHIVED"
  ["18-comments-inside.md"]="ARCHIVED"
)

corpus_names=()
while IFS= read -r name; do
  corpus_names+=("$name")
done < <(printf '%s\n' "${!EXPECTED[@]}" | sort)

fixture_list=()
for name in "${corpus_names[@]}"; do
  fixture_list+=("$FIXTURES/$name")
done

corpus_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "${fixture_list[@]}")"

for name in "${corpus_names[@]}"; do
  want="${EXPECTED[$name]}"
  got="$(outcome_of "$corpus_sandbox" "$name")"
  if [ "$got" = "$want" ]; then
    report "corpus: $name -> $want" 1
  else
    report "corpus: $name -> $want" 0 "got $got"
  fi
done

# Spot-check *why* the borderline cases were rejected, not just that they were.
reason_14="$(reason_of "$corpus_sandbox" "14-verdict-only-nested.md")"
case "$reason_14" in
  *"missing required fields: verdict"*)
    report "corpus: 14 quarantined for the right reason (top-level verdict genuinely absent)" 1
    ;;
  *)
    report "corpus: 14 quarantined for the right reason (top-level verdict genuinely absent)" 0 \
      "reason was: ${reason_14:-<none>} -- if this now says 'unknown agent' or similar, the nested continuation is leaking into a top-level field again"
    ;;
esac

rm -rf "$corpus_sandbox"

# ===========================================================================
# 3. Agent-whitelist completeness -- every Beast Mode phase agent accepted,
#    a bogus name still rejected.
# ===========================================================================
whitelist_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" \
  "$whitelist_dir/critic.md" \
  "$whitelist_dir/realist.md" \
  "$whitelist_dir/spec-author.md" \
  "$whitelist_dir/dreamer-first-principles.md" \
  "$whitelist_dir/dreamer-informed.md" \
  "$whitelist_dir/dreamer-convergence.md" \
  "$whitelist_dir/bogus-agent.md")"

for agent in critic realist spec-author dreamer-first-principles dreamer-informed dreamer-convergence; do
  got="$(outcome_of "$whitelist_sandbox" "$agent.md")"
  if [ "$got" = "ARCHIVED" ]; then
    report "whitelist: $agent drop accepted" 1
  else
    report "whitelist: $agent drop accepted" 0 "expected ARCHIVED, got $got"
  fi
done

bogus_got="$(outcome_of "$whitelist_sandbox" "bogus-agent.md")"
if [ "$bogus_got" = "QUARANTINED" ]; then
  report "whitelist: bogus agent name still rejected" 1
else
  report "whitelist: bogus agent name still rejected" 0 "expected QUARANTINED, got $bogus_got"
fi

# A hook that moves a valid drop's *file* to archive/ but never appends its
# *content* to decisions.md would still pass every check above -- outcome_of
# only inspects the archive/quarantine directories. Each synthetic drop's id
# is unique (`${agent}-...-whitelist-check`), so grepping decisions.md for
# it proves the append actually happened, not just the file move.
for agent in critic realist spec-author dreamer-first-principles dreamer-informed dreamer-convergence; do
  drop_id="${agent}-20260821T000000Z-whitelist-check"
  if grep -qF "$drop_id" "$whitelist_sandbox/.claude/docs/decisions.md"; then
    report "whitelist: $agent drop content appended to decisions.md" 1
  else
    report "whitelist: $agent drop content appended to decisions.md" 0 \
      "drop id $drop_id not found in decisions.md -- archived to disk without being appended?"
  fi
done

rm -rf "$whitelist_sandbox" "$whitelist_dir"

# ===========================================================================
# 4. Roster invariant -- ALLOWED_AGENTS in the hook must exactly equal the
#    file-name roster of .claude/agents/*.md, plus `lead` (the orchestrator,
#    which has no agent file and is prose-only). This is what closes the
#    defect class rather than just this instance: PR #5 added six agent
#    files without ever touching this hook, and no `paths:` filter on the CI
#    job would have caught that either (before this fix, the workflow only
#    triggered on .claude/hooks/** and decision-schema.md changes). Section
#    3 above proves today's six agents are accepted; it says nothing about
#    the next one. This section proves the *check itself* would have caught
#    PR #5's omission, by dropping a throwaway agent file into the real
#    .claude/agents/ directory at test time and asserting the suite fails,
#    then removing it and asserting it passes again.
# ===========================================================================
extract_allowed_agents() {
  python3 - "$1" <<'PY'
import ast, json, re, sys
path = sys.argv[1]
with open(path, "r", encoding="utf-8") as fh:
    text = fh.read()
m = re.search(r"ALLOWED_AGENTS\s*=\s*(\{.*?\})", text, re.DOTALL)
if not m:
    print("[]")
    sys.exit(0)
print(json.dumps(sorted(ast.literal_eval(m.group(1)))))
PY
}

roster_of() {
  python3 - "$1" "$SCRIPT_DIR" <<'PY'
import json, pathlib, sys

# agent_name() and its helpers live in agent_identity.py, sourced from this
# script's own directory ($SCRIPT_DIR, passed as argv[2]) rather than
# duplicated here and in duplicate_names_of() below. The two copies used to
# be kept in lockstep only by a comment -- see agent_identity.py's module
# docstring for why that's exactly what let the B1 BOM regression slip
# through both checks identically instead of being caught by either.
sys.path.insert(0, sys.argv[2])
from agent_identity import agent_name

d = pathlib.Path(sys.argv[1])
# rglob, not glob: agent files nested in a subdirectory must still be seen,
# not silently dropped from the roster with no failing check at all.
names = sorted(agent_name(p) for p in d.rglob("*.md")) if d.is_dir() else []
names.append("lead")
print(json.dumps(sorted(set(names))))
PY
}

# $1 = agents dir -> prints a JSON list of agent_name() values that are
# declared by more than one file. Two charter files claiming the same
# identity dedupe silently through the roster's set() and stay green; this
# is a distinct failure mode from the roster-vs-whitelist invariant above (a
# duplicate identity is whitelisted either way) so it needs its own check.
duplicate_names_of() {
  python3 - "$1" "$SCRIPT_DIR" <<'PY'
import json, pathlib, sys
from collections import Counter

# Sourced from agent_identity.py, not a second hand-kept-in-lockstep copy --
# see roster_of()'s comment above for why the previous two-heredoc split let
# a fix to one silently not apply to the other.
sys.path.insert(0, sys.argv[2])
from agent_identity import agent_name

d = pathlib.Path(sys.argv[1])
names = [agent_name(p) for p in d.rglob("*.md")] if d.is_dir() else []
dupes = sorted(name for name, count in Counter(names).items() if count > 1)
print(json.dumps(dupes))
PY
}

diff_roster() {
  python3 - "$1" "$2" <<'PY'
import json, sys
allowed = set(json.loads(sys.argv[1]))
roster = set(json.loads(sys.argv[2]))
missing_from_hook = sorted(roster - allowed)
extra_in_hook = sorted(allowed - roster)
parts = []
if missing_from_hook:
    parts.append(f"in .claude/agents/ but not ALLOWED_AGENTS: {missing_from_hook}")
if extra_in_hook:
    parts.append(f"in ALLOWED_AGENTS but no matching file: {extra_in_hook}")
print("; ".join(parts))
PY
}

allowed_json="$(extract_allowed_agents "$CURRENT_HOOK")"
roster_json="$(roster_of "$AGENTS_DIR")"
if [ "$allowed_json" = "$roster_json" ]; then
  report "invariant: ALLOWED_AGENTS == roster(.claude/agents/) union {lead}" 1
else
  report "invariant: ALLOWED_AGENTS == roster(.claude/agents/) union {lead}" 0 \
    "$(diff_roster "$allowed_json" "$roster_json")"
fi

duplicate_names_json="$(duplicate_names_of "$AGENTS_DIR")"
if [ "$duplicate_names_json" = "[]" ]; then
  report "invariant: no two agent files declare the same name:" 1
else
  report "invariant: no two agent files declare the same name:" 0 \
    "duplicate identities across .claude/agents/: $duplicate_names_json"
fi

# Negative control for the check above: it has only ever been asserted in
# the passing direction (today's real .claude/agents/ has no duplicates), in
# a suite whose stated purpose is proving a check *would have* failed.
#
# Built from a synthetic two-file fixture, not a clone of the live
# reviewer.md: an earlier version of this control copied the real
# .claude/agents/ tree and cloned reviewer.md's frontmatter verbatim, which
# coupled the control's own ability to fail to agent_name() correctly
# resolving reviewer.md's identity. When identity resolution broke (the B1
# BOM regression), the control's assertion failed with a message implying
# the check itself was broken ("the check may only ever be able to pass,
# never fail") when the actual fault was upstream in agent_name() -- twice
# reviewer had to trace through that indirection to find the real bug. A
# self-contained fixture makes this control's only dependency the
# duplicate-identity logic it's meant to test.
dup_scratch="$(mktemp -d)"
cat > "$dup_scratch/synthetic-a.md" <<'AGENT'
---
name: synthetic-dup-agent
description: x
---

Synthetic fixture A for the duplicate-identity negative control -- not
derived from any real charter, so this control's ability to fail doesn't
depend on a real agent file's frontmatter continuing to resolve correctly.
AGENT
cat > "$dup_scratch/synthetic-b.md" <<'AGENT'
---
name: synthetic-dup-agent
description: y
---

Synthetic fixture B: same declared name as A, different file and body --
a genuine duplicate identity across two files.
AGENT

dup_negative_json="$(duplicate_names_of "$dup_scratch")"
if [ "$dup_negative_json" = '["synthetic-dup-agent"]' ]; then
  report "invariant: duplicate-identity check fires on an actual duplicate (negative control)" 1
else
  report "invariant: duplicate-identity check fires on an actual duplicate (negative control)" 0 \
    "expected [\"synthetic-dup-agent\"], got $dup_negative_json -- the check may only ever be able to pass, never fail"
fi
rm -rf "$dup_scratch"

# Prove the invariant check itself would have caught PR #5's omission: add a
# throwaway agent file the hook doesn't know about, confirm the check fails,
# remove it, confirm the check passes again. This runs against a scratch
# copy of .claude/agents/, not the real directory -- the suite must never
# mutate the repo it's testing.
#
# Copy recursively (matching the real check's rglob), not a flat *.md glob --
# a flat glob would silently drop any agent file nested in a subdirectory
# from the scratch copy, so the "still passes with unrelated files present"
# half of this section would exercise a different (shallower) tree than the
# invariant it's meant to control for.
agents_scratch="$(mktemp -d)"
while IFS= read -r -d '' f; do
  rel="${f#"$AGENTS_DIR"/}"
  mkdir -p "$agents_scratch/$(dirname "$rel")"
  cp "$f" "$agents_scratch/$rel"
done < <(find "$AGENTS_DIR" -name '*.md' -print0 2>/dev/null)
cat > "$agents_scratch/brand-new-agent.md" <<'AGENT'
# Brand New Agent

A throwaway agent file used only to prove the roster-invariant check fails
when a real agent file exists that ALLOWED_AGENTS doesn't know about.
AGENT

roster_with_new_agent_json="$(roster_of "$agents_scratch")"
if [ "$allowed_json" != "$roster_with_new_agent_json" ]; then
  report "invariant: adding an agent file without updating the hook fails the check" 1
else
  report "invariant: adding an agent file without updating the hook fails the check" 0 \
    "expected a mismatch once brand-new-agent.md exists in the roster, but the check still reported equal"
fi
rm -rf "$agents_scratch"

# ---------------------------------------------------------------------------
# agent_name() regressions -- both reviewer-verified false GREENs where the
# check stayed green with the real identity unwhitelisted, plus the
# already-known trailing-comment case, each run against a throwaway
# single-file scratch dir (never the real .claude/agents/).
# ---------------------------------------------------------------------------

# V9: indented-root frontmatter -- every line, including `name:`, carries
# leading whitespace. PyYAML would resolve a top-level `name: brand-new-
# agent` regardless; a strict column-0 scan sees no candidate. The bug was
# falling back to path.stem here (the file's stem below, zzz-scratch-v9,
# doesn't collide with any real agent, so a leaked stem is directly visible
# in the roster rather than masked by a coincidental match).
v9_dir="$(mktemp -d)"
cat > "$v9_dir/zzz-scratch-v9.md" <<'AGENT'
---
  name: brand-new-agent
  description: x
---

Scratch fixture for the V9 regression (indented-root frontmatter).
AGENT

v9_roster_json="$(roster_of "$v9_dir")"
# The exact stem as a standalone roster entry, not just any substring match
# -- the sentinel value deliberately embeds path.name in its message
# (`<no top-level name: in zzz-scratch-v9.md>`), which is a superstring of
# the bare stem and must not be mistaken for a leaked-stem failure.
if [[ "$v9_roster_json" == *'"zzz-scratch-v9"'* ]]; then
  report "V9: indented-root frontmatter does not fall back to path.stem" 0 \
    "got $v9_roster_json -- stem leaked through despite frontmatter being present"
else
  report "V9: indented-root frontmatter does not fall back to path.stem" 1
fi
rm -rf "$v9_dir"

# V10: duplicate top-level `name:` -- the LAST one must win, matching
# PyYAML/YAML's own last-key-wins rule for a duplicate mapping key, not the
# first (the bug: agent_name() returned on the first match).
v10_dir="$(mktemp -d)"
cat > "$v10_dir/zzz-scratch-v10.md" <<'AGENT'
---
name: reviewer
description: x
name: brand-new-agent
---

Scratch fixture for the V10 regression (duplicate top-level name:).
AGENT

v10_roster_json="$(roster_of "$v10_dir")"
if [ "$v10_roster_json" = '["brand-new-agent", "lead"]' ]; then
  report "V10: duplicate top-level name: -- last one wins, not first" 1
else
  report "V10: duplicate top-level name: -- last one wins, not first" 0 \
    "expected [\"brand-new-agent\", \"lead\"], got $v10_roster_json"
fi
rm -rf "$v10_dir"

# V4: a trailing ` # comment` on the name: line must be stripped, not folded
# into the identity (`name: reviewer # note` previously resolved to the
# literal string "reviewer # note", a live false RED against the real
# reviewer.md's plain `name: reviewer`).
v4_dir="$(mktemp -d)"
cat > "$v4_dir/zzz-scratch-v4.md" <<'AGENT'
---
name: reviewer # trailing comment must not become part of the identity
description: x
---

Scratch fixture for the V4 regression (trailing comment on name:).
AGENT

v4_roster_json="$(roster_of "$v4_dir")"
if [ "$v4_roster_json" = '["lead", "reviewer"]' ]; then
  report "V4: trailing comment on name: is stripped, not folded into the identity" 1
else
  report "V4: trailing comment on name: is stripped, not folded into the identity" 0 \
    "expected [\"lead\", \"reviewer\"], got $v4_roster_json"
fi
rm -rf "$v4_dir"

# V11: a leading UTF-8 BOM before the opening fence -- `\s` does not match
# U+FEFF, so a BOM'd file with plainly-present frontmatter must not be
# treated as "no frontmatter at all" (path.stem fallback) any more than the
# indented-root case in V9 above. Reviewer proved this end-to-end against
# the real suite: a BOM-prefixed reviewer.md with its identity changed to
# `brand-new-agent` left both invariants green while the charter declared
# an unwhitelisted name.
v11_dir="$(mktemp -d)"
printf '\xef\xbb\xbf---\nname: brand-new-agent\ndescription: x\n---\n\nScratch fixture for the V11 regression (BOM before the opening fence).\n' \
  > "$v11_dir/zzz-scratch-v11.md"

v11_roster_json="$(roster_of "$v11_dir")"
if [ "$v11_roster_json" = '["brand-new-agent", "lead"]' ]; then
  report "V11: BOM before opening fence does not defeat frontmatter detection" 1
else
  report "V11: BOM before opening fence does not defeat frontmatter detection" 0 \
    "expected [\"brand-new-agent\", \"lead\"], got $v11_roster_json"
fi
rm -rf "$v11_dir"

# V12: a degenerate empty-body fence (`---\n---\n`, zero lines between the
# markers) -- the old pattern required two newlines (one ending the opening
# line, one before the closing line), so this never matched at all and fell
# back to path.stem the same way V9's indented-root case did. An empty body
# has no `name:` line either way, so the correct outcome is the no-name
# sentinel, not a leaked stem -- same assertion shape as V9.
v12_dir="$(mktemp -d)"
printf -- '---\n---\n\nScratch fixture for the V12 regression (empty-body fence).\n' \
  > "$v12_dir/zzz-scratch-v12.md"

v12_roster_json="$(roster_of "$v12_dir")"
if [[ "$v12_roster_json" == *'"zzz-scratch-v12"'* ]]; then
  report "V12: empty-body fence does not fall back to path.stem" 0 \
    "got $v12_roster_json -- stem leaked through despite the (empty) fence being present"
else
  report "V12: empty-body fence does not fall back to path.stem" 1
fi
rm -rf "$v12_dir"

# V13: a leading whitespace character OTHER than the four the fence regex's
# character class explicitly names (space, tab, CR, LF) -- specifically one
# that IS in Python's `\s` class on `str` but is NOT ASCII horizontal/
# vertical whitespace. V11 above proved the U+FEFF BOM case (not in `\s` at
# all); this proves the mirror-image regression: `[ \t\r\n]*` as the leading
# run (introduced when narrowing the trailing run in the same fix) silently
# excludes NBSP, form feed, and the rest of `\s`'s other 23 members just as
# thoroughly as `\s` itself excludes the BOM, and for the identical reason
# (the class doesn't include the byte). Same false-GREEN shape as V11: a
# charter prefixed with one of these characters and an unwhitelisted
# `name:` must still resolve its real identity, not fall back to path.stem.
# Four representative code points, spanning the class: NBSP (U+00A0, the
# one most likely to arrive by accident via copy-paste from rendered text),
# form feed (U+000C, a C0 control code `\s` matches that `\r`/`\n` don't
# cover), U+2028 LINE SEPARATOR (a Unicode line-breaking character with no
# ASCII analogue), and U+3000 IDEOGRAPHIC SPACE (a full-width space, the
# highest code point `\s` matches on `str`).
declare -A v13_cases=(
  ["nbsp"]='\xc2\xa0'
  ["form-feed"]='\x0c'
  ["line-separator"]='\xe2\x80\xa8'
  ["ideographic-space"]='\xe3\x80\x80'
)
for v13_label in "${!v13_cases[@]}"; do
  v13_dir="$(mktemp -d)"
  v13_stem="zzz-scratch-v13-$v13_label"
  printf "${v13_cases[$v13_label]}" > "$v13_dir/$v13_stem.md"
  printf -- '---\nname: brand-new-agent\ndescription: x\n---\n\nScratch fixture for the V13 regression (%s before the opening fence).\n' \
    "$v13_label" >> "$v13_dir/$v13_stem.md"

  v13_roster_json="$(roster_of "$v13_dir")"
  if [ "$v13_roster_json" = '["brand-new-agent", "lead"]' ]; then
    report "V13 ($v13_label): non-ASCII \\s whitespace before opening fence does not defeat frontmatter detection" 1
  else
    report "V13 ($v13_label): non-ASCII \\s whitespace before opening fence does not defeat frontmatter detection" 0 \
      "expected [\"brand-new-agent\", \"lead\"], got $v13_roster_json"
  fi
  rm -rf "$v13_dir"
done

# V14: a closing fence at end-of-file with no trailing newline
# (`---\nname: ...\n---`, no final `\n`). Pre-existing, same class as V12's
# empty-body fence, not introduced by this pass: the closing-fence search
# required a literal `\r?\n` after the second `---`, so a file that simply
# ends right after the fence (no blank line, no trailing newline at all --
# plausible from an editor with "trim final newline" but not "insert final
# newline" enabled) never matched and stem-leaked the same way V9/V12 did.
v14_dir="$(mktemp -d)"
printf -- '---\nname: brand-new-agent\ndescription: x\n---' \
  > "$v14_dir/zzz-scratch-v14.md"

v14_roster_json="$(roster_of "$v14_dir")"
if [ "$v14_roster_json" = '["brand-new-agent", "lead"]' ]; then
  report "V14: closing fence at EOF with no trailing newline does not defeat frontmatter detection" 1
else
  report "V14: closing fence at EOF with no trailing newline does not defeat frontmatter detection" 0 \
    "expected [\"brand-new-agent\", \"lead\"], got $v14_roster_json"
fi
rm -rf "$v14_dir"

# ===========================================================================
# 5. LEGACY path and .last-review-verdict latest-selection -- neither is
#    exercised by the corpus (all 18 cases have front-matter) or the
#    whitelist section (all reviewer-scope checks upstream use a single
#    fixed `created`).
# ===========================================================================
legacy_dir="$(mktemp -d)"
cat > "$legacy_dir/legacy-drop.md" <<'LEGACY'
No front matter here -- a legacy decision note that predates the schema.
LEGACY

legacy_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$legacy_dir/legacy-drop.md")"
legacy_outcome="$(outcome_of "$legacy_sandbox" "legacy-drop.md")"
if [ "$legacy_outcome" = "ARCHIVED" ]; then
  report "legacy: no-front-matter drop archived" 1
else
  report "legacy: no-front-matter drop archived" 0 "expected ARCHIVED, got $legacy_outcome"
fi

if grep -qF '<!-- legacy -->' "$legacy_sandbox/.claude/docs/decisions.md" \
  && grep -qF 'legacy decision note that predates the schema' "$legacy_sandbox/.claude/docs/decisions.md"; then
  report "legacy: appended under <!-- legacy --> marker with body content" 1
else
  report "legacy: appended under <!-- legacy --> marker with body content" 0 \
    "marker or body content missing from decisions.md"
fi

rm -rf "$legacy_sandbox" "$legacy_dir"

# Two reviewer-scope drops with different `created` timestamps, filenames
# chosen so glob (alphabetical) processing order is the *reverse* of
# chronological order. If the cache picked "whichever file the loop saw
# last" instead of comparing `created`, this would report the wrong
# verdict.
verdict_dir="$(mktemp -d)"
cat > "$verdict_dir/a-processed-first.md" <<'DROP'
---
id: reviewer-verdict-check-late
agent: reviewer
scope: review
verdict: NEEDS-CHANGES
created: 2026-08-21T10:00:00Z
blockers:
  - file: src/Foo.cs
    reason: "later created timestamp, must win the verdict-cache race"
---

Synthetic drop: later `created`, must win despite sorting first
alphabetically (glob order processes this file before the other one).
DROP

cat > "$verdict_dir/z-processed-last.md" <<'DROP'
---
id: reviewer-verdict-check-early
agent: reviewer
scope: review
verdict: PASS
created: 2026-08-21T05:00:00Z
blockers: []
---

Synthetic drop: earlier `created`, must lose despite sorting last
alphabetically (glob order processes this file after the other one).
DROP

verdict_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$verdict_dir/a-processed-first.md" "$verdict_dir/z-processed-last.md")"
verdict_cache_content="$(cat "$verdict_sandbox/.squad/.last-review-verdict" 2>/dev/null || true)"
if [ "$verdict_cache_content" = "NEEDS-CHANGES" ]; then
  report "verdict cache: latest-by-created reviewer verdict wins over file processing order" 1
else
  report "verdict cache: latest-by-created reviewer verdict wins over file processing order" 0 \
    "expected NEEDS-CHANGES (created 10:00 > 05:00), got '$verdict_cache_content'"
fi
rm -rf "$verdict_sandbox" "$verdict_dir"

# ===========================================================================
# 6. B2 -- the same BOM/empty-fence bug proved against agent_name() above
#    (V11/V12) also defeated scribe-decision-merger.sh's own, separate
#    frontmatter-fence detection in validate(). Before the fix, a BOM'd or
#    empty-fence drop read as "no front matter at all" and fell into LEGACY
#    mode -- archived and appended to decisions.md with the whitelist,
#    verdict-enum, scope-enum, and blockers-consistency checks skipped
#    entirely, not just weakened. See the TODO(#8) comment in
#    scribe-decision-merger.sh next to the fix.
# ===========================================================================
b2_bom_dir="$(mktemp -d)"
printf '\xef\xbb\xbf' > "$b2_bom_dir/bom-bypass.md"
cat >> "$b2_bom_dir/bom-bypass.md" <<'DROP'
---
id: bom-bypass-check
agent: bogus-not-a-real-agent
scope: decision
verdict: PASS
created: 2026-08-21T00:00:00Z
blockers: []
---

BOM'd drop with an unwhitelisted agent -- must be quarantined by the
whitelist check, not silently accepted as a legacy no-frontmatter drop.
DROP

b2_bom_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$b2_bom_dir/bom-bypass.md")"
b2_bom_outcome="$(outcome_of "$b2_bom_sandbox" "bom-bypass.md")"
if [ "$b2_bom_outcome" = "QUARANTINED" ]; then
  report "B2: BOM'd drop with unwhitelisted agent is quarantined, not treated as LEGACY" 1
else
  report "B2: BOM'd drop with unwhitelisted agent is quarantined, not treated as LEGACY" 0 \
    "expected QUARANTINED, got $b2_bom_outcome"
fi

b2_bom_reason="$(reason_of "$b2_bom_sandbox" "bom-bypass.md")"
case "$b2_bom_reason" in
  *"unknown agent: bogus-not-a-real-agent"*)
    report "B2: quarantined for the right reason (whitelist check ran, not skipped as LEGACY)" 1
    ;;
  *)
    report "B2: quarantined for the right reason (whitelist check ran, not skipped as LEGACY)" 0 \
      "reason was: ${b2_bom_reason:-<none>} -- if this says something else, the BOM may be falling into LEGACY mode again"
    ;;
esac
rm -rf "$b2_bom_sandbox" "$b2_bom_dir"

# Empty-body fence (`---\n---\n`): a drop with no frontmatter fields at all
# must be quarantined for missing required fields, not treated as LEGACY
# (which would skip the whitelist/verdict/scope/blockers checks entirely).
b2_empty_dir="$(mktemp -d)"
cat > "$b2_empty_dir/empty-fence.md" <<'DROP'
---
---

Empty-body fence -- no frontmatter fields at all.
DROP

b2_empty_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$b2_empty_dir/empty-fence.md")"
b2_empty_outcome="$(outcome_of "$b2_empty_sandbox" "empty-fence.md")"
if [ "$b2_empty_outcome" = "QUARANTINED" ]; then
  report "B2: empty-body fence is quarantined for missing required fields, not treated as LEGACY" 1
else
  report "B2: empty-body fence is quarantined for missing required fields, not treated as LEGACY" 0 \
    "expected QUARANTINED, got $b2_empty_outcome"
fi

b2_empty_reason="$(reason_of "$b2_empty_sandbox" "empty-fence.md")"
case "$b2_empty_reason" in
  *"missing required fields"*)
    report "B2: empty-fence drop quarantined for the right reason" 1
    ;;
  *)
    report "B2: empty-fence drop quarantined for the right reason" 0 \
      "reason was: ${b2_empty_reason:-<none>} -- if this says something else, the empty fence may be falling into LEGACY mode again"
    ;;
esac
rm -rf "$b2_empty_sandbox" "$b2_empty_dir"

# B2 (cont.): the mirror-image regression to the BOM case above, proved
# against scribe-decision-merger.sh's OWN fence detection the same way V13
# proved it against agent_identity.py's. The leading run's character class
# was narrowed to `[ \t\r\n]*` in the same fix that closed the BOM/empty-
# fence bug -- that class excludes NBSP, form feed, and the rest of `\s`'s
# other members just as thoroughly as `\s` itself excludes the BOM. A drop
# prefixed with one of these must still be parsed (whitelist/verdict/scope/
# blockers checks must run), not silently promoted to LEGACY mode the way a
# BOM'd drop was before the original fix. Same two representative code
# points as V13's most-likely-by-accident and most-distant-from-ASCII ends
# of the class: NBSP (U+00A0) and U+3000 IDEOGRAPHIC SPACE.
declare -A b2_ws_cases=(
  ["nbsp"]='\xc2\xa0'
  ["ideographic-space"]='\xe3\x80\x80'
)
for b2_ws_label in "${!b2_ws_cases[@]}"; do
  b2_ws_dir="$(mktemp -d)"
  b2_ws_file="$b2_ws_dir/ws-bypass-$b2_ws_label.md"
  printf "${b2_ws_cases[$b2_ws_label]}" > "$b2_ws_file"
  cat >> "$b2_ws_file" <<DROP
---
id: ws-bypass-$b2_ws_label
agent: bogus-not-a-real-agent
scope: decision
verdict: PASS
created: 2026-08-21T00:00:00Z
blockers: []
---

Drop prefixed with $b2_ws_label before the opening fence, with an
unwhitelisted agent -- must be quarantined by the whitelist check, not
silently accepted as a legacy no-frontmatter drop.
DROP

  b2_ws_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$b2_ws_file")"
  b2_ws_outcome="$(outcome_of "$b2_ws_sandbox" "ws-bypass-$b2_ws_label.md")"
  if [ "$b2_ws_outcome" = "QUARANTINED" ]; then
    report "B2 ($b2_ws_label): drop with unwhitelisted agent is quarantined, not treated as LEGACY" 1
  else
    report "B2 ($b2_ws_label): drop with unwhitelisted agent is quarantined, not treated as LEGACY" 0 \
      "expected QUARANTINED, got $b2_ws_outcome"
  fi

  b2_ws_reason="$(reason_of "$b2_ws_sandbox" "ws-bypass-$b2_ws_label.md")"
  case "$b2_ws_reason" in
    *"unknown agent: bogus-not-a-real-agent"*)
      report "B2 ($b2_ws_label): quarantined for the right reason (whitelist check ran, not skipped as LEGACY)" 1
      ;;
    *)
      report "B2 ($b2_ws_label): quarantined for the right reason (whitelist check ran, not skipped as LEGACY)" 0 \
        "reason was: ${b2_ws_reason:-<none>} -- if this says something else, this whitespace char may be falling into LEGACY mode again"
      ;;
  esac
  rm -rf "$b2_ws_sandbox" "$b2_ws_dir"
done

# B2 (cont.): the closing-fence-at-EOF case (V14's sibling), proved against
# scribe-decision-merger.sh's own fence detection. A drop that ends
# immediately after the closing `---` with no trailing newline at all must
# still be parsed (whitelist check must run), not silently promoted to
# LEGACY mode.
b2_eof_dir="$(mktemp -d)"
printf -- '---\nid: eof-fence-bypass\nagent: bogus-not-a-real-agent\nscope: decision\nverdict: PASS\ncreated: 2026-08-21T00:00:00Z\nblockers: []\n---' \
  > "$b2_eof_dir/eof-fence-bypass.md"

b2_eof_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$b2_eof_dir/eof-fence-bypass.md")"
b2_eof_outcome="$(outcome_of "$b2_eof_sandbox" "eof-fence-bypass.md")"
if [ "$b2_eof_outcome" = "QUARANTINED" ]; then
  report "B2 (eof-fence): drop with unwhitelisted agent is quarantined, not treated as LEGACY" 1
else
  report "B2 (eof-fence): drop with unwhitelisted agent is quarantined, not treated as LEGACY" 0 \
    "expected QUARANTINED, got $b2_eof_outcome"
fi

b2_eof_reason="$(reason_of "$b2_eof_sandbox" "eof-fence-bypass.md")"
case "$b2_eof_reason" in
  *"unknown agent: bogus-not-a-real-agent"*)
    report "B2 (eof-fence): quarantined for the right reason (whitelist check ran, not skipped as LEGACY)" 1
    ;;
  *)
    report "B2 (eof-fence): quarantined for the right reason (whitelist check ran, not skipped as LEGACY)" 0 \
      "reason was: ${b2_eof_reason:-<none>} -- if this says something else, the EOF fence may be falling into LEGACY mode again"
    ;;
esac
rm -rf "$b2_eof_sandbox" "$b2_eof_dir"

# ===========================================================================
echo
echo "Summary: $pass passed, $fail failed"
[ "$fail" -eq 0 ]
