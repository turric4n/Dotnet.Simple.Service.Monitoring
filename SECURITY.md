# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 2.0.x   | ✅ Current |
| 1.0.x   | ⚠️ Critical fixes only |
| < 1.0   | ❌ Unsupported |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly.

**Do NOT open a public issue for security vulnerabilities.**

### How to Report

1. Email: Send details to the repository maintainer via GitHub private messaging
2. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

### What to Expect

- **Acknowledgement**: Within 48 hours
- **Initial assessment**: Within 1 week
- **Fix timeline**: Depends on severity (critical: ASAP, high: 1-2 weeks, medium: next release)

### Scope

The following are in scope:
- Authentication/authorization bypasses in transport publishers
- Credential exposure in logs, configuration, or error messages
- Injection vulnerabilities in SQL monitors or HTTP health checks
- Cross-site scripting (XSS) in the monitoring dashboard
- Denial of service through crafted health check configurations
- Insecure defaults in transport settings

### Security Best Practices

When using this library:

1. **Never commit credentials** — use environment variables or secret managers for transport settings (SMTP passwords, API tokens, bot tokens)
2. **Use HTTPS** — always use TLS for HTTP health checks and webhook transports
3. **Restrict dashboard access** — place the `/monitoring` endpoint behind authentication
4. **Limit SQL queries** — use read-only database users for SQL health checks
5. **Network segmentation** — run the monitoring service in a trusted network zone
