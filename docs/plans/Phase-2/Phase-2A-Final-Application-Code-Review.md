# Phase 2A final application-code review

## Current closure status after the new hard-cap requirement

**PHASE 2A APPLICATION CODING = CLOSED.** The approved hard pending-waiter cap is implemented and validated: default 16, range 1-128, separate from active concurrency, typed ResourceExhausted/503 rejection. The final interaction review preserves JWT/RSA budgets, body/header/host controls, LDAP classification, cancellation/deadline precedence, native permit ownership, group/signing containment, audit and middleware order. Full suite: 700 passed (465 unit, 235 integration); 35 waiter-cap cases, 64 existing concurrency cases, 424 targeted LDAP/cooperative cases; zero failures/skips, Release zero warnings/errors. No unresolved P0/P1/P2 defect was identified in this slice. See [implementation and closure evidence](Phase-2A-Hard-Pending-Waiter-Cap.md). Earlier review checkpoints below remain historical; infrastructure and release acceptance stay deferred.

2026-09-10: **COMPLETE. READY TO CLOSE the application-coding workstream after the targeted fixes below.** This is source review and deterministic automated validation, not deployment approval or operational release sign-off.

## Scope and baseline

Reviewed the actual application code, configuration, middleware, tests and prior Phase 2A records, with the repository's AGENTS.md, Project_Status, Architecture, internal/Coding_Standard_and_SOP and internal/Development_Plan instructions. Existing tracked/untracked work was preserved. No speculative refactor, dependency, authentication-provider redesign or infrastructure control was added.

Baseline HEAD: `6793324365d860085080ab53bb7ccdb3b4401c28`, nothing staged. Baseline: 646 tests (424 unit, 222 integration), 64 concurrency and 389 targeted LDAP/cooperative tests; Release zero warnings/errors. Captured git status, full tracked diff and hashes of all 206 nonignored repository files before editing. Baseline diff check passed.

## Findings and fixes

### F1 — P1: previous-key expiry disabled the active key

Path: `src/LabAuthServer.Infrastructure/Security/TokenOptionsValidator.cs`, Validate's previous-overlap check; runtime callers in CertificateSigningKeyProvider.GetActiveKeyAsync/GetKeyAsync/GetValidationKeyAsync, ProtectedSigningKeyProvider equivalents, RsaTokenSigningService.SignAsync, and Api/Extensions/JwtBearerAuthenticationOptions.Configure.

Observed: the whole options object was rejected when PreviousKeyExpiresAt passed, even while resolving/signing with the current active key. A normally started process with rotation metadata would lose active signing and validation when only its previous key should retire. This is a concrete timed availability failure, not a preference about rotation policy.

Reproduction: `SigningOverlapRegressionTests.ExpiredOverlap_RejectsPreviousButPreservesActiveSigningAndValidation`, for both real key-provider implementations. Synthetic in-memory RSA/certificate material, initially valid options and previous-key resolution, then a past expiry model the runtime transition without sleeping or modifying a store. Both cases failed at active-key resolution before the fix. The same tests now sign a token and validate it using the real bearer token handler after expiry, and reject previous-key private/public resolution.

Fix: retain strict startup Validate, add ValidateForRuntime with identical structural validation but without the whole-configuration future-expiry check, and use it at runtime. The existing per-request previous-key expiry checks remain authoritative. The active key never inherits the retired key's expiry. Fresh startup with stale previous metadata still fails and requires configuration cleanup; this review does not change that startup policy. No key identifiers, signing parameters, certificate settings, lifetime or token claims changed. The existing expired-key diagnostic assertion remains passing without weakening its test.

### F2 — P2: startup accepted settings that cannot issue tokens

Path: `src/LabAuthServer.Infrastructure/Security/TokenOptionsValidator.cs`, AccessTokenLifetime and SigningAlgorithm checks, used by AddTokenConfiguration's ValidateOnStart. RsaTokenSigningService.GetSigningParameters accepts exactly six algorithms; TokenClaimsBuilder requires at least one second.

Observed: `RS`, `RS999`, `PSgarbage` and a 500-millisecond lifetime passed options validation but fail the actual issuance path. Misconfiguration therefore appeared healthy at startup and caused login failures later. Four deterministic negative validator tests failed before the fix.

Fix: validate the signer's exact existing algorithm set (RS256/384/512 and PS256/384/512) and the claims builder's existing one-second minimum; retain the one-day maximum. Tests reject the incompatible cases and preserve all six supported algorithms. This changes rejection timing for unusable configuration, not the supported issuance contract. Current source settings remain unchanged.

### F3 — P2: oversized identities caused entire audit events to be dropped

Paths: `src/LabAuthServer.Api/Controllers/AuthController.cs`, RecordAuditAsync, and `src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs`, InvokeAsync, passing Username/Subject to `Application/Auditing/AuditEventValidator.cs` through SqlAuditEventService.WriteAsync.

Observed: the approved login/JWT identity contract can exceed the audit fields' 256 UTF-16-code-unit maximum. The audit validator rejects the entire event; the best-effort caller catches that rejection. A 257- or 1024-character accepted login can therefore return its normal token but lose its success audit. A denied request with a long signed subject has the same mismatch. Existing counting audit fakes did not execute this validator and missed the problem.

Fix: preserve the event and its category/status/correlation; if identity exceeds 256, omit Username and Subject together and set the bounded boolean `identityOmitted` in DetailsJson. Do not truncate an identity into another apparent username or copy it into another diagnostic field. Identities at/below 256 remain exact. Authentication inputs, directory queries and JWT subject remain untouched. SQL schema, procedure, writer, event codes and persistence semantics are unchanged. An omitted identity is an explicit information limitation, not a claim of full identity audit retention.

Evidence: AuditBoundaryRegressionTests use the same production pre-SQL AuditEventValidator, retain validation failures outside swallowed exceptions, and exercise HTTP login, actual token issuance and real bearer authorization. Tests cover 256/257/1024 lengths, unchanged JWT subject, denied requests and typed 401/503/504 failures. LDAP is faked here because the defect is the audit boundary; separate existing LDAP seam tests exercise provider classification. No database connection is made. The oversized login cases failed the production validator before the fix.

### F4 — P2: an all-zero client correlation GUID suppressed audit

Path: `src/LabAuthServer.Api/Middleware/CorrelationMiddleware.cs`, canonical X-Correlation-ID acceptance, followed by AuditEventValidator's nonempty CorrelationId requirement.

Observed: `00000000-0000-0000-0000-000000000000` was accepted as canonical but made audit events fail validation. A caller could induce audit loss with an otherwise valid login. The new HTTP test reproduced `Correlation ID is required.` before the fix.

Fix: treat Guid.Empty like other invalid supplied correlations and generate a fresh identifier. The test checks valid audit construction and exact response/audit correlation agreement. Valid nonempty canonical client correlations retain their existing behavior.

No P0 finding. All identified P1/P2 findings above are fixed and regression-tested. No additional unresolved P0/P1/P2 defect was established in this review. This is a bounded source/test conclusion, not a proof against all possible defects or production failure modes.

### F5 — P3: JWT documentation overstated inbound per-claim enforcement

Path: `docs/JWT.md`, bearer validation paragraph. It described oversized claims as rejected by authentication, but the inspected bearer path enforces the encoded token ceiling and required claims/roles, not all issuance per-claim limits. Clarified the text to distinguish encoded-token rejection from issuance field budgets. This directly related documentation correction changes no application behavior. Existing issuance and encoded-boundary tests support the distinction.

## Completed control review

| Area and actual implementation inspected | Result and evidence boundary |
| --- | --- |
| RSA policy; CertificateSigningKeyProvider, ProtectedSigningKeyProvider, RsaTokenSigningService, bearer resolver | Fixed inclusive 2048-4096-bit checks remain at provider/signer/validation boundaries. Certificate selection, public-only validation, active kid selection and previous overlap remain fail-closed; F1 fixes active availability at retirement. Synthetic key tests exercise these boundaries. |
| JWT budgets; JwtRequestSizePolicy, TokenClaimsBuilder, signer, TokenConfigurationExtensions | 12288 encoded bytes, 12352 Authorization-value bytes, 7680 serialized issuance-payload bytes. Issuer/audience/subject signer checks use UTF-16 units; role/scope builder checks use UTF-8 bytes; aggregate estimate and actual serialized payload are checked. No truncation. Startup conservative encoded-budget compatibility check unchanged. Bearer size bounds do not independently impose all issuance per-claim limits on externally supplied signed tokens. |
| Request budgets; GeneralHeaderSizeMiddleware and RequestBodySizeMiddleware | 8192-byte login body; at most 8193 bytes buffered to detect excess, including absent/untrusted content length. 16128-byte managed envelope counts request path-base/path/query, header names, four separator bytes per entry, UTF-8 values and commas between duplicate values. 413/431 unchanged. This is managed decoded accounting, not raw HTTP/2 compression or native wire accounting. |
| Security Slice 2; Program and ApiResponseHeadersMiddleware | Source AllowedHosts remains DC01.lab.local, empty hosts disallowed. Outer host filtering can reject before application correlation/headers. API OnStarting policy retains no-store, nosniff and DENY, including early application responses. HSTS and forwarding trust remain disabled/unconfigured. |
| LDAP classification; LdapFailureClassifier and service/client paths | User-bind 49 -> InvalidCredentials/401; service-bind 49 -> DirectoryUnavailable/503; 81/51/52 -> unavailable; 85/3 -> timeout/504; 1/2/4/11 -> protocol failure/503; malformed response -> protocol failure. Configuration and unexpected remain distinct. No service credential failure turns into user credential rejection. Expected failures use bounded diagnostics without exception objects. |
| LDAP response integrity; LdapResponseValidator, LdapConnectionFactory, DN parser and group lookup | Exact one-entry, complete responses and identity attributes are validated. Null/ambiguous/partial/referenced/malformed responses fail closed. Ranged membership is rejected; failed membership cannot expose Groups or become successful empty membership. Complete empty membership remains separate from failure. No paging, hidden retries or nested-group redesign. |
| AuthenticationOperation and controller/service checkpoints | One shared 30-second default decision budget, inclusive 1-60 seconds. First recorded origin persists; elapsed deadline wins at equality, including delayed timer observation. Caller -> 499 where writable; deadline -> 504. Checks surround stages, await boundaries and successful completion. |
| LDAP concurrency; singleton DI registration, limiter and AuthController | Default 4, range 1-32; one permit for sequential identity/groups and awaited cleanup. No per-request limiter or nested acquisition. Awaited semaphore waiting uses the same operation token; handoff recheck restores a late permit. Interlocked lease disposal is idempotent. Release precedes mapping/signing/audit, but native holders remain occupied until actual completion. |
| Pending waiter decision | Previously deferred; now implemented under explicit approval, default 16 and range 1-128, atomic typed ResourceExhausted/503 rejection. See the current closure section and hard-cap implementation record. No total memory/FIFO/distributed guarantee. |
| Authorization and JWT workflow | Existing issuer, audience, lifetime settings, claims, highest-role selection and Reader/Operator/Administrator policies preserved. Empty membership/no approved role retains generic 500 with no token. LDAP changes do not alter key selection or protected endpoint behavior. |
| Audit and disclosure | Success audit follows accepted token completion. Directory failures retain category/stage/reason and explicitly sourced numeric codes. F3/F4 repair event validity; no password/token/filter/DN/stack details are added. RequestAborted still governs best-effort audit: disconnect can prevent persistence and response delivery. No durable-after-disconnect promise or queue introduced. |
| Configuration and secrets | Deadline/concurrency and payload compatibility use ValidateOnStart. F2 aligns token validation with actual runtime support. Source appsettings, ignored development/local settings, project references and dependencies unchanged. Checked-in source references secret locations, not secret contents. No live settings, credential files or certificate stores were accessed. |

### Authentication state machine and native limitation

Request validation -> shared operation -> singleton permit -> identity result -> membership -> permit release -> role mapping -> token issuance -> operation.Complete -> best-effort success audit -> response. Typed identity/group failures stop before mapping/signing; failed membership has no readable groups. Exceptions unwind the lease; successful early release plus using fallback cannot double-release. A prior immutable identity success is not complete-login success: a later authoritative operation failure prevents token return and success audit.

Cancellation after a stage admission check can coexist with that stage already executing. A synchronous native LDAP call or signer already admitted may finish after the logical deadline; the returned late result/token is discarded. No later stage is started after an authoritative failure is observed. The implementation does not promise that CPU/native work physically stops at the cancellation instant. Operation.Complete linearizes acceptance before best-effort audit/delivery.

The source contains no `.Wait()`, `.Result` or Thread.Sleep in LDAP admission. Existing awaited Task.Run bridges execute synchronous provider operations; group credential loading uses GetAwaiter().GetResult inside that protected worker, and the bearer key resolver uses its existing synchronous API boundary. These are inspected limitations, not newly introduced asynchronous-native guarantees. Root DSE has its separate legacy awaited diagnostic path, is not exposed by health/login, and does not inherit the login gate. No fire-and-forget, retry, parallel identity/group work, thread abortion or background queue was found in the reviewed login path.

### Middleware ordering and precedence

Built-in host filtering precedes the explicit pipeline: API response headers -> global exception handling -> correlation -> HTTPS redirect -> routing -> aggregate header limit -> endpoint body limit -> Login rate limiter -> Authorization-value limit -> authentication -> authorization audit -> authorization -> endpoints. Body reading can precede rate rejection; the authentication budget begins in the controller. Host/HTTPS/general-header/body/rate rejection can intentionally win before later JWT checks. No ordering change was made. Managed tests cover accounting and rejection behavior; they do not emulate IIS HTTP/2 parsing, HPACK, native limits or production network behavior.

## Test review and validation

Existing LDAP tests exercise actual authentication/client/group implementations through connection seams and manual clocks, not only prebuilt controller results. They cover late native success/errors, source precedence, expiry equality, retained cleanup, repeated cancellation and permit reuse. Controller result fakes deliberately test orchestration separately. Concurrency probes retain failure flags outside caught audit exceptions, preventing false passes. F3/F4 add the previously missing production audit-validation boundary. A test count alone is not treated as proof of coverage.

New tests: 12 unit cases (2 overlap provider/signing/bearer transitions, 10 token configuration cases), 7 integration cases (identity boundary/denial, typed failure audits, empty correlation). Existing tests were not removed, disabled or weakened.

| Validation | Final result |
| --- | --- |
| Full solution | **665 passed: 436 unit + 229 integration; 0 failures, 0 skips** |
| Previous suite | 646; **19 added** |
| Targeted changed-behavior/configuration selection | 30 passed (23 unit + 7 integration) |
| Targeted LDAP/directory-result/cooperative selection | 389 passed (267 unit + 122 integration) |
| Targeted concurrency | 64 passed (25 unit + 39 integration) |
| Release build | 0 warnings, 0 errors |
| Restore | No dependency changes; projects restored/up to date |
| Security/architecture regression | Existing JWT/RSA/budget/header/host/policy/configuration tests included in full run; no standalone architecture-test project |

Evidence directory: `C:\Apps\LabAuthServer\Temp\Phase2A-FinalCodeReview-20260910`. Includes baseline status/diff/hashes, failing reproduction TRXs, targeted/final TRXs, build/restore logs and final integrity report. Initial audit-test setup also exposed a test-only scoped-service resolution error and analyzer warning; both were corrected in the new fixture. An intermediate full run caught the existing expired-key diagnostic assertion; the provider's safe diagnostic was preserved instead of changing that assertion. All final validation is green. Failed reproduction/intermediate artifacts remain for traceability.

## Exact file inventory for this review increment

Earlier dirty work is not part of this increment. Paths below are repository-relative.

| File | Purpose |
| --- | --- |
| src/LabAuthServer.Infrastructure/Security/TokenOptionsValidator.cs | Separate runtime overlap treatment; reject unsupported algorithms and subsecond lifetime. |
| src/LabAuthServer.Infrastructure/Security/CertificateSigningKeyProvider.cs | Apply runtime validation while retaining previous-key expiry enforcement. |
| src/LabAuthServer.Infrastructure/Security/ProtectedSigningKeyProvider.cs | Same overlap correction; retain safe expired-key diagnostic contract. |
| src/LabAuthServer.Infrastructure/Security/RsaTokenSigningService.cs | Keep active signing available after previous-key retirement. |
| src/LabAuthServer.Api/Extensions/JwtBearerAuthenticationOptions.cs | Keep active bearer validation available after previous-key retirement. |
| src/LabAuthServer.Api/Controllers/AuthController.cs | Preserve audit events with explicitly omitted oversized identity. |
| src/LabAuthServer.Api/Middleware/AuthorizationAuditMiddleware.cs | Same identity bound for denied-request audits. |
| src/LabAuthServer.Api/Middleware/CorrelationMiddleware.cs | Replace empty client GUID before audit creation. |
| tests/LabAuthServer.UnitTests/TokenOptionsTests.cs | Add incompatible/supported configuration cases. |
| tests/LabAuthServer.UnitTests/SigningOverlapRegressionTests.cs | New real-provider/signing/bearer overlap regression. |
| tests/LabAuthServer.IntegrationTests/AuditBoundaryRegressionTests.cs | New HTTP audit validation regressions. |
| docs/plans/Phase-2/Phase-2A-Final-Application-Code-Review.md | This review, findings, boundaries, fixes and evidence. |
| docs/Project_Status.md | Current review completion and totals. |
| docs/Validation_Status.md | Current final evidence and acceptance boundary. |
| docs/Testing.md | Current totals and new regression coverage. |
| docs/Configuration.md | Exact algorithm/lifetime validation and overlap distinction. |
| docs/JWT.md | Runtime overlap retirement and issuance/validation boundary clarification. |
| docs/AuditLogging.md | Identity omission and nonempty correlation semantics. |

## Deferred work retained

| Item | Reason |
| --- | --- |
| Native LDAP hard cancellation | Synchronous Windows LDAP provider limitation; no guaranteed immediate abort. |
| IIS acceptance testing | Deployment/operations work. |
| HTTP.sys tuning | OS/IIS infrastructure. |
| Proxy/forwarding validation | Network/infrastructure topology and trust verification. |
| HSTS deployment | Browser/deployment policy; remains disabled. |
| Network/WAF/load-balancer validation | Infrastructure access and path validation. |
| Infrastructure/production load testing | Operational workload and capacity analysis. |
| Operational release review | Change management, monitoring, rollback and sign-off. |
| Distributed/global LDAP concurrency | Separate architecture/infrastructure concern; instance-local gates do not coordinate globally. |

## Final recommendation and safety

**READY TO CLOSE Phase 2A application coding**, with the four concrete findings repaired and regression-tested. Do not infer deployment readiness or approve an operational release from this result. Review assertions apply to this source snapshot and test seams; deferred work remains separate.

HEAD remains `6793324365d860085080ab53bb7ccdb3b4401c28`, nothing staged. Existing uncommitted work is preserved. No source appsettings, LDAP controls, fixed size budgets, SQL settings, certificate configuration or dependencies changed. Only the listed application fixes, tests and documentation were edited.

NO DEPLOYMENT
NO IIS CHANGE
NO HTTP.sys CHANGE
NO SQL CHANGE
NO CERTIFICATE CHANGE
NO RESTART
NO PROXY CHANGE
NO HSTS
NO NETWORK/WAF CHANGE
NO PRODUCTION LOAD TEST
NO COMMIT
NO PUSH
