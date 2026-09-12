# Phase 5.4 - Remaining Owner Decision Packet

> DOCUMENTATION-ONLY DECISION PREPARATION — PHASE 5.4 DESIGN IS OWNER-APPROVED; SIX OPERATING-POLICY DECISIONS REMAIN

## 1. Purpose

This packet prepares the remaining owner-required Phase 5.4 operating-policy decisions for explicit approval. The Phase 5.4 design itself remains owner-approved and same-host clean publish repeatability is verified; the unresolved items are not technical implementation work and are not release actions. This packet does not authorize a real release, manifest artifact, code signing, repo setting change, deployment, or source/runtime change.

## 2. Verified baseline

| Item | Verified value |
| --- | --- |
| Branch | `main` |
| Current Git SHA | `b80374712e5fad9831379bfd22c418f17224eb66` |
| Current HEAD commit | `Verify Phase 5.4 build repeatability` |
| Hosted validation | `LabAuthServer CI`, run `#31`, run ID `34695872722`, head `b80374712e5fad9831379bfd22c418f17224eb66`, status `completed`, conclusion `success` |
| Same-host reproducibility | `VERIFIED — SAME-HOST CLEAN PUBLISH REPEATABILITY` |
| Cross-host / cross-environment reproducibility | `NOT YET VERIFIED` |
| Repository visibility | `PRIVATE` |
| Release existence | `NO RELEASE EXISTS` |
| Release-authorizing action | `NOT AUTHORIZED` |
| Phase 5.6 status | `FROZEN / UNCHANGED` per the documented Phase 5.6 boundary |

The baseline remains the verified owner-accepted state already recorded for the repository in this working branch. The packet below records only the remaining owner-required decisions and the deferred items, without implementing them.

## 3. Owner decision matrix

### P54-D1 — Release identifier format

- Decision: Release identifier format for approved formal releases.
- Current status: `OWNER DECISION REQUIRED`.
- Recommended choice: `LAS-vMAJOR.MINOR.PATCH-<shortsha>`.
- Rationale: This format is simple, deterministic, immutable once issued, tied to the product version and exact source revision, non-secret, and does not depend on mutable branch names or local dates. It is easier to audit than a date-based identifier and remains stable for evidence, support, and register matching.
- Tradeoffs: A date-based identifier can be useful for operational chronology, but it adds an unnecessary mutable business dimension to a format that is already tied to a version and commit. If a later operational need for a date is established, it can be represented in a separate release timestamp field rather than in the ID itself.
- Exact owner-approval field: `P54-D1 RELEASE IDENTIFIER FORMAT: LAS-vMAJOR.MINOR.PATCH-<shortsha>`

### P54-D2 — Release channel taxonomy

- Decision: Release channel taxonomy to use for formal release governance.
- Current status: `OWNER DECISION REQUIRED`.
- Recommended choice: Freeze the existing taxonomy as `Internal`, `Evaluation`, and `Production`.
- Rationale: The taxonomy is already documented, understandable, and operationally sufficient for non-production and production release governance. Freezing it preserves clarity and avoids creating additional channels before an actual release process exists. The label remains informational only; it does not grant commercial or legal authorization.
- Tradeoffs: Adding more categories could reflect nuanced operational needs later, but it increases governance complexity and introduces ambiguity before a real release process is running. Preserving the current taxonomy avoids overengineering and keeps the model aligned with the current design.
- Exact owner-approval field: `P54-D2 RELEASE CHANNEL TAXONOMY: Internal / Evaluation / Production (freeze current taxonomy)`

### P54-D3 — Manifest storage / custody model

- Decision: Policy for manifest storage and custody.
- Current status: `OWNER DECISION REQUIRED`.
- Recommended choice: Use vendor-controlled private storage limited to authorized release operators, with immutable-or-append-preserving retention where practical, and keep the manifest associated with the exact release ID, Git SHA, and artifact hashes. Store no private keys, credentials, or customer licenses with it. Customer-facing copies are distributed only through the approved vendor process.
- Rationale: A manifest is evidence, not a general document store. It should be kept in a controlled private archive or vendor-managed repository area accessible only to the named release operators and approvers, while preserving a traceable history and retaining the manifest with the release evidence. This keeps provenance intact without introducing a new public or third-party service.
- Tradeoffs: Private vendor storage is more operationally controlled than a shared or public venue, but it requires disciplined access control and retention. The recommendation intentionally avoids inventing a third-party service or location before owner approval.
- Exact owner-approval field: `P54-D3 MANIFEST STORAGE POLICY: VENDOR-CONTROLLED PRIVATE STORAGE; ACCESS LIMITED TO AUTHORIZED RELEASE OPERATORS; IMMEDIATE LOCATION / PROVIDER TO BE SELECTED BEFORE FIRST REAL RELEASE`

### P54-D4 — External release register storage

- Decision: Policy for the external vendor-controlled release register.
- Current status: `OWNER DECISION REQUIRED`.
- Recommended choice: Approve the policy requirements now and retain the exact platform / location value as `OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`.
- Rationale: The policy requirements are clear and stable: private/vendor-controlled, append-preserving, access-controlled, backed up, auditable, separate from the application database, and never containing private signing keys, credentials, or customer licenses. The platform choice should be deferred until a first real release requires the exact storage product or location, without delaying the governance policy.
- Tradeoffs: Settling the product/location too early risks choosing a system that is not operationally suitable for the real release environment. Deferring the exact vendor location preserves policy readiness without inventing a solution before owner approval.
- Exact owner-approval field: `P54-D4 RELEASE REGISTER STORAGE POLICY: POLICY APPROVED; STORAGE PRODUCT / LOCATION = OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`

### P54-D5 — Release-record retention

- Decision: Operational retention policy for release records.
- Current status: `OWNER DECISION REQUIRED`.
- Recommended choice: Adopt a conservative operational retention policy of `retain for the supported lifetime of the release plus a defined additional period`, with the explicit note that the exact legal / contractual retention period remains `LEGAL / CONTRACTUAL RETENTION REQUIREMENT — PROFESSIONAL REVIEW PENDING`.
- Rationale: The repository does not contain a professional legal review establishing a binding legal retention period. An operational retention policy is therefore appropriate now, while acknowledging that future legal or contractual policy may supersede it. This is conservative, auditable, and realistic for a controlled vendor process without pretending the legal answer is known.
- Tradeoffs: Indefinite retention is simple but may be unnecessarily expensive and broad; a short fixed multi-year period is simpler to operate but may conflict with a later legal requirement. A supported-lifetime-plus-buffer model is a prudent default until legal review is completed.
- Exact owner-approval field: `P54-D5 RELEASE RECORD RETENTION: OPERATIONAL RETENTION POLICY = SUPPORT LIFETIME PLUS DEFINED BUFFER; LEGAL / CONTRACTUAL RETENTION REQUIREMENT = PROFESSIONAL REVIEW PENDING`

### P54-D6 — Release approval / separation of duties

- Decision: Release roles and separation of duties for release and security operations.
- Current status: `OWNER DECISION REQUIRED`.
- Recommended choice: Use the already-approved governance identity `ALOT` for all core roles initially, while explicitly documenting that `separation of duties: REQUIRED WHERE PRACTICAL`, and that a production release should use independent review when another authorized reviewer is available. The current record must not falsely claim two-person control when only one named operator exists.
- Rationale: The existing owner-approved governance identity is the stable named operator and security response owner. The initial state is therefore `Release Operator = ALOT`, `Release Approval Authority = ALOT`, `Distribution Operator = ALOT`, `Security Response Owner = ALOT`. This is a realistic starting state and does not pretend to provide independent review where it does not exist. The policy explicitly requires independent review when a second authorized reviewer is available.
- Tradeoffs: A single-person model is operationally simpler but weaker for independent approval. The recommended policy reflects the current operational reality while preserving the principle that independent review should be used where practical, without inventing nonexistent staffing.
- Exact owner-approval field: `P54-D6 RELEASE ROLES / SEPARATION OF DUTIES: Release Operator = ALOT; Release Approval Authority = ALOT initially; Distribution Operator = ALOT initially; Security Response Owner = ALOT; separation of duties = REQUIRED WHERE PRACTICAL; second-person review only when another authorized reviewer is available; absence of second reviewer MUST BE RECORDED`

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

## 7. Owner response form

```text
P54-D1 RELEASE IDENTIFIER FORMAT: LAS-vMAJOR.MINOR.PATCH-<shortsha>
P54-D2 RELEASE CHANNEL TAXONOMY: Internal / Evaluation / Production (freeze current taxonomy)
P54-D3 MANIFEST STORAGE POLICY: VENDOR-CONTROLLED PRIVATE STORAGE; ACCESS LIMITED TO AUTHORIZED RELEASE OPERATORS; IMMEDIATE LOCATION / PROVIDER TO BE SELECTED BEFORE FIRST REAL RELEASE
P54-D4 RELEASE REGISTER STORAGE POLICY: POLICY APPROVED; STORAGE PRODUCT / LOCATION = OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE
P54-D5 RELEASE RECORD RETENTION: OPERATIONAL RETENTION POLICY = SUPPORT LIFETIME PLUS DEFINED BUFFER; LEGAL / CONTRACTUAL RETENTION REQUIREMENT = PROFESSIONAL REVIEW PENDING
P54-D6 RELEASE ROLES / SEPARATION OF DUTIES: Release Operator = ALOT; Release Approval Authority = ALOT initially; Distribution Operator = ALOT initially; Security Response Owner = ALOT; separation of duties = REQUIRED WHERE PRACTICAL; second-person review only when another authorized reviewer is available; absence of second reviewer MUST BE RECORDED
```

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

`PHASE 5.4 REMAINING OWNER DECISIONS PREPARED — NO RELEASE ACTION AUTHORIZED`
