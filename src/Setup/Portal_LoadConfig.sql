/*
<lang>
  <zh-CN>旧 Portal 模块定义装载脚本。本脚本清空并重建 `PortalCfg_ModuleDefinitions` 的固定示例模块目录，只适合初始化或受控重置场景；它不会装载用户内容、角色或真实业务数据。</zh-CN>
  <en>Legacy Portal module-definition load script. This script clears and rebuilds the fixed sample module catalog in `PortalCfg_ModuleDefinitions` and is suitable only for initialization or controlled reset scenarios; it does not load user content, roles, or real business data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>显式切换到 Portal 数据库，确保模块定义清空和固定 ID 插入不会落到调用者当前数据库。</zh-CN>
--   <en>Explicitly switch to the Portal database so module-definition clearing and fixed-id inserts do not land in the caller's current database.</en>
-- </lang>
use [Portal]

/****** Object:  Table [dbo].[PortalCfg_ModuleDefinitions]    Script Date: 02/28/2012 21:05:20 ******/
-- <lang>
--   <zh-CN>清空模块定义表，为后续固定 `ModuleDefId` seed 重建完整旧目录；执行会删除管理员已有模块定义。</zh-CN>
--   <en>Clear the module-definition table before rebuilding the complete legacy catalog with fixed `ModuleDefId` seed values; execution removes administrator-maintained module definitions.</en>
-- </lang>
DELETE FROM [PortalCfg_ModuleDefinitions]
GO
/****** Object:  Table [dbo].[PortalCfg_ModuleDefinitions]    Script Date: 02/28/2012 21:05:20 ******/
-- <lang>
--   <zh-CN>临时启用 identity insert 以保留旧模块定义 ID；这些 ID 被历史配置和示例数据引用。</zh-CN>
--   <en>Temporarily enable identity insert to preserve legacy module-definition ids referenced by historical configuration and sample data.</en>
-- </lang>
SET IDENTITY_INSERT [PortalCfg_ModuleDefinitions] ON
-- <lang>
--   <zh-CN>以下 seed 只列出旧桌面/移动模块入口和后台管理模块入口，不验证 ASCX 文件是否存在。</zh-CN>
--   <en>The seed below lists only legacy desktop/mobile module entries and administration module entries, and does not verify whether the ASCX files exist.</en>
-- </lang>
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (1, N'Announcements', N'DesktopModules/Announcements.ascx', N'MobileModules/Announcements.ascx')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (2, N'Contacts', N'DesktopModules/Contacts.ascx', N'MobileModules/Contacts.ascx')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (3, N'Discussion', N'DesktopModules/Discussion.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (4, N'Events', N'DesktopModules/Events.ascx', N'MobileModules/Events.ascx')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (5, N'Html Document', N'DesktopModules/HtmlModule.ascx', N'MobileModules/text.ascx')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (6, N'Image', N'DesktopModules/ImageModule.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (7, N'Links', N'DesktopModules/Links.ascx', N'MobileModules/Links.ascx')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (8, N'QuickLinks', N'DesktopModules/QuickLinks.ascx', N'MobileModules/Links.ascx')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (9, N'XML/XSL', N'DesktopModules/XmlModule.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (10, N'Documents', N'DesktopModules/Document.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (11, N'Module Types (Admin)', N'Admin/ModuleDefs.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (12, N'Roles (Admin)', N'Admin/Roles.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (13, N'Tabs (Admin)', N'Admin/Tabs.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (14, N'Site Settings (Admin)', N'Admin/SiteSettings.ascx', N'')
INSERT [PortalCfg_ModuleDefinitions] ([ModuleDefId], [FriendlyName], [DesktopSourceFile], [MobileSourceFile]) VALUES (15, N'Manage Users (Admin)', N'Admin/Users.ascx', N'')
-- <lang>
--   <zh-CN>关闭 identity insert，恢复表对后续普通插入的自增主键保护。</zh-CN>
--   <en>Disable identity insert to restore the table's identity-key protection for later normal inserts.</en>
-- </lang>
SET IDENTITY_INSERT [PortalCfg_ModuleDefinitions] OFF
