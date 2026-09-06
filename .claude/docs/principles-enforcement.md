# Principles Enforcement Directive

**This directive applies to every agent in the squad. No exceptions.**

## The Rule

**Every deviation from an established principle MUST be discussed with and approved by the user before proceeding.** No agent may silently compromise, work around, or "pragmatically adjust" any principle. If you cannot follow a principle as written, you stop and ask.

This is not a suggestion. This is a hard gate.

## The Three Levels, and Which One a Rule Deserves

Every rule in this framework sits at one of three levels. The level is not a
description of how seriously the rule is meant; it is a statement of what
actually holds it up.

| Level | What it means | What it takes to break |
|---|---|---|
| **Instruction** | An agent is told not to. | Forgetting. Or being told otherwise by something else in its context. |
| **Invariant** | Something checks, and refuses. | The check has to fail or be bypassed. The attempt is visible. |
| **Constraint** | The thing is not reachable. | It would have to be brought into existence first. |

Most multi-agent systems are built entirely at the instruction level: a long
prompt telling an agent what not to do. That works until the context grows, the
session runs long, or another instruction contradicts the first. The interesting
question is not whether an agent behaves — it is what happens on the seventh
pull request, when nobody is watching and the context is full.

### Choosing the level

Shigeo Shingo drew this line at Toyota in the 1960s, between a **warning**
method that detects an error and relies on the operator to correct it, and a
**control** method that halts the process. His rule for choosing: mistakes that
are easily correctable without danger or significant cost suit a warning;
everything else needs a control, **even when the resulting stoppage causes
delays** — because the delay costs less than an uncorrected error persisting.

Translated for this framework:

> **If the mistake is cheap to catch later, an instruction is enough. If it is
> not, it needs a check — even when the check will be annoying.**

**The test is not how *likely* the mistake is.** It is what it costs once made,
and whether it stays visible afterwards.

**The same mistake can deserve different levels at different points**, and the
framework already does this. Take a pattern name appearing where something
should have been derived:

| Where | Mechanism | What it does | Level |
|---|---|---|---|
| `00-scope.md` | `scope-warden.sh` | **Reports.** Always exits 0. The findings go to a person at gate 1, who decides. | a warning — see below |
| `01-track-a.md` | `lexicon-check.sh` | **Refuses.** Exits 2 and sends the artifact back for a rewrite. | invariant |

Same mistake, two mechanisms, deliberately. The difference is the cost of being
wrong about it. A false positive in the scope costs a rewrite of a document you
were about to approve anyway, and a real leak there is still in front of a human
who can catch it. A pattern name that reaches Track A's *output* has already
produced a design nobody can distinguish from a derived one — cheap to make,
invisible once made, and expensive at the end of the pass. **Those three
properties together are what buys a control rather than a warning.**

### One place this scale is coarser than Shingo's

A detector that feeds a human gate refuses nothing on its own. On the
three-level scale above it therefore lands under **instruction**, next to rules
with no mechanism at all — which understates it, because `scope-warden.sh` runs
on every pass and puts its findings in front of a person whether or not anyone
remembers to look.

Shingo's *warning* method is a real category and this scale has no name for it.
No fourth level has been added: it would make the inventory harder to argue with
rather than easier. The gap is named instead, here and in §0 of the flow
specification (maintained outside this repository; see
`.claude/docs/flow-changelog.md`),
and the inventory undercounts accordingly.

The practical consequence when you are choosing a level: **"it reports to a
human" is not the same as "something checks and refuses", and writing it in the
invariant column is the error this whole scale exists to prevent.**

Two corollaries, because both get argued about:

**A check that annoys people is not thereby wrong.** It is wrong if the mistake
it prevents was cheap. Shingo's answer to the delay objection is that the
stoppage is the cheaper of the two costs, and the argument has to be had in
those terms rather than in terms of friction. If you want a check removed, the
case to make is that its mistake is cheap — not that the check is irritating.

**An unenforceable rule is not thereby unimportant.** Some expensive mistakes
have no available check. The training data behind `dreamer-first-principles` is
the clearest one here: no tool grant reaches the weights. Those stay
instructions, and the honest move is to **name the gap rather than downgrade the
rule so the inventory looks tidier.**

Shingo also renamed his own concept, from fool-proofing to mistake-proofing, on
the principle that responsibility for an error lies with the design of the
system rather than with the person inside it. That transfers without
modification: when an agent does the wrong thing, the first question is what in
the design made it available.

### Who this section is for

- **`curator`** — a proposal must say which level it is asking for, and why the
  mistake is or is not cheap to catch later. A proposal that asks for a check
  without making that argument is incomplete.
- **`curator-adversary`** — "what does it cost" is a question about the level,
  not only about the rule. An instruction that should have been a check, and a
  check that should have been an instruction, are both findings.
- **`critic`** — a plan that relies on an agent remembering something expensive
  is a plan with an instruction where it needs an invariant.
- **`architect`** — when a design's correctness rests on a rule, say which level
  that rule sits at. "We will be careful about X" is an instruction, and should
  be recorded as one rather than assumed to be stronger.

## What Counts as a Deviation

A deviation is any action that contradicts, weakens, bypasses, or works around an established principle. This includes but is not limited to:

### Functional DDD Principles
- Using a mutable class where an immutable record belongs
- Using boolean flags or nullable fields instead of a state machine with distinct types per state
- Using null where `Option<T>` is required
- Using exceptions for expected business errors instead of `Result<T, TError>`
- Using primitive types where a constrained type (smart constructor) is warranted
- Exposing a public constructor on a constrained type, bypassing the smart constructor
- Using an enum + switch for state management where states carry different data
- Putting IO or side effects in domain functions instead of pushing them to the edges
- Using OO patterns (inheritance for behavior, mutable classes, Manager/Helper/Util) in the domain
- Leaking domain types into infrastructure (ORM attributes, JSON attributes on domain records)
- Violating aggregate boundaries

### Namespace & Architecture Principles
- Using namespaces as abbreviations instead of bounded contexts
- Folder structure not mirroring namespace declarations
- Misaligning project names with root namespaces
- Deviating from the Architect's namespace plan without discussion
- Introducing coupling between bounded contexts without an explicit ACL

### Coding Standards
- Prefixing your own interface names with "I"
- Using the `Async` suffix on async methods
- Using CommonJS or non-ES-module patterns (JS)
- Adding a framework (React, Vue, Angular) without Architect approval (JS)
- Adding a dependency that duplicates a BCL/platform capability
- Adding a build step without justification (JS)
- Skipping `AddServiceDefaults()` in an Aspire-hosted service
- Using `WebApplicationFactory` or Testcontainers instead of the Aspire AppHost for integration/E2E tests

### Security Principles
- Adding an endpoint without an authorization policy
- Using string concatenation for SQL or HTML output
- Hardcoding or committing secrets
- Skipping threat model update when the attack surface changed
- Disabling a security analyzer without documenting why

### Observability Principles
- Shipping a functional flow without OTEL traces
- Shipping a `dotnet new` template without observability wired up
- Missing custom `ActivitySource` spans on workflow entry points

### Architectural Cleanness
- Choosing a pragmatic shortcut over the architecturally clean solution
- Compromising for ergonomics or performance without demonstrating the need

### Boy Scout Rule
- Leaving code worse than you found it
- Touching a file and not improving it (rename a poorly named variable, extract a helper, add a missing type annotation, fix a stale comment, improve an error message)
- Ignoring existing code smells in files you're modifying

### Git Workflow
- Committing directly to `main` — locally or remotely. All changes go through feature branches and pull requests. No exceptions.
- Commit messages not following Conventional Commits format.
- Branch names not following the `<type>/<issue-number>-<short-slug>` convention.

### Dependency Policy
- Adding a NuGet or npm dependency without Security Expert SCA review.
- Adding a framework-level dependency without Architect approval.
- Adding a dependency that duplicates BCL/platform functionality.
- Adding a dependency without documenting the decision.

### Testing Principles
- Fixing a bug without adding a regression test that reproduces the original failure.
- Regression test must fail before the fix and pass after — proving the fix works and preventing recurrence.
- Beginning implementation of a new feature or behavior change without an approved Spec-by-Example test.
- Modifying the Spec-by-Example test during implementation without re-approval from the user.
- Skipping the Spec-by-Example phase for anything outside the documented skip list (pure refactoring with no behavior change, trivial config/doc changes, bug fixes).

### Code Review
- Marking any code-touching work as complete, ready-for-merge, or shippable without an explicit `reviewer-reconcile` verdict recorded.
- Merging or applying code changes that have not gone through both reviewers.
- A specialist self-reviewing their own code instead of handing off to the review pair.
- Bypassing the reviewers for "trivial" code changes — there is no such thing as a trivial code change for review purposes. Trivial changes still need a `reviewer-reconcile` verdict; they just review faster.
- The Lead approving any code-shaped change — see `CLAUDE.md` § 4 for the full, current list (kept in one place so it can't drift from `enforce-review-verdict.sh`'s `case` list). The Lead's lightweight-review authority is limited to true non-code: README/CONTRIBUTING/CHANGELOG prose, decisions in `decisions/inbox/`, code comments without logic changes, and `.md` documentation.
- An agent attempting to declare a code work item complete without `reviewer-reconcile`'s explicit verdict triggers the **Missing Review Lockout** (see protocol below).

## The Protocol

When any agent encounters a situation where a principle cannot be followed as written:

### Step 1: Stop
Do not proceed. Do not implement the deviation. Do not "fix it later."

### Step 2: Explain
State clearly:
- **Which principle** would be violated (name it exactly).
- **Why** you believe a deviation is necessary (concrete technical reason, not "it's easier").
- **What the deviation would look like** (show the code or design that would result).
- **What the principled approach would look like** (show the alternative that follows the principle).
- **What you'd lose** by following the principle strictly (performance numbers, ergonomic impact, complexity cost — be specific).

### Step 3: Wait
Wait for the user's explicit approval. Do not interpret silence as approval. Do not proceed on the assumption that "they'd probably agree."

> **One exception, and only one: the design-pass phase agents.** They cannot hold a conversation — each runs in an isolated context, returns once, and cannot be replied to. They flag the deviation in their artifact and in their summary, and the orchestrator surfaces it at the next 🛑. See **Phase agents** under Agent-Specific Enforcement. Everyone else stops and waits.

### Step 4: Document
If the user approves the deviation:
- Log the decision to `.squad/decisions/inbox/` with: the principle violated, the reason, the user's approval, and the date.
- Add a code comment at the deviation site referencing the decision: `// Deviation from [principle]: [reason]. Approved [date]. See decision D-NNN.`
- If the deviation is architecturally significant, create an ADR.

If the user rejects the deviation:
- Follow the principle as written. Find a way to make it work.

## Agent-Specific Enforcement

### Architect
During the Dreamer/Realist/Critic phases, if any candidate approach or plan element deviates from a principle, flag it explicitly in the phase output. Do not present a deviating approach as the recommended option without flagging the deviation and pausing for the user.

### C# Dev / JS Dev (Specialists)
During implementation, if you encounter a situation where following a principle creates a genuine technical problem, do not work around it. Stop, explain, and wait. The fact that a workaround is faster is not a valid reason to skip the protocol.

### Reviewers (`reviewer-blind`, `reviewer-reconcile`)
During code review, if you find code that deviates from any principle and there is no documented approval (decision log entry + code comment), it is a **🔴 Must Fix**. Undocumented deviations block merge unconditionally.

Read that together with **The Merge Criterion — Continuous Improvement** below: it holds without qualification for a deviation the changeset **introduces**. A **pre-existing** deviation the review merely surfaces is registrable instead, and the registration — owner, level consequence, expiry — is the documentation this rule asks for.

`reviewer-blind` **describes** such a deviation in `08-review-blind.md` and does not grade it — severity is a verdict, and it issues none. `reviewer-reconcile` grades it and blocks. Neither may fix it: `enforce-reviewer-readonly.sh` refuses `Write` and `Edit` outside their own outputs, because a reviewer that patches what it finds has stopped reporting it.

### Phase agents (dreamers, convergence, realist, critic, spec-author)

**Do not pause.** Flag the deviation in your phase artifact and finish; the
orchestrator surfaces it at the next 🛑, where the user is already being asked
to decide something.

This is an explicit exception to Step 3 above, and it exists because the
directive and the phase charters were in direct contradiction: this document
told every agent to stop and wait, while the charters that govern these agents
forbid pausing — *"Pause for the user. The orchestrator handles the 🛑"*. A
mandatory document instructing an agent to do what its charter forbids is not a
rule, it is a coin toss, and the agent's behaviour under it is unpredictable in
exactly the situation the rule was written for.

The substance of the directive is unchanged: the deviation is still surfaced,
still before any code is written, still decided by the user. What changes is
**where** it surfaces. A phase agent cannot hold a conversation — it runs in an
isolated context, returns once, and cannot be replied to. "Stopping and waiting"
inside one means stopping and not being asked again.

So write it where it will be read:

```markdown
## ⚠️ Principle deviation flagged
**Principle:** [name it exactly]
**Where:** [the candidate, the plan step, the test]
**The deviation:** [what it would look like]
**The principled alternative:** [what following the principle would look like]
**What we lose by following it:** [specific — numbers, ergonomics, complexity]
**Recommendation:** [yours, stated as a recommendation, not a decision]
```

Put it in the artifact **and** in your returned summary, because the artifact is
read by the next phase and the summary is read by the orchestrator, and the gate
question is asked by the orchestrator. A deviation that reaches only the artifact
reaches the next phase agent instead of the user.

The `architect` is bound by this too when it performs a phase, and by the
ordinary Stop-Explain-Wait protocol when it conducts one: it is the agent that
opens and closes a pass, and those are conversations.

### Security Expert
If a security principle would be violated and the user approves the deviation, log the risk in the threat model with the user's acceptance. The threat model must reflect all conscious security trade-offs.

### Lead (the main Claude session)
The Lead's lightweight-review authority is **strictly limited** to true non-code: README/CONTRIBUTING/CHANGELOG prose, decisions in `decisions/inbox/`, code comments without logic changes, and `.md` documentation. **The Lead never approves** any code-shaped change — see `CLAUDE.md` §§ 4–5 for the full, current list — or any file the runtime executes. Anything code-shaped goes to `reviewer-blind`, then `reviewer-reconcile`. If unsure whether something counts as code — route to them.

## Missing Review Lockout

Any agent that attempts to mark code-touching work as complete, ready-for-merge, or shippable without an explicit `reviewer-reconcile` verdict triggers immediate lockout. This includes specialists who skip handoff, the Lead approving code-shaped changes outside its narrow non-code carve-out, or any agent that says "this is good to merge" without `reviewer-reconcile` having said so first.

**The review is a pair, and half of it is not a review.** `reviewer-blind` produces `08-review-blind.md` — an independent reading, deliberately formed with no access to the plan, the PR body or the commit messages. It issues no verdict and it is not authorised to. A `08-review-blind.md` with no `09-review-verdict.md` behind it is not an approval, and treating it as one triggers this lockout exactly as skipping review entirely would.

### Protocol

1. **Halt the work.** The agent that attempted the unauthorized completion is locked out for this work item, the same way the Reviewer Rejection Protocol locks out an agent on a 🔴 Must Fix finding.
2. **Lead is notified.** The coordinator routes the locked-out work to the Lead.
3. **Lead reassigns or escalates.** The Lead has two options:
   - **Reassign to the review pair** — `reviewer-blind` first, then `reviewer-reconcile` — to perform the missed review. If `reviewer-reconcile` returns PASS, the lockout lifts and work continues. If it finds 🔴 Must Fix issues, the standard Reviewer Rejection Protocol applies. Dispatching only `reviewer-reconcile` to save a step reintroduces the capture the split exists to prevent: it will refuse to substitute itself for a missing `08-review-blind.md`, and it is right to.
   - **Escalate to the user** if the situation is ambiguous (e.g., it's not clear whether the change is code-shaped, or whether `reviewer-reconcile` has already implicitly approved).
4. **No silent recovery.** The Lead cannot simply re-route the work back to the same locked-out agent and pretend the violation didn't happen. The lockout is recorded in the session log.
5. **No bypass.** Even if the user is unavailable, the agent stays locked out. Both reviewers must run.
6. **The commit gate backs this up.** `enforce-review-verdict.sh` refuses `git commit` on code paths, `git push` to a protected branch and `gh pr merge` unless `.squad/.last-review-verdict` records a PASS for the current HEAD. The lockout made skipping review visible; the gate makes it fail.

### Why This Matters

Code review is the squad's last line of defense before code reaches `main`. Allowing agents to declare their own work complete defeats the purpose of independent review — and erodes the reviewers' authority over time.

Self-approval is the failure mode that quietly destroys review as an institution, and **every instance looks reasonable in isolation**: small change, reviewer busy, obviously works. That is exactly why it cannot be left to judgement. The lockout makes skipping review visible; the commit gate makes it fail.

## The Merge Criterion — Continuous Improvement

Adopted by the user on 2026-09-05, replacing the de-facto *"no open findings"*
criterion. The full ruling originates in an upstream-template design pass
(`.squad/design/pathless-read-blindness/11-continuous-improvement-criterion.md`)
that has no corresponding artifact in this repository — `git ls-tree -r
--name-only HEAD -- .squad/design` returns only `undo-redo/00-scope-undo-redo.md`.
The section below is the self-contained summary of that ruling and is binding
here; where it and the `reviewer-reconcile` charter differ, **this summary
wins** until the charter is synced. If the source pass is ever imported into
this repository, retarget this citation at the real path.

`reviewer-reconcile` issues ✅ / `PASS` when all three hold:

- **(a) the stated properties are green** — the acceptance criterion the
  architect or the spec named for this changeset, executed and passing. **A
  changeset with no stated property is not green; it is unstated** — the verdict
  is ⚠️ and the `architect` names one.
- **(b) every finding that is not a regression of a stated property is
  registered** — in `.claude/enforcement/refutations.md`, or the threat model for
  a security residual — with an **owner**, a **level consequence**, and an
  **`expires:` date**, written *before* the verdict.
- **(c) nothing is published above its computed level.**

**Still blocking:** a red stated property · an unregistered or malformed
registration · a level claim above its ceiling · a **regression** introduced by
the changeset · a refusal message that renders the capability it refuses.

**No longer blocking, once registered:** pre-existing gaps the changeset did not
introduce · the *length* of a correctly fail-closed enumeration (its *polarity*
still blocks) · over-match cost inside a shadow period · advisory findings ·
findings belonging to a different changeset.

**Registration is not laundering, and the reason is mechanical:** the reviewer
classifies, the author registers, the reviewer verifies. The reviewer is confined
to its own outputs by `enforce-reviewer-readonly.sh` and therefore cannot write
the entry it requires; the author cannot classify its own finding as a residual.
Neither half can complete the loop alone. Do not "simplify" that separation.

**Round cap: two.** On the third review round of one changeset, the changeset is
split — what passes under (a)–(c) ships, the rest is registered and becomes the
next changeset. The round count is evidenced by the archived `reviewer-reconcile`
drops for the same base commit, cross-checked against the `**Round:** n` line
`09-review-verdict.md` now carries; on disagreement the higher wins. **The split
never ships a red stated property** — if nothing passes, the cap escalates to the
`architect`, it does not merge.

**Newly refusing controls ship in shadow** — `.squad/.gate-shadow` with an
`expires:` of at most 14 days, affecting only decisions the previous control also
allowed, auto-flipping to enforcing at expiry. Only the user may extend, once.

**Expiry blocks nothing.** A lapsed registration demotes the level of the claim
it caps and surfaces at session start as the next work item.

**Unchanged by all of the above:** the review pair is the terminal node for code;
fail-closed polarity everywhere; the user merges to `main`; and the deviation
protocol above. A deviation the changeset **introduces** is a regression and
blocks unconditionally — registration documents an inherited gap, it is not an
approval channel, and no agent may use it as one.

## Non-Deviations (Don't Over-Trigger)

The following are NOT deviations and do NOT require user approval:

- Using BCL interfaces with "I" prefix (`IOptions<T>`, `IEntityTypeConfiguration<T>`) — those are Microsoft's names, not yours.
- Using classes when required by a framework API (ASP.NET middleware, Custom Elements, EF entity configuration).
- Performance optimization in documented hot paths (this is explicitly permitted by the principles, though the approach should still be commented).
- Choosing between two approaches that both follow the principles (e.g., picking `record struct` vs `record` for a value object — both are valid).
- Routine tool and library usage that doesn't contradict any principle.
