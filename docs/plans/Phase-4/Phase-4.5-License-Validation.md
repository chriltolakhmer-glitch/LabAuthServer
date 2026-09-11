# Phase 4.5 — License Validation in LabAuthServer

Status: IMPLEMENTED — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED. [Phase 4 README](Phase-4-README.md) | Previous: [4.4](Phase-4.4-License-Issuer.md).

Implementation note (2026-09-11): the validator, typed result model, injectable clock and structural bounds policy are implemented in the documented rule order; M-1 and M-2 are resolved. Validation cadence (O-09) and license file location (O-10) remain open decisions and are not required by this phase's scope. Feature/edition enforcement (4.6) and expiration grace policy (4.7) remain out of scope.

## Objective

Design the in-server validator that produces a typed, ordered validation result from license bytes.

## Scope

In scope:

- The ordered validation rule list.
- Typed result model.
- Safe error handling and what must never be exposed publicly.
- Where validation runs and when.

Out of scope:

- Feature gating behavior (4.6).
- Expiration policy semantics (4.7).
- CI wiring (4.11).

## Why it exists

A boolean "is the license valid" is insufficient: operators and tests need to distinguish a missing license from an expired one, a wrong product from a bad signature, and a configuration error from tampering.

## Prerequisites

- 4.2 format frozen.
- 4.3 verification implemented or specified.
- 4.4 available to produce test licenses.

## Inputs

- License file bytes or an approved license source.
- Trusted public key set.
- Current UTC time source (injectable).

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.5-1 | Validation returns a typed result, never a bare boolean | PROPOSED | Distinguishes causes safely |
| D4.5-2 | Validation follows the fixed order in this document | PROPOSED | Deterministic, testable |
| D4.5-3 | Validation is pure: bytes plus trusted keys plus clock in, result out | PROPOSED | Offline and testable |
| D4.5-4 | The result carries an internal reason code and a public-safe category | PROPOSED | Internal detail must not leak |
| D4.5-5 | Public API responses expose only a coarse category | PROPOSED | Prevents information disclosure |
| D4.5-6 | Clock is injected, not read from a static call | PROPOSED | Enables boundary and skew tests |
| D4.5-7 | Any unexpected exception maps to the internal-error deny result | PROPOSED | Fail closed |

DECISION REQUIRED: Where validation runs. Options: (a) once at startup with the result held for the process lifetime, (b) startup plus periodic revalidation, (c) per-request. Recommendation: (a) plus (b) on a bounded interval, so a license replacement can take effect without a full restart, and so expiry is not missed by a long-running process.

DECISION REQUIRED: License file location. Options: a path in configuration, a well-known path beside the application, an environment variable. Recommendation: a configured path with a documented default, validated at startup.

## Proposed architecture

Validation rules in order:

| # | Rule | Failure code (internal) |
| --- | --- | --- |
| 1 | License file exists | `LICENSE_MISSING` |
| 2 | License file is readable | `LICENSE_UNREADABLE` |
| 3 | Content parses as a single well-formed document | `LICENSE_MALFORMED` |
| 4 | `licenseVersion` is supported | `VERSION_UNSUPPORTED` |
| 5 | `product` equals `LabAuthServer` | `PRODUCT_MISMATCH` |
| 6 | Signature envelope present and well-formed | `SIGNATURE_MALFORMED` |
| 7 | `keyId` resolves in the trusted key set | `KEY_UNTRUSTED` |
| 8 | `alg` is accepted | `ALGORITHM_UNSUPPORTED` |
| 9 | Signature verifies over canonical payload | `SIGNATURE_INVALID` |
| 10 | Required fields present and well-typed | `FIELD_MISSING` / `FIELD_INVALID` |
| 11 | `issuedAt` is a valid instant and not in the future beyond allowed skew | `NOT_YET_VALID` |
| 12 | `expiresAt` is a valid instant and after `issuedAt` | `EXPIRY_INVALID` |
| 13 | `expiresAt` has not passed (subject to 4.7 grace rules) | `EXPIRED` |
| 14 | Every feature identifier is recognized | `FEATURE_UNKNOWN` |
| 15 | Every limit key is recognized and within allowed bounds | `LIMIT_UNKNOWN` / `LIMIT_OUT_OF_RANGE` |
| 16 | Policy is internally consistent (edition covers features, limits within edition) | `POLICY_INCONSISTENT` |

Result model (proposed shape, names TO BE CONFIRMED DURING IMPLEMENTATION):

| Field | Meaning |
| --- | --- |
| `Status` | `Valid`, `ValidWithWarning`, `Invalid`, `Expired`, `NotYetValid`, `Unsupported`, `Missing` |
| `ReasonCode` | Internal stable code from the table above |
| `Warnings` | Non-fatal conditions, for example approaching expiry |
| `Policy` | The effective license policy when validation succeeded |
| `PublicCategory` | Coarse, non-disclosing category for API responses |

Safe error handling:

- Never return the internal reason code, signature bytes, key identifier, customer name or validation stack to a client.
- Log internal reason codes at an appropriate level, without customer-identifying content.
- Map every unexpected exception to a deny result and log it.
- Do not expose whether a signature or the key identifier failed specifically; both map to the same public category.

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: validator in the application/infrastructure layer.
- TO BE CONFIRMED DURING IMPLEMENTATION: registration of the validator and the effective policy in dependency injection.
- TO BE CONFIRMED DURING IMPLEMENTATION: health or diagnostics surface, if an approved decision adds one.

## Files that must NOT change

- Authentication and LDAP code paths.
- JWT issuance and validation.
- Request/header/claim limit configuration.
- Public API contracts, unless an approved decision adds a licensing status endpoint.

## Implementation steps

1. Resolve the validation cadence and license location decisions.
2. Define the typed result and internal reason codes.
3. Implement rules in the documented order.
4. Implement public-safe mapping.
5. Add rule-by-rule tests including negative cases.

## Security considerations

- Validation must not be reachable in a way that changes authentication outcomes.
- A failure must never fall through to "allow".
- Internal diagnostics must not become an oracle for guessing valid license structure.
- Reading the license must not follow untrusted paths.

## Failure cases

- Exception in the parser treated as valid.
- Rule order changed so an unverified payload reaches semantic validation and is trusted.
- Public endpoint returning the internal reason, aiding forgery attempts.
- License file replaced while the process holds a cached valid policy.

## Testing requirements

- One positive test and at least one negative test per rule in the table.
- A test proving an invalid license does not disable or bypass authentication.
- A test proving a missing license yields a deny policy, not an exception.
- A test proving the public category does not reveal the internal reason.
- A test proving an unreadable file and a missing file are distinguishable internally but not publicly.

## Acceptance criteria

- Typed result model documented.
- Validation order documented and implemented identically.
- Safe error mapping documented.
- Fail-closed behavior verified for every rule.

## Rollback considerations

Reverting the validator commit returns the server to its pre-licensing behavior. No production licenses exist before 4.4 is used operationally.

## Evidence to record

- Validation result examples for each category.
- Test results including the authentication-independence test.
- Public response mapping table.

## Git/commit strategy

- Suggested message: `feat(licensing): add offline license validator`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Requires 4.2, 4.3 and 4.4.

## Risks

- Validation cadence chosen poorly, missing expiry in a long-running process.
- Diagnostics leaking structure.
- Cached policy surviving a license replacement.

## Deferred items

- Online revocation check (4.13).
- Machine binding evaluation inside validation (4.8).
- Administrative licensing status UI.