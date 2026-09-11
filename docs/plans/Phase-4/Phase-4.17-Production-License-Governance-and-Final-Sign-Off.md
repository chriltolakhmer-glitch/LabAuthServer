# Phase 4.17 — Production License Governance & Final Sign-Off

Status: PLAN ONLY. Implementation NOT STARTED. [Phase 4 README](Phase-4-README.md) | Previous: [4.16](Phase-4.16-Production-Licensing-Readiness.md).

## 1. Purpose

Phase 4.16 made the offline licensing implementation operationally usable: a bounded runtime loader, startup validation, fail-closed restricted mode, an audit/logging surface, and a frozen License Version 1 signing profile. Phase 4.16 left a defined set of non-blocking conditions (key custody, feature-to-edition matrix, hosted CI verification, issuance authority, register, delivery vehicle, and final sign-off).

Phase 4.17 exists to close those conditions with **governance, operational procedures, verification evidence and a final production sign-off** — not new licensing features. It converts the remaining OPEN decisions into documented, owned, testable outcomes and records the final production readiness verdict.

This document is a plan. It implements nothing, changes no source, and authorizes no production issuance.

## 2. Current Baseline

| Item | Value |
| --- | --- |
| Branch | main |
| HEAD | 59f7285 "Implement Phase 4.16 production licensing readiness" |
| Previous Phase 4.15 commit | 1c495ea "Finalize Phase 4 security review" |
| Working tree | Clean |
| Current Phase 4 verdict | PASS WITH CONDITIONS (Phase 4.15), improved by Phase 4.16 |
| Test baseline | 1007 passed, 0 failed, 0 skipped (761 unit + 246 integration, Release) |

Current runtime licensing behavior (implemented):

- `LicensePolicyProvider` loads and validates the configured license once at startup via `BoundedLicenseFileReader` (default bound 64 KiB) and `LicenseValidator`.
- Every missing/unreadable/oversized/malformed/untrusted/expired/unsupported input yields `LicensePolicy.Restricted` (Community). Startup never crashes on a licensing condition.
- Only security/operational metadata is logged; license content, payload, signature and key material are never logged.
- Trusted public keys are provisioned through `Licensing:TrustedKeys`; `TrustedKeySetFactory` rejects private PEM material.
- License replacement requires application reload/restart.

Current known conditions carried into Phase 4.17: O-07 (production vendor key custody), O-01 (feature-to-edition matrix), hosted CI verification, O-17 (issuance authority), O-18 (license register), O-20/O-21 (visibility/release vehicle), plus operational verification of the runtime lifecycle, replacement/recovery, audit/logging policy, and final sign-off.

## 3. Scope

Phase 4.17 will:

1. Resolve or formally accept the open governance decisions (O-07, O-01, O-17, O-18, O-20, O-21, hosted CI).
2. Document the production key-custody, issuance, register, delivery, replacement/recovery and audit/logging procedures.
3. Verify the runtime lifecycle, logging security, and CI against objective evidence.
4. Perform a final production security review and record a final sign-off verdict.

## 4. Out of Scope

Explicitly excluded from Phase 4.17:

- Machine binding (D-09, deferred).
- Online activation and online revocation (D-10, D-22, deferred).
- Any licensing server.
- Production private-key generation inside this repository.
- Hot reload and per-request revalidation (unless a future approved decision requires them).
- Fuzz, property-based and load testing (deferred).
- Unrelated authentication/security changes and unrelated application features.

## 5. Phase 4.17 Workstreams

### 5.1 Production Vendor Key Custody — O-07

- **Objective:** decide and document where the production vendor private signing key lives and how it is protected.
- **Required decision:** the production key storage technology and operator model. **BUSINESS/OPERATIONS DECISION REQUIRED.** No commercial product is selected here.
- **Security requirements:** the key is offline/vendor-controlled; the server never receives it; the customer installation receives only trusted public material; CI never receives it.
- **Operator responsibilities:** named key custodian(s); documented access control; issuance performed only by authorized operators.
- **Storage requirements:** protected store (certificate store, HSM or vault — to be decided); the key never appears in source, appsettings, environment files, tests, CI or publish output.
- **Backup/recovery:** documented offline backup of the private key under dual control; documented recovery rehearsal.
- **Rotation:** add a new public key to `Licensing:TrustedKeys`, keep the superseded key trusted until all licenses it signed expire.
- **Compromise response:** future operational policy (revocation is online-only and deferred); short-term mitigation is key rotation plus re-issuance.
- **Test-key separation:** test signing keys remain ephemeral and in-memory; production key is never used in tests.
- **CI separation:** no production key or secret in GitHub Actions.
- **Customer-server boundary:** the boundary is `Licensing:TrustedKeys` (public keys only).
- **Acceptance criteria:** see 4.17-AC-01, 4.17-AC-02, 4.17-AC-03, 4.17-AC-04.

### 5.2 Feature-to-Edition Matrix — O-01

- **Review:** editions Community, Professional, Enterprise; features `auth.basic`, `auth.jwt`, `auth.ldap`, `audit.logging`, `admin.console`.
- **Constraint:** do not invent business requirements. The matrix requires a business/product decision. **BUSINESS DECISION REQUIRED.**
- **Plan:** approve the matrix commercially, then represent it technically (a mapping consumed only for documentation/UI help, never as an implicit grant) and test it.
- **Acceptance criteria:** the approved matrix preserves default-deny, edition never independently grants a feature, unknown features are denied, missing limits are not unlimited (4.17-AC-05, 4.17-AC-06).

### 5.3 GitHub Hosted CI Verification

- **Objective:** produce real hosted-run evidence for `.github/workflows/ci.yml`.
- **Verify:** workflow triggers; .NET `10.0.400` matching `global.json`; restore; Release build; full test suite; `permissions: contents: read`; no secrets; no production signing key; no deployment.
- **Constraint:** do not add production signing secrets.
- **Evidence required to mark VERIFIED:** a hosted run URL/ID showing a green build-and-test job on `main` with the current test count.
- **Acceptance criteria:** 4.17-AC-07. If hosted execution is unavailable, record explicit acceptance (4.17-AC-08).

### 5.4 Offline Issuance Authority — O-17

- **Objective:** document who may issue production licenses and under what approval.
- **Define:** authorized issuer/operator; approval responsibility (named owner + second approver); issuance workflow; license input validation (issuer already validates editions/features/limits/timestamps); signing (RSA-PSS/SHA-256); post-sign verification before release; license ID handling; expiry handling; entitlement approval; separation of duties where practical.
- **Constraint:** no licensing server; offline issuance only.
- **Acceptance criteria:** 4.17-AC-09, 4.17-AC-10.

### 5.5 License Register — O-18

- **Objective:** decide whether a register is required (recommended) and document it.
- **If required, define:** minimum fields (license ID, customer, product, edition, features, limits, issuedAt, expiresAt, keyId, issuer, status); lifecycle states (Issued, Delivered, Active, Expired, Replaced, Revoked-policy-future); ownership; access control; backup; retention; auditability.
- **Constraint:** do not create a database unless explicitly required; a simple structured record is acceptable.
- **Acceptance criteria:** 4.17-AC-11.

### 5.6 License Visibility / Release Vehicle — O-20/O-21

- **Objective:** define how the customer receives the license.
- **Requirement:** the license stays external to source, Git, binaries, static web content and publish output; delivered out of band.
- **Boundary:** `Licensing:LicenseFilePath` is the configuration boundary; deployment supplies it. No customer-specific path is hardcoded.
- **Acceptance criteria:** 4.17-AC-12, 4.17-AC-13.

### 5.7 License Replacement and Recovery

- **Procedure:** prepare → validate → stage → atomic replace → restart/reload → revalidate → rollback if invalid.
- **Define:** backup (previous license retained as rollback copy); rollback; failure behavior (remain restricted); operator verification; recovery (restore from backup set, re-apply permissions, revalidate).
- **Acceptance criteria:** 4.17-AC-14, 4.17-AC-15.

### 5.8 Runtime Lifecycle Verification

- **Plan verification of the startup-only lifecycle:** DI lifetime (singleton policy provider, single authoritative state); startup loading; validation; restart-required replacement; concurrency behavior; bounded reads; restricted mode; arbitrary-path safety (administrator-controlled `Licensing:LicenseFilePath`); no license exposure over HTTP or static content.
- **Constraint:** do not implement hot reload.
- **Acceptance criteria:** 4.17-AC-16, 4.17-AC-17.

### 5.9 Audit / Logging Governance

- **Define:** licensing event categories (not loaded with typed status, validation failed with status + internal reason, loaded with edition and restricted flag); severity (Warning for restricted entry, Information for success); structured fields; retention; operator troubleshooting; sensitive-data exclusions.
- **Prohibit logging:** full license document, payload, signature, private keys, secrets.
- **Acceptance criteria:** 4.17-AC-18, 4.17-AC-19.

### 5.10 Final Production Security Review

- **Cover:** cryptography, parser, validation, enforcement, expiration, tamper resistance, abuse resistance, key custody, runtime loader, logging, CI, deployment, authentication isolation.
- **Acceptance criteria:** 4.17-AC-20 through 4.17-AC-24.

## 6. Implementation Order

Dependency-aware sequence:

1. Business/operations decisions (O-07, O-01, O-17, O-18, O-20, O-21).
2. Production key custody (5.1).
3. Feature/edition matrix (5.2).
4. Issuance governance (5.4).
5. License register decision (5.5).
6. Delivery/release procedure (5.6, 5.7).
7. Runtime lifecycle verification (5.8).
8. Audit/logging verification (5.9).
9. Hosted CI verification (5.3).
10. Final production security review (5.10).
11. Documentation reconciliation.
12. Final sign-off.

Dependencies: 5.1–5.6 depend on business/operations decisions that cannot be automated; 5.7 depends on 5.8 (the lifecycle); 5.10 depends on all prior workstreams; sign-off depends on 5.10 and documentation reconciliation.

## 7. Acceptance Criteria

| ID | Criterion |
| --- | --- |
| 4.17-AC-01 | Production vendor private key is absent from the repository, build output, CI and the server |
| 4.17-AC-02 | Production key custody decision is recorded with a named owner |
| 4.17-AC-03 | Key rotation procedure is documented and the trusted-key set supports it |
| 4.17-AC-04 | Test signing keys remain ephemeral and separate from production |
| 4.17-AC-05 | Feature-to-edition matrix approved (or formally accepted as a business decision) |
| 4.17-AC-06 | Default-deny, edition-does-not-grant-feature, unknown-feature denial and non-unlimited limits remain enforced |
| 4.17-AC-07 | Hosted CI run evidence captured, OR hosted CI explicitly accepted as unavailable |
| 4.17-AC-08 | No production secret or signing key in CI |
| 4.17-AC-09 | Issuance authority documented with named approvers |
| 4.17-AC-10 | Issuance workflow includes post-sign verification before release |
| 4.17-AC-11 | License register decision documented (required or not) |
| 4.17-AC-12 | Delivery/release vehicle documented; license external to source/Git/binaries/static/publish |
| 4.17-AC-13 | `Licensing:LicenseFilePath` preserved as the configuration boundary; no hardcoded customer path |
| 4.17-AC-14 | Replacement procedure documented (prepare/validate/stage/atomic-replace/restart/revalidate) |
| 4.17-AC-15 | Recovery and rollback procedure documented |
| 4.17-AC-16 | Runtime lifecycle verified: startup load, single state, restart-required replacement |
| 4.17-AC-17 | Bounded reads and restricted-mode fail-closed behavior verified |
| 4.17-AC-18 | Logging governance documented; sensitive-data exclusions explicit |
| 4.17-AC-19 | No sensitive license content, payload, signature or key material is logged |
| 4.17-AC-20 | Final security review complete across all licensing areas |
| 4.17-AC-21 | No security blocker remains |
| 4.17-AC-22 | No unexplained release blocker remains |
| 4.17-AC-23 | Full test suite passes at or above the 1007 baseline |
| 4.17-AC-24 | Final production sign-off recorded |
| 4.17-AC-25 | All open decisions resolved, accepted, or explicitly deferred |

## 8. Security Requirements

### Key Security
- Production private key never in the repository, CI, server or customer artifacts.
- Only public keys provisioned via `Licensing:TrustedKeys`; private PEM rejected.

### Cryptographic Security
- RSA-PSS + SHA-256; RSA minimum 2048; initial issuer profile RSA-3072; frozen License Version 1 profile preserved.

### License File Security
- Bounded read (default 64 KiB); external file; treated as confidential entitlement data, not public.

### Parser Security
- Strict UTF-8/BOM handling, duplicate/unknown property rejection, depth limits, strict Base64.

### Runtime Security
- Fail closed; restricted Community on any non-valid path; single authoritative state; no license exposure over HTTP.

### Logging Security
- Metadata only; no license content, payload, signature or key material.

### CI Security
- `permissions: contents: read`; no secrets; no production key; no deployment.

### Operational Security
- Named custodians; access control; dual-control backup; documented recovery.

### Release Security
- Limitation statement published verbatim; no overclaiming; internal runbooks internal.

**Invariant preserved:** licensing must never weaken authentication, authorization, JWT, LDAP, SQL TLS or any other security control.

## 9. Test Plan

This is a plan only; no tests are added now. Future Phase 4.17 tests:

- Key-custody policy check: repository/CI contain no private-key material (extend the existing Phase 4.10 guard).
- Runtime loader regression: retain the 16 Phase 4.16 tests.
- Feature/edition matrix tests: allowed/denied per approved matrix; default-deny; edition-does-not-grant.
- Issuance workflow tests where applicable: post-sign verification rejects a tampered license.
- Logging security tests: assert no sensitive material is emitted.
- Replacement/recovery tests where applicable.
- CI verification: hosted-run evidence.

Current baseline: 1007 passed, 0 failed, 0 skipped. Expected after Phase 4.17: baseline preserved or increased; 0 failed; 0 skipped.

## 10. Documentation Plan

- **Create:** `Phase-4.17-Production-License-Governance-and-Final-Sign-Off.md` (this plan); a final production sign-off document on completion.
- **Update:** `docs/Licensing.md` (remove the "no loader/no DI/no logging" limitations now that Phase 4.16 implements them); `Phase-4-README.md` (reference 4.17); `Phase-4-Implementation-Checklist.md` (add 4.17 rows); `Phase-4-Decision-Log.md` (record resolved decisions); `Phase-4-Risk-Register.md` (add production-governance risks); `Phase-4-Change-Record.md` (append the 4.17 planning row).
- **Preserve unchanged:** historical phase records 4.0–4.16.

## 11. Open Decision Register

| ID | Decision | Owner | Required Before Production? | Planned Resolution |
| --- | --- | --- | --- | --- |
| O-07 | Production vendor key custody | Operations | Yes (before issuance) | 5.1 |
| O-01 | Feature-to-edition matrix | Business/Product | Yes (before issuance) | 5.2 |
| O-17 | Issuance authority | Operations | Yes (before issuance) | 5.4 |
| O-18 | License register | Operations | Recommended | 5.5 |
| O-20 | Document visibility | Project owner | Yes (before release) | 5.6 |
| O-21 | Release vehicle | Project owner | Yes (before release) | 5.6 |
| CI | Hosted CI verification | Engineering | No (evidence) | 5.3 |
| O-03/O-04 | Canonicalization/signature profile | Architect | No (Version 1 frozen) | Future format version |
| O-10 | Concrete license file location | Operations | No (config model approved) | Deployment config |

Classification: O-07/O-17/O-18 = operations decisions; O-01 = business decision; O-20/O-21 = project-owner decisions; CI = technical verification; O-03/O-04/O-10 = technical/deferred.

## 12. Risk Register

| Risk | Severity | Likelihood | Impact | Mitigation | Owner | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Private signing key compromise | High | Low | Forged licenses | Offline custody, rotation, compromise policy | Operations | OPEN |
| Incorrect entitlement assignment | Medium | Medium | Wrong capability granted | Issuance validation + approval | Operations | OPEN |
| License delivery error | Medium | Medium | Customer cannot operate | Out-of-band delivery + verification | Operations | OPEN |
| Accidental license exposure | Medium | Low | Entitlement data leak | External file, permissions, no logging | Operations | OPEN |
| Incorrect replacement | Medium | Medium | Service disruption | Validate-before-activate, rollback copy | Operations | OPEN |
| Invalid license activation | Low | Medium | Restricted mode | Fail-closed; diagnostics | Engineering | MITIGATED |
| Logging sensitive data | Medium | Low | Data leak | Metadata-only logging + tests | Engineering | MITIGATED |
| CI secret exposure | High | Low | Key leak | No secrets in CI | Engineering | MITIGATED |
| Operational recovery failure | Medium | Medium | Extended outage | Documented recovery + rehearsal | Operations | OPEN |
| Business matrix ambiguity | Medium | Medium | Entitlement disputes | Explicit approval | Business | OPEN |

Normal operational risks are not presented as technical vulnerabilities.

## 13. Final Exit Criteria

Phase 4.17 can close only when:

- O-07 resolved or formally accepted by operations.
- O-01 resolved or formally accepted as a business decision.
- Issuance governance documented.
- License register decision documented.
- Delivery/release procedure documented.
- Runtime lifecycle verified.
- Logging verified.
- Hosted CI verified or explicitly accepted as unavailable.
- Security regression passes.
- Full test suite passes.
- Documentation reconciled.
- No security blocker remains.
- No unexplained release blocker remains.
- Final production sign-off recorded.

Final statuses: **READY FOR CONTROLLED PRODUCTION**, **READY WITH ACCEPTED CONDITIONS**, or **NOT READY**.

## 14. Update High-Level Phase 4 Plan References

If appropriate, update `Phase-4-README.md` and `Phase-4-Implementation-Checklist.md` to reference Phase 4.17 as PLAN ONLY. Do not mark Phase 4.17 complete and do not claim any implementation has been completed by this plan.

## 15. Git Safety

`git diff --check`, `git status --short`, `git diff --stat` confirmed only documentation changes. No source, tests, CI, configuration, keys, secrets or customer licenses are committed. No push.