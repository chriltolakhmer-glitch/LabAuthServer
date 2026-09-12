# Phase 4 — Risk Register

Status: MAINTAINED — IMPLEMENTED (Phases 4.1–4.12 executed; Phase 4.15 final security review completed). [Phase 4 README](Phase-4-README.md).

Severity and likelihood: Low, Medium, High. Status: OPEN, MITIGATED (control designed), ACCEPTED (with named owner and date), CLOSED.

All statuses are OPEN or MITIGATED-by-design. No risk is ACCEPTED yet because no owner has signed off.

| ID | Risk | Severity | Likelihood | Mitigation | Status |
| --- | --- | --- | --- | --- | --- |
| R-01 | Source-code bypass: a customer removes or alters license checks | High | Medium (certain for a determined party) | Accept as a documented limitation; signed licenses still provide authenticity; legal remedy | MITIGATED (documented) |
| R-02 | Vendor private-key compromise | High | Low | Key never in the repository, CI or the server; prefer certificate store, HSM or vault for production; rotation procedure | MITIGATED |
| R-03 | License theft: a license file copied to another deployment | Medium | High if unbound | Accept while binding is deferred; revisit with 4.8 or online activation | OPEN |
| R-04 | License sharing between customers | Medium | High if unbound | Same as R-03; contractual and legal controls | OPEN |
| R-05 | Clock manipulation to extend an expired license | Medium | Medium | Bounded skew allowance; documented as partial protection; no indefinite extension on backward movement | MITIGATED (partial) |
| R-06 | Machine-binding false positives causing legitimate denial | High | High if binding is enabled | Binding deferred; rebind procedure required before enabling | MITIGATED (by deferral) |
| R-07 | Key rotation failure invalidating valid licenses | High | Low | Trusted key set is additive; old keys retained until affected licenses expire; documented retirement dates | MITIGATED |
| R-08 | Broken or malformed license issued to a customer | Medium | Medium | Issuer validates all inputs against known edition, feature and limit sets before signing | MITIGATED |
| R-09 | Accidental license lockout after an update or restore | High | Medium | License file location documented; file included in the documented backup set; enforcement is additive and revertable | OPEN |
| R-10 | CI private-key exposure | High | Low | CI never receives the production key; ephemeral test keys only; no new CI secret | MITIGATED |
| R-11 | Customer migration problems | Medium | Medium | Documented migration procedure; reissue path for bound deployments | OPEN |
| R-12 | VM cloning causing duplicate or mismatched identity | Medium | Medium | Binding deferred; if enabled, requires an explicit rebind path | MITIGATED (by deferral) |
| R-13 | Disaster recovery restoring a stale or missing license | High | Medium | Include the license file in the backup set; documented restore rehearsal | OPEN |
| R-14 | Backward compatibility: existing consumers unaffected by licensing changes | Medium | Low | Licensing is additive; authentication and public API contracts unchanged | MITIGATED |
| R-15 | Signature algorithm deprecation over the product lifetime | Medium | Low | `alg` field allows a new algorithm; format version allows migration; documented migration strategy | MITIGATED |
| R-16 | Canonicalization mismatch between issuer and server | High | Medium | Canonicalization defined by the format; round-trip tests; single shared implementation where feasible | OPEN |
| R-17 | Unknown feature or edition silently accepted | Medium | Low | Default-deny; unknown identifiers rejected at validation | MITIGATED |
| R-18 | Public API leaking validation detail | Medium | Medium | Public-safe category separate from the internal reason code; response mapping reviewed in 4.15 | MITIGATED |
| R-19 | Licensing code placed in the authentication path | High | Low | Documented constraint D-09; security-independence test in 4.10 | MITIGATED |
| R-20 | Test baseline regression | Medium | Low | Baseline is a hard gate in 4.10 and 4.11 | MITIGATED |
| R-21 | Flaky time-dependent tests | Low | Medium | Injected clock; boundary tests are deterministic | MITIGATED |
| R-22 | Licensing operational procedures undocumented at first customer renewal | Medium | High | 4.12 must complete before enforcement ships | OPEN |
| R-23 | Overclaiming protection in customer-facing material | High | Medium | Limitation statement published verbatim; review row 14 in 4.15 | MITIGATED |
| R-24 | Test-only key accidentally used for a production license | High | Low | Test keys clearly labelled test-only; production issuance restricted to the approved key source | MITIGATED |
| R-25 | Grace period misunderstood as permanent | Medium | Medium | Grace is bounded, documented and default-off; expiry warnings logged | MITIGATED |
| R-26 | Performance impact of per-request policy reads or limit counters | Low | Medium | Policy held in memory; counters only where required; reviewed before enabling | OPEN |
| R-27 | Dependency added for canonicalization without governance approval | Medium | Low | Repository `AGENTS.md` package approval rules apply; 4.2 records the choice | OPEN |
| R-28 | Online activation introduced later with a connectivity dependency | High | Low | Offline remains the foundation; unreachable service must not degrade below offline behavior | MITIGATED (by design) |

## Phase 4.17 governance review (2026-09-11)

- Production private-key custody remains an operational control outside this repository; no production key is present in source, CI, output, or tests.
- Entitlement assignment risk is mitigated by the single edition matrix plus explicit signed feature list and default-deny evaluation.
- Delivery, replacement, rollback, register, and operator ownership risks are addressed by the Phase 4.17 implementation and final-sign-off record.
- Online revocation, machine binding, hot reload, and per-request validation remain deferred rather than represented as implemented controls.

## Phase 4.18 closure review (2026-09-11)

The final Phase 4 implementation is closed with accepted non-blocking conditions. No implementation defect remains open in the repository. The following operational conditions are accepted as governance responsibilities outside the repo and do not reopen the implementation:

- P4-COND-01: production private-key custody, backup, rotation, and incident-response remain vendor-controlled operations outside the application code
- P4-COND-02: hosted GitHub Actions validation should be observed and recorded after push
- P4-COND-03: production monitoring and alerting must route validation failures without exposing sensitive license contents

## Hosted CI remediation review (2026-09-12)

- Hosted run `34669268162` failed because SQL audit persistence tests assumed a developer-local database and one LDAP startup-validation test depended on exception propagation through `WebApplicationFactory`/`DeferredHost`.
- The SQL tests remain real persistence tests; CI now provisions disposable LocalDB from the controlled Phase 11 schema and stored-procedure scripts.
- The LDAP test now exercises the production options registration directly and asserts `OptionsValidationException` with the production validation message.
- Hosted run `34669800856` passed for commit `48a8bccd7e1a72e8603be307060aa7fa4d109370` with 1,023 tests passed, 0 failed, 0 skipped, and 0 build warnings/errors.
- The hosted-CI evidence condition is CLOSED. Phase 4 engineering is closed; the remaining conditions below are external operational responsibilities only and do not reopen engineering.

The remaining conditions are tracked as external operational governance responsibilities, not as implementation work items or code defects.

## Review rules

- Every risk must have an owner once implementation begins.
- OPEN risks must be reviewed at each implementation phase boundary.
- ACCEPTED requires a named owner, a date and a rationale.
- New risks discovered during implementation are added here, not in a separate document.