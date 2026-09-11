# Phase 4.8 — Optional Machine/Installation Binding

Status: DEFERRED — DESIGN/ANALYSIS RECORDED; NO IMPLEMENTATION. [Phase 4 README](Phase-4-README.md) | Previous: [4.7](Phase-4.7-Expiration-and-Grace-Period.md).

Approval assessment (2026-09-11): machine binding is **not approved for implementation**. Decision D-09 is APPROVED with the wording "Machine binding: deferred" and open question O-14 is recorded as RESOLVED — deferred. The Phase 4.8 plan itself states "Recommendation for the first implementation: no binding" and lists selecting a final binding mechanism as out of scope. Consequently this task records design and security analysis only; no production machine-binding code was added.

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

## Approval assessment and deferral record (2026-09-11)

Approval status: **NOT APPROVED FOR IMPLEMENTATION.**

| Source | Wording | Status |
| --- | --- | --- |
| [Decision Log](Phase-4-Decision-Log.md) D-09 | "Machine binding: deferred" | APPROVED |
| [Decision Log](Phase-4-Decision-Log.md) O-14 | "Machine binding: adopt or defer" → "Deferred (D-09)" | RESOLVED — DEFERRED |
| This document, Design decisions | D4.8-1 machine binding OPTIONAL and deferred in the first implementation | PROPOSED (not elevated) |
| This document, Scope | "Selecting a final binding mechanism" is out of scope | — |
| This document, recommendation | "Recommendation for the first implementation: no binding" | — |

Because the approved decision is deferral, this phase performs **analysis and documentation only**. No `LicenseDocument` field, no binding version, no binding algorithm, no `ILicenseMachineIdentityProvider`, no binding check in `LicenseValidator`, no binding configuration and no binding tests were added. Adding any of those would silently convert a deferred decision into an implementation decision, which the Phase 4 change-control rules prohibit.

## Security analysis (read-only, no implementation)

What would be bound, and how stable it is. Candidate identities and their behavior under ordinary administration:

| Candidate | Stability across reboot | Stability across OS upgrade | VM clone | Hardware replacement | Admin spoofability |
| --- | --- | --- | --- | --- | --- |
| Server-generated installation ID (persisted opaque value) | High | High | Copied with the disk image | Survives | Copyable by anyone with file access |
| Hardware fingerprint (CPU/motherboard/disk serial composite) | High | Medium (driver/board changes) | Usually duplicates or partially changes | Breaks | Moderate |
| NIC MAC address | High | High | Frequently regenerated or duplicated | Breaks on NIC swap | Trivially spoofable in software |
| Windows `MachineGuid` | High | Low (reinstall resets it) | Often duplicated by imaging | Usually survives | Readable and writable by an administrator |
| Certificate thumbprint | Medium | Medium | Duplicated with the certificate | Survives | Replaced by certificate renewal |
| Domain/SID-based machine identity | Medium | Low (reimage/rename resets it) | Duplicated by cloning | Usually survives | Requires domain-level access to change |
| Hostname | High | High | Duplicated by cloning | Survives | Trivially changed by an administrator |

Environment-specific behavior:

- **VMware and virtualization generally.** Cloning a VM typically duplicates whatever identity is stored on disk, so a bound license follows the clone unless a hardware-derived component differs. Snapshot restore rolls the identity back to the snapshot state. vMotion and storage migration are usually transparent to disk-persisted identities but may change a hardware-derived composite. Virtual NIC and virtual disk changes alter a hardware-derived composite but not a persisted installation ID.
- **Cloud and container environments.** A recreated instance or container generally receives a new ephemeral host identity, so any scheme keyed to instance identity fails on every redeploy. The Phase 4.8 plan already lists "container re-creation without persistent state" as a known false-positive scenario.
- **High availability and multi-instance deployments.** The current offline-first architecture has no shared state. A single signed license carrying one binding value cannot be evaluated identically by multiple independent instances without either (a) sharing the persisted identity, which requires storage the plan explicitly places out of scope, or (b) accepting that only one instance is bound. Machine binding is therefore not safely compatible with HA in the current architecture.
- **Disaster recovery and migration.** A restore to different hardware, a rebuilt server, a hostname change and a domain change each invalidate a hardware-derived or domain-derived binding. Only a restored persisted installation ID survives, and only if the license file and the identity file are both in the backup set.
- **Spoofability.** A customer holding the source and administrative access to their own host can read, copy or replace any value this software reads from the host. Machine binding cannot be a cryptographic control in this deployment model; it can only discourage casual licence copying.

Why deferral remains correct:

1. The Phase 4.8 plan requires a documented rebind procedure, including an offline path, before binding is enabled (D4.8-5). No such procedure exists and 4.12 operational procedures are not complete.
2. Offline signed licenses with expiry already provide cryptographic authenticity. Binding adds support burden and lockout risk without materially improving protection against a source-holding customer.
3. Any binding scheme introduces a new denial-of-service vector for legitimate customers, which the plan's own security considerations prohibit.
4. The approved decision is deferral; implementing now would contradict D-09.

## Required future decisions (before any implementation)

All remain open and must be recorded in the [Decision Log](Phase-4-Decision-Log.md) before code:

- **Which identity is bound** (persisted installation ID vs. a hardware-derived composite), with the privacy and stability rationale.
- **Whether the binding is required or optional**, and whether an unbound license is still accepted.
- **Binding representation**: raw value, salted one-way hash, or an opaque vendor-issued token. A hash must not be presented as making the identity secret.
- **Binding version and algorithm identifiers**, and the rejection behavior for unknown values.
- **Whether multiple machine identities are permitted** in one license, for example during a migration window.
- **The rebind procedure**, including an offline path and the evidence required from a customer.
- **License-format impact**: whether the binding claim can be added without a new `licenseVersion`, and how existing unbound licenses remain valid.
- **HA and multi-instance support**: whether binding is explicitly unsupported for load-balanced deployments.
- **Recovery behavior**: what happens after snapshot restore, VM clone and bare-metal restore.

## Impact if later implemented (documented, not built)

- **License format.** A binding claim could be added as an optional payload field. Whether that requires a new `licenseVersion` depends on whether the format is frozen; Phase 4.2 currently rejects unknown payload properties, so adding a field without a version bump would break existing parsers. This must be resolved before implementation.
- **Server.** Identity acquisition, normalization and comparison would sit behind a new abstraction; binding mismatch must map to restricted mode only and must never disable authentication or any other security control (D4.8-4).
- **Issuer.** The vendor would need to obtain the identity out-of-band and include it in the signed payload. No private key would ever be placed on the customer machine.
- **Tests.** Matching, mismatched, malformed, unknown-version, unsupported-algorithm and unavailable-identity cases, plus an explicit assertion that binding failure does not alter authentication behavior.

No such code exists today. The licensing time window is decided in exactly one place (`ILicenseExpirationEvaluator`), and no binding input participates in it.