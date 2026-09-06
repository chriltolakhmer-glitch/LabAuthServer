# Validation Status

**Status:** Current repository validation summary
**Last reviewed:** 2026-09-06

This document is the authoritative summary of what can be established from the repository and its recorded validation evidence. Historical phase records are retained under `docs/archive/` and are not treated as current proof unless explicitly identified below.

## Current implementation status

| Area | Status | Evidence boundary |
| --- | --- | --- |
| ASP.NET Core API and four-layer solution | IMPLEMENTED | Current solution and project files |
| LDAPS authentication | IMPLEMENTED | Infrastructure code and automated boundary tests |
| JWT issuance and validation | IMPLEMENTED | Token services, bearer configuration, and tests |
| AD group-to-role mapping | IMPLEMENTED | Mapping service, configuration validation, and tests |
| Policy authorization | IMPLEMENTED | Named policies and protected controller |
| SQL audit persistence | IMPLEMENTED | SQL scripts, repository, middleware, and tests |
| Correlation and safe exception handling | IMPLEMENTED | API middleware and tests |
| IIS publishing/deployment workflow | IMPLEMENTED | Publish profile and deployment documentation |

## Build status

**VERIFIED:** The Release solution build completed successfully with zero errors and zero warnings during the current documentation cleanup validation.

## Automated test status

**VERIFIED:** The full solution test run completed with 184 passed, 0 failed, and 0 skipped tests. The suite includes unit tests and ASP.NET Core integration tests. Test results do not prove access to a production directory, certificate private key, DPAPI secret, or deployed IIS environment.

## Authentication validation

**VERIFIED:** Automated tests cover configured-domain UPN validation, typed authentication failure mapping, HTTPS enforcement, cancellation, LDAP filter escaping, and safe failure behavior.

**NOT VERIFIED:** A real user credential is not stored in the repository, and no current repository evidence independently proves a live real-identity login.

## LDAP/LDAPS validation

**IMPLEMENTED:** The runtime requires LDAPv3 over LDAPS on TCP port 636, validates the configured domain, and uses the Windows DPAPI-backed service-account provider for directory queries.

**VERIFIED:** Boundary and failure behavior are covered by automated tests. Live directory behavior remains environment-dependent.

## JWT validation

**VERIFIED:** Tests cover RSA private-key signing, public-key-only validation, the configured algorithm, issuer, audience, lifetime, `kid`, previous-key overlap, required claims, role validation, tampering, and size limits. Certificate tests cover validity windows, RSA key size, digital-signature usage, duplicate matches, and validation without a private key. The current claim set includes `iss`, `aud`, `sub`, `jti`, `iat`, `nbf`, `exp`, `role`, and `scope`.

**VERIFIED:** The certificate policy requires a currently valid RSA certificate with a key of at least 2048 bits. Signing requires a private key; validation uses only the public key. Incompatible KeyUsage is rejected, certificates containing an EKU extension are rejected under the current general-purpose signing policy, and multiple currently valid matching certificates fail closed.

**VERIFIED:** Login requests use a bounded fixed-window rate limit of 10 requests per minute with no queue. Health and protected endpoint behavior remains covered separately.

**VERIFIED:** The default audit connection configuration enables SQL transport encryption. Development/test configuration uses an explicit local certificate-trust exception only where required by the local SQL instance.

## Authorization validation

**VERIFIED:** Tests cover default authenticated-user behavior, Reader authorization, Operator denial, invalid or missing roles, group mapping, role precedence, and fail-closed configuration.

## Audit logging validation

**VERIFIED:** Tests and SQL scripts cover typed stored-procedure writes, event validation, sensitive JSON rejection, correlation data, unavailable-database behavior, and concurrent writes.

**NOT IMPLEMENTED:** Retention, archival, purge automation, and SQL Agent scheduling are not implemented in the repository.

## Health endpoint validation

**VERIFIED:** `GET /api/v1/health` returns the typed healthy response and is anonymous. The endpoint is an application liveness endpoint; it does not claim to prove LDAP, SQL, or certificate-store availability.

## IIS and deployment validation

**IMPLEMENTED:** The API has a file-system publish profile and the repository contains a staged deployment procedure with parity checks and rollback guidance.

**HISTORICAL:** Phase deployment records describe environment-specific checks. They are retained as historical records and are not independent current evidence.

## Real AD identity validation

**NOT VERIFIED FROM REPOSITORY EVIDENCE:** The repository does not contain sufficient independently reproducible evidence to prove a current real-user AD login, live group mapping, issued-token verification, or authenticated access against a protected deployment. The historical acceptance record is not treated as current proof.

## Known limitations

- Live AD authentication requires protected credentials and a reachable directory.
- Certificate-store private-key behavior depends on the target Windows certificate store and runtime identity.
- DPAPI behavior depends on the protected secret file and Windows identity.
- SQL integration depends on an authorized target database and application identity.
- No refresh tokens, sessions, MFA, federation, account lockout, or directory-level brute-force protection are implemented. Login rate limiting is an application-level control.
- Audit retention and purge ownership remain outside the implementation.

## Evidence and reproducibility

Reproduce the repository checks from the repository root:

```powershell
dotnet restore .\LabAuthServer.slnx
dotnet build .\LabAuthServer.slnx -c Release --no-restore --nologo
dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo
```

Historical phase documents remain available under `docs/archive/` for traceability. They may contain older counts, proposed designs, or environment-specific observations and must be interpreted as historical.
