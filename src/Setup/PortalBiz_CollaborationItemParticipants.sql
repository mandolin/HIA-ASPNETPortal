/*
<lang>
  <zh-CN>P47.1 企业协同事项参与人迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；本表保存事项的参与人集合（协办/关注两类增量角色），发起人与负责人仍由主表单值字段承载，不在此重复保存。只保存低敏用户标识与角色，不保存密码、Cookie、Token、连接串、证件号、薪资或其它高敏个人资料。</zh-CN>
  <en>P47.1 enterprise collaboration-item participant migration. This script is idempotent and the application never runs it automatically at startup; the table stores the item's participant set (the two incremental roles Collaborator and Watcher), while the initiator and owner remain carried by the fact-table scalar fields and are not duplicated here. It stores only low-sensitivity user identifiers and roles, and stores no passwords, cookies, tokens, connection strings, government ids, compensation data, or other high-sensitivity personal data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证角色与创建人约束按 SQL Server 基线求值。</zh-CN>
--   <en>Enable standard NULL comparison semantics so role and creator constraints evaluate on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护参与人表 DDL 对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so participant-table DDL object and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>参与人表依赖协同事项主表；缺失主表时停止，避免创建无法归属到事项的参与人孤岛。</zh-CN>
--   <en>The participant table depends on the collaboration-item fact table; stop when it is missing to avoid a participant island that cannot belong to an item.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_CollaborationItems must exist before PortalBiz_CollaborationItemParticipants.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>参与人引用旧用户表；缺失用户表时停止，避免参与人引用失去身份边界。</zh-CN>
--   <en>Participants reference the legacy user table; stop when it is missing so participant references do not lose their identity boundary.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_CollaborationItemParticipants.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护确保重复执行不重建既有参与人集合。</zh-CN>
--   <en>The create-table guard ensures repeated execution does not rebuild existing participant sets.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemParticipants]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>参与人表是事项的集合扩展维度，不承载事项事实或审计日志职责。</zh-CN>
    --   <en>The participant table is the item's set-extension dimension and carries neither item facts nor audit-log duties.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_CollaborationItemParticipants]
    (
        [ParticipantId] BIGINT IDENTITY(1,1) NOT NULL,
        [ItemId] BIGINT NOT NULL,
        [UserId] INT NOT NULL,
        [ParticipantRoleKey] NVARCHAR(20) NOT NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_CollaborationItemParticipants_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>技术主键服务内部关系，事项与用户组合唯一保证同一用户在同一事项只有一种参与角色。</zh-CN>
        --   <en>The technical primary key serves internal relations, while the item-user uniqueness guarantees one participant role per user per item.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_CollaborationItemParticipants]
            PRIMARY KEY CLUSTERED ([ParticipantId]),
        CONSTRAINT [UX_PortalBiz_CollaborationItemParticipants_ItemUser]
            UNIQUE ([ItemId], [UserId]),
        -- <lang>
        --   <zh-CN>事项删除时级联清除其参与人；参与人不是权限真源，删除不改变授权。</zh-CN>
        --   <en>Deleting an item cascades to its participants; participants are not an authorization source, so deletion does not change authorization.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_CollaborationItemParticipants_Items]
            FOREIGN KEY ([ItemId]) REFERENCES [dbo].[PortalBiz_CollaborationItems] ([ItemId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PortalBiz_CollaborationItemParticipants_User]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        -- <lang>
        --   <zh-CN>参与角色白名单定义本期增量两值：协办与关注；发起人与负责人仍由主表单值字段承载。</zh-CN>
        --   <en>The participant-role whitelist defines this phase's two incremental values, Collaborator and Watcher; initiator and owner remain on the fact-table scalar fields.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItemParticipants_Role]
            CHECK ([ParticipantRoleKey] IN (N'Collaborator', N'Watcher')),
        CONSTRAINT [CK_PortalBiz_CollaborationItemParticipants_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL)
    )
END
GO

-- <lang>
--   <zh-CN>用户索引服务"某用户参与的所有事项"查询，按事项倒序便于前台呈现。</zh-CN>
--   <en>The user index serves "all items a user participates in" queries, ordered by item descending for front-end display.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItemParticipants_User' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemParticipants]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItemParticipants_User]
    ON [dbo].[PortalBiz_CollaborationItemParticipants] ([UserId], [ItemId] DESC)
END
GO
