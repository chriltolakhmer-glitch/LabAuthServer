# Phase 4.2 — License Document Format

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.1](Phase-4.1-License-Architecture.md).

## Objective

Define a versioned, unambiguous license document format whose canonical byte sequence is the signed artifact.

## Scope

In scope:

- Conceptual payload structure.
- Required and optional fields.
- Canonicalization and encoding rules.
- Versioning and forward-compatibility rules.
- Algorithm and key identifier fields.
- Signature envelope.
- Validation order for structural rules.

Out of scope:

- Algorithm selection (4.3).
- Issuer implementation (4.4).
- Server validation logic (4.5).

## Why it exists

Signature verification is only meaningful if both sides agree on the exact bytes. Ambiguous serialization is the most common cause of "valid license rejected" and of exploitable parser differentials.

## Prerequisites

- 4.0 vocabulary frozen.
- 4.1 architecture accepted.

## Inputs

- Requirement list.
- Existing configuration and serialization conventions in the repository (TO BE CONFIRMED DURING IMPLEMENTATION).

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.2-1 | Versioned format with an explicit `licenseVersion` integer | PROPOSED | Enables staged evolution |
| D4.2-2 | Canonicalization is defined by the format, not by a serializer default | PROPOSED | Removes serializer-dependent byte differences |
| D4.2-3 | Detached or enveloped signature, decided in 4.3 | PROPOSED | Depends on algorithm choice |
| D4.2-4 | Unknown fields are rejected, not ignored | PROPOSED | Prevents silent downgrade of expectations |
| D4.2-5 | Unknown feature identifiers are rejected | PROPOSED | Default-deny |
| D4.2-6 | Unknown editions are rejected | PROPOSED | Default-deny |
| D4.2-7 | Timestamps are UTC, ISO 8601, second precision, `Z` suffix | PROPOSED | Deterministic comparison |

DECISION REQUIRED: Exact canonicalization scheme. Options: (a) JSON Canonicalization Scheme (RFC 8785), (b) a fixed key-order, no-whitespace, UTF-8 minimal-escape profile defined by this project, (c) a non-JSON binary or text format. Recommendation: (a) if the platform libraries are trusted, otherwise (b).

DECISION REQUIRED: Signature envelope. Options: detached signature stored alongside the payload; enveloped signature with a `signature` object; single base64 container. Recommendation: enveloped `signature` object so the license is a single file.

## Proposed architecture

Conceptual payload (not final):

```json
{
  "licenseVersion": 1,
  "licenseId": "...",
  "product": "LabAuthServer",
  "edition": "Professional",
  "customer": "...",
  "issuedAt": "2026-09-11T00:00:00Z",
  "expiresAt": "2027-09-11T00:00:00Z",
  "features": [],
  "limits": {}
}
```

Required fields:

| Field | Type | Notes |
| --- | --- | --- |
| `licenseVersion` | integer | Format version, starts at 1 |
| `licenseId` | string | Unique, opaque, vendor-generated |
| `product` | string | Must equal `LabAuthServer` |
| `edition` | string | Must be a known edition identifier |
| `issuedAt` | string | UTC ISO 8601 |
| `expiresAt` | string | UTC ISO 8601, must be after `issuedAt` |
| `features` | array of string | Known feature identifiers only |
| `limits` | object | Known limit keys only, values within allowed bounds |

Optional fields:

| Field | Type | Notes |
| --- | --- | --- |
| `customer` | string | Display/record only; must not leak through API responses |
| `notBefore` | string | UTC ISO 8601; optional explicit activation time |
| `keyId` | string | Present in the signature envelope, not the payload |
| `notes` | string | Vendor-internal; excluded from any API exposure |

Signature envelope (proposed):

| Field | Type | Notes |
| --- | --- | --- |
| `alg` | string | Algorithm identifier, registered in 4.3 |
| `keyId` | string | Identifier of the signing key, for example `lab-license-signing-2026` |
| `value` | string | Base64 of the signature over the canonical payload bytes |

Encoding rules:

- Payload is UTF-8.
- The signed bytes are the canonical serialization of the payload object only, never including the signature envelope.
- Field names are case-sensitive.
- Duplicate keys are a parse error.
- Numbers used as limits are integers; floating point is rejected for limit values.

Validation order (structural, before cryptographic verification):

1. File exists and is readable.
2. Bytes decode as UTF-8.
3. Document parses as a single object; no trailing data.
4. `licenseVersion` is a supported integer.
5. `product` equals `LabAuthServer`.
6. Signature envelope is present and well-formed.
7. `alg` and `keyId` are syntactically valid.
8. Then cryptographic verification (4.3).
9. Then semantic validation (4.5).

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: license document model types.
- TO BE CONFIRMED DURING IMPLEMENTATION: canonicalization helper.
- A format specification document may be added under `docs/` during implementation.

## Files that must NOT change

- Any existing JSON configuration consumed by the application.
- Existing JWT claim or token serialization.
- Existing API request/response contracts.

## Implementation steps

1. Resolve canonicalization and envelope decisions.
2. Write the format specification with worked canonical examples.
3. Define the supported-version policy and the behavior for future versions.
4. Freeze the format before starting 4.3.

## Security considerations

- Parser differentials are a security issue: the same bytes must not parse two ways.
- Rejecting unknown fields prevents a future feature flag from being smuggled in.
- The signature must cover the canonical payload and nothing else; signing the file with whitespace would make formatting significant.
- The customer field is operational metadata and must be treated as potentially sensitive.

## Failure cases

- Serializer adds whitespace or reorders keys, invalidating signatures.
- Trailing newline or BOM changes signed bytes.
- Two implementations disagree on number formatting for limits.
- A future `licenseVersion` is silently treated as version 1.

## Testing requirements

- Canonicalization round-trip tests: parse then canonicalize yields identical bytes.
- Rejection tests for duplicate keys, trailing data, BOM, wrong case, float limits.
- Version tests: unsupported future version is rejected with a distinct code.
- Unknown field, unknown feature and unknown edition rejection tests.

## Acceptance criteria

- Format documented with required and optional fields.
- Canonicalization defined precisely enough to be reimplemented independently.
- Validation order documented.
- Forward-compatibility rule stated.

## Rollback considerations

Because the format is frozen before any license is issued, rollback in this phase is documentation-only. After issuance begins, format changes require a new `licenseVersion` and dual-version support.

## Evidence to record

- Canonicalization decision and rationale.
- Envelope decision and rationale.
- Worked canonical example with its exact bytes.

## Git/commit strategy

- Scope: `docs/plans/Phase-4/` only (plus, later, a format spec under `docs/`).
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.0 and 4.1.

## Risks

- Choosing a canonicalization scheme whose library differs across runtimes.
- Over-constraining limits, forcing a format break for ordinary commercial changes.

## Deferred items

- Online revocation list format (4.13).
- Machine binding claim (4.8).
- Multi-product licensing.