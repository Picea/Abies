# ADR-005: Security Scanning (SAST/DAST/SCA) in Build Pipeline

**Status:** Proposed  
**Date:** 2026-02-05  
**Deciders:** Development Team  
**Related:** ADR-002 (Pure Functional Programming)

## Context

The Abies framework is a public open-source project that:
- Targets .NET 10
- Has minimal external dependencies (only Praefixum package)
- Publishes to NuGet.org
- Includes a production example (Conduit) that demonstrates real-world usage
- Uses GitHub Actions for CI/CD

We need to decide whether to implement automated security scanning (SAST/DAST/SCA) in our build pipeline to identify vulnerabilities early in the development lifecycle.

## Decision Drivers

- **Project maturity**: Open-source framework that will be consumed by other developers
- **Supply chain security**: Published to public package repository (NuGet.org)
- **Dependency risk**: External dependencies can introduce vulnerabilities
- **Compliance**: Security best practices for open-source projects
- **Cost**: GitHub Advanced Security features cost considerations for public repos
- **Maintenance overhead**: Additional tooling requires ongoing maintenance

## Options Considered

### Option 1: No Security Scanning (Status Quo)
**Pros:**
- Zero additional maintenance overhead
- No CI/CD pipeline complexity
- No cost

**Cons:**
- No automated vulnerability detection
- Higher risk of shipping vulnerable code
- Reactive rather than proactive security posture
- May discourage enterprise adoption

### Option 2: Minimal SCA Only (Recommended)
**Pros:**
- **Free and built-in**: `dotnet list package --vulnerable` is native to .NET SDK
- **Zero configuration**: Works out of the box
- **Low maintenance**: Microsoft maintains the vulnerability database
- **Fast**: Minimal impact on CI/CD runtime
- **Immediate value**: Catches known vulnerable dependencies

**Cons:**
- Limited to dependency vulnerabilities only
- No code-level vulnerability detection
- No runtime security testing

**Implementation:**
```yaml
# Add to .github/workflows/cd.yml
- name: Check for vulnerable dependencies
  run: dotnet list package --vulnerable --include-transitive
  continue-on-error: true  # Initially warn only
```

### Option 3: Full SAST/DAST/SCA Suite
**Pros:**
- Comprehensive security coverage
- Multiple layers of protection
- Industry best practices

**Cons:**
- **Significant cost**: GitHub Advanced Security requires paid plan for private repos
- **High complexity**: Multiple tools to configure and maintain
- **Slower CI/CD**: Additional analysis time
- **False positives**: Requires triage effort
- **May be overkill**: Minimal attack surface for a pure functional framework

## Decision

**We choose Option 2: Minimal SCA Only** with a phased approach:

### Phase 1: Immediate Implementation (Now)
✅ **SCA - Software Composition Analysis**
- Use built-in `dotnet list package --vulnerable`
- Enable NuGetAudit in projects (already enabled by default in .NET 10+)
- Add CI check in all workflows
- Configure as **warning** initially, not error

### Phase 2: SAST Implementation (Completed 2026-02-05)
✅ **SAST - Static Application Security Testing**
- GitHub CodeQL for C# (free for public repos) - **ENABLED**
- Uses `build-mode: none` for C# (no build required)
- Runs `security-and-quality` query suite
- Weekly scheduled scans + PR checks
- Results visible in Security tab

### Phase 3: Future Consideration (6-12 months)
⏭️ **DAST - Dynamic Application Security Testing**
- Only if Abies grows to include server-side components
- Currently NOT needed (Blazor WASM runs client-side only)
- Reassess when/if backend authentication/API features are added

## Rationale

### Why SCA is Essential
1. **Supply chain attacks are increasing**: Dependencies are the #1 attack vector
2. **Transitive dependencies**: We may not even know what we're using
3. **Zero cost, high value**: Native .NET tooling with GitHub vulnerability database
4. **Minimal effort**: One command line in CI pipeline
5. **Framework responsibility**: Users trust our dependency choices

### Why SAST Should Wait
1. **Pure functional architecture**: Limited attack surface (no SQL, no file system, no network)
2. **CodeQL is free for public repos**: We can enable it anytime
3. **Let the project mature**: Establish baseline before adding more tooling
4. **Focus on core features**: Don't over-engineer security prematurely

### Why DAST is Not Needed (Yet)
1. **No backend**: Blazor WASM runs entirely in browser
2. **No authentication**: Example apps use demo APIs
3. **No sensitive data processing**: Framework is for UI rendering
4. **Future-proof**: Can add if architecture changes

## Implementation Plan

### Step 1: Enable NuGetAudit (Already Active)
```xml
<!-- In Directory.Build.props or .csproj -->
<PropertyGroup>
  <NuGetAuditMode>all</NuGetAuditMode>
  <NuGetAuditLevel>low</NuGetAuditLevel>
  <NuGetAudit>true</NuGetAudit>
</PropertyGroup>
```

### Step 2: Add CI Check
```yaml
# .github/workflows/cd.yml
jobs:
  security-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Check for vulnerable packages
        run: |
          dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerability-report.txt
          if grep -q "critical\|high\|moderate" vulnerability-report.txt; then
            echo "⚠️ Vulnerable packages detected! Review the report above."
            exit 0  # Warning only for now
          fi
```

### Step 3: Document Security Policy
Create `SECURITY.md`:
```markdown
# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | ✅ Yes             |

## Reporting a Vulnerability

Please report security vulnerabilities to [security email].

## Dependency Scanning

We automatically scan dependencies using:
- NuGet Audit (built into .NET SDK)
- GitHub Dependabot alerts
```

### Step 4: Configure Dependabot
Create `.github/dependabot.yml`:
```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
```

## Consequences

### Positive
- ✅ Proactive vulnerability detection at zero cost
- ✅ Builds trust with users and enterprises
- ✅ Catches issues before they reach production
- ✅ Minimal CI/CD performance impact
- ✅ Easy to maintain and understand

### Negative
- ⚠️ May occasionally block builds due to vulnerable dependencies
- ⚠️ Requires reviewing and updating dependencies more frequently
- ⚠️ Some vulnerability reports may be false positives

### Neutral
- 🔄 Establishes security-conscious development culture
- 🔄 Creates foundation for more comprehensive security scanning later
- 🔄 May require occasional suppression of irrelevant vulnerabilities

## Monitoring & Review

- **Monthly**: Review any new vulnerability alerts
- **Quarterly**: Assess if CodeQL SAST should be enabled
- **Annually**: Reassess full security scanning strategy
- **Trigger**: If framework adds server-side features, reassess DAST need

## References

- [Microsoft: Auditing NuGet Packages](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
- [GitHub: About Code Scanning](https://docs.github.com/en/code-security/code-scanning/introduction-to-code-scanning/about-code-scanning)
- [OWASP: Dependency Check](https://owasp.org/www-project-dependency-check/)
- [GitHub: CodeQL for C#](https://docs.github.com/en/code-security/code-scanning/creating-an-advanced-setup-for-code-scanning/codeql-code-scanning-for-compiled-languages#building-c)
- [Snyk: Open Source Security](https://snyk.io/product/open-source-security-management/)

## Notes

This ADR focuses on **practical, incremental security improvements** rather than comprehensive coverage. The Abies framework's architecture (pure functional, client-side only, minimal dependencies) reduces attack surface significantly, making aggressive SAST/DAST less critical initially.

As the project grows and adds features, we should revisit this decision and consider:
- Enabling GitHub CodeQL (Phase 2)
- Adding secret scanning
- Implementing SBOM (Software Bill of Materials) generation
- Considering third-party SCA tools like Snyk or WhiteSource if the project becomes enterprise-critical
