# Sovereignty Profile

> **Plaats deze file in:** `.claude/docs/profiles/sovereignty.md`

This profile adds principles and restrictions for projects working with sovereign compute, EU government workloads, regulated industries (DORA / GDPR / NIS2), or any context where CLOUD Act exposure is a concern.

## Activation

In `.claude/docs/profiles-active.md`:

```
# Active profiles for this project
- sovereignty
```

When activated:
- The Lead reads this file at session start (in addition to `decisions.md` and `tech-stack.md`)
- Specialists check their work against the rules below
- `sovereignty-auditor` subagent becomes available for cross-cutting reviews

## Additional principles when active

### 1. IP Protection (cryptographic)

- **All commits MUST be GPG-signed.** Enforced by `hooks/enforce-gpg-signing.sh`. No exceptions; the hook will refuse `git commit` without a valid signature.
- **GPG key continuity is preferred** over rotation. Rotating the signing key requires an ADR documenting why, because it breaks the cryptographic auteurschap chain.
- **GitHub Verified badge required** for all commits to the project's primary branches. CI may fail builds without it.
- **Tags also signed** (`tag.gpgsign=true`).

### 2. Vendor restrictions

- **No Docker.** Use Podman exclusively. Rationale: rootless, daemonless, no commercial license restrictions, EU-friendly upstream (Red Hat).
- **No US enterprise SaaS in critical path.** Excluded: Datadog, New Relic, Splunk, Auth0, Snowflake, etc. Use EU-resident or open-source alternatives.
- **Cloud routing must stay in EU.** Scaleway preferred for compute (H100 GPUs, EU-sovereign). OVHcloud fallback for SecNumCloud requirements. AWS / Azure / GCP excluded except where explicitly approved via ADR for non-sensitive workloads.
- **CDNs / DNS / observability stacks**: prefer EU-resident options.

### 3. AI tooling — CLOUD Act mitigation

Anthropic is US-based. This profile acknowledges that Claude Code (Anthropic) has CLOUD Act exposure for prompts and context sent to the API. Mitigation rules:

- **Code stays local.** Claude Code uses on-device context — never upload entire codebase or sensitive customer data.
- **`.claudeignore` is mandatory** for projects with this profile. At minimum, exclude:
  ```
  # Secrets / credentials
  **/secrets/
  **/.env*
  **/appsettings.Local.json
  
  # Customer / production data
  **/data/customer/
  **/data/production/
  
  # Sensitive integration code (route to local LLM instead)
  **/src/Soveritas.Sovereign.Core/
  ```
- **High-sensitivity work uses local Ollama.** For code touching encryption, key management, customer data flows, or compliance-critical paths, switch to `ollama` (e.g. `qwen2.5-coder:14b`) and document the choice in the session log.
- **Audit log retention.** Claude Code session logs in `~/.config/claude/sessions/` are included in Borg backups for reconstructability in case of IP disputes.

### 4. Compliance awareness

When the project touches regulated work, specialists must:

- **DORA** (Digital Operational Resilience Act): use correct terminology in code, comments, docs. ICT incident management, third-party risk, operational resilience testing are domain terms — not "system reliability" or "monitoring."
- **GDPR**: data flow diagrams required for any new feature processing personal data. Lawful basis documented per processing activity.
- **NIS2**: incident response patterns implemented (detection → analysis → containment → eradication → recovery → lessons learned).

The `tech-writer` subagent must use regulatory terminology in user-facing docs when the audience is regulated. Don't dumb it down.

### 5. Hardware sovereignty

For projects developed under Dutch art. 7 Auteurswet IP-positie or similar:

- **Own hardware** for development required (strengthens IP claim).
- **Network segmentation** (VLANs / zones) for sensitive work.
- **Backups encrypted and EU-resident** (Borg on local NAS, not US cloud).
- **Audit trail** of where the code was developed (host metadata, GPG signatures, ADR documentation).

This is operational documentation rather than enforced by hooks — but the Lead should remind the user on `SessionStart` when sovereignty profile is active and the project is in a `clients/` or `soveritas/` folder.

## Additional subagent: sovereignty-auditor

> **Charter location:** `.claude/agents/sovereignty-auditor.md`

When this profile is active, an additional subagent becomes available:

**Role:** Cross-cutting review of sovereignty implications of code, dependencies, and architecture decisions.

**Invoke when:**
- Adding a new dependency (checks vendor jurisdiction, CLOUD Act exposure)
- Adding a new cloud service / SaaS integration
- Architectural decision involving data residency
- Pre-release audit before public deployment
- ADR involves cross-jurisdiction concerns

**Reviews against:**
- Vendor jurisdiction matrix (US / EU / Other)
- Data residency mapping
- CLOUD Act exposure paths
- Encryption in transit + at rest
- Audit log completeness
- Compliance terminology accuracy

**Does NOT replace** the standard `reviewer` (which remains the terminal node for code quality). Sovereignty-auditor runs in parallel — its verdict feeds into the reviewer's final decision.

**Output format:**
```markdown
## Sovereignty Audit: <work item>

### Jurisdiction analysis
- New US-resident dependencies: [list, or "none"]
- Data flows to US infrastructure: [list, or "none"]
- CLOUD Act exposure: [low/medium/high + rationale]

### Compliance check
- DORA terminology: [accurate / needs revision]
- GDPR data flow: [documented / missing]
- NIS2 considerations: [N/A / addressed]

### Verdict
- 🟢 GREEN: No sovereignty concerns
- 🟡 YELLOW: Concerns, mitigations recommended (see below)
- 🔴 RED: Cannot proceed without ADR documenting trade-off

### Recommended actions
[list]
```

## Routing additions

When sovereignty profile is active, add to the routing table in `CLAUDE.md`:

| Request shape | Route to |
|---|---|
| New dependency (NuGet, npm, Docker image) | `security-expert` + `sovereignty-auditor` in parallel |
| New cloud service integration | `architect` + `sovereignty-auditor` |
| Data flow change (storing personal data) | `architect` + `sovereignty-auditor` + `security-expert` |
| Production release prep | `reviewer` + `sovereignty-auditor` audit before tagging |

## Skill router additions

Add to `.claude/skill-router.json` when this profile is active:

```json
{
  "pattern": "(?i)\\b(sovereignty|cloud act|dora|gdpr|nis2|eu[- ]residency|data[- ]residency|cross[- ]border|patriot act|export control|trade compliance|sovereign[- ]compute)\\b",
  "skill": "sovereignty-audit"
}
```

(Then create `.claude/skills/sovereignty-audit/SKILL.md` with the audit checklist.)

## Statusline indicator

When this profile is active, the statusline should show an indicator. Suggested addition to `statusline.py`:

```python
def fmt_profile(active_profiles: list[str]) -> str:
    """Show profile indicator in statusline."""
    if "sovereignty" in active_profiles:
        return RED("🇪🇺 sov")  # or BLUE
    return ""
```

(Implementation left as exercise — read `profiles-active.md` from project root.)

## Review cycle

This profile should be reviewed every **12 months** against:
- Anthropic CLOUD Act policy changes
- EU regulatory landscape (DORA / NIS2 evolution)
- Vendor jurisdiction shifts (acquisitions, ownership changes)
- Local LLM maturity (Ollama models competitive with cloud?)

Next review: 2027-05-11.

## When NOT to activate this profile

Don't activate sovereignty profile for:
- OSS libraries with no customer data (Picea libraries)
- Personal experiments / learning projects
- Public-facing demos / marketing material
- Anything where convenience > compliance

This profile adds operational overhead. Activate only when the project genuinely operates under sovereignty constraints.

## References

- Dutch art. 7 Auteurswet (IP ownership in employment context)
- EU CLOUD Act mitigation patterns
- DORA: Regulation (EU) 2022/2554
- NIS2: Directive (EU) 2022/2555
- GDPR: Regulation (EU) 2016/679
- Soveritas thesis docs (in project)
