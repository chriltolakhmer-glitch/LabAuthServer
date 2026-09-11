# Phase 4.16 — Production Licensing Readiness

Status: COMPLETE. [Phase 4 README](Phase-4-README.md) | Previous: [4.15](Phase-4.15-Final-Security-Review.md).

## 1. Objective

Complete the remaining non-blocking/conditional Phase 4 licensing work identified by the Phase 4.15 Final Security Review so the offline licensing implementation is operationally ready for controlled production/customer use, without weakening any security boundary and without altering the license format or cryptographic profile.

## 2. Implemented Runtime Behavior

New production components:

| Component | Location | Purpose |
| --- | --- | --- |
| `ILicenseFileReader`, `LicenseFileReadResult`, `LicenseFileReadStatus` | `src/LabAuthServer.Application/Licensing/ILicenseFileReader.cs` | Typed, bounded license-file read contract |
| `BoundedLicenseFileReader` | `src/LabAuthServer.Infrastructure/Security/Licensing/BoundedLicenseFileReader.cs` | Reads the configured file within a hard size bound |
| `LicensePolicyProvider` | `src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs` | Loads and validates once, exposes the effective policy |
| `TrustedKeySetFactory` | `src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs` | Populates the public-only trusted key set from configuration |
| `LicenseConfigurationExtensions.AddLicenseConfiguration` | `src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs` | DI wiring for the licensing subsystem |
| `LicenseValidationOptions` (extended) | `src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs` | Adds `MaximumLicenseFileBytes` and `TrustedKeys` |

`Program.cs` registers the subsystem and resolves `ILicensePolicyProvider` once at startup inside a guarded block; a licensing condition never crashes startup.

## 3. License Loading Lifecycle

- The license is loaded and validated **once at application startup** when `ILicensePolicyProvider` is first resolved.
- **License replacement requires application reload/restart.** No background polling or hot reload is implemented.
- Configuration is read from the `Licensing` section. `Licensing:LicenseFilePath` empty means no license configured.
- If the license file is replaced while the application runs, the change takes effect only after a reload/restart.

## 4. File-Size Boundary

- `LicenseValidationOptions.MaximumLicenseFileBytes` defaults to 64 KiB.
- `BoundedLicenseFileReader` rejects on declared file length before reading, and again on bytes actually read, so an oversized or growing file is never buffered unbounded.
- Oversized input yields `LicenseFileReadStatus.TooLarge` and therefore the restricted Community policy.

## 5. Restricted Behavior

Every non-success path yields `LicensePolicy.Restricted` (Community): no feature granted, no limit defined. Covered paths: empty path, missing file, unreadable file, oversized file, malformed content, invalid signature, unknown key, unsupported version, expired license, invalid feature/limit data. An invalid license never bypasses authentication or authorization; licensing is additive and default-deny.

## 6. Logging

`LicensePolicyProvider` logs structured security/operational metadata only: license not loaded (with the typed read status), validation failed (status + internal reason), and license loaded and validated (edition, restricted flag). It never logs the license JSON, payload, signature, key material or customer secrets. A test asserts that sensitive material is not emitted.

## 7. Production Key Custody (decision requirements — NOT decided here)

- The vendor private signing key is offline and vendor-controlled; the server never receives it.
- Customer installations receive only trusted public verification material.
- CI never receives the production private key.
- Production public keys are provisioned through `Licensing:TrustedKeys` as PEM `BEGIN PUBLIC KEY` entries; `TrustedKeySetFactory` rejects any entry containing private material.
- Backup/recovery, access control, rotation and compromise procedures are **operational decisions** and remain to be finalized by operations. No production key storage technology is selected or invented here.

## 8. Signing Profile Freeze

The current, frozen issuer profile for License Version 1:

- Algorithm: RSA-PSS; Hash: SHA-256; algorithm identifier `RSA-PSS-SHA256` (ordinal match).
- RSA minimum: 2048 bits (`LicenseConstants.MinimumRsaKeySize`); initial issuer profile: RSA-3072.
- Signature representation: raw signature bytes, strict Base64 (no whitespace).
- Exact signed bytes: the decoded payload bytes, verbatim; no reserialization before verification.
- `keyId`: identifies the trusted verification key.
- Payload serialization: fixed property order `licenseVersion, licenseId, product, edition, customer, issuedAt, expiresAt, features, limits`; camelCase names; no whitespace; UTF-8 without BOM; UTC ISO 8601 at second precision with `Z` suffix; `expiresAt` null for perpetual; features in array order; limits in ordinal key order.

This is the current frozen issuer profile for License Version 1. It is not a formal canonicalization standard; the canonicalization scheme (O-03) remains open for a future license format version.

## 9. Operational Replacement

Documented procedure (no in-place truncation): prepare the replacement license outside the active file, validate it, atomically replace the active file, restart/reload, validate again, and remain restricted if invalid. A recovery copy is preserved.

## 10. Release Handling

The customer license is an external file supplied by deployment configuration via `Licensing:LicenseFilePath`. It is not bundled into the binary, not embedded in source, not a static web file, not committed to Git, and not copied into publish output. No customer-specific path is hardcoded.

## 11. CI Verification Status

`.github/workflows/ci.yml` was reviewed and is unchanged: restore, Release build, full test suite; `permissions: contents: read`; no secrets; no production signing key; no deployment. **Hosted CI run not verified in this phase.** The local Release equivalent passed 1007 tests.

## 12. Open Decisions and Deferred Work

- O-01 feature-to-edition matrix: BUSINESS DECISION REQUIRED (technical default-deny model enforced).
- O-07 production key source: operational decision (documented, not final).
- O-10 concrete license file location: deployment-supplied via configuration.
- O-17/O-18 issuance authority and register: deferred.
- O-20/O-21 visibility and release vehicle: deferred.
- Machine binding (D-09) and online activation/revocation (D-10/D-22): DEFERRED.
- Hot reload, per-request revalidation, operator-visible expiry-warning logging: DEFERRED.

## 13. Tests

`tests/LabAuthServer.UnitTests/Licensing/Phase416RuntimeLicenseLoaderTests.cs` (16 tests): empty path, missing file, valid license, malformed file, invalid signature, unknown key, expired license, unsupported version, oversized file, unreadable file, restricted-policy denial, sensitive-logging checks (valid and invalid), bounded-reader size rejection, bounded-reader empty-path status, and trusted-key-set public-key acceptance with unparsable/invalid entries skipped.

Full solution: **1007 tests, 1007 passed, 0 failed, 0 skipped** (Release) — 761 unit + 246 integration; baseline 991 preserved, +16 Phase 4.16 tests.

## 14. Security Review

- Fail closed on every path; invalid/missing license yields restricted Community.
- Bounded license-file input; no unbounded read.
- `Licensing:LicenseFilePath` is administrator-controlled deployment configuration; the loader reads exactly that path and exposes nothing over HTTP and nothing as static content.
- No private-key exposure; only public keys are provisioned; private PEM blocks are rejected.
- No sensitive logging; tests assert it.
- No authentication/authorization weakening; no cryptographic weakening; no new network dependency; no online activation; no machine binding; no insecure fallback; no default "unlimited" or default feature enablement.
</parameter>