# Phase 2C — Audit Reliability

Status: PLANNED. [Phase 2 sequence](README.md).

# Objective

Make audit loss, latency and SQL outage behavior explicit and observable, then choose the least complex persistence model that satisfies approved requirements. Define retention and integrity validation without automatically adding an outbox.

# Current State

src/LabAuthServer.Infrastructure/Auditing/SqlAuditEventService.cs validates AuditEvent before SQL, opens a connection and executes Audit.usp_WriteAuditEvent with typed parameters. It returns the inserted long ID, rethrows ArgumentException, and logs/returns null for other exceptions including most SQL failures and cancellation. Callers generally await writes but ignore a null result and catch thrown errors to preserve the primary response.

Call sites include AuthController (login outcomes), ProtectedController (access granted), JwtBearerAuthenticationOptions (authentication failures), AuthorizationAuditMiddleware (403) and GlobalExceptionMiddleware (500). RequestAborted is generally passed to SQL. Audit is best-effort but synchronous with request completion: connection-open delay and command delay can still affect latency.

Audit.CommandTimeoutSeconds is five in supplied configuration and validated in the range 1–60. Connection-open timeout is a separate connection-string concern. SQL defaults encrypt transport. The writer procedure commits its insert before returning the result; a network failure after commit is an ambiguous outcome.

database/Phase11 contains an identity primary key, 13 catalog event types, constraints and event-time/correlation indexes. It has no caller-supplied unique event identifier or replay ledger. CorrelationId is not unique and multiple valid events can share it. Retention, archival, purge and scheduled jobs are absent.

# Problem / Risk

SQL failure can silently lose security history from the caller's perspective. Cancellation/process termination can lose an event. Retry after ambiguous commit can duplicate it. SQL connection/command delays and per-invalid-token writes can amplify denial of service. Unlimited retention threatens database/log disk capacity. Append-oriented storage is not cryptographic tamper evidence.

# Scope

Requirements and option selection, classified write outcomes, bounded latency, monitoring independent of SQL, primary-response semantics, retention/archival ownership and integrity tests. Preserve stored-procedure-only application writes and least privilege.

# Non-Goals

No automatic outbox, transactional coupling to AD, token/session database, new queue broker, blanket retry package, application purge permissions or claimed exactly-once guarantee.

# Proposed Architecture

## Requirements gate

DECISION REQUIRED: security/business owner must specify acceptable event loss by event type, whether successful login requires durable recording, allowed request latency, outage availability, recovery point/time objectives, acceptable duplicate behavior, peak volume, retention duration, privacy/legal holds and operator ownership.

Recommendation: Option A is the smallest initial Phase 2 solution if best-effort loss is explicitly accepted. It preserves current availability and avoids database architecture changes. Monitoring reveals failure; it cannot recover lost events. If loss is unacceptable, Option A cannot meet acceptance and C/D require architect review before implementation.

| Option | Guarantees and cost | Failure/compatibility implications | Selection |
| --- | --- | --- | --- |
| A: current best-effort + strong monitoring | No durability beyond successful SQL write; classify outcomes and bound latency | Events can be lost on outage/cancel/crash; preserve primary responses | Recommended initial model, conditional on explicit loss acceptance |
| B: bounded retry | Helps known transient pre-commit failures; adds latency/load | Ambiguous commit risks duplicates; needs classification, finite attempts/time/backoff and idempotency for replay | OPTIONAL only after evidence and requirements |
| C: durable local spool or limited outbox | Accepted local append can survive process restart, subject to filesystem/host durability | Needs ACLs, capacity, encryption decision, replay, poison-event handling, disk-full policy and multi-instance ownership | FUTURE/OPTIONAL if loss requirement rejects A |
| D: full transactional/outbox architecture | Atomicity within a chosen durable database boundary | AD bind and HTTP delivery are not part of a SQL transaction; introduces a persistent workflow and significant architecture | FUTURE/OPTIONAL, separate approval |

For A, retain the existing long?/exception interface if sufficient: centralize exactly one terminal write-outcome metric/log at the writer and handle validation rejection explicitly. Evaluate call sites for semantic correctness rather than logging every null again. No fire-and-forget request task or unbounded in-memory queue. Keep connection and command operations under an approved total audit deadline, respecting the chosen cancellation policy.

## Outcome and interaction policy

| Condition | Classification/action under recommended A | Primary request |
| --- | --- | --- |
| SQL returned valid ID | Persisted | Normal response |
| Invalid AuditEvent/SQL validation rejection | Permanent defect, safe diagnostic and alert | Preserve established safe response; never grant on validation failure |
| SQL login/permission/TLS/configuration error | Permanent/configuration until repaired; no automatic retry | Authentication/authorization decision unchanged |
| Network unavailable/deadline | Dependency/timeout; count loss risk or unknown outcome | Primary response preserved within approved budget |
| Caller cancellation | Cancelled or unknown outcome; count separately | No further response where disconnected |
| Procedure committed but acknowledgment lost | Unknown persistence, not proven loss | Do not blindly replay |
| Missing procedure result | Protocol failure/unknown outcome | Safe log and investigation |

Under A, SQL outage alone is degraded operation, not automatic readiness failure. Login still requires successful AD, mapping and signing; audit unavailability never converts 401/403 to success. Authorization logs reflect the actual endpoint result. Audit failure diagnostics must not call IAuditEventService recursively. Already-started responses must not be rewritten.

If the owner requires durable login auditing, define the exact commit point and failure contract first; a successful SQL row does not prove the client received a token. Do not issue a success response before the selected durability guarantee. This is an explicit architecture/availability decision, not a hidden configuration toggle.

## Retry and duplicate prevention gate

If B is selected, retry only an approved transient allowlist within a total deadline, with bounded jitter/backoff and cancellation. Never retry invalid payloads, credentials/permissions, certificate trust or application validation failures. Determine whether a failed operation definitely did not commit.

Before replaying any uncertain outcome, approve a stable producer event ID carried through every attempt and enforced uniquely by SQL with atomic insert-or-return behavior. Identity AuditEventId, request ID and correlation ID cannot serve that purpose. Concurrent retries must yield one logical event, preserve original event time/payload and not collapse different events from the same request. Schema/procedure changes require architect/DBA approval and backwards-compatible rollout. If duplicate prevention is not approved, do not replay ambiguous writes.

## Retention and archive/purge policy

DECISION REQUIRED: retention duration per event class, cutoff basis (EventTimeUtc versus CreatedAtUtc), late event treatment, legal hold representation, archive destination/access/encryption, archive verification, backup retention, batch size/window, schedule technology and DBA owner.

Recommend a separately operated DBA maintenance process. Keep the application role execute-only on the writer. Stage future maintenance as: read-only candidate count/dry run; verified archive with manifest/counts and restore sample; bounded batches against a stable cutoff; verification and recorded maintenance outcome. Never purge held/unverified data. Set maintenance disabled until policy and recovery rehearsal are approved. SQL Agent availability/edition is a decision, not an assumption.

Use existing time indexes where appropriate; assess locks, log growth, concurrent writes and query plans before adding indexes/schema. Any future changes should be new reviewed migration/maintenance scripts, not destructive rewrites of baseline scripts. Retention settings belong in the approved maintenance configuration unless an application responsibility is explicitly selected.

# Implementation Steps

## Step 1

Inventory all audit producers, their expected event counts and response timing. Approve requirements and select A/B/C/D. Capture SQL outage latency and null/exception handling with no production fault injection.

## Step 2

For recommended A, add centralized classified outcomes, bounded connection+command deadline and nonrecursive operational signals. Preserve existing HTTP decisions and event vocabulary. Test each producer during SQL failure. Add retries only through the separate B gate.

## Step 3

Design/rehearse approved retention with DBA, verify archive recovery and permission boundaries, then supply audit degradation semantics and metrics to 2D. Deliver observability and maintenance as separate future changes.

# Source Areas Expected to Change

- src/LabAuthServer.Infrastructure/Auditing/SqlAuditEventService.cs and AuditOptions.cs; src/LabAuthServer.Api/Program.cs option validation.
- src/LabAuthServer.Application/Interfaces/IAuditEventService.cs only if an explicit outcome type is justified; Application/Auditing/AuditEvent.cs and AuditEventValidator.cs only for approved contract changes.
- src/LabAuthServer.Api/Controllers/AuthController.cs, ProtectedController.cs, Extensions/JwtBearerAuthenticationOptions.cs, Middleware/AuthorizationAuditMiddleware.cs and GlobalExceptionMiddleware.cs.
- tests/LabAuthServer.UnitTests/SqlAuditEventServiceTests.cs, AuditEventValidatorTests.cs and CorrelationAndExceptionMiddlewareTests.cs; IntegrationTests/AuthenticationTests.cs and ProtectedEndpointTests.cs.
- database/Phase11/02_CreateSchemasTables.sql, 03_CreateAuditProcedures.sql and 04_ConfigureAuditPermissions.sql are baseline references for future additive migrations, not files to alter in this planning task.

# Configuration Changes

Reuse Audit.ConnectionString and CommandTimeoutSeconds. DECISION REQUIRED: overall audit budget, connection timeout and cancellation policy. B would additionally require approved finite retry settings; C requires storage/capacity/replay settings. Do not add inactive speculative options. Production retains encryption and normal certificate trust.

# Database Changes

Option A monitoring needs no schema change. Approved retention may need additive maintenance procedures/jobs and a separate maintenance principal. B with replay needs the approved event-ID uniqueness change. C/D need their own architecture approval. No database files change now.

# Testing Strategy

## Unit Tests

Classify safe SQL error categories, validation exceptions, null results, cancellation and unknown outcomes with deterministic seams. Verify one terminal metric, bounded labels and no recursive audit. Use an injectable delay/clock if retry is approved.

## Integration Tests

Existing SqlAuditEventServiceTests includes real SQL despite its project name. Use an authorized isolated database to verify typed writes, returned IDs, encryption/trust, least privilege, outage latency and concurrent distinct events. Exercise all HTTP producers with null and throwing writers.

## Security Tests

Reject nested sensitive JSON, oversized details, unknown/retired event types and invalid roles. Verify application identity cannot alter/delete tables or execute maintenance. Inspect operational output for absence of connection strings, credentials, tokens and raw SQL exceptions.

## Regression Tests

Preserve primary success/401/403/500 outcomes under SQL failure. Preserve event types, UTC time, correlation, role and status accuracy. If B is selected, simulate post-commit lost acknowledgment and parallel retries to prove idempotency; otherwise assert no automatic replay.

## Manual Validation

DBA rehearses archive/purge with synthetic aged/boundary/held/late rows, live concurrent writes, interrupted batches, disk/log pressure and archive restoration. Reconcile counts and approved integrity hashes/manifests; do not claim this makes the live database tamper-proof.

# Security Considerations

Keep operational diagnostics available when SQL is down and protect their access. Audit identities/IPs are sensitive personal/operational data; retention and access apply to exports/backups too. Do not collect payloads merely to troubleshoot lost events.

# Operational Considerations

Track attempts, persisted, rejected, unavailable, timeout, cancelled and unknown outcomes plus latency. Use independent monitoring for audit failure; alert on loss risk, disk growth, maintenance failure and overdue archives. Under A, recovery resumes new writes; missing events cannot be backfilled from memory.

# Compatibility Risks

New deadlines can increase dropped events while improving response latency. B adds delay and replay semantics. Retention changes investigator access to old history. Any durability-required policy can make SQL an authentication availability dependency.

# Rollback Plan

Restore prior audit implementation/configuration while retaining Phase 1 encryption and approved diagnostics. Disable retry/replay safely, preserving pending durable events if C/D was selected. Pause maintenance on failure; do not delete its state or audit history. Deleted data requires verified archive/backup restoration through DBA change control, not an application rollback. Event-ID schema changes require a compatible reader/writer rollback design before rollout.

# Acceptance Criteria

Selected loss/availability policy is signed off; every attempted write has a classified terminal outcome within the approved budget. HTTP authorization semantics are unchanged under A. No retry duplicates or claimed guarantees without evidence. Retention has an owner, tested dry-run/archive/restore/purge process and no application maintenance privilege; activation follows separate authorization.

# Definition of Done

Option decision, measured latency/outage evidence, safe independent alerts, producer regressions, integrity/permissions evidence and retention recovery procedure are reviewed. If loss requirements exceed A, this stage cannot be declared complete until the approved alternative is delivered.

# Open Decisions

DECISION REQUIRED: event loss/durability, availability during SQL outage, total latency, cancellation, retention/holds/archive policy, maintenance technology and whether retry/idempotency or durable architecture is justified.

# Dependencies

Consumes 2B error categories and 2A workload budgets. Defines SQL degraded/readiness policy for 2D. 2E validates writes, trust, privilege and outage behavior.
