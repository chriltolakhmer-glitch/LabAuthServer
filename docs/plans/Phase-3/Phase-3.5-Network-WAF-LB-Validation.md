# Phase 3.5 — Network / WAF / Load Balancer Validation

Date: 2026-09-10. Mode: **READ-ONLY**. No network, firewall, DNS, WAF or load-balancer change was made.

Related: [Phase 3.1 proxy/forwarded-header validation](Phase-3.1-Proxy-Forwarded-Headers-Validation.md),
[Phase 3.4 HTTP.sys assessment](Phase-3.4-HTTPsys-Assessment.md).

## 1. Objective

Determine what can actually be verified **from this server** about the network, proxy, WAF and load
balancer path. Do not assume topology. Record everything else as UNKNOWN.

## 2. Result

**PASS WITH INFRASTRUCTURE CONFIRMATION.** No local reverse proxy, WAF or load balancer exists on this
host, and the application fails closed on forwarded headers. The external request path beyond this
server cannot be established locally and remains UNKNOWN.

## 3. Locally Verified Evidence

### 3.1 DNS resolution

| Name | Type | Address |
| --- | --- | --- |
| `DC01.lab.local` | A | `192.168.56.138` |
| `DC01.lab.local` | AAAA | `fe80::55b0:5aa:ca5:f567` |

The name resolves directly to this host's own address. No hosts-file override was present. This is
consistent with a direct route, not with a name pointing at an intermediate device.

### 3.2 Local network interfaces

| IPAddress | InterfaceAlias | PrefixLength |
| --- | --- | --- |
| `192.168.56.138` | Ethernet0 | 24 |

A single IPv4 address on a single interface. No secondary VIP or loopback-based proxy address was
observed.

### 3.3 Listening sockets on relevant ports

| LocalAddress | LocalPort | OwningProcess |
| --- | --- | --- |
| `::` | 80 | 4 (`System` / HTTP.sys) |
| `::` | 443 | 4 (`System` / HTTP.sys) |
| `0.0.0.0` | 1433 | 5824 (SQL Server) |
| `::` | 1433 | 5824 (SQL Server) |

Ports 80 and 443 are owned by HTTP.sys (PID 4), which is the expected IIS in-process model. No
third-party process (nginx, haproxy, Envoy, etc.) is listening on those ports. SQL Server listens on
1433 locally as expected.

### 3.4 Windows Firewall rules for 80/443

| DisplayName | Action | Enabled |
| --- | --- | --- |
| World Wide Web Services (HTTP Traffic-In) | Allow | True |
| World Wide Web Services (HTTPS Traffic-In) | Allow | True |
| World Wide Web Services (QUIC Traffic-In) | Allow | True |
| Windows Remote Management (HTTP-In) (×2) | Allow | True |
| DIAL protocol server (HTTP-In) (×2) | Allow | True |
| Cast to Device streaming server (HTTP-Streaming-In) (×3) | Allow | True |
| Core Networking - IPHTTPS (TCP-In) | Allow | True |

The relevant inbound allowances are the standard IIS rules for HTTP and HTTPS. No custom or
unexpected rule exposing LabAuthServer was identified. Firewall rules were **not** modified.

### 3.5 IIS proxy-related modules

A search of installed IIS global modules for `Rewrite`, `ApplicationRequestRouting`, `ARR`, `Proxy`,
`WebSocket` and `HttpRedirection` returned **no matches**. There is no local ARR, URL Rewrite or proxy
module installed. This independently corroborates Phase 3.1.

### 3.6 IIS custom headers on the site

`system.webServer/httpProtocol/customHeaders` for the `LabAuthServer` site is **empty**. IIS is not
emitting HSTS, CSP, or any other custom header that would compete with the application's
`ApiResponseHeadersMiddleware`.

### 3.7 Application forwarded-header behaviour

Independently re-confirmed from Phase 3.1 evidence: `src/**` contains no `UseForwardedHeaders`,
`ForwardedHeadersOptions`, `KnownProxies`, `KnownNetworks`, `ForwardLimit` or any read of
`X-Forwarded-For` / `X-Forwarded-Proto` / `X-Forwarded-Host` / `X-Real-IP`. `HttpContext.Connection.RemoteIpAddress`
is the socket peer.

## 4. Findings Against the Phase Questions

| # | Question | Finding |
| --- | --- | --- |
| 1 | Does the application receive the real socket peer IP? | **Yes.** `RemoteIpAddress` is the direct connection peer; no forwarding middleware exists to overwrite it. |
| 2 | Are `X-Forwarded-For` / `-Proto` / `-Host` trusted? | **No.** None is read. Spoofing cannot change IP, scheme or host. |
| 3 | Evidence of a local reverse proxy? | **None.** Ports 80/443 are HTTP.sys; no proxy process; no ARR/Rewrite module. |
| 4 | Evidence of IIS ARR? | **None.** Module search returned no matches. |
| 5 | Evidence of URL Rewrite acting as a proxy? | **None.** Module not installed. |
| 6 | Evidence of another proxy/LB/WAF on this server? | **None observed** on the relevant ports. |
| 7 | What network ports are locally exposed? | 80 and 443 (IIS/HTTP.sys), 1433 (SQL Server). Others are default Windows services. |
| 8 | What firewall rules apply? | Standard IIS HTTP/HTTPS/QUIC inbound allow rules; no custom LabAuthServer rule. |

## 5. UNKNOWN — Requires Infrastructure Confirmation

The following cannot be established from this host and are explicitly **UNKNOWN**:

| Item | Status |
| --- | --- |
| External load balancer in front of this server | UNKNOWN |
| WAF in front of this server | UNKNOWN |
| Upstream reverse proxy outside this server | UNKNOWN |
| Location of TLS termination | UNKNOWN (locally it terminates at IIS/HTTP.sys on 443) |
| Number of external proxy hops | UNKNOWN |
| Trusted proxy CIDR ranges | UNKNOWN |
| Direct external client reachability to this host | UNKNOWN |
| External firewall / NAT / perimeter rules | UNKNOWN |
| Production network segmentation | UNKNOWN |
| Client IP preservation end-to-end | UNKNOWN beyond this host |

No proxy, load balancer, WAF, DNS or firewall topology was assumed or invented.

## 6. Security Implications

Because the application does not consume forwarded headers, the security implications of any unknown
upstream device are limited to:

1. **Observability, not bypass.** If a proxy exists and does not preserve the source IP, audit
   `ClientIp` would record the proxy address rather than the true client. This degrades forensic value
   but cannot be exploited to change an authorization decision.
2. **TLS termination.** If TLS terminates upstream of IIS, the application would see plain HTTP and
   reject login with `400` (`HTTPS is required`) rather than silently accepting it — fail-closed.
3. **Rate limiting.** The `Login` policy uses a constant partition (`"login"`), so it is unaffected by
   client IP either way. It cannot be bypassed by forwarded-header spoofing.
4. **No spoofing surface.** With no forwarded-header consumption, there is no header-trust path to
   abuse from a direct or proxied client.

## 7. Explicitly Not Performed

- No network scan of external infrastructure.
- No firewall rule change.
- No DNS change.
- No proxy/LB/WAF configuration change.
- No packet capture.
- No production traffic test.
- No HTTP.sys change.
- No restart, no deployment, no commit, no push.

## 8. Required Infrastructure Confirmations

1. Is there any reverse proxy, load balancer or WAF between clients and this server?
2. If yes, does it terminate TLS, and over what scheme does traffic reach IIS?
3. What are its source IP addresses or CIDRs (for any future `KnownProxies`/`KnownNetworks` design)?
4. How many hops exist, and is the client IP preserved end-to-end?
5. Can clients reach IIS directly, bypassing any device?
6. Are external perimeter firewall/NAT/segmentation rules consistent with the local allow rules?

## 9. Conclusion

No local network, proxy, WAF or load-balancer defect was found. The application's refusal to consume
forwarded headers means it fails closed regardless of what exists upstream. The external request path
remains UNKNOWN and must be confirmed by the infrastructure owner before any forwarding trust is
introduced.