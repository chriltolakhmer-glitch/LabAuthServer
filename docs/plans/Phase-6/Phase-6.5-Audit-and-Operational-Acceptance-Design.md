# Phase 6.5 — Audit and Operational Acceptance Design

Status: PLANNED — NOT AUTHORIZED FOR IMPLEMENTATION.

## Purpose

Define the operational acceptance and risk policy for audit persistence, retention, monitoring, and readiness before any production or first-release authorization decision is made. This is a design-first subphase; it does not implement logging, queues, or monitoring platforms.

## Current confirmed behavior

The current repository implements SQL audit persistence through the SQL writer and validation path, but it does not implement retention, purge, archival, or SQL Agent automation. The repo currently distinguishes application liveness from environment-dependent availability but has not yet established a formal operational acceptance standard for audit persistence outages or retention decisions.

## Required decision gates

### Audit loss policy

The owner must decide:

- how much audit loss is acceptable when SQL persistence fails;
- whether best-effort logging is an acceptable operational risk;
- whether audit loss must be explicitly monitored and reported.

### Request latency policy

The owner, with operations input, must decide:

- what request latency is acceptable when audit persistence is synchronous;
- whether the application should continue in degraded mode or fail closed on audit outage.

### Database outage handling

The operation design must distinguish:

- application request behavior;
- logging and monitoring behavior;
- alert ownership;
- audit loss and replay possibilities.

### Retention and purge ownership

The design must identify explicit responsibilities for:

- retention period;
- purge process;
- storage growth monitoring;
- backup and restore coverage;
- separation of application audit data from release evidence and governance records.

### Monitoring and alert ownership

This subphase must define who owns the following alerts:

- audit persistence failure;
- SQL availability;
- LDAP availability if appropriate;
- certificate/key expiry;
- license restricted-mode status.

### Readiness semantics

The team must determine whether `/health` remains pure liveness or whether a separate readiness endpoint is justified. This is a required owner/architect decision before changing available behavior semantics.

## Dependencies on earlier work

- Phase 6.1 establishes safe validation boundaries.
- Phase 6.2 defines the authorization and audit identity boundary.
- Phase 6.3 defines licensing risk and restricted-mode design.
- Phase 6.4 reconciles documentation and build status.

## External / professional / operational dependencies

- DBA input for SQL outage, retention, and purge design.
- operations ownership for alerting and response.
- architecture sign-off for readiness semantics.
- no vendor-specific monitoring platform is selected in this planning step.

## Narrow implementation scope

This plan governs design decisions only. It does not implement:

- durable outbox;
- message queue;
- Kafka;
- Redis;
- distributed tracing platform;
- monitoring vendor solution;
- infrastructure automation.

## Explicit non-goals

- no preselected operational platform;
- no forced outbox or queue architecture;
- no monitoring-vendor lock-in;
- no change to the current application behavior without owner approval.

## Expected files/components

Likely design inputs include:

- operational docs in [docs/Operations.md](../../Operations.md)
- [docs/AuditLogging.md](../../AuditLogging.md)
- [docs/Database.md](../../Database.md)
- [docs/Project_Status.md](../../Project_Status.md)
- related SQL audit and middleware code

## Required automated validation

This subphase is mainly design and decision validation, but any future implementation must include:

- audit failure and fallback behavior tests;
- latency boundary test coverage, if behavior remains synchronous;
- alert and monitoring requirement specification review;
- readiness-vs-liveness decision review.

## Acceptance criteria

The future implementation is acceptable only if:

- audit loss is explicitly and acceptably bounded;
- outage behavior is separated for application request path, logging, monitoring, and retention;
- ownership for retention and alerting is assigned;
- readiness semantics are approved before server deployment or release-readiness claims.

## Implementation Authorization Packet

### Baseline prerequisites

- Phase 6.1 verification is complete.
- Technical boundaries for authorization and licensing are known.
- The operational ownership model is identified.

### Exact implementation scope

- audit loss and latency policy;
- SQL outage behavior design;
- retention, purge, backup, and growth ownership plan;
- monitoring and alert requirements;
- liveness vs readiness decision.

### Explicit non-goals

- queue/outbox implementation;
- platform selection;
- monitoring vendor selection;
- distributed tracing design.

### Expected files/components

- operations design docs;
- audit and SQL outage decision notes;
- monitoring threshold and ownership matrix.

### Tests/validation

- design-review validation;
- operational acceptance scenarios;
- regression checks against the current audit mechanism.

### Acceptance criteria

- documented decision gates exist for each operational risk;
- no release or deployment claim is made while the decision remains open.

### Owner/architect decisions required first

- audit loss tolerance;
- latency budget;
- SQL outage handling policy;
- retention and purge ownership;
- readiness semantics;
- monitoring ownership and thresholds.

### External dependencies

- DBA input;
- operations owner approval;
- platform or vendor selection only if later required.

### Safety boundaries

- no product release without approved operational loss and retention policy;
- no outbox or queue architecture introduced without explicit decision;
- no monitoring vendor lock-in in this planning step.

### Recommended signed commit message

Plan remaining Phase 6 work

---

This subphase remains planning-only and does not authorize implementation.
