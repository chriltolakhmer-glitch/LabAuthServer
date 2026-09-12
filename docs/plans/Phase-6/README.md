# Phase 6 — Operational Assurance and First-Release Readiness

Current roadmap: [Phase 6 plan](Phase-6-Plan.md). The owner authorized this reprioritization and [Phase 6.1 — Safe Automated Validation Boundaries](Phase-6.1-Safe-Automated-Validation-Boundaries.md) on 2026-09-12. Phases 6.2–6.7 are planning entries only. Completion means **READY TO REQUEST SEPARATE RELEASE AUTHORIZATION**; no release is authorized.

## Superseded candidate roadmap — retained for traceability

The prior advanced-security candidate scope below remains optional/deferred. It is not the current Phase 6 implementation order and grants no implementation authorization.

Status: FUTURE. Every topic below is OPTIONAL pending a requirements decision. No implementation, provider, package, schema or delivery date is approved. [Master roadmap](../README.md).

## Objective

Evaluate whether project needs justify extending the production-hardened baseline in this area.

## Candidate scope

- HSM.
- advanced threat detection.
- stronger key lifecycle.
- policy engine.
- enterprise integrations.
- security automation.

## Entry criteria and dependencies

Phase 2 security and operational evidence is the baseline gate. Review outcomes of earlier relevant phases before selecting scope; a phase number is not a technical requirement to implement every preceding optional feature. Reprioritization needs explicit project ownership and approval.

## Open decisions

DECISION REQUIRED: Which threat model, assurance obligation or enterprise integration requirement justifies each extension? Who operates and recovers it? How will false positives, break-glass access and cost be handled?

For each selected candidate, record the problem, affected clients, measurable acceptance criteria, owner, operational cost and architectural impact before producing a detailed implementation plan.

## Architecture and risks

HSM/key lifecycle and policy engines can change key-provider or authorization semantics. Preserve current least privilege and fail-closed behavior until an approved replacement is validated. Automation must have bounded authority and explicit recovery ownership.

Keep Domain <- Application <- Infrastructure <- Api unless a separately approved architecture change says otherwise. Existing AGENTS.md package and architecture approval rules continue to apply.

## Validation and rollback expectations

A future proposal must include key-loss/rotation recovery, policy regression, false-positive handling, integration trust boundaries and rollback that does not silently broaden access. Detailed tests, source areas, configuration and database changes belong in a later scoped design once requirements exist.

## Exit gate

An approved requirements/option decision may create a new detailed plan. Declining or deferring a candidate is a valid outcome. This README does not authorize implementation and does not block Phase 2 completion.
