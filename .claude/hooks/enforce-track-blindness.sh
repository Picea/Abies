#!/usr/bin/env bash
#
# PreToolUse hook for the read tools: Read | Grep | Glob | NotebookRead, plus
# every MCP tool (`mcp__*`) — an MCP tool that reads files is a read, and a
# deny list scoped to the built-in read tools alone would have a hole the
# moment a charter gains one.
#
# Enforces the Dreamer phase's mutual blindness, keyed on `agent_type` from
# the hook payload, which Claude Code populates when a hook fires inside a
# subagent:
#
#   dreamer-first-principles  — denied the project's recorded conclusions
#                               (register, archive, tech stack, agent
#                               memories, decision drops), the conductor's
#                               knowledge scan, Track B's artifact, the
#                               pattern lexicon — which is, literally, a
#                               list of pattern names — gate 1's scan and
#                               report, and another agent's worktree.
#   dreamer-informed          — denied Track A's artifact and another
#                               agent's worktree. The blindness is claimed
#                               as mutual; enforcing one side and promising
#                               the other makes the claim false in a way
#                               that is hard to see.
#
# Any other agent — including the main session — passes through untouched.
#
# THE RULE, IN ONE SENTENCE: for a governed agent, refuse any read whose
# effective scope is at or above a denied path. That covers what would
# otherwise look like three separate rules:
#   - exact:    Read/Grep/Glob targeting the denied path itself.
#   - ancestor: Grep/Glob rooted at a real directory that contains it, e.g.
#               `path: ".claude"` reaches `.claude/docs/decisions.md` even
#               though ".claude" is not itself on the deny list.
#   - no path:  Grep/Glob take an optional `path` that defaults to this
#               entire cwd when omitted -- cwd is the ultimate ancestor of
#               every path in the repo, so "no path" is the
#               root-of-everything instance of "ancestor", not a separate
#               case.
# A deny list that matches paths cannot refuse an invocation naming none, or
# naming only a coarser directory than the entries on the list -- so all of
# the above are folded into one containment test (`reach_hit`, below)
# instead of an exact-match test plus separate pre-checks.
#
# AGAINST aa743c4 (the last commit before this fix).
#
# What base already refused: an exact denied file (`Read`, or a `path`/leaf
# naming it exactly), and a denied `/**` directory named exactly. Base
# tested every string leaf's resolved path, plus every trailing segment run
# of it, against each deny pattern with an *anchored, exact* regex -- so a
# denied file's real relative form was still caught through a worktree
# copy (the trailing-run trick finds it at whatever depth it recurs), but
# nothing coarser than the pattern itself ever matched: `path=".claude"`,
# `path="."`, and no `path` at all were all allowed, because none of them
# is a byte-for-byte match of any listed pattern.
#
# What this change adds: containment (`reach_hit`/`reaches`, below) so an
# *ancestor* of a denied path also refuses, not just an exact match; root
# resolution (`resolve`/`classify`) so `..`, `~` and an absolute path above
# cwd are recognised as "at or above" rather than compared as literal
# strings; tool-shape-aware scope so `Grep`'s `pattern` (content) and
# `Glob`'s `pattern` (always scope-bearing, `path` present or not) are no
# longer both walked as if either could be a path; fail-closed parsing
# (below); and a restored
# trailing-segment-run fallback in `reach_hit`'s `under`/`above` branch,
# which is what actually keeps a worktree copy's exact/descendant form
# caught under the new containment rule (see that function).
#
# What remains open, deliberately: the sibling worktree layout
# `git-advanced/SKILL.md` also documents -- a second checkout that is a true
# sibling directory rather than nested under `.claude/worktrees/` -- resolves
# to the `elsewhere` branch below and only ever gets the exact/descendant
# trailing-run treatment, not an ancestor one; there is no fixed path segment
# to put on a deny list for a location that is, by that layout's own design,
# outside this checkout entirely. Also open: symlinks (would need
# `os.path.realpath`). `{a,b}`-style brace expansion in a `Glob` pattern is
# NOT open -- `glob_root()` returns `("grouped", None)` for it and the
# caller escalates to `cwd` (D20, fail-closed), so a brace-grouped pattern
# cannot reach a denied path; the measured cost is the opposite of a gap, a
# benign `{a,b}/*.md` is refused too. See docs/security/threat-model.md,
# TM-015, which marks this ✅ Mitigated. The sibling-worktree layout above
# and symlinks remain a deny-closure completeness gap -- not an existing
# INV-3 instance; INV-3 is cited in an upstream-template design pass as
# `04-realist-plan.md:2054`, which has no corresponding artifact in this
# repository (`git ls-tree -r --name-only HEAD -- .squad/design` returns
# only `undo-redo/00-scope-undo-redo.md`; if that pass is ever imported here,
# retarget this citation at the real path). INV-3 closes over `WRITER`
# edges, and a worktree copy has no writer edge, so that derivation would
# not reach this even if run. Tracked for the design pass's own planning,
# not solved here.
#
# CLOSED (PR #358 review round 2, finding 🔴-B): the *ancestor* direction
# through a worktree NESTED under this checkout -- rooting a search AT
# `.claude/worktrees` itself, or at the agent directory one level below it,
# rather than at one of its descendants -- used to reach nothing, the same
# as it reached nothing at base, even after the descendant-direction fix
# above. Verified by execution against a real registered worktree:
# `Grep(path=".claude/worktrees")` and
# `Glob(".claude/worktrees/**/decisions.md")` both exited 0 for
# `dreamer-first-principles`, reaching every path this hook denies. Closed
# below by an explicit `(".claude/worktrees/**", ...)` DENY entry for both
# agents this hook governs -- neither Track A nor Track B has a legitimate
# reason to read another agent's checkout, including each other's, and a
# worktree copy can carry a real copy of anything on either deny list.
# Accepted collateral cost of that entry: a read rooted exactly AT `.claude`
# (not a descendant of it) is now refused for `dreamer-informed` too, which
# was not refused there for any reason before this fix -- `.claude` is a
# real ancestor of `.claude/worktrees/**` the same way it is already a real
# ancestor of `.claude/docs/decisions.md` for `dreamer-first-principles`.
# `dreamer-informed` can still search any subdirectory of `.claude` other
# than `.claude/worktrees` itself (e.g. `.claude/skills`) without being
# refused; only the exact coarse root `.claude` (and its own ancestors) picks
# up this new refusal.
#
# Two things containment must get right or it creates new holes of its own:
#
#   1. THE ROOT MUST BE RESOLVED, NOT COMPARED AS A LITERAL STRING -- AND
#      A DESCENDANT'S TRUE POSITION IS NOT THE ONLY THING THAT REFUSES IT.
#      `..`, `~`, and an absolute path above the repo must all be
#      recognised as "at or above cwd" (which reaches everything).
#      `resolve`/`classify` below do this: they join against `cwd`, expand
#      `~`, normalise `..`, and only then decide whether the result is
#      at-or-above cwd, a real descendant of it, or a foreign path
#      elsewhere entirely. `above` already reaches everything on the direct
#      check alone (its segment list collapses to empty, and an empty list
#      is vacuously a prefix of every pattern). A descendant of cwd
#      (`under`) is tried first at its true relative position -- but if
#      that finds nothing, `under` and `elsewhere` both also try every
#      trailing segment run of it. That fallback cannot be described as
#      "still judged by true position": whether a directory is a second
#      copy of the repo (a worktree nested under cwd, or a foreign checkout
#      resolved elsewhere) recreating a denied pattern at a deeper offset
#      is not knowable from the path alone, and is not distinguishable, by
#      path alone, from a directory that merely happens to be *named* like
#      one. The fallback is an existential quantifier over unknown roots --
#      "does *any* suffix of this path coincide with a denied pattern" --
#      because a second copy's true relative position cannot be computed
#      without already knowing which directories are second copies. It
#      over-approximates on purpose, in the fail-closed direction, and that
#      has a measured, accepted cost: `tests/fixtures/.claude` and
#      `docs/examples/.squad` -- ordinary directories in a template repo's
#      consumer tree, not nested checkouts of anything -- are refused for
#      Track A the same way a real worktree copy is. Pinned below so the
#      cost stays visible rather than being rediscovered as a bug report.
#
#   2. GREP'S `pattern` IS CONTENT, NOT A PATH, AND GLOB'S `pattern` IS
#      ALWAYS SCOPE, `path` PRESENT OR NOT. `Grep`'s only scope-bearing
#      argument is `path` (defaulting to cwd); its `pattern` is a regex
#      matched against file contents and must never be tested as if it
#      were a path -- `Grep(pattern=".claude", path="src")` is a
#      legitimate search for the literal text ".claude" inside src/ and
#      must be allowed. `Glob` is the mirror image: its `pattern` is
#      *always* scope-bearing, so the effective root composes `path` with
#      `glob_root(pattern)` rather than using `path` alone whenever it is
#      given -- `Glob(pattern="src/**/*.cs")` with no `path` is scoped to
#      `src/`, and `Glob(pattern="../denied/**", path="docs")` composes to
#      a root above `docs`, not to `docs` unchanged. Each tool's scope is
#      therefore computed by its own rule below, not by walking every
#      string in tool_input and hoping. (This fix was referenced by the
#      internal shorthand "T-012" before a real row existed for it.
#      `docs/security/threat-model.md`, TM-015, Trust Boundary 6 --
#      "Dreamer/reviewer blindness boundary" -- is now that row; its
#      Mitigation column names this composition, and its Test column cites
#      `.claude/hooks/tests/blindness.sh`'s `[T-012]` cases below by that
#      label, which is why the label stays `[T-012]` rather than being
#      renamed. PR #358 review round 2, ⚠️-C registered the citation gap;
#      security-expert closed it with TM-015 the same round.)
#
# `mcp__*` tools have no fixed schema, so neither of the above is knowable
# in general. Rather than fail closed on a call with no path-shaped
# argument at all (refusing e.g. a hypothetical `mcp__learn__search
# {"question": ...}` and telling it to add a `path` it has no way to
# supply), this hook keeps the pre-existing, more conservative behaviour for
# `mcp__*`: every string leaf in the payload is tested as a candidate path
# (same as Read/NotebookRead), but an `mcp__*` call is never refused merely
# for lacking one. No governed agent’s charter grants an mcp__* tool today
# (grep the `tools:` frontmatter), so whichever way this is decided is
# latent here and live only in a consumer's tree; this is the deliberate
# choice, not a fallout of leaving the case unhandled.
#
# A NAMED FALSE-POSITIVE CLASS: free-text mcp__* parameters that merely
# CONTAIN a denied-looking trailing segment. Because every string leaf is
# walked as a candidate path (above), and that candidate then gets the same
# trailing-segment-run fallback principle 1 accepts for real paths, a leaf
# that was never a path at all -- `mcp__learn__search`'s free-text
# `question`, for example -- refuses the whole call the moment any suffix
# of it happens to read like an ancestor of something denied:
# `{"question": "check foo/.claude/docs"}` and `{"question": "see
# src/.claude"}` both refuse, in full, over a token that is not a path and
# never claimed to be one. Deliberate, not gated per-tool: the hook cannot
# tell a free-text parameter from a real path by shape alone once both are
# just strings in `tool_input`, so the same fail-closed over-approximation
# applies to both. Latent today -- no governed agent’s charter grants an
# `mcp__*` tool (grep the `tools:` frontmatter) -- so the assertion below
# is the documentation of what happens the day one does, not a desired
# behaviour.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}. Hook paths do not follow a worktree: CLAUDE_PROJECT_DIR
# stays at the project root where the session started while cwd is the
# worktree root, so a hook rooted at CLAUDE_PROJECT_DIR silently stops
# matching inside a worktree — and still passes any test that has no
# worktree in it.
#
# A PAYLOAD THAT CANNOT BE PARSED REFUSES, FOR EVERY AGENT, NOT JUST THE
# GOVERNED ONES. This hook fires on every tool call from every agent and
# only governs a few of them by `agent_type`; a payload that fails to parse
# as JSON carries no reliable `agent_type` at all, so there is no safe way
# to tell whether the call belongs to a governed agent or not. Allowing by
# default in that case (as an earlier revision did, via a bare `except
# Exception: sys.exit(0)`) means a malformed payload silently defeats the
# blindness rule for whichever agent happened to trigger it. Refusing
# universally is the deliberate trade: a malformed payload is a
# malfunction, not a workflow, so a call that is loudly refused for
# everyone is preferable to a leak that is silently allowed for one.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "dreamer-first-principles",
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Read",
#     "tool_input": { "file_path": "..." }
#   }

set -euo pipefail

HOOKS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export HOOKS_LIB_DIR="$HOOKS_DIR/lib"

# Fail closed, not open, when the interpreter itself is unavailable. Every
# python3 invocation below is wrapped in a top-level try/except that refuses
# on a malformed PAYLOAD -- but an interpreter that is missing, the wrong
# version, or broken by a bad edit never reaches that except at all, and the
# previous `2>/dev/null || true` on the substitution turned that failure into
# an EMPTY $reason, indistinguishable from "nothing to report" (allow). That
# is the same silent-leak shape the payload-parsing fail-closed argument
# above exists to prevent, one level up.
if ! command -v python3 >/dev/null 2>&1; then
  cat >&2 <<'EOF'
🚫 python3 is not available on PATH.

This hook enforces the Dreamer tracks' mutual blindness using an embedded
Python containment check. Without python3 there is no way to evaluate a read
against the deny list, so refusing is the only choice that does not silently
defeat the rule this hook exists to enforce -- the same fail-closed posture
this hook already takes for a malformed JSON payload.

Install python3 (or add it to PATH) and retry.
EOF
  exit 2
fi

payload="$(cat)"

# `set -e` alone does not get this to exit 2: under `-euo pipefail`, a
# failing substitution aborts the SCRIPT immediately with WHATEVER exit
# code the failing command returned (127 for "command not found" inside a
# broken interpreter stub, for instance) -- and Claude Code only treats
# exit 2 from a PreToolUse hook as "block"; any other nonzero code is a
# non-blocking error that lets the tool call proceed, which is fail-OPEN.
# `-e` is suspended for exactly this one substitution so the exit code can
# be inspected and converted to a real, deliberate `exit 2` below, instead
# of leaking whatever raw code the interpreter happened to return.
set +e
reason="$(printf '%s' "$payload" | python3 -c '
import json, os, sys

sys.path.insert(0, os.environ["HOOKS_LIB_DIR"])
from path_containment import reach_hit, glob_root, leaves, truthy_str

RS = "\x1e"  # python->bash field separator; avoids collision with pipes in
             # Grep patterns or Glob globs, unlike the "|" used before.

DENY = {
    "dreamer-first-principles": [
        (".claude/docs/decisions.md",        "the decision register"),
        (".claude/docs/decisions-archive/**", "the archived decision register"),
        (".claude/docs/tech-stack.md",       "the tech stack record"),
        (".claude/docs/pattern-lexicon.md",  "the pattern lexicon"),
        (".claude/agent-memory/**",          "another agent’s persistent memory"),
        (".claude/worktrees/**",             "another agent’s worktree checkout"),
        (".squad/decisions/**",              "the decision drops"),
        (".squad/design/*/00-knowledge.md",  "the conductor’s knowledge scan"),
        (".squad/design/*/02-track-b.md",    "Track B’s artifact"),
        (".squad/design/*/00-warden-scan.md", "gate 1’s mechanical scan (quotes lexicon terms verbatim)"),
        (".squad/design/*/00-warden.md",     "gate 1’s report"),
    ],
    "dreamer-informed": [
        (".squad/design/*/01-track-a.md",    "Track A’s artifact"),
        (".claude/worktrees/**",             "another agent’s worktree checkout"),
    ],
}

WHY = {
    "dreamer-first-principles":
        "Track A derives from the problem’s structure. The project’s recorded "
        "conclusions, the conductor’s knowledge scan and Track B’s artifact are "
        "prior art, and prior art is the one thing this phase is built to be "
        "blind to. Read 00-scope.md and the codebase.",
    "dreamer-informed":
        "Track A and Track B are mutually blind. Reading the other track "
        "destroys the independence that makes dreamer-convergence meaningful.",
}

# Everything needed to even ask "which agent is this" lives inside this one
# try: a payload that cannot be parsed, or parses to something that is not
# the shaped object this hook expects, cannot be attributed to any agent --
# governed or not -- so it is treated as unreadable and refused for
# everyone (see the header). This is deliberately not narrowed to just
# json.loads: a payload that parses but is not a dict (e.g. a bare JSON
# array or number) would otherwise raise AttributeError on the first
# `.get()` below, which is exactly the same "cannot attribute this" case.
try:
    d = json.loads(sys.stdin.read())
    if not isinstance(d, dict):
        raise ValueError("payload is not a JSON object")
    agent = (d.get("agent_type") or "").strip()
    tool = (d.get("tool_name") or "").strip()
    cwd = os.path.normpath(d.get("cwd") or os.getcwd())
    tool_input = d.get("tool_input") or {}
    if not isinstance(tool_input, dict):
        raise ValueError("tool_input is not a JSON object")
except Exception:
    print("UNREADABLE")
    sys.exit(0)

rules = DENY.get(agent)
if not rules:
    sys.exit(0)

if tool not in ("Read", "Grep", "Glob", "NotebookRead") and not tool.startswith("mcp__"):
    sys.exit(0)

# leaves/reach_hit/glob_root/truthy_str: see path_containment.py, imported
# above. Shared with enforce-review-blindness.sh -- see that module’s header
# for why (review round 1 of PR #358, ⚠️-7).

hit = None          # (pattern, label)
target = None       # what to show as "Target:" in the refusal
is_search_tool = False

if tool == "Grep":
    is_search_tool = True
    p = tool_input.get("path")
    if truthy_str(p):
        target = p
        hit = reach_hit(p, cwd, rules)
    else:
        target = None
        hit = reach_hit(".", cwd, rules)
elif tool == "Glob":
    is_search_tool = True
    # Glob’s effective root is the composition of `path` and `pattern`’s own
    # derived root, ALWAYS -- unlike Grep, `pattern` is scope-bearing
    # whether or not `path` is also given, so `path` alone (correct for
    # Grep, wrong for Glob) must never be the whole story here. See the
    # header: docs/security/threat-model.md, TM-015, Trust Boundary 6.
    p = tool_input.get("path")
    pattern = tool_input.get("pattern")
    g_reason, pattern_root = glob_root(pattern)
    if g_reason != "ok":
        # A pattern glob_root cannot classify (a brace group anywhere, or a
        # ".." after the first wildcard -- fix 2 / D20) is refused on its
        # shape alone, before any denied-path check runs -- this is
        # deliberately its OWN reporting path, never the shared denied-path
        # `hit` path below. Composing an unclassifiable root via a plain
        # join would silently discard that the pattern’s true root is
        # unknown, not "nothing to add"; reporting it through the denied-
        # path path would instead name an arbitrary denied label this call
        # never touched, which is a false accusation, not just a vague
        # message -- security requires the two refusal classes to read as
        # visibly distinct, not just technically distinct. `hit` is
        # deliberately left unset (None) so the shared print at the bottom
        # of this script stays silent for this branch.
        print(RS.join(["UNSAFE_GLOB", agent, tool, g_reason, pattern if isinstance(pattern, str) else ""]))
    elif truthy_str(p):
        composed = os.path.join(p, pattern_root)
        target = "%r composed with pattern %r: effective root %r" % (p, pattern, composed)
        hit = reach_hit(composed, cwd, rules)
    else:
        target = "%r (no path given -- scope derived from pattern: root %r)" % (pattern_root, pattern)
        hit = reach_hit(pattern_root, cwd, rules)
else:
    # Read, NotebookRead, mcp__* -- every string leaf is a candidate path.
    for raw in leaves(tool_input):
        if not raw or raw.startswith("-"):
            continue
        h = reach_hit(raw, cwd, rules)
        if h:
            target = raw
            hit = h
            break

if hit:
    pat, label = hit
    shown_target = target if target is not None else "(none given -- defaults to this entire checkout)"
    scope_flag = "SCOPE" if is_search_tool else "-"
    print(RS.join([agent, tool, shown_target, label, WHY[agent], scope_flag]))
' 2>/dev/null)"
py_rc=$?
set -e

if [ "$py_rc" -ne 0 ]; then
  cat >&2 <<EOF
🚫 this hook's embedded Python containment check exited with an unexpected
error (exit $py_rc) instead of a clean allow or a reported denial.

This hook enforces the Dreamer tracks' mutual blindness; an internal crash
is not the same thing as "nothing to report" and must not be treated as an
allow. Refusing is the only choice that does not silently defeat the rule
this hook exists to enforce.

Check python3's version and the hook's own syntax -- this is a bug in the
hook, not in the tool call it was evaluating.
EOF
  exit 2
fi

if [ "$reason" = "UNREADABLE" ]; then
  cat >&2 <<'EOF'
🚫 could not parse this tool call's payload as JSON.

This hook enforces read boundaries for specific agents, keyed on fields
inside the payload it is given on stdin. A payload that does not parse (or
that parses to something other than the object this hook expects) carries
no reliable agent identity, so there is no safe way to tell whether this
call belongs to a governed agent or not -- refusing for every agent is the
only choice that cannot silently let a governed agent's blindness be
defeated. A malformed payload is a malfunction, not a workflow: a call that
is loudly refused for everyone gets noticed; a leak that is silently
allowed for one agent would not be.

If you did not expect this, the payload feeding this hook likely needs
attention -- a well-formed tool call should not trigger it.
EOF
  exit 2
fi

case "$reason" in
  UNSAFE_GLOB$'\x1e'*)
    IFS=$'\x1e' read -r _ agent tool glob_reason glob_pattern <<<"$reason"
    case "$glob_reason" in
      grouped)
        body="This Glob pattern contains a brace group (\`{...}\`), so its literal root can't be determined and it is refused on that shape alone — no denied path was matched or reached by this call. Split it into separate Glob calls with the braces removed, one per alternative (e.g. \`{src,docs}/**\` → two calls, \`src/**\` and \`docs/**\`), and each will be evaluated on its own."
        ;;
      climb)
        body="This Glob pattern contains \`..\` after a wildcard segment (\`*\`, \`?\`, or \`[...]\`), so where it resolves can't be determined from the pattern text alone and it is refused on that shape alone — no denied path was matched or reached by this call. Remove the wildcard segment before the \`..\`, or replace the climb with an explicit \`path\` naming exactly where you need to look, and it will be evaluated normally."
        ;;
      *)
        body="This Glob call has no usable \`pattern\`, so its scope can't be determined and it is refused on that shape alone — no denied path was matched or reached by this call. Supply a \`pattern\`."
        ;;
    esac
    cat >&2 <<EOF
🚫 ${agent}'s Glob pattern is refused: its shape can't be classified.

  Tool:    ${tool}
  Pattern: ${glob_pattern}

${body}

See .claude/agents/${agent}.md (Hard Prohibitions).
EOF
    exit 2
    ;;
esac

[ -z "$reason" ] && exit 0

IFS=$'\x1e' read -r agent tool target label why scope_flag <<<"$reason"

scope_note=""
if [ "$scope_flag" = "SCOPE" ]; then
  scope_note="
${tool} searches everything under the path you give it — the whole checkout,
if you give none — so a search whose scope reaches at or above a denied path
is refused the same as reading that path directly. Scope your search to a path below the denied paths
(for example \`path: \"src/\"\`) or to the specific file you need;
this refuses the reach, not the searching.
"
fi

cat >&2 <<EOF
🚫 ${agent} may not read ${label}.

  Tool:   ${tool}
  Target: ${target}

${why}
${scope_note}
This is a structural rule of the design pass, not a preference. If you believe
the scope is missing a constraint you need, say so in your artifact and stop —
the orchestrator surfaces it at the next 🛑. Do not route around this.

See .claude/agents/${agent}.md (Hard Prohibitions) and
.claude/skills/beast-mode-design/SKILL.md (Phase Ownership and the Artifact Contract).
EOF
exit 2
