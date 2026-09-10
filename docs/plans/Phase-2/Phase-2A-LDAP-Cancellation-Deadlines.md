# Phase 2A cooperative LDAP cancellation and authentication deadlines

2026-09-10: **IMPLEMENTED, TESTED, SOURCE ONLY.** The cooperative contract is approved and implemented on top of the existing [typed LDAP failure classification](Phase-2A-LDAP-Failure-Classification.md). The earlier design gate is resolved by explicit approval of cooperative containment. No production deployment or infrastructure acceptance is claimed.

**The application guarantees logical cancellation/deadline containment, not immediate interruption of already-running synchronous native LDAP work.**

## Architectural decision and provider limitation

The original requirement demanded that no native LDAP operation continue beyond cancellation/deadline. The existing System.DirectoryServices.Protocols 10.0.0 Windows Negotiate bind uses synchronous ldap_bind_s. It has no supported guaranteed immediate abort mechanism. Connection disposal does not establish that a running bind has terminated. See the [matching Windows provider implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.DirectoryServices.Protocols/src/System/DirectoryServices/Protocols/ldap/LdapConnection.Windows.cs#L58) and [native bind documentation](https://learn.microsoft.com/en-us/windows/win32/api/winldap/nf-winldap-ldap_bind_s).

The approved replacement contract retains ownership of in-flight work, awaits its completion and cleanup, rejects late results, and stops subsequent authentication stages. Cancellation callbacks no longer dispose LDAP connections from another thread. The existing awaited Task.Run bridge for synchronous LDAP remains; it is not an interruption mechanism. No detached task, worker queue, background cleanup, retry, thread abortion, new package or provider/authentication-mode change was introduced.

An in-flight native call can outlast the application deadline. The deadline is a decision budget, not a hard HTTP response-time bound. ConnectionTimeout remains unchanged and is not advertised as a total bind/login timeout. No live native interruption experiment or real LDAP test was performed.

## One operation and one budget

Application.Services.AuthenticationOperation owns the caller registration, one timer, one cancellation signal, a monotonic TimeProvider timestamp, active stage and immutable DirectoryFailure once interrupted. It is a small application context using the existing result/failure architecture; it does not carry LDAP exceptions or HTTP details.

AuthController creates one context after HTTPS/body validation and passes it through IAuthenticationService, ILdapAuthenticationClient and ILdapService. Identity and membership use the same deadline; group lookup does not reset it. The combined token reaches credential loading, the awaited LDAP scheduling boundary, role mapping and token issuance.

The existing CancellationToken arguments remain. An optional AuthenticationOperation argument explicitly carries origin and stage across the interfaces. With a supplied context, that context is authoritative and its creator owns disposal. Callers should pass its Token for delegated work. Standalone authentication/client/group entry points create and dispose their own bounded context when none is supplied. Implementations/fakes of these interfaces must adopt the added optional parameter; existing invocation sites that only pass CancellationToken remain source-compatible. A completed context cannot start more work.

Root DSE remains a separate diagnostic operation and does not participate in login orchestration. Its implementation and earlier cancellation limitations are unchanged.

## Configuration

| Setting | Behavior |
| --- | --- |
| ActiveDirectory:AuthenticationTimeout | New application decision budget; default and source example 00:00:30. |
| Valid range | Inclusive 1 through 60 seconds, including fractional values within those bounds. Zero, negative, unlimited and out-of-range values are rejected. |
| Validation | LdapOptionsValidator participates in existing ValidateOnStart; AuthenticationOperation also rejects invalid durations when constructed directly. |
| Missing setting | Uses the explicit safe 30-second model default. An invalid configured value is not replaced with that default. |
| Clock | TimeProvider.System in normal operation; tests supply a manual monotonic clock and controllable/inert timers. No additional dependency. |
| Native timeout | Existing ConnectionTimeout remains 10 seconds in source. AuthenticationTimeout does not force a running native bind to stop. |

Only repository appsettings.json gained the new setting. Environment-specific and live configuration files were not edited. SQL, certificates, JWT settings, token budgets, AllowedHosts and ingress/security controls retain their previous values.

## Cancellation origin and exact precedence

All origin selection and stage/completion transitions are serialized by a private lock. This lock protects one operation's state; it is not an LDAP concurrency limiter.

At each observation (caller callback, deadline timer, stage transition, explicit check or completion):

1. If a failure was already recorded, retain the same immutable failure and originating stage.
2. Otherwise compare monotonic elapsed time with the configured timeout. At or beyond the deadline, record Timeout / AuthenticationDeadlineExceeded. Equality is resolved in favor of the deadline, including a delayed timer callback.
3. Before the deadline, a signalled caller token records Cancelled / CallerCancelled. The caller registration runs synchronously when cancellation is delivered, including a caller already cancelled at context creation.
4. If neither source has won, permit the stage transition or successful completion.

Thus caller cancellation recorded before the deadline remains Cancelled even if native work returns much later. Deadline recorded first remains Timeout after later caller cancellation. Simultaneous observations at the deadline become Timeout; subsequent signals cannot change the outcome. The originating exception type or timing of a late native result never determines this choice.

This precedence is defined at application observation/linearization. CancellationToken does not expose the historical timestamp of its signal; the application does not invent one if callback delivery was delayed. Both timer callbacks and explicit elapsed-time checks are used so delayed timer delivery cannot extend the budget. The combined token signals asynchronous consumers; callback exceptions cannot change the recorded failure or escape the timer callback.

## Cooperative stage boundaries and late results

Each LDAP stage is authorized by a cancellation/deadline check. Native work already admitted at that boundary is allowed to finish. After it returns, the authoritative state is checked before accepting the result or entering another stage. Awaited outer boundaries also check after native connection cleanup, preventing a late completion/disposal from passing a stale success upward.

Existing stages retain their enum values: CredentialLoading, ConnectionSetup, ServiceBind, UserSearch, UserBind, GroupSearch and ResponseValidation. RoleMapping and TokenIssuance are appended for the post-LDAP controller boundaries, rather than labelling those failures as LDAP work. AuthenticationDeadlineExceeded is appended to bounded reasons. No existing category was renamed or renumbered.

Cancellation records the active stage once. Group cancellation/deadline preserves credential/setup/service-bind stages when those are the actual origin, instead of always collapsing to GroupSearch. An interruption during native membership search is GroupSearch. Existing non-cancellation group error normalization is retained for compatibility.

A late successful user search cannot start user bind. A late user bind cannot start membership lookup or issue a JWT. A late group result cannot start role mapping or token signing. Cancellation wins over late LDAP 49, timeout exceptions, OperationCanceledException and unrelated exceptions when the context already has an authoritative origin. Validation and membership iteration also have checkpoints.

AuthController checks before/after authentication and groups, before/after mapping, and before signing. It atomically accepts completed login only after token issuance returns within the permitted operation. A signer already admitted before interruption may finish, but its late token is discarded. No complete-login success audit is emitted for a rejected late result. Once successful completion is accepted, audit/HTTP delivery occurs outside the decision budget; disconnects can still prevent delivery.

## Typed results, HTTP and audit

AuthenticationResult, GroupLookupResult and DirectoryFailure remain the existing immutable contracts. Failed membership has no readable Groups value and cannot become an empty membership success. Complete empty membership remains valid, and the existing no-approved-role generic 500 policy is unchanged.

| Outcome | Category / HTTP |
| --- | --- |
| Caller cancellation | Cancelled / 499 where writable |
| Application deadline | Timeout / 504 |
| User-bind LDAP 49 | InvalidCredentials / 401 |
| Service-bind LDAP 49; unavailable/busy 81/51/52 | DirectoryUnavailable / 503 |
| LDAP 85, TimeLimitExceeded, provider timeout | Timeout / 504 |
| Existing operations/protocol/size/admin/malformed failures | ProtocolFailure / 503 |
| Configuration | Configuration / 500 |
| Unexpected | Unexpected / 500 |
| Invalid request | InvalidRequest / 400 |
| Existing rate limiter | 429 |
| Complete login | 200 |

The pre-existing unassociated OperationCanceledException classification remains Unexpected / UnassociatedCancellation when neither caller cancellation nor deadline is established. It is not misrepresented as caller cancellation. All caller-associated cancellation paths use the recorded Cancelled outcome, irrespective of late exception shape.

Public failures retain fixed generic messages, correlation and response headers. No provider messages, exception objects, DNs, filters, service secrets or stack traces are added to responses or expected-failure logs. Deadline/caller outcomes have bounded reasons and no fabricated numeric diagnostic; existing explicitly sourced LDAP diagnostics remain on ordinary provider failures.

Audit mapping remains AUTH_LOGIN_FAILURE for cancellation and AUTH_LDAP_FAILURE for timeout. DetailsJson records bounded category, actual originating stage, reason and explicitly sourced code metadata. AUTH_LOGIN_SUCCESS is still only written after groups, roles and accepted token issuance. Existing username/subject bounds and SQL persistence are unchanged.

Audit continues to use RequestAborted and remains best effort. A cancelled request can prevent durable persistence or a 499 response. Tests verify the deterministic audit attempt and safe handling when that token prevents persistence. This slice does not promise durable audit after disconnect, detach audit work or change the database/schema/events.

## Validation

- Previous full suite: 391 passed (260 unit, 131 integration).
- New full suite: **582 passed (399 unit, 183 integration), 0 failed, 0 skipped**.
- Added tests: **191** (139 unit and 52 integration). No existing test was removed or disabled.
- Targeted LDAP/directory-result/configuration selection: **339 passed** (242 unit, 97 integration), no failures/skips.
- Release build: **0 warnings, 0 errors**. Restore: all projects up to date; no package or project-reference changes.
- Existing JWT/signing, exact size boundaries, authentication/protected endpoints, rate limiting, security headers, correlation, configuration and safe-error regression tests pass in the full suite.
- Evidence: C:\Apps\LabAuthServer\Temp\Phase2A-CooperativeLdap-20260910, including the pre-edit source snapshot/hashes, original status, targeted/full TRX results, build-final.log, tests-final.log and final-integrity.json.

New unit tests cover both interruption origins at every relevant LDAP stage with late success, LDAP 49, timeout, cancellation and unexpected exceptions; before-start cancellation/expiry; direct bind; cancellation-aware asynchronous credential loading; shared identity/group budget; stable origin under repeated/racing signals; delayed timer delivery; completed-context reuse rejection; cleanup ownership; and valid/invalid timeout boundaries. An uncooperative bind fake proves the request continues awaiting the operation and connection disposal occurs only after it returns.

New integration tests use the actual LDAP services with the connection seam, manual time and synthetic credentials. They verify safe 504 responses, writable-controller 499 behavior, correlation/headers, bounded audit fields, lost audit persistence on RequestAborted, no downstream work after failed groups, controller rejection of deliberately non-cooperative service fakes, startup rejection of invalid timeout settings, normal token issuance and protected access, and unchanged empty-group/no-role behavior. Caller-abort tests inspect controller results because a genuinely disconnected HTTP client cannot be relied upon to receive 499. No test requires AD, LDAP, IIS, SQL or network infrastructure.

## Exact file inventory for this increment

Paths are relative to C:\Apps\LabAuthServer\Source\LabAuthServer. Earlier uncommitted work is preserved; this inventory is against the pre-edit snapshot, not HEAD.

| Path | Purpose |
| --- | --- |
| src/LabAuthServer.Application/Services/AuthenticationOperation.cs | New shared deadline/origin/stage context and cooperative signal ownership. |
| src/LabAuthServer.Application/Enums/DirectoryFailureReason.cs | Append bounded authentication-deadline reason. |
| src/LabAuthServer.Application/Enums/DirectoryFailureStage.cs | Append mapping and token-issuance stages. |
| src/LabAuthServer.Application/Interfaces/IAuthenticationService.cs | Carry optional shared operation; document ownership. |
| src/LabAuthServer.Application/Interfaces/ILdapService.cs | Carry shared membership budget; document ownership. |
| src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptions.cs | Explicit safe default for authentication decision timeout. |
| src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptionsValidator.cs | Enforce inclusive 1-60 second startup validation. |
| src/LabAuthServer.Infrastructure/Services/ILdapAuthenticationClient.cs | Carry existing operation into client entry points. |
| src/LabAuthServer.Infrastructure/Services/LdapAuthenticationClient.cs | Cooperative stage checks, authoritative late-result rejection and awaited cleanup; remove disposal callbacks. |
| src/LabAuthServer.Infrastructure/Services/LdapAuthenticationService.cs | Own/join bounded operation and reject late client outcomes. |
| src/LabAuthServer.Infrastructure/Services/LdapService.cs | Same cooperative semantics for groups; preserve cancellation origin stage. |
| src/LabAuthServer.Api/Controllers/AuthController.cs | One complete-login budget, downstream gates, origin-aware error handling and accepted-completion boundary. |
| src/LabAuthServer.Api/appsettings.json | Source-only AuthenticationTimeout example. |
| tests/LabAuthServer.UnitTests/CooperativeLdapTests.cs | New deterministic stage, origin, lifetime, late-success and configuration tests. |
| tests/LabAuthServer.IntegrationTests/CooperativeLdapHttpTests.cs | New service/HTTP/audit/startup and compatibility tests. |
| tests/LabAuthServer.UnitTests/LdapAuthenticationServiceTests.cs | Adapt existing fake signature; retain expectations. |
| tests/LabAuthServer.IntegrationTests/AuthenticationTests.cs | Adapt existing fake signatures; retain expectations. |
| tests/LabAuthServer.IntegrationTests/ApiSecurityResponseTests.cs | Adapt existing fake signature; retain expectations. |
| tests/LabAuthServer.IntegrationTests/JwtSizeBoundaryTests.cs | Adapt existing fake signatures; retain size/security expectations. |
| tests/LabAuthServer.IntegrationTests/LdapFailureHttpTests.cs | Adapt existing fake signatures; retain classification/audit expectations. |
| docs/plans/Phase-2/Phase-2A-LDAP-Cancellation-Deadlines.md | Replace blocked proposal with approved implementation, evidence and limitations. |
| docs/Project_Status.md | Current implementation/test checkpoint. |
| docs/Validation_Status.md | Current evidence and source-only acceptance boundary. |
| docs/Architecture.md | Document shared operation and cooperative provider boundary. |
| docs/Configuration.md | Document new setting and limits. |
| docs/Testing.md | Document new coverage and current totals. |

## Deferred work

| Item | Status and reason |
| --- | --- |
| Native LDAP hard cancellation | DEFERRED: synchronous provider/native implementation offers no safe guaranteed immediate abort. |
| Infrastructure timeout/tuning | DEFERRED: outside application coding scope. |
| IIS acceptance testing | DEFERRED: deployment/operations scope. |
| HTTP.sys tuning | DEFERRED: OS/IIS infrastructure scope. |
| Proxy/forwarding validation | DEFERRED: network/infrastructure scope. |
| HSTS deployment | DEFERRED: deployment/browser policy; remains disabled. |
| Network/WAF/load-balancer validation | DEFERRED: infrastructure scope. |
| Infrastructure capacity/load testing | DEFERRED: operational capacity scope. |
| Operational release review | DEFERRED: deployment approval, monitoring, rollback and release sign-off are outside coding-only scope. |
| Concurrency limits, pools, retries, circuit breakers, queues and background work | DEFERRED: explicitly excluded separate resource/concurrency workstream. |

## Final safety boundary

Final snapshot comparison: 198 baseline files; 23 modified existing files and 3 new files, exactly matching the 26-file inventory. All other 175 baseline files retain their original hashes; no baseline file is missing. No project/dependency file changed. Structural comparison of appsettings confirms every prior value is preserved; only AuthenticationTimeout was added. git diff --check passes, and the staged diff is empty. The worktree is intentionally dirty with the earlier work and this increment.

HEAD remains 6793324365d860085080ab53bb7ccdb3b4401c28. Nothing was staged, committed or pushed. Existing modified/untracked work is retained. JWT/security controls, SQL/certificate settings, source layer/project references and live environment files were not changed by this increment. Only the source appsettings AuthenticationTimeout setting was added. No infrastructure action or live probe was performed.

NO DEPLOYMENT
NO IIS CHANGE
NO HTTP.sys CHANGE
NO SQL CHANGE
NO CERTIFICATE CHANGE
NO RESTART
NO PROXY CHANGE
NO HSTS
NO COMMIT
NO PUSH
