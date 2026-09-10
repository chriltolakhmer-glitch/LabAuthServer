# Phase 5 — Enterprise Operations & Scalability

Status: FUTURE. Every topic below is OPTIONAL pending a requirements decision. No implementation, provider, package, schema or delivery date is approved. [Master roadmap](../README.md).

## Objective

Evaluate whether project needs justify extending the production-hardened baseline in this area.

## Candidate scope

- Distributed deployment.
- centralized key management.
- high availability.
- distributed rate limiting.
- centralized logging.
- observability.
- scaling.
- disaster recovery.

## Entry criteria and dependencies

Phase 2 security and operational evidence is the baseline gate. Review outcomes of earlier relevant phases before selecting scope; a phase number is not a technical requirement to implement every preceding optional feature. Reprioritization needs explicit project ownership and approval.

## Open decisions

DECISION REQUIRED: What availability, throughput, recovery point/time and regional requirements justify distribution? How will Windows DPAPI LocalMachine secrets and certificate access be provisioned per host? Who owns key coordination and rate-limit consistency?

For each selected candidate, record the problem, affected clients, measurable acceptance criteria, owner, operational cost and architectural impact before producing a detailed implementation plan.

## Architecture and risks

The current Login limiter is per process; extra instances change aggregate capacity. LocalMachine DPAPI is machine-bound, and local certificate stores require explicit provisioning. Build on Phase 2 monitoring and audit-loss decisions; do not assume a shared store or a particular logging backend.

Keep Domain <- Application <- Infrastructure <- Api unless a separately approved architecture change says otherwise. Existing AGENTS.md package and architecture approval rules continue to apply.

## Validation and rollback expectations

A future proposal must include failover/recovery drills, key-overlap consistency, per-host identity provisioning, distributed-limit semantics, load evidence and staged topology rollback without losing audit data. Detailed tests, source areas, configuration and database changes belong in a later scoped design once requirements exist.

## Exit gate

An approved requirements/option decision may create a new detailed plan. Declining or deferring a candidate is a valid outcome. This README does not authorize implementation and does not block Phase 2 completion.
