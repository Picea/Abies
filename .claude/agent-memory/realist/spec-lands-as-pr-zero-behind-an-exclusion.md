---
name: spec-lands-as-pr-zero-behind-an-exclusion
description: A locked spec test that cannot compile yet lands as its own PR 0 behind a <Compile Remove>, not as the first commit of the PR that greens it
metadata:
  type: project
---

When a plan locks a spec test that **cannot compile until a mid-plan step**, the landing shape is:
its own **PR 0** — the spec file plus any support attribute, plus a `<Compile Remove="…" />` in the
*test* project's csproj — with the exclusion removed in the step that makes it compilable, listed
there as an expected change.

**Why:** the alternative I proposed first (land it as the first commit of the PR that greens it)
trips `reviewer-reconcile`'s git-history check — spec modified in the same PR that brings it to
passing — and pays for it with a sentence in the PR body asking the reviewer to disregard the flag.
The user overruled that on `undo-redo` 2026-09-07: *nothing in a PR body tells a reviewer to ignore
a check.* A waiver requested once is a waiver available always, and the check's value is that it
is not negotiable from inside the change it checks. Two PRs carrying a deliberately excluded test
file is the cheaper cost, and it is visible in one csproj line.

**How to apply:** when a plan has a spec phase and the spec's dependencies land at step N > 1, plan
the spec commit as PR 0 up front rather than leaving the timing as a handoff open item. Two
consequences must be written into the plan or the exclusion becomes invisible debt: step N's
Done-when names the removal as expected, and the "touching a csproj is a slip signal" rule (most of
my plans have one) names this exception **by file and by element**, so no other csproj touch can
shelter behind it. Note the test project is usually not in the untouched-files list at all — write
the exception where the reader applies the rule, not where the file happens to be listed.

Related: [[collected-lists-must-be-a-prefix-of-the-downstream-one]] — same failure family, two
artifacts disagreeing about what one sentence means.
