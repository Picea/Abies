# Conference demo — undo/redo design record

Index for `content/demo/`. Every excerpt and copy in this tree is sourced from a
single commit, `07607bf71152ef8d224352e2d2a737d8312dcb2d` on `main` (the merged
undo-redo design record, PR #359 plus its round-2 residue), read with
`git show 07607bf:<path>` so line numbers below refer to that commit, not the
working tree. Nothing under `.squad/` was moved, edited, or deleted to build
this tree — everything here is a copy.

## `full/` — unedited copies

| File | Source (`@ 07607bf`) |
|---|---|
| `00-knowledge.md` | `.squad/design/undo-redo/00-knowledge.md` |
| `00-scope.md` | `.squad/design/undo-redo/00-scope.md` (as amended and landed — 254 lines) |
| `01-track-a.md` | `.squad/design/undo-redo/01-track-a.md` |
| `02-track-b.md` | `.squad/design/undo-redo/02-track-b.md` |
| `03-convergence.md` | `.squad/design/undo-redo/03-convergence.md` |
| `04-realist-plan.md` | `.squad/design/undo-redo/04-realist-plan.md` (revision 4 — confirmed at `:3`) |
| `05-critic.md` | `.squad/design/undo-redo/05-critic.md` (fourth pass, plus the 2026-09-07 navigation confirmation appended at `:428`) |
| `06-spec.md` | `.squad/design/undo-redo/06-spec.md` |
| `07-handoff.md` | `.squad/design/undo-redo/07-handoff.md` |
| `room-security.md` | `.squad/design/undo-redo/room-security.md` |
| `refutations.md` | `.claude/enforcement/refutations.md` (the ledger, in full) |
| `decision-drops/*.md` | `.squad/decisions/archive/2026-09/` — the eight drops for this pass and for PR #359's review (below). Grouped in a subfolder for readability; not a structural change from the source layout. |

**Decision drops included** (all `undo-redo`/PR #359; `2026-09-06T13-50-07-review-9b71b18.md`
is excluded — it is PR #358's commit-boundary confirmation, unrelated to this pass):

- `2026-09-06T15-02-41-undo-redo-adr-008-left-as-is.md`
- `2026-09-06T19-29-55-critic-undo-redo.md`
- `2026-09-06T21-34-31-critic-undo-redo.md`
- `2026-09-07T06-34-07-critic-undo-redo.md`
- `2026-09-07T08-03-52-arch-undo-redo.md`
- `2026-09-07T09-34-55-arch-undo-redo-navigation.md`
- `2026-09-07T10-25-28-review-pr359.md`
- `2026-09-07T10-41-14-review-pr359-round2.md`

## `timing/`

| File | What it is |
|---|---|
| `pass-cost.md` | The 42 rows of `.squad/log/pass-cost.md` whose `slug` column is exactly `undo-redo` (header + rows, verbatim). Excludes the unrelated `chore-0-squad-flow-v2-1` slug and the separate `undo-redo-design-record` slug (PR #359's own review timing). **Caveat, worth saying on stage:** `refutations.md` R-24 (`full/refutations.md:707`) finds this file's own header states units the underlying data does not have — the `wall-clock` column is cumulative-since-session-start, not per-phase duration. Read the deltas between consecutive rows, not the raw values. |
| `hooks-fired.log` | A compiled, cited index of hook activity during the pass — not a single verbatim source, because no consolidated hook-firing log exists in this repository. Every line cites the file it was verified against. Documents what the tracked evidence files (`gate-shadow.md`, `lexicon-hits.md`, both empty for this pass) actually show: zero lexicon hits, zero shadow-allow writes, and no logged blindness-hook refusal. |

## `graphics/`

Empty; SVGs are added by hand. `.gitkeep` present so the directory exists in git.

## `SHA256SUMS` — generated, not an excerpt

**Added 2026-09-07, review finding ⚠️-3.** Checksums of all 39 files under `full/` and
`stops/` (19 + 20), generated with:

```bash
find full stops -type f | LC_ALL=C sort | xargs sha256sum > SHA256SUMS
```

This file is **authored tooling output, not a copy or an excerpt** — it carries no line
range and cites no source path, because its content is the hash, not a quotation of
anything. It exists so the bundle's central claim ("these are unedited copies") stays
cheaply re-checkable after the fact — including by `reviewer-blind`, which is denied
`.squad/design/` by hook and could otherwise never verify `full/` against its sources
directly. It is a **snapshot at generation time**, not a live guarantee: `full/refutations.md`
copies a ledger the design process keeps appending to, and several `full/` artifacts copy
files the design process is documented to overwrite in place (`00-warden.md`, `05-critic.md`'s
own revision history). Re-run the command above and diff against this file to check the tree
is still what it claims to be; a mismatch does not by itself mean the copy was edited — it
may mean the *source* moved on and the copy, correctly, did not follow. Verify with
`sha256sum -c SHA256SUMS` from `content/demo/`.

## `stops/` — fragment index

One row per fragment. "Why" explains why this span and not a longer or shorter one.

| File | Stop | Screen state | Source @ `07607bf` (lines) | Why this excerpt |
|---|---|---|---|---|
| `5.1-degrees-of-freedom.md` | 5.1 | Bulleted list, scope excerpt | `.squad/design/undo-redo/00-scope.md:215-232` | The full "Degrees of freedom" section, verbatim — the pass's five genuinely open questions, in the scope's own words, before any design work happens. |
| `5.1-invariants-four.md` | 5.1 | Invariant catalogue | `.squad/design/undo-redo/00-scope.md:128-168` | INV-1 through INV-4 together (not one) — the contract Gate 1 clears Track A to read, shown at the size it actually is. |
| `5.1-warden-report.composite.md` | 5.1 | Gate report + ledger citation (composite, two sources) | `.squad/design/undo-redo/00-warden.md:1-19` (full file, the re-run) **+** `.claude/enforcement/refutations.md:546-572` (R-21, cut mid-line at "...instruction-level for the `undo-redo` pass") | The original warden report that carried the ADR-008 finding was overwritten in place by the warden's own re-run — CLAUDE.md's documented happy path when a scope goes back and comes forward amended. Nothing at this commit contains both the CLEAN verdict and the finding in one file; this composite is the only way to show them together, and showing the overwrite itself is the point ("the warden earning its place" survives only because the ledger — not the warden — kept a citation of it). **Both halves are faithfully copied and both carry defects inherited from the source, not introduced here — read together, they demonstrate exactly the failure mode the slide is about.** (1) The warden's own header (`:3`) says it checked a **153-line** scope; the shipped `full/00-scope.md` is **254 lines**. The 153-line version was never committed — `git log --all -- .squad/design/undo-redo/00-scope.md` at `07607bf` shows the design directory first appears already at 254 lines — so this citation, and the fragment's own `(line 99–101)` reference (which resolves to unrelated prose about the durability exclusion; the sentence it means to cite, "at what level undo operates," is at **line 221** of the shipped scope), are **permanently unverifiable against anything that was ever committed**. (2) The R-21 half originally ended one line before its own conclusion ("...so the experiment is"), cutting the sentence that names the classification the stop exists to show. **Extended by two lines** (`546-570` → `546-572`, cut mid-line rather than at the next line break) to include "...instruction-level for the `undo-redo` pass" — chosen over a README-only flag because the runbook's own smallest-span rule asks for the smallest span that *still makes the point*, and a flag in this index doesn't fix what the audience reads on the slide; extending two lines does, at negligible cost to "smallest." The excerpt still stops short of the full sentence's closing qualifier (what would make it invariant-level instead) — that clause is not needed to land the point and is left out on the same principle. |
| `5.2-frontmatter-track-a.yaml` | 5.2 | Agent metadata card | `.claude/agents/dreamer-first-principles.md:1-9` | Frontmatter only: no `WebSearch`/`WebFetch`, no `memory:` key. The mechanical half of the blindness, without the prose that explains it. |
| `5.2-frontmatter-track-b.yaml` | 5.2 | Agent metadata card | `.claude/agents/dreamer-informed.md:1-11` | Paired against Track A's — the contrast (full retrieval grant, `memory: project`) is the point; neither card alone makes it. |
| `5.2-method-steps.md` | 5.2 | Numbered method | `.claude/agents/dreamer-first-principles.md:57-63` | The five-step method in full — what "first principles" cashes out to procedurally. Screen-sized on its own. |
| `5.2-line-abstract.txt` | 5.2 | — | **Not found at `07607bf`.** No file in the tree contains the sentence "structurally unable to see each other's work" (checked with `git grep` across `.md`/`.sh`, whole repo, this commit). The closest genuine quotes are `dreamer-convergence.md:14` ("Two tracks explored the problem independently and neither saw the other's work") and the CLAUDE.md roster row ("neither may see the other") — neither is the requested wording. Created no file, per the runbook's own instruction for this case. |
| `5.2-line-charter.txt` | 5.2 | Single quoted line | `.claude/agents/dreamer-first-principles.md:40` | The literal, imperative instruction — "Do not read... even if it already exists" — contrasted against the missing abstract phrasing above. |
| `5.2-hook-line.sh` | 5.2 | Code snippet | `.claude/hooks/enforce-track-blindness.sh:316-330` | **Corrected 2026-09-07 (review finding, blocker 2c):** the span contains the JSON parse, the `agent_type` attribution, the `print("UNREADABLE"); sys.exit(0)` signal an unparseable payload emits, and the `DENY.get(agent)` lookup that follows — the mechanism is keyed on a payload field, not on trust. It does **not** contain the "refuse for everyone" enforcement itself: that behaviour lives at `enforce-track-blindness.sh:422`, and the comment explaining the intent behind the `UNREADABLE` signal sits at `:308-315`, immediately above and also outside this span. Both are too far from `:316-330` to fold in without pulling in unrelated surrounding script. |
| `5.2-track-a-summary.md` | 5.2 | Log line | `.squad/log/2026-09-06-session.md:424` | **Caveat:** `session-logger.sh` fires on `SubagentStop` and logs the *Lead's* outgoing narration at that moment, labelled with whichever agent just stopped — not the subagent's own returned text verbatim. Confirmed independently by ledger entry R-25 (`full/refutations.md:723`): "the log records the orchestrator's intentions, not the agents' results." This is the closest surviving artifact to "Track A's returned summary" and is presented with that caveat rather than omitted. **This line's subject is not Track A's own work — it is the Lead reporting that Track B has landed while Track A has not**: the text reads "Track B has landed with three ranked candidates. I'm holding its content until Track A finishes… Waiting on Track A," i.e. it is timestamped *before* Track A finished. Whose voice the line is in (the Lead's) and what it is about (Track B, and Track A's own incompleteness) are two different facts; a caption reading only "Track A's summary" states the second one wrong. |
| `5.2-track-b-summary.md` | 5.2 | Log line | `.squad/log/2026-09-06-session.md:422` | Same voice-caveat as above (R-25). This particular line is also truncated mid-sentence ("...if it blocks, I'll surface the") in the source file itself — not an edit made here; the session-logger's capture is genuinely cut off at this length. **Its subject is not Track B's findings either** — it is the Lead noting that both tracks are running in parallel and isolated, and describing the pending lexicon check on Track A's artifact. Neither track's own content is in this line; both fragments in this pair are the Lead talking about the *pair*, at the moment each track happened to stop. |
| `5.2-refusal.log` | 5.2 | — | **Not found at `07607bf`.** No blindness-hook refusal line exists in `.squad/log/2026-09-06-session.md`, `2026-09-07-session.md`, `gate-shadow.md` (header only, no rows), or `refutations.md`. `enforce-track-blindness.sh`'s refusal path was never exercised as far as the tracked evidence shows — both tracks stayed inside their reading rules on their own. Created no file. |
| `5.3-convergence-verdict.md` | 5.3 | Classification + first evidence item | `.squad/design/undo-redo/03-convergence.md:336-345` | The classification sentence ("reasoned past it") plus the strongest of the five pieces of evidence — enough for the excerpt to argue the point rather than merely assert it, without the other four items. |
| `5.3-invariant-holes.md` | 5.3 | Numbered findings | `.squad/design/undo-redo/03-convergence.md:545-555` | Items 2 and 3 of "Gaps I am reporting rather than filling" — the INV-2 and INV-3 defects Track A's D2/D3 findings surfaced. The other four gap items (effect-boundary policy, Picea unknowns, measurement, ux) are off-topic for this stop. |
| `5.4-critic-verdicts.composite.md` | 5.4 | Verdict sequence (composite, five sources) | Drop `2026-09-06T19-29-55-critic-undo-redo.md:56`; drop `2026-09-06T21-34-31-critic-undo-redo.md:58`; drop `2026-09-07T06-34-07-critic-undo-redo.md:39`; `.squad/design/undo-redo/05-critic.md:25-27` (pass 4) and `:446-448` (navigation confirmation) | Passes 1-3 were each overwritten in place by the next revision of `05-critic.md` — only pass 4 plus the navigation confirmation survive there. The three earlier LOOP BACK verdicts survive only as one-line summaries in their own archived decision drops. By blocker count the sequence is 4 → 3 → 2 → (approved) — the third LOOP BACK is much the smallest of the three. |
| `5.4-critic-finding-B9.md` | 5.4 | Blocker card | Drop `2026-09-07T06-34-07-critic-undo-redo.md:18-20` | The fullest surviving statement of B9, with the `Runtime.cs:288-291` / `:332` citations that show a subscription tick and a user click share one delegate. `05-critic.md`'s own third-pass wording was overwritten by the fourth pass; the ledger drop is the only place this finding's original wording and citations survive. |
| `5.5-invariant.md` | 5.5 | Invariant text | `.squad/design/undo-redo/00-scope.md:153-163` | INV-3 in full — the invariant the paired property claims to test. |
| `5.5-property.cs` | 5.5 | Code block (not a compilation unit — `content/**` is excluded from the project's compile glob) | `.squad/design/undo-redo/06-spec.md:1034-1072` | `INV_3_redo_after_undo_restores_the_model_the_document_and_all_subsequent_behaviour`, through assertion (iii) — the smallest span that shows all three of INV-3's promises (model equality, document equality up to handler-id renaming, behavioural equality) actually asserted, not just described. |
| `5.5-inv-coverage.md` | 5.5 | Coverage statement | `.squad/design/undo-redo/06-spec.md:1269-1270`, cut at "...already states." | The "none" statement and its reason, cut before the sentence that opens a different point (two invariants flagged as narrower than they read) — that belongs to a different stop, not this one. |
| `5.6-review-headers.composite.md` | 5.6 | Two document headers | `.squad/design/undo-redo-design-record/08-review-blind.md:1-10` and `09-review-verdict.md:1-10` | The first ten lines of each, as requested. Neither header carries a literal timestamp field — no line was invented to supply one. The closest verified timestamp is **not** `07607bf`'s own author date — `2026-09-07T11:56:59+02:00` (`= 09:56:59Z`, per `08-review-blind.md:105`) is the author date of `66379e7`, the pre-squash branch commit `08-review-blind.md` was reviewing when it wrote that sentence. `07607bf` itself, the commit this whole bundle is pinned to, is authored `2026-09-07T13:00:00+02:00` (verified: `git show -s --format='%aI' 66379e7 07607bf`). Citable separately if the slide needs a clock reading, with the correct commit attached — the squash rewriting the timestamp the blind reviewer cited is the same overwrite-in-place theme stop 5.1 already exists to make. **Self-containment note (added 2026-09-07, review finding, blocker 2d):** the source slug is `undo-redo-design-record`, a *different* design directory from `undo-redo` (the slug every other `full/` copy in this bundle is drawn from). Neither `08-review-blind.md` nor `09-review-verdict.md` for that slug is copied into `full/`, and until this note the README never named the slug at all. Stops 5.1–5.5 are checkable entirely from files this bundle carries in `full/`; 5.6 is not — verifying it requires `git show 07607bf:.squad/design/undo-redo-design-record/<file>` against the working tree, the same way every fragment in this index was originally verified, but without a local copy to check it against. Not fixed by copying the two files in, per the runbook's copies-only-what-was-asked-for scope; recorded here instead, to the same standard the excluded PR #358 decision drop already gets above. |
| `5.6-round-2-escalation.composite.md` | 5.6 | Verdict + decision paragraph (composite, one file, two spans) | `.squad/design/undo-redo-design-record/09-review-verdict.md:475-487` and `:735-748` | The round-2 ⚠️ Needs Human Review verdict line and the "two honest paths" paragraph are ~250 lines apart in the source; both are needed to show the chain surfacing a proportionality call to the human rather than the human having to go around the chain to get one. **Same self-containment note as `5.6-review-headers.composite.md` above** — sourced from `undo-redo-design-record`, not copied into `full/`. |
| `5.6-refusal.log` | 5.6 | — | **Not found at `07607bf`.** No literal denied-read log line for `reviewer-blind` exists for PR #359. The closest evidence is `reviewer-blind`'s own narrative self-report, "`gh issue view` is denied to me" (`08-review-blind.md:224`), which is a sentence, not a log line. Created no file. |
| `6-register-why-not-caught.md` | 6 | Retrospective paragraph | Drop `2026-09-07T09-34-55-arch-undo-redo-navigation.md:29-33` | The one paragraph in the record that states, in the architect's own words, why every phase's property was green despite the navigation bug: `00-scope.md`'s INV-2 said "application state," its falsifier said "model," so nothing downstream ever quantified over the difference. |

## Fragments not created

Three requested fragments do not exist verbatim at `07607bf` and were not
fabricated: `5.2-line-abstract.txt`, `5.2-refusal.log`, `5.6-refusal.log`. See
their rows above for what was checked and what the closest surviving evidence
is.

## Overrides

**User, 2026-09-07 — ⚠️-4, composite fragment provenance (`09-review-verdict.md`, "Should Fix"):**

> The rule that every file under stops/ is a verbatim excerpt exists for one reason: nothing
> that reaches a slide may be text that is not in the source. A marker at a seam inside a
> composite file is exactly that. The README row is the provenance record by design — it is
> what the index is for — and the manifest verifies the bytes of every span against 07607bf,
> which an inserted comment would break. Provenance lives beside the fragment, never inside
> it. If the reviewer wants the seam visible from the file itself, the four composites take a
> .composite.md suffix: the name is metadata, the content is not.

Applied: the four fragments the finding names — `5.1-warden-report.composite.md`,
`5.4-critic-verdicts.composite.md`, `5.6-review-headers.composite.md`, and
`5.6-round-2-escalation.composite.md` — carry the `.composite.md` suffix below. No file under
`stops/` was edited to add this override; only these four were renamed, byte-for-byte
unchanged, and `SHA256SUMS` was regenerated against the new names.
