# LabAuthServer

LabAuthServer is an ASP.NET Core authentication and authorization service for Active Directory-backed login and role-based access control. The implementation in this repository is the verified production configuration that has passed end-to-end validation for secure LDAPS authentication, AD group-to-role mapping, JWT issuance and validation, and SQL audit persistence.

## Solution structure

- `src/LabAuthServer.Api` — HTTP boundary, endpoint configuration, middleware, and authentication pipeline.
- `src/LabAuthServer.Application` — DTOs, interfaces, constants, auditing contracts, and application orchestration.
- `src/LabAuthServer.Domain` — core entities and domain rules.
- `src/LabAuthServer.Infrastructure` — AD integration, JWT security, authorization mapping, and SQL audit repository.
- `tests/LabAuthServer.UnitTests` — unit tests for configuration validation, token validation, LDAP security boundaries, and audit behavior.
- `tests/LabAuthServer.IntegrationTests` — API and integration coverage for health, login, authorization, and middleware behavior.
- `docs` — final architecture, authentication, JWT, deployment, operations, and troubleshooting documentation.

## Verified implementation summary

- LDAPS authentication to the AD domain controller over TCP 636.
- AD service-account credential retrieval from the DPAPI-backed file at `C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi`.
- Explicit group-to-role mapping with fail-closed handling for malformed or unapproved groups.
- JWT issuance using RSA signing via the certificate-backed protected key provider.
- JWT validation with issuer, audience, required claims, and role enforcement.
- Reader-only protected endpoint authorization via policy enforcement.
- SQL Server audit logging through `Audit.usp_WriteAuditEvent` with correlation IDs and sanitized ProblemDetails responses.
- IIS deployment under the LabAuthServer site, application pool, and safe deploy workflow.

## Phase 18 documentation set

- [docs/Architecture.md](docs/Architecture.md)
- [docs/Authentication.md](docs/Authentication.md)
- [docs/JWT.md](docs/JWT.md)
- [docs/AuditLogging.md](docs/AuditLogging.md)
- [docs/Configuration.md](docs/Configuration.md)
- [docs/Deployment.md](docs/Deployment.md)
- [docs/Operations.md](docs/Operations.md)
- [docs/Troubleshooting.md](docs/Troubleshooting.md)
- [docs/Phase18_FinalAcceptance.md](docs/Phase18_FinalAcceptance.md)

## Operational status

- Phase 17 acceptance: complete
- Phase 18 documentation: complete
- Applicable production deployment: safe deployment script and backed-up Current directory only
- No production credentials, JWTs, private keys, or secret material are stored in this repository

## Standard checks

```powershell
cd C:\Apps\LabAuthServer\Source\LabAuthServer
dotnet restore .\LabAuthServer.slnx
dotnet build .\LabAuthServer.slnx -c Release --no-restore
dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo
```

For deployment and rollback procedures, see [scripts/Deploy-LabAuthServerSafe.ps1](../Scripts/Deploy-LabAuthServerSafe.ps1) and [docs/Deployment.md](docs/Deployment.md).
