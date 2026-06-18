IF OBJECT_ID('[dbo].[DA_AcquisitionTaskLog]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DA_AcquisitionTaskLog]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TaskId] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionTaskLog_TaskId] DEFAULT (0),
        [TaskCode] VARCHAR(50) NULL,
        [TriggerType] VARCHAR(10) NULL,
        [StartTime] DATETIME2 NOT NULL,
        [EndTime] DATETIME2 NULL,
        [Status] NVARCHAR(50) NULL,
        [TotalConfigs] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionTaskLog_TotalConfigs] DEFAULT (0),
        [SuccessCount] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionTaskLog_SuccessCount] DEFAULT (0),
        [FailureCount] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionTaskLog_FailureCount] DEFAULT (0),
        [ProcessedCount] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionTaskLog_ProcessedCount] DEFAULT (0),
        [Progress] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionTaskLog_Progress] DEFAULT (0),
        [Message] NVARCHAR(500) NULL
    );
END;

IF OBJECT_ID('[dbo].[DA_AcquisitionLog]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DA_AcquisitionLog]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TaskLogId] NVARCHAR(50) NOT NULL,
        [ConfigId] INT NOT NULL,
        [FileName] NVARCHAR(500) NULL,
        [StartRow] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionLog_StartRow] DEFAULT (0),
        [ProcessedRows] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionLog_ProcessedRows] DEFAULT (0),
        [StartTime] DATETIME2 NULL,
        [EndTime] DATETIME2 NULL,
        [Status] NVARCHAR(50) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL
    );

    CREATE INDEX [IX_DA_AcquisitionLog_TaskLogId] ON [dbo].[DA_AcquisitionLog]([TaskLogId]);
    CREATE INDEX [IX_DA_AcquisitionLog_Config_File_Status] ON [dbo].[DA_AcquisitionLog]([ConfigId], [FileName], [Status]);
END;
