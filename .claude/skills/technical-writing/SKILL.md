---
name: technical-writing
description: Documentation reference — Diátaxis four modes (tutorial, how-to, reference, explanation), README standard, ADR template, Keep a Changelog format, writing style rules (active voice, second person, present tense, no weasel words), document structure conventions, and inline doc comment standards (XML doc, JSDoc). Use when writing or reviewing any `.md` file, drafting an ADR, designing API reference, or building a new doc set.
---

# Technical Writing Reference

The Tech Writer's deep reference. Role and protocol live in `.claude/agents/tech-writer.md`. The squad's documentation principles (Markdown only, docs ship with code, Diátaxis framework, ADR template) live in `.claude/docs/decisions.md`.

---

## The Four Diátaxis Modes

Every document fits exactly **one** of these modes. Mixing modes is the most common documentation failure — it's why so many docs are unreadable.

| Mode | Purpose | Reader's State | Form |
|---|---|---|---|
| **Tutorial** | Learning by doing | Wants to acquire a skill | Lesson, step-by-step, hand-holding |
| **How-to** | Achieving a specific goal | Has a known problem | Recipe, prerequisites + steps |
| **Reference** | Information lookup | Knows what to find | Catalog, encyclopedic, dry |
| **Explanation** | Understanding concepts | Wants to understand *why* | Discursive, contextual, opinionated |

**The litmus test:** if you can't say in one sentence which mode a doc is in, it's wrong. Split it.

### Tutorial

- **Goal:** Get the reader to a moment of "I made it work!"
- **Tone:** Patient, encouraging, hand-holding. Assume the reader knows nothing.
- **Structure:** Linear. Each step builds on the previous.
- **Anti-patterns:** Skipping prerequisites. Forking paths. Adding optional sections. Assuming knowledge.
- **Example titles:** "Getting Started with the Articles API", "Your First End-to-End Test"

### How-to Guide

- **Goal:** The reader has a problem; this guide solves it.
- **Tone:** Direct, task-focused. Assume the reader is competent.
- **Structure:** Prerequisites → ordered steps → verification.
- **Anti-patterns:** Teaching concepts (that's explanation). Listing all options (that's reference).
- **Example titles:** "How to Add a New Bounded Context", "How to Configure DAST in CI"

### Reference

- **Goal:** Lookup. Reader knows what to find.
- **Tone:** Dry, authoritative, exhaustive.
- **Structure:** Catalog. Predictable layout. Easy to scan.
- **Anti-patterns:** Storytelling. Recommendations (those go in explanation). Omitting edge cases.
- **Example titles:** "API Endpoint Reference", "Configuration Reference", "Smart Constructor Catalog"

### Explanation

- **Goal:** Help the reader understand *why* something is the way it is.
- **Tone:** Discursive. Can have opinions. Connects ideas.
- **Structure:** Argumentative. Builds a case.
- **Anti-patterns:** Telling readers how to do things (that's how-to). Listing facts (that's reference).
- **Example titles:** "Why We Chose Functional DDD", "The Trade-offs of TUnit", "Threat Modeling for the Articles Module"

---

## Writing Style Rules

The team's documentation style — applied consistently, enforced at review.

### Voice & Person

- **Second person ("you").** Talk to the reader. Not "the user" or "one." `"You can configure..."` not `"The user may configure..."`.
- **Active voice.** `"The pipeline runs SAST"` not `"SAST is run by the pipeline"`. Active voice is shorter and tells you who's responsible.
- **Present tense.** `"The token expires after one hour"` not `"The token will expire..."`. Use future tense only for genuinely future events.

### Tone

- **Direct, not chatty.** Don't introduce, don't summarize, don't preface. Get to the point.
- **No weasel words.** Avoid "may", "might", "could", "perhaps" unless precision genuinely requires uncertainty. They erode trust.
- **No marketing language.** No "powerful", "robust", "world-class", "next-generation". Show, don't claim.
- **No apology.** Don't apologize for a feature being incomplete or for a doc being a draft. Either fix it or don't mention it.

### Structure

- **Front-load the answer.** First sentence answers the question implied by the title. Details follow.
- **Short paragraphs.** Two to four sentences. Walls of text don't get read.
- **Scannable.** Use headings, lists, code blocks, tables. Readers scan first, read second.
- **Concrete over abstract.** A working code example beats three paragraphs of explanation.
- **One idea per paragraph.** If a paragraph covers two ideas, split it.

### Code Examples

- **Examples must compile and run.** Every code example in docs is verified — outdated examples are worse than no examples.
- **Minimal viable example.** Show only what's necessary to make the point. Cut imports, cut error handling, cut everything that's not the point. Then add back what the reader genuinely needs.
- **Realistic data.** `EmailAddress.Create("alice@example.com")`, not `EmailAddress.Create("foo")`.

### Terminology

- **Pick a term, stick with it.** If the codebase calls them "draft articles", the docs call them "draft articles" — never "unpublished articles" or "article drafts" or "articles in draft state."
- **Use the team's ubiquitous language.** No translating domain terms into generic ones for readability — the domain language IS the readable form.
- **Spell out acronyms on first use.** "Static Application Security Testing (SAST)" the first time, "SAST" thereafter.

---

## Standard Document Structure

Every non-trivial document follows this skeleton:

```markdown
# Document Title (matches H1)

One-paragraph summary: what this doc covers, who it's for, and what mode (tutorial/how-to/reference/explanation).

## Table of Contents (if > 5 sections)

[auto-generated or hand-curated]

## Prerequisites (for tutorial/how-to only)

- What the reader needs to have set up
- What knowledge is assumed

## [Body sections — mode-appropriate]

## Related Reading

- Link to ADR-NNN: ...
- Link to companion how-to: ...

## Changelog (for ADRs and reference docs that evolve)

| Date | Change | Author |
|---|---|---|
```

---

## README Standard

Every project root and every component-level project has a README. Fixed structure:

```markdown
# Project Name

One-sentence description of what this project does.

## What It Is

Two-paragraph description: what problem it solves, who uses it, what it's NOT.

## Quick Start

```bash
# minimum commands to clone, build, and run
git clone ...
cd ...
dotnet run --project src/<Root>.AppHost
```

## Architecture

Brief overview with a link to the architecture deep-dive.

## Contributing

Link to CONTRIBUTING.md.

## License

License name + link to LICENSE file.

## Related

- Link to docs/
- Link to issue tracker
- Link to deployed instance(s)
```

**Rules:**
- README is for **finding your way**, not exhaustive documentation. The Quick Start should fit on one screen.
- The README in the project root is for someone who just landed in the repo. The README in a sub-project is for someone who knows the overall project and is now looking at this piece.

---

## ADR Template

Architectural Decision Records live in `/docs/adr/`, sequentially numbered (`0001-...md`, `0002-...md`).

```markdown
# ADR-NNNN: Decision Title (concise, action-oriented)

**Status:** Proposed | Accepted | Deprecated | Superseded by ADR-NNNN
**Date:** YYYY-MM-DD
**Decision Makers:** [list of people / roles]
**Supersedes:** ADR-NNNN (if applicable)

## Context

What forces this decision? What's the situation? What are the constraints?

Two to four short paragraphs. Be specific. Cite numbers, evidence, prior decisions.

## Decision

The decision, in one paragraph. State it plainly: "We will use X." Not "We are considering X" — that's not a decision yet.

Then a few paragraphs of detail: what's included, what's excluded, what conventions follow from this.

## Consequences

### Positive
- Specific benefits, with reasoning.

### Negative
- Specific costs, with reasoning. **Don't omit these.** An ADR with only positives is suspicious.

### Neutral
- Trade-offs that aren't clearly good or bad — for the future to evaluate.

## Alternatives Considered

- **Alternative A:** description. Why rejected.
- **Alternative B:** description. Why rejected.

Two to four alternatives is the sweet spot. One looks like the decision was foregone. Five+ looks like analysis paralysis.

## Related Decisions

- ADR-NNNN: [related decision]

## References

- Link to relevant external research, RFC, paper, blog post, vendor docs.
```

**Rules:**
- An ADR is **immutable once accepted**. To change a decision, write a new ADR that supersedes it.
- Every architecturally significant decision gets an ADR. Mundane code-style choices don't.
- The architect (`.claude/agents/architect.md`) drafts the technical content; the tech writer reviews structure, voice, and clarity.

---

## CHANGELOG — Keep a Changelog Format

Project root `CHANGELOG.md` follows [keepachangelog.com](https://keepachangelog.com/) exactly:

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Article publishing workflow with smart constructors and state machine.

### Changed
- Promoted `EmailAddress` smart constructor to the shared kernel.

### Deprecated
- `LegacyAuthHandler` will be removed in 2.0. Use `TokenAuthHandler` instead.

### Removed
- `Newtonsoft.Json` dependency.

### Fixed
- Race condition in token refresh under high concurrency.

### Security
- Patched CVE-2026-1234 by upgrading `SomePackage` to 4.2.1.

## [1.3.0] — 2026-01-15

### Added
- ...
```

**Rules:**
- Six categories, in this order: Added, Changed, Deprecated, Removed, Fixed, Security.
- The `[Unreleased]` section accumulates changes for the next release; CI moves it under a version heading at release time.
- Entries are user-focused, not commit-focused. "Added article publishing" is right; "Refactored ArticleHandler" is not (that's a commit message, not a changelog entry).

---

## API Reference Standard

For any HTTP API, the reference doc per endpoint follows this:

```markdown
### POST /articles/{id}/publish

Publish a draft article.

**Authentication:** Required. Token must belong to the article's author.

**Path parameters:**
| Name | Type | Description |
|---|---|---|
| `id` | UUID | Article ID |

**Request body:** None.

**Response 200:**
```json
{
  "id": "...",
  "title": "...",
  "publishedAt": "2026-01-15T10:30:00Z"
}
```

**Errors:**
| Status | Code | Cause |
|---|---|---|
| 400 | `validation` | The article has unfilled required fields. |
| 403 | `not_author` | The token's user is not the article's author. |
| 404 | `not_found` | No draft article with that ID. |
| 409 | `already_published` | The article is already in published state. |

**Example:**
```bash
curl -X POST https://api.example.com/articles/abc-123/publish \
  -H "Authorization: Bearer $TOKEN"
```
```

---

## Inline Doc Comments

### XML Doc Comments (C#)

Public APIs have XML doc comments. Required minimum:
- `<summary>` for the type or member.
- `<param>` for every parameter.
- `<returns>` for non-`void` returns.
- `<exception>` for exceptions the caller should expect (only programmer-bug exceptions, since domain errors are `Result<T, TError>`).

```csharp
/// <summary>
/// Publishes a draft article. Validates that the requester is the article's author and that the article is currently in draft state.
/// </summary>
/// <param name="cmd">The publish command, including the article ID and the requester's user ID.</param>
/// <param name="repo">Article repository capability.</param>
/// <param name="now">Clock capability.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>The published article on success, or a domain error describing why publication was refused.</returns>
public static Task<Result<PublishedArticle, PublishArticleError>> Run(
    PublishArticleCommand cmd,
    ArticleRepository repo,
    Now now,
    CancellationToken ct) { /* ... */ }
```

**Rules:**
- Use `<inheritdoc/>` on interface implementations rather than copy-pasting.
- Keep the summary to one or two sentences. Detail goes in the doc, not the summary.
- Don't restate the type. `<summary>The user's email address.</summary>` on a property of type `EmailAddress` is noise.

### JSDoc (JavaScript)

```javascript
/**
 * Publishes a draft article via the API.
 *
 * @param {string} articleId - The article's UUID.
 * @param {string} token - Bearer token for the requester.
 * @returns {Promise<{ ok: true, data: PublishedArticle } | { ok: false, error: PublishError }>}
 *   Discriminated result. `ok: false` for expected failures (validation, auth, conflict).
 *   Throws only for unexpected failures (network unreachable, server 5xx).
 */
export async function publishArticle(articleId, token) { /* ... */ }
```

---

## What This Skill Does NOT Cover

- **Decisions about *what* to document** — driven by the Tech Writer charter (every feature, every API change, every config change, every ADR).
- **Doc-sync verification process** — the Tech Writer charter describes when to run it; this skill provides the writing standards used during the verification.
- **The team's principles content** — `.claude/docs/decisions.md`. The tech writer formats and clarifies the writing of that file but does not author the principles themselves.
