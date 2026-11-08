/*
<lang>
  <zh-CN>Form Builder（HIA.FormBuilder，Independent 形态）安装脚本。把模块定义、页签/模块实例与包状态写入 PortalCfg_* 表，使容纳机制能实际装载运行。
  本脚本幂等：已存在的页签/定义/实例/包状态不会重复插入。配合部署 Profile 允许集（web.config 的 Portal.ModuleProfiles.CoreOnly.Packages 含 HIA.FormBuilder）共同生效。</zh-CN>
  <en>Form Builder (HIA.FormBuilder, Independent shape) provisioning script. Writes the module definition, tab/module instance and package state into PortalCfg_* tables so the hosting mechanism can actually load and run it.
  This script is idempotent: an existing tab/definition/instance/package-state is not inserted again. It takes effect together with the deployment profile allow-list (web.config Portal.ModuleProfiles.CoreOnly.Packages containing HIA.FormBuilder).</en>
</lang>
*/

-- 1) 页签：不存在才创建（PortalId 固定 1，与种子一致）
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Tabs] WHERE [TabName] = N'Form Builder')
BEGIN
    INSERT INTO [dbo].[PortalCfg_Tabs] ([TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId])
    VALUES (N'Form Builder', 11, N'All Users;', 1, N'Form Builder', 1);
END
DECLARE @TabId int = (SELECT [TabId] FROM [dbo].[PortalCfg_Tabs] WHERE [TabName] = N'Form Builder');

-- 2) 模块定义：按 DesktopSourceFile 去重
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile] = N'DesktopModules/FormBuilder/FormBuilder.ascx')
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModuleDefinitions] ([FriendlyName], [DesktopSourceFile], [MobileSourceFile])
    VALUES (N'Form Builder', N'DesktopModules/FormBuilder/FormBuilder.ascx', N'');
END
DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile] = N'DesktopModules/FormBuilder/FormBuilder.ascx');

-- 3) 模块实例：按「定义 + 页签」去重
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId] = @DefId AND [TabId] = @TabId)
BEGIN
    INSERT INTO [dbo].[PortalCfg_Modules] ([ModuleDefId], [ModuleTitle], [ModuleTitleEn], [PaneName], [EditRoles], [CacheTimeout], [ShowMobile], [ModuleOrder], [TabId])
    VALUES (@DefId, N'表单构建器', N'Form Builder', N'ContentPane', N'Admins;', 0, 1, 1, @TabId);
END

-- 4) 包状态：确保 HIA.FormBuilder 启用
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId] = N'HIA.FormBuilder')
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModulePackageStates] ([PackageId], [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc])
    VALUES (N'HIA.FormBuilder', 1, N'Enabled by Form Builder provisioning (W-anp-P285).', N'Provisioner', SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalCfg_ModulePackageStates] SET [IsEnabled] = 1, [UpdatedBy] = N'Provisioner', [UpdatedUtc] = SYSUTCDATETIME() WHERE [PackageId] = N'HIA.FormBuilder';
END
