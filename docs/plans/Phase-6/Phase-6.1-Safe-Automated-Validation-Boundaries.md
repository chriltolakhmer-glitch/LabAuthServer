# Phase 6.1 — Safe Automated Validation Boundaries

Implementation checkpoint: IMPLEMENTED AND LOCALLY VALIDATED. Exact-SHA hosted validation is separately required for the delivering commit. Authorized 2026-09-12 against clean `main` at `0b6d1ee92220090fb5570323e30f10bfc2dd9ed5`. [Phase 6 plan](Phase-6-Plan.md) | [Testing commands](../../Testing.md).

## Objective

**Normal automated validation must not implicitly contact or mutate operational SQL Server, Active Directory, protected host credentials, or other environment-specific infrastructure.**

Explicit infrastructure tests must be identifiable, opted into, bound to an authorized/disposable target and unable to substitute application defaults. Mandatory hosted SQL coverage remains a real persistence check.

## Implementation

- SQL persistence cases carry `Category=SqlInfrastructure` and a small xUnit Fact attribute. Without `LABAUTHSERVER_RUN_SQL_TESTS=1`, an unfiltered run visibly skips them. With enablement, `LABAUTHSERVER_SQL_AUDIT_TEST_CONNECTION` is mandatory; missing/empty input fails before constructing a connection. The exact supplied connection is used; there is no localhost fallback. Target presence alone does not enable SQL tests.
- Both stored-procedure persistence tests remain mandatory. Concurrent writes now verify each returned row's correlation ID, in addition to unique positive IDs.
- Invalid-event validation uses options that throw if read, proving rejection before connection configuration access. The deterministic unavailable-connection test uses an empty SqlClient connection string: opening fails locally without a DNS/socket attempt and the unchanged production writer returns null. This verifies the local persistence-failure path, not a live network outage.
- `InfrastructureSafeApiFactory` underlies health, protected and JWT-size API fixtures. It replaces SQL audit persistence with `RecordingAuditEventService`, rejects host credential/LDAP-connection/certificate-key access and clears environment-provided licensing file/key inputs in test options. Existing test scenarios can explicitly supply synthetic keys and fake LDAP seams. Production registrations are untouched.
- The recording audit service applies the real `AuditEventValidator`, records valid events in a thread-safe queue and retains validation failures independently because production handlers can swallow audit exceptions. It never persists to SQL. Existing scenario-specific failure and concurrency doubles remain in place.
- Root DSE default tests assert deterministic safe failure and cancellation. One `Category=LdapAcceptance` case requires `LABAUTHSERVER_RUN_LDAP_ACCEPTANCE=1` and every explicit `LABAUTHSERVER_LDAP_TEST_*` value. Only that opted-in test constructs the real DPAPI/LDAPS acceptance path; it requires successful connectivity and attributes rather than passing on dependency failure.
- Existing cooperative LDAP cancellation, concurrency, deadline and classification tests are retained.
- CI executes deterministic tests before LocalDB provisioning. It then provisions the existing disposable hosted database from the unchanged Phase11 scripts and explicitly executes SQL tests with enablement and the supplied connection. TRX counters must show both SQL tests executed and passed; absent/skipped tests or test failure fail the job.

## Categories and execution

| Category | Ordinary unfiltered run, flags absent | Explicit execution | Hosted CI |
| --- | --- | --- | --- |
| Default (neither infrastructure trait) | Runs without operational SQL/AD/credentials | Default command in Testing | Mandatory before SQL provisioning |
| SqlInfrastructure | Visible skip | Enable flag plus required connection; missing target fails | Both persistence tests mandatory after disposable provisioning |
| LdapAcceptance | Visible skip | Enable flag plus explicit host/domain/base DN/username/DPAPI path; unsuccessful connectivity fails | Excluded; no real directory target is provisioned |

Filters alone never grant infrastructure permission. Setting an enable flag is an explicit opt-in and must only be done for the authorized target. Test helpers never infer that an arbitrary supplied connection is disposable: the operator must verify it. Tests never create, truncate or clean operational databases.

## Acceptance and validation evidence

Local results: restore and Release build passed with zero warnings/errors under SDK 10.0.401. The default command passed 1,038 tests (783 unit-project + 255 integration-project), zero failures/skips, three infrastructure exclusions, with SQL connection and enable flags absent. The category-only disabled run visibly skipped two SQL and one LDAP case. Explicit enablement with targets absent produced the expected two SQL failures and one LDAP failure before connection/credential access; these negative guard checks are not unresolved defects.

The two real SQL cases passed against a newly created LocalDB instance and database both named `LabAuthServer_Phase61_20260912`, isolated from the existing `MSSQLLocalDB` and operational application database. The checked-in Phase11 scripts were streamed with only their database identifier substituted in memory; the repository schema/scripts were unchanged. Both positive IDs/persisted fields and four concurrent row-to-correlation mappings passed. A read-back confirmed the test database name and five synthetic rows. The disposable instance was stopped afterward and retained for inspection; no operational database was targeted or cleaned. The same two-executed/two-passed TRX assertion used by CI passed locally. Report: `%TEMP%\LabAuthServer-Phase61-SqlTests\sql-infrastructure.trx`.

Inventory: 785 unit-project + 256 integration-project = 1,041 cases, versus the 1,023 baseline. Eighteen new isolation regression cases were added; existing SQL coverage is explicitly classified, three Root DSE tests are deterministic and one successful real-directory acceptance case is separately classified. Mandatory hosted-equivalent total: 1,040 (default + SQL); one LDAP acceptance case is not run. Real AD acceptance was not performed. Hosted results must be tied to the eventual commit SHA; baseline CI #39 is not evidence for these changes.

Regression coverage demonstrates absent-target failure, explicit target preservation, target-without-enable rejection, in-memory API auditing with real validation, blocked host resource access, ignored environment license input and required LDAP target values. CI's actual mandatory SQL execution provides the orchestration proof; no source-string tests substitute for it.

## Explicit non-goals and delivery

No production code, schema/procedure, authentication/authorization/audit/licensing semantics or deployment changes. No fallback policy, ProtectedController identity fix, licensing provider/expiry/catalog fix, issuer Release correction, outbox/retention/monitoring implementation or Phase 5.6 change. Those are later separately approved workstreams.

The authorized delivery is one SSH-signed commit `Implement Phase 6.1 safe test isolation`, using existing ALOT identity/signing configuration, normal push, GitHub commit verification and exact-SHA hosted CI observation. No release authorization is created. Stop after Phase 6.1.
