# DevOps / Infrastructure Engineer — History

## About This File
Pipeline decisions, container configs, deployment patterns, and CI optimization. Read this before every session.

## Pipeline Configuration
*None yet — workflow structure, caching strategy, stage ordering tracked here.*

## Container Images
| Image | Base | Size | Last Optimized |
|---|---|---|---|
| *None yet* | | | |

## Deployment Topology
*None yet — environment descriptions, deployment targets, parity notes.*

## CI Failures Investigated
| Date | Failure | Root Cause | Fix |
|---|---|---|---|
| *None yet* | | | |

## Release Process
*None yet — versioning strategy, release flow, automation status.*

## Environment Gotchas
*None yet — environment-specific issues and workarounds.*

## Learnings
- 2026-04-19: PR title validation in `.github/workflows/pr-validation.yml` enforces Conventional Commits and requires the subject to start with an uppercase letter (`subjectPattern: ^[A-Z].+$`). Titles that otherwise look valid can fail on lowercase subjects.
- 2026-04-19: When Scribe merges inbox decisions into `.squad/decisions.md`, follow-up hygiene should include checking agent history files for references to deleted `.squad/decisions/inbox/*.md` files and repointing to canonical entries in `.squad/decisions.md`.
