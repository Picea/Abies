# Work Routing

Default assignments for common patterns. First match wins. Named routing (user says "Architect, do X") always overrides pattern matching.

## Routing Table

### Architecture & Design
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| New feature design | Architect | Dreamer/Realist/Critic cycle before code |
| Architecture changes | Architect | Structural decisions need deliberation |
| Cross-boundary refactoring | Architect | Changes touching multiple bounded contexts |
| New bounded context / namespace | Architect | Domain analysis required |
| Technology selection | Architect | Framework, library, or tool decisions |
| "How should we..." (structural) | Architect | Design questions before implementation |
| ADR creation (content) | Architect | Architectural Decision Records |

### C# / .NET
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| `**/*.cs` | C# Dev | C# implementation |
| `**/*.csproj`, `Directory.Build.props` | C# Dev | .NET project config |
| `**/Migrations/**` | C# Dev | EF Core migrations |
| `appsettings*.json` | C# Dev | .NET configuration |
| `*.AppHost/**` | C# Dev | Aspire orchestration |
| `*.ServiceDefaults/**` | C# Dev | Aspire service defaults |
| Domain modeling (constrained types, workflows) | C# Dev | Functional DDD implementation |
| TUnit test implementation | C# Dev | Test code |
| `dotnet new` template content | C# Dev | Template authoring |

### JavaScript
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| `**/*.js`, `**/*.mjs` | JS Dev | JavaScript implementation |
| Import maps, browser module config | JS Dev | JS tooling |
| Web Component definitions | JS Dev | Custom Elements |
| Service Worker / Web Worker scripts | JS Dev | Worker scripts |

### Documentation (mandatory on every feature/change)
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| Any new feature being built | Tech Writer (alongside specialists) | Docs ship with code |
| Any API endpoint added/modified/removed | Tech Writer | API reference update |
| Any ADR created by Architect | Tech Writer | Format review + architecture docs |
| Any config/env var/setup change | Tech Writer | Setup and config docs |
| Any Aspire AppHost topology change | Tech Writer | Infrastructure docs |
| Any `dotnet new` template change | Tech Writer | Template docs and README |
| `docs/**`, `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md` | Tech Writer | Documentation files |
| `/docs/adr/**` (formatting) | Tech Writer | ADR formatting and clarity |

### Security
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| Auth, encryption, secrets, OWASP concerns | Security Expert | Security design + tooling |
| New public API surface | Security Expert | Threat model + scan config |
| Dependency additions (NuGet, npm) | Security Expert | SCA review |
| CI/CD security pipeline stages | Security Expert | Pipeline config |
| `.zap/`, `.semgrep/`, `.gitleaks.toml` | Security Expert | Security tool config |
| Threat model updates | Security Expert | Living threat model maintenance |
| Pentest execution | Security Expert | Penetration testing |

### Review
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| Implementation complete / PR ready | Reviewer | Independent code review |
| Post-fix re-review | Reviewer | Targeted review of resolved findings |
| `"Review the changes in..."` | Reviewer | Explicit review request |

### Coordination
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| "Team, ..." (multi-agent task) | Lead | Decompose and assign |
| Bug fix with obvious root cause | Lead → Specialist | Quick triage |
| Config change, dependency bump | Lead | Handle directly |
| Status check, roster question | Lead | Answer directly |
| Ceremony (retro, design review) | Lead | Facilitate |

### Performance
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| Benchmark design, execution, analysis | Performance Engineer | Benchmark ownership |
| Load testing | Performance Engineer | Load test execution and reporting |
| Performance regression investigation | Performance Engineer | Profiling and diagnosis |
| Performance budget definition | Performance Engineer + Architect | Budget setting at design time |
| `**/Benchmarks/**` | Performance Engineer | Benchmark code |
| k6 / NBomber scripts | Performance Engineer | Load test scripts |

### Infrastructure & CI/CD
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| `.github/workflows/**` | DevOps | CI/CD pipeline |
| `Dockerfile`, `.dockerignore`, `docker-compose.yml` | DevOps | Container config |
| Container registry, deployment config | DevOps | Deployment infrastructure |
| Release automation (tagging, versioning) | DevOps | Release process |
| CI caching strategy | DevOps | Pipeline optimization |
| `dotnet new` template CI/CD scaffolding | DevOps | Template infrastructure |
| Environment setup / parity issues | DevOps | Environment management |

### UI/UX
| Pattern / Trigger | Owner | Reason |
|---|---|---|
| User-facing UI changes | UX Expert | UX review for cognitive load, accessibility, consistency |
| Form design, input validation UX | UX Expert | Error handling, progressive disclosure |
| Navigation, information architecture | UX Expert | Wayfinding, scanning, hierarchy |
| Error messages (user-facing) | UX Expert | Error message design |
| API response structure, error format | UX Expert | Developer experience |
| Accessibility compliance | UX Expert | WCAG 2.2 AA |
| Web Component interaction design | UX Expert + JS Dev | UX designs, JS Dev implements |

## Fallback

| Situation | Route To |
|---|---|
| Design/architecture question, no match | Architect |
| Implementation work, no match | Lead (triage) |
| Completed work, no match | Reviewer |
| Everything else | Lead |
