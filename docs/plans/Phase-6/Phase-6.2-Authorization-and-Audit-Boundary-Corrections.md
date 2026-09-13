# Phase 6.2 — Authorization and Audit Boundary Corrections

Status: IMPLEMENTED — PHASE 6.2 COMPLETE. P6-D1 and P6-D2 were owner approved before implementation. No Phase 6.3–6.7 work was performed.

## Implementation record

The approved Phase 6.2 boundaries are implemented without changing the authentication architecture or database schema:

- `FallbackPolicy` now uses the existing authenticated-user `DefaultPolicy`, while explicit `[AllowAnonymous]` endpoints remain public and explicit role policies remain unchanged.
- Unmatched routes retain normal 404 routing behavior; bearer challenge and forbid behavior remains 401 and 403 respectively.
- Successful protected-access audit events preserve the request and event when the subject exceeds the 256-character identity limit by omitting both identity fields and recording `identityOmitted: true` in `DetailsJson`.
- The audit identity limit is shared through `AuditEventValidator.MaximumIdentityLength`; the validator and SQL parameter boundaries remain unchanged.

Files changed for this subphase:

- `src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs`
- `src/LabAuthServer.Api/Controllers/ProtectedController.cs`
- `src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs`
- `src/LabAuthServer.Application/Auditing/AuditEventValidator.cs`
- `tests/LabAuthServer.IntegrationTests/ProtectedEndpointTests.cs`
- `tests/LabAuthServer.IntegrationTests/AuditBoundaryRegressionTests.cs`
- this Phase 6.2 implementation record

Validation evidence:

- Protected endpoint tests cover unauthenticated 401, insufficient-role 403, and successful Reader access.
- Health remains anonymously accessible, unmatched routes remain 404, and the configured fallback policy requires authenticated users.
- Successful protected access is tested at 256 and 257-character subject lengths; oversized identity values are omitted without rejecting the request, and the identity-omitted indicator is validated through the same pre-SQL validator used by the test audit service.
- `dotnet restore` succeeded; `dotnet build LabAuthServer.slnx --no-restore` succeeded; `dotnet test LabAuthServer.slnx --no-build --no-restore` completed with 1,043 passed, 0 failed, and 3 skipped.

Remaining limitations:

- Audit persistence remains best effort; loss tolerance, retention, monitoring, and SQL-outage policy remain deferred to Phase 6.5.
- License, release-qualification, environment-acceptance, and first-release work remain outside Phase 6.2.

## Purpose

Correct the authorization and audit boundaries that are currently implied rather than explicitly approved, without changing the overall authentication architecture. This subphase addresses: default/deny authorization behavior at the boundary, public-endpoint exposure, and the oversized-subject audit issue on successful protected access.

## Confirmed current findings

### Authorization boundary

The current authorization configuration in [src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs](../../../src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs) establishes:

- `DefaultPolicy` as a JWT-authenticated-user policy.
- Named role policies for Reader, Operator, and Administrator.
- Explicit public endpoints for health and login.

There is no global `FallbackPolicy` defined. The default behavior therefore relies on the framework default policy and on explicit controller annotations. This means the future contract must be decided explicitly for endpoints that are not covered by a route-level or controller-level policy.

### Successful-access audit identity

In [src/LabAuthServer.Api/Controllers/ProtectedController.cs](../../../src/LabAuthServer.Api/Controllers/ProtectedController.cs), the successful protected-access event writes both `Username` and `Subject` from the JWT `sub` claim.

The production SQL audit validator enforces a maximum length of 256 characters for both fields in [src/LabAuthServer.Application/Auditing/AuditEventValidator.cs](../../../src/LabAuthServer.Application/Auditing/AuditEventValidator.cs), and the SQL persistence layer binds them as `NVARCHAR(256)` in [src/LabAuthServer.Infrastructure/Auditing/SqlAuditEventService.cs](../../../src/LabAuthServer.Infrastructure/Auditing/SqlAuditEventService.cs).

The login failure and authorization-denied paths already omit oversized identities rather than failing the event, as seen in [src/LabAuthServer.Api/Controllers/AuthController.cs](../../../src/LabAuthServer.Api/Controllers/AuthController.cs) and [src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs](../../../src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs).

This indicates a likely correction point: successful private-resource access should follow the same omission pattern when the JWT subject exceeds the supported audit limit, while preserving a valid audit record and a successful request outcome.

## Problem to solve

The implementation must decide and enforce a narrow, explicit contract for how the application behaves in the following categories:

- endpoints with explicit `[Authorize]`;
- endpoints with explicit role policies;
- explicitly anonymous/public endpoints;
- future unannotated endpoints;
- unmatched routes;
- challenge vs forbidden behavior;
- successful protected accesses that carry an oversized JWT subject.

## Dependencies on earlier work

This subphase depends on Phase 6.1 for safe validation boundaries and on the approved behavior of the JWT/authentication layer. It does not change the authentication architecture, certificate flow, or the token issuance format.

## Resolved decision gates

The following gates were resolved by the owner before implementation:

- P6-D1: authenticated by default; explicit public endpoints use `[AllowAnonymous]`; mapped unannotated endpoints require authentication; unmatched routes retain 404; authentication challenge is 401 and insufficient authorization is 403.
- P6-D2: preserve authorized access; omit oversized username/subject values; record a safe identity-omitted indicator; preserve the remainder of the audit event.

## External/professional dependencies

- Operations or security owner for default-deny policy intent.
- Architecture sign-off for fallback behavior and 401/403 semantics.
- No external vendor dependency is required for this correction.

## Narrow implementation scope

The completed implementation changed only the following:

- authorization policy configuration and route boundary contract;
- explicit public endpoint allowlist behavior for controllers/endpoints;
- controller-level and endpoint-level exposure regression tests;
- the success-path audit event for oversized `sub` values;
- audit boundary tests at the exact supported 256-character limit.

## Explicit non-goals

This plan does not include:

- new role hierarchy;
- MFA, SSO, or federation;
- refresh tokens or session storage;
- audit outbox/retry architecture;
- licensing changes;
- broader authorization refactoring beyond boundary correction.

## Expected files/components

Likely file candidates include:

- [src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs](../../../src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs)
- [src/LabAuthServer.Api/Controllers/ProtectedController.cs](../../../src/LabAuthServer.Api/Controllers/ProtectedController.cs)
- [src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs](../../../src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs)
- [src/LabAuthServer.Application/Auditing/AuditEventValidator.cs](../../../src/LabAuthServer.Application/Auditing/AuditEventValidator.cs)
- [src/LabAuthServer.Infrastructure/Auditing/SqlAuditEventService.cs](../../../src/LabAuthServer.Infrastructure/Auditing/SqlAuditEventService.cs)
- test projects covering controller authorization and audit boundary behavior

## Automated validation performed

The implementation includes:

- endpoint exposure tests for public vs protected routes;
- default/deny and fallback-policy decision tests if the contract is codified;
- 401 vs 403 tests for authenticated and unauthorized principals;
- a successful protected access audit test for a normal subject length;
- a successful protected access audit test for an oversized subject greater than 256 characters;
- boundary tests at exactly 256 and 257 characters;
- no regression in login audit behavior, denied-access audit behavior, or valid authorization flow.

## Acceptance results

All of the following are true:

- the contract for fallback authorization is explicit and approved;
- public endpoints remain public and protected endpoints remain protected;
- unauthorized valid users receive the correct status code and no authentication bypass;
- successful access with an oversized subject does not break the event or the request;
- the audit event remains valid and bounded by the current 256-character SQL validator;
- regression tests cover exact supported boundaries and the associated failure windows.

## Implementation authorization record

### Baseline prerequisites

- Phase 6.1 is verified and clean.
- The fallback authorization contract is explicitly approved.
- The audit omission pattern is approved for successful access with oversized identity values.

### Exact implementation scope

- authorization policy contract for fallback/default-deny behavior;
- public and protected endpoint exposure enforcement;
- successful-access audit identity truncation/omission handling;
- endpoint and boundary regression tests.

### Explicit non-goals

- role hierarchy redesign;
- MFA/federation;
- refresh/session architecture;
- audit queue/outbox design;
- licensing behavior changes.

### Expected files/components

- API authorization configuration and controller middleware;
- audit validator and SQL writer boundary;
- tests covering public/private exposure and exact boundary sizing.

### Tests/validation

- integration tests for endpoint exposure;
- controller behavior tests for 200/401/403;
- audit unit tests at 256/257 boundary values;
- full solution validation after the change.

### Acceptance criteria

- no unapproved broader policy change;
- default deny/fallback is explicitly documented;
- audit event remains valid at the supported size limit;
- successful access does not produce invalid SQL-bound identity data.

### Owner/architect decisions resolved before implementation

- P6-D1 approved fallback contract for unannotated endpoints and 401/403 behavior;
- P6-D2 approved omission convention for oversized successful-access identity values.

### External dependencies

- owner/architect approval only.

### Safety boundaries

- no production release or deployment action;
- no authentication architecture change beyond default/fallback acceptance contract;
- no license or database schema changes.

### Signed commit message

Implement Phase 6.2 authorization and audit boundaries

---

This subphase is complete. No Phase 6.3–6.7 implementation or release activity is authorized by this record.
