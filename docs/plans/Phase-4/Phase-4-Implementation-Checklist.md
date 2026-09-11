# Phase 4 — Implementation Checklist

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md).

Legend: `[ ]` Not started, `[~]` In progress, `[x]` Complete, `[!]` Blocked, `[-]` Deferred.

Nothing below is complete except the documentation rows already produced by this planning task. Implementation rows are unchecked by definition.

## Phase 4.0 — Requirements and licensing model

- [x] Requirement list documented
- [x] Four-way distinction documented (legal, cryptographic, technical, online)
- [x] Source-code limitation documented
- [ ] Edition and feature model approved (O-01)
- [ ] Unlicensed deployment behavior approved (O-02)
- [ ] Decisions recorded as FINAL in the decision log

## Phase 4.1 — License architecture

- [x] Component responsibilities documented
- [x] Trust boundaries documented
- [x] Key custody table documented
- [ ] Public key set storage decision resolved (O-04 related)
- [ ] Issuer project placement approved (O-08)
- [ ] Layering placement confirmed against `AGENTS.md`

## Phase 4.2 — License document format

- [x] Required and optional fields documented
- [x] Validation order documented
- [x] License document model implemented (`LicenseDocument`, `SignedLicense`, `LicenseSignatureEnvelope`)
- [x] Strict container parser implemented (`JsonLicenseDocumentParser`)
- [x] Signature envelope shape selected for this model (O-04): `algorithm`, `keyId`, `signature`
- [ ] Canonicalization scheme chosen (O-03, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Worked canonical example with exact bytes recorded (TO BE CONFIRMED DURING IMPLEMENTATION)

## Phase 4.3 — Cryptographic signing and verification

- [x] Algorithm trade-offs documented
- [x] Rotation strategy documented
- [x] First algorithm confirmed (RSA-PSS + SHA-256 + RSA-3072, D-11)
- [x] Library availability verified on the target runtime (`System.Security.Cryptography`, framework only, no new package)
- [x] Verification implemented in the server (`RsaPssLicenseSignatureVerifier`)
- [x] Multiple trusted public keys supported for rotation (`InMemoryTrustedLicenseKeyProvider`, D-12)
- [x] Round-trip and negative cryptographic tests added (`Phase43CryptographicVerificationTests`)
- [x] Public-only key enforcement (F-3) resolved
- [ ] Minimum key size policy confirmed (O-06, TO BE CONFIRMED DURING IMPLEMENTATION; 2048 enforced in code)
- [ ] Signing implemented in the issuer (Phase 4.4, NOT STARTED)
- [ ] F-2 envelope binding revisited before the production license format is frozen

## Phase 4.4 — License issuer

- [x] Issuer responsibilities and prohibitions documented
- [x] Issuer project created (`tools/LabAuthServer.LicenseIssuer/`, outside `LabAuthServer.slnx`)
- [x] Dependency direction confirmed (Domain only; no server project references it)
- [x] Signing-key provider abstraction implemented (`ILicenseSigningKeyProvider`)
- [x] Signing abstraction implemented (`ILicenseSigner`, `RsaPssLicenseSigner`)
- [x] Input validation implemented (product, edition, timestamps, expiry, feature, limit, keyId, key policy)
- [x] Signing implemented (RSA-PSS + SHA-256, Base64 signature)
- [x] Deterministic payload serialization implemented and documented (O-03 current implementation)
- [x] Container output compatible with `JsonLicenseDocumentParser`
- [x] Issuer tests using an ephemeral key added (`Phase44LicenseIssuerTests`, 35 tests)
- [x] Confirmed the server cannot invoke the issuer (issuer not in the server build graph)
- [ ] Private key source decided (O-07, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] CLI command surface
- [ ] Non-sensitive audit logging implemented

## Phase 4.5 — License validation

- [x] Validation rule table documented
- [x] Typed result model documented
- [x] Safe error mapping documented
- [ ] Validation cadence decided (O-09)
- [ ] License file location decided (O-10)
- [x] Validator implemented in the documented order (`LicenseValidator`; parse → algorithm → key → signature → product → edition → time → features → limits → expiry)
- [x] Public-safe category mapping implemented (`LicenseValidationStatus`; internal `LicenseValidationReason` never public)
- [x] Rule-by-rule tests added (`Phase45LicenseValidatorTests`, 37 tests)
- [x] Clock abstraction implemented (`ILicenseClock`, `SystemLicenseClock`)
- [x] Structural bounds implemented (`LicenseValidationPolicy`)
- [x] M-1 resolved (unknown container and payload properties rejected; duplicates still rejected)
- [x] M-2 resolved (strict, non-normalising Base64 for payload and signature)

## Phase 4.6 — Feature and edition enforcement

- [x] Feature identifier scheme documented
- [x] Edition hierarchy documented
- [x] Default-deny documented
- [x] Known feature catalog implemented (`LicenseFeatureIds`)
- [x] Known limit catalog implemented (`LicenseLimitKeys`)
- [x] Policy boundary implemented (`ILicensePolicy`, `LicensePolicy`, `ILicensePolicyProvider`)
- [x] Typed feature decision implemented (`LicenseFeatureDecision`)
- [x] Default-deny verified for absent, unknown and null features
- [x] Restricted policy implemented for every non-valid validation status
- [x] Limits fail closed: missing/unknown key is never unlimited; `int.MaxValue` boundary and negative usage covered
- [x] Security-independence test added
- [x] Tests added (`Phase46FeatureAndEditionEnforcementTests`, 47 tests)
- [ ] Edition and feature mapping approved (O-01, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Enforcement timing approved (O-09, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Denied-feature API response approved (TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Enforcement added to licensed features only (endpoint integration deferred)

## Phase 4.7 — Expiration and grace period

- [x] Timeline semantics documented
- [x] Clock handling rules documented
- [x] Clock abstraction implemented (`ILicenseClock`, `SystemLicenseClock`, Phase 4.5)
- [x] Expiry implemented in a single evaluator (`ILicenseExpirationEvaluator`, `LicenseExpirationEvaluator`)
- [x] Typed timeline state and outcome (`LicenseExpirationState`, `LicenseExpirationStatus`)
- [x] Grace explicitly default-disabled and bounded (`LicenseGracePeriod`; `MaximumGracePeriod` 90 days)
- [x] Invalid grace configuration rejected, never treated as unlimited
- [x] Boundary tests added (`Phase47ExpirationAndGracePeriodTests`, 28 tests)
- [x] Perpetual regression covered
- [x] Clock-skew boundary covered on both sides
- [ ] Grace length and carrier decided (O-11, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Clock-skew allowance confirmed (O-13, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Operator-visible warning logging implemented (no logging surface yet)

## Phase 4.8 — Machine binding

- [x] Options analyzed
- [x] False-positive scenarios documented
- [x] Recommendation recorded
- [x] Approval status assessed: NOT approved for implementation (D-09 APPROVED = deferred; O-14 RESOLVED = deferred)
- [x] Read-only security analysis recorded (identity stability, virtualization, HA, spoofability, recovery)
- [x] Required future decisions enumerated
- [-] Binding implementation (deferred)
- [-] Rebinding tooling (deferred)
- [-] License-format binding field (deferred)
- [-] Server-side binding check (deferred)

## Phase 4.9 — Tamper and abuse resistance

- [x] Realistic protections documented
- [x] Limitation statement recorded verbatim
- [x] Abuse scenarios tabulated
- [x] Threat model recorded (A–L, with protected/not-protected classification)
- [x] Pre-existing protections verified by test (`Phase49TamperAndAbuseResistanceTests`)
- [x] Source-available bypass limitation documented
- [x] Offline-copy limitation documented as model property, not vulnerability
- [x] Private-key custody boundary re-verified
- [x] Key rotation behavior re-verified
- [x] No anti-debugging, obfuscation or hostile-runtime mechanism introduced
- [ ] Additional-validation-point decision resolved (O-15, TO BE CONFIRMED DURING IMPLEMENTATION)
- [ ] Integrity checks implemented, if approved (none added; plan states no production code)

## Phase 4.10 — Testing strategy

- [x] Test matrix documented
- [x] Cross-cutting tests documented
- [x] Test data strategy documented
- [x] Test project identified (`tests/LabAuthServer.UnitTests/Licensing/`)
- [x] Fixture helper implemented (`LicenseTestFixture`)
- [x] Matrix implemented and passing (per-phase suites 4.2–4.9)
- [x] Cross-cutting tests implemented and passing (`Phase410TestingStrategyTests`)
- [x] Assembly-separation guarantee verified
- [x] Private-key absence and production private-parameter guard verified
- [x] Network independence verified
- [x] Deterministic-clock guarantee verified
- [x] Public-safe status/reason separation verified
- [x] Baseline test count confirmed above 733 (991 total)
- [ ] Fuzz testing of the parser (deferred)
- [ ] Property-based testing of canonicalization (deferred)
- [ ] Load testing of per-request enforcement (deferred)

## Phase 4.11 — GitHub CI integration

- [x] Test-key strategy options documented
- [x] Test-key strategy approved (O-16 resolved: ephemeral key per run, D-19)
- [x] CI reviewed — no workflow change required; the existing job already runs restore, Release build and all tests
- [x] CI green with no vendor private key (local Release run: 991 passed, 0 failed, 0 skipped)
- [x] Test count report recorded (745 unit + 246 integration = 991)
- [x] No new CI secret added
- [x] No production private key, certificate or production configuration in CI
- [x] CI permissions confirmed least-privilege (`contents: read`)
- [x] Third-party actions reviewed (all first-party: `actions/checkout@v4`, `actions/setup-dotnet@v4`, `actions/cache@v4`)

## Phase 4.12 — Operational license management

- [x] Procedures documented (operational design only; no runtime implementation)
- [x] Audit trail fields defined (no runtime audit surface exists — limitation recorded)
- [x] License lifecycle documented (issue → install → validate → replace → renew → expire → recover)
- [x] Installation procedure documented against the actual configured-path model (`LicenseValidationOptions.LicenseFilePath`, section name `Licensing`)
- [x] Replacement procedure documented (validate-before-activate, atomic replace, rollback copy)
- [x] Backup and recovery procedure documented (license file in the backup set; vendor private key is not a customer backup artifact)
- [x] Vendor/customer key-custody boundary documented
- [x] Operator diagnostics documented from `LicenseValidationStatus` (public-safe) and `LicenseValidationReason` (internal)
- [x] Runtime limitation recorded: no license-file loader, no reload, no DI registration and no logging surface exist in the server today
- [x] Machine-binding status recorded as DEFERRED (D-09); online activation/revocation recorded as DEFERRED (D-10, D-22)
- [x] Grace-period status recorded as default-disabled, diagnostic only (D-08, Phase 4.7)
- [ ] Issuance authority approved (O-17) — OPEN
- [ ] Register format decided (O-18) — OPEN
- [ ] Customer-visible vs internal document visibility approved (O-20) — OPEN
- [ ] Production private key source approved (O-07) — OPEN
- [ ] Concrete license file location decided (O-10) — OPEN (only the configurable-path model D-13 is approved)
- [ ] Support runbook drafted
- [ ] Customer instructions drafted
- [ ] Renewal reminder timeline defined
- [ ] Procedure rehearsal performed

## Phase 4.13 — Online activation (future)

- [x] Architecture documented
- [x] Capabilities listed
- [-] Implementation (deferred)
- [ ] Pursuit-or-defer decision recorded (O-19)

## Phase 4.14 — Documentation and release

- [x] Document set defined
- [x] Release checklist defined
- [ ] Document visibility approved (O-20)
- [ ] Release vehicle approved (O-21)
- [ ] Release notes drafted with the limitation statement
- [ ] Customer instructions published
- [ ] Troubleshooting entries updated

## Phase 4.15 — Final security review

- [x] Review checklist defined
- [ ] Reviewer and sign-off decided (O-22)
- [ ] Checklist executed with current evidence
- [ ] Required confirmations stated in the review record
- [ ] Open risks recorded with owners and dates
- [ ] Sign-off recorded
- [ ] Release gate released

## Change control

- [x] No commit or push without explicit authorization (standing rule)
- [x] Each implementation phase isolated in its own change set
- [x] Rollback point recorded before each phase begins
- [x] Evidence recorded in the [Change Record](Phase-4-Change-Record.md)
- [x] Production vendor private key never committed to GitHub (D-17)
- [x] Production vendor private key never provided to GitHub Actions (D-18)
- [x] Separate test signing key for automated tests (D-19)

## Approved planning decisions (2026-09-11)

- [x] Licensing model approved
- [x] Offline-first architecture approved
- [x] RSA-PSS/SHA-256/RSA-3072 approved
- [x] Key rotation requirement approved
- [x] Edition model approved
- [x] Feature licensing approved
- [x] Expiration model approved
- [x] Machine binding deferred
- [x] Online activation deferred

## Implementation steps (all Not Started)

- [ ] Implement license document
- [ ] Implement signing
- [ ] Implement validator
- [ ] Implement issuer
- [ ] Implement feature enforcement
- [ ] Implement tests