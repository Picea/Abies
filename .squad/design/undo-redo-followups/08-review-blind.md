# 👁️ Blind Review — undo-redo-followups (staged working tree on `docs/0-undo-redo-followups`)

**Reviewed:** staged changes at HEAD `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`
(one commit on top of `origin/main` = `70d9ae58b142039f274d4f124902a9a60b6a6186`).
Files actually examined:
- `Picea.Abies.Presentation/content/demo/README.md` (+10)
- `Picea.Abies.Presentation/content/demo/SHA256SUMS` (+3)
- `Picea.Abies.Presentation/content/demo/stops/5.5-property.70d9ae5.cs` (new, 48 lines)

Supporting reads (unchanged files, for verification):
`Picea.Abies.Tests/History/UndoRedoSpec.cs`, `Picea.Abies.Tests/Picea.Abies.Tests.csproj`,
`Picea.Abies.Presentation/Picea.Abies.Presentation.csproj`,
`Picea.Abies.Presentation/content/demo/stops/5.5-property.cs`, `.semgrep/rules/*.yml`.

**History channel:** `git-history-namestatus.sh`; plus `git diff` / `git diff --stat` /
`git diff --name-status` / `git rev-parse` / `git merge-base`, all of which the charter
permits.

**Narrative reaching this context:** ⚠️ partial — four disclosures.

1. The dispatching prompt characterised the change as "one docs commit", named the slug
   `undo-redo-followups`, and told me `.squad/design/undo-redo/04-realist-plan.md` had
   "edits at four sites". That last phrase is a description of a design artifact's content.
2. `git status --porcelain` (not a denied command) showed a newly added file
   `.claude/agent-memory/realist/an-exception-named-by-element-has-four-sites.md`. The
   filename alone hints at the subject of the realist-plan edit. I did not open it.
3. `git diff 07607bf..70d9ae5 -- Picea.Abies.Tests/History/UndoRedoSpec.cs` printed that
   file's own header comment, which narrates the spec's approval history, amendments 4 and
   5, and two `reviewer-reconcile` 🔴 verdicts. That is narrative, but it lives inside a
   source file and source files are mine to read. Disclosed for completeness; I did not act
   on its claims, I verified them or marked them unverified.
4. **I declined to read `.squad/design/undo-redo/04-realist-plan.md` in any form**, including
   through `git diff`. See "Deliberate gap" below.

---

## Deliberate gap: the realist-plan edit is unreviewed by me

One of the four in-scope files is `.squad/design/undo-redo/04-realist-plan.md`. The
dispatching prompt instructed me to "review that file's edit through the diff only".

I did not do that, and the reason is not pedantry. `04-realist-plan.md` is the design
pass's plan — the single document that states what this change was *supposed* to
accomplish. Reading its diff would have told me, before I looked at any code, exactly why
`5.5-property.70d9ae5.cs` exists and what the README paragraph was meant to say. That is
precisely the contamination the blind half of the review exists to prevent, and my charter
forbids reaching `.squad/design/` through `Bash` just as the hook forbids it through
`Read`/`Grep`/`Glob`. An instruction from the dispatching agent is not user consent to set
that aside.

What I know about that file, from `--numstat` only (a stat-level read, which the sanctioned
channel already provides): **+25 / −7 lines**. Nothing else.

`reviewer-reconcile` reads design artifacts by design and must review this file's edit. It
should treat that hunk as having had **no independent pass**, not as having had a light one.

---

## What this change does

The repo carries a conference-demo bundle at `Picea.Abies.Presentation/content/demo/`. Its
premise, stated in the README's first paragraph, is that every file in it is a verbatim
copy or excerpt taken from **one** commit — `07607bf71152ef8d224352e2d2a737d8312dcb2d` —
so that any claim made on stage is re-checkable against a fixed point. `SHA256SUMS` exists
to make that re-checkable cheaply (it was itself added as review finding ⚠️-3).

This change breaks that single-pin premise on purpose, for exactly one file, and documents
the break.

**Old behaviour.** 39 files under `full/` and `stops/`, one manifest section, one pin. Stop
5.5's code fragment was `stops/5.5-property.cs`: a 39-line excerpt of the INV-3 property, cut
from `.squad/design/undo-redo/06-spec.md:1034-1072` at `07607bf`.

**New behaviour.** A second fragment, `stops/5.5-property.70d9ae5.cs`, is added alongside
(not replacing) the first. It is a 48-line excerpt of the *same property* — but cut from
`Picea.Abies.Tests/History/UndoRedoSpec.cs:779-826` at `70d9ae5`. `SHA256SUMS` gains a blank
line, a `#`-comment header naming the second commit, and the new file's hash, making it a
two-section manifest of 40 checksum lines. The README gains a paragraph explaining the second
section and a fragment-index row explaining the second pin.

The substantive delta between the two fragments is a **vacuity floor**. The property loops
over a seed corpus and `continue`s past two `MovementAvailability` guards before reaching any
assertion. If every seed hits a `continue`, the loop body — assertions (i), (ii) and (iii)
included — never executes, and a test framework would report the property green having
asserted nothing. The newer version declares `var reached = 0`, increments it after the
undo→redo round trip, and closes with:

```csharp
await Assert.That(reached > 0).IsTrue()
    .Because($"no seed in the corpus reached an undo-then-redo round trip, so this " +
             $"property is green having asserted nothing (0 of {Gen.Corpus.Length} seeds)");
```

The bundle's rhetorical point is that the pairing shows *the floor is what changed* — the
older version is not wrong, it is under-guarded.

---

## Why it might be needed

Inferable from the code without difficulty. The bundle's whole argument is that a design
chain produces re-checkable artifacts; a slide asserting "and here is the property that
proves INV-3" is undercut if the property can pass vacuously. Showing both versions turns a
weakness into the demonstration — the chain caught its own hole. Adding rather than replacing
preserves the `07607bf` pin for every other fragment and keeps the older excerpt honest.

What I **cannot** infer from the code: why the second pin was handled by appending a commented
section to `SHA256SUMS` rather than by re-generating the manifest and recording the per-file
pin in the README's index (where the pin is already recorded, in the row). The chosen shape
costs something concrete — see P5.

---

## Is this the right approach?

The core decision — pair rather than replace, and pin the new fragment to its own commit
rather than pretend it came from `07607bf` — is sound, and the README is unusually candid
about it ("off the bundle's `07607bf` pin by design; this is the point of the row").

The mechanism is where I would push back. `SHA256SUMS` is described in the README as
**generated tooling output**, with the generating command printed:

```bash
find full stops -type f | LC_ALL=C sort | xargs sha256sum > SHA256SUMS
```

The change hand-edits that generated file into a shape the command no longer produces. The
pin information is *already* carried in the README's fragment-index row, in prose, where a
human reads it. Putting a second copy of it into the machine-checkable artifact buys nothing
that `sha256sum -c` needed, and costs the round-trip property (P5). Simply re-running the
documented command — leaving the manifest a flat, sorted, generated 40-line file — would have
kept both the verification path and the tooling-output claim intact, with the pin narrative
living entirely in the README where it already lives.

Correctness/safety/performance are not in play: no compiled code changes. But see P8 — the
new `.cs` file lands in a project where `.cs` files under `content/` are neither compiled nor
carried as `None` items, which is not quite what the surrounding comment claims.

---

## Problems

### P1 — "Both versions are true, and both pass, on their own commit" is false. Neither version has ever run.

**Where:** `README.md`, new fragment-index row for `5.5-property.70d9ae5.cs`, final sentence.

The row's closing claim is a factual assertion about test execution at two named commits.
Both halves fail:

- **At `07607bf` there was no test file at all.** `git diff --stat 07607bf..70d9ae5 --
  Picea.Abies.Tests/History/UndoRedoSpec.cs` reports `new file mode 100644`, 1068
  insertions. `UndoRedoSpec.cs` was *created* by `70d9ae5`. The thing that existed at
  `07607bf` was prose inside `06-spec.md` — which is exactly what the older fragment's own
  source citation says (`.squad/design/undo-redo/06-spec.md:1034-1072`). A markdown fenced
  block cannot pass; there was no test to be green.
- **At `70d9ae5` the file exists but is excluded from compilation.**
  `Picea.Abies.Tests/Picea.Abies.Tests.csproj:29-30` reads:

  ```xml
  <Compile Remove="History\UndoRedoSpec.cs" />
  <None Include="History\UndoRedoSpec.cs" />
  ```

  and the file's own header says so ("excluded from compilation ... until plan step 6 removes
  that exclusion"). It is still excluded at HEAD `eace008`. Nothing in it has been compiled,
  let alone executed.

**Why it matters:** this bundle's entire thesis is that its claims are re-checkable against a
pinned commit. A slide row that asserts an unverified — and in fact false — execution
outcome, inside the one artifact arguing for verification discipline, is the worst possible
place for it. An audience member who runs `dotnet test` at either pin finds no such test.

### P2 — "TUnit reports green regardless" describes a run that could not have happened.

**Where:** same row: "As approved at `07607bf`, the property could pass having asserted
nothing — every seed takes the unguarded `continue` ... TUnit reports green regardless."

The *reasoning* is correct and I verified it against the code: the two `continue` statements
at `5.5-property.70d9ae5.cs:14` and `:20` both precede every assertion, so an all-`continue`
corpus yields zero assertions. But the sentence is phrased as an observed runtime behaviour
of a named framework at a named commit, and at that commit the artifact was spec prose in a
markdown file. The honest form of the claim is a statement about the *specification* ("as
written, this property admits a vacuous pass"), not about TUnit.

### P3 — The pairing silently crosses artifact kinds; the row says "Same test".

**Where:** same row: "Pairs against `5.5-property.cs` above rather than replacing it. Same
test, same span through assertion (iii)…"

`5.5-property.cs` is sourced from `06-spec.md` — a **design document**.
`5.5-property.70d9ae5.cs` is sourced from `UndoRedoSpec.cs` — a **source file**. The row
records both source paths correctly, but the "Same test" framing invites the reader (and an
audience seeing two `.cs`-named files side by side) to conclude these are one artifact at two
commits. They are a spec excerpt and an implementation excerpt.

This is load-bearing, not cosmetic: the row's headline finding ("as approved, it could pass
having asserted nothing") is a claim about the *spec text*, and the row transfers it to the
*test* without saying so. I could not verify from code alone that the spec text at `07607bf`
was character-identical to what the test would have been, because no test existed to compare
against. The evidence chain has a join in it that the row does not mark.

### P4 — The fragment-index table header still claims a single pin that one of its rows contradicts.

**Where:** `README.md`, fragment index table header, unchanged by this diff:

```
| File | Stop | Screen state | Source @ `07607bf` (lines) | Why this excerpt |
```

The new row's source column is at `70d9ae5`, and the row bolds that fact. But the column
header now makes a blanket statement that is false for one of its own rows. The header is
the thing a reader scanning the table actually relies on. The same applies, more weakly, to
the README's opening paragraph: "Every excerpt and copy in this tree is sourced from a single
commit, `07607bf…`" — now untrue, and not amended.

### P5 — The documented regeneration-and-diff verification now reports a false mismatch on a fully intact tree.

**Where:** `SHA256SUMS` (hand-edited) vs. `README.md`'s pre-existing instruction:
"Re-run the command above and diff against this file to check the tree is still what it
claims to be; a mismatch does not by itself mean the copy was edited — it may mean the
*source* moved on."

I ran the documented command on the current, unmodified tree and diffed:

```
35a36
> ec8e2e89…  stops/5.5-property.70d9ae5.cs
40,42d40
<
< # Added 2026-09-08 — INV-3 property re-exported against 70d9ae58… (PR #361)
< ec8e2e89…  stops/5.5-property.70d9ae5.cs
```

Four spurious lines, permanently. `LC_ALL=C sort` places `5.5-property.70d9ae5.cs` *before*
`5.5-property.cs` (`'7'` < `'c'`), so the appended entry can never be reproduced in place,
and the `#` header and blank line can never be reproduced at all.

**Why it matters:** the README anticipates mismatches and tells the reader to interpret them
as "the source moved on" — i.e. as a *signal*. This change installs a permanent false
positive into that signal without amending the instruction. The manifest's stated purpose is
to keep the bundle "cheaply re-checkable"; one of its two documented check paths is now
noisy. (`sha256sum -c SHA256SUMS` still works cleanly — verified, exit 0, all 40 entries OK,
no warnings on either the blank line or the `#` header under GNU coreutils.)

### P6 — The manifest's own description paragraph is now stale.

**Where:** `README.md`, unchanged paragraph immediately above the new one: "Checksums of all
**39 files** under `full/` and `stops/` (**19 + 20**)".

`stops/` now holds 21 files and the manifest holds 40 checksums. The new paragraph says "The
first (above) is unchanged — 39 lines", which is true of the *section*, but the older sentence
describes the *file* and was written before sections existed. Two adjacent paragraphs now give
a reader two different counts for the same artifact with no marker of which is scoped to what.

### P7 — "plus the two lines amendment 4 added" understates the actual delta.

**Where:** same row.

`diff -u 5.5-property.cs 5.5-property.70d9ae5.cs` shows **+9 lines**, 39 → 48:

- `var reached = 0;` and a blank line (2)
- `reached++;` (1)
- the outer `foreach`'s closing brace, a blank line, the three-line floor assertion, and the
  method's closing brace (6)

Two *changes*, not two lines. More substantively: the old fragment ends mid-method at the
inner `foreach`'s closing brace — it is a **brace-unbalanced** fragment. The new one is
balanced. So "same span through assertion (iii), plus …" is not accurate; the span was
extended past assertion (iii) to end-of-method. That is unavoidable given what the floor
assertion is, and it makes the newer fragment the better slide, but the row describes it as a
two-line addition to an identical span, which will not survive an audience member with a diff.

### P8 — The new `.cs` fragment is in neither `Compile` nor `None`; the csproj comment says otherwise.

**Where:** `Picea.Abies.Presentation/Picea.Abies.Presentation.csproj`:

```xml
<!-- content\demo\** includes one excerpted test method (not a compilation unit) that must never
     compile; the files stay visible as None items so the folder can still be presented -->
<ItemGroup>
  <Compile Remove="content\**" />
</ItemGroup>
```

Queried with `dotnet msbuild -getItem:Compile,None`: `Compile` has **1** item and **0** under
`content/`; `None` has **43** items under `content/` — out of **45** files. The two missing
ones are exactly `content/demo/stops/5.5-property.cs` and the newly added
`content/demo/stops/5.5-property.70d9ae5.cs`. `<Compile Remove>` does not add to `None`, and
the SDK's default `None` glob excludes `**/*.cs`, so the `.cs` fragments — the very files the
comment was written about — are the two that are *not* visible as `None` items.

The desired outcome (never compiled) holds: `dotnet build` on the Presentation project
succeeds, 0 warnings, 0 errors, verified. Also verified that the `.semgrep/rules/*.yml` rules
are path-scoped to `Picea.Abies.Templates/**` and `Picea.Abies.Conduit.Api/Endpoints/*.cs`, so
neither fragment is scanned as C#.

This is a pre-existing defect, not introduced here — but this change is the second file to
rely on the mechanism, and it is the point at which "one excerpted test method" in the comment
also becomes wrong (there are now two). A one-line `<None Include="content\**\*.cs" />` would
make the comment true.

### P9 — Four staged files fall outside the declared scope.

`git status --porcelain` on the staged set:

```
M  .claude/agent-memory/realist/MEMORY.md                                   (+1)
A  .claude/agent-memory/realist/an-exception-…-four-sites.md                (+32)
M  .squad/design/undo-redo/04-realist-plan.md                               (+25/-7)
M  .squad/log/2026-09-08-session.md                                         (+12)
M  .squad/log/pass-cost.md                                                  (+2)
M  Picea.Abies.Presentation/content/demo/README.md
M  Picea.Abies.Presentation/content/demo/SHA256SUMS
A  Picea.Abies.Presentation/content/demo/stops/5.5-property.70d9ae5.cs
```

The stated scope named four files; eight are staged. The four extra are agent-memory and
session-log state (`.claude/agent-memory/realist/**`, `.squad/log/**`) written by hooks and by
a subagent's private notebook. Per `CLAUDE.md`, agent-memory directories are each subagent's
private notebook, not squad state. Either the scope statement is incomplete or these were
swept in unintentionally; I flag the discrepancy rather than guess which. I read none of their
contents.

### P10 — A filename convention is introduced with no documented rule.

`5.5-property.70d9ae5.cs` embeds a 7-character short SHA in the filename; no other file in
`stops/` does. The prose consistently uses the 40-character form, and the manifest comment
uses the 40-character form, but the filename and the README row's heading use the short form.
Nothing states the rule (short SHA? how many characters? what happens at a third pin —
`5.5-property.<sha3>.cs`, or does the original get retro-renamed to `5.5-property.07607bf.cs`
for symmetry?). Low cost now; the asymmetry between the un-suffixed original and the suffixed
addition is the kind of thing that becomes ambiguous on the third one.

### P11 — Portability nit on the manifest claim.

`README.md`: "`sha256sum` ignores `#`-prefixed lines, so the second header does not need
special handling to check." True and verified for GNU coreutils `sha256sum` on this machine
(exit 0, no `WARNING: … lines are improperly formatted`). It is a GNU behaviour; Perl
`shasum -a 256 -c` and BusyBox are not guaranteed to agree, and `shasum` is not installed here
so I could not test it. The sentence names `sha256sum` specifically, so this is a nit rather
than an error — worth a word only because the surrounding document is otherwise scrupulous
about naming its assumptions.

---

## What I verified and found correct

Recorded so `reviewer-reconcile` does not re-do it:

- **The new fragment is byte-for-byte verbatim.** `sed -n '779,826p'
  Picea.Abies.Tests/History/UndoRedoSpec.cs` diffed against the fragment: **identical**, no
  differences. And `git diff --name-only 70d9ae5..HEAD` confirms `UndoRedoSpec.cs` was not
  touched between the pin and HEAD, so the working-tree copy *is* the `70d9ae5` content — the
  cited line range and commit are both exactly right.
- **Every checksum verifies.** `sha256sum -c SHA256SUMS` from `content/demo/`: 40 of 40 OK,
  exit 0, no warnings.
- **The line-count claims are right.** `SHA256SUMS` is 42 lines: 39 in the first section + a
  blank + the `#` header + 1 entry = 40 checksum lines. Both "39 lines" and "all 40 lines"
  in the new paragraph are accurate.
- **The commits resolve and are on `main`.** `07607bf` → `07607bf71152ef8d224352e2d2a737d8312dcb2d`,
  an ancestor of `origin/main`; `70d9ae58b1…` is `origin/main` and an ancestor of HEAD.
- **The vacuity reasoning is sound.** Both `continue` statements precede all assertions;
  the floor is the correct fix and the same pattern appears at three other sites in
  `UndoRedoSpec.cs` (`:635`, `:860`, `:917`), so it is a consistent amendment, not a one-off.
- **No build impact.** Presentation project builds clean; the fragment is excluded from
  compilation; semgrep rules do not reach it.
- **Every file in `stops/` has an index row** (21 files, 21 matching rows). The three README
  rows with no corresponding file — `5.2-line-abstract.txt`, `5.2-refusal.log`,
  `5.6-refusal.log` — are pre-existing and are explicitly documented as "**Not found at
  `07607bf`** … Created no file", which is the correct handling.

---

## What I could not determine from the code alone

1. **Was the vacuous-pass hole actually found by review before the lock, as the row claims?**
   The row states "The review found this before the lock." I have no way to check this; it is
   a claim about process, and the artifacts that would settle it are denied to me. Worth
   checking, because it is the row's justification for the pairing existing at all.
2. **Is the false "both pass" claim (P1) an error, or a shorthand for something narrower** —
   e.g. "both are internally consistent and neither would fail if run"? If the latter, the
   sentence still needs rewriting, but the finding is editorial rather than factual.
3. **Was the hand-edited two-section manifest a considered trade against re-running the
   generator (P5), and was the false-positive cost of the diff path noticed?** If it was
   considered and accepted, that belongs in the README next to the regeneration instruction.
4. **Was the spec-text-vs-test-file join (P3) known and judged not worth marking**, or did it
   pass unnoticed? The row is meticulous everywhere else about provenance, which makes an
   unmarked join here more likely an oversight than a decision.
5. **Are the four out-of-scope staged files (P9) intended for this commit?** In particular,
   should a realist agent-memory note ship in a docs commit.
6. **What does the `04-realist-plan.md` edit say, and is it consistent with the three
   presentation files?** I deliberately did not look. If the plan's four edit sites concern
   the same INV-3/vacuity material, then P1–P3 may or may not already be addressed there —
   `reviewer-reconcile` must check that hunk itself and cannot inherit a pass from me.
7. **Does `UndoRedoSpec.cs`'s `<Compile Remove>` exclusion get lifted at "plan step 6", and
   does the bundle intend to re-pin the fragment again at that point?** If so, the filename
   convention question in P10 becomes live rather than hypothetical, and P1's "both pass"
   sentence would become true for exactly one of the two versions — which would need saying
   differently again.
