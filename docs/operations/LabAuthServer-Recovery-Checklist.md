# LabAuthServer incident/recovery checklist

Use the [operations runbook](LabAuthServer-Operations-Runbook.md) for verified paths and commands. Server: `DC01.lab.local`; site: `LabAuthServer`; pool: `LabAuthServerAppPool`. Record UTC time and safe correlation IDs. Never record passwords, tokens, connection strings or key material.

1. **Check health:** normally validated HTTPS to `https://DC01.lab.local/api/v1/health`; expect 200/Healthy. Liveness does not prove dependencies.
2. **Check IIS:** pool/site state, physical path `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`, pool identity `LAB\svc_labauth`. Record unexpected drift.
3. **Check recent diagnostics:** Windows Application log/ANCM startup events and `C:\inetpub\logs\LogFiles\W3SVC2`. Runtime stdout capture is disabled; do not assume an application file log exists.
4. **Check SQL/audit:** trusted encrypted connection to `tcp:DC01.lab.local,1433`, database `LabAuthServer`; inspect correlated rows in `Audit.AuditEvents`. Do not change schema/permissions or bypass certificate trust to make a probe pass.
5. **Restart if appropriate:** use the runbook's pool-only restart, then health. Avoid machine-wide `iisreset` and repeated restart loops on this domain controller.
6. **If deployment is suspected:** use the verified path-switch rollback to `C:\Apps\LabAuthServer\Current`; preserved copy and IIS backup are recorded in the runbook. Verify backup parity/references first. Do not delete audit history. Rollback readiness is verified; a restore has not been rehearsed.
7. **Recheck service:** trusted HTTPS, health 200, real Reader login/Reader access 200, non-Reader login/Reader denial 403, corresponding SQL audit rows. Use secure local password entry. The existing test account is normally Operator; follow the documented temporary-membership procedure if using it for both roles and confirm restoration. Operator-specific HTTP 200 is NOT APPLICABLE.
8. **Record the incident:** symptoms, UTC timeline, correlations, checks, exact changes, restored/deployed path/version/hash, outcome and remaining issue. Keep evidence private and sanitized. Stop on missing rollback resources, TLS/DPAPI/JWT/login/SQL failure; investigate without weakening security or rebuilding the release.
