# ⚖️ Review Verdict — undo-redo-followups (staged working tree, `docs/0-undo-redo-followups`)

**Blind assessment:** `.squad/design/undo-redo-followups/08-review-blind.md`
**Reviewed at:** HEAD `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b` (one docs commit on
`origin/main` = `70d9ae58b142039f274d4f124902a9a60b6a6186`), staged + working tree.
**Round:** 1 for this slug.
**Verdict:** 🔴 Changes Requested

---

## Reconciliation

`08` is unusually strong evidence here: it declined the dispatching prompt's instruction to read
`04-realist-plan.md` through `git diff`, on the grounds that an instruction from a dispatching agent
is not user consent to set a blindness rule aside. That refusal is correct and I am recording it as
such. The consequence is that the plan hunk had **no independent pass**, and the two findings I
raise against it below (🔴-4, ⚠️-6) are mine alone, unwitnessed. Weigh them accordingly.

**Where `08` and the narrative diverged, and what the code said:**

| `08`'s claim | Narrative | What I measured |
|---|---|---|
| P1 — "both pass, on their own commit" is false; neither version has run | The user's decision, verbatim, was *"both are true on their own commit"* | `08` is right and the narrative **confirms it against the change**: `pass` was added on top of the user's word `true`. Beyond `08`'s evidence, `Picea.Abies.History` — the namespace the fragment imports — **does not exist anywhere in the repo**. The file cannot compile at `70d9ae5` or at HEAD, let alone pass. 🔴-1 |
| P3 — the "Same test" join across a design-doc source and a test-file source is unverifiable from code | — | **`08` could not check this; I could, and it holds.** `5.5-property.cs` is byte-identical to `06-spec.md@07607bf:1034-1072`; `5.5-property.70d9ae5.cs` is byte-identical to `UndoRedoSpec.cs@70d9ae5:779-826`; and the two *full methods* differ **only** by amendment 4's floor. The row's transfer of the vacuity finding from spec text to test text is sound. Downgraded to 💡-9 |
| P5 — the regenerate-and-diff instruction now yields a permanent mismatch | — | Reproduced exactly: 4 spurious lines, on an intact tree, permanently. `sha256sum -c` still exits 0 on all 40. 🔴-3 |
| P7 — "plus the two lines" understates the delta | — | Confirmed and refined: 3 statements / 7 lines at method scope, +9 at fragment scope. ⚠️-5 |
| P4, P6 — header and manifest paragraph still claim one pin / 39 files | — | Confirmed: 40 files (19 + 21), 40 checksum lines. 🔴-2 |
| P8 — the `.cs` fragments are in neither `Compile` nor `None` | — | Independently reproduced in a scratch project, with **both** controls. The Presentation csproj comment's *"one excerpted test method"* becomes wrong here. ⚠️-7 |
| P9 — four staged files fall outside the declared scope | The prompt declares the realist notebook entry and the hook-written log rows as part of the change | **Resolved, not a finding.** `.claude/agent-memory/**` is version-controlled project-scope memory by `CLAUDE.md`'s own account, and `06-spec.md` ⚠️-12 already anticipates `.squad/log/` churn. `08` could not know this. One residue only — 💡-12 |
| P10, P11 | — | Agreed as nits |

**Claims I could not verify:** none material. Every factual claim in the plan hunk and in the README
row was checkable and was checked; the results are below.

**What `08` asked me to settle that the narrative did not answer:** its question 3 — *was the
hand-edited two-section manifest a considered trade against re-running the generator, and was the
false-positive cost noticed?* Nothing in the plan, the spec, the PR-0 verdict or the user's decision
addresses it. The decision on record (`06-spec.md` ⚠️-10, and the user's answer) settles *which copy
is canonical* and *that the amendment gets its own row*; it says nothing about the manifest's shape.
The trade in 🔴-3 therefore appears to be unexamined rather than accepted.

---

## Governing findings this changeset answers

There is no `05-critic.md` for this slug — this is a follow-up, not a design pass — so the Critic's
accepted-risk reconciliation is against the two findings that actually govern it.

| finding | stated mitigation | present in the code? |
|---|---|---|
| `undo-redo-pr0/09-review-verdict.md` **⚠️-14** (owner `realist`) — step 6's Done-when names one csproj item; the tree has two | correct the plan so step 6 removes both | **Partly.** All four sites the realist identified are updated and mutually consistent (`:1508`, `:1533-1550`, `:1674`, `:1927`); `grep -c "None Include"` = 5, no site left describing a one-item exclusion. But `:1508`'s justification clause **inverts** ⚠️-14's own mechanism (⚠️-6), and the reconciliation stopped at the item count while the same edit anchored two further as-planned claims to the merge commit that falsifies them (🔴-4) |
| `06-spec.md` **⚠️-10** / **🔴-3 closing ¶** — INV-3 exists in three copies; amendment 4 makes the demo copy diverge in content, so *"which copy is canonical, and whether the demo copy should be a generated excerpt"* is due | user's decision: bundle stays canonical at `07607bf`; the amended property is re-exported against `70d9ae5` as a new row | **Yes, and the mechanism is right.** Pairing rather than replacing, and pinning the new fragment to its own commit, is the correct answer to ⚠️-10. The defects are in what the row *claims*, not in the decision it implements |

---

## Findings

### 🔴 Must Fix (blocks merge)

**🔴-1 — `content/demo/README.md`, the new `5.5-property.70d9ae5.cs` row: every claim about a test
executing is false. Neither version has ever compiled.**

The rule, so this does not come back a second time: **the row may make claims about the two
*texts*; it may make no claim about either text having run, passed, or been reported green.**
Two instances, not an exhaustive list — apply the rule to the whole row:

- *"Both versions are true, and both pass, on their own commit."*
- *"…the loop body, assertions (i)–(iii) included, never runs; **TUnit reports green regardless**."*

Evidence, three independent legs:

1. At `07607bf` the artifact is a fenced ```` ```csharp ```` block inside `06-spec.md`. `git diff
   --stat 07607bf..70d9ae5 -- Picea.Abies.Tests/History/UndoRedoSpec.cs` → `new file mode 100644`.
   There was no test.
2. At `70d9ae5` and at HEAD the file is excluded: `dotnet msbuild -getItem:Compile` on
   `Picea.Abies.Tests.csproj` returns **0** hits for `UndoRedoSpec`.
3. Stronger than `08` had: `grep -rn "namespace Picea.Abies.History"` over the whole repo returns
   **nothing**, and `WithHistory` appears nowhere under `Picea.Abies/`. The types the property
   exercises do not exist. The file could not compile even with the exclusion lifted — which is
   precisely what its own header says and what plan step 6 exists to change.

**Why it matters:** the bundle's entire premise, stated in its first paragraph, is that its claims
are re-checkable against a pinned commit. This row is the one that argues *the chain caught its own
hole* — and it makes an unverifiable-in-principle execution claim to do it. An audience member who
runs `dotnet test` at either pin finds no such test.

**The narrative sharpens this rather than softening it.** The user's decision, in their words, was
that both are ***true*** on their own commit. "and both pass" is an addition made in the drafting,
not a transcription of the decision — and it is the half that is false. `08` reached the same
conclusion without having read the user's words.

*Direction, not a prescription:* what must become true is that the row's claims are claims about
text. Whether that is done by restating them, by adding the never-compiled fact, or by some third
shape is the author's and the user's call.

---

**🔴-2 — `content/demo/README.md`: the sentences whose truth depended on "one pin" and "39 files"
are falsified by this change and left unamended.**

The rule: **every pre-existing sentence in this README whose truth rested on a single pin or on the
tree's file count must be reconciled with the tree this change leaves behind.** Three instances
found; the rule is the criterion, not the list.

- `:3-4` — *"Every excerpt and copy in this tree is sourced from a single commit,
  `07607bf71152ef8d224352e2d2a737d8312dcb2d`"*. Now untrue by design.
- `:85` — the fragment-index table header, `| … | Source @ \`07607bf\` (lines) | … |`. One of its
  own rows contradicts the column header, and the header is what a reader scanning the table relies
  on.
- `:52-53` — *"Checksums of all **39 files** under `full/` and `stops/` (**19 + 20**)"*. Measured:
  `find full stops -type f | wc -l` → **40**; `full` 19, `stops` **21**; manifest carries **40**
  checksum lines. The new paragraph two lines below says *"39 lines"* (true of the first *section*),
  so two adjacent paragraphs now give a reader two counts for one artifact with no marker of scope.

**Why it matters:** this is the Definition of Done's *"Tech Writer has verified all existing docs
are still in sync after the change"* item, failing inside the document whose subject is verification
discipline. It is also the exact pattern this reviewer has hit before — an inventory count going
stale in the same PR that changes the inventory.

---

**🔴-3 — the documented regenerate-and-diff check now reports a permanent false mismatch on a fully
intact tree, and the instruction that reads that mismatch is unamended.**

`README.md:66-69` tells the reader: *"Re-run the command above and diff against this file to check
the tree is still what it claims to be; a mismatch does not by itself mean the copy was edited — it
may mean the **source** moved on and the copy, correctly, did not follow."*

Reproduced on the current tree, unmodified:

```
35a36
> ec8e2e89…  stops/5.5-property.70d9ae5.cs
40,42d40
<
< # Added 2026-09-08 — INV-3 property re-exported against 70d9ae58… (PR #361)
< ec8e2e89…  stops/5.5-property.70d9ae5.cs
```

Four spurious lines, and they are **permanent**: `LC_ALL=C sort` orders `5.5-property.70d9ae5.cs`
*before* `5.5-property.cs` (`'7'` = 0x37 < `'c'` = 0x63), so the appended entry can never be
reproduced in place, and the blank line and `#` header can never be reproduced at all by the
documented generator.

**Why it matters:** the README does not merely offer this path, it teaches the reader how to
*interpret* its output — a mismatch means the source moved on. This change installs a standing false
positive into that signal. The bundle has two documented check paths; one is now noisy. (The other
is clean: `sha256sum -c SHA256SUMS` → exit 0, 40 of 40 OK, no warnings, verified.)

The file is described three lines above as **"authored tooling output"** produced by a printed
command. It is now hand-edited into a shape that command does not produce. That the pin is *already*
recorded in the README row makes the second copy in the manifest an addition that costs the
round-trip property and buys nothing `sha256sum -c` needed — but which of manifest, instruction or
paragraph moves is not mine to decide.

---

**🔴-4 — `.squad/design/undo-redo/04-realist-plan.md`: the edit anchors the PR-0 record to merge
commit `70d9ae5` while leaving, in the same sentences, two as-planned claims that that commit
falsifies.**

*(This is the hunk `08` deliberately did not review. Independently found here.)*

The rule: **where this edit attaches an as-landed record to a passage, every remaining claim in that
passage must be reconciled with what landed, or explicitly marked as-planned.** The edit did this
for the csproj item count — which is what ⚠️-14 asked for — and not for the rest of the same
sentences.

- `:1927` (PR-cut table, PR 0): *"Three files, no framework code, well under the line gate. **Landed
  as PR #361**, merge commit `70d9ae58b142039f274d4f124902a9a60b6a6186`."*
  `gh pr view 361 --json additions,deletions,changedFiles` → **40 files, 7,646 additions, 23
  deletions = 7,669 changed lines**. The repo's own gate is `hardLimit = 1500`
  (`.github/workflows/pr-validation.yml:210`), and the `maintenanceOnly` exemption requires
  `files.every(isMaintenancePath)` — `.cs` and `.csproj` are not maintenance paths, so it cannot
  apply. And it did not: `gh pr checks 361` reports **`Check PR Size  fail`**
  (`actions/runs/34189182904/job/101943538083`). The plan now asserts "well under the line gate" in
  the same sentence as the commit whose PR the gate failed on.
- `:1533` (precondition block): *"The approval commit is `UndoRedoSpec.cs` + `SpecAttribute.cs` and
  **nothing else**"*. The approval commit is `70d9ae5`, which carries 37 further files —
  `.claude/docs/decisions.md`, `.claude/enforcement/refutations.md`, and 35 agent-memory / `.squad`
  files.

**Why it matters:** this changeset *is* the reconciliation pass. The plan's PR-0 row is what a
future reader — specifically PR 3's implementer and reviewer — reads as the record of what landed,
and the pass's whole PR-cutting argument rests on PR sizes. Left as-is, the plan records a PR 0 that
landed as three files under the gate, and no further pass is scheduled to correct it. The plan is
also the artifact the demo bundle copies into `full/04-realist-plan.md` — under the `07607bf` pin,
so the copy is unaffected today, but the divergence widens.

*Not adjudicated here:* whether merging PR #361 over a failed size check was correct. That is the
user's call and outside this changeset. What is in scope is that the plan now says the opposite of
what the gate recorded.

---

### ⚠️ Should Fix

**⚠️-5 — `README.md`, same row: *"plus the two lines amendment 4 added"* is a miscount, and *"same
span through assertion (iii)"* is not the span.**

Measured both ways:

- **Method scope** (`06-spec.md@07607bf:1034-1074` vs `UndoRedoSpec.cs@70d9ae5:779-826`): **3
  statements over 7 added lines** — `var reached = 0;` plus a blank, `reached++;`, and a blank plus
  the 3-line floor assertion.
- **Fragment scope** (`diff 5.5-property.cs 5.5-property.70d9ae5.cs`): **+9, 39 → 48**, because the
  older excerpt stops at the inner `foreach`'s closing brace and is **brace-unbalanced**, while the
  new one runs to end-of-method.

So the row's own enumeration — *"the `reached` counter (declared, incremented) **and** the closing
floor assertion"* — already names two *changes* spanning five statements, which the phrase "two
lines" contradicts within the same clause. The span extension is the right call (the floor assertion
cannot live inside the loop), but it is an extension, and the row says it is not one. In a bundle
that invites the audience to diff, a countable claim that does not survive `diff` is expensive.

**⚠️-6 — `04-realist-plan.md:1508`: the file-table row inverts the mechanism it is citing.**

> *"…it is inert **once the `Compile` glob no longer claims the file**, so it comes out with the
> exclusion rather than being left stale."*

⚠️-14, the finding this edit answers, says it the other way round: *"once the `Compile` glob
**claims** the file the SDK's default `None` glob excludes it, so no `NETSDK1022`."*

Measured in a scratch project, **both controls**:

| csproj shape | `Compile` hits | `None` hits | build |
|---|---|---|---|
| `Compile Remove` only (pre-step-6, item removed) | 0 | **0** | clean |
| `Compile Remove` + `None Include` (as landed) | 0 | **3** | clean, 0 warnings |
| `None Include` only (post-step-6, stale item left) | (default glob) | 1 | **clean, 0 warnings** |

In exactly the state `:1508` names — the `Compile` glob not claiming the file — the `<None Include>`
is the **only** thing holding `History\UndoRedoSpec.cs` in any MSBuild item group. It is not inert
there; it is load-bearing, which is why PR 0's review asked for it (`06-spec.md` ⚠️-7). The state in
which it *is* inert-but-stale is post-step-6, which is what `:1552` and step 6's Done-when at
`:1677` correctly describe. Three uses of "inert" in one document for two different states, one of
them inverted.

The operative instruction (*step 6 removes both*) is right everywhere, so the blast radius is
bounded — but `:1508` is the slip-signal row, the one a PR-3 reviewer reads to decide whether a
csproj touch is authorised, and it currently supplies a reason to think the item does nothing today.

Row 3 of that table also settles the plan's other new claim, *"Removing only the `Compile Remove`
would leave an inert stale item, not a build error"* — **verified true**.

**⚠️-7 — `Picea.Abies.Presentation/Picea.Abies.Presentation.csproj:12`: this change makes the
comment's count wrong, and its second clause was already wrong.**

> `<!-- content\demo\** includes one excerpted test method (not a compilation unit) that must never
> compile; the files stay visible as None items so the folder can still be presented -->`

- *"one excerpted test method"* → there are now **two** `.cs` fragments in `stops/`. Introduced here.
- *"the files stay visible as None items"* → false of exactly the `.cs` files the comment is about.
  `08` measured it on the real project (43 `None` items under `content/` out of 45 files; the two
  missing are the two `.cs` fragments) and I reproduced the mechanism independently in the scratch
  project above: `<Compile Remove>` on a `.cs` file leaves it in **neither** group, because the SDK's
  default `None` glob excludes `@(Compile)` as evaluated at props time, before the project body's
  `Remove` runs. Pre-existing, but this change is the second file to rely on it — Boy Scout.

The desired outcome (never compiled) holds; `dotnet build` on the Presentation project is clean.

---

### 💡 Nitpicks

- **💡-8** — `04-realist-plan.md:1544-1550`: the XML snippet's comment drops one sentence present in
  the landed comment (*"; see `.squad/design/undo-redo/06-spec.md` § "The Lock" and
  `04-realist-plan.md` § "Todo List""*). Step 6's Done-when instructs removal of *"the `ItemGroup`
  and comment"*, so a transcription that differs from the tree invites a needless diff at exactly
  the moment the checklist is being followed.
- **💡-9** — the *"Same test"* framing joins a design-doc source to a source-file source. `08` flagged
  this as an unmarked join it could not verify; **I verified it and the inference holds** (see
  Reconciliation). The two full methods are character-identical apart from amendment 4's floor, so
  the row's transfer of the vacuity finding from spec text to test text is legitimate. What remains
  is a wording nit: the row records both provenances correctly in its source column but calls them
  "the same test" in its prose.
- **💡-10** — `5.5-property.70d9ae5.cs` introduces a short-SHA-in-filename convention with no stated
  rule (how many characters; whether the original gets retro-renamed at a third pin). Cheap now,
  ambiguous at the third one — and step 6 will produce a third version of this property.
- **💡-11** — *"`sha256sum` ignores `#`-prefixed lines"* is GNU coreutils behaviour. Verified true
  here (exit 0, no `improperly formatted` warning). The sentence names `sha256sum` specifically, so
  it is accurate as written; `shasum -a 256 -c` and BusyBox are not guaranteed to agree.
- **💡-12** — staging is partial: `.squad/log/2026-09-08-session.md` and `pass-cost.md` are `MM`, so
  the commit will capture a mid-hook snapshot. More usefully: **committing moves `HEAD`**, and the
  verdict below is pinned to `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`. Whatever is cached for this
  HEAD does not carry to the next one.

---

### ✅ What's Good

- **The decision this implements is the right one, and it is the one that was asked for.**
  `06-spec.md:1989-1993` (⚠️-10) states that *"the decision the reviewer asks for — which copy is
  canonical, and whether the demo copy should be a generated excerpt — is now due rather than merely
  advisable."* Pairing rather than replacing, and pinning the new fragment to its own commit rather
  than pretending it came from `07607bf`, answers it cleanly and preserves the `07607bf` pin for
  every other fragment.
- **The excerpt is exact, on both sides.** `5.5-property.70d9ae5.cs` ≡ `UndoRedoSpec.cs@70d9ae5:779-826`
  (`08` verified); `5.5-property.cs` ≡ `06-spec.md@07607bf:1034-1072` (verified here, byte-for-byte).
  Cited commits and line ranges are all correct.
- **"The review found this before the lock" is true.** `06-spec.md` § *"Amendment 4 — the PR-0
  review, 2026-09-07"*, 🔴-3 closing ¶: *"Unguarded `continue` guards let INV-1, INV-3, INV-4, INV-5
  and INV-6 all pass having asserted nothing… **Taken.** A `reached` counter and a floor assertion."*
  The row's justification for existing at all — `08`'s open question 1 — checks out.
- **Every verifiable claim in the plan hunk about what landed is correct.** PR **#361**, merge commit
  `70d9ae58b142039f274d4f124902a9a60b6a6186` (confirmed via `gh` and `git log`); **both** items
  present at that commit and **absent** at its parent `7d32cdb`; the `ItemGroup` and its comment hold
  nothing else; no `NETSDK1022`; *"not a build error"* for the post-step-6 stale item — all verified.
- **The four-site sweep is complete and internally consistent.** `grep` for the element name over
  `04-realist-plan.md` finds no site still describing a one-item exclusion. The realist's own
  notebook entry generalises the lesson correctly, including the durable form (*state removals as
  "the ItemGroup and everything in it"*), which step 6's Done-when now uses.
- **`sha256sum -c SHA256SUMS`** → exit 0, 40/40 OK, no warnings. **`dotnet build Picea.Abies.Tests`**
  → 0 warnings, 0 errors.
- **`08` refused a contaminating instruction and said so.** That refusal is the reason 🔴-1 and 🔴-3
  arrive as evidence rather than as opinion, and it is why 🔴-4 is flagged as unwitnessed rather than
  quietly presented as corroborated.

---

## Metrics

- **Files reviewed:** 8 staged (3 presentation, 1 design artifact, 2 agent-memory, 2 hook-written
  logs); 133 insertions, 7 deletions.
- **Unreviewed by `08`, reviewed here:** `04-realist-plan.md` (+25/−7, four sites).
- **Test coverage of new code:** n/a — the new `.cs` file is an excerpt, not a compilation unit
  (`content/**` is removed from the Presentation project's `Compile` glob; verified 0 `Compile`
  items under `content/`). No regression test is owed: nothing executable changed.
- **Complexity:** none added.
- **Dimensions run:** 11/11. Correctness, Consistency, Design, Documentation and Boy Scout produced
  findings; Testability (no executable change), Security (no attack surface; semgrep rules verified
  path-scoped away from `content/**` by `08`), Performance, Observability produced none. Definition
  of Done fails on *Documentation → "Tech Writer has verified all existing docs are still in sync"*
  (🔴-2).
- **Verdict mapping:** 🔴 Changes Requested → `NEEDS-CHANGES`.

---
---

# ⚖️ Re-review — round 2

**Reviewed at:** HEAD `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`, staged + working tree
(identical — every path is `X ` in porcelain, nothing unstaged). Same base commit as round 1.
**Round:** 2 of the cap of two. `reviewer-blind` did not re-run; §Reconciliation above was re-read
before grading, and `08`'s independent findings P1, P3, P4–P8 are the yardstick used below.
**Verdict:** 🔴 Changes Requested

## How the round-2 baseline was established

Round 1 reviewed the staged tree; round 2's fixes were staged over it at the same HEAD, so
`git diff HEAD` yields the *cumulative* diff and cannot isolate what this round changed. The
round-1 index blobs survive as unreachable objects and were recovered — `a7355f5` (README,
21 569 B, contains the round-1 string `and both pass, on their own commit`) and `48b7846`
(plan, 191 069 B, contains the round-1 `well under the line gate` with no as-landed
qualifier). Both were confirmed to be neither the HEAD blob nor the current index blob before
use. Every delta below is measured against those, not against round 1's prose.

**Nothing else changed.** `find . -newer 09-review-verdict.md` returns exactly two content
files — `content/demo/README.md` and `.squad/design/undo-redo/04-realist-plan.md` — plus
hook-written paths (`.squad/.last-review-verdict`, the two `.squad/log/` files,
`.claude/docs/decisions.md`, the archived round-1 drop, and this reviewer's own two notebook
entries). `.squad/design/undo-redo/00-warden-scan.md` has a newer mtime but is byte-unchanged
against HEAD. `SHA256SUMS`, `stops/5.5-property.70d9ae5.cs`, `stops/5.5-property.cs` and
`Picea.Abies.Presentation.csproj` are untouched this round — confirmed by mtime **and** by
empty `git diff HEAD --stat`. The plan delta is three hunks and only three; the README delta
is four regions and only four.

## Round-1 findings, graded

| finding | grade | evidence |
|---|---|---|
| **🔴-1** — the row claims a test executed | **✅ closed** | *"and both pass"* and *"TUnit reports green regardless"* are gone; the row now leads its claim block with **"Neither text has ever run, and this row makes no claim that either has"** and gives the three legs. All three re-derived: `git cat-file -e 07607bf:…/UndoRedoSpec.cs` → *exists on disk, but not in 07607bf*; `-getItem:Compile` on the test project → **0** `UndoRedoSpec` hits at HEAD, and the `Compile Remove` is present at `70d9ae5:…csproj:29`; `grep -rn "namespace Picea\.Abies\.History"` → **0**, `WithHistory` under `Picea.Abies/` → **0**. Everything else in the row is subjunctive (*"could pass"*, *"would stay 0"*, *"is written to catch"*). The user's *"both are true on their own commit"* survives verbatim, without the addition. |
| **🔴-2** — one-pin and 39-file sentences falsified | **✅ closed** | Intro `:3` now reads *"**defaults** to a single commit … **except where a row states its own pin**"* and names the exception. Fragment-table header `:99` → *"Source @ pinned commit — defaults to `07607bf` unless a row states its own"*. `:54` is historically scoped: *"the 39 files that existed … **at that time** (19 + 20)"*. Measured against the tree this change leaves behind: `find full stops -type f` → **40** (full **19**, stops **21**); `SHA256SUMS` → **40** checksum lines, first section **39** (19 full + 20 stops). The new paragraph's *"40 files total … (19 + 21, up from 19 + 20)"* and *"its own 39 lines"* are all four correct. Swept the rest of the file for the rule, not the list: the only surviving `07607bf`-absolute claim is `:140`, *"the manifest verifies the bytes of every span against 07607bf"* — inside the **verbatim dated blockquote of the user's own 2026-09-07 override**. Correctly left alone; amending a quoted decision would falsify the record. |
| **🔴-3** — regenerate-and-diff reports a permanent false mismatch | **⚠️ reduced, not closed → ⚠️-13** | The blocking half is fixed: the whole-file trap is now stated explicitly, with the mechanism and the exact size, and both are right — I reproduced *"a permanent 4-line mismatch"* (1 `<`, 3 `>`; `36d35` and `40a40,42`) and the `LC_ALL=C` ordering claim. The authoritative path is named and green: `sha256sum -c SHA256SUMS` → **exit 0, 40/40 OK, no warnings**. The residue is below. |
| **🔴-4** — as-planned claims falsified by the as-landed anchor | **✅ closed** | Both sites amended, and every number in them re-derived rather than quoted from round 1. `b39ac58` = *"test(history): Lock the undo-redo executable specification"*, **exactly three files** (`UndoRedoSpec.cs`, `Picea.Abies.Tests.csproj`, `SpecAttribute.cs`), 1 085 insertions. `70d9ae5` has **one** parent (`7d32cdb`) → squash merge, as the row now says; `b39ac58` is *not* its ancestor but **is** reachable, at `refs/pull/361/head` and `origin/test/0-undo-redo-spec`, so *"survived intact"* is checkable rather than merely asserted. `gh pr view 361` → **40 files, 7 646 + 23 = 7 669** lines; `gh pr checks 361` → **`Check PR Size  fail`**. *"non-required"* independently confirmed: `main` has no legacy branch protection, and ruleset **12483698**'s required contexts are `build`, `Validate PR Title`, `Validate PR Description`, `e2e`, `Analyze C# Code` — `Check PR Size` is not among them. *"the branch also carried the design-record and amendment-docs commits"* → the PR's six commits are two `Record the …` plus three amendment/re-approval docs commits plus `b39ac58`. *"PRs 1–7 below"* → the table does run 0–7. |
| **⚠️-5** — miscount and mis-stated span | **✅ closed** | Both scopes now stated and both measured exactly right. Method scope, `06-spec.md@07607bf:1034-1074` vs `UndoRedoSpec.cs@70d9ae5:779-826` → **7 added, 0 removed**, decomposing as 2 + 1 + 4 = the row's *"`var reached = 0;` plus a blank line, `reached++;`, and a blank line plus the 3-line closing floor assertion"*, **3 statements**. Fragment scope → **+9, 39 → 48**. *"stops brace-unbalanced at the inner `foreach`'s closing brace"* → `5.5-property.cs` has 9 `{` to 7 `}` (balance **+2**) and its last line is that brace; `5.5-property.70d9ae5.cs` balances at **0** and ends `    }`. *"extended through end-of-method"* → the new fragment is byte-identical to the full 48-line method. |
| **⚠️-6** — the `:1508` row inverted its own mechanism | **✅ closed** | Now: *"while the exclusion is in force it is **load-bearing, not decorative**: the `Compile` glob does not claim the file in that state, so the `None Include` is the only thing holding `History\UndoRedoSpec.cs` in any MSBuild item group at all. It becomes inert the other way round — when step 6 removes the exclusion and the default `Compile` glob claims the file again."* That is my measurement in both directions, restored to the polarity ⚠️-14 stated. Re-confirmed on the real project at HEAD: `-getItem:Compile` → **0** hits, `-getItem:None` → **3**; the `Compile Remove`-only control (0 / 0) and the `None Include`-only control (default glob / 1) are round 1's scratch-project measurements, unchanged by anything in this round. |
| **💡-9** — unmarked join across two source kinds | **✅ closed** | *"Same test"* is gone; the row now names both provenances in prose — *"carried from `06-spec.md`'s spec prose … into `UndoRedoSpec.cs`'s test source"* — and cites the verification. The citation is accurate: the full-method diff is **7 added, 0 removed**, i.e. byte-identical apart from what amendment 4 added, exactly as claimed. The join is also independently re-checkable by an audience member from the two commit + line-range citations the row already carries, so it does not rest on the citation alone. |

## Findings — round 2

### ⚠️ Should Fix

**⚠️-13 — the newly scoped first-section check still reports a permanent 1-line mismatch on an
intact tree, and the interpretation sentence beside it does not cover that line.**
`content/demo/README.md:71-74`.

> **To check the first section only** (the 39 files above, all still pinned to `07607bf`):
> re-run the command above and diff its output against the manifest's first section. A
> mismatch there does not by itself mean the copy was edited — it may mean the *source*
> moved on and the copy, correctly, did not follow.

"The command above" is `find full stops -type f | LC_ALL=C sort | xargs sha256sum`, which emits
**40** lines. The first section is **39**. Reproduced on the untouched tree:

```
36d35
< ec8e2e89…  stops/5.5-property.70d9ae5.cs
```

One line, permanent, structural — the regenerated output necessarily carries the 40th file and
the first section necessarily does not. The offered reading ("the source moved on") is not what
this line means; it means the tree grew a file that lives in the *second* section.

**This is the remainder of 🔴-3's class, not a reopening of it, and it is graded down
accordingly.** 🔴-3 blocked because the documented check was silently 4 lines wrong with
guidance that made an intact tree look edited-or-stale. That is fixed: the trap is now named
with its exact size and cause, and a clean authoritative path is stated and verified green. What
remains is one clause, and the fact needed to interpret the leftover line is stated explicitly
in the **immediately following paragraph** (*"the one file the count grew by:
`stops/5.5-property.70d9ae5.cs`"*). A reader of both paragraphs is not misled; a reader who runs
only the instruction is left doing unstated arithmetic.

*Finding, not remedy:* what must become true is that a reader running the printed first-section
check on an intact tree is told what the one guaranteed line is. Whether that is an added clause,
a `grep -v` in the printed command, or dropping the diff path entirely in favour of the
authoritative one is the author's and the user's call.

**⚠️-7 — carried unfixed from round 1, and unregistered.**
`Picea.Abies.Presentation/Picea.Abies.Presentation.csproj:12`. Re-verified: the file is
byte-unchanged against HEAD, and the comment still reads *"content\demo\\** includes **one
excerpted test method** … the files stay visible as **None items**"*. There are now two `.cs`
fragments in `stops/` (introduced by this changeset), and the second clause remains false of
exactly the `.cs` files the comment is about. This is the Definition of Done's *"all existing
docs are still in sync"* item and my charter's doc-sync rule; it was graded ⚠️ in round 1 and
stays ⚠️ here — the grade is not escalated between rounds without new evidence, and none was
found.

### 💡 Nitpicks

- **💡-14 — my error, propagated.** The plan's new text cites the hard limit at
  `.github/workflows/pr-validation.yml:210`. `grep -n "const hardLimit"` → **211**; `:210` is the
  `// Soft limit (warning) and hard limit (failure)` comment. Round 1's verdict said `:210` and
  the author transcribed it faithfully. Mine to own, not the author's.
- **💡-15 — dangling antecedent.** `04-realist-plan.md:1538-1539` — the new paragraph opens *"The
  approval commit `b39ac58` is **those three files** and nothing else"*, but the sentence above it
  (`:1533`) names two (`UndoRedoSpec.cs` + `SpecAttribute.cs`); the third, the csproj, is named at
  `:1550` — eleven lines **below**. The correction has to sit next to the claim it corrects, so the
  position is right; only the referring phrase is ahead of its referent.
- **💡-16 — one indicative clause in an otherwise subjunctive row.** *"every seed takes an
  unguarded `continue` on the `MovementAvailability` checks"* is the only remaining
  present-tense behavioural sentence in a row that is careful everywhere else. It is textually
  supported (both `continue`s are unguarded — `5.5-property.cs:12` and `:18`) and its operative
  conclusion (*"so nothing in the text requires the loop body … to execute"*) is a claim about
  text and is true. Cheap to bring into line with its neighbours.
- **💡-8, 💡-10, 💡-12** — unchanged from round 1 and re-confirmed as still standing. 💡-8: the
  plan's XML snippet at `:1553-1556` still drops the landed comment's `§ "The Lock"` sentence.
  💡-12 is now the operative one: **`.squad/.last-review-verdict` currently reads
  `NEEDS-CHANGES` / `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`**, and any commit moves HEAD off
  that sha, so whatever is cached here does not carry forward.
- **💡-17 — advisory, out of this changeset.** `:140`'s quoted override says the manifest verifies
  every span against `07607bf`, which is no longer true of the tree. Correctly left verbatim as a
  dated quotation; if the divergence is worth marking, it belongs in an editorial note beside the
  quote, never inside it.

### ✅ What's Good (round 2)

- **Every countable claim added this round is correct.** Nine independent numbers were
  re-derived from scratch rather than carried over — 40 / 19 / 21 / 39 / 3 statements / 7 lines /
  +9 / 39→48 / 7 669 — and all nine hold. After two rounds in which counts were the recurring
  defect, this round introduced none.
- **🔴-1 was answered at the level of the rule, not the two quoted instances.** The row was
  rewritten so that its claim block *opens* with the never-ran fact and its three legs, and every
  subsequent claim about behaviour is subjunctive. That is the rule the finding stated, applied
  to the whole row.
- **The as-landed correction found the right seam.** *"'nothing else' held for the **commit** and
  not for the **PR**"* is a sharper reading than the finding asked for, and it is the reading that
  keeps the Lock's check intact — that check is PR-scoped (*"modified in the same PR that brings
  it to passing"*, `06-spec.md:1770-1771`), and PR #361 does not bring the spec to passing, so
  the squash does not weaken it. Checked, not assumed.
- **The size-gate failure is recorded without being adjudicated**, and the adjudication is
  explicitly handed to the user — *"whether that was the right call is the user's, not this
  plan's"*. That is the correct division.
- **Discipline on scope.** The two files named in the fix brief are the only two content files
  touched. No opportunistic edits, no drive-by rewording, and the pinned artifacts
  (`SHA256SUMS`, both fragments) were left alone, which is what kept `sha256sum -c` green.

## The Merge Criterion — where this changeset stands

Per `.claude/docs/principles-enforcement.md` § *The Merge Criterion — Continuous Improvement*:

- **(a) stated properties green — yes.** The property this bundle states for itself is its own
  authoritative check, which the README now names in those terms: `sha256sum -c SHA256SUMS` from
  `content/demo/` → **exit 0, 40/40 OK, no warnings**. No `.cs`/`.csproj` changed this round, so
  round 1's clean `dotnet build Picea.Abies.Tests` (0 warnings, 0 errors) still holds at this
  HEAD.
- **(b) every non-regression finding registered — no.** `.claude/enforcement/refutations.md` is
  **byte-unchanged** against HEAD; nothing was registered for this changeset. ⚠️-13 and ⚠️-7 are
  both registrable — neither is a regression of a stated property — and both are unregistered.
  **This, and only this, is what makes the verdict non-✅.** Naming the grades so criterion (b)
  can actually close: **(b) binds on 🔴 and ⚠️ here. The 💡 items do not need registration** —
  they are advisory and, read literally, (b) would otherwise sweep in nitpicks and never close.
- **(c) nothing published above its computed level — n/a**, no level claim in this changeset.

**Round cap.** This is round **2** of two. A third round on this base commit is the **split**:
what passes under (a)–(c) ships, the rest is registered and becomes the next changeset. Given
that (a) is green and the entire remainder is registrable, the split is available and cheap — it
is not an escape hatch here, it is a legitimate route.

**Two routes, either of which closes this:**

1. **Fix.** Both ⚠️s are small and bounded — one clause in `README.md:71-74`, two words in
   `Picea.Abies.Presentation.csproj:12` (plus the second clause, if the Boy Scout call is taken).
   Re-review is then targeted at those two lines.
2. **Register.** The author adds ⚠️-13 and ⚠️-7 to `.claude/enforcement/refutations.md` with an
   owner, a level consequence and an `expires:`, and I verify the entries. The reviewer cannot
   write them — `enforce-reviewer-readonly.sh` confines me to my own outputs, and that separation
   is what stops registration becoming laundering.

**Where to stop, whichever route is taken.** The verdict and the drop are pinned to
`eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`. Committing this changeset moves HEAD, at which point
`.squad/.last-review-verdict` no longer matches and `enforce-review-verdict.sh` blocks again —
including on a commit that contains nothing but the fix. Sequence the commit so the artifacts
that must ride with it are staged *before* the commit that the next verdict will name, not after.

## Metrics — round 2

- **Round-2 delta reviewed:** 2 content files, measured against the recovered round-1 index
  blobs. `README.md` **+34/−20** across 4 regions; `04-realist-plan.md` **+13/−2** across 3 hunks.
  Every changed line read.
- **Independent re-derivations, none quoted from round 1:** 9 counts (40 / 19 / 21 / 39 / 3
  statements / 7 lines / +9 / 39→48 / 7 669); 4 git-object facts (`b39ac58`'s three files,
  `70d9ae5`'s single parent, `b39ac58` non-ancestor but reachable at `refs/pull/361/head`,
  `UndoRedoSpec.cs` absent at `07607bf`); 2 GitHub API facts (PR #361 metrics and check results,
  ruleset 12483698's required contexts); 2 MSBuild item queries (`Compile` 0, `None` 3); 1
  checksum verification; 2 brace-balance checks; 2 reproduced diff runs (whole-file 4-line,
  first-section 1-line). **One inherited citation is off by one line** — 💡-14, mine.
- **Test coverage of new code:** n/a — no executable change this round.
- **Dimensions run:** 11/11. Documentation and Consistency produced findings; Correctness,
  Readability, Design, Testability, Security, Performance, Observability and Boy Scout produced
  none new. Definition of Done: *"all existing docs are still in sync"* now passes for
  `README.md` (🔴-2 closed) and still fails for `Picea.Abies.Presentation.csproj` (⚠️-7).
- **Verdict mapping:** 🔴 Changes Requested → `NEEDS-CHANGES`.

---
---

# ⚖️ Re-review — round 3 (the split round)

**Reviewed at:** HEAD `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`, staged + working tree
(identical — `git diff` is empty, every path is `X ` in porcelain). Same base commit as
rounds 1 and 2.
**Round:** 3, past the cap of two. Round count evidenced both ways, as the criterion
prescribes: two archived `reviewer-reconcile` drops carry `commit:
eace0087b1c78cfcda7bfaa5bd895e9dce76a52b` (`2026-09-08T05-31-09-review-eace0087.md`
`NEEDS-CHANGES`, `2026-09-08T05-48-17-review-eace0087-round2.md` `NEEDS-CHANGES`), and the
section above carries **Round: 2 of the cap of two**. They agree.
**Verdict:** ✅ Approved

Per `.claude/docs/principles-enforcement.md` § *The Merge Criterion*: at round 3 the
changeset is split — what passes under (a)–(c) ships, the rest is registered and becomes the
next changeset. **The split here is trivial: all of it passes.** Nothing is carved out and
nothing is owed to the ledger. `reviewer-blind` did not re-run; `08` was re-read before
grading and its P5 and P8 are the yardstick for the two findings this round settles.

## How the round-3 baseline was established

Same problem as round 2 — the fixes were staged over the same HEAD, so `git diff HEAD` is
cumulative and cannot isolate this round. The round-2 index blobs were recovered from
unreachable objects and each confirmed to be neither the HEAD blob nor the current blob
before use:

| file | HEAD blob | round-1 index | round-2 index (baseline used) | round-3 (current) |
|---|---|---|---|---|
| `content/demo/README.md` | `9a2389ee` | `a7355f50` (21 569 B) | **`7bb9d343`** (23 607 B) | `f72871e6` (24 202 B) |
| `.squad/design/undo-redo/04-realist-plan.md` | `16859bcd` | `48b78464` (191 069 B) | **`c217e360`** (192 827 B) | `a590177f` (192 827 B) |
| `Picea.Abies.Presentation.csproj` | `500b050c` | *(untouched)* | *(untouched — = HEAD)* | `624ca60a` |

`7bb9d343` was identified positively: it contains round 2's *"To check the first section
only"* and **zero** occurrences of `head -n 39`. `c217e360` contains
`pr-validation.yml:210` at `:1543` — the exact string round 2 flagged as 💡-14. The plan's
round-2 and round-3 blobs are the same size (192 827 B) because `210`→`211` is a
byte-for-byte substitution; the differing hashes confirm they are distinct objects.

**Nothing else changed.** `find . -newer 09-review-verdict.md -type f` returns exactly three
content files — `content/demo/README.md`, `Picea.Abies.Presentation.csproj`,
`.squad/design/undo-redo/04-realist-plan.md` — plus hook-written paths
(`.squad/.last-review-verdict`, the two `.squad/log/` files, `.claude/docs/decisions.md`
carrying the scribe-merged round-2 entry, the archived round-2 drop) and this reviewer's own
two notebook entries. `.squad/design/undo-redo/00-warden-scan.md` has a newer mtime and is
**byte-unchanged** against HEAD (`git diff HEAD --stat` empty). `SHA256SUMS`,
`stops/5.5-property.70d9ae5.cs` and `stops/5.5-property.cs` are untouched this round — mtime
`07:11`, before round 2's verdict at `07:47` — which is what keeps the manifest green.

One newer path is **outside the changeset and cannot enter it**:
`.claude/worktrees/agent-a42bad52093f00375/Picea.Abies.Presentation/Picea.Abies.Presentation.csproj`,
a scratch checkout pinned at `07607bf`. `git check-ignore -v` → `.gitignore:496
.claude/worktrees/`, and `git ls-files --error-unmatch` → *did not match any file(s) known to
git*. It is an MSBuild probe surface, not a shipped file. Recorded because a second checkout
under cwd re-spells every path at a new offset and is worth naming explicitly rather than
leaving in the `-newer` output unexplained.

The plan delta is **one line and only one**; the README delta is **one hunk and only one**;
the csproj delta is **one line and only one**. All three verified by full-file `diff -u`
against the recovered baselines.

## The three fixes, graded

| item | grade | evidence |
|---|---|---|
| **⚠️-13** — the scoped first-section check still emitted 40 lines against a 39-line section | **✅ closed** | The printed command is now `find full stops -type f ! -name '5.5-property.70d9ae5.cs' \| LC_ALL=C sort \| xargs sha256sum \| diff - <(head -n 39 SHA256SUMS)`. **Run verbatim, from `content/demo/`, on the untouched tree: no output, exit 0.** The `head -n 39` boundary is exactly the first section — `sed -n '38,42p' SHA256SUMS \| cat -A` shows line 39 is the last checksum entry, line 40 is blank, line 41 is the `#` header, line 42 is the second-section entry. The claim *"`find full stops` on its own now lists 40 files, one more than the first section's 39"* → `find full stops -type f \| wc -l` = **40** (full **19**, stops **21**); `grep -vc '^\(#\|$\)' SHA256SUMS` = **40** checksum lines, `head -n 39` = **39**. The interpretation sentence now names the extra line for what it is — *"not 'the source moved on,' it is `5.5-property.70d9ae5.cs` correctly belonging to the **second** section"* — which is the fact ⚠️-13 said a reader running only the instruction was left to derive unstated. The whole-file trap paragraph survives and its *"permanent 4-line mismatch"* re-reproduced exactly: `36d35` (1 `<`) and `40a40,42` (3 `>`), **4 lines**. |
| **⚠️-7, clause 1** — *"one excerpted test method"* where this changeset makes it two | **✅ closed** | `:12` now reads *"content\demo\\** includes **excerpted test methods (not compilation units)** that must never compile"*. `find Picea.Abies.Presentation/content -name '*.cs'` → exactly **two**: `stops/5.5-property.cs` and `stops/5.5-property.70d9ae5.cs`. Plural is now correct, and *"not compilation units"* is verified true: `-getItem:Compile` on the Presentation project → **1** item total, **0** under `content/`. This was the half the changeset **introduced**, i.e. the regression half, and it is fixed rather than registered. |
| **⚠️-7, clause 2** — *"the files stay visible as None items"* | **↓ downgraded to 💡-18, open** | Re-measured, unchanged: `-getItem:None` → **43** items under `content/` out of **45** files; `comm` against the file list puts the two missing ones at exactly `content/demo/stops/5.5-property.cs` and `content/demo/stops/5.5-property.70d9ae5.cs`. Still true, still inaccurate. See the downgrade below — it is a call, and it is mine. |
| **💡-14** — the `:210` citation, my error, transcribed faithfully | **✅ closed** | `:1543` now reads `pr-validation.yml:211`, and `:1930` already did. Re-derived rather than quoted: `grep -n 'hardLimit' .github/workflows/pr-validation.yml` → **211** `const hardLimit = 1500;`; `sed -n '208,213p'` confirms **210** is the `// Soft limit (warning) and hard limit (failure)` comment. The plan's two citations now agree with each other and with the file. |

## The ⚠️-7 downgrade, stated plainly

Round 1 raised ⚠️-7 as a **compound**: a count this changeset made wrong, *and* a second
clause that was already wrong. Round 2 carried it at ⚠️ unchanged. This round closes the
first half. What is left is verbatim what stood at HEAD and at every commit before it.

I am grading the residue **💡, down from ⚠️**, and the reasons are these:

1. **The half that carried the ⚠️ is gone.** The Merge Criterion's own dividing line is
   introduced-vs-inherited: *"a **regression** introduced by the changeset"* blocks
   unconditionally; *"pre-existing gaps the changeset did not introduce"* do not, once
   registered. The count was the introduced half. What remains is inherited, and the
   changeset no longer touches it in any way that makes it more wrong — the sentence has been
   edited and is now correct about the thing this changeset changed.
2. **The Boy Scout escalation was load-bearing only while the same sentence carried an
   introduced error.** *"You are already editing this sentence, fix the whole sentence"* is a
   real argument; it stops being one once the sentence has been edited correctly and the
   remaining clause is about MSBuild item groups rather than about anything in this diff.
3. **Consistency with my own grades in this same review.** 💡-8 (a plan snippet dropping a
   sentence from a landed comment) and 💡-16 (one indicative clause in an otherwise
   subjunctive row) are structurally the same defect — an artifact's prose saying slightly
   more than is true, with no behavioural consequence. Standing alone, clause 2 is that.
4. **No consequence, verified.** The outcome the comment exists to explain holds:
   `dotnet build Picea.Abies.Presentation` → **0 warnings, 0 errors**; `dotnet build
   Picea.Abies.Tests` → **0 warnings, 0 errors**; **0** `Compile` items under `content/`.
   The two `.cs` fragments are invisible in the IDE tree — that is the whole cost.
5. **Not cap-forcing.** The fix — one line of `<None Include="content\**\*.cs" />`, or five
   words of rewording — costs exactly the same after this merge as before it. At the round
   cap that is the question that decides whether a defect earns another round, and the answer
   is no.

**What I am not doing:** I am not claiming clause 2 is true. It is false of exactly the two
files the first clause names, I measured it twice, and it stays open as 💡-18 for whoever next
touches that csproj. Re-grading is not a way to make a finding disappear, and if this pattern
recurs — a compound finding whose introduced half gets fixed and whose inherited half is
re-graded each round — that is laundering by another name and should be called out. It has
happened once, here, with the reasoning above on the record.

**What I am also not doing:** I am not treating round 2's parenthetical (*"plus the second
clause, if the Boy Scout call is taken"*) as the justification. That parenthetical was in the
verdict prose but **not** in the round-2 drop, whose `high` entry states both clauses flatly.
The author could reasonably have read either. The downgrade rests on the five reasons above,
not on which of my own two documents the author happened to read.

## Findings — round 3

### 💡 Nitpicks

- **💡-18 — carried, downgraded, open.** `Picea.Abies.Presentation.csproj:12`, second clause.
  See above. Owner on a future changeset: `csharp-dev`.
- **💡-19 — the new interpretation sentence is not exhaustive.** `README.md:85-86`: *"A
  non-empty diff here means one of the 39 first-section copies no longer matches its
  `07607bf` source."* Probed in a scratch copy of `content/demo/`, **both controls**:

  | tree | scoped-check diff | `sha256sum -c` |
  |---|---|---|
  | untouched copy (positive control) | **empty, exit 0** | exit 0 |
  | copy + one hypothetical future `stops/9.9-future-fragment.md` | `40d39 < … stops/9.9-future-fragment.md` | **exit 0** |

  A file *added* to `full/` or `stops/` also yields a non-empty diff with no first-section
  copy altered — which is precisely the shape that produced ⚠️-13 in the first place, and
  `sha256sum -c` is silent about it because a manifest cannot detect an addition. Two reasons
  this is 💡 and not a reopening of ⚠️-13's class: the blocking property is gone (the printed
  check is **empty on the tree as it stands**, which is what ⚠️-13 was about), and the case
  requires a *future* third fragment, which by the bundle's own convention arrives with its
  own manifest section and README row. Strictly, the sentence is also loose in a second way —
  the manifest records the **copy's** hash at generation time, not a comparison against the
  `07607bf` source, so a diff means the copy's bytes moved, not that the copy diverged from
  the source. Both are one clause's worth of tightening, whenever that file is next open.
- **💡-20 — the printed command is bash-only.** `<(head -n 39 SHA256SUMS)` is process
  substitution; it fails in `sh`/dash. The fence is marked ```` ```bash ````, so it is
  accurate as written, and I ran it in bash — noted only because the surrounding document is
  scrupulous about naming its assumptions, in the same spirit as 💡-11's `sha256sum`-is-GNU
  point.
- **💡-21 — reflow residue.** `README.md:89` ends the whole-file-trap sentence at *"reports a
  permanent 4-line mismatch on an"* — a short line left by editing around the inserted block.
  Cosmetic; renders identically.
- **💡-8, 💡-10, 💡-11, 💡-15, 💡-16, 💡-17** — unchanged from round 2 and re-confirmed as still
  standing. None was in scope for this round's fix brief and none needs registration under the
  grade ruling below. **💡-12 is now the operative one:** `.squad/.last-review-verdict` reads
  `NEEDS-CHANGES` / `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b` at the moment of writing; the
  drop below supersedes it for this same sha, and any commit moves HEAD off it.

### ✅ What's Good (round 3)

- **Three fixes, three files, three lines' worth of change, nothing else.** One hunk in
  `README.md`, one line in the csproj, one line in the plan — every one of them inside the
  region the fix brief named. No opportunistic edits, no drive-by rewording, and the four
  pinned artifacts (`SHA256SUMS`, both `.cs` fragments, the test project) untouched, which is
  what kept `sha256sum -c` at 40/40 and both builds at zero warnings.
- **⚠️-13 was answered by making the printed procedure actually run clean, not by explaining
  the residue away.** The finding said what must become true — *a reader running the printed
  first-section check on an intact tree is told what the one guaranteed line is* — and left
  the remedy open, naming an added clause, a `grep -v`, or dropping the diff path as
  candidates. The author picked a fourth: exclude the file by name and scope the comparison
  with `head -n 39`, so the reader is told nothing because there is nothing left to tell. That
  is a better answer than any of the three I listed, and it is the second time in this review
  that leaving the remedy open beat prescribing one.
- **The exclusion is justified in the text, not just applied.** *"The exclusion is required,
  not cosmetic"* followed by the arithmetic (40 vs 39) and the reason the extra line is not
  the documented failure mode. A reader who deletes the `! -name` to "simplify" the command
  has been told, in advance, exactly what they will see.
- **💡-14 was my error and it was corrected without argument**, and the correction re-derives
  rather than trusts: `:1543` and `:1930` now agree with each other and with `grep -n`.
- **The csproj fix is minimal and the item is untouched.** *"excerpted test methods (not
  compilation units)"* is two words changed. Declining to add `<None Include>` in a
  documentation changeset is a defensible scope call, not an omission — a csproj item change
  has pack/publish blast radius that a comment does not, and this changeset ships no
  executable change at all.
- **`sha256sum -c SHA256SUMS`** → exit 0, **40/40 OK**, no warnings. **`dotnet build
  Picea.Abies.Presentation`** and **`Picea.Abies.Tests`** → 0 warnings, 0 errors each.

## The Merge Criterion — where this changeset lands

- **(a) stated properties green — yes.** The property this bundle states for itself is its own
  authoritative check, and the README names it in those terms: `sha256sum -c SHA256SUMS` from
  `content/demo/` → **exit 0, 40 of 40 OK, no warnings**. Its newly printed first-section
  check → **empty diff, exit 0**, run verbatim. Builds clean on both touched projects.
- **(b) every non-regression finding registered — yes, vacuously, and that is the honest
  form.** `.claude/enforcement/refutations.md` is **byte-unchanged** against HEAD, so nothing
  was registered this round. Round 2 named the grades (b) binds on — **🔴 and ⚠️; 💡 items do
  not need registration** — and after this round **there is no open 🔴 or ⚠️ left to
  register**: ⚠️-13 is closed by fix, ⚠️-7's introduced half is closed by fix, and its
  inherited half is graded 💡 on the record above. Route 1 (*fix*) was taken and it closes (b)
  the same way route 2 (*register*) would have. The ledger append that round 2 offered as an
  alternative is no longer owed.
- **(c) nothing published above its computed level — n/a.** No level claim in this changeset.

**The split.** This is round 3, past the cap of two, so the disposition is the split rather
than a fourth fix round — and **the split is degenerate in the good direction: the whole
changeset passes (a)–(c) and all of it ships.** Nothing is carved out, nothing becomes the
next changeset, and the ledger stays as it is. **I am not requesting another round**, and the
five 💡 items carried forward are advisory: they are recorded here so they are not lost, not
as conditions on this merge.

**Where to stop, and this is the last time it can be said before it matters.** The verdict and
the drop are pinned to `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`. The drop is written to
`.squad/decisions/inbox/`, the `scribe-decision-merger` hook consumes it on `SubagentStop` and
writes `.squad/.last-review-verdict` from its `commit:` field — I do not write that file and
`enforce-reviewer-readonly.sh` denies it to me. **Committing moves HEAD**, at which point the
cache no longer matches and `enforce-review-verdict.sh` blocks again, including on a commit
carrying nothing but this verdict.

**This verdict invalidates its own staging snapshot, and saying so is part of the verdict.**
The 19 changeset paths — the three fixes, `08`, this file, the round-1 and round-2 archived
drops, the log rows — were all staged and `git diff` was empty at the moment the fixes were
graded. Writing this round-3 section, the decision drop, and the reviewer notebook entry then
added six paths that are **not** staged:

```
MM .claude/agent-memory/reviewer-reconcile/MEMORY.md
MM .claude/docs/decisions.md                                   (scribe-merged round-3 entry)
AM .squad/design/undo-redo-followups/09-review-verdict.md      (this section)
MM .squad/log/2026-09-08-session.md
?? .claude/agent-memory/reviewer-reconcile/grade-a-compound-finding-by-which-half-survives.md
?? .squad/decisions/archive/2026-09/2026-09-08T06-02-33-review-eace0087-round3.md
```

`.squad/.last-review-verdict` now reads `PASS` / `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`,
written by `scribe-decision-merger.sh` from the drop's `commit:` field. It is
`.gitignore:473` and untracked, so it is not one of the six and never enters a commit.

**`git add -A` those six, then commit once.** Do not re-run the review after staging them:
the PASS is pinned to this sha, staging changes no file content the verdict graded, and a
fourth round is not available under the cap.

## Metrics — round 3

- **Round-3 delta reviewed:** 3 content files, measured against the recovered round-2 index
  blobs (`7bb9d343`, `c217e360`) and against HEAD for the csproj. `README.md` **+12/−6**, one
  hunk; `04-realist-plan.md` **+1/−1**, one hunk; `.csproj` **+1/−1**, one hunk. Every changed
  line read.
- **Independent verifications, none quoted from rounds 1–2:** the scoped check run **verbatim
  as printed** (empty, exit 0); a 2-arm scratch-copy probe of that check (untouched → empty;
  +1 file → `40d39`); the whole-file trap re-reproduced at **4** lines; `sha256sum -c` → 40/40,
  exit 0; the `head -n 39` boundary confirmed by `cat -A` on lines 38–42; file counts 19 / 21 /
  40 and manifest counts 39 / 40; `grep -n hardLimit` → **211** with `sed -n '208,213p'`
  confirming 210 is the comment; `-getItem:Compile` → 1 total / 0 under `content/`;
  `-getItem:None` → 43 under `content/` with `comm` naming the 2 absentees; 2 `dotnet build`
  runs; 2 blob-identification greps; `git check-ignore` and `git ls-files` on the nested
  worktree; the archived drops' `commit:` fields.
- **Test coverage of new code:** n/a — no executable change in this changeset at all. The one
  new `.cs` file is an excerpt, not a compilation unit (0 `Compile` items under `content/`,
  verified). No regression test is owed.
- **Complexity:** none added.
- **Dimensions run:** 11/11. Documentation produced the only findings (all 💡); Correctness,
  Readability, Consistency, Design, Testability, Security, Performance, Observability and Boy
  Scout produced none new. Definition of Done: *"all existing docs are still in sync after the
  change"* now **passes** — `README.md` (🔴-2, ⚠️-13 closed) and `Picea.Abies.Presentation.csproj`
  (⚠️-7 clause 1 closed; clause 2 is inherited, not a sync failure of this change).
- **Cumulative:** 3 rounds, 4 🔴 raised and all 4 closed, 3 ⚠️ raised — 2 closed by fix, 1
  closed-in-half and downgraded — 0 registered, 0 outstanding at ⚠️ or above.
- **Verdict mapping:** ✅ Approved → `PASS`.

**Commit-boundary confirmation, 2026-09-08T06:06:04Z — the round-3 PASS carries to `0d6b208ca012e9cbf6710fba64ffd44f62cc6c60`** (single parent `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`, 21 files, `+2266/−18`): `git diff --name-status eace008..0d6b208` is exactly the 7 graded paths + 2 review artifacts (`08`, `09`) + 6 reviewer-reconcile notebook paths + 3 merged drops (`archive/2026-09/…05-31-09`, `…05-48-17`, `…06-02-33`, all `agent: reviewer-reconcile`, all `commit: eace0087…`) + 3 hook-written rows (`decisions.md` a zero-deletion append at `@@ -3823,3 +3823,296 @@` holding only those same three scribe-merged entries, `2026-09-08-session.md`, `pass-cost.md`) — 7+2+6+3+3 = 21, nothing else; each graded path's committed blob equals the blob graded above (`README.md` `f72871e6`, `04-realist-plan.md` `a590177f`, `.csproj` `624ca60a` — the round-3 "current" column verbatim — plus `SHA256SUMS` `a036722b`, `stops/5.5-property.70d9ae5.cs` `6775d948`, `realist/MEMORY.md` `81c0d186`, `realist/an-exception-named-by-element-has-four-sites.md` `db25b0f9`), and each equals `git hash-object` on the working tree; `sha256sum -c SHA256SUMS` re-run inside a `git archive 0d6b208` export → **exit 0, 40/40 OK, no warnings**, with the manifest's 40 entries covering every file under `content/demo/` except the 5 documented non-payload paths (`README.md`, `SHA256SUMS`, `graphics/.gitkeep`, `timing/hooks-fired.log`, `timing/pass-cost.md`) and zero manifest entries lacking a file; the working tree holds **only hook-written state** — `git status --porcelain --untracked-files=all` was empty at the start of this pass and now shows one path, `.squad/log/2026-09-08-session.md` `+3/−0`, three `session-logger.sh` rows, with no untracked non-ignored file anywhere and no ignored file under `content/`. **`.squad/.last-review-verdict` still reads `PASS` / `eace0087…` and therefore does not match HEAD — the drop written alongside this line, pinned to `0d6b208…`, is what unblocks `enforce-review-verdict.sh` for the merge. Stop there: this line and that drop are themselves uncommitted changes, and committing them would move HEAD off `0d6b208…` and re-block the gate on a commit carrying nothing but a confirmation.**
