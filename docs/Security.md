# Security

## Security controls

LabAuthServer implements a layered security model built around the following controls:

- LDAPS-only directory access over TCP 636
- service account password stored outside source control in a DPAPI-protected file
- certificate-backed RSA signing keys in the Windows certificate store
- JWT validation with issuer, audience, expiry, signing key, and role enforcement
- default deny authorization policy
- correlation IDs and auditable failures
- sanitized ProblemDetails responses

## Secret and credential handling

### LDAP service account password

The AD service-account credential is not stored in source code or application settings. The runtime reads it from the Windows DPAPI-protected file at:

`C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi`

The application is expected to run under a Windows identity with permission to read this file. The secret is decrypted only inside the service-account credential provider and is never returned in logs or API responses.

### Signing certificate and keys

The JWT signing certificate remains in the certificate store under the configured `LocalMachine\My` location. The software resolves the certificate by thumbprint and uses its RSA private key only when issuing tokens. The repository contains no private key material and no certificate export artifacts.

### SQL access

The application uses Windows Integrated Security with the `LabAuthServer` database. The runtime identity is the IIS app pool identity mapped to the SQL role `LabAuthServer_AuditWriter` with `EXECUTE` only on `Audit.usp_WriteAuditEvent`.

## Fail-closed design

The application is intentionally fail-closed in the following cases:

- missing or unreadable DPAPI secret
- invalid AD configuration
- non-matching UPN domain
- missing or invalid JWT signing configuration
- invalid or missing required claims
- unknown or malformed AD group mapping
- invalid or unauthorized authorization result

In each case the API returns a safe error and, where applicable, records an audit event.

## Sensitive data handling rules

The project explicitly avoids storing or returning:

- passwords
- JWTs
- DPAPI contents
- certificate private keys
- service-account secrets
- raw LDAP details
- authorization headers

This behavior is enforced by the validation and middleware layers and is covered in the automated tests.
