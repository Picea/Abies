# Beast Mode × Squad — Complete Package

## What's Inside

This package contains a fully configured Squad team with Beast Mode 4.1 integration. Drop the `.squad/` folder into your repo after running `squad init`, replacing the generated defaults.

### Team (11 agents)

| Agent | Role | Charter |
|---|---|---|
| **Lead** | Triage, coordination, unblocking | `.squad/agents/lead/charter.md` |
| **Architect** | Beast Mode Disney Creative Strategy (Dreamer/Realist/Critic) | `.squad/agents/architect/charter.md` |
| **C# Dev** | Pure functional C# 14, .NET 10, DDD, TUnit, Aspire | `.squad/agents/csharpdev/charter.md` |
| **JS Dev** | Vanilla JavaScript, ES2024+, Web Components, zero-framework | `.squad/agents/jsdev/charter.md` |
| **UI/UX Expert** | Don't Make Me Think, WCAG 2.2 AA, cognitive load, DX | `.squad/agents/uxdev/charter.md` |
| **Tech Writer** | Diátaxis documentation, ADRs, Markdown only | `.squad/agents/techwriter/charter.md` |
| **Reviewer** | Independent code review with lockout authority | `.squad/agents/reviewer/charter.md` |
| **Security Expert** | SAST/DAST/SCA, pentesting, living threat model | `.squad/agents/securitydev/charter.md` |
| **Performance Engineer** | Benchmarking, profiling, load testing, performance budgets | `.squad/agents/perfeng/charter.md` |
| **DevOps Engineer** | CI/CD, Docker, deployment, release automation | `.squad/agents/devops/charter.md` |
| **Scribe** | *(auto-managed by Squad)* | |

### Squad Files

| File | Purpose |
|---|---|
| `.squad/team.md` | Full roster with roles, expertise, review authority |
| `.squad/routing.md` | Routing table — what goes where |
| `.squad/decisions.md` | Pre-seeded with all core principles as directives |
| `.squad/principles-enforcement.md` | Hard gate: every deviation requires user approval |
| `.squad/skills/squad-beast-mode-conventions/SKILL.md` | Starter skill documenting the workflow |

### Standalone Agent (optional)

| File | Purpose |
|---|---|
| `beast-mode.agent.md` | Standalone Beast Mode 4.1 for `.github/agents/` — use without Squad for solo work |

## Setup

### Fresh project

```bash
cd your-project
git init
squad init          # Accept defaults — we'll replace them
```

Then replace the generated `.squad/` contents with this package's `.squad/` folder.

### Existing Squad project

Copy the agent directories into `.squad/agents/`, replace `team.md`, `routing.md`, and `decisions.md` with the ones from this package. Add `principles-enforcement.md` to `.squad/`.

### Standalone Beast Mode (no Squad)

Copy `beast-mode.agent.md` to `.github/agents/beast-mode.agent.md`. Select it from the agent picker in VS Code Chat. Works independently of Squad.

### Both (recommended)

Use Squad for team-based work, standalone Beast Mode for solo sessions. Both coexist in the same repo.

## Casting

Squad will assign cast names from your chosen universe when agents are first initialized. The folder names (`csharpdev`, `jsdev`, etc.) are functional placeholders — Squad's casting system will rename them. The charters and history files transfer regardless of the cast name.

## After Setup

1. Open VS Code
2. Open Chat (Ctrl+Alt+I)  
3. Select **Squad** from the agent picker
4. Try: `"Show me the current team roster"`
5. Try: `"Architect, how should we approach building a recipe API?"`
6. Try: `"What decisions has the team made?"`
