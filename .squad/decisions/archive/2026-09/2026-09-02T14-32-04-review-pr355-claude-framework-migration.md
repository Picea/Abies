---
id: reviewer-20260902T144500Z-pr355-hooks
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-02T14:45:00Z
targets:
  - path: .claude/hooks/
    lines: "all 11 scripts"
  - path: .claude/settings.json
    lines: "1-111"
  - path: .claude/skill-router.json
    lines: "1-74"
  - path: .claude/hooks/tests/
    lines: "run.sh, agent_identity.py, 20 fixtures"
blockers:
  - file: .claude/settings.json
    line: 16
    reason: "statusLine command points at .claude/statusline.py, which does not exist in the repo. With refreshInterval 10 this fails every 10s, and the .squad/.last-review-verdict and .squad/.hooks-ok writes in three hooks have no consumer."
  - file: .claude/hooks/enforce-conventional-commits.sh
    line: 47
    reason: "Greedy sed extraction takes the LAST -m, so the standard two-flag form git commit -m <subject> -m <body> validates the BODY against the subject pattern and is blocked. Verified live."
  - file: .claude/hooks/enforce-conventional-commits.sh
    line: 29
    reason: "All four PreToolUse hooks gate on a bare substring match for git commit in raw command text. Verified false positive (a non-commit command containing that text was blocked) and false negative (git -C <path> commit bypasses all four gates). This is the exact defect class the PR says it avoided by not installing block-direct-commits-to-main.sh and validate-branch-name.sh."
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 217
    reason: "Nested-key promotion lets any drop forge agent reviewer and verdict PASS. Verified end-to-end: a csharp-dev NEEDS-CHANGES drop with a nested meta block was archived as reviewer PASS and wrote PASS to .last-review-verdict, also bypassing the verdict-vs-blockers consistency check. Known bug per the in-code TODO; needs explicit user sign-off or a fix before shipping."
  - file: .github/agents/squad.agent.md
    line: 73
    reason: "Workflow removal is partial. This live Copilot agent definition still directs a coordinator to .squad/team.md, .squad/routing.md, .squad/agents/*/charter.md and the four deleted workflows, all removed by this PR. .gitattributes line 3 also still references .squad/agents/*/history.md."
  - file: .claude/docs/decisions.md
    line: 254
    reason: "States branch naming is mechanically enforced by the validate-branch-name hook in .claude/hooks/, which this PR deliberately does not install. git-advanced and gh-cli SKILL.md make the same claim for block-direct-commits-to-main.sh. A false claim about an enforcement gate in the authoritative conventions doc."
high:
  - file: .github/workflows/
    reason: "No CI job runs .claude/hooks/tests/run.sh, despite the suite's own header assuming a paths-scoped CI job. 67 regression assertions with zero automated execution."
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "Uses the deprecated gitleaks protect --staged while .githooks/pre-commit in the same repo already uses the current gitleaks git --staged. On a gitleaks major bump the unknown-subcommand exit lands in the catch-all branch and blocks every commit with a misleading malformed .gitleaks.toml message."
  - file: .claude/hooks/enforce-gpg-signing.sh
    reason: "Lines 215-219 hardcode one contributor's GPG key id and personal email as the reference setup in a tracked file in a public repo. Any other contributor copy-pasting it configures a key they do not hold, and check 5 then blocks all their commits."
  - file: .claude/hooks/dotnet-format-on-save.sh
    reason: "Claims .csproj/.props/.targets are in scope; dotnet format does not touch MSBuild XML (verified). Each such edit pays a full 48-project workspace load (12.8s warm) for zero effect, and rapid successive edits stack concurrent workspace loads."
  - file: .claude/settings.json
    reason: "Enables CLAUDE_CODE_ENABLE_TELEMETRY and the enhanced-telemetry beta for every contributor via a tracked file, with OTLP gRPC and no endpoint, defaulting to localhost:4317 - the port Aspire's dashboard binds. Belongs in the gitignored settings.local.json."
  - file: .squad/decisions/inbox/
    reason: "Five pre-schema drops carried over from main are inlined wholesale (171 lines) into decisions.md by the merger's LEGACY path on the first SubagentStop, unreviewed. Observed live in this working tree during the review."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Header says entries land under the Session Decisions anchor; the code appends at EOF and drop bodies carry their own H2 headings, so the last H2 in decisions.md is now an unrelated Guardrails from a prior drop."
medium:
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "Missing --redact (the sibling .githooks/pre-commit passes it); config_arg is unquoted at line 94 so a project path with a space breaks the flag; duplicates coverage the git pre-commit hook already provides for all commits."
  - file: .claude/hooks/squad-rotate.py
    reason: "Dead code: unused mode at line 87, a .gitkeep guard inside a *.md glob at line 172. precompact-snapshot.py line 130 assigns an unused today."
  - file: .github/workflows/pr-validation.yml
    reason: "isMaintenancePath and the NON_DOCS filters still allowlist .squad/ but not .claude/, where framework prose now lives."
good:
  - file: .claude/hooks/tests/run.sh
    reason: "67 of 67 pass in 1.1s. Coverage is real, not decorative: the pre-fix fixtures are sha256-pinned and asserted to quarantine for their NAMED reason rather than any reason, the roster invariant has a working negative control, and a decisions.md content grep closes the archived-but-never-appended gap."
  - file: .claude/hooks/session-context-loader.py
    reason: "All five python hooks verified to exit 0 on empty and malformed stdin; none can wedge a session."
  - file: .gitignore
    reason: "Correctly ignores the precompact transcript copies, snapshots and per-machine caches, which would otherwise commit full conversation transcripts."
references: []
---

## Findings

See the reviewer's detailed report for the full write-up. Verdict is
NEEDS-CHANGES on six blockers, the most serious being a verified forgery
path through the decision-drop validator that lets any agent record a
Reviewer PASS it never earned.
