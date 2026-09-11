# Phase 4.12 — Operational License Management

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.11](Phase-4.11-GitHub-CI-Integration.md).

## Objective

Document the future operational procedures for issuing, renewing, replacing, rotating and recovering licenses.

## Scope

In scope:

- Issuing, renewing and replacing licenses.
- Key rotation operations.
- Customer migration, disaster recovery and VM migration.
- Support and audit trail requirements.
- Revocation, contingent on 4.13.

Out of scope:

- Implementing any procedure or tooling.
- Legal contract terms.
- Pricing.

## Why it exists

Most licensing incidents are operational, not cryptographic. The procedures must exist before enforcement is enabled for paying customers.

## Prerequisites

- 4.4 issuer available.
- 4.5 through 4.7 enforcement behavior defined.
- 4.11 CI strategy confirmed.

## Inputs

- The vendor's support and account-management process (TO BE CONFIRMED).
- The customer's deployment and disaster-recovery procedures.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.12-1 | Every issued license is recorded in an internal vendor register | PROPOSED | Needed for renewal and audit |
| D4.12-2 | Renewal issues a new license rather than modifying an existing one | PROPOSED | Signatures are immutable |
| D4.12-3 | Replacement requires the same approval path as initial issuance | PROPOSED | Prevents ad-hoc key handling |
| D4.12-4 | Key rotation follows the overlap procedure in 4.3 | PROPOSED | Avoids invalidating valid licenses |
| D4.12-5 | Revocation is only meaningful with online activation | PROPOSED | Offline licenses cannot be recalled |
| D4.12-6 | Support procedures must not require the vendor private key to be shared | FINAL (constraint) | Key custody |

DECISION REQUIRED: Who holds authority to issue a production license, and what approval evidence is retained. Recommendation: a named owner plus a second approver, with the issuance record retained.

DECISION REQUIRED: Whether an internal vendor license register is a document, a spreadsheet or a database. Recommendation: a simple register with a stable schema, decided at implementation.

## Proposed architecture

Planned procedures:

| Procedure | Trigger | Key handling | Customer impact |
| --- | --- | --- | --- |
| Issue | New customer or new deployment | Sign with current key | Install license, restart or wait for revalidation |
| Renew | Approaching expiry | Sign with current key | Replace license before `expiresAt` |
| Replace | Lost, corrupted or wrong license | Sign with current key | Replace file |
| Rotate signing key | Scheduled or on suspicion | New key pair, extend trusted key set, reissue over time | None if overlap is respected |
| Revoke | Contractual or security event | Requires online activation (4.13) | Not enforceable offline |
| Migrate | Customer moves to new infrastructure | Reissue if binding is enabled; otherwise unchanged | None if unbound |
| Disaster recovery | Restore from backup | Restore the same valid license | None, provided the license file is in the backup set |
| VM migration / cloning | Host change | None if unbound; rebind if bound | Possible delay if binding is enabled |
| Support | Customer reports a licensing problem | Diagnose via logs; never request the private key | Depends on cause |

Operational requirements:

- The license file must be included in the customer's documented backup set.
- The customer must know where the license file is configured.
- The vendor must retain the ability to reproduce a license for a paying customer.
- Logs must record expiry warnings early enough to allow renewal.
- No procedure may require transmitting the vendor private key.

Audit trail:

| Event | Recorded |
| --- | --- |
| License issued | Customer, edition, features, limits, validity window, key ID, timestamp, approver |
| License renewed | Previous license ID, new license ID, reason |
| Key rotated | Old key ID, new key ID, retirement date |
| Support case | Customer, symptom, resolution |

## Files likely to change

- New documentation under `docs/` during implementation (support runbook).
- New vendor-side record format or tool (TO BE CONFIRMED DURING IMPLEMENTATION).

## Files that must NOT change

- Server source code.
- CI workflow.
- Customer-facing configuration beyond the documented license path.

## Implementation steps

1. Resolve the issuance authority and register decisions.
2. Draft the support runbook.
3. Define the customer-facing instructions for installing and replacing a license.
4. Define the renewal reminder timeline.
5. Define the key rotation calendar.

## Security considerations

- The license register contains customer information; treat it as confidential.
- Never email a private key.
- Never store a private key in a shared drive or ticket system.
- Support staff should be able to diagnose without access to signing capability.

## Failure cases

- A customer discovers expiry only when licensed features stop working.
- A rotated key retires before all licenses signed by it have expired.
- A support engineer requests the private key to "test locally".
- A backup excludes the license file, causing a post-restore outage.

## Testing requirements

- Procedure rehearsal: issue, install, renew, replace, rotate, restore from backup, all in a non-production environment.
- Verify that each rehearsal step matches the documented procedure.

## Acceptance criteria

- All procedures documented with triggers and impacts.
- Audit trail fields defined.
- Renewal reminder timeline defined.
- No procedure requires sharing the private key.
- Rehearsal evidence recorded.

## Rollback considerations

Procedures are documentation until enforcement ships. Once shipped, the fallback for a licensing operational failure is to restore the last known-good license file and, if necessary, disable enforcement under an approved emergency change.

## Evidence to record

- Approved procedures.
- Rehearsal results.
- Key rotation calendar.

## Git/commit strategy

- Documentation only in this phase.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.4 through 4.11. Contingent on 4.13 for revocation.

## Risks

- Insufficient renewal lead time causing customer outages.
- Support processes leaking key material.
- Register drift making renewal impossible.

## Deferred items

- Customer self-service portal.
- Automated renewal reminders.
- Revocation workflow (4.13).