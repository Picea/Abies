# CodeQL Quick Setup Guide

## ✅ Ready to Enable!

Your CodeQL configuration is **ready to go**. Follow these simple steps:

## Option 1: Enable via Workflow File (Recommended) ✨

The workflow file is already created at `.github/workflows/codeql.yml`.

**To activate:**

1. **Commit the workflow file**
   ```bash
   git add .github/workflows/codeql.yml
   git commit -m "Enable CodeQL security scanning"
   git push
   ```

2. **That's it!** 🎉
   - CodeQL will run automatically on:
     - Every push to `main`
     - Every pull request
     - Weekly (Mondays at 9 AM UTC)

3. **Check results**
   - Go to: **Security tab → Code scanning**
   - View alerts, suppress false positives, track trends

## Option 2: Enable via GitHub UI (Alternative)

If you prefer the GUI:

1. Go to: **Settings → Code security and analysis**
2. Find: **Code scanning**
3. Click: **Set up → Default**
4. Select: C#
5. Click: **Enable CodeQL**

⚠️ **Note:** Option 1 (workflow file) gives you more control and is already configured optimally for Abies.

## What to Expect

### First Run
- **Duration:** 5-10 minutes
- **Results:** Likely 5-20 findings (mostly informational)
- **Action:** Review and triage findings

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
- **What about .NET 9/10?** Fully supported!

---

**Status:** Ready to enable  
**Estimated setup time:** < 5 minutes  
**Benefit:** Comprehensive code security scanning
