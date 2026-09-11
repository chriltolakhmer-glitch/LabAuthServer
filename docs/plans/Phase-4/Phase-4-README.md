# Phase 4 ��� Technical License Enforcement

Status: PLANNING COMPLETE — DECISIONS APPROVED. Implementation NOT STARTED. This document set is a permanent project record. It authorizes no implementation, no source change, no configuration change and no commit. [Master roadmap](../README.md).

> Phase 4 planning decisions approved; implementation has not started.

The 22 approved Phase 4 decisions are recorded in the [Decision Log](Phase-4-Decision-Log.md) with status `APPROVED` (date 2026-09-11). Details that were not approved remain marked `TO BE CONFIRMED DURING IMPLEMENTATION`.

## Approved architecture vs. implementation details still to be confirmed

APPROVED ARCHITECTURE (baseline, no longer merely proposed):

- Licensing model: source-available + commercial technical license (D-01).
- Offline-first architecture; no Internet dependency initially (D-02).
- Missing license starts the application in Community/restricted mode (D-03).
- Editions: Community, Professional, Enterprise (D-04).
- Explicit feature list in the signed license (D-05); maximum-user limits allowed (D-06).
- Perpetual and time-limited licenses both supported (D-07); no grace period initially (D-08).
- Machine binding deferred (D-09); online activation optional future capability (D-10).
- Cryptography: RSA-PSS with SHA-256 using RSA-3072 for the initial implementation (D-11).
- Multiple trusted license-signing public keys for rotation (D-12).
- Configurable license file location (D-13).
- Invalid/missing/expired license enters restricted mode, not silent continuation (D-14).
- Licensing never disables or weakens authentication or security controls (D-15).
- Vendor private key never distributed with LabAuthServer, never committed to GitHub, never provided to GitHub Actions (D-16, D-17, D-18).
- Separate test signing key for automated licensing tests (D-19).
- Default-deny for unknown features/editions (D-20).
- Typed/structured validation results, not boolean-only (D-21).
- Revocation only via future online activation, not in the initial offline implementation (D-22).

IMPLEMENTATION DETAILS STILL TO BE CONFIRMED:

- Canonicalization scheme (O-03).
- Signature envelope shape (O-04).
- Minimum accepted RSA key size (O-06).
- Private key source for the issuer (O-07).
- Issuer placement in or outside the server solution (O-08).
- Validation cadence and license file location (O-09, O-10).
- Clock-skew allowance value (O-13).
- Additional validation points (O-15).
- CI test-key strategy details (O-16).
- Issuance authority, vendor license register format (O-17, O-18).
- Document visibility, release vehicle, security reviewer (O-20, O-21, O-22).

Naming note: `docs/plans/Phase-4/README.md` already exists and describes the *Identity Federation & Standards* candidate scope registered in the [master roadmap](../README.md). Technical License Enforcement is a separate, self-contained plan set that happens to live in the same folder. Neither document overrides the other; this file is the entry point for Technical License Enforcement only.

Planning baseline: `main`, commit `237802f0aeea87da671172fcef7a32affa38885e`. No build, test or remote fetch was performed for this task.

## 1. What Phase 4 is

Phase 4 designs a Technical License Enforcement capability for LabAuthServer:

- The software vendor signs a license document with a private key.
- LabAuthServer ships only the vendor public key.
- LabAuthServer verifies the signature offline and enforces edition, features, limits and expiration.
- Invalid, modified, expired or unsupported licenses fail closed.
- A missing or invalid license places the application in Community/restricted mode rather than silently continuing or refusing to start.

Phase 4 planning decisions are approved (2026-09-11). Implementation has NOT started. Phase 4 is delivered as a sequence of document-only and then implementation-only increments. Phase 4.0–4.2 are documentation. Phase 4.3 onward are code-bearing and each requires separate authorization.

## 2. Why technical licensing is being added

- Provide a cryptographically authentic, machine-verifiable record of what a customer is entitled to use.
- Enable commercial editions and optional features without shipping different binaries per customer.
- Give the vendor a single issuer tool so license content is generated, not hand-edited.
- Keep enforcement separate from authentication and security controls so licensing can never weaken them.

## 3. What technical license enforcement can and cannot protect

CAN protect:

- License content authenticity: a customer cannot produce a license that verifies against the vendor public key without the vendor private key.
- Accidental or ordinary misuse: editing fields, swapping files, using an expired license, using another product's license.
- Ordinary tampering: replaced or truncated license files, wrong key identifier, malformed payload.
- Commercial entitlement bookkeeping when combined with a future online activation service.

CANNOT protect:

- A sufficiently capable party holding the source code. Because LabAuthServer is delivered as source, license checks can be removed or altered. Technical enforcement is a commercial and authenticity control, not unbreakable DRM.
- A customer who controls the build, the runtime host or the signing configuration of their own deployment.
- Revocation, activation counting or telemetry without a future online component.

Required disclaimer, reproduced verbatim in [Phase-4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md):

> Because the customer receives source code, technical enforcement can be modified by a sufficiently capable party. The objective is to prevent accidental/ordinary misuse and provide cryptographically authentic licensing, not to claim unbreakable DRM.

Approved architectural constraint (recorded verbatim, 2026-09-11):

> Because LabAuthServer source code is distributed to customers, technical license enforcement cannot make the software impossible to modify. A sufficiently capable party controlling the source can remove or alter client-side license checks. The purpose of the system is to provide legally defined licensing, cryptographically authentic licenses, default-deny enforcement, and resistance to ordinary misuse—not unbreakable DRM.

Do not claim that the licensing system is impossible to bypass.

The design therefore distinguishes four separate things: legal licensing, cryptographic license authenticity, technical enforcement, and optional online activation. Do not conflate them in any later document or release note.

## 4. Overall architecture

```
Vendor
  |
  | private signing key
  v
License Issuer
  |
  | signed license
  v
license.lic
  |
  | customer installs
  v
LabAuthServer
  |
  | parse
  v
License Validator
  |
  | verify signature
  v
Embedded Vendor Public Key
  |
  +--> valid
  +--> invalid
  +--> expired
  +--> unsupported
  +--> revoked/disabled (future online only)
  |
  v
License Policy
  |
  +--> edition
  +--> features
  +--> limits
  +--> expiration
  |
  v
Application Feature Enforcement
```

Key separation:

| Material | Custody | Shipped with LabAuthServer | In Git | In CI |
| --- | --- | --- | --- | --- |
| Vendor private signing key | Vendor only | Never | Never | Never |
| Vendor public key | Vendor publishes | Yes, embedded or configured | Yes | Yes |
| Test private key | Ephemeral, test-only | No | Never | Generated at test time |
| Test public key | Test code | No | Yes | Yes |

## 5. Phase dependency diagram

```
4.0 Requirements
    |
    v
4.1 Architecture
    |
    v
4.2 License Format
    |
    v
4.3 Cryptography
    |
    v
4.4 Issuer
    |
    v
4.5 Server Validation
    |
    v
4.6 Feature Enforcement
    |
    v
4.7 Expiration
    |
    v
4.8 Optional Binding (deferred by default)
    |
    v
4.9 Tamper Testing
    |
    v
4.10 Testing Strategy
    |
    v
4.11 CI Integration
    |
    v
4.12 Operations
    |
    v
4.13 Online Activation (future)
    |
    v
4.14 Documentation and Release
    |
    v
4.15 Final Security Review
```

4.8 and 4.13 are OPTIONAL and may be declined. Declining them must not block 4.9–4.15.

## 6. Implementation order

1. Freeze requirements (4.0) and architecture (4.1) before any code.
2. Freeze the license format (4.2) before writing the issuer, because the format is the contract between issuer and validator.
3. Implement and test cryptography (4.3) before the issuer (4.4).
4. Implement the issuer (4.4) before server validation (4.5), so test licenses exist.
5. Implement validation (4.5) before feature enforcement (4.6), so enforcement has a trusted policy source.
6. Add expiration (4.7) before tamper testing (4.9).
7. Land tests (4.10) and CI (4.11) before operations (4.12).

## 7. Security principles

1. Fail closed. Any unverifiable, unparsable, unknown or expired license denies licensed functionality.
2. Licensing never disables authentication, authorization, TLS, LDAP transport security, request limits or audit. Licensing restricts commercial functionality only.
3. The private key never enters the repository, the build, the CI pipeline or the running server.
4. Validation is a pure function of license bytes plus trusted public keys. No network dependency in the first implementation.
5. Structured, typed validation results. No boolean-only validation for anything security-relevant.
6. Public API responses expose no signing detail, no internal validation reason, no customer data and no cryptographic material.
7. Default deny for unknown editions, unknown features and out-of-range limits.
8. Key rotation is designed in from the start: the server trusts a key set, not a single key.
9. Licensing code stays out of the authentication path. It observes, it does not gate credentials.

## 8. Testing principles

- Every validation rule has a positive and a negative test.
- Negative tests assert the specific structured failure code, not just "false".
- Licensing tests never require the production private key.
- Tests prove that an invalid license does not weaken or bypass authentication.
- Tests prove that an unlicensed optional feature is denied while licensed features remain available.
- The existing baseline must not regress: 733 tests, 733 passed, 0 failed, 0 skipped, Release build, 0 warnings, 0 errors.

## 9. Git workflow

- One phase per change set. Documentation phases commit only Markdown under `docs/plans/Phase-4/`.
- Implementation phases never mix with security, deployment, configuration or dependency changes.
- No `git add`, `git commit` or `git push` without explicit authorization.
- Every phase records a rollback point before it starts.

## 10. Commit strategy

Recommended messages:

- `docs(phase-4): add technical license enforcement plan`
- `feat(licensing): add license document model and canonicalization`
- `feat(licensing): add signature verification with trusted key set`
- `feat(licensing): add offline license validator`
- `feat(licensing): enforce edition features and limits`
- `test(licensing): add validation, tamper and boundary tests`
- `ci(licensing): add licensing tests without vendor private key`

Do not combine unrelated phases in one commit.

## 11. Rollback strategy

- Documentation phases: revert the commit; no runtime effect.
- Implementation phases: the license subsystem must be removable by reverting its commit range because enforcement is additive and default-permissive only where an approved decision says so.
- Every phase declares a rollback point (last known-good commit) and the expected behavior after rollback: the server reverts to the previous licensing behavior, never to a broken startup.
- The 22 approved baseline decisions are recorded as `APPROVED` in the [Decision Log](Phase-4-Decision-Log.md); unapproved implementation details remain `TO BE CONFIRMED DURING IMPLEMENTATION`ication cannot start for an unlicensed deployment unless a later, explicitly approved decision says so.

## 12. Definition of Done (Phase 4)

- All 16 phase documents plus the four supporting registers exist under `docs/plans/Phase-4/`.
- Every phase document contains all required sections.
- Every DECISION REQUIRED is marked PROPOSED, not FINAL.
- The license format, algorithm choice and trust model are documented with trade-offs.
- Tests cover every failure case listed in [Phase-4.10](Phase-4.10-Testing-Strategy.md).
- CI runs restore, Release build, all existing tests and licensing tests, with no vendor private key present.
- The 733-test baseline is preserved or increased.
- No private key, secret, production configuration or source change is present in the documentation change set.
- [Phase-4.15](Phase-4.15-Final-Security-Review.md) is completed and signed off before any release.

## 13. Document index

| Document | Purpose |
| --- | --- |
| [Phase-4.0](Phase-4.0-Requirements-and-Licensing-Model.md) | Requirements and licensing model |
| [Phase-4.1](Phase-4.1-License-Architecture.md) | License architecture |
| [Phase-4.2](Phase-4.2-License-Document-Format.md) | License document format |
| [Phase-4.3](Phase-4.3-Cryptographic-Signing-and-Verification.md) | Cryptographic signing and verification |
| [Phase-4.4](Phase-4.4-License-Issuer.md) | License issuer tool |
| [Phase-4.5](Phase-4.5-License-Validation.md) | License validation in LabAuthServer |
| [Phase-4.6](Phase-4.6-Feature-and-Edition-Enforcement.md) | Feature and edition enforcement |
| [Phase-4.7](Phase-4.7-Expiration-and-Grace-Period.md) | Expiration and grace-period rules |
| [Phase-4.8](Phase-4.8-Machine-Binding.md) | Optional machine/installation binding |
| [Phase-4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md) | Tamper and abuse resistance |
| [Phase-4.10](Phase-4.10-Testing-Strategy.md) | Testing strategy |
| [Phase-4.11](Phase-4.11-GitHub-CI-Integration.md) | GitHub CI integration |
| [Phase-4.12](Phase-4.12-Operational-License-Management.md) | Operational license management |
| [Phase-4.13](Phase-4.13-Online-Activation-Future.md) | Future online activation architecture |
| [Phase-4.14](Phase-4.14-Documentation-and-Release.md) | Documentation and release |
| [Phase-4.15](Phase-4.15-Final-Security-Review.md) | Final security review |
| [Decision Log](Phase-4-Decision-Log.md) | Decisions and their status |
| [Implementation Checklist](Phase-4-Implementation-Checklist.md) | Trackable implementation steps |
| [Risk Register](Phase-4-Risk-Register.md) | Risks and mitigations |
| [Change Record](Phase-4-Change-Record.md) | Running change history |

## 14. Related existing documentation

- [Master roadmap](../README.md)
- [Security](../../Security.md)
- [Configuration](../../Configuration.md)
- [Deployment](../../Deployment.md)
- [Testing](../../Testing.md)
- [Operations](../../Operations.md)
- [JWT](../../JWT.md)
- [Validation Status](../../Validation_Status.md)
- [Project Status](../../Project_Status.md)
- Repository `AGENTS.md` and `.github/copilot-instructions.md`

Read those before implementing any phase. Do not duplicate their content here.