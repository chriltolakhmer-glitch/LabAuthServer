# Phase 3.6 — Capacity / Load Assessment

Date: 2026-09-10. Mode: **READ-ONLY**. No production load test was performed. No capacity setting was changed.

Related: [Phase 3.4 HTTP.sys assessment](Phase-3.4-HTTPsys-Assessment.md),
[Phase 3.5 network validation](Phase-3.5-Network-WAF-LB-Validation.md).

## 1. Objective

Assess whether the application's resource bounds are configured, bounded and fail-closed, using safe
evidence only. No aggressive stress test against production.

## 2. Result

**PASS (application bounds) / DEFERRED (live load measurement).**

Every application-level resource control is configured, bounded, and covered by deterministic tests
that do not require AD, network, IIS or SQL. Live throughput measurement against production is
**DEFERRED — unsafe for live production environment**.

## 3. Configured Resource Bounds

Verified from the deployed `C:\Apps\LabAuthServer\Current\appsettings.json` and the source code defaults.

| Control | Effective value | Source | Failure mode |
| --- | --- | --- | --- |
| Login rate limit | 10 requests / 60 s, queue 0 | `Program.cs` fixed policy | `429` |
| Login request body | 8192 UTF-8 bytes | `IngressSizePolicy` | `413` |
| Decoded general envelope | 16128 bytes | `GeneralHeaderSizeMiddleware` | `431` |
| Authorization header value | 12352 bytes | `AuthorizationSizeMiddleware` | `401`/`431` |
| Encoded JWT ceiling | 12288 bytes | token configuration | `401` |
| Issuance payload | 7680 bytes | `Token.MaximumTokenSize` (deployed) | fail-closed, no truncation |
| Claim size | 4096 bytes | `Token.MaximumClaimSize` (deployed) | fail-closed |
| RSA key size | 2048–4096 inclusive | `RsaKeySizePolicy` | startup rejection |
| Max concurrent LDAP operations | 4 (code default) | `LdapOptions` | bounded |
| Max pending LDAP waiters | 16 (code default) | `LdapOptions` | `ResourceExhausted` → `503` |
| Maximum group memberships | 100 (code default) | `LdapOptions` | `ProtocolFailure` → `503` |
| Authentication deadline | 30 s (code default) | `AuthenticationOperation.DefaultTimeout` | `Timeout` → `504` |
| LDAP connection timeout | 10 s (deployed `ConnectionTimeout`) | `LdapOptions` | `Timeout`/`DirectoryUnavailable` |
| Audit command timeout | 5 s (deployed) | `Audit.CommandTimeoutSeconds` | best-effort, primary response preserved |

Note: `AllowedHosts` in the deployed file is `"*"` while source is `DC01.lab.local`. See Phase 3.7 §7.

## 4. Failure Classification (fail-closed)

| Condition | Category | HTTP |
| --- | --- | --- |
| Invalid credentials | `InvalidCredentials` | 401 |
| Directory unavailable / busy | `DirectoryUnavailable` | 503 |
| LDAP / application timeout | `Timeout` | 504 |
| Caller cancellation | `Cancelled` | 499 where writable |
| Protocol / malformed response | `ProtocolFailure` | 503 |
| Pending-waiter capacity exceeded | `ResourceExhausted` | 503 |
| Configuration error | `Configuration` | 500 |
| Unexpected | `Unexpected` | 500 |
| Login rate exceeded | rate limiter | 429 |
| Body / header oversize | middleware | 413 / 431 |

Every path fails closed. No failure produces a token.

## 5. Existing Deterministic Concurrency / Bound Tests

These tests exercise concurrency and resource bounds using synthetic seams — no AD, network, IIS or SQL.

| Test file | Covers |
| --- | --- |
| `LdapConcurrencyLimiterTests.cs` | permit acquisition, release, capacity recovery |
| `LdapPendingWaiterTests.cs` | hard waiter cap, no leak, recovery, no double release |
| `LdapConcurrencyTests.cs` (integration) | HTTP contention, 504/429 behavior under load |
| `LdapPendingWaiterHttpTests.cs` (integration) | pending-waiter cap through the HTTP pipeline |
| `CooperativeLdapTests.cs` | cancellation/deadline at every stage, shared budget |
| `IngressSizeBoundaryTests.cs` | body/header exact boundaries |
| `JwtSizeBoundaryTests.cs` | JWT 12287/12288/12289, Authorization 12351/12352/12353 |
| `GeneralHeaderSizeMiddlewareTests.cs` | 16128/16129 envelope boundary |
| `RequestBodySizeMiddlewareTests.cs` | 8191/8192/8193 login body |
| `AuthorizationSizeMiddlewareTests.cs` | Authorization value boundary |
| `RsaKeySizePolicyTests.cs` | 2048–4096 key policy |
| `LdapReliabilityHardeningTests.cs` | membership bound, Root DSE cancellation |

All pass (see §7).

## 6. Deferred Live Load Testing

| Item | Status | Reason |
| --- | --- | --- |
| Sustained concurrent login throughput | **DEFERRED** | No isolated non-production environment established; running it against production risks a self-inflicted DoS. |
| LDAP saturation under real AD | **DEFERRED** | Would require controlled AD-side load and lockout-safe budget. |
| SQL audit write throughput | **DEFERRED** | Requires DBA-approved test window. |
| Memory/handle growth under sustained load | **DEFERRED** | Requires an isolated environment and monitoring. |
| Requests-per-second capacity figures | **NOT MEASURED** | No number is claimed. Any such figure would be fabricated here. |

No requests/second, concurrency ceiling, or saturation figure is asserted. None was measured.

## 7. Verification Run

| Selection | Result |
| --- | --- |
| Full suite | 487 unit + 242 integration = **729 passed**, 0 failed, 0 skipped |
| Release build | 0 warnings, 0 errors |

The concurrency and bound tests above are included in that run. No high-volume test was executed.

## 8. Explicitly Not Performed

- No production stress or load test.
- No sustained CPU/memory/network/SQL load.
- No denial-of-service style request volume.
- No LDAP account lockout risk introduced.
- No capacity setting changed.
- No restart, deployment, commit or push.

## 9. Conclusion

The application is **bounded and fails closed** at every inspected resource control, and those bounds
are covered by deterministic tests. Live capacity figures remain **unmeasured** and are correctly
deferred to an approved isolated test environment.