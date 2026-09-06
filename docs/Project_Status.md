# Project Status

**Last reviewed:** 2026-09-06
**Status:** Current implementation summary

## Implementation state

LabAuthServer is a .NET 10 ASP.NET Core API using the approved four-layer dependency direction:

`Domain <- Application <- Infrastructure <- Api`

Implemented production code includes:

- LDAPv3 authentication over LDAPS on TCP 636 with configured-domain UPN validation.
- Windows DPAPI-backed service-account credential loading for directory queries.
- LDAP filter escaping and safe typed authentication failures.
- JWT issuance and bearer validation with RSA signing, certificate-store key resolution, `kid`, previous-key overlap, bounded claims, and required identity/time claims.
- AD group-to-role mapping with Reader, Operator, and Administrator roles and deterministic precedence.
- Default authenticated-user authorization plus named role policies.
- Public health and login endpoints plus a Reader-protected resource.
- Correlation middleware, authorization audit middleware, and global safe exception handling.
- SQL Server audit persistence through `Audit.usp_WriteAuditEvent`.

## API surface

- `GET /api/v1/health` — anonymous application liveness response.
- `POST /api/v1/auth/login` — anonymous HTTPS-only authentication and token issuance.
- `GET /api/v1/protected` — Reader-policy protected resource.

## Verification status

- **Build:** VERIFIED; current Release build completed with zero errors and zero warnings.
- **Automated tests:** VERIFIED; current full solution run completed with 173 passed, 0 failed, and 0 skipped.
- **Security boundaries:** VERIFIED by automated tests for configuration, token, authorization, LDAP-input, audit, correlation, and safe-error behavior.
- **Live AD identity:** NOT VERIFIED from independently reproducible repository evidence.
- **Current IIS deployment:** NOT VERIFIED as current from repository evidence; deployment records are historical.
- **Certificate private-key and DPAPI behavior:** NOT VERIFIED against a target environment.
- **Audit retention/purge:** NOT IMPLEMENTED.

The authoritative details and evidence boundary are documented in [Validation_Status.md](Validation_Status.md).

## Current documents

- [Architecture](Architecture.md)
- [Authentication](Authentication.md)
- [Authorization](Authorization.md)
- [JWT](JWT.md)
- [Audit logging](AuditLogging.md)
- [Database](Database.md)
- [Configuration](Configuration.md)
- [Deployment](Deployment.md)
- [Operations](Operations.md)
- [Security](Security.md)
- [Testing](Testing.md)
- [Validation status](Validation_Status.md)
- [Troubleshooting](Troubleshooting.md)

Historical phase records are preserved in [archive](archive/README.md) and are not current implementation guidance.
