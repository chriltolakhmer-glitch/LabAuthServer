# Phase 3 — Advanced Authentication & Token Lifecycle

Status: FUTURE. Every topic below is OPTIONAL pending a requirements decision. No implementation, provider, package, schema or delivery date is approved. [Master roadmap](../README.md).

## Objective

Evaluate whether project needs justify extending the production-hardened baseline in this area.

## Candidate scope

- MFA.
- refresh tokens.
- token revocation.
- session/token lifecycle.
- stronger credential protection.
- step-up authentication.

## Entry criteria and dependencies

Phase 2 security and operational evidence is the baseline gate. Review outcomes of earlier relevant phases before selecting scope; a phase number is not a technical requirement to implement every preceding optional feature. Reprioritization needs explicit project ownership and approval.

## Open decisions

DECISION REQUIRED: Which users/actions require MFA or step-up? What client types and offline behavior exist? What are token lifetime, revocation latency, recovery and account-lockout expectations? Would established identity services better meet requirements than custom lifecycle storage?

For each selected candidate, record the problem, affected clients, measurable acceptance criteria, owner, operational cost and architectural impact before producing a detailed implementation plan.

## Architecture and risks

Adding persisted sessions, refresh tokens or revocation changes the current stateless access-token design. Any credential-protection change must retain LDAPS and approved Windows secret boundaries until replaced by an approved design.

Keep Domain <- Application <- Infrastructure <- Api unless a separately approved architecture change says otherwise. Existing AGENTS.md package and architecture approval rules continue to apply.

## Validation and rollback expectations

A future proposal must include replay/theft/revocation tests, recovery flows, credential privacy, compatibility with current JWT consumers and rollback without reactivating revoked credentials. Detailed tests, source areas, configuration and database changes belong in a later scoped design once requirements exist.

## Exit gate

An approved requirements/option decision may create a new detailed plan. Declining or deferring a candidate is a valid outcome. This README does not authorize implementation and does not block Phase 2 completion.
