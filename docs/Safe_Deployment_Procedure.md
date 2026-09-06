# Safe Deployment Procedure

This document describes the repository’s intended staged deployment workflow. It is not proof that a current target environment is deployed or healthy.

## Inputs

The procedure requires an immutable, approved release directory at `<RELEASE_PATH>` and deployment-specific values for `<DEPLOYMENT_PATH>`, `<IIS_APP_POOL>`, `<HOST>`, `<SECRET_FILE>`, `<THUMBPRINT>`, and `<CONNECTION_STRING>`.

The deployment script is maintained in the separately controlled operational environment. It must receive an approved release directory and must not be used to build a release.

## Safety sequence

1. Copy the release into a unique staging directory.
2. Validate expected file count, hashes, configuration safety, and absence of development settings, PDBs, secret files, and private-key artifacts.
3. Create a complete timestamped backup of the current deployment.
4. Stop only the approved application pool.
5. Replace the deployment directory from the validated staging directory.
6. Validate deployment parity against the release.
7. Restore the backup and leave the pool stopped if replacement or validation fails.
8. Start the application pool only after validation passes.
9. Verify the site state, HTTPS health response, correlation header, and anonymous protected-resource `401` behavior.
10. Remove staging content only after successful completion.

## Operational requirements

- Run deployment through authorized change control.
- Preserve server-specific configuration outside the release package.
- Do not copy passwords, DPAPI files, private keys, tokens, PDBs, or development configuration into the release.
- Do not change AD, LDAP, certificate, database, firewall, or IIS bindings as an incidental deployment action.
- Keep rollback backups until release verification is complete.

## Validation boundary

The repository contains the publish profile and the documented workflow. Current target deployment status, valid AD identity behavior, certificate access, DPAPI decryption, and SQL permission behavior require protected environment validation. See [Validation Status](Validation_Status.md).
