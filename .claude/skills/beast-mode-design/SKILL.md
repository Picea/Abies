---
name: beast-mode-design
description: The full Beast Mode 4.4 × Disney Creative Strategy design procedure — phase ownership and the `.squad/design/<slug>/` artifact contract, Dreamer (dual-track First Principles + Informed Design + Convergence), Realist, Critic, Spec-by-Example phase, Domain Expert Rooms catalog, dynamic agent weighting, and handoff protocol. Use when running an architectural design pass for any non-trivial feature, refactor, or technology decision.
---

# Beast Mode 4.3 × Disney Creative Strategy

The squad's design procedure. Each agent's charter describes when and why to use its phase; this skill describes how — the room mechanics, the dual-track Dreamer, the convergence analysis, the expert rooms, the spec-by-example phase, and the handoff protocol.

> *"There were actually three different Walts: the dreamer, the realist, and the spoiler. You never knew which one was coming to the meeting."* — Ollie Johnston & Frank Thomas

Johnston and Thomas were describing a problem as much as a method: nobody knew which Walt was in the room. This procedure fixes the schedule — and since 4.4, the rooms are separate agents, so nobody has to take on faith which one is speaking.

---

## Phase Ownership and the Artifact Contract

Each phase is a **separate subagent in an isolated context**. You are running exactly one of them. Find your row, do that phase, write that artifact, and stop.

| Phase | Agent | Writes | Reads |
|---|---|---|---|
| Scope + knowledge scan | `architect` | `00-scope.md` | `decisions.md`, own MEMORY |
| Dreamer Track A | `dreamer-first-principles` | `01-track-a.md` | `00-scope.md`, the codebase — **nothing else** |
| Dreamer Track B | `dreamer-informed` | `02-track-b.md` | `00-scope.md`, `decisions.md`, own MEMORY, the web |
| Convergence | `dreamer-convergence` | `03-convergence.md` | `00`, `01`, `02` |
| Realist | `realist` | `04-realist-plan.md` | `00`, `03` (+ `01`/`02` for detail), user's chosen direction |
| Critic | `critic` | `05-critic.md` | `00`, `03`, `04`, Track B's failure modes, the codebase |
| Spec-by-Example | `spec-author` | `06-spec.md` | `00`, `04`, `05`, existing tests |
| Close-out | `architect` | `07-handoff.md` + decision drop | all of the above |

All artifacts live under `.squad/design/<slug>/`. Rules:

1. **Artifacts are the interface.** Returned summaries are for the orchestrator and the user; the next phase reads your file. Write the file to be read by a peer who has none of your context.
2. **Track A and Track B are mutually blind.** They run concurrently. Neither reads the other's artifact, at any point, for any reason. `dreamer-convergence` is the only agent that reads both — which is why it is a separate context and not a section at the bottom of the Dreamer.
3. **One phase per agent.** Do not run the next phase because it seems obvious. Every transition is gated by a 🛑 the user must answer.
4. **Loop-backs overwrite in place.** The pass keeps its slug; a re-run of `realist` replaces `04-realist-plan.md`. The Critic's findings that triggered the loop-back stay in `05-critic.md` and the re-run must address them.
5. **Subagents cannot spawn subagents.** You never dispatch the next phase. You return to the orchestrator, which does.

---

## The Three Rooms

### 🌈 DREAMER Phase — *"What if…?"*

The Dreamer phase runs **two creative tracks** in sequence, then converges. This dual-track approach exists because language models (and humans) default to pattern-matching against known solutions. Track A deliberately suppresses that instinct to create space for genuinely original thinking. Track B then provides the conventional counterweight. The tension between them is where the best designs emerge.

#### Track A — 🧠 First Principles *(reasoning-only, no retrieval)*

**Mindset:** Pure reasoning from constraints. No web search. No pattern libraries. No "how does everyone else do this." Start from the problem's fundamental structure and derive solutions from first principles.

**Enforced structurally since 4.4:** `dreamer-first-principles` has no `WebSearch`/`WebFetch` in its tool grant and no persistent memory. Retrieval is unavailable rather than discouraged, and no prior session's conclusions are in context. The rules below remain because tooling cannot stop you from *recalling* a pattern — but the main leak is now closed.

**Method:**
1. **Decompose the problem** into its irreducible constraints. What *must* be true for any valid solution? What are the invariants? What are the degrees of freedom?
2. **Reason upward** from those constraints. If we had no knowledge of existing solutions, what would the shape of a correct solution look like? What does the problem's structure demand?
3. **Explore the design space** using analogical reasoning across domains — not by looking up known approaches, but by asking: *"What other problems share this same structural shape?"* Draw from mathematics, physics, biology, game theory, distributed systems theory, type theory.
4. **Generate at least 2 candidate approaches** that are derived entirely from reasoning. These candidates should feel unfamiliar. If they look like a textbook pattern, push further.

**Rules:**
- ❌ No web search or retrieval tools during this track
- ❌ No referencing named design patterns ("this is basically the Strategy pattern")
- ❌ No "the standard approach is..." or "conventionally, you would..."
- ✅ Derive from constraints, invariants, and structural properties
- ✅ Use cross-domain analogies discovered through reasoning
- ✅ Name the mathematical or structural properties that make each candidate work

**Output format:**
```
## 🧠 TRACK A — First Principles

### Problem Decomposition
**Irreducible constraints:** [what must be true]
**Degrees of freedom:** [where we have design choices]
**Structural shape:** [what kind of problem is this, structurally]

### Candidate A1: [name]
**Derived from:** [which constraints/properties led here]
**How it works:** [description]
**Structural property:** [why this works mathematically/logically]
**Feels like:** [one-sentence intuition]

### Candidate A2: [name]
[same structure]
```

#### Track B — 🔍 Informed Design *(full retrieval, existing knowledge)*

**Mindset:** Standing on shoulders. Use every resource available — web search, pattern libraries, prior decisions, community best practices, published architectures, research papers, benchmarks.

**Method:**
1. **Survey the landscape.** How do established systems solve this? What design patterns exist? What does the literature say?
2. **Check the Knowledge Base.** Read `.claude/docs/decisions.md` for prior art. Check the architect's MEMORY.md for related sessions.
3. **Search for prior art.** Use web search to find how other projects, frameworks, or papers approach this. Look for benchmarks, empirical comparisons, and battle-tested implementations.
4. **Generate at least 2 candidate approaches** rooted in established knowledge. These should be the well-understood, well-documented, production-proven options.

**Rules:**
- ✅ Web search, documentation lookup, pattern matching encouraged
- ✅ Reference named patterns, published architectures, research papers
- ✅ Cite sources, benchmarks, and empirical evidence
- ✅ Check `.claude/docs/decisions.md` and the architect's MEMORY.md for prior art

**Lenses applied within this track:**

- **🔬 Scientific lens:** Search for analogous solved problems. Look up relevant theoretical frameworks, research papers, or cross-domain patterns. If you find a generalization that reframes the problem, explain it. Example: *"This is structurally the producer-consumer problem — here are three known solutions."*
- **🏛️ Cleanness lens:** Favor established solutions that are structurally elegant and mathematically grounded. Rank by architectural purity.
- **📁 Namespace lens:** When proposing modules or features, think in bounded contexts. Propose namespace structures alongside solutions.
- **📚 Knowledge lens:** Surface relevant prior work from `.claude/docs/decisions.md` and architect MEMORY.md.

**Output format:**
```
## 🔍 TRACK B — Informed Design

### Landscape Survey
**Known approaches:** [what exists]
**Prior art in this codebase:** [relevant decisions/patterns]
**External references:** [papers, docs, benchmarks found]

### Candidate B1: [name]
**Based on:** [pattern/framework/prior art]
**How it works:** [description]
**Evidence:** [benchmarks, production usage, paper references]
**Trade-offs:** [known limitations from the literature]

### Candidate B2: [name]
[same structure]
```

#### Convergence — ⚖️ Track Comparison

After both tracks complete, produce a convergence analysis before handing off to the Realist. This is where the value of dual-track thinking materializes.

**Method:**
1. **Map the candidates.** Lay out all candidates (A1, A2, B1, B2, ...) side by side.
2. **Find convergences.** Where did both tracks arrive at structurally similar solutions? Convergence from independent tracks is a strong signal — the problem's structure demands that shape.
3. **Find divergences.** Where did Track A produce something Track B never would? This is where originality lives. Evaluate: is the divergence because Track A found something genuinely novel, or because it missed a constraint experience would have caught?
4. **Identify hybrids.** Can elements of a first-principles candidate be combined with the reliability of a known pattern? The best solutions often come from grafting a novel insight onto a proven foundation.
5. **Rank candidates.** Use the Cleanness Principle as the default ranking axis: architecturally clean, mathematically sound, formally correct. Flag where pragmatic trade-offs might change the ranking.

**Output format:**
```
## ⚖️ CONVERGENCE ANALYSIS

### All Candidates
| ID | Name | Track | Core Idea |
|----|------|-------|-----------|
| A1 | ...  | First Principles | ... |
| A2 | ...  | First Principles | ... |
| B1 | ...  | Informed Design  | ... |
| B2 | ...  | Informed Design  | ... |

### Convergences
[Where both tracks arrived at similar shapes — and what that tells us]

### Divergences
[Where Track A produced something Track B missed — and vice versa]

### Hybrid Opportunities
[Can we combine novel insights with proven foundations?]

### Recommended Direction
**Primary:** [candidate or hybrid] — because [reasoning]
**Fallback:** [candidate] — if [condition]
```

**🛑 Pause:** Before leaving the Dreamer phase, present the convergence analysis to the user and ask: *"Which direction excites you? Did the first-principles track surface anything unexpected? Any constraints I'm missing?"* Wait for their response before moving to the Realist phase.

#### When to Skip Track A

Not every task warrants a full dual-track pass. **Skip Track A** (go straight to Track B) when:

- The task is a well-understood implementation with no design ambiguity (e.g., "add a health check endpoint")
- The user explicitly asks for a conventional solution: *"Just use the standard approach"*
- The task is a bug fix with a known root cause
- Time pressure is explicitly stated and the user asks to move fast

When Track A is skipped, note it: *"⏩ Skipping first-principles track — [reason]. Going directly to informed design."*

#### When to Emphasize Track A

**Give Track A extra weight** when:

- The existing architecture has known design debt and the user wants to rethink it
- Multiple previous attempts at solving this problem have failed or been unsatisfying
- The problem crosses multiple bounded contexts and no single established pattern fits cleanly
- The user says something like *"I want something original"* or *"what if we approached this differently"*
- You notice the codebase has accumulated accidental complexity from layering conventional patterns

---

### 🔧 REALIST Phase — *"How would we actually build this?"*

**Mindset:** Pragmatic producer. Concrete, step-by-step, action-oriented.

**Goal:** Turn the best Dreamer ideas into a feasible implementation plan.

- Select the most promising idea (or hybrid) from the convergence analysis. Sketch architecture, file changes, dependencies, task sequence.
- Ask: *What resources do we need? What are the steps? What's the simplest path to a working solution? What does the dependency graph look like?*
- **🔬 Scientific lens:** Identify which established design patterns, algorithms, or architectural principles back the chosen approach. Search for benchmarks, empirical studies, or best-practice papers.
- **🏛️ Cleanness lens:** Build the plan around the cleanest viable design. If pragmatic compromises are needed for ergonomics or hot-path performance, flag them explicitly and check in with the user.
- **📁 Namespace lens:** Enforce namespace-as-bounded-context in all file and folder structures. Verify new code fits the existing namespace hierarchy.
- **📚 Knowledge lens:** Check `.claude/docs/decisions.md` for similar implementations. Reference past decisions that constrain the plan.
- **🏠 Expert rooms:** Summon any domain-specific rooms needed (Security, Performance, etc.). Integrate their recommendations into the plan.

Produce a **concrete todo list** (markdown checkboxes) with clear, small, testable steps.

Identify which squad members handle which parts. Tag them in the plan: `→ csharp-dev: implement token service`, `→ js-dev: build the web component`, `→ tech-writer: API reference for /api/tokens`. **Always include `tech-writer`.**

Identify unknowns and flag them for research.

**🛑 Pause:** Present the plan and agent assignments to the user. Ask for approval before the squad executes.

---

### 🔍 CRITIC Phase — *"What could go wrong?"*

**Mindset:** Skeptical evaluator. Adversarial, thorough, quality-obsessed.

**Goal:** Stress-test the plan. Find every flaw before the user does.

- Examine each step for edge cases, security holes, performance issues, missing tests, incorrect assumptions, hidden coupling.
- Ask: *What are the failure modes? What assumptions are we making? What did we forget? What happens at the boundaries? Are we over-engineering or under-engineering?*
- **🔬 Scientific lens:** Validate the approach against known theoretical limits and failure modes from literature. Use complexity analysis, known bounds, or empirical research to challenge assumptions.
- **🏛️ Cleanness lens:** Challenge any deviation from architectural cleanness. Ask: *"Is this shortcut truly necessary, or are we being lazy? Does the math still hold?"*
- **📁 Namespace lens:** Audit namespace consistency. Flag namespaces used as abbreviations instead of domain boundaries.
- **📚 Knowledge lens:** Check past sessions (architect MEMORY.md) for similar mistakes.
- **🏠 Expert rooms:** All active expert rooms perform their final assessments.

If significant issues are found, **loop back** to Dreamer or Realist — don't paper over problems.

Check for violations of the team's established principles (functional DDD, illegal states unrepresentable, etc.).

**🐛 Bug-fix gate:** For bug-fix tasks, verify the plan includes a regression test that reproduces the original bug. The test must fail before the fix and pass after. No fix ships without a test that would have caught it.

**🛑 Pause:** Present findings to the user — risks identified, mitigations proposed, trade-offs. Wait for approval. No autonomous continuation.

---

## Domain-Specific Expert Rooms

Beyond the three core rooms, summon domain-specific expert rooms when a task enters specialized territory.

| Room | Icon | Trigger | Focus |
|---|---|---|---|
| **Security** | 🛡️ | Auth, encryption, input validation, secrets, OWASP | Threat modeling, attack surface, secure defaults, least privilege |
| **Performance** | ⚡ | Hot paths, latency, memory, scalability | Profiling, complexity, caching, benchmarks, resource budgets |
| **UX** | 🎨 | User-facing changes, API ergonomics, error messages, DX | Cognitive load, discoverability, consistency, accessibility |
| **Data** | 🗄️ | Schema, migrations, queries, data integrity, ETL | Normalization, indexing, consistency models, GDPR/lifecycle |
| **Operations** | 🚀 | Deployment, monitoring, alerting, SLAs, incidents | Observability, graceful degradation, rollback, runbooks |
| **Concurrency** | 🔀 | Async, parallelism, shared state, race conditions | Lock-free algorithms, actor models, CSP, linearizability |
| **Test Strategy** | 🧪 | Every new feature (mandatory), behavior change, new bounded context | Test pyramid health, which level to test at (E2E/integration/unit), spec-by-example, property-based testing, mutation testing, test data builders, fixture design |

### How Expert Rooms Work

1. **Summoning** — When the task touches a domain that has an expert room, you **must** activate it. Announce: `## 🛡️ SECURITY ROOM — [topic]`.
2. **Integration** — Expert rooms operate *within* the current phase. During Track B of the Dreamer, the Security Room brainstorms threat models alongside solutions. During the Critic, it audits the plan for vulnerabilities.
3. **Output format:**
   ```
   ## [icon] [ROOM NAME] — [topic]

   **Assessment:** [2-3 sentence summary]
   **Risks:** [ranked by severity]
   **Recommendations:** [concrete actions, integrated into the phase's output]
   **References:** [standards, papers, tools]
   ```
4. **Dismissal** — A room stays active until concerns are resolved: `✅ [ROOM NAME] concerns resolved — [summary]`.
5. **Escalation** — Critical issues (SQL injection, O(n²) in hot loop) force a phase loop-back, even mid-phase.
6. **Custom Rooms** — The user can define ad-hoc rooms: *"We need a Compliance Room for HIPAA."* Create it following the same structure.

### When Real Specialist Subagents Replace In-Room Representation

For domain rooms that map to real specialist subagents in the squad (Security → `security-expert`, Performance → `performance-engineer`, UX → `ux-expert`):

- During design, you can hold the room yourself for fast feedback.
- When concerns are non-trivial, flag in your output that the orchestrator should spawn the matching subagent for full input before proceeding.
- For features that touch threat boundaries, performance budgets, or accessibility requirements, **always** flag for subagent spawn — don't rely on your in-room representation alone.

The Test Strategy Room (🧪) is mandatory during the Spec-by-Example phase and you hold it yourself.

---

## Dynamic Agent Weighting

The three core agents adjust influence based on context:

- **Greenfield work** — The Dreamer gets the most room. Track A is weighted heavily. Wide exploration before narrowing.
- **Legacy/brownfield** — The Realist and Critic get extra weight. The existing codebase constrains the solution space. Track B leads, but Track A can reveal escape hatches from accumulated tech debt.
- **Production incident / hotfix** — The Critic leads. Skip Track A entirely. Safety and correctness over creativity.
- **Refactoring** — The Dreamer's Track A gets emphasis: *"What would this look like if we designed it from scratch?"* Then the Realist bridges from current state to ideal state.

When the user is being reckless or moving too fast, the Critic gets extra weight to pump the brakes. Trigger: pattern detection in conversation — repeated safe choices, or repeated dismissals of risk.

---

## 🧪 Spec-by-Example Phase — *"Write the test before the code"*

After the Critic phase is approved and **before** Handoff, every feature passes through a **Spec-by-Example Phase**. The test is the executable specification — if the user can't recognize the feature in the test, the team doesn't yet understand the feature.

This phase exists because most rework comes from misunderstood requirements, not bad implementation. Catching the misunderstanding when only a test exists is dramatically cheaper than catching it when production code, docs, and downstream changes already exist.

### When This Phase Runs

**Always run for:**
- New features
- Behavior changes (anything a user could observe)
- New bounded contexts or new public APIs
- Changes to workflow functions, state machines, or domain invariants

**Skip for** (note the skip explicitly: *"⏩ Skipping Spec-by-Example — [reason]"*):
- Pure refactoring with no behavior change (the existing test suite is the spec; no test should change)
- Trivial changes: one-line config changes, doc-only changes, dependency bumps with no behavior change
- Bug fixes — already covered by the bug-fix regression test rule

### Process

1. **Summon the Test Strategy Room.** During this phase, the Test Strategy Room (🧪) is mandatory. It decides:
   - **Which level** the first test should be at — E2E through UI (Playwright via Aspire), API/integration (HTTP client through Aspire AppHost), or workflow-function-direct (domain-level integration). The choice is per-feature based on what best captures user-observable behavior. A pure domain rule may best be tested at the workflow level; a user journey needs Playwright; a new endpoint needs an HTTP integration test.
   - **How many tests** — usually one acceptance test for the happy path plus a small set of variants. The goal is "smallest test set that, if all pass, the feature is done." Edge cases get added as unit tests later, driven by the implementation.
   - **What scope** — what's inside the test boundary, what's mocked or substituted, what infrastructure runs (for new tests, always through the Aspire AppHost — never `WebApplicationFactory` or Testcontainers). The project's `.claude/docs/decisions.md` is authoritative if it documents a narrow exception for pre-existing fixtures; such exceptions don't extend to new work by default.

2. **Designate the test author.** The test is drafted by **whichever specialist owns the layer being tested first** (the orchestrator spawns this specialist after your handoff). For your purposes here, just identify the layer:
   - E2E through UI → `js-dev` (Playwright) with `csharp-dev` for backend test data setup
   - API/integration → `csharp-dev` (TUnit + Aspire `DistributedApplicationTestingBuilder`)
   - Workflow-function-direct → `csharp-dev` (TUnit, no AppHost needed if pure domain)
   - Multi-layer feature → owners coordinate; the agent owning the outermost test layer drives

3. **Draft the test yourself for review.** Even though a specialist will own it, you draft a candidate so the user can approve the spec, not just the strategy. The test must:
   - **Read like a specification.** Names and structure express the user-observable behavior, not the implementation. `Submitting_a_draft_order_with_unavailable_stock_returns_OutOfStock_error`, not `OrderService_Submit_Test_3`.
   - **Fail meaningfully.** Run it before any implementation exists. The failure message must point at what's missing, not at a `NullReferenceException` from scaffolding.
   - **Be honest about what it asserts.** No `Assert.True(result is not null)` placeholders — assert the actual expected outcome (exact `Result` variant, exact field values, exact event emitted).
   - **Use the team's testing conventions** — TUnit, Aspire AppHost for anything beyond pure domain, deterministic test data, no wall-clock dependencies.

4. **🛑 Pause for user approval.** Present the test to the user and ask:
   > *"This test is the executable specification for the feature. If this test passes, do you consider the feature done? Anything missing? Anything wrong?"*

   The user reviews and either approves, requests changes to the test, or sends the design back to the Critic/Realist for rework. **Implementation does not begin until the user has explicitly approved the test.**

5. **Lock the approved test.** Once approved, the test is **immutable for this feature** — implementation must make the test pass without modifying the test. If implementation reveals the test is wrong, the implementation pauses, the test change is proposed back to the user, and the user re-approves before continuing. Silent test edits during implementation are a 🔴 Must Fix at review.

### Output Format

```
## 🧪 SPEC-BY-EXAMPLE — [feature]

### Test Strategy
**Level:** [E2E-UI / API-integration / Workflow-direct]
**Why this level:** [reasoning — what user-observable behavior this captures]
**Test author:** [agent who will own the test going forward]
**Scope:** [what's in, what's out, what's mocked, what runs through AppHost]

### The Test
[Drafted test code — TUnit, Playwright, etc.]

### What This Test Proves
**Behavior captured:** [plain-language description of what passing this test means]
**Behavior not captured:** [what edge cases or variants will need additional tests, added during implementation]

### 🛑 Approval Request
"If this test passes, the feature is done. Approve? Reject? Modify?"
```

---

## Handoff Protocol

**Handoff only happens after the user has explicitly approved all three phases AND, where applicable, the Spec-by-Example test.** The Dreamer direction, the Realist plan, the Critic assessment, and (for features and behavior changes) the spec-by-example test must each receive user sign-off. If any approval is missing, do not hand off.

When all approvals are in:

1. Write the final plan as a structured task list with agent assignments.
2. **Always include `tech-writer`.** Every change gets a tech-writer assignment — either to write new docs or to verify all existing docs are still in sync with reality after the change. Format: `→ tech-writer: [new docs needed] + doc-sync verification`.
3. **Reference the approved spec test** in the handoff message so implementers know the bar: *"Spec test: [path/to/test.cs]. Implementation passes when this test passes without modification."*
4. Log architectural decisions to `.squad/decisions/inbox/`.
5. Tell the orchestrator: *"All phases approved. Spec test approved at [path]. Ready for squad execution. Assign: [agent] → [task], ..., tech-writer → [doc scope] + doc-sync verification."*
6. Stay available for questions during implementation — specialist subagents can be re-spawned with questions for you.
7. After implementation, the **`reviewer`** subagent performs an independent code review. You do not review code — the reviewer does. This separation is intentional.

---

## Knowledge Capture

Tag notable moments during work so your post-session MEMORY.md update is easy:

- `📓 Journal:` — interesting observation about the codebase or problem
- `📚 Pattern:` — a reusable pattern emerging
- `🗺️ Decision:` — a decision being made (also write to `.squad/decisions/inbox/`)

When a current task relates to a past decision or pattern, **explicitly link them** in your output: *"This is the same pattern we used in session [date] — see Pattern: [name]. Last time we chose [X] because [reason]. Does that still hold?"*

### Knowledge Scan (start of every significant task)

```
## 📚 KNOWLEDGE SCAN

**Related patterns:** [list any patterns from MEMORY.md that apply]
**Related decisions:** [list any active decisions that constrain or inform this task]
**Related sessions:** [list any past sessions that dealt with similar problems]
**Revisit triggers:** [list any triggers that may have fired]

**Implication:** [how this prior knowledge shapes the current approach]
```

---

## Version

Beast Mode 4.4 — Phase Agents
Changelog:
- 4.4: Each phase became a separate subagent with its own context, tool grant, and memory (`dreamer-first-principles`, `dreamer-informed`, `dreamer-convergence`, `realist`, `critic`, `spec-author`). Track A's no-retrieval rule is now enforced by tool grant and by the absence of persistent memory, and the two Dreamer tracks run concurrently in mutually blind contexts. Phases communicate through numbered artifacts under `.squad/design/<slug>/` rather than through returned summaries. The `architect` retains scope, close-out, and a solo fast path for small designs; the orchestrator sequences the phases, since subagents cannot spawn subagents.
- 4.3: Added Spec-by-Example Phase between Critic and Handoff. Test Strategy Expert Room (🧪) is mandatory for new features and behavior changes. Test is drafted by the agent owning the layer being tested and approved by the user before implementation begins. Approved test is immutable during implementation. Handoff Protocol updated to require approved test alongside the three phase approvals.
- 4.2: Added Track A (First Principles) and Track B (Informed Design) with Convergence Analysis to the Dreamer phase. Dynamic agent weighting by context. Skip/emphasize guidance for Track A.
- 4.1: Initial Squad integration. Disney Creative Strategy as dedicated Architect agent. Independent Reviewer with lockout authority.
