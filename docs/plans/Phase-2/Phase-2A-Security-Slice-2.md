# Phase 2A Security Slice 2

> Current scope: [Coding-only scope](Coding-Only-Scope.md). Source implementation and automated tests complete this coding slice. Deployment, real IIS acceptance and operational release review are **DEFERRED**, not coding-completion blockers. Historical deployment checklists below apply only to a separately authorized future workstream.

2026-09-09: source implementation and automated validation complete. **NOT DEPLOYED.** Separate authorization is required for deployment and real IIS acceptance. This checkpoint supersedes earlier pending-decision text for A6 response headers and A7 AllowedHosts only. HSTS and proxy trust remain deferred.

## Baseline and ownership

Before editing, source and Current both had `AllowedHosts: "*"`. IIS bindings were `*:80:DC01.lab.local` and `*:443:DC01.lab.local`; Current uses ASP.NET Core in-process. No additional production alias was evidenced. Localhost launch profiles are development settings, not evidence for a production alias. Effective IIS custom headers were empty and client cache mode was `NoControl`. Live health returned JSON with correlation and no cache, MIME, framing, CSP, referrer, permissions or HSTS policy.

No reverse proxy, rewrite rule or forwarded-header configuration was found in the inspected source, Current web.config, applicationHost.config or process/user/machine enable flag. App-pool environment inspection showed no forwarding override. This is evidence for the direct local IIS path, not a complete inventory of all network infrastructure.

Pre-edit copies/hashes of all 171 tracked/nonignored source files, Current file hashes and IIS configuration hash are in `C:\Apps\LabAuthServer\Temp\Phase2A-SecuritySlice2-20260909`. `Baseline.md` records the pre-implementation response paths and order. All pre-existing dirty/untracked work is preserved.

## AllowedHosts

Source now specifies **DC01.lab.local only**. Existing ASP.NET Core default-builder HostFiltering is retained; `AllowEmptyHosts` is explicitly false. There is no second custom host parser and no IIS binding change.

| Host input | Application-host result |
| --- | --- |
| DC01.lab.local | Allowed |
| dc01.lab.local | Allowed, case-insensitive comparison |
| DC01.lab.local:443 | Allowed; host matching ignores the numeric port |
| Unknown name, direct IP, localhost | Not in the list; host filter returns 400 |
| Empty Host, `bad host`, comma-separated mixed names | Raw TestServer verifies 400 before login |
| DC01.lab.local:invalid | Raw TestServer bypasses native parsing; framework port parsing throws FormatException before the explicit pipeline |

Unknown-host 400 is framework middleware-generated, before correlation, authentication, LDAP, audit and endpoint processing; it is not an MVC routing response. The framework default HTML error body is retained. These outer responses do not acquire the API response policy or application correlation. Normal server syntax validation and IIS site selection may reject or route invalid/unmatched requests before LabAuthServer. Native status/body/header behavior remains pending separate IIS deployment testing. Do not claim all malformed syntax necessarily yields an application 400.

Port handling and case comparison follow the [ASP.NET Core HostString implementation](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.11/src/Http/Http.Abstractions/src/HostString.cs); early rejection and reliance on server syntax parsing follow [HostFilteringMiddleware](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.11/src/Middleware/HostFiltering/src/HostFilteringMiddleware.cs). Numeric ports are not a listener allowlist; IIS owns listeners. Spoofed X-Forwarded-Host does not make an unknown Host pass the tests.

## Response policy

`ApiResponseHeadersMiddleware` owns one final value for each header, using `OnStarting` before the response is sent:

- `Cache-Control: no-store`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`

The case-insensitive `/api` path-segment scope includes all current endpoints and API errors, including unknown API routes. It excludes unrelated paths such as `/public/logo.svg` and `/apiculture`. No static resources or HTML UI are currently mapped. An outer callback runs after downstream callbacks and assigns values instead of appending; tests prove it replaces a conflicting downstream cache/MIME/framing value without duplicates. IIS supplies no conflicting policy in the inspected configuration.

Covered response classes: login token 200; MVC validation/request 400; invalid credentials 401; directory unavailable 503; timeout 504; returned cancellation category 499; configuration/unexpected/post-authentication errors 500; protected 200/401/403 and application errors; API authentication/authorization-related 4xx/5xx; body 413, aggregate/Authorization 431, oversized JWT 401, limiter 429; global application errors under `/api`. The policy is independent of status, so downstream short circuits are covered.

**Health decision:** anonymous liveness is explicitly `no-store`; stale Healthy responses would mislead probes. Health remains JSON and does not establish LDAP, SQL or certificate availability. Public resources outside `/api` retain their own caching behavior.

Status, response bytes, content type, WWW-Authenticate and correlation are not modified. Existing bearer challenges remain intact. Returned cancellation 499 is covered; an actually aborted connection has no guarantee of a deliverable response, and cancellation/exception propagation is unchanged. Native IIS errors and outer host-filter rejections remain outside the explicit response middleware.

## Related-header assessment

None of these headers was supplied by inspected IIS configuration.

| Header | Decision and priority | Scope, rationale and compatibility |
| --- | --- | --- |
| Cache-Control | Implement now, P0 | `/api`; token and identity responses must not be stored; fresh health is deliberate. Clients relying on cached API results will need live requests. |
| X-Content-Type-Options | Implement now, P0 | `/api`; preserve declared JSON MIME type. No content-type changes; clients relying on sniffing would be incompatible. |
| X-Frame-Options | Implement DENY now, P1 defense in depth | `/api`; no embedding contract or HTML UI exists. Browser framing is intentionally denied; ordinary JSON API clients are unaffected. |
| Content-Security-Policy | Defer, later browser/HTML review | No executable HTML document surface; a speculative global CSP lacks a document policy to protect. Future UI needs endpoint-specific script/style/embed inventory to avoid breaking resources. |
| Referrer-Policy | Defer, later browser/HTML review | No API document navigation/link surface requiring a new policy. Future browser pages should assess document scope and referrer-dependent integrations; this header does not redact inbound Referer. |
| Permissions-Policy | Defer, later browser/HTML review | No document requesting browser features. Future HTML endpoints need a feature inventory before restrictions that could break required features. |
| Strict-Transport-Security | Assess only; deferred ingress/TLS work | Host-wide browser persistence has operational implications beyond this JSON API. Explicitly disabled in this slice. |

### HSTS assessment, not implementation

For internal `DC01.lab.local`, HSTS could help compatible browser clients insist on HTTPS once a trusted HTTPS response establishes policy. Its value for service clients depends on client support. Before enablement, inventory the intended hostname, ingress/TLS termination and all consumers; establish continuously valid, trusted certificates for every HTTPS path. Existing local certificate success is not proof for all client trust stores or routes.

Scope would be the exact host; includeSubDomains would additionally affect names below `DC01.lab.local`, not sibling names under `lab.local`. Do not infer those subdomains support HTTPS. Browser-cached max-age outlives application rollback: recovery needs working trusted HTTPS and a delivered max-age=0 policy, and cannot instantly reach offline clients. Internal `.local` naming is not a public preload deployment target; no preload submission or directive is proposed. Monitor HTTPS availability, certificate expiry/renewal and client behavior, and rehearse recovery before a separately approved rollout. **Defer until hostname and ingress/proxy inventory is complete.** No UseHsts, HSTS response header, includeSubDomains or preload configuration was added.

### Forwarding

Client -> IIS HTTPS binding -> ASP.NET Core in-process -> LabAuthServer is preserved. No UseForwardedHeaders, KnownProxies, KnownNetworks, or trust of X-Forwarded-For/Proto/Host was configured. A future discovered proxy requires explicit review of trusted hops, scheme and host handling.

## Middleware order

The default host filter remains outside the explicit pipeline. Explicit order is now response headers -> global exception -> correlation -> HTTPS redirection -> routing -> general-header limit -> login-body limit -> login limiter -> Authorization-size limit -> authentication -> authorization audit -> authorization -> health/controllers. Every existing relative ordering is unchanged. Registering the response callback before global exceptions covers their generated 500 responses as well as 413/431/429 short circuits.

## Verification and deployment boundary

- Full solution: **283 passed, 0 failed, 0 skipped** (175 unit, 108 integration); 35 tests added to the 248-test baseline.
- Release build: **0 warnings, 0 errors**. Initial successful compilation reported two deprecated test-host API warnings; the fixture was migrated to generic HostBuilder and the final build is clean.
- New tests exercise approved/case/port/unknown/IP/empty/malformed Host inputs, spoofed forwarded Host, pre-business rejection, login failure categories, 8191/8192/8193 login outcomes, protected 200/401/403, safe global 500, health/correlation, 400/429, 413/431, cancellation propagation, scope and conflicting downstream policy replacement.
- Existing JWT 12287/12288/12289 and Authorization 12351/12352/12353 regressions, tampering and ingress unit tests pass unchanged. Existing HTTP test fixtures only changed their request host to the production-approved name.
- No new package or dependency. Tests use synthetic authentication and signing material through existing fixtures; no real-user credential was requested or persisted.

Prior JWT, Authorization, login-body and aggregate-header controls are **COMPLETED AND DEPLOYED**. The separately completed HTTP/2 investigation established that IIS contributes `Connection: close`, accounting for 19 bytes in the managed envelope; correcting methodology proved 16128 accepted / 16129 rejected. No production fix was needed. See local evidence `Phase2A-BodyHeaderDeploy-20260909-065832/Deployment-Report.md` and `Phase2A-Http2Accounting-20260909-072752/Diagnostic-Report.md` under `C:\Apps\LabAuthServer\Temp`.

This source slice is ready for deployment review, not live acceptance. During a separately authorized deployment, preserve deployment-specific SQL/key/AD settings and change only the reviewed host policy alongside the built code; do not replace Current appsettings wholesale with repository defaults. Inventory caller/probe Host values, validate effective allowlist, both HTTP versions, headers on the entire response matrix, native versus managed rejection, no IIS duplication, normal login/protected flow and existing exact size boundaries. Use actual managed header accounting for HTTP/2. TestServer does not establish IIS wire behavior. Local development must use an explicit per-development host configuration or an approved Host request; localhost is intentionally absent from production defaults.

No completed RSA/JWT/Authorization/body/header implementation was changed. No deployment, IIS change, HTTP.sys change, SQL change, certificate change, restart, commit or push occurred. HEAD remains `6793324365d860085080ab53bb7ccdb3b4401c28`; worktree remains intentionally dirty.

## Final change inventory and quality gate

This slice changes 13 existing files and adds 5; all other pre-existing work is preserved. Paths are relative to the repository root.

| Files | Slice change |
| --- | --- |
| src/LabAuthServer.Api/Program.cs | Configure empty-host rejection; insert response middleware |
| src/LabAuthServer.Api/appsettings.json | Only AllowedHosts changes relative to the pre-slice file |
| src/LabAuthServer.Api/Middleware/ApiResponseHeadersMiddleware.cs | New centralized response policy |
| tests/LabAuthServer.IntegrationTests/ApiSecurityResponseTests.cs | New application response and Host matrix |
| tests/LabAuthServer.IntegrationTests/ApiResponseHeadersMiddlewareTests.cs | New isolated response preservation/scope tests |
| tests/LabAuthServer.UnitTests/ApiResponseHeadersMiddlewareTests.cs | New cancellation/exception propagation tests |
| tests/LabAuthServer.IntegrationTests/HealthEndpointTests.cs | Approved request hostname |
| tests/LabAuthServer.IntegrationTests/ProtectedEndpointTests.cs | Approved factory hostname |
| tests/LabAuthServer.IntegrationTests/JwtSizeBoundaryTests.cs | Approved factory/request hostname; prior boundary assertions retained |
| tests/LabAuthServer.IntegrationTests/IngressSizeBoundaryTests.cs | Approved raw request hostname |
| docs/Architecture.md | Middleware order and response boundary |
| docs/Configuration.md | Host configuration and deployment preservation |
| docs/Project_Status.md | Completed prior deployment and source-only Slice 2 status |
| docs/Security.md | Implemented/deferred headers and scope |
| docs/Testing.md | 283-test result and new coverage |
| docs/Validation_Status.md | Current source/deployment evidence boundaries |
| docs/plans/Phase-2/Phase-2A-API-Resource-Protection.md | Checkpoint superseding earlier A6/A7 decisions |
| docs/plans/Phase-2/Phase-2A-Security-Slice-2.md | This implementation/assessment report |

Final `git diff --check` exits 0. Git emits existing Windows LF/CRLF conversion notices; no whitespace errors were found. New files also pass a separate trailing-whitespace scan. Git status remains intentionally dirty: 21 modified tracked files plus pre-existing/new untracked files (full list in `git-after.txt` in the evidence directory). Nothing is staged or committed by this task; HEAD remains unchanged.

Final integrity inspection confirms all 52 Current files are unchanged, the Current file count is unchanged, applicationHost.config has its original hash, and all existing production source except Program.cs/appsettings.json has its original pre-slice hash. Source appsettings differs semantically only in AllowedHosts. No completed size-control implementation or budget changed.

No unresolved source test/build failures remain. Remaining acceptance concerns are operational: deployment-specific configuration/overrides, caller/probe hostname inventory, actual IIS handling of invalid Host/native errors and no duplicate headers after rollout. Those require the separate authorized deployment; this report does not claim new live behavior.

**NO DEPLOYMENT. NO IIS CHANGE. NO HTTP.sys CHANGE. NO SQL CHANGE. NO CERTIFICATE CHANGE. NO RESTART. NO COMMIT. NO PUSH.**
