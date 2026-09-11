# Phase 4.17 - Production License Governance and Final Sign-Off

Status: IMPLEMENTED. This record is the implementation and final-sign-off evidence for Phase 4.17. The original Phase 4.17 plan remains historical planning evidence.

## 1. Scope and decisions

The approved Phase 4.17 decisions are implemented as follows:

- O-01: Community permits `auth.basic` and `auth.jwt`; Professional permits those plus `auth.ldap` and `audit.logging`; Enterprise permits all five catalogued features, including `admin.console`.
- O-07: Production signing private keys are offline vendor-controlled secrets. They are never stored in this repository, Git, CI, appsettings, publish output, customer servers, tests, or fixtures. Servers receive trusted public keys only. Test keys are separate and ephemeral.
- O-17: Licenses are issued by an authorized offline vendor licensing operator. LabAuthServer is not an issuer and does not provide online activation or an issuance service.
- O-18: The vendor maintains an external controlled license register. It contains no production private keys.
- O-20/O-21: A customer license remains an external file at the administrator-controlled `Licensing:LicenseFilePath` configuration boundary. It is not embedded in source, Git, binaries, static web content, or publish output.

The feature matrix is an upper bound, not an entitlement grant. A feature is enabled only when it is known, permitted for the license edition, and explicitly present in the signed license. Missing or invalid license data remains restricted Community behavior.

## 2. Offline issuance workflow

Only an authorized vendor licensing operator may issue a production license:

1. Select the customer and product data approved for issuance.
2. Select an approved edition.
3. Select the explicit features permitted by that edition.
4. Apply approved numeric limits and validity dates.
5. Select the signing key and `keyId` from the vendor-controlled offline process.
6. Sign the license offline using the approved signing profile.
7. Independently validate the signed license, including signature, product, edition, features, limits, and dates.
8. Record the issuance in the external controlled license register.
9. Securely deliver the license file to the customer through the approved out-of-band channel.

LabAuthServer only validates and consumes a delivered license. It does not issue licenses, host an issuance database, contact a licensing server, activate licenses online, or provide revocation endpoints.

## 3. Production key custody and rotation

The production private signing key is an offline vendor-controlled secret. Physical storage, backup technology, access control, dual-control procedures, and recovery rehearsal are operational responsibilities outside application code and are intentionally not assigned a fictional location or product here.

The customer/server boundary contains trusted public keys only. Production private-key material must never be committed, placed in CI variables or GitHub Actions, included in application settings or published output, copied to customer/server machines, or used by tests. Automated tests use separate ephemeral test keys.

For rotation, add the new public key and `keyId` to the trusted public-key configuration before issuing licenses with it. Retain the superseded public key until all licenses signed by it have expired or been replaced, then retire it according to the vendor procedure. A suspected compromise requires rotation and re-issuance; no online revocation mechanism is implied.

## 4. External license register

The register is an external controlled operational record owned by the vendor licensing/operations owner. It is not an application database and is not stored in this repository.

Minimum fields:

- License ID
- Customer
- Product
- Edition
- Features
- Limits
- Issued At
- Expires At
- Key ID
- Status
- Issued By
- Notes

The License ID in the register must match the signed license document's `licenseId`. The `keyId` must match the signing envelope and is tracked to support rotation and incident response. Status should cover at least Issued, Delivered, Active, Expired, Replaced, and any approved future revocation-policy state. The register must not contain production private keys, private key backups, credentials, or customer secrets beyond the controlled license metadata required for operations.

## 5. Delivery, installation, replacement, and rollback

The administrator supplies the external file path through `Licensing:LicenseFilePath`. No customer-specific path is hardcoded.

Initial installation:

1. Receive the license through the approved secure delivery channel.
2. Validate it independently before activation.
3. Place it in the administrator-controlled staging location with least-privilege file permissions.
4. Atomically move or replace the configured license file.
5. Restart or reload the application according to the supported host procedure.
6. Revalidate the effective edition, explicit features, limits, and expiry state.

Replacement uses the same validate-before-activate sequence. Retain the previous file as a rollback copy, stage the new file on the same volume where practical, atomically replace the configured file, restart/reload the application, and perform post-replacement validation. Do not edit a signed license in place.

If validation fails, restore the previous known-good file atomically, restart/reload, and revalidate. If no valid prior file is available, the application remains in restricted Community mode. Hot reload and per-request license revalidation are not implemented or implied by this procedure.

## 6. Runtime and security verification

Phase 4.16 runtime protections remain intact:

- Startup-only loading through the bounded license file reader and a single policy provider state.
- Strict parsing, trusted public-key signature verification, expiration handling, feature validation, and limits validation.
- Bounded license file reads.
- Fail-closed restricted Community behavior for missing, unreadable, oversized, malformed, untrusted, expired, or otherwise invalid licenses.
- Metadata-only licensing logs; license content, payloads, signatures, private keys, credentials, and secrets are not logged.
- No network calls, online activation, revocation endpoint, machine binding, hot reload, or per-request revalidation.
- Licensing does not weaken authentication, authorization, JWT, LDAP, SQL TLS, rate limits, or other security controls.

The Phase 4.17 source change is limited to the authoritative edition matrix and its policy tests. No production key, customer license, secret, network behavior, or dependency was added.

## 7. Deferred items

The following remain explicitly deferred or out of scope:

- Machine binding and rebinding tooling.
- Online activation and online revocation.
- Hot reload and per-request license revalidation.
- An issuance server or online licensing service.
- An application license database or embedded customer license.
- Fuzz testing, property-based testing, and load testing.
- Any production private-key generation in this repository.
- Physical production key storage and backup technology selection.

## 8. Validation evidence

- `dotnet restore`: PASS.
- `dotnet build --configuration Release --no-restore`: PASS; 0 warnings and 0 errors.
- Focused `Phase46FeatureAndEditionEnforcementTests`: PASS; 63 passed, 0 failed, 0 skipped.
- `dotnet test --configuration Release --no-build`: PASS; 777 unit tests and 246 integration tests, 1,023 passed, 0 failed, 0 skipped.
- `git diff --check`: PASS.

## 9. Sign-off

Phase 4.17 is ready for controlled production governance after the required validation commands passed and the final implementation commit is reviewed. This record does not authorize production issuance, deployment, or push operations by itself.
