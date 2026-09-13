> Historical multi-owner workflow, superseded 2026-09-13 by the [solo developer release checklist](../../plans/Phase-6/Release-Checklist.md). Original decisions, pending items, and results below are retained as history, not future release gates.

# Release Governance

> Operational-facing summary. Derived from the Phase 5.7 plan.

This document summarizes how LabAuthServer releases are governed. It is a governance summary, not a release announcement and not a legal agreement.

## Repository status

- The repository is **PRIVATE**.
- **No public release is currently authorized.**
- Repository visibility changes are not authorized.
- Deployment is not authorized.

## Official releases

- Official releases require vendor approval.
- An official release corresponds to an **exact Git commit SHA**.
- An official release is produced through a vendor-approved process and recorded in a vendor-controlled release register.
- Source builds produced by customers or other parties are not official vendor releases unless separately approved.

## Release tags

- Official release tags use the form `vMAJOR.MINOR.PATCH`.
- Release tags are **immutable once published**.
- Force-retagging an official release is prohibited.
- A corrected release receives a new version.

## Artifacts and provenance

- Official artifacts must correspond to an exact Git SHA.
- **Release Manifest V1** provides provenance and integrity metadata for an official release.
- Release Manifest V1 records the artifact filename, exact byte size, and lowercase hexadecimal SHA-256 digest, alongside version, Git SHA, build timestamp, CI run reference, and release operator.
- Checksums do **not** independently establish publisher identity. An unsigned manifest establishes integrity relative to a trusted channel only.

## Reproducibility

- Same-host clean publish repeatability: `VERIFIED — SAME-HOST CLEAN PUBLISH REPEATABILITY`.
- Cross-host / cross-environment reproducibility: `NOT YET VERIFIED`.
- This reproducibility evidence does not constitute a real release.

## Versioning

Product versions use `MAJOR.MINOR.PATCH`.

- `MAJOR` — breaking or materially incompatible changes
- `MINOR` — backward-compatible features or substantial supported functionality
- `PATCH` — backward-compatible fixes, security corrections, and maintenance

No product release version is declared by this document.

## Supported versions

Supported-version status is defined by release status combined with commercial support policy.

- Current release: **SUPPORTED**
- Immediately previous MINOR release: **SECURITY / CRITICAL FIX SUPPORT** where practical
- Older releases: **UNSUPPORTED** unless a commercial agreement explicitly says otherwise

Formal calendar support duration: `OWNER APPROVED — NO FIXED CALENDAR TERM; FUTURE COMMERCIAL / LEGAL POLICY REQUIRED`.
LTS: `OWNER APPROVED — NO LTS DESIGNATION UNTIL SEPARATELY APPROVED`. No release is designated LTS.

A valid signed commercial license does not automatically imply software-version support entitlement.

## Release signing

- Official governance/release **commit signing** is `OWNER APPROVED — IMPLEMENTED` using the owner-approved repository-local SSH signing policy.
- **Release-tag signing** remains `UNRESOLVED / NOT IMPLEMENTED`.
- **Release Manifest signing** remains `DEFERRED`.
- **Platform / artifact code signing** remains `DEFERRED / NOT IMPLEMENTED`.
- No signing private keys are generated or stored in the repository or CI.
- SBOM generation remains a future governance item and is not implemented.

## Security releases

- Security fixes may use expedited governance.
- Expedited governance does not bypass integrity or provenance controls.
- A security release still requires a version, source SHA, CI evidence, manifest, and approval.
- The security contact is `chriltola.khmer@gmail.com` and is owner-approved for operational use; the same mailbox is the approved shared-mailbox exception for security and commercial/evaluation intake.
- Security Response Owner: `ALOT` (backup `NOT DESIGNATED`).

## Release roles

- Release Operator: `ALOT`
- Release Approval Authority: `ALOT` initially
- Distribution Operator: `ALOT` initially
- Licensing Operator / Authorized License Issuer: `ALOT` initially
- Commercial Approval Authority: `ALOT`
- Commercial Approval Authority is distinct from Release Approval Authority (`ALOT` initially).
- Security Response Owner: `ALOT`
- Separation of duties: `REQUIRED WHERE PRACTICAL`
- Independent second-person review is required when another authorized reviewer is available; absence of a second authorized reviewer must be explicitly recorded.
- Two-person separation is not claimed under the current single-named-operator state.

## Withdrawal and supersession

A release may be marked `CURRENT`, `SUPERSEDED`, or `WITHDRAWN`.

- Withdrawal does not erase Git history and does not silently replace artifacts.
- A withdrawn artifact is never replaced in place with different bytes under the same version.
- A corrected release receives a new version.

## Distribution

Official production artifacts may only be delivered through an approved vendor distribution channel.

The delivery policy is owner-approved: use a vendor-controlled private channel with access limited to authorized operators, verified recipient identity, and recorded delivery and receipt evidence. The exact provider/channel must be selected before first external customer delivery. No provider is selected by this document.

## Authorization

- Actual release operation requires **separate authorization**.
- No customer-license issuance or commercial operation is authorized by this document.
- No Git tag, GitHub Release, artifact publication, or deployment is authorized by this document.
- Repository visibility remains private.

## Documentation validation

- Phase 5.7 is `PHASE 5.7 — OWNER APPROVED`; the project owner approved the documentation-only closeout on 2026-09-12, and its documentation acceptance criteria remain satisfied.
- The Phase 5.7 and Release-Governance current-state wording was reconciled on 2026-09-12 against the owner-approved Phase 5 commercial/support decisions. The delivery policy, Licensing Operator, Commercial Approval Authority, formal supported-version policy, and LTS policy are resolved at the governance level; exact provider/storage selections, signing items, and professional legal/business review remain pending.
- This approval does not resolve any open, pending, blocked, or deferred operational decisions and does not authorize a tag, Release, artifact publication, deployment, visibility or ownership change, signing configuration or key generation, license creation or issuance, branch-protection or ruleset change, or runtime licensing change.
- Commit `ee3ab733a764a3e2c32009d4806d43ca5b5559d5` was validated by GitHub Actions workflow `LabAuthServer CI`, run `#14` (run ID `34681947100`), with conclusion `SUCCESS`.
- CI execution success is separate from branch-protection enforcement; `main` enforcement is not claimed by this record.

## Related documents

- [Phase 5.7 — Release Governance and Supported-Version Policy](../../plans/Phase-5/Phase-5.7-Release-Governance-and-Supported-Version-Policy.md)
- [Phase 5.4 — Official Build Provenance and Release Manifest Design](../../plans/Phase-5/Phase-5.4-Official-Build-Provenance-and-Release-Manifest-Design.md)
- [Licensing](../../Licensing.md)
- [Commercial Licensing](../../Commercial-Licensing.md)