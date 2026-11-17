/*
<lang>
  <zh-CN>同形模块首证（HIA.ModuleHostProof）卸载脚本。按 DesktopSourceFile / TabName / PackageId 精确定位并清理安装脚本写入的四张 PortalCfg_* 表，与 PortalCfg_ModuleHostProof_Provision.sql 对称。
  只删本模块写入的行：模块实例、模块定义（在无其它实例引用时）、包状态、页签（在无其它模块引用时）。Profile 允许集回滚与物理包移除不在本脚本内（属部署配置与文件操作）。</zh-CN>
  <en>Same-form first-proof (HIA.ModuleHostProof) deprovisioning script. Precisely locates and clears the four PortalCfg_* tables written by the provisioning script, symmetric with PortalCfg_ModuleHostProof_Provision.sql.
  Only rows written by this module are removed: module instance, module definition (when no other instance references it), package state and tab (when no other module references it). Profile allow-list rollback and physical package removal are out of scope.</en>
</lang>
*/

DECLARE @TabId int = (SELECT [TabId] FROM [dbo].[PortalCfg_Tabs] WHERE [TabName] = N'Module Host Proof');
DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile] = N'DesktopModules/ModuleHostProof/ModuleHostProof.ascx');
IF @DefId IS NOT NULL
BEGIN
    DELETE FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId] = @DefId AND (@TabId IS NULL OR [TabId] = @TabId);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId] = @DefId)
    BEGIN
        DELETE FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [ModuleDefId] = @DefId;
    END
END
DELETE FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId] = N'HIA.ModuleHostProof';
IF @TabId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [TabId] = @TabId)
BEGIN
    DELETE FROM [dbo].[PortalCfg_Tabs] WHERE [TabId] = @TabId;
END
