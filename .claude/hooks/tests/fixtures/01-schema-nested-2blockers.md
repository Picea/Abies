---
id: reviewer-20260821T000000Z-x
agent: reviewer
scope: review
created: 2026-08-21T00:00:00Z
verdict: NEEDS-CHANGES
targets:
  - path: src/Articles/PublishCommand.cs
    lines: "45-78"
blockers:
  - file: src/Articles/PublishCommand.cs
    line: 52
    reason: "Throws `InvalidOperationException` instead of returning `Result`."
  - file: src/Articles/PublishCommand.cs
    line: 71
    reason: "Mutates `Article` in place rather than returning a new state."
high:
  - file: tests/Articles/PublishCommandTests.cs
    reason: "Missing test for the failure branch."
medium: []
good: []
references:
  - architect-20260415T120000Z-article-state-machine
---

body text
