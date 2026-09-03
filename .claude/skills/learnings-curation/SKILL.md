---
name: learnings-curation
description: Methodology for promoting durable session learnings into the framework while filtering out noise. Procedure, evidence standards, common failure modes, and worked examples. Used by the curator subagent.
---

# Learnings curation

This skill describes how to scan recent session activity, identify durable learnings, and write proposals that pass review. The default voice is skeptical: most candidate "learnings" are noise, and a curator who proposes too much is worse than a curator who proposes nothing.

## The two-pass procedure

### Pass 1: harvest candidates (wide net)

Read everything in scope. Don't filter yet — the goal here is recall, not precision.

Sources, in order:

1. `.squad/log/*.md` for the requested time window. Each entry is a one-line summary of a subagent's run. Pay attention to:
   - Repeated agent invocations on similar topics (`security-expert` called 4 times on auth issues in a week → likely a pattern).
   - Reviewer findings that recur across different reviews (same Must Fix theme more than once).
   - Performance regressions, security incidents, doc-sync misses, build breakages.
2. `.squad/decisions/archive/<YYYY-MM>/*.md` for the same window. Each accepted decision is, by definition, something the team thought was worth recording at the time. Look for:
   - Decisions that contradict an earlier decision (the team learned something).
   - Clusters of decisions on the same topic (auth strategy, EF Core conventions, etc.) — clusters are usually a sign that a more general rule wants to crystallize.
3. `.squad/orchestration-log/*.md` if present. These are Lead-authored handoff snapshots. They expose patterns that don't show up in subagent logs because they're about coordination, not execution.
4. The current conversation, if the user is curating from a specific thread.

Write each candidate down in your scratchpad with: a one-sentence claim, the evidence pointers, and a guess at the target file. Don't worry about quality yet.

### Pass 2: filter and write (narrow funnel)

For each candidate, apply the three tests in order. Drop the candidate as soon as it fails any test.

#### Test 1: Recurrence

Does this show up in at least two distinct prior incidents? Cite both. If not, drop. Single-incident "learnings" are how cargo cults form.

A subtle case: two incidents *for the same root cause* count as one. If the team forgot to add an index three Tuesdays in a row on the same query, that's one bug, not three pieces of evidence. Look for two *independent* manifestations.

#### Test 2: Specificity

Can the proposed rule be acted on without a judgement call? "Be careful with concurrency" is not actionable. "When using `IAsyncEnumerable<T>` from EF Core, always pair it with `WithCancellation(token)` to honor cooperative cancellation" is actionable.

A useful test: imagine you're the reviewer applying this rule on a future PR. Could you point to a line of code and say "this violates the rule" or "this satisfies the rule"? If the answer is "depends on context", the rule is still too vague.

#### Test 3: Generalisability

Would this rule have applied to *all* the incidents you cited, or just the most recent one? If you have to add caveats ("…except in the auth code where we did it differently"), the rule isn't general — it's a description of the most recent fix dressed up as a principle.

A clean way to check: rewrite the rule with the most recent incident's specifics removed. Does it still help? If it becomes vacuous, the rule was secretly about that one incident.

If a candidate passes all three tests, write the proposal. Otherwise drop it.

## Target-file matrix

Different kinds of learnings belong in different files. Pick the right home or the proposal will be rejected during review.

| Learning shape | Target file | Section |
|---|---|---|
| New framework convention (style, structure, naming) | `.claude/docs/decisions.md` | The matching framework section, or "(new)" |
| Refinement to an existing convention | `.claude/docs/decisions.md` | The exact existing section, with before/after |
| Retiring an obsolete convention | `.claude/docs/decisions.md` | The exact existing section, with retirement reason |
| Stack adoption (new library, version bump, tool change) | `.claude/docs/tech-stack.md` | The matching section |
| New reusable pattern with code samples | `.claude/skills/<existing>/SKILL.md` | Append, or refine an existing pattern |
| Recurring topic deserving its own reference | New skill folder | n/a — this is `change_kind: new-skill` |

If a candidate doesn't fit any of these, it probably isn't a framework learning. It's either project-specific (belongs in project docs), an architectural decision (route to architect for an ADR instead), or noise (drop it).

## Evidence standards

Every proposal cites at least two pieces of evidence. Each piece must be:

- **A file path you can grep for.** "We saw this in March" is not evidence. "`.squad/decisions/archive/2026-03/2026-03-12T14-22-01-jwt-rotation.md`" is evidence.
- **An incident, not an opinion.** A decision that was made counts. A reviewer Must Fix that recurred counts. A session log entry where a subagent flagged the issue counts. "I think this would be useful" does not count.
- **Concrete enough to look up.** Date plus filename plus enough context that a reviewer can find the original within 30 seconds.

When in doubt, cite three pieces of evidence rather than two. The marginal cost is small and the credibility gain is large.

## Worked examples

### Example A: Promoted

**Candidate:** "Use `JsonWebTokenHandler` for JWT validation, not the deprecated `JwtSecurityTokenHandler`."

**Evidence:**
- `.squad/decisions/archive/2026-04/2026-04-08T…-jwt-validation-handler.md` — security-expert ruled on the choice for the auth service.
- `.squad/log/2026-04-22-session.md` — reviewer flagged a PR using the old handler in the notifications service. Eight lines: "Must Fix: replace `JwtSecurityTokenHandler` per 2026-04-08 decision."
- `.squad/log/2026-05-02-session.md` — same Must Fix, different service.

**Tests:** Recurrence ✅ (three independent incidents). Specificity ✅ (a reviewer can grep for the deprecated type name). Generalisability ✅ (the rule applies to every JWT-handling site, not just the original auth service).

**Action:** Write a proposal targeting `decisions.md` in the Security section, `change_kind: add`. Cite all three pieces of evidence.

### Example B: Rejected — single incident

**Candidate:** "Always include a `correlation_id` field in domain events."

**Evidence:** One ADR from last week proposing it, currently unimplemented.

**Tests:** Recurrence ❌ (one incident, and it's a proposal, not a track record).

**Action:** Drop. Note in the handoff summary: "Considered correlation_id-on-events — a recent ADR proposes it but there's no track record yet. Worth revisiting after it's been in production for a few months."

### Example C: Rejected — too vague

**Candidate:** "Be careful with EF Core query performance."

**Evidence:** Three perf regressions in the last quarter caused by EF Core issues.

**Tests:** Recurrence ✅. Specificity ❌ ("be careful" isn't actionable).

**Action:** Don't propose this rule. Instead, look at the three incidents and ask: do they share a *specific* root cause? If yes, propose that specific rule. ("Avoid `.Include()` chains deeper than 2 levels — use projections instead.") If the three incidents have unrelated root causes, there's no general rule yet.

### Example D: Rejected — not generalisable

**Candidate:** "Always use SemaphoreSlim with a count of 1 for cross-async-task mutual exclusion in the article publishing flow."

**Evidence:** Two prior incidents, both in the article publishing flow.

**Tests:** Recurrence ✅. Specificity ✅. Generalisability ❌ — the rule mentions a specific flow.

**Action:** Don't propose. The pattern, if real, is "use SemaphoreSlim for cross-async-task mutual exclusion" — but that's just standard advice, not a learning. The article-flow-specific bit belongs in that flow's code comments, not the framework.

## Common failure modes for curators

- **Listing instead of judging.** "Here are 14 things we learned this month" is a worse output than "Here are 2 proposals; I considered 12 other candidates and rejected them for these reasons." The rejected list is often the more valuable signal.
- **Recency bias.** What happened in the last three days dominates attention. Force yourself to read at least the last two weeks before writing anything, and prefer evidence that spans a longer window.
- **Promoting workarounds into principles.** A workaround for a third-party library bug is not a framework rule. It's a comment in the code. If the bug gets fixed upstream, the rule becomes wrong.
- **Erasing useful tension.** Sometimes two principles in `decisions.md` are deliberately in tension (e.g. "favor immutability" vs "favor performance"). A learning that "resolves" the tension by deleting one side is usually destroying signal. Refinements that *clarify* when each applies are fine; refinements that delete the tension are not.
- **Editing your own evidence.** Never modify session logs or decision archives. They're the historical record. If a log entry is misleading, note that in the proposal's Risks section — don't rewrite history.

## What to put in the handoff summary

The summary you return to the orchestrator is more important than the proposals themselves. Include:

1. **Window scanned.** "I read `.squad/log/` from 2026-04-15 through 2026-05-06 (3 weeks) and `.squad/decisions/archive/2026-04/` and `.squad/decisions/archive/2026-05/`."
2. **Proposal counts.** "Wrote 2 proposals: 1 add, 1 refine."
3. **Proposal slugs.** "`jwt-validation-handler-rule.md`, `ef-core-include-depth.md`."
4. **Considered but rejected.** A short list with one-line reasons. This is the institutional-memory output — even rejected candidates inform future curation.
5. **Concerns about load-bearing files.** Anything you noticed in `principles-enforcement.md`, charter frontmatter, hooks, or `CLAUDE.md` that *might* warrant change. Flag, don't fix. The user decides.
