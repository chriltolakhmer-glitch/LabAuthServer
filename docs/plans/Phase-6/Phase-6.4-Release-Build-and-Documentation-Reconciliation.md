# Phase 6.4 — Release-Build and Documentation Reconciliation

Status: IMPLEMENTED AND LOCALLY VALIDATED; RELEASE QUALIFICATION COMPLETE. NO RELEASE AUTHORIZED.

## Purpose

Verify that the Release build and the current state documentation accurately match the actual repository. This subphase handles two separate but related gaps: the Release build coverage of the standalone issuer component and the reconciliation of stale current-state documentation against code and the actual phase records.

## Confirmed current findings

### Release build coverage

[LabAuthServer.slnx](../../../LabAuthServer.slnx) explicitly includes the API, application, domain, infrastructure, standalone issuer, and both test projects. The issuer project at [tools/LabAuthServer.LicenseIssuer](../../../tools/LabAuthServer.LicenseIssuer) is therefore covered by the solution Release restore/build path and is not an unqualified side project.

The Phase 4 implementation history describes the issuer as a separate project used by tests and sign-off workflows. It remains a build-qualified utility component, while the API publish profile remains API-only. Solution qualification does not create or publish a release artifact.

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

## Implemented qualification

The supported repository qualification commands are:

```powershell
dotnet restore
dotnet build LabAuthServer.slnx -c Release --no-restore
dotnet test LabAuthServer.slnx --no-build --no-restore
```

The solution Release build covers seven projects: four production application projects, `LabAuthServer.LicenseIssuer`, and two test projects. The test command covers the two test projects; environment-dependent SQL and LDAP cases retain their explicit opt-in boundaries documented in [Testing](../../Testing.md). The API file-system publish profile is a separate local packaging validation path and is not run by CI or this phase.

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

## Decision gates resolved in this implementation

- The issuer is required as an explicit Release-qualified solution component.
- Current-state sections in `Project_Status.md`, `Validation_Status.md`, and `Testing.md` are authoritative for repository qualification; dated and archived records remain historical evidence.
- Current-state sections identify implementation, test, environment-dependent, deferred, and unauthorized release boundaries explicitly.

## External/professional dependencies

- release/build owner for component inclusion and Release qualification;
- operations or product owner for documentation claims about distribution, acceptance, or environment readiness;
- legal/commercial review only if the docs should state a new or modified authorization or support claim.

## Narrow implementation scope

This implementation only did the following:

- correct the Release project graph so the issuer is deliberately included;
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

Implementation included:

- solution/project graph validation for Release build coverage;
- build verification that demonstrates the Release solution status for the issuer;
- documentation review against code and evidence boundaries;
- no hidden changes to runtime or environment configuration.

## Acceptance criteria

This implementation is acceptable because:

- the Release build graph clearly names the issuer component or explains why it is intentionally excluded;
- the issue of current docs vs historical docs is reconciled without losing evidence;
- a developer can determine from the docs what exists, what does not, and what is deferred;
- commercial licensing claims remain limited to actual enforcement and implementation status.

## Implementation Authorization Packet

### Baseline prerequisites

- Phase 6.1 is verified and clean.
- The actual Release build coverage and current-state doc contradictions have been identified.
- The issuer component is explicitly included in the Release-qualified solution graph.

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

- Resolved by approved P6-I1 and this implementation: the issuer is part of the Release-qualified solution graph.
- Resolved by this reconciliation: current-state conclusions are documented separately from historical records.

### External dependencies

- release/build owner sign-off;
- documentation owner/maintainer review;
- professional/legal review only if a doc claim is materially changed for external release statements.

### Safety boundaries

- no implementation or release behavior change beyond solution/project inclusion or documentation remediation;
- no customer delivery or production signing action.

### Recommended signed commit message

Implement Phase 6.4 release qualification

---

This subphase does not authorize a release, deployment, customer delivery, production signing, tag creation, artifact publication, or implementation of Phases 6.5–6.7.
