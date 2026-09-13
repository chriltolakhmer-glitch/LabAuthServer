# Phase 6.3 — Licensing Boundary Assurance

Status: IMPLEMENTED — PHASE 6.3 COMPLETE. P6-D3, P6-D4, P6-D5, and P6-D6 were owner approved before implementation. No Phase 6.4–6.7 work was performed.

## Implementation record

The approved Phase 6.3 licensing boundaries are implemented within the existing offline licensing architecture:

- Invalid, missing, unreadable, malformed, expired, untrusted, or unexpectedly failing licensing initialization returns an explicit restricted Community policy and emits only safe operator diagnostics.
- Invalid or duplicate trusted-key configuration is reported as invalid; the API registration discards the partially populated key set and uses an empty trusted set, preventing a malformed configuration from validating a commercial license.
- A valid license is checked against its expiration on every `ILicensePolicyProvider.GetPolicy()` call. Once expired or not yet valid, the provider deterministically returns restricted Community mode without requiring a process restart.
- Structurally valid but catalog-unknown feature and limit identifiers remain compatible with Version 1 validation and are denied at runtime policy access. Known catalog behavior is unchanged.
- Commercial enforcement claims remain limited to capabilities demonstrated by runtime policy enforcement. This change does not finalize legal terms, issue customer licenses, or perform production licensing operations.
- `LabAuthServer.LicenseIssuer` is already included through the existing unit-test project reference and was built by the solution qualification command; no Phase 6.4 solution or release-qualification change was made.

Files changed for this subphase are recorded by the implementation commit and include the licensing provider/configuration, trusted-key handling, licensing contract documentation, and focused licensing tests.

Validation evidence:

- Focused licensing tests: 293 passed, 0 failed, 0 skipped.
- Full restore, solution build, and solution test validation are required before commit; infrastructure-dependent tests remain isolated under the Phase 6.1 rules.

Remaining limitations:

- Licensing remains offline and restricted-mode based; there is no activation, revocation, machine binding, billing, customer portal, or DRM service.
- Legal, commercial, copyright, retention, and support reviews remain external gates.
- No customer license was issued and no production license was deployed.

## Purpose

Verify the actual licensing runtime behavior and reconcile it with the project’s intended commercial claims. This subphase is about correctness, not feature expansion. It is intended to determine whether the current startup-time licensing provider is operating as a safe restricted fallback, and to document exact current behavior for expiry, catalog-unknown identifiers, and runtime enforcement.

## Confirmed current implementation

The implemented licensing subsystem is wired in [src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs](../../../src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs), and the startup behavior is exercised in [src/LabAuthServer.Api/Program.cs](../../../src/LabAuthServer.Api/Program.cs).

Key current findings confirmed by code:

- `LicenseConfigurationExtensions` registers a `LicensePolicyProvider` and a `SystemLicenseClock`.
- `Program.cs` resolves `ILicensePolicyProvider` at startup inside a guarded `try/catch` block.
- `LicensePolicyProvider` loads and validates the license at startup, then re-evaluates expiration on every policy access.
- Every missing, unreadable, malformed, untrusted, invalid, or expired license maps to `LicensePolicy.Restricted`.
- The provider never throws for a licensing failure; it logs and continues in restricted Community mode.
- Expiry is evaluated inside the validator and expiration evaluator, and an expired running-process policy becomes restricted on the next policy access.

The license contract is documented in [docs/Licensing.md](../../Licensing.md). The runtime behavior is implemented in:

- [src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs)
- [src/LabAuthServer.Application/Licensing/LicensePolicy.cs](../../../src/LabAuthServer.Application/Licensing/LicensePolicy.cs)
- [src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs](../../../src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs)

## Implemented boundary decisions

### 1. Restricted-mode initialization failure

Invalid or duplicate trusted-key configuration cannot grant a license: configuration is marked invalid and the registered verifier receives an empty trusted set, while normal policy resolution remains available in restricted mode.

### 2. Cached expiry behavior

The provider evaluates the validated license expiration on each `GetPolicy()` call. A license valid at startup becomes restricted after expiry while the process remains running.

### 3. Catalog-unknown identifiers

The plan must determine the exact behavior for:

- syntactically valid but catalog-unknown feature IDs;
- syntactically valid but catalog-unknown limit IDs;
- validation result;
- policy access result;
- issuance behavior.

This preserves the frozen Version 1 contract: structural validation remains permissive for compatible identifiers, while policy access remains deny-by-default for unknown catalog entries.

### 4. Commercial enforcement reality

This subphase must separate the following:

- technically available license policy APIs;
- actual runtime enforcement in the application;
- planned but not implemented commercial features;
- actual usage-limit enforcement currently in place.

## Confirmed findings to verify against evidence

- The project intends to preserve a frozen Version 1 licensing contract.
- Valid and invalid licenses are normalized to a restricted `LicensePolicy`.
- The system is intentionally offline and does not perform online activation or revocation.
- The project explicitly does not implement machine binding or customer portal workflows.

## Dependencies on earlier work

- Phase 6.1 provides safe validation boundaries.
- Policy semantics are not an operational release decision by themselves; they are a licensing correctness decision.
- The actual runtime enforcement picture must be verified before any behavior-changing claim is made.

## Approved decisions implemented

- P6-D3: invalid initialization fails safe to usable restricted mode with no commercial grant and safe diagnostics.
- P6-D4: expiry is continuously re-evaluated during runtime and expired state is restricted deterministically.
- P6-D5: structurally valid unknown identifiers remain compatible at validation but are denied by runtime policy access.
- P6-D6: only demonstrated runtime enforcement supports a commercial capability claim.

## External/professional dependencies

- Product/licensing owner for commercial feature claims.
- Architecture review if behavior changes are proposed.
- Legal/commercial review if the repo or docs claim more enforcement than the runtime actually guarantees.

## Narrow implementation scope

This implementation is limited to current-state correction and the approved runtime licensing boundaries. It does not expand the licensing model into online activation, online revocation, machine binding, DRM, billing, or customer portal functions.

## Explicit non-goals

- online activation;
- online revocation;
- machine binding;
- billing or customer portal;
- licensing server;
- new DRM system;
- changing the Version 1 contract without explicit approval.

## Expected files/components

Likely files and areas of review include:

- [src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs](../../../src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs)
- [src/LabAuthServer.Api/Program.cs](../../../src/LabAuthServer.Api/Program.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs)
- [src/LabAuthServer.Application/Licensing/LicensePolicy.cs](../../../src/LabAuthServer.Application/Licensing/LicensePolicy.cs)
- [src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs](../../../src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs)
- [docs/Licensing.md](../../Licensing.md)
- relevant unit tests under the licensing test area

## Automated validation performed

Validation includes:

- valid license startup path;
- missing/unreadable/oversized/malformed license path;
- invalid signature and unknown key path;
- expired license path;
- runtime expiry path while the process remains alive;
- unknown feature and unknown limit IDs;
- restricted provider initialization under invalid and duplicate-key configuration;
- policy access and feature denial behavior;
- no regression to authentication/authorization security semantics.

## Acceptance results

All of the following are true:

- current runtime behavior is accurately documented;
- restricted mode is deterministic and safe when the provider is invalid or misconfigured;
- expiry behavior for a running process is explicit, approved, and continuously enforced;
- unknown features and unknown limits are classified correctly;
- commercial claims match actual runtime enforcement;
- no online licensing or DRM expansions are introduced.

## Implementation authorization record

### Baseline prerequisites

- Phase 6.1 is verified and clean.
- Current licensing behavior has been inspected and confirmed from the code and tests.
- All licensing behavior changes are explicitly classified as offline, restricted-mode, and non-DRM.

### Exact implementation scope

- restricted-mode initialization safety design;
- exact expiry contract for a running process;
- unknown feature/limit handling documentation and behavior correction;
- documentation alignment on actual enforced commercial capabilities.

### Explicit non-goals

- online activation or revocation;
- machine binding;
- billing or customer portal;
- licensing server;
- DRM expansion.

### Expected files/components

- licensing provider, validator, and policy code;
- relevant tests;
- docs reconciliation for licensing behavior and claims.

### Tests/validation

- license success/failure matrix;
- expiry and startup semantics tests;
- unknown catalog ID regression tests;
- restricted-mode fail-safe tests.

### Acceptance criteria

- no unapproved behavior change beyond correct restricted-mode and documentation alignment;
- owner approval is recorded before any semantic licensing behavior change.

### Owner/architect decisions resolved before implementation

- P6-D3 restricted-mode fail-safe contract;
- P6-D4 continuous expiry re-evaluation decision;
- P6-D5 unknown catalog identifier policy;
- P6-D6 commercial claim boundary for actual enforcement.

### External dependencies

- licensing owner and architecture sign-off;
- possibly legal/commercial review if commercial claims are revised.

### Safety boundaries

- no production license issuance or customer-facing activation flow;
- no production signing-key use or runtime configuration change; 
- no new licensing architecture beyond the approved offline contract.

### Signed commit message

Implement Phase 6.3 licensing boundaries

---

This subphase is complete. No Phase 6.4–6.7 implementation or release activity is authorized by this record.
