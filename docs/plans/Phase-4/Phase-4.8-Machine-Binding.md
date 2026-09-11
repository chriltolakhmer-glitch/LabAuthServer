# Phase 4.8 — Optional Machine/Installation Binding

Status: PLANNING ONLY, DEFERRED BY DEFAULT. [Phase 4 README](Phase-4-README.md) | Previous: [4.7](Phase-4.7-Expiration-and-Grace-Period.md).

## Objective

Analyze machine and installation binding options, document their risks, and recommend whether to bind at all in the first implementation.

## Scope

In scope:

- Option analysis: no binding, installation ID, machine fingerprint, certificate thumbprint, Windows machine identity, server-generated installation identity.
- Reliability, privacy and operational risk.
- A recommendation for the first implementation.

Out of scope:

- Selecting a final binding mechanism.
- Implementing binding.
- Online activation, which is the only sound basis for strong binding (4.13).

## Why it exists

Binding is the most common cause of legitimate licensing failures. It must be a deliberate decision with documented recovery paths, not a default.

## Prerequisites

- 4.5 validation and 4.7 expiration defined, since binding adds another validation input.

## Inputs

- Deployment model information: physical, virtual, container, clustered, disaster-recovery topology (TO BE CONFIRMED DURING IMPLEMENTATION).

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.8-1 | Machine binding is OPTIONAL and deferred in the first implementation | PROPOSED | Avoids false lockouts before recovery procedures exist |
| D4.8-2 | If adopted, binding uses a server-generated installation identity, not a hardware fingerprint | PROPOSED | Stable across hardware changes |
| D4.8-3 | If adopted, binding is recorded as a claim in the license and checked during validation | PROPOSED | Fits the existing format |
| D4.8-4 | Binding failures never disable authentication or security controls | FINAL (constraint) | Security independence |
| D4.8-5 | A documented rebind procedure must exist before binding is enabled | PROPOSED | Prevents permanent lockout |

DECISION REQUIRED: Whether to bind at all in the first release. Recommendation: no binding initially. Revisit after 4.12 operational procedures exist and only if a commercial requirement justifies the support cost.

## Proposed architecture

Option analysis:

| Option | Stability | Privacy | Bypass difficulty | Operational cost |
| --- | --- | --- | --- | --- |
| No binding | N/A | Best | Trivial for a source-holding customer | Lowest |
| Installation ID (server-generated, stored) | High; survives hardware change | Good; opaque value | Moderate | Low |
| Machine fingerprint (hardware-derived) | Low; breaks on hardware, VM and clone events | Poor; may be identifying | Moderate | High |
| Certificate thumbprint | Medium; tied to certificate lifecycle | Good | Moderate | Medium; couples to certificate renewal |
| Windows machine identity (domain/SID-based) | Medium; breaks on reimaging and rename | Medium | Moderate | Medium |
| Server-generated installation identity in a database | High | Good | Moderate | Medium; requires database state |

Known false-positive scenarios for any binding scheme:

- Hardware changes (CPU, motherboard, NIC).
- Virtual machine cloning.
- Server migration to new hosts.
- Disaster recovery and restore to different hardware.
- Hostname changes.
- Certificate renewal, for thumbprint binding.
- Container re-creation without persistent state.

Recommendation for the first implementation: no binding. Offline signed licenses with expiry already provide cryptographic authenticity. Binding without an online activation service adds support burden and outage risk without materially improving protection against a source-holding customer.

If binding is later adopted, the safe pattern is:

1. First run generates an opaque installation ID and persists it.
2. The ID is provided to the vendor for license issuance.
3. The ID is included as a claim in the signed license.
4. Validation compares the claim to the persisted ID.
5. A documented rebind procedure exists, including an offline path.
6. A mismatch denies licensed features only.

## Files likely to change

- None in this phase (planning only).
- If adopted later: TO BE CONFIRMED DURING IMPLEMENTATION: installation identity persistence and validation input.

## Files that must NOT change

- Authentication and security configuration.
- Database schema, unless an approved decision requires persistent installation identity.
- Deployment scripts.

## Implementation steps

1. Confirm the deployment topologies that must be supported.
2. Resolve the bind-or-not decision.
3. If binding is approved, document the rebind procedure before any code.
4. Record the decision in the [Decision Log](Phase-4-Decision-Log.md).

## Security considerations

- A fingerprint may constitute personal or identifying data; treat it as sensitive.
- A stored installation ID must be protected against trivial copying, while acknowledging that a source-holding customer can copy it.
- Binding must not create a denial-of-service vector for the customer.
- Binding must not be used as a substitute for signature verification.

## Failure cases

- Binding enabled with no rebind procedure, permanently locking a paying customer.
- Fingerprint changed by a routine patch, causing an outage.
- Identity stored in a location wiped by an update.
- Binding treated as anti-piracy proof when it is only a weak control.

## Testing requirements

- If binding is adopted: matching identity passes; mismatched identity denies; missing identity denies with a distinct code; rebind procedure restores service; binding failure does not affect authentication.
- If binding is deferred: a test asserting that no binding input is required.

## Acceptance criteria

- All options analyzed with reliability and privacy risks.
- Bind-or-not decision recorded with rationale.
- False-positive scenarios documented.
- If deferred, the deferral is explicit and traceable.

## Rollback considerations

Not applicable while deferred. If implemented later, rollback must include a path to disable binding without reissuing licenses, for example by making the binding claim optional and ignored when absent by an approved configuration.

## Evidence to record

- Deployment topology confirmation.
- Bind-or-not decision with rationale.
- If deferred, the record of deferral and the conditions for revisiting.

## Git/commit strategy

- Documentation only in this phase.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.5 and 4.7. Not a prerequisite for 4.9.

## Risks

- Late binding requirement after licenses are already issued, forcing reissue.
- Customers on ephemeral infrastructure unable to keep a stable identity.

## Deferred items

- The binding mechanism itself.
- Rebinding tooling.
- Online binding validation (4.13).