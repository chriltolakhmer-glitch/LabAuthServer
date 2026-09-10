# Phase 2A hard pending LDAP waiter cap

2026-09-10: **IMPLEMENTED AND SOURCE-TESTED. PHASE 2A APPLICATION CODING = CLOSED.** The user explicitly approved ResourceExhausted / ConcurrencyWait / PendingWaiterCapacityExceeded, HTTP 503 and a configurable 16-default, 1-128 cap. This resolves the earlier contract gate and supersedes the prior deferred-cap decision. Closure is application-only, not deployment or operational acceptance.

## Baseline and scope

Branch main; HEAD `6793324365d860085080ab53bb7ccdb3b4401c28`; nothing staged. Previous full suite: 665 passed, zero failures/skips; Release zero warnings/errors. Inspected gate, shared AuthenticationOperation, options/startup validation, singleton DI registration, controller results/audit, and existing concurrency/cooperative tests. Prior modified/untracked files were snapshotted before edits and preserved.

Only application source, tests and documentation changed. No dependencies, infrastructure, deployment or live configuration actions occurred. The approved new source option is the only appsettings change.

## Configuration and counted resource

| Setting | Behavior |
| --- | --- |
| ActiveDirectory:MaxPendingLdapWaiters | Default and source example 16; inclusive integer range 1-128. |
| Validation | LdapOptionsValidator participates in the existing ValidateOnStart registration; gate construction also rejects invalid values before semaphore allocation. Missing setting uses 16. No unlimited value. |
| Active work | Existing MaxConcurrentLdapOperations: default 4, range 1-32, unchanged. |
| Decision deadline | Existing AuthenticationTimeout: default 30 seconds, range 1-60, unchanged. |
| Scope | One gate/cap per application service provider, fixed for that singleton lifetime. |

The cap bounds admission to pending SemaphoreSlim waits for the current login LDAP phase. It does not count active permit holders, requests before this admission point, completed waits, or downstream mapping/signing/audit. It does not cap request rate, all retained HTTP requests, total process memory or enterprise-wide LDAP capacity.

## Atomic admission and bounded tracking

LdapConcurrencyLimiter retains its existing SemaphoreSlim and adds one short admission lock and a bounded HashSet of incomplete wait tasks. This set tracks reservations; it does not schedule work or define a second queue. Its entries never exceed the configured pending cap.

1. Enter the existing ConcurrencyWait stage and observe the operation's existing cancellation/deadline state.
2. Under the admission lock, check the operation again and remove completed waits from tracking.
3. If semaphore CurrentCount is positive, acquire immediately without pending registration. All acquisitions use this lock; outside releases can only add availability, so another caller cannot steal that observed available count through a concurrent acquisition.
4. Otherwise, if pending capacity is full, observe cancellation/deadline again and return the approved typed failure before calling WaitAsync. There is no semaphore permit or pending reservation for that rejected request.
5. If pending capacity remains, call the existing WaitAsync(operation.Token) while still holding the lock and register its task only if it remains incomplete. Check and registration cannot race with another admission.
6. Await the wait outside the lock. In finally, remove its reservation. On successful wait completion, recheck the authoritative operation before exposing the lease; return any late permit on interruption.

No lock spans an await, native call, audit, mapping or signing. No synchronous Wait/Result/sleep, new timer, retry, channel, thread pool, background worker, detached LDAP task or distributed coordination was added.

## Cancellation, completion and cleanup

The pending reservation lasts while the existing semaphore wait is incomplete, including its cancellation cleanup. It is not released merely when the cancellation token is signalled, because a still-registered semaphore wait must not be replaced without a bound. Once the semaphore wait completes by acquisition/cancellation/failure, its request's finally removes it. Every new admission also removes completed wait tasks before counting capacity, so a delayed request continuation cannot indefinitely retain pending capacity. An internal count accessor uses the same cleanup for deterministic tests; no count is exposed to clients or logs.

There is no independent integer to decrement twice or make negative. HashSet removal is idempotent even when admission cleanup already removed a completed entry. Failure before wait registration creates no reservation; an exception from the await still executes finally. The set can retain at most the cap's completed references while idle; they are removed on the next admission or count inspection and cannot accumulate across arrivals.

Caller cancellation remains Cancelled/499 where writable. Deadline remains Timeout/504. First recorded origin persists; deadline wins at equality. The same AuthenticationOperation and token drive waiting; no new deadline mechanism or reset exists. Checks before selecting capacity failure prevent an already-observed caller/deadline outcome from being mislabeled ResourceExhausted. Capacity failure is selected at that admission decision; a signal arriving after a returned typed capacity rejection does not turn it into a new wait or successful admission. As with existing stage checks, this is an application observation boundary, not a claim of historical token-signal timestamps.

An internally delivered permit after expiry/cancellation is returned before a usable success result is exposed. A cancelled/timed-out wait never rejoins on later release. Acquired permits are no longer pending; the controller retains ownership through identity, membership, native completion and cleanup. Existing Interlocked lease disposal plus the result's idempotent Dispose prevents double release. Role mapping, signing and audit run only after LDAP permit release, as before.

## Typed outcome, HTTP and audit

The existing failure enum appends ResourceExhausted=9; existing numeric values remain unchanged. The existing bounded reason enum appends PendingWaiterCapacityExceeded. ConcurrencyWait is reused. No directory-library diagnostic code is fabricated.

The Application LdapAdmissionResult follows the existing typed-result pattern: either a privately owned disposable lease or DirectoryFailure, never both. The constructor is private; success/failure factories reject null. Its failure state is immutable and Dispose is safe on both outcomes. ILdapConcurrencyLimiter now returns this typed result instead of a bare IDisposable. AuthController checks it before invoking authentication and uses its existing DirectoryFailureAsync response/audit mechanism. Cancellation exceptions continue through the existing authoritative-operation failure path.

| Outcome | Mapping |
| --- | --- |
| Full pending cap | ResourceExhausted / ConcurrencyWait / PendingWaiterCapacityExceeded |
| HTTP | 503, independently mapped from DirectoryUnavailable |
| Client detail | Fixed `Authentication service unavailable.` with existing ProblemDetails/correlation/header policy |
| Diagnostic | Code null; source None |
| Audit event | Existing AUTH_LDAP_FAILURE, Success=false, status 503 |
| Audit details | Existing bounded category/stage/reason/source/code fields and existing identityOmitted flag |

No pending count, configured capacity, semaphore/queue state, exception message, credentials, filter, DN or hostname is added to the response or diagnostic details. Existing ordinary audit identity fields retain their approved semantics. Resource rejection invokes no authentication service, LDAP connection/bind/search, groups, role mapping, signer or success audit. No SQL schema, event catalog or persistence redesign occurred. RequestAborted still governs best-effort audit and can prevent persistence/delivery after disconnect.

InvalidCredentials remains 401; DirectoryUnavailable and ProtocolFailure remain 503; Timeout remains 504; Cancelled remains 499 where writable; Configuration/Unexpected remain 500. Existing no-approved-role behavior remains generic 500 with no token; complete empty membership remains separate from failed lookup.

## Native work, fairness and instance limits

The cooperative native LDAP contract is unchanged. Cancellation or expiry may make an outcome authoritative while synchronous native work continues. The permit stays occupied until the protected work and cleanup actually finish. Late results cannot advance authentication; the cap does not abort native calls or reclaim active permits early.

No FIFO fairness or starvation freedom is promised. Semaphore scheduling and request execution determine ordering. A waiter can expire before receiving a permit. Each application instance has independent ingress, active and pending limits; two instances with default active=4/pending=16 do not share a global 4/16 quota. Raw internal LDAP helpers still require explicit admission if exposed by future endpoints/jobs; this gate covers the current public login orchestration.

## Deterministic validation and combined model

New unit tests cover defaults and all configuration boundaries, exact pending capacity, immediate excess failure, separate active/pending accounting, immutable result/lease disposal, pre-admission cancellation/deadline with a full cap, five origin orderings over repeated cycles, delayed timer handoff, capacity recovery, and simultaneous admission at caps 1/2/16/128. Active limits 1/4/32 remain separate. Pending tasks are explicitly held by occupied permits; manual clocks and explicit cancellation control expiry. No sleeps or production load testing.

The combined in-memory HTTP test uses the real DI gate with default four active permits, configured two pending waits, default thirty-second decision budget and the unchanged ten-per-fixed-minute ingress policy. Four permits are deliberately held through the test seam. Two requests wait; eight further admitted requests receive generic correlated 503 capacity failures with zero downstream calls; the eleventh HTTP attempt is the existing 429 and does not reach the gate. Advancing the manual clock gives the two pending requests 504, with bounded valid audit; releasing holders proves reusable capacity. This is a controlled correctness test, not evidence of infrastructure throughput or memory capacity.

Startup integration tests use the real AddActiveDirectoryOptions/ValidateOnStart registration in a directly owned HostBuilder. Audit assertions use production AuditEventValidator; counters/log records remain outside caught exceptions. Existing provider/cooperative tests still cover LDAP failure classification, late native results, actual connection cleanup, group-failure containment, successful issuance and protected endpoints.

| Run | Result |
| --- | --- |
| Existing baseline after typed-result wiring | 665 passed: 436 unit + 229 integration |
| New waiter-cap tests | 35 passed: 29 unit + 6 integration |
| Existing concurrency selection | 64 passed: 25 unit + 39 integration |
| Targeted LDAP/directory-result/cooperative selection including new tests | 424 passed: 296 unit + 128 integration |
| Full solution | **700 passed: 465 unit + 235 integration; 0 failed, 0 skipped** |
| Release build | **0 warnings, 0 errors** |

The first new integration compilation exposed an int/nullable-short assertion mismatch; it was corrected in the new test. No production workaround or existing expectation was weakened. The existing 24-contender active-concurrency test now explicitly configures pending capacity 24 so it continues testing all 24 successful admissions; it also asserts typed admission success. Existing release probes assert typed success so a completed capacity rejection cannot masquerade as an available permit. All other prior assertions remain.

Evidence: `C:\Apps\LabAuthServer\Temp\Phase2A-HardWaiterCap-20260910`, including pre-edit copies/hashes/status, baseline/focused/full TRXs, build logs, increment diff and integrity report. No native/IIS/SQL/live AD test was run.

## Exact increment inventory

| Repository-relative file | Purpose |
| --- | --- |
| src/LabAuthServer.Application/DTOs/LdapAdmissionResult.cs | New typed success/failure result and idempotent lease ownership. |
| src/LabAuthServer.Application/DTOs/DirectoryFailure.cs | Fixed generic message for ResourceExhausted. |
| src/LabAuthServer.Application/Enums/AuthenticationFailureCategory.cs | Append ResourceExhausted without renumbering. |
| src/LabAuthServer.Application/Enums/DirectoryFailureReason.cs | Append bounded pending-cap reason. |
| src/LabAuthServer.Application/Interfaces/ILdapConcurrencyLimiter.cs | Typed admission result contract. |
| src/LabAuthServer.Infrastructure/Services/LdapConcurrencyLimiter.cs | Atomic bounded wait registration/cleanup and typed rejection. |
| src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptions.cs | Default pending cap 16. |
| src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptionsValidator.cs | Validate inclusive 1-128 at startup. |
| src/LabAuthServer.Api/Controllers/AuthController.cs | Stop on admission failure and map ResourceExhausted independently to 503. |
| src/LabAuthServer.Api/appsettings.json | Source-only MaxPendingLdapWaiters=16. |
| tests/LabAuthServer.UnitTests/LdapPendingWaiterTests.cs | New deterministic admission/configuration/race/cleanup tests. |
| tests/LabAuthServer.IntegrationTests/LdapPendingWaiterHttpTests.cs | New startup and combined HTTP/audit/security test. |
| tests/LabAuthServer.UnitTests/LdapConcurrencyLimiterTests.cs | Preserve 24-contender test scope with explicit pending capacity and success assertion. |
| tests/LabAuthServer.IntegrationTests/LdapConcurrencyTests.cs | Adapt observer signature; assert successful permit release probe. |
| docs/plans/Phase-2/Phase-2A-Hard-Pending-Waiter-Cap.md | This implementation and closure record. |
| docs/plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md | Supersede deferred/blocked state with approved implementation. |
| docs/plans/Phase-2/Phase-2A-Final-Application-Code-Review.md | Final application-only closure and interaction review. |
| docs/plans/Phase-2/Phase-2A-Hard-Waiter-Cap-Contract-Gate.md | Mark prior contract gate resolved by explicit approval. |
| docs/Project_Status.md | Current implementation/closure and totals. |
| docs/Validation_Status.md | Current evidence and acceptance boundary. |
| docs/Testing.md | New tests, totals and proof limits. |
| docs/Configuration.md | Approved option, validation, response and instance scope. |
| docs/Architecture.md | Typed bounded admission in existing orchestration. |

## Final application-only regression review

The changed path is confined to pre-LDAP admission, its typed failure mapping, bounded diagnostics and configuration. Existing JWT/RSA signing and validation, 12288/12352/7680 budgets, 8192-byte body/16128-byte envelope, AllowedHosts, no-store/nosniff/DENY, LDAP classification, first-origin/deadline-equality semantics, native permit ownership, group failure/role mapping/token containment, prior rotation/configuration/audit fixes and middleware ordering remain intact. No pending rejection can reach authentication or signing. Source settings differ only by the approved cap property. No unresolved P0/P1/P2 defect was identified in the cap interaction review.

**PHASE 2A APPLICATION CODING = CLOSED.** This does not approve deployment, real IIS acceptance or operational release. HEAD and staged state remain unchanged and all unrelated prior work is preserved.

## Skipped non-coding work

| Item | Why skipped / status |
| --- | --- |
| Proxy/forwarding validation | DEFERRED: infrastructure/network topology and trust. |
| HSTS deployment | DEFERRED: browser/HTTPS policy and deployment risk; remains disabled. |
| Real IIS acceptance for Security Slice 2 | DEFERRED: deployment/operations. |
| HTTP.sys tuning | DEFERRED: OS/IIS infrastructure. |
| Network/WAF/load-balancer validation | DEFERRED: infrastructure. |
| Infrastructure capacity/production load tests | DEFERRED: operational capacity, not deterministic application testing. |
| Final operational release review | DEFERRED: deployment/change management/rollback/sign-off. |
| Native LDAP hard cancellation | DEFERRED: synchronous Windows provider limitation; no forced-abort redesign. |
| Distributed/global LDAP limits | DEFERRED: separate architecture/infrastructure concern. |

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
