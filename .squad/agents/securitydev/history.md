# Security Expert & Pentester — History

## About This File
Project-specific security learnings, tool evaluations, vulnerability patterns, and pentest results. Read this before every session.

## Security Toolchain Status
| Layer | Tool | Status | Last Evaluated |
|---|---|---|---|
| SAST | Roslyn Analyzers | Not yet configured | |
| SAST | Semgrep | Not yet configured | |
| SCA | dotnet vuln scan | Not yet configured | |
| SCA | Dependabot | Not yet configured | |
| Secrets | Gitleaks | Not yet configured | |
| DAST | OWASP ZAP | Not yet configured | |
| Container | Trivy | Not yet configured | |

## Vulnerability Patterns Found
*None yet — recurring vulnerability classes tracked here.*

## Scanner Rules Added/Tuned
| Rule | Tool | Reason | Date |
|---|---|---|---|
| *None yet* | | | |

## False Positive Patterns
*None yet — findings that look bad but aren't, so the team doesn't waste time.*

## Tool Evaluations
*None yet — document what was tested, what was chosen, and why.*

## Pentest History
| Date | Scope | Critical | High | Medium | Low | Report |
|---|---|---|---|---|---|---|
| *None yet* | | | | | | |

## Threat Models
*None yet — threat models for features/components tracked here.*

## Threat Intelligence Log
| Date | Threat/CVE | Affects | Severity | Mitigated | Regression Test | Scanner Rule Added |
|---|---|---|---|---|---|---|
| *None yet* | | | | | | |

## Proactive Hardening
| Date | Action | Result |
|---|---|---|
| *None yet — dependency pruning, baseline scans, rule updates tracked here.* | | |

## Attack Surface Map
*Not yet mapped — all public endpoints, auth flows, data flows, external integrations tracked here.*

## Security Standards
*Refer to charter for baseline. Project-specific additions tracked here.*

## Learnings

### 2026-04-01 - PR Security Gating Audit (CI workflows)
- Mandatory PR checks already present: gitleaks secrets scan in both `secrets-scan.yml` and `pr-validation.yml` security-scan job; SCA high/critical gate in `pr-validation.yml`; Trivy high/critical gate in `trivy.yml`; CodeQL on PR in `codeql.yml`.
- Duplicate PR SCA exists in `pr-validation.yml` and `cd.yml`; keep PR gate in `pr-validation.yml` as source of truth and remove PR-triggered SCA from `cd.yml` to reduce PR latency without lowering merge protection.
- Heavy security jobs currently running on every PR: `zap-baseline.yml` (starts services + API + baseline + authenticated profile) and `template-security.yml` (packs templates, scaffolds, restores/builds, Semgrep, Trivy). These are better suited to push/main + nightly and path-filtered PR runs.
- Non-negotiables that should stay PR-blocking: secrets detection, dependency vulnerability gate (HIGH/CRITICAL), and at least one code-level SAST signal (CodeQL or Semgrep) to catch injection/authz patterns before merge.
- Scope-limiting opportunities without blind spots:
	- Path-filter heavy scans: run template security only when template/framework/packaging inputs change.
	- Path-filter ZAP only when API/auth/HTTP middleware/routing changes.
	- Keep full-repo scans on scheduled/nightly as compensating control.
	- For Semgrep PR optimization, diff-based targeting is acceptable only if nightly full scan remains enforced.
