# Phase 3.7 — Operational Release Review

Date: 2026-09-10. Mode: **READ-ONLY**. Nothing was changed during this review.

Related: [Phase 3.4](Phase-3.4-HTTPsys-Assessment.md), [Phase 3.5](Phase-3.5-Network-WAF-LB-Validation.md),
[Phase 3.6](Phase-3.6-Capacity-Load-Assessment.md), [Phase 3.2](Phase-3.2-HSTS-Assessment.md).

## 1. Result

**PASS WITH CONDITIONS.**

The application, deployment state, TLS, audit and rollback posture are ready. Three conditions carry
forward: HSTS requires infrastructure confirmation (Phase 3.2), the external proxy/WAF/LB path is
UNKNOWN (Phase 3.5), and a **deployed-config divergence** was found (see §7).

## 2. A — Application

| Item | Status | Evidence |
| --- | --- | --- |
| Authentication | PASS | LDAPS 636, typed failure model, cooperative deadline, tests |
| Authorization | PASS | Reader/Operator/Administrator, default-deny, role precedence |
| JWT | PASS | RS256, `kid`, previous-key overlap, 12288/12352/7680 budgets |
| LDAP reliability | PASS | Cancellation/deadline at every stage, hard waiter cap 16 |
| Group membership bounds | PASS | `MaximumGroupMemberships` default 100, fail-closed |
| Resource bounds | PASS | Body 8192, envelope 16128, Authorization 12352, rate 10/min |
| Security headers | PASS | `no-store`, `nosniff`, `DENY` live on 200/307/401 |
| Host validation | **CONDITION** | Source `DC01.lab.local`; deployed `"*"` — see §7 |
| HTTPS redirect | PASS | HTTP → `307` → `https://dc01.lab.local/api/v1/health` |
| Audit behavior | PASS | SQL writer, bounded fields, best-effort, primary response preserved |

## 3. B — Deployment

| Item | Value |
| --- | --- |
| Deployed artifact | `ProductVersion=1.0.0+6793324365d860085080ab53bb7ccdb3b4401c28` |
| Deployment path | `C:\Apps\LabAuthServer\Current` |
| DLL mtime (UTC) | `2026-09-10T15:07:02Z` |
| Source release | `C:\Apps\LabAuthServer\Releases\release-20260910-222640` (52 files, no PDBs, no dev settings) |
| Backup | `C:\Apps\LabAuthServer\Backups\current-20260910-222656` (52 files) |
| Server-specific appsettings | Preserved during the 2026-09-10 deploy |
| IIS site | `LabAuthServer`, Started |
| App pool | `LabAuthServerAppPool`, Started |
| Bindings | `http *:80:DC01.lab.local`, `https *:443:DC01.lab.local` |

The deployed assembly version string embeds the current HEAD, confirming the binary corresponds to
commit `6793324365d860085080ab53bb7ccdb3b4401c28`.

## 4. C — Observability

| Item | Status |
| --- | --- |
| Application logs | PASS — structured, bounded category/stage/reason, no secrets |
| Audit records | PASS — SQL `Audit.usp_WriteAuditEvent`, bounded fields |
| IIS logging | PASS — default, no Authorization/body fields selected |
| Correlation ID | PASS — `X-Correlation-ID` propagated and returned |
| Error handling | PASS — generic client messages, no stack traces |

## 5. D — Security

| Item | Status |
| --- | --- |
| TLS (HTTPS) | PASS — `CN=DC01.lab.local`, valid to 2027-08-26, RSA 2048, revocation checking enabled |
| JWT signing certificate | PASS — separate cert `94D4AC5345479614B945096CC9CEDE87C48FC51B`, `kid=lab-jwt-signing-20260907` |
| SQL TLS | PASS — `Encrypt=True`, `TrustServerCertificate=False`, `tcp:DC01.lab.local,1433` |
| Security headers | PASS — live on all API responses |
| HSTS | NOT ENABLED — Phase 3.2 decision C |
| Forwarded-header trust | PASS — none consumed, spoofing inert |
| Host validation | **CONDITION** — deployed `"*"` (see §7) |
| Request limits | PASS — body/header/Authorization/JWT bounds enforced |

## 6. E — Testing

| Item | Result |
| --- | --- |
| Unit tests | 487 passed |
| Integration tests | 242 passed |
| Live IIS probes | health 200, protected 401, HTTP 307 |
| Regression baseline | matches the stated 729 baseline exactly |

## 7. FINDING — Deployed `AllowedHosts` divergence

**This is a reported finding, not a change.** No configuration was modified.

| Aspect | Detail |
| --- | --- |
| Source value | `AllowedHosts: "DC01.lab.local"` |
| Deployed value | `AllowedHosts: "*"` |
| Location | `C:\Apps\LabAuthServer\Current\appsettings.json` |
| Effect | ASP.NET Core Host Filtering accepts any `Host` value instead of only `DC01.lab.local` |
| Not affected | HTTPS enforcement, `Request.IsHttps`, forwarded-header trust, JWT, LDAP, audit |
| Why it was preserved | The 2026-09-10 deployment deliberately preserved the server-specific appsettings; changing it would be an unauthorized production configuration change |

The `AllowedHosts: "*"` value predates the current deployment and is the live server value. Source
`DC01.lab.local` is not yet deployed. Reconciling this is a **configuration/deployment decision for the
owner**, not an application defect — the application code is correct; the deployed data differs.

Documentation evidence gathered 2026-09-10 confirms `DC01.lab.local` is the sole intended production
hostname. `docs/Security.md` states "The sole production hostname allowlist is `DC01.lab.local`";
`docs/Configuration.md` records that source `AllowedHosts` was changed to `DC01.lab.local` "based on
the inspected HTTP/HTTPS IIS bindings"; `docs/Architecture.md` describes the host filter as accepting
only `DC01.lab.local`. No document names an additional production hostname, a subdomain, or a
localhost/IP requirement. Full detail in
[Phase 3 Infrastructure Confirmation](Phase-3-Infrastructure-Confirmation.md) Part D.

Additional deployed-vs-source differences, all resolving to safe code defaults:

| Setting | Deployed | Code default | Effect |
| --- | --- | --- | --- |
| `AuthenticationTimeout` | absent | 30 s | bounded |
| `MaxConcurrentLdapOperations` | absent | 4 | bounded |
| `MaxPendingLdapWaiters` | absent | 16 | bounded |
| `MaximumGroupMemberships` | absent | 100 | bounded |
| `ConnectionTimeout` | `00:00:10` | 10 s | same |

These four use the explicit safe model defaults, so no behavior change or unsafe state results. They
are recorded for transparency.

## 8. F — Rollback

| Item | Status |
| --- | --- |
| Latest valid backup | `C:\Apps\LabAuthServer\Backups\current-20260910-222656` (52 files) — **exists** |
| Rollback procedure documented | YES — `docs/Safe_Deployment_Procedure.md` |
| Rollback performed | **NO** |

## 9. G — Known Blockers and Gaps

> **UPDATED 2026-09-10 (post-implementation).** The first three blockers below were resolved by the
> authorized HSTS + AllowedHosts deployment. See
> [HSTS + AllowedHosts Implementation](Phase-3-HSTS-AllowedHosts-Implementation.md).

| Item | Status |
| --- | --- |
| HSTS enablement | **RESOLVED** — enabled at `max-age=31536000`, no `includeSubDomains`, no `preload` |
| External proxy/WAF/LB topology | **RESOLVED** — infrastructure confirmed none; direct IIS access |
| Deployed `AllowedHosts: "*"` vs source | **RESOLVED** — live value is now `DC01.lab.local` |
| Live capacity/throughput figures | DEFERRED — no isolated test environment (Phase 3.6) |
| Real-user AD login acceptance | NOT VERIFIED from reproducible repository evidence |
| Audit retention/purge | NOT IMPLEMENTED |

## 10. Explicitly Not Changed

No deployment, no IIS change, no HTTP.sys change, no appsettings change, no secret change, no
certificate change, no firewall/DNS/proxy change, no restart, no app-pool recycle, no commit, no push.

## 11. Conclusion

Operationally the service is healthy and its security controls are enforced and verified. Following the
2026-09-10 authorized deployment, the three previously-open conditions are resolved: HSTS is enabled
with the approved policy, the external topology is confirmed as direct-to-IIS with no proxy/LB/WAF, and
the deployed `AllowedHosts` now matches source at `DC01.lab.local`. The review is therefore
**PASS**. Remaining non-application items (live capacity figures, monitoring/alerting, real-user AD
acceptance, audit retention) are tracked separately in §9 and are not release blockers for this change.