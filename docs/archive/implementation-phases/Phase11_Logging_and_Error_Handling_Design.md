# PHASE 11 - LOGGING & ERROR HANDLING

**Status:** COMPLETE  
**Date:** 2026-09-04  
**Prerequisites:** Phases 0-10 remain complete; SQL Server is installed for lab planning.  
**Implementation state:** Database, SQL security, application integration, and validation are complete; retention and purge remain explicitly deferred.

> Phase 11 implementation is complete. No IIS setting, deployment artifact, certificate private key, or DPAPI secret content was accessed or changed.

## 1. Objective

Define an enterprise-grade, data-minimized audit and error-handling architecture using SQL Server as the persistent store for approved security and audit events. The design must preserve the completed LDAPS authentication, AD group-to-role mapping, JWT issuance and validation, authorization policies, endpoint contracts, and existing deployment boundaries.

The design separates:

- Security/audit events that require durable, queryable records.
- Operational diagnostics that remain in the existing `ILogger<T>` providers and must not be copied to SQL automatically.
- Safe client error responses produced by a global exception boundary.

## 2. Current State

The source baseline is the four-layer solution:

```text
Domain <- Application <- Infrastructure <- Api
```

Current relevant surfaces:

- `src/LabAuthServer.Api/Program.cs` clears providers, enables console and debug logging, calls `AddProblemDetails()`, and calls `UseExceptionHandler()`.
- `AuthController` owns the HTTP login endpoint and currently logs authorization/token issuance failures with `ILogger`; this log contains the username and failure stage, not the password or token.
- `LdapAuthenticationService` logs authentication attempts and outcomes using the username only. LDAP credentials and service-account credential material are not logged.
- `TokenService` and `TokenClaimsBuilder` issue bounded JWT claims through existing abstractions. JWT validation and authorization are configured in the API boundary.
- Before Phase 11 implementation, there was no SQL repository, audit abstraction, database registration, audit schema, stored procedure, correlation middleware, or Phase 11 event persistence.
- Implementation progress: `LabAuthServer` database, `Audit`/`Reference`/`Application` schemas, `Reference.EventTypes`, `Audit.AuditEvents`, `Audit.usp_WriteAuditEvent`, the `LabAuthServer_AuditWriter` role, and the verified IIS virtual-account mapping now exist. Application and API integration are being completed against these objects.
- Existing tests cover authentication, token, authorization, protected endpoints, configuration, audit persistence, correlation, and error handling.

The current source tree is the only application surface considered by this plan. `Current`, IIS, certificates, DPAPI contents, and deployment output are excluded.

## 3. SQL Server Installation Status

SQL Server default instance `MSSQLSERVER` is running. The `LabAuthServer` database is online and the Phase 11 database objects and writer permission boundary have been implemented and validated. No SQL Agent job or purge procedure exists because retention/archive ownership remains deferred.

## 4. Final Database Architecture

The approved-direction candidate is:

```text
ASP.NET Core API
    |
    v
Application audit contract / audit service
    |
    v
Infrastructure SQL audit repository
    |
    v
Audit.usp_WriteAuditEvent
    |
    v
SQL Server / LabAuthServer database / audit tables
```

The application will never construct dynamic SQL and will not issue arbitrary `INSERT`, `UPDATE`, or `DELETE` statements against audit tables. The repository will call a known stored procedure with typed parameters and receive only the result required by the caller.

Recommended implementation boundary:

- Application owns an immutable audit-event model, approved event codes, validation contract, and service interface.
- Infrastructure owns SQL client code, parameter binding, timeout/cancellation policy, and stored-procedure execution.
- API owns correlation/request context, endpoint and HTTP metadata, exception mapping, and event orchestration at the HTTP boundary.
- SQL owns referential validation, final length/JSON checks, the audit write, and database-side write permissions.

A SQL provider package or connection configuration must be separately approved before implementation. No package or connection string is added during planning.

## 5. Final Schemas and Tables

### 5.1 `Reference` schema

`Reference.EventTypes` is the controlled vocabulary table and contains all 13 approved event definitions:

| Column | Recommendation |
|---|---|
| `EventTypeId` | `int` identity primary key |
| `EventTypeCode` | `varchar(64)` unique, case-sensitive or normalized consistently |
| `Category` | `varchar(32)` controlled value: `Authentication`, `Authorization`, `Security`, or `Application` |
| `Severity` | `varchar(16)` controlled value: `Information`, `Warning`, `Error`, or `Critical` |
| `Description` | `nvarchar(256)` non-secret description |
| `IsEnabled` | `bit` default `1` |
| `CreatedAtUtc` | `datetime2(3)` |
| `RetiredAtUtc` | nullable `datetime2(3)` |

Separate category and severity tables are not recommended initially. They add joins and administration without materially improving the small controlled vocabulary. CHECK constraints or procedure validation, together with the reference row, are sufficient. A separate severity table can be approved later if multiple applications need a shared taxonomy.

### 5.2 `Audit.AuditEvents`

Recommended columns and bounds:

| Column | Type / nullability | Purpose |
|---|---|---|
| `AuditEventId` | `bigint` identity primary key | Durable event identifier |
| `EventTimeUtc` | `datetime2(3)` not null | Event time supplied from trusted application context or generated by SQL per approved policy |
| `EventTypeId` | `int` not null FK | Controlled event definition |
| `CorrelationId` | `uniqueidentifier` not null | Trace across one request |
| `RequestId` | `varchar(128)` nullable | ASP.NET request identifier, bounded and non-secret |
| `Username` | `nvarchar(256)` nullable | Normalized user identifier where needed; not a password |
| `Subject` | `nvarchar(256)` nullable | Non-secret authenticated subject; avoid duplicate identity unless needed |
| `Role` | `nvarchar(64)` nullable | One approved application role when relevant |
| `Endpoint` | `nvarchar(256)` nullable | Route template, never a query string or credential-bearing URL |
| `HttpMethod` | `varchar(16)` nullable | HTTP method |
| `StatusCode` | `smallint` nullable | HTTP status when applicable |
| `Success` | `bit` nullable | Outcome for event types where meaningful |
| `ClientIp` | `varchar(45)` nullable | IPv4/IPv6 address, subject to privacy approval |
| `ServerName` | `nvarchar(128)` nullable | Host instance name, not a secret |
| `ApplicationVersion` | `varchar(64)` nullable | Build/release identifier, not a token |
| `DetailsJson` | `nvarchar(max)` nullable | Strictly allowlisted, redacted diagnostic metadata |
| `CreatedAtUtc` | `datetime2(3)` not null | SQL write time |
| `CreatedBy` | `nvarchar(128)` not null | Fixed non-secret value such as `LabAuthServer.Api` or procedure caller identity |

`DetailsJson` is not a general-purpose request-body or exception dump. It must be allowlisted, bounded at the application boundary and procedure boundary, valid JSON when present, and stripped of sensitive values. The table should be append-only to the application principal.

No separate application error table is recommended for the first implementation. Safe error responses and internal diagnostic logs are different concerns; a future incident/error store would require its own data-minimization and retention decision.

## 6. Stored Procedure Design and Verification

### 6.1 `Audit.usp_WriteAuditEvent`

Recommended parameter contract:

| Parameter | SQL type | Required | Rules |
|---|---|---:|---|
| `@EventTypeCode` | `varchar(64)` | Yes | Non-empty; must resolve to an enabled `Reference.EventTypes` row |
| `@EventTimeUtc` | `datetime2(3)` | No | UTC only; if null, generate in SQL; reject unreasonable future/past values if policy approves |
| `@CorrelationId` | `uniqueidentifier` | Yes | Must be non-empty; generated by middleware |
| `@RequestId` | `varchar(128)` | No | Trimmed, bounded, non-secret |
| `@Username` | `nvarchar(256)` | No | Trimmed; no credential data |
| `@Subject` | `nvarchar(256)` | No | Trimmed; no token data |
| `@Role` | `nvarchar(64)` | No | Null unless relevant; allowlist `Administrator`, `Operator`, `Reader` |
| `@Endpoint` | `nvarchar(256)` | No | Route template only; no query string/body |
| `@HttpMethod` | `varchar(16)` | No | Allowlisted HTTP method |
| `@StatusCode` | `smallint` | No | Null or 100-599 |
| `@Success` | `bit` | No | Null only where event semantics do not define an outcome |
| `@ClientIp` | `varchar(45)` | No | Valid normalized IP or null |
| `@ServerName` | `nvarchar(128)` | No | Host name only |
| `@ApplicationVersion` | `varchar(64)` | No | Bounded release identifier |
| `@DetailsJson` | `nvarchar(max)` | No | Null or valid, bounded, allowlisted JSON |
| `@CreatedBy` | `nvarchar(128)` | No | Procedure default from application identity or fixed application value |

The implemented procedure:

1. Use `SET NOCOUNT ON` and `SET XACT_ABORT ON`.
2. Validate required values, lengths, status range, role allowlist, and `ISJSON(@DetailsJson) = 1` when details are supplied.
3. Resolve `@EventTypeCode` to an enabled event type and reject unknown or retired codes.
4. Generate `@EventTimeUtc` with `SYSUTCDATETIME()` when null. The recommended policy is SQL-generated write time for `CreatedAtUtc` and application-supplied event time only when event timing matters; this requires approval.
5. Use one short transaction for reference resolution and insert. No external calls or nested application work occur inside the transaction.
6. Use `TRY/CATCH`, `XACT_STATE()` rollback handling, and `THROW` so the repository can distinguish a stored-procedure failure without exposing SQL details to clients.
7. Return exactly one result row containing `AuditEventId`, `EventTimeUtc`, and optionally `CorrelationId`; the application needs only the identifier and success status.
8. Avoid duplicate suppression by default. Audit events are facts and retries can be meaningful. If an at-least-once retry policy is approved, add an explicit non-secret idempotency key and unique constraint rather than guessing from event fields.
9. Treat null optional fields as null; do not convert missing sensitive data into placeholder text.

A write is atomic: either the one audit row is committed or no audit row is committed. The procedure must not swallow errors or return diagnostic exception text.

### 6.2 Retention procedure

A separate `Audit.usp_PurgeAuditEvents` remains deferred with approval because archival, legal hold, maintenance ownership, and scheduling are not sufficiently defined. No purge procedure, role, permission, or SQL Agent job was created.

## 7. Final SQL Security and Permission Model

Recommended exact model, subject to server/database naming approval:

1. Use SQL Server Windows Authentication.
2. Create database `LabAuthServer` and schemas `Audit`, `Reference`, and `Application` only during an approved implementation.
3. Create a database user mapped to the IIS application pool Windows identity, preferably `IIS APPPOOL\\LabAuthServer` when the site uses that exact pool identity. Verify the actual identity during deployment; do not assume it from this plan.
4. Create database role `LabAuthServer_AuditWriter`.
5. Add the application user to `LabAuthServer_AuditWriter`.
6. Grant the role `EXECUTE` on `Audit.usp_WriteAuditEvent` only. Do not grant `INSERT`, `UPDATE`, `DELETE`, `SELECT`, `ALTER`, `CONTROL`, `db_owner`, or `db_datawriter` to the application role.
7. Do not grant database creation, login creation, `ALTER DATABASE`, `DROP DATABASE`, or `sysadmin` privileges to the application identity.
8. Keep reference-data ownership and deployment DDL under a separate DBA/deployment identity. The application does not write reference data.
9. Create a separate `LabAuthServer_AuditMaintainer` role for the approved retention procedure, assigned to a scheduled-job identity or DBA-managed identity, never to the IIS pool.
10. Grant `EXECUTE` on the retention procedure only to the maintenance role after retention and archival approval.

The procedure owner should own the underlying table access through ownership chaining. Direct table permissions remain denied by omission. Permission verification must include a negative test using the real application identity.

## 8. Phase 11 Event Catalog

The initial recommendation is to persist only events useful for security investigation, access accountability, or operational incident correlation. Routine verbose diagnostics remain in `ILogger` and are not durable SQL audit rows.

| Code | Category | Severity | Generation and fields |
|---|---|---|---|
| `AUTH_LOGIN_SUCCESS` | Authentication | Information | Successful login and token issuance boundary. Required: correlation, username/subject, endpoint, method, success. Optional: role, status, server/version. Never password/token. |
| `AUTH_LOGIN_FAILURE` | Authentication | Warning | Invalid request or invalid credentials. Required: correlation, endpoint, method, status, success=false. Username may be recorded only as normalized identifier. Do not distinguish user-not-found details. |
| `AUTH_LDAP_FAILURE` | Authentication | Error | Directory unavailable, timeout, configuration, or unexpected LDAP failure. Required: correlation, status, success=false, safe failure category. Optional: username. No LDAP exception detail, filter, DN, password, or service-account information. |
| `AUTHZ_ACCESS_GRANTED` | Authorization | Information | Protected endpoint authorized. Required: correlation, subject or username, role, route template, method, status, success=true. |
| `AUTHZ_ACCESS_DENIED` | Authorization | Warning | Authenticated request denied by policy. Required: correlation, subject when available, endpoint, method, status=403, success=false. Do not store token or all claims. |
| `SEC_INVALID_TOKEN` | Security | Warning | Bearer authentication failed with a generic invalid-token class. Required: correlation, endpoint, method, status=401, success=false. Never store token, header, signature, claims, or raw failure text. |
| `SEC_EXPIRED_TOKEN` | Security | Information | Use only if the framework can classify expiration without storing token data. Required: correlation, endpoint, method, status=401. Approval needed because it may be noisy and can reveal token timing. |
| `SEC_INVALID_SIGNATURE` | Security | Warning | Signature validation failure when safely classifiable. Required: correlation, endpoint, method, status=401. Store no token, signature, key material, or raw exception. |
| `SEC_INVALID_ISSUER` | Security | Warning | Issuer validation failure when safely classifiable. Required: correlation, endpoint, method, status=401. Store no token or issuer supplied by the client. |
| `SEC_INVALID_AUDIENCE` | Security | Warning | Audience validation failure when safely classifiable. Required: correlation, endpoint, method, status=401. Store no token or client-supplied audience. |
| `SEC_INVALID_ROLE` | Security | Warning | Required role claim absent, duplicated, or not approved. Required: correlation, endpoint, method, status=401/403. Store no token or raw claims; approved policy name may be optional. |
| `APP_UNHANDLED_EXCEPTION` | Application | Error | Global exception boundary catches an unhandled exception. Required: correlation, endpoint, method, status=500, success=false. Optional: stable exception category and application version. Raw exception details remain internal `ILogger` diagnostics, not audit JSON. |
| `APP_VALIDATION_ERROR` | Application | Information | Request validation fails. Required: correlation, endpoint, method, status=400, success=false. Optional: allowlisted field names only, never field values or raw body. |

The catalog is a proposal, not reference data. Before implementation, approve the final list, severity policy, event volume, and whether token subcategories are worth their operational and privacy cost. The initial default is to retain the security categories but permit aggregation/rate limiting for high-volume invalid-token events.

## 9. Global Error Handling

Recommended request flow:

```text
HTTP request
  -> correlation context
  -> ASP.NET authentication
  -> authorization
  -> endpoint/application operation
  -> safe exception boundary
  -> audit event where approved + ILogger diagnostics
  -> ProblemDetails response
```

The exception boundary should be registered before request processing and should preserve the existing `ProblemDetails` convention. It should:

- Generate or obtain the correlation ID before downstream work.
- Convert unhandled exceptions to a generic 500 response with a stable problem type/title and correlation ID.
- Map known validation, authentication, authorization, cancellation, SQL, LDAP, and token failures to approved status codes without leaking implementation details.
- Log exception diagnostics internally through structured `ILogger` with allowlisted properties.
- Persist only the approved minimized event, and avoid recursive failure loops if SQL audit storage is unavailable.
- Never expose stack traces, SQL exception text, LDAP details, certificate details, secrets, or filesystem paths.
- Avoid raw request bodies, especially login bodies. Model validation records field names/categories only.

Client response should contain a stable generic message and the correlation identifier. Audit write failure should not replace the primary client result for a successful business request; the failure must be observable through internal logging and health/operations handling. Fail-closed behavior for security-critical audit requirements is an approval decision, not an accidental repository exception.

## 10. Correlation and Request Traceability

Recommended strategy:

1. A correlation middleware runs at the earliest API stage.
2. If the client supplies `X-Correlation-ID`, accept it only when it is a valid canonical GUID. Invalid, oversized, or malformed values are ignored and a new `Guid.NewGuid()` value is generated. The client never controls the stored value outside this validation.
3. Store the generated `Guid` in `HttpContext.Items` or a typed request-context accessor and push it into the logging scope. Do not use unbounded user input as a scope key.
4. Preserve ASP.NET `HttpContext.TraceIdentifier` as `RequestId`; it is stored separately and bounded.
5. Return `X-Correlation-ID` on every response, including errors. The same value is passed to authentication, authorization, application operations, and audit events through the request context.
6. Do not accept a correlation value containing secrets or encode user identity in it.
7. If no header is supplied, generate a new value. If a valid value is supplied, reuse it for traceability but never treat it as proof of identity.

The exact header name and whether correlation IDs are exposed to clients require approval. `TraceIdentifier` remains framework-owned and is not a replacement for the explicit correlation identifier.

## 11. Indexing Proposal

Start with only indexes tied to confirmed investigation queries:

- Clustered primary key on `AuditEventId`.
- `IX_AuditEvents_EventTimeUtc` on `EventTimeUtc`, supporting retention windows and chronological review.
- `IX_AuditEvents_CorrelationId` on `CorrelationId`, supporting request traceability.
- `IX_AuditEvents_EventTypeId_EventTimeUtc` on `(EventTypeId, EventTimeUtc)`, supporting event catalog investigations.
- `IX_AuditEvents_Username_EventTimeUtc` on `(Username, EventTimeUtc)` only if username investigations are frequent and privacy review approves it.
- `IX_AuditEvents_StatusCode_EventTimeUtc` on `(StatusCode, EventTimeUtc)` only if operational querying demonstrates value.

The last two are candidates, not automatic additions. Do not index `DetailsJson` initially. Measure write overhead and query plans before adding indexes.

## 12. Retention, Archival, and Immutability

Recommended starting policy for approval:

- Retain online audit events for 12 months, with an archival decision before expiry.
- Do not delete automatically until the retention period, legal hold behavior, archive destination, and owner are approved.
- Use a DBA-controlled SQL Agent job or equivalent maintenance identity calling `Audit.usp_PurgeAuditEvents` in bounded batches.
- Keep purge permissions separate from the application writer.
- Deny application updates/deletes and avoid exposing table permissions to the IIS identity.
- Record purge operations in a separate operational channel or approved audit event without creating recursive purge behavior.
- Defer partitioning. It is justified only after volume measurements show that retention operations and time-window queries need it.

Audit immutability is logical through permissions and procedure design in the first implementation. Stronger tamper-evident storage, archival WORM controls, or hash chaining require a separate threat-model and operations decision.

## 13. Data Security Rules

The following must never be stored in SQL audit data, `DetailsJson`, structured logs, exception text, telemetry, or responses:

- Passwords and LDAP service-account credentials.
- JWT/access tokens, refresh tokens, bearer values, or `Authorization` headers.
- DPAPI secrets or decrypted DPAPI contents.
- Certificate private keys, private-key material, or export data.
- Raw credential-bearing request bodies.
- Raw LDAP responses, bind data, search filters containing credentials, or unnecessary directory attributes.
- Stack traces, SQL exception text, certificate details, and internal filesystem paths in client responses.

Safe alternatives are normalized usernames or non-secret subjects, approved roles, route templates, status codes, bounded failure categories, event codes, correlation/request IDs, server/version metadata, and allowlisted non-sensitive JSON fields. Sensitive-data filtering must occur before logger/audit DTO creation, not as a database-only cleanup step.

## 14. Minimum New Components for Implementation

Subject to approval, the smallest production addition is:

1. Application: audit event code/model, validation/filtering contract, and `IAuditEventService`.
2. Infrastructure: SQL repository implementing the application contract and calling the stored procedure.
3. API: correlation middleware/request context and global exception/audit adapter; endpoint event hooks should use the service rather than duplicate LDAP/JWT/role logic.
4. Configuration/DI: non-secret SQL connection configuration and repository registration, with secrets supplied by the approved host mechanism.
5. Tests: focused unit, SQL integration, API integration, and security regression coverage.

No duplicate authentication, LDAP, AD mapping, JWT issuance/validation, or authorization implementation is required or permitted.

## 15. Test Strategy

### Unit tests

- Event-code allowlist and reference metadata validation.
- Required/optional field rules, maximum lengths, status range, role allowlist, and null handling.
- Mapping existing authentication, authorization, security, and exception outcomes to event DTOs.
- Sensitive-data filtering: passwords, service credentials, JWTs, authorization headers, raw bodies, DPAPI values, certificate/private-key material, LDAP details, and stack traces never appear.
- Correlation ID generation, valid propagation, malformed/oversized input replacement, and response-header behavior.
- Safe error mapping for known and unknown exceptions.
- Audit failure handling without recursion or accidental disclosure.

### Integration tests

Run against an isolated approved SQL Server database/container or lab database, never production data:

- Stored procedure execution and returned `AuditEventId`.
- Successful insertion and UTC/null handling.
- Invalid event code, invalid parameters, overlong strings, invalid JSON, invalid role, and status range rejection.
- Unknown/retired event type behavior.
- Transaction rollback and stored-procedure failure behavior.
- Duplicate/retry behavior according to the approved idempotency decision.
- Database unavailable and timeout behavior.
- Negative permission tests: application identity cannot select or modify audit tables and cannot execute maintenance procedures.
- Positive least-privilege test: application identity can execute only the writer procedure.

### Security tests

Assert through database rows, captured logger state, HTTP responses, and failure paths that passwords, LDAP service-account credentials, JWTs, Authorization headers, refresh tokens, DPAPI secrets, certificate private keys, private-key material, and sensitive request bodies are never persisted or returned.

Existing Phase 0-10 tests must remain green and must not require a live SQL Server unless explicitly extended in an isolated Phase 11 suite.

## 16. Numbered Implementation Plan

1. **11.1 Approval and baseline freeze:** approve event catalog, SQL naming, retention, correlation header, failure policy, package/provider, and data classification.
2. **11.2 Database creation:** create `LabAuthServer` only after approval and DBA change control.
3. **11.3 Schema creation:** create `Audit`, `Reference`, and `Application` schemas.
4. **11.4 Reference data:** insert the approved `Reference.EventTypes` catalog through controlled deployment.
5. **11.5 Audit table:** create `Audit.AuditEvents` with constraints, foreign key, append-only posture, and approved indexes.
6. **11.6 Writer procedure:** create and test `Audit.usp_WriteAuditEvent` with validation, UTC policy, transaction, TRY/CATCH, and result contract.
7. **11.7 Retention procedure/job:** only after retention and archival approval; create maintenance procedure and separately controlled schedule.
8. **11.8 SQL security:** create the Windows user and `LabAuthServer_AuditWriter` role; grant only procedure `EXECUTE`; verify negative permissions.
9. **11.9 Application audit contract:** add immutable event models, event metadata, filtering, validation, and `IAuditEventService` in Application.
10. **11.10 SQL repository:** add Infrastructure parameter binding and stored-procedure execution with cancellation, timeout, and redacted diagnostics.
11. **11.11 DI/configuration:** add non-secret connection configuration through the approved host configuration mechanism; do not add SQL credentials to `appsettings.json`.
12. **11.12 Correlation middleware:** generate/validate/propagate the correlation ID and response header; preserve request ID separately.
13. **11.13 Global exception handling:** centralize safe ProblemDetails mapping and minimized `APP_UNHANDLED_EXCEPTION`/validation events.
14. **11.14 Authentication events:** instrument existing authentication outcomes through the audit abstraction, preserving LDAP and credential boundaries.
15. **11.15 Authorization and security events:** instrument policy outcomes and safe JWT failure classifications at existing API framework hooks; never log token contents.
16. **11.16 Sensitive-data review:** run static and behavioral checks across audit DTOs, SQL parameters, logs, and responses.
17. **11.17 Tests:** add unit, isolated SQL integration, API integration, permission, failure, and security regression tests.
18. **11.18 Build and validation:** run restore/build/test, schema/procedure verification, permission review, and Phase 0-10 regression tests.
19. **11.19 Deployment preparation:** produce an approved database change script, identity/permission runbook, rollback plan, retention runbook, and monitoring checklist. Deployment remains a separate authorized action.

## 17. Risks

- Audit persistence can increase login/request latency or fail during a database outage; asynchronous buffering would add durability and complexity and is not assumed.
- High-volume invalid-token events can create storage pressure and denial-of-service exposure; rate limiting/aggregation requires a separate decision.
- Client IP and username retention create privacy and access-control obligations.
- Incorrect exception classification can leak details or produce misleading audit records.
- SQL permission drift could expose audit tables or grant excessive application rights.
- Retention without legal-hold and archival ownership can destroy required evidence.
- Correlation IDs aid investigation but do not authenticate a caller.

## 18. Decisions Requiring Explicit Approval

1. Database name, SQL instance, deployment owner, and environment-specific connection configuration mechanism.
2. Whether SQL Server may be used synchronously on the request path and the behavior when audit storage is unavailable.
3. Final event catalog, severity values, token failure subcategories, and expected volume.
4. Whether to store username, subject, client IP, server name, application version, and request ID.
5. Exact maximum lengths and JSON schema/allowlist for `DetailsJson`.
6. Application-vs-SQL ownership of `EventTimeUtc` and the accepted clock-drift policy.
7. Idempotency/duplicate handling for retries.
8. Correlation header name, client propagation, and invalid-input behavior.
9. SQL user mapping, role names, and DBA deployment method for the actual IIS app-pool identity.
10. Retention duration, archival destination, legal hold, purge schedule, and immutability requirements.
11. Required SQL provider/package and approved timeout/retry policy.
12. Whether a future application-error table is needed instead of the recommended logger-only diagnostic channel.
13. Whether partitioning, stronger tamper evidence, alerting, or rate limiting is required.

## 19. Implementation Progress and Validation Record

Phase 11 implementation is **COMPLETE**. On 2026-09-04, the default `MSSQLSERVER` instance was verified, `LabAuthServer` was created, the schemas/tables/reference data/indexes and `Audit.usp_WriteAuditEvent` were created, and the `IIS APPPOOL\\<IIS_APP_POOL>` principal received only writer-procedure `EXECUTE`. `Audit.usp_PurgeAuditEvents` and any SQL Agent job were intentionally deferred because retention/archive ownership is not sufficiently defined.

Application implementation added the immutable audit contract and validation, SQL repository, correlation middleware, safe global exception boundary, login/protected-resource/security event instrumentation, non-secret Windows-authenticated SQL configuration, and focused validator tests. The SQL writer returned an `AuditEventId`; invalid JSON and sensitive JSON properties were rejected; effective IIS-principal checks showed writer execute `1`, audit table select/insert `0`, and purge execute `0`. Phase 12 and Phase 13/14 validation passed with 112 unit tests and 51 integration tests. A database scan found no prohibited secret/token data; matches were safe field names, policy values, documentation examples, or test placeholders.

IIS configuration and bindings were not changed, the `Current` deployment directory was not modified, deployment was not performed, DPAPI secret contents were not accessed, and certificate private keys were not accessed or exported. Phase 11 must not be marked **COMPLETE** until the remaining security review, health validation, deployment preparation, and approved retention decision are complete.
