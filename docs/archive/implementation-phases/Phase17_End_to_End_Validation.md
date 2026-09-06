# Phase 17 - End-to-End Validation

**Date:** 2026-09-04
**Status:** NOT COMPLETE - BLOCKED

## Deployed-system checks

| Test | Expected | Actual | Status |
|---|---|---|---|
| HTTPS health | 200, safe body | 200, `{"status":"Healthy"}` | PASS |
| HTTP health | Redirect to HTTPS | 307 | PASS |
| Correlation generation | Canonical response header | Present; valid GUID | PASS |
| Correlation propagation | Same supplied canonical ID | Exact match | PASS |
| Protected endpoint without token | 401 | 401 | PASS |
| Malformed Authorization headers | 401 | All four cases 401 | PASS |
| Placeholder invalid login | Safe authentication failure | 401 | PASS |
| Malformed login JSON | Safe validation failure | 400 | PASS |
| Audit persistence | Approved procedure path and safe events | Login failure and authorization events persisted | PASS |
| Repeated health stability | No crashes or unstable responses | 10/10 HTTP 200, distinct IDs | PASS |

The SQL audit path was verified through the existing application service and stored procedure. Recent event aggregation contained `AUTH_LOGIN_FAILURE` with status 401, `AUTHZ_ACCESS_DENIED` with status 403, and `AUTHZ_ACCESS_GRANTED` with status 200. No direct table writes were introduced and no additional permissions were granted.

## Security and limitations

JWT behavior, AD group mapping, and LDAP filter protections remain covered by the Phase 12/13 regression suite. A real valid AD login was not attempted because no real credentials may be exposed or documented. Certificate private keys and DPAPI contents were not inspected. These are documented environment-bound limitations, not bypasses.

## Blocker

The mandatory deployed valid-credential login, live AD group-to-role mapping, issued-JWT claim/signature verification, and authenticated Reader/Operator/Administrator checks could not be completed without an approved test identity and its secret being supplied through the protected operational process. No credentials were guessed, logged, or copied. Phase 17 cannot be marked complete until those checks are executed by an authorized operator.

## Gate

All safe deployed checks passed, but the mandatory real-identity checks remain blocked. Phase 17 is **NOT COMPLETE**.
