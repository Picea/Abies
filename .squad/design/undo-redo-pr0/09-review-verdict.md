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
