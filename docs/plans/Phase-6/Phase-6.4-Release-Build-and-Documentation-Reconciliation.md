# Phase 6.4 — Release-Build and Documentation Reconciliation

Status: PLANNED — NOT AUTHORIZED FOR IMPLEMENTATION.

## Purpose

Verify that the Release build and the current state documentation accurately match the actual repository. This subphase handles two separate but related gaps: the Release build coverage of the standalone issuer component and the reconciliation of stale current-state documentation against code and the actual phase records.

## Confirmed current findings

### Release build coverage

[LabAuthServer.slnx](../../../LabAuthServer.slnx) currently includes the application, domain, infrastructure, and test projects, but it does not explicitly include the standalone issuer project at [tools/LabAuthServer.LicenseIssuer](../../../tools/LabAuthServer.LicenseIssuer).

The Phase 4 implementation history describes the issuer as a separate project used by tests and sign-off workflows. That means the Release build may not currently qualify the issuer as an explicit Release component even if the code is present in the repository and used as a supported utility.

### Documentation drift

The current docs contain both authoritative current-state statements and historical records. The plan must preserve archival evidence while correcting contradictions in current-state documentation and any operational statements that imply a current live acceptance not proven by repository evidence.

Priority documentation candidates include:

- [docs/Project_Status.md](../../Project_Status.md)
- [docs/Validation_Status.md](../../Validation_Status.md)
- [docs/Testing.md](../../Testing.md)
- [docs/Architecture.md](../../Architecture.md)
- [docs/Authorization.md](../../Authorization.md)
- [docs/Licensing.md](../../Licensing.md)
- [docs/Deployment.md](../../Deployment.md)
- [docs/Operations.md](../../Operations.md)
- [AGENTS.md](../../../AGENTS.md)
- [.github/copilot-instructions.md](../../../.github/copilot-instructions.md)

## Problem to solve

The project must produce an accurate, current-state perspective that clearly separates:

- what exists;
- what does not exist;
- what is tested;
- what is designed but not implemented;
- what is deferred;
- what is environment-dependent;
- what the licensing subsystem actually enforces.

## Dependencies on earlier work

- Phase 6.1 establishes the safe validation boundary.
- Phase 6.2 and 6.3 provide the corrected technical semantics for authorization, audit, and licensing.
- The final documentation reconciliation should follow, not precede, those technical clarifications.

## Decision gates required before implementation

- Is the issuer library required as an explicit Release-valid component for the distribution workflow?
- Which current-state docs are authoritative for the released baseline, and which remain historical evidence only?
- What constitutes an acceptable current-state annotation when a file contains both historical and current material?

## External/professional dependencies

- release/build owner for component inclusion and Release qualification;
- operations or product owner for documentation claims about distribution, acceptance, or environment readiness;
- legal/commercial review only if the docs should state a new or modified authorization or support claim.

## Narrow implementation scope

The future implementation may only do the following:

- correct the Release project graph so the issuer is deliberately included or intentionally excluded with the reason recorded;
- reconcile current-state documentation to match the actual code and repository evidence;
- preserve historical records under docs/archive and earlier phase docs as evidence rather than rewriting them.

## Explicit non-goals

- release authorization;
- production signing-key creation or issuance;
- live environment deployment;
- customer distribution or external delivery workflows;
- rewriting historical phase records.

## Expected files/components

Likely review targets include:

- [LabAuthServer.slnx](../../../LabAuthServer.slnx)
- [tools/LabAuthServer.LicenseIssuer](../../../tools/LabAuthServer.LicenseIssuer)
- [docs/Project_Status.md](../../Project_Status.md)
- [docs/Validation_Status.md](../../Validation_Status.md)
- [docs/Testing.md](../../Testing.md)
- [docs/Architecture.md](../../Architecture.md)
- [docs/Authorization.md](../../Authorization.md)
- [docs/Licensing.md](../../Licensing.md)
- [docs/Deployment.md](../../Deployment.md)
- [docs/Operations.md](../../Operations.md)
- [AGENTS.md](../../../AGENTS.md)
- [.github/copilot-instructions.md](../../../.github/copilot-instructions.md)

## Required automated validation

Implementation must include:

- solution/project graph validation for Release build coverage;
- build verification that demonstrates the Release solution status for the issuer if it is included;
- documentation review against code and evidence boundaries;
- no hidden changes to runtime or environment configuration.

## Acceptance criteria

The future implementation is acceptable only if:

- the Release build graph clearly names the issuer component or explains why it is intentionally excluded;
- the issue of current docs vs historical docs is reconciled without losing evidence;
- a developer can determine from the docs what exists, what does not, and what is deferred;
- commercial licensing claims remain limited to actual enforcement and implementation status.

## Implementation Authorization Packet

### Baseline prerequisites

- Phase 6.1 is verified and clean.
- The actual Release build coverage and current-state doc contradictions have been identified.
- The issuer component status is clearly categorized as either included or intentionally excluded with rationale.

### Exact implementation scope

- Release solution/project reconciliation;
- documentation current-state correction and evidence preservation;
- owner-approved documentation claims only.

### Explicit non-goals

- release authorization;
- production signing or issuance;
- deployment or environment execution;
- rewriting historical evidence.

### Expected files/components

- solution/project list;
- issuer component project and Release build behavior;
- current-state docs and any needed navigation links.

### Tests/validation

- Release build verification;
- documentation consistency review;
- no runtime or database behavior change.

### Acceptance criteria

- the Release build graph is accurate;
- the docs accurately separate current state, deferred work, and historical records;
- no contradictory claim of live deployment or release readiness remains in the current docs.

### Owner/architect decisions required first

- whether the issuer is part of the Release-qualified solution graph;
- whether the documentation should call out a current-state-only conclusion vs a historical record.

### External dependencies

- release/build owner sign-off;
- documentation owner/maintainer review;
- professional/legal review only if a doc claim is materially changed for external release statements.

### Safety boundaries

- no implementation or release behavior change beyond solution/project inclusion or documentation remediation;
- no customer delivery or production signing action.

### Recommended signed commit message

Plan remaining Phase 6 work

---

This subphase remains planning-only and does not authorize implementation.
