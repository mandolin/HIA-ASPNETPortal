/*
<lang>
  <zh-CN>P6.3 门户账号与员工绑定基础表迁移。本脚本可重复执行且不会由应用启动流程自动执行；第一版只允许一个门户账号和一个员工之间存在一条当前有效绑定，绑定变化会影响员工工号登录，后续服务层必须同步写运营审计并递增目标用户安全版本。</zh-CN>
  <en>P6.3 Portal-user to employee binding foundation migration. This script is idempotent and is not executed automatically by application startup; the first version allows only one currently active binding between a Portal account and an employee, and binding changes affect employee-code sign-in, so later service code must record operations audit and increment the target user's security version.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保结束时间、原因和过滤唯一索引的空值处理与绑定规则一致。</zh-CN>
--   <en>Enable standard NULL comparison semantics so ended timestamps, reasons, and filtered unique indexes handle nulls consistently with binding rules.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保持绑定表、外键、过滤索引和状态约束名称稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so binding table, foreign key, filtered index, and status constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>绑定表必须依赖旧门户账号主体；缺失时 fail fast，避免无法审计绑定归属。</zh-CN>
--   <en>The binding table must depend on the legacy Portal account authority; fail fast when it is missing to avoid unauditable binding ownership.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_UserEmployeeBindings.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>绑定表同时依赖员工主数据；缺失员工表时中止，避免创建不能落地到企业身份目录的绑定行。</zh-CN>
--   <en>The binding table also depends on employee master data; stop when the employee table is missing to avoid binding rows that cannot land in the enterprise identity directory.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_Employees must be created before PortalBiz_UserEmployeeBindings.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护保留历史绑定轨迹和当前有效绑定，支持脚本在修复部署中重复运行。</zh-CN>
--   <en>The create-table guard preserves historical binding traces and current active bindings, supporting repeated execution during deployment repair.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>绑定事实表记录账号与员工的当前/历史关系；它不保存密码，也不直接更改认证票据。</zh-CN>
    --   <en>The binding fact table records current and historical relationships between accounts and employees; it stores no passwords and does not directly change authentication tickets.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_UserEmployeeBindings]
    (
        [BindingId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [EmployeeId] INT NOT NULL,
        [BindingStatus] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserEmployeeBindings_BindingStatus] DEFAULT (N'Active'),
        [BoundUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserEmployeeBindings_BoundUtc] DEFAULT (SYSUTCDATETIME()),
        [BoundBy] NVARCHAR(100) NULL,
        [EndedUtc] DATETIME2(0) NULL,
        [EndedBy] NVARCHAR(100) NULL,
        [Reason] NVARCHAR(200) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserEmployeeBindings_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>主键为绑定事件技术标识；两个外键分别锚定门户账号主体和员工目录主体。</zh-CN>
        --   <en>The primary key is the technical id of the binding event, while the two foreign keys anchor the Portal account authority and employee directory authority.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_UserEmployeeBindings]
            PRIMARY KEY CLUSTERED ([BindingId]),
        CONSTRAINT [FK_PortalBiz_UserEmployeeBindings_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [FK_PortalBiz_UserEmployeeBindings_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[PortalBiz_Employees] ([EmployeeId]),
        -- <lang>
        --   <zh-CN>状态与结束时间约束表达“Active/Pending/Disabled/Ended”的生命周期，结束状态必须带结束时间。</zh-CN>
        --   <en>Status and ended-time constraints express the Active/Pending/Disabled/Ended lifecycle, and an ended binding must carry an ended timestamp.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_UserEmployeeBindings_Status]
            CHECK ([BindingStatus] IN (N'Active', N'Pending', N'Disabled', N'Ended')),
        CONSTRAINT [CK_PortalBiz_UserEmployeeBindings_EndedUtc]
            CHECK ([BindingStatus] <> N'Ended' OR [EndedUtc] IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_UserEmployeeBindings_Reason]
            CHECK ([Reason] IS NULL OR ([Reason] = LTRIM(RTRIM([Reason])) AND NULLIF([Reason], N'') IS NOT NULL))
    )
END
GO

-- <lang>
--   <zh-CN>Active 用户过滤唯一索引确保同一个门户账号同时最多绑定一个当前员工，保护员工工号登录唯一性。</zh-CN>
--   <en>The active-user filtered unique index ensures one Portal account can bind to at most one current employee, protecting employee-code sign-in uniqueness.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_UserEmployeeBindings_ActiveUser' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_UserEmployeeBindings_ActiveUser]
    ON [dbo].[PortalBiz_UserEmployeeBindings] ([UserId])
    WHERE [BindingStatus] = N'Active'
END
GO

-- <lang>
--   <zh-CN>Active 员工过滤唯一索引确保一个员工不会同时被多个门户账号声明为当前有效身份。</zh-CN>
--   <en>The active-employee filtered unique index ensures one employee is not simultaneously claimed as the current identity by multiple Portal accounts.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_UserEmployeeBindings_ActiveEmployee' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_UserEmployeeBindings_ActiveEmployee]
    ON [dbo].[PortalBiz_UserEmployeeBindings] ([EmployeeId])
    WHERE [BindingStatus] = N'Active'
END
GO

-- <lang>
--   <zh-CN>状态索引用于管理员绑定队列和诊断查询，按状态再回到员工/用户两端。</zh-CN>
--   <en>The status index supports administrator binding queues and diagnostic queries, grouping by status before returning to employee and user endpoints.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_UserEmployeeBindings_Status' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_UserEmployeeBindings_Status]
    ON [dbo].[PortalBiz_UserEmployeeBindings] ([BindingStatus], [EmployeeId], [UserId])
END
GO
