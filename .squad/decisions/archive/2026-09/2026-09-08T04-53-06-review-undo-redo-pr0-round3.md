---
id: reviewer-reconcile-20260908T045002Z-undo-redo-pr0-round3
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-08T04:50:02Z
commit: 3baa6015d3be3a4461ece23f87150100d63647cb
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-1068"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-32"
blockers:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 2
    reason: >-
      Round 3 is past the cap and the split is degenerate (PR 0 is three files
      the plan requires to land together), so this is an escalation, not a
      rejection. The header states amendment 5 was re-approved on 2026-09-07;
      06-spec.md:2137 records "answered 2026-09-08" and :66-67 agrees. A false
      claim introduced by this changeset into a file the process declares
      immutable, so it is not registrable - but unlike rounds 1 and 2 it costs
      one line either side of the approval commit, by csharp-dev, with no
      spec-author amendment and no re-approval, because the Lock's consequence 3
      assigns csharp-dev's transcription commentary to csharp-dev. The user's
      call is route 1 (correct the date in the working tree, then commit;
      recommended) or route 2 (commit as-is and correct after). What must not
      happen is a third spec-author amendment - no approved fence, assertion or
      fence claim is touched by this round.
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 1659
    reason: >-
      Step 6's Done-when names one csproj item to remove; the tree now carries
      two after this changeset fixed round-2's warning-7 with
      <None Include="History\UndoRedoSpec.cs" />. Left as-is, step 6 removes the
      Compile Remove per its checklist and the None Include survives as an inert
      stale item (no NETSDK1022 - the default None glob excludes the file once
      the Compile glob claims it). Owner realist; inherited-shaped and the clean
      registration for the next changeset.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 1
    reason: >-
      Carried and unchanged since round 1, both satisfied at PR-open and commit
      time rather than registrable. No PR exists for test/0-undo-redo-spec
      (gh pr list --head returns empty) and 04-realist-plan.md:1909 requires the
      body to state the deliberate compile exclusion and the git-history check.
      Working tree also carries a modified .squad/log/2026-09-08-session.md
      alongside the three intended paths, so stage explicitly rather than
      git commit -a.
medium: []
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 301
    reason: >-
      Round-2 blocker 5 closed by execution. The three assertions at :301, :306
      and :381 now use IsEquivalentTo(expected, CollectionOrdering.Matching)
      with using TUnit.Assertions.Enums; verified on the pinned TUnit 1.19.57
      with 17 executed cases and both controls - matching order passes on
      string[], List, IEnumerable, IReadOnlyList, ImmutableArray, ImmutableList
      and ICollection subjects; a permutation fails on all of them; A7's exact
      two-element shape passes forward and fails reversed; length mismatches
      fail. The sweep is wider than the amendment's stated three subject types,
      so no support-file choice for EditorLog.Timeline breaks it and no new Lock
      obligation is created. INV-7's deliberately bare IsEquivalentTo at :1043
      and :1059 is untouched and still order-insensitive - the change did not
      over-apply.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 275
    reason: >-
      Round-2 blocker 6 closed. The paragraph that recorded two falsehoods as
      fact is replaced by measurements beside the assertion, and every claim in
      it was re-executed rather than read - bare IsEquivalentTo order-insensitive
      but content-sensitive; IsEqualTo unsatisfiable on all six named subject
      types on matching content; the bespoke CollectionBuilder probe reproducing
      equals-new=True on the line before the failure; A7's stop-then-start
      passing under bare IsEquivalentTo. All true. The evidence now sits where
      step 6 reads it instead of in a review nobody re-opens.
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    line: 30
    reason: >-
      Round-2 warning-7 fixed rather than registered, and in the codebase's own
      idiom. -getItem:None now returns one item, History\UndoRedoSpec.cs;
      -getItem:Compile still returns 20 without it. <None Include= matches five
      sibling csprojs, the comment explains the lifecycle, and [R4-spec-commit]
      is a real Realist tag (04-realist-plan.md:1508, :1531, :1909) rather than
      an unresolved placeholder. Adding a None item does not pull the file into
      dotnet format's Roslyn workspace, so R-28's registered analysis is
      unchanged.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 564
    reason: >-
      The transcription is verifiable by construction. Re-extracting the six
      approved csharp fences from 06-spec.md (361-897, 987-1082, 1113-1201,
      1244-1291, 1304-1497, 1522-1566 - re-derived, since amendment 5 moved every
      line number) and diffing gives 74 lines containing only csharp-dev's
      30-line header, the 24-line invariant-layer bridge and the relocated
      closing brace. No assertion, no fence claim, no whitespace reflow. The Gen
      stub is again correctly omitted. Build green with the exclusion (0
      warnings) and CS0234-only without it, with nothing at the new
      TUnit.Assertions.Enums using - which the GlobalUsings.g.cs inspection shows
      is required and therefore not at IDE0005 risk. 224/224 existing tests pass;
      git log --all over Picea.Abies.Tests/History/ is still empty, so the Lock's
      git-history check is never waived.
references:
  - .squad/design/undo-redo-pr0/09-review-verdict.md
  - .squad/design/undo-redo-pr0/08-review-blind.md
  - .squad/design/undo-redo/06-spec.md
  - .claude/docs/principles-enforcement.md
  - .claude/enforcement/refutations.md
---

# Review — undo/redo locked spec (PR 0), round 3

Round 3 is past the two-round cap. Two archived drops evidence the count:
`2026-09-07T13-46-42-review-undo-redo-pr0.md` (`commit: 3bd7af3d…`) and
`2026-09-07T14-49-50-review-undo-redo-pr0-round2.md` (`commit: faafc8b1…`),
cross-checked against the `**Round:** n` lines in `09-review-verdict.md`.

Criterion (a) is green — every stated property of the three files as § *The Lock*
defines them was measured this round, not carried. Criterion (c) is green.
Criterion (b) has one item the changeset introduced and therefore cannot register:
a false re-approval date in `csharp-dev`'s own file header.

The cap's remedy is *split, what passes ships*, and the split is degenerate here:
PR 0 is three files the plan requires to land together, and one sentence cannot be
carved out of a file. But the property that made rounds 1 and 2 cap-forcing is
absent — this correction costs one line before or after the approval commit, by
`csharp-dev`, with no `spec-author` amendment and no user re-approval, because
amendment 5 added Lock consequence 3 assigning transcription commentary to
`csharp-dev`. So the cap escalates as a question, not as a rejection.

Full findings, evidence and the two routes: `.squad/design/undo-redo-pr0/09-review-verdict.md`
§ *Re-review — round 3, the split round*.
