# Phase 5.1 — Commercial Model and Decision Gates

Status: PHASE 5.1 — OWNER APPROVED. All eight decision gates were approved by the project owner on 2026-09-12. Professional legal review remains pending where applicable. Phase 5.2 implementation, external source distribution, public release, deployment, production key generation, and customer-license generation are not authorized by this closeout.

## 1. Purpose

Phase 4 provides LabAuthServer's technical licensing foundation: offline signed licenses, trusted public-key verification, explicit feature and edition policy, and fail-closed restricted behavior.

Phase 5 provides the commercial, source-distribution, release, and governance model around that technical foundation. Phase 5.1 resolves the decision gates that must be settled before later implementation work or external distribution.

No public release or deployment is authorized by this document. This document records recommendations for owner and legal review; it does not create final legal terms or silently approve a distribution model.

## 2. Current Repository State

The current repository state is:

- The GitHub repository is private.
- Phase 4 engineering is closed.
- Phase 4 hosted CI evidence is closed: run `34669800856` passed for remediation commit `48a8bccd7e1a72e8603be307060aa7fa4d109370`.
- The Phase 5 plan exists; Phase 5 implementation remains planning-only.
- No public source distribution is approved.
- No production commercial release is created by Phase 5.1.
- No final evaluation license, commercial license terms, or public-repository release is created by Phase 5.1.
- The existing Phase 4 offline signed-license architecture remains unchanged.

## 3. Business Objective

The intended business direction is a proprietary evaluation-to-commercial flow:

- LabAuthServer remains proprietary and is not an open-source project.
- Approved prospective customers may evaluate the product through a controlled vendor process.
- Evaluation may include source inspection, architecture review, internal demonstration, and non-production proof of concept under terms approved separately.
- Production, live, customer-facing, or business-operational use requires commercial permission and a vendor-issued signed commercial license.
- Unauthorized modification, redistribution, derivative commercial use, and commercial deployment are intended to be restricted through contractual and legal controls, subject to professional legal review.
- Official vendor builds remain distinguishable from arbitrary source builds through release governance and provenance records.

These are owner-approved business and governance directions, not approved legal wording. Professional legal review remains required for legal terms and restrictions.

## 4. Technical Boundary

A party that controls the source code, build process, and host can technically modify an unofficial build and bypass client-side licensing enforcement. Phase 5 must preserve this limitation and must not claim unbreakable DRM or technically impossible modification.

The intended protection model is:

- existing Phase 4 signed-license verification and fail-closed policy
- explicit edition and feature enforcement
- official-build provenance and release integrity information
- controlled distribution
- contractual and legal restrictions
- clear proprietary and evaluation-use notices

These controls support authenticity, governance, and legal enforcement. They do not make source-distributed software impossible to modify.

Phase 5.1 does not authorize anti-debugging, obfuscation-as-security, hardware fingerprinting, invasive anti-tamper, online activation, remote attestation, or any other expansion of the Phase 4 technical architecture.

## 5. Decision Gate Matrix

All eight decisions below were approved by the project owner on 2026-09-12. Legal approval is separate and remains pending where applicable. No row constitutes final legal wording.

| Gate | Recommended Decision | Alternatives | Reason | Approval Required | Status |
| --- | --- | --- | --- | --- | --- |
| Distribution model | Private / controlled source distribution initially | Public GitHub with proprietary terms; hybrid public summary with private source access | Controlled access better matches evaluation while limiting uncontrolled copying and exposure | Project owner; legal review before external distribution | OWNER APPROVED — LEGAL REVIEW PENDING WHERE APPLICABLE |
| Evaluation rights | Source inspection, internal demo, and non-production evaluation only; no general modification or redistribution right | Inspection only; narrowly authorized evaluator modifications | Preserves a useful evaluation path without granting broad production or derivative-use rights | Project owner and legal counsel | OWNER APPROVED — LEGAL REVIEW PENDING |
| Production licensing | Manual vendor contact and offline signed commercial license | Predefined packages; future portal or automation | Matches the existing offline-first Phase 4 architecture and vendor governance | Project owner; legal/business review | OWNER APPROVED — LEGAL REVIEW PENDING WHERE APPLICABLE |
| Official build trust | Vendor release provenance, immutable Git commit SHA, version, release manifest, build timestamp, CI evidence, and SHA-256 checksums | Authenticated artifact signing; manifest-only records | Provides practical first-stage identification and integrity evidence without generating production signing credentials now | Project owner; release/security owner | OWNER APPROVED |
| Technical protection scope | Phase 4 licensing plus release provenance; no DRM expansion | Additional future technical controls after separate approval | Keeps the boundary technically honest and avoids architecture scope creep | Project owner; security review for future changes | OWNER APPROVED |
| Legal/documentation readiness | Full proprietary documentation package plus professional legal review before external distribution | Minimal notices; staged documentation review | Reduces ambiguity about evaluation, commercial use, ownership, redistribution, and liability | Project owner and professional legal counsel | OWNER APPROVED — LEGAL REVIEW PENDING |
| External contributions | No external source contributions initially | Controlled contributions after an IP/contribution policy | Keeps ownership and release control vendor-managed while the proprietary model is being established | Project owner; legal/IP review if changed | OWNER APPROVED — LEGAL REVIEW PENDING WHERE APPLICABLE |
| Public release readiness | Not authorized yet | Public release after all gates, legal review, security review, documentation, and owner approval pass | Public source distribution is a separate decision and should not be inferred from source availability | Project owner and legal counsel | OWNER APPROVED — LEGAL REVIEW PENDING |

The previous Phase 5 plan's public-repository option remains a documented alternative for later review. Phase 5.1 recommends private/controlled distribution initially and does not change repository visibility.

## 6. Evaluation vs Production Definition

### Evaluation

Evaluation is non-production, non-customer-facing activity intended for internal review, demonstration, architecture/security assessment, isolated testing, or proof of concept. It does not include commercial service delivery, redistribution, resale, sublicensing, competing-product integration, or general modification rights.

Recommended evaluation categories:

- Proof of concept: isolated, time-bounded validation of suitability with no live customer service or business-critical operation.
- Development and QA: internal engineering or test activity that does not serve live users or deliver a commercial service.
- Demo environment: isolated demonstration using synthetic or approved non-production data and no customer-facing production dependency.
- Customer pilot: requires explicit classification and written approval; it is evaluation only when time-bounded, non-production, non-live, and not used to deliver an ongoing commercial service.
- Staging: evaluation or pre-production only when it does not serve live users or real organizational operations; staging supporting live service is production in substance.

Evaluation rights and restrictions must be defined in terms reviewed by counsel. Phase 5.1 does not create those legal terms.

### Production

Production is any live, customer-facing, business-operational, or service-delivery use, including use supporting real organizational services or real users. Production requires both:

- a commercial agreement or other written vendor authorization; and
- a vendor-issued signed technical entitlement installed through the approved licensing process.

A valid technical license is not a substitute for commercial permission, and evaluation permission is not a production entitlement.

## 7. Commercial Customer Flow

The recommended vendor-controlled flow is:

Discover/contact
→ evaluation approval
→ requirements review
→ commercial agreement
→ offline license issuance
→ official build delivery
→ customer installation
→ startup validation
→ support and renewal

The issuance process remains offline-first. No online activation service, automated billing, customer portal, or licensing server is authorized by Phase 5.1.

The technical license continues to be installed at the configured `Licensing:LicenseFilePath` location and validated at startup. Commercial terms remain outside the application.

## 8. Source Distribution Flow

The recommended controlled evaluation process is:

1. Record evaluator identity and customer reference.
2. Define the approved access period.
3. Identify the approved source revision or build version.
4. Present the evaluation terms for acceptance after legal review.
5. Grant controlled repository or package access.
6. Record the access/download event and responsible vendor operator.
7. State explicitly that production use, redistribution, and commercial service delivery are not authorized.
8. Expire or revoke distribution access at the vendor-control layer where practical.

Source-access expiration or revocation is a distribution-control operation. It is not technical license revocation and does not change the Phase 4 offline license model.

## 9. Official Build Trust Model

The minimum recommended release record is:

- product name
- semantic/versioned product version
- immutable Git commit SHA
- build timestamp
- CI run or CI evidence reference
- artifact filename
- SHA-256 checksum
- supported edition compatibility
- release owner/operator

A checksum proves artifact integrity against the published manifest. It is not, by itself, proof of cryptographic publisher identity unless the manifest and artifact record are authenticated through a separate trusted mechanism.

Future code-signing or artifact-signing review is recommended as a later implementation subphase. Phase 5.1 does not generate a production code-signing certificate, private key, customer license, or signed release artifact.

Non-vendor source builds must not be represented as official vendor production releases. They should be treated as untrusted evaluation or source-available builds unless explicitly approved by the vendor.

## 10. Repository Governance

The recommended initial governance state is:

- GitHub repository remains private.
- `main` remains vendor-controlled.
- External source pull requests are not accepted initially.
- No public source release or public fork-based distribution is authorized.
- Documentation may later be published separately after owner and legal approval.
- No GitHub visibility, repository settings, branch protection, secrets, or contribution settings are changed by Phase 5.1.

The existing repository evidence shows a read-only CI permission boundary and no production signing secret in the workflow. These controls should be preserved. A future contribution policy must state whether bug reports/security reports are accepted and whether external code contributions are declined.

## 11. Required Legal/Business Review

The following require professional legal review and explicit business-owner decisions before external source distribution or final legal publication:

- evaluation terms and permitted-use scope
- production and commercial-use terms
- modification restrictions
- redistribution and sublicensing restrictions
- derivative-work restrictions
- warranty disclaimer
- limitation of liability
- termination and breach consequences
- intellectual-property ownership
- trademark rules
- jurisdiction and governing law as appropriate
- customer support, maintenance, or SLA obligations
- treatment of pilots, staging, development, QA, and demos
- source-access expiration and post-termination obligations
- public issue, security-report, and contribution policy implications

Any future license or commercial document must be labeled `DRAFT — REQUIRES PROFESSIONAL LEGAL REVIEW` until counsel and the business owner approve it. Phase 5.1 creates no `LICENSE`, `EVALUATION-LICENSE.md`, or final commercial legal terms.

## 12. Phase 5 Implementation Subphases

The following subphases are proposed for later approval and are not started by Phase 5.1:

- Phase 5.2 — Proprietary repository notices and evaluation documentation.
- Phase 5.3 — Commercial licensing and customer workflow documentation.
- Phase 5.4 — Official build provenance and release-manifest design.
- Phase 5.5 — Repository, security, and contribution governance.
- Phase 5.6 — Legal-review reconciliation and public-distribution readiness audit.
- Phase 5.7 — Final Phase 5 sign-off.

Each subphase requires the relevant owner approvals and must preserve the Phase 4 technical boundary.

## 13. Risks

| Risk | Impact | Likelihood | Mitigation | Residual risk |
| --- | --- | --- | --- | --- |
| Evaluator redistributes source | High | Medium | Private/controlled access, evaluation terms, access records, limited approved revisions, legal enforcement | Medium |
| Evaluator modifies source | High | High | Do not grant general modification rights, distinguish unofficial builds, contractual restrictions, preserve technical limitation honestly | High |
| Unauthorized production use | High | Medium | Written commercial terms, signed production entitlement, official-build process, support and audit records | Medium |
| Legal terms are unclear or unenforceable | High | Medium | Professional legal review and explicit owner approval before distribution | Low if reviewed; otherwise High |
| Official and unofficial builds are confused | Medium | Medium | Release manifest, commit SHA, version, CI evidence, checksum, vendor release records | Medium |
| Release manifest is spoofed | High | Medium | Restrict publication authority; authenticate manifests in a future approved release-governance subphase | Medium |
| Vendor signing-key compromise | Critical | Low | Offline custody, restricted operators, backup/recovery, rotation, incident response, external issuance governance | Medium |
| Contribution/IP ownership confusion | High | Medium | No external source contributions initially; later contribution policy and legal review | Low if policy is enforced |
| Accidental repository or public exposure | High | Low | Keep repository private, restrict access, review publication gates, secret scans, owner approval | Medium |
| Secrets exposed during source distribution | Critical | Low | Pre-distribution review, secret scanning, no production keys/licenses in repository, controlled package contents | Medium |

These are planning risks, not findings that authorize implementation or public release.

## 14. Approval Record

The project owner approved all eight recommended values on 2026-09-12 with the instruction to keep the repository private and controlled. Owner approval does not constitute legal approval.

| Decision | Recommended Value | Owner Approval | Legal Approval |
| --- | --- | --- | --- |
| Distribution model | Private / controlled source distribution | APPROVED — 2026-09-12 | PENDING — PROFESSIONAL LEGAL REVIEW |
| Evaluation rights | Inspection/demo/non-production evaluation only | APPROVED — 2026-09-12 | PENDING — PROFESSIONAL LEGAL REVIEW |
| Production licensing | Manual vendor contact + offline signed commercial license | APPROVED — 2026-09-12 | PENDING — PROFESSIONAL LEGAL REVIEW |
| Official build trust | Vendor provenance + manifest + SHA-256 checksums | APPROVED — 2026-09-12 | NOT REQUIRED FOR THIS TECHNICAL DECISION |
| Technical protection scope | Phase 4 licensing + release provenance; no DRM expansion | APPROVED — 2026-09-12 | NOT REQUIRED FOR THIS TECHNICAL DECISION |
| Legal/documentation readiness | Full proprietary documentation package + professional legal review | APPROVED — 2026-09-12 | PENDING — PROFESSIONAL LEGAL REVIEW |
| External contributions | No external source contributions initially | APPROVED — 2026-09-12 | PENDING — PROFESSIONAL LEGAL REVIEW |
| Public release readiness | Not authorized yet | APPROVED — 2026-09-12 | PENDING — PROFESSIONAL LEGAL REVIEW |

## 15. Phase 5.2 Readiness

PHASE 5.1 — OWNER APPROVED

READY TO BEGIN PHASE 5.2

Phase 5.1 is complete from the project-owner decision perspective. Phase 5.2 may begin as a separately controlled implementation/documentation step, but it must not publish the repository, change repository visibility, or create legally final terms without the required legal review and explicit approvals. Phase 5 implementation has not started.
