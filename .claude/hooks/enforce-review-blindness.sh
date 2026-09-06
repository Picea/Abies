#!/usr/bin/env bash
#
# PreToolUse hook for the read tools: Read | Grep | Glob | NotebookRead, plus
# every MCP tool (`mcp__*`) — an MCP tool that reads files is a read.
#
# Denies `reviewer-blind` any access to `.squad/design/`. The blind reviewer
# forms its own reading of what the code does and writes 08-review-blind.md
# before any narrative is read; because that file is on disk first, the
# independent assessment can no longer be softened retroactively. Reaching
# the plan, the critique or the spec first is exactly the capture the split
# exists to prevent.
#
# Identity comes from `agent_type` in the payload, which Claude Code
# populates when a hook fires inside a subagent. Any other agent passes
# through untouched.
#
# THE RULE, IN ONE SENTENCE: refuse any read whose effective scope is at or
# above a denied path -- `.squad/design`, or another agent's worktree
# checkout. That covers what would otherwise look like three separate rules:
#   - exact:    Read/Grep/Glob targeting something inside .squad/design/.
#   - ancestor: Grep/Glob rooted at a real directory that contains it, e.g.
#               `path: ".squad"` reaches .squad/design even though ".squad"
#               is not itself the denied root.
#   - no path:  Grep/Glob take an optional `path` that defaults to this
#               entire cwd when omitted -- cwd is the ultimate ancestor of
#               .squad/design, so "no path" is the root-of-everything
#               instance of "ancestor", not a separate case.
# A deny root that matches paths cannot refuse an invocation naming none, or
# naming only a coarser directory than the root itself -- so all of the
# above are folded into one containment test (`reach_hit`, below) instead
# of an exact-prefix test plus separate pre-checks.
#
# AGAINST aa743c4 (the last commit before this fix).
#
# What base already refused: an exact denied path inside .squad/design/
# (`Read`, or a `path`/leaf naming it exactly), tested with an *anchored,
# exact* regex against `.squad/design` itself and its `/**` descendants,
# over every string leaf plus every trailing segment run of its resolved
# path -- so a denied file's real relative form was still caught through a
# worktree copy (the trailing-run trick finds it at whatever depth it
# recurs), but nothing coarser than `.squad/design` itself ever matched:
# `path=".squad"`, `path="."`, and no `path` at all were all allowed.
#
# What this change adds: containment (`reach_hit`/`reaches`, below) so an
# *ancestor* of .squad/design/ also refuses, not just an exact match; root
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
# `Grep(path=".claude/worktrees")` and `Glob(".claude/worktrees/**/*.md")`
# both exited 0 for `reviewer-blind`, reaching `.squad/design/**` through
# it. Closed below by an explicit `(".claude/worktrees/**", ...)` entry in
# RULES -- `reviewer-blind` has no legitimate reason to read another
# agent's checkout; it reviews the diff and the tree it was invoked
# against, not a different agent's isolated worktree. Accepted collateral
# cost of that entry: reading a builder's source through a copy of it
# nested inside `.claude/worktrees/<agent>/` is now refused too, where it
# previously was not -- `reviewer-blind` reviews the same source from its
# own cwd instead.
#
# Two things containment must get right or it creates new holes of its own
# (see enforce-track-blindness.sh for the fuller version of this argument;
# the same fix applies here since both hooks share the shape):
#
#   1. The root must be resolved (join against cwd, expand ~, normalise ..)
#      before deciding containment -- never compared as a literal string.
#      `above` reaches everything on the direct check alone. A descendant
#      of cwd (`under`) is tried first at its true relative position, but
#      if that finds nothing, `under` and `elsewhere` both also try every
#      trailing segment run of it -- and that fallback is not "still judged
#      by true position": it is an existential quantifier over unknown
#      roots ("does *any* suffix of this path coincide with .squad/design/
#      or its descendants"), because a second copy's true relative position
#      cannot be computed without already knowing which directories are
#      second copies. It over-approximates on purpose, in the fail-closed
#      direction. Measured, accepted cost: `docs/examples/.squad` -- an
#      ordinary directory in a template repo's consumer tree, not a nested
#      checkout of anything -- is refused for `reviewer-blind` the same way
#      a real worktree copy of `.squad/design/` is. Pinned below so the
#      cost stays visible rather than being rediscovered as a bug report.
#   2. Grep's `pattern` is content, not a path, and must never be tested as
#      one; Grep's only scope-bearing argument is `path`. Glob is the
#      mirror image: its `pattern` is always scope-bearing, so the
#      effective root composes `path` with `glob_root(pattern)` rather
#      than using `path` alone whenever it is given. (This fix was
#      referenced by the internal shorthand "T-012" before a real row
#      existed for it. `docs/security/threat-model.md`, TM-015, Trust
#      Boundary 6 -- "Dreamer/reviewer blindness boundary" -- is now that
#      row; its Test column cites `.claude/hooks/tests/blindness.sh`'s
#      `[T-012]` cases by that label, which is why the label stays
#      `[T-012]` rather than being renamed. PR #358 review round 2, ⚠️-C
#      registered the citation gap; security-expert closed it with TM-015
#      the same round. See enforce-track-blindness.sh for the fuller note.)
#
# `mcp__*` tools have no fixed schema, so neither is knowable in general.
# This hook keeps the same, more conservative choice as
# enforce-track-blindness.sh: every string leaf is tested as a candidate
# path (same as Read/NotebookRead), but an `mcp__*` call is never refused
# merely for lacking a path-shaped argument -- `reviewer-blind` holds no
# mcp__* tool today (grep its `tools:` frontmatter), so this is deliberate,
# not fallout.
#
# A NAMED FALSE-POSITIVE CLASS, the same one enforce-track-blindness.sh
# documents: free-text mcp__* parameters that merely contain a
# denied-looking trailing segment refuse the whole call, because the same
# leaf-as-candidate-path treatment and trailing-run fallback apply to text
# that was never a path. Deliberate, not gated per-tool, and latent today
# (`reviewer-blind` holds no `mcp__*` tool) -- see
# enforce-track-blindness.sh for the worked example.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR silently stops matching inside a worktree — and still
# passes any test that has no worktree in it.
#
# A PAYLOAD THAT CANNOT BE PARSED REFUSES, FOR EVERY AGENT, NOT JUST
# reviewer-blind. This hook fires on every tool call from every agent and
# only governs one of them by `agent_type`; a payload that fails to parse
# as JSON carries no reliable `agent_type` at all, so there is no safe way
# to tell whether the call is reviewer-blind's or not. Allowing by default
# in that case (as an earlier revision did, via a bare `except Exception:
# sys.exit(0)`) means a malformed payload silently defeats this hook
# whenever it happens to be reviewer-blind's call. Refusing universally is
# the deliberate trade: a malformed payload is a malfunction, not a
# workflow, so a call that is loudly refused for everyone is preferable to
# a leak that is silently allowed for the one agent this hook governs.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "reviewer-blind",
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

This hook enforces reviewer-blind's independence using an embedded Python
containment check. Without python3 there is no way to evaluate a read against
the denied path, so refusing is the only choice that does not silently defeat
the rule this hook exists to enforce -- the same fail-closed posture this hook
already takes for a malformed JSON payload.

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

RS = "\x1e"

AGENT = "reviewer-blind"
RULES = [
    (".squad/design/**",   ".squad/design/"),
    (".claude/worktrees/**", "another agent’s worktree checkout"),
]

# Everything needed to even ask "is this reviewer-blind" lives inside this
# one try: a payload that cannot be parsed, or parses to something that is
# not the shaped object this hook expects, cannot be attributed to any
# agent -- so it is treated as unreadable and refused for everyone (see the
# header). Deliberately not narrowed to just json.loads: a payload that
# parses but is not a dict would otherwise raise AttributeError on the
# first `.get()` below, which is exactly the same "cannot attribute this"
# case.
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

if agent != AGENT:
    sys.exit(0)

if tool not in ("Read", "Grep", "Glob", "NotebookRead") and not tool.startswith("mcp__"):
    sys.exit(0)

# leaves/reach_hit/glob_root/truthy_str: see path_containment.py, imported
# above. Shared with enforce-track-blindness.sh -- see that module’s header
# for why (review round 1 of PR #358, ⚠️-7).

hit = None
target = None
is_search_tool = False

if tool == "Grep":
    is_search_tool = True
    p = tool_input.get("path")
    if truthy_str(p):
        target = p
        hit = reach_hit(p, cwd, RULES)
    else:
        hit = reach_hit(".", cwd, RULES)
elif tool == "Glob":
    is_search_tool = True
    # Glob’s effective root is the composition of `path` and `pattern`’s own
    # derived root, ALWAYS -- see enforce-track-blindness.sh for the full
    # reasoning. docs/security/threat-model.md, TM-015, Trust Boundary 6.
    p = tool_input.get("path")
    pattern = tool_input.get("pattern")
    g_reason, pattern_root = glob_root(pattern)
    if g_reason != "ok":
        # Own reporting path -- never the shared denied-path `hit` path.
        # See enforce-track-blindness.sh for the full reasoning (security
        # requirement, not just DX: the two refusal classes must read as
        # visibly distinct, not just be technically distinct).
        print(RS.join(["UNSAFE_GLOB", agent, tool, g_reason, pattern if isinstance(pattern, str) else ""]))
    elif truthy_str(p):
        composed = os.path.join(p, pattern_root)
        target = "%r composed with pattern %r: effective root %r" % (p, pattern, composed)
        hit = reach_hit(composed, cwd, RULES)
    else:
        target = "%r (no path given -- scope derived from pattern: root %r)" % (pattern_root, pattern)
        hit = reach_hit(pattern_root, cwd, RULES)
else:
    for raw in leaves(tool_input):
        if not raw or raw.startswith("-"):
            continue
        h = reach_hit(raw, cwd, RULES)
        if h:
            target = raw
            hit = h
            break

if hit:
    pat, label = hit
    shown_target = target if target is not None else "(none given -- defaults to this entire checkout)"
    scope_flag = "SCOPE" if is_search_tool else "-"
    print(RS.join([tool, shown_target, label, scope_flag]))
' 2>/dev/null)"
py_rc=$?
set -e

if [ "$py_rc" -ne 0 ]; then
  cat >&2 <<EOF
🚫 this hook's embedded Python containment check exited with an unexpected
error (exit $py_rc) instead of a clean allow or a reported denial.

This hook enforces reviewer-blind's independence; an internal crash is not
the same thing as "nothing to report" and must not be treated as an allow.
Refusing is the only choice that does not silently defeat the rule this
hook exists to enforce.

Check python3's version and the hook's own syntax -- this is a bug in the
hook, not in the tool call it was evaluating.
EOF
  exit 2
fi

if [ "$reason" = "UNREADABLE" ]; then
  cat >&2 <<'EOF'
🚫 could not parse this tool call's payload as JSON.

This hook enforces read boundaries for reviewer-blind, keyed on fields
inside the payload it is given on stdin. A payload that does not parse (or
that parses to something other than the object this hook expects) carries
no reliable agent identity, so there is no safe way to tell whether this
call is reviewer-blind's or not -- refusing for every agent is the only
choice that cannot silently let reviewer-blind's independence be defeated.
A malformed payload is a malfunction, not a workflow: a call that is
loudly refused for everyone gets noticed; a leak that is silently allowed
would not be.

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

See .claude/agents/reviewer-blind.md.
EOF
    exit 2
    ;;
esac

[ -z "$reason" ] && exit 0

IFS=$'\x1e' read -r tool target label scope_flag <<<"$reason"

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
🚫 reviewer-blind may not read ${label}.

  Tool:   ${tool}
  Target: ${target}

Your review is independent because you cannot reach the author's narrative.
The plan, the critique, the spec and the handoff are all in .squad/design/;
reading any of them makes this a review of the narrative rather than of the
code.
${scope_note}
Read the diff, the full files it touches, their callers and siblings, and the
history through .claude/hooks/git-history-namestatus.sh. Write your assessment
to .squad/design/<slug>/08-review-blind.md — writing your own artifact is not
reading anyone else's — and stop. reviewer-reconcile reads everything else and
treats it as claims to verify.

See .claude/agents/reviewer-blind.md.
EOF
exit 2
