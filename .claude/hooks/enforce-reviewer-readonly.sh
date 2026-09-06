#!/usr/bin/env bash
#
# PreToolUse hook for Write | Edit | MultiEdit | NotebookEdit.
#
# Three agents may not write the thing they judge. Each of them must still
# write its own report, so the restriction is about an OBJECT rather than
# about a CAPABILITY, and that is why it lives in a hook:
#
#   reviewer-blind       .squad/design/<slug>/08-review-blind.md
#   reviewer-reconcile   .squad/design/<slug>/09-review-verdict.md
#                        .squad/decisions/inbox/**
#                        .claude/agent-memory/reviewer-reconcile/**
#   scope-warden         .squad/design/<slug>/00-warden.md
#
# Everything else is refused — for the warden that means `00-scope.md` above
# all, the artifact it exists to check.
#
# ---------------------------------------------------------------------------
# .squad/.last-review-verdict IS DENY-BY-DEFAULT FOR EVERY AGENT, NOT
# ALLOW-LISTED FOR ANY — architect ruling
# arch-verdict-cache-and-gate-classification (INV-5, "single writer") plus
# security-expert's T-013 (docs/security/threat-model.md, Trust Boundary 6):
# `reviewer-reconcile` used to be allow-listed to write this file directly,
# and every OTHER agent fell through this hook's passthrough default
# (`allowed = ALLOW.get(agent); if allowed is None: sys.exit(0)`) untouched,
# so 17 of 19 agents could write the framework's only commit-gate token via
# `Write`/`Edit` alone -- `reviewer-blind` and `scope-warden` were already
# refused by the ALLOW table below and never could. Both problems are fixed
# the same way: the check below for THIS ONE PATH runs unconditionally,
# before the per-agent ALLOW-list logic, and applies to every `agent_type`
# including ones this hook does not otherwise govern at all.
# `.squad/.last-review-verdict` now has exactly one writer in the whole
# framework: `scribe-decision-merger.sh`, a `SubagentStop` hook that runs
# outside any tool call this hook (or any Write/Edit-mediating hook) can
# see. That single-writer claim holds outside an active CI-6 shadow
# window; while `.squad/.gate-shadow` is active, this check logs and
# allows a non-exempt agent's write here instead of refusing it -- not
# enforcing today, said deliberately -- and goes back to refusing the
# moment the shadow lapses.
#
# What this closes and what it does not, stated as an object, not a
# capability (T-013's own framing) — and stated as what the MATCHER
# delivers, not what would be nice to claim: a wording like "no agent can
# write this path", or the refusal's own "no tool call may write this
# path, ever", both overclaim what a textual `os.path.normpath` match can
# deliver. What this hook actually refuses: a
# `Write`/`Edit`/`MultiEdit`/`NotebookEdit` whose `file_path` LEXICALLY
# NORMALISES, OR REALPATH-CANONICALISES, to `.squad/.last-review-verdict`,
# for every `agent_type` this hook fires for, listed or not —
# `candidates()` below checks both the literal path-spelling variants
# (relative, `./`-prefixed, absolute, `..`-traversal) and the
# `os.path.realpath`-resolved form, so a symlink FILE pointing at the
# cache and a symlink DIRECTORY standing in for `.squad` both refuse too.
# Falsifier: reverting the `os.path.realpath` resolution in
# `candidates()` and re-firing either symlink shape must exit 0 again.
# EXPLICITLY NOT COVERED: a HARDLINK to the cache (a distinct directory
# entry sharing an inode, not a symbolic reference — there is nothing in
# the path text for `realpath` to follow) and a case-spelling variant on a
# case-insensitive filesystem (this matcher is case-sensitive; it does not
# fold). Both remain open and are tracked as a residual pending
# security-expert's `T-` row — docs/security/threat-model.md, Trust
# Boundary 6, T-017 (ledger pointer: refutations.md entry 10) — named here
# rather than silently claimed closed.
#
# What remains open, unscheduled, named rather than silently passed:
# the `Bash` redirection channel (`echo ... > .squad/.last-review-verdict`)
# for the 9 agents holding `Bash` is NOT closed by this hook — `Write`/`Edit`
# are the only tools it mediates. `enforce-review-verdict.sh`'s Q2
# classification stages gate `git`/`gh` verbs, not arbitrary shell
# redirections, and closing that channel is T-013's fix direction (3), an
# unscheduled follow-on referencing T-010's precedent, not attempted here.
# The claim this hook can make is therefore "no TOOL-MEDIATED write except
# the merger's, for every path spelling this matcher can canonicalise", not
# "no write" — object-not-capability, same discipline as the rest of this
# file's header. That claim itself holds only outside an active CI-6
# shadow window: while `.squad/.gate-shadow` is active, a non-exempt
# agent's tool-mediated write here is logged and allowed rather than
# refused -- not enforcing today, said deliberately -- see the shadow
# logic below.
#
# This is an invariant, not a constraint, and it has to be, twice over.
# `reviewer-reconcile` declares `memory: project`, which enables Read, Write
# and Edit regardless of its `tools:` line, so the absence of a tool cannot be
# relied on. And `scope-warden` needs `Write` to produce its own report at all,
# so the absence of the tool would break it rather than confine it. The check
# goes on the call in both cases.
#
# The rule for the reviewers is "cannot fix what it finds" — the review stays a
# verdict rather than a negotiation. The rule for the warden is the same shape:
# a checker that can fix what it finds becomes a co-author, and then nobody is
# checking.
#
# Both were once written as claims about a capability — "cannot write source"
# stated as "cannot write", "cannot write what it checks" stated as "cannot
# write at all" — and both broke. A claim about an object survives a widened
# grant, because a hook can confine the capability to that object. See
# .claude/docs/flow-changelog.md (2026-09-04) -- and §1 of the flow
# specification, which is maintained outside this repository.
#
# NOTE — the file name is now narrower than the contents. It covers the two
# reviewers and the scope-warden. Renaming it would touch settings.json and
# the hook table in the flow specification, so the name is left alone and
# the mismatch is recorded here instead of being fixed quietly.
#
# Identity comes from `agent_type` in the payload, which Claude Code
# populates when a hook fires inside a subagent. Any other agent passes
# through untouched.
#
# Paths resolve against the `cwd` field in the payload, never against
# ${CLAUDE_PROJECT_DIR}: CLAUDE_PROJECT_DIR stays at the project root where
# the session started while cwd is the worktree root, so a hook rooted at
# CLAUDE_PROJECT_DIR silently stops matching inside a worktree — and still
# passes any test that has no worktree in it.
#
# SQUAD_NOW: an ISO YYYY-MM-DD date
# that overrides the wall clock used by the CI-6 shadow logic below
# (_shadow_active/_shadow_log) -- TEST-ONLY, and honoured only when
# SQUAD_TEST_HARNESS=1 is ALSO set, which only tests/blindness.sh exports.
# Its one weakening direction is real: set to a date BEFORE a shadow's
# expires:, it revives a lapsed shadow, defeating CI-6 constraint 2's
# auto-flip-to-enforcing. Reachability outside a harness that sets both
# variables is nil (this hook runs in the host process reading its own
# environment, not an arbitrary agent shell), which is why this is
# documented rather than removed -- but an env var that can un-expire a
# security control belongs where a maintainer will find it, not only in
# two source files.
#
# Exit codes:
#   0 — allow
#   2 — block (Claude sees stderr as the reason)
#
# Reads the standard Claude Code hook payload on stdin:
#   {
#     "agent_type": "reviewer-reconcile",
#     "cwd": "/path/to/checkout-or-worktree",
#     "tool_name": "Edit",
#     "tool_input": { "file_path": "..." }
#   }

set -euo pipefail

payload="$(cat)"

reason="$(printf '%s' "$payload" | python3 -c '
import datetime, json, os, re, sys

# The three-agent object confinement. .squad/.last-review-verdict is
# DELIBERATELY not in any of these lists any more -- it is governed
# unconditionally below, for every agent, before this table is even
# consulted. See the header for why (INV-5 / T-013).
ALLOW = {
    "reviewer-blind": [
        ".squad/design/*/08-review-blind.md",
    ],
    "reviewer-reconcile": [
        ".squad/design/*/09-review-verdict.md",
        ".squad/decisions/inbox/**",
        ".claude/agent-memory/reviewer-reconcile/**",
    ],
    "scope-warden": [
        ".squad/design/*/00-warden.md",
    ],
}

# The one path that is deny-by-default for EVERY agent, listed or not.
VERDICT_CACHE_PATTERN = ".squad/.last-review-verdict"

# CI-6 shadow period, scoped to ONLY the verdict-cache deny above.
# Before this shadow existed, the cache path was allowed for `reviewer-reconcile`
# (explicitly allow-listed) and for every agent NOT in ALLOW at all (the blanket
# `if allowed is None: sys.exit(0)` passthrough below). It was already refused
# for `reviewer-blind`/`scope-warden` -- both ARE governed by ALLOW and neither
# list ever named the cache. So "the previous control would also have
# allowed it" (CI-6 constraint 1) reduces to: agent is anything OTHER than
# reviewer-blind/scope-warden. Those two are refused unconditionally below,
# shadow or not -- the shadow must never touch a refusal the old control also
# produced.
SHADOW_EXEMPT_AGENTS = ("reviewer-blind", "scope-warden")


def _shadow_active(cwd):
    """Returns True iff .squad/.gate-shadow (resolved at the payload cwd,
    per-tree like the cache it shadows) exists with a well-formed `created:`
    and at most one appended `expires:` extension (two `expires:` lines
    total, never more), the FIRST `expires:` no more than 14 days after
    `created:`, any extension no more than 14 days beyond the FIRST
    `expires:`, the total span no more than 28 days, `created:` strictly
    before the first `expires:`, and the LAST `expires:` strictly in the
    future. Missing file, a missing/malformed date, a third `expires:`
    line, a non-positive initial span, or any span over its ceiling all
    return False -- a missing or malformed shadow means NOT shadowing, i.e.
    enforcing. `SQUAD_NOW` (an ISO `YYYY-MM-DD` date) overrides the wall
    clock so this is testable without waiting.

    An appended `expires:` line (below the first, per the documented
    extension procedure in that file) is the LAST line collected, not the
    first -- an implementation that stops at the first `expires:` match
    silently ignores every extension appended below it. Falsifier: a
    shadow file with `expires: 2026-09-19` then `expires: 2026-10-03`,
    `SQUAD_NOW=2026-09-25`, must be active (the extension honoured), not
    lapsed (the first date used).

    Three further falsifiers, one per check below, each independently
    exercised in tests/blindness.sh:
      - initial ceiling: `created: 2026-09-01` with a single `expires:`
        more than 14 days later must be enforcing (False) -- an
        un-extended shadow must not reach the 28-day TOTAL ceiling on its
        own; checking only the total let it run to twice the initial
        maximum.
      - extension count: three `expires:` lines (created + two appended
        extensions), every adjacent gap individually <= 14 days, must be
        enforcing (False) -- "at most one extension" is a COUNT, not a
        gap check, and a gap-only check cannot see a second extension
        appended within the ceiling.
      - date ordering: `created:` on or after the first `expires:` must be
        enforcing (False) -- a non-positive or negative span is malformed,
        not a long-lived shadow, and an unsigned day-count comparison
        alone cannot distinguish the two."""
    shadow_path = os.path.join(cwd, ".squad", ".gate-shadow")
    try:
        with open(shadow_path, "r", encoding="utf-8", errors="replace") as fh:
            lines = fh.read().splitlines()
    except OSError:
        return False

    created = None
    expires_values = []
    for line in lines:
        m_created = re.match(r"^\s*created:\s*(.*?)\s*$", line)
        if m_created and created is None:
            created = m_created.group(1)
        m_expires = re.match(r"^\s*expires:\s*(.*?)\s*$", line)
        if m_expires:
            expires_values.append(m_expires.group(1))

    if not expires_values or not created:
        return False

    if len(expires_values) > 2:
        # "The user may extend once" is a COUNT: created: plus one initial
        # expires: plus at most one appended extension is two expires:
        # lines maximum. A third means more than one extension was
        # appended, regardless of how small each individual gap is.
        return False

    try:
        created_date = datetime.date.fromisoformat(created)
        first_expires_date = datetime.date.fromisoformat(expires_values[0])
        last_expires_date = datetime.date.fromisoformat(expires_values[-1])
    except ValueError:
        return False

    if first_expires_date <= created_date:
        # A shadow whose (first) expires: does not strictly follow
        # created: is malformed -- never honour a non-positive or
        # negative span (an unsigned days-over-ceiling check alone passes
        # a negative span, since it is never "over" anything).
        return False

    if (first_expires_date - created_date).days > 14:
        # The INITIAL ceiling -- created: to the FIRST expires: -- is 14
        # days on its own, independent of whether an extension exists.
        return False

    if (last_expires_date - first_expires_date).days > 14:
        return False
    if (last_expires_date - created_date).days > 28:
        return False

    now_date = _squad_now()
    return last_expires_date > now_date


def _squad_now():
    """SQUAD_NOW is a test-only clock override for the shadow window above
    -- honoured ONLY when
    SQUAD_TEST_HARNESS=1 is ALSO set, which only the test suite in this
    repository (tests/blindness.sh) exports. Outside a harness that sets
    both, SQUAD_NOW is ignored and the real wall clock governs, so a
    stray SQUAD_NOW left in an agent or a maintainer shell cannot
    revive a lapsed shadow. See the file header for the full account of
    what this variable is and its one weakening direction."""
    if os.environ.get("SQUAD_TEST_HARNESS") == "1":
        now_raw = os.environ.get("SQUAD_NOW")
        if now_raw:
            try:
                return datetime.date.fromisoformat(now_raw)
            except ValueError:
                pass
    return datetime.date.today()


def _shadow_log(cwd, agent, tool, target, session_id):
    """.squad/log/gate-shadow.md is the ONLY audit trail of every decision
    the CI-6 shadow allowed instead of refusing -- who wrote the
    commit-gate token, when (to the second, in UTC, not merely the day),
    in which session, and with what result, during a window when the
    write-side deny was not enforcing. CI-6 treats shadow-period data as
    the thing that makes shadowing safe; gitignored, rotatable, or
    coarse-grained data is observation in name only, and a log with no
    session id cannot separate two agents shadow-allowed the same day.

    Best-effort, deliberately: an `OSError` here (a read-only filesystem,
    a `.squad/log/` the process cannot create) is swallowed rather than
    turned into a second refusal stacked on top of the one this call site
    is choosing NOT to issue -- the whole purpose of the shadow is to
    allow during its window, so a logging failure must not silently
    become a refusal. Falsifier: an `OSError` raised inside the
    `with open(...)` block must not propagate past this function, and the
    exit(0) at the call site must still run.

    Unlike every other file under .squad/log/, this one is TRACKED (see
    the .gitignore `!.squad/log/gate-shadow.md` negation, alongside
    lexicon-hits.md and pass-cost.md, both tracked for the same reason --
    durable evidence a clone or a rotation must not discard)."""
    log_path = os.path.join(cwd, ".squad", "log", "gate-shadow.md")
    try:
        os.makedirs(os.path.dirname(log_path), exist_ok=True)
        stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        with open(log_path, "a", encoding="utf-8") as fh:
            fh.write(
                "%s SHADOW-ALLOW agent=%s tool=%s target=%s session=%s "
                "result=allow (would refuse once the shadow at "
                ".squad/.gate-shadow expires)\n"
                % (stamp, agent or "<unlisted>", tool, target, session_id or "<none>")
            )
    except OSError:
        pass


try:
    d = json.loads(sys.stdin.read())
except Exception:
    sys.exit(0)

tool = (d.get("tool_name") or "").strip()
if tool not in ("Write", "Edit", "MultiEdit", "NotebookEdit"):
    sys.exit(0)

agent = (d.get("agent_type") or "").strip()
cwd = d.get("cwd") or os.getcwd()


def to_regex(pat):
    out, i = "", 0
    while i < len(pat):
        if pat.startswith("**", i):
            out += ".*"
            i += 2
        elif pat[i] == "*":
            out += "[^/]*"
            i += 1
        else:
            out += re.escape(pat[i])
            i += 1
    return re.compile("^" + out + "$")


def _suffixes(path):
    parts = path.strip("/").split("/")
    return ["/".join(parts[i:]) for i in range(len(parts))]


def candidates(raw):
    p = os.path.normpath(os.path.join(cwd, os.path.expanduser(raw)))
    out = []
    rel = os.path.relpath(p, cwd)
    if not rel.startswith(".."):
        out.append(rel)
    out.extend(_suffixes(p))

    # CONFIRMED BY EXECUTION: a symlink FILE pointing at the cache and a
    # symlink DIRECTORY standing in for .squad both exited 0 against the
    # textual candidates above alone, before this resolution existed.
    # os.path.realpath resolves both -- it walks the path component by
    # component and follows any symlink it finds, and tolerates a leaf that
    # does not exist yet (exactly the case for a Write that has not
    # happened). It does NOT resolve a hardlink (a distinct directory entry
    # sharing an inode, not a symbolic reference -- there is nothing in the
    # path text to follow) and it does NOT fold case -- both left open,
    # named in the header, tracked as a residual (threat-model.md T-017;
    # ledger pointer refutations.md entry 10).
    real_p = os.path.realpath(p)
    if real_p != p:
        real_cwd = os.path.realpath(cwd)
        real_rel = os.path.relpath(real_p, real_cwd)
        if not real_rel.startswith(".."):
            out.append(real_rel)
        out.extend(_suffixes(real_p))
    return out


target = ""
ti = d.get("tool_input") or {}
for key in ("file_path", "notebook_path", "path"):
    v = ti.get(key)
    if isinstance(v, str) and v:
        target = v
        break

if not target:
    sys.exit(0)

cands = candidates(target)

# Deny-by-default for the verdict cache -- runs BEFORE the per-agent table,
# for EVERY agent_type this hook fires for, including ones the table below
# has never heard of. Object-not-capability: this is a rule about the file,
# not about who is asking.
verdict_rx = to_regex(VERDICT_CACHE_PATTERN)
if any(verdict_rx.match(c) for c in cands):
    # CI-6 shadow, constraint 1: never for a decision the previous control
    # already refused. reviewer-blind/scope-warden were refused before this
    # fix in this round too (see SHADOW_EXEMPT_AGENTS above) -- they stay
    # refused unconditionally, shadow or not.
    if agent not in SHADOW_EXEMPT_AGENTS and _shadow_active(cwd):
        _shadow_log(cwd, agent, tool, target, d.get("session_id"))
        sys.exit(0)
    print("VERDICT|%s|%s|%s" % (agent or "<unlisted>", tool, target))
    sys.exit(0)

allowed = ALLOW.get(agent)
if allowed is None:
    sys.exit(0)

# The ALLOW side must match the CANONICALISED destination, not merely a
# literal spelling that happens to match -- the same realpath treatment
# the cache deny above already gets. `candidates()` deliberately drops any
# realpath-resolved candidate that escapes this repository (see the
# comment there: that exclusion is what lets an out-of-repo target never
# accidentally satisfy the CACHE deny), which leaves only the literal
# in-repo name for THIS check to see when a governed agent target is a
# symlink pointing outside the repo -- and the literal name alone is
# enough to match an ALLOW pattern. Falsifier: a symlink named exactly
# one of the allowed artifact names (e.g. `09-review-verdict.md` under
# `.squad/design/*/`) pointing outside the repository must refuse a
# governed agent write through it, even though the literal path matches
# the pattern; the same symlink pointing at an in-repo target must stay
# allowed (tests/blindness.sh exercises both directions).
real_target = os.path.realpath(os.path.join(cwd, os.path.expanduser(target)))
real_cwd = os.path.realpath(cwd)
real_target_rel = os.path.relpath(real_target, real_cwd)
target_escapes_repo = (real_target_rel == os.pardir
                        or real_target_rel.startswith(os.pardir + os.sep))

RX = [to_regex(p) for p in allowed]
if not target_escapes_repo and any(rx.match(c) for rx in RX for c in cands):
    sys.exit(0)

print("OWN|%s|%s|%s|%s" % (agent, tool, target, "; ".join(allowed)))
' 2>/dev/null || true)"

[ -z "$reason" ] && exit 0

kind="${reason%%|*}"
rest="${reason#*|}"

if [ "$kind" = "VERDICT" ]; then
  IFS='|' read -r agent tool target <<<"$rest"
  cat >&2 <<EOF
🚫 .squad/.last-review-verdict has exactly one writer in this framework:
scribe-decision-merger.sh (a SubagentStop hook, not a tool call).

  Agent:  ${agent}
  Tool:   ${tool}
  Target: ${target}

An agent writing this file is forging a verdict. This is deny-by-default for
every agent, listed or not — it is that no Write/Edit/MultiEdit/NotebookEdit
whose file_path lexically normalises or realpath-canonicalises to this name
is permitted outside an active CI-6 shadow window, not that ${agent} lacks
permission today. (While \`.squad/.gate-shadow\` is active, a non-exempt
agent's write here is logged and allowed instead of refused -- not
enforcing today, said deliberately; this message only prints when no
shadow is active or ${agent} is shadow-exempt.) (This governs
a path spelling, not the file itself: a hardlink to this file, or a case
variant on a case-insensitive filesystem, is not covered by this check --
see docs/security/threat-model.md, Trust Boundary 6, T-017 — ledger
pointer refutations.md entry 10.) The cache reflects a review that actually
happened; a direct write is the review that didn't.

If reviewer-reconcile has just finished, its decision drop
(.squad/decisions/inbox/**) is what the merger reads to derive this file —
write the drop, not the cache.

See .claude/docs/principles-enforcement.md (Missing Review Lockout) and
docs/security/threat-model.md (Trust Boundary 6, T-013).
EOF
  exit 2
fi

IFS='|' read -r agent tool target allowed <<<"$rest"

cat >&2 <<EOF
🚫 ${agent} may not write outside its own outputs.

  Tool:   ${tool}
  Target: ${target}

You cannot fix what you find. That is what keeps a review a verdict rather than
a negotiation, and a scope check a check rather than a co-authorship — an agent
that patches a problem on its way past has stopped reporting it.

You may write only:
  ${allowed}

Record the finding in your artifact and let whoever owns the artifact fix it.
You write your report; you do not write the thing you are reporting on.

See .claude/agents/${agent}.md and .claude/docs/principles-enforcement.md.
EOF
exit 2
