# Security Policy

## Supported Versions

We actively support the latest major version of Abies. Security updates are provided for:

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | ✅ Yes             |
| < 1.0   | ❌ No              |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report security vulnerabilities by emailing: me@mauricepeters.dev

You should receive a response within 48 hours. If for some reason you do not, please follow up via email to ensure we received your original message.

Please include the following information in your report:

- Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

This information will help us triage your report more quickly.

## Preferred Languages

We prefer all communications to be in English.

## Security Measures

### Automated Dependency Scanning

We automatically scan for vulnerable dependencies using:

- **NuGet Audit**: Built into .NET SDK, runs on every restore
- **GitHub Dependabot**: Monitors for security updates weekly
- **CI/CD Pipeline**: Checks for vulnerabilities on every build

### Manual Code Review

All pull requests are reviewed for:

- Secure coding practices
- Proper input validation
- Dependency updates
- Adherence to pure functional programming principles

### Dependency Management

- We minimize external dependencies to reduce attack surface
- Dependencies are regularly updated
- Transitive dependencies are monitored
- All dependencies must be from trusted sources (NuGet.org)

## Security Best Practices for Users

When using Abies in your projects:

1. **Keep Abies Updated**: Always use the latest stable version
2. **Review Dependencies**: Check the dependencies Abies brings into your project
3. **Enable NuGet Audit**: Ensure `NuGetAudit` is enabled in your projects (enabled by default in .NET 9+)
4. **Follow CSP Guidelines**: Configure appropriate Content Security Policy headers for your apps
5. **Validate User Input**: Even in client-side applications, validate and sanitize all user input

## Known Limitations

- Abies is a client-side framework running in the browser sandbox
- Security of your application also depends on:
  - Your backend API security
  - Your hosting configuration
  - Your implementation of authentication/authorization
  - Third-party libraries you add to your project

## Security Updates

Security updates will be released as soon as possible after a vulnerability is confirmed. We will:

1. Publish a GitHub Security Advisory
2. Release a patch version
3. Update this document with mitigation steps if immediate patching is not possible
4. Notify users through GitHub releases and repository notifications

## Acknowledgments

We thank security researchers who responsibly disclose vulnerabilities to us. With your permission, we will acknowledge your contribution in our release notes.

## Contact

For security-related questions that are not vulnerability reports, please open a GitHub discussion or issue.

---

*Last Updated: February 5, 2026*
