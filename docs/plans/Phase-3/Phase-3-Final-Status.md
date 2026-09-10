# Phase 3 — Final Status

Date: 2026-09-10. Consolidated status of all Phase 3 operational validation and readiness work.

Phase documents: [3.1](Phase-3.1-Proxy-Forwarded-Headers-Validation.md) ·
[3.2](Phase-3.2-HSTS-Assessment.md) · [3.3](#phase-33) ·
[3.4](Phase-3.4-HTTPsys-Assessment.md) · [3.5](Phase-3.5-Network-WAF-LB-Validation.md) ·
[3.6](Phase-3.6-Capacity-Load-Assessment.md) · [3.7](Phase-3.7-Operational-Release-Review.md) ·
[3.8](Phase-3.8-Deployment-Readiness.md).

## Overall Decision

> **UPDATED 2026-09-10 (post-implementation).** The infrastructure confirmations were received and the
> authorized HSTS + AllowedHosts change was deployed and live-verified. See
> [HSTS + AllowedHosts Implementation](Phase-3-HSTS-AllowedHosts-Implementation.md).

**A — READY FOR EXPLICIT DEPLOYMENT AUTHORIZATION** (was C before the 2026-09-10 change).

All application-side work is complete and validated. The prior infrastructure blockers were resolved and
the approved configuration change was deployed and verified on the live IIS endpoint.

## Phase Results

| Phase | Result | Key finding |
| --- | --- | --- |
| 3.1 Proxy / Forwarded Headers | PASS WITH INFRA CONFIRMATION | No forwarded-header middleware; `X-Forwarded-*` and `X-Real-IP` are inert; spoofing cannot alter IP, scheme or host |
| 3.2 HSTS | **RESOLVED — DEPLOYED** | Infrastructure confirmed; HSTS enabled at `max-age=31536000`, no `includeSubDomains`, no `preload` |
| 3.3 IIS Acceptance | PASS | Site/pool Started; HTTPS health 200; anonymous protected 401; HTTP→HTTPS 307; host/SNI binding enforced |
| 3.4 HTTP.sys | A — ADEQUATE | Binding matches expected cert; TLS 1.2/1.3 and HTTP/2 enabled; revocation checking on; no tuning needed |
| 3.5 Network / WAF / LB | **RESOLVED** | Infrastructure confirmed no proxy/LB/WAF; direct IIS access; TLS terminated at IIS/HTTP.sys |
| 3.6 Capacity / Load | PASS (bounds) / DEFERRED (live load) | All resource bounds configured and fail-closed; live throughput not measured and correctly deferred |
| 3.7 Operational Release Review | **PASS** | All previously-open conditions resolved by the 2026-09-10 deployment |
| 3.8 Deployment Readiness | **A — READY / DEPLOYED** | 21-item checklist; all blocking items PASS; change authorized and executed |
| 3.x HSTS + AllowedHosts | **DEPLOYED & VERIFIED** | HSTS live; `AllowedHosts` = `DC01.lab.local`; 733 tests pass |

## Current Blockers
**No release blockers remain.** The three infrastructure/configuration blockers were resolved and the
approved change was deployed and verified. The items below are non-blocking follow-ups.

| # | Item | Phase | Type | Blocking? |
| --- | --- | --- | --- | --- |
| 1 | Live capacity figures unmeasured | 3.6 | Operational (needs isolated environment) | No |
| 2 | Monitoring/alerting not implemented | 3.7 | Operations (Phase 2D scope) | No |
| 3 | Real-user AD login acceptance not verified from reproducible evidence | 3.7 | Environment acceptance | No |
| 4 | Audit retention/purge not implemented | 3.7 | Database/operations | Noence | 3.7 | Environment acceptance |
| 7 | Audit retention/purge not implemented | 3.7 | Database/operations |

## Infrastructure Confirmations Required

**Forwarded headers / network (Phase 3.5):**
1. Is there any reverse proxy, load balancer or WAF between clients and this server?
2. If yes, does it terminate TLS, and over what scheme does traffic reach IIS?
3. What are its source IPs/CIDRs?
4. How many hops, and is the client IP preserved end-to-end?
5. Can clients reach IIS directly?
6. Are external firewall/NAT/segmentation rules consistent with the local allow rules?

**HSTS (Phase 3.2):**
7. Is HTTPS guaranteed for every production access path?
8. Are there any HTTP-only clients, probes or integrations?
9. Are subdomains expected to use HTTPS (is `includeSubDomains` appropriate)?
10. Is `preload` actually required?
11. What `max-age` is acceptable given rollback implications?
12. Is there a tested `max-age=0` rollback path?

**Deployed configuration (Phase 3.7):**
13. Should the source `AllowedHosts: DC01.lab.local` be deployed to replace the live `"*"`?

## Deferred Actions

| Item | Reason |
| --- | --- |
| HSTS enablement | Browser-persisted host-wide policy; needs topology + rollback rehearsal |
| Live load / capacity testing | No isolated non-production environment; production DoS risk |
| HTTP.sys tuning | Not required; adequate as-is |
| Forwarded-header trust (`KnownProxies`/`KnownNetworks`/`ForwardLimit`) | Cannot be configured without confirmed proxy addresses |
| Real IIS Security Slice 2 acceptance for a *new* release | Requires a separate authorized deployment window |
| Monitoring/readiness aggregation | Phase 2D scope |
| Durable audit spool / outbox | Separate Phase 2C architecture decision |
| Distributed / multi-instance concurrency limits | Separate distributed architecture |

## Current Deployment Identity

| Property | Value |
| --- | --- |
| Deployed product version | `1.0.0+6793324365d860085080ab53bb7ccdb3b4401c28` |
| Deployment path | `C:\Apps\LabAuthServer\Current` |
| Source release dir | `C:\Apps\LabAuthServer\Releases\release-20260910-222640` |
| IIS site / pool | `LabAuthServer` / `LabAuthServerAppPool` (both Started) |
| App pool identity | `LAB\svc_labauth` |
| HTTPS binding | `https *:443:DC01.lab.local`, cert `BD545B…` |
| HTTPS certificate | `CN=DC01.lab.local`, valid to 2027-08-26, RSA 2048 |
| JWT signing certificate | `94D4AC…`, `kid=lab-jwt-signing-20260907` |
| SQL | `tcp:DC01.lab.local,1433`, `Encrypt=True`, `TrustServerCertificate=False` |

## Backup / Rollback Information

| Item | Value |
| --- | --- |
| Latest backup | `C:\Apps\LabAuthServer\Backups\current-20260910-222656` (52 files) |
| Backup verified | YES |
| Rollback procedure | `docs/Safe_Deployment_Procedure.md` |
| Rollback performed | NO |

## Final Test Results

| Selection | Result |
| --- | --- |
| Unit tests | 487 passed |
| Integration tests | 242 passed |
| Total | **729 passed** |
| Failed | 0 |
| Skipped | 0 |
| Release warnings | 0 |
| Release errors | 0 |

## Live Verification Summary

| Probe | Result |
| --- | --- |
| `GET https://dc01.lab.local/api/v1/health` | 200, `{"status":"Healthy"}`, `Strict-Transport-Security: max-age=31536000`, `nosniff`/`DENY`/`no-store` |
| `GET http://dc01.lab.local/api/v1/health` | 307 → `https://dc01.lab.local/api/v1/health`, `nosniff`/`DENY`/`no-store`, no HSTS (correct) |
| `GET https://dc01.lab.local/api/v1/protected` | 401, `WWW-Authenticate: Bearer`, `Strict-Transport-Security: max-age=31536000`, same headers, empty body |
| `GET https://127.0.0.1/api/v1/health` | TLS rejected — cert does not cover IP literal |

## Safety Confirmation

- No deployment occurred during the Phase 3.4–3.8 *review* runs.
- The separate 2026-09-10 authorized change **did** deploy the application and recycle
  `LabAuthServerAppPool` — see [HSTS + AllowedHosts Implementation](Phase-3-HSTS-AllowedHosts-Implementation.md).
- No server restart occurred.
- No IIS binding, HTTP.sys, firewall, DNS, proxy, WAF or load-balancer change occurred.
- No SQL, secret or certificate change occurred.
- **HSTS WAS enabled** by the separate authorized change.
- No commit and no push occurred.
- Pre-existing worktree changes were preserved.

## Recommendation

The project has reached **READY FOR EXPLICIT DEPLOYMENT AUTHORIZATION** and the authorized HSTS +
AllowedHosts change has been deployed and validated. Remaining follow-ups (live capacity figures,
monitoring/alerting, real-user AD acceptance, audit retention) are tracked in the non-blocking table
above and should be scheduled under their own authorizations.

## Phase 3.3

The IIS acceptance result carried forward from the prior deployment (site/pool Started, HTTPS health
200, anonymous protected 401, HTTP→HTTPS 307, host/SNI binding enforced) was re-confirmed by the live
probes recorded above and requires no separate document.

## Infrastructure Confirmation Status

Full question sheet: [Phase 3 Infrastructure Confirmation](Phase-3-Infrastructure-Confirmation.md).

### VERIFIED LOCALLY

| # | Item | Status |
| --- | --- | --- |
| — | Git HEAD / branch / staged / diff-check | PASS — `6793324…`, `main`, 0, clean |
| — | Deployed artifact version + SHA256 + mtime | PASS — `1.0.0+6793324…`, `71A07E15…`, `2026-09-10T15:07:02Z` |
| — | IIS site / app pool state | PASS — both Started |
| — | IIS bindings 80/443 | PASS — `*:80:DC01.lab.local`, `*:443:DC01.lab.local` |
| — | HTTPS certificate subject/SAN/validity | PASS — `CN=DC01.lab.local`, SAN `DC01.lab.local`, to 2027-08-26 |
| — | HTTP.sys 443 binding hash + App ID | PASS — `bd545ba2…`, IIS App ID |
| — | Local reverse proxy / ARR / rewrite | PASS — none (0 modules) |
| — | Local WAF / load balancer | PASS — none observed |
| — | TLS terminated locally at IIS/HTTP.sys | PASS — yes |
| — | Local DNS resolution | PASS — `dc01.lab.local` → `192.168.56.138` (this host) |
| — | Ports 80/443 owner | PASS — PID 4 (HTTP.sys) |
| — | Site custom headers | PASS — 0 (no IIS-supplied HSTS) |
| — | HTTPS health / HTTP redirect / protected 401 | PASS — see live verification above |
| — | IP-literal access | PASS — TLS rejected |
| — | Forwarded headers trusted | PASS — none consumed |
| — | HSTS currently enabled | NO — header absent on every response |
| — | Live `AllowedHosts` | OBSERVED `*` (source `DC01.lab.local`) |

### REQUIRES INFRASTRUCTURE OWNER

| # | Question | Status |
| --- | --- | --- |
| 1 | Reverse proxy / LB / WAF between clients and IIS? | REQUIRES CONFIRMATION |
| 2 | If yes, which component/vendor? | REQUIRES CONFIRMATION |
| 3 | Does it terminate TLS? | REQUIRES CONFIRMATION |
| 4 | If so, HTTP or HTTPS forwarded to IIS? | REQUIRES CONFIRMATION |
| 5 | Source IPs/CIDRs IIS receives from the proxy | REQUIRES CONFIRMATION |
| 6 | Proxy hop count | REQUIRES CONFIRMATION |
| 7 | Original client IP preserved? | REQUIRES CONFIRMATION |
| 8 | Can clients reach IIS directly, bypassing the proxy? | REQUIRES CONFIRMATION |
| 9 | Is HTTPS guaranteed for every production access path? | REQUIRES CONFIRMATION |
| 10 | Any HTTP-only clients / probes / integrations? | REQUIRES CONFIRMATION |
| 11 | Any additional production hostnames/subdomains? | REQUIRES CONFIRMATION |
| 12 | Should subdomains be covered by HSTS? | REQUIRES CONFIRMATION |
| 13 | Is `includeSubDomains` required? | REQUIRES CONFIRMATION |
| 14 | Is `preload` required? | REQUIRES CONFIRMATION |
| 15 | Approved HSTS `max-age`? | REQUIRES CONFIRMATION |
| 16 | Tested `max-age=0` rollback procedure? | REQUIRES CONFIRMATION |
| 17 | Should production accept only `DC01.lab.local`? | REQUIRES CONFIRMATION |
| 18 | Additional exact hostnames to allow? | REQUIRES CONFIRMATION |
| 19 | Replace live `AllowedHosts: "*"`? | REQUIRES CONFIRMATION |

### Unknown / Not Establishable Locally

External firewall/NAT and perimeter segmentation remain UNKNOWN. No scan or external probe was
performed. Nothing was assumed or invented.