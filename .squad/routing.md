# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture planning and multi-context design | Gandalf | Feature design, refactoring plans, ADRs, namespace strategy, cross-cutting trade-offs |
| Frontend implementation | Galadriel | Components, UX polish, Fluent UI consistency |
| C#/.NET implementation and domain modeling | Faramir | `.cs` files, functional domain modeling, TUnit tests, Aspire/OTEL wiring, EF Core configuration |
| JavaScript runtime and browser interop | Legolas | JavaScript runtime behavior, browser APIs, client-side performance/debugging |
| Documentation updates and information architecture | Boromir | README/ADR/changelog/docs updates, doc quality, cross-linking, doc consistency after code changes |
| Backend implementation | Aragorn | APIs, services, data integration |
| CI/CD and GitHub automation | Gimli | GitHub Actions, release pipelines, deployment checks |
| Quality gate / PR approval | Elrond | Final code review, blocking authority, rejection lockout |
| Security scanning, threat modeling, pentesting | Lúthien | SAST/DAST/SCA integration, secrets detection, security regression tests, HTTP headers, CORS, CVE triage |
| Testing | Samwise | Write tests, find edge cases, verify fixes |
| Scope & priorities | Gandalf | What to build next, trade-offs, decisions |
| Async issue work (bugs, tests, small features) | @copilot 🤖 | Well-defined tasks matching capability profile |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, evaluate @copilot fit, assign `squad:{member}` label | Architect |
| `squad:{name}` | Pick up issue and complete the work | Named member |
| `squad:copilot` | Assign to @copilot for autonomous work (if enabled) | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Architect** triages it — analyzing content, evaluating @copilot's capability profile, assigning the right `squad:{member}` label, and commenting with triage notes.
2. **@copilot evaluation:** The Architect checks if the issue matches @copilot's capability profile (🟢 good fit / 🟡 needs review / 🔴 not suitable). If it's a good fit, the Architect may route to `squad:copilot` instead of a squad member.
3. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
4. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` is assigned on the issue and picks it up autonomously.
5. Members can reassign by removing their label and adding another member's label.
6. The `squad` label is the "inbox" — untriaged issues waiting for Architect review.

### Architect Triage Guidance for @copilot

When triaging, the Architect should ask:

1. **Is this well-defined?** Clear title, reproduction steps or acceptance criteria, bounded scope → likely 🟢
2. **Does it follow existing patterns?** Adding a test, fixing a known bug, updating a dependency → likely 🟢
3. **Does it need design judgment?** Architecture, API design, UX decisions → likely 🔴
4. **Is it security-sensitive?** Auth, encryption, access control → always 🔴
5. **Is it medium complexity with specs?** Feature with clear requirements, refactoring with tests → likely 🟡

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Architect handles all `squad` (base label) triage.
8. **@copilot routing** — when evaluating issues, check @copilot's capability profile in `team.md`. Route 🟢 good-fit tasks to `squad:copilot`. Flag 🟡 needs-review tasks for PR review. Keep 🔴 not-suitable tasks with squad members.
