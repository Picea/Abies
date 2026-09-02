"""
Shared agent-identity resolution used by .claude/hooks/tests/run.sh's
roster_of() and duplicate_names_of() checks.

Extracted from two heredocs that were previously kept in lockstep only by
a "kept in lockstep" comment, with nothing that actually enforced it: fix
one copy and the other silently disagrees. That's exactly what happened
with the B1 regression (a leading UTF-8 BOM defeating frontmatter
detection) -- both invariants read the same wrong answer only because both
copies happened to share the same bug, not because anything forced them
to share the fix.

Deliberately NOT shared with scribe-decision-merger.sh's decision-drop
validator despite that also having its own frontmatter-fence detection
with the same historical BOM/empty-fence bug (see its TODO(#8) comment):
that parser's downstream walk over the frontmatter body (the nested-
mapping list-item logic) is a different schema, parsed for a different
purpose (field extraction against a whitelist/verdict/scope schema, not
identity resolution), and is explicitly not a candidate for reuse here --
see agent_name()'s own comment below for why.
"""
import re


def _unquote(v):
    if (v.startswith('"') and v.endswith('"')) or (v.startswith("'") and v.endswith("'")):
        return v[1:-1]
    return v


def _strip_trailing_comment(v):
    # A ` #...` outside of a quoted scalar is a YAML comment, not part of
    # the value (e.g. `name: reviewer # note`). Quote-aware so a literal `#`
    # inside a quoted value (`name: "rev#iewer"`) is not mistaken for one.
    # Runs BEFORE _unquote: a quoted value followed by a comment
    # (`name: "reviewer" # note`) doesn't start-and-end with a quote char
    # until the comment is off, so _unquote alone can't see through it.
    in_quote = None
    for i, ch in enumerate(v):
        if in_quote:
            if ch == in_quote:
                in_quote = None
        elif ch in ('"', "'"):
            in_quote = ch
        elif ch == "#" and (i == 0 or v[i - 1].isspace()):
            return v[:i].rstrip()
    return v


def _frontmatter_body(text):
    """Returns the YAML frontmatter body (the text strictly between the
    opening and closing `---` fences), or None if no fence is present.

    Replaces a naive `re.match(r"\\s*---\\s*\\n(.*?)\\n---\\s*\\n", ...)`,
    which got two cases wrong -- both reviewer-verified false GREENs
    against the roster invariant (a BOM-prefixed charter with its identity
    silently changed left the check green, and a bogus agent in a BOM'd
    decision drop bypassed scribe-decision-merger.sh's whitelist the same
    way -- see that script's TODO(#8)):

      * A leading UTF-8 BOM (U+FEFF, category Cf) is not matched by `\\s`,
        so a BOM'd file with plainly-present frontmatter read as "no
        frontmatter at all" and fell through to the caller's no-frontmatter
        handling (agent_name()'s path.stem fallback here; LEGACY mode in
        the decision-drop validator) instead of having its fields parsed.
      * A degenerate empty-body fence (`---\\n---\\n`, zero lines between
        the markers) required *two* newlines under the old pattern (one
        ending the opening line, one before the closing line), but the
        input only has one -- so it also read as "no frontmatter."

    Strips a leading BOM before matching, and finds the closing fence with
    its own regex search (a `^---` line) rather than folding it into the
    same alternation as the body, so a zero-line body is a valid match
    rather than a required-blank-line special case.
    """
    if text.startswith("\ufeff"):
        text = text[1:]
    # Leading run: `\s*`, anchored at the start of the (BOM-stripped) text,
    # so it can only ever consume whitespace before matching `---` -- not
    # narrowed to `[ \t\r\n]`, which excludes every other character in
    # Python's `\s` class on `str` (NBSP U+00A0, form feed U+000C, vertical
    # tab U+000B, U+001C-001F, U+0085, U+1680, U+2000-200A, U+2028, U+2029,
    # U+202F, U+205F, U+3000). A prior narrowing here reopened the same
    # false-GREEN this function's docstring describes for the BOM: any of
    # those 25 code points prefixing an otherwise-valid fence made this
    # return None (treated as "no frontmatter") instead of finding the
    # fence -- reviewer-verified against agent_name()'s path.stem fallback.
    # The trailing `[ \t]*` below (before the fence's own newline) is
    # intentionally NOT `\s*`: it must not consume the newline that ends
    # the opening fence line, so it stays restricted to horizontal
    # whitespace only. That asymmetry is safe and correct, unlike the
    # leading one.
    m = re.match(r"\s*---[ \t]*\r?\n", text)
    if not m:
        return None
    rest = text[m.end():]
    # `(?:\r?\n|\Z)`, not a bare `\r?\n`: a closing fence at end-of-file
    # with no trailing newline (`---\nid: x\n---`, no final `\n`) is a
    # syntactically complete fence -- YAML frontmatter parsers don't
    # require a trailing newline after the closing `---` -- but a
    # newline-only terminator never matches at `\Z`, so this fell through
    # to "no fence" and stem-leaked in agent_name() the same way V12's
    # empty-body fence did before being fixed. `\Z` (absolute end of
    # string) rather than `$` because `$` in MULTILINE mode also matches
    # just before a trailing `\n`, which would wrongly accept a fence line
    # that has more content after it on the same logical line.
    close = re.search(r"(?m)^---[ \t]*(?:\r?\n|\Z)", rest)
    if not close:
        return None
    return rest[:close.start()]


def agent_name(path):
    # Identity is the file's *top-level* `name:` frontmatter field -- what a
    # drop's `agent:` field is actually compared against. The stem fallback
    # exists ONLY for the "no frontmatter block at all" case; frontmatter
    # that IS present but declares no usable top-level `name:` must NOT
    # degrade to path.stem too. Two reviewer-verified variants defeat a
    # blanket fallback:
    #   - indented-root frontmatter (every line, including `name:`, carries
    #     leading whitespace) -- PyYAML resolves the mapping's `name` key
    #     regardless, but a column-0 scan sees no candidate at all;
    #   - a file whose frontmatter genuinely has no `name:` line.
    # A third, distinct case defeats *detecting the frontmatter block at
    # all* rather than the name-line scan within it -- see
    # _frontmatter_body()'s docstring for the BOM and empty-fence variants.
    # In all cases, falling back to path.stem let a coincidentally-matching
    # stem (e.g. a file still named reviewer.md) stand in for a real,
    # different (or entirely absent) identity -- silently keeping the
    # roster invariant green while the real identity went unwhitelisted.
    # Falling through to the sentinel below instead guarantees no
    # accidental equality with a real agent name, so the invariant fails
    # loud on these instead of passing on a stem coincidence.
    #
    # Matching against the raw (unstripped) line -- not a `.strip()`-first
    # scan -- is deliberate: frontmatter keys start at column 0, so
    # `raw.startswith("name:")` is true only for a genuine top-level key. A
    # stripped scan matches a `name:` nested under any other key (e.g.
    # inside a `metadata:` block) just as readily as the real one.
    #
    # When more than one column-0 `name:` line exists, the LAST one wins --
    # matching YAML's own last-key-wins rule for a duplicate mapping key.
    # Returning on the first match let a file declare its real identity
    # second and have an earlier, shadowing `name:` silently stand in for
    # it instead -- again a false-negative roster entry admitting a
    # same-named drop from a different, unintended agent.
    #
    # We do NOT reuse the decision-drop validator's indentation tracking
    # here even though it exists in the same codebase: that parser only
    # tracks indentation to extend a list item's mapping after a `- `
    # marker; for any OTHER line it treats `key: value` as top-level
    # regardless of indentation, so a nested `name:` there wins by being
    # *last write*, not by being genuinely top-level -- promotion, not
    # last-top-level-wins. Borrowing it would swap a first-wins bug for a
    # promotion bug, not fix either. Column-0 scanning with
    # last-top-level-wins is necessary (nested keys never masquerade as
    # identity) AND, unlike the first-match version this replaces,
    # last-write-correct among genuine top-level duplicates -- it doesn't
    # require sharing a parser between two unrelated schemas (agent charter
    # frontmatter has no nested mappings at all; decision-drop frontmatter's
    # only nesting is the documented list-of-mappings form).
    try:
        text = path.read_text(encoding="utf-8")
    except Exception:
        return path.stem
    body = _frontmatter_body(text)
    if body is None:
        return path.stem
    resolved = None
    for raw in body.splitlines():
        if raw.startswith("name:"):
            value = _unquote(_strip_trailing_comment(raw.split(":", 1)[1].strip()))
            if value:
                resolved = value
    if resolved is not None:
        return resolved
    # Frontmatter exists but declared no usable top-level `name:` -- see the
    # comment above for why this does NOT fall back to path.stem.
    return f"<no top-level name: in {path.name}>"
