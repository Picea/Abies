#!/usr/bin/env bash
#
# SubagentStop hook, matching `architect`.
#
# Runs the deterministic half of gate 1: a lexical scan of the design pass's
# 00-scope.md — the only artifact `dreamer-first-principles` reads — for the
# three things that can be found by matching rather than by reading:
#
#   1. decision drop ids, and the pattern names their slugs carry
#   2. terms and recall grammar from .claude/docs/pattern-lexicon.md
#   3. paths into files Track A is denied by enforce-track-blindness.sh
#
# It writes .squad/design/<slug>/00-warden-scan.md and prints a summary.
# The fourth category — prior work presented as reference material — is a
# judgement call and belongs to the `scope-warden` subagent, which reads this
# scan and writes 00-warden.md on top of it. The user holds gate 1.
#
# IT REPORTS. IT NEVER REWRITES. 00-scope.md is not touched, and this hook
# never blocks: a scope with findings is not necessarily a bad scope, it is a
# scope somebody should look at. Exit is always 0.
#
# The `architect` matcher is declared in settings.json AND re-checked here
# against `agent_type` from the payload, which Claude Code populates when a
# hook fires inside a subagent. Belt and braces: SubagentStop matcher support
# is the platform's, the agent_type check is ours, and if either works the
# hook only runs where it should.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR silently stops matching inside a worktree — and still
# passes any test that has no worktree in it.
#
# Exit codes:
#   0 — always. This hook informs a gate; it does not hold one.
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "architect",
#     "cwd": "/path/to/checkout-or-worktree",
#     "stop_hook_active": false
#   }

set -uo pipefail

HOOKS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export HOOKS_LIB_DIR="$HOOKS_DIR/lib"

# Unlike the blocking hooks in this directory, this one's own contract is
# "exit is always 0" (see the header) -- a missing interpreter must not
# become a silent block, but it also must not become a silent no-op: without
# python3 this hook cannot write 00-warden-scan.md at all, and gate 1
# downstream (the scope-warden SUBAGENT) depends on that file existing. Say
# so loudly on stderr and still exit 0, rather than leaving no trace of why
# the scan never appeared.
if ! command -v python3 >/dev/null 2>&1; then
  echo "scope-warden: python3 is not on PATH -- could not run the mechanical scan; 00-warden-scan.md was NOT written. Gate 1 still requires the scope-warden subagent's own read." >&2
  exit 0
fi

payload="$(cat 2>/dev/null || true)"

printf '%s' "$payload" | python3 -c '
import json, os, re, sys

sys.path.insert(0, os.environ["HOOKS_LIB_DIR"])
from artifact_attribution import resolve_artifact

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)

if d.get("stop_hook_active"):
    sys.exit(0)

agent = (d.get("agent_type") or "").strip()
# Empty agent_type means an older payload shape that does not carry it; the
# settings.json matcher is then the only filter and we proceed. A populated
# agent_type that is not the architect means the matcher let something else
# through, and we stop.
if agent and agent != "architect":
    sys.exit(0)

root = d.get("cwd") or os.getcwd()
design = os.path.join(root, ".squad", "design")
if not os.path.isdir(design):
    sys.exit(0)

# Which pass THIS architect invocation wrote 00-scope.md for -- see
# artifact_attribution.py for why this is no longer a bare newest-mtime
# guess across every slug directory.
transcript_path = d.get("transcript_path") or d.get("agent_transcript_path") or ""
resolved = resolve_artifact(design, "00-scope.md", transcript_path)
if resolved is None:
    sys.exit(0)
slug, scope_path, _mtime, _method = resolved

lexicon_path = os.path.join(root, ".claude", "docs", "pattern-lexicon.md")


def load_lexicon(path):
    """Bullet items under `## Terms` and under `## Recall grammar`."""
    terms, recall = [], []
    if not os.path.isfile(path):
        return terms, recall
    bucket = None
    for line in open(path, encoding="utf-8", errors="replace"):
        s = line.strip()
        if s.startswith("## "):
            h = s[3:].strip().lower()
            bucket = terms if h == "terms" else (recall if h == "recall grammar" else None)
            continue
        if bucket is not None and s.startswith("- "):
            bucket.append(s[2:].strip())
    return terms, recall


def term_regex(t):
    # `-` and space interchangeable; whole words; metacharacters escaped.
    parts = [re.escape(p) for p in re.split(r"[\s-]+", t) if p]
    if not parts:
        return None
    return re.compile(r"(?<![A-Za-z0-9])" + r"[\s-]+".join(parts) + r"(?![A-Za-z0-9])", re.I)


TERMS, RECALL = load_lexicon(lexicon_path)

ID_RX = re.compile(
    r"\b([a-z][a-z0-9-]*)-(\d{8}T\d{6}Z|\d{4}-\d{2}-\d{2}T[\d:-]+Z?)-([a-z0-9][a-z0-9-]*)\b", re.I)

PATH_RX = re.compile(
    r"(?<![\w/.-])("
    r"\.claude/docs/decisions\.md"
    r"|\.claude/docs/decisions-archive/[^\s`)\"]*"
    r"|\.claude/docs/tech-stack\.md"
    r"|\.claude/agent-memory/[^\s`)\"]*"
    r"|\.squad/decisions/[^\s`)\"]*"
    r"|[^\s`)\"]*/00-knowledge\.md"
    r"|[^\s`)\"]*/02-track-b\.md"
    r")")

lines = open(scope_path, encoding="utf-8", errors="replace").read().splitlines()

ids, names, paths = [], [], []
for n, line in enumerate(lines, 1):
    for m in ID_RX.finditer(line):
        drop_id, _, tail = m.group(0), m.group(2), m.group(3)
        carries = [t for t in TERMS if (rx := term_regex(t)) and rx.search(tail.replace("-", " "))]
        ids.append((n, line.strip(), drop_id, carries))
    for t in TERMS:
        rx = term_regex(t)
        if rx and rx.search(line):
            names.append((n, line.strip(), t, "term"))
    for t in RECALL:
        rx = term_regex(t)
        if rx and rx.search(line):
            names.append((n, line.strip(), t, "recall"))
    for m in PATH_RX.finditer(line):
        paths.append((n, line.strip(), m.group(0)))

total = len(ids) + len(names) + len(paths)

out = []
out.append("# 🛡️ Scope Warden — mechanical scan — %s" % slug)
out.append("")
out.append("Written by `.claude/hooks/scope-warden.sh` on `SubagentStop`. This is the")
out.append("deterministic half of gate 1: what can be found by matching. The fourth")
out.append("category — prior work presented as reference material — is a judgement call")
out.append("and belongs to the `scope-warden` subagent, which reads this file and writes")
out.append("`00-warden.md`. Nothing here rewrites the scope.")
out.append("")
out.append("**Checked:** `.squad/design/%s/00-scope.md` (%d lines)" % (slug, len(lines)))
out.append("**Lexicon:** `.claude/docs/pattern-lexicon.md` — %d terms, %d recall phrases"
           % (len(TERMS), len(RECALL)))
out.append("**Verdict:** %s" % ("CLEAN" if total == 0 else "FINDINGS (%d)" % total))
out.append("")

def section(title, rows, fmt, empty):
    out.append("## %s" % title)
    if not rows:
        out.append("")
        out.append("_%s_" % empty)
    else:
        out.append("")
        for r in rows:
            out.append(fmt(r))
    out.append("")

section("Decision ids", ids,
        lambda r: "- **L%d:** `%s`%s\n  > %s" % (
            r[0], r[2],
            ("\n  Slug carries the pattern name(s): %s — the id form hides this; it looks like an opaque handle and is not one."
             % ", ".join("`%s`" % c for c in r[3])) if r[3] else "",
            r[1]),
        "None found.")

section("Pattern names and recall grammar", names,
        lambda r: "- **L%d:** %s `%s`\n  > %s" % (
            r[0], "lexicon term" if r[3] == "term" else "recall phrase", r[2], r[1]),
        "None found.")

section("Paths Track A may not read", paths,
        lambda r: "- **L%d:** `%s`\n  > %s" % (r[0], r[2], r[1]),
        "None found.")

out.append("## Not checked here")
out.append("")
out.append("**Prior work presented as reference material.** No regex separates")
out.append("*\"transitions must be total\"* from *\"we solved this with a state machine")
out.append("last time\"*. Dispatch `scope-warden` for that category and for the gate")
out.append("question.")
out.append("")

target = os.path.join(design, slug, "00-warden-scan.md")
try:
    with open(target, "w", encoding="utf-8") as fh:
        fh.write("\n".join(out) + "\n")
except OSError as e:
    print("scope-warden: could not write %s (%s)" % (target, e))
    sys.exit(0)

rel = os.path.relpath(target, root)
if total == 0:
    print("🛡️  scope-warden: %s — mechanical scan CLEAN. See %s." % (slug, rel))
else:
    print("🛡️  scope-warden: %s — %d finding(s) in 00-scope.md: %d decision id(s), "
          "%d pattern name(s)/recall phrase(s), %d denied path(s). See %s."
          % (slug, total, len(ids), len(names), len(paths), rel))
print("    Gate 1 belongs to the user. Dispatch scope-warden for the judgement category, "
      "then surface both before dispatching either Dreamer track.")
' 2>/dev/null || true

exit 0
