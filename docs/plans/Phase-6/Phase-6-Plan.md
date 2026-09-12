# Phase 6 — Operational Assurance and First-Release Readiness

Status: OWNER-APPROVED ROADMAP; ONLY PHASE 6.1 IMPLEMENTATION AUTHORIZED by the 2026-09-12 task. Later subphases are planning entries requiring separate approval. [Phase index](README.md).

## Objective

Establish a safely testable, operationally evidenced baseline that can eventually support a separately authorized first release.

## Entry state

- Baseline: clean `main`, `0b6d1ee92220090fb5570323e30f10bfc2dd9ed5`, `Record Phase 5 copyright owner decision`.
- `LabAuthServer CI` #39, run `34699662852`, succeeded on that exact SHA.
- Phase 5 owner-value/governance decisions are complete, including owner-approved P54-D1–D6 and P5-C1–C6.
- Phase 5.6 remains platform/ownership-model blocked or configured but unenforced. Nothing in this plan changes those controls or claims their enforcement.
- The repository remains private; ALOT role assignments, approved private-delivery policy, no fixed support calendar and no LTS remain unchanged.
- No real commercial release, customer-license issuance or deployment is authorized.
- Professional legal/business review remains external and pending.

## Ordered workstreams

| Subphase | Scope and intended evidence | Authorization / dependencies |
| --- | --- | --- |
| [6.1 — Safe Automated Validation Boundaries](Phase-6.1-Safe-Automated-Validation-Boundaries.md) | Infrastructure-safe default tests; explicitly selected SQL/LDAP categories; mandatory disposable SQL CI coverage | Implementation authorized in this task; existing test stack only |
| 6.2 — Authorization and Audit Boundary Corrections | Approve fallback/public/unmatched-route behavior; correct successful-access oversized audit identity handling with regression tests | PLANNED ONLY; separate owner/architect approval; 6.1 safe validation |
| 6.3 — Licensing Boundary Assurance | Reliable restricted provider initialization; reconcile catalog/diagnostic and startup-cached expiry contracts; qualify actual feature/limit enforcement | PLANNED ONLY; preserve frozen V1 and core-security independence; separately approve semantic changes |
| 6.4 — Release-Build and Documentation Reconciliation | Correct LicenseIssuer Release configuration and reconcile current summaries against actual evidence while retaining history | PLANNED ONLY; separate approval; issuer remains outside the solution in 6.1 |
| 6.5 — Audit and Operational Acceptance Design | Decide acceptable audit loss/latency, retention ownership, readiness/alert requirements and measurable capacity/acceptance thresholds | PLANNED ONLY; owner/architect/DBA/operations decisions; no automatic outbox, purge or monitoring implementation |
| 6.6 — Target-Environment Acceptance | Authorized exact-build real AD → JWT → protected access; key/DPAPI/SQL permissions, TLS, capacity/outage and rollback evidence | PLANNED ONLY; approved environment and credentials, prior corrections and acceptance criteria; no implied deployment authority |
| 6.7 — First-Release Prerequisite Readiness | Exact manifest/register storage and private-delivery channel; custody/recovery and external registers; tag-signing disposition; professional review and release evidence checklist | PLANNED ONLY; vendor, legal/business and infrastructure dependencies; release execution remains separate |

Subphases are independently reviewable. Do not combine production fixes into test isolation. A later numbering entry is not implementation approval.

## Non-goals

Unless separately approved: online activation; online revocation; billing; customer portal; admin-console implementation; MFA/federation; refresh/session architecture; elaborate DRM; microservices; Kubernetes; multi-region architecture; LTS promises; public-release automation; production signing material in CI; GitHub ownership/plan transfer; reopening Phase 5 owner decisions.

The previous Phase 6 advanced-security candidates (HSM, advanced threat detection, stronger key lifecycle, policy engine, enterprise integrations and security automation) remain optional future requirements topics. Reprioritizing the roadmap does not approve those designs, packages or infrastructure.

## Approval and safety boundaries

Preserve `Domain <- Application <- Infrastructure <- Api`. Packages, architecture and production behavior require their existing approval gates. No incidental authentication, database, license-format, deployment or governance changes are authorized. Preserve historical evidence and explicitly distinguish source tests from live environment acceptance.

## Exit principle

Phase completion means **READY TO REQUEST SEPARATE RELEASE AUTHORIZATION**, never **RELEASE AUTHORIZED**.

Exit evidence must demonstrate safe classified validation, approved boundary corrections, Release coverage, accurate current documentation, accepted operational loss/retention/monitoring decisions, authorized target-environment acceptance and completion or explicit disposition of pre-release professional/operational/platform conditions. Actual release, tags, artifacts, license issuance and deployment each retain their separate authorization requirements.
