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
- [ ] Canonicalization scheme chosen (O-03)
- [ ] Signature envelope shape chosen (O-04)
- [ ] Worked canonical example with exact bytes recorded

## Phase 4.3 — Cryptographic signing and verification

- [x] Algorithm trade-offs documented
- [x] Rotation strategy documented
- [ ] First algorithm confirmed (O-05)
- [ ] Minimum key size confirmed (O-06)
- [ ] Library availability verified on the target runtime
- [ ] Signing implemented in the issuer
- [ ] Verification implemented in the server
- [ ] Round-trip and negative cryptographic tests added

## Phase 4.4 — License issuer

- [x] Issuer responsibilities and prohibitions documented
- [ ] Private key source decided (O-07)
- [ ] Issuer project created
- [ ] Input validation implemented
- [ ] Signing implemented
- [ ] Non-sensitive audit logging implemented
- [ ] Issuer tests using an ephemeral key added
- [ ] Confirmed the server cannot invoke the issuer

## Phase 4.5 — License validation

- [x] Validation rule table documented
- [x] Typed result model documented
- [x] Safe error mapping documented
- [ ] Validation cadence decided (O-09)
- [ ] License file location decided (O-10)
- [ ] Validator implemented in the documented order
- [ ] Public-safe category mapping implemented
- [ ] Rule-by-rule tests added

## Phase 4.6 — Feature and edition enforcement

- [x] Feature identifier scheme documented
- [x] Edition hierarchy documented
- [x] Default-deny documented
- [ ] Edition and feature mapping approved (O-01)
- [ ] Enforcement timing approved
- [ ] Denied-feature API response approved
- [ ] Policy service implemented
- [ ] Enforcement added to licensed features only
- [ ] Security-independence test added

## Phase 4.7 — Expiration and grace period

- [x] Timeline semantics documented
- [x] Clock handling rules documented
- [ ] Grace length and carrier decided (O-11)
- [ ] Post-grace behavior decided (O-12)
- [ ] Clock-skew allowance decided (O-13)
- [ ] Clock abstraction implemented
- [ ] Expiry and grace implemented
- [ ] Boundary tests added

## Phase 4.8 — Machine binding

- [x] Options analyzed
- [x] False-positive scenarios documented
- [x] Recommendation recorded
- [-] Binding implementation (deferred)
- [-] Rebinding tooling (deferred)

## Phase 4.9 — Tamper and abuse resistance

- [x] Realistic protections documented
- [x] Limitation statement recorded verbatim
- [x] Abuse scenarios tabulated
- [ ] Additional-validation-point decision resolved (O-15)
- [ ] Integrity checks implemented, if approved

## Phase 4.10 — Testing strategy

- [x] Test matrix documented
- [x] Cross-cutting tests documented
- [x] Test data strategy documented
- [ ] Test project identified
- [ ] Fixture helper implemented
- [ ] Matrix implemented and passing
- [ ] Cross-cutting tests implemented and passing
- [ ] Baseline test count confirmed at or above 733

## Phase 4.11 — GitHub CI integration

- [x] Test-key strategy options documented
- [ ] Test-key strategy approved (O-16)
- [ ] CI change authorized and applied
- [ ] CI green with no vendor private key
- [ ] Test count report recorded
- [ ] No new CI secret added

## Phase 4.12 — Operational license management

- [x] Procedures documented
- [x] Audit trail fields defined
- [ ] Issuance authority approved (O-17)
- [ ] Register format decided (O-18)
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