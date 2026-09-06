# Phase 16 - IIS Deployment

**Date:** 2026-09-04
**Status:** COMPLETE

## Deployment target

- Site: `LabAuthServer`
- Application pool: `LabAuthServerAppPool`
- Identity: `ApplicationPoolIdentity`
- Physical path: `C:\Apps\LabAuthServer\Current`
- HTTPS: `https://DC01.lab.local:443`
- Package: `Releases\2026-09-04_114744_Release`
- Rollback backup: `Releases\2026-09-04_114832_Current_Backup`

## Procedure performed

The live IIS site and pool were captured before deployment. The existing `Current` directory was backed up. The validated package was copied to `Current`, excluding its manifest and preserving the existing server-specific `appsettings.json`; development configuration was not copied. The app pool was recycled and no unrelated IIS, AD, LDAP, certificate, or SQL permission changes were made.

The preserved configuration initially lacked the approved Phase 11 `Audit` section and AD user-search base, causing a 500.30 startup failure. Those two approved non-secret settings were added to `Current\appsettings.json`; the site was recycled and passed validation.

## Gate evidence

- IIS site: Started.
- App pool: Started.
- Physical path: Correct.
- Identity: Correct.
- HTTPS health: HTTP 200 with `{"status":"Healthy"}`.
- Correlation header: Present and canonical.
- Protected endpoint without token: 401.
- Deployed binary startup from `Current`: successful in Production mode.

**PASS.**
