# Phase 2B / 2C — Application Reliability & Hardening (increment report)

Date: 2026-09-10. **IMPLEMENTED, TESTED, SOURCE ONLY.**

Cross-references: [Phase 2A cooperative LDAP cancellation and authentication deadlines](Phase-2A-LDAP-Cancellation-Deadlines.md),
[Phase 2A LDAP failure classification](Phase-2A-LDAP-Failure-Classification.md),
[Phase 2A hard pending waiter cap](Phase-2A-Hard-Pending-Waiter-Cap.md),
[Phase 2A API resource protection](Phase-2A-API-Resource-Protection.md),
[Phase 2B LDAP reliability](Phase-2B-LDAP-Reliability.md),
[Phase 2C audit reliability](Phase-2C-Audit-Reliability.md),
[coding-only scope](Coding-Only-Scope.md).

## Scope outcome

Repository inspection established that the great majority of Phase 2B and Phase 2C application
requirements were already satisfied by the uncommitted Phase 2A work:

- Typed failure model, bounded reasons/stages, fixed client-visible messages.
- One shared `AuthenticationOperation` deadline/origin/stage context with deterministic precedence.
- Cooperative cancellation propagation through credential loading, connection setup, service bind,
  user search, user bind, group search, response validation, role mapping and token issuance.
- Awaited cleanup ownership; late results rejected; no post-cancellation group lookup, role mapping,
  JWT signing or success audit.
- Hard pending-waiter cap with admission/registration atomicity and permit release on every path.
- LDAP timeout classification (`85`, `3`, `TimeoutException`, socket timeout) to `Timeout`.
- LDAPS/636 enforcement, filter escaping, single-entry cardinality, ranged-`memberOf` rejection,
  DN parsing hardening and successful-empty-groups vs failed-lookup separation.
- Ingress/JWT/header/body bounds, RSA key policy, login rate limiting.

Two genuine application-level gaps remained and were implemented in this increment.

## Phase 2B — Root DSE cancellation propagation

The login pipeline was already fully cooperative. `LdapService.QueryRootDseAsync` was the one
remaining asynchronous entry point that discarded the caller token and performed a synchronous
credential wait (`GetAwaiter().GetResult()`).

Implemented, within the existing architecture and without a second error-handling path:

- The caller token is checked before any work begins.
- Credential acquisition is now awaited with the caller token, so a cancelled request never enters
  the blocking provider call and never blocks a thread on the synchronous wait.
- The awaited blocking Root DSE provider call receives the caller token through `Task.Run`, and the
  blocking body re-checks it before bind and before connection creation.
- Existing safe-failure contract preserved: cancellation returns the unchanged
  `"The operation was cancelled."` result; missing service account or empty password returns the
  unchanged generic `"The directory service could not complete the Root DSE query."` message with
  no configuration disclosure.

Root DSE remains a separate diagnostic operation and does not participate in login orchestration or
the login deadline. As documented in Phase 2A, an already-running synchronous native bind cannot be
force-aborted by this provider; the token governs admission and cooperative checkpoints.

## Phase 2C — Bounded LDAP membership collection

`LdapService.PerformUserGroupQuery` copied every `memberOf` value into a `SortedSet` with no bound.
The authorization policy bound (`Authorization:MaximumGroupCount`, default 100, validated 1-1000)
is applied only later, by `AdGroupRoleMappingService`, after the full collection had already been
materialized. That left application allocation proportional to the directory response.

Implemented:

- New option `ActiveDirectory:MaximumGroupMemberships`, default `100`, validated inclusive `1-1000`
  by `LdapOptionsValidator` through the existing `ValidateOnStart` path.
- The membership set is checked **before** any value is copied. Exceeding the bound is a fail-closed
  typed failure, `ProtocolFailure` / `ResponseValidation` / `MembershipLimitExceeded`, surfaced as
  the existing generic `"Authentication service unavailable."` (HTTP 503).
- Values are never truncated. Truncation would silently produce a different authorization result,
  which the Phase 2A/2B contract forbids.
- Exactly-at-bound remains a complete success; empty membership remains a success distinct from
  failure; ranged-`memberOf` incompleteness handling is unchanged.
- Logging carries only bounded category/stage/reason; no DNs, filters, membership values or
  credentials.

No duplicate concurrency gate, retry mechanism, second audit architecture or distributed control was
introduced.

## Failure classification

No existing category, stage or reason was renamed, renumbered or reinterpreted.

| Change | Detail |
| --- | --- |
| Appended reason | `DirectoryFailureReason.MembershipLimitExceeded` (appended after `PendingWaiterCapacityExceeded`). |
| Category used | Existing `AuthenticationFailureCategory.ProtocolFailure`. |
| HTTP mapping | Existing `ProtocolFailure` -> 503. No new status mapping. |
| Precedence | Unchanged: caller cancellation first -> Cancelled/499; deadline first -> Timeout/504; equality -> Timeout/504. |

## Tests

New file `tests/LabAuthServer.UnitTests/LdapReliabilityHardeningTests.cs`, 22 deterministic tests,
no AD/network/IIS/SQL dependency and no arbitrary sleeps.

Phase 2B coverage: default and valid/invalid `MaximumGroupMemberships` configuration; already-cancelled
caller does not load credentials or touch the connection provider; credential load receives the
caller token and cancellation completes the operation; missing service-account username fails safely
without credential loading; empty service password fails safely without disclosure.

Phase 2C coverage: membership exactly at bound (1/100/1000) is a complete success; above bound
(1/100/1000+1) is a typed failure, is never truncated, exposes no readable groups and still disposes
the connection; empty membership stays successful; ranged `memberOf` still reports
`IncompleteMembership`; failure logging contains no sensitive content.

## Results

| Gate | Result |
| --- | --- |
| Focused Phase 2B/2C tests | 22 passed, 0 failed, 0 skipped |
| Full suite | 722 passed (487 unit, 235 integration), 0 failed, 0 skipped |
| Release build | 0 warnings, 0 errors |

The full suite was 700 passed at the Phase 2A baseline; the increment adds 22 tests and no existing
test was removed, disabled or weakened.

## Configuration changes

| Setting | Value | Validation |
| --- | --- | --- |
| `ActiveDirectory:MaximumGroupMemberships` | `100` | Inclusive 1-1000; invalid values fail startup. |

Only the repository source `appsettings.json` was updated. Environment-specific and live
configuration files were not touched. No other LDAP, JWT, SQL, certificate, host or security value
changed.

## Explicitly not changed

No deployment, no IIS or application-pool action, no restart, no HTTP.sys, SQL, certificate, DNS,
firewall, proxy, WAF or load-balancer change, no infrastructure or capacity testing, no commit, no
push, no reset, no clean, no stash, no unrelated file deletion. Existing uncommitted Phase 2A work and
untracked files were preserved.

## Deferred items (infrastructure / out of application coding scope)

| Item | Reason |
| --- | --- |
| Native LDAP hard cancellation | Provider/native synchronous bind offers no safe guaranteed immediate abort. |
| Distributed / multi-instance concurrency limits | Requires separate distributed architecture approval. |
| Proxy/forwarding validation | Infrastructure/network topology. |
| HSTS deployment | Deployment/browser policy. |
| Real IIS acceptance testing | Deployment/operations. |
| HTTP.sys tuning | OS/IIS infrastructure. |
| Network/WAF/load-balancer validation | Infrastructure. |
| Infrastructure capacity/load testing | Operational capacity. |
| Final operational release review | Deployment, change management and rollback. |
| Durable audit spool / outbox / retry | Separate architecture decision under Phase 2C audit reliability; not approved here. |

## Status

APPLICATION VALIDATED (source, tests, Release build).

INFRASTRUCTURE DEFERRED (all items above).

No Phase 2C application-level gap beyond this remained after inspection; the other listed controls
were already implemented in Phase 2A and were verified, not duplicated.