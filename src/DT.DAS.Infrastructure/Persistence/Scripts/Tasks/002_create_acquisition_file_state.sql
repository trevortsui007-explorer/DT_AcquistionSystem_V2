IF OBJECT_ID('[dbo].[DA_AcquisitionFileState]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DA_AcquisitionFileState]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ConfigId] INT NOT NULL,
        [BusinessDate] DATE NOT NULL,
        [FileName] NVARCHAR(500) NOT NULL,
        [FullPath] NVARCHAR(1000) NULL,
        [DataRowCount] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionFileState_DataRowCount] DEFAULT (0),
        [LastStartRow] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionFileState_LastStartRow] DEFAULT (0),
        [LastProcessedRows] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionFileState_LastProcessedRows] DEFAULT (0),
        [LastTaskLogId] NVARCHAR(50) NULL,
        [LastStatus] NVARCHAR(50) NULL,
        [LastUpdateSource] VARCHAR(30) NULL,
        [IsSealed] BIT NOT NULL CONSTRAINT [DF_DA_AcquisitionFileState_IsSealed] DEFAULT (0),
        [SealTime] DATETIME2 NULL,
        [LastScanTime] DATETIME2 NULL,
        [CreateTime] DATETIME2 NOT NULL CONSTRAINT [DF_DA_AcquisitionFileState_CreateTime] DEFAULT (SYSUTCDATETIME()),
        [UpdateTime] DATETIME2 NOT NULL CONSTRAINT [DF_DA_AcquisitionFileState_UpdateTime] DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX [UX_DA_AcquisitionFileState_Config_Date_File]
        ON [dbo].[DA_AcquisitionFileState]([ConfigId], [BusinessDate], [FileName]);
END;
