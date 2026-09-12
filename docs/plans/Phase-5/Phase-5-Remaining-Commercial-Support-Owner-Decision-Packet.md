# Phase 5 - Remaining Commercial and Support Owner Decision Packet

> DOCUMENTATION-ONLY DECISION PREPARATION — NO COMMERCIAL OPERATION, LICENSE ISSUANCE, RELEASE, OR LEGAL CLAIM IS AUTHORIZED

## 1. Purpose

This packet prepares the remaining Phase 5 commercial and support owner decisions that the project owner can decide without pretending that professional legal review has occurred.

It distinguishes:

1. exact owner values needed now;
2. policy decisions the owner can approve now;
3. values that should remain deferred until actual commercial operation;
4. items that require professional legal or business review.

This packet does not implement commercial operation, issue a customer license, create a release, publish artifacts, deploy, change runtime behavior, or make legal claims. No legal identity, delivery provider, contractual promise, or support obligation is invented here.

## 2. Verified baseline

| Item | Verified value |
| --- | --- |
| Branch | `main` |
| Current Git SHA | `ac915ddefcbe3395e09e4426a75cdb890484982c` |
| Current HEAD commit | `Reconcile Phase 5 commercial decision reference` |
| Hosted validation | `LabAuthServer CI`, run `#37`, run ID `34698559913`, head `ac915ddefcbe3395e09e4426a75cdb890484982c`, status `completed`, conclusion `success` |
| Repository visibility | `PRIVATE` |
| Release existence | `NO RELEASE EXISTS` |
| Commercial operation | `NOT AUTHORIZED` |
| Phase 5.4 status | P54-D1 through P54-D6 are `OWNER APPROVED` |
| Phase 5.7 status | `OWNER APPROVED`; governance-state reconciliation complete |
| Phase 5.6 status | `FROZEN / UNCHANGED` — platform-blocked and not modified by this packet |

## 3. Decision matrix

Each decision records its current status, the recommendation, the rationale, the tradeoff or boundary, and the exact owner-response field.

### P5-C1 — Copyright / rights-holder identity

- Decision: the exact individual or legal-entity name that should appear in `COPYRIGHT.md`.
- Current status: `OWNER VALUE REQUIRED`.
- Current placeholder: `<COPYRIGHT_OWNER>` in `COPYRIGHT.md`.
- Recommendation: none. This is a legal identity field; no value is recommended.
- Rationale: copyright ownership is a legal fact, not a governance convention. `ALOT` is a governance identity, `chriltolakhmer-glitch` is a platform account name, and `chriltola.khmer@gmail.com` is a contact address. None of these is automatically the legal copyright owner.
- Boundary: do not infer `ALOT`, `chriltolakhmer-glitch`, `chriltola.khmer@gmail.com`, or any company or legal entity. The exact legal/rights-holder name must come from the owner. `COPYRIGHT.md` is not modified by this packet.
- Exact owner-response field: `P5-C1 COPYRIGHT / RIGHTS-HOLDER IDENTITY:` (owner input required; intentionally left blank)

### P5-C2 — Approved delivery channel

- Decision: the controlled channel used for customer license delivery, approved production artifact delivery where applicable, and delivery evidence / receipt confirmation.
- Current status: `OWNER APPROVED — POLICY`.
- Current placeholder: `<APPROVED_DELIVERY_CHANNEL>`.
- Recommendation: `VENDOR-CONTROLLED PRIVATE DELIVERY CHANNEL; ACCESS LIMITED TO AUTHORIZED OPERATORS; RECIPIENT IDENTITY VERIFIED; DELIVERY EVENT AND RECEIPT RECORDED; EXACT PROVIDER / CHANNEL TO BE SELECTED BEFORE FIRST EXTERNAL CUSTOMER DELIVERY`.
- Rationale: the policy requirements are stable and can be approved now; the exact service or provider is an operational choice that does not need to be fixed before a first external delivery exists.
- Boundary: ordinary public links are not approved; GitHub / source control must not be used for customer license files; customer licenses must not be bundled into generic releases; private signing material must never be sent. No provider is selected by this packet.
- Exact owner-response field: `P5-C2 APPROVED DELIVERY CHANNEL POLICY: VENDOR-CONTROLLED PRIVATE DELIVERY CHANNEL; ACCESS LIMITED TO AUTHORIZED OPERATORS; RECIPIENT IDENTITY VERIFIED; DELIVERY EVENT AND RECEIPT RECORDED; EXACT PROVIDER / CHANNEL TO BE SELECTED BEFORE FIRST EXTERNAL CUSTOMER DELIVERY`

### P5-C3 — Commercial Approval Authority

- Decision: the named role that authorizes commercial permission and business approval.
- Current status: `OWNER APPROVED`.
- Current placeholder: `<COMMERCIAL_APPROVAL_AUTHORITY>` (Phase 5.3 placeholder register).
- Recommendation: `ALOT`.
- Rationale: `ALOT` is already the approved governance identity and the current operation is single-owner / single-operator.
- Boundary: this role does not grant private signing-key access, does not make draft legal terms legally final, and does not substitute for professional legal review.
- Exact owner-response field: `P5-C3 COMMERCIAL APPROVAL AUTHORITY: ALOT`

### P5-C4 — Licensing Operator / Authorized License Issuer

- Decision: the named operator authorized to perform the approved offline license-issuance procedure.
- Current status: `OWNER APPROVED`.
- Current placeholder: `<LICENSING_OPERATOR>` (Phase 5.3 placeholder register).
- Recommendation: `ALOT initially`.
- Scope and limitations:
  - authorized only to perform the approved offline license-issuance process after the required commercial and technical authorization;
  - must use the approved signing process and authorized `keyId`;
  - does not receive authority to alter runtime licensing rules;
  - does not independently authorize commercial permission;
  - no new key access is granted merely by this governance approval;
  - no key generation occurs;
  - no signing operation occurs;
  - no customer license is issued by this packet;
  - separation of duties remains `REQUIRED WHERE PRACTICAL`.
- Rationale: `ALOT` is the current approved governance and operational identity, so `ALOT initially` is an accurate starting assignment scoped to the approved process.
- Exact owner-response field: `P5-C4 LICENSING OPERATOR / AUTHORIZED LICENSE ISSUER: ALOT initially`

### P5-C5 — Supported-version duration

- Decision: whether a fixed calendar support term is committed.
- Current status: `OWNER APPROVED — POLICY`.
- Approved policy: `NO FIXED CALENDAR TERM; FUTURE COMMERCIAL / LEGAL POLICY REQUIRED`.
- Preserved supported-version classification: current release `SUPPORTED`; immediately previous MINOR release `SECURITY / CRITICAL FIX SUPPORT where practical`; older releases `UNSUPPORTED unless a commercial agreement explicitly says otherwise`.
- Recorded policy: `FORMAL CALENDAR SUPPORT DURATION: NOT COMMITTED UNTIL COMMERCIAL / LEGAL SUPPORT POLICY IS APPROVED`.
- Rationale: no fixed number of months or years has received commercial or legal approval; this avoids accidentally creating a support warranty or contractual promise; the existing version-state model can operate without a fixed calendar term; a later approved commercial/support policy may define a calendar period.
- Boundary: do not invent a 12-month, 24-month, or other support term.
- Exact owner-response field: `P5-C5 FORMAL SUPPORTED-VERSION DURATION: NO FIXED CALENDAR TERM; FUTURE COMMERCIAL / LEGAL POLICY REQUIRED`

### P5-C6 — LTS policy

- Decision: whether any release is designated LTS.
- Current status: `OWNER APPROVED — POLICY`.
- Approved policy: `NO LTS DESIGNATION UNTIL SEPARATELY APPROVED`.
- Rationale: no release should currently be described as LTS; LTS requires an explicit servicing duration, maintenance obligations, support capacity, commercial terms, and legal/business approval; the ordinary supported-version model remains available without making an LTS commitment.
- Boundary: do not designate any release as LTS.
- Exact owner-response field: `P5-C6 LTS POLICY: NO LTS DESIGNATION UNTIL SEPARATELY APPROVED`

## 4. Already resolved — do not reopen

### Contacts

- Security contact: `chriltola.khmer@gmail.com`
- Commercial / evaluation contact: `chriltola.khmer@gmail.com`
- Shared-mailbox exception: `OWNER APPROVED`

### Governance identity

- Official governance/release commit identity: `ALOT <chriltola.khmer@gmail.com>`

### Security Response Owner

- `ALOT`

### Release governance

- Release Operator: `ALOT`
- Release Approval Authority: `ALOT` initially
- Distribution Operator: `ALOT` initially
- separation of duties: `REQUIRED WHERE PRACTICAL`

### Phase 5.4

- P54-D1 through P54-D6 are `OWNER APPROVED`. Not reopened by this packet.

### Phase 5.6

- Left completely unchanged in its platform-blocked state. Not modified, not reopened, and not revisited by this packet.

### Placeholder classification

| Placeholder / phrase | Classification |
| --- | --- |
| `<COPYRIGHT_OWNER>` in `COPYRIGHT.md` | Current unresolved value — see P5-C1 |
| `<APPROVED_DELIVERY_CHANNEL>` in the Phase 5.3 workflow and release summary | Owner-approved policy; exact provider/channel remains a prerequisite before first external customer delivery |
| `<COMMERCIAL_APPROVAL_AUTHORITY>` in the Phase 5.3 placeholder register | Owner-approved assignment: `ALOT` |
| `<LICENSING_OPERATOR>` in the Phase 5.3 placeholder register | Owner-approved assignment: `ALOT initially` |
| `PENDING — COMMERCIAL / SUPPORT POLICY DECISION` (LTS, support duration) | Superseded by the owner-approved policies in P5-C5 and P5-C6 |
| `OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE` (release-register storage) | Current pre-release operational prerequisite — see section 6 |
| `TO BE SELECTED BEFORE FIRST REAL RELEASE` (manifest storage provider/location) | Current pre-release operational prerequisite — see section 6 |
| `<SECURITY_CONTACT>` references in Phase 5.3 / Phase 5.5 | Historical text — the value is now resolved; do not reopen |
| `<COMMERCIAL_CONTACT>` references in Phase 5.5 | Historical text — the value is now resolved; do not reopen |
| `MECHANISM UNRESOLVED` for commit signing in Phase 5.5 | Historical text — SSH commit signing is now implemented; do not reopen |
| `BLOCKED — UNRESOLVED ASSIGNMENT` rows in Phase 5.7 section 18 | Already reconciled by the Phase 5.7 reconciliation commit; historical |
| Unrelated `unresolved` hits in Phase 2 / Phase 4 records | Historical planning text; out of scope |

## 5. Professional legal/business review still pending

- Phase 5.2 professional legal review
- Phase 5.3 professional legal/business review
- copyright / legal wording review
- evaluation terms review
- commercial terms review
- legal / contractual retention review

`Owner approval establishes internal governance/business intent only and does not substitute for professional legal review.`

## 6. Deferred / future operational items

- exact delivery-channel provider
- exact manifest-storage provider / location: `TO BE SELECTED BEFORE FIRST REAL RELEASE`
- exact release-register storage product / location: `OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`
- release-tag signing: `UNRESOLVED / NOT IMPLEMENTED`
- manifest signing: `DEFERRED`
- artifact / code signing: `DEFERRED / NOT IMPLEMENTED`
- SBOM: `DEFERRED / FUTURE GOVERNANCE`
- runtime build metadata: `DEFERRED`
- cross-host reproducibility: `NOT YET VERIFIED`

## 7. Current-state reconciliation

The Phase 5.3 contact values are already owner-approved and implemented, including the shared-mailbox exception. P5-C2 through P5-C6 are now owner-approved as recorded in this packet. P5-C1 remains the only exact owner-supplied identity value required; the exact delivery provider/channel and other operational, signing, storage, and professional-review items remain deferred or pending as listed above.

## 8. Non-authorizations

This packet does not authorize any of the following:

- customer transaction
- customer source access
- commercial agreement
- customer-license issuance
- private-key use
- license signing
- release
- release ID creation
- tag
- GitHub Release
- Release Manifest production
- artifact publication
- deployment
- public-repository conversion
- legal terms
- LTS commitment
- source / runtime / CI changes
- GitHub setting changes
- Phase 5.6 changes

## 9. Approved Owner Decision Record / Remaining Owner Value

```text
P5-C1 COPYRIGHT / RIGHTS-HOLDER IDENTITY:
P5-C2 APPROVED DELIVERY CHANNEL POLICY: VENDOR-CONTROLLED PRIVATE DELIVERY CHANNEL; ACCESS LIMITED TO AUTHORIZED OPERATORS; RECIPIENT IDENTITY VERIFIED; DELIVERY EVENT AND RECEIPT RECORDED; EXACT PROVIDER / CHANNEL TO BE SELECTED BEFORE FIRST EXTERNAL CUSTOMER DELIVERY
P5-C3 COMMERCIAL APPROVAL AUTHORITY: ALOT
P5-C4 LICENSING OPERATOR / AUTHORIZED LICENSE ISSUER: ALOT initially
P5-C5 FORMAL SUPPORTED-VERSION DURATION: NO FIXED CALENDAR TERM; FUTURE COMMERCIAL / LEGAL POLICY REQUIRED
P5-C6 LTS POLICY: NO LTS DESIGNATION UNTIL SEPARATELY APPROVED
```

P5-C2 through P5-C6 are recorded as `OWNER APPROVED`. P5-C1 remains intentionally blank and `OWNER VALUE REQUIRED`.

## 10. Packet status

`P5-C2 THROUGH P5-C6 OWNER APPROVED — P5-C1 COPYRIGHT / RIGHTS-HOLDER IDENTITY REMAINS OWNER VALUE REQUIRED — NO COMMERCIAL OPERATION AUTHORIZED`