#!/usr/bin/env bash
#
# Regression + coverage tests for .claude/hooks/scribe-decision-merger.sh,
# plus (see the "GIT COMMIT DETECTION" section near the end) the shared
# lib/git-commit-detect.sh helper and the four commit-time PreToolUse hooks
# that source it (enforce-conventional-commits.sh, enforce-gpg-signing.sh,
# enforce-no-secrets.sh, block-large-files.sh).
#
# Run:
#   bash .claude/hooks/tests/run.sh
#
# No dependencies beyond bash 4+ (this suite uses `declare -A`; the hook
# itself only needs bash 3) and python3 (already required by the hook
# itself). No git history requirement: the three pre-fix hook revisions
# used by the regression checks are vendored as fixtures (see
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
#   6. The forgeable-verdict regression (PR #355 review, blocker #4): the
#      nested-key-promotion bug (search GUARDED_TOPLEVEL_FIELDS in the
#      hook) let a drop declare a mundane real identity at column 0 and
#      then bury a nested `meta:` block that silently overwrote
#      agent/verdict/blockers, forging a `reviewer · PASS` archive entry
#      and a `PASS` write to `.last-review-verdict` for a drop no reviewer
#      ever produced. Two rounds:
#        - Round 1: checked against
#          fixtures-pre-fix/scribe-decision-merger.pre-column0-guard-fix.sh
#          (the revision before ANY guard) and
#          fixtures/19-nested-agent-verdict-forgery.md (the reviewer's
#          exact forged drop, reconstructed with the id/scope/created
#          boilerplate the original PR-review snippet omitted for
#          brevity). Landed with two gaps a re-review caught: the guard
#          counted ASCII spaces only, and `blockers` wasn't in the guarded
#          set.
#        - Round 2: checked against
#          fixtures-pre-fix/scribe-decision-merger.pre-whitespace-guard-fix.sh
#          (round 1's hook -- HAS the guard, still bypassable) and six new
#          fixtures, one per non-space whitespace character the reviewer
#          verified bypasses round 1 (tab, NBSP, ideographic space,
#          vertical tab, form feed, em space -- fixtures
#          20-nested-forgery-tab.md through 25-nested-forgery-em-space.md),
#          plus fixtures/26-blockers-nested-shadow.md for the
#          unguarded-`blockers` exploit. Fixed by whitespace-aware
#          indentation (`line.lstrip()`), a strict `\r\n|\r|\n` line
#          splitter (vertical tab and form feed are themselves line
#          terminators under plain `str.splitlines()`, which no indent fix
#          alone can compensate for), and adding `blockers` to
#          GUARDED_TOPLEVEL_FIELDS.
#      The guard now covers six field NAMES (id, agent, verdict, scope,
#      created, blockers) -- the broader nested-key-promotion bug for every
#      OTHER key, and the list-item acceptance branch generally, remain
#      open; see https://github.com/MCGPPeters/squad-template/issues/8 and
#      the TODO(#8) comments in the hook itself.
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
COLUMN0_GUARD_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-column0-guard-fix.sh"    # nested key promotion forges reviewer/PASS, no guard at all
WHITESPACE_GUARD_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-whitespace-guard-fix.sh"  # HAS the column-0 guard (space-only) but non-space whitespace and the unguarded blockers key still forge it
CR_NEWLINE_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-cr-newline-fix.sh"            # HAS the whitespace-aware guard AND the strict \r\n|\r|\n splitter, but reads with newline=None (universal newlines), so a lone CR is rewritten to LF at read time, upstream of the splitter and the guard both
LEGACY_FENCE_BYPASS_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-legacy-fence-bypass-fix.sh"  # HAS newline="" and the Cf-strip loop, but the Cf-strip only strips a LEADING RUN of Cf characters -- a non-Cf character in front, a non-Cf junk character (Cc/Mn), a Cf run interrupted by a newline, or a CR-only line-ending fence all still fall through to LEGACY
PRE_CAP_AND_CATEGORY_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-cap-and-category-fix.sh"  # HAS the junk-prefix walk from round 5, but (a) it fails OPEN on cap-hit (256-char cap, returns False/LEGACY past it) and (b) _JUNK_CATEGORIES enumerates six category MEMBERS, missing Cn (unassigned/Default_Ignorable) and Mc (spacing mark) -- also still has the quadratic Cf-strip loop
UNTERMINATED_FENCE_BUG_HOOK="$PRE_FIX/scribe-decision-merger.pre-unterminated-fence-fix.sh"  # HAS all of round 4/5/6's fixes, but an opening fence with NO closing fence at all still falls through to LEGACY unvalidated -- route 3, pre-existing, fixed in round 7

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
verify_fixture_sha256 \
  "pre-column0-guard-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-column0-guard-fix.sh" \
  "b2366e5e7105500ab70c8581e1627769ab6903b28a99f4190b2e1d53ed342cda"
verify_fixture_sha256 \
  "pre-whitespace-guard-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-whitespace-guard-fix.sh" \
  "5f997f5024a7284cc624324af9a862e3fd70b8f52bd121c1ac85a90f2675128f"
verify_fixture_sha256 \
  "pre-cr-newline-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-cr-newline-fix.sh" \
  "52d5bd319c676c58c9d4343abba6adfbe61115c98346665ec51f0c62a8ed6920"
verify_fixture_sha256 \
  "enforce-no-secrets.pre-cd-subshell-fix.sh" \
  "$PRE_FIX/enforce-no-secrets.pre-cd-subshell-fix.sh" \
  "052f13791157147deadd07a70f81b90c341332465c3dbac13ff96c491281a350"
verify_fixture_sha256 \
  "lib/git_commit_detect.py (pre-transparent-prefix-fix, also enforce-no-secrets.pre-cd-subshell-fix.sh's own lib/ dependency)" \
  "$PRE_FIX/lib/git_commit_detect.py" \
  "fde84c1cc579f66d789e9b3a6d7252e35481fad2a7749579d70128cd02736e5c"
verify_fixture_sha256 \
  "statusline.pre-must-not-raise-fix.py" \
  "$PRE_FIX/statusline.pre-must-not-raise-fix.py" \
  "f674313ec9c1129897315c6e4c8f3023a740086915739185452dfbaeb6a33b30"
verify_fixture_sha256 \
  "pre-legacy-fence-bypass-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-legacy-fence-bypass-fix.sh" \
  "1f6e24b7eec9df6525588529ce466107389b8a794355edbeef615a71533c510d"
verify_fixture_sha256 \
  "lib/git-commit-detect.sh (round 5 re-review, review-pr355-round4.md nit: previously the one vendored fixture without a pin)" \
  "$PRE_FIX/lib/git-commit-detect.sh" \
  "324d3a9abdf3a718759a048aa10ec40e82100d742b3b8f9d4c463404e0cbc6cb"
verify_fixture_sha256 \
  "pre-cap-and-category-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-cap-and-category-fix.sh" \
  "8e6a76d05f6393b5afcfd4be333cfea49345215e00838036b1408fee75b96e10"
verify_fixture_sha256 \
  "pre-unterminated-fence-fix.sh" \
  "$PRE_FIX/scribe-decision-merger.pre-unterminated-fence-fix.sh" \
  "fbd4c0007d284e6ac2f84560e6cf8b4b66d55d36fe1138d76f67db62a34d6825"

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
# $1 = human label, $2 = vendored pre-fix hook script (must reproduce the
# forgery), $3 = fixture path, $4 = the exact drop id inside that fixture
# (used to locate its heading line in decisions.md), $5 = expected CURRENT
# outcome (ARCHIVED or QUARANTINED), $6 = for ARCHIVED, a substring
# expected in the drop's TRUE (non-forged) heading (e.g.
# "[csharp-dev · NEEDS-CHANGES]"); for QUARANTINED, a substring expected in
# the .reason file.
#
# Inverted polarity from check_regression above, same as before: the bug
# made the pre-fix hook wrongly ARCHIVE the forged drop as a false
# `reviewer · PASS` and forge `.last-review-verdict`. What "fixed" looks
# like now has TWO shapes depending on the fixture, both asserted here:
#   - identity-forgery shape (fixtures 19-25): once agent/verdict/blockers
#     can no longer be overwritten by a nested block, the drop's real,
#     internally-consistent content is honoured and it archives under its
#     TRUE identity -- NOT quarantined, and specifically NOT archived as
#     `[reviewer · PASS]`.
#   - blockers-only shape (fixture 26): a genuine top-level
#     `agent: reviewer` / `verdict: PASS` whose real non-empty `blockers`
#     was covertly zeroed by an unguarded nested key now correctly fails
#     the verdict<->blockers consistency check it was trying to dodge --
#     QUARANTINED for that real inconsistency.
# Either way, the property that actually matters is asserted unconditionally
# at the end: `.last-review-verdict` is never written for this drop.
check_forgery_regression() {
  local label="$1" before_hook="$2" fixture="$3" drop_id="$4" expected_after_outcome="$5" expected_after_detail="$6" base
  base="$(basename "$fixture")"

  if [ -f "$before_hook" ]; then
    local before_sandbox before_outcome before_heading before_cache
    before_sandbox="$(run_hook_sandbox "$before_hook" "$fixture")"
    before_outcome="$(outcome_of "$before_sandbox" "$base")"
    if [ "$before_outcome" = "ARCHIVED" ]; then
      report "regression: $label -- vendored pre-fix hook reproduces the forgery (archives, does not reject)" 1
    else
      report "regression: $label -- vendored pre-fix hook reproduces the forgery (archives, does not reject)" 0 \
        "expected ARCHIVED, got $before_outcome -- $before_hook may no longer represent the pre-fix state"
    fi

    before_heading="$(grep -F "$drop_id" "$before_sandbox/.claude/docs/decisions.md" 2>/dev/null | head -n1)"
    case "$before_heading" in
      *"[reviewer · PASS]"*)
        report "regression: $label -- vendored pre-fix hook forges it specifically as [reviewer · PASS]" 1
        ;;
      *)
        report "regression: $label -- vendored pre-fix hook forges it specifically as [reviewer · PASS]" 0 \
          "heading was: ${before_heading:-<none>}"
        ;;
    esac

    before_cache="$(cat "$before_sandbox/.squad/.last-review-verdict" 2>/dev/null || echo "<missing>")"
    if [ "$before_cache" = "PASS" ]; then
      report "regression: $label -- vendored pre-fix hook forges PASS into .last-review-verdict" 1
    else
      report "regression: $label -- vendored pre-fix hook forges PASS into .last-review-verdict" 0 \
        "expected PASS, got $before_cache"
    fi
    rm -rf "$before_sandbox"
  else
    report "regression: $label -- missing vendored pre-fix hook" 0 "$before_hook not found"
    report "regression: $label -- vendored pre-fix hook forges it specifically as [reviewer · PASS]" 0 \
      "skipped -- $before_hook not found"
    report "regression: $label -- vendored pre-fix hook forges PASS into .last-review-verdict" 0 \
      "skipped -- $before_hook not found"
  fi

  local after_sandbox after_outcome after_cache
  after_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$fixture")"
  after_outcome="$(outcome_of "$after_sandbox" "$base")"
  if [ "$after_outcome" = "$expected_after_outcome" ]; then
    report "regression: $label -- current hook outcome is $expected_after_outcome, not a forged archive" 1
  else
    report "regression: $label -- current hook outcome is $expected_after_outcome, not a forged archive" 0 \
      "expected $expected_after_outcome, got $after_outcome"
  fi

  if [ "$expected_after_outcome" = "ARCHIVED" ]; then
    local after_heading
    after_heading="$(grep -F "$drop_id" "$after_sandbox/.claude/docs/decisions.md" 2>/dev/null | head -n1)"
    case "$after_heading" in
      *"[reviewer · PASS]"*)
        report "regression: $label -- current hook does not forge [reviewer · PASS] (archives under true identity instead)" 0 \
          "heading was: $after_heading"
        ;;
      *"$expected_after_detail"*)
        report "regression: $label -- current hook does not forge [reviewer · PASS] (archives under true identity instead)" 1
        ;;
      *)
        report "regression: $label -- current hook does not forge [reviewer · PASS] (archives under true identity instead)" 0 \
          "expected heading containing '$expected_after_detail', got '${after_heading:-<none>}'"
        ;;
    esac
  else
    local after_reason
    after_reason="$(reason_of "$after_sandbox" "$base")"
    case "$after_reason" in
      *"$expected_after_detail"*)
        report "regression: $label -- current hook rejects for the expected reason" 1
        ;;
      *)
        report "regression: $label -- current hook rejects for the expected reason" 0 \
          "expected reason containing '$expected_after_detail', got '${after_reason:-<none>}'"
        ;;
    esac
  fi

  local after_cache
  after_cache="$(cat "$after_sandbox/.squad/.last-review-verdict" 2>/dev/null || echo "<missing>")"
  if [ "$after_cache" = "<missing>" ]; then
    report "regression: $label -- current hook does NOT write PASS to .last-review-verdict for this drop" 1
  else
    report "regression: $label -- current hook does NOT write PASS to .last-review-verdict for this drop" 0 \
      "expected no write (fresh sandbox, file should not exist), got: $after_cache"
  fi
  rm -rf "$after_sandbox"
}

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

# ---------------------------------------------------------------------------
# LEGACY-fall-through-as-bypass regression (round 5 re-review,
# review-pr355-round4.md, the one blocker): inverted polarity from
# check_regression above -- the bug made the pre-fix hook wrongly ARCHIVE
# the drop under the `<!-- legacy -->` marker (whitelist/verdict/scope/
# blockers checks all skipped, content appended to decisions.md
# unvalidated), where "fixed" means QUARANTINED with a specific reason
# instead. Cannot write .last-review-verdict either way (LEGACY mode never
# does), so unlike check_forgery_regression there is no verdict-cache
# assertion here -- see the blocker's own severity note for why that
# makes this narrower than blocker #4 was.
# ---------------------------------------------------------------------------
check_legacy_bypass_regression() {
  local label="$1" before_hook="$2" fixture="$3" expected_after_reason="$4" base
  base="$(basename "$fixture")"

  if [ -f "$before_hook" ]; then
    local before_sandbox before_outcome
    before_sandbox="$(run_hook_sandbox "$before_hook" "$fixture")"
    before_outcome="$(outcome_of "$before_sandbox" "$base")"
    if [ "$before_outcome" = "ARCHIVED" ] \
      && grep -qF '<!-- legacy -->' "$before_sandbox/.claude/docs/decisions.md" 2>/dev/null; then
      report "regression: $label -- vendored pre-fix hook reproduces the LEGACY fall-through" 1
    else
      report "regression: $label -- vendored pre-fix hook reproduces the LEGACY fall-through" 0 \
        "expected ARCHIVED under <!-- legacy -->, got outcome=$before_outcome"
    fi
    rm -rf "$before_sandbox"
  else
    report "regression: $label -- missing vendored pre-fix hook" 0 "$before_hook not found"
  fi

  local after_sandbox after_outcome after_reason
  after_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$fixture")"
  after_outcome="$(outcome_of "$after_sandbox" "$base")"
  if [ "$after_outcome" = "QUARANTINED" ]; then
    report "regression: $label -- current hook quarantines instead of falling through to LEGACY" 1
    after_reason="$(reason_of "$after_sandbox" "$base")"
    case "$after_reason" in
      *"$expected_after_reason"*)
        report "regression: $label -- current hook rejects for the expected reason" 1
        ;;
      *)
        report "regression: $label -- current hook rejects for the expected reason" 0 \
          "expected reason containing '$expected_after_reason', got '${after_reason:-<none>}'"
        ;;
    esac
  else
    report "regression: $label -- current hook quarantines instead of falling through to LEGACY" 0 \
      "expected QUARANTINED, got $after_outcome"
    report "regression: $label -- current hook rejects for the expected reason" 0 \
      "skipped -- outcome was not QUARANTINED"
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

check_forgery_regression \
  "forgeable verdict (PR #355 review blocker #4: nested meta: block impersonates reviewer/PASS, spaces, no guard at all)" \
  "$COLUMN0_GUARD_BUG_HOOK" \
  "$FIXTURES/19-nested-agent-verdict-forgery.md" \
  "csharpdev-20260821T000000Z-forged-verdict-poc" \
  "ARCHIVED" \
  "[csharp-dev · NEEDS-CHANGES]"

# ---------------------------------------------------------------------------
# Re-review round 2 (PR #355, blocker #4 not closed): the column-0 guard
# above counted ASCII spaces only (`line.lstrip(" ")`), and only guarded
# the five schema-required field names. Two independent bypasses, both
# reviewer-verified end-to-end against the round-1 "fixed" hook (vendored
# as $WHITESPACE_GUARD_BUG_HOOK, the "current broken hook" these checks
# target -- NOT the original no-guard-at-all hook, which was already known
# broken):
#   1. Indenting the nested block with a tab, NBSP (U+00A0), ideographic
#      space (U+3000), vertical tab, form feed, or em space (U+2003)
#      instead of spaces measures as indent 0 under the space-only
#      calculation, so the guard never fires. Worse for two of the six:
#      vertical tab and form feed are themselves treated as LINE
#      separators by `str.splitlines()`, so the "nested" line isn't merely
#      miscounted -- it becomes a genuinely separate, truly-zero-indent
#      line that no indent guard could ever distinguish from a real
#      column-0 declaration. Fixed by switching to `re.split(r"\r\n|\r|\n",
#      fm)` for line splitting (so only real newlines end a line) plus
#      `line.lstrip()` (whitespace-aware, no args) for indent counting.
#   2. `blockers` was never in the guarded set, so a genuine top-level
#      `agent: reviewer` / `verdict: PASS` with a real non-empty top-level
#      `blockers` list could still have that list covertly zeroed by a
#      nested nested `blockers: []` under an unrelated key, defeating the
#      verdict<->blockers consistency check the space-indented fixture's
#      rejection was resting on. Fixed by adding `blockers` to
#      GUARDED_TOPLEVEL_FIELDS.
# One fixture per named character (all archive under their TRUE identity,
# csharp-dev/NEEDS-CHANGES, once fixed -- see the check_forgery_regression
# header comment for why that's the correct "closed" shape here) plus one
# for the blockers-only exploit (quarantined for a genuine, no-longer-
# maskable inconsistency).
# ---------------------------------------------------------------------------
declare -A whitespace_forgery_fixtures=(
  ["20-nested-forgery-tab.md"]="csharpdev-20260821T000001Z-forged-tab"
  ["21-nested-forgery-nbsp.md"]="csharpdev-20260821T000002Z-forged-nbsp"
  ["22-nested-forgery-ideographic-space.md"]="csharpdev-20260821T000003Z-forged-ideospace"
  ["23-nested-forgery-vertical-tab.md"]="csharpdev-20260821T000004Z-forged-vtab"
  ["24-nested-forgery-form-feed.md"]="csharpdev-20260821T000005Z-forged-formfeed"
  ["25-nested-forgery-em-space.md"]="csharpdev-20260821T000006Z-forged-emspace"
)
for wf_name in "${!whitespace_forgery_fixtures[@]}"; do
  check_forgery_regression \
    "forgeable verdict, non-space indentation ($wf_name)" \
    "$WHITESPACE_GUARD_BUG_HOOK" \
    "$FIXTURES/$wf_name" \
    "${whitespace_forgery_fixtures[$wf_name]}" \
    "ARCHIVED" \
    "[csharp-dev · NEEDS-CHANGES]"
done

check_forgery_regression \
  "forgeable verdict, unguarded blockers key (spaces only, exploit 2)" \
  "$WHITESPACE_GUARD_BUG_HOOK" \
  "$FIXTURES/26-blockers-nested-shadow.md" \
  "reviewer-20260821T000007Z-blockers-nested-shadow-poc" \
  "QUARANTINED" \
  "verdict PASS but blockers list is non-empty"

# ---------------------------------------------------------------------------
# Round 4 re-review (PR #355 review-b93b430.md, blocker #1): a THIRD, outer
# layer of the same bug class, one call further out than round 2. The
# round-2 fix's own strict splitter, `re.split(r"\r\n|\r|\n", fm)`, names a
# bare `\r` as a line terminator it handles -- but the file was opened with
# `open(path, "r", encoding="utf-8")`, i.e. `newline=None` (universal
# newlines), which translates every lone `\r` to `\n` at READ time, before
# `fm` (the frontmatter substring the splitter runs on) exists at all. The
# `|\r|` alternation was therefore dead code with respect to the one
# terminator it named: a lone CR used as the indentation prefix on a nested
# `meta:` block is consumed as a line terminator by the io layer itself, so
# `agent: reviewer` arrives at the splitter as a genuinely separate,
# genuinely column-0 line -- not merely miscounted indentation the guard
# could catch, real column-0, same failure mode as the vertical-tab/
# form-feed splitlines() bug fixed in round 2, one layer further upstream.
# Reviewer-verified end-to-end against the round-3 "fixed" hook (vendored as
# $CR_NEWLINE_BUG_HOOK -- HAS both the whitespace-aware indent guard and the
# \r\n|\r|\n splitter, still bypassable): bytes
# `b"agent: csharp-dev\n\ragent: reviewer\n"` read back as
# `'agent: csharp-dev\n\nagent: reviewer\n'` -- the CR is gone -- and the
# forged drop archived as `[reviewer · PASS]` / wrote PASS to
# .squad/.last-review-verdict. Fixed by reading with `newline=""` (disables
# universal-newline translation; `\r`, `\r\n` and `\n` all pass through as
# literal bytes) paired with narrowing the splitter to `\r\n|\n` (a bare
# `\r` is no longer treated as ITS OWN line terminator, so it stays part of
# the line it indents and is correctly counted as whitespace by
# `line.lstrip()`). Both halves are necessary together: `newline=""` alone
# without narrowing the splitter would still split on the literal `\r`
# character once it survives to `fm`, reopening the same bug one step
# later.
check_forgery_regression \
  "forgeable verdict, bare-CR indentation (round 4: newline=None translates a lone CR to LF upstream of the splitter)" \
  "$CR_NEWLINE_BUG_HOOK" \
  "$FIXTURES/27-nested-forgery-cr.md" \
  "csharpdev-20260821T000008Z-forged-cr" \
  "ARCHIVED" \
  "[csharp-dev · NEEDS-CHANGES]"

# ---------------------------------------------------------------------------
# Round 5 re-review (review-pr355-round4.md, the one blocker): the LEGACY
# fall-through the round-4 Cf-strip loop claimed to close was only closed
# for a LEADING RUN of Cf characters. Six reproductions, each a drop with
# plainly-present, grep-visible front matter declaring
# `agent: bogus-not-a-real-agent` / `verdict: TOTALLY-FINE` /
# `scope: nonsense-scope` that the pre-fix hook (vendored as
# $LEGACY_FENCE_BYPASS_BUG_HOOK -- HAS newline="" and the Cf-strip loop,
# still bypassable) silently archives under `<!-- legacy -->` with
# ALLOWED_AGENTS/ALLOWED_VERDICTS/ALLOWED_SCOPES and the blockers-
# consistency check all skipped. Fixed by `_fence_hidden_behind_junk_prefix()`
# -- NOT by unbounded `re.search(r"(?m)^---", text)` over the whole
# document, which was verified live to also reject a genuine legacy drop's
# body horizontal rule (see fixtures/34 and /35 below, which exist
# specifically to prove this narrower fix doesn't reintroduce that
# regression).
# ---------------------------------------------------------------------------
declare -A legacy_bypass_fixtures=(
  ["28-legacy-bypass-space-then-bom.md"]="front-matter fence present but not recognised"
  ["29-legacy-bypass-nul.md"]="front-matter fence present but not recognised"
  ["30-legacy-bypass-bel.md"]="front-matter fence present but not recognised"
  ["31-legacy-bypass-combining-acute.md"]="front-matter fence present but not recognised"
  ["32-legacy-bypass-bom-newline-bom.md"]="front-matter fence present but not recognised"
  ["33-legacy-bypass-cr-only.md"]="front-matter fence present but not recognised"
)
for lb_name in "${!legacy_bypass_fixtures[@]}"; do
  check_legacy_bypass_regression \
    "LEGACY fall-through bypass ($lb_name)" \
    "$LEGACY_FENCE_BYPASS_BUG_HOOK" \
    "$FIXTURES/$lb_name" \
    "${legacy_bypass_fixtures[$lb_name]}"
done

# Non-vacuousness in the OTHER direction: the fix must not turn genuine
# legacy drops into hard quarantine errors. Both stay LEGACY under the
# CURRENT hook -- no "before" state needed, since this behaviour was never
# broken; the risk is the FIX breaking it, not a bug being fixed.
for genuine_legacy_name in "34-genuine-legacy-body-hr.md" "35-genuine-legacy-short-heading-then-hr.md"; do
  gl_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$FIXTURES/$genuine_legacy_name")"
  gl_outcome="$(outcome_of "$gl_sandbox" "$genuine_legacy_name")"
  if [ "$gl_outcome" = "ARCHIVED" ] \
    && grep -qF '<!-- legacy -->' "$gl_sandbox/.claude/docs/decisions.md" 2>/dev/null; then
    report "legacy-fence-fix control: $genuine_legacy_name still archives as LEGACY (not falsely rejected)" 1
  else
    report "legacy-fence-fix control: $genuine_legacy_name still archives as LEGACY (not falsely rejected)" 0 \
      "expected ARCHIVED under <!-- legacy -->, got outcome=$gl_outcome"
  fi
  rm -rf "$gl_sandbox"
done

# ---------------------------------------------------------------------------
# Raw-traceback-as-quarantine-reason nitpick (round 5 re-review,
# review-pr355-round4.md): a required field written as a YAML block list
# (`agent:\n  - reviewer`) parses to a python `list`, and
# `list not in ALLOWED_AGENTS` used to raise `TypeError: cannot use 'list'
# as a set element` -- correct OUTCOME (still quarantined), unreadable
# REASON (a raw traceback written into the .reason file). Both the buggy
# and fixed hook quarantine fixtures/36 either way, so this checks the
# REASON text changed, not the outcome -- reusing
# $LEGACY_FENCE_BYPASS_BUG_HOOK as the "before" state since the underlying
# bug predates and is independent of this round's fence-presence fix
# (verified: it reproduces the same raw traceback).
# ---------------------------------------------------------------------------
alb_fixture="$FIXTURES/36-agent-block-list-not-scalar.md"
alb_base="$(basename "$alb_fixture")"

if [ -f "$LEGACY_FENCE_BYPASS_BUG_HOOK" ]; then
  alb_before_sandbox="$(run_hook_sandbox "$LEGACY_FENCE_BYPASS_BUG_HOOK" "$alb_fixture")"
  alb_before_reason="$(reason_of "$alb_before_sandbox" "$alb_base")"
  case "$alb_before_reason" in
    *"Traceback"*"TypeError"*)
      report "agent-as-block-list: vendored pre-fix hook reproduces the raw traceback reason" 1
      ;;
    *)
      report "agent-as-block-list: vendored pre-fix hook reproduces the raw traceback reason" 0 \
        "expected a Traceback/TypeError reason, got '${alb_before_reason:-<none>}'"
      ;;
  esac
  rm -rf "$alb_before_sandbox"
else
  report "agent-as-block-list: missing vendored pre-fix hook" 0 "$LEGACY_FENCE_BYPASS_BUG_HOOK not found"
fi

alb_after_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$alb_fixture")"
alb_after_outcome="$(outcome_of "$alb_after_sandbox" "$alb_base")"
alb_after_reason="$(reason_of "$alb_after_sandbox" "$alb_base")"
if [ "$alb_after_outcome" = "QUARANTINED" ]; then
  case "$alb_after_reason" in
    *"Traceback"*)
      report "agent-as-block-list: current hook gives a readable reason, not a traceback" 0 \
        "reason still contains a Traceback: $alb_after_reason"
      ;;
    *"must be a plain scalar value"*"agent"*)
      report "agent-as-block-list: current hook gives a readable reason, not a traceback" 1
      ;;
    *)
      report "agent-as-block-list: current hook gives a readable reason, not a traceback" 0 \
        "expected a 'must be a plain scalar value' reason naming agent, got '${alb_after_reason:-<none>}'"
      ;;
  esac
else
  report "agent-as-block-list: current hook gives a readable reason, not a traceback" 0 \
    "expected QUARANTINED, got $alb_after_outcome"
fi
rm -rf "$alb_after_sandbox"

# ---------------------------------------------------------------------------
# Round 6 re-review (review-pr355-round5.md, blockers 1 and 2): the round-5
# junk-prefix walk itself became the bypass, two independent ways.
#
# Blocker 1 -- fail-open safety cap: `_fence_hidden_behind_junk_prefix()`
# returned False (== LEGACY) on cap-hit, so any junk prefix past 256
# characters routed straight back into the unvalidated LEGACY branch. Exact
# boundary reproduced: 256 rejects, 257 does not. Fixture 37 pins the
# still-correct boundary (must stay QUARANTINED before AND after this
# round's fix -- it was never broken); fixture 38 is the one-character-over
# case that WAS broken; fixture 39 shows no exotic bytes are needed at all
# (1 NUL + 300 ordinary spaces). Fixed by removing the cap: the walk
# already stops at the first VISIBLE character, so it was already
# O(prefix), not O(file) -- a cap was never load-bearing for anything the
# walk doesn't already bound itself.
#
# Blocker 2 -- category enumeration, not classes: `_JUNK_CATEGORIES`
# enumerated six members (Cc/Cf/Cs/Co/Mn/Me) and the comment claimed this
# excluded "every visible category", which was false -- it also
# (unintentionally) excluded Cn (unassigned, where the Default_Ignorable
# ranges live -- code points renderers are REQUIRED not to draw) and Mc
# (spacing mark). Fixtures 40-43 pin Default_Ignorable Cn code points,
# fixture 44 a plain unassigned Cn, fixture 45 an Mc spacing mark. Fixed by
# testing the category CLASS (`unicodedata.category(c)[0]`) instead of
# enumerating members: `C`/`Z`/`M`, i.e. "not a Letter, Number,
# Punctuation, or Symbol".
#
# Fixture 46 is a THIRD genuine-legacy control (leading blank lines, real
# `\s`, before front-matter-less content) alongside 34/35, proving the
# blocker fixes don't regress the false-positive risk that killed the
# round-4 candidate.
# ---------------------------------------------------------------------------
declare -A round6_bypass_fixtures=(
  ["38-legacy-bypass-cap-boundary-257.md"]="front-matter fence present but not recognised"
  ["39-legacy-bypass-nul-plus-spaces.md"]="front-matter fence present but not recognised"
  ["40-legacy-bypass-cn-default-ignorable-2065.md"]="front-matter fence present but not recognised"
  ["41-legacy-bypass-cn-default-ignorable-fff0.md"]="front-matter fence present but not recognised"
  ["42-legacy-bypass-cn-default-ignorable-e0002.md"]="front-matter fence present but not recognised"
  ["43-legacy-bypass-cn-default-ignorable-e0080.md"]="front-matter fence present but not recognised"
  ["44-legacy-bypass-cn-unassigned-0378.md"]="front-matter fence present but not recognised"
  ["45-legacy-bypass-mc-spacing-mark-0903.md"]="front-matter fence present but not recognised"
)
for r6_name in "${!round6_bypass_fixtures[@]}"; do
  check_legacy_bypass_regression \
    "round 6 junk-prefix-walk bypass ($r6_name)" \
    "$PRE_CAP_AND_CATEGORY_BUG_HOOK" \
    "$FIXTURES/$r6_name" \
    "${round6_bypass_fixtures[$r6_name]}"
done

# The cap boundary itself: 256 was ALREADY correctly rejected before this
# round's fix (the walk's old `i <= n` with `n = min(len, 256)` covers
# positions 0..256 inclusive) -- this fixture must stay QUARANTINED under
# BOTH the pre-fix and current hook, proving the fix didn't just move the
# boundary rather than removing it.
r6_256_fixture="$FIXTURES/37-legacy-bypass-cap-boundary-256.md"
r6_256_base="$(basename "$r6_256_fixture")"
if [ -f "$PRE_CAP_AND_CATEGORY_BUG_HOOK" ]; then
  r6_256_pre_sandbox="$(run_hook_sandbox "$PRE_CAP_AND_CATEGORY_BUG_HOOK" "$r6_256_fixture")"
  r6_256_pre_outcome="$(outcome_of "$r6_256_pre_sandbox" "$r6_256_base")"
  if [ "$r6_256_pre_outcome" = "QUARANTINED" ]; then
    report "round 6 cap boundary: exactly 256 junk chars already quarantines pre-fix (boundary, not a regression)" 1
  else
    report "round 6 cap boundary: exactly 256 junk chars already quarantines pre-fix (boundary, not a regression)" 0 \
      "expected QUARANTINED, got $r6_256_pre_outcome"
  fi
  rm -rf "$r6_256_pre_sandbox"
else
  report "round 6 cap boundary: missing vendored pre-fix hook" 0 "$PRE_CAP_AND_CATEGORY_BUG_HOOK not found"
fi
r6_256_post_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$r6_256_fixture")"
r6_256_post_outcome="$(outcome_of "$r6_256_post_sandbox" "$r6_256_base")"
if [ "$r6_256_post_outcome" = "QUARANTINED" ]; then
  report "round 6 cap boundary: exactly 256 junk chars still quarantines after cap removal" 1
else
  report "round 6 cap boundary: exactly 256 junk chars still quarantines after cap removal" 0 \
    "expected QUARANTINED, got $r6_256_post_outcome"
fi
rm -rf "$r6_256_post_sandbox"

# Third genuine-legacy control (leading blank lines) -- forward-only, this
# behaviour was never broken.
r6_gl_fixture="$FIXTURES/46-genuine-legacy-leading-blank-lines.md"
r6_gl_base="$(basename "$r6_gl_fixture")"
r6_gl_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$r6_gl_fixture")"
r6_gl_outcome="$(outcome_of "$r6_gl_sandbox" "$r6_gl_base")"
if [ "$r6_gl_outcome" = "ARCHIVED" ] \
  && grep -qF '<!-- legacy -->' "$r6_gl_sandbox/.claude/docs/decisions.md" 2>/dev/null; then
  report "legacy-fence-fix control: $r6_gl_base still archives as LEGACY (not falsely rejected)" 1
else
  report "legacy-fence-fix control: $r6_gl_base still archives as LEGACY (not falsely rejected)" 0 \
    "expected ARCHIVED under <!-- legacy -->, got outcome=$r6_gl_outcome"
fi
rm -rf "$r6_gl_sandbox"

# ---------------------------------------------------------------------------
# Quadratic Cf-strip regression (round 6 re-review, review-pr355-round5.md,
# high): `while text and unicodedata.category(text[0]) == "Cf": text =
# text[1:]` re-copies the whole remaining string every iteration -- O(N^2)
# for N leading Cf characters. Fixed by scanning for the run's end with an
# index, then slicing once. Timing-based, not outcome-based: both hooks
# must reach the SAME correct outcome (a valid reviewer/PASS drop with
# 150000 leading BOMs archives normally either way -- this is not a
# correctness bug), so the assertion is that the vendored pre-fix hook
# EXCEEDS a small budget while the current hook stays well UNDER a much
# larger one, at the SAME input size -- not both compared against one
# shared threshold. 500000 leading BOMs was chosen after measuring both
# hooks end-to-end on this box: pre-fix ~3.0s, post-fix ~0.09s, a ~34x
# gap. The 1000ms/2000ms thresholds below both have wide margin on either
# side of that measurement -- a CI runner would need to be roughly 3x
# slower than this box before the post-fix assertion could false-fail, or
# 3x faster before the pre-fix one could.
# ---------------------------------------------------------------------------
perf_dir="$(mktemp -d)"
perf_fixture="$perf_dir/perf-500k-bom.md"
python3 - "$perf_fixture" <<'PYPERF'
import sys
n = 500_000
content = ("﻿" * n) + (
    "---\n"
    "id: reviewer-20260821T000050Z-perf-check\n"
    "agent: reviewer\n"
    "scope: review\n"
    "created: 2026-08-21T00:00:00Z\n"
    "verdict: PASS\n"
    "blockers: []\n"
    "---\n\nbody\n"
)
with open(sys.argv[1], "w", encoding="utf-8") as f:
    f.write(content)
PYPERF

if [ -f "$PRE_CAP_AND_CATEGORY_BUG_HOOK" ]; then
  perf_pre_start=$(date +%s%N)
  perf_pre_sandbox="$(run_hook_sandbox "$PRE_CAP_AND_CATEGORY_BUG_HOOK" "$perf_fixture")"
  perf_pre_end=$(date +%s%N)
  perf_pre_ms=$(( (perf_pre_end - perf_pre_start) / 1000000 ))
  rm -rf "$perf_pre_sandbox"

  perf_post_start=$(date +%s%N)
  perf_post_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$perf_fixture")"
  perf_post_end=$(date +%s%N)
  perf_post_ms=$(( (perf_post_end - perf_post_start) / 1000000 ))
  rm -rf "$perf_post_sandbox"

  if [ "$perf_pre_ms" -gt 1000 ]; then
    report "perf: quadratic Cf-strip regression -- vendored pre-fix hook exceeds the 1s budget on 500k leading BOMs" 1
  else
    report "perf: quadratic Cf-strip regression -- vendored pre-fix hook exceeds the 1s budget on 500k leading BOMs" 0 \
      "expected >1000ms (demonstrating the quadratic cost), got ${perf_pre_ms}ms -- $PRE_CAP_AND_CATEGORY_BUG_HOOK may no longer represent the pre-fix state, or this runner is unexpectedly fast"
  fi

  if [ "$perf_post_ms" -lt 2000 ]; then
    report "perf: current hook stays well inside the 2s budget on 500k leading BOMs (scan-and-slice-once fix)" 1
  else
    report "perf: current hook stays well inside the 2s budget on 500k leading BOMs (scan-and-slice-once fix)" 0 \
      "expected <2000ms, got ${perf_post_ms}ms"
  fi
else
  report "perf: missing vendored pre-fix hook" 0 "$PRE_CAP_AND_CATEGORY_BUG_HOOK not found"
  report "perf: current hook stays well inside the 2s budget on 500k leading BOMs (scan-and-slice-once fix)" 0 \
    "skipped -- pre-fix hook missing"
fi
rm -rf "$perf_dir"

# ---------------------------------------------------------------------------
# Route 3 (round 7 re-review -- the user's explicit decision to fix this
# in-PR rather than defer it): an opening fence with NO closing fence at
# all falls through to LEGACY unvalidated. Pre-existing since before
# round 4, reachable with no exotic bytes and no prefix trickery -- just
# omit the trailing `---`.
#
# Fixture 47 is the reported exploit. Fixture 48 stress-tests the fix
# against padding evasion (60 prose lines between the opening fence and
# the real schema-shaped block) -- an EARLIER, bounded-scan version of
# this fix (self-caught and discarded before shipping, see the
# `_unterminated_fence_looks_schema_shaped()` block comment for the full
# iteration history) would have missed this one. Fixtures 49 and 50 are
# genuine-legacy controls: 49 is the coordinator's explicitly-requested
# case (a genuine legacy drop that itself opens with a `---` horizontal
# rule and has no closing fence -- ordinary markdown, must stay LEGACY,
# not error); 50 is an adversarial-but-honest stress case found while
# iterating on the fix (two of the five required field names appearing by
# coincidence, but separated by ordinary prose, in an otherwise genuine
# document) that an earlier "two hits anywhere" version of the fix would
# have wrongly quarantined.
# ---------------------------------------------------------------------------
check_legacy_bypass_regression \
  "route 3: opening fence, no closing fence (47-legacy-bypass-unterminated-fence.md)" \
  "$UNTERMINATED_FENCE_BUG_HOOK" \
  "$FIXTURES/47-legacy-bypass-unterminated-fence.md" \
  "front-matter fence opened but never closed"
check_legacy_bypass_regression \
  "route 3: opening fence, no closing fence, padded to evade a bounded scan (48-legacy-bypass-unterminated-fence-padded.md)" \
  "$UNTERMINATED_FENCE_BUG_HOOK" \
  "$FIXTURES/48-legacy-bypass-unterminated-fence-padded.md" \
  "front-matter fence opened but never closed"

for r3_genuine_name in "49-genuine-legacy-hr-open-unterminated.md" "50-genuine-legacy-prose-separated-coincidence.md"; do
  r3_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$FIXTURES/$r3_genuine_name")"
  r3_outcome="$(outcome_of "$r3_sandbox" "$r3_genuine_name")"
  if [ "$r3_outcome" = "ARCHIVED" ] \
    && grep -qF '<!-- legacy -->' "$r3_sandbox/.claude/docs/decisions.md" 2>/dev/null; then
    report "legacy-fence-fix control: $r3_genuine_name still archives as LEGACY (not falsely rejected)" 1
  else
    report "legacy-fence-fix control: $r3_genuine_name still archives as LEGACY (not falsely rejected)" 0 \
      "expected ARCHIVED under <!-- legacy -->, got outcome=$r3_outcome"
  fi
  rm -rf "$r3_sandbox"
done

# ---------------------------------------------------------------------------
# Round 6 re-review (review-pr355-round6.md, blocker 2 -- restate the
# residual's width honestly): fixtures 49/50 above cover genuine legacy
# content the discriminator correctly leaves alone. Fixtures 51/52 are the
# OPPOSITE kind of control -- they PIN the documented, knowingly-unclosed
# false-positive residual itself, so a future change that accidentally
# narrows or widens it is visible here rather than only in a comment.
# Both are real prose with real colons, NOT adjacent, and are hard
# QUARANTINED by the current hook -- this is the accepted trade recorded
# in the RESIDUAL paragraph above `_unterminated_fence_looks_schema_shaped()`,
# not a bug being asserted as fixed. Also checked against the vendored
# pre-route-3 hook to confirm this residual is specific to the route-3 fix
# (both correctly stayed LEGACY before route 3 existed at all).
# ---------------------------------------------------------------------------
for r3_residual_name in "51-genuine-legacy-colon-prose-not-adjacent.md" "52-genuine-legacy-colon-prose-short.md"; do
  r3_residual_pre_sandbox="$(run_hook_sandbox "$UNTERMINATED_FENCE_BUG_HOOK" "$FIXTURES/$r3_residual_name")"
  r3_residual_pre_outcome="$(outcome_of "$r3_residual_pre_sandbox" "$r3_residual_name")"
  if [ "$r3_residual_pre_outcome" = "ARCHIVED" ] \
    && grep -qF '<!-- legacy -->' "$r3_residual_pre_sandbox/.claude/docs/decisions.md" 2>/dev/null; then
    report "residual control: $r3_residual_name archived as LEGACY before route 3 existed (confirms the residual is route-3-specific)" 1
  else
    report "residual control: $r3_residual_name archived as LEGACY before route 3 existed (confirms the residual is route-3-specific)" 0 \
      "expected ARCHIVED under <!-- legacy --> pre-route-3, got outcome=$r3_residual_pre_outcome"
  fi
  rm -rf "$r3_residual_pre_sandbox"

  r3_residual_sandbox="$(run_hook_sandbox "$CURRENT_HOOK" "$FIXTURES/$r3_residual_name")"
  r3_residual_outcome="$(outcome_of "$r3_residual_sandbox" "$r3_residual_name")"
  if [ "$r3_residual_outcome" = "QUARANTINED" ]; then
    report "residual control: $r3_residual_name is quarantined -- the documented, knowingly-unclosed residual, pinned rather than only described" 1
  else
    report "residual control: $r3_residual_name is quarantined -- the documented, knowingly-unclosed residual, pinned rather than only described" 0 \
      "expected QUARANTINED (the documented residual), got outcome=$r3_residual_outcome -- if this now passes as ARCHIVED, the RESIDUAL paragraph in scribe-decision-merger.sh needs updating to match, not this test silently changed"
  fi
  rm -rf "$r3_residual_sandbox"
done

# ===========================================================================
# 2. Corpus coverage -- 52 cases (18 cross-checked against PyYAML 6.0.3;
#    19-52 are the forgery/legacy-bypass/scalar-coercion/junk-prefix-walk/
#    unterminated-fence fixtures added in later rounds, batch-run here
#    alongside the original 18 to prove the hook still handles a mixed
#    inbox correctly, not just one fixture at a time).
# ===========================================================================
declare -A EXPECTED=(
  ["01-schema-nested-2blockers.md"]="ARCHIVED"
  ["02-schema-pass.md"]="ARCHIVED"
  ["03-flow-style.md"]="ARCHIVED"
  ["04-orphan-list-item.md"]="QUARANTINED"
  ["05-pass-with-blockers.md"]="QUARANTINED"
  ["06-nc-empty-blockers.md"]="QUARANTINED"
  # Legitimate outcome change from the whitespace-aware indent fix (round 2
  # of the blocker #4 re-review): `line.lstrip()` now counts a tab as
  # indentation, so the tab-indented list-item continuation lines
  # (`line:`/`reason:` under `- file:`) are correctly recognised as
  # extending the same blockers entry instead of being miscounted as
  # top-level lines. The fixture's own name says "tab-indent" -- it was
  # QUARANTINED before only because the prior space-only indent
  # calculation mis-parsed valid tab indentation, not because the drop was
  # actually malformed. Strict YAML itself forbids tabs for indentation
  # entirely (would be a PyYAML syntax error), so this is the SAME class of
  # "known, reviewed divergence from strict YAML" as 08-ragged-indent below
  # -- flattening/lenient-continuation behaviour this hook already accepts
  # by design, now reachable via tabs too. No PASS<->blockers gate hole:
  # the six guarded field names (GUARDED_TOPLEVEL_FIELDS) are unaffected by
  # this, since the continuation branch was never the vulnerable one.
  ["07-tab-indent.md"]="ARCHIVED"
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
  ["19-nested-agent-verdict-forgery.md"]="ARCHIVED"
  ["20-nested-forgery-tab.md"]="ARCHIVED"
  ["21-nested-forgery-nbsp.md"]="ARCHIVED"
  ["22-nested-forgery-ideographic-space.md"]="ARCHIVED"
  ["23-nested-forgery-vertical-tab.md"]="ARCHIVED"
  ["24-nested-forgery-form-feed.md"]="ARCHIVED"
  ["25-nested-forgery-em-space.md"]="ARCHIVED"
  ["26-blockers-nested-shadow.md"]="QUARANTINED"
  ["27-nested-forgery-cr.md"]="ARCHIVED"
  ["28-legacy-bypass-space-then-bom.md"]="QUARANTINED"
  ["29-legacy-bypass-nul.md"]="QUARANTINED"
  ["30-legacy-bypass-bel.md"]="QUARANTINED"
  ["31-legacy-bypass-combining-acute.md"]="QUARANTINED"
  ["32-legacy-bypass-bom-newline-bom.md"]="QUARANTINED"
  ["33-legacy-bypass-cr-only.md"]="QUARANTINED"
  ["34-genuine-legacy-body-hr.md"]="ARCHIVED"
  ["35-genuine-legacy-short-heading-then-hr.md"]="ARCHIVED"
  ["36-agent-block-list-not-scalar.md"]="QUARANTINED"
  ["37-legacy-bypass-cap-boundary-256.md"]="QUARANTINED"
  ["38-legacy-bypass-cap-boundary-257.md"]="QUARANTINED"
  ["39-legacy-bypass-nul-plus-spaces.md"]="QUARANTINED"
  ["40-legacy-bypass-cn-default-ignorable-2065.md"]="QUARANTINED"
  ["41-legacy-bypass-cn-default-ignorable-fff0.md"]="QUARANTINED"
  ["42-legacy-bypass-cn-default-ignorable-e0002.md"]="QUARANTINED"
  ["43-legacy-bypass-cn-default-ignorable-e0080.md"]="QUARANTINED"
  ["44-legacy-bypass-cn-unassigned-0378.md"]="QUARANTINED"
  ["45-legacy-bypass-mc-spacing-mark-0903.md"]="QUARANTINED"
  ["46-genuine-legacy-leading-blank-lines.md"]="ARCHIVED"
  ["47-legacy-bypass-unterminated-fence.md"]="QUARANTINED"
  ["48-legacy-bypass-unterminated-fence-padded.md"]="QUARANTINED"
  ["49-genuine-legacy-hr-open-unterminated.md"]="ARCHIVED"
  ["50-genuine-legacy-prose-separated-coincidence.md"]="ARCHIVED"
  # QUARANTINED, not a bug: these two pin the documented, knowingly-
  # unclosed false-positive residual (round 6 re-review, blocker 2) -- see
  # the RESIDUAL paragraph above _unterminated_fence_looks_schema_shaped()
  # in scribe-decision-merger.sh.
  ["51-genuine-legacy-colon-prose-not-adjacent.md"]="QUARANTINED"
  ["52-genuine-legacy-colon-prose-short.md"]="QUARANTINED"
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
# GIT COMMIT DETECTION -- lib/git-commit-detect.sh and the four commit-time
# PreToolUse hooks that source it.
# ===========================================================================
#
# Replaces `case "$command" in *"git commit"*)` in all four of
# enforce-conventional-commits.sh, enforce-gpg-signing.sh,
# enforce-no-secrets.sh, and block-large-files.sh. That substring match had
# two independent, confirmed bugs (PR #355 review):
#
#   - False negative: `git -C <path> commit`, `git --git-dir=... commit`,
#     and `git -c k=v commit` never contain the literal substring
#     "git commit" adjacently, so all four hooks silently exited 0 --
#     bypassing conventional-commits, GPG, secrets, and size checks in one
#     command.
#   - False positive: a payload that merely *contains* the text
#     "git commit" (e.g. `echo see: git commit -m msg`) triggered them.
#
# Two layers of coverage:
#   1. Unit tests directly against git_commit_detect.py's output (fast,
#      exercises every recognised global-flag form without needing a real
#      git repo).
#   2. End-to-end tests against the actual hook scripts via a scratch git
#      repo, invoked exactly as Claude Code would (JSON payload on stdin).
#      block-large-files.sh and enforce-conventional-commits.sh have zero
#      external tool dependencies beyond git itself, so their E2E tests
#      assert the full allow/block outcome. enforce-gpg-signing.sh and
#      enforce-no-secrets.sh depend on this machine's/runner's gpg keyring
#      and gitleaks installation respectively -- neither is guaranteed
#      present on a fresh CI runner, and asserting a specific block/allow
#      outcome there would make this suite flaky by environment rather than
#      by regression. For those two, E2E coverage instead exercises
#      argv-parsing paths that are deterministic regardless of gpg/gitleaks
#      state (the false-positive text guard, and the Merge/--no-gpg-sign
#      skip logic reached only if `-C` was seen at all) -- proving the
#      detection fix reaches those hooks without coupling the suite to
#      external tool availability.

GIT_COMMIT_DETECT_PY="$REPO_ROOT/.claude/hooks/lib/git_commit_detect.py"
PRE_FIX_DETECT_PY="$PRE_FIX/lib/git_commit_detect.py"
GIT_COMMIT_DETECT_SH="$REPO_ROOT/.claude/hooks/lib/git-commit-detect.sh"
CONVENTIONAL_COMMITS_HOOK="$REPO_ROOT/.claude/hooks/enforce-conventional-commits.sh"
GPG_SIGNING_HOOK="$REPO_ROOT/.claude/hooks/enforce-gpg-signing.sh"
NO_SECRETS_HOOK="$REPO_ROOT/.claude/hooks/enforce-no-secrets.sh"
LARGE_FILES_HOOK="$REPO_ROOT/.claude/hooks/block-large-files.sh"

# Builds a Bash-tool PreToolUse JSON payload: {"tool_input":{"command":<cmd>}}
commit_payload() {
  python3 -c 'import json,sys; print(json.dumps({"tool_input":{"command":sys.argv[1]}}))' "$1"
}

# Runs a hook script against a raw shell command string, capturing exit code
# and combined output. Sets HOOK_EXIT and HOOK_OUT.
run_commit_hook() {
  local hook_script="$1" command="$2"
  HOOK_OUT="$(commit_payload "$command" | CLAUDE_PROJECT_DIR="$REPO_ROOT" bash "$hook_script" 2>&1)"
  HOOK_EXIT=$?
}

# Runs git_commit_detect.py directly against a raw command string, printing
# its three output lines (GIT_COMMIT_MATCH / GIT_COMMIT_GLOBAL_ARGS /
# GIT_COMMIT_ARGV) for grep-based assertions.
detect_raw() {
  printf '%s' "$1" | python3 "$GIT_COMMIT_DETECT_PY" 2>/dev/null
}

# Same, but against the vendored pre-transparent-prefix-fix revision --
# used only by the round-4 regression checks below to prove those cases
# genuinely failed before this pass.
detect_raw_pre_fix() {
  printf '%s' "$1" | python3 "$PRE_FIX_DETECT_PY" 2>/dev/null
}

# Minimal scratch git repo with one committed file, so `--staged` and
# `rev-parse --show-toplevel` both have something real to operate on. Prints
# the repo path.
mk_scratch_git_repo() {
  local dir
  dir="$(mktemp -d)"
  git -C "$dir" init -q
  git -C "$dir" config user.email "hook-test@example.invalid"
  git -C "$dir" config user.name "Hook Test"
  git -C "$dir" config commit.gpgsign false
  printf 'seed\n' > "$dir/seed.txt"
  git -C "$dir" add seed.txt
  git -C "$dir" commit -q -m "chore: seed"
  printf '%s\n' "$dir"
}

# --- 1. Unit coverage: git_commit_detect.py -------------------------------

detect_out="$(detect_raw 'git -C /some/path commit -m "x"')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=1$' \
   && printf '%s' "$detect_out" | grep -qF "GIT_COMMIT_GLOBAL_ARGS=(-C /some/path)"; then
  report "detect: git -C <path> commit -m ... is caught (false negative fix)" 1
else
  report "detect: git -C <path> commit -m ... is caught (false negative fix)" 0 "$detect_out"
fi

detect_out="$(detect_raw 'git --git-dir=/x/.git commit -m y')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=1$' \
   && printf '%s' "$detect_out" | grep -qF -- "--git-dir=/x/.git"; then
  report "detect: git --git-dir=... commit is caught" 1
else
  report "detect: git --git-dir=... commit is caught" 0 "$detect_out"
fi

detect_out="$(detect_raw 'git -c user.name=x commit -m y')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=1$' \
   && printf '%s' "$detect_out" | grep -qF "GIT_COMMIT_GLOBAL_ARGS=(-c user.name=x)"; then
  report "detect: git -c k=v commit is caught" 1
else
  report "detect: git -c k=v commit is caught" 0 "$detect_out"
fi

detect_out="$(detect_raw 'echo see: git commit -m "not conventional" for details')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=0$'; then
  report "detect: payload merely containing the text 'git commit' is NOT caught (false positive fix)" 1
else
  report "detect: payload merely containing the text 'git commit' is NOT caught (false positive fix)" 0 "$detect_out"
fi

detect_out="$(detect_raw 'git status')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=0$'; then
  report "detect: unrelated git subcommand (status) is not caught" 1
else
  report "detect: unrelated git subcommand (status) is not caught" 0 "$detect_out"
fi

detect_out="$(detect_raw 'git add -A && git commit -m "x"')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=1$'; then
  report "detect: commit half of a compound (&&) command is caught" 1
else
  report "detect: commit half of a compound (&&) command is caught" 0 "$detect_out"
fi

# ---------------------------------------------------------------------------
# Round 4 re-review (review-b93b430.md, "four still-open items", flagged
# twice now -- round 2's review named these same three under-matches as
# "high"): a NAME=VALUE assignment prefix, a sudo/env wrapper, or a bare
# `( ... )` subshell in front of `git commit` all put a token OTHER than
# "git" at argv[0], so the old `os.path.basename(argv[0]) != "git"` check
# rejected every one of them -- a commit made through any of these forms
# silently skipped conventional-commits, GPG, secrets, and size checks.
# Fixed by `_strip_transparent_prefix()`. Each case is checked against the
# vendored pre-fix parser ($PRE_FIX_DETECT_PY) to prove it genuinely failed
# before, and the current parser to prove it's fixed now -- plus a control
# proving the fix doesn't turn into a false trigger (sudo with a
# flag-that-takes-a-value stays a documented, safe under-match, not a
# mis-detection).
# ---------------------------------------------------------------------------
declare -A transparent_prefix_fixtures=(
  ["GIT_AUTHOR_DATE=2020-01-01T00:00:00 git commit -m 'x'"]="env-assignment prefix"
  ["sudo git commit -m 'x'"]="sudo wrapper"
  ["env X=1 git commit -m 'x'"]="env wrapper"
  ["(git commit -m 'x')"]="parenthesised subshell"
)
for tp_cmd in "${!transparent_prefix_fixtures[@]}"; do
  tp_label="${transparent_prefix_fixtures[$tp_cmd]}"

  tp_pre_out="$(detect_raw_pre_fix "$tp_cmd")"
  if printf '%s' "$tp_pre_out" | grep -q '^GIT_COMMIT_MATCH=0$'; then
    report "detect regression: $tp_label -- vendored pre-fix parser reproduces the under-match" 1
  else
    report "detect regression: $tp_label -- vendored pre-fix parser reproduces the under-match" 0 \
      "expected GIT_COMMIT_MATCH=0, got: $tp_pre_out"
  fi

  tp_post_out="$(detect_raw "$tp_cmd")"
  if printf '%s' "$tp_post_out" | grep -q '^GIT_COMMIT_MATCH=1$'; then
    report "detect: $tp_label is now caught (round 4 transparent-prefix fix)" 1
  else
    report "detect: $tp_label is now caught (round 4 transparent-prefix fix)" 0 \
      "expected GIT_COMMIT_MATCH=1, got: $tp_post_out"
  fi
done

# The parenthesised-subshell case must also preserve a message that
# legitimately contains its own trailing paren, not truncate it.
detect_out="$(detect_raw '(git commit -m "fix(auth))")')"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=1$' \
   && printf '%s' "$detect_out" | grep -qF -- "-m 'fix(auth))'"; then
  report "detect: parenthesised subshell does not truncate a message ending in its own ')'" 1
else
  report "detect: parenthesised subshell does not truncate a message ending in its own ')'" 0 "$detect_out"
fi

# Documented safe under-match, not a false trigger: sudo with a flag that
# itself takes a value (-u <user>) is intentionally NOT special-cased (see
# the module docstring) -- `user` is mis-treated as the next command token
# and the `== "git"` check correctly fails, rather than risk over-reading.
detect_out="$(detect_raw "sudo -u someuser git commit -m 'x'")"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=0$'; then
  report "detect: sudo -u <user> git commit is a documented under-match, not mis-detected" 1
else
  report "detect: sudo -u <user> git commit is a documented under-match, not mis-detected" 0 "$detect_out"
fi

# ---------------------------------------------------------------------------
# `env -u NAME` under-match (round 5 re-review, review-pr355-round4.md,
# should-fix): the round-4 `env` branch only skipped a flag TOKEN, never a
# following value token, so `env -u FOO git commit` left `FOO` mis-treated
# as the next command token and under-matched -- while the inline comment
# at the time read as if `-u NAME` were already handled. Fixed by
# `_ENV_FLAGS_WITH_VALUE`, which also distinguishes the separate-token form
# (`-u FOO` / `--unset FOO`, consume two tokens) from the attached form
# (`--unset=FOO`, already one token -- consuming a second would eat `git`
# itself, caught and fixed during this same pass before it shipped).
# Checked against the SAME vendored round-4 parser used for the transparent-
# prefix fixtures above ($PRE_FIX_DETECT_PY) -- it already reproduces this
# specific under-match, no new vendoring needed.
# ---------------------------------------------------------------------------
declare -A env_unset_fixtures=(
  ["env -u FOO git commit -m 'x'"]="short -u, separate-token value"
  ["env --unset FOO git commit -m 'x'"]="long --unset, separate-token value"
  ["env --unset=FOO git commit -m 'x'"]="long --unset=FOO, attached value"
  ["env -u FOO -u BAR git commit -m 'x'"]="two chained -u flags"
  ["env -C /tmp git commit -m 'x'"]="short -C, separate-token value"
)
for eu_cmd in "${!env_unset_fixtures[@]}"; do
  eu_label="${env_unset_fixtures[$eu_cmd]}"

  eu_pre_out="$(detect_raw_pre_fix "$eu_cmd")"
  if printf '%s' "$eu_pre_out" | grep -q '^GIT_COMMIT_MATCH=0$'; then
    report "detect regression: env value-flag ($eu_label) -- vendored pre-fix parser reproduces the under-match" 1
  else
    report "detect regression: env value-flag ($eu_label) -- vendored pre-fix parser reproduces the under-match" 0 \
      "expected GIT_COMMIT_MATCH=0, got: $eu_pre_out"
  fi

  eu_post_out="$(detect_raw "$eu_cmd")"
  if printf '%s' "$eu_post_out" | grep -q '^GIT_COMMIT_MATCH=1$'; then
    report "detect: env value-flag ($eu_label) is now caught (round 5 fix)" 1
  else
    report "detect: env value-flag ($eu_label) is now caught (round 5 fix)" 0 \
      "expected GIT_COMMIT_MATCH=1, got: $eu_post_out"
  fi
done

# Attached-value control from the fixed bug above, made explicit: the
# attached form must NOT consume a second token (that would eat `git`
# itself). If this regresses, GIT_COMMIT_ARGV below would be missing `-m x`
# or GIT_COMMIT_MATCH would flip back to 0.
detect_out="$(detect_raw "env --unset=FOO git commit -m 'x'")"
if printf '%s' "$detect_out" | grep -q '^GIT_COMMIT_MATCH=1$' \
   && printf '%s' "$detect_out" | grep -qF "GIT_COMMIT_ARGV=(-m x)"; then
  report "detect: env --unset=FOO (attached form) does not over-consume and eat 'git'" 1
else
  report "detect: env --unset=FOO (attached form) does not over-consume and eat 'git'" 0 "$detect_out"
fi

# git-commit-detect.sh sourcing contract: GIT_COMMIT_REPO_DIR resolves to
# the ACTUAL targeted repo, not the sourcing shell's cwd.
detect_repo="$(mk_scratch_git_repo)"
detect_resolved="$(bash -c '
  source "'"$GIT_COMMIT_DETECT_SH"'"
  git_commit_detect "git -C '"'"'"$1"'"'"' commit -m x"
  printf "%s" "$GIT_COMMIT_REPO_DIR"
' _ "$detect_repo")"
# Resolve both sides through realpath so a /tmp vs /private/tmp (or similar
# symlink) mismatch on some platforms doesn't produce a false failure.
detect_repo_real="$(cd "$detect_repo" && pwd -P)"
if [ "$detect_resolved" = "$detect_repo_real" ]; then
  report "git-commit-detect.sh: GIT_COMMIT_REPO_DIR resolves the -C target, not the caller's cwd" 1
else
  report "git-commit-detect.sh: GIT_COMMIT_REPO_DIR resolves the -C target, not the caller's cwd" 0 \
    "expected $detect_repo_real, got $detect_resolved"
fi
rm -rf "$detect_repo"

# --- 2. End-to-end: enforce-conventional-commits.sh (no external deps) ----

# The greedy-sed regression: git commit -m "<subject>" -m "<body>" used to
# validate the BODY (the LAST -m) instead of the subject (the FIRST -m).
run_commit_hook "$CONVENTIONAL_COMMITS_HOOK" \
  'git commit -m "fix(auth): reject expired tokens" -m "this is free-form body text, not a conventional subject"'
if [ "$HOOK_EXIT" -eq 0 ]; then
  report "conventional-commits: first -m (valid subject) wins over a non-conventional second -m (greedy-sed regression)" 1
else
  report "conventional-commits: first -m (valid subject) wins over a non-conventional second -m (greedy-sed regression)" 0 \
    "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
fi

# Same shape, but the FIRST -m is the bad one -- must still block.
run_commit_hook "$CONVENTIONAL_COMMITS_HOOK" \
  'git commit -m "not conventional" -m "fix(auth): this looks conventional but is only the body"'
if [ "$HOOK_EXIT" -eq 2 ]; then
  report "conventional-commits: a non-conventional first -m still blocks even when a later -m looks conventional" 1
else
  report "conventional-commits: a non-conventional first -m still blocks even when a later -m looks conventional" 0 \
    "expected exit 2, got $HOOK_EXIT -- $HOOK_OUT"
fi

# False negative fix: git -C <path> commit with a bad subject must now block.
cc_repo="$(mk_scratch_git_repo)"
run_commit_hook "$CONVENTIONAL_COMMITS_HOOK" "git -C $cc_repo commit -m 'not conventional at all'"
if [ "$HOOK_EXIT" -eq 2 ]; then
  report "conventional-commits: git -C <path> commit with a bad subject is now caught (false negative fix)" 1
else
  report "conventional-commits: git -C <path> commit with a bad subject is now caught (false negative fix)" 0 \
    "expected exit 2, got $HOOK_EXIT -- $HOOK_OUT"
fi
rm -rf "$cc_repo"

# False positive fix: a non-commit command merely containing the text must
# never block.
run_commit_hook "$CONVENTIONAL_COMMITS_HOOK" 'echo see: git commit -m "not conventional" for details'
if [ "$HOOK_EXIT" -eq 0 ]; then
  report "conventional-commits: payload merely containing 'git commit' text is not caught (false positive fix)" 1
else
  report "conventional-commits: payload merely containing 'git commit' text is not caught (false positive fix)" 0 \
    "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
fi

# -F/--file: a valid conventional subject read from a real file passes.
cc_file_repo="$(mk_scratch_git_repo)"
printf 'feat: add thing\n\nlonger body\n' > "$cc_file_repo/msg.txt"
run_commit_hook "$CONVENTIONAL_COMMITS_HOOK" "git -C $cc_file_repo commit -F $cc_file_repo/msg.txt"
if [ "$HOOK_EXIT" -eq 0 ]; then
  report "conventional-commits: -F <file> validates the file's first line as the subject" 1
else
  report "conventional-commits: -F <file> validates the file's first line as the subject" 0 \
    "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
fi
printf 'not conventional\n' > "$cc_file_repo/msg2.txt"
run_commit_hook "$CONVENTIONAL_COMMITS_HOOK" "git -C $cc_file_repo commit -F $cc_file_repo/msg2.txt"
if [ "$HOOK_EXIT" -eq 2 ]; then
  report "conventional-commits: -F <file> with a bad subject blocks" 1
else
  report "conventional-commits: -F <file> with a bad subject blocks" 0 \
    "expected exit 2, got $HOOK_EXIT -- $HOOK_OUT"
fi
rm -rf "$cc_file_repo"

# --- 3. End-to-end: block-large-files.sh (no external deps) ---------------

lf_repo="$(mk_scratch_git_repo)"
head -c 6291456 /dev/zero > "$lf_repo/big.bin" 2>/dev/null || dd if=/dev/zero of="$lf_repo/big.bin" bs=1M count=6 >/dev/null 2>&1
git -C "$lf_repo" add big.bin

run_commit_hook "$LARGE_FILES_HOOK" "git -C $lf_repo commit -m 'fix: add binary'"
if [ "$HOOK_EXIT" -eq 2 ]; then
  report "block-large-files: git -C <path> commit with an oversized staged file is now caught (false negative fix)" 1
else
  report "block-large-files: git -C <path> commit with an oversized staged file is now caught (false negative fix)" 0 \
    "expected exit 2, got $HOOK_EXIT -- $HOOK_OUT"
fi

run_commit_hook "$LARGE_FILES_HOOK" 'echo see: git commit -m "whatever" for details'
if [ "$HOOK_EXIT" -eq 0 ]; then
  report "block-large-files: payload merely containing 'git commit' text is not caught (false positive fix)" 1
else
  report "block-large-files: payload merely containing 'git commit' text is not caught (false positive fix)" 0 \
    "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
fi

git -C "$lf_repo" restore --staged big.bin
rm -f "$lf_repo/big.bin"
run_commit_hook "$LARGE_FILES_HOOK" "git -C $lf_repo commit -m 'fix: nothing large staged'"
if [ "$HOOK_EXIT" -eq 0 ]; then
  report "block-large-files: control -- same repo, no oversized file staged, passes" 1
else
  report "block-large-files: control -- same repo, no oversized file staged, passes" 0 \
    "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
fi
rm -rf "$lf_repo"

# --- 4. Detection propagation: enforce-gpg-signing.sh, enforce-no-secrets.sh
#        (deterministic paths only -- see section header for why full
#        block/allow outcomes aren't asserted for these two) -------------

for pair in "GPG_SIGNING_HOOK:gpg-signing" "NO_SECRETS_HOOK:no-secrets"; do
  hook_var="${pair%%:*}"
  hook_label="${pair##*:}"
  hook_path="${!hook_var}"

  run_commit_hook "$hook_path" 'echo see: git commit -m "whatever" for details'
  if [ "$HOOK_EXIT" -eq 0 ]; then
    report "$hook_label: payload merely containing 'git commit' text is not caught (false positive fix)" 1
  else
    report "$hook_label: payload merely containing 'git commit' text is not caught (false positive fix)" 0 \
      "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
  fi

  dp_repo="$(mk_scratch_git_repo)"
  # A merge commit message reached only if -C was actually parsed through to
  # the skip logic -- deterministic regardless of gpg/gitleaks state.
  run_commit_hook "$hook_path" "git -C $dp_repo commit -m \"Merge branch 'x' into main\""
  if [ "$HOOK_EXIT" -eq 0 ]; then
    report "$hook_label: git -C <path> commit -m \"Merge ...\" is parsed through -C and skipped" 1
  else
    report "$hook_label: git -C <path> commit -m \"Merge ...\" is parsed through -C and skipped" 0 \
      "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
  fi
  rm -rf "$dp_repo"
done

# --no-gpg-sign bypass is reached through -C too.
gpg_bypass_repo="$(mk_scratch_git_repo)"
run_commit_hook "$GPG_SIGNING_HOOK" "git -C $gpg_bypass_repo commit --no-gpg-sign -m 'chore: whatever'"
if [ "$HOOK_EXIT" -eq 0 ]; then
  report "gpg-signing: --no-gpg-sign bypass is reached through -C" 1
else
  report "gpg-signing: --no-gpg-sign bypass is reached through -C" 0 \
    "expected exit 0, got $HOOK_EXIT -- $HOOK_OUT"
fi
rm -rf "$gpg_bypass_repo"

# ---------------------------------------------------------------------------
# enforce-no-secrets.sh cd-subshell regression (round 4 re-review,
# review-b93b430.md, "four still-open items"): $target_dir can pass
# `[ ! -d "$target_dir" ]` while still not being `cd`-able (e.g. mode 000 --
# exists, `-d` only needs the PARENT directory's search permission, not the
# target's own). The scan line is
# `gitleaks_out="$(cd "$target_dir" 2>/dev/null && gitleaks ... 2>&1)"` --
# on a `cd` failure, `&&` short-circuits, gitleaks never runs, and the
# command substitution's own exit status is `cd`'s failure code, 1, which
# the dispatch below reads as "leaks found" and blocks with an empty
# findings report. Reached via `git -C <mode-000-dir> commit ...` PLUS
# `CLAUDE_PROJECT_DIR` also pointed at a non-cd-able dir: `-C` on a
# non-enterable directory makes git itself fail, so
# `GIT_COMMIT_REPO_DIR` resolves empty and the hook falls back to
# `$CLAUDE_PROJECT_DIR` -- the actual $target_dir this test exercises.
# A `gitleaks` STUB is put first on PATH so the toolchain-presence check
# (section 1 of the hook, which runs BEFORE the buggy cd logic) passes
# deterministically on a runner that doesn't have real gitleaks installed;
# the stub is never actually expected to execute here (the whole point of
# the bug is that `cd` fails before gitleaks would ever be invoked) --
# a run that hits the stub is itself evidence the target_dir resolution
# took an unexpected path, not just a fixture problem.
ns_stub_bin="$(mktemp -d)"
cat > "$ns_stub_bin/gitleaks" <<'STUB'
#!/usr/bin/env bash
echo "no-secrets cd-subshell test: gitleaks stub was invoked -- target_dir did not resolve to the non-cd-able directory as expected" >&2
exit 99
STUB
chmod +x "$ns_stub_bin/gitleaks"

ns_bad_dir="$(mktemp -d)"
chmod 000 "$ns_bad_dir"

ns_cd_command="git -C $ns_bad_dir commit -m 'chore: whatever'"

NO_SECRETS_PRE_FIX_HOOK="$PRE_FIX/enforce-no-secrets.pre-cd-subshell-fix.sh"
if [ -f "$NO_SECRETS_PRE_FIX_HOOK" ]; then
  ns_pre_out="$(commit_payload "$ns_cd_command" \
    | PATH="$ns_stub_bin:$PATH" CLAUDE_PROJECT_DIR="$ns_bad_dir" bash "$NO_SECRETS_PRE_FIX_HOOK" 2>&1)"
  ns_pre_exit=$?
  if [ "$ns_pre_exit" -eq 2 ]; then
    report "no-secrets: vendored pre-fix hook reproduces the cd-subshell bug (blocks on a non-cd-able target_dir)" 1
  else
    report "no-secrets: vendored pre-fix hook reproduces the cd-subshell bug (blocks on a non-cd-able target_dir)" 0 \
      "expected exit 2, got $ns_pre_exit -- $ns_pre_out"
  fi
else
  report "no-secrets: missing vendored pre-fix hook" 0 "$NO_SECRETS_PRE_FIX_HOOK not found"
fi

ns_post_out="$(commit_payload "$ns_cd_command" \
  | PATH="$ns_stub_bin:$PATH" CLAUDE_PROJECT_DIR="$ns_bad_dir" bash "$NO_SECRETS_HOOK" 2>&1)"
ns_post_exit=$?
if [ "$ns_post_exit" -eq 0 ]; then
  report "no-secrets: current hook fails open on a non-cd-able (but existing) target_dir (cd-subshell fix)" 1
else
  report "no-secrets: current hook fails open on a non-cd-able (but existing) target_dir (cd-subshell fix)" 0 \
    "expected exit 0, got $ns_post_exit -- $ns_post_out"
fi

chmod 755 "$ns_bad_dir"
rm -rf "$ns_bad_dir" "$ns_stub_bin"

# ===========================================================================
# STATUSLINE -- .claude/statusline.py "MUST NOT raise" regressions (round 4
# re-review, review-b93b430.md, "four still-open items"). Two independent
# paths sit OUTSIDE main()'s own try/except and can turn the statusline
# into an uncaught traceback instead of the documented placeholder-on-
# failure degradation:
#   1. resolve_project_dir()'s Path.cwd() fallback, called from main()
#      BEFORE the try, raises FileNotFoundError if the process's cwd has
#      been deleted out from under it.
#   2. print(line), called from main() AFTER the try/except, raises
#      UnicodeEncodeError under a strict-ASCII stdout when `line` contains
#      non-ASCII text -- guaranteed reachable via the U+2013 "no verdict
#      recorded yet" placeholder in last_review_verdict().
# Checked against the vendored pre-fix revision (subprocess, not `source`,
# since this is a standalone script invoked as `python3 statusline.py`,
# not sourced) to prove each genuinely crashed before, and the current
# script to prove both now degrade instead of raising.
# ===========================================================================

STATUSLINE_PY="$REPO_ROOT/.claude/statusline.py"
STATUSLINE_PRE_FIX_PY="$PRE_FIX/statusline.pre-must-not-raise-fix.py"

# A minimal on-disk "repo" with no real .squad/.claude state, so
# last_review_verdict() falls through to its U+2013 placeholder (case 2
# needs that character to actually appear in the line).
sl_empty_repo="$(mktemp -d)"
mkdir -p "$sl_empty_repo/.squad/decisions/inbox" "$sl_empty_repo/.squad/decisions/quarantine" "$sl_empty_repo/.claude"

# --- Case 1: deleted cwd -> Path.cwd() raises FileNotFoundError ----------
sl_deleted_cwd_dir="$(mktemp -d)"
sl_pre_stderr="$(mktemp)"

if [ -f "$STATUSLINE_PRE_FIX_PY" ]; then
  sl_pre_exit=0
  (cd "$sl_deleted_cwd_dir" && rmdir "$sl_deleted_cwd_dir" 2>/dev/null; \
    printf '{}' | env -u CLAUDE_PROJECT_DIR python3 "$STATUSLINE_PRE_FIX_PY" >/dev/null 2>"$sl_pre_stderr") \
    || sl_pre_exit=$?
  if [ "$sl_pre_exit" -ne 0 ] && grep -q "FileNotFoundError" "$sl_pre_stderr" 2>/dev/null; then
    report "statusline: vendored pre-fix script reproduces the deleted-cwd crash (Path.cwd())" 1
  else
    report "statusline: vendored pre-fix script reproduces the deleted-cwd crash (Path.cwd())" 0 \
      "expected non-zero exit with FileNotFoundError in stderr, got exit=$sl_pre_exit stderr=$(cat "$sl_pre_stderr" 2>/dev/null)"
  fi
else
  report "statusline: missing vendored pre-fix script" 0 "$STATUSLINE_PRE_FIX_PY not found"
fi
rm -f "$sl_pre_stderr"

sl_deleted_cwd_dir2="$(mktemp -d)"
sl_post_stdout="$(mktemp)"
sl_post_stderr="$(mktemp)"
sl_post_exit=0
(cd "$sl_deleted_cwd_dir2" && rmdir "$sl_deleted_cwd_dir2" 2>/dev/null; \
  printf '{}' | env -u CLAUDE_PROJECT_DIR python3 "$STATUSLINE_PY" >"$sl_post_stdout" 2>"$sl_post_stderr") \
  || sl_post_exit=$?
if [ "$sl_post_exit" -eq 0 ] && [ -s "$sl_post_stdout" ]; then
  report "statusline: current script degrades instead of crashing on a deleted cwd (Path.cwd() fix)" 1
else
  report "statusline: current script degrades instead of crashing on a deleted cwd (Path.cwd() fix)" 0 \
    "expected exit 0 with non-empty stdout, got exit=$sl_post_exit stdout=$(cat "$sl_post_stdout" 2>/dev/null) stderr=$(cat "$sl_post_stderr" 2>/dev/null)"
fi
rm -f "$sl_post_stdout" "$sl_post_stderr"

# --- Case 2: strict-ASCII stdout -> print(line) raises UnicodeEncodeError
if [ -f "$STATUSLINE_PRE_FIX_PY" ]; then
  sl_ascii_pre_exit=0
  sl_ascii_pre_out="$(printf '{}' \
    | PYTHONIOENCODING=ascii CLAUDE_PROJECT_DIR="$sl_empty_repo" python3 "$STATUSLINE_PRE_FIX_PY" 2>&1)" \
    || sl_ascii_pre_exit=$?
  if [ "$sl_ascii_pre_exit" -ne 0 ] && printf '%s' "$sl_ascii_pre_out" | grep -q "UnicodeEncodeError"; then
    report "statusline: vendored pre-fix script reproduces the ASCII-stdout crash (print(line))" 1
  else
    report "statusline: vendored pre-fix script reproduces the ASCII-stdout crash (print(line))" 0 \
      "expected non-zero exit with UnicodeEncodeError, got exit=$sl_ascii_pre_exit output=$sl_ascii_pre_out"
  fi
else
  report "statusline: missing vendored pre-fix script (ASCII case)" 0 "$STATUSLINE_PRE_FIX_PY not found"
fi

sl_ascii_post_exit=0
sl_ascii_post_out="$(printf '{}' \
  | PYTHONIOENCODING=ascii CLAUDE_PROJECT_DIR="$sl_empty_repo" python3 "$STATUSLINE_PY" 2>&1)" \
  || sl_ascii_post_exit=$?
if [ "$sl_ascii_post_exit" -eq 0 ] && [ -n "$sl_ascii_post_out" ] && ! printf '%s' "$sl_ascii_post_out" | grep -q "Traceback"; then
  report "statusline: current script degrades instead of crashing under strict-ASCII stdout (print fix)" 1
else
  report "statusline: current script degrades instead of crashing under strict-ASCII stdout (print fix)" 0 \
    "expected exit 0 with no traceback, got exit=$sl_ascii_post_exit output=$sl_ascii_post_out"
fi

rm -rf "$sl_empty_repo"

# ===========================================================================
echo
echo "Summary: $pass passed, $fail failed"
[ "$fail" -eq 0 ]
