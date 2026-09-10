# JWT transport budget reduction

Decision, implementation and deployment: 2026-09-08. **Option A deployed and production-validated.** This supersedes the 2026-09-07 24576/24640-byte policy. Remaining Phase 2A controls and Phase 2B are outside this increment.

## Contract

| Concept | Old | New | Owner and units |
| --- | ---: | ---: | --- |
| MaximumClaimSize | 4096 | 4096 | Token configuration; existing field-specific semantics |
| MaximumTokenSize | 16384 | 7680 | Token configuration; serialized UTF-8 JSON payload bytes and aggregate claim-value estimate |
| MaximumEncodedJwtSize | 24576 | 12288 | Fixed API constant; complete compact JWT bytes |
| MaximumAuthorizationHeaderSize | 24640 | 12352 | Fixed API constant; parsed UTF-8 Authorization values including join commas |
| Native transport | Approximately 16 KiB field/aggregate defaults | Unchanged | HTTP.sys/IIS; not an application option |

The application constants are centralized in `JwtRequestSizePolicy`. The middleware and default bearer token handlers consume the same encoded ceiling. `TokenConfigurationExtensions` checks configured payload compatibility at API startup. `TokenOptionsValidator` handles generic token configuration; it deliberately does not depend on the API transport policy. Standalone Infrastructure instances are not an independently configured HTTP server.

Only the two API constants and `Token:MaximumTokenSize` in source appsettings change in production code/configuration. Existing middleware, signing, claims building, validation, algorithm, keys, lifetime, clock skew, issuer and audience remain unchanged. There are no new settings or dependencies. Explicit 16384 values in independent certificate/claim/service test fixtures are not live API overrides.

## Why the old envelope was abandoned

The deployed application accepted its old boundary in isolated tests, but actual HTTP/1.1 IIS traffic carrying 24576-byte JWTs or 24640-byte Authorization values received HTTP.sys 400 before application correlation/authentication. HTTP/2 testing of a 24577-byte token produced a stream reset with ENHANCE_YOUR_CALM. The application also permitted issuance of tokens exceeding native capacity.

No current business requirement establishes a need for that envelope. Login emits exactly one mapped role and empty scopes. Increasing HTTP.sys capacity would broaden machine-wide input capacity and require host restart planning and per-protocol validation. Option A keeps native limits unchanged and reduces issuance and acceptance together. It does not promise that arbitrarily large other headers or URLs will fit.

## Issuer inspection and size proof

`TokenClaimsBuilder` normalizes role/scope values using existing trim/distinct/sort behavior, checks each against the UTF-8 claim budget, and checks the aggregate UTF-8 claim-value estimate. Exactly one of Reader, Operator or Administrator is allowed. Unicode roles are unsupported and rejected; Administrator is the longest supported role.

`RsaTokenSigningService` serializes the allowlisted claims using `JsonSerializer.SerializeToUtf8Bytes`, rejects a payload above `MaximumTokenSize`, and separately checks issuer/audience/subject UTF-16 lengths. The independent 4096 claim ceiling is unchanged. The login DTO permits at most 1024 username UTF-16 code units; the token service's 4096 subject ceiling is a different boundary. Scopes are supported by the token service but currently empty in login.

JSON escaping consumes payload bytes. A value within its independent claim limit can still exceed the aggregate/serialized budget. Issuance throws on failure; no claim is shortened to fit. The existing controller maps issuance failure to its safe 500 response; this increment does not redesign that response.

For unpadded base64url, `B(n) = ceil(4n/3)`. Complete JWT length is `B(header) + B(payload) + B(signature) + 2`.

The actual deployed issuer and active 2048-bit RS256 certificate were used, with only an in-memory payload option changed to 7680. DLL hashes were checked against Current. The active ASCII key ID is 24 characters, producing a 60-byte header (80 encoded bytes). A 256-byte signature contributes 342 encoded bytes. Thus the exact current-key maximum is:

`80 + 10240 + 342 + 2 = 10664 bytes`.

The fixed policy must also accommodate every supported 2048–4096-bit RSA key and the existing key-ID validator, which permits 128 UTF-16 code units. Maximum JSON escaping gives an 804-byte header, and a 4096-bit key gives a 512-byte signature:

`1072 + 10240 + 683 + 2 = 11997 bytes <= 12288`.

This leaves 291 bytes of encoded margin under the full supported configuration, rather than choosing a ceiling solely for today's smaller key. The integration suite actually signs and validates the 11997-byte maximum fixture with a synthetic 4096-bit key and maximum escaped key ID. That is a supported-configuration regression fixture, not a claim about the deployed key ID or ordinary login size.

The overflow-safe startup check remains `1072 + ceil(4 * MaximumTokenSize / 3) + 683 + 2 <= 12288`. Its largest mathematically compatible positive payload override is 7898; 7899 fails. The selected deployment payload is 7680, not 7898. The former live 16384 setting fails startup with the new binaries, so configuration and binaries must move together.

## Actual deployed issuer measurements

Measured 2026-09-08. Synthetic, contract-valid inputs were passed through the real deployed token service and signer. They are upper-bound fixtures, not proof that such accounts exist in AD. Input contents and issued tokens were not persisted. The active certificate was used without export or certificate/ACL changes.

| Fixture | Payload bytes | Encoded JWT | Authorization value | HTTP/1.1 Authorization line |
| --- | ---: | ---: | ---: | ---: |
| Ordinary login-shaped Reader, empty scopes | 208 | 702 | 709 | 726 |
| 1024-unit ASCII identity, Administrator, empty scopes | 1217 | 2047 | 2054 | 2071 |
| 1024-unit Unicode identity, Administrator, empty scopes | 6287 | 8807 | 8814 | 8831 |
| 4096-unit ASCII token-service identity, Administrator | 4289 | 6143 | 6150 | 6167 |
| One 4096-byte ASCII scope | 4306 | 6166 | 6173 | 6190 |
| Unicode identity and scope | 243 | 748 | 755 | 772 |
| Scope containing JSON-escaped characters | 258 | 768 | 775 | 792 |
| Exactly 7680 payload bytes, two independently bounded scopes | 7680 | 10664 | 10671 | 10688 |

The exact-payload fixture uses only supported claims; no extra padding claim or impossible role is used to measure issuer overhead. An additional payload byte is rejected. A 4097-unit subject is rejected independently. A Unicode scope of 4096 UTF-8 bytes expands past 7680 serialized bytes and is rejected rather than truncated. Unicode roles remain rejected.

## Authorization and native accounting

The new value budget is `12288 + 7 + 57 = 12352`. The seven bytes are `Bearer `; 57 bytes retain the existing finite whitespace allowance. Repeated parsed values add one comma each before joining. This accounting does not include the header name, colon, wire whitespace, CRLF, other fields or request line. Arbitrary non-ASCII wire headers may be decoded/rejected by the server before this managed UTF-8 calculation.

For a single HTTP/1.1 field, `Authorization: ` plus terminating CRLF adds 17 bytes. The maximum field line is therefore 12369 bytes, leaving 4015 of 16384 for the request line, all other field lines and final CRLF. This subtraction is a planning bound; the real-host probes below provide the route-specific evidence.

| Application input | Behavior when reached |
| --- | --- |
| JWT 12287 / 12288 bytes, otherwise valid | Authentication runs; Reader request succeeds |
| JWT 12289 within header budget | Empty 401, WWW-Authenticate: Bearer; no downstream authentication/key/audit work |
| Authorization 12351 / 12352 bytes | Continues to normal JWT parsing/authentication; size alone does not make malformed credentials valid |
| Authorization 12353 bytes | Empty 431; no downstream authentication/key/audit work |

Header rejection precedes token rejection. HTTPS redirect and the existing login limiter retain their earlier pipeline positions and can take precedence. Native parsing remains earlier than all application middleware.

## Actual HTTP/1.1 and HTTP/2 evidence

Before changing source policy, candidate boundary requests were sent to the existing IIS deployment on the inspected direct HTTPS route, using ordinary TLS certificate/name validation and online revocation checks. HTTP/1.1 used raw TLS to preserve exact whitespace. HTTP/2 used exact-version requests and internal scheme whitespace, avoiding trailing-whitespace normalization. No protocol or host setting changed.

Each request used `/api/v1/protected`, Host/authority, User-Agent, Accept, Content-Type, Content-Length: 0, a 36-byte correlation ID, and an additional 3072-byte `X-Budget-Reserve` value. HTTP/1.1 also used Connection: close. This is an explicit tested request profile, not an unverified proxy allowance.

| Case | HTTP/1.1 | HTTP/2 | Application correlation |
| --- | --- | --- | --- |
| Actual issuer maximum (10664-byte JWT) | 200 | 200 | Matched |
| JWT 12287 | 200 | 200 | Matched |
| JWT 12288 | 200 | 200 | Matched |
| JWT 12289 | 200 | 200 | Matched |
| Authorization 12351 | 200 | 200 | Matched |
| Authorization 12352 | 200 | 200 | Matched |
| Authorization 12353 | 200 | 200 | Matched |

These are **old-deployment transport reachability results**, not new-policy rejection results. Separately signed inbound boundary fixtures are larger than the proposed issuance allowance; they exercise the acceptance ceiling and are not presented as issuer output. All tokens were transient and signed with the approved active key.

At the 12352 Authorization boundary, the complete HTTP/1.1 request line/headers total 15712 bytes, leaving 672 against 16384. HTTP/2 decoded field-section accounting, including four pseudo-fields and 32 bytes per field, is estimated at 16029 bytes, leaving 355. The additional 3072-byte metadata value is already included. Without it, the same field profile has 3744 and 3427 bytes of numerical headroom, respectively. The HTTP/2 estimate is not a measurement of internal HTTP.sys allocation or a captured compressed frame size.

Both protocols therefore reached the candidate boundary, including one byte over, on the inspected route with the explicit profile. This supports selecting 12288/12352 without a native increase. Larger ancillary fields, extra repeated fields or longer request targets require separate budget validation. Proxy/LB/WAF paths remain unverified; none was identified on this direct route. HTTP/3 is outside this change.

Sanitized local measurements and the diagnostic source are retained under `<WORKSPACE>/Temp/JwtBudget-20260908/`; `measurements.json` records sizes, hashes and statuses only. This is host-local evidence, not a portable CI dependency.

## Tests and build

Full Release suite: **237 passed (165 unit, 72 integration), 0 failed, 0 skipped**. Previous increment: 225 passed (162 unit, 63 integration). The extra 12 cases extend repeated-header accounting, startup incompatibility, valid below/exact boundaries, signed oversized rejection, Unicode/escaping preservation, and maximum login identity behavior for all approved roles. Existing issuer/audience/key/lifetime/signature and LDAP/audit regressions remain covered.

Release build: **0 warnings, 0 errors**. Restore completed without dependency changes.

TestServer proves application behavior and downstream invocation counts. Network probes prove candidate transport reachability under the old deployment. Neither is a claim that the new policy has been deployed.

## Authorized deployment plan (not executed)

1. Obtain explicit deployment authorization. Publish the reviewed source, verify package hashes and preserve the current deployment/configuration for rollback.
2. Preserve live issuer, audience, algorithm, lifetime, skew, approved key list, active certificate/key, SQL TLS and IIS HTTPS. Merge only `Token:MaximumTokenSize = 7680` into the preserved deployment settings. Do not copy source appsettings wholesale: its existing key and database defaults are not the current production configuration. Inspect environment/IIS configuration overrides so 16384 cannot remain effective.
3. Deploy the matching binaries and payload setting as one controlled change. No registry, IIS transport, protocol, certificate or unrelated security changes are included.
4. Confirm startup, health 200, anonymous protected 401 and a normal authorized login through the approved operator procedure. Preserve safe audit behavior.
5. Over both HTTP/1.1 and HTTP/2, repeat the profile above with issuer maximum, valid JWT 12287/12288, signed JWT 12289, and valid-token Authorization values 12351/12352/12353. Expect 200/200/401 for the token cases and 200/200/431 for the header cases. Check matching correlation, empty rejection bodies and the Bearer challenge only for the early JWT 401. Include a tampered supported-size token returning 401.
6. Use automated invocation-spy evidence for no downstream work; reconcile safe runtime audit/correlation evidence where possible without adding per-rejection logging or exposing tokens. A network status alone cannot count authentication calls.
7. If native transport rejects the supported boundary first, stop acceptance and recommend a smaller coordinated budget. Do not increase native limits. Restore the preserved package and matching configuration if deployment health or compatibility fails, then repeat baseline smoke checks.

## Compatibility and remaining work

This intentionally removes support for larger issuance payloads and larger inbound tokens. Existing issued tokens exceeding the new acceptance ceiling will be rejected after deployment; identify affected clients before rollout. Independent 4096-unit/byte claims are not guaranteed to fit simultaneously, and Unicode escaping can reduce usable data volume.

No deployment, commit or push occurred. The live service retains 16384/24576/24640 until authorized rollout. General body/header budgets, HSTS, response security headers, AllowedHosts and LDAP Phase 2B remain separate work. Other hostnames, request profiles and ingress paths do not inherit this route's transport evidence automatically.
