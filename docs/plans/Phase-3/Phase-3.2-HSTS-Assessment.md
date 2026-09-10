# Phase 3.2 — HSTS Assessment and Safe Readiness Validation

Date: 2026-09-10. Mode: **READ-ONLY at time of writing**.

> **RESOLVED 2026-09-10.** The infrastructure confirmations listed in section 8 were subsequently
> provided: no proxy, no load balancer, no WAF/ADC, no upstream TLS termination, direct IIS access,
> HTTPS-only production access, and `DC01.lab.local` as the sole production hostname. HSTS was then
> enabled under explicit authorization with `max-age=31536000` and **no** `includeSubDomains` or
> `preload`. See [HSTS + AllowedHosts Implementation](Phase-3-HSTS-AllowedHosts-Implementation.md).
> This document is retained as the pre-implementation assessment record.

Related: [Phase 3.1 proxy/forwarded-header validation](Phase-3.1-Proxy-Forwarded-Headers-Validation.md),
[Phase 2A Security Slice 2](../Phase-2/Phase-2A-Security-Slice-2.md),
[coding-only scope](../Phase-2/Coding-Only-Scope.md).

## 1. Executive Summary

HSTS is **not implemented, not enabled and not deployed**. Source contains no `UseHsts`, `AddHsts`,
`HstsOptions` or `Strict-Transport-Security` reference, and no live response — HTTPS or HTTP — carries
the header. The application instead relies on `UseHttpsRedirection()` plus a controller-level
`Request.IsHttps` check.

The environment is **technically prepared** for HSTS on the inspected direct route: HTTPS is bound and
valid, the certificate is currently valid and covers the exact hostname, HTTP redirects to HTTPS, and
the three existing security headers are intact. However, HSTS is a browser-persisted host-wide policy
whose safety depends on facts this host cannot establish — ingress/TLS-termination topology, HTTP-only
consumers, subdomain expectations and whether `includeSubDomains`/`preload` are wanted.

**Decision: C — HSTS REQUIRES INFRASTRUCTURE CONFIRMATION.**

## 2. Safety Gate

| Item | Value |
| --- | --- |
| HEAD | `6793324365d860085080ab53bb7ccdb3b4401c28` (unchanged) |
| Branch | `main` |
| Staged files | 0 |
| `git diff --check` | clean (0) |
| Modified files | 39 (all under `src|tests|docs`) |
| Untracked files | 70 |

No reset, clean, stash, commit or push occurred. Existing uncommitted Phase 2/3.1 work was preserved.

## 3. Current HSTS State

| Question | Answer |
| --- | --- |
| Is `UseHsts()` present? | No |
| Is `AddHsts()` present? | No |
| Is an HSTS middleware present? | No |
| Is `Strict-Transport-Security` emitted on HTTPS? | No |
| Is it emitted on the HTTP redirect? | No |
| Is `max-age`, `includeSubDomains` or `preload` configured anywhere? | No |
| Is IIS configured to emit HSTS? | No — IIS bindings carry no custom HSTS header |
| Enabled during this task? | **NO** |

## 4. Application HSTS Implementation Review

A search of `src/**` for `UseHsts`, `AddHsts`, `Strict-Transport-Security`, `StrictTransportSecurity`,
`HstsOptions` and `MaxAge` returned **no matches**. There is no HSTS configuration key in
`appsettings.json` and no environment-specific HSTS branch.

If HSTS were ever enabled, the correct single location would be `Program.cs` in the existing pipeline —
either `app.UseHsts()` alongside the current `app.UseHttpsRedirection()`, or an added assignment inside
`ApiResponseHeadersMiddleware`. **Neither was added.** `Program.cs` is unchanged by this task.

Existing security headers are independent of HSTS and remain owned by `ApiResponseHeadersMiddleware`,
which sets them on `/api` responses via `OnStarting`:

| Header | Value | Observed live |
| --- | --- | --- |
| `Cache-Control` | `no-store` | Yes |
| `X-Content-Type-Options` | `nosniff` | Yes |
| `X-Frame-Options` | `DENY` | Yes |
| `Strict-Transport-Security` | (absent) | Confirmed absent |

## 5. Live HTTPS/HTTP Verification

Probes ran against the real IIS endpoint using default certificate validation.

| Probe | Result |
| --- | --- |
| `GET https://dc01.lab.local/api/v1/health` | `200 OK`, body `{"status":"Healthy"}`, `nosniff`, `DENY`, `no-store`, no HSTS |
| `GET http://dc01.lab.local/api/v1/health` (redirects disabled) | `307 TemporaryRedirect`, `Location: https://dc01.lab.local/api/v1/health`, `nosniff`, `DENY`, `no-store`, no HSTS |
| `GET https://dc01.lab.local/api/v1/protected` (anonymous) | `401 Unauthorized`, `WWW-Authenticate: Bearer`, `nosniff`, `DENY`, `no-store`, empty body |
| `GET https://127.0.0.1/api/v1/health` | TLS rejected — certificate does not cover the IP literal |

The HTTP redirect does **not** emit HSTS, which is correct: a browser only honours HSTS received over
HTTPS. The HTTPS responses omit it, which is the current intended state.

## 6. Security Header Verification

All three existing headers were present on the 200, the 307 and the 401 after the 2026-09-10 deployment
of `ApiResponseHeadersMiddleware`. `Strict-Transport-Security` was absent on all of them, confirming the
header policy and HSTS remain independent.

## 7. Certificate Verification

| Property | Value |
| --- | --- |
| Subject | `CN=DC01.lab.local` |
| Issuer | `CN=LAB-ROOT-CA, DC=lab, DC=local` |
| NotBefore | `2026-08-26T14:50:33Z` |
| NotAfter | `2027-08-26T14:50:33Z` |
| Key size | RSA 2048 |
| Has private key | True |
| Subject Alternative Name | `DC01.lab.local` only |
| HTTPS binding | `https *:443:DC01.lab.local`, thumbprint `BD545BA289EBFC645C8C3DC424311975579D7E09`, store `MY` |

Certificate validity is not the HSTS blocker. The relevant observation is the **single-name SAN**: the
certificate covers exactly `DC01.lab.local` with no wildcard and no sibling SANs, so it provides no
evidence that any other name (including subdomains) is served over valid HTTPS.

HTTP.sys state for `0.0.0.0:443` shows TLS 1.2 and TLS 1.3 not disabled, HTTP/2 not disabled, client
certificate revocation checking enabled, and no custom HSTS or Ctl binding. No HTTP.sys change was made.

## 8. Infrastructure Unknowns

The following cannot be established from this host or the repository. They are
**UNKNOWN — requires infrastructure confirmation**:

1. Is there a reverse proxy, load balancer or WAF between clients and IIS?
2. If yes, does it terminate TLS before IIS?
3. Are there multiple proxy hops, and what are their address ranges?
4. Can clients reach IIS directly, bypassing the proxy?
5. Are there any HTTP-only clients or integrations that would break under a forced-HTTPS policy?
6. Are any subdomains of `DC01.lab.local` expected to serve HTTPS?
7. Is any non-`DC01.lab.local` hostname served by this application?
8. Does any consumer cache or depend on plain HTTP behavior?
9. Is a rollout window and rollback rehearsal available for a browser-persisted policy?

No proxy, load-balancer, WAF, DNS or browser topology was assumed or invented.

## 9. HSTS Risks

| Risk | Assessment |
| --- | --- |
| Browser persistence outlives rollback | `max-age` is cached by the browser; an application rollback does not clear it. Recovery requires a delivered `max-age=0` over valid HTTPS. |
| `includeSubDomains` blast radius | Not proposed. Would affect names below the host, not siblings under `lab.local`. The certificate SAN provides no subdomain coverage evidence. |
| `preload` irreversibility | Not proposed. Preload list removal is slow and requires browser-vendor action. |
| HTTP-only consumers | Unknown. If any exist, they would fail once policy is cached. |
| TLS termination upstream | Unknown. If TLS terminates before IIS, the application currently sees plain HTTP and would reject login with 400 rather than silently accepting it — fail-closed, but a functional impact. |
| Internal `.local` naming | Not a viable public preload target; no preload submission is proposed. |
| Certificate coverage | Single SAN only; adding subdomains would require a new certificate before any `includeSubDomains` decision. |

## 10. Test Results

| Selection | Result |
| --- | --- |
| Focused security-header / host tests | 2 unit + 40 integration passed, 0 failed, 0 skipped |
| Full suite | 487 unit + 242 integration = **729 passed**, 0 failed, 0 skipped |
| Release build | 0 warnings, 0 errors |

Counts match the post-deployment baseline exactly. No test was modified.

## 11. Decision

**C — HSTS REQUIRES INFRASTRUCTURE CONFIRMATION.**

Application-side state is clean and HSTS-ready in the sense that nothing blocks adding it and the
existing header policy is independent. But the safety of a browser-persisted, host-wide policy cannot
be established without the answers in section 8. Enabling HSTS on this evidence alone would be a guess.

## 12. Required Confirmation Before HSTS Enablement

The infrastructure/application owners must answer:

1. Is HTTPS guaranteed for every production access path to this host?
2. Does any reverse proxy or load balancer terminate TLS before IIS?
3. Are there any HTTP-only clients, probes or integrations?
4. Are subdomains expected to use HTTPS (is `includeSubDomains` appropriate)?
5. Is `preload` actually required, or is a plain `max-age` sufficient?
6. What `max-age` is acceptable given the rollback implications?
7. Is there a tested rollback path that can deliver `max-age=0` over valid HTTPS?
8. Are all client types (service clients, browsers, scripts) known and TLS-capable?

## 13. Explicitly Deferred Actions

| Item | Status |
| --- | --- |
| `app.UseHsts()` | NOT added |
| `AddHsts()` / `HstsOptions` configuration | NOT added |
| `Strict-Transport-Security` response header | NOT emitted |
| `includeSubDomains` | NOT configured |
| `preload` | NOT configured |
| IIS HSTS configuration | NOT changed |
| HTTP.sys changes | NONE |
| Certificate changes | NONE |

### Safety statement

- HSTS was **NOT** enabled.
- IIS was **NOT** changed.
- HTTP.sys was **NOT** changed.
- No deployment occurred.
- No restart or app-pool recycle occurred.
- No commit occurred.
- No push occurred.
- No reset, clean or stash occurred.
- No unrelated working-tree change was overwritten.

The only artifact produced by this task is this assessment document.