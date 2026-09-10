# Phase 3 — Infrastructure & Configuration Confirmation

Date: 2026-09-10. Mode: **READ-ONLY**. Nothing was modified.

Related: [Phase 3.1](Phase-3.1-Proxy-Forwarded-Headers-Validation.md) ·
[3.2](Phase-3.2-HSTS-Assessment.md) · [3.4](Phase-3.4-HTTPsys-Assessment.md) ·
[3.5](Phase-3.5-Network-WAF-LB-Validation.md) · [3.7](Phase-3.7-Operational-Release-Review.md) ·
[3.8](Phase-3.8-Deployment-Readiness.md) · [Final Status](Phase-3-Final-Status.md).

This document is the **question sheet** that must be answered by the infrastructure/configuration
owner before the remaining Phase 3 blockers can be closed.

## Part A — Verified Locally

| Item | Value |
| --- | --- |
| Git HEAD | `6793324365d860085080ab53bb7ccdb3b4401c28` |
| Branch | `main` |
| Deployed product version | `1.0.0+6793324365d860085080ab53bb7ccdb3b4401c28` |
| Deployed DLL SHA256 | `71A07E150DFDA3CF4C86E66B6A994F1BE2FB37CB1E1182310A2DFE9ECCDE0FEA` |
| Deployed DLL mtime (UTC) | `2026-09-10T15:07:02Z` |
| IIS site state | Started |
| App pool state | Started |
| Bindings | `http *:80:DC01.lab.local`, `https *:443:DC01.lab.local` |
| HTTPS certificate | `CN=DC01.lab.local`, SAN `DC01.lab.local`, valid to 2027-08-26 |
| HTTP.sys 443 hash | `bd545ba289ebfc645c8c3dc424311975579d7e09`, App ID `{4dc3e181-...}` (IIS) |
| DNS `dc01.lab.local` | `192.168.56.138` (this host) |
| Local IPv4 | `192.168.56.138` |
| Ports 80/443 owner | PID 4 (HTTP.sys) |
| ARR/proxy/rewrite modules | 0 |
| Site custom headers | 0 |
| Live `AllowedHosts` | `*` |
| Live `MaximumTokenSize` / `MaximumClaimSize` | `7680` / `4096` |
| Live `ActiveKeyId` / signing thumbprint | `lab-jwt-signing-20260907` / `94D4AC…` |
| Live audit connection | `tcp:DC01.lab.local,1433`, `Encrypt=True`, `TrustServerCertificate=False` |

### Live probe results

| Probe | Status | Headers |
| --- | --- | --- |
| `GET https://dc01.lab.local/api/v1/health` | 200 OK | `nosniff`, `DENY`, `no-store`; body `{"status":"Healthy"}`; no HSTS |
| `GET http://dc01.lab.local/api/v1/health` | 307 | `Location: https://dc01.lab.local/api/v1/health`; same headers; no HSTS |
| `GET https://dc01.lab.local/api/v1/protected` | 401 | `WWW-Authenticate: Bearer`; same headers; empty body; no HSTS |
| `GET https://127.0.0.1/api/v1/health` | TLS rejected | certificate does not cover the IP literal |

## Part B — Questions for the Infrastructure / Configuration Owner

### Proxy / Load Balancer / WAF

1. Is there any reverse proxy, load balancer, WAF, ADC, or gateway between clients and IIS?
2. If yes, what component/vendor is used?
3. Does it terminate TLS?
4. If TLS terminates upstream, does it forward traffic to IIS using HTTP or HTTPS?
5. What source IPs/CIDRs does IIS receive from the proxy?
6. What is the proxy hop count?
7. Is the original client IP preserved?
8. Can clients reach IIS directly, bypassing the proxy?

### HSTS

9. Is HTTPS guaranteed for every production access path?
10. Are any HTTP-only clients, monitoring probes, integrations, or legacy systems present?
11. Are any additional production hostnames/subdomains served by this application?
12. Should subdomains be covered by HSTS?
13. Is `includeSubDomains` required?
14. Is `preload` required?
15. What HSTS `max-age` is approved?
16. Is a tested `max-age=0` rollback procedure available?

### Host validation

17. Should production accept only `DC01.lab.local`?
18. If not, what additional exact hostnames must be allowed?
19. Should the live `AllowedHosts: "*"` be replaced?

## Part C — HSTS Decision Options (Not Implemented)

**OPTION A — Conservative (recommended default if HSTS is approved):**

```
Strict-Transport-Security: max-age=<approved-value>
```

No `includeSubDomains`. No `preload`.

**OPTION B — Expanded (only if explicitly approved):**

```
Strict-Transport-Security: max-age=<approved-value>; includeSubDomains
```

Neither option is implemented. No `max-age` is chosen. `preload` is **not** recommended unless
explicitly required: it is difficult to reverse and requires browser-vendor action.

## Part D — AllowedHosts Decision

| Aspect | Value |
| --- | --- |
| Source value | `DC01.lab.local` |
| Live value | `*` |
| Documented production hostname | `DC01.lab.local` — the sole allowlist value per `docs/Security.md`, `docs/Configuration.md` and `docs/Architecture.md` |
| Additional documented hostnames | None found |
| Localhost / IP-literal support | Not documented as a production requirement; IP-literal HTTPS already fails TLS validation |
| Environment-specific hostname config | None found in source |
| Deployment override | The 2026-09-10 deployment deliberately preserved the pre-existing live `appsettings.json`; `"*"` was pre-existing and was not introduced by that deploy |

**Recommendation: REQUIRES OWNER CONFIRMATION**, with a strong lean toward **ALIGN TO
`DC01.lab.local`** — every repository document identifies that name as the sole intended production
host, and the certificate covers only that name. Alignment is a configuration/deployment action in a
separately authorized window, not a code change. Neither source nor live configuration was modified.