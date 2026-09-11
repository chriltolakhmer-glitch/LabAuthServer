# Phase 4.1 — License Architecture

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.0](Phase-4.0-Requirements-and-Licensing-Model.md).

## Objective

Define the end-to-end component architecture, the trust boundaries and the separation of the vendor side from the customer side.

## Scope

In scope:

- Component list and responsibilities.
- Trust boundary between vendor and customer.
- Key custody model.
- Data flow from issuer to enforcement.
- Extension points for future online activation.

Out of scope:

- Concrete class names and file paths in `src/` (TO BE CONFIRMED DURING IMPLEMENTATION).
- Algorithm selection (4.3).
- Format definition (4.2).

## Why it exists

The architecture fixes where trust lives. Getting this wrong (for example embedding a private key, or letting the issuer ship with the product) makes every later control meaningless.

## Prerequisites

- [Phase-4.0](Phase-4.0-Requirements-and-Licensing-Model.md) decisions resolved.

## Inputs

- Requirement list and constraints.
- Existing layering rule: Domain <- Application <- Infrastructure <- Api (from repository `AGENTS.md` and the master roadmap).

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.1-1 | Issuer is a separate vendor-side tool, not part of the deployed server | PROPOSED | Removes any reason for the private key to ship |
| D4.1-2 | Server embeds or configures only public keys, as a key set | PROPOSED | Enables rotation |
| D4.1-3 | Validation lives in Application/Infrastructure, never in Api controllers directly | PROPOSED | Keeps layering and testability |
| D4.1-4 | Validation is a pure function over license bytes and trusted key set | PROPOSED | Offline, deterministic, testable |
| D4.1-5 | Enforcement is a policy object consumed by feature code | PROPOSED | Separates decision from enforcement |
| D4.1-6 | Revocation is out of scope until online activation | PROPOSED | Offline-only first |

DECISION REQUIRED: Where the trusted public key set is stored — embedded constant, configuration file, or both. Recommendation: configuration file with a compiled-in default, so operators can add a rotated key without a rebuild, while a missing configuration still verifies against the default.

## Proposed architecture

```
Vendor side (never shipped)
  Vendor private signing key
        |
        v
  License Issuer tool
        |
        v
  license.lic  (signed document)

Customer side (shipped)
  license.lic
        |
        v
  License Loader  -> raw bytes
        |
        v
  License Parser  -> typed license document
        |
        v
  Signature Verifier (trusted public key set)
        |
        v
  License Validator -> typed validation result
        |
        v
  License Policy  (edition, features, limits, expiry)
        |
        v
  Feature Enforcement points
        |
        v
  Application behaviour
```

Component responsibilities:

| Component | Responsibility | Trust |
| --- | --- | --- |
| Vendor private key | Sign license payloads | Vendor only |
| License Issuer | Validate input, canonicalize, sign, write license | Vendor only |
| Trusted public key set | Verify signatures | Shipped |
| License Loader | Read license bytes from an approved location | Shipped |
| License Parser | Decode and structurally validate | Shipped |
| Signature Verifier | Cryptographic verification | Shipped |
| License Validator | Apply all validation rules in defined order | Shipped |
| License Policy | Expose edition, features, limits, expiry | Shipped |
| Feature Enforcement | Allow or deny a named feature | Shipped |

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: application service registration and configuration binding for the license subsystem.
- TO BE CONFIRMED DURING IMPLEMENTATION: application-layer license abstractions.
- TO BE CONFIRMED DURING IMPLEMENTATION: infrastructure-layer parser, verifier and loader.
- New vendor-side project under a `tools/` folder: `tools/LabAuthServer.LicenseIssuer/` (proposed path).

## Files that must NOT change

- Authentication, LDAP, JWT issuance and validation code.
- TLS, certificate and key-store configuration.
- Request/header/claim limit configuration.
- CI workflow definition (until 4.11 explicitly authorizes it).
- Database schema (until an approved decision requires it).

## Implementation steps

1. Confirm the layering placement with repository `AGENTS.md`.
2. Confirm the public key set storage decision.
3. Confirm the issuer project location and whether it is added to the solution.
4. Record decisions, then proceed to 4.2.

## Security considerations

- The issuer must not be referenced by the server project.
- The server must not contain any code path that generates a signing key.
- The trusted key set must be additive (rotation), never a silent replacement that invalidates all existing licenses.
- Validation must not run before authentication in a way that changes authentication outcomes.

## Failure cases

- Public key stored alongside a private key by accident.
- Issuer project added to the server solution and shipped.
- Validation invoked from a middleware that short-circuits authentication.
- Key set replaced instead of extended, invalidating valid customer licenses.

## Testing requirements

- Architectural tests: the server assembly does not reference the issuer assembly.
- Test that a missing key set results in deny, not allow.
- Test that an added rotated key does not invalidate licenses signed by the previous key.

## Acceptance criteria

- Component responsibilities and trust boundaries documented.
- Key custody table present and unambiguous.
- Layering placement recorded.
- Rotation-capable key set design recorded.

## Rollback considerations

Documentation-only. Reverting the commit has no runtime effect.

## Evidence to record

- Layering review outcome.
- Key set storage decision and rationale.
- Issuer project location decision.

## Git/commit strategy

- Scope: `docs/plans/Phase-4/` only.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.0 vocabulary and constraints.

## Risks

- Over-coupling enforcement into the request pipeline.
- Under-specifying rotation, causing a future forced reissue of all licenses.

## Deferred items

- Revocation architecture (4.13).
- Machine binding integration point (4.8).
- Hardware-backed key storage for the issuer (4.4 and 4.12).