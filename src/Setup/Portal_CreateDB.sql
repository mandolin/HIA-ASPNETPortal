use [master]

create database [Portal]
GO

use [Portal]

/****** Object:  ForeignKey [FK_Portal_Announcements_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Announcements_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]'))
ALTER TABLE [dbo].[Portal_Announcements] DROP CONSTRAINT [FK_Portal_Announcements_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Contacts_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Contacts_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]'))
ALTER TABLE [dbo].[Portal_Contacts] DROP CONSTRAINT [FK_Portal_Contacts_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Discussion_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Discussion_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]'))
ALTER TABLE [dbo].[Portal_Discussion] DROP CONSTRAINT [FK_Portal_Discussion_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Documents_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Documents_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Documents]'))
ALTER TABLE [dbo].[Portal_Documents] DROP CONSTRAINT [FK_Portal_Documents_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Events_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Events_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Events]'))
ALTER TABLE [dbo].[Portal_Events] DROP CONSTRAINT [FK_Portal_Events_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Links_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Links_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Links]'))
ALTER TABLE [dbo].[Portal_Links] DROP CONSTRAINT [FK_Portal_Links_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_UserRoles_Roles]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] DROP CONSTRAINT [FK_UserRoles_Roles]
GO
/****** Object:  ForeignKey [FK_UserRoles_Users]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] DROP CONSTRAINT [FK_UserRoles_Users]
GO
/****** Object:  ForeignKey [FK_PortalCfg_Modules_PortalCfg_Tabs]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Modules_PortalCfg_Tabs]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
ALTER TABLE [dbo].[PortalCfg_Modules] DROP CONSTRAINT [FK_PortalCfg_Modules_PortalCfg_Tabs]
GO
/****** Object:  ForeignKey [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_ModuleSettings_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]'))
ALTER TABLE [dbo].[PortalCfg_ModuleSettings] DROP CONSTRAINT [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_PortalCfg_Tabs_PortalCfg_Globals]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Tabs_PortalCfg_Globals]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
ALTER TABLE [dbo].[PortalCfg_Tabs] DROP CONSTRAINT [FK_PortalCfg_Tabs_PortalCfg_Globals]
GO
/****** Object:  StoredProcedure [dbo].[Portal_GetSingleMessage]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetSingleMessage]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetSingleMessage]
GO
/****** Object:  StoredProcedure [dbo].[Portal_GetThreadMessages]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetThreadMessages]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetThreadMessages]
GO
/****** Object:  StoredProcedure [dbo].[Portal_GetTopLevelMessages]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetTopLevelMessages]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetTopLevelMessages]
GO
/****** Object:  StoredProcedure [dbo].[Portal_DeleteModule]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_DeleteModule]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_DeleteModule]
GO
/****** Object:  StoredProcedure [dbo].[Portal_AddMessage]    Script Date: 03/01/2012 23:02:09 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_AddMessage]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_AddMessage]
GO
/****** Object:  StoredProcedure [dbo].[Portal_GetNextMessageID]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetNextMessageID]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetNextMessageID]
GO
/****** Object:  StoredProcedure [dbo].[Portal_GetPrevMessageID]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetPrevMessageID]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetPrevMessageID]
GO
/****** Object:  Table [dbo].[Portal_Announcements]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Announcements]
GO
/****** Object:  Table [dbo].[Portal_Contacts]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Contacts]
GO
/****** Object:  Table [dbo].[Portal_Discussion]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Discussion]
GO
/****** Object:  Table [dbo].[Portal_Documents]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Documents]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Documents]
GO
/****** Object:  Table [dbo].[Portal_Events]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Events]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Events]
GO
/****** Object:  Table [dbo].[Portal_Links]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Links]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Links]
GO
/****** Object:  Table [dbo].[PortalCfg_ModuleSettings]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_ModuleSettings]
GO
/****** Object:  Table [dbo].[PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_Modules]
GO
/****** Object:  StoredProcedure [dbo].[Portal_GetRoleMembership]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_GetRoleMembership]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[Portal_GetRoleMembership]
GO
/****** Object:  Table [dbo].[Portal_UserRoles]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_UserRoles]
GO
/****** Object:  Table [dbo].[PortalCfg_Tabs]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_Tabs]
GO
/****** Object:  Table [dbo].[Portal_Users]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Users]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Users]
GO
/****** Object:  Table [dbo].[PortalCfg_Globals]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_Globals]
GO
/****** Object:  Table [dbo].[PortalCfg_ModuleDefinitions]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleDefinitions]') AND type in (N'U'))
DROP TABLE [dbo].[PortalCfg_ModuleDefinitions]
GO
/****** Object:  Table [dbo].[Portal_Roles]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_Roles]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_Roles]
GO
/****** Object:  Table [dbo].[Portal_HtmlText]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_HtmlText]') AND type in (N'U'))
DROP TABLE [dbo].[Portal_HtmlText]
GO
/****** Object:  Default [DF_PortalCfg_Globals_AlwaysShowEditButton]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Globals] DROP CONSTRAINT [DF_PortalCfg_Globals_AlwaysShowEditButton]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Modules_ShowMobile]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] DROP CONSTRAINT [DF_PortalCfg_Modules_ShowMobile]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Modules_CacheTimeout]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_CacheTimeout]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_CacheTimeout]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] DROP CONSTRAINT [DF_PortalCfg_Modules_CacheTimeout]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Tabs_TabOrder]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_TabOrder]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_TabOrder]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] DROP CONSTRAINT [DF_PortalCfg_Tabs_TabOrder]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Tabs_ShowMobile]    Script Date: 03/01/2012 23:02:10 ******/
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] DROP CONSTRAINT [DF_PortalCfg_Tabs_ShowMobile]
END


End
GO
/****** Object:  Table [dbo].[Portal_HtmlText]    Script Date: 03/01/2012 23:02:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_HtmlText]') AND type in (N'U'))
BEGIN
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
/****** Object:  Table [dbo].[Portal_Roles]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[PortalCfg_ModuleDefinitions]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[PortalCfg_Globals]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Users]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[PortalCfg_Tabs]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_UserRoles]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_GetRoleMembership]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[PortalCfg_ModuleSettings]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Links]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Events]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Documents]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Discussion]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Contacts]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Table [dbo].[Portal_Announcements]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_GetPrevMessageID]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_GetNextMessageID]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_AddMessage]    Script Date: 03/01/2012 23:02:09 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_DeleteModule]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_GetTopLevelMessages]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_GetThreadMessages]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  StoredProcedure [dbo].[Portal_GetSingleMessage]    Script Date: 03/01/2012 23:02:10 ******/
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
/****** Object:  Default [DF_PortalCfg_Globals_AlwaysShowEditButton]    Script Date: 03/01/2012 23:02:10 ******/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Globals]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Globals_AlwaysShowEditButton]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Globals] ADD  CONSTRAINT [DF_PortalCfg_Globals_AlwaysShowEditButton]  DEFAULT ((0)) FOR [AlwaysShowEditButton]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Modules_ShowMobile]    Script Date: 03/01/2012 23:02:10 ******/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] ADD  CONSTRAINT [DF_PortalCfg_Modules_ShowMobile]  DEFAULT ((0)) FOR [ShowMobile]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Modules_CacheTimeout]    Script Date: 03/01/2012 23:02:10 ******/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Modules_CacheTimeout]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Modules_CacheTimeout]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Modules] ADD  CONSTRAINT [DF_PortalCfg_Modules_CacheTimeout]  DEFAULT ((0)) FOR [CacheTimeout]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Tabs_TabOrder]    Script Date: 03/01/2012 23:02:10 ******/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_TabOrder]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_TabOrder]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] ADD  CONSTRAINT [DF_PortalCfg_Tabs_TabOrder]  DEFAULT ((0)) FOR [TabOrder]
END


End
GO
/****** Object:  Default [DF_PortalCfg_Tabs_ShowMobile]    Script Date: 03/01/2012 23:02:10 ******/
IF Not EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_PortalCfg_Tabs_ShowMobile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
Begin
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_PortalCfg_Tabs_ShowMobile]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PortalCfg_Tabs] ADD  CONSTRAINT [DF_PortalCfg_Tabs_ShowMobile]  DEFAULT ((0)) FOR [ShowMobile]
END


End
GO
/****** Object:  ForeignKey [FK_Portal_Announcements_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Announcements_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]'))
ALTER TABLE [dbo].[Portal_Announcements]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Announcements_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Announcements_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Announcements]'))
ALTER TABLE [dbo].[Portal_Announcements] CHECK CONSTRAINT [FK_Portal_Announcements_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Contacts_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Contacts_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]'))
ALTER TABLE [dbo].[Portal_Contacts]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Contacts_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Contacts_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Contacts]'))
ALTER TABLE [dbo].[Portal_Contacts] CHECK CONSTRAINT [FK_Portal_Contacts_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Discussion_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Discussion_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]'))
ALTER TABLE [dbo].[Portal_Discussion]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Discussion_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Discussion_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Discussion]'))
ALTER TABLE [dbo].[Portal_Discussion] CHECK CONSTRAINT [FK_Portal_Discussion_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Documents_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Documents_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Documents]'))
ALTER TABLE [dbo].[Portal_Documents]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Documents_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Documents_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Documents]'))
ALTER TABLE [dbo].[Portal_Documents] CHECK CONSTRAINT [FK_Portal_Documents_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Events_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Events_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Events]'))
ALTER TABLE [dbo].[Portal_Events]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Events_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Events_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Events]'))
ALTER TABLE [dbo].[Portal_Events] CHECK CONSTRAINT [FK_Portal_Events_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_Portal_Links_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Links_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Links]'))
ALTER TABLE [dbo].[Portal_Links]  WITH CHECK ADD  CONSTRAINT [FK_Portal_Links_PortalCfg_Modules] FOREIGN KEY([ModuleID])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Portal_Links_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_Links]'))
ALTER TABLE [dbo].[Portal_Links] CHECK CONSTRAINT [FK_Portal_Links_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_UserRoles_Roles]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles]  WITH NOCHECK ADD  CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY([RoleID])
REFERENCES [dbo].[Portal_Roles] ([RoleID])
ON DELETE CASCADE
NOT FOR REPLICATION
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles]
GO
/****** Object:  ForeignKey [FK_UserRoles_Users]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles]  WITH NOCHECK ADD  CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Portal_Users] ([UserID])
ON DELETE CASCADE
NOT FOR REPLICATION
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Portal_UserRoles]'))
ALTER TABLE [dbo].[Portal_UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users]
GO
/****** Object:  ForeignKey [FK_PortalCfg_Modules_PortalCfg_Tabs]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Modules_PortalCfg_Tabs]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
ALTER TABLE [dbo].[PortalCfg_Modules]  WITH CHECK ADD  CONSTRAINT [FK_PortalCfg_Modules_PortalCfg_Tabs] FOREIGN KEY([TabId])
REFERENCES [dbo].[PortalCfg_Tabs] ([TabId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Modules_PortalCfg_Tabs]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]'))
ALTER TABLE [dbo].[PortalCfg_Modules] CHECK CONSTRAINT [FK_PortalCfg_Modules_PortalCfg_Tabs]
GO
/****** Object:  ForeignKey [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_ModuleSettings_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]'))
ALTER TABLE [dbo].[PortalCfg_ModuleSettings]  WITH CHECK ADD  CONSTRAINT [FK_PortalCfg_ModuleSettings_PortalCfg_Modules] FOREIGN KEY([ModuleId])
REFERENCES [dbo].[PortalCfg_Modules] ([ModuleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_ModuleSettings_PortalCfg_Modules]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModuleSettings]'))
ALTER TABLE [dbo].[PortalCfg_ModuleSettings] CHECK CONSTRAINT [FK_PortalCfg_ModuleSettings_PortalCfg_Modules]
GO
/****** Object:  ForeignKey [FK_PortalCfg_Tabs_PortalCfg_Globals]    Script Date: 03/01/2012 23:02:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Tabs_PortalCfg_Globals]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
ALTER TABLE [dbo].[PortalCfg_Tabs]  WITH CHECK ADD  CONSTRAINT [FK_PortalCfg_Tabs_PortalCfg_Globals] FOREIGN KEY([PortalId])
REFERENCES [dbo].[PortalCfg_Globals] ([PortalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PortalCfg_Tabs_PortalCfg_Globals]') AND parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]'))
ALTER TABLE [dbo].[PortalCfg_Tabs] CHECK CONSTRAINT [FK_PortalCfg_Tabs_PortalCfg_Globals]
GO
