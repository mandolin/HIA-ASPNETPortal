use [Portal]

/****** Object:  Table [dbo].[PortalCfg_ModuleDefinitions]    Script Date: 02/28/2012 21:05:20 ******/
DELETE FROM [PortalCfg_ModuleDefinitions]
GO
/****** Object:  Table [dbo].[PortalCfg_ModuleDefinitions]    Script Date: 02/28/2012 21:05:20 ******/
SET IDENTITY_INSERT [PortalCfg_ModuleDefinitions] ON
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
SET IDENTITY_INSERT [PortalCfg_ModuleDefinitions] OFF
