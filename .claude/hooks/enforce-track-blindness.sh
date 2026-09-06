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
#                               knowledge scan, Track B's artifact, and the
#                               pattern lexicon — which is, literally, a
#                               list of pattern names.
#   dreamer-informed          — denied Track A's artifact. The blindness is
#                               claimed as mutual; enforcing one side and
#                               promising the other makes the claim false in
#                               a way that is hard to see.
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
#      *always* scope-bearing (T-012), so the effective root composes
#      `path` with `glob_root(pattern)` rather than using `path` alone
#      whenever it is given -- `Glob(pattern="src/**/*.cs")` with no
#      `path` is scoped to `src/`, and `Glob(pattern="../denied/**",
#      path="docs")` composes to a root above `docs`, not to `docs`
#      unchanged. Each tool's scope is therefore computed by its own rule
#      below, not by walking every string in tool_input and hoping.
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

payload="$(cat)"

reason="$(printf '%s' "$payload" | python3 -c '
import json, os, sys

RS = "\x1e"  # python->bash field separator; avoids collision with pipes in
             # Grep patterns or Glob globs, unlike the "|" used before.

DENY = {
    "dreamer-first-principles": [
        (".claude/docs/decisions.md",        "the decision register"),
        (".claude/docs/decisions-archive/**", "the archived decision register"),
        (".claude/docs/tech-stack.md",       "the tech stack record"),
        (".claude/docs/pattern-lexicon.md",  "the pattern lexicon"),
        (".claude/agent-memory/**",          "another agent’s persistent memory"),
        (".squad/decisions/**",              "the decision drops"),
        (".squad/design/*/00-knowledge.md",  "the conductor’s knowledge scan"),
        (".squad/design/*/02-track-b.md",    "Track B’s artifact"),
    ],
    "dreamer-informed": [
        (".squad/design/*/01-track-a.md",    "Track A’s artifact"),
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


def leaves(v):
    """Every string in the tool input. Used only for tools whose whole
    schema is path-shaped (Read, NotebookRead, mcp__*) -- never for Grep or
    Glob, which have a non-path argument (`pattern`) that must not be
    walked this way. See the header."""
    if isinstance(v, str):
        yield v
    elif isinstance(v, dict):
        for x in v.values():
            yield from leaves(x)
    elif isinstance(v, list):
        for x in v:
            yield from leaves(x)


def segs(rel):
    """rel as path segments, with "." and "" (cwd itself) as zero segments."""
    if rel in ("", "."):
        return []
    return [s for s in rel.split("/") if s not in ("", ".")]


def resolve(raw, base):
    """raw resolved as a real tool would resolve it: ~ expands to the home
    directory independently of base; an already-absolute path is used as
    is; anything else is joined against base. Never compared as a literal
    string before this."""
    expanded = os.path.expanduser(raw)
    if os.path.isabs(expanded):
        return os.path.normpath(expanded)
    return os.path.normpath(os.path.join(base, expanded))


def classify(raw, cwd):
    """Where raw actually resolves to, relative to cwd:
      ("under", segments) -- cwd itself (segments == []) or a real
        descendant of it (segments is its path below cwd).
      ("above", None)     -- cwd itself or a proper ancestor of it (.., ~,
        an absolute path higher in the tree, ...). Reaches everything cwd
        contains, which is everything on the deny list.
      ("elsewhere", abs)  -- neither: some other absolute location, e.g. a
        path into a different checkout of the same repo. Only this case
        falls back to trying every trailing segment run of abs, which is
        the one place that heuristic is legitimate."""
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
    """True if root segments rseg name pat exactly, name a real ancestor
    directory of it, or (via a "**" segment) are already inside the subtree
    pat denies. An empty segment list is vacuously a prefix of every
    pattern -- the "root is cwd itself, or above it" case falls out of this
    naturally rather than needing its own rule."""
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
    .claude/docs/decisions.md` is not itself an ancestor of
    `.claude/docs/decisions.md` -- its segments diverge at "worktrees" vs
    "docs" -- but its tail, past the nested checkout’s own root, is exactly
    that file. This restores what the pre-containment version of this hook
    did unconditionally for every candidate (see NOT HANDLED HERE, ABOVE);
    it does not attempt the ancestor direction of the same problem (e.g.
    rooting at ".claude/worktrees" itself, which matches nothing here
    either) -- that stays open."""
    kind, payload = classify(raw, cwd)
    if kind == "above":
        payload = []  # strictly broader than cwd; same effect as rooting at cwd
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
    """Glob has no separate scope argument -- its `pattern` is always
    scope-bearing, `path` present or not. The scan for danger markers
    covers every segment of `pattern`, independent of where the literal
    prefix is cut -- a marker behind the first wildcard is exactly as real
    as one in front of it (round-2 defect: the previous version returned at
    the first wildcard segment and never looked past it, so a `..` or a
    brace group behind that point was invisible -- `*/../../.claude/docs/*.md`
    genuinely resolves to real denied files and was allowed).

    Returns ("ok", prefix): a literal (non-wildcard, non-group) prefix of
    segments up to the first plain wildcard (`*?[`), e.g. "src/**/*.cs"
    roots at "src", "../../.claude/docs/**" roots at "../../.claude/docs".
    `prefix` may be empty when a plain wildcard occupies the very first
    segment (e.g. "*.md") -- composes safely with a real `path` via
    os.path.join (contributes nothing further, PROVIDED no later segment
    is a post-wildcard ".." -- see "climb" below) and, with no `path` at
    all, resolves to cwd itself, the same escalate-to-cwd result the
    unclassifiable cases get, by construction.

    Returns ("grouped", None) when a brace-group marker (`{`, `}`, `,`;
    T-008) appears in ANY segment, at ANY position in the pattern -- a
    group can hide an alternate branch this single-prefix scan cannot
    represent (`{../denied,ok}` could resolve above whatever prefix was
    already accumulated, whether seen before or after a wildcard).

    Returns ("climb", None) when a ".." segment appears anywhere AFTER the
    first plain wildcard -- a wildcard-matched directory followed by ".."
    can climb to wherever that match happened to land, which this
    single-prefix scan cannot predict from the pattern text alone.

    Returns ("missing", None) when pattern is empty/non-string (Glob’s
    schema requires it; defensive, not an expected shape).

    None of "grouped"/"climb"/"missing" may be silently absorbed by
    os.path.join as if they meant "nothing to add" (T-012 fix 2 / D20):
    the caller refuses these on the pattern’s shape alone, before any
    denied-path check runs -- an accepted false-positive class, not a
    desired behaviour, and distinct from a genuine denied-path match."""
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
    # T-012: Glob’s effective root is the composition of `path` and
    # `pattern`’s own derived root, ALWAYS -- unlike Grep, `pattern` is
    # scope-bearing whether or not `path` is also given, so `path` alone
    # (the pre-T-012 rule, correct for Grep, wrong for Glob) must never be
    # the whole story here.
    p = tool_input.get("path")
    pattern = tool_input.get("pattern")
    g_reason, pattern_root = glob_root(pattern)
    if g_reason != "ok":
        # A pattern glob_root cannot classify (T-012 fix 2 / D20: a brace
        # group anywhere, or a ".." after the first wildcard) is refused on
        # its shape alone, before any denied-path check runs -- this is
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
' 2>/dev/null || true)"

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
