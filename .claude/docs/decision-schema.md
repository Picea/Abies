# Decision-drop schema

Every file written to `.squad/decisions/inbox/` must conform to this schema. The `scribe-decision-merger` hook validates on `SubagentStop`; malformed entries are quarantined to `.squad/decisions/quarantine/` and surfaced in the statusline.

This is the contract subagents emit and the orchestrator parses. It is load-bearing — changes here ripple to every reviewer-reconcile/security-expert/performance-engineer/architect/curator output.

## File location and naming

- **Path:** `.squad/decisions/inbox/<short-slug>.md`
- **Slug:** lowercase, hyphenated, ≤ 50 chars. Examples: `auth-uses-jwt`, `ef-include-depth-limit`, `review-2026-05-06-pr-142`.
- **Encoding:** UTF-8, LF line endings.

## Required front-matter

YAML front-matter at the top of the file. All fields are required unless marked optional.

```yaml
---
id: <agent>-<utc-iso8601-compact>-<slug>
agent: reviewer-reconcile | security-expert | performance-engineer | architect | critic | realist | spec-author | scope-warden | dreamer-first-principles | dreamer-informed | dreamer-convergence | curator | curator-adversary | tech-writer | ux-expert | csharp-dev | js-dev | devops | lead
verdict: PASS | NEEDS-CHANGES | BLOCKED | INFO
scope: review | decision | threat-model | benchmark | retro | handoff | architecture | doc | other
created: <utc-iso8601>
commit: <40-hex-sha>              # REQUIRED when agent: reviewer-reconcile and scope: review — see below
targets:                          # optional — paths/lines this drop pertains to
  - path: src/MyApp/Foo.cs
    lines: "120-145"              # optional, string form to allow ranges
blockers:                         # optional — 🔴 must-fix items; non-empty → verdict ≠ PASS
  - file: src/MyApp/Foo.cs
    line: 132                     # optional
    reason: "Mutating shared state without synchronisation."
high:                             # optional — 🟠 strong recommendations; ignored at risk
  - file: ...
    reason: ...
medium:                           # optional — 🟡 nits / preferences
  - file: ...
    reason: ...
good:                             # optional — ✅ explicit positive callouts (rare; use sparingly)
  - file: ...
    reason: ...
references:                       # optional — decision IDs this drop supersedes / cites
  - <decision-id>
---
```

### Field rules

- **`id`** must be globally unique. Format: `<agent>-<YYYYMMDDTHHMMSSZ>-<slug>`. Example: `reviewer-reconcile-20260506T143012Z-pr-142-auth-flow`.
- **`agent`** must be one of the values listed above: a subagent name matching a file in `.claude/agents/<name>.md`, or `lead` for the orchestrator's own decisions.
  - **`reviewer-blind` never writes a drop.** It produces `08-review-blind.md` and no verdict; the drop for a review is `reviewer-reconcile`'s.
  - **Retired names stay valid.** `scribe-decision-merger.sh` keeps a `LEGACY_AGENTS` set alongside the live roster — currently `reviewer`, which split into `reviewer-blind` and `reviewer-reconcile`. A drop that was valid when written stays valid; quarantining history would destroy exactly the evidence the register exists to keep. Do not emit a legacy name in a new drop.
- **`verdict`**:
  - `PASS` — no blockers; high/medium may exist but are non-binding.
  - `NEEDS-CHANGES` — at least one blocker; the implementer should fix and re-request review.
  - `BLOCKED` — at least one blocker that the reviewer believes the implementer cannot resolve without escalation (architect, security, etc.).
  - `INFO` — purely informational; no implied action. Used for handoffs, retros, threat-model deliveries, benchmark results.
- **`scope`** controls how the drop is indexed and routed; pick the closest match.
- **`commit`** — full 40-hex git sha. No abbreviation, no branch or tag name, no `^{commit}` peel. **Required when `agent: reviewer-reconcile` and `scope: review`.** Read it live with `git rev-parse HEAD` in the checkout the review actually examined — never copy it from the PR description, the plan, or any other artifact — and it must equal that checkout's true `HEAD` at the moment the drop is written.
  - This is a compound condition — both `agent: reviewer-reconcile` and `scope: review` must hold before `validate()` treats `commit:` as required. A drop declaring the **legacy** `agent: reviewer` name (see the `agent` field rule above) does not trigger the check, even though it represents exactly the kind of review this field exists to pin — which is why every worked review example below uses `agent: reviewer-reconcile`, not the legacy name, and carries a `commit:` field.
  - **Fail-closed consequence:** before writing `.squad/.last-review-verdict` (the token `enforce-review-verdict.sh` reads as the commit gate), `scribe-decision-merger.sh` cross-checks this field against `git -C <destination> rev-parse HEAD`, where `<destination>` is resolved from the hook payload's `cwd`. A drop with `commit:` absent, abbreviated, malformed, or unequal to that HEAD ⇒ the merger writes the cache **nowhere** and reports the refusal. There is no fallback; the commit gate stays blocked until a compliant drop lands.
  - **Not a security control on its own.** This field is an integrity cross-check on the process rule — "the review happened in the tree this session is sitting in" — not a selector and not a security control. The destination path always comes from the payload's `cwd`; it is never derived from this field.
- **Verdict ↔ blockers consistency:**
  - `PASS` ⇒ `blockers: []` (or omitted).
  - `NEEDS-CHANGES` or `BLOCKED` ⇒ `blockers` non-empty.
  - The validator rejects mismatches.

## Body

Free-form markdown after the front-matter. Conventions:

- **First line** is a one-sentence summary. The statusline and session-logger both read this.
- **Sections** beyond that are at the agent's discretion. Reviewers typically include "Findings" and "Suggested fix"; architects typically include "Decision", "Alternatives considered", "Consequences"; security-expert typically includes "Threat", "Mitigation", "Regression test".
- **Quotes from source files** are fine; keep ≤ 20 lines. Reference paths + line numbers from the front-matter `targets` field.

## Example: reviewer-reconcile PASS

```markdown
---
id: reviewer-reconcile-20260506T091200Z-pr-142-auth-flow
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-05-06T09:12:00Z
commit: 1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b
targets:
  - path: src/Auth/TokenService.cs
  - path: tests/Auth/TokenServiceTests.cs
blockers: []
high:
  - file: src/Auth/TokenService.cs
    reason: "Consider extracting the validation predicate to a named function for readability — not blocking."
medium: []
good:
  - file: tests/Auth/TokenServiceTests.cs
    reason: "Property-based tests cover the algebraic identities cleanly."
references: []
---

PR #142 auth-flow refactor passes review.

The change replaces nested `if`/`else` with Result composition, which aligns with `decisions.md → Functional DDD → Result chaining`. No new mutable state, no exceptions thrown across the seam. Tests are TUnit, no xUnit slip.
```

## Example: reviewer-reconcile NEEDS-CHANGES

```markdown
---
id: reviewer-reconcile-20260506T101500Z-pr-143-article-publish
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-05-06T10:15:00Z
commit: 9f8e7d6c5b4a3f2e1d0c9b8a7f6e5d4c3b2a1f0e
targets:
  - path: src/Articles/PublishCommand.cs
    lines: "45-78"
blockers:
  - file: src/Articles/PublishCommand.cs
    line: 52
    reason: "Throws `InvalidOperationException` instead of returning `Result<Unit, PublishError>`. Violates `decisions.md → Errors as values`."
  - file: src/Articles/PublishCommand.cs
    line: 71
    reason: "Mutates `Article` in place rather than returning a new state. Violates `decisions.md → Smart constructors / immutability`."
high:
  - file: tests/Articles/PublishCommandTests.cs
    reason: "Missing test for the failure branch when the article is already published."
medium: []
good: []
references:
  - architect-20260415T120000Z-article-state-machine
---

PR #143 (article publish) requires changes before merge: two functional-DDD violations and one missing test.
```

## Example: security-expert INFO (threat model)

```markdown
---
id: security-expert-20260506T140000Z-feature-bulk-import
agent: security-expert
verdict: INFO
scope: threat-model
created: 2026-05-06T14:00:00Z
targets:
  - path: src/BulkImport/
blockers: []
high:
  - reason: "CSV upload endpoint is unauthenticated by default in Aspire dev profile; add `[Authorize]` before merging to staging."
medium: []
good: []
references: []
---

Threat model for the bulk-import feature: STRIDE pass identified 3 medium-severity concerns; full write-up below.

## STRIDE

(...)
```

## Forbidden / common mistakes

- **No front-matter** → archived as LEGACY under a `<!-- legacy -->` marker, not quarantined. See "Migration notes" below.
- **`verdict: PASS` with non-empty `blockers`** → quarantined as inconsistent.
- **`agent`** value not matching any subagent (and not `lead`) → quarantined.
- **Free-form verdict strings** like `"approved"`, `"lgtm"`, `"reject"` → quarantined; use the enum.
- **Multiple decisions in one file** → not rejected. The hook's front-matter regex matches only the *first* `---`-delimited block, so it takes that block's `id`/`agent`/`verdict` for the entry heading and then appends the file's entire raw text — including the second front-matter block — verbatim underneath it. Split into separate inbox entries before dropping; the hook will not catch this for you.
- **Body before front-matter** → not rejected as invalid. The front-matter regex requires `---` to open the file (only leading whitespace is tolerated before it), so any prose before it fails the match and the whole drop — structured fields included — is archived as LEGACY, the same as a file with no front-matter at all. This is a known sharp edge, not intended design: because the drop never validates, a `reviewer-reconcile` verdict written this way never reaches `.last-review-verdict` (below), so the statusline can silently keep showing a stale value. It's tracked separately as a hook defect (owned outside this doc) — front-matter must still be the first thing in the file.

## Statusline integration

The scribe-decision-merger writes the most recent reviewer verdict to `.squad/.last-review-verdict` (two lines: `PASS|NEEDS-CHANGES|BLOCKED|INFO`, then `commit: <sha>`) whenever it archives a *valid* drop with `agent: reviewer-reconcile` and `scope: review` **and** that drop's `commit:` field passes the cross-check above. `.squad/.last-review-verdict` has exactly one writer — the merger — and no agent, including `reviewer-reconcile`, writes it directly by any tool-mediated path; see `.claude/agents/reviewer-reconcile.md`. It is currently in a CI-6 shadow period that logs to `.squad/log/gate-shadow.md` and allows instead of refusing until `expires:` in `.squad/.gate-shadow` (2026-09-20), after which no action is required for it to enforce. The statusline reads the cache for the `last-review:` field. The `Q=<n>` quarantine count next to the inbox count is computed by the statusline itself, counting files in `.squad/decisions/quarantine/` directly — the hook does not write a count anywhere.

**Known limitation — `agent:` is self-declared, not authenticated.** The merger's validator has, across several review rounds, closed multiple *parsing* bugs that let a drop's structural content disagree with its own declared `agent`/`verdict` fields (nested-key promotion, whitespace-indentation tricks, a read-time CR-to-LF translation — see the "Known limitation" comment near the top of `scribe-decision-merger.sh` for the full list). None of that closes, and nothing in this schema or hook *can* close, the simpler case: any well-formed, self-consistent drop that just writes `agent: reviewer` / `verdict: PASS` at column 0 is archived and cached as a real reviewer verdict, regardless of which agent (or a human editing `.squad/decisions/inbox/` directly) actually produced it. The `SubagentStop` payload the hook receives carries no authorship channel to check `agent:` against. Treat `.squad/.last-review-verdict` and the `[agent · verdict]` heading in `decisions.md` as *what the drop claimed*, not as independently verified. Closing this would require a trusted identity to be stamped into the payload upstream of this hook — it is out of scope for the parser itself.

## Migration notes for existing free-form drops

Existing inbox entries written before this schema landed are tolerated indefinitely, not for a single cycle: the merger appends them under a `<!-- legacy -->` comment in `decisions.md` rather than rejecting them, and nothing subsequently removes that entry. `squad-rotate.py` explicitly excludes `decisions.md` from rotation ("semantic; manual + curator-driven") — there is no automated flush. Cleaning up legacy entries is a manual or curator-driven edit to `decisions.md`, same as any other pruning of that file. New code paths must emit valid front-matter from day one.
