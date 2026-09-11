# Phase 4.0 — Requirements and Licensing Model

Status: APPROVED — PLANNING COMPLETE. Implementation NOT STARTED. [Phase 4 README](Phase-4-README.md) | [Master roadmap](../README.md).

## Objective

Define what Technical License Enforcement must achieve, what it must never do, and the licensing vocabulary the rest of Phase 4 depends on.

## Approved requirements baseline (2026-09-11)

MUST HAVE:

- Offline-first licensing (no Internet dependency in the first implementation).
- Signed license document.
- RSA-PSS with SHA-256 using RSA-3072 for the initial implementation.
- Multiple trusted license-signing public keys (key rotation).
- Feature licensing (explicit feature list in the signed license).
- Editions (Community, Professional, Enterprise).
- Maximum-user limits.
- Expiration (time-limited licenses) and perpetual licenses.
- Configurable license file location.
- Default-deny for unknown features/editions.
- Restricted mode (Community/restricted) on missing, invalid, expired or tampered license.
- No production private key in the application, GitHub or CI.

DEFERRED:

- Machine binding.
- Online activation.
- Revocation.
- HSM.
- Customer portal.
- Hardware-backed signing.

Approved licensing behavior:

```
No license            -> Community/restricted mode
Valid Community       -> Community features
Valid Professional    -> Professional features
Valid Enterprise      -> Enterprise features
Invalid/tampered/expired -> Restricted mode
Unknown feature/edition  -> Default deny
```

Licensing must not disable authentication or security controls.

## Scope

In scope:

- Requirement statements for offline signed-license enforcement.
- The licensing model: product, edition, features, limits, expiration.
- The distinction between legal licensing, cryptographic authenticity, technical enforcement and online activation.
- The explicit statement that source-code delivery limits enforcement.

Out of scope:

- Any code, configuration, test, CI, database or deployment change.
- Any key generation, key storage or license issuance.
- Any commercial pricing, contract or legal wording.

## Why it exists

Every later phase depends on the same vocabulary and the same non-negotiable constraints. Without 4.0, the format, the algorithm and the enforcement points can drift into incompatible designs.

## Prerequisites

- Completed security hardening baseline, including JWT hardening, RSA policy, dedicated signing certificate, SQL Server TLS, request/header/claim limits, LDAP classification and limits, proxy/forwarded-headers assessment, HSTS, AllowedHosts, HTTP.sys assessment, network/WAF/LB assessment, capacity assessment, operational readiness review and GitHub Actions CI.
- Baseline build and test state: 733 tests, 733 passed, 0 failed, 0 skipped, Release build, 0 warnings, 0 errors.
- Read [Security](../../Security.md), [Configuration](../../Configuration.md), [Testing](../../Testing.md), [Validation Status](../../Validation_Status.md) and repository `AGENTS.md`.

## Inputs

- The requirement list in [Phase-4-README](Phase-4-README.md).
- The existing security controls that licensing must not weaken.
- The existing CI workflow `.github/workflows/ci.yml`.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.0-1 | Offline-first licensing; no Internet dependency in the first implementation | APPROVED (D-02) | Customers may run air-gapped; online activation is a later option |
| D4.0-2 | Licenses are signed documents, not opaque blobs | APPROVED (D-05) | Auditable, versionable, testable |
| D4.0-3 | Enforcement is default-deny for licensed functionality | APPROVED (D-20) | Fail-closed is the only safe default |
| D4.0-4 | Licensing must never disable authentication or security controls | FINAL (constraint, D-15) | Security regression would be worse than unlicensed use |
| D4.0-5 | Editions approved: Community, Professional, Enterprise | APPROVED (D-04) | Commercial tiers recorded |

RESOLVED: Edition names approved as Community, Professional and Enterprise (D-04, 2026-09-11). The exact feature-to-edition mapping remains TO BE CONFIRMED DURING IMPLEMENTATION before Phase 4.6.

RESOLVED: An unlicensed deployment starts in Community/restricted mode that keeps authentication and security active (D-03, D-14). It does not refuse to start.

## Proposed architecture

The licensing model is a layered set of concepts:

- **Product** — the licensed software identity, here `LabAuthServer`.
- **Edition** — a named entitlement bundle (for example Community / Professional / Enterprise).
- **Feature** — a stable identifier for a gated capability.
- **Limit** — a numeric or enumerated bound attached to an edition (for example maximum users).
- **Validity window** — issued-at and expiry timestamps.
- **Key identifier** — which vendor signing key produced the signature.

Each concept is defined once here and reused verbatim by the format, the issuer and the validator.

## Files likely to change

- None in Phase 4.0. Documentation only.

## Files that must NOT change

- Everything under `src/`.
- Everything under `tests/`.
- `appsettings*.json` and any configuration file.
- `.github/workflows/ci.yml`.
- Database scripts under `database/`.
- Deployment scripts and `publish/` output.

## Implementation steps

1. Confirm the requirement list and constraints with project ownership.
2. Resolve the DECISION REQUIRED items in this document.
3. Record the resolved decisions in [Phase-4-Decision-Log](Phase-4-Decision-Log.md).
4. Freeze the vocabulary before starting 4.1.

## Security considerations

- The requirement set must not permit a "license disables security" shortcut.
- Requirements must not mandate storing customer-identifying data in a way that leaks through API responses.
- Requirements must not mandate Internet egress.

## Failure cases

- Edition names chosen without ownership approval, then changed later, invalidating issued licenses.
- A requirement that a license be checked only at startup, leaving long-running processes unenforced.
- A requirement that licensing run before authentication, allowing licensing to become an authentication bypass.

## Testing requirements

- No tests in 4.0. Requirements are verified by review and by traceability into 4.10.

## Acceptance criteria

- Requirement list is complete, unambiguous and traceable.
- Every DECISION REQUIRED is either resolved or explicitly deferred with an owner.
- The four-way distinction (legal, cryptographic, technical, online) is stated.
- The source-code limitation is stated without overclaiming.

## Rollback considerations

Documentation-only phase. Rollback is reverting the commit.

## Evidence to record

- Review outcome and approver.
- Resolved decision values with dates.
- Link to the reviewed requirement list.

## Git/commit strategy

- Scope: `docs/plans/Phase-4/` only.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Depends on the completed security hardening baseline only. No Phase 4 predecessor.

## Risks

- Commercial requirements changing after the format is frozen.
- Ambiguity between "feature not licensed" and "feature not implemented".

## Deferred items

- Online activation requirements (deferred to 4.13).
- Machine binding requirements (deferred to 4.8).
- Pricing, contract and legal wording (out of scope for Phase 4).