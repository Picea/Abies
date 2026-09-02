---
name: cicd-pipelines
description: CI/CD reference — pipeline structure (build → test → security → benchmark → package → deploy), GitHub Actions patterns, Dockerfile multi-stage standards, environment parity rules (Local/CI/Staging/Production), release automation (semantic versioning, changelog generation, image tagging), and `dotnet new` template CI/CD scaffolding. Use when building or modifying GitHub Actions workflows, writing Dockerfiles, designing release flows, or scaffolding CI for new templates.
---

# CI/CD Pipeline Reference

The DevOps Engineer's deep reference. Role and protocol live in `.claude/agents/devops.md`. The squad's principles around "infrastructure is code", environment parity, and pipeline-is-the-quality-gate live in `.claude/docs/decisions.md`.

---

## Pipeline Structure

The standard pipeline shape, applied to every service:

```
┌──────────────────────────────────────────────────────────────────────────┐
│                              CI Pipeline                                 │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌────────────┐  ┌───────────┐  ┌─────────┐  │
│  │  Build   │→ │  Tests   │→ │  Security  │→ │  Bench    │→ │  Pack   │  │
│  └──────────┘  └──────────┘  └────────────┘  └───────────┘  └─────────┘  │
│       │              │              │              │            │       │
│  Restore         Unit + Int    SAST/SCA       BenchmarkDotNet  Container │
│  Build           E2E (Aspire)  Secrets/DAST   k6/NBomber       Image    │
│  Lint            Coverage      Container scan baseline check   Push     │
└──────────────────────────────────────────────────────────────────────────┘
                                                                  │
                                                                  ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                             CD Pipeline                                  │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌────────────┐  ┌───────────┐               │
│  │ Staging  │→ │ Smoke    │→ │ Production │→ │ Verify    │               │
│  └──────────┘  └──────────┘  └────────────┘  └───────────┘               │
│       │              │              │              │                    │
│  Deploy         Health/E2E    Canary deploy   OTEL traces                │
│  Migrate        OTEL trace    Progressive     SLO check                  │
│  Smoke gate     verification  rollout         Auto-rollback              │
└──────────────────────────────────────────────────────────────────────────┘
```

### Stages, in detail

| Stage | What runs | Fail-fast? | Owner |
|---|---|---|---|
| **Build** | `dotnet restore`, `dotnet build` (with `TreatWarningsAsErrors`), Roslyn analyzers | yes | devops |
| **Tests** | `dotnet test` — unit + integration + E2E via Aspire `DistributedApplicationTestingBuilder` | yes | csharp-dev / js-dev (content), devops (job structure) |
| **Security** | SAST (Semgrep, DevSkim), SCA (`dotnet list package --vulnerable`, OWASP Dependency-Check), secrets (Gitleaks), DAST (ZAP baseline scan against AppHost), container scan (Trivy on the built image) | block on Critical/High | security-expert (content), devops (job structure) |
| **Benchmark** | BenchmarkDotNet against baseline; load tests (k6/NBomber) on PRs touching hot paths | warn on regression > threshold; block on critical regressions | performance-engineer (content), devops (job structure) |
| **Pack** | Build container image, sign, push to registry; tag with commit SHA + semver if release | yes | devops |
| **Deploy (staging)** | Deploy to staging environment; run migrations | yes | devops |
| **Smoke** | Health checks, E2E smoke tests against staging | yes — block production deploy on failure | devops |
| **Deploy (prod)** | Canary or blue-green deploy to production; run migrations carefully | progressive — auto-rollback on SLO breach | devops |
| **Verify** | OTEL traces visible, SLOs holding, error rate within bounds | auto-rollback if not | devops |

---

## Pipeline Rules

### Cache Aggressively

- NuGet packages cached by `packages.lock.json` hash.
- Docker layers cached for build stages that don't change frequently.
- BenchmarkDotNet baseline artifacts cached across runs.

### Fail Fast

The first failed stage stops the pipeline. Don't run benchmarks if tests fail. Don't push images if security scans fail.

### Parallel Where Possible

Within a stage, jobs that don't depend on each other run in parallel:
- SAST, SCA, and secrets detection are three independent jobs.
- Unit tests and integration tests can be split into separate jobs.
- BenchmarkDotNet and k6 load tests are independent of each other.

### Immutable Artifacts

The image built in CI is **the same image** deployed to staging and production. No rebuild. No "promote from staging by rebuilding from main."

### Secrets

Never inline. Always pulled from the platform's secret store (GitHub Actions secrets, Azure Key Vault, AWS Secrets Manager). Local dev uses `dotnet user-secrets`.

---

## Dockerfile Standards

Multi-stage builds. Small final images. Non-root user. No build tools in the final stage.

```dockerfile
# syntax=docker/dockerfile:1.7
ARG DOTNET_VERSION=10.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy csproj/Directory.Packages.props files first to leverage Docker layer caching for restore
COPY ["Directory.Build.props", "Directory.Packages.props", "Directory.Packages.props.user", "global.json", "./"]
COPY ["src/<Root>.SomeService/<Root>.SomeService.csproj", "src/<Root>.SomeService/"]
COPY ["src/<Root>.ServiceDefaults/<Root>.ServiceDefaults.csproj", "src/<Root>.ServiceDefaults/"]
RUN dotnet restore "src/<Root>.SomeService/<Root>.SomeService.csproj"

# Copy the rest and publish
COPY . .
WORKDIR /src/src/<Root>.SomeService
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

# Run as non-root
RUN groupadd --system --gid 1000 app \
 && useradd --uid 1000 --gid app --no-create-home --shell /usr/sbin/nologin app
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=false

EXPOSE 8080

# Aspire health check endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "<Root>.SomeService.dll"]
```

**Rules:**
- **Pinned base images.** `mcr.microsoft.com/dotnet/aspnet:10.0` — not `:latest`. Patch version handled by Dependabot updates.
- **Non-root user in final stage.** UID/GID 1000 by convention.
- **No `apt-get install` in the final stage** unless the runtime genuinely needs an OS package (and document why).
- **Health check defined.** Aspire's `/health` endpoint is the target.
- **Layer ordering for cache hits:** csproj files → restore → source → publish. Source changes don't bust the restore layer.
- **`.dockerignore`** excludes `bin/`, `obj/`, `.git/`, `.vs/`, `node_modules/`, `*.md`, test data.

### Anti-patterns

- ❌ Running as root in the final stage.
- ❌ `COPY . .` before `dotnet restore` (every code change busts the restore cache).
- ❌ `latest` tag on base images.
- ❌ `dotnet publish` in the final stage (drags the SDK into production).
- ❌ Storing secrets in `ENV` instructions.

---

## Environment Strategy

| Environment | What's there | Differs from prod by | Owner |
|---|---|---|---|
| **Local** | Aspire AppHost; emulators or in-process for external deps | Resource scale, mock external services, debug logging | individual developer |
| **CI** | Aspire AppHost runs in the test job for integration/E2E | Single CI runner, ephemeral, mocked external services | devops |
| **Staging** | Same image as production, real external services (or staging instances), real DB schema | Lower scale, lower cost | devops |
| **Production** | The deployed image, real services, real users | (the target) | devops |

**The parity invariant:** Local, CI, staging, and production all run the same containers with the same configs. They differ only in **scale and secrets**, never in **code or behavior**.

When something works in local but breaks in CI, the bug is parity — not "CI is weird." Investigate and close the parity gap.

---

## Release Automation

### Versioning via Nerdbank.GitVersioning

The squad uses **Nerdbank.GitVersioning** (`nbgv`) for version derivation. The `Nerdbank.GitVersioning` package is referenced from `Directory.Build.props` with `PrivateAssets="all"`, so every project in the solution gets versioning automatically.

**Source of truth:** `version.json` at the repo root.

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/main/src/NerdBank.GitVersioning/version.schema.json",
  "version": "1.4",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/heads/v\\d+(?:\\.\\d+)?$"
  ],
  "cloudBuild": {
    "buildNumber": { "enabled": true },
    "setVersionVariables": true
  }
}
```

**How the version is computed:**
- Major.Minor comes from `version.json` (`"1.4"`).
- Patch (`.Build`) comes from the git height — the count of commits since `version.json` last changed its `version` field.
- A revision component encodes the commit (truncated SHA).
- On a **public release ref** (matching `publicReleaseRefSpec` above — `main` and `v<X>[.<Y>]` branches), the version is "stable" (no prerelease tag). Otherwise it's a prerelease (`-alpha`, `-beta`, etc., depending on `version.json` config).

**Net effect:** every commit produces a deterministic, monotonically-increasing version. No CI-side version computation needed.

**Bumping major or minor:** edit `version.json` and commit. The git height resets; subsequent commits start at `.0`, `.1`, ... again. Conventional commits are still used for **changelog generation**, just not for version computation.

```bash
# Inspect the version that will be produced for the current HEAD
dotnet tool install -g nbgv
nbgv get-version

# Show all derived properties
nbgv get-version --format json
```

The Conventional Commit type still drives **whether to release** and **what to put in the changelog**, but the version number is what nbgv computes — not what a parser derives from `feat:` vs `fix:`.

### Release Flow

```
PR merged to main
        │
        ▼
CI runs full pipeline on main
        │
        ▼ (passes)
Release workflow triggered
        │
        ├── nbgv get-version  →  V := computed version (e.g. 1.4.42)
        ├── Generate CHANGELOG entries from commits since last tag
        │     (moves [Unreleased] → [V])
        ├── Tag git commit with vV
        ├── Push image with tags: {sha}, vV, V.Major.Minor, V.Major
        ├── Deploy to staging
        ├── Run smoke tests
        ├── Deploy to production (canary or blue-green)
        └── Verify SLOs, auto-rollback if breached
```

**Why this layout works.** The version is already in the artefact (the assembly's `InformationalVersion` carries it; `nbgv` injects it at build time). The release workflow is just observing what `nbgv` already decided, not deciding itself. That makes the build idempotent: the same commit always produces the same version regardless of when or where it's built.

### Image Tagging

Every image gets multiple tags so callers can pin at the level they want:
- `{commit-sha}` — immutable, exact identity
- `v1.4.42` — exact version (matches the git tag)
- `v1.4` — latest patch in the 1.4 minor line
- `v1` — latest minor in the 1.x major line
- `latest` — only on the most recent release (use sparingly; rarely the right choice for production references)

The tags after `{commit-sha}` are mutable pointers that the release workflow updates on each release.

---

## `dotnet new` Template CI/CD Requirements

Every template the squad ships includes a working CI/CD setup out of the box:

- `.github/workflows/ci.yml` — full build/test/security/benchmark pipeline
- `.github/workflows/release.yml` — semantic-versioning + changelog + image push
- `Dockerfile` — multi-stage, non-root, health check
- `.dockerignore` — sensible exclusions
- `Directory.Build.props` with `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, security analyzer references
- `Directory.Packages.props` with central package version management
- `global.json` pinning the .NET SDK
- README with build/run instructions
- `.gitleaks.toml`, `.semgrep/` directory with project rules
- Pre-commit hook installer for Gitleaks

A template that ships without these is incomplete. The Tech Writer reviews template scaffolding for completeness during the template's review.

---

## GitHub Actions Patterns

### Standard build/test job

```yaml
name: CI
on:
  pull_request:
  push:
    branches: [main]

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Restore
        run: dotnet restore --locked-mode

      - name: Build
        run: dotnet build --configuration Release --no-restore /p:TreatWarningsAsErrors=true

      - name: Test
        run: dotnet test --configuration Release --no-build --logger "trx" --collect:"XPlat Code Coverage"

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/TestResults/*.trx'
```

### Security stage (combined)

```yaml
  security:
    needs: build-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0   # Gitleaks needs history

      - name: Secrets — Gitleaks
        uses: gitleaks/gitleaks-action@v2

      - name: SAST — Semgrep
        uses: returntocorp/semgrep-action@v1
        with:
          config: |
            p/csharp
            p/security-audit
            .semgrep/

      - name: SCA — vulnerable packages
        run: dotnet list package --vulnerable --include-transitive | tee vuln.txt
      - name: Fail on Critical/High
        run: |
          if grep -E "(Critical|High)" vuln.txt; then
            echo "Critical or High vulnerabilities found"; exit 1
          fi
```

### Benchmark gate

```yaml
  benchmark:
    needs: build-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4

      - name: Run benchmarks
        run: dotnet run --project tests/<Root>.Benchmarks -c Release -- --filter '*' --exporters json

      - name: Compare against baseline
        uses: benchmark-action/github-action-benchmark@v1
        with:
          tool: 'benchmarkdotnet'
          output-file-path: 'BenchmarkDotNet.Artifacts/results/*-report-full-compressed.json'
          alert-threshold: '120%'
          fail-on-alert: true
          comment-on-alert: true
          github-token: ${{ secrets.GITHUB_TOKEN }}
```

---

## What This Skill Does NOT Cover

- **The decision of *what* to put in CI** — driven by the team's principles. This skill describes how to wire it.
- **Security tool selection and configuration content** — `security-toolchain` skill.
- **Benchmark and load test design** — `performance-engineering` skill.
- **Application code packaging concerns** — `csharp-dev` (Aspire AppHost / ServiceDefaults) and `js-dev` (build steps for browser code).
