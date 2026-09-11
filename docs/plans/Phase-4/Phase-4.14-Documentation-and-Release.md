# Phase 4.14 — Documentation and Release

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.13](Phase-4.13-Online-Activation-Future.md).

## Objective

Define the documentation and release artifacts required before licensing ships to a customer.

## Scope

In scope:

- Documentation set to publish.
- Release notes content, including the limitation statement.
- Customer-facing installation and renewal instructions.
- Release gating.

Out of scope:

- Changing the release process for other features.
- Marketing material.
- Contract wording.

## Why it exists

A licensing feature shipped without accurate documentation produces support incidents and expectation mismatches, particularly around what enforcement can and cannot do.

## Prerequisites

- 4.0 through 4.12 complete.
- 4.15 review scheduled but not necessarily complete.

## Inputs

- The Phase 4 document set.
- The existing documentation conventions under `docs/`.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.14-1 | The limitation statement is published in the release notes | FINAL (constraint) | Prevents overclaiming |
| D4.14-2 | Customer-facing install and renewal instructions are published | PROPOSED | Reduces support load |
| D4.14-3 | The license file location and format are documented for operators | PROPOSED | Required for support |
| D4.14-4 | Internal runbooks remain internal | PROPOSED | Key and process confidentiality |
| D4.14-5 | Release is blocked until 4.15 is complete | PROPOSED | Security gate |

DECISION REQUIRED: Which documents are customer-visible and which remain internal. Recommendation: install/renew instructions and the limitation statement are customer-visible; the issuer, key and runbook material are internal.

DECISION REQUIRED: Whether licensing ships in a major or minor release. Recommendation: a minor release with explicit release notes, since enforcement is additive.

## Proposed architecture

Documents to produce or update:

| Document | Audience | Content |
| --- | --- | --- |
| Release notes | All | What licensing does, what it does not do, the limitation statement |
| Install and activation instructions | Customer operators | Where the license file goes, how to replace it |
| Renewal instructions | Customer operators | What to do before expiry |
| Licensing overview | Customer technical staff | Editions, features, limits, expiration and grace |
| Troubleshooting entries | Support | Common failure categories and their safe resolution |
| Internal runbook | Vendor | Issuance, rotation, recovery |

Release checklist:

1. All Phase 4 documents present and internally consistent.
2. Test suite at or above the baseline, all passing.
3. CI green with no vendor private key.
4. 4.15 final security review complete.
5. Release notes containing the limitation statement.
6. Customer instructions published.
7. Rollback plan confirmed.

## Files likely to change

- Release notes location (TO BE CONFIRMED DURING IMPLEMENTATION).
- `docs/` pages for licensing overview, install, renewal and troubleshooting.
- Possibly `docs/plans/README.md` to reference this plan set.

## Files that must NOT change

- Source code and tests in this phase.
- CI workflow.
- Existing security documentation, except to add a cross-reference to licensing.

## Implementation steps

1. Resolve the visibility decisions.
2. Draft the release notes and customer instructions.
3. Update the troubleshooting documentation.
4. Confirm the release checklist.
5. Record evidence.

## Security considerations

- Customer-facing documents must not describe internal reason codes or key identifiers in a way that aids forgery.
- Internal runbooks must not be published.
- Release notes must not imply protection stronger than 4.9 documents.

## Failure cases

- Shipping without the limitation statement, creating a contractual risk.
- Publishing internal key handling detail.
- Customer instructions that do not match the actual configured license path.
- Release notes claiming unbreakable protection.

## Testing requirements

- Verify that documented install and renewal steps work in a non-production environment.
- Verify that every customer-facing statement matches implemented behavior.

## Acceptance criteria

- Documentation set complete.
- Limitation statement published verbatim.
- Release checklist defined and satisfied.
- 4.15 gate recorded.

## Rollback considerations

Documentation can be corrected in a follow-up; release rollback follows the existing release process and is not specific to licensing.

## Evidence to record

- Published release notes.
- Customer instruction pages.
- Completed release checklist.

## Git/commit strategy

- Suggested message: `docs(licensing): add license documentation and release notes`.
- No commit or push without explicit authorization.

## Dependencies on previous phases

- Requires 4.0 through 4.12. Gated by 4.15.

## Risks

- Documentation drifting from behavior.
- Over-disclosure of internal detail.
- Under-communication of expiry timelines to customers.

## Deferred items

- Localization of customer-facing licensing documentation.
- Public FAQ.
- Training material for support staff.