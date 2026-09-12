# Phase 5.4 - Remaining Owner Decision Packet

> DOCUMENTATION-ONLY APPROVAL RECORD — PHASE 5.4 P54-D1 THROUGH P54-D6 ARE OWNER APPROVED; NO RELEASE ACTION AUTHORIZED

## 1. Purpose

This packet records the project owner's explicit approval of the remaining Phase 5.4 operating-policy decisions P54-D1 through P54-D6. The Phase 5.4 design itself remains owner-approved and same-host clean publish repeatability is verified; these six items are governance policy choices, not technical implementation work and not release actions. This packet does not authorize a real release, manifest artifact, code signing, repo setting change, deployment, or source/runtime change.

## 2. Verified baseline

| Item | Verified value |
| --- | --- |
| Branch | `main` |
| Current Git SHA | `b7d0408b1ecc7a284d1a2280990476bb262ce72f` |
| Current HEAD commit | `Finalize Phase 5.4 retention recommendation` |
| Hosted validation | `LabAuthServer CI`, run `#33`, run ID `34696584613`, head `b7d0408b1ecc7a284d1a2280990476bb262ce72f`, status `completed`, conclusion `success` |
| GitHub signature verification | `verified: true`, reason `valid` |
| Same-host reproducibility | `VERIFIED — SAME-HOST CLEAN PUBLISH REPEATABILITY` |
| Cross-host / cross-environment reproducibility | `NOT YET VERIFIED` |
| Repository visibility | `PRIVATE` |
| Release existence | `NO RELEASE EXISTS` |
| Release-authorizing action | `NOT AUTHORIZED` |
| Phase 5.6 status | `FROZEN / UNCHANGED` per the documented Phase 5.6 boundary |

The baseline above is the independently verified prerequisite state for this approval record. The packet below records the owner-approved decisions and the still-deferred items, without implementing them.

## 3. Owner decision matrix

### P54-D1 — Release identifier format

- Decision: Release identifier format for approved formal releases.
- Current status: `OWNER APPROVED` — 2026-09-12.
- Approved value: `LAS-vMAJOR.MINOR.PATCH-<shortsha>`.
- Rules: human-readable; tied to the product version; tied to the source SHA; immutable once issued; no mutable branch dependency; the release timestamp remains separate metadata. No actual release ID is created by this record.
- Rationale: This format is simple, deterministic, immutable once issued, tied to the product version and exact source revision, non-secret, and does not depend on mutable branch names or local dates. It is easier to audit than a date-based identifier and remains stable for evidence, support, and register matching.
- Tradeoffs: A date-based identifier can be useful for operational chronology, but it adds an unnecessary mutable business dimension to a format that is already tied to a version and commit. If a later operational need for a date is established, it can be represented in a separate release timestamp field rather than in the ID itself.
- Approved owner-decision record: `P54-D1 RELEASE IDENTIFIER FORMAT: LAS-vMAJOR.MINOR.PATCH-<shortsha>`

### P54-D2 — Release channel taxonomy

- Decision: Release channel taxonomy to use for formal release governance.
- Current status: `OWNER APPROVED` — 2026-09-12.
- Approved taxonomy: `Internal`, `Evaluation`, `Production`. This taxonomy is frozen for the current Phase 5 release-governance model.
- Preserved rule: a channel label does not itself grant legal, commercial, evaluation, or production permission.
- Rationale: The taxonomy is already documented, understandable, and operationally sufficient for non-production and production release governance. Freezing it preserves clarity and avoids creating additional channels before an actual release process exists. The label remains informational only; it does not grant commercial or legal authorization.
- Tradeoffs: Adding more categories could reflect nuanced operational needs later, but it increases governance complexity and introduces ambiguity before a real release process is running. Preserving the current taxonomy avoids overengineering and keeps the model aligned with the current design.
- Approved owner-decision record: `P54-D2 RELEASE CHANNEL TAXONOMY: Internal / Evaluation / Production (freeze current taxonomy)`

### P54-D3 — Manifest storage / custody model

- Decision: Policy for manifest storage and custody.
- Current status: `OWNER APPROVED — POLICY` — 2026-09-12.
- Approved policy: vendor-controlled private storage; access limited to authorized release operators; immutable or append-preserving history where practical; the manifest is associated with the exact release ID, Git SHA, and artifact hashes; no private keys; no credentials; no customer licenses; customer-facing copies are delivered only through the approved distribution process.
- Preserved future operational value: `LOCATION / PROVIDER TO BE SELECTED BEFORE FIRST REAL RELEASE`. No provider is invented or created, and no storage is created, by this record.
- Rationale: A manifest is evidence, not a general document store. It should be kept in a controlled private archive or vendor-managed repository area accessible only to the named release operators and approvers, while preserving a traceable history and retaining the manifest with the release evidence. This keeps provenance intact without introducing a new public or third-party service.
- Tradeoffs: Private vendor storage is more operationally controlled than a shared or public venue, but it requires disciplined access control and retention. The policy intentionally defers the exact provider/location to a future pre-release operational step.
- Approved owner-decision record: `P54-D3 MANIFEST STORAGE POLICY: VENDOR-CONTROLLED PRIVATE STORAGE; ACCESS LIMITED TO AUTHORIZED RELEASE OPERATORS; LOCATION / PROVIDER TO BE SELECTED BEFORE FIRST REAL RELEASE`

### P54-D4 — External release register storage

- Decision: Policy for the external vendor-controlled release register.
- Current status: `OWNER APPROVED — POLICY` — 2026-09-12.
- Approved policy: private/vendor-controlled; append-preserving; access-controlled; backed up; auditable; separate from the application database; no private keys; no credentials; no customer licenses.
- Preserved future pre-release operational value: `STORAGE PRODUCT / LOCATION = OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`. This is intentionally a future pre-release operational prerequisite, not an unresolved policy decision. No storage product is invented or created by this record.
- Rationale: The policy requirements are clear and stable. The platform choice should be deferred until a first real release requires the exact storage product or location, without delaying the governance policy.
- Tradeoffs: Settling the product/location too early risks choosing a system that is not operationally suitable for the real release environment. Deferring the exact vendor location preserves policy readiness without inventing a solution before it is needed.
- Approved owner-decision record: `P54-D4 RELEASE REGISTER STORAGE POLICY: POLICY APPROVED; STORAGE PRODUCT / LOCATION = OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`

### P54-D5 — Release-record retention

- Decision: Operational retention policy for release records.
- Current status: `OWNER APPROVED — OPERATIONAL POLICY` — 2026-09-12.
- Approved value: `RETAIN FOR SUPPORTED LIFETIME AND INDEFINITELY THEREAFTER UNTIL SUPERSEDED BY APPROVED LEGAL/BUSINESS RETENTION POLICY`.
- Preserved: `LEGAL / CONTRACTUAL RETENTION REQUIREMENT = PROFESSIONAL REVIEW PENDING`.
- Clarifications: this is an operational preservation rule, not legal advice; a later approved legal/business retention policy may supersede it; the rule concerns release-governance and provenance records; and it does not require retention of private keys, credentials, customer licenses, or unrelated customer data.
- Rationale: The repository does not contain a professional legal review establishing a binding legal retention period. An operational retention policy is therefore appropriate now, while acknowledging that future legal or contractual policy may supersede it. This is conservative, auditable, and realistic for a controlled vendor process without pretending the legal answer is known.
- Tradeoffs: A shorter fixed retention window may be easier to administer but risks destroying provenance evidence before a legal or business requirement is finalized. Extending preservation indefinitely for the minimal governance records is a conservative default while the legal/business policy remains pending.
- Approved owner-decision record: `P54-D5 RELEASE RECORD RETENTION: RETAIN FOR SUPPORTED LIFETIME AND INDEFINITELY THEREAFTER UNTIL SUPERSEDED BY APPROVED LEGAL/BUSINESS RETENTION POLICY; LEGAL / CONTRACTUAL RETENTION REQUIREMENT = PROFESSIONAL REVIEW PENDING`

### P54-D6 — Release approval / separation of duties

- Decision: Release roles and separation of duties for release and security operations.
- Current status: `OWNER APPROVED` — 2026-09-12.
- Approved assignments: Release Operator `ALOT`; Release Approval Authority `ALOT` initially; Distribution Operator `ALOT` initially; Security Response Owner `ALOT`; separation of duties `REQUIRED WHERE PRACTICAL`; second-person review required when another authorized reviewer is available; if no second authorized reviewer exists, the absence of independent review must be explicitly recorded.
- Two-person control: not claimed. The current record does not pretend that independent review exists when only one named operator is available. No GitHub permission is changed by this record.
- Rationale: The existing owner-approved governance identity is the stable named operator and security response owner. This is a realistic starting state that does not pretend to provide independent review where it does not exist. The policy explicitly requires independent review when a second authorized reviewer is available.
- Tradeoffs: A single-person model is operationally simpler but weaker for independent approval. The policy reflects the current operational reality while preserving the principle that independent review should be used where practical, without inventing nonexistent staffing.
- Approved owner-decision record: `P54-D6 RELEASE ROLES / SEPARATION OF DUTIES: Release Operator = ALOT; Release Approval Authority = ALOT initially; Distribution Operator = ALOT initially; Security Response Owner = ALOT; separation of duties = REQUIRED WHERE PRACTICAL; second-person review only when another authorized reviewer is available; absence of second reviewer MUST BE RECORDED`

## 4. Deferred items

These items remain deferred and are not approved for implementation by this packet:

- Manifest signing — `DEFERRED`
- Artifact / code signing — `DEFERRED / NOT IMPLEMENTED`
- SBOM — `DEFERRED / FUTURE GOVERNANCE`
- Runtime build metadata embedding — `DEFERRED`
- Cross-host reproducibility — `NOT YET VERIFIED`

These are governance items requiring later owner decision or approval and are intentionally not implemented in this task.

## 5. External dependencies

The following dependencies remain unresolved or outside the scope that Phase 5.4 can finalize alone:

- `<APPROVED_DELIVERY_CHANNEL>`
- commercial approval authority
- licensing operator
- formal supported-version duration
- LTS policy
- professional legal / business review
- release-manifest signing and code-signing governance
- any future customer-distribution authorization

These are not resolved by this packet unless an exact owner-approved value already exists elsewhere in the project and is explicitly preserved.

## 6. Non-authorizations

This packet does not authorize any of the following:

- release creation
- tags
- GitHub Release
- artifact publication
- deployment
- release-manifest production
- source / runtime changes
- CI changes
- key or certificate generation
- repository visibility / ownership changes
- repository permission or collaborator changes
- Phase 5.6 changes

This packet is limited to governance decision preparation only.

## 7. Approved Owner Decision Record

The project owner explicitly approved all P54-D1 through P54-D6 recommended decisions. The approved values are recorded below. This is an approval record, not a pending response form.

```text
P54-D1 RELEASE IDENTIFIER FORMAT: LAS-vMAJOR.MINOR.PATCH-<shortsha>
P54-D2 RELEASE CHANNEL TAXONOMY: Internal / Evaluation / Production (freeze current taxonomy)
P54-D3 MANIFEST STORAGE POLICY: VENDOR-CONTROLLED PRIVATE STORAGE; ACCESS LIMITED TO AUTHORIZED RELEASE OPERATORS; LOCATION / PROVIDER TO BE SELECTED BEFORE FIRST REAL RELEASE
P54-D4 RELEASE REGISTER STORAGE POLICY: POLICY APPROVED; STORAGE PRODUCT / LOCATION = OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE
P54-D5 RELEASE RECORD RETENTION: RETAIN FOR SUPPORTED LIFETIME AND INDEFINITELY THEREAFTER UNTIL SUPERSEDED BY APPROVED LEGAL/BUSINESS RETENTION POLICY; LEGAL / CONTRACTUAL RETENTION REQUIREMENT = PROFESSIONAL REVIEW PENDING
P54-D6 RELEASE ROLES / SEPARATION OF DUTIES: Release Operator = ALOT; Release Approval Authority = ALOT initially; Distribution Operator = ALOT initially; Security Response Owner = ALOT; separation of duties = REQUIRED WHERE PRACTICAL; second-person review only when another authorized reviewer is available; absence of second reviewer MUST BE RECORDED
```

Approving these decisions does not select a storage provider or location, does not create a release ID, does not create a release, and does not change any GitHub setting.

## 8. Follow-up reconciliation required

The following Phase 5.7 wording requires later narrow reconciliation because it has become stale relative to the later verified state:

- commit signing described as unresolved even though SSH commit signing is now implemented;
- security contact described as unresolved even though D4 is implemented;
- Security Response Owner described as unnamed even though D7 is implemented;
- release tag signing should not be treated as resolved merely because commit signing exists;
- manifest signing remains unresolved;
- artifact / code signing remains unresolved;
- no actual release is authorized.

These should be recorded as `FOLLOW-UP RECONCILIATION REQUIRED` in a later documentation-only task and are not rewritten here.

## 9. Packet status

`PHASE 5.4 P54-D1 THROUGH P54-D6 OWNER APPROVED — NO RELEASE ACTION AUTHORIZED`
