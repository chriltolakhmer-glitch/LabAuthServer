# JWT

LabAuthServer issues and validates signed bearer JWTs after successful directory authentication and approved role mapping.

## Issuance

The supplied configuration uses `RS256`. The signer uses RSA PKCS#1 SHA-256 signing and resolves the active private key through the configured Windows certificate store. Private keys are not stored in source control or emitted in responses.

The token header contains `alg`, `typ`, and `kid`. The payload contains the allowlisted claims:

- `iss` — configured HTTPS issuer.
- `aud` — configured audience.
- `sub` — authenticated username/subject.
- `jti` — unique token identifier.
- `iat` — issued-at time.
- `nbf` — not-before time.
- `exp` — expiration time.
- `role` — exactly one approved role.
- `scope` — the configured scope collection, currently empty in the login flow.

The supplied configuration uses a one-hour lifetime, five-minute clock skew, `MaximumClaimSize = 4096` and `MaximumTokenSize = 7680`. The signer checks issuer/audience/subject lengths in UTF-16 code units; the claims builder checks role/scope values in UTF-8 bytes. The aggregate claim estimate and serialized JSON payload checks retain their meanings but now use the reduced 7680-byte budget. `MaximumTokenSize` is not a complete encoded JWT ceiling. Deployment values must be supplied through approved configuration; use `<ISSUER_HTTPS_URI>`, `<AUDIENCE>`, `<KEY_ID>`, and `<THUMBPRINT>` placeholders in public material.

Signing and validation use separate key access paths. Issuance requires the active certificate's accessible RSA private key. Bearer validation resolves only the RSA public key and can operate with a certificate that has no private key. Validation never exports or requires private RSA parameters.

## Validation

Bearer validation requires HTTPS metadata, a signed token, exact issuer and audience, lifetime validation, the configured algorithm, an approved `kid`, and the RSA public key corresponding to the approved certificate. Inbound claim mapping is disabled. The validator requires exactly one `sub`, `jti`, `iat`, and `nbf`, plus exactly one approved `role`.

Certificate selection fails closed unless exactly one currently valid certificate matches the configured key. The certificate must contain an RSA key within the inclusive 2048–4096-bit range, may specify Digital Signature key usage, and must not contain an EKU extension because JWT signing uses a general-purpose signing certificate policy. Invalid signatures, issuers, audiences, lifetimes, formats, key identifiers, required claims, roles, and oversized encoded tokens fail authentication. Issuance per-claim size checks are separate from the encoded bearer-token ceiling. Previous-key validation is supported only during a configured, unexpired overlap window.

No refresh tokens, token persistence, stateful revocation, or token introspection endpoint is implemented.

## Implemented Phase 2A size boundaries

The fixed RSA range is 2048–4096 bits for active/previous signing and public validation keys, with 2048, 3072 and 4096 tested. Keys below 2048 or above 4096 are rejected. The shared Infrastructure policy also guards the signer and bearer key resolver. Previous keys require an unexpired overlap window.

With the reduced 7680-byte payload budget and maximum escaped `kid`, the 4096-bit maximum gives a conservative complete JWT bound of 11997 bytes: 1072 encoded header bytes + 10240 encoded payload bytes + 683 encoded signature bytes + two separators. `JwtRequestSizePolicy.MaximumEncodedJwtSize = 12288` leaves 291 bytes of margin. `MaximumAuthorizationHeaderSize = 12352` covers the complete header value, including the seven-byte `Bearer ` prefix and 57 bytes of formatting headroom. These are fixed API constants, not configuration settings. Startup rejects a configured payload budget whose conservative encoded output exceeds the fixed transport ceiling.

After routing and the existing login limiter, before authentication, `AuthorizationSizeMiddleware` counts UTF-8 value bytes (including comma separators for multiple values) before joining or trimming. Values above 12352 bytes return an empty 431; Bearer tokens above 12288 bytes within that header budget return an empty 401 with `WWW-Authenticate: Bearer`. The bearer token handler uses the same encoded ceiling. These early exits retain correlation and avoid key lookup, endpoint work and audit writes; they do not log credentials. Existing parsing and validation handle remaining malformed input.

Application enforcement occurs after server header parsing. IIS/native and upstream proxy transport compatibility requires deployment verification. See the [JWT Size Boundary Decision](plans/Phase-2/Phase-2A-API-Resource-Protection.md#jwt-size-boundary-decision) for derivation and evidence.

## 2026-09-08 reduction and deployment boundary

Option A reduces issuance and acceptance together. The prior 24576/24640-byte envelope was unreachable through the inspected native path. Actual HTTP/1.1 and HTTP/2 probes reached the new candidate boundaries, including one byte over, with normal headers and 3072 bytes of additional metadata. Those probes used the old deployment, so new-policy rejection after deployment remains pending. Other client profiles and proxy/LB capacity remain unverified. See [the measured budget decision and deployment plan](plans/Phase-2/JWT-Transport-Budget-Reduction.md). No deployment or native/TLS/key changes were made.

## Runtime key retirement

The final application-code review separates startup overlap validation from runtime structural validation. Once a running host crosses PreviousKeyExpiresAt, previous-key resolution fails closed while active signing and validation continue. A fresh startup still rejects stale overlap metadata. Synthetic provider/signer/bearer regression tests verify this without certificate-store changes. See [the finding and fix](plans/Phase-2/Phase-2A-Final-Application-Code-Review.md).
