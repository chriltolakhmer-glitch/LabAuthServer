# Phase 2E — Production Environment Validation

Status: PLANNED; no environment actions performed. [Phase 2 sequence](README.md).

# Objective

Establish repeatable evidence that an approved Phase 2 release works under its actual Windows/IIS identity, certificates, DPAPI, AD and SQL dependencies. Separate code verification from live infrastructure proof.

# Current State

The API has a file-system FolderProfile.pubxml and a staged deployment procedure. The deployment script is maintained separately in the operational environment. Validation_Status explicitly does not prove current IIS, real-user login, target DPAPI or private-key accessibility.

Health currently proves application liveness only. Controller-level HTTP tests are not IIS TLS/redirect evidence. Root DSE tests can pass on safe dependency failure. SQL tests under UnitTests use a local database and a development trust exception; they do not prove production SQL trust or permissions.

# Problem / Risk

A passing test suite can coexist with invalid HTTPS binding, inaccessible private key, unreadable DPAPI secret, untrusted LDAPS/SQL certificates, wrong role mapping or missing audit records. Unsafe validation can expose credentials, lock AD accounts, stop service or delete audit history.

# Scope

A future authorized environment procedure for IIS, certificate store, DPAPI, AD/LDAP, SQL, HTTP/JWT/authorization/audit, overload and safe errors. Collect redacted outcomes with explicit sign-off.

# Non-Goals

No deployment, production change, app-pool restart, outage injection, certificate/ACL update, credential use, SQL write/purge, package creation or test execution in this planning task. This plan is not authorization for future production mutation. No destructive tests on live users/data.

# Proposed Architecture

Use an evidence-driven gate, not a new software component. Rehearse intrusive/failure/load cases in an isolated staging environment matching production topology. During a later approved production window, run only the selected low-impact cases with controlled identities. Mark unreproducible cases BLOCKED or NOT RUN, never PASS based on history.

Each future evidence record contains case ID, date/time UTC, environment alias, release commit/hash, sanitized effective setting names, operator role, preconditions, action, expected/actual result, PASS/FAIL/BLOCKED/NOT RUN, redacted evidence reference and residual risk owner. Store credentials, real hostnames/thumbprints, raw tokens and identity mappings only in approved protected systems; the plan contains placeholders.

# Implementation Steps

## Step 1

Perform pre-checks below, obtain the future environment/test-window authorization and rehearse intrusive cases in staging. Confirm 2A–2D decisions and acceptance evidence are complete.

## Step 2

Execute the case matrix in dependency order: host/TLS, certificates/DPAPI, LDAP, SQL, then application journey and failure/monitoring checks. Stop on security gate failure and record evidence without changing the environment incidentally.

## Step 3

Review results with security, application, IIS/AD and DBA owners. Apply rollback/stop criteria, verify recovery, record sign-off or unresolved blockers. Do not infer release approval from a partial pass.

## Pre-checks

- Identify immutable approved release and external configuration versions; future build/test evidence records actual counts rather than copying 173/184/188.
- Confirm Windows Hosting Bundle/runtime, IIS hosting mode/proxy topology, DNS/time synchronization, network routes and approved hosts.
- Record app-pool/runtime identity using protected aliases; verify operator access and who may restart/restore.
- Verify rollback package/config backup, restoration procedure and maintenance window; do not create a package during planning.
- Arrange controlled Reader, Operator/Administrator, unmapped and invalid-credential test scenarios without publishing account names/passwords; agree AD lockout-safe request budget.
- Approve SQL test-event tagging/correlation and later cleanup/retention ownership; no audit-table deletion as test cleanup.
- Confirm certificate roles are distinct: HTTPS/LDAPS/SQL TLS trust versus JWT signing policy. Identify active and optional previous keys.
- Resolve audit Option A/B/C/D, readiness aggregation, limits/headers and alert recipients before testing expectations.
- Disable automatic redirect following in HTTP boundary tests; prevent tooling from saving tokens/credentials in history, transcripts or reports.

## Case matrix

| ID | Procedure in later authorized window | Expected result | Evidence |
| --- | --- | --- | --- |
| IIS-01 | Inspect site, app pool, actual worker identity, hosting mode and runtime | Approved identity/runtime/configuration, least required filesystem access | Redacted inventory and version/check outcome |
| IIS-02 | Connect to approved HTTPS hostname; inspect binding, hostname, chain and TLS negotiation | Correct binding/trusted certificate and approved TLS policy | Sanitized handshake/binding result, no certificate export |
| IIS-03 | Send HTTP health and synthetic login request without following redirects | 2A approved redirect/rejection; no credential processing over HTTP | Status/location host classification and dependency spy/staging evidence |
| IIS-04 | Test allowed/unknown Host and trusted/untrusted forwarding in staging topology | Approved hosts and scheme only, no malicious redirect/loop | Sanitized status matrix |
| IIS-05 | Recycle/restart only in an approved staging/window procedure; observe startup and idle/recycle behavior | Startup succeeds, readiness recovers, identity/config remain correct | Start/recycle timestamps, redacted startup and readiness |
| CERT-01 | Resolve active signing certificate as runtime identity and exercise controlled signing | Exactly one valid RSA >=2048 cert, compatible usage/no EKU, accessible private key | Policy/access results without private key material |
| CERT-02 | Validate with public-only certificate/key in isolated fixture | Bearer verification works without private-key permissions or export | Validation pass and public-only access evidence |
| CERT-03 | Inspect configured active/previous certificate validity and store/ACL assignments | Correct store, current validity, least necessary private-key access | Redacted policy/ACL assessment |
| CERT-04 | Review chain/trust requirements separately for TLS and JWT signing certificate | TLS chains/hostnames trusted; JWT chain policy explicitly documented, not falsely claimed as current code enforcement | Trust assessment and approved JWT policy decision |
| CERT-05 | In staging use missing/expired/future/duplicate/wrong-usage/weak certificates | Signing/validation fail closed, safe logs/readiness alert | Case outcomes and redacted alerts |
| CERT-06 | Rehearse active/previous validation before/at/after overlap cutoff with controlled clock/config | Previous accepted only within approved overlap and certificate validity; active continues | Boundary results; no production clock changes |
| DPAPI-01 | Invoke existing credential path under actual deployed identity without displaying returned secret | LocalMachine-protected file readable/decryptable on intended machine, no plaintext output | Success/failure and identity alias |
| DPAPI-02 | Repeat after approved process/host restart rehearsal | Decryption remains usable; no hidden interactive-user dependency | Restart and post-restart outcome |
| DPAPI-03 | Staging-only missing/unreadable/corrupt file or wrong-machine protected content | Generic configuration failure, no anonymous bind or token | Safe failure/status/log proof |
| LDAP-01 | Verify TCP 636 and actual LDAPS bind with normal trust | LDAPv3/LDAPS, valid server hostname/trust, no callback bypass | Connectivity/TLS/bind classifications |
| LDAP-02 | Controlled correct/incorrect user credential login with service lookup | Correct user succeeds; invalid credentials generic 401; no credential logging | Correlation and redacted status |
| LDAP-03 | Query known membership and role scenarios | Existing direct group mapping/precedence, exactly one issued role | Protected expected-role comparison, no raw LDAP dump |
| LDAP-04 | Test valid user with no groups/unmapped groups and simulated failed/partial/multiple lookup | Approved 2B no-role/error contract; no token in every denied/failed case | Status/category and no-signing assertion |
| LDAP-05 | Staging service-account invalid/locked, timeout, cancellation, unavailable/trust failure | Dependency distinct from user failure; bounded cleanup; no fallback | Stage/outcome, duration/handle recovery |
| SQL-01 | Connect as app identity; DBA verifies session encryption and trusted server identity | Encrypt enabled; production TrustServerCertificate bypass absent; trusted hostname/chain | Redacted client setting check plus server session encryption result |
| SQL-02 | Inspect effective grants and exercise approved writer procedure | Execute-only audit boundary; no direct write/delete/admin/maintenance access | Permission assessment; destructive statements not executed in production |
| SQL-03 | Write approved tagged audit event and query it with separate read-authorized identity | Returned ID and exact approved event/correlation/time/status/role | Redacted row comparison/count, no secret payload |
| SQL-04 | Staging SQL refused/slow/untrusted/permission failure | Approved 2C budget and outcome, independent alert, primary response preserved under A | Duration, failure category, response and alert |
| SQL-05 | If retry selected, simulate lost acknowledgment after commit and replay | One logical event by approved idempotency design | Event-ID/count evidence; otherwise NOT APPLICABLE with option rationale |
| SQL-06 | Rehearse approved retention dry run, archive verification, interrupted purge and restoration in staging | Holds respected, counts reconcile, application lacks maintenance grants | DBA rehearsal/restore evidence; no production purge here |
| APP-01 | GET /api/v1/health anonymously | 200 healthy shape and canonical correlation; no dependency claim | Sanitized response metadata |
| APP-02 | Access approved readiness probe normally, with stale/failing cached probes in staging | 2D 200/503 contract; SQL degraded under A; details restricted | Aggregate status/freshness and access check |
| APP-03 | Controlled Reader login -> JWT verification -> protected GET | Token Bearer/expiresAt, correct signature/issuer/audience/kid/claims and Reader 200 | Validation assertions only, never raw token |
| APP-04 | Anonymous, invalid/tampered/expired/wrong issuer/audience/kid/role token and Operator/Administrator on Reader route | 401 for auth failure, 403 for authenticated non-Reader; no implied role hierarchy | Status/approved audit classification |
| APP-05 | Correlate login, granted/denied, invalid-token and safe-error events | Expected vocabulary/status/outcome, shared request correlation without assuming one event/request | Redacted event reconciliation |
| APP-06 | Exercise Login fixed-window boundary in staging or approved controlled window | 10/minute/no queue, next request 429; health/protected outside Login limiter | Counts/window and bounded rejection observations |
| APP-07 | Exercise 2A size/header/host/security-header/HSTS matrix on actual IIS | Valid clients pass; oversize rejected before business work; correct host/app headers | Limit boundary and IIS status/substatus evidence |
| APP-08 | Staging overload, cancellation and unhandled exception; inspect responses/logs | Bounded work, safe ProblemDetails when writable, no secrets/stack traces, recovery | Resource/time trace and sanitized logs |
| OPS-01 | Trigger approved test alerts for LDAP/SQL/audit/key expiry/staleness and restore | Correct owner receives alert/recovery; monitoring itself stays bounded | Delivery timestamps and runbook acknowledgment |

CERT-04 does not automatically introduce chain building/revocation checks into JWT validation. Current signing policy is certificate selection/validity/RSA/usage; any additional policy requires a separate approved decision. Previous-key expiry is the earlier effective boundary from configured overlap and certificate validity, not permission to extend token lifetime.

# Source Areas Expected to Change

No application source change is expected for executing this validation stage. Read src/LabAuthServer.Api/Program.cs, Controllers/AuthController.cs, Controllers/ProtectedController.cs, Extensions/JwtBearerAuthenticationOptions.cs, Infrastructure/Security/CertificateSigningKeyProvider.cs, Infrastructure/ActiveDirectory/DpapiLdapServiceAccountCredentialProvider.cs, Infrastructure/Services/LdapAuthenticationClient.cs and Infrastructure/Auditing/SqlAuditEventService.cs (Infrastructure paths are under src/LabAuthServer.Infrastructure/). Read database/Phase11 and Api/Properties/PublishProfiles/FolderProfile.pubxml for intended boundaries.

Future evidence may be added as sanitized Markdown under docs/plans/Phase-2/ with separate authorized scope; protected operational evidence remains outside public repository. Defects found return to the owning 2A–2D plan and a separate implementation change.

# Configuration Changes

None in this task or incidental to validation. Inspect effective production values, not just checked-in defaults. Any correction to hosts, TLS, store references, DPAPI path, timeout, SQL trust or probe settings requires the later environment change process.

# Database Changes

None. Future approved SQL-03 creates a test audit event through the existing procedure, not schema/table maintenance. SQL-05/06 apply only to selected implemented options and isolated rehearsals.

# Testing Strategy

## Unit Tests

Consume 2A–2D unit evidence; unit tests cannot prove deployed identity/trust.

## Integration Tests

Consume future full-suite results with environment-dependent tests identified. Execute live matrix cases only with protected test inputs and authorization.

## Security Tests

Prioritize no-token-on-failure, TLS trust/hostnames, certificate/public-key separation, least privilege, token tampering, size boundaries, no fallback and redaction. Rehearse intrusive negative cases in staging.

## Regression Tests

Verify route/status/token shape, exact-role policy, correlation, SQL event contract, 10/minute Login policy and previous-key overlap after any authorized restart/deployment.

## Manual Validation

IIS/AD/DBA/security owners execute their matrix cases and review evidence. Lack of access or credential availability yields BLOCKED, not inferred success.

# Security Considerations

Use controlled accounts without increasing lockout risk. Do not save raw JWTs, credentials, DPAPI data, SQL connection strings or private-key artifacts. Evidence should prove assertions through redacted classifications and IDs. Do not weaken trust to make a failing check pass.

# Operational Considerations

Budget test traffic, specify observer/on-call contacts in protected runbooks and record monitoring baselines. Use the existing Safe_Deployment_Procedure for any future deployment; the planning repository is not proof that its external script is available.

# Compatibility Risks

IIS differs from WebApplicationFactory; host/proxy filtering may alter error codes and bypass middleware. Actual certificate policies may differ between TLS and JWT. Production aliases, identities and AD group size can expose assumptions not represented in fixtures.

# Rollback Plan

Stop validation and invoke the approved rollback decision on token issuance after dependency failure, insecure transport/trust bypass, key/DPAPI exposure, incorrect authorization, unacceptable audit loss, persistent startup/readiness failure or load beyond approved budgets.

If a later release was deployed, use the staged procedure to restore the last validated package and matching external configuration. Preserve logs/audit evidence and database history. A failed replacement/parity check follows the existing procedure's restore-and-leave-pool-stopped rule. Do not change AD/SQL/certificates opportunistically. Recheck health, readiness, anonymous 401 and controlled login after authorized recovery. HSTS and purged data require their specific non-instant rollback procedures.

# Acceptance Criteria

Every applicable matrix case has actual evidence and status. Mandatory security cases pass; unresolved live identity, DPAPI, trust, signing, authorization or audit-policy checks block production sign-off. Optional unselected retry/outbox cases are marked not applicable with rationale. No current production proof is claimed from automated or archived records.

# Definition of Done

Security owner signs security controls/residual risks; IIS/AD/DBA owners sign their environment checks; application owner signs release/test parity; operations owner signs alerts and rollback readiness. Approval dates and protected evidence references are recorded. Any blocked mandatory case means Phase 2 production validation is incomplete.

# Open Decisions

DECISION REQUIRED: target environment/window, operators, test identities, protected evidence location, specific mandatory sign-off set, accepted residual risks and how intrusive staging results map to production. Resolve 2A–2D decisions before defining final expected values.

# Dependencies

Final gate after 2A–2D. Inventory and matrix drafting can proceed in parallel, but live acceptance depends on delivered controls and their approved contracts.
