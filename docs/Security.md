# Security

## Implemented controls

- LDAPS-only directory communication over TCP 636.
- LDAP filter escaping and configured-domain UPN validation.
- Windows DPAPI-backed service-account credential loading.
- RSA certificate-store signing with algorithm, issuer, audience, lifetime, key-ID, and claim validation.
- Default authenticated-user policy and explicit role policies.
- Correlation IDs, minimized audit events, and generic ProblemDetails responses.
- Typed stored-procedure audit writes with sensitive JSON rejection.
- Startup validation for Active Directory, token, authorization, and audit configuration.

## Protected data boundaries

Passwords, bearer tokens, authorization headers, private keys, DPAPI contents, raw LDAP responses, and stack traces must not be placed in source control, logs, SQL audit details, or API responses. Use `<SECRET_FILE>`, `<THUMBPRINT>`, `<CONNECTION_STRING>`, and `<USERNAME>` placeholders in examples.

The repository does not implement MFA, federation, refresh tokens, stateful revocation, rate limiting, brute-force protection, or audit retention automation.

## Operational responsibilities

The target Windows identity must have only the access required to read the protected DPAPI file, use the signing certificate private key, and execute the audit writer procedure. Environment-specific identity and permission assignments belong in an approved private runbook, not public documentation.

## Verification boundary

Automated tests cover security behavior and sensitive-data rejection. They do not prove production certificate, DPAPI, AD, SQL identity, or IIS configuration. See [Validation Status](Validation_Status.md) for the current evidence boundary.
