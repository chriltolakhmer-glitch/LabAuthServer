# Phase 4 — Identity Federation & Standards

Status: FUTURE. Every topic below is OPTIONAL pending a requirements decision. No implementation, provider, package, schema or delivery date is approved. [Master roadmap](../README.md).

## Objective

Evaluate whether project needs justify extending the production-hardened baseline in this area.

## Candidate scope

- OAuth 2.x.
- OpenID Connect.
- federation.
- external identity providers.
- standards-compliant authorization flows.

## Entry criteria and dependencies

Phase 2 security and operational evidence is the baseline gate. Review outcomes of earlier relevant phases before selecting scope; a phase number is not a technical requirement to implement every preceding optional feature. Reprioritization needs explicit project ownership and approval.

## Open decisions

DECISION REQUIRED: Which clients, trust domains and federation partners exist? Is LabAuthServer an issuer, resource server, broker or a component to retire? Which flows and assurance requirements apply? Select no provider until requirements and integration ownership are known.

For each selected candidate, record the problem, affected clients, measurable acceptance criteria, owner, operational cost and architectural impact before producing a detailed implementation plan.

## Architecture and risks

Current login plus JWT issuance is not a claim of OAuth/OIDC compliance. Federation changes trust and authentication architecture; compare managed/established implementations before proposing custom protocol work.

Keep Domain <- Application <- Infrastructure <- Api unless a separately approved architecture change says otherwise. Existing AGENTS.md package and architecture approval rules continue to apply.

## Validation and rollback expectations

A future proposal must define protocol conformance/security validation, redirect/client trust boundaries, claim/role translation, issuer migration and rollback for existing consumers. Detailed tests, source areas, configuration and database changes belong in a later scoped design once requirements exist.

## Exit gate

An approved requirements/option decision may create a new detailed plan. Declining or deferring a candidate is a valid outcome. This README does not authorize implementation and does not block Phase 2 completion.
