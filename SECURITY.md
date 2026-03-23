# Security Policy

## Supported Versions

| Version | Supported |
|---|---|
| 0.1.x (current) | Yes |

## Reporting a Vulnerability

If you discover a security vulnerability in PSProxmoxVE, please report it responsibly.

**Do not open a public issue for security vulnerabilities.**

Instead, please email the maintainer directly or use [GitHub's private vulnerability reporting](https://github.com/goodolclint/PSProxmoxVE/security/advisories/new).

### What to include

- A description of the vulnerability
- Steps to reproduce the issue
- The potential impact
- Any suggested fixes (optional)

### Response timeline

- **Acknowledgment**: Within 48 hours of receipt
- **Initial assessment**: Within 1 week
- **Fix release**: As soon as practical, depending on severity

## Security Considerations

### Credential Handling

- PSProxmoxVE accepts credentials via `PSCredential` objects and API tokens.
- All password parameters use `SecureString` with secure memory cleanup (`Marshal.ZeroFreeGlobalAllocUnicode`).
- Credentials are never written to disk or included in verbose/debug output.
- Ticket-based sessions expire after 2 hours. The module detects and reports expiry.

### TLS/HTTPS

- All API communication uses HTTPS. There is no HTTP fallback.
- The `-SkipCertificateCheck` parameter disables TLS certificate validation and emits a warning when used. Use only in trusted networks or test environments.

### Input Validation

- All VM ID parameters are validated with `[ValidateRange(100, 999999999)]` to match PVE constraints.
- All dynamic values in API URL paths are encoded with `Uri.EscapeDataString()` to prevent injection.

### Dependencies

NuGet dependencies are pinned to specific versions and monitored for vulnerabilities via Dependabot.
