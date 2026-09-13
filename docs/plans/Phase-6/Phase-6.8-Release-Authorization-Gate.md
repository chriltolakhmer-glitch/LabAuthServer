# Phase 6.8 — Release Authorization Gate

Status: DECISION GATE PREPARED; NO RELEASE AUTHORIZED.

## 1. Purpose

Phase 6.8 is the final documented decision gate before a first real release may be authorized. It assembles the completed evidence, unresolved owner decisions, and required approvals into one go/no-go record.

Phase 6.8 does not authorize a release by itself. It creates no release, tag, artifact, customer delivery, deployment, license issuance, or production-system change. A separate owner approval must be recorded after every required gate has acceptable evidence or an explicitly approved exception.

## 2. Completed evidence

The following evidence packages are available as inputs to this gate. Their limits remain in force and are not upgraded into release authorization by this document.

| Evidence | Status | Boundary |
| --- | --- | --- |
| Phase 6.1 validation | COMPLETE / VERIFIED | Safe default validation isolates operational SQL, LDAP, credentials, and key access; explicit infrastructure tests remain controlled and opt-in. | 
| Phase 6.3 licensing | COMPLETE / VERIFIED | Restricted-mode behavior, expiry handling, unknown-identifier denial, and enforcement-claim boundaries are implemented and tested. No customer or production license was issued. |
| Phase 6.4 build qualification | COMPLETE / LOCALLY VALIDATED | Release solution qualification includes `LabAuthServer.LicenseIssuer`; build qualification does not create or publish release artifacts. |
| Phase 6.5 operations design | COMPLETE DESIGN / OPERATIONAL DECISIONS OPEN | Audit durability, SQL outage, retention, monitoring, and liveness/readiness evidence requirements are documented; owners and thresholds remain open. |
| Phase 6.6 environment acceptance | COMPLETE DESIGN / TARGET NOT ACCEPTED | Windows/IIS, SQL, AD/LDAP, TLS/certificate, permissions, and handoff evidence are defined; no target environment is accepted without evidence. |
| Phase 6.7 readiness package | COMPLETE ASSESSMENT / NOT READY | First-release prerequisites and blockers are catalogued; target evidence, custody/provider choices, professional review, signing decisions, and rehearsal remain open. |

Repository build and test results prove repository behavior only. They do not prove target acceptance, legal approval, production custody, customer authorization, or release approval.

## 3. Final release gate checklist

| Gate | Status | Evidence Required | Owner |
|---|---|---|---|
| Build qualification | COMPLETE FOR REPOSITORY | Release restore/build evidence, exact source SHA, project coverage including `LabAuthServer.LicenseIssuer`, and successful hosted CI | ALOT for release process |
| Test qualification | COMPLETE FOR CURRENT BASELINE | Approved unit/integration results, infrastructure-test classification, and exact hosted CI evidence | ALOT for release process |
| Environment acceptance | OPEN | Completed Phase 6.6 decision record covering IIS, SQL, AD/LDAP, TLS, permissions, configuration, logging, and handoff | **TBD** |
| Licensing readiness | OPEN FOR RELEASE OPERATION | Approved product/version/channel terms, production issuance custody, license register, and evidence that claims match runtime enforcement | **TBD**; ALOT is approved Licensing Operator initially |
| Security review | OPEN | Security review appropriate to release risk, dependency/security assessment, signing/key custody review, and recorded findings disposition | ALOT is approved Security Response Owner; reviewer **TBD** |
| Operational ownership | OPEN | Named target, SQL, AD, certificate, monitoring, backup/recovery, incident, and support owners with escalation paths | **TBD** |
| Legal/business review | OPEN | Copyright, proprietary/evaluation terms, commercial licensing, support, and retention/legal review records | **TBD**; professional review required |
| Delivery custody | OPEN | Approved private delivery provider/channel, recipient verification, manifest storage, release register, access control, backup, and receipt evidence | **TBD**; ALOT is approved Distribution Operator initially |
| Rollback readiness | OPEN | Approved rollback/withdrawal/supersession procedure, retained prior artifact evidence, deployment recovery owner, and tested internal rehearsal | **TBD** |

Every gate must be `PASS` or have a separately approved exception before the authorization decision can be `YES`. `COMPLETE` repository evidence does not close an `OPEN` operational or external gate.

## 4. Remaining owner decisions

No value is invented here. The following decisions must be recorded by the appropriate owner before release authorization:

| Decision | Current status | Required record |
| --- | --- | --- |
| Release approval authority confirmation | Existing policy assigns ALOT initially; no release-specific approval exists | Confirm the approver for the actual release and record any independent review or its absence |
| Target environment owner | **TBD** / P6-D12 | Named owner and acceptance authority |
| SQL owner | **TBD** / P6-D9 and P6-D13 | Database, backup/recovery, permissions, retention, capacity, and outage ownership |
| AD owner | **TBD** / P6-D12 and P6-D13 | Directory, service-account, permissions, acceptance, and outage ownership |
| Certificate owner | **TBD** | HTTPS, SQL TLS, LDAPS, JWT signing custody, renewal, monitoring, and emergency replacement |
| Delivery provider | **TBD** | Approved private delivery channel/provider and recipient/receipt controls |
| Manifest storage | **TBD** | Vendor-controlled private location/provider, access, integrity, backup, and retention |
| Release register | **TBD** | Private register product/location, append history, access, backup, recovery, and audit controls |
| Support owner | **TBD** | Supported-version, response, escalation, and customer-facing support commitments |

Existing `ALOT` assignments are preserved only where already approved: Release Operator, initial Release Approval Authority, initial Distribution Operator, Licensing Operator initially, Commercial Approval Authority, and Security Response Owner. These assignments do not fill the unassigned operational or professional roles above.

## 5. Release authorization boundary

No release is authorized until owner approval is recorded in a release-specific decision record after the final checklist is reviewed.

No tag, artifact, customer delivery, deployment, or license issuance may occur before that approval. This includes signed tag creation, manifest or artifact publication, production signing-key use, and customer-facing distribution.

Phase 6.8 completion means only that the decision package is prepared. It does not convert policy into execution authority, does not approve exceptions implicitly, and does not override Phase 5.6 constraints or the deferred Phase 6 decisions.

## 6. Go / No-Go form

Complete this form only through a separate owner decision process. Blank fields are intentional and are not approval defaults.

```text
FIRST RELEASE AUTHORIZATION DECISION

APPROVE RELEASE:
YES / NO

OWNER:
DATE:

BLOCKERS ACCEPTED:
```

The form must reference the exact source SHA, release identifier, release channel, evidence package, checklist result, exceptions, approver, and approval date when it is eventually completed. A `YES` is invalid unless the owner has reviewed the evidence and explicitly accepted any recorded blockers.

## Validation and non-authorizations

This documentation-only package is validated with:

```powershell
dotnet restore
dotnet build LabAuthServer.slnx -c Release --no-restore
dotnet test LabAuthServer.slnx --no-build --no-restore
```

The package makes no source, runtime, dependency, database, deployment, workflow, or repository-setting change. It does not create a release, tag, artifact, customer license, deployment, or production environment change, and it does not modify Phase 5.6.