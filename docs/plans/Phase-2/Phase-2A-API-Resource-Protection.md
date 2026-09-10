# Phase 2A — API & Resource Protection

> 2026-09-10: [LDAP failure classification](Phase-2A-LDAP-Failure-Classification.md) is implemented and source-tested under the coding-only scope. It supplies typed failure/group outcomes and deterministic HTTP/audit mapping; it does not implement the later cancellation/deadline/concurrency controls.

> 2026-09-09 checkpoint: JWT/Authorization and login-body/aggregate-header controls are COMPLETED AND DEPLOYED, including corrected HTTP/2 managed-header accounting. A6 response headers and A7 AllowedHosts are now source-implemented and tested in [Security Slice 2](Phase-2A-Security-Slice-2.md); deployment remains separately authorized. A5 HSTS and forwarded-header trust remain deferred. Earlier current-state/decision text below describes the planning baseline.

Status: PARTIALLY IMPLEMENTED: JWT/Authorization size boundary and RSA range are deployed and validated; login body and general decoded-header budgets are implemented in source and not deployed; remaining controls are PLANNED. [Phase 2 sequence](README.md). All paths below are repository-relative.

## JWT Size Boundary Decision

Decision review: 2026-09-06, baseline `6793324365d860085080ab53bb7ccdb3b4401c28`.
Implementation review: 2026-09-08. Status: **Option A budget reduction deployed and production-validated.** Direct IIS HTTP/1.1 and HTTP/2 boundary validation passed for the documented profile. Remaining Phase 2A controls, including deployment of the new body/header policies, are not complete.

### Repository compatibility investigation

At baseline, `CertificateSigningKeyProvider.ValidateCertificate` enforced a minimum RSA size of 2,048 bits and no maximum. `TokenOptions` has no RSA-size setting. The signer still checks the serialized payload budget with unchanged semantics; inbound transport has a separate fixed ceiling. The baseline's permissive RSA implementation was not a requirement to support larger keys.

Searches covered active source, tests, current Markdown documentation, hidden/ignored local configuration and publish copies, certificate/script filenames, and the two current SOP Word documents. No current project requirement, test certificate, certificate-generation script or repository infrastructure procedure requiring/using RSA above 4,096 bits was found. At the decision baseline, positive RSA test fixtures used 2,048 bits and the weak-key negative certificate test used 1,024. References to 16,384 are payload/audit or unrelated tool settings, not RSA key requirements. The previous 8,192-bit planning example was a conditional calculation, not a supported deployment requirement, and is superseded here.

Public-key-only inspection of certificate references in the source settings and both local publish settings found matching 2,048-bit RSA certificates, with no configured previous certificate in those inspected settings. No private keys, certificate exports or secrets were read or written. Publish copies and tool caches are not current architecture authorities. Archived documents were excluded from current requirements. The separately controlled deployment script and uninspected remote environments are not verified by this repository review; deployment inventory remains a rollout responsibility.

### Established RSA policy

| Policy element | Implemented policy |
| --- | --- |
| Minimum RSA modulus size | 2,048 bits |
| Maximum RSA modulus size | 4,096 bits |
| Required supported/tested sizes | 2,048, 3,072 and 4,096 bits |
| Rejected sizes | Below 2,048 or above 4,096 bits |
| Representation | Fixed security-policy constants; no operator-configurable RSA maximum |
| Scope | Active and previous certificates, signing-key loading and public validation-key loading |

Enforce the inclusive 2,048–4,096-bit range; do not add a separate discrete-size restriction beyond that range. Keep RS256 and all existing algorithm requirements unchanged. Preserve certificate validity, RSA type, compatible KeyUsage, current no-EKU policy, private-key requirement for signing, public-key-only validation, unique currently valid certificate selection, key identifiers and previous-key overlap checks.

The maximum establishes a deterministic maximum RSA signature length and thus a bounded JWT/Authorization transport budget and resource envelope. It is not merely a cryptographic-strength preference. With 4,096-bit RSA, the signature is 512 bytes, independent of payload length. Ordinary configuration must not override the maximum; do not introduce an unrestricted `MaximumRsaKeySize` option.

`Infrastructure/Security/RsaKeySizePolicy.cs` supplies the shared fixed policy used by `CertificateSigningKeyProvider` and the runtime/test `ProtectedSigningKeyProvider` for active/previous signing and validation access. The signer and bearer resolver also check the range. Validation checks only public material. No package or authentication architecture changes are required. An out-of-policy previous key fails closed; missing previous signing overlap expiry also fails closed in the alternate provider.

**Runtime status:** the inclusive range is implemented and tested. Deployments must inventory active and overlap keys before rollout. If a previously uninspected >4,096-bit key is discovered, stop that deployment and obtain a migration decision; do not weaken the maximum or silently remove overlap.

### Approved Option A reduction, 2026-09-08

The former 16384 payload / 24576 encoded JWT / 24640 Authorization policy was implemented and deployed, but its maximum could not traverse native HTTP.sys. It is superseded in source by **7680 / 12288 / 12352**. Native limits remain unchanged. The independent **MaximumClaimSize = 4096**, mixed UTF-16 identity / UTF-8 role-scope semantics, RSA range, keys, algorithm and validation rules remain unchanged. No truncation is introduced.

The actual deployed issuer and active 2048-bit key produce **10664 encoded bytes** from exactly 7680 payload bytes. The conservative bound for every supported key/header is `1072 + 10240 + 683 + 2 = 11997`, leaving 291 bytes below the fixed 12288 ceiling. Startup requires `1072 + ceil(4 * MaximumTokenSize / 3) + 683 + 2 <= 12288`. Payload 7898 is the mathematical compatibility edge; 7899 and the old 16384 fail. The approved supplied setting is 7680.

Authorization retains seven scheme bytes and 57 formatting bytes: `12288 + 64 = 12352`. The middleware counts parsed UTF-8 values and comma separators before joining. Above 12352 returns empty 431; an oversized Bearer token within the value allowance returns empty 401 with a Bearer challenge. Both reject before downstream authentication/key/audit work. The bearer handler uses the same fixed encoded ceiling. Correlation, HTTPS redirect, routing and the existing login limiter retain their earlier ordering.

Complete HTTP/1.1 Authorization framing adds 17 bytes, giving 12369 and 4015 bytes of nominal aggregate headroom. Actual direct IIS probes with ordinary headers and an additional 3072-byte metadata value reached all below/exact/one-over candidate boundaries in HTTP/1.1 and HTTP/2. At the exact header boundary, HTTP/1.1 wire request headers/line total 15712; the HTTP/2 decoded field-section estimate is 16029. Both returned 200 under the OLD deployment with matching correlation. These prove reachability for the explicit profile, not deployment of new 401/431 behavior. Other client profiles, larger request targets, proxies/LBs and HTTP/3 are not verified.

Full suite: **237 passed (165 unit, 72 integration), zero failures/skips**. Release: **zero warnings/errors**. Existing 225-test regressions were retained and 12 cases added. Application tests include exact 7680 issuance and one-byte-over rejection, actual 11997-byte worst-key output, valid inbound boundaries, early rejection spies, repeated values, Unicode/escaping preservation and unchanged approved-role behavior.

See [JWT Transport Budget Reduction](JWT-Transport-Budget-Reduction.md) for implementation ownership, measured claim fixtures, exact header profile/accounting, configuration compatibility, remaining risks and the unexecuted deployment plan. At authorized rollout, merge only MaximumTokenSize into preserved live settings; do not replace current key/SQL metadata with source defaults. No deployment, commit, push or unrelated security control is included in this increment.

# Objective

Reject excessive or untrusted HTTP input before model binding, LDAP, JWT cryptography or audit writes consume unnecessary resources. Establish a verifiable HTTPS and host boundary for Windows/IIS.

# Current State

Program.cs configures HTTPS redirection, routing, Login rate limiting, authentication, authorization auditing and authorization. AuthController.Login also checks Request.IsHttps. Login is globally limited per application process by one constant partition: 10 requests/minute, queue zero. Health and protected resources are outside that policy.

The application now enforces the narrow Authorization/JWT ceilings described above. General body/header/request-line limits, HSTS and security-header policy remain unimplemented. Candidate transport reachability is verified only for the documented direct HTTP/1.1 and HTTP/2 profile; deployed new-policy rejections and other environments remain unverified. appsettings.json has AllowedHosts="*". Source issuance retains Token.MaximumClaimSize=4096 and reduces MaximumTokenSize to 7680; the inbound handler uses the separate fixed 12288-byte ceiling. Live deployment still has the old budgets. RequireHttpsMetadata is not enforcement of HTTPS on incoming bearer requests.

# Problem / Risk

Large JSON, authorization headers, slow requests and repeated invalid JWTs can consume memory, threads, signing-key lookup work and SQL audit latency. A login rate limit alone does not bound all resource use. Wildcard hosts and an unverified proxy scheme boundary can undermine redirect correctness. Limits at IIS may reject traffic before application telemetry or ProblemDetails can run.

# Scope

Current work follows the [approved coding-only scope](Coding-Only-Scope.md). Infrastructure, IIS, HTTP.sys, proxy/network/WAF, browser/HSTS, capacity and operational-release activities are **DEFERRED** with reasons there. Their acceptance criteria below belong to a future infrastructure/release workstream and do not block application coding. Retain application resource protections and automated tests; do not add source workarounds to satisfy deferred infrastructure requirements.

The following control records provide the current state, problem, change location, configuration, tests, operational effect, compatibility risk, rollback and completion evidence for every proposed control. Proposed option names are design suggestions, not settings that already exist.

# Non-Goals

Do not change token claims, role precedence, routes, authentication design or existing limiter semantics. Do not introduce distributed/per-username partitioning, browser authentication, arbitrary CORS policy or a new package.

# Proposed Architecture

Keep transport controls in Api and host configuration; keep token policy in the existing token boundaries. Use startup-validated, environment-approved limits. Determine effective ceilings across IIS request filtering, the actual IIS hosting mode and ASP.NET Core; Kestrel settings alone do not establish the IIS boundary. Preserve correlation for application-generated rejections and record host-generated rejections through IIS monitoring.

## Control A1: request body limits

- **Current state:** LoginRequest is protected by an 8192-byte endpoint-specific body ceiling after routing and before model binding. The source implementation is not deployed.
- **Problem:** Large or streamed bodies can allocate memory before authentication is attempted.
- **Implemented solution:** The endpoint metadata selects application middleware that reads at most 8193 bytes into a bounded buffer, covering Content-Length, absent Content-Length, chunked input, and HTTP/2 streams without truncation or unlimited buffering. Over-limit requests return 413 before model binding.
- **Source areas:** src/LabAuthServer.Api/Program.cs, src/LabAuthServer.Api/Controllers/AuthController.cs, src/LabAuthServer.Application/DTOs/LoginRequest.cs only if approved field bounds are needed. Proposed API request-limit options/validator may be added under Api.
- **Configuration:** DECISION REQUIRED: login body bytes, any global ceiling and credential-field lengths, based on valid requests and Unicode encodings. Corresponding IIS request-filtering values belong to a future host change.
- **Tests:** Below/exactly/above byte limit, Unicode and valid long credentials; full pipeline spies prove no authentication/token/audit business call for early rejection.
- **Negative tests:** Chunked and misleading length input, malformed JSON, oversized strings, cancellation mid-body; real-host slow-body test.
- **Operational impact:** Rejections may appear only in IIS logs. Application rejection should use 413 when its boundary handles the request; record actual IIS status/substatus.
- **Compatibility risk:** Over-tight credential length can reject valid AD credentials; do not truncate input.
- **Rollback:** Restore last approved finite bounds and matching host configuration.
- **Acceptance:** Valid boundary samples succeed; oversize never reaches LDAP; streaming does not evade the ceiling.

## Control A2: request line and headers

- **Current state:** A 16128-byte application policy counts the decoded request target and each header name/value with field and duplicate-value separators. The source implementation is not deployed. Authorization remains governed by its completed separate 12352-byte value policy.
- **Problem:** Excessive headers and long targets consume parser resources and may enter diagnostics.
- **Implemented solution:** General decoded-header middleware rejects above 16128 with 431 before rate limiting and authentication, while counting each Authorization field once and leaving AuthorizationSizeMiddleware unchanged. The limit is below the approximate native 16 KiB envelope and preserves the measured 16029-byte HTTP/2 profile.
- **Source areas:** Program.cs; future IIS request filtering and host runbook; Middleware/CorrelationMiddleware.cs only if needed for validated-header handling.
- **Configuration:** DECISION REQUIRED: aggregate/header count/request-line limits and timeout settings. Measure effective host defaults first.
- **Tests:** Each boundary below/at/above, multiple headers, HTTP versions supported by the deployment, canonical correlation behavior.
- **Negative tests:** Duplicate Authorization, malformed headers, newline injection and oversized correlation header. Assert no LDAP/crypto work for server rejection.
- **Operational impact:** Host rejection codes may differ from application responses; alert aggregation must accommodate this.
- **Compatibility risk:** Large valid bearer tokens plus tracing/proxy headers may exceed aggregate limits.
- **Rollback:** Restore prior approved host/app limit pair without accepting arbitrary header sizes.
- **Acceptance:** A documented host-by-host limit matrix and real-server evidence prove rejection and valid-client compatibility.

## Control A3: JWT/request size boundaries

- **Current state:** IMPLEMENTED for the approved size-boundary increment. See the decision and evidence above: separate fixed transport limits, early rejection and shared RSA range. Claim-validation semantics remain unchanged; the issuance payload budget is reduced as approved above.
- **Problem:** Issuance limits do not bound attacker-supplied tokens or the cost of parsing/signature/key resolution.
- **Proposed solution:** Enforce the separate encoded-token size before parsing/key resolution and align handler limits with that boundary. Implement the approved fixed RSA policy consistently before relying on its size derivation. Preserve existing claim-validation behavior, required claims and single approved role checks; do not add claim-size normalization.
- **Source areas:** src/LabAuthServer.Api/Extensions/JwtBearerAuthenticationOptions.cs, src/LabAuthServer.Infrastructure/Security/TokenOptions.cs and TokenOptionsValidator.cs, src/LabAuthServer.Application/Services/TokenService.cs and TokenClaimsBuilder.cs for consistency investigation only.
- **Configuration:** The claim setting remains 4096, the issuance payload is reduced to 7680, and mixed character/UTF-8 semantics are preserved. Encoded-JWT/header-value limits of 12288/12352 bytes and RSA range 2048–4096 bits are fixed constants. Host field/aggregate capacity remains deployment-owned and must include framing and other headers.
- **Tests:** Issued token round trip, token/claim below/at/above configured bounds, both supported token representations, no public/private key lookup for oversized tokens.
- **Negative tests:** Huge malformed/base64 payloads, duplicate claims/roles, invalid kid, algorithm/signature/issuer/audience, expired and overlap-expired tokens.
- **Operational impact:** Oversized bearer requests rejected at the application boundary should challenge safely; server limits may reject earlier. Avoid per-request expensive SQL amplification.
- **Compatibility risk:** Previously accepted external oversized tokens may stop working; preserve accepted issuer/audience and valid-size tokens.
- **Rollback:** Revert the affected validator change with reviewed finite transport protection retained; never restore private-key validation.
- **Acceptance:** Boundary tests prove early rejection, issuance compatibility and all Phase 1 bearer regressions pass.

## Control A4: resource exhaustion

- **Current state:** Login limiter does not cap protected-resource JWT/audit work or lingering blocking LDAP operations.
- **Problem:** Window-boundary bursts, slow dependencies and malicious tokens can exhaust workers or SQL capacity.
- **Proposed solution:** Define total request/dependency budgets; use bounded admission for LDAP in 2B and audit deadlines in 2C. Investigate server connection/header/body timeouts and bounded endpoint concurrency only where load evidence justifies them.
- **Source areas:** Program.cs and proposed Api options; Infrastructure/Services LDAP boundaries (2B), Infrastructure/Auditing/SqlAuditEventService.cs (2C).
- **Configuration:** DECISION REQUIRED: concurrency, queue/admission behavior and timeout budgets per host; no unbounded queue or attacker-keyed limiter.
- **Tests:** Bursts across a fixed-window reset, slow LDAP/SQL, parallel invalid-token traffic, cancellation and recovery; measure active work, memory, worker availability and health latency.
- **Negative tests:** Unresponsive dependency cannot create unbounded pending work; releasing a caller must not hide continuing native work.
- **Operational impact:** Intentional overload rejection must be distinguishable from invalid credentials.
- **Compatibility risk:** Shared admission can reduce throughput and fairness.
- **Rollback:** Restore last measured finite budget; isolate or roll back the increment if saturation persists.
- **Acceptance:** Agreed load envelope meets recorded latency and memory/worker ceilings and recovers after fault removal.

## Control A5: HSTS

- **Current state:** HTTPS redirection exists; UseHsts is absent.
- **Problem:** Browser clients lack a persisted HTTPS preference.
- **Proposed solution:** Add production-only HSTS at one chosen host/app owner after validating the real TLS boundary.
- **Source areas:** Program.cs and future IIS HTTPS runbook/settings.
- **Configuration:** DECISION REQUIRED: max-age and host scope. Start with reviewed conservative rollout; includeSubDomains/preload require separate ownership evidence and approval.
- **Tests:** HTTPS production responses include exactly the approved header; Development excludes it; validate HTTP redirect behavior separately.
- **Negative tests:** Spoofed forwarded scheme cannot cause insecure treatment; health/error responses and proxy duplication are checked.
- **Operational impact:** Clients cache policy beyond deployment rollback.
- **Compatibility risk:** Subdomains or clients without working HTTPS may become inaccessible.
- **Rollback:** Restore valid HTTPS first; a reduced/max-age=0 policy must reach clients over HTTPS, and does not instantly clear every cache.
- **Acceptance:** TLS/binding checks pass before enablement and browser persistence/recovery is rehearsed in staging.

## Control A6: security headers

- **Current state:** No explicit response-header policy is registered.
- **Problem:** Token responses can be cached; missing MIME/framing policy leaves unnecessary browser exposure.
- **Proposed solution:** Review no-store on token responses, X-Content-Type-Options: nosniff and an appropriate framing policy. Decide whether CSP adds value for this JSON-only API; do not invent a UI policy.
- **Source areas:** Program.cs, Controllers/AuthController.cs and a proposed Api/Middleware/SecurityHeadersMiddleware.cs if application ownership is selected.
- **Configuration:** DECISION REQUIRED: header ownership and exact set; avoid duplicate/conflicting IIS and application values.
- **Tests:** Login success/failure, health, protected 200/401/403, 429 and safe 500; assert headers without exposing response contents.
- **Negative tests:** Errors and early application exits retain required headers; separately capture host-generated responses.
- **Operational impact:** One owner maintains policy; caching behavior changes must be documented.
- **Compatibility risk:** Browser framing/caching assumptions may change.
- **Rollback:** Revert only incompatible headers; maintain safe token cache behavior.
- **Acceptance:** Approved response matrix passes at application and IIS boundaries with no duplicate policy.

## Control A7: AllowedHosts

- **Current state:** appsettings.json allows "*".
- **Problem:** Unexpected Host values may affect redirects and virtual-host routing.
- **Proposed solution:** Require explicit production hosts and verify effective host filtering before application work.
- **Source areas:** Program.cs if startup validation is needed; appsettings.json/environment configuration in a future implementation; deployment runbook.
- **Configuration:** DECISION REQUIRED: public DNS names, aliases, probe hostname, proxy topology and local-development exception.
- **Tests:** Approved names, casing/port behavior, unknown names, direct-IP probes and forwarded-host handling.
- **Negative tests:** Forged Host/forwarded host produces no attacker-controlled redirect and invokes no login logic.
- **Operational impact:** Probes and administrators must use approved names.
- **Compatibility risk:** Undocumented aliases may fail.
- **Rollback:** Restore the last explicit approved list or add a verified alias; do not use a production wildcard as routine recovery.
- **Acceptance:** Only approved production hosts work, including the actual monitoring path.

## Control A8: HTTPS boundary

- **Current state:** Redirect middleware precedes login; the controller's HTTP 400 test bypasses that middleware. No explicit forwarded-header policy appears in Program.cs.
- **Problem:** Controller-only tests do not prove network behavior; untrusted scheme forwarding could bypass intended protection.
- **Proposed solution:** Inventory IIS in/out-of-process and any upstream TLS termination; verify framework integration before adding forwarding middleware. Trust only approved proxies if required. Ensure plaintext credentials are not accepted; a redirect does not undo plaintext transmission.
- **Source areas:** Program.cs, Controllers/AuthController.cs and future IIS/TLS deployment settings.
- **Configuration:** DECISION REQUIRED: edge HTTP login rejection/redirect contract, HTTPS port and trusted proxy/network settings.
- **Tests:** End-to-end HTTP/HTTPS login and bearer routes with redirects disabled, valid binding, trusted and untrusted forwarding.
- **Negative tests:** Forged X-Forwarded-Proto/Host, redirect loop and wrong host; no credentials processed on insecure path.
- **Operational impact:** Host TLS and application scheme must agree.
- **Compatibility risk:** Clients relying on HTTP redirects may need explicit HTTPS URLs.
- **Rollback:** Restore the known-good binding/topology configuration while retaining HTTPS-only authentication.
- **Acceptance:** Staging network evidence verifies correct scheme and safe handling across every hop.

## Control A9: existing login limiter interaction

- **Current state:** Constant partition "login", 10/minute, queue zero; middleware runs after routing and before authentication.
- **Problem:** New middleware may bypass/obscure 429 or change aggregate capacity; early rejections may never reach limiter/correlation.
- **Proposed solution:** Preserve the policy and explicitly test middleware order with each new boundary. Use bounded metrics for rejections in 2D.
- **Source areas:** Program.cs, Controllers/AuthController.cs and existing HealthEndpointTests.cs under IntegrationTests.
- **Configuration:** No rate-limit change is proposed; any future tunability requires a separate measured decision.
- **Tests:** First ten qualifying requests, eleventh rejection, reset and parallel callers; health/protected unaffected by Login policy.
- **Negative tests:** Many usernames/IP values cannot create growing partitions; oversized input cannot bypass size controls.
- **Operational impact:** Per-process aggregate limit remains shared by legitimate clients and resets on process restart.
- **Compatibility risk:** Tests sharing an application factory can consume each other's quota; isolate fixtures.
- **Rollback:** Restore established routing/limiter ordering.
- **Acceptance:** Existing 429 contract and partition bound remain intact alongside A1–A8.

# Implementation Steps

## Step 1

Capture effective IIS/host limits and legitimate request/token sizes in staging; resolve A1–A9 configuration and response decisions before changing enforcement.

## Step 2

Implement small future increments: body/header/token ceilings; host/HTTPS policy; response headers. Maintain layer boundaries and independently review middleware order.

## Step 3

Run pipeline and real-host tests, record effective limits and observed codes, then hand bounded load findings to 2B/2C and rejection signals to 2D.

# Source Areas Expected to Change

Primary files are src/LabAuthServer.Api/Program.cs, src/LabAuthServer.Api/Controllers/AuthController.cs and src/LabAuthServer.Api/Extensions/JwtBearerAuthenticationOptions.cs. Token option/validation changes belong under src/LabAuthServer.Infrastructure/Security/. Tests extend tests/LabAuthServer.IntegrationTests/HealthEndpointTests.cs, AuthenticationTests.cs, ProtectedEndpointTests.cs and tests/LabAuthServer.UnitTests/JwtBearerValidationTests.cs, TokenOptionsTests.cs, TokenServiceTests.cs. New boundary test files may be added in those existing projects. Host changes belong in later approved deployment configuration, not a new architecture.

# Configuration Changes

A1–A9 are the configuration decision register. Validate positive finite bounds and cross-limit consistency at startup where applicable. Keep production DNS/proxy values outside public plans. Record defaults, units, environment override mechanism and restart requirements before implementation.

# Database Changes

None required. Do not add an audit row for every rejected byte/header or introduce a new event type merely to count HTTP rejections.

# Testing Strategy

## Unit Tests

Validate option cross-limits, byte/character semantics and pre-parse JWT rejection without key access.

## Integration Tests

Use WebApplicationFactory with invocation spies and isolated limiter state for the route/status/header matrix. Test controller contracts separately from pipeline behavior.

## Security Tests

Execute A1–A9 negative cases; record sanitized outcomes, allocations and dependency-call counts.

## Regression Tests

Retain Phase 1 certificate/public-key/JWT tests, exact-role 401/403 behavior, canonical correlation and SQL encryption configuration tests.

## Manual Validation

Validate actual IIS request filtering, HTTPS forwarding, slow requests and HSTS behavior in staging; framework test hosts are insufficient.

# Security Considerations

Do not echo attacker input, log Authorization/body content, trust arbitrary forwarding, weaken TLS, or relax role/key validation to fit a limit.

# Operational Considerations

Map IIS-only errors into monitoring. Coordinate limits with probe hostnames and token/header overhead. Define a baseline load envelope rather than an invented throughput target.

# Compatibility Risks

A1–A9 enumerate control-specific risks. Roll out compatible bounds first; security rejection changes must be documented for clients.

# Rollback Plan

Use each control's rollback entry and the Phase 2 package/configuration procedure. Host configuration must match the restored binary. HSTS rollback requires its separate cached-client procedure.

# Acceptance Criteria

All A1–A9 acceptance entries pass. No early oversized request reaches LDAP/token issuance; valid-size login/JWT traffic and existing rate-limit behavior are preserved.

# Definition of Done

Approved limit/host/header matrix, passing automated and IIS checks, redacted load evidence, documented rollback and no unresolved enforcement decision. Future implementation review identifies every changed host/app setting.

# Open Decisions

RSA minimum/maximum, separate encoded-JWT/header-value budgets and preservation of claim-size semantics are implemented and tested as recorded above. DECISION REQUIRED for later work: other measured request limits, complete proxy/hosting topology and effective transport capacity, host allowlist, HSTS scope, security-header owner/set, HTTP edge behavior and workload budgets. Do not ship placeholder values.

# Dependencies

First implementation stage. Coordinate LDAP admission with 2B and audit deadlines with 2C; deliver stable rejection classifications to 2D and real-host cases to 2E.
