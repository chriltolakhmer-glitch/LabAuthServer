# Phase 4.7 — Expiration and Grace-Period Rules

Status: IMPLEMENTED — REVIEW REQUIRED; NOT COMMITTED; NOT PUSHED. [Phase 4 README](Phase-4-README.md) | Previous: [4.6](Phase-4.6-Feature-and-Edition-Enforcement.md).

Approved initial implementation: UTC timestamps; time-limited licenses supported; perpetual licenses supported; NO grace period; an expired license enters restricted mode.

Implementation note (2026-09-11): the licensed time window is decided in exactly one place by `ILicenseExpirationEvaluator` / `LicenseExpirationEvaluator`. Grace is explicitly default-disabled (`LicenseGracePeriod.None`); a bounded, validated representation exists for a future approved decision but never grants capability in this phase.

## Objective

Define expiration, clock handling and grace-period behavior explicitly, with no hidden or permanent grace.

## Scope

In scope:

- Expiration timestamp semantics.
- Clock source, UTC handling, skew tolerance.
- Optional grace period and its exact rules.
- Behavior before activation, after expiration, and on clock movement.
- Observable signals for operators.

Out of scope:

- Format field definitions (4.2).
- Renewal operations (4.12).
- Online revocation (4.13).

## Why it exists

Expiration is the rule most likely to cause an outage. It must be explicit, testable and visible, not emergent from a comparison written inline.

## Prerequisites

- 4.5 validation result model, including the expiry rule.
- 4.6 enforcement points that consume the policy.

## Inputs

- `issuedAt`, `expiresAt`, optional `notBefore`.
- Injected UTC clock.
- Documented grace configuration.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.7-1 | All timestamps are UTC; comparison uses UTC only | FINAL (constraint) | Avoids timezone-dependent behavior |
| D4.7-2 | `expiresAt` is exclusive: the license is valid strictly before that instant | PROPOSED | Unambiguous boundary |
| D4.7-3 | Clock skew tolerance is a small, documented, configurable allowance | TO BE CONFIRMED DURING IMPLEMENTATION | Handles ordinary NTP drift |
| D4.7-4 | No grace period in the initial implementation | APPROVED (D-08) | Conservative default; avoids hidden permanent grace |
| D4.7-5 | Time-limited licenses supported | APPROVED (D-07) | Commercial requirement |
| D4.7-6 | Perpetual licenses supported | APPROVED (D-07) | Commercial requirement |
| D4.7-7 | Expired license enters restricted mode | APPROVED (D-14) | Fail closed without an outage |
| D4.7-8 | Backward clock movement beyond skew does not extend validity indefinitely | PROPOSED | Mitigates trivial clock manipulation |

RESOLVED: No grace period in the initial implementation (D-08, 2026-09-11). Both time-limited and perpetual licenses are supported (D-07). An expired license enters restricted mode (D-14), keeping authentication and security active; it never refuses to start.

TO BE CONFIRMED DURING IMPLEMENTATION: Clock-skew allowance value. No clock-skew value is invented here. The recommendation remains a small, configurable, conservatively defaulted allowance on the order of minutes.

## Proposed architecture

Timeline semantics:

| Condition | Status | Behavior |
| --- | --- | --- |
| Now < `issuedAt` minus skew | `NotYetValid` | Deny licensed features |
| `issuedAt` minus skew <= now < `expiresAt` | `Valid` | Allow licensed features |
| `expiresAt` <= now < `expiresAt` + grace | `ValidWithWarning` | Allo, restricted mode |
| `issuedAt` minus skew <= now < `expiresAt` | `Valid` | Allow licensed features |
| now >= `expiresAt` | `Expired` | Deny licensed features, restricted mode (no grace, D-08) |
| Perpetual license (`expiresAt` absent) | `Valid` | Allow licensed features

- Read the clock through an injected abstraction.
- Use UTC exclusively; convert only for display.
- Compare with the documented skew allowance applied symmetrically where appropriate, never widening validity beyond the licensed window by more than the allowance plus the documented grace.
- On detecting a backward clock jump beyond the allowance, log it and continue using the observed time; do not cache a "high water mark" unless a decision explicitly adds one, because a high-water mark introduces its own lockout risk.
- On forward clock jumps, expiration takes effect immediately.

Every grace-period decision must be explicit and documented. A hidden permanent grace period is prohibited.

## Files likely to change

- TO BE CONFIRMED DURING IMPLEMENTATION: clock abstraction and its registration.
- TO BE CONFIRMED DURING IMPLEMENTATION: expiry evaluation in the validator.
- TO BE CONFIRMED DURING IMPLEMENTATION: grace configuration binding.
- TO BE CONFIRMED DURING IMPLEMENTATION: logging of grace and expiry warnings.

## Files that must NOT change

- Authentication and security middleware.
- Existing time-dependent security code, unless a shared clock abstraction is introduced deliberately and reviewed.
- CI workflow (until 4.11).

## Implementation steps

1. Resolve grace length and post-grace behavior.
2. Define and register the clock abstraction.
3. Implement the timeline table exactly.
4. Implement warnings and operator-visible logging.
5. Add boundary tests.

## Security considerations

- Clock manipulation is expected; the design minimizes its benefit without introducing a denial-of-service vector.
- Expiry must not disable authentication.
- Warnings must not disclose license internals to clients.

## Failure cases

- Hidden permanent grace.
- Grace applied even when not configured.
- Expiry check written with an inclusive comparison, creating a one-second ambiguity.
- Backward clock protection implemented as a stored maximum that permanently locks the license after an accidental future timestamp.
- Timezone conversion applied before comparison.

## Testing requirements

- One tick before `expiresAt`: valid.
- Exactly at `expiresAt`: expired or in grace, per the documented rule.
- Inside grace: `ValidWithWarning` and the warning is emitted.
- At grace end boundary: expired.
- Before `issuedAt`: `NotYetValid`.
- Clock skew within allowance: unchanged behavior.
- Clock moved backward within allowance: unchanged behavior.
- Clock moved backward beyond allowance: documented behavior, no indefinite extension.
- Clock moved forward: immediate expiry.
- No-grace configuration: immediate expiry at `expiresAt`.

## Acceptance criteria

- Expiration, skew and grace rules documented precisely.
- Grace is explicit, bounded, and default-off.
- Post-expiry behavior documented and does not disable security.
- Boundary tests implemented.

## Rollback considerations

Reverting this phase returns expiry to the previous behavior, which for a first implementation means no enforcement. No customer impact before issuance.

## Evidence to record

- Boundary test results.
- Grace configuration values chosen.
- Log samples showing grace warnings without sensitive content.

## Git/commit strategy

- Suggested message: `feat(licensing): enforce expiration and grace rules`.
- No commit without explicit authorization.

## Dependencies on previous phases

- Requires 4.5 and 4.6.

## Risks

- Customer outage at expiry due to an unplanned renewal.
- Grace misunderstood as permanent.
- Clock drift in virtualized environments causing false expiry.

## Deferred items

- Online renewal reminders (4.13).
- Time-source attestation.
- Per-feature expiry.

## Implementation record (Phase 4.7 expiration and grace, 2026-09-11)

| Concern | Implementation |
| --- | --- |
| Time window | `ILicenseExpirationEvaluator` / `LicenseExpirationEvaluator` — the single place the licensed window is decided |
| Timeline state | `LicenseExpirationState` — `Perpetual`, `Active`, `NotYetValid`, `Expired` |
| Typed outcome | `LicenseExpirationStatus` — state, issued/expiry instants, remaining time, `IsWithinGrace`, `IsApproachingExpiry` |
| Grace representation | `LicenseGracePeriod` — `None` by default; `TryCreate` rejects negative durations and durations above `MaximumGracePeriod` (90 days) |
| Expiration bounds | `LicenseExpirationPolicy` — `MaximumGracePeriod` (90 days), `WarningThreshold` (30 days) |
| Clock | Reuses `ILicenseClock`; no `DateTime.Now` and no new ambient time call |

Expiration behavior: `expiresAt` remains exclusive (D4.7-2). Before `expiresAt` the state is `Active`. At or after `expiresAt` the state is `Expired`, and the validator returns `LicenseValidationStatus.Expired` with `LicenseValidationReason.Expired`.

Perpetual behavior: an absent `expiresAt` yields `LicenseExpirationState.Perpetual` at any clock value. A perpetual license never expires, never enters a grace state, is never treated as the Unix epoch or zero, and never becomes restricted merely because expiry is absent.

Clock-skew behavior: the skew allowance applies to `issuedAt` only. Beyond the allowance the state is `NotYetValid`; within it the license is `Active`. Skew never widens the expiry side of the window, so it cannot become a grace period.

Grace-period behavior: grace is explicitly disabled (D-08). `LicenseGracePeriod.None` is the only value used by the validator. When a future approved decision configures a bounded grace period, the evaluator reports `IsWithinGrace` for diagnostics only — the license remains `Expired`, remains unusable, and never gains a commercial capability. No configuration path can enable grace implicitly, and an invalid duration is rejected rather than treated as unlimited.

Restricted-mode behavior: the validator reports `Expired`, and `LicensePolicy.FromValidationResult` maps it to `LicensePolicy.Restricted` — Community edition, no features, no limits, no minimum-edition satisfaction. Restricted mode affects only licensing capability; it does not touch authentication, authorization, TLS, request limits, rate limiting, audit or LDAP.

Validator change: `LicenseValidator` delegates the time checks to the evaluator. The three-argument constructor still works (the evaluator is constructed from the supplied clock when not provided), so existing callers and tests are unaffected.

Tests added: `tests/LabAuthServer.UnitTests/Licensing/Phase47ExpirationAndGracePeriodTests.cs` (28 tests) covering perpetual licenses at several future instants, the `expiresAt` boundary (one tick before, exactly at, one second after, ten years after), the skew boundary on both sides, grace disabled/configured/expired/after-window, invalid and maximum grace durations, the warning signal, expired and not-yet-valid mapping to restricted policy, expiry-before-issuance rejection, and the guarantee that no evaluator path yields a usable status.

Still deferred: configurable grace duration (O-11), the clock-skew value (O-13), online renewal reminders (4.13), time-source attestation, per-feature expiry, machine binding (4.8) and online activation (4.13).