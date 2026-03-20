# Security Hardening Backlog

This backlog is derived from the initial repository assessment and the baseline threat model.

## P0 — Immediate (Blockers For Defense-In-Depth)

1. Add secret scanning (local + CI) ✅ Completed
- Scope: add `.gitleaks.toml`, pre-commit hook wiring, and CI job.
- Acceptance criteria:
  - Commit with test secret pattern is blocked locally.
  - PR pipeline fails on verified secret findings.
  - False-positive allowlist process documented.

2. Align security gate behavior across PR and CD workflows ✅ Completed
- Scope: remove soft-warning behavior for high/critical dependency findings in CD.
- Acceptance criteria:
  - High/critical SCA findings fail both PR and CD.
  - Exception path requires explicit tracked issue URL.

3. Add security headers baseline ✅ Completed
- Scope: API and frontend hosts must set strict baseline headers.
- Acceptance criteria:
  - `X-Content-Type-Options: nosniff` present.
  - `X-Frame-Options: DENY` or explicit equivalent policy.
  - `Referrer-Policy` and `Permissions-Policy` set.
  - HSTS enabled outside development.

## P1 — Short Term (High Value)

1. Introduce Semgrep policy pack ✅ Completed (initial baseline)
- Scope: add `.semgrep/` rules for endpoint authorization and unsafe string construction patterns.
- Acceptance criteria:
  - CI Semgrep job runs on PRs.
  - High findings fail merge.
  - Project-specific rules cover auth on mutating endpoints.

2. Introduce DAST baseline (OWASP ZAP) ✅ Completed (initial baseline)
- Scope: add `.zap/` baseline config and CI PR scan against AppHost API surface.
- Acceptance criteria:
  - ZAP baseline runs in PR pipeline.
  - High-risk findings fail.
  - Medium findings produce tracked issues.

3. Add Trivy baseline for container and filesystem scan ✅ Completed (initial baseline)
- Scope: workflow job and optional `trivy.yaml` policy tuning.
- Acceptance criteria:
  - PR scans produce SARIF or artifact report.
  - Critical/high findings fail pipeline.

## P2 — Medium Term (Maturity)

0. Add dedicated template security gates ✅ Completed (initial baseline)
- Scope: scaffold `dotnet new` templates in CI and run SCA + Trivy + Semgrep template rules.
- Acceptance criteria:
  - Generated template projects restore/build in CI.
  - High/critical dependency vulnerabilities fail CI.
  - High/critical Trivy findings fail CI.
  - Template source security rules are enforced by Semgrep.
  - PR Validation enforces security scans on scaffolded templates when template files change.

4. Add image-level container scan in CD ✅ Completed (initial baseline)
- Scope: build AppHost container image in CD and enforce Trivy HIGH/CRITICAL gate.
- Acceptance criteria:
  - CD fails on high/critical image vulnerabilities.
  - CD discovers Dockerfiles dynamically and scans each resulting image.

1. Security regression test suite
- Scope: add focused tests for injection, BOLA/IDOR, and XSS payload safety.
- Acceptance criteria:
  - At least one negative test per high-severity threat row in threat model.
  - Tests run in CI and block regressions.

2. Threat model governance gate
- Scope: PR validation check to ensure threat model update when trust boundaries or endpoint maps change.
- Acceptance criteria:
  - PRs touching security-sensitive paths require threat model diff or explicit exemption.

3. Node action runtime deprecation remediation
- Scope: upgrade pinned actions to Node 24-compatible versions.
- Acceptance criteria:
  - No Node 20 deprecation warnings in workflow logs.

## Suggested Owners

- Luthien: secret scanning, Semgrep, ZAP, Trivy, threat model governance.
- Faramir and Legolas: host/API middleware hardening and test implementation.
- Elrond: enforce checklist and gate consistency in review.

## Tracking

- Keep this backlog synchronized with [docs/security/threat-model.md](docs/security/threat-model.md).
- Promote completed items by marking corresponding threat rows as mitigated.
