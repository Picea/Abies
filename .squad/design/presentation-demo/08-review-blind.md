# 👁️ Blind Review — `presentation-demo` (working tree on `docs/0-presentation-demo`)

**Reviewed:** working tree vs `07607bf71152ef8d224352e2d2a737d8312dcb2d` (= `origin/main`, = HEAD).
43 new untracked files under `Picea.Abies.Presentation/content/demo/**` (10,144 lines, 844 KB)
plus a 7-line `ItemGroup` in `Picea.Abies.Presentation/Picea.Abies.Presentation.csproj`.
`.squad/log/2026-09-07-session.md` is also dirty (hook-written, out of scope, not reviewed).
Nothing is staged — `git diff --cached` is empty; the change is entirely in the worktree.

**History channel:** not needed — the changed tree is 43 untracked files plus one two-hunk
csproj edit against a HEAD equal to `origin/main`. No churn or revert question arose.

**Narrative reaching this context:** none. The orchestrator's prompt gave me the branch,
the base commit, the file scope and the slug, and no statement of intent, motivation or
design rationale. I did not read `.squad/design/`, the PR body, or any issue.

**One blindness note, stated up front because it shapes the whole review:** the change
under review is *a set of copies of `.squad/design/undo-redo/**`* — the exact directory my
charter denies me by hook, and which I also did not reach via `Bash`. The central factual
claim of this change ("everything here is a copy", "unedited copies") is therefore
**structurally unverifiable by the blind reviewer**. I verified every citation whose source
lies outside `.squad/design/`, and every citation that is internal to the demo tree itself;
I could verify nothing that crosses the boundary. See § "What I could not determine".

---

## What this change does

It adds a self-contained, citation-indexed evidence bundle for a conference talk to the
`Picea.Abies.Presentation` project, and adds a build guard so that bundle is inert.

**Old behaviour:** `Picea.Abies.Presentation` was a small `Microsoft.NET.Sdk.WebAssembly`
slide app — `Program.cs`, `wwwroot/`, and two loose `.md` files at the project root. The
undo/redo design record existed only in its live locations: `.squad/design/undo-redo/`,
`.squad/design/undo-redo-design-record/`, `.squad/decisions/archive/2026-09/`,
`.claude/enforcement/refutations.md`, `.squad/log/pass-cost.md`.

**New behaviour:** a new `content/demo/` tree pins a snapshot of that record at one commit,
in four parts:

- `full/` — 11 whole-file copies (the nine numbered `undo-redo` design artifacts,
  `room-security.md`, `refutations.md`) plus 8 archived decision drops in
  `full/decision-drops/`.
- `stops/` — 21 excerpt fragments, one per talk beat, numbered `5.1`–`6`. Several are
  composites stitched from two to five sources.
- `timing/` — `pass-cost.md` (the 42 `undo-redo` rows of the cost log) and
  `hooks-fired.log` (an **authored**, cited index of hook activity — explicitly not a
  verbatim source, because no consolidated hook log exists).
- `graphics/` — empty, `.gitkeep` only.
- `README.md` — the index. One table row per file, each naming its source path and, for
  fragments, its line range and a rationale for the span chosen.

The csproj hunk adds four `Remove` items (`Compile`, `Content`, `None`, `EmbeddedResource`)
for `content\**`, with a comment stating the tree "must never be compiled, embedded, or
published". This is **load-bearing, not cosmetic**: `stops/5.5-property.cs` is an
indentation-preserving method excerpt with no namespace, no type declaration, and
unbalanced braces (it ends mid-method after the final `foreach`). Without the
`<Compile Remove>` the project would not compile.

## Why it might be needed

Inferable from the code with reasonable confidence, and the README states it directly: the
talk narrates the squad's undo/redo design pass, and the presenter wants every slide to be
backed by a quotable, checkable artifact rather than a paraphrase. Pinning to one commit
and recording line ranges is what makes an audience member able to reproduce a slide.

The design intent visible in the artifact is unusually explicit and, on its own terms,
disciplined. Three requested fragments (`5.2-line-abstract.txt`, `5.2-refusal.log`,
`5.6-refusal.log`) were **not created** because the claimed text does not exist at that
commit, and the README says so with what was searched and what the nearest genuine quote
is. Excerpts that were cut mid-thought are flagged as such. A decision drop belonging to a
different PR is explicitly excluded with a reason. That is the behaviour of someone trying
not to fabricate, and it is the strongest thing about this change.

What I **cannot** infer from the code: why the copies need to live in the repository at all,
given that every one of their sources is already in this repository at the same commit and
reachable with `git show 07607bf:<path>`. The README says "Nothing under `.squad/` was
moved, edited, or deleted" — so this is pure duplication, and the reason for it (offline
presenting? a slide build step that reads `content/`? avoiding `.squad/` on a projector?)
is not stated anywhere in the change.

## Is this the right approach?

**The csproj guard: correct, and I verified it end to end.** I checked three things rather
than reasoning about MSBuild glob ordering:

- `dotnet build Picea.Abies.Presentation` → succeeds, 0 warnings, 0 errors.
- `dotnet msbuild -getItem:Compile,Content,None,EmbeddedResource` → **zero** items under
  `content/demo`.
- `dotnet msbuild -t:ComputeFilesToPublish -getItem:ResolvedFileToPublish` → **zero**
  entries matching `content` or `demo`. Nothing leaks into the published static web assets.

The Windows-style `content\**` separators normalise correctly on Linux, so there is no
cross-platform hazard here. The `Remove` items sit in the project body, after the SDK's
default globs are established in `Sdk.props`, which is the documented placement.

**But the guard exists only because of the file extensions chosen.** That is the design
question worth asking. Every file in `content/demo/` is inert *documentation*; only two of
them carry a code extension, and both are excerpts that are not valid programs in their own
right. `5.5-property.cs` is not compilable C#, and `5.2-hook-line.sh` is not shell at
all — it is **Python**, lifted out of a heredoc inside `enforce-track-blindness.sh`. Naming
these `.cs.txt`/`.py.txt`, or fencing them inside `.md`, or putting the whole tree under
`docs/presentation/` outside any project directory, would have required **no csproj change
at all** and would have made the "must never be compiled" comment unnecessary rather than
load-bearing. The current shape trades a zero-cost naming decision for a permanent build
invariant that a future edit can silently break.

**A duplication concern with no guard.** `full/refutations.md` is a copy of a *living*
ledger (`.claude/enforcement/refutations.md`, appended to as findings accrue) and the
`full/*` design artifacts are copies of files the design process **overwrites in place** —
the README itself explains this twice (the warden report and critic passes 1-3 were lost to
overwrite). So the copies will diverge from their sources by design, which is the point;
but nothing in the change makes the divergence *detectable*. There is no checksum manifest,
no CI check, no `.editorconfig`/`.gitattributes` marking the tree read-only. The README's
"unedited copies" claim degrades from verified to asserted the moment anyone edits a file
in `full/`, with nothing to catch it. A `SHA256SUMS` file generated at snapshot time would
make the change's own central claim cheaply checkable forever, including by a reviewer who
is denied `.squad/design/`.

**`<None Remove="content\**" />` has a DX cost.** `None` items are what populate the
Solution Explorer tree in Visual Studio and Rider. Removing them makes the entire demo tree
**invisible in the IDE** to the person who has to present from it. Only `<Compile Remove>`
is actually necessary for correctness; `Content`/`EmbeddedResource` are cheap insurance;
`None` is the one that costs something. Worth a deliberate decision rather than a reflex.

## Problems

### 1. Stop 5.1 ships three mutually contradictory statements about which scope the gate cleared

This is the most serious content problem I found, and it lands on the slide whose stated
subject (README, `5.1-warden-report.md` row) is *"the warden earning its place."*

- `full/00-scope.md` is **254 lines** (README: "as amended and landed — 254 lines"; I
  counted it).
- `stops/5.1-warden-report.md:3` reads `**Checked:** .squad/design/undo-redo/00-scope.md
  (153 lines)` — and the README says this fragment **is** the re-run, i.e. the gate pass
  that cleared the landed scope.
- `timing/hooks-fired.log` asserts, in its own words, `scope-warden.sh (mechanical scan),
  run 2 — ... at the amended (254-line) scope: verdict CLEAN`.

The warden report's two internal line references then split the difference, which is what
makes this concrete rather than a quibble:

- `"Subscriptions and timers" (lines 28–50)` — **correct** for the 254-line file
  (`grep -n` puts the heading at exactly line 28).
- `It leaves "at what level undo operates" explicitly open (line 99–101)` — **wrong** for
  the 254-line file. That sentence is at **line 221**
  (`full/00-scope.md:221`); lines 99–101 contain unrelated prose about the durability
  exclusion ("against holding it elsewhere. / Two consequences, so the exclusion is not
  read wider or narrower than it is:").

So on one slide the audience is shown a gate report that claims to have checked a 153-line
file, cites one location that only makes sense in the 254-line file and another that makes
sense in neither, next to the 254-line file itself, next to an authored log asserting the
two match. At least one of these four things is false. `hooks-fired.log` is *authored by
this change* — it is the one file in the bundle that is not a copy — so its "at the amended
(254-line) scope" is an original claim of this change that the change's own bundled
evidence contradicts. The README, which flags far smaller defects with care, does not
mention this at all.

*How I verified:* `wc -l full/00-scope.md` → 254; `grep -n "Subscriptions and timers"` → 28;
`grep -n "at what level undo operates"` → 221; `sed -n '99,101p'` → unrelated text.
I could not determine whether the "153 lines" is faithfully copied from the source
artifact or introduced here — see § "What I could not determine".

### 2. `5.1-warden-report.md` is truncated mid-sentence, and the truncation removes the point

The R-21 half of this composite is `refutations.md:546-570` and ends on:

> `chose to leave both the ADR and the hook as they are, so the experiment is`

The source continues at `:571-572` with `not disturbed mid-run. Net: Track A's blindness to
ADRs is / instruction-level for the undo-redo pass`. The excerpt therefore stops one line
before the sentence that states the finding's actual conclusion — the "instruction-level,
not invariant-level" classification that is presumably why the fragment is on the slide.

What makes this a defect rather than a taste call is that the README **does** flag
mid-sentence truncation elsewhere (the `5.2-track-b-summary.md` row: "also truncated
mid-sentence ... not an edit made here") and does **not** flag it here. A reader trusting
the index will assume this fragment is complete.

*How I verified:* `tail -4 stops/5.1-warden-report.md` against
`sed -n '570,571p' .claude/enforcement/refutations.md`.

### 3. Two of the three stop-5.6 fragments cite sources the bundle does not contain

`5.6-review-headers.md` and `5.6-round-2-escalation.md` are sourced from
`.squad/design/undo-redo-design-record/08-review-blind.md` and `09-review-verdict.md`.
Neither file is copied into `full/`, and the slug `undo-redo-design-record` is never
mentioned in the README's `full/` table or its exclusions note — unlike the excluded
decision drop, which gets a full sentence explaining why it is out.

The bundle is therefore self-contained for stops 5.1–5.5 and **not** self-contained for
5.6. If the tree exists so a slide can be checked in context, the review stop — the one
about the review chain catching things — is the one stop that cannot be. The asymmetry is
undocumented, which is what makes it a defect: the README is otherwise scrupulous about
saying what it left out and why.

*How I verified:* `ls full/` contains no `08-`/`09-` file; `grep` of README for
`undo-redo-design-record` finds it only in the two `stops/` rows.

### 4. The PR will hard-fail `check-pr-size`, and the file extensions are why

`.github/workflows/pr-validation.yml` (`check-pr-size`, lines 160-236) sets
`hardLimit = 1500` on `additions + deletions` and calls `core.setFailed` above it, unless
**every** changed file matches `isMaintenancePath` — `.gitkeep`, `docs/`,
`.github/instructions/`, `README`/`CHANGELOG`/`CONTRIBUTING`/`SECURITY`/`LICENSE`, or
`.md`.

This change is ~10,151 lines and includes six files that fail that predicate:
`5.2-frontmatter-track-a.yaml`, `5.2-frontmatter-track-b.yaml`, `5.2-hook-line.sh`,
`5.2-line-charter.txt`, `5.5-property.cs`, `timing/hooks-fired.log` — plus the `.csproj`.
`maintenanceOnly` is `false`, so the job fails hard.

The same six extensions also flip `detect-changes`'s `docs_only` to `false`
(`NON_DOCS` regex, line 76), so a documentation-only branch runs the whole expensive CI
matrix — lint, CodeQL, e2e — for nothing.

Two things about this are worth separating. First, the `.gitkeep` in `graphics/` is
*already* in the maintenance regex, which suggests these gates were partly considered; the
other extensions were not. Second, and more importantly: **even if every fragment were
renamed to `.md`, the `.csproj` change alone keeps `maintenanceOnly` false**, so at ~10 k
lines this PR fails the size gate no matter what. It needs either an explicit override, an
amendment to `isMaintenancePath`, or the tree moved somewhere that requires no csproj edit
at all (which would also dissolve problem 4 and the guard-fragility concern together).

*How I verified:* read `.github/workflows/pr-validation.yml:160-236` and `:73-84`; listed
the 43 files and applied the predicate by hand.

### 5. Composite fragments carry no in-file provenance

Five fragments are stitched from multiple sources (`5.1-warden-report.md`,
`5.4-critic-verdicts.md`, `5.6-review-headers.md`, `5.6-round-2-escalation.md`). None of
them carries a label, a source comment, or in three cases even a separator saying where one
source ends and the next begins.

`5.4-critic-verdicts.md` is the clearest case: four verdict statements from four different
files, run together with blank lines, ending in two bare `## Verdict` markdown headings
whose text ("APPROVED WITH MITIGATIONS", "CONFIRMED WITH A BOUNDED LIST") gives no clue
they come from different passes of the same document. `5.6-review-headers.md` concatenates
two document headers with nothing between them.

Every one of these is correct — I checked all four against their sources and they are
exact — but the correctness lives *only* in the README table. A fragment pasted into a
slide, or opened on its own, is unattributable and easy to mis-caption. An HTML comment at
the top of each composite naming its spans would cost nothing and would keep the fragment
honest when it is separated from its index, which is exactly what happens to slide content.

### 6. README off-by-one on the R-25 citation

The README cites `full/refutations.md:722` twice, for the R-25 caveat that qualifies both
`5.2-track-a-summary.md` and `5.2-track-b-summary.md`. `### R-25` is at line **723**; 722
is blank. The neighbouring R-24 citation (`:707`) is exactly right.

Trivial in isolation. It matters only because line-precise citation is this artifact's
entire premise, and because R-25 is the caveat doing the load-bearing work on two
fragments — it is the reference an audience member is most likely to follow.

*How I verified:* `grep -n "^### R-2[1-5]" full/refutations.md` → R-24 at 707, R-25 at 723.

### 7. `5.2-hook-line.sh` is Python in a `.sh` file, and its README rationale overclaims

Two small things on one fragment.

The file extension is `.sh`; the content is Python (the body of a heredoc inside
`enforce-track-blindness.sh`). Any syntax highlighter on a slide will render it as shell
and get it wrong.

The README's "why this excerpt" says the span contains *"the 'unreadable → refuse for
everyone' fallback"*. The span (`:316-330`) contains `print("UNREADABLE"); sys.exit(0)` —
the **signal**, not the fallback. The behaviour that makes it a refusal-for-everyone is at
`enforce-track-blindness.sh:422` (`if [ "$reason" = "UNREADABLE" ]; then`), outside the
excerpt, and the comment block that explains the intent is at `:308-315`, immediately above
it and also outside. As shown, the excerpt supports three of the four claims made for it.

*How I verified:* `grep -n UNREADABLE .claude/hooks/enforce-track-blindness.sh` → 327 and
422; read `:295-330`.

### 8. `hooks-fired.log`'s extension and status

`timing/hooks-fired.log` is markdown with `##` headings, named `.log`. It is also the one
file in the bundle that is **authored rather than copied** — the README says so, and its
own header repeats it. Given that it is the file most likely to be quoted as raw machine
evidence on a slide (a `.log` extension invites exactly that reading), the `.md` extension
and a more explicit filename would reduce the chance of it being presented as something it
is not. This compounds problem 1, where its authored claim is the one that conflicts.

---

### What I checked and found clean

Recording these so `reviewer-reconcile` does not re-spend the effort.

- **Every citation whose source lies outside `.squad/design/` is byte-exact.**
  `5.2-frontmatter-track-a.yaml` ← `dreamer-first-principles.md:1-9`;
  `5.2-frontmatter-track-b.yaml` ← `dreamer-informed.md:1-11`;
  `5.2-line-charter.txt` ← `dreamer-first-principles.md:40`;
  `5.2-method-steps.md` ← `:57-63`;
  `5.2-hook-line.sh` ← `enforce-track-blindness.sh:316-330`;
  `5.2-track-a-summary.md` ← `.squad/log/2026-09-06-session.md:424`;
  `5.2-track-b-summary.md` ← `:422`. All exact, including the documented mid-sentence cut
  on the last one.
- **`full/refutations.md` is byte-identical to `.claude/enforcement/refutations.md`**
  (`diff` clean, both 783 lines). One of only two copy claims I could test; it holds.
- **`timing/pass-cost.md` filtering is exactly right.** The source has 42 rows with slug
  exactly `undo-redo`, 5 `chore-0-squad-flow-v2-1`, 1 `undo-redo-design-record`. The copy
  has the 42, byte-identical, and correctly excludes the other 6. The README's account of
  the exclusion matches.
- **Every intra-bundle fragment↔`full/` citation is byte-exact:** `5.1-degrees-of-freedom`,
  `5.1-invariants-four`, `5.5-invariant` (all ← `full/00-scope.md`);
  `5.3-convergence-verdict`, `5.3-invariant-holes` (← `full/03-convergence.md`);
  `5.5-property.cs` (← `full/06-spec.md:1034-1072`); `5.4-critic-verdicts` (five sources);
  `5.4-critic-finding-B9`; `6-register-why-not-caught`. The single diff —
  `5.5-inv-coverage.md` — is the deliberate cut the README documents ("cut at '...already
  states.'"), and it cuts exactly where it says.
- **`hooks-fired.log`'s evidence claims check out** where checkable:
  `.squad/log/lexicon-hits.md` is 0 bytes; `.squad/log/gate-shadow.md` is a 19-line HTML
  comment with no `SHADOW-ALLOW` rows (the one grep hit is inside the header prose, so the
  file's "header comment only, no rows" claim is accurate);
  `validate-phase-artifact.sh` has 273 lines so `:192-220` exists;
  `.squad/log/2026-09-06-session.md:391` does carry the partial quote of the overwritten
  warden report, as claimed.
- **README index integrity is complete.** 21 files on disk, 24 rows, and the 3 extra rows
  are exactly the three deliberately-not-created fragments. No orphan file, no orphan row.
  Internal citations into `full/` verified: `04-realist-plan.md:3` does say "Revision 4";
  `05-critic.md:427` is the appended navigation confirmation.
- **Hygiene:** no CRLF anywhere; every non-empty file ends with a newline; no secrets,
  credentials, API keys or tokens; no absolute or home-directory paths. `room-security.md`
  discusses password-bearing message types but discloses nothing not already tracked in
  this repository at the same commit.

---

## What I could not determine from the code alone

1. **Whether the `full/` copies are actually verbatim.** This is the change's central claim
   and I am structurally the wrong reviewer for it: `.squad/design/` is denied to me by
   hook, and I did not route around it via `Bash`. I verified the two copies whose sources
   live elsewhere (`refutations.md` — identical; `pass-cost.md` — identical after the
   documented filter). The other **nine design artifacts and eight decision drops are
   unverified**. `reviewer-reconcile` can read `.squad/design/` and should run a plain
   `diff` of `full/00-knowledge.md`, `00-scope.md`, `01-track-a.md`, `02-track-b.md`,
   `03-convergence.md`, `04-realist-plan.md`, `05-critic.md`, `06-spec.md`, `07-handoff.md`,
   `room-security.md` against `.squad/design/undo-redo/`, and `full/decision-drops/*`
   against `.squad/decisions/archive/2026-09/`. If the answer is "identical", problem 1
   resolves to a faithful copy of a flawed source. If not, it is an error introduced here.
   Either way the verdict differs, and I cannot tell which.
2. **Whether the "153 lines" in `5.1-warden-report.md` is copied or introduced.** Same
   boundary. This is the single highest-value check in the list.
3. **Why the copies exist at all**, given that every source is in this repo at
   `07607bf`. Is there a slide build step that reads `content/`? Is the talk given
   offline? Is `.squad/` considered unshowable on a projector? Nothing in the change says,
   and the answer determines whether the duplication (and its divergence risk) is the right
   trade or an avoidable one.
4. **Whether `content/demo/` is permanent or a staging area for one talk.** The `.gitkeep`
   in `graphics/` and the csproj guard both imply permanence; the commit-pinned snapshot
   implies a one-shot. If it is one-shot, most of problems 4 and 5 stop mattering; if it is
   permanent, the missing checksum manifest matters more, not less.
5. **Whether the `check-pr-size` hard failure is expected and will be overridden**, or
   whether the file extensions / tree location are still open to change. I can see the gate
   will fail; I cannot see whether that was priced in.
6. **Whether the three not-created fragments and the two 5.6 sources are meant to be
   supplied later**, or are permanently accepted gaps. The README treats the former as a
   closed decision and is silent on the latter.
7. **The slide deck itself is not in this change.** The README refers to "the runbook's own
   instruction for this case" and to numbered "stops", but no runbook, script, or deck is
   present. So I could not check that the 21 fragments cover the stops, that no stop lacks a
   fragment, or that any fragment's README rationale matches what the slide actually claims.
   That mapping is where a fragment most plausibly gets mis-captioned, and it is entirely
   outside what I can see.
8. **`Picea.Abies.Presentation/**` factual claims require `reviewer-reconcile` sign-off**
   per the routing rule in `CLAUDE.md`. Almost every line of this change is a factual claim
   with a citation. I verified the ones I could reach; the sign-off is not mine to give.
