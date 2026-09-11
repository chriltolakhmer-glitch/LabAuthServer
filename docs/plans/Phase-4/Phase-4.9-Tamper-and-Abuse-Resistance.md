# Phase 4.9 — Tamper and Abuse Resistance

Status: VERIFIED — NO NEW PRODUCTION CODE; NOT COMMITTED; NOT PUSHED. [Phase 4 README](Phase-4-README.md) | Previous: [4.8](Phase-4.8-Machine-Binding.md).

Implementation note (2026-09-11): this phase added no production code, consistent with the plan's own statements that "Files likely to change: None in this phase (planning only)" and "Documentation only in this phase". The protections it describes were already implemented in Phases 4.3-4.7; Phase 4.9 adds a verification suite that exercises them and records the threat model and limitations. See the implementation record below.

## Objective

Document realistic tamper and abuse protections, and state explicitly what cannot be protected.

## Scope

In scope:

- Realistic protective measures.
- Abuse scenarios and expected outcomes.
- The explicit limitation of source-delivered enforcement.

Out of scope:

- Impossible or deceptive anti-tamper claims.
- Code obfuscation as a security control.
- Client-side attestation.

## Why it exists

Overclaiming here creates contractual and expectation risk. Underclaiming leaves ordinary misuse unprotected. The document records the honest boundary.

## Prerequisites

- 4.2 through 4.7 defined.

## Inputs

- The threat model in this document.
- The [Risk Register](Phase-4-Risk-Register.md).

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.9-1 | Signed licenses are the primary authenticity control | PROPOSED | Cryptographic, not obscurity-based |
| D4.9-2 | Validation is default-deny | PROPOSED | Fail closed |
| D4.9-3 | Validation is separated from feature implementation | PROPOSED | A single bypass does not automatically unlock every feature |
| D4.9-4 | Multiple validation points are used only where justified | PROPOSED | Avoids maintenance cost without real benefit |
| D4.9-5 | No obfuscation or anti-debugging measures are claimed as protection | PROPOSED | They do not survive source delivery |
| D4.9-6 | Stronger enforcement is deferred to a future online service | PROPOSED | Server-side validation is the only meaningful upgrade |

DECISION REQUIRED: Which features, if any, warrant a second validation point beyond startup and periodic revalidation. Recommendation: none in the first implementation; add only with a documented justification.

## Proposed architecture

Realistic protections:

| Measure | Protects against | Does not protect against |
| --- | --- | --- |
| Signed license | Edited fields, forged content | Removal of the check in source |
| Default-deny validation | Missing, malformed, unknown licenses | A build with validation removed |
| Trusted key set with key IDs | Licenses signed by an unknown key | Possession of the vendor private key |
| Separation of validation from enforcement | One bypass unlocking everything | A coordinated source-level removal |
| Expiry enforcement | Perpetual use of an expired license | Clock manipulation, partially |
| Integrity checks on the license file | Accidental corruption | Deliberate source modification |
| Future online validation | Revocation, activation counting | Offline deployments that must remain offline |

Abuse scenarios and expected outcomes:

| Scenario | Expected outcome |
| --- | --- |
| Customer edits a field in the license | Signature verification fails; deny |
| Customer copies another customer's license | Valid if signed by a trusted key and not bound; see 4.8 |
| Customer reuses a license past expiry | Deny after expiry plus grace |
| Customer moves the system clock backward | Limited by skew rules; documented partial protection |
| Customer deletes the license file | Missing-license deny |
| Customer removes the validation call in source | Possible; out of technical scope; legal remedy |

Required statement:

> Because the customer receives source code, technical enforcement can be modified by a sufficiently capable party. The objective is to prevent accidental/ordinary misuse and provide cryptographically authentic licensing, not to claim unbreakable DRM.

## Files likely to change

- None in this phase (planning only).
- Later phases may add integrity checks within the validation component.

## Files that must NOT change

- Authentication and security controls.
- Deployment and CI configuration.

## Implementation steps

1. Confirm the threat model with project ownership.
2. Resolve the additional-validation-point decision.
3. Publish the limitation statement alongside the release notes (4.14).
4. Record decisions in the [Decision Log](Phase-4-Decision-Log.md).

## Security considerations

- Do not let licensing measures interfere with security controls.
- Do not introduce integrity checks that can fail open.
- Do not make the application depend on unverifiable environmental signals.

## Failure cases

- Claiming unbreakable protection and failing a customer audit.
- Adding obfuscation that breaks legitimate debugging and support.
- Integrity checks that deny a valid license after a routine file-system event.

## Testing requirements

- Edited-field test denies.
- Truncated-file test denies.
- Replaced-file test denies.
- Expired-license test denies after grace.
- Test proving a licensing failure does not disable authentication.
- Test proving no integrity check fails open.

## Acceptance criteria

- Realistic protections documented.
- The limitation statement present verbatim.
- Abuse scenarios tabulated with expected outcomes.
- No impossible claims made.

## Rollback considerations

Documentation only. Later integrity checks must be individually revertable without affecting validation of valid licenses.

## Evidence to record

- Confirmed threat model.
- Additional-validation-point decision.
- The published limitation statement.

## Git/commit strategy

- Documentation only in this phase.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.2 through 4.7.

## Risks

- Customer expectations exceeding what source-delivered enforcement can deliver.
- Over-engineering tamper measures at the cost of maintainability.

## Deferred items

- Online revocation (4.13).
- Server-side feature execution for high-value features.
- Code signing of the distributed binary as a separate integrity story.

## Implementation record (Phase 4.9 verification, 2026-09-11)

Scope: verification and documentation only. No production code was added, no license-format change was made, and no anti-debugging, obfuscation, hostile-environment or integrity-check mechanism was introduced.

Threat model, classified by whether the current design protects against it:

| ID | Threat | Protected? | Mechanism |
| --- | --- | --- | --- |
| A | Customer modifies the license file | Yes | RSA-PSS signature verification over the exact payload bytes |
| B | Customer modifies the signature | Yes | Signature verification fails closed |
| C | Customer changes keyId | Yes | `KeyUntrusted`; unknown identifiers are never trusted |
| D | Customer changes algorithm | Yes | Only `RSA-PSS-SHA256` is accepted (`AlgorithmUnsupported`) |
| E | Customer injects unknown JSON properties | Yes | Strict allow-list on container and payload (`FieldUnknown`) |
| F | Customer modifies expiration/features/limits | Yes | Covered by the signature; semantic bounds re-checked after verification |
| G | Customer supplies large or malformed input | Partly | Bounded parsing (`MaxDepth` 16), strict UTF-8, no BOM, strict Base64, documented feature/limit bounds |
| H | Customer attempts repeated validation abuse | Partly | Validation is pure and allocation-bounded; no rate limiting exists because no network surface consumes it yet |
| I | Customer modifies application source code | No | Documented limitation; out of technical scope |
| J | Customer modifies compiled binaries | No | Documented limitation; out of technical scope |
| K | Customer controls the host administrator account | No | Documented limitation; out of technical scope |
| L | Customer obtains the vendor private signing key | Prevented by custody | The key is never distributed, committed, placed in CI or present in the server (D-16, D-17, D-18) |

Protections verified, all pre-existing:

| Control | Where |
| --- | --- |
| Signed license over exact bytes | `RsaPssLicenseSignatureVerifier` (Phase 4.3) |
| Strict container/payload parsing, allow-list, duplicate rejection | `JsonLicenseDocumentParser` (Phases 4.2, 4.5) |
| Bounded depth, strict UTF-8, no BOM, strict Base64 | `JsonLicenseDocumentParser` |
| Structural bounds | `LicenseValidationPolicy` (Phase 4.5) |
| Default-deny feature/edition decisions | `LicensePolicy` (Phase 4.6) |
| Single expiration decision point | `LicenseExpirationEvaluator` (Phase 4.7) |

Validator ordering: parsing and cryptographic verification complete before any semantic trust, so a payload edit is reported as `InvalidSignature` rather than as a field error. No tampered document reaches semantic validation.

Source-available limitation (restated): a customer with full source and administrative control can modify feature checks, policy checks, validators, binaries, startup and configuration. Technical enforcement therefore cannot be made impossible to bypass. The objective is cryptographic authenticity and fail-closed behavior in the unmodified distributed application, not unbreakable DRM. This matches the required statement already recorded above and in [Phase 4.9's limitation clause](Phase-4.9-Tamper-and-Abuse-Resistance.md).

Offline-copy limitation: offline-first licenses without machine binding or online revocation can be copied between compatible installations and will remain cryptographically valid. This is a property of the approved licensing model (D-02, D-09, D-10, D-22), not a defect. It is not classified as a vulnerability.

Private-key security: no private key exists in server source, tests, CI, configuration or the license itself; the issuer obtains key material only through `ILicenseSigningKeyProvider`, and the server verifier requires public keys only.

Key rotation: multiple trusted public keys remain supported, key IDs are explicit, unknown IDs fail closed, duplicate IDs are rejected, and the provider stores public-only copies (F-3 resolution, Phase 4.3).

Tests added: `tests/LabAuthServer.UnitTests/Licensing/Phase49TamperAndAbuseResistanceTests.cs`. Every fixture issues a real license through the Phase 4.4 issuer with an ephemeral in-memory key, then mutates it. Coverage: payload, signature, keyId, algorithm and individual field tampering; unknown container and payload properties; malformed, non-object, invalid UTF-8, BOM, trailing-data and malformed/whitespace Base64 input; oversized feature arrays and deeply nested JSON; wrong key, weak key and rotated-key behavior; duplicate trusted key rejection; and fail-closed guarantees that no tampered or missing license yields a valid result or grants a commercial feature.

Still deferred: the additional-validation-point decision (O-15); operator-visible abuse logging (no logging surface exists and none was added); code signing of the distributed binary; online revocation (4.13); server-side execution of high-value features. Phase 4.10 is NOT started.