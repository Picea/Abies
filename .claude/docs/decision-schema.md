# Decision-drop schema

Every file written to `.squad/decisions/inbox/` must conform to this schema. The `scribe-decision-merger` hook validates on `SubagentStop`; malformed entries are quarantined to `.squad/decisions/quarantine/` and surfaced in the statusline.

This is the contract subagents emit and the orchestrator parses. It is load-bearing — changes here ripple to every reviewer/security-expert/performance-engineer/architect/curator output.

## File location and naming

- **Path:** `.squad/decisions/inbox/<short-slug>.md`
- **Slug:** lowercase, hyphenated, ≤ 50 chars. Examples: `auth-uses-jwt`, `ef-include-depth-limit`, `review-2026-05-06-pr-142`.
- **Encoding:** UTF-8, LF line endings.

## Required front-matter

YAML front-matter at the top of the file. All fields are required unless marked optional.

```yaml
---
id: <agent>-<utc-iso8601-compact>-<slug>
agent: reviewer | security-expert | performance-engineer | architect | critic | realist | spec-author | dreamer-first-principles | dreamer-informed | dreamer-convergence | curator | tech-writer | ux-expert | csharp-dev | js-dev | devops | lead
verdict: PASS | NEEDS-CHANGES | BLOCKED | INFO
scope: review | decision | threat-model | benchmark | retro | handoff | architecture | doc | other
created: <utc-iso8601>
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

- **`id`** must be globally unique. Format: `<agent>-<YYYYMMDDTHHMMSSZ>-<slug>`. Example: `reviewer-20260506T143012Z-pr-142-auth-flow`.
- **`agent`** must be one of the values listed above: a subagent name matching a file in `.claude/agents/<name>.md`, or `lead` for the orchestrator's own decisions.
- **`verdict`**:
  - `PASS` — no blockers; high/medium may exist but are non-binding.
  - `NEEDS-CHANGES` — at least one blocker; the implementer should fix and re-request review.
  - `BLOCKED` — at least one blocker that the reviewer believes the implementer cannot resolve without escalation (architect, security, etc.).
  - `INFO` — purely informational; no implied action. Used for handoffs, retros, threat-model deliveries, benchmark results.
- **`scope`** controls how the drop is indexed and routed; pick the closest match.
- **Verdict ↔ blockers consistency:**
  - `PASS` ⇒ `blockers: []` (or omitted).
  - `NEEDS-CHANGES` or `BLOCKED` ⇒ `blockers` non-empty.
  - The validator rejects mismatches.

## Body

Free-form markdown after the front-matter. Conventions:

- **First line** is a one-sentence summary. The statusline and session-logger both read this.
- **Sections** beyond that are at the agent's discretion. Reviewers typically include "Findings" and "Suggested fix"; architects typically include "Decision", "Alternatives considered", "Consequences"; security-expert typically includes "Threat", "Mitigation", "Regression test".
- **Quotes from source files** are fine; keep ≤ 20 lines. Reference paths + line numbers from the front-matter `targets` field.

## Example: reviewer PASS

```markdown
---
id: reviewer-20260506T091200Z-pr-142-auth-flow
agent: reviewer
verdict: PASS
scope: review
created: 2026-05-06T09:12:00Z
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

## Example: reviewer NEEDS-CHANGES

```markdown
---
id: reviewer-20260506T101500Z-pr-143-article-publish
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-05-06T10:15:00Z
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
- **Body before front-matter** → not rejected as invalid. The front-matter regex requires `---` to open the file (only leading whitespace is tolerated before it), so any prose before it fails the match and the whole drop — structured fields included — is archived as LEGACY, the same as a file with no front-matter at all. This is a known sharp edge, not intended design: because the drop never validates, a `reviewer` verdict written this way never reaches `.last-review-verdict` (below), so the statusline can silently keep showing a stale value. It's tracked separately as a hook defect (owned outside this doc) — front-matter must still be the first thing in the file.

## Statusline integration

The scribe-decision-merger writes the most recent reviewer verdict to `.squad/.last-review-verdict` (single line, `PASS|NEEDS-CHANGES|BLOCKED|–`) whenever it archives a *valid* drop with `agent: reviewer` and `scope: review`. The statusline reads this for the `last-review:` field. The `Q=<n>` quarantine count next to the inbox count is computed by the statusline itself, counting files in `.squad/decisions/quarantine/` directly — the hook does not write a count anywhere.

**Known limitation — `agent:` is self-declared, not authenticated.** The merger's validator has, across several review rounds, closed multiple *parsing* bugs that let a drop's structural content disagree with its own declared `agent`/`verdict` fields (nested-key promotion, whitespace-indentation tricks, a read-time CR-to-LF translation — see the "Known limitation" comment near the top of `scribe-decision-merger.sh` for the full list). None of that closes, and nothing in this schema or hook *can* close, the simpler case: any well-formed, self-consistent drop that just writes `agent: reviewer` / `verdict: PASS` at column 0 is archived and cached as a real reviewer verdict, regardless of which agent (or a human editing `.squad/decisions/inbox/` directly) actually produced it. The `SubagentStop` payload the hook receives carries no authorship channel to check `agent:` against. Treat `.squad/.last-review-verdict` and the `[agent · verdict]` heading in `decisions.md` as *what the drop claimed*, not as independently verified. Closing this would require a trusted identity to be stamped into the payload upstream of this hook — it is out of scope for the parser itself.

## Migration notes for existing free-form drops

Existing inbox entries written before this schema landed are tolerated indefinitely, not for a single cycle: the merger appends them under a `<!-- legacy -->` comment in `decisions.md` rather than rejecting them, and nothing subsequently removes that entry. `squad-rotate.py` explicitly excludes `decisions.md` from rotation ("semantic; manual + curator-driven") — there is no automated flush. Cleaning up legacy entries is a manual or curator-driven edit to `decisions.md`, same as any other pruning of that file. New code paths must emit valid front-matter from day one.
