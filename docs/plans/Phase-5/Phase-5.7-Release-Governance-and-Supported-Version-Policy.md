# Phase 5.7 — Release Governance and Supported-Version Policy

> DOCUMENTATION / GOVERNANCE SUBPHASE — OWNER APPROVED — 2026-09-12

Status: PHASE 5.7 — OWNER APPROVED. The project owner approved the completed Phase 5.7 documentation on 2026-09-12. The documentation acceptance criteria remain satisfied. This is a documentation-only closeout and does not resolve any open, pending, blocked, or deferred operational decisions. It does not authorize a release, a Git tag, artifact publication, deployment, repository visibility or ownership change, signing configuration or key generation, production or legal license creation or issuance, branch-protection or ruleset changes, or customer distribution.

Phase 5.6 remains: IMPLEMENTATION IN PROGRESS. Its remote GitHub governance controls are CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION. Phase 5.6 is not closed or overridden by this document.

## 1. Objective

Define the vendor-controlled release governance and supported-version policy for LabAuthServer without performing an actual release.

This subphase establishes, as documentation only:

1. what qualifies as an official vendor release
2. a versioning policy
3. a Git tag policy
4. a supported-version policy
5. release artifact requirements
6. Release Manifest V1 integration
7. release approval roles
8. a release checklist and gates
9. release rollback / withdrawal handling
10. security-release handling
11. customer-facing release expectations
12. the distinction between official releases and arbitrary source builds

No actual release is authorized, attempted, prepared, tagged, published, or deployed by this phase.

## 2. Scope

This phase covers:

- official vendor release definition
- versioning policy
- Git tag governance
- supported version lifecycle
- release artifact set
- Release Manifest V1 integration
- release approval gates
- release operator responsibilities
- release register
- withdrawal / supersession
- security releases
- customer communication
- release verification

## 3. Non-goals

This phase explicitly excludes:

- creating a GitHub Release
- creating Git tags
- publishing artifacts
- deploying
- making the repository public
- changing runtime licensing
- generating signing keys
- implementing online activation
- introducing billing or a customer portal
- creating final legal terms

## 4. Official Vendor Release Definition

An official vendor release is a release that satisfies **all** of the following:

- produced through a vendor-approved process
- tied to an exact Git commit
- tied to an explicit product version
- associated with successful approved CI
- represented by a Release Manifest V1
- recorded in a vendor-controlled release register
- approved by an authorized release operator
- distributed only through an approved channel

An official release designation is a governance designation. It does not by itself cryptographically prove publisher identity unless a signing mechanism is later approved and implemented.

### Source builds

Source builds made by customers or other parties are **not automatically malicious**. They are simply not official vendor releases unless separately approved through the same vendor-controlled process.

The distinction is about provenance and approval, not about intent or quality.

## 5. Versioning Policy

Product versions use a semantic-style scheme:

```
MAJOR.MINOR.PATCH
```

Generic examples only (not actual product releases): `1.0.0`, `1.1.0`, `1.1.1`.

Component meaning:

| Component | Meaning |
| --- | --- |
| `MAJOR` | Breaking or materially incompatible changes. |
| `MINOR` | Backward-compatible feature additions or substantial supported functionality. |
| `PATCH` | Backward-compatible fixes, security corrections, and maintenance changes. |

Pre-release identifiers (for example `-rc1`, `-beta1`) may be reserved for future use but must not be used without separate approval.

This document does **not** declare an actual product release version and does **not** create any version tag.

## 6. Git Tag Policy

Future official release tags use the form:

```
vMAJOR.MINOR.PATCH
```

Generic example only: `v1.2.3`.

Rules:

- official release tags must point to the exact approved release commit
- tags must not be moved after publication
- force-retagging an official release is prohibited
- corrected releases receive a new version
- tag creation requires release approval
- release tags must eventually follow the approved signing policy once signing governance is resolved

Important: commit/tag signing is currently **unresolved**. Signing is **not** implemented. This document does not pretend signing is implemented and does not create any tag.

## 7. Supported-Version Policy

Initial supported-version model:

| Release state | Support classification |
| --- | --- |
| Current release | SUPPORTED |
| Immediately previous MINOR release | SECURITY / CRITICAL FIX SUPPORT where practical |
| Older releases | UNSUPPORTED unless a commercial agreement explicitly says otherwise |

No fixed calendar support period is promised at this time.

Formal LTS duration: `PENDING — COMMERCIAL / SUPPORT POLICY DECISION`.

Clarification: a valid signed commercial license does **not** automatically imply entitlement to support for any specific software version. License validity and version support entitlement are separate concerns.

## 8. Release Artifact Requirements

The release artifact set is defined conceptually as:

| Item | Requiredness |
| --- | --- |
| application/package artifact | Required |
| Release Manifest V1 | Required |
| SHA-256 artifact digest | Required |
| artifact size | Required |
| version | Required |
| Git SHA | Required |
| build timestamp | Required |
| CI run reference | Required |
| release operator | Required |
| release notes | Required |
| supported-version status | Required |

Future optional items — marked accurately as **future / not implemented**:

| Item | Status |
| --- | --- |
| detached manifest signature | FUTURE — NOT IMPLEMENTED |
| platform / code signing | FUTURE — NOT IMPLEMENTED |
| SBOM | RECOMMENDED FOR FUTURE RELEASE GOVERNANCE — NOT IMPLEMENTED |

This phase does **not** generate any artifact.

## 9. Release Manifest V1 Integration

Phase 5.7 consumes the Phase 5.4 Release Manifest V1 design as the official release provenance record. It does **not** redesign the schema.

Preserved from Phase 5.4:

- lowercase hexadecimal SHA-256, exactly 64 characters, no prefix or separators
- exact artifact size in bytes
- normalized relative artifact paths using `/`
- no absolute paths and no drive letters
- no path traversal (`..`) segments
- no manifest self-hash (a manifest must not contain its own digest)
- trusted-channel limitation: an unsigned manifest establishes integrity relative to a trusted channel, not publisher identity

Any conflict between this document and the Phase 5.4 schema must be resolved in favor of Phase 5.4 unless Phase 5.4 is explicitly superseded.

## 10. Release Register

A vendor-controlled external release register is required. Minimum fields:

- release version
- release tag
- source Git SHA
- CI run
- release manifest digest (if externally recorded)
- artifact digest(s)
- release date/time
- release operator
- approval authority
- distribution state
- withdrawn / superseded status where applicable

The register must not store private keys, signing secrets, passwords, credentials, or customer licenses. This document does not invent operator names; unresolved identities remain placeholders or statuses.

## 11. Release Approval Gates

A release gate requires all of:

- exact source revision identified
- required CI successful
- release manifest generated and reviewed
- artifact hashes verified
- no production private signing material exposed
- licensing regression confidence established
- security review appropriate to release risk
- release notes prepared
- supported-version classification assigned
- release operator identified
- approval authority identified
- distribution channel approved
- repository / public-release decision compatible with current policy

Current unresolved identities remain unresolved. This document does not invent approvers, operators, or authorities.

## 12. Security Release Policy

- security fixes may use expedited governance
- expedited does not mean bypassing integrity/provenance controls
- sensitive vulnerability details may be withheld until remediation is available
- a security release must still have a version, source SHA, CI evidence, manifest, and approval
- the security contact is currently unresolved

`<SECURITY_CONTACT>` remains unresolved. This document does not invent a security contact.

## 13. Release Withdrawal / Supersession

A release may be marked:

- `CURRENT`
- `SUPERSEDED`
- `WITHDRAWN`

Rules:

- withdrawal does not erase Git history
- withdrawal does not silently replace artifacts
- a withdrawn artifact must not be replaced in place with different bytes under the same version
- a corrected release must receive a new version
- the reason must be documented in the release register

## 14. Customer Distribution Boundary

The controlled-distribution model is preserved.

Official production artifacts may only be delivered through an approved vendor distribution channel.

Current placeholder remains:

```
<APPROVED_DELIVERY_CHANNEL>
```

The channel is not invented here. Nothing is published by this phase.

## 15. Release Roles

Roles are defined conceptually:

| Role | Responsibility |
| --- | --- |
| Release Operator | Builds, prepares, and records the release under the approved process. |
| Release Approval Authority | Reviews and approves the release gate. |
| Security Response Owner | Owns security-release coordination and vulnerability handling. |
| Licensing Operator | Owns commercial license issuance and license/version compatibility. |

Existing unresolved placeholders apply. No names are assigned.

Least privilege and separation of duties should be applied where practical; the exact separation remains an owner decision.

## 16. Release Checklist

This is a future operational checklist. Every step is documentation only. It is **not** executed by this phase.

### PRE-RELEASE

- [ ] confirm repository remains private and policy-compliant
- [ ] confirm supported-version classification for this release
- [ ] prepare release notes draft
- [ ] identify release operator and approval authority

### BUILD / PROVENANCE

- [ ] select and record exact source Git SHA
- [ ] run approved CI to successful conclusion
- [ ] record CI run reference
- [ ] record build timestamp (UTC), configuration, target framework

### VALIDATION

- [ ] calculate artifact SHA-256 (lowercase hex) and exact byte size
- [ ] generate Release Manifest V1
- [ ] review manifest against schema and artifact set
- [ ] verify licensing regression confidence
- [ ] perform security review appropriate to release risk

### APPROVAL

- [ ] confirm no production private signing material exposed
- [ ] confirm release gate satisfied
- [ ] record approval authority decision
- [ ] record approval timestamp

### DISTRIBUTION

- [ ] confirm approved distribution channel
- [ ] record distribution state in the release register
- [ ] deliver only through the approved channel

### POST-RELEASE

- [ ] record release in the release register with final digests
- [ ] publish release notes through the approved channel
- [ ] record supported-version status
- [ ] monitor for security or withdrawal events

## 17. Rollback / Recovery

- repository history is never rewritten as a release rollback
- bad releases are superseded or withdrawn, not silently replaced
- corrected artifacts get a new version
- customer rollback must use an already-approved known artifact
- the release register records the event
- license compatibility must be considered during rollback

## 18. Open Decisions / Blockers

| Decision / Item | Status |
| --- | --- |
| formal supported-version duration | OPEN — COMMERCIAL / SUPPORT POLICY DECISION |
| LTS policy | PENDING — COMMERCIAL / SUPPORT POLICY DECISION |
| release operator assignment | BLOCKED — UNRESOLVED ASSIGNMENT |
| release approval authority assignment | BLOCKED — UNRESOLVED ASSIGNMENT |
| approved distribution channel (`<APPROVED_DELIVERY_CHANNEL>`) | BLOCKED — UNRESOLVED VALUE |
| commit/tag signing mechanism | BLOCKED — UNRESOLVED SIGNING MECHANISM |
| security contact (`<SECURITY_CONTACT>`) | BLOCKED — UNRESOLVED VALUE |
| commercial contact (`<COMMERCIAL_CONTACT>`) | BLOCKED — UNRESOLVED VALUE |
| SBOM requirement | DEFERRED — RECOMMENDED FUTURE WORK |
| manifest-signing decision | DEFERRED |
| platform/code-signing decision | DEFERRED |
| stable vendor-controlled commit identity | BLOCKED — UNRESOLVED VALUE |
| repository-admin assignments | BLOCKED — UNRESOLVED ASSIGNMENTS |
| Security Response Owner | BLOCKED — UNRESOLVED ASSIGNMENT |

No value is invented for any of these items.

## 19. Acceptance Criteria

For this closeout, the Phase 5.7 documentation acceptance criteria are satisfied:

- official release definition is documented
- versioning policy is documented
- tag policy is documented
- support lifecycle is documented
- Release Manifest V1 relationship is documented
- release gate is documented
- release register requirements are documented
- withdrawal/supersession policy is documented
- security release policy is documented
- unresolved owner decisions are explicit

No actual release is required for Phase 5.7 documentation completion.

## 20. Consistency and Dependencies

This phase builds on and must not contradict:

- Phase 5.4 — Official Build Provenance and Release Manifest Design
- Phase 5.5 — Repository Security and Contribution Governance
- Phase 5.6 — Governance Control Implementation

Preserved official-build principles:

- known source revision
- version
- Git SHA
- build timestamp
- CI reference
- artifact filename
- artifact size
- SHA-256
- release operator
- Release Manifest V1
- checksum proves integrity relative to a trusted manifest
- checksum alone does **not** prove publisher identity
- reproducibility remains `NOT YET VERIFIED`
- SBOM remains recommended future governance work
- private signing keys must never enter the repository or CI

## 21. Security and Legal Boundary

This document introduces no private keys, passwords, tokens, credentials, production signing material, customer licenses, or customer secrets. It does not alter licensing runtime behavior and does not assert final legal terms.

Professional legal review remains `PENDING — PROFESSIONAL LEGAL REVIEW` where applicable.

## 22. Current Status

PHASE 5.7 — OWNER APPROVED. Documentation and governance only. The project owner approved this documentation on 2026-09-12, and all ten acceptance criteria in section 19 remain satisfied; no actual release is required for this closeout. Approval does not resolve the open, pending, blocked, or deferred decisions in section 18.

- No Git tag created.
- No GitHub Release created.
- No artifact published.
- No deployment performed.
- No repository visibility change.
- No repository ownership transfer.
- No signing configuration change.
- No signing key generated.
- No production or legal license created or issued.
- No branch-protection or ruleset change.
- No runtime licensing change.

The repository remains private. Phase 5.6 remains IN PROGRESS with its remote controls CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION. Phase 5.7 does not close or override Phase 5.6.

The Phase 5.7 documentation closeout commit `ee3ab733a764a3e2c32009d4806d43ca5b5559d5` was validated by GitHub Actions workflow `LabAuthServer CI`, run `#14` (run ID `34681947100`), with conclusion `SUCCESS`. This records CI execution success only; it does not establish branch-protection or required-status-check enforcement on `main`.