---
name: pr-blocking-security-minimum
description: Three security gates are non-negotiably PR-blocking — secrets detection, HIGH/CRITICAL dependency vulnerabilities, and at least one code-level SAST signal
metadata:
  type: feedback
---

Three gates stay PR-blocking regardless of how much CI latency pressure exists:

1. **Secrets detection** (gitleaks).
2. **Dependency vulnerability gate** at HIGH/CRITICAL.
3. **At least one code-level SAST signal** — CodeQL or Semgrep — to catch
   injection and authorization patterns before merge.

**Why:** Set 2026-04-01 during the PR security gating audit. These three are the
ones where post-merge detection is materially worse than pre-merge: a leaked
secret is compromised the moment it is pushed, and an injection or authz flaw
merged to main is exploitable. Everything else in the security suite is
defense-in-depth and can be rescheduled.

**How to apply:** When asked to speed up PRs, offer any other scan for
rescheduling but hold these three. "We'll catch it nightly" is a valid answer
for DAST and template scanning; it is not a valid answer for a secret. See
[[path-filter-with-nightly-compensation]] for how to move the rest safely and
[[security-gate-ownership-map]] for which workflow currently owns what.
