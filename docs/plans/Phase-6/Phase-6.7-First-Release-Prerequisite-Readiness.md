# Phase 6.7 — First-Release Prerequisite Readiness

Status: PLANNED — NOT AUTHORIZED FOR IMPLEMENTATION OR RELEASE.

## Purpose

Prepare the operational and governance prerequisites for a future first external release request without creating a release, issuing customer licenses, or using production signing material. This phase carries forward the already-approved Phase 5 governance decisions and resolves the remaining operational gates that are not yet selected.

## Phase 5 decisions carried forward without reopening

The following decisions remain in force and are not reopened by this plan:

- repository remains private;
- rights holder is `ALOT`;
- Commercial Approval Authority is `ALOT`;
- Licensing Operator is `ALOT` initially;
- Release Operator is `ALOT`;
- Release Approval Authority is `ALOT` initially;
- Distribution Operator is `ALOT` initially;
- approved private-delivery policy remains in effect;
- no fixed support-term calendar is approved;
- no LTS designation is approved;
- separation of duties remains required where practical.

## Outstanding operational selections

### Delivery provider / channel

Exact provider/channel selection is required before any first external customer delivery.

### Manifest storage

Exact private vendor-controlled storage location and provider are required before any actual external release.

### Release register

The project needs an exact release register product/location with access control, retention, and recovery rules.

### Production issuance custody

The owner must define evidence for:

- private signing-key custody;
- recovery and backup procedures;
- authorized access control;
- approved issuance procedures;
- external issued-license register.

This plan explicitly does not create or use production signing material.

### Release-tag signing

This remains unresolved and requires a real decision packet comparing:

- signed release tags; or
- unsigned tags relying on an approved alternative provenance mechanism.

The plan must record the trade-offs and prerequisites for each option without selecting the owner’s choice in advance.

### Professional review

The project must track external gates for:

- proprietary/evaluation terms;
- commercial terms;
- copyright/legal wording;
- support and retention commitments.

### Release rehearsal

A future separately authorized internal rehearsal must cover:

- release identifier;
- exact source SHA;
- CI evidence;
- Release build;
- hashes;
- Release Manifest;
- register entry;
- artifact set;
- private delivery process;
- reconciliation and recovery.

This rehearsal must not become a customer release.

## Dependencies on earlier work

- Phase 6.4 provides the current-state documentation and build coverage baseline.
- Phase 6.6 provides the actual target-environment acceptance evidence.
- Phase 6.5 provides the operational acceptance decisions for retention, loss, and monitoring.

## Decision gates required before implementation

- exact delivery provider/channel;
- exact manifest storage provider/location;
- exact release register and custody flow;
- release-tag signing decision or approved alternative provenance mechanism;
- commercial/legal review dependencies;
- operational release rehearsal design and scope.

## External/professional dependencies

- legal and commercial review; 
- operations or platform input for manifest/storage/register custody; 
- external release governance if the organization intends a first customer-facing delivery.

## Narrow implementation scope

This is a planning step only. It prepares the operational and governance readiness packet without creating a release or external customer delivery.

## Explicit non-goals

- no release execution;
- no customer/production license issuance;
- no production signing-key creation or use;
- no public/software distribution;
- no release authorization.

## Expected files/components

Likely planning inputs include:

- [docs/Project_Status.md](../../Project_Status.md)
- [docs/Deployment.md](../../Deployment.md)
- [docs/Operations.md](../../Operations.md)
- [docs/Licensing.md](../../Licensing.md)
- [docs/Release-Governance.md](../../Release-Governance.md)
- [docs/Commercial-Licensing.md](../../Commercial-Licensing.md)
- relevant Phase 5 and Phase 6 planning records

## Required automated validation

This subphase is governance and process planning only. Any future implementation should validate:

- release evidence pack completeness;
- manifest store and register integrity; 
- issuance custody and recovery controls;
- rehearsal process viability without customer release.

## Acceptance criteria

The future implementation is acceptable only if:

- each release prerequisite is explicitly assigned to an owner;
- a future release cannot proceed without the required evidence and approvals;
- no operational decision is presented as a release authorization.

## Implementation Authorization Packet

### Baseline prerequisites

- Phase 6.2–6.6 results are approved or explicitly deferred with their decisions.
- The owner has selected or acknowledged the remaining authority and custody model.
- The release governance scope remains internal readiness only.

### Exact implementation scope

- release readiness packet;
- provider and custody selections;
- release evidence and rehearsal design;
- sign-off and legal/commercial dependency tracking.

### Explicit non-goals

- actual customer release;
- production signing-key creation;
- production license issuance;
- release execution.

### Expected files/components

- release governance checklist;
- release register and manifest storage design;
- custody and rehearsal process notes.

### Tests/validation

- governance review and evidence checklist;
- readiness review against the actual release controls required by the owner.

### Acceptance criteria

- no release can proceed without a separate authorized release decision;
- all remaining pre-release gates are documented and assigned.

### Owner/architect decisions required first

- delivery channel/provider;
- manifest storage provider/location;
- release register location and custody;
- release tag signing or approved provenance alternative;
- commercial/legal review gates and support commitments.

### External dependencies

- legal/commercial review;
- external release, manifest, or distribution provider selection;
- platform and operations ownership for storage/custody.

### Safety boundaries

- no production license issuance;
- no production signing-key use;
- no actual customer release or tag publication;
- no release authorization is granted by this subphase.

### Recommended signed commit message

Plan remaining Phase 6 work

---

This subphase remains planning-only and does not authorize release implementation.
