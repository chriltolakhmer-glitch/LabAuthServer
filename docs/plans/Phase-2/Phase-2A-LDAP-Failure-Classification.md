# Phase 2A LDAP failure classification

2026-09-10: IMPLEMENTED, TESTED, SOURCE ONLY. This is the approved classification portion of [Phase 2B LDAP reliability](Phase-2B-LDAP-Reliability.md), delivered under the [coding-only scope](Coding-Only-Scope.md). It does not implement deadlines, cancellation redesign, concurrency, retries or infrastructure work.

## Architecture and result contracts

The existing flow is retained: AuthController -> IAuthenticationService/LdapAuthenticationService -> ILdapAuthenticationClient/LdapAuthenticationClient -> service credential loading -> service bind -> escaped UPN search -> account-control validation -> submitted-UPN bind. After successful identity verification, the controller separately calls ILdapService/LdapService for service-account group lookup, followed by existing role mapping and JWT issuance.

AuthenticationResult is now immutable with Succeeded/Failed factories; IsAuthenticated, FailureCategory and ErrorMessage are derived. Its former definition was moved from IAuthenticationService.cs into its own file without changing its namespace. The Infrastructure client reuses this application result, replacing duplicate LdapAuthenticationResult/LdapBindResult/LdapBindFailureCategory definitions. Both existing client methods and their CancellationToken parameter names are retained.

GroupLookupResult contains either an immutable successful membership snapshot or a DirectoryFailure. Groups cannot be accessed on a failure and cannot silently return an empty list. Successful zero membership is valid and distinct from failure.

DirectoryFailure contains a validated category, stage, enum reason, and optional numeric diagnostic with an explicit source (LdapError or OperationResult). No exception object, arbitrary message, LDAP response, filter or DN crosses this failure boundary. A numeric code without a source, a source without a code, undefined enum values and a None failure category are rejected. SafeMessage is selected from fixed strings. The controller never imports LDAP library types.

Authentication categories retain their numeric values and add ProtocolFailure: InvalidRequest, InvalidCredentials, DirectoryUnavailable, Timeout, Cancelled, ProtocolFailure, Configuration and Unexpected. None denotes successful authentication only.

Stages are CredentialLoading, ConnectionSetup, ServiceBind, UserSearch, UserBind, GroupSearch and ResponseValidation. Authentication failures retain their originating stage. Group failures expose GroupSearch or ResponseValidation; a service-bind/credential-loading cause remains explicit in the bounded reason, such as ServiceBindRejected or CredentialStoreUnavailable.

## Classification

Expected exceptions use a closed type/stage filter and the Infrastructure LdapFailureClassifier. The final catch is only the Unexpected fallback. Configuration errors are recognized at credential/configuration boundaries; unrelated InvalidOperationException is not automatically configuration. No localized exception text or server diagnostic string is parsed.

| Source/outcome | Category and reason | HTTP |
| --- | --- | --- |
| Complete identity, groups, role and token success | Success | 200 |
| User bind LDAP 49 | InvalidCredentials / UserBindRejected | 401 |
| Service bind LDAP 49, including group-query service bind | DirectoryUnavailable / ServiceBindRejected | 503 |
| LDAP 49 from another stage | Unexpected; stage retained, no inferred user rejection | 500 |
| LDAP 81 | DirectoryUnavailable / TransportFailure | 503 |
| Known DNS/socket transport error, network-stage IOException or TLS AuthenticationException | DirectoryUnavailable / TransportFailure | 503 |
| Busy 51 / Unavailable 52 | DirectoryUnavailable / DirectoryBusy or DirectoryUnavailable | 503 |
| Client timeout 85, server TimeLimitExceeded 3, TimeoutException, socket TimedOut | Timeout / OperationTimedOut | 504 |
| Caller-associated OperationCanceledException, cancellation-shaped LDAP/disposal error | Cancelled / CallerCancelled | 499 where writable |
| OperationCanceledException without caller cancellation | Unexpected / UnassociatedCancellation | 500 |
| OperationsError 1 / ProtocolError 2 | ProtocolFailure / OperationFailed or ProtocolError | 503 |
| SizeLimitExceeded 4 / AdminLimitExceeded 11 | ProtocolFailure / ResultLimitExceeded | 503 |
| Null/ambiguous/incomplete/malformed response | ProtocolFailure with bounded validation reason | 503 |
| DirectoryOperationException without Response | Unexpected / MissingOperationResult, no invented code | 500 |
| Missing/empty/unreadable/corrupt service credential | Configuration / CredentialStoreUnavailable | 500 |
| Invalid local LDAPS/service settings | Configuration / InvalidConfiguration | 500 |
| Invalid input | InvalidRequest / InvalidInput | 400 |
| Other/unidentifiable failure | Unexpected / UnexpectedFailure | 500 |

LdapException.ErrorCode and DirectoryOperationException.Response.ResultCode are handled separately and retain their diagnostic source. Direct search response codes use OperationResult. A bind operation exception may lack a Response; it is not classified by its message. Unknown socket errors are not assumed to be network outages. No 502 mapping is introduced. The existing 429 limiter is untouched.

## Response completeness and authorization boundary

Identity search requires a successful response without unresolved references and exactly one entry. Zero and multiple identity entries now produce ProtocolFailure, not InvalidCredentials. The entry DN and distinguishedName attribute must be present, parseable and matching; userPrincipalName must match the supplied UPN; userAccountControl must be a single nonnegative integer. A valid disabled flag remains an authoritative generic credential rejection. Display name remains optional. There is no fallback to a DN bind, anonymous bind or missing account-control value.

Group search requires a successful complete response and exactly one user entry with a valid DN. Missing memberOf on that complete entry, or an empty memberOf list, is successful empty membership. Ranged memberOf attributes, unresolved references, non-success status, malformed group DNs and ambiguous entry sets are failures. No failed lookup reaches role mapping or token issuance.

The DN parser preserves escaped delimiters and UTF-8 hex escapes rather than shortening a CN at an escaped comma. It accepts the existing single-CN mapping shape and rejects unsupported multi-valued/quoted RDNs, malformed escapes and control characters. Such unsupported forms now fail closed; no new directory/group semantics or nested-group expansion is introduced. Ranged membership is rejected rather than silently treated as empty or implementing a new paging mechanism.

**No-role policy is unchanged:** successful complete membership with no approved role still reaches the existing token contract, fails the exactly-one-approved-role requirement and returns the existing generic 500. Changing this to 403 is a separate authorization decision. JWT code, budgets, role precedence and mapping limits are unchanged.

## HTTP, audit and logging

Authentication/group failures return ProblemDetails with the mapped status, fixed safe Detail and correlationId when middleware supplied one. Existing X-Correlation-ID, no-store, nosniff and DENY response behavior remains. Internal category/stage/code and directory details are not added to the public body. Current fixed details are Authentication failed; Authentication service unavailable; Authentication request timed out; Authentication request was cancelled; or Authentication error.

| Outcome | Existing audit event |
| --- | --- |
| Complete login success, after groups/roles/token | AUTH_LOGIN_SUCCESS |
| InvalidCredentials, InvalidRequest, Cancelled | AUTH_LOGIN_FAILURE |
| DirectoryUnavailable, Timeout, ProtocolFailure, Configuration, Unexpected | AUTH_LDAP_FAILURE |
| Separate post-authentication role/token failure | APP_UNHANDLED_EXCEPTION |

The controller adds bounded failureCategory, stage, reason, diagnosticSource and diagnosticCode fields to existing DetailsJson. Username/subject, source IP, correlation/request IDs, status and success handling are retained. No event-code or SQL/schema/persistence change was required. The prior success audit was already after token issuance; that ordering is preserved and tested. The earlier identity-only success log was removed to avoid suggesting complete login success.

Expected LDAP logs contain bounded category/stage/reason/code with no raw exception object. Infrastructure logs participate in the existing correlation scope; controller failure logs explicitly include correlation. The controller's remaining role/token fallback also omits the exception object. Tests inject sensitive marker strings and verify they are absent from responses, failure audit details and logs.

Audit remains best-effort and uses RequestAborted. A cancelled request may not persist an audit event or deliver a response. Existing 1024-character login username versus 256-character audit username/subject bounds are not redesigned here; oversized audit fields can still reject persistence. No audit-delivery guarantee is claimed.

## Test seam and unchanged execution behavior

ILdapConnectionFactory/ILdapConnection wrap the existing System.DirectoryServices.Protocols connection with synchronous ConfigureSession, Bind, Search and Dispose operations. LdapConnectionFactory is registered through DI; the production path always uses the real provider. Infrastructure-only search snapshots expose result status, entries and reference presence for deterministic response validation. Tests can inject setup/bind/search exceptions and responses without a live directory or mocking package.

LDAPv3, LDAPS, default provider authentication mode, service-account search followed by submitted-UPN bind, separate service-account membership query, ConnectionTimeout values, DPAPI provider, Task.Run scheduling and existing disposal registrations are retained. The factory disposes a connection if timeout initialization fails. Group lookup still validates but does not bind with its password parameter; the interface comment now states the actual behavior.

Root DSE's existing diagnostic result/operation and in-flight cancellation behavior were not redesigned. This task changes the login identity/group classification path, not public health or probe architecture.

All existing cancellation parameters remain. Recognized caller cancellation and timeout are separate categories, and unassociated cancellation has an explicit reason. This does not promise that connection disposal immediately stops native work. No total deadline, new timeout value, retry, queue, concurrency limit or detached task was introduced. Cancellation after identity/group success can still hit the existing role/token fallback; revising the whole login cancellation contract belongs to the later cancellation work.

## Verification

- Restore: all projects up to date; no package/dependency changes.
- Final Release build: **0 warnings, 0 errors**.
- Full suite: **391 passed, 0 failed, 0 skipped** (260 unit, 131 integration), **108 added tests** over the 283-test baseline.
- Existing JWT 12287/12288/12289, Authorization 12351/12352/12353, body 8191/8192/8193, aggregate envelope, host filtering, response-header, normal login/protected, tampering and limiter regressions pass.
- New unit cases cover typed-result invariants, service/user/stage distinctions, setup/network/LDAP errors, operation exceptions with and without result codes, timeout/cancellation/disposal, credential failures, malformed/ambiguous identity, complete/empty/partial groups, DN parsing and safe logs.
- HTTP tests verify statuses, content type, fixed messages, correlation, response headers, category/stage/code audit fields, no mapping/signing after failure, unchanged no-role behavior, and success audit only after token completion. Additional tests run the actual authentication/group services through the connection seam to verify end-to-end classification and audit.
- A test assertion initially had an int-versus-nullable-short compilation mismatch; its type was corrected. No test expectation was weakened. Both full test runs completed with no failing tests.
- Evidence: `C:\Apps\LabAuthServer\Temp\Phase2A-LdapClassification-20260909`, including pre-edit source copies/hashes, build-final.log, tests-final.log and FinalTestResults/*.trx.

## Remaining scope

Classification implementation is complete. Source tests do not claim real IIS/AD deployment acceptance. Deadlines, cancellation redesign, concurrency and audit resource/persistence policy remain future coding tasks. Proxy/forwarding, HSTS, IIS acceptance, HTTP.sys, WAF/network/load-balancer, infrastructure capacity and operational release review remain DEFERRED under the coding-only scope.

NO DEPLOYMENT. NO IIS CHANGE. NO HTTP.sys CHANGE. NO SQL CHANGE. NO CERTIFICATE CHANGE. NO RESTART. NO PROXY CHANGE. NO HSTS. NO COMMIT. NO PUSH.

## Final inventory and preservation checks

This increment modifies 21 existing files and adds 20 files. Paths below are relative to the repository root. All pre-existing files remain; the pre-edit snapshot contains 177 files.

| Change | File |
| --- | --- |
| Modified | docs/plans/Phase-2/Phase-2A-API-Resource-Protection.md |
| Modified | docs/plans/Phase-2/Phase-2B-LDAP-Reliability.md |
| Modified | tests/LabAuthServer.IntegrationTests/ApiSecurityResponseTests.cs |
| Modified | tests/LabAuthServer.IntegrationTests/JwtSizeBoundaryTests.cs |
| Modified | docs/Architecture.md |
| Modified | docs/Project_Status.md |
| Modified | docs/Security.md |
| Modified | docs/Testing.md |
| Modified | docs/Validation_Status.md |
| Modified | src/LabAuthServer.Api/Controllers/AuthController.cs |
| Modified | src/LabAuthServer.Api/Extensions/ActiveDirectoryOptionsExtensions.cs |
| Modified | src/LabAuthServer.Application/Enums/AuthenticationFailureCategory.cs |
| Modified | src/LabAuthServer.Application/Interfaces/IAuthenticationService.cs |
| Modified | src/LabAuthServer.Application/Interfaces/ILdapService.cs |
| Modified | src/LabAuthServer.Infrastructure/Services/ILdapAuthenticationClient.cs |
| Modified | src/LabAuthServer.Infrastructure/Services/LdapAuthenticationClient.cs |
| Modified | src/LabAuthServer.Infrastructure/Services/LdapAuthenticationService.cs |
| Modified | src/LabAuthServer.Infrastructure/Services/LdapService.cs |
| Modified | tests/LabAuthServer.IntegrationTests/AuthenticationTests.cs |
| Modified | tests/LabAuthServer.UnitTests/LdapAuthenticationServiceTests.cs |
| Modified | tests/LabAuthServer.UnitTests/LdapInfrastructureTests.cs |
| Added | docs/plans/Phase-2/Phase-2A-LDAP-Failure-Classification.md |
| Added | src/LabAuthServer.Application/DTOs/DirectoryFailure.cs |
| Added | src/LabAuthServer.Application/DTOs/GroupLookupResult.cs |
| Added | src/LabAuthServer.Application/Enums/DirectoryDiagnosticSource.cs |
| Added | src/LabAuthServer.Application/Enums/DirectoryFailureReason.cs |
| Added | src/LabAuthServer.Application/Enums/DirectoryFailureStage.cs |
| Added | src/LabAuthServer.Application/Interfaces/AuthenticationResult.cs |
| Added | src/LabAuthServer.Infrastructure/Services/ILdapConnection.cs |
| Added | src/LabAuthServer.Infrastructure/Services/ILdapConnectionFactory.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapConnectionFactory.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapDistinguishedNameParser.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapFailureClassifier.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapFailureLogging.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapResponseValidationException.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapResponseValidator.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapSearchEntry.cs |
| Added | src/LabAuthServer.Infrastructure/Services/LdapSearchResult.cs |
| Added | tests/LabAuthServer.IntegrationTests/LdapFailureHttpTests.cs |
| Added | tests/LabAuthServer.UnitTests/DirectoryResultTests.cs |
| Added | tests/LabAuthServer.UnitTests/LdapFailureClassificationTests.cs |

Final git diff --check passes (exit 0); new files also pass a trailing-whitespace scan. Git emits Windows LF/CRLF conversion notices without remaining whitespace errors. The final check corrected one extra blank line at the end of IAuthenticationService.cs.

All 73 pre-existing source files outside the nine approved existing LDAP/API files match their pre-edit hashes. This includes Program.cs, all settings, JWT/signing/key code, completed request/security middleware, LDAP options/timeout validation, DPAPI provider and audit persistence. No package/project/dependency file changed. Current source HEAD remains 6793324365d860085080ab53bb7ccdb3b4401c28. The worktree is intentionally dirty with preserved earlier work and this implementation; nothing was staged, committed or pushed. Full status and integrity results are retained as git-after.txt and final-integrity.json in the evidence directory.

No unresolved source build/test failures remain. Compatibility edges and deferred work are described above: stricter directory completeness/DN validation, retained no-role 500, best-effort audit and existing cancellation limitations. No production acceptance is claimed.
