"""
Shared containment engine for the PreToolUse blindness hooks
(enforce-track-blindness.sh, enforce-review-blindness.sh).

Extracted because `leaves`/`segs`/`resolve`/`classify`/`reaches`/`reach_hit`/
`glob_root`/`truthy_str` were byte-identical between the two hooks apart from
docstrings -- ~100 lines of security-critical path logic maintained in two
places that nothing asserted agreed, so a fix applied to one would not reach
the other. See review round 1 of PR #358, finding ⚠️-7.

Both hooks still decide their OWN thing: which agent they govern, what their
DENY rules are, and how they format a refusal. Only the "does this read reach
a denied path" computation lives here. Import it with the same two-line
pattern both hooks use (see either hook's `python3 -c '...'` block for the
call site):

    import sys, os
    sys.path.insert(0, os.environ["HOOKS_LIB_DIR"])
    from path_containment import reach_hit, glob_root, leaves, truthy_str

`HOOKS_LIB_DIR` is set by the bash wrapper to this file's own directory
(`$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)`) before invoking python3 --
not derived inside the embedded script itself, because a `python3 -c` string
has no `__file__` to resolve a sibling import from.

THE RULE, IN ONE SENTENCE: refuse any read whose effective scope is at or
above a denied path. That covers what would otherwise look like three
separate rules:
  - exact:    Read/Grep/Glob targeting a denied path itself.
  - ancestor: Grep/Glob rooted at a real directory that contains it, e.g.
              `path: ".claude"` reaches `.claude/docs/decisions.md` even
              though ".claude" is not itself on the deny list.
  - no path:  Grep/Glob take an optional `path` that defaults to the entire
              cwd when omitted -- cwd is the ultimate ancestor of everything
              in the repo, so "no path" is the root-of-everything instance
              of "ancestor", not a separate case.
A deny list that matches paths cannot refuse an invocation naming none, or
naming only a coarser directory than the entries on the list -- so all of the
above are folded into one containment test (`reach_hit`) instead of an
exact-match test plus separate pre-checks.

See enforce-track-blindness.sh's header for the fuller derivation (root
resolution, the worktree trailing-segment-run fallback, the accepted
false-positive costs on `tests/fixtures/.claude` and `docs/examples/.squad`,
and the Grep-pattern-is-content-vs-Glob-pattern-is-always-scope distinction)
-- this module is the computation the two hooks share; the reasoning for WHY
it is shaped this way lives with the callers, not duplicated a third time
here.
"""

import os


def leaves(v):
    """Every string in the tool input. Used only for tools whose whole
    schema is path-shaped (Read, NotebookRead, mcp__*) -- never for Grep or
    Glob, which have a non-path argument (`pattern`) that must not be walked
    this way."""
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
    directory independently of base; an already-absolute path is used as is;
    anything else is joined against base. Never compared as a literal string
    before this."""
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
    branch below already needs for a foreign absolute path, and for the same
    underlying reason: a worktree is a second full checkout nested *under*
    cwd (see .gitignore), so a denied pattern's real relative form can recur
    at a deeper offset inside one. This restores what the pre-containment
    version of both hooks did unconditionally for every candidate."""
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


# NOTE (PR #358 review round 2, finding 🔴-B): a prior revision of this
# docstring said reach_hit "does not attempt the ancestor direction of the
# same problem (e.g. rooting at `.claude/worktrees` itself) -- that stays
# open in both callers." It no longer does. That gap was never inside this
# function's containment logic -- `reaches()` above already refuses a query
# rooted AT or ABOVE any pattern that is actually on a caller's deny list,
# the same way `path=".claude"` already reaches `.claude/docs/decisions.md`
# -- it was that neither caller's list carried an entry naming the worktree
# directory itself, so nothing here had anything to reach FOR that case.
# Both callers now carry an explicit `(".claude/worktrees/**", ...)` deny
# entry; see enforce-track-blindness.sh and enforce-review-blindness.sh for
# that entry and the accepted collateral cost it carries (a coarse read
# rooted exactly at `.claude` becomes newly refused for an agent that was
# not already refused there for an unrelated reason).


def glob_root(pattern):
    """Glob has no separate scope argument -- its `pattern` is always
    scope-bearing, `path` present or not. The scan for danger markers covers
    every segment of `pattern`, independent of where the literal prefix is
    cut -- a marker behind the first wildcard is exactly as real as one in
    front of it.

    Returns ("ok", prefix): a literal (non-wildcard, non-group) prefix of
    segments up to the first plain wildcard (`*?[`), e.g. "src/**/*.cs" roots
    at "src", "../../.claude/docs/**" roots at "../../.claude/docs". `prefix`
    may be empty when a plain wildcard occupies the very first segment (e.g.
    "*.md") -- composes safely with a real `path` via os.path.join
    (contributes nothing further, PROVIDED no later segment is a
    post-wildcard ".." -- see "climb" below) and, with no `path` at all,
    resolves to cwd itself, the same escalate-to-cwd result the
    unclassifiable cases get, by construction.

    Returns ("grouped", None) when a brace-group marker (`{`, `}`, `,`)
    appears in ANY segment, at ANY position in the pattern -- a group can
    hide an alternate branch this single-prefix scan cannot represent
    (`{../denied,ok}` could resolve above whatever prefix was already
    accumulated, whether seen before or after a wildcard).

    Returns ("climb", None) when a ".." segment appears anywhere AFTER the
    first plain wildcard -- a wildcard-matched directory followed by ".."
    can climb to wherever that match happened to land, which this
    single-prefix scan cannot predict from the pattern text alone.

    Returns ("missing", None) when pattern is empty/non-string (Glob's
    schema requires it; defensive, not an expected shape).

    None of "grouped"/"climb"/"missing" may be silently absorbed by
    os.path.join as if they meant "nothing to add": the caller refuses these
    on the pattern's shape alone, before any denied-path check runs -- an
    accepted false-positive class, not a desired behaviour, and distinct
    from a genuine denied-path match."""
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
