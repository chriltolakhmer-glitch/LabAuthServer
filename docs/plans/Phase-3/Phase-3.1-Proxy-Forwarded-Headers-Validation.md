# Phase 3.1 — Proxy / Forwarded-Header Trust Validation

Date: 2026-09-10. Evidence-based validation, source only. **No deployment, no IIS, no network change.**

Scope boundary: this document separates **APPLICATION FINDINGS** (verified from repository source and tests)
from **INFRASTRUCTURE FINDINGS** (unverified; require infrastructure confirmation).

## 1. Scope

Determine whether the current ASP.NET Core forwarded-header / trusted-proxy configuration is safe for the
actual deployment topology. Repository inspection and deterministic application tests only. No network
scan, proxy change, firewall/DNS change, packet capture, IIS exposure test or production traffic test.

## 2. Current implementation (APPLICATION FINDINGS)

Searches over `src/**` for `UseForwardedHeaders`, `ForwardedHeadersOptions`, `ForwardedHeadersMiddleware`,
`KnownProxies`, `KnownNetworks`, `ForwardLimit`, `X-Forwarded-*` and `X-Real-IP` returned **no matches**.
The application does not consume forwarded headers at all.

`src/LabAuthServer.Api/Program.cs` middleware order:

```
ApiResponseHeadersMiddleware -> GlobalExceptionMiddleware -> CorrelationMiddleware
-> UseHttpsRedirection -> UseRouting -> GeneralHeaderSizeMiddleware -> RequestBodySizeMiddleware
-> UseRateLimiter -> AuthorizationSizeMiddleware -> UseAuthentication
-> AuthorizationAuditMiddleware -> UseAuthorization -> health/controllers
```

There is no `UseForwardedHeaders` call anywhere in this pipeline. Host filtering is the framework
default (`AddControllers`), configured only with `AllowEmptyHosts = false`.

Effective value sources in the current code:

| Value | Source | Consumed from forwarded headers? |
| --- | --- | --- |
| Client IP | `HttpContext.Connection.RemoteIpAddress` | No |
| Scheme | `Request.IsHttps` / `Request.Scheme` | No |
| Host | `Request.Host` (framework host filtering) | No |

Client-IP consumers: `AuthController` (login audit), `ProtectedController`, `JwtBearerAuthenticationOptions`,
`AuthorizationAuditMiddleware` — all read `Connection.RemoteIpAddress` directly. None reads
`X-Forwarded-For` or `X-Real-IP`.

## 3. Current trust model (APPLICATION FINDINGS)

| Question | Answer |
| --- | --- |
| Which forwarded headers are trusted? | **None.** |
| Is `X-Forwarded-For` trusted? | No — never read. |
| Is `X-Forwarded-Proto` trusted? | No — never read. |
| Is `X-Forwarded-Host` trusted? | No — never read. |
| Is `ForwardLimit` configured? | No — no forwarded-header middleware is registered. |
| Are `KnownProxies` configured? | No. |
| Are `KnownNetworks` configured? | No. |
| Does the app accept forwarded headers from arbitrary clients? | No. Forwarded headers are inert; they are neither parsed nor applied. |
| Where is the effective client IP obtained? | The socket peer (`Connection.RemoteIpAddress`). |
| Where is the effective scheme obtained? | The actual connection (`Request.IsHttps`). |
| Where is the effective host obtained? | The actual `Host` request header. |
| Do security decisions depend on those values? | Login HTTPS enforcement uses `Request.IsHttps`; host filtering uses `Request.Host`. Neither depends on forwarded headers. |
| Does rate limiting depend on client IP? | **No.** The `Login` policy uses a constant partition key (`"login"`), not the client IP. |
| Does auditing depend on it? | It records the socket peer address. |
| Does HTTPS enforcement depend on it? | It uses the actual connection scheme. |
| Does `AllowedHosts` depend on it? | It evaluates the actual `Host` header. |

Because no forwarded header is consumed, the application trust boundary is the socket itself.
A private network is *not* assumed trustworthy; trust is simply not delegated to any header.

## 4. Actual topology evidence (INFRASTRUCTURE FINDINGS)

| Fact | Evidence | Status |
| --- | --- | --- |
| IIS in-process hosting | `Current/web.config`: `hostingModel="inprocess"`, `AspNetCoreModuleV2` | Observed |
| No IIS-side forwarding/rewrite module | Deployment report: no local ARR/rewrite/proxy module found | Observed (at that time) |
| Host resolves directly to this host | `DC01.lab.local` -> 192.168.56.138 | Observed |
| HTTPS binds to certificate `BD545B...` on 443 | Deployment evidence | Observed |
| External reverse proxy | — | **UNKNOWN** |
| External load balancer | — | **UNKNOWN** |
| WAF | — | **UNKNOWN** |
| TLS terminated before IIS | — | **UNKNOWN** |
| Number of proxy hops | — | **UNKNOWN** |
| Trusted proxy/LB/WAF IP ranges | — | **UNKNOWN** |
| Direct client reachability to IIS | — | **UNKNOWN** |

## 5. Unknown topology information (REQUIRES INFRASTRUCTURE CONFIRMATION)

Marked **UNKNOWN / REQUIRES INFRASTRUCTURE CONFIRMATION**:

1. Is there any reverse proxy, load balancer or WAF between clients and IIS?
2. If yes: is TLS terminated at that device, or passed through to IIS on 443?
3. If TLS is terminated before IIS, over what protocol/scheme does traffic reach IIS?
4. What are the exact trusted proxy/LB/WAF source IP addresses or CIDRs?
5. How many proxy hops exist?
6. Can clients reach IIS directly, bypassing the proxy?
7. Are new proxies planned in the near term?

## 6. Security analysis (APPLICATION FINDINGS)

| # | Scenario | Result |
| --- | --- | --- |
| A | Direct client sends `X-Forwarded-For: 10.10.10.10` | No effect. Header is never read; apparent IP cannot be spoofed. |
| B | Direct client sends `X-Forwarded-Proto: https` on HTTP | No effect. `Request.IsHttps` reflects the real connection; login still returns 400. |
| C | Direct client sends `X-Forwarded-Host: trusted-host` | No effect. Host filtering evaluates the real `Host` header; existing test proves an unapproved `Host` still returns 400 while the approved value is present in `X-Forwarded-Host`. |
| D | Multiple `X-Forwarded-For` values | Not applicable — header is not parsed, so no value is selected. |
| E | Multiple proxy hops | Not applicable — no `ForwardLimit`/trusted-proxy logic exists to mis-evaluate. |
| F | Direct access bypassing a proxy | Not applicable — there is no header trust to bypass. |
| G | Rate limiting bypass via spoofed forwarded IP | Not possible. Partition key is the constant `"login"`. |
| H | Audit poisoning via forwarded headers | Not possible. Audit records the socket peer. |
| I | HTTPS bypass via spoofed `X-Forwarded-Proto` | Not possible. Enforcement uses the real scheme. |
| J | Host bypass via spoofed `X-Forwarded-Host` | Not possible. Host filtering uses the real `Host` header. |

Conclusion: the application **fails closed** with respect to forwarded headers. There is no
application-level forwarded-header spoofing defect, and none depends on the production topology.

## 7. Spoofing analysis summary

All three `X-Forwarded-*` families and `X-Real-IP` are inert. Spoofing them cannot change the
effective client IP, scheme or host, and therefore cannot change a security decision, an audit
record, a rate-limit partition or host validation.

## 8. Rate-limit implications

The `Login` policy is `RateLimitPartition.GetFixedWindowLimiter(partitionKey: "login", ...)` with
10 requests/minute and `QueueLimit = 0`. The partition is a **single constant partition for the whole
process**, so:

- Spoofed forwarded IPs cannot obtain a fresh bucket.
- Conversely, all login callers currently share one bucket. This is an existing availability
  characteristic, not a forwarded-header defect, and is **out of scope** for Phase 3.1.

## 9. Audit implications

`ClientIp` is populated from `HttpContext.Connection.RemoteIpAddress`, validated by
`AuditEventValidator` as a parseable IP (max 45 chars). Forwarded headers cannot alter it.
If a proxy is later introduced, the recorded address would become the proxy address unless the
application is changed to consume forwarded headers with an explicit trusted-proxy list — an
operational-observability consequence, not a security bypass.

## 10. HTTPS implications

`app.UseHttpsRedirection()` and `AuthController`'s `if (!Request.IsHttps)` both use the real
connection scheme. A spoofed `X-Forwarded-Proto` cannot make HTTP appear as HTTPS.

If TLS is later terminated upstream of IIS, the application as written would see plain HTTP and
would reject login with 400 (`HTTPS is required`) rather than silently accepting it. That is a
fail-closed functional impact requiring an infrastructure/architecture decision — not a security
defect, and not remediated here.

## 11. AllowedHosts implications

`AllowedHosts` is `DC01.lab.local`; empty hosts are rejected. Host evaluation uses the real `Host`
header, so `X-Forwarded-Host` cannot admit an unapproved host. Existing test evidence confirms an
unapproved `Host` returns 400 even when `X-Forwarded-Host` carries the approved name.

## 12. Test coverage

Existing coverage (`ApiSecurityResponseTests`):

| # | Item | Status |
| --- | --- | --- |
| 1 | Trusted proxy | N/A — no proxy trust configured |
| 2 | Untrusted proxy | N/A — no proxy trust configured |
| 3 | Spoofed `X-Forwarded-Host` vs host filtering | Covered |
| 4 | Spoofed `X-Forwarded-Proto` vs HTTPS enforcement | Covered (added) |
| 5 | Spoofed `X-Forwarded-For` vs audit/rate-limit | Covered (added) |
| 6 | Multiple forwarded values | N/A — not parsed |
| 7 | Multiple proxy hops / `ForwardLimit` | N/A — not configured |
| 8 | Direct client access | Covered by host-filter and HTTPS tests |
| 9 | Rate limiting / client IP | Covered (constant partition) |
| 10 | Audit / client IP | Covered (added) |
| 11 | `AllowedHosts` interaction | Covered |

New tests are topology-independent: they assert that forwarded headers have **no** effect. They do
not encode any production proxy address, network range or hop count.

## 13. Required infrastructure confirmations

The infrastructure team must provide the answers in section 5. If a proxy exists that terminates
TLS or forwards client IPs, the application would need an explicitly authorized change to consume
forwarded headers with an exact trusted-proxy allowlist. **No such change is made here.**

## 14. Final Phase 3.1 classification

**B — PASS WITH INFRASTRUCTURE CONFIRMATION**

Application implementation appears safe: forwarded headers are entirely untrusted and inert, so
spoofing cannot influence IP, scheme, host, rate limiting, audit or HTTPS enforcement. One or more
topology facts (section 5) must still be confirmed before the production trust boundary can be
declared validated.

> Application configuration reviewed; production trust boundary requires infrastructure confirmation.

## 15. Remediation recommendation

**None required at application level for the currently observed direct-IIS topology.**

If an external proxy/load balancer/WAF is discovered or planned, the smallest safe remediation would
be to register forwarded-header processing restricted to explicitly supplied proxy addresses —
`KnownProxies`/`KnownNetworks` set to the confirmed values and a bounded `ForwardLimit`. This
requires the exact infrastructure values from section 5 and explicit authorization. It is **not
implemented**, because doing so without those values would introduce an unvalidated trust decision.

No arbitrary proxy IPs or network ranges were invented. Configuration was left unchanged.

## Explicitly not performed

No deployment, no IIS or application-pool change, no restart, no HTTP.sys change, no network, DNS,
firewall, WAF, load-balancer or reverse-proxy change, no SQL change, no certificate change, no
packet capture, no scan, no production traffic test, no commit, no push, no reset, no clean, no stash.
Existing Phase 2 uncommitted work was preserved.