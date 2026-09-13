> Historical multi-owner workflow, superseded 2026-09-13 by the [solo developer release checklist](../../plans/Phase-6/Release-Checklist.md). Original decisions, pending items, and results below are retained as history, not future release gates.

# Phase Release Evidence Ownership Assignment Record

Status: EVIDENCE COLLECTION IN PROGRESS; RELEASE PREFLIGHT NOT READY.

This record converts the 18 pending items from the [Phase Release Preflight Completion Checklist](Phase-Release-Preflight-Completion-Checklist.md) and [Phase Release Evidence Collection Packet](Phase-Release-Evidence-Collection-Packet.md) into tracked assignments. No item is marked `COMPLETE`. The existing execution approval does not waive missing evidence, ownership, signoff, or environment acceptance.

## 1. Evidence collection ownership matrix

| ID | Pending evidence item | Proposed accountable owner | Evidence collection action | Acceptance criteria | Dependency / blocker | Status |
| --- | --- | --- | --- | --- | --- | --- |
| EO-01 | Release tag signing procedure | ALOT / release approval authority; procedure owner TBD | Draft the approved `vMAJOR.MINOR.PATCH` procedure with tag contents, signing tool, verification, immutability, rotation/revocation, failure, and recovery steps; perform a non-publishing dry run | Procedure is versioned, reviewed, verification output is retained, and signer/custody responsibilities are explicit | Release-tag signing remains unresolved/not implemented in Release Governance | PENDING OWNER |
| EO-02 | Signing custody | ALOT / release operator; custody owner TBD | Record public key fingerprint, protected storage, access, recovery, rotation/revocation, separation of duties, and signer attestation without exposing private material | Custody record is complete, access is approved, recovery is evidenced, and security/approval signoff is present | EO-01 procedure and protected custody owner are required | PENDING OWNER |
| EO-03 | Provenance evidence | ALOT / release operator | Assemble exact release SHA, identifier, source snapshot, repository state, SDK/build metadata, CI reference, artifact inventory, hashes, manifest, SBOM, runtime metadata, signatures, and reproducibility limits | One immutable provenance package binds every artifact and record to the exact source SHA and has review signoff | EO-01, EO-04 through EO-07, and cross-host reproducibility evidence | PENDING OWNER |
| EO-04 | Artifact inventory | ALOT / release operator | Produce a reviewed inventory of filenames, purposes, versions, sizes, source SHA, build configuration, runtime/dependency contents, templates, documentation, and issuer qualification output | Every proposed artifact has one manifest row and the final contents are approved | Release identifier and approved build output are not finalized | PENDING OWNER |
| EO-05 | Artifact hashes | ALOT / release operator | Generate SHA-256 and byte-size records from final artifacts using a recorded tool/version and bind them to source/build identity | Hash file is machine-generated, independently compared with the manifest, and signed/reviewed | EO-04 final inventory and release build artifacts | PENDING OWNER |
| EO-06 | Release manifest | ALOT / release operator | Generate Release Manifest V1 or approved equivalent with identifier, SHA, CI, timestamp, inventory, hashes, operator, SBOM, runtime metadata, signatures, limitations, and approvals | Schema validation, source/artifact binding, review, and retained approval evidence all pass | EO-03 through EO-05 and unresolved signing/SBOM/metadata policy | PENDING OWNER |
| EO-07 | Artifact/manifest storage location | ALOT / distribution operator; provider owner TBD | Select exact private provider/location and document access, integrity, retention, backup, recovery, audit, withdrawal, and custody controls | Provider is approved, storage controls are tested/evidenced, and register reference is recorded | Exact provider/location and retention/legal decisions are unresolved | PENDING OWNER |
| EO-08 | Release operator | ALOT | Record operator role, access, custody duties, backup operator, separation-of-duties decision, and acceptance | Named operator signs responsibility and access review; missing independent reviewer is explicitly recorded | Backup operator and custody evidence are not assigned | PENDING OWNER |
| EO-09 | Distribution operator | ALOT initially; provider/operator owner TBD | Select private channel/provider, document recipient verification, receipt, withdrawal, access, audit, and delivery records without delivering | Provider and operator are approved; a controlled receipt/withdrawal procedure is signed | Exact provider/channel and customer delivery terms are unresolved | PENDING OWNER |
| EO-10 | Rollback owner | Operations owner TBD | Assign primary/backup rollback owner and produce target-specific triggers, recovery point, backup, restore, withdrawal, communications, and rehearsal plan | Owner signs runbook; backup/recovery evidence and authorized rehearsal or exception are recorded | Target environment, backup/recovery, and operations ownership are incomplete | PENDING OWNER |
| EO-11 | Incident owner | ALOT security response owner initially; backup TBD | Record primary/backup contacts, severity route, escalation, response target, security/commercial handoff, and communications | Responsibility matrix and contact verification are signed and support handoff is accepted | Operations/support owner and backup contact are unresolved | PENDING OWNER |
| EO-12 | Environment approval | Target-environment owner TBD; SQL, AD, IIS, certificate owners required | Complete target acceptance for IIS, SQL, AD/LDAPS, TLS/certificates, permissions, DPAPI/key access, configuration, capacity, logging, and handoff | Redacted target record has current command/output evidence, timestamps, component-owner signatures, and no secrets | Current target equivalence and real AD/certificate/DPAPI evidence are not established | PENDING OWNER |
| EO-13 | Deployment owner | Deployment owner TBD; target authority required | Assign deployment operator and target authority; link approved runbook, prerequisites, change window, backup, validation, and rollback contacts | Responsibility record and target acceptance are signed by deployment/operations authority | EO-12 environment approval and deployment procedure acceptance | PENDING OWNER |
| EO-14 | Rollback plan | Rollback owner TBD; operations and target owners required | Produce target-specific rollback/withdrawal runbook with triggers, decision authority, recovery point, backup, restore, communications, and rehearsal | Runbook, backup verification, trigger matrix, owner signoff, and rehearsal/approved exception are retained | EO-10 ownership and EO-12 target acceptance | PENDING OWNER |
| EO-15 | Validation plan | Deployment and operations owners TBD; security/QA reviewer required | Define pre/post deployment checks for package/hash, HTTPS/TLS, health, protected endpoint, AD identity/group mapping, SQL audit, headers, and rollback | Versioned plan has commands, expected results, evidence locations, pass/fail fields, and target-owner approval | EO-12 target acceptance and EO-13 deployment ownership | PENDING OWNER |
| EO-16 | Support approval | ALOT / commercial authority; support owner TBD | Approve supported versions, scope, response targets, escalation, exclusions, lifecycle, contacts, and customer-facing wording | Support policy/decision has named owner, date, conditions, commercial signoff, and legal acknowledgement | Professional commercial/legal review and support owner are incomplete | PENDING OWNER |
| EO-17 | Customer delivery approval | ALOT / distribution operator; provider and terms owner TBD | Select channel/provider and recipient class; approve identity verification, restrictions, receipt, withdrawal, data handling, and customer notice | Signed delivery authorization and provider record exist; no delivery is performed during collection | Exact provider/channel and customer delivery terms are unresolved | PENDING OWNER |
| EO-18 | Commercial/legal approval | ALOT / commercial authority; professional reviewers TBD | Obtain review of copyright, proprietary/evaluation terms, licensing claims, retention, warranty/disclaimer, support, delivery, and restrictions | Versioned legal/business memo or decision record has named reviewers, conditions, dates, and approval | Professional legal/business review remains pending | PENDING OWNER |

No status may advance to `IN PROGRESS`, `READY FOR REVIEW`, or `COMPLETE` without a named owner accepting the assignment. No status may become `COMPLETE` without the required evidence and signoff stored at the approved location.

## 2. Dependency tracking

| Dependency | Blocks | Required resolution |
| --- | --- | --- |
| EO-01 release-tag procedure and EO-02 signing custody | EO-03 provenance, tag creation readiness, execution retry | Approve procedure, custody, verification, and recovery evidence |
| Release identifier and exact source snapshot | EO-03, EO-04, EO-06, all artifact/hash records | Name the release candidate and freeze the source only after preflight retry approval |
| Release build artifacts | EO-04, EO-05, EO-06 | Run the approved Release build only after owner evidence and retry gate are approved |
| EO-04 artifact inventory | EO-05 hashes and EO-06 manifest | Approve final artifact contents and inventory |
| EO-05 hashes and EO-06 manifest | EO-03 provenance and storage | Generate, compare, review, and bind to exact SHA |
| EO-07 storage/provider selection | EO-06 manifest retention, EO-09 distribution, release register | Approve private provider, access, integrity, backup, recovery, retention, and custody |
| EO-08 through EO-11 custody/operations ownership | Execution retry and release control | Name owners, backups, signoffs, and escalation routes |
| EO-12 environment approval | EO-13 through EO-15 deployment/rollback/validation | Complete target acceptance and component-owner signatures |
| EO-16 through EO-18 legal/business decisions | Customer delivery, licensing, support, and execution retry | Complete professional review and approve customer-facing terms |
| Clean repository and approved environment | Retry gate | Confirm all evidence is current, stored, and reconciled before retry |

## 3. Review workflow

1. Proposed owner accepts the assignment and changes the item from `PENDING OWNER` to `IN PROGRESS` only after the owner is named in the record.
2. Owner collects evidence in the required format and stores it at the approved location without secrets or private keys in the repository.
3. Owner records exact SHA, version, timestamps, evidence identifiers, conditions, and expiry/review dates.
4. Owner submits the item as `READY FOR REVIEW` with the evidence reference and required signoff.
5. Required reviewer verifies completeness, authenticity, source binding, scope, and storage access; failed review returns the item to `IN PROGRESS` or `PENDING OWNER`.
6. Only the accountable approval authority may mark an item `COMPLETE`, and only after all acceptance criteria and signoffs are present.
7. The release owner reconciles all 18 items and updates the preflight retry gate only after every mandatory item is complete, the repository is clean, and the approved environment is confirmed.

Current state: all 18 items remain `PENDING OWNER`; no review or completion is recorded by this document.

## 4. Escalation path

- Item owner escalates missing access, provider selection, or evidence format to ALOT / release approval authority.
- Signing or provenance issues escalate to ALOT and the security/provenance reviewer; no unsigned or unverifiable substitute is permitted.
- Artifact, storage, distribution, or register issues escalate to ALOT / distribution operator and the custody/provider owner.
- Environment, deployment, rollback, monitoring, or incident issues escalate to the target-environment owner and operations authority; no target acceptance may be inferred from repository tests.
- Legal, commercial, support, customer delivery, or licensing issues escalate to ALOT / commercial authority and professional reviewers.
- Any failed, stale, conflicting, expired, or unverifiable evidence blocks progression and returns the item to `PENDING OWNER` or `IN PROGRESS`.
- No escalation path authorizes a release tag, release creation, artifact publication, deployment, license issuance, or customer delivery.

## 5. Retry readiness criteria

The preflight may be considered ready to retry only when all of the following are true:

- every EO-6.01 through EO-6.18 item has a named accountable owner;
- every required evidence record is complete, current, traceable to the exact release candidate, and stored at the approved location;
- every required authority and reviewer has signed or recorded an explicitly approved exception with owner, mitigation, evidence, and expiry;
- release-tag signing procedure and custody are approved;
- artifact inventory, hashes, manifest, storage, and provenance are approved;
- release, distribution, rollback, incident, deployment, monitoring, support, and target-environment owners are accepted;
- target IIS, SQL, AD/LDAPS, TLS, permissions, capacity, configuration, logging, deployment, validation, and rollback evidence is approved;
- legal/business, support, customer delivery, commercial, and licensing terms are approved;
- repository status is clean and the approved environment is confirmed current;
- the owner completes the retry gate in the preflight checklist.

The current status is not ready. No item is marked complete and no retry option is selected.

## RELEASE PREFLIGHT STATUS:

**NOT READY - EVIDENCE COLLECTION IN PROGRESS**

This record is documentation-only. It does not modify source code, create tags or releases, publish artifacts, deploy software, issue licenses, authorize customer delivery, or modify production environments.
