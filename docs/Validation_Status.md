# Validation Status

## Latest hard-cap acceptance status

2026-09-10: **IMPLEMENTED AND SOURCE-VALIDATED; APPLICATION CODING CLOSED.** Full suite: 700 passed (465 unit, 235 integration); 35 new waiter-cap tests, 64 existing concurrency tests, 424 targeted LDAP/cooperative tests. Zero failures/skips; Release zero warnings/errors. Atomic admission, exact classification, bounded audit, no downstream work, cancellation/deadline cleanup, capacity recovery and combined ingress/active/pending behavior pass deterministic tests. [Evidence and closure](plans/Phase-2/Phase-2A-Hard-Pending-Waiter-Cap.md). This does not establish production capacity or IIS/operational acceptance.

**Status:** Current repository validation summary
**Last reviewed:** 2026-09-10

## Coding-only acceptance boundary

The [approved coding-only scope](plans/Phase-2/Coding-Only-Scope.md) marks infrastructure and release validation **DEFERRED** with explicit reasons. Security Slice 2 source implementation and automated validation are complete; real IIS acceptance and deployment are deferred, not requirements for this coding workstream. HSTS remains disabled, forwarding trust unchanged, and no HTTP.sys tuning is required. Earlier references to pending deployment describe a separate future workstream, not an instruction to perform it now.

This document is the authoritative summary of what can be established from the repository and its recorded validation evidence. Historical phase records are retained under `docs/archive/` and are not treated as current proof unless explicitly identified below.

## Current implementation status

| Area | Status | Evidence boundary |
| --- | --- | --- |
| ASP.NET Core API and four-layer solution | IMPLEMENTED | Current solution and project files |
| LDAPS authentication | IMPLEMENTED | Infrastructure code and automated boundary tests |
| JWT issuance and validation | IMPLEMENTED | Token services, bearer configuration, and tests |
| Phase 2A JWT/Authorization size boundaries and RSA maximum | IMPLEMENTED AND DEPLOYED | Fixed policies, early middleware, provider and pipeline tests; direct IIS HTTP/1.1 and HTTP/2 deployment validation completed |
| Phase 2A login body and general decoded-header budgets | COMPLETED AND DEPLOYED | 8192-byte body and 16128-byte managed envelope; direct IIS HTTP/1.1 and HTTP/2 accepted; accounting discrepancy resolved as test methodology |
| AD group-to-role mapping | IMPLEMENTED | Mapping service, configuration validation, and tests |
| Policy authorization | IMPLEMENTED | Named policies and protected controller |
| SQL audit persistence | IMPLEMENTED | SQL scripts, repository, middleware, and tests |
| Correlation and safe exception handling | IMPLEMENTED | API middleware and tests |
| IIS publishing/deployment workflow | IMPLEMENTED | Publish profile and deployment documentation |

## Build status

**VERIFIED:** The Release solution build completed successfully with zero errors and zero warnings during the final Phase 2A application-code review.

## Automated test status

**VERIFIED:** The final hard pending-waiter cap full solution test run completed with 700 passed (465 unit, 235 integration), 0 failed, and 0 skipped tests, against an independently rerun baseline of 188 passing tests at `6793324`. Test results do not prove access to a production directory, certificate private key, DPAPI secret, or deployed IIS environment.

## Authentication validation

**VERIFIED:** Automated tests cover configured-domain UPN validation, typed authentication failure mapping, HTTPS enforcement, cancellation, LDAP filter escaping, and safe failure behavior.

**NOT VERIFIED:** A real user credential is not stored in the repository, and no current repository evidence independently proves a live real-identity login.

## LDAP/LDAPS validation

**IMPLEMENTED:** The runtime requires LDAPv3 over LDAPS on TCP port 636, validates the configured domain, and uses the Windows DPAPI-backed service-account provider for directory queries.

**VERIFIED:** Boundary and failure behavior are covered by automated tests. Live directory behavior remains environment-dependent.

## JWT validation

**VERIFIED:** Tests cover RSA private-key signing, public-key-only validation, the configured algorithm, issuer, audience, lifetime, `kid`, previous-key overlap, required claims, role validation, tampering, and size limits. Certificate tests cover validity windows, RSA key size, digital-signature usage, duplicate matches, and validation without a private key. The current claim set includes `iss`, `aud`, `sub`, `jti`, `iat`, `nbf`, `exp`, `role`, and `scope`.

**VERIFIED:** The certificate policy requires a currently valid RSA certificate within the fixed inclusive 2048–4096-bit range. Active/previous signing and public validation paths in both providers reject 1024/4160 and accept 2048/3072/4096-bit synthetic keys. Signing requires a private key; validation uses only the public key. Incompatible KeyUsage is rejected, certificates containing an EKU extension are rejected under the current general-purpose signing policy, and multiple currently valid matching certificates fail closed.

**VERIFIED:** `MaximumTokenSize` is reduced to 7680 in source and deployed. The independent 4096 claim limit and existing UTF-16 identity/UTF-8 role/scope semantics are preserved. The fixed encoded ceiling is 12288 bytes and the Authorization-header value ceiling is 12352 bytes. Tests and the 2026-09-08 production deployment prove accepted maximum-envelope issuance, exact valid transport boundaries, startup rejection of inconsistent payload overrides, and early empty 401/431 responses without key/authentication/audit calls. The existing limiter remains before the completed Authorization middleware; correlation and HTTPS behavior remain upstream.

**VERIFIED IN SOURCE AND DEPLOYED:** Login request bodies are limited to 8192 UTF-8 bytes after routing and before model binding. The middleware reads at most 8193 bytes into a bounded buffer, so a missing or misleading `Content-Length`, chunked HTTP/1.1 body, or HTTP/2 stream cannot bypass the limit. Exact-limit input is preserved for model binding; one byte over returns 413 without authentication, LDAP, token, key, or audit business work. The general decoded request envelope is limited to 16128 bytes, counting the UTF-8 request target and each header name/value with field and duplicate-value separators. Each header, including duplicate Authorization values, is counted once by the general policy; the completed Authorization middleware retains its separate 12352-byte value limit. One byte over returns 431. These policies are deployed and accepted on direct IIS HTTP/1.1 and HTTP/2. The measured managed envelope includes IIS-inserted Connection: close (19 bytes); corrected accounting proves the exact 16128/16129 boundary without a production fix.

**VERIFIED:** Login requests use a bounded fixed-window rate limit of 10 requests per minute with no queue. Health and protected endpoint behavior remains covered separately.

**VERIFIED:** The default audit connection configuration enables SQL transport encryption. Development/test configuration uses an explicit local certificate-trust exception only where required by the local SQL instance.

**ENVIRONMENT VERIFIED (2026-09-07):** On the inspected application/SQL host, a CA-issued SQL server certificate with matching FQDN/short-name SANs, Server Authentication EKU, RSA 2048 and AT_KEYEXCHANGE passed chain/revocation validation. The SQL service loaded the recorded certificate after restart. A Microsoft.Data.SqlClient 6.1.2 probe using the FQDN, `Encrypt=True` and `TrustServerCertificate=False` reached the application database over encrypted TCP. The production connection configuration was updated accordingly; no Audit override was found in the inspected IIS/environment sources. A synthetic invalid-token HTTPS request produced its expected audit row, and the IIS identity's SQL session reported encrypted TCP. Certificate metadata, configuration/script backups and binding evidence are retained in the controlled server backup directory. No private key was exported and no trust bypass was introduced. This evidence applies to this host, not uninspected deployments or real-user login.

## Authorization validation

**VERIFIED:** Tests cover default authenticated-user behavior, Reader authorization, Operator denial, invalid or missing roles, group mapping, role precedence, and fail-closed configuration.

## Audit logging validation

**VERIFIED:** Tests and SQL scripts cover typed stored-procedure writes, event validation, sensitive JSON rejection, correlation data, unavailable-database behavior, and concurrent writes.

**NOT IMPLEMENTED:** Retention, archival, purge automation, and SQL Agent scheduling are not implemented in the repository.

## Health endpoint validation

**VERIFIED:** `GET /api/v1/health` returns the typed healthy response and is anonymous. The endpoint is an application liveness endpoint; it does not claim to prove LDAP, SQL, or certificate-store availability.

## IIS and deployment validation

**IMPLEMENTED:** The API has a file-system publish profile and the repository contains a staged deployment procedure with parity checks and rollback guidance.

**ENVIRONMENT VERIFIED (2026-09-07):** After SQL TLS remediation, the existing IIS application returned HTTPS health 200 and anonymous protected-resource 401 with normal certificate validation. The operational deployment script now validates HTTPS certificates and curl exit codes. The IIS site/pool are started. At that SQL-remediation checkpoint, application binaries had not been replaced. The later 2026-09-07 Phase 2A deployment supersedes that deployment-pending statement; the 2026-09-08 reduction and subsequent body/header deployment are now complete.

**VERIFIED:** The deployed JWT/Authorization boundaries were validated through direct IIS HTTP/1.1 and HTTP/2 using the documented request profile. The application body/general-header policies are also deployed and accepted on the inspected direct route. Security Slice 2 response headers and AllowedHosts are implemented and source-tested only; separate IIS deployment and acceptance remain pending. TestServer does not establish native/proxy behavior. No IIS or HTTP.sys limits were changed in Security Slice 2. HSTS and forwarded headers remain deferred.

**HISTORICAL:** Phase deployment records describe environment-specific checks. They are retained as historical records and are not independent current evidence.

## Real AD identity validation

**NOT VERIFIED FROM REPOSITORY EVIDENCE:** The repository does not contain sufficient independently reproducible evidence to prove a current real-user AD login, live group mapping, issued-token verification, or authenticated access against a protected deployment. The historical acceptance record is not treated as current proof.

## Known limitations

- Live AD authentication requires protected credentials and a reachable directory.
- Certificate-store private-key behavior depends on the target Windows certificate store and runtime identity.
- DPAPI behavior depends on the protected secret file and Windows identity.
- SQL integration depends on an authorized target database and application identity.
- No refresh tokens, sessions, MFA, federation, account lockout, or directory-level brute-force protection are implemented. Login rate limiting is an application-level control.
- Audit retention and purge ownership remain outside the implementation.

## Evidence and reproducibility

Reproduce the repository checks from the repository root:

```powershell
dotnet restore .\LabAuthServer.slnx
dotnet build .\LabAuthServer.slnx -c Release --no-restore --nologo
dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo
```

Historical phase documents remain available under `docs/archive/` for traceability. They may contain older counts, proposed designs, or environment-specific observations and must be interpreted as historical.

## Historical pre-deployment checkpoint: 2026-09-08 transport-compatible budget reduction

**SOURCE VERIFIED:** 7680-byte issuance payload, 12288-byte encoded JWT and 12352-byte Authorization values; claim semantics unchanged. Full suite: 237 passed (165 unit, 72 integration), zero failures/skips. Release: zero warnings/errors. The prior 16384 payload override is rejected at API startup with the new constants.

**ENVIRONMENT VERIFIED, LIMITED SCOPE:** The actual deployed issuer and active signing certificate produced a 10664-byte token at exactly 7680 payload bytes using in-memory options only. Synthetic contract-valid Unicode identity/scope data round-tripped; oversized serialized claims failed without truncation. This is not a fresh real-user login or an IIS-identity private-key permission test.

Actual IIS HTTP/1.1 and HTTP/2 requests at 12287/12288/12289 token bytes and 12351/12352/12353 Authorization bytes all returned 200 with matching correlation under the OLD deployment. The explicit profile includes ordinary headers plus 3072 bytes of additional metadata. This proves candidate boundary reachability on that direct route, not deployment of the new rejection policy. The new 401/431 behavior and absence of downstream calls are proven by automated application tests; actual deployed rejections remain pending authorization. Uninspected proxies and other request profiles are not verified.

See [the detailed measurements, accounting and deployment plan](plans/Phase-2/JWT-Transport-Budget-Reduction.md). Live configuration/binaries and IIS settings were preserved. No registry, TLS, certificate, unrelated control, deployment, commit or push changes are part of this increment.

## Security Slice 2 checkpoint (2026-09-09)

Source-only host filtering and API response policy are verified by 35 new tests; the full suite is 283 passing tests with no failures/skips and the final Release build has no warnings/errors. Unknown/empty Host rejection precedes application work; malformed-port TestServer input throws in the framework outside the explicit pipeline. No live validation of these new headers/host settings is claimed. HSTS and forwarded headers were not enabled.

The prior body/header deployment and corrected HTTP/2 accounting are COMPLETED, supported by local `Phase2A-BodyHeaderDeploy-20260909-065832/Deployment-Report.md` and `Phase2A-Http2Accounting-20260909-072752/Diagnostic-Report.md` under `C:\Apps\LabAuthServer\Temp`. This checkpoint supersedes prior deployment-pending statements for the four size controls. Detailed scope, tests, header assessment and remaining IIS acceptance are in [Security Slice 2](plans/Phase-2/Phase-2A-Security-Slice-2.md).

## LDAP failure-classification checkpoint (2026-09-10)

SOURCE VERIFIED: all 391 tests pass (260 unit, 131 integration), with no failures/skips and no Release warnings/errors. New tests inject real LDAP exception types and Infrastructure search snapshots; six additional HTTP scenarios use the actual authentication/group services through the connection seam. They verify service/user bind classification, failed-group short circuit, safe errors/logging, existing audit codes and success ordering. Existing JWT/body/header/security regressions pass.

This is source/test evidence, not deployment or native-cancellation validation. No new timeout/deadline, retry or concurrency behavior is claimed. The existing no-role policy, best-effort audit and Root DSE cancellation limitations remain. Details and local evidence location: [LDAP classification plan](plans/Phase-2/Phase-2A-LDAP-Failure-Classification.md).

## Cooperative LDAP cancellation and deadlines (2026-09-10)

IMPLEMENTED AND SOURCE-TESTED under the approved cooperative contract. The application preserves origin/stage, rejects late LDAP outcomes, shares one validated decision budget across login and awaits native work/cleanup. Native work may outlast the deadline; immediate abort is not claimed. 191 tests added over the 391-test classification baseline. Targeted LDAP/configuration selection: 339 passed; full suite: 582 passed, no failures/skips; Release: zero warnings/errors. No live infrastructure validation performed. See [implementation and evidence](plans/Phase-2/Phase-2A-LDAP-Cancellation-Deadlines.md).

## LDAP concurrency/resource validation (2026-09-10)

SOURCE VERIFIED: 64 new concurrency tests pass (25 unit, 39 integration); targeted LDAP/configuration selection 403 passed; full suite 646 passed, zero failures/skips. Release: zero warnings/errors. Tests prove shared lifetime, finite concurrency, origin-aware waiting, release and retained permits during late native completion, and unchanged public rate limiting/security behavior. Two new startup-fixture failures in the first full run were fixed by directly owning the validation host; no production/test assertion weakening. The gate covers the existing public login orchestration; raw helpers and a strict pending-waiter cardinality cap are outside that guarantee. [Implementation, limitations and retained evidence](plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md). No infrastructure acceptance was performed.

## Final Phase 2A application-code review (2026-09-10)

COMPLETE; READY TO CLOSE application coding after four targeted fixes: previous-key retirement no longer disables the active key, token configuration rejects unsupported algorithms/subsecond lifetimes, oversized audit identities are explicitly omitted without dropping the event, and empty client correlation GUIDs are replaced. Final suite: 665 passed (436 unit, 229 integration), 0 failures/skips; 19 new regression cases. Targeted LDAP/cooperative: 389; concurrency: 64; changed-behavior/configuration: 30. Release: 0 warnings/errors. No existing tests weakened. See [findings, exact inventory and evidence](plans/Phase-2/Phase-2A-Final-Application-Code-Review.md). All infrastructure/release work and the documented hard-waiter-cap decision remain deferred. No deployment, live configuration, commit or push change.
