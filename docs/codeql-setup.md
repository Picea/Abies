# CodeQL Operations Guide

## ✅ Current Status

CodeQL is enabled via [.github/workflows/codeql.yml](../.github/workflows/codeql.yml) and runs on:
- Every push to `main`
- Every pull request targeting `main`
- Weekly schedule (Mondays at 9 AM UTC)

Use this page for operational checks and maintenance, not first-time enablement.

## Verify CodeQL Is Healthy

1. Check workflow history:
   - Actions -> `CodeQL Security Analysis`
2. Check alert surface:
   - Security -> Code scanning alerts
3. Confirm no stale failures on recent PRs and main pushes.

## What to Expect

### Typical Run
- **Duration:** ~5-10 minutes
- **Action:** Review new/changed findings and triage by severity

### Typical Findings
- ℹ️ **Informational:** Code style suggestions (can ignore)
- ⚠️ **Low/Medium:** Best practice recommendations
- 🔴 **High/Critical:** Actual security issues (address immediately)

### False Positives
If CodeQL flags something incorrectly:

1. Click on the alert
2. Click "Dismiss alert"
3. Select reason: "False positive"
4. Add comment explaining why

## Configuration Details

Current setup in `.github/workflows/codeql.yml`:

```yaml
language: csharp
build-mode: none  # No build needed!
queries: security-and-quality  # Comprehensive coverage
schedule: weekly  # Monday 9 AM UTC
```

### Query Suites Available

| Suite | Coverage | Recommended |
|-------|----------|-------------|
| `security-extended` | Security only | Good start |
| `security-and-quality` | Security + quality | ✅ **Current** |
| Custom | Define your own | Advanced |

## Integration with Existing Security

CodeQL complements your current setup:

```
┌─────────────────────────────────────┐
│   Abies Security Stack              │
├─────────────────────────────────────┤
│ SCA (Dependencies)                  │
│ • NuGet Audit           ✅ Active   │
│ • Dependabot            ✅ Active   │
├─────────────────────────────────────┤
│ SAST (Code Analysis)                │
│ • CodeQL                🔄 Ready    │
├─────────────────────────────────────┤
│ Documentation                       │
│ • SECURITY.md           ✅ Active   │
│ • ADR-005               ✅ Active   │
└─────────────────────────────────────┘
```

## Monitoring & Maintenance

### Weekly (5 minutes)
- Check Security tab for new alerts
- Review and address high/critical findings

### Monthly (30 minutes)
- Review trends (are findings increasing?)
- Update suppression rules if needed
- Check for CodeQL updates (automatic)

### Quarterly (1 hour)
- Review entire security posture
- Update ADR-005 if strategy changes
- Consider additional query packs

## Troubleshooting

### Build Fails
- **Cause:** CodeQL found critical vulnerability
- **Solution:** Review alert in Security tab, fix code
- **Workaround:** Temporarily set `continue-on-error: true`

### No Results
- **Cause:** Workflow not triggered yet
- **Solution:** Make a commit or trigger manually
- **Check:** Actions tab → CodeQL workflow

### Too Many False Positives
- **Cause:** Too sensitive query suite
- **Solution:** Change to `security-extended` in workflow
- **Or:** Suppress individual alerts

## Next Steps

1. ✅ Commit and push CodeQL workflow
2. ⏳ Wait for first scan (5-10 min)
3. 🔍 Review findings in Security tab
4. 📝 Update ADR-005 status (if needed)
5. 🎉 Celebrate secure code!

## Resources

- [CodeQL C# Queries](https://github.com/github/codeql/tree/main/csharp/ql/src/Security%20Features)
- [Managing Alerts](https://docs.github.com/en/code-security/code-scanning/managing-code-scanning-alerts)
- [Best Practices](https://docs.github.com/en/code-security/code-scanning/creating-an-advanced-setup-for-code-scanning/codeql-code-scanning-best-practices)
- [ADR-005](docs/adr/ADR-005-security-scanning-sast-dast-sca.md)

## Questions?

- **How much does it cost?** Free for public repos!
- **Will it slow CI?** ~5 minutes added
- **Can I disable it?** Yes, delete workflow file
- **What about .NET 10?** Fully supported!

---

**Status:** Ready to enable  
**Estimated setup time:** < 5 minutes  
**Benefit:** Comprehensive code security scanning
