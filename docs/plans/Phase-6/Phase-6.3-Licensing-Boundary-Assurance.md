# Phase 6.3 — Licensing Boundary Assurance

Status: PLANNED — NOT AUTHORIZED FOR IMPLEMENTATION. Required owner/architecture gates are resolved: P6-D3, P6-D4, P6-D5, and P6-D6 are owner approved. This subphase remains planning-only and does not authorize implementation; it is ready for future execution authorization once a separate implementation approval is given.

## Purpose

Verify the actual licensing runtime behavior and reconcile it with the project’s intended commercial claims. This subphase is about correctness, not feature expansion. It is intended to determine whether the current startup-time licensing provider is operating as a safe restricted fallback, and to document exact current behavior for expiry, catalog-unknown identifiers, and runtime enforcement.

## Confirmed current implementation

The implemented licensing subsystem is wired in [src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs](../../../src/LabAuthServer.Api/Extensions/LicenseConfigurationExtensions.cs), and the startup behavior is exercised in [src/LabAuthServer.Api/Program.cs](../../../src/LabAuthServer.Api/Program.cs).

Key current findings confirmed by code:

- `LicenseConfigurationExtensions` registers a `LicensePolicyProvider` and a `SystemLicenseClock`.
- `Program.cs` resolves `ILicensePolicyProvider` once at startup inside a guarded `try/catch` block.
- `LicensePolicyProvider` loads the license file once, validates it, and stores an immutable `ILicensePolicy`.
- Every missing, unreadable, malformed, untrusted, invalid, or expired license maps to `LicensePolicy.Restricted`.
- The provider never throws for a licensing failure; it logs and continues in restricted Community mode.
- Expiry is evaluated inside the validator and expiration evaluator, and the in-memory policy is not refreshed during the process lifetime.

The license contract is documented in [docs/Licensing.md](../../Licensing.md). The runtime behavior is implemented in:

- [src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/LicensePolicyProvider.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/TrustedKeySetFactory.cs)
- [src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs](../../../src/LabAuthServer.Infrastructure/Security/Licensing/LicenseValidator.cs)
- [src/LabAuthServer.Application/Licensing/LicensePolicy.cs](../../../src/LabAuthServer.Application/Licensing/LicensePolicy.cs)
- [src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs](../../../src/LabAuthServer.Application/Licensing/LicenseValidationOptions.cs)

## Scope of investigation

### 1. Restricted-mode initialization failure

The review must determine whether invalid or duplicate trusted-key configuration can leave the DI/runtime licensing provider unavailable even though startup logs suggest restricted mode. The implementation must decide whether a deterministic restricted-mode fail-safe is required, without silently changing the current contract.

### 2. Cached expiry behavior

The current implementation loads the license once at startup and does not re-evaluate it during the process lifetime. The plan must document the exact current behavior when a license is valid at startup but expires later while the process is still running.

### 3. Catalog-unknown identifiers

The plan must determine the exact behavior for:

- syntactically valid but catalog-unknown feature IDs;
- syntactically valid but catalog-unknown limit IDs;
- validation result;
- policy access result;
- issuance behavior.

This must be documented without changing the frozen Version 1 contract.

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

## Decision gates required before implementation

- What is the exact desired restricted-mode fail-safe when trusted-key configuration is invalid or duplicated?
- Is startup-cached expiry acceptable as a documented contract, or is a re-evaluation policy required?
- Should unknown catalog entries fail validation, fail policy generation, or be treated as restricted without a new version contract?
- Which commercial capability claims are valid to advertise against the actual enforced runtime policy?

## External/professional dependencies

- Product/licensing owner for commercial feature claims.
- Architecture review if behavior changes are proposed.
- Legal/commercial review if the repo or docs claim more enforcement than the runtime actually guarantees.

## Narrow implementation scope

This plan is limited to analysis, current-state correction, and a precise future implementation packet. It does not expand the licensing model into online activation, online revocation, machine binding, DRM, billing, or customer portal functions.

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

## Required automated validation

Implementation must include validation for:

- valid license startup path;
- missing/unreadable/oversized/malformed license path;
- invalid signature and unknown key path;
- expired license path;
- startup-cached expiry path while the process remains alive;
- unknown feature and unknown limit IDs;
- restricted provider initialization under invalid configuration;
- policy access and feature denial behavior;
- no regression to authentication/authorization security semantics.

## Acceptance criteria

The future implementation is acceptable only if all of the following are true:

- current runtime behavior is accurately documented;
- restricted mode is deterministic and safe when the provider is invalid or misconfigured;
- expiry behavior for a running process is explicit and approved;
- unknown features and unknown limits are classified correctly;
- commercial claims match actual runtime enforcement;
- no online licensing or DRM expansions are introduced.

## Implementation Authorization Packet

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

### Owner/architect decisions required first

- restricted-mode fail-safe contract;
- expiry cadence and re-evaluation decision;
- exact treatment of unknown catalog entries;
- commercial claim boundary for actual enforcement.

### External dependencies

- licensing owner and architecture sign-off;
- possibly legal/commercial review if commercial claims are revised.

### Safety boundaries

- no production license issuance or customer-facing activation flow;
- no production signing-key use or runtime configuration change; 
- no new licensing architecture beyond the approved offline contract.

### Recommended signed commit message

Plan remaining Phase 6 work

---

This subphase remains planning-only and does not authorize implementation.
