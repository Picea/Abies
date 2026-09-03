---
name: threat-model
description: Dispatch the security-expert subagent to produce a STRIDE threat model for a feature or component, with mitigations and regression test recommendations. Use when the user types `/threat-model [feature]`.
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Bash(git*)
---

# /threat-model — STRIDE threat model

Dispatches the `security-expert` subagent to produce a structured threat model. Writes the result to `.squad/decisions/inbox/security-threat-<feature>.md` (verdict `INFO`, scope `threat-model`).

## Arguments

- `feature` — required. Hyphenated name of the feature or bounded context. Examples: `bulk-import`, `oauth-flow`, `article-publish`.

## Procedure

1. **Identify scope.** Map the `feature` argument to:
   - Files/directories under analysis (grep + glob; ask the user if ambiguous).
   - Trust boundaries (where untrusted input enters, where authority decisions are made).
   - External dependencies the feature touches (DB, message broker, external APIs).

2. **Build the brief** for the security-expert. Include:
   - Scope summary (files, boundaries, dependencies).
   - The STRIDE template (the security-expert is expected to know this; it's defined in the `security-toolchain` skill).
   - References to load: `.claude/agents/security-expert.md`, `.claude/skills/security-toolchain/SKILL.md`, `.claude/docs/decisions.md` (Security section), `.claude/docs/decision-schema.md`.
   - The current state: branch, recent commits touching the scoped paths.

3. **Dispatch the `security-expert` subagent** with the brief. The agent must produce a threat model conforming to the schema (verdict `INFO` for delivery, or `NEEDS-CHANGES` if it finds an unmitigated 🔴-level threat in code that's already shipped).

4. **Surface the result** with a short summary:
   - Threat counts by severity (🔴 / 🟠 / 🟡).
   - Which mitigations already exist vs. need work.
   - Path to the full report.

5. The scribe-decision-merger hook merges to `decisions.md` on `SubagentStop`.

## What the security-expert produces

The threat model body should include:

- **Diagram or text description of trust boundaries.**
- **STRIDE table** — for each STRIDE category (Spoofing, Tampering, Repudiation, Info disclosure, Denial of service, Elevation of privilege), the identified threats with severity.
- **Mitigations** — for each 🔴 and 🟠 threat: the recommended mitigation, whether it's already implemented, and where (file/line if applicable).
- **Regression tests** — for each accepted mitigation, what test should exist to prevent regression. The test file path and a brief description (security-expert does not write the test; reviewer or csharp-dev does).

Reference: `.claude/skills/security-toolchain/SKILL.md` for the canonical templates.

## What this skill does NOT do

- Does not write code.
- Does not run scanners (the security-expert may, via Bash + Semgrep MCP; not this skill's concern).
- Does not modify production data, configurations, or secrets.

## Failure modes

- **Feature argument missing:** ask for it before dispatching.
- **Scope cannot be resolved** (no files match the feature name): present what was found and ask the user to confirm or refine the scope before dispatch.
- **Massive scope** (>30 files): warn the user that a useful threat model may need to be split per sub-feature.
