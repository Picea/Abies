---
name: style-rule-cost-depends-on-checker-scope
description: Before costing a style-rule deviation in a plan step, check what actually enforces it — build severity, and whether the CI formatter is diff-scoped
metadata:
  type: project
---

A plan step that says "the style check will flag this" is a claim about a *checker*, not about
`.editorconfig`. In Abies, verify three things before costing it:

- **No `TreatWarningsAsErrors`, no `EnforceCodeStyleInBuild`** anywhere in `Directory.Build.props`
  or the test csproj — so an IDE-prefixed rule at `:warning` severity does **not** fail the build.
- **The CI enforcer is `dotnet format`**, in `pr-validation.yml`'s `lint-check` job.
- **That job is diff-scoped**: it collects changed `.cs` files with `git diff --name-only` and
  passes them to `dotnet format --verify-no-changes --include $FILES`. A file the PR does not
  touch is never checked, even when the PR is what makes it compile.

**Why:** the `[R4-ns]` decision in `undo-redo/04-realist-plan.md` step 6 turns on a locked spec
file whose `using` placement violates `csharp_using_directive_placement = outside_namespace`. The
PR that removes the file's `<Compile Remove>` changes only the csproj, so CI stays green while an
IDE and a repo-wide `dotnet format` both flag it. Costing it as "PR 3 goes red" would have been
wrong in the direction that matters — it makes the decision look forced when it is actually owed
to the *next* author, which is the argument for taking it deliberately rather than under pressure.

**How to apply:** when a plan step's Done-when rests on a linter, name the enforcer and its scope
in the same sentence as the rule. `reviewer-reconcile` records unexecutable predictions as
predictions; a plan that states the bound saves it the round trip. Related:
[[an-exception-named-by-element-has-four-sites]], [[spec-lands-as-pr-zero-behind-an-exclusion]].
