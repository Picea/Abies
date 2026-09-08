---
id: reviewer-reconcile-20260907T120325Z-presentation-demo
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T12:03:25Z
commit: 07607bf71152ef8d224352e2d2a737d8312dcb2d
targets:
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12-19"
  - path: Picea.Abies.Presentation/content/demo/README.md
  - path: Picea.Abies.Presentation/content/demo/stops/5.1-warden-report.md
  - path: Picea.Abies.Presentation/content/demo/stops/5.2-hook-line.sh
blockers:
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    line: 17
    reason: >-
      Merge-blocking. The <None Remove> item strips the 42 None items that populate the IDE
      file tree, making a folder meant to be presented from invisible in Visual Studio and
      Rider. Measured: with <Compile Remove> as the sole guard the build succeeds,
      ComputeFilesToPublish yields 0 entries under content/demo, and Content/EmbeddedResource
      match 0 items either way - so all three stated goals (never compiled, embedded, or
      published) are met by one line, and the other three Remove items are a no-op or a net
      cost. Fix: reduce the ItemGroup to the Compile Remove line alone.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Talk-blocking, per CLAUDE.md's rule that Picea.Abies.Presentation factual claims
      require reviewer-reconcile sign-off before a talk ships. Rule: every shipped fragment
      that contradicts another shipped file, and every README rationale that asserts more
      than its cited span contains, must be annotated to the standard the README already
      applies elsewhere. Four instances, non-exhaustive: (a) 5.1-warden-report.md:3 says the
      gate checked a 153-line scope while the shipped full/00-scope.md is 254 lines, and its
      "(line 99-101)" citation resolves to unrelated prose - the quoted sentence is at 221;
      (b) the same fragment is truncated one line before the R-21 conclusion while the README
      flags exactly that defect on 5.2-track-b-summary.md; (c) the 5.2-hook-line.sh rationale
      claims the span contains the refuse-for-everyone fallback when the span holds only the
      UNREADABLE signal - the fallback is at enforce-track-blindness.sh:422, outside it;
      (d) both 5.6 fragments cite undo-redo-design-record sources absent from full/ and
      unmentioned in the exclusions note.
high:
  - file: Picea.Abies.Presentation/content/demo/
    reason: >-
      No divergence detector for a snapshot of living files. full/refutations.md copies an
      appended-to ledger, and the full/* artifacts copy files the design process overwrites
      in place. The "unedited copies" claim is true today - I verified all 19 byte-identical
      at 07607bf - but degrades to asserted the moment anyone edits full/. A SHA256SUMS
      generated at snapshot time would keep the claim checkable forever, including by
      reviewer-blind, which is denied .squad/design/ by hook and could never otherwise
      verify it.
  - file: Picea.Abies.Presentation/content/demo/stops/5.4-critic-verdicts.md
    reason: >-
      Composite fragments carry no in-file provenance. Five spans from four files, ending in
      two bare "## Verdict" headings from different passes of the same document with no
      separator. All five spans verified exact with no authored text added, so the fragments
      are honest - but the attribution lives only in the README table, and slide content is
      precisely what gets separated from its index. An HTML comment naming the spans costs
      nothing.
medium:
  - file: Picea.Abies.Presentation/content/demo/stops/5.2-hook-line.sh
    reason: >-
      Extension misleads on a projector: the content is Python lifted from a heredoc, so any
      highlighter renders it as shell. Likewise timing/hooks-fired.log is markdown named
      .log, and it is the one authored (not copied) file in the bundle. Renaming
      5.5-property.cs would additionally dissolve the csproj blocker entirely, since that
      one extension is the only reason the build guard is needed.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Off-by-one: full/refutations.md:722 is cited for R-25, which is at 723 (722 is blank).
      The neighbouring R-24 citation at :707 is exact. Trivial except that line-precise
      citation is this artifact's entire premise.
  - file: .github/workflows/pr-validation.yml
    reason: >-
      Informational, no action required. check-pr-size hard-fails (10,151 lines,
      maintenanceOnly false due to six non-.md fragments plus the .csproj), but it is not a
      required status check - the PR shows UNSTABLE, not BLOCKED, and squash-merges normally.
      Renaming fragments would not help, since the .csproj alone keeps maintenanceOnly false.
      The real cost is detect-changes setting docs_only=false, running the full CI matrix on
      a documentation change. Worth a line in the PR description so the red X reads as
      priced-in rather than missed.
good:
  - file: Picea.Abies.Presentation/content/demo/full/
    reason: >-
      All 19 whole-file copies byte-identical to their sources, verified twice - against the
      working tree and against git show 07607bf. The blind reviewer could verify 2 of 19; the
      remaining 17 are now confirmed. The change's central claim holds completely.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Refuses to fabricate. Three requested fragments were not created because the text does
      not exist at 07607bf, each row recording what was searched and the nearest genuine
      quote; a PR #358 decision drop is excluded with a stated reason. Index integrity
      confirmed by comm - 20 files, 23 rows, the 3 orphan rows are exactly the not-created
      three, zero orphan files.
  - file: Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
    reason: >-
      Declares itself compiled-not-verbatim in its own header, separates hook runs from
      subagent runs, and carries a "Not evidenced at all" section. Contrary to the blind
      reading, it does not overclaim the warden's line count - it attributes the 254-line
      figure to the mechanical scan only, which the surviving scan file supports.
references: []
---

Every verbatim claim this change makes is true - all 19 full/ copies and all 21 stops fragments verified byte-exact at the pinned commit - but the csproj guard hides the tree from the IDE it is meant to be presented from, and the index leaves four factual discrepancies unannotated to a standard it meets everywhere else.

## Reconciliation with the blind reading

The blind reviewer's headline finding is resolved and partly withdrawn. The 153-vs-254 line
discrepancy is in the source at 07607bf, not introduced here: `00-warden.md:3` says 153,
`00-scope.md` is 254, and `00-warden-scan.md:9` says 254 - the bundle copies all of them
faithfully. And `hooks-fired.log` does not make the contradictory claim attributed to it; it
attributes the 254-line figure to the mechanical scan only, which is correct.

What survives is a presentation hazard rather than an error: the warden report's
`(line 99-101)` citation resolves to unrelated prose in the shipped scope (the sentence is at
221), the 153-line scope was never committed so those citations are permanently
unverifiable, and the README is silent on both.

## Suggested fix

Reduce the csproj ItemGroup to the `<Compile Remove>` line alone - proven sufficient by
measurement - and add four annotations to README.md per the rule stated in the second
blocker. Neither fix touches the bundle's substance; the copies and excerpts are sound.

Full verdict: `.squad/design/presentation-demo/09-review-verdict.md`
