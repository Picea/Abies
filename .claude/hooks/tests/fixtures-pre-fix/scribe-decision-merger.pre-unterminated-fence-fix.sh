#!/usr/bin/env bash
#
# ============================================================================
# FIXTURE PROVENANCE (round 6 re-review, review-pr355-round6.md, should-fix)
# ============================================================================
# Unlike every other file under fixtures-pre-fix/, this one was NOT
# snapshotted from a real prior revision of scribe-decision-merger.sh before
# editing it -- the round-7 pass that added route 3's fix started work
# without first `cp`-ing the live file, so no true "before" snapshot exists.
# This file is a RECONSTRUCTION: the round-7 route-3 addition (the
# `_UNTERMINATED_FENCE_*` constants, `_unterminated_fence_looks_schema_shaped()`,
# and its call site in the "no closing fence" branch) was mechanically
# removed from the post-fix hook, restoring the "if not close: print(LEGACY)"
# call site to its pre-route-3 form.
#
# Faithfulness, checked rather than assumed:
#   * Reproduces the reported route-3 exploit end-to-end (an unterminated,
#     schema-shaped forged drop archives under `<!-- legacy -->` instead of
#     being quarantined) before this file was vendored here.
#   * `diff` against the current (post-route-3) hook is exactly and only the
#     route-3 addition -- no other drift.
#   * Round 6 re-review independently re-ran this file across a 49-fixture
#     behavioural sweep: it diverges from the current hook on the
#     unterminated-fence fixtures only, and matches every rounds-1-5 outcome
#     identically (nested-key forgery, whitespace-indent bypasses, the cap
#     boundary, the Cn/Mc category gap, etc.) -- i.e. it correctly represents
#     "everything through round 6 fixed, route 3 not yet fixed", not some
#     other, accidentally-different revision.
#
# The preamble below (shebang through the "Known limitation" block) is
# BYTE-IDENTICAL to the current hook's preamble, because reconstruction
# means "the current file minus one addition", not "an independently
# authored file that happens to look similar". Do not read that similarity
# as evidence this was captured from git history -- it was not; there is no
# commit this corresponds to. The sha256 pin in run.sh guards this file
# against FUTURE drift; it does not and cannot attest to historical
# accuracy, which is why this note exists instead.
# ============================================================================
#
# SubagentStop hook.
# Validates and merges decision drops from .squad/decisions/inbox/ into
# .claude/docs/decisions.md, quarantining malformed entries and updating
# the .squad/.last-review-verdict cache used by the statusline.
#
# Schema reference: .claude/docs/decision-schema.md
#
# Behaviour:
#   * Each *.md file in inbox/ is parsed for YAML front-matter.
#   * Valid drops are appended under "## Session Decisions" and moved to
#     .squad/decisions/archive/<YYYY-MM>/.
#   * Malformed drops are moved to .squad/decisions/quarantine/ with a
#     sibling .reason file containing the validator's complaint.
#   * Legacy drops with no front-matter are tolerated indefinitely, not for
#     a single cycle: appended under a <!-- legacy --> marker and archived
#     normally, with no automated flush of that marker's contents.
#   * If the most recent successfully-merged drop is from `agent: reviewer`,
#     its verdict is written to .squad/.last-review-verdict.
#
# Exit codes:
#   0 — always (this hook never blocks).
#
# Known limitation: no authorship check (round 4 re-review,
# review-b93b430.md, finding 3).
#   This hook has been through three rounds of forgery fixes (nested-key
#   promotion, whitespace/line-terminator parsing differentials, a
#   universal-newline read-time CR-to-LF translation). Every one of them
#   closed a way for a drop's DECLARED `agent:`/`verdict:` fields to
#   disagree with what the file's structure actually says, i.e. a
#   PARSING bug. None of them, and nothing else in this file, can close
#   the simpler case: a well-formed, non-nested, entirely self-consistent
#   drop that just honestly writes `agent: reviewer` / `verdict: PASS` /
#   `blockers: []` at column 0. That is accepted and archived at face
#   value, and it writes PASS to .squad/.last-review-verdict, regardless
#   of which agent (or human, editing the inbox by hand) actually wrote
#   it. The SubagentStop payload this hook receives on stdin carries only
#   `stop_hook_active` and `cwd` (see the `payload`/`STOP_ACTIVE` parsing
#   just below) -- there is no field identifying which subagent's stop
#   triggered this run, so there is no signal here that could
#   authenticate `agent:` against it. Closing THAT would need a change
#   outside this hook (e.g. the invoking harness stamping a trusted
#   agent identity into the payload) — it is not a bug in this parser,
#   and no amount of stricter YAML parsing here will fix it. Treat
#   .squad/.last-review-verdict as "the most recent well-formed drop that
#   CLAIMED to be a reviewer verdict", not as a cryptographically or
#   procedurally verified one. See decision-schema.md's "Statusline
#   integration" section for the same caveat stated for the schema's
#   readers.

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

read -r STOP_ACTIVE PAYLOAD_CWD <<<"$(
  printf '%s' "$payload" | python3 -c '
import json,sys
try:
    d = json.loads(sys.stdin.read())
except Exception:
    d = {}
print("true" if d.get("stop_hook_active") else "false", d.get("cwd",""))
' 2>/dev/null
)"

[ "${STOP_ACTIVE:-false}" = "true" ] && exit 0

project_dir="${CLAUDE_PROJECT_DIR:-${PAYLOAD_CWD:-$PWD}}"
[ -z "$project_dir" ] && project_dir="$PWD"

inbox="$project_dir/.squad/decisions/inbox"
quarantine="$project_dir/.squad/decisions/quarantine"
decisions_doc="$project_dir/.claude/docs/decisions.md"
verdict_cache="$project_dir/.squad/.last-review-verdict"

[ -d "$inbox" ] || exit 0
[ -f "$decisions_doc" ] || exit 0

shopt -s nullglob
files=( "$inbox"/*.md )
shopt -u nullglob
[ "${#files[@]}" -eq 0 ] && exit 0

mkdir -p "$quarantine"

anchor='## Session Decisions'
grep -qF "$anchor" "$decisions_doc" || printf '\n%s\n' "$anchor" >> "$decisions_doc"

today="$(date -u +%Y-%m-%d)"
archive_month="$(date -u +%Y-%m)"
archive_dir="$project_dir/.squad/decisions/archive/$archive_month"
mkdir -p "$archive_dir"

# Validator. Returns:
#   0 + stdout="VALID|<agent>|<verdict>|<scope>|<id>"  for valid front-matter drops
#   0 + stdout="LEGACY"                                 for files with no front-matter (legacy mode)
#   1 + stdout="<reason>"                               for invalid drops
validate() {
  local file="$1"
  python3 - "$file" <<'PY'
import sys, re, unicodedata
ALLOWED_AGENTS = {"reviewer","security-expert","performance-engineer","architect",
                  "curator","tech-writer","ux-expert","csharp-dev","js-dev","devops","lead",
                  "critic","realist","spec-author",
                  "dreamer-first-principles","dreamer-informed","dreamer-convergence"}
ALLOWED_VERDICTS = {"PASS","NEEDS-CHANGES","BLOCKED","INFO"}
ALLOWED_SCOPES   = {"review","decision","threat-model","benchmark","retro",
                    "handoff","architecture","doc","other"}
# TODO(#8) narrow fix -- see the indentation guard in the parse loop below
# (search GUARDED_TOPLEVEL_FIELDS). Column-0 (indent 0) is required for
# these field NAMES specifically; a nested occurrence of one of them (e.g.
# `meta:\n  agent: reviewer`) must not be promoted to -- or silently
# pre-declared at -- top level. This is the five schema-required fields
# (id/agent/verdict/scope/created) PLUS `blockers`, which isn't itself
# schema-required but gates the verdict<->blockers consistency check below
# -- reviewer-verified second exploit: a genuine top-level
# `agent: reviewer` / `verdict: PASS` with a real non-empty top-level
# `blockers` list, plus a nested `notes:\n  blockers: []`, satisfied the
# consistency check by silently zeroing the real blockers list instead of
# by forging agent/verdict. Deliberately scoped to just these six names,
# not every key: see the parse-loop comment for why.
GUARDED_TOPLEVEL_FIELDS = {"id", "agent", "verdict", "scope", "created", "blockers"}

# What counts as "invisible, not content" for _fence_hidden_behind_junk_prefix()
# below (round 6 re-review, review-pr355-round5.md, blockers 1 and 2 --
# both closed here; round 5's version of this comment shipped a false
# claim, which is what made this the third consecutive round a comment in
# this file overclaimed what the code did).
#
# BLOCKER 2 (round 5): the round-5 version of `_is_junk_char()` tested
# membership in an ENUMERATED set of six category members
# (`{"Cc","Cf","Cs","Co","Mn","Me"}`) and the comment above it claimed
# "DELIBERATELY excluded: every visible category". That claim was false --
# the enumeration also, and unintentionally, excluded `Cn` (unassigned;
# this is exactly where the Unicode Default_Ignorable_Code_Point ranges
# live, code points renderers are REQUIRED not to draw, so a `Cn` prefix
# is genuinely as invisible as a `Cf` one) and `Mc` (spacing combining
# mark). Six reviewer-verified single-character reproductions, each
# archived unvalidated under `<!-- legacy -->`: U+2065, U+FFF0, U+E0002,
# U+E0080 (all `Cn`, all Default_Ignorable), U+0378 (plain `Cn`, simply
# unassigned), U+0903 (`Mc`).
#
# Fixed by testing the CATEGORY CLASS (the general category's first
# letter -- `unicodedata.category(c)[0]`) rather than enumerating members:
# `C` (Other: Cc/Cf/Cs/Co/Cn -- every control/format/surrogate/private-use/
# unassigned code point, not just the five this file used to name), `Z`
# (Separator: Zs/Zl/Zp), `M` (Mark: Mn/Mc/Me -- every combining character,
# not just the two this file used to name). Equivalently: "not a Letter,
# Number, Punctuation, or Symbol" -- the four classes something has to
# belong to in order to actually be READ as content. `.isspace()` stays as
# an explicit extra check on top of the class test, not because the class
# test misses anything relevant here, but because it is the established,
# separately-verified predicate the fence-detection code elsewhere in this
# file already relies on (see the BOM/`\s` discussion above), kept
# consistent rather than assumed subsumed.
#
# A real markdown heading's `#` is category `Po` (Punctuation) -- class
# `P`, not `C`/`Z`/`M` -- so it is never treated as skippable junk. That is
# what keeps a genuine legacy drop's heading-then-rule body (e.g. a `#`
# heading immediately followed by a `---` separator line) on the LEGACY
# path instead of being misread as an attempted-bypass prefix; see
# fixtures/34 and /35.
def _is_junk_char(c: str) -> bool:
    return c.isspace() or unicodedata.category(c)[0] in ("C", "Z", "M")


# Same terminator set the strict `fm_lines` splitter further down accepts
# (`\r\n|\n`) PLUS a bare `\r`, deliberately wider than the primary
# `\s*---[ \t]*\r?\n` match above: this function's only job is telling
# "should this be REJECTED instead of silently falling to LEGACY", not
# "can this be safely parsed as-is" -- a CR-only opening fence is
# recognisable as an attempted fence even though nothing downstream (this
# function included) will go on to parse a CR-only body correctly.
_FENCE_OPEN_WIDE_RE = re.compile(r"---[ \t]*(?:\r\n|\r|\n)")


def _fence_hidden_behind_junk_prefix(text: str) -> bool:
    """True if an opening fence line appears at the start of ``text`` once
    a (possibly empty) run of purely invisible/non-content characters is
    skipped -- i.e. the file WAS attempting a front-matter fence, just
    behind characters the primary regex doesn't treat as leading
    whitespace. False only if a VISIBLE character is reached first with no
    fence found (a genuine legacy drop) -- there is no length cap; see the
    BLOCKER 1 note below for why one existed here once and why it doesn't
    now."""
    # BLOCKER 1 (round 5): this walk used to stop at a fixed
    # `_FENCE_SAFETY_CAP` (256) and return False -- i.e. LEGACY -- on
    # cap-hit. False here does not mean "no bypass attempt was found", it
    # means "give up and let it through unvalidated": a junk prefix ONE
    # character longer than the cap routed straight back into the exact
    # bug this function exists to close. Reviewer-verified end-to-end
    # against the real hook, exact boundary: 256 leading NUL bytes then a
    # forged fence rejects; 257 does not -- and no exotic bytes are needed
    # to reach it, "1 NUL + 300 ordinary spaces" reproduces it too. The
    # comment that used to sit here called the cap "not load-bearing for
    # correctness ... all of which have three or fewer junk characters" --
    # reasoning from the exploits already found rather than from what an
    # attacker would try next, the same over-claim shape as blocker 2
    # above, just aimed at a boundary instead of a category set.
    #
    # There is no cap now. The walk already terminates at the first
    # VISIBLE character (see `_is_junk_char()`), so it is bounded by
    # however much invisible junk actually precedes real content --
    # already O(prefix length), not O(file length), for anything that
    # isn't itself pathological; and the file is already fully resident in
    # memory by this point (read whole in `open(...).read()` above), so
    # there is no separate memory concern a cap would be protecting
    # against either. A cap here would only ever be able to fail SAFE by
    # returning True (reject) on cap-hit, never False -- and a walk that
    # already stops at the first visible character doesn't need one.
    i = 0
    n = len(text)
    while i <= n:
        if _FENCE_OPEN_WIDE_RE.match(text, i):
            return True
        if i == n or not _is_junk_char(text[i]):
            return False
        i += 1
    return False


path = sys.argv[1]
try:
    # `newline=""` -- NOT the default `newline=None` (universal newlines).
    # Universal-newline translation runs at the io layer, BEFORE this
    # function or the line-splitter below ever sees the text: every lone
    # `\r` in the file is silently rewritten to `\n` on read. That is a
    # third, outer layer of the same bug class as the whitespace-indent and
    # unguarded-`blockers` bypasses (round 4 re-review, blocker #1):
    # indenting a nested `meta:` block with a bare CR instead of a tab looks
    # like `meta:\n\ragent: reviewer\n...` on disk, but with `newline=None`
    # the `\r` is consumed as a line terminator before `fm_lines` exists, so
    # `agent: reviewer` becomes a genuinely separate, genuinely column-0
    # line -- not merely miscounted indentation, an actually-zero-indent
    # line, same as the vertical-tab/form-feed splitlines() bug this file
    # already fixed once (search "line.lstrip()" below), except one layer
    # further out: at read time, upstream of the parser entirely, where no
    # per-line indent guard could ever see it. Reviewer-verified end-to-end
    # against the un-fixed hook: bytes
    # `b"agent: csharp-dev\n\ragent: reviewer\n"` read back under
    # `newline=None` as `'agent: csharp-dev\n\nagent: reviewer\n'` -- the CR
    # is gone and `agent: reviewer` is its own line -- and a CR-indented
    # forgery archived as `[reviewer · PASS]` / wrote PASS to
    # .squad/.last-review-verdict. `newline=""` disables that translation:
    # `\r`, `\r\n` and `\n` all pass through untouched, so a lone `\r` used
    # as indentation stays part of the SAME line (`\r` is whitespace under
    # `.isspace()`/`.lstrip()`, so it's still correctly counted as
    # indentation, not silently dropped). See fixtures/27-nested-forgery-cr.md
    # and the paired fm_lines split fix immediately below -- the two must
    # change together, or a lone CR anywhere in `fm` still splits as its own
    # line via the `|\r|` alternation this pass removes.
    with open(path, "r", encoding="utf-8", newline="") as fh:
        text = fh.read()
except Exception as e:
    print(f"unreadable: {e}")
    sys.exit(1)

# Detect front-matter; tolerate leading whitespace/newlines, a leading
# UTF-8 BOM, and a degenerate empty-body fence (`---\n---\n`).
#
# TODO(#8): this fence-boundary detection used to be a single
# `re.match(r"\s*---\s*\n(.*?)\n---\s*\n", text, re.DOTALL)`, which got two
# cases wrong -- both reviewer-verified false GREENs, proved end-to-end
# against this live validator:
#   * A leading UTF-8 BOM (U+FEFF, category Cf) is not matched by `\s`, so
#     a BOM'd drop with plainly-present frontmatter read as "no front
#     matter at all" and fell through to LEGACY mode below -- archived and
#     appended to decisions.md with the whitelist, verdict-enum,
#     scope-enum, and blockers-consistency checks skipped entirely. A
#     BOM'd drop declaring `agent: bogus-not-a-real-agent` was archived
#     clean; the same drop without the BOM was correctly quarantined for
#     "unknown agent".
#   * `---\n---\n` (zero lines between the markers) needed *two* newlines
#     under the old pattern -- one ending the opening line, one before the
#     closing line -- but the input only has one, so it also fell into
#     LEGACY mode instead of being quarantined for missing required
#     fields.
# Both are fixed here: strip a leading BOM, then find the closing fence as
# its own line-anchored search rather than requiring a blank line before
# it. See .claude/hooks/tests/agent_identity.py's `_frontmatter_body()` for
# the sibling fix to the *other* frontmatter-fence regex this codebase
# has (agent charter identity resolution, a different schema, not shared
# with this parser) -- same one-line root cause, fixed independently
# because the two parsers diverge immediately after fence detection.
#
# This is DISTINCT from the nested-key-promotion bug further down (search
# GUARDED_TOPLEVEL_FIELDS below): that one is now narrowly fixed for the
# six names in GUARDED_TOPLEVEL_FIELDS (the five schema-required ones plus
# `blockers`), with
# the broader question -- every OTHER key, and the list-item acceptance
# branch generally -- still open pending a user scope decision. Only the
# fence-boundary bug above was fixed by THIS COMMENT's original change;
# the nested-key-promotion fix landed in a later pass, noted where it is.
# Strip ALL leading Unicode format-control characters (category "Cf"), not
# just a single BOM. Round 4 re-review (review-b93b430.md, finding 3, "LEGACY
# fall-through on non-\s leading characters") verified live that stripping
# only one leading U+FEFF reopens the exact BOM bug this comment block
# already describes for every OTHER invisible format character, and for a
# BOM repeated more than once: a doubled BOM, or a single leading
# ZERO WIDTH SPACE (U+200B), MONGOLIAN VOWEL SEPARATOR (U+180E), WORD JOINER
# (U+2060), or SOFT HYPHEN (U+00AD) -- none matched by `\s`, same as the
# original BOM case -- makes a drop declaring `agent: bogus-not-a-real-agent`
# / `verdict: TOTALLY-FINE` fall into LEGACY mode and archive silently, with
# ALLOWED_AGENTS/ALLOWED_VERDICTS/ALLOWED_SCOPES and the verdict<->blockers
# consistency check all skipped, exactly like the single-BOM bug this same
# comment block already claims closed. All five of those characters (and
# U+FEFF itself) are Unicode category "Cf" (format), so stripping by
# category rather than by a fixed one-character prefix widens the fix from
# one specific character to the whole category.
#
# CORRECTION (round 5 re-review, review-pr355-round4.md, blocker): the
# sentence that used to sit here claimed this "closes the whole class
# instead of pinning one more instance of it". That was wrong, and shipping
# it is exactly what earned a fifth round: this loop strips only a LEADING
# RUN of Cf characters starting at text[0]. Six live reproductions defeat
# it, each with plainly-present, grep-visible front matter that still falls
# through to LEGACY (whitelist/verdict/scope/blockers checks all skipped):
# a single ASCII space before a BOM (the loop is anchored at text[0]; a
# non-Cf character in front of it defeats the loop outright, before the Cf
# character is ever reached); a leading NUL U+0000 or BEL U+0007 (category
# "Cc", control -- not "Cf"); a leading combining acute accent U+0301
# (category "Mn", mark -- not "Cf"); a BOM, then a newline, then a second
# BOM (the second Cf run is no longer LEADING once a non-Cf newline
# interrupts it); and a drop written entirely with CR-only line endings
# (not a leading-character problem at all -- see the fence-presence check
# below for why that one is different in kind, not just another
# character). This loop is still worth keeping (see the comment on the
# fence-presence check below for why), but it is a narrow, real fix for
# the Cf case specifically -- not a closure of "the whole class" of
# leading-junk-before-the-fence bugs. That is a different, broader
# invariant, checked separately immediately after the main fence match
# below.
# Scan for the end of the leading Cf run, then slice ONCE -- NOT
# `while text and unicodedata.category(text[0]) == "Cf": text = text[1:]`,
# which round 5 re-review (review-pr355-round4.md, high) measured as
# genuinely quadratic: `text = text[1:]` re-copies the entire remaining
# string on every iteration, so N leading Cf characters cost O(N^2), not
# O(N). Measured on the reviewer's box and reproduced independently here
# (see the header of .claude/hooks/tests/run.sh's STATUSLINE section for
# the sibling perf-measurement style): 50k leading BOMs ~0.03-0.05s, 200k
# ~0.34-0.41s, 400k (1.2 MB) ~1.6-1.7s -- roughly 4x cost per doubling of
# input, the signature of quadratic behaviour. ~12 MB of leading BOMs (a
# write-access-to-the-inbox threat, the same surface as every forged-drop
# reproduction elsewhere in this file) would burn minutes of CPU inside a
# `SubagentStop` hook. `_fence_hidden_behind_junk_prefix()` below has the
# same shape of walk (advance an index while a predicate holds) and does
# NOT have this problem, because it only ever reads `text[i]` by index and
# slices at most once, at the very end, via `text[m.end():]` /
# `rest[:close.start()]` elsewhere in this function -- never inside its
# own loop. This loop now does the same: advance an index, slice once.
_cf_end = 0
while _cf_end < len(text) and unicodedata.category(text[_cf_end]) == "Cf":
    _cf_end += 1
text = text[_cf_end:]
# Leading run: `\s*`, anchored at text-start (post-format-strip), so it can
# only ever consume whitespace before matching `---` -- NOT narrowed to
# `[ \t\r\n]`, which excludes 25 other code points Python's `\s` matches on
# `str` (NBSP U+00A0, form feed U+000C, vertical tab U+000B, U+001C-001F,
# U+0085, U+1680, U+2000-200A, U+2028, U+2029, U+202F, U+205F, U+3000). A
# prior narrowing here reopened the exact bug this comment block already
# describes for the BOM: an NBSP/form-feed/etc-prefixed drop with a bogus
# `agent:` fell into LEGACY mode (whitelist/verdict/scope/blockers checks
# skipped) instead of being parsed and quarantined -- reviewer-verified
# end-to-end, same blast radius as the BOM case above. The trailing
# `[ \t]*` immediately below is intentionally NOT `\s*`: it sits before the
# fence line's own newline and must not consume it, so it stays restricted
# to horizontal whitespace. See .claude/hooks/tests/agent_identity.py's
# `_frontmatter_body()` for the sibling fix.
m = re.match(r"\s*---[ \t]*\r?\n", text)
if not m:
    # Legacy mode -- no front-matter at all... usually. Round 5 re-review
    # (review-pr355-round4.md, the one blocker): before declaring LEGACY,
    # check whether an opening fence is sitting just behind a SMALL run of
    # characters that are individually invisible/non-content (control,
    # format, surrogate, private-use, combining-mark, or whitespace
    # categories) rather than assuming the fence match failing means there
    # is no fence at all. This is what actually closes the six
    # reproductions above -- not by enumerating one more Unicode category
    # to pre-strip (that is the mistake the Cf-loop comment corrected
    # above already made once), but by refusing to fall through to LEGACY
    # just because the FIRST match attempt failed.
    #
    # Why not the simpler `re.search(r"(?m)^---", text)` over the WHOLE
    # document (the reviewer's first verified candidate)? Tried it, and it
    # is wrong in a way the 173/173 suite at the time could not see: it
    # also matches a legitimate horizontal rule in the BODY of a genuine,
    # front-matter-less legacy drop -- verified live against a realistic
    # legacy entry shaped like the ones already archived in decisions.md
    # (a heading, a paragraph, a `---` body separator, more prose): the
    # unbounded search finds that `---` and the fix would REJECT an
    # honest legacy drop outright, converting silent-but-recoverable
    # LEGACY tolerance into a hard quarantine error for content that was
    # never trying to forge anything. That is a worse failure than the
    # hole it closes -- LEGACY mode exists specifically so pre-schema
    # content doesn't need to be hand-fixed under time pressure.
    #
    # The walk below is deliberately narrower than "search the whole
    # document": it only ever advances past characters that are
    # themselves invisible/non-content, starting from position 0, and
    # stops the instant it meets a VISIBLE character (this is why a
    # genuine `# Notes\n---\n` -- a real heading immediately followed by
    # a body rule -- stays LEGACY: `#` is visible, the walk never gets
    # past it). It also re-tries the fence match with a WIDER terminator
    # (`\r\n|\r|\n`, not just `\r?\n`) at every step, which is what
    # catches the CR-only case even though it has zero leading junk
    # characters at all -- that file's problem isn't a prefix, it's that
    # its own genuine opening fence ends in a bare `\r` the primary regex
    # was never written to accept (deliberately -- `fm_lines` below still
    # only treats `\r\n`/`\n` as line terminators, so a CR-only drop that
    # got past this check would still fail to parse correctly; REJECTing
    # it here for a human to look at is the honest outcome, not silently
    # parsing it wrong).
    #
    # `_is_junk_char()` (defined above `_FENCE_OPEN_WIDE_RE`) is what
    # actually decides "invisible" here -- see its own comment for the
    # category-class test and why `#` (and every other visible character)
    # stops the walk. This function has no length cap: round 6 re-review
    # (review-pr355-round5.md, blocker) removed the one that used to sit
    # here, because it was fail-open (returned False -- LEGACY -- on
    # cap-hit) and therefore WAS load-bearing for correctness, the
    # opposite of what an earlier version of this comment claimed. See the
    # BLOCKER 1 note above `_fence_hidden_behind_junk_prefix()`'s loop for
    # the full history; this is a shorter pointer to it, not a second copy
    # of the claim -- the duplication of that same claim in two places is
    # exactly how it survived one edit and not the other.
    if _fence_hidden_behind_junk_prefix(text):
        print("front-matter fence present but not recognised "
              "(unsupported character(s) before the opening fence, or an "
              "unsupported line-ending convention)")
        sys.exit(1)
    print("LEGACY")
    sys.exit(0)
rest = text[m.end():]
# `(?:\r?\n|\Z)`, not a bare `\r?\n`: a closing fence at end-of-file with
# no trailing newline (`---\nid: x\n---`, no final `\n`) is a
# syntactically complete fence, but a newline-only terminator never
# matches at `\Z` -- pre-existing, same class as the empty-body-fence bug
# above, not introduced by this pass. `\Z` rather than `$` because `$` in
# MULTILINE mode also matches just before a trailing `\n`, which would
# wrongly accept a fence line followed by more content on the same
# logical line. See agent_identity.py's `_frontmatter_body()` for the
# sibling fix.
close = re.search(r"(?m)^---[ \t]*(?:\r?\n|\Z)", rest)
if not close:
    print("LEGACY")
    sys.exit(0)
fm = rest[:close.start()]

# Minimal YAML parse: lines of `key: value`, plus list items starting with
# `- ` (block style, e.g. `  - file: ...`) whose *further-indented* follow-on
# `key: value` lines extend the same list item — the nested-mapping form
# documented as canonical in decision-schema.md. We don't need the full spec.
def _unquote(v):
    if (v.startswith('"') and v.endswith('"')) or (v.startswith("'") and v.endswith("'")):
        return v[1:-1]
    return v

fields = {}
current_list = None        # name of the key currently collecting list items
current_item = None        # dict of the list item currently being extended
current_item_indent = None # indentation column of that item's `- ` marker

# Line splitting: `fm.splitlines()` (str.splitlines(), no args) is too
# permissive for this purpose -- besides \r\n/\r/\n it ALSO treats
# vertical tab (\x0b), form feed (\x0c), \x1c-\x1e, NEL (\x85), LS
# (\u2028) and PS (\u2029) as line terminators. Reviewer-verified: a
# vertical-tab or form-feed character placed where an indentation prefix
# would go doesn't get miscounted as indentation at all -- splitlines()
# consumes it as a boundary FIRST, so `meta:\n\x0bagent: reviewer` becomes
# the two independent lines "meta:" and "agent: reviewer", the second with
# GENUINELY zero leading whitespace, not merely zero as miscounted. No
# indentation guard can distinguish that from a real column-0 declaration,
# because by the time the guard runs it IS one. Splitting on exactly
# \r\n / \n (matching the CRLF fixture's expectations, nothing broader)
# closes this at the source: none of those six extra separators split a
# line here, so a vertical-tab/form-feed/etc used as indentation stays
# part of the SAME line and is correctly counted below.
#
# NOTE (round 4 re-review, blocker #1): the alternation is `\r\n|\n` --
# deliberately NOT `\r\n|\r|\n` as this comment previously described (that
# three-way form is now a documented dead end, not a live behaviour). A
# bare `\r` alone is no longer a line terminator here at all, and that is
# now load-bearing rather than incidental: with `newline=""` on the read
# above, a lone `\r` used as an indentation prefix (e.g.
# `meta:\n\ragent: reviewer\n`) reaches this point as a literal character
# INSIDE the "agent: reviewer" line, not as a separator that would produce
# a second, genuinely-column-0 line -- `\r` is still whitespace under
# `.isspace()`, so `line.lstrip()` below counts it correctly as
# indentation. Splitting on a bare `\r` here would silently reopen exactly
# the bug the `newline=""` change above closes: it would manufacture a
# fresh, genuinely-zero-indent line at the split point regardless of what
# the file's actual bytes intended, the same "no indent guard can
# distinguish real column-0 from manufactured column-0" failure mode as
# the vertical-tab/form-feed case above, just re-derived one call site
# later. `\r\n` stays in the alternation (tried first, so it still wins
# over the bare-`\n` branch) because CRLF is a genuine, intentional
# two-character line terminator -- see fixtures/12-crlf.md -- not an
# indentation character standing in for one.
fm_lines = re.split(r"\r\n|\n", fm)

for raw in fm_lines:
    line = raw.rstrip()
    if not line or line.lstrip().startswith("#"):
        continue
    # Whitespace-aware indent -- `line.lstrip()` (no args) strips every
    # Unicode "White_Space" character, not just U+0020. The prior
    # `line.lstrip(" ")` counted ASCII-space indentation only, so a nested
    # `agent:`/`verdict:`/etc. line indented with a tab, NBSP (U+00A0),
    # ideographic space (U+3000), vertical tab, form feed, or em space
    # (U+2003) measured as indent 0 -- the GUARDED_TOPLEVEL_FIELDS check
    # above never fired, and `.strip()` a few lines below removes the same
    # character anyway, so the key parsed cleanly as `agent`/`verdict`/etc.
    # Reviewer-verified end-to-end: all six characters bypassed the
    # column-0 guard and forged `[reviewer · PASS]` into decisions.md and
    # `.squad/.last-review-verdict`. See
    # fixtures/20-nested-forgery-tab.md through
    # fixtures/25-nested-forgery-em-space.md.
    indent = len(line) - len(line.lstrip())
    stripped = line.strip()

    if stripped.startswith("- "):
        if current_list is None:
            print("malformed yaml: list item without parent key")
            sys.exit(1)
        item_body = stripped[2:].strip()
        # `- key: value` starts a mapping item; further-indented lines below
        # it extend the same mapping (see the continuation branch below).
        # Flow-style (`- {k: v, ...}`) and bare scalar items are stored as-is.
        if item_body.startswith("{") or ":" not in item_body:
            fields.setdefault(current_list, []).append(item_body)
            current_item = None
        else:
            k, _, v = item_body.partition(":")
            current_item = {k.strip(): _unquote(v.strip())}
            fields.setdefault(current_list, []).append(current_item)
        current_item_indent = indent
        continue

    if current_item is not None and indent > current_item_indent and ":" in stripped:
        # Continuation of the current list item's mapping (e.g. `line:`,
        # `reason:` following `- file:` at a deeper indent).
        k, _, v = stripped.partition(":")
        current_item[k.strip()] = _unquote(v.strip())
        continue

    if ":" in stripped:
        k, _, v = stripped.partition(":")
        k = k.strip()
        v = v.strip()
        current_item = None
        current_item_indent = None

        # TODO(#8) -- NARROWLY FIXED here (see GUARDED_TOPLEVEL_FIELDS
        # above and .claude/hooks/tests/fixtures/19-nested-agent-verdict-forgery.md).
        # Reached only for a bare `key: value` line that is neither a list
        # item nor a list-item continuation (both handled above and already
        # indentation-aware via current_item_indent) -- i.e. a line nested
        # under an unrelated bare key, e.g. `meta:\n  agent: reviewer`.
        # This used to promote ANY such line to top level regardless of
        # indentation, last write wins. Reviewer-verified forgery this
        # enabled, now closed by the guard below:
        #   agent: csharp-dev
        #   verdict: NEEDS-CHANGES
        #   blockers:
        #     - file: x.cs
        #       reason: "a real blocker"
        #   meta:
        #     agent: reviewer
        #     verdict: PASS
        #     blockers: []
        # Before the guard: the nested meta.agent/meta.verdict silently
        # overwrote the real top-level ones, so this archived as
        # `VALID|reviewer|PASS|...` and forged PASS into
        # .last-review-verdict -- bypassing ALLOWED_AGENTS (the drop was
        # never authored by reviewer) and the verdict<->blockers
        # consistency check below (only satisfied because the nested
        # `blockers: []` also overwrote the real, non-empty blockers list).
        # After the guard: `agent`/`verdict` can no longer be overwritten by
        # a nested occurrence, so the drop is evaluated under its true
        # declared TOP-LEVEL identity (csharp-dev / NEEDS-CHANGES) -- this
        # specific PARSING-DIFFERENTIAL impersonation route is closed.
        # Do NOT read that as "impersonation is closed" in general: nothing
        # here or anywhere else in this file authenticates the `agent:`
        # field against who actually ran the SubagentStop hook. A drop that
        # honestly, non-nestedly declares `agent: reviewer` / `verdict:
        # PASS` / `blockers: []` at column 0 is accepted at face value --
        # any agent (or a human editing the inbox by hand) can write that
        # file and have it archived and cached as a real reviewer PASS. The
        # SubagentStop payload this hook reads (see the top of the file)
        # carries only `stop_hook_active` and `cwd`, no authorship claim, so
        # there is no signal available to this hook that could close that
        # gap. See "Known limitation: no authorship check" near the top of
        # this file and decision-schema.md's Statusline integration section
        # -- round 4 re-review (review-b93b430.md, finding 3) flagged that a
        # prior version of THIS comment over-read as "the impersonation is
        # closed" full stop, which is wrong; say the narrower true thing
        # here so a future pass doesn't have to re-derive it.
        # `blockers` is guarded too (see below), so
        # the nested `blockers: []` no longer shadows the real list either:
        # the drop above now ARCHIVES under its true identity as
        # `[csharp-dev - NEEDS-CHANGES]`, with the whole nested `meta:`
        # block ignored. That is what fixtures/19 asserts.
        #
        # GUARDED_TOPLEVEL_FIELDS covers six names: the five schema-required
        # ones plus `blockers`. `blockers` is in the set despite not being
        # schema-required because the verdict<->blockers consistency check
        # below is a gate in its own right, and a nested `blockers: []` that
        # shadows a real, non-empty list defeats it (re-review round 2,
        # exploit 2). A gate that another key can silently disarm is not a
        # gate. fixtures/26-blockers-nested-shadow.md pins this.
        #
        # Indentation is measured with a bare `.lstrip()`, NOT `.lstrip(" ")`.
        # The round-1 guard counted ASCII spaces only, so a tab, NBSP,
        # ideographic space, vertical tab, form feed or em space measured as
        # indent 0, skipped the guard entirely, and still `.strip()`ed down
        # to a clean key -- six working bypasses (round 2, exploit 1).
        # fixtures/20 through 25 pin one per character. Any future change
        # here must keep measuring ALL whitespace.
        #
        # What remains open (the OTHER, larger half of #8, deliberately NOT
        # touched by this pass -- see
        # https://github.com/MCGPPeters/squad-template/issues/8 for the
        # scope question raised to the user, and do not silently widen this
        # without that decision): the guard covers only the names in
        # GUARDED_TOPLEVEL_FIELDS. Any other key nested the same way can
        # still be silently promoted to top level, or shadow a
        # previously-collected value. Nothing currently gates on such a key,
        # which is why this is bounded rather than urgent -- but adding a
        # new schema field or a new consistency check WITHOUT adding its key
        # here re-opens exactly the bypass above. Broadening the guard to
        # every key, or reworking list-item acceptance more generally, is
        # the scope question in #8.
        if indent > 0 and k in GUARDED_TOPLEVEL_FIELDS:
            continue

        if v == "":
            current_list = k
            fields.setdefault(k, [])
        else:
            current_list = None
            fields[k] = _unquote(v)
    else:
        # Unrecognised continuation — ignore.
        pass

required = ["id","agent","verdict","scope","created"]
missing = [r for r in required if r not in fields or fields[r] in (None,"",[])]
if missing:
    print(f"missing required fields: {','.join(missing)}")
    sys.exit(1)

# A required field written as a YAML block LIST (`agent:\n  - reviewer`)
# parses through the list-item branch above as a python `list`, not the
# plain scalar every downstream check assumes. Round 5 re-review
# (review-pr355-round4.md, nitpick): `agent not in ALLOWED_AGENTS` on a
# `list` value raises `TypeError: cannot use 'list' as a set element`
# (lists aren't hashable), and that RAW TRACEBACK was written verbatim
# into the .reason file -- correct outcome (still quarantined), unhelpful
# message. Coercing to a rejection reason here, before any set-membership
# check runs, keeps the outcome identical and makes the reason readable.
non_scalar = [r for r in required if not isinstance(fields[r], str)]
if non_scalar:
    print(f"field(s) must be a plain scalar value, not a list: {','.join(non_scalar)}")
    sys.exit(1)

agent = fields["agent"]
verdict = fields["verdict"]
scope = fields["scope"]

if agent not in ALLOWED_AGENTS:
    print(f"unknown agent: {agent}")
    sys.exit(1)
if verdict not in ALLOWED_VERDICTS:
    print(f"invalid verdict: {verdict}")
    sys.exit(1)
if scope not in ALLOWED_SCOPES:
    print(f"invalid scope: {scope}")
    sys.exit(1)

# Verdict ↔ blockers consistency.
blockers = fields.get("blockers", [])
if isinstance(blockers, str):
    # Inline form like `blockers: []`.
    blockers = [] if blockers.strip() in ("[]","") else [blockers]
has_blockers = bool(blockers)

if verdict == "PASS" and has_blockers:
    print("verdict PASS but blockers list is non-empty")
    sys.exit(1)
if verdict in ("NEEDS-CHANGES","BLOCKED") and not has_blockers:
    print(f"verdict {verdict} but blockers list is empty")
    sys.exit(1)

# `created` is appended as a sixth field so the bash dispatch loop below
# can use the SAME value the validator itself extracted, rather than
# re-deriving it with a separate `grep -E '^created:' | head -n1` (carried
# medium finding, rounds 2-4: that grep takes the first COLUMN-0 match,
# while this parser takes the LAST top-level write for a repeated key --
# divergent tie-breaking if a drop ever carried two top-level `created:`
# lines). Removes the divergence by construction: there is only one
# extraction now.
print(f"VALID|{agent}|{verdict}|{scope}|{fields['id']}|{fields['created']}")
sys.exit(0)
PY
}

last_reviewer_verdict=""
last_reviewer_created=""
appended_count=0

tmp_append="$(mktemp)"
trap 'rm -f "$tmp_append"' EXIT

for f in "${files[@]}"; do
  result="$(validate "$f" 2>&1)"
  status=$?

  if [ "$status" -ne 0 ]; then
    # Quarantine.
    ts="$(date -u +%Y-%m-%dT%H-%M-%S)"
    base="$(basename "$f")"
    mv "$f" "$quarantine/${ts}-${base}" 2>/dev/null || true
    printf 'reason: %s\nquarantined_at: %sZ\n' "$result" "$(date -u +%Y-%m-%dT%H:%M:%S)" > "$quarantine/${ts}-${base}.reason"
    continue
  fi

  if [ "$result" = "LEGACY" ]; then
    {
      printf '\n<!-- legacy -->\n### %s — %s\n\n' "$today" "$(basename "$f" .md)"
      cat "$f"
      printf '\n'
    } >> "$tmp_append"
  else
    # VALID|agent|verdict|scope|id|created
    IFS='|' read -r _tag agent verdict scope drop_id created <<<"$result"
    {
      printf '\n### %s — %s [%s · %s]\n\n' "$today" "$drop_id" "$agent" "$verdict"
      cat "$f"
      printf '\n'
    } >> "$tmp_append"

    if [ "$agent" = "reviewer" ] && [ "$scope" = "review" ]; then
      # `created` comes straight from the validator's own field extraction
      # above -- no separate re-grep of "$f", so there is exactly one
      # source of truth for it instead of two that could disagree.
      if [ -z "$last_reviewer_created" ] || [[ "$created" > "$last_reviewer_created" ]]; then
        last_reviewer_verdict="$verdict"
        last_reviewer_created="$created"
      fi
    fi
  fi

  appended_count=$((appended_count + 1))
  ts="$(date -u +%Y-%m-%dT%H-%M-%S)"
  mv "$f" "$archive_dir/${ts}-$(basename "$f")" 2>/dev/null || true
done

if [ "$appended_count" -gt 0 ]; then
  cat "$tmp_append" >> "$decisions_doc"
fi

# Update the verdict cache for the statusline. Only overwrite if we saw a
# reviewer verdict this round; otherwise leave the previous value alone.
if [ -n "$last_reviewer_verdict" ]; then
  printf '%s\n' "$last_reviewer_verdict" > "$verdict_cache"
fi

exit 0
