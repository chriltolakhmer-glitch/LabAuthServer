# Phase 4 Implementation Plan

**Status:** Historical implementation plan; approved implementation slices are present in the repository
**Phase:** Phase 4 — Token & Authorization  
**Authority:** `docs/Phase4_Architect_Approval_Record.md`  
**Design basis:** `docs/Token_and_Authorization_Design.md`  
**Date:** 2026-08-31

> This document records the approved implementation plan. Current implementation status is maintained in `docs/Project_Status.md`; refresh tokens, revocation storage, databases, MFA, SSO, rate limiting, and brute-force protection remain out of scope.

## 1. Phase 4 Objectives and Scope

### Objective

Add the approved token and authorization capabilities after successful Phase 3 LDAP authentication:

- Issue short-lived JWT bearer access tokens.
- Validate tokens at the API HTTP boundary.
- Map explicitly approved Active Directory groups to application-owned roles.
- Apply named authorization policies with default deny for protected endpoints.
- Use RSA signing through a protected external key-storage boundary.
- Keep the implementation incremental, testable, and within the approved architecture.

### Scope

The implementation is limited to the 49 approved decisions in the Architect Approval Record. The current login endpoint will evolve from returning `authenticated: true` to returning the approved access-token response after successful Phase 3 authentication.

### Approval boundary

A design decision, package, project reference, configuration category, security control, or behavior not described in the approval record is outside this plan and requires separate approval before implementation.

## Approved Decision Traceability (1-49)

The following traceability list preserves every approved recommendation from the Architect Approval Record and identifies where it is implemented in this plan.

1. JWT bearer access token: implement in Steps 8-10; see Sections 5-6.
2. LabAuthServer self-issued token authority: implement in Application token issuance; see Sections 4-5 and 11.
3. Stable HTTPS issuer owned by LabAuthServer: validate in token configuration; see Sections 6 and 10.
4. Distinct issuer values per environment: configure as non-secret environment values; see Section 10.
5. One stable LabAuthServer API audience: configure and validate exactly; see Section 6.
6. Reject multiple audiences initially: enforce single-audience validation; see Section 6.
7. Stable directory object identifier, otherwise normalized UPN, as subject: implement subject selection; see Sections 5-6.
8. Case-insensitive UPN comparison with minimal directory data: preserve Phase 3 normalization and minimize claims; see Sections 3 and 6.
9. No ordinary request-time AD revalidation: do not add request-time directory calls; see Sections 5 and 8.
10. Registered claims `iss`, `aud`, `sub`, `iat`, `exp`, with conditional `nbf`: implement exact claim construction; see Section 6.
11. Standard `role` and `scope` names only when required: use approved names conditionally; see Sections 6 and 8.
12. Small bounded claim allowlist and explicit maximum: validate claim and token sizes; see Sections 6 and 10.
13. Omit `jti` for stateless revocation: do not issue `jti` initially; see Sections 6 and 23.
14. Issue only approved application roles: allowlist role values; see Section 8.
15. Minimum approved claim type and no raw LDAP data: implement claim minimization; see Sections 6 and 16.
16. Short-lived production tokens with environment values: configure and test lifetime; see Sections 6 and 10.
17. Small bounded clock skew, such as five minutes: configure and test bounded skew; see Sections 6 and 10.
18. Synchronized UTC host clocks: make time synchronization a deployment requirement; see Section 37.
19. RSA asymmetric signing: implement the RSA signer boundary; see Section 7 and Step 5.
20. Managed or HSM-backed production key store: keep private-key access behind the approved external boundary; see Section 7.
21. Runtime-generated or approved development test keys: use only test-time keys; see Section 17.
22. Dedicated least-privilege issuer identity: configure key access for that identity; see Sections 7 and 37.
23. Protected/non-exportable keys where supported: enforce approved key protection; see Section 7.
24. Audit key access and failures without key material: implement redacted operational auditing; see Sections 7 and 38.
25. Explicit platform/security key ownership and runbooks: document operational ownership; see Sections 7 and 37.
26. Scheduled rotation shorter than the accepted risk window: implement scheduled rotation configuration; see Section 14 and Step 6.
27. Retain previous public key through token expiry: implement bounded overlap; see Sections 7 and 24.
28. Immediate replacement with bounded public-key retirement: implement emergency rotation behavior; see Sections 7 and 24.
29. Stable random non-secret unique `kid`: generate and validate issuer-unique identifiers; see Section 7.
30. Publish approved public keys and reject unknown `kid`: implement public-key selection and rejection; see Sections 6-7.
31. Return access token, token type, and expiration metadata only: change login response accordingly; see Section 13 and Step 9.
32. Application owns issuance; Infrastructure signs behind an abstraction: preserve layer ownership; see Sections 4, 11, and 12.
33. API middleware validates tokens at the HTTP boundary: implement only in API composition; see Section 9 and Step 10.
34. Application-owned explicit roles with default deny: define role and policy contracts; see Section 8.
35. Explicit allowlist of approved AD groups: implement configuration-backed mapping; see Section 8 and Step 7.
36. Reject implicit nested-group expansion: fail closed for unexpanded nesting; see Section 8.
37. Enforce account state at authentication and bound stale-token exposure: preserve Phase 3 checks and short lifetimes; see Sections 3 and 8.
38. Bound group claims and fail closed on approved-limit overflow: validate group data before claim issuance; see Section 8.
39. Map roles from approved groups and add scopes only when necessary: implement the approved mapping strategy; see Section 8.
40. Named API policies with default deny: register named policies and protect endpoints; see Section 9 and Step 11.
41. Explicit public endpoints with standard 401/403 responses: allow only approved public endpoints; see Section 9.
42. Versioned non-secret group-to-role configuration allowlist: version and validate mapping configuration; see Sections 8 and 10.
43. Apply mapping changes on issuance and avoid long-lived caches: resolve mappings during issuance; see Section 8.
44. No refresh tokens initially; require reauthentication: omit refresh functionality; see Sections 23 and 26.
45. Future refresh-token behavior requires a separate design and approval: keep all refresh work out of this plan; see Section 26.
46. Short-lived stateless tokens with emergency key rotation: omit stateful revocation initially; see Sections 23-24.
47. Defer stateful revocation storage: do not add database or denylist storage; see Sections 23 and 26.
48. External non-secret configuration, approved key storage, fail-closed security configuration, and documented operations: implement configuration and operational boundaries; see Sections 10, 15, 24, and 25.
49. Runtime test keys, isolated tests, no live DC dependency, and no new dependency or architecture change without approval: enforce test and governance boundaries; see Sections 14, 15, 16, 17, 21, and 26.

## 2. Approved Architecture and Dependency Direction

The production dependency direction remains:

```text
Domain <- Application <- Infrastructure <- Api
```

- **Domain:** No Phase 4 changes are expected.
- **Application:** Owns token-issuance and authorization use cases and contracts without HTTP, LDAP protocol, or key-store details.
- **Infrastructure:** Implements RSA signing, protected key access, and approved directory-group integration behind Application abstractions.
- **API:** Owns token validation middleware, endpoint composition, token response shaping, and policy enforcement.

No circular dependency, dependency-direction change, new production project, or new project reference is planned.

## 3. Current Phase 3 Baseline

Phase 3 is complete and must remain unchanged:

- Login accepts a UPN in the configured `lab.local` domain.
- Authentication uses direct LDAP bind over LDAPS/TCP 636.
- LDAPv3 is required.
- Normal platform certificate validation is used; no certificate bypass exists.
- Passwords are used only for authentication and are not persisted or logged.
- LDAP failures are typed and map to 401, 503, 504, or 500.
- Authentication/controller tests are isolated from the live DC.

Phase 4 must consume the successful authentication result without changing LDAP connection behavior, UPN validation, password handling, or Phase 3 failure semantics.

## 4. Phase 4 Target Architecture

```text
POST /api/v1/auth/login over HTTPS
        |
        v
Api AuthController
        |
        v
Application ITokenService
        |
        +--> Phase 3 IAuthenticationService
        |
        +--> approved identity and role claims
        |
        v
Infrastructure ITokenSigningService
        |
        v
RSA key provider -> approved managed key store/HSM boundary
        |
        v
JWT access-token response
        |
        v
API bearer-token authentication middleware
        |
        v
Named authorization policies -> application roles
```

The implementation must keep token creation and policy semantics out of controllers and keep cryptographic key access out of Application and Domain.

## 5. Token Issuance Flow

1. Receive a login request over HTTPS.
2. Preserve the existing Phase 3 validation and direct LDAP authentication behavior.
3. On successful authentication, obtain the approved stable subject identifier. Use a directory object identifier when available; otherwise use normalized UPN.
4. Resolve only the approved authorization data. Use the explicit AD-group allowlist and application-role mapping when group data is available through an approved boundary.
5. Build the minimum allowlisted claims.
6. Set the approved issuer, one audience, `iat`, `exp`, and any conditionally approved `nbf` or authorization claims.
7. Sign the token with RSA through the Infrastructure signing abstraction and active `kid`.
8. Return only the access token, token type, and expiration metadata.
9. Never return passwords, raw LDAP responses, sensitive directory attributes, private keys, or unnecessary identity data.

Invalid credentials must remain generic. Directory unavailability, timeout, and unexpected failures must retain the Phase 3 status mappings.

## 6. JWT Structure and Validation Requirements

### Issuance claims

Required baseline claims:

- `iss`: stable HTTPS issuer identifier owned by LabAuthServer.
- `aud`: one stable LabAuthServer API audience.
- `sub`: stable directory object identifier when available; otherwise normalized UPN.
- `iat`: UTC issue time.
- `exp`: short-lived expiration time.

Optional claims are permitted only when required by the approved models:

- `nbf`: only if needed.
- `role`: only approved application roles.
- `scope`: only when demonstrated necessary.
- `groups`: only the minimum approved mapped data, never a raw membership dump.
- `jti`: omitted for the initial stateless revocation model.

### Validation

API middleware must:

- Require a bearer token for protected endpoints.
- Validate the RSA signature and fixed approved algorithm allowlist.
- Validate exact issuer and one audience.
- Validate `exp`, `iat`, and `nbf` when present.
- Apply the bounded clock-skew tolerance.
- Resolve the public key by `kid`.
- Reject unsigned tokens, algorithm changes, unknown keys, malformed tokens, oversized tokens, wrong issuer, wrong audience, and expired/not-yet-valid tokens.
- Avoid request-time Active Directory revalidation for ordinary requests.

## 7. Signing and Key-Management Implementation Boundary

### RSA signing

Use RSA asymmetric signing. The token issuer accesses the private key; validators receive public verification material only where needed.

### Signing abstraction

Application should depend on a signing contract, for example:

```text
Application: ITokenSigningService
Infrastructure: RsaTokenSigningService
Infrastructure: IProtectedSigningKeyProvider
```

The exact names may be finalized during implementation without changing the approved responsibilities.

The Application layer must not reference RSA APIs, certificates, HSM SDKs, or key-store SDKs. Infrastructure owns those concerns.

### Production key-storage boundary

Production private keys must come from an approved managed key store or HSM-backed store. The implementation must not read private keys from source control or ordinary configuration files.

The issuer identity must have least-privilege access. Key access and failures may be audited, but key material must never be logged or exposed through diagnostics.

### Non-production/test keys

Tests use runtime-generated keys or an approved development key store. No private test key is committed. Test fixtures must be deterministic in behavior without reusing production key material.

## 8. Authorization Model and AD-Group-to-Role Mapping

### Roles

Roles are application-owned, explicitly named, and evaluated with default deny. The initial role catalogue must be represented in Application constants or an approved non-secret configuration boundary once the implementation slice is decomposed.

### AD groups

Only an explicit allowlist of approved AD groups may provide authorization membership. Implicit nested-group expansion is rejected initially. Group claims must be bounded, and oversized membership data must fail closed.

### Mapping

The initial mapping is a versioned non-secret configuration allowlist:

```text
approved AD group identifier -> application role
```

Mapping changes apply at token issuance. Long-lived authorization caches are avoided initially. Account state is enforced during authentication, while short token lifetimes bound stale-membership exposure.

No directory-wide group dump, arbitrary LDAP attribute mapping, or request-time group lookup is planned unless separately approved.

## 9. API Authentication and Authorization Middleware Boundary

API middleware is responsible for:

- Reading the `Authorization: Bearer` header.
- Validating the token using the approved issuer, audience, RSA public key, algorithm, time, size, and `kid` rules.
- Creating the authenticated principal from approved claims.
- Returning standard 401 responses for missing or invalid authentication.
- Applying named authorization policies.
- Returning standard 403 responses when an authenticated principal lacks an approved policy requirement.

The login endpoint remains explicitly public. Health remains public only if the approved endpoint policy permits it. Other endpoints default to protected once authorization is introduced.

## 10. Configuration Requirements and Validation

Planned non-secret configuration categories:

- Environment-specific issuer.
- One API audience.
- Short access-token lifetime per environment.
- Small clock-skew tolerance, such as five minutes.
- RSA algorithm and active `kid` policy.
- Public-key selection/publication metadata.
- Protected key-store reference, never private key material.
- Approved AD-group-to-role allowlist.
- Named authorization policy definitions.

Validation must fail closed for missing or invalid security configuration. Configuration values must be bounded and validated at startup where possible.

No passwords, private keys, certificates, tokens, or other secrets may be added to `appsettings.json` or source control. Any key-store integration beyond the approved managed/HSM boundary requires separate approval.

## 11. Required Application Interfaces

These are planned responsibilities, not source-code changes in this task.

### `ITokenService`

**Project:** `LabAuthServer.Application`  
**Layer:** Application  
**Likely path:** `src/LabAuthServer.Application/Interfaces/ITokenService.cs`  
**Responsibility:** Orchestrate successful authentication into an access-token result; build only approved claims; own the token response contract.  
**Dependencies:** `IAuthenticationService`, approved identity/authorization abstractions, `ITokenSigningService`; no HTTP, LDAP protocol, RSA, or key-store dependency.  
**Tests:** Claim allowlisting, subject selection, expiration, response shape, no-password/no-sensitive-data guarantees, authentication failure propagation.

### `ITokenSigningService`

**Project:** `LabAuthServer.Application`  
**Layer:** Application contract  
**Likely path:** `src/LabAuthServer.Application/Interfaces/ITokenSigningService.cs`  
**Responsibility:** Define signing input/output without exposing cryptographic storage details.  
**Dependencies:** Application token models only.  
**Tests:** Fake signer tests for issuance orchestration; key identifier and signing failure propagation.

### Authorization mapping contract

**Project:** `LabAuthServer.Application`  
**Layer:** Application  
**Likely path:** `src/LabAuthServer.Application/Interfaces/IAuthorizationMappingService.cs`  
**Responsibility:** Translate approved identity/group information to application-owned roles without exposing LDAP details.  
**Dependencies:** Application role and mapping models.  
**Tests:** Allowlist, unknown group, nested-group rejection, group-size limit, default-deny behavior, mapping version behavior.

### Token DTOs and models

**Project:** `LabAuthServer.Application`  
**Layer:** Application  
**Likely paths:** `src/LabAuthServer.Application/DTOs/TokenResponse.cs`, `src/LabAuthServer.Application/DTOs/TokenClaims.cs`, and approved role/policy model files as needed.  
**Responsibility:** Represent the minimum token response and claim inputs.  
**Dependencies:** No HTTP or cryptographic types.  
**Tests:** Serialization, bounded values, required claims, prohibited data.

## 12. Required Infrastructure Implementations

### `RsaTokenSigningService`

**Project:** `LabAuthServer.Infrastructure`  
**Layer:** Infrastructure  
**Likely path:** `src/LabAuthServer.Infrastructure/Security/RsaTokenSigningService.cs`  
**Responsibility:** Sign approved token material with RSA and attach the active non-secret `kid`.  
**Dependencies:** Approved framework/library APIs only; `ITokenSigningService`; protected key provider.  
**Tests:** Runtime test keys, valid signatures, wrong-key rejection, algorithm allowlist, key identifier behavior, no key-material logging.

### `IProtectedSigningKeyProvider` and implementation

**Project:** `LabAuthServer.Infrastructure`  
**Layer:** Infrastructure  
**Likely paths:** `src/LabAuthServer.Infrastructure/Security/IProtectedSigningKeyProvider.cs` and `ProtectedSigningKeyProvider.cs`  
**Responsibility:** Obtain active private signing keys and public validation keys through the approved external key-storage boundary.  
**Dependencies:** Approved external key store/HSM integration only; no source-controlled secrets.  
**Tests:** Key access failure, least-privilege assumptions, active/retired key overlap, unknown `kid`, no key leakage.

### AD group mapping implementation

**Project:** `LabAuthServer.Infrastructure`  
**Layer:** Infrastructure  
**Likely paths:** `src/LabAuthServer.Infrastructure/Identity/AdGroupRoleMappingService.cs` and supporting models.  
**Responsibility:** Read the approved non-secret group allowlist and provide only mapped application roles.  
**Dependencies:** Application authorization contract and existing approved configuration; no database.  
**Tests:** Approved group mapping, unknown group, nested-group rejection, group-size bound, configuration validation.

### Directory identity/group boundary

**Project:** `LabAuthServer.Infrastructure`  
**Layer:** Infrastructure  
**Likely path:** `src/LabAuthServer.Infrastructure/Identity/ActiveDirectoryIdentityService.cs` if required by the approved subject/group implementation.  
**Responsibility:** Obtain only the approved stable directory identifier or group data without changing Phase 3 LDAP authentication behavior.  
**Dependencies:** Existing Infrastructure LDAP abstractions and approved directory protocol package only.  
**Tests:** Subject fallback, minimum data retrieval, no raw directory response propagation, no ordinary request-time revalidation.

## 13. Required API Changes

### Login endpoint

**Project:** `LabAuthServer.Api`  
**Layer:** API  
**Likely file:** `src/LabAuthServer.Api/Controllers/AuthController.cs`  
**Class:** `AuthController`  
**Responsibility:** Call the Application token use case after successful Phase 3 authentication and return access token, token type, and expiration metadata only. Preserve existing HTTPS enforcement and authentication failure mappings.  
**Dependencies:** `IAuthenticationService`, `ITokenService`, API DTOs, ProblemDetails.  
**Tests:** Successful token response, generic invalid credentials, 503/504/500 preservation, HTTPS rejection, no password/token leakage in errors.

### Token middleware registration

**Project:** `LabAuthServer.Api`  
**Layer:** API  
**Likely files:** `src/LabAuthServer.Api/Extensions/TokenAuthenticationExtensions.cs` and `AuthorizationExtensions.cs`  
**Responsibility:** Register JWT bearer validation and named authorization policies at the HTTP boundary.  
**Dependencies:** Only already approved/framework capabilities; no new package without separate approval.  
**Tests:** Issuer/audience/signature/time/algorithm/`kid` rejection, public endpoints, protected endpoint 401/403 behavior, default deny.

### API pipeline composition

**Project:** `LabAuthServer.Api`  
**Layer:** API  
**Likely file:** `src/LabAuthServer.Api/Program.cs`  
**Responsibility:** Add approved middleware/policy composition only after the implementation slice and package availability are confirmed.  
**Dependencies:** Existing API services and approved extension methods.  
**Tests:** Application startup, middleware ordering, public login/health behavior, protected endpoint behavior.

## 14. Required Unit Tests

**Project:** `LabAuthServer.UnitTests`  
**Layer:** Test boundary for Application and approved Infrastructure seams.

Planned test files:

- `tests/LabAuthServer.UnitTests/TokenServiceTests.cs`
- `tests/LabAuthServer.UnitTests/TokenClaimsTests.cs`
- `tests/LabAuthServer.UnitTests/AuthorizationMappingTests.cs`
- `tests/LabAuthServer.UnitTests/RsaTokenSigningServiceTests.cs`
- `tests/LabAuthServer.UnitTests/TokenConfigurationTests.cs`

Required coverage:

- Exact claim allowlist.
- `iss`, `aud`, `sub`, `iat`, `exp`, conditional `nbf`.
- No `jti` for initial stateless revocation.
- Approved role claims only.
- Scope/group/display claim minimization.
- Stable subject and normalized UPN fallback.
- Short lifetime and bounded clock skew.
- RSA signing and `kid` behavior.
- Key rotation overlap and unknown-key rejection.
- Explicit group allowlist, nested-group rejection, group-size fail closed.
- Named policies and default deny.
- Generic errors and prohibited password/sensitive LDAP data.
- No live DC, production key, committed private key, or real password dependency.

## 15. Required Integration Tests

**Project:** `LabAuthServer.IntegrationTests`  
**Layer:** API boundary and configured service composition.

Planned test files:

- `tests/LabAuthServer.IntegrationTests/TokenIssuanceTests.cs`
- `tests/LabAuthServer.IntegrationTests/TokenValidationTests.cs`
- `tests/LabAuthServer.IntegrationTests/AuthorizationPolicyTests.cs`
- `tests/LabAuthServer.IntegrationTests/KeyRotationTests.cs`

Required coverage:

- Successful login returns the approved token response.
- Invalid credentials remain generic 401.
- Directory unavailable, timeout, and unexpected authentication failures retain 503/504/500.
- Wrong issuer, audience, signature, algorithm, `kid`, expiration, and not-before are rejected.
- Multiple audiences are rejected.
- Public login and approved health behavior remain available.
- Protected endpoints require authentication.
- Missing roles or scopes produce standard 403 responses.
- Approved group mappings produce only approved roles.
- Authentication tests do not contact the live DC.
- Key rotation accepts the approved overlap and rejects retired keys after expiry.

## 16. Security Tests

Security testing must verify:

- Passwords never occur in JWT headers, payloads, responses, exceptions, or logs.
- Sensitive LDAP data and raw LDAP responses never occur in claims.
- Tokens never contain private keys, key material, or secrets.
- Unsigned tokens and algorithm confusion attempts are rejected.
- Only approved RSA algorithms and keys are accepted.
- Unknown `kid` values are rejected.
- Wrong issuer and audience are rejected.
- Expired, not-yet-valid, malformed, oversized, and replayed bearer tokens follow the approved controls.
- Token lifetime and clock skew are bounded.
- Private-key access is least privileged and key material is not logged.
- Authorization defaults to deny and unknown groups do not grant roles.
- Nested groups are not implicitly expanded.
- Configuration fails closed when security settings are invalid.
- No production signing keys, real credentials, or secrets are present in test fixtures.

## 17. Test-Key Strategy

- Generate RSA test keys at runtime or use an approved development key store.
- Use separate test keys per fixture or test collection as needed.
- Never commit private test keys.
- Never use production keys in tests.
- Use deterministic token assertions for claims and behavior, not fixed private-key material.
- Provide current and previous public keys for rotation-overlap tests.
- Provide unknown and retired `kid` fixtures.
- Keep the normal suite independent of the live DC.

Creating runtime test keys is test setup, not creation of a production signing key. No real signing key may be created during this planning task.

## 18. Implementation Order with Small, Independently Verifiable Steps

### Step 1 — Freeze and capture Phase 3 contract

Document the unchanged authentication inputs, outputs, error categories, HTTPS requirement, and LDAPS boundary in tests.  
**Verification:** Existing full unit/integration suite; no source behavior changes.

### Step 2 — Define Application token contracts

Add token response, claim-input models, `ITokenService`, `ITokenSigningService`, and authorization mapping contracts.  
**Verification:** Application build and focused unit tests for models and interfaces.

### Step 3 — Add non-secret token configuration models

Define issuer, audience, lifetime, clock-skew, algorithm, `kid`, claim-size, policy, and mapping settings with fail-closed validation.  
**Verification:** Configuration unit tests for valid, missing, invalid, and oversized values.

### Step 4 — Implement isolated claim construction

Build exact registered claims, approved subject fallback, bounded application claims, and prohibited-data tests.  
**Verification:** Focused claim unit tests; no LDAP/network/key-store dependency.

### Step 5 — Implement RSA signing abstraction with runtime test keys

Implement Infrastructure signing and protected-key provider boundaries. Do not hard-code or generate production keys.  
**Verification:** Signing unit tests with runtime keys, algorithm and `kid` checks.

### Step 6 — Implement initial key selection and rotation overlap

Support active key selection, public-key validation material, previous-key overlap until token expiry, unknown-key rejection, and emergency replacement hooks.  
**Verification:** Key rotation tests using runtime fixtures.

### Step 7 — Implement approved AD-group mapping

Read the versioned non-secret allowlist, map approved groups to application roles, reject implicit nesting, bound group data, and fail closed.  
**Verification:** Mapping unit tests without live AD or a database.

### Step 8 — Implement Application token issuance

Orchestrate Phase 3 success into claim construction and RSA signing. Preserve all Phase 3 failures and avoid ordinary request-time AD revalidation.  
**Verification:** Application service tests with faked authentication, mapping, and signer services.

### Step 9 — Change the login success response

Update `AuthController` to return access token, token type, and expiration metadata only. Keep login public and preserve HTTPS/error behavior.  
**Verification:** Controller/API tests with fake Application services; assert no password or unnecessary identity data.

### Step 10 — Register JWT validation at the API boundary

Add bearer-token validation only after confirming required capabilities are available without an unapproved package.  
**Verification:** Token validation integration tests for signature, issuer, audience, algorithm, time, size, and `kid`.

### Step 11 — Add named authorization policies and default deny

Register approved roles, group mappings, named policies, explicit public endpoints, and standard 401/403 responses.  
**Verification:** Authorization integration tests for allowed, denied, anonymous, unknown-group, and missing-claim cases.

### Step 12 — Complete deployment/key operational configuration

Bind approved non-secret settings to the external key-storage boundary, document monitoring, ownership, rotation, incident response, and rollback.  
**Verification:** Startup/configuration tests and deployment-readiness review; no secrets or keys committed.

### Step 13 — Full regression and acceptance verification

Run the complete solution checks, security tests, package/reference audit, and documentation/status review.  
**Verification:** `dotnet restore`, `dotnet build LabAuthServer.slnx`, and `dotnet test LabAuthServer.slnx`.

### Implementation Sequence

`Step 1 → Step 2 → Step 3 → Step 4 → Step 5 → Step 6 → Step 7 → Step 8 → Step 9 → Step 10 → Step 11 → Step 12 → Step 13`

A step must not begin until the previous step’s focused verification passes. Any need for a new package, project reference, Phase 3 change, database, or architectural deviation stops the sequence for separate approval.

## 19. Files Expected to Be Created

These are expected implementation files only; none are created by this planning task.

### Application

- `src/LabAuthServer.Application/Interfaces/ITokenService.cs`
- `src/LabAuthServer.Application/Interfaces/ITokenSigningService.cs`
- `src/LabAuthServer.Application/Interfaces/IAuthorizationMappingService.cs`
- `src/LabAuthServer.Application/DTOs/TokenResponse.cs`
- `src/LabAuthServer.Application/DTOs/TokenClaims.cs`
- `src/LabAuthServer.Application/Services/TokenService.cs`
- `src/LabAuthServer.Application/Validators/TokenOptionsValidator.cs`, if the existing validation structure requires it

### Infrastructure

- `src/LabAuthServer.Infrastructure/Security/RsaTokenSigningService.cs`
- `src/LabAuthServer.Infrastructure/Security/IProtectedSigningKeyProvider.cs`
- `src/LabAuthServer.Infrastructure/Security/ProtectedSigningKeyProvider.cs`
- `src/LabAuthServer.Infrastructure/Identity/AdGroupRoleMappingService.cs`
- Supporting Infrastructure options/models only if required by the approved configuration boundary

### API

- `src/LabAuthServer.Api/Extensions/TokenAuthenticationExtensions.cs`
- `src/LabAuthServer.Api/Extensions/AuthorizationExtensions.cs`
- Additional policy requirement/handler files only if required by the approved named-policy model

### Tests

- `tests/LabAuthServer.UnitTests/TokenServiceTests.cs`
- `tests/LabAuthServer.UnitTests/TokenClaimsTests.cs`
- `tests/LabAuthServer.UnitTests/AuthorizationMappingTests.cs`
- `tests/LabAuthServer.UnitTests/RsaTokenSigningServiceTests.cs`
- `tests/LabAuthServer.UnitTests/TokenConfigurationTests.cs`
- `tests/LabAuthServer.IntegrationTests/TokenIssuanceTests.cs`
- `tests/LabAuthServer.IntegrationTests/TokenValidationTests.cs`
- `tests/LabAuthServer.IntegrationTests/AuthorizationPolicyTests.cs`
- `tests/LabAuthServer.IntegrationTests/KeyRotationTests.cs`

An exact file may be consolidated or split during implementation only where responsibility and architecture remain unchanged. New files outside these responsibilities require review before use.

## 20. Exact Existing Files Expected to Be Modified

These are the only expected existing-file modifications for the approved implementation slices:

- `src/LabAuthServer.Api/Controllers/AuthController.cs` — return the approved token response through `ITokenService`.
- `src/LabAuthServer.Api/Program.cs` — compose approved authentication/authorization middleware and policies.
- `src/LabAuthServer.Api/Extensions/ActiveDirectoryOptionsExtensions.cs` — register approved token, signing, mapping, and policy services if this existing composition extension remains the correct boundary.
- `src/LabAuthServer.Api/appsettings.json` — add non-secret approved baseline configuration only if configuration is intentionally kept there.
- `src/LabAuthServer.Api/appsettings.Development.json` — add non-secret development overrides only if required.
- `tests/LabAuthServer.IntegrationTests/HealthEndpointTests.cs` — only if public endpoint policy behavior requires an assertion update.
- Existing test project files only if a required already-approved project reference is discovered; no such change is currently planned.

Phase 3 LDAP files, infrastructure project files, solution files, and package manifests are not expected to change.

## 21. Package and Project-Reference Impact

- **NuGet:** No new packages are approved by this plan. Existing approved packages remain limited to Infrastructure.
- **Project references:** No new production project reference is planned.
- **Framework APIs:** Use existing .NET/ASP.NET Core capabilities only where available in the current projects.
- **Stop condition:** If JWT bearer validation, RSA/key-store integration, or policy support requires a package not already approved, stop and request separate approval. Do not add it silently.

## 22. Architecture Impact

The approved architecture is extended within existing boundaries:

- Application gains token and authorization contracts/use cases.
- Infrastructure gains RSA signing, protected key access, and approved group mapping implementations.
- API gains token validation middleware and named policy composition.
- Domain remains unchanged.
- Dependency direction remains `Domain <- Application <- Infrastructure <- Api`.
- Phase 3 LDAP authentication remains unchanged.

No database, token persistence, refresh-token persistence, external identity provider, or authorization architecture outside the approval record is introduced.

## 23. Verification Requirements

After each major implementation slice, run the narrowest applicable check first:

- Application contracts/models: focused Application/unit tests.
- Claim construction: focused claim tests.
- RSA signing: focused signing/key tests with runtime keys.
- Group mapping: focused mapping tests.
- Token issuance: focused Application/controller tests.
- Token validation: focused API integration tests.
- Authorization: focused policy integration tests.
- Final slice: full restore, build, and test.

Required final commands:

```text
dotnet restore
dotnet build LabAuthServer.slnx
dotnet test LabAuthServer.slnx
```

Expected final result: zero warnings, zero errors, and all tests passing. Tests must not require a live DC, production signing key, committed private key, real password, or database.

## 24. Rollback Considerations

- Keep Phase 3 authentication behavior unchanged and independently verifiable.
- Introduce token issuance behind a service abstraction so the login success response can be reverted without changing LDAP binding.
- Keep token middleware and authorization policy registration in isolated extension methods.
- Preserve the prior public login/health behavior until token issuance and validation tests pass.
- During key rotation rollback, retain the prior public key for the approved overlap window and restore the prior active `kid` only through the protected key-storage process.
- Remove or disable only the Phase 4 composition changes if deployment rollback is required; do not weaken certificate validation or alter Phase 3 credential handling.
- Never roll back by committing keys, disabling signature validation, accepting any algorithm, bypassing issuer/audience checks, or making protected endpoints default allow.

## 25. Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Required JWT capability is not available without a new package | Stop and request separate package approval; do not alter project boundaries. |
| Private-key exposure | Use only the approved managed/HSM key boundary and least-privilege issuer identity. |
| Stolen bearer token replay | Short lifetime, HTTPS, secure client handling, and approved stateless controls. |
| Stale AD-group membership | Apply mappings at issuance and use short token lifetimes; no long-lived cache initially. |
| Oversized group claims | Bound claims and fail closed. |
| Nested-group privilege expansion | Reject implicit nested expansion. |
| Wrong issuer/audience acceptance | Exact validation and dedicated configuration tests. |
| Algorithm confusion | Fixed RSA algorithm allowlist; reject unsigned/unknown algorithms. |
| Key rotation outage | Public-key overlap through token expiry and tested emergency replacement. |
| Clock drift | Synchronized UTC clocks and small bounded skew. |
| Sensitive directory data leakage | Strict claim allowlist and security tests. |
| Phase 3 regression | Preserve Phase 3 files/contract and run regression tests after each slice. |
| Live-DC-dependent tests | Use fakes and runtime test keys; isolate live RootDSE tests. |
| Scope expansion | Treat any new package, database, refresh token, MFA, SSO, rate limiting, or Phase 3 change as a stop condition. |

## 26. Explicit Out-of-Scope Items

The following are not part of this implementation plan:

- Refresh-token implementation.
- Refresh-token storage, rotation, reuse detection, or revocation.
- Stateful token revocation storage or database tables.
- MFA.
- SSO or federation.
- Rate limiting.
- Brute-force protection.
- External identity provider integration.
- Changes to Phase 3 LDAP authentication, UPN validation, LDAPS/TCP 636, certificate validation, password handling, or error semantics.
- Raw LDAP data or broad directory group dumps in tokens.
- Signing keys in source control or ordinary configuration.
- New NuGet packages or project references without separate approval.
- Changes to Domain responsibilities or dependency direction.
- Unapproved security controls or unrelated feature work.

## 27. Definition of Done

Phase 4 implementation is complete only when:

- JWT bearer access tokens are issued after successful Phase 3 authentication.
- The approved token response contains only access token, token type, and expiration metadata.
- Issuer, audience, subject, claims, lifetime, and clock skew follow the approved baseline.
- RSA signing uses the approved protected key-storage boundary and `kid` behavior.
- Key rotation and emergency replacement behavior are tested.
- No production or private signing keys are committed or logged.
- API middleware validates signature, algorithm, issuer, audience, time claims, size, and key identifier.
- Authorization uses approved application roles, explicit AD-group allowlisting, named policies, and default deny.
- Nested groups are not implicitly expanded; oversized claims fail closed.
- Login and approved public endpoints remain intentionally public; protected endpoints enforce 401/403 behavior.
- No refresh tokens or stateful revocation storage are implemented.
- Phase 3 LDAP authentication behavior is unchanged.
- No unapproved packages, project references, architecture changes, databases, MFA, SSO, rate limiting, or brute-force protection are introduced.
- Unit, integration, and security tests pass without live-DC, production-key, real-password, or database dependencies.
- `dotnet restore`, `dotnet build LabAuthServer.slnx`, and `dotnet test LabAuthServer.slnx` pass with zero warnings and zero errors.
- Project Status and implementation documentation are updated after the approved implementation is complete.

**Current status:** The approved Phase 4 implementation slices are present and verified in the repository. Remaining operational deployment steps require separate environment validation; excluded features remain unimplemented.
