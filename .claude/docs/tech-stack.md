# Tech Stack

The concrete tools and versions this project uses. Subagents read this so their advice and code match reality. Update it whenever the team adopts new technology, and the change will propagate through the squad.

This file was derived by inspecting the actual repository (`Picea.Abies.sln`, `global.json`, each `*.csproj`, `.github/workflows/`, `.zap/`, `.semgrep/`, `.gitleaks.toml`, `trivy.yaml`) on 2026-09-02, not inherited from a charter. Where a claim could not be verified against the source, it's marked so below rather than asserted.

---

## Language & Runtime

- **Language:** C# 14 (`<LangVersion>latest</LangVersion>` in `Directory.Build.props`, `<EnablePreviewFeatures>true</EnablePreviewFeatures>`). **Verified.**
- **Target framework:** `net10.0` across almost every project; `net10.0-desktop` / `net10.0-windows10.0.26100` for the Uno-based `Picea.Abies.WinUI` head; `netstandard2.0` for `Picea.Abies.Analyzers` (Roslyn analyzers ship netstandard2.0 by ecosystem convention); `browser-wasm` RID for `Picea.Abies.Benchmark.Wasm`. **Verified** by grepping every `.csproj`.
- **SDK:** pinned via `global.json` — `"version": "10.0.103"`, `rollForward: latestMajor`. Also declares `msbuild-sdks: { "Uno.Sdk": "6.6.33" }` (see WinUI/Native below) and `"test": { "runner": "Microsoft.Testing.Platform" }`. **Verified.**
- **Project namespace root:** `Picea.Abies`. **Verified** — every project's root namespace and every `PackageId` follows this prefix (`Picea.Abies`, `Picea.Abies.Conduit.*`, `Picea.Abies.Native`, etc.).
- **Nullable reference types:** enabled repo-wide (`Directory.Build.props`: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`). **Verified.**
- **NuGet auditing:** `Directory.Build.props` sets `<NuGetAudit>true</NuGetAudit>`, `<NuGetAuditMode>all</NuGetAuditMode>`, `<NuGetAuditLevel>moderate</NuGetAuditLevel>` — vulnerable transitive packages fail the build, not just direct ones. **Verified.**
- **No `Directory.Packages.props`.** Central Package Management is **not** in use — every project pins its own `PackageReference` versions individually. **Verified** (file does not exist at repo root).

## Functional Programming Primitives

- **`Result<T, TError>` and `Option<T>`:** **not defined in this repository.** They live in the external `Picea` NuGet package (namespace `Picea`), the Mealy-machine kernel Abies is built on (source at the sibling repo `~/dev/picea/Picea`, restored from NuGet as `Picea` — versions observed in use: `1.0.0` in `Picea.Abies.csproj`, `1.0.27-rc-0002` in `Picea.Abies.Conduit.ReadStore.PostgreSQL.csproj`). **Verified** by reading `Result.cs`/`Option.cs` in the restored package cache and grepping for `namespace` declarations. Do not assume Abies defines its own Result/Option types — it consumes them from `Picea`, and version drift between projects (`1.0.0` vs `1.0.27-rc-0002`) is a real, currently-unreconciled fact, not a typo.
- **`Unit`:** aliased globally to `System.ValueTuple` (`global using Unit = System.ValueTuple;` in `Global/Usings.cs`), not a project-defined type. **Verified.**

## Frontend

- **Approach:** Vanilla JavaScript for the browser/WASM interop layer (`Picea.Abies.Browser/wwwroot/abies.js`, `abies-otel.js`, `debugger.js`) plus a full client-side Blazor-WASM-shaped .NET runtime (`Picea.Abies.Conduit.Wasm`, `Picea.Abies.Counter.Wasm`) via `RuntimeIdentifier=browser-wasm`. **Verified.**
- **Module loading:** ES modules loaded natively via `<script type="module">`; no bundler/build step for the JS layer itself. **Verified** by inspecting `Picea.Abies.Browser`'s packed `wwwroot/*.js` and its `contentFiles` packaging.
- **Native desktop:** `Picea.Abies.Native` (platform-neutral patch interpreter, own NuGet package) + `Picea.Abies.WinUI` (real WinUI 3 controls) built on the **Uno.Sdk** — `net10.0-desktop` (Skia renderer, builds on Linux/macOS/Windows) and, Windows-only, `net10.0-windows10.0.26100` (real Windows App SDK head). **Verified** by reading both `.csproj` files; this is Abies-specific and not present in the squad-template default stack.

## Testing

- **Test framework:** TUnit only — **verified** (`TUnit` `1.19.57` referenced consistently across all 16 test projects; zero hits for `xunit`/`NUnit`/`MSTest` anywhere in the tree).
- **Test runner:** Microsoft.Testing.Platform (declared in `global.json`'s `"test"` block), not `dotnet test`'s legacy VSTest path. **Verified.**
- **Browser/E2E testing:** `Microsoft.Playwright` `1.58.0` used directly (in `Picea.Abies.Conduit.Testing.E2E` and others), **not** the `TUnit.Playwright` integration package — that package is not referenced anywhere in this repo. **Verified.** If a doc or agent charter says "TUnit + Playwright via TUnit.Playwright," that's the squad-template default, not what Abies actually does — correct it to "TUnit tests calling `Microsoft.Playwright` directly" when writing about this repo.
- **Integration test fixture — mixed, not purely Aspire.** `Picea.Abies.Conduit.Testing.E2E` starts the real stack (KurrentDB + PostgreSQL + Conduit API) via `Aspire.Hosting.Testing`'s `DistributedApplicationTestingBuilder`, matching the squad-template convention. Alongside it, `Picea.Abies.Conduit.Api.Tests/ConduitApiFactory.cs` (a `WebApplicationFactory<Program>` subclass) and `Picea.Abies.Server.Kestrel.Tests/EndpointTests.cs`/`OtlpProxyEndpointTests.cs` (an in-process `TestServer` via `WebApplication.CreateBuilder()` + `.UseTestServer()`) use in-memory fakes in place of KurrentDB/PostgreSQL, to avoid container startup cost for fast integration tests. **Verified by reading all files.** **Resolved 2026-09-02:** what was previously flagged here as a live, unreconciled contradiction with the "No `WebApplicationFactory`" line in `.claude/docs/decisions.md`'s Testing section has been resolved by explicit user decision — the Testing section's "Aspire AppHost Is the Test Fixture" entry now documents this as a named, closed exception (fast in-memory tests for single-service HTTP-pipeline verification) rather than a drift. See that entry for the full policy and the exact list of files it covers.
- **Property-based testing:** **not present.** No `FsCheck` reference anywhere in the repo — do not assume it's available; if a workflow needs it, it's a new dependency requiring the standard approval flow.
- **Benchmarking:** BenchmarkDotNet `0.15.8`, `[MemoryDiagnoser]` applied on every benchmark class observed (`Picea.Abies.Benchmarks/*.cs`). **Verified.**
- **Load testing:** **not present.** No k6 or NBomber scripts found in the repo.
- **Visual regression:** `Picea.Abies.Testing.Visual` (snapshot capture) paired with `Picea.Abies.Cli` (packed as the `abies` global tool — `dotnet tool install -g Picea.Abies.Cli`) for reviewing/accepting/pruning baselines. **Verified**, Abies-specific, no squad-template equivalent.

## Data & Persistence

- **Event store:** KurrentDB, via `Picea.Glauca` (`0.1.14`) and `Picea.Glauca.KurrentDB` (event-sourcing kernel companion to `Picea`), orchestrated locally through `CommunityToolkit.Aspire.Hosting.KurrentDB` (`13.4.0`) in the AppHost. **Verified.**
- **Read store:** PostgreSQL, accessed with **raw Npgsql `9.0.3`** — no EF Core, no `Microsoft.EntityFrameworkCore.*` reference anywhere in the tree, no `Migrations/` directories. **Verified.** `Picea.Abies.Conduit.ReadStore.PostgreSQL` hand-writes denormalized CQRS read models and projections. Do not route schema-change work to "EF migrations" conventions — there are none; read-store schema changes are plain SQL/Npgsql code, owned by `csharp-dev`.

## Observability

- **Tracing:** OpenTelemetry, `AddOtlpExporter()` wired in `Picea.Abies.Conduit.ServiceDefaults/Extensions.cs` and `Picea.Abies.Conduit.Api/Program.cs`. **Verified.**
- **Local sink:** Aspire dashboard (OTLP), with `AddConsoleExporter()` also configured as a default trace sink per generated-template convention (see `.claude/docs/decisions.md` Session Decisions, 2026-03-26). **Verified in ServiceDefaults; template default is a decision-log claim, not independently re-verified against every template's `Program.cs` in this pass.**
- **Browser-side OTEL exporter:** `@opentelemetry/exporter-trace-otlp-proto`, loaded from CDN with pinned versions per the 2026-03-27 decisions above. **Not independently re-verified in this pass** — carried forward from `.squad/decisions.md`; if you touch browser OTEL bootstrap code, confirm the CDN pins are still what's shipped before relying on this line.

## Hosting & Orchestration

- **Local:** .NET Aspire AppHost — `Picea.Abies.Conduit.AppHost` (`Aspire.AppHost.Sdk/13.2.0`), hosting Postgres (`Aspire.Hosting.PostgreSQL` `13.4.6`) and KurrentDB, launching `Picea.Abies.Conduit.Server` and `Picea.Abies.Conduit.Wasm.Host`. **Verified** by reading the AppHost `.csproj`.
- **Service defaults:** `Picea.Abies.Conduit.ServiceDefaults` project, called from server-hosted entry points. **Verified.**
- **Production hosting:** **not determinable from the repo** — no Dockerfile, `docker-compose.yml`, or `.dockerignore` was found at the repo root or in any project directory searched. If containerized deployment exists, it isn't checked into this repo as of this migration; don't assert an image base or registry.

## Security Toolchain

All entries in this section are **verified** by reading the actual config files, not assumed from the squad-template default.

- **SAST:** Semgrep, with **repo-specific rule packs** at `.semgrep/rules/conduit-security.yml` and `.semgrep/rules/template-security.yml` (not just the default ruleset). CodeQL also runs (`.github/workflows/codeql.yml`).
- **SCA:** `dotnet list package --vulnerable` plus build-time NuGet Audit (see above); Dependabot config not inspected in this pass.
- **Secrets detection:** Gitleaks, extending the built-in default ruleset (`.gitleaks.toml`: `[extend] useDefault = true`), with an allowlist carved out for `benchmark-results/`, `BenchmarkDotNet.Artifacts/`, `test-results/`, `TestResults/`. Also `.github/workflows/secrets-scan.yml`.
- **DAST:** OWASP ZAP, with two distinct target profiles under `.zap/`: `apphost-auth-policy.conf`/`apphost-auth-targets.txt` and `full-scan-policy.conf`/`full-scan-targets.txt`, run via `.github/workflows/zap-baseline.yml` (PR-scoped) and `.github/workflows/zap-nightly.yml` (full nightly scan) — matching the 2026-04-01 "PR gating matrix realignment" decision that moved heavy DAST off the PR-blocking path.
- **Container scanning:** Trivy, configured via `trivy.yaml` at the repo root — `severity: [HIGH, CRITICAL]`, `ignore-unfixed: true`, scanners `[vuln, misconfig, secret]`, with `benchmark-results/`, `BenchmarkDotNet.Artifacts/`, `test-results/`, `TestResults/`, `js-framework-benchmark/`, `**/bin`, `**/obj`, `**/.git` excluded. No Dockerfile was found, so this is presumably scanning the filesystem/dependency surface rather than a container image — **not independently confirmed** which Trivy scan mode CI actually invokes.
- **Local development secrets:** not independently verified in this pass (no `dotnet user-secrets` usage grepped for).

## CI/CD

- **CI provider:** GitHub Actions. **Verified** — workflows live in `.github/workflows/`, including `benchmark.yml`, `cd.yml`, `claude-hooks-tests.yml`, `codeql.yml`, `docs-snippets.yml`, `e2e.yml`, `pr-conduit-integration.yml`, `pr-validation.yml`, `release.yml`, `secrets-scan.yml`, `semgrep.yml`, `template-security.yml`, `trivy.yml`, `visual-regression.yml`, `zap-baseline.yml`, `zap-nightly.yml`. This list is a point-in-time inventory, not a count to keep in sync by hand — run `ls .github/workflows/*.yml | wc -l` for the current total rather than trusting a number written here.
- **Squad-aware automation:** removed. The label-driven GitHub Actions integration (`squad-heartbeat.yml`, `squad-issue-assign.yml`, `squad-triage.yml`, `sync-squad-labels.yml`) referenced the old `squad:<agent>` issue-label scheme and was deleted as part of the `.claude/`-based agent migration rather than updated to the new subagent names (`csharp-dev`, etc.). If issue-label-driven agent assignment is wanted again, it needs to be redesigned against the current roster, not restored from these files.
- **Container registry:** **not determinable** — no Dockerfile/registry push step located in this pass.
- **Versioning:** Nerdbank.GitVersioning (`Directory.Build.props`: `PackageReference Include="Nerdbank.GitVersioning" Version="3.9.50"`, driven by `version.json` at repo root, currently `"version": "2.4"`). **Verified** — this is more specific than squad-template's generic "Semantic versioning from git tags" line; prefer this description for Abies.
- **Branch protection:** `main` protected, PRs only — **stated per team convention** (see `.claude/docs/decisions.md` Git Workflow); not independently checked against GitHub branch protection API in this pass.

## Documentation

- **Format:** Markdown only. **Verified** — `docs/` contains `adr/`, `api/`, `concepts/`, `getting-started/`, `guides/`, `investigations/`, `migration/`, `reference/`, `research/`, `security/`, `tutorials/`, all `.md`.
- **Framework:** Diátaxis (tutorial / how-to / reference / explanation) — matches `docs/` top-level structure (`getting-started`, `guides`, `reference`, `concepts`).
- **ADRs:** `/docs/adr/`, sequentially numbered, currently up to at least `ADR-028` (`ADR-027-native-winui-renderer.md`, `ADR-028-program-core-view-split.md` referenced from `README.md`, plus `ADR-000` through `ADR-018` listed directly). **Verified** the directory and numbering scheme exist; did not read every ADR's content in this pass.
- **Threat model:** `docs/security/threat-model.md` exists, alongside `docs/security/hardening-backlog.md`. **Verified present** — content currency not re-audited in this pass.
- **Changelog:** `CHANGELOG.md` at repo root. **Verified present**, format not re-checked against Keep a Changelog in this pass.

## MCP Servers

- **Browser inspection:** Playwright MCP (preferred over curl/wget for any rendered-output verification) — carried forward from squad-template as a tooling convention, not a repo artifact to verify.

---

## How to Update This File

When the team adopts new technology:
1. Discuss it with the Architect (often via a full Beast Mode cycle if it's a framework-level choice).
2. If approved, the Architect or DevOps writes a decision into `.squad/decisions/inbox/` describing the change.
3. The Tech Writer updates this file in the same PR that introduces the new tooling.
4. The Reviewer verifies that this file matches the actual project configuration during review.
