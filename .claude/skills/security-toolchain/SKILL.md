---
name: security-toolchain
description: The full security toolchain — SAST/SCA/DAST/secrets/container scanning layers, specific tools (Roslyn, Semgrep, DevSkim, dotnet-vuln, Dependabot, OWASP Dependency-Check, Gitleaks, OWASP ZAP, Nuclei, Trivy, Docker Scout), ZAP-against-Aspire setup, secure coding standards, pentest methodology, threat model template, regression test patterns, and threat intelligence sources. Use when integrating or tuning security tools, writing the threat model, designing security regression tests, or running a pentest pass.
---

# Security Toolchain Reference

This is the security expert's deep reference. The role and protocol live in `.claude/agents/security-expert.md`. This skill catalogs the **tools, configurations, and patterns** the team uses for application security and automated penetration testing.

The squad's security principles (every endpoint has an authorization policy, parameterized queries only, no hardcoded secrets, threat model living document, automated security pipeline, defense in depth) live in `.claude/docs/decisions.md`.

---

## The Five Layers

Defense in depth means no single tool catches everything. The team layers SAST, SCA, secrets detection, DAST, and container scanning. Each layer catches what the others miss.

### Semgrep MCP (preferred SAST runtime for the security-expert)

The security-expert subagent has direct access to Semgrep via MCP tools (`mcp__semgrep__*`). Prefer these over shelling out to the `semgrep` CLI when running ad-hoc scans during a review or threat-model exercise:

- **`mcp__semgrep__security_check`** — scan a snippet or set of files for security issues using Semgrep's curated rule pack. Returns structured findings (rule id, severity, file, line, message). Use this first when the user asks "is this code secure?" or "are there obvious vulnerabilities here?"
- **`mcp__semgrep__semgrep_scan`** — scan with a specific config or rule set. Use when you need to point at the project's `.semgrep/` directory or a registry rule pack (`p/owasp-top-10`, `p/dotnet`, `p/csharp`).
- **`mcp__semgrep__semgrep_scan_with_custom_rule`** — one-shot scan with a custom YAML rule. Use when authoring or testing a new project-specific rule before committing it to `.semgrep/`.
- **`mcp__semgrep__semgrep_findings`** — when the project is connected to Semgrep AppSec Platform (`SEMGREP_APP_TOKEN` set), pull historical findings rather than re-scanning.

Project-specific rules in `.semgrep/` always run in addition to whatever pack you choose. If you find a recurring pattern via the MCP scan that the rule pack doesn't catch, propose a project-specific rule via a `/decide` drop and let the user accept it before committing.

The MCP server is wired in `.mcp.json` at the repo root. Set `SEMGREP_APP_TOKEN` in the environment to unlock platform features; the tools degrade gracefully without it.

### Layer 1 — SAST (Static Application Security Testing)

Static analysis of source code. Catches injection vulnerabilities, insecure deserialization, hardcoded credentials in code, and a hundred OWASP-flavoured antipatterns — *before* the code runs.

**Tools:**

| Tool | Scope | Where it runs | Notes |
|---|---|---|---|
| `Microsoft.CodeAnalysis.NetAnalyzers` | C# language patterns, BCL misuse | IDE + CI | Built into the SDK. Keep `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props` |
| `SecurityCodeScan.VS2019` | .NET-specific security antipatterns | IDE + CI | Catches SQLi, XSS, weak crypto, insecure deserialization patterns Roslyn doesn't |
| Semgrep | Language-agnostic pattern matching | IDE + CI | Custom rules for project-specific patterns. Run `semgrep ci --config=auto --config=.semgrep/` |
| DevSkim (Microsoft) | Cross-language security linting | IDE + CI | Lightweight; complements Semgrep |
| GitHub CodeQL | Deep dataflow analysis | CI (GitHub-hosted) | Free for public repos; paid for private. Excellent at finding hard-to-spot taint-flow bugs |

**`.semgrep/` directory layout:**
```
.semgrep/
├── csharp-domain-rules.yml       # custom rules for the team's functional-DDD style
├── auth-policy-required.yml      # every endpoint must have an authorization policy
├── parameterized-sql-only.yml    # flag string concatenation into SQL
└── no-string-concat-html.yml     # flag string concat into HTML output
```

Custom rule example — *every endpoint must declare an authorization policy*:
```yaml
rules:
  - id: endpoint-without-auth-policy
    pattern-either:
      - pattern: |
          $APP.MapGet($PATH, ...)
      - pattern: |
          $APP.MapPost($PATH, ...)
      - pattern: |
          $APP.MapPut($PATH, ...)
      - pattern: |
          $APP.MapDelete($PATH, ...)
    pattern-not-inside: |
      $APP.MapGroup(...).RequireAuthorization(...)
    pattern-not: |
      $X.RequireAuthorization(...)
    message: "Endpoint declared without an authorization policy. Either RequireAuthorization or AllowAnonymous explicitly."
    severity: ERROR
    languages: [csharp]
```

### Layer 2 — SCA (Software Composition Analysis)

Scans dependencies for known vulnerabilities (CVEs). Catches what attackers most commonly exploit: known bugs in libraries you depend on.

The squad runs SCA at **three cadences** because each catches a different class of failure:

| Tool | Scope | When it runs | What it catches |
|---|---|---|---|
| `NuGetAudit=true` (built-in) | Direct + transitive NuGet refs | **Every `dotnet restore`/`dotnet build`** | Vulns landed since your last restore |
| `dotnet list package --vulnerable --include-transitive` | Same as above | On demand / audit scripts | Same data, but enumerable for reporting |
| Dependabot (GitHub) | NuGet + npm + Actions | GitHub continuous | Vulns disclosed since you last touched the dep |
| OWASP Dependency-Check | Multi-ecosystem CVE scanning | CI nightly | Edge-case ecosystems (binary deps, JARs in Java interop) |
| Snyk (optional) | Multi-ecosystem | CI | Reachability analysis (is the vuln on a code path you actually use?) |

**`NuGetAudit` is the squad's primary control.** It's enabled in `Directory.Build.props` with:

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>          <!-- transitive deps too, not just direct -->
<NuGetAuditLevel>moderate</NuGetAuditLevel>   <!-- moderate, high, critical -->
```

It runs as part of the SDK's restore step, hits the GitHub Advisory Database, and emits NU1901-NU1904 warnings (which are errors when `TreatWarningsAsErrors=true`, also squad policy). Net effect: a newly-disclosed vuln in a transitive dep breaks the build the next time anyone runs `dotnet build`. No separate scanner needed for the common case.

The other tools complement it:
- **Dependabot** runs continuously on GitHub's side and opens PRs for upgrades — useful when nobody's touched the repo in a while.
- **`dotnet list package --vulnerable`** is what you run when you want a *report* (machine-readable JSON via `--format json`) rather than a build failure.
- **OWASP Dependency-Check** matters only if you have non-NuGet ecosystems in scope.

**SCA gating policy:**
- Critical CVE → blocks merge unconditionally.
- High CVE → blocks merge unless waiver decision is on file with target remediation date.
- Moderate → audit-level threshold; with `NuGetAuditLevel=moderate` these break the build by default. Suppress only with explicit waiver.
- Low → tracked, not gating; reviewed monthly.

**Fixing transitive vulns under CPM** (the squad uses Central Package Management with `CentralPackageTransitivePinningEnabled=true`): when audit flags a vuln in a transitive dep, the fix is one line in `Directory.Packages.props` — add a `<PackageVersion Include="<vulnerable-package>" Version="<patched-version>" />` entry. That promotes the patched version across the whole solution. Without CPM transitive pinning, the only fix is to update whichever direct dep pulls it in (which often hasn't shipped a patched version yet).

### Layer 3 — Secrets Detection

Catches credentials accidentally committed to the repo.

**Tools:**

| Tool | Scope |
|---|---|
| Gitleaks | Pre-commit hook + CI (full-history scan) |
| GitHub Secret Scanning | Continuous (alerts on push) |
| TruffleHog | CI deeper scans |
| `dotnet user-secrets` | The team's local-dev secrets store — never check secrets into source |

**Pre-commit hook content (`.husky/pre-commit` or `.git/hooks/pre-commit`):**
```bash
#!/usr/bin/env bash
gitleaks protect --staged --redact -v
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Gitleaks found a potential secret. Commit blocked."
    exit 1
fi
```

`.gitleaks.toml` should extend the default config with project-specific allowlists (e.g., test-only fixture data) — not relax the rules globally.

### Layer 4 — DAST (Dynamic Application Security Testing)

Runs against the live application. Catches what static analysis can't see — runtime behavior, auth bypasses, business logic flaws, real injection at the boundary.

**Tools:**

| Tool | Scope | Notes |
|---|---|---|
| OWASP ZAP | Web application scanner | Run as a CI job pointed at the Aspire AppHost |
| Nuclei | Template-based vulnerability scanner | Fast, low-noise; complements ZAP for known CVE checks |
| Burp Suite (manual) | Pentest sessions | Used by humans during scheduled pentest passes |

**Running ZAP against the Aspire AppHost in CI:**

```yaml
# .github/workflows/dast.yml
name: DAST
on:
  workflow_dispatch:
  schedule:
    - cron: '0 2 * * *'  # nightly

jobs:
  zap-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Start Aspire AppHost
        run: |
          dotnet run --project src/<Root>.AppHost &
          # wait for the entrypoint service health check
          for i in {1..60}; do
            curl -fs http://localhost:5000/health && break || sleep 2
          done

      - name: ZAP baseline scan
        uses: zaproxy/action-baseline@v0.13.0
        with:
          target: 'http://localhost:5000'
          rules_file_name: '.zap/rules.tsv'
          cmd_options: '-J zap-report.json'

      - name: ZAP full scan (weekly)
        if: github.event.schedule == '0 2 * * 0'
        uses: zaproxy/action-full-scan@v0.10.0
        with:
          target: 'http://localhost:5000'
          rules_file_name: '.zap/rules.tsv'
          cmd_options: '-J zap-full-report.json'

      - name: Upload report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: zap-report
          path: zap-report.json
```

`.zap/rules.tsv` lets you tune individual rules (raise/lower severity, ignore false positives that have a documented reason).

### Layer 5 — Container Scanning

Scans Docker images and their base layers for vulnerabilities.

**Tools:**

| Tool | Scope |
|---|---|
| Trivy | OS packages, language deps, Dockerfile misconfigurations |
| Docker Scout | Layer-by-layer analysis, SBOM, runtime advisories |
| Hadolint | Dockerfile linting (best practices) |

**`trivy.yaml`:**
```yaml
severity: CRITICAL,HIGH
ignore-unfixed: false
format: sarif
output: trivy-report.sarif
```

In CI, fail the build on `CRITICAL` and `HIGH` for the production image. `MEDIUM`/`LOW` are tracked but not blocking.

---

## Secure Coding Standards

These are the patterns the C# Dev and JS Dev follow; the Reviewer enforces them.

### Input validation
- Validate at the boundary (controllers, endpoints, message handlers).
- Use the team's smart-constructor pattern for domain primitives — validation happens once, at type creation.
- Never trust input from the network, the URL, the DOM, or message buses you don't own.

### Output encoding
- Use Razor's automatic HTML encoding; never use `Html.Raw` with user data.
- For JS contexts: use `JsonSerializer.Serialize` with `JsonStringEscapeHandling`-equivalent defaults, or `System.Text.Json` defaults that escape safely.
- For URL contexts: `Uri.EscapeDataString`.

### SQL & data access
- Parameterized queries only. EF Core does this by default. Raw SQL via `FromSqlInterpolated` (parameterizes correctly), never `FromSqlRaw($"...{userInput}...")`.

### Authentication & authorization
- Every endpoint has an explicit policy. `AllowAnonymous` declared explicitly when correct.
- ASP.NET authorization policies in a single registration block — easy to audit.
- Avoid implicit fall-through where missing authorization means anonymous.

### Secrets management
- `dotnet user-secrets` for local development.
- Environment variables / secret manager (Azure Key Vault, AWS Secrets Manager) for deployed environments.
- Never log secrets. Configure logging filters to redact known-sensitive properties.

### Cryptography
- `System.Security.Cryptography` only — never roll your own.
- For random values that need to be unpredictable: `RandomNumberGenerator.GetBytes(...)`. Never `System.Random` for security.
- For password hashing: `Microsoft.AspNetCore.Identity` defaults (PBKDF2 with proper iterations) or Argon2 via a vetted library. Never custom hash implementations.

### HTTP security headers
Every web service ships with at minimum:
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `X-Content-Type-Options: nosniff`
- `Content-Security-Policy: default-src 'self'; ...` (project-specific)
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: ...` (deny by default)

### CORS
- Explicit allowlist. No `AllowAnyOrigin` in production.
- Credentialed requests require the most restrictive policy possible.

---

## Threat Model

`/docs/security/threat-model.md` is a **living document**. It is updated after every change that alters the attack surface. Format:

```markdown
# Threat Model

## System Overview
[Short description of the system, with a high-level architecture diagram]

## Trust Boundaries
1. **Internet → Public API** — anonymous traffic enters here
2. **Public API → Internal Services** — authenticated traffic only
3. **Services → Database** — service identity, not user identity
4. ...

## Assets & Sensitivities
| Asset | Sensitivity | Controls |
|---|---|---|
| User credentials | High | Hashed with PBKDF2; Key Vault for hashing keys |
| Personal data | High | Encrypted at rest; row-level access control |
| ... | ... | ... |

## Threats (STRIDE)
| ID | Threat | Category | Likelihood | Impact | Mitigation | Regression Test |
|---|---|---|---|---|---|---|
| T-001 | SQL injection on /search | Tampering | Low | High | Parameterized queries; Semgrep rule `parameterized-sql-only` | `SearchEndpointTests.SqlInjection_returns_400_with_no_db_call` |
| T-002 | Mass assignment on /profile | Tampering | Med | Med | Explicit DTOs; never bind directly to entities | `ProfileEndpointTests.Mass_assignment_ignored_for_unbound_fields` |
| ... | ... | ... | ... | ... | ... | ... |

## Open Risks (accepted, not yet mitigated)
| ID | Risk | Severity | Owner | Target Date | Acceptance Rationale |
|---|---|---|---|---|---|

## Change Log
| Date | Change | Reason | Updated By |
|---|---|---|---|
| 2026-01-15 | Added T-007 — credential stuffing on /login | New endpoint shipped | security-expert |
```

**Hard rules:**
- Every threat in the table has a corresponding regression test in the `Regression Test` column. **Threats without tests are 🔴 Must Fix.**
- Every accepted-but-unmitigated risk has an owner and a target date. Indefinite "accepted" entries are not allowed.

---

## Security Regression Tests

When a vulnerability is fixed (or a threat is mitigated), a test is added that **proves the vulnerability is no longer exploitable**. The test stays in the suite forever.

```csharp
public class AuthenticationSecurityRegressionTests
{
    [Test]
    public async Task SQL_injection_in_username_returns_400_and_does_not_query_db()
    {
        var spy = new SpyDbCommandLogger();
        await using var app = await BuildAppHostAsync(services =>
            services.AddSingleton<IDbCommandLogger>(spy));

        var http = app.CreateHttpClient("auth-api");
        var response = await http.PostAsJsonAsync("/login", new
        {
            Username = "admin' OR 1=1 --",
            Password = "anything"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        // The smart constructor on Username should have rejected the input
        // before any DB query happened.
        await Assert.That(spy.QueriesExecuted).IsEqualTo(0);
    }
}
```

The test is **named for the threat ID** (`T-001_...`) when there's a corresponding entry in the threat model.

---

## Pentest Methodology

Run an automated pentest pass on a defined cadence (every release minimum). The pass:

1. **Scope.** Define what's in (production-like environment, all public endpoints, authenticated user journeys) and what's out (third-party services).
2. **Reconnaissance.** Inventory of endpoints, parameters, request shapes. The OpenAPI spec is the starting point.
3. **Automated scanning.** ZAP full scan against the Aspire AppHost. Nuclei templates for known CVEs in dependencies.
4. **Manual probing.** Burp Suite session for authentication, authorization, business logic flaws — areas automated scanners miss.
5. **Triage.** Each finding rated (severity, exploitability, blast radius). False positives marked with rationale.
6. **Report.** Threats mapped to threat-model entries (existing or new). Regression tests written. PRs filed for fixes.
7. **Decision log.** Pentest summary written to `.squad/decisions/inbox/` so the squad sees the pattern.

---

## Threat Intelligence Sources

The Security Expert monitors:

- [NIST NVD](https://nvd.nist.gov/) — CVE database
- [GitHub Security Advisories](https://github.com/advisories) — package-specific CVEs
- [MSRC Security Update Guide](https://msrc.microsoft.com/update-guide) — Microsoft-specific
- OWASP — releases, top-10 updates, ZAP tool releases
- CNA-issued advisories for the project's specific dependencies
- Conference talks and security research blogs (Black Hat, DEF CON, USENIX Security, Project Zero, Naked Security)
- Tool-vendor channels (Snyk Vulnerability DB, GitHub Dependabot advisories, Trivy advisories)

When a relevant new threat appears, the Security Expert assesses, mitigates, adds a regression test, updates scanner rules, and writes a decision to `.squad/decisions/inbox/`.

---

## What This Skill Does NOT Cover

- **Threat-model maintenance and authorization patterns at the code level** are above; the actual implementation of authentication/authorization is `csharp-dev` work that follows these standards.
- **CI/CD pipeline structure** (caching, parallel jobs, artifact handling) — `cicd-pipelines` skill. This skill covers the *content* of security stages; that skill covers how those stages plug into the pipeline.
- **Code-review patterns** — `code-review` skill. Reviewer enforces these standards using the patterns there.
