# Phase 12 - Automated Testing Checklist

**Date:** 2026-09-04
**Status:** COMPLETE

## Objective

Validate the approved Phase 11 application audit, correlation, error handling, authentication, authorization, JWT, API, reliability, and sensitive-data boundaries without deploying to IIS or changing protected environment configuration.

## Test strategy

- Existing unit tests were preserved and extended only at genuine boundary gaps.
- ASP.NET Core request-pipeline behavior was exercised with `WebApplicationFactory`.
- SQL repository tests called `SqlAuditEventService` and the approved `Audit.usp_WriteAuditEvent` procedure against the local `LabAuthServer` database.
- LDAP behavior remained isolated behind existing fakes/unit seams; live RootDSE tests remained unchanged.
- Tests use placeholders only and do not inspect DPAPI contents or certificate private keys.

## Baseline

The required baseline was established before edits:

- Restore: passed.
- Release build: passed.
- Unit tests: 99 passed, 0 failed, 0 skipped.
- Integration tests: 46 passed, 0 failed, 0 skipped.
- Total: 145 passed, 0 failed, 0 skipped.

## Coverage matrix

| Category | Existing and Phase 12 coverage | Status |
|---|---|---|
| Configuration | AD, token, authorization, audit valid/invalid and fail-closed validation | COMPLETE |
| LDAPS authentication | UPN validation, escaping, cancellation, unavailable/missing secret boundaries, typed failures | COMPLETE |
| AD user validation | Invalid domain/input, not-authenticated and failure mapping | COMPLETE |
| AD group lookup | LDAP group boundary and safe failure behavior | COMPLETE |
| AD group-to-role mapping | Administrator, Operator, Reader, report, unknown, duplicate, normalization, precedence, malformed configuration | COMPLETE |
| Role precedence | Deterministic Administrator > Operator > Reader ordering | COMPLETE |
| JWT issuance | Approved response, claims, role, signing and lifetime metadata | COMPLETE |
| JWT claims | Required identity claims, jti, one approved role, size limits, sensitive claim exclusion | COMPLETE |
| JWT signing | RSA algorithm restriction, certificate/provider behavior, key identifiers and rotation | COMPLETE |
| JWT validation | Issuer, audience, lifetime, signature, key id, malformed/tampered/multiple-role cases | COMPLETE |
| Authorization policies | Reader success, Operator denial, missing/invalid role, unauthenticated request | COMPLETE |
| API endpoints | Health, login controller, protected resource, status and response contracts | COMPLETE |
| HTTP authentication | Missing and malformed bearer headers, invalid/expired tokens | COMPLETE |
| HTTP authorization | 401 versus 403 and policy ordering through the real pipeline | COMPLETE |
| Audit event validation | Event, role, status, length, JSON, IP, correlation and sensitive-property rules | COMPLETE |
| SQL audit repository | Procedure persistence, returned ID, invalid event, SQL outage, concurrent writes | COMPLETE |
| Correlation ID | Canonical generation, valid propagation, malformed replacement, response header, request isolation | COMPLETE |
| Global exception handling | Safe 500 ProblemDetails, correlation retention, minimized audit, audit-failure resilience | COMPLETE |
| ProblemDetails | Stable generic error response without exception details | COMPLETE |
| Sensitive-data protection | Source/test/docs scan and behavioral assertions for passwords, tokens and exception details | COMPLETE |
| Failure handling | LDAP/SQL/audit failure paths fail safely without bypass or disclosure | COMPLETE |
| Reliability/concurrency | Concurrent health requests and concurrent SQL audit writes preserve isolation | COMPLETE |

## Executed results

### Unit tests

111 passed, 0 failed, 0 skipped. This includes the four SQL repository tests executed against the approved local database.

### Integration tests

51 passed, 0 failed, 0 skipped. The HTTP pipeline tests cover health correlation behavior, protected endpoint authentication/authorization, malformed authorization headers, and concurrent request isolation.

### Security tests

Passed. JWT tampering/validation boundaries, LDAP filter escaping, authorization boundaries, sensitive audit-property rejection, safe ProblemDetails, and safe failure handling passed. Test placeholders were used; no real secrets were printed.

### SQL and audit tests

Passed. `SqlAuditEventService` persisted an approved event through `Audit.usp_WriteAuditEvent`, returned an audit identifier, rejected invalid input before database access, returned null on database unavailability, and persisted concurrent events with distinct correlation identifiers. Read-only SQL verification confirmed the approved database objects exist. Phase 11 permission evidence remains the authoritative least-privilege record: writer procedure execute is enabled and direct audit-table access is denied for the application identity.

### Reliability tests

Passed. Concurrent health requests returned distinct canonical correlation IDs. Concurrent audit writes returned distinct IDs and preserved per-event correlations. No sleeps or IIS dependencies were added.

### Sensitive-data scan

Passed. The scoped scan of `src/`, `tests/`, and `docs/` found only approved field names, configuration vocabulary, documentation examples, and safe test placeholders. No actual passwords, credentials, bearer values, private keys, DPAPI contents, or connection-string passwords were found. Build output directories were excluded from interpretation.

## Full validation

- Release restore: passed.
- Release build: passed.
- Full test suite: 162 passed, 0 failed, 0 skipped.
- Failed tests: 0.
- Skipped tests: 0.

## Known limitations

- Live AD user authentication scenarios are represented by deterministic unit seams; no production directory mutation or credential access was performed.
- Certificate-store and DPAPI behavior remain boundary-tested without exporting keys or inspecting protected secret contents.
- Least-privilege negative permission evidence is from the completed Phase 11 controlled validation, not a test-run identity switch in this suite.
- Retention, archival, purge, and SQL Agent scheduling remain explicitly deferred by Phase 11 approval.

## Acceptance result

Phase 12 was accepted after the final full Release restore, build, and test commands completed with zero failures and zero skips. Phases 13-18 are recorded in their respective completion documents.
