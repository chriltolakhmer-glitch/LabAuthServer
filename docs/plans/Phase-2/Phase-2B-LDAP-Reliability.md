# Phase 2B — LDAP Reliability & Fail-Closed Behavior

> 2026-09-10: the classification/result-contract portion is implemented and tested in [Phase 2A LDAP failure classification](Phase-2A-LDAP-Failure-Classification.md). The Current State and proposed classification narrative below describe the earlier planning baseline. Deadline/cancellation/concurrency work remains future coding; infrastructure/release validation is DEFERRED under the [coding-only scope](Coding-Only-Scope.md).

Status: APPLICATION-CODING WORK COMPLETE. See the [Phase 2B/2C increment report](Phase-2B-2C-Application-Hardening.md) for the implemented root-DSE cancellation propagation and bounded membership collection, tests, and the deferred infrastructure items. [Phase 2 sequence](README.md).

# Objective

Preserve the existing AD authentication sequence while bounding LDAP work and distinguishing authoritative empty membership from dependency failure. Never issue a token after an incomplete, failed or cancelled lookup.

# Current State

- src/LabAuthServer.Infrastructure/Services/LdapAuthenticationClient.cs uses Task.Run around blocking Bind/SendRequest. AuthenticateAsync loads the DPAPI service password, service-binds, searches the UPN, checks disabled state, then binds the submitted UPN/password. The same exception handler currently maps LDAP code 49 from either bind to InvalidCredentials.
- LdapService.GetUserGroupsAsync accepts a user password but only validates its presence; the group query uses service-account credentials. ILdapService's comment incorrectly describes a user-credential bind. Preserve the runtime service-account sequence.
- Group lookup catches LDAP errors/cancellation/unexpected errors and returns an empty array. Missing service configuration, null/non-success response and non-LDAPS configuration also return empty. It combines entries rather than requiring exactly one matching user.
- Both user bind and group query register cancellation-triggered connection disposal. This does not prove native work ends immediately; Task.Run cancellation mainly governs scheduling. Disposed-object races are not consistently classified.
- Root DSE uses Task.Run without in-flight cancellation propagation, synchronously waits for credential retrieval, requests all attributes and relies on configured LDAPS settings. Group query also synchronously waits for credentials within blocking work.
- LdapConnection.Timeout uses ActiveDirectory.ConnectionTimeout (sample ten seconds). There is no explicit total login deadline across service bind, user search, user bind, separate group query and audit.
- AuthController calls mapping/token issuance after receiving groups. Missing roles fail closed downstream, but the broad post-authentication catch turns these and cancellation into generic 500 token-issuance failures and logs an exception object.

# Problem / Risk

Empty membership and outage are operationally indistinguishable. Service-account failure can appear to be a user's wrong password. Worker starvation, repeated request cancellation and sequential timeouts can outlive the caller. Partial/multiple user results must not become an authorization source. Raw exception logging can conflict with Security.md.

# Scope

Result classification, bounded admission/deadlines, cleanup, safe logs and consistent HTTP/audit mapping across existing LDAP methods. Preserve service-account search then submitted-UPN bind, LDAPv3/LDAPS:636, platform certificate validation, filter escaping, direct memberOf mapping and role precedence.

# Non-Goals

No new LDAP provider/package, directory cache, connection pool, authentication redesign, anonymous fallback, DN-bind fallback, nested-group expansion or user-password persistence. Do not change DPAPI protection scope or role policies.

# Proposed Architecture

Use the smallest explicit result contract at the Application boundary. Proposed GroupLookupResult contains success/failure classification and a bounded group collection only for successful complete searches. Success with zero groups is distinct from failure. Proposed failure categories: configuration, dependency unavailable, timeout, cancelled and unexpected. Exact type/file names require ordinary design review before implementation; this is not a new authentication architecture.

Require exactly one user entry. Null response, non-success/partial response, ambiguous user result, parsing/truncation failure and excessive membership cannot grant roles. A successful single user without memberOf can yield an empty collection. Do not silently truncate groups at MaximumGroupCount.

| Stage/outcome | Proposed internal handling | HTTP/token contract |
| --- | --- | --- |
| User bind rejects credentials/disabled or absent user | Existing generic authentication failure | Preserve 401, no token |
| Service-account bind rejects credentials | Classify dependency/configuration with stage | Generic service failure, proposed 503; never blame user credentials |
| DPAPI/configuration missing or unreadable | Configuration | Preserve generic 500, no token |
| Directory unavailable / transport trust failure | Dependency failure with safe classification | 503, no token; no TLS bypass |
| Deadline expires | Timeout, dispose operation resources | 504 where response can still be written |
| Caller cancellation | Cancelled; stop downstream mapping/signing | Existing 499 convention where writable; otherwise no response |
| Complete successful query with no approved groups | Explicit no-role outcome | DECISION REQUIRED: recommend generic 403 rather than current incidental 500 |
| Ambiguous/incomplete/unexpected query | Failed lookup | Safe 500 or approved dependency category, never partial authorization |
| Complete mapped membership | Existing precedence and one role | Existing token response |

DECISION REQUIRED: approve the no-role and service-account status changes, and exact ambiguous-result classification before implementation. Public messages must not disclose account existence, bind stage, AD status or group membership. Internal stage/category supports diagnosis without raw response data.

Bound execution with a process-wide LDAP admission limit shared across request scopes and a bounded/no-queue policy decided from 2A load evidence. Retain the blocking library initially; prove whether its native operations respond to disposal/deadline on the target Windows runtime. A timed-out caller must not release the admission slot while native work is still running. Never implement fake cancellation by abandoning unlimited Task.Run work or by assuming Task.WhenAny terminates the operation. If adequate bounds cannot be demonstrated, record a blocked implementation decision and obtain approval for a different adapter strategy.

Apply a total LDAP-stage deadline linked to RequestAborted, with per-operation timeout no larger than remaining budget. Await credential acquisition before entering blocking network work where possible; keep the existing secret provider. Cancellation/disposal must have explicit ownership and be idempotent. Retain the slot until actual completion in finally. Root DSE must share the same bounded path and runtime LDAPS guard; monitoring must not create a competing unbounded pool.

# Implementation Steps

## Step 1

Add the typed group outcome and approved mapping table; update ILdapService, controller consumers and fakes together. Keep the existing password parameter in the first compatibility-preserving slice, document its actual use, and consider removal only as a separately reviewed internal contract cleanup. Add explicit failure/no-role branches before mapping/signing.

## Step 2

Separate service-bind, search and user-bind classifications; use one bounded execution mechanism for the existing operations. Add deadline/cancellation cleanup and cardinality/completeness checks. Correct Root DSE's in-flight behavior and minimize probe attributes without adding a public diagnostic endpoint.

## Step 3

Replace raw exception logging in the affected login/LDAP path with stage, bounded category and correlation. Test native cancellation and saturated/degraded dependencies in Windows staging. Commit result-contract and execution changes separately in the future when each is coherent and regression-tested.

# Source Areas Expected to Change

- src/LabAuthServer.Application/Interfaces/ILdapService.cs; proposed result DTO under Application/DTOs and category under Application/Enums if needed.
- src/LabAuthServer.Infrastructure/Services/LdapService.cs, LdapAuthenticationClient.cs, ILdapAuthenticationClient.cs and LdapAuthenticationService.cs.
- src/LabAuthServer.Infrastructure/ActiveDirectory/LdapOptions.cs and LdapOptionsValidator.cs; DpapiLdapServiceAccountCredentialProvider.cs only if cancellation/error classification evidence requires it.
- src/LabAuthServer.Api/Extensions/ActiveDirectoryOptionsExtensions.cs for shared admission registration; Controllers/AuthController.cs for explicit outcomes.
- tests/LabAuthServer.UnitTests/LdapInfrastructureTests.cs, LdapAuthenticationServiceTests.cs, AdGroupRoleMappingServiceTests.cs; tests/LabAuthServer.IntegrationTests/AuthenticationTests.cs, LdapRootDseTests.cs, ActiveDirectoryOptionsTests.cs. Proposed narrow connection seam remains in Infrastructure and must not expose LDAP library types to Application.

# Configuration Changes

Reuse ConnectionTimeout. DECISION REQUIRED: total LDAP deadline, maximum concurrent native work, admission wait/queue size, overload status, search result/membership handling and how these fit HTTP and audit deadlines. Proposed new options must validate finite positive values, cross-budget relationships and documented restart behavior. Do not alter LDAPS/domain/service-account defaults as a reliability workaround.

# Database Changes

None required. Use existing LDAP failure/login/access-denied vocabulary where semantically correct. Any new SQL event code requires separate coordinated approval in 2C; logs/metrics can carry bounded stage categories without schema changes.

# Testing Strategy

## Unit Tests

Use controllable connection/clock/admission seams. Cover service vs user bind code 49, 81/85, timeout, cryptographic/configuration failure, null/partial/multiple search results, valid empty groups, unknown groups, configured maximum and overflow. Spy on mapping/signing to prove failed lookup cannot call either. Verify admission is shared across scoped services and every terminal path cleans up.

## Integration Tests

Exercise full HTTP login for each approved outcome with fake dependencies. Existing AuthenticationTests directly instantiate the controller, so add pipeline coverage. Exercise cancellation before scheduling, during bind/search, after success before signing, concurrent completion/disposal and capacity recovery. Authorize live AD checks separately.

## Security Tests

Reject non-LDAPS and non-636 in every path, invalid trust/hostname, escaped filter metacharacters, ambiguous directory identities and incomplete results. Missing or corrupt service credentials never fall back to anonymous/user-password group binding. Log-capture assertions exclude secrets, filters, raw LDAP attributes and exception stacks.

## Regression Tests

Keep configured-UPN binding, successful token shape, role precedence, unknown-group denial, MaximumGroupCount, JWT validation and 2A limiter/boundaries. Do not introduce implicit Administrator access to Reader endpoints.

## Manual Validation

In Windows staging, simulate stalled/refused/trust-failing LDAPS and cancel in-flight work. Record elapsed caller time and actual native completion, active workers/handles, sustained-memory behavior, admission rejection and recovery. Existing RootDseTests accepting safe failure do not prove any of these live properties.

# Security Considerations

No fallback authorization from prior/partial data; no retries of invalid user credentials that amplify AD lockout. Keep generic public outcomes and constrained private diagnostics. Minimize password lifetime and do not put credentials in queued work records or logs.

# Operational Considerations

AD operator confirms latency and service-account lockout/expiry behavior. Monitor stage/category/duration, in-flight work and saturation with low-cardinality labels. Native timeout behavior is a release gate, not an assumed property.

# Compatibility Risks

No-role 500-to-403 and service-account 401-to-503 are proposed client-visible changes. Typed results change fakes/internal callers. Bounded admission can reject bursts allowed by the login window. Keep these changes explicit and test clients.

# Rollback Plan

Restore the prior coherent LDAP/controller contract and configuration package. Retain 2A limits; do not restore a package that can grant on lookup failure. If native work cannot drain safely, remove the instance from service and follow authorized app-pool recovery. Never fix outage by allowing plaintext LDAP or skipping certificate checks.

# Acceptance Criteria

Every failure category has a tested safe outcome; no failed/empty-unmapped/cancelled lookup issues a token. Service and user failures are distinct internally. All native operations and queued callers remain within approved bounds; resources return to baseline after repeated cancellation/outage. AD mapping and Phase 1 controls remain unchanged.

# Definition of Done

Approved outcome table and budgets, passing fake-based regressions, Windows cancellation/load evidence, documented safe logs and operator recovery procedure. Unverified native cancellation is recorded as a release blocker.

# Open Decisions

DECISION REQUIRED: no-role/public error contracts, shared admission and total budget values, native cancellation guarantees, narrow test seam, malformed/ranged memberOf completeness policy and whether later removal of the unused password argument is worthwhile. Do not expand directory semantics while resolving these.

# Dependencies

Uses 2A resource/HTTP envelope. Supplies typed outcomes and timeout budget to 2C and 2D. 2E validates service-account, trust, real membership and restart behavior.
