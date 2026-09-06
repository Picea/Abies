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
# above a denied path -- `.squad/design`. That covers what would otherwise
# look like three separate rules:
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
# What remains open, deliberately: the *ancestor* direction through a
# worktree copy -- e.g. `path=".claude/worktrees"` or the agent directory
# one level below it -- reaches nothing, the same as it reached nothing at
# base. Neither does the sibling worktree layout `git-advanced/SKILL.md`
# also documents, which resolves to the `elsewhere` branch below and only
# ever gets the same exact/descendant trailing-run treatment, not the
# ancestor one. Also open: symlinks (would need `os.path.realpath`) and
# `{a,b}`-style brace expansion in a `Glob` pattern. These are a
# deny-closure completeness gap -- not an existing INV-3 instance; INV-3
# (`04-realist-plan.md:2054`) closes over `WRITER` edges, and a worktree
# copy has no writer edge, so that derivation would not reach this even if
# run. Tracked for the design pass's own planning, not solved here.
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
#      mirror image: its `pattern` is always scope-bearing (T-012), so the
#      effective root composes `path` with `glob_root(pattern)` rather
#      than using `path` alone whenever it is given.
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

payload="$(cat)"

reason="$(printf '%s' "$payload" | python3 -c '
import json, os, sys

RS = "\x1e"

AGENT = "reviewer-blind"
RULES = [
    (".squad/design/**",   ".squad/design/"),
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


def leaves(v):
    if isinstance(v, str):
        yield v
    elif isinstance(v, dict):
        for x in v.values():
            yield from leaves(x)
    elif isinstance(v, list):
        for x in v:
            yield from leaves(x)


def segs(rel):
    if rel in ("", "."):
        return []
    return [s for s in rel.split("/") if s not in ("", ".")]


def resolve(raw, base):
    expanded = os.path.expanduser(raw)
    if os.path.isabs(expanded):
        return os.path.normpath(expanded)
    return os.path.normpath(os.path.join(base, expanded))


def classify(raw, cwd):
    abs_path = resolve(raw, cwd)
    if abs_path == cwd:
        return ("under", [])
    cwd_slash = cwd.rstrip("/") + "/"
    if abs_path.startswith(cwd_slash):
        return ("under", segs(os.path.relpath(abs_path, cwd)))
    if cwd.startswith(abs_path.rstrip("/") + "/"):
        return ("above", None)
    return ("elsewhere", abs_path)


def reaches(rseg, pat):
    pseg = pat.split("/")
    for i, r in enumerate(rseg):
        if i >= len(pseg):
            return False
        p = pseg[i]
        if p == "**":
            return True
        if p == "*":
            continue
        if r != p:
            return False
    return True


def reach_hit(raw, cwd, rules):
    """The first (pattern, label) in rules that a read/search rooted at raw
    can reach, or None. A root that resolves under (or above) cwd is tested
    first at its own true position; if nothing matched there, every shorter
    trailing run of it is tried too -- the same fallback the `elsewhere`
    branch below already needs for a foreign absolute path, and for the
    same underlying reason: a worktree is a second full checkout nested
    *under* cwd (see .gitignore), so a denied pattern’s real relative form
    can recur at a deeper offset inside one. `.claude/worktrees/agent-x/
    .squad/design/x/plan.md` is not itself an ancestor of `.squad/design`
    -- its segments diverge at "worktrees" vs "design" -- but its tail,
    past the nested checkout’s own root, is exactly that path. This
    restores what the pre-containment version of this hook did
    unconditionally for every candidate (see NOT HANDLED HERE, ABOVE); it
    does not attempt the ancestor direction of the same problem (e.g.
    rooting at ".claude/worktrees" itself, which matches nothing here
    either) -- that stays open."""
    kind, payload = classify(raw, cwd)
    if kind == "above":
        payload = []
    if kind in ("under", "above"):
        for pat, label in rules:
            if reaches(payload, pat):
                return (pat, label)
        # Starts at 1: index 0 is the check just made. An empty `payload`
        # (root is cwd itself, or above it) has no further suffixes to try,
        # so range(1, 0) is empty and this is correctly a no-op for that case.
        for start in range(1, len(payload)):
            for pat, label in rules:
                if reaches(payload[start:], pat):
                    return (pat, label)
        return None
    parts = payload.strip("/").split("/")
    for i in range(len(parts)):
        for pat, label in rules:
            if reaches(parts[i:], pat):
                return (pat, label)
    return None


def glob_root(pattern):
    """See enforce-track-blindness.sh for the full derivation (round-2
    defect: this used to return at the first wildcard segment and never
    scan past it, so a `..` or brace group behind that point was
    invisible -- `*/../../.squad/design/**` genuinely resolved and was
    allowed). The scan for danger markers now covers every segment,
    independent of where the literal prefix is cut.

    Returns ("ok", prefix): a literal prefix of segments up to the first
    plain wildcard (`*?[`) -- possibly empty (composes safely with a real
    `path`; with none, resolves to cwd itself), PROVIDED no later segment
    is a post-wildcard "..". Returns ("grouped", None) when a brace-group
    marker (`{`, `}`, `,`; T-008) appears in any segment, at any position.
    Returns ("climb", None) when a ".." segment appears anywhere after the
    first plain wildcard -- unpredictable, since the wildcard match is
    unknown. Returns ("missing", None) for an empty/non-string pattern.
    None of "grouped"/"climb"/"missing" may be silently absorbed by
    os.path.join as "nothing to add" (T-012 fix 2 / D20): the caller
    refuses these on the pattern’s shape alone, before any denied-path
    check runs -- an accepted false-positive class, not a desired
    behaviour, distinct from a genuine denied-path match."""
    if not isinstance(pattern, str) or not pattern:
        return ("missing", None)
    out = []
    prefix_open = True
    for seg in pattern.split("/"):
        if any(ch in seg for ch in "{},"):
            return ("grouped", None)
        if any(ch in seg for ch in "*?["):
            prefix_open = False
            continue
        if prefix_open:
            out.append(seg)
        elif seg == "..":
            return ("climb", None)
    return ("ok", "/".join(out))


def truthy_str(v):
    return isinstance(v, str) and bool(v.strip())


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
    # T-012: Glob’s effective root is the composition of `path` and
    # `pattern`’s own derived root, ALWAYS -- see
    # enforce-track-blindness.sh for the full reasoning.
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
' 2>/dev/null || true)"

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
