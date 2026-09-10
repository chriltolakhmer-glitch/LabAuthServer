# Phase 3 — HSTS + AllowedHosts Implementation

Date: 2026-09-10. Authorized configuration/deployment task. **No commit. No push.**

Related: [3.1](Phase-3.1-Proxy-Forwarded-Headers-Validation.md) · [3.2](Phase-3.2-HSTS-Assessment.md) ·
[3.7](Phase-3.7-Operational-Release-Review.md) · [3.8](Phase-3.8-Deployment-Readiness.md) ·
[Final Status](Phase-3-Final-Status.md) · [Infrastructure Confirmation](Phase-3-Infrastructure-Confirmation.md).

## 1. Infrastructure confirmations received

| Item | Confirmed value |
| --- | --- |
| Reverse proxy / load balancer / WAF / ADC | **None** |
| Upstream TLS termination | **None** — IIS/HTTP.sys terminates TLS directly |
| Direct client access to IIS | **Expected** |
| Forwarded-header configuration | **Not required** |
| Original client connection info | **Preserved** |
| Production access | **HTTPS-only**; no HTTP-only requirement identified |
| Production hostname | **`DC01.lab.local` only** |

## 2. Approved decisions

| Setting | Approved value |
| --- | --- |
| `AllowedHosts` | `DC01.lab.local` |
| `Strict-Transport-Security` | `max-age=31536000` |
| `includeSubDomains` | **Not used** |
| `preload` | **Not used** |

## 3. Previous state

| Aspect | Before |
| --- | --- |
| Source `AllowedHosts` | `DC01.lab.local` (already correct — no source change) |
| Live `AllowedHosts` | `*` |
| HSTS in source | Absent (`UseHsts`/`AddHsts`/`Strict-Transport-Security` = 0 matches) |
| HSTS live | Absent on every response |
| Deployed version | `1.0.0+6793324365d860085080ab53bb7ccdb3b4401c28` |

## 4. New state

| Aspect | After |
| --- | --- |
| Source `AllowedHosts` | `DC01.lab.local` (unchanged) |
| Live `AllowedHosts` | `DC01.lab.local` |
| HSTS in source | `AddHsts` + `UseHsts` in `Program.cs` |
| HSTS live | `max-age=31536000` on HTTPS responses |
| Deployed version | `1.0.0+6793324365d860085080ab53bb7ccdb3b4401c28` + HSTS build |
| Deployed DLL SHA256 | `EEA12B480062ACF586738E29C635673AC5452895A761BDDD58D7649697913D53` |

## 5. HSTS implementation

Implemented once, in the existing HTTPS pipeline in `src/LabAuthServer.Api/Program.cs`:

```csharp
builder.Services.AddHsts(options =>
{
    // Approved Phase 3 configuration: one year, no includeSubDomains, no preload.
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = false;
    options.Preload = false;
});
```

```csharp
app.UseHsts();
app.UseHttpsRedirection();
```

Placement: after `CorrelationMiddleware`, before `UseHttpsRedirection`. `UseHsts()` emits the header only
on HTTPS responses — never on plain HTTP, which is correct (a browser only honours HSTS received over
HTTPS).

No second HSTS implementation and no custom middleware were added. `ApiResponseHeadersMiddleware`
continues to own `Cache-Control`, `X-Content-Type-Options` and `X-Frame-Options` and was not changed.

## 6. AllowedHosts implementation

Source `appsettings.json` already contained `"AllowedHosts": "DC01.lab.local"` — no source change was
required. The live `appsettings.json` value `"*"` was replaced with `DC01.lab.local` during deployment,
while every other server-specific value was preserved exactly (SQL connection string, JWT signing key
id, certificate thumbprint, token budgets).

## 7. Backup

| Item | Value |
| --- | --- |
| Backup path | `C:\Apps\LabAuthServer\Backups\current-20260910-231902` |
| File count | 52 |
| Verified readable | Yes |

The pre-existing backup `current-20260910-222656` was left untouched.

## 8. Deployment

| Step | Result |
| --- | --- |
| Publish directory | `C:\Apps\LabAuthServer\Releases\release-20260910-231849` (52 files, 0 PDBs, no dev settings) |
| HSTS in published artifact | Yes |
| Header middleware in artifact | Yes |
| App pool stopped | Yes (`LabAuthServerAppPool`) |
| Deployment path | `C:\Apps\LabAuthServer\Current` (52 files) |
| Server-specific config preserved | Yes |
| `AllowedHosts` applied | `DC01.lab.local` |
| App pool started | Yes |
| Site state | Started |
| Deployment time | 2026-09-10T23:19 local |

## 9. Live verification

| Probe | Result |
| --- | --- |
| `GET https://dc01.lab.local/api/v1/health` | **200 OK**; `Strict-Transport-Security: max-age=31536000`; `Cache-Control: no-store`; `X-Content-Type-Options: nosniff`; `X-Frame-Options: DENY`; body `{"status":"Healthy"}` |
| `GET https://dc01.lab.local/api/v1/protected` (anonymous) | **401 Unauthorized**; `Strict-Transport-Security: max-age=31536000`; `WWW-Authenticate: Bearer`; `no-store`; `nosniff`; `DENY`; empty body |
| `GET http://dc01.lab.local/api/v1/health` | **307 TemporaryRedirect**; `Location: https://dc01.lab.local/api/v1/health`; same three headers; **no HSTS** (correct) |
| Approved host `DC01.lab.local` | **200 OK** — accepted |
| Unapproved host `unapproved.example` | Rejected at TLS/SNI (certificate mismatch) before reaching the application; application-level host filter rejection is proven by `ApiSecurityResponseTests.HostFiltering_EnforcesApprovedNameBeforeLogin` |
| `https://127.0.0.1/api/v1/health` | TLS rejected — certificate does not cover the IP literal |

`max-age=31536000` contains no `includeSubDomains` and no `preload`.

## 10. Regression results

| Selection | Result |
| --- | --- |
| Pre-deploy Release build | 0 warnings, 0 errors |
| Full suite (pre-deploy) | 487 unit + 246 integration = **733 passed**, 0 failed, 0 skipped |
| Full suite (post-deploy) | 487 unit + 246 integration = **733 passed**, 0 failed, 0 skipped |

Four new focused HSTS tests were added in `ApiSecurityResponseTests.cs`; `AssertPolicy` now also asserts
the approved HSTS value. No existing test was removed, disabled or weakened.

## 11. Rollback procedure

Not required — all Gate 11 critical checks passed.

If rollback had been required:

1. Stop `LabAuthServerAppPool`.
2. Restore `C:\Apps\LabAuthServer\Backups\current-20260910-231902` over `C:\Apps\LabAuthServer\Current`.
3. Start `LabAuthServerAppPool`.
4. Re-run the HTTPS health probe and confirm 200.

**Browser note:** HSTS persists in browsers after the server stops sending it. Rolling back the server
does **not** clear client-side HSTS. Recovery requires delivering `max-age=0` over valid HTTPS. This is
inherent to HSTS and was accepted as part of the approved decision.

## 12. Remaining risks

| Risk | Assessment |
| --- | --- |
| Browser-cached HSTS outlives rollback | Accepted; recovery requires `max-age=0` over valid HTTPS |
| Certificate expiry | Valid to 2027-08-26; renewal must retain the `DC01.lab.local` SAN |
| Single-name certificate | `includeSubDomains` was not enabled, so no subdomain coverage is required |
| Unknown external topology | Superseded — infrastructure confirmed no proxy/LB/WAF/TLS termination |
| Live capacity figures | Still unmeasured (Phase 3.6); unrelated to this change |

## 13. Explicit safety confirmation

- Application **was** deployed (explicitly authorized).
- App pool **was** recycled (explicitly authorized, required by the deployment procedure).
- No server restart occurred.
- No IIS binding change, no HTTP.sys change, no firewall/DNS/proxy/WAF/LB change.
- No SQL configuration or SQL TLS change.
- No certificate change.
- No JWT signing configuration change.
- No LDAP limit change.
- No unrelated application setting change.
- **No commit. No push.**
- Pre-existing worktree changes were preserved.