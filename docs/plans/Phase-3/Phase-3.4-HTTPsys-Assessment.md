# Phase 3.4 — HTTP.sys Assessment

Date: 2026-09-10. Mode: **READ-ONLY**. No HTTP.sys change was made.

Related: [Phase 3.1 proxy/forwarded-header validation](Phase-3.1-Proxy-Forwarded-Headers-Validation.md),
[Phase 3.2 HSTS assessment](Phase-3.2-HSTS-Assessment.md).

## 1. Objective

Determine whether the current HTTP.sys configuration presents an application or security issue
requiring remediation. Read-only inspection only.

## 2. Result

**A — ADEQUATE.** No HTTP.sys configuration change is required or recommended.

## 3. SSL Certificate Binding

| Property | Value |
| --- | --- |
| IP:port | `0.0.0.0:443` |
| Certificate Hash | `bd545ba289ebfc645c8c3dc424311975579d7e09` |
| Application ID | `{4dc3e181-e14b-4a21-b022-59fc669b0914}` (IIS) |
| Certificate Store Name | `MY` |
| Verify Client Certificate Revocation | Enabled |
| Verify Revocation Using Cached Client Certificate Only | Disabled |
| Usage Check | Enabled |
| Negotiate Client Certificate | Disabled |
| Reject Connections | Disabled |
| Disable HTTP2 | Not Set |
| Disable QUIC | Not Set |
| Disable TLS1.2 | Not Set |
| Disable TLS1.3 | Not Set |
| Disable Legacy TLS Versions | Not Set |
| Extended Property 0 Receive Window | 1048576 |
| Extended Property 1 Max Settings Per Frame | 2796202 |

The bound hash matches the IIS HTTPS binding thumbprint `BD545BA289EBFC645C8C3DC424311975579D7E09`
verified in Phase 3.2. The Application ID is the standard IIS HTTP.sys application identifier, so the
binding is owned by IIS as expected — not by a stray process.

## 4. Competing Bindings

`netsh http show sslcert` lists many additional `0.0.0.0:443xx` entries (ports 44300–44343 and similar)
plus WSS/SSTP/TermService reservations under `https://+:443/sra_{BA195980-...}/`. Those are default
Windows/IIS Express/Terminal Services registrations on distinct ports or URL prefixes, not competing
listeners on `0.0.0.0:443`. No unexpected second certificate is bound to port 443.

## 5. URL Reservations

No URLACL reservation exists for the LabAuthServer site. This is expected: IIS in-process hosting
registers its listeners through the application pool SID via the IIS configuration, not through an
explicit `netsh http add urlacl` entry. Reservations present are all default Windows services:

| Reserved URL | Owner |
| --- | --- |
| `http://*:5357/`, `https://*:5358/` | `BUILTIN\Users`, `NT AUTHORITY\LOCAL SERVICE` |
| `http://+:80/Temporary_Listen_Addresses/` | `\Everyone` |
| `http://*:2869/` | `NT AUTHORITY\LOCAL SERVICE` |
| `https://+:5986/wsman/`, `http://+:47001/wsman/`, `http://+:5985/wsman/` | `NT SERVICE\WinRM`, `NT SERVICE\Wecsvc` |
| `https://+:3392/rdp/`, `http://+:3387/rdp/` | `NT SERVICE\TermService` |
| `https://+:443/sra_{BA195980-...}/` | `NT SERVICE\SstpSvc`, Administrators, SYSTEM |
| `http://+:10247/apps/` | `NT AUTHORITY\Authenticated Users` |
| `https://+:10245/WMPNSSv4/`, `http://+:10243/WMPNSSv4/` | `NT SERVICE\WMPNetworkSvc` |
| `http://+:10246/MDEServer/` | `NT AUTHORITY\Authenticated Users` |

None of these grants a non-administrative process a broad reservation on 80 or 443 that would conflict
with IIS.

## 6. IP Listen List

`netsh http show iplisten` reports an **empty** list. HTTP.sys therefore binds on all local addresses
(equivalent to `0.0.0.0`/`::`). This matches the `*:80:DC01.lab.local` and `*:443:DC01.lab.local` IIS
bindings and is the normal configuration. Restricting the IP listen list was not performed and is not
recommended without an approved network design.

## 7. Timeouts

| Setting | Value |
| --- | --- |
| Idle connection timeout | 120 seconds |
| Header wait timeout | 120 seconds |

Both are Windows defaults. They are not tuned, not exposed as an application concern, and are not a
finding.

## 8. Request Queues

| Queue | Active processes | Registered URLs | Max connections | Queue timeout |
| --- | --- | --- | --- | --- |
| (unnamed, HTTP.sys system) | 1 | 2 | inherited | 120 s |
| `DefaultAppPool` | 0 | 1 | 4294967295 | 65535 s |
| `LabAuthServerAppPool` | **1** | **2** | 4294967295 | 65535 s |

`LabAuthServerAppPool` has one attached worker process and two registered URLs, matching the site's
HTTP and HTTPS bindings. `Max connections: 4294967295` is the inherited/unlimited default; application
and IIS-level limits are what bound work in practice. This was not changed.

## 9. HTTP Response Cache

`netsh http show cachestate` returned an empty snapshot — no cached responses. Consistent with the
application's `Cache-Control: no-store` policy on `/api`.

## 10. Relationship to Application-Level Protections

HTTP.sys provides transport-level limits; the application provides the bounded resource controls that
were verified in Phases 2A–2C:

| Control | Layer | Value |
| --- | --- | --- |
| Login request body | Application | 8192 UTF-8 bytes |
| Decoded general envelope | Application | 16128 bytes |
| Authorization header value | Application | 12352 bytes |
| Encoded JWT ceiling | Application | 12288 bytes |
| Issuance payload | Application | 7680 bytes |
| Claim size | Application | 4096 bytes |
| Login rate limit | Application | 10 requests/minute, no queue |
| Max concurrent LDAP operations | Application | 4 (code default) |
| Max pending LDAP waiters | Application | 16 (code default) |
| Maximum group memberships | Application | 100 (code default) |
| Authentication deadline | Application | 30 s (code default) |

HTTP.sys does not need to duplicate these. Application limits fail closed and return deterministic
status codes (`413`, `431`, `429`, `401`, `503`).

## 11. Verified Items

- HTTPS binding maps to the expected certificate hash.
- Certificate hash matches the expected HTTPS certificate thumbprint.
- No unexpected competing binding on `0.0.0.0:443` was identified.
- Port 80 and 443 behavior is understood: both bound through HTTP.sys (PID 4 / System) and routed to IIS.
- HTTP → HTTPS redirect remains functional (`307` with correct `Location`).
- TLS 1.2 and TLS 1.3 are not disabled; HTTP/2 is not disabled.
- No obvious insecure HTTP.sys configuration was found.

## 12. Unknown / Not Established

- Effective TLS cipher suite ordering was not enumerated.
- HTTP/3 (QUIC) actual client negotiation was not exercised.
- External network reachability of 80/443 is UNKNOWN (see Phase 3.5).

## 13. Explicitly Not Changed

- No `netsh http` mutation of any kind.
- No SSL certificate binding change.
- No URL reservation added or removed.
- No IP listen list entry added.
- No timeout, cache, or connection-limit tuning.
- No restart, no app-pool recycle.
- No deployment, commit, or push.

## 14. Conclusion

HTTP.sys is **adequate** for the current deployment. Every inspected setting is either a Windows
default or matches the intended IIS binding. Nothing inspected indicates an application or security
defect, and no tuning is recommended merely because a tuning option exists.