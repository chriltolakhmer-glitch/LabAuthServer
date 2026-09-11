# Phase 4.13 — Online Activation Architecture (Future)

Status: FUTURE, OPTIONAL — DESIGN ONLY. No production code added. [Phase 4 README](Phase-4-README.md) | Previous: [4.12](Phase-4.12-Operational-License-Management.md).

> This phase is design and documentation only. It implements nothing, defines no production network client, adds no endpoint, adds no telemetry, changes no license-format field and changes no offline behavior. Every statement below is labelled **IMPLEMENTED TODAY**, **APPROVED DESIGN**, **FUTURE OPTION**, **OPEN DECISION** or **DEFERRED**. Where policy is not approved it is marked OPEN rather than decided.

## Objective

Design an optional future online activation and revocation service, while keeping offline signed-license validation as the foundation.

## Scope

In scope:

- Service architecture for online activation.
- Capabilities that online activation uniquely enables.
- Compatibility rules with the offline path.
- Privacy and connectivity constraints.

Out of scope:

- Any implementation.
- Making the first release depend on Internet connectivity.
- Telemetry policy decisions.

## Why it exists

Offline licenses cannot be revoked and cannot enforce activation counts. If those become commercial requirements, the design must be ready and must not require redesigning the offline path.

## Prerequisites

- 4.5 through 4.12 complete and operating.
- A business requirement that justifies operating a service.

## Inputs

- Customer deployment topologies, including air-gapped environments.
- Legal and privacy constraints on telemetry.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.13-1 | Offline signed-license validation remains the foundation | FINAL (constraint) | Customers may be air-gapped |
| D4.13-2 | Online activation is additive and optional | PROPOSED | No dependency for core operation |
| D4.13-3 | Failure to reach the service must not degrade below offline behavior | PROPOSED | Connectivity is not a licensing control |
| D4.13-4 | Revocation requires online activation and is documented as such | PROPOSED | Cannot be enforced offline |
| D4.13-5 | Telemetry is collected only if legally appropriate and disclosed | PROPOSED | Privacy |
| D4.13-6 | Activation responses are signed and verifiable by the server | PROPOSED | Prevents a trivial local spoof |

DECISION REQUIRED: Whether online activation is pursued at all. Recommendation: defer until a commercial requirement exists.

DECISION REQUIRED: Data residency and retention for activation records. Recommendation: minimal data, documented retention, no personal data beyond what the contract requires.

## Proposed architecture

```
LabAuthServer
      |
      | HTTPS (optional, outbound)
      v
License Service
      |
      v
License Database
```

Capabilities that online activation can add:

| Capability | Offline equivalent |
| --- | --- |
| Activation count enforcement | None |
| Revocation | None |
| Renewal without reissuing a file | Manual reissue |
| Customer portal showing license status | Manual support |
| Telemetry, if legally appropriate | None |
| Rebind after hardware change | Manual reissue |

Compatibility rules:

- The license file remains the primary artifact.
- An online check may add a status, never replace signature verification.
- If the service is unreachable, behavior falls back to the offline result exactly.
- Cached online state has a documented maximum age.
- The offline path must remain fully testable without the service.

## Files likely to change

- None in this phase (planning only).
- If pursued: TO BE CONFIRMED DURING IMPLEMENTATION: a client for the activation service, configuration for the service endpoint, and a new service component outside this repository.

## Files that must NOT change

- Authentication and security controls.
- Existing offline validation behavior, except as explicitly amended by an approved decision.
- CI workflow, unless the new service introduces its own pipeline.

## Implementation steps

1. Confirm whether a commercial requirement exists.
2. If yes, define the service contract, data model and privacy policy.
3. Define the outage and fallback behavior precisely.
4. Define the security model for the client-to-service channel.
5. Record decisions, then produce a separate detailed implementation plan.

## Security considerations

- The activation channel is a new outbound trust relationship and must be authenticated both ways where feasible.
- A compromised service must not be able to grant more than the signed license permits.
- Do not send customer-identifying data unless required and disclosed.
- Do not let a network failure become a denial of service for the product.

## Failure cases

- Service outage causing licensed features to stop working, contradicting the offline foundation.
- Activation response replay.
- Unauthenticated activation endpoint allowing forged activation.
- Telemetry collection without disclosure.

## Testing requirements

- Offline behavior unchanged when the service is unreachable.
- Service unreachable does not degrade below offline behavior.
- Replayed activation response rejected.
- Forged activation response rejected.
- Offline test suite passes with no service configured.

## Acceptance criteria

- Architecture documented without implementing anything.
- Offline foundation preserved as the primary mechanism.
- Fallback behavior documented exactly.
- Capabilities only online activation can provide are listed.

## Rollback considerations

Not applicable while deferred. If implemented, the service must be removable without affecting offline validation.

## Evidence to record

- Decision to pursue or defer.
- If pursued, the service contract and privacy review.

## Git/commit strategy

- Documentation only in this phase.
- Suggested message: `docs(phase-4): add technical license enforcement plan`.

## Dependencies on previous phases

- Requires 4.5, 4.7 and 4.12. Not a prerequisite for 4.14 or 4.15.

## Risks

- Introducing a connectivity dependency by accident.
- Privacy or contractual exposure from telemetry.
- Operational cost of running a service.

## Deferred items

- The service itself.
- Customer portal.
- Revocation workflow automation.

---

## Implementation record (2026-09-11)

Status: DESIGN ONLY — NO PRODUCTION CODE, NO NETWORK CLIENT, NO ENDPOINT, NO TELEMETRY.

### 1. Current offline-first behavior (IMPLEMENTED TODAY)

The following is verified in the current source tree and is unchanged by Phase 4.13:

- Validation is a pure function of license bytes plus the trusted public-key set. No network call exists on the licensing path.
- `LicenseValidator` (`src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs`) applies the documented order (input → parse → algorithm → trusted key → signature → product → edition → issuedAt/time → features → limits → expiry) with no I/O beyond the supplied bytes.
- No `HttpClient`, `IHttpClientFactory`, `AddHttpClient`, `HttpListener`, `TcpListener`, `WebApplication.CreateBuilder` in a licensing context exists in the licensing code. The only `WebApplication.CreateBuilder` in the repository is the API host itself (`src/LabAuthServer.Api/Program.cs`); the only `Socket` usage is LDAP transport error classification, which is unrelated to licensing.
- No licensing endpoint, no telemetry, no outbound call, no activation token, no revocation field and no customer/installation/machine identifier exists in the licensing code.
- Missing, invalid or expired licenses enter Community/restricted mode (D-03, D-14). Startup never blocks on a licensing service because there is no licensing service.

### 2. Authorization status of Phase 4.13 (classification)

Phase 4.13 is **explicitly a future/design phase**. D-10 (online activation: optional future capability) and D-22 (revocation only via future online activation) are APPROVED as deferrals. D4.13-1 is FINAL (constraint); D4.13-2 through D4.13-6 remain PROPOSED. There is no approved requirement anywhere in the Phase 4 plan that requires production online functionality in this phase. Therefore Phase 4.13 does **not** implement anything, and the stop-and-report clause did not trigger.

### 3. Activation model (FUTURE OPTION — OPEN DECISION)

A future activation flow would add, at minimum: a client that presents proof of the signed license, a service that records an activation, a signed response the server can verify, and a policy that decides what the activation grants. None of these are approved. Design questions left OPEN: protocol shape (see 4.13-O-01), client authentication (4.13-O-03), customer identity (4.13-O-04), installation identity (4.13-O-05), endpoint ownership (4.13-O-07), API version (4.13-O-08).

Hard constraints if it is ever pursued:

- An activation response may only **add** a status; it may never replace signature verification of the license.
- A compromised activation service must not be able to grant more than the signed license permits.
- Activation must be optional; a deployment that never enables it must behave exactly as today.

### 4. Deactivation model (FUTURE OPTION — OPEN DECISION)

A future deactivation (release of an activation slot) is a commercial bookkeeping action, not a security control. Design questions left OPEN: whether deactivation is best-effort, whether it requires an authenticated operator action, and how it behaves when the service is unreachable (see 4.13-O-09). Deactivation cannot make an offline license stop working.

### 5. Revocation model options (FUTURE OPTION — OPEN DECISION)

No revocation model is selected. The table below records the trade-offs. Revocation cannot be enforced offline (D-22); every option depends on some future online component.

| Model | Security properties | Availability impact | Privacy implications | Offline behavior | Replay considerations | Operational complexity | Key-management requirements |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Online status lookup (per check) | Server-authoritative; hard revoke | Needs service for every check | Server sees each check | Cannot revoke while offline | Needs nonce/timestamp | Highest (always-on service) | Service signing + TLS key |
| Signed revocation list (offline, cached) | Vendor-signed; verifiable offline | Only needs periodic fetch | Minimal; only fetch metadata | Can honour a cached list while offline | Needs freshness window to prevent stale reuse | Medium | Vendor list-signing key + rotation |
| Periodically refreshed revocation metadata | Same as signed list, shorter lifetime | Needs periodic fetch | Minimal | Honours last cached state | Needs clock/freshness handling | Medium | Vendor signing key + rotation |
| Short-lived online authorization | Server-authoritative; strongest timing | Needs frequent service reachability | Frequent checks | Degrades to offline behaviour after expiry | Needs nonce/timestamp and short TTL | High | Service signing key + rotation |
| Vendor-issued revocation statement (per license) | Vendor-signed statement; verifiable offline | Only needs delivery | Minimal | Honours delivered statement offline | Needs statement identity/uniqueness | Low–Medium | Vendor signing key + rotation |

Observations that hold for every model:

- All of them depend on the vendor's key custody (see section 9). None of them requires the customer to hold a private signing key.
- None of them is a substitute for authentication or transport security; a revocation model must never disable or weaken authentication (D-15).
- A revoked state must fail closed for the licensed feature it applies to, while never refusing to start the application (D-03).

### 6. License status checking (FUTURE OPTION — OPEN DECISION)

A future status check would need an explicit cadence, a cached result with a documented maximum age, and a defined behaviour when the cached result is stale. Cadence interacts with O-09 (validation cadence) and must not contradict the offline foundation. No cadence is approved here (see 4.13-O-11, 4.13-O-12).

### 7. Offline operation requirement (APPROVED DESIGN — D4.13-1)

The offline path is the foundation and must remain the primary mechanism. A future online feature must be additive: a deployment that never configures a service must behave exactly as it does today, with no new required configuration, no new required network access and no change to Community/Professional/Enterprise behaviour (4.13-O-18).

### 8. Temporary network unavailability (FUTURE OPTION — OPEN DECISION)

Failure modes that a future implementation would have to define explicitly:

| Scenario | Candidate behaviour (NOT selected) |
| --- | --- |
| Service unreachable for a short time | Continue with cached valid state until the documented maximum age |
| Service unreachable for a long time | Bounded continuation, then fall back to the offline result exactly |
| Service permanently unavailable | Fall back to the offline signed-license result exactly |
| First run with no cached state and no service | Offline result only; no failure caused by the missing service |
| Service returns an error | Treat as unreachable; never widen entitlement |

No behaviour is selected. D4.13-3 states as a PROPOSED constraint that failure to reach the service must not degrade below offline behaviour; it is not yet APPROVED. The corresponding OPEN decisions are 4.13-O-12 (network outage behaviour) and 4.13-O-13 (offline grace behaviour).

### 9. Server availability failure and business trade-offs (FUTURE OPTION — OPEN DECISION)

Documented trade-offs, none selected:

- **Continue indefinitely with cached state** — best availability; weakest revocation freshness; a revoked license keeps working for as long as the cache is honoured.
- **Continue for a bounded period** — balanced; introduces a clock/freshness dependency (see section 27).
- **Enter restricted mode on staleness** — strongest revocation freshness; converts a vendor outage into a customer-visible licensing event, which the offline foundation deliberately avoids (D-03).
- **Require manual offline renewal** — no connectivity dependency; highest operational cost (see Phase 4.12 procedures).

The recommended posture for any future pursuit is availability-preserving (the product must never fail because the vendor's service failed), with revocation freshness as a bounded, documented property. This is a recommendation, not an approved decision (4.13-O-13, 4.13-O-19).

### 10. License server authentication and client authentication (FUTURE OPTION — OPEN DECISION)

Both directions would need explicit design. Client authentication method (for example mutual TLS, a customer-specific credential, or an activation credential issued alongside the license) is OPEN (4.13-O-03). Server authentication is mandatory in any design that returns a signed response and is constrained by D4.13-6 (PROPOSED): responses are signed and verifiable by the server. Credential storage and lifetime are OPEN (4.13-O-10).

### 11. Request authentication, replay protection and request signing (FUTURE OPTION — OPEN DECISION)

A future request format would need nonce/timestamp design, request signing or equivalent, and a server-side replay window. Replay protection cannot be evaluated in isolation from the revocation model (section 5): an offline cached revocation list needs freshness handling that a per-request online lookup does not. Specific mechanism, nonce lifetime and clock assumptions are OPEN (4.13-O-01, 4.13-O-15).

### 12. TLS, certificate validation and server identity (FUTURE OPTION — OPEN DECISION)

Any future channel must use TLS with certificate validation; a pinned or custom trust anchor policy is OPEN (4.13-O-09b). Server identity must be verified against the expected service identity; the exact trust model and any pinning policy are OPEN. No TLS client code exists today.

### 13. API versioning and endpoint ownership (FUTURE OPTION — OPEN DECISION)

API version policy, backward-compatibility rules and endpoint ownership are OPEN (4.13-O-07, 4.13-O-08). A future license-format version bump (if ever needed) is a separate versioned decision (section 12).

### 14. Rate limiting and abuse protection (FUTURE OPTION — OPEN DECISION)

Rate limits, per-customer quotas, activation-count limits and abuse handling are OPEN (4.13-O-16, 4.13-O-17). Denial-of-service risk to the vendor service is a vendor-side operational concern and must not become a customer-side availability dependency.

### 15. Audit logging (FUTURE OPTION — OPEN DECISION)

What a future service would log (activation events, deactivation events, revocation statements, service errors) and what retention applies is OPEN (4.13-O-18b). Phase 4.12 already records that the current server has no licensing audit surface; Phase 4.13 does not add one.

### 16. Privacy considerations (FUTURE OPTION — RECOMMENDATION)

Data that a future activation service could collect, with the recommendation that every field below is OPEN and should be minimized:

| Field | Purpose | Recommendation |
| --- | --- | --- |
| License identifier | Bookkeeping | Likely required; retention OPEN |
| Customer identifier | Commercial mapping | Only if contractually required |
| Installation identifier | Activation counting | Design OPEN (4.13-O-05) |
| Machine identifier | Not applicable while binding is deferred | Do not collect |
| Activation timestamp | Bookkeeping | Likely required |
| IP address | Operational | Minimize; retention OPEN |
| Server version | Compatibility diagnostics | Minimize |
| Product / edition | Bookkeeping | Likely required |

Data minimization and an explicit retention policy are recommended as future decisions (4.13-O-14). No telemetry is introduced now (D4.13-5, PROPOSED).

### 17. Customer, license, installation and machine identity (FUTURE OPTION — OPEN DECISION)

These four identities are distinct and must not be conflated:

- **License identity** — the identity of the signed license document itself (for example the `keyId` and any future license identifier). IMPLEMENTED TODAY only as far as the signed envelope's `keyId`.
- **Customer identity** — the commercial entity. FUTURE OPTION; OPEN (4.13-O-04).
- **Installation identity** — the identity of a running deployment. FUTURE OPTION; OPEN (4.13-O-05).
- **Machine identity** — the identity of a physical or virtual host. DEFERRED (D-09). Online activation does **not** solve machine binding; see section 22.

### 18. Key rotation (APPROVED DESIGN — D-12)

The server already trusts a key set rather than a single key (D-12; `InMemoryTrustedLicenseKeyProvider`). Any future online component must respect the same rotation model for any key it uses (service response-signing key, revocation-list-signing key). Rotation procedure details are OPEN (4.13-O-20).

### 19. Vendor key custody (APPROVED DESIGN — D-16, D-17, D-18)

A future online infrastructure must never require the customer or the server to possess the vendor private signing key. The boundary is unchanged from Phase 4.12:

| Role | Responsibilities |
| --- | --- |
| Vendor | Signing authority, activation service, revocation authority, key custody |
| Customer | License, public verification material, activation credentials only if future policy requires them |
| Server | Verification, local enforcement, cached status only if future policy permits |

No production vendor private key is generated, stored or referenced by this phase.

### 20. Revocation distribution (FUTURE OPTION — OPEN DECISION)

Distribution mechanism follows from the chosen revocation model (section 5). Delivery, freshness window, cache invalidation and offline applicability are OPEN (4.13-O-02, 4.13-O-13b).

### 21. Cache behaviour (FUTURE OPTION — OPEN DECISION)

Any future cached online state needs a documented maximum age, a defined behaviour for stale state and a clear precedence against the offline validation result. Cache lifetime is OPEN (4.13-O-11).

### 22. Machine binding relationship (DEFERRED — D-09)

Machine binding remains DEFERRED. Online activation does **not** automatically solve machine binding, and Phase 4.13 does not add binding fields to the current license format or any server-side binding check. If binding is ever pursued, it is a separate approved decision with its own rebind procedure (Phase 4.8 recorded the analysis).

### 23. License-format impact (APPROVED DESIGN — unchanged)

The current license document format is unchanged. No activation token, server URL, device ID, customer ID, revocation field or online status field is added. Any future license-format change (if ever needed) is a versioned decision with its own migration strategy (4.13-O-21).

### 24. Clock manipulation (APPROVED DESIGN — Phase 4.7 unchanged; FUTURE OPTION — OPEN)

Phase 4.7 clock behaviour is unchanged: `LicenseExpirationEvaluator` is the single decision point, `expiresAt` is exclusive, and clock skew applies to `issuedAt` only. No new clock-skew value is invented here (O-13 remains OPEN). A future online feature would need to define how it reacts to clock manipulation without widening the licensed time window; that design is OPEN (4.13-O-15).

### 25. Recovery behaviour (FUTURE OPTION — OPEN DECISION)

Recovery behaviour for a future activation state (for example after a restore or a service outage) is OPEN. The existing operational recovery procedure for the license file itself is already documented in Phase 4.12 and is unchanged.

### 26. Security requirements for a future channel (FUTURE OPTION — RECOMMENDATION)

A future design should cover: TLS with certificate validation; server identity verification; client authentication; authorization; replay prevention with nonce/timestamp; request signing or an equivalent integrity control; rate limiting; abuse handling; credential storage; token lifetime; key rotation; audit trails; privacy; and denial-of-service resistance. None of these mechanisms is implemented in the current application. The list is a design checklist, not a set of approved requirements.

### 27. Fail-open vs fail-closed (OPEN DECISION)

The existing offline behaviour is fail-closed on the licensing path (D-14) and availability-preserving on startup (D-03). A future online feature would need an explicit, documented decision for each failure mode: service unreachable, service error, replayed response, invalid response signature, stale cache, and clock manipulation. None is selected here (4.13-O-12).

### 28. Source-available limitation (APPROVED CONSTRAINT — unchanged)

A source-available client cannot be trusted to enforce online activation against a customer who controls the source code, the binaries, the host and administrator privileges. Future online licensing can improve commercial control and revocation but cannot make bypass mathematically impossible. This phase does not propose anti-debugging, anti-tamper theatre, kernel drivers, process scanning or obfuscation as a primary security boundary. The approved constraint recorded in [4.9](Phase-4.9-Tamper-and-Abuse-Resistance.md) applies verbatim.

### 29. Open decisions list

| ID | Decision | Status |
| --- | --- | --- |
| 4.13-O-01 | Activation protocol (message shape, transport, versioning of the wire format) | OPEN |
| 4.13-O-02 | Revocation model (see section 5; none selected) | OPEN |
| 4.13-O-03 | Client authentication method | OPEN |
| 4.13-O-04 | Customer identity model | OPEN |
| 4.13-O-05 | Installation identity model | OPEN |
| 4.13-O-06 | Machine identity model | DEFERRED (D-09) |
| 4.13-O-07 | API endpoint ownership and hosting | OPEN |
| 4.13-O-08 | API version policy | OPEN |
| 4.13-O-09 | TLS policy (trust anchors, pinning) | OPEN |
| 4.13-O-09b | Server identity verification policy | OPEN |
| 4.13-O-10 | Credential storage and token lifetime | OPEN |
| 4.13-O-11 | Cache lifetime and maximum age | OPEN |
| 4.13-O-12 | Network outage behaviour (fail-open vs bounded vs fail-closed) | OPEN |
| 4.13-O-13 | Offline grace behaviour when online state is stale | OPEN |
| 4.13-O-13b | Revocation freshness window | OPEN |
| 4.13-O-14 | Privacy, data minimization and retention policy | OPEN |
| 4.13-O-15 | Clock manipulation handling | OPEN |
| 4.13-O-16 | Rate limits | OPEN |
| 4.13-O-17 | Abuse handling | OPEN |
| 4.13-O-18 | Licensing service availability requirements (SLO / vendor-side) | OPEN |
| 4.13-O-18b | Audit requirements for the future service | OPEN |
| 4.13-O-19 | Continue-with-cached-state posture (recommended, not approved) | OPEN |
| 4.13-O-20 | Key rotation procedure for online components | OPEN |
| 4.13-O-21 | License format/version changes | OPEN |
| 4.13-O-22 | Pursuit-or-defer decision (O-19 in the Decision Log) | RESOLVED — DEFERRED |

No OPEN decision is resolved by this document.

### 30. Deferred / skipped work (explicit)

| Item | Reason |
| --- | --- |
| Activation service, activation client, activation endpoints | Phase 4.13 is design-only; no approved requirement asks for them now |
| Online revocation (all models in section 5) | D-22: revocation is not part of the initial offline-only implementation |
| Licensing server, HTTP/REST licensing endpoints | Prohibited by this task and by the offline-first decision (D-02) |
| Telemetry | D4.13-5 remains PROPOSED; no telemetry introduced |
| Automatic license download | Not authorized |
| Mandatory network connectivity | Prohibited; would contradict D-02 and D4.13-1 |
| Machine binding | DEFERRED (D-09) |
| License-format changes (activation token, server URL, device ID, customer ID, revocation field, online status field) | Not authorized; requires a versioned decision (4.13-O-21) |
| Anti-debugging, obfuscation, kernel drivers, process scanning | Prohibited by the source-available constraint (section 28) |
| Production vendor private key generation or storage | Prohibited (D-16, D-17, D-18) |
| Deployment, release publishing | Out of scope |

### 31. Security scan of changed files

Searched the changed documentation files for `HttpClient`, `HttpListener`, `TcpListener`, `Socket`, `WebApplication`, `AddHttpClient`, `activation`, `revocation`, `endpoint`, `token`, `credential`, `password`, `secret`, `private key`, `production key`. All matches are descriptive references to concepts, prohibitions or future options. No production network code, no network dependency in licensing, no secret and no production private key was introduced. The repository-wide search confirmed that no `HttpClient`/`IHttpClientFactory`/`AddHttpClient`/`HttpListener`/`TcpListener` exists in the licensing code; the only `WebApplication.CreateBuilder` is the API host itself, and the only `Socket` usage is LDAP transport error classification.

### 32. Final validation

- No production code was changed in Phase 4.13.
- Release build: succeeded, 0 warnings, 0 errors.
- Full test suite with the documented validation command `dotnet test LabAuthServer.slnx -c Release --no-build -m:1`: Unit 745 passed, Integration 246 passed; total 991, 0 failed, 0 skipped. The `-m:1` form is required because of the pre-existing TEST-ISOLATION-1 finding recorded in Phase 4.12; it is not fixed here.
- `git diff --check`: clean.
- Offline-first behaviour is unchanged. No network dependency, no endpoint, no telemetry, no secret and no production private key was introduced.
- The document distinguishes IMPLEMENTED TODAY, APPROVED DESIGN, FUTURE OPTION, OPEN DECISION and DEFERRED throughout.

### Findings

| ID | Finding | Severity | Disposition |
| --- | --- | --- | --- |
| 4.13-F-1 | Design only; no online functionality implemented. Risk of future drift if a follow-up phase implements something without resolving the OPEN decisions. | Informational | Recorded; each OPEN decision must be resolved before any implementation phase |
| 4.13-F-2 | A future revocation model's freshness window is a clock-dependent property and inherits the residual clock-manipulation risk (R-05). | Low | Recorded for the future design |
| 4.13-F-3 | Source-available bypass remains possible regardless of any online component; online activation cannot be presented as unbreakable. | Informational | Consistent with the approved constraint in 4.9 |