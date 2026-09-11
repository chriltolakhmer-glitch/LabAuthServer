# Phase 4 — Decision Log

Status: APPROVED — IMPLEMENTED. Phases 4.1–4.12 executed; Phase 4.15 final security review completed. [Phase 4 README](Phase-4-README.md). Historical decision rows below are retained unchanged.

Status values: `APPROVED` (approved baseline decision), `PROPOSED` (needs approval), `FINAL` (approved constraint), `DEFERRED` (explicitly postponed), `REJECTED` (not adopted).

On 2026-09-11 the project owner approved the 22 Phase 4 Technical License Enforcement baseline decisions below. Approved rows are marked `APPROVED`. Historical decision information is retained; rows are not deleted. Nothing in this log is evidence that a decision has been implemented — implementation has NOT started.

## Core decisions

| ID | Decision | Status | Rationale | Date | Evidence |
| --- | --- | --- | --- | --- | --- |
| D-01 | Licensing model: source-available + commercial technical license | APPROVED | Records the commercial model the enforcement supports | 2026-09-11 | This decision approval |
| D-02 | Licensing architecture: offline-first; no Internet dependency in the first implementation | APPROVED | Customers may be air-gapped; online activation is a later option | 2026-09-11 | This decision approval |
| D-03 | Missing license: application starts in Community/restricted mode | APPROVED | Avoids turning a licensing event into an outage | 2026-09-11 | This decision approval |
| D-04 | Editions: Community, Professional, Enterprise | APPROVED | Simple, well-understood commercial tiers | 2026-09-11 | This decision approval |
| D-05 | Feature licensing: explicit feature list in the signed license | APPROVED | Auditable, versionable, default-deny source of entitlement | 2026-09-11 | This decision approval |
| D-06 | User limits: license may define maximum-user limits | APPROVED | Commercial limit enforcement without a separate binary | 2026-09-11 | This decision approval |
| D-07 | License lifetime: both perpetual and time-limited licenses are supported | APPROVED | Different customer requirements | 2026-09-11 | This decision approval |
| D-08 | Grace period: none in the initial implementation | APPROVED | Conservative default; avoids hidden permanent grace | 2026-09-11 | This decision approval |
| D-09 | Machine binding: deferred | APPROVED | False-positive lockout risk without an online rebind path | 2026-09-11 | This decision approval |
| D-10 | Online activation: optional future capability, not required initially | APPROVED | No commercial requirement yet; offline is the foundation | 2026-09-11 | This decision approval |
| D-11 | Cryptographic algorithm: RSA-PSS with SHA-256 using RSA-3072 for the initial implementation | APPROVED | Aligns with existing RSA policy; avoids ECDSA nonce-reuse hazard | 2026-09-11 | This decision approval |
| D-12 | Key rotation: multiple trusted license-signing public keys must be supported | APPROVED | Enables rotation without invalidating valid licenses | 2026-09-11 | This decision approval |
| D-13 | License storage: configurable license file location | APPROVED | Operational flexibility | 2026-09-11 | This decision approval |
| D-14 | Invalid/missing/expired license: application enters restricted mode rather than silently continuing | APPROVED | Fail closed without an outage | 2026-09-11 | This decision approval |
| D-15 | Licensing must NEVER disable or weaken authentication or other security controls | FINAL (constraint) | Security regression is worse than unlicensed use | 2026-09-11 | This decision approval |
| D-16 | Vendor private key: never distributed with LabAuthServer | FINAL (constraint) | Key custody | 2026-09-11 | This decision approval |
| D-17 | GitHub: production vendor private key must NEVER be committed to GitHub | FINAL (constraint) | Repository is an exposure surface | 2026-09-11 | This decision approval |
| D-18 | CI: production vendor private key must NEVER be provided to GitHub Actions | FINAL (constraint) | CI logs, forks and workflow runs are an exposure surface | 2026-09-11 | This decision approval |
| D-19 | CI testing: a separate test signing key may be used for automated licensing tests | APPROVED | Requires no stored production secret | 2026-09-11 | This decision approval |
| D-20 | Unknown features/editions: default-deny | APPROVED | Prevents silent widening of entitlement | 2026-09-11 | This decision approval |
| D-21 | Validation uses typed/structured validation results rather than boolean-only results | APPROVED | Boolean-only validation cannot be diagnosed or tested safely | 2026-09-11 | This decision approval |
| D-22 | Revocation: may be provided by future online activation; NOT part of the initial offline-only implementation | APPROVED | Offline licenses cannot be recalled | 2026-09-11 | This decision approval |

Historical note: the original 22 rows used different IDs and wording before this approval (D-01 offline-first, D-10 algorithm, D-14 machine binding, etc.). They are superseded by the rows above and are retained in the section below for traceability.

### Superseded / historical wording (retained for traceability)

| ID | Decision | Prior status | Reason | Date | Evidence |
| --- | --- | --- | --- | --- | --- |
| D-H-01 | Offline-first licensing; no Internet dependency in the first implementation | PROPOSED → approved as D-02 | Customers may be air-gapped; online activation is a later option | 2026-09-11 | [4.0](Phase-4.0-Requirements-and-Licensing-Model.md), [4.13](Phase-4.13-Online-Activation-Future.md) |
| D-H-02 | Licenses are signed documents, not opaque blobs | PROPOSED → approved as D-05 | Auditable, versionable, testable | 2026-09-11 | [4.2](Phase-4.2-License-Document-Format.md) |
| D-H-03 | Vendor private key is separated from LabAuthServer entirely | FINAL (constraint) → approved as D-16 | Key custody; the key must never ship | 2026-09-11 | [4.1](Phase-4.1-License-Architecture.md), [4.4](Phase-4.4-License-Issuer.md) |
| D-H-04 | Only the vendor public key is distributed with LabAuthServer | PROPOSED → approved as D-12 | Verification needs no secret | 2026-09-11 | [4.1](Phase-4.1-License-Architecture.md) |
| D-H-05 | The server trusts a key set, not a single key | PROPOSED → approved as D-12 | Enables rotation without invalidating valid licenses | 2026-09-11 | [4.3](Phase-4.3-Cryptographic-Signing-and-Verification.md) |
| D-H-06 | Validation is default-deny and fail-closed on every path | FINAL (constraint) → approved as D-14, D-20 | Only safe default for a licensing control | 2026-09-11 | [4.5](Phase-4.5-License-Validation.md), [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md) |
| D-H-07 | Validation returns a typed result with an internal reason code and a public-safe category | PROPOSED → approved as D-21 | Boolean-only validation cannot be diagnosed or tested safely | 2026-09-11 | [4.5](Phase-4.5-License-Validation.md) |
| D-H-08 | Feature gating is default-deny; unknown and missing features are denied | PROPOSED → approved as D-05, D-20 | Prevents silent widening of entitlement | 2026-09-11 | [4.6](Phase-4.6-Feature-and-Edition-Enforcement.md) |
| D-H-09 | Licensing never disables or weakens authentication or security controls | FINAL (constraint) → approved as D-15 | Security regression is worse than unlicensed use | 2026-09-11 | [4.0](Phase-4.0-Requirements-and-Licensing-Model.md), [4.6](Phase-4.6-Feature-and-Edition-Enforcement.md) |
| D-H-10 | First algorithm: RSA-PSS with SHA-256, 3072-bit key | PROPOSED → approved as D-11 | Aligns with existing RSA 2048–4096 policy; avoids ECDSA nonce-reuse hazard | 2026-09-11 | [4.3](Phase-4.3-Cryptographic-Signing-and-Verification.md) |
| D-H-11 | Ed25519 remains a candidate behind the `alg` field | PROPOSED | Forward compatibility without a format change | 2026-09-11 | [4.3](Phase-4.3-Cryptographic-Signing-and-Verification.md) |
| D-H-12 | Timestamps are UTC ISO 8601, second precision, `Z` suffix | FINAL (constraint) | Avoids timezone-dependent behavior | 2026-09-11 | [4.2](Phase-4.2-License-Document-Format.md), [4.7](Phase-4.7-Expiration-and-Grace-Period.md) |
| D-H-13 | Grace period is explicit, bounded, opt-in and never permanent | PROPOSED → superseded by D-08 (no grace initially) | Prevents hidden permanent grace | 2026-09-11 | [4.7](Phase-4.7-Expiration-and-Grace-Period.md) |
| D-H-14 | Machine binding is deferred in the first implementation | DEFERRED → approved as D-09 | False-positive lockout risk without an online rebind path | 2026-09-11 | [4.8](Phase-4.8-Machine-Binding.md) |
| D-H-15 | Online activation is deferred | DEFERRED → approved as D-10 | No commercial requirement yet; offline is the foundation | 2026-09-11 | [4.13](Phase-4.13-Online-Activation-Future.md) |
| D-H-16 | CI never receives the production vendor private signing key | FINAL (constraint) → approved as D-18 | CI logs, forks and workflow runs are an exposure surface | 2026-09-11 | [4.11](Phase-4.11-GitHub-CI-Integration.md) |
| D-H-17 | Licensing tests generate an ephemeral signing key per run | PROPOSED → approved as D-19 | Requires no stored secret | 2026-09-11 | [4.10](Phase-4.10-Testing-Strategy.md), [4.11](Phase-4.11-GitHub-CI-Integration.md) |
| D-H-18 | The 733-test baseline must be preserved or increased | FINAL (constraint) | Regression gate | 2026-09-11 | [4.10](Phase-4.10-Testing-Strategy.md), [4.11](Phase-4.11-GitHub-CI-Integration.md) |
| D-H-19 | No obfuscation or anti-debugging measure is claimed as protection | PROPOSED | Does not survive source delivery | 2026-09-11 | [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md) |
| D-H-20 | The limitation statement is published in release notes | FINAL (constraint) | Prevents overclaiming | 2026-09-11 | [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md), [4.14](Phase-4.14-Documentation-and-Release.md) |
| D-H-21 | Release is gated on 4.15 completion | PROPOSED | Security gate | 2026-09-11 | [4.15](Phase-4.15-Final-Security-Review.md) |
| D-H-22 | No commit or push without explicit authorization | FINAL (constraint) | Change control | 2026-09-11 | [Phase 4 README](Phase-4-README.md) |

## Open questions resolved by the 2026-09-11 approval

| ID | Question | Resolution | Status |
| --- | --- | --- | --- |
| O-01 | Edition names and feature-to-edition mapping | Editions Community / Professional / Enterprise approved (D-04); exact feature-to-edition mapping still TO BE CONFIRMED DURING IMPLEMENTATION | PARTIALLY RESOLVED |
| O-02 | Unlicensed deployment behavior: restricted mode or refuse to start | Community/restricted mode approved (D-03, D-14); application does not refuse to start | RESOLVED |
| O-14 | Machine binding: adopt or defer | Deferred (D-09) | RESOLVED |
| O-19 | Online activation: pursue or defer | Deferred; optional future capability (D-10, D-22) | RESOLVED |

## Open decisions still awaiting approval

| ID | Question | Recommendation | Owner | Status |
| --- | --- | --- | --- | --- |
| O-03 | Canonicalization scheme: RFC 8785, project profile, or non-JSON | RFC 8785 if libraries are trusted, otherwise a fixed project profile | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-04 | Signature envelope shape: enveloped, detached or container | Enveloped `signature` object, single file | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-06 | Minimum accepted RSA key size | Reject below 2048, warn below 3072 | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-07 | Private key source for the issuer | File path for prototype; certificate store or HSM for production | Operations | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-08 | Issuer placement: inside or outside the server solution | Outside the server build graph | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-09 | Validation cadence: startup, periodic, per-request | Startup plus bounded periodic revalidation | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-10 | License file location | Configured path with documented default (model approved as D-13) | Operations | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-11 | Grace period length and where it is carried | Not applicable initially — no grace period approved (D-08) | Project owner | RESOLVED — NO GRACE |
| O-12 | Behavior after grace expires | Deny licensed features, enter restricted mode, plus operator signal; never refuse to start | Project owner | RESOLVED |
| O-13 | Clock-skew allowance value | Small, configurable, conservatively defaulted | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-15 | Additional validation points beyond startup and periodic | None initially | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-16 | CI test-key strategy | Separate test signing key (D-19); exact generation ephemeral per run | Architect | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-17 | Issuance authority and approval evidence | Named owner plus second approver | Operations | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-18 | Vendor license register format | Simple register with a stable schema | Operations | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-20 | Customer-visible versus internal documents | Install/renew and limitation statement visible; issuer and runbook internal | Project owner | TO BE CONFIRMED DURING IMPLEMENTATION |
| O-21 | Release vehicle: major or minor | Minor release with explicit release notes | Project owner | TO BE CONFIRMED DURING IMPLEMENTATION |
| A row becomes `APPROVED` only with a recorded approver and date. The 2026-09-11 approval is recorded above.
- Never delete a row; supersede it with a new row that references the old one.
- Every implementation phase must cite the decision IDs it depends on.
- An unresolved open decision is a blocker for the phase that depends on it.
- The approved architectural constraint on bypassability (source is distributed, so checks can be removed; the goal is legal licensing, cryptographic authenticity, default-deny enforcement and resistance to ordinary misuse, not unbreakable DRM) is recorded verbatim in [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md) and must not be contradicted
- Move a row from PROPOSED to FINAL only with a recorded approver and date.
- Never delete a row; supersede it with a new row that references the old one.
- Every implementation phase must cite the decision IDs it depends on.
- An unresolved open decision is a blocker for the phase that depends on it.

## Phase 4.17 approved governance decisions (2026-09-11)

The following supersede the earlier open rows for Phase 4.17 implementation:

- O-01: Approved matrix is Community = `auth.basic`, `auth.jwt`; Professional = those plus `auth.ldap`, `audit.logging`; Enterprise = all five known features. Edition is an upper bound and never an implicit grant. The signed feature list remains authoritative.
- O-07: Production private keys are offline vendor-controlled secrets and are absent from the repository, CI, application output, customer servers, and tests. Servers receive public keys only. Physical custody technology remains outside this repository.
- O-17: Issuance is performed offline by an authorized vendor licensing operator. LabAuthServer is not an issuer and no online licensing service is introduced.
- O-18: An external controlled license register is required with the minimum fields documented in the Phase 4.17 implementation record; it contains no private keys.
- O-20/O-21: Licenses remain external files delivered out of band and configured through `Licensing:LicenseFilePath`; replacement requires validation, atomic replacement, restart/reload, and revalidation.