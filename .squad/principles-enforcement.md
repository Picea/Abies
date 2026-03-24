# Principles Enforcement Directive

**This directive applies to every agent in the squad. No exceptions.**

## The Rule

**Every deviation from an established principle MUST be discussed with and approved by the user before proceeding.** No agent may silently compromise, work around, or "pragmatically adjust" any principle. If you cannot follow a principle as written, you stop and ask.

This is not a suggestion. This is a hard gate.

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

### Reviewer
During code review, if you find code that deviates from any principle and there is no documented approval (decision log entry + code comment), it is a **🔴 Must Fix**. Undocumented deviations block merge unconditionally.

### Security Expert
If a security principle would be violated and the user approves the deviation, log the risk in the threat model with the user's acceptance. The threat model must reflect all conscious security trade-offs.

## Non-Deviations (Don't Over-Trigger)

The following are NOT deviations and do NOT require user approval:

- Using BCL interfaces with "I" prefix (`IOptions<T>`, `IEntityTypeConfiguration<T>`) — those are Microsoft's names, not yours.
- Using classes when required by a framework API (ASP.NET middleware, Custom Elements, EF entity configuration).
- Performance optimization in documented hot paths (this is explicitly permitted by the principles, though the approach should still be commented).
- Choosing between two approaches that both follow the principles (e.g., picking `record struct` vs `record` for a value object — both are valid).
- Routine tool and library usage that doesn't contradict any principle.
