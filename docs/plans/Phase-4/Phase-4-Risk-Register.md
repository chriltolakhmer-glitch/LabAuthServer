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

## Review rules

- Every risk must have an owner once implementation begins.
- OPEN risks must be reviewed at each implementation phase boundary.
- ACCEPTED requires a named owner, a date and a rationale.
- New risks discovered during implementation are added here, not in a separate document.