---
name: path-filter-with-nightly-compensation
description: Heavy scans may be narrowed by path filter or diff only while a full scheduled scan remains enforced as the compensating control
metadata:
  type: feedback
---

Narrowing a heavy security scan is acceptable only with a compensating full
scan:

- Run template security only when template/framework/packaging inputs change.
- Run ZAP only when API, auth, HTTP middleware or routing changes.
- Keep full-repo scans on a schedule (push to main / nightly) as the
  compensating control.
- Diff-based Semgrep targeting on PRs is acceptable **only** while the nightly
  full scan stays enforced.

**Why:** From the 2026-04-01 audit. A path filter is a bet that the filter is
correct, and path filters are wrong in exactly the cases that matter — a
vulnerability introduced through a file nobody predicted would be relevant.
Diff-based scanning has the sharper version of this problem: it cannot see a
flaw that arises from the *interaction* between changed and unchanged code. The
scheduled full scan is what makes the bet survivable.

**How to apply:** Treat "path filter" and "nightly full scan" as a single
change — never merge the narrowing without the compensating scan already in
place and enforced. If someone later proposes dropping the nightly to save cost,
that also un-approves the path filter. The three gates in
[[pr-blocking-security-minimum]] are out of scope for this technique.
