# Phase 3.8 — Deployment Readiness

Date: 2026-09-10. Mode: **READ-ONLY / PLANNING**. **No deployment was performed. No deployment is authorized by this document.**

Related: [Phase 3.4](Phase-3.4-HTTPsys-Assessment.md), [Phase 3.5](Phase-3.5-Network-WAF-LB-Validation.md),
[Phase 3.6](Phase-3.6-Capacity-Load-Assessment.md), [Phase 3.7](Phase-3.7-Operational-Release-Review.md),
[Safe Deployment Procedure](../../Safe_Deployment_Procedure.md).

## 1. Readiness Decision

> **UPDATED 2026-09-10 (post-implementation).** The three conditions below were resolved by the
> authorized HSTS + AllowedHosts deployment. See
> [HSTS + AllowedHosts Implementation](Phase-3-HSTS-AllowedHosts-Implementation.md).

**A — READY FOR EXPLICIT DEPLOYMENT AUTHORIZATION** (was C before the 2026-09-10 change).

The application artifact, tests, TLS, SQL, rollback and IIS state are ready. The infrastructure owner
confirmed no proxy/LB/WAF, direct IIS access, HTTPS-only production access and `DC01.lab.local` as the
sole hostname; HSTS was enabled at the approved policy and the live `AllowedHosts` was aligned to source.
The deployment was executed and validated successfully.

## 2. Readiness Checklist

| # | Item | State | Note |
| --- | --- | --- | --- |
| 1 | Application artifact | PASS | `release-20260910-231849`, 52 files, no PDBs, no dev settings; HSTS + header middleware present |
| 2 | Tests | PASS | 487 unit + 246 integration = 733 passed, 0 failed, 0 skipped |
| 3 | Database | PASS | `LabAuthServer` reachable via `tcp:DC01.lab.local,1433`, `Encrypt=True` |
| 4 | SQL TLS | PASS | `TrustServerCertificate=False`; CA-issued cert with matching SAN (Phase 2A evidence) |
| 5 | JWT signing certificate | PASS | `94D4AC5345479614B945096CC9CEDE87C48FC51B`, `kid=lab-jwt-signing-20260907` |
| 6 | IIS | PASS | Site `LabAuthServer` Started; pool `LabAuthServerAppPool` Started; identity `LAB\svc_labauth` |
| 7 | HTTPS | PASS | `https://dc01.lab.local/api/v1/health` → 200; cert valid to 2027-08-26 |
| 8 | HTTP redirect | PASS | HTTP → `307` → `https://dc01.lab.local/api/v1/health` |
| 9 | Security headers | PASS | `no-store`, `nosniff`, `DENY` on 200/307/401 |
| 10 | HSTS | **PASS** | Enabled — `max-age=31536000`; no `includeSubDomains`; no `preload` |
| 11 | Host validation | **PASS** | Live and source `AllowedHosts` = `DC01.lab.local` |
| 12 | Forwarded headers | PASS | None consumed; spoofing inert (Phase 3.1/3.5) |
| 13 | HTTP.sys | PASS | Adequate; no change required (Phase 3.4) |
| 14 | Firewall/network | PASS (local) | Standard IIS HTTP/HTTPS/QUIC inbound allow rules (Phase 3.5) |
| 15 | Proxy/LB/WAF | **PASS** | Infrastructure confirmed none; direct IIS access |
| 16 | Resource limits | PASS | All bounds configured, fail-closed, tested (Phase 3.6) |
| 17 | Logging/audit | PASS | Structured logs, SQL audit, correlation, bounded fields |
| 18 | Backup | PASS | `current-20260910-231902`, 52 files |
| 19 | Rollback | PASS | Documented in `Safe_Deployment_Procedure.md`; backup exists |
| 20 | Monitoring | DEFERRED | No alerting/readiness aggregation implemented; Phase 2D scope |
| 21 | Change authorization | **GRANTED** | 2026-09-10 HSTS + AllowedHosts authorization received and executed |
| 21 | Change authorization | REQUIRED | Not yet granted for any new deployment |

## 3. Current Deployment Identity

| Property | Value |
| --- | --- |
| Deployed product version | `1.0.0+6793324365d860085080ab53bb7ccdb3b4401c28` |
| Deployment path | `C:\Apps\LabAuthServer\Current` |
| DLL last write (UTC) | `2026-09-10T15:07:02Z` |
| Source release dir | `C:\Apps\LabAuthServer\Releases\release-20260910-222640` |
| Backup dir | `C:\Apps\LabAuthServer\Backups\current-20260910-222656` |
| IIS site / pool | `LabAuthServer` / `LabAuthServerAppPool` |
| App pool identity | `LAB\svc_labauth` |
| Hosting model | `inprocess`, `AspNetCoreModuleV2` |

## 4. Pre-Deployment Steps (for a future authorized window)

1. **Backup** — take a fresh timestamped copy of `C:\Apps\LabAuthServer\Current`.
2. **Record current version** — capture DLL `ProductVersion` and mtime.
3. **Stage the new package** — publish to a new `Releases\release-<timestamp>` directory; verify file count, absence of PDBs/dev settings, and presence of expected middleware types.
4. **Configuration verification** — compare deployed `appsettings.json` against the intended values; **preserve server-specific keys** (SQL connection string, certificate thumbprints, LDAP paths) exactly as done on 2026-09-10.
5. **Certificate verification** — confirm the HTTPS and JWT signing certificates are valid and accessible to the pool identity.
6. **IIS verification** — confirm bindings, pool identity, and `web.config` hosting model.
7. **Database connectivity** — confirm `Encrypt=True`, `TrustServerCertificate=False`, and reachability.

## 5. Post-Deployment Validation Steps

8. **Health check** — `GET https://dc01.lab.local/api/v1/health` → 200 `{"status":"Healthy"}`.
9. **Authentication test** — a controlled successful login returning a token.
10. **Invalid credential test** — expect `401`.
11. **Protected endpoint test** — anonymous `401` with `WWW-Authenticate: Bearer`; valid token `200`.
12. **Security header test** — `no-store`, `nosniff`, `DENY` present; HSTS absent unless separately authorized.
13. **JWT validation test** — verify `iss`, `aud`, `kid`, `exp`, `role`.
14. **LDAP failure test** — confirm `503`/`504` classification with generic client messages.

## 6. Rollback Procedure

15. **Rollback procedure** — restore the pre-deployment backup over `C:\Apps\LabAuthServer\Current`; re-apply preserved server-specific configuration.
16. **Rollback trigger conditions** — health endpoint non-200; authentication regression; security headers missing; JWT validation failure; unexpected 5xx increase; LDAP/SQL failure misclassification.

## 7. Operations

17. **Monitoring** — track health, HTTP status distribution, LDAP failure categories, audit write outcomes.
18. **Post-deployment validation** — repeat steps 8–14 and confirm the full test suite still reports 729 passed.
19. **Change approval** — explicit written authorization required before any deployment.
20. **Operator sign-off** — named operator records version, timestamp, validation outcome, and rollback retention.

## 8. Conditions Blocking a "Ready" Verdict

> The first three conditions were **resolved** by the 2026-09-10 authorized change.

| Condition | Owner | Status / required action |
| --- | --- | --- |
| HSTS enablement | Infrastructure / application owner | **RESOLVED** — enabled at `max-age=31536000`; no `includeSubDomains`; no `preload` |
| External proxy/WAF/LB topology | Infrastructure owner | **RESOLVED** — confirmed none; direct IIS access; HTTPS-only |
| Deployed `AllowedHosts: "*"` | Application/config owner | **RESOLVED** — live value is now `DC01.lab.local` |
| Live capacity figures | Operations | OPEN (non-blocking) — provide an isolated non-production environment (Phase 3.6 §6) |
| Monitoring/alerting | Operations | OPEN (non-blocking) — implement per Phase 2D before broader production acceptance |
| Real-user AD login acceptance | AD/app owner | OPEN (non-blocking) — controlled live acceptance test |

## 9. Explicit Safety Confirmation

- No deployment occurred.
- No restart occurred.
- No app-pool recycle occurred.
- No IIS change occurred.
- No HTTP.sys change occurred.
- No certificate change occurred.
- No firewall, DNS, proxy, WAF or load-balancer change occurred.
- **HSTS WAS enabled** by the separate 2026-09-10 authorized change (this section describes the
  earlier planning review; see [HSTS + AllowedHosts Implementation](Phase-3-HSTS-AllowedHosts-Implementation.md)).
- No commit occurred.
- No push occurred.

## 10. Conclusion

The system is **ready for an explicit deployment authorization decision** on all application-side
criteria. The verdict is **C — REQUIRES INFRASTRUCTURE CONFIRMATION** because three non-application
items remain open. Once those are answered and a written change authorization is issued, the
pre/post-deployment checklist in §4–§7 is sufficient to execute a controlled release.