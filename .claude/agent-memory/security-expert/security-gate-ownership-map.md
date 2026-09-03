---
name: security-gate-ownership-map
description: Which workflow owns which security gate, and why gitleaks and the PR SCA gate each have a single source of truth
metadata:
  type: project
---

Gate ownership, as established by the 2026-04-01 PR security gating audit:

- **Secrets (gitleaks)** — `secrets-scan.yml`, single source of truth. Not
  duplicated into `pr-validation.yml`.
- **SCA (dependency vulnerabilities)** — `pr-validation.yml` is the PR gate,
  scanning both direct and transitive packages. It is the single PR source of
  truth; `cd.yml` runs its own SCA on push to main only.
- **Container image scanning** — `trivy.yml` on PR, plus a Trivy image scan in
  `cd.yml`.
- **SAST** — `codeql.yml` on PR; `semgrep.yml` separately.
- **Heavy scans** — `zap-baseline.yml` (starts services + API + baseline +
  authenticated profile) and `template-security.yml` (packs templates,
  scaffolds, restores, builds, then runs Semgrep and Trivy).

**Why:** Single-source ownership was chosen deliberately. Duplicated gates cost
runner time and queue latency without raising protection, and they make it
ambiguous which failure is authoritative when the two disagree. At audit time
SCA ran on PRs from both `pr-validation.yml` and `cd.yml`; removing the
PR-triggered duplicate cut latency without lowering merge protection.

**How to apply:** Before adding a scan, check whether the gate already has an
owner here and extend that workflow instead of adding a parallel one. Verify
current state before acting — this map has drifted once already.

Verified 2026-09-02: `cd.yml` now triggers on `push` to main only, so the PR SCA
duplication described in the original audit has been resolved. All named
workflows still exist. Related:
[[ci-critical-path-baseline-2026-04]] in the Performance Engineer's memory.
