# Phase 2A LDAP concurrency and resource protection

## Latest required hard-cap slice (2026-09-10)

**IMPLEMENTED AND SOURCE-TESTED.** Explicit approval resolved the ResourceExhausted contract. MaxPendingLdapWaiters defaults to 16 and validates 1-128; one admission lock bounds incomplete semaphore waits independently of active permits. Full capacity returns ResourceExhausted / ConcurrencyWait / PendingWaiterCapacityExceeded through the existing generic 503/audit path, before LDAP work. Cancellation, deadline, native ownership, fairness and instance limitations are preserved. See [algorithm, cleanup proof, tests and exact inventory](Phase-2A-Hard-Pending-Waiter-Cap.md). Current full suite: 700 passed, zero failures/skips; Release zero warnings/errors. The older deferred decisions below are superseded history.

2026-09-10: **IMPLEMENTED, TESTED, SOURCE ONLY** for the existing public login path. This builds on the completed [cooperative cancellation/deadline contract](Phase-2A-LDAP-Cancellation-Deadlines.md) and [typed LDAP failure classification](Phase-2A-LDAP-Failure-Classification.md).

**The concurrency limit bounds application-level concurrent LDAP work. It does not forcibly terminate an already-running synchronous native LDAP operation.**

## Baseline and inspection

HEAD: 6793324365d860085080ab53bb7ccdb3b4401c28. Initial suite: 582 passed (399 unit, 183 integration), zero failures/skips; targeted LDAP/configuration suite 339; Release zero warnings/errors. The pre-edit snapshot contains 201 files. Nothing was staged; earlier tracked modifications and untracked work were preserved.

Inspection found no existing LDAP concurrency gate. AuthController.Login is the only public LDAP entry point. Its flow is IAuthenticationService/LdapAuthenticationService -> ILdapAuthenticationClient/LdapAuthenticationClient -> service credential loading and synchronous connection/service bind/user search/user bind; successful identity is followed by ILdapService/LdapService membership lookup. Both paths await native work and connection disposal. Health is application liveness, not a Root DSE query. The direct BindAsync and Root DSE diagnostic helpers are not public endpoints.

The existing Login rate policy is one application-wide constant partition, 10 requests per minute, QueueLimit=0, returning 429 before the controller. There is no per-user/address partition growth. AuthenticationOperation already owns a 30-second default, 1-60 second validated decision budget and records the authoritative cancellation origin.

## Protected resource and chosen scope

One permit covers the login's complete LDAP phase: authentication service invocation, credential loading, identity connection/binds/search/validation/cleanup, then membership connection/bind/search/validation/cleanup. The two connections are sequential; the same permit protects both. There is no nested acquisition or permit per bind/search.

This scope protects the expensive synchronous work without serializing role mapping, JWT signing, audit persistence, health or protected-resource requests. On success, the permit is returned immediately after membership lookup and cleanup, before role mapping. On typed failures it is returned before audit. On exceptions/cancellation it is returned after the protected awaited operation unwinds, before failure handling outside that phase.

Admission belongs to AuthController, which already owns the combined identity/group login orchestration. ILdapConcurrencyLimiter is the Application contract and LdapConcurrencyLimiter is the Infrastructure implementation. It is injected as a required controller dependency. There is no per-request fallback limiter or static mutable global state.

**Scope boundary:** this bounds LDAP work initiated through the current public login endpoint. Low-level authentication/client/group interfaces remain internal building blocks; directly calling them does not automatically acquire this orchestration permit. A future endpoint, scheduled job or diagnostic exposed to callers must adopt the shared admission scope and an appropriate arrival/waiter policy before using those helpers. Root DSE is unchanged and is not included in this login gate. The limiter is not represented as a universal interception layer around every possible LDAP library call.

## Configuration and lifetime

| Property | Value / rule |
| --- | --- |
| Setting | ActiveDirectory:MaxConcurrentLdapOperations |
| Default and source example | 4 |
| Minimum | 1 |
| Maximum | 32 |
| Invalid values | Startup validation fails; zero, negative, above-32 and integer-extreme values are rejected before semaphore allocation. |
| Primitive | SemaphoreSlim(limit, limit), asynchronous WaitAsync with the existing operation token. |
| Lifetime | DI singleton per application service provider; shared across controllers/request scopes. |
| Configuration lifetime | Resolved for the singleton lifetime; dynamic resizing is not implemented. |
| Disposal | Root provider owns the limiter; ordinary shutdown must drain request work before singleton disposal. Forced shutdown/native interruption remains an operational/provider limitation. |

Four is a conservative application default for the existing low admission rate and blocking LDAP provider. Thirty-two is a finite safety ceiling, not a load-tested capacity recommendation. This is not an enterprise-wide or distributed directory-capacity limit. Separate application instances have separate gates.

Only source appsettings.json gains MaxConcurrentLdapOperations. AuthenticationTimeout remains 30 seconds with its unchanged inclusive 1-60 second validation. Existing ConnectionTimeout, all environment-specific/live settings, SQL, JWT, certificates and ingress/security values are preserved.

## Acquisition, waiting and precedence

AuthenticationOperation is created before acquisition. AcquireAsync enters the appended ConcurrencyWait stage, awaits SemaphoreSlim.WaitAsync(operation.Token), and re-checks the authoritative operation state before returning a usable lease. No second timeout, CancelAfter source or error architecture is introduced.

At capacity, the request waits using its remaining authentication budget. Caller cancellation produces Cancelled / CallerCancelled; deadline expiry produces Timeout / AuthenticationDeadlineExceeded. Existing first-recorded-origin semantics and deadline-wins-at-equality behavior are unchanged. Waiting does not restart the budget before authentication or groups.

If cancellation/deadline wins during permit handoff, an internal semaphore permit can transiently be delivered; the post-acquisition check returns it before exposing a lease or starting LDAP. A cancelled/expired waiter cannot proceed into authentication later when capacity becomes available. If WaitAsync fails before acquisition, no permit is released by that waiter.

After admission, the controller enters CredentialLoading and the existing services continue their precise stage tracking. Cancellation during ServiceBind, UserSearch, UserBind, GroupSearch or ResponseValidation retains that native/application stage. ConcurrencyWait is only the admission stage; existing stage values were not renumbered.

| Outcome | Public status and existing audit mapping |
| --- | --- |
| Caller cancellation while waiting | 499 where writable; AUTH_LOGIN_FAILURE |
| Deadline while waiting | 504; AUTH_LDAP_FAILURE |
| Ordinary provider/input failures after admission | Existing 400/401/503/504/500 mappings unchanged |
| Existing ingress rate policy rejects arrival | Existing 429; no concurrency acquisition or LDAP call |
| Complete login | Existing 200, with success audit only after token issuance |

## Waiting-resource assessment and compatibility limit

No separate application/native-work queue or queue timeout was introduced. Semaphore waiters are attached to admitted login requests and have the existing finite cancellation/deadline budget. Under the current sole public entry point, arrivals are constrained by the shared 10-per-minute, no-ingress-queue policy. With timely request/timer processing, a 60-second waiting horizon can span two fixed windows (up to 20 arrivals), rather than retaining waiters indefinitely as new windows pass. In-flight native holders can exceed that horizon but remain capped by the permit limit.

**This is not a hard cardinality cap on the semaphore's waiter list.** SemaphoreSlim itself has no configured waiter maximum, and delayed scheduling/cancellation callbacks can delay waiter removal. The current admission rate and finite budgets make asynchronous waiting appropriate for this inspected login path; no claim is made that arbitrary internal callers or future unthrottled paths inherit a fixed waiting-request bound. A hard pending-waiter cap and its overflow response require a separately approved bounded-admission/rejection policy. That policy is recorded as future work rather than silently adding a queue, new resource-exhaustion category or alternate 429 behavior. Revisit this assessment before increasing/removing the existing ingress limit or adding LDAP callers.

## Historical Pending Waiter Cap Decision (superseded by approved implementation)

2026-09-10 follow-up: **REMAINS DEFERRED (Outcome A).** No application code, configuration, public error contract or tests are changed in this follow-up. The existing deterministic tests already cover the relevant behavior. A further limiter is not justified merely by the absence of a hard semaphore queue limit.

Hard pending-waiter cap remains deferred because the existing ingress admission, bounded LDAP concurrency, and bounded authentication deadline already provide application-level bounds under the current architecture.

This decision is conditional on the current sole public login route and normally progressing request scheduling and cancellation cleanup. It is not a claim of a hard memory/cardinality bound under arbitrary scheduler stalls. The distinction is material: the source does not enforce a maximum number of pending semaphore nodes.

### Arrival and waiting model

Program.cs uses one constant `login` partition: ten admissions per fixed 60-second window, queue zero, rejected arrivals receive the existing 429. It is shared across identities, addresses and requests in one application instance, not ten per user or IP. Routing and body/header guards precede rate limiting; rate limiting precedes authentication middleware, controller/model binding and LDAP acquisition. The controller's Login endpoint explicitly enables that policy. Failed and successful admitted requests both consume window admissions; completion does not replenish the fixed-window quota. Health/protected routes do not invoke LDAP.

Let R=10, W=60 seconds, C=4 by default (validated 1-32), and D=30 seconds by default (validated 1-60). A duration T>0 can intersect at most `ceil(T/W)+1` fixed windows, giving the conservative arrival envelope `R * (ceil(T/W)+1)`. Ten can arrive just before a reset and ten just after: 10/minute is not an evenly spaced arrival rate or a ten-per-rolling-minute bound. Over repeated complete windows the admitted rate is ten per minute; cumulative admissions over unlimited uptime are not bounded.

With prompt progression from ingress admission to the controller and timely deadline removal, live pending waiters come from at most the last D seconds of admissions. For every allowed D this yields **at most 20 pending candidates**, including the default 30 seconds. This is a conservative upper envelope, not a measured queue size. Do not subtract C from 20 in general: all C permits may be occupied by older native operations, leaving all recent admissions waiting. Active LDAP holders remain at most C; a conservative normal-progress holder-plus-waiter envelope is C+20 (24 at the default). Neither number bounds all HTTP requests, audit work or application memory.

The authentication clock starts in the controller, not at ingress admission. Arbitrarily delayed downstream scheduling/model binding can bunch admissions from older windows into later controller starts. Likewise, delayed timer/cancellation continuations can retain expired waiting tasks. If a maximum ingress-to-controller delay S and removal delay L were established, a conservative envelope could instead use the arrival interval D+S+L. The source establishes neither finite S nor L, so **no unconditional numeric pending-request bound is claimed**. RequestAborted helps on disconnect; inspected application code establishes no independent hard server request-lifetime limit. A cap here would not bound requests still upstream of the controller or waiting for post-LDAP audit.

### Behavior, resources and races

With C=1 and one occupied permit, each further admitted login asynchronously waits without starting credential loading, connection creation, bind or search. Multiple waiters share the same singleton semaphore, each with its own existing authentication operation. A released permit allows an eligible waiter to proceed through identity and membership; the budget is not restarted. With several slow native operations, at most C holders continue; new waiting requests still expire independently. Even indefinitely occupied native permits do not themselves prevent pending deadlines from completing when the scheduler makes progress.

Before waiting, EnterStage observes cancellation/elapsed time. During waiting, the combined token cancels WaitAsync; caller cancellation maps to 499 where writable, deadline to 504. First recorded origin remains authoritative and deadline wins at equality. At handoff the operation is checked again: an internally delivered permit after expiry/cancellation is returned without exposing a usable lease or starting LDAP. A completed cancelled waiter cannot rejoin when capacity returns. Cancellation after a successful admission check remains cooperative and is checked at subsequent stage boundaries.

There is no explicit application waiter counter to decrement or leak. WaitAsync cancellation and completed acquisition end that pending wait; request objects may remain until continuations, error/audit handling and garbage collection finish. Repeated cancellation is safe. WaitAsync failure releases no permit; post-acquisition rejection releases its acquired permit; lease disposal uses Interlocked.Exchange for exactly-once release. Tests verify capacity remains reusable across cancellation, deadline, exceptions and repeated cleanup. No FIFO or starvation-freedom guarantee is made by this design; no custom fairness queue is added. A request can time out before receiving capacity.

Pending work retains an HTTP context/request DTO (including submitted credentials), scoped services, async state/tasks, semaphore wait state, timer and cancellation registrations. Async waiting does not dedicate a blocked native worker or LDAP connection per waiter. Per-request body/header controls bound their respective inputs, not total retained memory. Exact allocation sizes, peak memory, practical significance and capacity have not been measured; no production measurements are invented. Native holders can retain worker/connection resources beyond D, but only within C for this route. Their permits remain occupied until awaited native work and cleanup finish; late results never authorize further stages.

### Scope, reconsideration and rejection contract

Each DI service provider owns its gate and ingress partition. Two instances with C=4 can run eight protected LDAP phases, not four globally; each has its own 10/minute admission and conditional waiting envelope. Direct internal helpers do not automatically acquire the controller gate or ingress quota. No distributed coordination is added.

Reconsider before increasing/removing ingress admission, increasing the deadline range, adding LDAP endpoints/background callers, introducing materially delayed work between ingress and controller, changing to per-identity/IP quota partitions, or requiring strict pending cardinality despite scheduling delays. Evidence of unacceptable retained-resource pressure would also justify review. The current source and deterministic evidence do not establish such a new workload or requirement; scheduler-delay limitations alone do not establish measured exhaustion under this low-volume route.

If reconsidered, approve overflow semantics before implementation. Existing 429 is specifically the upstream rate-policy response, not a typed LDAP resource-exhaustion result. InvalidCredentials, DirectoryUnavailable, Timeout before actual expiry, Configuration and Unexpected would misdescribe an ordinary full waiter cap. Reusing HTTP 429 would still require a deliberate controller/admission result and safe audit/message contract; it is not authorized implicitly by the existing rate limiter. No new category, status behavior, cap value or configuration is introduced here.

### Follow-up evidence and exact inventory

Existing tests explicitly cover exact capacity, concurrent/repeated release, all cancellation/deadline orderings, delayed timer handoff, repeated interrupted waiters, native/cleanup retention, singleton DI sharing, typed failures and safe HTTP/audit behavior. `HttpContention_Preserves504CorrelationHeaders_AndExisting429Admission` admits ten blocked logins, rejects the eleventh before gate/authentication invocation, expires all ten using manual time and proves capacity reuse. It proves a single-window scenario; the two-window envelope above is arithmetic from the fixed-window policy, not a claimed executed multi-window test. No redundant or artificial tests were added.

Follow-up validation: previous and current full suite **646 passed** (424 unit, 222 integration), zero added tests, failures or skips. Targeted concurrency **64 passed**; targeted LDAP/directory-result/cooperative selection **389 passed** (267 unit, 122 integration). This follow-up filter is `FullyQualifiedName~Ldap|FullyQualifiedName~DirectoryResult|FullyQualifiedName~Cooperative`; the historical broader 403-test selection remains covered by the full suite. Full-suite security and architecture-related regression coverage remains included; no standalone architecture project exists. Release build: **0 warnings, 0 errors**. Evidence: `C:\Apps\LabAuthServer\Temp\Phase2A-PendingWaiterDecision-20260910` (baseline file hashes, original document, TRX runs, build log and final integrity comparison).

Exact repository change for this follow-up: only `docs/plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md`, adding this decision and evidence. All existing source/configuration/tests and earlier dirty work are preserved. HEAD remains `6793324365d860085080ab53bb7ccdb3b4401c28`; nothing staged, committed or pushed. The earlier increment inventory below is historical, not additional changes in this follow-up.

The deferred-work table below continues to apply: native hard cancellation lacks a supported guaranteed provider abort; IIS acceptance requires deployment; HTTP.sys is OS/IIS tuning; proxy/forwarding and network/WAF/load-balancer validation require infrastructure; HSTS is browser/deployment policy and stays disabled; infrastructure capacity and production load tests require operational workloads; operational release review requires change management and sign-off. Distributed/global LDAP coordination remains deferred as architecture/infrastructure work. No application workaround or live configuration change is made for any of these items.

## Release and native work ownership

The returned lease uses Interlocked.Exchange to return the permit exactly once. The controller uses a using scope as the exception-safe fallback and explicit early Dispose calls at the end of LDAP work. Repeated Dispose calls do not increase capacity. No permit is released when acquisition never succeeded.

Successful authentication plus groups releases before mapping/signing. Identity failure releases before its audit. Group failure releases before its audit and cannot reach mapping/signing. Exceptions unwind the using scope; role/token failures occur after the LDAP permit has already been returned. Existing successful empty membership and the no-approved-role generic 500 policy remain distinct from failed membership.

When synchronous native work outlasts cancellation/deadline, the permit remains held until that operation and its cleanup actually return. The holder is not cancelled by the semaphore and has no token-triggered lease disposal. Existing service checks reject late results and stop subsequent stages. There is no abandoned native task, background release/cleanup or forced thread/connection abortion.

The inherited awaited Task.Run LDAP bridge and synchronous provider behavior are unchanged. Slot acquisition introduces no synchronous Wait, Result or Thread.Sleep. Test-only blocking primitives deliberately model uncooperative native work.

## Audit and disclosure

Existing DirectoryFailure, safe ProblemDetails, correlation and bounded structured logging are retained. Admission failures carry ConcurrencyWait plus the existing cancellation/deadline reason; no invented diagnostic code is added. Provider failures retain explicitly sourced diagnostics. Expected failures do not expose raw exceptions, DNs, LDAP filters, passwords, service-account details or stack traces.

The permit is not held for audit persistence. Audit still uses RequestAborted and remains best effort; a disconnected request may not persist its failure or receive 499. No SQL, event-code, schema or audit-delivery redesign was performed.

## Validation and evidence

- Existing baseline re-run after wiring: **582 passed**, zero failures/skips.
- New concurrency/resource tests: **64 passed** (25 unit, 39 integration).
- Targeted LDAP/directory-result/configuration selection: **403 passed** (267 unit, 136 integration).
- Final full suite: **646 passed** (424 unit, 222 integration), **0 failed, 0 skipped**.
- Release build: **0 warnings, 0 errors**. Restore: all projects up to date; no package/project-reference changes.
- Existing security/architecture-related coverage passes in the full suite; no standalone architecture-test project exists.

Unit tests cover default/configured/invalid limits, exact capacity and waiting, cancellation/deadline/equality/precedence, delayed-timer handoff rejection, repeated cancelled waiters, idempotent concurrent lease disposal, controlled contention, and reusable capacity. Integration tests prove singleton sharing across request scopes, production startup validation, release across success and all existing typed failure classes, group/role/token/throw paths, release before mapping/signing/audit, native and cleanup retention after interruption, waiting 499/504 behavior, and HTTP correlation/security headers. A controlled in-memory test holds one permit, admits ten waiting logins, observes unchanged 429 for the eleventh, expires the ten budgets, and verifies no authentication invocation occurred.

Twenty outcome cases each repeat three times to expose leakage. Release probes record failures outside controller exception handling, so best-effort audit swallowing cannot hide a failed release assertion. Native fakes hold user search, user bind, group search and connection disposal while a second controller invocation waits. No test uses real AD/LDAP/IIS/SQL/network infrastructure or production load.

The first full run exposed two failures in new invalid-configuration tests: WebApplicationFactory's deferred startup teardown could surface ObjectDisposedException instead of the underlying OptionsValidationException. Those new tests now use a directly owned HostBuilder with the exact production AddActiveDirectoryOptions/ValidateOnStart registration, preserving and strengthening the exact validation-exception assertion. No production code or existing test expectation was altered for that fixture issue. Targeted and full reruns pass. Failed and final TRX evidence is retained.

Evidence directory: C:\Apps\LabAuthServer\Temp\Phase2A-LdapConcurrency-20260910. It contains the pre-edit source copies/hashes and status, ConcurrencyResults, LdapResults, FinalResults, final build/test logs and final integrity results.

## Exact file inventory

Paths are relative to C:\Apps\LabAuthServer\Source\LabAuthServer and describe this increment against its pre-edit snapshot, not against HEAD.

| File | Purpose |
| --- | --- |
| src/LabAuthServer.Application/Interfaces/ILdapConcurrencyLimiter.cs | New application admission/lease contract. |
| src/LabAuthServer.Infrastructure/Services/LdapConcurrencyLimiter.cs | New singleton async semaphore gate and exactly-once lease release. |
| src/LabAuthServer.Application/Enums/DirectoryFailureStage.cs | Append ConcurrencyWait without renumbering existing stages. |
| src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptions.cs | Add MaxConcurrentLdapOperations with safe default 4. |
| src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptionsValidator.cs | Validate inclusive 1-32 range at startup. |
| src/LabAuthServer.Api/Extensions/ActiveDirectoryOptionsExtensions.cs | Register one application-instance limiter. |
| src/LabAuthServer.Api/Controllers/AuthController.cs | Acquire once for identity/groups; release after awaited cleanup and before unrelated work. |
| src/LabAuthServer.Api/appsettings.json | Source-only example setting. |
| tests/LabAuthServer.UnitTests/LdapConcurrencyLimiterTests.cs | New deterministic gate/capacity/origin/release tests. |
| tests/LabAuthServer.IntegrationTests/LdapConcurrencyTests.cs | New controller/native/HTTP/configuration/lifetime/release tests. |
| tests/LabAuthServer.IntegrationTests/AuthenticationTests.cs | Supply the required limiter to existing direct-controller fixtures; expectations unchanged. |
| tests/LabAuthServer.IntegrationTests/CooperativeLdapHttpTests.cs | Supply the same DI limiter to an existing direct-controller fixture; expectations unchanged. |
| docs/plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md | Implementation, resource/waiter boundaries, tests, file inventory and deferred work. |
| docs/Project_Status.md | Current implementation and validation checkpoint. |
| docs/Validation_Status.md | Current evidence and scope. |
| docs/Architecture.md | Login LDAP admission scope and singleton lifetime. |
| docs/Configuration.md | Default, bounds and source/live configuration distinction. |
| docs/Testing.md | New coverage and final counts. |

## Security regression and git safety

The completed RSA 2048-4096 policy, encoded JWT 12288, Authorization 12352, MaximumTokenSize 7680, login body 8192, decoded-header 16128, AllowedHosts DC01.lab.local, API response headers, SQL TLS and dedicated signing-certificate architecture remain unchanged. The failure classifier, typed result models and AuthenticationOperation implementation are unchanged; only a new stage is appended for gate waiting. Program's existing admission/rate-limiter policy and middleware ordering are unchanged.

HEAD remains 6793324365d860085080ab53bb7ccdb3b4401c28. Nothing is staged. The worktree intentionally retains previous changes and this increment. No reset, clean, stash, revert, commit or push was performed. Final hashes/status and diff checks establish the source preservation boundary; no live configuration, deployment or infrastructure action occurred.

Final snapshot comparison: 13 modified existing files and 5 added files, exactly matching the 18-file inventory. All other 188 baseline files retain their original hashes; no baseline file is missing. Existing appsettings values are structurally identical after excluding the one new MaxConcurrentLdapOperations property. No unexpected source or dependency changes were found. git diff --check passes (exit 0); staged diff is empty. New files pass the trailing-whitespace check.

## Deferred items

| Item | Status and reason |
| --- | --- |
| Native hard LDAP cancellation | DEFERRED: synchronous Windows LDAP provider limitation. |
| Infrastructure LDAP capacity tuning | DEFERRED: infrastructure/operations scope; configured permits are not an enterprise capacity claim. |
| IIS/HTTP.sys tuning and real IIS acceptance | DEFERRED: deployment/infrastructure scope. |
| Proxy/WAF/load-balancer validation | DEFERRED: network/infrastructure scope. |
| Production load testing | DEFERRED: operational/infrastructure scope. |
| Operational release review | DEFERRED: change-management, monitoring, rollback and release sign-off scope. |
| HSTS deployment | DEFERRED: browser/HTTPS deployment policy; remains disabled. |
| Explicit pending-waiter cap and overflow policy | IMPLEMENTED with explicit ResourceExhausted approval; default 16, range 1-128. See [current implementation](Phase-2A-Hard-Pending-Waiter-Cap.md). |
| New LDAP entry points or scheduled diagnostic callers | DEFERRED until their admission scope is explicitly designed; raw helpers do not automatically inherit the controller gate. |

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
