# Phase 2A hard pending-waiter cap: contract gate

2026-09-10: **RESOLVED BY EXPLICIT APPROVAL; IMPLEMENTED AND SOURCE-TESTED.** The user approved ResourceExhausted, HTTP 503, ConcurrencyWait, PendingWaiterCapacityExceeded and default/range 16/1-128. See [the implemented design and 700-test closure evidence](Phase-2A-Hard-Pending-Waiter-Cap.md). The remainder of this document preserves the earlier blocked analysis and proposal as history; its pending/blocked statements no longer describe current status.

## Why implementation stopped

The current request, Step 5, explicitly says: "If the current categories cannot safely represent resource exhaustion, STOP and report the design conflict rather than silently introducing a new public contract." Step 6 permits DirectoryUnavailable only if semantically justified, and requires documenting why existing categories are insufficient before introducing ResourceExhausted. This is a conditional allowance, not an unconditional redefinition of the existing directory-failure contract.

Inspected evidence:

- `Application/Enums/AuthenticationFailureCategory.cs` contains None, InvalidCredentials, DirectoryUnavailable, Timeout, InvalidRequest, Configuration, Cancelled, Unexpected and ProtocolFailure. No capacity category exists.
- `Application/Enums/DirectoryFailureReason.cs` contains provider DirectoryBusy/DirectoryUnavailable/TransportFailure and interruption/validation reasons; none identifies a local waiter-cap rejection.
- `Infrastructure/Services/LdapFailureClassifier.cs` produces DirectoryUnavailable for service-bind rejection, transport errors and LDAP busy/unavailable results. The local queue can be full while the directory is healthy, before any directory call.
- `Application/DTOs/DirectoryFailure.cs` and `Api/Controllers/AuthController.cs` provide the existing generic 503 response for DirectoryUnavailable. Sharing that HTTP status/message would be compatible at the client boundary, but does not by itself make the provider category/diagnostic semantically accurate for application capacity.
- The existing `Phase-2A-LDAP-Concurrency-Resource-Protection.md`, "Scope, reconsideration and rejection contract", explicitly records that DirectoryUnavailable would misdescribe an ordinary full waiter cap. Reusing it now would extend that documented category meaning. The new task allows reuse only after establishing semantic compatibility; the repository does not already establish it.
- The 429 in Program is the upstream fixed-window rate policy, not a typed resource-exhaustion outcome. Reusing it would invent a different response path and contradict the suggested 503 capacity behavior.
- InvalidCredentials/InvalidRequest would blame the request; Timeout/Cancelled require an actual deadline/caller event; Configuration/Unexpected/ProtocolFailure would misdescribe normal capacity exhaustion.

No existing enum was repurposed, LDAP error code fabricated, new category added or exception allowed to fall through to generic 500. This is an application contract decision, not an infrastructure blocker. Implementing only the counter while leaving the rejection path undefined would not produce a safe, reviewable feature.

## Concrete contract proposed for approval

Recommended new bounded outcome, **proposal only**:

| Field | Proposed value |
| --- | --- |
| Category | Append `ResourceExhausted` without renumbering existing categories. |
| Stage | Existing `ConcurrencyWait`. |
| Reason | Append `PendingWaiterCapacityExceeded`. |
| Diagnostic code/source | Null / None; this is not an LDAP server result. |
| HTTP | Existing status 503 Service Unavailable. |
| Client message | Existing generic `Authentication service unavailable.` |
| Response shape | Existing safe ProblemDetails and correlation, with API response headers. |
| Audit | Existing AUTH_LDAP_FAILURE event, Success=false, bounded category/stage/reason and no new SQL event/schema. This records failure to admit the LDAP phase, distinguished explicitly from provider unavailability. |
| Downstream work | No credentials, connection, bind, group lookup, mapping, signing or login-success audit after capacity rejection. |

This is a new application outcome and audit classification even though it reuses an existing HTTP status and generic message. Explicit approval is needed under the task's stop rule. Alternatively, the architect can explicitly authorize broadening DirectoryUnavailable to include local LDAP admission capacity, with a distinct local-capacity reason; that would supersede the earlier documented meaning. No such reinterpretation has been applied here.

## Prepared cap design, not implemented

Proposed option: `ActiveDirectory:MaxPendingLdapWaiters`, consistent with MaxConcurrentLdapOperations. Suggested default **16**, inclusive range **1-128**, validated at startup and construction. These are finite conservative application values, not measured capacity recommendations. Sixteen pending plus four active accommodates a twenty-admission boundary burst when active holders belong to that burst; older native holders may make some new admissions reject, which is the purpose of explicit admission capacity. Source and live settings have not changed.

The option would count only requests admitted to the existing gate that await a permit. Active permit owners do not consume pending capacity. Immediate capacity should bypass pending accounting; otherwise reserve a pending slot atomically before entering SemaphoreSlim.WaitAsync. Full capacity must immediately produce the approved typed failure, with no semaphore registration or downstream work. A small immutable admission result can carry either the existing disposable permit or DirectoryFailure, matching the existing authentication/group result pattern; the interface and its tests would change deliberately rather than throwing an unclassified overload exception.

The implementation must prove single ownership of pending reservations across acquisition, cancellation, expiry and exceptions; preserve the post-handoff operation check; and return each lease exactly once after actual native completion. Cancellation/deadline observation must precede capacity rejection and preserve the first recorded origin and deadline-at-equality contract. A full-cap result must be selected at a defined admission boundary so later signals cannot silently relabel an already accepted result.

An implementation detail remains to prove, not assume: a logical cancelled waiter, a retained semaphore wait operation and its not-yet-run continuation are different states. Releasing a counter in a cancellation callback before the semaphore actually removes the wait can let replacement work grow retained wait state; waiting only for an arbitrarily delayed finally can leave logically cancelled work counted. The implementation and deterministic tests must establish both correct logical waiter removal and bounded retained wait registration, without an unbounded callback/background queue. This document does not claim that a proposed counter already proves those properties.

No FIFO fairness is promised. Limits remain per DI service provider/application instance. Neither the cap nor active concurrency bounds all upstream requests, post-LDAP audit work, total memory or cluster-wide LDAP capacity.

## Existing behavior reconfirmed

The gate is one DI singleton and currently has no hard pending count/cap. Active concurrency defaults to 4, validated 1-32. AuthController owns one AuthenticationOperation before acquisition and one lease across sequential identity/groups and awaited cleanup; it releases before mapping/signing/audit. The default 30-second budget validates 1-60 seconds. Program's shared constant login partition allows 10 per fixed minute, queue zero, before the controller/gate.

Current waiting uses the operation token. Caller cancellation maps to 499 where writable; expiry to 504. First recorded origin is retained; deadline wins at equality. Late internal permit delivery is rejected and returned before exposing a usable lease. Repeated disposal uses Interlocked.Exchange and does not inflate active capacity. Native work is not forcibly terminated: holders remain occupied until awaited work/cleanup finishes, and late results cannot authorize subsequent stages.

Existing deterministic concurrency/cooperative tests cover exact active limits, singleton sharing, cancellation/expiry/handoff races, repeated cleanup, native retention, failure paths, blocked HTTP admission and security/correlation. They do not prove a nonexistent hard pending cap. No cap-specific tests were added because implementation stopped at the contract gate.

## Validation and safety

Baseline and revalidated full suite: **665 passed (436 unit, 229 integration), 0 failures, 0 skips**. Targeted concurrency: **64 passed**. Targeted LDAP/directory-result/cooperative: **389 passed**. No new tests. Release build: **0 warnings, 0 errors**. Existing security regressions are included in the full suite. The cap's required tests, combined-model validation and post-implementation closure review remain pending.

HEAD: `6793324365d860085080ab53bb7ccdb3b4401c28`; nothing staged. Baseline status/hashes, TRXs, build log and final comparison are under `C:\Apps\LabAuthServer\Temp\Phase2A-WaiterCapContract-20260910`. Prior fixes and uncommitted work are preserved. No application/configuration/dependency change and no live action occurred.

Exact documentation-only increment:

| Path | Purpose |
| --- | --- |
| docs/plans/Phase-2/Phase-2A-Hard-Waiter-Cap-Contract-Gate.md | Contract conflict, concrete proposal and unchanged validation. |
| docs/plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md | Current cap-request status; earlier deferred decision remains historical evidence. |
| docs/plans/Phase-2/Phase-2A-Final-Application-Code-Review.md | Clarify closure is pending the newly required cap. |
| docs/Project_Status.md | Current expanded-scope blocker. |
| docs/Validation_Status.md | Separate existing green tests from unimplemented cap acceptance. |

## Deferred infrastructure/provider work

Native hard cancellation remains deferred because of synchronous Windows LDAP provider limitations. IIS acceptance is deployment/operations; HTTP.sys is OS/IIS infrastructure; proxy/forwarding is network infrastructure; HSTS is browser/deployment policy; network/WAF/load-balancer validation is infrastructure; production/infrastructure load testing is operational capacity; release review is change management/sign-off; distributed LDAP concurrency is separate architecture/infrastructure. None is part of resolving this application contract.

**PHASE 2A APPLICATION CODING = NOT READY TO CLOSE** under the latest required-cap scope. Approval of the proposed ResourceExhausted contract (or an explicit alternative category interpretation) is the next required input; implementation and complete validation must follow before closure.
