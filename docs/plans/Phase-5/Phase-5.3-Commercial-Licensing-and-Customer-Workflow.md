# Phase 5.3 — Commercial Licensing and Customer Workflow

> DRAFT — REQUIRES PROFESSIONAL LEGAL REVIEW

Status: PHASE 5.3 — OWNER APPROVED. Documentation/governance approval recorded on 2026-09-12. All Phase 5.3 documentation completion criteria are satisfied. This record remains a draft operational process around the existing Phase 4 offline signed-license implementation; professional legal/business review remains pending. It does not create a legal contract, authorize real customer-license issuance, production signing, commercial operation, implementation, deployment, public release, or customer distribution.

## 1. Purpose

Phase 4 provides technical licensing: deterministic License Version 1 signing, trusted public-key verification, explicit feature and edition enforcement, startup validation, and restricted Community behavior for missing or invalid licenses.

Phase 5.1 approved the private/controlled commercial model, manual vendor-controlled licensing, official-build provenance, and the separation between commercial permission and technical entitlement. Phase 5.2 established proprietary/evaluation documentation and the source-available boundary.

Phase 5.3 defines the commercial customer lifecycle and operator process. It describes proposed records, approvals, handoffs, delivery, installation, replacement, renewal, support, and offboarding. It does not create final legal terms.

## 2. Scope

This phase covers:

- commercial inquiry and evaluation-to-commercial transition
- requirements capture and edition/feature/limit selection
- commercial approval and issuance authorization
- external issuance recordkeeping
- offline license issuance and controlled delivery
- customer installation, restart/reload, and startup verification
- replacement, expiration, renewal, support escalation, and offboarding
- key-compromise escalation references
- safe support diagnostics and placeholder ownership

## 3. Non-Goals

This phase does not include:

- payment processing or automated billing
- CRM implementation
- customer portal or self-service
- license server
- online activation or online revocation
- machine binding
- DRM or invasive anti-tamper
- production private-key implementation
- legal contract drafting
- production source, test, CI, runtime licensing, or deployment changes
- generation of production signing keys or real customer licenses

## 4. Customer Lifecycle

| Stage | Actor | Input | Output | Approval required | Evidence/record | Security consideration |
| --- | --- | --- | --- | --- | --- | --- |
| 1. Commercial inquiry | Customer / Commercial Owner | Business need and contact reference | Tracked inquiry | Commercial Owner accepts inquiry | Request ID and customer reference | Treat customer/contact data as commercially sensitive |
| 2. Evaluation approval, if applicable | Commercial Owner | Evaluation request and intended use | Approved or declined evaluation path | Commercial Owner; legal review for terms | Evaluation approval and access period | Controlled access; no production entitlement implied |
| 3. Requirements review | Commercial Owner / Licensing Approver | Requested use, environment, edition, features, limits, duration | Validated request | Licensing Approver reviews technical request | Completed commercial request record | Do not disclose private keys or collect unnecessary secrets |
| 4. Edition/features/limits selection | Licensing Approver | Requirements and approved matrix | Proposed technical entitlement | Licensing Approver confirms explicit features | Edition, feature, limit, and validity fields | Edition is an upper bound; explicit signed features remain authoritative |
| 5. Commercial approval/agreement | Commercial Owner | Validated request and business terms | Approved commercial permission | Authorized business owner; legal review pending | Approval reference and agreement reference | Commercial permission is separate from technical entitlement |
| 6. License issuance authorization | Licensing Approver | Approved request and agreement reference | Authorized issuance instruction | Licensing Approver authorizes issuer | Issuance authorization record | Issuer access is restricted; approver need not access the private key |
| 7. Offline license issuance | Authorized License Issuer | Authorized request and approved keyId | Signed License Version 1 document | Authorized License Issuer follows approved process | Issuance metadata and License ID | Private key remains outside repository, application, and CI |
| 8. Controlled delivery | Support Operator / Release Operator | Independently verified license file | Delivered license file | Delivery recipient is approved | Delivery event and receipt confirmation | Use `<APPROVED_DELIVERY_CHANNEL>`; never send key material |
| 9. Customer installation | Customer / Support Operator | Delivered license file and configured path | File staged at `Licensing:LicenseFilePath` | Customer administrator follows procedure | Installation confirmation | Do not place licenses in GitHub, source, webroot, or generic releases |
| 10. Restart/reload | Customer / Support Operator | Installed file | Application reloads startup licensing state | Customer administrator | Restart/reload timestamp | No hot reload or per-request revalidation is implied |
| 11. Startup validation | Application / Support Operator | License file and trusted public keys | Effective policy and status | Technical verification by operator | Safe status, edition, features, limits, expiry metadata | Invalid/missing content remains restricted Community behavior |
| 12. Production-use confirmation | Commercial Owner / Customer | Commercial approval and startup validation | Operational-use confirmation | Commercial Owner confirms both boundaries | Confirmation record | A valid technical license does not replace commercial permission |
| 13. Support/renewal | Support Operator / Commercial Owner | Support request or approaching expiry | Support action or renewal request | Commercial Owner for entitlement changes | Support and renewal records | Do not request passwords, private keys, or unrelated secrets |
| 14. Replacement/correction | Licensing Approver / Authorized License Issuer | Correction, upgrade, expiry, corruption, or rotation need | Superseding signed license | Same approval boundary as issuance | Supersession, delivery, installation, validation records | Validate before activation; retain rollback copy |
| 15. Expiration/commercial termination | Commercial Owner / Support Operator | Expiry or external termination decision | External status update and support action | Commercial/legal process as applicable | Register status and offboarding record | Offline licenses cannot be remotely revoked by the current baseline |
| 16. Record retention | Commercial Owner / Licensing Owner | Request, issuance, delivery, support records | Retained controlled records | Vendor retention policy owner | Request and issued-license register entries | Keep records outside the application database; limit access |

The lifecycle is a proposed operational process and remains subject to business and professional legal review.

## 5. Roles and Separation of Duties

- **Customer / Evaluator**: supplies requirements, accepts approved process terms, installs the delivered file, and reports safe validation/support results.
- **Commercial Owner**: owns the commercial inquiry, intended-use review, commercial approval, and customer relationship.
- **Licensing Approver**: verifies the requested edition, explicit features, limits, duration, and issuance authorization.
- **Authorized License Issuer**: performs offline signing using the vendor-controlled issuance process. This role alone requires signing access; it does not require ownership of commercial approval.
- **Release Operator**: maintains vendor release provenance and records the approved product/build context delivered to the customer.
- **Support Operator**: assists with delivery, installation, restart/reload, safe diagnostics, replacement, and escalation.
- **Security Owner**: receives compromise/security escalations and coordinates incident response and key-compromise handling.

Commercial approval and license issuance are separate responsibilities. The person approving commercial entitlement does not automatically need access to the production private signing key. Only authorized issuance operators should perform signing. No specific HSM/KMS or physical key-storage product is selected by this phase.

## 6. Edition and Feature Request Workflow

The approved technical matrix is unchanged:

| Edition | Feature upper bound |
| --- | --- |
| Community | `auth.basic`, `auth.jwt` |
| Professional | Community features plus `auth.ldap`, `audit.logging` |
| Enterprise | Professional features plus `admin.console` |

Edition does not automatically grant features. The signed explicit feature list remains authoritative. Unknown features are denied, and features outside the edition upper bound are not approved.

A commercial request should record:

- requested edition
- requested explicit features
- requested limits
- perpetual or time-limited duration
- `issuedAt`
- `expiresAt`, when applicable
- approved `keyId` or key-selection instruction
- customer reference
- proposed License ID
- intended environment and use classification

## 7. Commercial Licensing Request Record

The following is a proposed vendor-side record shape. It is not an application database or implementation requirement.

| Field | Purpose |
| --- | --- |
| Request ID | Stable request reference |
| Customer Reference | Vendor-controlled customer identifier |
| Customer Name / Organization | Commercial party reference; commercially sensitive |
| Contact Reference | Approved contact reference; commercially sensitive |
| Product | Product being licensed |
| Requested Edition | Community, Professional, or Enterprise technical edition |
| Requested Features | Explicit feature request |
| Requested Limits | Requested numeric limits |
| License Duration Type | Perpetual or time-limited |
| Requested Expiry | Proposed expiry, if time-limited |
| Environment / Intended Use | Evaluation, pilot, staging, or production classification |
| Commercial Approval Status | Pending, approved, declined, or superseded |
| Approved By | Commercial approval reference |
| License Issuance Status | Not authorized, authorized, issued, delivered, superseded |
| License ID | Proposed or issued identifier |
| `keyId` | Signing-key identifier, not private key material |
| Issued At | Issuance timestamp |
| Expires At | Signed expiry, if applicable |
| Issuer | Authorized issuer role/reference |
| Delivery Status | Pending, delivered, receipt confirmed |
| Support Status | Support/renewal state |
| Notes | Controlled operational notes without secrets |

Customer and contact fields are commercially sensitive. No real customer records are created by this phase.

## 8. Commercial Request vs Issued License Register

The **Commercial Request Record** captures what was requested, reviewed, and approved before issuance.

The **Issued License Register** records what was actually issued and delivered. It remains an external controlled operational record as approved by Phase 4.17, not an application database.

The issued-license register should retain at least:

- License ID
- Customer Reference
- Product
- Edition
- Features
- Limits
- `IssuedAt`
- `ExpiresAt`
- `keyId`
- Status
- Issuer
- Notes

The register must never contain a production private key, private-key backup, credential, or signing secret.

## 9. Commercial Permission vs Technical Entitlement

**Commercial permission** comes from the applicable business process and written agreement, subject to professional legal review.

**Technical entitlement** comes from the signed license document validated by the application.

Neither automatically substitutes for the other:

- A valid signed license without commercial permission does not automatically grant production rights.
- Commercial approval without an installed valid technical license may result in restricted Community runtime behavior.
- Restricted Community runtime behavior is a technical state, not a legal evaluation grant.

This is an operational distinction, not a legal conclusion. Legal interpretation remains pending professional legal review.

## 10. Offline License Issuance Workflow

The proposed offline sequence is:

1. Verify the approved commercial request and intended use.
2. Verify edition, explicit features, limits, validity type, and dates.
3. Allocate or confirm the License ID.
4. Select the current authorized `keyId`.
5. Construct the deterministic License Version 1 payload.
6. Sign using the vendor-controlled private key outside the repository, application, and CI.
7. Verify the resulting license using the trusted public key.
8. Record issuance metadata in the external issued-license register.
9. Deliver the license through the approved controlled channel.
10. Confirm customer receipt.

LabAuthServer must never issue commercial licenses. GitHub Actions must never hold the production private key. Phase 5.3 does not implement or perform real signing.

## 11. Controlled License Delivery

- Deliver only to the approved customer/contact.
- Record the delivery event and receipt confirmation.
- Never send private signing material.
- Treat the license document as customer-sensitive operational data.
- Use `<APPROVED_DELIVERY_CHANNEL>` until an approved channel is documented.
- Do not store customer licenses in GitHub or source control.
- Do not put customer license files in the webroot.
- Do not package customer licenses into generic product releases.

No specific delivery vendor or platform is prescribed by this phase.

## 12. Customer Installation and Startup Verification

The configured location is `Licensing:LicenseFilePath`.

Operational sequence:

1. Validate the license before installation.
2. Stage the file with appropriate administrator-controlled permissions.
3. Replace the configured file atomically where practical.
4. Restart or reload the application.
5. Verify startup licensing status.
6. Confirm the expected edition, explicit features, limits, and expiry state.

Hot reload and per-request license revalidation are not implemented. Missing, invalid, expired, or otherwise unverifiable content produces restricted Community behavior; this technical state does not itself authorize evaluation or production use.

## 13. Replacement and Correction

Replacement may be required for:

- wrong edition
- wrong explicit features
- wrong limits or expiry
- customer-reference correction
- damaged or corrupted file
- signing-key rotation
- commercial upgrade or downgrade

The process is:

request
→ approval
→ issue replacement
→ record supersession
→ controlled delivery
→ stage
→ atomic replacement
→ restart/reload
→ startup validation

The current baseline does not provide technical revocation. An external register status such as Replaced or Terminated is an operational record, not remote invalidation of an already issued offline license.

## 14. Expiration and Renewal

- **Perpetual license**: omits expiry according to the current implementation and does not expire through the license timeline.
- **Time-limited license**: uses the existing exclusive expiry enforcement rules.
- **Grace policy**: no usable grace period; grace remains default-disabled and diagnostic only.
- **Renewal**: issue a new signed license through the approved request and issuance process. Automatic renewal is not implemented.

## 15. Offboarding and Termination

Because online revocation is deferred:

- record terminated or replaced status in the external operational register
- do not issue future licenses without authorization
- apply customer obligations through the applicable reviewed agreement
- recognize that an existing offline license cannot be remotely revoked by the current baseline
- remove source-access distribution access at the vendor layer where practical
- follow support and incident escalation procedures for retained files or credentials

This section is an operational draft and is not legal advice.

## 16. Key-Compromise Escalation

At a high level:

1. Suspend issuance using the affected `keyId`.
2. Escalate to the Security Owner and Licensing Approver.
3. Activate the approved replacement-key process.
4. Identify affected licenses and issue replacements where required.
5. Update the trusted public-key set through a separately approved application/release process.
6. Record the incident, decisions, and customer communications in controlled operational records.

No key material or production rotation mechanism is created by Phase 5.3.

## 17. Customer Support Boundary

Support operators may safely inspect or request, where necessary:

- License ID
- edition
- explicit feature identifiers
- limits metadata
- expiry metadata
- `keyId`
- public validation status/reason category
- application version
- safe startup log metadata

Do not request or collect:

- production private keys or signing secrets
- customer passwords
- unrelated authentication secrets
- raw license payloads when metadata is sufficient
- unrelated customer data

## 18. Placeholder Register

| Placeholder | Purpose | Files | Resolve before external commercial operation? |
| --- | --- | --- | --- |
| `<COMMERCIAL_CONTACT>` | Commercial and evaluation contact | `README.md`, `docs/Evaluation-Use.md`, `docs/Commercial-Licensing.md` | Yes |
| `<SECURITY_CONTACT>` | Vulnerability-reporting channel | `SECURITY.md` | Yes |
| `<COPYRIGHT_OWNER>` | Ownership identity | `COPYRIGHT.md` | Yes |
| `<APPROVED_DELIVERY_CHANNEL>` | Approved controlled license-delivery channel | This document | Yes |
| `<COMMERCIAL_APPROVAL_AUTHORITY>` | Named commercial approval authority | Future operational record | Yes |
| `<LICENSING_OPERATOR>` | Named authorized issuer/operator reference | Future operational record | Yes |

No real placeholder values are invented by this phase.

## 19. Phase 5.3 Completion Checklist

- [x] Customer lifecycle documented.
- [x] Roles and separation of duties documented.
- [x] Edition/feature workflow documented without changing the technical matrix.
- [x] Commercial request record documented.
- [x] External issued-license register alignment documented.
- [x] Commercial permission versus technical entitlement documented.
- [x] Offline issuance workflow documented without performing signing.
- [x] Controlled delivery process documented.
- [x] Installation, restart/reload, and replacement documented.
- [x] Expiration and renewal documented without introducing grace.
- [x] Termination limitation documented without claiming remote revocation.
- [x] Key-compromise escalation documented without creating key material.
- [x] Support data boundary documented.
- [x] Placeholders registered.
- [x] Legal review markers preserved.
- [x] No source/runtime implementation changed.

## 20. Legal and Review Boundary

> DRAFT — REQUIRES PROFESSIONAL LEGAL REVIEW

This process document is not a final agreement, final license, legal opinion, or counsel-approved interpretation. Commercial terms, customer obligations, termination, support, delivery, retention, and rights require professional legal and business review.

## Current Status

PHASE 5.3 — OWNER APPROVED — 2026-09-12

The project owner approved the completed Phase 5.3 documentation on 2026-09-12. All 16 documentation completion criteria in section 19 are satisfied. Professional legal/business review remains pending; unresolved placeholders remain unresolved; no production signing or customer-license issuance occurred; and no commercial operation is authorized solely by this approval. The Phase 5.4 source document is currently ready for owner review; the earlier statement that Phase 5.4 was not authorized is historical context and is superseded by the current Phase 5.4 status in the master Phase 5 plan. Phase 5.4 is not owner-approved by this record.
