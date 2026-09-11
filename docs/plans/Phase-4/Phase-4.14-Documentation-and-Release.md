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

---

## Implementation record (2026-09-11)

Status: COMPLETE — DOCUMENTATION READY; NO RELEASE PERFORMED. No source code, test, configuration or CI change was made.

### Authorization status

Phase 4.14 is a documentation/release-readiness phase. It authorizes documentation updates and a release-readiness checklist; it does not authorize publishing packages, binaries or releases, generating keys, or adding deployment automation. None of those was performed. The stop-and-report clause did not trigger.

### Customer-visible documentation set

The repository already has a `docs/` documentation set with an established shape. Rather than invent a new documentation architecture, the licensing documentation was added to that set:

| Document | Audience | Change |
| --- | --- | --- |
| `docs/Licensing.md` | Customer operators and technical staff | **New.** Full licensing overview covering what is implemented, what is not, the license document concept, editions, features, limits, perpetual/expiring licenses, restricted Community behaviour, validation, cryptographic verification, trusted keys, rotation, tampering, install/replace/backup/recovery pointers, troubleshooting, security model, offline-first behaviour, source-available limitations, future online licensing, machine binding, known limitations and open decisions. |
| `README.md` | All | Added one cross-reference to `docs/Licensing.md`, noting that licensing is not yet wired into the running host. |
| `docs/Security.md` | All | Added a short licensing cross-reference section stating the security-independence constraint (D-15) and the restricted-mode fallback. |
| `docs/Configuration.md` | All | Added a `Licensing` section documenting `LicenseFilePath` (empty default, no hard-coded path) and noting that no `Licensing` section exists in the shipped `appsettings*.json`. |
| `docs/Operations.md` | Operators | Added an operational licensing section pointing to the Phase 4.12 procedures and the key-custody boundary. |
| `docs/Troubleshooting.md` | Support | Added a licensing troubleshooting section mapping the public-safe status values to causes and forbidding the vendor private-key request. |
| `docs/Testing.md` | All | Added a licensing test section with the current baseline (745 unit + 246 integration = 991) and the TEST-ISOLATION-1 limitation. |
| `docs/plans/README.md` | All | Added a Technical License Enforcement plan-set section so the Phase 4 plan set is discoverable from the roadmap. |

### Internal documents (not published)

The Phase 4.0–4.13 plan documents under `docs/plans/Phase-4/` are internal design records. They are not customer-facing. The issuer is vendor-side and remains outside the server build graph.

### What was explicitly NOT done

- No release was created or published.
- No package or binary was published.
- No production signing key was generated.
- No GitHub secret was created.
- No deployment script, GitHub release automation, signing automation or production license generation was added.
- No source code or test was changed.
- No CI workflow was changed.
- No production `appsettings` was modified.
- No private key, production certificate, credential, production license file or customer secret was included.

### License example policy

The documentation describes the license document conceptually and does not embed a full example license. No fictional license container is published in a form that could be mistaken for a production license; no production key ID, customer identifier or credential is used anywhere.

### Security model and source-available limitation

The `docs/Licensing.md` security section states the controls that licensing does provide (cryptographic integrity, trusted-key verification, strict parsing, fail-closed validation, default-deny enforcement boundary, expiration handling, tamper and boundary protections) and the source-available limitation (a customer controlling source, binaries and host can bypass client-side enforcement). No claim of unbreakable DRM, impossible bypass, tamper-proof binaries or hardware-level enforcement is made. No anti-debugging or similar mechanism was introduced.

### Offline-first, machine binding, grace, CI, tests

- Offline-first is documented as the current behaviour: no activation server, no revocation service, no telemetry, no network call for license validation. Online activation/revocation remain future work (Phase 4.13).
- Machine binding is documented as deferred: no machine identity required, no binding field in the license format, no rebind workflow.
- Grace is documented as default-disabled, bounded and diagnostic only; it is not an entitlement bypass.
- CI documentation records the Phase 4.11 review outcome: the existing workflow performs checkout, .NET setup, cache, restore, Release build and test with `permissions: contents: read`. **CI configuration validated locally; GitHub-hosted run not executed/verified.**
- Test documentation records the final state (745 unit + 246 integration = 991 passing, 0 failed, 0 skipped with `-m:1`) and the TEST-ISOLATION-1 limitation. Parallel execution of the full solution is not claimed to be reliable.

### Release-readiness checklist

| # | Item | Status |
| --- | --- | --- |
| 1 | Source review (licensing code present and reviewed in Phases 4.1–4.10) | COMPLETE |
| 2 | Test verification (`-m:1` full suite green) | COMPLETE |
| 3 | Security review (Phase 4.9 and 4.15 scope) | PHASE 4.15 NOT STARTED — release gate not released |
| 4 | License format review (format frozen by Phase 4.2; canonicalization O-03 still OPEN) | PARTIAL — OPEN DECISION |
| 5 | Cryptographic review (RSA-PSS/SHA-256 implemented; O-06 wording OPEN) | PARTIAL — OPEN DECISION |
| 6 | Key custody (vendor private key never in repo, CI or server) | COMPLETE — production key source O-07 still OPEN |
| 7 | Documentation (this phase) | COMPLETE |
| 8 | Operational procedures (Phase 4.12 documented; no runtime loader) | DOCUMENTED — LOADER NOT IMPLEMENTED |
| 9 | Backup/recovery (Phase 4.12 documented) | DOCUMENTED |
| 10 | CI (Phase 4.11 reviewed; no workflow change) | COMPLETE — GITHUB-HOSTED RUN NOT VERIFIED |
| 11 | Release artifacts (what could safely be distributed) | NOT DEFINED — no release process exists for licensing; recorded as a gap |
| 12 | Secrets scan (no secret in changed files) | COMPLETE |
| 13 | Private-key scan (no private key in changed files or repository) | COMPLETE |
| 14 | Production configuration review (no production `appsettings` modified) | COMPLETE — concrete license path O-10 still OPEN |
| 15 | Final security sign-off (Phase 4.15) | NOT STARTED — RELEASE GATE CLOSED |

### Findings

| ID | Finding | Severity | Disposition |
| --- | --- | --- | --- |
| 4.14-F-1 | The server has no runtime license loader, no DI registration and no `Licensing` section in `appsettings*.json`; the operational install/replace procedures documented in 4.12 are therefore not yet executable end to end. | Medium | Documented as a known limitation in `docs/Licensing.md`; wiring the loader is a separate authorized phase that depends on the still-OPEN O-10 and O-09 |
| 4.14-F-2 | No release process or release-artifact definition exists for the licensing feature. | Low | Recorded as a release-readiness gap; no automation was invented |
| 4.14-F-3 | TEST-ISOLATION-1 remains OPEN; full parallel solution test execution is not reliable. | Low–Medium | Documented accurately in `docs/Testing.md`; not fixed in this phase |

### Final validation

- Release build: succeeded, 0 warnings, 0 errors.
- Full test suite with `dotnet test LabAuthServer.slnx -c Release --no-build -m:1`: Unit 745, Integration 246, total 991 passed, 0 failed, 0 skipped.
- `git diff --check`: clean.
- No production code, test, configuration or CI change; no secret and no private key introduced.
- Documentation accurately distinguishes IMPLEMENTED, APPROVED, DEFERRED, FUTURE, OPEN and NOT IMPLEMENTED.
- **CI configuration validated locally; GitHub-hosted run not executed/verified.**