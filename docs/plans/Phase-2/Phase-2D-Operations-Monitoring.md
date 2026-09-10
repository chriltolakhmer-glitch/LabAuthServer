# Phase 2D — Operations & Monitoring

Status: PLANNED. [Phase 2 sequence](README.md).

# Objective

Provide actionable, safe operational evidence for application availability, dependencies, audit persistence and key lifecycle without turning health checks or logs into a new load or data-disclosure risk.

# Current State

Program.cs exposes anonymous GET /api/v1/health returning 200 with {"status":"Healthy"}. It is application liveness only; no LDAP/SQL/key readiness checks are registered. Logging uses Console and Debug. CorrelationMiddleware validates a canonical GUID supplied in X-Correlation-ID or generates one, stores request context and creates a logging scope.

Authentication/LDAP logs include usernames; AuthController's post-authentication catch and CertificateSigningKeyProvider lookup catch pass exception objects to the logger. This needs a privacy review against Security.md rather than an assumption that every current log is sanitized.

CertificateSigningKeyProvider enforces current validity, RSA >=2048, compatible KeyUsage, absence of EKU, unique valid thumbprint selection and signing-private/validation-public separation. It does not actively alert on approaching expiry. TokenOptions exposes PreviousKeyId, PreviousKeyExpiresAt and PreviousSigningCertificateThumbprint; validation rejects the previous key after overlap expiry. No proactive overlap dashboard exists.

Audit persistence failure logs exist, but no explicit metrics/alerts/readiness policy. Login rate limiting has no custom observation callback in Program.cs.

# Problem / Risk

Healthy liveness can coexist with unusable login, SQL audit loss or an expiring signing key. Dependency probes can amplify outages or disclose directory/store information. Log storms can fill disks; raw exceptions and high-cardinality identity labels can leak data and overwhelm metrics.

# Scope

Separate readiness, bounded dependency monitoring, classified outcome/latency metrics, safe logs/correlation, alerts/dashboards and Windows/IIS/SQL/AD operational ownership.

# Non-Goals

No observability vendor/package selection, distributed tracing backend, public dependency dump, directory enumeration, token issuance on every probe, automated certificate rotation or remediation automation.

# Proposed Architecture

Keep /api/v1/health unchanged as application-only liveness. Proposed readiness route: /api/v1/ready (DECISION REQUIRED: name/access policy). Prefer a host/network-restricted probe endpoint with a minimal aggregate response; do not silently add it to Authorization.PublicEndpoints or rely only on the default policy for arbitrary minimal endpoints. Detailed diagnostics stay protected and internal.

Use one bounded, timed probe schedule with cached results, freshness timestamps and non-overlapping execution. A health request reads the snapshot; it does not start a new LDAP/SQL call. Probe credentials remain in existing protected providers. Share 2B admission or reserve an explicitly bounded probe allowance so probes cannot starve login. Define startup unknown, stale, degraded, unhealthy and shutdown behavior.

| Signal | Probe/source | Proposed effect and alert |
| --- | --- | --- |
| Application liveness | Existing route, process uptime/start failures | Process unavailable is actionable; dependency failure alone does not restart-loop liveness |
| LDAP readiness | Minimal service-account bind/read using 2B boundary, no raw Root DSE attributes returned | Unavailable/stale LDAP makes login readiness unhealthy; alert AD/operator |
| SQL connectivity | Bounded least-privilege connection/procedure availability check plus actual write outcomes | Under 2C Option A: degraded, not automatically unready; alert audit loss |
| Audit persistence | Writer terminal outcome and time since last successful write | Failure/unknown outcome requires attention; absence of traffic alone is not proof of failure |
| Signing key | Certificate metadata and controlled private-key accessibility validation | Missing/expired/unusable active signing key makes login readiness unhealthy |
| Public validation key | Public key resolution through existing provider | Missing/invalid approved validation key fails readiness for protected-token traffic |
| Certificate expiry | Active/previous signing, IIS TLS, LDAP TLS and SQL TLS certificates via approved owner | Thresholds tied to renewal lead time; metadata only |
| Previous overlap | PreviousKeyExpiresAt plus previous certificate NotAfter and token lifetime/skew | Alert before effective previous-key acceptance ends; expired overlap is expected after a completed rotation |
| Rate limiting | Login 429/rejection counts and host early-rejection counts | Sustained changes or exhaustion investigated; never create username/IP labels |
| Authentication | Outcome/stage/duration from 2B | Separate invalid credentials from dependency/configuration/timeout |
| Authorization/JWT | 401/403 and bounded token failure categories | Baseline-relative spikes; audit actual decisions |
| Resource state | IIS/process counters and 2A/2B load observations | Worker/queue/memory/handle saturation, unexpected recycle or crash |

DECISION REQUIRED: readiness should represent the whole instance or distinct login/resource capabilities. Recommended initial aggregate readiness requires LDAP plus signing/validation capability; SQL is degraded under A. Document that this can remove healthy bearer-serving capacity during an AD outage. If routing supports capability-specific readiness, architect/operations must approve its scope before implementing it.

Readiness does not prove a real user can authenticate or receive the correct role. Routine signing probes must not export keys or produce reusable bearer tokens; choose a minimally privileged check with an explicit non-token challenge only if needed to verify key access. Certificate metadata alone does not prove private-key ACL access. Chain/revocation policy for JWT signing certificates is not currently implemented and must not be implied by monitoring.

## Logs and privacy

Log UTC time, severity/event code, application version, route template, HTTP method/status, bounded outcome/stage, elapsed time and canonical correlation/request IDs. Dependency labels should be logical names, not raw connection strings. Keep identity/role/IP only in the approved restricted audit channel when necessary; DECISION REQUIRED on operational username removal or pseudonymization. Metric labels must never contain username, subject, IP, raw URL, correlation ID, jti or exception text.

Never log passwords, request bodies, Authorization headers, bearer/refresh tokens, DPAPI plaintext/ciphertext, private keys, raw directory attributes/filters/responses, connection strings, certificate exports or stack traces. Replace unsafe exception-object logging in affected paths with bounded classifications. Preserve trusted audit field semantics without copying every audit field into console logs.

Use event categories and severity with rate-controlled repetitive diagnostics, but do not sample away the only signal of a failed audit write. Every terminal audit outcome increments a counter; repeated descriptive logs may be aggregated. Never write audit-failure logs back to SQL audit recursively. Correlation IDs are trace aids, not trusted identity or unique audit-event keys.

## Metrics and dashboards

Proposed names are illustrative, to be finalized before implementation:

- Request rate/duration/in-flight and 4xx/5xx by route template; body/header rejections and login 429.
- LDAP operation duration, active work, admission rejection, timeout/cancel/failure by stage.
- Audit attempts/persisted/failure/unknown/cancelled, write duration and freshness of successful writes.
- Probe duration/status/age and missed/overlapping probe attempts.
- Days/time to certificate expiry and previous-key effective acceptance end; avoid publishing thumbprints.
- Process CPU/private bytes/handles/thread-pool queue, app-pool recycles/crashes and disk free space.
- SQL connection pressure, storage/log growth, blocking/deadlocks, writer errors and maintenance/archive age.

Dashboard pages: service overview (traffic, latency, liveness/readiness); identity dependency (LDAP stages and capacity); audit pipeline (outcomes, SQL/retention health); key lifecycle (active/previous/TLS certificates and overlap); host capacity/security outcomes. Do not build these dashboards in this task.

## Alert rules and ownership

DECISION REQUIRED: thresholds, duration windows, severity, notification channel, on-call owner and escalation. Immediate actionable conditions include unusable active key, repeated startup configuration failure, unexplained audit loss and missing/stale probes. Near-expiry thresholds must allow renewal/change lead time. Sustained LDAP/SQL failure, 429/401/403 changes, saturation, disk/log growth and failed retention/archive jobs need baseline-based rules. Distinguish expected previous-key retirement from an outage.

Each alert must carry service/version, safe classification, affected capability, timestamp and a protected runbook reference. Include recovery notification and a test route for alert delivery. Do not send any notifications during planning.

# Implementation Steps

## Step 1

Approve readiness semantics/access and low-cardinality event contract with 2B/2C owners. Inventory existing logs and host monitoring; select available framework/host facilities before proposing dependencies.

## Step 2

Implement bounded cached probes and writer/request metrics in separate future increments. Add sanitized logging at LDAP, audit, key and HTTP boundaries. Keep the public liveness contract stable and protect readiness details.

## Step 3

Configure approved dashboards/alerts in the operational environment, rehearse failures and stale results, and document operator response. Validate observability overhead and hand evidence expectations to 2E.

# Source Areas Expected to Change

- src/LabAuthServer.Api/Program.cs and Health/HealthResponse.cs (preserve liveness DTO); proposed readiness DTO/probe coordinator under Api/Health.
- src/LabAuthServer.Api/Middleware/CorrelationMiddleware.cs, GlobalExceptionMiddleware.cs, AuthorizationAuditMiddleware.cs; Requests/CorrelationContext.cs only if scope propagation needs correction.
- src/LabAuthServer.Api/Controllers/AuthController.cs, Extensions/JwtBearerAuthenticationOptions.cs.
- src/LabAuthServer.Infrastructure/Services/LdapService.cs, LdapAuthenticationService.cs; Auditing/SqlAuditEventService.cs; Security/CertificateSigningKeyProvider.cs and TokenOptions.cs for metadata observation without key-policy changes.
- tests/LabAuthServer.IntegrationTests/HealthEndpointTests.cs and ProtectedEndpointTests.cs; UnitTests/CorrelationAndExceptionMiddlewareTests.cs, CertificateSigningKeyProviderTests.cs and new probe/log tests in existing projects.
- Future environment runbooks/configuration for IIS counters, SQL jobs and AD/LDAPS monitoring; no new project dependency by default.

# Configuration Changes

Proposed operational options: enablement, probe interval/deadline/stale age, bounded concurrency, readiness access and expiry thresholds. DECISION REQUIRED on values and telemetry destinations/log retention. Validate finite intervals and total budgets; external alert routing remains operational configuration. No actual configuration change now.

# Database Changes

None for basic monitoring. Check SQL through existing least-privilege boundaries; do not grant server-wide monitoring privileges to the API identity. DBA monitoring uses a separate approved principal. Retention work remains owned by 2C.

# Testing Strategy

## Unit Tests

Use fake clock/probes to verify status aggregation, SQL degraded under A, startup unknown, stale results, non-overlap, cancellation and cleanup. Verify expiry/overlap boundaries and log redaction with synthetic sensitive values.

## Integration Tests

Liveness remains anonymous 200 with canonical correlation. Readiness returns approved aggregate 200/503 behavior with restricted access and no raw dependency details. Parallel probe requests cause no extra native work. Ensure correlation survives error paths and background jobs use their own safe operation IDs.

## Security Tests

Attempt anonymous diagnostic access, forged correlation headers and high-cardinality inputs. Verify no private key export, raw exceptions or credentials in telemetry. Flood probe route during LDAP outage; prove bounded resource use and no arbitrary-host probing.

## Regression Tests

Retain endpoint auth/role semantics, 2A limiter, 2B outcomes and 2C response-preserving audit failures. Monitoring cannot alter certificate selection or extend previous overlap.

## Manual Validation

Windows/IIS operator validates app-pool identity, recycles, Event Viewer/host logs, counters and disk retention. DBA validates encrypted connectivity, blocking, disk/log and maintenance signals. AD operator validates service-account health, LDAPS TLS and minimal query latency. Rehearse alert delivery/recovery and monitoring-backend outage in staging.

# Security Considerations

Monitoring data is restricted operational data. Apply access/retention limits and sanitize sinks, not just public responses. Trust neither correlation IDs nor log-supplied usernames for authorization.

# Operational Considerations

Probes must have owners, budgets and freshness; missing telemetry is distinct from health. Centralized logging backend selection can remain Phase 5, but Phase 2 still needs usable local/host alerts. No automatic AD/SQL/IIS remediation is proposed.

# Compatibility Risks

New readiness semantics can remove traffic during dependency outages. Logging identity changes affect investigations. Extra probes consume service-account/SQL capacity. Changes to response middleware may affect correlation/header order.

# Rollback Plan

Disable new probes/exporters through approved configuration while retaining existing liveness and minimum local failure logs. Restore previous routing/readiness configuration together; do not leave load balancers targeting a removed route. Preserve alert evidence and never use readiness rollback to bypass authentication.

# Acceptance Criteria

All defined signals have a source, owner and tested failure/recovery path. Liveness remains application-only. Readiness respects 2C's selected audit policy and rejects stale evidence. Probes and log volume remain within approved bounds; redaction tests and alert delivery pass.

# Definition of Done

Approved readiness/access contract, tested safe telemetry, usable dashboards/runbooks, measured overhead and staging alert evidence. Backend selection and notification recipients are recorded through protected operations configuration.

# Open Decisions

DECISION REQUIRED: aggregate versus capability readiness, route/access, intervals/staleness/thresholds, certificate accessibility check, sink/retention, identity handling and on-call routing. No vendor or new package is preapproved.

# Dependencies

Consumes 2A rejection/resource signals, 2B categories/bounds and 2C audit/readiness semantics. Dashboard and alert design can start earlier; integration closes before 2E.
