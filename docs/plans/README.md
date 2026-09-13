# LabAuthServer implementation roadmap

## Current Phase 6 release workflow (2026-09-13)

Phase 6 now uses [ALOT's solo developer release checklist](Phase-6/Release-Checklist.md). Its six checks replace the historical multi-owner release gates. The planning baseline and older phase descriptions below retain their original context; current implementation and validation are maintained in Project Status and Validation Status.

Planning baseline: 2026-09-06, commit `6793324` on `main`. The initial working tree was clean and the local tracking reference showed `main` aligned with `origin/main`; no remote fetch was performed.

These documents are planning deliverables, not authorization to implement or deploy. Only Markdown under `docs/plans/` is changed by this task. No application, test, configuration, database, deployment, archive, image, or generated build artifact belongs in this change.

| Phase | Roadmap | Status | Entry point |
| --- | --- | --- | --- |
| 1 | Security Remediation | COMPLETE | Baseline commit above; [Security](../Security.md) |
| 2 | Production Hardening & Operational Resilience | PLANNED | [Phase 2](Phase-2/README.md) |
| 3 | Advanced Authentication & Token Lifecycle | FUTURE | [Phase 3](Phase-3/README.md) |
| 4 | Identity Federation & Standards | FUTURE | [Phase 4](Phase-4/README.md) |
| 5 | Enterprise Operations & Scalability | FUTURE | [Phase 5](Phase-5/README.md) |
| 6 | Solo Developer Releases | ACTIVE — ALOT | [Phase 6](Phase-6/README.md) |

COMPLETE means delivered at the recorded baseline, not independently certified in production. PLANNED means a proposed implementation sequence needing its stated decisions resolved. FUTURE means candidate scope without implementation approval. OPTIONAL identifies a candidate that may never be selected; it is not a required dependency.

## Technical License Enforcement plan set

A separate, self-contained Phase 4 plan set describes a **Technical License Enforcement** capability (signed offline license document, vendor-side issuer, server-side validator, feature/edition/limit enforcement, expiration, tamper resistance, CI integration, operational management, future online activation). It is independent of, and does not override, the Phase 4 candidate scope above. Entry point: [Phase-4 Technical License Enforcement](Phase-4/Phase-4-README.md).

Current status of that plan set: Phases 4.0–4.13 are documented; implementation phases 4.1–4.13 are complete as recorded in the [Change Record](Phase-4/Phase-4-Change-Record.md). The server currently has no runtime license-file loader; the licensing services are exercised by the validator boundary and the test suites. No online activation, no revocation, no machine binding and no grace entitlement are implemented.

## Evidence and authority

Read current [Architecture](../Architecture.md), [Security](../Security.md), [Validation Status](../Validation_Status.md), [Project Status](../Project_Status.md), repository [AGENTS.md](../../AGENTS.md), and [Copilot instructions](../../.github/copilot-instructions.md) before future implementation. The instruction references to coding standards and development planning resolve to [the internal standard](../internal/Coding_Standard_and_SOP.md) and [internal development plan](../internal/Development_Plan.md); their older proposed milestones do not override current source.

Inspection also covered README, Authentication, Authorization, JWT, Configuration, Deployment, Operations, Testing, Database, and the safe deployment procedure, plus the HTTP, LDAP, certificate, audit, configuration, database, and test boundaries. All source paths in detailed plans are repository-relative; proposed new files are explicitly identified. Archived numbered implementation phases are historical and unrelated to this roadmap's Phase 1–6 numbering.

Known evidence discrepancies are intentionally preserved rather than silently rewritten:

- The supplied Phase 1 completion record reports 188 passing tests. Testing and Validation_Status record 184; Project_Status records 173. No build or test was run for this planning-only task, so none is a newly reproduced count.
- Copilot instructions still list rate limiting outside implemented scope. Program.cs and AuthController.cs implement the Login policy, consistent with Security and Configuration.
- Architecture's pipeline summary omits explicit routing/rate-limiter middleware now present in Program.cs.
- Documentation describes a user bind followed by groups. The actual authentication client first binds the service account, searches the user, then binds the submitted UPN; the separate group query also uses the service account.
- JWT issuance has configured claim/token limits; bearer configuration does not explicitly apply those configured size limits before parsing. Existing validation summaries must not be read as proof of an inbound HTTP size boundary.
- Live AD login, IIS, DPAPI and private-key access remain environment-unverified. Signing-certificate checks do not establish a chain/revocation validation policy.
- Some tests in the UnitTests project use real SQL. Root DSE integration tests permit safe dependency failure; a passing suite is not proof of live directory availability.

## Delivery model

Deliver Phase 2A, 2B, 2C, 2D, then 2E as separate reviewable increments. Logical subchanges may use separate future commits; this task creates no commits. Security headers/limits, LDAP result contracts/execution, audit monitoring/retention, and readiness/alerts should not become one giant change.

Future implementers must resolve each DECISION REQUIRED with the project architect and relevant operator, record the chosen value and evidence in the affected plan, implement only approved scope, and rerun applicable validation. New packages, authentication/database architecture changes, and dependency changes retain existing governance requirements.

Phase 2 completion is the entry gate for considering Phase 3. Phases 3–6 are a roadmap, not a mandate to implement them sequentially: later operational needs can be reprioritized with explicit approval. No MFA provider, federation provider, distributed store, HSM, or observability vendor is selected.
