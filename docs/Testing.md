# Testing

## Toolchain

- .NET SDK: `10.0.400`, pinned by `global.json`.
- Target framework: `net10.0`.
- Test framework: xUnit with the .NET test SDK.
- Integration host: `Microsoft.AspNetCore.Mvc.Testing`.

## Projects

- `tests/LabAuthServer.UnitTests` covers application and infrastructure seams, configuration validation, LDAP boundaries, token issuance/signing/validation, role mapping, audit validation, and middleware behavior.
- `tests/LabAuthServer.IntegrationTests` covers API/controller behavior, the ASP.NET Core request pipeline, protected-resource authorization, health behavior, authentication contracts, and selected environment/database boundaries.

## Commands

Run from the repository root:

```powershell
dotnet restore .\LabAuthServer.slnx
dotnet build .\LabAuthServer.slnx -c Release --no-restore --nologo
dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo
```

## Current result

Phase 2A final hard pending-waiter cap validation completed with **700 passed, 0 failed, and 0 skipped** tests (465 unit and 235 integration), including all prior JWT/HTTP and ingress-budget regressions. The independently rerun baseline at `6793324` was 188 passing tests. Release build: zero warnings and errors.

## Coverage areas

The approved hard pending-waiter cap adds 35 deterministic tests (29 unit, 6 integration). They cover configuration/startup bounds, atomic cap enforcement, active/pending separation, cancellation/deadline/equality, repeated recovery, late handoff and typed-result ownership. A combined in-memory HTTP test holds four active permits, fills two pending slots, rejects eight further admissions safely with 503, verifies the next rate-limit 429 and expires waiting requests to 504 without downstream calls. Existing 64 concurrency tests and 424 targeted LDAP/cooperative tests pass. The earlier 24-contender active-limit test explicitly allows 24 pending slots and retains all-success assertions. No existing test is disabled or weakened; no infrastructure load claim is made. [Exact evidence and test boundaries](plans/Phase-2/Phase-2A-Hard-Pending-Waiter-Cap.md).

The tests cover configuration fail-closed behavior, LDAPS/UPN validation, LDAP filter escaping, group-to-role mapping, JWT claims and signing, public-key-only JWT validation, certificate validity/key-size/key-usage/duplicate-selection boundaries, issuer/audience/lifetime/algorithm/key-ID validation, 401/403 authorization behavior, login rate limiting, correlation IDs, safe `ProblemDetails`, encrypted SQL test configuration, SQL audit persistence, sensitive-data filtering, bounded login-body reads, aggregate decoded-header counting, and concurrency boundaries.

The original 37 size-boundary cases, updated for the reduced contract, cover RSA 1024 rejection, 2048/3072/4096 acceptance and 4160 rejection across active/previous signing and public validation in both providers; missing previous signing expiry; unchanged UTF-16 identity and UTF-8 role/scope limits; exact 7680-byte issuance payload; fixed-policy startup consistency; and encoded/header boundary rejection before key, authentication or audit calls. A real 4096-bit synthetic signing key and maximally escaped `kid` produce an accepted 11997-byte issuer token. Separately signed valid tokens at 12288 bytes and raw Authorization values at 12352 bytes pass the application pipeline. Multiple values and multibyte input cannot bypass header byte counting.

## What the tests do not prove

These new integration cases use real signing/bearer validation with synthetic authentication, group lookup and audit seams. The exact header whitespace case uses TestServer directly because HttpClient trims trailing whitespace. Neither proves IIS/native/proxy transport compatibility; that requires deployment verification. Existing Phase 1 validation and limiter regressions remain in the full suite.

Automated tests do not replace authorized environment validation. They do not prove that a target AD account can authenticate, that a target certificate private key is accessible to the runtime identity, that a DPAPI secret can be decrypted on a target server, or that an IIS deployment is currently running. Tests also do not establish an approved audit retention or purge process.

Tests use placeholders and isolated seams where possible. Do not add real passwords, tokens, private keys, or DPAPI contents to test fixtures.

## Transport budget reduction verification

The 2026-09-08 increment adds 12 cases over the previous 225-test suite. Coverage includes below/exact/above JWT and Authorization values, repeated-value separator accounting, old payload configuration startup failure, valid signed oversize rejection without downstream work, Unicode/escaping preservation, and all approved roles with a maximum login-field Unicode identity. Unicode roles remain unsupported. Exact 7680-byte issuance succeeds; one byte over fails without truncation. Release build has zero warnings/errors.

Actual HTTP/1.1 and HTTP/2 JWT/Authorization reachability and rejection behavior was verified by the completed 2026-09-08 deployment. The new login-body and general-header rejection policies are tested in TestServer and still require an authorized deployment. See [measurement results and the deployment test matrix](plans/Phase-2/JWT-Transport-Budget-Reduction.md).

## Security Slice 2 coverage and evidence

`ApiSecurityResponseTests` exercises approved/case/port/unknown/IP/empty/malformed hosts, spoofed forwarded Host, login 200/400/401/499/500/503/504, 8191/8192/8193 bodies, protected 200/401/403 and global 500, health/correlation, 429 and both 431 paths. Isolated `ApiResponseHeadersMiddlewareTests` verify API scope, preservation of status/body/content type/challenge/correlation, replacement of conflicting downstream header values and no HSTS. Unit tests verify cancellation and exception propagation. Existing pipeline fixtures now send the approved Host while retaining their JWT/body/header assertions.

Full suite: 175 unit + 108 integration = 283 passing, 0 failed/skipped. Final Release: 0 warnings/errors. Logs and TRX files are in `C:\Apps\LabAuthServer\Temp\Phase2A-SecuritySlice2-20260909`. Actual IIS HTTP/1.1/HTTP/2 validation of Security Slice 2 remains pending separate deployment authorization; TestServer does not reproduce native host parsing, site selection or header rewriting. See [the deployment acceptance matrix](plans/Phase-2/Phase-2A-Security-Slice-2.md).

## LDAP failure-classification tests

108 cases were added over the 283-test baseline. DirectoryResultTests verifies immutable result invariants and explicit diagnostic sources. LdapFailureClassificationTests exercises connection/bind/search injection, code 49 by stage, 81/85 and server operation codes, operation exceptions with/without response, network/TLS failures, cancellation/disposal, credential-store errors, identity completeness, group success/empty/failure, partial membership, DN escapes and safe logging. No live LDAP connection is used by these new tests.

LdapFailureHttpTests covers all approved failure statuses, safe ProblemDetails/correlation/security headers, deterministic audit contents, no mapping/signing after directory failure, retained no-role 500, token-failure behavior and login-success audit ordering. It also exercises actual LDAP services with a fake connection seam. Existing authentication and size/security fixtures were updated only for the new immutable result/group contract and approved fixed messages; their unrelated expectations remain.

Final results: 391 passed, 0 failed/skipped; Release 0 warnings/errors. Logs/TRX are under C:\Apps\LabAuthServer\Temp\Phase2A-LdapClassification-20260909. See [the full classification matrix](plans/Phase-2/Phase-2A-LDAP-Failure-Classification.md).

## Cooperative LDAP cancellation/deadline coverage (2026-09-10)

Current full suite: 582 passed (399 unit, 183 integration), no failures/skips; Release build: zero warnings/errors. CooperativeLdapTests adds 139 deterministic cases for stages, both origins, late results/errors, races, delayed timers, shared budgets, configuration and awaited cleanup. CooperativeLdapHttpTests adds 52 tests for real-service/fake-connection HTTP behavior, safe correlation/audit, RequestAborted persistence limits, invalid startup settings, late-service-result rejection, normal JWT/protected access and no-role compatibility. Targeted LDAP/configuration selection: 339 passed. No real AD/LDAP/IIS/SQL/network dependency or test deletion. Existing security and architecture-related checks pass in the full suite. [Contract and evidence](plans/Phase-2/Phase-2A-LDAP-Cancellation-Deadlines.md).

## LDAP concurrency/resource coverage (2026-09-10)

LdapConcurrencyLimiterTests adds 25 deterministic capacity, cancellation/deadline/precedence, handoff, idempotent release and contention tests. LdapConcurrencyTests adds 39 controller/native/HTTP/lifetime/configuration tests. Release checks cannot be hidden by best-effort audit swallowing. Native fakes hold search, bind, groups or cleanup while a second request waits. A ten-waiter in-memory test preserves the eleventh-request 429 response and safe deadline failures without LDAP entry. No real infrastructure load is generated. Targeted concurrency 64, targeted LDAP/configuration 403, full suite 646 passed; zero failures/skips and zero Release warnings/errors. Failed and corrected startup-fixture runs are retained in the evidence directory. [Report and exact evidence](plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md).

## Final Phase 2A application-code review (2026-09-10)

COMPLETE; READY TO CLOSE application coding after four targeted fixes: previous-key retirement no longer disables the active key, token configuration rejects unsupported algorithms/subsecond lifetimes, oversized audit identities are explicitly omitted without dropping the event, and empty client correlation GUIDs are replaced. Final suite: 665 passed (436 unit, 229 integration), 0 failures/skips; 19 new regression cases. Targeted LDAP/cooperative: 389; concurrency: 64; changed-behavior/configuration: 30. Release: 0 warnings/errors. No existing tests weakened. See [findings, exact inventory and evidence](plans/Phase-2/Phase-2A-Final-Application-Code-Review.md). All infrastructure/release work and the documented hard-waiter-cap decision remain deferred. No deployment, live configuration, commit or push change.
