# Historical Architecture Decision Record

> This record documents the Phase 4 approval history. It is not current implementation guidance; current behavior is documented in `docs/Project_Status.md` and `docs/Validation_Status.md`.

# Phase 4 Architect Approval Record

**Status:** APPROVED  
**Phase:** Phase 4 — Token & Authorization  
**Approval basis:** `docs/Token_and_Authorization_Design.md`  
**Approval date:** 2026-08-31

## Executive Approval Statement

The recommended Phase 4 baseline in `docs/Token_and_Authorization_Design.md` is approved for design and implementation planning. The 49 decisions below are approved using the documented recommendations.

This approval authorizes implementation of the approved design direction only. It does not authorize uncontrolled scope expansion, reinterpretation of the recommendations, or implementation of excluded features. Any departure from an approved recommendation requires a new architect decision before implementation.

## Approved Architectural Baseline

- LabAuthServer issues JWT bearer access tokens after successful Phase 3 LDAP authentication.
- LabAuthServer is the token issuer; no external identity provider is used.
- RSA asymmetric signing is used with protected externally managed signing keys.
- The Application layer owns token issuance use cases; Infrastructure provides signing and key access; API validates tokens and applies authorization policies.
- Authorization uses application-owned roles mapped from explicitly approved Active Directory groups.
- Access tokens are short-lived and stateless initially.
- No refresh tokens are implemented initially; clients reauthenticate.
- No database-backed revocation is implemented initially.
- The existing Phase 3 LDAPS authentication behavior remains unchanged.

## Approved Decisions

### Token Contract and Trust

### Decision 1 — Access Token Architecture
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use JWT bearer access tokens.  
**Basis:** `Token_and_Authorization_Design.md` recommendation for JWT local validation when the API remains the issuer.

### Decision 2 — Token Issuer Model
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** LabAuthServer is a self-issued token authority; do not use an external identity provider.  
**Basis:** `Token_and_Authorization_Design.md` recommendation for the current single-service scope.

### Decision 3 — Canonical Issuer
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a stable HTTPS issuer identifier owned by LabAuthServer.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 4 — Environment-Specific Issuer
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use distinct issuer values per deployment environment.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 5 — Audience Identifier
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use one stable audience for the LabAuthServer API.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 6 — Multiple Audiences
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Reject multiple audiences initially.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 7 — Subject Identifier
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a stable directory object identifier when available; otherwise use the normalized UPN.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 8 — Subject Normalization
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Compare UPN values case-insensitively and do not expose unnecessary directory data.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 9 — Active Directory Revalidation
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Do not revalidate identity against Active Directory during ordinary requests; rely on the short token lifetime unless a later security decision requires revalidation.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Claims and Time

### Decision 10 — Registered Claims
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use `iss`, `aud`, `sub`, `iat`, and `exp`; add `nbf` only if needed.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 11 — Application Claim Names and Formats
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use standard `role` and `scope` names only when the corresponding approved authorization models require them.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 12 — Token and Claim Size
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a small, bounded claim allowlist with an explicit maximum token and claim size.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 13 — JWT Identifier
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Omit `jti` for stateless revocation; require it only if a future denylist model is separately approved.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 14 — Role Claims
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Issue only approved application roles.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 15 — Scope, Group, and Display Claims
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Issue only the minimum approved claim type; never include raw LDAP data.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 16 — Access Token Lifetime
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use short-lived production tokens with separately configured environment values.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 17 — Clock Skew
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a small bounded clock-skew tolerance, such as five minutes.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 18 — Time Synchronization
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Require synchronized UTC host clocks.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Signing and Key Lifecycle

### Decision 19 — Signing Algorithm Family
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use RSA asymmetric signing.  
**Basis:** `Token_and_Authorization_Design.md` recommendation for limiting private-key distribution.

### Decision 20 — Production Key Storage
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Store production signing keys in an approved managed key store or HSM-backed store.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 21 — Non-Production Key Source
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use runtime-generated test keys or an approved development key store.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 22 — Private-Key Access Identity
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a dedicated least-privilege issuer identity.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 23 — Key Protection Controls
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use protected or non-exportable keys where supported and enforce least-privilege access.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 24 — Key-Access Auditing
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Audit key access and failures without logging key material.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 25 — Key Ownership
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Assign explicit platform/security ownership with documented runbooks.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 26 — Key Rotation Frequency
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a scheduled rotation interval shorter than the maximum accepted key-risk window.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 27 — Rotation Overlap
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Retain the previous public key until all tokens signed by it expire.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 28 — Emergency Rotation and Retirement
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Support immediate key replacement with bounded public-key retirement.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 29 — Key Identifier Generation
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use stable random, non-secret `kid` values unique within the issuer.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 30 — Key Publication and Unknown Keys
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Publish approved public keys and reject unknown key identifiers.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Issuance and Authorization

### Decision 31 — Login Token Response
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Return an access token, token type, and expiration metadata only.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 32 — Token Issuance Location
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** The Application layer owns the token-issuance use case; Infrastructure signs behind an abstraction.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 33 — Token Validation Location
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** API authentication middleware validates tokens at the HTTP boundary.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 34 — Application Roles
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use application-owned roles with explicit names and default deny.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 35 — Active Directory Groups
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Active Directory groups may be used as an authorization source only through an explicit allowlist of approved groups.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 36 — Nested Groups
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Reject implicit nested-group expansion until it is explicitly supported and tested.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 37 — Disabled Accounts and Membership Changes
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Enforce account state at authentication and bound stale-token exposure with short token lifetimes.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 38 — Group Size
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Bound group claims and fail closed when approved limits are exceeded.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 39 — Authorization Claims
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Map application roles from approved groups; add scopes only when demonstrated necessary.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 40 — Authorization Policies
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use named API policies with default deny for protected endpoints.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 41 — Anonymous Endpoints and Failures
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Explicitly allow only approved public endpoints such as health and login; use standard 401/403 responses.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 42 — Group-to-Role Mapping
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use a versioned, non-secret configuration allowlist initially.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 43 — Mapping Propagation and Cache
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Apply mapping changes on token issuance and avoid long-lived authorization caches initially.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Refresh, Revocation, and Operations

### Decision 44 — Refresh Tokens
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Do not use refresh tokens initially; require reauthentication.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 45 — Future Refresh Tokens
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Any future refresh-token storage, rotation, reuse detection, revocation, transport, expiration, or client requirements require a separate design and explicit approval before implementation.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 46 — Token Revocation
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use short-lived stateless tokens initially with documented emergency key rotation.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 47 — Stateful Revocation Storage
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Defer stateful revocation storage until a concrete requirement exists.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 48 — Configuration and Operations
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use external non-secret configuration plus approved key storage; fail closed on invalid security configuration; document deployment, monitoring, incident response, and ownership.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

### Decision 49 — Testing and Dependencies
**Status:** APPROVED — USE RECOMMENDATION  
**Approved decision:** Use runtime test keys, isolated tests by default, no live-DC dependency, and no new package, project reference, middleware, or architecture change without separate approval.  
**Basis:** `Token_and_Authorization_Design.md` recommendation.

## Approval Scope

The 49 decisions above are approved for Phase 4 design purposes and for implementation planning within the stated constraints. This record does not authorize implementation outside those decisions.

## Implementation Constraints

The following remain prohibited unless separately approved:

- New NuGet packages
- New project references
- Architecture or dependency-direction changes
- Database implementation
- Refresh-token implementation
- MFA
- SSO or federation
- Rate limiting
- Brute-force protection
- Secrets-management architecture beyond the approved key-store direction
- Any change to Phase 3 LDAP authentication behavior
- Any security control not covered by the approved design

Implementation must preserve `Domain <- Application <- Infrastructure <- Api`. No signing key may be generated or committed during design work. Secrets, private keys, passwords, tokens, and sensitive LDAP data must not be placed in source control or logs.

## Scope Boundaries

Approved Phase 4 work is limited to the token and authorization design direction recorded here. It does not include implementation of JWTs, JWT middleware, authorization policies, refresh tokens, database tables, signing keys, MFA, SSO/federation, rate limiting, brute-force protection, or unrelated security features.

Phase 3 remains the approved LDAP authentication boundary: UPN authentication through LDAPS/TCP 636 with normal certificate validation, no fabricated DN, no password persistence, and no password logging.

## Security and Governance Constraints

- Passwords must never be placed in tokens.
- Sensitive LDAP data and raw directory responses must never be placed in tokens.
- Secrets and signing keys must never be committed to source control.
- Signing keys must be protected by an approved external key store and least-privilege access.
- Token validation must enforce the approved issuer, audience, signature, algorithm, time claims, and key identifier behavior.
- Bearer-token transport must use HTTPS.
- Claims must remain minimal, allowlisted, and bounded.
- The implementation must use only approved dependencies and preserve project boundaries.
- Any deviation from this record requires separate architect approval before implementation.

## Approval Authority and Sign-Off

**Approval authority:** Project architect / authorized architecture decision owner  
**Decision date:** 2026-08-31  
**Decision status:** APPROVED — USE RECOMMENDATIONS  
**Recorded by:** Codex implementation agent

This record documents the architect's approval supplied for the recommended Phase 4 baseline. It is not approval for uncontrolled scope expansion.
