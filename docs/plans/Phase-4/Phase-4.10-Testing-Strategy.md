# Phase 4.10 — Testing Strategy

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md).

## Objective

Define the complete test matrix for licensing, including valid, invalid, boundary and security-independence cases.

## Scope

In scope:

- The full case list.
- Test data strategy, including ephemeral test keys.
- Assertion style for structured validation results.
- Preservation of the existing 733-test baseline.

Out of scope:

- CI wiring mechanics (4.11).
- Operational support procedures (4.12).

## Why it exists

Licensing failures are either silent (an invalid license accepted) or catastrophic (a valid license rejected). Both must be caught by tests that do not depend on the vendor private key.

## Prerequisites

- 4.2 through 4.7 specified.
- Test project conventions understood from the existing test suite (TO BE CONFIRMED DURING IMPLEMENTATION).

## Inputs

- The validation rule table from 4.5.
- The timeline table from 4.7.
- The enforcement rules from 4.6.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.10-1 | Every validation rule has at least one positive and one negative test | PROPOSED | Rule coverage is auditable |
| D4.10-2 | Negative tests assert the specific internal reason code | PROPOSED | Prevents "false for the wrong reason" |
| D4.10-3 | Tests generate ephemeral signing keys at run time | PROPOSED | No key material in the repository |
| D4.10-4 | Tests never require the production vendor private key | FINAL (constraint) | CI must not hold vendor secrets |
| D4.10-5 | Test licenses are built from a shared fixture helper, not checked-in binary files where avoidable | PROPOSED | Readable, reviewable, regenerable |
| D4.10-6 | Clock is injected in every time-related test | PROPOSED | Deterministic boundary testing |
| D4.10-7 | The existing 733-test baseline is a regression gate | FINAL (constraint) | No reduction permitted |

DECISION REQUIRED: Whether any signed test license fixture is committed as a file. Recommendation: commit only keys and payloads that are explicitly test-only and clearly labelled; prefer generating licenses at test time.

## Proposed architecture

Test matrix:

| # | Case | Expected outcome |
| --- | --- | --- |
| 1 | Valid license | Valid, policy available |
| 2 | Invalid signature | Deny, `SIGNATURE_INVALID` |
| 3 | Modified license payload | Deny, `SIGNATURE_INVALID` |
| 4 | Expired license | Deny, `EXPIRED` |
| 5 | Not-yet-valid license | Deny, `NOT_YET_VALID` |
| 6 | Wrong product | Deny, `PRODUCT_MISMATCH` |
| 7 | Unsupported version | Deny, `VERSION_UNSUPPORTED` |
| 8 | Unknown edition | Deny, `FIELD_INVALID` |
| 9 | Unknown feature | Deny, `FEATURE_UNKNOWN` |
| 10 | Invalid limits | Deny, `LIMIT_OUT_OF_RANGE` |
| 11 | Missing required field | Deny, `FIELD_MISSING` |
| 12 | Malformed license | Deny, `LICENSE_MALFORMED` |
| 13 | Wrong key ID | Deny, `KEY_UNTRUSTED` |
| 14 | Old signing key still trusted | Valid |
| 15 | New signing key | Valid |
| 16 | Key rotation with both keys trusted | Both validate |
| 17 | Multiple trusted public keys | Each validates its own licenses |
| 18 | Clock boundary at `expiresAt` | Documented boundary behavior |
| 19 | Clock skew within allowance | Unchanged behavior |
| 20 | Clock moved backward | Documented behavior, no indefinite extension |
| 21 | License missing | Deny, `LICENSE_MISSING` |
| 22 | License unreadable | Deny, `LICENSE_UNREADABLE` |
| 23 | License file replaced | New file revalidated, old policy discarded |
| 24 | Feature disabled by license | Denied |
| 25 | Feature enabled by license | Allowed |
| 26 | Maximum-user boundary at limit | Allowed |
| 27 | Maximum-user boundary above limit | Denied |
| 28 | Over-limit boundary | Denied deterministically |

Cross-cutting tests:

| Test | Assertion |
| --- | --- |
| Licensing cannot bypass authentication | With an invalid license, authentication outcomes are unchanged |
| Unlicensed optional feature is denied | Policy denies, response contains no licensing internals |
| Licensed feature remains available | Policy allows |
| Invalid license fails closed | Every failure path yields deny, never allow |
| Application tests require no private key | Test run succeeds with no vendor key present |
| Assembly separation | Server assembly does not reference the issuer assembly |
| Public response safety | Public category never reveals the internal reason code |

Test data strategy:

- Ephemeral key pair generated per test run for signing fixtures.
- Test public key injected into the verifier under test.
- A clearly labelled development key may be committed only if it is provably test-only and documented as such.
- Production vendor keys are never present in the repository or CI.

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: licensing test project or test classes within the existing test structure.
- TO BE CONFIRMED DURING IMPLEMENTATION: test fixture helpers for license generation.

## Files that must NOT change

- Existing tests that establish the 733-test baseline, except where a deliberate, reviewed addition is required.
- Production configuration.
- CI workflow (until 4.11).

## Implementation steps

1. Identify the correct test project(s) for licensing tests.
2. Add the fixture helper for ephemeral keys and license construction.
3. Implement the matrix rule by rule.
4. Implement the cross-cutting tests.
5. Confirm the total test count is at least the baseline plus the new licensing tests.

## Security considerations

- Test-only keys must be unmistakably labelled to prevent production misuse.
- Test output must not print key material.
- Tests must not weaken existing security assertions.
- Do not add a test that disables a security control in order to test licensing.

## Failure cases

- A test that passes for the wrong reason because it only asserts a boolean.
- A committed test key reused in production by mistake.
- A test that depends on real wall-clock time, becoming flaky.
- Baseline test count reduced by deleting or skipping tests.

## Testing requirements

- The matrix above, implemented.
- Coverage measurement or equivalent evidence for the licensing components.
- A clean run: 733 baseline tests still passing plus the new licensing tests, 0 failed, 0 skipped.

## Acceptance criteria

- Full matrix implemented and passing.
- Negative tests assert specific reason codes.
- No private key required.
- Baseline preserved or increased.
- Cross-cutting security-independence tests present.

## Rollback considerations

Test additions are additive. Reverting the test commit removes the licensing tests without affecting the existing suite.

## Evidence to record

- Test run summary: total, passed, failed, skipped.
- Baseline comparison.
- List of tests added per rule.

## Git/commit strategy

- Suggested message: `test(licensing): add validation, tamper and boundary tests`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Requires 4.3 through 4.7.

## Risks

- Flaky time-based tests.
- Fixture helpers diverging from the real format.
- Coverage gaps in cross-cutting guarantees.

## Deferred items

- Fuzz testing of the parser.
- Property-based testing of canonicalization.
- Load testing of per-request enforcement.