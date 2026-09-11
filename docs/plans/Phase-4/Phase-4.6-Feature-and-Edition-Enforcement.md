# Phase 4.6 — Feature and Edition Enforcement

Status: IMPLEMENTED — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED. [Phase 4 README](Phase-4-README.md) | Previous: [4.5](Phase-4.5-License-Validation.md).

Implementation note (2026-09-11): the reusable enforcement boundary is implemented as `ILicensePolicy` / `LicensePolicy` with a typed `LicenseFeatureDecision`, a known-feature catalog (`LicenseFeatureIds`) and known limit keys (`LicenseLimitKeys`). Endpoint wiring, the commercial feature-to-edition matrix (O-01), enforcement timing (O-09) and the denied-feature API response remain TO BE CONFIRMED DURING IMPLEMENTATION.

## Objective

Define how a validated license policy gates commercial features and editions without weakening security.

## Scope

In scope:

- Feature identifier scheme.
- Edition hierarchy.
- Default-deny behavior for unknown and missing features.
- Startup versus per-request enforcement.
- API, admin and UI behavior.

Out of scope:

- Expiration semantics (4.7).
- Format changes (4.2).
- Commercial edition naming approval (4.0).

## Why it exists

Enforcement is where licensing becomes visible to users. Done badly it either disables security controls or produces inconsistent behavior between endpoints.

## Prerequisites

- 4.5 producing a typed policy.
- Approved edition and feature list from 4.0.

## Inputs

- Validated license policy.
- The set of known feature identifiers.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.6-1 | Feature identifiers are stable, lowercase, dot-separated ASCII strings | PROPOSED | Readable, versionable, unambiguous |
| D4.6-2 | Unknown feature identifiers in a license are rejected at validation time | PROPOSED | Default deny, prevents silent widening |
| D4.6-3 | Missing feature means denied | PROPOSED | Default deny |
| D4.6-4 | Editions are an ordered hierarchy; higher editions include lower-edition features unless explicitly excluded | PROPOSED | Simple mental model |
| D4.6-5 | Enforcement points call a single policy object | PROPOSED | One decision source |
| D4.6-6 | Authentication, LDAP transport security, TLS, request limits, audit and JWT remain unconditional | FINAL (constraint) | Licensing must not weaken security |
| D4.6-7 | Denied licensed functionality returns a documented, non-disclosing error | PROPOSED | Consistent client behavior |

DECISION REQUIRED: The edition names and the feature-to-edition mapping. Placeholder model only, not final:

| Edition (placeholder) | Example features (placeholder) |
| --- | --- |
| Community | basic authentication, JWT |
| Professional | Community plus LDAP, audit, advanced features |
| Enterprise | Professional plus extended/unlimited features |

DECISION REQUIRED: Whether enforcement is startup-only, per-request, or a mixture (for example startup for endpoint exposure, per-request for limit counters). Recommendation: startup for feature exposure, per-request or periodic for counters such as maximum users.

DECISION REQUIRED: The API response for a denied licensed feature. Options: a generic authorization-style failure with no licensing detail; a distinct documented status. Recommendation: generic, non-disclosing failure with server-side logging.

## Proposed architecture

Enforcement model:

1. The validator produces an effective policy.
2. A policy service exposes `IsFeatureEnabled(featureId)` and limit accessors.
3. Feature code asks the policy before performing licensed work.
4. Denied work returns a documented failure and is logged server-side.
5. Security-relevant code never asks the policy.

Feature identifier examples (illustrative, not final):

- `auth.basic`
- `auth.jwt`
- `auth.ldap`
- `audit.logging`
- `admin.console`

Edition hierarchy (illustrative, not final):

```
Community  <  Professional  <  Enterprise
```

Rules:

- An edition implicitly enables the features of lower editions unless the license explicitly excludes them by omission.
- A limit not present in the license takes its edition default, which for safety is the most restrictive documented default.
- A limit present and outside the allowed range is a validation failure, not a silent clamp.

Startup versus per-request:

| Concern | Recommended timing |
| --- | --- |
| Which optional endpoints are exposed | Startup |
| Feature enablement for a request | Per-request read of an in-memory policy |
| Maximum-user and similar counters | Per-request or periodic, against the live store |
| Expiry | Periodic revalidation |

Security constraint:

Authentication and core security controls must never be disabled merely because an optional feature is unlicensed. Licensing restricts commercial functionality, not security. Any proposed exception requires explicit written approval recorded in the [Decision Log](Phase-4-Decision-Log.md).

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: policy service and enforcement helpers.
- TO BE CONFIRMED DURING IMPLEMENTATION: optional feature endpoints or services that consult the policy.
- TO BE CONFIRMED DURING IMPLEMENTATION: dependency injection registration.

## Files that must NOT change

- Authentication controllers and services.
- LDAP client and failure classification.
- JWT issuance and validation.
- TLS, certificate and rate/size limit configuration.
- Audit logging core.

## Implementation steps

1. Approve the edition and feature list.
2. Define the policy service interface.
3. Add enforcement to licensed features only.
4. Add limit counters where required.
5. Add tests for denied and allowed features, and for the security-independence guarantee.

## Security considerations

- Enforcement must not become an authorization replacement.
- Deny responses must not disclose licensing internals.
- Limits must be enforced against authoritative data, not client-supplied values.
- A policy read failure must deny, not allow.

## Failure cases

- A new optional endpoint added without a policy check.
- Licensing enforced on an authentication endpoint, blocking login for unlicensed deployments.
- Counter race conditions allowing temporary over-limit operation.
- Denied feature returning a 500 instead of a documented denial.

## Testing requirements

- Unlicensed optional feature is denied.
- Licensed feature remains available.
- Unknown feature identifier in a license is rejected.
- Maximum-user boundary: at limit, one below, one above.
- Over-limit behavior is deterministic and documented.
- Security controls remain active with an invalid license.
- Denied-feature response contains no licensing internals.

## Acceptance criteria

- Feature identifier scheme documented.
- Edition hierarchy documented.
- Default-deny behavior verified.
- Security-independence constraint verified by test.

## Rollback considerations

Enforcement is additive. Reverting the enforcement commit restores unrestricted behavior; it does not break authentication because enforcement was never placed in the security path.

## Evidence to record

- Approved edition and feature mapping.
- Boundary test results.
- Security-independence test result.

## Git/commit strategy

- Suggested message: `feat(licensing): enforce edition features and limits`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Requires 4.5.

## Risks

- Edition mapping churn after licenses are issued.
- Enforcement accidentally placed in the authentication path.
- Limit counters causing performance or consistency problems.

## Deferred items

- Admin UI for license status.
- Per-tenant editioning.
- Usage metering.

## Implementation record (Phase 4.6 enforcement boundary, 2026-09-11)

| Concern | Implementation |
| --- | --- |
| Feature catalog | `LicenseFeatureIds` (`auth.basic`, `auth.jwt`, `auth.ldap`, `audit.logging`, `admin.console`) with `IsKnown` and `IsWellFormed` |
| Limit catalog | `LicenseLimitKeys` (`max.users`) with `IsKnown` |
| Typed decision | `LicenseFeatureDecision` (`IsAllowed`, internal `Reason`); a denial requires a non-None reason |
| Policy boundary | `ILicensePolicy` — `Edition`, `IsRestricted`, `EvaluateFeature`, `IsFeatureEnabled`, `TryGetLimit`, `IsWithinLimit`, `MeetsMinimumEdition` |
| Policy implementation | `LicensePolicy` — immutable; `FromDocument`, `FromValidationResult`, `Restricted` |
| Provider boundary | `ILicensePolicyProvider` — never returns null; a missing valid license yields `LicensePolicy.Restricted` |

Feature model: a feature is enabled only when it is a known catalog identifier **and** the validated license explicitly lists it. Edition alone never grants a feature; a syntactically valid but catalog-unknown identifier cannot be granted even if present in the license payload.

Edition model: `Community < Professional < Enterprise` via the existing `LicenseEditionExtensions.Rank()`. `MeetsMinimumEdition` requires a non-restricted policy, a known minimum, and `Edition.Rank() >= minimum.Rank()`. Unknown editions rank `-1` and never satisfy a minimum.

Restricted mode: `LicensePolicy.Restricted` reports Community edition, defines no features and no limits, and fails every minimum-edition check. `FromValidationResult` maps every non-valid `LicenseValidationResult` to restricted, so a missing, malformed, unsupported, wrong-product, signature-invalid, untrusted-key, expired, not-yet-valid, invalid-configuration or invalid-content result grants nothing.

User limits: `max.users` is the only known limit. `TryGetLimit` returns `false` when the policy is restricted, the key is unknown, or the license does not define it — a missing limit is never unlimited. `IsWithinLimit` denies negative usage and uses `currentUsage <= limit` with no arithmetic that could overflow.

Security independence (D4.6-6): the policy exposes only commercial capability and limits. No API exists by which licensing could alter authentication, authorization, TLS, request limits, rate limiting, audit or JWT behavior, and no endpoint, middleware or service consumes the policy yet.

Tests added: `tests/LabAuthServer.UnitTests/Licensing/Phase46FeatureAndEditionEnforcementTests.cs` (47 tests) covering edition ranks, explicit/absent/unknown/null features, empty feature lists, multiple features, edition-plus-feature combinations, minimum-edition hierarchy, restricted policy behavior for every non-valid status, limit under/at/above/missing/unknown, `int.MaxValue` boundary, negative usage, and the security-independence property.

Still deferred: endpoint integration, the commercial feature-to-edition matrix (O-01), enforcement timing (O-09), the denied-feature API response, the license file location (O-10) and the concrete policy provider. Phase 4.7 is NOT started.