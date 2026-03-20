# Threat Model — Abies Conduit

## Scope

This threat model covers the Abies security surface with two primary areas:

1. Framework and template supply chain (critical):
- Core library packages: `Picea.Abies`, `Picea.Abies.Browser`, `Picea.Abies.Server`, `Picea.Abies.Server.Kestrel`
- Template package and sources: `Picea.Abies.Templates`
- Scaffolded output from `dotnet new abies-browser`, `dotnet new abies-browser-empty`, `dotnet new abies-server`

2. Conduit reference implementation (secondary):

- API: `Picea.Abies.Conduit.Api`
- Frontend hosts: `Picea.Abies.Conduit.Server`, `Picea.Abies.Conduit.Wasm.Host`
- Local orchestration: `Picea.Abies.Conduit.AppHost`
- Data stores: KurrentDB + PostgreSQL

## Trust Boundaries

1. Public client boundary
- Browser and external HTTP clients crossing into API routes.

2. API process boundary
- ASP.NET Core minimal API endpoint handlers and authentication middleware.

3. Data boundary
- API process crossing into PostgreSQL and KurrentDB over connection strings.

4. Build and CI boundary
- Source code and workflows crossing into CI runners and dependency/package sources.

## Attack Surface

| Entry Point | Auth Required | Input Type | Data Sensitivity | Notes |
|---|---|---|---|---|
| `POST /api/users` | No | JSON body (registration) | Credentials, identity | Account creation |
| `POST /api/users/login` | No | JSON body (email/password) | Credentials, JWT issuance | Auth boundary |
| `GET /api/user` | Yes | Header token | Identity/profile | Current user read |
| `PUT /api/user` | Yes | JSON body + header token | Identity/profile | Current user update |
| `GET /api/articles` | No | Query params | Public content | Search/listing |
| `GET /api/articles/{slug}` | No | Route param | Public content | Read path |
| `POST /api/articles` | Yes | JSON body + header token | User-generated content | Create path |
| `PUT /api/articles/{slug}` | Yes | Route + JSON + token | User-generated content | Update path |
| `DELETE /api/articles/{slug}` | Yes | Route + header token | User-generated content | Delete path |
| `POST /api/articles/{slug}/favorite` | Yes | Route + header token | User relation data | State mutation |
| `DELETE /api/articles/{slug}/favorite` | Yes | Route + header token | User relation data | State mutation |
| `GET /api/profiles/{username}` | No | Route param | Public profile | Optional auth context |
| `POST /api/profiles/{username}/follow` | Yes | Route + header token | Social graph | State mutation |
| `DELETE /api/profiles/{username}/follow` | Yes | Route + header token | Social graph | State mutation |
| `GET /api/articles/{slug}/comments` | No | Route param | User content | Public read |
| `POST /api/articles/{slug}/comments` | Yes | Route + JSON + token | User content | State mutation |
| `DELETE /api/articles/{slug}/comments/{id}` | Yes | Route + header token | User content | State mutation |
| `GET /api/tags` | No | None | Public metadata | Public read |

## Threats And Mitigations

| ID | Threat | STRIDE | Entry Point | Severity | Mitigation | Test | Status |
|---|---|---|---|---|---|---|---|
| TM-001 | Token forgery via weak JWT secret | Spoofing | Auth middleware | High | Minimum 32-byte JWT secret enforced; non-dev environments require explicit secret | Add integration test that startup fails in non-dev without `Jwt:Secret` | ⚠️ Partially mitigated |
| TM-002 | Unauthorized access to protected routes | Elevation of Privilege | Auth-required endpoints | High | Endpoint-level `RequireAuthorization()` on mutating routes; token handler validates signature and lifetime | Add endpoint matrix auth regression tests | ⚠️ Partially mitigated |
| TM-003 | Broken object-level authorization (BOLA/IDOR) on article/comment ownership | Elevation of Privilege | Article/comment update/delete endpoints | High | Domain command handlers enforce ownership checks via aggregate invariants | Add explicit negative tests for non-owner delete/update attempts | ⚠️ Partially mitigated |
| TM-004 | Injection via route/query/body input into persistence queries | Tampering | Article/profile/tag/comment reads and writes | High | Typed domain creation methods and query abstractions reduce direct injection risk | Add security regression tests with malicious payload corpus | ⚠️ Partially mitigated |
| TM-005 | XSS through rendered user content | Tampering | Article/comment content rendered in clients | High | HTML rendering path should encode untrusted output before render | Add E2E test proving script payload is rendered inert | 🔴 Open |
| TM-006 | Credential brute force on login endpoint | Denial of Service | `POST /api/users/login` | Medium | No explicit lockout/rate limit detected in API startup | Add rate limiting middleware + auth lockout policy | 🔴 Open |
| TM-007 | Missing HTTP security headers on API/frontend hosts | Information Disclosure | All HTTP responses | Medium | Baseline security headers + HSTS added across API and frontend hosts | Add explicit CSP tuning for static/websocket requirements and host-level tests | ⚠️ Partially mitigated |
| TM-008 | Secret leakage in repository or CI logs | Information Disclosure | Source tree and workflow execution | High | Gitleaks configured in local pre-commit and CI workflows | Add periodic historical scans and documented false-positive triage policy | ⚠️ Partially mitigated |
| TM-009 | Vulnerable transitive dependencies introduced | Tampering | Build and runtime deps | High | PR pipeline fails on high/critical vulnerable dependencies | Existing PR security scan validation | ✅ Mitigated |
| TM-010 | Insufficient runtime DAST coverage before merge | Repudiation | Running app/API | Medium | ZAP baseline workflow runs unauthenticated and authenticated profiles in CI, using versioned `.zap` policy and target context files with high-risk gating | Expand to full AppHost crawl and alert-tuning policy by endpoint class | ⚠️ Partially mitigated |
| TM-011 | Container/IaC vulnerabilities in deploy artifacts | Tampering | Images and infra descriptors | Medium | Trivy workflow scans filesystem and Dockerfile, and CD enforces high/critical gate on built AppHost container image | Expand image scanning to all publishable containerized hosts | ⚠️ Partially mitigated |

## Open Risks

| ID | Threat | Severity | Why Open | Planned Mitigation | Target Date |
|---|---|---|---|---|---|
| OR-001 | Secret scanning policy tuning and historical scan backlog | Medium | Secrets gate exists; allowlist/tuning and full-history cadence still evolving | Add recurring history scan and documented triage SLA | 2026-04-15 |
| OR-002 | Missing documented threat model lifecycle integration in CI review gates | Medium | Threat model file absent before this baseline | Require threat model updates on trust-boundary touching PRs | 2026-04-07 |
| OR-003 | DAST coverage needs full AppHost crawl and policy tuning | Medium | Versioned `.zap` policy/context exists and authenticated API profile runs in CI, but full AppHost/user-flow crawling is not yet enforced | Add AppHost-targeted authenticated context expansion and endpoint-class alert policy | 2026-04-21 |
| OR-004 | Image-level Trivy policy tuning | Low | CD now scans images built from all repository Dockerfiles with high/critical gating; remaining work is policy tuning and suppressions hygiene | Maintain scan coverage and tune findings policy per image profile | 2026-04-21 |
| OR-005 | Security header policy needs endpoint-specific CSP tuning | Medium | Baseline headers present but CSP is conservative/non-final | Add endpoint-aware CSP policy and host integration tests | 2026-04-21 |
| OR-006 | Template output security drift risk | Medium | Templates can accumulate insecure patterns or vulnerable transitive dependencies over time | Keep dedicated template security workflow with SCA+Trivy+Semgrep gates | 2026-04-21 |

## Review Checklist For Security-Sensitive Changes

When PRs touch authentication, authorization, endpoint shape, persistence, or hosting startup:

1. Confirm attack surface table reflects new/changed entry points.
2. Confirm affected threat row status remains accurate.
3. Add or update at least one regression test per newly introduced threat.
4. Update open risks with mitigation owner and target date if threat remains open.
