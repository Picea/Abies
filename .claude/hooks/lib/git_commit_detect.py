#!/usr/bin/env python3
"""
Argv-level detector for `git ... commit ...` invocations.

Reads the raw shell command line (the Bash tool's `tool_input.command`) on
stdin and writes bash-`eval`-able assignments on stdout:

  GIT_COMMIT_MATCH=0|1
  GIT_COMMIT_GLOBAL_ARGS=(...)
  GIT_COMMIT_ARGV=(...)

`GIT_COMMIT_GLOBAL_ARGS` is every recognised global git option that appeared
before the `commit` subcommand (`-C <dir>`, `--git-dir[=]<dir>`,
`--work-tree[=]<dir>`, `-c <key>=<value>`, ...), in original order —
replayable in front of any later `git` invocation to target the same repo.
`GIT_COMMIT_ARGV` is every token after `commit` itself.

Why this replaces a `case "$command" in *"git commit"*)` substring match:
see the co-located `git-commit-detect.sh` header and PR #355's review.
Short version — the substring match has two independent bugs:

  * False negative: `git -C <path> commit ...`, `git --git-dir=... commit`,
    and `git -c k=v commit` all put a global option before the literal
    string "commit", so the two words "git commit" never appear adjacently
    and the substring never matches.
  * False positive: any payload that merely *contains* the text
    "git commit" — e.g. `echo see: git commit -m msg` — matches, even
    though `echo` is not `git`.

This module fixes both by tokenizing the command with real argv semantics
(via `shlex`, which understands shell quoting) and checking that `git` is
actually `argv[0]` of a simple command, with `commit` reached by walking
past *only* the specific global options this repo's hooks care about.

Deliberate scope limits (documented, not silently missing):

  * Not a full shell parser. Command substitution (`$(...)`), backticks,
    and process substitution are never evaluated or recursed into — a
    `git commit` hidden inside `bash -c "git commit ..."` or produced by a
    function/alias is invisible here, exactly as it was invisible to the
    substring match it replaces.
  * Compound commands ARE split correctly on `;`, `&&`, `||`, `|`, and bare
    (unquoted) newlines, respecting quoting — so `git add -A && git commit
    -m "x"` and a two-line unquoted script both detect the commit half.
  * An unrecognised flag ahead of `commit` is assumed to take no value
    (best effort) rather than mis-consuming the next token; this can only
    ever cause an under-match (fail open), never a false trigger.
  * `_strip_transparent_prefix()` (round 4 re-review, review-b93b430.md,
    "four still-open items", closing a gap flagged twice now) peels off
    constructs that precede `git` without changing which command ultimately
    runs, before the `argv[0] == "git"` check: leading `NAME=VALUE` shell
    assignments (`GIT_AUTHOR_DATE=... git commit`), `env`/`sudo` wrapper
    invocations including `env`'s own leading flags/assignments
    (`env X=1 git commit`, `sudo git commit`), and a single enclosing
    `( ... )` subshell pair wrapping one simple command
    (`(git commit -m x)`). This is still deliberately best-effort, not
    exhaustive:
      - `sudo`'s own flags are skipped assuming none take a value (same
        "under-match, never false-trigger" posture as unrecognised git
        global flags above) — `sudo -u user git commit` will NOT be
        recognised, because `-u`'s value `user` is mis-treated as the next
        command token and fails the `== "git"` check. Fails open (no
        match), not a mis-detection.
      - Only a single top-level `( ... )` pair around exactly one simple
        command is unwrapped. `(cd dir && git commit ...)` splits into two
        simple commands on `&&` before this function ever runs, so the
        `git commit` half is examined without its enclosing parens intact
        — already handled correctly, just not through this function.
        Nested or multi-statement subshell forms beyond that are not
        specially handled.
      - Chained wrappers (`sudo env X=1 git commit`) are supported via the
        strip loop running until nothing more peels off, but each layer's
        own best-effort flag handling still applies.
"""
from __future__ import annotations

import os
import re
import shlex
import sys

# `NAME=VALUE` shell assignment prefix, e.g. `GIT_AUTHOR_DATE=2020-01-01`
# or `X=1`. Matches POSIX shell identifier rules (letters/digits/underscore,
# not starting with a digit); the value half is unconstrained (shlex has
# already resolved any quoting inside it by this point).
_ENV_ASSIGNMENT_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*=.*$")

# Wrapper commands whose presence doesn't change WHICH program ultimately
# runs, just how it's invoked -- see `_strip_transparent_prefix()`.
_TRANSPARENT_WRAPPERS = {"env", "sudo"}

# `env`'s own flags that consume a following, SEPARATE argv token as their
# value (short and long GNU forms) -- e.g. `env -u FOO git commit` must
# skip BOTH `-u` and `FOO`, not just `-u`. Attached-form long options
# (`--unset=FOO`) need no special handling: they don't split into two
# tokens, so the generic `argv[0].startswith("-")` skip in
# `_strip_transparent_prefix()` already consumes the whole thing in one
# step.
_ENV_FLAGS_WITH_VALUE = {"-u", "--unset", "-C", "--chdir"}

# Global git options that consume a following argument, e.g. `-C <dir>`.
GLOBAL_FLAGS_WITH_VALUE = {
    "-C",
    "--git-dir",
    "--work-tree",
    "-c",
    "--namespace",
    "--exec-path",
}

# The subset of the above that git also accepts in `--flag=value` form.
GLOBAL_FLAGS_ATTACHABLE = {
    "-C",
    "--git-dir",
    "--work-tree",
    "--exec-path",
    "--namespace",
}

# Global git options that never take a value.
GLOBAL_FLAGS_NO_VALUE = {
    "-p",
    "--paginate",
    "--no-pager",
    "--no-replace-objects",
    "--bare",
    "--literal-pathspecs",
    "--no-optional-locks",
    "--no-lazy-fetch",
    "--no-advice",
    "--no-superproject",
}

_SEPARATOR_CHARS = "&|;\n"


def split_simple_commands(command: str) -> list[list[str]]:
    """Best-effort split into simple commands on ``;``, ``&&``, ``||``,
    ``|``, and bare newlines, respecting shell quoting. Not a full shell
    parser: text inside a quoted word (including any command substitution
    it contains) is treated as opaque and never split or evaluated."""
    lexer = shlex.shlex(command, posix=True, punctuation_chars=_SEPARATOR_CHARS)
    lexer.whitespace_split = True
    # Newlines are punctuation (statement separators) here, not plain
    # whitespace — but only *outside* quotes; shlex still preserves a
    # literal newline that occurs inside a quoted token (e.g. a heredoc
    # body embedded in a double-quoted `-m` argument).
    lexer.whitespace = " \t\r"
    try:
        tokens = list(lexer)
    except ValueError:
        # Unbalanced quoting (e.g. a command whose heredoc/quote state
        # this best-effort tokenizer can't resolve). Fail open — same
        # posture as every hook here on ambiguity.
        return []

    commands: list[list[str]] = []
    current: list[str] = []
    for tok in tokens:
        if tok and all(c in _SEPARATOR_CHARS for c in tok):
            if current:
                commands.append(current)
                current = []
            continue
        current.append(tok)
    if current:
        commands.append(current)
    return commands


def _strip_transparent_prefix(argv: list[str]) -> list[str]:
    """Best-effort: peel off leading shell constructs that precede the
    actual command without changing which program runs -- inline
    `NAME=VALUE` assignments, `env`/`sudo` wrapper invocations (including
    `env`'s own leading flags/assignments), and a single enclosing
    `( ... )` subshell pair around one simple command. See the module
    docstring's scope-limits section for exactly what this does and does
    not cover. Never raises; worst case returns `argv` unchanged (falls
    through to the existing `argv[0] != "git"` under-match, not a false
    trigger)."""
    if not argv:
        return argv
    argv = list(argv)

    had_paren = argv[0].startswith("(")
    if had_paren:
        argv[0] = argv[0][1:]
        if argv[0] == "":
            argv = argv[1:]

    changed = True
    while changed and argv:
        changed = False
        while argv and _ENV_ASSIGNMENT_RE.match(argv[0]):
            argv = argv[1:]
            changed = True
        if argv and os.path.basename(argv[0]) in _TRANSPARENT_WRAPPERS:
            wrapper = os.path.basename(argv[0])
            argv = argv[1:]
            changed = True
            if wrapper == "sudo":
                # Best-effort: assume sudo's own flags take no value (same
                # posture as unrecognised git global flags in
                # find_commit()) -- `sudo -u user git commit` will under-
                # match (fails open) rather than mis-consume `user`.
                while argv and argv[0].startswith("-"):
                    argv = argv[1:]
            else:  # "env"
                # `env` accepts its own flags (-i, -0, -u NAME, ...) and
                # NAME=VALUE assignments before the target command. Flags
                # in `_ENV_FLAGS_WITH_VALUE` consume the NEXT token too
                # (round 5 re-review, review-pr355-round4.md, should-fix:
                # `env -u FOO git commit` previously under-matched because
                # only `-u` was skipped, leaving `FOO` mis-treated as the
                # next command token -- the inline comment claimed `-u
                # NAME` was handled when it wasn't, the same "comment says
                # more than the code does" defect class flagged across
                # four rounds now).
                while argv and (
                    argv[0].startswith("-") or _ENV_ASSIGNMENT_RE.match(argv[0])
                ):
                    tok = argv[0]
                    name, sep, _ = tok.partition("=")
                    # `sep` is non-empty for the attached form
                    # (`--unset=FOO`), which is already a single, complete
                    # token -- only the SEPARATE-token form (`-u FOO`,
                    # `--unset FOO`) needs a second token consumed. Getting
                    # this backwards would eat the next REAL argv token
                    # (potentially `git` itself) for `--unset=FOO`.
                    if not sep and name in _ENV_FLAGS_WITH_VALUE and len(argv) > 1:
                        argv = argv[2:]
                    else:
                        argv = argv[1:]

    if had_paren and argv:
        last = argv[-1]
        if last == ")":
            argv = argv[:-1]
        elif last.endswith(")"):
            argv[-1] = last[:-1]

    return argv


def find_commit(argv: list[str]):
    """If ``argv`` (with ``argv[0] == 'git'``) invokes the ``commit``
    subcommand, return ``(global_args, commit_argv)``. Otherwise
    ``(None, None)`` — argv[0] is git but the subcommand isn't commit, or
    an unrecognisable token appeared where a subcommand was expected."""
    global_args: list[str] = []
    i = 1
    n = len(argv)
    while i < n:
        tok = argv[i]
        if tok == "commit":
            return global_args, argv[i + 1 :]
        if not tok.startswith("-"):
            # A non-flag, non-"commit" token before the subcommand: some
            # other git invocation (`git status`, `git log`, ...).
            return None, None

        name, sep, _value = tok.partition("=")
        if sep and name in GLOBAL_FLAGS_ATTACHABLE:
            global_args.append(tok)
            i += 1
            continue
        if tok in GLOBAL_FLAGS_WITH_VALUE:
            global_args.append(tok)
            if i + 1 < n:
                global_args.append(argv[i + 1])
                i += 2
                continue
            i += 1
            continue
        if tok in GLOBAL_FLAGS_NO_VALUE:
            global_args.append(tok)
            i += 1
            continue
        # Unrecognised flag ahead of the subcommand: assume no value
        # (best effort) and keep scanning rather than risk mis-reading its
        # value as the subcommand.
        global_args.append(tok)
        i += 1
    return None, None


def detect(command: str) -> tuple[bool, list[str], list[str]]:
    for argv in split_simple_commands(command):
        if not argv:
            continue
        argv = _strip_transparent_prefix(argv)
        if not argv:
            continue
        if os.path.basename(argv[0]) != "git":
            continue
        global_args, commit_argv = find_commit(argv)
        if global_args is None:
            continue
        return True, global_args, commit_argv
    return False, [], []


def bash_array(name: str, items: list[str]) -> str:
    quoted = " ".join(shlex.quote(item) for item in items)
    return f"{name}=({quoted})"


def main() -> int:
    try:
        command = sys.stdin.read()
    except Exception:
        command = ""

    matched, global_args, commit_argv = detect(command)

    print(f"GIT_COMMIT_MATCH={'1' if matched else '0'}")
    print(bash_array("GIT_COMMIT_GLOBAL_ARGS", global_args))
    print(bash_array("GIT_COMMIT_ARGV", commit_argv))
    return 0


if __name__ == "__main__":
    sys.exit(main())
