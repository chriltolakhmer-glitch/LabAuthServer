> Historical multi-owner workflow, superseded 2026-09-13 by the [solo developer release checklist](../../plans/Phase-6/Release-Checklist.md). Original decisions, pending items, and results below are retained as history, not future release gates.

# Phase Release Evidence Collection Packet

Status: EVIDENCE COLLECTION DEFINED; RELEASE PREFLIGHT NOT READY TO RETRY.

This packet operationalizes the pending items in [Phase Release Preflight Completion Checklist](Phase-Release-Preflight-Completion-Checklist.md). No item is complete. Existing design, governance, CI, or role evidence is identified where useful, but it does not substitute for the required owner evidence and signoff below.

## A. Signing and provenance

| Pending item | Exact required evidence | Acceptable evidence format | Accountable owner | Approval/signoff requirement | Evidence storage |
| --- | --- | --- | --- | --- | --- |
| Release tag signing procedure | Approved `vMAJOR.MINOR.PATCH` tag procedure; exact tag contents; signer identity; signing command/tool; verification command/result; immutable-tag rule; rotation/revocation response; failure and recovery procedure | Version-controlled Markdown procedure plus a completed dry-run record that does not publish a tag; verification output and signed approval record | ALOT as release approval authority; named procedure owner TBD | ALOT approval authority signs the procedure; independent reviewer signs when available, otherwise the exception is recorded | Vendor-controlled private release-governance location and release register; no private key in the repository |
| Signing custody | Named signer; key identifier/fingerprint; protected storage description; access list; recovery; rotation/revocation; separation-of-duties decision; evidence that the key is available without exposing secret material | Custody record containing public fingerprint, role/access matrix, custody attestation, recovery test result, and exception record; never store private key material | ALOT / release operator; custody owner TBD | ALOT signs custody acceptance; security owner acknowledges controls; missing second-person review is explicitly recorded | Restricted private custody record and release register; public fingerprint may be referenced in the manifest |
| Provenance evidence | Exact release SHA; release identifier; source snapshot; repository state; build SDK/configuration; CI run; artifact inventory; hashes; manifest; signatures; SBOM; runtime metadata; reproducibility limits | Immutable provenance manifest plus source snapshot record, CI link, hash file, SBOM, runtime metadata, and signed review record | ALOT / release operator | Release approval authority signs the complete package; security/provenance reviewer signs or records unavailable-review exception | Private release register with immutable manifest storage and controlled backup |

Existing references: [Release Governance](Release-Governance.md) defines tag format and Release Manifest V1 but records release-tag signing as unresolved; [Phase Release Execution Report](Phase-Final-Release-Execution-Report.md) records that no signing attempt occurred.

## B. Artifact custody

| Pending item | Exact required evidence | Acceptable evidence format | Accountable owner | Approval/signoff requirement | Evidence storage |
| --- | --- | --- | --- | --- | --- |
| Artifact inventory | Final filenames; artifact purpose; version; exact source SHA; build configuration; byte size; runtime/dependency contents; configuration-template treatment; documentation package; issuer qualification output where applicable | Reviewed inventory table in the release manifest with one row per artifact and matching file listing | ALOT / release operator | Release operator signs inventory; approval authority signs release-candidate contents | Private manifest storage and release register; artifacts remain unpublished during collection |
| Artifact hashes | SHA-256 digest and exact byte size for every final artifact; hash tool/version; generation timestamp; source/build binding | Machine-generated lowercase hexadecimal hash file plus independently reviewed manifest comparison | ALOT / release operator | Release operator signs generation record; approval authority or independent reviewer signs comparison | Immutable private artifact record and manifest; retain hash output with release register |
| Release manifest | Release identifier; exact Git SHA; CI run; build timestamp; artifact names/sizes/SHA-256; operator; SBOM reference; runtime metadata; signing status; limitations; approvals | Release Manifest V1 JSON or approved equivalent, schema-validated and review-signed | ALOT / release operator | Release approval authority signs manifest; provenance/security reviewer signs or records exception | Vendor-controlled private manifest store and release register with backup/recovery evidence |
| Artifact/manifest storage location | Exact provider and private location; access controls; integrity protection; retention; backup; recovery; audit trail; withdrawal procedure; custody owner | Provider selection record, access/custody matrix, storage test record, backup/recovery evidence, and location reference; no upload required for readiness | ALOT / distribution operator; provider owner TBD | ALOT approves provider and custody; operations owner signs backup/recovery; legal owner signs retention where applicable | Approved vendor-controlled private storage and private release register |

Existing references: [Release Governance](Release-Governance.md) requires exact SHA, Release Manifest V1, and vendor-controlled private distribution, but does not select the provider or storage location.

## C. Release operations

| Pending item | Exact required evidence | Acceptable evidence format | Accountable owner | Approval/signoff requirement | Evidence storage |
| --- | --- | --- | --- | --- | --- |
| Release operator | Named operator; role; access; custody responsibilities; separation-of-duties decision; backup operator; operator acceptance | Signed responsibility record and access review; no execution activity required | ALOT is named in governance; ALOT acceptance and backup designation required | ALOT signs operator acceptance; independent review absence recorded if applicable | Private release register and restricted responsibility record |
| Distribution operator | Named provider/operator; approved private channel; recipient verification; delivery record; receipt; withdrawal; access and audit controls | Provider selection record, channel control description, sample receipt/withdrawal procedure, and signed operator acceptance; no delivery performed | ALOT initially; provider/operator TBD | ALOT approves distribution operator and channel; commercial owner approves customer-facing controls | Private distribution/custody record and release register |
| Rollback owner | Named owner and backup; rollback triggers; authority; recovery point; backup; withdrawal/supersession; rehearsal plan and result | Signed rollback runbook, responsibility matrix, backup/recovery evidence, and rehearsal record; rehearsal must be separately authorized | Operations owner TBD | Deployment authority and operations owner sign; target owner confirms feasibility | Restricted operations runbook and release register; no production rehearsal in this packet |
| Incident owner | Primary and backup contacts; severity/escalation route; response target; security/commercial handoff; communications procedure | Signed incident responsibility matrix, contact verification, escalation test record, and support handoff approval | ALOT security response owner initially; backup TBD | Operations/support owner signs; security owner acknowledges; commercial owner signs customer route | Restricted operations/support record and release register |

Existing references: [Release Governance](Release-Governance.md) names ALOT for release, distribution, and security roles, but explicitly leaves provider selection and two-person separation unresolved.

## D. Environment acceptance

| Pending item | Exact required evidence | Acceptable evidence format | Accountable owner | Approval/signoff requirement | Evidence storage |
| --- | --- | --- | --- | --- | --- |
| Environment approval | Target identity; IIS site/app pool; SQL server/database/permissions; AD/LDAPS endpoint and service account; TLS/certificates; DPAPI/key access; configuration ownership; capacity; logging; handoff | Completed target acceptance record with redacted configuration identifiers, command/output evidence, timestamps, owner signatures, and no secrets | Named target-environment owner TBD; SQL, AD, IIS, certificate, and AD owners named separately | Target owner signs acceptance; each component owner signs; security/operations owner signs residual risks | Restricted target-acceptance repository and operations evidence store; secrets remain in approved secret stores |
| Deployment owner | Named deployment operator; target authority; prerequisites; approved procedure; change window; backup; validation; rollback contact | Signed deployment responsibility record linked to approved deployment runbook and target acceptance | Deployment owner TBD; target owner required | Target authority and operations owner sign; release authority acknowledges | Restricted deployment/change-management record and release register |
| Rollback plan | Target-specific package backup; recovery point; trigger thresholds; decision authority; restore steps; communications; rehearsal or approved reason rehearsal is unavailable | Versioned rollback runbook, backup verification, trigger matrix, owner signoff, and rehearsal evidence/exception | Rollback owner TBD; operations owner required | Deployment and target owners sign; approval authority accepts only explicit residual risk | Restricted operations runbook, backup record, and release register |
| Validation plan | Pre-deployment checks; package/hash verification; HTTPS/TLS; health; protected endpoint; AD identity/group mapping; SQL audit; response headers; post-deployment checks; rollback criteria | Versioned validation plan with commands, expected results, evidence locations, pass/fail fields, and target owner approval; no validation execution required for collection | Deployment and operations owners TBD; security/QA reviewer required | Target owner and operations owner sign; security reviewer signs or records exception | Restricted validation evidence store linked from release register |

Existing references: [Deployment](../../Deployment.md) defines the generic release flow and states target IIS, real AD identity, certificate private-key, and DPAPI availability are not independently proven; [Validation Status](../../Validation_Status.md) records the same boundary.

## E. Legal/business approval

| Pending item | Exact required evidence | Acceptable evidence format | Accountable owner | Approval/signoff requirement | Evidence storage |
| --- | --- | --- | --- | --- | --- |
| Support approval | Supported versions; support scope; response targets; escalation; exclusions; lifecycle/retirement; contact; backup contact; customer-facing wording | Approved support policy or decision record with named owner, version/date, conditions, and customer-facing copy | ALOT / commercial authority; support owner TBD | Commercial authority signs; professional legal/business reviewer acknowledges terms | Restricted commercial/support record and release register |
| Customer delivery approval | Approved recipient class; delivery provider/channel; identity verification; restrictions; receipt; withdrawal; data handling; delivery record; customer notice | Signed delivery authorization and channel/provider selection record with template receipt; no delivery performed | ALOT / distribution operator; provider and terms owner TBD | Commercial authority and distribution operator sign; legal reviewer signs customer terms | Restricted commercial/delivery record and release register |
| Commercial/legal approval | Copyright/proprietary/evaluation terms; licensing claims; retention; warranty/disclaimer; support; customer delivery; restrictions; approvers and dates | Professional legal/commercial review memo or approved decision record with exact version/reference and conditions | ALOT / commercial authority; professional reviewers TBD | ALOT signs business approval; professional legal/commercial reviewer signs or records formal exception | Restricted legal/commercial repository and release register |

Existing reference: [Release Governance](Release-Governance.md) records governance-level support and commercial roles but states exact provider selections and professional legal/business review remain pending.

## Collection rules

- A design document, role assignment, successful CI run, or commit signature is supporting evidence only; it does not complete an execution prerequisite by itself.
- Evidence must identify the exact release candidate and remain traceable to the owner, approval, storage location, and expiry or review date.
- Redact secrets, private keys, credentials, tokens, and protected directory responses. Store sensitive evidence only in approved restricted locations.
- No tag, artifact, manifest publication, deployment, license issuance, or customer delivery is performed by this packet.
- Any failed, stale, conflicting, or unverifiable evidence returns the item to `PENDING` and blocks retry.

## Release preflight retry status

```text
RELEASE PREFLIGHT RETRY STATUS:

[ ] READY TO RETRY
[ ] NOT READY
```

The status remains **NOT READY** until every mandatory item has acceptable evidence, accountable ownership, required signoff, and the approved storage reference. No item is marked complete by this packet.
