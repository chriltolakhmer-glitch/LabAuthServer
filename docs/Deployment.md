# Deployment

## Deployment model

The API includes a file-system publish profile and is designed for Windows/IIS hosting with the .NET 10 ASP.NET Core Hosting Bundle. Deployment requires an approved HTTPS binding, external configuration, directory connectivity, certificate-store access, DPAPI access, and SQL permissions.

Use placeholders in environment-specific runbooks:

- `<IIS_SITE>`
- `<IIS_APP_POOL>`
- `<DEPLOYMENT_PATH>`
- `<HOST>`
- `<LDAP_HOST>`
- `<DB_SERVER>`
- `<SECRET_FILE>`
- `<THUMBPRINT>`

## Release flow

1. Restore and build the solution in Release mode.
2. Run the unit and integration suites.
3. Publish the API to a clean release directory.
4. Validate the package contents and hashes.
5. Stage the package and preserve the current deployment for rollback.
6. Replace the application only after staging validation passes.
7. Start the approved app pool and verify health and anonymous protected-resource behavior.

The safe deployment procedure is an operational workflow, not a source-code feature. Use the separately controlled deployment script and do not copy secret-bearing files into a release package.

## Current validation boundary

The repository proves that the publish profile and deployment procedure exist. It does not independently prove that a current target IIS deployment, real AD identity, certificate private key, or DPAPI secret is available. See [Validation Status](Validation_Status.md).

Historical IIS and release evidence is preserved under `docs/archive/deployment-evidence/` and is not current deployment proof.
