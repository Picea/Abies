---
name: tunit-filtering-needs-tree-node-filters
description: TUnit in this repo does not respond to FullyQualifiedName filters — they produce zero-test runs that look like passes
metadata:
  type: feedback
---

Targeted test selection with TUnit in this repo does not work the way
`dotnet test --filter "FullyQualifiedName~..."` suggests. Such filters produce
**zero-test runs**, which exit successfully and look like a pass. Use tree-node
filters, run the full suite, or execute through the TUnit host directly
(`dotnet run --project <TestProject>`).

**Why:** Independently hit twice in late March 2026 — by the Reviewer on
2026-03-26 (FullyQualifiedName filters returning zero tests) and by the JS Dev
on 2026-03-23, who fell back to the TUnit host to validate a single expected
failure. A zero-test run reporting success is the dangerous part: it is
indistinguishable from "your change is fine" unless you read the test count.

**How to apply:** When someone reports "I ran the test and it passed", check the
*test count* before accepting it, especially if they used `--filter`. Note that
several skill docs in `.claude/skills/` still show `dotnet test --filter
"Name~..."` examples; treat those as generic .NET guidance rather than as
validated for this repo's TUnit setup. This has not been re-verified since
2026-03-26 and TUnit's filter support may have changed — confirm before relying
on it either way.
