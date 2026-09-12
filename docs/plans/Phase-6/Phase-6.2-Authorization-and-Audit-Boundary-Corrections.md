# Phase 6.2 — Authorization and Audit Boundary Corrections

Status: PLANNED — NOT AUTHORIZED FOR IMPLEMENTATION.

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

## Decision gates required before implementation

The following must be resolved by the owner or architect before behavior changes are allowed:

- What is the desired fallback contract for a route that is neither public nor explicitly protected?
- Should the application default to 401 for absent/invalid authentication and 403 for valid-but-unauthorized users?
- Should unmatched routes intentionally be 404, or should they also be covered by an explicit auth policy?
- What is the exact desired behavior for security-sensitive endpoints with explicit `[Authorize]` vs explicit named role policies?
- Should successful access with an oversized subject be omitted to null or reduced to a bounded alias, while preserving the rest of the event?

## External/professional dependencies

- Operations or security owner for default-deny policy intent.
- Architecture sign-off for fallback behavior and 401/403 semantics.
- No external vendor dependency is required for this correction.

## Narrow implementation scope

The future implementation may change only the following:

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

## Required automated validation

Implementation must include:

- endpoint exposure tests for public vs protected routes;
- default/deny and fallback-policy decision tests if the contract is codified;
- 401 vs 403 tests for authenticated and unauthorized principals;
- a successful protected access audit test for a normal subject length;
- a successful protected access audit test for an oversized subject greater than 256 characters;
- boundary tests at exactly 256 and 257 characters;
- no regression in login audit behavior, denied-access audit behavior, or valid authorization flow.

## Acceptance criteria

The future implementation is acceptable only if all of the following are true:

- the contract for fallback authorization is explicit and approved;
- public endpoints remain public and protected endpoints remain protected;
- unauthorized valid users receive the correct status code and no authentication bypass;
- successful access with an oversized subject does not break the event or the request;
- the audit event remains valid and bounded by the current 256-character SQL validator;
- regression tests cover exact supported boundaries and the associated failure windows.

## Implementation Authorization Packet

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

### Owner/architect decisions required first

- fallback contract for unannotated endpoints;
- 401 vs 403 decision for authentication and authorization conditions;
- acceptable audit omission convention for oversized subject.

### External dependencies

- owner/architect approval only.

### Safety boundaries

- no production release or deployment action;
- no authentication architecture change beyond default/fallback acceptance contract;
- no license or database schema changes.

### Recommended signed commit message

Plan remaining Phase 6 work

---

This subphase remains planning-only and does not authorize implementation.
