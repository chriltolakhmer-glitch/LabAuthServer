# Phase 4.3 — Cryptographic Signing and Verification

Status: APPROVED — PLANNING COMPLETE. Implementation NOT STARTED. [Phase 4 README](Phase-4-README.md) | Previous: [4.2](Phase-4.2-License-Document-Format.md).

Approved initial algorithm (2026-09-11): RSA-PSS with SHA-256 using RSA-3072. Multiple trusted license-signing public keys must be supported (key rotation). Exact signature encoding and canonicalization remain TO BE CONFIRMED DURING IMPLEMENTATION.

## Objective

Select and document the asymmetric signing design, including algorithm, parameters, hash, signature format, key identification and rotation.

## Scope

In scope:

- Trade-off evaluation of RSA-PSS, RSA PKCS#1 v1.5, ECDSA and Ed25519.
- A single recommended first algorithm with parameters.
- Signature format and hash.
- Key identifier scheme.
- Rotation and migration strategy.
- Public key distribution.

Out of scope:

- Key generation for production (vendor operational task, 4.12).
- Issuer CLI design (4.4).
- Server validation rule ordering (4.5).

## Why it exists

The algorithm choice determines key sizes, performance, library support, FIPS/compliance posture and long-term migration cost. It must be an explicit, recorded decision.

## Prerequisites

- 4.2 format frozen, including canonicalization.
- Confirmation of the target runtime and available cryptographic libraries (TO BE CONFIRMED DURING IMPLEMENTATION).

## Inputs

- Existing RSA policy in the project (RSA 2048–4096) and the dedicated JWT signing certificate.
- Repository dependency governance rules in `AGENTS.md`.

## Design decisions

Trade-off evaluation:

| Option | Strengths | Weaknesses | Notes |
| --- | --- | --- | --- |
| RSA-PSS | Widely supported, probabilistic, modern padding, natural fit with existing RSA policy | Larger signatures, slower verification than Ed25519, parameter choices matter | Aligns with the project's existing RSA posture |
| RSA PKCS#1 v1.5 | Maximum compatibility, simplest | Deterministic padding, older designs, more sensitive to implementation issues | Avoid for new designs unless compatibility demands it |
| ECDSA | Small keys and signatures, fast | Requires high-quality randomness per signature, nonce-reuse catastrophic, more complex verification | Acceptable but the operational risk is higher |
| Ed25519 | Fast, small, deterministic, misuse-resistant, simple API | Availability depends on runtime/library; not universally available in older .NET or FIPS-only environments | Preferred if verified available |

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.3-1 | Sign the canonical payload with an asymmetric signature; the vendor holds the only private key | APPROVED (D-11, D-16) | Core trust anchor |
| D4.3-2 | First implementation algorithm: RSA-PSS with SHA-256, 3072-bit key | APPROVED (D-11) | Matches the existing RSA policy, broadly supported, no randomness-reuse hazard of ECDSA |
| D4.3-3 | Hash: SHA-256 | APPROVED (D-11) | Standard, sufficient, available |
| D4.3-4 | Signature encoding: Base64 of the raw signature bytes | TO BE CONFIRMED DURING IMPLEMENTATION | Simple, deterministic to compare in tests |
| D4.3-5 | Key identifier: stable ASCII string, for example `lab-license-signing-2026` | TO BE CONFIRMED DURING IMPLEMENTATION | Rotation without changing the format |
| D4.3-6 | The server trusts a key set, not a single key (multiple trusted public keys) | APPROVED (D-12) | Enables overlap during rotation |
| D4.3-7 | Ed25519 remains a candidate for a future `alg` value | PROPOSED | Forward-compatible `alg` field already exists |

RESOLVED: Initial algorithm approved as RSA-PSS with SHA-256 and a 3072-bit key (D-11, 2026-09-11). Ed25519 remains a future candidate behind the `alg` field.

TO BE CONFIRMED DURING IMPLEMENTATION: Minimum acceptable RSA key size for acceptance (reject below 2048, warn below 3072 is the current recommendation). Also TO BE CONFIRMED: exact signature encoding/canonicalization.

Key rotation is required: multiple trusted license-signing public keys must be supported (D-12).

## Proposed architecture

Signing (vendor side):

1. Receive validated license parameters.
2. Build the payload object.
3. Canonicalize to bytes per 4.2.
4. Sign with the private key using the selected algorithm and hash.
5. Emit the license with the signature envelope containing `alg`, `keyId` and `value`.
6. Never log the private key; never log the full license if it carries customer data.

Verification (customer side):

1. Parse the envelope; extract `alg` and `keyId`.
2. Resolve `keyId` in the trusted key set. Unknown `keyId` is a hard deny.
3. Reject an `alg` that is not on the accepted algorithm list.
4. Reject a key whose parameters are below the accepted minimum.
5. Canonicalize the payload with the same rules used at signing time.
6. Verify the signature.
7. On success, proceed to semantic validation.

Key rotation:

- A new key gets a new `keyId` and a new key pair.
- The server's trusted key set is extended, not replaced.
- New licenses are signed with the new key.
- Old keys remain trusted until all licenses signed by them expire or are reissued.
- A documented retirement date is recorded per key.

Migration strategy:

- The `alg` field allows introducing a new algorithm without a format change.
- A future format change must use a new `licenseVersion` and dual-version support.

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: cryptographic verification component in the infrastructure layer.
- TO BE CONFIRMED DURING IMPLEMENTATION: trusted key set configuration.
- TO BE CONFIRMED DURING IMPLEMENTATION: signing component in the issuer tool.

## Files that must NOT change

- JWT signing and validation code.
- The dedicated JWT signing certificate and its configuration.
- TLS and SQL Server TLS configuration.
- Any existing key or certificate store location.

## Implementation steps

1. Resolve the algorithm and minimum-key-size decisions.
2. Verify library availability on the target runtime.
3. Implement signing in the issuer only.
4. Implement verification in the server only.
5. Add round-trip and negative cryptographic tests.
6. Document the key generation and rotation procedure (operational, 4.12).

## Security considerations

- Never accept a signature from an untrusted or unknown `keyId`.
- Never fall back to "skip verification" on error.
- Constant-time comparison where applicable; rely on vetted libraries rather than hand-rolled verification.
- Do not log raw signatures, keys or customer payloads.
- Reject keys below the minimum size rather than accepting them with a warning.

## Failure cases

- Unknown `keyId` treated as "trust all".
- A rotated key added by replacing the key set, invalidating existing licenses.
- Signing the file including the signature envelope, creating a circular dependency.
- Non-ASCII or whitespace differences between issuer and server canonicalization.

## Testing requirements

- Valid signature verifies.
- Tampered payload fails.
- Tampered signature fails.
- Wrong `keyId` fails.
- Unknown `alg` fails.
- Key below minimum size fails.
- Two trusted keys: a license signed by either verifies.
- Removed key: a license signed by the removed key fails.
- Cross-runtime canonicalization consistency test where feasible.

## Acceptance criteria

- Algorithm, parameters, hash and signature encoding recorded.
- Key identifier scheme recorded.
- Rotation procedure documented.
- Verification is default-deny for every unresolved condition.

## Rollback considerations

Reverting the implementation removes verification; the server returns to its pre-licensing behavior. Because no production licenses exist at this point, no customer impact. After issuance begins, rollback must keep the trusted key set intact.

## Evidence to record

- Algorithm decision with rationale and rejected alternatives.
- Key parameters used.
- Test results including negative cases.

## Git/commit strategy

- Implementation phase: separate commit from documentation.
- Suggested message: `feat(licensing): add signature verification with trusted key set`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Requires 4.2 canonicalization and envelope format.

## Risks

- Algorithm deprecation over the product lifetime.
- Library unavailability on the target runtime.
- Key set misconfiguration causing a full licensing outage.

## Deferred items

- Ed25519 adoption.
- Hardware-backed signing (4.12).
- Post-quantum migration planning.