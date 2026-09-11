# Licensing

LabAuthServer uses a signed, offline license document to carry commercial entitlement (edition, features, limits and validity window). This page documents the licensing subsystem as it is implemented today, and clearly separates what is implemented from what is deferred or future work.

## Audience

This page is for customer operators and technical staff. It is not a substitute for the vendor-side issuer runbook (internal) or for the Phase 4 plan set.

## What licensing currently does

- The vendor signs a license document with a vendor-controlled RSA private key.
- LabAuthServer ships only the vendor public key(s); it never holds the vendor private signing key.
- LabAuthServer verifies the signature offline, then validates the license content (product, edition, features, limits, validity window) using a strict allow-list parser.
- A valid license produces a typed policy that the application can use to decide whether a licensed feature is available.
- A missing, invalid, unsupported or expired license places the application in Community/restricted mode. The application does not refuse to start.
- Validation is a pure function of license bytes plus the trusted public-key set. It requires no network access.

## What licensing does not do today

- It does not contact a licensing server. No online activation is implemented.
- It does not implement online revocation. Offline licenses cannot be recalled.
- It does not implement machine binding. No machine identity is required.
- It does not implement a grace period. Grace is default-disabled and diagnostic only.
- It does not provide hot reload or per-request license revalidation. License state is loaded at startup and replacement requires restart/reload.

> The approved issuance, custody, register, delivery, replacement and recovery procedures are documented in [Phase 4.17 — Production License Governance and Final Sign-Off](plans/Phase-4/Phase-4.17-Implementation-and-Final-Sign-Off.md).

## License document concept

A license is a signed JSON container. It contains:

- A format version.
- The product identifier.
- The licensed edition (`Community`, `Professional` or `Enterprise`).
- An explicit feature list.
- Optional numeric limits (for example maximum users).
- An issuance timestamp and an optional expiry instant (perpetual licenses omit the expiry).
- A signature envelope with `algorithm`, `keyId` and a Base64 `signature`.

The signature is computed over the exact signed payload bytes. The container is parsed strictly: unknown container or payload properties are rejected, duplicate properties are rejected, JSON nesting depth is bounded, UTF-8/BOM handling is strict, and Base64 decoding is strict (no normalisation).

## Supported editions

Community, Professional and Enterprise are the approved editions. The approved matrix permits Community features `auth.basic` and `auth.jwt`; Professional additionally permits `auth.ldap` and `audit.logging`; Enterprise permits all five known features, including `admin.console`. The matrix is an upper bound: edition alone never grants a feature, and the explicit feature list in the signed license remains authoritative.

## Features

Licensed features are identified by known feature identifiers defined in the code. A feature must be present in the signed license for the application to consider it licensed. An unknown feature identifier is rejected; a feature that is absent is denied. Default-deny is the rule.

## User limits

A license may carry numeric limits (for example a maximum user count). Limits are validated against an allowed range. A missing or unknown limit key is never treated as unlimited; out-of-range or negative values fail validation.

## Perpetual licenses

A perpetual license omits the expiry instant. It never expires as long as the signature verifies against a trusted key and the other validations pass.

## Expiring licenses

A time-limited license carries an `expiresAt` instant. The expiry instant is exclusive. The single decision point for the licensed time window is the expiration evaluator. Clock skew applies to the issuance timestamp only and never widens the expiry side of the window.

## Restricted Community behaviour

If the license is missing, invalid, unsupported, wrong-product, expired or malformed, the application enters Community/restricted mode rather than silently continuing or refusing to start. Licensing never disables or weakens authentication, authorization, TLS, LDAP transport security, request limits or audit. Licensing restricts commercial functionality only.

## License validation

Validation is performed in a fixed order: input → parse → algorithm → trusted key → signature → product → edition → issuedAt/time → features → limits → expiry. The validator returns a typed result:

- A public-safe status category.
- An internal diagnostic reason code, which must never be surfaced in a public API response.
- The effective license document, present only when validation succeeds.

If the license cannot be read, parsed or verified, validation fails closed with a typed result; it does not throw into the request path.

## Cryptographic verification

The current implementation uses **RSA-PSS with SHA-256** for signature verification. The trusted key set carries public keys keyed by `keyId`. Verification uses only the public key; private key material is never present in the server, the repository or CI. The minimum accepted RSA key size is enforced in code (2048 bits); the exact published policy wording remains an open decision (see the Decision Log, O-06).

The exact canonicalization profile, the signature envelope wording and the minimum accepted key-size policy are all preserved as open decisions in the Phase 4 plan; they are not silently decided here.

## Trusted public keys

The server trusts a **key set**, not a single key. Each trusted entry has a `keyId` and a public key. A license references the key that signed it through `keyId`. If the `keyId` is not in the trusted set, validation fails as `UnknownKey`.

## Key rotation

Key rotation is designed in from the start. A new key is added to the trusted set; licenses signed by the previous key continue to verify until they expire or are replaced. Old keys are retired only after all affected licenses have expired. Rotation procedure details for the issuer are an internal vendor operation.

## Tampering behaviour

Any modification to the signed payload, the signature, the `keyId` or the algorithm causes verification to fail closed. The parser rejects unknown or duplicate properties, enforces JSON depth limits, rejects oversized input, and applies strict Base64 decoding. These behaviours are covered by the licensing test suite (Phase 4.3 through 4.10).

## Operational installation

See [Phase 4.12 — Operational License Management](plans/Phase-4/Phase-4.12-Operational-License-Management.md). The configured location is exposed through a single configuration setting under the `Licensing` section (`LicenseFilePath`). The default is empty, which means no license is configured. There is no hard-coded default path.

## License replacement

Replacement is a versioned issue-and-install operation. A renewal, edition upgrade, feature change or limit change is delivered as a **new signed license**, not as an in-place edit. The previous license file is retained as the rollback copy until the new one is confirmed working. See Phase 4.12 for the full procedure.

## Backup and recovery

The license file should be included in the customer's documented backup set. The vendor private signing key is **not** a customer backup artifact and no customer procedure assumes the customer holds it. Recovery restores the license file from the backup set, re-applies permissions and re-validates it.

## Troubleshooting

See [Troubleshooting](Troubleshooting.md) for the operator-facing entry points. In summary:

- `Valid` — the license is valid for the current time.
- `Missing` — no license configured or file not found; the application runs in Community/restricted mode.
- `Malformed` — the license could not be read or parsed.
- `UnsupportedVersion` — the format version is not supported by this build.
- `WrongProduct` — the license was issued for a different product.
- `InvalidSignature` — the signature did not verify.
- `UnknownKey` — the `keyId` is not in the trusted key set (possible rotation gap).
- `Expired` — the license has expired.
- `NotYetValid` — the license is not yet valid.
- `InvalidConfiguration` — the trusted key set or validation configuration is unusable.
- `InvalidContent` — the license parsed and verified but a property, edition, feature or limit is unknown or out of range.

Do not paste the internal diagnostic reason code into customer-facing output. Do not weaken a validation rule to unblock a customer; issue a corrected license instead.

## Security model

Licensing provides:

- Cryptographic integrity of the license content.
- Trusted-key verification against a configured key set.
- Strict parsing with bounded input.
- Fail-closed validation.
- A default-deny feature/edition enforcement boundary.
- Expiration handling with bounded clock-skew on the issuance side only.
- Tamper and boundary protections verified by tests.

## Offline-first behaviour

The current implementation requires no online activation, no revocation service and no telemetry. No licensing server is required. No network call is required for license validation. Online activation and revocation remain future work (see [Phase 4.13](plans/Phase-4/Phase-4.13-Online-Activation-Future.md)).

## Source-available limitations

LabAuthServer is delivered as source. A customer with control of the source code, the binaries and the host can modify the application and bypass client-side enforcement. Licensing enforcement is a commercial authenticity and default-deny control, not unbreakable DRM. Do not claim tamper-proof binaries, impossible bypass or hardware-level enforcement.

## Future online licensing

Online activation and online revocation are **future/optional**. The Phase 4.13 document records the design options, the security and privacy questions, and the open decisions. No part of it is implemented.

## Machine binding

Machine binding is **deferred**. No machine identity is currently required, no binding field exists in the current license format, and no rebind workflow exists. Online activation does not by itself provide machine binding.

## Known limitations

- Replacement requires an application restart/reload; hot reload and per-request revalidation are not implemented.
- Grace is default-disabled and diagnostic only; it is not an entitlement bypass.

## Open decisions

The Phase 4.17 governance decisions for production key custody, feature matrix, issuance authority, external register, and external delivery are approved. Future online activation/revocation, machine binding, and other deferred technical work remain out of scope. See [Phase 4 Decision Log](plans/Phase-4/Phase-4-Decision-Log.md) and the [Phase 4.17 sign-off](plans/Phase-4/Phase-4.17-Implementation-and-Final-Sign-Off.md).

## Related documents

- [Phase 4 plan set](plans/Phase-4/Phase-4-README.md)
- [Phase 4.12 — Operational License Management](plans/Phase-4/Phase-4.12-Operational-License-Management.md)
- [Phase 4.13 — Online Activation and Revocation (Future)](plans/Phase-4/Phase-4.13-Online-Activation-Future.md)
- [Phase 4.17 — Production License Governance and Final Sign-Off](plans/Phase-4/Phase-4.17-Implementation-and-Final-Sign-Off.md)
- [Security](Security.md)
- [Configuration](Configuration.md)
- [Troubleshooting](Troubleshooting.md)
- [Testing](Testing.md)