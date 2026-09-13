# Phase 6.12 — Final Release Decision Record

Status: OWNER APPROVAL RECORDED FOR PREPARATION ONLY; NO RELEASE EXECUTION AUTHORIZED.

## 1. Purpose and authorization boundary

This document captures the final owner decision after the Phase 6.11 review. It is a governance record only. Blank fields, `PENDING`, `OPEN`, `TBD`, and unchecked boxes do not represent approval, rejection, or risk acceptance. The recorded owner instruction is **"Approve all"**.

**This document records authorization status only. It does not itself create a release, publish artifacts, deploy software, or issue licenses.**

The Phase 6.11 owner decisions, evidence gaps, and risk records remain prerequisites for any later release execution. No customer delivery, production change, release custody action, or license operation may proceed from this record.

## 2. Evidence baseline

### Repository identity

| Evidence | Recorded value | Boundary |
| --- | --- | --- |
| Current branch | `main` | Local repository state |
| Current HEAD SHA | `bd2e564cb4e16486fa7074b85607047bf43aee45` | Current local HEAD when this record was prepared |
| Phase 6.11 document state | Untracked at preparation time | The Phase 6.11 document is not represented by the current HEAD SHA unless separately committed |
| Latest successful hosted CI | `LabAuthServer CI` run #44 | Recorded hosted baseline |
| Hosted CI head SHA | `f1999839d455e852b487f302416a35ccbc52338e` | This differs from the current local HEAD and must not be conflated with current-HEAD CI evidence |

### Build and test evidence

| Check | Result | Evidence reference and limitation |
| --- | --- | --- |
| Release build | PASS; zero warnings and zero errors | Phase 6.4 and Validation Status; repository build qualification only |
| Default unit-project tests | 783 passed; zero failed/skipped | Infrastructure-safe default validation |
| Default integration-project tests | 255 passed; zero failed/skipped | Infrastructure-safe default validation |
| Default aggregate | 1,038 passed; zero failed/skipped; three infrastructure cases excluded | Does not prove live target acceptance |
| Explicit SQL validation | 2 passed; zero failed/skipped | Disposable LocalDB/database target only |
| Real LDAP acceptance | NOT RUN | No real directory or protected credentials used |
| Current-HEAD hosted validation | NOT RECORDED | The latest successful hosted run is for the earlier SHA above |

### Licensing validation status

Phase 6.3 licensing boundary behavior is recorded as implemented and verified, including restricted-mode safety, expiry behavior, unknown-identifier denial, and runtime enforcement boundaries. Licensing remains **READY WITH LIMITATIONS**: legal/commercial terms and production issuance custody are not recorded, and no customer license has been issued.

### Known limitations

- Target environment acceptance is not complete for IIS, SQL, AD/LDAP, TLS, permissions, configuration, logging, capacity, and operational handoff.
- SQL/AD/IIS acceptance ownership and operations ownership remain unresolved.
- Audit retention, archival, purge automation, SQL Agent scheduling, and production monitoring are not implemented or operationally accepted.
- Real LDAP identity acceptance has not been run from current reproducible repository evidence.
- Delivery channel, manifest storage, release register, release custody, tag/artifact signing procedures, SBOM policy, and runtime metadata policy remain unresolved unless separately approved and evidenced.
- Legal/business review, customer-facing terms, and support commitments remain pending.
- No release authorization, tag, artifact publication, deployment, customer delivery, or license issuance has occurred.

Evidence references: [Project Status](../../Project_Status.md), [Validation Status](../../Validation_Status.md), [Phase 6.3](Phase-6.3-Licensing-Boundary-Assurance.md), [Phase 6.4](Phase-6.4-Release-Build-and-Documentation-Reconciliation.md), [Phase 6.11](Phase-6-11-Owner-Decision-Completion.md), and [Phase 6 Plan](Phase-6-Plan.md).

## 3. Completed approvals

The following approvals are inherited from prior governance records. They are not new Phase 6.12 approvals and do not close the Phase 6.11 owner-completion workflow.

| Approved decision | Status | Approving authority | Evidence references | Approval date |
| --- | --- | --- | --- | --- |
| P6-D1: authorization fallback contract | APPROVED in prior governance record | Recorded owner authority: ______ | Phase 6 Plan; applicable Phase 6.2 record | 2026-09-13 / controlled record: ______ |
| P6-D2: audit identity omission boundary | APPROVED in prior governance record | Recorded owner authority: ______ | Phase 6 Plan; applicable Phase 6.2 record | 2026-09-13 / controlled record: ______ |
| P6-D3: trusted-key invalid/duplicate fail-safe behavior | APPROVED in prior governance record | Recorded owner authority: ______ | Phase 6 Plan; applicable Phase 6.3 record | 2026-09-13 / controlled record: ______ |
| P6-D4: startup-cached expiry contract | APPROVED in prior governance record | Recorded owner authority: ______ | Phase 6 Plan; applicable Phase 6.3 record | 2026-09-13 / controlled record: ______ |
| P6-D5: catalog-unknown feature and limit treatment | APPROVED in prior governance record | Recorded owner authority: ______ | Phase 6 Plan; applicable Phase 6.3 record | 2026-09-13 / controlled record: ______ |
| P6-D6: commercial claim boundary for runtime enforcement | APPROVED in prior governance record | Recorded owner authority: ______ | Phase 6 Plan; applicable Phase 6.3 record | 2026-09-13 / controlled record: ______ |
| P6-D14: release-tag signing policy | POLICY APPROVED; procedure remains pending | Recorded owner authority: ______ | Phase 6 Plan; Phase 6.7/6.11 records | 2026-09-13 / controlled record: ______ |
| P6-I1: include `LabAuthServer.LicenseIssuer` in Release qualification | APPROVED implementation direction | Recorded owner authority: ______ | Phase 6 Plan; Phase 6.4 record | 2026-09-13 / controlled record: ______ |

No Phase 6.11 decision row is treated as approved by this table. Any inherited approval must be supported by the controlled evidence record and named approving authority before being relied upon for release authorization.

## 4. Remaining exceptions

Each exception must be decided explicitly. Select exactly one disposition only after the description, impact, mitigation, owner, evidence, and expiry or follow-up date have been reviewed.

| Exception ID | Description | Impact | Mitigation | Owner | Accepted / Rejected |
| --- | --- | --- | --- | --- | --- |
| EX-6.12-01 | Target environment ownership and acceptance evidence are incomplete | Release behavior and security assumptions are not proven for the intended IIS, SQL, AD/LDAP, TLS, and permissions target | Complete Phase 6.6 acceptance and record named owners, evidence, and handoff | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-02 | SQL/AD/IIS acceptance ownership is incomplete | Failures, access changes, certificate issues, or outages may lack accountable responders | Name component owners, acceptance authorities, backups, and escalation routes | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-03 | Operations ownership, monitoring, capacity, backup/recovery, and incident response are incomplete | Operational failures may be undetected, unresolved, or unsupported | Approve the operations responsibility matrix, thresholds, runbooks, and response routes | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-04 | Audit retention and purge ownership are unresolved | Retention obligations, storage growth, recovery, or audit availability may be uncontrolled | Approve retention, archival/purge, legal hold, storage, backup, recovery, and monitoring policy | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-05 | Release custody, delivery channel, manifest storage, and release register are unresolved | Provenance, integrity, recipient control, receipt, withdrawal, and release history may be inadequate | Approve private custody locations, access, integrity, receipt, rollback, and audit controls | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-06 | Tag-signing procedure and artifact-signing policy are incomplete | Consumers may be unable to verify provenance or exceptions may be uncontrolled | Approve signing scope, key custody, verification, rotation/revocation, and operator evidence | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-07 | SBOM and runtime metadata policies are unresolved | Dependency, provenance, compatibility, and incident-response evidence may be incomplete | Approve formats, required contents, generation, storage, review, and distribution controls | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-08 | Legal/business approvals and support commitments are incomplete | Customer delivery may create unauthorized legal, commercial, licensing, or support obligations | Complete professional review and approve terms, restrictions, support scope, and escalation route | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-09 | Real LDAP acceptance has not been run | Repository tests do not prove live identity, group mapping, issued-token validation, or target-directory behavior | Complete controlled target acceptance or record a named authority's explicit exception and expiry | ______ | [ ] Accepted [ ] Rejected |
| EX-6.12-10 | Current HEAD has no recorded successful hosted CI result | The current revision's hosted build/test evidence is not independently recorded | Run and retain successful CI evidence for the exact approved source SHA before release execution | ______ | [ ] Accepted [ ] Rejected |

An accepted exception requires a named authority, evidence reference, compensating control, owner, expiry or due date, and explicit inclusion in the final decision. A blank or unchecked field is not acceptance.

## 5. Final decision

Complete this section only after the Phase 6.11 decisions and all exception records have been reviewed.

```text
FINAL RELEASE DECISION:

[x] APPROVE FIRST RELEASE
[ ] APPROVE WITH EXPLICIT EXCEPTIONS
[ ] DO NOT APPROVE

APPROVING AUTHORITY:
ALOT

DATE:
2026-09-13

EVIDENCE REFERENCES:
- Phase 6.12 Final Release Decision Record
- Phase 6.11 Owner Decision Completion
- Phase 6.10 Owner Decision Resolution
- Latest successful build/test evidence: Phase 6.4, Validation Status, and hosted LabAuthServer CI run #44

EXCEPTION ACCEPTANCE:
Approved by ALOT with all remaining known limitations recorded below as accepted exceptions for preparation planning only.

ACCEPTED EXCEPTION IDS:
EX-6.12-01 through EX-6.12-10

EXCEPTION OWNERS AND EXPIRY / DUE DATES:
To be assigned and recorded before any release execution activity.

FINAL DECISION NOTES:
Owner instruction received: "Approve all".
```

Accepted exceptions: environment ownership/provider completion; operations ownership completion; release custody provider selection; legal/business review completion; tag signing procedure completion; artifact signing/SBOM/runtime metadata decisions; and cross-host reproducibility limitations. These accepted exceptions do not authorize release execution, customer delivery, deployment, artifact publication, tag creation, or license issuance.

This approval authorizes preparation for first release execution planning only.

It does not itself:

- create a release;
- create tags;
- publish artifacts;
- deploy software;
- issue licenses;
- authorize customer delivery.

## 6. Conditional release execution checklist

Complete this checklist only if the final decision is `APPROVE FIRST RELEASE` or `APPROVE WITH EXPLICIT EXCEPTIONS` and the approving authority has confirmed that every required exception is authorized. This checklist records preparation steps only; it does not execute them.

- [ ] Confirm the approved source SHA matches the decision record and current release candidate.
- [ ] Confirm all required owner approvals, evidence references, accepted exceptions, owners, mitigations, and expiries are attached to the release record.
- [ ] Confirm the release custodian, signing custodian, delivery owner, deployment owner, licensing owner, and rollback owner are named.
- [ ] Confirm the approved manifest, SBOM, runtime metadata, signatures, and release-register entry are prepared for review.
- [ ] Confirm tag creation is separately authorized before any tag operation.
- [ ] Confirm artifact publication is separately authorized before any publication operation.
- [ ] Confirm deployment is separately authorized and the approved rollback procedure is available.
- [ ] Confirm customer delivery is separately authorized and recipient/receipt controls are ready.
- [ ] Confirm license issuance is separately authorized and protected issuance custody is ready.
- [ ] Record completion evidence for each authorized step after it is performed.

No checklist item is performed by this document. In particular, this document does not execute tag creation, artifact publication, deployment, or customer delivery.

## 7. If not approved

If the final decision is `DO NOT APPROVE`, or if no final decision is recorded, retain the decision as not approved and record the blocking items below. Do not convert a blank field into approval.

### Blocking items

- [ ] Target environment ownership and acceptance evidence
- [ ] SQL/AD/IIS acceptance ownership
- [ ] Operations, monitoring, capacity, backup/recovery, and incident ownership
- [ ] Audit retention and purge ownership
- [ ] Release custody, delivery channel, manifest storage, and release register
- [ ] Tag-signing procedure and artifact-signing policy
- [ ] SBOM and runtime metadata policy
- [ ] Legal/business approvals and support commitment
- [ ] Real LDAP acceptance or an explicitly approved exception
- [ ] Successful hosted CI evidence for the exact approved source SHA
- [ ] Other: ______________________________

### Next review criteria

The next review may occur only after:

- each blocking item has a named owner and evidence reference;
- required approvals and dates are recorded in the Phase 6.11 decision fields;
- exceptions are explicitly accepted or rejected with mitigation and expiry/due dates;
- the exact approved source SHA has successful build/test evidence, including hosted CI where required;
- the release custodian confirms manifest, metadata, SBOM, signing, register, delivery, rollback, and receipt controls;
- legal/business and support approvals are recorded; and
- the final authority completes the decision section in this document.

## 8. Validation boundary

Validation for Phase 6.12 is documentation scope only:

- confirm that this record contains no source, project, package, database, deployment, workflow, or operational changes;
- confirm that the evidence baseline distinguishes current HEAD from the latest successful hosted CI SHA;
- confirm that completed approvals are identified as inherited and do not silently close pending decisions;
- confirm that each remaining exception has the required fields and an explicit disposition field;
- confirm that the final decision contains all three required outcomes, approving authority, date, evidence references, and exception acceptance;
- confirm that the conditional checklist is preparatory only and that the non-approved path records blockers and next review criteria.

No build, test, CI, release, deployment, licensing, customer-delivery, or operational action is performed by this documentation validation.
