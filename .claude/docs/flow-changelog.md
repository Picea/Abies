# Flow changelog

What changed in the agent topology, when, and **which enforcement level each
change moved between**. The levels are the three from
`principles-enforcement.md`: instruction, invariant, constraint.

The level column is the point of this file. A change that moves a rule from
instruction to invariant is a different kind of event from one that reworks
prose, and the difference is invisible in a diff. Entries that record no level
movement are observations about the topology rather than changes to it — those
are marked **observation** and are kept because the design makes claims about
itself that only running it can test.

Newest first.

---

## 2026-09-06 — Observation: `docs/adr/` is reachable by Track A and not on its deny list

**Level movement:** none. Spec/implementation gap, flagged for its own review
pair after this pass rather than fixed inline.

`docs/adr/ADR-008-immutable-state.md:85` is reachable by
`dreamer-first-principles` as ordinary codebase reading —
`enforce-track-blindness.sh`'s `DENY` list for that agent does not cover
`docs/adr/`. The flow specification lists ADRs among the paths denied to
Track A; the hook does not enforce that line.

Recorded generically here; `security-expert` is registering the same gap in
`.claude/enforcement/refutations.md` in parallel. Closing it is out of scope
for the `undo-redo` pass.

---

## 2026-09-06 — Observation: gate 1 caught two prior-work leaks a human review passed

**Level movement:** none. This is evidence for gate 1, not a change to it.

The pre-hook draft `.squad/design/undo-redo/00-scope-undo-redo.md` was
reviewed by a human and passed. `scope-warden`, reading `00-warden.md`'s
category — prior work presented as reference material, the one no regex
reaches — found two leaks in it: a sentence presenting the existing
debug-only machinery as "the starting point," and a sentence referring to
"the well-known conventional answer" as though a comparison shape were
already decided.

Neither leak is a decision id, a pattern name, or a denied path — the three
things the mechanical scan already covers. Both are prose asserting a
conclusion before Track A has had the chance to reach one. The final scope
corrects both; the draft has been removed by `git rm` and is not being
dispatched.

This is what gate 1's mechanical-plus-judgement split is for: the mechanical
scan is exhaustive over what it can name, and the category it cannot name is
exactly the one that got past a human reader.

---

## 2026-09-06 — Interpreter-unavailable polarity: *claimed* fail-closed → actual fail-closed

**Level movement:** *claimed* invariant → **actual invariant**. No new rule —
every hook's header already asserted fail-closed on a malformed payload. What
moved is whether that held when `python3` itself was the thing missing.

`09-review-verdict.md` round 1, finding 🔴-4: 18 of 19 hooks piped their
payload through `python3 -c '…' 2>/dev/null || true`, which swallows a
non-zero interpreter exit the same way it swallows a parse error — an
interpreter that is missing, wrong-versioned, or broken by an edit produced
empty output, indistinguishable from "allow." `scribe-decision-merger.sh`
already carried a `command -v timeout` guard for the identical reason; the
other 17 scripts did not check for their interpreter at all. Closed by adding
a shared `command -v python3` preamble that exits 2 rather than falling
through, and dropping the `|| true` so a non-zero interpreter exit is no
longer indistinguishable from a clean empty match.

---

## 2026-09-04 — Observation: the split reviewer corrected itself in both directions

**Level movement:** none. This is evidence, not a change.

The first full run of the split reviewer on a real change produced something
the design predicted but had never demonstrated.

### What happened

`reviewer-blind` reviewed a staged change with no access to `.squad/design/`,
raw `git log`, `gh pr view` or any narrative about why the change existed. It
described eight problems. `reviewer-reconcile` then read that artifact first,
then the author's narrative as claims to verify, and issued
`NEEDS-CHANGES` with three blockers.

**Two of the corrections travelled from the reconciling context back into the
blind one:**

1. **A case-sensitivity error.** `reviewer-blind` reported that
   `dreamer-first-principles` is not told to avoid ranking against Track B. Its
   grep was case-sensitive; the instruction is at
   `agents/dreamer-first-principles.md:43` — *"Do not rank the candidates
   against Track B's."* The blind finding was false and the reconciling pass
   said so, with the line number.
2. **Right claim, wrong evidence.** `reviewer-blind` reported that the new
   artifact-contract assertions were weak, and named which rows it thought
   would survive deletion. Its examples were wrong — the rows it named *do*
   fail. The rows that survive deletion silently are Realist and Critic, which
   it had not tested. The general claim was correct and understated; the
   reconciling pass established it by mutation-testing every row rather than
   reasoning about the greps.

### Why this is worth a changelog entry

The split is justified in §1 by an argument about anchoring: an agent that has
read the author's narrative reviews the narrative. **That argument is theory,
and it only ever claimed a benefit in one direction** — protecting the blind
reading from the narrative.

What was observed is the reverse direction working as well. The reconciling
context has something the blind one structurally lacks — the ability to check a
finding against the record — and it used that to *correct* the blind pass rather
than only to soften or accept it. Two independent readings produced a result
neither would have produced alone, and the correction flowed the way the design
did not specifically promise.

### The blocker that matters more

The third blocker was that `scope-warden` is documented as writing
`00-warden.md` while declaring `tools: Read, Grep, Glob` and no `memory:` — a
section titled "Why You Cannot Write" sitting twenty-five lines above an
instruction to write a file.

**That contradiction survived:**

- the implementation session that created it,
- the session report that enumerated the agent's tools,
- a line-by-line audit of the repository against the specification, and
- the specification itself, which asserted **both halves** of it — §8 said the
  warden cannot write at all, and §3 and §4.3 required its report.

A checklist cannot catch a checklist that contradicts itself. Every one of those
four passes was reading a list, and the list was wrong in both places
consistently, so nothing disagreed with anything. `reviewer-blind` caught it
because it was reading the **artifact** — the charter as a document that has to
make sense on its own terms — with no list in hand to check against.

That is the strongest evidence this system has produced about itself, and it
argues for something narrower than "reviews are good": **an agent that has to
form its own reading of a document will notice the document contradicting
itself, and an agent checking the document against a specification will not, if
the specification carries the same contradiction.**

### What changed as a result

Recorded in the flow specification — `squad-flow-reference-v2.1.md`, maintained
outside this repository — rather than here: §1 gained the rule that a
restriction is written as a claim about an **object**, not about a **capability**
— *cannot write what it judges*, not *cannot write*. §8 and §9 were corrected
accordingly, moving `scope-warden`'s write restriction from constraint 7 to
invariant 9, and §0 gained the note that a detector feeding a human gate has no
name on this three-level scale.

---

## 2026-09-04 — `scope-warden` write restriction: constraint → invariant

**Level movement:** constraint → **invariant**.

`scope-warden` gains `Write`, confined by hook to
`.squad/design/*/00-warden.md`. Everything else, including `00-scope.md`, is
denied.

**The reason the old constraint was wrong.** Its justification —
*"a checker that can fix what it finds becomes a co-author, and then nobody is
checking"* — is a claim about **one object**: the scope. The constraint was
written about a **capability**: writing at all. Writing its own report is not
fixing what it found, so the constraint was broader than its own reason, and
the extra breadth made the agent unable to produce the artifact gate 1 depends
on.

This is the second instance of the same defect in one audit. The first was the
reviewer: *cannot write source* stated as *cannot write*, which `memory:
project` handed back. A claim about an object survives a widened grant, because
a hook can confine the capability to that object. A claim about a capability is
only as true as the grant.

---

## 2026-09-04 — Hook wiring: unverified → checked

**Level movement:** the **evidence** for every hook-backed invariant changed
from absent to present. No rule moved; what moved is whether anything would
notice if the rule stopped being wired.

Every assertion in the suite invokes its hook script directly. None went
through `settings.json`. So an unwired hook tested identically to a wired one,
and a review proved it rather than supposing it: **dropping all twelve
`PreToolUse` entries left the suite at 261 passed, 0 failed.** Dropping the
whole `SubagentStop` event, dropping the blindness hook alone, and repointing
`enforce-reviewer-readonly`'s matcher at `Read` were all green too.

That is every hook-backed invariant in section 9 — including invariant 8, the
one confining both reviewers' writes, and invariant 9, the warden's. They were
enforced in the running system and unverified in CI, which is the gap between
a check and a check you can rely on continuing to exist.

The `wiring:` assertion family closes it, bidirectionally and without a list to
maintain: a script is expected to be wired unless its own header says `NOT A
HOOK`; every `settings.json` entry must name a file that exists; and the
enforcement hooks must appear at the right event under a matcher that covers
the tool they guard, because a hook on the wrong matcher is unwired with extra
steps. All four mutations now fail.

**The first version of this check inherited the defect it was written to
catch.** Two fail-opens, both found by the same review that found the gap:
`check_place` matched the matcher by substring, so `Edit` was satisfied by
`MultiEdit` and `Read` by `NotebookRead` — narrowing the readonly matcher to
`Write|MultiEdit` left the `Edit` assertion green. And the `NOT A HOOK`
exemption matched the phrase anywhere in a header, so adding one casual
sentence to a real hook exempted it and printed a green line affirming the
exemption. Six mutation shapes now fail where three did.

**The seventh shape was a duplicate pin.** Two `check_place` invocations were
byte-identical, so the artifact validator went unpinned for both dreamer tracks
and its matcher could be narrowed to `critic|spec-author` — dropping Track A's
artifact from validation — at 327 passed, 0 failed. The family read as 66
assertions and was 65 distinct. There is now an assertion that no two pins are
identical, which makes the next duplicate the suite's problem rather than a
reviewer's.

**Later rounds closed the eighth shape and three mechanism defects in the
check itself.** Every matcher alternative is pinned — 38 of 38, derived by
diffing `settings.json` against the pins in both directions rather than probed
shape by shape, which also catches a pin outliving its matcher. `grep -qx`
became `-qxF`, because a pin used as a *pattern* rather than a literal let
`mcp__.*` be narrowed to `mcp__filesystem` silently. The `ANY` sentinel now
requires the entry to have no matcher at all, so it cannot be used to weaken a
pin. And deletion is now loud for every entry: all 22 tested as a universal,
22 of 22 go red when a script is removed from disk and from `settings.json`
together.

**Alternative coverage is now derived rather than audited.** The 38-of-38
census above was a hand count, and a hand count is a snapshot: adding an
unpinned alternative to a matcher passed at 345/0. A `coverage:` assertion now
derives the requirement from `settings.json` — every alternative of every
matcher must have a pin — and both probes that passed it now fail.

That derivation has two sides, and only one of them was derived. The pins side
was a regex over this test file's own **source**, so a pin's *presence*
satisfied coverage whether or not it ran: moving all 38 into a never-called
function left every `coverage:` assertion green and deleted the entire
placement family. `check_place` now records each invocation and the executed
set is compared to the parsed set in both directions. **Only with that
comparison is the census a property rather than a property of text.**

**The limits that remain are in `hook-tests.yml`'s `does NOT check` list.**
That list is the register; this entry deliberately does not restate its length,
because restating it is how these two documents came to disagree twice — the
count was six, then five, and was written here as four. The list is kept even
where a limit is currently empty, because an empty limit returns the moment
someone adds an alternative or an entry without a pin.

**The one worth naming here: placement is not behaviour.**
Gutting a correctly-wired `enforce-no-secrets.sh` to `exit 0` passes with a
green suite, while gutting `enforce-reviewer-readonly.sh` gives **16
failures**. The failure count is the durable half and the only half recorded
here: every pass count written into this file has gone stale within a round,
including the pair that accompanied the finding which said so. Six git-guard
hooks
have a placement pin and no behavioural assertion. Naming that is not the same
as fixing it, so it has an owner rather than a bullet:
`.squad/learnings/inbox/design-pass-enforcement-layer.md`.

**A rule moved that is not 8 or 9.** *Every script in `.claude/hooks/` must be
wired unless its header declares `NOT A HOOK`* existed in no form before today
— nothing → invariant, skipping warning and constraint. It also makes a literal
string at `git-history-namestatus.sh:3` load-bearing; that file now says so in
its own header, because a marker nothing documents is a marker somebody
rewords.

**It was found by a sentence claiming the opposite.** A workflow comment said
the suite's roster and layout assertions catch wiring drift. They do not — no
assertion read `settings.json` for anything but `worktree.baseRef` and an
existence check. The claim was written, flagged for falsification, and
falsified in one pass. Writing down what you believe a check covers is how you
discover it does not.

---

## 2026-09-04 — Contract-drift assertions: green → actually checking

**Level movement:** none in the topology; the **evidence** for invariant 11
changed from unfounded to founded.

Twelve assertions claimed to check that every artifact appears in the design
pass's artifact-contract table. Nine of them matched prose elsewhere in the same
file and reported green when the row they were named for was deleted. Found by
mutation testing, not by reading.

An assertion that cannot fail on the mistake it names is worse than no
assertion, because it converts an unchecked claim into a checked-looking one.

There are now **thirteen artifacts across twelve rows** — row 1's Writes cell
holds both `00-scope.md` and `00-knowledge.md`, and a row for `00-warden-scan.md`
closed a drift between the contract table and `memory-policy.md` that the review
found. Each assertion targets the Writes column specifically.

Mutation testing, stated precisely because the rounded version of this sentence
was itself a finding: twelve rows under two mutation kinds is **24 distinct file
states**, each confirmed failing the right assertion. Deleting row 1 fails two
assertions together, so those two artifacts are not individually isolable by
either named kind; a third mutation — removing one artifact from the shared cell
and leaving the other — does isolate them, and was run.

**A later round added the reverse assertion**, which fails when the table names
an artifact the loop does not check. (The counts in the two paragraphs above are
prose. Nothing checks them, and the correct maintenance path — adding an
artifact to both the table and the loop — leaves them stale with a green suite.
Re-derive them if you take it.) That is what closed the real gap: the
forward check alone was one-directional, and the remedy originally shipped
alongside it was an instruction telling maintainers to update a count. An
instruction where a check is available is the defect this document keeps
recording; it should not have taken a review round to notice it in a change
about exactly that.

---

## 2026-09-03 — spec v2.1

The changes behind this document's current shape. Recorded per level:

| Change | Movement |
|---|---|
| Track A's read prohibitions | instruction → **invariant** (`enforce-track-blindness.sh`) |
| Track B's symmetric prohibition on `01-track-a.md` | instruction → **invariant** (same hook) |
| Pattern names in `01-track-a.md` | instruction → **invariant** (`lexicon-check.sh`, overridable, logged) |
| `00-knowledge.md` split out of `00-scope.md` | instruction → **constraint** |
| Track A's preloaded skill reduced to its own method | instruction → **constraint** (`beast-mode-track-a`) |
| `reviewer` split into `reviewer-blind` + `reviewer-reconcile` | instruction → **constraint** (no memory) + **invariant** (hooks) |
| Reviewer cannot write source | *claimed* constraint → **invariant** (`memory: project` had been granting Write all along) |
| Raw git history denied to `reviewer-blind` | — → **invariant** (`enforce-review-history-channel.sh`) |
| Phase order | instruction → **invariant** (`enforce-phase-order.sh`) |
| Phase artifact shape | — → **invariant** (`validate-phase-artifact.sh`) |
| Commit without a review verdict | instruction (Missing Review Lockout) → **invariant** (`enforce-review-verdict.sh`) |
| Scope free of ids and pattern names | — → **warning** (`scope-warden.sh` + gate 1; see §0 on why this scale has no name for it) |
| Invariant-to-property chain | — → **invariant** (validator + the critic's fourth gate) |
| Curator's self-counter-argument | instruction → separate context (`curator-adversary`) |
| Builders' edits | instruction → **invariant** (`isolation: worktree`) |

---

## How to add an entry

State the level movement first. If there is none, say so and mark the entry an
**observation** — those are worth as much, and sometimes more, because the
design's claims about itself are the ones nothing else tests.
