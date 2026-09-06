# Phase 15 - Deployment Package

**Date:** 2026-09-04
**Status:** COMPLETE

## Package

- Location: `<RELEASE_PATH>\2026-09-04_114744_Release`
- Hash report: `<RELEASE_PATH>\2026-09-04_114744_SHA256.txt`
- Contents: 52 runtime files required by the published API, dependencies, `web.config`, and externalized `appsettings.json`.
- Excluded: source, tests, `appsettings.Development.json`, PDB files, private keys, DPAPI files, and unrelated documentation.
- Manifest: `release-manifest.txt`.

## Creation and validation

The package was created with:

```powershell
dotnet publish .\Source\LabAuthServer\src\LabAuthServer.Api\LabAuthServer.Api.csproj -c Release -o .\Build\Release --no-restore --nologo
```

Restore, Release build, and full tests passed before publishing: 163 passed, 0 failed, 0 skipped. A second clean publish matched all 52 runtime-file hashes. Package text and filename scans found no credentials, tokens, private keys, DPAPI files, development settings, source, or test artifacts.

The package was not copied to `Current` during package construction and no IIS settings were changed during Phase 15.

## Prerequisites and rollback

The package requires the documented .NET 10 ASP.NET Core Hosting Bundle, IIS site/app pool, HTTPS certificate binding, external configuration, DPAPI file access, certificate-store access, LDAPS access, and SQL Windows Authentication. Rollback uses the Phase 16 backup `Releases\2026-09-04_114832_Current_Backup` and preserves server-specific configuration.

## Gate

**PASS.** The package is reproducible, validated, secret-free, and suitable for the documented IIS deployment.
