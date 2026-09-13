> Historical multi-owner workflow, superseded 2026-09-13 by the [solo developer release checklist](../../plans/Phase-6/Release-Checklist.md). Original decisions, pending items, and results below are retained as history, not future release gates.

# Phase 6.5 — Audit and Operational Acceptance Design

Status: IMPLEMENTED DESIGN; OPERATIONAL ACCEPTANCE AND OWNER DECISIONS REMAIN OPEN.

## Purpose and boundary

This document defines the operational contract and evidence required for audit durability and observability. It does not implement a queue, retry policy, metrics exporter, alert integration, readiness endpoint, retention job, SQL change, deployment, or customer operation. No operational owner is inferred from the repository.

Phase 6.5 is complete as a design package only. Phase 6.6 target-environment acceptance and Phase 6.7 first-release readiness remain outside this work.

## Current implementation evidence

| Area | Current behavior | Evidence boundary |
| --- | --- | --- |
| Audit validation | `AuditEventValidator` rejects invalid or sensitive events before SQL configuration/connection use. | Unit tests and application audit contracts |
| Audit persistence | `SqlAuditEventService` calls only `Audit.usp_WriteAuditEvent` with typed parameters and a bounded command timeout. | Infrastructure code and SQL scripts |
| Failure behavior | Database and command failures are caught, logged, and returned as `null`; there is no retry, durable queue, replay path, or failure counter. | `SqlAuditEventService` and middleware tests |
| Request impact | Callers await the write and pass `RequestAborted`; a failed write does not replace the primary response, but connection/command wait can add latency up to the configured timeout. Cancellation can prevent persistence. | Audit documentation, service implementation, and tests |
| Logging | Console/debug providers are enabled. Audit failures are emitted as structured-template error/warning messages without passwords, tokens, authorization headers, raw LDAP data, or exception dumps. | `Program`, audit service, and middleware |
| Health | `GET /api/v1/health` is anonymous and returns `200 Healthy` as application liveness. It does not check SQL, LDAP, certificate-store access, licensing, or audit durability. | `Program` and `HealthEndpointTests` |
| Metrics/monitoring | No application metrics, audit backlog, alert integration, or monitoring platform configuration is implemented. | Repository-wide source review |
| Retention | Retention, archival, purge, backup policy, restore testing, and SQL Agent scheduling are not implemented by the repository. | `Database.md`, SQL scripts, and current status docs |

## P6-D7 — Audit loss tolerance

**Current contract:** audit persistence is best effort. A persistence outage may lose an event; the application does not retain it for replay. This is an observed behavior, not an approved loss budget.

**Required decision:** owner and operations/DBA input must define whether any loss is acceptable and the maximum tolerated loss, expressed at minimum as an event count or time window. They must also define escalation when the tolerance is exceeded. Until approved, audit durability is an open acceptance gate and must not be described as guaranteed.

**Required evidence:** inject SQL unavailability and cancellation, record attempted/successful/failed events, request outcomes, elapsed time, and log correlation IDs; demonstrate the agreed escalation path without exposing sensitive data.

## P6-D8 — Audit latency and SQL outage policy

**Observed behavior:** audit writes are awaited on the request path where invoked, have no retry, and use the configured five-second maximum command timeout. The primary response is not replaced solely because persistence returns `null`; an audit attempt can still delay completion, and cancellation can prevent the write.

**Decision gate:** owner plus operations/DBA must approve all of the following:

- the maximum audit latency budget and whether it is included in endpoint SLOs;
- whether degraded operation remains best effort or any event class must fail closed;
- whether a future retry/outbox/replay design is required, including its durability and capacity contract;
- the operator-visible condition and escalation when SQL is unavailable.

No retry or weaker audit guarantee is introduced by this phase.

**Required evidence:** healthy SQL, unavailable SQL, timeout, cancellation, and recovery scenarios with request status, latency, audit outcome, logs, and correlation. The evidence must prove that no secret or token is logged and must distinguish an audit failure from an application failure.

## P6-D9 — Retention and storage responsibility

No legal or contractual retention period is selected. No purge, archive, SQL Agent schedule, backup policy, restore test, or storage-growth control is implemented.

| Decision | Required owner/input | Acceptance evidence |
| --- | --- | --- |
| Retention period and exceptions | Owner + legal/compliance review as applicable | Approved written policy |
| Audit database storage ownership | DBA / database service owner: **TBD** | Named owner and access boundary |
| Purge/archive execution and approval | DBA/operations owner: **TBD** | Reviewed procedure, schedule, and failure escalation |
| Backup and restore coverage | DBA: **TBD** | Restore evidence and recovery target decisions |
| Growth monitoring and thresholds | Operations/DBA: **TBD** | Capacity baseline, threshold, and alert route |

Application audit data must remain distinct from release evidence and governance records. No retention claim is accepted until these decisions and evidence exist.

## P6-D10 — Monitoring and alert design

Monitoring is a requirement, not an implemented capability. No production alert is configured by this repository and no owner is assigned.

Required measurements are:

- audit events attempted, persisted, failed, and cancelled, by event category;
- audit write latency, timeout count, and SQL outage duration;
- request outcome and latency for affected endpoints, without recording credentials or token material;
- SQL connectivity/procedure availability and audit table growth/capacity;
- liveness and, if approved, readiness state;
- LDAP authentication failure/timeout rates;
- certificate/key validity horizon and license restricted-mode state.

Required alert categories and placeholders:

| Alert category | Trigger/threshold | Owner | Status |
| --- | --- | --- | --- |
| Audit persistence failure/loss | Approved D7 threshold | Operations: **TBD** | Not configured |
| SQL availability/latency | Approved D8 threshold | DBA/operations: **TBD** | Not configured |
| Storage growth/retention | Approved D9 threshold | DBA: **TBD** | Not configured |
| LDAP failure/timeout | Approved environment threshold | Operations: **TBD** | Not configured |
| Certificate/key expiry | Approved lead time | Security/platform: **TBD** | Not configured |
| Restricted license mode | Approved operational response | Owner/operations: **TBD** | Not configured |

Thresholds, notification channels, escalation time, and on-call ownership require external approval. The repository must not claim these controls exist.

## P6-D11 — Liveness/readiness contract

**Current contract:** `/api/v1/health` is pure application liveness. It is anonymous, static, and dependency-independent. It must remain suitable for process/route checks and must not be redefined silently to fail because SQL, LDAP, or certificate access is unavailable.

**Recommended contract for owner/architect approval:** retain `/api/v1/health` for liveness and, only if deployment requires dependency gating, add a separately specified readiness endpoint with explicit dependency checks, timeouts, status semantics, and startup behavior. Readiness must not cause destructive retries or expose secrets.

No readiness endpoint or health dependency check is added in Phase 6.5. Approval and target-environment evidence are required before changing deployment behavior.

## Operational acceptance evidence package

Acceptance must include:

1. Source-to-contract review confirming the behavior table above.
2. Automated audit validation and failure-boundary results from the required repository commands.
3. Controlled SQL outage, timeout, cancellation, and recovery evidence with correlation and latency data.
4. Approved D7-D11 decisions, named owners or explicit owner placeholders, thresholds, escalation, and retention/backup evidence.
5. A review record showing that no release, deployment, production monitoring rollout, database migration, customer operation, or Phase 5.6 change occurred.

The evidence package is not target-environment acceptance and does not authorize Phase 6.6 or 6.7.

## Validation performed for this design

The baseline gate was verified before implementation:

- `HEAD` and `origin/main`: `9fd3dc1ddc82942f72d9f509295e9ec2b9a2c691`;
- commit: `Implement Phase 6.4 release qualification`;
- hosted `LabAuthServer CI`: run `34729638947`, completed, success, same head SHA.

The required local validation remains:

```powershell
dotnet restore
dotnet build LabAuthServer.slnx -c Release --no-restore
dotnet test LabAuthServer.slnx --no-build --no-restore
```

This phase changes documentation only; no runtime test is added because no runtime behavior changed.

## Deferred decisions and non-authorizations

P6-D7, P6-D8, P6-D9, P6-D10, and P6-D11 remain deferred pending owner, operations, DBA, architecture, legal/compliance, and target-environment input as applicable. No operational owner is invented here.

This phase does not authorize Phase 6.6 environment acceptance, Phase 6.7 first-release readiness, release creation, deployment, customer delivery, production monitoring rollout, external alert configuration, SQL infrastructure change, retention deletion jobs, or any Phase 5.6 change.
