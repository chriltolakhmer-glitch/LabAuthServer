# Project Status

## Current update — Phase 6.8 release authorization gate (2026-09-13)

**DECISION GATE PREPARED AND LOCALLY VALIDATED; NO RELEASE AUTHORIZED.** Phase 6.8 consolidates completed evidence from Phases 6.1 and 6.3 through 6.7, the final release checklist, remaining owner decisions, and a blank go/no-go authorization form. Build/test evidence is repository-only; environment, operational, security, legal/business, delivery, custody, and rollback gates remain open where evidence or owners are missing.

No release, tag, artifact publication, deployment, customer license issuance, or production environment change occurred. See [Phase 6.8 Release Authorization Gate](plans/Phase-6/Phase-6.8-Release-Authorization-Gate.md). Phase 6.8 does not authorize release activity.

## Previous update — Phase 6.7 first-release prerequisite readiness (2026-09-13)

**READINESS ASSESSMENT PREPARED AND LOCALLY VALIDATED; NO RELEASE AUTHORIZED.** Phase 6.7 now separates completed repository prerequisites, approved release-governance policies, remaining owner decisions, operational blockers, legal/business blockers, and deferred capabilities. The repository is not ready to request first-release authorization: target-environment evidence, operational ownership, exact provider/custody selections, professional review, signing decisions, and release rehearsal remain open.

No release, tag, artifact publication, deployment, customer license issuance, certificate operation, production environment change, repository-setting change, or Phase 5.6 modification occurred. See [Phase 6.7 First-Release Prerequisite Readiness](plans/Phase-6/Phase-6.7-First-Release-Prerequisite-Readiness.md). Phase 6.7 completion does not authorize release activity.

## Previous update — Phase 6.6 target-environment acceptance design (2026-09-13)

**IMPLEMENTED DESIGN AND LOCALLY VALIDATED.** Phase 6.6 now defines the evidence gate for Windows/IIS hosting, SQL Server, Active Directory/LDAP, TLS/certificates, configuration ownership, and operational handoff. It records current implementation assumptions and requires target evidence before any environment may be called accepted. Target environment owner, SQL operational owner, AD owner, certificate owner, capacity thresholds, and related operational decisions remain **TBD**; no names or thresholds were invented.

No production deployment, IIS installation change, AD/domain change, SQL production change, certificate operation, release creation, customer onboarding, runtime behavior change, database migration, or Phase 5.6 modification occurred. See [Phase 6.6 Target-Environment Acceptance](plans/Phase-6/Phase-6.6-Target-Environment-Acceptance.md). Phase 6.7 remains outside this implementation.

## Previous update — Phase 6.5 operational acceptance design (2026-09-13)

**IMPLEMENTED DESIGN AND LOCALLY VALIDATED.** Phase 6.5 now records the observed audit durability, SQL outage, retention, monitoring, and liveness behavior plus the evidence contract for acceptance. P6-D7 through P6-D11 remain intentionally deferred pending owner, operations, DBA, architecture, legal/compliance, and target-environment input as applicable. No operational owners, retention periods, alert thresholds, readiness checks, retries, queues, or monitoring integrations were invented or implemented.

The current application remains best-effort for SQL audit persistence: failures are logged and do not replace the primary response, but no replay guarantee exists. `/api/v1/health` remains dependency-independent liveness. Retention/purge and production monitoring are not implemented. See [Phase 6.5 Audit and Operational Acceptance Design](plans/Phase-6/Phase-6.5-Audit-and-Operational-Acceptance-Design.md) for decisions, placeholders, and evidence requirements.

Phase 6.6 environment acceptance and Phase 6.7 first-release readiness remain outside this implementation. No release, deployment, monitoring rollout, database migration, customer operation, or Phase 5.6 change occurred.

## Previous update — Phase 6.4 release qualification (2026-09-13)

**IMPLEMENTED AND LOCALLY VALIDATED.** [Phase 6 — Operational Assurance and First-Release Readiness](plans/Phase-6/Phase-6-Plan.md), [Phase 6.1 implementation/evidence](plans/Phase-6/Phase-6.1-Safe-Automated-Validation-Boundaries.md), [Phase 6.2 authorization/audit boundaries](plans/Phase-6/Phase-6.2-Authorization-and-Audit-Boundary-Corrections.md), [Phase 6.3 licensing boundaries](plans/Phase-6/Phase-6.3-Licensing-Boundary-Assurance.md), and [Phase 6.4 release qualification](plans/Phase-6/Phase-6.4-Release-Build-and-Documentation-Reconciliation.md) are recorded in the current workstream. Phase 6.5–6.7 remain outside this implementation. The Release solution explicitly covers `LabAuthServer.LicenseIssuer`; the API publish profile remains a separate local packaging path. The governance-only owner decision packet is prepared in [Phase 6 — Owner and Architecture Decision Packet](plans/Phase-6/Phase-6-Owner-Decision-Packet.md); no release is authorized.

Release build: zero warnings/errors. Infrastructure-safe default tests: **1,038 passed (783 unit-project, 255 integration-project), zero failures/skips**, with SQL connection and infrastructure enable flags absent; three infrastructure cases excluded. Explicit disposable SQL validation: **2 passed**, zero failures/skips. Separate real LDAP acceptance: **1 case, not run**. Total inventory: 1,041; hosted-equivalent mandatory total: 1,040. The prior full baseline was 1,023 tests, confirmed by CI #39 on `0b6d1ee92220090fb5570323e30f10bfc2dd9ed5`.

No localhost SQL fallback exists. Ordinary API fixtures use production-validated in-memory audit recording and block operational credentials/connections/key access. Real SQL and LDAP tests require explicit opt-in and target configuration. CI runs the default set before provisioning disposable SQL, then requires both real SQL tests to execute and pass. See [Testing](Testing.md) for exact commands and [Validation Status](Validation_Status.md) for evidence boundaries.

Phase 5 owner-value/governance decisions remain complete; Phase 5.6 remains platform/ownership-model blocked. Professional review remains external/pending and no release is authorized. Phase 6.2 authorization/audit boundaries and Phase 6.3 licensing boundaries are implemented and validated; Phase 6.4 issuer Release configuration and Phase 6.5–6.7 work remain later work.

The earlier dated Phase 2/3 records below are retained as historical checkpoints, including their original counts and scope. This update supersedes their automated-test/current-workstream statements; it does not establish new live-environment acceptance or perform the broader Phase 6.4 documentation reconciliation.

## Latest required slice: hard pending-waiter cap

2026-09-10: **IMPLEMENTED; PHASE 2A APPLICATION CODING = CLOSED.** The explicitly approved hard pending LDAP waiter cap defaults to 16, validates 1-128 and returns typed ResourceExhausted/503 before semaphore waiting when full. Existing active concurrency and cancellation/deadline contracts remain intact. Full suite: 700 passed, zero failures/skips; Release zero warnings/errors. [Final implementation, file inventory and closure](plans/Phase-2/Phase-2A-Hard-Pending-Waiter-Cap.md). No deployment, infrastructure, restart, commit or push occurred.

**Last reviewed:** 2026-09-10
**Status:** Current implementation summary

## Current coding-only scope

Application coding, automated unit/integration tests and directly relevant source documentation remain in scope. Proxy/forwarding infrastructure validation, HSTS deployment, real IIS Security Slice 2 acceptance, HTTP.sys tuning, network/WAF/load-balancer validation, infrastructure capacity testing and operational release review are **DEFERRED**, with reasons in [Coding-only scope](plans/Phase-2/Coding-Only-Scope.md). These are not blockers to coding-only completion. HSTS remains disabled; forwarding trust is unchanged. Do not implement application workarounds for deferred infrastructure controls.

## Implementation state

LabAuthServer is a .NET 10 ASP.NET Core API using the approved four-layer dependency direction:

`Domain <- Application <- Infrastructure <- Api`

Implemented production code includes:

- LDAPv3 authentication over LDAPS on TCP 636 with configured-domain UPN validation.
- Windows DPAPI-backed service-account credential loading for directory queries.
- LDAP filter escaping and safe typed authentication failures.
- JWT issuance and bearer validation with RSA signing, certificate-store key resolution, `kid`, previous-key overlap, bounded claims, and required identity/time claims.
- Phase 2A size boundaries: fixed RSA 2048–4096-bit policy, 12288-byte encoded JWT ceiling and 12352-byte Authorization-value ceiling before authentication; reduced 7680-byte issuance payload, unchanged 4096 claim limit and field semantics. The JWT transport-budget reduction is deployed and production-validated. Login bodies are limited to 8192 bytes and the decoded general request envelope to 16128 bytes before authentication.
- AD group-to-role mapping with Reader, Operator, and Administrator roles and deterministic precedence.
- Default authenticated-user authorization plus named role policies.
- Public health and login endpoints plus a Reader-protected resource.
- Correlation middleware, authorization audit middleware, and global safe exception handling.
- SQL Server audit persistence through `Audit.usp_WriteAuditEvent`.

## API surface

- `GET /api/v1/health` — anonymous application liveness response.
- `POST /api/v1/auth/login` — anonymous HTTPS-only authentication and token issuance.
- `GET /api/v1/protected` — Reader-policy protected resource.

## Verification status

- **Build:** VERIFIED; current Release build completed with zero errors and zero warnings.
- **Automated tests:** VERIFIED; current full solution run completed with 729 passed (487 unit, 242 integration), 0 failed, and 0 skipped; implementation baseline was 188 passing tests at `6793324`.
- **Security boundaries:** VERIFIED by automated tests for configuration, token, authorization, LDAP-input, audit, correlation, and safe-error behavior.
- **Live AD identity:** NOT VERIFIED from independently reproducible repository evidence.
- **Existing local IIS application:** The 2026-09-08 JWT transport-budget reduction was deployed and production-validated. The 8192-byte login-body and 16128-byte general-header policies are also deployed and accepted through HTTP/1.1 and HTTP/2. Security Slice 2 host/response-header controls are deployed and live-verified as of 2026-09-10.
- **JWT/header transport through IIS/proxies:** JWT/Authorization and login-body/aggregate-header limits are COMPLETED on direct IIS HTTP/1.1 and HTTP/2. The HTTP/2 19-byte discrepancy was resolved as test methodology (IIS Connection: close), with no production fix. Uninspected proxy paths remain unverified.
- **Local SQL TLS remediation:** VERIFIED on the inspected host with a trusted CA-issued certificate, matching DNS names, encrypted TCP and `TrustServerCertificate=False`; application audit persistence and the IIS identity's encrypted SQL session were confirmed. Existing application HTTPS health passes. See Validation Status for the limited environment evidence.
- **JWT signing private-key and DPAPI behavior:** NOT VERIFIED against a target environment; the local SQL TLS private-key access check is recorded separately.
- **Audit retention/purge:** NOT IMPLEMENTED.

The authoritative details and evidence boundary are documented in [Validation_Status.md](Validation_Status.md).

## Current documents

- [Architecture](Architecture.md)
- [Authentication](Authentication.md)
- [Authorization](Authorization.md)
- [JWT](JWT.md)
- [Audit logging](AuditLogging.md)
- [Database](Database.md)
- [Configuration](Configuration.md)
- [Deployment](Deployment.md)
- [Operations](Operations.md)
- [Security](Security.md)
- [Testing](Testing.md)
- [Validation status](Validation_Status.md)
- [Troubleshooting](Troubleshooting.md)

Historical phase records are preserved in [archive](archive/README.md) and are not current implementation guidance.

## Approved transport budget reduction
Current deployment status: Option A and the 8192-byte login-body / 16128-byte general-header policies are deployed and validated. Security Slice 2 is deployed and live-verified as of 2026-09-10.

The original measurement wording below is retained for traceability; the current deployment status above supersedes its rollout wording.

Option A is implemented with no host, TLS, key, AllowedHosts, HSTS or LDAP changes. The actual deployed issuer produced a maximum 10664-byte token from 7680 payload bytes with its active key; the full supported key/header upper bound is 11997 bytes. The new 12288 encoded and 12352 Authorization budgets passed direct-route reachability probes. Configuration must be merged with preserved live settings at a separately authorized deployment. See [the measured decision](plans/Phase-2/JWT-Transport-Budget-Reduction.md). No commit or push was performed.

## Security Slice 2 checkpoint (2026-09-10)

Explicit source AllowedHosts (`DC01.lab.local`) and centralized `/api` no-store, nosniff and DENY framing headers are implemented and tested, including fresh health responses and early application errors. HSTS and forwarded-header/proxy trust configuration remain unchanged and disabled/unconfigured. This slice is now DEPLOYED and live-verified: an anonymous `GET /api/v1/protected` returns `401` with `WWW-Authenticate: Bearer`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and `Cache-Control: no-store`; the `200` health response and the `307` HTTP→HTTPS redirect carry the same headers. The prior JWT, Authorization, login-body and aggregate-header controls are COMPLETED AND DEPLOYED. See [Security Slice 2 decisions and acceptance boundary](plans/Phase-2/Phase-2A-Security-Slice-2.md).

Deployment note (2026-09-10): the deployed binary had been built before `ApiResponseHeadersMiddleware.cs` existed, so the header policy was absent from all live responses. A fresh Release publish was deployed via the safe deployment procedure (backup taken; server-specific `appsettings.json` preserved; app pool stopped/started). Validation: 487 unit + 242 integration tests passed, 0 failed, 0 skipped. Backup retained at `C:\Apps\LabAuthServer\Backups\current-20260910-222656`. No commit or push was performed.

## LDAP classification checkpoint (2026-09-10)

IMPLEMENTED AND SOURCE-TESTED: immutable authentication/group results; operation-stage and bounded-reason failures; service/user bind distinction; LDAP exception/result-code mapping; complete identity/group validation; fixed client errors; deterministic existing-event audit mapping. Failed group lookup cannot reach mapping/signing. Successful empty membership and the current no-role 500 policy remain separate.

Full suite: 391 passed (260 unit, 131 integration), no failures/skips. Release: zero warnings/errors. No deployment or infrastructure change. Deadlines, cancellation redesign, concurrency and audit resource/persistence work remain separate coding tasks. [Implementation and evidence](plans/Phase-2/Phase-2A-LDAP-Failure-Classification.md).

## LDAP cancellation/deadline design gate (2026-09-10)

RESOLVED BY APPROVED COOPERATIVE CONTRACT; IMPLEMENTED AND SOURCE-TESTED. One validated 30-second default decision budget spans identity, membership, mapping and token issuance. Recorded caller cancellation/deadline origin and active stage determine safe 499/504 outcomes; late success is rejected and cleanup remains awaited. Existing synchronous native work can finish after the deadline. Native hard cancellation remains deferred. Full suite: 582 passed (399 unit, 183 integration), zero failures/skips; Release: zero warnings/errors. Source settings gained AuthenticationTimeout (1-60 seconds); live settings are untouched. See [LDAP cancellation and deadlines](plans/Phase-2/Phase-2A-LDAP-Cancellation-Deadlines.md). No deployment, infrastructure, commit or push change.

## LDAP concurrency/resource checkpoint (2026-09-10)

IMPLEMENTED AND SOURCE-TESTED: one DI singleton gate protects the public login's complete identity/group LDAP phase, including awaited cleanup. Default 4 concurrent phases; source setting MaxConcurrentLdapOperations validates 1-32. Waits consume the existing authentication budget; caller/deadline origin and late-result rejection are unchanged. Permits remain held for native work after interruption and are returned before mapping/signing/audit. Current suite: 646 passed (424 unit, 222 integration), zero failures/skips; Release: zero warnings/errors. See [scope, waiting-resource limitations and evidence](plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md). Current public ingress rate limiting is retained; a hard pending-waiter cap/overflow policy and new LDAP entry-point admission require separate work. No deployment, infrastructure, commit or push changes.

## Final Phase 2A application-code review (2026-09-10)

COMPLETE; READY TO CLOSE application coding after four targeted fixes: previous-key retirement no longer disables the active key, token configuration rejects unsupported algorithms/subsecond lifetimes, oversized audit identities are explicitly omitted without dropping the event, and empty client correlation GUIDs are replaced. Final suite: 665 passed (436 unit, 229 integration), 0 failures/skips; 19 new regression cases. Targeted LDAP/cooperative: 389; concurrency: 64; changed-behavior/configuration: 30. Release: 0 warnings/errors. No existing tests weakened. See [findings, exact inventory and evidence](plans/Phase-2/Phase-2A-Final-Application-Code-Review.md). All infrastructure/release work and the documented hard-waiter-cap decision remain deferred. No deployment, live configuration, commit or push change.
