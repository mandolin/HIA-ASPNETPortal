/*
    P2.4 运营审计表迁移。
    P2.4 operations-audit table migration.

    此脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.
*/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalCfg_OperationAudits]
    (
        [AuditId] BIGINT IDENTITY(1, 1) NOT NULL,
        [OccurredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_OperationAudits_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
        [Category] NVARCHAR(80) NOT NULL,
        [Action] NVARCHAR(80) NOT NULL,
        [Outcome] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_PortalCfg_OperationAudits_Outcome] DEFAULT (N'Success'),
        [ActorUserName] NVARCHAR(100) NOT NULL,
        [TargetType] NVARCHAR(80) NOT NULL,
        [TargetId] NVARCHAR(200) NOT NULL,
        [Summary] NVARCHAR(500) NOT NULL,
        [RelatedEventId] NVARCHAR(64) NULL,
        [ClientIp] NVARCHAR(64) NULL,
        [UserAgent] NVARCHAR(400) NULL,
        [CorrelationId] NVARCHAR(64) NULL,

        CONSTRAINT [PK_PortalCfg_OperationAudits]
            PRIMARY KEY CLUSTERED ([AuditId]),

        CONSTRAINT [CK_PortalCfg_OperationAudits_Outcome]
            CHECK ([Outcome] IN (N'Success', N'Failure', N'Skipped'))
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_OperationAudits_OccurredUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_OperationAudits_OccurredUtc]
    ON [dbo].[PortalCfg_OperationAudits] ([OccurredUtc] DESC, [AuditId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_OperationAudits_CategoryActionUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_OperationAudits_CategoryActionUtc]
    ON [dbo].[PortalCfg_OperationAudits] ([Category], [Action], [OccurredUtc] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_OperationAudits_TargetUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_OperationAudits_TargetUtc]
    ON [dbo].[PortalCfg_OperationAudits] ([TargetType], [TargetId], [OccurredUtc] DESC)
END
GO
