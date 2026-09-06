USE [LabAuthServer];
GO

IF DATABASE_PRINCIPAL_ID(N'LabAuthServer_AuditWriter') IS NULL
    CREATE ROLE [LabAuthServer_AuditWriter] AUTHORIZATION [dbo];
GO

IF DATABASE_PRINCIPAL_ID(N'IIS APPPOOL\LabAuthServerAppPool') IS NULL
    CREATE USER [IIS APPPOOL\LabAuthServerAppPool] FOR LOGIN [IIS APPPOOL\LabAuthServerAppPool];
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members drm
    INNER JOIN sys.database_principals role_principal ON role_principal.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals member_principal ON member_principal.principal_id = drm.member_principal_id
    WHERE role_principal.name = N'LabAuthServer_AuditWriter'
      AND member_principal.name = N'IIS APPPOOL\LabAuthServerAppPool'
)
    ALTER ROLE [LabAuthServer_AuditWriter] ADD MEMBER [IIS APPPOOL\LabAuthServerAppPool];
GO

GRANT EXECUTE ON OBJECT::[Audit].[usp_WriteAuditEvent] TO [LabAuthServer_AuditWriter];
GO
