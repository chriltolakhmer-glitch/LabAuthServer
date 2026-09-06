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

The supplied configuration uses a one-hour lifetime, five-minute clock skew, a maximum claim size of 4096 bytes, and a maximum token size of 16384 bytes. Deployment values must be supplied through approved configuration; use `<ISSUER_HTTPS_URI>`, `<AUDIENCE>`, `<KEY_ID>`, and `<THUMBPRINT>` placeholders in public material.

Signing and validation use separate key access paths. Issuance requires the active certificate's accessible RSA private key. Bearer validation resolves only the RSA public key and can operate with a certificate that has no private key. Validation never exports or requires private RSA parameters.

## Validation

Bearer validation requires HTTPS metadata, a signed token, exact issuer and audience, lifetime validation, the configured algorithm, an approved `kid`, and the RSA public key corresponding to the approved certificate. Inbound claim mapping is disabled. The validator requires exactly one `sub`, `jti`, `iat`, and `nbf`, plus exactly one approved `role`.

Certificate selection fails closed unless exactly one currently valid certificate matches the configured key. The certificate must contain an RSA key of at least 2048 bits, may specify Digital Signature key usage, and must not contain an EKU extension because JWT signing uses a general-purpose signing certificate policy. Invalid signatures, issuers, audiences, lifetimes, formats, key identifiers, required claims, roles, and oversized claims fail authentication. Previous-key validation is supported only during a configured, unexpired overlap window.

No refresh tokens, token persistence, stateful revocation, or token introspection endpoint is implemented.
