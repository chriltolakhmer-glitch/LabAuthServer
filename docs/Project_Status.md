# Project Status

Last updated: 2026-09-03

## Current state

## Phase status

- Phase 11: COMPLETE.
- Phase 12: COMPLETE as of 2026-09-04 after 162 automated tests passed with zero failures and zero skips, including SQL audit repository, HTTP pipeline, security-boundary, and concurrency coverage.
- Phase 13: COMPLETE as of 2026-09-04 after the authentication, LDAP/AD, JWT, authorization, API, SQL/audit, information-disclosure, dependency, and code-security review passed.
- Phase 14: COMPLETE as of 2026-09-04 after the Release build passed with zero warnings/errors and 163 tests passed with zero failures/skips on two runs.
- Phase 15: COMPLETE as of 2026-09-04; reproducible Release package created at `Releases\2026-09-04_114744_Release` with 52 runtime files, manifest, and SHA-256 report.
- Phase 16: COMPLETE as of 2026-09-04; validated deployment to IIS site `LabAuthServer` and app pool `LabAuthServerAppPool` at `C:\Apps\LabAuthServer\Current`.
- Phase 17: IN PROGRESS as of 2026-09-04; safe deployed HTTPS, health, authentication-failure, authorization, audit, correlation, and stability checks passed, but approved real AD identity validation remains blocked.
- Phase 18: NOT STARTED; final documentation awaits completion of the mandatory Phase 17 real-identity checks.
- Phase 15-18 validation: 163 tests passed, 0 failed, 0 skipped; Release build passed with zero warnings/errors.

The solution follows the four-layer architecture and has completed the approved LDAPS RootDSE and Phase 3 authentication slices.

- .NET SDK is pinned to `10.0.400` in `global.json`.
- API, Application, Domain, Infrastructure, UnitTests, and IntegrationTests projects use the approved dependency direction.
- The API provides console/debug logging, centralized ProblemDetails handling, HTTPS redirection, and `GET /api/v1/health`.
- LDAPS RootDSE connectivity is implemented.
- User authentication via LDAP direct bind is implemented and remediated.
- Authentication and controller tests do not depend on a live DC.
- WeatherForecast and template code have been removed.

## LDAP Root DSE hardening

Implemented 2026-09-03 to support directories that reject anonymous LDAP binds.

- Root DSE queries use the configured `ActiveDirectory:ServiceAccountUsername` and a Windows DPAPI-backed credential provider for the service-account password.
- The LDAP service-account password is loaded from the protected file path configured in `ActiveDirectory:ServiceAccountPasswordFile` and is not required as an environment variable or source-controlled secret.
- The application fails closed when the protected file is missing, unreadable, or empty; it does not fall back to anonymous bind.
- User authentication continues to bind with the end-user UPN and password.
- LDAP filter values are escaped before the user group search is issued.
- Root DSE failures return a generic operational message and do not expose LDAP exception details.
- No password, certificate, key, or new package was added to the repository.

## Phase 3: Authentication Implementation and Remediation

Implemented 2026-08-31 according to the approved Authentication Design:

- Authentication accepts only a UPN in the configured `lab.local` domain.
- The supplied UPN is passed directly to LDAP; no distinguished name is fabricated or returned.
- Authentication requires LDAPS over TCP port 636 and LDAP protocol version 3.
- Platform certificate validation remains enabled; no certificate callback or bypass is used.
- Passwords are used only for the bind operation and are not persisted or logged.
- Typed failure categories map invalid credentials to 401, directory unavailability to 503, timeouts to 504, and unexpected failures to 500.
- Login requests require HTTPS before authentication is invoked.
- LDAP connection disposal and cancellation handling are isolated in an Infrastructure client.

**Files created:**
- `src/LabAuthServer.Application/Enums/AuthenticationFailureCategory.cs`
- `src/LabAuthServer.Infrastructure/Services/ILdapAuthenticationClient.cs`
- `src/LabAuthServer.Infrastructure/Services/LdapAuthenticationClient.cs`
- `tests/LabAuthServer.UnitTests/LdapAuthenticationServiceTests.cs`

**Files modified:**
- `src/LabAuthServer.Application/Interfaces/IAuthenticationService.cs`
- `src/LabAuthServer.Infrastructure/Services/LdapAuthenticationService.cs`
- `src/LabAuthServer.Api/Controllers/AuthController.cs`
- `src/LabAuthServer.Api/Extensions/ActiveDirectoryOptionsExtensions.cs`
- `tests/LabAuthServer.UnitTests/LabAuthServer.UnitTests.csproj`
- `tests/LabAuthServer.IntegrationTests/AuthenticationTests.cs`
- `docs/Authentication_Design.md`

**Architecture impact:**
- Production dependency direction remains Domain <- Application <- Infrastructure <- Api.
- Typed failure metadata is defined in Application.
- LDAP connection execution remains in Infrastructure.
- Tests reference Infrastructure only to exercise its isolated authentication seam.
- No new production project reference or package was added.

**Testing:**
- Authentication service tests cover valid and invalid UPN domains, credential forwarding, typed failures, strict LDAPS configuration, and cancellation.
- Controller tests cover 401, 503, 504, and 500 mappings, generic error details, successful response shape, and HTTPS enforcement.
- Authentication/controller tests do not contact the live DC. RootDSE connectivity tests remain the live directory boundary.

## Approved packages

The following packages are limited to `LabAuthServer.Infrastructure`:

- `System.DirectoryServices.Protocols` 10.0.0 — LDAPS connectivity.
- `Microsoft.Extensions.Logging` 10.0.0 — `ILogger<T>` structured logging.
- `Microsoft.Extensions.Options` 10.0.0 — `IOptions<T>` configuration injection.

No other NuGet packages were added for Phase 3 remediation.

## Verification

On 2026-08-31, the required `dotnet restore`, `dotnet build LabAuthServer.slnx`, and `dotnet test LabAuthServer.slnx` commands completed successfully.

- Build: succeeded with zero warnings and zero errors.
- Tests: 37 passed, 0 failed, 0 skipped.
- Authentication and controller tests do not require a live DC; RootDSE connectivity tests remain the live directory boundary.

## Deliberately not implemented

The following remain outside the approved Phase 3 scope:

- JWTs and refresh tokens
- Authorization and access control
- Database integration
- Secrets management
- MFA
- Rate limiting and brute-force protection
- Session management
- User directory lookup

## Historical Phase 4 Planning Status

Before approval, Phase 4 token and authorization design required separate architect approval before implementation. This historical status was superseded by the approval recorded below.

## Governance foundation

Repository governance is defined by `AGENTS.md`, `docs/Architecture.md`, `docs/Development_Plan.md`, and `docs/Coding_Standard_and_SOP.md`. Architectural, dependency, package, authentication, database, and requirement changes require architect approval.

## Historical Phase 4 Draft Status

Drafted 2026-08-31 in `docs/Token_and_Authorization_Design.md`, before the Architect Approval Record was established.

- Phase 3 Authentication remediation is complete.
- Phase 4 Token and Authorization Design has been drafted for architect review.
- Phase 4 implementation is blocked pending explicit approval of the token, signing, authorization, refresh-token, revocation, configuration, deployment, and testing decisions documented in the design.
- No source-code changes, project-reference changes, signing keys, database changes, or package changes were made in Phase 4.
- JWT, authorization, refresh tokens, signing keys, database storage, secrets management, MFA, and related implementation remain unimplemented.

## Phase 4 Architect Approval

Approved 2026-08-31 and recorded in `docs/Phase4_Architect_Approval_Record.md`.

- Phase 3 Authentication remediation remains complete.
- Phase 4 Token and Authorization Design has been architect-approved.
- All 49 Phase 4 decisions are approved according to the Architect Approval Record.
- Phase 4 implementation is authorized only within the approved scope and constraints.
- New packages, project references, architecture or dependency-direction changes, database implementation, refresh tokens, MFA, SSO/federation, rate limiting, brute-force protection, unauthorized secrets-management changes, Phase 3 behavior changes, and unrelated security controls remain prohibited without separate approval.
- The next milestone is Phase 4 implementation planning and design decomposition before implementation code is written.

## Current Governance Status

- Phase 3 Authentication: COMPLETE.
- Phase 4 Token and Authorization Design: APPROVED.

## Phase 4 current implementation reconciliation

The historical planning notes below are retained for traceability. The repository
currently contains the approved token, certificate-store signing, JWT validation,
AD group mapping, named policy, and protected-resource implementation slices.

- Certificate-backed signing resolves the active certificate directly from the configured store and uses `GetRSAPrivateKey()` without PKCS#12 export.
- Active and previous certificate thumbprints can be selected by `kid` during a bounded overlap; invalid, expired, or unknown keys fail closed.
- The API exposes `GET /api/v1/protected` under the existing Reader policy for authorization verification.
- Local verification now covers certificate rotation, JWT key overlap, protected-resource authorization, and the existing authentication/token slices; the current result is 107 passed, 0 failed, and 0 skipped.
- Phase 4 Implementation: COMPLETE for the approved Step 8 token issuance and Step 9 login response redesign scope.
- Phase 4 Implementation Planning: COMPLETE for the approved implementation slices executed in this branch.
- Phase 4 implementation is authorized only according to `docs/Phase4_Architect_Approval_Record.md` and its approved constraints.

## Phase 4 Step 7: Approved AD group-to-role mapping

Implemented 2026-08-31 in the approved scope for AD group-to-role mapping and default-deny policy configuration.

- Added the application contract and infrastructure implementation for deterministic AD group-to-role mapping.
- Enforced fail-closed validation for empty values, invalid role names, and ambiguous normalized group entries.
- Registered the mapping service behind the existing Authorization options registration pattern without introducing JWT issuance or validation middleware.
- Verified mapping behavior with focused unit tests that exercise valid mapping, unmapped groups, default deny, invalid configuration, and deterministic ordering.

## Phase 4 Step 8: Approved application token issuance

Implemented 2026-08-31 in the approved scope for application-owned token issuance.

- Added the application token issuance contract and service to generate signed bearer tokens from the approved claims shape.
- Registered the token service and signing dependencies behind the existing API configuration pattern without introducing JWT validation middleware or authorization enforcement.
- Kept the signing key and token configuration within the approved application/infrastructure boundary and did not add database or refresh-token infrastructure.
- Verified token issuance behavior with focused unit/integration tests covering claims, signing, expiration metadata, and configuration validation.

## Phase 4 Step 9: Approved login response redesign

Implemented 2026-08-31 in the approved scope for the successful login response contract.

- Updated the login endpoint to return the approved token payload containing accessToken, tokenType, and expiresAt.
- Preserved the public login flow and HTTPS enforcement semantics while leaving authentication and directory-error behavior unchanged.
- Kept Step 10+ authorization enforcement, JWT validation middleware, and database/refresh-token features outside scope.
- Verified the successful login response contract and the orchestration path through integration tests asserting the approved payload and downstream service calls.

Later Phase 4 steps remain outside the approved scope for this task, including JWT validation middleware, resource authorization enforcement, key rotation workflows, and database-backed refresh-token handling.

## Phase 4 Step 10: Approved JWT validation middleware and resource authorization slice

Implemented 2026-08-31 in the approved scope for API-bound JWT validation and authenticated-resource authorization.

- Registered JWT bearer authentication using the ASP.NET Core framework pipeline with explicit issuer, audience, lifetime, algorithm, and key-id validation configured at the HTTP boundary.
- Kept the signing-key resolution behind the existing Infrastructure abstraction and retained fail-closed validation semantics without expanding into database, refresh-token, or LDAP validation behavior.
- Applied the default authorization policy as an authenticated-user requirement while preserving the public login and health endpoints as anonymous.
- Confirmed the solution remains within the Step 10 scope and does not advance into Step 11+ designs, refresh-token handling, or database-backed authorization state.

**Files created:**
- `src/LabAuthServer.Api/Extensions/JwtBearerAuthenticationOptions.cs`

**Files modified:**
- `src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs`
- `src/LabAuthServer.Api/Program.cs`
- `src/LabAuthServer.Infrastructure/Security/ProtectedSigningKeyProvider.cs`
- `src/LabAuthServer.Infrastructure/Security/IProtectedSigningKeyProvider.cs`
- `src/LabAuthServer.Infrastructure/Security/AuthorizationPolicyOptions.cs`
- `src/LabAuthServer.Infrastructure/Security/TokenOptions.cs`
- `docs/Project_Status.md`

**Architecture impact:**
- JWT validation remains at the API boundary using ASP.NET Core middleware, consistent with the approved Phase 4 design.
- The key-provider abstraction remains in Infrastructure; no new project reference or dependency-direction change was introduced.
- Public login and health endpoints remain intentionally anonymous, while protected resources require authenticated bearer tokens by default.
- No Step 11+ implementation work was started.

**Verification:**
- `dotnet build LabAuthServer.slnx` succeeded.
- `dotnet test LabAuthServer.slnx` succeeded.
- Test result: 84 passed, 0 failed, 0 skipped.

## Phase 4 Step 10.1: Approved certificate-store signing implementation

Implemented in the approved scope for certificate-backed JWT signing using the Windows certificate store.

- Added certificate-store metadata to the token configuration model and validator: store location, store name, and thumbprint normalization.
- Registered a certificate-backed signing key provider in the API composition layer, replacing the generic key-only provider while preserving the approved Infrastructure boundary.
- Resolved the certificate from `Cert:\LocalMachine\My` by thumbprint and fail-closed when the certificate is missing, private-keyless, or invalid.
- Kept the JWT validation pipeline, authorization model, and Active Directory behavior unchanged beyond the signing provider required to support approved certificate-backed issuance.
- Verified the implementation with unit tests covering valid certificate resolution, missing thumbprint handling, private-keyless certificate rejection, and normalized thumbprint parsing.

**Architecture impact:**
- JWT token signing remains behind the existing Infrastructure security abstraction and does not add new packages or dependency-direction changes.
- No production server or certificate-store mutation was performed; this remains a local-development implementation aligned to the approved configuration semantics.

**Verification:**
- `dotnet restore`, `dotnet build LabAuthServer.slnx`, and `dotnet test LabAuthServer.slnx` completed successfully after the implementation.
- Final test result: 91 passed, 0 failed, 0 skipped.

## Phase 5: LDAPS authentication service

Implemented 2026-09-03 using the existing LDAP and DPAPI provider boundaries.

- Added the configured `CN=Users,DC=lab,DC=local` user search base.
- Added service-account lookup over LDAPS/TCP 636 using the existing `ILdapServiceAccountCredentialProvider` abstraction.
- Escaped the UPN in the LDAP filter, rejected ambiguous/not-found users, rejected disabled accounts, and authenticated the resolved user distinguished name over the same TLS connection.
- Returned only safe user identity metadata on successful authentication; passwords, DPAPI contents, LDAP connections, and key material remain outside application results and logs.
- Preserved LDAPv3, certificate validation, explicit timeout/cancellation handling, connection disposal, generic external failure messages, and the existing DI registration pattern.
- Added focused coverage for provider failure, identity propagation, user search-base validation, LDAPS configuration, cancellation, failure mapping, and filter escaping.

**Verification:**

- `dotnet restore .\LabAuthServer.slnx` succeeded.
- `dotnet build .\LabAuthServer.slnx -c Release --no-restore` succeeded with zero warnings and zero errors.
- `dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo` succeeded: 115 passed, 0 failed, 0 skipped (76 unit, 39 integration).
- Security review found no actual credentials, private keys, PEM/PFX/key files, or stale `C:\0001\Project\Lab\LabAuthServer` source/configuration.
- IIS was not deployed or modified; the existing production deployment and approved DPAPI architecture remain unchanged.

## Phase 6: AD group-to-role mapping

Implemented 2026-09-03 using the existing application mapping boundary and read-only LDAP service.

- Approved mappings are `GG-APP-ADMIN` to `Administrator`, `GG-APP-APPROVER` to `Operator`, and `GG-APP-USER`/`GG-APP-REPORT` to `Reader`.
- Role precedence is explicit: `Administrator > Operator > Reader`.
- Group matching is case-insensitive, trims identifiers, deduplicates input, returns no role for unknown or missing membership, and rejects malformed input or invalid mapping configuration.
- LDAP group lookup uses the existing DPAPI-backed service-account provider over LDAPS/TCP 636 and the configured `UserSearchBaseDn`; no anonymous fallback or nested-group expansion was added.
- No JWT issuance, authorization-policy changes, IIS deployment, AD infrastructure change, or Phase 7 work was performed.

**Verification:**

- `dotnet restore .\LabAuthServer.slnx` succeeded.
- `dotnet build .\LabAuthServer.slnx -c Release --no-restore` succeeded with zero warnings and zero errors.
- `dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo` succeeded: 127 passed, 0 failed, 0 skipped (88 unit, 39 integration).
- Security scan found no credentials, private-key files, PEM/PFX/key material, or stale `C:\0001\Project\Lab\LabAuthServer` paths. The sole private-key text match is an existing unit-test placeholder explicitly permitted by the Phase 6 requirements.
- IIS remains started with `ApplicationPoolIdentity`, bindings unchanged, and `Current` unchanged. The DPAPI secret contents were not accessed.

## Phase 7: JWT token issuance

Implemented 2026-09-03 using the existing Application token service and Infrastructure certificate-store signing boundary.

- JWT issuance now requires exactly one approved role from the Phase 6 precedence result: `Administrator`, `Operator`, or `Reader`.
- Tokens contain the approved `iss`, `aud`, `sub`, `iat`, `nbf`, `exp`, unique `jti`, and singular `role` claims. Passwords, LDAP credentials, DPAPI data, certificate details, and private-key material are excluded.
- Signing continues to use the configured RSA certificate provider for `LocalMachine\\My`; no fallback signing key or export path was added.
- Missing/invalid roles and invalid signing requests fail closed before token signing. Existing certificate/provider failures remain controlled by the current signing boundary and controller error handling.
- The login flow consumes the Phase 6 precedence winner without changing Phase 5 LDAP authentication or Phase 6 group mapping.

**Configuration:**

- Issuer: `https://DC01.lab.local`
- Audience: `LabAuthServer.API`
- Lifetime: `01:00:00` (3600 seconds)
- Signing certificate store: `LocalMachine\\My`
- Signing certificate reference remains configuration-only; private-key material is not stored in source or output.

**Verification:**

- `dotnet restore .\\LabAuthServer.slnx` succeeded.
- `dotnet build .\\LabAuthServer.slnx -c Release --no-restore` succeeded with zero warnings and zero errors.
- `dotnet test .\\LabAuthServer.slnx -c Release --no-build --nologo` succeeded: 129 passed, 0 failed, 0 skipped.
- Scoped security scan found no credentials, DPAPI contents, PFX/PEM/key files, stale paths, or access-token logging. Existing test placeholders and public-certificate test conversion remain non-secret.
- IIS was not modified or restarted. `Current` is unchanged. DPAPI contents were not accessed. Deployment was not performed.

# PHASE 8 – JWT TOKEN VALIDATION – COMPLETE

Implemented 2026-09-03 by extending the existing JWT bearer configuration and certificate-store key-validation boundary.

- RSA signature validation remains tied to the configured signing-key provider; no HMAC fallback or alternate signing key was added.
- Validation requires the approved issuer `https://DC01.lab.local`, audience `LabAuthServer.API`, configured algorithm, signed tokens, expiration, and the configured UTC clock-skew policy.
- Phase 7 structure is enforced: exactly one non-empty `sub`, `jti`, `iat`, and `nbf`, plus exactly one approved `role` claim (`Administrator`, `Operator`, or `Reader`).
- Invalid signature, issuer, audience, lifetime, required claims, role, `kid`, or token format fails authentication without exposing token contents or sensitive validation details.
- No AD lookup, group mapping, Phase 9 authorization behavior, DPAPI change, certificate export, or deployment was added.

**Verification:**

- `dotnet restore .\\LabAuthServer.slnx` succeeded.
- `dotnet build .\\LabAuthServer.slnx -c Release --no-restore` succeeded with zero warnings and zero errors.
- `dotnet test .\\LabAuthServer.slnx -c Release --no-build --nologo` succeeded: 135 passed, 0 failed, 0 skipped (90 unit, 45 integration).
- Security scan over `src`/`tests` found no credentials, DPAPI contents, private-key files, stale paths, raw JWT logging, or private-key export. Existing permitted test placeholders and public-certificate test conversion were classified as non-secret.
- IIS configuration and bindings remain unchanged. `Current` remains unchanged and contains no DPAPI file. DPAPI contents were not accessed. Certificate private-key material was not accessed or exported.
- Deployment: NOT PERFORMED.

=== PHASE 9 – AUTHORIZATION POLICIES AND PROTECTED RESOURCES ===

Implemented and validated 2026-09-03 using the existing authorization architecture from the approved Token and Authorization Design.

- The default policy requires an authenticated principal produced by the existing JWT bearer validation pipeline.
- Named policies remain available for `Reader`, `Operator`, and `Administrator` roles and consume the already validated singular JWT `role` claim.
- The protected API resource remains explicitly protected by the Reader policy; unauthenticated or invalid-token requests fail with 401, while authenticated users with an insufficient valid role receive 403.
- Login and health remain the only explicitly anonymous endpoints in the existing API flow. No AD reauthentication, group lookup, role remapping, JWT issuance, or certificate/DPAPI behavior was added.
- Middleware order remains authentication before authorization, and default-deny behavior remains enforced through the existing policy registration.

**Verification:**

- Focused Phase 9 tests passed: policy registration and protected-resource authorization coverage.
- `dotnet restore .\\LabAuthServer.slnx` succeeded.
- `dotnet build .\\LabAuthServer.slnx -c Release --no-restore` succeeded with zero warnings and zero errors.
- `dotnet test .\\LabAuthServer.slnx -c Release --no-build --nologo` succeeded: 136 passed, 0 failed, 0 skipped (90 unit, 46 integration).
- Scoped source/tests security scan found no credentials, stale paths, secret files, or private-key files. Existing permitted test placeholder text and non-secret key-identifier logging were classified as non-secret.
- IIS site, app pool, identity, bindings, and physical path remain unchanged. `Current` remains unchanged and contains no DPAPI file.
- DPAPI contents were not accessed. Certificate private-key material was not accessed or exported. Deployment: NOT PERFORMED.

=== PHASE 10 – API ENDPOINTS AND CONTRACTS ===

Implemented and validated 2026-09-03 against the approved Token and Authorization Design. No dedicated Phase 10 specification file exists in the repository; the approved design and existing phase sequence identify this slice as the API endpoint surface.

- Verified the existing HTTPS login endpoint, anonymous health endpoint, and protected resource endpoint use the completed Phase 5-9 authentication, JWT, and authorization boundaries.
- Preserved the existing API response/error conventions, explicit anonymous endpoints, protected Reader resource, JWT role consumption, and authentication-before-authorization middleware order.
- Added focused regression coverage for policy registration and authenticated-but-insufficient protected-resource access. No duplicate authentication, LDAP, role-mapping, JWT, or policy implementation was introduced.
- Phase 11 was not started.

**Verification:**

- Focused Phase 10 tests passed: 15 protected-resource integration tests and 4 policy-registration unit tests.
- `dotnet restore .\\LabAuthServer.slnx` succeeded.
- `dotnet build .\\LabAuthServer.slnx -c Release --no-restore` succeeded with zero warnings and zero errors.
- `dotnet test .\\LabAuthServer.slnx -c Release --no-build --nologo` succeeded: 136 passed, 0 failed, 0 skipped (90 unit, 46 integration).
- Scoped `src`/`tests` security scan found no hard-coded service passwords, secret files, stale paths, PFX/PEM/KEY files, or credential leakage. Existing permitted test placeholder text and non-secret key-identifier logging were classified as non-secret.
- IIS site, app pool, identity, bindings, and physical path remain unchanged. `Current` remains unchanged with 29 files and no DPAPI file.
- DPAPI contents were not accessed. Certificate private-key material was not accessed or exported. Deployment: NOT PERFORMED.
- Health check: HTTP `307` redirect to HTTPS; HTTPS `200 {"status":"Healthy"}`.

## PHASE 11 - LOGGING & ERROR HANDLING

**Status:** COMPLETE  
**Completion date:** 2026-09-04  
**Design:** `docs/Phase11_Logging_and_Error_Handling_Design.md`

### 1. Objective

Implemented an enterprise-grade, data-minimized SQL Server audit architecture and centralized safe error-handling flow while preserving all completed Phase 0-10 behavior.

### 2. Current state

SQL Server was verified as the running default `MSSQLSERVER` instance. `LabAuthServer` is now online with the approved Phase 11 schemas, tables, reference data, indexes, writer procedure, application database role, and verified IIS virtual-account mapping. The source solution retains the existing four-layer architecture, console/debug `ILogger<T>` providers, and ProblemDetails boundary, now extended with the approved audit repository, correlation middleware, safe exception boundary, and event instrumentation.

### 3. Proposed architecture

The proposed boundary is ASP.NET Core -> Application audit service -> Infrastructure SQL repository -> `Audit.usp_WriteAuditEvent` -> SQL Server. The application will pass approved typed values only; it will not construct dynamic SQL or directly modify audit tables.

### 4. Proposed schemas and tables

- `Reference.EventTypes` will hold the controlled event catalog.
- `Audit.AuditEvents` will be append-only for the application identity and will store bounded event metadata, correlation/request identifiers, outcome, route, and minimized details.
- `Application` is reserved as the application schema boundary; no application error table was created because diagnostics remain in `ILogger` and safe audit events.

### 5. Proposed stored procedures

- `Audit.usp_WriteAuditEvent` will validate event codes, lengths, status codes, approved roles, UTC timestamps, nulls, and JSON; use a short atomic transaction and `TRY/CATCH`; and return only the new `AuditEventId` and approved timing data.
- `Audit.usp_PurgeAuditEvents` is a future DBA-controlled maintenance procedure, contingent on explicit retention and archival approval. It is not created or scheduled.

### 6. Proposed permissions

Use SQL Server Windows Authentication. The verified `IIS APPPOOL\\LabAuthServerAppPool` identity is mapped to `LabAuthServer`, added to `LabAuthServer_AuditWriter`, and granted only `EXECUTE` on `Audit.usp_WriteAuditEvent`. Effective checks showed no audit-table `SELECT`/`INSERT` and no purge `EXECUTE` for that principal. No maintenance identity or purge permission was created.

### 7. Proposed event catalog

The design evaluates authentication success/failure and LDAP failure, authorization granted/denied, safely classified token failures, unhandled exceptions, and validation errors. Final event inclusion, severity, volume controls, and privacy fields require approval. High-volume framework token failures should not store token data and may require aggregation.

### 8. Error handling and correlation

The proposed flow adds an early validated correlation context, safe global exception mapping, minimized audit events, and internal structured diagnostics. A valid GUID correlation ID may be reused from the client; missing, malformed, or oversized values receive a new server-generated ID. The ID is returned in `X-Correlation-ID`, while ASP.NET `TraceIdentifier` is stored separately as `RequestId`. Responses never expose stack traces, SQL/LDAP/certificate details, secrets, or filesystem paths.

### 9. Data security and retention

Passwords, LDAP service-account credentials, JWTs, refresh tokens, Authorization headers, DPAPI contents, certificate private keys, private-key material, sensitive request bodies, raw LDAP responses, and secret-bearing diagnostics must never be stored. The proposed starting retention period is 12 months online with archival/legal-hold decisions required before purge. Partitioning and stronger tamper evidence are deferred pending volume and threat-model evidence.

### 10. Test strategy

The plan covers unit tests for event validation, mapping, filtering, correlation, and error mapping; isolated SQL integration tests for procedure execution, invalid/null/duplicate behavior, rollback, unavailable database, and least privilege; API integration tests; and security tests proving prohibited data never reaches rows, logs, or responses. Existing Phase 0-10 tests remain regression gates.

### 11. Implementation sequence

Completed: baseline freeze; database and schemas; reference data, tables, indexes, and writer procedure; least-privilege Windows access; Application audit contracts; Infrastructure repository; non-secret configuration; correlation and exception boundaries; authentication/authorization/security instrumentation; tests; build/security validation; and deployment-readiness documentation. Retention/purge remains deferred pending separate ownership and archival approval.

### 12. Risks and approval items

Open decisions include database/instance ownership, synchronous audit failure behavior, final event and privacy fields, JSON limits, timestamp ownership, idempotency, correlation header policy, actual IIS identity mapping, retention/archive/legal hold, SQL provider approval, and whether stronger controls or an error table are needed. Risks include database latency/outage, high-volume security events, privacy obligations, classification errors, permission drift, and premature data deletion.

### 13. Completion history statement

On 2026-09-04, Phase 11 implementation began after explicit approval and completed all mandatory gates. The default `MSSQLSERVER` instance was verified; `LabAuthServer`, the `Audit`/`Reference`/`Application` schemas, `Reference.EventTypes`, `Audit.AuditEvents`, five documented indexes, `Audit.usp_WriteAuditEvent`, the `LabAuthServer_AuditWriter` role, and the verified IIS virtual-account mapping were created. Application audit contracts/validation, the SQL repository, non-secret SQL configuration, correlation middleware, safe global exception handling, and login/protected-resource/JWT event instrumentation were added. `Microsoft.Data.SqlClient` 6.1.2 was added only to Infrastructure for the approved Windows-authenticated stored-procedure boundary.

Validation passed: the writer returned an `AuditEventId`, invalid JSON was rejected, effective IIS-principal permissions were writer `EXECUTE=1`, audit table `SELECT=0`, audit table `INSERT=0`, purge `EXECUTE=0`, focused audit tests passed, and the Phase 11 Release suite passed 99 unit plus 46 integration tests. A database sensitive-data scan found no prohibited secret/token data. `Audit.usp_PurgeAuditEvents` and SQL Agent scheduling remain deferred pending retention/archive ownership. Phase 11 is **COMPLETE**; subsequent completion records are maintained in the Phase 12-18 documents.
