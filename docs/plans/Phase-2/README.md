# Phase 2 — Production Hardening & Operational Resilience

Status: PLANNED. Baseline and evidence caveats: [master roadmap](../README.md). This phase plans the remaining security and operational weaknesses identified by the Phase 1 review and verified against current source.

## Objective

Bound work at HTTP and LDAP boundaries, make dependency and audit failures distinguishable and observable, and establish repeatable Windows/IIS production validation without weakening Phase 1 security.

Phase 1 delivered private/public JWT key separation, certificate validity/RSA/usage/duplicate-selection checks, encrypted SQL defaults, and a constant-partition login limiter of 10 requests per minute with no queue. Preserve these controls. Phase 1 completion does not prove live environment readiness.

## Scope and implementation order

| Order | Plan | Deliverable and exit gate |
| --- | --- | --- |
| 2A | [API & Resource Protection](Phase-2A-API-Resource-Protection.md) | Approved HTTP limits, host/HTTPS boundary and headers; oversized requests cannot reach business logic |
| 2B | [LDAP Reliability & Fail-Closed Behavior](Phase-2B-LDAP-Reliability.md) | Typed group outcomes, bounded execution and tested cleanup; failed lookup cannot issue a token |
| 2C | [Audit Reliability](Phase-2C-Audit-Reliability.md) | Approved loss/latency policy, monitored write outcomes and retention procedure |
| 2D | [Operations & Monitoring](Phase-2D-Operations-Monitoring.md) | Separate liveness/readiness, bounded probes, safe metrics and actionable alerts |
| 2E | [Production Environment Validation](Phase-2E-Production-Validation.md) | Authorized environment evidence and explicit security/operations sign-off |

Recommended dependency order: 2A -> 2B -> 2C -> 2D -> 2E. 2A sets transport and work budgets used by 2B and probes. 2B defines error categories consumed by audit and monitoring. 2C decides whether SQL loss is degraded service or readiness failure. 2D supplies the evidence and alerting used by 2E.

Safe parallel work includes 2A traffic measurement, 2B LDAP cancellation investigation, 2C retention/loss requirements, 2D dashboard/alert design, and 2E environment inventory/runbook drafting. These can proceed independently; shared controller, Program.cs, and event-contract edits should be serialized. Final end-to-end validation waits for all selected controls.

## Non-goals

No authentication redesign, role expansion, token lifecycle store, MFA, federation, nested AD group expansion, distributed deployment, automatic outbox adoption, or new dependency by default. Preserve four-layer direction, LDAPS/LDAPv3, submitted-UPN binding, DPAPI LocalMachine protection, generic failures, public-key-only bearer validation, and existing role precedence. Reader policy is exact-role based: Administrator and Operator do not automatically satisfy it.

## Dependencies and decisions

Owners: architect approves contracts and architecture; API maintainer implements boundaries; Windows/IIS operator owns host settings and identities; AD operator owns dependency validation; DBA owns transport, permissions and retention; security owner approves data loss and release evidence.

DECISION REQUIRED before dependent implementation: actual IIS hosting/proxy topology; valid hostnames; HTTP limits and total latency/concurrency budget; no-role HTTP contract; acceptable audit loss and outage availability; retention/legal hold ownership; probe access, intervals, thresholds and destinations. Record decision, owner, rationale, date, compatibility impact, and acceptance test in the relevant detailed plan. Do not guess environment-specific numbers.

## Testing strategy

Use existing xUnit projects and WebApplicationFactory. Controller-only tests do not prove middleware, IIS filtering, or request-size enforcement. Separate deterministic fake-based tests from explicitly authorized Windows/AD/SQL checks; the existing SQL tests under UnitTests require a database.

Future implementation runs restore, Release build, and applicable/full solution tests using the pinned SDK; record actual counts and zero-warning/zero-error build evidence. Add boundary, cancellation/race, dependency outage, privacy and regression tests described in each plan. Use real-host staging tests for HTTP filtering, TLS, worker saturation and certificate/DPAPI identity. Do not run build/test/publish during this planning task.

## Acceptance criteria

- Each subplan's measurable criteria and chosen decisions are satisfied with redacted evidence.
- Oversized traffic is rejected before expensive work; legitimate configured JWTs remain usable.
- No dependency failure, incomplete group search, cancellation or missing role yields a token.
- Audit outcome and loss semantics are explicit; audit failures never grant authorization.
- Liveness remains application-only; readiness and telemetry reflect approved dependency policy without leaking details or amplifying outages.
- Existing issuer/audience/role/key/certificate/LDAPS/SQL/rate-limit protections remain covered.
- 2E records PASS, FAIL, BLOCKED or NOT RUN per case; neither safe failure tests nor historical records substitute for live validation.

## Rollback strategy

Use separate future releases and the existing staged deployment procedure. Preserve previous application package and approved external configuration. Roll back only the affected increment after compatibility checks; never disable TLS trust, fail-closed authorization, or Phase 1 controls. Retain audit records and any durable pending events. HSTS persists in clients, so rollback is not instantaneous. Database/retention changes require a separately reviewed recovery procedure; disabling a job cannot restore deleted rows.

## Production validation strategy

Draft 2E now; perform it only in a later authorized environment window. Rehearse outages, overload, certificate failures and restart behavior in staging. Production validation uses controlled identities, minimal traffic and protected evidence. Fault injection, app-pool restart, credential/ACL/binding changes and SQL outages require their own approved window; none occurs here.

## Risks

Tight HTTP limits may reject valid tokens. Blocking LDAP cancellation may leave native work running. Retry can amplify outages or duplicate committed events. Best-effort audit can lose events. Readiness can remove every instance during dependency outages. Alerts can expose identity data or create log storms. HSTS mistakes persist. Address these through bounded budgets, explicit contracts, limited rollout and the detailed rollback gates.

## Definition of Done

All selected Phase 2 work is separately reviewed, validated and operationally owned; outstanding risks have an explicit owner and acceptance. Future implementation updates current documentation under its own authorized scope. This planning delivery does not mark Phase 2 complete.
