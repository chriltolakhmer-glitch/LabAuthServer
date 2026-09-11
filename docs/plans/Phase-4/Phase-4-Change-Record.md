# Phase 4 — Change Record

Status: APPROVED — PLANNING COMPLETE. Implementation NOT STARTED. [Phase 4 README](Phase-4-README.md).

This is a running record. It is updated during future implementation phases. Rows are appended, never rewritten.

| Date | Phase | Change | Files | Tests | Commit | Status |
| --- | --- | --- | --- | --- | --- | --- |
| 2026-09-11 | 4 (planning) | Created the Phase 4 Technical License Enforcement plan set: master README, phase documents 4.0–4.15, decision log, implementation checklist, risk register, change record | `docs/plans/Phase-4/Phase-4-README.md`, `Phase-4.0-*.md` through `Phase-4.15-*.md`, `Phase-4-Decision-Log.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Risk-Register.md`, `Phase-4-Change-Record.md` | None run (documentation only) | Not committed | COMPLETE — DOCUMENTATION ONLY |
| 2026-09-11 | 4.0 | Approved Phase 4 Technical License Enforcement baseline decisions. | `docs/plans/Phase-4/Phase-4-README.md`, `Phase-4.0-*.md` through `Phase-4.15-*.md`, `Phase-4-Decision-Log.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Risk-Register.md`, `Phase-4-Change-Record.md` | None run (documentation only) | Not committed | APPROVED — PLANNING COMPLETE; Implementation: NOT STARTED |
| 2026-09-11 | 4.1 + 4.2 | Implemented the license architecture foundation and the signed license document model: domain model, typed validation results, signature-verifier and key-provider abstractions, RSA-PSS verifier, strict JSON container parser, and licensing unit tests. No issuer, no private key, no production license, no machine binding, no online activation, no feature enforcement. | `src/LabAuthServer.Domain/Licensing/*`, `src/LabAuthServer.Application/Licensing/*`, `src/LabAuthServer.Infrastructure/Security/Licensing/*`, `tests/LabAuthServer.UnitTests/Licensing/*`, `docs/plans/Phase-4/Phase-4.1-License-Architecture.md`, `Phase-4.2-License-Document-Format.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Change-Record.md` | Release build 0 warnings 0 errors; tests: Unit 517, Integration 246, total 763, 0 failed, 0 skipped (baseline 733 preserved, +30 licensing tests) | Not committed | COMPLETE — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED |
| 2026-09-11 | 4.3 | Implemented the cryptographic verification boundary: RSA-PSS + SHA-256 verification over exact signed-payload bytes, multi-key trusted key set with keyId selection, minimum RSA key size enforcement (2048), public-only key enforcement (F-3 resolved), and signature-encoding validation in the parser. F-2 (envelope binding) documented as unchanged intentional design. No issuer, no private key, no production signing capability, no feature/edition/limit/expiration enforcement. | `src/LabAuthServer.Infrastructure/Security/Licensing/InMemoryTrustedLicenseKeyProvider.cs`, `RsaPssLicenseSignatureVerifier.cs`, `JsonLicenseDocumentParser.cs`, `tests/LabAuthServer.UnitTests/Licensing/RsaPssLicenseSignatureVerifierTests.cs`, `Phase43CryptographicVerificationTests.cs`, `docs/plans/Phase-4/Phase-4.3-Cryptographic-Signing-and-Verification.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Change-Record.md` | Release build 0 warnings 0 errors; tests: Unit 554, Integration 246, total 800, 0 failed, 0 skipped (+37 Phase 4.3 tests) | Not committed | COMPLETE — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED |
| 2026-09-11 | 4.4 | Implemented the vendor-side license issuer: a standalone `tools/LabAuthServer.LicenseIssuer/` project referencing only `LabAuthServer.Domain` and kept out of `LabAuthServer.slnx`; `ILicenseSigningKeyProvider` and `ILicenseSigner` abstractions; `RsaPssLicenseSigner` (RSA-PSS/SHA-256); deterministic `LicensePayloadSerializer` and `LicenseContainerSerializer`; `LicenseIssuer` orchestration with structured `LicenseIssuanceResult`; and 35 issuer tests using ephemeral in-memory keys. No production private key, no CLI, no audit logging, no server wiring, no feature/edition/limit/expiration enforcement. | `tools/LabAuthServer.LicenseIssuer/*`, `tests/LabAuthServer.UnitTests/Licensing/Phase44LicenseIssuerTests.cs`, `tests/LabAuthServer.UnitTests/LabAuthServer.UnitTests.csproj`, `docs/plans/Phase-4/Phase-4.4-License-Issuer.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Change-Record.md` | Release build 0 warnings 0 errors; tests: Unit 589, Integration 246, total 835, 0 failed, 0 skipped (+35 Phase 4.4 tests) | Not committed | COMPLETE — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED |
| 2026-09-11 | 4.5 | Implemented the server-side license validator: injectable clock (`ILicenseClock`, `SystemLicenseClock`), structural bounds policy (`LicenseValidationPolicy`), the `ILicenseValidator` boundary, and `LicenseValidator` applying the documented order (input → parse → algorithm → trusted key → signature → product → edition → issuedAt/time → features → limits → expiry) with fail-closed exception handling and public-safe status mapping. Resolved M-1 (unknown container and payload properties rejected, duplicates still rejected) and M-2 (strict, non-normalising Base64 for payload and signature). Added `FieldUnknown`, `FeatureUnknown`, `LimitOutOfRange` reasons and `InvalidContent` status. No private key, no network/online activation, no feature enforcement. | `src/LabAuthServer.Application/Licensing/ILicenseClock.cs`, `ILicenseValidator.cs`, `LicenseValidationPolicy.cs`, `LicenseValidationReason.cs`, `LicenseValidationStatus.cs`, `src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs`, `JsonLicenseDocumentParser.cs`, `tests/LabAuthServer.UnitTests/Licensing/Phase45LicenseValidatorTests.cs`, `docs/plans/Phase-4/Phase-4-Implementation-Checklist.md`, `Phase-4-Change-Record.md` | Release build 0 warnings 0 errors; tests: total 872, 0 failed, 0 skipped (+37 Phase 4.5 tests) | Not committed | COMPLETE — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED |
| 2026-09-11 | 4.6 | Implemented the reusable feature and edition enforcement boundary: `LicenseFeatureIds` and `LicenseLimitKeys` catalogs, typed `LicenseFeatureDecision`, `ILicensePolicy` with `LicensePolicy` (`FromDocument`, `FromValidationResult`, `Restricted`), and `ILicensePolicyProvider`. Default-deny for absent, unknown and null features; edition never grants a feature implicitly; every non-valid validation status yields the restricted policy; a missing or unknown limit is never unlimited. No endpoint wiring, no middleware change, no feature-to-edition matrix (O-01 remains open). | `src/LabAuthServer.Domain/Licensing/LicenseFeatureIds.cs`, `LicenseLimitKeys.cs`, `src/LabAuthServer.Application/Licensing/LicenseFeatureDecision.cs`, `ILicensePolicy.cs`, `LicensePolicy.cs`, `ILicensePolicyProvider.cs`, `tests/LabAuthServer.UnitTests/Licensing/Phase46FeatureAndEditionEnforcementTests.cs`, `docs/plans/Phase-4/Phase-4.6-Feature-and-Edition-Enforcement.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Change-Record.md` | Release build 0 warnings 0 errors; tests: Unit 673, Integration 246, total 919, 0 failed, 0 skipped (+47 Phase 4.6 tests) | Not committed | COMPLETE — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED |
| 2026-09-11 | 4.7 | Implemented expiration and grace behavior: `LicenseExpirationPolicy` bounds, default-disabled bounded `LicenseGracePeriod`, `LicenseExpirationState`, `LicenseExpirationStatus`, and `ILicenseExpirationEvaluator` / `LicenseExpirationEvaluator` as the single decision point for the licensed time window. `LicenseValidator` now delegates its time checks to the evaluator; the three-argument constructor is unchanged. Perpetual licenses never expire; `expiresAt` stays exclusive; clock skew applies to `issuedAt` only and never widens the expiry side; expired licenses map to restricted policy; a configured grace period is diagnostic only and never grants capability. No configuration binding, no logging, no machine binding, no online activation. | `src/LabAuthServer.Application/Licensing/LicenseExpirationPolicy.cs`, `LicenseGracePeriod.cs`, `LicenseExpirationState.cs`, `LicenseExpirationStatus.cs`, `ILicenseExpirationEvaluator.cs`, `src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs`, `tests/LabAuthServer.UnitTests/Licensing/Phase47ExpirationAndGracePeriodTests.cs`, `docs/plans/Phase-4/Phase-4.7-Expiration-and-Grace-Period.md`, `Phase-4-Implementation-Checklist.md`, `Phase-4-Change-Record.md` | Release build 0 warnings 0 errors; tests: Unit 701, Integration 246, total 947, 0 failed, 0 skipped (+28 Phase 4.7 tests) | Not committed | COMPLETE — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED |

## Decision approval entry (2026-09-11)

Date:
2026-09-11

Phase:
4.0

Change:
Approved Phase 4 Technical License Enforcement baseline decisions.

Status:
APPROVED — PLANNING COMPLETE

Implementation:
NOT STARTED

## Planned phases (not started)

| Phase | Expected scope | Expected tests | Rollback point | Status |
| --- | --- | --- | --- | --- |
| 4.0 | Documentation only | None | Pre-phase commit | Not started |
| 4.1 | Documentation only | None | Pre-phase commit | Not started |
| 4.2 | Documentation only | None | Pre-phase commit | Not started |
| 4.3 | Crypto implementation | Round-trip and negative crypto tests | Pre-phase commit | Not started |
| 4.4 | Issuer tool | Issuer tests with ephemeral key | Pre-phase commit | Not started |
| 4.5 | Server validator | Rule-by-rule tests | Pre-phase commit | Not started |
| 4.6 | Feature enforcement | Denied/allowed and security-independence tests | Pre-phase commit | Not started |
| 4.7 | Expiration and grace | Boundary tests | Pre-phase commit | Not started |
| 4.8 | Machine binding (deferred) | Deferral assertion test only | Pre-phase commit | Deferred |
| 4.9 | Tamper resistance | Tamper tests | Pre-phase commit | Not started |
| 4.10 | Full licensing test suite | Full matrix | Pre-phase commit | Not started |
| 4.11 | CI integration | CI run evidence | Pre-phase commit | Not started |
| 4.12 | Operations | Procedure rehearsal | Pre-phase commit | Not started |
| 4.13 | Online activation (future) | Offline-independence tests | Pre-phase commit | Deferred |
| 4.14 | Documentation and release | Documented steps verified | Pre-phase commit | Not started |
| 4.15 | Final security review | Checklist evidence | Pre-phase commit | Not started |

## Recording rules

- One row per implemented phase.
- Record the exact files changed, not a summary.
- Record the test counts before and after.
- Record the commit hash only after an authorized commit exists.
- Record the rollback point before the phase begins, not after.
- Never edit a historical row; append a correction row instead.