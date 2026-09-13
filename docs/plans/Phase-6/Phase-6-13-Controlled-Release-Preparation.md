# Phase 6.13 — Controlled Release Preparation Plan

Status: PREPARATION PLAN AUTHORIZED; RELEASE EXECUTION NOT AUTHORIZED.

## 1. Authorization evidence

Phase 6.12 records the owner's explicit instruction, **"Approve all"**, and the selected outcome `[x] APPROVE FIRST RELEASE`. That approval authorizes preparation for first release execution planning only.

| Evidence | Recorded value |
| --- | --- |
| Decision record | [Phase 6.12 Final Release Decision Record](Phase-6-12-Final-Release-Decision-Record.md) |
| Approval commit | `9e0e71b2a58ce56a5bc76e20546129f8b01f55ed` — `Record Phase 6.12 first-release approval` |
| Commit signature | Valid SSH signature for ALOT / `chriltola.khmer@gmail.com`; key `SHA256:GwFTsR04jnXRbLiRnaEhjiGEgnx36rtuWmxFubZHXyo` |
| Approving authority | `ALOT` |
| Approval date | `2026-09-13` |
| Exact-commit CI | `LabAuthServer CI` run `34732750519`, successful for commit `9e0e71b2a58ce56a5bc76e20546129f8b01f55ed` |
| CI evidence | Release build passed; deterministic boundary tests passed; disposable SQL audit test database was provisioned; mandatory SQL persistence tests passed |
| Repository validation baseline | Release build: zero warnings/errors; default tests: 1,038 passed; explicit SQL validation: 2 passed; real LDAP acceptance: not run |

### Accepted exceptions carried from Phase 6.12

The approval carries these limitations as accepted exceptions for preparation planning only:

- environment ownership and provider completion;
- operations ownership completion;
- release custody provider selection;
- legal/business review completion;
- tag signing procedure completion;
- artifact signing, SBOM, and runtime metadata decisions; and
- cross-host reproducibility limitations.

These exceptions do not authorize release execution, artifact publication, deployment, customer delivery, or license issuance. Each must be resolved or explicitly re-confirmed at the release execution gate with named owners, evidence, mitigation, and expiry or due date.

## 2. Release preparation checklist

This checklist records preparation work and required evidence. Checking an item does not execute the associated release action.

### Versioning

- [ ] Define the release identifier using the approved project format: product name, semantic version, and any approved build/revision qualifier.
- [ ] Record the version owner and obtain confirmation that the identifier is unique in the release register.
- [ ] Name the release notes owner and confirm review responsibility.
- [ ] Confirm the release identifier is not represented as a published release until the execution gate is approved.

### Source/provenance

- [ ] Record the approved source commit reference: `9e0e71b2a58ce56a5bc76e20546129f8b01f55ed`, or a separately approved superseding SHA.
- [ ] Complete the approved signed-tag procedure, including signer custody, tag message, verification, rotation/revocation handling, and retained evidence. Do not create the tag during preparation.
- [ ] Define the source snapshot procedure, including exact SHA, repository state, submodule/dependency state if applicable, generated-file policy, and retained snapshot evidence.
- [ ] Confirm the source snapshot and release identifier are recorded in the release manifest and register before any execution action.

### Build/artifacts

- [ ] Use the approved Release build command after the exact source SHA is confirmed:

  ```powershell
  dotnet restore .\LabAuthServer.slnx
  dotnet build .\LabAuthServer.slnx -c Release --no-restore --nologo
  ```

- [ ] Record the build SDK, configuration, command output, warnings/errors, and exact source SHA.
- [ ] Prepare the artifact inventory, including the API deployment package, required configuration templates, `LabAuthServer.LicenseIssuer` qualification output where applicable, dependency/runtime files, and documentation package. Do not publish artifacts during preparation.
- [ ] Define artifact hash generation using an approved cryptographic hash, record file names and sizes, and retain the hash output with the manifest.
- [ ] Prepare the manifest with release identifier, source SHA, build identity, artifact inventory, hashes, SBOM reference, runtime metadata, signing status, limitations, and approvals.
- [ ] Prepare the private artifact-storage location and access/custody record. Do not upload or publish artifacts during preparation.

### Documentation

- [ ] Prepare release notes with version, scope, compatibility, security boundaries, known limitations, upgrade/rollback notes, and evidence references.
- [ ] Carry forward known limitations: target environment acceptance, SQL/AD/IIS ownership, audit retention and monitoring, real LDAP acceptance, delivery/custody decisions, signing/SBOM/metadata policies, legal/business review, support commitments, and cross-host reproducibility.
- [ ] Prepare support documentation with supported versions, support scope, response/escalation route, exclusions, and lifecycle terms.
- [ ] Prepare deployment documentation covering approved prerequisites, configuration ownership, IIS/SQL/AD/TLS acceptance, backup, rollback, validation, and handoff. Do not deploy during preparation.

### Operations

- [ ] Name the deployment owner and record target-environment acceptance responsibility.
- [ ] Name the rollback owner and confirm the tested rollback/withdrawal procedure.
- [ ] Name the monitoring owner and record signals, thresholds, notification routes, and response targets.
- [ ] Record the incident contact, escalation route, backup contact, and support handoff.
- [ ] Confirm SQL, AD/LDAP, IIS, certificate, backup/recovery, capacity, and audit-retention owners are recorded before execution authorization.

### Licensing

- [ ] Confirm the license workflow is ready for a separately authorized release operation, including protected issuer custody, authorization, evidence, recovery, and register controls.
- [ ] Preserve the issuer boundary: `LabAuthServer.LicenseIssuer` is included in Release qualification, but preparation does not issue or distribute a customer license.
- [ ] Confirm no license issuance occurs until release execution approval is separately recorded.

## 3. Remaining exceptions

The following exceptions remain open for preparation and must be resolved or explicitly accepted again before release execution:

| Exception area | Current state | Required closure evidence | Owner |
| --- | --- | --- | --- |
| Unresolved providers | Delivery channel, manifest storage, release register, artifact storage, and custody providers are not fully selected | Named providers, access/custody records, integrity controls, receipt, backup, recovery, and withdrawal procedures | TBD |
| Legal/business review | Legal, commercial, evaluation, copyright, retention, licensing, and support review is incomplete | Named reviewers, approved terms, conditions, restrictions, support commitment, and recorded dates | TBD |
| Operational ownership | Operations, monitoring, incident, backup/recovery, capacity, audit retention, SQL, AD, IIS, and certificate owners are incomplete | Responsibility matrix, thresholds, routes, runbooks, acceptance authority, and escalation evidence | TBD |
| Environment-specific decisions | Target environment, permissions, configuration, TLS/certificates, AD/LDAPS, SQL, IIS, capacity, and handoff are not fully accepted | Phase 6.6 target acceptance record and named environment/component owners | TBD |
| Signing/SBOM decisions | Tag signing procedure, artifact signing policy, SBOM policy, and runtime metadata policy remain incomplete | Approved procedures, key custody, verification, formats, generation, storage, review, and exception records | TBD |
| Cross-host reproducibility | Current evidence does not independently reproduce every build and runtime condition across target hosts | Exact-SHA build evidence, environment metadata, dependency/runtime inventory, and approved reproducibility limitation | TBD |

## 4. Release execution gate

Preparation must stop at this gate until the approving authority reviews the completed preparation evidence and explicitly records an execution decision.

```text
RELEASE EXECUTION AUTHORIZATION:

[ ] APPROVED TO EXECUTE RELEASE

[ ] NOT APPROVED

AUTHORITY:

DATE:

EVIDENCE REFERENCES:

ACCEPTED EXCEPTIONS:

DECISION NOTES:
```

**Phase 6.13 authorizes preparation only.** It does not itself:

- create a release;
- create tags;
- publish artifacts;
- deploy software;
- issue licenses;
- authorize customer delivery.

A completed preparation checklist, successful CI run, or Phase 6.12 approval does not select `[ ] APPROVED TO EXECUTE RELEASE`. Execution requires this separate gate, exact source/artifact evidence, resolved or explicitly accepted exceptions, and the required custody and operational approvals.

## 5. Abort conditions

Stop release preparation and do not proceed to execution if any of the following occurs:

- the source SHA, version identifier, release manifest, or artifact inventory is ambiguous or inconsistent;
- the exact approved source cannot be reproduced, built, or tested under the required Release configuration;
- Release build or mandatory test evidence fails, is incomplete, or does not correspond to the approved SHA;
- a tag, artifact, manifest, SBOM, runtime metadata record, or signature cannot be verified against the approved source and release identifier;
- signing keys, issuer credentials, certificates, deployment credentials, or other protected material are unavailable, unauthorized, exposed, or subject to custody uncertainty;
- artifact signing, SBOM, runtime metadata, or provenance decisions remain unapproved at execution time;
- a required delivery, storage, register, rollback, or custody provider is not approved or cannot provide integrity and access evidence;
- target environment ownership, SQL/AD/IIS/TLS acceptance, permissions, capacity, monitoring, backup/recovery, or operational handoff is incomplete;
- legal/business approval, licensing terms, support commitments, or customer-facing restrictions are incomplete or changed;
- an accepted exception has expired, lost its owner or mitigation, exceeded its stated scope, or lacks required re-approval;
- release notes, deployment instructions, support documentation, or known limitations are materially incomplete;
- any requested action would create a release, tag, artifact publication, deployment, customer delivery, license issuance, or production change without explicit execution authorization; or
- any evidence indicates a security, integrity, provenance, or authorization inconsistency.

An abort returns the work to the owner decision process. No operator may bypass an abort condition by relying on the Phase 6.12 preparation approval.

## 6. Documentation-only validation

Validation for Phase 6.13 is limited to documentation scope:

- confirm that authorization evidence references the committed Phase 6.12 approval and successful exact-commit CI;
- confirm that all requested preparation checklist areas are present;
- confirm that unresolved providers, legal/business review, operations, environment decisions, signing, SBOM, metadata, and reproducibility are tracked;
- confirm that the execution authorization remains unchecked;
- confirm that all execution boundaries and abort conditions are explicit;
- confirm that no release operation, artifact publication, deployment, licensing operation, or production-environment change occurred.

This validation does not create a release, create tags, publish artifacts, deploy software, issue licenses, authorize customer delivery, or modify production environments.
