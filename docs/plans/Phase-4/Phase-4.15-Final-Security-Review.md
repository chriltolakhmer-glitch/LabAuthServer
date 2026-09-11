# Phase 4.15 — Final Security Review

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.14](Phase-4.14-Documentation-and-Release.md).

## Objective

Define the final security review that must pass before licensing is released to any customer.

## Scope

In scope:

- Review checklist across cryptography, custody, validation, enforcement, CI and operations.
- Required evidence.
- Sign-off requirements.
- Explicit confirmation that existing security controls are unaffected.

Out of scope:

- Re-reviewing unrelated security work already completed in earlier phases.
- Penetration testing of unrelated surfaces.

## Why it exists

Licensing adds a new trust relationship and a new validation path. Both must be reviewed against the existing hardened baseline before release.

## Prerequisites

- Phases 4.0 through 4.14 complete for the scope being released.

## Inputs

- All Phase 4 documents.
- Test results and CI evidence.
- The [Risk Register](Phase-4-Risk-Register.md) and [Decision Log](Phase-4-Decision-Log.md).

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.15-1 | No release without a completed security review | FINAL (constraint) | Gate |
| D4.15-2 | The review confirms existing security controls are unchanged | FINAL (constraint) | Licensing must not weaken security |
| D4.15-3 | The review confirms no private key is present in any artifact | FINAL (constraint) | Key custody |
| D4.15-4 | The review confirms fail-closed behavior on every validation path | FINAL (constraint) | Core property |
| D4.15-5 | Unresolved risks are recorded, accepted by a named owner, and dated | PROPOSED | Accountability |

DECISION REQUIRED: Who performs the review and who signs off. Recommendation: a reviewer who did not implement the licensing code, plus project ownership sign-off.

## Proposed architecture

Review checklist:

| # | Area | Question | Evidence required |
| --- | --- | --- | --- |
| 1 | Key custody | Is the vendor private key absent from the repository, build output, CI configuration and the server? | Search results and review |
| 2 | Key custody | Is the private key required only by the issuer, on the vendor side? | Component review |
| 3 | Cryptography | Is the algorithm and key size as approved, with no weak fallback? | Code review and test output |
| 4 | Cryptography | Does the server trust a key set, and does rotation preserve existing licenses? | Rotation test result |
| 5 | Format | Is canonicalization identical on both sides? | Round-trip test result |
| 6 | Format | Are unknown fields, versions, editions and features rejected? | Negative test results |
| 7 | Validation | Does every failure path deny? | Failure-path test results |
| 8 | Validation | Does the validation order place cryptographic verification before semantic trust? | Code review |
| 9 | Validation | Are internal reasons excluded from public responses? | Response mapping review |
| 10 | Enforcement | Are authentication and security controls unconditionally active? | Security-independence test result |
| 11 | Enforcement | Are unknown and missing features denied by default? | Test results |
| 12 | Expiration | Is grace explicit, bounded and default-off? | Configuration review and boundary tests |
| 13 | Binding | If deferred, is no binding input required? | Test result |
| 14 | Tamper | Is the limitation statement published and accurate? | Release notes review |
| 15 | CI | Does the pipeline run without any vendor private key? | Workflow review and run evidence |
| 16 | CI | Is the test baseline preserved or increased? | Test count report |
| 17 | Operations | Do issuance, renewal and recovery procedures avoid key sharing? | Procedure review |
| 18 | Operations | Is the license file included in the documented backup set? | Procedure review |
| 19 | Regression | Are all previously hardened controls unchanged? | Diff review of protected files |
| 20 | Risk | Are all open risks recorded with owners and dates? | Risk register review |

Required confirmations, stated explicitly in the review record:

- The private key never exists in LabAuthServer.
- A customer cannot legitimately modify license properties without invalidating the signature.
- Invalid, modified, expired or incompatible licenses fail closed.
- Licensing does not disable or bypass authentication or any security control.
- Technical enforcement cannot make the source impossible to modify, and no document claims otherwise.

## Files likely to change

- None. The review produces a record, not code.
- A review record document may be added under `docs/` after the review.

## Files that must NOT change

- Source code and tests, except to fix a defect found by the review, which requires its own change and re-review.

## Implementation steps

1. Resolve the reviewer and sign-off decisions.
2. Execute the checklist and collect evidence per row.
3. Record unresolved risks with owners and dates.
4. Record the sign-off.
5. Release the gate for 4.14.

## Security considerations

- The review must be independent of the implementation.
- Evidence must be current, not cited from planning documents.
- A failing row blocks release unless an explicit, recorded, owned acceptance exists.

## Failure cases

- Review performed by the implementer without independent verification.
- Checklist completed by assertion rather than evidence.
- An open risk accepted without a named owner.
- Release proceeding while a checklist row is unresolved.

## Testing requirements

- Confirm all licensing tests pass.
- Confirm the baseline suite passes.
- Confirm the CI run used for the review contains no vendor private key.
- Confirm the negative-path tests cover every row in the 4.5 rule table.

## Acceptance criteria

- All checklist rows completed with evidence.
- Required confirmations stated in the review record.
- Open risks recorded with owner and date.
- Sign-off recorded.
- Release gate status recorded.

## Rollback considerations

If the review finds a blocking defect, the affected phase reverts to its last known-good state and the release is held. The review does not itself change runtime behavior.

## Evidence to record

- Completed checklist with evidence references.
- Test and CI results.
- Open risk acceptances.
- Sign-off record.

## Git/commit strategy

- Suggested message: `docs(licensing): add final security review record`.
- No commit or push without explicit authorization.

## Dependencies on previous phases

- Requires 4.0 through 4.14.

## Risks

- Review treated as a formality.
- Evidence collected from an earlier, stale run.
- Scope of the review not matching the scope of the release.

## Deferred items

- External third-party review.
- Periodic re-review cadence.
- Post-release monitoring thresholds.