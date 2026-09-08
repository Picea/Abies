# ⚖️ Review Verdict — undo/redo locked spec (PR 0), uncommitted working tree on `test/0-undo-redo-spec`

**Blind assessment:** `08-review-blind.md`
**Reviewed:** working tree at `3bd7af3d7e5cc79bb194912c21d45de692557e6c`
**Round:** 1
**Verdict:** 🔴 Changes Requested

---

## Reconciliation

### The question the dispatch asked me to settle, settled

**Both compile-verified findings originate in the approved spec text, not in the transcription.**
I extracted every `csharp` fence from `.squad/design/undo-redo/06-spec.md` (lines 317-761, 777-817,
849-906, 929-994, 1033-1075, 1086-1197, 1205-1251) and diffed the reassembly against the committed
file. `.Or.That(...)` is `06-spec.md:1186`. A6's two `IsEquivalentTo` calls are `06-spec.md:505` and
`:509`. The third is `:575`. Verbatim.

**Consequence, and it is the whole verdict:** the fix is a `spec-author` amendment with user
re-approval under the spec-by-example rule, **not** a `csharp-dev` correction. `07-handoff.md`
§ 6.1 🟡 4 states the cost of getting this wrong in so many words — *"After the approval commit a move
is a `// SPEC CONFLICT:` hand-back and a re-approval, **not** a relocation."* PR 0 **is** the approval
commit. This review is the last moment at which either defect is cheap.

### Where 08 and the narrative diverged, and what the code said

| 08's reading | Narrative's claim | What I measured | Outcome |
|---|---|---|---|
| P1 `.Or.That` will not compile | header: non-compilation is *"expected: the types … do not exist yet"* | independent build against TUnit 1.19.57 → `CS1061: 'OrContinuation<int>' does not contain a definition for 'That'` | **08 upheld; the narrative's header is false** (see 🔴-1) |
| P2 A6 is vacuous | `06-spec.md` § A6: *"both asserted AGAINST the unwrapped program so the difference is recorded as a difference"* | executed: `IsEquivalentTo` passes on a permutation; `CollectionOrdering` → `CS0103`; `IsEqualTo` on a collection **is** order-sensitive (fails as expected) | **08 upheld** |
| P7 INV-5 may compare a stale availability; *"I cannot settle this without the implementation"* | — | `04-realist-plan.md:1120` settles it: `if e is MovementRefused -> pass(h, next, cmd, origin)` (S7 transparency). A refusal records no step and supersedes nothing. | **08's fear refuted, a narrower one survives** — `pass` at `:1128-1131` still calls `sealTops(SealedByEffect(...))` when the command is not silent. Downgraded to ⚠️-5(f) |
| P4 the precedent's comment *"the files stay visible as None items"* misdescribes it; *"I verified they are in neither `None` nor `Content` there either"* | — | `-getItem:None` on `Picea.Abies.Presentation`: **45** `None` items, including all of `content/demo/full/*.md`; **0** `.cs` files | **08 half-wrong.** The comment is true for the `.md` files it exists to keep presentable and false only for its one `.cs` file. The new csproj copies the mechanism and does **not** repeat the claim — credit, not a fault |
| P5 two properties document falsifiers they do not implement | `06-spec.md` § The Lock: the support-file hole is closed by *"every property carries a named falsifier"* | confirmed, **plus a third 08 did not find** (🔴-3(b)) | **08 upheld and extended** |
| *"21 `[Test]` methods … acceptance layer (A1–A10, **13** tests)"* | — | `grep -n '\[Test\]'` → **25** total: 17 acceptance (lines 22–414), 8 invariant (469–807) | **08's count is off by four in the acceptance layer.** P3's blast radius is 25 tests on one static log, not 21 |
| P11 `SpecAttribute` is dead code, *"Why `[Spec]` exists"* unresolved | `07-handoff.md` § 4: *"It exists for `reviewer-reconcile`'s grep, not for execution"*, flagged as a declared plan file-table amendment | narrative answers it; approval trail intact. I also probed `[Property("Spec", …)]` at class level — it **compiles** on 1.19.57, so the type is avoidable | **narrative accepted; downgraded to 💡** |
| P10 the csproj cites slug `undo-redo`, *"not the slug this review was dispatched under (`undo-redo-pr0`)"* | — | `.squad/design/undo-redo/` is the real design pass; `undo-redo-pr0` is this **review's** slug and holds only `08`/`09` | **08 refuted.** The citation resolves correctly |

### Claims I verified and upheld

- **The `using Picea.Abies.History;` is present** (`UndoRedoSpec.cs:13`). The `csharp-dev`'s report of
  the strip-and-restore is corroborated mechanically, not merely accepted: `.editorconfig:268`
  sets `dotnet_diagnostic.IDE0005.severity = warning` (*Remove unnecessary using directive*), and
  while the file was in the compile set with no `Picea.Abies.History` namespace, that is exactly the
  diagnostic `dotnet format`'s fix pass would have applied. See ⚠️-4.
- **The git-history check the whole PR-0 shape exists to protect is genuinely satisfied.**
  `git log --all -- 'Picea.Abies.Tests/History/*'` returns nothing — the spec has never appeared in
  history, so the check never has to be waived, which is precisely the property
  `04-realist-plan.md` ~`:1554` and `07-handoff.md` § 6 argue for. **The shape is correct; its
  contents are not.**
- **Every Critic mitigation owed to this file landed** — see the table below.
- **The transcription is substantively faithful.** The only semantic delta is the dropped
  `file static class Gen` stub, which is correct: it declares bodiless methods and `06-spec.md`
  § The Lock assigns it to the unlocked support files. All other deltas are the added header, the
  spec's own prose folded in as `// >` comments, and whitespace.

### Claims I could not verify

- **Whether the two defects are acceptable to the approver as-is.** Only the user can answer that;
  it is why this verdict escalates rather than prescribes.
- **Anything requiring the support files.** `HistoryTestProgram.cs` and `Generators.cs` do not exist.
  Six obligations the lock places on them are enumerated in ⚠️-5; none is testable today.
- **Whether the run-time cost of the seed loops is bounded.** No `[Timeout]`, 200 seeds × 24–32
  dispatches × 8 properties, per-seed `Runtime` construction. Unknowable until PR 3.

---

## Critic's accepted risks

Only the rows whose mitigation was owed to **this file**. The rest are owed to later steps and are
out of scope for PR 0.

| risk | stated mitigation | present in the code? |
|---|---|---|
| **S25(a)** — a message arriving during a bracket does not end the run | `06-spec.md` line 15 authoritative; a transit state is recorded and the branch superseded, mid-movement | ✅ `:271-288`, `A_message_arriving_during_a_bracket_records_a_transit_state_and_supersedes_the_branch` — asserts `Movement.Held`, `Past.Peek().Model.Title`, and `BlockedBySupersedingAction` |
| **S26** — the headline new behaviour omitted from the obligations table | landed as `06-spec.md` line 14 | ✅ A3 at `:102-131` asserts `Future.Count == 1` **preserved**, not cleared |
| **S27** — the generator alphabet partition | INV-1/INV-2(1st)/INV-3/INV-4/INV-6 → `UserActions`; INV-5/INV-7 → `UserActionsAndDeliveries`; INV-2(2nd) → `UserActionsAndNavigation` | ✅ all eight checked property-for-property against `06-spec.md`'s table — `:475, :511, :559, :637, :692, :725, :759, :814`. Exact match |
| **S28** — ordered interleave without sleeping | `WhenApplied<T>().WaitAsync(timeout)`, no sleeps | ✅ `:67, :237, :257, :305`. No `Task.Delay`, no `Thread.Sleep` anywhere in the file |
| **B10** — the seal keys on a message type the seam does not guarantee | mitigation (1), scope the claim; `06-spec.md` obligations line 17 | ✅ stated twice — A10's SCOPE block `:407-412` and INV-2's second property `:621-628`, both naming `Navigation.cs:17-29` and obligation 17 |
| **🟡 7** — spec line 16 had no acceptance-layer example | A10 | ✅ `:401-451` |
| **🟡 4** — step 8(n)'s relocation contingency is withdrawn by the lock | *"after the approval commit a move is a `// SPEC CONFLICT:` hand-back and a re-approval"* | ⚠️ **the condition is what 🔴-1 and 🔴-2 trigger.** The acceptance was conditional on the locked text being right |
| **S29 / S33 / S30 / S31 / S32 / 🟡 a–e, 1–3, 5, 6** | owed to steps 3, 5, 10, 13, 16 | n/a — not this changeset |

---

## Findings

### 🔴 Must Fix (blocks merge)

**🔴-1 — `UndoRedoSpec.cs:787-788` does not compile in TUnit 1.19.57, and the file header states a
false reason for the non-compilation.**

```csharp
await Assert.That(runtime.Model.Present).IsNotEqualTo(h.Present)
    .Or.That(CursorMoved(h, runtime.Model)).IsTrue().Because($"seed {seed}");
```

*Evidence:* a scratch project referencing `TUnit 1.19.57` from the local NuGet cache, built with the
same SDK: `error CS1061: 'OrContinuation<int>' does not contain a definition for 'That'`. TUnit's
`.Or` continues on the **same** subject; a disjunction across two different values is not expressible
this way at all, so this is not a wrong overload but a wrong shape.

*Why it matters, and why it is a blocker rather than a defect to fix later.* The header this
changeset adds at `:7-9` says:

> This file does not compile until plan step 6 (04-realist-plan.md), which is expected: the types it
> exercises (WithHistory, History<TModel>, MovementAvailability, etc.) and its support fixtures
> (HistoryTestProgram.cs, Generators.cs) do not exist yet.

That sentence is false, and **it is not approved spec text** — `grep -c "Locked spec for undo/redo"
.squad/design/undo-redo/06-spec.md` returns `0`. The nine-line header is this changeset's own
addition. After step 6 delivers every named type and both named fixtures, line 787 still will not
compile. The header therefore tells the one reader who most needs to know — the `csharp-dev` at
step 6, reading the file for the first time in a compiling workspace — that a hard error is expected
and attributable to missing types. That is a durable false claim inside an artifact declared
immutable, and it is the changeset's to answer regardless of what happens to the assertion itself.

*The assertion is the spec's; the header sentence is this changeset's.* Both need resolving, by
different routes.

---

**🔴-2 — `UndoRedoSpec.cs:197-203` (A6) cannot fail for the reason it exists.**

The test is named `A_multi_event_decision_applies_every_event_before_any_command_is_interpreted`, and
`06-spec.md` § A6 frames it as recording *"the two behavioural differences between a wrapped and an
unwrapped program … asserted AGAINST the unwrapped program so the difference is recorded as a
difference rather than as a passing test."* The difference is **ordering**. The two expected
sequences are permutations of the same four strings.

*Evidence, by execution against TUnit 1.19.57:*
- `IsEquivalentTo` is order-insensitive — I asserted the **bare** expectation against the **wrapped**
  actual and it passed.
- `IsEquivalentTo(expected, CollectionOrdering.Matching)` → `CS0103: The name 'CollectionOrdering'
  does not exist in the current context`. There is no option flag on this version.
- `Assert.That(collection).IsEqualTo(sequence)` **is** order-sensitive — my probe failed on a
  permutation, as it should.

So a regression that reverted the wrapper to the unwrapped interleaving leaves A6 green, and the
remedy is a different assertion, not a parameter.

*Lower stakes, same mechanism, named for completeness rather than piled on:* `:267-268`
(`SubscriptionActivity` equivalent to `["start:…", "stop:…"]` — *stop then start* also passes, and
A7's second test claims two movements *reconcile twice*, which is an ordering claim). `:827-830` and
`:842-843` are order-insensitive by intent — a reconciliation set and a one-element distinct set —
and I do **not** flag them.

---

**🔴-3 — Three claims in the locked comments that the locked code does not implement, one of which
is the Lock's own named closure mechanism.**

**(a) `:800-804`, closing INV-6.** *"the property asserts that coverage at the end of the loop rather
than assuming the generator found them."* INV-6's body ends at `:797`, the closing brace of the seed
loop. Nothing counts the five `MovementAvailability` cases, nothing counts directions, nothing fails
if the corpus only ever produced `Nothing`.

**(b) `:528-529`, opening the INV-2 commentary.** *"INV-2 runs twice, over `Editor` (whole-model) and
`ProjectedEditor`."* It does not. The first property starts `Editor` at `:517`; the second starts
`Editor` at `:560` and its own comment at `:555` says *"Whole-model policy DELIBERATELY."*
`ProjectedEditor` appears exactly once in the file, at `:315`, in A8. **This one 08 did not find.**

**(c) `:616-620`, closing INV-2's second property.** *"the property is not done until both branches
have been observed taken at least once."* The branches are `:599` and `:604`. Neither is counted.

All three are verbatim from `06-spec.md` prose — (b) is `06-spec.md:908`.

*Why this is 🔴 and not a stale-comment ⚠️.* `06-spec.md` § The Lock names the support-file hole
explicitly and closes it with two mechanisms:

> Two things close it: the generator contract above is part of the approved spec text, and **every
> property carries a named falsifier that a weakened generator would fail to reproduce.**

INV-6's named falsifier **is** the coverage assertion in (a). It is not implemented. The Lock's
stated closure is therefore unbuilt for the property whose comment most loudly claims it, and the
comment is the thing a later reviewer is most likely to take at face value instead of re-deriving.
Combined with the unguarded `continue` guards at `:490, :492, :642, :649, :699, :732` — none of
which records that *any* seed reached the assertions — INV-1, INV-3, INV-4, INV-5 and INV-6 can all
pass having asserted nothing.

---

**🔴-4 — The route to fixing 🔴-1, 🔴-2 and 🔴-3(a)/(c) is a `spec-author` amendment with user
re-approval, and this changeset is the last cheap moment for it.**

Not a separate defect — the process consequence of the three above, recorded as a blocker because it
determines *who* acts and because committing PR 0 as-is forecloses it.

The text is approved (`06-spec.md` § *Approval Request — answered 2026-09-07*). `csharp-dev` may not
correct it: `07-handoff.md` § 4 makes *"editing it to match what the code does"* a 🔴 Must Fix, and
§ 6.1 🟡 4 says a change after the approval commit is a `// SPEC CONFLICT:` hand-back plus
re-approval. Before the commit, an amendment is the ordinary `spec-author` route — amendments 1, 2
and 3 already took it on 2026-09-07.

**This is a user decision, and I am not making it.** The two routes are (i) amend and re-approve
before committing, or (ii) commit as-is and accept that PR 3 opens with a hand-back on the pass's
most load-bearing file. The header sentence in 🔴-1 is the exception: it is not approved text and is
`csharp-dev`'s to correct either way.

---

### ⚠️ Should Fix

**⚠️-5 — The lock imposes at least six obligations on the unlocked support files, and the narrative's
stated closure covers one of them.**

`06-spec.md` § The Lock closes the support-file hole with the generator contract and per-property
falsifiers, and `07-handoff.md` § 4.1 makes that closure mandatory review wording for step 8. It is
incomplete. Enumerated so PR 3's author inherits a list rather than a surprise:

- **(a) A `global using static`.** ~13 unqualified fixture calls (`Start`, `StartBare`, `NoFeedback`,
  `Both`, `Drive`, `Movements`, `SingleReconciliation`, `Keys`, `CursorMoved`,
  `ReachableByOrdinaryActionsAlone`, `LoadReturns`, `RecordingInterpreter`, `FirstCommandFails`) must
  bind from outside a `sealed`, non-`partial` class with no base type. Nothing else can work.
- **(b) `EditorLog` must be per-test-isolated.** See ⚠️-6. The alternative — `[NotInParallel]` — is
  inside the lock.
- **(c) `EditorLog.Timeline` must return a snapshot, not the backing list.** `:190-195` captures
  `wrappedOrder`, calls `Clear()`, then captures `bareOrder`. A live reference makes both variables
  alias the bare timeline.
- **(d) `Start<Editor>()` must reset the static log.** A1 (`:22-53`) asserts
  `EditorLog.CommandsInterpreted).IsEmpty()` at `:52` with no `Clear()` anywhere in the method — the
  only one of the 25 tests that does.
- **(e) `Gen.Corpus` must actually reach all five availability cases in both directions**, because
  INV-6's coverage assertion is missing (🔴-3(a)).
- **(f) `EditorProgram.Transition(MovementRefused, …)` must issue no command.** This is 08's P7,
  narrowed. `04-realist-plan.md:1120`'s S7 rule — `if e is MovementRefused -> pass(...)` — means a
  refusal records no step and supersedes nothing, so INV-5's `Both(runtime.Model)` snapshot at `:730`
  is safe from the recording path. But `pass` at `:1128-1131` still calls
  `sealTops(SealedByEffect(redact(origin)))` when the command is not silent, which changes **both**
  edges. INV-6, which re-reads `var h = runtime.Model` inside its loop at `:766`, is immune; INV-5 is
  not. The inconsistency between the two loops is real and inside the lock.

Only `DocumentComparer.EqualUpToHandlerIds` is covered by the narrative's stated closure (`:679-683`).

**⚠️-6 — No `[NotInParallel]`, against a visible sibling convention and 25 tests on process-wide
mutable state.**

Measured: `Picea.Abies.Tests` has **no** assembly-level parallel configuration and no `.runsettings`;
TUnit's default is parallel. Six sibling classes carry class-level `[NotInParallel]`
(`DiffTests.cs:27`, `RenderTests.cs:24`, `HeadDiffTests.cs:15`, `UiComponentRenderTests.cs:8`,
`UiAccessibilityContractTests.cs:7`, `HotReloadTests.cs:8`) and `NavigationTests.cs` uses the
method-level form four times. The new file has none, and calls `EditorLog.Clear()` twelve times
across 25 tests, with emptiness assertions at `:52, :149-151, :247-248, :708-710, :773-775`.

The attribute site is the class declaration — inside the lock. ⚠️ rather than 🔴 because ⚠️-5(b)
offers a real escape: an `AsyncLocal`-backed `EditorLog` keeps the static call syntax the locked file
requires while giving per-test isolation. That escape needs to be *chosen deliberately at step 6*,
not discovered when the suite goes flaky, which is why it is named now.

**⚠️-7 — `History/UndoRedoSpec.cs` is in no MSBuild item group.**

Measured: `-getItem:Compile` → 20 items, `UndoRedoSpec.cs` absent, `SpecAttribute.cs` present;
`-getItem:None` → **0 items** in this project. The SDK's default `None` glob subtracts the
`Compile` **glob pattern** (`**/*.cs`), not the resulting item list, so a `Compile Remove` of a `.cs`
file lands it nowhere. Visual Studio and Rider will not show it without *Show All Files*, for the two
PRs during which its entire purpose is to be looked at and kept intact.

Correcting 08 on the precedent: `Picea.Abies.Presentation` has **45** `None` items including every
`content/demo/full/*.md`, so its comment's *"the files stay visible as None items"* is true for the
`.md` files and false only for `stops/5.5-property.cs`, the one `.cs` file — and the new csproj
comment does not repeat the claim. That is the precedent handled better, not copied.

**⚠️-8 — The format-on-save hook already mutated the approved text, and nothing guards a locked file
from it.** *(pre-existing framework gap — register)*

The committed file is not byte-faithful to `06-spec.md`. Every divergence I found is whitespace, and
`.editorconfig:80` explains all of it: `csharp_preserve_single_line_statements = false` turns the
spec's `if (…) continue;` into two lines at `:490-491, :492-493, :642-643, :649-650, :699-700,
:732-733` and splits five `foreach (…) await …;` statements. Harmless semantically; the point is the
mechanism.

`.claude/hooks/dotnet-format-on-save.sh` fires `PostToolUse` on every `Write`/`Edit`/`MultiEdit` of
any `.cs` file, with **no exclusion for a file the process declares immutable**, and it runs the
analyzer-fix pass including `IDE0005` (`.editorconfig:268`, severity `warning`) — which is exactly
how `using Picea.Abies.History;` was stripped on first write. The line is present now (`:13`).

Currently neutralised by the exclusion, and I verified it rather than assuming:
`dotnet format Picea.Abies.Tests.csproj --include History/UndoRedoSpec.cs` reports *"Formatted 0 of
27 files"* and leaves the file byte-identical, because a file outside the compile set is not in the
Roslyn workspace. **That protection disappears the moment PR 3 removes the exclusion.** Owner
`devops`; needs an entry in `.claude/enforcement/refutations.md`.

**⚠️-9 — The complete design pass is readable outside `.squad/design/`, defeating review blindness
through the mediated tools.** *(08's item 8, pre-existing — register)*

Confirmed and made precise. I fired `.claude/hooks/enforce-review-blindness.sh` with a
`reviewer-blind` payload:

| path | exit |
|---|---|
| `Picea.Abies.Presentation/content/demo/full/06-spec.md` | **0 — allowed** |
| `Picea.Abies.Presentation/content/demo/full/05-critic.md` | **0 — allowed** |
| `.../full/decision-drops/2026-09-07T10-41-14-review-pr359-round2.md` | **0 — allowed** |
| `.squad/design/undo-redo/06-spec.md` | 2 — denied |

The files are byte-identical: `md5sum` matches for `00-scope`, `03-convergence`, `04-realist-plan`,
`05-critic`, `06-spec` and `07-handoff`. The tree also carries `decision-drops/` including two
**previous review verdicts** (`review-pr359`, `review-pr359-round2`) and
`stops/5.6-review-headers.composite.md`. The deny list is `.squad/design/**` and one worktree
pattern; committed at `7d32cdb` (PR #360, merged), so **not this changeset's fault**.

Not covered by an existing residual: **R-19** is `Bash` being unmediated, **R-20** is
sibling-worktrees and symlinks. This is a second *path* reachable by the *mediated* tools — closing
R-19 entirely would leave it standing. `grep -n "Presentation\|content/demo"
.claude/enforcement/refutations.md` returns nothing. Owner `devops`.

**⚠️-10 — INV-3 exists in three copies, one checksummed, and they already disagree.**

`Picea.Abies.Presentation/content/demo/stops/5.5-property.cs` is INV-3 (`UndoRedoSpec.cs:637-670`) in
its **pre-format** spelling — I diffed them; the differences are precisely the
`csharp_preserve_single_line_statements` ones from ⚠️-8. It is line 36 of
`content/demo/SHA256SUMS`, and it is in no item group there either. So a correction to INV-3 now has
three homes and a checksum to refresh, and the drift has already occurred at the whitespace level
before anyone has tried to change anything.

**⚠️-11 — The PR-body precondition is unmet, because no PR exists yet.**

`04-realist-plan.md` PR-0 row and `07-handoff.md` § 6 both require the body to state that the spec is
deliberately excluded until step 6 removes the exclusion. Recorded so it is not lost between this
verdict and the push. The stated criterion is satisfied by *stating* it — the body still asks the
reviewer for nothing, which is the property that shape exists to preserve.

**⚠️-12 — `.squad/log/` churn would ride along on a `git commit -a`.**

`git check-ignore` exits 1 for `.squad/log/2026-09-07-session.md` and `.squad/log/pass-cost.md`; both
are tracked and both are modified (+24 lines, hook-written). The stated criterion for this PR is
*"Three files … and nothing else"* — the committer needs to stage explicitly. Process caution, not a
code defect.

---

### 💡 Nitpicks

- **`SpecAttribute`.** `07-handoff.md` § 4 answers 08's open question — *"It exists for
  `reviewer-reconcile`'s grep, not for execution"* — and flags it as a declared plan file-table
  amendment, so the approval trail is intact and I am not blocking on it. Two small things: I probed
  `[Property("Spec", "…")]` at class level on 1.19.57 and it **compiles**, so the new public type is
  avoidable and the greppability argument holds for either; and `[AttributeUsage(AttributeTargets.Class)]`
  leaves `Inherited` at its default `true` on a marker for `sealed` classes.
- **`TimeSpan.FromSeconds(5)`** as a literal at `:67, :237, :257, :305`.
- **Hand-rolled `foreach (var seed in Gen.Corpus)`** rather than `[Arguments]`/`[MethodDataSource]`:
  one reported test per invariant instead of one per seed, first failing seed aborts the rest, no
  per-seed isolation.
- **No `[Timeout]`** on eight properties that construct a `Runtime` per seed across 200 seeds.
- **Three things named `History` in one lookup scope** (`Picea.Abies.Tests.History`,
  `Picea.Abies.History`, the static `History` class). 08 confirmed by probe that it resolves; it is a
  legibility cost, not a defect.

---

### ✅ What's Good

- **The PR-0 shape is right, and it delivers the property it was designed for.** The spec has never
  appeared in git history, so the Lock's git-history check is satisfied without a waiver — exactly
  the argument `04-realist-plan.md` and `07-handoff.md` § 6 make for this shape over landing the spec
  as PR 3's first commit. *A waiver requested once is a waiver available always* is a good rule and
  this PR honours it.
- **Every Critic mitigation owed to this file landed**, including the two most easily lost: S27's
  alphabet partition matches `06-spec.md`'s table property-for-property across all eight properties,
  and S28's `WhenApplied<T>().WaitAsync(…)` is used throughout with no sleeps anywhere — continuing
  `ca2519d`'s direction.
- **The transcription is faithful where it counts.** The one semantic omission — the `file static
  class Gen` stub — is correct, since it declares bodiless methods and the spec assigns it to the
  support files. Folding the spec's prose in as `// >` comments preserves reviewable context that
  markdown fences would have dropped.
- **The exclusion is minimal and correctly scoped**: one item, one file, with a comment naming the
  removing step. It does not repeat its precedent's inaccurate claim about `None` visibility.
- **Build is clean** — 0 warnings, 0 errors; `SpecAttribute.cs` compiled, `UndoRedoSpec.cs` not.
- **The `using` was caught and restored.** The hook stripped it silently; someone noticed and said so
  rather than letting it pass. That is the behaviour that makes ⚠️-8 registrable instead of a
  post-mortem.
- **A9, A10 and A7's third test are genuinely good specifications** — each names the falsifier in
  prose *and* asserts it, and `A_terminated_program_with_no_history_left_is_terminal` (`:385-399`)
  explicitly exists to stop `IsTerminal` being hardcoded `false`. That is the discipline 🔴-3 finds
  missing in INV-2 and INV-6.

---

## Metrics

- **Files reviewed:** 3 (852 + 7 new lines, +7 modified) — 866 changed lines against
  `pr-validation.yml`'s 1500 hard limit
- **Tests in the changeset:** 25 `[Test]` — 17 acceptance (`:22`–`:414`), 8 invariant
  (`:469`–`:807`), covering INV-1…INV-7 with two properties on INV-2
- **Executable coverage of new code:** **0%** — deliberately. `-getItem:Compile` confirms
  `UndoRedoSpec.cs` is outside the compile set; `SpecAttribute.cs` is inside it and unreferenced
- **Probes run:** 6 (`.Or.That` compile, `IsEquivalentTo` order, `CollectionOrdering`, `IsEqualTo`
  order, `[Property]` at class level, `dotnet format` on the excluded file), plus 4 hook invocations
  and an extract-and-diff of `06-spec.md` against the committed file
- **Dimensions run:** 11/11. Observability, Security and Performance are non-applicable to this
  changeset and recorded as such: no runtime code, no attack surface, no hot path
- **Round:** 1 of 2 before the cap

---
---

# ⚖️ Re-review — round 2

**Reviewed:** working tree on `test/0-undo-redo-spec`, uncommitted, over HEAD
`faafc8b179f3069473a4116d9232d1dce9eab8d7`
**Blind assessment:** `08-review-blind.md` (round 1; `reviewer-blind` does not re-run, and I
re-read it before starting)
**Round:** 2 of 2 before the cap (`principles-enforcement.md` § *The Merge Criterion*). Round 1's
base was `3bd7af3`; the base moved to `faafc8b`, but the `**Round:** n` line governs on
disagreement and the higher wins. **A third round splits the changeset.**
**Verdict:** 🔴 Changes Requested

---

## Before anything else: round 1's evidence on 🔴-2 was wrong, in both directions

Amendment 4 chose its remedy because I had executed it. § *Amendment 4* says so in as many words —
*"Where amendment 4 had a choice of shape it took the one the reviewer had already executed
(`IsEqualTo` on a collection; a plain boolean subject) rather than a shape that merely looks
reasonable."* That was the right instinct and it was pointed at a bad datum. Two errors, both mine:

| round 1 claimed | measured now | how round 1 got it wrong |
|---|---|---|
| *"`Assert.That(collection).IsEqualTo(sequence)` **is** order-sensitive — my probe failed on a permutation, as it should."* | `IsEqualTo` on a collection fails on the **matching** sequence too. It is not order-sensitive; it is *never satisfiable*. | I probed only the negative case. A permutation failing is equally consistent with "order-sensitive" and with "always fails", and I did not run the positive control that separates them. |
| *"`IsEquivalentTo(expected, CollectionOrdering.Matching)` → `CS0103: The name 'CollectionOrdering' does not exist in the current context`. There is no option flag on this version."* | `CollectionOrdering` **exists** on 1.19.57, in `TUnit.Assertions.Enums`. `CS0103` was a missing `using`, not an absent type. | I read a name-resolution failure as an API absence without checking the assembly. `strings` on `TUnit.Assertions.dll` lists `CollectionOrdering`, `IsEquivalentToAssertion\`2` and `CollectionIsInOrderAssertionExtensions`. |

Both errors are recorded in the locked text as fact — `UndoRedoSpec.cs:239-241` repeats them
verbatim, attributed to me — which is the second half of 🔴-5 below. The finding stands on the
measurements, not on the apology; every claim below has a passing **and** a failing control.

---

## Reconciliation

### What I verified, item by item, against round 1

| round 1 finding | disposition | how I verified |
|---|---|---|
| **🔴-1** `.Or.That` does not compile | ✅ **fixed** | `grep '\.Or\.'` → one hit, `:906`, inside a comment quoting the old shape. The replacement at `:918-922` — `var moved = runtime.Model.Present != h.Present \|\| CursorMoved(h, runtime.Model); await Assert.That(moved).IsTrue().Because(…)` — compiles and passes in a 1.19.57 scratch project (probe 3, `INV6_available_shape`). |
| **🔴-1** the header states a false reason for non-compilation | ✅ **fixed** | The header (`:1-18`) is rewritten. It no longer claims the non-compilation is only missing types; it states the exclusion, the lock's scope, and the hand-back protocol. Every claim in it checks out (see below). |
| **🔴-2** A6/A7 `IsEquivalentTo` cannot fail for the reason they exist | 🔴 **replaced by a worse defect** | See 🔴-5. |
| **🔴-3(a)** INV-6's coverage assertion missing | ✅ **fixed** | `:931-947`. Ten (case × direction) combinations asserted after the loop; `observed.Add((direction, answer.GetType().Name))` at `:885`, unguarded — no `continue` in that loop. Compiles and passes (probe 3, `Coverage_shape`); `GetType().Name` matches `nameof` for nested record cases, verified for `Nothing` and `BlockedByWorld` (probe 3, `Nameof_matches_runtime_type_name`). |
| **🔴-3(a)** INV-6's interpreter | ✅ **taken, and it is the right call** | `:876` now `Start<Editor>(LoadReturns("server"))` driven through `Drive`. The user's Decision 2 reasoning is the correct one and I have nothing to add: narrowing the claim to what the old fixture could reach is adjusting the spec to the code. Alphabet unchanged — `Gen.UserActions` at `:875`, so S27's partition table stands. |
| **🔴-3(b)** INV-2 claims two lenses, runs one | ✅ **fixed properly** | `:591-613`. Both `using` blocks, one `reachable` set computed once at `:578`, distinct `Because` text per lens. `ProjectedEditor` now appears in the invariant layer at `:603`. The falsifier table gained a projection-specific mutation. Made true to the claim, not weakened to the code. |
| **🔴-3(c)** INV-2's branch flags | ✅ **fixed** | `:625-626` declare, `:679` and `:687` set, `:696-703` assert. Compiles and passes (probe 3, `Flags_shape`). |
| **🔴-3** closing ¶ — unguarded `continue`s | ✅ **fixed, and placed correctly** | `reached` on INV-1 (`:531`/`:553`/`:561`), INV-3 (`:712`/`:728`/`:751`), INV-4 (`:761`/`:777`/`:786`), INV-5 (`:796`/`:828`/`:843`). Each increment sits **after** the last `continue` and **before** the first assertion — I checked all four; a floor placed before the guard would have been decorative. INV-2's first property needs none: it asserts unconditionally per script step. |
| **⚠️-5** six support-file obligations | ✅ **enumerated**, and **one is missing** | The table is in `06-spec.md` § *The Lock*. (b) correctly downgraded by ⚠️-6's fix. A seventh is now measurable — see ⚠️-9. |
| **⚠️-5(f)** INV-5's once-taken `Both(…)` snapshot | ✅ **stated in both places** | `:798-812` at the loop, and on the fixture `Transition`'s `_ =>` fall-through in `06-spec.md`, which is where it is satisfied. |
| **⚠️-6** no `[NotInParallel]` | ✅ **fixed** | `:35`, `[NotInParallel("history-editor-log")]`. Compiles on 1.19.57 (probe 2). Matches the six siblings' key convention. |
| **⚠️-7** csproj item group | 🔴 **not fixed, not registered** | Re-measured on the working tree: `-getItem:None` → `{"None": []}`, `-getItem:Compile` → 20 items, `UndoRedoSpec.cs` in neither. See "criterion (b)" below. |
| **⚠️-8** format hook has no lock exclusion | ✅ **registered** | `R-28`, owner `devops`, `expires: 2026-09-21`. One stale detail: it cites `UndoRedoSpec.cs:13` for the restored `using`; the re-transcription moved it to `:23`. 💡 only. |
| **⚠️-9** design pass readable outside `.squad/design/` | ✅ **registered** | `R-27`, owner `devops`, `expires: 2026-09-21`, and it correctly distinguishes itself from R-19 and R-20. |
| **⚠️-10** INV-3 in three copies | ✅ **settled by the user** | Bundle canonical at `07607bf`, re-export scheduled after PR 0 merges. Verified the divergence is now content-level as predicted (the demo copy lacks `reached`, `reached++` and the floor assertion) and that `sha256sum -c SHA256SUMS` in `Picea.Abies.Presentation/content/demo` still passes cleanly — the bundle is internally consistent, it is simply older. Nothing to do. |
| **⚠️-11** PR-body precondition | ⏳ **still open** | `gh pr list --head test/0-undo-redo-spec` → empty. |
| **⚠️-12** `.squad/log/` churn | ⏳ **still open** | `git status --porcelain` → ` M .squad/log/2026-09-07-session.md` alongside the three intended paths. Stage explicitly. |
| **💡** no `[Timeout]` | ✅ **taken**, with two measured consequences | See ⚠️-8 below. |

### The transcription itself

**Byte-faithful to the six approved fences.** I re-extracted `06-spec.md`'s `csharp` fences at
`344-829`, `917-1014`, `1043-1133`, `1174-1223`, `1234-1429` and `1443-1489` — six, matching
`csharp-dev`'s report — reassembled them and diffed against the working-tree file. The entire
delta is **87 diff lines** and contains no assertion, no claim and no whitespace reflow:

- the 18-line header (`:1-18`), `csharp-dev`'s own text;
- a 15-line bridging comment at `:504-518` introducing the invariant layer, also `csharp-dev`'s;
- five blank lines between fences;
- the class's closing `}` moved from the end of fence 1 to the end of the file, which is correct —
  fences 2–6 are class-body fragments.

The `file static class Gen` stub at `06-spec.md:845-885` is again correctly omitted; it declares
bodiless methods and the spec assigns it to the unlocked support files.

**No formatter damage this time**, and that is a real difference from round 1: the diff contains
zero `csharp_preserve_single_line_statements` reflows, so the six `if (…) continue;` lines survive
as written. R-28's mechanism did not fire because the exclusion was in the csproj before the file
was written. The residual is unchanged and correctly registered.

**The build claims check out.** `dotnet build Picea.Abies.Tests.csproj` → 0 warnings, 0 errors with
the exclusion. With `<Compile Remove>` temporarily deleted → `error CS0234: The type or namespace
name 'History' does not exist in the namespace 'Picea.Abies'` at `UndoRedoSpec.cs(23,19)`. So
`using Picea.Abies.History;` is present and the exclusion is doing exactly the work claimed. csproj
restored byte-identical afterwards.

**The git-history property still holds.** `git log --all -- 'Picea.Abies.Tests/History/*'` returns
nothing. The Lock's history check needs no waiver.

### What I could not verify

- **That the ten (case × direction) combinations INV-6 now asserts are all reachable.** Amendment 4
  fixed one unreachable case (`BlockedByWorld` under `NoFeedback`) by reasoning, not execution —
  `spec-author` has no `Bash` and the file does not compile. I cannot settle, for instance, whether
  `BlockedBySupersedingAction` is reachable on the **backward** edge, and neither could anyone
  before step 6. This is not a new finding: it is ⚠️-5(e), which amendment 4 correctly converted
  from *hoped for* into *fails loudly*. It is named here only so the step-6 author expects the
  possibility of a second re-approval on this exact line rather than treating a red coverage
  assertion as a feature bug.
- **Anything requiring the support files.** Still absent. Seven obligations now, not six.
- **Whether the `[Timeout(30_000)]` derivation's ×10 is the right factor on this project's CI.** The
  measurement (0.22 ms/seed, 41.8 ms/200 seeds, Ryzen 9 9950X, .NET 10.0.110) is recorded beside
  the value as Decision 3 required, and the arithmetic checks out. Whether the margin is adequate
  is unknowable until step 6.

---

## Critic's accepted risks

Round 1's table stands unchanged — I re-checked S25(a), S26, S27, S28, B10 and 🟡 7 against the
re-transcribed file and every mitigation is still present at the same assertions. S27's partition
in particular survives amendment 4: INV-6's alphabet is still `Gen.UserActions` (`:875`), only its
interpreter moved, so the table is untouched. One row changes state:

| risk | stated mitigation | present in the code? |
|---|---|---|
| **🟡 4** — a change after the approval commit is a `// SPEC CONFLICT:` hand-back plus re-approval | the lock takes effect at the approval commit | ⚠️ **still the open condition.** Round 1 triggered it once and the pass paid the amendment cost correctly. 🔴-5 triggers it again, and PR 0 is *still* the approval commit — so the second amendment is still the cheap route, and it is the last time that is true. |

---

## Findings

### 🔴 Must Fix (blocks merge)

**🔴-5 — `UndoRedoSpec.cs:243, :247, :318` — `Assert.That(<collection>).IsEqualTo([…])` compiles and
can never pass. Amendment 4 replaced three assertions that passed for the wrong reason with three
that fail for no reason.**

```csharp
await Assert.That(wrappedOrder).IsEqualTo([
    "applied:BatchFirst", "applied:BatchSecond",
    "interpreted:FailingCommand", "interpreted:MarkCommand"]);
```

*Evidence, in a 1.19.57 scratch project, with both controls run.* The assertion fails on the
**matching** sequence:

```
AssertionException: Expected to be equal to <>z__ReadOnlyArray`1[System.String]
but received <>z__ReadOnlyArray`1[System.String]
```

The collection expression is target-typed to a `<>z__ReadOnlyArray<string>` and compared by
`Equals`, which is reference equality — so the subject and the expectation are never equal, whatever
they contain or in whatever order.

*Swept across every plausible support-file return type for `EditorLog.Timeline`*, because the
support files are unlocked and could in principle have closed this: `string[]`, `List<string>`,
`IEnumerable<string>`, `IReadOnlyList<string>`, `ImmutableArray<string>`, `ImmutableList<string>` —
**all six fail on identical content.** I also built a bespoke `[CollectionBuilder]` type with
correct `IEquatable<T>` value equality; it fails too, and the probe prints
`PROBE actual=[a, b] equals-new=True` on the line before the assertion fails. That is the decisive
control: the subject's own equality says *equal*, and the assertion still says *not equal*, because
the expectation was materialised as a different type. **No support-file choice rescues this.**

*Why it is more than a wrong overload.* A6 exists to record an ordering difference between a wrapped
and an unwrapped program. Under `IsEquivalentTo` it was green whatever happened; under `IsEqualTo`
it is red whatever happens. Both are useless, and the second is worse in this specific process:
step 6's obligation is *observe every test red for the right reason, then make it green*. Three
tests that cannot be made green by any implementation cannot close step 6. That is a defect this
changeset introduces — round 1's file was vacuous here, this one is unsatisfiable — so it blocks
under *"a regression introduced by the changeset"* rather than under registration.

*Two shapes that do work on 1.19.57, both executed with a passing and a failing control, stated as
measurements and not as a prescription:*

- `IsEquivalentTo(expected, CollectionOrdering.Matching)` — `using TUnit.Assertions.Enums;`.
  Matching order passes; permutation fails. Verified on `string[]`, `IEnumerable<string>` and
  `IReadOnlyList<string>` subjects.
- `Assert.That(actual.SequenceEqual([…])).IsTrue()` — passes on match. Costs the diff in the
  failure message.

Which one lands, and whether A7's `["start:…", "stop:…"]` wants the same treatment, is the
`spec-author`'s and the approver's call, not mine.

**🔴-6 — `UndoRedoSpec.cs:239-241` states two false claims about TUnit inside the locked text, and
the lock covers claims.**

```
// could not fail for the reason it exists. `IsEqualTo` on a collection IS
// order-sensitive on this version (the reviewer verified both by execution), and
// `IsEquivalentTo(expected, CollectionOrdering.Matching)` does not exist here — so the
// remedy is a different assertion, not a parameter.
```

Both sentences are false, and the first cites me as its authority. `CollectionOrdering` exists in
`TUnit.Assertions.Enums` on 1.19.57 and the two-argument overload compiles and discriminates order
correctly. `IsEqualTo` on a collection is not order-sensitive; it is unsatisfiable.

This is the same class round 1 raised as 🔴-3 — a comment in the locked text claiming something the
code and the toolchain do not support — and it is worth being explicit that it is *not* a
duplicate of 🔴-5. 🔴-5 is the assertion; 🔴-6 is the sentence that would still be there, and still
wrong, and still attributed to a review, if the assertion were fixed on its own. Decision 1 makes
the file immutable *in its assertions and its claims*, so after the approval commit correcting this
paragraph is itself a hand-back. `:314-317` (A7) carries the same claim in shorter form.

---

### ⚠️ Should Fix

**⚠️-8 — `[Timeout(30_000)]` at `:54` emits `TUnit0015` on all 25 test methods, and does not
interrupt a synchronous hang.** *(new; the attribute is new)*

Measured on 1.19.57, class-level `[Timeout]` with parameterless `[Test]` methods:

- `warning TUnit0015: Missing TimeoutAttribute cancellation token parameter`, one per test method.
  Not fatal — no `TreatWarningsAsErrors` in `Directory.Build.props`, the test csproj or any
  workflow, so step 6 gains **25 warnings**, not 25 errors. Round 1 recorded *"Build is clean — 0
  warnings"*; that stops being true when the exclusion comes off.
- The timeout **does** fire on an `await`-shaped body without a `CancellationToken` parameter — a
  `[Timeout(2_000)]` test awaiting `Task.Delay(6_000)` failed at 2 s with TUnit's `TimeoutException`.
  So Decision 3 buys what it was meant to buy: the seed loops are `await`-dense and a real hang
  there is async-shaped.
- It does **not** fire on a synchronous spin: a `[Timeout(2_000)]` test spinning 6 s **passed**.
- The timed-out test's body **keeps running** after TUnit reports the failure — my probe printed
  `PROBE: body completed after 5999 ms` well after the test was recorded as failed. On a class whose
  correctness rests on the process-wide static `EditorLog`, an orphaned body still dispatching into
  that log will corrupt whichever test runs next. `[NotInParallel]` does not prevent this; the
  orphan is a detached continuation, not a test.

The remedy for the warning — a `CancellationToken` parameter on each `[Test]` — is a **signature**
change, which is neither an assertion nor a claim, so on Decision 1's wording step 6 could make it
without a hand-back. That reading is not obvious enough to leave implicit, which is why it is here
rather than discovered in an argument at step 6.

**⚠️-9 — `UndoRedoSpec.cs:986-987` is a seventh support-file obligation, and it is not in the
amendment's table of six.** *(round 1 cleared this site on reasoning; execution changes the answer)*

```csharp
await Assert.That(EditorLog.ReportedKeySetsDuringHold.Distinct())
    .IsEquivalentTo([anchorKeys]).Because($"seed {seed}");
```

Round 1 called this *"order-insensitive by intent — a one-element distinct set"* and did not flag
it. That was right about ordering and silent about element equality. Measured: `IsEquivalentTo`
compares **elements** with `Equals`, so a sequence-of-sets compared against `[anchorKeys]` fails
whenever the elements are reference-equality types — `HashSet<string>` and `string[]` elements both
fail on identical content — and **passes** when the elements have value equality (a `record`
element: matching passes, non-matching fails, both controls run).

So this is genuinely a support-file obligation and **not** a lock defect: `Keys(…)` and
`EditorLog.ReportedKeySetsDuringHold` must yield a value-equality element type. It belongs in
`06-spec.md` § *The Lock*'s table as **(g)**, next to (c) and (d), which are the same kind of
obligation. The same reasoning applies to `SingleReconciliation(…)` at `:971`, which is safe if it
returns a flat `IEnumerable<string>` and has this problem if it returns a set of sets.

**⚠️-7 (carried, unfixed, unregistered) — `History/UndoRedoSpec.cs` is still in no MSBuild item
group.**

Re-measured on this working tree, not carried over on trust: `-getItem:None` → `{"None": []}`;
`-getItem:Compile` → 20 items without it. The SDK's default `None` glob subtracts the `Compile`
glob *pattern*, so a `Compile Remove` of a `.cs` file lands it nowhere and neither Visual Studio nor
Rider shows it without *Show All Files* — for the two PRs during which being looked at is its entire
job. `06-spec.md` § *Amendment 4* assigns it to `csharp-dev`; `csharp-dev` neither did it nor
registered it. It is the only round-1 ⚠️ in that state.

---

### 💡 Nitpicks

- **`R-28` cites `UndoRedoSpec.cs:13` for the restored `using`; it is now `:23`.** The ledger is
  append-only and the entry is otherwise accurate; noting it so a later reader does not conclude the
  `using` moved out of the file.
- **Two blocks of non-approved commentary now live inside the locked file** — the header (`:1-18`)
  and the invariant-layer bridge (`:504-518`), both `csharp-dev`'s own text. I checked every claim
  in both and found none false, which is the round-1 header defect not repeated. The wrinkle is
  jurisdictional: from the approval commit the lock covers *"what a comment claims"*, so these two
  blocks become locked text that never went through the approval they describe. Worth one sentence
  somewhere saying whether they are inside or outside.
- **The `// >` prose folding from round 1 is gone**, and the file is byte-faithful to the fences as
  a result. I flag it only because it is a deliberate reversal of something round 1 praised: the
  gain is that `diff <(extract fences) UndoRedoSpec.cs` is now an 87-line answer instead of a
  judgement call, which for a locked artifact is worth more than the inline context. Not a finding.
- **`TimeSpan.FromSeconds(5)` at four sites** — offered and declined in the amendment, with a
  reason I agree with. Not raised again.

---

### ✅ What's Good

- **Six of the seven round-1 findings that were the amendment's to fix are fixed properly, and the
  two that mattered most were fixed by making the code true to the claim rather than the claim true
  to the code.** 🔴-3(b) added the *projection* lens — the half that can actually fail — instead of
  deleting the sentence, and Decision 2 kept INV-6's feedback interpreter on exactly that reasoning.
  Both were open invitations to take the cheaper route and neither did.
- **Writing the missing assertion found the unreachable case.** 🔴-3(a) was a comment claiming a
  coverage check; implementing it immediately exposed that `BlockedByWorld` could never occur under
  `NoFeedback`. That is the argument for named falsifiers being *code*, made by the artifact itself
  rather than asserted about it.
- **The floors are placed where they do work.** All four `reached` increments sit after the last
  guard and before the first assertion. Getting this wrong is easy and silent.
- **The `[Timeout]` derivation is recorded, not asserted.** The measurement, the machine, the
  runtime version, the ×8 × ×10 factors and the asymmetry argument are all beside the value, so the
  arithmetic can be checked instead of guessed at. That is what Decision 3 asked for.
- **The header is fixed, not hedged.** Round 1's finding was a false sentence; the response was to
  rewrite it as `csharp-dev`'s own accurate text, not to append a disclaimer to it.
- **The transcription is verifiable by construction.** Six fences, 87 diff lines, none of them an
  assertion.
- **The registrations are real registrations.** R-27 and R-28 both carry an owner, a level
  consequence written from executed evidence, and an `expires:` — and R-27 argues explicitly why it
  is neither R-19 nor R-20, which is the part that usually gets skipped.

---

## The merge criterion, applied

`principles-enforcement.md` § *The Merge Criterion — Continuous Improvement*:

- **(a) stated properties green** — ❌. The Lock's stated closure is *"every property carries a named
  falsifier"*; A6's and A7's falsifiers are ordering claims, and after 🔴-5 those three assertions
  cannot be green under any implementation. The changeset introduced that, so it blocks
  unconditionally and is not registrable.
- **(b) every non-regression finding registered** — ❌, on one item. **(b) binds on 🔴 and ⚠️ here,
  not 💡** — stated explicitly so the scope does not drift either way. ⚠️-8 and ⚠️-9 from round 1 are
  registered as R-27/R-28. **⚠️-7 is neither fixed nor registered.** ⚠️-8 and ⚠️-9 in *this* round
  are new and unregistered; ⚠️-8 belongs to this changeset (the attribute is new), ⚠️-9 is a
  spec-text obligation rather than a framework gap and should land in § *The Lock*'s table rather
  than the ledger.
- **(c) nothing published above its computed level** — ✅. Nothing in this changeset claims a level.

**Round cap.** This is round 2. On round 3 the changeset splits: what satisfies (a)–(c) ships and
the rest becomes the next changeset — and *"the split never ships a red stated property"*, so a
still-broken A6/A7 could not be split off into "ships anyway". That is the concrete reason to
resolve 🔴-5 and 🔴-6 in one amendment rather than two.

---

## Metrics

- **Files reviewed:** 3 — `UndoRedoSpec.cs` (996 lines, new), `SpecAttribute.cs` (7 lines, new),
  `Picea.Abies.Tests.csproj` (+7). 1,010 changed lines against `pr-validation.yml`'s 1,500 limit.
- **Tests in the changeset:** 25 `[Test]` — 17 acceptance, 8 invariant across INV-1…INV-7 (two on
  INV-2); 8 `[Property("Invariant", …)]` attributes, plus one occurrence inside a comment.
- **Executable coverage of new code:** 0%, deliberately — `-getItem:Compile` → 20 items, without it.
- **Probes run this round: 7 scratch projects, 25 executed test cases**, each finding carrying a
  passing control and a failing control: `[Timeout]` + `[NotInParallel]` compile; `[Timeout]`
  behaviour on async vs. synchronous bodies; `IsEqualTo` on a collection expression across six
  subject types; `IsEqualTo` against a bespoke `[CollectionBuilder]` value-equality type;
  `IsEquivalentTo` + `CollectionOrdering.Matching` in both directions; nested-collection
  `IsEquivalentTo` with reference-equality and value-equality elements; the four new assertion
  shapes (`moved`, the `reached` floor, the two edge flags, the ten-combination sweep) and
  `GetType().Name` vs. `nameof`. Plus a fence extract-and-diff, two repository builds (with and
  without the exclusion), two `-getItem` measurements, a `SHA256SUMS` verification of the demo
  bundle, and a `git log --all` history check.
- **Dimensions run:** 11/11. Security, Observability and Performance remain non-applicable —
  no runtime code, no attack surface, no hot path.
- **Round:** 2 of 2 before the cap.
