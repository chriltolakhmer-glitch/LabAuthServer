# Phase 4.9 — Tamper and Abuse Resistance

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.8](Phase-4.8-Machine-Binding.md).

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