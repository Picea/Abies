#!/usr/bin/env bash
#
# SubagentStop hook, matching `dreamer-first-principles`.
#
# Track A's deepest limit is that its weights are prior art and no tool grant
# reaches them. Prevention is unavailable; detection is not. This hook scans
# .squad/design/<slug>/01-track-a.md for pattern names and for the grammar of
# recall — "the standard approach", "commonly", "typically", "the usual",
# "well-known" — and BLOCKS, quoting the offending sentences, so the artifact
# goes back for a rewrite. That moves "do not name patterns" from an
# instruction to an invariant. It does not make the agent ignorant; it makes
# recall visible and rejectable.
#
# The vocabulary is .claude/docs/pattern-lexicon.md, owned by the curator.
#
# THE OVERRIDE IS NOT AN ESCAPE HATCH. A scope legitimately about state
# machines trips a check looking for "state machine". A blocking check with no
# way past it is a check somebody deletes after the second false positive, and
# then there is no data about why. So the override exists from the first run:
#
#   .squad/design/<slug>/.lexicon-override
#
# Create it — empty, or with one term per line to override only those — and
# re-run the phase. `touch` is enough. The Lead is instructed to offer this
# rather than to reword the artifact on the agent's behalf.
#
# Every hit and every override appends to .squad/log/lexicon-hits.md: term,
# artifact, surrounding sentence, outcome. That file is the curator's only
# calibration input — recurring true hits become new terms, recurring
# overrides get the term narrowed or dropped. It is never rotated.
#
# The `dreamer-first-principles` matcher is declared in settings.json AND
# re-checked here against `agent_type` from the payload, which Claude Code
# populates when a hook fires inside a subagent.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR silently stops matching inside a worktree — and still
# passes any test that has no worktree in it.
#
# Exit codes:
#   0 — clean, or overridden (both logged)
#   2 — blocked (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "dreamer-first-principles",
#     "cwd": "/path/to/checkout-or-worktree",
#     "stop_hook_active": false
#   }

set -uo pipefail

payload="$(cat 2>/dev/null || true)"

reason="$(printf '%s' "$payload" | python3 -c '
import json, os, re, sys, datetime

try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)

if d.get("stop_hook_active"):
    sys.exit(0)

agent = (d.get("agent_type") or "").strip()
if agent and agent != "dreamer-first-principles":
    sys.exit(0)

root = d.get("cwd") or os.getcwd()
design = os.path.join(root, ".squad", "design")
if not os.path.isdir(design):
    sys.exit(0)

arts = []
for slug in os.listdir(design):
    p = os.path.join(design, slug, "01-track-a.md")
    if os.path.isfile(p):
        arts.append((os.path.getmtime(p), slug, p))
if not arts:
    sys.exit(0)
arts.sort()
_, slug, art_path = arts[-1]


def load_lexicon(path):
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
    parts = [re.escape(p) for p in re.split(r"[\s-]+", t) if p]
    if not parts:
        return None
    return re.compile(r"(?<![A-Za-z0-9])" + r"[\s-]+".join(parts) + r"(?![A-Za-z0-9])", re.I)


TERMS, RECALL = load_lexicon(os.path.join(root, ".claude", "docs", "pattern-lexicon.md"))
if not TERMS and not RECALL:
    # No vocabulary means no check. Say so rather than passing silently: a
    # check that quietly does nothing is worse than one that is absent.
    print("NOLEXICON|%s|" % slug)
    sys.exit(0)

text = open(art_path, encoding="utf-8", errors="replace").read()

# Sentence-ish split that keeps the offending line intact for quoting.
SENT = re.compile(r"(?<=[.!?])\s+|\n")
segments, pos = [], 0
lineno = 1
for line in text.splitlines(True):
    for seg in SENT.split(line):
        if seg.strip():
            segments.append((lineno, seg.strip()))
    lineno += 1

# The override file: empty overrides everything, otherwise one term per line.
ov_path = os.path.join(design, slug, ".lexicon-override")
ov_all, ov_terms = False, set()
if os.path.isfile(ov_path):
    body = open(ov_path, encoding="utf-8", errors="replace").read()
    listed = [l.strip().lower() for l in body.splitlines()
              if l.strip() and not l.strip().startswith("#")]
    if listed:
        ov_terms = set(listed)
    else:
        ov_all = True

hits = []
for label, vocab in (("term", TERMS), ("recall", RECALL)):
    for t in vocab:
        rx = term_regex(t)
        if not rx:
            continue
        for ln, seg in segments:
            if rx.search(seg):
                hits.append((ln, seg, t, label))

# Deduplicate: one entry per (term, line).
seen, uniq = set(), []
for h in hits:
    k = (h[0], h[2])
    if k not in seen:
        seen.add(k)
        uniq.append(h)
uniq.sort()

blocked = [h for h in uniq if not (ov_all or h[2].lower() in ov_terms)]
overridden = [h for h in uniq if (ov_all or h[2].lower() in ov_terms)]

# --- the hit log: the only calibration input either mechanism has ----------
log_dir = os.path.join(root, ".squad", "log")
log_path = os.path.join(log_dir, "lexicon-hits.md")
if uniq:
    try:
        os.makedirs(log_dir, exist_ok=True)
        fresh = not os.path.isfile(log_path)
        with open(log_path, "a", encoding="utf-8") as fh:
            if fresh:
                fh.write(
                    "# Lexicon hits\n\n"
                    "Append-only. Every hit `lexicon-check.sh` finds in `01-track-a.md`,\n"
                    "and every one a user overrode. This is the curator’s only calibration\n"
                    "input for `.claude/docs/pattern-lexicon.md`: recurring BLOCKED entries\n"
                    "argue for neighbouring terms, recurring OVERRIDDEN entries argue for\n"
                    "narrowing or deleting the term that produced them.\n\n"
                    "Never rotated. A hit log that ages out takes the evidence for\n"
                    "narrowing a term with it.\n\n"
                    "| when | slug | artifact | line | kind | term | outcome | sentence |\n"
                    "|---|---|---|---|---|---|---|---|\n")
            now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
            rel = os.path.relpath(art_path, root)
            for ln, seg, t, label in uniq:
                outcome = "OVERRIDDEN" if (ov_all or t.lower() in ov_terms) else "BLOCKED"
                cell = seg.replace("|", "\\|").replace("\n", " ")
                if len(cell) > 300:
                    cell = cell[:297] + "..."
                fh.write("| %s | %s | `%s` | %d | %s | `%s` | %s | %s |\n"
                         % (now, slug, rel, ln, label, t, outcome, cell))
    except OSError:
        pass

if not blocked:
    sys.exit(0)

quoted = []
for ln, seg, t, label in blocked[:12]:
    quoted.append("  L%d  [%s: %s]\n      %s" % (ln, label, t, seg))
more = "\n  ... and %d more." % (len(blocked) - 12) if len(blocked) > 12 else ""

print("BLOCK|%s|%s" % (slug, os.path.relpath(art_path, root)))
print("\n".join(quoted) + more)
' 2>/dev/null || true)"

[ -z "$reason" ] && exit 0

head_line="$(printf '%s' "$reason" | head -n1)"
IFS='|' read -r verdict slug artifact <<<"$head_line"

if [ "$verdict" = "NOLEXICON" ]; then
  cat >&2 <<EOF
⚠️  lexicon-check: the lexicon has no terms and no recall grammar, so nothing
was checked in ${slug}. The check is wired but toothless.

Restore .claude/docs/pattern-lexicon.md before treating Track A's artifact as
screened. A check that quietly does nothing is worse than one that is absent,
which is why this warns rather than passing in silence.
EOF
  exit 0
fi

body="$(printf '%s' "$reason" | tail -n +2)"

cat >&2 <<EOF
🚫 01-track-a.md names patterns, or reaches for the grammar of recall.

  Artifact: ${artifact}

${body}

Track A derives from the problem's structure. A named pattern is a recalled
solution wearing a derivation, and convergence cannot tell the difference from
the outside — which destroys the only thing that phase exists to do.

Rewrite the offending sentences. Say what the structure *requires* and what
property makes the candidate work, not what the shape is called. If you cannot
restate a candidate without naming it, that is the finding: it was recalled.

--- If this is a false positive ---

A scope legitimately about state machines will trip a check looking for
"state machine". The override exists for exactly that, and it is logged rather
than hidden:

  touch .squad/design/${slug}/.lexicon-override            # override every term
  echo 'state machine' >> .squad/design/${slug}/.lexicon-override   # or just one

Then re-run the phase. Every hit and every override is appended to
.squad/log/lexicon-hits.md, which is the curator's only evidence for narrowing
or dropping a term. Overriding is how the check gets better; working around it
silently is how it gets deleted.

The override is the user's call, not yours. Report the hits and stop.

See .claude/docs/pattern-lexicon.md.
EOF
exit 2
