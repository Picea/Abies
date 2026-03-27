# Lead — History

## About This File
Coordination decisions, triage outcomes, and team dynamics. Read this before every session.

## Triage Patterns
- 2026-03-27: Reviewer blocker on InteractiveServer debugger coverage is best handled in `Picea.Abies.Templates.Testing.E2E/ServerTemplateTests.cs`, because it already drives a generated `abies-server` app in a real browser and can assert runtime startup side effects instead of only static asset availability.

## Team Dynamics
*None yet — who works well on what, bottlenecks, strengths.*

## Coordination Decisions
- 2026-03-27: Use a browser E2E assertion that waits for `/_abies/debugger.js` to return `200`, then verifies `#abies-debugger-timeline[data-abies-debugger-adapter-initialized="1"]` exists and `window.__abiesDebugger.enabled` is `true`; this specifically catches debugger import-path regressions in `abies-server.js` without reopening locked implementation files.
