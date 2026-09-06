USE [master];
GO

IF DB_ID(N'LabAuthServer') IS NULL
BEGIN
    CREATE DATABASE [LabAuthServer];
END;
GO

IF DB_ID(N'LabAuthServer') IS NULL
BEGIN
    THROW 51000, 'LabAuthServer database was not created.', 1;
END;
GO
