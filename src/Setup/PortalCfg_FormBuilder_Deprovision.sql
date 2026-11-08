/*
<lang>
  <zh-CN>Form Builder（HIA.FormBuilder，Independent 形态）卸载脚本。删除模块实例，并在其定义不再被任何实例引用时回收定义，同时清理包状态。
  部署 Profile 允许集（web.config 的 Portal.ModuleProfiles.CoreOnly.Packages）中移除 HIA.FormBuilder 属部署配置回滚，不在此脚本内；物理文件保留于 DesktopModules/FormBuilder/，可由部署包单独移除。</zh-CN>
  <en>Form Builder (HIA.FormBuilder, Independent shape) deprovisioning script. Removes the module instance, reclaims the definition when no instance references it, and clears the package state.
  Reverting the deployment profile allow-list (removing HIA.FormBuilder from web.config Portal.ModuleProfiles.CoreOnly.Packages) is a deployment-config rollback outside this script; physical files remain under DesktopModules/FormBuilder/ and may be removed separately by the deployable package.</en>
</lang>
*/

DECLARE @TabId int = (SELECT [TabId] FROM [dbo].[PortalCfg_Tabs] WHERE [TabName] = N'Form Builder');
DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile] = N'DesktopModules/FormBuilder/FormBuilder.ascx');

-- 1) 删除模块实例；若定义已无实例引用则回收定义
IF @DefId IS NOT NULL
BEGIN
    DELETE FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId] = @DefId AND (@TabId IS NULL OR [TabId] = @TabId);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId] = @DefId)
    BEGIN
        DELETE FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [ModuleDefId] = @DefId;
    END
END

-- 2) 清理包状态
DELETE FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId] = N'HIA.FormBuilder';

-- 3) 若专用页签已无任何模块实例承载，则移除该页签
IF @TabId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [TabId] = @TabId)
BEGIN
    DELETE FROM [dbo].[PortalCfg_Tabs] WHERE [TabId] = @TabId;
END
