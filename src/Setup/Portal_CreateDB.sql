/*
<lang>
  <zh-CN>旧 ASP.NET Portal Starter Kit 建库总脚本。本脚本会创建固定名称 `Portal` 数据库、删除同名旧对象并重建旧门户内容表、配置表、用户/角色表、讨论区存储过程、默认约束和外键；只能用于受控初始化或重建场景，不能直接用于承载真实数据的生产环境。</zh-CN>
  <en>Legacy ASP.NET Portal Starter Kit database creation script. This script creates the fixed-name `Portal` database, drops same-name legacy objects, and rebuilds legacy portal content tables, configuration tables, user/role tables, discussion stored procedures, default constraints, and foreign keys; it is only for controlled initialization or rebuild scenarios and must not be run directly against production environments containing real data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>先切换到 master 再创建数据库，避免在调用者当前数据库上下文中执行实例级建库操作。</zh-CN>
--   <en>Switch to master before creating the database so the instance-level create operation does not execute in the caller's current database context.</en>
-- </lang>
use [master]

-- <lang>
--   <zh-CN>创建固定名称 `Portal` 数据库；脚本未做存在性保护，执行前必须确认目标实例中不存在需要保留的同名数据库。</zh-CN>
--   <en>Create the fixed-name `Portal` database; the script has no existence guard here, so operators must confirm that no same-name database needing preservation exists on the target instance.</en>
-- </lang>
create database [Portal]
GO

-- <lang>
--   <zh-CN>后续 DDL/DROP 均在新建或目标 `Portal` 数据库中执行，作用域不应泄漏到其它业务库。</zh-CN>
--   <en>All later DDL and DROP operations run inside the new or target `Portal` database and must not leak into other business databases.</en>
-- </lang>
use [Portal]

-- <lang>
--   <zh-CN>重建前先移除旧外键，释放内容表、模块表、Tab 表和用户角色表之间的依赖，便于后续按旧顺序 DROP。</zh-CN>
--   <en>Before rebuilding, remove old foreign keys to release dependencies among content tables, module tables, tab tables, and user-role tables so later DROP statements can follow the legacy order.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Announcements_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Announcements_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Announcements_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]'))
ALTER TABLE [dbo].[Portal_Announcements] DROP CONSTRAINT [FK_Portal_Announcements_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Contacts_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Contacts_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Contacts_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]'))
ALTER TABLE [dbo].[Portal_Contacts] DROP CONSTRAINT [FK_Portal_Contacts_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Discussion_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Discussion_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Discussion_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]'))
ALTER TABLE [dbo].[Portal_Discussion] DROP CONSTRAINT [FK_Portal_Discussion_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Documents_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Documents_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Documents_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Documents]'))
ALTER TABLE [dbo].[Portal_Documents] DROP CONSTRAINT [FK_Portal_Documents_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Events_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Events_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Events_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Events]'))
ALTER TABLE [dbo].[Portal_Events] DROP CONSTRAINT [FK_Portal_Events_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Links_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Links_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Links_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Links]'))
ALTER TABLE [dbo].[Portal_Links] DROP CONSTRAINT [FK_Portal_Links_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_UserRoles_Roles]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_UserRoles_Roles]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] DROP CONSTRAINT [FK_UserRoles_Roles]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_UserRoles_Users]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_UserRoles_Users]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] DROP CONSTRAINT [FK_UserRoles_Users]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_PortalCfg_Modules_PortalCfg_Tabs]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_PortalCfg_Modules_PortalCfg_Tabs]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Modules_PortalCfg_Tabs]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
ALTER TABLE [dbo].[PortalCfg_Modules] DROP CONSTRAINT [FK_PortalCfg_Modules_PortalCfg_Tabs]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_ModuleSettings_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]'))
ALTER TABLE [dbo].[PortalCfg_ModuleSettings] DROP CONSTRAINT [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_PortalCfg_Tabs_PortalCfg_Globals]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_PortalCfg_Tabs_PortalCfg_Globals]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Tabs_PortalCfg_Globals]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
ALTER TABLE [dbo].[PortalCfg_Tabs] DROP CONSTRAINT [FK_PortalCfg_Tabs_PortalCfg_Globals]
GO
-- <lang>
--   <zh-CN>删除旧讨论区和模块维护存储过程，为后续通过动态 SQL 重新创建同名过程清理命名空间。</zh-CN>
--   <en>Drop legacy discussion and module-maintenance stored procedures to clear the namespace before recreating same-name procedures through dynamic SQL later.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetSingleMessage]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetSingleMessage]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetSingleMessage]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetSingleMessage]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetThreadMessages]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetThreadMessages]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetThreadMessages]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetThreadMessages]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetTopLevelMessages]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetTopLevelMessages]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetTopLevelMessages]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetTopLevelMessages]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_DeleteModule]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_DeleteModule]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_DeleteModule]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_DeleteModule]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_AddMessage]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_AddMessage]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_AddMessage]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_AddMessage]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetNextMessageID]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetNextMessageID]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetNextMessageID]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetNextMessageID]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetPrevMessageID]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetPrevMessageID]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetPrevMessageID]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetPrevMessageID]
GO
-- <lang>
--   <zh-CN>删除旧内容、配置、用户和角色表；这是破坏性重建路径，执行前必须确认不需要保留旧数据。</zh-CN>
--   <en>Drop legacy content, configuration, user, and role tables; this is a destructive rebuild path and operators must confirm old data does not need preservation before execution.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Announcements]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Announcements]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Announcements]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Contacts]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Contacts]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Contacts]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Discussion]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Discussion]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Discussion]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Documents]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Documents]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Documents]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Documents]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Events]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Events]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Events]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Events]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Links]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Links]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Links]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Links]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_ModuleSettings]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_ModuleSettings]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_ModuleSettings]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetRoleMembership]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetRoleMembership]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetRoleMembership]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetRoleMembership]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_UserRoles]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_UserRoles]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_UserRoles]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_Tabs]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_Tabs]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_Tabs]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Users]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Users]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Users]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Users]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_Globals]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_Globals]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_Globals]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_ModuleDefinitions]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_ModuleDefinitions]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleDefinitions]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_ModuleDefinitions]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Roles]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Roles]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Roles]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Roles]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_HtmlText]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_HtmlText]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_HtmlText]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_HtmlText]
GO
-- <lang>
--   <zh-CN>移除旧默认约束，避免重建表或补默认值时遇到同名约束残留；这里同时兼容新旧系统目录检查写法。</zh-CN>
--   <en>Remove old default constraints to avoid same-name leftovers during table rebuild or default backfill; this block keeps both newer and older system-catalog checks for compatibility.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Globals_AlwaysShowEditButton]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Globals_AlwaysShowEditButton]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Globals] DROP CONSTRAINT [DF_PortalCfg_Globals_AlwaysShowEditButton]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Modules_ShowMobile]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Modules_ShowMobile]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] DROP CONSTRAINT [DF_PortalCfg_Modules_ShowMobile]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Modules_CacheTimeout]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Modules_CacheTimeout]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_CacheTimeout]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_CacheTimeout]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] DROP CONSTRAINT [DF_PortalCfg_Modules_CacheTimeout]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Tabs_TabOrder]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Tabs_TabOrder]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_TabOrder]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_TabOrder]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] DROP CONSTRAINT [DF_PortalCfg_Tabs_TabOrder]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Tabs_ShowMobile]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Tabs_ShowMobile]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] DROP CONSTRAINT [DF_PortalCfg_Tabs_ShowMobile]
END


End
GO
-- <lang>
--   <zh-CN>从这里开始重建旧 Portal 表结构；每个表都带存在性保护，用于从已清理或空数据库中恢复基础 schema。</zh-CN>
--   <en>From here the script rebuilds the legacy Portal table schema; each table has an existence guard so the base schema can be restored into a cleaned or empty database.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_HtmlText]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_HtmlText]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_HtmlText]') AND type in (N'U'))
BEGIN
-- <lang>
--   <zh-CN>HTML 文本表保存旧 HtmlModule 的桌面与移动文本载荷；内容可信度由旧模块编辑权限控制，不在此 DDL 中净化。</zh-CN>
--   <en>The HTML text table stores desktop and mobile text payloads for the legacy HtmlModule; content trust is controlled by legacy module edit permissions and is not sanitized by this DDL.</en>
-- </lang>
CREATE TABLE [dbo].[Portal_HtmlText](
	[ModuleID] [int] NOT NULL,
	[DesktopHtml] [ntext] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MobileSummary] [ntext] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MobileDetails] [ntext] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_HtmlText] PRIMARY KEY NONCLUSTERED 
(
	[ModuleID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>角色表是旧门户授权目录的根表，后续用户角色表通过外键和级联删除关联到它。</zh-CN>
--   <en>The role table is the root of the legacy portal authorization catalog, and the later user-role table links to it through foreign keys and cascade deletion.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Roles]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Roles]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Roles]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Roles](
	[RoleID] [int] IDENTITY(0,1) NOT NULL,
	[PortalID] [int] NOT NULL,
	[RoleName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_PortalRoles] PRIMARY KEY NONCLUSTERED 
(
	[RoleID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>模块定义表登记旧桌面/移动 ASCX 控件入口，后续 `Portal_LoadConfig.sql` 会按固定 ID 装载示例定义。</zh-CN>
--   <en>The module-definition table registers legacy desktop/mobile ASCX control entry points, and later `Portal_LoadConfig.sql` loads sample definitions with fixed ids.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_ModuleDefinitions]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_ModuleDefinitions]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleDefinitions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PortalCfg_ModuleDefinitions](
	[ModuleDefId] [int] IDENTITY(1,1) NOT NULL,
	[FriendlyName] [nvarchar](150) COLLATE Latin1_General_CI_AS NOT NULL,
	[DesktopSourceFile] [nvarchar](250) COLLATE Latin1_General_CI_AS NOT NULL,
	[MobileSourceFile] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL,
 CONSTRAINT [PK_PortalCfg_ModuleDefinitions] PRIMARY KEY NONCLUSTERED 
(
	[ModuleDefId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>全局门户配置表保存旧 PortalName 和编辑按钮显示策略，是 Tab 层级的父级配置根。</zh-CN>
--   <en>The global portal configuration table stores the legacy PortalName and edit-button display policy and is the parent configuration root for tabs.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_Globals]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_Globals]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PortalCfg_Globals](
	[PortalId] [int] IDENTITY(1,1) NOT NULL,
	[PortalName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AlwaysShowEditButton] [bit] NULL,
 CONSTRAINT [PK_PortalCfg_Globals] PRIMARY KEY CLUSTERED 
(
	[PortalId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>旧用户表保存登录名、旧密码样本和邮箱；新凭据治理已由后续 `Portal_UserCredentials.sql` 分离承接。</zh-CN>
--   <en>The legacy user table stores sign-in name, legacy password sample, and email; newer credential governance is separated later by `Portal_UserCredentials.sql`.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Users]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Users]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Users]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Users](
	[UserID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Password] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Email] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_PortalUsers] PRIMARY KEY NONCLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
 CONSTRAINT [IX_PortalUsers] UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>Tab 表保存旧导航层级、访问角色文本和移动显示开关，后续模块表通过 TabId 归属到该导航节点。</zh-CN>
--   <en>The tab table stores legacy navigation hierarchy, access-role text, and mobile display flags, and later module rows attach to navigation nodes through TabId.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_Tabs]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_Tabs]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PortalCfg_Tabs](
	[TabId] [int] IDENTITY(1,1) NOT NULL,
	[TabName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TabOrder] [int] NULL,
	[AccessRoles] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ShowMobile] [bit] NULL,
	[MobileTabName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PortalId] [int] NULL,
 CONSTRAINT [PK_PortalCfg_Tabs] PRIMARY KEY CLUSTERED 
(
	[TabId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>用户角色桥表建立账号与角色之间的多对多关系；实际外键在脚本末尾恢复，以便先完成表重建。</zh-CN>
--   <en>The user-role bridge table establishes the many-to-many relationship between accounts and roles; its real foreign keys are restored at the end so table rebuild can finish first.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_UserRoles]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_UserRoles]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_UserRoles](
	[UserID] [int] NOT NULL,
	[RoleID] [int] NOT NULL
)
END
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetRoleMembership]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetRoleMembership]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetRoleMembership]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'



/* returns all members for the specified role */
CREATE PROCEDURE [dbo].[Portal_GetRoleMembership]
(
    @RoleID  int
)
AS

SELECT  
    Portal_UserRoles.UserID,
    Name,
    Email

FROM Portal_UserRoles
    
INNER JOIN 
    Portal_Users On Portal_Users.UserID = Portal_UserRoles.UserID

WHERE   
    Portal_UserRoles.RoleID = @RoleID



' 
END
GO
-- <lang>
--   <zh-CN>模块实例表保存 Tab 内模块标题、排序、编辑角色、pane、缓存和模块定义引用，是旧 Web Forms 页面组装核心。</zh-CN>
--   <en>The module instance table stores module title, ordering, edit roles, pane, cache, and module-definition reference within tabs, forming the core of legacy Web Forms page composition.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PortalCfg_Modules](
	[ModuleId] [int] IDENTITY(1,1) NOT NULL,
	[ModuleTitle] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ModuleOrder] [int] NULL,
	[EditRoles] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaneName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ShowMobile] [bit] NULL,
	[CacheTimeout] [int] NULL,
	[ModuleDefId] [int] NULL,
	[TabId] [int] NULL,
 CONSTRAINT [PK_PortalCfg_Modules] PRIMARY KEY CLUSTERED 
(
	[ModuleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>模块设置表保存旧模块级键值配置，外键稍后指回模块实例，避免设置行脱离模块上下文。</zh-CN>
--   <en>The module-setting table stores legacy module-level key/value configuration, with a later foreign key back to module instances so setting rows cannot outlive module context.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[PortalCfg_ModuleSettings]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[PortalCfg_ModuleSettings]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PortalCfg_ModuleSettings](
	[ModuleSettingId] [int] IDENTITY(1,1) NOT NULL,
	[SettingName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SettingText] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ModuleId] [int] NULL,
 CONSTRAINT [PK_PortalCfg_ModuleSettings] PRIMARY KEY CLUSTERED 
(
	[ModuleSettingId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>以下内容模块表共享 `ModuleID` 归属模型；每张表只保存旧示例模块所需的低敏展示字段。</zh-CN>
--   <en>The content-module tables below share the `ModuleID` ownership model, and each table stores only the low-sensitivity display fields required by legacy sample modules.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Links]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Links]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Links]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Links](
	[ItemID] [int] IDENTITY(0,1) NOT NULL,
	[ModuleID] [int] NOT NULL,
	[CreatedByUser] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime] NULL,
	[Title] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Url] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MobileUrl] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ViewOrder] [int] NULL,
	[Description] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Links] PRIMARY KEY NONCLUSTERED 
(
	[ItemID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>事件表保存旧 Events 模块标题、地点时间、描述和过期时间，不表达现代工作流事件或审计事件。</zh-CN>
--   <en>The events table stores legacy Events module title, where/when text, description, and expiration time, and does not represent modern workflow or audit events.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Events]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Events]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Events]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Events](
	[ItemID] [int] IDENTITY(0,1) NOT NULL,
	[ModuleID] [int] NOT NULL,
	[CreatedByUser] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime] NULL,
	[Title] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WhereWhen] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ExpireDate] [datetime] NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY NONCLUSTERED 
(
	[ItemID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>文档表保存旧 Documents 模块的文件名、友好名、类别和可选二进制内容；现代上传安全由后续策略代码补强。</zh-CN>
--   <en>The documents table stores legacy Documents module filename, friendly name, category, and optional binary content; modern upload safety is strengthened later by policy code.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Documents]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Documents]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Documents]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Documents](
	[ItemID] [int] IDENTITY(0,1) NOT NULL,
	[ModuleID] [int] NOT NULL,
	[CreatedByUser] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime] NULL,
	[FileNameUrl] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FileFriendlyName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Category] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Content] [image] NULL,
	[ContentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ContentSize] [int] NULL,
 CONSTRAINT [PK_Documents] PRIMARY KEY NONCLUSTERED 
(
	[ItemID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>讨论表使用 DisplayOrder 字符串表达树形回复顺序，后续存储过程依赖该兼容排序模型。</zh-CN>
--   <en>The discussion table uses a DisplayOrder string to represent threaded reply order, and later stored procedures depend on this compatible ordering model.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Discussion]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Discussion]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Discussion](
	[ItemID] [int] IDENTITY(0,1) NOT NULL,
	[ModuleID] [int] NOT NULL,
	[Title] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime] NULL,
	[Body] [nvarchar](3000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DisplayOrder] [nvarchar](750) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedByUser] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Discussion] PRIMARY KEY NONCLUSTERED 
(
	[ItemID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>联系人和公告表延续旧内容模块字段模型，邮箱、链接和描述均为展示数据，不在建库脚本中做语义校验。</zh-CN>
--   <en>The contacts and announcements tables keep the legacy content-module field model, and email, link, and description values are display data without semantic validation in the creation script.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Contacts]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Contacts]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Contacts](
	[ItemID] [int] IDENTITY(0,1) NOT NULL,
	[ModuleID] [int] NOT NULL,
	[CreatedByUser] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime] NULL,
	[Name] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Role] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Email] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Contact1] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Contact2] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Contacts] PRIMARY KEY NONCLUSTERED 
(
	[ItemID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Table [dbo].[Portal_Announcements]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Table [dbo].[Portal_Announcements]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Portal_Announcements](
	[ItemID] [int] IDENTITY(0,1) NOT NULL,
	[ModuleID] [int] NOT NULL,
	[CreatedByUser] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime] NULL,
	[Title] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MoreLink] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MobileMoreLink] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ExpireDate] [datetime] NULL,
	[Description] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Announcements] PRIMARY KEY NONCLUSTERED 
(
	[ItemID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
END
GO
-- <lang>
--   <zh-CN>讨论区导航过程族读取 DisplayOrder 前后关系、顶级消息、线程回复和单条消息详情，只服务旧讨论模块。</zh-CN>
--   <en>The discussion navigation procedure family reads DisplayOrder adjacency, top-level messages, thread replies, and single-message details, serving only the legacy discussion module.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetPrevMessageID]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetPrevMessageID]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetPrevMessageID]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'


CREATE PROCEDURE [dbo].[Portal_GetPrevMessageID]
(
    @ItemID int,
    @PrevID int OUTPUT
)
AS

DECLARE @CurrentDisplayOrder as nvarchar(750)
DECLARE @CurrentModule as int

/* Find DisplayOrder of current item */
SELECT
    @CurrentDisplayOrder = DisplayOrder,
    @CurrentModule = ModuleID
FROM Portal_Discussion
WHERE
    ItemID = @ItemID

/* Get the previous message in the same module */
SELECT Top 1
    @PrevID = ItemID

FROM Portal_Discussion

WHERE
    DisplayOrder < @CurrentDisplayOrder
    AND
    ModuleID = @CurrentModule

ORDER BY
    DisplayOrder DESC

/* already at the beginning of this module? */
IF @@Rowcount < 1
    SET @PrevID = null



' 
END
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetNextMessageID]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetNextMessageID]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetNextMessageID]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'


CREATE PROCEDURE [dbo].[Portal_GetNextMessageID]
(
    @ItemID int,
    @NextID int OUTPUT
)
AS

DECLARE @CurrentDisplayOrder as nvarchar(750)
DECLARE @CurrentModule as int

/* Find DisplayOrder of current item */
SELECT
    @CurrentDisplayOrder = DisplayOrder,
    @CurrentModule = ModuleID
FROM Portal_Discussion
WHERE
    ItemID = @ItemID

/* Get the next message in the same module */
SELECT Top 1
    @NextID = ItemID

FROM Portal_Discussion

WHERE
    DisplayOrder > @CurrentDisplayOrder
    AND
    ModuleID = @CurrentModule

ORDER BY
    DisplayOrder ASC

/* end of this thread? */
IF @@Rowcount < 1
    SET @NextID = null



' 
END
GO
-- <lang>
--   <zh-CN>新增讨论消息过程按父消息 DisplayOrder 拼接当前时间字符串，保持旧 Starter Kit 的树形排序兼容策略。</zh-CN>
--   <en>The add-message procedure appends the current timestamp string to the parent message DisplayOrder, preserving the tree-ordering compatibility strategy from the old Starter Kit.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_AddMessage]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_AddMessage]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_AddMessage]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'



CREATE PROCEDURE [dbo].[Portal_AddMessage]
(
    @ItemID int OUTPUT,
    @Title nvarchar(100),
    @Body nvarchar(3000),
    @ParentID int,
    @UserName nvarchar(100),
    @ModuleID int
)   

AS 

/* Find DisplayOrder of parent item */
DECLARE @ParentDisplayOrder as nvarchar(750)

SET @ParentDisplayOrder = ""

SELECT 
    @ParentDisplayOrder = DisplayOrder
FROM Portal_Discussion 
WHERE 
    ItemID = @ParentID

INSERT INTO Portal_Discussion
(
    Title,
    Body,
    DisplayOrder,
    CreatedDate, 
    CreatedByUser,
    ModuleID
)

VALUES
(
    @Title,
    @Body,
    @ParentDisplayOrder + CONVERT( nvarchar(24), GetDate(), 21 ),
    GetDate(),
    @UserName,
    @ModuleID
)

SELECT 
    @ItemID = @@Identity



' 
END
GO
-- <lang>
--   <zh-CN>删除模块过程按 ModuleID 清除各旧内容表记录；它不会删除模块配置行本身，也不处理现代业务模块表。</zh-CN>
--   <en>The delete-module procedure clears records from legacy content tables by ModuleID; it does not delete the module configuration row itself and does not handle modern business-module tables.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_DeleteModule]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_DeleteModule]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_DeleteModule]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'

CREATE  PROCEDURE [dbo].[Portal_DeleteModule]
(
    @ModuleID       int
)
AS
      DELETE FROM Portal_Announcements
      WHERE ModuleID = @ModuleID

      DELETE FROM Portal_Contacts
      WHERE ModuleID = @ModuleID

      DELETE FROM Portal_Discussion
      WHERE ModuleID = @ModuleID

      DELETE FROM Portal_Documents
      WHERE ModuleID = @ModuleID

      DELETE FROM Portal_Events
      WHERE ModuleID = @ModuleID

      DELETE FROM Portal_HtmlText
      WHERE ModuleID = @ModuleID

      DELETE FROM Portal_Links
      WHERE ModuleID = @ModuleID


' 
END
GO
-- <lang>
--   <zh-CN>讨论列表查询过程族基于 DisplayOrder 深度和前缀计算父子关系、缩进和回复数量，保留旧 UI 的线程显示模型。</zh-CN>
--   <en>The discussion-list query procedure family calculates parent-child relationships, indentation, and reply counts from DisplayOrder depth and prefix, preserving the old UI thread display model.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetTopLevelMessages]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetTopLevelMessages]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetTopLevelMessages]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'


CREATE PROCEDURE [dbo].[Portal_GetTopLevelMessages]
(
    @ModuleID int
)
AS

SELECT
    ItemID,
	Body,
    DisplayOrder,
    LEFT(DisplayOrder, 23) AS Parent,    
    (SELECT COUNT(*) -1  FROM Portal_Discussion Disc2 WHERE LEFT(Disc2.DisplayOrder,LEN(RTRIM(Disc.DisplayOrder))) = Disc.DisplayOrder) AS ChildCount,
    Title,  
    CreatedByUser,
    CreatedDate

FROM Portal_Discussion Disc

WHERE 
    ModuleID=@ModuleID
  AND
    (LEN( DisplayOrder ) / 23 ) = 1

ORDER BY
    DisplayOrder



' 
END
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetThreadMessages]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetThreadMessages]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetThreadMessages]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'



CREATE PROCEDURE [dbo].[Portal_GetThreadMessages]
(
    @Parent nvarchar(750)
)
AS

SELECT
    ItemID,
	ModuleID,
    DisplayOrder,
    REPLICATE( ''&nbsp;'', ( ( LEN( DisplayOrder ) / 23 ) - 1 ) * 5 ) AS Indent,
    Title,  
    CreatedByUser,
    CreatedDate,
    Body

FROM Portal_Discussion

WHERE
    LEFT(DisplayOrder, 23) = @Parent
  AND
    (LEN( DisplayOrder ) / 23 ) > 1

ORDER BY
    DisplayOrder



' 
END
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：StoredProcedure [dbo].[Portal_GetSingleMessage]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: StoredProcedure [dbo].[Portal_GetSingleMessage]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetSingleMessage]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'



CREATE  PROCEDURE [dbo].[Portal_GetSingleMessage]
(
    @ItemID int
)
AS

DECLARE @nextMessageID int
EXECUTE Portal_GetNextMessageID @ItemID, @nextMessageID OUTPUT
DECLARE @prevMessageID int
EXECUTE Portal_GetPrevMessageID @ItemID, @prevMessageID OUTPUT

SELECT
    ItemID,
    ModuleID,
    Title,
    CreatedByUser,
    CreatedDate,
    Body,
    DisplayOrder,
    NextMessageID = @nextMessageID,
    PrevMessageID = @prevMessageID

FROM Portal_Discussion

WHERE
    ItemID = @ItemID




' 
END
GO
-- <lang>
--   <zh-CN>重建默认约束，恢复旧配置字段的缺省行为；这些默认值仅覆盖旧 UI 兼容字段，不代表现代策略默认值。</zh-CN>
--   <en>Rebuild default constraints to restore default behavior for legacy configuration fields; these defaults cover only old UI compatibility fields and do not represent modern policy defaults.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Globals_AlwaysShowEditButton]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Globals_AlwaysShowEditButton]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Globals] ADD  CONSTRAINT [DF_PortalCfg_Globals_AlwaysShowEditButton]  DEFAULT ((0)) FOR [AlwaysShowEditButton]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Modules_ShowMobile]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Modules_ShowMobile]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] ADD  CONSTRAINT [DF_PortalCfg_Modules_ShowMobile]  DEFAULT ((0)) FOR [ShowMobile]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Modules_CacheTimeout]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Modules_CacheTimeout]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_CacheTimeout]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_CacheTimeout]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] ADD  CONSTRAINT [DF_PortalCfg_Modules_CacheTimeout]  DEFAULT ((0)) FOR [CacheTimeout]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Tabs_TabOrder]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Tabs_TabOrder]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_TabOrder]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_TabOrder]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] ADD  CONSTRAINT [DF_PortalCfg_Tabs_TabOrder]  DEFAULT ((0)) FOR [TabOrder]
END


End
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：Default [DF_PortalCfg_Tabs_ShowMobile]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: Default [DF_PortalCfg_Tabs_ShowMobile]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] ADD  CONSTRAINT [DF_PortalCfg_Tabs_ShowMobile]  DEFAULT ((0)) FOR [ShowMobile]
END


End
GO
-- <lang>
--   <zh-CN>脚本末尾恢复内容表、用户角色表和配置表外键，重新建立模块、Tab、Portal、用户和角色之间的旧引用边界。</zh-CN>
--   <en>The end of the script restores foreign keys for content, user-role, and configuration tables, re-establishing legacy reference boundaries among modules, tabs, portals, users, and roles.</en>
-- </lang>
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Announcements_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Announcements_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Announcements_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]'))
ALTER TABLE [dbo].[Portal_Announcements]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Announcements_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Announcements_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]'))
ALTER TABLE [dbo].[Portal_Announcements] CHECK CONSTRAINT [FK_Portal_Announcements_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Contacts_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Contacts_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Contacts_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]'))
ALTER TABLE [dbo].[Portal_Contacts]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Contacts_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Contacts_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]'))
ALTER TABLE [dbo].[Portal_Contacts] CHECK CONSTRAINT [FK_Portal_Contacts_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Discussion_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Discussion_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Discussion_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]'))
ALTER TABLE [dbo].[Portal_Discussion]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Discussion_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Discussion_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]'))
ALTER TABLE [dbo].[Portal_Discussion] CHECK CONSTRAINT [FK_Portal_Discussion_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Documents_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Documents_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Documents_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Documents]'))
ALTER TABLE [dbo].[Portal_Documents]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Documents_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Documents_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Documents]'))
ALTER TABLE [dbo].[Portal_Documents] CHECK CONSTRAINT [FK_Portal_Documents_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Events_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Events_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Events_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Events]'))
ALTER TABLE [dbo].[Portal_Events]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Events_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Events_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Events]'))
ALTER TABLE [dbo].[Portal_Events] CHECK CONSTRAINT [FK_Portal_Events_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_Portal_Links_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_Portal_Links_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Links_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Links]'))
ALTER TABLE [dbo].[Portal_Links]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Links_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Links_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Links]'))
ALTER TABLE [dbo].[Portal_Links] CHECK CONSTRAINT [FK_Portal_Links_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_UserRoles_Roles]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_UserRoles_Roles]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles]  WITH NOCHECK ADD  CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY([RoleID])
REFERENCES [dbo].[Portal_Roles] ([RoleID])
ON DELETE CASCADE
NOT FOR REPLICATION
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_UserRoles_Users]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_UserRoles_Users]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles]  WITH NOCHECK ADD  CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Portal_Users] ([UserID])
ON DELETE CASCADE
NOT FOR REPLICATION
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_PortalCfg_Modules_PortalCfg_Tabs]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_PortalCfg_Modules_PortalCfg_Tabs]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Modules_PortalCfg_Tabs]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
ALTER TABLE [dbo].[PortalCfg_Modules]  WITH CHECK ADD  CONSTRAINT [FK_PortalCfg_Modules_PortalCfg_Tabs] FOREIGN KEY([TabId])
REFERENCES [dbo].[PortalCfg_Tabs] ([TabId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Modules_PortalCfg_Tabs]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
ALTER TABLE [dbo].[PortalCfg_Modules] CHECK CONSTRAINT [FK_PortalCfg_Modules_PortalCfg_Tabs]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_ModuleSettings_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]'))
ALTER TABLE [dbo].[PortalCfg_ModuleSettings]  WITH CHECK ADD  CONSTRAINT [FK_PortalCfg_ModuleSettings_PortalCfg_Modules] FOREIGN KEY([ModuleId])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_ModuleSettings_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]'))
ALTER TABLE [dbo].[PortalCfg_ModuleSettings] CHECK CONSTRAINT [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]
GO
/*
<lang>
  <zh-CN>SQL Server 生成的对象边界标记：ForeignKey [FK_PortalCfg_Tabs_PortalCfg_Globals]；仅作为脚本分段导航，后续 DDL/DML 语义由实际 SQL 语句决定。</zh-CN>
  <en>SQL Server-generated object boundary marker: ForeignKey [FK_PortalCfg_Tabs_PortalCfg_Globals]; retained only as script-section navigation, while the following DDL/DML statements define the actual behavior.</en>
</lang>
*/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Tabs_PortalCfg_Globals]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
ALTER TABLE [dbo].[PortalCfg_Tabs]  WITH CHECK ADD  CONSTRAINT [FK_PortalCfg_Tabs_PortalCfg_Globals] FOREIGN KEY([PortalId])
REFERENCES [dbo].[PortalCfg_Globals] ([PortalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Tabs_PortalCfg_Globals]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
ALTER TABLE [dbo].[PortalCfg_Tabs] CHECK CONSTRAINT [FK_PortalCfg_Tabs_PortalCfg_Globals]
GO
