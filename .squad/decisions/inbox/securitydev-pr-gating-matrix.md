### 2026-04-01: Security PR Gating Matrix and Trigger Realignment
**By:** Security Expert (requested by Maurice)
**What:** Keep critical security gates on PR, move heavyweight DAST/template scans to push-main + nightly with path-filtered PR exceptions, and de-duplicate overlapping SCA checks.

#### Proposed gating baseline
- PR must block on:
  - Secrets scan (gitleaks)
  - SCA HIGH/CRITICAL (direct + transitive)
  - One mandatory code SAST gate (CodeQL or Semgrep)
  - Trivy HIGH/CRITICAL filesystem + Dockerfile gate for changed relevant areas
- Push/main and nightly should run:
  - Full CodeQL + Semgrep full-repo
  - Full Trivy (all scanner types)
  - ZAP baseline + authenticated profile
  - Template security scaffold scans

#### Why
- Preserves fast fail for exploit-critical findings before merge.
- Reduces PR cycle time by moving environment-heavy scans out of every PR.
- Maintains defense-in-depth by requiring full scheduled scans as compensating controls.

#### Practical repo changes implied
- Keep `pr-validation.yml` `security-scan` as PR authority for gitleaks + SCA HIGH/CRITICAL.
- Remove/disable duplicate PR vulnerability gate in `cd.yml` or limit CD workflow to push main.
- Add `paths` filters to `zap-baseline.yml` and `template-security.yml` so PR runs only for security-relevant file changes.
- Keep nightly schedule for ZAP/template scans and retain main push runs.

#### Risk and compensating controls
- Risk: delayed detection for lower-severity or broad runtime issues moved off PR.
- Controls: nightly full scans, SARIF trend review, strict SLA for fixing scheduled findings, and emergency re-run labels for risky PRs.
