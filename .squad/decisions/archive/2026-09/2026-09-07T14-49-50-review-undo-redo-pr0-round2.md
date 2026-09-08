---
id: reviewer-reconcile-20260907T144534Z-undo-redo-pr0-round2
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T14:45:34Z
commit: faafc8b179f3069473a4116d9232d1dce9eab8d7
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-996"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-29"
  - path: .squad/design/undo-redo/06-spec.md
    lines: "40-56, 236-249, 314-319, 1696-1730, 1832-1960"
blockers:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 243
    reason: >-
      Assert.That(<collection>).IsEqualTo([...]) compiles on the pinned TUnit
      1.19.57 but can never pass. The collection expression is target-typed to
      a compiler-generated <>z__ReadOnlyArray<string> and compared by Equals,
      which is reference equality, so the assertion fails on the MATCHING
      sequence as well as on a permutation. Verified with both controls in a
      1.19.57 scratch project, and swept across every plausible support-file
      return type for EditorLog.Timeline (string[], List<string>,
      IEnumerable<string>, IReadOnlyList<string>, ImmutableArray<string>,
      ImmutableList<string>) - all six fail on identical content. Also verified
      against a bespoke [CollectionBuilder] type with correct IEquatable<T>
      value equality, which fails while the subject's own Equals returns True,
      proving no support-file choice closes it. Amendment 4 replaced an
      assertion that passed for the wrong reason (order-insensitive
      IsEquivalentTo) with one that fails for no reason, so step 6's
      obligation - observe every test red for the right reason, then make it
      green - can never be discharged for A6. This is a regression the
      changeset introduces, not an inherited gap, so it is not registrable.
      Two shapes that do work on 1.19.57 were executed with passing and
      failing controls: IsEquivalentTo(expected, CollectionOrdering.Matching)
      (using TUnit.Assertions.Enums) and
      Assert.That(actual.SequenceEqual([...])).IsTrue(). Which one lands is
      the spec-author's and the approver's call.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 247
    reason: >-
      Second instance of the same defect, A6's bare-program expectation. Same
      evidence, same route. Named separately because a fix applied only at the
      wrapped assertion would leave the test still unable to pass.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 318
    reason: >-
      Third instance, A7's subscription-activity ordering claim
      (EditorLog.SubscriptionActivity IsEqualTo ["start:...", "stop:..."]).
      Same evidence, same route.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 239
    reason: >-
      The locked comment states two false claims about TUnit 1.19.57 and cites
      the round-1 review as their authority: that IsEqualTo on a collection is
      order-sensitive on this version, and that
      IsEquivalentTo(expected, CollectionOrdering.Matching) does not exist
      here. Measured now - IsEqualTo on a collection is not order-sensitive,
      it is unsatisfiable, and CollectionOrdering DOES exist on 1.19.57 in
      namespace TUnit.Assertions.Enums (round 1's CS0103 was a missing using
      directive misread as an absent type; strings on TUnit.Assertions.dll
      lists CollectionOrdering, IsEquivalentToAssertion`2 and
      CollectionIsInOrderAssertionExtensions). Decision 1 makes the file
      immutable in its assertions AND ITS CLAIMS from the approval commit, so
      correcting this paragraph after the commit is itself a hand-back. This
      is not a duplicate of the assertion blockers: the paragraph would still
      be present and still wrong if the three assertions were fixed alone.
      :314-317 carries the same claim in shorter form. Both round-1 errors
      were mine - the amendment cites my executed probe as its reason for
      choosing this shape - and both are recorded in the verdict's opening
      section.
high:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 54
    reason: >-
      [Timeout(30_000)] emits warning TUnit0015 (Missing TimeoutAttribute
      cancellation token parameter) on all 25 parameterless test methods,
      verified by probe on 1.19.57. Not fatal - no TreatWarningsAsErrors in
      Directory.Build.props, the test csproj or any workflow - so step 6 gains
      25 warnings where round 1 recorded a clean build. Also measured: the
      timeout DOES fire on an await-shaped body without a CancellationToken
      parameter (a [Timeout(2_000)] test awaiting Task.Delay(6_000) failed at
      2 s), so Decision 3 buys what it was meant to buy for these await-dense
      seed loops; it does NOT fire on a synchronous spin (a 6 s spin passed);
      and the timed-out body keeps running after the failure is reported,
      which on a class built on the process-wide static EditorLog will corrupt
      whichever test runs next - [NotInParallel] does not prevent this because
      the orphan is a detached continuation, not a test. The remedy for the
      warning is a signature change, which is neither an assertion nor a
      claim, so on Decision 1's wording step 6 could make it without a
      hand-back; that reading is not obvious enough to leave implicit.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 986
    reason: >-
      A seventh support-file obligation, absent from amendment 4's table of
      six. INV-7's assertion (4),
      Assert.That(EditorLog.ReportedKeySetsDuringHold.Distinct())
      .IsEquivalentTo([anchorKeys]), compares ELEMENTS with Equals, so a
      sequence-of-sets fails whenever the elements are reference-equality
      types (HashSet<string> and string[] elements both fail on identical
      content) and passes when they have value equality (a record element -
      matching passes, non-matching fails, both controls run). Round 1 cleared
      this site as order-insensitive by intent, which was right about ordering
      and silent about element equality. This is an obligation on the UNLOCKED
      support files rather than a lock defect - Keys(...) and
      ReportedKeySetsDuringHold must yield a value-equality element type - so
      it belongs in 06-spec.md section The Lock as item (g), beside (c) and
      (d). SingleReconciliation(...) at :971 is safe if it returns a flat
      IEnumerable<string> and has the same problem if it returns a set of sets.
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    line: 23
    reason: >-
      Round 1's warning-7, carried, unfixed and unregistered - the only
      round-1 finding in that state. Re-measured on this working tree rather
      than carried on trust - getItem:None returns {"None": []} and
      getItem:Compile returns 20 items, with History/UndoRedoSpec.cs in
      neither. The SDK's default None glob subtracts the Compile glob PATTERN,
      not the resulting item list, so a Compile Remove of a .cs file lands it
      in no item group and neither Visual Studio nor Rider shows it without
      Show All Files, for the two PRs during which being looked at is its
      entire purpose. 06-spec.md section Amendment 4 assigns it to csharp-dev.
      Merge criterion (b) is unmet on this item alone.
  - file: .squad/log/2026-09-07-session.md
    line: 1
    reason: >-
      Round 1's warning-12, still open. git status shows this tracked file
      modified alongside the three intended paths, and the PR's stated
      criterion is three files and nothing else, so the committer must stage
      explicitly rather than use git commit with the all flag. Process
      caution, not a code defect. Related, also still open - no PR exists for
      branch test/0-undo-redo-spec (gh pr list --head returns empty), so
      round 1's warning-11 precondition on the PR body stating that the spec
      is deliberately excluded until step 6 remains unmet by absence.
medium: []
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 591
    reason: >-
      Round-1 blocker 3(b) fixed by making the code true to the claim rather
      than the claim true to the code. INV-2 now walks the script under both
      lenses against one reachable set computed once, with distinct Because
      text per lens, and the falsifier table gains a projection-specific
      mutation. The whole-model lens is the half that holds by construction,
      so adding the projection lens added the half that can actually fail.
      Decision 2 kept INV-6's feedback interpreter on exactly the same
      reasoning. Both were open invitations to take the cheaper route.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 931
    reason: >-
      Round-1 blocker 3(a) fixed, and implementing it immediately exposed that
      BlockedByWorld was unreachable in either direction under NoFeedback, so
      the ten-combination claim could never have been met. That is the case
      for named falsifiers being code rather than prose, made by the artifact
      itself. Verified: the coverage assertion compiles and passes on 1.19.57,
      the observed.Add call at :885 is unguarded, and GetType().Name matches
      nameof for nested record cases.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 531
    reason: >-
      All four reached floors (INV-1, INV-3, INV-4, INV-5) are placed after
      the last continue guard and before the first assertion. Getting this
      wrong is easy and silent, and would have made the floor decorative.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 1
    reason: >-
      The header is fixed rather than hedged - round 1's finding was a false
      sentence, and the response was to rewrite it as csharp-dev's own
      accurate text instead of appending a disclaimer. Every claim in it
      checks out, including the CS0234 behaviour, which I verified by
      temporarily deleting the Compile Remove and restoring the csproj
      byte-identical afterwards.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 344
    reason: >-
      The transcription is verifiable by construction. Re-extracting
      06-spec.md's six csharp fences (344-829, 917-1014, 1043-1133, 1174-1223,
      1234-1429, 1443-1489), reassembling and diffing against the working-tree
      file yields 87 diff lines containing no assertion, no claim and no
      whitespace reflow - the 18-line header, a 15-line bridging comment, five
      blank lines and the class's closing brace moved to the end of the file.
      The Gen stub is again correctly omitted. Zero formatter damage this
      round, because the exclusion was in the csproj before the file was
      written.
  - file: .claude/enforcement/refutations.md
    line: 777
    reason: >-
      R-27 and R-28 are real registrations - each carries an owner, an
      expires date of 2026-09-21, and a level consequence written from
      executed evidence, and R-27 argues explicitly why it is neither R-19
      (unmediated Bash) nor R-20 (worktree/symlink deny-closure), which is the
      part that usually gets skipped.
references:
  - .squad/design/undo-redo-pr0/09-review-verdict.md
  - .squad/design/undo-redo-pr0/08-review-blind.md
  - .squad/design/undo-redo/06-spec.md
  - .squad/design/undo-redo/05-critic.md
  - .squad/design/undo-redo/07-handoff.md
  - .claude/docs/principles-enforcement.md
  - .claude/enforcement/refutations.md
---

# Review verdict - undo/redo locked spec (PR 0), round 2

Working tree on `test/0-undo-redo-spec`, uncommitted, over HEAD `faafc8b`. Round 2 of 2
before the cap; a third round splits the changeset under
`principles-enforcement.md` section *The Merge Criterion*.

## What was settled

Amendment 4 fixed six of the seven round-1 findings that were the spec's to fix, and fixed
the two that mattered most in the harder direction - adding the projection lens to INV-2
rather than deleting the sentence that claimed it, and keeping INV-6's feedback interpreter
rather than narrowing the coverage claim to what the old fixture could reach. The
transcription is byte-faithful to the six approved fences and the build claims check out.

## What blocks

The remedy amendment 4 adopted for round-1 blocker 2 does not work.
`Assert.That(<collection>).IsEqualTo([...])` compiles on TUnit 1.19.57 and fails on the
matching sequence, across every plausible subject type and even against a bespoke
value-equality collection type. Three assertions in the locked text - A6 twice, A7 once -
went from passing for the wrong reason to failing for no reason, which is strictly worse
in this process because step 6 cannot make them green. The locked comment that explains
the change states two false claims about TUnit and cites the round-1 review as their
authority.

## The error was mine, and it is recorded

Round 1 told the spec-author that `IsEqualTo` on a collection is order-sensitive (I ran only
the negative control - a permutation failing is equally consistent with "always fails") and
that `CollectionOrdering` does not exist on this version (I read a `CS0103` missing-using
error as an absent type). The amendment states that it chose this shape because the reviewer
had already executed it. Every claim in this round carries a passing control and a failing
control.

## The route

Same as round 1: a `spec-author` amendment with user re-approval, because the defects are in
approved text and `csharp-dev` may not edit it. PR 0 is still the approval commit, so this
is still the cheap moment - and it is the last one, because round 3 hits the cap and the
split never ships a red stated property.
