USE [LabAuthServer];
GO

IF SCHEMA_ID(N'Audit') IS NULL EXEC(N'CREATE SCHEMA [Audit] AUTHORIZATION [dbo]');
IF SCHEMA_ID(N'Reference') IS NULL EXEC(N'CREATE SCHEMA [Reference] AUTHORIZATION [dbo]');
IF SCHEMA_ID(N'Application') IS NULL EXEC(N'CREATE SCHEMA [Application] AUTHORIZATION [dbo]');
GO

IF OBJECT_ID(N'[Reference].[EventTypes]', N'U') IS NULL
BEGIN
    CREATE TABLE [Reference].[EventTypes]
    (
        [EventTypeId] int IDENTITY(1, 1) NOT NULL,
        [EventTypeCode] varchar(64) NOT NULL,
        [Category] varchar(32) NOT NULL,
        [Severity] varchar(16) NOT NULL,
        [Description] nvarchar(256) NOT NULL,
        [IsEnabled] bit NOT NULL CONSTRAINT [DF_EventTypes_IsEnabled] DEFAULT (1),
        [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_EventTypes_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [RetiredAtUtc] datetime2(3) NULL,
        CONSTRAINT [PK_EventTypes] PRIMARY KEY CLUSTERED ([EventTypeId]),
        CONSTRAINT [UQ_EventTypes_EventTypeCode] UNIQUE ([EventTypeCode]),
        CONSTRAINT [CK_EventTypes_Category] CHECK ([Category] IN ('Authentication', 'Authorization', 'Security', 'Application')),
        CONSTRAINT [CK_EventTypes_Severity] CHECK ([Severity] IN ('Information', 'Warning', 'Error', 'Critical')),
        CONSTRAINT [CK_EventTypes_RetiredEnabled] CHECK ([IsEnabled] = 1 OR [RetiredAtUtc] IS NOT NULL)
    );
END;
GO

IF OBJECT_ID(N'[Audit].[AuditEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [Audit].[AuditEvents]
    (
        [AuditEventId] bigint IDENTITY(1, 1) NOT NULL,
        [EventTimeUtc] datetime2(3) NOT NULL,
        [EventTypeId] int NOT NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        [RequestId] varchar(128) NULL,
        [Username] nvarchar(256) NULL,
        [Subject] nvarchar(256) NULL,
        [Role] nvarchar(64) NULL,
        [Endpoint] nvarchar(256) NULL,
        [HttpMethod] varchar(16) NULL,
        [StatusCode] smallint NULL,
        [Success] bit NULL,
        [ClientIp] varchar(45) NULL,
        [ServerName] nvarchar(128) NULL,
        [ApplicationVersion] varchar(64) NULL,
        [DetailsJson] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_AuditEvents_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NOT NULL CONSTRAINT [DF_AuditEvents_CreatedBy] DEFAULT (N'LabAuthServer.Api'),
        CONSTRAINT [PK_AuditEvents] PRIMARY KEY CLUSTERED ([AuditEventId]),
        CONSTRAINT [FK_AuditEvents_EventTypes] FOREIGN KEY ([EventTypeId]) REFERENCES [Reference].[EventTypes] ([EventTypeId]),
        CONSTRAINT [CK_AuditEvents_CorrelationId] CHECK ([CorrelationId] <> '00000000-0000-0000-0000-000000000000'),
        CONSTRAINT [CK_AuditEvents_StatusCode] CHECK ([StatusCode] IS NULL OR [StatusCode] BETWEEN 100 AND 599),
        CONSTRAINT [CK_AuditEvents_Role] CHECK ([Role] IS NULL OR [Role] IN (N'Administrator', N'Operator', N'Reader')),
        CONSTRAINT [CK_AuditEvents_HttpMethod] CHECK ([HttpMethod] IS NULL OR [HttpMethod] IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS', 'TRACE')),
        CONSTRAINT [CK_AuditEvents_DetailsJson] CHECK ([DetailsJson] IS NULL OR (ISJSON([DetailsJson]) = 1 AND DATALENGTH([DetailsJson]) <= 32768)),
        CONSTRAINT [CK_AuditEvents_RequestIdLength] CHECK ([RequestId] IS NULL OR DATALENGTH([RequestId]) <= 256),
        CONSTRAINT [CK_AuditEvents_CreatedByLength] CHECK (DATALENGTH([CreatedBy]) <= 256)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [Reference].[EventTypes])
BEGIN
    INSERT INTO [Reference].[EventTypes]
        ([EventTypeCode], [Category], [Severity], [Description])
    VALUES
        ('AUTH_LOGIN_SUCCESS', 'Authentication', 'Information', N'Successful user authentication and token issuance boundary'),
        ('AUTH_LOGIN_FAILURE', 'Authentication', 'Warning', N'User login request or credential failure'),
        ('AUTH_LDAP_FAILURE', 'Authentication', 'Error', N'Directory authentication service failure'),
        ('AUTHZ_ACCESS_GRANTED', 'Authorization', 'Information', N'Protected endpoint authorization granted'),
        ('AUTHZ_ACCESS_DENIED', 'Authorization', 'Warning', N'Protected endpoint authorization denied'),
        ('SEC_INVALID_TOKEN', 'Security', 'Warning', N'Bearer token authentication failed'),
        ('SEC_EXPIRED_TOKEN', 'Security', 'Information', N'Bearer token was expired'),
        ('SEC_INVALID_SIGNATURE', 'Security', 'Warning', N'Bearer token signature validation failed'),
        ('SEC_INVALID_ISSUER', 'Security', 'Warning', N'Bearer token issuer validation failed'),
        ('SEC_INVALID_AUDIENCE', 'Security', 'Warning', N'Bearer token audience validation failed'),
        ('SEC_INVALID_ROLE', 'Security', 'Warning', N'Bearer token role validation failed'),
        ('APP_UNHANDLED_EXCEPTION', 'Application', 'Error', N'Unhandled application exception was safely mapped'),
        ('APP_VALIDATION_ERROR', 'Application', 'Information', N'Request validation failed');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Audit].[AuditEvents]') AND name = N'IX_AuditEvents_EventTimeUtc')
    CREATE INDEX [IX_AuditEvents_EventTimeUtc] ON [Audit].[AuditEvents] ([EventTimeUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Audit].[AuditEvents]') AND name = N'IX_AuditEvents_CorrelationId')
    CREATE INDEX [IX_AuditEvents_CorrelationId] ON [Audit].[AuditEvents] ([CorrelationId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Audit].[AuditEvents]') AND name = N'IX_AuditEvents_EventTypeId_EventTimeUtc')
    CREATE INDEX [IX_AuditEvents_EventTypeId_EventTimeUtc] ON [Audit].[AuditEvents] ([EventTypeId], [EventTimeUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Audit].[AuditEvents]') AND name = N'IX_AuditEvents_Username_EventTimeUtc')
    CREATE INDEX [IX_AuditEvents_Username_EventTimeUtc] ON [Audit].[AuditEvents] ([Username], [EventTimeUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Audit].[AuditEvents]') AND name = N'IX_AuditEvents_StatusCode_EventTimeUtc')
    CREATE INDEX [IX_AuditEvents_StatusCode_EventTimeUtc] ON [Audit].[AuditEvents] ([StatusCode], [EventTimeUtc]);
GO
