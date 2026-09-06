---
name: split-prs-when-migration-scope-drifts
description: Migration changesets attract unrelated work; require a split when extra concerns dilute the release-risk assessment
metadata:
  type: feedback
---

Migration work items are unusually prone to scope drift. Require split PRs when
unrelated concerns are bundled in.

**Why:** Observed 2026-05-06, when a Picea version migration arrived carrying CI
policy changes and a large new testing-infrastructure addition in the same
changeset. The problem is not size — it is that a migration's review question is
"what is the release risk of this version change", and every unrelated file
makes that question harder to answer honestly. Reviewers start assessing the
testing infrastructure and stop assessing the migration.

**How to apply:** When a migration PR touches CI config, test infrastructure or
unrelated features, ask for the split before reviewing the substance rather than
after — a conditional approval on a diluted changeset is how the risk assessment
gets skipped. This is specifically about *release-risk* dilution, so it does not
mean every large PR must be split; a large but single-concern migration is fine.
