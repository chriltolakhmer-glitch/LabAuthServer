# Phase 7 deployment record — 2026-09-20

Status: **DEPLOYMENT COMPLETE**. Post-deployment smoke completed at **2026-09-20 16:38:18 UTC**. Solo developer execution on `DC01.lab.local`, Windows Server 2022 Standard x64, domain-controller role 5. User authorized the scoped ACL correction, minimum test-account membership changes, IIS-context smoke, rollback readiness check and conditional deployment.

## Immutable baseline and paths

- Source/tag: `784fa96b9436aee315fae2d7e669a2650dbff794` / `v1.0.0`.
- ZIP: `C:\Apps\LabAuthServer\Releases\v1.0.0-preparation-20260913-212903\LabAuthServer-1.0.0.zip`.
- Verified SHA256: `564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0`.
- Verified candidate path: `C:\Apps\LabAuthServer\Staging\v1.0.0-Phase7-20260920`.
- Final deployed path: `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`; HTTPS endpoint `https://DC01.lab.local` on the existing port 443 binding.
- All 51 extracted artifact files independently matched their ZIP-entry hashes. External `appsettings.json` was copied separately and matches the existing runtime configuration; no artifact file or ZIP was changed.
- Previous deployment retained unchanged: `C:\Apps\LabAuthServer\Current`.
- Site/pool: `LabAuthServer` / `LabAuthServerAppPool`; identity `LAB\svc_labauth`, confirmed from the actual worker process during smoke.
- Private operational evidence: `C:\Apps\LabAuthServer\Backups\Phase7-20260920`.

## Scoped changes

Removed only BUILTIN\Users write/create/append grants from the `Current`, `Releases`, `Backups`, `Backup`, and `Staging` directory trees beneath `C:\Apps\LabAuthServer`. Root inheritance was protected while preserving existing entries so inherited write grants could be removed without changing the parent or unrelated directories. Remaining entries were preserved. Recursive inspection found zero remaining Users write grants.

| Principal | Before | After |
| --- | --- | --- |
| BUILTIN\Users | Read/execute, create files, append/create subdirectories | Read/execute and synchronize |
| BUILTIN\Administrators | Full Control | Full Control |
| SYSTEM | Full Control | Full Control |
| LAB\svc_labauth | Read/execute through Users membership | Same; actual IIS startup confirms runtime access |

Original root security descriptors are retained in `Acl-Before.json`. The new private evidence directory is restricted to Administrators/SYSTEM; its rollback copy receives runtime read access only if activated for recovery.

Neither Reader group had recursive test users. The user selected an existing enabled test account currently mapped to Operator. For each smoke phase, only its direct application memberships are temporarily changed from `GG-APP-APPROVER` to `GG-APP-USER`, then restored before the Operator check. Other memberships are unchanged; no account is created. This is necessary because v1.0.0 issues only the highest mapped role and does not expand nested memberships. Credentials are entered in a local secure prompt and used only in memory. No password, token, key, DPAPI plaintext or connection string is retained in this record or smoke evidence.

The pre-deployment test temporarily uses the existing HTTPS site and pool with the candidate directory, then restores the original physical path in cleanup. No separate IIS identity, certificate, DNS record or firewall rule is introduced.

## Pre-deployment smoke

Initial test proved trusted HTTPS/health 200, anonymous 401, Reader login/JWT/Reader 200, Operator login/JWT/Reader denial 403, and persisted login/grant/denial audit rows. A verifier defect required a role on the denial audit row, but the released middleware intentionally omits that field. Read-only SQL inspection confirmed the matching denial row with status 403 and null role. Only the external verification script was corrected; the application remains unchanged. Initial results are preserved in `Pre-Smoke-InitialVerifier.json`.

Corrected combined test: **PASS**, completed at 2026-09-20 16:37:02 UTC. Actual worker ownership was `LAB\svc_labauth`; SQL session inspection independently showed that login with encrypted TCP. Successful real-user logins and accepted issued tokens prove DPAPI decryption and JWT signing in the actual IIS context. Original IIS path and test-account membership were restored after the pre-deployment test. Full sanitized results: `Pre-Smoke.json` in the private evidence directory.

## Rollback

**READY — RESTORE NOT EXECUTED.** Fresh preserved copy: `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Previous`, 52 files, all matching the existing deployment by hash. Application files, external configuration and runtime dependencies are present. Configured DPAPI file and signing certificate still exist on the same machine.

Previous API DLL SHA256: `eea12b480062acf586738e29c635673ac5452895a761bddd58d7649697913d53`. IIS configuration backup: `Phase7-20260920`. `Recovery.json` records paths, identity, backup parity and restoration steps.

Recovery: stop only the application pool, point the site back to the unchanged `Current` directory, restore corresponding IIS settings if required, start the pool and repeat trusted HTTPS/health/authentication checks. If the private preserved copy must be activated instead, first grant the runtime identity read/execute on that copy without opening it to general users. No SQL history deletion, database restore, key export or destructive rollback rehearsal is required.

## Deployment and post-deployment smoke

After Steps 1–3 passed, copied the verified candidate to the final versioned deployment directory, checked all 51 artifact files and the separately supplied external configuration, stopped only `LabAuthServerAppPool`, changed the existing site's physical path and started the same pool. Site bindings, certificate, pool identity and external application settings were preserved. No machine-wide IIS reset occurred.

| Check | Pre-deployment | Post-deployment |
| --- | --- | --- |
| Trusted HTTPS and health | PASS — 200 | PASS — 200 |
| Anonymous Reader resource | PASS — 401 | PASS — 401 |
| Actual IIS worker identity | PASS — LAB\svc_labauth | PASS — LAB\svc_labauth |
| DPAPI and JWT signing/issuance | PASS through real login and accepted JWT | PASS through real login and accepted JWT |
| Reader login / issued role / authorization | PASS — 200 / Reader / 200 | PASS — 200 / Reader / 200 |
| Operator login / issued role / Reader denial | PASS — 200 / Operator / 403 | PASS — 200 / Operator / 403 |
| Application-identity SQL session | PASS — encrypted TCP | PASS — encrypted TCP |
| Corresponding SQL audit rows | PASS — four correlated rows | PASS — four correlated rows |
| Original test-account memberships restored | PASS | PASS |
| Operator-specific resource HTTP 200 | NOT APPLICABLE | NOT APPLICABLE |

The artifact exposes only the Reader-protected resource; no endpoint was invented. The same existing test account exercised both roles sequentially and now has its original Operator membership, not a permanent Reader grant. Full post-deployment evidence: `Post-Smoke.json` and `Deployment.json` in the private evidence directory.

### Sanitized SQL evidence

| Phase | Event | Role in row | HTTP | Correlation ID |
| --- | --- | --- | --- | --- |
| Pre | AUTH_LOGIN_SUCCESS | Reader | 200 | c8300bb7-4555-4a68-b6ed-83589f721686 |
| Pre | AUTHZ_ACCESS_GRANTED | Reader | 200 | 2437fea0-85e6-42c1-94d5-ebbad4c45ea4 |
| Pre | AUTH_LOGIN_SUCCESS | Operator | 200 | 576d4543-2456-4579-987b-3d05d769e58d |
| Pre | AUTHZ_ACCESS_DENIED | Not populated by released middleware | 403 | b1a2f2d5-eacd-46cd-a9de-60d67f124546 |
| Post | AUTH_LOGIN_SUCCESS | Reader | 200 | 7f8ac5f1-b0bc-46dd-9ae8-bfacdec1a91d |
| Post | AUTHZ_ACCESS_GRANTED | Reader | 200 | dd98bb4d-7574-4ebe-a43f-f03a28c5570c |
| Post | AUTH_LOGIN_SUCCESS | Operator | 200 | 3890bbf4-c7bf-40f6-8d92-465c30f22dbf |
| Post | AUTHZ_ACCESS_DENIED | Not populated by released middleware | 403 | 5ec5805f-b773-40aa-a4bd-6e75acfe9b65 |

### Remaining limitations

No critical deployment issue remains. Rollback readiness is verified without executing a destructive restore, as requested. Tests ran from the server through its real hostname/IIS HTTPS endpoint; remote-client routing, load and wider operational acceptance are not claimed. Health is liveness only; dependency acceptance comes from real authentication and correlated SQL evidence. SQL audit remains the release's existing best-effort implementation.

No application source, JWT/LDAP configuration, SQL schema, certificates or DNS changed. No rebuild, commit, tag, release or GitHub change was performed.
