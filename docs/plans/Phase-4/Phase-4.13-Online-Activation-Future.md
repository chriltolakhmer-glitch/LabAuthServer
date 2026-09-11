# Phase 4.13 — Online Activation Architecture (Future)

Status: FUTURE, OPTIONAL. [Phase 4 README](Phase-4-README.md) | Previous: [4.12](Phase-4.12-Operational-License-Management.md).

## Objective

Design an optional future online activation and revocation service, while keeping offline signed-license validation as the foundation.

## Scope

In scope:

- Service architecture for online activation.
- Capabilities that online activation uniquely enables.
- Compatibility rules with the offline path.
- Privacy and connectivity constraints.

Out of scope:

- Any implementation.
- Making the first release depend on Internet connectivity.
- Telemetry policy decisions.

## Why it exists

Offline licenses cannot be revoked and cannot enforce activation counts. If those become commercial requirements, the design must be ready and must not require redesigning the offline path.

## Prerequisites

- 4.5 through 4.12 complete and operating.
- A business requirement that justifies operating a service.

## Inputs

- Customer deployment topologies, including air-gapped environments.
- Legal and privacy constraints on telemetry.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.13-1 | Offline signed-license validation remains the foundation | FINAL (constraint) | Customers may be air-gapped |
| D4.13-2 | Online activation is additive and optional | PROPOSED | No dependency for core operation |
| D4.13-3 | Failure to reach the service must not degrade below offline behavior | PROPOSED | Connectivity is not a licensing control |
| D4.13-4 | Revocation requires online activation and is documented as such | PROPOSED | Cannot be enforced offline |
| D4.13-5 | Telemetry is collected only if legally appropriate and disclosed | PROPOSED | Privacy |
| D4.13-6 | Activation responses are signed and verifiable by the server | PROPOSED | Prevents a trivial local spoof |

DECISION REQUIRED: Whether online activation is pursued at all. Recommendation: defer until a commercial requirement exists.

DECISION REQUIRED: Data residency and retention for activation records. Recommendation: minimal data, documented retention, no personal data beyond what the contract requires.

## Proposed architecture

```
LabAuthServer
      |
      | HTTPS (optional, outbound)
      v
License Service
      |
      v
License Database
```

Capabilities that online activation can add:

| Capability | Offline equivalent |
| --- | --- |
| Activation count enforcement | None |
| Revocation | None |
| Renewal without reissuing a file | Manual reissue |
| Customer portal showing license status | Manual support |
| Telemetry, if legally appropriate | None |
| Rebind after hardware change | Manual reissue |

Compatibility rules:

- The license file remains the primary artifact.
- An online check may add a status, never replace signature verification.
- If the service is unreachable, behavior falls back to the offline result exactly.
- Cached online state has a documented maximum age.
- The offline path must remain fully testable without the service.

## Files likely to change

- None in this phase (planning only).
- If pursued: TO BE CONFIRMED DURING IMPLEMENTATION: a client for the activation service, configuration for the service endpoint, and a new service component outside this repository.

## Files that must NOT change

- Authentication and security controls.
- Existing offline validation behavior, except as explicitly amended by an approved decision.
- CI workflow, unless the new service introduces its own pipeline.

## Implementation steps

1. Confirm whether a commercial requirement exists.
2. If yes, define the service contract, data model and privacy policy.
3. Define the outage and fallback behavior precisely.
4. Define the security model for the client-to-service channel.
5. Record decisions, then produce a separate detailed implementation plan.

## Security considerations

- The activation channel is a new outbound trust relationship and must be authenticated both ways where feasible.
- A compromised service must not be able to grant more than the signed license permits.
- Do not send customer-identifying data unless required and disclosed.
- Do not let a network failure become a denial of service for the product.

## Failure cases

- Service outage causing licensed features to stop working, contradicting the offline foundation.
- Activation response replay.
- Unauthenticated activation endpoint allowing forged activation.
- Telemetry collection without disclosure.

## Testing requirements

- Offline behavior unchanged when the service is unreachable.
- Service unreachable does not degrade below offline behavior.
- Replayed activation response rejected.
- Forged activation response rejected.
- Offline test suite passes with no service configured.

## Acceptance criteria

- Architecture documented without implementing anything.
- Offline foundation preserved as the primary mechanism.
- Fallback behavior documented exactly.
- Capabilities only online activation can provide are listed.

## Rollback considerations

Not applicable while deferred. If implemented, the service must be removable without affecting offline validation.

## Evidence to record

- Decision to pursue or defer.
- If pursued, the service contract and privacy review.

## Git/commit strategy

- Documentation only in this phase.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.5, 4.7 and 4.12. Not a prerequisite for 4.14 or 4.15.

## Risks

- Introducing a connectivity dependency by accident.
- Privacy or contractual exposure from telemetry.
- Operational cost of running a service.

## Deferred items

- The service itself.
- Customer portal.
- Revocation workflow automation.