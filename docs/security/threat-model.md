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

5. Squad-flow enforcement boundary
- Agent tool calls (`Write`/`Edit`/`MultiEdit`/`NotebookEdit`/`Bash`) crossing
  from an agent's own context into the squad-flow framework's own governance
  state: the review-verdict cache (`.squad/.last-review-verdict`), the
  decision-drop inbox/archive (`.squad/decisions/**`), and per-agent memory
  (`.claude/agent-memory/**`). Added 2026-09-06 to give the residuals
  `.claude/hooks/enforce-reviewer-readonly.sh` and
  `.claude/hooks/scribe-decision-merger.sh` already named in their own
  headers (as `T-013`/`T-015`/`T-017`, a numbering that was never actually
  present in this file — see TM-012 through TM-014 below, PR #358 review
  round 1, 🔴-6) a real referent rather than a citation to nothing. This
  boundary is about the framework's OWN enforcement layer, not the Conduit
  application; TM-001 through TM-011 above are unaffected.

6. Dreamer/reviewer blindness boundary
- Agent tool calls (`Grep`/`Glob`/`Read`/`NotebookRead`) crossing from an
  agent under a mutual-blindness constraint (`dreamer-first-principles`,
  `reviewer-blind`) into design artifacts, decision history, pattern-lexicon
  content, or review narrative that agent's independence depends on not
  seeing. Enforced by `.claude/hooks/enforce-track-blindness.sh` and
  `.claude/hooks/enforce-review-blindness.sh`. Added 2026-09-06 (PR #358
  review round 2, ⚠️-C) to give `T-012` — cited around ten times across both
  hooks and `.claude/hooks/tests/blindness.sh` for "Glob's effective root is
  the composition of `path` and `pattern`, not `path` alone" — a real
  referent distinct from TM-012 below, which is an unrelated threat (`Bash`
  redirection bypassing the verdict-cache write confinement, on Trust
  Boundary 5) that happened to collide with the same informal number. A
  reader who followed `T-012` before this addition landed on TM-012's real
  but unrelated row, which is worse than landing on nothing. This boundary
  is about preserving agent independence, not the framework's write
  confinement (boundary 5) or the Conduit application; TM-001 through TM-014
  are unaffected.

- **Scope note, added 2026-09-06 (PR #358 review round 3, ⚠️-1):** the tool
  list above (`Grep`/`Glob`/`Read`/`NotebookRead`) is `dreamer-first-principles`'s
  entire read surface — its `tools:` grant is `Read, Grep, Glob, Write` — but
  it is not `reviewer-blind`'s, whose `tools:` grant additionally includes
  `Bash`. Neither `enforce-track-blindness.sh` nor
  `enforce-review-blindness.sh`/`enforce-review-history-channel.sh` mediates a
  `Bash` read of the same design artifacts, decision history, or review
  narrative this boundary names; see TM-016 and ledger entry R-19. This
  boundary's own text is accurate for the tools it names — it agrees with the
  ledger only because it does not claim coverage of the one channel that
  differs between its two governed agents.

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
| TM-012 | `Bash` redirection (`echo ... > .squad/.last-review-verdict`) bypasses the verdict-cache write confinement | Tampering | Squad-flow enforcement boundary — `.squad/.last-review-verdict` | High | `enforce-reviewer-readonly.sh` closes the `Write`/`Edit`/`MultiEdit`/`NotebookEdit` channel for every `agent_type` (deny-by-default on the path, not allow-listed for any); it mediates none of those tools' equivalent effect via `Bash`, and `enforce-review-verdict.sh` gates `git`/`gh` verbs, not arbitrary shell redirection. Any of the nine `Bash`-holding roles can still write this file directly. Referenced in the hooks as `T-013` before this row existed | Add a suite assertion that a raw `Bash` redirection to this path is refused for every `Bash`-holding agent, then close the channel (fix direction 3 named in `enforce-reviewer-readonly.sh`'s header) | 🔴 Open |
| TM-013 | Decision-drop `agent:`/`verdict:` fields are unauthenticated, so any agent (or a hand-edited inbox file) can archive and cache a self-declared `reviewer-reconcile` / `PASS` as a genuine review verdict | Spoofing | Squad-flow enforcement boundary — `.squad/decisions/inbox/**`, `.squad/.last-review-verdict` | High | `scribe-decision-merger.sh`'s schema/consistency checks close every known PARSING-differential forgery (nested-key promotion, whitespace/line-terminator differentials, BOM/Cf-prefix bypasses, interleaved-block-sequence bypass), but a well-formed, honest, non-nested self-declaration is accepted at face value — the `SubagentStop` payload carries no authenticated agent identity for this hook to check against. Referenced in `scribe-decision-merger.sh` as `T-015` before this row existed | Authenticate the drop's `agent:` field against the invoking harness (would need a trusted identity stamped into the `SubagentStop` payload), or gate who may write `.squad/decisions/inbox/**` | 🔴 Open |
| TM-014 | Verdict-cache path-spelling confinement does not resolve a hardlink, and is case-sensitive on a case-insensitive filesystem | Tampering | Squad-flow enforcement boundary — `.squad/.last-review-verdict` | Low | `enforce-reviewer-readonly.sh`'s `candidates()` resolves symlink files and symlink directories via `os.path.realpath` before matching; a hardlink (a distinct directory entry sharing an inode, with nothing in the path text for `realpath` to follow) and a case-folded spelling on a case-insensitive filesystem are both untouched by that resolution. Referenced in the hooks as `T-017` before this row existed | Detect same-inode hardlinks via `os.stat` comparison against the known cache path; decide whether case-folding is in scope given the project's filesystem assumptions | 🔴 Open |
| TM-015 | `Glob`'s effective read root is the composition of its `path` and `pattern` arguments, not `path` alone — a benign-looking `path` combined with a climbing (`../`) or brace-grouped pattern can reach a file outside `path` that the containment check, reading `path` in isolation, would have allowed | Elevation of Privilege | Dreamer/reviewer blindness boundary — `Glob` containment in `enforce-track-blindness.sh` and `enforce-review-blindness.sh` | High | `glob_root()`/`reach_hit()` in both hooks compose `path` and `pattern` before classifying the result against each agent's `DENY` list (fix 1), and separately refuse any pattern `glob_root()` cannot classify at all, e.g. a brace-grouped alternation (fix 2 / D20), rather than treating "cannot classify" as "allow". Referenced in both hooks and in `.claude/hooks/tests/blindness.sh` as `T-012` before this row existed | `.claude/hooks/tests/blindness.sh`'s `[T-012]` cases: climbing pattern refused, grouped-pattern evasion refused (fix 2, not just fix 1), and the two benign-control cases proving neither fix over-blocks an ordinary `path`+`pattern` pair | ✅ Mitigated |
| TM-016 | `reviewer-blind` holds `Bash`, which neither `enforce-review-blindness.sh` nor `enforce-review-history-channel.sh` mediates for reads of `.squad/design/**`, decision history, or review narrative — the four-tool DENY list Trust Boundary 6 names is the entire read surface only for the sibling agent `dreamer-first-principles` (`tools:` grant `Read, Grep, Glob, Write`); for `reviewer-blind` (`tools:` grant additionally `Bash`) the same blindness is instruction-level, not invariant-level | Elevation of Privilege | Dreamer/reviewer blindness boundary — `Bash` reads of `.squad/design/**`, `.squad/decisions/**`, review narrative, by `reviewer-blind` | High | None in the hooks — `reviewer-blind.md`'s charter asks the agent not to use `Bash` for this ("the One Thing That Remains a Promise"), but no hook enforces it. Verified by execution: every hook in `.claude/hooks/` exits 0 against a `reviewer-blind` `Bash` payload of `cat .squad/design/<slug>/04-realist-plan.md`, including `enforce-review-history-channel.sh`, which gates `git`/`gh` history verbs but not `cat`. Ledger entry R-19 | Add a suite assertion that a `Bash` read of design/decision/narrative paths is refused for `reviewer-blind`, matching the coverage `enforce-review-blindness.sh` already gives `Read`/`Grep`/`Glob`/`NotebookRead` — or drop `Bash` from `reviewer-blind`'s grant, per R-19 | 🔴 Open |

## Open Risks

| ID | Threat | Severity | Why Open | Planned Mitigation | Target Date |
|---|---|---|---|---|---|
| OR-001 | Secret scanning policy tuning and historical scan backlog | Medium | Secrets gate exists; allowlist/tuning and full-history cadence still evolving | Add recurring history scan and documented triage SLA | 2026-06-30 |
| OR-002 | Missing documented threat model lifecycle integration in CI review gates | Medium | Threat model lifecycle enforcement still being hardened in PR governance | Require threat model updates on trust-boundary touching PRs | 2026-06-15 |
| OR-003 | DAST coverage needs full AppHost crawl and policy tuning | Medium | Versioned `.zap` policy/context exists and authenticated API profile runs in CI, but full AppHost/user-flow crawling is not yet enforced | Add AppHost-targeted authenticated context expansion and endpoint-class alert policy | 2026-06-30 |
| OR-004 | Image-level Trivy policy tuning | Low | CD now scans images built from all repository Dockerfiles with high/critical gating; remaining work is policy tuning and suppressions hygiene | Maintain scan coverage and tune findings policy per image profile | 2026-06-30 |
| OR-005 | Security header policy needs endpoint-specific CSP tuning | Medium | Baseline headers present but CSP is conservative/non-final | Add endpoint-aware CSP policy and host integration tests | 2026-06-30 |
| OR-006 | Template output security drift risk | Medium | Templates can accumulate insecure patterns or vulnerable transitive dependencies over time | Keep dedicated template security workflow with SCA+Trivy+Semgrep gates | 2026-06-30 |
| OR-007 | `Bash`-redirection bypass of the verdict-cache write confinement (TM-012) | High | `enforce-reviewer-readonly.sh` mediates `Write`/`Edit`/`MultiEdit`/`NotebookEdit` only; a raw shell redirection from any of the nine `Bash`-holding roles is not tool-mediated by it, and `enforce-review-verdict.sh` classifies `git`/`gh` verbs, not arbitrary redirection | Owner: security-expert. Add a suite assertion proving the gap live, then close the `Bash`-redirection channel (fix direction 3 named in `enforce-reviewer-readonly.sh`'s own header) | 2026-10-31 |
| OR-008 | Decision-drop authorship unauthenticated (TM-013) | High | The `SubagentStop` payload `scribe-decision-merger.sh` reads carries no trusted agent identity to check a drop's self-declared `agent:` field against; a well-formed, honest, non-nested `agent: reviewer-reconcile` / `verdict: PASS` self-declaration is archived and cached at face value | Owner: security-expert, coordinating with devops on a harness-level trusted-identity stamp for `SubagentStop` payloads, or a write-gate on `.squad/decisions/inbox/**` | 2026-10-31 |
| OR-009 | Verdict-cache hardlink/case-fold path-spelling gap (TM-014) | Low | `os.path.realpath` in `enforce-reviewer-readonly.sh`'s `candidates()` does not resolve a hardlink (no path text for it to follow) and the matcher does not fold case | Owner: security-expert. Add `os.stat`-based same-inode detection; decide whether case-folding is in scope given this project's filesystem assumptions | 2026-11-14 |
| OR-010 | `Bash` channel unmediated on the dreamer/reviewer blindness boundary for `reviewer-blind` (TM-016) | High | `enforce-review-blindness.sh` and `enforce-review-history-channel.sh` mediate `Read`/`Grep`/`Glob`/`NotebookRead` and `git`/`gh` history verbs respectively; neither mediates an arbitrary `Bash` read (e.g. `cat`) of the same design/decision/narrative paths, and `reviewer-blind` is the one governed agent on this boundary that holds `Bash` | Owner: security-expert, pending an architect decision on fix direction (close the channel in-hook, or drop `Bash` from `reviewer-blind`'s grant). Ledger pointer R-19 | 2026-10-31 |

## Review Checklist For Security-Sensitive Changes

When PRs touch authentication, authorization, endpoint shape, persistence, or hosting startup:

1. Confirm attack surface table reflects new/changed entry points.
2. Confirm affected threat row status remains accurate.
3. Add or update at least one regression test per newly introduced threat.
4. Update open risks with mitigation owner and target date if threat remains open.
