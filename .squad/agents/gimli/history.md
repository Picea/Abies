# Gimli — DevOps Engineer

## Project Context
- User: Maurice Cornelius Gerardus Petrus Peters
- Project: Abies (Tolkien legendarium theme)
- Created: 2026-03-19

## Learnings

- Integrated terminal safety protocol: use redirection (`> file.log 2>&1`) for long-running commands and avoid piping through `head`, `tail`, `tee`, or `grep`.
- Keep shell commands simple and atomic in VS Code terminal to avoid signal/quoting-induced failures.
- Performance workflow guardrail: benchmark conclusions require js-framework-benchmark validation, not micro-benchmark wins alone.
- PR CI gate alignment: ensure build, lint (`dotnet format --verify-no-changes`), tests, and E2E checks are present and green before merge.
- For PR formatting corrections, prefer scoped `dotnet format ... --include` to avoid repository-wide reformat noise.
