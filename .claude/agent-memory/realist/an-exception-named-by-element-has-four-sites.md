---
name: an-exception-named-by-element-has-four-sites
description: A plan exception stated "by file and element" is restated in four places, not three — slip-signal row, precondition block, the step's Done-when, and the delivery PR-cut table; fix all four or the checklist drifts from the tree
metadata:
  type: project
---

When `04-realist-plan.md` authorises a single deliberate exception to an unchanged-files rule, the
exception ends up written out in **four** places, and they drift independently:

1. the File-Level Changes **slip-signal row** (*"if a step touches this, the design has slipped —
   one exception:"*),
2. the tagged **precondition block** in the Todo List preamble,
3. the owning step's **Done-when**,
4. the **delivery PR-cut table** row, which describes what the PR *contained*.

**Why:** in `undo-redo`, PR 0 authorised one csproj item (`<Compile Remove>`); review added a second
(`<None Include>`, for IDE-tree visibility) and merged it. The correction was scoped to three sites,
and site 4 was still describing a one-item commit afterwards — a second round-trip for one line. The
Done-when is the only one of the four that is *executable*, so it is the one that must name every
element; but a stale (4) is what a future reviewer reads as the record of what landed.

**How to apply:** when writing an exception, grep the artifact for the element's own name before
declaring the edit complete — the count of hits is the count of sites, and it is rarely the count
you were given. Two more habits from the same incident: state removals as *"the ItemGroup and
everything in it"* rather than item-by-item, so a later addition is covered without an edit; and
when a Done-when names an artefact to delete, add the failure mode (*"removing only X leaves an
inert stale item, not a build error"*) — a checklist that cannot fail loudly needs the reason
written down.

Related: [[spec-lands-as-pr-zero-behind-an-exclusion]],
[[collected-lists-must-be-a-prefix-of-the-downstream-one]], [[one-route-mitigations-repeat]].
