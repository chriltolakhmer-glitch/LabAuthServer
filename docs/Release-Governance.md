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

Formal support duration and LTS policy are `PENDING — COMMERCIAL / SUPPORT POLICY DECISION`.

A valid signed commercial license does not automatically imply software-version support entitlement.

## Release signing

- Release signing is **not yet implemented**.
- Commit/tag signing is unresolved.
- No signing keys are generated or stored in the repository or CI.
- Platform/code signing and SBOM generation are future items and are not implemented.

## Security releases

- Security fixes may use expedited governance.
- Expedited governance does not bypass integrity or provenance controls.
- A security release still requires a version, source SHA, CI evidence, manifest, and approval.
- The security contact remains unresolved.

## Withdrawal and supersession

A release may be marked `CURRENT`, `SUPERSEDED`, or `WITHDRAWN`.

- Withdrawal does not erase Git history and does not silently replace artifacts.
- A withdrawn artifact is never replaced in place with different bytes under the same version.
- A corrected release receives a new version.

## Distribution

Official production artifacts may only be delivered through an approved vendor distribution channel.

Approved delivery channel: `<APPROVED_DELIVERY_CHANNEL>` (unresolved).

## Authorization

- Actual release operation requires **separate authorization**.
- No Git tag, GitHub Release, artifact publication, or deployment is authorized by this document.
- Repository visibility remains private.

## Documentation validation

- Phase 5.7 documentation is complete pending owner review / approval; no explicit owner approval is recorded.
- Commit `e7929ff282be5144e302a13118133f718e7273cd` was validated by GitHub Actions workflow `LabAuthServer CI`, run `#13` (run ID `34681060457`), with conclusion `SUCCESS`.
- CI execution success is separate from branch-protection enforcement; `main` enforcement is not claimed by this record.

## Related documents

- [Phase 5.7 — Release Governance and Supported-Version Policy](plans/Phase-5/Phase-5.7-Release-Governance-and-Supported-Version-Policy.md)
- [Phase 5.4 — Official Build Provenance and Release Manifest Design](plans/Phase-5/Phase-5.4-Official-Build-Provenance-and-Release-Manifest-Design.md)
- [Licensing](Licensing.md)
- [Commercial Licensing](Commercial-Licensing.md)