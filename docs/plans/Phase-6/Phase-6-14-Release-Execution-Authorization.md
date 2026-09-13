# Phase 6.14 — Release Execution Authorization

Status: FINAL EXECUTION GATE PREPARED; RELEASE EXECUTION NOT AUTHORIZED.

## 1. Authorization baseline

This document reviews the Phase 6.13 controlled preparation plan and creates the final authorization gate before any release execution. The Phase 6.12 approval and Phase 6.13 preparation approval authorize preparation only. They do not select the execution outcome in this document.

| Evidence | Recorded value | Boundary |
| --- | --- | --- |
| Phase 6.12 approval evidence | Signed commit `9e0e71b2a58ce56a5bc76e20546129f8b01f55ed`; ALOT; `2026-09-13`; `[x] APPROVE FIRST RELEASE` | Preparation authorization only |
| Phase 6.13 preparation commit | Signed commit `d9aea63d3478f4f5a0004ee3a94d06a1496979d4`; `Prepare Phase 6.13 controlled release plan` | Documentation and preparation plan only |
| Latest successful CI | `LabAuthServer CI` run `34732964419`, successful for `d9aea63d3478f4f5a0004ee3a94d06a1496979d4` | Exact-commit hosted validation; not execution authorization |
| Release build status | PASS; zero warnings and zero errors in Release CI job | Repository qualification only |
| Test status | CI Release deterministic-boundary tests and mandatory SQL persistence tests PASS; earlier baseline recorded 1,038 default tests and 2 explicit SQL tests passing; real LDAP acceptance not run | Controlled validation only |
| Signature status | Phase 6.12 and Phase 6.13 commits verified with valid ALOT SSH signatures | Source provenance evidence only |

### Known limitations

- Target environment ownership and IIS, SQL, AD/LDAPS, TLS, permissions, capacity, configuration, and operational handoff are not fully accepted.
- Operations, deployment, rollback, monitoring, incident, backup/recovery, certificate, and audit-retention ownership remain incomplete.
- Delivery, manifest, release-register, artifact-storage, and release-custody providers remain unresolved.
- Tag-signing procedure, artifact-signing policy, SBOM policy, and runtime metadata policy remain incomplete.
- Legal/business review, licensing terms, support commitments, and customer-facing restrictions remain incomplete.
- Real LDAP identity acceptance and complete cross-host reproducibility are not established by repository evidence.
- Audit retention, archival, purge automation, SQL Agent scheduling, and production monitoring are not implemented or operationally accepted.
- No release, tag, artifact publication, deployment, customer delivery, or license issuance has occurred.

Evidence references: [Phase 6.12 Final Release Decision Record](Phase-6-12-Final-Release-Decision-Record.md), [Phase 6.13 Controlled Release Preparation](Phase-6-13-Controlled-Release-Preparation.md), [Validation Status](../../Validation_Status.md), and [Project Status](../../Project_Status.md).

## 2. Final pre-release checklist

Every item must be evidenced before execution approval. An unchecked item blocks execution unless the final authority explicitly records a permitted exception in Section 4.

### Source/provenance

- [ ] Record the exact release commit SHA and confirm it is the approved source revision.
- [ ] Confirm the signed tag procedure is ready, including signer custody, tag contents, verification, rotation/revocation, and retained evidence. Do not create the tag during this review.
- [ ] Confirm source snapshot readiness, including repository state, exact SHA, dependency/runtime state, generated-file policy, and retained snapshot evidence.
- [ ] Confirm the source snapshot, release identifier, and provenance records agree with the manifest and release register.

### Artifacts

- [ ] Approve and record the complete artifact list, including deployment package, configuration templates, runtime/dependency files, issuer qualification output where applicable, and documentation.
- [ ] Generate and record cryptographic hashes for every artifact after the exact release build is approved.
- [ ] Confirm manifest readiness with source SHA, release identifier, artifact list, hashes, build identity, signing status, SBOM, runtime metadata, limitations, and approvals.
- [ ] Confirm private artifact-storage readiness, access control, backup, recovery, integrity, custody, and withdrawal procedures. Storage readiness does not authorize publication.

### Operations

- [ ] Name the deployment owner and record target-environment acceptance responsibility.
- [ ] Name the rollback owner and confirm the approved rollback/withdrawal procedure and backup evidence.
- [ ] Name the monitoring owner and confirm signals, thresholds, notification routes, and response targets.
- [ ] Record the incident contact, escalation route, backup contact, and support handoff.
- [ ] Confirm SQL, AD/LDAPS, IIS, TLS/certificate, capacity, backup/recovery, and audit-retention owners are accountable for the target.

### Licensing

- [ ] Confirm issuer readiness, including protected key/certificate custody, authorization, recovery, issuance records, and register controls.
- [ ] Confirm the license workflow is ready for a separately authorized execution operation and that legal/commercial terms are approved.
- [ ] Confirm no license issuance occurs before execution approval is recorded in Section 4 and all licensing prerequisites are met.
- [ ] Preserve the issuer boundary: inclusion of `LabAuthServer.LicenseIssuer` in Release qualification does not authorize issuance.

### Documentation

- [ ] Complete release notes with version, scope, compatibility, security boundaries, limitations, upgrade/rollback notes, and evidence references.
- [ ] Complete support documents with supported versions, scope, response/escalation route, exclusions, lifecycle, and contact ownership.
- [ ] Confirm known limitations and accepted exceptions are visible in the release record, release notes, support documents, and delivery material as applicable.
- [ ] Confirm deployment documentation is complete for prerequisites, configuration, IIS/SQL/AD/TLS acceptance, backup, rollback, validation, and handoff.

## 3. Remaining exceptions

The Phase 6.12 owner approval accepted these limitations for preparation planning. They remain execution exceptions and are not closed by this document. Each requires a named owner, impact, mitigation, evidence, and explicit disposition before execution.

| Exception ID | Impact | Mitigation | Owner | Accepted status |
| --- | --- | --- | --- | --- |
| EX-6.14-01 — unresolved providers | Missing delivery, manifest, register, artifact-storage, or custody providers can prevent controlled provenance, receipt, withdrawal, and recovery | Select approved private providers; record access, integrity, backup, recovery, receipt, withdrawal, and custody evidence | TBD | Accepted for preparation only; execution disposition pending |
| EX-6.14-02 — legal/business review | Unapproved legal, commercial, evaluation, licensing, retention, or support terms can make customer delivery unauthorized | Complete professional review and record approved terms, restrictions, support commitments, and dates | TBD | Accepted for preparation only; execution disposition pending |
| EX-6.14-03 — operational ownership | Unassigned deployment, rollback, monitoring, incident, backup/recovery, capacity, SQL, AD, IIS, certificate, or audit owners can leave failures unmanaged | Complete the responsibility matrix, runbooks, thresholds, escalation routes, and target handoff | TBD | Accepted for preparation only; execution disposition pending |
| EX-6.14-04 — environment-specific decisions | Unaccepted target permissions, TLS, AD/LDAPS, SQL, IIS, configuration, capacity, or handoff can invalidate release assumptions | Complete Phase 6.6 target acceptance with named component owners and evidence | TBD | Accepted for preparation only; execution disposition pending |
| EX-6.14-05 — signing, SBOM, and runtime metadata | Incomplete provenance, artifact-signing, SBOM, or runtime metadata policy can prevent verification and incident response | Approve procedures, key custody, formats, generation, storage, review, verification, and exception handling | TBD | Accepted for preparation only; execution disposition pending |
| EX-6.14-06 — cross-host reproducibility | Build or runtime differences across hosts can produce unverified artifacts or deployment behavior | Capture exact-SHA build evidence, SDK/dependency/runtime metadata, environment assumptions, and approved reproducibility limits | TBD | Accepted for preparation only; execution disposition pending |
| EX-6.14-07 — real LDAP acceptance | Repository tests do not prove live identity, group mapping, token issuance, or target-directory behavior | Complete controlled target acceptance or record an explicit execution exception with owner, mitigation, and expiry | TBD | Accepted for preparation only; execution disposition pending |

An exception marked accepted for preparation only is not permission to execute release activity. The final authority must either close it or explicitly accept it for execution in Section 4.

## 4. Final release execution authorization

Complete this gate only after the checklist and every exception have been reviewed by the approving authority. A blank or unchecked gate is not authorization.

```text
FINAL RELEASE EXECUTION DECISION:

[ ] APPROVE EXECUTION OF FIRST RELEASE

[ ] APPROVE WITH EXPLICIT EXCEPTIONS

[ ] DO NOT APPROVE

APPROVING AUTHORITY:

DATE:

EVIDENCE REFERENCES:

ACCEPTED EXCEPTIONS:

EXCEPTION OWNERS, MITIGATIONS, AND EXPIRY / DUE DATES:

FINAL DECISION NOTES:
```

An execution approval is valid only when the exact release commit, artifact inventory, hashes, manifest, provenance, operational owners, licensing readiness, documentation, and exception dispositions are attached to the decision record. `APPROVE WITH EXPLICIT EXCEPTIONS` requires every exception to be named and accepted by the appropriate authority with mitigation, owner, evidence, and expiry or due date.

## 5. Execution boundary

**This document records authorization status only.**

It does not itself:

- create a release;
- create tags;
- publish artifacts;
- deploy software;
- issue licenses;
- authorize customer delivery.

Phase 6.14 creates the final gate but does not select an execution outcome. No release operation, production change, customer delivery, or license issuance may proceed from this preparation document.

## 6. Abort criteria

Cancel release preparation and return to owner review before execution if any condition below occurs:

- the exact release commit, version identifier, source snapshot, or repository state is ambiguous or changes after evidence capture;
- the signed tag procedure, signing custody, artifact-signing decision, SBOM decision, or runtime metadata decision is incomplete or unverifiable;
- any artifact, hash, manifest, signature, SBOM, or metadata record is missing, inconsistent, altered, or not reproducible from the approved source;
- Release build or mandatory test evidence fails, is stale, is incomplete, or does not correspond to the approved exact commit;
- artifact storage, release register, delivery channel, or release custody lacks approved provider, access, integrity, backup, recovery, withdrawal, or receipt evidence;
- deployment, rollback, monitoring, incident, support, SQL, AD/LDAPS, IIS, certificate, capacity, audit-retention, or target-environment ownership is not named and accepted;
- legal/business approval, licensing terms, support commitment, or customer-facing restrictions are missing, changed, or expired;
- a protected key, certificate, issuer credential, deployment credential, or secret is unavailable, unauthorized, exposed, or subject to uncertain custody;
- any accepted exception lacks an owner, mitigation, evidence, expiry, or required re-approval, or exceeds its authorized scope;
- known limitations are omitted from the release, support, or delivery documentation;
- a requested action would create a release, tag, publication, deployment, license, customer delivery, or production change without explicit execution authorization; or
- any security, provenance, integrity, operational, legal, or authorization inconsistency is discovered.

An abort requires a new owner review and updated evidence. No operator may bypass an abort criterion by relying on the Phase 6.12 or Phase 6.13 preparation approvals.

## 7. Documentation-only validation

Validation for Phase 6.14 is documentation-only:

- confirm the Phase 6.12 approval, Phase 6.13 preparation commit, and latest successful CI evidence are recorded;
- confirm the final checklist covers source/provenance, artifacts, operations, licensing, and documentation;
- confirm every remaining exception has an ID, impact, mitigation, owner, and accepted status;
- confirm all three final execution outcomes, authority, date, evidence references, and accepted exceptions are present;
- confirm the execution boundary and abort criteria are explicit;
- confirm no release operation, tag, artifact publication, deployment, license issuance, customer delivery, or production change occurred.

A signed documentation commit and successful CI verify this document's repository integrity only. They do not authorize release execution.
