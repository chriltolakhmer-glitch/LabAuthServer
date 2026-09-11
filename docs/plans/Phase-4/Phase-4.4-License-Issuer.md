# Phase 4.4 — License Issuer Tool

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.3](Phase-4.3-Cryptographic-Signing-and-Verification.md).

## Objective

Design the vendor-side tool that turns approved license parameters into a signed license document.

## Scope

In scope:

- Issuer command surface and input validation.
- Key acquisition and custody requirements.
- Output format and file handling.
- Logging and privacy constraints.
- Development/test key versus production key separation.

Out of scope:

- Key generation ceremony (operational, 4.12).
- Server-side validation (4.5).
- Commercial approval workflow for issuing a license (4.12).

## Why it exists

The issuer is the only component that touches the private key. Isolating it from the product is what makes the key-custody model credible.

## Prerequisites

- 4.2 format frozen.
- 4.3 algorithm and key identification decided.

## Inputs

- Approved license parameters: customer, edition, features, limits, validity window.
- Vendor private key, obtained from an approved store.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.4-1 | Issuer is a standalone vendor-side tool, not referenced by the server | PROPOSED | Key isolation |
| D4.4-2 | Issuer never contains a private key in source or configuration | FINAL (constraint) | Prevents accidental disclosure |
| D4.4-3 | Issuer validates all inputs against the known edition/feature/limit sets | PROPOSED | Prevents issuing unusable licenses |
| D4.4-4 | Issuer never logs private key material | FINAL (constraint) | Operational security |
| D4.4-5 | Issuer avoids logging complete licenses when they contain customer information | PROPOSED | Privacy |
| D4.4-6 | Issuer supports a development/test key mode clearly marked as non-production | PROPOSED | Enables CI and local testing |
| D4.4-7 | Output is a single `license.lic` file | PROPOSED | Simple distribution |

DECISION REQUIRED: Where the issuer obtains the private key. Options: (a) a file path provided interactively, (b) Windows certificate store, (c) HSM or hardware-backed key, (d) secure key vault. Recommendation for the prototype: (a) with a strict warning; recommendation for production: (b) or better, with (c)/(d) as the target.

DECISION REQUIRED: Whether the issuer is added to the solution file `LabAuthServer.slnx` or kept outside it. Recommendation: keep it outside the server's build graph or in a separate solution, so the server build cannot pull it in.

## Proposed architecture

Proposed conceptual structure (TO BE CONFIRMED DURING IMPLEMENTATION):

```
tools/
└── LabAuthServer.LicenseIssuer/
```

Issuer responsibilities:

1. Accept license parameters from a file or interactive input.
2. Validate that the product, edition, features and limits are recognized.
3. Validate that `expiresAt` is after `issuedAt` and that `issuedAt` is not in the future beyond an allowed skew.
4. Build the payload.
5. Canonicalize per 4.2.
6. Acquire the private key from the approved source.
7. Sign per 4.3.
8. Write `license.lic` with the signature envelope.
9. Record a non-sensitive issuance audit entry.

Issuer must not:

- Embed a private key.
- Log the private key, its bytes, or a password used to unlock it.
- Log the complete license body if it contains customer information.
- Connect to the product database.
- Be callable from the server.

## Files likely to change

- New: `tools/LabAuthServer.LicenseIssuer/` (proposed path, TO BE CONFIRMED DURING IMPLEMENTATION).
- New: issuer input schema or example parameter file.
- Documentation for issuing a license under `docs/` (4.12).

## Files that must NOT change

- Everything under `src/` and `tests/`.
- `.github/workflows/ci.yml`.
- Any server configuration file.
- The solution file, unless the separate-project decision explicitly approves it.

## Implementation steps

1. Resolve private key source and solution placement.
2. Implement parameter validation.
3. Implement canonicalization reuse (shared library or a verified duplicate).
4. Implement signing.
5. Implement output writing and non-sensitive audit logging.
6. Add issuer unit tests using a generated test key.

## Security considerations

- Treat the issuer host as a high-value asset; it holds signing capability.
- Prefer not to persist a private key on a general-purpose workstation.
- Clear buffers where feasible; never write key material to temp files.
- Validate all input, because a malformed license is a support incident.
- Do not allow arbitrary free-text fields to flow into signed content without bounds.

## Failure cases

- Private key accidentally committed after being placed next to the tool.
- Issuer logs the full license including customer name.
- Issuer issues a license with an unknown feature, causing a customer-side deny.
- Issuer uses a different canonicalization than the server.

## Testing requirements

- Issuer produces a license that the server's verifier accepts.
- Issuer rejects unknown edition, unknown feature, out-of-range limit.
- Issuer rejects `expiresAt` before `issuedAt`.
- Issuer logs contain no key material and no customer-identifying license body.
- Issuer tests use a generated ephemeral test key only.

## Acceptance criteria

- Issuer responsibilities and prohibitions documented.
- Key source decision recorded.
- Development versus production key separation documented.
- Issuer cannot be invoked from the server.

## Rollback considerations

Reverting the issuer commit removes the tool; no runtime impact on the server.

## Evidence to record

- Example issuance run with a test key.
- Test results including rejection cases.
- Confirmation that no key material is logged.

## Git/commit strategy

- Separate commit from server-side validation.
- Suggested message: `feat(licensing): add license issuer tool`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Requires 4.2 and 4.3.

## Risks

- Signing capability concentrated on an unprotected workstation.
- Issuer and server canonicalization drifting apart.
- Test key accidentally used to produce a production license.

## Deferred items

- HSM integration.
- Automated issuance workflow.
- Customer self-service portal (4.13).