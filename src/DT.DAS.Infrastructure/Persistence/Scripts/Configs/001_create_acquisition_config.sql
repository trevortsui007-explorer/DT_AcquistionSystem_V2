IF OBJECT_ID('[dbo].[DA_AcquisitionConfig]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DA_AcquisitionConfig]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EqName] NVARCHAR(100) NULL,
        [TableName] NVARCHAR(128) NULL,
        [FilePathPattern] NVARCHAR(1000) NULL,
        [FileNamePattern] NVARCHAR(500) NULL,
        [FileType] NVARCHAR(20) NULL,
        [HeaderRow] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionConfig_HeaderRow] DEFAULT (1),
        [StartRow] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionConfig_StartRow] DEFAULT (2),
        [FieldMappings] NVARCHAR(MAX) NULL,
        [ExtFields] NVARCHAR(500) NULL,
        [IsEnabled] BIT NOT NULL CONSTRAINT [DF_DA_AcquisitionConfig_IsEnabled] DEFAULT (1),
        [PostProcessingType] INT NOT NULL CONSTRAINT [DF_DA_AcquisitionConfig_PostProcessingType] DEFAULT (0),
        [PostTableName] NVARCHAR(128) NULL,
        [ProcedureName] NVARCHAR(128) NULL,
        [ServiceName] NVARCHAR(128) NULL,
        [Flag] NVARCHAR(100) NULL,
        [FlagName] NVARCHAR(100) NULL,
        [CreateTime] DATETIME2 NOT NULL CONSTRAINT [DF_DA_AcquisitionConfig_CreateTime] DEFAULT (SYSUTCDATETIME())
    );
END;
