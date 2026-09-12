# Phase 5 — Evaluation Distribution, Proprietary Source Licensing, and Commercial Governance

Status: PLANNING ONLY. No implementation, no source-code changes, no production configuration changes, no deployment actions, and no code signing/secret generation are authorized by this plan.

## 1. Executive summary

Phase 4 establishes the technical foundation for signed offline commercial licensing, but it does not define the business/legal package that governs how source is distributed, how evaluation use is permitted, how commercial use is sold, or how official vendor builds are distinguished from arbitrary source builds. Phase 5 addresses that gap.

The recommended direction is to treat the repository as a proprietary source-available project, not an open-source project. The product should permit source inspection and evaluation under a separate proprietary evaluation/source-available license, while prohibiting modification and commercial use without a vendor-signed commercial license. Production/live use must remain governed by the signed licensing system already established in Phase 4.

This plan does not claim that source distribution can prevent modification or bypass. Rather, it defines realistic technical and legal controls: signed production licenses, official-build provenance, release governance, source notices, clear evaluation restrictions, and vendor-controlled commercial distribution. The repository remains source-available for review/evaluation only; it is not a general open-source distribution model.

## 2. Background

The repository already documents a clear technical licensing model:

- Signed offline commercial licenses.
- Trusted public-key verification with keyId.
- Community/Professional/Enterprise edition model.
- Default-deny feature enforcement.
- Fail-closed behavior for invalid, missing, or expired licenses.
- Vendor-controlled private signing key outside the application and repository.
- Startup validation and runtime policy evaluation.
- No online activation or revocation service in the baseline implementation.

The Phase 4 materials repeatedly state that the system is intended to support a source-available + commercial technical license model. The repository also explicitly states that a customer with the source code can modify the software and remove or alter license checks. This is the key boundary: technical enforcement is a commercial and authenticity control, not unbreakable DRM.

Phase 5 is therefore not a technical implementation phase in the same sense as Phase 4. It is a commercialization and governance phase that translates the technical licensing model into a clear business and distribution model.

## 3. Current-state evidence

This plan is traceable to the repository evidence and Phase 4 decisions currently present in the project.

Evidence base in the repository:

- [README.md](../../README.md) documents a signed offline license document and states that licensing is not wired into the running host yet.
- [docs/Licensing.md](../../docs/Licensing.md) explicitly states: source-available limitations, offline-first design, no online activation, no machine binding, and the limitation that source control enables modification.
- [docs/plans/Phase-4/Phase-4-README.md](../Phase-4/Phase-4-README.md) records the approved architecture and states: “Licensing model: source-available + commercial technical license.”
- [docs/plans/Phase-4/Phase-4.15-Final-Security-Review.md](../Phase-4/Phase-4.15-Final-Security-Review.md) confirms the source-available limitation and that technical enforcement cannot prevent a customer from modifying source or binaries.
- [docs/plans/Phase-4/Phase-4.16-Production-Licensing-Readiness.md](../Phase-4/Phase-4.16-Production-Licensing-Readiness.md) confirms startup-only runtime loading, operator-controlled license file path, and the absence of hot reload or per-request revalidation.
- [docs/plans/Phase-4/Phase-4.17-Implementation-and-Final-Sign-Off.md](../Phase-4/Phase-4.17-Implementation-and-Final-Sign-Off.md) defines the approved production license governance model, external register, offline issuance flow, and public-key-only server boundary.
- [docs/Security.md](../../docs/Security.md) reinforces that licensing does not weaken authentication/authorization and clarifies the source-available limitation.
- [.github/workflows/ci.yml](../../.github/workflows/ci.yml) shows CI is build/test only, with no secrets or production signing material.

Approved Phase 4 decisions relevant to Phase 5:

- D-01: licensing model is source-available + commercial technical license.
- D-02: offline-first design and no Internet dependency initially.
- D-03: missing license starts in Community/restricted mode.
- D-04: editions are Community, Professional, Enterprise.
- D-09: machine binding is deferred.
- D-10: online activation is optional future capability.
- D-16-D-18: vendor private key must never be committed to GitHub or CI.
- D-22: revocation remains outside the initial offline implementation.

These are the baseline dependencies that Phase 5 must fit without weakening or redefining them.

## 4. Business objective

The business objective is to support the commercial model already anticipated by Phase 4 while preserving a legitimate evaluation path for source inspection and limited demo use, without converting the project into a publicly open-source product.

The project direction is therefore:

- Source may be distributed for evaluation, review, and demonstration under a proprietary evaluation/source-available license.
- Evaluation users may inspect the code and test the product in non-production scenarios.
- Modification is not an authorized right under the evaluation license; it is treated as a prohibited act unless separately licensed.
- Production or live commercial use requires a valid commercial license issued by the vendor.
- Official vendor builds remain governed by the signed licensing system and release governance model.

This is a business decision and legal framework requirement, not a technical claim that source distribution can prevent alteration.

## 5. Scope

Phase 5 covers:

- Source distribution model and repository access model.
- Proprietary evaluation/source-available license terms.
- Evaluation rights vs. production rights.
- Commercial licensing workflow.
- Official build trust and release governance.
- GitHub/public repository messaging and legal notices.
- Documentation to be added or updated.
- Security and release governance around public distribution.
- Decision gates and risk review for public release.

## 6. Non-goals

Phase 5 explicitly does not include:

- Online licensing server or activation service.
- Online revocation service.
- Machine binding or hardware fingerprinting.
- DRM-like enforcement.
- Automated billing or payment integration.
- Automatic customer portal or license self-service.
- Production implementation of HSM/KMS or secure private-key hosting.
- Per-request or hot-reload license revalidation.

These remain deferred unless a later phase explicitly approves them.

## 7. Existing architecture dependencies

Phase 5 must align with the existing technical licensing implementation and not undermine it.

Dependencies and constraints:

- The server validates the signed license at startup using a trusted public key set and a fail-closed policy.
- The customer receives trusted public keys, not private production keys.
- Community/restricted behavior is already the default for invalid or missing licenses.
- The project is source-available and the project cannot claim technical impossibility of modification.
- Production build governance must preserve the distinction between official vendor releases and arbitrary source builds.
- The project must not introduce a licensing architecture that silently weakens security controls or the license validation model.

## 8. Distribution model options

The following distribution models should be evaluated before public or semi-public source distribution is approved.

### Option A — Public GitHub repository with proprietary evaluation/source-available license

Pros:

- Easy to share the product for evaluation.
- Makes source inspection possible.
- Supports transparent evaluation and demo use.
- Allows legal notices and commercial terms to be published alongside the project.

Cons:

- Public source exposure increases imitation risk.
- Public repo visibility requires more formal governance and security review.
- Public project issues and PRs may become operationally and legally burdensome.
- This model is not equivalent to open source; it requires clear “no-open-source” messaging.

### Option B — Private repository or controlled distribution repository

Pros:

- Better control over who can inspect the source.
- Lower risk of public leakage and uncontrolled issue traffic.
- Greater control over evaluation distribution.

Cons:

- Harder for prospective customers to evaluate.
- Slower sales/technical evaluation pipeline.
- Requires governed access permissions and distribution procedures.

### Option C — Public source inspection with commercial-only rebuild rights

Pros:

- Adds investor/customer transparency.
- Gains marketing value without full open-source rights.

Cons:

- Requires clear legal terms and enforcement processes.
- Must avoid ambiguity between “code is public” and “right to use commercially is granted.”

### Recommendation

The recommended model is Option A with explicit proprietary source-available terms, but only after legal review and explicit approval. This fits the documented project direction and the technical licensing baseline. The project should avoid pretending it is open source; it should state clearly that the source is available for inspection and evaluation under a proprietary license, not open-source licensing.

## 9. Recommended model

Recommended Phase 5 direction:

- Public repository is acceptable only if it is clearly labeled as proprietary source-available and not open source.
- The repository is for evaluation/review/demonstration, not unrestricted commercial deployment.
- Source inspection is permitted under the evaluation terms.
- Modification is prohibited under the evaluation terms.
- Commercial use without a signed commercial license is prohibited.
- Redistribution is prohibited except as expressly authorized by the vendor.
- Derivative product creation is prohibited without a written commercial license.
- Removing licensing checks, bypassing access controls, or reusing the code in a commercial product is prohibited without written permission.
- Reverse engineering is only permitted to the extent explicitly stated in the evaluation/source-available license and legal counsel approval; it must not be used to override the licensing model.

This recommendation is intentionally conservative and clearly distinguishes the legal/business boundary from the technical enforcement boundary.

## 10. Evaluation license requirements

The evaluation license should be a proprietary source-available license with explicit rights and limitations. The following items should be covered in the proposed language and reviewed by legal counsel before publication.

The following are proposed business/legal requirements, not final legal wording:

- Permitted use: source inspection, research, internal proof of concept, limited demonstration, and non-production evaluation.
- Prohibited use: modification, commercial use, production deployment, sublicensing, redistribution, integration into a competing product, and use of modified source in a production/commercial system.
- Modification restrictions: no editing, derivative work implementation, or source-based productization without written permission.
- Commercial-use restrictions: no production, live, or customer-facing production use without a separate commercial license.
- Production-use restrictions: evaluation license does not permit live or production use.
- Derivative-work restrictions: no derivative product or bundle may be created without a separate license.
- License-removal restrictions: no deletion, bypass, or tampering with license notices or product notices is permitted.
- Copyright ownership: vendor retains all ownership rights in the code and associated materials.
- Trademark ownership: vendor retains trademark and brand ownership as applicable; no commercial use of marks without written consent.
- Warranty disclaimer: evaluation software is provided without warranties.
- Limitation of liability: subject to legal review; the vendor should limit liability consistent with the business model.
- Termination: the evaluation license terminates on expiration, breach, or violation of restrictions.
- Violation consequences: vendor may revoke rights, require removal, and pursue legal remedies consistent with the license.
- Evaluation period: a time-limited evaluation period may be used; if used, the term must be explicit and enforceable.

Important: this is a proposed legal/business requirement set and must be reviewed by counsel before publication. It is not a final legal opinion.

## 11. Evaluation vs production model

The project should define a clear separation between these categories:

- Source evaluation/demo.
- Internal testing and customer proof of concept.
- Development and QA use.
- Production/live use.

Recommended policy:

- Source evaluation/demo may use the evaluation license only.
- Internal testing and proof-of-concept work may be permitted only if it is clearly non-production and not customer-facing production deployment.
- Development/testing environments may use the same source but should not be treated as production entitlement.
- Production/live use always requires a valid commercial license and official vendor-signed license file.

When a user attempts to run without a production license:

- The current technical behavior is already to remain in restricted Community mode if the license is invalid or missing.
- That behavior is suitable only as a technical protection layer and not as a substitute for a legal commercial-use permission model.
- Official release notes and docs should be explicit: the software may be evaluated, but production use requires a license.

A dedicated Evaluation edition should be considered as a business-level model, but it must not replace the existing Community/Professional/Enterprise technical license definitions without a Phase 4 follow-up decision. The existing Community edition may be a valid technical restricted mode, but it is not automatically the best legal evaluation tier. The Phase 5 recommendation is to distinguish evaluation rights from production licensing even if the code remains using the technical Community edition as the restricted default.

## 12. Commercial licensing workflow

The commercial licensing path should be straightforward and vendor-controlled.

Recommended path:

1. Customer requests evaluation or commercial access.
2. Vendor reviews use case, edition, feature needs, and commercial terms.
3. Vendor issues a commercial license under a controlled offline process.
4. Customer receives a vendor-signed license file via approved secure channel.
5. Customer installs the license file at the configured `Licensing:LicenseFilePath` location.
6. Application validates the license on startup.
7. Application enforces edition and feature policy using the signed license.
8. Customer can use the product only under the vendor-approved commercial terms.

This model matches the existing Phase 4 architecture and avoids requiring a public online activation service. It is consistent with the project’s current decision to remain offline-first.

## 13. Technical protection strategy

Phase 5 must define technical protection boundaries honestly and without overstating effectiveness.

### What the technical model protects well

- License content authenticity.
- Tamper resistance when the signed license is modified or replaced.
- Default-deny enforcement for unknown or invalid features.
- Validation of edition, explicit features, expiry, and limits.
- Strong separation between public-key verification and private-key custody.
- Official release artifact integrity when combined with signed releases and provenance controls.

### What it does not protect against

- A customer who has the source code and rebuilds the software.
- A customer who removes or modifies the license checks in their local source copy.
- A host administrator who modifies the runtime environment.
- A user who bypasses the licensing boundary at the source or host level.

Important explanation:

The project is a source-available codebase. A sufficiently capable party with source and the ability to rebuild can modify the program and distribute a custom build. Technical controls can deter accidental misuse, support legal enforcement, and preserve customer authenticity, but they cannot make a source-distributed application effectively immune to modification. This is not a weak point in the architecture; it is a boundary that must be acknowledged explicitly in the business model and docs.

## 14. Official build/release trust model

The project should distinguish official vendor builds from arbitrary source builds.

Recommended model:

- Official vendor releases are built by the vendor using an approved release pipeline.
- Release artifacts are signed or checksum-verified in a way that provides provenance.
- Versioning and release manifests should be clear and consistent.
- The vendor should maintain a release record showing who built the artifact, when, and which source revision it corresponds to.
- Official vendor builds must carry both build metadata and license-policy metadata needed for trusted production use.
- Non-vendor builds should be treated as untrusted evaluation or source-available builds unless explicitly approved.

This is not a claim that arbitrary builds are impossible to alter; it is a governance mechanism that helps distinguish official vendor distribution from unapproved derivative builds.

## 15. GitHub/public repository governance

If the project is public, governance must be explicit and readable.

Required policy areas:

- README messaging must say the project is proprietary and source-available, not open source.
- Copyright notice must state vendor ownership.
- Proprietary license notice must explain the evaluation and commercial-use boundaries.
- Evaluation-use notice must say what users may inspect and what they may not do.
- Commercial licensing contact instructions must be listed.
- Security policy should specify how to disclose vulnerabilities and who owns the response path.
- Issue/discussion policy should state whether public issues, bug reports, or PRs are accepted.
- Contribution policy should state whether external contributions are accepted, and if not, why.
- Supported version policy should clearly explain which versions are supported and which are status-by-status evaluation-only.
- Release/tag policy should define how release tags are created and what they mean.
- PR policy must be explicit: accept or decline contributions based on proprietary governance and ownership decisions.

This should be treated as a governance decision to consider rather than a silent assumption. The project should not silently accept external contributions if the business model is proprietary and owner-controlled without an explicit policy.

## 16. Documentation changes

The following documentation should be added or updated before public distribution:

- README.md
- docs/Licensing.md
- docs/Evaluation-License.md or similar
- docs/Commercial-Licensing.md
- docs/Release-Governance.md
- SECURITY.md
- CONTRIBUTING.md
- COPYRIGHT.md or a clear proprietary notice
- docs/plans/Phase-5/Phase-5-Plan.md

These documents should be clear about what is permitted under evaluation, what is prohibited, and how production licensing is obtained.

## 17. Security considerations

Phase 5 introduces governance and public exposure risks that are distinct from technical licensing enforcement.

Security concerns to review:

- Private signing key protection.
- Source exposure by public distribution.
- Public verification key exposure.
- License forgery and tampering.
- Binary modification by a customer with the source.
- Build pipeline compromise.
- GitHub repository compromise.
- Unauthorized release artifacts.
- Accidental publication of secrets.
- Insider misuse and license-register leakage.
- Dependency and supply-chain risk in public repos.

The plan should preserve the existing Phase 4 security model while extending governance to public distribution.

## 18. Legal/business review items

The following items require legal/business review before public distribution and should be labeled as requiring professional review, not treated as legal fact:

- Evaluation license wording.
- Commercial-use restrictions and acceptable use terms.
- Copyright and source ownership statement.
- Trademark usage policy.
- Distribution of source code and what constitutes redistribution.
- Restrictions on modification and derivative use.
- Warranty disclaimers and liability allocation.
- Termination and enforcement procedures.
- Public issue and contribution policy implications.

These items must be reviewed by counsel and approved by the owning business authority.

## 19. Decision gates

### Gate 1 — Distribution Model

Decision: Whether the project will be public GitHub, private repository, or controlled distribution. 
Options: 
- Public GitHub with proprietary evaluation license.
- Private repo with controlled evaluation distribution.
- Hybrid with a public summary and private source access.
Recommendation: public GitHub only if the legal model is properly defined and the project is explicitly labeled proprietary source-available. 
Trade-off: public visibility versus control. 
Approval required: yes.

### Gate 2 — Evaluation Rights

Decision: What evaluation users may and may not do. 
Options: 
- Source inspection only.
- Limited demo use only.
- Non-production testing only.
- No modification allowed.
Recommendation: allow source inspection and demo evaluation without modification or production use. 
Trade-off: flexibility versus control. 
Approval required: yes.

### Gate 3 — Production Licensing

Decision: How customers obtain production rights. 
Options: 
- Vendor contact and manual license issuance.
- Predefined edition packages with signed license file delivery.
- Customer portal or other automated mechanism.
Recommendation: vendor contact and manual controlled issuance remain the simplest and most consistent with the current Phase 4 architecture. 
Trade-off: operational rigor versus automation. 
Approval required: yes.

### Gate 4 — Official Build Trust

Decision: How vendors distinguish official builds from arbitrary source builds. 
Options: 
- Signed release artifacts only.
- Release manifests and checksums.
- Build provenance and vendor-controlled artifact pipeline.
Recommendation: require official release artifact provenance and signed or checksum-verified release files. 
Trade-off: operational complexity versus trust assurance. 
Approval required: yes.

### Gate 5 — Technical Protection

Decision: What technical controls are justified and what limits remain explicit. 
Options: 
- Strong licensing enforcement only.
- Licensing enforcement plus official-build governance.
- Licensing enforcement plus source notice and legal restrictions.
Recommendation: technical enforcement should be limited to signed licenses and production-policy enforcement, while explicitly acknowledging the source-available limitation. 
Trade-off: realistic enforcement versus over-claiming DRM. 
Approval required: yes.

### Gate 6 — Documentation and Legal Readiness

Decision: What documentation must exist before public distribution. 
Options: 
- Minimal notices only.
- Full evaluation/commercial licensing documentation set.
- Legal review and public-doc package before release.
Recommendation: full public legal and release docs are required before any public distribution decision. 
Trade-off: governance overhead versus speed. 
Approval required: yes.

### Gate 7 — Release Readiness

Decision: readiness gate for public release. 
Criteria:
- Distribution model approved.
- Evaluation rights approved.
- Production licensing path approved.
- Build trust model defined.
- Legal review completed.
- Documentation package prepared.
- Security review completed.
Recommendation: no public release until all gates pass and approval is recorded. 
Approval required: yes.

## 20. Risk register

| Risk | Impact | Likelihood | Mitigation | Owner / decision authority | Phase status | Residual risk |
| --- | --- | --- | --- | --- | --- | --- |
| Source is downloaded, license checks are removed, and the application is rebuilt for production use | High | High | Document explicit legal restrictions, use official vendor builds, separate evaluation and production messaging, and treat technical enforcement as a deterrent not a guarantee | Product owner + legal counsel | Phase 5 open | Moderate |
| Public repository leads to unapproved forks or reuse | High | Medium | Clear proprietary notices, public contribution policy, and legal restrictions | Product owner | Phase 5 open | Moderate |
| Customer assumes source distribution equals open source | High | Medium | README and license notices must state proprietary source-available model clearly | Product owner + legal | Phase 5 open | Low |
| Evaluation users treat demo software as production-ready | Medium | Medium | Clear evaluation-use notice and production-license gating | Product owner | Phase 5 open | Low |
| Official build provenance is not clear | Medium | Medium | Release manifests, provenance proof, vendor release process | Release owner | Phase 5 open | Medium |
| License key or issuance process is mishandled | High | Low | Keep license issuance outside the app and under vendor controls; document operational procedures | Vendor licensing owner | Phase 4 baseline, Phase 5 governance | Low |
| GitHub repository compromise exposes design or public keys | Medium | Low | Restrict repo content, secrets scanning, minimal release exposure, and documented incident response | Security owner | Phase 5 open | Low |
| Legal language is inaccurate or unenforceable | High | Medium | Formal legal review before public distribution | Counsel | Phase 5 open | Low if reviewed |

Important statement for risk register:

“User downloads source, removes licensing checks, rebuilds the application, and uses the modified build.”

This is an unavoidable risk of distributing source code. The mitigation is not technical absolution; it is: explicit legal restrictions, clear evaluation vs production separation, official vendor build governance, signed production licenses, and explicit acknowledgment that source-distributed software cannot be made impossible to modify by technical means alone.

## 21. Validation/testing strategy

Because this is a planning phase, implementation testing is deferred, but the future validation plan should include:

- Licensing regression tests for valid, invalid, expired, malformed, and downgraded licenses.
- Official-build validation for vendor release artifacts and provenance checks.
- Release artifact validation for checksums, manifest integrity, and artifact signing or verification.
- Secret scanning for private keys, production credentials, and signing material.
- Source-distribution checks to confirm the public repo contains only intended disclosures and not production secrets.
- Documentation consistency checks comparing README, legal notices, and licensing docs.
- Evaluation/production separation tests to ensure evaluation and production messages remain distinct in documentation and release notes.
- Tamper/modification behavior validation that remains realistic and does not overclaim that a source-distributed product is impossible to alter.
- CI security checks for workflow permissions, dependency safety, and secret leak detection.

No implementation test work is performed as part of this planning action.

## 22. Implementation work breakdown

This is the proposed work decomposition for a later implementation phase. It is intentionally limited to governance and release readiness.

1. Confirm business objective and distribution model.
2. Approve evaluation rights and commercial-use restrictions.
3. Draft and review evaluation/source-available license text with counsel.
4. Draft commercial licensing workflow and approved issuance model.
5. Define official vendor build governance and release artifact trust model.
6. Update README and legal notices to disclose proprietary source-available status.
7. Create or update SECURITY.md and CONTRIBUTING.md.
8. Define release tag and supported-version policy.
9. Draft public-facing commercial licensing guide and contact instructions.
10. Determine whether public PRs and external contributions are allowed.
11. Run risk and security review before public distribution.
12. Approve release gate and final public-access decision.

## 23. Deferred work

The following work remains explicitly outside Phase 5 unless separately approved:

- Online activation.
- Online revocation.
- Licensing server or customer portal.
- Machine binding or hardware fingerprinting.
- DRM-like enforcement.
- Automatic billing or payment integration.
- Cloud licensing or remote attestation.
- HSM/KMS production key hosting.
- Per-request license validation and hot reload.

These are explicitly deferred because they are not required by the current project baseline and would materially change the architecture or legal model.

## 24. Phase 5 completion criteria

Phase 5 is complete when all of the following are true:

- Distribution model is formally decided.
- Evaluation rights are documented.
- Production licensing path is documented.
- Legal review items are identified and triaged.
- Official build trust model is defined.
- Technical protection boundaries are documented honestly.
- Public repository messaging is defined.
- Security and release risks are reviewed.
- Required documentation set is identified.
- No production secrets or private keys are introduced into the repository or build pipeline.
- Licensing architecture remains consistent with the Phase 4 technical baseline.
- Future implementation work is decomposed into clear tasks.

## 25. Open decisions requiring explicit approval

The following decisions remain open and must be approved explicitly instead of being silently assumed:

1. Public GitHub vs private repository or controlled distribution.
2. Whether the source is published as proprietary source-available or kept private.
3. Whether evaluation rights include source inspection only or also limited demo and proof-of-concept use.
4. Whether modification is prohibited outright or whether narrow internal technical review rights are granted under separate explicit terms.
5. Whether the commercial license process remains manual/vendor-controlled or adds automation.
6. Whether official builds will be provenance-tracked and release-verified.
7. Whether external contributions are accepted or the project remains vendor-owned and source-controlled only.
8. Whether README and repository notices will publicly distinguish proprietary evaluation use from production licensing.
9. Whether all legal language is reviewed by counsel before public release.

Each of these decisions has trade-offs. The recommended option is stated above, but the final decision must be approved by the project owner and legal counsel before implementation or release.

## 26. Final recommendation

The recommended Phase 5 direction is to establish a proprietary source-available model with a clearly separated evaluation license and a separate commercial production license path. This aligns with the existing Phase 4 technical decisions: offline-first signature verification, vendor-controlled private key custody, public-key trust model, restricted Community behavior, and no online activation/revocation in the baseline.

This recommendation supports both business reality and technical honesty. It recognizes that source code distribution cannot make a program impossible to modify, but it can still create a meaningful legal and governance boundary, supported by signed production licenses, official build trust, and clear public messaging.

The project should not publicly describe the project as open source. It should clearly distinguish evaluation/demo use, internal testing, and production use, and it should require written commercial licensing for any production or live deployment.

This recommendation is suitable for a phase plan only; it does not authorize source release or implementation work until all decision gates have been approved.
