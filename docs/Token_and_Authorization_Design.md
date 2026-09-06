# Token and Authorization Design — Phase 4

**Status:** APPROVED — IMPLEMENTATION AUTHORIZED  
**Date:** 2026-08-31  
**Prerequisite:** Phase 3 authentication remediation is complete and approved

> This is a design-only document. It introduces no JWT implementation, authorization implementation, database storage, refresh-token implementation, signing keys, packages, or source-code changes.

---

## 1. Executive Summary

Phase 3 authenticates a `user@lab.local` UPN against Active Directory over LDAPS/TCP 636. Phase 4 defines the approved decisions for issuing an access token after successful authentication and protecting future API resources with authorization policies.

The approved direction is a short-lived signed JWT access token issued by LabAuthServer after successful LDAP authentication. The token contract, signing algorithm, key lifecycle, claims, refresh behavior, revocation model, and authorization mapping are defined by the Architect Approval Record.

Phase 4 implementation may proceed according to the approved Architect Approval Record and the constraints in this document. Implementation has not yet occurred.

---

## 2. Scope and Non-Scope

### In Scope

- Access-token architecture and contract
- JWT format and validation requirements
- Signing algorithm and key lifecycle options
- Mapping successful LDAP authentication to token issuance
- Authorization roles, groups, claims, and policies
- Refresh-token decision
- Revocation decision
- Security, configuration, testing, deployment, and threat requirements

### Out of Scope

- JWT implementation or middleware
- Authorization implementation or endpoint protection
- Refresh-token implementation or persistence
- Database implementation or schema design
- Signing-key generation or storage
- Secrets-management implementation
- MFA, SSO, federation, rate limiting, or brute-force protection
- Changes to the existing LDAP authentication flow

---

## 3. Current Authentication Flow

1. The client submits `POST /api/v1/auth/login` over HTTPS.
2. The API validates the login request shape.
3. Phase 3 validates that the username is a UPN in the configured `lab.local` domain.
4. Infrastructure performs a direct LDAP bind using the supplied UPN and password.
5. The bind uses LDAPv3 over LDAPS/TCP 636 with normal platform certificate validation.
6. Passwords are not stored, returned, or logged.
7. The current successful response contains only `authenticated: true`; it does not contain a token.

A future token flow would begin only after step 5 succeeds.

---

## 4. Approved Token Architecture

The proposed architecture is a bearer access-token model:

```text
Client
  |
  | HTTPS login credentials
  v
API login endpoint
  |
  v
Application authentication contract
  |
  v
Infrastructure LDAP authentication
  |
  v
Successful authentication result
  |
  v
Token issuance component
  |
  v
Signed JWT access token
  |
  v
Client sends Authorization: Bearer <token>
  |
  v
JWT validation + authorization policies
```

The token issuer should be a dedicated application use case or service. Cryptographic signing and key access should remain behind an Infrastructure abstraction. API middleware should validate tokens and apply authorization policies without embedding directory-specific logic.

**APPROVED BASELINE:** Use JWT bearer tokens issued by LabAuthServer; no external identity provider.

---

## 5. JWT Access Token

### 5.1 Token Format

Approved format: a compact, signed JWT carried in the HTTP `Authorization` header:

```text
Authorization: Bearer <signed-jwt>
```

The token should contain only the minimum claims required for authentication context and authorization. It must not contain passwords, secrets, raw LDAP responses, or unnecessary directory data.

**APPROVED BASELINE:** Use compact signed JWTs with an approved JOSE algorithm and serialization profile.

### 5.2 Issuer

The `iss` claim should identify the LabAuthServer token issuer using a stable, deployment-specific URI or identifier.

**APPROVED BASELINE:** Use a stable HTTPS issuer identifier owned by LabAuthServer, with distinct values per environment.

### 5.3 Audience

The `aud` claim should identify the API resources for which the token is valid. An explicit audience prevents a token issued for one service from being accepted by another service.

**APPROVED BASELINE:** Use one stable LabAuthServer API audience and reject multiple audiences initially.

### 5.4 Subject

The `sub` claim should be a stable, non-secret user identifier. A UPN may be used only if its stability, case handling, and privacy implications are accepted. A directory object identifier may be preferable if it can be obtained without expanding Phase 3 behavior.

**APPROVED BASELINE:** Use a stable directory object identifier when available, otherwise normalized UPN; do not use a fabricated DN.

### 5.5 Claims

Candidate registered claims:

- `iss`: issuer
- `aud`: intended API audience
- `sub`: stable authenticated user identifier
- `iat`: issue time in UTC
- `nbf`: earliest valid time, if required
- `exp`: expiration time
- `jti`: unique token identifier, if required for revocation or replay analysis

Candidate application claims:

- `name`: display name only if approved and available from a trusted source
- `role`: application role values, if role authorization is approved
- `scope`: permission values, if scope authorization is approved
- `groups`: selected normalized AD group mappings, if group-based authorization is approved

Claims should be allowlisted, bounded in size, and stable across deployments. Raw LDAP attributes, full directory entries, group membership dumps, email addresses, employee data, and other sensitive directory data should be excluded unless explicitly justified and approved.

**APPROVED BASELINE:** Use the minimum bounded allowlist and issue only claims required by the approved authorization model.

### 5.6 Expiration

The proposed default is a short-lived access token. A shorter lifetime limits the impact of theft but increases login or renewal frequency.

**APPROVED BASELINE:** Use short-lived production tokens with separately configured environment values.

### 5.7 Clock Skew

Token validation may allow a small clock-skew tolerance to account for differences between API hosts. Excessive skew weakens expiration and not-before enforcement.

**APPROVED BASELINE:** Use a small bounded clock-skew tolerance and synchronized UTC host clocks.

---

## 6. Signing Strategy

### 6.1 RSA vs Symmetric Signing

**RSA asymmetric signing** is the preferred candidate for a multi-service architecture:

- The issuer holds the private key.
- Validators can receive only the public key.
- Key distribution limits exposure of signing authority.
- Key identifiers can support rotation.

**Symmetric signing** is simpler for a tightly controlled single service:

- Issuer and validators share one secret.
- Secret distribution is broader and compromise affects all validators.
- Rotation and service isolation are more difficult.

**APPROVED BASELINE:** Use RSA asymmetric signing.

### 6.2 Key Storage

Signing keys must be stored outside source control and outside ordinary application configuration files. Candidate stores include an approved operating-system certificate/private-key store, a managed cloud key store, an HSM-backed service, or an approved enterprise secret store.

**APPROVED BASELINE:** Use an approved managed or HSM-backed production key store and runtime-generated or approved development test keys.

### 6.3 Key Protection

- Restrict private-key access to the token issuer identity.
- Use least-privilege access policies.
- Do not log private keys, key material, or secrets.
- Do not expose private keys through health endpoints, diagnostics, or configuration dumps.
- Monitor key access and failures without recording key material.

**APPROVED BASELINE:** Use a dedicated least-privilege issuer identity, protected keys, audited access, and explicit platform/security ownership.

### 6.4 Key Rotation

Key rotation should allow existing tokens to expire naturally while new tokens use the replacement key. Validators may need the current and recently retired public keys during a bounded overlap period.

**APPROVED BASELINE:** Use scheduled rotation, retain the prior public key through token expiry, and support immediate replacement with bounded retirement.

### 6.5 Key Identifiers

Each signing key should have a stable `kid` value in the JWT header. Validators use it to select the corresponding public key. A `kid` must not reveal secret material.

**APPROVED BASELINE:** Use stable random non-secret `kid` values, publish approved public keys, and reject unknown identifiers.

---

## 7. Authentication-to-Token Flow

Approved flow:

1. Client submits credentials over HTTPS to the existing login endpoint.
2. Phase 3 authenticates the UPN through the existing LDAPS direct bind.
3. Application receives a successful authentication result without a password or fabricated DN.
4. A token-issuance use case creates the approved claims.
5. Infrastructure signs the token using the active protected key.
6. API returns the access token and its approved metadata, such as token type and expiration.
7. The client sends the bearer token to protected API endpoints.
8. API validates signature, issuer, audience, time claims, and any required token identifier.
9. Authorization policies evaluate approved role, group, or scope claims.

Authentication failure responses must remain generic for invalid credentials. Directory availability and timeout mappings from Phase 3 must remain distinct.

**APPROVED BASELINE:** Application owns issuance, Infrastructure signs, API validates, and ordinary requests do not revalidate against AD.

---

## 8. Authorization Model

### 8.1 Roles

Application roles provide coarse-grained permissions such as `Administrator`, `Operator`, or `Reader`. Roles should be application-owned and should not be inferred from arbitrary directory attributes without an explicit mapping.

**APPROVED BASELINE:** Use application-owned explicitly named roles with default deny.

### 8.2 AD Groups

Active Directory groups may provide the source of authorization membership. Only explicitly allowlisted groups should map to application permissions. Nested groups, disabled accounts, group changes, and large memberships require defined behavior.

**APPROVED BASELINE:** Use only explicitly allowlisted AD groups; reject implicit nesting and bound group claims.

### 8.3 Claims

Claims may carry roles or scopes to allow stateless authorization checks. Claims must be minimized and must not expose sensitive directory data.

**APPROVED BASELINE:** Map application roles from approved groups; add scopes only when demonstrated necessary.

### 8.4 Policies

API policies should express business permissions rather than duplicate LDAP implementation details. Examples include `CanReadReports` or `CanManageUsers`, with policy requirements evaluated from approved claims.

**APPROVED BASELINE:** Use named policies, default deny, explicit public endpoints, and standard 401/403 responses.

### 8.5 Mapping Strategy

Candidate strategies:

- Static configuration mapping from approved AD group identifiers to application roles
- Application-owned role mapping stored in a future data store
- Directory-driven claims resolved during login
- Request-time directory lookup for high-change permissions

A static allowlist is the simplest initial strategy but requires token renewal before group changes take effect.

**APPROVED BASELINE:** Use a versioned non-secret configuration allowlist, apply changes on issuance, and avoid long-lived caches initially.

---

## 9. Refresh Token Decision

Refresh tokens can extend a user session without resubmitting the password. They also create durable bearer credentials that require storage, rotation, replay detection, revocation, and secure client handling.

A no-refresh design is simpler and limits persistent credential exposure, but requires the client to authenticate again when the access token expires.

**APPROVED BASELINE:** Do not use refresh tokens initially; any future refresh-token design requires separate approval.

No refresh tokens are implemented in Phase 4 design work.

---

## 10. Token Revocation

JWT access tokens are normally self-contained and remain valid until expiration. Candidate revocation models include:

- Short access-token lifetime with no server-side blacklist
- A `jti` denylist, requiring a future data store
- Per-user or per-session version values
- Key rotation for emergency broad invalidation
- Introspection or opaque tokens instead of self-contained JWTs

Every stateful revocation option adds storage, availability, and operational complexity.

**APPROVED BASELINE:** Use short-lived stateless tokens initially and documented emergency key rotation; defer stateful storage.

---

## 11. Security Requirements

The following are mandatory regardless of the selected design:

- Never put passwords in tokens.
- Never put sensitive LDAP data, raw LDAP responses, or directory dumps in tokens.
- Never commit secrets, private keys, certificates, or signing material to source control.
- Protect signing keys with least privilege and approved key-management controls.
- Keep access tokens short-lived enough to limit theft impact.
- Use HTTPS for login and bearer-token transport.
- Validate signature, issuer, audience, expiration, not-before, and algorithm allowlist.
- Reject unsigned tokens and algorithm confusion/downgrade attempts.
- Never accept a token signed with an unapproved algorithm or key.
- Do not log passwords, tokens, bearer headers, private keys, or sensitive LDAP data.
- Treat bearer tokens as credentials; clients must store and transmit them securely.
- Consider replay: bearer tokens can be replayed until expiry if stolen.
- Use `jti` only if the approved revocation or replay-detection model requires it.
- Minimize claims and bound token size.
- Keep host clocks synchronized.

**APPROVED BASELINE:** Apply the documented security requirements, short lifetime, replay controls, key controls, and minimal claims.

---

## 12. Configuration Requirements

Potential configuration areas:

- Issuer
- Audience
- Access-token lifetime
- Clock skew
- Signing algorithm
- Active signing-key identifier
- Public-key discovery or validation configuration
- Key-store reference and access policy
- Authorization group-to-role mappings
- Authorization policy names
- Refresh-token and revocation settings, if later approved

Configuration must contain references and non-secret policy values only. Private keys and secret material must come from an approved external store.

**APPROVED BASELINE:** Use external non-secret configuration, approved key storage, and fail closed on invalid security configuration.

---

## 13. Layer Responsibilities

### Domain

- Remains independent of JWT libraries, HTTP, LDAP, and key stores.
- May later contain domain-level permission concepts only if they are genuinely domain rules.

### Application

- Define token issuance and validation contracts.
- Define authorization use cases, claims, roles, scopes, and policy abstractions.
- Orchestrate successful authentication into token issuance.
- Remain independent of HTTP, LDAP protocol details, and concrete cryptographic storage.

### Infrastructure

- Implement token signing behind an Application abstraction.
- Access approved key stores and public-key material.
- Implement directory-group retrieval only if approved.
- Keep cryptographic and external-provider concerns out of Domain.

### API

- Accept login requests and return the approved token response.
- Validate bearer tokens at the HTTP boundary.
- Apply authorization policies to endpoints.
- Never manually construct signing keys or bypass token validation.

**APPROVED BASELINE:** Application owns use cases, Infrastructure owns external signing/key access, and API owns HTTP validation/policies.

---

## 14. Testing Strategy

### Unit Tests

- Token claim construction and allowlisting
- Issuer, audience, subject, expiration, and clock-skew rules
- Algorithm and key-identifier validation
- Authorization role, group, claim, and policy mapping
- Default-deny behavior
- No-password/no-sensitive-directory-data claim guarantees
- Revocation decisions, if a stateful model is approved

### Integration Tests

- Successful Phase 3 authentication produces the approved token response after implementation
- Invalid credentials remain generic
- Token validation rejects wrong issuer, audience, signature, algorithm, expiration, and not-before values
- Protected endpoints enforce approved policies
- Unauthenticated endpoints remain intentionally public
- Key rotation accepts the approved overlap and rejects retired keys after the overlap

### Security Tests

- No secrets or signing keys in source, configuration snapshots, or logs
- Passwords and bearer tokens do not appear in logs or error responses
- Oversized or malformed tokens are rejected
- Replay and revocation behavior matches the approved model
- Claims do not contain raw LDAP data

### Test Isolation

Normal tests must use deterministic test keys generated at test runtime or test-only fixtures. They must not use production keys, committed private keys, real user passwords, or a live DC unless explicitly marked as an environment-dependent integration test.

**APPROVED BASELINE:** Use runtime test keys, isolated tests by default, and no live-DC dependency; future dependencies remain separately gated.

---

## 15. Deployment Considerations

- Deploy the token issuer only where private-key access is required.
- Give token validators public verification material only when asymmetric signing is selected.
- Use an approved secret/key-management service or protected certificate store.
- Configure issuer, audience, lifetime, and clock skew per environment.
- Ensure TLS termination preserves the original HTTPS security model.
- Define key rotation and rollback procedures before production enablement.
- Monitor token validation failures, key-access failures, clock drift, and authorization denials without logging credentials.
- Document incident response for stolen tokens and compromised signing keys.

**APPROVED BASELINE:** Use protected issuer deployment, approved public-key distribution, explicit ownership, monitoring, and incident response.

---

## 16. Threat Considerations

| Threat | Design concern | Required control |
|---|---|---|
| Token theft | Bearer token replay | HTTPS, short lifetime, secure client storage, approved replay controls |
| Signing-key compromise | Forged tokens | Protected key store, least privilege, rotation, emergency invalidation |
| Algorithm confusion | Validation bypass | Fixed algorithm allowlist and strict header validation |
| Audience confusion | Cross-service token acceptance | Exact issuer and audience validation |
| Clock drift | Premature or late acceptance | Time synchronization and bounded clock skew |
| Claim leakage | Directory privacy exposure | Minimal allowlisted claims |
| Group overreach | Excess permissions | Explicit group allowlist and default deny |
| Stale authorization | Revoked membership remains in token | Short lifetime or approved revocation/revalidation |
| Error disclosure | User or directory enumeration | Generic authentication failures |
| Log leakage | Credential or token exposure | Structured redacted logging and log review |
| Denial of service | Expensive validation or directory calls | Bounded token size and approved caching/availability controls |

**APPROVED BASELINE:** Apply the threat controls listed in this section to the first implementation slice.

---

## 17. Alternatives Considered

### JWT vs Opaque Tokens

- JWTs support local validation and low request-time dependency on a token store.
- Opaque tokens support central introspection and straightforward revocation but add a runtime dependency.

**APPROVED BASELINE:** JWT bearer access tokens.

### RSA vs Symmetric Signing

- RSA limits private-key distribution.
- Symmetric signing is simpler but expands secret-sharing risk.

**APPROVED BASELINE:** RSA asymmetric signing.

### Refresh Tokens vs Reauthentication

- Refresh tokens improve session continuity but require durable credential controls.
- Reauthentication avoids refresh-token state but increases login frequency.

**APPROVED BASELINE:** Reauthentication; no refresh tokens initially.

### Stateless vs Stateful Revocation

- Stateless short-lived tokens reduce infrastructure complexity.
- Stateful revocation improves control but requires storage and availability.

**APPROVED BASELINE:** Short-lived stateless tokens with emergency key rotation.

### Directory Groups vs Application Roles

- Directory groups reuse existing identity administration.
- Application roles provide explicit application ownership and stable semantics.

**APPROVED BASELINE:** Application-owned roles mapped from an explicit AD-group allowlist.

---

## 18. Decisions Requiring Architect Approval

The following 49 approved decisions are grouped by subject and remain individually preserved. Every item is approved for implementation according to its recommendation.

### Token Contract and Trust (1-9)

1. **APPROVED — USE RECOMMENDATION:** Select JWT bearer tokens or opaque access tokens. **Recommendation:** JWT for local validation if the API remains the issuer.
2. **APPROVED — USE RECOMMENDATION:** Select a self-issued token model or external identity provider. **Recommendation:** Self-issued tokens for the current single-service scope.
3. **APPROVED — USE RECOMMENDATION:** Select the canonical `iss` value. **Recommendation:** A stable HTTPS issuer identifier owned by LabAuthServer.
4. **APPROVED — USE RECOMMENDATION:** Decide whether the issuer is environment-specific. **Recommendation:** Use distinct issuer values per deployment environment.
5. **APPROVED — USE RECOMMENDATION:** Select the `aud` identifier. **Recommendation:** One stable audience for the LabAuthServer API.
6. **APPROVED — USE RECOMMENDATION:** Decide whether multiple audiences are supported. **Recommendation:** Reject multiple audiences initially.
7. **APPROVED — USE RECOMMENDATION:** Select the `sub` identifier. **Recommendation:** A stable directory object identifier when available; otherwise normalized UPN.
8. **APPROVED — USE RECOMMENDATION:** Define subject case and normalization rules. **Recommendation:** Normalize UPN comparison case-insensitively and preserve no sensitive directory data.
9. **APPROVED — USE RECOMMENDATION:** Decide whether identity claims are revalidated against AD after issuance. **Recommendation:** Do not revalidate for ordinary requests; rely on short token lifetime unless risk requires otherwise.

### Claims and Time (10-18)

10. **APPROVED — USE RECOMMENDATION:** Select registered claims. **Recommendation:** `iss`, `aud`, `sub`, `iat`, and `exp`; add `nbf` only if needed.
11. **APPROVED — USE RECOMMENDATION:** Select application claim names and formats. **Recommendation:** Use standard `role` and `scope` names only when their models are approved.
12. **APPROVED — USE RECOMMENDATION:** Select the maximum claim and token size. **Recommendation:** Use a small bounded allowlist with an explicit maximum.
13. **APPROVED — USE RECOMMENDATION:** Decide whether `jti` is issued. **Recommendation:** Omit it for stateless revocation; require it for a denylist model.
14. **APPROVED — USE RECOMMENDATION:** Decide whether `role` claims are issued. **Recommendation:** Issue only approved application roles.
15. **APPROVED — USE RECOMMENDATION:** Decide whether `scope`, `groups`, or display claims are issued. **Recommendation:** Issue only the minimum approved claim type; never raw LDAP data.
16. **APPROVED — USE RECOMMENDATION:** Select access-token lifetime. **Recommendation:** Short-lived production tokens with separately approved environment values.
17. **APPROVED — USE RECOMMENDATION:** Select clock-skew tolerance. **Recommendation:** A small bounded tolerance, such as five minutes.
18. **APPROVED — USE RECOMMENDATION:** Define deployment time synchronization. **Recommendation:** Require synchronized UTC host clocks.

### Signing and Key Lifecycle (19-30)

19. **APPROVED — USE RECOMMENDATION:** Select RSA or symmetric signing. **Recommendation:** RSA asymmetric signing.
20. **APPROVED — USE RECOMMENDATION:** Select production key storage. **Recommendation:** An approved managed key store or HSM-backed store.
21. **APPROVED — USE RECOMMENDATION:** Select the non-production key source. **Recommendation:** Runtime-generated test keys or an approved development key store.
22. **APPROVED — USE RECOMMENDATION:** Select the private-key access identity. **Recommendation:** A dedicated least-privilege issuer identity.
23. **APPROVED — USE RECOMMENDATION:** Define private-key protection controls. **Recommendation:** Non-exportable protected keys where supported and least-privilege access.
24. **APPROVED — USE RECOMMENDATION:** Define key-access auditing. **Recommendation:** Audit access and failures without logging key material.
25. **APPROVED — USE RECOMMENDATION:** Assign operational key ownership. **Recommendation:** Explicit platform/security ownership with documented runbooks.
26. **APPROVED — USE RECOMMENDATION:** Define rotation frequency. **Recommendation:** A scheduled rotation interval shorter than the maximum key-risk window.
27. **APPROVED — USE RECOMMENDATION:** Define rotation overlap duration. **Recommendation:** Retain the previous public key until all tokens it signed expire.
28. **APPROVED — USE RECOMMENDATION:** Define emergency rotation and retirement. **Recommendation:** Support immediate key replacement and bounded public-key retirement.
29. **APPROVED — USE RECOMMENDATION:** Define `kid` generation and uniqueness. **Recommendation:** Stable random non-secret identifiers unique within the issuer.
30. **APPROVED — USE RECOMMENDATION:** Define `kid` publication and unknown-key behavior. **Recommendation:** Publish approved public keys and reject unknown identifiers.

### Issuance and Authorization (31-39)

31. **APPROVED — USE RECOMMENDATION:** Select the login token response contract. **Recommendation:** Return an access token, token type, and expiration metadata only.
32. **APPROVED — USE RECOMMENDATION:** Select token issuance location. **Recommendation:** Application owns the use case; Infrastructure signs behind an abstraction.
33. **APPROVED — USE RECOMMENDATION:** Select token validation location. **Recommendation:** API authentication middleware validates tokens at the HTTP boundary.
34. **APPROVED — USE RECOMMENDATION:** Define the role catalogue and ownership. **Recommendation:** Application-owned roles with explicit names and default deny.
35. **APPROVED — USE RECOMMENDATION:** Decide whether AD groups are an authorization source. **Recommendation:** Use only an explicit allowlist of approved groups.
36. **APPROVED — USE RECOMMENDATION:** Define nested-group handling. **Recommendation:** Reject implicit nested expansion until explicitly supported and tested.
37. **APPROVED — USE RECOMMENDATION:** Define disabled-account and membership-change behavior. **Recommendation:** Enforce account state at authentication and bound stale-token exposure with short lifetimes.
38. **APPROVED — USE RECOMMENDATION:** Define group-size handling. **Recommendation:** Bound group claims and fail closed when the approved limit is exceeded.
39. **APPROVED — USE RECOMMENDATION:** Select roles, scopes, groups, custom claims, or a combination. **Recommendation:** Application roles mapped from approved groups; add scopes only for a demonstrated need.
40. **APPROVED — USE RECOMMENDATION:** Define policy model and default-deny behavior. **Recommendation:** Named API policies with default deny for protected endpoints.
41. **APPROVED — USE RECOMMENDATION:** Define anonymous endpoint rules and authorization failure responses. **Recommendation:** Explicitly allow only health/login endpoints and return standard 401/403 responses.
42. **APPROVED — USE RECOMMENDATION:** Select group-to-role mapping source and format. **Recommendation:** Versioned non-secret configuration allowlist initially.
43. **APPROVED — USE RECOMMENDATION:** Define mapping change propagation and caching. **Recommendation:** Apply changes on token issuance; avoid long-lived authorization caches initially.

### Refresh, Revocation, and Operations (44-49)

44. **APPROVED — USE RECOMMENDATION:** Decide whether refresh tokens are required. **Recommendation:** No refresh tokens initially; require reauthentication.
45. **APPROVED — USE RECOMMENDATION:** If refresh tokens are later approved, define storage, rotation, reuse detection, revocation, transport, expiration, and client requirements. **Recommendation:** Separate design and explicit approval before implementation.
46. **APPROVED — USE RECOMMENDATION:** Select token revocation and emergency invalidation. **Recommendation:** Short-lived stateless tokens initially, with documented emergency key rotation.
47. **APPROVED — USE RECOMMENDATION:** Decide whether stateful revocation storage is permitted. **Recommendation:** Defer stateful storage until a concrete requirement exists.
48. **APPROVED — USE RECOMMENDATION:** Approve configuration schema, environment overrides, validation, startup behavior, secret-store integration, deployment topology, key distribution, monitoring, and incident response. **Recommendation:** External non-secret configuration plus approved key storage, fail closed on invalid security configuration, and document operational ownership.
49. **APPROVED — USE RECOMMENDATION:** Approve test-key strategy, rotation/revocation fixtures, live-directory boundaries, and any new package, project reference, middleware, or architecture change. **Recommendation:** Runtime test keys, isolated tests by default, no live DC dependency, and no new dependency or architectural change without separate approval.

---

## 19. Implementation Gate

Implementation may proceed according to all approved decisions in Section 18:

- Implement JWTs only according to the approved baseline.
- Implement authorization only according to the approved baseline.
- Do not implement refresh tokens initially.
- Do not add database storage for revocation or tokens initially.
- Create or provision signing keys only through the approved protected key-storage process.
- Do not add NuGet packages without separate approval.
- Do not modify project references or dependency direction.
- Do not modify the Phase 3 LDAP authentication behavior.

**Status: DESIGN APPROVED — IMPLEMENTATION AUTHORIZED; IMPLEMENTATION NOT YET COMPLETED**
