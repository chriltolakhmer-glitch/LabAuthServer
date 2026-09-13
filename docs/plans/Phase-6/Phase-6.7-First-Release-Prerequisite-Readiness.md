# Phase 6.7 — First-Release Prerequisite Readiness

Status: READINESS ASSESSMENT AND DECISION PACKAGE; NO RELEASE AUTHORIZED.

## 1. Purpose

This document defines the complete evidence and decision gate required before the first real LabAuthServer release can be authorized. It is a readiness assessment, not release authorization. It creates no release, tag, artifact, customer license, deployment, or production signing operation.

Phase 6.7 records what is complete, what policy is already approved, what owner values remain open, and what technical, operational, legal, and commercial gates block a first release. A readiness status must not be interpreted as approval to execute release activity.

## Readiness position

The repository is **NOT READY TO REQUEST FIRST-RELEASE AUTHORIZATION** because environment acceptance evidence, operational ownership, exact custody/provider selections, professional review, signing decisions, and other checklist items remain open. The assessment can be updated only with attributable evidence and approvals; no missing value is inferred from repository access or the existence of a successful build.

## 2. Completed prerequisites

The following prerequisites are complete as repository-level design or validation work:

| Prerequisite | Status | Evidence boundary |
| --- | --- | --- |
| Safe automated validation boundaries | COMPLETE / VERIFIED | Phase 6.1 isolates default tests from operational SQL, LDAP, credentials, and key access; explicit infrastructure tests remain opt-in. |
| Authorization and audit boundary corrections | COMPLETE / VERIFIED | Phase 6.2 records approved authorization fallback and safe audit identity handling. |
| Licensing boundary implementation | COMPLETE / VERIFIED | Phase 6.3 implements restricted-mode failure behavior, runtime expiry handling, unknown-identifier denial, and enforcement-claim boundaries. No customer license was issued. |
| Release build qualification | COMPLETE / LOCALLY VALIDATED | Phase 6.4 qualifies the Release solution, including `LabAuthServer.LicenseIssuer`, and reconciles current-state documentation. It does not publish a release artifact. |
| Audit and operational acceptance design | COMPLETE DESIGN / OPERATIONAL GATES OPEN | Phase 6.5 documents best-effort SQL audit behavior, liveness semantics, retention, monitoring, outage, and recovery evidence requirements. |
| Target-environment acceptance design | COMPLETE DESIGN / TARGET NOT ACCEPTED | Phase 6.6 defines Windows/IIS, SQL, AD/LDAP, TLS/certificate, ownership, and handoff evidence. No target environment is accepted without external evidence. |
| Repository validation | VERIFIED for the current baseline | Restore, Release build, and full test commands qualify repository behavior only; they do not prove target readiness or release authorization. |

These completed items are prerequisites for a future release review, not proof that the first release gate is satisfied.

## 3. Release governance readiness

Phase 5.4 and Phase 5.7 remain the governing records. Their approved decisions are referenced here without reopening or modifying them:

| Governance item | Current approved policy or state | Readiness implication |
| --- | --- | --- |
| Release identifier format | `LAS-vMAJOR.MINOR.PATCH-<shortsha>` is owner approved. | An actual identifier is created only in an authorized release process. |
| Release roles | Release Operator `ALOT`; Release Approval Authority `ALOT` initially; Distribution Operator `ALOT` initially; Security Response Owner `ALOT`. Separation of duties remains required where practical. | Use `ALOT` only where the existing approval assigns it; do not invent a second reviewer. Record absence of independent review when applicable. |
| Release Manifest V1 | Phase 5.4 defines source SHA, version, channel, build/CI identity, artifact names, sizes, SHA-256 values, and release record fields. | A manifest must be generated, reviewed, and stored before an actual release. No manifest artifact is created here. |
| Manifest custody policy | Vendor-controlled private storage with controlled access and append-preserving/immutable history where practical. Exact provider/location remains open. | Exact storage provider and location are a pre-release blocker. |
| Release register policy | Private/vendor-controlled, append-preserving, access-controlled, backed up, auditable, and separate from the application database. Exact product/location remains open. | Exact register product/location and custody/recovery evidence are required before release. |
| Release-record retention | Retain for the supported lifetime and indefinitely thereafter until superseded by an approved legal/business policy. Legal/contractual retention review remains pending. | Operational preservation policy exists; legal/business confirmation is still required. |
| Release channels and delivery | `Internal`, `Evaluation`, and `Production` taxonomy is approved. Controlled private delivery is approved as policy; exact provider/channel is not selected. | A channel label is not a delivery authorization. Exact delivery provider/channel remains a blocker. |
| Tag policy | Future official tags use `vMAJOR.MINOR.PATCH` and must point to the approved commit. P6-D14 approves signed release tags as policy, with an approved procedure and verification process still required. | No tag may be created during Phase 6.7. |

The source records remain authoritative: [Phase 5.4](../Phase-5/Phase-5.4-Official-Build-Provenance-and-Release-Manifest-Design.md), [Phase 5.4 owner decisions](../Phase-5/Phase-5.4-Remaining-Owner-Decision-Packet.md), and [Phase 5.7](../Phase-5/Phase-5.7-Release-Governance-and-Supported-Version-Policy.md).

## 4. Technical release blockers

| Blocker | Required resolution/evidence | Status |
| --- | --- | --- |
| Release-tag signing decision and procedure | Confirm the approved signing method, key custody, verification procedure, rotation/revocation response, and operator evidence for signed release tags under P6-D14. | OPEN; policy approved, procedure not established |
| Manifest signing | Decide whether a detached manifest signature is required for the first release, then approve custody and verification if selected. | DEFERRED / OWNER DECISION |
| Artifact/code signing | Decide whether platform or code signing is required for the first release; if selected, define certificates, custody, renewal, and verification. | DEFERRED / NOT IMPLEMENTED |
| SBOM | Decide whether an SBOM is mandatory for the first release, its format, generation source, review, and retention. | DEFERRED / FUTURE GOVERNANCE |
| Runtime metadata | Decide whether official builds must expose product version, source SHA, release ID, or build timestamp at runtime or diagnostics, including exposure limits. | DEFERRED / OWNER DECISION |
| Cross-host reproducibility | Obtain evidence beyond the verified same-host clean publish repeatability. Record OS, SDK, toolchain, artifact, and digest boundaries if cross-host reproducibility is required. | NOT VERIFIED |
| Release artifact generation | In a separately authorized rehearsal, produce the approved package, exact file inventory, byte sizes, SHA-256 values, Release Manifest V1, and review record. | NOT PERFORMED |
| Production issuance custody | Approve protected custody, access, backup/recovery, issuance procedure, and external issued-license register before any customer license operation. | OPEN; no production material selected |

These blockers must not be hidden by the successful solution build. The current CI workflow is build/test validation; it does not package, manifest, sign, publish, or deploy release artifacts.

## 5. Operational blockers

| Blocker | Required resolution/evidence | Owner |
| --- | --- | --- |
| Final target environment approval | Complete the Phase 6.6 checklist and record target identity, runtime, IIS, SQL, LDAP, TLS, permissions, and handoff evidence. | **TBD; P6-D12** |
| SQL operations ownership | Name database, backup/restore, permission, retention, growth, and outage owners; approve SQL failure and recovery policy. | **TBD; P6-D9/P6-D13** |
| AD ownership | Name directory, service-account, permission, and outage owners; provide controlled acceptance evidence without requiring production AD access. | **TBD; P6-D12/P6-D13** |
| Certificate ownership | Assign HTTPS, SQL TLS, LDAPS, and JWT signing certificate custody, renewal, expiry monitoring, and emergency replacement. | **TBD** |
| Monitoring ownership | Approve metrics, thresholds, alert destinations, on-call route, and escalation for audit, SQL, LDAP, certificates, liveness/readiness, and licensing state. | **TBD; P6-D10** |
| Backup/recovery ownership | Approve SQL and release-evidence backup/restore responsibilities, recovery targets, and restore evidence. | **TBD; P6-D9** |
| Incident escalation ownership | Define operational escalation for deployment failure, SQL/LDAP outage, certificate failure, audit loss, security event, and release withdrawal. | **TBD** |
| Audit operational policy | Resolve loss tolerance, latency/outage behavior, retention, purge, backup, recovery, and monitoring decisions before claiming operational readiness. | **TBD; P6-D7 through P6-D11** |
| Release rehearsal | Conduct a separately authorized internal rehearsal covering identity, CI, build, hashes, manifest, register, custody, reconciliation, rollback, and recovery without customer delivery. | **TBD; not performed** |
| Delivery operations | Select and validate the private delivery channel, recipient verification, delivery record, receipt evidence, and withdrawal handling. | **TBD; P54-D3/P54-D4 follow-up** |

Phase 6.6 defines the required target evidence but does not provide it. `/api/v1/health` remains liveness only and does not close SQL, LDAP, certificate, or readiness gates.

## 6. Commercial/legal blockers

| Blocker | Required resolution/evidence | Status |
| --- | --- | --- |
| Copyright/legal review | Review rights-holder, notices, proprietary wording, third-party notices, and release-facing claims. | OPEN; professional review required |
| Licensing terms review | Approve the legal/commercial terms represented by the offline licensing behavior and restrict claims to demonstrated runtime enforcement. | OPEN; professional review required |
| Evaluation terms | Approve evaluation rights, limits, duration, distribution boundaries, and customer-facing wording. | OPEN; professional review required |
| Support commitments | Define supported-version, response, maintenance, and escalation commitments; no fixed calendar support term or LTS designation is currently approved. | OPEN; commercial/legal policy required |
| Retention/legal review | Review operational release-record retention and audit/operational retention obligations against legal and contractual requirements. | OPEN; P54-D5/P6-D9 follow-up |
| Commercial approval authority | Preserve the approved `ALOT` role assignment, but obtain the actual approval record for any real release and any commercial offering. | **ALOT** approved role; no release approval exists |
| Customer license issuance terms | Define authorization, custody, issuance records, delivery, support, and withdrawal obligations before issuing any customer license. | OPEN; no customer license may be issued |

No legal, commercial, or support claim is completed by the Phase 6.3 runtime implementation alone.

## 7. First-release readiness checklist

The checklist is a gate, not an execution list. `ALOT` appears only where an existing approved record assigns that role; all other ownership remains `TBD`.

| Item | Status | Owner | Evidence |
|---|---|---|---|
| Exact source commit selected | NOT COMPLETE | ALOT when release is authorized | Approved release candidate record with full Git SHA |
| Release identifier assigned | POLICY READY; VALUE NOT CREATED | ALOT | `LAS-vMAJOR.MINOR.PATCH-<shortsha>` record |
| Release channel selected | POLICY READY; VALUE NOT SELECTED | ALOT / **TBD** | Approved Internal/Evaluation/Production classification and rationale |
| Release build qualification | COMPLETE | ALOT | Phase 6.4 Release build evidence and hosted CI reference |
| Automated validation | COMPLETE FOR REPOSITORY BASELINE | ALOT | Restore/build/test results; infrastructure opt-ins identified |
| Licensing boundary verification | COMPLETE | ALOT | Phase 6.3 implementation and licensing test evidence |
| Environment acceptance | NOT COMPLETE | **TBD** | Phase 6.6 target evidence package and decision record |
| SQL operational ownership and acceptance | NOT COMPLETE | **TBD** | Connectivity, schema, procedure, permissions, backup/recovery, outage, and capacity evidence |
| AD/LDAP ownership and acceptance | NOT COMPLETE | **TBD** | Controlled LDAPS, Root DSE, account/group, permission, and failure evidence |
| Certificate ownership and acceptance | NOT COMPLETE | **TBD** | HTTPS/SQL/LDAPS/JWT certificate inventory, trust, private-key access, renewal, and monitoring evidence |
| Monitoring and alert ownership | NOT COMPLETE | **TBD** | Approved thresholds, alerts, notification routes, and escalation evidence |
| Backup/recovery ownership | NOT COMPLETE | **TBD** | Approved backup policy, recovery targets, restore evidence, and owner |
| Incident escalation | NOT COMPLETE | **TBD** | Contact path, severity model, on-call responsibility, and withdrawal/escalation procedure |
| Manifest schema and required fields | POLICY READY | ALOT | Phase 5.4 Release Manifest V1 design |
| Manifest storage provider/location | NOT SELECTED | **TBD** | Vendor-controlled location, access, integrity, backup, and retention evidence |
| Release register provider/location | NOT SELECTED | **TBD** | Private register location, access, append history, backup, recovery, and audit evidence |
| Release-tag signing procedure | NOT COMPLETE | **TBD** | Approved signing process and verification record under P6-D14 |
| Manifest signing | NOT COMPLETE | **TBD** | Owner decision and implementation evidence, if required |
| Artifact/code signing | NOT COMPLETE | **TBD** | Owner decision and certificate/key verification, if required |
| SBOM | NOT COMPLETE | **TBD** | Approved requirement, format, generation, review, and retention, if required |
| Runtime metadata | NOT COMPLETE | **TBD** | Approved metadata contract and exposure review, if required |
| Cross-host reproducibility | NOT VERIFIED | **TBD** | Controlled cross-host comparison or approved boundary statement |
| Release artifact rehearsal | NOT PERFORMED | ALOT / **TBD** | Separately authorized internal rehearsal evidence |
| Production license issuance custody | NOT COMPLETE | **TBD** | Custody, access, backup/recovery, issuance, and register approvals |
| Copyright/legal review | NOT COMPLETE | **TBD** | Professional review record |
| Licensing terms review | NOT COMPLETE | **TBD** | Approved terms and claim review |
| Evaluation terms | NOT COMPLETE | **TBD** | Approved evaluation terms and distribution wording |
| Support commitments | NOT COMPLETE | **TBD** | Approved support/version policy and customer-facing commitments |
| Retention/legal review | NOT COMPLETE | **TBD** | Legal/business review of release and operational retention |
| Delivery channel/provider | NOT SELECTED | **TBD** | Approved private delivery path, recipient verification, and receipt evidence |
| Separate release approval | NOT COMPLETE | ALOT initially / **TBD** independent reviewer | Explicit approval after all gates pass; no approval exists in this phase |

### Deferred capabilities

The following are intentionally outside the first-release readiness implementation unless separately approved and evidenced: online activation, online revocation, machine binding, MFA, SSO/federation, refresh tokens, stateful token revocation, audit retention/purge automation, production monitoring integration, manifest signing, artifact/code signing, SBOM generation, and runtime build metadata embedding. A deferred capability must not be represented as implemented or commercially enforced.

## 8. Release authorization boundary

Completion of Phase 6.7 does not create release authorization. It only produces a readiness assessment and exposes the evidence required for a separate owner decision.

A separate, explicit owner approval is required before any of the following:

- tag creation;
- artifact generation for distribution or artifact publication;
- customer delivery;
- deployment;
- production license issuance;
- production signing-key or certificate operation.

No Phase 6.7 document, successful build, CI run, manifest design, owner role assignment, or readiness checklist entry authorizes those actions.

## Validation and non-authorizations

The required repository validation for this documentation-only package is:

```powershell
dotnet restore
dotnet build LabAuthServer.slnx -c Release --no-restore
dotnet test LabAuthServer.slnx --no-build --no-restore
```

Validation proves repository behavior only. This package makes no source, runtime, dependency, database, deployment, workflow, or repository-setting change. It does not create a release, tag, manifest artifact, signed artifact, customer license, deployment, production environment change, or Phase 5.6 modification.

The governing inputs are [Phase 5.4](../Phase-5/Phase-5.4-Official-Build-Provenance-and-Release-Manifest-Design.md), [Phase 5.4 owner decisions](../Phase-5/Phase-5.4-Remaining-Owner-Decision-Packet.md), [Phase 5.7](../Phase-5/Phase-5.7-Release-Governance-and-Supported-Version-Policy.md), [Phase 6 owner decisions](Phase-6-Owner-Decision-Packet.md), [Phase 6.3](Phase-6.3-Licensing-Boundary-Assurance.md), [Phase 6.4](Phase-6.4-Release-Build-and-Documentation-Reconciliation.md), [Phase 6.5](Phase-6.5-Audit-and-Operational-Acceptance-Design.md), and [Phase 6.6](Phase-6.6-Target-Environment-Acceptance.md).
