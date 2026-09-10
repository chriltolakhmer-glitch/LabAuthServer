# Coding-only scope and deferred controls

User-approved scope, 2026-09-09. This scope governs current Phase 2A application-development work and supersedes infrastructure/release acceptance requirements in earlier plans for the current coding workstream. Deferred controls are not implemented or validated, and are not blockers to completing authorized application coding.

## Deferred work

| Activity | Status | Reason and current behavior |
| --- | --- | --- |
| Proxy / forwarded-header infrastructure validation | DEFERRED | Requires actual ingress topology, proxy/WAF/load-balancer, TLS termination, trusted IP and X-Forwarded-* verification. Current direct-IIS application behavior is unchanged; no unrestricted forwarding trust is configured and no source change is required for that topology. |
| HSTS deployment | DEFERRED | Requires hostname, certificate, subdomain, ingress and persistent browser-policy validation. HSTS remains disabled; no HSTS application coding is authorized. |
| Real IIS acceptance for Security Slice 2 | DEFERRED | Requires deployment, restart/recycle, production configuration checks and real HTTP/1.1/HTTP/2 probing. Source implementation is covered by automated tests. Deployment is not required for coding-only completion. |
| HTTP.sys configuration/tuning | DEFERRED | Operating-system/IIS infrastructure work. No MaxFieldLength or MaxRequestBytes changes are required. |
| Network/WAF/load-balancer validation | DEFERRED | Requires infrastructure access and network-path testing, rather than application source implementation. |
| Infrastructure capacity/load testing | DEFERRED | Requires production-like infrastructure, workload generation, monitoring and operational capacity analysis. Application-level resource protections and their automated tests remain in scope. |
| Final operational release review | DEFERRED | Deployment approval, change management, operational monitoring, rollback execution and release sign-off belong to a separate release workstream. |

## Retained application-development scope

- Authentication/LDAP failure classification.
- LDAP cancellation and request deadlines.
- Bounded LDAP concurrency/work.
- Authentication resource protection.
- Audit/resource bounds implementable in application code.
- Automated unit and integration tests for implemented controls.
- Application-level security behavior and middleware.
- Source documentation explaining implemented controls.

This scope defines eligible work; it does not establish unreviewed limit values or authorize unrelated architecture changes.

## Scope rule

Do not implement infrastructure-only controls merely to satisfy a Phase 2A checklist. When a requirement needs IIS, HTTP.sys, network, proxy, WAF, browser, deployment or operational changes rather than application source code, mark it **DEFERRED** with its reason. Do not introduce application workarounds for deferred infrastructure controls.

Deferred activities may be revisited as a separately authorized infrastructure/release workstream. Preserve historical evidence of completed deployments; coding-only completion does not assert new production deployment or operational acceptance.
