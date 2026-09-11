# Phase 4.12 — Operational License Management

Status: DOCUMENTED — OPERATIONAL DESIGN ONLY. No production code added. [Phase 4 README](Phase-4-README.md) | Previous: [4.11](Phase-4.11-GitHub-CI-Integration.md).

> This phase is operational design and documentation only. It implements no runtime behavior, no license loader, no reload, no audit subsystem, no CLI and no configuration change. Every statement below is labelled as **IMPLEMENTED** (exists in code today), **APPROVED PROCEDURE** (approved decision or constraint), **RECOMMENDED** (proposed, not approved) or **OPEN** (unresolved decision). Where the current application has no runtime mechanism, that is recorded as a limitation rather than implemented.

## 1. Scope

In scope:

- The operational lifecycle of a license: issuance, storage, installation, validation, inspection, replacement, renewal, expiration, backup and recovery.
- Operator procedures and troubleshooting.
- License identification and auditability requirements.
- The vendor/customer key-custody boundary.
- Operational failure modes and their safe handling.
- Administrator-facing documentation.

Out of scope:

- Implementing any runtime procedure or tooling.
- Legal contract terms.
- Pricing.
- Online activation, online revocation, machine binding, HSM/vault integration, a license management web UI and automatic license download.

Phase 4.12 does **not** authorize resolving any OPEN decision that it does not own. Decisions it owns or feeds are named explicitly in section 22.

## 2. Current architecture

The following is **IMPLEMENTED** and verified against the current source tree.

| Element | Type | Location |
| --- | --- | --- |
| `LicenseValidationOptions` | Configuration model | `src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs` |
| `LicenseValidationStatus` | Public-safe status enum | `src/LabAuthServer.Application/Licensing/LicenseValidationStatus.cs` |
| `LicenseValidationReason` | Internal diagnostic reason enum | `src/LabAuthServer.Application/Licensing/LicenseValidationReason.cs` |
| `LicenseValidationResult` | Typed validation outcome | `src/LabAuthServer.Application/Licensing/LicenseValidationResult.cs` |
| `LicenseValidator` | Validator implementation | `src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs` |
| `JsonLicenseDocumentParser` | Strict container parser | `src/LabAuthServer.Infrastructure/Security/Licensing/JsonLicenseDocumentParser.cs` |
| `RsaPssLicenseSignatureVerifier` | Signature verification | `src/LabAuthServer.Infrastructure/Security/Licensing/RsaPssLicenseSignatureVerifier.cs` |
| `InMemoryTrustedLicenseKeyProvider` | Trusted public-key set | `src/LabAuthServer.Infrastructure/Security/Licensing/InMemoryTrustedLicenseKeyProvider.cs` |
| `LabAuthServer.LicenseIssuer` | Vendor-side issuer (outside the server build graph) | `tools/LabAuthServer.LicenseIssuer/` |

Configuration model (**IMPLEMENTED**):

- Section name is `Licensing` (`LicenseValidationOptions.SectionName`).
- The only setting is `LicenseFilePath`.
- `LicenseFilePath` defaults to `string.Empty`, which means **no license is configured**. The code comment states explicitly that no default path is hard-coded and that a missing configuration yields restricted mode rather than an exception.

Verified limitations (**IMPLEMENTED, recorded honestly**):

- There is **no runtime license-file loader** in the server. Nothing reads `LicenseFilePath` from disk at startup or during a request.
- There is **no reload or hot-swap** mechanism.
- There is **no DI registration** for `ILicenseValidator`, `ILicensePolicyProvider`, `ILicenseExpirationEvaluator` or `LicenseValidationOptions` in the API project.
- There is **no `Licensing` configuration section** in `appsettings*.json`.
- There is **no logging surface** in the licensing code (no `ILogger`, `LogWarning`, `LogError` or `LogInformation`).
- The issuer is outside `LabAuthServer.slnx` and is not referenced by any server project. Only `LabAuthServer.UnitTests` references it, so issuer tests can run.

Operational consequence: the procedures in this document describe how licensing **will** be operated once a loader and enforcement wiring exist. They do not describe behavior available in the current build.

## 3. License lifecycle

```
Vendor                     Customer deployment
------                     -------------------
1. Issue (sign)   ------>  2. Receive license file
                           3. Verify integrity/signature
                           4. Install to configured path
                           5. Validate (loader -> validator)
                           6. Confirm edition/features/limits
                           7. Confirm expiry/perpetual
                           8. Operate; revalidate per cadence
                           9. Replace or renew before expiry
                          10. Back up the license file
                          11. Recover from backup if lost/corrupt
                          12. Remove/retire on decommission
```

Lifecycle states of a license document: **Issued → Installed → Valid → (Expiring → Expired) | Perpetual → Replaced/Renewed → Retired.** A license that fails validation enters restricted mode per D-14; it does not stop the application (D-03).

## 4. Issuance

**APPROVED PROCEDURE (constraints)** — D-16, D-17, D-18, D4.12-6: the production vendor private signing key is never distributed, never committed to GitHub and never provided to CI or the customer. No issuance procedure may require sharing that key.

**RECOMMENDED (not approved)** — a production issuance run should:

1. Confirm the customer's entitlement against the commercial record.
2. Confirm the edition, feature list, limits and validity window to be signed.
3. Sign with the current trusted key using the issuer (RSA-PSS/SHA-256, D-11).
4. Record the issuance in the vendor register (see section 22, O-18).
5. Deliver the signed license document to the customer out of band.
6. Retain the issuance record for renewal and audit.

Authority and approval evidence are **OPEN** (O-17). Until O-17 is resolved, no production issuance run is authorized.

Issuer behavior is **IMPLEMENTED** (Phase 4.4): deterministic payload serialization, structured `LicenseIssuanceResult`, input validation against known editions/features/limits. It is not wired to any CLI or audit log.

## 5. Installation

The following is an **APPROVED PROCEDURE** shape, constrained by **IMPLEMENTED** configuration behavior.

1. Obtain the license from the authorized vendor source (section 4).
2. Verify integrity and signature out of band if a verification tool is available; otherwise rely on the server's validator (step 5).
3. Place the license file at the path configured by `Licensing:LicenseFilePath`.
4. Apply filesystem permissions so that only the service identity and administrators can read the file. The license contains customer entitlement data; treat it as confidential. It is not secret key material, but it is not public either.
5. Validate the license. **LIMITATION:** the current build has no loader, so validation only occurs when a loader and enforcement wiring exist. Until then, validation is performed by running the validator through the test harness or a future operator tool.
6. Confirm edition, features and limits from the validation result (section 6).
7. Confirm expiration or perpetual status.
8. Record the license ID and operational metadata in the customer's change record. Do not record the full payload, signature or any key material.
9. Restart or reload only if the actual implementation requires it. **LIMITATION:** no reload exists; once a loader exists, decide restart-vs-reload as part of that implementation, not in this document.
10. Confirm the application remains secure: authentication, authorization, TLS, LDAP transport security, request limits and audit must be unchanged. Licensing never weakens these (D-15).

Do not invent commands. No CLI or operator tool exists today to run these steps; that is a recorded limitation, not a missing procedure.

## 6. Validation

Validation is **IMPLEMENTED** as a pure function over license bytes plus the trusted public-key set (Phases 4.3–4.7). It is offline and deterministic (D-02).

The typed result (`LicenseValidationResult`) carries:

- `Status` (`LicenseValidationStatus`) — public-safe category.
- `Reason` (`LicenseValidationReason`) — internal diagnostic code, **never** surfaced through a public API response.
- `Policy` (`LicenseDocument`) — present only when validation succeeded.
- `IsValid` — true only when `Status == Valid`.

Validation order is fixed (`LicenseValidator`): input → parse → algorithm → trusted key → signature → product → edition → issuedAt/time → features → limits → expiry.

Operator diagnostics mapping (safe to record in a change ticket):

| Operator sees | Public-safe status | Internal reason (do not paste into customer-facing tickets) |
| --- | --- | --- |
| Valid license | `Valid` | `None` |
| No license configured or file missing | `Missing` | `LicenseMissing` |
| File present but unreadable | `Malformed` | `LicenseUnreadable` |
| Malformed document | `Malformed` | `LicenseMalformed` |
| Unknown format version | `UnsupportedVersion` | `VersionUnsupported` |
| Wrong product | `WrongProduct` | `ProductMismatch` |
| Malformed signature envelope | `InvalidSignature` | `SignatureMalformed` |
| Unknown key id | `UnknownKey` | `KeyUntrusted` |
| Unsupported algorithm | `InvalidSignature` | `AlgorithmUnsupported` |
| Signature does not verify | `InvalidSignature` | `SignatureInvalid` |
| Missing required field | `InvalidContent` | `FieldMissing` |
| Invalid field value (e.g. unknown edition) | `InvalidContent` | `FieldInvalid` |
| Unknown property present | `InvalidContent` | `FieldUnknown` |
| Unknown feature identifier | `InvalidContent` | `FeatureUnknown` |
| Limit out of allowed range | `InvalidContent` | `LimitOutOfRange` |
| Not yet valid | `NotYetValid` | `NotYetValid` |
| Invalid expiry instant | `InvalidContent` | `ExpiryInvalid` |
| Expired | `Expired` | `Expired` |
| Trusted key set or validation configuration unusable | `InvalidConfiguration` | `InvalidConfiguration` |

Rules:

- Do not alter validation semantics to improve messages.
- Do not expose `LicenseValidationReason` to the customer or through any public response.
- Do not weaken any validation rule to make a license pass.

## 7. Replacement

An **APPROVED PROCEDURE** shape for replacing a license safely:

1. Obtain the new license (renewal, edition upgrade, feature or limit change, or a corrected replacement).
2. Validate the new license **before** activating it. If validation fails, do not replace the live file.
3. Write the new license to a temporary file in the same directory as the live license.
4. Atomically move the temporary file over the live file (rename within the same volume) so a partial or interrupted write cannot leave a corrupt live license.
5. Keep the previous license file (renamed, not deleted) as the rollback copy until the new one is confirmed working.
6. Confirm validation again against the live path.
7. Record the replacement: previous license ID, new license ID, reason, operator, timestamp.
8. Remove the temporary and rollback copies once the new license is confirmed stable.

Triggers covered: expired license replacement, renewed license, edition upgrade, feature changes, limit changes, invalid/corrupted license, rollback to previous valid license.

**LIMITATION:** no hot reload exists. The replacement procedure assumes the new license is picked up on the next load; whether that requires a restart is a property of the loader, which is not implemented.

**RECOMMENDED (not approved):** validate-before-activate and atomic replacement should be enforced by tooling when a loader is implemented. They are not enforced by the current build because no loader exists.

## 8. Backup

**APPROVED PROCEDURE:**

- Include the license file at the configured `Licensing:LicenseFilePath` in the customer's documented backup set. This is a Phase 4.12 operational requirement and matches R-13 (disaster recovery restoring a stale or missing license).
- Back up the license file with the same confidentiality and access controls as the running copy. It contains customer entitlement data, not key material.
- Do not back up the vendor private signing key: it is **not** a customer artifact (D-16, section 15).
- Do not back up temporary or rollback copies once the replacement is confirmed stable.
- Do not place the license file in static web content, a publish output directory, or any path served by the application.

## 9. Recovery

**APPROVED PROCEDURE:**

1. Restore the license file from the documented backup set to the configured `Licensing:LicenseFilePath`.
2. Re-apply filesystem permissions (section 5, step 4).
3. Validate the restored license (section 6).
4. Confirm edition, features, limits and expiry/perpetual status.
5. If the restored license is invalid, expired or wrong, request a replacement from the vendor (section 7).

Recovery of the **vendor private signing key** is explicitly **out of scope for the customer**. The vendor private key is not a customer backup artifact and no customer procedure may assume the customer holds it. Vendor-side key recovery, if required, is a vendor operation governed by O-07 and is not defined in this document.

## 10. Expiration

**IMPLEMENTED** (Phase 4.7):

- `LicenseExpirationEvaluator` is the single decision point for the licensed time window.
- Perpetual licenses never expire.
- `expiresAt` is exclusive.
- Clock skew applies to `issuedAt` only and never widens the expiry side.
- Expired licenses map to the restricted policy (D-14) and the application continues in restricted mode (D-03). It does not refuse to start.

**APPROVED PROCEDURE:**

- Track `expiresAt` in the vendor register and the customer change record.
- Replace or renew the license **before** `expiresAt` (section 11).
- Do not extend the window by manipulating the system clock; the evaluator applies bounded skew only on `issuedAt` (R-05).

## 11. Renewal

**APPROVED PROCEDURE** (D4.12-2): renewal issues a **new** signed license rather than modifying the existing one. Signatures are immutable; editing a license invalidates it.

Recommended renewal timeline (**RECOMMENDED, not approved**):

- Vendor: start renewal contact well before expiry; the exact lead time is not yet defined and is an operational decision for 4.12 follow-up, not a Phase 4.12 requirement.
- Customer: apply the replacement procedure (section 7) once the renewed license is received.
- Do not rely on a grace period: none is configured by default (section 20, D-08).

**LIMITATION:** the current build emits no expiry warnings because there is no logging surface (section 21). The renewal timeline therefore cannot yet be driven by application logs.

## 12. Edition / feature changes

**APPROVED PROCEDURE:** an edition upgrade, feature addition, feature removal or limit change is delivered as a **new signed license** (same as renewal). It is not an in-place edit.

- Confirm the new entitlement against the commercial record.
- Issue the new license (section 4).
- Replace the installed license (section 7).
- Confirm the new edition/features/limits through the validator result (section 6).

Default-deny applies (D-20): unknown editions, unknown features and out-of-range limits are rejected, never silently widened. The exact feature-to-edition matrix remains **OPEN** (O-01, partial).

## 13. Operator troubleshooting

Diagnostic entry points available today:

- `LicenseValidationStatus` (public-safe) and `LicenseValidationReason` (internal) from `LicenseValidationResult`.
- The trusted key set (`InMemoryTrustedLicenseKeyProvider`) keyed by `keyId`.
- The configured path (`Licensing:LicenseFilePath`) and the configured `Licensing` section name.

What an operator can safely determine, by status:

- `Valid` — the license is valid for the current time.
- `Missing` — no license present; the application runs in Community/restricted mode.
- `Malformed` — the license could not be read or parsed.
- `UnsupportedVersion` — the format version is newer/older than this build supports.
- `WrongProduct` — the license was issued for a different product.
- `InvalidSignature` — the signature did not verify.
- `UnknownKey` — the `keyId` is not in the trusted key set (possible rotation gap).
- `Expired` — the license has expired.
- `NotYetValid` — the license is not yet valid.
- `InvalidConfiguration` — the trusted key set or validation configuration is unusable.
- `InvalidContent` — the license parsed and verified but a property, edition, feature or limit is unknown or out of range.

Rules:

- Do not paste the internal `Reason` into customer-facing output.
- Never request or transmit the vendor private signing key to diagnose a problem (D4.12-6).
- Never weaken a validation rule to unblock a customer; issue a corrected license instead.
- Preserve the license file (and its previous copy) for diagnosis; do not delete it.

## 14. Vendor / customer responsibility boundary

| Responsibility | Vendor | Customer |
| --- | --- | --- |
| Owns signing authority | Yes | No |
| Protects the production private signing key | Yes | No |
| Issues licenses | Yes | No |
| Controls key rotation and the trusted key set | Yes | No |
| Provides licenses to customers | Yes | No |
| Receives the signed license | No | Yes |
| Stores the license at the configured path | No | Yes |
| Holds only the license/public verification material (as designed) | No | Yes |
| Generates a valid vendor signature | Only the vendor | Never |
| Backs up the license file | No | Yes |
| Recovers a lost license from backup | No | Yes |
| Requests a replacement or renewal | On request | Yes |
| Diagnoses licensing problems without the private key | Yes (with diagnostics) | Yes (with diagnostics) |

## 15. Private-key custody

**APPROVED CONSTRAINTS** — D-16, D-17, D-18, D4.12-6:

- The production vendor private signing key is never distributed with LabAuthServer.
- It is never committed to GitHub.
- It is never provided to GitHub Actions.
- No operational procedure in this document requires the vendor private key to be shared with a customer or a support engineer.
- The vendor private key is **not** a customer backup artifact (section 8, section 9).
- The private key source for production is **OPEN** (O-07). Until O-07 is resolved, no production signing configuration is approved.

**IMPLEMENTED:** the issuer signs through `ILicenseSigningKeyProvider` / `ILicenseSigner`, with an `RsaPssLicenseSigner`. Test runs use ephemeral in-memory keys. No production key material exists in the repository.

## 16. Security requirements

Operational procedures must preserve:

- Fail-closed behavior on every path (D-14).
- Cryptographic verification (RSA-PSS/SHA-256, D-11).
- Trusted-key validation against a key set, not a single key (D-12).
- Strict parsing (no unknown container or payload properties, no duplicates, strict non-normalising Base64).
- No private-key distribution (D-16).
- No authentication bypass; licensing must never weaken authentication or security controls (D-15).
- Community/restricted fallback when the license is missing, invalid or expired (D-03).
- No source-code secrets and no production credentials in the repository.
- No network dependency introduced by licensing (D-02).

## 17. Source-available limitations

Because LabAuthServer is delivered as source, operational licensing controls cannot prevent a customer with full source and administrative control from modifying the application. The approved constraint recorded in [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md) applies verbatim and is not contradicted here.

Operational documentation must not promise:

- unbreakable DRM;
- impossible bypass;
- anti-debugging or anti-administration;
- hardware-level enforcement.

The objective is legal licensing, cryptographically authentic licenses, default-deny enforcement and resistance to ordinary misuse — not unbreakable DRM.

## 18. Machine-binding status

Machine binding remains **DEFERRED** (D-09, O-14 resolved as deferred; [4.8](Phase-4.8-Machine-Binding.md) design-only). No license installation procedure in this document depends on machine identity. No binding field, check or rebind tool exists.

## 19. Online activation / revocation status

Online activation and online revocation are **DEFERRED** (D-10, D-22; [4.13](Phase-4.13-Online-Activation-Future.md) future work). No procedure in this document depends on a network call, a licensing server, telemetry or a cloud service. Offline licenses cannot be recalled; revocation is only meaningful with a future online component.

## 20. Grace-period status

**IMPLEMENTED** (Phase 4.7): no grace period is enabled. D-08 approves no grace in the initial implementation. The `LicenseGracePeriod` type exists with a bounded maximum (90 days) and is default-disabled; a configured grace period is diagnostic only and never grants capability. Grace must not be turned into an operational bypass. If a grace policy is later approved, it must be bounded, documented and never permanent.

## 21. Audit / logging status

**LIMITATION:** the current build has no operational license audit surface. There is no `ILogger` in the licensing code, no expiry-warning log and no runtime loader to log a load event.

**RECOMMENDED (not implemented):** if and when a loader and enforcement wiring are implemented, the following should eventually be logged — without ever logging key material, the full license payload or the signature:

- License load attempt: configured path present/absent, outcome status (public-safe only).
- Validation outcome on load and on revalidation: public-safe status only.
- Expiry approaching: license ID and `expiresAt`.
- Expired / not-yet-valid transitions.
- Replacement or reload event: previous license ID, new license ID, source operator.
- Configuration errors (unusable trusted key set).

Do not log `LicenseValidationReason` values into customer-visible output. Do not log license payload bytes, the signature, or any key material.

## 22. Open decisions

The following decisions remain **OPEN**. Phase 4.12 is **not** authorized to resolve them, so they are recorded here rather than silently decided.

| ID | Question | Owner | Phase 4.12 stance |
| --- | --- | --- | --- |
| O-17 | Issuance authority and approval evidence | Operations | Remains OPEN. No production issuance run is authorized until O-17 is resolved. |
| O-18 | Vendor license register format | Operations | Remains OPEN. Register fields are described in this document; the storage format is not chosen. |
| O-20 | Customer-visible vs internal documents | Project owner | Remains OPEN. This document is treated as internal until O-20 is resolved. |
| O-07 | Private key source for the issuer | Operations | Remains OPEN. No production signing configuration is approved. |
| O-10 | License file location | Operations | Remains OPEN. Only the configurable-path **model** is approved (D-13). No concrete default path is set. |
| O-09 | Validation cadence | Architect | Remains OPEN. Installation/replacement procedures assume revalidation on load; the cadence is not decided. |
| O-13 | Clock-skew allowance value | Architect | Remains OPEN. The evaluator applies bounded skew on `issuedAt` only; the value is configurable but not finalized. |
| O-15 | Additional validation points | Architect | Remains OPEN. None added. |
| O-03 | Canonicalization profile | Architect | Remains OPEN. Issuer serialization is deterministic; the canonicalization profile is not finalized. |
| O-01 | Feature-to-edition matrix | Architect | PARTIALLY RESOLVED. Editions approved (D-04); exact matrix remains OPEN. |
| O-06 | Minimum RSA key-size wording | Architect | IMPLEMENTED as 2048 in code; wording remains OPEN. |

The Phase 4.12 design decisions D4.12-1 through D4.12-6 are unchanged in status: D4.12-1, D4.12-2, D4.12-3, D4.12-4 and D4.12-5 remain **PROPOSED**; D4.12-6 remains **FINAL (constraint)**.

## 23. Deferred work

The following are out of Phase 4.12 scope and are recorded as deferred:

| Item | Reason |
| --- | --- |
| Runtime license loader and reload | Not authorized by Phase 4.12; depends on O-10 and O-09 |
| Operator CLI or verification tool | Not authorized; no such tool exists |
| Runtime audit/logging surface for licensing | Not authorized; depends on the loader |
| Customer self-service portal | Deferred by the original 4.12 plan |
| Automated renewal reminders | Deferred; depends on the logging surface |
| Revocation workflow | Deferred to 4.13 |
| Online activation | Deferred (D-10, D-22) |
| Machine binding | Deferred (D-09) |
| HSM/vault integration and production key provisioning | Out of scope; governed by O-07 |
| License management web UI | Out of scope |
| Automatic license download | Out of scope |
| Deployment automation | Out of scope |
| Release gating on 4.15 | 4.15 not started |

## 24. Security review

Search performed over the changed files:

- `BEGIN PRIVATE KEY`, `BEGIN RSA PRIVATE KEY`, `ExportRSAPrivateKey`, `ExportPkcs8PrivateKey` — none present. The document refers to these only as terms to prohibit.
- `.pfx`, `.p12` — none present. `.gitignore` excludes `*.pfx`, `*.p12`, `*.key`, `secrets.json` and `appsettings.Local.json`.
- `credentials`, `password`, `token`, `secret`, `production key`, `signing key` — references are descriptive only; no real values.

Additional checks:

- No hardcoded production path was introduced. `LicenseValidationOptions.LicenseFilePath` remains empty by default; the document does not choose a concrete location (O-10 OPEN).
- No credentials and no unsafe filesystem permission instructions were introduced; the document requires restrictive permissions and does not specify a world-readable location.
- No static web exposure is introduced; the document forbids placing the license in static content or a publish output path.
- No license file was committed. No backup procedure distributes the vendor private key; section 8 and section 9 explicitly exclude it.
- No instructions transmit the vendor private key to support or to a customer.

## 25. Final validation

- No production code was changed in Phase 4.12.
- Release build: succeeded, 0 warnings, 0 errors.
- Full test suite: 991 passed, 0 failed, 0 skipped (baseline 733 preserved and exceeded) when the solution is run with `dotnet test LabAuthServer.slnx -c Release --no-build -m:1`.
- **Pre-existing test-isolation flake recorded (not caused by Phase 4.12):** running the solution with MSBuild parallelism enabled (`dotnet test LabAuthServer.slnx -c Release --no-build`) intermittently fails exactly one integration test, `JwtSizeBoundaryTests.InconsistentPayloadOverride_FailsOptionsValidation`, while `LabAuthServer.UnitTests` reports 745/745 every time. The test:
  - passes in isolation (`--filter FullyQualifiedName~JwtSizeBoundaryTests.InconsistentPayloadOverride_FailsOptionsValidation`),
  - passes in a full run of `LabAuthServer.IntegrationTests` alone (246/246),
  - passes when the two test assemblies do not run concurrently (`-m:1`).
  It is a JWT issuance-budget/options-validation test in the authentication path. It does not touch licensing, does not depend on the license validator, the trusted key set, the issuer or the parser, and it is not related to the Phase 4.12 documentation change. The flake is recorded as an OPEN finding for a future test-infrastructure phase; the test was not weakened, disabled, retried or skipped, and no retry policy was added.
- `git diff --check`: clean.
- No private key, secret, production credential, production license file or production signing configuration was introduced.
- No online activation, revocation, machine binding, HSM/vault, audit subsystem, loader, reload or deployment capability was introduced.
- Operational documentation reflects implemented behavior; where behavior does not yet exist, it is labelled as a limitation or a recommendation rather than a requirement.
- **Limitation recorded:** the server has no runtime license loader, no reload, no DI registration and no logging surface. Until those exist, the installation, replacement and recovery procedures in this document are operational design, not executable steps.