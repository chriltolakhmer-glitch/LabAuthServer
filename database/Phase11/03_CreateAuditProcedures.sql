USE [LabAuthServer];
GO

CREATE OR ALTER PROCEDURE [Audit].[usp_WriteAuditEvent]
    @EventTypeCode varchar(64),
    @EventTimeUtc datetime2(3) = NULL,
    @CorrelationId uniqueidentifier,
    @RequestId varchar(128) = NULL,
    @Username nvarchar(256) = NULL,
    @Subject nvarchar(256) = NULL,
    @Role nvarchar(64) = NULL,
    @Endpoint nvarchar(256) = NULL,
    @HttpMethod varchar(16) = NULL,
    @StatusCode smallint = NULL,
    @Success bit = NULL,
    @ClientIp varchar(45) = NULL,
    @ServerName nvarchar(128) = NULL,
    @ApplicationVersion varchar(64) = NULL,
    @DetailsJson nvarchar(max) = NULL,
    @CreatedBy nvarchar(128) = N'LabAuthServer.Api'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EventTypeId int;
    DECLARE @ContainsSensitiveJson bit = 0;
    DECLARE @ResolvedEventTimeUtc datetime2(3) = COALESCE(@EventTimeUtc, CONVERT(datetime2(3), SYSUTCDATETIME()));

    BEGIN TRY
        IF NULLIF(LTRIM(RTRIM(@EventTypeCode)), '') IS NULL
            THROW 51001, 'EventTypeCode is required.', 1;

        IF @CorrelationId IS NULL OR @CorrelationId = '00000000-0000-0000-0000-000000000000'
            THROW 51002, 'CorrelationId is required.', 1;

        IF @EventTimeUtc IS NOT NULL AND @EventTimeUtc > DATEADD(MINUTE, 5, CONVERT(datetime2(3), SYSUTCDATETIME()))
            THROW 51003, 'EventTimeUtc cannot be more than five minutes in the future.', 1;

        IF @StatusCode IS NOT NULL AND (@StatusCode < 100 OR @StatusCode > 599)
            THROW 51004, 'StatusCode must be between 100 and 599.', 1;

        IF @Role IS NOT NULL AND @Role NOT IN (N'Administrator', N'Operator', N'Reader')
            THROW 51005, 'Role is not approved.', 1;

        IF @HttpMethod IS NOT NULL AND @HttpMethod NOT IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS', 'TRACE')
            THROW 51006, 'HttpMethod is not approved.', 1;

        IF @DetailsJson IS NOT NULL AND (ISJSON(@DetailsJson) <> 1 OR DATALENGTH(@DetailsJson) > 32768)
            THROW 51007, 'DetailsJson must be valid JSON and no larger than 32768 bytes.', 1;

        IF @DetailsJson IS NOT NULL
        BEGIN
            ;WITH JsonNodes AS
            (
                SELECT CONVERT(nvarchar(max), [value]) AS JsonValue, [type] AS JsonType
                FROM OPENJSON(@DetailsJson)
                UNION ALL
                SELECT CONVERT(nvarchar(max), child.[value]), child.[type]
                FROM JsonNodes AS node
                CROSS APPLY OPENJSON(node.JsonValue) AS child
                WHERE node.JsonType IN (4, 5)
            )
            SELECT @ContainsSensitiveJson = 1
            FROM JsonNodes AS node
            CROSS APPLY OPENJSON(node.JsonValue) AS property
            WHERE node.JsonType = 5
              AND LOWER(property.[key]) IN
                  ('password', 'token', 'accesstoken', 'refreshtoken', 'authorization',
                   'headers', 'requestbody', 'secret', 'privatekey', 'certificate',
                   'dpapi', 'stacktrace', 'exception', 'ldapresponse', 'ldapfilter',
                   'connectionstring')
            OPTION (MAXRECURSION 100);

            IF @ContainsSensitiveJson = 1
                THROW 51011, 'DetailsJson contains a prohibited property.', 1;
        END

        IF @RequestId IS NOT NULL AND DATALENGTH(@RequestId) > 256
            THROW 51008, 'RequestId is too long.', 1;

        IF @CreatedBy IS NULL OR NULLIF(LTRIM(RTRIM(@CreatedBy)), '') IS NULL
            THROW 51009, 'CreatedBy is required.', 1;

        BEGIN TRANSACTION;

        SELECT @EventTypeId = [EventTypeId]
        FROM [Reference].[EventTypes]
        WHERE [EventTypeCode] = LTRIM(RTRIM(@EventTypeCode))
          AND [IsEnabled] = 1
          AND [RetiredAtUtc] IS NULL;

        IF @EventTypeId IS NULL
            THROW 51010, 'Event type is unknown, disabled, or retired.', 1;

        INSERT INTO [Audit].[AuditEvents]
        (
            [EventTimeUtc], [EventTypeId], [CorrelationId], [RequestId], [Username], [Subject],
            [Role], [Endpoint], [HttpMethod], [StatusCode], [Success], [ClientIp], [ServerName],
            [ApplicationVersion], [DetailsJson], [CreatedBy]
        )
        VALUES
        (
            @ResolvedEventTimeUtc, @EventTypeId, @CorrelationId, NULLIF(LTRIM(RTRIM(@RequestId)), ''),
            NULLIF(LTRIM(RTRIM(@Username)), ''), NULLIF(LTRIM(RTRIM(@Subject)), ''),
            NULLIF(LTRIM(RTRIM(@Role)), ''), NULLIF(LTRIM(RTRIM(@Endpoint)), ''),
            NULLIF(LTRIM(RTRIM(@HttpMethod)), ''), @StatusCode, @Success,
            NULLIF(LTRIM(RTRIM(@ClientIp)), ''), NULLIF(LTRIM(RTRIM(@ServerName)), ''),
            NULLIF(LTRIM(RTRIM(@ApplicationVersion)), ''), @DetailsJson,
            LTRIM(RTRIM(@CreatedBy))
        );

        DECLARE @AuditEventId bigint = CONVERT(bigint, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT
            @AuditEventId AS [AuditEventId],
            @ResolvedEventTimeUtc AS [EventTimeUtc],
            @CorrelationId AS [CorrelationId];
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
