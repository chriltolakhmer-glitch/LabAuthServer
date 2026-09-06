# Database

## Purpose

The database integration persists security and operational audit events. The application does not use the database as a user store, token store, refresh-token store, or authorization cache.

## Current objects

The scripts under `database/Phase11/` define the following objects:

- `Reference.EventTypes`: controlled event catalog with 13 event definitions.
- `Audit.AuditEvents`: append-oriented audit records containing event, correlation, request, identity, role, endpoint, status, outcome, host/version, and bounded details fields.
- `Audit.usp_WriteAuditEvent`: validates event data, rejects prohibited sensitive JSON properties, resolves enabled event types, and inserts one audit record transactionally.
- `LabAuthServer_AuditWriter`: database role granted execute permission on the writer procedure for the configured application identity.

The scripts also define indexes for event time, correlation ID, event type/time, username/time, and status/time.

## Application access

`SqlAuditEventService` uses `Microsoft.Data.SqlClient`, typed parameters, the configured connection string, and a bounded command timeout. It calls only `Audit.usp_WriteAuditEvent`; application code does not issue direct audit-table writes or construct dynamic SQL.

Use a deployment-specific connection value such as `<CONNECTION_STRING>` and a deployment-approved database name such as `<DB_NAME>`. Do not publish server names, credentials, or application identity details in public documentation.

## Permissions

The intended least-privilege boundary is procedure execution through the `LabAuthServer_AuditWriter` role. The application identity should not receive direct table write or broad database administration permissions. Apply and review `04_ConfigureAuditPermissions.sql` through authorized database change control and adapt the identity to the target environment.

## Setup

Apply the scripts in order with an authorized SQL Server identity:

1. `database/Phase11/01_CreateDatabase.sql`
2. `database/Phase11/02_CreateSchemasTables.sql`
3. `database/Phase11/03_CreateAuditProcedures.sql`
4. `database/Phase11/04_ConfigureAuditPermissions.sql`

Review database naming, transport encryption, application identity mapping, and permissions before deployment.

## Retention and maintenance

Retention policy ownership, archival, purge procedures, and SQL Agent scheduling are **NOT IMPLEMENTED** in the repository. No application role should receive purge or maintenance permissions by default.
