---
id: reviewer-reconcile-20260907T134423Z-undo-redo-pr0-locked-spec
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T13:44:23Z
commit: 3bd7af3d7e5cc79bb194912c21d45de692557e6c
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-852"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-28"
blockers:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 787
    reason: >-
      INV-6's Available case does not compile on TUnit 1.19.57 -- verified by
      independent build: CS1061, OrContinuation<int> has no member That. TUnit's
      .Or continues on the same subject, so a disjunction across two values is
      not expressible this way at all. Origin is 06-spec.md:1186, the approved
      text, so the route is a spec-author amendment with user re-approval, not a
      csharp-dev fix.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 7
    reason: >-
      The nine-line header this changeset adds states that the file's
      non-compilation is "expected: the types it exercises ... do not exist
      yet". False -- line 787 will still not compile after step 6 delivers every
      named type and fixture. The header is NOT approved spec text (it appears
      zero times in 06-spec.md), so this sentence is the changeset's own and
      blocks on its own account, independently of what happens to line 787.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 197
    reason: >-
      A6's two IsEquivalentTo expectations are permutations of the same four
      strings, and IsEquivalentTo is order-insensitive -- verified by execution:
      the bare expectation passes against the wrapped actual. A6's entire stated
      purpose is to record an ORDERING difference between the wrapped and
      unwrapped programs, so it cannot fail for the reason it exists.
      CollectionOrdering does not exist on 1.19.57 (CS0103), so the remedy is a
      different assertion, not an option flag. Origin is 06-spec.md:505 and :509.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 800
    reason: >-
      INV-6's closing comment claims "the property asserts that coverage at the
      end of the loop rather than assuming the generator found them". It does
      not -- the body ends at the seed loop's closing brace. This coverage
      assertion IS the named falsifier that 06-spec.md's The Lock relies on to
      close the unlocked-support-file hole, so the Lock's own stated closure is
      unbuilt for this property. With the unguarded continue guards at :490,
      :492, :642, :649, :699 and :732, INV-1, INV-3, INV-4, INV-5 and INV-6 can
      all pass having asserted nothing.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 528
    reason: >-
      "INV-2 runs twice, over Editor (whole-model) and ProjectedEditor" is false
      against the code -- both INV-2 properties start Editor (:517, :560), and
      the second one's own comment at :555 says "Whole-model policy
      DELIBERATELY". ProjectedEditor appears once in the file, at :315, in A8.
      Verbatim from 06-spec.md:908. A third claim at :616-620 says the property
      "is not done until both branches have been observed taken at least once";
      neither the Past nor the Future branch is counted.
high:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      The lock imposes at least six obligations on the unlocked support files
      and the narrative's stated closure covers one. (a) a global using static
      for ~13 unqualified fixture calls, since the class is sealed, non-partial
      and has no base type; (b) EditorLog must be per-test-isolated; (c)
      EditorLog.Timeline must return a snapshot, not the backing list (:190-195
      captures, clears, captures); (d) Start<Editor>() must reset the static log
      (A1 asserts emptiness at :52 with no Clear()); (e) Gen.Corpus must reach
      all five availability cases in both directions, because INV-6's coverage
      assertion is missing; (f) EditorProgram.Transition(MovementRefused) must
      issue no command -- 04-realist-plan.md:1120's S7 transparency rule means a
      refusal records no step, but pass() at :1128-1131 still calls sealTops
      when the command is not silent, and INV-5 snapshots Both() once at :730
      while INV-6 re-reads inside its loop at :766.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      No [NotInParallel] on a class of 25 tests built on a static EditorLog with
      twelve Clear() calls and emptiness assertions at :52, :149-151, :247-248,
      :708-710 and :773-775. Measured: no assembly-level parallel config and no
      .runsettings in this project; TUnit defaults to parallel; six sibling
      classes carry class-level [NotInParallel] and NavigationTests.cs uses the
      method-level form four times. The attribute site is inside the lock, so
      the escape is an AsyncLocal-backed EditorLog -- a step-6 choice to make
      deliberately rather than discover when the suite goes flaky.
  - file: .claude/hooks/dotnet-format-on-save.sh
    reason: >-
      PRE-EXISTING FRAMEWORK GAP, register for devops. The hook fires on every
      Write/Edit of any .cs file with no exclusion for a file the process
      declares immutable, and runs the analyzer-fix pass including IDE0005
      (.editorconfig:268, severity warning) -- which is how "using
      Picea.Abies.History;" was stripped on first write. The line is present now
      (:13). Every whitespace divergence between the committed file and
      06-spec.md is explained by .editorconfig:80,
      csharp_preserve_single_line_statements = false. Currently neutralised by
      the Compile Remove -- verified: dotnet format --include reports "Formatted
      0 of 27 files" and leaves the file byte-identical -- and that protection
      disappears when PR 3 removes the exclusion.
  - file: .claude/hooks/enforce-review-blindness.sh
    reason: >-
      PRE-EXISTING FRAMEWORK GAP, register for devops. Picea.Abies.Presentation/
      content/demo/full/ carries a byte-identical copy of the whole design pass
      (md5 verified for 00-scope, 03-convergence, 04-realist-plan, 05-critic,
      06-spec, 07-handoff) plus decision-drops/ including two previous review
      verdicts, outside the hook's .squad/design/** deny list. Verified by
      firing the hook with a reviewer-blind payload -- exit 0 for the demo
      copies of 06-spec.md, 05-critic.md and the review-pr359-round2 drop, exit
      2 for .squad/design/undo-redo/06-spec.md. This is a second PATH reachable
      by the MEDIATED tools, so it is distinct from R-19 (Bash unmediated) and
      R-20 (worktrees, symlinks); closing R-19 entirely would leave it standing.
      Committed at 7d32cdb (PR #360, merged) -- not this changeset's fault.
      Unregistered: grep for "Presentation" and "content/demo" in
      .claude/enforcement/refutations.md returns nothing.
medium:
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    reason: >-
      History/UndoRedoSpec.cs is in no MSBuild item group -- measured, Compile
      has 20 items without it and None has zero items in this project. The SDK's
      default None glob subtracts the Compile glob PATTERN, not the resulting
      item list, so a Compile Remove of a .cs file lands it nowhere and IDEs
      hide it without Show All Files, for the two PRs during which its whole
      purpose is to be looked at.
  - file: Picea.Abies.Presentation/content/demo/stops/5.5-property.cs
    reason: >-
      INV-3 now exists in three copies -- 06-spec.md, this checksummed
      presentation stop (SHA256SUMS line 36), and the locked file -- and they
      already disagree: the presentation copy carries the pre-format spelling.
      A correction to INV-3 has three homes and a checksum to refresh.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      PR-body precondition unmet because no PR exists yet -- the plan's PR 0 row
      and 07-handoff.md section 6 both require the body to state the deliberate
      exclusion. Separately, .squad/log/2026-09-07-session.md and
      .squad/log/pass-cost.md are tracked, not ignored (git check-ignore exits
      1) and modified, so staging everything at once would breach the stated
      "three files and nothing else" criterion.
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      The PR-0 shape delivers the property it was designed for -- git log --all
      on the path returns nothing, so the Lock's git-history check is satisfied
      without a waiver, which is exactly the argument the plan and handoff make
      for this shape over landing the spec as PR 3's first commit.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      Every Critic mitigation owed to this file landed. S27's alphabet partition
      matches 06-spec.md's table property-for-property across all eight
      properties (:475, :511, :559, :637, :692, :725, :759, :814); S28's
      WhenApplied<T>().WaitAsync is used throughout with no sleeps anywhere,
      continuing ca2519d; S25(a) is A7's third test; S26 is A3's preserved
      forward branch; B10's scoping is stated twice; and 7-yellow is A10.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      A9, A10 and A7's third test each name their falsifier in prose AND assert
      it -- A_terminated_program_with_no_history_left_is_terminal (:385-399)
      exists specifically to stop IsTerminal being hardcoded false. That is the
      discipline the INV-2 and INV-6 blockers find missing.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      The stripped "using Picea.Abies.History;" was caught and restored rather
      than left to pass silently. That is the behaviour that makes the
      format-on-save gap registrable instead of a post-mortem.
references: []
---

PR 0's shape is correct and its two compile-verified defects are in the approved spec text, not the transcription -- so the fix needs a spec-author amendment and user re-approval, and this commit is the last moment at which that is cheap.

## What was settled

I extracted every csharp fence from `.squad/design/undo-redo/06-spec.md` and diffed the reassembly
against the committed file. `.Or.That(...)` is `06-spec.md:1186`; A6's two `IsEquivalentTo` calls are
`:505` and `:509`. Verbatim. The transcription is faithful; the approved text is not correct.

`07-handoff.md` section 6.1, item 4-yellow states the consequence: *"After the approval commit a move
is a `// SPEC CONFLICT:` hand-back and a re-approval, not a relocation."* PR 0 **is** the approval
commit.

## The route is the user's

Two options, and I am not choosing between them: amend and re-approve before committing, or commit
as-is and accept that PR 3 opens with a hand-back on the pass's most load-bearing file. The header
sentence at `:7-9` is the exception -- it is not approved text and is `csharp-dev`'s to correct
either way.

## Where the blind reading and the narrative diverged

08 upheld on the two compile findings and on the missing falsifiers, and extended -- I found a third
false coverage claim it did not (`:528`, "INV-2 runs twice ... and ProjectedEditor"). 08's P7 fear is
refuted by `04-realist-plan.md:1120`'s S7 transparency rule and replaced by a narrower unstated
fixture obligation. 08's P4 reading of the `Picea.Abies.Presentation` precedent is half-wrong -- that
project has 45 `None` items and its comment is true for the `.md` files it exists to keep
presentable. 08's P10 slug complaint is refuted: `undo-redo` is the design pass, `undo-redo-pr0` is
this review's slug. 08's test count is off by four in the acceptance layer -- 25 total, not 21.

Full verdict: `.squad/design/undo-redo-pr0/09-review-verdict.md`.
